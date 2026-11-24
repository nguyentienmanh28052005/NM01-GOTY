// Tiêm vào trang HanaMart24.html: xuất __HANA.store.scene ra glTF 2.0 (JSON + nhiều .bin + PNG riêng).
// Trả về { gltf, bins:[base64], images:[{name,data:base64}], meta }.
window.__exportStore = function (opts) {
  opts = opts || {};
  const MAX_BIN = opts.maxBin || 20 * 1024 * 1024;
  const store = window.__HANA.store, scene = store.scene;
  const THREE = { Vector3: scene.position.constructor, Quaternion: scene.quaternion.constructor, Matrix4: scene.matrix.constructor };
  scene.updateMatrixWorld(true);

  // ---- tên theo khóa của store ----
  const names = new Map();
  const tag = (v, name) => {
    if (!v) return;
    if (v.isObject3D) { if (!names.has(v)) names.set(v, name); return; }
    if (Array.isArray(v)) { v.forEach((x, i) => tag(x, name + '[' + i + ']')); return; }
    if (typeof v === 'object' && !v.isMaterial && !v.isTexture && !v.isBufferGeometry)
      for (const k of Object.keys(v)) if (v[k] && v[k].isObject3D) tag(v[k], name + '.' + k);
  };
  for (const k of Object.keys(store.refs)) tag(store.refs[k], 'R:' + k);
  for (const k of Object.keys(store)) if (!['scene', 'refs', 'renderer', 'hits'].includes(k)) tag(store[k], 'S:' + k);
  store.hits.forEach((h, i) => names.set(h, 'H:' + (h.userData && (h.userData.id || h.userData.key) || i)));

  // ---- hàng hóa: geometry -> type ----
  const geoType = new Map();
  for (const t of Object.keys(store._geos || {})) geoType.set(store._geos[t].g, t);
  const productMeshes = new Set();
  const products = []; // {type, v, restock, m4}
  for (const k of Object.keys(store._buckets || {})) {
    const b = store._buckets[k];
    for (const key of Object.keys(b)) {
      if (key === 'type' || key === 'v') continue;
      const arr = b[key]; if (!Array.isArray(arr)) continue;
      arr.forEach(m4 => products.push({ type: b.type, v: b.v, restock: key === 'mats' ? null : key, m4 }));
    }
  }
  scene.traverse(o => { if (o.isInstancedMesh && geoType.has(o.geometry)) productMeshes.add(o); });

  // ---- bộ đệm nhị phân ----
  const bins = [[]]; let binLen = [0];
  const gltf = { asset: { version: '2.0', generator: 'HanaMart24 three-export' }, scene: 0, scenes: [{ name: 'Store', nodes: [] }],
    nodes: [], meshes: [], materials: [], textures: [], images: [], samplers: [], accessors: [], bufferViews: [], buffers: [] };
  function pushBytes(u8, target) {
    let bi = bins.length - 1;
    if (binLen[bi] + u8.byteLength > MAX_BIN && binLen[bi] > 0) { bins.push([]); binLen.push(0); bi++; }
    const pad = (4 - (binLen[bi] % 4)) % 4; if (pad) { bins[bi].push(new Uint8Array(pad)); binLen[bi] += pad; }
    const off = binLen[bi]; bins[bi].push(u8); binLen[bi] += u8.byteLength;
    const bv = { buffer: bi, byteOffset: off, byteLength: u8.byteLength }; if (target) bv.target = target;
    gltf.bufferViews.push(bv); return gltf.bufferViews.length - 1;
  }
  function accessor(arr, type, comp, target, minmax) {
    const bv = pushBytes(new Uint8Array(arr.buffer, arr.byteOffset, arr.byteLength), target);
    const n = { 'SCALAR': 1, 'VEC2': 2, 'VEC3': 3, 'VEC4': 4 }[type];
    const a = { bufferView: bv, componentType: comp, count: arr.length / n, type };
    if (minmax) { const mn = Array(n).fill(Infinity), mx = Array(n).fill(-Infinity);
      for (let i = 0; i < arr.length; i++) { const c = i % n; if (arr[i] < mn[c]) mn[c] = arr[i]; if (arr[i] > mx[c]) mx[c] = arr[i]; }
      a.min = mn; a.max = mx; }
    gltf.accessors.push(a); return gltf.accessors.length - 1;
  }
  const readAttr = (attr, n) => { const out = new Float32Array(attr.count * n);
    for (let i = 0; i < attr.count; i++) { out[i * n] = attr.getX(i); if (n > 1) out[i * n + 1] = attr.getY(i); if (n > 2) out[i * n + 2] = attr.getZ(i); if (n > 3) out[i * n + 3] = attr.getW(i); }
    return out; };

  // ---- texture ----
  const texIndex = new Map(); const images = [];
  function texture(t) {
    if (!t) return undefined;
    if (texIndex.has(t)) return texIndex.get(t);
    const img = t.image; if (!img || !(img.width > 0)) return undefined;
    const c = document.createElement('canvas'); c.width = img.width; c.height = img.height; const x = c.getContext('2d');
    if (t.flipY) { x.translate(0, c.height); x.scale(1, -1); }
    try { x.drawImage(img, 0, 0); } catch (e) {
      if (img.data) { const id = x.createImageData(img.width, img.height); id.data.set(img.data.length === id.data.length ? img.data : new Uint8ClampedArray(id.data.length)); x.putImageData(id, 0, 0); } else return undefined; }
    const name = 'tex_' + images.length + '.png';
    images.push({ name, data: c.toDataURL('image/png').split(',')[1] });
    gltf.images.push({ uri: 'textures/' + name, mimeType: 'image/png' });
    const wrap = w => w === 1000 ? 10497 : w === 1002 ? 33648 : 33071; // Repeat, Mirrored, Clamp
    gltf.samplers.push({ wrapS: wrap(t.wrapS), wrapT: wrap(t.wrapT), magFilter: 9729, minFilter: 9987 });
    gltf.textures.push({ source: gltf.images.length - 1, sampler: gltf.samplers.length - 1 });
    const idx = gltf.textures.length - 1; texIndex.set(t, idx); return idx;
  }
  function texInfo(t) {
    const i = texture(t); if (i === undefined) return undefined;
    const info = { index: i };
    if (t.repeat.x !== 1 || t.repeat.y !== 1 || t.offset.x !== 0 || t.offset.y !== 0 || t.rotation !== 0)
      info.extensions = { KHR_texture_transform: { scale: [t.repeat.x, t.repeat.y], offset: [t.offset.x, t.offset.y], rotation: t.rotation } };
    return info;
  }

  // ---- material ----
  const matIndex = new Map();
  function material(m) {
    if (matIndex.has(m)) return matIndex.get(m);
    const col = m.color ? m.color.toArray() : [1, 1, 1];
    const g = { name: m.name || (m.type + '_' + gltf.materials.length), pbrMetallicRoughness: {
      baseColorFactor: [col[0], col[1], col[2], m.opacity ?? 1],
      metallicFactor: m.metalness ?? 0, roughnessFactor: m.roughness ?? 1 }, extras: { threeType: m.type } };
    const bm = texInfo(m.map); if (bm) g.pbrMetallicRoughness.baseColorTexture = bm;
    const nm = texInfo(m.normalMap); if (nm) { g.normalTexture = nm; if (m.normalScale) g.normalTexture.scale = m.normalScale.x; }
    const rm = texInfo(m.roughnessMap || m.metalnessMap); if (rm) g.extras.roughnessMap = rm.index;
    if (m.emissive) { const e = m.emissive.toArray().map(v => v * (m.emissiveIntensity ?? 1)); const mx = Math.max(...e);
      if (mx > 0) { g.emissiveFactor = mx > 1 ? e.map(v => v / mx) : e; if (mx > 1) g.extras.emissiveStrength = mx;
        const em = texInfo(m.emissiveMap); if (em) g.emissiveTexture = em; } }
    if (m.transparent || (m.opacity ?? 1) < 1) g.alphaMode = 'BLEND'; else if (m.alphaTest > 0) { g.alphaMode = 'MASK'; g.alphaCutoff = m.alphaTest; }
    if (m.side === 2) g.doubleSided = true;
    if (m.isMeshBasicMaterial) g.extras.unlit = true;
    if (m.isShaderMaterial) g.extras.shader = true;
    for (const k of ['clearcoat', 'clearcoatRoughness', 'transmission', 'envMapIntensity', 'depthWrite', 'blending', 'toneMapped', 'vertexColors'])
      if (m[k] !== undefined) g.extras[k] = m[k];
    if (m.isMeshBasicMaterial) { g.extensions = { KHR_materials_unlit: {} }; }
    gltf.materials.push(g); const i = gltf.materials.length - 1; matIndex.set(m, i); return i;
  }

  // ---- mesh ----
  const meshIndex = new Map();
  function mesh(geo, mats) {
    const key = geo.uuid + '|' + (Array.isArray(mats) ? mats.map(m => m.uuid).join(',') : mats.uuid);
    if (meshIndex.has(key)) return meshIndex.get(key);
    const attrs = {};
    attrs.POSITION = accessor(readAttr(geo.attributes.position, 3), 'VEC3', 5126, 34962, true);
    if (geo.attributes.normal) attrs.NORMAL = accessor(readAttr(geo.attributes.normal, 3), 'VEC3', 5126, 34962);
    if (geo.attributes.uv) { const uv = readAttr(geo.attributes.uv, 2); attrs.TEXCOORD_0 = accessor(uv, 'VEC2', 5126, 34962); }
    if (geo.attributes.color) attrs.COLOR_0 = accessor(readAttr(geo.attributes.color, geo.attributes.color.itemSize), geo.attributes.color.itemSize === 4 ? 'VEC4' : 'VEC3', 5126, 34962);
    const prims = [];
    const nv = geo.attributes.position.count;
    let index = geo.index ? Array.from(geo.index.array) : null;
    const groups = (Array.isArray(mats) && geo.groups.length) ? geo.groups : [{ start: 0, count: index ? index.length : nv, materialIndex: 0 }];
    for (const gr of groups) {
      const m = Array.isArray(mats) ? mats[gr.materialIndex] : mats; if (!m) continue;
      const p = { attributes: attrs, material: material(m), mode: 4 };
      const cnt = Math.min(gr.count, (index ? index.length : nv) - gr.start);
      if (cnt <= 0) continue;
      const idx = index ? index.slice(gr.start, gr.start + cnt) : Array.from({ length: cnt }, (_, i) => gr.start + i);
      p.indices = accessor(nv > 65535 ? new Uint32Array(idx) : new Uint16Array(idx.length % 2 ? idx.concat([idx[idx.length - 1]]).slice(0, idx.length) : idx), 'SCALAR', nv > 65535 ? 5125 : 5123, 34963);
      prims.push(p);
    }
    gltf.meshes.push({ name: geo.name || 'mesh' + gltf.meshes.length, primitives: prims });
    const i = gltf.meshes.length - 1; meshIndex.set(key, i); return i;
  }

  const safeExtras = ud => { const o = {}; for (const k of Object.keys(ud || {})) { const v = ud[k];
    if (v == null || ['string', 'number', 'boolean'].includes(typeof v)) o[k] = v;
    else if (Array.isArray(v) && v.every(x => typeof x === 'number' || typeof x === 'string')) o[k] = v; } return o; };
  const trs = (m4, n) => { const p = new THREE.Vector3(), q = new THREE.Quaternion(), s = new THREE.Vector3(); m4.decompose(p, q, s);
    n.translation = p.toArray(); n.rotation = q.toArray(); n.scale = s.toArray(); };

  const counts = { lights: 0, hits: store.hits.length, colliders: store.colliders.length, productInstances: products.length, skipped: {} };
  let lightIdx = 0;
  function node(o) {
    if (productMeshes.has(o)) return null;
    if (o.isPoints || o.isLine || o.isLineSegments || o.isSprite) { counts.skipped[o.type] = (counts.skipped[o.type] || 0) + 1;
      const n = { name: (names.get(o) || 'o' + o.id) + '#' + o.type, extras: { skipped: o.type } }; trs(o.matrix, n); gltf.nodes.push(n); return gltf.nodes.length - 1; }
    const n = { name: names.get(o) || ('o' + o.id) };
    trs(o.matrix, n);
    const ex = safeExtras(o.userData); if (!o.visible) ex.hidden = true; if (o.castShadow) ex.castShadow = true;
    if (o.isLight) {
      n.name = 'L:' + (lightIdx++) + (names.has(o) ? '|' + names.get(o) : ''); counts.lights++;
      ex.kind = o.isRectAreaLight ? 'RectArea' : o.isSpotLight ? 'Spot' : o.isPointLight ? 'Point' : o.isHemisphereLight ? 'Hemisphere' : o.isDirectionalLight ? 'Directional' : o.type;
      ex.color = o.color.toArray(); ex.intensity = o.intensity;
      if (o.groundColor) ex.groundColor = o.groundColor.toArray();
      for (const k of ['distance', 'decay', 'angle', 'penumbra', 'width', 'height']) if (o[k] !== undefined) ex[k] = o[k];
      if (o.target && (o.isSpotLight || o.isDirectionalLight)) ex.target = o.target.getWorldPosition(new THREE.Vector3()).toArray();
      ex.castShadow = !!o.castShadow;
    }
    if (o.isInstancedMesh) { // không phải hàng hóa: bung từng instance thành node con
      const kids = []; const m4 = new THREE.Matrix4();
      for (let i = 0; i < o.count; i++) { o.getMatrixAt(i, m4); const c = { name: n.name + '#' + i, mesh: mesh(o.geometry, o.material) }; trs(m4, c); gltf.nodes.push(c); kids.push(gltf.nodes.length - 1); }
      n.children = kids;
    } else if (o.isMesh && o.geometry && o.geometry.attributes.position) {
      n.mesh = mesh(o.geometry, o.material);
      if (o.material && o.material.isShaderMaterial) ex.shaderMaterial = true;
    }
    const kids = (n.children || []).slice();
    for (const c of o.children) { const ci = node(c); if (ci !== null) kids.push(ci); }
    if (kids.length) n.children = kids;
    if (Object.keys(ex).length) n.extras = ex;
    gltf.nodes.push(n); return gltf.nodes.length - 1;
  }
  for (const c of scene.children) { const i = node(c); if (i !== null) gltf.scenes[0].nodes.push(i); }

  // ---- hàng hóa: node P:type:variant[:restock] mang mesh gốc (để giữ thuốc lá + đối chiếu) ----
  const pRoot = { name: 'Products', children: [] };
  for (const p of products) {
    const G = store._geos[p.type];
    const im = [...productMeshes].find(m => m.geometry === G.g && m.material === store._matFor(p.type, p.v));
    const n = { name: 'P:' + p.type + ':' + p.v + (p.restock ? ':' + p.restock : ''), extras: { type: p.type, variant: p.v, restock: p.restock, w: G.w, h: G.h, d: G.d } };
    if (im) n.mesh = mesh(G.g, im.material);
    trs(p.m4, n); gltf.nodes.push(n); pRoot.children.push(gltf.nodes.length - 1);
  }
  gltf.nodes.push(pRoot); gltf.scenes[0].nodes.push(gltf.nodes.length - 1);

  // ---- buffers ----
  const binsB64 = bins.map((parts, i) => { const len = binLen[i]; const u8 = new Uint8Array(len); let o = 0; for (const p of parts) { u8.set(p, o); o += p.byteLength; }
    gltf.buffers.push({ uri: 'store_' + i + '.bin', byteLength: len });
    let s = ''; const CH = 0x8000; for (let k = 0; k < u8.length; k += CH) s += String.fromCharCode.apply(null, u8.subarray(k, k + CH)); return btoa(s); });
  gltf.extensionsUsed = ['KHR_texture_transform', 'KHR_materials_unlit'];
  for (const n of gltf.nodes) if (n.children && !n.children.length) delete n.children;

  const meta = {
    background: scene.background ? scene.background.toArray() : null,
    fog: scene.fog ? { color: scene.fog.color.toArray(), density: scene.fog.density ?? null, near: scene.fog.near ?? null, far: scene.fog.far ?? null } : null,
    colliders: store.colliders,
    tubes: store.lights.map(l => ({ x: l.x, z: l.z, base: l.base, zone: l.zone, group: store.lightGroups.indexOf(l.group), color: l.col ? l.col.toArray() : null })),
    lightGroups: store.lightGroups.map(g => ({ base: g.base, light: names.get(g.light) || null })),
    geos: Object.fromEntries(Object.entries(store._geos).map(([k, v]) => [k, { w: v.w, h: v.h, d: v.d }])),
    restockTags: Object.fromEntries(Object.entries(store.restock).map(([k, v]) => [k, v.tag ? names.get(v.tag) || null : null])),
    counts, exposure: window.__HANA.composer ? 1.02 : 1.02
  };
  return { gltf, bins: binsB64, images, meta };
};

using System.Collections.Generic;
using System.IO;
using System.Linq;
using HanaMart.Store;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace HanaMart.EditorTools
{
    /// Dựng cửa hàng trong scene HanaMart24 từ store.gltf (xuất từ bản Three.js) + store_meta.json.
    public static class StoreImporter
    {
        public const string GltfPath = "Assets/HanaMart/Art/FromThree/store.gltf";
        public const string MetaPath = "Assets/HanaMart/Art/FromThree/store_meta.json";
        public const string ScenePath = "Assets/HanaMart/Scenes/HanaMart24.unity";
        const string MatDir = "Assets/HanaMart/Art/Materials/Converted";
        const float StoreHeight = 3.0f;

        public const string ProbeCubemapPath = "Assets/HanaMart/Art/FromThree/StoreProbe.cubemap";

        /// Chụp cubemap tại vị trí probe (tắt probe khi chụp) rồi gán làm customBakedTexture.
        [MenuItem("HanaMart/Bake Store Probe")]
        public static void BakeProbe()
        {
            var probe = Object.FindObjectsByType<ReflectionProbe>(FindObjectsSortMode.None).FirstOrDefault(x => x.name == "StoreProbe");
            if (probe == null) { Debug.LogError("Không thấy StoreProbe"); return; }
            probe.enabled = false;
            var camGo = new GameObject("ProbeCam") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>(); cam.transform.position = probe.transform.position;
            cam.nearClipPlane = 0.05f; cam.farClipPlane = 100f; cam.allowHDR = true; cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black;
            var cube = new Cubemap(256, TextureFormat.RGBAHalf, true);
            cam.RenderToCubemap(cube);
            Object.DestroyImmediate(camGo);
            var old = AssetDatabase.LoadAssetAtPath<Cubemap>(ProbeCubemapPath);
            if (old != null) { EditorUtility.CopySerialized(cube, old); cube = old; } else AssetDatabase.CreateAsset(cube, ProbeCubemapPath);
            probe.mode = ReflectionProbeMode.Custom; probe.customBakedTexture = cube; probe.enabled = true;
            EditorUtility.SetDirty(probe); AssetDatabase.SaveAssets();
        }

        [MenuItem("HanaMart/Build Store")]
        public static void BuildAndSave()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Build(scene);
            BakeProbe();
            EditorSceneManager.SaveScene(scene);
        }

        public static GameObject Build(Scene scene)
        {
            foreach (var g in scene.GetRootGameObjects().Where(g => g.name == "Store").ToList()) Object.DestroyImmediate(g);
            var gltf = JObject.Parse(File.ReadAllText(GltfPath));
            var meta = JObject.Parse(File.ReadAllText(MetaPath));

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GltfPath);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
            PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            root.name = "Store";

            var convert = new MaterialConverter(gltf);
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
                r.sharedMaterials = r.sharedMaterials.Select(convert.Convert).ToArray();

            // ghép node glTF với GameObject theo thứ tự cây
            var nodes = (JArray)gltf["nodes"];
            var sceneNodes = (JArray)gltf["scenes"][0]["nodes"];
            var top = Enumerable.Range(0, root.transform.childCount).Select(i => root.transform.GetChild(i)).ToList();
            Walk(nodes, sceneNodes, top);

            ReplaceProducts(root);
            AddColliders(root, (JArray)meta["colliders"]);
            AddProbe(root);
            ApplySceneSettings(scene, gltf, meta);
            return root;
        }

        static void Walk(JArray nodes, JArray indices, List<Transform> candidates)
        {
            int ptr = 0;
            foreach (var idxTok in indices)
            {
                var j = (JObject)nodes[(int)idxTok];
                string name = (string)j["name"] ?? "";
                Transform t = null;
                for (int k = ptr; k < candidates.Count; k++) if (SameNode(candidates[k].name, name)) { t = candidates[k]; ptr = k + 1; break; }
                if (t == null) continue;
                Mark(t.gameObject, name, j["extras"] as JObject);
                if (j["children"] is JArray ch)
                    Walk(nodes, ch, Enumerable.Range(0, t.childCount).Select(i => t.GetChild(i)).ToList());
            }
        }

        /// glTFast thêm hậu tố _0, _1… cho node trùng tên.
        static bool SameNode(string unityName, string gltfName) =>
            unityName == gltfName || (unityName.Length > gltfName.Length + 1 && unityName.StartsWith(gltfName + "_") && unityName.Substring(gltfName.Length + 1).All(char.IsDigit));

        static void Mark(GameObject go, string name, JObject ex)
        {
            StoreMarker.Kind kind;
            if (name.StartsWith("P:")) kind = StoreMarker.Kind.Product;
            else if (name.StartsWith("L:")) kind = StoreMarker.Kind.Light;
            else if (name.StartsWith("H:")) kind = StoreMarker.Kind.Hit;
            else if (name.StartsWith("S:") || name.StartsWith("R:")) kind = StoreMarker.Kind.Ref;
            else if (ex != null && ex["skipped"] != null) kind = StoreMarker.Kind.Skipped;
            else if (ex != null && ex["shaderMaterial"] != null) kind = StoreMarker.Kind.ShaderMesh;
            else kind = StoreMarker.Kind.Static;
            if (ex != null && ex["shaderMaterial"] != null && kind == StoreMarker.Kind.Ref) kind = StoreMarker.Kind.ShaderMesh;

            if (kind != StoreMarker.Kind.Static)
            {
                var m = go.AddComponent<StoreMarker>(); m.kind = kind; m.key = name; m.json = ex?.ToString(Newtonsoft.Json.Formatting.None) ?? "";
            }
            else if (go.GetComponent<MeshRenderer>())
                GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic | StaticEditorFlags.ReflectionProbeStatic | StaticEditorFlags.OccludeeStatic | StaticEditorFlags.OccluderStatic);

            if (kind == StoreMarker.Kind.Light) AddLight(go, ex);
            if (kind == StoreMarker.Kind.Hit)
            {
                var mr = go.GetComponent<MeshRenderer>();
                var col = go.AddComponent<BoxCollider>(); col.isTrigger = true;
                if (mr) mr.enabled = false;
                go.layer = EnsureLayer("Interact");
            }
            if (kind == StoreMarker.Kind.ShaderMesh) // shader riêng (quầng đèn, vệt đường ướt, mảng mục) — port ở kế hoạch sau
                foreach (var r in go.GetComponents<Renderer>()) r.enabled = false;
            if (ex != null && (bool?)ex["hidden"] == true && kind != StoreMarker.Kind.Hit) go.SetActive(false);
            if (go.TryGetComponent<MeshRenderer>(out var rr))
                rr.shadowCastingMode = ex != null && (bool?)ex["castShadow"] == true ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        /// Màu Three.js (r152+) là tuyến tính; Light.color / RenderSettings của Unity nhận màu sRGB.
        static Color Col(JToken t, float k = 1f) => new Color((float)t[0] * k, (float)t[1] * k, (float)t[2] * k).gamma;

        static void AddLight(GameObject go, JObject ex)
        {
            string kind = (string)ex["kind"];
            float I = (float)ex["intensity"];
            if (kind == "Hemisphere")
            {
                var hs = go.AddComponent<HemisphereAmbient>(); hs.sky = Col(ex["color"]); hs.ground = Col(ex["groundColor"]); hs.intensity = I;
                return;
            }
            var holder = new GameObject("light");
            holder.transform.SetParent(go.transform, false);
            var l = holder.AddComponent<Light>();
            l.color = Col(ex["color"]);
            l.shadows = (bool?)ex["castShadow"] == true ? LightShadows.Soft : LightShadows.None;
            float dist = (float?)ex["distance"] ?? 0f;
            switch (kind)
            {
                case "Point":
                    l.type = LightType.Point; l.range = dist > 0 ? dist : 25f; l.intensity = I / Mathf.PI; break; // distance 0 của Three.js = không giới hạn
                case "Spot":
                    l.type = LightType.Spot; l.range = dist > 0 ? dist : 25f; l.intensity = I / Mathf.PI;
                    float a = (float)ex["angle"] * Mathf.Rad2Deg * 2f; l.spotAngle = a; l.innerSpotAngle = a * (1f - ((float?)ex["penumbra"] ?? 0f));
                    if (ex["target"] is JArray tg) holder.transform.LookAt(ThreeCoords.ToUnity((float)tg[0], (float)tg[1], (float)tg[2]));
                    break;
                case "RectArea": // URP không có đèn vùng realtime → đèn spot rộng chiếu theo hướng -Z của Three.js
                    l.type = LightType.Spot; l.spotAngle = 150f; l.innerSpotAngle = 40f; l.range = 8f;
                    l.intensity = I * (float)ex["width"] * (float)ex["height"] / Mathf.PI * 1.2f; // hệ số chỉnh theo ảnh so sánh (Task 7)
                    holder.transform.localRotation = Quaternion.Euler(0, 180, 0);
                    var ar = holder.AddComponent<AreaLightInfo>(); ar.width = (float)ex["width"]; ar.height = (float)ex["height"]; ar.threeIntensity = I;
                    break;
                default:
                    l.type = LightType.Directional; l.intensity = I; break;
            }
        }

        /// Thay hình học hàng hóa Three.js bằng prefab asset siêu thị (giữ thuốc lá), co vừa ô (w,h,d) của bản gốc.
        static void ReplaceProducts(GameObject root)
        {
            var cat = AssetDatabase.LoadAssetAtPath<ProductCatalog>(ProductExtractor.CatalogPath);
            if (cat == null) return;
            int i = 0;
            foreach (var m in root.GetComponentsInChildren<StoreMarker>(true).Where(x => x.kind == StoreMarker.Kind.Product))
            {
                var ex = JObject.Parse(m.json); string type = (string)ex["type"];
                var e = cat.Find(type); if (e == null || e.prefabs.Length == 0) continue;
                var prefab = e.prefabs[(i++ * 7 + (int)ex["variant"]) % e.prefabs.Length];
                Object.DestroyImmediate(m.GetComponent<MeshRenderer>()); Object.DestroyImmediate(m.GetComponent<MeshFilter>());
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, m.transform);
                var size = ProductExtractor.PrefabSize(prefab);
                float s = Mathf.Min((float)ex["w"] / size.x, (float)ex["h"] / size.y, (float)ex["d"] / size.z) * 0.98f;
                inst.transform.localPosition = Vector3.zero; inst.transform.localRotation = Quaternion.identity; inst.transform.localScale = Vector3.one * s;
            }
        }

        static void AddColliders(GameObject root, JArray cols)
        {
            var parent = new GameObject("Colliders"); parent.transform.SetParent(root.transform, false);
            int i = 0;
            foreach (JArray c in cols)
            {
                float x0 = (float)c[0], z0 = (float)c[1], x1 = (float)c[2], z1 = (float)c[3];
                var go = new GameObject("C:" + i++); go.transform.SetParent(parent.transform, false);
                go.transform.localPosition = ThreeCoords.ToUnity((x0 + x1) / 2f, StoreHeight / 2f, (z0 + z1) / 2f);
                var bc = go.AddComponent<BoxCollider>(); bc.size = new Vector3(Mathf.Abs(x1 - x0), StoreHeight, Mathf.Abs(z1 - z0));
                var m = go.AddComponent<StoreMarker>(); m.kind = StoreMarker.Kind.Collider; m.key = go.name; m.json = c.ToString(Newtonsoft.Json.Formatting.None);
            }
        }

        static void AddProbe(GameObject root)
        {
            var go = new GameObject("StoreProbe"); go.transform.SetParent(root.transform, false);
            go.transform.localPosition = ThreeCoords.ToUnity(6.2f, 1.55f, 4.0f);
            var p = go.AddComponent<ReflectionProbe>();
            // Probe "Custom": cubemap chụp sẵn lúc đủ đèn (như captureProbe bản Three.js); probe realtime làm Forward+ của URP lỗi trong Editor
            p.mode = ReflectionProbeMode.Custom;
            p.customBakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(ProbeCubemapPath);
            p.boxProjection = true; p.size = new Vector3(12, 3, 8);
            p.center = ThreeCoords.ToUnity(6f, 1.5f, 4f) - go.transform.localPosition;
            p.resolution = 256; p.hdr = true; p.importance = 1;
        }

        static void ApplySceneSettings(Scene scene, JObject gltf, JObject meta)
        {
            var prev = SceneManager.GetActiveScene();
            SceneManager.SetActiveScene(scene);
            if (meta["fog"] is JObject f)
            {
                RenderSettings.fog = true; RenderSettings.fogMode = FogMode.ExponentialSquared;
                RenderSettings.fogColor = Col(f["color"]); RenderSettings.fogDensity = (float)f["density"];
            }
            var cam = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Camera>(true)).FirstOrDefault();
            if (cam && meta["background"] is JArray bg) cam.backgroundColor = Col(bg);
            if (prev.IsValid() && prev != scene) SceneManager.SetActiveScene(prev);
        }

        static int EnsureLayer(string name)
        {
            int l = LayerMask.NameToLayer(name); if (l >= 0) return l;
            var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
            var layers = tm.FindProperty("layers");
            for (int i = 8; i < layers.arraySize; i++)
            {
                var e = layers.GetArrayElementAtIndex(i);
                if (string.IsNullOrEmpty(e.stringValue)) { e.stringValue = name; tm.ApplyModifiedProperties(); return i; }
            }
            return 0;
        }

        /// Chuyển material glTFast sang URP Lit/Unlit, lưu thành asset (tái dùng nếu đã có).
        class MaterialConverter
        {
            readonly Dictionary<string, JObject> src = new Dictionary<string, JObject>();
            readonly Dictionary<Material, Material> cache = new Dictionary<Material, Material>();
            public MaterialConverter(JObject gltf)
            {
                Directory.CreateDirectory(MatDir);
                foreach (JObject m in (JArray)gltf["materials"]) { var n = (string)m["name"]; if (!src.ContainsKey(n)) src[n] = m; }
            }

            public Material Convert(Material m)
            {
                if (m == null) return null;
                if (cache.TryGetValue(m, out var c)) return c;
                src.TryGetValue(m.name, out var j);
                var ex = j?["extras"] as JObject;
                bool unlit = ex != null && (bool?)ex["unlit"] == true;
                string path = $"{MatDir}/{Sanitize(m.name)}.mat";
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
                if (mat == null) { mat = new Material(shader); AssetDatabase.CreateAsset(mat, path); }
                mat.shader = shader;

                var baseTex = m.HasProperty("baseColorTexture") ? m.GetTexture("baseColorTexture") : null;
                var st = m.HasProperty("baseColorTexture_ST") ? m.GetVector("baseColorTexture_ST") : new Vector4(1, 1, 0, 0);
                // glTF lưu baseColorFactor ở không gian tuyến tính; _BaseColor của URP nhận màu sRGB
                var bcf = j?["pbrMetallicRoughness"]?["baseColorFactor"] as JArray;
                var baseCol = bcf != null ? new Color((float)bcf[0], (float)bcf[1], (float)bcf[2]).gamma : Color.white;
                baseCol.a = bcf != null ? (float)bcf[3] : 1f;
                mat.SetColor("_BaseColor", baseCol);
                mat.SetTexture("_BaseMap", baseTex);
                mat.SetTextureScale("_BaseMap", new Vector2(st.x, st.y)); mat.SetTextureOffset("_BaseMap", new Vector2(st.z, st.w));

                if (!unlit)
                {
                    mat.SetFloat("_Metallic", m.HasProperty("metallicFactor") ? m.GetFloat("metallicFactor") : 0f);
                    mat.SetFloat("_Smoothness", 1f - (m.HasProperty("roughnessFactor") ? m.GetFloat("roughnessFactor") : 1f));
                    var nrm = m.HasProperty("normalTexture") ? m.GetTexture("normalTexture") : null;
                    mat.SetTexture("_BumpMap", nrm);
                    if (nrm) { mat.EnableKeyword("_NORMALMAP"); mat.SetFloat("_BumpScale", m.HasProperty("normalTexture_scale") ? m.GetFloat("normalTexture_scale") : 1f); }
                    else mat.DisableKeyword("_NORMALMAP");
                    var e = j?["emissiveFactor"] as JArray;
                    if (e != null && e.Any(v => (float)v > 0))
                    {
                        float k = ex != null && ex["emissiveStrength"] != null ? (float)ex["emissiveStrength"] : 1f;
                        mat.SetColor("_EmissionColor", new Color((float)e[0], (float)e[1], (float)e[2]).gamma * k);
                        mat.SetTexture("_EmissionMap", m.HasProperty("emissiveTexture") ? m.GetTexture("emissiveTexture") : null);
                        mat.EnableKeyword("_EMISSION"); mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                    }
                    else { mat.DisableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", Color.black); }
                }

                string alpha = (string)j?["alphaMode"] ?? "OPAQUE";
                mat.SetFloat("_Surface", alpha == "BLEND" ? 1 : 0);
                mat.SetFloat("_AlphaClip", alpha == "MASK" ? 1 : 0);
                if (alpha == "MASK") mat.SetFloat("_Cutoff", (float?)j["alphaCutoff"] ?? 0.5f);
                mat.SetFloat("_Cull", (bool?)j?["doubleSided"] == true ? 0 : 2);
                if (ex != null && ex["depthWrite"] != null && (bool)ex["depthWrite"] == false) mat.SetFloat("_ZWrite", 0);
                if (ex != null && ex["blending"] != null && (int)ex["blending"] == 2) mat.SetFloat("_Blend", 2); // Additive
                BaseShaderGUI.SetupMaterialBlendMode(mat);
                EditorUtility.SetDirty(mat);
                cache[m] = mat; return mat;
            }

            static string Sanitize(string s) => string.Concat(s.Select(ch => Path.GetInvalidFileNameChars().Contains(ch) || ch == '|' ? '_' : ch));
        }
    }
}

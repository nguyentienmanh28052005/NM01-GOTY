"""Kiểm tra kết quả xuất cảnh Three.js (Task 3). Chạy: python3 test_export.py"""
import json, os, struct, math, sys
OUT = os.path.join(os.path.dirname(__file__), 'out')

def load():
    g = json.load(open(os.path.join(OUT, 'store.gltf'), encoding='utf-8'))
    meta = json.load(open(os.path.join(OUT, 'store_meta.json'), encoding='utf-8'))
    return g, meta

def test_gltf_valid():
    g, _ = load()
    assert g['asset']['version'] == '2.0'
    for b in g['buffers']:
        p = os.path.join(OUT, b['uri']); assert os.path.getsize(p) == b['byteLength'], b['uri']
        assert os.path.getsize(p) < 24 * 1024 * 1024, 'buffer quá 24MB: ' + b['uri']

def names(g, prefix): return [n for n in g['nodes'] if n.get('name', '').startswith(prefix)]

def test_counts_match_page():
    g, meta = load(); c = meta['counts']
    assert len(names(g, 'P:')) == c['productInstances'] > 1000
    assert len(names(g, 'L:')) == c['lights'] == 15
    assert len(names(g, 'H:')) == c['hits'] == 33
    assert len(meta['colliders']) == c['colliders'] == 38

def test_images_exported():
    g, _ = load()
    assert len(g['images']) >= 100
    for im in g['images']:
        assert os.path.exists(os.path.join(OUT, im['uri'])), im['uri']

def test_no_nan_positions():
    g, _ = load()
    bins = {i: open(os.path.join(OUT, b['uri']), 'rb').read() for i, b in enumerate(g['buffers'])}
    for m in g['meshes']:
        for p in m['primitives']:
            a = g['accessors'][p['attributes']['POSITION']]
            assert all(math.isfinite(v) for v in a['min'] + a['max']), m.get('name')

def test_light_extras_complete():
    g, _ = load()
    for n in names(g, 'L:'):
        e = n['extras']
        for k in ('kind', 'color', 'intensity'): assert k in e, (n['name'], k)
        if e['kind'] == 'RectArea': assert e['width'] > 0 and e['height'] > 0

if __name__ == '__main__':
    fails = 0
    for k, f in list(globals().items()):
        if k.startswith('test_'):
            try: f(); print('PASS', k)
            except Exception as ex: fails += 1; print('FAIL', k, '-', repr(ex)[:300])
    print(f'{fails} failed'); sys.exit(1 if fails else 0)

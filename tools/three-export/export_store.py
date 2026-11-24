"""Xuất cảnh cửa hàng của HanaMart24.html (Three.js) ra glTF + meta. Chạy: python3 export_store.py [đường dẫn html]"""
import base64, json, os, sys
from playwright.sync_api import sync_playwright
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'out')
HTML = sys.argv[1] if len(sys.argv) > 1 else '/mnt/user-data/uploads/HanaMart24/HanaMart24.html'

def main():
    os.makedirs(os.path.join(OUT, 'textures'), exist_ok=True)
    with sync_playwright() as p:
        b = p.chromium.launch(executable_path='/usr/bin/google-chrome', args=['--use-gl=swiftshader', '--enable-unsafe-swiftshader'])
        pg = b.new_page(viewport={'width': 1280, 'height': 720}); pg.set_default_timeout(600000)
        errs = []; pg.on('pageerror', lambda e: errs.append(str(e)))
        pg.goto('file://' + HTML + '?q=ultra&nofade')
        pg.wait_for_function('() => window.__HANA && window.__HANA.store', timeout=600000)
        pg.add_script_tag(path=os.path.join(HERE, 'gltf_writer.js'))
        r = pg.evaluate('() => window.__exportStore()')
        b.close()
    for i, s in enumerate(r['bins']):
        open(os.path.join(OUT, f'store_{i}.bin'), 'wb').write(base64.b64decode(s))
    for im in r['images']:
        open(os.path.join(OUT, 'textures', im['name']), 'wb').write(base64.b64decode(im['data']))
    json.dump(r['gltf'], open(os.path.join(OUT, 'store.gltf'), 'w', encoding='utf-8'), ensure_ascii=False)
    json.dump(r['meta'], open(os.path.join(OUT, 'store_meta.json'), 'w', encoding='utf-8'), ensure_ascii=False, indent=1)
    print('nodes', len(r['gltf']['nodes']), 'meshes', len(r['gltf']['meshes']), 'materials', len(r['gltf']['materials']),
          'images', len(r['images']), 'bins', len(r['bins']), 'counts', r['meta']['counts'], 'errors', errs[:5])

if __name__ == '__main__': main()

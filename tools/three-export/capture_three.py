"""Chụp ảnh tham chiếu bản Three.js tại các góc trong viewpoints.json → out/ref/<name>.png (1280×720, FOV 72)."""
import json, os, sys
from playwright.sync_api import sync_playwright
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'out', 'ref')
HTML = sys.argv[1] if len(sys.argv) > 1 else '/mnt/user-data/uploads/HanaMart24/HanaMart24.html'
VP = json.load(open(os.path.join(HERE, 'viewpoints.json')))

def main():
    os.makedirs(OUT, exist_ok=True)
    with sync_playwright() as p:
        b = p.chromium.launch(executable_path='/usr/bin/google-chrome', args=['--use-gl=swiftshader', '--enable-unsafe-swiftshader'])
        pg = b.new_page(viewport={'width': 1280, 'height': 720}); pg.set_default_timeout(600000)
        pg.goto('file://' + HTML + '?q=high&nofade')
        pg.wait_for_function('() => window.__HANA && window.__HANA.store', timeout=600000)
        pg.wait_for_timeout(1500)
        pg.evaluate("""() => { window.__SIMRUN = true; for (const e of document.body.children) if (e.id !== 'c' && e.tagName !== 'CANVAS' && e.tagName !== 'SCRIPT') e.style.visibility = 'hidden'; }""")
        pg.wait_for_timeout(300)
        for v in VP:
            pg.evaluate("""(v) => { const H = window.__HANA, c = H.camera; c.fov = 72; c.aspect = 1280/720; c.updateProjectionMatrix();
                c.position.set(...v.pos); c.lookAt(...v.target); c.updateMatrixWorld(true);
                for (let i = 0; i < 3; i++) H.composer.render(0.016); }""", v)
            pg.screenshot(path=os.path.join(OUT, v['name'] + '.png'))
            print('ref', v['name'])
        b.close()

if __name__ == '__main__': main()

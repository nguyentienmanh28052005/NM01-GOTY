"""Ghép ảnh Three.js (out/ref) và Unity (out/unity) cạnh nhau + bảng số liệu → out/compare/*.jpg, out/compare/report.md"""
import os, json
from PIL import Image, ImageStat, ImageDraw
HERE = os.path.dirname(os.path.abspath(__file__)); O = os.path.join(HERE, 'out')
VP = [v['name'] for v in json.load(open(os.path.join(HERE, 'viewpoints.json')))]
os.makedirs(os.path.join(O, 'compare'), exist_ok=True)
def stats(im):
    g = im.convert('L'); s = ImageStat.Stat(im); l = ImageStat.Stat(g).mean[0]
    px = list(im.resize((160, 90)).getdata()); dark = [p for p in px if sum(p) / 3 < 80] or [(0, 0, 0)]
    db = tuple(sum(p[i] for p in dark) / len(dark) for i in range(3))
    return l, s.mean, db
rows = ['| Góc | Độ sáng TB Three | Độ sáng TB Unity | Chênh lệch | Vùng tối Three (R,G,B) | Vùng tối Unity (R,G,B) |', '|---|---|---|---|---|---|']
for n in VP:
    a = Image.open(os.path.join(O, 'ref', n + '.png')).convert('RGB').resize((1280, 720))
    b = Image.open(os.path.join(O, 'unity', n + '.png')).convert('RGB').resize((1280, 720))
    sa, sb = stats(a), stats(b)
    diff = (sb[0] - sa[0]) / max(1, sa[0]) * 100
    rows.append(f'| {n} | {sa[0]:.0f} | {sb[0]:.0f} | {diff:+.0f}% | {tuple(round(x) for x in sa[2])} | {tuple(round(x) for x in sb[2])} |')
    c = Image.new('RGB', (1280, 360)); c.paste(a.resize((640, 360)), (0, 0)); c.paste(b.resize((640, 360)), (640, 0))
    d = ImageDraw.Draw(c); d.text((8, 8), 'Three.js: ' + n, fill='yellow'); d.text((648, 8), 'Unity: ' + n, fill='yellow')
    c.save(os.path.join(O, 'compare', n + '.jpg'), quality=82)
open(os.path.join(O, 'compare', 'report.md'), 'w', encoding='utf-8').write('# So sánh hình ảnh Three.js ↔ Unity\n\n' + '\n'.join(rows) + '\n')
print('\n'.join(rows))

"""Thu âm thanh tổng hợp WebAudio của HanaMart24 (source/src/audio.js) ra WAV (OfflineAudioContext, 44.1 kHz stereo 16-bit).
Chạy: python3 export_audio.py [thư mục source/src] → out/audio/*.wav"""
import base64, http.server, json, os, socketserver, sys, threading
from playwright.sync_api import sync_playwright
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, 'out', 'audio')
SRC = sys.argv[1] if len(sys.argv) > 1 else '/mnt/user-data/uploads/HanaMart24/source/src'
# (tên file, biểu thức gọi trên đối tượng a, thời lượng giây)
SFX = [
 ('footstep_walk', 'a.footstep(false)', 0.6), ('footstep_run', 'a.footstep(true)', 0.6), ('knock', 'a.knock()', 1.4),
 ('rattle', 'a.rattle()', 1.0), ('creak', 'a.creak()', 2.2), ('drip', 'a.drip()', 1.0), ('buzz', 'a.buzz()', 1.0),
 ('whisper', 'a.whisper()', 3.5), ('scare', 'a.scare()', 3.0), ('thud', 'a.thud()', 1.5), ('ui_move', "a.ui('move')", 0.3),
 ('ui_ok', "a.ui('ok')", 0.3), ('ui_back', "a.ui('back')", 0.3), ('ui_task', "a.ui('task')", 0.6), ('chime', 'a.chime()', 3.0),
 ('beep', 'a.beep()', 0.3), ('cash', 'a.cash()', 0.5), ('ring', 'a.ring()', 4.5), ('pickup', 'a.pickup()', 0.3),
 ('knock_323', 'a.knockPattern(null,[3,2,3])', 4.5), ('whisper_name', 'a.whisperName()', 3.5), ('sting', 'a.sting()', 2.5),
 ('meow', 'a.meow()', 1.6), ('door', 'a.door()', 0.8), ('shelf', 'a.shelf()', 1.0), ('mop', 'a.mop()', 1.2),
 ('microwave', 'a.microwave()', 6.5), ('ding', 'a.ding()', 1.6), ('paper', 'a.paper()', 0.6), ('gasp', 'a.gasp()', 1.0),
 ('power_down', 'a.powerDown()', 2.0), ('power_up', 'a.powerUp()', 1.8), ('glass_knock', 'a.glassKnock()', 1.3),
 ('radio_glitch', 'a.radioGlitch()', 1.7), ('printer', 'a.printer()', 1.8), ('tap', 'a.tap()', 0.3),
]
# lớp nền lặp: giữ một lớp, tắt các lớp khác
AMB = [('amb_rain_inside', {'rain': 0.1, 'rainLP': 1500}), ('amb_rain_outside', {'rain': 0.3, 'rainLP': 6000}), ('amb_hum', {'hum': 0.012}),
       ('amb_hvac', {'hvac': 0.18}), ('amb_breath', {'breath': 0.14}), ('amb_drone', {'drone': 0.12})]

PAGE = """<!doctype html><meta charset=utf-8><script type=module>
import { Audio } from './audio.js';
function setup(a, c) { a.ctx = c;
  a.master = c.createGain(); a.master.gain.value = 1; a.master.connect(c.destination);
  a.ambBus = c.createGain(); a.ambBus.connect(a.master); a.sfxBus = c.createGain(); a.sfxBus.connect(a.master);
  a.verb = c.createConvolver(); a.verb.buffer = a._impulse(1.8, 2.6); a.verbGain = c.createGain(); a.verbGain.gain.value = 0.42; a.verb.connect(a.verbGain); a.verbGain.connect(a.sfxBus);
  a.noiseBuf = a._noise(2); a._panner = () => a.sfxBus; }
function wav(buf) { const n = buf.length, ch = 2, sr = buf.sampleRate, b = new ArrayBuffer(44 + n * ch * 2), v = new DataView(b);
  const S = (o, s) => { for (let i = 0; i < s.length; i++) v.setUint8(o + i, s.charCodeAt(i)); };
  S(0, 'RIFF'); v.setUint32(4, 36 + n * ch * 2, true); S(8, 'WAVE'); S(12, 'fmt '); v.setUint32(16, 16, true); v.setUint16(20, 1, true); v.setUint16(22, ch, true);
  v.setUint32(24, sr, true); v.setUint32(28, sr * ch * 2, true); v.setUint16(32, ch * 2, true); v.setUint16(34, 16, true); S(36, 'data'); v.setUint32(40, n * ch * 2, true);
  const L = buf.getChannelData(0), R = buf.numberOfChannels > 1 ? buf.getChannelData(1) : L; let o = 44, peak = 0;
  for (let i = 0; i < n; i++) for (const d of [L, R]) { const x = Math.max(-1, Math.min(1, d[i])); peak = Math.max(peak, Math.abs(x)); v.setInt16(o, x * 32767, true); o += 2; }
  let s = ''; const u = new Uint8Array(b); for (let k = 0; k < u.length; k += 0x8000) s += String.fromCharCode.apply(null, u.subarray(k, k + 0x8000)); return { b64: btoa(s), peak }; }
window.renderSfx = async (expr, dur) => { const c = new OfflineAudioContext(2, Math.ceil(44100 * dur), 44100); const a = new Audio(); setup(a, c); new Function('a', expr)(a); return wav(await c.startRendering()); };
window.renderAmb = async (keep, dur) => { const c = new OfflineAudioContext(2, Math.ceil(44100 * dur), 44100); const a = new Audio(); setup(a, c); a._ambience();
  for (const k of ['rain', 'hum', 'hvac', 'breath', 'drone']) a[k].gain.value = keep[k] ?? 0; if (keep.rainLP) a.rainLP.frequency.value = keep.rainLP;
  return wav(await c.startRendering()); };
window.ready = true;
</script>"""

def main():
    os.makedirs(OUT, exist_ok=True)
    open(os.path.join(SRC, '_export_audio.html'), 'w').write(PAGE)
    handler = lambda *a, **k: http.server.SimpleHTTPRequestHandler(*a, directory=SRC, **k)
    httpd = socketserver.TCPServer(('127.0.0.1', 0), handler); port = httpd.server_address[1]
    threading.Thread(target=httpd.serve_forever, daemon=True).start()
    report = {}
    try:
        with sync_playwright() as p:
            b = p.chromium.launch(executable_path='/usr/bin/google-chrome', args=['--autoplay-policy=no-user-gesture-required'])
            pg = b.new_page(); pg.set_default_timeout(120000)
            errs = []; pg.on('pageerror', lambda e: errs.append(str(e)))
            pg.goto(f'http://127.0.0.1:{port}/_export_audio.html'); pg.wait_for_function('() => window.ready')
            for name, expr, dur in SFX:
                r = pg.evaluate('([e, d]) => window.renderSfx(e, d)', [expr, dur])
                open(os.path.join(OUT, name + '.wav'), 'wb').write(base64.b64decode(r['b64'])); report[name] = round(r['peak'], 3)
            for name, keep in AMB:
                r = pg.evaluate('([k, d]) => window.renderAmb(k, d)', [keep, 8.0])
                open(os.path.join(OUT, name + '.wav'), 'wb').write(base64.b64decode(r['b64'])); report[name] = round(r['peak'], 3)
            b.close()
    finally:
        httpd.shutdown(); os.remove(os.path.join(SRC, '_export_audio.html'))
    json.dump(report, open(os.path.join(OUT, 'peaks.json'), 'w'), indent=1)
    print(len(report), 'files; errors:', errs[:3]); print(report)

if __name__ == '__main__': main()

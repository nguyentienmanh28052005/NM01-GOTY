import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { spawn } from 'node:child_process';
import { fileURLToPath } from 'node:url';

const HERE = path.dirname(fileURLToPath(import.meta.url));
const OUT_DIR = path.join(HERE, 'out', 'ref');
const VIEWPOINTS = JSON.parse(fs.readFileSync(path.join(HERE, 'viewpoints.json'), 'utf8'));
const HTML_PATH = process.argv[2] || 'E:/Downloads/top/top/HanaMart24/HanaMart24.html';
const CHROME = 'C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe';
const PORT = 9229;

fs.mkdirSync(OUT_DIR, { recursive: true });
const userDataDir = fs.mkdtempSync(path.join(os.tmpdir(), 'hanamart-cdp-'));
const url = 'file:///' + HTML_PATH.replace(/\\/g, '/') + '?q=high&nofade';

const chrome = spawn(CHROME, [
  '--headless=new',
  '--disable-gpu-sandbox',
  '--use-gl=angle',
  '--use-angle=swiftshader',
  '--enable-webgl',
  '--ignore-gpu-blocklist',
  `--remote-debugging-port=${PORT}`,
  `--user-data-dir=${userDataDir}`,
  '--window-size=1280,720',
  url,
], { stdio: 'ignore' });

async function sleep(ms) { return new Promise(r => setTimeout(r, ms)); }

async function getTargetWs() {
  for (let i = 0; i < 60; i++) {
    try {
      const res = await fetch(`http://127.0.0.1:${PORT}/json/list`);
      const list = await res.json();
      const page = list.find(t => t.type === 'page' && t.webSocketDebuggerUrl);
      if (page) return page.webSocketDebuggerUrl;
    } catch {}
    await sleep(250);
  }
  throw new Error('Chrome CDP endpoint did not start');
}

function createCdp(wsUrl) {
  return new Promise((resolve, reject) => {
    const ws = new WebSocket(wsUrl);
    let nextId = 1;
    const pending = new Map();
    ws.addEventListener('open', () => resolve({
      send(method, params = {}) {
        const id = nextId++;
        return new Promise((res, rej) => {
          pending.set(id, { res, rej });
          ws.send(JSON.stringify({ id, method, params }));
        });
      },
      close() { ws.close(); },
    }));
    ws.addEventListener('message', ev => {
      const msg = JSON.parse(ev.data);
      if (msg.id && pending.has(msg.id)) {
        const { res, rej } = pending.get(msg.id);
        pending.delete(msg.id);
        if (msg.error) rej(new Error(msg.error.message));
        else res(msg.result);
      }
    });
    ws.addEventListener('error', reject);
  });
}

async function evalJs(cdp, expression) {
  const res = await cdp.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
  if (res.exceptionDetails) throw new Error(res.exceptionDetails.text || 'JS error');
  return res.result?.value;
}

try {
  const wsUrl = await getTargetWs();
  const cdp = await createCdp(wsUrl);
  await cdp.send('Runtime.enable');
  await cdp.send('Emulation.setDeviceMetricsOverride', {
    width: 1280, height: 720, deviceScaleFactor: 1, mobile: false,
  });

  for (let i = 0; i < 120; i++) {
    const ready = await evalJs(cdp, 'Boolean(window.__HANA && window.__HANA.store)');
    if (ready) break;
    await sleep(500);
    if (i === 119) throw new Error('Timed out waiting for window.__HANA.store');
  }

  await sleep(1200);
  await evalJs(cdp, `(() => {
    window.__SIMRUN = true;
    for (const e of document.body.children) {
      if (e.id !== 'c' && e.tagName !== 'CANVAS' && e.tagName !== 'SCRIPT') e.style.visibility = 'hidden';
    }
    return true;
  })()`);
  await sleep(300);

  for (const v of VIEWPOINTS) {
    await evalJs(cdp, `(() => {
      const H = window.__HANA, c = H.camera;
      c.fov = 72; c.aspect = 1280 / 720; c.updateProjectionMatrix();
      c.position.set(${v.pos[0]}, ${v.pos[1]}, ${v.pos[2]});
      c.lookAt(${v.target[0]}, ${v.target[1]}, ${v.target[2]});
      c.updateMatrixWorld(true);
      if (H.game && H.game.fx) H.game.fx.update(0.016, c, { rain: true, power: true, bufH: 720 });
      for (let i = 0; i < 3; i++) H.composer.render(0.016);
      return true;
    })()`);
    const shot = await cdp.send('Page.captureScreenshot', { format: 'png' });
    fs.writeFileSync(path.join(OUT_DIR, v.name + '.png'), Buffer.from(shot.data, 'base64'));
    console.log('ref', v.name);
  }
  cdp.close();
} finally {
  try { chrome.kill(); } catch {}
  try { fs.rmSync(userDataDir, { recursive: true, force: true }); } catch {}
}

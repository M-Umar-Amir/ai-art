// ================= v2 kit: real photos, document zooms with highlighter sweeps, NASA clips, kinetic type, code, three.js =================
// ---------- assets ----------
const IMG = {};
function assetsReady() {
  const ps = Object.entries(IMGSRC).map(([k, src]) => { const im = new Image(); im.src = src; IMG[k] = im; return im.decode().catch(() => {}); });
  const pt = new Promise(res => { if (window.THREE) res(); else window.__threeReady = res; });
  return Promise.all([...ps, pt]);
}
const CLIPUSE = [], CLIPCACHE = {};
function clipIndex(key, t, t0, u) {
  const n = CLIPSRC[key].length, s = SHOTS.find(x => x.name === u.shot), dur = s ? Math.max(0.5, s.t1 - s.t0) : 4;
  const speed = u.speed ?? Math.min(1, (n / 12) / dur);              // slow a short clip down to fill a long shot
  return Math.max(0, Math.min(n - 1, Math.floor(Math.max(0, t - t0) * 12 * speed)));
}
async function clipLoad(key, i) { const c = CLIPCACHE[key] || (CLIPCACHE[key] = {}); if (c[i]) return; const im = new Image(); im.src = CLIPSRC[key][i]; await im.decode().catch(() => {}); c[i] = im; }
function clipEvict() { for (const key in CLIPCACHE) { const ks = Object.keys(CLIPCACHE[key]).map(Number).sort((a, b) => a - b); if (ks.length > 24) for (const k of ks.slice(0, ks.length - 24)) delete CLIPCACHE[key][k]; } }
function clipFrame(key, i) {
  const c = CLIPCACHE[key]; if (!c) return null; if (c[i]) return c[i];
  let best = null, bd = 1e9; for (const k in c) { const d = Math.abs(k - i); if (d < bd) { bd = d; best = c[k]; } } return best;
}
// draw an image to cover a box (object-fit: cover) with a Ken Burns view: (cx, cy) in 0..1 of the image, z >= 1
function coverDraw(g, im, x, y, w, h, cx = 0.5, cy = 0.5, z = 1) {
  if (!im || !im.width) return; const s = Math.max(w / im.width, h / im.height) * z, sw = w / s, sh = h / s;
  const sx = clamp(cx * im.width - sw / 2, 0, im.width - sw), sy = clamp(cy * im.height - sh / 2, 0, im.height - sh);
  g.drawImage(im, sx, sy, sw, sh, x, y, w, h);
}
function credit(g, text, o = {}) {
  g.save(); g.font = `500 ${o.size ?? 20}px Georgia, serif`; g.textAlign = o.align || 'right'; g.textBaseline = 'bottom';
  g.fillStyle = 'rgba(0,0,0,0.45)'; const w = g.measureText(text).width; const x = o.x ?? 1890, y = o.y ?? 1066;
  g.fillRect((o.align === 'left' ? x : x - w) - 8, y - (o.size ?? 20) - 6, w + 16, (o.size ?? 20) + 10);
  g.fillStyle = 'rgba(255,255,255,0.85)'; g.fillText(text, x, y - 2); g.restore();
}
const CREDIT = {
  godel: 'Kurt Gödel, 1925 · public domain', vonneumann: 'John von Neumann · LANL', valiant: 'Leslie Valiant · Dennis Hamilton, CC BY 2.0',
  cook: 'Stephen Cook · Jiří Janíček, CC BY-SA 3.0', karp: 'Richard Karp · Rama, CC BY-SA 2.0 FR', levin: 'Leonid Levin · Sergio01, CC BY-SA 3.0',
  pauli: 'Wolfgang Pauli · Nobel Foundation, public domain', bose: 'Satyendra Nath Bose, 1925 · public domain', fermi: 'Enrico Fermi · US DOE, public domain',
  dirac: 'Paul Dirac, 1933 · public domain', strassen: 'Volker Strassen · David Eppstein, CC BY-SA 3.0', cantor: 'Georg Cantor · public domain',
  turing: 'Alan Turing · Elliott & Fry, public domain', aaronson: 'Scott Aaronson · public domain', razborov: 'Alexander Razborov · Petra Lein, CC BY-SA 2.0 DE',
  eniac: 'ENIAC · CC BY 4.0', sudoku: 'Sudoku · Tim Stellmach, CC0', tsp: 'Travelling salesman · public domain', laser_sky: 'Starfire Optical Range · US Air Force, public domain',
  green_laser: 'Green laser · CC BY-SA 4.0',
};
// ---------- photos ----------
// full-frame photo with a slow Ken Burns move; o.from/o.to = [cx, cy, z]; o.bw for black and white
function photoFull(g, key, t, t0, t1, o = {}) {
  const u = E.io2(seg(t, t0, t1)), a = o.from || [0.5, 0.45, 1.0], b = o.to || [0.5, 0.4, 1.12];
  g.save(); g.fillStyle = '#0b0b10'; g.fillRect(0, 0, W, H); if (o.bw) g.filter = 'grayscale(1) contrast(1.12) brightness(1.02)';
  if (o.fit) { const im = IMG[key], s = Math.min(W / im.width, H / im.height) * lerp(a[2], b[2], u); g.drawImage(im, (W - im.width * s) / 2, (H - im.height * s) / 2, im.width * s, im.height * s); }
  else coverDraw(g, IMG[key], 0, 0, W, H, lerp(a[0], b[0], u), lerp(a[1], b[1], u), lerp(a[2], b[2], u));
  g.restore();
  const v = g.createLinearGradient(0, H * 0.55, 0, H); v.addColorStop(0, 'rgba(0,0,0,0)'); v.addColorStop(1, 'rgba(0,0,0,0.55)'); g.fillStyle = v; g.fillRect(0, 0, W, H);
  if (o.credit !== false && CREDIT[key]) credit(g, CREDIT[key]);
}
// a photo print (white border, shadow) that pops in; h = image height on screen
function print(g, key, x, y, h, t, tIn, o = {}) {
  const tt = on2(t); if (tt < tIn || !IMG[key]) return; const im = IMG[key], w = h * im.width / im.height;
  const k = E.out3(seg(tt, tIn, tIn + 0.35)), sc = lerp(0.8, 1, k) * (o.out != null ? 1 - 0.2 * seg(tt, o.out, o.out + 0.3) : 1);
  const a = o.out != null ? 1 - seg(tt, o.out, o.out + 0.3) : 1; if (a <= 0) return;
  g.save(); g.globalAlpha = a * Math.min(1, k * 1.5); g.translate(x, y); g.rotate(o.rot ?? (hash(Math.floor(x), 7) - 0.5) * 0.08); g.scale(sc, sc);
  const bw = o.border ?? 14; g.fillStyle = 'rgba(0,0,0,0.35)'; g.fillRect(-w / 2 - bw + 12, -h / 2 - bw + 16, w + bw * 2, h + bw * 2 + (o.caption ? 54 : 0));
  g.fillStyle = '#f6f1e6'; g.fillRect(-w / 2 - bw, -h / 2 - bw, w + bw * 2, h + bw * 2 + (o.caption ? 54 : 0));
  if (o.bw) g.filter = 'grayscale(1) contrast(1.1)'; g.drawImage(im, -w / 2, -h / 2, w, h); g.filter = 'none';
  if (o.caption) { g.font = `700 ${o.cs ?? 32}px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(o.caption, 0, h / 2 + 30); }
  g.restore();
  if (o.credit !== false && CREDIT[key]) { g.save(); g.globalAlpha = a; g.font = '500 17px Georgia, serif'; g.fillStyle = o.creditCol || 'rgba(40,30,20,0.75)'; g.textAlign = 'center'; g.fillText(CREDIT[key], x, y + h / 2 + (o.caption ? 92 : 40)); g.restore(); }
}
// ---------- documents: zoom + highlighter ----------
// o.path: [[time, target], ...] target = 'all' | phrase | [x, y, w, h]  (image pixels); o.hl: [[phrase, tIn, color]]
function docRect(key, tg) { const d = DOCS[key] || {}; if (!tg || tg === 'all') return [0, 0, d.w || IMG['doc_' + key].width, Math.min(d.h || IMG['doc_' + key].height, (d.w || IMG['doc_' + key].width) * 0.62)]; if (typeof tg === 'string') { const r = d.hl && d.hl[tg]; if (!r) { console.warn('doc phrase miss', key, tg); return docRect(key, 'all'); } const dw = d.w || 1920, ww = Math.min(dw, Math.max(r[2] * 1.6, dw * 0.6)), hh = ww * 0.52; return [clamp(r[0] + r[2] / 2 - ww / 2, 0, dw - ww), Math.max(0, r[1] + r[3] / 2 - hh / 2), ww, hh]; } return tg; }
function docView(g, key, t, o = {}) {
  const im = IMG['doc_' + key]; if (!im) return; const P = o.path || [[0, 'all']];
  let R = docRect(key, P[0][1]);
  for (let i = 1; i < P.length; i++) if (t > P[i][0]) { const u = E.io3(seg(t, P[i][0], P[i][0] + 0.9)); const A = docRect(key, P[i - 1][1]), Bq = docRect(key, P[i][1]); R = A.map((v, k) => lerp(v, Bq[k], u)); }
  const bx = o.box || [70, 50, 1780, 980], ar = R[2] / R[3], s = Math.min(bx[2] / R[2], bx[3] / R[3]), ox = bx[0] + (bx[2] - R[2] * s) / 2 - R[0] * s, oy = bx[1] + (bx[3] - R[3] * s) / 2 - R[1] * s;
  g.save(); g.fillStyle = o.desk || '#1a1c22'; g.fillRect(0, 0, W, H);
  g.translate(ox, oy); g.scale(s, s);
  g.fillStyle = 'rgba(0,0,0,0.5)'; g.fillRect(16 / s, 22 / s, im.width, im.height); g.drawImage(im, 0, 0);
  for (const [ph, tIn, col] of (o.hl || [])) {
    if (t < tIn) continue; const r = (DOCS[key] && DOCS[key].hl && DOCS[key].hl[ph]); if (!r) { console.warn('hl miss', key, ph); continue; }
    const u = E.out2(seg(t, tIn, tIn + 0.6)); g.globalCompositeOperation = 'multiply'; g.fillStyle = col || 'rgba(255,214,64,0.75)';
    g.fillRect(r[0] - 6, r[1] - 4, (r[2] + 12) * u, r[3] + 8); g.globalCompositeOperation = 'source-over';
  }
  g.restore();
  if (o.src) credit(g, o.src, { size: 19 });
}
// ---------- clips (NASA public-domain footage, 12 fps frame sequences decoded on demand) ----------
function clipShot(name, t0, key, o = {}) {
  CLIPUSE.push({ key, shot: name, speed: o.speed });
  scene(name, t0, () => ({}), (g, l, t, S) => {
    const u = CLIPUSE.find(c => c.shot === name), im = clipFrame(key, clipIndex(key, t, S._sh.t0, u));
    g.fillStyle = '#000'; g.fillRect(0, 0, W, H);
    if (im) { const z = lerp(1.0, o.z ?? 1.08, seg(t, S._sh.t0, S._sh.t1)); coverDraw(g, im, 0, 0, W, H, o.cx ?? 0.5, o.cy ?? 0.5, z); }
    if (o.tint) { g.fillStyle = o.tint; g.fillRect(0, 0, W, H); }
    const v = g.createRadialGradient(W / 2, H / 2, H * 0.3, W / 2, H / 2, H * 0.95); v.addColorStop(0, 'rgba(0,0,0,0)'); v.addColorStop(1, 'rgba(0,0,0,0.5)'); g.fillStyle = v; g.fillRect(0, 0, W, H);
    if (o.draw) o.draw(g, t, S);
    credit(g, 'Footage: NASA (public domain)');
  }, { in: o.in });
}
// ---------- kinetic type ----------
// lines: [[text, color?], ...] centred; pops in at tIn (each line staggered by o.stagger); o.bg fills the frame
function kin(g, lines, t, tIn, o = {}) {
  const tt = on2(t); if (tt < tIn) return; const fs = o.size ?? 150, lh = fs * 1.05, n = lines.length, y0 = (o.y ?? 540) - (n - 1) * lh / 2;
  g.save(); g.textAlign = 'center'; g.textBaseline = 'middle'; g.font = `${fs}px Impact, "Arial Black", sans-serif`;
  lines.forEach((L, i) => {
    const [txt, col] = Array.isArray(L) ? L : [L, null], ti = tIn + i * (o.stagger ?? 0.18); if (tt < ti) return;
    const k = seg(tt, ti, ti + 0.22), sc = kf(tt, [[ti, 1.35], [ti + 0.12, 0.96, E.out3], [ti + 0.22, 1, E.io2]]);
    g.save(); g.translate(o.x ?? 960, y0 + i * lh); g.scale(sc, sc); g.globalAlpha = Math.min(1, k * 2);
    g.lineWidth = fs * 0.09; g.strokeStyle = o.stroke || 'rgba(0,0,0,0.9)'; g.lineJoin = 'round'; g.strokeText(txt, 0, 0);
    g.fillStyle = col || o.col || '#ffffff'; g.fillText(txt, 0, 0); g.restore();
  });
  g.restore();
}
function kinShot(name, t0, lines, o = {}) {
  scene(name, t0, () => ({}), (g, l, t, S) => {
    g.fillStyle = o.bg || '#0d0d12'; g.fillRect(0, 0, W, H);
    if (o.grid) { g.strokeStyle = 'rgba(255,255,255,0.05)'; g.lineWidth = 2; for (let x = 0; x < W; x += 80) { g.beginPath(); g.moveTo(x, 0); g.lineTo(x, H); g.stroke(); } for (let y = 0; y < H; y += 80) { g.beginPath(); g.moveTo(0, y); g.lineTo(W, y); g.stroke(); } }
    const z = 1 + 0.04 * seg(t, S._sh.t0, S._sh.t1); g.save(); g.translate(960, 540); g.scale(z, z); g.translate(-960, -540);
    kin(g, lines, t, S._sh.t0 + (o.delay ?? 0.05), o); if (o.draw) o.draw(g, t, S); g.restore();
  }, { in: o.in });
}
// ---------- Lean code (the real Statement.lean, 98 lines) ----------
const LEANKW = /\b(import|open|universe|namespace|end|def|theorem|lemma|structure|where|fun|let|in|∀|∃|Prop|Type|variable|noncomputable|instance)\b/g;
function codeView(g, x, y, w, h, t, o = {}) {
  const L = STATEMENT.split('\n'), lh = o.lh ?? 34, top = o.top != null ? (typeof o.top === 'function' ? o.top(t) : o.top) : 0;
  g.save(); g.beginPath(); g.rect(x, y, w, h); g.clip(); g.fillStyle = '#14181f'; g.fillRect(x, y, w, h);
  g.font = `500 ${Math.round(lh * 0.62)}px Consolas, "Courier New", monospace`; g.textBaseline = 'middle';
  const first = Math.max(0, Math.floor(top)), off = (top - first) * lh;
  for (let i = first; i < Math.min(L.length, first + Math.ceil(h / lh) + 1); i++) {
    const yy = y + (i - first) * lh - off + lh / 2;
    for (const [a, b, tIn, col] of (o.hl || [])) if (i + 1 >= a && i + 1 <= b && t > tIn) { g.fillStyle = col || `rgba(255,200,64,${0.22 * seg(t, tIn, tIn + 0.4)})`; g.fillRect(x, yy - lh / 2, w, lh); }
    g.fillStyle = 'rgba(160,170,190,0.45)'; g.textAlign = 'right'; g.fillText(String(i + 1), x + 60, yy); g.textAlign = 'left';
    const s = L[i]; const isC = /^\s*(--|\/-|-\/)/.test(s) || /^\s*[A-Z][^:=]*[a-z]\.\s*$/.test(s);
    g.fillStyle = isC ? '#7f9a7a' : '#d7dee8'; g.fillText(s.slice(0, 120), x + 80, yy);
    if (!isC) { g.fillStyle = '#f0c060'; let m; LEANKW.lastIndex = 0; while ((m = LEANKW.exec(s))) { const pre = g.measureText(s.slice(0, m.index)).width; g.fillText(m[0], x + 80 + pre, yy); } }
  }
  g.restore();
}
// ---------- three.js (one shared WebGL canvas; scenes are deterministic functions of time) ----------
let GL = null;
function glInit() {
  if (GL) return GL; const c = document.createElement('canvas'); c.width = W * SC; c.height = H * SC;
  const r = new THREE.WebGLRenderer({ canvas: c, antialias: true, preserveDrawingBuffer: true }); r.setPixelRatio(1); r.setSize(W * SC, H * SC, false);
  r.outputColorSpace = THREE.SRGBColorSpace; r.toneMapping = THREE.ACESFilmicToneMapping; r.toneMappingExposure = 1.1;
  GL = { c, r }; return GL;
}
function glLabel(text, o = {}) {
  const fs = o.size ?? 96, c = document.createElement('canvas'), g = c.getContext('2d'); g.font = `700 ${fs}px ${o.font || 'Georgia, serif'}`;
  const w = Math.ceil(g.measureText(text).width + fs * 0.6), h = Math.ceil(fs * 1.4); c.width = w; c.height = h;
  g.font = `700 ${fs}px ${o.font || 'Georgia, serif'}`; g.textAlign = 'center'; g.textBaseline = 'middle';
  if (o.bg) { g.fillStyle = o.bg; g.fillRect(0, 0, w, h); }
  g.lineWidth = fs * 0.12; g.strokeStyle = 'rgba(0,0,0,0.8)'; g.strokeText(text, w / 2, h / 2); g.fillStyle = o.col || '#fff'; g.fillText(text, w / 2, h / 2);
  const tex = new THREE.CanvasTexture(c); tex.colorSpace = THREE.SRGBColorSpace;
  const sp = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, transparent: true, depthWrite: false })); const sc = (o.scale ?? 1) * 0.01;
  sp.scale.set(w * sc, h * sc, 1); return sp;
}
// glShot: build(THREE) -> { scene, camera, update(lt, t) }; o.over(g, t, S) draws 2D on top
function glShot(name, t0, build, o = {}) {
  scene(name, t0, () => { glInit(); return build(THREE); }, (g, l, t, S) => {
    S.update(t - S._sh.t0, t, S); GL.r.render(S.scene, S.camera);
    g.drawImage(GL.c, 0, 0, W, H); if (o.over) o.over(g, t, S);
  }, { in: o.in });
}
// ---------- sentence-timed "punch" framing for board shots (hard cuts inside a shot) ----------
function secAt(t) { let i = 0; for (let k = 0; k < B.sec.length; k++) if (t >= B.sec[k] - 0.01) i = k; return i; }
function sentCount(t) { const i = secAt(t), ss = (B.sent && B.sent[i]) || []; let k = 0; for (const s of ss) if (B.sec[i] + s <= t + 1e-3) k++; return i * 1000 + k; }
const PUNCH = [[1.0, 960, 540], [1.1, 920, 525], [1.05, 1000, 560], [1.13, 960, 510]];
function punchCam(t, sh, o = {}) {
  if (o.punch === false) return [1, 960, 540];
  const j = Math.max(0, sentCount(t) - sentCount(sh.t0 + 0.02)), P = o.frames || PUNCH, f = P[j % P.length];
  return [f[0] * (1 + 0.012 * seg(t, sh.t0, sh.t1)), f[1], f[2]];
}

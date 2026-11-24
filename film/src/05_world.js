// ================= "The Proof Nobody Can Read" — shared world: palette, timing, painted plates, ink diagram kit, physics sims =================
// Timing: B.sec[i] = start of VO section i on the film timeline, B.secDur[i] its length, B.words[i] = [[word, t], ...]
// (exact edge-tts word boundaries, seconds from the section start). SW(i,'word') = absolute time that word is spoken.
const _norm = w => w.toLowerCase().replace(/[^a-z0-9']/g, '').replace(/'s$/, "'s");
function SW(i, word, n = 1, frac = 0.5) {
  const ws = (B.words && B.words[i]) || []; let c = 0; word = _norm(word);
  for (const [w, t] of ws) if (_norm(w) === word && ++c === n) return B.sec[i] + t;
  console.warn(`SW miss: section ${i} word "${word}" #${n}`);
  return B.sec[i] + B.secDur[i] * frac;
}
const SE = i => i + 1 < B.sec.length ? B.sec[i + 1] : B.duration;   // end of section i on the timeline
const SB = i => B.sec[i];

const PAL = {
  ink: '#22160f', inkCool: '#17202c', red: '#b8361f', blue: '#2f5f8f', teal: '#2f7f7a', gold: '#d79a2b',
  card: '#f3e8d2', cardSh: '#d9c7a4',
  paper: ['#efe4cc', '#e6d8bb', '#f4ead6'], paperBlue: ['#dfe3dc', '#d2d9d6', '#e8ebe2'], paperSage: ['#e3e2c9', '#d6d8b9', '#ecebd6'],
  deep: [[0, '#0b1426'], [540, '#12233d'], [1080, '#0a1120']],
  tea: '#9a5a22', teaDk: '#6a3612', teaLt: '#c88a44', cream: '#f5ead8',
  wood: ['#6b4428', '#7a4f2e', '#5b3820', '#83583a'],
  flow: '#3f7fb6', flowLt: '#8fc4e8', hot: '#fff1cf', ember: '#ff9a4a',
};
const LIGHT = { lx: -0.6, ly: -0.75 };

// ---------- text, cards, stamps ----------
function tag(g, x, y, text, t, tIn, tOut = 1e9, o = {}) {
  const tt = on2(t); if (tt < tIn || tt > tOut + 0.4) return;
  const a = Math.min(seg(tt, tIn, tIn + 0.25), 1 - seg(tt, tOut, tOut + 0.4));
  const sc = kf(tt, [[tIn, 0.6], [tIn + 0.17, 1.08, E.out3], [tIn + 0.33, 1, E.io2]]);
  const fs = o.size ?? 40, font = `${o.italic ? 'italic ' : ''}${o.weight ?? 600} ${fs}px Georgia, "Times New Roman", serif`;
  g.save(); g.globalAlpha = a; g.translate(x, y); g.rotate(o.rot ?? (hash(Math.floor(x + y), 3) - 0.5) * 0.05); g.scale(sc, sc);
  g.font = font; const lines = String(text).split('\n'), wmax = Math.max(...lines.map(l => g.measureText(l).width));
  const pw = wmax + fs * 0.9, ph = lines.length * fs * 1.2 + fs * 0.5, sd = Math.floor(x * 3 + y);
  if (o.card !== false) {
    const box = rough([[-pw / 2, -ph / 2], [pw / 2, -ph / 2], [pw / 2, ph / 2], [-pw / 2, ph / 2]], 2.2, sd, 10);
    fillPts(g, offsetPts(box, 5, 6), css('#000000', 0.18));
    cels(g, box, { base: o.bg || PAL.card, sh: PAL.cardSh, sw: 4, L: LIGHT, line: PAL.ink, lw: 2.2, seed: sd });
  }
  g.fillStyle = css(o.col || PAL.ink); g.textAlign = 'center'; g.textBaseline = 'middle';
  lines.forEach((l, i) => g.fillText(l, 0, (i - (lines.length - 1) / 2) * fs * 1.2 + fs * 0.04));
  if (o.strike != null && tt > o.strike) {
    const u = E.out3(seg(tt, o.strike, o.strike + 0.35));
    ink(g, [[-pw * 0.48, 4], [lerp(-pw * 0.48, pw * 0.48, u), -4]], fs * 0.22, { col: PAL.red, t0: 0.05, t1: 0.3, seed: sd });
  }
  g.restore();
}
function title(g, x, y, text, t, tIn, tOut, size = 64, col = '#fff6e6', o = {}) {
  const tt = on2(t); if (tt < tIn || tt > tOut + 0.6) return;
  const a = Math.min(seg(tt, tIn, tIn + 0.6), 1 - seg(tt, tOut, tOut + 0.6));
  g.save(); g.globalAlpha = a; g.font = `${o.upright ? '' : 'italic '}${o.weight ?? 500} ${size}px Georgia, "Times New Roman", serif`; g.textAlign = o.align || 'center'; g.textBaseline = 'middle';
  if (o.shadow !== false) { g.shadowColor = 'rgba(0,0,0,0.45)'; g.shadowBlur = 12; }
  g.fillStyle = col; String(text).split('\n').forEach((l, i, L) => g.fillText(l, x, y + (i - (L.length - 1) / 2) * size * 1.2)); g.restore();
}
function rollNum(t, t0, t1, v0, v1, fmt = v => Math.round(v).toLocaleString('en-US')) { return fmt(lerp(v0, v1, E.out3(seg(on2(t), t0, t1)))); }
// rubber stamp: pops in big and slams down, slightly rotated, rough inked border
function stamp(g, x, y, text, t, tIn, tOut = 1e9, o = {}) {
  const tt = on2(t); if (tt < tIn || tt > tOut + 0.4) return;
  const a = Math.min(seg(tt, tIn, tIn + 0.08), 1 - seg(tt, tOut, tOut + 0.4));
  const sc = kf(tt, [[tIn, 1.9], [tIn + 0.17, 0.96, E.in2], [tIn + 0.3, 1, E.out2]]);
  const fs = o.size ?? 64, col = o.col || PAL.red;
  g.save(); g.globalAlpha = a * 0.92; g.translate(x, y); g.rotate(o.rot ?? -0.08); g.scale(sc, sc);
  g.font = `800 ${fs}px Georgia, serif`; const w = g.measureText(text).width + fs * 0.8, h = fs * 1.35;
  const box = rough([[-w / 2, -h / 2], [w / 2, -h / 2], [w / 2, h / 2], [-w / 2, h / 2]], 3, Math.floor(x + y), 10);
  inkLoop(g, box, fs * 0.12, { col, seed: 5, jit: 0.4 });
  g.fillStyle = col; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(text, 0, fs * 0.05);
  // dry-ink speckle knocked out of the letters (fixed, never flickers)
  g.globalCompositeOperation = 'destination-out'; const r = RNG(Math.floor(x * 7 + y));
  for (let i = 0; i < 60; i++) { g.globalAlpha = 0.5 * r(); g.beginPath(); g.arc((r() - 0.5) * w, (r() - 0.5) * h, 1 + r() * 3, 0, TAU); g.fill(); }
  g.restore();
}
// a big quote card centred on screen, text wrapped, with an attribution line
function quoteCard(g, text, who, t, tIn, tOut, o = {}) {
  const tt = on2(t); if (tt < tIn || tt > tOut + 0.5) return;
  const a = Math.min(seg(tt, tIn, tIn + 0.35), 1 - seg(tt, tOut, tOut + 0.5));
  const sc = kf(tt, [[tIn, 0.9], [tIn + 0.3, 1.0, E.out3]]);
  const fs = o.size ?? 52, maxW = o.w ?? 1250, cx = o.x ?? 960, cy = o.y ?? 520;
  g.save(); g.globalAlpha = a; g.translate(cx, cy); g.scale(sc, sc); g.rotate(o.rot ?? -0.012);
  g.font = `italic 500 ${fs}px Georgia, serif`;
  const words = String(text).split(' '), lines = []; let cur = '';
  for (const w of words) { const s = cur ? cur + ' ' + w : w; if (g.measureText(s).width > maxW && cur) { lines.push(cur); cur = w; } else cur = s; }
  lines.push(cur);
  const lh = fs * 1.3, ph = lines.length * lh + fs * 2.6, pw = maxW + fs * 1.6;
  const box = rough([[-pw / 2, -ph / 2], [pw / 2, -ph / 2], [pw / 2, ph / 2], [-pw / 2, ph / 2]], 3, 77, 12);
  fillPts(g, offsetPts(box, 8, 10), css('#000', 0.25));
  cels(g, box, { base: o.bg || '#f6eedc', sh: PAL.cardSh, sw: 6, L: LIGHT, line: PAL.ink, lw: 2.6, seed: 31 });
  g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.textBaseline = 'middle';
  ink(g, [[-pw / 2 + 60, -ph / 2 + 70], [-pw / 2 + 60, ph / 2 - 70]], 8, { col: o.accent || PAL.red, t0: 0.2, t1: 0.2, seed: 4 });
  lines.forEach((l, i) => g.fillText(l, 0, -ph / 2 + fs * 1.1 + i * lh + lh / 2));
  g.font = `600 ${fs * 0.55}px Georgia, serif`; g.fillStyle = css('#5a4a3a'); g.fillText('— ' + who, 0, ph / 2 - fs * 0.85);
  g.restore();
}
// ink handwriting reveal: text appears left to right as if written (clip sweep), with a soft pen-tip glow
function writeText(g, text, x, y, size, t, tIn, dur = 1.0, o = {}) {
  const tt = t; if (tt < tIn) return 0;
  const u = E.out2(seg(tt, tIn, tIn + dur)), a = o.tOut != null ? 1 - seg(tt, o.tOut, o.tOut + 0.5) : 1; if (a <= 0) return 0;
  g.save(); g.globalAlpha = a; g.font = `${o.upright ? '' : 'italic '}${o.weight ?? 500} ${size}px Georgia, "Cambria Math", serif`;
  g.textAlign = 'left'; g.textBaseline = 'middle'; const w = g.measureText(text).width, x0 = o.align === 'center' ? x - w / 2 : x;
  g.beginPath(); g.rect(x0 - 10, y - size, (w + 20) * u, size * 2); g.clip();
  g.fillStyle = css(o.col || PAL.ink); g.fillText(text, x0, y); g.restore();
  return w;
}
// underline / circle highlight painted with a brush
function brushUnder(g, x0, x1, y, t, tIn, col = PAL.red, w = 10) {
  if (t < tIn) return; const u = E.out3(seg(t, tIn, tIn + 0.5));
  ink(g, [[x0, y + 3], [lerp(x0, x1, 0.5), y - 2], [lerp(x0, x1, u), y + 1]], w, { col, a: 0.85, t0: 0.1, t1: 0.4, seed: Math.floor(x0) });
}
function brushCircle(g, cx, cy, rx, ry, t, tIn, col = PAL.red, w = 7) {
  if (t < tIn) return; const u = E.out3(seg(t, tIn, tIn + 0.6)), pts = [];
  for (let i = 0; i <= 64 * u * 1.08; i++) { const a = -2.2 + i / 64 * TAU; pts.push([cx + Math.cos(a) * rx * (1 + 0.04 * Math.sin(i * 0.7)), cy + Math.sin(a) * ry]); }
  if (pts.length > 2) ink(g, pts, w, { col, a: 0.85, t0: 0.05, t1: 0.3, seed: 9 });
}
function arrow(g, x0, y0, x1, y1, w = 6, col = PAL.ink, a = 1, head = 1) {
  const L = Math.hypot(x1 - x0, y1 - y0); if (L < 2) return; const ux = (x1 - x0) / L, uy = (y1 - y0) / L, hs = Math.min(L * 0.4, 18 + w * 2.4) * head;
  ink(g, [[x0, y0], [lerp(x0, x1, 0.5) - uy * L * 0.015, lerp(y0, y1, 0.5) + ux * L * 0.015], [x1 - ux * hs * 0.5, y1 - uy * hs * 0.5]], w, { col, a, t0: 0.1, t1: 0.02, min: 0.5, seed: Math.floor(x0 + y1) });
  const hp = [[x1, y1], [x1 - ux * hs - uy * hs * 0.55, y1 - uy * hs + ux * hs * 0.55], [x1 - ux * hs * 0.7, y1 - uy * hs * 0.7], [x1 - ux * hs + uy * hs * 0.55, y1 - uy * hs - ux * hs * 0.55]];
  g.globalAlpha = a; fillPts(g, hp, css(col)); g.globalAlpha = 1;
}
function glowDot(g, x, y, r, col, a = 1) {
  const gr = g.createRadialGradient(x, y, 0, x, y, r);
  gr.addColorStop(0, css(col, a)); gr.addColorStop(0.35, css(col, a * 0.45)); gr.addColorStop(1, css(col, 0));
  g.fillStyle = gr; g.beginPath(); g.arc(x, y, r, 0, TAU); g.fill();
}
const fade = (t, a, b, fi = 0.4, fo = 0.4) => Math.min(seg(t, a, a + fi), 1 - seg(t, b, b + fo));

// ---------- painted plates ----------
// warm paper board for ink diagrams (a big plate so the camera can drift); tone: 'warm' | 'blue' | 'sage'
function paperPlate(seed, tone = 'warm') {
  const r = RNG(seed), cols = tone === 'blue' ? PAL.paperBlue : tone === 'sage' ? PAL.paperSage : PAL.paper;
  return makePlate(-200, -160, 2320, 1400, 0.75, g => {
    g.fillStyle = css(cols[0]); g.fillRect(-200, -160, 2320, 1400);
    const pts = [[-200, -160], [2120, -160], [2120, 1240], [-200, 1240]];
    paintRegion(g, pts, cols[0], r, { size: 70, dens: 1.6, stretch: 2.6, a: 0.18, cols, var: 8 });
    // faint ruled guide lines and a soft vignette of pigment at the edges
    for (let y = -100; y < 1240; y += 72) ink(g, [[-200, y + r() * 4], [2120, y + r() * 4]], 1.4, { col: tone === 'warm' ? '#b9a27c' : '#9aa6a4', a: 0.16, t0: 0, t1: 0, min: 0.6, jit: 0.4, seed: y });
    const v = g.createRadialGradient(960, 540, 500, 960, 540, 1500); v.addColorStop(0, 'rgba(90,60,30,0)'); v.addColorStop(1, 'rgba(90,60,30,0.35)');
    g.fillStyle = v; g.fillRect(-200, -160, 2320, 1400);
    for (let i = 0; i < 14; i++) { const x = r() * 2200 - 150, y = r() * 1300 - 120; g.fillStyle = css('#a07a4a', 0.05 + 0.05 * r()); g.beginPath(); g.ellipse(x, y, 30 + r() * 120, 20 + r() * 80, r() * 3, 0, TAU); g.fill(); }
    paperOver(g, 2320, 1400, 0.35);
  });
}
// deep indigo watercolour wash with faint flow streaks, for vortex and singularity shots
function deepPlate(seed, warm = 0) {
  const r = RNG(seed);
  return makePlate(-260, -200, 2440, 1480, 0.7, g => {
    g.save(); g.translate(-260, -200); paintSky(g, 2440, 1480, PAL.deep.map(([y, c]) => [y * 1.37, mixv(c, '#3a1a20', warm)]), r, { n: 2600, var: 6 }); g.restore();
    for (let i = 0; i < 70; i++) { // long curling streamline washes
      const cx = 960 + (r() - 0.5) * 2200, cy = 540 + (r() - 0.5) * 1300, R0 = 100 + r() * 700, a0 = r() * TAU, pts = [];
      for (let k = 0; k < 40; k++) { const a = a0 + k * 0.05, R = R0 * (1 - k * 0.006); pts.push([cx + Math.cos(a) * R, cy + Math.sin(a) * R * 0.7]); }
      ink(g, pts, 2 + r() * 6, { col: r() < 0.5 ? '#2c4f7a' : '#1f3a5c', a: 0.25, t0: 0.4, t1: 0.4, seed: i });
    }
    paperOver(g, 2440, 1480, 0.22);
  });
}
function plateDrift(g, pl, t, t0, t1, o = {}) { // slow push-in so no board ever sits dead still
  const u = seg(t, t0, t1), z = lerp(o.z0 ?? 1.0, o.z1 ?? 1.05, u), cx = lerp(o.x0 ?? 960, o.x1 ?? 970, u), cy = lerp(o.y0 ?? 540, o.y1 ?? 535, u);
  cam(g, cx, cy, z); plateDraw(g, pl); return { z, cx, cy };
}

// ---------- graphs ----------
// axes box at (x,y) bottom-left, w x h; returns a mapper from data to screen
function axes(g, x, y, w, h, o = {}) {
  const a = o.a ?? 1; g.globalAlpha = a;
  ink(g, [[x, y + 6], [x, y - h]], 4, { col: PAL.ink, t0: 0.02, t1: 0.1, min: 0.5, seed: 3 });
  ink(g, [[x - 6, y], [x + w, y]], 4, { col: PAL.ink, t0: 0.02, t1: 0.1, min: 0.5, seed: 4 });
  arrow(g, x, y - h + 20, x, y - h - 10, 3, PAL.ink, a); arrow(g, x + w - 20, y, x + w + 10, y, 3, PAL.ink, a);
  g.font = `italic 500 ${o.fs ?? 34}px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textBaseline = 'middle';
  if (o.xl) { g.textAlign = 'right'; g.fillText(o.xl, x + w + 10, y + 36); }
  if (o.yl) { g.textAlign = 'left'; g.fillText(o.yl, x + 14, y - h - 4); }
  g.globalAlpha = 1;
  const X0 = o.x0 ?? 0, X1 = o.x1 ?? 1, Y0 = o.y0 ?? 0, Y1 = o.y1 ?? 1;
  return (vx, vy) => [x + (vx - X0) / (X1 - X0) * w, y - (vy - Y0) / (Y1 - Y0) * h];
}
// curve drawn on progressively (u in 0..1 of the x range), clipped to the plot box
function plot(g, map, fn, x0, x1, u, o = {}) {
  const n = o.n ?? 220, pts = [];
  for (let i = 0; i <= n * u; i++) { const vx = lerp(x0, x1, i / n), vy = fn(vx); if (!isFinite(vy)) break; const p = map(vx, vy); if (o.clipY != null && p[1] < o.clipY) { pts.push([p[0], o.clipY]); break; } pts.push(p); }
  if (pts.length > 1) ink(g, pts, o.w ?? 6, { col: o.col || PAL.blue, a: o.a ?? 1, t0: 0.02, t1: 0.02, min: 0.7, jit: 0.1, seed: o.seed ?? 8 });
  return pts[pts.length - 1];
}

// ---------- physics: 2D vortex flows (Lamb-Oseen = exact viscous vortex; point-vortex dynamics for the cores) ----------
// velocity at (x,y) from cores [{x,y,G,a2}] where a2 = core radius^2 (grows as 4*nu*t: viscous spreading)
function vortexVel(cores, x, y) {
  let u = 0, v = 0;
  for (const c of cores) { const dx = x - c.x, dy = y - c.y, r2 = dx * dx + dy * dy + 1e-6, f = c.G / (TAU * r2) * (1 - Math.exp(-r2 / c.a2)); u += -dy * f; v += dx * f; }
  return [u, v];
}
// integrate cores (point-vortex dynamics with Lamb-Oseen cores) and tracers together; store every frame at fps.
// spec: { cores:[{x,y,G,a}], nu, tracers:[[x,y],...], T, fps, sub, decay } -> { F, nF, nT, cores:[frames] }
function simFlow(spec) {
  const fps = spec.fps ?? 24, nF = Math.ceil(spec.T * fps) + 2, sub = spec.sub ?? 3, dt = 1 / fps / sub, nu = spec.nu ?? 0;
  const C = spec.cores.map(c => ({ x: c.x, y: c.y, G: c.G, a2: c.a * c.a })), P = spec.tracers.map(p => [p[0], p[1]]), nT = P.length;
  const F = new Float32Array(nF * nT * 2), CF = [];
  const step = (pts, cores, h) => { // RK2 midpoint
    const k1 = pts.map(p => vortexVel(cores, p[0], p[1])), mid = pts.map((p, i) => [p[0] + k1[i][0] * h / 2, p[1] + k1[i][1] * h / 2]);
    const k2 = mid.map(p => vortexVel(cores, p[0], p[1])); pts.forEach((p, i) => { p[0] += k2[i][0] * h; p[1] += k2[i][1] * h; });
  };
  for (let f = 0; f < nF; f++) {
    for (let i = 0; i < nT; i++) { F[(f * nT + i) * 2] = P[i][0]; F[(f * nT + i) * 2 + 1] = P[i][1]; }
    CF.push(C.map(c => [c.x, c.y, Math.sqrt(c.a2), c.G]));
    for (let s = 0; s < sub; s++) {
      // cores move in each other's field (a core does not advect itself)
      const vc = C.map((c, i) => vortexVel(C.filter((_, j) => j !== i), c.x, c.y));
      C.forEach((c, i) => { c.x += vc[i][0] * dt; c.y += vc[i][1] * dt; c.a2 += 4 * nu * dt; if (spec.decay) c.G *= Math.exp(-spec.decay * dt); if (spec.boundCores) spec.boundCores(c); });
      step(P, C, dt);
      if (spec.bound) for (const p of P) spec.bound(p);
    }
  }
  return { F, nF, nT, CF, fps };
}
// draw tracers as short tapered dye streaks from frame f-lag to f (time-continuous: interpolates between stored frames)
function drawTracers(g, sim, tl, o = {}) {
  const f = clamp(tl * sim.fps, 0, sim.nF - 1.001), f0 = Math.floor(f), fr = f - f0, lag = o.lag ?? 6, nT = sim.nT, F = sim.F;
  const P = (fi, i) => { fi = clamp(fi, 0, sim.nF - 1); const a = (fi * nT + i) * 2, b = (Math.min(fi + 1, sim.nF - 1) * nT + i) * 2; return [lerp(F[a], F[b], fr), lerp(F[a + 1], F[b + 1], fr)]; };
  g.save(); g.lineCap = 'round';
  for (let i = 0; i < nT; i += (o.every ?? 1)) {
    const col = o.colOf ? o.colOf(i) : (o.col || PAL.flow), p1 = P(f0, i), p0 = P(f0 - lag, i), pm = P(f0 - lag / 2, i);
    g.strokeStyle = col; g.globalAlpha = o.a ?? 0.7; g.lineWidth = o.w ?? 2.4;
    g.beginPath(); g.moveTo(p0[0] - 0.8, p0[1]); g.quadraticCurveTo(pm[0], pm[1], p1[0] + 0.8, p1[1]); g.stroke();
  }
  g.restore();
}

// ---------- physics: vortex tube with helical strands; spin follows angular-momentum conservation (omega ~ 1/r^2) ----------
// c: centreline points; rad(s) radius along the tube; phase: rotation (rad); o.heat 0..1 lights it up near the pinch
function vortexTube(g, c, rad, phase, o = {}) {
  const n = c.length, N = [], R = [];
  for (let i = 0; i < n; i++) { const p = c[Math.max(0, i - 1)], q = c[Math.min(n - 1, i + 1)]; let nx = -(q[1] - p[1]), ny = q[0] - p[0]; const l = Math.hypot(nx, ny) || 1; N.push([nx / l, ny / l]); R.push(rad(i / (n - 1))); }
  const body = [], back = [];
  for (let i = 0; i < n; i++) body.push([c[i][0] + N[i][0] * R[i], c[i][1] + N[i][1] * R[i]]);
  for (let i = n - 1; i >= 0; i--) body.push([c[i][0] - N[i][0] * R[i], c[i][1] - N[i][1] * R[i]]);
  g.save(); g.globalAlpha = o.a ?? 1;
  cels(g, body, { base: o.base || '#2f6b9e', sh: o.sh || '#1d4a75', sw: 8, rim: o.rim || '#bfe4ff', rw: 3, L: LIGHT, line: PAL.inkCool, lw: 2.6, seed: 12, rimA: 0.7 });
  const strands = o.strands ?? 7, tw = o.twist || (s => 10 * s); // twist(s): accumulated winding along the tube
  for (let k = 0; k < strands; k++) {
    let run = [], front = null;
    const flush = () => { if (run.length > 1) ink(g, run, front ? (o.sw ?? 3.2) : 1.4, { col: front ? (o.strandCol || '#d8f0ff') : '#4a7fae', a: front ? 0.9 : 0.5, t0: 0.15, t1: 0.15, min: 0.2, seed: k }); run = []; };
    for (let i = 0; i < n; i++) {
      const s = i / (n - 1), a = tw(s) + phase + k * TAU / strands, f = Math.sin(a) > 0;
      if (front !== null && f !== front) flush(); front = f;
      run.push([c[i][0] + N[i][0] * R[i] * Math.cos(a) * 0.92, c[i][1] + N[i][1] * R[i] * Math.cos(a) * 0.92]);
    }
    flush();
  }
  g.restore();
}

// ---------- physics: soft-disc molecular dynamics (velocity Verlet, reflecting walls), precomputed ----------
function simMolecules(seed, n, box, T, fps = 24, speed = 220, rad = 14) {
  const r = RNG(seed), [bx, by, bw, bh] = box, X = [], V = [];
  const cols = Math.ceil(Math.sqrt(n * bw / bh)), rows = Math.ceil(n / cols);
  for (let i = 0; i < n; i++) { X.push([bx + (i % cols + 0.5 + (r() - 0.5) * 0.3) * bw / cols, by + (Math.floor(i / cols) + 0.5 + (r() - 0.5) * 0.3) * bh / rows]); const a = r() * TAU, s = speed * (0.5 + r()); V.push([Math.cos(a) * s, Math.sin(a) * s]); }
  const nF = Math.ceil(T * fps) + 2, sub = 8, dt = 1 / fps / sub, F = new Float32Array(nF * n * 2), d0 = rad * 2, k = 6000;
  for (let f = 0; f < nF; f++) {
    for (let i = 0; i < n; i++) { F[(f * n + i) * 2] = X[i][0]; F[(f * n + i) * 2 + 1] = X[i][1]; }
    for (let s = 0; s < sub; s++) {
      for (let i = 0; i < n; i++) for (let j = i + 1; j < n; j++) {
        const dx = X[j][0] - X[i][0], dy = X[j][1] - X[i][1], d = Math.hypot(dx, dy);
        if (d < d0 && d > 1e-3) { const fo = k * (d0 - d) / d * dt; V[i][0] -= dx * fo; V[i][1] -= dy * fo; V[j][0] += dx * fo; V[j][1] += dy * fo; }
      }
      for (let i = 0; i < n; i++) {
        X[i][0] += V[i][0] * dt; X[i][1] += V[i][1] * dt;
        if (X[i][0] < bx + rad) { X[i][0] = bx + rad; V[i][0] = Math.abs(V[i][0]); } if (X[i][0] > bx + bw - rad) { X[i][0] = bx + bw - rad; V[i][0] = -Math.abs(V[i][0]); }
        if (X[i][1] < by + rad) { X[i][1] = by + rad; V[i][1] = Math.abs(V[i][1]); } if (X[i][1] > by + bh - rad) { X[i][1] = by + bh - rad; V[i][1] = -Math.abs(V[i][1]); }
      }
    }
  }
  return { F, nF, n, fps, rad };
}
function molAt(sim, tl, i) { const f = clamp(tl * sim.fps, 0, sim.nF - 1.001), f0 = Math.floor(f), fr = f - f0, a = (f0 * sim.n + i) * 2, b = ((f0 + 1) * sim.n + i) * 2; return [lerp(sim.F[a], sim.F[b], fr), lerp(sim.F[a + 1], sim.F[b + 1], fr)]; }
function drawMolecule(g, x, y, s, col = '#7fb8e0', seed = 1) { // a water molecule as a small cel: O with two H
  const ang = hash(seed, 5) * TAU, hx1 = pol([x, y], ang + 0.92, s * 0.78), hx2 = pol([x, y], ang - 0.92, s * 0.78);
  for (const h of [hx1, hx2]) { const c = []; for (let a = 0; a < 10; a++) c.push([h[0] + Math.cos(a / 10 * TAU) * s * 0.42, h[1] + Math.sin(a / 10 * TAU) * s * 0.42]); cels(g, c, { base: '#f4f1ea', sh: '#cfc8bc', sw: s * 0.12, L: LIGHT, line: PAL.inkCool, lw: 1.3, seed }); }
  const c = []; for (let a = 0; a < 14; a++) c.push([x + Math.cos(a / 14 * TAU) * s * 0.62, y + Math.sin(a / 14 * TAU) * s * 0.62]);
  cels(g, c, { base: col, sh: mixv(col, '#1d3550', 0.4), sw: s * 0.16, rim: '#e8f6ff', rw: s * 0.1, L: LIGHT, line: PAL.inkCool, lw: 1.6, seed });
}

// ---------- the "speed meter": a painted dial whose needle follows a given value (log-ish scale, pins at infinity) ----------
function dial(g, x, y, R, val, label, o = {}) {
  const arc = []; for (let i = 0; i <= 40; i++) { const a = Math.PI * (1 + i / 40); arc.push([x + Math.cos(a) * R, y + Math.sin(a) * R]); }
  const disc = arc.concat([[x + R, y + R * 0.18], [x - R, y + R * 0.18]]);
  cels(g, disc, { base: '#f1e6cf', sh: PAL.cardSh, sw: 6, L: LIGHT, line: PAL.ink, lw: 3, seed: 2 });
  for (let k = 0; k <= 10; k++) { const a = Math.PI * (1 + k / 10); ink(g, [[x + Math.cos(a) * R * 0.82, y + Math.sin(a) * R * 0.82], [x + Math.cos(a) * R * 0.95, y + Math.sin(a) * R * 0.95]], k % 5 ? 2 : 4, { col: k > 8 ? PAL.red : PAL.ink, t0: 0.1, t1: 0.1 }); }
  g.font = `italic 600 ${R * 0.22}px Georgia, serif`; g.fillStyle = css(PAL.red); g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('∞', x + R * 0.78, y - R * 0.2);
  const f = clamp(1 - 1 / (1 + Math.max(0, val) / (o.half ?? 3))), a = Math.PI * (1 + f * 0.985) + (f > 0.97 ? 0.01 * Math.sin(f * 900) : 0);
  ink(g, [[x, y], [x + Math.cos(a) * R * 0.85, y + Math.sin(a) * R * 0.85]], 7, { col: PAL.red, t0: 0.02, t1: 0.6, min: 0.2, seed: 6 });
  const hub = []; for (let i = 0; i < 12; i++) hub.push([x + Math.cos(i / 12 * TAU) * 14, y + Math.sin(i / 12 * TAU) * 14]); cels(g, hub, { base: '#3a2a20', lw: 2, line: PAL.ink, seed: 3 });
  if (label) { g.font = `600 ${R * 0.13}px Georgia, serif`; g.fillStyle = css(PAL.ink); g.fillText(label, x, y + R * 0.18 + 46); }
}
// envelope (for the seven Millennium problems); open 0..1 lifts the flap; glow highlights one
function envelope(g, x, y, w, label, open = 0, o = {}) {
  const h = w * 0.64, body = rough([[x - w / 2, y - h / 2], [x + w / 2, y - h / 2], [x + w / 2, y + h / 2], [x - w / 2, y + h / 2]], 1.5, Math.floor(x), 10);
  if (o.glow) { g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, y, w * 0.9, '#ffd27a', 0.35 * o.glow); g.restore(); }
  cels(g, body, { base: o.col || '#e9dcc0', sh: '#cdb98f', sw: 5, L: LIGHT, line: PAL.ink, lw: 2.4, seed: Math.floor(x) });
  const fy = y - h / 2 + h * 0.55 * (1 - 2 * open);
  cels(g, [[x - w / 2, y - h / 2], [x + w / 2, y - h / 2], [x, fy]], { base: open > 0.5 ? '#d8c69e' : '#efe3c8', sh: '#cdb98f', sw: 3, L: LIGHT, line: PAL.ink, lw: 2, seed: 3 });
  if (!open) { const s = []; for (let i = 0; i < 12; i++) s.push([x + Math.cos(i / 12 * TAU) * w * 0.07, fy + Math.sin(i / 12 * TAU) * w * 0.07 - 4]); cels(g, s, { base: '#a8321c', sh: '#7a2010', sw: 3, L: LIGHT, line: PAL.ink, lw: 1.6, seed: 4 }); }
  if (label) { g.font = `600 ${w * 0.11}px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(label, x, y + h * 0.3); }
}
// timeline ribbon with pins that drop in on cue
function ribbon(g, x0, x1, y, t, tIn, o = {}) {
  const u = E.out3(seg(t, tIn, tIn + 0.9)); if (u <= 0) return;
  ink(g, [[x0, y], [lerp(x0, x1, 0.5), y - 4], [lerp(x0, x1, u), y]], 8, { col: PAL.ink, t0: 0.02, t1: 0.05, min: 0.6, seed: 3 });
}
function pin(g, x, y, label, t, tIn, o = {}) {
  if (on2(t) < tIn) return; const k = E.out3(seg(on2(t), tIn, tIn + 0.3)), yy = y - 40 * (1 - k);
  g.globalAlpha = k; ink(g, [[x, y], [x, yy - (o.h ?? 70)]], 3, { col: PAL.ink, t0: 0, t1: 0.1 });
  const c = []; for (let i = 0; i < 12; i++) c.push([x + Math.cos(i / 12 * TAU) * 13, yy - (o.h ?? 70) + Math.sin(i / 12 * TAU) * 13]);
  cels(g, c, { base: o.col || PAL.red, sh: '#7a2010', sw: 4, rim: '#ffd0b8', rw: 3, L: LIGHT, line: PAL.ink, lw: 2, seed: Math.floor(x) }); g.globalAlpha = 1;
  tag(g, x, yy - (o.h ?? 70) - (o.up ?? 70), label, t, tIn + 0.1, o.tOut ?? 1e9, { size: o.size ?? 30 });
}
// grid of n "agents" lighting up (on twos), a wave sweeping out from the centre
function agentGrid(g, x, y, w, h, cols, rows, t, tIn, o = {}) {
  const dx = w / cols, dy = h / rows, tt = on2(t);
  if (t > tIn) { g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x + w / 2, y + h / 2, Math.max(w, h) * 0.75, o.col || '#ffd27a', 0.18 * seg(t, tIn, tIn + 2)); g.restore(); }
  for (let j = 0; j < rows; j++) for (let i = 0; i < cols; i++) {
    const cx = x + (i + 0.5) * dx, cy = y + (j + 0.5) * dy, d = Math.hypot(i - cols / 2, j - rows / 2) / (cols / 2);
    const on = seg(tt, tIn + d * (o.spread ?? 1.6), tIn + d * (o.spread ?? 1.6) + 0.3), pulse = 0.5 + 0.5 * Math.sin(tt * 3 + hash(i * 131 + j, 2) * 9);
    g.fillStyle = on > 0 ? css(mixv('#5a7a9a', o.col || '#ffd27a', on), 0.55 + 0.45 * on * pulse) : css('#5a6a7a', 0.35);
    g.fillRect(cx - dx * 0.32, cy - dy * 0.32, dx * 0.64, dy * 0.64);
  }
}
// Lean-style code lines scrolling with ticks
function codeScroll(g, x, y, w, h, lines, t, t0, o = {}) {
  g.save(); g.beginPath(); g.rect(x, y, w, h); g.clip();
  fillPts(g, rough([[x, y], [x + w, y], [x + w, y + h], [x, y + h]], 2, 5, 12), css('#1c2430'));
  const lh = 44, off = Math.max(0, (t - t0) * (o.speed ?? 36));
  g.font = `500 28px Consolas, "Courier New", monospace`; g.textBaseline = 'middle'; g.textAlign = 'left';
  for (let k = 0; k < 60; k++) {
    const yy = y + 40 + k * lh - off; if (yy < y - lh || yy > y + h + lh) continue;
    const L = lines[k % lines.length], ok = !(o.bad && o.bad(k));
    g.fillStyle = css('#cfd8e3', 0.9); g.fillText(L, x + 70, yy);
    const shown = yy < y + h * 0.72;
    if (shown) { g.fillStyle = css(ok ? '#6fd08a' : '#ff6a5a'); g.font = `700 30px "Segoe UI Symbol", Georgia, serif`; g.fillText(ok ? '✓' : '✗', x + 22, yy); g.font = `500 28px Consolas, "Courier New", monospace`; }
  }
  g.restore();
  inkLoop(g, rough([[x, y], [x + w, y], [x + w, y + h], [x, y + h]], 2, 5, 12), 3, { col: PAL.ink, seed: 2 });
}
// a painted pencil balanced on its tip; theta from the real inverted-pendulum equation (precomputed)
function simPencil(T, theta0 = 0.002, L = 0.18, fps = 24) {
  const gq = 9.81, nF = Math.ceil(T * fps) + 2, out = new Float32Array(nF); let th = theta0, w = 0; const sub = 40, dt = 1 / fps / sub;
  for (let f = 0; f < nF; f++) { out[f] = th; for (let s = 0; s < sub; s++) { if (Math.abs(th) < 1.45) { w += 1.5 * gq / L * Math.sin(th) * dt; th += w * dt; } } }
  return { out, fps };
}
function drawPencil(g, x, y, len, th, o = {}) {
  const tip = [x, y], top = [x + Math.sin(th) * len, y - Math.cos(th) * len], n = [Math.cos(th), Math.sin(th)], w = len * 0.06;
  const body = [[tip[0], tip[1]], [tip[0] + Math.sin(th) * len * 0.16 + n[0] * w, tip[1] - Math.cos(th) * len * 0.16 + n[1] * w], [top[0] + n[0] * w, top[1] + n[1] * w], [top[0] - n[0] * w, top[1] - n[1] * w], [tip[0] + Math.sin(th) * len * 0.16 - n[0] * w, tip[1] - Math.cos(th) * len * 0.16 - n[1] * w]];
  cels(g, body, { base: '#e8b23a', sh: '#b8822a', sw: 5, rim: '#ffe7a0', rw: 3, L: LIGHT, line: PAL.ink, lw: 2.6, seed: 3 });
  const wood = [[tip[0], tip[1]], body[1], body[4]]; cels(g, wood, { base: '#e8c89a', sh: '#c8a070', sw: 2, L: LIGHT, line: PAL.ink, lw: 2, seed: 4 });
  const er = [[top[0] + n[0] * w, top[1] + n[1] * w], [top[0] - n[0] * w, top[1] - n[1] * w], [top[0] - n[0] * w + Math.sin(th) * len * 0.07, top[1] - n[1] * w - Math.cos(th) * len * 0.07], [top[0] + n[0] * w + Math.sin(th) * len * 0.07, top[1] + n[1] * w - Math.cos(th) * len * 0.07]];
  cels(g, er, { base: '#e07a8a', sh: '#b85a6a', sw: 2, L: LIGHT, line: PAL.ink, lw: 2, seed: 5 });
}
// spinning toy: two weights on arms; arm length r(t); angle integrates omega = w0*(r0/r)^2 (angular momentum conserved)
function spinAngle(t, t0, rOf, w0, r0) { let a = 0; const dt = 1 / 120; for (let s = t0; s < t; s += dt) { const r = rOf(s); a += w0 * (r0 / r) * (r0 / r) * Math.min(dt, t - s); } return a; }
function drawSpinner(g, x, y, r, ang, o = {}) {
  const ry = 0.32, pA = [x + Math.cos(ang) * r, y + Math.sin(ang) * r * ry], pB = [x - Math.cos(ang) * r, y - Math.sin(ang) * r * ry];
  const back = Math.sin(ang) < 0 ? pA : pB, front = back === pA ? pB : pA;
  const ball = (p, s) => { const c = []; for (let i = 0; i < 16; i++) c.push([p[0] + Math.cos(i / 16 * TAU) * s, p[1] + Math.sin(i / 16 * TAU) * s]); cels(g, c, { base: '#c8452c', sh: '#8a2a18', sw: s * 0.3, rim: '#ffc0a0', rw: s * 0.2, L: LIGHT, line: PAL.ink, lw: 2.4, seed: Math.floor(s) }); };
  ink(g, [[x, y - 220], [x, y + 160]], 10, { col: '#5a4a3a', t0: 0.05, t1: 0.05, min: 0.7 });
  ball(back, 30); ink(g, [back, [x, y], front], 7, { col: '#3a2a20', t0: 0.05, t1: 0.05, min: 0.6, seed: 2 }); ball(front, 32);
  // motion arcs: count and length grow with spin rate
  const sp = o.rate ?? 1; for (let k = 1; k <= Math.min(5, Math.ceil(sp)); k++) { const pts = []; for (let i = 0; i <= 12; i++) { const a = ang - i * 0.06 * k * 0.6; pts.push([x + Math.cos(a) * (r + 46), y + Math.sin(a) * (r + 46) * ry]); } ink(g, pts, 3, { col: PAL.ink, a: 0.35, t0: 0.9, t1: 0.1, seed: k }); }
}
// a nested "cascade" of swirling rings; layer k has radius R*q^k and fires at time tk (spins up, hands off inward)
function cascadeRings(g, x, y, R, q, n, t, fire, o = {}) {
  for (let k = 0; k < n; k++) {
    const rk = R * Math.pow(q, k), tk = fire(k), act = seg(t, tk - 0.4, tk + 0.4), spent = seg(t, tk + 0.6, tk + 2.0);
    const spin = (t - tk) * (1.2 / Math.pow(q, k * 1.5)), pts = [];
    for (let i = 0; i <= 60; i++) { const a = i / 60 * TAU; pts.push([x + Math.cos(a) * rk, y + Math.sin(a) * rk * 0.62]); }
    const col = mixv('#5a86b4', '#ffd27a', act * (1 - spent * 0.6));
    ink(g, pts, Math.max(1.2, 6 * Math.pow(q, k * 0.5)), { col: css(col), a: 0.4 + 0.6 * act, t0: 0, t1: 0, min: 0.5, jit: 0.15, seed: k });
    if (act > 0) for (let j = 0; j < 4; j++) { const a0 = spin + j * TAU / 4, arc = []; for (let i = 0; i <= 10; i++) { const a = a0 + i * 0.09; arc.push([x + Math.cos(a) * rk, y + Math.sin(a) * rk * 0.62]); } arrow(g, arc[0][0], arc[0][1], arc[10][0], arc[10][1], Math.max(1.5, 5 * Math.pow(q, k * 0.5)), css(col), act * (1 - spent * 0.7), Math.max(0.3, Math.pow(q, k * 0.6))); }
  }
}

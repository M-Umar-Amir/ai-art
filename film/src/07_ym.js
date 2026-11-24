// ================= Yang–Mills kit: real physics computed on screen =================
// LAT (from ../lattice.py, injected by build.mjs): a 2D U(1) Wilson-action Monte Carlo, L x L, beta = 4:
//   LAT.movie = base64 uint8 link angles [frame][mu][x][y] (angle = v / 256 * 2pi), LAT.loops = measured <W(R,T)> vs exact.
const QC = { r: '#d8452e', g: '#3f9a5a', b: '#3a6fd0', glu: '#f2c14e', light: '#ffe9a8', anti: '#8a5ad0' };

// ---------- glowing particle cel ----------
function particle(g, x, y, r, col, o = {}) {
  const a = o.a ?? 1; if (a <= 0) return;
  g.save(); g.globalAlpha = a;
  if (o.glow !== false) { g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, y, r * (o.glowR ?? 2.8), col, 0.4); g.restore(); }
  const pts = []; for (let i = 0; i < 30; i++) pts.push([x + Math.cos(i / 30 * TAU) * r, y + Math.sin(i / 30 * TAU) * r]);
  cels(g, pts, { base: col, sh: mixv(col, '#000000', 0.35), sw: r * 0.22, rim: mixv(col, '#ffffff', 0.55), rw: r * 0.12, L: LIGHT, line: o.line || PAL.ink, lw: Math.max(1.5, r * 0.09), seed: Math.floor(x + y) });
  if (o.label) { g.font = `700 ${r * 0.9}px Georgia, serif`; g.fillStyle = '#fff'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(o.label, x, y + r * 0.05); }
  g.restore();
}

// ---------- electric field lines of 2D-placed point charges (3D Coulomb field, RK2), computed once ----------
// charges: [{x, y, q}], returns polylines starting on positive charges (or on negatives if none positive)
function fieldLines(charges, o = {}) {
  const n = o.n ?? 20, step = o.step ?? 5, maxN = o.max ?? 900, box = o.box ?? [-400, -400, 2320, 1480], out = [];
  const E = (x, y) => { let ex = 0, ey = 0; for (const c of charges) { const dx = x - c.x, dy = y - c.y, r2 = dx * dx + dy * dy + 1e-6, r3 = r2 * Math.sqrt(r2); ex += c.q * dx / r3; ey += c.q * dy / r3; } return [ex, ey]; };
  const srcs = charges.filter(c => c.q > 0).length ? charges.filter(c => c.q > 0) : charges, sgn = charges.some(c => c.q > 0) ? 1 : -1;
  for (const c of srcs) for (let k = 0; k < n; k++) {
    const a = (k + 0.5) / n * TAU + (o.rot ?? 0); let x = c.x + Math.cos(a) * (o.r0 ?? 22), y = c.y + Math.sin(a) * (o.r0 ?? 22); const pts = [[x, y]];
    for (let s = 0; s < maxN; s++) {
      let [ex, ey] = E(x, y); let l = Math.hypot(ex, ey) || 1; const mx = x + sgn * ex / l * step * 0.5, my = y + sgn * ey / l * step * 0.5;
      [ex, ey] = E(mx, my); l = Math.hypot(ex, ey) || 1; x += sgn * ex / l * step; y += sgn * ey / l * step; pts.push([x, y]);
      if (x < box[0] || y < box[1] || x > box[2] || y > box[3]) break;
      if (charges.some(d => d.q * sgn < 0 && Math.hypot(x - d.x, y - d.y) < (o.r0 ?? 22) * 0.8)) break;
    }
    out.push(pts);
  }
  return out;
}
function drawLines(g, lines, u, o = {}) {
  for (let i = 0; i < lines.length; i++) { const L = lines[i], m = Math.max(2, Math.floor(L.length * clamp(u))); ink(g, L.slice(0, m), o.w ?? 3, { col: o.col || PAL.blue, a: o.a ?? 0.85, t0: 0.04, t1: 0.06, min: 0.4, jit: 0.15, seed: i + 3 }); }
}

// ---------- flux-tube cartoon: n lines from A to B, width profile interpolates from dipole bulge (sq=0) to a tube (sq=1) ----------
// gap: [s0, s1] removes a stretch (the snap). Returns nothing; draws.
function fluxTube(g, A, B, n, sq, o = {}) {
  const dx = B[0] - A[0], dy = B[1] - A[1], L = Math.hypot(dx, dy) || 1, ux = dx / L, uy = dy / L, nx = -uy, ny = ux;
  const W0 = o.w ?? 26, bul = o.bulge ?? L * 0.55, flare = o.flare ?? 46, ph = o.phase ?? 0;
  for (let k = 0; k < n; k++) {
    const off = n > 1 ? k / (n - 1) * 2 - 1 : 0, segs = o.gap ? [[0, o.gap[0]], [o.gap[1], 1]] : [[0, 1]];
    for (const [s0, s1] of segs) {
      const pts = [];
      for (let i = 0; i <= 60; i++) {
        const s = lerp(s0, s1, i / 60), env = Math.sin(Math.PI * s);
        const dip = off * bul * Math.pow(env, 0.8) * (1 + 0.5 * Math.abs(off)), tube = off * (W0 + flare * (Math.exp(-s / 0.07) + Math.exp(-(1 - s) / 0.07))) * Math.pow(Math.min(1, env * 6), 0.5);
        const w = lerp(dip, tube, sq) + (o.wob ?? 3) * Math.sin(s * 18 + k * 1.7 + ph);
        pts.push([A[0] + ux * s * L + nx * w, A[1] + uy * s * L + ny * w]);
      }
      ink(g, pts, o.lw ?? 3, { col: o.col || '#ffd98a', a: o.a ?? 0.85, t0: 0.05, t1: 0.05, min: 0.4, jit: 0.12, seed: k + 11 });
    }
  }
  if (o.glow) { g.save(); g.globalCompositeOperation = 'lighter'; for (let i = 0; i <= 12; i++) { const s = i / 12; if (o.gap && s > o.gap[0] && s < o.gap[1]) continue; glowDot(g, A[0] + dx * s, A[1] + dy * s, W0 * 2.6, o.glowCol || '#ffcf7a', 0.10 * o.glow); } g.restore(); }
}

// ---------- potentials and couplings (GeV, fm) ----------
const HBARC = 0.1973;                                  // GeV fm
const SIGMA = 0.18 / HBARC;                            // string tension ~0.91 GeV/fm
const cornell = r => -(4 / 3) * 0.3 * HBARC / r + SIGMA * r;   // quark-antiquark potential (Cornell form), GeV
const coulombV = r => -HBARC / 137.036 / r;            // two opposite unit charges, GeV
// one-loop running coupling alpha(Q) = 4 pi / (b0 ln(Q^2/Lambda^2)), b0 = 11 - 2 nf / 3 (Yang-Mills: nf = 0)
const alphaS = (Q, nf = 5, Lam = 0.088) => { const b0 = 11 - 2 * nf / 3, l = Math.log(Q * Q / (Lam * Lam)); return l > 0 ? 4 * Math.PI / (b0 * l) : Infinity; };
// QED fine-structure "constant" rising slowly with energy (one-loop, electron only), alpha(Q)
const alphaQED = Q => { const a0 = 1 / 137.036, me = 0.000511; return Q <= me ? a0 : a0 / (1 - 2 * a0 / (3 * Math.PI) * Math.log(Q / me)); };

// ---------- the live lattice (2D U(1)) ----------
let _LATF = null;
function latFrames() {
  if (_LATF) return _LATF;
  const bin = atob(LAT.movie), u8 = new Uint8Array(bin.length); for (let i = 0; i < bin.length; i++) u8[i] = bin.charCodeAt(i);
  const L = LAT.L, per = 2 * L * L; _LATF = [];
  for (let f = 0; f < LAT.frames; f++) { const a = new Float32Array(per); for (let i = 0; i < per; i++) a[i] = u8[f * per + i] / 256 * TAU; _LATF.push(a); }
  return _LATF;
}
const lnk = (th, L, mu, x, y) => th[(mu * L + ((x % L) + L) % L) * L + ((y % L) + L) % L];
// plaquette angle at (x, y): th0(x,y) + th1(x+1,y) - th0(x,y+1) - th1(x,y)
const plaqA = (th, L, x, y) => lnk(th, L, 0, x, y) + lnk(th, L, 1, x + 1, y) - lnk(th, L, 0, x, y + 1) - lnk(th, L, 1, x, y);
// 2x2 blocking: coarse links are products (angle sums) of two fine links -> a (L/2) lattice in the same layout
function blockLat(th, L) {
  const M = L / 2, o = new Float32Array(2 * M * M);
  for (let x = 0; x < M; x++) for (let y = 0; y < M; y++) {
    o[(0 * M + x) * M + y] = lnk(th, L, 0, 2 * x, 2 * y) + lnk(th, L, 0, 2 * x + 1, 2 * y);
    o[(1 * M + x) * M + y] = lnk(th, L, 1, 2 * x, 2 * y) + lnk(th, L, 1, 2 * x, 2 * y + 1);
  }
  return o;
}
// frame index at time t (snapshots advance at `rate` per second, ping-pong so it never jumps)
function latAt(t, t0, rate = 4) { const F = latFrames(), n = F.length, k = Math.floor(Math.max(0, t - t0) * rate) % (2 * n - 2); return F[k < n ? k : 2 * n - 2 - k]; }
// draw an n x n window of the lattice at (x, y) with cell size c: plaquette energy wash + link dials
function drawLattice(g, x, y, c, th, L, n, o = {}) {
  const heat = o.heat ?? 1, dials = o.dials ?? (c >= 40);
  for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) {
    const e = (1 - Math.cos(plaqA(th, L, i, j))) * 1.1 / (o.emax ?? 1.1); // 0 calm .. 2 max
    if (heat > 0) { g.fillStyle = css(mixv(o.calm || '#f2e6c8', o.hot || '#c2412a', clamp(e / 1.1)), heat * (0.25 + 0.6 * clamp(e / 1.1))); g.fillRect(x + i * c + 1, y + (n - 1 - j) * c + 1, c - 2, c - 2); }
  }
  g.strokeStyle = css(o.grid || PAL.ink, o.gridA ?? 0.55); g.lineWidth = Math.max(1, c * 0.04);
  g.beginPath(); for (let i = 0; i <= n; i++) { g.moveTo(x + i * c, y); g.lineTo(x + i * c, y + n * c); g.moveTo(x, y + i * c); g.lineTo(x + n * c, y + i * c); } g.stroke();
  if (dials) for (let i = 0; i < n; i++) for (let j = 0; j < n; j++) for (let mu = 0; mu < 2; mu++) {
    const a = lnk(th, L, mu, i, j), cx = x + i * c + (mu === 0 ? c / 2 : 0), cy = y + (n - j) * c - (mu === 1 ? c / 2 : 0), r = c * 0.17;
    g.fillStyle = css('#fbf4e4', 0.95); g.beginPath(); g.arc(cx, cy, r, 0, TAU); g.fill(); g.strokeStyle = css(PAL.ink, 0.8); g.lineWidth = 1.5; g.stroke();
    g.beginPath(); g.moveTo(cx, cy); g.lineTo(cx + Math.cos(a) * r * 0.9, cy - Math.sin(a) * r * 0.9); g.strokeStyle = css(o.hand || PAL.red); g.lineWidth = 2.2; g.stroke();
  }
}
// a Wilson loop outline (R x T cells) on a drawn lattice window
function loopRect(g, x, y, c, n, i0, j0, R, T, u = 1, o = {}) {
  const px = x + i0 * c, py = y + (n - j0 - T) * c, pts = [[px, py + T * c], [px + R * c, py + T * c], [px + R * c, py], [px, py], [px, py + T * c]];
  const d = densify(pts, false, 8), m = Math.max(2, Math.floor(d.length * clamp(u)));
  if (o.fill) { g.fillStyle = css(o.fill, (o.fillA ?? 0.25) * clamp(u)); g.fillRect(px, py, R * c, T * c); }
  ink(g, d.slice(0, m), o.w ?? 7, { col: o.col || PAL.gold, a: 0.95, t0: 0.02, t1: 0.02, min: 0.7, jit: 0.1, seed: R * 7 + T });
}

// ---------- rotations of a book (real 3D rotation matrices; the order matters) ----------
const RX = a => [[1, 0, 0], [0, Math.cos(a), -Math.sin(a)], [0, Math.sin(a), Math.cos(a)]];
const RY = a => [[Math.cos(a), 0, Math.sin(a)], [0, 1, 0], [-Math.sin(a), 0, Math.cos(a)]];
const RZ = a => [[Math.cos(a), -Math.sin(a), 0], [Math.sin(a), Math.cos(a), 0], [0, 0, 1]];
const M3 = (A, B) => A.map((r, i) => [0, 1, 2].map(j => r[0] * B[0][j] + r[1] * B[1][j] + r[2] * B[2][j]));
const MV = (A, v) => [A[0][0] * v[0] + A[0][1] * v[1] + A[0][2] * v[2], A[1][0] * v[0] + A[1][1] * v[1] + A[1][2] * v[2], A[2][0] * v[0] + A[2][1] * v[1] + A[2][2] * v[2]];
// a book: 1.0 x 1.4 x 0.25 box; cover (z+) red with a gold band; spine (x-) dark; pages cream. Drawn with a fixed oblique view.
function drawBook(g, x, y, s, R, o = {}) {
  const hx = 0.5, hy = 0.7, hz = 0.14, V = R0 => [[-hx, -hy, -hz], [hx, -hy, -hz], [hx, hy, -hz], [-hx, hy, -hz], [-hx, -hy, hz], [hx, -hy, hz], [hx, hy, hz], [-hx, hy, hz]].map(v => MV(R0, v));
  const view = M3(RX(-0.45), RY(0.55)), P0 = V(M3(view, R));
  const F = [[4, 5, 6, 7, '#b8361f', 'cover'], [0, 3, 2, 1, '#7a2416', 'back'], [0, 4, 7, 3, '#3a2218', 'spine'], [1, 2, 6, 5, '#f3ead6', 'pages'], [3, 7, 6, 2, '#ece0c4', 'pages'], [0, 1, 5, 4, '#ece0c4', 'pages']];
  const faces = F.map(f => { const p = f.slice(0, 4).map(i => P0[i]), nz = (p[1][0] - p[0][0]) * (p[2][1] - p[0][1]) - (p[1][1] - p[0][1]) * (p[2][0] - p[0][0]); return { p, col: f[4], kind: f[5], z: p.reduce((a, q) => a + q[2], 0) / 4, nz }; });
  faces.filter(f => f.nz > 0).sort((a, b) => a.z - b.z).forEach(f => {
    const pts = f.p.map(q => [x + q[0] * s, y - q[1] * s]);
    cels(g, pts, { base: f.col, line: PAL.ink, lw: 3, seed: 5 });
    if (f.kind === 'cover') { const c = (i, j, u, v) => [lerp(lerp(pts[0][0], pts[1][0], u), lerp(pts[3][0], pts[2][0], u), v), lerp(lerp(pts[0][1], pts[1][1], u), lerp(pts[3][1], pts[2][1], u), v)]; fillPts(g, [c(0, 0, 0.12, 0.62), c(0, 0, 0.88, 0.62), c(0, 0, 0.88, 0.74), c(0, 0, 0.12, 0.74)], css('#e8b84a')); }
  });
}

// ---------- energy spectrum: (a) continuum from zero, (b) a gap then levels ----------
function spectrum(g, x, y, w, h, gap, u, o = {}) {
  ink(g, [[x, y], [x + w, y]], 5, { col: PAL.ink, t0: 0.02, t1: 0.02, min: 0.8, seed: 2 });
  g.font = `italic 500 28px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textAlign = 'left'; g.textBaseline = 'middle'; g.fillText(o.vac ?? 'empty space', x + w + 14, y);
  if (!gap) { for (let i = 1; i < 60 * u; i++) { const yy = y - i / 60 * h; ink(g, [[x, yy], [x + w, yy]], 2.5, { col: o.col || PAL.blue, a: 0.7, t0: 0.1, t1: 0.1, seed: i }); } return; }
  const gy = y - gap * h;
  for (let i = 0; i < 40 * u; i++) { const yy = gy - Math.pow(i / 40, 0.8) * (h * (1 - gap)); ink(g, [[x, yy], [x + w, yy]], i ? 2.5 : 5, { col: i ? (o.col || PAL.blue) : PAL.red, a: i ? 0.7 : 1, t0: 0.1, t1: 0.1, seed: i + 50 }); }
  if (u > 0.3) { g.fillStyle = css(PAL.gold, 0.25 * seg(u, 0.3, 0.8)); g.fillRect(x, gy, w, y - gy); arrow(g, x - 40, y, x - 40, gy + 6, 4, PAL.red, seg(u, 0.4, 0.9)); arrow(g, x - 40, gy, x - 40, y - 6, 4, PAL.red, seg(u, 0.4, 0.9)); g.globalAlpha = seg(u, 0.4, 0.9); g.font = `italic 700 44px Georgia, serif`; g.fillStyle = css(PAL.red); g.textAlign = 'right'; g.fillText('Δ', x - 58, (y + gy) / 2); g.globalAlpha = 1; }
}

// ---------- a horizontal stacked bar (e.g. proton mass budget) ----------
function stackBar(g, x, y, w, h, parts, u, o = {}) { // parts: [[value, col, label]]
  const tot = parts.reduce((a, p) => a + p[0], 0); let cx = x;
  for (const [v, col, label] of parts) {
    const ww = w * v / tot * clamp(u); if (ww <= 0) continue;
    cels(g, [[cx, y], [cx + ww, y], [cx + ww, y + h], [cx, y + h]], { base: col, line: PAL.ink, lw: 2.5, seed: Math.floor(cx) });
    if (label && ww > 120) { g.font = `600 ${o.fs ?? 32}px Georgia, serif`; g.fillStyle = '#fff'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(label, cx + ww / 2, y + h / 2); }
    cx += ww;
  }
}

// ---------- check list rows: [text, mark, tIn] (mark: '✓' '✗' '?') ----------
function checklist(g, x, y, rows, t, o = {}) {
  const fs = o.size ?? 40, dy = o.dy ?? fs * 1.9;
  rows.forEach(([text, mark, tIn, tMark], i) => {
    if (on2(t) < tIn) return; const a = seg(on2(t), tIn, tIn + 0.3), yy = y + i * dy;
    g.globalAlpha = a; g.font = `500 ${fs}px Georgia, serif`; g.fillStyle = css(o.col || PAL.ink); g.textAlign = 'left'; g.textBaseline = 'middle'; g.fillText(text, x + fs * 1.4, yy);
    const box = rough([[x, yy - fs * 0.5], [x + fs, yy - fs * 0.5], [x + fs, yy + fs * 0.5], [x, yy + fs * 0.5]], 1.5, i + 3, 6);
    inkLoop(g, box, 3, { col: o.col || PAL.ink, seed: i });
    if (mark && on2(t) > (tMark ?? tIn + 0.4)) { const k = seg(on2(t), tMark ?? tIn + 0.4, (tMark ?? tIn + 0.4) + 0.2); g.globalAlpha = a * k; g.font = `800 ${fs * 1.25}px "Segoe UI Symbol", Georgia, serif`; g.fillStyle = css(mark === '✓' ? '#3f9a5a' : mark === '✗' ? PAL.red : PAL.gold); g.textAlign = 'center'; g.fillText(mark, x + fs * 0.55, yy - 2); }
    g.globalAlpha = 1;
  });
}

// ---------- a document page with ruled text lines; hl: index of a highlighted line block ----------
function docPage(g, x, y, w, h, t, tIn, o = {}) {
  if (on2(t) < tIn) return; const k = E.out3(seg(on2(t), tIn, tIn + 0.4));
  g.save(); g.globalAlpha = k; g.translate(x, y + 30 * (1 - k));
  const sh = rough([[-w / 2, -h / 2], [w / 2, -h / 2], [w / 2, h / 2], [-w / 2, h / 2]], 2, 7, 12);
  fillPts(g, offsetPts(sh, 10, 12), css('#000', 0.22)); cels(g, sh, { base: '#fbf6ea', sh: '#e3d8c2', sw: 5, L: LIGHT, line: PAL.ink, lw: 2.4, seed: 7 });
  if (o.head) { g.font = `700 ${o.hs ?? 30}px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(o.head, 0, -h / 2 + 50); }
  const n = o.lines ?? 16;
  for (let l = 0; l < n; l++) { const yy = -h / 2 + 100 + l * ((h - 160) / n); ink(g, [[-w / 2 + 50, yy], [w / 2 - 50 - hash(l, 4) * w * 0.3, yy]], 3, { col: '#8a7a68', a: 0.5, t0: 0, t1: 0, seed: l }); }
  if (o.foot) { const yy = h / 2 - 70; g.fillStyle = css(PAL.gold, 0.35 * seg(on2(t), o.footT ?? tIn, (o.footT ?? tIn) + 0.4)); g.fillRect(-w / 2 + 34, yy - 30, w - 68, 60); g.font = `italic 600 ${o.fs ?? 26}px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textAlign = 'left'; g.fillText(o.foot, -w / 2 + 50, yy); }
  g.restore();
}

// ---------- a blackboard plate (dark green slate with chalk dust) ----------
function boardPlate(seed) {
  const r = RNG(seed);
  return makePlate(-200, -160, 2320, 1400, 0.7, g => {
    g.fillStyle = '#1f3a30'; g.fillRect(-200, -160, 2320, 1400);
    paintRegion(g, [[-200, -160], [2120, -160], [2120, 1240], [-200, 1240]], '#1f3a30', r, { size: 80, dens: 1.4, stretch: 3, a: 0.2, cols: ['#24433a', '#1a3229', '#2a4a40'], var: 6 });
    for (let i = 0; i < 40; i++) { const x = r() * 2300 - 200, y = r() * 1400 - 160; ink(g, [[x, y], [x + 200 + r() * 400, y + (r() - 0.5) * 30]], 20 + r() * 40, { col: '#c8d6cc', a: 0.035, t0: 0.4, t1: 0.4, seed: i }); }
    // wooden frame and chalk ledge
    g.fillStyle = '#5a3a22'; g.fillRect(-200, 1010, 2320, 260); g.fillStyle = '#7a5232'; g.fillRect(-200, 1010, 2320, 22);
    paperOver(g, 2320, 1400, 0.25);
  });
}
const CHALK = '#eef2e8';
function chalkText(g, text, x, y, size, t, tIn, dur = 1.2, o = {}) { return writeText(g, text, x, y, size, t, tIn, dur, Object.assign({ col: CHALK, upright: true }, o)); }

// ---------- an empty wooden chair (Yang sat down), side view ----------
function chair(g, x, y, s, o = {}) {
  const C = (pts, col) => cels(g, pts.map(p => [x + p[0] * s, y + p[1] * s]), { base: col, line: PAL.ink, lw: 3, seed: 4 });
  C([[-0.5, -0.05], [0.5, -0.05], [0.5, 0.05], [-0.5, 0.05]], '#8a5a34');            // seat
  C([[0.38, -1.1], [0.5, -1.1], [0.5, -0.05], [0.38, -0.05]], '#7a4e2c');             // back post
  C([[-0.04, -1.0], [0.42, -1.0], [0.42, -0.6], [-0.04, -0.6]].map(p => [p[0] + 0.0, p[1]]), '#9a6a40'); // back rail
  for (const lx of [-0.46, 0.4]) C([[lx, 0.05], [lx + 0.09, 0.05], [lx + 0.09, 0.95], [lx, 0.95]], '#6a4226');
}

// ---------- Higgs potential cross-section V(phi) = (phi^2 - 1)^2 ----------
const higgsV = p => (p * p - 1) * (p * p - 1);

// ---------- bounded sequences for the compactness picture ----------
const seqA = n => Math.sin(n);                                    // dense in [-1, 1]
// indices where sin(n) gets ever closer to a target (a convergent subsequence), computed by search
function subseq(target, count, nMax = 400000) { const out = []; let best = 1; for (let n = 1; n < nMax && out.length < count; n++) { const d = Math.abs(Math.sin(n) - target); if (d < best * 0.6) { out.push(n); best = d; } } return out; }

// ---------- eight free waves (Maxwell plane waves) in a 4 x 2 panel grid ----------
function eightWaves(g, x, y, w, h, t, u, o = {}) {
  const cols = ['#d8452e', '#e08a2e', '#d8b02e', '#5aa04a', '#2f9a8a', '#3a6fd0', '#6a4fc0', '#b04a9a'], pw = w / 4, ph = h / 2;
  for (let k = 0; k < 8; k++) {
    const a = seg(u, k * 0.08, k * 0.08 + 0.3); if (a <= 0) continue;
    const px = x + (k % 4) * pw, py = y + Math.floor(k / 4) * ph, dir = hash(k, 9) * TAU;
    g.save(); g.globalAlpha = a; const box = rough([[px + 8, py + 8], [px + pw - 8, py + 8], [px + pw - 8, py + ph - 8], [px + 8, py + ph - 8]], 1.5, k, 10);
    fillPts(g, box, css('#0f1a2a', 0.85)); g.save(); path(g, box); g.clip();
    for (let l = 0; l < 7; l++) { const pts = []; for (let i = 0; i <= 50; i++) { const s = i / 50 * (pw - 16), ph0 = (l - 3) * 22; const xx = px + 8 + s, yy = py + ph / 2 + ph0 + 14 * Math.sin(s * 0.06 * (1 + 0.2 * Math.cos(dir)) - t * 3.2 + l * 0.4 + k); pts.push([xx, yy]); } ink(g, pts, 2.2, { col: cols[k], a: 0.85, t0: 0.05, t1: 0.05, seed: k * 9 + l }); }
    g.restore(); inkLoop(g, box, 2.5, { col: '#e8eef4', seed: k }); g.restore();
  }
}

// ---------- jets from a collision point (schematic event display) ----------
function jets(g, x, y, R, t, tIn, o = {}) {
  if (t < tIn) return; const u = E.out3(seg(t, tIn, tIn + 0.9)), r = RNG(o.seed ?? 5);
  const axes = o.axes ?? [-0.4, 2.7];
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, y, 90, '#ffe2a0', 0.6 * (1 - seg(t, tIn, tIn + 1.2)) + 0.15); g.restore();
  for (const ax of axes) for (let k = 0; k < 26; k++) {
    const a = ax + (r() - 0.5) * 0.5, len = R * (0.5 + 0.5 * r()) * u, curv = (r() - 0.5) * 0.002, pts = [];
    for (let i = 0; i <= 20; i++) { const s = i / 20 * len, aa = a + curv * s; pts.push([x + Math.cos(aa) * s, y + Math.sin(aa) * s]); }
    ink(g, pts, 2.4, { col: r() < 0.5 ? '#ffd98a' : '#8fc4e8', a: 0.85, t0: 0.02, t1: 0.3, seed: k });
  }
}

// ---------- declaration names quoted in the assessment (only the names; no invented proof text) ----------
const LEAN_YM = [
  'YangMills.WilsonFamily.Token', 'YangMills.WilsonFamily.insertion', 'YangMills.WilsonFamily.denominator', 'YangMills.WilsonRegularity.running',
  'YangMills.Hierarchy.Field', 'YangMills.Hierarchy.labels', 'YangMills.Hierarchy.correlation', 'YangMills.Hierarchy.Properties.wilson',
  'YangMills.Hierarchy.Reconstructed.smooth', 'YangMills.Hierarchy.Reconstructed.axioms', 'YangMills.Hierarchy.centeredTrace',
  'YangMills.Hierarchy.Properties.nonfree', 'YangMills.Hierarchy.TraceTest.excludes', 'YangMills.Hierarchy.Properties.ope',
  'YangMills.jointExistence', 'YMReview.Acceptance.combined_acceptance', 'Kitchen.lean  ·  Cake, cookie', 'Parade.lean  ·  anxiety', 'TicketsChart.lean  ·  Farewell.dead',
];

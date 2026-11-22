// ================= scene helpers: shot wrapper with camera drift, the tea cup, the Navier-Stokes equation, flow vignettes =================
// scene(): a shot whose t1 comes from the next shot's start (95_timeline). S._sh is the shot itself.
function scene(name, t0, prep, draw, o = {}) {
  shot(name, t0, 0, () => { const S = prep ? prep() : {}; S._sh = SHOTS.find(s => s.name === name); return S; }, draw, o.in === undefined ? { in: { type: 'dissolve', dur: 0.8 } } : o);
}
// a paper board shot: drifting paper plate + items drawn in board space
function board(name, t0, tone, seed, draw, o = {}) {
  scene(name, t0, () => Object.assign({ pl: paperPlate(seed, tone) }, o.prep ? o.prep() : {}), (g, l, t, S) => {
    g.save(); plateDrift(g, S.pl, t, S._sh.t0, S._sh.t1, o.cam || {}); draw(g, t, S); g.restore(); if (o.over) o.over(g, t, S);
  }, o.tr ? { in: o.tr } : {});
}
function deepScene(name, t0, seed, draw, o = {}) {
  scene(name, t0, () => Object.assign({ pl: deepPlate(seed, o.warm ?? 0) }, o.prep ? o.prep() : {}), (g, l, t, S) => {
    g.save(); plateDrift(g, S.pl, t, S._sh.t0, S._sh.t1, o.cam || { z1: 1.06 }); draw(g, t, S); g.restore(); if (o.over) o.over(g, t, S);
  }, o.tr ? { in: o.tr } : {});
}

// ---------- the Navier-Stokes equation, written term by term ----------
// terms: time derivative, advection, pressure, viscosity, force; plus incompressibility. at[k]: reveal time of term k; hl[k]: highlight time
const NS_TERMS = ['∂u/∂t', '+', '(u·∇)u', '=', '−∇p/ρ', '+', 'ν∇²u', '+', 'f'];
function nsEquation(g, x, y, size, t, at, o = {}) {
  g.save(); g.font = `italic 500 ${size}px Georgia, "Cambria Math", serif`;
  const gap = size * 0.28, ws = NS_TERMS.map(s => g.measureText(s).width), tot = ws.reduce((a, b) => a + b, 0) + gap * (ws.length - 1);
  let xx = x - tot / 2; const pos = [];
  NS_TERMS.forEach((s, k) => {
    const tk = at[k] ?? at[at.length - 1];
    writeText(g, s, xx, y, size, t, tk, 0.6, { col: o.col || PAL.ink });
    pos.push([xx, xx + ws[k]]); xx += ws[k] + gap;
  });
  g.restore();
  if (o.hl) for (const [k, th, col, toff] of o.hl) { if (t > th && (toff == null || t < toff + 0.5)) { const a = toff != null ? 1 - seg(t, toff, toff + 0.5) : 1; g.globalAlpha = a; brushUnder(g, pos[k][0] - 6, pos[k][1] + 6, y + size * 0.62, t, th, col || PAL.red, size * 0.14); g.globalAlpha = 1; } }
  if (o.div != null) writeText(g, '∇·u = 0', x, y + size * 1.55, size * 0.8, t, o.div, 0.6, { align: 'center', col: o.col || PAL.ink });
  return pos;
}

// ---------- the tea cup, top-down: painted plate + real vortex dye (Lamb-Oseen vortex, viscous spreading and decay) ----------
const CUP = { x: 960, y: 540, R: 300 };
function teaPlate(seed) {
  const r = RNG(seed);
  return makePlate(-200, -160, 2320, 1400, 0.75, g => {
    // wooden table: long grain strokes
    g.fillStyle = css(PAL.wood[0]); g.fillRect(-200, -160, 2320, 1400);
    paintRegion(g, [[-200, -160], [2120, -160], [2120, 1240], [-200, 1240]], PAL.wood[0], r, { size: 46, dens: 2.0, stretch: 7, a: 0.3, cols: PAL.wood, ang: 0.02, angVar: 0.03 });
    for (let i = 0; i < 90; i++) { const y = -160 + r() * 1400, x = -200 + r() * 2320; ink(g, [[x, y], [x + 300 + r() * 600, y + (r() - 0.5) * 20]], 1.5 + r() * 2, { col: '#3a2414', a: 0.25, t0: 0.4, t1: 0.4, seed: i }); }
    // window light falling across the table
    g.save(); g.globalCompositeOperation = 'soft-light'; const lg = g.createLinearGradient(0, 0, 1920, 1080); lg.addColorStop(0, 'rgba(255,230,180,0.7)'); lg.addColorStop(0.6, 'rgba(255,230,180,0)'); g.fillStyle = lg; g.fillRect(-200, -160, 2320, 1400); g.restore();
    const circ = (R, cx = CUP.x, cy = CUP.y, n = 120) => { const p = []; for (let i = 0; i < n; i++) { const a = i / n * TAU; p.push([cx + Math.cos(a) * R, cy + Math.sin(a) * R]); } return p; };
    fillPts(g, circ(CUP.R + 150, CUP.x + 26, CUP.y + 34), css('#000', 0.28));                       // saucer shadow
    cels(g, rough(circ(CUP.R + 150), 1.2, 3, 10), { base: '#e9e2d6', sh: '#c9bfae', sw: 14, rim: '#fffaf0', rw: 6, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 });
    inkLoop(g, circ(CUP.R + 95), 2, { col: '#a89e8c', seed: 4, a: 0.7 });
    fillPts(g, circ(CUP.R + 40, CUP.x + 18, CUP.y + 24), css('#000', 0.25));
    // handle
    const hdl = capsule([CUP.x + CUP.R + 10, CUP.y + 40], [CUP.x + CUP.R + 120, CUP.y + 120], 56, 50, 0);
    cels(g, hdl, { base: '#f1ebe0', sh: '#cfc4b2', sw: 8, rim: '#fff', rw: 4, L: LIGHT, line: PAL.ink, lw: 3, seed: 6 });
    cels(g, rough(circ(CUP.R + 36), 1, 5, 10), { base: '#f4eee4', sh: '#d2c8b6', sw: 12, rim: '#fffaf2', rw: 5, L: LIGHT, line: PAL.ink, lw: 3.2, seed: 5 });
    // tea surface: deep amber with a darker rim where the liquid meets the porcelain
    paintRegion(g, circ(CUP.R), PAL.tea, r, { size: 18, dens: 2.4, stretch: 1.2, a: 0.3, colAt: (x, y, rr) => jit(mixv(PAL.teaLt, PAL.teaDk, Math.pow(Math.hypot(x - CUP.x, y - CUP.y) / CUP.R, 2.2)), rr, 10), ang: 0, angVar: 3 });
    inkLoop(g, circ(CUP.R), 3, { col: '#3a1c08', seed: 7, a: 0.6 });
    // a bright window reflection on the tea
    g.save(); path(g, circ(CUP.R)); g.clip(); g.globalCompositeOperation = 'screen'; g.globalAlpha = 0.22;
    fillPts(g, rough([[CUP.x - 200, CUP.y - 210], [CUP.x - 60, CUP.y - 240], [CUP.x - 20, CUP.y - 130], [CUP.x - 170, CUP.y - 110]], 4, 9, 8), '#ffe9c8'); g.restore();
    paperOver(g, 2320, 1400, 0.3);
  });
}
function teaSim(seed, T, o = {}) {
  const r = RNG(seed), tr = [];
  // milk poured in three thin radial lines; the vortex's differential rotation winds them into spiral arms
  for (let i = 0; i < (o.n ?? 1500); i++) { const arm = i % 3, s = r(), rad = 20 + s * 255, a = arm * TAU / 3 + 0.3 + (r() - 0.5) * 0.05; tr.push([CUP.x + Math.cos(a) * rad + (r() - 0.5) * 6, CUP.y + Math.sin(a) * rad + (r() - 0.5) * 6]); }
  return simFlow({
    cores: [{ x: CUP.x, y: CUP.y, G: o.G ?? 150000, a: 110 }], nu: o.nu ?? 120, decay: o.decay ?? 0.09, tracers: tr, T, fps: 24, sub: 2,
    bound: p => { const dx = p[0] - CUP.x, dy = p[1] - CUP.y, d = Math.hypot(dx, dy); if (d > CUP.R - 8) { p[0] = CUP.x + dx / d * (CUP.R - 8); p[1] = CUP.y + dy / d * (CUP.R - 8); } },
  });
}
function drawTea(g, S, tl, o = {}) {
  g.save(); g.beginPath(); g.arc(CUP.x, CUP.y, CUP.R - 2, 0, TAU); g.clip();
  drawTracers(g, S.sim, tl, { lag: o.lag ?? 2, w: 4.2, a: o.a ?? 0.6, colOf: i => (i % 5 ? '#f3e3c4' : '#fff6e4') });
  g.restore();
}
function drawSpoon(g, x, y, ang, lift = 0) {
  g.save(); g.translate(x, y); g.rotate(ang); g.globalAlpha = 1 - lift;
  const sc = 1 + lift * 0.25; g.scale(sc, sc);
  const handle = capsule([0, 0], [520, 0], 22, 30, 0), bowl = []; for (let i = 0; i < 24; i++) { const a = i / 24 * TAU; bowl.push([-46 + Math.cos(a) * 62, Math.sin(a) * 40]); }
  fillPts(g, offsetPts(handle, 14, 18), css('#000', 0.25));
  cels(g, handle, { base: '#c9cdd2', sh: '#8f959c', sw: 6, rim: '#ffffff', rw: 3, L: LIGHT, line: PAL.inkCool, lw: 2.4, seed: 2 });
  cels(g, bowl, { base: '#bfc4ca', sh: '#868c94', sw: 8, rim: '#fff', rw: 4, L: LIGHT, line: PAL.inkCool, lw: 2.6, seed: 3 });
  g.restore();
}

// ---------- Joukowski airfoil: exact potential flow with the Kutta condition (lift), tracers in the circle plane ----------
function airfoilSim(T, n = 420, seed = 5) {
  const mu = [-0.09, 0.1], a = Math.hypot(1 - mu[0], mu[1]), al = 6 * Math.PI / 180, beta = Math.asin(mu[1] / a), U = 1, Gm = 4 * Math.PI * U * a * Math.sin(al + beta);
  const cm = (p, q) => [p[0] * q[0] - p[1] * q[1], p[0] * q[1] + p[1] * q[0]], cd = (p, q) => { const d = q[0] * q[0] + q[1] * q[1]; return [(p[0] * q[0] + p[1] * q[1]) / d, (p[1] * q[0] - p[0] * q[1]) / d]; };
  const J = z => cm(z, [1, 0]).map((v, i) => v + cd([1, 0], z)[i]);   // z = zeta + 1/zeta
  const vel = z => { // d zeta/dt for a tracer in the circle plane
    const s = [z[0] - mu[0], z[1] - mu[1]], ea = [Math.cos(al), Math.sin(al)], em = [Math.cos(al), -Math.sin(al)];
    const t1 = em, t2 = cd(cm([a * a, 0], ea), cm(s, s)), t3 = cd([0, Gm / TAU], s);
    const Wp = [U * (t1[0] - t2[0]) + t3[0], U * (t1[1] - t2[1]) + t3[1]];
    const dz = cd([1, 0], cm(z, z)), Jp = [1 - dz[0], -dz[1]];
    const wz = cd(Wp, Jp), vz = [wz[0], -wz[1]];                       // physical velocity (conj of dw/dz)
    return cd(vz, Jp);
  };
  const r = RNG(seed), P = [], nF = Math.ceil(T * 24) + 2, F = new Float32Array(nF * n * 2), sub = 6, dt = 1 / 24 / sub * 0.9;
  const spawn = (x0) => { for (let k = 0; k < 50; k++) { const z = [x0, (r() - 0.5) * 3.2]; if (Math.hypot(z[0] - mu[0], z[1] - mu[1]) > a * 1.02) return z; } return [x0, 1.6]; };
  for (let i = 0; i < n; i++) P.push(spawn(-2.6 + r() * 5.2));
  for (let f = 0; f < nF; f++) {
    for (let i = 0; i < n; i++) { const z = J(P[i]); F[(f * n + i) * 2] = z[0]; F[(f * n + i) * 2 + 1] = z[1]; }
    for (let s = 0; s < sub; s++) for (let i = 0; i < n; i++) {
      const v = vel(P[i]); const sp = Math.hypot(v[0], v[1]); const h = sp > 6 ? dt * 6 / sp : dt;
      P[i][0] += v[0] * h; P[i][1] += v[1] * h;
      if (Math.hypot(P[i][0] - mu[0], P[i][1] - mu[1]) < a) { const d = Math.hypot(P[i][0] - mu[0], P[i][1] - mu[1]); P[i][0] = mu[0] + (P[i][0] - mu[0]) / d * a * 1.01; P[i][1] = mu[1] + (P[i][1] - mu[1]) / d * a * 1.01; }
      const z = J(P[i]); if (z[0] > 2.8) P[i] = spawn(-2.6);
    }
  }
  const foil = []; for (let i = 0; i < 120; i++) { const th = i / 120 * TAU; foil.push(J([mu[0] + a * Math.cos(th), mu[1] + a * Math.sin(th)])); }
  return { F, nF, nT: n, fps: 24, foil };
}
function drawAirfoil(g, sim, tl, x, y, sc, o = {}) {
  const f = clamp(tl * 24, 0, sim.nF - 1.001), f0 = Math.floor(f), n = sim.nT, F = sim.F, lag = 5;
  g.save(); g.lineCap = 'round'; g.strokeStyle = css(o.col || '#bfe4ff'); g.lineWidth = 2.2; g.globalAlpha = 0.75;
  for (let i = 0; i < n; i++) {
    const a = (f0 * n + i) * 2, b = (Math.max(0, f0 - lag) * n + i) * 2;
    const x1 = F[a], y1 = F[a + 1], x0 = F[b], y0 = F[b + 1]; if (Math.abs(x1 - x0) > 1) continue;
    g.beginPath(); g.moveTo(x + x0 * sc, y - y0 * sc); g.lineTo(x + x1 * sc, y - y1 * sc); g.stroke();
  }
  g.restore();
  const pts = sim.foil.map(p => [x + p[0] * sc, y - p[1] * sc]);
  cels(g, pts, { base: '#d9dde2', sh: '#9aa3ad', sw: 5, rim: '#fff', rw: 3, L: LIGHT, line: PAL.inkCool, lw: 2.6, seed: 4 });
}
// a painted vignette card (rounded rough frame) that clips its contents
function vignette(g, x, y, w, h, t, tIn, fn, o = {}) {
  const tt = on2(t); if (tt < tIn) return; const k = E.out3(seg(tt, tIn, tIn + 0.4)), a = o.tOut != null ? 1 - seg(t, o.tOut, o.tOut + 0.5) : 1; if (a <= 0) return;
  const box = rough([[x - w / 2, y - h / 2], [x + w / 2, y - h / 2], [x + w / 2, y + h / 2], [x - w / 2, y + h / 2]], 2.5, Math.floor(x + y), 12);
  g.save(); g.globalAlpha = a * k; g.translate(x, y); g.scale(lerp(0.85, 1, k), lerp(0.85, 1, k)); g.translate(-x, -y);
  fillPts(g, offsetPts(box, 10, 12), css('#000', 0.3));
  g.save(); path(g, box); g.clip(); fn(g); g.restore();
  inkLoop(g, box, 4, { col: '#f3e8d2', seed: 3 });
  if (o.label) tag(g, x, y + h / 2 - 6, o.label, t, tIn + 0.3, o.tOut ?? 1e9, { size: 30 });
  g.restore();
}

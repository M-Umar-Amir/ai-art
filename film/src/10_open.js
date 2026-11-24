// ================= 0. Cold open =================
// a short gluon "spring" (a coil drawn as a sine along a line)
function gluonCoil(g, x0, y0, x1, y1, t, o = {}) {
  const L = Math.hypot(x1 - x0, y1 - y0), ux = (x1 - x0) / L, uy = (y1 - y0) / L, nx = -uy, ny = ux, pts = [], amp = o.amp ?? 16, k = o.k ?? 0.12;
  for (let i = 0; i <= 120; i++) { const s = i / 120 * L, w = amp * Math.sin(s * k - t * (o.speed ?? 4)) * Math.sin(Math.PI * i / 120); pts.push([x0 + ux * s + nx * w, y0 + uy * s + ny * w]); }
  if (o.glow) { g.save(); g.globalCompositeOperation = 'lighter'; ink(g, pts, (o.w ?? 6) * 3, { col: o.col || QC.glu, a: 0.25, t0: 0.1, t1: 0.1, seed: 4 }); g.restore(); }
  ink(g, pts, o.w ?? 6, { col: o.col || QC.glu, a: o.a ?? 1, t0: 0.08, t1: 0.08, min: 0.3, seed: 5 });
}
function scaleDevice(g, x, y, s, reading, o = {}) {
  const body = rough([[x - 1.3 * s, y - 0.25 * s], [x + 1.3 * s, y - 0.25 * s], [x + 1.4 * s, y + 0.25 * s], [x - 1.4 * s, y + 0.25 * s]], 2, 3, 10);
  cels(g, body, { base: '#d9d2c4', sh: '#b6ab98', sw: 8, L: LIGHT, line: PAL.ink, lw: 3, seed: 4 });
  const win = rough([[x - 0.45 * s, y - 0.12 * s], [x + 0.45 * s, y - 0.12 * s], [x + 0.45 * s, y + 0.14 * s], [x - 0.45 * s, y + 0.14 * s]], 1, 5, 8);
  cels(g, win, { base: '#22302a', line: PAL.ink, lw: 2.4, seed: 5 });
  g.font = `700 ${0.2 * s}px Consolas, monospace`; g.fillStyle = '#9ff0b8'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(reading, x, y + 0.02 * s);
}
function galaxy(g, x, y, R, rot, a = 1) {
  const r = RNG(77); g.save(); g.globalCompositeOperation = 'lighter';
  glowDot(g, x, y, R * 0.5, '#ffe6b0', 0.5 * a);
  for (let i = 0; i < 900; i++) { const arm = i % 2, u = r(), th = rot + arm * Math.PI + u * 3.2 + (r() - 0.5) * 0.5, rr = R * (0.08 + u * 0.92); glowDot(g, x + Math.cos(th) * rr, y + Math.sin(th) * rr * 0.45, 3 + r() * 5, r() < 0.3 ? '#9fc4ff' : '#ffe2b8', 0.45 * a * (1 - u * 0.6)); }
  g.restore();
}
function protonCel(g, x, y, R, t, o = {}) {
  g.save(); g.globalCompositeOperation = 'lighter';
  if (o.field) { const gr = g.createRadialGradient(x, y, R * 0.6, x, y, R * (1 + 0.5 * o.field)); gr.addColorStop(0, css('#ffcf7a', 0.35)); gr.addColorStop(0.75, css('#ffcf7a', 0.18)); gr.addColorStop(1, css('#ffcf7a', 0)); g.fillStyle = gr; g.beginPath(); g.arc(x, y, R * (1 + 0.5 * o.field), 0, TAU); g.fill(); }
  g.restore();
  const pts = []; for (let i = 0; i < 48; i++) pts.push([x + Math.cos(i / 48 * TAU) * R, y + Math.sin(i / 48 * TAU) * R]);
  g.save(); g.globalAlpha = 0.55; cels(g, pts, { base: '#3a2a4a', sh: '#22182e', sw: R * 0.1, L: LIGHT, line: '#e8d8ff', lw: 3, seed: 2 }); g.restore();
  const qs = [[QC.r, -0.35, -0.2], [QC.g, 0.32, -0.25], [QC.b, 0.02, 0.36]];
  qs.forEach(([c, qx, qy], k) => { const jx = n1(t * 0.9 + k * 5, 3) * R * 0.12, jy = n1(t * 0.8 + k * 9, 7) * R * 0.12; if (o.tubes) fluxTube(g, [x + qx * R + jx, y + qy * R + jy], [x + qs[(k + 1) % 3][1] * R + n1(t * 0.9 + ((k + 1) % 3) * 5, 3) * R * 0.12, y + qs[(k + 1) % 3][2] * R + n1(t * 0.8 + ((k + 1) % 3) * 9, 7) * R * 0.12], 5, 1, { w: 6, flare: 10, lw: 2, a: 0.6, phase: t * 3, wob: 2 }); });
  qs.forEach(([c, qx, qy], k) => { const jx = n1(t * 0.9 + k * 5, 3) * R * 0.12, jy = n1(t * 0.8 + k * 9, 7) * R * 0.12; particle(g, x + qx * R + jx, y + qy * R + jy, R * 0.13, c); });
}

board('massless', 0, 'warm', 11, (g, t, S) => {
  title(g, 960, 170, 'gluons', t, 0.4, 1e9, 110, PAL.ink, { shadow: false });
  scaleDevice(g, 960, 700, 260, t > SW(0, 'nothing') ? '0.000 kg' : '-.---');
  gluonCoil(g, 760, 600, 1160, 600, t, { glow: true });
  stamp(g, 1500, 520, 'MASS = 0', t, SW(0, 'nothing'), 1e9, { size: 64 });
  tag(g, 960, 900, 'they carry the strong force', t, SW(0, 'strong'), 1e9, { size: 40 });
  tag(g, 960, 990, 'which holds the centre of every atom together', t, SW(0, 'together') - 0.3, 1e9, { size: 32, bg: '#fbe2c0' });
}, { cam: { z0: 1.02, z1: 1.06 } });

deepScene('reach', SW(0, 'rule') - 0.4, 21, (g, t, S) => {
  const T0 = S._sh.t0, tG = SW(0, 'galaxy');
  galaxy(g, 1500, 520, 300, (t - T0) * 0.05, seg(t, tG - 0.6, tG + 0.4));
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, 260, 540, 160, '#fff2c8', 0.9);
  const u = E.out2(seg(t, T0 + 0.6, tG + 0.3)); for (let k = 0; k < 9; k++) { const a = (k - 4) * 0.05; ink(g, [[300, 540], [300 + Math.cos(a) * 1150 * u, 540 + Math.sin(a) * 1150 * u]], 4, { col: '#fff2c8', a: 0.35, t0: 0.02, t1: 0.4, seed: k }); }
  g.restore();
  tag(g, 620, 220, 'massless carrier → reaches forever', t, SW(0, 'rule'), 1e9, { size: 42 });
  tag(g, 600, 860, 'light: carried by massless photons', t, SW(0, 'light'), 1e9, { size: 36 });
  tag(g, 1500, 880, 'a galaxy · ≈ 2.5 million light years away', t, tG, 1e9, { size: 34, bg: '#fbe2c0' });
});

deepScene('stops', SW(0, 'doesn\'t') - 0.4, 31, (g, t, S) => {
  const T0 = S._sh.t0, tD = SW(0, 'dead');
  protonCel(g, 960, 500, 230, t, { field: 1 - 0.85 * E.out3(seg(t, tD - 0.4, tD + 0.5)), tubes: true });
  // the field strength vs distance, falling off a cliff past the edge
  if (t > SW(0, 'edge') - 0.3) {
    const a = seg(t, SW(0, 'edge') - 0.3, SW(0, 'edge') + 0.3); g.globalAlpha = a;
    ink(g, [[960, 820], [1190, 820]], 4, { col: '#e8eef4', t0: 0, t1: 0, seed: 2 }); ink(g, [[960, 805], [960, 835]], 3, { col: '#e8eef4', t0: 0, t1: 0 }); ink(g, [[1190, 805], [1190, 835]], 3, { col: '#e8eef4', t0: 0, t1: 0 });
    g.globalAlpha = 1; tag(g, 1075, 885, '≈ 10⁻¹⁵ m  (a millionth of a billionth of a metre)', t, SW(0, 'millionth'), 1e9, { size: 30 });
  }
  stamp(g, 1520, 300, 'STOPS DEAD', t, tD, 1e9, { size: 70 });
  tag(g, 420, 250, 'the strong force', t, T0 + 0.3, 1e9, { size: 40 });
  title(g, 1520, 470, "…it's gone", t, SW(0, 'gone'), 1e9, 52);
});

board('gap-title', SW(0, 'why', 2) - 0.3, 'blue', 41, (g, t, S) => {
  title(g, 960, 200, 'THE MASS GAP', t, SW(0, 'called'), 1e9, 120, PAL.ink, { shadow: false, upright: true, weight: 700 });
  tag(g, 560, 400, 'computer simulations: they see it', t, SW(0, 'simulations'), 1e9, { size: 36 });
  tag(g, 1360, 400, 'a proof: none in 70+ years', t, SW(0, 'prove'), 1e9, { size: 36, bg: '#fbe2c0' });
  const tE = SW(0, 'seven'), names = ['Poincaré', 'Navier–Stokes', 'Riemann', 'Yang–Mills', 'P vs NP', 'Hodge', 'BSD'];
  names.forEach((n, k) => { const tk = tE + k * 0.12; if (t < tk) return; g.save(); g.globalAlpha = seg(t, tk, tk + 0.25); envelope(g, 250 + k * 237, 650, 200, n, k === 0 ? 1 : 0, { glow: k === 3 ? seg(t, tE + 1, tE + 2) : 0 }); g.restore(); });
  tag(g, 960, 880, 'Clay Mathematics Institute · 2000 · $1,000,000 each', t, SW(0, 'million'), 1e9, { size: 38 });
}, { cam: { z0: 1.0, z1: 1.04 } });

board('news', SW(0, 'then') - 0.3, 'warm', 51, (g, t, S) => {
  ribbon(g, 220, 1700, 420, t, SW(0, 'then'));
  pin(g, 520, 420, '26 Oct 2026 · OpenAI announces an AI proof', t, SW(0, 'announced'), { size: 32 });
  pin(g, 1380, 420, '10 Nov · the panel’s verdict', t, SW(0, 'verdict'), { size: 32 });
  tag(g, 1380, 210, 'led by a Nobel prize winner', t, SW(0, 'nobel'), 1e9, { size: 30, bg: '#fbe2c0' });
  checklist(g, 600, 640, [['the proof checks out', '✓', SW(0, 'checks')], ['the theory exists (in the panel’s judgement)', '✓', SW(0, 'exists')], ['the mass gap: why the force stops', '✗', SW(0, 'untouched') - 0.6, SW(0, 'untouched')]], t, { size: 42 });
});

deepScene('footnote-tease', SW(0, 'half', 2) - 0.4, 61, (g, t, S) => {
  title(g, 960, 170, 'half of a million-dollar problem?', t, SW(0, 'half', 2), SW(0, 'footnote') - 0.3, 64);
  docPage(g, 960, 560, 640, 760, t, SW(0, 'footnote') - 0.2, { head: 'Quantum Yang–Mills Theory (2000)', foot: '² We specifically exclude …', footT: SW(0, 'footnote') + 0.4, lines: 14 });
  tag(g, 1500, 320, 'written 26 years ago\nby a member of the panel', t, SW(0, 'written'), 1e9, { size: 34 });
  stamp(g, 1480, 760, '?', t, SW(0, 'really'), 1e9, { size: 130 });
});

board('questions', SW(0, 'here\'s') - 0.3, 'sage', 71, (g, t, S) => {
  tag(g, 960, 230, '1 · How can a force made of massless particles stop dead?', t, SW(0, 'massless', 3), 1e9, { size: 40 });
  tag(g, 960, 420, '2 · What exactly did the machine prove, and how do we know?', t, SW(0, 'exactly'), 1e9, { size: 40 });
  tag(g, 960, 610, '3 · Why does the missing half matter most?', t, SW(0, 'most'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 960, 860, '1954 · a question that made a physicist sit down', t, SW(0, 'nineteen'), 1e9, { size: 36 });
});

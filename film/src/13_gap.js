// ================= 5. Mass from nothing =================
deepScene('gluon-universe', SB(5) - 0.3, 501, (g, t, S) => {
  const T0 = S._sh.t0;
  for (let k = 0; k < 7; k++) { const x = 260 + hash(k, 2) * 1400, y = 250 + hash(k, 5) * 520, a = hash(k, 8) * TAU; gluonCoil(g, x, y, x + Math.cos(a) * 200, y + Math.sin(a) * 200, t + k, { glow: true, amp: 14, w: 4, a: seg(t, T0 + k * 0.2, T0 + k * 0.2 + 0.5) }); }
  tag(g, 960, 120, 'imagine: no quarks at all, only gluons and empty space', t, SW(5, 'imagine'), 1e9, { size: 36 });
  // a radio wave made fainter and fainter (any energy, as small as you like)
  const tF = SW(5, 'expect'); if (t > tF) { const amp = 90 * Math.exp(-1.2 * Math.max(0, t - tF - 0.8)), pts = []; for (let i = 0; i <= 200; i++) pts.push([360 + i * 6, 860 - amp * Math.sin(i * 0.15 - t * 4)]); ink(g, pts, 4, { col: '#8fc4e8', a: 0.9, t0: 0.05, t1: 0.05 }); tag(g, 960, 1000, 'expected: a wave as faint as you like', t, tF + 0.3, 1e9, { size: 32 }); }
});

board('spectrum', SW(5, 'says') - 0.4, 'warm', 511, (g, t, S) => {
  const tS = SW(5, 'says');
  tag(g, 520, 140, 'no gap: any energy, down to zero', t, tS - 0.2, 1e9, { size: 34 });
  spectrum(g, 300, 880, 440, 620, 0, seg(t, tS - 0.2, tS + 1.2), { col: PAL.blue, vac: '' });
  tag(g, 1380, 140, 'a mass gap: a smallest possible lump', t, SW(5, 'smallest'), 1e9, { size: 34, bg: '#fbe2c0' });
  spectrum(g, 1160, 880, 440, 620, 0.32, seg(t, SW(5, 'smallest'), SW(5, 'smallest') + 2), { col: PAL.blue, vac: 'empty space' });
  tag(g, 1380, 1010, 'below it: nothing', t, SW(5, 'below'), 1e9, { size: 32 });
  stamp(g, 960, 560, 'THE MASS GAP', t, SW(5, 'gap', 4), 1e9, { size: 50 });
});

deepScene('glueball', SW(5, 'simulations') - 0.4, 521, (g, t, S) => {
  const T0 = S._sh.t0, cx = 640, cy = 520;
  // a knot of gluon field (trefoil, cartoon)
  for (let s = 0; s < 6; s++) { const pts = []; for (let i = 0; i <= 240; i++) { const u = i / 240 * TAU, r = 90 * (Math.cos(3 * u) + 2) + (s - 2.5) * 6; pts.push([cx + r * Math.cos(2 * u + (t - T0) * 0.2), cy + r * Math.sin(2 * u + (t - T0) * 0.2) * 0.85]); } ink(g, pts, 3, { col: QC.glu, a: 0.75 * seg(t, T0, T0 + 1), t0: 0.0, t1: 0.0, seed: s }); }
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, cx, cy, 330, '#ffcf7a', 0.25); g.restore();
  tag(g, cx, 900, 'a “glueball”: a knot of gluon field (cartoon)', t, SW(5, 'glueball'), 1e9, { size: 32 });
  // masses, GeV: glueball 1.71-1.73 (quenched lattice), proton 0.938
  const u = E.out3(seg(t, SW(5, 'twice'), SW(5, 'twice') + 1.4));
  [['glueball (gluons only)', 1.72, '#d79a2b'], ['proton', 0.938, '#3a6fd0']].forEach(([n, m, c], k) => { const w = m / 1.8 * 640 * u + 4, y = 360 + k * 200; cels(g, [[1180, y], [1180 + w, y], [1180 + w, y + 110], [1180, y + 110]], { base: c, line: '#e8eef4', lw: 2.4, seed: k }); if (u > 0.5) { g.font = '600 30px Georgia'; g.fillStyle = '#fff1cf'; g.textAlign = 'left'; g.fillText(`${n} · ${m === 1.72 ? '≈ 1.7' : '0.94'} GeV`, 1180, y - 26); } });
  tag(g, 1500, 180, 'lattice simulations, gluons only', t, SW(5, 'simulations'), 1e9, { size: 32 });
  tag(g, 1500, 900, 'built from things that weigh nothing', t, SW(5, 'entirely'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('short-range', SW(5, 'range') - 0.5, 'blue', 531, (g, t, S) => {
  const map = axes(g, 220, 880, 900, 600, { x0: 0, x1: 4, y0: 0, y1: 1.05, xl: 'distance', yl: 'force', fs: 28 });
  plot(g, map, r => 0.25 * Math.exp(-2.2 * (r - 0.5)) / (r * r), 0.5, 4, seg(t, SW(5, 'exponentially') - 0.6, SW(5, 'exponentially') + 1), { col: PAL.red, w: 6, clipY: 260 });
  tag(g, 680, 160, 'mass gap → short range (Yukawa)', t, SW(5, 'range'), 1e9, { size: 38 });
  tag(g, 680, 960, 'massless particles behaving like heavy ones', t, SW(5, 'heavy'), 1e9, { size: 30, bg: '#fbe2c0' });
  quoteCard(g, 'to explain why the nuclear force is strong but short-ranged', 'Arthur Jaffe & Edward Witten, the official problem (2000)', t, SW(5, 'explain') - 0.3, 1e9, { x: 1450, y: 520, w: 620, size: 42 });
});

board('proton-mass', SW(5, 'shock') - 0.4, 'warm', 541, (g, t, S) => {
  title(g, 960, 120, 'a scale shock: where a proton’s mass comes from', t, SW(5, 'shock'), 1e9, 50, PAL.ink, { shadow: false });
  protonCel(g, 300, 470, 170, t, { tubes: true });
  const tP = SW(5, 'percent'), tH = SW(5, 'higgs'), t99 = SW(5, 'ninetynine'), bx = 560, by = 380, bw = 1200, bh = 150;
  if (t > tH) { g.globalAlpha = seg(t, tH, tH + 0.5); inkLoop(g, rough([[bx, by], [bx + bw, by], [bx + bw, by + bh], [bx, by + bh]], 1.5, 3, 10), 3, { col: PAL.ink, a: 0.5 }); g.fillStyle = css('#3a6fd0'); g.fillRect(bx, by, Math.max(6, bw * 9 / 938), bh); g.globalAlpha = 1; }
  if (t > t99) { const w = (bw - bw * 9 / 938) * E.out3(seg(t, t99, t99 + 1.5)); cels(g, [[bx + bw * 9 / 938, by], [bx + bw * 9 / 938 + w, by], [bx + bw * 9 / 938 + w, by + bh], [bx + bw * 9 / 938, by + bh]], { base: '#c2412a', line: PAL.ink, lw: 2.5, seed: 4 }); if (w > 600) { g.font = '600 36px Georgia'; g.fillStyle = '#fff'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('energy of quarks and gluons', bx + bw / 2, by + bh / 2); } }
  tag(g, 820, 590, 'three quark masses (from the Higgs): ≈ 9 MeV ≈ 1 %', t, tP, 1e9, { size: 30 });
  tag(g, 1360, 690, 'the other 99 %: energy  (proton = 938 MeV)', t, SW(5, 'ninetynine'), 1e9, { size: 30, bg: '#fbe2c0' });
  writeText(g, 'E = mc²', 960, 790, 90, t, SW(5, 'einstein\'s'), 1.0, { align: 'center', upright: true, col: PAL.red });
  tag(g, 960, 960, 'on a bathroom scale: almost all of it is strong-force energy', t, SW(5, 'scale', 2), 1e9, { size: 34 });
});

board('why-million', SW(5, 'confident') - 0.4, 'sage', 551, (g, t, S) => {
  checklist(g, 620, 330, [['experiments agree', '✓', SW(5, 'experiments')], ['simulations agree', '✓', SW(5, 'simulations', 2)]], t, { size: 48 });
  stamp(g, 960, 720, 'SO WHY $1,000,000?', t, SW(5, 'million'), 1e9, { size: 72 });
});

// ================= 6. Physics on a grid =================
board('wilson-grid', SB(6) - 0.3, 'blue', 601, (g, t, S) => {
  const F = latFrames(), th = F[0], n = 6, c = 120, x0 = 300, y0 = 220;
  tag(g, 1450, 160, 'Kenneth Wilson · 1974', t, SW(6, 'wilson'), 1e9, { size: 40 });
  tag(g, 1450, 280, 'chop space and time into a grid', t, SW(6, 'chop'), 1e9, { size: 34 });
  if (t > SW(6, 'grid') - 0.3) { g.globalAlpha = seg(t, SW(6, 'grid') - 0.3, SW(6, 'grid') + 0.4); drawLattice(g, x0, y0, c, th, LAT.L, n, { heat: seg(t, SW(6, 'energy'), SW(6, 'energy') + 0.8), dials: t > SW(6, 'link') }); g.globalAlpha = 1; }
  tag(g, 1450, 420, 'on every link: a little rotation (a dial)', t, SW(6, 'link'), 1e9, { size: 32 });
  if (t > SW(6, 'square')) loopRect(g, x0, y0, c, n, 2, 2, 1, 1, seg(t, SW(6, 'square'), SW(6, 'square') + 0.8), { col: PAL.gold, w: 9 });
  tag(g, 1450, 560, 'around one square: four turns combined', t, SW(6, 'square'), 1e9, { size: 32 });
  tag(g, 1450, 700, 'back where you started → calm', t, SW(6, 'calm'), 1e9, { size: 32 });
  tag(g, 1450, 840, 'not → that square holds field energy', t, SW(6, 'energy'), 1e9, { size: 32, bg: '#fbe2c0' });
});

deepScene('mc', SW(6, 'chance') - 0.4, 611, (g, t, S) => {
  const T0 = S._sh.t0, th = latAt(t, T0, 4), c = 25;
  g.save(); fillPts(g, rough([[150, 130], [990, 130], [990, 970], [150, 970]], 2, 4, 12), css('#f3e8d2', 0.95)); g.restore();
  drawLattice(g, 170, 150, c, th, LAT.L, 32, { heat: 1, dials: false, gridA: 0.25, emax: 0.45 });
  tag(g, 1440, 200, 'a giant game of chance', t, SW(6, 'chance'), 1e9, { size: 40 });
  tag(g, 1440, 330, 'calm arrangements are more likely', t, SW(6, 'likely'), 1e9, { size: 32 });
  tag(g, 1440, 460, 'roll the dice, link by link', t, SW(6, 'dice'), 1e9, { size: 32 });
  tag(g, 1440, 640, 'LIVE · a real Monte Carlo simulation\n2D U(1) lattice gauge theory · 32 × 32 · β = 4', t, SW(6, 'simulation'), 1e9, { size: 28, bg: '#fbe2c0' });
  tag(g, 1440, 820, 'toy version: 2 dimensions, one dial per link\n(not three-colour rotations)', t, SW(6, 'toy'), 1e9, { size: 28 });
  tag(g, 570, 1020, 'red = field energy in that square', t, T0 + 0.8, 1e9, { size: 26 });
});

board('loops', SW(6, 'draw', 2) - 0.4, 'warm', 621, (g, t, S) => {
  const T0 = S._sh.t0, th = latAt(t, T0, 2), n = 9, c = 70, x0 = 150, y0 = 240;
  drawLattice(g, x0, y0, c, th, LAT.L, n, { heat: 0.5, dials: false });
  const k = Math.floor(Math.max(0, t - SW(6, 'loop')) / 1.6) % 5, R = k + 1;
  if (t > SW(6, 'loop')) loopRect(g, x0, y0, c, n, 4 - Math.floor(R / 2), 4 - Math.floor(R / 2), R, R, seg((t - SW(6, 'loop')) % 1.6, 0, 0.7), { col: PAL.gold, w: 8, fill: PAL.gold, fillA: 0.22 });
  // measured <W> vs area on a log scale against the exact (I1/I0)^A
  const map = axes(g, 1000, 900, 780, 620, { x0: 0, x1: 32, y0: -2, y1: 0.1, xl: 'loop area (squares)', yl: 'loop value (log scale)', fs: 26 });
  [0, -1, -2].forEach(e => { const [, y] = map(0, e); g.font = '500 24px Georgia'; g.fillStyle = css(PAL.ink); g.textAlign = 'right'; g.fillText(['1', '0.1', '0.01'][-e], 990, y); });
  const tA = SW(6, 'area'), tX = SW(6, 'punished') + 1.2;
  if (t > tA) plot(g, map, A => A * Math.log10(LAT.ratio), 0, 31, seg(t, tA, tA + 1.5), { col: PAL.red, w: 5 });
  LAT.loops.forEach((L, i) => { const tk = tX - 1.2 + i * 0.12; if (t < tk) return; const [x, y] = map(L.A, Math.log10(Math.max(1e-4, L.W))); const e1 = map(L.A, Math.log10(Math.max(1e-4, L.W + L.err)))[1], e0 = map(L.A, Math.log10(Math.max(1e-4, L.W - L.err)))[1]; ink(g, [[x, e0], [x, e1]], 3, { col: PAL.ink, t0: 0, t1: 0 }); g.fillStyle = css(PAL.blue); g.beginPath(); g.arc(x, y, 10, 0, TAU); g.fill(); });
  tag(g, 1400, 160, 'electromagnetism in our 4D world: perimeter law', t, SW(6, 'perimeter'), tA - 0.2, { size: 28 });
  tag(g, 1400, 160, 'confining theory: area law (each square costs the same)', t, tA, 1e9, { size: 28 });
  tag(g, 1400, 1010, 'dots: measured in the simulation · line: the exact 2D answer', t, SW(6, 'toy', 2), 1e9, { size: 26, bg: '#fbe2c0' });
  tag(g, 460, 960, 'the rope again: a sheet of field energy', t, SW(6, 'rope'), 1e9, { size: 30 });
});

deepScene('creutz', SW(6, 'kroyts') - 0.6, 631, (g, t, S) => {
  title(g, 960, 160, '1980 · Michael Creutz: 4D Yang–Mills on a computer', t, SW(6, 'kroyts'), 1e9, 50);
  tag(g, 960, 330, 'the area law appears', t, SW(6, 'appear'), 1e9, { size: 38 });
  tag(g, 560, 520, 'the proton’s mass from the theory\nto within a few percent (2008)', t, SW(6, 'proton'), 1e9, { size: 32 });
  tag(g, 1360, 520, 'glueballs: measured', t, SW(6, 'glueballs'), 1e9, { size: 34 });
  stamp(g, 960, 760, 'THEY SEE THE MASS GAP', t, SW(6, 'gap'), 1e9, { size: 58, col: '#ffcf7a' });
});

board('limits', SW(6, 'catch') - 0.4, 'sage', 641, (g, t, S) => {
  const F0 = latAt(t, S._sh.t0, 3), B1 = blockLat(F0, 32), B2 = blockLat(B1, 16), B3 = blockLat(B2, 8);
  const tZ = SW(6, 'zero'), lv = t < tZ ? 0 : Math.min(3, Math.floor((t - tZ) / 0.7) + 1), lat = [[B3, 4], [B2, 8], [B1, 16], [F0, 32]][lv];
  const box = 600, zout = 1 - 0.45 * E.io2(seg(t, SW(6, 'infinity') - 0.3, SW(6, 'infinity') + 1.6));
  g.save(); g.translate(560, 560); g.scale(zout, zout);
  for (let i = -2; i <= 2; i++) for (let j = -2; j <= 2; j++) { if ((i || j) && zout > 0.98) continue; g.globalAlpha = i || j ? seg(t, SW(6, 'infinity') - 0.3, SW(6, 'infinity') + 1) : 1; drawLattice(g, -box / 2 + i * box, -box / 2 + j * box, box / lat[1], lat[0], lat[1], lat[1], { heat: 0.7, dials: lat[1] <= 8 }); }
  g.restore(); g.globalAlpha = 1;
  tag(g, 1450, 220, 'the real world has no grid', t, SW(6, 'catch'), 1e9, { size: 34 });
  tag(g, 1450, 360, 'shrink the spacing → 0', t, tZ, 1e9, { size: 34 });
  tag(g, 1450, 500, 'grow the box → ∞', t, SW(6, 'infinity'), 1e9, { size: 34 });
  tag(g, 1450, 640, 'blow up?', t, SW(6, 'blow'), 1e9, { size: 32 });
  tag(g, 1450, 760, 'drain away to nothing?', t, SW(6, 'drain'), 1e9, { size: 32 });
  stamp(g, 1450, 900, 'TREND ≠ THEOREM', t, SW(6, 'theorem'), 1e9, { size: 52 });
  title(g, 960, 1020, 'does this theory even exist?', t, SW(6, 'exist'), 1e9, 48, PAL.ink, { shadow: false });
});

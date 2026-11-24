// ================= 3. Gluons that grab each other =================
deepScene('proton', SB(3) - 0.3, 301, (g, t, S) => {
  protonCel(g, 960, 520, 300, t, { field: 0.3, tubes: t > SW(3, 'glue') });
  tag(g, 960, 130, 'inside every proton and neutron: three quarks', t, SW(3, 'quarks'), 1e9, { size: 40 });
  tag(g, 960, 930, '“colour” charge: nothing to do with real colour', t, SW(3, 'colour'), 1e9, { size: 36 });
  tag(g, 960, 1010, 'three kinds, nicknamed red · green · blue', t, SW(3, 'kinds'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('gluons8', SW(3, 'force') - 0.3, 'warm', 311, (g, t, S) => {
  title(g, 960, 130, 'gluons, from the word “glue”', t, SW(3, 'glue'), 1e9, 56, PAL.ink, { shadow: false });
  const pairs = [[QC.r, QC.g], [QC.r, QC.b], [QC.g, QC.r], [QC.g, QC.b], [QC.b, QC.r], [QC.b, QC.g], ['#c07a5a', '#5a8a9a'], ['#9a6aa0', '#7a9a5a']];
  pairs.forEach(([a, b], k) => { const tk = SW(3, 'eight') + k * 0.12; if (t < tk) return; const x = 330 + (k % 4) * 420, y = 360 + Math.floor(k / 4) * 260, u = seg(t, tk, tk + 0.3); g.globalAlpha = u; gluonCoil(g, x - 140, y, x + 140, y, t + k, { col: QC.glu, w: 5, amp: 18 }); particle(g, x - 150, y, 22, a, { glow: false }); particle(g, x + 150, y, 22, b, { glow: false }); g.globalAlpha = 1; });
  tag(g, 960, 880, 'eight kinds, each carrying colour (and anticolour) · schematic', t, SW(3, 'eight') + 1.2, 1e9, { size: 32 });
  tag(g, 960, 990, 'the theory: Yang–Mills, with a symmetry of three colours', t, SW(3, 'symmetry'), 1e9, { size: 34, bg: '#fbe2c0' });
});

deepScene('cross', SW(3, 'twist') - 0.3, 321, (g, t, S) => {
  const T0 = S._sh.t0;
  vignette(g, 500, 520, 820, 620, t, SW(3, 'photon'), gg => {
    gg.fillStyle = '#0e1626'; gg.fillRect(80, 200, 840, 640);
    for (let k = 0; k < 2; k++) { const u = clamp((t - SW(3, 'photon')) / 3.5), pts = []; for (let i = 0; i <= 80; i++) { const s = i / 80; pts.push([lerp(110, 890, s), lerp(k ? 260 : 780, k ? 780 : 260, s) + 12 * Math.sin(s * 50 - (t - T0) * 5)]); } ink(gg, pts.slice(0, Math.max(2, Math.floor(81 * u))), 5, { col: '#ffe9a8', a: 0.9, t0: 0.05, t1: 0.05, seed: k }); }
  }, { label: 'light beams pass straight through' });
  vignette(g, 1420, 520, 820, 620, t, SW(3, 'gluon', 2), gg => {
    gg.fillStyle = '#1a1226'; gg.fillRect(1000, 200, 840, 640);
    for (let k = 0; k < 2; k++) { const u = clamp((t - SW(3, 'gluon', 2)) / 3.5), pts = []; for (let i = 0; i <= 80; i++) { const s = i / 80, sc = s > 0.5 ? (s - 0.5) * 360 * (k ? 1 : -1) : 0; pts.push([lerp(1030, 1810, s), lerp(k ? 260 : 780, k ? 780 : 260, Math.min(s, 0.5)) + (s > 0.5 ? (k ? -1 : 1) * (s - 0.5) * 520 + sc * 0 : 0) + 12 * Math.sin(s * 50 - (t - T0) * 5)]); } ink(gg, pts.slice(0, Math.max(2, Math.floor(81 * u))), 5, { col: QC.glu, a: 0.9, t0: 0.05, t1: 0.05, seed: k + 4 }); }
    if (t > SW(3, 'gluon', 2) + 1.7) { gg.save(); gg.globalCompositeOperation = 'lighter'; glowDot(gg, 1420, 520, 70, '#ffcf7a', 0.6); gg.restore(); }
  }, { label: 'gluons carry colour: they interact' });
  tag(g, 960, 110, 'photons: no charge  ·  gluons: they feel the force they carry', t, SW(3, 'feel'), 1e9, { size: 34 });
});

board('tube', SW(3, 'watch') - 0.3, 'blue', 331, (g, t, S) => {
  const tE = SW(3, 'electric', 3), tQ = SW(3, 'squeezed'), sq = E.io2(seg(t, tQ - 0.3, tQ + 1.4));
  if (!S.lines) S.lines = fieldLines([{ x: 640, y: 540, q: 1 }, { x: 1280, y: 540, q: -1 }], { n: 22 });
  if (t < tQ + 0.2) { g.globalAlpha = 1 - seg(t, tQ - 0.3, tQ + 0.2); drawLines(g, S.lines, seg(t, tE - 0.2, tE + 1.4), { col: PAL.blue }); g.globalAlpha = 1; }
  if (t > tQ - 0.3) fluxTube(g, [640, 540], [1280, 540], 15, sq, { col: '#c98a1e', a: seg(t, tQ - 0.3, tQ + 0.2), lw: 3, phase: t * 2, glow: sq, glowCol: '#ffcf7a' });
  particle(g, 640, 540, 34, QC.r, { label: t > tQ - 0.3 ? 'q' : '+' }); particle(g, 1280, 540, 34, QC.b, { label: t > tQ - 0.3 ? 'q̄' : '−' });
  tag(g, 960, 140, t < tQ - 0.3 ? 'electric charges: lines bulge and spread' : 'colour charges: squeezed into a tube', t, SW(3, 'electric', 3), 1e9, { size: 38 });
  tag(g, 960, 880, 'a cartoon', t, SW(3, 'cartoon'), 1e9, { size: 34 });
  tag(g, 960, 980, 'supercomputer simulations: the tube is real', t, SW(3, 'real', 2), 1e9, { size: 34, bg: '#fbe2c0' });
});

board('rope-plot', SW(3, 'spreading') - 0.3, 'warm', 341, (g, t, S) => {
  const map = axes(g, 260, 900, 1000, 640, { x0: 0, x1: 2, y0: -1.2, y1: 1.6, xl: 'separation (fm)', yl: 'energy (GeV)', fs: 30 });
  const [zx, zy] = map(0, 0); ink(g, [[zx, zy], [map(2, 0)[0], zy]], 2, { col: PAL.ink, a: 0.35, t0: 0, t1: 0 });
  const tS = SW(3, 'spreading'); plot(g, map, r => coulombV(r) * 20, 0.05, 2, seg(t, tS, tS + 1.5), { col: PAL.blue, w: 5 });
  const tR = SW(3, 'rope', 3); if (t > tR) plot(g, map, cornell, 0.12, 2, seg(t, tR, tR + 2), { col: PAL.red, w: 6 });
  tag(g, 1560, 600, 'electric: weaker with distance\n(drawn ×20 to be visible)', t, tS + 0.4, 1e9, { size: 28 });
  tag(g, 1560, 300, 'the rope: energy grows in a straight line\n= the same force at any distance', t, SW(3, 'same', 2), 1e9, { size: 28, bg: '#fbe2c0' });
  tag(g, 760, 140, 'quark–antiquark energy (Cornell form, σ ≈ 0.9 GeV/fm)', t, tR + 0.4, 1e9, { size: 28 });
});

board('tonnes', SW(3, 'enormous') - 0.3, 'sage', 351, (g, t, S) => {
  const tB = SW(3, 'sixteen'), tK = SW(3, 'kilograms');
  const beam = -0.13 * E.out3(seg(t, tB, tB + 0.8));
  // two weights on one beam: the 16-tonne pull dwarfs the 23-kg push
  g.save(); g.translate(960, 560); g.rotate(beam); ink(g, [[-640, 0], [640, 0]], 12, { col: PAL.ink, t0: 0.02, t1: 0.02 });
  if (t > tB) { cels(g, rough([[-620, -360], [-200, -360], [-200, -10], [-620, -10]], 2, 3, 12), { base: '#8a3a26', sh: '#5a2416', sw: 10, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 }); g.font = '700 64px Georgia'; g.fillStyle = '#fff'; g.textAlign = 'center'; g.fillText('16 t', -410, -170); }
  if (t > tK) { cels(g, rough([[380, -70], [460, -70], [460, -10], [380, -10]], 1, 5, 8), { base: '#3a6fd0', line: PAL.ink, lw: 2.4, seed: 5 }); }
  g.restore();
  fillPts(g, [[930, 580], [990, 580], [1020, 680], [900, 680]], css(PAL.ink));
  tag(g, 520, 820, 'the tube’s pull ≈ 16 tonnes (a city bus)', t, SW(3, 'bus'), 1e9, { size: 34 });
  tag(g, 1380, 820, 'electric push, two protons, same distance ≈ 23 kg (a suitcase)', t, SW(3, 'suitcase'), 1e9, { size: 28, bg: '#fbe2c0' });
  tag(g, 960, 150, 'on something a millionth of a billionth of a metre across', t, SW(3, 'millionth'), 1e9, { size: 34 });
});

deepScene('snap', SW(3, 'harder') - 0.3, 361, (g, t, S) => {
  const tH = SW(3, 'harder'), tS = SW(3, 'snaps'), d = 480 + 600 * E.io2(seg(t, tH, tS)), A = [960 - d / 2, 540], B = [960 + d / 2, 540];
  const gap = t > tS ? [0.5 - 0.06 * E.out3(seg(t, tS, tS + 0.5)) - 0.04, 0.5 + 0.06 * E.out3(seg(t, tS, tS + 0.5)) + 0.04] : null;
  fluxTube(g, A, B, 13, 1, { col: '#ffd98a', a: 0.9, lw: 3, w: 22, phase: t * 3, gap, glow: 1 });
  particle(g, A[0], A[1], 34, QC.r); particle(g, B[0], B[1], 34, QC.b);
  if (gap) { const k = seg(t, tS, tS + 0.4), mA = lerp(A[0], B[0], gap[0]), mB = lerp(A[0], B[0], gap[1]); particle(g, mA, 540, 26, QC.b, { a: k }); particle(g, mB, 540, 26, QC.r, { a: k }); g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, 960, 540, 160, '#fff0c0', 0.8 * (1 - seg(t, tS, tS + 1.2))); g.restore(); }
  // energy stored in the tube
  const E0 = Math.min(1, (d - 480) / 600); g.fillStyle = css('#1c2430', 0.9); g.fillRect(660, 840, 600, 34); g.fillStyle = css(mixv('#ffd98a', '#ff6a3a', E0)); g.fillRect(660, 840, 600 * (gap ? 0.15 : E0), 34); inkLoop(g, rough([[660, 840], [1260, 840], [1260, 874], [660, 874]], 1, 3, 8), 2.5, { col: '#e8eef4' });
  tag(g, 960, 930, 'energy in the rope', t, tH + 0.3, 1e9, { size: 30 });
  tag(g, 960, 150, 'pull harder…', t, tH, tS - 0.3, { size: 40 });
  tag(g, 960, 150, 'it snaps: the energy becomes a new quark and antiquark', t, tS, 1e9, { size: 36 });
  tag(g, 960, 290, 'one pair → two pairs · never a lone quark', t, SW(3, 'lone'), 1e9, { size: 36, bg: '#fbe2c0' });
});

deepScene('jets', SW(3, 'that\'s') - 0.3, 371, (g, t, S) => {
  title(g, 960, 150, 'CONFINEMENT', t, SW(3, 'confinement'), 1e9, 96, '#fff1cf', { upright: true, weight: 700 });
  jets(g, 960, 600, 520, t, SW(3, 'smash'), { axes: [-0.35, 2.8, 1.3] });
  tag(g, 960, 960, 'smash protons: out come jets of new particles (schematic)', t, SW(3, 'jets'), 1e9, { size: 34 });
  stamp(g, 1500, 820, 'A PICTURE ≠ A PROOF', t, SW(3, 'proof'), 1e9, { size: 46 });
  tag(g, 420, 330, 'late 1960s: a puzzle', t, SW(3, 'puzzle'), 1e9, { size: 36, bg: '#fbe2c0' });
});

// ================= 4. Two answers to Pauli =================
board('slac', SB(4) - 0.3, 'warm', 401, (g, t, S) => {
  const T0 = S._sh.t0, R = 260, cx = 1100, cy = 560;
  const pr = []; for (let i = 0; i < 40; i++) pr.push([cx + Math.cos(i / 40 * TAU) * R, cy + Math.sin(i / 40 * TAU) * R]);
  g.globalAlpha = 0.6; cels(g, pr, { base: '#e7d9f0', sh: '#cdb9dc', sw: 10, L: LIGHT, line: PAL.ink, lw: 3, seed: 2 }); g.globalAlpha = 1;
  [[QC.r, -80, -60], [QC.g, 90, -40], [QC.b, 0, 90]].forEach(([c, x, y], k) => particle(g, cx + x + 30 * n1((t - T0) * 1.6 + k * 4, 3), cy + y + 30 * n1((t - T0) * 1.4 + k * 7, 5), 26, c, { glow: false }));
  for (let k = 0; k < 4; k++) { const tk = SW(4, 'fired') + k * 0.5; if (t < tk) continue; const u = seg(t, tk, tk + 0.6), y = 420 + k * 90; arrow(g, 200, y, lerp(200, 880, u), y + (u > 0.95 ? (k - 1.5) * 40 : 0), 5, PAL.blue); }
  tag(g, 520, 160, 'Stanford, late 1960s: electrons fired into protons', t, SW(4, 'stanford'), 1e9, { size: 34 });
  tag(g, 1100, 900, 'inside: almost free, like marbles rattling in a bag', t, SW(4, 'marbles'), 1e9, { size: 32 });
  tag(g, 1100, 1000, 'yet nobody could ever pull one out', t, SW(4, 'out'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('two-faces', SW(4, 'free', 2) - 0.3, 'blue', 411, (g, t, S) => {
  title(g, 960, 300, 'free on the inside,\nunbreakable on the outside', t, SW(4, 'free', 2), 1e9, 72, PAL.ink, { shadow: false });
  stamp(g, 960, 620, 'HOW CAN ONE FORCE DO BOTH?', t, SW(4, 'both'), 1e9, { size: 52 });
  tag(g, 960, 840, '1973', t, SW(4, 'nineteen'), 1e9, { size: 60 });
});

board('af-names', SW(4, 'gross') - 0.4, 'warm', 421, (g, t, S) => {
  nameplate(g, 420, 380, 'David Gross', 'Princeton · 32', t, SW(4, 'gross'));
  nameplate(g, 960, 380, 'Frank Wilczek', 'his student · 21', t, SW(4, 'wilcheck'));
  nameplate(g, 1500, 380, 'David Politzer', 'Harvard, on his own', t, SW(4, 'politzer'));
  tag(g, 960, 640, 'how does a Yang–Mills force change with distance, quantum effects included?', t, SW(4, 'calculated'), 1e9, { size: 34 });
});

board('running', SW(4, 'electromagnetism') - 0.4, 'blue', 431, (g, t, S) => {
  // coupling strength vs distance (log scale). Yang-Mills: alpha_s(Q = hbar c / r), one loop. QED: screening (schematic arrow)
  const lx = r => Math.log10(r), map = axes(g, 260, 900, 1250, 640, { x0: -3, x1: 0.4, y0: 0, y1: 0.8, xl: 'distance (fm, log)', yl: 'strength of the force', fs: 30 });
  [-3, -2, -1].forEach(e => { const [x] = map(e, 0); g.font = '500 26px Georgia'; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.fillText(`10${['⁻³', '⁻²', '⁻¹'][e + 3]}`, x, 940); });
  const tQ = SW(4, 'electromagnetism'); tag(g, 1300, 200, 'electromagnetism: stronger up close\n(the charge’s cloud screens it from far away)', t, tQ, SW(4, 'mills', 2) - 0.2, { size: 28 });
  const tW = SW(4, 'weaker'); if (t > tW - 0.6) plot(g, map, e => alphaS(HBARC / Math.pow(10, e), 3, 0.25), -3, -0.25, seg(t, tW - 0.6, tW + 2.2), { col: PAL.red, w: 7, clipY: 260 });
  tag(g, 520, 760, 'up close: weaker, fading towards zero', t, tW, 1e9, { size: 30 });
  tag(g, 1300, 330, 'further out: it grows', t, SW(4, 'grows'), 1e9, { size: 30, bg: '#fbe2c0' });
  title(g, 960, 120, 'asymptotic freedom', t, SW(4, 'asymptotic'), 1e9, 66, PAL.ink, { shadow: false });
  tag(g, 1300, 470, 'one-loop running, three light quarks (illustrative)', t, tW + 1, 1e9, { size: 24 });
});

board('nobel', SW(4, 'nobel') - 0.4, 'sage', 441, (g, t, S) => {
  docPage(g, 960, 420, 900, 420, t, SW(4, 'nobel') - 0.2, { head: 'Nobel Prize in Physics · 2004', lines: 3 });
  tag(g, 960, 470, 'Gross · Politzer · Wilczek: “asymptotic freedom”', t, SW(4, 'nobel') + 0.3, 1e9, { size: 34, card: false });
  stamp(g, 960, 780, 'REMEMBER: DAVID GROSS', t, SW(4, 'remember'), 1e9, { size: 58 });
  tag(g, 960, 960, '53 years later: chair of the panel that judged the machine', t, SW(4, 'chair'), 1e9, { size: 34, bg: '#fbe2c0' });
});

deepScene('higgs', SW(4, 'answers') - 0.4, 451, (g, t, S) => {
  title(g, 960, 110, 'two completely different answers to Pauli', t, SW(4, 'answers'), 1e9, 52);
  vignette(g, 500, 540, 800, 640, t, SW(4, 'weak'), gg => {
    gg.fillStyle = '#f3e8d2'; gg.fillRect(100, 220, 800, 640);
    const map = (p, v) => [500 + p * 240, 760 - v * 380]; const pts = []; for (let i = 0; i <= 80; i++) { const p = -1.45 + i / 80 * 2.9; pts.push(map(p, higgsV(p))); } ink(gg, pts, 6, { col: PAL.ink, t0: 0.02, t1: 0.02, seed: 2 });
    const tb = SW(4, 'higgs'), p = lerp(0.02, 1, E.out3(seg(t, tb, tb + 1.6))); const [bx, by] = map(p, higgsV(p)); particle(gg, bx, by - 22, 22, PAL.gold, { glow: false });
    gg.font = 'italic 600 30px Georgia'; gg.fillStyle = css(PAL.ink); gg.textAlign = 'center'; gg.fillText('the Higgs field: 1964 → found at CERN, 2012', 500, 290);
  }, { label: 'weak force: a new field gives the carriers mass' });
  vignette(g, 1420, 540, 800, 640, t, SW(4, 'strong'), gg => {
    gg.fillStyle = '#12233d'; gg.fillRect(1020, 220, 800, 640); gluonCoil(gg, 1220, 560, 1620, 560, t, { glow: true, amp: 24 });
    gg.font = '700 56px Georgia'; gg.fillStyle = '#fff1cf'; gg.textAlign = 'center'; gg.fillText('mass = 0', 1420, 400);
  }, { label: 'strong force: no extra field, gluons stay massless' });
  stamp(g, 960, 960, 'MASS OUT OF NOTHING = THE MASS GAP', t, SW(4, 'gap'), 1e9, { size: 46, col: '#ffcf7a' });
});

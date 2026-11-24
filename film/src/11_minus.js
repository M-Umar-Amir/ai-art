// ================= 3. One minus sign =================
board('twins3', SB(3) - 0.3, 'warm', 301, (g, t, S) => {
  numGrid(g, 520, 520, 200, [['a', 'b'], ['c', 'd']], { on: (r, c) => t > SW(3, 'determinant') && ((r === c && Math.floor(t * 1.2) % 2 === 0) || (r !== c && Math.floor(t * 1.2) % 2 === 1)) });
  writeText(g, 'det = ad − bc', 1060, 400, 92, t, SW(3, 'determinant'), 0.8, { upright: true, col: '#2f5f8f' });
  writeText(g, 'perm = ad + bc', 1060, 640, 92, t, SW(3, 'permanent'), 0.8, { upright: true, col: '#b8361f' });
}, { punch: false });

board('count3', SW(3, 'six') - 0.4, 'blue', 311, (g, t, S) => {
  const steps = [['3 × 3', '6', SW(3, 'six')], ['4 × 4', '24', SW(3, 'twenty-four')], ['10 × 10', '3,628,800', SW(3, 'million')], ['100 × 100', '158 digits', SW(3, 'digits')]];
  steps.forEach(([a, b, tk], i) => { tag(g, 560, 230 + i * 190, a, t, tk - 0.2, 1e9, { size: 50 }); tag(g, 1240, 230 + i * 190, b + ' products', t, tk, 1e9, { size: 56, bg: i === 3 ? '#fbe2c0' : PAL.card }); });
  tag(g, 900, 980, 'determinant: half get a minus sign · permanent: all added', t, SW(3, 'added'), 1e9, { size: 32 });
}, { punch: false });

glShot('perms3d', SW(3, 'ten') - 0.3, T => glPerms(T, { p0: SW(3, 'hundred') - 0.4 }), { over: (g, t, S) => {
  tag(g, 960, 110, 'a 4 × 4 grid: all 24 ways to pick one per row and column', t, S._sh.t0 + 0.3, SW(3, 'hundred') - 0.4, { size: 32 });
  tag(g, 960, 110, '100 × 100: a number of products with 158 digits', t, SW(3, 'hundred'), 1e9, { size: 36 });
  credit(g, '3D: computed scene');
} });

board('dance3', SW(3, 'abstract') - 0.3, 'sage', 321, (g, t, S) => {
  const Z = [[1, 1, 0, 0], [0, 1, 1, 0], [1, 0, 1, 1], [0, 0, 1, 1]], good = PERM4.filter(p => p.every((c, r) => Z[r][c]));
  const tD = SW(3, 'dance'), k = t > tD ? good[Math.floor((t - tD) * 1.5) % good.length] : null;
  numGrid(g, 470, 540, 120, Z, { on: (r, c) => k && k[r] === c });
  const L = i => [980, 260 + i * 160], R = j => [1560, 260 + j * 160];
  for (let i = 0; i < 4; i++) for (let j = 0; j < 4; j++) if (Z[i][j]) ink(g, [L(i), R(j)], 3, { col: PAL.ink, a: 0.25 * seg(t, tD, tD + 0.5), t0: 0, t1: 0 });
  if (k) for (let i = 0; i < 4; i++) ink(g, [L(i), R(k[i])], 7, { col: PAL.red, a: 0.9, t0: 0.05, t1: 0.05 });
  for (let i = 0; i < 4; i++) { if (t < tD) break; particle(g, ...L(i), 26, '#3a6fd0', { glow: false, label: 'ABCD'[i] }); particle(g, ...R(i), 26, '#d8452e', { glow: false, label: '1234'[i] }); }
  tag(g, 1270, 960, `ways to pair everyone up = permanent = ${good.length}`, t, SW(3, 'floor'), 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 470, 140, '1 = willing to dance', t, SW(3, 'willing'), 1e9, { size: 36 });
});

board('elim3', SW(3, 'anyone') - 0.3, 'warm', 331, (g, t, S) => {
  const M = [[[2, 1, 3], [4, 5, 6], [2, 7, 1]], [[2, 1, 3], [0, 3, 0], [0, 6, -2]], [[2, 1, 3], [0, 3, 0], [0, 0, -2]]];
  const ts = [SW(3, 'subtract'), SW(3, 'triangular')], st = t < ts[0] ? 0 : t < ts[1] ? 1 : 2;
  numGrid(g, 560, 540, 150, M[st], { on: (r, c) => st === 2 && r === c, onCol: '#2f5f8f' });
  tag(g, 560, 160, ['a shortcut you may have learned in school', 'subtract multiples of one row from another', 'until the grid is triangular'][st], t, S._sh.t0 + 0.2, 1e9, { size: 36 });
  tag(g, 1380, 360, 'det = 2 × 3 × (−2) = −12', t, ts[1] + 0.4, 1e9, { size: 50 });
  tag(g, 1380, 560, 'minus signs let terms cancel unseen', t, SW(3, 'cancel'), 1e9, { size: 38 });
  tag(g, 1380, 760, 'work grows like n³', t, SW(3, 'cube'), 1e9, { size: 46, bg: '#d9f0ff' });
});

board('ryser3', SW(3, 'nothing') - 0.3, 'blue', 341, (g, t, S) => {
  tag(g, 960, 130, 'the permanent: no minus signs, nothing cancels · row tricks change its value', t, SW(3, 'nothing'), 1e9, { size: 32 });
  tag(g, 520, 300, 'Herbert Ryser · 1963', t, SW(3, 'ryezer'), 1e9, { size: 44 });
  tag(g, 520, 450, '≈ 2ⁿ steps', t, SW(3, 'power'), 1e9, { size: 60, bg: '#fbe2c0' });
  const tD = SW(3, 'doubles'); for (let k = 0; k < 10; k++) { const tk = tD - 1.2 + k * 0.15; if (t < tk) continue; const h = Math.pow(2, k) / 512 * 620 * E.out3(seg(t, tk, tk + 0.3)); cels(g, [[960 + k * 80, 930 - h], [1020 + k * 80, 930 - h], [1020 + k * 80, 930], [960 + k * 80, 930]], { base: k === 9 ? '#b8361f' : '#d79a2b', line: PAL.ink, lw: 2, seed: k }); }
  tag(g, 1340, 260, 'every extra row doubles the work', t, tD, 1e9, { size: 36 });
});

board('mod23', SW(3, 'strangest') - 0.3, 'sage', 351, (g, t, S) => {
  const tz = SW(3, 'zero');
  for (const [lab, a] of [['0', -Math.PI / 2], ['1', Math.PI / 2]]) { const x = 520 + Math.cos(a) * 200, y = 520 + Math.sin(a) * 200; particle(g, x, y, 60, lab === '0' ? '#3a6fd0' : '#d79a2b', { label: lab, glow: false }); }
  g.strokeStyle = css(PAL.ink, 0.5); g.lineWidth = 5; g.beginPath(); g.arc(520, 520, 200, 0, TAU); g.stroke();
  if (t > tz) { const u = seg(t, tz, tz + 1.2), a = -Math.PI / 2 + u * TAU; arrow(g, 520, 520, 520 + Math.cos(a) * 150, 520 + Math.sin(a) * 150, 7, PAL.red); }
  writeText(g, '1 + 1 = 0', 1080, 300, 100, t, tz, 0.7, { upright: true, col: PAL.ink });
  tag(g, 1300, 450, 'computers use it all the time (bits)', t, SW(3, 'computers'), 1e9, { size: 34 });
  writeText(g, '−1 = +1', 1080, 600, 100, t, SW(3, 'minus', 5), 0.7, { upright: true, col: PAL.red });
  stamp(g, 1340, 820, 'PERMANENT = DETERMINANT', t, SW(3, 'formula', 2) - 0.3, 1e9, { size: 46 });
  tag(g, 1340, 960, '… so there, the permanent is easy', t, SW(3, 'easy'), 1e9, { size: 36, bg: '#fbe2c0' });
});
kinShot('lives3', SW(3, 'lives') - 0.4, [['THE WHOLE DIFFICULTY', '#ffffff'], ['LIVES IN ONE MINUS SIGN', '#ff6a55']], { bg: '#0b0b10', size: 120, stagger: 0.6,
  draw: (g, t, S) => tag(g, 960, 900, 'and nature cares about it very much', t, SW(3, 'nature'), 1e9, { size: 42 }) });

// ================= 4. The minus sign in nature =================
board('types4', SB(4) - 0.3, 'warm', 401, (g, t, S) => {
  print(g, 'fermi', 520, 470, 560, t, SW(4, 'fermions'), { caption: 'FERMIONS · Enrico Fermi', bw: true });
  print(g, 'bose', 1400, 470, 560, t, SW(4, 'bosons'), { caption: 'BOSONS · Satyendra Nath Bose', bw: true, rot: 0.04 });
  tag(g, 520, 920, 'electrons', t, SW(4, 'electrons'), 1e9, { size: 40 });
  tag(g, 1400, 920, 'photons: particles of light', t, SW(4, 'photons'), 1e9, { size: 40 });
}, { punch: false });

flat('swap4', SW(4, 'swap') - 0.3, '#10131c', (g, t, S) => {
  const row = (y, col, sign, tIn, label) => { if (t < tIn) return; const u = E.io2(seg(t, tIn + 0.3, tIn + 1.3)), ax = lerp(700, 1220, u), bx = lerp(1220, 700, u), lift = Math.sin(u * Math.PI) * 70;
    particle(g, ax, y - lift, 42, col); particle(g, bx, y + lift, 42, col);
    kin(g, [[sign, sign === '−' ? '#ff6a55' : '#7fe0a0']], t, tIn + 1.3, { size: 140, x: 1500, y });
    tag(g, 380, y, label, t, tIn, 1e9, { size: 40 }); };
  row(360, '#3a8cff', '−', SW(4, 'swap'), 'two identical fermions');
  row(720, '#ffcf5a', '+', SW(4, 'swap', 2), 'two identical bosons');
  tag(g, 960, 960, 'that’s the whole difference', t, SW(4, 'difference'), 1e9, { size: 44, bg: '#fbe2c0' });
}, { punch: false });

board('slater4', SW(4, 'describe') - 0.3, 'blue', 421, (g, t, S) => {
  tag(g, 960, 120, 'many electrons → a SLATER DETERMINANT', t, SW(4, 'slater'), 1e9, { size: 42 });
  const tI = SW(4, 'identical', 2);
  numGrid(g, 580, 540, 170, [['ψ₁(1)', 'ψ₁(2)', 'ψ₁(3)'], ['ψ₂(1)', 'ψ₂(2)', 'ψ₂(3)'], [t > tI ? 'ψ₁(1)' : 'ψ₃(1)', t > tI ? 'ψ₁(2)' : 'ψ₃(2)', t > tI ? 'ψ₁(3)' : 'ψ₃(3)']], { fs: 44, on: (r, c) => t > tI && (r === 0 || r === 2), onCol: '#b8361f' });
  tag(g, 1380, 300, 'two identical rows → det = 0', t, tI, 1e9, { size: 40 });
  tag(g, 1380, 480, 'swap two rows: the sign flips', t, SW(4, 'swapping'), 1e9, { size: 38 });
  tag(g, 1380, 640, 'identical rows: swapping changes nothing', t, SW(4, 'changes', 2), 1e9, { size: 36 });
  writeText(g, 'D = −D  ⇒  D = 0', 1380, 830, 72, t, SW(4, 'negative'), 0.8, { upright: true, col: PAL.red, align: 'center' });
});
pshot('pauli4', SW(4, "pauli's") - 0.4, 'pauli', { bw: true, fit: true, from: [0.5, 0.4, 0.95], to: [0.5, 0.4, 1.05], tags: [[960, 130, "Pauli's exclusion principle · 1925", SW(4, "pauli's"), { size: 44 }], [960, 950, 'the same Pauli as in our last video', SW(4, 'pauli'), { size: 40, bg: '#fbe2c0' }]] });
board('shells4', SW(4, 'shells') - 0.4, 'sage', 441, (g, t, S) => {
  particle(g, 700, 540, 50, '#b8361f', { label: '+' });
  [140, 240, 340].forEach((R, k) => { g.strokeStyle = css(PAL.ink, 0.35); g.lineWidth = 3; g.beginPath(); g.arc(700, 540, R, 0, TAU); g.stroke(); const n = [2, 8, 4][k]; for (let i = 0; i < n; i++) { const a = i / n * TAU + t * (0.6 - k * 0.15); particle(g, 700 + Math.cos(a) * R, 540 + Math.sin(a) * R, 16, '#3a8cff', { glow: false }); } });
  tag(g, 1420, 330, 'atoms have shells', t, SW(4, 'shells'), 1e9, { size: 42 });
  tag(g, 1420, 520, 'chemistry works', t, SW(4, 'chemistry'), 1e9, { size: 42 });
  tag(g, 1420, 710, 'you don’t sink through the floor', t, SW(4, 'floor'), 1e9, { size: 40, bg: '#fbe2c0' });
});
pshot('laser4', SW(4, 'bosons', 3) - 0.3, 'laser_sky', { from: [0.5, 0.55, 1.0], to: [0.5, 0.4, 1.15], tags: [[960, 140, 'bosons: no minus sign → the permanent', SW(4, 'mathematics', 2), { size: 42 }], [960, 900, 'bosons bunch up: that’s how a laser works', SW(4, 'laser'), { size: 40, bg: '#fbe2c0' }]] });

glShot('hom3d', SW(4, 'beam') - 0.6, T => glHOM(T, { f0: SW(4, 'identical', 5) - 0.3 }), { over: (g, t, S) => {
  const ferm = t >= SW(4, 'identical', 5) - 0.3;
  tag(g, 960, 100, ferm ? 'two identical FERMIONS: always leave apart (determinant)' : 'two identical PHOTONS: always leave together', t, S._sh.t0 + 0.4, 1e9, { size: 36, bg: ferm ? '#d9f0ff' : '#fbe2c0' });
  tag(g, 430, 930, '50 : 50 beam splitter  U = (1/√2)·[[1, 1], [1, −1]]', t, SW(4, 'beam'), 1e9, { size: 28 });
  tag(g, 1560, 860, 'perm U = ½(1·(−1) + 1·1) = 0', t, SW(4, 'exactly'), 1e9, { size: 32, bg: '#fbe2c0' });
  tag(g, 1560, 960, '|det U| = 1', t, SW(4, 'apart') - 0.6, 1e9, { size: 32, bg: '#d9f0ff' });
  tag(g, 1560, 760, 'Hong–Ou–Mandel · 1987', t, SW(4, 'hong'), 1e9, { size: 32 });
  credit(g, '3D: computed scene · ideal case');
} });
board('aa4', SW(4, 'scale') - 0.3, 'warm', 461, (g, t, S) => {
  print(g, 'aaronson', 460, 470, 420, t, SW(4, 'aaronson'), { caption: 'Scott Aaronson' });
  tag(g, 460, 860, '+ Alex Arkhipov · 2011', t, SW(4, 'arkeepov'), 1e9, { size: 36 });
  // a maze of mirrors (schematic mesh of beam splitters)
  for (let i = 0; i < 6; i++) for (let j = 0; j <= i; j++) { const x = 1000 + i * 120, y = 540 + (j - i / 2) * 120; g.save(); g.translate(x, y); g.rotate(Math.PI / 4); g.fillStyle = css('#9fc8ff', 0.5 * seg(t, SW(4, 'maze') + i * 0.1, SW(4, 'maze') + i * 0.1 + 0.3)); g.fillRect(-34, -6, 68, 12); g.restore(); }
  tag(g, 1300, 150, 'many photons through a maze of mirrors', t, SW(4, 'maze'), 1e9, { size: 36 });
  tag(g, 1300, 930, 'probabilities set by permanents of big grids', t, SW(4, 'probabilities'), 1e9, { size: 34, bg: '#fbe2c0' });
});
dshot('bsdoc4', SW(4, 'truly') - 0.4, 'bosonsamp', { path: [[0, 'all'], [SW(4, 'truly') + 0.3, 'The Computational Complexity of Linear Optics']], hl: [['The Computational Complexity of Linear Optics', SW(4, 'truly') + 0.8], ['permanent', SW(4, 'sampling')]], src: 'arXiv:1011.3245 (Aaronson & Arkhipov)' });
clipShot('photons4', SW(4, 'twenty', 2) - 0.3, 'photons', { draw: (g, t, S) => {
  tag(g, 960, 200, '2020 · Jiuzhang (USTC, China): a version of boson sampling', t, SW(4, 'jyohjahng') - 1.2, 1e9, { size: 36 });
} });
kinShot('bet4', SW(4, 'formulas') - 0.4, [['A BET ABOUT LIGHT', '#ffcf5a']], { bg: '#06070b', size: 150,
  draw: (g, t, S) => kin(g, [['47 YEARS. NO PROOF.', '#ffffff']], t, SW(4, 'close') - 0.6, { size: 90, y: 820 }) });

// ================= 5. Forty-seven years of nothing =================
flat('fog5', SB(5) - 0.3, '#0e1016', (g, t, S) => {
  const T0 = S._sh.t0;
  for (let k = 0; k < 40; k++) { const x = 100 + hash(k, 1) * 1720, y = 120 + hash(k, 2) * 820, a = 0.08 + 0.18 * hash(k, 3) * seg(t, T0 + k * 0.05, T0 + k * 0.05 + 0.5); g.strokeStyle = `rgba(150,180,255,${a})`; g.lineWidth = 2; for (let e = 0; e < 4; e++) { g.beginPath(); g.moveTo(x, y); g.lineTo(x + (hash(k * 9 + e, 4) - 0.5) * 160, y - 40 - hash(k * 7 + e, 5) * 80); g.stroke(); } g.fillStyle = `rgba(255,210,120,${a * 2})`; g.beginPath(); g.arc(x, y, 6, 0, TAU); g.fill(); }
  kin(g, [['RULE OUT EVERY', '#ffffff'], ['CLEVER TRICK', '#ffcf5a']], t, SW(5, 'clever'), { size: 110, stagger: 0.4 });
  tag(g, 960, 900, 'including circuits nobody has imagined', t, SW(5, 'imagined'), 1e9, { size: 40 });
}, { punch: false });
board('warn5', SW(5, 'warning') - 0.3, 'warm', 511, (g, t, S) => {
  title(g, 960, 150, 'the determinant is the warning', t, SW(5, 'warning'), 1e9, 64, PAL.ink, { shadow: false });
  tag(g, 560, 400, 'det: n! products', t, SW(5, 'paper'), 1e9, { size: 44 }); tag(g, 1360, 400, 'perm: n! products', t, SW(5, 'paper'), 1e9, { size: 44 });
  stamp(g, 560, 600, 'SHORTCUT EXISTS', t, SW(5, 'shortcut'), 1e9, { size: 44, col: '#3f7a4a' });
  tag(g, 960, 820, 'a proof for the permanent must fail for the determinant', t, SW(5, 'explain'), 1e9, { size: 38, bg: '#fbe2c0' });
});
board('strassen5', SW(5, 'turn') - 0.4, 'blue', 521, (g, t, S) => {
  print(g, 'strassen', 400, 500, 640, t, SW(5, 'strassen'), { caption: 'Volker Strassen' });
  tag(g, 1240, 170, '1969: 2 × 2 matrices with 7 multiplications, not 8', t, SW(5, 'seven'), 1e9, { size: 34 });
  for (let k = 0; k < 8; k++) { const x = 900 + k * 100, y = 360, gone = k === 7 && t > SW(5, 'instead'); g.save(); g.globalAlpha = seg(t, SW(5, 'seven') + k * 0.06, SW(5, 'seven') + k * 0.06 + 0.2); particle(g, x, y, 32, gone ? '#999999' : '#d79a2b', { label: '×', glow: !gone }); if (gone) { ink(g, [[x - 40, y - 40], [x + 40, y + 40]], 7, { col: PAL.red, t0: 0, t1: 0 }); } g.restore(); }
  tag(g, 1240, 560, 'applied over and over: huge matrix products get faster', t, SW(5, 'faster'), 1e9, { size: 32 });
  tag(g, 1240, 760, 'so why not a shortcut for the permanent?', t, SW(5, 'shortcut', 2), 1e9, { size: 38, bg: '#fbe2c0' });
});
board('harvey5', SW(5, 'ignorance') - 0.3, 'sage', 531, (g, t, S) => {
  title(g, 960, 150, 'our ignorance runs deep', t, SW(5, 'ignorance'), 1e9, 66, PAL.ink, { shadow: false });
  writeText(g, '7 412 593 × 8 830 261', 960, 380, 80, t, SW(5, 'multiplying'), 0.9, { upright: true, align: 'center' });
  tag(g, 960, 560, '2019 · David Harvey & Joris van der Hoeven: about n log n steps', t, SW(5, 'harvey'), 1e9, { size: 36 });
  tag(g, 960, 720, 'believed best possible', t, SW(5, 'believe'), 1e9, { size: 40 });
  stamp(g, 960, 880, 'NOBODY CAN PROVE IT', t, SW(5, 'nobody', 2), 1e9, { size: 52 });
});
board('canyon5', SW(5, 'unrestricted') - 0.3, 'warm', 541, (g, t, S) => {
  const map = axes(g, 260, 900, 1300, 680, { x0: 2, x1: 64, y0: 0, y1: 66, xl: 'grid size m', yl: 'steps (log₂ scale)', fs: 30 });
  const fl = m => Math.log2(m * m * Math.log2(m)), ce = m => m + Math.log2(m);
  const tF = SW(5, 'logarithm'), tC = SW(5, 'canyon');
  if (t > tF - 0.5) plot(g, map, fl, 2, 64, seg(t, tF - 0.5, tF + 1), { col: '#2f5f8f', w: 6 });
  if (t > tC - 1.2) plot(g, map, ce, 2, 64, seg(t, tC - 1.2, tC), { col: PAL.red, w: 6 });
  if (t > tC) { g.save(); g.globalAlpha = 0.25 * seg(t, tC, tC + 0.8); g.fillStyle = css(PAL.gold); g.beginPath(); for (let m = 2; m <= 64; m += 1) { const [x, y] = map(m, ce(m)); m === 2 ? g.moveTo(x, y) : g.lineTo(x, y); } for (let m = 64; m >= 2; m -= 1) { const [x, y] = map(m, fl(m)); g.lineTo(x, y); } g.fill(); g.restore(); }
  tag(g, 1520, 820, 'proved floor: ~ m² log m (Strassen; Baur & Strassen)', t, tF, 1e9, { size: 28 });
  tag(g, 1520, 220, 'best known method: ~ 2ᵐ (Ryser)', t, tC - 1, 1e9, { size: 30 });
  tag(g, 900, 420, 'THE CANYON · 40+ years', t, tC + 0.2, 1e9, { size: 44, bg: '#fbe2c0' });
});
board('hands5', SW(5, 'hands') - 0.3, 'blue', 551, (g, t, S) => {
  tag(g, 520, 300, 'no subtraction (monotone)', t, SW(5, 'subtraction'), 1e9, { size: 40 });
  tag(g, 520, 480, 'only a few layers deep', t, SW(5, 'layers'), 1e9, { size: 40 });
  tag(g, 1340, 390, '2021 · Limaye, Srinivasan, Tavenas:\nany fixed depth', t, SW(5, 'leemahyeh'), 1e9, { size: 36, bg: '#fbe2c0' });
  stamp(g, 960, 820, 'UNRESTRICTED: NOTHING', t, SW(5, 'unrestricted', 2), 1e9, { size: 56 });
});
board('walls5', SW(5, 'walls') - 0.3, 'warm', 561, (g, t, S) => {
  print(g, 'razborov', 520, 450, 360, t, SW(5, 'razborov'), { caption: 'Alexander Razborov' });
  tag(g, 560, 820, '+ Steven Rudich · “natural proofs”', t, SW(5, 'natural'), 1e9, { size: 30 });
  tag(g, 1340, 330, '2001 · Mulmuley & Sohoni:\ngeometric complexity theory', t, SW(5, 'moolmoolay'), 1e9, { size: 36 });
  tag(g, 1340, 560, 'symmetry + algebraic geometry', t, SW(5, 'symmetry'), 1e9, { size: 34 });
  tag(g, 1340, 760, 'beautiful mathematics · no proof (yet)', t, SW(5, 'beautiful'), 1e9, { size: 36, bg: '#fbe2c0' });
});
kinShot('clock5', SW(5, 'state') - 0.3, [['20:04 · LONDON', '#ffffff'], ['MONDAY 23 NOVEMBER 2026', '#ffcf5a']], { bg: '#0b0b10', size: 110, stagger: 1.0, delay: 0.3,
  draw: (g, t, S) => tag(g, 960, 900, 'a new folder appears on GitHub', t, SW(5, 'folder'), 1e9, { size: 44 }) });

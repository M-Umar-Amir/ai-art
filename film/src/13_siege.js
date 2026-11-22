// ================= 5. A century of siege =================
deepScene('maddening', SB(5) - 0.3, 501, (g, t, S) => {
  const map = stripPlane(g, 620, 120, 680, 860, { s0: -0.25, s1: 1.25, t0: 0, t1: 60, ink: '#e8eef4', lineCol: '#ffcf7a', stripCol: '#6fa8dc' });
  const n = Math.floor(lerp(0, 14, seg(t, SW(5, 'more'), SW(5, 'more') + 3)));
  ZEROS.slice(0, n).forEach(gm => { const [x, y] = map(0.5, gm); zeroLantern(g, x, y, 8); });
  tag(g, 1600, 300, 'off the line: none found', t, SW(5, 'single'), 1e9, { size: 34 });
  tag(g, 1600, 460, 'on the line: more and more', t, SW(5, 'more'), 1e9, { size: 34 });
  stamp(g, 1600, 660, 'PROVES NOTHING', t, SW(5, 'anything'), 1e9, { size: 46, col: '#ff8a5a' });
});

board('siegel', SW(5, 'computers') - 0.3, 'warm', 511, (g, t, S) => {
  tag(g, 520, 200, '1932 · Carl Ludwig Siegel', t, SW(5, 'siegel'), 1e9, { size: 40 });
  tag(g, 520, 340, "Riemann's private notes:\nan unpublished formula", t, SW(5, 'notes'), 1e9, { size: 34 });
  tag(g, 1380, 220, 'the Riemann–Siegel formula', t, SW(5, 'formula', 2), 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 960, 560, rollNum(t, SW(5, 'exploded') - 0.5, SW(5, 'twelve') + 0.5, 0, 12e12) + '\nzeros checked: all on the line', t, SW(5, 'exploded') - 0.5, 1e9, { size: 64 });
  tag(g, 960, 800, 'Platt & Trudgian, 2021', t, SW(5, 'platt'), 1e9, { size: 36 });
  stamp(g, 1500, 900, 'NOT ONE EXCEPTION', t, SW(5, 'exception'), 1e9, { size: 44, col: PAL.teal });
});

board('late-fail', SW(5, 'nothing') - 0.5, 'blue', 521, (g, t, S) => {
  const tS = SW(5, 'infinitely'), map = axes(g, 200, 760, 1500, 500, { x0: 0, x1: 1, y0: -1, y1: 1, xl: 'how far you check →', a: seg(t, S._sh.t0, S._sh.t0 + 0.5), fs: 30 });
  ink(g, [map(0, 0), map(1, 0)], 2, { col: PAL.ink, a: 0.4, t0: 0, t1: 0 });
  if (t > SW(5, 'cases')) plot(g, map, x => x < 0.82 ? -0.55 + 0.12 * Math.sin(x * 40) * (1 - x) : -0.55 + (x - 0.82) * 6, 0, 0.95, seg(t, SW(5, 'cases'), SW(5, 'fails') + 0.5), { col: PAL.red, w: 6, clipY: 260 });
  tag(g, 600, 900, 'a pattern holds… for an absurdly long stretch', t, SW(5, 'stretches'), 1e9, { size: 34 });
  tag(g, 1460, 320, 'then fails', t, SW(5, 'fails'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 1500, 980, 'schematic · not to scale', t, SW(5, 'cases'), 1e9, { size: 24 });
  title(g, 960, 120, '12 trillion of infinitely many', t, SW(5, 'nothing'), 1e9, 54, PAL.ink, { shadow: false });
});

board('siege-meter', SW(5, 'proof') - 0.4, 'sage', 531, (g, t, S) => {
  title(g, 960, 120, 'what fraction of zeros are PROVED to be on the line?', t, SW(5, 'proof'), 1e9, 46, PAL.ink, { shadow: false });
  const keys = [[SW(5, 'hardy'), 0.004], [SW(5, 'selberg'), 0.03], [SW(5, 'levinson'), 0.3434], [SW(5, 'conrey'), 0.4088], [SW(5, 'team'), 5 / 12]];
  let v = 0; for (let i = 0; i < keys.length; i++) if (t > keys[i][0]) v = lerp(i ? keys[i - 1][1] : 0, keys[i][1], E.out3(seg(t, keys[i][0], keys[i][0] + 1.2)));
  fracMeter(g, 220, 520, 1400, v, [[0.004, '1914 Hardy:\ninfinitely many', SW(5, 'hardy'), true], [0.3333, '1974\n> 1/3', SW(5, 'levinson'), false], [0.4, '1989\n> 2/5', SW(5, 'conrey'), true], [5 / 12, '2020\n≈ 5/12', SW(5, 'team'), false]], t);
  tag(g, 400, 300, '1942 Selberg: a positive fraction', t, SW(5, 'selberg'), SW(5, 'levinson'), { size: 30 });
  tag(g, 1300, 780, 'a century: a few metres up the mountain', t, SW(5, 'mountain'), 1e9, { size: 36 });
  stamp(g, 1300, 940, 'NOT EVEN HALF', t, SW(5, 'half'), 1e9, { size: 46 });
}, { cam: { z0: 1.0, z1: 1.03 } });

board('average', SW(5, 'averaging') - 0.4, 'warm', 541, (g, t, S) => {
  // most dots behave; the proof has to rule out the one that might not
  for (let i = 0; i < 300; i++) { const x = 260 + (i % 30) * 46, y = 240 + Math.floor(i / 30) * 52; fillPts(g, [[x - 9, y - 9], [x + 9, y - 9], [x + 9, y + 9], [x - 9, y + 9]], css(i === 187 && t > SW(5, 'single', 2) ? PAL.red : PAL.teal, 0.85)); }
  stamp(g, 960, 860, 'AVERAGE ≠ ALL', t, SW(5, 'averages') + 0.2, 1e9, { size: 64 });
  tag(g, 1600, 1000, 'missing: an entire idea', t, SW(5, 'idea'), 1e9, { size: 34, bg: '#fbe2c0' });
});

// ================= 6. The physics rhyme =================
scene('ias-tea', SB(6) - 0.3, () => ({ pl: teaPlate(611) }), (g, l, t, S) => {
  const z = kf(t, [[S._sh.t0, 1.0], [S._sh.t1, 1.08, E.io2]]); g.save(); cam(g, 960, 540, z); plateDraw(g, S.pl); g.restore();
  tag(g, 960, 120, 'Princeton · Institute for Advanced Study · spring 1972', t, SW(6, 'spring'), 1e9, { size: 36 });
  tag(g, 420, 860, 'Hugh Montgomery\nmathematician', t, SW(6, 'montgomery'), 1e9, { size: 36 });
  tag(g, 1500, 860, 'Freeman Dyson\nphysicist', t, SW(6, 'dyson'), 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 960, 300, 'the zeros seem to repel each other', t, SW(6, 'repel'), SW(6, 'tea') - 0.3, { size: 40 });
  tag(g, 960, 300, 'same formula as uranium energy levels', t, SW(6, 'uranium'), 1e9, { size: 40 });
});

board('spacing', SW(6, 'side') - 0.6, 'blue', 621, (g, t, S) => {
  const tS = SW(6, 'side'), sp = S.sp, bins = new Array(24).fill(0), dx = 3 / 24;
  for (const s of sp) { const k = Math.floor(s / dx); if (k < 24) bins[k] += 1 / (sp.length * dx); }
  const map = axes(g, 240, 920, 1300, 700, { x0: 0, x1: 3, y0: 0, y1: 1.1, xl: 'gap between neighbouring zeros (normalised)', yl: 'how often', a: seg(t, tS - 0.3, tS + 0.2), fs: 28 });
  if (t > tS) histo(g, map, bins, 0, dx, seg(t, tS, tS + 2), PAL.blue);
  if (t > SW(6, 'right')) plot(g, map, wigner, 0, 3, seg(t, SW(6, 'right'), SW(6, 'right') + 2), { col: PAL.red, w: 7, n: 200 });
  tag(g, 1460, 260, `bars: first ${ZEROS.length.toLocaleString('en-US')} zeta zeros (computed)`, t, tS + 0.3, 1e9, { size: 30 });
  tag(g, 1460, 380, 'curve: random-matrix theory', t, SW(6, 'right'), 1e9, { size: 30, col: PAL.red });
  stamp(g, 1460, 560, 'SAME CURVE', t, SW(6, 'curve'), 1e9, { size: 56 });
  tag(g, 600, 300, 'almost never close together', t, SW(6, 'right') + 2.5, 1e9, { size: 30 });
  tag(g, 1300, 760, 'Odlyzko, 1980s: near the 10²⁰-th zero, even better', t, SW(6, 'odlizzko'), 1e9, { size: 30, bg: '#fbe2c0' });
}, { prep: () => ({ sp: zeroSpacings() }) });

deepScene('hilbert-polya', SW(6, 'dream') - 0.4, 631, (g, t, S) => {
  title(g, 960, 120, 'the Hilbert–Pólya dream', t, SW(6, 'dream'), 1e9, 60);
  ZEROS.slice(0, 12).forEach((gm, k) => { const tk = SW(6, 'maybe') + k * 0.15; if (t < tk) return; const y = 980 - gm * 13.5, a = seg(t, tk, tk + 0.3); g.globalAlpha = a; ink(g, [[700, y], [1220, y]], 4, { col: '#ffcf7a', t0: 0.05, t1: 0.05, seed: k }); g.font = 'italic 500 24px Georgia'; g.fillStyle = '#e8eef4'; g.textAlign = 'left'; g.fillText(gm.toFixed(2), 1250, y + 8); g.globalAlpha = 1; });
  tag(g, 400, 420, 'zeros = energy levels\nof an unknown system?', t, SW(6, 'unknown'), 1e9, { size: 36 });
  tag(g, 400, 640, 'real energies ⇔ every zero on the line', t, SW(6, 'real'), 1e9, { size: 30, bg: '#fbe2c0' });
  stamp(g, 1550, 560, 'NOT FOUND', t, SW(6, 'nobody'), 1e9, { size: 48, col: '#ff8a5a' });
  tag(g, 1550, 820, 'but the zeros are organised', t, SW(6, 'organised') - 0.2, 1e9, { size: 32 });
});

// ================= 7. The toy universe =================
board('clock', SB(7) - 0.3, 'warm', 701, (g, t, S) => {
  const t5 = SW(7, 'five'), t4 = SW(7, 'four'), t2 = SW(7, 'two'), hand = t < t5 ? 0 : t < t4 ? lerp(0, 5, E.io2(seg(t, t5, t5 + 0.6))) : lerp(5, 9, E.io2(seg(t, t4, t4 + 0.8)));
  clockFace(g, 600, 540, 330, 7, hand % 7);
  writeText(g, '5 + 4 = 2', 1380, 380, 96, t, t2 - 0.2, 0.7, { align: 'center', upright: true });
  tag(g, 1380, 560, 'a "finite field": only 7 numbers', t, SW(7, 'finite'), 1e9, { size: 34 });
  tag(g, 1380, 700, 'add · subtract · multiply · divide', t, SW(7, 'divide'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('ec', SW(7, 'curves') - 0.4, 'sage', 711, (g, t, S) => {
  const tC = SW(7, 'curves'), p = 37, pts = S.pts;
  // left: the points of y^2 = x^3 + x + 1 over the 37-hour clock
  const m = axes(g, 160, 900, 620, 620, { x0: 0, x1: p, y0: 0, y1: p, xl: 'x', yl: 'y', a: seg(t, tC - 0.3, tC + 0.2), fs: 26 });
  pts.forEach(([x, y], i) => { if (t > tC + 0.3 + i * 0.03) { const [px, py] = m(x + 0.5, y + 0.5); g.fillStyle = css(PAL.blue); g.beginPath(); g.arc(px, py, 7, 0, TAU); g.fill(); } });
  tag(g, 470, 210, 'y² = x³ + x + 1  on a 37-hour clock', t, tC, 1e9, { size: 28 });
  tag(g, 470, 1000, `count: ${pts.length + 1} points`, t, SW(7, 'count'), 1e9, { size: 30 });
  // right: point counts for many clock sizes stay inside the window p + 1 ± 2√p (Hasse; RH for this curve)
  const tW = SW(7, 'zeros'), m2 = axes(g, 980, 940, 800, 500, { x0: 0, x1: 400, y0: -45, y1: 45, xl: 'clock size p', yl: 'count − (p + 1)', a: seg(t, tW - 0.3, tW), fs: 26 });
  if (t > tW) { plot(g, m2, x => 2 * Math.sqrt(x), 0, 400, seg(t, tW, tW + 1), { col: PAL.red, w: 4 }); plot(g, m2, x => -2 * Math.sqrt(x), 0, 400, seg(t, tW, tW + 1), { col: PAL.red, w: 4 }); }
  S.cnt.forEach(([q, d], i) => { if (t > tW + 1 + i * 0.03) { const [px, py] = m2(q, d); g.fillStyle = css(PAL.ink); g.beginPath(); g.arc(px, py, 5, 0, TAU); g.fill(); } });
  tag(g, 1600, 230, 'window ± 2√p', t, tW + 1, 1e9, { size: 28, col: PAL.red });
  tag(g, 1380, 1010, 'André Weil, 1940s: always inside → RH true here', t, SW(7, 'weil'), 1e9, { size: 30, bg: '#fbe2c0' });
  stamp(g, 1220, 330, 'PROVED', t, SW(7, 'yes'), 1e9, { size: 44, col: PAL.teal });
  tag(g, 1640, 360, 'Deligne, 1974 (Fields Medal)', t, SW(7, 'deligne'), 1e9, { size: 28 });
}, { prep: () => { const pts = ecPoints(37), cnt = []; for (const q of PRIMES) { if (q > 400) break; if (q < 5) continue; cnt.push([q, ecCount(q) - q - 1]); } return { pts, cnt }; } });

board('stepanov', SW(7, 'humbler') - 0.4, 'warm', 721, (g, t, S) => {
  tag(g, 960, 110, '1969 · Sergei Stepanov: just polynomials', t, SW(7, 'stepanov'), 1e9, { size: 40 });
  const tP = SW(7, 'polynomial'), m = axes(g, 200, 760, 1500, 520, { x0: 0, x1: 10, y0: -1, y1: 6, a: seg(t, tP - 0.4, tP), fs: 26 });
  const roots = [1.5, 4, 6.5, 9]; const f = x => Math.min(6, roots.reduce((a, r) => a * (x - r) * (x - r), 1) / 400);
  if (t > tP) plot(g, m, f, 0.6, 9.8, seg(t, tP, tP + 2), { col: PAL.blue, w: 6, n: 400 });
  roots.forEach((r, k) => { if (t > SW(7, 'touches') + k * 0.25) { const [x, y] = m(r, 0); zeroLantern(g, x, y, 10); tag(g, x, y + 60, '×2', t, SW(7, 'touches') + k * 0.25, 1e9, { size: 26 }); } });
  tag(g, 1500, 860, 'counting with multiplicity → a tight window', t, SW(7, 'window'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('derive7', SW(7, 'decades') - 0.4, 'blue', 731, (g, t, S) => {
  title(g, 960, 200, 'why not for the ordinary whole numbers?', t, SW(7, 'decades'), 1e9, 56, PAL.ink, { shadow: false });
  tag(g, 960, 420, 'multiplicity needs something like a derivative', t, SW(7, 'derivative'), 1e9, { size: 40 });
  writeText(g, 'd/dx ( 7 ) = ?', 960, 640, 140, t, SW(7, 'differentiate') - 0.2, 0.9, { align: 'center' });
  tag(g, 960, 900, 'hold on to this question', t, SW(7, 'hold'), 1e9, { size: 38, bg: '#fbe2c0' });
});

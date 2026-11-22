// ================= 11. The catch =================
function balance(g, x, y, L, tilt, left, right) {
  ink(g, [[x, y + 260], [x, y]], 10, { col: PAL.ink, t0: 0, t1: 0 }); fillPts(g, rough([[x - 120, y + 260], [x + 120, y + 260], [x + 120, y + 290], [x - 120, y + 290]], 2, 3, 10), css('#5a4a3a'));
  const dx = Math.cos(tilt) * L, dy = Math.sin(tilt) * L; ink(g, [[x - dx, y - dy], [x + dx, y + dy]], 8, { col: PAL.ink, t0: 0.05, t1: 0.05 });
  [[x - dx, y - dy, left], [x + dx, y + dy, right]].forEach(([px, py, lab], k) => { ink(g, [[px, py], [px - 70, py + 120]], 2, { col: PAL.ink, t0: 0, t1: 0 }); ink(g, [[px, py], [px + 70, py + 120]], 2, { col: PAL.ink, t0: 0, t1: 0 }); const pan = rough([[px - 110, py + 120], [px + 110, py + 120], [px + 80, py + 160], [px - 80, py + 160]], 2, k, 8); cels(g, pan, { base: PAL.gold, sh: '#9a6a18', sw: 4, L: LIGHT, line: PAL.ink, lw: 2.4, seed: k }); g.font = '700 34px Georgia'; g.fillStyle = PAL.ink; g.textAlign = 'center'; g.fillText(lab, px, py + 210); });
}

deepScene('nobody', SB(11) - 0.3, 1101, (g, t, S) => {
  lockBox(g, 960, 520, 300, 'RH  ✓', t, SB(11) + 0.3);
  title(g, 960, 130, 'as of today, nobody understands this proof', t, SW(11, 'nobody'), 1e9, 56);
  tag(g, 960, 900, 'by his own account, not fully even its author', t, SW(11, 'account'), 1e9, { size: 34 });
});

board('admits', SW(11, 'openly') - 0.4, 'warm', 1111, (g, t, S) => {
  quoteCard(g, '…little mastery of automorphic forms on higher rank groups…', 'Levent Alpöge, on himself, in the note', t, SW(11, 'mastery') - 0.8, SW(11, 'pages') - 0.4, { size: 50, y: 440 });
  tag(g, 960, 300, 'a human-readable version: 250,000+ pages', t, SW(11, 'pages'), 1e9, { size: 44, bg: '#fbe2c0' });
  quoteCard(g, 'massive unreadable travesty of "slop"', 'Alpöge, on posting it', t, SW(11, 'slop') - 0.6, SW(11, 'quarter') - 0.4, { size: 50, y: 600 });
  tag(g, 960, 520, 'at 10 pages a day: ' + rollNum(t, SW(11, 'seventy') - 1.2, SW(11, 'seventy'), 0, 68) + ' years', t, SW(11, 'quarter'), 1e9, { size: 56 });
  title(g, 960, 820, 'we may know THAT it is true\nwhile knowing almost nothing about WHY', t, SW(11, 'strange'), 1e9, 52, PAL.ink, { shadow: false });
});

board('reactions', SW(11, 'maynard') - 0.6, 'blue', 1121, (g, t, S) => {
  const tM = SW(11, 'maynard'), tC = SW(11, 'conrad'), tT = SW(11, 'tao'), tL = SW(11, 'lamzouri');
  tag(g, 960, 300, 'James Maynard (Oxford, Fields Medal):\n"98 or 99 percent" certain', t, tM, tC - 0.4, { size: 44 });
  tag(g, 960, 520, 'to NYT · to IBM: "a historic milestone… if this fully checks out"', t, SW(11, 'ibm') - 0.5, tC - 0.4, { size: 30, bg: '#fbe2c0' });
  quoteCard(g, 'frankly completely outrageous', 'Brian Conrad, Stanford, to The New York Times, on the release', t, tC, tT - 0.4, { size: 64 });
  tag(g, 960, 860, 'not a claim that it is wrong · about how it was released', t, SW(11, 'believed'), tT - 0.4, { size: 30 });
  quoteCard(g, 'the full power of modern developments on the Langlands program and arithmetic geometry', 'Terence Tao, blog, 20 Nov 2026', t, SW(11, 'langlands') - 1.2, tL - 0.4, { size: 46 });
  tag(g, 960, 860, '"the digestion process will take some time"', t, SW(11, 'digestion'), tL - 0.4, { size: 32 });
  tag(g, 960, 500, 'Youness Lamzouri: understanding it could take\n"months or even years"', t, tL, 1e9, { size: 42 });
});

deepScene('locked', SW(11, 'explanation') - 0.6, 1131, (g, t, S) => {
  title(g, 960, 130, 'a proof is supposed to be an explanation', t, SW(11, 'explanation') - 0.4, 1e9, 52);
  tag(g, 520, 480, "Weil's proof in the toy universe\n→ whole new fields", t, SW(11, 'toy'), 1e9, { size: 32 });
  lockBox(g, 1300, 560, 260, 'RH  ✓', t, SW(11, 'locked') - 0.3);
  tag(g, 1300, 920, 'the right answer, printed on the lid', t, SW(11, 'lid'), 1e9, { size: 34, bg: '#fbe2c0' });
});

board('clay-rules', SW(11, 'practical') - 0.4, 'sage', 1141, (g, t, S) => {
  tag(g, 960, 200, "Clay's rules: published in a recognised venue\n+ generally accepted for 2 years", t, SW(11, 'clay'), 1e9, { size: 40 });
  tag(g, 960, 400, 'a code library is not a journal paper', t, SW(11, 'journal'), 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 960, 520, 'no public statement from Clay found (22 Nov)', t, SW(11, 'statement'), 1e9, { size: 30 });
  const tC = SW(11, 'credit'), tilt = t < tC ? 0 : kf(t, [[tC + 0.5, 0], [SW(11, 'share'), 0.18], [SW(11, 'deserves'), -0.1], [SW(11, 'deserves') + 2, 0]]);
  if (t > tC) balance(g, 960, 650, 360, tilt, 'human', 'machine');
}, { cam: { z0: 1.0, z1: 1.03 } });

board('honest', SW(11, 'honest') - 0.6, 'warm', 1151, (g, t, S) => {
  title(g, 960, 150, 'so, did AI solve the Riemann hypothesis?', t, SW(11, 'solve'), 1e9, 56, PAL.ink, { shadow: false });
  tag(g, 960, 360, 'a human and a machine produced a proof', t, SW(11, 'produced'), 1e9, { size: 42 });
  tag(g, 960, 530, 'the strictest checkers say it is correct', t, SW(11, 'strictest'), 1e9, { size: 42 });
  tag(g, 960, 700, 'understanding it has barely begun', t, SW(11, 'barely'), 1e9, { size: 42, bg: '#fbe2c0' });
  tag(g, 960, 920, '…and the same day, something else happened', t, SW(11, 'else'), 1e9, { size: 34 });
});

// ================= 12. The other catch falls =================
scene('tea-callback', SB(12) - 0.3, () => ({ pl: teaPlate(1201), sim: teaSim(5, SE(12) - SB(12) + 4) }), (g, l, t, S) => {
  const T0 = S._sh.t0, z = kf(t, [[T0, 1.05], [S._sh.t1, 1.0, E.io2]]);
  g.save(); cam(g, 960, 540, z); plateDraw(g, S.pl); drawTea(g, S, t - T0);
  const tA = SW(12, 'alone'), fa = t < tA ? seg(t, SW(12, 'forcing'), SW(12, 'forcing') + 0.5) : 1 - seg(t, tA, tA + 0.8);
  if (fa > 0) for (let k = 0; k < 8; k++) { const a = k / 8 * TAU + t * 0.6, r0 = 360, r1 = 250; arrow(g, CUP.x + Math.cos(a) * r0, CUP.y + Math.sin(a) * r0, CUP.x + Math.cos(a + 0.5) * r1, CUP.y + Math.sin(a + 0.5) * r1, 6, PAL.red, fa); }
  g.restore();
  tag(g, 360, 140, 'video 03: the catch was forcing', t, SW(12, 'forcing'), 1e9, { size: 34 });
  tag(g, 1560, 940, 'left completely alone?', t, tA, 1e9, { size: 38, bg: '#fbe2c0' });
});

board('ns-claims', SW(12, 'tao') - 0.5, 'blue', 1211, (g, t, S) => {
  tag(g, 960, 110, "Terence Tao's blog, 20 Nov 2026", t, SW(12, 'tao'), 1e9, { size: 36 });
  tag(g, 960, 300, 'Navier–Stokes blows up in 3D, no outside push\nAlpöge & Buckmaster, heavy AI assistance (claimed)', t, SW(12, 'buckmaster'), 1e9, { size: 36 });
  tag(g, 960, 520, 'same for the Euler equations\nAlpöge, Buckmaster & Coiculescu (claimed)', t, SW(12, 'euler'), 1e9, { size: 36 });
  tag(g, 960, 700, 'both with Lean formalisations', t, SW(12, 'formalisations'), 1e9, { size: 32, bg: '#fbe2c0' });
  tag(g, 960, 860, 'Tao: human-readable, strategy broadly clear;\nneeds a proper, professionally written proof', t, SW(12, 'professionally') - 1.5, 1e9, { size: 30 });
});

board('two-problems', SW(12, 'fallen') - 0.6, 'sage', 1221, (g, t, S) => {
  stamp(g, 960, 160, 'UNFORCED? (CLAIMED · 2 DAYS OLD)', t, SW(12, 'fallen'), 1e9, { size: 44 });
  const tS = SW(12, 'millennium'), names = ['P vs NP', 'Hodge', 'Poincaré', 'Riemann', 'Yang–Mills', 'Navier–Stokes', 'Birch–Swinnerton-Dyer'];
  names.forEach((nm, k) => { const x = 260 + k * 233, y = 520 + (k % 2) * 60; const hot = nm === 'Riemann' || nm === 'Navier–Stokes'; envelope(g, x, y, 200, nm, nm === 'Poincaré' ? 1 : 0, { glow: hot ? seg(t, tS, tS + 1) * (0.7 + 0.3 * Math.sin(t * 3)) : 0, col: hot ? '#f3e2b8' : undefined }); });
  tag(g, 960, 820, 'two of the seven touched in a single week', t, tS, 1e9, { size: 40, bg: '#fbe2c0' });
});

board('four-months', SW(12, 'back') - 0.5, 'warm', 1231, (g, t, S) => {
  const tB = SW(12, 'back'); ribbon(g, 200, 1720, 600, t, tB);
  pin(g, 360, 600, 'Aug: 2/3 on the line', t, tB + 0.6, { size: 28 });
  pin(g, 900, 600, 'Oct: a band emptied', t, SW(12, 'emptied'), { size: 28 });
  pin(g, 1560, 600, 'Nov: the whole thing?', t, SW(12, 'closed'), { size: 28, col: PAL.gold });
  tag(g, 960, 800, '6 weeks  vs  167 years', t, SW(12, 'weeks'), 1e9, { size: 52 });
  tag(g, 960, 960, 'a proof: written by a person, for a person… until now', t, SW(12, 'history'), 1e9, { size: 32 });
});

board('certainty', SW(12, 'decide') - 0.6, 'blue', 1241, (g, t, S) => {
  const tC = SW(12, 'certainty'), tU = SW(12, 'understanding');
  balance(g, 960, 420, 420, t < tC ? 0 : t < tU ? lerp(0, 0.16, seg(t, tC, tC + 0.6)) : lerp(0.16, -0.16, seg(t, tU, tU + 0.8)) * (1 - 0.5 * seg(t, tU + 1, tU + 3)), 'CERTAINTY', 'UNDERSTANDING');
  title(g, 960, 140, 'what will mathematicians value more?', t, SW(12, 'decide'), 1e9, 52, PAL.ink, { shadow: false });
});

// ================= 13. Close =================
deepScene('close-stair', SB(13) - 0.4, 1301, (g, t, S) => {
  const T0 = S._sh.t0, map = axes(g, 160, 940, 1100, 720, { x0: 0, x1: 50, y0: -2, y1: 56, a: 0, fs: 1 });
  const N = lerp(0, 300, E.io2(seg(t, SW(13, 'waves') - 0.5, SW(13, 'waves') + 4)));
  plot(g, map, psiTrue, 1.5, 50, seg(t, T0, T0 + 2), { col: '#8fa3b8', w: 3, n: 800, a: 0.7 });
  if (N > 0.2) plot(g, map, x => psiZeros(x, N), 1.6, 50, 1, { col: '#ffcf7a', w: 5, n: 900 });
  [2, 3, 5, 7, 11].forEach((p, k) => title(g, 220 + k * 120, 100, String(p), t, T0 + 0.3 + k * 0.25, 1e9, 64, '#fff1cf', { upright: true }));
  const m2 = stripPlane(g, 1380, 80, 400, 920, { s0: 0, s1: 1, t0: 0, t1: 60, a: seg(t, SW(13, 'line') - 0.5, SW(13, 'line')), ink: '#e8eef4', lineCol: '#ffcf7a', labels: false, strip: false });
  if (t > SW(13, 'line')) ZEROS.slice(0, 14).forEach((gm, k) => { const [x, y] = m2(0.5, gm); zeroLantern(g, x, y, 8, seg(t, SW(13, 'line') + k * 0.1, SW(13, 'line') + k * 0.1 + 0.4)); });
});

deepScene('close-riemann', SW(13, 'eighteen') - 0.4, 1311, (g, t, S) => {
  title(g, 960, 300, '"very probable"', t, SW(13, 'probable'), 1e9, 90);
  tag(g, 960, 520, 'Bernhard Riemann · 1826 – 1866', t, SW(13, 'died'), 1e9, { size: 44 });
  tag(g, 960, 760, 'today: a "yes", 17,000,000 lines long', t, SW(13, 'yes'), 1e9, { size: 46, bg: '#fbe2c0' });
});

deepScene('close-why', SW(13, 'whether') - 0.4, 1321, (g, t, S) => {
  title(g, 960, 260, 'not just whether', t, SW(13, 'whether'), 1e9, 64);
  title(g, 960, 480, 'WHY?', t, SW(13, 'why'), 1e9, 220, '#ffcf7a', { upright: true, weight: 700 });
  title(g, 960, 760, 'why are the primes in perfect harmony?', t, SW(13, 'harmony'), 1e9, 48);
  title(g, 960, 900, 'the machine says they are · now it\'s our turn', t, SW(13, 'machine'), 1e9, 40);
});

// ================= 9. Inside the machine's proof =================
board('verdict', SB(9) - 0.3, 'warm', 901, (g, t, S) => {
  tag(g, 960, 110, 'the assessment’s verdict (10 Nov 2026)', t, SW(9, 'verdict'), 1e9, { size: 38 });
  quoteCard(g, 'In our judgment the submission proves that a relativistic quantum field theory satisfying the Wightman axioms can be constructed as a scaling limit of four-dimensional Wilson lattice gauge theory for any simple compact gauge group.', 'Yang–Mills group, assessment 1', t, SW(9, 'judgment') - 0.3, SW(9, 'plain') - 0.3, { y: 540, size: 42, w: 1400 });
  title(g, 960, 420, 'in plain words:', t, SW(9, 'plain'), 1e9, 56, PAL.ink, { shadow: false });
  tag(g, 960, 600, 'Wilson’s grid → the limit really exists', t, SW(9, 'grid'), 1e9, { size: 52, bg: '#fbe2c0' });
});

board('route', SW(9, 'route') - 0.4, 'blue', 911, (g, t, S) => {
  const F0 = latAt(t, S._sh.t0, 3), B1 = blockLat(F0, 32), B2 = blockLat(B1, 16), B3 = blockLat(B2, 8);
  const ts = [SW(9, 'shrink'), SW(9, 'finer', 1), SW(9, 'finer', 2), SW(9, 'finer', 3)]; let lv = 0; ts.forEach((tk, i) => { if (t > tk) lv = i; });
  const lat = [[B3, 4], [B2, 8], [B1, 16], [F0, 32]][lv], box = 640, x0 = 160, y0 = 220;
  tag(g, 480, 140, '1 · fix a box of space and time', t, SW(9, 'first'), 1e9, { size: 32 });
  if (t > SW(9, 'first')) { drawLattice(g, x0, y0, box / lat[1], lat[0], lat[1], lat[1], { heat: 0.8, dials: lat[1] <= 8 }); inkLoop(g, rough([[x0, y0], [x0 + box, y0], [x0 + box, y0 + box], [x0, y0 + box]], 1, 2, 10), 6, { col: PAL.ink }); }
  tag(g, 480, 930, ['grid: 4 × 4', 'finer: 8 × 8', 'finer: 16 × 16', 'finer: 32 × 32'][lv], t, SW(9, 'shrink'), 1e9, { size: 30 });
  // renormalization group: 2 x 2 squares averaged into one (blocking), shown on the right
  const tA = SW(9, 'averages'); if (t > tA) { const a = seg(t, tA, tA + 0.6); g.globalAlpha = a; drawLattice(g, 1060, 300, 60, B2, 16, 4, { heat: 0.8, dials: true }); arrow(g, 1330, 420, 1440, 420, 6, PAL.ink); drawLattice(g, 1460, 300, 120, B3, 8, 2, { heat: 0.8, dials: true }); g.globalAlpha = 1; tag(g, 1440, 620, '2 × 2 squares → 1 bigger square\n(real blocking of the simulation)', t, tA, 1e9, { size: 28 }); }
  tag(g, 1440, 760, 'the renormalization group', t, SW(9, 'renormalization'), 1e9, { size: 34 });
  tag(g, 1440, 870, 'Bałaban’s approach, “with many improvements”', t, SW(9, 'improvements'), 1e9, { size: 30 });
  stamp(g, 1440, 1000, 'THE ANSWERS SETTLE ✓', t, SW(9, 'settle'), 1e9, { size: 44, col: '#3f7a4a' });
});

board('steps', SW(9, 'second') - 0.4, 'warm', 921, (g, t, S) => {
  checklist(g, 300, 260, [['1 · fix a box, shrink the grid: a limit exists', '✓', SW(9, 'second') - 0.3], ['2 · grow the box towards infinity', '✓', SW(9, 'second')], ['3 · the rules hold: energy ≥ 0, cause → effect, relativity', '✓', SW(9, 'third')], ['4 · rebuild a full quantum theory', '✓', SW(9, 'finally')]], t, { size: 42, dy: 150 });
  tag(g, 1200, 880, 'step 2 has a catch: we’ll come back to it', t, SW(9, 'finally') + 1.2, 1e9, { size: 30, bg: '#fbe2c0' });
});

board('impostor', SW(9, 'impostor') - 0.4, 'sage', 931, (g, t, S) => {
  title(g, 960, 110, 'is it really Yang–Mills, or an impostor?', t, SW(9, 'impostor'), 1e9, 52, PAL.ink, { shadow: false });
  // g^2(r) ~ 1 / (2 b0 log(1/(r Lambda))): the one-loop law proved in the assessment's Theorem 7.2 (SU(3): b0 = 33/(48 pi^2))
  const b0 = 33 / (48 * Math.PI * Math.PI), map = axes(g, 280, 900, 1100, 600, { x0: 0, x1: 1, y0: 0, y1: 12, xl: 'distance (arbitrary units)', yl: 'coupling g²', fs: 30 });
  const tR = SW(9, 'rate'); if (t > tR - 0.4) plot(g, map, r => 1 / (2 * b0 * Math.log(1.25 / r)), 0.002, 0.78, seg(t, tR - 0.4, tR + 1.6), { col: PAL.red, w: 6, clipY: 300 });
  tag(g, 1560, 360, 'fades at short distances at the rate\npredicted by Gross, Wilczek, Politzer (1973)', t, tR, 1e9, { size: 28 });
  stamp(g, 1560, 600, 'PROVED IN LEAN', t, SW(9, 'lean'), 1e9, { size: 50 });
  tag(g, 1560, 780, 'the jury chair’s Nobel formula', t, SW(9, 'jury'), 1e9, { size: 32, bg: '#fbe2c0' });
  tag(g, 820, 980, 'leading (one-loop) law, SU(3)', t, tR + 1, 1e9, { size: 24 });
});

deepScene('eight', SW(9, 'lovely') - 0.4, 941, (g, t, S) => {
  const tE = SW(9, 'eight');
  eightWaves(g, 160, 220, 1600, 600, t, seg(t, tE - 0.3, tE + 2.5));
  tag(g, 960, 120, 'zoom in: energy and momentum behave like eight free copies of light', t, tE, 1e9, { size: 36 });
  tag(g, 960, 900, 'one for each kind of gluon · up close, they forget each other', t, SW(9, 'forget'), 1e9, { size: 32 });
  tag(g, 960, 1000, 'asymptotic freedom as a theorem · and not a free theory in disguise', t, SW(9, 'theorem'), 1e9, { size: 30, bg: '#fbe2c0' });
});

deepScene('lean', SW(9, 'lean', 2) - 0.4, 951, (g, t, S) => {
  const T0 = S._sh.t0;
  codeScroll(g, 120, 140, 980, 800, LEAN_YM, t, T0, { speed: 45 });
  tag(g, 610, 1000, 'declaration names from the assessment (layout illustrative)', t, T0 + 0.6, 1e9, { size: 24 });
  tag(g, 1490, 200, rollNum(t, SW(9, 'files') - 1.6, SW(9, 'files'), 0, 9477) + ' Lean files', t, SW(9, 'files') - 1.6, 1e9, { size: 44 });
  tag(g, 1490, 360, 'clean rebuild ≈ 184 minutes', t, SW(9, 'minutes'), 1e9, { size: 36 });
  stamp(g, 1490, 500, '0 ERRORS', t, SW(9, 'errors'), 1e9, { size: 56, col: '#6fd08a' });
  tag(g, 1490, 660, rollNum(t, SW(9, 'lemmas') - 1.2, SW(9, 'lemmas'), 0, 250319) + '\ndefinitions & lemmas', t, SW(9, 'lemmas') - 1.2, 1e9, { size: 38 });
  tag(g, 1490, 840, 'only the 3 standard axioms Lean allows', t, SW(9, 'axioms', 2), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('comparator', SW(9, 'comparator') - 0.4, 'blue', 961, (g, t, S) => {
  title(g, 960, 110, 'Comparator: is the proof answering the question we fixed?', t, SW(9, 'comparator'), SW(9, 'twist') - 0.3, 46, PAL.ink, { shadow: false });
  checklist(g, 380, 290, [['the valid proof', '✓', SW(9, 'question')], ['a changed statement', '✗', SW(9, 'statement', 2)], ['a forbidden assumption', '✗', SW(9, 'forbidden')], ['a changed definition', '✗', SW(9, 'definition')]], t, { size: 44, dy: 120 });
  tag(g, 1400, 520, 'rejected all three ✓', t, SW(9, 'rejected'), SW(9, 'twist') - 0.3, { size: 38, bg: '#fbe2c0' });
  if (t > SW(9, 'twist')) { g.fillStyle = css('#e8e2d4', seg(t, SW(9, 'twist'), SW(9, 'twist') + 0.4) * 0.98); g.fillRect(0, 0, 1920, 1080); }
  stamp(g, 960, 300, 'A TWIST', t, SW(9, 'twist'), 1e9, { size: 70 });
  tag(g, 960, 520, 'OpenAI’s own public statement left out links to the grid', t, SW(9, 'public'), 1e9, { size: 36 });
  tag(g, 960, 660, 'the proof was stronger than its summary', t, SW(9, 'stronger'), 1e9, { size: 42, bg: '#fbe2c0' });
  tag(g, 960, 820, 'so the panel checked a stronger target of its own', t, SW(9, 'target'), 1e9, { size: 36 });
});

board('kitchen', SW(9, 'complaints') - 0.4, 'warm', 971, (g, t, S) => {
  quoteCard(g, 'concerns with the quality of the code', 'the assessment', t, SW(9, 'concerns'), SW(9, 'matters') - 0.4, { y: 200, size: 44, w: 900 });
  tag(g, 960, 400, 'fewer than half the declarations are used by the main results\n(100,168 of 250,319)', t, SW(9, 'half'), SW(9, 'matters') - 0.4, { size: 30 });
  const file = (x, y, name, items, tIn) => { if (t < tIn || t > SW(9, 'matters')) return; docPage(g, x, y, 460, 300, t, tIn, { head: name, hs: 34, lines: 3 }); tag(g, x, y + 60, items, t, tIn + 0.5, SW(9, 'matters') - 0.4, { size: 34, bg: '#fbe2c0' }); };
  file(500, 700, 'Kitchen.lean', 'Cake · cookie', SW(9, 'kitchen'));
  file(1420, 700, 'Parade.lean', 'anxiety', SW(9, 'parade'));
  quoteCard(g, 'We do not claim to fully understand the proof yet.', 'the Yang–Mills group', t, SW(9, 'understand') - 0.4, 1e9, { y: 480, size: 56 });
  tag(g, 960, 860, 'they used OpenAI’s AI model, Astra, to help digest the code', t, SW(9, 'astra'), 1e9, { size: 32 });
});

// ================= 10. The footnote =================
board('sequence', SB(10) - 0.3, 'warm', 1001, (g, t, S) => {
  title(g, 960, 110, 'now, the footnote', t, SB(10) + 0.1, SW(10, 'growing') - 0.2, 56, PAL.ink, { shadow: false });
  const map = axes(g, 200, 760, 1500, 520, { x0: 0, x1: 1000, y0: -1.1, y1: 1.1, xl: 'step', yl: 'an answer that keeps jumping (bounded)', fs: 28 });
  const tS = SW(10, 'wildly') - 0.6, N = Math.floor(1000 * seg(t, tS, tS + 2.5));
  g.fillStyle = css(PAL.blue, 0.55); for (let n = 1; n <= N; n++) { const [x, y] = map(n, Math.sin(n)); g.fillRect(x - 2, y - 2, 4, 4); }
  if (!S.sub) { S.sub = []; let last = 0, eps = 0.3; while (S.sub.length < 9) { let n = last + 15; while (Math.abs(Math.sin(n) - 0.5) >= eps) n++; S.sub.push(n); last = n; eps *= 0.55; } }
  const tP = SW(10, 'pick'); if (t > tP) { const pts = S.sub.map(n => map(n, Math.sin(n))); const m = Math.max(1, Math.floor(pts.length * seg(t, tP, tP + 1.5))); const [, yT] = map(0, 0.5); ink(g, [[200, yT], [1700, yT]], 2, { col: PAL.red, a: 0.35, t0: 0, t1: 0 }); if (m > 1) ink(g, pts.slice(0, m), 3, { col: PAL.red, a: 0.8, t0: 0, t1: 0 }); for (let i = 0; i < m; i++) glowDot(g, pts[i][0], pts[i][1], 18, PAL.gold, 1); tag(g, 1560, 300, 'a part that settles down', t, tP + 0.5, 1e9, { size: 28 }); }
  tag(g, 960, 900, 'a classic theorem: you can always pick out a part that settles down', t, SW(10, 'settles'), 1e9, { size: 32, bg: '#fbe2c0' });
});

deepScene('ball', SW(10, 'ball') - 0.4, 1011, (g, t, S) => {
  const T0 = SW(10, 'ball'), bx = 460, by = 180, bw = 1000, bh = 720, tri = u => 1 - Math.abs(((u % 2) + 2) % 2 - 1);
  const pos = k => [bx + 30 + (bw - 60) * tri(k * 0.7310585 + 0.13), by + 30 + (bh - 60) * tri(k * 0.4142136 + 0.61)];
  inkLoop(g, rough([[bx, by], [bx + bw, by], [bx + bw, by + bh], [bx, by + bh]], 2, 3, 12), 6, { col: '#e8eef4' });
  const tSn = SW(10, 'snapshot'), N = Math.floor(Math.max(0, t - tSn) * 220);
  g.fillStyle = css('#8fc4e8', 0.55); for (let k = 0; k < Math.min(N, 3000); k++) { const [x, y] = pos(k); g.fillRect(x - 2.5, y - 2.5, 5, 5); }
  const [x, y] = pos((t - T0) * 1.3); particle(g, x, y, 20, PAL.gold);
  const cluster = (P, tIn, col) => { if (t < tIn) return; if (!S[col]) { S[col] = []; let best = 1e9; for (let k = 0; k < 3000; k++) { const [qx, qy] = pos(k), d = Math.hypot(qx - P[0], qy - P[1]); if (d < best * 0.75) { S[col].push(k); best = d; } } } const m = Math.max(1, Math.floor(S[col].length * seg(t, tIn, tIn + 1.4))); for (let i = 0; i < m; i++) { const [qx, qy] = pos(S[col][i]); glowDot(g, qx, qy, 18, col, 1); } brushCircle(g, P[0], P[1], 46, 46, t, tIn + 1.2, col, 5); };
  cluster([bx + 330, by + 260], SW(10, 'crowd'), '#ffcf7a');
  cluster([bx + 760, by + 520], SW(10, 'somewhere'), '#ff7a6a');
  tag(g, 960, 110, 'a ball bouncing in a box, forever · one snapshot per tick', t, SW(10, 'snapshot'), 1e9, { size: 32 });
  tag(g, 1700, 330, 'COMPACTNESS', t, SW(10, 'compactness'), 1e9, { size: 36 });
  tag(g, 1700, 470, 'a limit exists ✓', t, SW(10, 'exists'), 1e9, { size: 30 });
  tag(g, 1700, 610, 'the only one? ✗', t, SW(10, 'only'), 1e9, { size: 30, bg: '#fbe2c0' });
  tag(g, 960, 990, 'the panel: the limit “is not proven to be unique”', t, SW(10, 'unique'), 1e9, { size: 32 });
});

board('footnote2', SW(10, 'footnote', 2) - 1.2, 'blue', 1021, (g, t, S) => {
  docPage(g, 420, 540, 560, 760, t, SW(10, 'footnote', 2) - 1.0, { head: 'Quantum Yang–Mills Theory', hs: 26, foot: '² We specifically exclude weak-existence …', footT: SW(10, 'footnote', 2), lines: 14, fs: 20 });
  tag(g, 420, 1000, 'Jaffe & Witten · official problem · 2000', t, SW(10, 'footnote', 2) - 0.6, 1e9, { size: 26 });
  quoteCard(g, 'We specifically exclude weak-existence (compactness) as the solution to the existence part of the Millennium problem, unless one also uses other techniques to establish properties of the limit (such as the existence of a mass gap and the axioms).', 'footnote 2', t, SW(10, 'quote'), 1e9, { x: 1220, y: 540, w: 900, size: 38 });
});

board('debate', SW(10, 'which') - 0.4, 'warm', 1031, (g, t, S) => {
  tag(g, 500, 110, 'the panel (Jaffe is a member)', t, SW(10, 'himself'), 1e9, { size: 32 });
  tag(g, 1420, 110, 'Peter Woit, Not Even Wrong', t, SW(10, 'woyt'), 1e9, { size: 32 });
  ink(g, [[960, 160], [960, 1000]], 4, { col: PAL.ink, a: 0.4, t0: 0.1, t1: 0.1 });
  quoteCard(g, 'regards a subsequential limit obtained via compactness as insufficient, unless additional arguments establish the requisite properties of the limit.', 'the assessment', t, SW(10, 'regards') - 0.3, SW(10, 'substantial') - 0.4, { x: 500, y: 520, w: 700, size: 34 });
  quoteCard(g, 'substantial information beyond compactness', 'the assessment, on the machine’s proof', t, SW(10, 'substantial') - 0.3, 1e9, { x: 500, y: 520, w: 700, size: 44 });
  tag(g, 500, 880, 'it proves the rules hold for what it builds', t, SW(10, 'supplies'), 1e9, { size: 28, bg: '#fbe2c0' });
  tag(g, 1420, 250, '“The Yang-Mills Publicity Stunt” (11 Nov)', t, SW(10, 'titled'), 1e9, { size: 28, italic: true });
  quoteCard(g, 'it is clear that this claim is also extremely misleading.', 'Peter Woit', t, SW(10, 'misleading') - 1.2, SW(10, 'separately') - 0.5, { x: 1420, y: 560, w: 700, size: 40 });
  quoteCard(g, 'they have not separately established the mass gap or properties comparable to it', 'Peter Woit', t, SW(10, 'separately') - 0.4, 1e9, { x: 1420, y: 560, w: 700, size: 38 });
});

board('facts', SW(10, 'facts') - 0.4, 'sage', 1041, (g, t, S) => {
  title(g, 960, 110, 'what the documents agree on', t, SW(10, 'facts'), 1e9, 50, PAL.ink, { shadow: false });
  checklist(g, 340, 260, [['a theory that obeys the rules', '✓', SW(10, 'obeys')], ['matches Yang–Mills in every test the panel ran', '✓', SW(10, 'matches')], ['the only one', '?', SW(10, 'only', 2)], ['counts as the existence half?', '?', SW(10, 'counts')]], t, { size: 40, dy: 120 });
  tag(g, 960, 790, 'the group: “no present connection to the Clay Mathematics Institute”', t, SW(10, 'connection'), 1e9, { size: 30 });
  tag(g, 960, 900, 'only Clay rules on the prize · and it needs both halves', t, SW(10, 'halves'), 1e9, { size: 32, bg: '#fbe2c0' });
  title(g, 960, 1010, 'back to Pauli', t, SW(10, 'pauli'), 1e9, 44, PAL.ink, { shadow: false });
});

// ================= 8. The machines arrive =================
deepScene('ai-rise', SB(8) - 0.3, 801, (g, t, S) => {
  title(g, 960, 200, '2025 – 2026', t, SB(8), 1e9, 80);
  tag(g, 560, 450, 'school maths competitions', t, SW(8, 'struggling'), 1e9, { size: 38 });
  arrow(g, 840, 450, 1060, 450, 6, '#e8eef4');
  tag(g, 1360, 450, 'research mathematics', t, SW(8, 'research'), 1e9, { size: 38, bg: '#fbe2c0' });
  tag(g, 960, 700, 'target: the Riemann hypothesis', t, SW(8, 'sights'), 1e9, { size: 44 });
});

board('two-thirds', SW(8, 'august') - 0.4, 'warm', 811, (g, t, S) => {
  tag(g, 960, 120, 'August 2026 · arXiv 2608.13637', t, SW(8, 'august'), 1e9, { size: 40 });
  tag(g, 960, 250, 'Levent Alpöge & Ralph Furman', t, SW(8, 'authors'), 1e9, { size: 36 });
  tag(g, 960, 380, '"More than two thirds of the zeta zeros are\nsimple and on the critical line"', t, SW(8, 'title'), SW(8, 'remember') + 0.2, { size: 38, italic: true });
  const keys = [[SW(8, 'remember'), 0.3434], [SW(8, 'two', 2), 0.4088], [SW(8, 'percent'), 5 / 12], [SW(8, 'claimed'), 2 / 3]];
  let v = 0; for (let i = 0; i < keys.length; i++) if (t > keys[i][0]) v = lerp(i ? keys[i - 1][1] : 0, keys[i][1], E.out3(seg(t, keys[i][0], keys[i][0] + (i === 3 ? 1.8 : 0.8))));
  if (t > SW(8, 'remember')) fracMeter(g, 220, 560, 1400, v, [[1 / 3, '1974', SW(8, 'remember'), false, -40], [0.4, '1989', SW(8, 'two', 2), true], [5 / 12, '2020', SW(8, 'percent'), false, 50], [2 / 3, 'Aug 2026: 2/3', SW(8, 'claimed'), true]], t, { col: PAL.red });
  quoteCard(g, 'Proof discovered autonomously by Claude (Anthropic); verified and communicated by the listed authors.', 'arXiv comment on the paper', t, SW(8, 'autonomously') - 0.6, SW(8, 'weeks') - 0.3, { y: 820, size: 38, w: 1300 });
  tag(g, 960, 870, 'within weeks: improved · a shorter human proof (Lamzouri)', t, SW(8, 'weeks'), 1e9, { size: 32 });
}, { cam: { z0: 1.0, z1: 1.03 } });

deepScene('quasi', SW(8, 'october') - 0.4, 821, (g, t, S) => {
  const map = stripPlane(g, 640, 120, 640, 860, { s0: -0.1, s1: 1.1, t0: 0, t1: 60, ink: '#e8eef4', lineCol: '#ffcf7a', stripCol: '#6fa8dc' });
  ZEROS.slice(0, 14).forEach(gm => { const [x, y] = map(0.5, gm); zeroLantern(g, x, y, 7); });
  tag(g, 960, 60, 'October 2026 · OpenAI: hundreds of AI-written papers', t, SW(8, 'october'), 1e9, { size: 34 });
  const band = (a, b, tIn, col) => { if (t < tIn) return; const k = seg(t, tIn, tIn + 0.8), [x0] = map(a, 0), [x1] = map(b, 0); g.fillStyle = css(col, 0.45 * k); g.fillRect(x0, 120, x1 - x0, 860); };
  band(7 / 8, 1, SW(8, 'eighths'), '#ff8a5a'); band(0, 1 / 8, SW(8, 'eighths') + 0.4, '#ff8a5a');
  band(11 / 12, 7 / 8, SW(8, 'twelfths') + 99, '#ff8a5a');
  tag(g, 1560, 300, 'no zeros beyond 7/8\n(199 pages)', t, SW(8, 'eighths'), SW(8, 'lichtman') - 0.5, { size: 32 });
  tag(g, 1560, 470, 'shorter paper: 11/12', t, SW(8, 'twelfths'), SW(8, 'lichtman') - 0.5, { size: 30 });
  tag(g, 330, 500, 'still not the line itself', t, SW(8, 'line', 2), SW(8, 'lichtman') - 0.5, { size: 30 });
  tag(g, 1560, 660, 'the "quasi Riemann hypothesis"', t, SW(8, 'quasi'), SW(8, 'lichtman') - 0.5, { size: 32, bg: '#fbe2c0' });
  quoteCard(g, 'the most significant AI-for-math achievement so far, by quite a margin.', 'Jared Duker Lichtman, Stanford, to Quanta', t, SW(8, 'lichtman'), SW(8, 'months') - 0.3, { size: 46, y: 560 });
  title(g, 960, 1030, 'claimed results · not yet peer reviewed', t, SW(8, 'eighths'), 1e9, 28);
});

board('summit', SW(8, 'months') - 0.4, 'blue', 831, (g, t, S) => {
  title(g, 960, 200, 'three months: two old walls moved', t, SW(8, 'months'), 1e9, 60, PAL.ink, { shadow: false });
  tag(g, 600, 450, '5/12 → 2/3 on the line', t, SW(8, 'walls'), 1e9, { size: 42 });
  tag(g, 1320, 450, 'a band of the strip emptied', t, SW(8, 'walls') + 0.4, 1e9, { size: 42 });
  stamp(g, 960, 650, 'FIRST PUSH: A MACHINE', t, SW(8, 'machine'), 1e9, { size: 50 });
  tag(g, 960, 880, 'how close to the summit?', t, SW(8, 'summit'), 1e9, { size: 42, bg: '#fbe2c0' });
});

// ================= 9. Sunday night =================
board('note2', SB(9) - 0.3, 'warm', 901, (g, t, S) => {
  // a four-page note, drawn as paper sheets (no likeness of anyone)
  for (let k = 0; k < 4; k++) { const x = 360 + k * 46, y = 330 + k * 30, tk = SB(9) + 0.3 + k * 0.2; if (t < tk) continue; const sh = rough([[x - 180, y - 240], [x + 180, y - 240], [x + 180, y + 240], [x - 180, y + 240]], 2, k, 12); fillPts(g, offsetPts(sh, 8, 10), css('#000', 0.18)); cels(g, sh, { base: '#fbf6ea', sh: '#e3d8c2', sw: 5, L: LIGHT, line: PAL.ink, lw: 2.2, seed: k }); for (let l = 0; l < 14; l++) ink(g, [[x - 140, y - 190 + l * 28], [x + 140 - hash(k * 20 + l, 3) * 90, y - 190 + l * 28]], 3, { col: '#8a7a68', a: 0.5, t0: 0, t1: 0, seed: l }); }
  tag(g, 1260, 160, 'Friday 20 November 2026', t, SW(9, 'friday'), 1e9, { size: 40 });
  tag(g, 1260, 300, 'number theorist · PhD Princeton · now at Anthropic', t, SW(9, 'princeton'), 1e9, { size: 30 });
  tag(g, 1260, 430, 'a secret goal since student days', t, SW(9, 'student'), 1e9, { size: 32 });
  quoteCard(g, 'Claude far outpaced me near the finish line. No, Claude flew, and danced, and sparkled near that finish line.', 'Levent Alpöge, remark (20 Nov 2026)', t, SW(9, 'words') - 0.2, SW(9, 'timeline') - 0.3, { x: 1260, y: 700, w: 800, size: 40 });
});

board('calendar', SW(9, 'timeline') - 0.4, 'sage', 911, (g, t, S) => {
  const t0 = SW(9, 'sixth');
  tag(g, 960, 120, 'timeline in the note (compiled by Claude, he says)', t, SW(9, 'timeline'), 1e9, { size: 34 });
  // 6 Oct .. 16 Nov = 42 days as a strip of day cells
  for (let d = 0; d <= 41; d++) { const tk = t0 + d * 0.05; if (t < tk) continue; const x = 160 + (d % 21) * 76, y = 300 + Math.floor(d / 21) * 110, last = d === 41, sun = d === 40; fillPts(g, rough([[x, y], [x + 66, y], [x + 66, y + 90], [x, y + 90]], 1.2, d, 8), css(last ? PAL.red : sun ? PAL.gold : '#f6eedc', 0.95)); g.font = '600 26px Georgia'; g.fillStyle = last ? '#fff' : PAL.ink; g.textAlign = 'center'; g.fillText(d < 26 ? `${6 + d}` : `${d - 25}`, x + 33, y + 52); }
  tag(g, 300, 560, '6 Oct', t, t0, 1e9, { size: 30 }); tag(g, 1620, 560, '16 Nov: "GRH"', t, SW(9, 'sixteenth'), 1e9, { size: 30, bg: '#fbe2c0' });
  tag(g, 960, 700, '≈ six weeks', t, SW(9, 'weeks'), 1e9, { size: 48 });
  tag(g, 560, 880, 'late Sunday night', t, SW(9, 'sunday'), 1e9, { size: 36 });
  tag(g, 1360, 880, 'a message to the CEO: may I announce?', t, SW(9, 'dario'), 1e9, { size: 32 });
  tag(g, 960, 1010, 'he says: the Generalized RH too', t, SW(9, 'further'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('chain', SW(9, 'remember') - 0.4, 'warm', 921, (g, t, S) => {
  tag(g, 960, 110, "a descendant of Stepanov's argument", t, SW(9, 'descendant'), 1e9, { size: 40 });
  tag(g, 960, 230, 'multiplicity, mimicked by a chain of "theta lifts" (schematic)', t, SW(9, 'theta'), 1e9, { size: 32 });
  const tS = SW(9, 'square'), X = t < tS ? 0 : lerp(0, 1e6, Math.pow(seg(t, tS, tS + 4), 3)), n = t < SW(9, 'theta') ? 0 : Math.max(5, Math.min(34, Math.round(Math.sqrt(X) / 30) + 5));
  ringChain(g, 140, 470, 1640, n, t, SW(9, 'theta'), { dt: 0.05 });
  tag(g, 960, 650, 'each link ≈ one extra layer of multiplicity', t, SW(9, 'link'), SW(9, 'chairs') - 0.2, { size: 30 });
  tag(g, 960, 650, 'how long should the chain be?', t, SW(9, 'long'), 1e9, { size: 40 });
  if (t > tS) tag(g, 960, 800, `primes up to X = ${Math.round(X).toLocaleString('en-US')}  →  chain length √X = ${Math.round(Math.sqrt(X)).toLocaleString('en-US')}`, t, tS, 1e9, { size: 38, bg: '#fbe2c0', rot: 0 });
  quoteCard(g, '(And yet, what else could it be?)', 'Levent Alpöge, on Claude\'s suggestion of √X', t, SW(9, 'else') - 0.3, SW(9, 'same') - 0.3, { size: 54, y: 560 });
});

board('sqrt-echo', SW(9, 'same') - 0.4, 'blue', 931, (g, t, S) => {
  const tS = SW(9, 'same'), map = axes(g, 200, 900, 1400, 640, { x0: 0, x1: 20000, y0: 0, y1: 2400, xl: 'N', yl: 'primes up to N', a: seg(t, tS - 0.3, tS), fs: 28 });
  if (t > tS) { plot(g, map, x => Li(x) + Math.sqrt(x) * Math.log(x) / (8 * Math.PI), 2700, 20000, seg(t, tS, tS + 1.5), { col: PAL.red, w: 3, a: 0.6 }); plot(g, map, x => Li(x) - Math.sqrt(x) * Math.log(x) / (8 * Math.PI), 2700, 20000, seg(t, tS, tS + 1.5), { col: PAL.red, w: 3, a: 0.6 }); plot(g, map, piCount, 2, 20000, seg(t, tS, tS + 2), { col: PAL.ink, w: 3, n: 800 }); }
  tag(g, 1300, 260, 'the √-sized band RH promises', t, tS + 0.6, 1e9, { size: 34, col: PAL.red });
  tag(g, 600, 260, 'chain length matches the size of the answer\n(in his account)', t, SW(9, 'account'), 1e9, { size: 30, bg: '#fbe2c0' });
  stamp(g, 960, 980, 'A STORY · WHERE IS THE PROOF?', t, SW(9, 'story'), 1e9, { size: 40 });
});

// ================= 10. Seventeen million lines =================
deepScene('lean', SB(10) - 0.3, 1001, (g, t, S) => {
  const T0 = S._sh.t0;
  codeScroll(g, 140, 140, 1060, 780, LEAN, t, T0, { speed: 40, bad: k => t < SW(10, 'compile') + 1 && t > SW(10, 'follow') && k === 6 });
  tag(g, 670, 980, 'illustrative Lean-style code, not the real proof', t, T0 + 0.3, 1e9, { size: 24 });
  tag(g, 1560, 260, 'Lean: a proof assistant', t, SW(10, 'assistant'), 1e9, { size: 36 });
  tag(g, 1560, 420, 'the kernel checks\nevery single step', t, SW(10, 'kernel'), 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 1560, 600, 'no "it is easy to see"', t, SW(10, 'easy'), 1e9, { size: 32 });
  stamp(g, 1560, 780, "ONE BAD STEP: WON'T COMPILE", t, SW(10, 'compile'), 1e9, { size: 32 });
});

board('mathlib', SW(10, 'decade') - 0.4, 'sage', 1011, (g, t, S) => {
  const tM = SW(10, 'decade'); title(g, 960, 110, 'Mathlib: a shared library of formalised mathematics', t, SW(10, 'mathlib'), 1e9, 48, PAL.ink, { shadow: false });
  // a growing tree of theorem boxes
  const n = Math.floor(lerp(0, 63, seg(t, tM, tM + 5))); for (let i = 1; i <= n; i++) { const lv = Math.floor(Math.log2(i)), k = i - (1 << lv), cnt = 1 << lv, x = 960 + (k - (cnt - 1) / 2) * (1500 / cnt), y = 900 - lv * 120; if (i > 1) { const p = i >> 1, plv = lv - 1, pk = p - (1 << plv), pc = 1 << plv; ink(g, [[x, y], [960 + (pk - (pc - 1) / 2) * (1500 / pc), y + 120]], 2, { col: PAL.ink, a: 0.4, t0: 0, t1: 0 }); } fillPts(g, rough([[x - 16, y - 12], [x + 16, y - 12], [x + 16, y + 12], [x - 16, y + 12]], 1, i, 6), css(PAL.teal, 0.85)); }
  tag(g, 960, 230, 'already contains: RiemannHypothesis (a precise definition)', t, SW(10, 'definition'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('size', SW(10, 'seventeen') - 0.6, 'warm', 1021, (g, t, S) => {
  tag(g, 960, 300, rollNum(t, SW(10, 'seventeen') - 0.4, SW(10, 'pieces'), 0, 17e6) + ' lines', t, SW(10, 'seventeen') - 0.4, 1e9, { size: 110 });
  for (let k = 0; k < 14; k++) { const tk = SW(10, 'fourteen') + k * 0.08; if (t < tk) continue; const x = 300 + k * 102; envelope(g, x, 620, 86, '', 0, {}); g.font = '600 22px Georgia'; g.fillStyle = PAL.ink; g.textAlign = 'center'; g.fillText(`part ${String(k + 1).padStart(2, '0')}`, x, 700); }
  tag(g, 960, 860, 'depends directly only on Mathlib', t, SW(10, 'depends'), 1e9, { size: 36, bg: '#fbe2c0' });
});

board('two-ways', SW(10, 'wrong') - 0.4, 'blue', 1031, (g, t, S) => {
  title(g, 960, 120, 'two ways a computer-checked proof can still fail', t, SW(10, 'wrong'), 1e9, 48, PAL.ink, { shadow: false });
  tag(g, 520, 360, '1 · a hidden assumption', t, SW(10, 'assumption'), 1e9, { size: 40 });
  tag(g, 1400, 360, '2 · the wrong statement', t, SW(10, 'subtly'), 1e9, { size: 40, bg: '#fbe2c0' });
  writeText(g, 'proved:   ∀ ρ, ζ ρ = 0 → … → ρ.re = 1/2', 1400, 560, 34, t, SW(10, 'statement', 2), 1.0, { align: 'center', upright: true });
  writeText(g, 'Mathlib:  RiemannHypothesis', 1400, 640, 34, t, SW(10, 'statement', 3), 1.0, { align: 'center', upright: true });
  tag(g, 1400, 760, 'must match exactly (schematic)', t, SW(10, 'statement', 3) + 0.8, 1e9, { size: 26 });
  stamp(g, 1400, 900, 'OTHERWISE WORTHLESS', t, SW(10, 'worthless'), 1e9, { size: 40 });
});

board('checking', SW(10, 'hours') - 0.6, 'sage', 1041, (g, t, S) => {
  agentGrid(g, 160, 160, 900, 560, 24, 15, t, SW(10, 'forum'), { spread: 1.2, col: '#6fd08a' });
  tag(g, 610, 800, 'downloaded and compiled on their own machines', t, SW(10, 'downloaded'), 1e9, { size: 30 });
  tag(g, 1480, 260, 'checking began within hours, in public', t, SW(10, 'hours'), 1e9, { size: 30 });
  quoteCard(g, 'Honestly the chance that anything is fishy here is approximately zero.', 'Andrew Sutherland, MIT, to Scientific American', t, SW(10, 'honestly') - 0.3, SW(10, 'twentyfirst') - 0.4, { x: 1400, y: 600, w: 760, size: 40 });
});

board('certified', SW(10, 'twentyfirst') - 0.4, 'warm', 1051, (g, t, S) => {
  tag(g, 960, 110, '21 November 2026 · Lean & Mathlib communities', t, SW(10, 'twentyfirst'), 1e9, { size: 38 });
  tag(g, 960, 220, 'endorsed by the Lean FRO and the Mathlib Initiative', t, SW(10, 'endorsed'), 1e9, { size: 30 });
  tag(g, 520, 380, 'statement = Mathlib\'s RH\n(two comparison tools)', t, SW(10, 'comparison'), SW(10, 'hereby') - 0.4, { size: 32 });
  tag(g, 1400, 380, "proof accepted: Lean's kernel\n+ independent checkers", t, SW(10, 'kernel', 2), SW(10, 'hereby') - 0.4, { size: 32 });
  quoteCard(g, 'We hereby certify that both the statement and the proof of the Riemann Hypothesis are correct.', 'Lean and Mathlib communities, joint statement', t, SW(10, 'hereby') - 0.6, 1e9, { y: 560, size: 48 });
  tag(g, 560, 930, '167 years of waiting', t, SW(10, 'waiting'), 1e9, { size: 44 });
  tag(g, 1360, 930, '1 day of checking', t, SW(10, 'day', 2), 1e9, { size: 44, bg: '#fbe2c0' });
});

deepScene('unease', SW(10, 'yet') - 0.3, 1061, (g, t, S) => {
  title(g, 960, 420, 'so why the unease?', t, SW(10, 'unease') - 0.3, 1e9, 84);
  tag(g, 960, 620, 'the end of the story… isn\'t', t, SW(10, 'story'), 1e9, { size: 40 });
});

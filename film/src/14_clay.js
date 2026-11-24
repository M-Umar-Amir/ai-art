// ================= 7. A million dollars for "does it exist?" =================
board('clay', SB(7) - 0.3, 'warm', 701, (g, t, S) => {
  tag(g, 960, 120, 'Paris · 24 May 2000 · Clay Mathematics Institute', t, SW(7, 'paris'), 1e9, { size: 40 });
  const tE = SW(7, 'seven'), names = ['Poincaré', 'Navier–Stokes', 'Riemann', 'Yang–Mills', 'P vs NP', 'Hodge', 'BSD'];
  names.forEach((n, k) => { const tk = tE + k * 0.12; if (t < tk) return; const open = k === 0 ? seg(t, SW(7, 'solved') - 0.2, SW(7, 'solved') + 0.4) : 0; g.save(); g.globalAlpha = seg(t, tk, tk + 0.25); envelope(g, 250 + k * 237, 480, 200, n, open, { glow: k === 3 ? 1 : (k === 1 || k === 2) ? seg(t, SW(7, 'videos'), SW(7, 'videos') + 0.6) * 0.5 : 0 }); g.restore(); });
  tag(g, 960, 300, '$1,000,000 each', t, SW(7, 'million'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 330, 720, 'solved: Grigori Perelman\n(turned the money down)', t, SW(7, 'perelman'), 1e9, { size: 28 });
  tag(g, 770, 720, 'our last two videos', t, SW(7, 'videos'), 1e9, { size: 28 });
  stamp(g, 960, 900, 'THIS ONE', t, SW(7, 'riemann') + 0.8, 1e9, { size: 52, rot: 0.04 });
});

board('statement', SW(7, 'official') - 0.4, 'blue', 711, (g, t, S) => {
  tag(g, 960, 130, 'the official problem · Arthur Jaffe & Edward Witten', t, SW(7, 'jaffe'), 1e9, { size: 38 });
  quoteCard(g, 'Prove that for any compact simple gauge group G, a non-trivial quantum Yang–Mills theory exists on ℝ⁴ and has a mass gap Δ > 0.', 'Jaffe & Witten, Clay Mathematics Institute', t, SW(7, 'words'), 1e9, { y: 560, size: 50 });
});

board('halves', SW(7, 'halves') - 0.4, 'warm', 721, (g, t, S) => {
  title(g, 520, 150, '1 · EXISTENCE', t, SW(7, 'existence'), 1e9, 60, PAL.ink, { shadow: false, upright: true, weight: 700 });
  tag(g, 520, 270, 'build it rigorously in 4D spacetime', t, SW(7, 'rigorously'), 1e9, { size: 30 });
  checklist(g, 160, 420, [['energy is never negative', '✓', SW(7, 'negative')], ['causes come before effects', '✓', SW(7, 'causes')], ['same laws for every steady observer', '✓', SW(7, 'steady')]], t, { size: 34 });
  tag(g, 520, 820, 'the Wightman · Osterwalder–Schrader axioms', t, SW(7, 'wightman'), 1e9, { size: 30, bg: '#fbe2c0' });
  title(g, 1420, 150, '2 · MASS GAP', t, SW(7, 'second'), 1e9, 60, PAL.ink, { shadow: false, upright: true, weight: 700 });
  if (t > SW(7, 'smallest') - 0.4) spectrum(g, 1220, 820, 400, 520, 0.3, seg(t, SW(7, 'smallest') - 0.4, SW(7, 'smallest') + 1.4), { vac: 'empty space' });
});

deepScene('not-one', SW(7, 'hard') - 0.4, 731, (g, t, S) => {
  quoteCard(g, 'at present we do not know any non-trivial relativistic field theory that satisfies the Wightman (or any other reasonable) axioms in four dimensions.', 'Jaffe & Witten (2000)', t, SW(7, 'present'), 1e9, { y: 440, size: 46 });
  stamp(g, 960, 860, 'NOT ONE', t, SW(7, 'one', 4), 1e9, { size: 80, col: '#ffcf7a' });
});

board('trivial', SW(7, 'trap') - 0.4, 'sage', 741, (g, t, S) => {
  tag(g, 960, 110, 'the simplest interacting theory: one field that bounces off itself', t, SW(7, 'bounces'), 1e9, { size: 34 });
  // schematic: the effective interaction at fixed physics drains like 1 / log(1/spacing) (marginal triviality)
  const map = axes(g, 280, 900, 1100, 600, { x0: 0, x1: 10, y0: 0, y1: 1.1, xl: 'finer grid →', yl: 'strength of the interaction', fs: 30 });
  const tD = SW(7, 'drains'); if (t > tD - 0.6) plot(g, map, x => 1 / (1 + 0.9 * x), 0, 10, seg(t, tD - 0.6, tD + 1.8), { col: PAL.red, w: 6 });
  tag(g, 1560, 280, 'Michael Aizenman &\nHugo Duminil-Copin · 2021', t, SW(7, 'eyezenman'), 1e9, { size: 30 });
  tag(g, 1560, 470, 'in 4D: a free field is left', t, SW(7, 'free'), 1e9, { size: 30 });
  stamp(g, 1560, 640, 'TRIVIAL', t, SW(7, 'trivial'), 1e9, { size: 64 });
  tag(g, 800, 240, 'schematic', t, tD, 1e9, { size: 24 });
  tag(g, 960, 1010, 'Yang–Mills is expected to escape (asymptotic freedom) · expected ≠ proved', t, SW(7, 'escape'), 1e9, { size: 30, bg: '#fbe2c0' });
});

board('balaban', SW(7, 'eighties') - 0.6, 'warm', 751, (g, t, S) => {
  for (let k = 0; k < 6; k++) docPage(g, 330 + k * 40, 470 + k * 26, 380, 500, t, SW(7, 'papers') + k * 0.12, { head: k === 5 ? 'Comm. Math. Phys.' : '', lines: 10 });
  nameplate(g, 1300, 200, 'Tadeusz Bałaban', '1980s · a long series of papers', t, SW(7, 'bahlahbahn'), { w: 620 });
  quoteCard(g, 'a qualitative breakthrough', 'Jaffe & Witten, on his four-dimensional work', t, SW(7, 'qualitative'), SW(7, 'ideas') - 0.4, { x: 1300, y: 500, w: 640, size: 46 });
  tag(g, 1300, 720, 'control as the grid shrinks, inside a finite box', t, SW(7, 'control'), 1e9, { size: 30 });
  stamp(g, 1300, 860, 'NEVER FINISHED', t, SW(7, 'finished'), 1e9, { size: 50 });
  quoteCard(g, 'no present ideas point the direction', 'Jaffe & Witten, on the mass gap', t, SW(7, 'ideas'), 1e9, { x: 1300, y: 500, w: 640, size: 46 });
  title(g, 960, 1020, '… until this autumn', t, SW(7, 'autumn'), 1e9, 48, PAL.ink, { shadow: false });
});

// ================= 8. The announcement =================
board('post', SB(8) - 0.3, 'blue', 801, (g, t, S) => {
  tag(g, 960, 130, 'OpenAI · 26 October 2026', t, SW(8, 'published'), 1e9, { size: 40 });
  tag(g, 960, 250, '“Working with academic communities to build scientific understanding”', t, SW(8, 'understanding') - 0.8, 1e9, { size: 32, italic: true });
  quoteCard(g, 'a mathematically rigorous existence proof of certain interacting quantum field theories', 'OpenAI', t, SW(8, 'rigorous') - 0.6, 1e9, { y: 520, size: 46 });
  tag(g, 560, 830, 'also claimed: the strong force with quarks (QCD)', t, SW(8, 'quarks'), 1e9, { size: 30 });
  tag(g, 1360, 830, '“a unified completion of the Standard Model”', t, SW(8, 'unified'), 1e9, { size: 30, italic: true });
  tag(g, 960, 980, 'not part of the review that followed (pure Yang–Mills only)', t, SW(8, 'unified') + 2, 1e9, { size: 26, bg: '#fbe2c0' });
});

board('timeline', SW(8, 'timeline') - 0.4, 'warm', 811, (g, t, S) => {
  ribbon(g, 160, 1760, 470, t, SW(8, 'timeline'));
  pin(g, 300, 470, '9 Sep · written proof', t, SW(8, 'ninth'), { size: 28 });
  pin(g, 700, 470, '16 Oct · Lean version done', t, SW(8, 'sixteenth'), { size: 28, up: 130 });
  pin(g, 1080, 470, '26 Oct · announced', t, SW(8, 'sixteenth') + 1.2, { size: 28 });
  pin(g, 1420, 470, '28 Oct · code to the reviewers', t, SW(8, 'handed'), { size: 28, up: 130 });
  pin(g, 1720, 470, '10 Nov · assessment', t, SW(8, 'handed') + 0.6, { size: 28 });
  tag(g, 700, 800, 'Lean: a language that checks every logical step', t, SW(8, 'lean'), 1e9, { size: 30 });
  if (t > SW(8, 'pages') - 0.5) { docPage(g, 1400, 820, 300, 340, t, SW(8, 'pages') - 0.5, { head: '1,147 pp.', lines: 6 }); stamp(g, 1400, 820, 'UNRELEASED', t, SW(8, 'released'), 1e9, { size: 40, rot: -0.2 }); tag(g, 1400, 1020, 'reported length', t, SW(8, 'pages'), 1e9, { size: 24 }); }
});

board('shepherds', SW(8, 'shepherding') - 0.8, 'sage', 821, (g, t, S) => {
  title(g, 960, 120, '“shepherding” the result', t, SW(8, 'shepherding'), 1e9, 56, PAL.ink, { shadow: false });
  nameplate(g, 960, 300, 'David Gross · chair', 'asymptotic freedom (Nobel 2004)', t, SW(8, 'gross'), { w: 640 });
  nameplate(g, 480, 520, 'Arthur Jaffe', 'co-wrote the official problem', t, SW(8, 'jaffe'), { w: 560 });
  nameplate(g, 1440, 520, 'Michael Douglas', 'Harvard', t, SW(8, 'douglas'), { w: 520 });
  nameplate(g, 480, 760, 'Igor Klebanov', 'leads a collaboration on confinement', t, SW(8, 'klebahnoff'), { w: 640 });
  nameplate(g, 1440, 760, 'Nikita Nekrasov', 'mathematical physicist', t, SW(8, 'neckrahsoff'), { w: 520 });
  tag(g, 960, 980, 'no faces: names only', t, SW(8, 'gross') + 0.5, 1e9, { size: 24 });
});

deepScene('flood', SW(8, 'same') - 0.4, 831, (g, t, S) => {
  const cards = [['pure Yang–Mills: existence', SW(8, 'same')], ['local smoothing (pure mathematics)', SW(8, 'smoothing')], ['“the exponential time hypothesis is false”', SW(8, 'exponential')]];
  cards.forEach(([txt, tk], k) => { if (t < tk) return; const y = 300 + k * 200, drop = 1 - E.out3(seg(t, tk, tk + 0.5)); tag(g, 960 + (k - 1) * 160, y - 120 * drop, txt, t, tk, 1e9, { size: 40, bg: k ? '#f3e8d2' : '#fbe2c0' }); });
  title(g, 960, 920, 'a flood', t, SW(8, 'flood'), 1e9, 90, '#fff1cf');
});

board('critics', SW(8, 'impressed') - 0.4, 'warm', 841, (g, t, S) => {
  quoteCard(g, 'From this press release, it’s hard to know exactly what they’re claiming to have proved.', 'Peter Woit, Not Even Wrong (26 Oct)', t, SW(8, 'woyt'), SW(8, 'secrecy') - 0.3, { y: 520, size: 48 });
  tag(g, 960, 150, 'others objected to the secrecy', t, SW(8, 'secrecy'), 1e9, { size: 38 });
  quoteCard(g, 'This review can be done more efficiently and quickly in the open.', 'Scott Armstrong, NYU, to Scientific American', t, SW(8, 'armstrong'), 1e9, { y: 560, size: 50 });
});

board('disclosure', SW(8, 'disclosure') - 0.4, 'blue', 851, (g, t, S) => {
  tag(g, 960, 300, 'DISCLOSURE', t, SW(8, 'disclosure'), SW(8, 'tenth') - 0.4, { size: 60, bg: '#fbe2c0' });
  tag(g, 960, 470, 'researched and written with Claude (Anthropic), a competitor of OpenAI', t, SW(8, 'claude'), SW(8, 'tenth') - 0.4, { size: 34 });
  tag(g, 960, 600, 'every judgement: quoted from named people and documents (links below)', t, SW(8, 'judgement'), SW(8, 'tenth') - 0.4, { size: 32 });
  docPage(g, 960, 540, 760, 820, t, SW(8, 'tenth'), { head: 'Validation of the OpenAI Yang–Mills construction', hs: 28, lines: 15 });
  tag(g, 960, 980, '10 November 2026 · 35 pages · with the Lean code', t, SW(8, 'assessment'), 1e9, { size: 32 });
});

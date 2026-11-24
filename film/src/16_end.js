// ================= 11. The half that's missing =================
chalkScene('pauli2', SB(11) - 0.3, 1101, (g, t, S) => {
  chalkText(g, 'What is the mass of this field?', 960, 260, 84, t, SB(11) + 0.2, 1.4, { align: 'center', upright: false });
  tag(g, 960, 430, 'the mass gap = Pauli’s question, in modern form', t, SW(11, 'modern'), 1e9, { size: 36 });
  tag(g, 960, 540, 'is the lightest thing heavier than zero?', t, SW(11, 'lightest'), 1e9, { size: 34 });
  quoteCard(g, 'The construction provides no evidence for or against a mass gap in the theory.', 'the Yang–Mills group, assessment 1', t, SW(11, 'evidence') - 0.6, 1e9, { y: 780, size: 44 });
});

board('decay', SW(11, 'show') - 0.4, 'warm', 1111, (g, t, S) => {
  // log plot of a disturbance vs distance: exp(-m r) (gap) vs a power law (no gap)
  const map = axes(g, 260, 900, 1150, 640, { x0: 0, x1: 10, y0: -6, y1: 0.2, xl: 'distance', yl: 'disturbance (log scale)', fs: 30 });
  const tE = SW(11, 'exponentially'); if (t > tE - 0.5) plot(g, map, r => Math.log10(Math.exp(-1.3 * r)), 0, 10, seg(t, tE - 0.5, tE + 1.4), { col: PAL.red, w: 6, clipY: 260 });
  const tS = SW(11, 'slowly'); if (t > tS - 0.3) plot(g, map, r => Math.log10(1 / Math.pow(1 + r, 2)), 0, 10, seg(t, tS - 0.3, tS + 1.4), { col: PAL.blue, w: 6 });
  tag(g, 1600, 760, 'mass gap: a straight plunge', t, SW(11, 'plunging'), 1e9, { size: 32 });
  tag(g, 1600, 420, 'no gap: it flattens out', t, SW(11, 'flattens'), 1e9, { size: 32 });
  tag(g, 960, 120, 'simulations show the plunge', t, SW(11, 'plunge'), 1e9, { size: 38 });
  tag(g, 960, 1010, 'a proof must show it for every size of box, out to infinity', t, SW(11, 'infinity'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('zones', SW(11, 'harder') - 0.4, 'blue', 1121, (g, t, S) => {
  const map = axes(g, 260, 900, 1250, 640, { x0: -3, x1: 0.4, y0: 0, y1: 0.9, xl: 'distance (fm, log)', yl: 'strength of the force', fs: 30 });
  const [xs] = map(-3, 0), [xm] = map(-1, 0), [xe] = map(0, 0);
  const tG = SW(11, 'gentle'); if (t > tG) { g.fillStyle = css('#6fd08a', 0.22 * seg(t, tG, tG + 0.5)); g.fillRect(xs, 260, xm - xs, 640); }
  const tSt = SW(11, 'strong'); if (t > tSt) { g.fillStyle = css(PAL.red, 0.2 * seg(t, tSt, tSt + 0.5)); g.fillRect(xm, 260, xe - xm, 640); }
  plot(g, map, e => alphaS(HBARC / Math.pow(10, e), 3, 0.25), -3, -0.25, seg(t, S._sh.t0 + 0.2, S._sh.t0 + 1.6), { col: PAL.ink, w: 6, clipY: 260 });
  tag(g, (xs + xm) / 2, 420, 'short distance: gentle\n(where the machine’s proof works)', t, SW(11, 'work'), 1e9, { size: 28 });
  tag(g, 1640, 560, 'long distance: strong, the rope\napproximations fail', t, SW(11, 'fail'), 1e9, { size: 28, bg: '#fbe2c0' });
  tag(g, 960, 1000, 'the gap isn’t visible classically: classical Yang–Mills waves are massless', t, SW(11, 'classical'), SW(11, 'viewpoint') - 0.2, { size: 30 });
  tag(g, 960, 1000, 'it may need “a completely new change of viewpoint” (after Jaffe & Witten)', t, SW(11, 'viewpoint'), 1e9, { size: 30 });
});

board('open-list', SW(11, 'list') - 0.4, 'warm', 1131, (g, t, S) => {
  title(g, 960, 110, 'the panel’s open questions', t, SW(11, 'list'), 1e9, 56, PAL.ink, { shadow: false });
  checklist(g, 340, 250, [['prove the limit is unique', '?', SW(11, 'unique')], ['a proper probability rule for the continuum theory', '?', SW(11, 'probability')], ['a mass gap for every size of box', '?', SW(11, 'matter')], ['Wilson’s loops and the area law (the rope)', '?', SW(11, 'area')], ['the particles themselves', '?', SW(11, 'particles')]], t, { size: 40, dy: 130 });
});

board('queue', SW(11, 'trying') - 0.4, 'sage', 1141, (g, t, S) => {
  tag(g, 960, 150, 'V. P. Nair (Aug 2026): “the outlines of a proof” of a mass gap in 3D', t, SW(11, 'nair'), 1e9, { size: 34 });
  tag(g, 960, 260, 'a simpler cousin, and an outline, not a complete proof', t, SW(11, 'cousin'), 1e9, { size: 28, bg: '#fbe2c0' });
  title(g, 960, 420, 'the Yang–Mills group’s public queue', t, SW(11, 'submissions'), 1e9, 46, PAL.ink, { shadow: false });
  checklist(g, 380, 560, [['OpenAI · 28 Oct · assessed (existence)', '✓', SW(11, 'submissions')], ['OpenAI · 4 Nov · work in progress', '?', SW(11, 'fourth')], ['Alpöge and Sussman · 15 Nov · work in progress', '?', SW(11, 'fifteenth')]], t, { size: 38, dy: 120 });
  stamp(g, 1500, 940, 'CONTENTS NOT PUBLIC', t, SW(11, 'public', 2), 1e9, { size: 42 });
});

// ================= 12. Close =================
deepScene('first', SB(12) - 0.3, 1201, (g, t, S) => {
  title(g, 960, 170, 'if the panel is right…', t, SW(12, 'right'), 1e9, 64);
  tag(g, 960, 330, 'the first rigorous four-dimensional quantum field theory with real interactions', t, SW(12, 'first'), 1e9, { size: 34 });
  quoteCard(g, 'No rigorous definition of it existed.', 'Michael Douglas, to Scientific American', t, SW(12, 'existed') - 0.8, SW(12, 'more') - 0.2, { y: 640, size: 54, w: 1000 });
  quoteCard(g, 'Not any more.', 'Michael Douglas', t, SW(12, 'more') - 0.3, 1e9, { y: 640, size: 80, w: 700 });
});

board('chain', SW(12, 'produced') - 0.4, 'warm', 1211, (g, t, S) => {
  const steps = [['a machine produced it', SW(12, 'produced')], ['Lean checked it', SW(12, 'checked')], ['Astra helped read it', SW(12, 'third')], ['still not fully understood', SW(12, 'understood')]];
  steps.forEach(([txt, tk], k) => { const y = 220 + k * 210; tag(g, 960, y, txt, t, tk, 1e9, { size: 44, bg: k === 3 ? '#fbe2c0' : PAL.card }); if (k && t > tk) arrow(g, 960, y - 160, 960, y - 50, 6, PAL.ink, seg(t, tk, tk + 0.3)); });
});

chalkScene('board-end', SW(12, 'question') - 0.4, 1221, (g, t, S) => {
  chalkText(g, 'What is the mass of this field?', 960, 300, 92, t, SW(12, 'field', 2) - 0.6, 1.3, { align: 'center', upright: false });
  if (t > SW(12, 'seminar') - 0.5) { g.save(); g.globalAlpha = seg(t, SW(12, 'seminar') - 0.5, SW(12, 'seminar') + 0.3); chair(g, 1560, 780, 150); g.restore(); }
  tag(g, 520, 560, '1954 → 2026: still open', t, SW(12, 'open'), 1e9, { size: 40 });
  tag(g, 960, 720, 'the simulations give a number', t, SW(12, 'number'), 1e9, { size: 34 });
  tag(g, 960, 860, 'nobody, human or machine, can yet prove why it’s there', t, SW(12, 'nobody'), 1e9, { size: 34 });
});

deepScene('end', SW(12, 'gluons') - 0.4, 1231, (g, t, S) => {
  protonCel(g, 600, 500, 230, t, { field: 0.15, tubes: true });
  stamp(g, 600, 860, 'MASS = 0 · STILL STOPS', t, SW(12, 'stops'), 1e9, { size: 40, col: '#ffcf7a' });
  g.save(); g.fillStyle = css('#f3e8d2', 0.9 * seg(t, SW(12, 'somewhere') - 0.4, SW(12, 'somewhere') + 0.4)); fillPts(g, rough([[1160, 220], [1760, 220], [1760, 900], [1160, 900]], 2, 3, 12), css('#f3e8d2', 0.92 * seg(t, SW(12, 'somewhere') - 0.4, SW(12, 'somewhere') + 0.4))); g.restore();
  if (t > SW(12, 'somewhere') - 0.4) spectrum(g, 1260, 820, 380, 520, 0.34, seg(t, SW(12, 'somewhere'), SW(12, 'somewhere') + 2), { vac: '' });
  title(g, 960, 120, 'WHAT IS THE MASS OF THIS FIELD?', t, SW(12, 'problem') + 0.6, 1e9, 64, '#fff1cf', { upright: true, weight: 700 });
});

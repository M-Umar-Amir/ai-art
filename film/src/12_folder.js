// ================= 6. The folder =================
dshot('gh6', SB(6) - 0.2, 'github', { path: [[0, 'all'], [SW(6, 'holds'), 'VP ≠ VNP over every field']], hl: [['VP ≠ VNP over every field', SW(6, 'titled')]], src: 'github.com/anthropics/formal-math/tree/main/vp-vnp',
  tags: [[960, 920, 'a paper + a computer proof', SW(6, 'holds'), { size: 40, bg: '#fbe2c0' }]] });
dshot('p1_6', SW(6, 'author') - 0.3, 'paper_p1', { path: [[0, [0, 0, 1429, 700]], [SW(6, 'readme') - 0.2, 'discovered by an internal research Claude model']], hl: [['discovered by an internal research Claude model', SW(6, 'readme') + 0.4]], src: 'paper.pdf, page 1' });
dshot('nohuman6', SW(6, 'both') - 0.3, 'github', { path: [[0, 'no human mathematician has checked the proof']], hl: [['no human mathematician has checked the proof', SW(6, 'human')]], src: 'vp-vnp/README.md' });
flat('bound6', SW(6, 'claim') - 0.3, '#10131c', (g, t, S) => {
  kin(g, [['CIRCUIT SIZE  >  m^( c · ∛log m )', '#ffffff']], t, SW(6, 'power'), { size: 90, y: 380 });
  tag(g, 960, 560, 'every arithmetic circuit for the m × m permanent', t, SW(6, 'circuit'), 1e9, { size: 38 });
  tag(g, 960, 690, 'for some constant c > 0 and every large enough m', t, SW(6, 'constant'), 1e9, { size: 36 });
  tag(g, 960, 820, 'eventually beats any polynomial', t, SW(6, 'eventually'), 1e9, { size: 40, bg: '#fbe2c0' });
  stamp(g, 960, 960, 'VP ≠ VNP … IF IT’S RIGHT', t, SW(6, 'settled'), 1e9, { size: 50, col: '#ffcf5a' });
}, { punch: true });
board('prize6', SW(6, 'prize') - 0.3, 'warm', 611, (g, t, S) => {
  const names = ['Poincaré', 'Navier–Stokes', 'Riemann', 'Yang–Mills', 'P vs NP', 'Hodge', 'BSD'];
  names.forEach((n, k) => { envelope(g, 250 + k * 237, 420, 200, n, k === 0 ? 1 : 0, {}); });
  stamp(g, 960, 660, 'VP vs VNP: NOT ON THE LIST', t, SW(6, "clay's"), 1e9, { size: 50 });
  tag(g, 960, 880, 'but in algebraic computation: the big one', t, SW(6, 'big'), 1e9, { size: 44, bg: '#fbe2c0' });
});
board('fields6', SW(6, 'field', 2) - 0.3, 'blue', 621, (g, t, S) => {
  const F = [['real numbers', 'perm ✓'], ['complex numbers', 'perm ✓'], ['fields of prime size p (p odd)', 'perm ✓'], ['the world where 1 + 1 = 0', 'perm = det → use the HAMILTONIAN CYCLE polynomial']];
  F.forEach(([a, b], i) => { const tk = SW(6, 'field', 2) + i * 0.35; tag(g, 520, 260 + i * 170, a, t, tk, 1e9, { size: 40 }); tag(g, 1320, 260 + i * 170, b, t, i === 3 ? SW(6, 'hamiltonian') : tk + 0.2, 1e9, { size: i === 3 ? 30 : 38, bg: i === 3 ? '#fbe2c0' : PAL.card }); });
  tag(g, 960, 960, 'also for circuits that only approximate (border complexity) → touches GCT’s goal', t, SW(6, 'approximate'), 1e9, { size: 30 });
});
dshot('abs6', SW(6, 'abstract') - 0.3, 'paper_p1', { path: [[0, 'does not settle that conjecture']], hl: [['does not settle that conjecture', SW(6, 'settle')]], src: 'paper.pdf, abstract' });
board('flood6', SW(6, 'alone') - 0.3, 'warm', 631, (g, t, S) => {
  ribbon(g, 260, 1680, 520, t, SW(6, 'alone'));
  pin(g, 420, 520, '26–27 Oct · OpenAI, and separately\nAnthropic: “ETH is false”', t, SW(6, 'october'), { size: 26, up: 110 });
  pin(g, 900, 520, '20 Nov · Riemann claim', t, SW(6, 'twentieth'), { size: 28 });
  pin(g, 1250, 520, '22 Nov · Brilliant’s AI fleet\non P vs NP', t, SW(6, 'twenty-second'), { size: 26, up: 110 });
  pin(g, 1580, 520, '23 Nov · VP ≠ VNP', t, SW(6, 'brilliant') + 1.5, { size: 28, col: '#d79a2b' });
  quoteCard(g, 'it is safe to assume now that the exponential-time hypothesis is toast', 'Lance Fortnow, Computational Complexity blog (28 Oct), who had not checked the proof himself', t, SW(6, 'fortnow') + 0.5, SW(6, 'twentieth') - 0.3, { y: 800, size: 38, w: 1300 });
});
dshot('brill6', SW(6, 'brilliant') - 0.4, 'brilliant', { path: [[0, [0, 0, 1920, 1200]]], src: 'pequalnp.com (Brilliant) · P vs NP still listed open' });
kinShot('flood6k', SW(6, 'flood') - 0.3, [['THE FLOOD REACHED', '#ffffff'], ['COMPLEXITY THEORY', '#7fd0ff']], { bg: '#06080d', size: 120, stagger: 0.4 });

// ================= 7. How big is "more than"? =================
board('growth7', SB(7) - 0.3, 'blue', 701, (g, t, S) => {
  const map = axes(g, 260, 920, 1300, 700, { x0: 1, x1: 40, y0: 0, y1: 150, xl: 'log₂ m (grid size, log scale)', yl: 'log₂ (steps)', fs: 28 });
  const tP = SW(7, 'polynomial'), tE = SW(7, 'exponential'), tQ = SW(7, 'quasipolynomial');
  if (t > tP) plot(g, map, x => 3 * x, 1, 40, seg(t, tP, tP + 1.2), { col: '#2f5f8f', w: 6 });
  if (t > tE) plot(g, map, x => Math.pow(2, x), 1, 7.2, seg(t, tE, tE + 1), { col: PAL.red, w: 6, clipY: 220 });
  if (t > tQ - 2) plot(g, map, x => Math.pow(x, 4 / 3), 1, 40, seg(t, tQ - 2, tQ), { col: '#d79a2b', w: 7 });
  tag(g, 1640, 330, 'polynomial: m³', t, tP, 1e9, { size: 30 }); tag(g, 560, 260, 'exponential: 2ᵐ', t, tE, 1e9, { size: 30 });
  tag(g, 1640, 200, 'quasipolynomial (c = 1, illustration)', t, tQ, 1e9, { size: 28, bg: '#fbe2c0' });
});
flat('runners7', SW(7, 'runners') - 0.3, '#0f1a12', (g, t, S) => {
  const T0 = SW(7, 'runners'), u = clamp((t - T0) / 16);
  const lanes = [['polynomial (steady)', '#7fb4ff', v => 0.95 * v], ['new bound (creeping up)', '#ffcf5a', v => 1.35 * Math.pow(v, 1.7)], ['exponential', '#ff6a55', v => 0.02 * (Math.exp(4.6 * v) - 1)]];
  lanes.forEach(([lab, col, f], i) => { const y = 330 + i * 230; g.strokeStyle = 'rgba(255,255,255,0.25)'; g.lineWidth = 3; g.beginPath(); g.moveTo(140, y + 60); g.lineTo(1800, y + 60); g.stroke();
    const x = 160 + Math.min(1.08, f(u)) * 1500; particle(g, x, y, 36, col); tag(g, 360, y - 70, lab, t, T0 + i * 0.4, 1e9, { size: 30 }); });
  tag(g, 960, 120, 'illustration, not to scale', t, T0, 1e9, { size: 26 });
  stamp(g, 1300, 980, 'IT OVERTAKES EVERY STEADY RUNNER', t, SW(7, 'overtakes'), 1e9, { size: 40, col: '#ffcf5a' });
}, { punch: false });
qshot('gap7', SW(7, 'candid') - 0.3, 'Our lower bound is quasipolynomial, whereas the best upper bounds are exponential … Thus there is still plenty of room to tighten the bounds.', 'the paper, §1', { size: 46 });
flat('horizon7', SW(7, 'notice') - 0.3, '#0b1020', (g, t, S) => {
  const gr = g.createLinearGradient(0, 300, 0, 1080); gr.addColorStop(0, '#1a2a50'); gr.addColorStop(0.5, '#e8a060'); gr.addColorStop(0.52, '#20283a'); gr.addColorStop(1, '#0a0e18'); g.fillStyle = gr; g.fillRect(0, 300, W, 780);
  kin(g, [['“EVERY LARGE ENOUGH m”', '#ffffff']], t, SW(7, 'notice'), { size: 100, y: 260 });
  tag(g, 960, 640, 'kicks in eventually · no promise for today’s grid sizes', t, SW(7, 'promise'), 1e9, { size: 38 });
  tag(g, 960, 800, 'no practical computation changes', t, SW(7, 'practical'), 1e9, { size: 40, bg: '#fbe2c0' });
}, { punch: false });
board('oldnew7', SW(7, 'enormous') - 0.3, 'warm', 731, (g, t, S) => {
  tag(g, 560, 300, 'before: ~ m² log m', t, SW(7, 'floor'), 1e9, { size: 50 });
  tag(g, 1360, 300, 'now (claimed): beats every polynomial', t, SW(7, 'beats'), 1e9, { size: 44, bg: '#fbe2c0' });
  tag(g, 960, 640, 'by the paper’s own survey: nothing like it for unrestricted circuits', t, SW(7, 'survey'), 1e9, { size: 36 });
  kin(g, [['ENORMOUS', '#b8361f']], t, SW(7, 'enormous'), { size: 120, y: 860, stroke: 'rgba(0,0,0,0)' });
});

// ================= 8. Inside the proof =================
dshot('fig8', SB(8) - 0.3, 'paper_fig1', { path: [[0, [0, 0, 1429, 1500]], [SW(8, 'strange') - 0.3, [80, 120, 1270, 1150]]], hl: [['Figure 1. The structure of the proof', SW(8, 'shape')]], src: 'paper.pdf, Figure 1: the structure of the proof' });
board('adapter8', SW(8, 'first') - 0.3, 'warm', 811, (g, t, S) => {
  const tA = SW(8, 'adapter'), tP = SW(8, 'plug');
  cels(g, rough([[300, 400], [700, 400], [700, 680], [300, 680]], 2, 3, 10), { base: '#d79a2b', sh: '#a87420', sw: 8, L: LIGHT, line: PAL.ink, lw: 4, seed: 3 });
  g.font = '800 44px Georgia'; g.fillStyle = '#111'; g.textAlign = 'center'; g.fillText('small circuit', 500, 520); g.fillText('for PERMANENT?', 500, 580);
  ['special polynomial A', 'special polynomial B'].forEach((s, i) => { if (t < tP + i * 0.4) return; const y = 360 + i * 320; arrow(g, 720, 540, 1100, y + 40, 6, PAL.ink); tag(g, 1420, y + 40, s + ' → small circuit too', t, tP + i * 0.4, 1e9, { size: 32 }); });
  tag(g, 500, 300, 'a universal adapter', t, tA, 1e9, { size: 44 });
  stamp(g, 960, 960, 'THEN: THOSE CAN’T HAVE SMALL CIRCUITS', t, SW(8, 'proves'), 1e9, { size: 40 });
});
board('fork8', SW(8, 'second') - 0.3, 'sage', 821, (g, t, S) => {
  const tS = SW(8, 'flat'), tC = SW(8, 'contracting');
  ink(g, [[960, 1000], [960, 760]], 18, { col: '#6b5a44', t0: 0, t1: 0 });
  if (t > tS - 0.3) ink(g, [[960, 760], [600, 520], [700, 280], [960, 160]], 18, { col: '#6b5a44', t0: 0, t1: 0, a: seg(t, tS - 0.3, tS + 0.3) });
  if (t > tC - 0.3) ink(g, [[960, 760], [1320, 520], [1220, 280], [960, 160]], 18, { col: '#6b5a44', t0: 0, t1: 0, a: seg(t, tC - 0.3, tC + 0.3) });
  tag(g, 520, 520, 'FLAT CASE', t, tS, 1e9, { size: 44 }); tag(g, 1400, 520, 'CONTRACTING CASE', t, tC, 1e9, { size: 44 });
  tag(g, 960, 110, 'the permanent has no small circuits', t, SW(8, 'road'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 960, 930, 'at least one of two statements about tensors is true', t, SW(8, 'tensors'), 1e9, { size: 32 });
});
dshot('cases8', SW(8, 'admits') - 0.3, 'paper_cases', { path: [[0, 'We do not know which of the two hypotheses holds']], hl: [['We do not know which of the two hypotheses holds', SW(8, 'know')]], src: 'paper.pdf, §2.1',
  tags: [[960, 930, 'R = the real numbers', SW(8, 'real'), { size: 36 }]] });
flat('detective8', SW(8, 'detective') - 0.3, '#14161c', (g, t, S) => {
  kin(g, [['RAIN?', '#7fb4ff']], t, SW(8, 'rained'), { size: 120, x: 560, y: 360 }); kin(g, [['NO RAIN?', '#ffcf5a']], t, SW(8, 'rained') + 0.4, { size: 120, x: 1360, y: 360 });
  arrow(g, 560, 470, 900, 700, 8, '#e8eef4', seg(t, SW(8, 'evidence'), SW(8, 'evidence') + 0.4)); arrow(g, 1360, 470, 1020, 700, 8, '#e8eef4', seg(t, SW(8, 'evidence'), SW(8, 'evidence') + 0.4));
  kin(g, [['SAME SUSPECT', '#ff6a55']], t, SW(8, 'suspect'), { size: 110, y: 820 });
  tag(g, 960, 990, 'either way, the permanent loses', t, SW(8, 'loses'), 1e9, { size: 40, bg: '#fbe2c0' });
}, { punch: false });
board('spectral8', SW(8, 'third') - 0.3, 'blue', 841, (g, t, S) => {
  print(g, 'strassen', 400, 480, 520, t, SW(8, 'strassen'), { caption: 'Strassen’s asymptotic spectrum' });
  tag(g, 1260, 230, 'flat case: “spectral points”', t, SW(8, 'spectral'), 1e9, { size: 42 });
  quoteCard(g, 'are only known to exist by Zorn’s lemma', 'the paper, §1.2', t, SW(8, "zorn's") - 0.4, 1e9, { x: 1260, y: 520, w: 760, size: 46 });
  stamp(g, 1260, 860, 'NOBODY CAN WRITE THEM DOWN', t, SW(8, 'nobody'), 1e9, { size: 40 });
});
board('diag8', SW(8, 'fourth') - 0.3, 'warm', 851, (g, t, S) => {
  const rows = 7, tD = SW(8, 'diagonal');
  for (let r = 0; r < rows; r++) for (let c = 0; c < 9; c++) { const v = hash(r * 31 + c, 5) > 0.5 ? 1 : 0, x = 760 + c * 64, y = 260 + r * 74, on = r === c && t > tD + r * 0.2;
    g.font = '700 40px Consolas'; g.textAlign = 'center'; g.fillStyle = css(on ? PAL.red : PAL.ink, on ? 1 : 0.7); g.fillText(String(v), x, y); if (on) brushCircle(g, x, y - 12, 26, 26, t, tD + r * 0.2, PAL.red, 4); }
  if (t > tD + 1.8) { tag(g, 1060, 840, 'flip the diagonal → a row that is not on the list', t, tD + 1.8, 1e9, { size: 32, bg: '#fbe2c0' }); }
  print(g, 'cantor', 330, 360, 320, t, SW(8, 'cantor'), { caption: 'Georg Cantor, 1891', cs: 26 });
  print(g, 'turing', 1660, 360, 320, t, SW(8, 'turing'), { caption: 'Alan Turing, 1936', cs: 26 });
  tag(g, 960, 980, 'here: no “accelerator” circuits can exist at every scale', t, SW(8, 'accelerators'), 1e9, { size: 32 });
}, { punch: false });
board('detfail8', SW(8, 'wrongly') - 0.4, 'sage', 861, (g, t, S) => {
  tag(g, 960, 160, 'why doesn’t it also “prove” the determinant is hard?', t, SW(8, 'wrongly'), 1e9, { size: 40 });
  tag(g, 560, 440, 'uses the permanent at exactly two steps:\nthe plug-in steps', t, SW(8, 'two'), 1e9, { size: 36 });
  tag(g, 1360, 440, 'the determinant can’t do that job', t, SW(8, 'job'), 1e9, { size: 36 });
  stamp(g, 960, 760, 'BREAKS EXACTLY WHERE IT SHOULD', t, SW(8, 'breaks'), 1e9, { size: 48, col: '#3f7a4a' });
});
flat('boolean8', SW(8, 'boolean') - 0.6, '#0e1118', (g, t, S) => {
  kin(g, [['ON 0 AND 1:   x² = x', '#ffffff']], t, SW(8, 'squared'), { size: 110, y: 330 });
  tag(g, 960, 560, 'so a Boolean circuit may compute a different polynomial that agrees on 0s and 1s', t, SW(8, 'secretly'), 1e9, { size: 32 });
  tag(g, 960, 700, 'the proof needs the real polynomial', t, SW(8, 'needs'), 1e9, { size: 40 });
  stamp(g, 960, 880, 'BOOLEAN CIRCUITS SLIP THROUGH → P vs NP UNTOUCHED', t, SW(8, 'slip'), 1e9, { size: 40, col: '#ffcf5a' });
}, { punch: false });
kinShot('lbl8', SW(8, 'experts') - 0.3, [['EXPERTS WILL CHECK', '#ffffff'], ['LINE BY LINE', '#ffcf5a']], { bg: '#0b0b10', size: 120, stagger: 0.5 });

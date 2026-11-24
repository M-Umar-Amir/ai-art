// ================= 9. The checker =================
flat('kernel9', SB(9) - 0.3, '#14181f', (g, t, S) => {
  codeView(g, 160, 140, 1100, 800, t, { top: t2 => 20 + (t2 - S._sh.t0) * 3, lh: 32, hl: [[44, 52, SW(9, 'kernel'), 'rgba(80,200,120,0.18)']] });
  tag(g, 1500, 260, 'Lean: a language for mathematics', t, SW(9, 'formalization'), 1e9, { size: 30 });
  tag(g, 1500, 470, 'the KERNEL checks every step', t, SW(9, 'kernel'), 1e9, { size: 32, bg: '#d9f0ff' });
  stamp(g, 1500, 700, 'STEP MISSING? REFUSED', t, SW(9, 'refuses'), 1e9, { size: 36 });
  credit(g, 'the real Statement.lean (anthropics/formal-math, Apache-2.0)');
}, { punch: false });
board('leanhist9', SW(9, 'started') - 0.3, 'warm', 911, (g, t, S) => {
  tag(g, 960, 200, '2013 · Leonardo de Moura (then at Microsoft Research)', t, SW(9, 'moura'), 1e9, { size: 40 });
  tag(g, 960, 380, 'a decade of mathematics poured into its shared library: Mathlib', t, SW(9, 'library'), 1e9, { size: 36 });
  tag(g, 960, 560, '2024 · Google DeepMind’s AlphaProof: IMO silver-medal level, solutions in Lean', t, SW(9, 'alphaproof'), 1e9, { size: 32 });
  tag(g, 960, 760, '2026 · AI systems posting claimed solutions to famous open problems', t, SW(9, 'later'), 1e9, { size: 36, bg: '#fbe2c0' });
});
board('size9', SW(9, 'numbered') - 0.3, 'blue', 921, (g, t, S) => {
  tag(g, 960, 200, 'every numbered statement but one: a Lean counterpart', t, SW(9, 'numbered'), 1e9, { size: 40 });
  tag(g, 560, 480, rollNum(t, SW(9, 'files') - 1, SW(9, 'files'), 0, 324) + ' files', t, SW(9, 'files') - 1, 1e9, { size: 80 });
  tag(g, 1360, 480, '≈ ' + rollNum(t, SW(9, 'lines') - 1, SW(9, 'lines'), 0, 73000) + ' lines', t, SW(9, 'lines') - 1, 1e9, { size: 80, bg: '#fbe2c0' });
  tag(g, 960, 760, '(as reported: MACCUS, CASRAI)', t, SW(9, 'lines'), 1e9, { size: 28 });
});
board('checks9', SW(9, 'twice') - 0.3, 'sage', 931, (g, t, S) => {
  checklist(g, 380, 260, [['checked by Lean’s own kernel', '✓', SW(9, 'kernel', 2)], ['and by nanoda, an independent checker', '✓', SW(9, 'nahnohdah')], ['only the 3 standard axioms', '✓', SW(9, 'axioms')], ['no “sorry” outside the challenge file', '✓', SW(9, 'challenge')], ['Comparator: proofs match the stated file', '✓', SW(9, 'comparator')]], t, { size: 42, dy: 130 });
  stamp(g, 1500, 900, '“SORRY” = TRUST ME', t, SW(9, 'sorry'), SW(9, 'challenge') + 0.5, { size: 40 });
});
kinShot('right9', SW(9, 'airtight') - 0.3, [['AS AIRTIGHT AS', '#ffffff'], ['COMPUTERS CAN MAKE IT', '#7fe0a0']], { bg: '#081008', size: 110, stagger: 0.4,
  draw: (g, t, S) => kin(g, [['BUT ARE THEY THE RIGHT THEOREMS?', '#ffcf5a']], t, SW(9, 'right', 2) - 0.6, { size: 80, y: 860 }) });

// ================= 10. Ninety-eight lines =================
flat('stmt10', SB(10) - 0.3, '#14181f', (g, t, S) => {
  const T0 = S._sh.t0, tC = SW(10, 'circuit'), tP = SW(10, 'permanent'), tV = SW(10, 'vnp');
  const top = t < tC ? (t - T0) * 6 : t < tP ? 18 : t < tV ? 26 : 70;
  codeView(g, 120, 120, 1240, 840, t, { top, lh: 32, hl: [[23, 28, tC], [30, 37, tP], [76, 91, tV]] });
  tag(g, 1640, 200, 'Statement.lean', t, SW(10, 'statement'), 1e9, { size: 40 });
  tag(g, 1640, 340, rollNum(t, SW(10, 'ninety-eight') - 0.8, SW(10, 'ninety-eight'), 0, 98) + ' lines', t, SW(10, 'ninety-eight') - 0.8, 1e9, { size: 64, bg: '#fbe2c0' });
  tag(g, 1640, 520, 'imports only Mathlib', t, SW(10, 'imports'), 1e9, { size: 32 });
  tag(g, 1640, 640, t < tP ? 'what a circuit is' : t < tV ? 'what the permanent is' : 'what VP and VNP mean', t, tC, 1e9, { size: 34 });
  tag(g, 1640, 800, 'then: the theorems', t, SW(10, 'states'), 1e9, { size: 34 });
  credit(g, 'the real Statement.lean, line by line');
}, { punch: false });
board('wrong10', SW(10, 'proofs') - 0.3, 'warm', 1011, (g, t, S) => {
  tag(g, 960, 170, 'proofs are checked AGAINST this file · the file must be read by a person', t, SW(10, 'person'), 1e9, { size: 32 });
  tag(g, 560, 420, 'a circuit allowed something it shouldn’t', t, SW(10, 'allowed'), 1e9, { size: 36 });
  tag(g, 1360, 420, 'a VNP narrower than Valiant’s', t, SW(10, 'narrower'), 1e9, { size: 36 });
  stamp(g, 960, 650, '73,000 PERFECT LINES · THE WRONG THING', t, SW(10, 'wrong'), 1e9, { size: 44 });
  tag(g, 960, 880, 'true · checked · and beside the point', t, SW(10, 'beside'), 1e9, { size: 42, bg: '#fbe2c0' });
});
dshot('blunt10', SW(10, 'bluntly') - 0.3, 'github', { path: [[0, 'no machine checks that this file says what is claimed']], hl: [['no machine checks that this file says what is claimed', SW(10, 'machine')]], src: 'vp-vnp/README.md' });
dshot('appq10', SW(10, 'appendix') - 0.3, 'paper_appq', { path: [[0, 'Whether the statements say what is meant was judged only by AI readers']], hl: [['Whether the statements say what is meant was judged only by AI readers', SW(10, 'judged')], ['No human', SW(10, 'human')]], src: 'paper.pdf, Appendix Q' });
board('prec10', SW(10, 'hypothetical') - 0.3, 'blue', 1041, (g, t, S) => {
  tag(g, 960, 170, 'not a hypothetical worry', t, SW(10, 'hypothetical'), 1e9, { size: 44 });
  tag(g, 560, 450, 'last video · Yang–Mills:\nOpenAI’s public statement was\nweaker than its proof', t, SW(10, 'panel'), 1e9, { size: 32 });
  tag(g, 1360, 450, 'arXiv 2610.08144:\n“Navier-Stokes lost in translation”', t, SW(10, 'translation'), 1e9, { size: 32, bg: '#fbe2c0' });
  tag(g, 960, 820, 'a Lean check doesn’t guarantee the human-language version says the same thing', t, SW(10, 'guarantee'), 1e9, { size: 30 });
});
dshot('lost10', SW(10, 'translation') + 0.6, 'losttrans', { path: [[0, 'Navier-Stokes lost in translation']], hl: [['Navier-Stokes lost in translation', SW(10, 'translation') + 1]], src: 'arXiv:2610.08144' });
flat('can10', SW(10, 'anyone') - 0.3, '#14181f', (g, t, S) => {
  codeView(g, 120, 120, 1180, 840, t, { top: t < SW(10, 'excludes') ? 18 : 40, lh: 34, hl: [[23, 27, SW(10, 'gates')], [44, 52, SW(10, 'excludes')]] });
  tag(g, 1530, 300, 'constants count as gates', t, SW(10, 'gates'), 1e9, { size: 30 });
  tag(g, 1530, 480, 'skips the 1 + 1 = 0 world', t, SW(10, 'excludes'), 1e9, { size: 28 });
  tag(g, 1530, 680, 'Appendix Q: every difference', t, SW(10, 'differs'), 1e9, { size: 28, bg: '#fbe2c0' });
}, { punch: false });
qshot('viola10', SW(10, 'watching') - 0.3, 'a proof of the most famous open problem in algebraic complexity … The human-readable part of the paper is very short and mostly points to the Lean formalization.', 'Emanuele Viola, Northeastern University, blog (23 Nov 2026)', { size: 40 });
dshot('fort10', SW(10, 'fortnow') - 0.4, 'fortnow', { path: [[0, 'Why read proofs anymore'], [SW(10, 'leanverified') - 0.4, 'They are mostly Lean-verified']], hl: [['Why read proofs anymore', SW(10, 'dialogue')], ['They are mostly Lean-verified', SW(10, 'leanverified')], ['not the one to thoroughly check all the details', SW(10, 'thoroughly')]], src: 'Computational Complexity blog (Fortnow & Gasarch), 23 Nov 2026 · about AI proofs in general' });
board('edges10', SW(10, 'edges') - 0.3, 'sage', 1081, (g, t, S) => {
  quoteCard(g, 'not analysed how it relates to relativization and algebrization', 'the paper, §1.2', t, SW(10, 'analysed') - 0.5, 1e9, { y: 400, size: 46 });
  tag(g, 960, 780, 'two more famous barriers in complexity theory', t, SW(10, 'barriers'), 1e9, { size: 38 });
});
kinShot('read10', SW(10, 'concrete') - 0.3, [['READ 98 LINES.', '#ffffff'], ['VERY CAREFULLY.', '#ffcf5a']], { bg: '#0b0b10', size: 140, stagger: 0.7,
  draw: (g, t, S) => tag(g, 960, 900, 'then the 70 pages that explain them', t, SW(10, 'seventy'), 1e9, { size: 40 }) });

// ================= 11. What it would mean =================
board('know11', SB(11) - 0.3, 'warm', 1101, (g, t, S) => {
  title(g, 960, 150, 'how would we know it holds?', t, SW(11, 'know'), 1e9, 62, PAL.ink, { shadow: false });
  checklist(g, 340, 320, [['experts read the statement file against Valiant’s definitions', '?', SW(11, 'experts')], ['others re-derive the key steps by hand', '?', SW(11, 'rederive')], ['peer review, with human referees', '?', SW(11, 'review')]], t, { size: 38, dy: 140 });
  stamp(g, 960, 860, 'NOT YET · PUBLIC FOR A DAY', t, SW(11, 'happened'), 1e9, { size: 42 });
});
board('math11', SW(11, 'mathematics') - 0.3, 'blue', 1111, (g, t, S) => {
  tag(g, 960, 220, 'Valiant’s conjecture (1979): settled', t, SW(11, 'settled'), 1e9, { size: 48, bg: '#fbe2c0' });
  tag(g, 960, 450, 'GCT’s target: the permanent can’t be approximated by small determinants', t, SW(11, 'geometric'), 1e9, { size: 32 });
  tag(g, 960, 640, 'the one consequence NOT formalized in Lean', t, SW(11, 'formalize'), 1e9, { size: 38 });
});
clipShot('bank11', SW(11, 'bank') - 0.3, 'boards', { draw: (g, t, S) => {
  kin(g, [['YOUR BANK ACCOUNT:', '#ffffff'], ['NOTHING CHANGES', '#7fe0a0']], t, SW(11, 'bank'), { size: 110, stagger: 0.5, y: 440 });
  tag(g, 960, 820, 'it says something is HARD, not easy · permanents are as slow as last week', t, SW(11, 'slow') - 1, 1e9, { size: 32 });
} });
board('oneway11', SW(11, 'still') - 0.3, 'sage', 1131, (g, t, S) => {
  tag(g, 520, 380, 'P vs NP settled for circuits\n+ an assumption from number theory', t, SW(11, 'burgisser'), 1e9, { size: 32 });
  tag(g, 1440, 380, 'VP ≠ VNP', t, SW(11, 'burgisser') + 0.6, 1e9, { size: 56, bg: '#fbe2c0' });
  if (t > SW(11, 'burgisser') + 0.8) arrow(g, 860, 380, 1260, 380, 10, '#3f7a4a');
  if (t > SW(11, 'checkpoint')) { arrow(g, 1260, 560, 860, 560, 8, PAL.red, 0.8); stamp(g, 1060, 560, '✗', t, SW(11, 'checkpoint'), 1e9, { size: 70 }); }
  tag(g, 520, 160, 'Peter Bürgisser · 2000', t, SW(11, 'burgisser'), 1e9, { size: 36 });
  tag(g, 960, 820, 'a checkpoint on the road, not the end', t, SW(11, 'checkpoint'), 1e9, { size: 40 });
  stamp(g, 960, 980, 'P vs NP: STILL OPEN', t, SW(11, 'still'), 1e9, { size: 44 });
});
clipShot('light11', SW(11, 'physics') - 0.3, 'lasersky', { draw: (g, t, S) => {
  tag(g, 960, 180, 'fits the bet behind boson sampling …', t, SW(11, 'fits'), 1e9, { size: 38 });
  tag(g, 960, 860, '… but doesn’t prove it: ordinary computers aren’t limited to + and ×', t, SW(11, 'limited') - 1.5, 1e9, { size: 32, bg: '#fbe2c0' });
} });
qshot('viola11', SW(11, 'living') - 0.3, 'I still believe we will see more surprises in terms of upper bounds.', 'Emanuele Viola (23 Nov 2026)', { size: 54,
  draw: (g, t, S) => tag(g, 960, 880, 'expect some things we think are hard to turn out easy', t, SW(11, 'words'), 1e9, { size: 36 }) });

// ================= 12. Close =================
board('twins12', SB(12) - 0.3, 'warm', 1201, (g, t, S) => {
  writeText(g, 'det = ad − bc', 960, 300, 100, t, SB(12), 0.6, { upright: true, col: '#2f5f8f', align: 'center' });
  writeText(g, 'perm = ad + bc', 960, 470, 100, t, SB(12) + 0.4, 0.6, { upright: true, col: '#b8361f', align: 'center' });
  tag(g, 560, 700, 'a fraction of a second', t, SW(12, 'fraction'), 1e9, { size: 42 });
  tag(g, 1360, 700, 'millions of years', t, SW(12, 'millions'), 1e9, { size: 42, bg: '#fbe2c0' });
  tag(g, 960, 900, '47 years: nobody could prove the gap is real', t, SW(12, 'nobody'), 1e9, { size: 38 });
});
dshot('author12', SW(12, 'seventy-page') - 0.3, 'paper_lean', { path: [[0, 'The proofs in this paper were found and written by AI']], hl: [['The proofs in this paper were found and written by AI', SW(12, 'author')], ['No human mathematician has', SW(12, 'computer')]], src: 'paper.pdf, §3' });
flat('q12', SW(12, 'ninety-eight') - 0.6, '#14181f', (g, t, S) => {
  codeView(g, 120, 120, 1680, 840, t, { top: 74, lh: 36, hl: [[76, 96, SW(12, 'question') - 0.5]] });
  kin(g, [['THE RIGHT QUESTION?', '#ffcf5a']], t, SW(12, 'question'), { size: 110, y: 540 });
}, { punch: false });
pshot('godel12', SW(12, 'gurdle') - 0.4, 'godel', { bw: true, from: [0.5, 0.32, 1.12], to: [0.5, 0.3, 1.25], tags: [[960, 130, '1956: a machine that could replace a mathematician’s mental work', SW(12, 'replace'), { size: 34 }], [960, 950, '2026: a machine has done the work', SW(12, 'later'), { size: 42, bg: '#fbe2c0' }]] });
dshot('letter12', SW(12, 'mention') - 0.6, 'godel_letter', { path: [[0, 'completely'], [SW(12, 'meant') - 0.6, 'all']], hl: [['completely', SW(12, 'reading')], ['replaced by a machine', SW(12, 'reading') + 0.4]], desk: '#22201c', src: 'Gödel to von Neumann, 1956' });
kinShot('end12', SW(12, 'secret') - 0.5, [['CHECKED BY A MACHINE.', '#ffffff'], ['READ, SO FAR,', '#ffcf5a'], ['BY NO HUMAN.', '#ff6a55']], { bg: '#06070b', size: 110, stagger: 0.9, delay: 1.6,
  draw: (g, t, S) => kin(g, [['THE MINUS SIGN KEEPS ITS SECRET', '#9fb0c8']], t, SW(12, 'secret') - 0.3, { size: 54, y: 180 }) });

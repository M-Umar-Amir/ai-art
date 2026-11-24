// ================= helpers for v2 shots =================
// full-frame photo shot with optional overlay tags: o.tags = [[x, y, text, tIn, opts]]
function pshot(name, t0, key, o = {}) {
  scene(name, t0, () => ({}), (g, l, t, S) => {
    photoFull(g, key, t, S._sh.t0, S._sh.t1, o);
    for (const [x, y, txt, tIn, op] of (o.tags || [])) tag(g, x, y, txt, t, tIn, (op && op.out) ?? 1e9, Object.assign({ size: 40 }, op || {}));
    if (o.draw) o.draw(g, t, S);
  }, { in: o.in });
}
// document shot: o.path, o.hl, o.src; o.tags as above
function dshot(name, t0, key, o = {}) {
  scene(name, t0, () => ({}), (g, l, t, S) => {
    docView(g, key, t, o);
    for (const [x, y, txt, tIn, op] of (o.tags || [])) tag(g, x, y, txt, t, tIn, (op && op.out) ?? 1e9, Object.assign({ size: 38 }, op || {}));
    if (o.draw) o.draw(g, t, S);
  }, { in: o.in });
}
// a quote card on a dark desk
function qshot(name, t0, text, who, o = {}) {
  flat(name, t0, o.bg || '#15161c', (g, t, S) => {
    if (o.key) print(g, o.key, o.px ?? 330, o.py ?? 520, o.ph ?? 520, t, S._sh.t0 + 0.05, { bw: o.bw, credit: true, creditCol: 'rgba(230,230,230,0.8)' });
    quoteCard(g, text, who, t, S._sh.t0 + (o.delay ?? 0.1), 1e9, { x: o.key ? 1180 : 960, y: o.y ?? 520, w: o.key ? 900 : 1300, size: o.size ?? 50 });
    if (o.draw) o.draw(g, t, S);
  }, { punch: o.punch ?? true, frames: [[1, 960, 540], [1.06, 960, 530]] });
}
// a big number grid (rows of numbers) on a board; returns cell centre function
function numGrid(g, x, y, cell, nums, o = {}) {
  const n = nums.length, m = nums[0].length;
  for (let r = 0; r < n; r++) for (let c = 0; c < m; c++) {
    const cx = x + (c - (m - 1) / 2) * cell, cy = y + (r - (n - 1) / 2) * cell, on = o.on && o.on(r, c);
    if (on) { g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, cx, cy, cell * 0.7, o.glow || PAL.gold, 0.7); g.restore(); }
    g.font = `700 ${o.fs ?? cell * 0.55}px Georgia, serif`; g.textAlign = 'center'; g.textBaseline = 'middle';
    g.fillStyle = css(on ? (o.onCol || PAL.red) : (o.col || PAL.ink), on ? 1 : (o.dim ?? 0.85)); g.fillText(String(nums[r][c]), cx, cy + 2);
  }
  const bx = (m / 2) * cell + 18, by = (n / 2) * cell + 10;
  for (const s of [-1, 1]) ink(g, [[x + s * bx + -s * 26, y - by], [x + s * bx, y - by], [x + s * bx, y + by], [x + s * bx - s * 26, y + by]], 7, { col: o.bracket || PAL.ink, t0: 0.02, t1: 0.02, min: 0.8 });
  return (r, c) => [x + (c - (m - 1) / 2) * cell, y + (r - (n - 1) / 2) * cell];
}
const permProduct = (nums, p) => p.reduce((a, c, r) => a * nums[r][c], 1);
const permSign = p => { let s = 1; for (let i = 0; i < p.length; i++) for (let j = i + 1; j < p.length; j++) if (p[i] > p[j]) s = -s; return s; };

// ================= 0. Cold open =================
const G4 = [[3, 1, 4, 1], [5, 9, 2, 6], [5, 3, 5, 8], [9, 7, 9, 3]];
flat('pick', 0, '#f1e7d0', (g, t, S) => {
  const T0 = S._sh.t0, tP = SW(0, 'pick'), tE = SW(0, 'every'), tA = SW(0, 'add');
  const k = t < tE ? 0 : Math.min(23, Math.floor((t - tE) * 6)), p = PERM4[k];
  const cell = numGrid(g, 700, 520, 150, G4, { on: (r, c) => t > tP && p[r] === c });
  if (t > tP) { const pts = [0, 1, 2, 3].map(r => cell(r, p[r])); ink(g, pts, 6, { col: PAL.red, a: 0.6 * seg(t, tP, tP + 0.4), t0: 0.05, t1: 0.05 }); }
  if (t > SW(0, 'multiply')) tag(g, 1450, 360, `${p.map((c, r) => G4[r][c]).join(' × ')} = ${permProduct(G4, p)}`, t, SW(0, 'multiply'), 1e9, { size: 46 });
  if (t > tE) tag(g, 1450, 520, `path ${k + 1} of 24`, t, tE, 1e9, { size: 40, bg: '#fbe2c0' });
  if (t > tA) { let s = 0; for (let i = 0; i <= Math.min(23, Math.floor((t - tA) * 8 + k)); i++) s += permProduct(G4, PERM4[i]); tag(g, 1450, 700, `running sum: ${s.toLocaleString('en-US')}`, t, tA, 1e9, { size: 46 }); }
  tag(g, 700, 960, 'one number from every row, never two from the same column', t, tP, 1e9, { size: 32 });
}, { punch: false });

flat('minus', SW(0, 'second') - 0.3, '#f1e7d0', (g, t, S) => {
  const tM = SW(0, 'minus');
  const show = PERM4.slice(0, 12); g.font = '600 40px Georgia, serif'; g.textAlign = 'left'; g.textBaseline = 'middle';
  show.forEach((p, i) => { const x = 260 + (i % 3) * 480, y = 230 + Math.floor(i / 3) * 150, sg = permSign(p), neg = t > tM + i * 0.05 && sg < 0;
    g.fillStyle = css(neg ? PAL.red : '#3f7a4a'); g.font = '800 64px Georgia'; g.fillText(neg ? '−' : '+', x - 60, y);
    g.fillStyle = css(PAL.ink); g.font = '600 40px Georgia'; g.fillText(p.map((c, r) => G4[r][c]).join('·'), x, y); });
  tag(g, 960, 900, 'same products · half of them get a minus sign', t, tM, 1e9, { size: 40, bg: '#fbe2c0' });
}, { punch: false });

kinShot('det-name', SW(0, 'determinant') - 0.3, [['THE DETERMINANT', '#7fd0ff']], { bg: '#0d1420', size: 140, draw: (g, t, S) => {
  tag(g, 960, 720, 'students meet it in school', t, SW(0, 'school'), 1e9, { size: 40 });
  tag(g, 960, 860, `100 × 100 on a laptop: ${t > SW(0, 'laptop') ? '0.0003 s' : '…'}`, t, SW(0, 'laptop'), 1e9, { size: 44, bg: '#d9f0ff' });
} });

kinShot('perm-name', SW(0, 'permanent') - 0.35, [['THE PERMANENT', '#ffcf5a']], { bg: '#1a1208', size: 150 });

clipShot('racks0', SW(0, 'best') - 0.3, 'racks', { draw: (g, t, S) => {
  tag(g, 960, 200, 'fastest supercomputers: ~10¹⁸ operations per second', t, SW(0, 'fastest'), 1e9, { size: 40 });
  tag(g, 960, 820, rollNum(t, SW(0, 'busy'), SW(0, 'busy') + 1.6, 0, 4000000) + ' years', t, SW(0, 'busy'), 1e9, { size: 88, bg: '#fbe2c0' });
  tag(g, 960, 940, 'best known methods · 100 × 100 · our estimate (see notes)', t, SW(0, 'busy') + 0.6, 1e9, { size: 26 });
} });

kinShot('one-minus', SW(0, 'one', 3) - 0.25, [['ONE MINUS SIGN.', '#ff6a55'], ['MILLIONS OF YEARS.', '#ffffff']], { bg: '#0b0b0f', size: 150, stagger: 0.9 });

board('valiant0', SW(0, 'forty-seven') - 0.3, 'warm', 61, (g, t, S) => {
  print(g, 'valiant', 470, 500, 640, t, SW(0, 'forty-seven') - 0.2, { caption: 'Leslie Valiant' });
  tag(g, 1300, 260, '47 years: believed, never proved', t, SW(0, 'forty-seven'), 1e9, { size: 40 });
  tag(g, 1300, 450, 'no clever trick is waiting', t, SW(0, 'trick'), 1e9, { size: 36 });
  tag(g, 1300, 640, "Valiant's conjecture (1979)", t, SW(0, "valiant's"), 1e9, { size: 46, bg: '#fbe2c0' });
  tag(g, 1300, 820, 'the algebraic cousin of P vs NP', t, SW(0, 'cousin'), 1e9, { size: 42 });
});

dshot('gh0', SW(0, 'twenty-third') - 0.3, 'github', { path: [[0, 'all'], [SW(0, 'author'), 'VP ≠ VNP over every field']], hl: [['VP ≠ VNP over every field', SW(0, 'claims')]], src: 'github.com/anthropics/formal-math · 23 Nov 2026',
  tags: [[960, 120, 'Monday 23 November 2026 · Anthropic, on GitHub', SW(0, 'twenty-third') - 0.1, { size: 34 }]] });
dshot('paper0', SW(0, 'claude') - 0.3, 'paper_p1', { path: [[0, [0, 0, 1429, 900]], [SW(0, 'claims') + 0.3, 'every arithmetic circuit that computes the permanent']], hl: [['discovered by an internal research Claude model', SW(0, 'claude') + 0.3], ['every arithmetic circuit that computes the permanent', SW(0, 'claims') + 0.6]], src: 'paper.pdf, "VP ≠ VNP over every field" (Claude, Anthropic)' });
dshot('nohuman0', SW(0, 'computer', 2) - 0.3, 'github', { path: [[0, 'no human mathematician has checked the proof']], hl: [['no human mathematician has checked the proof', SW(0, 'human') - 0.2]], src: 'vp-vnp/README.md',
  tags: [[960, 900, 'a computer checked every step', SW(0, 'computer', 2), { size: 40, bg: '#d9f0ff' }]] });

flat('onefile', SW(0, 'small') - 0.3, '#14181f', (g, t, S) => {
  codeView(g, 260, 140, 1400, 760, t, { top: t2 => 8 + (t2 - S._sh.t0) * 2.2, lh: 34 });
  tag(g, 960, 120, 'one small file · about 100 lines', t, SW(0, 'small'), 1e9, { size: 40 });
  stamp(g, 1500, 700, 'NO MACHINE CAN CHECK THIS', t, SW(0, 'machine'), 1e9, { size: 44 });
  tag(g, 960, 960, 'everything depends on it', t, SW(0, 'everything'), 1e9, { size: 40, bg: '#fbe2c0' });
}, { punch: false });

board('disclose0', SW(0, 'disclosure') - 0.3, 'blue', 81, (g, t, S) => {
  title(g, 960, 230, 'DISCLOSURE', t, SW(0, 'disclosure'), 1e9, 96, PAL.ink, { upright: true, weight: 700, shadow: false });
  tag(g, 960, 470, 'this video was researched and written with Claude,\nthe same family of AI models named as the paper’s author', t, SW(0, 'researched'), 1e9, { size: 40 });
  tag(g, 960, 720, 'every judgement: named people and published documents (links below)', t, SW(0, 'judgement'), 1e9, { size: 36, bg: '#fbe2c0' });
}, { punch: false });

board('qs0', SW(0, "here's") - 0.3, 'sage', 91, (g, t, S) => {
  tag(g, 960, 260, '1 · Why is one formula so much harder than its twin?', t, SW(0, 'harder'), 1e9, { size: 44 });
  tag(g, 960, 480, '2 · What exactly did the machine claim, and how far does it go?', t, SW(0, 'exactly'), 1e9, { size: 44 });
  tag(g, 960, 700, '3 · Can we trust a proof that no human has read?', t, SW(0, 'trust'), 1e9, { size: 46, bg: '#fbe2c0' });
});

// ================= 1. Checking versus finding =================
pshot('sudoku1', SB(1) - 0.2, 'sudoku', { fit: true, from: [0.5, 0.5, 0.9], to: [0.5, 0.5, 0.97], credit: true, tags: [[420, 250, 'solve: about an hour', SW(1, 'hour'), { size: 44 }], [1500, 250, 'check: seconds', SW(1, 'seconds'), { size: 44, bg: '#d9f0ff' }]] });

board('examples1', SW(1, 'packing') - 0.3, 'warm', 111, (g, t, S) => {
  print(g, 'tsp', 1380, 540, 620, t, SW(1, 'route'), { caption: 'the shortest route through the cities?' });
  tag(g, 520, 260, 'packing a delivery van', t, SW(1, 'packing'), 1e9, { size: 42 });
  tag(g, 520, 460, 'exams, nobody gets two at once', t, SW(1, 'scheduling'), 1e9, { size: 42 });
  tag(g, 520, 660, 'the shortest route through 50 cities', t, SW(1, 'route'), 1e9, { size: 42 });
  tag(g, 520, 860, 'check an answer: fast · find one: forever?', t, SW(1, 'forever'), 1e9, { size: 38, bg: '#fbe2c0' });
});

board('pnp1', SW(1, 'computer') - 0.3, 'blue', 121, (g, t, S) => {
  const a = seg(t, SW(1, 'np'), SW(1, 'np') + 0.5), b = seg(t, SW(1, 'solve', 2), SW(1, 'solve', 2) + 0.5);
  g.save(); g.globalAlpha = a; g.fillStyle = css('#3a6fd0', 0.25); g.beginPath(); g.ellipse(960, 560, 560, 340, 0, 0, TAU); g.fill(); g.strokeStyle = css('#3a6fd0'); g.lineWidth = 6; g.stroke(); g.restore();
  g.save(); g.globalAlpha = b; g.fillStyle = css('#3f9a5a', 0.3); g.beginPath(); g.ellipse(820, 600, 250, 170, 0, 0, TAU); g.fill(); g.strokeStyle = css('#3f9a5a'); g.lineWidth = 6; g.stroke(); g.restore();
  tag(g, 1260, 420, 'NP: easy to CHECK', t, SW(1, 'np'), 1e9, { size: 46 });
  tag(g, 820, 600, 'P: easy to SOLVE', t, SW(1, 'solve', 2), 1e9, { size: 42 });
  stamp(g, 960, 900, 'SECRETLY THE SAME?', t, SW(1, 'same'), 1e9, { size: 56 });
});

board('stakes1', SW(1, 'answer', 2) - 0.3, 'warm', 131, (g, t, S) => {
  title(g, 960, 150, 'if P = NP …', t, SW(1, 'answer', 2), 1e9, 70, PAL.ink, { shadow: false });
  tag(g, 520, 380, 'online security, rethought', t, SW(1, 'security'), 1e9, { size: 44 });
  tag(g, 1400, 380, 'scheduling and logistics', t, SW(1, 'logistics'), 1e9, { size: 44 });
  tag(g, 520, 620, 'drug design', t, SW(1, 'drug'), 1e9, { size: 44 });
  tag(g, 1400, 620, 'finding proofs, not just checking them', t, SW(1, 'mathematics'), 1e9, { size: 40, bg: '#fbe2c0' });
});

board('ckl1', SW(1, 'cook') - 0.4, 'warm', 141, (g, t, S) => {
  print(g, 'cook', 360, 500, 520, t, SW(1, 'cook') - 0.2, { caption: 'Stephen Cook · 1971' });
  print(g, 'levin', 960, 500, 470, t, SW(1, 'levin'), { caption: 'Leonid Levin', rot: 0.03 });
  print(g, 'karp', 1560, 500, 520, t, SW(1, 'karp'), { caption: 'Richard Karp · 1972', rot: -0.04 });
  stamp(g, 1560, 880, '21 PROBLEMS', t, SW(1, 'twenty-one'), 1e9, { size: 50 });
  tag(g, 960, 930, 'today: thousands', t, SW(1, 'thousands'), 1e9, { size: 42, bg: '#fbe2c0' });
}, { punch: false });

board('clay1', SW(1, 'almost') - 0.3, 'blue', 151, (g, t, S) => {
  const names = ['Poincaré', 'Navier–Stokes', 'Riemann', 'Yang–Mills', 'P vs NP', 'Hodge', 'BSD'], tE = SW(1, 'clay');
  names.forEach((n, k) => { const tk = tE + k * 0.1; if (t < tk) return; g.save(); g.globalAlpha = seg(t, tk, tk + 0.25); envelope(g, 250 + k * 237, 520, 200, n, k === 0 ? 1 : 0, { glow: k === 4 ? 1 : (k >= 1 && k <= 3) ? 0.45 : 0 }); g.restore(); });
  tag(g, 960, 230, 'almost everyone believes P ≠ NP · nobody has proved it', t, SW(1, 'almost'), 1e9, { size: 40 });
  tag(g, 720, 790, 'our last three videos', t, SW(1, 'videos'), 1e9, { size: 34 });
  tag(g, 1200, 790, '$1,000,000', t, SW(1, 'milliondollar'), 1e9, { size: 44, bg: '#fbe2c0' });
});

pshot('godel1', SW(1, 'older') - 0.3, 'godel', { bw: true, from: [0.5, 0.35, 1.0], to: [0.5, 0.3, 1.15], tags: [[960, 140, 'Princeton · 20 March 1956', SW(1, 'march'), { size: 40 }], [960, 900, 'Kurt Gödel → John von Neumann', SW(1, 'letter'), { size: 44 }]] });
board('vn1', SW(1, 'ill') - 0.6, 'warm', 161, (g, t, S) => {
  print(g, 'godel', 560, 500, 560, t, S._sh.t0, { bw: true, caption: 'Kurt Gödel' });
  print(g, 'vonneumann', 1360, 500, 560, t, S._sh.t0 + 0.15, { bw: true, caption: 'John von Neumann', rot: 0.04 });
  tag(g, 1360, 930, 'gravely ill with cancer', t, SW(1, 'ill'), 1e9, { size: 36 });
}, { punch: false });
dshot('letter1', SW(1, 'asked') - 0.3, 'godel_letter', { path: [[0, 'all'], [SW(1, 'mental') - 0.4, 'replaced by a machine']], hl: [['completely', SW(1, 'mental') + 0.3], ['replaced by a machine', SW(1, 'replaced')]], src: 'Gödel to von Neumann, 1956 (English translation)', desk: '#22201c' });

kinShot('posting1', SW(1, 'seventy') - 0.3, [['70 YEARS LATER', '#ffffff'], ['A MACHINE IS POSTING PROOFS', '#ffcf5a']], { bg: '#101014', size: 110, stagger: 0.8,
  draw: (g, t, S) => tag(g, 960, 880, '… just not of P versus NP', t, SW(1, 'just'), 1e9, { size: 44 }) });

flat('bits1', SW(1, 'shuffle') - 0.3, '#05070a', (g, t, S) => {
  const T0 = S._sh.t0; g.font = '700 34px Consolas, monospace'; g.textAlign = 'center';
  for (let c = 0; c < 48; c++) for (let k = 0; k < 18; k++) { const y = ((k * 70 + (t - T0) * (120 + 60 * hash(c, 3)) + hash(c, 9) * 900) % 1250) - 100; g.fillStyle = `rgba(90,220,140,${0.15 + 0.5 * hash(c * 31 + k, 4)})`; g.fillText(hash(c * 7 + k + Math.floor((t - T0) * 6), 2) > 0.5 ? '1' : '0', 20 + c * 40, y); }
  kin(g, [['BITS ARE SLIPPERY', '#ffffff']], t, SW(1, 'slippery'), { size: 120 });
  kin(g, [['1979 · ALGEBRA ONLY', '#ffcf5a']], t, SW(1, 'nineteen', 3), { size: 96, y: 780 });
}, { punch: false });

// ================= 2. A calculator with two buttons =================
function calculator(g, x, y, s, t, tPress) {
  cels(g, rough([[x - 1.2 * s, y - 1.6 * s], [x + 1.2 * s, y - 1.6 * s], [x + 1.2 * s, y + 1.6 * s], [x - 1.2 * s, y + 1.6 * s]], 2, 4, 12), { base: '#2b2f3a', sh: '#1b1e26', sw: 10, L: LIGHT, line: PAL.ink, lw: 4, seed: 4 });
  cels(g, [[x - 0.95 * s, y - 1.35 * s], [x + 0.95 * s, y - 1.35 * s], [x + 0.95 * s, y - 0.75 * s], [x - 0.95 * s, y - 0.75 * s]], { base: '#a8d8a0', line: PAL.ink, lw: 3, seed: 5 });
  [['+', -0.5], ['×', 0.5]].forEach(([lab, dx], i) => { const pressed = tPress && Math.floor((t - tPress) * 2) % 2 === i && t > tPress; const bx = x + dx * s, by = y + 0.5 * s + (pressed ? 6 : 0);
    cels(g, rough([[bx - 0.38 * s, by - 0.5 * s], [bx + 0.38 * s, by - 0.5 * s], [bx + 0.38 * s, by + 0.5 * s], [bx - 0.38 * s, by + 0.5 * s]], 1.5, i, 8), { base: i ? '#f2b84b' : '#39c6b0', sh: '#555', sw: 6, L: LIGHT, line: PAL.ink, lw: 3, seed: i });
    g.font = `800 ${0.6 * s}px Georgia`; g.fillStyle = '#111'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(lab, bx, by + 4); });
}
board('calc2', SB(2) - 0.3, 'warm', 201, (g, t, S) => {
  print(g, 'valiant', 1530, 470, 420, t, SB(2), { caption: 'Leslie Valiant, 30', rot: 0.05 });
  calculator(g, 700, 540, 230, t, SW(2, 'buttons') + 0.3);
  tag(g, 700, 130, 'only two buttons: + and ×', t, SW(2, 'buttons'), 1e9, { size: 44 });
  tag(g, 700, 960, 'no comparisons · no if-then · no peeking at bits', t, SW(2, 'comparisons'), 1e9, { size: 34, bg: '#fbe2c0' });
  tag(g, 1530, 900, 'a program = an ARITHMETIC CIRCUIT', t, SW(2, 'circuit'), 1e9, { size: 34 });
});

board('square2', SW(2, 'shockingly') - 0.3, 'blue', 211, (g, t, S) => {
  title(g, 960, 140, 'x¹⁰²⁴', t, SW(2, 'thousand'), 1e9, 110, PAL.ink, { upright: true, shadow: false });
  tag(g, 960, 300, 'written out: 1,023 multiplications', t, SW(2, 'written'), 1e9, { size: 40 });
  const tS = SW(2, 'square'); if (t > tS) { const n = Math.min(10, Math.floor((t - tS) * 3.2) + 1); for (let k = 0; k <= n; k++) { const x = 170 + k * 158, y = 560 + (k % 2) * 60; tag(g, x, y, k ? `x^${2 ** k}` : 'x', t, tS + k * 0.3, 1e9, { size: 32, bg: k === 10 ? '#fbe2c0' : PAL.card }); if (k) arrow(g, x - 120, y - 10 * (k % 2 ? 1 : -1), x - 60, y, 4, PAL.ink); } tag(g, 960, 800, rollNum(t, tS, tS + 3.2, 0, 10) + ' presses', t, tS, 1e9, { size: 70 }); }
  tag(g, 960, 960, 'circuits reuse what they’ve built · that’s why “no small circuit” is so hard to prove', t, SW(2, 'reuse'), 1e9, { size: 30 });
});

glShot('circuit3d', SW(2, 'reuse') - 0.2, glCircuit, { over: (g, t, S) => {
  tag(g, 960, 110, 'an arithmetic circuit: inputs at the bottom, + and × gates, one output', t, S._sh.t0 + 0.4, 1e9, { size: 32 });
  credit(g, '3D: computed scene, not a real chip');
} });

board('vpvnp2', SW(2, 'classes') - 0.3, 'warm', 221, (g, t, S) => {
  const a = seg(t, SW(2, 'vnp'), SW(2, 'vnp') + 0.5), b = seg(t, SW(2, 'vp'), SW(2, 'vp') + 0.5);
  g.save(); g.globalAlpha = a; g.fillStyle = css('#d79a2b', 0.18); g.beginPath(); g.ellipse(980, 560, 620, 360, 0, 0, TAU); g.fill(); g.strokeStyle = css('#d79a2b'); g.lineWidth = 6; g.stroke(); g.restore();
  g.save(); g.globalAlpha = b; g.fillStyle = css('#39c6b0', 0.25); g.beginPath(); g.ellipse(760, 600, 260, 170, 0, 0, TAU); g.fill(); g.strokeStyle = css('#2a8f80'); g.lineWidth = 6; g.stroke(); g.restore();
  tag(g, 760, 600, 'VP: small circuits', t, SW(2, 'vp'), 1e9, { size: 40 });
  tag(g, 1300, 420, 'VNP: a giant sum, over\nexponentially many on-off choices,\nof something easy', t, SW(2, 'vnp'), 1e9, { size: 32 });
  tag(g, 1300, 760, '= the algebra version of “easy to check”', t, SW(2, 'algebra'), 1e9, { size: 32, bg: '#fbe2c0' });
});
kinShot('conj2', SW(2, 'asks') - 0.3, [['VP = VNP ?', '#ffffff']], { bg: '#101418', size: 170, draw: (g, t, S) => kin(g, [["VALIANT'S CONJECTURE: NO", '#ffcf5a']], t, SW(2, 'no'), { size: 80, y: 820 }) });
board('champion2', SW(2, 'champion') - 0.3, 'blue', 231, (g, t, S) => {
  title(g, 960, 200, 'the champion of VNP', t, SW(2, 'champion'), 1e9, 66, PAL.ink, { shadow: false });
  kin(g, [['THE PERMANENT', '#d79a2b']], t, SW(2, 'permanent'), { size: 120, y: 470 });
  tag(g, 960, 700, 'VNP-complete: if it had small circuits, everything in VNP would', t, SW(2, 'everything'), 1e9, { size: 36 });
});
board('count2', SW(2, 'monster') - 0.3, 'sage', 241, (g, t, S) => {
  const Z = [[1, 0, 1, 1], [0, 1, 1, 0], [1, 1, 0, 1], [0, 1, 1, 1]], tZ = SW(2, 'zeros');
  numGrid(g, 560, 520, 130, Z, { on: (r, c) => t > tZ && PERM4.filter(p => p.every((cc, rr) => Z[rr][cc])).length && PERM4.filter(p => p.every((cc, rr) => Z[rr][cc]))[Math.floor((t - tZ) * 2) % PERM4.filter(p => p.every((cc, rr) => Z[rr][cc])).length][r] === c });
  const cnt = PERM4.filter(p => p.every((c, r) => Z[r][c])).length;
  tag(g, 1360, 380, `permanent of this 0/1 grid = ${cnt}`, t, tZ + 0.5, 1e9, { size: 40 });
  tag(g, 1360, 560, 'it COUNTS things', t, tZ + 1, 1e9, { size: 44, bg: '#fbe2c0' });
  tag(g, 1360, 740, 'as hard as counting the solutions\nto any NP problem (1979)', t, SW(2, 'counting'), 1e9, { size: 34 });
});
board('turing2', SW(2, 'turing') - 0.4, 'warm', 251, (g, t, S) => {
  print(g, 'valiant', 640, 520, 700, t, S._sh.t0, { caption: 'Leslie Valiant' });
  tag(g, 1360, 420, 'ACM Turing Award · 2010', t, SW(2, 'turing'), 1e9, { size: 50, bg: '#fbe2c0' });
  tag(g, 1360, 600, '“computing’s highest honour”', t, SW(2, 'honour'), 1e9, { size: 38, italic: true });
  kin(g, [['THE TWINS', '#1d2230']], t, SW(2, 'twins'), { size: 110, y: 860, x: 1360, stroke: 'rgba(255,255,255,0.0)' });
}, { punch: false });

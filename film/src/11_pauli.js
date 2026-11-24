// ================= 1. The question Pauli asked =================
function chalkScene(name, t0, seed, draw, o = {}) {
  scene(name, t0, () => ({ pl: boardPlate(seed) }), (g, l, t, S) => { g.save(); plateDrift(g, S.pl, t, S._sh.t0, S._sh.t1, o.cam || { z0: 1.0, z1: 1.04 }); draw(g, t, S); g.restore(); });
}
function nameplate(g, x, y, name, sub, t, tIn, o = {}) {
  if (on2(t) < tIn) return; const k = E.out3(seg(on2(t), tIn, tIn + 0.35));
  g.save(); g.globalAlpha = k; g.translate(x, y + 20 * (1 - k));
  const w = o.w ?? 460, h = 130, box = rough([[-w / 2, -h / 2], [w / 2, -h / 2], [w / 2 + 18, h / 2], [-w / 2 - 18, h / 2]], 1.5, Math.floor(x), 10);
  fillPts(g, offsetPts(box, 6, 8), css('#000', 0.2)); cels(g, box, { base: o.bg || '#3a2a20', sh: '#22160f', sw: 6, L: LIGHT, line: PAL.ink, lw: 2.5, seed: 3 });
  g.fillStyle = '#f3d98a'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.font = `700 ${o.fs ?? 38}px Georgia, serif`; g.fillText(name, 0, sub ? -18 : 0);
  if (sub) { g.font = `italic 500 ${(o.fs ?? 38) * 0.7}px Georgia, serif`; g.fillStyle = '#e8dcc0'; g.fillText(sub, 0, 30); }
  g.restore();
}

board('brookhaven', SB(1) - 0.3, 'warm', 101, (g, t, S) => {
  tag(g, 960, 170, 'Brookhaven National Laboratory · Long Island · summer 1953', t, SW(1, 'brookhaven'), 1e9, { size: 40 });
  // an office door with two nameplates (no people)
  const door = rough([[760, 300], [1160, 300], [1160, 960], [760, 960]], 2, 4, 12);
  cels(g, door, { base: '#7a5232', sh: '#5a3a22', sw: 10, L: LIGHT, line: PAL.ink, lw: 3, seed: 4 });
  cels(g, rough([[800, 340], [1120, 340], [1120, 560], [800, 560]], 1.5, 5, 10), { base: '#cfe0e8', sh: '#a8c0cc', sw: 6, L: LIGHT, line: PAL.ink, lw: 2.4, seed: 5 });
  particle(g, 1110, 700, 14, '#d79a2b', { glow: false });
  nameplate(g, 440, 520, 'Chen Ning Yang', '“Frank” · age 30', t, SW(1, 'yang'));
  nameplate(g, 1480, 520, 'Robert Mills', 'age 26', t, SW(1, 'mills'));
  tag(g, 960, 1010, 'one shared office', t, SW(1, 'office'), 1e9, { size: 32, bg: '#fbe2c0' });
});

board('phase', SW(1, 'chasing') - 0.3, 'blue', 111, (g, t, S) => {
  const T0 = S._sh.t0, tD = SW(1, 'different');
  tag(g, 960, 120, 'electromagnetism’s hidden symmetry', t, SW(1, 'hidden'), 1e9, { size: 40 });
  // an electron's wave with a phase dial at each point; after "different" each dial turns by its own amount
  const wave = []; for (let i = 0; i <= 200; i++) { const x = 200 + i * 7.6; wave.push([x, 470 - 70 * Math.sin(i * 0.12 - (t - T0) * 1.5)]); }
  ink(g, wave, 5, { col: PAL.blue, a: 0.8, t0: 0.05, t1: 0.05, seed: 3 });
  for (let k = 0; k < 9; k++) {
    const x = 260 + k * 175, y = 680, R = 58, local = t > tD ? E.io2(seg(t, tD, tD + 1.5)) * (1.8 * Math.sin(k * 0.9 + 0.5)) : 0, a = 0.6 + k * 0.4 - (t - T0) * 0.0 + local;
    const disc = []; for (let i = 0; i < 24; i++) disc.push([x + Math.cos(i / 24 * TAU) * R, y + Math.sin(i / 24 * TAU) * R]);
    cels(g, disc, { base: '#f6eedc', sh: PAL.cardSh, sw: 4, L: LIGHT, line: PAL.ink, lw: 2.4, seed: k });
    ink(g, [[x, y], [x + Math.cos(a) * R * 0.82, y - Math.sin(a) * R * 0.82]], 6, { col: PAL.red, t0: 0.02, t1: 0.5, min: 0.3, seed: k });
  }
  tag(g, 960, 820, 'a dial (the “phase”) at every point', t, SW(1, 'phase'), 1e9, { size: 36 });
  tag(g, 960, 920, 'turn each dial differently: physics unchanged …', t, tD, 1e9, { size: 34 });
  tag(g, 960, 1010, '… if a field keeps track: the electromagnetic field (its particle: the photon)', t, SW(1, 'track'), 1e9, { size: 30, bg: '#fbe2c0' });
});

board('book', SW(1, 'asked') - 0.3, 'warm', 121, (g, t, S) => {
  const tF = SW(1, 'forward'), tR = SW(1, 'round'), q = Math.PI / 2;
  title(g, 960, 120, 'what if the order of rotations matters?', t, SW(1, 'order') - 0.4, 1e9, 52, PAL.ink, { shadow: false });
  // left: forward, then sideways. right: sideways, then forward. (world-frame rotations: v' = R2 R1 v)
  const a1 = E.io2(seg(t, tF, tF + 1.2)) * q, b1 = E.io2(seg(t, tF + 1.4, tF + 2.6)) * q;
  const b2 = E.io2(seg(t, tR, tR + 1.2)) * q, a2 = E.io2(seg(t, tR + 1.4, tR + 2.6)) * q;
  drawBook(g, 560, 560, 230, M3(RY(b1), RX(-a1)));
  drawBook(g, 1360, 560, 230, M3(RX(-a2), RY(b2)));
  tag(g, 560, 860, 'forward, then sideways', t, tF, 1e9, { size: 34 });
  tag(g, 1360, 860, 'sideways, then forward', t, tR, 1e9, { size: 34 });
  stamp(g, 960, 980, 'DIFFERENT ENDINGS', t, SW(1, 'facing'), 1e9, { size: 50 });
});

deepScene('new-field', SW(1, 'demand') - 0.3, 131, (g, t, S) => {
  const T0 = S._sh.t0;
  title(g, 960, 160, 'a new field, forced into existence', t, SW(1, 'forces'), 1e9, 56);
  // two field quanta that act on each other: wavy tracks that bend as they pass (schematic)
  for (let k = 0; k < 2; k++) {
    const pts = []; for (let i = 0; i <= 160; i++) { const s = i / 160, x = 300 + s * 1320, bend = 160 * Math.tanh((s - 0.5) * 6) * (k ? -1 : 1) * seg(t, SW(1, 'strange'), SW(1, 'strange') + 1.5); pts.push([x, 540 + (k ? 1 : -1) * (220 - Math.abs(s - 0.5) * 300) + bend * 0.6 + 18 * Math.sin(s * 60 - (t - T0) * 6)]); }
    ink(g, pts, 5, { col: k ? '#8fc4e8' : QC.glu, a: 0.9, t0: 0.05, t1: 0.05, seed: k });
  }
  tag(g, 960, 840, 'its particles carry the very charge they respond to', t, SW(1, 'charge'), 1e9, { size: 38 });
  tag(g, 960, 950, 'so they act on each other', t, SW(1, 'other', 2), 1e9, { size: 36, bg: '#fbe2c0' });
});

chalkScene('seminar', SW(1, 'february') - 0.3, 141, (g, t, S) => {
  tag(g, 960, 110, 'Princeton · February 1954 · Yang’s seminar', t, SW(1, 'february'), 1e9, { size: 36 });
  chalkText(g, '(∂μ − iεBμ) ψ', 960, 330, 110, t, SW(1, 'line') - 0.2, 1.3, { align: 'center' });
  tag(g, 300, 520, 'in the audience: Wolfgang Pauli', t, SW(1, 'pauli'), SW(1, 'asked', 2) - 0.2, { size: 32 });
  quoteCard(g, 'What is the mass of this field?', 'Wolfgang Pauli (as Yang recalled it)', t, SW(1, 'asked', 2), SW(1, 'replied') - 0.4, { y: 640, size: 54, w: 1000 });
  tag(g, 960, 600, 'Yang: “we did not know” · “a very complicated problem”', t, SW(1, 'know') - 0.2, SW(1, 'replied') - 0.4, { size: 34 });
  quoteCard(g, 'That is not sufficient excuse.', 'Wolfgang Pauli', t, SW(1, 'replied'), SW(1, 'sat') - 0.3, { y: 640, size: 58, w: 1000 });
  if (t > SW(1, 'sat') - 0.2) { g.save(); g.globalAlpha = seg(t, SW(1, 'sat') - 0.2, SW(1, 'sat') + 0.4); chair(g, 520, 790, 170); g.restore(); tag(g, 520, 1000, 'he sat down', t, SW(1, 'sat'), 1e9, { size: 30 }); }
  tag(g, 1250, 560, '“general embarrassment”', t, SW(1, 'embarrassment'), 1e9, { size: 34, italic: true });
  quoteCard(g, 'We should let Frank proceed.', 'Robert Oppenheimer', t, SW(1, 'oppenheimer'), 1e9, { x: 1250, y: 800, size: 46, w: 760 });
}, {});

board('note', SW(1, 'next') - 0.3, 'warm', 151, (g, t, S) => {
  docPage(g, 560, 520, 700, 520, t, SW(1, 'note') - 0.2, { head: 'February 24 · Dear Yang,', lines: 6 });
  quoteCard(g, 'I regret that you made it almost impossible for me to talk with you after the seminar.', 'W. Pauli, a note the next day', t, SW(1, 'regret'), SW(1, 'devastating') - 0.3, { x: 1260, y: 520, w: 720, size: 40 });
  tag(g, 1300, 300, 'the mathematics said: mass = 0', t, SW(1, 'zero'), 1e9, { size: 38 });
  tag(g, 1300, 480, 'massless, like the photon', t, SW(1, 'photon', 2), 1e9, { size: 36 });
  tag(g, 1300, 660, '→ a force that reaches forever', t, SW(1, 'forever'), 1e9, { size: 36 });
  stamp(g, 1300, 860, 'NEVER SEEN IN A NUCLEUS', t, SW(1, 'nucleus'), 1e9, { size: 46 });
});

board('paper', SW(1, 'published') - 0.3, 'blue', 161, (g, t, S) => {
  tag(g, 960, 150, 'Physical Review 96, 191 · October 1954', t, SW(1, 'published'), 1e9, { size: 40 });
  quoteCard(g, 'We next come to the question of the mass of the b quantum, to which we do not have a satisfactory answer.', 'C. N. Yang & R. L. Mills', t, SW(1, 'print'), SW(1, 'hold') - 0.3, { y: 500, size: 46 });
  stamp(g, 960, 520, 'HOLD ON TO THIS QUESTION', t, SW(1, 'hold'), 1e9, { size: 56 });
  tag(g, 960, 760, '72 years later: the half the machine left untouched', t, SW(1, 'later'), 1e9, { size: 40, bg: '#fbe2c0' });
});

// ================= 2. Why light reaches forever =================
board('dipole', SB(2) - 0.3, 'warm', 201, (g, t, S) => {
  const tA = SW(2, 'apart'), sep = E.io2(seg(t, tA, tA + 1.6));
  const near = S.near || (S.near = fieldLines([{ x: 800, y: 540, q: 1 }, { x: 1120, y: 540, q: -1 }], { n: 22 }));
  const far = S.far || (S.far = fieldLines([{ x: 560, y: 540, q: 1 }, { x: 1360, y: 540, q: -1 }], { n: 22 }));
  const u = seg(t, SW(2, 'spring') - 0.2, SW(2, 'spring') + 1.6);
  g.globalAlpha = 1 - sep; drawLines(g, near, u, { col: PAL.blue, w: 3 }); g.globalAlpha = sep; drawLines(g, far, 1, { col: PAL.blue, w: 3 }); g.globalAlpha = 1;
  const px = lerp(800, 560, sep), mx = lerp(1120, 1360, sep);
  particle(g, px, 540, 30, '#d8452e', { label: '+' }); particle(g, mx, 540, 30, '#3a6fd0', { label: '−' });
  tag(g, 960, 130, 'field lines of two opposite charges (computed)', t, SW(2, 'table'), 1e9, { size: 36 });
  tag(g, 960, 960, 'further out, the lines thin out', t, SW(2, 'thinner'), 1e9, { size: 36, bg: '#fbe2c0' });
});

board('inverse', SW(2, 'thinning') - 0.4, 'blue', 211, (g, t, S) => {
  const map = axes(g, 300, 900, 1300, 640, { x0: 0, x1: 4, y0: 0, y1: 1.1, xl: 'distance', yl: 'force', a: 1 });
  const tC = SW(2, 'thinning') - 0.2; if (t > tC) plot(g, map, r => 0.25 / (r * r), 0.48, 4, seg(t, tC, tC + 2), { col: PAL.red, w: 6, clipY: 260 });
  const tD = SW(2, 'double'); if (t > tD) { const [x1, y1] = map(1, 0.25), [x2, y2] = map(2, 0.0625); glowDot(g, x1, y1, 22, PAL.gold, 1); glowDot(g, x2, y2, 22, PAL.gold, seg(t, tD + 0.6, tD + 1)); tag(g, x1 + 40, y1 - 90, 'distance 1 → force 1', t, tD, 1e9, { size: 30 }); tag(g, x2 + 60, y2 - 110, 'distance 2 → force ¼', t, SW(2, 'quarter'), 1e9, { size: 30, bg: '#fbe2c0' }); }
  tag(g, 960, 140, 'the inverse square law', t, SW(2, 'inverse'), 1e9, { size: 44 });
  tag(g, 1350, 560, 'never zero: it fades forever', t, SW(2, 'forever'), 1e9, { size: 34 });
  stamp(g, 1350, 720, 'PHOTON MASS = 0', t, SW(2, 'mass'), 1e9, { size: 44 });
});

board('loan', SW(2, 'feel') - 0.3, 'sage', 221, (g, t, S) => {
  title(g, 960, 150, 'a force carrier, “borrowed” for a moment', t, SW(2, 'borrowed'), 1e9, 52, PAL.ink, { shadow: false });
  const row = (y, len, col, label, tIn) => { if (t < tIn) return; const u = E.out3(seg(t, tIn, tIn + 1)); particle(g, 300, y, 24, col); arrow(g, 340, y, 340 + len * u, y, 7, col); tag(g, 340 + len + 220, y, label, t, tIn + 0.6, 1e9, { size: 32 }); };
  row(420, 260, PAL.red, 'heavy: short loan, short reach', SW(2, 'heavier'));
  row(640, 1000, PAL.blue, 'massless: any distance', SW(2, 'massless', 1));
  tag(g, 960, 900, 'a loose picture: it breaks down if pushed too hard', t, SW(2, 'loose'), 1e9, { size: 32, bg: '#fbe2c0' });
});

deepScene('yukawa', SW(2, 'formula') - 0.4, 231, (g, t, S) => {
  g.save(); g.fillStyle = css('#f3e8d2', 0.92); fillPts(g, rough([[150, 160], [1100, 160], [1100, 960], [150, 960]], 2, 3, 12), css('#f3e8d2', 0.94)); g.restore();
  const map = axes(g, 230, 880, 800, 620, { x0: 0, x1: 4, y0: 0, y1: 1.05, xl: 'distance', yl: 'force', fs: 28 });
  plot(g, map, r => 0.25 / (r * r), 0.5, 4, seg(t, S._sh.t0 + 0.2, S._sh.t0 + 1.5), { col: PAL.blue, w: 5, clipY: 260 });
  const tY = SW(2, 'exponentially'); if (t > SW(2, 'yukawa')) plot(g, map, r => 0.25 * Math.exp(-2.2 * (r - 0.5)) / (r * r), 0.5, 4, seg(t, SW(2, 'yukawa'), tY + 0.5), { col: PAL.red, w: 6, clipY: 260 });
  tag(g, 820, 360, 'massless carrier: fades (1/r²)', t, S._sh.t0 + 0.8, 1e9, { size: 26 });
  tag(g, 560, 800, 'heavy carrier: dies off exponentially', t, tY, 1e9, { size: 26, bg: '#fbe2c0' });
  tag(g, 1480, 230, 'Hideki Yukawa · 1935', t, SW(2, 'yukawa'), 1e9, { size: 38 });
  // a torch beam swallowed by fog (intensity ~ e^-x)
  const tF = SW(2, 'fog') - 0.6; if (t > tF) { const a = seg(t, tF, tF + 0.8); for (let i = 0; i < 40; i++) { const x = 1180 + i * 17, I = Math.exp(-i / 9); g.fillStyle = css('#fff2c8', 0.6 * I * a); g.beginPath(); g.moveTo(x, 560 - 30 - i * 3); g.lineTo(x + 18, 560 - 30 - (i + 1) * 3); g.lineTo(x + 18, 560 + 30 + (i + 1) * 3); g.lineTo(x, 560 + 30 + i * 3); g.fill(); } motes(g, t, 60, 9, [1180, 380, 700, 360], '#c8d0e0', 0.25 * a); tag(g, 1500, 800, 'like a torch beam in fog', t, SW(2, 'fog'), 1e9, { size: 32 }); }
});

board('rule', SW(2, 'rule') - 0.3, 'warm', 241, (g, t, S) => {
  title(g, 960, 120, 'the rule', t, SW(2, 'rule'), 1e9, 64, PAL.ink, { shadow: false });
  checklist(g, 260, 300, [['massless carrier → infinite reach', '✓', SW(2, 'massless', 2)], ['heavy carrier → short reach', '✓', SW(2, 'heavy', 3)]], t, { size: 40 });
  tag(g, 520, 600, 'photon · mass 0 · reaches across galaxies', t, SW(2, 'galaxies'), 1e9, { size: 32 });
  tag(g, 520, 720, 'weak force · < 1/1000 of a proton’s width', t, SW(2, 'weak'), 1e9, { size: 32 });
  // carrier masses vs the proton (GeV): W 80.4, Z 91.2, proton 0.938
  if (t > SW(2, 'hundred')) { const u = E.out3(seg(t, SW(2, 'hundred'), SW(2, 'hundred') + 1.2)); [['W', 80.4, '#b8361f'], ['Z', 91.2, '#8a2a18'], ['proton', 0.938, '#3a6fd0']].forEach(([n, m, c], k) => { const h = m / 91.2 * 420 * u + 4, x = 1240 + k * 180; cels(g, [[x, 900 - h], [x + 110, 900 - h], [x + 110, 900], [x, 900]], { base: c, line: PAL.ink, lw: 2.4, seed: k }); g.font = '600 28px Georgia'; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.fillText(n, x + 55, 940); }); tag(g, 1420, 400, 'its carriers: ≈ 90 × a proton', t, SW(2, 'hundred') + 0.4, 1e9, { size: 30 }); }
  tag(g, 960, 1010, 'Yang–Mills: massless … so it should reach forever?', t, SW(2, 'beautiful'), SW(2, 'stranger') - 0.3, { size: 34, bg: '#fbe2c0' });
  stamp(g, 700, 1010, 'MASSLESS — AND IT STILL STOPS', t, SW(2, 'stops'), 1e9, { size: 46 });
});

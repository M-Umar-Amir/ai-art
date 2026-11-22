// ================= 1. The pattern in the primes =================
board('sieve', SB(1) - 0.3, 'warm', 101, (g, t, S) => {
  const steps = [[2, SW(1, 'circle'), 0.05], [3, SW(1, 'three'), 0.06], [5, SW(1, 'five'), 0.09], [7, SW(1, 'seven'), 0.12]];
  const tP = SW(1, 'primes'), tE = SW(1, 'eleven'), tG = SW(1, 'gap');
  sieveGrid(g, 160, 60, 96, t, steps, { revealPrimes: tP, hl: (n, tt) => (tt > tE && tt < tG && (n === 11 || n === 13)) || (tt > tG && n >= 89 && n <= 97 && (n === 89 || n === 97)) });
  tag(g, 1500, 220, 'the sieve of Eratosthenes', t, SW(1, 'sieve'), 1e9, { size: 38 });
  tag(g, 1500, 360, 'more than 2,000 years old', t, SW(1, 'thousand'), 1e9, { size: 32 });
  tag(g, 1500, 520, '25 primes below 100', t, SW(1, 'survives'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 1500, 680, '11 and 13: side by side', t, tE, tG - 0.2, { size: 34 });
  tag(g, 1500, 680, '89 … 97: a gap of seven', t, tG, 1e9, { size: 34 });
}, { cam: { z0: 1.0, z1: 1.03, x1: 980 } });

board('thinning', SW(1, 'further') - 0.3, 'sage', 111, (g, t, S) => {
  const rows = [['below 100', 25, SW(1, 'further') + 0.6], ['below 1,000', 168, SW(1, 'sixtyeight', 1, 0.3)], ['below 10,000', 1229, SW(1, 'below', 3)]];
  rows.forEach(([lab, n, tk], k) => {
    if (t < tk) return; const u = E.out3(seg(t, tk, tk + 0.8)), y = 260 + k * 170;
    tag(g, 380, y, lab, t, tk, 1e9, { size: 36 });
    fillPts(g, rough([[560, y - 30], [560 + 1100 * (n / 1229) * u, y - 30], [560 + 1100 * (n / 1229) * u, y + 30], [560, y + 30]], 2, k, 10), css(PAL.teal, 0.85));
    title(g, 600 + 1100 * (n / 1229) * u + 60, y, String(Math.round(n * u)), t, tk, 1e9, 44, PAL.ink, { shadow: false, upright: true, align: 'left' });
  });
  tag(g, 700, 830, 'they never stop (Euclid, c. 300 BC)', t, SW(1, 'euclid'), 1e9, { size: 34 });
  tag(g, 1350, 900, 'but they get rarer', t, SW(1, 'rarer'), 1e9, { size: 40, bg: '#fbe2c0' });
});

board('gauss', SW(1, 'surprise') - 0.3, 'warm', 121, (g, t, S) => {
  title(g, 960, 110, 'locally noise · from far away, a law', t, SW(1, 'noise'), 1e9, 52, PAL.ink, { shadow: false });
  tag(g, 1560, 260, 'Gauss, c. 1792, age 15', t, SW(1, 'gauss'), 1e9, { size: 34 });
  tag(g, 1560, 400, 'chance near N ≈ 1 / ln N', t, SW(1, 'logarithm'), 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 1560, 540, '10 digits: 1 in 23', t, SW(1, 'digits', 2), 1e9, { size: 34 });
  tag(g, 1560, 660, '100 digits: 1 in 230', t, SW(1, 'thirty'), 1e9, { size: 34 });
  const tS = SW(1, 'smooth'), map = axes(g, 180, 960, 1120, 700, { x0: 0, x1: 1000, y0: 0, y1: 185, xl: 'N', yl: 'primes up to N', a: seg(t, tS - 1.5, tS - 1), fs: 30 });
  if (t > tS - 1) plot(g, map, piCount, 1, 1000, seg(t, tS - 1, tS + 2), { col: PAL.ink, w: 3.5, n: 1000 });
  if (t > tS) plot(g, map, Li, 2, 1000, seg(t, tS, tS + 2.5), { col: PAL.red, w: 6, n: 200 });
  tag(g, 900, 300, 'Gauss\'s smooth curve', t, tS + 0.5, 1e9, { size: 32, col: PAL.red });
  tag(g, 1000, 600, 'true count', t, tS + 0.2, 1e9, { size: 30 });
}, { cam: { z0: 1.0, z1: 1.04 } });

board('pnt', SW(1, 'eighteen') - 0.4, 'blue', 131, (g, t, S) => {
  title(g, 960, 260, 'The Prime Number Theorem', t, SW(1, 'theorem') - 0.6, 1e9, 84, PAL.ink, { shadow: false });
  tag(g, 960, 120, '1896', t, SW(1, 'eighteen'), 1e9, { size: 54, bg: '#fbe2c0' });
  tag(g, 620, 440, 'Jacques Hadamard', t, SW(1, 'hadamard'), 1e9, { size: 40 });
  tag(g, 1300, 440, 'Charles de la Vallée Poussin', t, SW(1, 'charles'), 1e9, { size: 40 });
  // the wobble: pi(N) - Li(N), magnified
  const tW = SW(1, 'wobbles'), map = axes(g, 260, 860, 1400, 260, { x0: 0, x1: 20000, y0: -40, y1: 10, xl: 'N', yl: 'true count − smooth curve', a: seg(t, tW - 0.5, tW), fs: 28 });
  if (t > tW) { const pc = []; let c = 0, j = 0; plot(g, map, x => { while (j < PRIMES.length && PRIMES[j] <= x) { c++; j++; } return c - Li(x); }, 3, 20000, seg(t, tW, tW + 3), { col: PAL.red, w: 3.5, n: 500 }); }
  tag(g, 1500, 940, 'how big can the wobble get?', t, SW(1, 'haunt'), 1e9, { size: 38, bg: '#fbe2c0' });
});

// ================= 2. Riemann's eight pages =================
board('riemann', SB(2) - 0.3, 'warm', 201, (g, t, S) => {
  title(g, 960, 200, 'Bernhard Riemann', t, SB(2), 1e9, 92, PAL.ink, { shadow: false });
  tag(g, 960, 340, 'Göttingen', t, SW(2, 'worked'), 1e9, { size: 40 });
  tag(g, 560, 520, '1859 · elected to the Berlin Academy', t, SW(2, 'academy'), 1e9, { size: 36 });
  tag(g, 1380, 560, '≈ 8 pages', t, SW(2, 'eight'), 1e9, { size: 48, bg: '#fbe2c0' });
  tag(g, 960, 720, 'his only paper on prime numbers', t, SW(2, 'only'), 1e9, { size: 38 });
  tag(g, 960, 900, 'built on an idea of Leonhard Euler', t, SW(2, 'euler'), 1e9, { size: 36 });
});

board('sums', SW(2, 'take') - 0.3, 'sage', 211, (g, t, S) => {
  const tA = SW(2, 'take'), tB = SW(2, 'square');
  writeText(g, '1 + ½ + ⅓ + ¼ + …', 260, 170, 64, t, tA, 1.2, { col: PAL.ink });
  writeText(g, '1 + ¼ + ⅑ + 1/16 + …', 1060, 170, 64, t, tB, 1.2, { col: PAL.ink });
  const m1 = axes(g, 200, 900, 640, 560, { x0: 0, x1: 400, y0: 0, y1: 7, xl: 'terms', yl: 'running total', a: seg(t, tA, tA + 0.5), fs: 28 });
  const H = n => { let s = 0; for (let k = 1; k <= n; k++) s += 1 / k; return s; }, Q = n => { let s = 0; for (let k = 1; k <= n; k++) s += 1 / (k * k); return s; };
  if (t > tA) plot(g, m1, x => H(Math.floor(x)), 1, 400, seg(t, tA, SW(2, 'infinity') + 0.5), { col: PAL.red, w: 5, n: 400 });
  tag(g, 560, 360, 'grows forever', t, SW(2, 'infinity'), 1e9, { size: 34, col: PAL.red });
  const m2 = axes(g, 1040, 900, 640, 560, { x0: 0, x1: 400, y0: 0, y1: 7, xl: 'terms', a: seg(t, tB, tB + 0.5), fs: 28 });
  if (t > tB) { plot(g, m2, x => Q(Math.floor(x)), 1, 400, seg(t, tB, tB + 2), { col: PAL.blue, w: 5, n: 400 }); }
  tag(g, 1380, 600, 'settles at 1.6449… (π²/6)', t, SW(2, 'finite'), 1e9, { size: 32, col: PAL.blue });
});

board('euler', SW(2, 'magical') - 0.4, 'warm', 221, (g, t, S) => {
  tag(g, 960, 160, 'Euler, 1737', t, SW(2, 'seventeen'), 1e9, { size: 46, bg: '#fbe2c0' });
  const tP = SW(2, 'product');
  writeText(g, 'Σ  1/nˢ', 620, 420, 100, t, tP - 0.6, 0.8, { align: 'center' });
  writeText(g, '=', 960, 420, 100, t, tP, 0.3, { align: 'center' });
  writeText(g, 'Π  1/(1 − p⁻ˢ)', 1340, 420, 100, t, tP + 0.3, 0.9, { align: 'center' });
  tag(g, 620, 560, 'all whole numbers', t, SW(2, 'whole'), 1e9, { size: 34 });
  tag(g, 1340, 560, 'only the primes', t, SW(2, 'primes'), 1e9, { size: 34, bg: '#fbe2c0' });
  tag(g, 960, 760, 'every number = primes multiplied, in exactly one way', t, SW(2, 'bridge', 2), 1e9, { size: 36 });
  writeText(g, '360 = 2 × 2 × 2 × 3 × 3 × 5', 960, 900, 56, t, SW(2, 'bridge', 2) + 0.8, 1.2, { align: 'center', col: PAL.blue });
});

deepScene('complex', SW(2, 'complex') - 0.4, 231, (g, t, S) => {
  const T0 = S._sh.t0, map = stripPlane(g, 360, 140, 1200, 820, { s0: -3, s1: 3, t0: -10, t1: 30, strip: false, line: false, labels: false, ink: '#e8eef4', a: seg(t, T0, T0 + 0.8) });
  const tl = t - T0, x = 0.8 * Math.cos(tl * 0.5) + 0.6, y = 12 + 9 * Math.sin(tl * 0.37), [px, py] = map(x, y);
  ink(g, [map(-3, y), [px, py]], 2, { col: '#8fc4e8', a: 0.5, t0: 0, t1: 0 }); ink(g, [map(x, -10), [px, py]], 2, { col: '#8fc4e8', a: 0.5, t0: 0, t1: 0 });
  zeroLantern(g, px, py, 10);
  tag(g, px + 160, py - 60, `s = ${x.toFixed(1)} + ${y.toFixed(1)} i`, t, T0 + 0.6, 1e9, { size: 32, rot: 0 });
  tag(g, 1640, 960, 'real part →', t, SW(2, 'parts'), 1e9, { size: 30 });
  tag(g, 260, 180, '↑ imaginary part', t, SW(2, 'imaginary'), 1e9, { size: 30 });
  title(g, 960, 80, 'a number is now a point on a map', t, SW(2, 'map'), 1e9, 48);
});

scene('landscape', SW(2, 'zeta') - 0.4, () => ({ img: zetaLandscape(-1, 2, 0, 46, 360, 552) }), (g, l, t, S) => {
  const T0 = S._sh.t0, u = seg(t, T0, S._sh.t1), z = lerp(1.0, 1.12, E.io2(u));
  g.fillStyle = '#06101f'; g.fillRect(0, 0, W, H);
  g.save(); cam(g, 960, 540, z); g.imageSmoothingQuality = 'high'; g.drawImage(S.img, 600, 0, 720, 1104);
  const map = (s, tt) => [600 + (s + 1) / 3 * 720, 1104 - tt / 46 * 1104];
  if (t > SW(2, 'zeros')) ZEROS.slice(0, 8).forEach((gm, k) => { const tk = SW(2, 'zeros') + k * 0.2; if (t > tk) { const [x, y] = map(0.5, gm); brushCircle(g, x, y, 26, 26, t, tk, '#fff1cf', 4); } });
  g.restore();
  title(g, 300, 160, 'the zeta function', t, SW(2, 'zeta'), 1e9, 52);
  tag(g, 300, 300, 'computed: log |ζ(s)|', t, SW(2, 'zeta') + 0.6, 1e9, { size: 28 });
  tag(g, 1620, 380, 'hills', t, SW(2, 'hills'), 1e9, { size: 36 });
  tag(g, 1620, 520, 'holes = zeros', t, SW(2, 'hole'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 300, 760, 'know every zero →\nthe exact count of primes', t, SW(2, 'exact'), 1e9, { size: 34 });
}, { in: { type: 'dissolve', dur: 0.8 } });

board('trivial', SW(2, 'boring') - 0.4, 'blue', 241, (g, t, S) => {
  const T0 = S._sh.t0, tS = SW(2, 'interesting');
  if (t < tS + 0.3) {
    const a = 1 - seg(t, tS, tS + 0.3); g.globalAlpha = a;
    ink(g, [[160, 400], [1760, 400]], 4, { col: PAL.ink, t0: 0, t1: 0 });
    for (let k = -8; k <= 2; k++) { const x = 1460 + k * 150; ink(g, [[x, 385], [x, 415]], 3, { col: PAL.ink, t0: 0, t1: 0 }); g.font = 'italic 500 30px Georgia'; g.fillStyle = PAL.ink; g.textAlign = 'center'; g.fillText(k, x, 460); }
    [-2, -4, -6, -8].forEach((k, i) => { const tk = SW(2, 'minus') + i * 0.35; if (t > tk) { zeroLantern(g, 1460 + k * 150, 400, 12); } });
    tag(g, 860, 620, 'the trivial zeros', t, SW(2, 'trivial'), 1e9, { size: 44 });
    g.globalAlpha = 1;
  }
  if (t > tS) {
    const map = stripPlane(g, 560, 120, 800, 840, { s0: -0.5, s1: 1.5, t0: 0, t1: 50, a: seg(t, tS, tS + 0.5), line: t > SW(2, 'half') });
    tag(g, 960, 1010, 'every interesting zero is in the strip 0 – 1', t, SW(2, 'strip'), 1e9, { size: 32 });
    ZEROS.slice(0, 3).forEach((gm, k) => { const tk = SW(2, 'hand') + 0.3 + k * 0.6; if (t > tk) { const [x, y] = map(0.5, gm); zeroLantern(g, x, y, 11); } });
    tag(g, 1560, 300, 'Riemann computed a few by hand', t, SW(2, 'hand'), 1e9, { size: 30 });
    tag(g, 1560, 460, 'all on Re(s) = ½', t, SW(2, 'half'), 1e9, { size: 36, bg: '#fbe2c0' });
  }
}, { cam: { z0: 1.0, z1: 1.03 } });

board('rh-statement', SW(2, 'probable') - 0.6, 'warm', 251, (g, t, S) => {
  quoteCard(g, '…it is very probable that all roots are real. … I have put aside the search for a proof after some fleeting vain attempts.', 'Riemann, 1859 (translated)', t, SW(2, 'probable') - 0.4, SW(2, 'sentence') - 0.3, { size: 46, y: 480 });
  tag(g, 960, 860, '"roots are real" = on the line, in Riemann\'s notation', t, SW(2, 'fleeting'), SW(2, 'sentence') - 0.3, { size: 28 });
  title(g, 960, 420, 'Every interesting zero of the zeta function\nlies on the line Re(s) = ½', t, SW(2, 'sentence'), 1e9, 62, PAL.ink, { shadow: false });
  stamp(g, 960, 740, 'THE RIEMANN HYPOTHESIS', t, SW(2, 'hypothesis') + 0.2, 1e9, { size: 56, rot: -0.04 });
});

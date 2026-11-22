// ================= 3. The zeros make the music =================
// the explicit formula, live: the exact prime staircase psi(x) vs Gauss's curve + waves from the first N zeros
board('staircase', SB(3) - 0.3, 'warm', 301, (g, t, S) => {
  const tS = SW(3, 'staircase'), tG = SW(3, 'slope'), tW = SW(3, 'wave');
  title(g, 960, 90, 'why should a line have anything to do with primes?', t, SB(3), tS + 1, 46, PAL.ink, { shadow: false });
  const map = axes(g, 170, 980, 1240, 780, { x0: 0, x1: 50, y0: -2, y1: 56, xl: 'n', yl: 'prime staircase', a: seg(t, tS - 0.4, tS), fs: 30 });
  if (t > tS) plot(g, map, psiTrue, 1.5, 50, seg(t, tS, tS + 2.5), { col: '#9a8a70', w: 4, n: 800, a: 0.8 });
  [2, 3, 5, 7, 11, 13].forEach((p, k) => { if (t > SW(3, 'step') + k * 0.2) { const [x, y] = map(p, psiTrue(p)); tag(g, x, y - 40, String(p), t, SW(3, 'step') + k * 0.2, tW, { size: 24 }); } });
  // N zeros: steps at each cue
  const keys = [[tW - 1, 0], [SW(3, 'fourteen'), 1], [SW(3, 'second'), 2], [SW(3, 'third'), 3], [SW(3, 'ten'), 10], [SW(3, 'thirty'), 30], [SW(3, 'hundred'), 100], [SW(3, 'hundred') + 6, 300]];
  let N = 0; for (let i = 0; i < keys.length; i++) if (t > keys[i][0]) N = i + 1 < keys.length ? lerp(keys[i][1], keys[i + 1][1], E.io2(seg(t, keys[i][0], keys[i][0] + 1.2))) : keys[i][1];
  if (t > tG) plot(g, map, x => psiZeros(x, N), 1.6, 50, seg(t, tG, tG + 1.5), { col: PAL.red, w: 5, n: 900 });
  if (t > tG) tag(g, 1640, 220, N < 0.5 ? "Gauss's smooth curve" : `+ waves from ${Math.round(N)} zero${Math.round(N) === 1 ? '' : 's'}`, t, tG, 1e9, { size: 36, bg: '#fbe2c0', rot: 0 });
  if (t > SW(3, 'fourteen')) tag(g, 1640, 360, 'first zero: ½ + 14.13 i', t, SW(3, 'fourteen'), SW(3, 'ten'), { size: 30 });
  tag(g, 1640, 520, 'computed with the real zeros', t, SW(3, 'ten'), 1e9, { size: 28 });
  title(g, 960, 1040, 'the primes are a sum of waves · the zeros are the notes', t, SW(3, 'sum'), 1e9, 44, PAL.ink, { shadow: false });
}, { cam: { z0: 1.0, z1: 1.03, x1: 965 } });

board('loudness', SW(3, 'matters') - 0.4, 'blue', 311, (g, t, S) => {
  const T0 = S._sh.t0, tF = SW(3, 'faster'), tO = SW(3, 'orchestra');
  // wave from a zero at sigma: amplitude x^sigma.  sigma = 0.5 (on the line) vs 0.8 (off the line)
  const draw = (y, sigma, col, tIn, lab) => {
    if (t < tIn) return; const m = axes(g, 200, y, 1300, 150, { x0: 1, x1: 400, y0: -1, y1: 1, a: seg(t, tIn, tIn + 0.4), fs: 26 });
    plot(g, m, x => Math.pow(x, sigma) * Math.cos(14.1347 * Math.log(x)) / Math.pow(400, 0.8), 1, 400, seg(t, tIn, tIn + 2.5), { col, w: 4, n: 900 });
    tag(g, 1700, y - 70, lab, t, tIn + 0.3, 1e9, { size: 32 });
  };
  draw(360, 0.5, PAL.blue, SW(3, 'loud'), 'zero on the line:\ngrows like √x');
  draw(820, 0.8, PAL.red, tF, 'zero off the line:\ngrows like x⁰·⁸');
  if (t > tO) { const k = seg(t, tO, tO + 0.5); title(g, 960, 1010, 'one rogue instrument drowns out the rest', t, tO, 1e9, 42, PAL.ink, { shadow: false }); }
}, { cam: { z0: 1.0, z1: 1.04 } });

deepScene('harmony', SW(3, 'harmony') - 0.6, 321, (g, t, S) => {
  const T0 = S._sh.t0;
  for (let k = 0; k < 9; k++) { const y = 200 + k * 80, pts = []; for (let i = 0; i <= 220; i++) { const x = 2 + i / 220 * 60; pts.push([240 + i * 6.4, y - 26 * Math.cos(ZEROS[k] * Math.log(x) - (t - T0) * 0.8)]); } ink(g, pts, 3, { col: '#bfe4ff', a: 0.75, t0: 0.1, t1: 0.1, seed: k }); }
  title(g, 960, 120, 'no instrument plays out of turn', t, SW(3, 'instrument', 2), 1e9, 56);
  tag(g, 960, 960, 'the primes: as evenly spread as they could possibly be', t, SW(3, 'evenly'), 1e9, { size: 36 });
});

board('wobble-size', SW(3, 'trillion') - 1.2, 'sage', 331, (g, t, S) => {
  tag(g, 960, 150, 'if Riemann was right: wobble ≈ √N', t, S._sh.t0 + 0.2, 1e9, { size: 44 });
  tag(g, 560, 400, 'primes below a trillion:\n' + rollNum(t, SW(3, 'billion') - 0.5, SW(3, 'billion') + 1, 0, 37607912018), t, SW(3, 'trillion'), 1e9, { size: 44 });
  tag(g, 1400, 400, 'guaranteed error:\nunder a few million', t, SW(3, 'million'), 1e9, { size: 44, bg: '#fbe2c0' });
  // bar: 37.6 billion vs ~1 million (the error bar is a hairline)
  const tB = SW(3, 'tiny'); if (t > tB) { const u = E.out3(seg(t, tB, tB + 1)); fillPts(g, rough([[200, 640], [200 + 1500 * u, 640], [200 + 1500 * u, 700], [200, 700]], 2, 3, 10), css(PAL.teal, 0.85)); fillPts(g, [[1700, 620], [1703, 620], [1703, 720], [1700, 720]], css(PAL.red)); tag(g, 1450, 790, 'the error (drawn far thicker than to scale)', t, tB + 0.8, 1e9, { size: 28 }); }
  stamp(g, 960, 930, 'IF WRONG: WILDER THAN ANYONE THINKS', t, SW(3, 'wilder'), 1e9, { size: 40 });
});

// ================= 4. Why the world cares =================
board('myth', SB(4) - 0.3, 'warm', 401, (g, t, S) => {
  tag(g, 960, 120, 'does any of this matter?', t, SB(4), 1e9, { size: 44 });
  lockBox(g, 560, 560, 220, 'YOUR BANK', t, SW(4, 'encryption'));
  tag(g, 1360, 380, 'p × q  →  easy', t, SW(4, 'easy'), 1e9, { size: 46 });
  tag(g, 1360, 520, 'N → p × q  →  hard', t, SW(4, 'hard'), 1e9, { size: 46, bg: '#fbe2c0' });
  tag(g, 1360, 680, 'RH says nothing about splitting numbers', t, SW(4, 'split'), 1e9, { size: 32 });
  stamp(g, 960, 880, 'MYTH: RH BREAKS ENCRYPTION', t, SW(4, 'tonight') - 0.4, 1e9, { size: 50 });
});

board('assume', SW(4, 'tired') - 0.4, 'sage', 411, (g, t, S) => {
  const tA = SW(4, 'assume'), tK = SW(4, 'thousand'), tB = SW(4, 'building');
  writeText(g, '"Assume the Riemann hypothesis is true…"', 960, 140, 54, t, tA - 0.2, 1.4, { align: 'center' });
  // a building of result-bricks on a foundation marked RH ?
  const n = Math.floor(lerp(0, 120, E.out2(seg(t, tA + 1, tK + 1.5))));
  for (let i = 0; i < n; i++) { const row = Math.floor(i / 12), col = i % 12, x = 500 + col * 76 + (row % 2) * 38, y = 860 - row * 52; fillPts(g, rough([[x, y], [x + 70, y], [x + 70, y + 46], [x, y + 46]], 1.2, i, 10), css(mixv('#c88a44', '#e9d7b0', hash(i, 2)), 0.95)); }
  if (t > tB) { const k = seg(t, tB, tB + 0.6); g.globalAlpha = k; fillPts(g, rough([[440, 910], [1480, 910], [1480, 990], [440, 990]], 2, 9, 12), css('#5a4a3a', 0.9)); g.font = '700 40px Georgia'; g.fillStyle = '#fff1cf'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText('foundation: RH (never inspected)', 960, 950); g.globalAlpha = 1; }
  tag(g, 1660, 420, '1,000+ results', t, tK, 1e9, { size: 46, bg: '#fbe2c0' });
  tag(g, 1660, 600, 'primality tests · gaps ·\nprimes among remainders', t, SW(4, 'remainders'), 1e9, { size: 28 });
}, { cam: { z0: 1.0, z1: 1.04, y1: 560 } });

deepScene('grh', SW(4, 'sibling') - 0.4, 421, (g, t, S) => {
  title(g, 960, 120, 'the Generalized Riemann Hypothesis', t, SW(4, 'generalized'), 1e9, 58);
  for (let k = 0; k < 5; k++) { const tk = SW(4, 'family') + k * 0.3; if (t < tk) continue; const x = 240 + k * 300, a = seg(t, tk, tk + 0.4);
    const m = stripPlane(g, x, 250, 240, 640, { s0: -0.3, s1: 1.3, t0: 0, t1: 30, a, ink: '#e8eef4', lineCol: '#ffcf7a', labels: false, fs: 22 });
    for (let j = 0; j < 7; j++) { const gm = 2 + hash(k * 31 + j, 4) * 27; const [zx, zy] = m(0.5, gm); zeroLantern(g, zx, zy, 6, a); }
    tag(g, x + 120, 950, k === 0 ? 'ζ(s)' : `L(s, χ${k})`, t, tk, 1e9, { size: 28 }); }
  tag(g, 960, 1030, 'positions shown for illustration', t, SW(4, 'family') + 1.5, 1e9, { size: 22 });
});

board('hilbert', SW(4, 'nineteen') - 0.4, 'blue', 431, (g, t, S) => {
  tag(g, 520, 140, '1900 · David Hilbert', t, SW(4, 'hilbert'), 1e9, { size: 44 });
  for (let k = 0; k < 23; k++) { const tk = SW(4, 'twentythree', 1, 0.3) + k * 0.05; if (t < tk) continue; const col = Math.floor(k / 12), row = k % 12, x = 200 + col * 420, y = 240 + row * 64; g.globalAlpha = seg(t, tk, tk + 0.3); g.font = `${k === 7 ? 700 : 500} 34px Georgia`; g.fillStyle = css(k === 7 ? PAL.red : PAL.ink); g.textAlign = 'left'; g.fillText(`${k + 1}. ${k === 7 ? 'Riemann hypothesis' : '……………'}`, x, y); g.globalAlpha = 1; }
  if (t > SW(4, 'twentythree', 1, 0.3) + 0.8) brushCircle(g, 330, 690, 210, 40, t, SW(4, 'twentythree', 1, 0.3) + 0.8, PAL.red, 6);
  quoteCard(g, 'If I were to awaken after having slept for a thousand years, my first question would be: has the Riemann hypothesis been proven?', 'attributed to David Hilbert', t, SW(4, 'joked') - 0.3, SW(4, 'clay') - 0.6, { x: 1300, y: 540, w: 760, size: 40 });
});

board('clay', SW(4, 'clay') - 0.4, 'sage', 441, (g, t, S) => {
  const tS = SW(4, 'clay'), names = ['P vs NP', 'Hodge', 'Poincaré', 'Riemann', 'Yang–Mills', 'Navier–Stokes', 'Birch–Swinnerton-Dyer'];
  names.forEach((nm, k) => { const x = 260 + k * 233, y = 470 + (k % 2) * 60, tk = tS + 0.15 * k; if (on2(t) < tk) return; envelope(g, x, y - 30 * (1 - E.out3(seg(on2(t), tk, tk + 0.3))), 200, nm, nm === 'Poincaré' ? 1 : 0, { glow: nm === 'Riemann' ? seg(t, tS + 1, tS + 2) * (0.7 + 0.3 * Math.sin(t * 3)) : 0, col: nm === 'Riemann' ? '#f3e2b8' : undefined }); });
  title(g, 960, 200, 'The Millennium Prize Problems · 2000', t, tS, 1e9, 60, PAL.ink, { shadow: false });
  tag(g, 960, 780, '$1,000,000', t, SW(4, 'million'), 1e9, { size: 56, bg: '#fbe2c0' });
  stamp(g, 960, 940, 'WHY SO HARD?', t, SW(4, 'wall') - 0.2, 1e9, { size: 56 });
}, { cam: { z0: 1.02, z1: 1.07 } });

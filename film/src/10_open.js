// ================= 0. Cold open =================
board('primes', 0, 'warm', 11, (g, t, S) => {
  const ws = ['two', 'three', 'five', 'seven', 'eleven', 'thirteen'], nums = [2, 3, 5, 7, 11, 13];
  nums.forEach((n, k) => { const tk = Math.max(0.3 + k * 0.3, SW(0, ws[k]) - 0.1); writeText(g, String(n), 330 + k * 230, 300, 120, t, tk, 0.35, { upright: true, col: PAL.ink }); });
  tag(g, 960, 470, "can't be split into smaller whole numbers", t, SW(0, 'split'), 1e9, { size: 36 });
  tag(g, 1500, 600, 'no formula', t, SW(0, 'formula'), 1e9, { size: 40, bg: '#fbe2c0' });
  // the prime staircase drawing itself (Chebyshev psi, exact)
  const tS = SW(0, 'random') - 0.4, map = axes(g, 260, 1000, 1000, 330, { x0: 0, x1: 40, y0: 0, y1: 40, xl: 'n', yl: 'primes so far (weighted)', a: seg(t, tS, tS + 0.5), fs: 28 });
  if (t > tS) plot(g, map, psiTrue, 1.5, 40, seg(t, tS, tS + 3), { col: PAL.red, w: 5, n: 600 });
}, { cam: { z0: 1.02, z1: 1.07 } });

deepScene('music', SW(0, 'eighteen') - 0.3, 21, (g, t, S) => {
  const T0 = S._sh.t0;
  title(g, 960, 150, 'Bernhard Riemann · 1859', t, T0 + 0.3, SW(0, 'line') - 0.5, 56);
  // pure tones: the first zeros as cosine waves, summed below
  const tT = SW(0, 'tones'), tL = SW(0, 'line');
  if (t > SW(0, 'music') - 0.3 && t < tL + 0.4) {
    const a = Math.min(seg(t, SW(0, 'music') - 0.3, SW(0, 'music') + 0.5), 1 - seg(t, tL, tL + 0.4));
    g.save(); g.globalAlpha = a;
    for (let k = 0; k < 5; k++) { const y = 330 + k * 95, pts = []; for (let i = 0; i <= 200; i++) { const x = 2 + i / 200 * 48; pts.push([260 + i * 7, y - 30 * Math.cos(ZEROS[k] * Math.log(x) - (t - T0) * 0.6)]); } ink(g, pts, 3.5, { col: '#bfe4ff', a: 0.8, t0: 0.1, t1: 0.1, seed: k }); }
    g.restore();
    tag(g, 960, 860, 'add them up → every prime, exactly', t, tT, tL, { size: 36 });
  }
  if (t > tL) {
    const map = stripPlane(g, 560, 160, 800, 820, { s0: -0.25, s1: 1.25, t0: 0, t1: 52, a: seg(t, tL, tL + 0.6), ink: '#e8eef4', lineCol: '#ffcf7a', stripCol: '#6fa8dc' });
    ZEROS.slice(0, 9).forEach((gm, k) => { const tk = tL + 0.5 + k * 0.25; if (t > tk) { const [x, y] = map(0.5, gm); zeroLantern(g, x, y, 9, seg(t, tk, tk + 0.3)); } });
    tag(g, 1560, 300, 'one straight line', t, tL + 0.4, 1e9, { size: 38 });
  }
}, { cam: { z0: 1.0, z1: 1.05 } });

deepScene('hypothesis', SW(0, 'guess', 2) - 0.3, 31, (g, t, S) => {
  title(g, 960, 380, 'The Riemann Hypothesis', t, SW(0, 'guess', 2), 1e9, 110, '#fff1cf');
  tag(g, 620, 620, 'most famous unsolved problem', t, SW(0, 'famous'), 1e9, { size: 38 });
  tag(g, 1320, 640, rollNum(t, SW(0, 'hundred'), SW(0, 'years') + 0.6, 0, 167) + ' years', t, SW(0, 'hundred'), 1e9, { size: 54, bg: '#fbe2c0' });
  stamp(g, 960, 820, 'FAILED', t, SW(0, 'failed'), 1e9, { size: 60 });
});

board('note', SW(0, 'days') - 0.4, 'blue', 41, (g, t, S) => {
  tag(g, 960, 150, '20 November 2026 · a four-page note', t, SW(0, 'days'), 1e9, { size: 40 });
  quoteCard(g, 'I believe I have, working with Claude, discovered a proof of the Riemann Hypothesis.', 'Levent Alpöge (Anthropic), alpo.ge', t, SW(0, 'quote'), 1e9, { y: 520, size: 54 });
  tag(g, 960, 880, "Claude = the company's AI model", t, SW(0, 'model'), 1e9, { size: 34, bg: '#fbe2c0' });
});

deepScene('code-wall', SW(0, 'paper') - 0.4, 51, (g, t, S) => {
  const T0 = S._sh.t0;
  codeScroll(g, 160, 140, 1000, 800, LEAN, t, T0, { speed: 60 });
  tag(g, 660, 990, 'illustrative Lean-style code, not the real proof', t, T0 + 0.6, 1e9, { size: 26 });
  tag(g, 1520, 300, rollNum(t, SW(0, 'seventeen'), SW(0, 'code') + 1.2, 0, 17e6) + '\nlines of code', t, SW(0, 'seventeen'), 1e9, { size: 52 });
  stamp(g, 1520, 620, 'CHECKED', t, SW(0, 'correct'), 1e9, { size: 64, col: '#6fd08a' });
  tag(g, 1520, 800, 'Lean & Mathlib communities, 21 Nov', t, SW(0, 'correct') + 0.5, 1e9, { size: 28 });
});

board('questions', SW(0, 'something', 2) - 0.4, 'warm', 61, (g, t, S) => {
  const T0 = S._sh.t0, tQ = SW(0, 'three', 2);
  title(g, 960, 300, 'something no mathematician\nhas ever had to face', t, T0 + 0.2, tQ - 0.4, 64, PAL.ink, { shadow: false });
  stamp(g, 960, 620, '?', t, SW(0, 'history') + 0.2, tQ - 0.4, { size: 120 });
  tag(g, 960, 280, '1 · What does the Riemann hypothesis say?', t, SW(0, 'actually'), 1e9, { size: 42 });
  tag(g, 960, 450, '2 · How did a machine reach the finish line?', t, SW(0, 'machine'), 1e9, { size: 42 });
  tag(g, 960, 620, '3 · What does it mean to prove something\n     nobody understands?', t, SW(0, 'nobody'), 1e9, { size: 42, bg: '#fbe2c0' });
  tag(g, 960, 860, 'start with the primes', t, SW(0, 'napkin') - 0.5, 1e9, { size: 36 });
});

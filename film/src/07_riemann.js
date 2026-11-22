// ================= Riemann kit: real number theory computed on screen =================
// ZEROS: imaginary parts of the first nontrivial zeta zeros (mpmath.zetazero), injected by build.mjs as const ZEROS.
// ---------- primes ----------
const PRIMES = (() => { const N = 20000, s = new Uint8Array(N + 1), p = []; for (let i = 2; i <= N; i++) if (!s[i]) { p.push(i); for (let j = i * i; j <= N; j += i) s[j] = 1; } return p; })();
const isPrime = n => n >= 2 && PRIMES.indexOf(n) >= 0;
const piCount = x => { let c = 0; for (const p of PRIMES) { if (p > x) break; c++; } return c; };
function Li(x) { // offset log integral from 2, trapezoid on a log grid (accurate enough for drawing)
  if (x <= 2) return 0; let s = 0; const n = 400, a = Math.log(2), b = Math.log(x);
  for (let i = 0; i < n; i++) { const u0 = a + (b - a) * i / n, u1 = a + (b - a) * (i + 1) / n; s += 0.5 * (Math.exp(u0) / u0 + Math.exp(u1) / u1) * (u1 - u0); }
  return s;
}
// Chebyshev psi(x) = sum of log p over prime powers <= x (the exact staircase the explicit formula rebuilds)
function psiTrue(x) { let s = 0; for (const p of PRIMES) { if (p > x) break; let q = p; while (q <= x) { s += Math.log(p); q *= p; } } return s; }
// explicit formula with the first N zeros (N may be fractional: the last zero fades in)
function psiZeros(x, N) {
  let s = x - Math.log(TAU) - 0.5 * Math.log(1 - 1 / (x * x));
  const lx = Math.log(x), sq = Math.sqrt(x), n = Math.min(Math.ceil(N), ZEROS.length);
  for (let k = 0; k < n; k++) {
    const gm = ZEROS[k], w = Math.min(1, N - k); if (w <= 0) break;
    // 2 Re( x^rho / rho ),  rho = 1/2 + i gm
    const c = Math.cos(gm * lx), si = Math.sin(gm * lx), d = 0.25 + gm * gm;
    s -= w * 2 * sq * (0.5 * c + gm * si) / d;
  }
  return s;
}
// ---------- complex zeta via Euler-Maclaurin (valid for all s != 1, good for |t| < ~80) ----------
const CX = { mul: (a, b) => [a[0] * b[0] - a[1] * b[1], a[0] * b[1] + a[1] * b[0]], div: (a, b) => { const d = b[0] * b[0] + b[1] * b[1]; return [(a[0] * b[0] + a[1] * b[1]) / d, (a[1] * b[0] - a[0] * b[1]) / d]; } };
function npow(n, s) { const l = Math.log(n), m = Math.exp(-s[0] * l), a = -s[1] * l; return [m * Math.cos(a), m * Math.sin(a)]; } // n^(-s)
const B2K = [1 / 6, -1 / 30, 1 / 42, -1 / 30, 5 / 66, -691 / 2730, 7 / 6, -3617 / 510];
function zeta(s, N = 24) {
  let re = 0, im = 0;
  for (let n = 1; n < N; n++) { const p = npow(n, s); re += p[0]; im += p[1]; }
  const Ns = npow(N, s);                                  // N^-s
  const t1 = CX.div(CX.mul(Ns, [N, 0]), [s[0] - 1, s[1]]);   // N^(1-s)/(s-1)
  re += t1[0] + Ns[0] / 2; im += t1[1] + Ns[1] / 2;
  let poch = s.slice(), fact = 2, Np = CX.div(Ns, [N, 0]);  // s(s+1)...(s+2k-2) / (2k)! * N^(-s-2k+1)
  for (let k = 1; k <= B2K.length; k++) {
    const term = CX.mul(poch, Np); re += B2K[k - 1] / fact * term[0]; im += B2K[k - 1] / fact * term[1];
    poch = CX.mul(CX.mul(poch, [s[0] + 2 * k - 1, s[1]]), [s[0] + 2 * k, s[1]]); fact *= (2 * k + 1) * (2 * k + 2); Np = CX.div(Np, [N * N, 0]);
  }
  return [re, im];
}
// painted landscape of log|zeta| over a rectangle of the complex plane -> canvas (computed once per shot)
function zetaLandscape(s0, s1, t0, t1, w, h, o = {}) {
  const c = mk(w, h), g = c.getContext('2d'), im = g.createImageData(w, h);
  const lo = o.lo ?? -2.2, hi = o.hi ?? 2.2;
  const ramp = [[0, '#06101f'], [0.18, '#173a66'], [0.42, '#3f7fb6'], [0.6, '#e9d7b0'], [0.8, '#e0954a'], [1, '#fff1cf']];
  const col = u => { for (let i = 1; i < ramp.length; i++) if (u <= ramp[i][0]) return mixv(ramp[i - 1][1], ramp[i][1], (u - ramp[i - 1][0]) / (ramp[i][0] - ramp[i - 1][0])); return hx(ramp[ramp.length - 1][1]); };
  for (let j = 0; j < h; j++) for (let i = 0; i < w; i++) {
    const s = [lerp(s0, s1, i / (w - 1)), lerp(t1, t0, j / (h - 1))], z = zeta(s), v = Math.log(Math.hypot(z[0], z[1]) + 1e-9);
    let u = clamp((v - lo) / (hi - lo)); const band = 0.06 * Math.sin(v * 9); u = clamp(u + band * 0.5); // contour shimmer = painted strata
    const cc = col(u), k = (j * w + i) * 4; im.data[k] = cc[0]; im.data[k + 1] = cc[1]; im.data[k + 2] = cc[2]; im.data[k + 3] = 255;
  }
  g.putImageData(im, 0, 0); return c;
}
// ---------- drawing helpers ----------
// the complex plane with the critical strip; returns map(sigma, t) -> [x, y]
function stripPlane(g, x, y, w, h, o = {}) {
  const s0 = o.s0 ?? -1, s1 = o.s1 ?? 2, t0 = o.t0 ?? 0, t1 = o.t1 ?? 50, a = o.a ?? 1;
  const map = (s, t) => [x + (s - s0) / (s1 - s0) * w, y + h - (t - t0) / (t1 - t0) * h];
  g.save(); g.globalAlpha = a;
  const [ax] = map(0, 0), [bx] = map(1, 0);
  if (o.strip !== false) { g.fillStyle = css(o.stripCol || '#3f7fb6', o.stripA ?? 0.18); g.fillRect(ax, y, bx - ax, h); }
  ink(g, [[x, map(0, 0)[1]], [x + w, map(0, 0)[1]]], 3, { col: o.ink || PAL.ink, t0: 0.02, t1: 0.05, seed: 3 });
  ink(g, [[ax, y + h], [ax, y]], 3, { col: o.ink || PAL.ink, t0: 0.02, t1: 0.05, seed: 4 });
  ink(g, [[bx, y + h], [bx, y]], 2, { col: o.ink || PAL.ink, a: 0.5, t0: 0.02, t1: 0.05, seed: 5 });
  if (o.line !== false) { const [hx_] = map(0.5, 0); ink(g, [[hx_, y + h], [hx_, y]], o.lineW ?? 4, { col: o.lineCol || PAL.red, a: o.lineA ?? 0.85, t0: 0.02, t1: 0.05, seed: 6 }); }
  g.font = `italic 500 ${o.fs ?? 30}px Georgia, serif`; g.fillStyle = css(o.ink || PAL.ink); g.textAlign = 'center'; g.textBaseline = 'top';
  if (o.labels !== false) { g.fillText('0', ax, map(0, 0)[1] + 10); g.fillText('½', map(0.5, 0)[0], map(0, 0)[1] + 10); g.fillText('1', bx, map(0, 0)[1] + 10); }
  g.restore();
  return map;
}
function zeroLantern(g, x, y, r, a = 1) { g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, y, r * 3.2, '#ffcf7a', 0.55 * a); g.restore(); g.fillStyle = css('#fff6e0', a); g.beginPath(); g.arc(x, y, r, 0, TAU); g.fill(); }
// 10 x 10 sieve of Eratosthenes, 1..100. steps: [[p, tStart], ...]; multiples of p cross out one by one
function sieveGrid(g, x, y, cell, t, steps, o = {}) {
  g.save(); g.font = `500 ${cell * 0.42}px Georgia, serif`; g.textAlign = 'center'; g.textBaseline = 'middle';
  for (let n = 1; n <= 100; n++) {
    const i = (n - 1) % 10, j = Math.floor((n - 1) / 10), cx = x + i * cell + cell / 2, cy = y + j * cell + cell / 2;
    let crossT = null, circT = null;
    for (const [p, ts, dt] of steps) { if (n === p) circT = ts; else if (n % p === 0 && n > p && crossT == null) { const k = n / p - 2; crossT = ts + 0.3 + k * (dt ?? 0.08); } }
    if (n === 1) crossT = steps.length ? steps[0][1] - 0.4 : null;
    const crossed = crossT != null && t > crossT, prime = isPrime(n);
    const fin = o.revealPrimes != null && t > o.revealPrimes && prime;
    const box = rough([[cx - cell * 0.44, cy - cell * 0.44], [cx + cell * 0.44, cy - cell * 0.44], [cx + cell * 0.44, cy + cell * 0.44], [cx - cell * 0.44, cy + cell * 0.44]], 1.2, n, 10);
    fillPts(g, box, css(fin ? '#f6d48a' : crossed ? '#d9cbb0' : '#f6eedc', crossed ? 0.6 : 0.95));
    g.fillStyle = css(crossed ? '#9a8a70' : PAL.ink); g.fillText(n, cx, cy + 2);
    if (crossed) { const u = E.out3(seg(t, crossT, crossT + 0.2)); ink(g, [[cx - cell * 0.3, cy + cell * 0.3], [lerp(cx - cell * 0.3, cx + cell * 0.3, u), lerp(cy + cell * 0.3, cy - cell * 0.3, u)]], cell * 0.07, { col: PAL.red, a: 0.75, t0: 0.1, t1: 0.3, seed: n }); }
    if (circT != null && t > circT) brushCircle(g, cx, cy, cell * 0.36, cell * 0.36, t, circT, PAL.blue, cell * 0.06);
    if (o.hl && o.hl(n, t)) brushCircle(g, cx, cy, cell * 0.4, cell * 0.4, t, -1, PAL.gold, cell * 0.08);
  }
  g.restore();
}
// a horizontal progress meter (fraction of zeros proved on the line) with milestones
function fracMeter(g, x, y, w, val, marks, t, o = {}) {
  const h = o.h ?? 46, box = rough([[x, y - h / 2], [x + w, y - h / 2], [x + w, y + h / 2], [x, y + h / 2]], 2, 9, 12);
  fillPts(g, box, css('#efe4cc', 0.95));
  const fw = w * clamp(val); if (fw > 2) fillPts(g, rough([[x, y - h / 2 + 4], [x + fw, y - h / 2 + 4], [x + fw, y + h / 2 - 4], [x, y + h / 2 - 4]], 1.5, 3, 10), css(o.col || PAL.teal, 0.9));
  inkLoop(g, box, 3, { col: PAL.ink, seed: 2 });
  g.font = `600 ${o.fs ?? 28}px Georgia, serif`; g.textAlign = 'center'; g.textBaseline = 'top';
  for (const [v, lab, tIn, up, dx = 0] of marks) { if (t < tIn) continue; const a = seg(t, tIn, tIn + 0.4), mx = x + w * v; g.globalAlpha = a; ink(g, [[mx, y - h / 2 - 12], [mx, y + h / 2 + 12]], 3, { col: PAL.ink, t0: 0, t1: 0 }); g.fillStyle = css(PAL.ink); String(lab).split(String.fromCharCode(10)).forEach((L, i, A) => g.fillText(L, mx + dx, up ? y - h / 2 - 22 - (A.length - i) * 32 : y + h / 2 + 18 + i * 32)); g.globalAlpha = 1; }
  g.font = `600 30px Georgia, serif`; g.fillStyle = css(PAL.ink); g.textAlign = 'left'; g.fillText('0', x - 30, y - 14); g.textAlign = 'right'; g.fillText('100%', x + w + 96, y - 14);
}
// a chain of theta-lift rings (schematic), n links, lit progressively
function ringChain(g, x, y, w, n, t, tIn, o = {}) {
  const r = Math.min(46, w / n * 0.62), step = w / n;
  for (let i = 0; i < n; i++) {
    const tk = tIn + i * (o.dt ?? 0.12); if (t < tk) break; const k = E.out3(seg(t, tk, tk + 0.3)), cx = x + step * (i + 0.5), cy = y + Math.sin(i * 0.9 + t * 1.5) * 6;
    g.save(); g.globalAlpha = k; g.translate(cx, cy); g.rotate(i % 2 ? 0.5 : -0.5);
    const ring = []; for (let q = 0; q <= 36; q++) { const a = q / 36 * TAU; ring.push([Math.cos(a) * r, Math.sin(a) * r * 0.55]); }
    ink(g, ring, Math.max(2, r * 0.18), { col: o.col || PAL.gold, t0: 0.05, t1: 0.05, seed: i });
    g.restore();
  }
}
// a locked box with a label on the lid
function lockBox(g, x, y, s, label, t, tIn, o = {}) {
  if (t < tIn) return; const k = E.out3(seg(t, tIn, tIn + 0.5)); g.save(); g.globalAlpha = k; g.translate(x, y); g.scale(lerp(0.85, 1, k), lerp(0.85, 1, k));
  const body = rough([[-s, -s * 0.45], [s, -s * 0.45], [s, s * 0.6], [-s, s * 0.6]], 3, 4, 12), lid = rough([[-s * 1.05, -s * 0.7], [s * 1.05, -s * 0.7], [s * 1.05, -s * 0.42], [-s * 1.05, -s * 0.42]], 2, 5, 12);
  fillPts(g, offsetPts(body, 14, 16), css('#000', 0.3));
  cels(g, body, { base: '#6b4428', sh: '#43291a', sw: 10, rim: '#a8784e', rw: 4, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 });
  cels(g, lid, { base: '#83583a', sh: '#5b3820', sw: 6, rim: '#c09060', rw: 3, L: LIGHT, line: PAL.ink, lw: 3, seed: 4 });
  const lk = rough([[-s * 0.16, -s * 0.2], [s * 0.16, -s * 0.2], [s * 0.16, s * 0.18], [-s * 0.16, s * 0.18]], 1, 6, 8);
  cels(g, lk, { base: '#d79a2b', sh: '#9a6a18', sw: 4, rim: '#ffe0a0', rw: 2, L: LIGHT, line: PAL.ink, lw: 2.4, seed: 7 });
  const sh = []; for (let q = 0; q <= 16; q++) { const a = Math.PI + q / 16 * Math.PI; sh.push([Math.cos(a) * s * 0.12, -s * 0.2 + Math.sin(a) * s * 0.16]); } ink(g, sh, 6, { col: '#5a4a3a', t0: 0, t1: 0 });
  g.font = `700 ${s * 0.2}px Georgia, serif`; g.fillStyle = css('#fff1cf'); g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(label, 0, -s * 0.56);
  g.restore();
}
// a bar histogram + optional theory curve, drawn into an axes map
function histo(g, map, bins, x0, dx, u, col) {
  bins.forEach((v, i) => { if (i / bins.length > u) return; const [a, b] = map(x0 + i * dx, 0), [c, d] = map(x0 + (i + 1) * dx, v); fillPts(g, rough([[a + 2, b], [c - 2, b], [c - 2, d], [a + 2, d]], 1, i, 8), css(col || PAL.blue, 0.75)); });
}
// normalised nearest-neighbour spacings of the zeros (unfolded with the Riemann-von Mangoldt density)
function zeroSpacings() { const out = []; for (let k = 0; k + 1 < ZEROS.length; k++) { const g0 = ZEROS[k]; out.push((ZEROS[k + 1] - g0) * Math.log(g0 / TAU) / TAU); } return out; }
const wigner = s => 32 / (Math.PI * Math.PI) * s * s * Math.exp(-4 * s * s / Math.PI);
// points on y^2 = x^3 + x + 1 over F_p (including the point at infinity)
function ecCount(p) { let c = 1; const sq = new Int32Array(p); for (let y = 0; y < p; y++) sq[(y * y) % p]++; for (let x = 0; x < p; x++) c += sq[((x * x % p) * x + x + 1) % p]; return c; }
function ecPoints(p) { const pts = []; for (let x = 0; x < p; x++) for (let y = 0; y < p; y++) if ((y * y - (x * x * x + x + 1)) % p === 0) pts.push([x, y]); return pts; }
// a clock face with n hours (finite field picture)
function clockFace(g, x, y, R, n, hand, o = {}) {
  const rim = []; for (let i = 0; i < 48; i++) { const a = i / 48 * TAU; rim.push([x + Math.cos(a) * R, y + Math.sin(a) * R]); }
  fillPts(g, offsetPts(rim, 10, 12), css('#000', 0.25));
  cels(g, rim, { base: '#f6eedc', sh: PAL.cardSh, sw: 8, rim: '#fff', rw: 4, L: LIGHT, line: PAL.ink, lw: 3.4, seed: 2 });
  g.font = `600 ${R * 0.2}px Georgia, serif`; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillStyle = css(PAL.ink);
  for (let k = 0; k < n; k++) { const a = -Math.PI / 2 + k / n * TAU; g.fillText(k, x + Math.cos(a) * R * 0.78, y + Math.sin(a) * R * 0.78); }
  const a = -Math.PI / 2 + hand / n * TAU; arrow(g, x, y, x + Math.cos(a) * R * 0.58, y + Math.sin(a) * R * 0.58, 7, o.col || PAL.red);
}
// Lean-flavoured code lines (illustrative only, labelled as such on screen)
const LEAN = [
  'theorem riemann_hypothesis : RiemannHypothesis := by', '  intro s hs htriv hne', '  have h1 := zero_free_right s hs', '  obtain ⟨m, hm⟩ := theta_chain_length s',
  '  refine le_antisymm ?_ ?_', '  · exact stepanov_bound hm', '  · simpa using functional_eq s', '  calc s.re = 1 / 2 := by linarith', 'lemma theta_lift_mult (k : ℕ) : ...',
  '  induction k with', '  | zero => simp', '  | succ k ih => rw [lift_succ]; exact ih.trans', '  norm_num [Complex.ext_iff]', '  positivity',
];

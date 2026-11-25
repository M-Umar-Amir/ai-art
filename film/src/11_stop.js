// ================= 3. The first second =================
kinShot('stop3', SB(3) - 0.2, [["NOW LET'S STOP IT.", '#ff5a3c']], { bg: '#08080b', size: 140 });
board('fast3', SW(3, 'fast') - 0.3, 'warm', 301, (g, t, S) => {
  title(g, 960, 140, 'the fast version: the solid Earth stops in one second', t, S._sh.t0, 1e9, 46, PAL.ink, { shadow: false, upright: true, weight: 600 });
  tag(g, 520, 330, 'STOPS', t, SW(3, 'rock') - 0.2, 1e9, { size: 60, bg: '#e8c8b8' });
  ['the rock', 'the roads', 'buildings bolted to the rock'].forEach((s, i) => tag(g, 520, 470 + i * 120, s, t, SW(3, 'rock') + i * 0.5, 1e9, { size: 40 }));
  tag(g, 1400, 330, 'KEEPS MOVING', t, SW(3, 'everything') - 0.1, 1e9, { size: 60, bg: '#c8e0f0' });
  ['the air', 'the oceans', 'the cars', 'the people'].forEach((s, i) => tag(g, 1400, 470 + i * 120, s, t, SW(3, 'air') + i * 0.45, 1e9, { size: 40 }));
  stamp(g, 960, 960, 'INERTIA', t, SW(3, 'inertia'), 1e9, { size: 64 });
}, { punch: false });
flat('energy3', SW(3, 'motion') - 0.3, '#0b0e16', (g, t, S) => {
  writeText(g, 'E = ½ I ω²', 960, 300, 120, t, S._sh.t0 + 0.2, 0.8, { upright: true, col: '#ffffff', align: 'center' });
  tag(g, 960, 520, 'mass ≈ 6 × 10²⁴ kg · one turn per (sidereal) day', t, SW(3, 'weighs'), 1e9, { size: 38 });
  const tJ = SW(3, 'energy'); if (t > tJ) { const v = rollNum(t, tJ, SW(3, 'joules'), 0, 2.14, v => v.toFixed(2)); tag(g, 960, 760, `E ≈ ${v} × 10²⁹ joules`, t, tJ, 1e9, { size: 80, bg: '#fbe2c0' }); }
  tag(g, 960, 960, 'our computation · I = 8.03 × 10³⁷ kg m², ω = 7.29 × 10⁻⁵ per second', t, SW(3, 'joules'), 1e9, { size: 28 });
}, { punch: false });
board('compare3', SW(3, 'comparisons') - 0.3, 'blue', 311, (g, t, S) => {
  title(g, 960, 140, 'energy, in joules (log scale)', t, S._sh.t0, 1e9, 52, PAL.ink, { shadow: false });
  hbars(g, 120, 300, 1660, [['Hiroshima bomb', 6.3e13, '#9a8a7a', '6 × 10¹³'], ['largest H-bomb ever', 2.1e17, '#9a8a7a', '2 × 10¹⁷'], ['the dinosaur asteroid', 4e23, '#d79a2b', '≈ 4 × 10²³', SW(3, 'asteroid') - 0.3], ['the Earth’s spin', 2.14e29, PAL.red, '2.1 × 10²⁹', SW(3, 'asteroid') + 0.6]], t, S._sh.t0 + 0.3, { log: [10, 33], labelW: 560, rowH: 150 });
  tag(g, 960, 930, '≈ half a million dinosaur asteroids', t, SW(3, 'half'), SW(3, 'sun') - 0.2, { size: 44, bg: '#fbe2c0' });
  tag(g, 960, 930, '= the ENTIRE Sun’s output for about 9 minutes', t, SW(3, 'sun'), 1e9, { size: 44, bg: '#fbe2c0' });
}, { punch: false });
clipShot('bolted3', SW(3, 'somewhere') - 0.3, 'nightlights', { draw: (g, t, S) => kin(g, [['IT GOES INTO EVERYTHING', '#ffffff'], ["THAT WASN'T BOLTED DOWN", '#ffcf5a']], t, SW(3, 'everything', 2) - 0.2, { size: 100, stagger: 0.5 }) });
function room(g, x, y, w, ang, t, o = {}) { // a room whose floor tilts by ang (radians): a chair and a box slide if ang is large
  g.save(); g.translate(x, y); g.rotate(-ang);
  cels(g, rough([[-w / 2, -w * 0.5], [w / 2, -w * 0.5], [w / 2, 0], [-w / 2, 0]], 2, 3, 12), { base: '#efe3c8', sh: '#d6c49e', sw: 6, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 });
  cels(g, rough([[-w / 2, 0], [w / 2, 0], [w / 2, 26], [-w / 2, 26]], 1.5, 4, 10), { base: '#8a5a3a', line: PAL.ink, lw: 3, seed: 4 });
  const slide = ang > 0.3 ? (o.slide ?? 0) : 0, bx = -w * 0.25 + slide * w * 0.45;
  cels(g, rough([[bx - 60, -110], [bx + 60, -110], [bx + 60, 0], [bx - 60, 0]], 1.5, 5, 8), { base: '#c9883a', sh: '#9a6020', sw: 5, L: LIGHT, line: PAL.ink, lw: 3, seed: 5 });
  g.restore();
}
board('ladder3', SW(3, 'sudden') - 0.3, 'sage', 321, (g, t, S) => {
  title(g, 960, 130, 'how sudden is the stop?', t, S._sh.t0, 1e9, 60, PAL.ink, { shadow: false });
  tag(g, 520, 330, 'stop in 1 second', t, SW(3, 'second', 3) - 0.2, 1e9, { size: 46 }); tag(g, 1380, 330, '47 g, sideways', t, SW(3, 'fortyseven'), 1e9, { size: 64, bg: '#f0b8a8' });
  tag(g, 520, 560, 'stop over 1 minute', t, SW(3, 'minute') - 0.2, 1e9, { size: 46 }); tag(g, 1380, 560, 'like the floor tilting ≈ 38°', t, SW(3, 'tilted'), 1e9, { size: 46, bg: '#fbe2c0' });
  tag(g, 520, 790, 'stop over 1 hour', t, SW(3, 'hour') - 0.2, 1e9, { size: 46 }); tag(g, 1380, 790, 'a tilt under 1°', t, SW(3, 'degree') - 0.1, 1e9, { size: 46 });
  tag(g, 960, 970, 'a = 465 m/s ÷ stopping time', t, S._sh.t0 + 0.5, 1e9, { size: 30 });
}, { punch: false });
pshot('fighter3', SW(3, 'fighter') - 0.3, 'fighter', { tags: [[960, 860, 'fighter pilots in g-suits: about 9 g, for a few seconds', SW(3, 'fighter'), { size: 42, bg: '#fbe2c0' }]] });
flat('tilt3', SW(3, 'minute') + 0.2, '#e9dcc0', (g, t, S) => {
  const a = t < SW(3, 'hour') - 0.2 ? 38 * E.out3(seg(t, S._sh.t0 + 0.2, S._sh.t0 + 1.2)) : 0.75, sl = E.in2(seg(t, S._sh.t0 + 1.0, S._sh.t0 + 2.5));
  room(g, 960, 700, 900, a * Math.PI / 180, t, { slide: sl });
  tag(g, 960, 140, a > 1 ? 'stop over a minute: the floor seems to tilt ≈ 38°' : 'stop over an hour: < 1° · you might not notice', t, S._sh.t0, 1e9, { size: 42, bg: a > 1 ? '#fbe2c0' : '#d9f0ff' });
}, { punch: false });
board('fly3', SW(3, 'fly') - 0.3, 'warm', 331, (g, t, S) => {
  title(g, 960, 140, 'metres per second (log scale)', t, S._sh.t0, 1e9, 50, PAL.ink, { shadow: false });
  hbars(g, 120, 330, 1660, [['speed of sound', 343, '#7fa7c9', '343'], ['pistol bullet', 360, '#9a8a7a', '≈ 360'], ['you, at the equator', 465, PAL.red, '465'], ['orbit', 7900, '#3f7a4a', '≈ 7,900', SW(3, 'orbit')]], t, S._sh.t0 + 0.3, { log: [2, 4.4], labelW: 520, rowH: 140 });
  tag(g, 960, 930, 'orbit takes 17 × more speed', t, SW(3, 'seventeen'), 1e9, { size: 44, bg: '#fbe2c0' });
}, { punch: false });
docOr('clipright3', SW(3, 'viral') - 0.3, 'ladbible', { path: [[0, '1037 miles per hour']], hl: [['1037 miles per hour', SW(3, 'starting')]], src: 'LADbible, 5 Oct 2026',
  tags: [[960, 880, '1,037 mph: right, if you measure against the 24-hour day', SW(3, 'twentyfourhour'), { size: 38, bg: '#fbe2c0' }], [960, 980, '(against the stars: 1,040)', SW(3, 'stars'), { size: 32 }]] }, 'from spinning 1037 miles per hour to zero', 'LADbible, 5 Oct 2026');
board('detail3', SW(3, 'detail') - 0.3, 'blue', 341, (g, t, S) => {
  const panel = (x, quick, tIn) => {
    if (t < tIn) return; const M = axes(g, x, 800, 620, 440, { x0: 0, x1: 1, y0: 0, y1: 1.1, xl: 'time', yl: 'speed', fs: 28 }), u = seg(t, tIn, tIn + 1.4);
    plot(g, M, s => 1 - s, 0, 1, u, { col: '#6a5a4a', w: 6 });
    plot(g, M, s => quick ? 1 : 1 - s + 0.02, 0, 1, u, { col: PAL.blue, w: 5 });
    if (quick && u > 0.3) { g.save(); g.globalAlpha = 0.25; g.fillStyle = css(PAL.red); g.beginPath(); const p0 = M(0, 1), p1 = M(u, 1), p2 = M(u, 1 - u); g.moveTo(p0[0], p0[1]); g.lineTo(p1[0], p1[1]); g.lineTo(p2[0], p2[1]); g.fill(); g.restore(); }
    tag(g, x + 330, 280, quick ? 'quick stop (minutes, hours)' : 'slow stop (years)', t, tIn, 1e9, { size: 36, bg: quick ? '#fbe2c0' : '#d9f0ff' });
    tag(g, x + 330, 930, quick ? 'ground slows, air doesn’t: the gap is the wind' : 'friction drags the air along: no wind', t, tIn + 1, 1e9, { size: 28 });
  };
  panel(160, true, SW(3, 'quick') - 0.4); panel(1080, false, SW(3, 'stretch') - 0.2);
  tag(g, 960, 110, 'grey: the ground · blue: the air', t, S._sh.t0 + 0.3, 1e9, { size: 30 });
}, { punch: false });
docOr('cracks3', SW(3, 'cracks') - 0.6, 'ladbible', { path: [[0, 'Almost immediately, cracks would begin to form on the pavement around you']], hl: [['Almost immediately, cracks would begin to form on the pavement around you', SW(3, 'cracks') - 0.3]], src: 'LADbible, 5 Oct 2026',
  draw: (g, t, S) => { stamp(g, 1480, 160, 'ONLY IF THE STOP IS FAST', t, SW(3, 'pavement'), 1e9, { size: 44 }); kin(g, [['SOMETHING ELSE ENTIRELY', '#ffcf5a']], t, SW(3, 'entirely') - 0.3, { size: 90, y: 980 }); } },
  'Almost immediately, cracks would begin to form on the pavement around you', 'LADbible, 5 Oct 2026');

// ================= 4. The wind =================
glShot('atm4', SB(4) - 0.2, T => glGlobe(T, { air: true, spin: 0.55, stop: SB(4) + 0.9, d0: 3.0, d1: 2.6, camY: 0.25 }), { over: (g, t, S) => {
  tag(g, 520, 130, 'the ground has stopped', t, SB(4) + 0.4, 1e9, { size: 42 });
  tag(g, 1400, 130, 'the atmosphere hasn’t', t, SW(4, 'atmosphere'), 1e9, { size: 42, bg: '#d9f0ff' });
  credit(g, '3D: computed · air shell exaggerated, spin sped up');
} });
clipShot('wind4', SW(4, 'air') - 0.3, 'aerosols', { z: 1.18, cy: 0.45, draw: (g, t, S) => {
  tag(g, 960, 150, 'all the air above the equator: still moving east at ~1,000 mph', t, SW(4, 'air'), 1e9, { size: 36 });
  kin(g, [['TO YOU: A WIND', '#ffffff']], t, SW(4, 'wind') - 0.1, { size: 110, y: 880 });
} });
board('records4', SW(4, 'strongest') - 0.3, 'warm', 401, (g, t, S) => {
  title(g, 960, 130, 'wind speed (mph)', t, S._sh.t0, 1e9, 56, PAL.ink, { shadow: false });
  hbars(g, 120, 300, 1660, [['record gust · Barrow Island, 1996', 253, '#7fa7c9', '253', SW(4, 'strongest')], ['tornado, by radar · Oklahoma', 302, '#7f8fa9', '≈ 300', SW(4, 'tornado')], ['speed of sound', 767, '#9a8a7a', '767', SW(4, 'four') - 0.4], ['the stop, at the equator', 1040, PAL.red, '1,040', SW(4, 'four')]], t, S._sh.t0, { max: 1100, labelW: 640, rowH: 150 });
}, { punch: false });
pshot('tornado4', SW(4, 'funnel') - 0.6, 'tornado', { tags: [[960, 880, 'not a funnel …', SW(4, 'funnel'), { size: 52 }]], draw: (g, t, S) => kin(g, [['… THE WHOLE SKY', '#ffcf5a']], t, SW(4, 'sky') - 0.2, { size: 120, y: 300 }) });
pshot('boom4', SW(4, 'tropics') - 0.3, 'sonicboom', { tags: [[960, 150, 'in the tropics: faster than sound', SW(4, 'tropics'), { size: 42 }], [960, 880, 'a shock wave · a sonic boom that doesn’t stop', SW(4, 'shock'), { size: 42, bg: '#fbe2c0' }]] });
clipShot('wall4', SW(4, 'wall') - 0.3, 'erin', { draw: (g, t, S) => kin(g, [['A WALL OF AIR', '#ffffff']], t, SW(4, 'wall') - 0.1, { size: 130, y: 500 }) });
board('heat4', SW(4, 'heat') - 0.3, 'sage', 411, (g, t, S) => {
  writeText(g, 'ΔT = v² ÷ (2 c_p)', 960, 260, 92, t, S._sh.t0 + 0.2, 0.8, { upright: true, align: 'center' });
  writeText(g, '= 465² ÷ (2 × 1,005) ≈ 108 °C', 960, 440, 76, t, SW(4, 'meters') - 0.3, 0.9, { upright: true, align: 'center', col: PAL.red });
  tag(g, 960, 640, 'if all the motion turned into heat in place (an upper bound)', t, SW(4, 'friction'), 1e9, { size: 34 });
  stamp(g, 960, 850, 'SCORCHING · SUPERSONIC', t, SW(4, 'scorching'), 1e9, { size: 58 });
}, { punch: false });
mapShot('latwind4', SW(4, 'weakens') - 0.3, 'map_today', { z0: 1, z1: 1.04, draw0: (g, t, S) => {
  for (let la = -85; la <= 85; la += 5) { const y0 = mapXY(la + 2.5, 0)[1], y1 = mapXY(la - 2.5, 0)[1], v = Math.cos(la * Math.PI / 180); g.fillStyle = `rgba(${Math.round(255 * v)},${Math.round(90 + 60 * (1 - v))},${Math.round(60 + 160 * (1 - v))},${0.18 + 0.22 * v})`; g.fillRect(0, y0, 1920, y1 - y0); }
}, draw: (g, t, S, P) => {
  const lab = (la, lo, txt, tIn, dy = -70) => { const p = P(la, lo); pinDot(g, p[0], p[1], t, tIn); tag(g, p[0], p[1] + dy, txt, t, tIn + 0.1, 1e9, { size: 32, bg: '#fbe2c0' }); };
  lab(0, -30, 'equator: 1,040 mph', S._sh.t0 + 0.3); lab(40.71, -74, 'New York: 790', SW(4, 'york')); lab(59.9, 10.75, 'Oslo: 520', SW(4, 'oslo')); lab(61.2, -149.9, 'Anchorage: 500', SW(4, 'anchorage')); lab(80, 20, '80° N: 180', SW(4, 'eighty'), 60);
} });
flat('safe4', SW(4, 'safe') - 0.3, '#1a1410', (g, t, S) => { coffeeCup(g, 960, 650, 180, t); kin(g, [["WHERE'S THE SAFE SEAT?", '#ffffff']], t, S._sh.t0 + 0.1, { size: 100, y: 180 }); }, { punch: false });
board('poles4', SW(4, 'poles', 2) - 0.3, 'blue', 421, (g, t, S) => {
  const cx = 640, cy = 560, R = 380; tag(g, cx, 110, 'looking down on the North Pole', t, S._sh.t0, 1e9, { size: 34 });
  for (let k = 1; k <= 4; k++) { const r = R * k / 4, c = []; for (let i = 0; i <= 80; i++) { const a = i / 80 * TAU; c.push([cx + Math.cos(a) * r, cy + Math.sin(a) * r]); } ink(g, c, 2.5, { col: PAL.ink, a: 0.45, t0: 0, t1: 0, seed: k }); }
  for (let k = 1; k <= 4; k++) for (let j = 0; j < 8; j++) { const r = R * k / 4, a = j / 8 * TAU + k * 0.3, L = 26 * k + 6, x = cx + Math.cos(a) * r, y = cy + Math.sin(a) * r; arrow(g, x, y, x - Math.sin(a) * L, y + Math.cos(a) * L, 3 + k, k === 4 ? PAL.red : PAL.blue, seg(t, S._sh.t0 + 0.2 + k * 0.15, S._sh.t0 + 0.5 + k * 0.15)); }
  pinDot(g, cx, cy, t, SW(4, 'exact') - 0.1, '#ffd25a');
  tag(g, 1440, 360, 'at the pole: just turning on the spot', t, SW(4, 'spot') - 0.3, 1e9, { size: 36 });
  tag(g, 1440, 600, 'within ~100 km of a pole:\nunder 20 mph, a stiff breeze', t, SW(4, 'kilometers') - 0.2, 1e9, { size: 36, bg: '#fbe2c0' });
}, { punch: false });
board('np4', SW(4, 'north', 2) - 0.3, 'warm', 431, (g, t, S) => {
  tag(g, 520, 330, 'North Pole:\ndrifting sea ice over a 4-km-deep ocean', t, SW(4, 'north', 2), 1e9, { size: 38 }); stamp(g, 520, 560, 'PROBLEMS AHEAD', t, SW(4, 'problems'), 1e9, { size: 40 });
  print(g, 'southpole', 1350, 470, 440, t, SW(4, 'south') - 0.1, { caption: 'the South Pole: ice 2.8 km high', cs: 30 });
}, { punch: false });
pshot('station4', SW(4, 'station') - 0.6, 'southpole', { from: [0.5, 0.5, 1.0], to: [0.45, 0.5, 1.15], tags: [[960, 150, 'Amundsen–Scott South Pole Station · 2,835 m', SW(4, 'station') - 0.4, { size: 40 }], [960, 880, 'the safest seat on the planet', SW(4, 'safest'), { size: 54, bg: '#fbe2c0' }], [960, 990, '… in the first second, at least', SW(4, 'least') - 0.5, { size: 36 }]] });

// ================= 5. The water =================
mapShot('water5', SB(5) - 0.2, 'map_today', { z0: 1.02, z1: 1.08, draw: (g, t, S, P) => {
  kin(g, [['NOW THE OCEANS', '#ffffff']], t, S._sh.t0 + 0.1, { size: 100, y: 160 });
  const oc = [[0, -150], [10, -130], [-10, -110], [5, -30], [-15, -20], [20, -45], [0, 75], [-20, 80], [10, 160], [-5, 175], [30, -160], [35, -45], [-35, -15], [-35, 90]];
  if (t > SW(5, 'east') - 0.4) oc.forEach(([la, lo], i) => { const p = P(la, lo), L = 120 * Math.cos(la * Math.PI / 180), ph = ((t - SW(5, 'east')) * 0.8 + i * 0.13) % 1; arrow(g, p[0] - L / 2 + ph * 40, p[1], p[0] + L / 2 + ph * 40, p[1], 7, '#ffffff', 0.9); });
  tag(g, 960, 960, 'at the equator: 465 m/s, still going east', t, SW(5, 'meters') - 0.6, 1e9, { size: 40, bg: '#fbe2c0' });
} });
board('climb5', SW(5, 'way') - 0.3, 'sage', 501, (g, t, S) => {
  const base = 900, sc = 60, bar = (x, km, col, lab, tIn) => { if (t < tIn) return; const h = km * sc * E.out3(seg(t, tIn, tIn + 0.9)); cels(g, rough([[x - 110, base - h], [x + 110, base - h], [x + 110, base], [x - 110, base]], 1.5, Math.floor(x), 10), { base: col, sh: mixv(col, '#000', 0.3), sw: 6, L: LIGHT, line: PAL.ink, lw: 2.5, seed: Math.floor(x) }); tag(g, x, base - h - 60, lab, t, tIn + 0.3, 1e9, { size: 34 }); };
  bar(560, 8.85, '#9a8a7a', 'Everest: 8.85 km', SW(5, 'everest') - 0.4); bar(1000, 11.03, '#4f8ad0', 'climb of equator-speed water: 11.0 km', SW(5, 'climb') - 0.2);
  writeText(g, 'h = v² ÷ 2g = 465² ÷ (2 × 9.81) ≈ 11.0 km', 1480, 300, 40, t, SW(5, 'climb'), 0.9, { upright: true, align: 'center' });
  ink(g, [[200, base], [1750, base]], 4, { col: PAL.ink, t0: 0, t1: 0 });
}, { punch: false });
kinShot('hold5', SW(5, 'hold') - 0.3, [['ELEVEN KILOMETRES.', '#7fd0ff']], { bg: '#071226', size: 140, draw: (g, t, S) => kin(g, [['IT COMES BACK AT THE END', '#ffffff']], t, SW(5, 'comes') - 0.1, { size: 70, y: 780 }) });
mapShot('slam5', SW(5, 'tsunami') - 0.3, 'map_today', { z0: 1, z1: 1.05, draw: (g, t, S, P) => {
  kin(g, [['THE WHOLE OCEAN MOVES', '#ffffff']], t, SW(5, 'whole') - 0.1, { size: 90, y: 150 });
  const coast = (pts, col, tIn) => { if (t < tIn) return; const a = seg(t, tIn, tIn + 0.5) * (0.7 + 0.3 * Math.sin(t * 6)); const q = pts.map(([la, lo]) => P(la, lo)); g.save(); g.globalCompositeOperation = 'lighter'; for (const p of q) glowDot(g, p[0], p[1], 60, col, 0.5 * a); g.restore(); ink(g, q, 10, { col, a, t0: 0.05, t1: 0.05 }); };
  coast([[60, -150], [50, -127], [35, -120], [20, -105], [5, -80], [-15, -76], [-35, -72], [-52, -75]], '#ff4a2a', SW(5, 'pacific'));
  coast([[60, -5], [48, -5], [37, -9], [28, -13], [15, -17], [5, -8], [-5, 10], [-20, 13], [-34, 18]], '#ff4a2a', SW(5, 'atlantic'));
  coast([[50, -55], [40, -70], [30, -81], [10, -62], [-10, -36], [-30, -48]], '#7fd0ff', SW(5, 'far') - 0.2); coast([[35, 140], [20, 120], [0, 105], [-20, 150]], '#7fd0ff', SW(5, 'far') - 0.2);
  tag(g, 500, 940, 'red: water slams ashore · blue: the sea pulls away', t, SW(5, 'far'), 1e9, { size: 32 });
} });
pshot('tohoku5', SW(5, 'japan') - 0.3, 'tohoku', { tags: [[960, 150, 'Sendai, Japan · March 2011 (US Navy photo)', SW(5, 'japan'), { size: 36 }], [960, 860, 'up to 40 m · about 18,000 killed or never found', SW(5, 'forty'), { size: 42, bg: '#fbe2c0' }]] });
kinShot('jet5', SW(5, 'here') - 0.3, [['EVERY OCEAN ON EARTH', '#ffffff'], ['FASTER THAN A PASSENGER JET', '#ff6a55']], { bg: '#071226', size: 110, stagger: 0.8 });
flat('layers5', SW(5, 'ground') - 0.3, '#07090f', (g, t, S) => {
  earthCut(g, 640, 560, 400, t, { labels: SW(5, 'crust') - 0.2 });
  tag(g, 1420, 900, 'grab the surface and stop it: the layers below grind on', t, SW(5, 'grind') - 1.2, 1e9, { size: 32, bg: '#fbe2c0' });
}, { punch: false });
kinShot('cant5', SW(5, 'happen') - 0.4, [['IS THIS WHAT WOULD HAPPEN?', '#ffffff']], { bg: '#0b0d14', size: 96, y: 400, draw: (g, t, S) => kin(g, [['NOTHING WE KNOW OF CAN DO IT', '#ffcf5a']], t, SW(5, 'nothing') - 0.1, { size: 96, y: 640 }) });
board('angmom5', SW(5, 'angular') - 0.3, 'blue', 511, (g, t, S) => {
  writeText(g, 'L = I ω ≈ 5.9 × 10³³ kg m² per second', 960, 230, 64, t, S._sh.t0 + 0.2, 0.9, { upright: true, align: 'center' });
  tag(g, 960, 420, 'angular momentum: the momentum of turning', t, SW(5, 'momentum', 2) - 0.2, 1e9, { size: 40 });
  tag(g, 960, 600, 'only an outside push can take it away', t, SW(5, 'outside'), 1e9, { size: 44, bg: '#fbe2c0' });
  stamp(g, 960, 820, 'A 1-SECOND STOP WOULD MELT THE SURFACE', t, SW(5, 'melt'), 1e9, { size: 46 });
}, { punch: false });
board('theia5', SW(5, 'collision') - 0.3, 'warm', 521, (g, t, S) => {
  const k = E.in2(seg(t, S._sh.t0 + 0.2, S._sh.t0 + 2.2)); particle(g, 760, 560, 220, '#4f8ad0', { glow: false }); particle(g, lerp(1700, 1010, k), lerp(240, 430, k), 120, '#b06a3a', { glow: false });
  if (k >= 1) { g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, 960, 470, 260, '#ffb040', 0.8 * (1 - seg(t, S._sh.t0 + 2.2, S._sh.t0 + 3.5))); g.restore(); }
  tag(g, 760, 900, 'the impact thought to have formed the Moon', t, SW(5, 'formed') - 0.3, 1e9, { size: 36 });
  tag(g, 1400, 820, 'changes the spin · doesn’t stop it', t, SW(5, 'spins') - 0.3, 1e9, { size: 38, bg: '#fbe2c0' }); tag(g, 960, 100, 'artist’s diagram, not to scale', t, S._sh.t0 + 0.3, 1e9, { size: 26 });
}, { punch: false });
kinShot('lesson5', SW(5, 'lesson') - 0.4, [['THE SUDDEN STOP:', '#ffffff'], ['A LESSON IN INERTIA', '#7fd0ff']], { bg: '#0b0d14', size: 110, stagger: 0.5 });
board('slowq5', SW(5, 'interesting') - 0.4, 'sage', 531, (g, t, S) => {
  tag(g, 960, 300, 'what if the spin wound down gently, over years?', t, SW(5, 'gently') - 0.6, 1e9, { size: 48 });
  tag(g, 960, 520, 'nobody falls over · no wind blows', t, SW(5, 'nobody'), 1e9, { size: 42 });
  stamp(g, 960, 780, 'IT HAPPENS TO THE OCEANS', t, SW(5, 'oceans', 2) - 0.2, 1e9, { size: 56 });
});

// ================= 6. The oceans go to the poles =================
kinShot('slow6', SB(6) - 0.2, [['THE SLOW VERSION', '#7fd0ff']], { bg: '#071226', size: 140, draw: (g, t, S) => tag(g, 960, 760, 'over a few years · no killer wind · no wall of water', t, SW(6, 'years') - 0.2, 1e9, { size: 40 }) });
glShot('slowglobe6', SW(6, 'over') - 0.2, T => glGlobe(T, { air: true, spin: 0.5, decel: 9, d0: 3.2, d1: 2.9 }), { over: (g, t, S) => {
  tag(g, 960, 120, 'over a few years, the spin winds down to zero', t, S._sh.t0 + 0.2, 1e9, { size: 40 });
  tag(g, 960, 960, 'friction drags the air and the oceans along', t, SW(6, 'friction') - 0.2, 1e9, { size: 40, bg: '#fbe2c0' }); credit(g, '3D: computed · slowed down for the screen');
} });
board('weigh6', SW(6, 'weigh') - 0.4, 'warm', 601, (g, t, S) => {
  const x = 760, y = 620; cels(g, rough([[x - 260, y - 40], [x + 260, y - 40], [x + 240, y + 120], [x - 240, y + 120]], 2, 3, 12), { base: '#d8dde2', sh: '#9aa3ad', sw: 8, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 });
  cels(g, rough([[x - 150, y - 10], [x + 150, y - 10], [x + 150, y + 80], [x - 150, y + 80]], 1, 4, 10), { base: '#1c2a20', line: PAL.ink, lw: 3, seed: 4 });
  const v = rollNum(t, SW(6, 'percent') - 0.3, SW(6, 'percent') + 0.6, 70.0, 70.24, q => q.toFixed(2)); g.font = '700 64px Consolas, monospace'; g.fillStyle = '#7fe0a0'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(v + ' kg', x, y + 36);
  tag(g, 1440, 380, 'at the equator:\nabout ⅓ % heavier', t, SW(6, 'third'), 1e9, { size: 44, bg: '#fbe2c0' });
  tag(g, 1440, 640, 'the spin had been flinging you\nvery slightly outward', t, SW(6, 'flinging'), 1e9, { size: 36 });
}, { punch: false });
function bulgeWater(g, cx, cy, R, k, t, o = {}) { // rock ellipse (exaggerated) + water shell whose thickness shifts from equator to poles as k: 0 -> 1
  const rockE = 0.12, pts = [], wat = [];
  for (let i = 0; i <= 120; i++) { const th = i / 120 * TAU, phi = Math.asin(Math.sin(th)), rr = R * (1 + rockE * Math.cos(phi) ** 2); pts.push([cx + Math.cos(th) * rr, cy - Math.sin(th) * rr]);
    const sh = 34 + 60 * (1 - k) * Math.cos(phi) ** 2 + 110 * k * (Math.sin(phi) ** 2 - 0.25); wat.push([cx + Math.cos(th) * (rr + Math.max(0, sh)), cy - Math.sin(th) * (rr + Math.max(0, sh))]); }
  cels(g, wat, { base: '#4f8ad0', sh: '#2f5f9f', sw: 10, rim: '#a8d0ff', rw: 5, L: LIGHT, line: PAL.ink, lw: 2.5, seed: 11 });
  cels(g, pts, { base: '#9a6a3a', sh: '#6a4222', sw: 12, L: LIGHT, line: PAL.ink, lw: 3, seed: 12 });
}
board('fling6', SW(6, 'fling') - 0.3, 'blue', 611, (g, t, S) => {
  bulgeWater(g, 760, 540, 300, 0, t);
  if (t > SW(6, 'acts') - 0.3) for (const s of [-1, 1]) for (const dy of [-60, 0, 60]) arrow(g, 760 + s * 380, 540 + dy, 760 + s * 470, 540 + dy, 7, PAL.red, seg(t, SW(6, 'acts') - 0.3, SW(6, 'acts')));
  tag(g, 1500, 360, 'a small outward fling …', t, S._sh.t0 + 0.2, 1e9, { size: 40 });
  tag(g, 1500, 600, '… on the entire ocean:\nit holds the sea up at the equator', t, SW(6, 'holds') - 0.3, 1e9, { size: 38, bg: '#fbe2c0' }); tag(g, 760, 960, 'not to scale', t, S._sh.t0 + 0.3, 1e9, { size: 26 });
}, { punch: false });
board('stiff6', SW(6, 'remember') - 0.3, 'sage', 621, (g, t, S) => {
  const k = E.io2(seg(t, SW(6, 'without') - 0.2, SW(6, 'without') + 1.6)); bulgeWater(g, 700, 540, 300, k, t);
  tag(g, 1460, 250, 'rock bulges 21 km · stiff: it stays', t, SW(6, 'stiff') - 0.4, 1e9, { size: 36 });
  tag(g, 1460, 450, 'water isn’t stiff:\nit goes wherever gravity says is downhill', t, SW(6, 'water', 2) - 0.2, 1e9, { size: 34 });
  stamp(g, 1460, 700, 'THE BULGE BECOMES A HILL', t, SW(6, 'hill') - 0.2, 1e9, { size: 44 }); tag(g, 700, 960, 'exaggerated', t, S._sh.t0 + 0.3, 1e9, { size: 26 });
}, { punch: false });
board('calc6', SW(6, 'calculation') - 0.3, 'warm', 631, (g, t, S) => {
  print(g, 'map_today', 1380, 380, 360, t, S._sh.t0 + 0.1, { caption: 'NOAA ETOPO1 global relief', cs: 28, credit: false, rot: 0.02 });
  tag(g, 1380, 690, rollNum(t, SW(6, 'million') - 1.2, SW(6, 'million'), 0, 2337841) + ' points', t, SW(6, 'million') - 1.2, 1e9, { size: 52, bg: '#fbe2c0' });
  checklist(g, 180, 300, [['every patch of land and sea floor', '✓', SW(6, 'height')], ['switch off the spin', '✓', SW(6, 'switched')], ['keep the rock where it is', '✓', SW(6, 'kept')], ['same water: where does it settle?', '?', SW(6, 'settle') - 0.5]], t, { size: 40, dy: 150 });
}, { punch: false });
glShot('morph6', SW(6, 'answer') - 0.3, T => glMorph(T, { m0: SW(6, 'drops') - 0.3, m1: SW(6, 'polar') + 0.5, lon0: -0.3,
  el: (lt, t) => 0.12 + 0.75 * E.io2(seg(t, SW(6, 'rises') - 0.2, SW(6, 'rises') + 1.6)) * (1 - E.io2(seg(t, SW(6, 'between') - 1.5, SW(6, 'between')))) }), { over: (g, t, S) => {
  tag(g, 500, 120, 'sea level at the equator: −4.2 km', t, SW(6, 'drops'), 1e9, { size: 38, bg: '#d9f0ff' });
  tag(g, 1420, 120, 'at the poles: +6.9 km', t, SW(6, 'rises'), 1e9, { size: 38, bg: '#fbe2c0' });
  kin(g, [['TWO POLAR OCEANS', '#ffffff']], t, SW(6, 'polar') - 0.2, { size: 90, y: 960 });
  credit(g, 'our calculation: NOAA ETOPO1, spin removed, rock fixed, water conserved');
} });
mapShot('maps6', SW(6, 'between') - 0.3, 'map_nospin', { z0: 1, z1: 1.05, draw: (g, t, S, P) => {
  const a = P(10, 40), b = P(10, -100); tag(g, a[0], a[1], 'Africa + Asia + Australia', t, SW(6, 'africa') - 0.2, 1e9, { size: 40, bg: '#fbe2c0' }); tag(g, b[0], b[1], 'the Americas + new Pacific land', t, SW(6, 'americas') - 0.2, 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 960, 120, 'two enormous continents along the equator', t, SW(6, 'enormous') - 0.2, 1e9, { size: 40 });
} });
const OFF = { London: [-260, -40], Paris: [230, 60], Moscow: [0, -90], Toronto: [-260, -60], 'New York': [250, 70], Karachi: [-280, -70], Mumbai: [240, 80] };
const cityPins = (names, tIns) => (g, t, S, P) => names.forEach((n, i) => { const c = NOSPIN_CITIES.find(q => q[0] === n); const p = P(c[1], c[2]); pinDot(g, p[0], p[1], t, tIns[i], c[4] === 'under' ? '#ff5a3c' : '#ffd25a'); const o = OFF[n] || [0, -90];
  tag(g, p[0] + o[0], p[1] + o[1], `${n}: ${c[4] === 'under' ? 'under' : 'above the sea by'} ${c[3] >= 1000 ? (Math.round(c[3] / 100) / 10).toFixed(1) + ' km' : c[3] + ' m'}`, t, tIns[i] + 0.1, 1e9, { size: 34, bg: c[4] === 'under' ? '#f6d0c4' : '#fbe2c0' }); });
mapShot('north6', SW(6, 'north') - 0.3, 'map_nospin', { focus: [52, 18], z0: 2.4, z1: 2.7, k0: 1, draw: (g, t, S, P) => { kin(g, [['ALMOST ALL OF EUROPE: UNDERWATER', '#ffffff']], t, SW(6, 'europe') - 0.2, { size: 70, y: 110 }); cityPins(['London', 'Paris', 'Moscow'], [SW(6, 'london'), SW(6, 'paris'), SW(6, 'moscow')])(g, t, S, P); } });
mapShot('na6', SW(6, 'toronto') - 0.3, 'map_nospin', { focus: [45, -85], z0: 2.2, z1: 2.4, k0: 1, draw: (g, t, S, P) => { cityPins(['Toronto', 'New York'], [SW(6, 'toronto'), SW(6, 'york')])(g, t, S, P); tag(g, 960, 960, 'most of Russia and Canada: gone', t, SW(6, 'russia') - 0.2, 1e9, { size: 40, bg: '#f6d0c4' }); } });
mapShot('beijing6', SW(6, 'beijing') - 0.3, 'map_nospin', { focus: [40, 116], z0: 2.6, z1: 2.8, k0: 1, draw: cityPins(['Beijing'], [SW(6, 'beijing')]) });
mapShot('south6', SW(6, 'south') - 0.3, 'map_nospin', { focus: [20, 72], z0: 2.4, z1: 2.6, k0: 1, draw: (g, t, S, P) => cityPins(['Karachi', 'Mumbai'], [SW(6, 'karachi'), SW(6, 'mumbai')])(g, t, S, P) });
mapShot('sing6', SW(6, 'singapore') - 0.3, 'map_nospin', { focus: [8, 100], z0: 2.2, z1: 2.4, k0: 1, draw: cityPins(['Singapore'], [SW(6, 'singapore')]) });
mapShot('dallas6', SW(6, 'dallas') - 0.3, 'map_nospin', { focus: [32.8, -96.8], z0: 2.4, z1: 2.6, k0: 1, draw: cityPins(['Dallas'], [SW(6, 'dallas')]) });
mapShot('dry6', SW(6, 'amazon') - 0.3, 'map_nospin', { z0: 1.05, z1: 1.1, draw: (g, t, S, P) => {
  [['the Amazon', -4, -62], ['the Sahara', 23, 10], ['India', 21, 78], ['Indonesia', -2, 115]].forEach(([n, la, lo], i) => { const p = P(la, lo), ti = SW(6, 'amazon') + i * 0.45; pinDot(g, p[0], p[1], t, ti, '#ffd25a'); tag(g, p[0], p[1] - 70, n, t, ti, 1e9, { size: 34, bg: '#fbe2c0' }); });
  stamp(g, 960, 960, 'HIGH AND DRY', t, SW(6, 'dry') - 0.2, 1e9, { size: 56 });
} });
pshot('everest6', SW(6, 'strange') - 0.3, 'everest', { tags: [[960, 860, 'the highest mountain above sea level: no longer Everest', SW(6, 'highest'), { size: 40, bg: '#fbe2c0' }]] });
pshot('huas6', SW(6, 'calculation', 2) - 0.3, 'huascaran', { tags: [[960, 150, 'Huascarán, Peruvian Andes · 6,768 m today', SW(6, 'calculation', 2) + 0.4, { size: 40 }]] });
board('peaks6', SW(6, 'stand') - 0.6, 'blue', 641, (g, t, S) => {
  title(g, 960, 130, 'height above the new sea (km) · our calculation', t, S._sh.t0, 1e9, 46, PAL.ink, { shadow: false, upright: true, weight: 600 });
  hbars(g, 120, 330, 1660, [['Huascarán (today 6.77)', 10.67, PAL.red, '10.67'], ['Everest (today 8.85)', 10.59, '#9a8a7a', '10.59', SW(6, 'edging') - 0.2]], t, S._sh.t0 + 0.2, { max: 12, labelW: 620, rowH: 200 });
  tag(g, 960, 800, 'near the equator, the sea falls farther', t, SW(6, 'edging'), 1e9, { size: 38 });
}, { punch: false });
board('esri6', SW(6, 'first') - 0.3, 'warm', 651, (g, t, S) => {
  tag(g, 960, 230, '2010 · geographer Witold Fraczek, Esri', t, SW(6, 'twenty') - 0.2, 1e9, { size: 44 });
  tag(g, 960, 420, 'the same picture: two polar oceans, land around the equator', t, SW(6, 'picture') - 0.2, 1e9, { size: 38, bg: '#fbe2c0' });
  tag(g, 960, 610, 'ours: a few deep channels still link the two oceans', t, SW(6, 'channels') - 0.4, 1e9, { size: 38 });
  tag(g, 960, 800, 'assumptions: rigid rock, water conserved, water’s own gravity ignored (see description)', t, SW(6, 'assumptions') - 0.3, 1e9, { size: 30 });
});
board('eleven6', SW(6, 'tilt') - 0.3, 'sage', 661, (g, t, S) => {
  const M = axes(g, 220, 860, 1480, 640, { x0: -90, x1: 90, y0: -5, y1: 8, xl: 'latitude', yl: 'sea-level change (km)' }), u = seg(t, S._sh.t0 + 0.2, S._sh.t0 + 2);
  const zero = [M(-90, 0), M(90, 0)]; ink(g, zero, 2, { col: PAL.ink, a: 0.4, t0: 0, t1: 0 });
  plot(g, M, la => 6.856 - 11.03 * Math.cos(la * Math.PI / 180) ** 2, -90, 90, u, { col: PAL.blue, w: 7 });
  for (const v of [-90, -45, 0, 45, 90]) { const p = M(v, -5); g.font = '500 26px Georgia'; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.fillText(v + '°', p[0], p[1] + 34); }
  if (t > SW(6, 'props') - 0.2) { const a = M(0, -4.17), b = M(0, 6.86); arrow(g, a[0] + 60, a[1], a[0] + 60, b[1], 7, PAL.red); tag(g, a[0] + 330, (a[1] + b[1]) / 2, '≈ 11 km between equator and poles', t, SW(6, 'props'), 1e9, { size: 38, bg: '#fbe2c0' }); }
  kin(g, [['THE SAME ELEVEN KILOMETRES', '#b8361f']], t, SW(6, 'same', 3) - 0.2, { size: 70, y: 150, stroke: 'rgba(255,255,255,0.9)' });
}, { punch: false });

// ================= 7. A day that lasts a year =================
function starField(g, n, seed, drift = 0) { const r = RNG(seed); for (let i = 0; i < n; i++) { const x = (r() * W + drift * (0.3 + r())) % W, y = r() * H * 0.95, s = r(); g.fillStyle = `rgba(230,236,255,${0.3 + 0.7 * s})`; g.beginPath(); g.arc(x, y, 0.8 + s * 2.2, 0, TAU); g.fill(); } }
flat('look7', SB(7) - 0.2, '#04060c', (g, t, S) => {
  const moving = t < SW(7, 'stop') - 0.1; starField(g, 600, 9, moving ? (t - S._sh.t0) * 60 : (SW(7, 'stop') - 0.1 - S._sh.t0) * 60);
  kin(g, [['NOW LOOK UP', '#ffffff']], t, S._sh.t0 + 0.1, { size: 120, y: 400 });
  tag(g, 960, 760, moving ? 'tonight: the stars wheel across the sky' : 'no spin: they hang in exactly the same place', t, moving ? S._sh.t0 + 0.4 : SW(7, 'stop') - 0.1, 1e9, { size: 40, bg: moving ? PAL.card : '#fbe2c0' });
}, { punch: false });
board('orbit7', SW(7, 'around') - 0.3, 'blue', 701, (g, t, S) => {
  const cx = 760, cy = 540, R = 330, u = seg(t, S._sh.t0 + 0.6, S._sh.t0 + 9), a = -Math.PI / 2 + u * TAU;
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, cx, cy, 150, '#ffd27a', 0.9); g.restore(); particle(g, cx, cy, 60, '#ffc840', { glow: false });
  const orb = []; for (let i = 0; i <= 90; i++) { const q = i / 90 * TAU; orb.push([cx + Math.cos(q) * R, cy + Math.sin(q) * R]); } ink(g, orb, 3, { col: PAL.ink, a: 0.45, t0: 0, t1: 0 });
  const ex = cx + Math.cos(a) * R, ey = cy + Math.sin(a) * R, sunDir = Math.atan2(cy - ey, cx - ex);
  particle(g, ex, ey, 46, '#4f8ad0', { glow: false });
  g.save(); g.beginPath(); g.arc(ex, ey, 46, sunDir + Math.PI / 2, sunDir + 3 * Math.PI / 2); g.closePath(); g.fillStyle = 'rgba(10,15,30,0.62)'; g.fill(); g.restore();
  const day = Math.cos(sunDir - (-Math.PI / 2)) > 0; ink(g, [[ex, ey - 46], [ex, ey - 96]], 6, { col: day ? '#e8a020' : '#30405a', t0: 0.05, t1: 0.05 }); glowDot(g, ex, ey - 100, 14, day ? '#ffd25a' : '#8090b0', 1);
  tag(g, 1480, 300, 'no spin: the same side keeps facing the same way', t, S._sh.t0 + 0.3, 1e9, { size: 32 });
  tag(g, 1480, 520, 'the Sun crosses the sky once a year', t, SW(7, 'once') - 0.2, 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 1480, 740, '6 months of day · 6 months of night', t, SW(7, 'six') - 0.2, 1e9, { size: 36 }); tag(g, 760, 960, 'marker = you · orbit sped up', t, S._sh.t0 + 0.4, 1e9, { size: 26 });
}, { punch: false });
flat('west7', SW(7, 'odd') - 0.3, '#000', (g, t, S) => { skyRise(g, t, S._sh.t0, { dur: 4 }); kin(g, [['THE SUN WOULD RISE IN THE WEST', '#ffffff']], t, SW(7, 'west') - 0.1, { size: 90, y: 200 }); }, { punch: false });
const whyDraw = (g, t, S) => {
  const hy = 720; ink(g, [[160, hy], [1760, hy]], 6, { col: PAL.ink, t0: 0, t1: 0 }); const dome = []; for (let i = 0; i <= 60; i++) { const q = Math.PI * i / 60; dome.push([960 - Math.cos(q) * 700, hy - Math.sin(q) * 520]); } ink(g, dome, 3, { col: PAL.ink, a: 0.4, t0: 0, t1: 0 });
  g.font = '800 64px Georgia'; g.textAlign = 'center'; g.fillStyle = css(PAL.ink); g.fillText('E', 230, hy + 80); g.fillText('W', 1690, hy + 80);
  const fadeSpin = 1 - seg(t, SW(7, 'take') - 0.1, SW(7, 'take') + 0.5);
  if (t > SW(7, 'east') - 0.3) { g.save(); g.globalAlpha = fadeSpin; const p = []; for (let i = 0; i <= 30; i++) { const q = Math.PI * (0.1 + 0.8 * i / 30); p.push([960 - Math.cos(q) * 620, hy - Math.sin(q) * 450]); } ink(g, p, 12, { col: '#d79a2b', t0: 0.05, t1: 0.05 }); arrow(g, p[28][0], p[28][1], p[30][0], p[30][1], 12, '#d79a2b'); g.restore(); tag(g, 960, 170, 'every day: the spin carries the Sun east → west', t, SW(7, 'east') - 0.3, SW(7, 'take') - 0.1, { size: 36 }); }
  if (t > SW(7, 'creeping') - 0.3) { const p = []; for (let i = 0; i <= 20; i++) { const q = Math.PI * (0.55 - 0.1 * i / 20); p.push([960 - Math.cos(q) * 520, hy - Math.sin(q) * 360]); } ink(g, p, 7, { col: PAL.red, t0: 0.05, t1: 0.05 }); arrow(g, p[18][0], p[18][1], p[20][0], p[20][1], 7, PAL.red); tag(g, 960, 470, 'underneath: a slow creep west → east, ≈ 1° a day', t, SW(7, 'creeping'), 1e9, { size: 32, bg: '#fbe2c0' }); }
  tag(g, 960, 960, 'no spin: up in the WEST · highest after ~3 months · down in the EAST', t, SW(7, 'western') - 0.3, 1e9, { size: 34, bg: '#fbe2c0' });
};
board('why7', SW(7, 'why') - 0.3, 'warm', 711, whyDraw, { punch: false });
kinShot('four7', SW(7, 'four') - 0.4, [['ONE DAY = ONE TURN', '#ffffff'], ['+ ABOUT 4 MINUTES', '#ffcf5a']], { bg: '#0b0d14', size: 110, stagger: 0.6 });
board('why7b', SW(7, 'take') - 0.4, 'warm', 711, whyDraw, { punch: false });
board('tide7', SW(7, 'moon') - 0.3, 'blue', 721, (g, t, S) => {
  const cx = 760, cy = 560, ma = -0.4 + (t - S._sh.t0) * 0.35, mx = cx + Math.cos(ma) * 420, my = cy - Math.sin(ma) * 300;
  g.save(); g.translate(cx, cy); g.rotate(-ma); const b = []; for (let i = 0; i < 60; i++) { const q = i / 60 * TAU; b.push([Math.cos(q) * 210, Math.sin(q) * 165]); } cels(g, b, { base: '#6fa8e0', sh: '#3f78b0', sw: 8, L: LIGHT, line: PAL.ink, lw: 2.5, seed: 4 }); g.restore();
  particle(g, cx, cy, 150, '#7a9a5a', { glow: false }); particle(g, mx, my, 44, '#c8c8c0', { glow: false });
  tag(g, 1480, 360, 'the Moon: once around every 27 days', t, SW(7, 'twentyseven') - 0.2, 1e9, { size: 36 });
  tag(g, 1480, 600, 'still pulls up two tidal bulges', t, SW(7, 'tides') - 0.2, 1e9, { size: 36 });
  tag(g, 1480, 830, 'two high tides a MONTH,\nnot two a day', t, SW(7, 'month') - 0.4, 1e9, { size: 40, bg: '#fbe2c0' }); tag(g, 760, 980, 'bulges exaggerated', t, S._sh.t0 + 0.3, 1e9, { size: 26 });
}, { punch: false });
board('daynight7', SW(7, 'sunshine') - 0.3, 'warm', 731, (g, t, S) => {
  const cx = 960, cy = 560, R = 320; g.save(); g.beginPath(); g.arc(cx, cy, R, 0, TAU); g.clip(); const gr = g.createLinearGradient(cx - R, 0, cx + R, 0); gr.addColorStop(0, '#1d2f5a'); gr.addColorStop(0.48, '#2a4070'); gr.addColorStop(0.52, '#e08a3a'); gr.addColorStop(1, '#ffcf7a'); g.fillStyle = gr; g.fillRect(cx - R, cy - R, 2 * R, 2 * R); g.restore();
  inkLoop(g, (() => { const p = []; for (let i = 0; i < 80; i++) { const q = i / 80 * TAU; p.push([cx + Math.cos(q) * R, cy + Math.sin(q) * R]); } return p; })(), 4, { col: PAL.ink, seed: 2 });
  for (let k = 0; k < 5; k++) arrow(g, 1760, 300 + k * 130, 1330, 300 + k * 130, 5, '#e8a020', 0.8);
  tag(g, 1300, 200, 'the day side bakes for months', t, SW(7, 'bake') - 0.3, 1e9, { size: 38, bg: '#fbe2c0' }); tag(g, 560, 200, 'the night side freezes', t, SW(7, 'freeze') - 0.3, 1e9, { size: 38, bg: '#d9f0ff' });
}, { punch: false });
const yangDraw = (g, t, S) => {
  const cx = 820, cy = 560, R = 300; particle(g, cx, cy, R, '#5a7aa0', { glow: false });
  const k = E.out3(seg(t, SW(7, 'deck') - 0.6, SW(7, 'deck') + 0.6)); if (k > 0) { g.save(); g.globalAlpha = 0.92 * k; for (let i = 0; i < 26; i++) { const q = -0.9 + i / 25 * 1.8, rr = R * (0.55 + 0.4 * hash(i, 3)); const x = cx + Math.cos(q) * rr * 0.8 + 40, y = cy + Math.sin(q) * rr; g.fillStyle = '#ffffff'; g.beginPath(); g.ellipse(x, y, 70 + 40 * hash(i, 5), 50, 0, 0, TAU); g.fill(); } g.restore(); }
  for (let j = 0; j < 4; j++) { const y = 340 + j * 140; arrow(g, 1780, y, 1180, y, 5, '#e8a020', 0.8); if (t > SW(7, 'reflect') - 0.2) arrow(g, 1180, y + 30, 1640, y - 60, 4, '#ffffff', seg(t, SW(7, 'reflect') - 0.2, SW(7, 'reflect') + 0.4)); }
  tag(g, 820, 120, 'Yang, Boué, Fabrycky & Abbot · 2014 · slowly spinning planets', t, SW(7, 'twenty') - 0.2, 1e9, { size: 32 });
  tag(g, 1500, 950, 'a thick deck of cloud on the day side', t, SW(7, 'deck') - 0.3, 1e9, { size: 34 });
  stamp(g, 700, 960, 'COOLER THAN YOU’D EXPECT', t, SW(7, 'cooler') - 0.2, 1e9, { size: 44 });
};
board('yang7', SW(7, 'boil') - 0.3, 'sage', 741, yangDraw, { punch: false });
pshot('storm7', SW(7, 'storms') - 0.2, 'cyclone', { tags: [[960, 880, 'endless sunshine drives huge storms over the day side', SW(7, 'storms'), { size: 40, bg: '#fbe2c0' }]] });
board('yang7b', SW(7, 'deck') - 0.7, 'sage', 741, yangDraw, { punch: false });
pshot('venus7', SW(7, 'venus') - 0.3, 'venus', { fit: true, from: [0.5, 0.5, 0.92], to: [0.5, 0.5, 1.0], tags: [[1500, 330, 'one turn: 243 Earth days', SW(7, 'turns') - 0.2, { size: 40 }], [1500, 520, '… and it turns backwards', SW(7, 'backwards') - 0.2, { size: 40, bg: '#fbe2c0' }], [420, 820, 'the Sun already rises in the west', SW(7, 'west', 3) - 0.2, { size: 38 }]] });
board('venbars7', SW(7, 'cloud', 2) - 0.4, 'warm', 751, (g, t, S) => {
  title(g, 960, 150, 'Venus: days to go once around', t, S._sh.t0, 1e9, 54, PAL.ink, { shadow: false });
  hbars(g, 120, 360, 1660, [['the ground', 243, '#9a8a7a', '243 days'], ['the cloud tops', 4, '#e8c060', '≈ 4 days']], t, S._sh.t0 + 0.2, { max: 260, labelW: 460, rowH: 200 });
  stamp(g, 960, 860, '60 × FASTER THAN THE GROUND', t, SW(7, 'sixty') - 0.2, 1e9, { size: 52 });
}, { punch: false });
clipShot('venusart7', SW(7, "planet's") - 0.3, 'venusart', { draw: (g, t, S) => kin(g, [["A PLANET'S AIR", '#ffffff'], ["DOESN'T HAVE TO FOLLOW ITS GROUND", '#ffcf5a']], t, S._sh.t0 + 0.2, { size: 84, stagger: 0.7, y: 540 }) });

// ================= 8. Weather without a twist =================
clipShot('venus8', SB(8) - 0.2, 'aerosols', { z: 1.2, cy: 0.42, speed: 0.6, draw: (g, t, S) => tag(g, 960, 900, 'on Earth, the spin shapes the weather', t, S._sh.t0 + 0.3, 1e9, { size: 42 }) });
flat('merry8', SW(8, 'imagine') - 0.3, '#efe4cc', (g, t, S) => {
  const T0 = S._sh.t0 + 0.6, Om = 0.55, R = 300, v = 180, tt = Math.min(Math.max(0, t - T0), 2 * R / v), n = 60;
  const panel = (cx, rot, lab) => {
    const ang = rot ? 0 : Om * (t - S._sh.t0); const d = []; for (let i = 0; i < 80; i++) { const q = i / 80 * TAU; d.push([cx + Math.cos(q) * R, 560 + Math.sin(q) * R]); }
    cels(g, d, { base: '#d8a060', sh: '#a87030', sw: 10, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 });
    for (let k = 0; k < 6; k++) { const q = ang + k / 6 * TAU; ink(g, [[cx, 560], [cx + Math.cos(q) * R, 560 + Math.sin(q) * R]], 3, { col: PAL.ink, a: 0.35, t0: 0, t1: 0 }); }
    const path_ = []; for (let i = 0; i <= n; i++) { const s = tt * i / n; let x = -R + v * s, y = 0; if (rot) { const q = -Om * s; [x, y] = [x * Math.cos(q) - y * Math.sin(q), x * Math.sin(q) + y * Math.cos(q)]; } path_.push([cx + x, 560 + y]); }
    if (path_.length > 1 && tt > 0) ink(g, path_, 5, { col: PAL.red, t0: 0.05, t1: 0.05 }); const last = path_[path_.length - 1]; particle(g, last[0], last[1], 18, '#ffffff', { glow: false });
    tag(g, cx, 140, lab, t, S._sh.t0 + 0.2, 1e9, { size: 34, bg: rot ? '#fbe2c0' : PAL.card });
  };
  panel(520, false, 'seen from above: a straight line'); panel(1400, true, 'seen from the merry-go-round: it curves');
  tag(g, 960, 980, t < SW(8, 'north') ? 'nothing pushed it · you turned underneath it' : 'north: curves right · south: curves left', t, t < SW(8, 'north') ? SW(8, 'nothing') - 0.2 : SW(8, 'north') - 0.2, 1e9, { size: 36 });
}, { punch: false });
pshot('coriolis8', SW(8, 'called') - 0.3, 'coriolis', { fit: true, from: [0.5, 0.5, 0.92], to: [0.5, 0.5, 1.0], tags: [[1500, 420, 'the Coriolis effect', SW(8, 'called'), { size: 48, bg: '#fbe2c0' }], [1500, 620, 'Gaspard-Gustave de Coriolis · 1835', SW(8, 'french'), { size: 34 }]] });
pshot('hurr8', SW(8, 'hurricanes') - 0.3, 'hurricane', { from: [0.5, 0.45, 1.0], to: [0.5, 0.45, 1.2], tags: [[960, 150, 'Hurricane Katrina, 2005 (NASA)', SW(8, 'hurricanes'), { size: 36 }], [960, 880, 'counterclockwise in the north · clockwise in the south', SW(8, 'counterclockwise') - 0.2, { size: 40, bg: '#fbe2c0' }]] });
mapShot('band8', SW(8, 'trade') - 0.3, 'map_today', { z0: 1, z1: 1.04, draw0: (g, t, S) => {
  if (t > SW(8, 'five') - 0.3) { const y0 = mapXY(5, 0)[1], y1 = mapXY(-5, 0)[1]; g.fillStyle = `rgba(255,90,60,${0.35 * seg(t, SW(8, 'five') - 0.3, SW(8, 'five') + 0.3)})`; g.fillRect(0, y0, 1920, y1 - y0); }
}, draw: (g, t, S, P) => {
  for (const la of [15, -15]) for (let lo = -170; lo < 180; lo += 40) { const p = P(la, lo), ph = ((t - S._sh.t0) * 0.6 + lo / 400) % 1; arrow(g, p[0] + 30 - ph * 40, p[1], p[0] - 50 - ph * 40, p[1] + (la > 0 ? 18 : -18), 5, '#ffffff', 0.85); }
  for (const la of [48, -48]) { const pts = []; for (let lo = -180; lo <= 180; lo += 6) pts.push(P(la + 7 * Math.sin(lo / 25 + (t - S._sh.t0) * 0.5), lo)); ink(g, pts, 6, { col: '#ffd25a', a: 0.8, t0: 0.05, t1: 0.05 }); }
  tag(g, 400, 120, 'trade winds (white) · jet streams (gold)', t, SW(8, 'trade'), 1e9, { size: 34 });
  tag(g, 1350, 120, 'within ~5° of the equator: almost no hurricanes form', t, SW(8, 'five') - 0.2, 1e9, { size: 34, bg: '#fbe2c0' });
} });
board('loop8', SW(8, 'twist') - 0.3, 'blue', 801, (g, t, S) => {
  const cx = 960, cy = 600, R = 270; particle(g, cx, cy, R, '#6a8ab0', { glow: false });
  g.save(); g.beginPath(); g.arc(cx, cy, R, -Math.PI / 2, Math.PI / 2); g.closePath(); g.fillStyle = 'rgba(255,190,90,0.45)'; g.fill(); g.restore();
  stamp(g, 960, 170, 'NO HURRICANES · NO JET STREAMS', t, SW(8, 'hurricanes', 3) - 0.2, 1e9, { size: 46 });
  if (t > SW(8, 'loop') - 0.4) { const u = seg(t, SW(8, 'loop') - 0.4, SW(8, 'loop') + 2.5), lp = []; for (let i = 0; i <= 80 * u; i++) { const q = -i / 80 * TAU + 0.15; lp.push([cx + Math.cos(q) * (R + 90) * (Math.cos(q) > 0 ? 1 : 1), cy + Math.sin(q) * (R + 60)]); } if (lp.length > 2) ink(g, lp, 9, { col: PAL.red, t0: 0.05, t1: 0.05 }); }
  tag(g, 1550, 420, 'rises over the hot day side', t, SW(8, 'rising') - 0.2, 1e9, { size: 32 }); tag(g, 380, 420, 'sinks on the night side', t, SW(8, 'sinking') - 0.2, 1e9, { size: 32 }); tag(g, 960, 960, 'one enormous loop (models of non-spinning planets)', t, SW(8, 'loop') - 0.2, 1e9, { size: 34, bg: '#fbe2c0' });
}, { punch: false });
kinShot('myth8', SW(8, 'myth') - 0.4, [['NOW, A MYTH.', '#ffffff']], { bg: '#0b0d14', size: 140, y: 420, draw: (g, t, S) => kin(g, [['SINKS AND TOILETS?', '#7fd0ff']], t, SW(8, 'toilets') - 0.2, { size: 100, y: 640 }) });
board('sink8', SW(8, 'sink') - 0.3, 'sage', 811, (g, t, S) => {
  writeText(g, 'Coriolis in a sink: 2Ωv ≈ 2 × 7.3 × 10⁻⁵ × 0.01 m/s ≈ 10⁻⁶ m/s²', 960, 260, 46, t, S._sh.t0 + 0.2, 1.0, { upright: true, align: 'center' });
  writeText(g, 'gravity: 9.8 m/s²', 960, 400, 56, t, S._sh.t0 + 1.0, 0.5, { upright: true, align: 'center', col: PAL.blue });
  stamp(g, 960, 620, '≈ 10 MILLION TIMES WEAKER', t, SW(8, 'million') - 0.2, 1e9, { size: 60 });
  tag(g, 960, 860, 'the basin’s shape and how you filled it matter far more', t, SW(8, 'shape') - 0.2, 1e9, { size: 38, bg: '#fbe2c0' });
}, { punch: false });
function tank(g, cx, cy, R, t, tIn, ccw) { const d = []; for (let i = 0; i < 80; i++) { const q = i / 80 * TAU; d.push([cx + Math.cos(q) * R, cy + Math.sin(q) * R]); } cels(g, d, { base: '#9fd0e8', sh: '#6aa0c0', sw: 10, rim: '#e8f8ff', rw: 5, L: LIGHT, line: PAL.ink, lw: 3, seed: 4 }); glowDot(g, cx, cy, 30, '#1a3050', 1);
  if (t > tIn) { const u = seg(t, tIn, tIn + 1.5), sp = []; for (let i = 0; i <= 90 * u; i++) { const q = (ccw ? -1 : 1) * i / 90 * 3 * TAU, rr = R * 0.85 * (1 - i / 100); sp.push([cx + Math.cos(q) * rr, cy + Math.sin(q) * rr]); } if (sp.length > 2) { ink(g, sp, 5, { col: '#1f4f8f', t0: 0.05, t1: 0.05 }); const m = sp.length - 1, q = Math.max(0, m - 3); arrow(g, sp[q][0], sp[q][1], sp[m][0], sp[m][1], 7, '#1f4f8f'); for (const f of [0.25, 0.5]) { const j = Math.floor(m * f); if (j > 3) arrow(g, sp[j - 3][0], sp[j - 3][1], sp[j][0], sp[j][1], 7, '#1f4f8f'); } } } }
board('shapiro8', SW(8, 'zero') - 0.3, 'warm', 821, (g, t, S) => {
  tank(g, 700, 560, 330, t, SW(8, 'counterclockwise', 2) - 0.3, true);
  tag(g, 1480, 260, 'Ascher Shapiro · MIT · 1962', t, SW(8, 'nineteen') - 0.2, 1e9, { size: 40 }); tag(g, 1480, 430, 'a round tank ≈ 2 m across', t, SW(8, 'tank') - 0.2, 1e9, { size: 36 });
  tag(g, 1480, 600, 'left still for a full day', t, SW(8, 'full') - 0.2, 1e9, { size: 36 }); tag(g, 1480, 780, 'drained COUNTERCLOCKWISE\n(northern hemisphere)', t, SW(8, 'counterclockwise', 2) - 0.2, 1e9, { size: 38, bg: '#fbe2c0' });
}, { punch: false });
board('sydney8', SW(8, 'sydney') - 0.4, 'blue', 831, (g, t, S) => {
  tank(g, 700, 560, 330, t, SW(8, 'clockwise', 2) - 0.3, false);
  tag(g, 1480, 300, 'Sydney, Australia · 1965', t, SW(8, 'sydney') - 0.2, 1e9, { size: 40 }); tag(g, 1480, 500, 'drained CLOCKWISE', t, SW(8, 'clockwise', 2) - 0.2, 1e9, { size: 44, bg: '#fbe2c0' });
  stamp(g, 1480, 760, 'REAL · VERY, VERY SMALL', t, SW(8, 'real') - 0.2, 1e9, { size: 44 });
}, { punch: false });

// ================= 9. The shield =================
clipShot('airglow9', SB(9) - 0.2, 'airglow', { draw: (g, t, S) => kin(g, [["ONE MORE THING YOU CAN'T SEE", '#ffffff']], t, S._sh.t0 + 0.3, { size: 84, y: 900 }) });
flat('core9', SW(9, 'nearly') - 0.3, '#07090f', (g, t, S) => {
  earthCut(g, 640, 560, 420, t, { labels: S._sh.t0 + 0.3 });
  tag(g, 1420, 880, 'liquid iron ≈ 2,900 km down,' + String.fromCharCode(10) + 'about as hot as the Sun’s surface', t, SW(9, 'hot') - 0.3, 1e9, { size: 30, bg: '#fbe2c0' });
}, { punch: false });
clipShot('dynamo9', SW(9, 'moving') - 0.3, 'magfield', { draw: (g, t, S) => {
  tag(g, 960, 120, 'moving liquid metal → a magnetic field: the geodynamo', t, SW(9, 'geodynamo') - 0.6, 1e9, { size: 38 });
  tag(g, 520, 960, 'points your compass north', t, SW(9, 'compass') - 0.2, 1e9, { size: 36 }); tag(g, 1400, 960, 'steers the solar wind around us', t, SW(9, 'solar') - 0.2, 1e9, { size: 36, bg: '#fbe2c0' });
} });
const DIP = fieldLines([{ x: 0, y: -60, q: 1 }, { x: 0, y: 60, q: -1 }], { n: 16, step: 6, box: [-600, -600, 600, 600] });
const MULTI = fieldLines([{ x: -120, y: -80, q: 1 }, { x: 110, y: -40, q: -1 }, { x: -40, y: 110, q: 1 }, { x: 140, y: 120, q: -1 }, { x: -150, y: 60, q: -0.6 }, { x: 60, y: -150, q: 0.6 }], { n: 9, step: 6, box: [-600, -600, 600, 600] });
const shift = (L, x, y, s = 1) => L.map(l => l.map(([a, b]) => [x + a * s, y + b * s]));
board('columns9', SW(9, "here's") - 0.3, 'blue', 901, (g, t, S) => {
  const cx = 700, cy = 560, R = 360; const ring = (r, col) => { const p = []; for (let i = 0; i < 90; i++) { const q = i / 90 * TAU; p.push([cx + Math.cos(q) * r, cy + Math.sin(q) * r]); } cels(g, p, { base: col, line: PAL.ink, lw: 2.5, seed: Math.floor(r) }); };
  ring(R, '#f0a030'); ring(R * 0.35, '#fff1b8');
  ink(g, [[cx, cy - R - 80], [cx, cy + R + 80]], 4, { col: PAL.ink, t0: 0, t1: 0 }); tag(g, cx, cy - R - 120, 'spin axis', t, S._sh.t0 + 0.2, 1e9, { size: 28 });
  if (t > SW(9, 'columns') - 0.6) for (let k = -3; k <= 3; k++) { if (Math.abs(k) < 1) continue; const x = cx + k * 95, hh = Math.sqrt(Math.max(0, R * R - (k * 95) ** 2)) * 0.9; g.save(); g.globalAlpha = seg(t, SW(9, 'columns') - 0.6 + Math.abs(k) * 0.1, SW(9, 'columns') + Math.abs(k) * 0.1); cels(g, rough([[x - 28, cy - hh], [x + 28, cy - hh], [x + 28, cy + hh], [x - 28, cy + hh]], 1.5, k + 9, 10), { base: k % 2 ? '#d86a20' : '#f8c060', line: PAL.ink, lw: 2, seed: k + 20 }); g.restore(); }
  tag(g, 1480, 300, 'the Coriolis effect lines the churning iron\nup into columns along the axis', t, SW(9, 'columns') - 0.4, 1e9, { size: 32 });
  if (t > SW(9, 'dipole') - 0.4) { drawLines(g, shift(DIP, 1480, 680, 0.55), seg(t, SW(9, 'dipole') - 0.4, SW(9, 'dipole') + 1.2), { col: PAL.blue, w: 2.5 }); tag(g, 1480, 940, 'a dipole, like a bar magnet', t, SW(9, 'dipole') - 0.2, 1e9, { size: 32, bg: '#fbe2c0' }); }
  tag(g, cx, 1000, 'not to scale', t, S._sh.t0 + 0.3, 1e9, { size: 24 });
}, { punch: false });
board('models9', SW(9, 'computer') - 0.3, 'sage', 911, (g, t, S) => {
  tag(g, 960, 110, 'dynamo models · Christensen & Aubert, 2006', t, SW(9, 'christensen') - 0.3, 1e9, { size: 36 });
  drawLines(g, shift(DIP, 520, 560, 0.9), seg(t, SW(9, 'dominates') - 0.8, SW(9, 'dominates') + 0.8), { col: PAL.blue, w: 3 });
  tag(g, 520, 960, 'rotation dominates: a strong, simple dipole', t, SW(9, 'dominates') - 0.4, 1e9, { size: 32, bg: '#d9f0ff' });
  if (t > SW(9, 'loses') - 0.3) { drawLines(g, shift(MULTI, 1400, 560, 0.9), seg(t, SW(9, 'loses') - 0.3, SW(9, 'loses') + 1.2), { col: PAL.red, w: 3 }); tag(g, 1400, 960, 'rotation loses its grip: a messy tangle', t, SW(9, 'tangle') - 0.4, 1e9, { size: 32, bg: '#fbe2c0' }); }
  tag(g, 960, 1010, 'cartoon of field lines', t, S._sh.t0 + 0.3, 1e9, { size: 24 });
}, { punch: false });
kinShot('fade9', SW(9, 'nonspinning') - 0.5, [['NO SPIN → NO DIPOLE', '#ffffff']], { bg: '#0b0d14', size: 120, y: 380, draw: (g, t, S) => tag(g, 960, 640, 'not overnight', t, SW(9, 'overnight') - 0.2, 1e9, { size: 44 }) });
pshot('aurora9', SW(9, 'thousands') - 0.4, 'aurora', { tags: [[960, 150, 'a field like ours: thousands of years to fade', SW(9, 'thousands') - 0.2, { size: 40 }], [960, 880, 'a prediction from models, not an observation', SW(9, 'prediction') - 0.2, { size: 40, bg: '#fbe2c0' }]] });
board('venus9', SW(9, 'venus') - 0.3, 'warm', 921, (g, t, S) => {
  print(g, 'venus', 560, 470, 520, t, S._sh.t0 + 0.05, { caption: 'Venus: no global field' }); print(g, 'mars', 1360, 470, 520, t, SW(9, 'mars') - 0.2, { caption: 'Mars: lost its field ~4 billion years ago', cs: 28, rot: 0.04 });
  tag(g, 560, 940, '(its spin may not be the only reason)', t, SW(9, 'reason') - 0.4, 1e9, { size: 28 });
}, { punch: false });
docOr('maven9', SW(9, 'maven') - 0.3, 'maven', { path: [[0, 'solar wind'], [SW(9, 'hundred') - 0.6, '100 grams']], hl: [['100 grams', SW(9, 'hundred')], ['every second', SW(9, 'second') - 0.2]], src: 'NASA, 5 Nov 2015: MAVEN results',
  tags: [[960, 960, 'over billions of years: most of Mars’s air, gone', SW(9, 'billions') - 0.2, { size: 38, bg: '#fbe2c0' }]] }, 'the solar wind strips away gas at a rate of about 100 grams every second', 'NASA, MAVEN results, 5 Nov 2015');
kinShot('shield9', SW(9, 'shield') - 1.4, [['STOP THE SPIN …', '#ffffff']], { bg: '#06070b', size: 120, y: 420, draw: (g, t, S) => kin(g, [['… AND THE SHIELD GOES WITH IT', '#ff6a55']], t, SW(9, 'shield') - 0.3, { size: 90, y: 640 }) });

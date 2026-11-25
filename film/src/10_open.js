// ================= helpers for v2 shots (07: What if the Earth stopped spinning) =================
function pshot(name, t0, key, o = {}) {
  scene(name, t0, () => ({}), (g, l, t, S) => {
    photoFull(g, key, t, S._sh.t0, S._sh.t1, o);
    for (const [x, y, txt, tIn, op] of (o.tags || [])) tag(g, x, y, txt, t, tIn, (op && op.out) ?? 1e9, Object.assign({ size: 40 }, op || {}));
    if (o.draw) o.draw(g, t, S);
  }, { in: o.in });
}
function dshot(name, t0, key, o = {}) {
  scene(name, t0, () => ({}), (g, l, t, S) => {
    docView(g, key, t, o);
    for (const [x, y, txt, tIn, op] of (o.tags || [])) tag(g, x, y, txt, t, tIn, (op && op.out) ?? 1e9, Object.assign({ size: 38 }, op || {}));
    if (o.draw) o.draw(g, t, S);
  }, { in: o.in });
}
function qshot(name, t0, text, who, o = {}) {
  flat(name, t0, o.bg || '#15161c', (g, t, S) => {
    if (o.key) print(g, o.key, o.px ?? 330, o.py ?? 520, o.ph ?? 520, t, S._sh.t0 + 0.05, { bw: o.bw, credit: true, creditCol: 'rgba(230,230,230,0.8)' });
    quoteCard(g, text, who, t, S._sh.t0 + (o.delay ?? 0.1), 1e9, { x: o.key ? 1180 : 960, y: o.y ?? 520, w: o.key ? 900 : 1300, size: o.size ?? 50 });
    if (o.draw) o.draw(g, t, S);
  }, { punch: o.punch ?? true, frames: [[1, 960, 540], [1.06, 960, 530]] });
}
// a document screenshot if snap_docs captured it with its phrases, else a quote card with the same words
function docOr(name, t0, key, o, text, who) {
  const d = DOCS[key]; if (d && IMGSRC['doc_' + key] && d.hl && Object.keys(d.hl).length) dshot(name, t0, key, o); else qshot(name, t0, text, who, { size: 46 });
}
// ---------- maps (equirectangular images: our ETOPO1 relief, today and with no spin) ----------
const MAPBOX = [0, 60, 1920, 960];
const mapXY = (lat, lon) => [MAPBOX[0] + (lon + 180) / 360 * MAPBOX[2], MAPBOX[1] + (90 - lat) / 180 * MAPBOX[3]];
// mapShot: o.focus [lat, lon], o.z0/o.z1 zoom; o.draw(g, t, S, P) where P(lat, lon) -> screen [x, y] under the current zoom
function mapShot(name, t0, key, o = {}) {
  scene(name, t0, () => ({}), (g, l, t, S) => {
    const u = E.io2(seg(t, S._sh.t0, S._sh.t1)), z = lerp(o.z0 ?? 1, o.z1 ?? 1.08, u);
    const f = o.focus ? mapXY(o.focus[0], o.focus[1]) : [960, 540], k = o.focus ? lerp(o.k0 ?? 0.6, o.k1 ?? 1, u) : 0;
    const c = [lerp(960, f[0], k), lerp(540, f[1], k)];
    const P = (la, lo) => { const p = mapXY(la, lo); return [(p[0] - c[0]) * z + 960, (p[1] - c[1]) * z + 540]; };
    g.fillStyle = '#071226'; g.fillRect(0, 0, W, H);
    g.save(); g.translate(960, 540); g.scale(z, z); g.translate(-c[0], -c[1]); g.drawImage(IMG[key], MAPBOX[0], MAPBOX[1], MAPBOX[2], MAPBOX[3]);
    if (o.draw0) o.draw0(g, t, S); g.restore();
    if (o.draw) o.draw(g, t, S, P);
    credit(g, o.src || (key === 'map_nospin' ? 'our calculation: NOAA ETOPO1 relief, spin removed, water conserved' : 'relief: NOAA ETOPO1'));
  }, { in: o.in });
}
function pinDot(g, x, y, t, tIn, col = '#ff5a3c') {
  if (on2(t) < tIn) return; const k = E.out3(seg(on2(t), tIn, tIn + 0.3));
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, y, 46 * k, col, 0.7); g.restore();
  g.fillStyle = '#fff'; g.beginPath(); g.arc(x, y, 9 * k, 0, TAU); g.fill(); g.strokeStyle = css(col); g.lineWidth = 4; g.stroke();
}
// horizontal bars: items [[label, value, color, valueText]], o.max | o.log [lo, hi]
function hbars(g, x, y, w, items, t, tIn, o = {}) {
  const rh = o.rowH ?? 110, lw = o.labelW ?? 420;
  items.forEach(([lab, v, col, vt, tI], i) => {
    const ti = tI ?? tIn + i * (o.stagger ?? 0.35); if (on2(t) < ti) return;
    const yy = y + i * rh, u = E.out3(seg(t, ti, ti + 0.7)), fr = o.log ? clamp((Math.log10(v) - o.log[0]) / (o.log[1] - o.log[0])) : v / (o.max ?? 1);
    g.save(); g.globalAlpha = seg(on2(t), ti, ti + 0.2);
    g.font = `600 ${o.fs ?? 38}px Georgia, serif`; g.fillStyle = css(o.labCol || PAL.ink); g.textAlign = 'right'; g.textBaseline = 'middle'; g.fillText(lab, x + lw - 24, yy);
    const bw = (w - lw) * fr * u, box = [[x + lw, yy - rh * 0.3], [x + lw + Math.max(4, bw), yy - rh * 0.3], [x + lw + Math.max(4, bw), yy + rh * 0.3], [x + lw, yy + rh * 0.3]];
    cels(g, box, { base: col || PAL.blue, sh: mixv(col || PAL.blue, '#000', 0.3), sw: 5, L: LIGHT, line: PAL.ink, lw: 2.2, seed: i + 7 });
    if (vt) { g.font = `700 ${o.vfs ?? 36}px Georgia, serif`; g.textAlign = 'left'; g.fillStyle = css(o.valCol || PAL.ink); g.fillText(vt, x + lw + Math.max(4, bw) + 18, yy + 2); }
    g.restore();
  });
}
// a drawn coffee cup with steam (cel style)
function coffeeCup(g, x, y, s, t, o = {}) {
  const sw = o.spill ?? 0;
  cels(g, rough([[x - 1.7 * s, y + 0.55 * s], [x + 1.7 * s, y + 0.55 * s], [x + 1.4 * s, y + 0.8 * s], [x - 1.4 * s, y + 0.8 * s]], 1.5, 3, 10), { base: '#e8e1d6', sh: '#b9ae9c', sw: 6, L: LIGHT, line: PAL.ink, lw: 3, seed: 3 });
  const hd = []; for (let i = 0; i <= 20; i++) { const a = -Math.PI / 2 + i / 20 * Math.PI; hd.push([x + 0.95 * s + Math.cos(a) * 0.45 * s, y - 0.15 * s + Math.sin(a) * 0.4 * s]); }
  ink(g, hd, s * 0.14, { col: '#d9d2c6', t0: 0.1, t1: 0.1, seed: 4 }); ink(g, hd, 3, { col: PAL.ink, t0: 0.1, t1: 0.1, seed: 5, a: 0.8 });
  cels(g, rough([[x - 1.05 * s, y - 0.75 * s], [x + 1.05 * s, y - 0.75 * s], [x + 0.8 * s, y + 0.55 * s], [x - 0.8 * s, y + 0.55 * s]], 1.5, 6, 10), { base: '#f4efe6', sh: '#cfc5b4', sw: 10, rim: '#fff', rw: 4, L: LIGHT, line: PAL.ink, lw: 3, seed: 6 });
  const el = []; for (let i = 0; i < 40; i++) { const a = i / 40 * TAU; el.push([x + Math.cos(a) * 1.02 * s, y - 0.75 * s + Math.sin(a) * (0.2 + sw * 0.1) * s + Math.cos(a) * sw * 0.25 * s]); }
  cels(g, el, { base: '#5a3418', sh: '#3a200e', sw: 6, L: LIGHT, line: PAL.ink, lw: 2.5, seed: 8 });
  for (let k = 0; k < 3; k++) { const pts = []; for (let i = 0; i < 18; i++) { const yy = y - 0.9 * s - i * s * 0.07; pts.push([x + (k - 1) * 0.45 * s + Math.sin(i * 0.5 + t * 2 + k) * s * 0.12, yy]); } ink(g, pts, 5, { col: '#ffffff', a: 0.35, t0: 0.3, t1: 0.6, seed: k + 20 }); }
}
// an Earth cutaway: crust, mantle, outer core, inner core; o.labels true shows names; o.arrows: rotation arrows (mantle vs inner core)
function earthCut(g, x, y, R, t, o = {}) {
  const ring = (r, col, sh) => { const p = []; for (let i = 0; i < 90; i++) { const a = i / 90 * TAU; p.push([x + Math.cos(a) * r, y + Math.sin(a) * r]); } cels(g, p, { base: col, sh: sh || mixv(col, '#000', 0.3), sw: r * 0.06, L: LIGHT, line: PAL.ink, lw: 2.5, seed: Math.floor(r) }); };
  ring(R, '#5f7fa8'); ring(R * 0.985, '#b0562c'); ring(R * 0.546, '#f0a030', '#c07018'); ring(R * 0.19, '#fff1b8', '#e8c060');
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, y, R * 0.6, '#ffb040', 0.25); g.restore();
  if (o.labels) {
    tag(g, x + R * 1.35, y - R * 0.75, 'crust + mantle (rock)', t, o.labels, 1e9, { size: 30 });
    tag(g, x + R * 1.35, y - R * 0.1, 'outer core: liquid iron', t, o.labels + 0.3, 1e9, { size: 30, bg: '#fbe2c0' });
    tag(g, x + R * 1.35, y + R * 0.55, 'inner core: solid iron', t, o.labels + 0.6, 1e9, { size: 30 });
  }
}

// ================= 0. Cold open =================
clipShot('bullet0', 0, 'limb', { draw: (g, t, S) => {
  kin(g, [['RIGHT NOW', '#ffffff']], t, 0.3, { size: 110, y: 380 });
  kin(g, [['YOU ARE MOVING', '#ffffff'], ['FASTER THAN A BULLET', '#ffcf5a']], t, SW(0, 'moving') - 0.1, { size: 120, y: 640, stagger: 0.6 });
} });
glShot('globe0', SW(0, 'ground') - 0.25, T => glGlobe(T, { arrows: true, spin: 0.45, d0: 3.4, d1: 3.0 }), { over: (g, t, S) => {
  tag(g, 960, 120, 'the ground under you is turning', t, S._sh.t0 + 0.2, 1e9, { size: 40 });
  tag(g, 960, 960, 'equator: ≈ 1,040 mph · 1,674 km/h', t, SW(0, 'equator'), 1e9, { size: 46, bg: '#fbe2c0' });
  credit(g, '3D: our ETOPO1 relief globe · spin sped up');
} });
pshot('karachi0', SW(0, 'karachi') - 0.3, 'karachi', { tags: [[960, 860, 'Karachi ≈ 940 mph', SW(0, 'karachi'), { size: 64, bg: '#fbe2c0' }]] });
mapShot('ny0', SW(0, 'york') - 0.45, 'map_today', { focus: [40.7, -74], z0: 2.2, z1: 2.6, k0: 1, draw: (g, t, S, P) => {
  const p = P(40.71, -74.0); pinDot(g, p[0], p[1], t, S._sh.t0 + 0.1); tag(g, p[0] + 40, p[1] - 130, 'New York ≈ 790 mph', t, S._sh.t0 + 0.2, 1e9, { size: 56, bg: '#fbe2c0' });
} });
pshot('london0', SW(0, 'london') - 0.3, 'london', { tags: [[960, 860, 'London ≈ 650 mph', SW(0, 'london'), { size: 64, bg: '#fbe2c0' }]] });
kinShot('feel0', SW(0, 'feel') - 0.3, [["YOU CAN'T FEEL ANY OF IT", '#ffffff']], { bg: '#0b0d14', size: 110, draw: (g, t, S) => kin(g, [['… UNLESS IT STOPS', '#ff6a55']], t, SW(0, 'unless') - 0.1, { size: 110, y: 760 }) });
docOr('viral0', SW(0, 'october') - 0.3, 'ladbible', { path: [[0, 'all'], [SW(0, 'horrifying') - 0.3, 'Horrifying simulation shows what would happen if the Earth stopped spinning']], hl: [['Horrifying simulation shows what would happen if the Earth stopped spinning', SW(0, 'horrifying')]], src: 'LADbible, 5 October 2026', tags: [[960, 980, 'October 2026: a simulation goes viral', SW(0, 'october'), { size: 36 }]] },
  'Horrifying simulation shows what would happen if the Earth stopped spinning', 'LADbible headline, 5 October 2026');
board('search0', SW(0, 'searches') - 0.3, 'blue', 31, (g, t, S) => {
  title(g, 960, 150, 'US monthly searches: “what would happen if the earth stopped spinning”', t, S._sh.t0, 1e9, 40, PAL.ink, { shadow: false, upright: true, weight: 600 });
  hbars(g, 220, 320, 1500, [['July 2026', 5400, '#7fa7c9', '5,400'], ['August', 6600, '#7fa7c9', '6,600'], ['September', 8100, '#7fa7c9', '8,100'], ['October', 18100, PAL.red, '18,100', SW(0, 'doubled')]], t, S._sh.t0 + 0.3, { max: 19000, labelW: 360, rowH: 150 });
  credit(g, 'DataForSEO keyword data (Google Ads estimates), via OpenSEO, 26 Nov 2026');
}, { punch: false });
kinShot('proper0', SW(0, 'properly') - 0.3, [['REAL NUMBERS.', '#ffffff']], { bg: '#0d1117', size: 120, draw: (g, t, S) => {
  kin(g, [['REAL DATA.', '#7fd0ff']], t, SW(0, 'data') - 0.05, { size: 120, y: 540 + 140 }); kin(g, [['REAL PHYSICS.', '#ffcf5a']], t, SW(0, 'physics') - 0.05, { size: 120, y: 540 + 280 });
}, y: 400 });
board('judge0', SW(0, 'clip') - 0.3, 'warm', 41, (g, t, S) => {
  title(g, 960, 170, 'the viral clip', t, S._sh.t0, 1e9, 70, PAL.ink, { shadow: false });
  checklist(g, 420, 360, [['some of it is right', '✓', SW(0, 'right')], ['some depends on a detail it never mentions', '?', SW(0, 'detail')], ['the strangest consequences: not in it', '✗', SW(0, 'strangest')]], t, { size: 46, dy: 170 });
}, { punch: false });
mapShot('londonsea0', SW(0, 'london', 2) - 0.3, 'map_nospin', { focus: [51.5, 0], z0: 2.4, z1: 2.8, k0: 1, draw: (g, t, S, P) => {
  const p = P(51.51, -0.13); pinDot(g, p[0], p[1], t, S._sh.t0 + 0.1); tag(g, p[0], p[1] - 150, 'London: under ≈ 2.5 km of sea', t, S._sh.t0 + 0.3, 1e9, { size: 54, bg: '#fbe2c0' });
} });
mapShot('karachisea0', SW(0, 'karachi', 2) - 0.3, 'map_nospin', { focus: [24.9, 67], z0: 2.4, z1: 2.8, k0: 1, draw: (g, t, S, P) => {
  const p = P(24.86, 67.0); pinDot(g, p[0], p[1], t, S._sh.t0 + 0.1, '#ffd25a'); tag(g, p[0], p[1] - 150, 'Karachi: ≈ 2 km ABOVE the sea', t, S._sh.t0 + 0.3, 1e9, { size: 54, bg: '#fbe2c0' });
} });
function skyRise(g, t, t0, o = {}) { // a horizon at dawn with the Sun coming up in the WEST
  const sk = g.createLinearGradient(0, 0, 0, 760); sk.addColorStop(0, '#16213d'); sk.addColorStop(0.6, '#c0603a'); sk.addColorStop(1, '#f4b060'); g.fillStyle = sk; g.fillRect(0, 0, W, 760);
  const u = E.out2(seg(t, t0, t0 + (o.dur ?? 6))), sx = 380 + u * 120, sy = 800 - u * 330;
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, sx, sy, 260, '#ffd890', 0.8); g.restore(); g.fillStyle = '#fff4d0'; g.beginPath(); g.arc(sx, sy, 70, 0, TAU); g.fill();
  g.fillStyle = '#14100c'; g.beginPath(); g.moveTo(0, 760); for (let x = 0; x <= W; x += 40) g.lineTo(x, 740 + 18 * n1(x * 0.01, 3)); g.lineTo(W, H); g.lineTo(0, H); g.fill();
  g.font = '800 70px Georgia, serif'; g.textAlign = 'center'; g.fillStyle = '#ffcf5a'; g.fillText('W', 380, 900); g.fillStyle = 'rgba(255,255,255,0.6)'; g.fillText('E', 1540, 900);
}
flat('west0', SW(0, 'sun') - 0.25, '#000', (g, t, S) => { skyRise(g, t, S._sh.t0); kin(g, [['THE SUN RISES IN THE WEST', '#ffffff']], t, SW(0, 'west') - 0.1, { size: 96, y: 200 }); }, { punch: false });
kinShot('year0', SW(0, 'single', 2) - 0.25, [['ONE DAY', '#ffffff'], ['= ONE YEAR', '#ffcf5a']], { bg: '#0b0d14', size: 150, stagger: 0.5 });
flat('coffee0', SW(0, 'place') - 0.3, '#1a1410', (g, t, S) => {
  coffeeCup(g, 960, 600, 190, t); kin(g, [['ONE PLACE ON EARTH', '#ffffff']], t, S._sh.t0 + 0.1, { size: 90, y: 170 });
  tag(g, 960, 960, 'survive the first second · coffee unspilled', t, SW(0, 'coffee'), 1e9, { size: 40, bg: '#fbe2c0' }); stamp(g, 1500, 420, '?', t, SW(0, 'find'), 1e9, { size: 140 });
}, { punch: false });
flat('core0', SW(0, 'stranger') - 0.3, '#07090f', (g, t, S) => {
  earthCut(g, 760, 560, 380, t); const a = t * 0.6;
  if (t > SW(0, 'backwards') - 0.2) { const k = seg(t, SW(0, 'backwards') - 0.2, SW(0, 'backwards') + 0.6); g.save(); g.globalAlpha = k; const pts = []; for (let i = 0; i < 30; i++) { const th = -0.4 - i / 30 * 2.2; pts.push([760 + Math.cos(th) * 115, 560 + Math.sin(th) * 115]); } ink(g, pts, 10, { col: '#ff5a3c', t0: 0.05, t1: 0.05 }); arrow(g, pts[27][0], pts[27][1], pts[29][0], pts[29][1], 10, '#ff5a3c'); g.restore(); }
  tag(g, 1480, 420, 'part of our planet', t, SW(0, 'part'), 1e9, { size: 44 }); tag(g, 1480, 600, 'already drifting backwards?', t, SW(0, 'backwards'), 1e9, { size: 44, bg: '#fbe2c0' }); tag(g, 1480, 760, '… relative to you', t, SW(0, 'relative'), 1e9, { size: 36 });
}, { punch: false });
board('qs0', SW(0, "here's") - 0.3, 'sage', 51, (g, t, S) => {
  tag(g, 960, 250, '1 · What if the Earth stops in an instant?', t, SW(0, 'instant') - 0.6, 1e9, { size: 48 });
  tag(g, 960, 470, '2 · What if it stops slowly, over years?', t, SW(0, 'slowly') - 0.5, 1e9, { size: 48 });
  tag(g, 960, 690, '3 · Is the Earth actually stopping?', t, SW(0, 'actually') - 0.3, 1e9, { size: 50, bg: '#fbe2c0' });
  stamp(g, 1400, 880, 'YES. MEASURABLY.', t, SW(0, 'measurably'), 1e9, { size: 56 });
});

// ================= 1. You are moving =================
board('sidereal1', SB(1) - 0.2, 'blue', 101, (g, t, S) => {
  const cx = 620, cy = 560, R = 300, a0 = -0.6, a1 = a0 + 0.42 * seg(t, SW(1, 'orbit') - 0.3, SW(1, 'orbit') + 1.2);
  g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, cx, cy, 140, '#ffd27a', 0.9); g.restore(); particle(g, cx, cy, 54, '#ffc840', { glow: false });
  const orb = []; for (let i = 0; i <= 90; i++) { const a = i / 90 * TAU; orb.push([cx + Math.cos(a) * R, cy + Math.sin(a) * R * 0.8]); } ink(g, orb, 3, { col: PAL.ink, a: 0.5, t0: 0, t1: 0 });
  for (const [a, k] of [[a0, 0], [a1, 1]]) { if (k && a1 - a0 < 0.02) continue; const ex = cx + Math.cos(a) * R, ey = cy + Math.sin(a) * R * 0.8; particle(g, ex, ey, 34, '#4f8ad0', { glow: false, a: k ? 1 : 0.5 }); ink(g, [[ex, ey], [ex + 0, ey - 70]], 5, { col: PAL.red, t0: 0.05, t1: 0.05 }); }
  tag(g, 1420, 300, 'one turn vs the stars:\n23 h 56 min 4 s', t, SW(1, 'twentythree'), 1e9, { size: 42 });
  tag(g, 1420, 560, 'one day vs the Sun: 24 h', t, SW(1, 'twentyfourhour'), 1e9, { size: 42, bg: '#fbe2c0' });
  tag(g, 1420, 790, 'the Earth moves on its orbit,\nso it turns a little extra (≈ 1°)', t, SW(1, 'extra'), 1e9, { size: 34 });
});
glShot('circle1', SW(1, 'circle') - 0.3, T => glGlobe(T, { arrows: true, spin: 0.4, d0: 3.1, d1: 2.8, lon0: 0.4 }), { over: (g, t, S) => {
  tag(g, 960, 120, 'the equator: a circle ≈ 40,000 km long, once a turn', t, S._sh.t0 + 0.2, 1e9, { size: 38 });
  tag(g, 960, 960, '≈ 465 metres every second', t, SW(1, 'meters'), 1e9, { size: 52, bg: '#fbe2c0' }); credit(g, '3D: our ETOPO1 relief globe · spin sped up');
} });
board('speeds1', SW(1, 'sound') - 0.4, 'warm', 111, (g, t, S) => {
  title(g, 960, 170, 'metres per second', t, S._sh.t0, 1e9, 56, PAL.ink, { shadow: false });
  hbars(g, 160, 360, 1560, [['speed of sound', 343, '#7fa7c9', '≈ 340', SW(1, 'sound')], ['9 mm pistol bullet', 360, '#9a8a7a', '≈ 360', SW(1, 'pistol')], ['the ground at the equator', 465, PAL.red, '465', SW(1, 'pistol') + 1.4]], t, S._sh.t0, { max: 520, labelW: 520, rowH: 170 });
}, { punch: false });
board('lat1', SW(1, 'move') - 0.3, 'sage', 121, (g, t, S) => {
  const M = axes(g, 240, 900, 1400, 640, { x0: 0, x1: 90, y0: 0, y1: 1100, xl: 'latitude (°)', yl: 'ground speed (mph)' }), u = seg(t, S._sh.t0 + 0.2, S._sh.t0 + 2.2);
  plot(g, M, la => 1040.4 * Math.cos(la * Math.PI / 180), 0, 90, u, { col: PAL.red, w: 7 });
  for (const v of [0, 30, 60]) { const p = M(v, 0); g.font = '500 28px Georgia'; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.fillText(v + '°', p[0], p[1] + 40); }
  const pt = (la, lab, tIn, dy = -80, dx = 40) => { if (t < tIn) return; const p = M(la, 1040.4 * Math.cos(la * Math.PI / 180)); pinDot(g, p[0], p[1], t, tIn); tag(g, p[0] + dx, p[1] + dy, lab, t, tIn + 0.1, 1e9, { size: 32 }); };
  pt(0, 'equator: 1,040', S._sh.t0 + 0.4, 70, 140); pt(45, '45° N (Minneapolis, Bordeaux, Milan): 735', SW(1, 'fortyfive'), -90); pt(90, 'North Pole: 0', SW(1, 'pole') - 0.2, -100);
}, { punch: false });
flat('orbit1', SW(1, 'only') - 0.3, '#06070d', (g, t, S) => {
  kin(g, [['SPIN: 1,040 MPH', '#ffffff']], t, S._sh.t0 + 0.1, { size: 96, y: 300 });
  kin(g, [['ORBIT: 67,000 MPH', '#7fd0ff']], t, SW(1, 'orbits') - 0.1, { size: 96, y: 520 });
  kin(g, [['AROUND THE GALAXY: ~500,000 MPH', '#ffcf5a']], t, SW(1, 'galaxy') - 0.1, { size: 80, y: 740 });
}, { punch: false });
flat('cup1', SW(1, 'cup') - 0.3, '#1a1410', (g, t, S) => { coffeeCup(g, 960, 640, 200, t); kin(g, [['SO WHY IS IT PERFECTLY STILL?', '#ffffff']], t, S._sh.t0 + 0.2, { size: 84, y: 170 }); }, { punch: false });
pshot('galileo1', SW(1, 'sixteen') - 0.3, 'galileo', { from: [0.5, 0.3, 1.0], to: [0.5, 0.25, 1.12], tags: [[960, 900, '1632 · Galileo, Dialogue Concerning the Two Chief World Systems', SW(1, 'sixteen'), { size: 36 }]] });
const shipDraw = (g, t, S) => {
  const sail = t > SW(1, 'sail'), off = sail ? (t - SW(1, 'sail')) * 0 : 0;   // inside the cabin nothing changes: that is the point
  cels(g, rough([[360, 220], [1560, 220], [1560, 860], [360, 860]], 2, 4, 14), { base: '#d8c2a0', sh: '#b49a74', sw: 10, L: LIGHT, line: PAL.ink, lw: 4, seed: 4 });
  // the fish bowl
  const bowl = []; for (let i = 0; i < 50; i++) { const a = i / 50 * TAU; bowl.push([760 + Math.cos(a) * 170, 650 + Math.sin(a) * 140]); } cels(g, bowl, { base: '#9fd0e8', sh: '#6aa0c0', sw: 8, rim: '#e8f8ff', rw: 4, L: LIGHT, line: PAL.ink, lw: 3, seed: 6 });
  for (let k = 0; k < 3; k++) { const fx = 760 + 90 * Math.sin(t * 0.9 + k * 2.1), fy = 640 + 50 * Math.sin(t * 0.6 + k), d = Math.cos(t * 0.9 + k * 2.1) > 0 ? 1 : -1; cels(g, [[fx - 30 * d, fy], [fx + 22 * d, fy - 14], [fx + 30 * d, fy], [fx + 22 * d, fy + 14]], { base: '#f08a30', line: PAL.ink, lw: 2, seed: k }); }
  // dripping bottle: drops fall straight down
  cels(g, rough([[1280, 260], [1340, 260], [1350, 400], [1270, 400]], 1, 7, 8), { base: '#6a9a6a', line: PAL.ink, lw: 3, seed: 7 });
  for (let k = 0; k < 4; k++) { const ph = ((t * 0.8 + k * 0.25) % 1); glowDot(g, 1310, 420 + ph * 360, 10, '#9fd0ff', 0.9); }
  cels(g, rough([[1220, 800], [1400, 800], [1380, 840], [1240, 840]], 1, 8, 8), { base: '#a08060', line: PAL.ink, lw: 3, seed: 8 });
  // butterflies
  for (let k = 0; k < 3; k++) { const bx = 560 + k * 220 + 40 * Math.sin(t * 1.3 + k), by = 330 + 30 * Math.sin(t * 2.1 + k * 2), fl = 0.5 + 0.5 * Math.abs(Math.sin(t * 9 + k)); for (const s of [-1, 1]) cels(g, [[bx, by], [bx + s * 34 * fl, by - 26], [bx + s * 30 * fl, by + 14]], { base: ['#f2c14e', '#d8452e', '#3a6fd0'][k], line: PAL.ink, lw: 1.6, seed: k * 2 + s }); }
  tag(g, 960, 130, sail ? 'sailing, smooth and fast' : 'at anchor', t, sail ? SW(1, 'sail') : SW(1, 'anchor'), 1e9, { size: 44, bg: sail ? '#fbe2c0' : PAL.card });
  stamp(g, 960, 960, 'NO DIFFERENCE', t, SW(1, 'difference'), 1e9, { size: 56 });
};
board('ship1', SW(1, 'shut') - 0.3, 'warm', 131, shipDraw);
kinShot('nodiff1', SW(1, 'difference') - 0.5, [["YOU WON'T SEE", '#ffffff'], ['A SINGLE DIFFERENCE', '#ffcf5a']], { bg: '#0b0d14', size: 110, stagger: 0.4 });
board('ship1b', SW(1, 'drops') - 0.3, 'warm', 131, shipDraw);
kinShot('inertia1', SW(1, 'steady') - 0.3, [["STEADY MOTION: CAN'T BE FELT", '#ffffff']], { bg: '#0b0d14', size: 86, y: 380, draw: (g, t, S) => {
  kin(g, [['CHANGES IN MOTION: CAN', '#ffcf5a']], t, SW(1, 'changes') - 0.1, { size: 86, y: 560 }); tag(g, 960, 820, 'inertia', t, SW(1, 'inertia'), 1e9, { size: 60, bg: '#fbe2c0' });
} });
kinShot('hurt1', SW(1, 'speed', 3) - 0.3, [['SPEED NEVER HURT ANYONE.', '#ffffff']], { bg: '#08080b', size: 110, y: 430, draw: (g, t, S) => kin(g, [['STOPPING DOES.', '#ff5a3c']], t, SW(1, 'stopping') - 0.1, { size: 150, y: 650 }) });

// ================= 2. Proof it spins =================
kinShot('how2', SB(2) - 0.25, [['HOW DO WE KNOW', '#ffffff'], ["IT'S TURNING AT ALL?", '#7fd0ff']], { bg: '#0b0d14', size: 120, stagger: 0.6 });
pshot('foucault2', SW(2, 'january') - 0.3, 'foucault', { fit: true, from: [0.5, 0.5, 0.92], to: [0.5, 0.5, 1.0], tags: [[1500, 300, 'January 1851 · Paris', SW(2, 'january'), { size: 40 }], [1500, 460, 'Léon Foucault', SW(2, 'physicist'), { size: 44, bg: '#fbe2c0' }], [1500, 640, 'a heavy ball · a 2 m wire', SW(2, 'heavy'), { size: 36 }]] });
glShot('pend2', SW(2, 'watched') - 0.3, T => glFoucault(T, {}), { over: (g, t, S) => {
  tag(g, 960, 110, 'the line of the swing slowly turns', t, SW(2, 'line') - 0.2, 1e9, { size: 40 });
  kin(g, [['THE FLOOR WAS TURNING', '#ffcf5a']], t, SW(2, 'floor') - 0.2, { size: 84, y: 940 });
  credit(g, '3D: computed · swing and drift sped up');
} });
qshot('invite2', SW(2, 'weeks') - 0.3, 'You are invited to come and see the Earth turn.', 'Foucault’s invitation, Paris Observatory, February 1851 (translated)', { size: 64 });
pshot('pantheon2', SW(2, 'spring') - 0.3, 'pantheon', { tags: [[960, 160, 'the Panthéon, Paris · spring 1851', SW(2, 'spring'), { size: 40 }], [960, 880, '28 kg ball · 67 m wire', SW(2, 'twentyeightkilogram'), { size: 52, bg: '#fbe2c0' }]] });
pshot('pphoto2', SW(2, 'every') - 0.3, 'pendulum', { from: [0.5, 0.75, 1.0], to: [0.5, 0.8, 1.25], tags: [[960, 160, 'every swing cuts a line in a ridge of sand', SW(2, 'every'), { size: 38 }], [960, 900, 'every few minutes: a little clockwise', SW(2, 'clockwise'), { size: 42, bg: '#fbe2c0' }]] });
board('rate2', SW(2, 'paris', 3) - 0.3, 'blue', 211, (g, t, S) => {
  const M = axes(g, 260, 880, 1300, 600, { x0: 0, x1: 90, y0: 0, y1: 16, xl: 'latitude (°)', yl: 'swing-plane turn (° per hour)' }), u = seg(t, S._sh.t0 + 0.2, S._sh.t0 + 2);
  plot(g, M, la => 15.04 * Math.sin(la * Math.PI / 180), 0, 90, u, { col: PAL.blue, w: 7 });
  const pt = (la, lab, tIn, dx = 40, dy = -80) => { if (t < tIn) return; const p = M(la, 15.04 * Math.sin(la * Math.PI / 180)); pinDot(g, p[0], p[1], t, tIn); tag(g, p[0] + dx, p[1] + dy, lab, t, tIn + 0.1, 1e9, { size: 32 }); };
  pt(48.85, 'Paris: 11.3°/h · full circle ≈ 32 h', S._sh.t0 + 0.6, -60, -90); pt(90, 'North Pole: once a day', SW(2, 'pole') - 0.2, -160, -80); pt(0, 'equator: never', SW(2, 'equator') - 0.2, 180, -40);
  credit(g, 'rate = 15.04° per hour × sin(latitude)');
}, { punch: false });
pshot('space2', SW(2, 'fingerprint') - 0.3, 'bluemarble', { fit: true, from: [0.5, 0.5, 0.95], to: [0.5, 0.5, 1.02], tags: [[960, 980, 'a second fingerprint: you can see it from space', SW(2, 'fingerprint'), { size: 38 }]] });
pshot('newton2', SW(2, 'newton') - 0.3, 'newton', { from: [0.5, 0.3, 1.0], to: [0.5, 0.25, 1.1], tags: [[960, 860, '1687 · flattened at the poles', SW(2, 'flattened'), { size: 50, bg: '#fbe2c0' }]] });
function blob(g, x, y, rx, ry, col, seed) { const p = []; for (let i = 0; i < 80; i++) { const a = i / 80 * TAU; p.push([x + Math.cos(a) * rx, y + Math.sin(a) * ry]); } cels(g, p, { base: col, sh: mixv(col, '#000', 0.3), sw: 14, rim: mixv(col, '#fff', 0.4), rw: 6, L: LIGHT, line: PAL.ink, lw: 3, seed }); }
board('shapes2', SW(2, 'orange') - 0.35, 'warm', 221, (g, t, S) => {
  blob(g, 560, 520, 260, 205, '#f08a20', 3); tag(g, 560, 830, 'Newton: an orange', t, S._sh.t0 + 0.2, 1e9, { size: 44 });
  if (t > SW(2, 'astronomers') - 0.2) print(g, 'cassini', 1360, 330, 300, t, SW(2, 'astronomers') - 0.2, { caption: 'Jacques Cassini', rot: 0.04 });
  if (t > SW(2, 'lemon') - 0.2) { blob(g, 1360, 640, 190, 250, '#f2d64a', 5); tag(g, 1360, 940, 'the Cassinis: a lemon', t, SW(2, 'lemon') - 0.1, 1e9, { size: 44, bg: '#fbe2c0' }); }
  tag(g, 960, 120, 'shape exaggerated', t, S._sh.t0 + 0.4, 1e9, { size: 28 });
}, { punch: false });
mapShot('trips2', SW(2, 'seventeen') - 0.3, 'map_today', { focus: [35, -25], z0: 1.6, z1: 1.75, k0: 1, draw: (g, t, S, P) => {
  const a = P(66.5, 24), b = P(-0.2, -78.5); pinDot(g, a[0], a[1], t, SW(2, 'lapland') - 0.1); pinDot(g, b[0], b[1], t, SW(2, 'andes') - 0.1, '#ffd25a');
  tag(g, a[0], a[1] + 90, 'Lapland · 1736–37', t, SW(2, 'lapland'), 1e9, { size: 40, bg: '#fbe2c0' }); tag(g, b[0] + 60, b[1] - 110, 'the Andes (now Ecuador) · 1735–44', t, SW(2, 'andes'), 1e9, { size: 40 });
  tag(g, 960, 120, 'the French Academy of Sciences sends two expeditions', t, SW(2, 'academy'), 1e9, { size: 36 });
} });
board('degree2', SW(2, 'measured') - 0.3, 'sage', 231, (g, t, S) => {
  const cx = 640, cy = 540, a = 380, b = 300, E2 = th => [cx + a * Math.cos(th), cy - b * Math.sin(th)];
  const ell = []; for (let i = 0; i <= 100; i++) ell.push(E2(i / 100 * TAU)); ink(g, ell, 5, { col: PAL.ink, t0: 0.02, t1: 0.02 });
  const arcAt = (th0, col, tIn) => { if (t < tIn) return; const p = []; for (let i = 0; i <= 20; i++) p.push(E2(th0 + (i / 20 - 0.5) * 0.28)); ink(g, p, 14, { col, t0: 0.05, t1: 0.05 }); };
  arcAt(1.3, PAL.red, S._sh.t0 + 0.4); arcAt(0.05, PAL.blue, S._sh.t0 + 0.8);
  tag(g, 1380, 300, 'one degree in the far north: LONGER', t, SW(2, 'longer') - 0.2, 1e9, { size: 34, bg: '#fbe2c0' }); tag(g, 1380, 560, 'one degree near the equator: shorter', t, S._sh.t0 + 0.8, 1e9, { size: 32 });
  stamp(g, 1380, 820, 'NEWTON WAS RIGHT', t, SW(2, 'right'), 1e9, { size: 54 }); tag(g, 640, 960, 'flattening exaggerated', t, S._sh.t0 + 0.4, 1e9, { size: 26 });
});
board('voltaire2', SW(2, 'voltaire') - 0.3, 'warm', 241, (g, t, S) => {
  print(g, 'voltaire', 560, 480, 560, t, S._sh.t0 + 0.05, { caption: 'Voltaire' }); print(g, 'maupertuis', 1360, 480, 560, t, SW(2, 'leader'), { caption: 'Pierre-Louis Maupertuis', rot: 0.04 });
  tag(g, 960, 950, '“flattened both the Earth and the Cassinis”', t, SW(2, 'flattened', 2), 1e9, { size: 44, bg: '#fbe2c0', italic: true });
}, { punch: false });
board('bulge2', SW(2, 'today') - 0.3, 'blue', 251, (g, t, S) => {
  const cx = 620, cy = 540, R = 320, k = E.io2(seg(t, S._sh.t0 + 0.3, S._sh.t0 + 1.8)), ex = 1 + 0.16 * k;
  const c = []; for (let i = 0; i <= 90; i++) { const a = i / 90 * TAU; c.push([cx + Math.cos(a) * R, cy + Math.sin(a) * R]); } ink(g, c, 3, { col: PAL.ink, a: 0.5, t0: 0, t1: 0 });
  blob(g, cx, cy, R * ex, R, '#4f8ad0', 9);
  if (k > 0.5) { arrow(g, cx + R * 1.02, cy, cx + R * ex - 6, cy, 6, PAL.red); }
  tag(g, 1420, 380, 'equator: 21 km farther\nfrom the centre than the poles', t, SW(2, 'twentyone'), 1e9, { size: 40, bg: '#fbe2c0' });
  tag(g, 1420, 640, '6,378 km vs 6,357 km', t, SW(2, 'twentyone') + 0.6, 1e9, { size: 40 }); tag(g, cx, 960, 'bulge exaggerated ×160', t, S._sh.t0 + 0.4, 1e9, { size: 28 });
}, { punch: false });
pshot('chimbo2', SW(2, 'chimborazo') - 0.3, 'chimborazo', { tags: [[960, 150, 'Chimborazo, Ecuador · 6,263 m', SW(2, 'chimborazo'), { size: 42 }], [960, 860, 'the farthest point from Earth’s centre: 6,384.4 km', SW(2, 'farthest'), { size: 42, bg: '#fbe2c0' }], [960, 980, 'not Everest (6,382.3 km)', SW(2, 'everest'), { size: 34 }]] });
kinShot('drown2', SW(2, 'remember') - 0.3, [['REMEMBER THE BULGE', '#ffffff']], { bg: '#071226', size: 120, y: 420, draw: (g, t, S) => kin(g, [["IT'S WHY LONDON COULD DROWN", '#7fd0ff']], t, SW(2, 'reason') - 0.2, { size: 84, y: 640 }) });

// ================= 10. It is stopping =================
kinShot('twist10', SB(10) - 0.2, [['THE EARTH IS STOPPING.', '#ffffff']], { bg: '#08080b', size: 130, y: 420, draw: (g, t, S) => kin(g, [['RIGHT NOW. SLOWLY.', '#ff6a55']], t, SW(10, 'right') - 0.1, { size: 110, y: 640 }) });
pshot('brake10', SW(10, 'brake') - 0.3, 'moon', { fit: true, from: [0.5, 0.5, 0.9], to: [0.5, 0.5, 1.0], draw: (g, t, S) => kin(g, [['THE BRAKE: THE MOON', '#ffffff']], t, SW(10, 'brake') - 0.1, { size: 100, y: 960 }) });
board('bulge10', SW(10, 'gravity') - 0.3, 'blue', 1001, (g, t, S) => {
  const cx = 640, cy = 540, lag = 0.32 * seg(t, SW(10, 'friction') - 0.3, SW(10, 'friction') + 0.8), mx = 1560, my = 540;
  g.save(); g.translate(cx, cy); g.rotate(-lag); const b = []; for (let i = 0; i < 60; i++) { const q = i / 60 * TAU; b.push([Math.cos(q) * 300, Math.sin(q) * 215]); } cels(g, b, { base: '#6fa8e0', sh: '#3f78b0', sw: 8, L: LIGHT, line: PAL.ink, lw: 2.5, seed: 4 }); g.restore();
  particle(g, cx, cy, 190, '#7a9a5a', { glow: false }); particle(g, mx, my, 70, '#c8c8c0', { glow: false });
  const spin = []; for (let i = 0; i <= 30; i++) { const q = 0.6 - i / 30 * 1.6; spin.push([cx + Math.cos(q) * 120, cy - Math.sin(q) * 120]); } ink(g, spin, 6, { col: '#ffffff', t0: 0.05, t1: 0.05 }); arrow(g, spin[28][0], spin[28][1], spin[30][0], spin[30][1], 6, '#ffffff');
  tag(g, 640, 110, 'the Earth turns faster than the Moon goes round', t, SW(10, 'underneath') - 0.3, 1e9, { size: 32 });
  tag(g, 640, 960, 'friction drags the bulges slightly ahead', t, SW(10, 'friction') - 0.2, 1e9, { size: 34, bg: '#fbe2c0' });
  if (t > SW(10, 'forward') - 0.3) { arrow(g, mx, my - 90, mx, my - 220, 8, PAL.red); tag(g, mx, my - 290, 'tugs the Moon forward', t, SW(10, 'forward') - 0.3, 1e9, { size: 30 }); }
  if (t > SW(10, 'back') - 0.3) { arrow(g, cx + Math.cos(-lag) * 300 + 20, cy - Math.sin(-lag) * 300 - 40, cx + Math.cos(-lag) * 300 + 20, cy - Math.sin(-lag) * 300 + 90, 8, PAL.red); }
  stamp(g, 1200, 820, 'SPIN SLOWS · MOON DRIFTS OUT', t, SW(10, 'drifts') - 0.6, 1e9, { size: 40 }); tag(g, 1720, 1000, 'not to scale', t, S._sh.t0 + 0.3, 1e9, { size: 24 });
}, { punch: false });
pshot('apollo10', SW(10, 'measure') - 0.3, 'lrrr', { from: [0.5, 0.55, 1.0], to: [0.55, 0.6, 1.2], tags: [[960, 150, 'July 1969 · Apollo 11 (NASA)', SW(10, 'nineteen') - 0.2, { size: 40 }], [960, 880, 'a panel of mirrors, left on the Moon', SW(10, 'mirrors') - 0.3, { size: 44, bg: '#fbe2c0' }]] });
board('more10', SW(10, 'fourteen') - 0.3, 'warm', 1011, (g, t, S) => {
  print(g, 'apollo11', 560, 480, 560, t, S._sh.t0 + 0.05, { caption: 'Apollo 11, 1969', cs: 30 }); print(g, 'lunokhod', 1360, 480, 430, t, SW(10, 'soviet') - 0.2, { caption: 'Lunokhod (Soviet rover) replica', cs: 28, rot: 0.04 });
  tag(g, 960, 950, 'Apollo 14 and 15 left more · the Soviet Lunokhod rovers carry them too', t, SW(10, 'fifteen') - 0.2, 1e9, { size: 32 });
}, { punch: false });
flat('laser10', SW(10, 'observatories') - 0.3, '#05070d', (g, t, S) => {
  starField(g, 300, 4); particle(g, 280, 540, 170, '#4f8ad0', { glow: false }); particle(g, 1660, 540, 60, '#c8c8c0', { glow: false });
  const T0 = SW(10, 'lasers') - 0.2; if (t > T0) { const ph = ((t - T0) / 2.5) % 1, x = ph < 0.5 ? lerp(450, 1600, ph * 2) : lerp(1600, 450, ph * 2 - 1); ink(g, [[450, 530], [1600, 530]], 3, { col: '#3aff7a', a: 0.35, t0: 0, t1: 0 }); g.save(); g.globalCompositeOperation = 'lighter'; glowDot(g, x, 530, 40, '#3aff7a', 1); g.restore(); }
  tag(g, 960, 170, 'fire a laser · time the round trip (≈ 2.5 s)', t, SW(10, 'lasers') - 0.2, 1e9, { size: 38 });
  tag(g, 960, 820, 'the Moon is moving away: ≈ 3.8 cm a year', t, SW(10, 'centimeters') - 0.4, 1e9, { size: 46, bg: '#fbe2c0' });
  tag(g, 960, 960, 'about as fast as your fingernails grow', t, SW(10, 'fingernails') - 0.3, 1e9, { size: 36 });
}, { punch: false });
kinShot('century10', SW(10, 'longer') - 0.4, [['THE DAY GETS LONGER', '#ffffff'], ['+1.8 MILLISECONDS PER CENTURY', '#ffcf5a']], { bg: '#0b0d14', size: 96, stagger: 1.0 });
pshot('babylon10', SW(10, 'ancient') - 0.3, 'babylon', { fit: true, from: [0.5, 0.5, 0.92], to: [0.5, 0.5, 1.02], tags: [[1500, 380, 'eclipse records', SW(10, 'eclipses') - 0.2, { size: 44, bg: '#fbe2c0' }], [1500, 560, 'Babylon · China · Greece\nthe medieval Arab world', SW(10, 'babylon') - 0.2, { size: 34 }]] });
pshot('eclipse10', SW(10, 'always') - 0.3, 'eclipse', { fit: true, from: [0.5, 0.5, 0.95], to: [0.5, 0.5, 1.05], tags: [[960, 960, 'at today’s rate: many would have been seen in a completely different part of the world', SW(10, 'completely') - 0.4, { size: 32, bg: '#fbe2c0' }]] });
dshot('steph10', SW(10, 'stephenson') - 0.5, 'stephenson', { path: [[0, "Measurement of the Earth's rotation: 720 BC to AD 2015"], [SW(10, 'slowdown') - 0.8, 'ms per century']], hl: [["Measurement of the Earth's rotation: 720 BC to AD 2015", SW(10, 'stephenson')], ['ms per century', SW(10, 'slowdown') - 0.3]], src: 'Stephenson, Morrison & Hohenkerk, Proc. R. Soc. A (2016)' });
pshot('coral10', SW(10, 'further') - 0.3, 'heliophyllum', { tags: [[960, 150, 'the clocks are alive', SW(10, 'alive') - 0.2, { size: 46 }], [960, 880, 'a thin growth layer every day · a band every year', SW(10, 'layer') - 0.3, { size: 40, bg: '#fbe2c0' }]] });
board('wells10', SW(10, 'wells') - 0.3, 'sage', 1021, (g, t, S) => {
  const band = (y, n, lab, tIn, col) => { if (t < tIn) return; const u = seg(t, tIn, tIn + 1.2); cels(g, rough([[200, y - 60], [1720, y - 60], [1720, y + 60], [200, y + 60]], 1.5, y, 12), { base: col, line: PAL.ink, lw: 2.5, seed: y }); for (let i = 0; i < n * u; i++) { const x = 210 + (i / n) * 1500; ink(g, [[x, y - 52], [x, y + 52]], 1.4, { col: PAL.ink, a: 0.55, t0: 0, t1: 0, seed: i }); } tag(g, 960, y - 120, lab, t, tIn, 1e9, { size: 36 }); };
  band(380, 365, 'a coral today: ≈ 365 daily lines per yearly band', S._sh.t0 + 0.2, '#efe0c0');
  band(760, 400, 'Devonian coral, ~390 million years ago: ≈ 400 lines (John Wells, 1963)', SW(10, 'found') - 0.3, '#e8c8a0');
  stamp(g, 1450, 960, 'DAYS ≈ 22 HOURS', t, SW(10, 'twentytwo') - 0.2, 1e9, { size: 46 });
}, { punch: false });
docOr('agu10', SW(10, 'winter') - 0.6, 'agu', { path: [[0, 'Ancient shell shows days were half-hour shorter 70 million years ago'], [SW(10, 'seventytwo') - 0.5, '372']], hl: [['Ancient shell shows days were half-hour shorter 70 million years ago', SW(10, 'winter')], ['372', SW(10, 'seventytwo')]], src: 'AGU press release, 9 March 2020 (de Winter et al.)',
  tags: [[960, 980, 'the last dinosaurs: days of ≈ 23.5 hours', SW(10, 'dinosaurs') - 0.2, { size: 38, bg: '#fbe2c0' }]] }, 'Ancient shell shows days were half-hour shorter 70 million years ago', 'AGU, 9 March 2020');
docOr('meyers10', SW(10, 'rocks') - 0.3, 'meyers', { path: [[0, 'Proterozoic Milankovitch cycles and the history of the solar system'], [SW(10, 'answer') - 0.4, '18.68 ± 0.25 hours']], hl: [['Proterozoic Milankovitch cycles and the history of the solar system', SW(10, 'meyers') - 0.2], ['18.68 ± 0.25 hours', SW(10, 'eighteen') - 0.2], ['340,900 ± 2,600 km', SW(10, 'closer') - 0.4]], src: 'Meyers & Malinverno, PNAS (2018)' },
  'length of day of 18.68 ± 0.25 hours … an Earth–Moon distance of 340,900 ± 2,600 km', 'Meyers & Malinverno, PNAS 2018');
board('lodtime10', SW(10, 'braking') - 0.3, 'warm', 1031, (g, t, S) => {
  const M = axes(g, 240, 880, 1400, 620, { x0: 1500, x1: 0, y0: 18, y1: 24.5, xl: 'millions of years ago', yl: 'hours in a day' }), pts = [[1400, 18.68, 'rocks, China (2018)'], [390, 21.9, 'Devonian corals (1963)'], [70, 23.5, 'rudist clam (2020)'], [0, 24, 'today']];
  const u = seg(t, S._sh.t0 + 0.2, S._sh.t0 + 1.8); const line = pts.slice(0, Math.max(1, Math.ceil(u * pts.length))).map(p => M(p[0], p[1])); if (line.length > 1) ink(g, line, 6, { col: PAL.red, t0: 0.02, t1: 0.02 });
  pts.forEach((p, i) => { const q = M(p[0], p[1]), ti = S._sh.t0 + 0.2 + i * 0.4; pinDot(g, q[0], q[1], t, ti); tag(g, q[0] + [60, 60, -220, -40][i], q[1] + [-70, 70, -80, 70][i], `${p[1]} h · ${p[2]}`, t, ti + 0.1, 1e9, { size: 28 }); });
  for (const v of [1500, 1000, 500, 0]) { const p = M(v, 18); g.font = '500 26px Georgia'; g.fillStyle = css(PAL.ink); g.textAlign = 'center'; g.fillText(String(v), p[0], p[1] + 36); }
  stamp(g, 960, 130, 'BRAKING FOR BILLIONS OF YEARS', t, SW(10, 'billions') - 0.3, 1e9, { size: 44 });
}, { punch: false });

// ================= 11. Faster, lately =================
kinShot('lately11', SB(11) - 0.2, [['EXCEPT, LATELY …', '#7fd0ff']], { bg: '#0b0d14', size: 140 });
board('iers11', SW(11, 'since') - 0.3, 'blue', 1101, (g, t, S) => {
  print(g, 'atomicclock', 1440, 520, 560, t, SW(11, 'atomic') - 0.3, { caption: 'NIST-F1 caesium clock', cs: 28, rot: 0.03 });
  tag(g, 620, 320, 'since 1962: the length of every single day', t, S._sh.t0 + 0.1, 1e9, { size: 40 });
  tag(g, 620, 500, 'IERS: International Earth Rotation\nand Reference Systems Service', t, SW(11, 'international') - 0.2, 1e9, { size: 36, bg: '#fbe2c0' });
  tag(g, 620, 720, 'atomic clocks · radio telescopes · lasers', t, SW(11, 'atomic') - 0.2, 1e9, { size: 34 });
}, { punch: false });
function lodPlot(g, x, y, w, h, t, tIn, o = {}) { // the real IERS record (LOD excess over 86,400 s, ms)
  const X0 = o.x0 ?? 1962, X1 = o.x1 ?? 2027, Y0 = -2, Y1 = 4.6, M = (a, b) => [x + (a - X0) / (X1 - X0) * w, y + h - (b - Y0) / (Y1 - Y0) * h];
  g.fillStyle = 'rgba(255,255,255,0.04)'; g.fillRect(x, y, w, h); const z = M(X0, 0), z1 = M(X1, 0); ink(g, [z, z1], 2, { col: '#ffffff', a: 0.4, t0: 0, t1: 0 });
  g.font = '500 24px Georgia'; g.fillStyle = 'rgba(255,255,255,0.75)'; g.textAlign = 'right'; g.textBaseline = 'middle'; for (const v of [-1, 0, 1, 2, 3, 4]) { const p = M(X0, v); g.fillText((v > 0 ? '+' : '') + v + ' ms', p[0] - 12, p[1]); }
  g.textAlign = 'center'; for (let yr = Math.ceil(X0 / 10) * 10; yr <= X1; yr += (X1 - X0 > 20 ? 10 : 1)) { const p = M(yr, Y0); g.fillText(String(yr), p[0], p[1] + 28); }
  const u = E.io2(seg(t, tIn, tIn + (o.dur ?? 3))), xEnd = lerp(X0, X1, u);
  g.beginPath(); let first = true; for (let i = 0; i < LOD.t.length; i++) { const a = LOD.t[i]; if (a < X0) continue; if (a > xEnd) break; const p = M(a, LOD.v[i]); if (first) { g.moveTo(p[0], p[1]); first = false; } else g.lineTo(p[0], p[1]); }
  g.strokeStyle = o.col || '#7fd0ff'; g.lineWidth = o.lw ?? 1.6; g.stroke(); return M;
}
flat('lod11', SW(11, "here's") - 0.3, '#070a12', (g, t, S) => {
  const M = lodPlot(g, 220, 160, 1560, 760, t, S._sh.t0 + 0.2, { dur: 3.5 });
  tag(g, 960, 90, 'IERS: how much longer than 86,400 s each day was (ms)', t, S._sh.t0, 1e9, { size: 32 });
  const pin = (yr, v, lab, tIn, dy, dx = 0) => { if (t < tIn) return; const p = M(yr, v); pinDot(g, p[0], p[1], t, tIn, '#ffd25a'); tag(g, p[0] + dx, p[1] + dy, lab, t, tIn + 0.1, 1e9, { size: 32, bg: '#fbe2c0' }); };
  pin(1972.28, 4.355, 'early 1970s: up to 4 ms long', SW(11, 'seventies') - 0.2, 70); pin(2024.51, -1.651, '5 July 2024: −1.65 ms · the shortest day in the record', SW(11, 'fifth') - 0.2, -90, -380);
  credit(g, 'data: IERS EOP 20 C04 (Paris Observatory), 1962 – Oct 2026');
}, { punch: false });
board('why11', SW(11, 'why') - 0.3, 'sage', 1111, (g, t, S) => {
  const items = [['the rock', 480, 330], ['the oceans', 1440, 330], ['the atmosphere', 480, 760], ['the liquid core', 1440, 760]];
  items.forEach(([n, x, y], i) => tag(g, x, y, n, t, SW(11, 'shared') + i * 0.35, 1e9, { size: 46, bg: i % 2 ? '#d9f0ff' : '#fbe2c0' }));
  if (t > SW(11, 'trade') - 0.2) { const k = seg(t, SW(11, 'trade') - 0.2, SW(11, 'trade') + 0.6); arrow(g, 700, 330, 1220, 330, 6, PAL.red, k); arrow(g, 1220, 760, 700, 760, 6, PAL.red, k); arrow(g, 480, 420, 480, 660, 6, PAL.red, k); arrow(g, 1440, 660, 1440, 420, 6, PAL.red, k); }
  tag(g, 960, 545, 'one spin, shared · traded back and forth', t, SW(11, 'trade') - 0.2, 1e9, { size: 36 });
  tag(g, 960, 120, 'the Earth isn’t one solid ball', t, SW(11, 'solid') - 0.3, 1e9, { size: 40 });
}, { punch: false });
board('skater11', SW(11, 'melting') - 0.3, 'warm', 1121, (g, t, S) => {
  const fig = (cx, arms, w, lab, tIn) => { if (t < tIn) return; const a = (t - tIn) * w; ink(g, [[cx, 330], [cx, 640]], 10, { col: PAL.ink, t0: 0.05, t1: 0.05 }); particle(g, cx, 290, 40, '#e0b090', { glow: false }); const L = arms; ink(g, [[cx - L * Math.cos(a), 430 + 12 * Math.sin(a)], [cx + L * Math.cos(a), 430 - 12 * Math.sin(a)]], 9, { col: PAL.ink, t0: 0.05, t1: 0.05 }); const el = []; for (let i = 0; i <= 40; i++) { const q = i / 40 * TAU; el.push([cx + Math.cos(q) * (L + 40), 700 + Math.sin(q) * 30]); } ink(g, el, 3, { col: PAL.blue, a: 0.6, t0: 0, t1: 0 }); tag(g, cx, 840, lab, t, tIn, 1e9, { size: 34 }); };
  fig(520, 60, 6, 'arms in: spins fast', S._sh.t0 + 0.2); fig(1400, 230, 2.5, 'arms out: spins slower', SW(11, 'skater') - 0.3);
  tag(g, 960, 130, 'melting ice moves water from the poles toward the equator', t, SW(11, 'melting') - 0.1, 1e9, { size: 36, bg: '#fbe2c0' });
}, { punch: false });
qshot('quake11', SW(11, 'earthquakes') - 0.3, 'The 2011 earthquake in Japan shortened the day by about 1.4 millionths of a second.', 'Richard Gross, NASA JPL (revised estimate, 2012)', { size: 50 });
flat('leap11', SW(11, 'timekeepers') - 0.3, '#05070c', (g, t, S) => {
  g.font = '700 220px Consolas, monospace'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillStyle = '#ff5a3c'; g.fillText('23:59:60', 960, 440);
  tag(g, 960, 720, 'leap seconds added since 1972: ' + rollNum(t, SW(11, 'leap') - 0.6, SW(11, 'leap') + 0.4, 0, 27), t, SW(11, 'twentyseven') - 0.5, 1e9, { size: 52, bg: '#fbe2c0' });
  tag(g, 960, 900, 'so clocks wait for the slowing Earth', t, SW(11, 'catch') - 0.3, 1e9, { size: 36 });
}, { punch: false });
flat('neg11', SW(11, 'negative') - 0.4, '#05070c', (g, t, S) => {
  const k = Math.floor(Math.max(0, t - S._sh.t0 - 0.4) / 0.7) % 3, txt = ['23:59:58', '00:00:00', '00:00:01'][k];
  g.font = '700 220px Consolas, monospace'; g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillStyle = '#7fd0ff'; g.fillText(txt, 960, 440);
  tag(g, 960, 720, 'a negative leap second: 23:59:59 never happens', t, S._sh.t0 + 0.3, 1e9, { size: 44, bg: '#fbe2c0' });
  tag(g, 960, 880, 'a minute with only 59 seconds · it has never been done', t, SW(11, 'fiftynine') - 0.3, 1e9, { size: 36 });
}, { punch: false });
dshot('npr11', SW(11, 'agnew') - 0.6, 'npr', { path: [[0, 'negative leap second']], hl: [['negative leap second', SW(11, 'agnew')]], src: 'NPR, 30 March 2024, on Agnew, Nature 628, 333 (2024)',
  tags: [[960, 960, 'melting ice pushed it back to ≈ 2029', SW(11, 'melting', 2) - 0.2, { size: 40, bg: '#fbe2c0' }]] });
kinShot('abolish11', SW(11, 'decided') - 0.4, [['LEAP SECONDS:', '#ffffff'], ['STOPPED BY 2035', '#ffcf5a']], { bg: '#0b0d14', size: 120, stagger: 0.5 });
flat('zoom11', SW(11, 'fading') - 0.4, '#070a12', (g, t, S) => {
  const M = lodPlot(g, 220, 160, 1560, 720, t, S._sh.t0 - 5, { x0: 2019, x1: 2026.95, lw: 2.4 });
  const pin = (yr, v, lab, tIn, dy) => { if (t < tIn) return; const p = M(yr, v); pinDot(g, p[0], p[1], t, tIn, '#ffd25a'); tag(g, p[0], p[1] + dy, lab, t, tIn + 0.1, 1e9, { size: 30, bg: '#fbe2c0' }); };
  pin(2024.51, -1.651, '2024: −1.65', S._sh.t0 + 0.3, 80); pin(2025.52, -1.368, '2025: −1.37', S._sh.t0 + 0.6, 80); pin(2026.57, -0.67, '27 July 2026: −0.67 ms', SW(11, 'twentyseventh') - 0.3, -100);
  tag(g, 960, 960, 'shortest day of each year, getting less short', t, SW(11, 'slowing', 2) - 0.6, 1e9, { size: 36 });
  credit(g, 'data: IERS EOP 20 C04 · 2026 to 26 October');
}, { punch: false });
dshot('tad11', SW(11, 'slowing', 2) - 0.4, 'timeanddate', { path: [[0, 'Earth Is Slowing Down Again']], hl: [['Earth Is Slowing Down Again', SW(11, 'slowing', 2)]], src: 'timeanddate.com, July 2026' });
flat('core11', SW(11, 'center') - 0.3, '#07090f', (g, t, S) => {
  earthCut(g, 640, 560, 400, t); const T0 = S._sh.t0;
  const arc = (r, a0, len, col, w) => { const p = []; for (let i = 0; i <= 30; i++) { const q = a0 - i / 30 * len; p.push([640 + Math.cos(q) * r, 560 + Math.sin(q) * r]); } ink(g, p, w, { col, t0: 0.05, t1: 0.05 }); arrow(g, p[28][0], p[28][1], p[30][0], p[30][1], w, col); };
  if (t > SW(11, 'slower') - 0.4) { arc(300, -0.3 - (t - T0) * 0.4, 1.6, '#ffffff', 7); arc(110, -0.3 - (t - T0) * 0.32, 1.4, '#ffd25a', 7); }
  if (t > SW(11, 'backwards') - 0.3) { const p = []; for (let i = 0; i <= 20; i++) { const q = 1.2 + i / 20 * 0.9; p.push([640 + Math.cos(q) * 150, 560 + Math.sin(q) * 150]); } ink(g, p, 9, { col: '#ff5a3c', t0: 0.05, t1: 0.05 }); arrow(g, p[18][0], p[18][1], p[20][0], p[20][1], 9, '#ff5a3c'); }
  tag(g, 1420, 240, 'Wang, Vidale et al. · Nature · 2024', t, SW(11, 'wang') - 0.3, 1e9, { size: 32 }); tag(g, 1420, 400, 'repeating earthquakes, decades apart', t, SW(11, 'repeated') - 0.3, 1e9, { size: 32 });
  tag(g, 1420, 580, 'since ~2010: the inner core (⅔ the Moon’s size)\nturns slightly slower than the rest', t, SW(11, 'slower') - 0.4, 1e9, { size: 30, bg: '#fbe2c0' }); tag(g, 1420, 790, 'seen from the surface: drifting backwards', t, SW(11, 'backwards') - 0.3, 1e9, { size: 32, bg: '#f6d0c4' });
}, { punch: false });
dshot('usc11', SW(11, 'part') - 0.3, 'usc', { path: [[0, 'inner core has slowed']], hl: [['inner core has slowed', SW(11, 'part') + 0.3]], src: 'USC News, June 2024',
  draw: (g, t, S) => stamp(g, 1450, 880, 'STILL DEBATED', t, SW(11, 'debated') - 0.2, 1e9, { size: 52 }) });
flat('nested11', SW(11, 'several') - 0.4, '#07090f', (g, t, S) => {
  const T0 = S._sh.t0; [[420, '#5f7fa8', 0.5], [340, '#b0562c', 0.47], [230, '#f0a030', 0.53], [80, '#fff1b8', 0.44]].forEach(([r, col, w], i) => { const p = []; for (let k = 0; k < 90; k++) { const q = k / 90 * TAU; p.push([960 + Math.cos(q) * r, 560 + Math.sin(q) * r]); } inkLoop(g, p, 8, { col, seed: i }); const a = -(t - T0) * w; for (let k = 0; k < 3; k++) { const q = a + k * TAU / 3; glowDot(g, 960 + Math.cos(q) * r, 560 + Math.sin(q) * r, 22, col, 1); } });
  kin(g, [['SEVERAL SPINS', '#ffffff']], t, SW(11, 'several') - 0.1, { size: 100, y: 120 }); kin(g, [['NOT QUITE IN STEP', '#ffcf5a']], t, SW(11, 'step') - 0.4, { size: 90, y: 1000 });
}, { punch: false });

// ================= 12. Close =================
kinShot('ever12', SB(12) - 0.2, [['WILL THE EARTH EVER STOP?', '#ffffff']], { bg: '#08080b', size: 110 });
board('lock12', SW(12, 'brake') - 0.3, 'blue', 1201, (g, t, S) => {
  const cx = 700, cy = 540, a = (t - S._sh.t0) * 0.4, mx = cx + Math.cos(a) * 520, my = cy - Math.sin(a) * 330;
  particle(g, cx, cy, 170, '#4f8ad0', { glow: false }); particle(g, mx, my, 60, '#c8c8c0', { glow: false });
  const k = seg(t, SW(12, 'same') - 0.3, SW(12, 'same') + 0.3); if (k > 0) { const ex = cx + Math.cos(a) * 170, ey = cy - Math.sin(a) * 170; g.save(); g.globalAlpha = k; glowDot(g, ex, ey, 30, '#ffd25a', 1); ink(g, [[ex, ey], [lerp(ex, mx, 0.3), lerp(ey, my, 0.3)]], 4, { col: '#ffd25a', t0: 0.05, t1: 0.05 }); g.restore(); }
  tag(g, 1480, 330, 'the brake works until\nthe Earth’s day = the Moon’s month', t, SW(12, 'matches') - 0.4, 1e9, { size: 34, bg: '#fbe2c0' });
  tag(g, 1480, 620, 'then one side always faces the Moon,\nas the Moon already does to us', t, SW(12, 'face') - 0.6, 1e9, { size: 32 });
}, { punch: false });
board('timeline12', SW(12, 'calculations') - 0.3, 'warm', 1211, (g, t, S) => {
  ribbon(g, 200, 1720, 560, t, S._sh.t0 + 0.1);
  pin(g, 240, 560, 'today', t, S._sh.t0 + 0.3, { size: 32 });
  pin(g, 760, 560, '≈ 5 billion years:\nthe Sun swells into a red giant', t, SW(12, 'five') - 0.3, { size: 30, col: '#ff7a3a', up: 90 });
  pin(g, 1660, 560, 'tens of billions of years:\nday = month (tidal lock)', t, SW(12, 'tens') - 0.3, { size: 30, col: PAL.blue, up: 90 });
  stamp(g, 1200, 860, 'THE SUN GETS THERE FIRST', t, SW(12, 'giant') - 0.2, 1e9, { size: 44 });
});
pshot('mercury12', SW(12, 'closer') - 0.3, 'mercury', { fit: true, from: [0.5, 0.5, 0.9], to: [0.5, 0.5, 1.0], tags: [[1500, 330, 'Mercury: 3 turns for every 2 orbits', SW(12, 'three') - 0.3, { size: 38, bg: '#fbe2c0' }], [1500, 520, 'one day, sunrise to sunrise\n= two of its years', SW(12, 'sunrise') - 0.2, { size: 34 }], [420, 860, 'Venus barely turns at all', SW(12, 'venus') - 0.2, { size: 36 }]] });
kinShot('wont12', SW(12, 'anyone') - 1.2, [["THE EARTH WON'T STOP.", '#ffffff']], { bg: '#06070b', size: 110, y: 420, draw: (g, t, S) => kin(g, [['NOT WHILE ANYONE IS HERE TO NOTICE', '#7fd0ff']], t, SW(12, 'anyone') - 0.4, { size: 70, y: 640 }) });
board('recap12', SW(12, 'doing') - 1.2, 'sage', 1221, (g, t, S) => {
  title(g, 960, 150, 'what the turning is doing', t, S._sh.t0, 1e9, 60, PAL.ink, { shadow: false });
  checklist(g, 340, 320, [['lifts the sea at the equator', '✓', SW(12, 'lifts') - 0.2], ['twists every hurricane', '✓', SW(12, 'twists') - 0.2], ['lines up the core into our magnetic shield', '✓', SW(12, 'lines') - 0.2], ['slowing ~1.8 ms a century: the Moon takes it, one tide at a time', '✓', SW(12, 'slowing') - 0.2]], t, { size: 38, dy: 150 });
}, { punch: false });
board('elevens12', SW(12, 'last') - 0.3, 'warm', 1231, (g, t, S) => {
  kin(g, [['THE TWO ELEVENS', '#b8361f']], t, S._sh.t0 + 0.1, { size: 90, y: 140, stroke: 'rgba(255,255,255,0.9)' });
  writeText(g, 'v² ÷ 2g', 520, 380, 96, t, SW(12, 'climb') - 1.0, 0.6, { upright: true, align: 'center', col: '#2f5f8f' });
  tag(g, 520, 520, 'equator-speed water could climb 11.0 km', t, SW(12, 'climb') - 0.3, 1e9, { size: 32 });
  writeText(g, 'ω² R² ÷ 2g', 1400, 380, 96, t, SW(12, 'shift') - 1.0, 0.6, { upright: true, align: 'center', col: '#b8361f' });
  tag(g, 1400, 520, 'the sea’s equator-to-pole shift: 11.0 km', t, SW(12, 'shift') - 0.3, 1e9, { size: 32 });
  writeText(g, 'v = ω R', 960, 700, 110, t, SW(12, 'coincidence') - 0.3, 0.6, { upright: true, align: 'center' });
  stamp(g, 960, 900, 'SAME NUMBER · SAME ENERGY', t, SW(12, 'energy') - 0.3, 1e9, { size: 56 });
}, { punch: false });
glShot('sea12', SW(12, 'motion') - 0.3, T => glMorph(T, { m0: SW(12, 'motion'), m1: SW(12, 'motion') + 3, lon0: 1.2, el: (lt) => 0.3 * Math.sin(lt * 0.3) }), { over: (g, t, S) => {
  kin(g, [['THE SPIN IS STORED', '#ffffff'], ['IN THE SHAPE OF THE SEA', '#7fd0ff']], t, S._sh.t0 + 0.3, { size: 80, y: 880, stagger: 0.5 }); credit(g, 'our calculation: NOAA ETOPO1 · today → no spin');
} });
clipShot('coast12', SW(12, 'coastline') - 0.3, 'nightlights', { draw: (g, t, S) => kin(g, [['EVERY COASTLINE ON EARTH', '#ffffff'], ['DRAWN BY A PLANET TURNING', '#ffcf5a'], ['AT A THOUSAND MILES AN HOUR', '#ffcf5a']], t, S._sh.t0 + 0.2, { size: 80, stagger: 0.8 }) });
pshot('paris12', SW(12, 'paris') - 0.3, 'pendulum', { from: [0.5, 0.6, 1.0], to: [0.5, 0.72, 1.3], tags: [[960, 150, 'the Panthéon, Paris · the pendulum still swings', SW(12, 'pendulum') - 0.2, { size: 38 }]] });
kinShot('end12', SW(12, 'turning', 3) - 0.9, [["IT ISN'T TURNING.", '#ffffff']], { bg: '#04050a', size: 130, y: 430, draw: (g, t, S) => kin(g, [['WE ARE.', '#ffcf5a']], t, SW(12, 'we') - 0.15, { size: 170, y: 640 }) });

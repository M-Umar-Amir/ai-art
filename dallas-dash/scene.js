/**
 * Dallas Dash — the renderer.
 *
 * A small software 3D renderer on Canvas 2D: real 3D geometry, flat-shaded,
 * painter-sorted. No WebGL and no downloaded models — every van, bucket and
 * skyscraper below is code. The one image is the KFC logo, drawn onto signs.
 *
 * Porting note (Expo / React Native): the maths (makeCamera, proj) is pure and
 * every draw call goes through `ctx`, so a port swaps the output target for
 * react-native-skia (which mirrors this canvas API) and keeps the builders.
 */

import { HAZ, JET_HEIGHT } from "./sim.js";
import { ROUTES, routeById, districtAt as routeDistrictAt } from "./routes.js";

const ROAD_HALF = 4.6;
const LANE_X = [-2.3, 0, 2.3];

// KFC brand colours: the red is the brand's #E4002B
const RED = [228, 0, 43];
const RED_DEEP = [164, 0, 32];
const WHITE = [255, 255, 255];
const INK = [32, 33, 36];
const CREAM = [255, 246, 236];
const CRISP = [206, 128, 44]; // fried-chicken gold
const CRISP_DARK = [158, 88, 28];
const BUN = [226, 168, 96];
const LETTUCE = [116, 190, 98];
const GRAVY = [140, 92, 54];
const GLASS = [88, 128, 170];
const DARK_GLASS = [52, 72, 102];
const STONE = [186, 112, 92];
const GREEN = [98, 200, 120];
const GOLD = [255, 196, 60];

// the current route (set per frame by draw); its sky is Texas golden hour,
// a Deep Ellum dusk or a crisp Toronto autumn afternoon
let R = ROUTES[0];
let SKY = R.sky;

export const CAM = {
  h: 3.1,
  back: 6.4,
  lookY: 0.8,
  lookAhead: 9,
  cy: 0.6,
  focal: 1.38,
};

/* ── neighbourhoods: each route's districts mix their own kind of street ── */

export { DISTRICT_LEN } from "./routes.js";
export function districtAt(distance, routeId) {
  return routeDistrictAt(distance, routeId);
}

const SEG = 13; // one city block per side per SEG units of road
const FACE_X = 9.0; // building fronts sit at the back of the sidewalk
const MURAL_BG = ["#1b6fd1", "#f2a20c", "#16a37f", "#7a3fc4", "#e4002b", "#0b9bb5"];
const CANTINA = [[64, 196, 196], [246, 132, 156], [250, 204, 72], [120, 200, 120], [250, 160, 80]];
const BRICK = [[170, 72, 52], [150, 62, 48], [184, 96, 64], [128, 58, 50]];
const LOFT = [[214, 186, 146], [196, 170, 136], [222, 204, 172]];
const TOWER = [[70, 112, 168], [56, 96, 140], [92, 140, 186], [64, 120, 150]];
const ROWHOUSE = [[196, 84, 70], [92, 140, 176], [236, 196, 92], [132, 176, 120], [214, 132, 168], [240, 236, 226]];
const WAREHOUSE = [[132, 140, 148], [168, 156, 134], [112, 122, 120], [182, 120, 86]];
const DECO = [[236, 222, 196], [226, 206, 176], [214, 214, 206]];
const AD_LINES = ["FINGER LICKIN' GOOD", "ORIGINAL RECIPE", "GRAB A BUCKET", "FAMOUS BOWLS"];

/** A stable 0..1 value for an integer: the street is the same every visit. */
function hash(n) {
  let x = Math.imul((n | 0) ^ 0x9e3779b9, 0x85ebca6b);
  x ^= x >>> 13;
  x = Math.imul(x, 0xc2b2ae35);
  x ^= x >>> 16;
  return (x >>> 0) / 4294967296;
}

function pickMix(mix, r) {
  let total = 0;
  for (const k in mix) total += mix[k];
  let v = r * total;
  for (const k in mix) {
    v -= mix[k];
    if (v <= 0) return k;
  }
  return "brick";
}

/**
 * One block of street. Brand placements are decided here, between ordinary
 * buildings: rooftop billboards and plywood
 * construction hoardings papered with KFC posters. Nothing on the course
 * itself (obstacles, containers, vans) carries a brand.
 */
function blockSpec(i, side) {
  const r = hash(i * 2 + (side > 0 ? 1 : 0));
  const r2 = hash(i * 7 + (side > 0 ? 101 : 3));
  const r3 = hash(i * 13 + (side > 0 ? 57 : 29));
  const r4 = hash(i * 19 + (side > 0 ? 7 : 211));
  // KFC restaurants themselves come from the course (sim "store" objects, so
  // the "pass N KFC restaurants" mission counts what you see); blocks add the
  // ads: hoardings here, rooftop billboards below
  if (i > 1 && r4 > 0.93) return { style: "lot", len: SEG - 0.35, h: 0, r2, r3, ad: Math.floor(r2 * 97) % AD_LINES.length };
  const style = pickMix(routeDistrictAt(i * SEG, R.id).mix, r);
  const alley = r2 < 0.22 ? 3 : 0.35;
  const spec = { style, len: SEG - alley, r2, r3 };
  const pickOf = (list, v) => list[Math.floor(v * 991) % list.length];
  if (style === "brick") {
    spec.h = 7 + Math.floor(r3 * 3) * 1.6;
    spec.color = BRICK[Math.floor(r2 * BRICK.length)];
    if (r3 < 0.6) spec.mural = { text: pickOf(R.murals, r2), bg: MURAL_BG[Math.floor(r3 * 991) % MURAL_BG.length] };
  } else if (style === "loft") {
    spec.h = 10 + Math.floor(r3 * 3) * 2;
    spec.color = LOFT[Math.floor(r2 * LOFT.length)];
    spec.ghost = pickOf(R.ghosts, r3);
  } else if (style === "cantina") {
    spec.h = 4.6 + r3 * 1.4;
    spec.color = CANTINA[Math.floor(r2 * CANTINA.length)];
    spec.sign = pickOf(R.shops, r3);
  } else if (style === "rowhouse") {
    // Toronto bay-and-gable Victorians, painted bright in Kensington
    spec.h = 7.4 + r3 * 1.2;
    spec.color = ROWHOUSE[Math.floor(r2 * ROWHOUSE.length)];
    spec.sign = r3 < 0.5 ? pickOf(R.shops, r2) : null;
  } else if (style === "warehouse") {
    spec.h = 6 + Math.floor(r3 * 2) * 1.5;
    spec.color = WAREHOUSE[Math.floor(r2 * WAREHOUSE.length)];
    if (r3 < 0.5) spec.mural = { text: pickOf(R.murals, r2), bg: MURAL_BG[Math.floor(r3 * 991) % MURAL_BG.length] };
  } else if (style === "deco") {
    // Fair Park-style art deco: pale stone, a stepped centre and gold trim
    spec.h = 8 + r3 * 2;
    spec.color = DECO[Math.floor(r2 * DECO.length)];
    spec.ghost = pickOf(R.ghosts, r3);
  } else {
    spec.h = 20 + Math.floor(r3 * 4) * 5;
    spec.color = TOWER[Math.floor(r2 * TOWER.length)];
    spec.pegasus = R.skyline === "dallas" && r3 > 0.8; // the red neon Pegasus, Dallas's rooftop mascot
  }
  spec.water = (style === "brick" || style === "loft" || style === "warehouse") && r3 > 0.72; // a rooftop water tower
  // a KFC billboard on some low and mid-rise roofs
  spec.billboard = !spec.water && spec.h < 13 && style !== "cantina" && r4 > 0.74 && r4 < 0.86 ? Math.floor(r3 * 97) % AD_LINES.length : -1;
  return spec;
}

function rgb(c, mul) {
  const f = mul === undefined ? 1 : mul;
  return `rgb(${Math.min(255, Math.round(c[0] * f))},${Math.min(255, Math.round(c[1] * f))},${Math.min(255, Math.round(c[2] * f))})`;
}

function fogMix(c, depth) {
  const t = Math.max(0, Math.min(0.8, (depth - 60) / 190));
  return [
    c[0] + (SKY.haze[0] - c[0]) * t,
    c[1] + (SKY.haze[1] - c[1]) * t,
    c[2] + (SKY.haze[2] - c[2]) * t,
  ];
}

/** An orthonormal camera basis. Pure maths. */
export function makeCamera(px, py, pz, tx, ty, tz) {
  let fx = tx - px;
  let fy = ty - py;
  let fz = tz - pz;
  const fl = Math.hypot(fx, fy, fz) || 1;
  fx /= fl;
  fy /= fl;
  fz /= fl;
  let rx = -fz;
  let ry = 0;
  let rz = fx;
  const rl = Math.hypot(rx, ry, rz) || 1;
  rx /= rl;
  ry /= rl;
  rz /= rl;
  const ux = ry * fz - rz * fy;
  const uy = rz * fx - rx * fz;
  const uz = rx * fy - ry * fx;
  return { px, py, pz, fx, fy, fz, rx, ry, rz, ux, uy, uz };
}

export function createRenderer(canvas) {
  const ctx = canvas.getContext("2d", { alpha: false });
  let W = 1;
  let H = 1;
  let focal = 900;
  let focalBase = 900;
  let cx = 0;
  let cy = 0;
  let faces = [];
  let lines = [];
  let balls = [];
  let decals = [];
  let badges = [];
  let polys = 0;

  const logo = new Image();
  let logoReady = false;
  logo.onload = () => {
    logoReady = true;
  };
  logo.src = "/dallas-dash/assets/kfc-logo.svg";

  function resize() {
    const dpr = Math.min(1.5, window.devicePixelRatio || 1);
    W = Math.max(320, Math.floor(canvas.clientWidth || window.innerWidth));
    H = Math.max(320, Math.floor(canvas.clientHeight || window.innerHeight));
    canvas.width = Math.floor(W * dpr);
    canvas.height = Math.floor(H * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    // landscape screens get a wider lens so the runner doesn't fill the frame
    focalBase = Math.min(W, H) * (W > H * 1.2 ? CAM.focal * 0.8 : CAM.focal);
    focal = focalBase;
    cx = W / 2;
    cy = H * CAM.cy;
  }

  function proj(cam, x, y, z) {
    const dx = x - cam.px;
    const dy = y - cam.py;
    const dz = z - cam.pz;
    const depth = dx * cam.fx + dy * cam.fy + dz * cam.fz;
    if (depth <= 0.2) return null;
    const sx = cx + (focal * (dx * cam.rx + dy * cam.ry + dz * cam.rz)) / depth;
    const sy = cy - (focal * (dx * cam.ux + dy * cam.uy + dz * cam.uz)) / depth;
    return { x: sx, y: sy, d: depth };
  }

  /**
   * Queue a polygon (world points, any length ≥ 3). Polygons that cross the
   * camera's near plane are clipped to it, not dropped — otherwise a trailer
   * you are running on vanishes the moment its near end passes the camera.
   */
  const NEAR = 0.25;
  function camDepth(cam, p) {
    return (p[0] - cam.px) * cam.fx + (p[1] - cam.py) * cam.fy + (p[2] - cam.pz) * cam.fz;
  }
  function face(cam, pts, color, tone, paint) {
    let src = pts;
    let behind = 0;
    const ds = [];
    for (let i = 0; i < pts.length; i += 1) {
      const d = camDepth(cam, pts[i]);
      ds.push(d);
      if (d < NEAR) behind += 1;
    }
    if (behind === pts.length) return;
    if (behind > 0) {
      src = [];
      for (let i = 0; i < pts.length; i += 1) {
        const j = (i + 1) % pts.length;
        const a = pts[i];
        const b = pts[j];
        if (ds[i] >= NEAR) src.push(a);
        if ((ds[i] >= NEAR) !== (ds[j] >= NEAR)) {
          const t = (NEAR - ds[i]) / (ds[j] - ds[i]);
          src.push([a[0] + (b[0] - a[0]) * t, a[1] + (b[1] - a[1]) * t, a[2] + (b[2] - a[2]) * t]);
        }
      }
      if (src.length < 3) return;
    }
    const out = [];
    let depth = 0;
    for (let i = 0; i < src.length; i += 1) {
      const p = proj(cam, src[i][0], src[i][1], src[i][2]);
      if (!p) return;
      out.push(p);
      depth += p.d;
    }
    depth /= src.length;
    if (depth > 270) return;
    let minX = Infinity;
    let maxX = -Infinity;
    let minY = Infinity;
    let maxY = -Infinity;
    for (const p of out) {
      if (p.x < minX) minX = p.x;
      if (p.x > maxX) maxX = p.x;
      if (p.y < minY) minY = p.y;
      if (p.y > maxY) maxY = p.y;
    }
    if (maxX < -90 || minX > W + 90 || maxY < -90 || minY > H + 90) return;
    faces.push({ out, depth, color: rgb(fogMix(color, depth), tone === undefined ? 1 : tone), paint });
  }

  function quad(cam, a, b, c, d, color, tone, paint) {
    face(cam, [a, b, c, d], color, tone, paint);
  }

  function line(cam, a, b, color, width) {
    const p = proj(cam, a[0], a[1], a[2]);
    const q = proj(cam, b[0], b[1], b[2]);
    if (!p || !q) return;
    lines.push({ p, q, color: rgb(color), width: width || 1.5, depth: (p.d + q.d) / 2 });
  }

  /** An axis-aligned box, emitting only the three faces the camera can see. */
  function box(cam, cxx, cyy, czz, w, h, d, color, yaw) {
    const hw = w / 2;
    const hh = h / 2;
    const hd = d / 2;
    const corners = [];
    for (let i = 0; i < 8; i += 1) {
      const sx = i & 1 ? 1 : -1;
      const sy = i & 2 ? 1 : -1;
      const sz = i & 4 ? 1 : -1;
      let x = sx * hw;
      let z = sz * hd;
      if (yaw) {
        const co = Math.cos(yaw);
        const si = Math.sin(yaw);
        const nx = x * co - z * si;
        z = x * si + z * co;
        x = nx;
      }
      corners.push([cxx + x, cyy + sy * hh, czz + z]);
    }
    const c = (i) => corners[i];
    if (cxx > cam.px) quad(cam, c(0), c(2), c(6), c(4), color, 0.74);
    else quad(cam, c(1), c(3), c(7), c(5), color, 0.74);
    if (cyy > cam.py) quad(cam, c(0), c(4), c(5), c(1), color, 0.6);
    else quad(cam, c(2), c(6), c(7), c(3), color, 1.06);
    if (czz > cam.pz) quad(cam, c(0), c(1), c(3), c(2), color, 0.92);
    else quad(cam, c(4), c(5), c(7), c(6), color, 0.92);
  }

  /** A prism around the vertical axis. `stripes` paints alternate sides. */
  function prism(cam, cxx, cyy, czz, r, h, sides, color, stripe, rTop) {
    const top = [];
    const bot = [];
    const rt = rTop === undefined ? r : rTop;
    for (let i = 0; i < sides; i += 1) {
      const a = (i / sides) * Math.PI * 2;
      top.push([cxx + Math.sin(a) * rt, cyy + h, czz + Math.cos(a) * rt]);
      bot.push([cxx + Math.sin(a) * r, cyy, czz + Math.cos(a) * r]);
    }
    for (let i = 0; i < sides; i += 1) {
      const j = (i + 1) % sides;
      const col = stripe && i % 2 === 1 ? stripe : color;
      face(cam, [bot[i], bot[j], top[j], top[i]], col, 0.74 + 0.26 * Math.abs(Math.sin((i / sides) * Math.PI * 2)));
    }
    face(cam, top, color, 1.08);
  }

  function ball(cam, x, y, z, r, color, ring) {
    const p = proj(cam, x, y, z);
    if (!p) return;
    const radius = (focal * r) / p.d;
    if (radius < 0.6 || radius > H) return;
    balls.push({ p, radius, color: rgb(fogMix(color, p.d)), ring, depth: p.d });
  }

  /** Paint 2D content (text, the logo) fitted onto a projected quad. */
  function onQuad(q, paint) {
    if (q.length !== 4) return;
    const [p0, p1, , p3] = q;
    ctx.save();
    ctx.beginPath();
    ctx.moveTo(q[0].x, q[0].y);
    ctx.lineTo(q[1].x, q[1].y);
    ctx.lineTo(q[2].x, q[2].y);
    ctx.lineTo(q[3].x, q[3].y);
    ctx.closePath();
    ctx.clip();
    ctx.transform((p1.x - p0.x) / 100, (p1.y - p0.y) / 100, (p3.x - p0.x) / 100, (p3.y - p0.y) / 100, p0.x, p0.y);
    paint();
    ctx.restore();
  }

  function paintLogo() {
    if (logoReady) ctx.drawImage(logo, 0, 0, 100, 100);
    else {
      ctx.fillStyle = "#e4002b";
      ctx.fillRect(0, 0, 100, 100);
    }
  }

  function paintWordmark(bg) {
    return () => {
      ctx.fillStyle = bg || "#e4002b";
      ctx.fillRect(0, 0, 100, 100);
      ctx.fillStyle = "#fff";
      ctx.font = "italic 900 58px Oswald, Impact, 'Arial Narrow', sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillText("KFC", 50, 54);
    };
  }

  /* ── painted Dallas signage (cached closures, one per look) ─────────── */

  const paintCache = new Map();
  function cached(key, make) {
    let p = paintCache.get(key);
    if (!p) {
      p = make();
      paintCache.set(key, p);
    }
    return p;
  }

  function starPath(x, y, r) {
    ctx.beginPath();
    for (let i = 0; i < 10; i += 1) {
      const a = -Math.PI / 2 + (i * Math.PI) / 5;
      const rr = i % 2 ? r * 0.42 : r;
      ctx.lineTo(x + Math.cos(a) * rr, y + Math.sin(a) * rr);
    }
    ctx.closePath();
  }

  /** Text squeezed to fit `maxW` of the 100-unit sign space. */
  function fitText(text, x, y, size, maxW, fill, stroke) {
    ctx.font = `italic 900 ${size}px Oswald, Impact, 'Arial Narrow', sans-serif`;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    const w = ctx.measureText(text).width;
    ctx.save();
    ctx.translate(x, y);
    if (w > maxW) ctx.scale(maxW / w, 1);
    if (stroke) {
      ctx.lineWidth = size * 0.16;
      ctx.strokeStyle = stroke;
      ctx.lineJoin = "round";
      ctx.strokeText(text, 0, 0);
    }
    ctx.fillStyle = fill;
    ctx.fillText(text, 0, 0);
    ctx.restore();
  }

  function paintMural(text, bg) {
    return cached(`m:${text}:${bg}`, () => () => {
      ctx.fillStyle = bg;
      ctx.fillRect(0, 0, 100, 100);
      // sunburst rays behind the lettering, a Deep Ellum mural staple
      ctx.fillStyle = "rgba(255,255,255,0.16)";
      for (let i = 0; i < 12; i += 2) {
        const a0 = (i / 12) * Math.PI * 2;
        const a1 = ((i + 1) / 12) * Math.PI * 2;
        ctx.beginPath();
        ctx.moveTo(50, 50);
        ctx.lineTo(50 + Math.cos(a0) * 90, 50 + Math.sin(a0) * 90);
        ctx.lineTo(50 + Math.cos(a1) * 90, 50 + Math.sin(a1) * 90);
        ctx.closePath();
        ctx.fill();
      }
      starPath(50, 30, 18);
      ctx.fillStyle = "#fff";
      ctx.fill();
      fitText(text, 50, 70, 34, 92, "#ffd84a", "#202124");
    });
  }

  function paintShopSign(text, bg) {
    return cached(`s:${text}:${bg}`, () => () => {
      ctx.fillStyle = "#202124";
      ctx.fillRect(0, 0, 100, 100);
      ctx.fillStyle = bg;
      ctx.fillRect(4, 8, 92, 84);
      fitText(text, 50, 52, 62, 86, "#fff", "#202124");
    });
  }

  function paintGhost(text) {
    return cached(`g:${text}`, () => () => {
      ctx.fillStyle = "rgba(255,255,255,0.22)";
      ctx.fillRect(0, 0, 100, 100);
      fitText(text, 50, 52, 54, 94, "rgba(255,250,236,0.85)", null);
    });
  }

  /**
   * A KFC poster in the brand's red with the Colonel roundel and a bucket.
   * `aspect` is the panel's width/height in the world, so the art is drawn
   * undistorted however wide the billboard or hoarding is.
   */
  function paintAd(text, aspect) {
    return cached(`ad:${text}:${aspect}`, () => () => {
      const w = 100 * aspect;
      ctx.save();
      ctx.scale(1 / aspect, 1);
      ctx.fillStyle = "#e4002b";
      ctx.fillRect(0, 0, w, 100);
      // the bucket stripes down the left edge
      ctx.fillStyle = "#fff";
      for (let k = 0; k < 3; k += 1) ctx.fillRect(6 + k * 14, 0, 7, 100);
      // the roundel
      ctx.save();
      ctx.beginPath();
      ctx.arc(64, 50, 36, 0, Math.PI * 2);
      ctx.fillStyle = "#fff";
      ctx.fill();
      ctx.clip();
      if (logoReady) ctx.drawImage(logo, 28, 14, 72, 72);
      ctx.restore();
      // a bucket of chicken on the right
      const bx = w - 46;
      ctx.fillStyle = "#c97f2e";
      for (const [dx, dy, r] of [[-10, 26, 11], [4, 22, 12], [16, 28, 10]]) {
        ctx.beginPath();
        ctx.arc(bx + dx, dy + 6, r, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.beginPath();
      ctx.moveTo(bx - 24, 34);
      ctx.lineTo(bx + 24, 34);
      ctx.lineTo(bx + 18, 90);
      ctx.lineTo(bx - 18, 90);
      ctx.closePath();
      ctx.fillStyle = "#fff";
      ctx.fill();
      ctx.save();
      ctx.clip();
      ctx.fillStyle = "#e4002b";
      for (let k = -3; k <= 3; k += 2) ctx.fillRect(bx + k * 7 - 3.5, 30, 7, 64);
      ctx.restore();
      ctx.strokeStyle = "#a40020";
      ctx.lineWidth = 2;
      ctx.stroke();
      // the headline between them
      const left = 108;
      const right = bx - 30;
      const words = text.split(" ");
      const mid = Math.ceil(words.length / 2);
      const lines = words.length > 1 && right - left < 260 ? [words.slice(0, mid).join(" "), words.slice(mid).join(" ")] : [text];
      lines.forEach((ln, k) => {
        const y = lines.length === 1 ? 54 : 34 + k * 38;
        fitText(ln, (left + right) / 2, y, lines.length === 1 ? 46 : 36, right - left, "#fff", null);
      });
      ctx.restore();
    });
  }

  const paintCanada = () => {
    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, 100, 100);
    ctx.fillStyle = "#d52b1e";
    ctx.fillRect(0, 0, 25, 100);
    ctx.fillRect(75, 0, 25, 100);
    const leaf = [[50, 18], [55, 30], [61, 27], [59, 43], [68, 36], [71, 42], [79, 40], [74, 50], [78, 53], [63, 62], [65, 68], [52, 66], [52, 82], [48, 82], [48, 66], [35, 68], [37, 62], [22, 53], [26, 50], [21, 40], [29, 42], [32, 36], [41, 43], [39, 27], [45, 30]];
    ctx.beginPath();
    for (const [x, y] of leaf) ctx.lineTo(x, y);
    ctx.closePath();
    ctx.fill();
  };

  function paintDecoSign(text) {
    return cached(`d:${text}`, () => () => {
      ctx.fillStyle = "#14524f";
      ctx.fillRect(0, 0, 100, 100);
      ctx.strokeStyle = "#e3b04b";
      ctx.lineWidth = 5;
      ctx.strokeRect(4, 10, 92, 80);
      fitText(text, 50, 52, 54, 86, "#f2cf6a", null);
    });
  }

  const paintTexas = () => {
    ctx.fillStyle = "#fff";
    ctx.fillRect(0, 0, 100, 50);
    ctx.fillStyle = "#bf0a30";
    ctx.fillRect(0, 50, 100, 50);
    ctx.fillStyle = "#002868";
    ctx.fillRect(0, 0, 34, 100);
    starPath(17, 50, 12);
    ctx.fillStyle = "#fff";
    ctx.fill();
  };

  function paintHighway(text, shield, ontario) {
    return cached(`h:${text}:${shield}:${ontario}`, () => () => {
      ctx.fillStyle = "#0b6b3a";
      ctx.fillRect(0, 0, 100, 100);
      ctx.strokeStyle = "#fff";
      ctx.lineWidth = 4;
      ctx.strokeRect(5, 7, 90, 86);
      if (ontario) {
        // an Ontario King's Highway shield: white, black rim, a crown on top
        ctx.fillStyle = "#fff";
        ctx.beginPath();
        ctx.moveTo(11, 26);
        ctx.lineTo(39, 26);
        ctx.quadraticCurveTo(40, 54, 25, 64);
        ctx.quadraticCurveTo(10, 54, 11, 26);
        ctx.fill();
        ctx.strokeStyle = "#202124";
        ctx.lineWidth = 2;
        ctx.stroke();
        ctx.fillStyle = "#fff";
        ctx.beginPath();
        ctx.moveTo(14, 25);
        ctx.lineTo(16, 15);
        ctx.lineTo(20, 21);
        ctx.lineTo(25, 13);
        ctx.lineTo(30, 21);
        ctx.lineTo(34, 15);
        ctx.lineTo(36, 25);
        ctx.fill();
        fitText(shield, 25, 43, 17, 24, "#202124", null);
        fitText("EAST", 70, 26, 18, 48, "#fff", null);
        fitText(text, 52, 76, 26, 82, "#fff", null);
        return;
      }
      // interstate shield
      ctx.fillStyle = "#fff";
      ctx.beginPath();
      ctx.moveTo(12, 18);
      ctx.lineTo(38, 18);
      ctx.quadraticCurveTo(40, 50, 25, 62);
      ctx.quadraticCurveTo(10, 50, 12, 18);
      ctx.fill();
      ctx.fillStyle = "#1b3f94";
      ctx.fillRect(14, 28, 22, 22);
      ctx.fillStyle = "#c8102e";
      ctx.fillRect(14, 20, 22, 7);
      fitText(shield, 25, 40, 16, 20, "#fff", null);
      ctx.font = "800 15px Oswald, Impact, sans-serif";
      fitText("NORTH", 70, 26, 18, 48, "#fff", null);
      fitText(text, 52, 76, 26, 82, "#fff", null);
    });
  }

  const paintPegasus = () => {
    ctx.lineCap = "round";
    ctx.lineJoin = "round";
    const horse = () => {
      ctx.beginPath();
      ctx.ellipse(46, 62, 20, 9, -0.1, 0, Math.PI * 2); // body
      ctx.moveTo(62, 56);
      ctx.lineTo(74, 36); // neck
      ctx.lineTo(86, 40); // head
      ctx.moveTo(32, 68);
      ctx.lineTo(22, 86); // hind leg
      ctx.moveTo(40, 70);
      ctx.lineTo(42, 90);
      ctx.moveTo(56, 70);
      ctx.lineTo(66, 86); // fore legs
      ctx.moveTo(60, 68);
      ctx.lineTo(74, 78);
      ctx.moveTo(27, 60);
      ctx.quadraticCurveTo(12, 62, 10, 76); // tail
      ctx.moveTo(44, 54);
      ctx.quadraticCurveTo(26, 30, 18, 8); // wing
      ctx.quadraticCurveTo(44, 22, 56, 52);
      ctx.moveTo(30, 34);
      ctx.lineTo(48, 40);
    };
    horse();
    ctx.strokeStyle = "rgba(255, 60, 70, 0.35)";
    ctx.lineWidth = 11;
    ctx.stroke();
    horse();
    ctx.strokeStyle = "#ff3344";
    ctx.lineWidth = 4.5;
    ctx.stroke();
  };

  /** A 2D paint fitted to a vertical wall plane at x, readable from the road. */
  function wallPaint(cam, side, x, y0, y1, zNear, zFar, paint, bias, fromNear) {
    const q = quadScreen(cam, [x, y1, zFar], [x, y1, zNear], [x, y0, zNear], [x, y0, zFar]);
    if (!q) return;
    // seen from the road, left-hand walls read near→far and right-hand walls
    // far→near; a flag instead always starts at its pole (the near end)
    const ordered = fromNear || side < 0 ? [q[1], q[0], q[3], q[2]] : q;
    faces.push({ out: ordered, depth: (q[0].d + q[2].d) / 2 - (bias || 0.08), color: "#fff", paint });
  }

  /* ── roadside furniture ─────────────────────────────────────────────── */

  /* ── the city: continuous Dallas blocks along both sidewalks ───────── */

  function windowsOn(cam, side, x, zNear, len, y0, y1, rel, seed) {
    const floors = Math.max(1, Math.floor((y1 - y0) / 3.2));
    const per = Math.max(1, Math.floor((len - 1) / 3.2));
    const pitch = (len - 1) / per;
    for (let f = 0; f < floors; f += 1) {
      const wy0 = y0 + f * 3.2 + 0.9;
      const wy1 = wy0 + 1.7;
      if (rel > 70) {
        // far away: one band per floor is enough
        face(cam, [[x, wy0, zNear - 0.6], [x, wy0, zNear - len + 0.6], [x, wy1, zNear - len + 0.6], [x, wy1, zNear - 0.6]], [44, 56, 76], 1);
        continue;
      }
      for (let k = 0; k < per; k += 1) {
        const a = zNear - 0.5 - k * pitch - pitch * 0.2;
        const b = a - pitch * 0.6;
        const on = hash(seed * 31 + f * 7 + k) > 0.72;
        face(cam, [[x, wy0, a], [x, wy0, b], [x, wy1, b], [x, wy1, a]], on ? [255, 212, 140] : [44, 56, 76], 1);
      }
    }
  }

  function waterTower(cam, x, y, z) {
    for (const [dx, dz] of [[-0.8, -0.8], [0.8, -0.8], [-0.8, 0.8], [0.8, 0.8]]) box(cam, x + dx, y + 1, z + dz, 0.14, 2, 0.14, [80, 66, 56]);
    prism(cam, x, y + 2, z, 1.3, 2.2, 8, [150, 112, 82]);
    prism(cam, x, y + 4.2, z, 1.4, 0.9, 8, [96, 80, 70], null, 0.1);
  }

  function building(cam, side, zNear, spec, rel, seed) {
    const len = spec.len;
    const zFar = zNear - len;
    const fx = side * FACE_X;
    const bx = side * (FACE_X + 9);
    const h = spec.h;
    const lit = side < 0 ? 0.98 : 0.82;
    const n = Math.max(1, Math.ceil(len / 6.5));
    const sl = len / n;
    for (let k = 0; k < n; k += 1) {
      const a = zNear - sl * k;
      const b = zNear - sl * (k + 1);
      face(cam, [[fx, 0, a], [fx, 0, b], [fx, h, b], [fx, h, a]], spec.color, lit);
    }
    if (zNear < cam.pz) face(cam, [[fx, 0, zNear], [bx, 0, zNear], [bx, h, zNear], [fx, h, zNear]], spec.color, 1.08);
    if (rel > 150) return;
    const wx = fx - side * 0.04;
    const st = spec.style;
    if (st === "glass") {
      // curtain wall: pale spandrel bands between dark glass floors
      const top = Math.min(h, 32);
      for (let y = 3.2; y < top; y += 3.2) face(cam, [[wx, y, zNear - 0.2], [wx, y, zFar + 0.2], [wx, y + 0.5, zFar + 0.2], [wx, y + 0.5, zNear - 0.2]], [200, 220, 236], lit);
      face(cam, [[wx, 0, zNear - 0.3], [wx, 0, zFar + 0.3], [wx, 2.8, zFar + 0.3], [wx, 2.8, zNear - 0.3]], [230, 238, 244], lit);
      if (spec.pegasus) wallPaint(cam, side, side * (FACE_X + 3), h + 0.4, h + 6.4, zNear - 2, zNear - 9, paintPegasus, 0.2);
    } else if (st === "cantina") {
      face(cam, [[wx, 0.5, zNear - 0.8], [wx, 0.5, zFar + 0.8], [wx, 2.6, zFar + 0.8], [wx, 2.6, zNear - 0.8]], [60, 84, 112], 1);
      // striped awning sloping out over the sidewalk
      const ax = fx - side * 1.5;
      const strips = 6;
      for (let k = 0; k < strips; k += 1) {
        const a = zNear - 0.6 - ((len - 1.2) * k) / strips;
        const b = zNear - 0.6 - ((len - 1.2) * (k + 1)) / strips;
        face(cam, [[fx, 3.3, a], [fx, 3.3, b], [ax, 2.7, b], [ax, 2.7, a]], k % 2 ? WHITE : spec.color, 0.95);
      }
      if (rel < 110) wallPaint(cam, side, wx, 3.5, Math.min(h - 0.2, 4.6), zNear - 2.2, zNear - Math.min(len - 2, 8), paintShopSign(spec.sign, rgb(spec.color, 0.8)));
    } else if (st === "rowhouse") {
      // two narrow houses per block, each a gable, a bay window and a door
      const houses = 2;
      const hl = len / houses;
      for (let k = 0; k < houses; k += 1) {
        const a = zNear - hl * k;
        const b = a - hl;
        const m = (a + b) / 2;
        face(cam, [[wx, h, a - 0.2], [wx, h, b + 0.2], [wx, h + 2.6, m]], spec.color, lit * 0.9);
        line(cam, [wx - side * 0.02, h, a - 0.2], [wx - side * 0.02, h + 2.6, m], WHITE, 2);
        line(cam, [wx - side * 0.02, h + 2.6, m], [wx - side * 0.02, h, b + 0.2], WHITE, 2);
        const bay = fx - side * 0.7;
        const bz0 = a - hl * 0.42;
        const bz1 = b + hl * 0.12;
        face(cam, [[bay, 1.2, bz0], [bay, 1.2, bz1], [bay, 5.6, bz1], [bay, 5.6, bz0]], spec.color, lit * 1.04);
        if (bz0 < cam.pz) face(cam, [[fx, 1.2, bz0], [bay, 1.2, bz0], [bay, 5.6, bz0], [fx, 5.6, bz0]], spec.color, 0.8);
        face(cam, [[bay - side * 0.02, 1.8, bz0 - 0.3], [bay - side * 0.02, 1.8, bz1 + 0.3], [bay - side * 0.02, 3.2, bz1 + 0.3], [bay - side * 0.02, 3.2, bz0 - 0.3]], [70, 92, 120], 1);
        face(cam, [[bay - side * 0.02, 4.0, bz0 - 0.3], [bay - side * 0.02, 4.0, bz1 + 0.3], [bay - side * 0.02, 5.2, bz1 + 0.3], [bay - side * 0.02, 5.2, bz0 - 0.3]], [70, 92, 120], 1);
        face(cam, [[wx, 0, a - 0.5], [wx, 0, a - 1.6], [wx, 2.4, a - 1.6], [wx, 2.4, a - 0.5]], [64, 44, 36], 1);
        face(cam, [[bay, 5.6, bz0], [bay, 5.6, bz1], [fx, 5.9, bz1], [fx, 5.9, bz0]], WHITE, 0.9);
      }
      if (spec.sign && rel < 110) wallPaint(cam, side, fx - side * 0.75, 0.3, 1.1, zNear - hl * 0.45, zNear - hl + 0.6, paintShopSign(spec.sign, rgb(spec.color, 0.7)));
    } else if (st === "warehouse") {
      // corrugated sides, roll-up loading doors and a strip of clerestory glass
      const hasMural = spec.mural && len > 8;
      if (hasMural && rel < 120) wallPaint(cam, side, wx, 2.9, h - 0.9, zNear - 1.2, zNear - Math.min(len - 1.2, 10), paintMural(spec.mural.text, spec.mural.bg));
      else if (rel < 120) face(cam, [[wx, h - 1.4, zNear - 0.4], [wx, h - 1.4, zFar + 0.4], [wx, h - 0.6, zFar + 0.4], [wx, h - 0.6, zNear - 0.4]], [150, 186, 206], 1);
      if (rel < 120) {
        for (let k = 0; k < 2; k += 1) {
          const a = zNear - 1.4 - k * (len / 2);
          face(cam, [[wx, 0, a], [wx, 0, a - 3.6], [wx, 2.7, a - 3.6], [wx, 2.7, a]], [176, 178, 180], 1);
          for (let y = 0.45; y < 2.7; y += 0.45) line(cam, [wx - side * 0.02, y, a], [wx - side * 0.02, y, a - 3.6], [130, 132, 136], 1);
        }
      }
      face(cam, [[fx - side * 0.2, h, zNear], [fx - side * 0.2, h, zFar], [fx - side * 0.2, h + 0.4, zFar], [fx - side * 0.2, h + 0.4, zNear]], [90, 92, 96], lit);
    } else if (st === "deco") {
      // pilasters with gold caps, a stepped crown, and a painted name band
      for (let z = zNear - 1.2; z > zFar + 1; z -= 2.6) {
        face(cam, [[wx - side * 0.25, 0, z], [wx - side * 0.25, 0, z - 0.7], [wx - side * 0.25, h, z - 0.7], [wx - side * 0.25, h, z]], spec.color, lit * 1.08);
        face(cam, [[wx - side * 0.27, h - 0.6, z], [wx - side * 0.27, h - 0.6, z - 0.7], [wx - side * 0.27, h, z - 0.7], [wx - side * 0.27, h, z]], GOLD, 1);
      }
      const mid = zNear - len / 2;
      box(cam, side * (FACE_X + 2.4), h + 1.2, mid, 4.4, 2.4, 5, spec.color);
      box(cam, side * (FACE_X + 2.4), h + 3.0, mid, 2.6, 1.2, 3, spec.color);
      if (rel < 120) wallPaint(cam, side, wx - side * 0.3, h - 2.6, h - 0.9, zNear - 1.6, zNear - len + 1.6, paintDecoSign(spec.ghost));
      if (rel < 140) windowsOn(cam, side, wx, zNear, len, 0, h - 3, rel, seed);
    } else {
      const muralTop = Math.min(h - 1.2, 6.4);
      const hasMural = spec.mural && len > 8;
      if (hasMural && rel < 120) {
        wallPaint(cam, side, wx, 1.2, muralTop, zNear - 1.6, zNear - Math.min(len - 1.6, 9), paintMural(spec.mural.text, spec.mural.bg));
      }
      if (rel < 140) windowsOn(cam, side, wx, zNear, len, hasMural ? 6.4 : 0, h - (st === "loft" ? 3.6 : 1), rel, seed);
      if (st === "loft" && rel < 120) wallPaint(cam, side, wx, h - 3.2, h - 0.8, zNear - 1.5, zNear - len + 1.5, paintGhost(spec.ghost));
      // cornice: a ledge jutting toward the road
      const cx0 = fx - side * 0.35;
      face(cam, [[cx0, h - 0.6, zNear], [cx0, h - 0.6, zFar], [cx0, h, zFar], [cx0, h, zNear]], spec.color, lit * 0.78);
      face(cam, [[fx, h - 0.6, zNear], [fx, h - 0.6, zFar], [cx0, h - 0.6, zFar], [cx0, h - 0.6, zNear]], spec.color, 0.5);
    }
    if (spec.water && rel < 160) waterTower(cam, side * (FACE_X + 3.5), h, zNear - len / 2);
    if (spec.billboard >= 0 && rel < 170) billboard(cam, side, h, zNear - len * 0.5, AD_LINES[spec.billboard]);
  }

  /** A rooftop billboard, square to the road so it reads as you run at it. */
  function billboard(cam, side, roof, z, text) {
    const x0 = side * (FACE_X + 1.2);
    const x1 = side * (FACE_X + 10);
    const y0 = roof + 1.6;
    const y1 = y0 + 3.6;
    for (const px of [x0 + side * 1.8, x1 - side * 1.8]) box(cam, px, roof + 0.8, z - 0.3, 0.3, 1.6, 0.3, [70, 72, 78]);
    box(cam, (x0 + x1) / 2, (y0 + y1) / 2, z - 0.25, Math.abs(x1 - x0) + 0.3, y1 - y0 + 0.3, 0.3, [60, 62, 68]);
    const lx = Math.min(x0, x1);
    const rx = Math.max(x0, x1);
    const q = quadScreen(cam, [lx, y1, z], [rx, y1, z], [rx, y0, z], [lx, y0, z]);
    if (q) faces.push({ out: q, depth: (q[0].d + q[2].d) / 2 - 0.3, color: "#fff", paint: paintAd(text, (rx - lx) / (y1 - y0)) });
  }

  /**
   * A construction lot: a plywood hoarding along the sidewalk papered with
   * KFC posters, and a tower crane over the site behind it.
   */
  function lot(cam, side, zNear, spec, rel) {
    const len = spec.len;
    const fx = side * FACE_X;
    const zFar = zNear - len;
    face(cam, [[fx, 0, zNear], [fx, 0, zFar], [fx, 3.0, zFar], [fx, 3.0, zNear]], [196, 168, 120], side < 0 ? 0.98 : 0.84);
    if (rel < 130) {
      const half = len / 2;
      for (let k = 0; k < 2; k += 1) {
        const a = zNear - 0.4 - k * half;
        const b = a - half + 0.8;
        wallPaint(cam, side, fx - side * 0.04, 0.35, 2.75, a, b, paintAd(AD_LINES[(spec.ad + k) % AD_LINES.length], (half - 0.8) / 2.4));
      }
    }
    if (rel > 190) return;
    const mx = side * (FACE_X + 7);
    const mz = zNear - len / 2;
    box(cam, mx, 12, mz, 1.0, 24, 1.0, [244, 190, 40]);
    box(cam, mx, 24.4, mz + 3, 0.8, 0.8, 22, [244, 190, 40]);
    box(cam, mx, 23.2, mz - 6, 1.6, 1.4, 2.4, [90, 92, 98]);
    line(cam, [mx, 24, mz + 10], [mx, 14, mz + 10], [60, 60, 64], 1.2);
  }
  function maple(cam, x, z, seed) {
    const fall = [[214, 68, 34], [236, 132, 40], [226, 178, 52], [190, 46, 40]];
    prism(cam, x, 0, z, 0.8, 0.4, 6, [120, 112, 104]);
    prism(cam, x, 0.4, z, 0.18, 2.6, 5, [86, 62, 46]);
    ball(cam, x, 3.6, z, 1.4, fall[seed % 4]);
    ball(cam, x + 0.5, 3.1, z + 0.3, 0.95, fall[(seed + 1) % 4]);
  }

  function liveOak(cam, x, z) {
    prism(cam, x, 0, z, 0.9, 0.5, 6, [150, 120, 96]); // planter
    prism(cam, x, 0.5, z, 0.2, 2.4, 5, [104, 80, 58]);
    ball(cam, x, 3.4, z, 1.5, [62, 118, 66]);
    ball(cam, x - 0.6, 3.0, z + 0.4, 1.0, [78, 140, 74]);
  }

  function flagPole(cam, side, x, z) {
    prism(cam, x, 0, z, 0.08, 7.4, 5, [210, 210, 214]);
    wallPaint(cam, side, x, 5.8, 7.3, z - 0.1, z - 2.4, R.flag === "canada" ? paintCanada : paintTexas, 0.05, true);
  }

  function overpass(cam, wz) {
    box(cam, 0, 13.6, wz - 2.5, 40, 1.8, 5, [188, 182, 176]);
    box(cam, 0, 14.7, wz - 0.1, 40, 0.5, 0.3, [160, 154, 150]);
    for (const px of [-7.6, 7.6]) box(cam, px, 6.35, wz - 2.5, 1.2, 12.7, 2.2, [176, 170, 164]);
    const signs = [[-4.8, -0.5, R.signs[0]], [0.5, 4.8, R.signs[1]]];
    for (const [x0, x1, [text, shield]] of signs) {
      const q = quadScreen(cam, [x0, 12.7, wz + 0.02], [x1, 12.7, wz + 0.02], [x1, 10.5, wz + 0.02], [x0, 10.5, wz + 0.02]);
      if (q) faces.push({ out: q, depth: (q[0].d + q[2].d) / 2 - 0.3, color: "#fff", paint: paintHighway(text, shield, R.flag === "canada") });
    }
  }

  /** Blocks, sidewalk props and overpasses for the stretch of road in view. */
  function city(cam, run) {
    const d = run.distance;
    // keep storefronts and landmark plazas clear of buildings
    const clear = [];
    for (const o of run.objects) {
      if (o.t !== 2) continue;
      const rel = o.z - d;
      if (rel < -40 || rel > 240) continue;
      if (o.k === "store") clear.push({ side: Math.floor(o.z) % 2 === 0 ? 1 : -1, a: o.z - 9, b: o.z + 8 });
      else clear.push({ side: o.l === 0 ? -1 : 1, a: o.z - 16, b: o.z + 16, plaza: true });
    }
    const first = Math.floor((d - 14) / SEG);
    const last = Math.floor((d + 205) / SEG);
    for (let i = first; i <= last; i += 1) {
      if (i < 0) continue;
      const z0 = i * SEG;
      const rel = z0 - d;
      const wz = -rel;
      for (const side of [-1, 1]) {
        const spec = blockSpec(i, side);
        const blocked = clear.find((c) => c.side === side && z0 + spec.len > c.a && z0 < c.b);
        if (!blocked) {
          if (spec.style === "lot") lot(cam, side, wz, spec, Math.max(0, rel));
          else building(cam, side, wz, spec, Math.max(0, rel), i * 2 + side);
        }
        else if (blocked.plaza && i % 2 === 0) flagPole(cam, side, side * 8.4, wz - 4);
        if (rel > 120) continue;
        const sx = side * 7.0;
        const kind = (i + (side > 0 ? 1 : 0)) % 4;
        if (kind === 1) {
          if (R.tree === "maple") maple(cam, sx, wz - 6, i);
          else liveOak(cam, sx, wz - 6);
        }
        else if (kind === 3) {
          // Dallas acorn street lamp
          prism(cam, sx, 0, wz - 6, 0.12, 4.6, 5, [36, 38, 44]);
          ball(cam, sx, 4.9, wz - 6, 0.42, [255, 236, 190]);
        } else if (kind === 0 && i % 3 === 0) flagPole(cam, side, side * 7.6, wz - 3);
        else if (kind === 2) prism(cam, sx, 0, wz - 2, 0.22, 0.7, 6, [204, 40, 40]); // hydrant
      }
      // overhead power for the trains: a DART gantry in Dallas; in Toronto a
      // streetcar span wire strung between two poles
      if (i % 2 === 0 && rel < 150) {
        for (const px of [-5.3, 5.3]) prism(cam, px, 0, wz, 0.13, 6.6, 5, [70, 74, 84]);
        if (R.flag === "canada") line(cam, [-5.3, 6.5, wz], [5.3, 6.5, wz], [50, 50, 56], 1.4);
        else box(cam, 0, 6.5, wz, 10.8, 0.2, 0.2, [70, 74, 84]);
        if (rel < 110) for (const lx of LANE_X) line(cam, [lx, 6.25, wz], [lx, 6.25, wz - SEG * 2], [60, 60, 68], 0.9);
      }
      if (i % 17 === 9 && rel > -6) overpass(cam, wz);
    }
  }

  /* ── the runner ────────────────────────────────────────────────────── */

  /**
   * Visual-only motion layered on the simulation: arcade-style hops,
   * leans, squash and stretch. The sim's x/y stay the truth for collisions;
   * this only changes how big the moves *look*.
   */
  const fx = { run: null, tick: -1, visX: 0, velX: 0, visY: 0, lane: 1, hop: 1, dir: 0, jumps: 0, rolls: 0, coins: 0, air: false, base: 0, land: 0, spin: 0, spinOn: false, camX: 0, bank: 0, kick: 0, dip: 0, particles: [] };

  function puff(x, y, z, n, color, spread, up) {
    for (let i = 0; i < n; i += 1) {
      const a = Math.random() * Math.PI * 2;
      fx.particles.push({
        x: x + Math.cos(a) * 0.2,
        y: y + 0.05,
        z,
        spark: color[2] < 100,
        vx: Math.cos(a) * spread,
        vy: up * (0.5 + Math.random()),
        vz: 1.5 + Math.abs(Math.sin(a)) * spread * 0.5,
        life: 0,
        max: 0.35 + Math.random() * 0.25,
        r: color[2] < 100 ? 0.035 + Math.random() * 0.03 : 0.12 + Math.random() * 0.1,
        color,
      });
    }
  }

  function updateFx(run, dt) {
    if (fx.run !== run) {
      Object.assign(fx, { run, tick: run.tick, visX: run.x, velX: 0, visY: run.y, lane: run.lane, hop: 1, dir: 0, jumps: run.jumps, rolls: run.rolls, coins: run.coins, air: false, base: run.y, land: 0, spin: 0, spinOn: false, camX: run.x * 0.34, bank: 0, kick: 0, dip: 0, particles: [] });
    }
    const grounded = run.y <= run.floor + 0.001 && run.vy <= 0;
    if (run.lane !== fx.lane) {
      fx.dir = Math.sign(run.lane - fx.lane);
      fx.lane = run.lane;
      fx.hop = 0;
      if (grounded && run.jet <= 0) puff(run.x, run.floor, -0.2, 5, [230, 214, 196], 1.6, 1.2);
    }
    if (run.jumps !== fx.jumps) {
      fx.jumps = run.jumps;
      fx.air = true;
      fx.base = run.floor;
      fx.kick = 1;
      fx.spinOn = run.sneakers > 0;
      fx.spin = 0;
      puff(run.x, run.floor, 0, 6, [230, 214, 196], 1.2, 1.6);
    }
    if (run.rolls !== fx.rolls) {
      fx.rolls = run.rolls;
      fx.dip = 1;
    }
    if (run.coins !== fx.coins) {
      if (run.coins > fx.coins) puff(run.x, run.y + 1.3, -0.4, 3, [255, 206, 70], 2.2, 2.4);
      fx.coins = run.coins;
    }
    if (fx.air && grounded && run.tick !== fx.tick) {
      fx.air = false;
      fx.land = 1;
      puff(run.x, run.floor, 0, 8, [230, 214, 196], 2.4, 1.4);
    }
    fx.tick = run.tick;

    // springy sideways move with a little overshoot
    const k = 420;
    const damp = 2 * Math.sqrt(k) * 0.52;
    const steps = Math.max(1, Math.round(dt / (1 / 120)));
    const h = dt / steps;
    for (let i = 0; i < steps; i += 1) {
      fx.velX += (k * (run.x - fx.visX) - damp * fx.velX) * h;
      fx.visX += fx.velX * h;
    }
    // jumps read ~55% taller than the sim's arc; ramps and roofs stay exact
    const target = fx.air ? run.y + Math.max(0, run.y - fx.base) * 0.55 : run.y;
    fx.visY += (target - fx.visY) * Math.min(1, dt * (fx.air ? 40 : 26));
    if (run.jet > 0) fx.visY = run.y;

    fx.hop = Math.min(1, fx.hop + dt / 0.26);
    fx.land = Math.max(0, fx.land - dt / 0.2);
    fx.kick = Math.max(0, fx.kick - dt / 0.35);
    fx.dip = Math.max(0, fx.dip - dt / 0.5);
    if (fx.spinOn) fx.spin = Math.min(Math.PI * 2, fx.spin + dt * 13);
    if (!fx.air) fx.spinOn = false;

    // the camera trails the runner and banks into the turn
    fx.camX += (fx.visX * 0.42 - fx.camX) * Math.min(1, dt * 7);
    const bankTarget = Math.max(-0.07, Math.min(0.07, -fx.velX * 0.006));
    fx.bank += (bankTarget - fx.bank) * Math.min(1, dt * 10);

    if (run.slide > 0 && grounded && Math.random() < 0.6) puff(run.x, run.floor, 0.3, 1, [220, 204, 186], 1.4, 0.6);
    for (let i = fx.particles.length - 1; i >= 0; i -= 1) {
      const p = fx.particles[i];
      p.life += dt;
      if (p.life >= p.max) {
        fx.particles.splice(i, 1);
        continue;
      }
      p.x += p.vx * dt;
      p.y += p.vy * dt;
      p.z += p.vz * dt;
      p.vy -= 4 * dt;
    }
    if (fx.particles.length > 80) fx.particles.splice(0, fx.particles.length - 80);
  }

  /**
   * The crew: three original characters in KFC-branded kit. Dante is a
   * skater courier (backwards cap, hoodie, delivery bag); Maya a street
   * dancer (varsity bomber, headphones, high puff); R-11 a sidewalk delivery
   * robot built like today's humanoid robots (white shell, graphite joints, a
   * battery pack on its back). Each carries the Colonel roundel on its back,
   * the side the camera sees.
   */
  const SHELL = [234, 236, 240];
  const JOINT = [64, 68, 78];
  const GLOW = [90, 228, 255];
  const CREW = {
    dante: { skin: [150, 98, 66], hair: [34, 26, 22], top: RED, sleeve: RED, legs: [46, 50, 64], shoe: WHITE, sole: RED, w: 0.72, limb: 0.22 },
    maya: { skin: [206, 150, 112], hair: [52, 34, 26], top: RED, sleeve: WHITE, legs: [32, 34, 44], shoe: WHITE, sole: RED, w: 0.64, limb: 0.19 },
    r11: { skin: SHELL, hair: JOINT, top: SHELL, sleeve: SHELL, legs: SHELL, shoe: JOINT, sole: JOINT, w: 0.7, limb: 0.2 },
  };

  function runner(cam, run, outfit, t) {
    const who = CREW[outfit] ? outfit : "dante";
    const C = CREW[who];
    const robot = who === "r11";
    const x = fx.visX;
    const onBoard = run.board > 0;
    const y = fx.visY + (onBoard ? 0.22 : 0);
    const sliding = run.slide > 0;
    const flying = run.jet > 0;
    const down = Boolean(run.downed);
    const airborne = fx.air && !flying;
    if (run.invuln > 0 && !down && Math.floor(t / 70) % 2 === 0 && run.invuln < 140) return;

    // pose: hop arc and lean on a lane switch, stretch going up, squash on landing
    const hopS = fx.hop < 1 ? Math.sin(Math.PI * fx.hop) : 0;
    const rising = airborne && run.vy > 0;
    let sy = 1;
    if (down) sy = 0.25;
    else if (sliding) sy = 0.42;
    else if (rising) sy = 1.16;
    else if (fx.land > 0) sy = 1 - 0.3 * Math.sin(Math.PI * fx.land);
    const sx = 1 / Math.sqrt(sy);
    const P = {
      x,
      y: y + (sliding || flying || down ? 0 : hopS * 0.42),
      z: 0,
      yaw: fx.dir * 0.5 * hopS + fx.spin,
      roll: fx.dir * 0.3 * hopS + (flying ? Math.sin(t * 0.004) * 0.06 : 0),
      sx,
      sy,
    };
    const co = Math.cos(P.yaw);
    const si = Math.sin(P.yaw);
    const at = (dx, dy, dz) => {
      const ddx = dx * P.sx + P.roll * dy * P.sy;
      return [P.x + ddx * co - dz * si, P.y + dy * P.sy, P.z + ddx * si + dz * co];
    };
    const part = (dx, dy, dz, w, h, d, color) => {
      const c = at(dx, dy, dz);
      box(cam, c[0], c[1], c[2], w * P.sx, h * P.sy, d, color, P.yaw);
    };
    const knob = (dx, dy, dz, r, color) => {
      const c = at(dx, dy, dz);
      ball(cam, c[0], c[1], c[2], r * Math.sqrt(P.sx * P.sy), color);
    };
    // the Colonel roundel on a back panel (skipped while spun away from us)
    const roundel = (dx, dy, dz, size) => {
      if (co < 0.3) return;
      const h = size / 2;
      const q = quadScreen(cam, at(dx - h, dy + h, dz), at(dx + h, dy + h, dz), at(dx + h, dy - h, dz), at(dx - h, dy - h, dz));
      if (q) faces.push({ out: q, depth: (q[0].d + q[2].d) / 2 - 0.06, color: "#fff", paint: paintLogo, round: true });
    };

    const stride = sliding || flying || down || airborne ? 0 : Math.sin(run.tick * 0.42);
    const bob = sliding || flying || airborne ? 0 : Math.abs(Math.cos(run.tick * 0.42)) * 0.09;
    const lean = sliding ? -0.1 : 0.12;
    const legZ = stride * 0.52;

    if (onBoard) {
      box(cam, x, y - 0.14, 0.05, 0.7, 0.08, 1.6, RED);
      box(cam, x, y - 0.1, 0.05, 0.26, 0.02, 1.62, WHITE);
      decals.push({ x, z: 0.05, r: 0.8, color: [255, 120, 140], alpha: 0.35 });
    }
    // legs: tucked up in the air, pumping on the ground
    const legY = airborne ? 0.5 : 0.3;
    const tuck = airborne ? -0.22 : 0;
    const L = C.limb;
    for (const s of [-1, 1]) {
      const lz = s * -legZ + lean + tuck;
      part(s * 0.17, legY, lz, L, 0.6, L + 0.02, C.legs);
      if (robot) part(s * 0.17, legY + 0.1, lz - 0.02, L + 0.04, 0.12, L + 0.06, JOINT); // knee
      part(s * 0.17, legY - 0.23, lz - 0.07, L + 0.04, 0.15, 0.36, C.shoe);
      part(s * 0.17, legY - 0.3, lz - 0.07, L + 0.05, 0.04, 0.37, C.sole);
    }

    // torso
    if (robot) {
      part(0, 0.66 + bob, lean, 0.46, 0.18, 0.34, JOINT); // hip actuator
      part(0, 1.0 + bob, lean, C.w, 0.62, 0.44, SHELL);
      part(0, 1.08 + bob, lean - 0.23, C.w * 0.7, 0.12, 0.02, RED); // chest band
      part(0, 1.08 + bob, lean + 0.3, 0.5, 0.56, 0.18, JOINT); // battery pack
      part(0, 0.86 + bob, lean + 0.4, 0.34, 0.05, 0.02, GLOW);
      roundel(0, 1.16 + bob, lean + 0.395, 0.34);
    } else {
      part(0, 0.98 + bob, lean, C.w, 0.76, 0.44, C.top);
      part(0, 0.64 + bob, lean, C.w + 0.02, 0.1, 0.46, who === "maya" ? WHITE : RED_DEEP); // waistband
      if (who === "dante") {
        part(0, 1.38 + bob, lean + 0.2, 0.46, 0.2, 0.14, RED_DEEP); // the hood
        part(0, 1.12 + bob, lean + 0.34, 0.58, 0.58, 0.26, WHITE); // delivery bag
        part(0, 1.12 + bob, lean + 0.34, 0.6, 0.08, 0.27, RED);
        roundel(0, 1.14 + bob, lean + 0.48, 0.4);
      } else {
        part(0, 1.1 + bob, lean + 0.3, 0.42, 0.46, 0.18, RED); // mini backpack
        roundel(0, 1.12 + bob, lean + 0.395, 0.34);
      }
    }
    if (flying) {
      box(cam, x - 0.18, y + 1.0, lean + 0.52, 0.22, 0.6, 0.22, [180, 186, 196]);
      box(cam, x + 0.18, y + 1.0, lean + 0.52, 0.22, 0.6, 0.22, [180, 186, 196]);
      const flick = 0.5 + 0.3 * Math.sin(t * 0.05);
      face(cam, [[x - 0.28, y + 0.7, lean + 0.52], [x - 0.08, y + 0.7, lean + 0.52], [x - 0.18, y + 0.7 - flick, lean + 0.52]], [255, 170, 40], 1.2);
      face(cam, [[x + 0.08, y + 0.7, lean + 0.52], [x + 0.28, y + 0.7, lean + 0.52], [x + 0.18, y + 0.7 - flick, lean + 0.52]], [255, 170, 40], 1.2);
    }

    // arms: thrown up on a jump, swinging on the run
    const armSwing = flying || airborne ? 0 : legZ * 1.1;
    const armY = airborne ? 1.42 : 1.0;
    const handY = airborne ? 1.82 : 0.74;
    const armOut = airborne ? 0.54 : C.w * 0.66;
    for (const s of [-1, 1]) {
      const az = s * armSwing + lean;
      part(s * armOut, armY + bob, az, 0.18, 0.64, 0.2, C.sleeve);
      if (who === "dante") part(s * (armOut + 0.09), armY + bob, az, 0.02, 0.6, 0.06, WHITE); // sleeve stripe
      if (who === "maya") part(s * armOut, (airborne ? 1.7 : 0.82) + bob, az, 0.2, 0.08, 0.22, RED); // cuff
      if (robot) {
        part(s * (armOut - 0.04), 1.3 + bob, lean, 0.26, 0.16, 0.36, RED); // shoulder plate
        part(s * armOut, armY + bob, az, 0.2, 0.1, 0.22, JOINT); // elbow
      }
      part(s * armOut, handY + bob, az, 0.2, 0.16, 0.22, robot ? JOINT : C.skin);
    }

    // head
    const headY = 1.62 + bob;
    if (robot) {
      part(0, 1.4 + bob, lean, 0.18, 0.12, 0.18, JOINT); // neck
      part(0, headY, lean, 0.5, 0.4, 0.46, SHELL);
      part(0, headY + 0.02, lean - 0.24, 0.44, 0.18, 0.02, [20, 22, 30]); // visor
      for (const s of [-1, 1]) part(s * 0.26, headY + 0.02, lean - 0.08, 0.02, 0.1, 0.26, GLOW);
      const tip = at(0.12, headY + 0.52, lean + 0.1);
      const base = at(0.12, headY + 0.2, lean + 0.1);
      line(cam, base, tip, JOINT, 2);
      ball(cam, tip[0], tip[1], tip[2], 0.07, RED);
    } else {
      part(0, headY, lean, 0.46, 0.44, 0.44, C.skin);
      if (who === "dante") {
        part(0, headY + 0.24, lean, 0.5, 0.16, 0.5, RED); // cap, worn backwards
        part(0, headY + 0.19, lean + 0.32, 0.4, 0.05, 0.22, RED_DEEP); // brim
        part(0, headY + 0.27, lean + 0.2, 0.18, 0.08, 0.1, WHITE); // strap badge
        part(0, headY + 0.04, lean + 0.12, 0.48, 0.18, 0.22, C.hair);
      } else {
        part(0, headY + 0.12, lean + 0.04, 0.5, 0.3, 0.48, C.hair);
        // a high puff that bounces with the stride
        const sway = stride * 0.06;
        knob(sway, headY + 0.44, lean + 0.14, 0.26, C.hair);
        knob(sway * 1.6, headY + 0.22, lean + 0.34, 0.14, C.hair);
        for (const s of [-1, 1]) part(s * 0.26, headY, lean, 0.08, 0.2, 0.2, RED); // headphones
        part(0, headY + 0.3, lean, 0.56, 0.05, 0.08, RED);
      }
    }

    if (!flying) {
      const lift = Math.max(0, fx.visY - run.floor);
      decals.push({ x, y: run.floor, z: 0, r: 0.5 - Math.min(0.24, lift * 0.07), color: [20, 20, 30], alpha: 0.32 });
    }
  }

  /** The parking officer who chases you after a stumble. Falls back as the timer runs out. */
  function chaser(cam, run) {
    const caught = run.downed === "caught";
    if (run.chase < 90 && !caught) return;
    // just behind and to one side, so he reads as a threat without hiding the road
    const z = caught ? 1.4 : 2.0 + (1 - run.chase / 300) * 1.6;
    const side = run.x > 1 ? -1 : 1;
    const x = run.x + side * (caught ? 0.8 : 1.2);
    const stride = caught ? 0 : Math.sin(run.tick * 0.42 + 1);
    const navy = [34, 48, 92];
    const vest = [206, 236, 52];
    box(cam, x - 0.18, 0.32, z + stride * 0.4, 0.24, 0.64, 0.26, navy);
    box(cam, x + 0.18, 0.32, z - stride * 0.4, 0.24, 0.64, 0.26, navy);
    box(cam, x, 1.04, z, 0.78, 0.84, 0.48, navy);
    box(cam, x, 1.08, z, 0.8, 0.6, 0.5, vest); // hi-vis vest
    box(cam, x, 1.0, z, 0.82, 0.08, 0.52, [220, 224, 228]);
    box(cam, x + 0.5, 1.06, z - 0.1, 0.2, 0.66, 0.22, navy);
    box(cam, x - 0.5, 1.2, z - 0.3, 0.2, 0.2, 0.6, navy);
    box(cam, x - 0.5, 1.26, z - 0.62, 0.3, 0.44, 0.05, WHITE); // ticket book
    box(cam, x, 1.72, z, 0.46, 0.44, 0.44, [196, 150, 116]);
    box(cam, x, 1.98, z, 0.5, 0.14, 0.5, navy); // peaked cap
    box(cam, x, 1.93, z - 0.32, 0.44, 0.04, 0.2, INK);
    decals.push({ x, y: 0, z, r: 0.5, color: [20, 20, 30], alpha: 0.26 });
  }

  /* ── pickups ───────────────────────────────────────────────────────── */

  function drumstick(cam, x, y, z) {
    ball(cam, x, y, z, 0.3, CRISP);
    line(cam, [x, y - 0.1, z], [x + 0.26, y - 0.42, z], WHITE, 3);
    ball(cam, x + 0.28, y - 0.44, z, 0.08, WHITE);
  }

  function item(cam, kind, x, y, z) {
    switch (kind) {
      case "bucket":
        prism(cam, x, y - 0.36, z, 0.36, 0.66, 10, RED, WHITE, 0.44);
        ball(cam, x - 0.12, y + 0.36, z, 0.2, CRISP);
        ball(cam, x + 0.14, y + 0.34, z, 0.18, CRISP_DARK);
        break;
      case "bowl":
        prism(cam, x, y - 0.24, z, 0.32, 0.3, 10, WHITE, null, 0.42);
        box(cam, x, y + 0.08, z, 0.6, 0.07, 0.6, GRAVY);
        ball(cam, x, y + 0.16, z, 0.16, CRISP);
        break;
      case "sandwich":
        box(cam, x, y - 0.18, z, 0.66, 0.2, 0.66, BUN);
        box(cam, x, y, z, 0.72, 0.18, 0.72, CRISP);
        box(cam, x, y + 0.1, z, 0.62, 0.05, 0.62, LETTUCE);
        box(cam, x, y + 0.24, z, 0.66, 0.24, 0.66, BUN);
        break;
      case "popcorn":
        prism(cam, x, y - 0.3, z, 0.26, 0.5, 8, RED, WHITE, 0.32);
        ball(cam, x, y + 0.26, z, 0.22, CRISP);
        break;
      case "fries":
        box(cam, x, y - 0.12, z, 0.44, 0.56, 0.3, RED);
        box(cam, x, y + 0.04, z - 0.16, 0.2, 0.2, 0.02, WHITE);
        box(cam, x - 0.1, y + 0.3, z, 0.08, 0.3, 0.08, [246, 210, 120]);
        box(cam, x + 0.08, y + 0.32, z, 0.08, 0.34, 0.08, [246, 210, 120]);
        break;
      case "biscuit":
        prism(cam, x, y - 0.16, z, 0.3, 0.3, 8, [222, 170, 104], null, 0.26);
        break;
      case "coleslaw":
        prism(cam, x, y - 0.2, z, 0.26, 0.34, 8, WHITE, [210, 236, 200], 0.3);
        break;
      default:
        prism(cam, x, y - 0.32, z, 0.22, 0.64, 8, RED, WHITE, 0.27);
        line(cam, [x, y + 0.32, z], [x + 0.06, y + 0.6, z], WHITE, 2);
    }
  }

  const POWER_STYLE = {
    magnet: { color: [228, 0, 43], glyph: "🧲" },
    jetpack: { color: [255, 150, 30], glyph: "🚀" },
    sneakers: { color: [60, 160, 240], glyph: "👟" },
    double: { color: [255, 196, 30], glyph: "2×" },
  };

  /* ── hazards ───────────────────────────────────────────────────────── */

  const WORKS = [246, 112, 30]; // street-works orange
  const rgbMul = (c, f) => [c[0] * f, c[1] * f, c[2] * f];
  const paintClearance = () => {
    ctx.fillStyle = "#f6c428";
    ctx.fillRect(0, 0, 100, 100);
    ctx.strokeStyle = "#202124";
    ctx.lineWidth = 5;
    ctx.strokeRect(4, 8, 92, 84);
    fitText("LOW CLEARANCE", 50, 52, 40, 88, "#202124", null);
  };

  function stripedBox(cam, x, y, z, w, h, d, a, b, n) {
    // alternating bands across the width
    const bw = w / n;
    for (let i = 0; i < n; i += 1) {
      box(cam, x - w / 2 + bw * (i + 0.5), y, z, bw, h, d, i % 2 ? b : a);
    }
  }

  // course vehicles and freight are deliberately unbranded
  const FLEET = [[58, 108, 168], [36, 132, 104], [118, 120, 132], [226, 156, 40]];
  const BOX_COLORS = [[44, 98, 168], [32, 128, 92], [214, 112, 42], [128, 134, 146], [40, 140, 150], [150, 60, 64]];

  function van(cam, x, wz, len, oncoming, seed) {
    // wz is the near end; the van extends away from the camera
    const zc = wz - len / 2;
    const stripe = FLEET[Math.floor(hash(seed) * FLEET.length)];
    box(cam, x, 1.25, zc, 2.0, 2.0, len, [236, 238, 240]);
    box(cam, x, 0.95, zc, 2.02, 0.42, len + 0.02, stripe);
    box(cam, x, 2.27, zc, 1.9, 0.06, len - 0.2, [236, 236, 236]);
    const front = oncoming ? wz : wz - len;
    box(cam, x, 1.65, oncoming ? wz - 0.05 : front + 0.05, 1.7, 0.6, 0.1, DARK_GLASS);
    for (const s of [-0.9, 0.9]) {
      box(cam, x + s, 0.3, zc - len * 0.3, 0.3, 0.6, 0.6, INK);
      box(cam, x + s, 0.3, zc + len * 0.3, 0.3, 0.6, 0.6, INK);
    }
    if (oncoming) {
      ball(cam, x - 0.7, 0.85, wz + 0.05, 0.16, [255, 250, 210]);
      ball(cam, x + 0.7, 0.85, wz + 0.05, 0.16, [255, 250, 210]);
    }
    if (!oncoming) {
      // rear doors: a seam and two handles
      box(cam, x, 1.25, wz + 0.02, 0.05, 1.8, 0.04, [180, 184, 190]);
      box(cam, x - 0.2, 1.3, wz + 0.04, 0.1, 0.2, 0.04, INK);
      box(cam, x + 0.2, 1.3, wz + 0.04, 0.1, 0.2, 0.04, INK);
      ball(cam, x - 0.85, 0.7, wz + 0.05, 0.1, [220, 40, 40]);
      ball(cam, x + 0.85, 0.7, wz + 0.05, 0.1, [220, 40, 40]);
    }
  }

  function trailer(cam, x, wz, o) {
    const h = HAZ.trailer.h;
    const ramp = o.ramp || 0;
    const bodyNear = wz - ramp;
    const bodyLen = o.len - ramp;
    // Built from short segments: the painter's sort works per face, and one
    // 30-unit face would sort as if it were all at its middle — drawing the
    // trailer over a runner standing on its roof.
    const seg = Math.max(1, Math.ceil(bodyLen / 4));
    const sl = bodyLen / seg;
    const col = BOX_COLORS[Math.floor(hash(Math.floor(o.z) * 3 + o.l) * BOX_COLORS.length)];
    for (let k = 0; k < seg; k += 1) {
      const zc = bodyNear - sl * (k + 0.5);
      box(cam, x, h / 2 + 0.25, zc, 2.1, h - 0.5, sl, col);
      box(cam, x, h - 0.02, zc, 2.14, 0.06, sl, rgbMul(col, 0.8));
      // corrugation ribs
      for (let b = 0; b < 4; b += 1) box(cam, x, 0.75 + b * 0.42, zc, 2.15, 0.08, sl, rgbMul(col, 0.78));
      box(cam, x, 0.18, zc, 2.0, 0.16, sl, [60, 62, 68]); // flatbed chassis
    }
    for (const s of [-0.95, 0.95]) {
      box(cam, x + s, 0.22, bodyNear - 1.2, 0.3, 0.44, 0.7, INK);
      box(cam, x + s, 0.22, bodyNear - bodyLen + 1.2, 0.3, 0.44, 0.7, INK);
    }
    // container doors: two leaves, locking bars
    box(cam, x, h / 2 + 0.25, bodyNear + 0.03, 0.05, h - 0.7, 0.04, rgbMul(col, 0.6));
    for (const bx of [-0.6, -0.3, 0.3, 0.6]) box(cam, x + bx, h / 2 + 0.25, bodyNear + 0.05, 0.05, h - 0.9, 0.04, [170, 172, 176]);
    if (ramp > 0) {
      // the ramp: a sloped deck with yellow chevrons, then its two side walls
      const RS = 4;
      for (let k = 0; k < RS; k += 1) {
        const f0 = k / RS;
        const f1 = (k + 1) / RS;
        quad(cam, [x - 1.0, h * f0 + 0.02, wz - ramp * f0], [x + 1.0, h * f0 + 0.02, wz - ramp * f0], [x + 1.0, h * f1 + 0.02, wz - ramp * f1], [x - 1.0, h * f1 + 0.02, wz - ramp * f1], k % 2 ? INK : [220, 200, 120], 1.0);
      }
      const sx = x > cam.px ? x - 1.0 : x + 1.0;
      for (let k = 0; k < 2; k += 1) {
        const f0 = k / 2;
        const f1 = (k + 1) / 2;
        face(cam, [[sx, 0, wz - ramp * f0], [sx, 0, wz - ramp * f1], [sx, h * f1, wz - ramp * f1], [sx, h * f0, wz - ramp * f0]], [190, 170, 100], 0.7);
      }
    }
  }

  function obstacle(cam, o, x, wz) {
    switch (o.k) {
      case "cone":
        prism(cam, x, 0, wz, 0.42, 0.9, 8, [246, 112, 30], WHITE, 0.04);
        box(cam, x, 0.03, wz, 0.9, 0.06, 0.9, [246, 112, 30]);
        break;
      case "crate":
        box(cam, x, 0.5, wz, 1.0, 1.0, 0.9, [166, 118, 70]);
        box(cam, x, 0.5, wz + 0.46, 1.0, 0.14, 0.02, [124, 86, 52]);
        box(cam, x, 0.5, wz + 0.46, 0.14, 1.0, 0.02, [124, 86, 52]);
        break;
      case "barrier":
        stripedBox(cam, x, 0.62, wz, 1.9, 0.36, 0.3, WORKS, WHITE, 5);
        box(cam, x - 0.8, 0.3, wz, 0.14, 0.6, 0.14, [120, 120, 126]);
        box(cam, x + 0.8, 0.3, wz, 0.14, 0.6, 0.14, [120, 120, 126]);
        break;
      case "bucketstack":
        // a cluster of traffic drums (the sim id is kept for old replays)
        prism(cam, x - 0.34, 0, wz, 0.36, 1.0, 10, WORKS, WHITE, 0.34);
        prism(cam, x + 0.36, 0, wz, 0.36, 1.0, 10, WORKS, WHITE, 0.34);
        box(cam, x - 0.34, 0.03, wz, 0.9, 0.06, 0.9, INK);
        box(cam, x + 0.36, 0.03, wz, 0.9, 0.06, 0.9, INK);
        break;
      case "bar":
        box(cam, x - 0.95, 1.1, wz, 0.16, 2.2, 0.16, [120, 120, 126]);
        box(cam, x + 0.95, 1.1, wz, 0.16, 2.2, 0.16, [120, 120, 126]);
        stripedBox(cam, x, 1.75, wz, 2.0, 0.7, 0.24, [250, 204, 40], INK, 6);
        break;
      case "awning": {
        if (o.l !== 1) break; // one scaffold spans the road; draw it once
        for (const px of [-ROAD_HALF - 0.2, -1.15, 1.15, ROAD_HALF + 0.2]) {
          if (Math.abs(px) < 2) continue;
          box(cam, px, 1.25, wz, 0.18, 2.5, 0.18, [150, 152, 158]);
          box(cam, px, 1.25, wz - 1.2, 0.18, 2.5, 0.18, [150, 152, 158]);
        }
        box(cam, 0, 2.15, wz - 0.6, 9.6, 0.7, 1.4, [52, 120, 92]); // safety netting
        stripedBox(cam, 0, 1.72, wz + 0.1, 9.6, 0.16, 0.06, [250, 204, 40], INK, 16);
        const q = quadScreen(cam, [-1.4, 2.45, wz + 0.12], [1.4, 2.45, wz + 0.12], [1.4, 1.85, wz + 0.12], [-1.4, 1.85, wz + 0.12]);
        if (q) faces.push({ out: q, depth: (q[0].d + q[2].d) / 2 - 0.05, color: "#fff", paint: paintClearance });
        break;
      }
      case "van":
        van(cam, x, wz, o.len, false, Math.floor(o.z));
        break;
      case "oncoming":
        van(cam, x, wz, o.len, true, Math.floor(o.z) + 1);
        break;
      case "trailer":
        trailer(cam, x, wz, o);
        break;
      default:
        box(cam, x, 0.5, wz, 0.9, 1.0, 0.9, [150, 104, 62]);
    }
  }

  function quadScreen(cam, a, b, c, d) {
    const out = [];
    for (const p of [a, b, c, d]) {
      const q = proj(cam, p[0], p[1], p[2]);
      if (!q) return null;
      out.push(q);
    }
    return out;
  }

  /* ── KFC storefront + real Dallas landmarks on the roadside ───────────── */

  function storefront(cam, side, z) {
    // close enough to the road to stay in frame on a narrow phone
    const x = side * 11.6;
    const faceX = x - side * 3.5;
    box(cam, x, 2.8, z, 7, 5.6, 10, WHITE);
    // red-and-white striped roof band, the brand's signature
    for (let i = 0; i < 10; i += 1) {
      box(cam, faceX - side * 0.1, 4.9, z - 4.5 + i, 0.3, 1.4, 1, i % 2 ? WHITE : RED);
    }
    box(cam, faceX - side * 0.15, 1.5, z, 0.3, 2.6, 8.6, GLASS);
    box(cam, faceX - side * 0.2, 2.9, z, 0.5, 0.2, 9, RED);
    const q = quadScreen(cam, [faceX - side * 0.4, 7.6, z - 2], [faceX - side * 0.4, 7.6, z + 2], [faceX - side * 0.4, 5.6, z + 2], [faceX - side * 0.4, 5.6, z - 2]);
    if (q) {
      const ordered = side < 0 ? [q[1], q[0], q[3], q[2]] : q;
      faces.push({ out: ordered, depth: (q[0].d + q[2].d) / 2 - 0.1, color: "#fff", paint: paintWordmark() });
    }
    // the rotating bucket on its pole, with the Colonel on the front
    const px = side * 8.8;
    const pz = z + 7;
    prism(cam, px, 0, pz, 0.22, 7, 6, [120, 120, 126]);
    prism(cam, px, 7, pz, 1.1, 2.2, 12, RED, WHITE, 1.35);
    const lq = quadScreen(cam, [px - 1.2, 9.0, pz + 1.3], [px + 1.2, 9.0, pz + 1.3], [px + 1.2, 6.6, pz + 1.3], [px - 1.2, 6.6, pz + 1.3]);
    if (lq) faces.push({ out: lq, depth: (lq[0].d + lq[2].d) / 2 - 0.2, color: "#fff", paint: paintLogo, round: true });
  }

  /** A painted panel square to the road, facing the runner. */
  function facing(cam, x0, x1, y0, y1, z, paint) {
    const lx = Math.min(x0, x1);
    const rx = Math.max(x0, x1);
    const q = quadScreen(cam, [lx, y1, z], [rx, y1, z], [rx, y0, z], [lx, y0, z]);
    if (q) faces.push({ out: q, depth: (q[0].d + q[2].d) / 2 - 0.2, color: "#fff", paint });
  }

  function marquee(text, bg) {
    return cached(`q:${text}:${bg}`, () => () => {
      ctx.fillStyle = "#202124";
      ctx.fillRect(0, 0, 100, 100);
      ctx.fillStyle = bg;
      ctx.fillRect(3, 6, 94, 88);
      ctx.fillStyle = "#fff3c4";
      for (let i = 0; i < 12; i += 1) {
        ctx.beginPath();
        ctx.arc(8 + i * 7.6, 10, 2, 0, Math.PI * 2);
        ctx.arc(8 + i * 7.6, 90, 2, 0, Math.PI * 2);
        ctx.fill();
      }
      fitText(text, 50, 52, 56, 86, "#fff3c4", "#202124");
    });
  }

  const paintTorontoSign = () => {
    const letters = "TORONTO";
    const cols = ["#e4002b", "#1b6fd1", "#16a37f", "#f2a20c", "#7a3fc4", "#0b9bb5", "#e4002b"];
    ctx.font = "900 40px Oswald, Impact, 'Arial Narrow', sans-serif";
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";
    for (let i = 0; i < letters.length; i += 1) {
      ctx.fillStyle = cols[i];
      ctx.fillRect(1 + i * 14.1, 8, 13, 84);
      ctx.fillStyle = "#fff";
      ctx.save();
      ctx.translate(7.5 + i * 14.1, 52);
      ctx.scale(0.3, 1.6);
      ctx.fillText(letters[i], 0, 0);
      ctx.restore();
    }
  };

  function tree(cam, x, z, r, color) {
    prism(cam, x, 0, z, 0.35, 2.2, 5, [110, 84, 60]);
    ball(cam, x, 2.2 + r, z, r, color);
  }

  /** Forty real buildings across the four routes, each a silhouette you can name. */
  function landmark(cam, model, side, z) {
    // set back across the plaza the course clears for it, so it can be seen
    // through the gap in the street wall rather than only over the rooftops
    const x = side * 22;
    const s = side;
    switch (model) {
      /* ── Downtown Dallas ── */
      case "reunion":
        box(cam, x, 16, z, 3.6, 32, 3.6, [200, 196, 196]);
        prism(cam, x, 21.5, z, 4.6, 3.4, 8, [210, 206, 204]);
        prism(cam, x, 8.5, z, 5.6, 3.4, 8, [210, 206, 204]);
        ball(cam, x, 36, z, 4.4, [246, 236, 214], true);
        break;
      case "bofa":
        box(cam, x, 24, z, 9, 48, 9, DARK_GLASS);
        for (let i = -1; i <= 1; i += 1) {
          line(cam, [x + i * 4.5, 0, z + 4.5], [x + i * 4.5, 48, z + 4.5], GREEN, 2);
          line(cam, [x - s * 4.5, 0, z + i * 4.5], [x - s * 4.5, 48, z + i * 4.5], GREEN, 2);
        }
        break;
      case "fountain":
        box(cam, x, 15, z, 10, 30, 10, [96, 150, 170]);
        prism(cam, x, 30, z, 7.2, 12, 4, [120, 172, 192], null, 0.4);
        break;
      case "omni":
        box(cam, x, 9, z, 18, 18, 14, [88, 92, 110]);
        for (let i = 1; i <= 4; i += 1) box(cam, x - s * 9.1, 3.2 * i, z, 0.4, 0.5, 13, [RED, GOLD, GREEN, [90, 160, 240]][i % 4]);
        break;
      case "oldred":
        box(cam, x, 5, z, 16, 10, 12, [176, 84, 64]);
        box(cam, x + s * 3, 14, z, 5, 16, 5, [176, 84, 64]);
        prism(cam, x + s * 3, 22, z, 3.2, 3, 4, [96, 104, 100], null, 0.1);
        for (const dz of [-5.4, 5.4]) prism(cam, x - s * 7.4, 10, z + dz, 0.9, 2.4, 6, [96, 104, 100], null, 0.05);
        break;
      case "comerica":
        box(cam, x, 14, z, 12, 28, 12, [110, 104, 128]);
        box(cam, x, 29, z, 11, 8, 11, [96, 90, 116]);
        box(cam, x, 34.6, z, 5.6, 1, 5.6, [80, 76, 100]);
        break;
      case "discovery":
        box(cam, x, 8, z, 14, 16, 14, [80, 92, 116]);
        box(cam, x - s * 7.1, 9, z, 0.3, 9, 11, [52, 170, 236]);
        box(cam, x - s * 7.2, 9, z + 2, 0.3, 4, 3, [236, 80, 160]);
        break;
      case "cityhall":
        // I. M. Pei's city hall leans out over its plaza, a floor at a time
        box(cam, x + s * 2, 3, z, 9, 6, 22, [210, 200, 184]);
        box(cam, x + s * 0.8, 8, z, 11.4, 4, 22, [214, 204, 188]);
        box(cam, x - s * 0.4, 12, z, 13.8, 4, 22, [218, 208, 192]);
        box(cam, x - s * 1.6, 16, z, 16.2, 4, 22, [222, 212, 196]);
        for (const dz of [-8, 0, 8]) box(cam, x - s * 6, 4, z + dz, 1.2, 8, 1.2, [200, 190, 174]);
        break;
      case "winspear":
        face(cam, [[x - 7, 0, z - 6], [x + 7, 0, z - 6], [x + 5, 11, z - 6], [x - 5, 11, z - 6]], [190, 44, 58], 0.95);
        face(cam, [[x - 7, 0, z + 6], [x + 7, 0, z + 6], [x + 5, 11, z + 6], [x - 5, 11, z + 6]], [190, 44, 58], 0.8);
        box(cam, x - s * 3, 13, z, 24, 0.5, 20, [206, 206, 210]);
        for (const dz of [-9, 9]) box(cam, x - s * 13, 6.5, z + dz, 0.4, 13, 0.4, [180, 180, 186]);
        break;
      case "park":
        for (let i = 0; i < 6; i += 1) tree(cam, x - s * 2, z - 10 + i * 5, 2.1, [84, 150, 86]);
        box(cam, x - s * 9, 1.2, z, 3, 2.4, 6, [240, 240, 236]); // the pavilion
        break;

      /* ── East Dallas ── */
      case "ferris": {
        const cx0 = x + s * 8;
        const cy0 = 21;
        const r = 17;
        const n = 24;
        const pts = [];
        for (let i = 0; i < n; i += 1) {
          const a = (i / n) * Math.PI * 2;
          pts.push([cx0 + Math.cos(a) * r, cy0 + Math.sin(a) * r, z]);
        }
        for (let i = 0; i < n; i += 1) {
          line(cam, pts[i], pts[(i + 1) % n], [236, 236, 240], 2.4);
          if (i % 2 === 0) line(cam, pts[i], [cx0, cy0, z], [210, 210, 220], 1);
          ball(cam, pts[i][0], pts[i][1] - 0.8, z + 0.3, 0.7, [RED, GOLD, [90, 160, 240], GREEN][i % 4]);
        }
        line(cam, [cx0 - 6, 0, z], [cx0, cy0, z], [200, 200, 210], 3);
        line(cam, [cx0 + 6, 0, z], [cx0, cy0, z], [200, 200, 210], 3);
        ball(cam, cx0, cy0, z + 0.2, 1.2, [246, 236, 214]);
        break;
      }
      case "hallofstate":
        box(cam, x, 5, z, 12, 10, 26, [228, 210, 172]);
        box(cam, x - s * 5.6, 9, z, 2, 18, 8, [218, 196, 156]);
        box(cam, x - s * 6.7, 7, z, 0.3, 12, 4, [36, 84, 150]); // the blue portal
        box(cam, x - s * 6.9, 15.5, z, 0.6, 2.6, 1.0, GOLD); // the golden figure
        break;
      case "stadium":
        prism(cam, x, 0, z, 15, 9, 18, [216, 210, 198], [196, 188, 176], 15.5);
        prism(cam, x, 9, z, 15.5, 0.6, 18, [180, 40, 46]);
        for (const dz of [-12, 12]) {
          prism(cam, x - s * 12, 0, z + dz, 0.4, 20, 5, [160, 160, 168]);
          box(cam, x - s * 12, 20.5, z + dz, 3, 1.6, 0.5, [250, 246, 220]);
        }
        break;
      case "esplanade":
        quad(cam, [x - 5, 0.06, z + 18], [x + 5, 0.06, z + 18], [x + 5, 0.06, z - 18], [x - 5, 0.06, z - 18], [90, 150, 204], 1);
        for (let i = 0; i < 5; i += 1) {
          for (const dx of [-7, 7]) {
            box(cam, x + dx, 4, z - 16 + i * 8, 1.4, 8, 1.4, [232, 216, 184]);
            box(cam, x + dx, 8.4, z - 16 + i * 8, 1.6, 0.8, 1.6, GOLD);
          }
        }
        break;
      case "robotstatue": {
        // Deep Ellum's giant silver robot, rising out of the ground
        const m = [196, 202, 210];
        box(cam, x + s * 1, 3, z - 1, 2, 6, 2, m);
        box(cam, x - s * 1, 3, z + 1.4, 2, 6, 2, m);
        box(cam, x, 9.5, z, 5, 7, 3.4, m);
        box(cam, x, 14.4, z, 3, 2.8, 3, m);
        box(cam, x - s * 0.6, 14.6, z + 1.6, 1.8, 0.5, 0.2, [80, 220, 255]);
        box(cam, x - s * 3.4, 12, z, 1.4, 6, 1.4, m);
        box(cam, x + s * 3.4, 10, z, 1.4, 5, 1.4, m);
        break;
      }
      case "lakewood":
        box(cam, x, 4.5, z, 14, 9, 16, [222, 208, 182]);
        box(cam, x - s * 5.5, 11, z + 8.4, 2.2, 22, 1.2, [234, 226, 206]);
        facing(cam, x - s * 4.4, x - s * 6.6, 9, 20, z + 9.05, marquee("LAKEWOOD", "#1b6fd1"));
        break;
      case "granada":
        box(cam, x, 5, z, 13, 10, 16, [196, 160, 120]);
        box(cam, x - s * 2, 7.4, z + 8.6, 9, 2.4, 1.4, [52, 52, 60]);
        facing(cam, x - s * 6.5, x + s * 2.5, 6.4, 8.4, z + 9.35, marquee("GRANADA", "#c8102e"));
        break;
      case "lake":
        quad(cam, [x - s * 10, 0.05, z + 14], [x + s * 14, 0.05, z + 14], [x + s * 14, 0.05, z - 14], [x - s * 10, 0.05, z - 14], [74, 136, 186], 1);
        box(cam, x - s * 12, 2.4, z, 5, 4.8, 9, [240, 236, 224]); // the bath house
        for (const [dx, dz] of [[-4, -8], [3, 4], [6, -14]]) {
          face(cam, [[x + dx, 0.4, z + dz], [x + dx, 6, z + dz], [x + dx + 3, 0.4, z + dz]], WHITE, 1);
        }
        break;
      case "arboretum":
        for (let i = 0; i < 5; i += 1) tree(cam, x + s * 3, z - 12 + i * 6, 2.4, [70, 140, 80]);
        for (let i = 0; i < 10; i += 1) ball(cam, x - s * 4 + (i % 3) * s, 0.6, z - 14 + i * 3, 0.8, [[240, 120, 170], [250, 200, 60], [190, 110, 220], [250, 140, 60]][i % 4]);
        break;
      case "mansion":
        box(cam, x, 4, z, 12, 8, 16, [236, 230, 214]);
        prism(cam, x, 8, z, 10, 4, 4, [92, 72, 64], null, 0.2);
        for (const dz of [-4.5, -1.5, 1.5, 4.5]) prism(cam, x - s * 6.4, 0, z + dz, 0.35, 7, 6, WHITE);
        break;

      /* ── West Dallas ── */
      case "archbridge":
        prism(cam, 0, 12, z, 1.2, 24, 6, WHITE);
        for (let i = 0; i <= 12; i += 1) {
          const t = i / 12;
          const ax = (t - 0.5) * 62;
          const ay = 24 + Math.cos((t - 0.5) * Math.PI) * 15;
          box(cam, ax, ay, z, 3.6, 1.6, 2.2, WHITE);
          if (i % 2 === 0) line(cam, [ax, ay - 0.8, z], [0, 34, z], [240, 240, 240], 1.4);
        }
        // the deck crosses overhead on piers set back from the road
        box(cam, 0, 7.6, z - 3, 64, 0.9, 3, [196, 196, 200]);
        box(cam, 0, 8.15, z - 3, 64, 0.2, 3.1, [196, 196, 200]);
        for (const px of [-11, 11]) box(cam, px, 3.6, z - 3, 1.4, 7.2, 1.4, [176, 176, 182]);
        break;
      case "footbridge":
        box(cam, x, 6.4, z, 7, 0.8, 44, [206, 204, 198]);
        for (let i = 0; i < 4; i += 1) {
          const pz = z - 16.5 + i * 11;
          box(cam, x, 3, pz, 6, 6, 1.4, [190, 188, 182]);
          box(cam, x, 2.2, pz, 3.6, 4.4, 1.5, [120, 150, 170]); // arch opening
        }
        line(cam, [x - s * 3.4, 7.8, z + 22], [x - s * 3.4, 7.8, z - 22], [120, 124, 130], 1.4);
        break;
      case "groves": {
        const cols = [[220, 120, 80], [90, 150, 190], [236, 200, 100], [120, 170, 120]];
        for (let i = 0; i < 4; i += 1) box(cam, x, 3, z - 12 + i * 8, 10, 6, 7.4, cols[i]);
        for (let i = 0; i < 3; i += 1) line(cam, [x - s * 5.2, 5.8, z - 12 + i * 8], [x - s * 5.2, 5.4, z - 4 + i * 8], [255, 230, 150], 2);
        break;
      }
      case "gateway":
        for (const dz of [-5, 5]) box(cam, x - s * 8, 3.5, z + dz, 1, 7, 1, [70, 72, 80]);
        box(cam, x - s * 8, 7.4, z, 1, 0.8, 11, [70, 72, 80]);
        facing(cam, x - s * 4, x - s * 12, 8, 10.4, z + 0.6, marquee("BISHOP ARTS", "#7a3fc4"));
        box(cam, x + s * 2, 4, z, 10, 8, 16, [182, 96, 70]);
        break;
      case "texastheatre":
        box(cam, x, 5, z, 13, 10, 16, [214, 196, 160]);
        box(cam, x - s * 6.2, 9, z + 8.4, 1.2, 14, 1.2, [200, 40, 46]);
        facing(cam, x - s * 4.2, x - s * 8.2, 7, 17, z + 9.05, marquee("TEXAS", "#c8102e"));
        break;
      case "kessler":
        box(cam, x, 4.5, z, 12, 9, 14, [150, 120, 100]);
        facing(cam, x - s * 6, x + s * 2, 6, 8.2, z + 7.4, marquee("KESSLER", "#0b6b3a"));
        break;
      case "belmont":
        box(cam, x, 3.5, z - 6, 14, 7, 9, [244, 242, 236]);
        box(cam, x + s * 2, 3.5, z + 6, 10, 7, 9, [244, 242, 236]);
        for (let i = 0; i < 4; i += 1) box(cam, x - s * 7.1, 2.4, z - 9 + i * 2.2, 0.2, 1.8, 1.2, [52, 112, 170]);
        break;
      case "gallery":
        box(cam, x + s * 3, 4, z, 10, 8, 18, [176, 180, 184]);
        box(cam, x - s * 6, 4, z, 1.2, 8, 1.2, [220, 50, 50], 0.6);
        box(cam, x - s * 6, 7, z + 2, 6, 1.2, 1.2, [220, 50, 50], 0.4);
        box(cam, x - s * 7, 2, z - 4, 2.4, 4, 2.4, [40, 110, 200], 0.8);
        break;
      case "giraffe": {
        const g = [226, 172, 92];
        for (const [dx, dz] of [[-1.2, -2], [1.2, -2], [-1.2, 2], [1.2, 2]]) box(cam, x + dx, 4, z + dz, 0.6, 8, 0.6, g);
        box(cam, x, 9, z, 3, 2.6, 6, g);
        box(cam, x, 13, z - 3, 1.2, 7, 1.2, g);
        box(cam, x, 17, z - 4.6, 1.2, 4, 1.2, g);
        box(cam, x, 19.4, z - 5.4, 1.6, 1.4, 2.6, g);
        for (const [dx, dy, dz] of [[1.55, 9.4, -1], [-1.55, 8.8, 1.4], [0.65, 13, -3], [-0.65, 15, -3.6]]) box(cam, x + dx, dy, z + dz, 0.06, 0.6, 0.8, [140, 90, 50]);
        break;
      }
      case "overlook":
        prism(cam, x, 0, z, 12, 3, 8, [104, 150, 90], null, 9);
        box(cam, x, 3.4, z, 8, 0.4, 8, [196, 196, 200]);
        line(cam, [x - 4, 4.4, z + 4], [x + 4, 4.4, z + 4], [90, 94, 100], 1.4);
        tree(cam, x + 6, z - 6, 1.8, [84, 150, 86]);
        break;

      /* ── Toronto ── */
      case "cntower":
        prism(cam, x, 0, z, 3.4, 44, 3, [212, 208, 200], null, 1.3);
        prism(cam, x, 40, z, 2.4, 1.2, 12, [190, 186, 180], null, 5.4);
        prism(cam, x, 41.2, z, 5.4, 2.8, 12, [170, 168, 166], null, 5.4);
        prism(cam, x, 44, z, 5.4, 1.2, 12, [190, 186, 180], null, 2.2);
        prism(cam, x, 45.2, z, 1.2, 9, 6, [212, 208, 200], null, 0.9);
        prism(cam, x, 54, z, 1.6, 1.4, 8, [176, 172, 168]);
        prism(cam, x, 55.4, z, 0.5, 14, 5, [224, 54, 54], null, 0.12);
        break;
      case "dome":
        prism(cam, x, 0, z, 16, 8, 18, [206, 206, 210], [186, 186, 192]);
        prism(cam, x, 8, z, 16, 4, 18, [226, 228, 232], null, 11);
        prism(cam, x, 12, z, 11, 2.6, 18, [234, 236, 240], null, 5);
        break;
      case "tocityhall":
        box(cam, x + s * 3, 13, z - 8, 4, 26, 11, [226, 226, 220]);
        box(cam, x + s * 3, 10, z + 8, 4, 20, 11, [226, 226, 220]);
        prism(cam, x + s * 4, 0, z, 5.4, 4, 14, [236, 236, 232], null, 6.2);
        facing(cam, side * 14, side * 26, 0.2, 2.6, z + 10, paintTorontoSign);
        break;
      case "flatiron":
        prism(cam, x, 0, z, 8, 15, 3, [168, 74, 58]);
        prism(cam, x, 15, z, 8, 3.6, 3, [72, 128, 104], null, 6);
        break;
      case "union":
        box(cam, x, 6, z, 10, 12, 42, [214, 204, 184]);
        for (let k = 0; k < 12; k += 1) prism(cam, x - s * 5.4, 0, z - 18 + k * 3.3, 0.5, 10, 8, [232, 224, 206]);
        box(cam, x - s * 5.4, 10.6, z, 1.4, 1.2, 42, [204, 194, 172]);
        break;
      case "crystal":
        box(cam, x + s * 3, 6, z, 10, 12, 18, [196, 168, 132]);
        box(cam, x - s * 3, 9, z + 2, 9, 9, 10, [196, 204, 214], 0.5);
        box(cam, x - s * 5, 12, z - 4, 6, 7, 6, [176, 188, 202], -0.6);
        break;
      case "castle":
        box(cam, x, 6, z, 14, 12, 18, [190, 168, 128]);
        for (const [dx, dz, hh] of [[-6, -8, 18], [-6, 8, 15], [5, 0, 22]]) {
          prism(cam, x + s * dx, 0, z + dz, 2.2, hh, 8, [196, 174, 134]);
          prism(cam, x + s * dx, hh, z + dz, 2.6, 3.4, 8, [70, 96, 86], null, 0.1);
        }
        break;
      case "distillery":
        for (let i = 0; i < 3; i += 1) box(cam, x, 4.5, z - 12 + i * 10, 11, 9, 8.6, [160, 72, 54]);
        prism(cam, x + s * 4, 0, z + 2, 1.4, 22, 8, [150, 66, 50], null, 1.1);
        facing(cam, x - s * 5.6, x - s * 1.4, 6.4, 8, z + 13.4, paintGhost("DISTILLERY"));
        break;
      case "kensington": {
        for (let i = 0; i < 5; i += 1) {
          const c = ROWHOUSE[i % ROWHOUSE.length];
          box(cam, x, 3.4, z - 14 + i * 7, 9, 6.8, 6.4, c);
          prism(cam, x, 6.8, z - 14 + i * 7, 4.6, 2.4, 4, rgbMul(c, 0.7), null, 0.2);
        }
        break;
      }
      case "market":
        box(cam, x, 6, z, 14, 12, 30, [170, 82, 60]);
        prism(cam, x, 12, z, 9, 2.2, 4, [90, 98, 104], null, 0.4);
        box(cam, x - s * 7.1, 9, z, 0.3, 4, 6, [236, 230, 214]);
        break;
      default:
        box(cam, x, 14, z, 12, 28, 12, [110, 104, 128]);
    }
  }

  /* ── frame ─────────────────────────────────────────────────────────── */

  /** Extrapolate a near→far screen edge down past the bottom of the frame. */
  function toBottom(pts) {
    if (pts.length < 2) return pts;
    const a = pts[0];
    const b = pts[1];
    if (a.y >= H * 1.25 || a.y <= b.y) return pts;
    const t = (H * 1.25 - a.y) / (a.y - b.y);
    return [{ x: a.x + (a.x - b.x) * t, y: H * 1.25 }, ...pts];
  }

  function paintWorld(cam, run) {
    // drawn oversized so the camera can bank without exposing the corners
    const g = ctx.createLinearGradient(0, 0, 0, H * 0.7);
    g.addColorStop(0, rgb(SKY.top));
    g.addColorStop(0.5, rgb(SKY.mid));
    g.addColorStop(0.88, rgb(SKY.low));
    g.addColorStop(1, rgb(SKY.haze));
    ctx.fillStyle = g;
    ctx.fillRect(-W * 0.2, -H * 0.2, W * 1.4, H * 1.4);

    const horizon = proj(cam, 0, 0, -250);
    const hy = horizon ? horizon.y : H * 0.4;

    // low Texas sun
    const sunX = cx + W * 0.18 - run.x * 4;
    const sun = ctx.createRadialGradient(sunX, hy - 40, 4, sunX, hy - 40, Math.max(W, H) * 0.45);
    sun.addColorStop(0, "rgba(255, 240, 200, 0.95)");
    sun.addColorStop(0.08, "rgba(255, 214, 150, 0.55)");
    sun.addColorStop(1, "rgba(255, 190, 130, 0)");
    ctx.fillStyle = sun;
    ctx.fillRect(-W * 0.2, -H * 0.2, W * 1.4, hy + H * 0.2);

    // clouds, drifting slowly
    ctx.fillStyle = "rgba(255,255,255,0.7)";
    for (let i = 0; i < 5; i += 1) {
      const px = ((i * 211 - run.distance * 0.04) % (W + 300) + W + 300) % (W + 300) - 150;
      const py = hy * (0.14 + (i % 3) * 0.13);
      ctx.beginPath();
      ctx.ellipse(px, py, 60 + (i % 2) * 30, 14, 0, 0, Math.PI * 2);
      ctx.ellipse(px + 40, py - 9, 40, 16, 0, 0, Math.PI * 2);
      ctx.fill();
    }

    // the city skyline on the horizon, framed between the street walls
    const sx0 = cx - run.x * 7 - ((run.distance * 0.02) % 60);
    ctx.fillStyle = R.skyline === "toronto" ? "rgb(140, 150, 178)" : "rgb(150, 146, 184)";
    ctx.beginPath();
    for (let i = -14; i < 14; i += 1) {
      const bw = 18 + hash(i + 400) * 26;
      const bh = 24 + hash(i + 900) * (R.skyline === "toronto" ? 90 : 70);
      ctx.rect(sx0 + i * 30, hy - bh, bw, bh);
    }
    ctx.fill();
    if (R.skyline === "toronto") {
      ctx.fillStyle = "rgb(118, 128, 160)";
      // the CN Tower: tapering shaft, the pod, the antenna
      const tx = sx0 - 70;
      ctx.beginPath();
      ctx.moveTo(tx - 9, hy);
      ctx.lineTo(tx - 3, hy - 200);
      ctx.lineTo(tx + 3, hy - 200);
      ctx.lineTo(tx + 9, hy);
      ctx.fill();
      ctx.beginPath();
      ctx.ellipse(tx, hy - 196, 15, 8, 0, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillRect(tx - 2, hy - 250, 4, 50);
      ctx.fillRect(tx - 5, hy - 236, 10, 6);
      ctx.fillStyle = "rgba(255, 236, 200, 0.85)";
      ctx.fillRect(tx - 13, hy - 197, 26, 2);
      ctx.fillStyle = "rgb(118, 128, 160)";
      // the Rogers Centre dome
      ctx.beginPath();
      ctx.ellipse(sx0 - 12, hy, 44, 26, 0, Math.PI, Math.PI * 2);
      ctx.fill();
      // First Canadian Place and the TD towers
      ctx.fillRect(sx0 + 40, hy - 178, 26, 178);
      ctx.fillRect(sx0 + 74, hy - 140, 18, 140);
      ctx.fillStyle = "rgb(60, 64, 80)";
      ctx.fillRect(sx0 + 100, hy - 150, 20, 150);
    } else {
      ctx.fillStyle = "rgb(128, 124, 168)";
      // Reunion Tower: the ball on a stem
      const rx = sx0 - 96;
      ctx.fillRect(rx - 4, hy - 150, 8, 150);
      ctx.beginPath();
      ctx.arc(rx, hy - 158, 17, 0, Math.PI * 2);
      ctx.fill();
      ctx.fillStyle = "rgba(255, 236, 200, 0.85)";
      for (let k = -2; k <= 2; k += 1) {
        ctx.beginPath();
        ctx.arc(rx + k * 6, hy - 158 + Math.abs(k) * 1.5, 1.6, 0, Math.PI * 2);
        ctx.fill();
      }
      ctx.fillStyle = "rgb(128, 124, 168)";
      // Bank of America Plaza, with its green outline
      const bx = sx0 + 34;
      ctx.fillRect(bx, hy - 172, 30, 172);
      ctx.strokeStyle = "rgb(96, 220, 130)";
      ctx.lineWidth = 2;
      ctx.strokeRect(bx + 1, hy - 171, 28, 171);
      // Fountain Place: the slanted prism
      ctx.beginPath();
      ctx.moveTo(sx0 + 84, hy);
      ctx.lineTo(sx0 + 84, hy - 120);
      ctx.lineTo(sx0 + 104, hy - 150);
      ctx.lineTo(sx0 + 118, hy - 112);
      ctx.lineTo(sx0 + 118, hy);
      ctx.fill();
      // Comerica Bank Tower: the stepped crown
      const cxx = sx0 - 44;
      ctx.fillRect(cxx, hy - 128, 28, 128);
      ctx.fillRect(cxx + 4, hy - 140, 20, 12);
      ctx.fillRect(cxx + 9, hy - 148, 10, 8);
    }

    // ground
    const north = R.flag === "canada";
    ctx.fillStyle = rgb(north ? [178, 176, 170] : [200, 176, 156]);
    ctx.fillRect(-W * 0.2, hy, W * 1.4, H * 1.2 - hy);
    let strip = [];
    for (let i = 14; i >= -200; i -= 4) {
      const p = proj(cam, -FACE_X, 0.19, i);
      if (p) strip.push(p);
    }
    let stripR = [];
    for (let i = 14; i >= -200; i -= 4) {
      const p = proj(cam, FACE_X, 0.19, i);
      if (p) stripR.push(p);
    }
    if (strip.length > 2 && stripR.length > 2) {
      strip = toBottom(strip);
      stripR = toBottom(stripR);
      ctx.beginPath();
      ctx.moveTo(strip[0].x, strip[0].y);
      for (const p of strip) ctx.lineTo(p.x, p.y);
      for (let i = stripR.length - 1; i >= 0; i -= 1) ctx.lineTo(stripR[i].x, stripR[i].y);
      ctx.closePath();
      ctx.fillStyle = rgb(north ? [206, 204, 198] : [222, 204, 184]);
      ctx.fill();
    }
    // sidewalk paving joints scroll by: a strong speed cue at the edges
    ctx.strokeStyle = "rgba(150, 120, 96, 0.35)";
    ctx.lineWidth = 1.5;
    const jOff = -(run.distance % 3);
    ctx.beginPath();
    for (let i = 2; i > -90; i -= 3) {
      const z = i + jOff;
      for (const s of [-1, 1]) {
        const a = proj(cam, s * ROAD_HALF, 0.2, z);
        const b = proj(cam, s * FACE_X, 0.2, z);
        if (!a || !b) continue;
        ctx.moveTo(a.x, a.y);
        ctx.lineTo(b.x, b.y);
      }
    }
    ctx.stroke();

    const roadPts = (halfW) => {
      const pts = [];
      for (let i = 14; i >= -200; i -= 6) {
        const p = proj(cam, halfW, 0.01, i);
        if (p) pts.push(p);
      }
      return pts;
    };
    const left = toBottom(roadPts(-ROAD_HALF));
    const right = toBottom(roadPts(ROAD_HALF));
    if (left.length > 2 && right.length > 2) {
      ctx.beginPath();
      ctx.moveTo(left[0].x, left[0].y);
      for (const p of left) ctx.lineTo(p.x, p.y);
      for (let i = right.length - 1; i >= 0; i -= 1) ctx.lineTo(right[i].x, right[i].y);
      ctx.closePath();
      const roadGrad = ctx.createLinearGradient(0, hy, 0, H);
      roadGrad.addColorStop(0, rgb([126, 118, 116]));
      roadGrad.addColorStop(1, rgb([78, 76, 82]));
      ctx.fillStyle = roadGrad;
      ctx.fill();
    }

    // DART rail track in each lane: sleepers, then the two rails
    const tOff = -(run.distance % 2.2);
    ctx.fillStyle = "rgba(70, 58, 50, 0.75)";
    for (const lx of LANE_X) {
      for (let i = 4; i > -96; i -= 2.2) {
        const z = i + tOff;
        const a = proj(cam, lx - 0.98, 0.02, z);
        const b = proj(cam, lx + 0.98, 0.02, z);
        const c = proj(cam, lx + 0.98, 0.02, z - 0.5);
        const d = proj(cam, lx - 0.98, 0.02, z - 0.5);
        if (!a || !b || !c || !d) continue;
        ctx.beginPath();
        ctx.moveTo(a.x, a.y);
        ctx.lineTo(b.x, b.y);
        ctx.lineTo(c.x, c.y);
        ctx.lineTo(d.x, d.y);
        ctx.closePath();
        ctx.fill();
      }
    }
    for (const lx of LANE_X) {
      for (const rxo of [-0.66, 0.66]) {
        let l = [];
        let r = [];
        for (let i = 12; i >= -170; i -= 8) {
          const a = proj(cam, lx + rxo - 0.07, 0.05, i);
          const b = proj(cam, lx + rxo + 0.07, 0.05, i);
          if (a && b) {
            l.push(a);
            r.push(b);
          }
        }
        if (l.length < 2) continue;
        l = toBottom(l);
        r = toBottom(r);
        ctx.beginPath();
        ctx.moveTo(l[0].x, l[0].y);
        for (const p of l) ctx.lineTo(p.x, p.y);
        for (let i = r.length - 1; i >= 0; i -= 1) ctx.lineTo(r[i].x, r[i].y);
        ctx.closePath();
        ctx.fillStyle = "rgb(206, 210, 220)";
        ctx.fill();
      }
    }

    // concrete curbs, alternately weathered so they scroll by
    for (const sx of [-ROAD_HALF, ROAD_HALF]) {
      const cOff = -(run.distance % 4);
      for (let i = -2; i > -120; i -= 4) {
        const z = i + cOff;
        quad(cam, [sx, 0, z], [sx, 0, z - 2], [sx, 0.2, z - 2], [sx, 0.2, z], [196, 194, 188], 1);
        quad(cam, [sx, 0, z - 2], [sx, 0, z - 4], [sx, 0.2, z - 4], [sx, 0.2, z - 2], [160, 158, 154], 1);
      }
    }
  }

  let lastNow = 0;
  function draw(run, opts) {
    const t0 = performance.now();
    R = routeById(opts && opts.route);
    SKY = R.sky;
    faces = [];
    lines = [];
    balls = [];
    decals = [];
    badges = [];
    const now = (opts && opts.now) || t0;
    const dt = lastNow ? Math.max(0.001, Math.min(0.05, (now - lastNow) / 1000)) : 1 / 60;
    lastNow = now;
    updateFx(run, dt);

    // a wider lens as the pace climbs, and a punch on take-off
    const pace = Math.max(0, Math.min(1, (run.speed - 20) / 26));
    focal = focalBase * (1 - 0.12 * pace - 0.05 * Math.sin(Math.PI * fx.kick) - (run.jet > 0 ? 0.06 : 0));

    const dip = run.slide > 0 ? 0.9 : 0;
    const landDip = Math.sin(Math.PI * fx.land) * 0.28;
    const camY = CAM.h + fx.visY * 0.62 - dip - landDip;
    const cam = makeCamera(fx.camX, camY, CAM.back - pace * 0.6, fx.visX * 0.16, CAM.lookY + fx.visY * 0.72 - dip * 0.4, -CAM.lookAhead);

    ctx.save();
    ctx.translate(cx, cy);
    ctx.rotate(fx.bank);
    ctx.translate(-cx, -cy);
    paintWorld(cam, run);

    for (const o of run.objects) {
      const rel = o.z - run.distance;
      if (rel < -30 || rel > 220) continue;
      const wz = -rel;
      if (o.t === 2) {
        if (o.k === "store") storefront(cam, Math.floor(o.z) % 2 === 0 ? 1 : -1, wz);
        else landmark(cam, R.landmarks[o.n % R.landmarks.length].model, o.l === 0 ? -1 : 1, wz);
        continue;
      }
      let x = LANE_X[o.l];
      if (o.t === 0) {
        if (rel < -1) continue;
        let y = o.h + Math.sin(run.tick * 0.08 + o.z) * 0.08;
        // the magnet visibly pulls nearby drumsticks in
        if (o.k === "coin" && run.magnet > 0 && rel < 14 && (o.h > JET_HEIGHT) === run.jet > 0) {
          const f = 1 - Math.max(0, rel) / 14;
          x += (run.x - x) * f;
          y += (run.y + 1 - y) * f * 0.8;
        }
        if (o.k === "coin") drumstick(cam, x, y, wz);
        else {
          item(cam, o.k, x, y, wz);
          decals.push({ x, y: 0, z: wz, r: 0.6, color: [255, 200, 120], alpha: 0.35, ring: true });
        }
      } else if (o.t === 3) {
        if (rel < -1) continue;
        const p = proj(cam, x, o.h + Math.sin(run.tick * 0.1) * 0.12, wz);
        if (p) badges.push({ p, r: (focal * 0.5) / p.d, kind: o.k, depth: p.d });
      } else {
        obstacle(cam, o, x, wz);
      }
    }
    city(cam, run);
    chaser(cam, run);
    runner(cam, run, (opts && opts.outfit) || "dante", now);

    // one depth-sorted list for faces, discs, lines and badges
    const all = faces;
    for (const b of balls) all.push({ ball: b, depth: b.depth });
    for (const l of lines) all.push({ line: l, depth: l.depth });
    for (const b of badges) all.push({ badge: b, depth: b.depth });
    all.sort((a, b) => b.depth - a.depth);

    for (const d of decals) {
      const p = proj(cam, d.x, (d.y || 0) + 0.015, d.z);
      if (!p) continue;
      const r = (focal * d.r) / p.d;
      if (r < 1) continue;
      ctx.beginPath();
      ctx.ellipse(p.x, p.y, r, r * 0.34, 0, 0, Math.PI * 2);
      const paint = `rgba(${d.color[0]}, ${d.color[1]}, ${d.color[2]}, ${d.alpha})`;
      if (d.ring) {
        ctx.strokeStyle = paint;
        ctx.lineWidth = Math.max(1, r * 0.16);
        ctx.stroke();
      } else {
        ctx.fillStyle = paint;
        ctx.fill();
      }
    }

    polys = 0;
    let lastColor = "";
    for (const f of all) {
      if (f.ball) {
        const b = f.ball;
        ctx.beginPath();
        ctx.arc(b.p.x, b.p.y, b.radius, 0, Math.PI * 2);
        ctx.fillStyle = b.color;
        ctx.fill();
        if (b.ring) {
          ctx.strokeStyle = "rgba(60, 70, 90, 0.4)";
          ctx.lineWidth = 1;
          for (let i = -2; i <= 2; i += 1) {
            ctx.beginPath();
            ctx.ellipse(b.p.x, b.p.y, Math.abs(b.radius * (i / 3)) + 0.5, b.radius, 0, 0, Math.PI * 2);
            ctx.stroke();
          }
        }
        lastColor = "";
        continue;
      }
      if (f.line) {
        const l = f.line;
        ctx.beginPath();
        ctx.moveTo(l.p.x, l.p.y);
        ctx.lineTo(l.q.x, l.q.y);
        ctx.strokeStyle = l.color;
        ctx.lineWidth = l.width;
        ctx.stroke();
        continue;
      }
      if (f.badge) {
        const b = f.badge;
        const st = POWER_STYLE[b.kind];
        const pulse = 1 + Math.sin(now * 0.01) * 0.06;
        const r = b.r * pulse;
        ctx.beginPath();
        ctx.arc(b.p.x, b.p.y, r * 1.25, 0, Math.PI * 2);
        ctx.fillStyle = "rgba(255,255,255,0.5)";
        ctx.fill();
        ctx.beginPath();
        ctx.arc(b.p.x, b.p.y, r, 0, Math.PI * 2);
        ctx.fillStyle = rgb(st.color);
        ctx.fill();
        ctx.lineWidth = Math.max(1.5, r * 0.12);
        ctx.strokeStyle = "#fff";
        ctx.stroke();
        ctx.fillStyle = "#fff";
        ctx.font = `900 ${Math.max(8, r * 1.05)}px Oswald, system-ui, sans-serif`;
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText(st.glyph, b.p.x, b.p.y + r * 0.06);
        lastColor = "";
        continue;
      }
      if (f.paint) {
        if (f.round) {
          // the logo sits in a circle: clip it so its white corners vanish
          const [a, , c] = f.out;
          const mx = (a.x + c.x) / 2;
          const my = (a.y + c.y) / 2;
          const rr = Math.min(Math.abs(c.x - a.x), Math.abs(c.y - a.y)) / 2;
          ctx.save();
          ctx.beginPath();
          ctx.arc(mx, my, rr, 0, Math.PI * 2);
          ctx.clip();
          onQuad(f.out, f.paint);
          ctx.restore();
        } else onQuad(f.out, f.paint);
        lastColor = "";
        polys += 1;
        continue;
      }
      if (f.color !== lastColor) {
        ctx.fillStyle = f.color;
        lastColor = f.color;
      }
      ctx.beginPath();
      ctx.moveTo(f.out[0].x, f.out[0].y);
      for (let i = 1; i < f.out.length; i += 1) ctx.lineTo(f.out[i].x, f.out[i].y);
      ctx.closePath();
      ctx.fill();
      polys += 1;
    }

    // dust, sparkles: drawn last, they always sit around the runner
    for (const p of fx.particles) {
      const q = proj(cam, p.x, p.y, p.z);
      if (!q || q.d < 3) continue;
      const r = Math.min(18, (focal * p.r * (p.spark ? 1 : 1 + p.life * 1.5)) / q.d);
      ctx.fillStyle = `rgba(${p.color[0]}, ${p.color[1]}, ${p.color[2]}, ${0.7 * (1 - p.life / p.max)})`;
      ctx.beginPath();
      ctx.arc(q.x, q.y, r, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.restore();

    // speed streaks grow with pace: the "fast" feeling
    const k = Math.max(0, (run.speed - 22) / 20) + (run.sneakers > 0 ? 0.25 : 0);
    if (k > 0 || run.jet > 0) {
      ctx.strokeStyle = `rgba(255, 255, 255, ${0.14 + 0.32 * Math.min(1, k + (run.jet > 0 ? 0.5 : 0))})`;
      ctx.lineWidth = 2.5;
      for (let i = 0; i < 18; i += 1) {
        const a = (i / 18) * Math.PI * 2 + (run.tick % 7) * 0.13;
        const r0 = Math.min(W, H) * (0.4 + ((i * 37 + run.tick * 3) % 20) / 100);
        const x0 = cx + Math.cos(a) * r0;
        const y0 = cy * 0.8 + Math.sin(a) * r0 * 0.7;
        ctx.beginPath();
        ctx.moveTo(x0, y0);
        ctx.lineTo(x0 + Math.cos(a) * 46 * (0.5 + k), y0 + Math.sin(a) * 32 * (0.5 + k));
        ctx.stroke();
      }
    }

    return { polys, ms: performance.now() - t0, district: districtAt(run.distance).name };
  }

  resize();
  return { resize, draw };
}

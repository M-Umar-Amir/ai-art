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

const ROAD_HALF = 4.6;
const SIDEWALK_EDGE = 8.2;
const LANE_DIV = 1.15;
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

const SKY = { top: [64, 132, 214], mid: [128, 184, 240], low: [255, 220, 180], haze: [226, 214, 210] };

export const CAM = {
  h: 3.1,
  back: 6.4,
  lookY: 0.8,
  lookAhead: 9,
  cy: 0.6,
  focal: 1.38,
};

function rgb(c, mul) {
  const f = mul === undefined ? 1 : mul;
  return `rgb(${Math.min(255, Math.round(c[0] * f))},${Math.min(255, Math.round(c[1] * f))},${Math.min(255, Math.round(c[2] * f))})`;
}

function fogMix(c, depth) {
  const t = Math.max(0, Math.min(0.8, (depth - 50) / 200));
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
    focal = Math.min(W, H) * (W > H * 1.2 ? CAM.focal * 0.8 : CAM.focal);
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
    if (maxX < -40 || minX > W + 40 || maxY < -40 || minY > H + 40) return;
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

  /* ── roadside furniture ─────────────────────────────────────────────── */

  function streetFurniture(cam, run) {
    const base = Math.floor(run.distance / 26);
    for (let i = 0; i < 9; i += 1) {
      const gz = (base + i) * 26 - run.distance;
      if (gz < -4) continue;
      const wz = -gz;
      const side = (base + i) % 2 === 0 ? -1 : 1;
      const px = side * 6.6;
      prism(cam, px, 0, wz, 0.14, 4.8, 5, [96, 100, 112]);
      box(cam, px - side * 0.6, 4.8, wz, 1.4, 0.16, 0.3, [96, 100, 112]);
      box(cam, px - side * 1.2, 4.66, wz, 0.5, 0.14, 0.34, [255, 236, 190]);
      const q = (base + i) % 4;
      if (q === 0) {
        // a palm: Dallas, after all
        prism(cam, px + side * 0.9, 0, wz - 3, 0.18, 5.4, 5, [140, 108, 76]);
        ball(cam, px + side * 0.9, 5.6, wz - 3, 1.5, [80, 150, 84]);
      } else if (q === 1) box(cam, px - side * 1.6, 0.4, wz, 1.4, 0.2, 0.5, [150, 112, 78]);
      else if (q === 2) prism(cam, px - side * 1.6, 0, wz, 0.3, 0.8, 6, RED, WHITE);
      else prism(cam, px - side * 1.6, 0, wz, 0.14, 0.7, 5, [200, 60, 60]);
    }
  }

  /* ── the runner ────────────────────────────────────────────────────── */

  function runner(cam, run, outfit, t) {
    const x = run.x;
    const onBoard = run.board > 0;
    const y = run.y + (onBoard ? 0.22 : 0);
    const sliding = run.slide > 0;
    const flying = run.jet > 0;
    const down = Boolean(run.downed);
    if (run.invuln > 0 && !down && Math.floor(t / 70) % 2 === 0 && run.invuln < 140) return;

    const skin = [228, 180, 146];
    const kit =
      outfit === "gold" ? GOLD : outfit === "apron" ? [248, 248, 246] : outfit === "tie" ? WHITE : outfit === "bucket" ? RED : RED;
    const kitDeep = outfit === "gold" ? [214, 150, 30] : outfit === "apron" ? [222, 222, 222] : RED_DEEP;
    const legs = outfit === "apron" ? [60, 60, 66] : INK;
    const squash = down ? 0.25 : sliding ? 0.5 : 1;
    const stride = sliding || flying || down ? 0 : Math.sin(run.tick * 0.42);
    const bob = sliding || flying ? 0 : Math.abs(Math.cos(run.tick * 0.42)) * 0.07;
    const lean = sliding ? 0 : 0.08;
    const legZ = stride * 0.46;

    if (onBoard) {
      box(cam, x, y - 0.14, 0.05, 0.7, 0.08, 1.6, RED);
      box(cam, x, y - 0.1, 0.05, 0.26, 0.02, 1.62, WHITE);
      decals.push({ x, z: 0.05, r: 0.8, color: [255, 120, 140], alpha: 0.35 });
    }
    box(cam, x - 0.17, y + 0.3 * squash, legZ + lean, 0.22, 0.6 * squash, 0.24, legs);
    box(cam, x + 0.17, y + 0.3 * squash, -legZ + lean, 0.22, 0.6 * squash, 0.24, legs);
    box(cam, x - 0.17, y + 0.07 * squash, legZ + lean - 0.07, 0.25, 0.15, 0.36, WHITE);
    box(cam, x + 0.17, y + 0.07 * squash, -legZ + lean - 0.07, 0.25, 0.15, 0.36, WHITE);

    box(cam, x, y + 0.98 * squash + bob, lean, 0.7, 0.76 * squash, 0.44, kit);
    // the delivery bag on the back, branded
    box(cam, x, y + 1.14 * squash + bob, lean + 0.32, 0.56, 0.56 * squash, 0.26, outfit === "gold" ? RED : WHITE);
    box(cam, x, y + 1.14 * squash + bob, lean + 0.46, 0.4, 0.2 * squash, 0.02, RED);
    if (outfit === "tie") box(cam, x, y + 1.0 * squash + bob, lean - 0.23, 0.1, 0.46 * squash, 0.02, INK);
    if (flying) {
      box(cam, x - 0.18, y + 1.0, lean + 0.52, 0.22, 0.6, 0.22, [180, 186, 196]);
      box(cam, x + 0.18, y + 1.0, lean + 0.52, 0.22, 0.6, 0.22, [180, 186, 196]);
      const flick = 0.5 + 0.3 * Math.sin(t * 0.05);
      face(cam, [[x - 0.28, y + 0.7, lean + 0.52], [x - 0.08, y + 0.7, lean + 0.52], [x - 0.18, y + 0.7 - flick, lean + 0.52]], [255, 170, 40], 1.2);
      face(cam, [[x + 0.08, y + 0.7, lean + 0.52], [x + 0.28, y + 0.7, lean + 0.52], [x + 0.18, y + 0.7 - flick, lean + 0.52]], [255, 170, 40], 1.2);
    }

    const armSwing = flying ? 0 : legZ * 0.9;
    box(cam, x - 0.46, y + 1.0 * squash + bob, -armSwing + lean, 0.18, 0.64 * squash, 0.2, kit);
    box(cam, x + 0.46, y + 1.0 * squash + bob, armSwing + lean, 0.18, 0.64 * squash, 0.2, kit);
    box(cam, x - 0.46, y + 0.74 * squash + bob, -armSwing + lean, 0.2, 0.16, 0.22, skin);
    box(cam, x + 0.46, y + 0.74 * squash + bob, armSwing + lean, 0.2, 0.16, 0.22, skin);

    const headY = y + 1.62 * squash + bob;
    box(cam, x, headY, lean, 0.46, 0.44 * Math.max(0.6, squash), 0.44, skin);
    if (outfit === "bucket" || outfit === "gold") {
      prism(cam, x, headY + 0.18, lean, 0.34, 0.42, 10, outfit === "gold" ? GOLD : RED, WHITE, 0.4);
    } else if (outfit === "apron") {
      prism(cam, x, headY + 0.2, lean, 0.26, 0.5, 8, WHITE, null, 0.34);
    } else {
      box(cam, x, headY + 0.28, lean, 0.5, 0.18, 0.5, outfit === "tie" ? INK : RED);
      box(cam, x, headY + 0.27, lean - 0.33, 0.46, 0.07, 0.2, outfit === "tie" ? INK : RED_DEEP);
    }

    if (!flying) {
      const lift = Math.max(0, run.y - run.floor);
      decals.push({ x, y: run.floor, z: 0, r: 0.48 - Math.min(0.2, lift * 0.08), color: [20, 20, 30], alpha: 0.3 });
    }
  }

  /** The inspector who chases you after a stumble. Falls back as the timer runs out. */
  function chaser(cam, run) {
    const caught = run.downed === "caught";
    if (run.chase < 90 && !caught) return;
    // just behind and to one side, so he reads as a threat without hiding the road
    const z = caught ? 1.4 : 2.0 + (1 - run.chase / 300) * 1.6;
    const side = run.x > 1 ? -1 : 1;
    const x = run.x + side * (caught ? 0.8 : 1.2);
    const stride = caught ? 0 : Math.sin(run.tick * 0.42 + 1);
    const suit = [92, 98, 112];
    box(cam, x - 0.18, 0.32, z + stride * 0.4, 0.24, 0.64, 0.26, INK);
    box(cam, x + 0.18, 0.32, z - stride * 0.4, 0.24, 0.64, 0.26, INK);
    box(cam, x, 1.04, z, 0.78, 0.84, 0.48, suit);
    box(cam, x + 0.5, 1.06, z - 0.1, 0.2, 0.66, 0.22, suit);
    box(cam, x - 0.5, 1.2, z - 0.3, 0.2, 0.2, 0.6, suit);
    box(cam, x - 0.5, 1.26, z - 0.62, 0.42, 0.56, 0.05, WHITE); // clipboard
    box(cam, x, 1.72, z, 0.46, 0.44, 0.44, [222, 176, 140]);
    box(cam, x, 2.0, z, 0.64, 0.06, 0.64, INK);
    box(cam, x, 2.12, z, 0.4, 0.2, 0.4, INK);
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

  function stripedBox(cam, x, y, z, w, h, d, a, b, n) {
    // alternating bands across the width
    const bw = w / n;
    for (let i = 0; i < n; i += 1) {
      box(cam, x - w / 2 + bw * (i + 0.5), y, z, bw, h, d, i % 2 ? b : a);
    }
  }

  function van(cam, x, wz, len, oncoming) {
    // wz is the near end; the van extends away from the camera
    const zc = wz - len / 2;
    box(cam, x, 1.25, zc, 2.0, 2.0, len, WHITE);
    box(cam, x, 0.95, zc, 2.02, 0.42, len + 0.02, RED);
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
    const q = quadScreen(cam, [x - 0.8, 2.15, wz + 0.01], [x + 0.8, 2.15, wz + 0.01], [x + 0.8, 1.2, wz + 0.01], [x - 0.8, 1.2, wz + 0.01]);
    if (q && !oncoming) {
      const d = (q[0].d + q[2].d) / 2;
      faces.push({ out: q, depth: d - 0.05, color: "#fff", paint: paintWordmark() });
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
    for (let k = 0; k < seg; k += 1) {
      const zc = bodyNear - sl * (k + 0.5);
      box(cam, x, h / 2 + 0.25, zc, 2.1, h - 0.5, sl, RED);
      box(cam, x, h - 0.02, zc, 2.14, 0.06, sl, WHITE);
      for (let b = 0; b < 3; b += 1) box(cam, x, 0.7 + b * 0.5, zc, 2.16, 0.16, sl, b % 2 ? WHITE : RED_DEEP);
    }
    for (const s of [-0.95, 0.95]) {
      box(cam, x + s, 0.22, bodyNear - 1.2, 0.3, 0.44, 0.7, INK);
      box(cam, x + s, 0.22, bodyNear - bodyLen + 1.2, 0.3, 0.44, 0.7, INK);
    }
    const q = quadScreen(cam, [x - 1.0, h - 0.15, bodyNear + 0.02], [x + 1.0, h - 0.15, bodyNear + 0.02], [x + 1.0, 0.4, bodyNear + 0.02], [x - 1.0, 0.4, bodyNear + 0.02]);
    if (q) {
      const d = (q[0].d + q[2].d) / 2;
      faces.push({ out: q, depth: d - 0.05, color: "#fff", paint: ramp ? paintWordmark("#a40020") : paintWordmark() });
    }
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
        box(cam, x, 0.5, wz + 0.46, 0.7, 0.24, 0.02, RED);
        break;
      case "barrier":
        stripedBox(cam, x, 0.62, wz, 1.9, 0.36, 0.3, RED, WHITE, 5);
        box(cam, x - 0.8, 0.3, wz, 0.14, 0.6, 0.14, [120, 120, 126]);
        box(cam, x + 0.8, 0.3, wz, 0.14, 0.6, 0.14, [120, 120, 126]);
        break;
      case "bucketstack":
        prism(cam, x - 0.3, 0, wz, 0.38, 0.62, 10, RED, WHITE, 0.46);
        prism(cam, x + 0.36, 0, wz, 0.38, 0.62, 10, RED, WHITE, 0.46);
        prism(cam, x, 0.62, wz, 0.34, 0.5, 10, RED, WHITE, 0.42);
        break;
      case "bar":
        box(cam, x - 0.95, 1.1, wz, 0.16, 2.2, 0.16, [120, 120, 126]);
        box(cam, x + 0.95, 1.1, wz, 0.16, 2.2, 0.16, [120, 120, 126]);
        stripedBox(cam, x, 1.75, wz, 2.0, 0.7, 0.24, RED, WHITE, 6);
        break;
      case "awning": {
        if (o.l !== 1) break; // one awning spans the road; draw it once
        box(cam, -ROAD_HALF - 0.2, 1.1, wz, 0.22, 2.2, 0.22, RED_DEEP);
        box(cam, ROAD_HALF + 0.2, 1.1, wz, 0.22, 2.2, 0.22, RED_DEEP);
        stripedBox(cam, 0, 1.95, wz, 9.6, 0.9, 1.4, RED, WHITE, 12);
        const q = quadScreen(cam, [-1.6, 2.38, wz + 0.71], [1.6, 2.38, wz + 0.71], [1.6, 1.52, wz + 0.71], [-1.6, 1.52, wz + 0.71]);
        if (q) faces.push({ out: q, depth: (q[0].d + q[2].d) / 2 - 0.05, color: "#fff", paint: paintWordmark() });
        break;
      }
      case "van":
        van(cam, x, wz, o.len, false);
        break;
      case "oncoming":
        van(cam, x, wz, o.len, true);
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
      const ordered = side > 0 ? [q[1], q[0], q[3], q[2]] : q;
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

  /** Ten Dallas buildings, each with a silhouette you can name. */
  function landmark(cam, index, side, z) {
    const x = side * 34;
    const s = side;
    switch (index) {
      case 0:
        box(cam, x, 16, z, 3.6, 32, 3.6, [200, 196, 196]);
        prism(cam, x, 21.5, z, 4.6, 3.4, 8, [210, 206, 204]);
        prism(cam, x, 8.5, z, 5.6, 3.4, 8, [210, 206, 204]);
        ball(cam, x, 36, z, 4.4, [246, 236, 214], true);
        break;
      case 1:
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
        box(cam, 0, 8.15, z - 3, 64, 0.2, 3.1, RED);
        for (const px of [-11, 11]) box(cam, px, 3.6, z - 3, 1.4, 7.2, 1.4, [176, 176, 182]);
        break;
      case 2:
        box(cam, x, 24, z, 9, 48, 9, DARK_GLASS);
        for (let i = -1; i <= 1; i += 1) {
          line(cam, [x + i * 4.5, 0, z - 4.5], [x + i * 4.5, 48, z - 4.5], GREEN, 2);
          line(cam, [x - 4.5, 0, z + i * 4.5], [x - 4.5, 48, z + i * 4.5], GREEN, 2);
        }
        break;
      case 3:
        box(cam, x, 18, z, 10, 36, 10, [70, 100, 140]);
        for (let i = 1; i <= 7; i += 1) box(cam, x, i * 4.6, z, 10.2, 0.28, 10.2, [230, 236, 244]);
        break;
      case 4:
        box(cam, x, 9, z, 18, 18, 14, [88, 92, 110]);
        for (let i = 1; i <= 4; i += 1) box(cam, x - s * 9.1, 3.2 * i, z, 0.4, 0.5, 13, [RED, GOLD, GREEN, [90, 160, 240]][i % 4]);
        break;
      case 5:
        face(cam, [[x - 7, 0, z - 6], [x + 7, 0, z - 6], [x + 5, 11, z - 6], [x - 5, 11, z - 6]], [190, 44, 58], 0.95);
        face(cam, [[x - 7, 0, z + 6], [x + 7, 0, z + 6], [x + 5, 11, z + 6], [x - 5, 11, z + 6]], [190, 44, 58], 0.8);
        break;
      case 6:
        box(cam, x, 5, z, 16, 10, 12, STONE);
        box(cam, x + s * 3, 14, z, 5, 16, 5, STONE);
        break;
      case 7:
        for (let i = 0; i < 6; i += 1) {
          const tz = z - 10 + i * 5;
          prism(cam, x - s * 2, 0, tz, 0.4, 2.2, 5, [110, 84, 60]);
          ball(cam, x - s * 2, 3.6, tz, 2.1, [84, 150, 86]);
        }
        break;
      case 8:
        box(cam, x, 8, z, 14, 16, 12, [80, 92, 116]);
        ball(cam, x, 23, z, 4.2, [250, 250, 246], true);
        ball(cam, x, 23, z, 1.5, [60, 110, 170]);
        break;
      default:
        box(cam, x, 14, z, 12, 28, 12, [110, 104, 128]);
        box(cam, x, 29, z, 11, 8, 11, [96, 90, 116]);
        box(cam, x, 34.6, z, 5.6, 1, 5.6, [80, 76, 100]);
        break;
    }
  }

  /* ── frame ─────────────────────────────────────────────────────────── */

  function paintWorld(cam, run) {
    const g = ctx.createLinearGradient(0, 0, 0, H * 0.7);
    g.addColorStop(0, rgb(SKY.top));
    g.addColorStop(0.55, rgb(SKY.mid));
    g.addColorStop(0.9, rgb(SKY.low));
    g.addColorStop(1, rgb(SKY.haze));
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, W, H);

    const horizon = proj(cam, 0, 0, -250);
    const hy = horizon ? horizon.y : H * 0.4;

    // clouds, drifting slowly
    ctx.fillStyle = "rgba(255,255,255,0.75)";
    for (let i = 0; i < 5; i += 1) {
      const px = ((i * 211 - run.distance * 0.04) % (W + 300) + W + 300) % (W + 300) - 150;
      const py = hy * (0.18 + (i % 3) * 0.14);
      ctx.beginPath();
      ctx.ellipse(px, py, 60 + (i % 2) * 30, 16, 0, 0, Math.PI * 2);
      ctx.ellipse(px + 40, py - 10, 40, 18, 0, 0, Math.PI * 2);
      ctx.fill();
    }

    // far skyline, parallaxed
    const par = (run.distance * 0.06 + run.x * 6) % 420;
    ctx.fillStyle = "rgb(150,168,196)";
    ctx.beginPath();
    for (let i = 0; i < 46; i += 1) {
      const bw = 26 + ((i * 37) % 42);
      const bh = 22 + ((i * 61) % 96);
      ctx.rect(-60 + i * 34 - par * 0.5, hy - bh, bw, bh);
    }
    ctx.rect(W * 0.3 - par * 0.12, hy - 132, 8, 132);
    ctx.fill();
    ctx.beginPath();
    ctx.arc(W * 0.3 - par * 0.12 + 4, hy - 138, 16, 0, Math.PI * 2);
    ctx.fill();

    // ground + sidewalks
    ctx.fillStyle = rgb([196, 186, 176]);
    ctx.fillRect(0, hy, W, H - hy);
    const strip = [];
    for (let i = 14; i >= -200; i -= 4) {
      const p = proj(cam, -SIDEWALK_EDGE, 0.19, i);
      if (p) strip.push(p);
    }
    if (strip.length > 2) {
      ctx.beginPath();
      ctx.moveTo(strip[0].x, strip[0].y);
      for (const p of strip) ctx.lineTo(p.x, p.y);
      for (let i = strip.length - 1; i >= 0; i -= 1) ctx.lineTo(cx * 2 - strip[i].x, strip[i].y);
      ctx.closePath();
      ctx.fillStyle = rgb([214, 208, 200]);
      ctx.fill();
    }
    const roadPts = (halfW) => {
      const pts = [];
      for (let i = 14; i >= -200; i -= 6) {
        const p = proj(cam, halfW, 0.01, i);
        if (p) pts.push(p);
      }
      return pts;
    };
    const left = roadPts(-ROAD_HALF);
    const right = roadPts(ROAD_HALF);
    if (left.length > 2 && right.length > 2) {
      ctx.beginPath();
      ctx.moveTo(left[0].x, left[0].y);
      for (const p of left) ctx.lineTo(p.x, p.y);
      for (let i = right.length - 1; i >= 0; i -= 1) ctx.lineTo(right[i].x, right[i].y);
      ctx.closePath();
      const roadGrad = ctx.createLinearGradient(0, hy, 0, H);
      roadGrad.addColorStop(0, rgb([120, 120, 126]));
      roadGrad.addColorStop(1, rgb([82, 84, 92]));
      ctx.fillStyle = roadGrad;
      ctx.fill();
    }

    // lane dashes
    const off = -(run.distance % 12);
    ctx.fillStyle = "rgba(255,255,255,0.85)";
    for (const lane of [-LANE_DIV, LANE_DIV]) {
      for (let i = -2; i > -160; i -= 12) {
        const z = i + off;
        const a = proj(cam, lane - 0.09, 0.02, z);
        const b = proj(cam, lane + 0.09, 0.02, z);
        const c = proj(cam, lane + 0.09, 0.02, z - 5);
        const d = proj(cam, lane - 0.09, 0.02, z - 5);
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
    // red curbs, the brand along the road edge
    for (const sx of [-ROAD_HALF, ROAD_HALF]) {
      const cOff = -(run.distance % 4);
      for (let i = -2; i > -120; i -= 4) {
        const z = i + cOff;
        quad(cam, [sx, 0, z], [sx, 0, z - 2], [sx, 0.2, z - 2], [sx, 0.2, z], RED, 1);
        quad(cam, [sx, 0, z - 2], [sx, 0, z - 4], [sx, 0.2, z - 4], [sx, 0.2, z - 2], WHITE, 1);
      }
    }
  }

  function draw(run, opts) {
    const t0 = performance.now();
    faces = [];
    lines = [];
    balls = [];
    decals = [];
    badges = [];
    const now = (opts && opts.now) || t0;
    const camY = CAM.h + run.y * 0.62 - (run.slide > 0 ? 0.7 : 0);
    const cam = makeCamera(run.x * 0.34, camY, CAM.back, run.x * 0.14, CAM.lookY + run.y * 0.72, -CAM.lookAhead);

    paintWorld(cam, run);

    for (const o of run.objects) {
      const rel = o.z - run.distance;
      if (rel < -30 || rel > 220) continue;
      const wz = -rel;
      if (o.t === 2) {
        if (o.k === "store") storefront(cam, Math.floor(o.z) % 2 === 0 ? 1 : -1, wz);
        else landmark(cam, o.n, o.l === 0 ? -1 : 1, wz);
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
    streetFurniture(cam, run);
    chaser(cam, run);
    runner(cam, run, (opts && opts.outfit) || "classic", now);

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

    // speed streaks grow with pace: the "fast" feeling
    const k = Math.max(0, (run.speed - 24) / 22);
    if (k > 0 || run.jet > 0) {
      ctx.strokeStyle = `rgba(255, 255, 255, ${0.12 + 0.3 * Math.min(1, k + (run.jet > 0 ? 0.5 : 0))})`;
      ctx.lineWidth = 2;
      for (let i = 0; i < 14; i += 1) {
        const a = (i / 14) * Math.PI * 2 + (run.tick % 7) * 0.13;
        const r0 = Math.min(W, H) * (0.42 + ((i * 37 + run.tick * 3) % 20) / 100);
        const x0 = cx + Math.cos(a) * r0;
        const y0 = cy * 0.8 + Math.sin(a) * r0 * 0.7;
        ctx.beginPath();
        ctx.moveTo(x0, y0);
        ctx.lineTo(x0 + Math.cos(a) * 40 * (0.5 + k), y0 + Math.sin(a) * 28 * (0.5 + k));
        ctx.stroke();
      }
    }

    return { polys, ms: performance.now() - t0 };
  }

  resize();
  return { resize, draw };
}

/**
 * Dallas Dash — the renderer.
 *
 * A small software 3D renderer on Canvas 2D: real 3D geometry, flat-shaded,
 * painter-sorted. No WebGL, no libraries, no downloaded assets — every rock,
 * burger and skyscraper below is code. That is what keeps the build free to
 * test and identical on any device.
 *
 * Porting note (Expo / React Native): the maths (makeCamera, project) is pure
 * and the draw calls go through `ctx`, so a port swaps this file's output
 * target for Skia/Expo-GL and keeps the geometry builders.
 */

const ROAD_HALF = 4.6;
const SIDEWALK_EDGE = 8.2;
const LANE_DIV = 1.15;

const SKY = { top: [24, 11, 38], mid: [96, 40, 84], low: [214, 108, 74], haze: [72, 40, 76] };
const INK = [27, 14, 40];
const BRAND = [14, 124, 134];
const BRAND_DEEP = [10, 92, 100];
const CREAM = [255, 244, 230];
const WHITE = [252, 250, 246];
const BUN = [228, 172, 104];
const PATTY = [104, 62, 36];
const LETTUCE = [122, 196, 112];
const GOLD = [255, 196, 92];
const GLASS = [70, 92, 128];
const DARK_GLASS = [46, 34, 62];
const STONE = [176, 108, 92];
const GREEN = [126, 224, 138];
const LIGHT = { x: -0.42, y: 0.82, z: -0.38 };

/**
 * Camera framing, in one place so it can be tuned and reasoned about.
 * cy is the screen row the view axis lands on (0.55 keeps the runner in the
 * lower third with the horizon around 44% of the height); focal is a multiple
 * of the SHORTER canvas dimension — on a narrow phone that is what keeps the
 * outer lanes inside the frame while props stay readable.
 */
export const CAM = {
  h: 3.0, // camera height above the road
  back: 6.6, // camera distance behind the runner
  lookY: 0.7, // height of the aim point
  lookAhead: 8.5, // aim point distance ahead of the runner
  cy: 0.58, // pushes the runner into the lower third of the frame
  focal: 1.42,
};

function rgb(c, mul) {
  const f = mul === undefined ? 1 : mul;
  return `rgb(${Math.min(255, Math.round(c[0] * f))},${Math.min(255, Math.round(c[1] * f))},${Math.min(255, Math.round(c[2] * f))})`;
}

function fogMix(c, depth) {
  // Stronger depth cueing: distant props dissolve into the dusk haze instead of
  // stacking up as a hard row of clutter on the horizon.
  const t = Math.max(0, Math.min(0.86, (depth - 40) / 190));
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
  let polys = 0;

  function resize() {
    const dpr = Math.min(1.5, window.devicePixelRatio || 1);
    W = Math.max(320, Math.floor(canvas.clientWidth || window.innerWidth));
    H = Math.max(320, Math.floor(canvas.clientHeight || window.innerHeight));
    canvas.width = Math.floor(W * dpr);
    canvas.height = Math.floor(H * dpr);
    ctx.setTransform(dpr, 0, 0, dpr, 0, 0);
    focal = Math.min(W, H) * CAM.focal;
    cx = W / 2;
    cy = H * CAM.cy;
  }

  function proj(cam, x, y, z) {
    const dx = x - cam.px;
    const dy = y - cam.py;
    const dz = z - cam.pz;
    const depth = dx * cam.fx + dy * cam.fy + dz * cam.fz;
    if (depth <= 0.12) return null;
    const sx = cx + (focal * (dx * cam.rx + dy * cam.ry + dz * cam.rz)) / depth;
    const sy = cy - (focal * (dx * cam.ux + dy * cam.uy + dz * cam.uz)) / depth;
    return { x: sx, y: sy, d: depth };
  }

  /** Queue a polygon (world points, any length ≥ 3). */
  function face(cam, pts, color, tone) {
    const out = [];
    let depth = 0;
    for (let i = 0; i < pts.length; i += 1) {
      const p = proj(cam, pts[i][0], pts[i][1], pts[i][2]);
      if (!p) return;
      out.push(p);
      depth += p.d;
    }
    depth /= pts.length;
    if (depth > 260) return;
    if (out.length) {
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
    }
    faces.push({ out, depth, color: rgb(fogMix(color, depth), tone === undefined ? 1 : tone) });
  }

  function quad(cam, a, b, c, d, color, tone) {
    face(cam, [a, b, c, d], color, tone);
  }

  function line(cam, a, b, color, width) {
    const p = proj(cam, a[0], a[1], a[2]);
    const q = proj(cam, b[0], b[1], b[2]);
    if (!p || !q) return;
    lines.push({ p, q, color: rgb(color), width: width || 1.5 });
  }

  /**
   * An axis-aligned box, emitting only the three faces the camera can see, with
   * a per-axis tone so the flat shading reads as solid volume.
   */
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
    // Exactly the three faces that point at the camera — a box to the camera's
    // left shows its +x face, one in front shows its +z face, and so on.
    if (cxx > cam.px) quad(cam, c(0), c(2), c(6), c(4), color, 0.72);
    else quad(cam, c(1), c(3), c(7), c(5), color, 0.72);
    if (cyy > cam.py) quad(cam, c(0), c(4), c(5), c(1), color, 0.6);
    else quad(cam, c(2), c(6), c(7), c(3), color, 1.06);
    if (czz > cam.pz) quad(cam, c(0), c(1), c(3), c(2), color, 0.92);
    else quad(cam, c(4), c(5), c(7), c(6), color, 0.92);
  }

  /** A prism: `sides` around the vertical axis — cups, buckets, towers. */
  function prism(cam, cxx, cyy, czz, r, h, sides, color, bandColor, bandAt) {
    const top = [];
    const bot = [];
    for (let i = 0; i < sides; i += 1) {
      const a = (i / sides) * Math.PI * 2;
      const x = cxx + Math.sin(a) * r;
      const z = czz + Math.cos(a) * r;
      top.push([x, cyy + h, z]);
      bot.push([x, cyy, z]);
    }
    for (let i = 0; i < sides; i += 1) {
      const j = (i + 1) % sides;
      face(cam, [bot[i], bot[j], top[j], top[i]], color, 0.72 + 0.26 * Math.abs(Math.sin((i / sides) * Math.PI * 2)));
      if (bandColor && bandAt !== undefined) {
        const y0 = cyy + h * (bandAt - 0.12);
        const y1 = cyy + h * (bandAt + 0.12);
        face(
          cam,
          [
            [bot[i][0], y0, bot[i][2]],
            [bot[j][0], y0, bot[j][2]],
            [top[j][0], y1, top[j][2]],
            [top[i][0], y1, top[i][2]],
          ],
          bandColor,
          0.95,
        );
      }
    }
    face(cam, top, color, 1.1);
  }

  /** A billboard circle — spheres read fine as discs at this scale. */
  function ball(cam, x, y, z, r, color, lattice) {
    const p = proj(cam, x, y, z);
    if (!p) return;
    const radius = (focal * r) / p.d;
    if (radius < 0.6 || radius > H) return;
    balls.push({ p, radius, color: rgb(fogMix(color, p.d)), lattice });
  }

  /** Flat-shaded 2D shapes and text fitted onto a projected quad. */
  function onQuad(q, paint) {
    if (q.length !== 4) return;
    const [p0, p1, p2, p3] = q;
    ctx.save();
    ctx.beginPath();
    ctx.moveTo(p0.x, p0.y);
    ctx.lineTo(p1.x, p1.y);
    ctx.lineTo(p2.x, p2.y);
    ctx.lineTo(p3.x, p3.y);
    ctx.closePath();
    ctx.clip();
    ctx.transform(
      (p1.x - p0.x) / 100,
      (p1.y - p0.y) / 100,
      (p3.x - p0.x) / 100,
      (p3.y - p0.y) / 100,
      p0.x,
      p0.y,
    );
    paint();
    ctx.restore();
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

  let lines = [];
  let balls = [];
  let decals = [];

  /* ── roadside furniture ─────────────────────────────────────────────── */

  /**
   * Lamps, bins, benches, hydrants and parked cars, laid out from the distance
   * already travelled so they scroll with the world. Pure decoration: it is not
   * in the simulation, cannot be hit, and never touches the points ledger.
   */
  function streetFurniture(cam, run) {
    const base = Math.floor(run.distance / 26);
    for (let i = 0; i < 9; i += 1) {
      const gz = (base + i) * 26 - run.distance;
      if (gz < -4) continue;
      const wz = -gz;
      const side = (base + i) % 2 === 0 ? -1 : 1;
      const px = side * 6.6;
      prism(cam, px, 0, wz, 0.16, 4.6, 5, [92, 86, 104]);
      prism(cam, px, 4.6, wz, 0.36, 0.32, 6, [255, 214, 150]);
      line(cam, [px, 4.4, wz], [px - side * 1.2, 4.4, wz], [120, 112, 128], 2);
      const q = (base + i) % 3;
      if (q === 0) prism(cam, px - side * 1.7, 0, wz, 0.32, 0.64, 6, [70, 92, 84]);
      else if (q === 1) box(cam, px - side * 1.7, 0.36, wz, 1.3, 0.2, 0.5, [140, 106, 74]);
      else prism(cam, px - side * 1.7, 0, wz, 0.14, 0.7, 5, [176, 62, 62]);
    }
    const carBase = Math.floor((run.distance + 13) / 104);
    for (let i = 0; i < 3; i += 1) {
      const gz = (carBase + i) * 104 - run.distance;
      if (gz < -6 || gz > 210) continue;
      const side = (carBase + i) % 2 === 0 ? -1 : 1;
      box(cam, side * 7.5, 0.42, -gz, 1.5, 0.6, 3.0, [70, 78, 104]);
      box(cam, side * 7.5, 0.88, -gz + 0.1, 1.35, 0.45, 1.5, [96, 106, 134]);
    }
  }

  /* ── the runner ────────────────────────────────────────────────────── */

  function runner(cam, run, outfit) {
    const x = run.x;
    const y = run.y;
    const sliding = run.slide > 0;
    const skin = [232, 184, 148];
    const kit =
      outfit === "gold"
        ? GOLD
        : outfit === "apron"
          ? [246, 236, 224]
          : outfit === "bucket"
            ? CREAM
            : outfit === "tie"
              ? [58, 58, 78]
              : [70, 118, 190];
    const kitDeep =
      outfit === "gold" ? [206, 146, 56] : outfit === "apron" ? [222, 210, 196] : [44, 82, 140];
    const squash = sliding ? 0.5 : 1;
    const stride = sliding ? 0 : Math.sin(run.tick * 0.38);
    const bob = sliding ? 0 : Math.abs(Math.cos(run.tick * 0.38)) * 0.07;
    const lean = sliding ? 0 : 0.06;
    const legZ = stride * 0.44;

    // legs and shoes: the swing is the whole "running" read from behind
    box(cam, x - 0.17, y + 0.3 * squash, legZ + lean, 0.22, 0.6 * squash, 0.24, [52, 56, 74]);
    box(cam, x + 0.17, y + 0.3 * squash, -legZ + lean, 0.22, 0.6 * squash, 0.24, [52, 56, 74]);
    box(cam, x - 0.17, y + 0.07 * squash, legZ + lean - 0.07, 0.25, 0.15, 0.36, CREAM);
    box(cam, x + 0.17, y + 0.07 * squash, -legZ + lean - 0.07, 0.25, 0.15, 0.36, CREAM);

    // torso, plus the courier bag — it sits on the runner's back, which is the
    // side the camera sees, so it must be at a LARGER z than the torso
    box(cam, x, y + 0.98 * squash + bob, lean, 0.7, 0.76 * squash, 0.44, kit);
    box(cam, x, y + 1.16 * squash + bob, lean + 0.28, 0.52, 0.5 * squash, 0.22, BRAND_DEEP);
    box(cam, x, y + 1.36 * squash + bob, lean + 0.28, 0.14, 0.1, 0.24, CREAM);

    // arms, swinging opposite the legs
    box(cam, x - 0.46, y + 1.0 * squash + bob, -legZ * 0.9 + lean, 0.18, 0.64 * squash, 0.2, kit);
    box(cam, x + 0.46, y + 1.0 * squash + bob, legZ * 0.9 + lean, 0.18, 0.64 * squash, 0.2, kit);
    box(cam, x - 0.46, y + 0.74 * squash + bob, -legZ * 0.9 + lean, 0.2, 0.16, 0.22, skin);
    box(cam, x + 0.46, y + 0.74 * squash + bob, legZ * 0.9 + lean, 0.2, 0.16, 0.22, skin);

    // head, with the cap brim pointing away from the camera
    const headY = y + 1.62 * squash + bob;
    box(cam, x, headY, lean, 0.46, 0.44, 0.44, skin);
    box(cam, x - 0.24, headY, lean + 0.06, 0.07, 0.2, 0.1, skin);
    box(cam, x + 0.24, headY, lean + 0.06, 0.07, 0.2, 0.1, skin);
    if (outfit === "bucket") {
      prism(cam, x, headY + 0.2, lean, 0.36, 0.4, 8, BRAND, CREAM, 0.5);
    } else {
      box(cam, x, headY + 0.3, lean, 0.5, 0.18, 0.5, outfit === "gold" ? GOLD : kitDeep);
      box(cam, x, headY + 0.39, lean, 0.26, 0.1, 0.26, outfit === "gold" ? GOLD : kitDeep);
      box(cam, x, headY + 0.28, lean - 0.33, 0.46, 0.07, 0.18, kitDeep);
    }

    // the free-reward tie, worn as a bright stripe down the back
    if (outfit === "tie" || outfit === "gold") {
      box(cam, x, y + 1.06 * squash + bob, lean + 0.34, 0.14, 0.5, 0.06, GOLD);
    }

    // the warm pool of street light the runner is lit by, then the contact shadow
    decals.push({ x, z: 0.2, r: 0.95, color: [255, 176, 110], alpha: 0.16 });
    decals.push({
      x,
      z: 0,
      r: 0.46 - Math.min(0.16, y * 0.08),
      color: [16, 8, 24],
      alpha: 0.34,
    });
  }

  /* ── items ─────────────────────────────────────────────────────────── */

  function item(cam, kind, x, y, z, spin) {
    switch (kind) {
      case "cup":
        prism(cam, x, y - 0.3, z, 0.26, 0.62, 8, BRAND, CREAM, 0.72);
        prism(cam, x, y + 0.32, z, 0.28, 0.06, 8, CREAM);
        line(cam, [x, y + 0.38, z], [x + 0.07, y + 0.62, z], CREAM, 2);
        break;
      case "popcorn":
        box(cam, x, y, z, 0.5, 0.42, 0.34, BRAND);
        box(cam, x, y + 0.25, z, 0.44, 0.1, 0.28, GOLD);
        break;
      case "fries":
        box(cam, x, y - 0.14, z, 0.44, 0.56, 0.3, BRAND);
        box(cam, x, y + 0.3, z, 0.37, 0.26, 0.23, GOLD);
        box(cam, x - 0.1, y + 0.52, z, 0.09, 0.26, 0.09, [246, 214, 140]);
        box(cam, x + 0.1, y + 0.52, z, 0.09, 0.26, 0.09, [246, 214, 140]);
        break;
      case "coleslaw":
        prism(cam, x, y - 0.18, z, 0.29, 0.36, 8, [238, 238, 232], GREEN, 0.9);
        break;
      case "wrap":
        box(cam, x, y, z, 0.34, 0.28, 0.66, [236, 216, 176], spin);
        box(cam, x, y, z, 0.36, 0.14, 0.42, LETTUCE, spin);
        break;
      case "burger":
        box(cam, x, y - 0.2, z, 0.68, 0.24, 0.68, BUN);
        box(cam, x, y, z, 0.72, 0.16, 0.72, PATTY);
        box(cam, x, y + 0.08, z, 0.62, 0.07, 0.62, LETTUCE);
        box(cam, x, y + 0.22, z, 0.68, 0.24, 0.68, BUN);
        break;
      case "bucket":
        prism(cam, x, y - 0.3, z, 0.4, 0.7, 8, BRAND, CREAM, 0.55);
        prism(cam, x, y + 0.42, z, 0.36, 0.07, 8, CREAM);
        break;
      case "bowl":
        prism(cam, x, y - 0.28, z, 0.38, 0.3, 8, [214, 178, 128], null);
        prism(cam, x, y, z, 0.34, 0.14, 8, [232, 208, 164], null);
        box(cam, x, y + 0.18, z, 0.42, 0.09, 0.42, GOLD);
        break;
      default:
        box(cam, x, y, z, 0.44, 0.44, 0.44, GOLD);
    }
  }

  /* ── obstacles ─────────────────────────────────────────────────────── */

  function obstacle(cam, kind, x, z) {
    switch (kind) {
      case "cone":
        face(
          cam,
          [
            [x - 0.42, 0, z - 0.42],
            [x + 0.42, 0, z - 0.42],
            [x, 1.1, z],
          ],
          [242, 106, 34],
          0.95,
        );
        face(
          cam,
          [
            [x - 0.42, 0, z + 0.42],
            [x + 0.42, 0, z + 0.42],
            [x, 1.1, z],
          ],
          [242, 106, 34],
          0.8,
        );
        face(cam, [[x - 0.42, 0, z - 0.42], [x - 0.42, 0, z + 0.42], [x, 1.1, z]], [242, 106, 34], 0.7);
        face(cam, [[x + 0.42, 0, z - 0.42], [x + 0.42, 0, z + 0.42], [x, 1.1, z]], [242, 106, 34], 0.7);
        prism(cam, x, 0.42, z, 0.36, 0.18, 6, WHITE);
        break;
      case "barrier":
        box(cam, x, 0.42, z, 1.5, 0.7, 0.36, WHITE);
        box(cam, x, 0.42, z - 0.02, 1.5, 0.2, 0.38, BRAND);
        break;
      case "crate":
        box(cam, x, 0.5, z, 1.0, 1.0, 0.9, [150, 104, 62]);
        box(cam, x, 1.0, z, 1.02, 0.1, 0.92, [120, 80, 46]);
        break;
      case "spill":
        prism(cam, x, 0.03, z, 0.8, 0.06, 10, [42, 32, 52]);
        prism(cam, x, 0.09, z, 0.4, 0.04, 8, [96, 120, 150]);
        break;
      case "bucketstack":
        prism(cam, x, 0.3, z, 0.5, 0.58, 8, BRAND, WHITE, 0.5);
        prism(cam, x, 0.9, z, 0.44, 0.5, 8, BRAND, WHITE, 0.5);
        break;
      case "awning":
        box(cam, x, 3.0, z, 9.6, 0.8, 1.6, BRAND);
        box(cam, x, 3.0, z - 0.86, 9.6, 0.5, 0.1, WHITE);
        box(cam, -4.9, 1.5, z, 0.22, 3.0, 0.22, BRAND_DEEP);
        box(cam, 4.9, 1.5, z, 0.22, 3.0, 0.22, BRAND_DEEP);
        break;
      case "sign": {
        box(cam, -3.6, 1.5, z, 0.2, 3.0, 0.2, BRAND_DEEP);
        box(cam, 3.6, 1.5, z, 0.2, 3.0, 0.2, BRAND_DEEP);
        box(cam, 0, 3.3, z, 9.4, 1.5, 0.3, WHITE);
        box(cam, 0, 3.3, z - 0.2, 9.4, 0.32, 0.08, BRAND);
        const q = quadScreen(
          cam,
          [-4.7, 4.05, z - 0.21],
          [4.7, 4.05, z - 0.21],
          [4.7, 2.55, z - 0.21],
          [-4.7, 2.55, z - 0.21],
        );
        if (q) {
          onQuad(q, () => {
            ctx.fillStyle = "#0e7c86";
            ctx.font = "800 18px ui-sans-serif, system-ui, sans-serif";
            ctx.textAlign = "center";
            ctx.textBaseline = "middle";
            ctx.fillText("BIG D FRIED", 50, 50);
          });
        }
        break;
      }
      case "car":
        box(cam, x, 0.5, z, 1.85, 0.7, 3.4, [190, 76, 76]);
        box(cam, x, 1.04, z + 0.12, 1.6, 0.56, 1.7, [110, 132, 168]);
        box(cam, x - 0.88, 0.24, z - 1.05, 0.26, 0.48, 0.48, [30, 24, 34]);
        box(cam, x + 0.88, 0.24, z - 1.05, 0.26, 0.48, 0.48, [30, 24, 34]);
        box(cam, x - 0.88, 0.24, z + 1.05, 0.26, 0.48, 0.48, [30, 24, 34]);
        box(cam, x + 0.88, 0.24, z + 1.05, 0.26, 0.48, 0.48, [30, 24, 34]);
        break;
      case "truck":
        box(cam, x, 1.2, z, 2.2, 2.3, 4.2, CREAM);
        box(cam, x, 1.75, z - 2.12, 2.2, 0.65, 0.1, BRAND);
        box(cam, x, 2.45, z - 0.7, 1.7, 0.55, 1.3, BRAND);
        prism(cam, x, 2.7, z - 0.7, 0.44, 0.52, 8, BRAND, CREAM, 0.5);
        box(cam, x - 1.05, 0.32, z - 1.4, 0.32, 0.64, 0.64, [30, 24, 34]);
        box(cam, x + 1.05, 0.32, z - 1.4, 0.32, 0.64, 0.64, [30, 24, 34]);
        box(cam, x - 1.05, 0.32, z + 1.4, 0.32, 0.64, 0.64, [30, 24, 34]);
        box(cam, x + 1.05, 0.32, z + 1.4, 0.32, 0.64, 0.64, [30, 24, 34]);
        break;
      default:
        box(cam, x, 0.5, z, 0.9, 1.0, 0.9, [150, 104, 62]);
    }
  }

  /* ── Big D Fried storefront + real Dallas landmarks on the roadside ──── */

  function storefront(cam, side, z) {
    const x = side * 13.5;
    box(cam, x, 2.6, z, 11, 5.2, 9, CREAM);
    box(cam, x, 5.1, z, 11.2, 0.5, 9.2, [206, 190, 176]);
    // brand awning band facing the road
    box(cam, x - side * 5.6, 3.5, z, 0.6, 1.5, 9.4, BRAND);
    box(cam, x - side * 5.9, 4.3, z, 0.4, 0.3, 9.6, WHITE);
    // glass front
    box(cam, x - side * 5.6, 1.6, z, 0.35, 2.4, 8.6, [92, 116, 148]);
    // the sign panel, lettering drawn in code
    const q = quadScreen(
      cam,
      [x - side * 5.95, 6.9, z - 4.2],
      [x - side * 5.95, 6.9, z + 4.2],
      [x - side * 5.95, 5.2, z + 4.2],
      [x - side * 5.95, 5.2, z - 4.2],
    );
    if (q) {
      const flip = side > 0;
      const [p0, p1, p2, p3] = q;
      const ordered = flip ? [p1, p0, p3, p2] : [p0, p1, p2, p3];
      onQuad(ordered, () => {
        ctx.fillStyle = "#fff6e9";
        ctx.fillRect(0, 0, 100, 100);
        ctx.fillStyle = "#0e7c86";
        ctx.font = "900 30px ui-sans-serif, system-ui, sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("BIG D", 50, 34);
        ctx.fillText("FRIED", 50, 68);
      });
    }
    // the roadside sign every chicken shop lives by
    prism(cam, x, 1.2, z + 6.4, 0.22, 6.4, 6, [120, 120, 126]);
    prism(cam, x, 7.9, z + 6.4, 1.1, 1.9, 10, BRAND, WHITE, 0.5);
    prism(cam, x, 9.8, z + 6.4, 0.6, 0.3, 10, WHITE);
    const bq = quadScreen(
      cam,
      [x - 1.05, 9.35, z + 6.4 - side * 1.05],
      [x + 1.05, 9.35, z + 6.4 + side * 1.05],
      [x + 1.05, 8.0, z + 6.4 + side * 1.05],
      [x - 1.05, 8.0, z + 6.4 - side * 1.05],
    );
    if (bq) {
      const ordered = side > 0 ? [bq[1], bq[0], bq[3], bq[2]] : bq;
      onQuad(ordered, () => {
        ctx.fillStyle = "#fff6e9";
        ctx.font = "900 40px ui-sans-serif, system-ui, sans-serif";
        ctx.textAlign = "center";
        ctx.textBaseline = "middle";
        ctx.fillText("BDF", 50, 52);
      });
    }
  }

  /** Ten Dallas buildings, each with a silhouette you can name. */
  function landmark(cam, index, side, z) {
    const x = side * 34;
    const s = side;
    switch (index) {
      case 0: // Reunion Tower — shaft + lattice ball
        box(cam, x, 16, z, 3.6, 32, 3.6, [198, 178, 166]);
        prism(cam, x, 21.5, z, 4.6, 3.4, 8, [206, 188, 176]);
        prism(cam, x, 8.5, z, 5.6, 3.4, 8, [206, 188, 176]);
        ball(cam, x, 36, z, 4.4, [240, 214, 176], true);
        line(cam, [x, 32, z], [x, 40, z], [255, 232, 200], 2);
        break;
      case 1: // Margaret Hunt Hill Bridge — the white arch over the road
        prism(cam, 0, 12, z, 1.2, 24, 6, WHITE);
        for (let i = 0; i <= 12; i += 1) {
          const t = i / 12;
          const ax = (t - 0.5) * 62;
          const ay = 24 + Math.cos((t - 0.5) * Math.PI) * 15;
          box(cam, ax, ay, z, 3.6, 1.6, 2.2, WHITE);
          if (i % 2 === 0) line(cam, [ax, ay - 0.8, z], [0, 34, z], [245, 240, 236], 1.4);
          if (i % 2 === 0) line(cam, [ax, ay - 0.8, z], [ax, 2, z], [245, 240, 236], 1.2);
        }
        box(cam, 0, 2.6, z - 3, 64, 5.2, 1.4, [188, 186, 190]);
        break;
      case 2: // Bank of America Plaza — the green-edged tower
        box(cam, x, 24, z, 9, 48, 9, DARK_GLASS);
        box(cam, x, 48.5, z, 9.4, 1, 9.4, [60, 48, 74]);
        for (let i = -1; i <= 1; i += 1) {
          line(cam, [x + i * 4.5, 0, z - 4.5], [x + i * 4.5, 48, z - 4.5], GREEN, 2);
          line(cam, [x - 4.5, 0, z + i * 4.5], [x - 4.5, 48, z + i * 4.5], GREEN, 2);
        }
        break;
      case 3: // Fountain Place — dark glass with white bands
        box(cam, x, 18, z, 10, 36, 10, [40, 52, 74]);
        for (let i = 1; i <= 7; i += 1) box(cam, x, i * 4.6, z, 10.2, 0.28, 10.2, [220, 226, 236]);
        break;
      case 4: // Omni Dallas — LED bands
        box(cam, x, 9, z, 18, 18, 14, [64, 58, 78]);
        for (let i = 1; i <= 4; i += 1) {
          box(cam, x - s * 9.1, 3.2 * i, z, 0.4, 0.5, 13, [BRAND, GOLD, GREEN, [90, 160, 240]][i % 4]);
        }
        break;
      case 5: // Winspear Opera House — red glass, slanted roof
        face(
          cam,
          [
            [x - 7, 0, z - 6],
            [x + 7, 0, z - 6],
            [x + 5, 11, z - 6],
            [x - 5, 11, z - 6],
          ],
          [178, 40, 54],
          0.95,
        );
        face(
          cam,
          [
            [x - 7, 0, z + 6],
            [x + 7, 0, z + 6],
            [x + 5, 11, z + 6],
            [x - 5, 11, z + 6],
          ],
          [178, 40, 54],
          0.8,
        );
        face(cam, [[x - 7, 0, z - 6], [x - 7, 0, z + 6], [x - 5, 11, z + 6], [x - 5, 11, z - 6]], [150, 32, 46], 0.72);
        face(cam, [[x + 7, 0, z - 6], [x + 7, 0, z + 6], [x + 5, 11, z + 6], [x + 5, 11, z - 6]], [196, 52, 66], 0.72);
        break;
      case 6: // Old Red Museum — red sandstone with a tower
        box(cam, x, 5, z, 16, 10, 12, STONE);
        box(cam, x, 10.6, z, 16.6, 1.2, 12.6, [148, 84, 70]);
        box(cam, x + s * 3, 14, z, 5, 16, 5, STONE);
        prism(cam, x + s * 3, 22, z, 0.4, 0.4, 4, [148, 84, 70]);
        for (let i = 0; i < 3; i += 1) {
          box(cam, x - s * 6.9, 3 + i * 3, z, 0.3, 1.4, 10, [128, 70, 58]);
        }
        break;
      case 7: // Klyde Warren Park — trees and lights
        for (let i = 0; i < 6; i += 1) {
          const tz = z - 10 + i * 5;
          prism(cam, x - s * 2, 0, tz, 0.4, 2.2, 5, [96, 74, 54]);
          ball(cam, x - s * 2, 3.6, tz, 2.1, [70, 132, 78]);
          ball(cam, x + s * 3, 1.9, tz + 2, 0.35, [255, 236, 190]);
          prism(cam, x + s * 3, 0, tz + 2, 0.12, 1.7, 4, [120, 120, 126]);
        }
        break;
      case 8: // AT&T Discovery District — the eyeball
        box(cam, x, 8, z, 14, 16, 12, [58, 66, 88]);
        prism(cam, x, 16, z, 0.4, 6, 6, [140, 140, 146]);
        ball(cam, x, 23, z, 4.2, [248, 246, 240], true);
        ball(cam, x, 23, z, 1.5, [40, 40, 52]);
        break;
      default: // Comerica Bank Tower — stepped, barrel-vaulted top
        box(cam, x, 14, z, 12, 28, 12, [86, 78, 104]);
        box(cam, x, 29, z, 11, 8, 11, [72, 66, 92]);
        box(cam, x, 33.4, z, 8.6, 1.4, 8.6, [62, 56, 80]);
        box(cam, x, 34.6, z, 5.6, 1, 5.6, [58, 52, 76]);
        break;
    }
  }

  /* ── frame ─────────────────────────────────────────────────────────── */

  function paint(cam, run) {
    // sky
    const g = ctx.createLinearGradient(0, 0, 0, H * 0.72);
    g.addColorStop(0, rgb(SKY.top));
    g.addColorStop(0.28, rgb([58, 24, 60]));
    g.addColorStop(0.52, rgb(SKY.mid));
    g.addColorStop(0.72, rgb([154, 72, 84]));
    g.addColorStop(0.88, rgb(SKY.low));
    g.addColorStop(1, rgb(SKY.haze));
    ctx.fillStyle = g;
    ctx.fillRect(0, 0, W, H);

    const horizon = proj(cam, 0, 0, -240);
    const hy = horizon ? horizon.y : H * 0.4;

    // sun glow
    const sun = ctx.createRadialGradient(W * 0.22, hy - 30, 4, W * 0.22, hy - 30, H * 0.5);
    sun.addColorStop(0, "rgba(255, 214, 150, 0.5)");
    sun.addColorStop(1, "rgba(255, 150, 90, 0)");
    ctx.fillStyle = sun;
    ctx.fillRect(0, 0, W, H);

    // far skyline silhouette, parallaxed against the run
    const par = (run.distance * 0.06 + run.x * 6) % 420;
    ctx.fillStyle = rgb(INK, 0.96);
    ctx.beginPath();
    ctx.moveTo(-60, hy);
    for (let i = 0; i < 46; i += 1) {
      const bw = 26 + ((i * 37) % 42);
      const bh = 22 + ((i * 61) % 96);
      const bx = -60 + i * 34 - par * 0.5;
      ctx.rect(bx, hy - bh, bw, bh);
    }
    ctx.rect(W * 0.3 - par * 0.12, hy - 132, 8, 132);
    ctx.fill();
    // Reunion Tower's ball, drawn as its own path so no stray line joins it.
    ctx.beginPath();
    ctx.arc(W * 0.3 - par * 0.12 + 4, hy - 138, 16, 0, Math.PI * 2);
    ctx.fill();
    ctx.beginPath();
    for (let i = 0; i <= 20; i += 1) {
      const t = i / 20;
      const ax = W * 0.68 + (t - 0.5) * 300 - par * 0.3;
      ctx.lineTo(ax, hy - 60 - Math.cos((t - 0.5) * Math.PI) * 74);
    }
    ctx.lineWidth = 4;
    ctx.strokeStyle = rgb(INK, 0.96);
    ctx.stroke();

    // ground + road surface
    ctx.fillStyle = rgb([48, 34, 56]);
    ctx.fillRect(0, hy, W, H - hy);
    const strip = [];
    for (let i = 14; i >= -190; i -= 4) {
      const p = proj(cam, -SIDEWALK_EDGE, 0.19, i);
      if (p) strip.push(p);
    }
    if (strip.length > 2) {
      ctx.beginPath();
      ctx.moveTo(strip[0].x, strip[0].y);
      for (const p of strip) ctx.lineTo(p.x, p.y);
      for (let i = strip.length - 1; i >= 0; i -= 1) ctx.lineTo(W - strip[i].x + cx * 2 - W, strip[i].y);
      ctx.closePath();
      ctx.fillStyle = rgb([104, 98, 116]);
      ctx.fill();
    }

    const roadPts = (halfW) => {
      const pts = [];
      for (let i = 14; i >= -190; i -= 6) {
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
      // Asphalt warms up toward the viewer: a flat dark band read as "empty"
      // foreground, a lit one reads as road.
      const roadGrad = ctx.createLinearGradient(0, hy, 0, H);
      roadGrad.addColorStop(0, rgb([58, 48, 66]));
      roadGrad.addColorStop(0.5, rgb([74, 60, 80]));
      roadGrad.addColorStop(1, rgb([112, 88, 100]));
      ctx.fillStyle = roadGrad;
      ctx.fill();
    }

    // lane dashes and crosswalks
    const off = -(run.distance % 16);
    for (const lane of [-LANE_DIV, LANE_DIV]) {
      // Start at z = -2: a dash right under the camera projects to a huge
      // vertical streak and reads as broken perspective.
      for (let i = -2; i > -150; i -= 16) {
        const z = i + off;
        const a = proj(cam, lane - 0.08, 0.02, z);
        const b = proj(cam, lane + 0.08, 0.02, z);
        const c = proj(cam, lane + 0.08, 0.02, z - 6);
        const d = proj(cam, lane - 0.08, 0.02, z - 6);
        if (!a || !b || !c || !d) continue;
        ctx.beginPath();
        ctx.moveTo(a.x, a.y);
        ctx.lineTo(b.x, b.y);
        ctx.lineTo(c.x, c.y);
        ctx.lineTo(d.x, d.y);
        ctx.closePath();
        ctx.fillStyle = rgb([226, 214, 200], 0.75);
        ctx.fill();
      }
    }
    const cwOff = -((run.distance + 40) % 130);
    for (let k = 0; k < 8; k += 1) {
      const z = -18 + cwOff + k * 0.9;
      const a = proj(cam, -ROAD_HALF + 0.3, 0.02, z);
      const b = proj(cam, ROAD_HALF - 0.3, 0.02, z);
      const c = proj(cam, ROAD_HALF - 0.3, 0.02, z - 0.5);
      const d = proj(cam, -ROAD_HALF + 0.3, 0.02, z - 0.5);
      if (!a || !b || !c || !d) continue;
      ctx.beginPath();
      ctx.moveTo(a.x, a.y);
      ctx.lineTo(b.x, b.y);
      ctx.lineTo(c.x, c.y);
      ctx.lineTo(d.x, d.y);
      ctx.closePath();
      ctx.fillStyle = rgb([236, 230, 218], 0.6);
      ctx.fill();
    }

    // lane arrows and asphalt patches — near-field texture, so the foreground
    // is not a dead grey band
    const paintMark = [232, 222, 206];
    const arrowBase = Math.floor((run.distance + 6) / 46);
    for (let i = 0; i < 3; i += 1) {
      const gz = (arrowBase + i) * 46 - run.distance;
      if (gz < 5 || gz > 130) continue;
      const az = -gz;
      const ax = [-2.3, 0, 2.3][(arrowBase + i) % 3];
      quad(cam, [ax - 0.2, 0.02, az], [ax + 0.2, 0.02, az], [ax + 0.2, 0.02, az - 1.7], [ax - 0.2, 0.02, az - 1.7], paintMark, 0.85);
      face(
        cam,
        [
          [ax - 0.5, 0.02, az - 1.7],
          [ax + 0.5, 0.02, az - 1.7],
          [ax, 0.02, az - 2.7],
        ],
        paintMark,
        0.85,
      );
    }
    const patchBase = Math.floor((run.distance + 30) / 71);
    for (let i = 0; i < 2; i += 1) {
      const gz = (patchBase + i) * 71 - run.distance;
      if (gz < 4 || gz > 150) continue;
      const pz = -gz;
      const lane = [-2.3, 0, 2.3][(patchBase + i) % 3];
      quad(
        cam,
        [lane - 0.7, 0.015, pz],
        [lane + 0.7, 0.015, pz],
        [lane + 0.7, 0.015, pz - 1.8],
        [lane - 0.7, 0.015, pz - 1.8],
        [64, 54, 72],
        1,
      );
    }
    // a real curb along both road edges: a lit vertical face, not a hairline
    for (const sx of [-ROAD_HALF, ROAD_HALF]) {
      quad(cam, [sx, 0, -2], [sx, 0, -150], [sx, 0.19, -150], [sx, 0.19, -2], [192, 180, 194], 1);
    }
  }

  function draw(run, opts) {
    const t0 = performance.now();
    faces = [];
    lines = [];
    balls = [];
    decals = [];
    const cam = makeCamera(
      run.x * 0.32,
      CAM.h - (run.slide > 0 ? 0.85 : 0) + Math.min(0.45, run.y * 0.28),
      CAM.back,
      run.x * 0.12,
      CAM.lookY - (run.slide > 0 ? 0.3 : 0),
      -CAM.lookAhead,
    );

    paint(cam, run);

    for (const o of run.objects) {
      // `rel > 0` means the object is still AHEAD of the runner, and the camera
      // looks down -z, so an object ahead sits at a NEGATIVE world z. Getting
      // that sign wrong put everything the player was running toward behind the
      // camera: an empty road ahead, with the props already passed drifting away
      // toward the horizon instead of rushing at the player.
      const rel = o.z - run.distance;
      if (rel < -8 || rel > 200) continue;
      const wz = -rel;
      if (o.t === 2) {
        // Scenery never touches the rules, so the side it sits on is the
        // renderer's call: storefronts alternate, landmarks use the seeded lane.
        if (o.k === "store") storefront(cam, Math.floor(o.z) % 2 === 0 ? 1 : -1, wz);
        else landmark(cam, o.n, o.l === 0 ? -1 : 1, wz);
        continue;
      }
      const x = [-2.3, 0, 2.3][o.l];
      if (o.t === 0) {
        const spin = Math.sin((run.tick + rel) * 0.02) * 0.4;
        item(cam, o.k, x, 1.15 + Math.sin(run.tick * 0.05 + o.z) * 0.12, wz, spin);
        // a warm ring under each pickup: the eye finds these before the shapes
        decals.push({ x, z: wz, r: 0.62, color: [255, 214, 140], alpha: 0.34, ring: true });
      } else {
        obstacle(cam, o.k, x, wz);
      }
    }
    streetFurniture(cam, run);

    runner(cam, run, (opts && opts.outfit) || "classic");

    faces.sort((a, b) => b.depth - a.depth);

    for (const d of decals) {
      const p = proj(cam, d.x, 0.015, d.z);
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
    for (const f of faces) {
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

    for (const l of lines) {
      ctx.beginPath();
      ctx.moveTo(l.p.x, l.p.y);
      ctx.lineTo(l.q.x, l.q.y);
      ctx.strokeStyle = l.color;
      ctx.lineWidth = l.width;
      ctx.stroke();
    }

    for (const b of balls) {
      ctx.beginPath();
      ctx.arc(b.p.x, b.p.y, b.radius, 0, Math.PI * 2);
      ctx.fillStyle = b.color;
      ctx.fill();
      if (b.lattice) {
        ctx.strokeStyle = "rgba(40, 24, 48, 0.5)";
        ctx.lineWidth = 1;
        for (let i = -2; i <= 2; i += 1) {
          ctx.beginPath();
          ctx.ellipse(b.p.x, b.p.y, Math.abs(b.radius * (i / 3)) + 0.5, b.radius, 0, 0, Math.PI * 2);
          ctx.stroke();
        }
      }
    }

    // speed streaks + hit flash: polish only, never rules
    if (run.speed > 22) {
      const k = (run.speed - 22) / 8;
      ctx.strokeStyle = `rgba(255, 240, 220, ${0.18 * k})`;
      ctx.lineWidth = 2;
      for (let i = 0; i < 8; i += 1) {
        const p = proj(cam, -8 + i * 2.4, 1 + ((i * 7) % 5) * 0.6, -30 - ((i * 23) % 60));
        if (!p) continue;
        ctx.beginPath();
        ctx.moveTo(p.x, p.y);
        ctx.lineTo(p.x + (p.x - cx) * 0.06, p.y + (p.y - cy) * 0.06);
        ctx.stroke();
      }
    }
    if (run.invuln > 18) {
      ctx.fillStyle = `rgba(228, 0, 43, ${(run.invuln - 18) / 30})`;
      ctx.fillRect(0, 0, W, H);
    }

    return {
      polys,
      ms: performance.now() - t0,
    };
  }

  resize();
  return { resize, draw };
}

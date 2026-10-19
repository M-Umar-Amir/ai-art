/**
 * Dallas Dash — the ten hero landmarks, modelled after the client's
 * illustrations (assets/landmarks/*): Reunion Tower, the Margaret Hunt Hill
 * Bridge, Bank of America Plaza, Fountain Place and the Omni in Dallas; the
 * CN Tower, Rogers Centre, City Hall, the ROM and Casa Loma in Toronto.
 *
 * Same toy palette as the art: cream stone, terracotta trim, dark glass with
 * cream mullions, teal accents. Each model is plain geometry; city.js
 * flattens it to a handful of draw calls (painted parts are vertex-coloured,
 * glass shares one textured material).
 *
 * Coordinates: the landmark stands at x = side * 22 (side -1 left, +1 right
 * of the road), facing the road (-side along x). Metres, y up.
 */

import * as THREE from "three";

export const LM = {
  cream: [239, 226, 198],
  cream2: [224, 205, 168],
  stone: [214, 196, 160],
  terra: [198, 112, 78],
  terraDark: [158, 80, 56],
  teal: [64, 140, 134],
  tealDark: [44, 104, 100],
  glass: [58, 70, 86],
  slate: [86, 92, 104],
  green: [108, 214, 150],
  white: [246, 244, 238],
  water: [88, 168, 176],
};

/* ── shared textures ───────────────────────────────────────────────── */

let tex = null;
function textures() {
  if (tex) return tex;
  const make = (paint, w = 256, h = 256) => {
    const c = document.createElement("canvas");
    c.width = w;
    c.height = h;
    paint(c.getContext("2d"), w, h);
    const t = new THREE.CanvasTexture(c);
    t.colorSpace = THREE.SRGBColorSpace;
    t.wrapS = t.wrapT = THREE.RepeatWrapping;
    t.anisotropy = 4;
    return t;
  };
  // curtain wall: dark blue-grey panes, cream mullions, a soft sky reflection
  const curtain = make((g, w, h) => {
    const grd = g.createLinearGradient(0, 0, w, h);
    grd.addColorStop(0, "#4b5a6e");
    grd.addColorStop(0.55, "#36424f");
    grd.addColorStop(1, "#2c3640");
    g.fillStyle = grd;
    g.fillRect(0, 0, w, h);
    g.fillStyle = "rgba(190,215,235,0.12)";
    g.beginPath();
    g.moveTo(0, h * 0.7);
    g.lineTo(w * 0.7, 0);
    g.lineTo(w, 0);
    g.lineTo(0, h);
    g.fill();
    g.fillStyle = "#d9cdb0";
    for (let i = 0; i < 4; i += 1) {
      g.fillRect(i * (w / 4), 0, 6, h);
      g.fillRect(0, i * (h / 4), w, 7);
    }
  });
  // banded hotel facade: cream spandrels between dark window strips
  const bands = make((g, w, h) => {
    g.fillStyle = "#2f3a46";
    g.fillRect(0, 0, w, h);
    g.fillStyle = "rgba(180,205,230,0.14)";
    g.fillRect(0, h * 0.2, w, h * 0.12);
    g.fillStyle = "#efe2c6";
    g.fillRect(0, h * 0.72, w, h * 0.28);
    g.fillStyle = "#c5b796";
    g.fillRect(0, h * 0.72, w, 4);
    g.fillStyle = "#26303a";
    for (let i = 1; i < 4; i += 1) g.fillRect(i * (w / 4), 0, 4, h * 0.72);
  });
  // stone wall with arched windows (castle, museum)
  const arches = make((g, w, h) => {
    g.fillStyle = "#efe2c6";
    g.fillRect(0, 0, w, h);
    g.fillStyle = "#e2d2b0";
    for (let y = 0; y < h; y += 16) g.fillRect(0, y, w, 2);
    g.fillStyle = "#c6704e";
    g.fillRect(w * 0.3, h * 0.22, w * 0.4, h * 0.62);
    g.beginPath();
    g.arc(w / 2, h * 0.22, w * 0.2, Math.PI, 0);
    g.fill();
    g.fillStyle = "#34404c";
    g.fillRect(w * 0.36, h * 0.26, w * 0.28, h * 0.54);
    g.beginPath();
    g.arc(w / 2, h * 0.26, w * 0.14, Math.PI, 0);
    g.fill();
    g.fillStyle = "#d9cdb0";
    g.fillRect(w * 0.49, h * 0.18, w * 0.02, h * 0.62);
  });
  const glassMat = (map) => new THREE.MeshStandardMaterial({ map, roughness: 0.28, metalness: 0.35 });
  const curtainBoth = glassMat(curtain);
  curtainBoth.side = THREE.DoubleSide;
  tex = { curtain: glassMat(curtain), curtainBoth, bands: glassMat(bands), arches: new THREE.MeshStandardMaterial({ map: arches, roughness: 0.8 }) };
  return tex;
}

/** Scale a BoxGeometry's UVs so its texture tiles every `tile` metres. */
function tileBox(geo, w, h, d, tile, tileY = tile) {
  const uv = geo.attributes.uv;
  // BoxGeometry faces: +x, -x, +y, -y, +z, -z — four vertices each
  const size = [[d, h], [d, h], [w, d], [w, d], [w, h], [w, h]];
  for (let f = 0; f < 6; f += 1) {
    for (let k = 0; k < 4; k += 1) {
      const i = f * 4 + k;
      uv.setXY(i, uv.getX(i) * (size[f][0] / tile), uv.getY(i) * (size[f][1] / tileY));
    }
  }
  return geo;
}

/** A prism from a footprint polygon (x, z pairs), with per-corner heights. */
function prismFrom(pts, y0, heights, tile) {
  const pos = [];
  const uv = [];
  const n = pts.length;
  let u = 0;
  for (let i = 0; i < n; i += 1) {
    const [ax, az] = pts[i];
    const [bx, bz] = pts[(i + 1) % n];
    const ha = heights[i];
    const hb = heights[(i + 1) % n];
    const len = Math.hypot(bx - ax, bz - az);
    const quad = [[ax, y0, az, u, 0], [bx, y0, bz, u + len, 0], [bx, y0 + hb, bz, u + len, hb], [ax, y0 + ha, az, u, ha]];
    for (const k of [0, 1, 2, 0, 2, 3]) {
      pos.push(quad[k][0], quad[k][1], quad[k][2]);
      uv.push(quad[k][3] / tile, quad[k][4] / tile);
    }
    u += len;
  }
  const g = new THREE.BufferGeometry();
  g.setAttribute("position", new THREE.Float32BufferAttribute(pos, 3));
  g.setAttribute("uv", new THREE.Float32BufferAttribute(uv, 2));
  g.computeVertexNormals();
  return g;
}

/* ── the builders ──────────────────────────────────────────────────── */

/**
 * Build `model` into `put(geometry, color, glassy)` / `putMat(geometry, material)`.
 * Returns false when the model isn't one of the ten (city.js falls back).
 */
export function buildHero(model, side, put, putMat) {
  const mats = textures();
  const x = side * 22;
  const s = side;
  const V = (a, b, c) => new THREE.Vector3(a, b, c);
  const at = (geo, px, py, pz) => geo.translate(px, py, pz);
  const box = (px, py, pz, w, h, d, color, glassy) => put(at(new THREE.BoxGeometry(w, h, d), px, py, pz), color, glassy);
  const boxTex = (px, py, pz, w, h, d, mat, tile, tileY) => putMat(at(tileBox(new THREE.BoxGeometry(w, h, d), w, h, d, tile, tileY), px, py, pz), mat);
  const cyl = (px, py, pz, rb, rt, h, color, seg = 24, glassy) => put(at(new THREE.CylinderGeometry(rt, rb, h, seg).translate(0, h / 2, 0), px, py, pz), color, glassy);
  const ball = (px, py, pz, r, color, glassy) => put(at(new THREE.SphereGeometry(r, 24, 16), px, py, pz), color, glassy);
  const ring = (px, py, pz, r, tube, color, axis = "y") => {
    const g = new THREE.TorusGeometry(r, tube, 8, 40);
    if (axis === "y") g.rotateX(Math.PI / 2);
    if (axis === "x") g.rotateY(Math.PI / 2);
    put(at(g, px, py, pz), color);
  };
  const rod = (a, b, r, color) => {
    const len = a.distanceTo(b);
    const g = new THREE.CylinderGeometry(r, r, len, 6);
    g.translate(0, len / 2, 0);
    g.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(V(0, 1, 0), b.clone().sub(a).normalize()));
    put(at(g, a.x, a.y, a.z), color);
  };
  const steps = (px, pz, w, d, n, color) => {
    for (let i = 0; i < n; i += 1) box(px, 0.25 + i * 0.5, pz, w - i * 1.6, 0.5, d - i * 1.6, i % 2 ? LM.cream : color);
  };

  switch (model) {
    /* ── Dallas ── */
    case "reunion": {
      // terracotta drum base, a cream shaft, the lattice ball, a crown and mast
      cyl(x, 0, 0, 7.4, 7.2, 3.2, LM.terra, 40);
      cyl(x, 3.2, 0, 7.6, 7.6, 0.5, LM.cream, 40);
      box(x - s * 7.1, 1.6, 0, 1.2, 0.9, 6, LM.glass, true);
      cyl(x, 3.7, 0, 2.5, 2.1, 31, LM.cream, 24);
      cyl(x, 34.4, 0, 2.6, 4.4, 2.2, LM.cream, 24); // the flared collar under the ball
      const cy = 41.5;
      const R = 6.6;
      ball(x, cy, 0, R * 0.985, LM.glass, true);
      // geodesic lattice: the edges of a subdivided icosahedron
      const ico = new THREE.IcosahedronGeometry(R, 2);
      const edges = new THREE.EdgesGeometry(ico, 1);
      const ep = edges.attributes.position;
      for (let i = 0; i < ep.count; i += 2) {
        const a = V(ep.getX(i), ep.getY(i), ep.getZ(i));
        const b = V(ep.getX(i + 1), ep.getY(i + 1), ep.getZ(i + 1));
        rod(a.add(V(x, cy, 0)), b.add(V(x, cy, 0)), 0.13, LM.cream);
      }
      ring(x, cy - 0.6, 0, R * 1.0, 0.55, LM.cream);
      ring(x, cy - 2.6, 0, R * 0.92, 0.3, LM.terra);
      cyl(x, cy + R * 0.86, 0, 3.4, 2.6, 1.4, LM.cream, 24);
      cyl(x, cy + R * 0.86 + 1.4, 0, 1.6, 1.0, 1.2, LM.terra, 16);
      cyl(x, cy + R * 0.86 + 2.6, 0, 0.35, 0.12, 7, LM.white, 8);
      break;
    }
    case "archbridge": {
      // the deck crosses over the road; one white arch with fanned cables
      const y = 8;
      const L = 66;
      box(0, y, -3, L, 1.0, 7, LM.cream);
      box(0, y + 0.65, -3, L, 0.3, 7.2, LM.terra);
      for (const dz of [-6.4, 0.4]) box(0, y + 1.3, dz, L, 0.9, 0.3, LM.cream); // parapets
      box(0, y - 0.8, -3, L, 0.6, 5.6, LM.terraDark);
      for (const px of [-28, -12, 12, 28]) {
        box(px, (y - 1) / 2, -3, 2, y - 1, 4.4, LM.terra);
        box(px, 0.4, -3, 3, 0.8, 5.6, LM.terraDark);
      }
      // the arch: a box section swept along a parabola in the deck's centre plane
      const N = 26;
      const span = 54;
      const top = 30;
      const pts = [];
      for (let i = 0; i <= N; i += 1) {
        const t = i / N;
        const ax = (t - 0.5) * span;
        const ay = y + 1 + top * (1 - (2 * t - 1) ** 2);
        pts.push(V(ax, ay, -3));
      }
      for (let i = 0; i < N; i += 1) {
        const a = pts[i];
        const b = pts[i + 1];
        const len = a.distanceTo(b) + 0.4;
        const g = new THREE.BoxGeometry(len, 1.6, 1.3);
        g.rotateZ(Math.atan2(b.y - a.y, b.x - a.x));
        put(at(g, (a.x + b.x) / 2, (a.y + b.y) / 2, -3), LM.white);
      }
      // cables from the arch down to both deck edges
      for (let i = 2; i < N - 1; i += 1) {
        for (const dz of [-6.1, 0.1]) rod(pts[i].clone().add(V(0, -0.6, 0)), V(pts[i].x * 1.08, y + 1.4, dz), 0.07, LM.white);
      }
      break;
    }
    case "bofa": {
      // a cross-plan tower in dark glass, its edges traced in green light
      steps(x, 0, 20, 20, 2, LM.terra);
      box(x, 3, 0, 17, 4, 17, LM.cream);
      for (const dz of [-6, -2, 2, 6]) box(x - s * 8.6, 2.6, dz, 0.6, 3.2, 1.2, LM.terra);
      box(x - s * 8.4, 2.4, 0, 0.4, 2.8, 3.6, LM.glass, true);
      const H = 62;
      boxTex(x, 5 + H / 2, 0, 13, H, 7.5, mats.curtain, 3.2);
      boxTex(x, 5 + H / 2, 0, 7.5, H, 13, mats.curtain, 3.2);
      // stepped crown
      boxTex(x, 5 + H + 2, 0, 9, 4, 9, mats.curtain, 3.2);
      box(x, 5 + H + 4.4, 0, 6, 0.8, 6, LM.slate);
      // green edge lights on every outer corner of the cross, and around the top
      const corners = [];
      for (const [a, b] of [[6.5, 3.75], [3.75, 6.5]]) {
        for (const sx of [-1, 1]) for (const sz of [-1, 1]) corners.push([sx * a, sz * b]);
      }
      for (const [cx, cz] of corners) rod(V(x + cx, 5, cz), V(x + cx, 5 + H, cz), 0.16, LM.green);
      for (const [a, b, c, d] of [[6.5, 3.75, 6.5, -3.75], [-6.5, 3.75, -6.5, -3.75], [3.75, 6.5, -3.75, 6.5], [3.75, -6.5, -3.75, -6.5]]) rod(V(x + a, 5 + H, b), V(x + c, 5 + H, d), 0.16, LM.green);
      for (const sx of [-1, 1]) for (const sz of [-1, 1]) {
        rod(V(x + sx * 4.5, 5 + H, sz * 4.5), V(x + sx * 4.5, 5 + H + 4, sz * 4.5), 0.14, LM.green);
      }
      break;
    }
    case "fountain": {
      // a faceted prism whose roof is cut by two sloping planes
      box(x, 0.3, 0, 30, 0.6, 30, LM.cream);
      box(x, 0.65, 0, 26, 0.1, 26, LM.terra);
      steps(x, 0, 20, 20, 3, LM.terra);
      for (const sx of [-1, 1]) for (const sz of [-1, 1]) box(x + sx * 12.5, 0.9, sz * 12.5, 4, 0.6, 4, LM.teal);
      const w = 7.5;
      const pts = [[x - w, -w], [x + w, -w], [x + w, w], [x - w, w]];
      const hs = s > 0 ? [44, 58, 72, 52] : [58, 44, 52, 72];
      putMat(prismFrom(pts, 1.6, hs, 3), mats.curtain);
      // the roof as two facets meeting along a crease
      const tri = (a, b, c) => {
        const g = new THREE.BufferGeometry();
        g.setAttribute("position", new THREE.Float32BufferAttribute([...a, ...b, ...c], 3));
        g.setAttribute("uv", new THREE.Float32BufferAttribute([0, 0, 1, 0, 0, 1], 2));
        g.computeVertexNormals();
        return g;
      };
      const P = pts.map(([px, pz], i) => [px, 1.6 + hs[i], pz]);
      putMat(tri(P[0], P[2], P[1]), mats.curtain);
      putMat(tri(P[0], P[3], P[2]), mats.curtain);
      // cream mullion edges up the corners
      for (let i = 0; i < 4; i += 1) rod(V(P[i][0], 1.6, P[i][2]), V(P[i][0], P[i][1], P[i][2]), 0.18, LM.cream);
      rod(V(...P[0]), V(...P[2]), 0.18, LM.cream);
      for (let i = 0; i < 4; i += 1) rod(V(...P[i]), V(...P[(i + 1) % 4]), 0.18, LM.cream);
      break;
    }
    case "omni": {
      // a long slab with rounded ends, banded in cream and glass, on a terracotta podium
      box(x, 2.6, 0, 30, 5.2, 20, LM.terra);
      box(x, 5.4, 0, 30.6, 0.5, 20.6, LM.cream);
      box(x - s * 15.5, 2.2, 0, 1, 4.4, 12, LM.glass, true);
      box(x - s * 17, 4.6, 0, 4, 0.5, 9, LM.cream); // entrance canopy
      for (const dz of [-4, 4]) box(x - s * 18.6, 2.3, dz, 0.4, 4.6, 0.4, LM.cream);
      const shape = new THREE.Shape();
      const W = 13;
      const D = 6;
      const r = 3;
      shape.moveTo(-W + r, -D);
      shape.lineTo(W - r, -D);
      shape.quadraticCurveTo(W, -D, W, -D + r);
      shape.lineTo(W, D - r);
      shape.quadraticCurveTo(W, D, W - r, D);
      shape.lineTo(-W + r, D);
      shape.quadraticCurveTo(-W, D, -W, D - r);
      shape.lineTo(-W, -D + r);
      shape.quadraticCurveTo(-W, -D, -W + r, -D);
      const H = 30;
      const slab = new THREE.ExtrudeGeometry(shape, { depth: H, bevelEnabled: false, curveSegments: 6 });
      slab.rotateX(-Math.PI / 2);
      slab.rotateY(Math.PI / 2);
      // world-scale UVs on the walls: one band per 2.5 m storey
      const p = slab.attributes.position;
      const uv = slab.attributes.uv;
      for (let i = 0; i < p.count; i += 1) uv.setXY(i, (p.getX(i) + p.getZ(i)) / 4, p.getY(i) / 2.5);
      putMat(at(slab, x, 5.6, 0), mats.bands);
      const roof = new THREE.ShapeGeometry(shape);
      roof.rotateX(-Math.PI / 2);
      roof.rotateY(Math.PI / 2);
      put(at(roof, x, 5.6 + H + 0.02, 0), LM.slate);
      const lip = new THREE.ExtrudeGeometry(shape, { depth: 0.8, bevelEnabled: false, curveSegments: 6 });
      lip.rotateX(-Math.PI / 2);
      lip.rotateY(Math.PI / 2);
      lip.scale(1.02, 1, 1.03);
      put(at(lip, x, 5.6 + H, 0), LM.cream);
      break;
    }

    /* ── Toronto ── */
    case "cntower": {
      cyl(x, 0, 0, 7, 6.6, 1.6, LM.terra, 32);
      // three tapering legs around a hexagonal core
      for (let k = 0; k < 3; k += 1) {
        const a = (k / 3) * Math.PI * 2 + Math.PI / 6;
        const g = new THREE.CylinderGeometry(0.4, 2.2, 38, 4);
        g.translate(0, 19, 0);
        g.scale(1, 1, 0.55);
        g.rotateY(-a);
        put(at(g, x + Math.cos(a) * 1.8, 1.6, Math.sin(a) * 1.8), LM.cream);
      }
      cyl(x, 1.6, 0, 2.6, 1.3, 46, LM.cream, 6);
      for (let k = 0; k < 6; k += 1) {
        const a = (k / 6) * Math.PI * 2;
        rod(V(x + Math.cos(a) * 2.5, 1.6, Math.sin(a) * 2.5), V(x + Math.cos(a) * 1.25, 47.6, Math.sin(a) * 1.25), 0.12, LM.terra);
      }
      // the main pod: cream decks around a glass band and a terracotta ring
      const py = 46;
      cyl(x, py, 0, 2.2, 5.6, 1.6, LM.cream, 32);
      cyl(x, py + 1.6, 0, 5.8, 6.0, 2.4, LM.glass, 32, true);
      cyl(x, py + 4.0, 0, 6.2, 6.2, 1.4, LM.terra, 32);
      cyl(x, py + 5.4, 0, 5.6, 3.4, 1.2, LM.cream, 32);
      cyl(x, py + 6.6, 0, 1.2, 1.0, 10, LM.cream, 12);
      // the sky pod and the mast
      cyl(x, py + 16.6, 0, 2.2, 2.2, 1.6, LM.teal, 20);
      cyl(x, py + 18.2, 0, 2.0, 1.0, 1.0, LM.cream, 20);
      cyl(x, py + 19.2, 0, 0.8, 0.5, 6, LM.cream, 8);
      cyl(x, py + 25.2, 0, 0.5, 0.15, 10, LM.terra, 8);
      break;
    }
    case "dome": {
      // drum of dark glass bays, terracotta plinth, a ribbed cream roof with its slot
      const R = 16;
      cyl(x, 0, 0, R + 0.8, R + 0.8, 1.8, LM.terra, 56);
      const drum = new THREE.CylinderGeometry(R, R, 8, 56, 1, true);
      const uv = drum.attributes.uv;
      for (let i = 0; i < uv.count; i += 1) uv.setXY(i, uv.getX(i) * ((2 * Math.PI * R) / 3), uv.getY(i) * (8 / 3));
      putMat(at(drum.translate(0, 4, 0), x, 1.8, 0), mats.curtain);
      for (const yy of [1.8, 5.8, 9.8]) ring(x, yy, 0, R + 0.1, 0.35, LM.cream);
      for (let k = 0; k < 24; k += 1) {
        const a = (k / 24) * Math.PI * 2;
        rod(V(x + Math.cos(a) * (R + 0.15), 1.8, Math.sin(a) * (R + 0.15)), V(x + Math.cos(a) * (R + 0.15), 9.8, Math.sin(a) * (R + 0.15)), 0.22, LM.cream);
      }
      const capG = new THREE.SphereGeometry(R + 0.2, 56, 12, 0, Math.PI * 2, 0, Math.PI * 0.5);
      capG.scale(1, 0.5, 1);
      put(at(capG, x, 9.8, 0), LM.white);
      for (let k = 0; k < 16; k += 1) {
        const g = new THREE.TorusGeometry(R + 0.25, 0.18, 6, 24, Math.PI);
        g.scale(1, 0.5, 1);
        g.rotateY((k / 16) * Math.PI);
        put(at(g, x, 9.8, 0), LM.cream2);
      }
      // the retractable roof's opening seam
      const slot = new THREE.SphereGeometry(R + 0.3, 24, 6, -0.12, 0.24, 0, Math.PI * 0.5);
      slot.scale(1, 0.5, 1);
      put(at(slot, x, 9.8, 0), LM.slate);
      break;
    }
    case "tocityhall": {
      // podium, reflecting pool with arches, two curved towers around the saucer
      box(x, 1.5, 0, 30, 3, 26, LM.cream);
      box(x, 3.1, 0, 30.4, 0.3, 26.4, LM.terra);
      box(x - s * 11, 0.25, 0, 7, 0.4, 20, LM.water);
      for (const dz of [-8, -4, 0, 4, 8]) {
        const g = new THREE.TorusGeometry(2.2, 0.25, 6, 18, Math.PI);
        put(at(g, x - s * 11, 0.4, dz), LM.cream);
      }
      // each tower is a crescent in plan; its concave face (dark window ribs) looks at the saucer
      const tower = (h, flip) => {
        const Ro = 11;
        const Ri = 8.6;
        const a0 = -0.85;
        const a1 = 0.85;
        const sh = new THREE.Shape();
        sh.absarc(0, 0, Ro, a0, a1, false);
        sh.absarc(0, 0, Ri, a1, a0, true);
        const parts = [[new THREE.ExtrudeGeometry(sh, { depth: h, bevelEnabled: false, curveSegments: 18 }), LM.cream, false]];
        for (let k = 0; k < 11; k += 1) {
          const a = a0 + ((k + 0.5) / 11) * (a1 - a0);
          const rib = new THREE.BoxGeometry(0.3, 0.7, h - 1.2);
          rib.rotateZ(a);
          rib.translate(Math.cos(a) * (Ri - 0.12), Math.sin(a) * (Ri - 0.12), h / 2);
          parts.push([rib, LM.glass, true]);
        }
        for (const [g, color, gl] of parts) {
          // shape space (x, y, extrude z) -> world: extrude goes up; the arc's centre is the saucer
          g.rotateX(-Math.PI / 2);
          g.rotateY(flip ? -Math.PI / 2 : Math.PI / 2);
          put(at(g, x + s * 3, 3.2, 0), color, gl);
        }
      };
      tower(26, false);
      tower(20, true);
      cyl(x + s * 3, 3.2, 0, 5.4, 6.2, 2.2, LM.cream, 32);
      const sau = new THREE.SphereGeometry(6.2, 32, 8, 0, Math.PI * 2, 0, Math.PI * 0.5);
      sau.scale(1, 0.32, 1);
      put(at(sau, x + s * 3, 5.4, 0), LM.cream2);
      cyl(x + s * 3, 3.2, 0, 2.6, 2.6, 0.6, LM.terra, 24);
      break;
    }
    case "crystal": {
      // the heritage wing in stone with its arched portal, and the crystal bursting out
      boxTex(x + s * 3, 7, 0, 18, 14, 22, mats.arches, 3.5);
      box(x + s * 3, 0.8, 0, 18.6, 1.6, 22.6, LM.terra);
      box(x + s * 3, 14.3, 0, 19, 0.6, 23, LM.cream2);
      box(x + s * 3, 14.9, 0, 18, 0.6, 22, LM.teal);
      const fx = x + s * 3 - s * 9.1;
      box(fx, 6, -5, 0.6, 9, 6, LM.cream);
      box(fx - s * 0.2, 4.4, -5, 0.4, 6.4, 3.6, LM.glass, true);
      box(fx - s * 0.1, 9.2, -5, 0.5, 1.2, 4.8, LM.terra);
      // crystal: a few leaning prisms in glass, framed in cream
      // closed prisms leaning out toward the road, their tops sliced at an angle
      const shard = (pts, h, lean, tilt) => {
        const base = pts.map(([px, pz]) => [x + s * px, 0, pz]);
        const top = base.map(([bx, , bz], i) => [bx - s * lean, h + tilt * (i - 1.5), bz + lean * 0.3]);
        const pos = [];
        const quad = (a, b, c, d) => pos.push(...a, ...b, ...c, ...a, ...c, ...d);
        for (let i = 0; i < 4; i += 1) quad(base[i], base[(i + 1) % 4], top[(i + 1) % 4], top[i]);
        quad(top[0], top[1], top[2], top[3]);
        const g = new THREE.BufferGeometry();
        g.setAttribute("position", new THREE.Float32BufferAttribute(pos, 3));
        const uv = [];
        for (let i = 0; i < pos.length; i += 3) uv.push((pos[i] + pos[i + 2]) / 3, pos[i + 1] / 3);
        g.setAttribute("uv", new THREE.Float32BufferAttribute(uv, 2));
        g.computeVertexNormals();
        putMat(g, mats.curtainBoth);
        for (let i = 0; i < 4; i += 1) {
          rod(V(...base[i]), V(...top[i]), 0.16, LM.cream);
          rod(V(...top[i]), V(...top[(i + 1) % 4]), 0.16, LM.cream);
        }
      };
      shard([[-13, -2], [-8, -4], [-6, 3], [-11, 6]], 15, 4, 1.2);
      shard([[-10, -10], [-6, -12], [-4, -5], [-8, -3]], 11, 6, -1);
      shard([[-9, 5], [-5, 3], [-3, 10], [-8, 11]], 17, 3, 1.6);
      break;
    }
    case "castle": {
      // Casa Loma: stone walls, terracotta details, round towers with teal spires
      steps(x - s * 9, 0, 6, 8, 4, LM.terra);
      boxTex(x + s * 3, 6, 0, 16, 12, 20, mats.arches, 3);
      boxTex(x + s * 3, 4.5, -12, 12, 9, 6, mats.arches, 3);
      boxTex(x + s * 3, 4.5, 12, 12, 9, 6, mats.arches, 3);
      box(x + s * 3, 0.6, 0, 17, 1.2, 21, LM.terra);
      // crenellations along the roofline
      for (let k = 0; k < 9; k += 1) {
        for (const ez of [-10.2, 10.2]) box(x + s * 3 - 7 + k * 1.75, 12.5, ez, 0.9, 1, 0.6, LM.cream);
        box(x + s * 3 - s * 8.1, 12.5, -9 + k * 2.25, 0.6, 1, 1.1, LM.cream);
      }
      put(at(new THREE.ConeGeometry(11, 4, 4).rotateY(Math.PI / 4).scale(1, 1, 1.25).translate(0, 2, 0), x + s * 3, 12, 0), LM.teal);
      const tower = (tx, tz, r, h) => {
        cyl(tx, 0, tz, r, r, h, LM.cream, 20);
        ring(tx, h * 0.55, tz, r + 0.05, 0.18, LM.terra);
        cyl(tx, h, tz, r + 0.5, r + 0.5, 1, LM.cream2, 20);
        put(at(new THREE.ConeGeometry(r + 0.6, r * 2.6, 20).translate(0, r * 1.3, 0), tx, h + 1, tz), LM.teal);
        cyl(tx, h + 1 + r * 2.6, tz, 0.1, 0.08, 1.4, LM.terraDark, 6);
        for (let k = 0; k < 3; k += 1) box(tx - s * (r - 0.05), h * (0.3 + k * 0.2), tz, 0.2, 1.4, 0.8, LM.glass, true);
      };
      tower(x - s * 4.6, -9.6, 2.4, 17);
      tower(x - s * 4.6, 9.6, 2.2, 14);
      tower(x + s * 10, -6, 2.8, 20);
      tower(x + s * 10, 7, 2.0, 15);
      tower(x + s * 3, -15, 1.8, 12);
      tower(x + s * 3, 15, 1.8, 12);
      break;
    }
    default:
      return false;
  }
  return true;
}

/** Illustration cards (inlined by the mobile build, relative paths on the web). */
const art = (p) => (globalThis.DD_ASSETS && globalThis.DD_ASSETS[p]) || `assets/${p}`;

/** Which route landmark keys have a hero model (and an illustration card). */
export const HERO_ART = {
  reunion: art("art/lm-reunion-tower.webp"),
  archbridge: art("art/lm-margaret-hunt-hill-bridge.webp"),
  bofa: art("art/lm-bank-of-america-plaza.webp"),
  fountain: art("art/lm-fountain-place.webp"),
  omni: art("art/lm-omni-hotel.webp"),
  cntower: art("art/lm-cn-tower.webp"),
  dome: art("art/lm-rogers-centre.webp"),
  tocityhall: art("art/lm-city-hall.webp"),
  crystal: art("art/lm-royal-ontario-museum.webp"),
  castle: art("art/lm-casa-loma.webp"),
};

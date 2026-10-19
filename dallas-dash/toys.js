/**
 * Dallas Dash — the crew as toys. Each character is built like an action
 * figure: rounded plastic parts riding the bones of the shared rig, so the
 * whole animation library (run, jump, roll, stumble, dance…) drives them.
 *
 * The designs follow the client's character sheets (character_boy/girl/robot.png):
 * cream-and-red varsity kit, chunky sneakers, big cartoon heads, and the
 * delivery robot with a screen face and a teal satchel. The same part list is
 * the toy brief: every piece is a simple moulded shape in a flat colour.
 *
 * Proportions are made childlike by shortening the rig's limb bones (the
 * clips only animate rotations below the pelvis, so shorter bones just work)
 * and lowering the pelvis track to match, so feet stay on the ground.
 *
 * Per character: one toon mesh (vertex colours), one glow mesh (screen
 * eyes), one ink outline, one back patch — four draw calls.
 */

import * as THREE from "three";
import { mergeGeometries, mergeVertices } from "three/addons/utils/BufferGeometryUtils.js";

/* ── palette (sampled from the character sheets) ───────────────────── */

export const TOY = {
  cream: 0xf1e3c5,
  creamShade: 0xd9c6a1,
  red: 0xb32a2c,
  redDeep: 0x8a1c22,
  white: 0xfbf8f1,
  charcoal: 0x34302f,
  charcoalDark: 0x23201f,
  skin: 0xc98d62,
  skinShade: 0xb47650,
  hairBoy: 0x5b351f,
  hairGirl: 0x4f2d1a,
  eyeBoy: 0x5a3418,
  eyeGirl: 0x1f1714,
  brow: 0x3b2215,
  mouth: 0x6e2a22,
  graphite: 0x3a3d45,
  screen: 0x252a33,
  cyan: 0x8ff2ea,
  teal: 0x3f8f87,
  navy: 0x1f2b4f,
  navyDark: 0x161e38,
  hivis: 0xd4ec3a,
  silver: 0xd9dde3,
  gold: 0xe6b54a,
  ink: 0x1c1412,
};

const UP = new THREE.Vector3(0, 1, 0);
const FWD = new THREE.Vector3(0, 0, 1);
const tmpM = new THREE.Matrix4();

/** A rounded solid: an ellipsoid pushed toward a box as `p` rises above 2. */
function blobGeometry(p = 2, w = 20, h = 14) {
  const g = new THREE.SphereGeometry(1, w, h);
  if (p !== 2) {
    const a = g.attributes.position;
    const e = 2 / p;
    for (let i = 0; i < a.count; i += 1) {
      const f = (t) => Math.sign(t) * Math.abs(t) ** e;
      a.setXYZ(i, f(a.getX(i)), f(a.getY(i)), f(a.getZ(i)));
    }
  }
  return g;
}

/** Collects coloured, bone-tagged parts and merges them into skinned meshes. */
class Kit {
  constructor(boneIndex) {
    this.boneIndex = boneIndex;
    this.parts = [];
  }

  push(geo, color, bone, opts = {}) {
    const g = geo.index ? geo.toNonIndexed() : geo;
    g.deleteAttribute("uv");
    const n = g.attributes.position.count;
    const c = new THREE.Color(color);
    const col = new Float32Array(n * 3);
    for (let i = 0; i < n; i += 1) col.set([c.r, c.g, c.b], i * 3);
    g.setAttribute("color", new THREE.BufferAttribute(col, 3));
    const bi = this.boneIndex(bone);
    const si = new Uint16Array(n * 4);
    const sw = new Float32Array(n * 4);
    for (let i = 0; i < n; i += 1) {
      si[i * 4] = bi;
      sw[i * 4] = 1;
    }
    g.setAttribute("skinIndex", new THREE.BufferAttribute(si, 4));
    g.setAttribute("skinWeight", new THREE.BufferAttribute(sw, 4));
    this.parts.push({ g, glow: Boolean(opts.glow), outline: opts.outline !== false && !opts.glow });
    return g;
  }

  /**
   * A rounded solid at `c` with radii `r` ([x, y, z]). Orient it with `up`
   * (its y axis), `face` (its z axis) or a `quat`.
   */
  blob(c, r, color, bone, opts = {}) {
    const g = blobGeometry(opts.p ?? 2, opts.w ?? 20, opts.h ?? 14);
    g.scale(r[0], r[1], r[2]);
    const g2 = mergeVertices(g.deleteAttribute("uv") && g);
    g2.computeVertexNormals();
    const q = opts.quat || (opts.up ? new THREE.Quaternion().setFromUnitVectors(UP, opts.up.clone().normalize()) : opts.face ? new THREE.Quaternion().setFromUnitVectors(FWD, opts.face.clone().normalize()) : null);
    if (opts.spin) g2.rotateY(opts.spin);
    if (q) g2.applyQuaternion(q);
    g2.translate(c.x, c.y, c.z);
    return this.push(g2, color, bone, opts);
  }

  /** The upper half of a blob (cap crowns, helmets). */
  dome(c, r, color, bone, opts = {}) {
    const g = new THREE.SphereGeometry(1, 24, 10, 0, Math.PI * 2, 0, Math.PI / 2);
    g.scale(r[0], r[1], r[2]);
    if (opts.tilt) g.rotateX(opts.tilt);
    g.translate(c.x, c.y, c.z);
    return this.push(g, color, bone, opts);
  }

  /** A tapered tube from a (radius ra) to b (radius rb), with rounded ends. */
  limb(a, b, ra, rb, color, bone, opts = {}) {
    const d = b.clone().sub(a);
    const len = d.length();
    const flat = opts.flat ?? 1; // depth / width of the cross-section
    const q = new THREE.Quaternion().setFromUnitVectors(UP, d.normalize());
    const g = new THREE.CylinderGeometry(rb, ra, len, opts.seg ?? 16, 1, true);
    g.translate(0, len / 2, 0);
    g.scale(1, 1, flat);
    g.applyQuaternion(q);
    g.translate(a.x, a.y, a.z);
    this.push(g, color, bone, opts);
    if (opts.caps !== false) {
      const cap = { ...opts, w: 16, h: 12, quat: q };
      this.blob(a, [ra, ra * (opts.capY ?? 1), ra * flat], color, bone, cap);
      this.blob(b, [rb, rb * (opts.capY ?? 1), rb * flat], color, bone, cap);
    }
  }

  /** A ring around the axis `dir` at `c` (cuffs, hems, cap piping). */
  ring(c, dir, radius, tube, color, bone, opts = {}) {
    const g = new THREE.TorusGeometry(radius, tube, 8, 28, opts.arc ?? Math.PI * 2);
    if (opts.squash) g.scale(1, opts.squash, 1);
    if (opts.rotZ) g.rotateZ(opts.rotZ);
    g.applyQuaternion(new THREE.Quaternion().setFromUnitVectors(FWD, dir.clone().normalize()));
    g.translate(c.x, c.y, c.z);
    return this.push(g, color, bone, opts);
  }

  /** Merge into the three skinned geometries (toon, glow, outline). */
  build(outlineWidth) {
    const toon = this.parts.filter((p) => !p.glow).map((p) => p.g);
    const glow = this.parts.filter((p) => p.glow).map((p) => p.g);
    const ink = this.parts.filter((p) => p.outline).map((p) => {
      const g = p.g.clone();
      const pos = g.attributes.position;
      const nrm = g.attributes.normal;
      for (let i = 0; i < pos.count; i += 1) {
        pos.setXYZ(i, pos.getX(i) + nrm.getX(i) * outlineWidth, pos.getY(i) + nrm.getY(i) * outlineWidth, pos.getZ(i) + nrm.getZ(i) * outlineWidth);
      }
      g.deleteAttribute("color");
      return g;
    });
    return {
      toon: mergeGeometries(toon),
      glow: glow.length ? mergeGeometries(glow) : null,
      ink: ink.length ? mergeGeometries(ink) : null,
    };
  }
}

/* ── proportions ───────────────────────────────────────────────────── */

/**
 * Shorten legs (kl) and arms (ka) on a freshly cloned skeleton. Returns the
 * factor the pelvis height (and its animation track) must be scaled by.
 */
export function reproportion(skeleton, root, kl, ka, kt = 1) {
  const B = Object.fromEntries(skeleton.bones.map((b) => [b.name, b]));
  const world = (n) => new THREE.Vector3().setFromMatrixPosition(B[n].matrixWorld);
  root.updateMatrixWorld(true);
  const legLen = world("thigh_l").y - world("foot_l").y;
  const pelvisY = world("pelvis").y;
  for (const s of ["l", "r"]) {
    B[`calf_${s}`].position.multiplyScalar(kl);
    B[`foot_${s}`].position.multiplyScalar(kl);
    B[`lowerarm_${s}`].position.multiplyScalar(ka);
    B[`hand_${s}`].position.multiplyScalar(ka);
  }
  for (const n of ["spine_02", "spine_03", "neck_01"]) B[n].position.multiplyScalar(kt);
  const r = (pelvisY - (1 - kl) * legLen) / pelvisY;
  B.pelvis.position.multiplyScalar(r);
  root.updateMatrixWorld(true);
  skeleton.calculateInverses();
  return r;
}

/** Clips with the pelvis track scaled to the shorter legs. */
export function scaleClips(clips, r) {
  if (Math.abs(r - 1) < 1e-4) return clips;
  return clips.map((c) => {
    const k = c.clone();
    for (const t of k.tracks) {
      if (t.name === "pelvis.position") for (let i = 0; i < t.values.length; i += 1) t.values[i] *= r;
    }
    return k;
  });
}

/* ── the designs ───────────────────────────────────────────────────── */

export const TOY_SPEC = {
  dante: { body: "male", kl: 0.63, ka: 0.8, kt: 0.86, kid: true, hair: "boy", cap: "back", eyes: TOY.eyeBoy, hairColor: TOY.hairBoy },
  maya: { body: "female", kl: 0.64, ka: 0.82, kt: 0.86, kid: true, hair: "ponytail", cap: "front", eyes: TOY.eyeGirl, hairColor: TOY.hairGirl, lashes: true, slim: 0.92 },
  r11: { body: "male", kl: 0.7, ka: 0.86, kt: 0.9, robot: true },
  officer: { body: "male", kl: 0.84, ka: 0.9, kt: 0.95, officer: true, eyes: 0x2a1c14, hairColor: 0x2a1c14 },
};

/** Build the toy geometry for `id` on a skeleton already in its new rest pose. */
export function buildToy(id, skeleton) {
  const spec = TOY_SPEC[id] || TOY_SPEC.dante;
  const names = skeleton.bones.map((b) => b.name);
  const kit = new Kit((n) => Math.max(0, names.indexOf(n)));
  const J = {};
  for (const b of skeleton.bones) J[b.name] = new THREE.Vector3().setFromMatrixPosition(b.matrixWorld);
  const v = (x, y, z) => new THREE.Vector3(x, y, z);
  const at = (n, x = 0, y = 0, z = 0) => J[n].clone().add(v(x, y, z));
  const lerp = (a, b, t) => a.clone().lerp(b, t);

  if (spec.robot) robot(kit, J, at, lerp, v);
  else person(kit, spec, J, at, lerp, v);
  return { ...kit.build(spec.robot ? 0.011 : 0.01), spec, patch: kit.patch || null };
}

/** The varsity kids and the officer share one body plan. */
function person(kit, s, J, at, lerp, v) {
  const slim = s.slim || 1;
  const kid = Boolean(s.kid);
  const top = s.officer ? TOY.navy : TOY.cream;
  const sleeve = s.officer ? TOY.navy : TOY.red;
  const legs = s.officer ? TOY.navyDark : TOY.charcoal;

  /* head */
  const hr = kid ? [0.152, 0.162, 0.146] : [0.122, 0.135, 0.118];
  const H = at("Head", 0, kid ? 0.095 : 0.09, 0.03);
  kit.limb(at("neck_01"), at("Head", 0, 0.02, 0.01), 0.045, 0.045, TOY.skin, "neck_01", { caps: false });
  kit.blob(H, hr, TOY.skin, "Head", { p: 2.1 });
  // a point on the face: dx, dy across the front of the head ellipsoid
  const onFace = (dx, dy, lift = 0) => {
    const z = hr[2] * Math.sqrt(Math.max(0.02, 1 - (dx / hr[0]) ** 2 - (dy / hr[1]) ** 2));
    const p = v(H.x + dx, H.y + dy, H.z + z);
    const n = v(dx / hr[0] ** 2, dy / hr[1] ** 2, z / hr[2] ** 2).normalize();
    return { p: p.addScaledVector(n, lift), n };
  };
  for (const sx of [-1, 1]) {
    kit.blob(v(H.x + sx * hr[0] * 0.97, H.y - 0.012, H.z - 0.012), [0.026, 0.04, 0.024], TOY.skin, "Head");
    // eyes: white, iris, pupil, a highlight, and the lid line that gives the toon look
    const ex = sx * (kid ? 0.056 : 0.044);
    const ey = kid ? 0.004 : 0.012;
    const e = onFace(ex, ey, -0.004);
    const er = kid ? [0.033, 0.041, 0.013] : [0.022, 0.024, 0.01];
    kit.blob(e.p, er, TOY.white, "Head", { face: e.n, outline: false, w: 14, h: 10 });
    const ir = onFace(ex - sx * 0.006, ey - 0.004, 0.0075);
    kit.blob(ir.p, [er[0] * 0.72, er[1] * 0.72, 0.008], s.eyes, "Head", { face: ir.n, outline: false, w: 14, h: 10 });
    const pu = onFace(ex - sx * 0.006, ey - 0.004, 0.0125);
    kit.blob(pu.p, [er[0] * 0.36, er[1] * 0.36, 0.005], 0x0c0807, "Head", { face: pu.n, outline: false, w: 10, h: 8 });
    const hl = onFace(ex - sx * 0.002 + er[0] * 0.3, ey + er[1] * 0.3, 0.016);
    kit.blob(hl.p, [er[0] * 0.24, er[0] * 0.24, 0.004], 0xffffff, "Head", { face: hl.n, outline: false, w: 8, h: 6 });
    const lid = onFace(ex, ey + er[1] * 0.88, 0.003);
    kit.blob(lid.p, [er[0] * 1.08, 0.007, 0.008], TOY.ink, "Head", { face: lid.n, outline: false, w: 12, h: 6 });
    if (s.lashes) {
      const la = onFace(ex + sx * er[0] * 0.95, ey + er[1] * 0.75, 0.004);
      kit.blob(la.p, [0.014, 0.006, 0.006], TOY.ink, "Head", { face: la.n, outline: false, quat: new THREE.Quaternion().setFromEuler(new THREE.Euler(0, 0, sx * 0.6)), w: 8, h: 6 });
    }
    // brows: thick and a little raised at the outer end, like the sheets
    const br = onFace(ex + sx * 0.004, ey + (kid ? 0.066 : 0.045), 0.002);
    kit.blob(br.p, [kid ? 0.036 : 0.03, kid ? 0.011 : 0.009, 0.01], s.officer ? 0x2a1c14 : TOY.brow, "Head", {
      outline: false, quat: new THREE.Quaternion().setFromUnitVectors(FWD, br.n).multiply(new THREE.Quaternion().setFromAxisAngle(FWD, sx * 0.14)), w: 12, h: 8,
    });
  }
  const nose = onFace(0, kid ? -0.03 : -0.02, 0.004);
  kit.blob(nose.p, [0.02, 0.016, 0.018], TOY.skinShade, "Head", { face: nose.n, outline: false, w: 12, h: 8 });
  // the smile: a short arc, a little off-centre the way the sheets draw it
  const m = onFace(0.006, kid ? -0.072 : -0.06, 0.002);
  kit.ring(m.p.clone().add(v(0, 0.018, 0)), m.n, 0.024, 0.0045, TOY.mouth, "Head", { arc: Math.PI * 0.6, rotZ: Math.PI * 1.2, outline: false });
  if (s.officer) {
    const mo = onFace(0, -0.042, 0.004);
    kit.blob(mo.p, [0.042, 0.012, 0.012], 0x2a1c14, "Head", { face: mo.n, outline: false });
  }

  /* hair and cap */
  if (s.hair) {
    kit.blob(v(H.x, H.y + 0.015, H.z - 0.045), [hr[0] * 1.02, hr[1] * 0.85, hr[2] * 0.92], s.hairColor, "Head", { p: 2.2 });
    for (const sx of [-1, 1]) kit.blob(v(H.x + sx * 0.13, H.y + 0.02, H.z + 0.01), [0.035, 0.06, 0.06], s.hairColor, "Head");
  }
  if (s.hair === "boy") {
    // messy bangs poking out under the cap, swept to one side
    for (const [dx, dy, tilt, len] of [[-0.08, 0.05, -1.0, 0.045], [-0.025, 0.058, -0.8, 0.05], [0.035, 0.056, -0.6, 0.048], [0.095, 0.045, -0.4, 0.04]]) {
      const f = onFace(dx, dy, -0.012);
      kit.blob(f.p, [0.022, len, 0.022], s.hairColor, "Head", { quat: new THREE.Quaternion().setFromUnitVectors(FWD, f.n).multiply(new THREE.Quaternion().setFromAxisAngle(FWD, tilt)), w: 10, h: 8 });
    }
    // tufts at the back, under the turned cap
    for (const dx of [-0.07, 0, 0.07]) kit.blob(v(H.x + dx, H.y - 0.03, H.z - 0.14), [0.04, 0.055, 0.03], s.hairColor, "Head", { quat: new THREE.Quaternion().setFromEuler(new THREE.Euler(-0.8, 0, dx * 4)) });
  }
  if (s.hair === "ponytail") {
    // pulled through the back of the cap, up and then falling
    const tail = [[0, 0.06, -0.15, 0.052], [0, 0.1, -0.2, 0.06], [0.01, 0.08, -0.27, 0.062], [0.02, 0.01, -0.31, 0.056], [0.025, -0.07, -0.3, 0.048], [0.02, -0.14, -0.26, 0.038], [0.01, -0.19, -0.21, 0.026]];
    for (const [dx, dy, dz, r] of tail) kit.blob(v(H.x + dx, H.y + dy, H.z + dz), [r, r * 1.15, r], s.hairColor, "Head", { w: 14, h: 10 });
    kit.ring(v(H.x, H.y + 0.075, H.z - 0.165), v(0, 0.5, -1), 0.04, 0.012, TOY.red, "Head");
    // locks framing the face under the brim
    for (const [dx, tilt] of [[-1, -0.35], [1, 0.35]]) {
      const f = onFace(dx * 0.12, 0.0, -0.02);
      kit.blob(f.p, [0.028, 0.065, 0.026], s.hairColor, "Head", { quat: new THREE.Quaternion().setFromEuler(new THREE.Euler(0.2, 0, tilt)) });
    }
  }
  if (s.cap || s.officer) {
    const crown = s.officer ? TOY.navy : TOY.cream;
    const brimC = s.officer ? TOY.navyDark : TOY.red;
    const cy = H.y + (kid ? 0.03 : 0.045);
    const cr = [hr[0] * 1.08, hr[1] * 0.98, hr[2] * 1.09];
    const cc = v(H.x, cy, H.z - 0.006);
    kit.dome(cc, cr, crown, "Head");
    kit.blob(cc.clone().add(v(0, cr[1] - 0.004, 0)), [0.018, 0.012, 0.018], brimC, "Head");
    // panel seams over the crown
    for (const a of [0.5, 1.6, 2.6]) kit.ring(cc, v(Math.cos(a), 0, Math.sin(a)), cr[0] * 0.998, 0.0035, s.officer ? TOY.navyDark : TOY.creamShade, "Head", { arc: Math.PI, outline: false });
    // the brim: turned back and to the side (Dante), forwards (Maya, the officer)
    const yaw = s.cap === "back" ? Math.PI * 0.82 : 0;
    const dir = v(Math.sin(yaw), 0, Math.cos(yaw));
    const bc = cc.clone().add(v(0, 0.006, 0)).addScaledVector(dir, cr[2] * 1.1);
    kit.blob(bc, [0.1, 0.012, 0.085], brimC, "Head", { p: 2.6, quat: new THREE.Quaternion().setFromEuler(new THREE.Euler(s.cap === "back" ? 0.12 : -0.16, yaw, 0, "YXZ")) });
    if (s.officer) kit.blob(onFace(0, hr[1] * 0.62, 0.03).p, [0.022, 0.022, 0.008], TOY.gold, "Head", { outline: false });
  }

  /* torso: the varsity jacket over a red hoodie (or the officer's shirt and vest) */
  const P = J.pelvis;
  const S1 = J.spine_01;
  const S3 = J.spine_03;
  const N = J.neck_01;
  const tw = (kid ? 0.165 : 0.2) * slim;
  const FLAT = 0.76;
  const lowC = lerp(S1, S3, 0.15);
  const upC = lerp(S3, N, 0.2);
  const waist = v(lowC.x, P.y + 0.07, lowC.z);
  const midT = lerp(waist, upC, 0.5);
  const chestTop = v(upC.x, N.y - 0.03, upC.z);
  kit.limb(waist, midT, tw * 0.95, tw * 1.02, top, "spine_01", { flat: FLAT, capY: 0.5 });
  kit.limb(midT, chestTop, tw * 1.02, tw * 1.08, top, "spine_03", { flat: FLAT, capY: 0.6 });
  // where the jacket's front surface is at a given height
  const fz = (c, r) => c.z + r * FLAT - 0.003;
  if (!s.officer) kit.patch = { p: v(0, upC.y - 0.02, upC.z - tw * 1.06 * FLAT - 0.006), r: 0.08 };
  if (!s.officer) {
    // the hoodie shows down the open front
    // a flat red panel that follows the jacket's front from hem to collar
    kit.limb(v(0, waist.y + 0.01, fz(waist, tw * 0.95) - 0.006), v(0, chestTop.y - 0.02, fz(chestTop, tw * 1.08) - 0.006), 0.036, 0.046, TOY.red, "spine_03", { flat: 0.45, capY: 0.3, outline: false });
    for (const sx of [-1, 1]) kit.limb(v(sx * 0.018, chestTop.y - 0.04, fz(chestTop, tw * 1.08) + 0.012), v(sx * 0.02, chestTop.y - 0.12, fz(upC, tw * 1.05) + 0.012), 0.005, 0.005, TOY.white, "spine_03", { outline: false });
    // the hood bunched behind the neck and the ribbed collar
    kit.blob(at("neck_01", 0, -0.035, -0.08), [0.115, 0.07, 0.07], TOY.red, "spine_03");
    kit.ring(at("neck_01", 0, -0.04, 0.0), UP, 0.08, 0.022, TOY.red, "spine_03", { squash: 0.85 });
    // snaps and slit pockets
    for (const sx of [-1, 1]) {
      kit.blob(v(sx * 0.07, upC.y + 0.03, fz(upC, tw * 0.98)), [0.011, 0.011, 0.006], TOY.red, "spine_03", { outline: false });
      kit.blob(v(sx * 0.1, waist.y + 0.07, fz(waist, tw * 0.88)), [0.008, 0.045, 0.008], TOY.creamShade, "spine_01", { outline: false, quat: new THREE.Quaternion().setFromEuler(new THREE.Euler(0, 0, sx * 0.35)) });
    }
    // the striped hem band
    const hy = waist.y - 0.02;
    kit.ring(v(0, hy, waist.z), UP, tw * 0.94, 0.028, TOY.red, "spine_01", { squash: FLAT });
    kit.ring(v(0, hy + 0.011, waist.z), UP, tw * 0.975, 0.0055, TOY.white, "spine_01", { squash: FLAT, outline: false });
    kit.ring(v(0, hy - 0.009, waist.z), UP, tw * 0.975, 0.0055, TOY.white, "spine_01", { squash: FLAT, outline: false });
  } else {
    // hi-vis vest with reflective bands, a badge
    kit.limb(v(0, waist.y + 0.03, waist.z), chestTop.clone().add(v(0, -0.03, 0)), tw * 1.0, tw * 1.12, TOY.hivis, "spine_03", { flat: 0.8, capY: 0.4 });
    for (const dy of [-0.05, 0.05]) kit.ring(midT.clone().add(v(0, dy, 0)), UP, tw * 1.09, 0.011, TOY.silver, "spine_03", { squash: 0.8, outline: false });
    kit.blob(v(0.07, upC.y + 0.03, fz(upC, tw * 1.12) + 0.01), [0.022, 0.026, 0.008], TOY.gold, "spine_03", { outline: false });
    kit.ring(at("neck_01", 0, -0.04, 0.005), UP, 0.075, 0.02, TOY.navy, "spine_03", { squash: 0.8 });
  }
  /* hips and legs: baggy cargo trousers */
  kit.blob(P.clone().add(v(0, -0.01, 0.005)), [tw * 0.98, 0.11, tw * 0.78], legs, "pelvis", { p: 2.3 });
  for (const sd of ["l", "r"]) {
    const sx = sd === "l" ? 1 : -1;
    const T = J[`thigh_${sd}`];
    const C = J[`calf_${sd}`];
    const F = J[`foot_${sd}`];
    const lr = (kid ? 0.078 : 0.082) * slim;
    kit.limb(T, C, lr, lr * 0.95, legs, `thigh_${sd}`);
    kit.limb(C, F.clone().add(v(0, 0.06, 0)), lr * 0.95, lr * 0.9, legs, `calf_${sd}`);
    // the trousers bunch over the shoes
    kit.blob(F.clone().add(v(0, 0.075, 0.005)), [lr * 1.05, 0.045, lr * 1.05], legs, `calf_${sd}`);
    if (!s.officer) {
      const pc = lerp(T, C, 0.55).add(v(sx * lr * 0.95, 0, 0.01));
      kit.blob(pc, [0.026, 0.065, 0.05], TOY.charcoalDark, `thigh_${sd}`, { p: 3.2 });
      kit.blob(pc.clone().add(v(sx * 0.008, 0.055, 0)), [0.024, 0.014, 0.052], TOY.charcoalDark, `thigh_${sd}`, { p: 3, outline: false });
    }
    /* chunky sneakers: red uppers, white soles and toe caps */
    const toe = J[`ball_leaf_${sd}`];
    const shoeC = lerp(F, toe, 0.48);
    const shoeUp = s.officer ? 0x121214 : TOY.red;
    const soleC = s.officer ? 0x0b0b0c : TOY.white;
    kit.blob(v(shoeC.x, 0.065, shoeC.z), [0.07, 0.062, 0.135], shoeUp, `foot_${sd}`, { p: 2.5 });
    kit.blob(v(shoeC.x, 0.018, shoeC.z + 0.01), [0.075, 0.02, 0.145], soleC, `foot_${sd}`, { p: 5 });
    if (!s.officer) {
      kit.blob(v(toe.x, 0.045, toe.z - 0.03), [0.06, 0.04, 0.045], TOY.white, `ball_${sd}`, { p: 2.4 });
      kit.blob(v(shoeC.x, 0.07, F.z - 0.065), [0.062, 0.035, 0.025], TOY.redDeep, `foot_${sd}`, { p: 2.6 });
      kit.blob(v(shoeC.x + sx * 0.067, 0.06, shoeC.z - 0.01), [0.006, 0.026, 0.06], TOY.redDeep, `foot_${sd}`, { outline: false });
      for (let i = 0; i < 3; i += 1) kit.blob(v(shoeC.x, 0.108, shoeC.z + 0.01 + i * 0.03), [0.036, 0.006, 0.008], TOY.cream, `foot_${sd}`, { outline: false });
    }
  }

  /* arms: puffy red sleeves, striped cuffs, mitten hands */
  for (const sd of ["l", "r"]) {
    const U = J[`upperarm_${sd}`];
    const L = J[`lowerarm_${sd}`];
    const Hd = J[`hand_${sd}`];
    const dir = Hd.clone().sub(L).normalize();
    const ar = (kid ? 0.062 : 0.068) * slim;
    kit.blob(U, [ar * 1.2, ar * 1.2, ar * 1.2], sleeve, `upperarm_${sd}`);
    kit.limb(U, L, ar * 1.08, ar, sleeve, `upperarm_${sd}`);
    const wrist = Hd.clone().addScaledVector(dir, -0.035);
    kit.limb(L, wrist, ar, ar * 0.92, sleeve, `lowerarm_${sd}`);
    kit.ring(wrist, dir, ar * 0.9, 0.022, sleeve, `lowerarm_${sd}`);
    if (!s.officer) {
      for (const k of [-0.012, 0.01]) kit.ring(wrist.clone().addScaledVector(dir, k), dir, ar * 0.93, 0.0055, TOY.white, `lowerarm_${sd}`, { outline: false });
    }
    // the hand: a mitten with a thumb, palm facing down in the rest pose
    const hc = Hd.clone().addScaledVector(dir, 0.055);
    kit.blob(hc, [0.06, 0.03, 0.048], TOY.skin, `hand_${sd}`, { quat: new THREE.Quaternion().setFromUnitVectors(new THREE.Vector3(1, 0, 0), dir) });
    kit.limb(Hd.clone().addScaledVector(dir, 0.02).add(v(0, -0.005, 0.035)), Hd.clone().addScaledVector(dir, 0.05).add(v(0, -0.01, 0.06)), 0.017, 0.015, TOY.skin, `hand_${sd}`);
  }
}

/** R-11, the delivery robot: cream shell, red joints, a screen for a face. */
function robot(kit, J, at, lerp, v) {
  const C = TOY.cream;
  const R = TOY.red;
  const G = TOY.graphite;
  /* head: a rounded box with a dark screen and two glowing eyes */
  const H = at("Head", 0, 0.13, 0.02);
  kit.blob(H, [0.165, 0.14, 0.145], C, "Head", { p: 3.2 });
  kit.blob(H.clone().add(v(0, -0.005, 0.13)), [0.13, 0.095, 0.03], TOY.screen, "Head", { p: 3.6, outline: false });
  for (const sx of [-1, 1]) {
    kit.blob(H.clone().add(v(sx * 0.05, 0.005, 0.158)), [0.018, 0.03, 0.006], TOY.cyan, "Head", { p: 3, glow: true });
    // ear discs
    kit.limb(H.clone().add(v(sx * 0.15, -0.005, 0)), H.clone().add(v(sx * 0.185, -0.005, 0)), 0.055, 0.05, R, "Head", { caps: false });
    kit.blob(H.clone().add(v(sx * 0.186, -0.005, 0)), [0.012, 0.045, 0.045], R, "Head");
  }
  // antenna with a red ball
  kit.limb(H.clone().add(v(0, 0.12, -0.01)), H.clone().add(v(0, 0.22, -0.01)), 0.009, 0.009, G, "Head", { caps: false });
  kit.blob(H.clone().add(v(0, 0.235, -0.01)), [0.026, 0.026, 0.026], R, "Head");
  kit.limb(H.clone().add(v(0, 0.13, -0.01)), H.clone().add(v(0, 0.14, -0.01)), 0.03, 0.03, R, "Head", { caps: false });
  kit.limb(at("neck_01", 0, -0.04), at("Head", 0, 0.03), 0.04, 0.04, G, "neck_01", { caps: false });

  /* torso: chest shell with a red band and yoke; dark waist; cream hips */
  const S1 = J.spine_01;
  const S3 = J.spine_03;
  const N = J.neck_01;
  const chest = lerp(S3, N, 0.2);
  kit.blob(chest, [0.19, 0.17, 0.14], C, "spine_03", { p: 2.8 });
  kit.patch = { p: v(0, chest.y + 0.03, chest.z - 0.143), r: 0.075 };
  kit.blob(chest.clone().add(v(0, -0.07, 0.004)), [0.193, 0.045, 0.143], R, "spine_03", { p: 2.8 });
  kit.blob(chest.clone().add(v(0, 0.11, 0.0)), [0.17, 0.045, 0.125], R, "spine_03", { p: 2.6 });
  kit.limb(lerp(S1, S3, -0.2), lerp(S1, S3, 0.55), 0.11, 0.12, G, "spine_01", { caps: false });
  kit.blob(J.pelvis.clone().add(v(0, -0.01, 0.01)), [0.16, 0.1, 0.12], C, "pelvis", { p: 2.6 });
  // the teal satchel: strap across the chest, bag at the right hip
  kit.limb(chest.clone().add(v(0.12, 0.13, 0.11)), J.pelvis.clone().add(v(-0.13, 0.06, 0.12)), 0.011, 0.011, TOY.teal, "spine_03", { caps: false });
  kit.limb(chest.clone().add(v(0.12, 0.13, -0.11)), chest.clone().add(v(0.13, 0.15, 0.1)), 0.011, 0.011, TOY.teal, "spine_03", { caps: false });
  kit.limb(chest.clone().add(v(0.12, 0.13, -0.11)), J.pelvis.clone().add(v(-0.16, 0.06, -0.06)), 0.011, 0.011, TOY.teal, "spine_03", { caps: false });
  const bag = J.pelvis.clone().add(v(-0.19, -0.02, 0.04));
  kit.blob(bag, [0.045, 0.075, 0.08], TOY.teal, "pelvis", { p: 3.2 });
  kit.blob(bag.clone().add(v(-0.005, 0.045, 0)), [0.048, 0.03, 0.082], 0x35796f, "pelvis", { p: 3.2, outline: false });

  /* arms: red ball shoulders, dark rods, cream forearm shells, claw hands */
  for (const sd of ["l", "r"]) {
    const U = J[`upperarm_${sd}`];
    const L = J[`lowerarm_${sd}`];
    const Hd = J[`hand_${sd}`];
    const dir = Hd.clone().sub(L).normalize();
    kit.blob(U, [0.085, 0.085, 0.085], R, `upperarm_${sd}`);
    kit.limb(U, L, 0.03, 0.03, G, `upperarm_${sd}`);
    kit.limb(L.clone().addScaledVector(dir, 0.02), Hd.clone().addScaledVector(dir, -0.03), 0.068, 0.062, C, `lowerarm_${sd}`);
    kit.ring(Hd.clone().addScaledVector(dir, -0.05), dir, 0.064, 0.016, R, `lowerarm_${sd}`);
    const palm = Hd.clone().addScaledVector(dir, 0.02);
    kit.blob(palm, [0.035, 0.035, 0.035], G, `hand_${sd}`);
    for (const [dz, len] of [[-0.022, 0.07], [0.02, 0.07], [0.04, 0.05]]) {
      kit.limb(palm.clone().add(v(0, -0.005, dz)), palm.clone().addScaledVector(dir, len).add(v(0, -0.025, dz * 1.2)), 0.012, 0.01, G, `hand_${sd}`);
    }
  }
  /* legs: red ball hips and knees, dark rods, cream boots with red bands */
  for (const sd of ["l", "r"]) {
    const T = J[`thigh_${sd}`];
    const K = J[`calf_${sd}`];
    const F = J[`foot_${sd}`];
    const toe = J[`ball_leaf_${sd}`];
    kit.blob(T.clone().add(v(0, -0.02, 0)), [0.075, 0.075, 0.075], R, `thigh_${sd}`);
    kit.limb(T, K, 0.035, 0.035, G, `thigh_${sd}`);
    kit.limb(K.clone().add(v(0, -0.02, 0)), F.clone().add(v(0, 0.07, 0)), 0.068, 0.082, C, `calf_${sd}`);
    kit.ring(F.clone().add(v(0, 0.1, 0)), UP, 0.083, 0.02, R, `calf_${sd}`);
    const shoe = lerp(F, toe, 0.45);
    kit.blob(v(shoe.x, 0.055, shoe.z), [0.078, 0.055, 0.13], TOY.white, `foot_${sd}`, { p: 2.4 });
    kit.blob(v(shoe.x, 0.018, shoe.z), [0.082, 0.018, 0.138], R, `foot_${sd}`, { p: 5 });
  }
}

/* ── materials ─────────────────────────────────────────────────────── */

let shared = null;
export function toyMaterials() {
  if (shared) return shared;
  // three soft steps: the flat cel shading of the character sheets
  const steps = new Uint8Array([110, 175, 235, 255]);
  const grad = new THREE.DataTexture(steps, steps.length, 1, THREE.RedFormat);
  grad.minFilter = THREE.NearestFilter;
  grad.magFilter = THREE.NearestFilter;
  grad.generateMipmaps = false;
  grad.needsUpdate = true;
  shared = {
    toon: new THREE.MeshToonMaterial({ vertexColors: true, gradientMap: grad }),
    glow: new THREE.MeshBasicMaterial({ vertexColors: true, toneMapped: false }),
    ink: new THREE.MeshBasicMaterial({ color: TOY.ink, side: THREE.BackSide }),
  };
  return shared;
}

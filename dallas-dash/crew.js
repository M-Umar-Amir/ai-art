/**
 * Dallas Dash — the crew. Three original characters (and the parking officer
 * who chases them) built on scanned-quality CC0 base bodies with modelled
 * faces, sharing one skeleton and one animation library.
 *
 * Clothes are real skinned meshes cut from the body by bone region and pushed
 * out along the normals, so they bend with every animation for free. Branded
 * kit (the delivery bag, the Colonel patches) rides on the spine bone.
 */

import * as THREE from "three";
import * as SkeletonUtils from "three/addons/utils/SkeletonUtils.js";

/* ── who wears what ─────────────────────────────────────────────────── */

const RED = 0xd8102e;
const RED_DEEP = 0x9c0a22;
const WHITE = 0xf4f2ee;
const GRAPHITE = 0x2a2d34;

export const CREW_SPEC = {
  dante: {
    body: "male",
    hair: "Hair_Buzzed",
    skin: null,
    garments: [
      { region: "torso+arms", lift: 0.022, drape: 10, color: RED, rough: 0.85, name: "hoodie" },
      { region: "legs", lift: 0.016, drape: 8, color: 0x2f3440, rough: 0.9, name: "joggers" },
      { region: "feet", lift: 0.014, color: WHITE, rough: 0.55, name: "sneakers" },
      { region: "cap", lift: 0.014, color: RED, rough: 0.8, name: "cap" },
    ],
    brim: true,
    bag: true,
  },
  maya: {
    body: "female",
    hair: "Hair_Buns",
    hairColor: 0x2a1810,
    garments: [
      { region: "torso", lift: 0.022, drape: 10, color: RED, rough: 0.6, name: "bomber" },
      { region: "arms", lift: 0.02, drape: 6, color: WHITE, rough: 0.6, name: "sleeves" },
      { region: "legs", lift: 0.006, color: 0x1b1d24, rough: 0.5, name: "leggings" },
      { region: "feet", lift: 0.014, color: WHITE, rough: 0.55, name: "sneakers" },
    ],
    patch: true,
  },
  r11: {
    body: "male",
    hair: null,
    robot: true,
    garments: [
      { region: "hands", lift: 0.006, color: GRAPHITE, rough: 0.35, metal: 0.6, name: "gloves" },
      { region: "feet", lift: 0.01, color: GRAPHITE, rough: 0.35, metal: 0.6, name: "boots" },
      { region: "neck", lift: 0.006, color: GRAPHITE, rough: 0.35, metal: 0.6, name: "neck" },
      { region: "visor", lift: 0.012, color: 0x0b0f16, rough: 0.08, metal: 0.4, emissive: 0x1ad8ff, emissiveIntensity: 0.25, name: "visor" },
      { region: "chest", lift: 0.008, color: RED, rough: 0.35, metal: 0.2, name: "chestplate" },
      { region: "shoulders", lift: 0.012, color: RED, rough: 0.35, metal: 0.2, name: "shoulders" },
    ],
    patch: true,
  },
  officer: {
    body: "male",
    hair: "Hair_Buzzed",
    garments: [
      { region: "torso+arms", lift: 0.016, drape: 8, color: 0x1f2b4f, rough: 0.8, name: "shirt" },
      { region: "torso", lift: 0.034, drape: 12, color: 0xc9e83a, rough: 0.6, name: "vest", emissive: 0x2a3300, emissiveIntensity: 0.4 },
      { region: "legs", lift: 0.016, drape: 8, color: 0x1b2440, rough: 0.85, name: "trousers" },
      { region: "feet", lift: 0.014, color: 0x121214, rough: 0.4, name: "shoes" },
      { region: "cap", lift: 0.016, color: 0x1b2440, rough: 0.7, name: "cap" },
    ],
    brim: true,
    brimFront: true,
  },
};

/* ── region selection on the base body ──────────────────────────────── */

const TORSO = /^(spine_0[123]|clavicle_[lr]|pelvis)$/;
const ARMS = /^(upperarm|lowerarm)_[lr]$/;
const HANDS = /^(hand_[lr]|(index|middle|ring|pinky|thumb)_\d+.*)$/;
const LEGS = /^(thigh|calf)_[lr]$/;
const FEET = /^(foot|ball)_[lr]$|^ball_leaf/;
const HEAD = /^(Head|neck_01)$/i;

/**
 * Per-vertex facts in the body's bind pose: dominant bone, normalised height
 * (0 = soles, 1 = crown), how far forward the vertex faces, and left/right.
 */
function analyse(mesh) {
  const g = mesh.geometry;
  const pos = g.attributes.position;
  const nrm = g.attributes.normal;
  const si = g.attributes.skinIndex;
  const sw = g.attributes.skinWeight;
  const bones = mesh.skeleton.bones;

  const n = pos.count;
  const get = (attr, i, a) => (a === 0 ? attr.getX(i) : a === 1 ? attr.getY(i) : attr.getZ(i));
  const dominant = new Array(n);
  for (let i = 0; i < n; i += 1) {
    let best = 0;
    let w = -1;
    for (let k = 0; k < 4; k += 1) {
      const wk = sw.getComponent(i, k);
      if (wk > w) {
        w = wk;
        best = si.getComponent(i, k);
      }
    }
    dominant[i] = bones[best] ? bones[best].name : "";
  }
  // axes from the skeleton itself: up = feet -> head, forward = heel -> toes
  const centroid = (re) => {
    const c = [0, 0, 0];
    let k = 0;
    for (let i = 0; i < n; i += 1) {
      if (!re.test(dominant[i])) continue;
      c[0] += pos.getX(i);
      c[1] += pos.getY(i);
      c[2] += pos.getZ(i);
      k += 1;
    }
    return c.map((v) => v / Math.max(1, k));
  };
  const head = centroid(/^Head$/);
  const foot = centroid(/^foot_[lr]$/);
  const toe = centroid(/^ball_[lr]$/);
  const dUp = head.map((v, i) => v - foot[i]);
  const up = [0, 1, 2].reduce((a, b) => (Math.abs(dUp[b]) > Math.abs(dUp[a]) ? b : a), 0);
  const upSign = Math.sign(dUp[up]) || 1;
  const dF = toe.map((v, i) => v - foot[i]);
  const rest = [0, 1, 2].filter((a) => a !== up);
  const fwdAxis = Math.abs(dF[rest[0]]) > Math.abs(dF[rest[1]]) ? rest[0] : rest[1];
  const fwdSign = Math.sign(dF[fwdAxis]) || 1;
  const sideAxis = rest.find((a) => a !== fwdAxis);
  let lo = Infinity;
  let hi = -Infinity;
  for (let i = 0; i < n; i += 1) {
    const v = get(pos, i, up) * upSign;
    if (v < lo) lo = v;
    if (v > hi) hi = v;
  }
  const info = new Array(n);
  for (let i = 0; i < n; i += 1) {
    info[i] = {
      bone: dominant[i],
      h: (get(pos, i, up) * upSign - lo) / (hi - lo),
      f: get(pos, i, fwdAxis) * fwdSign,
      nf: get(nrm, i, fwdAxis) * fwdSign,
      side: get(pos, i, sideAxis),
    };
  }
  let mid = 0;
  let k = 0;
  for (const v of info) {
    if (/spine_0[23]/.test(v.bone)) {
      mid += v.side;
      k += 1;
    }
  }
  return { info, up, fwdAxis, fwdSign, height: hi - lo, mid: mid / Math.max(1, k) };
}

const REGIONS = {
  // tops end at the hip, so the trousers show below the hem
  torso: (v) => (TORSO.test(v.bone) && v.h > (v.bone === "pelvis" ? 0.575 : 0.5)) || (/neck_01/.test(v.bone) && v.h < 0.83),
  arms: (v) => ARMS.test(v.bone),
  "torso+arms": (v) => REGIONS.torso(v) || REGIONS.arms(v),
  hands: (v) => HANDS.test(v.bone),
  legs: (v) => LEGS.test(v.bone) || (TORSO.test(v.bone) && v.h <= 0.6),
  feet: (v) => FEET.test(v.bone) || (/calf/.test(v.bone) && v.h < 0.075),
  neck: (v) => /neck_01/.test(v.bone) || (/Head/.test(v.bone) && v.h < 0.885),
  cap: (v) => /Head/.test(v.bone) && v.h > 0.952,
  visor: (v) => /Head/.test(v.bone) && v.h > 0.9 && v.h < 0.955 && v.nf > 0.35,
  chest: (v) => /spine_03/.test(v.bone) && v.nf > 0.55 && v.h > 0.7,
  shoulders: (v) => /clavicle|upperarm/.test(v.bone) && v.h > 0.78,
};

/**
 * Cloth doesn't follow every muscle: it spans the hollows between them. This
 * relaxes the shell toward its neighbours but only ever outward, so concave
 * dips fill in (fabric) while convex shapes and the clearance stay intact.
 * Vertices split at UV seams are welded by position so seams don't tear.
 */
function drape(pos, nrm, tri, iterations) {
  const n = pos.length / 3;
  const key = new Map();
  const canon = new Int32Array(n);
  for (let i = 0; i < n; i += 1) {
    const k = `${Math.round(pos[i * 3] * 1e4)},${Math.round(pos[i * 3 + 1] * 1e4)},${Math.round(pos[i * 3 + 2] * 1e4)}`;
    if (!key.has(k)) key.set(k, i);
    canon[i] = key.get(k);
  }
  const nb = new Map();
  const link = (a, b) => {
    if (!nb.has(a)) nb.set(a, new Set());
    nb.get(a).add(b);
  };
  for (let t = 0; t < tri.length; t += 3) {
    const a = canon[tri[t]];
    const b = canon[tri[t + 1]];
    const c = canon[tri[t + 2]];
    link(a, b); link(a, c); link(b, a); link(b, c); link(c, a); link(c, b);
  }
  const next = new Float32Array(pos.length);
  for (let it = 0; it < iterations; it += 1) {
    next.set(pos);
    for (const [v, set] of nb) {
      if (set.size < 3) continue; // keep the hem and cuffs where they are
      let x = 0;
      let y = 0;
      let z = 0;
      for (const u of set) {
        x += pos[u * 3];
        y += pos[u * 3 + 1];
        z += pos[u * 3 + 2];
      }
      x = x / set.size - pos[v * 3];
      y = y / set.size - pos[v * 3 + 1];
      z = z / set.size - pos[v * 3 + 2];
      const out = x * nrm.getX(v) + y * nrm.getY(v) + z * nrm.getZ(v);
      if (out <= 0) continue; // only fill hollows
      next[v * 3] = pos[v * 3] + nrm.getX(v) * out * 0.8;
      next[v * 3 + 1] = pos[v * 3 + 1] + nrm.getY(v) * out * 0.8;
      next[v * 3 + 2] = pos[v * 3 + 2] + nrm.getZ(v) * out * 0.8;
    }
    pos.set(next);
  }
  for (let i = 0; i < n; i += 1) {
    const c = canon[i];
    if (c === i) continue;
    pos[i * 3] = pos[c * 3];
    pos[i * 3 + 1] = pos[c * 3 + 1];
    pos[i * 3 + 2] = pos[c * 3 + 2];
  }
}

/** A skinned shell over part of the body, sharing its skeleton. */
function garment(body, facts, spec, knit) {
  const g = body.geometry;
  const keep = facts.info.map(REGIONS[spec.region]);
  const index = g.index ? g.index.array : null;
  const tri = [];
  const count = index ? index.length : g.attributes.position.count;
  for (let t = 0; t < count; t += 3) {
    const a = index ? index[t] : t;
    const b = index ? index[t + 1] : t + 1;
    const c = index ? index[t + 2] : t + 2;
    if (keep[a] && keep[b] && keep[c]) tri.push(a, b, c);
  }
  if (!tri.length) return null;
  const geo = new THREE.BufferGeometry();
  const src = g.attributes.position;
  const nrm = g.attributes.normal;
  const pos = new Float32Array(src.count * 3);
  // lift in world metres: the mesh's own scale may not be 1
  const s = facts.height > 0 ? 1.8 / facts.height : 1;
  const lift = spec.lift / s;
  for (let i = 0; i < src.count; i += 1) {
    pos[i * 3] = src.getX(i) + nrm.getX(i) * lift;
    pos[i * 3 + 1] = src.getY(i) + nrm.getY(i) * lift;
    pos[i * 3 + 2] = src.getZ(i) + nrm.getZ(i) * lift;
  }
  if (spec.drape) drape(pos, nrm, tri, spec.drape);
  geo.setAttribute("position", new THREE.BufferAttribute(pos, 3));
  geo.setAttribute("normal", nrm.clone());
  if (g.attributes.uv) geo.setAttribute("uv", g.attributes.uv.clone());
  geo.setAttribute("skinIndex", g.attributes.skinIndex.clone());
  geo.setAttribute("skinWeight", g.attributes.skinWeight.clone());
  geo.setIndex(tri);
  const mat = new THREE.MeshStandardMaterial({
    color: spec.color,
    roughness: spec.rough ?? 0.8,
    metalness: spec.metal ?? 0,
    bumpMap: spec.metal ? null : knit,
    bumpScale: 0.6,
    emissive: spec.emissive ?? 0x000000,
    emissiveIntensity: spec.emissiveIntensity ?? 1,
  });
  const m = new THREE.SkinnedMesh(geo, mat);
  m.name = spec.name;
  m.bind(body.skeleton, body.bindMatrix);
  m.frustumCulled = false;
  return m;
}

/** Re-point a hair mesh's skin at the body's bones (matched by name). */
function bindHair(hairSrc, body, tint) {
  const hair = hairSrc.getObjectByProperty("type", "SkinnedMesh") || hairSrc.getObjectByProperty("isSkinnedMesh", true);
  if (!hair) return null;
  const byName = new Map(body.skeleton.bones.map((b) => [b.name, b]));
  const bones = hair.skeleton.bones.map((b) => byName.get(b.name) || body.skeleton.bones[0]);
  // the kit ships hair untinted (light grey) for recolouring
  const mat = hair.material.clone();
  mat.color = new THREE.Color(tint);
  const m = new THREE.SkinnedMesh(hair.geometry, mat);
  m.name = "hair";
  m.bind(new THREE.Skeleton(bones, hair.skeleton.boneInverses), hair.bindMatrix);
  m.frustumCulled = false;
  return m;
}

/** Find the body point at a bone, offset backwards, in the root's space. */
function backPoint(root, body, facts, h, out = 0.02) {
  const g = body.geometry;
  const pos = g.attributes.position;
  let best = -1;
  let bestF = Infinity;
  facts.info.forEach((v, i) => {
    if (Math.abs(v.h - h) < 0.02 && Math.abs(v.side - facts.mid) < 0.04 * (facts.height / 1.8) && v.f < bestF) {
      bestF = v.f;
      best = i;
    }
  });
  if (best < 0) return null;
  const p = new THREE.Vector3().fromBufferAttribute(pos, best);
  body.updateMatrixWorld(true);
  p.applyMatrix4(body.bindMatrix);
  // the root faces +z in world after setup; "back" is -z
  p.z -= out;
  return p;
}

/* ── animation set ──────────────────────────────────────────────────── */

const CLIP = {
  run: "Sprint_Loop",
  jog: "Jog_Fwd_Loop",
  jumpStart: "Jump_Start",
  jumpLoop: "Jump_Loop",
  land: "Jump_Land",
  roll: "Roll",
  stumble: "Hit_Chest",
  crash: "Death01",
  caught: "Hit_Knockback",
  idle: "Idle_FoldArms_Loop",
  idle2: "Idle_Loop",
  jet: "NinjaJump_Idle_Loop",
  dance: "Dance_Loop",
};

export class CrewFactory {
  constructor(crewGltf, clips, art) {
    this.src = crewGltf.scene;
    this.clips = new Map(clips.map((c) => [c.name, c]));
    this.art = art; // { knit, roundel, stripes }
    this.cache = new Map();
  }

  /** A ready-to-animate character. The root faces +z; rotate it to taste. */
  make(id) {
    const spec = CREW_SPEC[id] || CREW_SPEC.dante;
    const root = new THREE.Group();
    root.name = id;
    const model = SkeletonUtils.clone(this.src.getObjectByName(spec.body));
    root.add(model);
    const meshes = [];
    model.traverse((o) => {
      if (o.isSkinnedMesh) {
        o.frustumCulled = false;
        meshes.push(o);
      }
    });
    const body = meshes.find((m) => /superhero/i.test(m.name)) || meshes[meshes.length - 1];
    const facts = analyse(body);
    // the base bodies come in underwear: clothes go on top
    const parent = body.parent;
    if (spec.robot) {
      body.material = new THREE.MeshStandardMaterial({
        color: 0xe9ecf0,
        roughness: 0.28,
        metalness: 0.25,
        normalMap: body.material.normalMap,
        normalScale: new THREE.Vector2(0.6, 0.6),
      });
      for (const m of meshes) if (m !== body) m.visible = false; // no brows or eyes behind the visor
    }
    for (const gspec of spec.garments) {
      const m = garment(body, facts, gspec, this.art.knit);
      if (m) parent.add(m);
    }
    if (spec.hair) {
      const h = this.src.getObjectByName(spec.hair);
      if (h) {
        const hm = bindHair(h, body, spec.hairColor || 0x1c1410);
        if (hm) parent.add(hm);
      }
    }
    // attachments ride on the upper spine and the head
    const bone = (n) => body.skeleton.bones.find((b) => b.name === n);
    model.updateMatrixWorld(true);
    const spine = bone("spine_03");
    const head = bone("Head");
    if (spec.bag && spine) {
      const at = backPoint(root, body, facts, 0.69, 0.15);
      if (at) {
        const bag = new THREE.Group();
        const fabric = new THREE.MeshStandardMaterial({ color: RED, roughness: 0.75, bumpMap: this.art.knit, bumpScale: 0.5 });
        const face = new THREE.MeshStandardMaterial({ map: this.art.bagFace, roughness: 0.6 });
        const box = new THREE.Mesh(new THREE.BoxGeometry(0.4, 0.42, 0.22), [fabric, fabric, fabric, fabric, fabric, face]);
        // the BoxGeometry +z face is index 4, -z is 5: the camera sees -z (back)
        box.material = [fabric, fabric, fabric, fabric, fabric, face];
        bag.add(box);
        const strapMat = new THREE.MeshStandardMaterial({ color: 0x1b1d22, roughness: 0.8 });
        for (const sx of [-0.13, 0.13]) {
          const strap = new THREE.Mesh(new THREE.BoxGeometry(0.05, 0.42, 0.03), strapMat);
          strap.position.set(sx, 0.02, 0.13);
          bag.add(strap);
        }
        bag.position.copy(at);
        root.add(bag);
        spine.attach(bag);
      }
    }
    if (spec.patch && spine) {
      const at = backPoint(root, body, facts, 0.74, spec.robot ? 0.03 : 0.035);
      if (at) {
        const patch = new THREE.Mesh(
          new THREE.CircleGeometry(spec.robot ? 0.1 : 0.11, 32),
          new THREE.MeshStandardMaterial({ map: this.art.roundel, roughness: 0.5, transparent: true, polygonOffset: true, polygonOffsetFactor: -2 }),
        );
        patch.position.copy(at);
        patch.rotation.y = Math.PI; // face backwards, toward the camera
        root.add(patch);
        spine.attach(patch);
      }
    }
    if (spec.brim && head) {
      const brimMat = new THREE.MeshStandardMaterial({ color: id === "officer" ? 0x101420 : RED_DEEP, roughness: 0.7 });
      const brim = new THREE.Mesh(new THREE.CylinderGeometry(0.1, 0.1, 0.012, 24, 1, false, 0, Math.PI), brimMat);
      brim.scale.set(1, 1, 0.85);
      // worn backwards (Dante) or forwards (the officer)
      const top = backPoint(root, body, facts, 0.955, 0) || new THREE.Vector3(0, 1.68, -0.08);
      brim.position.set(0, top.y - 0.005, spec.brimFront ? top.z + 0.2 : top.z - 0.0);
      brim.rotation.y = spec.brimFront ? Math.PI / 2 : -Math.PI / 2;
      root.add(brim);
      head.attach(brim);
    }
    // contact shadow
    const shadow = new THREE.Mesh(new THREE.CircleGeometry(0.42, 24), new THREE.MeshBasicMaterial({ map: this.art.blob, transparent: true, depthWrite: false, opacity: 0.55 }));
    shadow.rotation.x = -Math.PI / 2;
    shadow.position.y = 0.02;
    shadow.renderOrder = 1;
    shadow.name = "shadow";
    root.add(shadow);
    return new Runner(root, model, this.clips, shadow);
  }
}

/* ── a character with a small animation state machine ───────────────── */

export class Runner {
  constructor(root, model, clips, shadow) {
    this.root = root;
    this.model = model;
    this.shadow = shadow;
    this.mixer = new THREE.AnimationMixer(model);
    this.actions = {};
    for (const [k, name] of Object.entries(CLIP)) {
      const clip = clips.get(name);
      if (!clip) continue;
      const a = this.mixer.clipAction(clip);
      if (/^(jumpStart|land|roll|stumble|crash|caught)$/.test(k)) {
        a.setLoop(THREE.LoopOnce, 1);
        a.clampWhenFinished = true;
      }
      this.actions[k] = a;
    }
    this.state = "";
    this.oneShotUntil = 0;
    this.t = 0;
  }

  play(state, fade = 0.12, timeScale = 1) {
    const a = this.actions[state];
    if (!a) return;
    a.timeScale = timeScale;
    if (this.state === state) return;
    const prev = this.actions[this.state];
    a.reset().play();
    if (prev) a.crossFadeFrom(prev, fade, false);
    this.state = state;
  }

  /** Choose the clip from the run's state. `ev` carries one-frame events. */
  drive(run, ev, idle, dt) {
    this.t += dt;
    if (idle) {
      this.play("idle", 0.3);
    } else if (run.downed) {
      this.play(run.downed === "caught" ? "caught" : "crash", 0.08);
    } else if (run.jet > 0) {
      this.play("jet", 0.2);
    } else if (ev.stumble) {
      this.play("stumble", 0.06, 1.6);
      this.oneShotUntil = this.t + 0.32;
    } else if (run.slide > 0) {
      const d = this.actions.roll ? this.actions.roll.getClip().duration : 1;
      this.play("roll", 0.06, d / 0.62);
    } else if (ev.air) {
      if (ev.rising) this.play("jumpStart", 0.06, 1.5);
      else this.play("jumpLoop", 0.18);
    } else if (ev.landed) {
      this.play("land", 0.05, 2.2);
      this.oneShotUntil = this.t + 0.16;
    } else if (this.t >= this.oneShotUntil || /jump|roll|crash|caught|idle|jet/.test(this.state)) {
      this.play("run", 0.14, Math.max(0.85, Math.min(1.45, run.speed / 30)));
    }
    this.mixer.update(dt);
  }
}

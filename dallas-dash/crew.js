/**
 * Dallas Dash — the crew: Dante, Maya, R-11 and the parking officer who
 * chases them. Toy-style characters (see toys.js) built on one shared rig,
 * driven by one animation library through a small state machine.
 */

import * as THREE from "three";
import * as SkeletonUtils from "three/addons/utils/SkeletonUtils.js";
import { TOY_SPEC, buildToy, reproportion, scaleClips, toyMaterials } from "./toys.js";

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
    this.clipList = clips;
    this.art = art; // { roundel, blob, … }
    this.scaled = new Map();
  }

  /** The clip library with the pelvis track fitted to a body's leg length. */
  clipsFor(r) {
    const key = r.toFixed(4);
    if (!this.scaled.has(key)) this.scaled.set(key, new Map(scaleClips(this.clipList, r).map((c) => [c.name, c])));
    return this.scaled.get(key);
  }

  /** A ready-to-animate character. The root faces +z; rotate it to taste. */
  make(id) {
    const spec = TOY_SPEC[id] || TOY_SPEC.dante;
    const root = new THREE.Group();
    root.name = id;
    const model = SkeletonUtils.clone(this.src.getObjectByName(spec.body));
    root.add(model);
    const meshes = [];
    model.traverse((o) => {
      if (o.isSkinnedMesh) meshes.push(o);
    });
    const body = meshes.find((m) => /superhero/i.test(m.name)) || meshes[meshes.length - 1];
    // a skeleton of our own: Skeleton.clone() shares the boneInverses array between
    // every copy, so reproportioning one character would re-skin all the others
    const skeleton = new THREE.Skeleton(body.skeleton.bones, body.skeleton.boneInverses.map((m) => m.clone()));
    skeleton.pose();
    // the scanned base bodies only lend their skeleton: the toy parts replace them
    for (const m of meshes) m.visible = false;
    const r = reproportion(skeleton, root, spec.kl, spec.ka, spec.kt);
    const toy = buildToy(id, skeleton);
    const mats = toyMaterials();
    const parent = body.parent;
    parent.updateMatrixWorld(true);
    const toLocal = parent.matrixWorld.clone().invert();
    const skinned = (geo, mat, name) => {
      if (!geo) return null;
      geo.applyMatrix4(toLocal);
      const m = new THREE.SkinnedMesh(geo, mat);
      m.name = name;
      m.frustumCulled = false;
      parent.add(m);
      m.updateMatrixWorld(true);
      m.bind(skeleton, m.matrixWorld);
      return m;
    };
    skinned(toy.toon, mats.toon, "toy");
    skinned(toy.glow, mats.glow, "toy-glow");
    skinned(toy.ink, mats.ink, "toy-ink");
    // the Colonel roundel on the back of the jacket (and on R-11's back plate)
    if (toy.patch && this.art.roundel) {
      const spine = skeleton.bones.find((b) => b.name === "spine_03");
      const patch = new THREE.Mesh(
        new THREE.CircleGeometry(toy.patch.r, 32),
        new THREE.MeshBasicMaterial({ map: this.art.roundel, transparent: true, polygonOffset: true, polygonOffsetFactor: -2 }),
      );
      patch.position.copy(toy.patch.p);
      patch.rotation.y = Math.PI; // faces backwards, toward the game camera
      root.add(patch);
      root.updateMatrixWorld(true);
      spine.attach(patch);
    }
    // contact shadow
    const shadow = new THREE.Mesh(new THREE.CircleGeometry(0.42, 24), new THREE.MeshBasicMaterial({ map: this.art.blob, transparent: true, depthWrite: false, opacity: 0.55 }));
    shadow.rotation.x = -Math.PI / 2;
    shadow.position.y = 0.02;
    shadow.renderOrder = 1;
    shadow.name = "shadow";
    root.add(shadow);
    return new Runner(root, model, this.clipsFor(r), shadow);
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

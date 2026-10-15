/**
 * Dallas Dash — what's on the course: KFC menu pickups, power-ups, and the
 * hazards (street-works kit, scanned concrete barriers, fleet vans, freight
 * containers on flatbeds). Nothing on the course carries a brand.
 *
 * Drumsticks are one InstancedMesh pair; everything else is pooled per kind
 * and re-used, so nothing is allocated while you run.
 */

import * as THREE from "three";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { HAZ, JET_HEIGHT, LANE_X } from "./sim.js";
import { makeCanvas, toTexture } from "./art.js";
import { flatten } from "./meshutil.js";

const std = (o) => new THREE.MeshStandardMaterial(o);

function lathe(profile, segs = 24) {
  return new THREE.LatheGeometry(profile.map(([r, y]) => new THREE.Vector2(r, y)), segs);
}

/** Pool of clones of one prototype. */
class Pool {
  constructor(scene, make) {
    this.scene = scene;
    this.make = make;
    this.items = [];
    this.used = 0;
  }
  begin() {
    this.used = 0;
  }
  take() {
    if (this.used >= this.items.length) {
      const o = this.make();
      this.scene.add(o);
      this.items.push(o);
    }
    const o = this.items[this.used++];
    o.visible = true;
    return o;
  }
  end() {
    for (let i = this.used; i < this.items.length; i += 1) this.items[i].visible = false;
  }
}

/** Horizontal slats for a roll-up shutter. */
function corrugationH() {
  const c = makeCanvas(8, 64);
  const ctx = c.getContext("2d");
  for (let y = 0; y < 64; y += 1) {
    const s = Math.sin((y / 64) * Math.PI * 2 * 6);
    ctx.fillStyle = `rgb(128,${Math.round(128 + s * 100)},255)`;
    ctx.fillRect(0, y, 8, 1);
  }
  return toTexture(c, { srgb: false });
}

/** Normal map for corrugated steel: vertical ribs. */
function corrugation() {
  const c = makeCanvas(64, 8);
  const ctx = c.getContext("2d");
  for (let x = 0; x < 64; x += 1) {
    const s = Math.sin((x / 64) * Math.PI * 2 * 4);
    const nx = Math.round(128 + s * 100);
    ctx.fillStyle = `rgb(${nx},128,255)`;
    ctx.fillRect(x, 0, 1, 8);
  }
  const t = toTexture(c, { repeat: true, srgb: false });
  return t;
}

export class Course {
  constructor(scene, A, art) {
    this.scene = scene;
    this.A = A;
    this.art = art;
    const props = A.props.scene;
    const pick = (name) => {
      const n = props.getObjectByName(name);
      return n ? n.clone(true) : new THREE.Group();
    };
    this.pools = {};
    const pool = (k, make, raw) => (this.pools[k] = new Pool(scene, raw ? make : () => flatten(make())));
    const shadow = (w, d, o = 0.5) => {
      const m = new THREE.Mesh(new THREE.PlaneGeometry(w, d), new THREE.MeshBasicMaterial({ map: art.blob, transparent: true, depthWrite: false, opacity: o }));
      m.rotation.x = -Math.PI / 2;
      m.position.y = 0.025;
      m.renderOrder = 1;
      return m;
    };

    /* ── drumsticks: the currency ─────────────────────────────────── */
    const crust = art.crust.clone();
    crust.repeat.set(2, 1);
    crust.needsUpdate = true;
    const meat = lathe([[0, -0.36], [0.12, -0.33], [0.2, -0.24], [0.24, -0.1], [0.23, 0.04], [0.17, 0.16], [0.09, 0.24], [0.06, 0.3], [0.05, 0.34]], 18);
    const bone = mergeGeometries([
      new THREE.CylinderGeometry(0.035, 0.04, 0.24, 8).translate(0, 0.44, 0),
      new THREE.SphereGeometry(0.055, 8, 6).translate(-0.03, 0.57, 0),
      new THREE.SphereGeometry(0.055, 8, 6).translate(0.03, 0.57, 0),
    ]);
    this.drumMeat = new THREE.InstancedMesh(meat, std({ map: crust, bumpMap: crust, bumpScale: 2.2, roughness: 0.55, color: 0xffffff }), 160);
    this.drumBone = new THREE.InstancedMesh(bone, std({ color: 0xf2ece0, roughness: 0.6 }), 160);
    for (const m of [this.drumMeat, this.drumBone]) {
      m.frustumCulled = false;
      m.count = 0;
      scene.add(m);
    }

    /* ── KFC menu pickups (packaging is branded; that's the product) ─ */
    const stripeMat = std({ map: art.stripes, roughness: 0.55 });
    const logoMat = std({ map: art.stripesLogo, roughness: 0.55 });
    const crustMat = std({ map: crust, bumpMap: crust, bumpScale: 2, roughness: 0.55 });
    const white = std({ color: 0xf6f4ef, roughness: 0.5 });
    const piece = (x, y, z, s) => {
      const m = new THREE.Mesh(new THREE.SphereGeometry(0.16 * s, 12, 10), crustMat);
      m.scale.set(1, 0.8, 1.2);
      m.position.set(x, y, z);
      return m;
    };
    const ITEM = {
      bucket: () => {
        const g = new THREE.Group();
        g.add(new THREE.Mesh(lathe([[0.26, -0.34], [0.36, 0.26], [0.38, 0.27]], 28), logoMat));
        g.add(new THREE.Mesh(new THREE.CircleGeometry(0.26, 24).rotateX(-Math.PI / 2).translate(0, -0.34, 0), white));
        g.add(piece(-0.12, 0.32, 0, 1.2), piece(0.12, 0.34, 0.05, 1.1), piece(0, 0.36, -0.12, 1.15), piece(0.05, 0.45, 0.08, 1));
        return g;
      },
      bowl: () => {
        const g = new THREE.Group();
        g.add(new THREE.Mesh(lathe([[0.2, -0.16], [0.3, 0.02], [0.34, 0.1]], 28), std({ color: 0xffffff, roughness: 0.5, side: THREE.DoubleSide })));
        g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.31, 0.31, 0.03, 24).translate(0, 0.06, 0), std({ color: 0x7a4a22, roughness: 0.35 })));
        for (let i = 0; i < 5; i += 1) g.add(piece(Math.cos(i * 1.3) * 0.14, 0.12, Math.sin(i * 1.3) * 0.14, 0.45));
        g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.345, 0.345, 0.04, 28, 1, true).translate(0, -0.02, 0), std({ color: 0xe4002b, roughness: 0.5 })));
        return g;
      },
      sandwich: () => {
        const g = new THREE.Group();
        const bun = std({ color: 0xd9973f, roughness: 0.55 });
        const top = new THREE.Mesh(new THREE.SphereGeometry(0.3, 20, 12, 0, Math.PI * 2, 0, Math.PI / 2), bun);
        top.scale.set(1, 0.6, 1);
        top.position.y = 0.1;
        const bottom = new THREE.Mesh(new THREE.CylinderGeometry(0.3, 0.28, 0.1, 20), bun);
        bottom.position.y = -0.14;
        const fillet = new THREE.Mesh(new THREE.CylinderGeometry(0.34, 0.33, 0.12, 14), crustMat);
        fillet.position.y = -0.03;
        const lettuce = new THREE.Mesh(new THREE.CylinderGeometry(0.33, 0.33, 0.025, 14), std({ color: 0x78b84e, roughness: 0.6 }));
        lettuce.position.y = 0.05;
        g.add(top, bottom, fillet, lettuce);
        return g;
      },
      popcorn: () => {
        const g = new THREE.Group();
        g.add(new THREE.Mesh(lathe([[0.16, -0.3], [0.24, 0.2]], 4).rotateY(Math.PI / 4), stripeMat));
        for (let i = 0; i < 9; i += 1) g.add(piece(Math.cos(i * 2.1) * 0.12, 0.22 + (i % 3) * 0.05, Math.sin(i * 2.1) * 0.12, 0.42));
        return g;
      },
      fries: () => {
        const g = new THREE.Group();
        g.add(new THREE.Mesh(lathe([[0.14, -0.28], [0.2, 0.12]], 4).rotateY(Math.PI / 4).scale(1, 1, 0.6), std({ color: 0xe4002b, roughness: 0.5 })));
        const fry = std({ color: 0xf2c65e, roughness: 0.6 });
        for (let i = 0; i < 10; i += 1) {
          const f = new THREE.Mesh(new THREE.BoxGeometry(0.035, 0.32, 0.035), fry);
          f.position.set(((i % 5) - 2) * 0.05, 0.16 + (i % 3) * 0.03, (Math.floor(i / 5) - 0.5) * 0.06);
          f.rotation.z = ((i % 5) - 2) * 0.08;
          g.add(f);
        }
        return g;
      },
      biscuit: () => {
        const m = new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.24, 0.2, 18), std({ color: 0xd9a35a, roughness: 0.7, bumpMap: crust, bumpScale: 0.8 }));
        m.scale.set(1, 1, 1);
        const g = new THREE.Group();
        g.add(m);
        return g;
      },
      coleslaw: () => {
        const g = new THREE.Group();
        g.add(new THREE.Mesh(lathe([[0.17, -0.18], [0.22, 0.14]], 22), white));
        g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.235, 0.235, 0.04, 22).translate(0, 0.16, 0), std({ color: 0xcfe8c4, roughness: 0.3, transparent: true, opacity: 0.85 })));
        return g;
      },
      drink: () => {
        const g = new THREE.Group();
        g.add(new THREE.Mesh(lathe([[0.15, -0.34], [0.21, 0.3]], 22), stripeMat));
        g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.22, 0.22, 0.04, 22).translate(0, 0.31, 0), white));
        g.add(new THREE.Mesh(new THREE.CylinderGeometry(0.02, 0.02, 0.36, 6).rotateZ(0.2).translate(0.04, 0.46, 0), std({ color: 0xe4002b })));
        return g;
      },
    };
    for (const [k, make] of Object.entries(ITEM)) {
      pool(`item:${k}`, () => {
        const g = make();
        const ring = new THREE.Mesh(new THREE.RingGeometry(0.5, 0.6, 32), new THREE.MeshBasicMaterial({ color: 0xffd07a, transparent: true, opacity: 0.55, depthWrite: false }));
        ring.rotation.x = -Math.PI / 2;
        ring.name = "ring";
        const w = new THREE.Group();
        w.add(flatten(g), ring);
        return w;
      }, true);
    }

    /* ── power-ups: glowing tokens with an icon ───────────────────── */
    const ICON = { magnet: ["#e4002b", "🧲"], jetpack: ["#ff961e", "🚀"], sneakers: ["#3ca0f0", "👟"], double: ["#e0a100", "2×"] };
    for (const [k, [col, glyph]] of Object.entries(ICON)) {
      const c = makeCanvas(256, 256);
      const ctx = c.getContext("2d");
      const g = ctx.createRadialGradient(128, 128, 30, 128, 128, 128);
      g.addColorStop(0, "rgba(255,255,255,0.9)");
      g.addColorStop(0.7, "rgba(255,255,255,0.35)");
      g.addColorStop(1, "rgba(255,255,255,0)");
      ctx.fillStyle = g;
      ctx.fillRect(0, 0, 256, 256);
      ctx.beginPath();
      ctx.arc(128, 128, 82, 0, Math.PI * 2);
      ctx.fillStyle = col;
      ctx.fill();
      ctx.lineWidth = 10;
      ctx.strokeStyle = "#fff";
      ctx.stroke();
      ctx.fillStyle = "#fff";
      ctx.font = "900 92px Oswald, system-ui, sans-serif";
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.fillText(glyph, 128, 136);
      const tex = toTexture(c);
      pool(`power:${k}`, () => {
        const s = new THREE.Sprite(new THREE.SpriteMaterial({ map: tex, depthWrite: false }));
        s.scale.set(1.3, 1.3, 1);
        return s;
      }, true);
    }

    /* ── hazards ──────────────────────────────────────────────────── */
    const kenney = (name, s, ry = 0) => () => {
      const g = new THREE.Group();
      const m = pick(name);
      m.scale.setScalar(s);
      m.rotation.y = ry;
      g.add(m);
      return g;
    };
    pool("cone", () => {
      const g = kenney("car_cone", 1.5)();
      g.add(shadow(0.9, 0.9, 0.4));
      return g;
    });
    pool("crate", () => {
      const g = new THREE.Group();
      for (let i = 0; i < 3; i += 1) {
        const c = pick("wooden_crate_01");
        c.scale.set(1.15, 0.95, 2.1);
        c.position.y = i * 0.335;
        c.rotation.y = (i % 2) * 0.06;
        g.add(c);
      }
      g.add(shadow(1.3, 1.1, 0.45));
      return g;
    });
    pool("barrier", () => {
      // a scanned concrete Jersey barrier spans the lane
      const g = new THREE.Group();
      const b = pick("concrete_road_barrier_02");
      b.scale.set(1.25, 0.86, 1);
      g.add(b, shadow(2.2, 0.9, 0.5));
      return g;
    });
    const drumGeo = lathe([[0.27, 0], [0.29, 0.06], [0.3, 0.9], [0.27, 0.98], [0.16, 1.0]], 20);
    const drumMat = std({ map: art.drum, roughness: 0.45 });
    const baseGeo = new THREE.CylinderGeometry(0.42, 0.44, 0.08, 16);
    const black = std({ color: 0x1b1c20, roughness: 0.8 });
    pool("bucketstack", () => {
      const g = new THREE.Group();
      for (const x of [-0.36, 0.36]) {
        const d = new THREE.Mesh(drumGeo, drumMat);
        d.position.x = x;
        d.scale.y = 1.05;
        const b = new THREE.Mesh(baseGeo, black);
        b.position.set(x, 0.04, 0);
        g.add(d, b);
      }
      g.add(shadow(1.8, 0.9, 0.45));
      return g;
    });
    const steel = std({ color: 0x8e949c, metalness: 0.8, roughness: 0.35 });
    const chevron = art.chevron.clone();
    chevron.repeat.set(4, 1);
    chevron.needsUpdate = true;
    const beamMat = std({ map: chevron, roughness: 0.5 });
    pool("bar", () => {
      const g = new THREE.Group();
      for (const x of [-0.98, 0.98]) {
        const p = new THREE.Mesh(new THREE.CylinderGeometry(0.06, 0.07, 2.25, 10), steel);
        p.position.set(x, 1.12, 0);
        g.add(p);
      }
      const beam = new THREE.Mesh(new THREE.BoxGeometry(2.05, 0.5, 0.16), beamMat);
      beam.position.y = 1.85;
      g.add(beam);
      return g;
    });
    // street-works scaffold across all three lanes: roll under it
    pool("awning", () => {
      const g = new THREE.Group();
      const tube = new THREE.CylinderGeometry(0.05, 0.05, 2.6, 8);
      for (const x of [-4.8, -3.45, 3.45, 4.8]) {
        for (const z of [0.1, -1.3]) {
          const t = new THREE.Mesh(tube, steel);
          t.position.set(x, 1.3, z);
          g.add(t);
        }
      }
      const rail = new THREE.Mesh(new THREE.CylinderGeometry(0.05, 0.05, 9.8, 8).rotateZ(Math.PI / 2), steel);
      rail.position.set(0, 2.55, 0.1);
      const rail2 = rail.clone();
      rail2.position.z = -1.3;
      const c = makeCanvas(64, 64);
      const ctx = c.getContext("2d");
      ctx.fillStyle = "rgba(40,120,80,0.85)";
      ctx.fillRect(0, 0, 64, 64);
      ctx.strokeStyle = "rgba(20,70,45,0.9)";
      ctx.lineWidth = 2;
      for (let i = 0; i < 64; i += 8) {
        ctx.beginPath();
        ctx.moveTo(i, 0);
        ctx.lineTo(i, 64);
        ctx.moveTo(0, i);
        ctx.lineTo(64, i);
        ctx.stroke();
      }
      const net = toTexture(c, { repeat: true });
      net.repeat.set(14, 1.6);
      const netting = new THREE.Mesh(new THREE.BoxGeometry(9.8, 0.75, 1.5), std({ map: net, transparent: true, roughness: 0.9 }));
      netting.position.set(0, 2.18, -0.6);
      const beam = new THREE.Mesh(new THREE.BoxGeometry(9.8, 0.16, 0.1), beamMat);
      beam.position.set(0, 1.74, 0.15);
      g.add(rail, rail2, netting, beam);
      return g;
    });
    // fleet box vans, modelled to scale: white paint, a coloured band, cab
    // glass, roll-up rear shutter, lights — and no logos. Cab faces -z.
    const VAN_COL = [0x2f5f9c, 0x1f7a5e, 0x6b6e78, 0xd08a1e];
    const paint = std({ color: 0xf2f3f5, roughness: 0.28, metalness: 0.15 });
    const glass = std({ color: 0x1c2430, roughness: 0.08, metalness: 0.7 });
    const tyre = std({ color: 0x18181a, roughness: 0.92 });
    const rim = std({ color: 0xa8adb4, roughness: 0.35, metalness: 0.8 });
    const trim = std({ color: 0x26282c, roughness: 0.6 });
    const tail = std({ color: 0xc81e1e, emissive: 0x7a0808, roughness: 0.3 });
    const head = std({ color: 0xfff6dc, emissive: 0xfff0c0, emissiveIntensity: 0.9, roughness: 0.2 });
    const shutter = std({ color: 0xd6d8dc, roughness: 0.5, metalness: 0.3, normalMap: corrugationH(), normalScale: new THREE.Vector2(0.8, 0.8) });
    let vanN = 0;
    pool("van", () => {
      const g = new THREE.Group();
      const band = std({ color: VAN_COL[vanN++ % VAN_COL.length], roughness: 0.35, metalness: 0.15 });
      const box = (w, h, d, x, y, z, m) => {
        const b = new THREE.Mesh(new THREE.BoxGeometry(w, h, d), m);
        b.position.set(x, y, z);
        g.add(b);
        return b;
      };
      box(2.0, 1.86, 3.0, 0, 1.42, 0.7, paint); // cargo box
      box(2.02, 0.32, 3.02, 0, 1.0, 0.7, band); // fleet band
      box(1.94, 1.25, 1.25, 0, 1.1, -1.42, paint); // cab
      box(1.9, 0.62, 0.55, 0, 0.82, -2.02, paint); // hood
      const ws = box(1.8, 0.66, 0.06, 0, 1.46, -2.02, glass); // windscreen, raked
      ws.rotation.x = -0.42;
      ws.position.z = -1.98;
      for (const sx of [-0.98, 0.98]) box(0.04, 0.5, 0.9, sx, 1.42, -1.42, glass); // side windows
      box(1.98, 0.22, 0.2, 0, 0.52, -2.3, trim); // front bumper
      box(1.4, 0.22, 0.04, 0, 0.82, -2.3, trim); // grille
      for (const sx of [-0.72, 0.72]) box(0.3, 0.14, 0.04, sx, 0.98, -2.3, head);
      box(1.84, 1.62, 0.05, 0, 1.36, 2.21, shutter); // roll-up rear door
      box(2.0, 0.18, 0.24, 0, 0.42, 2.25, trim); // rear bumper
      for (const sx of [-0.88, 0.88]) box(0.14, 0.42, 0.05, sx, 0.9, 2.22, tail);
      const tyreGeo = new THREE.CylinderGeometry(0.4, 0.4, 0.28, 18).rotateZ(Math.PI / 2);
      const rimGeo = new THREE.CylinderGeometry(0.22, 0.22, 0.3, 12).rotateZ(Math.PI / 2);
      for (const sx of [-0.88, 0.88]) {
        for (const zz of [-1.55, 1.45]) {
          const t = new THREE.Mesh(tyreGeo, tyre);
          t.position.set(sx, 0.4, zz);
          const r = new THREE.Mesh(rimGeo, rim);
          r.position.set(sx, 0.4, zz);
          g.add(t, r);
        }
      }
      box(2.0, 0.08, 4.5, 0, 0.47, -0.05, trim); // sill
      g.add(shadow(2.5, 5.0, 0.55));
      return g;
    });
    // freight containers on flatbeds; long ones are built from 4 m bays
    const corr = corrugation();
    const BOX = [0x2c62a8, 0x20805c, 0xd6702a, 0x80868f, 0x288c96, 0x963c40];
    this.boxMats = BOX.map((c) => std({ color: c, roughness: 0.55, metalness: 0.35, normalMap: corr, normalScale: new THREE.Vector2(0.9, 0.9) }));
    const frame = std({ color: 0x2a2c30, roughness: 0.6, metalness: 0.5 });
    const wheel = std({ color: 0x141416, roughness: 0.9 });
    const rampMat = std({ map: chevron, roughness: 0.6, metalness: 0.2 });
    this.frameMat = frame;
    this.wheelMat = wheel;
    this.rampMat = rampMat;
    pool("trailer", () => new THREE.Group(), true);
    pool("dust", () => {
      const s = new THREE.Sprite(new THREE.SpriteMaterial({ color: 0xe6dccb, transparent: true, opacity: 0.6, depthWrite: false }));
      return s;
    }, true);
    this.sparkles = [];
    this.protos = new Map();
  }

  /**
   * A container of a given length and ramp, merged into a few meshes. Built
   * once per (length, ramp) — the sim only makes a dozen of those — and
   * shared; only the paint colour changes per container.
   */
  containerProto(len, ramp) {
    const key = `${len}:${ramp}`;
    if (this.protos.has(key)) return this.protos.get(key);
    const g = new THREE.Group();
    const h = HAZ.trailer.h;
    const bodyLen = len - ramp;
    const bodyGeo = new THREE.BoxGeometry(2.1, h - 0.45, bodyLen);
    // corrugation density stays constant whatever the length
    const uv = bodyGeo.attributes.uv;
    for (let i = 0; i < uv.count; i += 1) uv.setX(i, uv.getX(i) * Math.max(1, bodyLen / 2));
    const body = new THREE.Mesh(bodyGeo, this.boxMats[0]);
    body.position.set(0, 0.45 + (h - 0.45) / 2, -ramp - bodyLen / 2);
    body.userData.keep = true;
    body.name = "body";
    const chassis = new THREE.Mesh(new THREE.BoxGeometry(2.0, 0.22, bodyLen + 0.2), this.frameMat);
    chassis.position.set(0, 0.34, -ramp - bodyLen / 2);
    g.add(body, chassis);
    for (const sx of [-0.98, 0.98]) {
      const post = new THREE.Mesh(new THREE.BoxGeometry(0.12, h - 0.45, 0.12), this.frameMat);
      post.position.set(sx, 0.45 + (h - 0.45) / 2, -ramp + 0.02);
      g.add(post);
      for (const zz of [-ramp - 1.4, -ramp - bodyLen + 1.4, -ramp - bodyLen + 2.6]) {
        const w = new THREE.Mesh(new THREE.CylinderGeometry(0.42, 0.42, 0.3, 14).rotateZ(Math.PI / 2), this.wheelMat);
        w.position.set(sx * 0.92, 0.42, zz);
        g.add(w);
      }
    }
    for (const bx of [-0.55, -0.2, 0.2, 0.55]) {
      const bar = new THREE.Mesh(new THREE.CylinderGeometry(0.025, 0.025, h - 0.7, 6), this.frameMat);
      bar.position.set(bx, 0.45 + (h - 0.45) / 2, -ramp + 0.06);
      g.add(bar);
    }
    if (ramp > 0) {
      const rl = Math.hypot(ramp, h);
      const r = new THREE.Mesh(new THREE.BoxGeometry(2.0, 0.1, rl), this.rampMat);
      r.position.set(0, h / 2, -ramp / 2);
      r.rotation.x = Math.atan2(h, ramp);
      g.add(r);
      for (const sx of [-1.0, 1.0]) {
        const side = new THREE.Mesh(new THREE.BoxGeometry(0.06, 0.4, rl), this.frameMat);
        side.position.set(sx, h / 2 + 0.1, -ramp / 2);
        side.rotation.x = Math.atan2(h, ramp);
        g.add(side);
      }
    }
    const sh = new THREE.Mesh(new THREE.PlaneGeometry(2.6, len + 0.8), new THREE.MeshBasicMaterial({ map: this.art.blob, transparent: true, depthWrite: false, opacity: 0.5 }));
    sh.rotation.x = -Math.PI / 2;
    sh.position.set(0, 0.03, -len / 2);
    g.add(sh);
    const flat = flatten(g);
    this.protos.set(key, flat);
    return flat;
  }

  container(g, o, x, wz) {
    const ramp = o.ramp || 0;
    const key = `${o.len}:${ramp}`;
    if (g.userData.key !== key) {
      g.clear();
      for (const c of this.containerProto(o.len, ramp).children) {
        const m = c.clone();
        if (c.name === "body") m.name = "body";
        g.add(m);
      }
      g.userData.key = key;
    }
    const body = g.getObjectByName("body");
    if (body) body.material = this.boxMats[Math.abs(Math.floor(o.z) * 3 + o.l) % this.boxMats.length];
    g.position.set(x, 0, wz);
  }

  /** Build everything up front, so nothing is created mid-run (no hitches). */
  prewarm() {
    for (const len of [18, 22, 26, 30, 34, 38]) for (const ramp of [0, 6]) this.containerProto(len, ramp);
    const want = { van: 4, cone: 4, crate: 4, barrier: 4, bucketstack: 3, bar: 3, awning: 2, trailer: 4, dust: 0 };
    for (const [k, p] of Object.entries(this.pools)) {
      const n = want[k] ?? 2;
      p.begin();
      for (let i = 0; i < n; i += 1) p.take();
      p.end();
    }
    for (const g of this.pools.trailer.items) this.container(g, { len: 30, ramp: 6, z: 0, l: 0 }, 0, -1000);
  }

  update(run, now) {
    for (const p of Object.values(this.pools)) p.begin();
    let coins = 0;
    const m4 = new THREE.Matrix4();
    const q = new THREE.Quaternion();
    const up = new THREE.Vector3(0, 1, 0);
    const tilt = new THREE.Quaternion().setFromAxisAngle(new THREE.Vector3(0, 0, 1), 0.35);
    const sc = new THREE.Vector3(1.15, 1.15, 1.15);
    const spin = now * 0.003;
    for (const o of run.objects) {
      const rel = o.z - run.distance;
      if (rel < -30 || rel > 220) continue;
      const wz = -o.z;
      if (o.t === 2) continue;
      let x = LANE_X[o.l];
      if (o.t === 0) {
        if (rel < -1) continue;
        let y = o.h + Math.sin(run.tick * 0.08 + o.z) * 0.08;
        if (o.k === "coin" && run.magnet > 0 && rel < 14 && (o.h > JET_HEIGHT) === run.jet > 0) {
          const f = 1 - Math.max(0, rel) / 14;
          x += (run.x - x) * f;
          y += (run.y + 1 - y) * f * 0.8;
        }
        if (o.k === "coin") {
          if (coins >= 160) continue;
          q.setFromAxisAngle(up, spin + o.z).multiply(tilt);
          m4.compose(new THREE.Vector3(x, y, wz), q, sc);
          this.drumMeat.setMatrixAt(coins, m4);
          this.drumBone.setMatrixAt(coins, m4);
          coins += 1;
        } else {
          const p = this.pools[`item:${o.k}`] || this.pools["item:drink"];
          const g = p.take();
          g.position.set(x, y, wz);
          g.children[0].rotation.y = spin + o.z;
          g.children[1].position.y = -y + 0.04;
        }
      } else if (o.t === 3) {
        if (rel < -1) continue;
        const p = this.pools[`power:${o.k}`];
        if (!p) continue;
        const s = p.take();
        const pulse = 1.3 + Math.sin(now * 0.01) * 0.08;
        s.scale.set(pulse, pulse, 1);
        s.position.set(x, o.h + Math.sin(run.tick * 0.1) * 0.12, wz);
      } else if (o.t === 1) {
        if (o.k === "trailer") {
          const g = this.pools.trailer.take();
          this.container(g, o, x, wz);
        } else if (o.k === "van" || o.k === "oncoming") {
          const g = this.pools.van.take();
          // parked vans show their back doors; oncoming ones drive at you
          g.position.set(x, 0, wz - o.len / 2);
          g.rotation.y = o.k === "oncoming" ? Math.PI : 0;
        } else if (o.k === "awning") {
          if (o.l !== 1) continue;
          const g = this.pools.awning.take();
          g.position.set(0, 0, wz);
        } else {
          const p = this.pools[o.k] || this.pools.crate;
          const g = p.take();
          g.position.set(x, 0, wz);
        }
      }
    }
    this.drumMeat.count = coins;
    this.drumBone.count = coins;
    this.drumMeat.instanceMatrix.needsUpdate = true;
    this.drumBone.instanceMatrix.needsUpdate = true;
    for (const p of Object.values(this.pools)) p.end();
  }
}

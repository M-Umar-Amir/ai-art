/**
 * Dallas Dash — the city around the course: textured modular buildings,
 * sidewalks, street furniture, KFC restaurants, billboards and hoardings,
 * overpasses, flags, rails and the 40 landmarks.
 *
 * Performance model (phones first): every repeated thing is an InstancedMesh,
 * one per material, so the whole street costs a few dozen draw calls however
 * many blocks are in view. Buildings are assembled once at load from the kit's
 * 2 m × 3 m facade modules and merged per material; each frame only rewrites
 * instance matrices.
 */

import * as THREE from "three";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";
import { routeById, districtAt } from "./routes.js";
import { flatten } from "./meshutil.js";
import { buildHero } from "./landmarks.js";
import { AD_LINES, paintAd, paintSignAtlas, paintStripes, paintWordmark, paintSkyline, toTexture } from "./art.js";

export const ROAD_HALF = 4.6;
export const FACE_X = 7.6; // building fronts: a 3 m sidewalk beyond the curb
export const SEG = 13; // one block (12 m of facade + a 1 m gap) per side
const LANE_X = [-2.3, 0, 2.3];

function hash(n) {
  let x = Math.imul((n | 0) ^ 0x9e3779b9, 0x85ebca6b);
  x ^= x >>> 13;
  x = Math.imul(x, 0xc2b2ae35);
  x ^= x >>> 16;
  return (x >>> 0) / 4294967296;
}

/** Modulo that stays positive for negative block indices. */
const mod = (a, n) => ((a % n) + n) % n;

function pickMix(mix, r) {
  let total = 0;
  for (const k in mix) total += mix[k];
  let v = r * total;
  for (const k in mix) {
    v -= mix[k];
    if (v <= 0) return k;
  }
  return Object.keys(mix)[0];
}

/* ── geometry helpers ───────────────────────────────────────────────── */

/** A plain float copy with just position/normal/uv, so pieces merge cleanly. */
function plainGeometry(src, matrix) {
  const g = new THREE.BufferGeometry();
  const n = src.attributes.position.count;
  for (const [name, size] of [["position", 3], ["normal", 3], ["uv", 2]]) {
    const a = src.attributes[name];
    const out = new Float32Array(n * size);
    for (let i = 0; i < n; i += 1) {
      if (!a) continue;
      out[i * size] = a.getX(i);
      out[i * size + 1] = a.getY(i);
      if (size === 3) out[i * size + 2] = a.getZ(i);
    }
    g.setAttribute(name, new THREE.BufferAttribute(out, size));
  }
  if (src.index) g.setIndex(Array.from(src.index.array));
  else g.setIndex([...Array(n).keys()]);
  if (matrix) g.applyMatrix4(matrix);
  if (!src.attributes.normal) g.computeVertexNormals();
  return g;
}

/** Mesh pieces of a named node, as [{geometry, material}] in the node's frame. */
function pieces(root, name) {
  const node = root.getObjectByName(name);
  if (!node) return [];
  node.updateMatrixWorld(true);
  const inv = node.matrixWorld.clone().invert();
  const out = [];
  node.traverse((o) => {
    if (!o.isMesh) return;
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    const m = new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld);
    if (mats.length === 1) out.push({ geometry: plainGeometry(o.geometry, m), material: mats[0] });
    else {
      for (const grp of o.geometry.groups) {
        const sub = o.geometry.clone();
        sub.setIndex(Array.from(o.geometry.index.array.slice(grp.start, grp.start + grp.count)));
        out.push({ geometry: plainGeometry(sub, m), material: mats[grp.materialIndex] });
      }
    }
  });
  return out;
}

/* ── templates: assembled once, drawn as instances ──────────────────── */

const TINTABLE = /RedBrick|Trim$|Trim_Green|Trim_MetalConcrete|Concrete|Roof_Slate|Leaves|Foliage/i;

class Template {
  constructor(name, cap = 24) {
    this.name = name;
    this.cap = cap;
    this.byMat = new Map();
    this.meshes = [];
    this.n = 0;
    this.height = 0;
  }

  add(parts, matrix) {
    for (const p of parts) {
      const g = p.geometry.clone().applyMatrix4(matrix);
      const key = p.material.uuid;
      if (!this.byMat.has(key)) this.byMat.set(key, { material: p.material, geos: [] });
      this.byMat.get(key).geos.push(g);
    }
    return this;
  }

  bake(scene) {
    for (const { material, geos } of this.byMat.values()) {
      const geo = mergeGeometries(geos, false);
      if (!geo) continue;
      geo.computeBoundingSphere();
      const m = new THREE.InstancedMesh(geo, material, this.cap);
      m.count = 0;
      m.frustumCulled = false;
      m.userData.tint = TINTABLE.test(material.name || "") || material.userData.tint === true;
      m.name = `${this.name}:${material.name}`;
      scene.add(m);
      this.meshes.push(m);
    }
    this.byMat = null;
    return this;
  }

  begin() {
    this.n = 0;
  }

  put(matrix, tint) {
    if (this.n >= this.cap) return;
    for (const m of this.meshes) {
      m.setMatrixAt(this.n, matrix);
      if (m.userData.tint) m.setColorAt(this.n, tint || WHITE_C);
    }
    this.n += 1;
  }

  end() {
    for (const m of this.meshes) {
      m.count = this.n;
      m.visible = this.n > 0;
      m.instanceMatrix.needsUpdate = true;
      if (m.instanceColor) m.instanceColor.needsUpdate = true;
    }
  }
}

const WHITE_C = new THREE.Color(1, 1, 1);
const _m = new THREE.Matrix4();
const _q = new THREE.Quaternion();
const _v = new THREE.Vector3();
const _s = new THREE.Vector3();
const _e = new THREE.Euler();

function mat(x, y, z, ry = 0, sx = 1, sy = 1, sz = 1) {
  _e.set(0, ry, 0);
  _q.setFromEuler(_e);
  return new THREE.Matrix4().compose(new THREE.Vector3(x, y, z), _q.clone(), new THREE.Vector3(sx, sy, sz));
}

/* ── signs on an atlas, one InstancedMesh, per-instance UV rectangle ─── */

class AtlasQuads {
  constructor(scene, texture, cap = 160, opts = {}) {
    const mat = new THREE.MeshStandardMaterial({ map: texture, roughness: 0.7, transparent: !!opts.transparent, alphaTest: opts.transparent ? 0.05 : 0, side: THREE.DoubleSide });
    mat.onBeforeCompile = (sh) => {
      sh.vertexShader = sh.vertexShader
        .replace("#include <common>", "#include <common>\nattribute vec4 aRect;")
        .replace("#include <uv_vertex>", "#include <uv_vertex>\n#ifdef USE_MAP\n vMapUv = mix(aRect.xy, aRect.zw, uv);\n#endif");
    };
    const geo = new THREE.PlaneGeometry(1, 1);
    this.rect = new THREE.InstancedBufferAttribute(new Float32Array(cap * 4), 4);
    geo.setAttribute("aRect", this.rect);
    this.mesh = new THREE.InstancedMesh(geo, mat, cap);
    this.mesh.frustumCulled = false;
    this.mesh.count = 0;
    this.cap = cap;
    this.n = 0;
    scene.add(this.mesh);
  }

  begin() {
    this.n = 0;
  }

  /** A w×h panel centred at (x,y,z), facing direction yaw (0 = +z). */
  put(r, x, y, z, w, h, yaw = 0, roll = 0) {
    if (!r || this.n >= this.cap) return;
    _e.set(0, yaw, roll, "YXZ");
    _q.setFromEuler(_e);
    _m.compose(_v.set(x, y, z), _q, _s.set(w, h, 1));
    this.mesh.setMatrixAt(this.n, _m);
    this.rect.setXYZW(this.n, r.u0, r.v0, r.u1, r.v1);
    this.n += 1;
  }

  end() {
    this.mesh.count = this.n;
    this.mesh.instanceMatrix.needsUpdate = true;
    this.rect.needsUpdate = true;
  }
}

/* ── the city ───────────────────────────────────────────────────────── */

export class City {
  constructor(scene, A, art, gl) {
    this.scene = scene;
    this.gl = gl;
    this.A = A;
    this.art = art;
    this.route = null;
    const kit = A.city.scene;
    const props = A.props.scene;
    // the kit's materials carry vertex-colour wear masks meant for Unreal
    for (const root of [kit, props]) {
      root.traverse((o) => {
        if (!o.isMesh) return;
        for (const m of Array.isArray(o.material) ? o.material : [o.material]) {
          m.vertexColors = false;
          if (/Glass/i.test(m.name)) {
            m.transparent = true;
            m.opacity = 0.45;
            m.roughness = 0.05;
            m.metalness = 0.6;
            m.depthWrite = false;
          }
          if (/leaves|leaf|foliage/i.test(m.name)) m.userData.tint = true;
        }
      });
    }
    const P = (n) => pieces(kit, n);
    const Q = (n) => pieces(props, n);
    this.P = P;
    this.Q = Q;
    this.templates = {};
    this.buildFacades(P);
    this.buildStreet(P, Q);
    this.buildBrand(P);
    this.atlasKeys = new Set();
    this.landmarks = new Map();
    this.landmarkMats = new Map();
  }

  /** One 12 m-wide building per style, `floors` storeys, 8 m deep. */
  facade(name, spec) {
    const T = this.facadeOne(name, spec, false);
    // far blocks are impostors: the facade rendered once into a texture
    this.impostor(name, T);
  }

  /**
   * Render a template's street face once, orthographically, into a texture;
   * the far version of the building is then that picture on a box: 12
   * triangles instead of ~15,000, and at 60 m+ nobody can tell.
   */
  impostor(name, T) {
    const W = 12.6;
    const H = T.height + 1.2;
    const s = new THREE.Scene();
    s.environment = this.scene.environment;
    s.environmentIntensity = this.scene.environmentIntensity;
    // light it the way the street does: sky from above, the sun at a grazing
    // angle (street facades face across the road, not into the sun)
    s.add(new THREE.HemisphereLight(0xdcecff, 0x8f8070, 1.0));
    const sun = new THREE.DirectionalLight(0xfff0d8, 0.9);
    sun.position.set(-8, 14, 3);
    s.add(sun);
    for (const m of T.meshes) s.add(new THREE.Mesh(m.geometry, m.material));
    const cam = new THREE.OrthographicCamera(-W / 2, W / 2, H, 0, 0.1, 60);
    cam.position.set(0, 0, 30);
    cam.lookAt(0, 0, 0);
    cam.position.set(0, 0, 30);
    const px = 256;
    const rh = Math.round((px * H) / W);
    // render once into an 8-bit target, then copy the pixels into an ordinary
    // texture: mipmaps are generated on upload, which every GPU supports
    // (mipmapping a render target directly fails on some phones)
    const rt = new THREE.WebGLRenderTarget(px, rh);
    const gl = this.gl;
    const prevClear = gl.getClearColor(new THREE.Color());
    const prevAlpha = gl.getClearAlpha();
    gl.setClearColor(0x000000, 0);
    gl.setRenderTarget(rt);
    gl.clear();
    gl.render(s, cam);
    const pixels = new Uint8Array(px * rh * 4);
    gl.readRenderTargetPixels(rt, 0, 0, px, rh, pixels);
    gl.setRenderTarget(null);
    gl.setClearColor(prevClear, prevAlpha);
    rt.dispose();
    // into a canvas (flipping rows: GL reads bottom-up) and upload from there
    const c = document.createElement("canvas");
    c.width = px;
    c.height = rh;
    const ctx = c.getContext("2d");
    const img = ctx.createImageData(px, rh);
    for (let y = 0; y < rh; y += 1) img.data.set(pixels.subarray((rh - 1 - y) * px * 4, (rh - y) * px * 4), y * px * 4);
    ctx.putImageData(img, 0, 0);
    const tex = new THREE.CanvasTexture(c);
    tex.colorSpace = THREE.LinearSRGBColorSpace;
    // facades are seen edge-on down the street: anisotropic filtering keeps
    // the windows legible instead of averaging to a flat wall
    tex.anisotropy = Math.min(8, gl.capabilities.getMaxAnisotropy());
    // a touch darker than the bake: the real modules have recess shadows
    const front = new THREE.MeshBasicMaterial({ map: tex, alphaTest: 0.5, name: "impostor", color: 0xc4c0bc });
    front.userData.tint = true;
    const back = new THREE.MeshStandardMaterial({ color: 0x5f5650, roughness: 0.95, name: "impostorBody" });
    const F = new Template(`${name}_far`, 24);
    F.add([{ geometry: new THREE.PlaneGeometry(W, H).translate(0, H / 2, 0.3), material: front }], new THREE.Matrix4());
    F.add([{ geometry: new THREE.BoxGeometry(12, T.height, 8).translate(0, T.height / 2, -4), material: back }], new THREE.Matrix4());
    F.height = T.height;
    F.bake(this.scene);
    this.templates[`${name}_far`] = F;
  }

  facadeOne(name, { floors, ground, upper, top, cornice, side, roof = true, mansard }) {
    const P = this.P;
    const T = new Template(name, 18);
    const W = 12;
    const D = 8;
    const row = (list, y) => {
      let x = -W / 2;
      for (const [n, w] of list) {
        T.add(P(n), mat(x + w / 2, y, 0));
        x += w;
      }
    };
    row(ground, 0);
    for (let f = 1; f < floors; f += 1) row(f === floors - 1 && top ? top : upper, f * 3);
    let H = floors * 3;
    if (mansard) {
      for (let k = 0; k < 6; k += 1) T.add(P(mansard), mat(-W / 2 + 1 + k * 2, H, 0));
      H += 3.2;
    }
    if (cornice) for (let k = 0; k < 6; k += 1) T.add(P(cornice), mat(-W / 2 + 1 + k * 2, floors * 3, 0));
    // side walls, so the gaps between buildings show depth, not paper
    for (let f = 0; f < floors; f += 1) {
      for (let k = 0; k < D / 2; k += 1) {
        T.add(P(side), mat(-W / 2, f * 3, -1 - k * 2, -Math.PI / 2));
        T.add(P(side), mat(W / 2, f * 3, -1 - k * 2, Math.PI / 2));
      }
    }
    if (roof) for (let i = 0; i < 3; i += 1) for (let j = 0; j < 2; j += 1) T.add(this.P("Roof_4x4"), mat(-4 + i * 4, H + 0.2, -2 - j * 4));
    T.height = H;
    T.bake(this.scene);
    this.templates[name] = T;
    return T;
  }

  buildFacades() {
    const r = (n, k, w = 2) => Array(k).fill([n, w]);
    this.facade("shop2", { floors: 2, ground: [...r("Trim_FirstFloor_Window_001", 5), ["DoorFrame_Trim", 2]], upper: r("Brick_Window_Trim_Single", 6), cornice: "Cornice_Brick_Center", side: "Brick_Plain_3" });
    this.facade("brick4", { floors: 4, ground: [...r("Metal_FirstFloor_Window", 5), ["DoorFrame_Metal_Single", 2]], upper: r("Brick_Window_Trim", 6), cornice: "Cornice_Trim_Center", side: "Brick_Plain_3" });
    this.facade("inset5", { floors: 5, ground: [["Trim_FirstFloor_Window_001", 2], ["Trim_FirstFloor_Window_001", 2], ["DoorFrame_Wooden", 2], ...r("Trim_FirstFloor_Window_001", 3)], upper: r("Brick_Inset_Window_Curved", 3, 4), cornice: "Cornice_Trim_Center", side: "Brick_Plain_3" });
    this.facade("loft6", { floors: 6, ground: r("Brick_BottomTrim", 6), upper: r("Brick_RedWhite_DoubleWindow", 3, 4), top: r("Brick_TopTrim", 6), cornice: "Cornice_Metal_Center", side: "Brick_Plain_3" });
    this.facade("tower", { floors: 12, ground: r("Metal_FirstFloor_Window", 6), upper: r("Metal_FullWindow", 6), cornice: "Cornice_Metal_Center", side: "Metal_Plain_3" });
    this.facade("victorian", { floors: 3, ground: [...r("Trim_FirstFloor_Window_001", 5), ["DoorFrame_Trim", 2]], upper: r("Trim_Window", 6), mansard: "Roof_SlateCornice_Window_1", side: "Trim_Plain_3" });
    this.facade("warehouse", { floors: 2, ground: r("Metal_FirstFloor_Wall", 6), upper: r("Metal_Window", 3, 4), cornice: "Cornice_Metal_Center", side: "Metal_Plain_3" });
    this.STYLE = {
      brick: ["brick4", "inset5", "shop2"],
      loft: ["loft6", "loft6", "brick4"],
      cantina: ["shop2", "shop2", "brick4"],
      glass: ["tower", "tower", "loft6"],
      rowhouse: ["victorian"],
      warehouse: ["warehouse", "warehouse", "shop2"],
      deco: ["inset5", "victorian"],
    };
  }

  buildStreet(P, Q) {
    const T = (name, parts, matrix = new THREE.Matrix4(), cap = 64) => {
      const t = new Template(name, cap).add(parts, matrix).bake(this.scene);
      this.templates[name] = t;
      return t;
    };
    const asphalt = P("Street_Asphalt_6x6");
    for (const p of asphalt) {
      p.material = p.material.clone();
      p.material.color.setRGB(0.42, 0.42, 0.44);
      p.material.roughness = 0.92;
    }
    T("asphalt", asphalt, new THREE.Matrix4(), 140);
    T("sidewalk", P("Sidewalk_Straight_3m"), new THREE.Matrix4(), 180);
    T("lamp", Q("street_lamp_01"), mat(0, 0, 0, 0, 1.2, 1.2, 1.2), 40);
    T("hydrant", Q("fire_hydrant"), mat(0, 0, 0, 0, 1.1, 1.1, 1.1), 24);
    T("bin", Q("metal_trash_can"), mat(0, 0, 0, 0, 0.8, 1, 0.8), 24);
    T("utility", Q("utility_box_02"), new THREE.Matrix4(), 24);
    T("bollard", P("Prop_Bollard"), mat(0, 0, 0, 0, 1.2, 1.2, 1.2), 60);
    T("planter", P("Prop_Planter_Single"), mat(0, 0, 0, 0, 0.7, 1, 0.7), 24);
    for (const [k, src] of [["tree1", "CommonTree_1"], ["tree2", "CommonTree_2"], ["tree3", "CommonTree_3"]]) {
      T(k, Q(src), mat(0, 0, 0, 0, 0.55, 0.55, 0.55), 30);
      // Toronto's maples: the same trees with a greyscale leaf texture, so
      // the per-instance tint turns them real autumn reds and golds
      const parts = Q(src).map((p) => (/Leaves/i.test(p.material.name) ? { geometry: p.geometry, material: autumn(p.material) } : p));
      T(`maple${k.slice(4)}`, parts, mat(0, 0, 0, 0, 0.55, 0.55, 0.55), 30);
    }
    T("ac", P("Prop_ACUnit"), new THREE.Matrix4(), 60);
    // rails set into the street, one pair per lane
    const steel = new THREE.MeshStandardMaterial({ color: 0x9aa0a8, metalness: 0.85, roughness: 0.35, name: "rail" });
    const railGeo = new THREE.BoxGeometry(0.08, 0.04, 6);
    this.templates.rail = new Template("rail", 160).add([{ geometry: railGeo, material: steel }], new THREE.Matrix4()).bake(this.scene);
    // dashed lane lines between the three tracks
    const paint = new THREE.MeshStandardMaterial({ color: 0xe8e4da, roughness: 0.7, name: "lanepaint" });
    this.templates.dash = new Template("dash", 90).add([{ geometry: new THREE.BoxGeometry(0.14, 0.01, 3), material: paint }], new THREE.Matrix4()).bake(this.scene);
    // overhead power: poles and span wires
    const pole = new THREE.MeshStandardMaterial({ color: 0x3c4048, metalness: 0.6, roughness: 0.5, name: "pole" });
    this.templates.pole = new Template("pole", 60).add([{ geometry: new THREE.CylinderGeometry(0.1, 0.13, 6.8, 8).translate(0, 3.4, 0), material: pole }], new THREE.Matrix4()).bake(this.scene);
    this.templates.span = new Template("span", 30).add([{ geometry: new THREE.BoxGeometry(10.8, 0.12, 0.12), material: pole }], new THREE.Matrix4()).bake(this.scene);
    this.templates.wire = new Template("wire", 90).add([{ geometry: new THREE.BoxGeometry(0.025, 0.025, 26), material: pole }], new THREE.Matrix4()).bake(this.scene);
    this.templates.flagpole = new Template("flagpole", 30).add([{ geometry: new THREE.CylinderGeometry(0.05, 0.06, 7.4, 6).translate(0, 3.7, 0), material: new THREE.MeshStandardMaterial({ color: 0xd8d8dc, metalness: 0.7, roughness: 0.3 }) }], new THREE.Matrix4()).bake(this.scene);
    // overpass: concrete deck and piers
    const concrete = new THREE.MeshStandardMaterial({ color: 0xbab4ac, roughness: 0.9, name: "Concrete" });
    const deck = new THREE.BoxGeometry(44, 1.8, 5).translate(0, 13.6, -2.5);
    const lip = new THREE.BoxGeometry(44, 0.5, 0.3).translate(0, 14.7, -0.1);
    const piers = [-7.6, 7.6].map((x) => new THREE.BoxGeometry(1.2, 12.7, 2.2).translate(x, 6.35, -2.5));
    this.templates.overpass = new Template("overpass", 4).add([{ geometry: mergeGeometries([deck, lip, ...piers]), material: concrete }], new THREE.Matrix4()).bake(this.scene);
    // the empty lot: a plywood hoarding and a tower crane
    const ply = new THREE.MeshStandardMaterial({ color: 0xc7a777, roughness: 0.9, name: "ply" });
    this.templates.hoarding = new Template("hoarding", 10).add([{ geometry: new THREE.BoxGeometry(0.12, 3, 12.6).translate(0, 1.5, -6.3), material: ply }], new THREE.Matrix4()).bake(this.scene);
    const yellow = new THREE.MeshStandardMaterial({ color: 0xf2b829, roughness: 0.6, metalness: 0.3 });
    const crane = mergeGeometries([
      new THREE.BoxGeometry(1, 26, 1).translate(0, 13, 0),
      new THREE.BoxGeometry(0.8, 0.8, 24).translate(0, 26.4, 4),
      new THREE.BoxGeometry(1.6, 1.4, 2.6).translate(0, 25.2, -6),
      new THREE.BoxGeometry(1.4, 1.6, 1.4).translate(0, 27.6, 0),
    ]);
    this.templates.crane = new Template("crane", 6).add([{ geometry: crane, material: yellow }], new THREE.Matrix4()).bake(this.scene);
    // billboard frames
    const frame = new THREE.MeshStandardMaterial({ color: 0x4a4e56, metalness: 0.7, roughness: 0.45 });
    const bb = mergeGeometries([
      new THREE.BoxGeometry(9, 4, 0.25).translate(0, 4.2, -0.2),
      new THREE.BoxGeometry(0.25, 2.2, 0.25).translate(-2.8, 1.1, -0.3),
      new THREE.BoxGeometry(0.25, 2.2, 0.25).translate(2.8, 1.1, -0.3),
      new THREE.BoxGeometry(9, 0.12, 0.8).translate(0, 2.15, 0.2),
    ]);
    this.templates.billboard = new Template("billboard", 12).add([{ geometry: bb, material: frame }], new THREE.Matrix4()).bake(this.scene);
  }

  /** The KFC restaurant and its rotating bucket sign. */
  buildBrand(P) {
    const T = new Template("kfc", 4);
    const W = 12;
    let x = -W / 2;
    for (const n of ["Metal_FirstFloor_Window", "Metal_FirstFloor_Window", "DoorFrame_Metal_Single", "Metal_FirstFloor_Window", "Metal_FirstFloor_Window", "Metal_FirstFloor_Window"]) {
      T.add(P(n), mat(x + 1, 0, 0));
      x += 2;
    }
    for (let k = 0; k < 4; k += 1) {
      T.add(P("Metal_Plain_3"), mat(-W / 2, 0, -1 - k * 2, -Math.PI / 2));
      T.add(P("Metal_Plain_3"), mat(W / 2, 0, -1 - k * 2, Math.PI / 2));
    }
    // the red-and-white striped fascia band, the brand's signature
    const stripes = this.art.stripes.clone();
    stripes.repeat.set(3, 1);
    stripes.needsUpdate = true;
    const band = new THREE.MeshStandardMaterial({ map: stripes, roughness: 0.5, name: "kfcBand" });
    T.add([{ geometry: new THREE.BoxGeometry(W + 0.4, 2.2, 8.4).translate(0, 4.1, -4), material: band }], new THREE.Matrix4());
    const roof = new THREE.MeshStandardMaterial({ color: 0xe8e4de, roughness: 0.8 });
    T.add([{ geometry: new THREE.BoxGeometry(W + 0.6, 0.4, 8.6).translate(0, 5.4, -4), material: roof }], new THREE.Matrix4());
    const word = new THREE.MeshStandardMaterial({ map: toTexture(paintWordmark(512, 160)), roughness: 0.4, emissive: 0x330000, name: "kfcWord" });
    T.add([{ geometry: new THREE.PlaneGeometry(4.2, 1.3).translate(0, 4.1, 0.24), material: word }], new THREE.Matrix4());
    // awning over the windows
    const awn = new THREE.MeshStandardMaterial({ color: 0xc8102e, roughness: 0.7 });
    T.add([{ geometry: new THREE.BoxGeometry(W, 0.12, 1.4).rotateX(0.25).translate(0, 2.95, 0.6), material: awn }], new THREE.Matrix4());
    T.height = 5.6;
    T.bake(this.scene);
    this.templates.kfc = T;
    // the bucket on its pole, with the Colonel on the front
    const pole = new THREE.MeshStandardMaterial({ color: 0x6d7078, metalness: 0.7, roughness: 0.4 });
    const pts = [];
    for (let i = 0; i <= 8; i += 1) pts.push(new THREE.Vector2(1.0 + i * 0.04, i * 0.3));
    const bucketGeo = new THREE.LatheGeometry(pts, 32);
    const bucketMat = new THREE.MeshStandardMaterial({ map: this.art.stripesLogo, roughness: 0.5, side: THREE.DoubleSide });
    this.bucketSign = new THREE.Group();
    const p = new THREE.Mesh(new THREE.CylinderGeometry(0.15, 0.2, 7, 10).translate(0, 3.5, 0), pole);
    const b = new THREE.Mesh(bucketGeo, bucketMat);
    b.position.y = 7;
    const lid = new THREE.Mesh(new THREE.CylinderGeometry(1.34, 1.34, 0.08, 32), new THREE.MeshStandardMaterial({ color: 0xe4002b, roughness: 0.5 }));
    lid.position.y = 9.42;
    this.bucketSign.add(p, b, lid);
    this.bucketSpin = b;
    this.bucketSigns = [0, 1].map(() => {
      const g = this.bucketSign.clone();
      g.visible = false;
      this.scene.add(g);
      return g;
    });
  }

  /** Route-dependent bits: the sign atlas, ads, skyline. Called on route change. */
  setRoute(R) {
    if (this.route === R) return;
    this.route = R;
    const keys = [];
    const add = (k) => {
      if (!keys.includes(k)) keys.push(k);
    };
    for (const t of R.murals) add(`mural:${t}`);
    for (const t of R.shops) add(`shop:${t}`);
    for (const t of R.ghosts) add(`ghost:${t}`);
    for (const t of R.ghosts) add(`deco:${t}`);
    for (const [t, sh] of R.signs) add(`highway:${t}:${sh}:${R.flag === "canada" ? "on" : "us"}`);
    add(`flag:${R.flag}`);
    add("clearance");
    add("toronto");
    for (const k of LANDMARK_SIGNS) add(k);
    const { canvas, map } = paintSignAtlas(keys);
    this.atlasMap = map;
    const tex = toTexture(canvas);
    if (!this.signs) {
      this.signs = new AtlasQuads(this.scene, tex, 200);
      this.ghosts = new AtlasQuads(this.scene, tex, 60, { transparent: true });
    } else {
      this.signs.mesh.material.map.dispose();
      this.signs.mesh.material.map = tex;
      this.ghosts.mesh.material.map = tex;
    }
    // KFC posters: one material per line, shared by billboards and hoardings
    if (!this.ads) {
      this.ads = AD_LINES.map((line) => {
        const m = new THREE.MeshStandardMaterial({ map: toTexture(paintAd(this.A.logo, line, 1024, 448)), roughness: 0.55 });
        const t = new Template(`ad:${line}`, 16).add([{ geometry: new THREE.PlaneGeometry(1, 1), material: m }], new THREE.Matrix4()).bake(this.scene);
        return t;
      });
    }
    // drop landmarks built for the previous route; build this route's now
    // (while you're on the home screen), not the first time one comes into view
    for (const g of this.landmarks.values()) this.scene.remove(g);
    this.landmarks.clear();
    for (const lm of R.landmarks) {
      for (const side of [-1, 1]) {
        const g = buildLandmark(this, lm.model, side);
        g.visible = false;
        this.landmarks.set(`${lm.model}:${side}`, g);
        this.scene.add(g);
      }
    }
  }

  sign(key) {
    return this.atlasMap ? this.atlasMap.get(key) : null;
  }

  /** What stands on block i of a side: decided by hash, stable every visit. */
  blockSpec(i, side) {
    const R = this.route;
    const r = hash(i * 2 + (side > 0 ? 1 : 0));
    const r2 = hash(i * 7 + (side > 0 ? 101 : 3));
    const r3 = hash(i * 13 + (side > 0 ? 57 : 29));
    const r4 = hash(i * 19 + (side > 0 ? 7 : 211));
    if ((i > 1 || i < -1) && r4 > 0.93) return { style: "lot", ad: Math.floor(r2 * 97) % AD_LINES.length };
    const style = pickMix(districtAt(i * SEG, R.id).mix, r);
    const options = this.STYLE[style] || this.STYLE.brick;
    const tpl = options[Math.floor(r2 * options.length)];
    const pickOf = (list, v) => list[Math.floor(v * 991) % list.length];
    const spec = { style, tpl, r2, r3 };
    if (style === "brick" || style === "warehouse") if (r3 < 0.55) spec.mural = pickOf(R.murals, r2);
    if (style === "cantina" || style === "rowhouse" || tpl === "shop2") spec.shop = pickOf(R.shops, r3);
    if (style === "loft") spec.ghost = pickOf(R.ghosts, r3);
    if (style === "deco") spec.deco = pickOf(R.ghosts, r3);
    const h = this.templates[tpl].height;
    spec.billboard = h < 16 && tpl !== "victorian" && r4 > 0.72 && r4 < 0.86 ? Math.floor(r3 * 97) % AD_LINES.length : -1;
    spec.ac = h < 16 && r3 > 0.4;
    return spec;
  }

  /**
   * Lay out everything in view for a runner at `d` (world z = -d). `clear`
   * lists course scenery (KFC stores, landmark plazas) that needs the block.
   */
  update(d, objects, R, camX, behind = 14) {
    this.setRoute(R);
    const all = Object.values(this.templates);
    for (const t of all) t.begin();
    for (const a of this.ads) a.begin();
    this.signs.begin();
    this.ghosts.begin();
    for (const g of this.bucketSigns) g.visible = false;
    for (const g of this.landmarks.values()) g.visible = false;
    const T = this.templates;
    const tint = new THREE.Color();
    // ground: asphalt tiles, track beds and rails, sidewalks
    const g0 = Math.floor((d - behind) / 6);
    for (let k = g0; k < g0 + Math.ceil(((globalThis.__viewAhead ?? 185) + 19) / 6) + Math.ceil(behind / 6); k += 1) {
      const z = -(k * 6 + 3);
      T.asphalt.put(mat(-3, 0.15, z), null);
      T.asphalt.put(mat(3, 0.15, z), null);
      for (const dx of [-1.15, 1.15]) T.dash.put(mat(dx, 0.006, z), null);
      for (const lx of LANE_X) {
        T.rail.put(mat(lx - 0.66, 0.02, z), null);
        T.rail.put(mat(lx + 0.66, 0.02, z), null);
      }
    }
    const s0 = Math.floor((d - behind) / 3);
    for (let k = s0; k < s0 + 68 + Math.ceil(behind / 3); k += 1) {
      const z = -(k * 3 + 1.5);
      T.sidewalk.put(mat(-(ROAD_HALF + 1.5), 0.15, z, 0), null);
      T.sidewalk.put(mat(ROAD_HALF + 1.5, 0.15, z, Math.PI), null);
    }
    // course scenery that needs the block cleared
    const clear = [];
    for (const o of objects) {
      if (o.t !== 2) continue;
      const rel = o.z - d;
      if (rel < -40 - behind || rel > 240) continue;
      if (o.k === "store") clear.push({ side: Math.floor(o.z) % 2 === 0 ? 1 : -1, a: o.z - 9, b: o.z + 8, store: o });
      else clear.push({ side: o.l === 0 ? -1 : 1, a: o.z - 16, b: o.z + 16, plaza: true, lm: o });
    }
    let bucket = 0;
    for (const c of clear) {
      if (c.store) {
        const side = c.side;
        const z = -c.store.z;
        T.kfc.put(mat(side * FACE_X, 0.15, z, side < 0 ? Math.PI / 2 : -Math.PI / 2), null);
        if (bucket < this.bucketSigns.length) {
          const g = this.bucketSigns[bucket++];
          g.visible = true;
          g.position.set(side * (ROAD_HALF + 2.6), 0.15, z + 8);
        }
      } else this.placeLandmark(c.lm, R);
    }
    const first = Math.floor((d - behind) / SEG);
    // how far ahead streets are built: the renderer shortens it (with the fog) on low-end phones
    const last = Math.floor((d + (globalThis.__viewAhead ?? 185)) / SEG);
    // blocks before the start line exist too: the home showcase looks back down the street
    for (let i = first; i <= last; i += 1) {
      const z0 = i * SEG;
      const rel = z0 - d;
      const wz = -z0;
      for (const side of [-1, 1]) {
        const blocked = clear.find((c) => c.side === side && z0 + 12 > c.a && z0 < c.b);
        const yaw = side < 0 ? Math.PI / 2 : -Math.PI / 2;
        const zc = wz - 6; // block centre
        const fx = side * FACE_X;
        if (blocked) {
          if (blocked.plaza && mod(i, 2) === 0) this.flag(side, side * (FACE_X - 0.6), wz - 4, R);
        } else {
          const spec = this.blockSpec(i, side);
          if (spec.style === "lot") {
            T.hoarding.put(mat(fx, 0.15, wz - 0.2), null);
            const ad = this.ads[spec.ad];
            const ad2 = this.ads[(spec.ad + 1) % this.ads.length];
            // posters on the hoarding, readable from the road
            ad.put(mat(fx - side * 0.07, 1.6, wz - 3.3, yaw, 5.6, 2.4, 1), null);
            ad2.put(mat(fx - side * 0.07, 1.6, wz - 9.3, yaw, 5.6, 2.4, 1), null);
            T.crane.put(mat(side * (FACE_X + 7), 0, zc, 0), null);
          } else {
            const t = Math.abs(rel + 6) > (globalThis.__lodAt ?? 72) ? T[`${spec.tpl}_far`] : T[spec.tpl];
            const tones = BRICK_TONES[R.id] || BRICK_TONES.downtown;
            tint.setRGB(...tones[Math.floor(spec.r2 * 997) % tones.length]);
            t.put(mat(fx, 0.15, zc, yaw), tint);
            const h = t.height;
            if (spec.mural && rel < 150) this.signs.put(this.sign(`mural:${spec.mural}`), fx - side * 0.3, Math.min(h - 1.5, 7.5), zc + 2.5, 6, 3, yaw);
            if (spec.shop && rel < 120) this.signs.put(this.sign(`shop:${spec.shop}`), fx - side * 0.35, 3.4, zc, 5, 1.2, yaw);
            if (spec.ghost && rel < 150) this.ghosts.put(this.sign(`ghost:${spec.ghost}`), fx - side * 0.25, h - 2, zc, 9, 2, yaw);
            if (spec.deco && rel < 150) this.signs.put(this.sign(`deco:${spec.deco}`), fx - side * 0.35, h - 1.6, zc, 7, 1.4, yaw);
            if (spec.billboard >= 0 && rel < 180) {
              // square to the road, so it reads head-on as you run at it
              const bx = side * (FACE_X + 5.5);
              T.billboard.put(mat(bx, h + 0.15, zc, 0), null);
              this.ads[spec.billboard].put(mat(bx, h + 0.15 + 4.2, zc + 0.01, 0, 8.6, 3.6, 1), null);
            }
            if (spec.ac && rel < 90) T.ac.put(mat(fx - side * 0.1, 4.2, zc + 3, yaw), null);
          }
        }
        if (rel > 95 || rel < -70) continue;
        // sidewalk furniture: by the curb and against the buildings
        const kind = mod(i + (side > 0 ? 1 : 0), 4);
        const cx = side * (ROAD_HALF + 0.75);
        const yawRoad = side < 0 ? Math.PI / 2 : -Math.PI / 2;
        if (kind === 1) {
          const tk = (R.tree === "maple" ? ["maple1", "maple2", "maple3"] : ["tree1", "tree2", "tree3"])[mod(i, 3)];
          tint.copy(R.tree === "maple" ? MAPLE[mod(i, MAPLE.length)] : WHITE_C);
          T[tk].put(mat(cx, 0.15, wz - 6, hash(i) * 6), tint);
          T.planter.put(mat(cx, 0.15, wz - 6), null);
        } else if (kind === 3) T.lamp.put(mat(cx, 0.15, wz - 6, yawRoad), null);
        else if (kind === 2) T.hydrant.put(mat(cx, 0.15, wz - 2, yawRoad), null);
        else if (mod(i, 3) === 0) this.flag(side, side * (FACE_X - 0.6), wz - 3, R);
        if (!blocked) {
          const k2 = mod(i * 3 + (side > 0 ? 2 : 0), 5);
          const bx = side * (FACE_X - 0.6);
          if (k2 === 1) T.bin.put(mat(bx, 0.15, wz - 9, yawRoad), null);
          else if (k2 === 3) T.utility.put(mat(bx, 0.15, wz - 10, yawRoad), null);
          T.bollard.put(mat(cx, 0.15, wz - 10.5), null);
        }
      }
      // overhead power for the trains: poles every other block, wires between
      if (mod(i, 2) === 0 && rel < 150) {
        T.pole.put(mat(-5.3, 0, wz), null);
        T.pole.put(mat(5.3, 0, wz), null);
        T.span.put(mat(0, 6.5, wz), null);
        if (rel < 110) for (const lx of LANE_X) T.wire.put(mat(lx, 6.25, wz - 13), null);
      }
      if (mod(i, 17) === 9 && rel > -6) {
        T.overpass.put(mat(0, 0, wz), null);
        R.signs.forEach(([text, sh], k) => this.signs.put(this.sign(`highway:${text}:${sh}:${R.flag === "canada" ? "on" : "us"}`), k ? 2.65 : -2.65, 11.6, wz + 0.06, 4.3, 2.2, 0));
      }
    }
    for (const t of all) t.end();
    for (const a of this.ads) a.end();
    this.signs.end();
    this.ghosts.end();
    for (const g of this.bucketSigns) if (g.visible) g.children[1].rotation.y += 0.01;
  }

  flag(side, x, z, R) {
    this.templates.flagpole.put(mat(x, 0.15, z), null);
    this.signs.put(this.sign(`flag:${R.flag}`), x - side * 0.0, 6.6, z - 1.25, 2.3, 1.4, side < 0 ? Math.PI / 2 : -Math.PI / 2);
  }

  /* ── landmarks: each model built once per route and side ───────────── */

  placeLandmark(o, R) {
    const side = o.l === 0 ? -1 : 1;
    const model = R.landmarks[o.n % R.landmarks.length].model;
    const key = `${model}:${side}`;
    let g = this.landmarks.get(key);
    if (!g) {
      g = buildLandmark(this, model, side);
      this.landmarks.set(key, g);
      this.scene.add(g);
    }
    g.visible = true;
    g.position.z = -o.z;
  }

  landmarkMaterial(color, glassy) {
    const key = `${color.join(",")}:${glassy ? 1 : 0}`;
    let m = this.landmarkMats.get(key);
    if (!m) {
      m = new THREE.MeshStandardMaterial({
        color: new THREE.Color(color[0] / 255, color[1] / 255, color[2] / 255).convertSRGBToLinear(),
        roughness: glassy ? 0.18 : 0.78,
        metalness: glassy ? 0.55 : 0.05,
        side: THREE.DoubleSide,
      });
      this.landmarkMats.set(key, m);
    }
    return m;
  }
}

const BRICK_TONES = {
  downtown: [[1, 1, 1], [0.92, 0.88, 0.86], [1.05, 1.02, 0.98], [0.86, 0.84, 0.86]],
  east: [[1, 0.94, 0.9], [1.08, 0.9, 0.84], [0.95, 1, 1.05], [1.1, 1.02, 0.86]],
  west: [[1, 1, 1], [0.96, 0.94, 0.9], [1.06, 1, 0.92], [0.88, 0.9, 0.92]],
  toronto: [[1, 1, 1], [1.1, 0.9, 0.9], [0.86, 0.96, 1.08], [1.08, 1.04, 0.84], [0.9, 1.06, 0.9]],
};
const MAPLE = [new THREE.Color(0.95, 0.3, 0.1), new THREE.Color(1.0, 0.62, 0.14), new THREE.Color(0.82, 0.18, 0.08), new THREE.Color(0.95, 0.78, 0.2)];

/** A copy of a leaf material whose texture is greyscale (alpha kept). */
function autumn(m) {
  const c = m.clone();
  c.name = `${m.name}_autumn`;
  const img = m.map && m.map.image;
  if (img && img.width) {
    const cv = document.createElement("canvas");
    cv.width = img.width;
    cv.height = img.height;
    const ctx = cv.getContext("2d");
    ctx.drawImage(img, 0, 0);
    const d = ctx.getImageData(0, 0, cv.width, cv.height);
    for (let i = 0; i < d.data.length; i += 4) {
      const l = Math.min(255, (d.data[i] * 0.3 + d.data[i + 1] * 0.59 + d.data[i + 2] * 0.11) * 1.9);
      d.data[i] = d.data[i + 1] = d.data[i + 2] = l;
    }
    ctx.putImageData(d, 0, 0);
    const t = new THREE.CanvasTexture(cv);
    t.colorSpace = THREE.SRGBColorSpace;
    t.flipY = m.map.flipY;
    t.wrapS = m.map.wrapS;
    t.wrapT = m.map.wrapT;
    c.map = t;
  }
  c.color = new THREE.Color(1, 1, 1);
  return c;
}

export const LANDMARK_SIGNS = [
  "marquee:LAKEWOOD:#1b6fd1", "marquee:GRANADA:#c8102e", "marquee:BISHOP ARTS:#7a3fc4", "marquee:TEXAS:#c8102e", "marquee:KESSLER:#0b6b3a", "ghost:DISTILLERY",
];

/* ── landmark builder: the ten-per-route silhouettes, in real materials ── */

const GOLD = [255, 196, 60];
const GREEN = [98, 200, 120];
const RED = [228, 0, 43];
const WHITE = [255, 255, 255];
const DARK_GLASS = [52, 72, 102];
const ROWHOUSE = [[196, 84, 70], [92, 140, 176], [236, 196, 92], [132, 176, 120], [214, 132, 168], [240, 236, 226]];
const rgbMul = (c, f) => [c[0] * f, c[1] * f, c[2] * f];
const marquee = (text, bg) => `marquee:${text}:${bg}`;
const paintTorontoSign = "toronto";
const paintGhost = (t) => `ghost:${t}`;

function buildLandmark(city, model, side) {
  const group = new THREE.Group();
  const cam = null;
  const glassy = (c) => c === DARK_GLASS || (c[2] > c[0] + 30 && c[2] > 120);
  const add = (geo, color) => {
    const m = new THREE.Mesh(geo, city.landmarkMaterial(color.map((v) => Math.min(255, v)), glassy(color)));
    group.add(m);
    return m;
  };
  const box = (_c, x, y, z, w, h, d, color, yaw) => {
    const m = add(new THREE.BoxGeometry(w, h, d), color);
    m.position.set(x, y, z);
    if (yaw) m.rotation.y = yaw;
  };
  const prism = (_c, x, y, z, r, h, sides, color, stripe, rTop) => {
    const m = add(new THREE.CylinderGeometry(rTop === undefined ? r : rTop, r, h, sides).translate(0, h / 2, 0), color);
    m.position.set(x, y, z);
  };
  const ball = (_c, x, y, z, r, color) => {
    const m = add(new THREE.SphereGeometry(r, 20, 14), color);
    m.position.set(x, y, z);
  };
  const line = (_c, a, b, color, width) => {
    const A = new THREE.Vector3(...a);
    const B = new THREE.Vector3(...b);
    const len = A.distanceTo(B);
    const m = add(new THREE.CylinderGeometry(0.06 * (width || 1), 0.06 * (width || 1), len, 5), color);
    m.position.copy(A).add(B).multiplyScalar(0.5);
    m.quaternion.setFromUnitVectors(new THREE.Vector3(0, 1, 0), B.clone().sub(A).normalize());
  };
  const face = (_c, pts, color) => {
    const g = new THREE.BufferGeometry();
    const v = [];
    for (let i = 1; i < pts.length - 1; i += 1) v.push(...pts[0], ...pts[i], ...pts[i + 1]);
    g.setAttribute("position", new THREE.Float32BufferAttribute(v, 3));
    g.computeVertexNormals();
    add(g, color);
  };
  const quad = (_c, a, b, c, d, color) => face(_c, [a, b, c, d], color);
  const facing = (_c, x0, x1, y0, y1, z, key) => {
    const r = city.sign(key);
    if (!r) return;
    const g = new THREE.PlaneGeometry(Math.abs(x1 - x0), y1 - y0);
    const uv = g.attributes.uv;
    for (let i = 0; i < uv.count; i += 1) uv.setXY(i, r.u0 + uv.getX(i) * (r.u1 - r.u0), r.v0 + uv.getY(i) * (r.v1 - r.v0));
    const m = new THREE.Mesh(g, city.signs.mesh.material.clone());
    m.material.onBeforeCompile = () => {};
    m.position.set((x0 + x1) / 2, (y0 + y1) / 2, z + 0.02);
    group.add(m);
  };
  const tree = (_c, x, z, r, color) => {
    prism(_c, x, 0, z, 0.35, 2.2, 6, [110, 84, 60]);
    ball(_c, x, 2.2 + r, z, r, color);
  };
  // the ten illustrated landmarks have full models (landmarks.js)
  const hero = buildHero(
    model,
    side,
    (geo, color, isGlass) => group.add(new THREE.Mesh(geo, city.landmarkMaterial(color.map((v) => Math.min(255, v)), Boolean(isGlass)))),
    (geo, material) => group.add(new THREE.Mesh(geo, material)),
  );
  if (hero) return flatten(group);
  const z = 0;
  const landmark0 = (() => {
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
  })();
  void landmark0;
  void cam;
  void rgbMul;
  void GOLD;
  void GREEN;
  void RED;
  void WHITE;
  void ROWHOUSE;
  void marquee;
  void paintTorontoSign;
  void paintGhost;
  void tree;
  void quad;
  void facing;
  void line;
  void ball;
  void box;
  void prism;
  void face;
  void z;
  void side;
  void model;
  // one mesh per material: a landmark costs a handful of draw calls, not dozens
  const flat = flatten(group);
  return flat;
}

/* ── sky dome and the distant skyline band ─────────────────────────── */

export function makeSky(scene) {
  const geo = new THREE.SphereGeometry(400, 24, 12);
  const mat = new THREE.ShaderMaterial({
    side: THREE.BackSide,
    depthWrite: false,
    fog: false,
    uniforms: { top: { value: new THREE.Color() }, mid: { value: new THREE.Color() }, low: { value: new THREE.Color() }, haze: { value: new THREE.Color() }, sun: { value: new THREE.Vector3(0.3, 0.12, -1).normalize() } },
    vertexShader: "varying vec3 vDir; void main(){ vDir = normalize(position); gl_Position = projectionMatrix * modelViewMatrix * vec4(position,1.0); }",
    fragmentShader: `uniform vec3 top; uniform vec3 mid; uniform vec3 low; uniform vec3 haze; uniform vec3 sun; varying vec3 vDir;
      void main(){ float h = clamp(vDir.y, -0.1, 1.0);
        vec3 c = h > 0.25 ? mix(mid, top, smoothstep(0.25, 0.9, h)) : (h > 0.03 ? mix(low, mid, smoothstep(0.03, 0.25, h)) : mix(haze, low, smoothstep(-0.05, 0.03, h)));
        float s = max(dot(normalize(vDir), sun), 0.0);
        c += vec3(1.0, 0.86, 0.62) * (pow(s, 600.0) * 3.0 + pow(s, 12.0) * 0.35);
        gl_FragColor = vec4(c, 1.0);
        #include <colorspace_fragment>
      }`,
  });
  const sky = new THREE.Mesh(geo, mat);
  sky.renderOrder = -10;
  scene.add(sky);
  // skyline: an open cylinder band near the horizon, behind everything
  const bandGeo = new THREE.CylinderGeometry(300, 300, 70, 48, 1, true, Math.PI * 0.6, Math.PI * 0.8);
  const bandMat = new THREE.MeshBasicMaterial({ transparent: true, side: THREE.BackSide, depthWrite: false, fog: false });
  const band = new THREE.Mesh(bandGeo, bandMat);
  band.position.y = 22;
  band.renderOrder = -9;
  scene.add(band);
  return {
    sky,
    band,
    set(R) {
      const c = (a) => new THREE.Color(a[0] / 255, a[1] / 255, a[2] / 255);
      mat.uniforms.top.value.copy(c(R.sky.top));
      mat.uniforms.mid.value.copy(c(R.sky.mid));
      mat.uniforms.low.value.copy(c(R.sky.low));
      mat.uniforms.haze.value.copy(c(R.sky.haze));
      if (bandMat.map) bandMat.map.dispose();
      const t = toTexture(paintSkyline(R.skyline));
      t.wrapS = THREE.RepeatWrapping;
      t.repeat.x = 2;
      bandMat.map = t;
      bandMat.needsUpdate = true;
    },
    follow(x, z) {
      sky.position.set(x, 0, z);
      band.position.set(x, 22, z);
    },
  };
}

export { routeById };

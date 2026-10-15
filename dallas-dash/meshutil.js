/**
 * Dallas Dash — small geometry helpers shared by the city and the course.
 */

import * as THREE from "three";
import { mergeGeometries } from "three/addons/utils/BufferGeometryUtils.js";

/** Position/normal/uv only, as floats, so pieces from any source merge. */
export function plain(src, matrix) {
  const g = new THREE.BufferGeometry();
  const n = src.attributes.position.count;
  for (const [name, size] of [["position", 3], ["normal", 3], ["uv", 2]]) {
    const a = src.attributes[name];
    const out = new Float32Array(n * size);
    if (a) {
      for (let i = 0; i < n; i += 1) {
        out[i * size] = a.getX(i);
        out[i * size + 1] = a.getY(i);
        if (size === 3) out[i * size + 2] = a.getZ(i);
      }
    }
    g.setAttribute(name, new THREE.BufferAttribute(out, size));
  }
  g.setIndex(src.index ? Array.from(src.index.array) : [...Array(n).keys()]);
  if (matrix) g.applyMatrix4(matrix);
  if (!src.attributes.normal) g.computeVertexNormals();
  return g;
}

const BAKED = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.55, metalness: 0.1, name: "baked" });
const BAKED_METAL = new THREE.MeshStandardMaterial({ vertexColors: true, roughness: 0.35, metalness: 0.75, name: "bakedMetal" });

/**
 * Merge a group's meshes into one mesh per material (sprites and anything
 * flagged userData.keep stay as they are). Five draw calls become one or two.
 */
export function flatten(group, { bakeColors = true } = {}) {
  group.updateMatrixWorld(true);
  const inv = group.matrixWorld.clone().invert();
  const by = new Map();
  const keep = [];
  group.traverse((o) => {
    if (o === group) return;
    if (o.isSprite || o.userData.keep) {
      // kept as its own mesh (e.g. a part whose material changes later)
      const k = o.clone();
      k.matrixAutoUpdate = true;
      new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld).decompose(k.position, k.quaternion, k.scale);
      keep.push(k);
      return;
    }
    if (!o.isMesh) return;
    const m = new THREE.Matrix4().multiplyMatrices(inv, o.matrixWorld);
    const mats = Array.isArray(o.material) ? o.material : [o.material];
    const groups = mats.length > 1 ? o.geometry.groups : [{ start: 0, count: o.geometry.index ? o.geometry.index.count : o.geometry.attributes.position.count, materialIndex: 0 }];
    for (const gr of groups) {
      const mat = mats[gr.materialIndex];
      let geo = o.geometry;
      if (mats.length > 1) {
        geo = o.geometry.clone();
        geo.setIndex(Array.from(o.geometry.index.array.slice(gr.start, gr.start + gr.count)));
      }
      // plain painted parts (no texture, no glow, opaque) share one
      // vertex-coloured material: a whole van body becomes a single draw
      const bakeable = bakeColors && mat.isMeshStandardMaterial && !mat.map && !mat.normalMap && !mat.transparent && mat.emissive.getHex() === 0;
      const key = bakeable ? (mat.metalness > 0.5 ? "baked:metal" : "baked") : mat;
      const g = plain(geo, m);
      if (bakeable) {
        const n = g.attributes.position.count;
        const col = new Float32Array(n * 3);
        for (let i = 0; i < n; i += 1) {
          col[i * 3] = mat.color.r;
          col[i * 3 + 1] = mat.color.g;
          col[i * 3 + 2] = mat.color.b;
        }
        g.setAttribute("color", new THREE.BufferAttribute(col, 3));
      }
      if (!by.has(key)) by.set(key, []);
      by.get(key).push(g);
    }
  });
  const out = new THREE.Group();
  for (const [key, geos] of by) {
    const mat = key === "baked" ? BAKED : key === "baked:metal" ? BAKED_METAL : key;
    const mesh = new THREE.Mesh(mergeGeometries(geos), mat);
    mesh.renderOrder = mat.transparent ? 1 : 0;
    out.add(mesh);
  }
  for (const k of keep) out.add(k);
  return out;
}

/**
 * Dallas Dash — asset loading. Four compressed GLBs (city, props, crew,
 * animations) plus the KFC logo, loaded in parallel with progress, then the
 * shared textures painted once.
 */

import * as THREE from "three";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import { MeshoptDecoder } from "three/addons/libs/meshopt_decoder.module.js";
import { makeCanvas, toTexture, paintKnit, paintRoundel, paintStripes, paintCrust, paintHazard } from "./art.js";

const BASE = "/dallas-dash/assets/";
/** Where an asset lives; the mobile build inlines them all into DD_ASSETS. */
export const assetUrl = (path) => (globalThis.DD_ASSETS && globalThis.DD_ASSETS[path]) || BASE + path;

function loadImage(url) {
  return new Promise((ok) => {
    const img = new Image();
    img.onload = () => ok(img);
    img.onerror = () => ok(null);
    img.src = url;
  });
}

/** Load everything; `onProgress(0..1)` as bytes arrive. */
export async function loadAssets(onProgress) {
  const loader = new GLTFLoader().setMeshoptDecoder(MeshoptDecoder);
  const files = ["city", "props", "crew", "anims"];
  const got = Object.fromEntries(files.map((f) => [f, 0]));
  const total = { city: 4.1e6, props: 2.9e6, crew: 2.3e6, anims: 0.55e6 };
  const report = () => {
    if (!onProgress) return;
    let a = 0;
    let b = 0;
    for (const f of files) {
      a += Math.min(got[f], total[f]);
      b += total[f];
    }
    onProgress(a / b);
  };
  const load = (f) =>
    new Promise((ok, no) =>
      loader.load(
        assetUrl(`models/${f}.glb`),
        (g) => {
          // GLTFLoader de-duplicates node names file-wide; the real ones are in extras
          const renamed = new Map();
          g.scene.traverse((o) => {
            if (o.userData && o.userData.name && o.userData.name !== o.name) {
              renamed.set(o.name, o.userData.name);
              o.name = o.userData.name;
            }
          });
          // clips bound to a renamed skeleton carry the renamed bone in each track
          for (const clip of g.animations) {
            for (const t of clip.tracks) {
              const dot = t.name.indexOf(".");
              const node = t.name.slice(0, dot);
              if (renamed.has(node)) t.name = renamed.get(node) + t.name.slice(dot);
            }
          }
          got[f] = total[f];
          report();
          ok(g);
        },
        (e) => {
          if (e.total) total[f] = e.total;
          got[f] = e.loaded;
          report();
        },
        no,
      ),
    );
  const [city, props, crew, anims, logo] = await Promise.all([load("city"), load("props"), load("crew"), load("anims"), loadImage(assetUrl("kfc-logo.svg"))]);
  return { city, props, crew, clips: anims.animations, logo };
}

/** Textures shared by the crew, the course and the city. */
export function makeArt(A) {
  const knit = toTexture(paintKnit(), { repeat: true, srgb: false });
  knit.repeat.set(24, 24);
  const roundel = toTexture(paintRoundel(A.logo, 256));
  // the delivery bag's back panel: white with the roundel
  const bf = makeCanvas(256, 256);
  const bctx = bf.getContext("2d");
  bctx.fillStyle = "#f3f1ec";
  bctx.fillRect(0, 0, 256, 256);
  bctx.fillStyle = "#e4002b";
  bctx.fillRect(0, 0, 256, 34);
  bctx.fillRect(0, 222, 256, 34);
  bctx.save();
  bctx.beginPath();
  bctx.arc(128, 128, 82, 0, Math.PI * 2);
  bctx.clip();
  if (A.logo) bctx.drawImage(A.logo, 46, 46, 164, 164);
  bctx.restore();
  const bagFace = toTexture(bf);
  // soft contact shadow
  const sc = makeCanvas(128, 128);
  const sctx = sc.getContext("2d");
  const g = sctx.createRadialGradient(64, 64, 4, 64, 64, 62);
  g.addColorStop(0, "rgba(0,0,0,0.75)");
  g.addColorStop(0.6, "rgba(0,0,0,0.3)");
  g.addColorStop(1, "rgba(0,0,0,0)");
  sctx.fillStyle = g;
  sctx.fillRect(0, 0, 128, 128);
  const blob = toTexture(sc, { srgb: false });
  const stripes = toTexture(paintStripes(8, 512, 256), { repeat: true });
  const stripesLogo = toTexture(paintStripes(8, 512, 256, A.logo));
  const crust = toTexture(paintCrust(), { repeat: true });
  const drum = toTexture(paintHazard("drum"));
  const chevron = toTexture(paintHazard("chevron"), { repeat: true });
  return { knit, roundel, bagFace, blob, stripes, stripesLogo, crust, drum, chevron };
}

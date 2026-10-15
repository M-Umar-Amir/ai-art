/**
 * Dallas Dash — offline asset pipeline (not deployed).
 *
 * Packs the CC0 source kits into a handful of small, mobile-ready GLBs in
 * ../assets/models: WebP textures at capped sizes, decimated scans, meshopt
 * compression. Every source model becomes one top-level node named after its
 * file, so the game can look pieces up by name.
 *
 *   ASSET_SRC=<folder with the unzipped kits> node build-assets.mjs
 *
 * Sources (all CC0 1.0): Quaternius Downtown City MegaKit, Universal Base
 * Characters, Universal Animation Library 1 & 2, Stylized Nature MegaKit
 * (quaternius.itch.io); Kenney Car Kit and City Kit Roads (kenney.nl); Poly
 * Haven models (polyhaven.com). See ../assets/CREDITS.md.
 */

import fs from "node:fs";
import path from "node:path";
import { Document, NodeIO } from "@gltf-transform/core";
import { ALL_EXTENSIONS, EXTTextureWebP } from "@gltf-transform/extensions";
import { dedup, prune, meshopt, resample, simplify, weld, mergeDocuments } from "@gltf-transform/functions";
import { MeshoptEncoder, MeshoptSimplifier } from "meshoptimizer";
import sharp from "sharp";

const SRC = process.env.ASSET_SRC;
if (!SRC) throw new Error("set ASSET_SRC to the folder holding the unzipped kits");
const OUT = path.resolve(import.meta.dirname, "../assets/models");
fs.mkdirSync(OUT, { recursive: true });

/** Resize every texture to fit `size` and re-encode it as WebP. */
function webpTextures(size) {
  return async (doc) => {
    for (const t of doc.getRoot().listTextures()) {
      const src = t.getImage();
      if (!src) continue;
      // decode to plain 8-bit pixels first: some kit PNGs are 16-bit greyscale,
      // which libvips cannot resize straight into WebP
      const { data, info } = await sharp(Buffer.from(src)).raw({ depth: "uchar" }).toBuffer({ resolveWithObject: true });
      const { width, height } = info;
      const img = sharp(data, { raw: { width, height, channels: info.channels } });
      const k = Math.min(1, size / Math.max(width, height));
      const out = await img
        .resize(Math.max(4, Math.round(width * k)), Math.max(4, Math.round(height * k)))
        .webp({ quality: 82, effort: 4 })
        .toBuffer();
      t.setImage(new Uint8Array(out)).setMimeType("image/webp").setURI(t.getURI().replace(/\.(png|jpe?g)$/i, ".webp"));
    }
    doc.createExtension(EXTTextureWebP).setRequired(true);
  };
}

await MeshoptEncoder.ready;
await MeshoptSimplifier.ready;
const io = new NodeIO().registerExtensions(ALL_EXTENSIONS).registerDependencies({ "meshopt.encoder": MeshoptEncoder });

const KIT = path.join(SRC, "Downtown_City_MegaKit_Standard_/Exports/glTF (Godot)");
const UBC = path.join(SRC, "Universal_Base_Characters_Standard_/Universal Base Characters[Standard]");
const CARS = path.join(SRC, "kenney_car-kit/Models/GLB format");
const ROADS = path.join(SRC, "kenney_city-kit-roads/Models/GLB format");
const PH = path.join(SRC, "polyhaven");
const NATURE = findDir(path.join(SRC, "Stylized_Nature_MegaKit_Standard_"), "CommonTree_1.gltf");

function findDir(root, file) {
  for (const e of fs.readdirSync(root, { withFileTypes: true })) {
    const p = path.join(root, e.name);
    if (e.isFile() && e.name === file) return root;
    if (e.isDirectory()) {
      const r = findDir(p, file);
      if (r) return r;
    }
  }
  return null;
}

/**
 * Merge source files into one document. Each source scene's roots are
 * wrapped in a node named `name`. `prep(doc)` can clean a source first.
 */
async function pack(outName, items, { tex = 1024, prep, quantize = true } = {}) {
  const target = new Document();
  const buffer = target.createBuffer();
  const scene = target.createScene("pack");
  for (const { file, name, simplifyRatio, error = 0.02 } of items) {
    const doc = await io.read(file);
    if (simplifyRatio) {
      // error is relative to the mesh size; ~2% keeps the silhouette at street distance
      await doc.transform(weld(), simplify({ simplifier: MeshoptSimplifier, ratio: simplifyRatio, error }));
    }
    if (prep) await prep(doc, name);
    const map = mergeDocuments(target, doc);
    const group = target.createNode(name);
    scene.addChild(group);
    for (const s of doc.getRoot().listScenes()) {
      const merged = map.get(s);
      for (const child of merged.listChildren()) group.addChild(child);
      merged.dispose();
    }
  }
  // GLTFLoader renames duplicate node names file-wide (pelvis -> pelvis_1), which
  // breaks clip binding on the second skeleton; keep the real name in extras
  for (const n of target.getRoot().listNodes()) n.setExtras({ ...n.getExtras(), name: n.getName() });
  for (const b of target.getRoot().listBuffers()) if (b !== buffer) b.dispose();
  for (const a of target.getRoot().listAccessors()) a.setBuffer(buffer);
  await target.transform(
    dedup(),
    prune({ keepLeaves: true }),
    webpTextures(tex),
    ...(quantize ? [meshopt({ encoder: MeshoptEncoder, level: "medium" })] : []),
  );
  const out = path.join(OUT, outName);
  await io.write(out, target);
  console.log(outName.padEnd(16), (fs.statSync(out).size / 1048576).toFixed(2), "MB,", items.length, "pieces");
}

/* ── city modules: facades, cornices, roofs, sidewalks, street props ── */
const MODULES = [
  "Brick_Plain_3", "Brick_Window_Trim", "Brick_Window_Trim_Single", "Brick_Window_Square_Single", "Brick_Window_CurvedDouble",
  "Brick_RedWhite_DoubleWindow", "Brick_Inset_Window", "Brick_Inset_Window_Curved", "Brick_BottomTrim", "Brick_TopTrim", "Brick_HalfTrim",
  "Brick_Ornament_Horizontal", "Brick_CornerColumn_Bottom", "Brick_CornerColumn_Center", "Brick_CornerColumn_Top", "Brick_CornerColumn_Cap",
  "Brick_Corner_Plain",
  "Cornice_Brick_Center", "Cornice_Metal_Center", "Cornice_Trim_Center",
  "Metal_FirstFloor_Wall", "Metal_FirstFloor_Window", "Metal_FullWindow", "Metal_Window", "Metal_Window_Half", "Metal_Plain_3",
  "Trim_FirstFloor_Wall", "Trim_FirstFloor_Window_001", "Trim_FirstFloor_Window_Columns", "Trim_Plain_3", "Trim_Window", "Trim_Wall_Guard",
  "Trim_Column_Bottom", "Trim_Column_Center", "Trim_Column_Top",
  "Door_1", "Door_2", "Door_3", "DoorFrame_Metal_Single", "DoorFrame_Trim", "DoorFrame_Wooden",
  "Roof_Slate_Center", "Roof_Slate_Window_1", "Roof_SlateCornice_Center", "Roof_SlateCornice_Window_1", "Roof_4x4",
  "Sidewalk_Straight_3m", "Sidewalk_Planter", "Street_4Lane_noSidewalk", "Street_Asphalt_6x6",
  "Prop_ACUnit", "Prop_Bollard", "Prop_ManholeCover", "Prop_Planter_Single", "Prop_Drain",
  "Building_Small_1", "Building_Medium_2_001", "Building_Large_2",
];
await pack("city.glb", MODULES.filter((n) => !/^Building_/.test(n)).map((n) => ({ file: path.join(KIT, `${n}.gltf`), name: n })), { tex: 1024 });

/* ── props: scanned street furniture, vehicles, road works, trees ── */
const ph = (n, r) => ({ file: path.join(PH, n, `${n}_1k.gltf`), name: n, simplifyRatio: r, error: 0.03 });
const PROPS = [
  ph("street_lamp_01", 0.05), { ...ph("fire_hydrant", 0.012), error: 0.08 }, ph("concrete_road_barrier_02", 0.08), ph("Barrel_01", 0.5),
  ph("wooden_crate_01", 0.2), { ...ph("metal_trash_can", 0.05), error: 0.08 }, ph("utility_box_02", 0.2), { ...ph("modular_street_seating", 0.04), error: 0.08 }, ph("planter_box_01", 0.2),
  ...["van", "delivery", "taxi", "police", "sedan", "suv", "truck", "truck-flat", "garbage-truck", "cone"].map((n) => ({ file: path.join(CARS, `${n}.glb`), name: `car_${n}` })),
  ...["construction-barrier", "construction-cone", "construction-light", "construction-fence", "traffic-light", "sign-highway-wide"].map((n) => ({ file: path.join(ROADS, `${n}.glb`), name: `road_${n}` })),
  ...["CommonTree_1", "CommonTree_2", "CommonTree_3", "CommonTree_4", "Bush_Common"].map((n) => ({ file: path.join(NATURE, `${n}.gltf`), name: n, simplifyRatio: 0.45, error: 0.02 })),
];
await pack("props.glb", PROPS, { tex: 512 });

/* ── the crew: two base bodies with faces, hair and brows rigged to the head ── */
const BODY = path.join(UBC, "Base Characters/Godot - UE");
const HAIR = path.join(UBC, "Hairstyles/Rigged to Head Bone/glTF (Godot -Unreal)");
await pack(
  "crew.glb",
  [
    { file: path.join(BODY, "Superhero_Male_FullBody.gltf"), name: "male" },
    { file: path.join(BODY, "Superhero_Female_FullBody.gltf"), name: "female" },
    ...["Hair_Buzzed", "Hair_SimpleParted", "Hair_Buns", "Hair_Long", "Hair_Beard"].map((n) => ({ file: path.join(HAIR, `${n}.gltf`), name: n })),
  ],
  // float positions: the game derives clothing shells from the body mesh
  { tex: 1024, quantize: false },
);

/* ── animations: only the clips the runner uses, no meshes ── */
const KEEP = new Set([
  "Sprint_Loop", "Jog_Fwd_Loop", "Jump_Start", "Jump_Loop", "Jump_Land", "Roll", "Slide_Start", "Slide_Loop", "Slide_Exit",
  "Hit_Chest", "Hit_Knockback", "Death01", "Idle_Loop", "Dance_Loop", "Walk_Loop", "Idle_FoldArms_Loop", "NinjaJump_Start", "NinjaJump_Idle_Loop", "NinjaJump_Land",
]);
const stripAnim = async (doc) => {
  for (const a of doc.getRoot().listAnimations()) {
    if (KEEP.has(a.getName())) continue;
    // disposing an animation leaves its samplers (and their keyframe data) behind
    for (const ch of a.listChannels()) ch.dispose();
    for (const sm of a.listSamplers()) sm.dispose();
    a.dispose();
  }
  for (const n of doc.getRoot().listNodes()) if (n.getMesh()) n.setMesh(null).setSkin(null);
  // bones only rotate; position tracks matter for the hips (and the root),
  // scale tracks never — dropping the rest cuts the file by about two thirds
  for (const a of doc.getRoot().listAnimations()) {
    for (const ch of a.listChannels()) {
      const p = ch.getTargetPath();
      const name = ch.getTargetNode() ? ch.getTargetNode().getName() : "";
      if (p === "scale" || (p === "translation" && !/^(pelvis|root|hips)$/i.test(name))) {
        const sm = ch.getSampler();
        ch.dispose();
        if (sm && !sm.listParents().some((x) => x.propertyType === "AnimationChannel")) sm.dispose();
      }
    }
  }
  for (const a of doc.getRoot().listAnimations()) {
    for (const sm of a.listSamplers()) if (!a.listChannels().some((c) => c.getSampler() === sm)) sm.dispose();
  }
  await doc.transform(resample({ tolerance: 0.0005 }), prune());
};
const UAL = (v) => {
  const root = path.join(SRC, v === 1 ? "Universal_Animation_Library_Standard_" : "Universal_Animation_Library_2_Standard_");
  const dir = findDir(root, v === 1 ? "UAL1_Standard.glb" : "UAL2_Standard.glb");
  return path.join(dir, v === 1 ? "UAL1_Standard.glb" : "UAL2_Standard.glb");
};
await pack("anims.glb", [{ file: UAL(1), name: "ual1" }, { file: UAL(2), name: "ual2" }], { tex: 256, prep: stripAnim });

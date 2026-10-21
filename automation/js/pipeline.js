/**
 * The campaign agents: brand profile → brief → 3 routes (human picks) → copy & TVC
 * script → brand/compliance QC → key visuals → TV commercial. Each step is one
 * function; the full product maps them onto a LangGraph orchestrator.
 */

import { chat, chatJson, contextBlock, get, image, put, saveBlob, search, speak, animate, byWs, settings } from "./engine.js";
import { compose, renderTVC, cropStill } from "./media.js";

/* ── workspaces & brand ──────────────────────────────────────────────── */

export async function createWorkspace(name) {
  const id = name.toLowerCase().replace(/[^a-z0-9]+/g, "-").replace(/^-|-$/g, "").slice(0, 40) + "-" + Math.random().toString(36).slice(2, 6);
  const w = { id, name, created: Date.now() };
  await put("workspaces", w);
  return w;
}

const PROBES = [
  "brand guidelines colour palette hex codes primary secondary colours",
  "logo placement footer lockup brand mark rules",
  "tone of voice brand personality how we speak",
  "do and don't rules avoid never always",
  "tagline brand line slogan sign-off",
  "products services offers propositions",
  "target audience customers segments",
];

export async function draftBrand(ws, onStep) {
  const meta = await get("workspaces", ws);
  const seen = new Set();
  const hits = [];
  for (const q of PROBES) {
    for (const h of await search(ws, q, 4)) {
      const k = `${h.file}:${h.page}:${h.text.slice(0, 50)}`;
      if (!seen.has(k)) { seen.add(k); hits.push(h); }
    }
  }
  if (!hits.length) throw new Error("Upload some documents first: the profile is drafted from them.");
  const p = await chatJson(
    "You are a brand strategist building a machine-readable brand profile from source documents. Use only what the sources support; leave a field empty rather than invent. Hex colours must be real #RRGGBB values found or clearly implied.",
    `Brand / workspace name: ${meta.name}\n\nSOURCES:\n${contextBlock(hits, 9000)}\n\nReturn JSON with exactly these keys:\n{"brand_name":"","tagline":"","palette":[{"name":"","hex":"#RRGGBB","use":""}],"tone":["3-6 short words"],"audiences":[""],"products":[{"name":"","what_it_sells":""}],"rules_do":["short checkable rules"],"rules_dont":["short checkable rules"],"mandatories":["things every asset must carry"],"languages":[""]}`,
    onStep,
  );
  p.palette = (p.palette || []).filter((c) => /^#[0-9a-fA-F]{6}$/.test(String(c.hex || "").trim())).map((c) => ({ ...c, hex: c.hex.trim().toUpperCase() }));
  if (!p.palette.length) p.palette = [{ name: "Primary", hex: "#112548", use: "default" }, { name: "Accent", hex: "#078581", use: "default" }];
  p.brand_name = p.brand_name || meta.name;
  p.source_refs = hits.map((h, i) => ({ n: i + 1, file: h.file, page: h.page }));
  meta.brand = p;
  await put("workspaces", meta);
  return p;
}

export function colours(profile) {
  const pal = (profile?.palette || []).map((c) => c.hex);
  const lum = (h) => { const [r, g, b] = [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16)); return 0.2126 * r + 0.7152 * g + 0.0722 * b; };
  const sorted = [...pal].sort((a, b) => lum(a) - lum(b));
  const primary = sorted[0] || "#112548";
  const accent = pal.find((c) => c !== primary && lum(c) > 60 && lum(c) < 200) || sorted[1] || "#078581";
  return [primary, accent];
}

function brandBrief(p) {
  if (!p) return "(no brand profile yet: rely on the sources)";
  return `Brand: ${p.brand_name}\nTagline: ${p.tagline}\nPalette: ${(p.palette || []).map((c) => `${c.name} ${c.hex}`).join(", ")}\nTone: ${(p.tone || []).join(", ")}\nDo: ${(p.rules_do || []).join("; ")}\nDon't: ${(p.rules_dont || []).join("; ")}\nMandatories: ${(p.mandatories || []).join("; ")}`;
}
const briefText = (b) => Object.entries(b).filter(([, v]) => v).map(([k, v]) => `${k[0].toUpperCase() + k.slice(1)}: ${v}`).join("\n");

/* ── knowledge Q&A ───────────────────────────────────────────────────── */

export async function ask(ws, q, onStep) {
  const hits = await search(ws, q, 8);
  if (!hits.length) return { answer: "No documents in this workspace yet.", sources: [] };
  const answer = await chat("Answer only from the numbered sources. Cite them like [2]. If they don't contain the answer, say so.", `SOURCES\n${contextBlock(hits, 7000)}\n\nQUESTION: ${q}`, onStep);
  return { answer, sources: hits.map((h, i) => ({ n: i + 1, ...h, text: h.text.slice(0, 400) })) };
}

/* ── campaign ────────────────────────────────────────────────────────── */

export async function createCampaign(ws, brief) {
  const c = { id: `${Date.now()}`, ws, title: brief.product || "Campaign", brief, status: "brief", created: Date.now(), log: [] };
  await put("campaigns", c);
  return c;
}

async function update(c, fields) { Object.assign(c, fields); await put("campaigns", c); return c; }

export async function strategy(c, onChange) {
  const ws = c.ws;
  const b = c.brief;
  const step = (s) => { update(c, { step: s }); onChange(c); };
  await update(c, { status: "thinking", step: "Retrieving past work and brand rules", error: null });
  onChange(c);
  const hits = [...(await search(ws, `${b.product} ${b.objective} ${b.message} ${b.audience} campaign idea insight`, 8)), ...(await search(ws, "campaign concept insight tension idea script", 4))];
  const seen = new Set();
  const uniq = hits.filter((h) => { const k = `${h.file}:${h.page}`; if (seen.has(k)) return false; seen.add(k); return true; });
  const brand = (await get("workspaces", ws)).brand;
  step("Writing three creative routes");
  let routes = await chatJson(
    "You are the strategy lead at a top advertising agency. Write campaign routes in the brand's own house style, learning from the past work in the sources. Cite the sources you drew on by number. Never reuse expired offers, prices or dates from the sources.",
    `BRAND PROFILE\n${brandBrief(brand)}\n\nBRIEF\n${briefText(b)}\n\nSOURCES (past work and brand material)\n${contextBlock(uniq, 7000)}\n\nWrite exactly 3 distinct creative routes. JSON:\n{"routes":[{"name":"","insight":"","tension":"","idea":"","line":"campaign line / sign-off","why_it_fits":"","sources":[1]}]}`,
    step,
  );
  routes = routes.routes || routes;
  await update(c, { routes, sources: uniq.map((h, i) => ({ n: i + 1, file: h.file, page: h.page, text: h.text.slice(0, 300) })), status: "routes", step: "" });
  onChange(c);
}

export async function produce(c, route, onChange) {
  const ws = c.ws;
  const log = (s) => { c.log.push({ t: Date.now(), msg: s }); update(c, { step: s }); onChange(c); };
  try {
    await update(c, { chosen: route, status: "producing", error: null, assets: { statics: [] }, tvc_report: [] });
    const r = c.routes[route];
    const b = c.brief;
    const wsMeta = await get("workspaces", ws);
    const brand = wsMeta.brand || { brand_name: wsMeta.name };
    const cols = colours(brand);
    const voLang = /urdu/i.test(b.languages || "") ? "ur" : "en";

    log("Writing copy, the TV script and the media plan");
    const facts = contextBlock(await search(ws, `${b.product} features benefits terms offer`, 5), 3500);
    const writer = "You are a senior copywriter. One audience, one benefit, one CTA per asset (Hook → Benefit → Proof → Action). Use only facts from the brief or the sources; no invented numbers, rates or dates. Write Urdu in Urdu script and Roman Urdu in Latin script.";
    const ctx = `BRAND PROFILE
${brandBrief(brand)}

BRIEF
${briefText(b)}

CHOSEN ROUTE
${JSON.stringify(r)}

PRODUCT FACTS (sources)
${facts}`;
    // two smaller calls are far more reliable than one huge JSON reply
    const copy = await chatJson(writer, `${ctx}

JSON:
{"key_visuals":[{"title":"","headline":"max 6 words","subline":"max 14 words","cta":"max 3 words","legal":"short T&C line or empty","scene":"photographic scene for an image model: people, place, light, mood. No text, no logos, no cards."}],"captions":{"en":"","roman_ur":"","ur":""},"media_plan":[{"channel":"","format":"","why":"","timing":""}]}
Give exactly 3 key_visuals and 4-6 media_plan rows.`, log);
    log("Writing the TV commercial script and shot list");
    const tvcOut = await chatJson(
      "You are an award-winning TV commercial director and scriptwriter.",
      `${ctx}

Write a 30-second TV commercial. Tell it with pictures and voice-over, never on-camera dialogue or lip-sync: one idea, one character, set-up → reveal → close. JSON:
{"tvc":{"title":"","hero":"ONE main character described precisely so every shot shows the same person: age, gender, face, hair, clothing colours","location":"one real-feeling Pakistani setting, precisely described","vo_language":"${voLang}","vo":["5-6 voice-over lines in ${voLang === "ur" ? "Urdu script" : "English"}, about 24 seconds spoken in total"],"subtitles":["the same lines in ${voLang === "ur" ? "Roman Urdu" : "English"}"],"shots":[{"dur":4.5,"shot":"wide | medium | close-up | insert","scene":"what the camera sees in the first frame. No text, no logos, no bank cards, no readable screens","motion":"what moves and how the camera moves, one sentence","super":"short on-screen text or empty"}],"end_line":"","legal":"short legal super"}}
Exactly 6 shots.`,
      log,
    );
    copy.tvc = tvcOut.tvc || tvcOut;
    await update(c, { copy });

    log("Brand & compliance QC");
    let qc = await qcCheck(copy, brand, b, log);
    if (qc.some((x) => !x.pass && x.fixable)) {
      log("QC found issues: revising copy");
      const fixes = qc.filter((x) => !x.pass).map((x) => x.note).join("; ");
      const fixed = await chatJson("You are a senior copywriter fixing QC issues. Keep everything else unchanged.", `QC ISSUES: ${fixes}\n\nCOPY JSON:\n${JSON.stringify(copy)}\n\nReturn the corrected JSON with the same keys.`, log);
      if (fixed && fixed.key_visuals) Object.assign(copy, fixed);
      qc = await qcCheck(copy, brand, b, log);
    }
    await update(c, { copy, qc });

    const mood = `colour grade inspired by ${(brand.palette || []).slice(0, 3).map((p) => p.hex).join(", ")}`;
    const logo = await get("blobs", `${ws}/logo`);
    for (const [i, kv] of (copy.key_visuals || []).slice(0, 3).entries()) {
      log(`Key visual ${i + 1}/3: generating the scene`);
      const scene = await image(`${kv.scene}. Premium advertising photography, natural light, Pakistan, ${mood}. No text, no letters, no logos, no bank cards.`, 1024, 1060, 11 + i, log);
      const clean = await cropStill(scene, 1024, 1024);
      log(`Key visual ${i + 1}/3: composing all formats`);
      for (const fmt of ["square", "portrait", "story"]) {
        const out = await compose(clean, fmt, kv, brand, cols, logo);
        const key = `${c.id}/kv${i + 1}_${fmt}`;
        await saveBlob(key, out);
        c.assets.statics.push({ kv: i + 1, format: fmt, key });
      }
      await update(c, {});
      onChange(c);
    }

    const spot = copy.tvc || {};
    const shots = (spot.shots || []).slice(0, 6);
    if (shots.length) {
      const look = `Photorealistic 35mm film still from a premium TV commercial, natural skin texture, shallow depth of field, soft motivated light, ${mood}. No text, no logos, no bank cards, no readable screens.`;
      const plates = [];
      for (const [i, sh] of shots.entries()) {
        log(`TV commercial shot ${i + 1}/${shots.length}: keyframe`);
        const raw = await image(`${sh.shot || ""} shot. ${sh.scene}. Main character: ${spot.hero}. Location: ${spot.location}. ${look}`, 1280, 760, 301, log);
        const still = await cropStill(raw, 1280, 720);
        await saveBlob(`${c.id}/key${i + 1}`, still);
        log(`TV commercial shot ${i + 1}/${shots.length}: motion (free GPU queue, ~1 min)`);
        const clip = await animate(still, sh.motion || "subtle natural motion, slow push-in", +sh.dur || 4.5, log);
        if (clip) await saveBlob(`${c.id}/clip${i + 1}`, clip);
        plates.push({ still, clip, dur: Math.max(3, Math.min(6, +sh.dur || 4.5)), super: sh.super || "" });
        c.tvc_report.push({ n: i + 1, shot: sh.shot, dur: plates[i].dur, super: sh.super || "", provider: clip ? (settings().video.provider === "veo" ? "veo 3.1" : "wan 2.2") : "camera move", key: `${c.id}/key${i + 1}` });
        await update(c, {});
        onChange(c);
      }
      log("TV commercial: voice-over");
      const vo = await speak((spot.vo || []).join(" "), spot.vo_language || voLang);
      const music = await get("blobs", `${ws}/music`);
      log("TV commercial: edit, grade, supers, sound mix (keep this tab visible)");
      const { master, vertical, mime } = await renderTVC({ plates, vo, music, brand, cols, logo, endLine: spot.end_line, legal: spot.legal, onProgress: (p) => { c.step = `TV commercial: rendering ${Math.round(p * 100)}% (keep this tab visible)`; onChange(c); } });
      await saveBlob(`${c.id}/tvc_16x9`, master);
      if (vertical) await saveBlob(`${c.id}/tvc_9x16`, vertical);
      c.assets.film = `${c.id}/tvc_16x9`;
      c.assets.film_916 = vertical ? `${c.id}/tvc_9x16` : null;
      c.assets.film_mime = mime;
    }
    await update(c, { status: "review", step: "" });
    log("Ready for review");
  } catch (e) {
    console.error(e);
    await update(c, { status: "error", error: String(e.message || e).slice(0, 400), step: "" });
  }
  onChange(c);
}

async function qcCheck(copy, brand, brief, onStep) {
  const res = [];
  (copy.key_visuals || []).forEach((kv, i) => {
    const n = (kv.headline || "").split(/\s+/).filter(Boolean).length;
    res.push({ check: `KV${i + 1} headline length`, pass: n <= 8, note: `KV${i + 1} headline has ${n} words (max 8)`, fixable: true });
    res.push({ check: `KV${i + 1} single CTA`, pass: !!kv.cta && kv.cta.split(/\s+/).length <= 4, note: `KV${i + 1} needs one short CTA`, fixable: true });
  });
  const briefNums = new Set(briefText(brief).match(/\d+(?:\.\d+)?/g) || []);
  const text = (copy.key_visuals || []).map((kv) => `${kv.headline} ${kv.subline} ${kv.legal}`).join(" ") + " " + (copy.tvc?.legal || "");
  const stray = (text.match(/\d+(?:\.\d+)?/g) || []).filter((n) => !briefNums.has(n));
  res.push({ check: "Figures come from the brief", pass: !stray.length, note: stray.length ? `Remove figures not in the brief: ${stray.join(", ")}` : "All figures sourced", fixable: true });
  const donts = brand?.rules_dont || [];
  if (donts.length) {
    const v = await chatJson("You are a strict brand compliance reviewer.", `RULES (don't): ${JSON.stringify(donts)}\nDOs: ${JSON.stringify(brand.rules_do || [])}\n\nCOPY: ${JSON.stringify({ key_visuals: copy.key_visuals, tvc: copy.tvc, captions: copy.captions })}\n\nReturn JSON {"violations":[{"rule":"","where":"","fix":""}]} - empty list if none.`, onStep);
    const vs = v.violations || [];
    res.push({ check: "Brand rules", pass: !vs.length, note: vs.map((x) => `${x.rule}: ${x.fix}`).join("; ") || "No violations", fixable: true });
  }
  res.push({ check: "Regulatory / Shariah wording", pass: true, note: "Flagged for a human approver before release (always)", fixable: false, human: true });
  return res;
}

export const campaignsFor = (ws) => byWs("campaigns", ws);

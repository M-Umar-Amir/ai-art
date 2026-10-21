/**
 * Campaign Studio AI — browser engine (demo build for epical.art/automation).
 *
 * Everything runs in the visitor's browser: documents are parsed, embedded and
 * searched locally (IndexedDB), and models are called directly from the page.
 * Free models by default; any provider can be switched to the visitor's own key
 * in Settings (kept in this browser only).
 */

const CDN = {
  transformers: "https://cdn.jsdelivr.net/npm/@huggingface/transformers@4.3.1/+esm",
  pdf: "https://cdn.jsdelivr.net/npm/pdfjs-dist@6.4.299/build/pdf.min.mjs",
  pdfWorker: "https://cdn.jsdelivr.net/npm/pdfjs-dist@6.4.299/build/pdf.worker.min.mjs",
  jszip: "https://cdn.jsdelivr.net/npm/jszip@3.10.2/+esm",
  gradio: "https://cdn.jsdelivr.net/npm/@gradio/client@2.7.1/+esm",
  anthropic: "https://cdn.jsdelivr.net/npm/@anthropic-ai/sdk@0.137.0/+esm",
  jsonrepair: "https://cdn.jsdelivr.net/npm/jsonrepair@3.15.0/+esm",
};
const EMBED_MODEL = "Xenova/paraphrase-multilingual-MiniLM-L12-v2"; // EN / Urdu / Roman Urdu
export const WAN_SPACE = "zerogpu-aoti/wan2-2-fp8da-aoti-faster";

/* ── storage (IndexedDB) ─────────────────────────────────────────────── */

let dbp = null;
function db() {
  if (!dbp)
    dbp = new Promise((ok, no) => {
      const r = indexedDB.open("campaign-studio", 1);
      r.onupgradeneeded = () => {
        const d = r.result;
        d.createObjectStore("workspaces", { keyPath: "id" });
        d.createObjectStore("docs", { keyPath: "id" }).createIndex("ws", "ws");
        d.createObjectStore("chunks", { keyPath: "id", autoIncrement: true }).createIndex("ws", "ws");
        d.createObjectStore("campaigns", { keyPath: "id" }).createIndex("ws", "ws");
        d.createObjectStore("blobs");
        d.createObjectStore("kv");
      };
      r.onsuccess = () => ok(r.result);
      r.onerror = () => no(r.error);
    });
  return dbp;
}
async function tx(store, mode, fn) {
  const d = await db();
  return new Promise((ok, no) => {
    const t = d.transaction(store, mode);
    const s = t.objectStore(store);
    let out;
    Promise.resolve(fn(s)).then((v) => (out = v));
    t.oncomplete = () => ok(out);
    t.onerror = () => no(t.error);
  });
}
const req = (r) => new Promise((ok, no) => ((r.onsuccess = () => ok(r.result)), (r.onerror = () => no(r.error))));
export const put = (store, v, key) => tx(store, "readwrite", (s) => req(key === undefined ? s.put(v) : s.put(v, key)));
export const get = (store, key) => tx(store, "readonly", (s) => req(s.get(key)));
export const del = (store, key) => tx(store, "readwrite", (s) => req(s.delete(key)));
export const byWs = (store, ws) => tx(store, "readonly", (s) => req(s.index("ws").getAll(ws)));
export const all = (store) => tx(store, "readonly", (s) => req(s.getAll()));

export async function saveBlob(key, blob) { await put("blobs", blob, key); }
export async function loadBlob(key) { return get("blobs", key); }
const urlCache = new Map();
export async function blobUrl(key) {
  if (urlCache.has(key)) return urlCache.get(key);
  const b = await loadBlob(key);
  if (!b) return "";
  const u = URL.createObjectURL(b);
  urlCache.set(key, u);
  return u;
}

/* ── settings (this browser only) ────────────────────────────────────── */

const DEFAULTS = {
  text: { provider: "free", model: "", key: "" },
  image: { provider: "free", model: "", key: "" },
  video: { provider: "free", model: "", key: "", hf_token: "" },
  voice: { voice_ur: "ur-PK-UzmaNeural", voice_en: "en-US-AvaNeural" },
};
export function settings() {
  let s = {};
  try { s = JSON.parse(localStorage.getItem("cs-settings") || "{}"); } catch {}
  const out = structuredClone(DEFAULTS);
  for (const k in s) Object.assign(out[k] || (out[k] = {}), s[k]);
  return out;
}
export function saveSettings(s) {
  try { localStorage.setItem("cs-settings", JSON.stringify(s)); } catch {}
}

/* ── models ──────────────────────────────────────────────────────────── */

const sleep = (ms) => new Promise((r) => setTimeout(r, ms));
// The anonymous free tier allows one text reply every 30 s and one image every
// 45 s per IP, so free calls go through queues that keep to that pace.
const queues = { text: { q: Promise.resolve(), last: 0, gap: 31000 }, image: { q: Promise.resolve(), last: 0, gap: 46000 } }; // free images: one per 45 s per IP
function serial(kind, fn) {
  const Q = queues[kind];
  const run = Q.q.then(async () => {
    const wait = Q.last + Q.gap - Date.now();
    if (wait > 0) await sleep(wait);
    try { return await fn(); } finally { Q.last = Date.now(); }
  });
  Q.q = run.catch(() => {});
  return run;
}
const retryAfter = (body, fallback) => {
  const m = String(body).match(/Retry after (\d+)s/i);
  return m ? (+m[1] + 2) * 1000 : fallback;
};

export async function chat(system, user, onWait) {
  const cfg = settings().text;
  if (cfg.provider === "openai") {
    const r = await fetch("https://api.openai.com/v1/chat/completions", {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${cfg.key}` },
      body: JSON.stringify({ model: cfg.model || "gpt-5-mini", messages: [{ role: "system", content: system }, { role: "user", content: user }] }),
    });
    if (!r.ok) throw new Error(`OpenAI ${r.status}: ${(await r.text()).slice(0, 200)}`);
    return (await r.json()).choices[0].message.content;
  }
  if (cfg.provider === "anthropic") {
    const { default: Anthropic } = await import(CDN.anthropic);
    const client = new Anthropic({ apiKey: cfg.key, dangerouslyAllowBrowser: true });
    const model = cfg.model || "claude-opus-5-5";
    const extra = ["claude-opus-5-5", "claude-fable-5-1", "claude-opus-5", "claude-sonnet-5-5"].includes(model)
      ? { betas: ["server-side-fallback-2026-07-01"], fallbacks: "default" } // re-run on a fallback model if declined
      : {};
    const msg = await client.beta.messages
      .stream({ model, max_tokens: 16000, system, output_config: { effort: "medium" }, messages: [{ role: "user", content: user }], ...extra })
      .finalMessage();
    if (msg.stop_reason === "refusal") throw new Error("The model declined this request.");
    return msg.content.filter((b) => b.type === "text").map((b) => b.text).join("");
  }
  if (cfg.provider === "gemini") {
    const model = cfg.model || "gemini-3-flash";
    const r = await fetch(`https://generativelanguage.googleapis.com/v1beta/models/${model}:generateContent?key=${encodeURIComponent(cfg.key)}`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ systemInstruction: { parts: [{ text: system }] }, contents: [{ role: "user", parts: [{ text: user }] }] }),
    });
    if (!r.ok) throw new Error(`Gemini ${r.status}: ${(await r.text()).slice(0, 200)}`);
    const j = await r.json();
    return j.candidates[0].content.parts.map((p) => p.text || "").join("");
  }
  // free (Pollinations)
  let last = "";
  let pause = 0;
  for (let attempt = 0; attempt < 10; attempt++) {
    const out = await serial("text", async () => {
      try {
        const r = await fetch("https://text.pollinations.ai/openai", {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify({ model: cfg.model || "openai", private: true, reasoning_effort: "low", max_tokens: 6000, messages: [{ role: "system", content: system }, { role: "user", content: user }] }),
        });
        if (r.ok) {
          const j = await r.json().catch(() => null);
          const text = j?.choices?.[0]?.message?.content;
          if (text) return text;
          last = "empty reply";
        } else {
          last = `${r.status}`;
          pause = retryAfter(await r.text().catch(() => ""), 0);
        }
      } catch (e) {
        last = e.message;
      }
      return null;
    });
    if (out !== null) return out;
    onWait && onWait(`Free text model is busy (one reply per 30 s on the free tier), retrying ${attempt + 1}/10…`);
    await sleep(pause || Math.min(45000, 10000 + attempt * 5000));
    pause = 0;
  }
  throw new Error(`Free text model busy (${last}). Try again in a minute, or add your own key in Settings.`);
}

/** Parse model JSON leniently: fences, smart quotes, trailing commas, and replies cut off mid-object. */
export function parseLoose(text) {
  let t = String(text || "").replace(/```(?:json)?/gi, "").trim();
  const start = t.search(/[\[{]/);
  if (start < 0) return null;
  t = t.slice(start).replace(/[“”]/g, '"').replace(/,\s*([}\]])/g, "$1");
  try { return JSON.parse(t); } catch {}
  // close whatever was left open (a reply cut off by the token limit)
  const stack = [];
  let inStr = false;
  let esc = false;
  let safe = null; // { at, stack } at the last comma outside a string
  for (let i = 0; i < t.length; i++) {
    const ch = t[i];
    if (inStr) {
      if (esc) esc = false;
      else if (ch === "\\") esc = true;
      else if (ch === '"') inStr = false;
      continue;
    }
    if (ch === '"') inStr = true;
    else if (ch === "{" || ch === "[") stack.push(ch === "{" ? "}" : "]");
    else if (ch === "}" || ch === "]") {
      stack.pop();
      if (!stack.length) { try { return JSON.parse(t.slice(0, i + 1)); } catch {} }
    } else if (ch === ",") safe = { at: i, stack: [...stack] };
  }
  const [body, open] = inStr || /:\s*$/.test(t) ? (safe ? [t.slice(0, safe.at), safe.stack] : [t, stack]) : [t.replace(/,\s*$/, ""), stack];
  try { return JSON.parse(body + [...open].reverse().join("")); } catch { return null; }
}

export async function chatJson(system, user, onWait) {
  const sys = system + "\nReply with valid JSON only. No markdown fences, no commentary.";
  let prompt = user;
  let last = "";
  for (let i = 0; i < 3; i++) {
    const out = await chat(sys, prompt, onWait);
    last = out;
    let parsed = parseLoose(out);
    if (!parsed) {
      // raw newlines, stray quotes in Urdu text, comments…: hand it to a real repairer
      try {
        const { jsonrepair } = await import(CDN.jsonrepair);
        const t = out.slice(Math.max(0, out.search(/[\[{]/)));
        parsed = JSON.parse(jsonrepair(t));
      } catch {}
    }
    if (parsed && typeof parsed === "object") return parsed;
    prompt = user + "\n\nYour previous reply was not valid JSON. Return only the JSON.";
  }
  throw new Error("Model did not return JSON: " + last.slice(0, 200));
}

/** An image as a Blob. Free tier: Pollinations FLUX (it stamps a small mark bottom-right; callers crop). */
export async function image(prompt, w, h, seed = 1, onWait) {
  const cfg = settings().image;
  if (cfg.provider === "openai") {
    const size = Math.abs(w - h) < 100 ? "1024x1024" : w > h ? "1536x1024" : "1024x1536";
    const r = await fetch("https://api.openai.com/v1/images/generations", {
      method: "POST",
      headers: { "Content-Type": "application/json", Authorization: `Bearer ${cfg.key}` },
      body: JSON.stringify({ model: cfg.model || "gpt-image-1", prompt, size, quality: "medium" }),
    });
    if (!r.ok) throw new Error(`OpenAI image ${r.status}`);
    const b64 = (await r.json()).data[0].b64_json;
    return await (await fetch(`data:image/png;base64,${b64}`)).blob();
  }
  let last = "";
  let pause = 0;
  for (let attempt = 0; attempt < 8; attempt++) {
    const out = await serial("image", async () => {
      try {
        const u = `https://image.pollinations.ai/prompt/${encodeURIComponent(prompt.slice(0, 900))}?width=${w}&height=${h}&seed=${seed + attempt}&nologo=true&model=${cfg.model || "flux"}`;
        const r = await fetch(u);
        if (r.ok && (r.headers.get("content-type") || "").startsWith("image")) return await r.blob();
        last = `${r.status}`;
        pause = retryAfter(await r.text().catch(() => ""), 0);
      } catch (e) {
        last = e.message;
      }
      return null;
    });
    if (out) return out;
    onWait && onWait(`Free image model is busy (one image per 45 s on the free tier), retrying ${attempt + 1}/8…`);
    await sleep(pause || Math.min(45000, 10000 + attempt * 5000));
    pause = 0;
  }
  throw new Error(`Free image model busy (${last}).`);
}

/** Voice-over MP3 via the site's /api/automation-tts (free Edge neural voices). */
export async function speak(text, lang) {
  const v = settings().voice;
  const r = await fetch("/api/automation-tts", {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ text, voice: lang === "ur" ? v.voice_ur : v.voice_en }),
  });
  if (!r.ok) throw new Error("Voice service unavailable");
  return await r.blob();
}

/** Motion for one shot: Blob (MP4) or null when the caller should fall back to a camera move. */
export async function animate(stillBlob, motion, seconds, onWait) {
  const cfg = settings().video;
  try {
    if (cfg.provider === "veo" && cfg.key) return await veo(stillBlob, motion, seconds, cfg, onWait);
    const { Client, handle_file } = await import(CDN.gradio);
    const client = await Client.connect(WAN_SPACE, cfg.hf_token ? { hf_token: cfg.hf_token } : {});
    const res = await client.predict("/generate_video", {
      input_image: handle_file(stillBlob),
      prompt: `${motion}. Realistic natural motion, cinematic TV commercial, stable faces and hands.`,
      steps: 6,
      duration_seconds: Math.min(5, Math.max(2, seconds)),
      guidance_scale: 1,
      guidance_scale_2: 1,
      seed: 42,
      randomize_seed: false,
    });
    const d = res.data[0];
    const url = d?.url || d?.video?.url || (typeof d === "string" ? d : null);
    if (!url) throw new Error("no video in response");
    const r = await fetch(url);
    if (!r.ok) throw new Error(`video ${r.status}`);
    return await r.blob();
  } catch (e) {
    console.warn("motion fallback:", e);
    onWait && onWait(`Motion model unavailable (${String(e.message || e).slice(0, 80)}); using a camera move`);
    return null;
  }
}

async function veo(stillBlob, motion, seconds, cfg, onWait) {
  const base = "https://generativelanguage.googleapis.com/v1beta";
  const model = cfg.model || "veo-3.1-generate-preview";
  const b64 = await new Promise((ok) => { const fr = new FileReader(); fr.onload = () => ok(String(fr.result).split(",")[1]); fr.readAsDataURL(stillBlob); });
  const body = {
    instances: [{ prompt: motion, image: { bytesBase64Encoded: b64, mimeType: stillBlob.type || "image/jpeg" } }],
    parameters: { aspectRatio: "16:9", durationSeconds: seconds > 6 ? 8 : seconds > 4 ? 6 : 4, resolution: "1080p" },
  };
  const r = await fetch(`${base}/models/${model}:predictLongRunning?key=${encodeURIComponent(cfg.key)}`, { method: "POST", headers: { "Content-Type": "application/json" }, body: JSON.stringify(body) });
  if (!r.ok) throw new Error(`Veo ${r.status}: ${(await r.text()).slice(0, 160)}`);
  const op = (await r.json()).name;
  for (let i = 0; i < 120; i++) {
    await sleep(10000);
    onWait && onWait(`Veo rendering… ${i * 10}s`);
    const s = await (await fetch(`${base}/${op}?key=${encodeURIComponent(cfg.key)}`)).json();
    if (s.done) {
      if (s.error) throw new Error(s.error.message || "Veo error");
      const uri = s.response.generateVideoResponse.generatedSamples[0].video.uri;
      const v = await fetch(uri + (uri.includes("?") ? "&" : "?") + "key=" + encodeURIComponent(cfg.key));
      return await v.blob();
    }
  }
  throw new Error("Veo timed out");
}

/* ── knowledge: read, chunk, embed, search ───────────────────────────── */

let embedderP = null;
export function embedder(onProgress) {
  if (!embedderP)
    embedderP = (async () => {
      const { pipeline, env } = await import(CDN.transformers);
      env.allowLocalModels = false;
      // WASM works in every browser; this small model doesn't need WebGPU
      return await pipeline("feature-extraction", EMBED_MODEL, { dtype: "q8", device: "wasm", progress_callback: (p) => onProgress && p.status === "progress" && onProgress(p) });
    })();
  return embedderP;
}

async function embed(texts) {
  const ex = await embedder();
  const out = await ex(texts, { pooling: "mean", normalize: true });
  const dim = out.dims[out.dims.length - 1];
  const vecs = [];
  for (let i = 0; i < texts.length; i++) vecs.push(new Float32Array(out.data.slice(i * dim, (i + 1) * dim)));
  return vecs;
}

async function readPages(file) {
  const name = file.name.toLowerCase();
  if (name.endsWith(".pdf")) {
    const pdfjs = await import(CDN.pdf);
    pdfjs.GlobalWorkerOptions.workerSrc = CDN.pdfWorker;
    const doc = await pdfjs.getDocument({ data: await file.arrayBuffer() }).promise;
    const pages = [];
    for (let i = 1; i <= doc.numPages; i++) {
      const p = await doc.getPage(i);
      const tc = await p.getTextContent();
      let text = "";
      for (const it of tc.items) text += it.str + (it.hasEOL ? "\n" : " ");
      pages.push([i, text]);
    }
    return pages;
  }
  if (name.endsWith(".pptx")) {
    const { default: JSZip } = await import(CDN.jszip);
    const zip = await JSZip.loadAsync(await file.arrayBuffer());
    const slides = Object.keys(zip.files).filter((n) => /^ppt\/slides\/slide\d+\.xml$/.test(n)).sort((a, b) => +a.match(/\d+/)[0] - +b.match(/\d+/)[0]);
    const out = [];
    for (const s of slides) {
      const xml = await zip.file(s).async("string");
      const paras = xml.split(/<\/a:p>/).map((p) => [...p.matchAll(/<a:t>([^<]*)<\/a:t>/g)].map((m) => m[1]).join("")).filter(Boolean);
      const txt = paras.join("\n").replace(/&amp;/g, "&").replace(/&lt;/g, "<").replace(/&gt;/g, ">").replace(/&quot;/g, '"').replace(/&apos;/g, "'");
      out.push([+s.match(/\d+/)[0], txt]);
    }
    return out;
  }
  return [[1, await file.text()]];
}

function* chunks(text, size = 900, overlap = 150) {
  text = text.replace(/ /g, " ").replace(/[ \t]+/g, " ").replace(/\n{3,}/g, "\n\n").trim();
  if (text.length <= size) {
    if (text.length > 40) yield text;
    return;
  }
  let start = 0;
  while (start < text.length) {
    let end = Math.min(text.length, start + size);
    const cut = Math.max(text.lastIndexOf(". ", end), text.lastIndexOf("\n", end));
    if (cut > start + size * 0.6 && end < text.length) end = cut + 1;
    const piece = text.slice(start, end).trim();
    if (piece.length > 40) yield piece;
    if (end >= text.length) break;
    start = end - overlap;
  }
}

export async function ingest(ws, file, onProgress) {
  const id = crypto.randomUUID().slice(0, 10);
  const doc = { id, ws, name: file.name, status: "reading", progress: 0, added: Date.now() };
  await put("docs", doc);
  onProgress && onProgress(doc);
  try {
    const pages = await readPages(file);
    const items = [];
    let empty = 0;
    for (const [page, text] of pages) {
      let got = false;
      for (const piece of chunks(text)) { items.push({ page, text: piece }); got = true; }
      if (!got) empty++;
    }
    doc.status = "embedding";
    doc.pages = pages.length;
    doc.image_only_pages = empty;
    await embedder((p) => { doc.status = `downloading model ${Math.round(p.progress || 0)}%`; onProgress && onProgress(doc); });
    doc.status = "embedding";
    for (let i = 0; i < items.length; i += 16) {
      await new Promise((r) => setTimeout(r, 0)); // let the page repaint between batches
      const part = items.slice(i, i + 16);
      const vecs = await embed(part.map((x) => x.text));
      await tx("chunks", "readwrite", (s) => { part.forEach((x, k) => s.put({ ws, doc: id, file: file.name, page: x.page, text: x.text, vec: vecs[k] })); });
      doc.progress = Math.min(1, (i + part.length) / Math.max(1, items.length));
      onProgress && onProgress({ ...doc });
    }
    Object.assign(doc, { status: "ready", progress: 1, chunks: items.length });
  } catch (e) {
    Object.assign(doc, { status: "error", error: String(e.message || e).slice(0, 200) });
  }
  await put("docs", doc);
  onProgress && onProgress(doc);
  return doc;
}

export async function deleteDoc(ws, docId) {
  const rows = await byWs("chunks", ws);
  await tx("chunks", "readwrite", (s) => { for (const r of rows) if (r.doc === docId) s.delete(r.id); });
  await del("docs", docId);
}

export async function search(ws, query, k = 8) {
  const rows = await byWs("chunks", ws);
  if (!rows.length) return [];
  const [q] = await embed([query]);
  const scored = rows.map((r) => {
    let s = 0;
    const v = r.vec;
    for (let i = 0; i < q.length; i++) s += q[i] * v[i];
    return { score: Math.round(s * 1000) / 1000, file: r.file, page: r.page, text: r.text };
  });
  scored.sort((a, b) => b.score - a.score);
  return scored.slice(0, k);
}

export function contextBlock(hits, maxChars = 6000) {
  const out = [];
  let used = 0;
  hits.forEach((h, i) => {
    const t = h.text.slice(0, 1200);
    if (used + t.length > maxChars) return;
    out.push(`[${i + 1}] (${h.file} p.${h.page})\n${t}`);
    used += t.length;
  });
  return out.join("\n\n");
}

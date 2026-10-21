/** Campaign Studio AI — UI controller for the browser demo. */

import * as E from "./engine.js";
import * as P from "./pipeline.js";

const $ = (s, r = document) => r.querySelector(s);
const esc = (s) => String(s ?? "").replace(/[&<>"]/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;" }[c]));
let WS = null;
let TAB = "knowledge";
let CID = null;
const running = new Map(); // campaign id → live campaign object while a job runs
const indexing = new Map(); // doc id → live doc while indexing

async function loadWorkspaces() {
  const list = (await E.all("workspaces")).sort((a, b) => a.created - b.created);
  $("#wsList").innerHTML = list.map((w) => `<button class="ws ${w.id === WS ? "on" : ""}" data-id="${w.id}">${esc(w.name)}</button>`).join("") || '<small style="color:#9fb3d1">none yet</small>';
  document.querySelectorAll(".ws").forEach((b) => (b.onclick = () => openWs(b.dataset.id)));
  if (!WS && list.length) openWs(list[0].id);
}
$("#wsNew").onsubmit = async (e) => {
  e.preventDefault();
  const n = $("#wsName").value.trim();
  if (!n) return;
  const w = await P.createWorkspace(n);
  $("#wsName").value = "";
  WS = w.id;
  await loadWorkspaces();
  openWs(w.id);
};
async function openWs(id) {
  WS = id;
  CID = null;
  const w = await E.get("workspaces", id);
  $("#noWs").style.display = "none";
  $("#wsView").style.display = "";
  $("#wsTitle").textContent = w.name;
  document.querySelectorAll(".ws").forEach((b) => b.classList.toggle("on", b.dataset.id === id));
  show(TAB);
}
document.querySelectorAll("nav button").forEach((b) => (b.onclick = () => show(b.dataset.t)));
function show(t) {
  TAB = t;
  document.querySelectorAll("nav button").forEach((b) => b.classList.toggle("on", b.dataset.t === t));
  document.querySelectorAll("main section").forEach((s) => (s.hidden = s.id !== "t-" + t));
  ({ knowledge, brand, campaigns, settings })[t]();
}

/* ── knowledge ── */
async function knowledge() {
  const el = $("#t-knowledge");
  const docs = (await E.byWs("docs", WS)).map((d) => indexing.get(d.id) || d);
  el.innerHTML = `<div class="card"><h3 style="margin-top:0">Upload brand material</h3><p class="muted">Brand books, past campaign decks, scripts, product sheets, rate cards (PDF, PPTX, TXT). Each file is read and embedded in your browser and is searchable as soon as it's indexed. The first upload downloads a ~120 MB multilingual model once.</p>
    <div class="row"><input type="file" id="up" multiple accept=".pdf,.pptx,.txt,.md" style="max-width:420px" /><button class="p" id="upBtn">Upload &amp; learn</button></div></div>
    <div class="card"><h3 style="margin-top:0">Documents</h3>${docs.length ? `<table><tr><th>File</th><th>Status</th><th>Pages</th><th>Chunks</th><th>Pages with little text</th><th></th></tr>${docs.map((d) => `<tr><td>${esc(d.name)}</td><td>${d.status === "ready" || d.status === "error" ? `<span class="pill ${d.status === "error" ? "err" : ""}">${d.status}</span> ${esc(d.error || "")}` : `<span class="bar"><i style="width:${Math.round((d.progress || 0) * 100)}%"></i></span> ${esc(d.status)}`}</td><td>${d.pages ?? ""}</td><td>${d.chunks ?? ""}</td><td>${d.image_only_pages ?? ""}</td><td><button class="s" data-del="${d.id}">Remove</button></td></tr>`).join("")}</table>` : '<p class="muted">No documents yet.</p>'}</div>
    <div class="card"><h3 style="margin-top:0">Ask the knowledge base</h3><div class="row"><input id="q" placeholder="e.g. What are the footer rules? What did we do for Independence Day?" style="flex:1" /><button class="p" id="askBtn">Ask</button></div><div id="ans"></div></div>`;
  $("#upBtn").onclick = async () => {
    const files = [...$("#up").files];
    if (!files.length) return;
    $("#upBtn").disabled = true;
    for (const f of files) {
      await E.ingest(WS, f, (d) => {
        indexing.set(d.id, d);
        if (TAB === "knowledge") knowledgeRows();
      });
    }
    indexing.clear();
    if (TAB === "knowledge") knowledge();
  };
  el.querySelectorAll("[data-del]").forEach((b) => (b.onclick = async () => { if (confirm("Remove this document and forget it?")) { await E.deleteDoc(WS, b.dataset.del); knowledge(); } }));
  $("#askBtn").onclick = async () => {
    const q = $("#q").value.trim();
    if (!q) return;
    $("#ans").innerHTML = '<p><span class="spin"></span> <span id="askStep">Searching and answering…</span></p>';
    try {
      const r = await P.ask(WS, q, (s) => ($("#askStep") && ($("#askStep").textContent = s)));
      $("#ans").innerHTML = `<h3>Answer</h3><p style="white-space:pre-wrap">${esc(r.answer)}</p><h3>Sources</h3>${r.sources.map((s) => `<p class="src"><b>[${s.n}]</b> ${esc(s.file)} · p.${s.page} · match ${s.score}<br>${esc(s.text)}…</p>`).join("")}`;
    } catch (e) {
      $("#ans").innerHTML = `<p class="pill err">${esc(e.message)}</p>`;
    }
  };
}
let rowTimer = null;
function knowledgeRows() {
  clearTimeout(rowTimer);
  rowTimer = setTimeout(knowledge, 400);
}

/* ── brand ── */
async function brand() {
  const el = $("#t-brand");
  const w = await E.get("workspaces", WS);
  const b = w.brand || {};
  const has = !!b.brand_name;
  const logoUrl = await E.blobUrl(`${WS}/logo`);
  el.innerHTML = `<div class="card"><div class="row" style="justify-content:space-between"><div><h3 style="margin:0">Brand profile</h3><p class="muted" style="margin:4px 0 0">Drafted automatically from this workspace's documents. Edit anything; the QC checks use these rules.</p></div><button class="p" id="draft">${has ? "Re-draft from documents" : "Draft from documents"}</button></div></div>
  ${has ? `<div class="grid2"><div class="card"><label>Brand name</label><input id="bn" value="${esc(b.brand_name)}"/><label>Tagline</label><input id="tg" value="${esc(b.tagline)}"/><label>Palette</label><div>${(b.palette || []).map((c) => `<span class="swatch"><i style="background:${c.hex}"></i>${esc(c.name)} ${c.hex}</span>`).join("")}</div><label>Tone</label><input id="tone" value="${esc((b.tone || []).join(", "))}"/>
    <label>Logo (PNG, optional)</label><div class="row"><input type="file" id="logo" accept="image/png" style="max-width:260px"/>${logoUrl ? `<img src="${logoUrl}" alt="logo" style="height:36px"/>` : ""}</div>
    <label>Music for commercials (licensed MP3, optional)</label><input type="file" id="music" accept="audio/mpeg" style="max-width:260px"/></div>
  <div class="card"><label>Do (one per line)</label><textarea id="do" rows="6">${esc((b.rules_do || []).join("\n"))}</textarea><label>Don't (one per line)</label><textarea id="dont" rows="6">${esc((b.rules_dont || []).join("\n"))}</textarea><label>Mandatories</label><textarea id="mand" rows="3">${esc((b.mandatories || []).join("\n"))}</textarea></div></div>
  <div class="card"><label>Products</label>${(b.products || []).map((p) => `<p style="margin:2px 0"><b>${esc(p.name)}</b> — ${esc(p.what_it_sells)}</p>`).join("") || '<p class="muted">none found</p>'}<p class="src">Drafted from: ${(b.source_refs || []).map((s) => `${esc(s.file)} p.${s.page}`).join(" · ")}</p><button class="p" id="saveB">Save profile</button></div>` : '<div class="card empty">No profile yet. Upload documents, then click “Draft from documents”.</div>'}`;
  $("#draft").onclick = async () => {
    const btn = $("#draft");
    btn.disabled = true;
    btn.innerHTML = '<span class="spin"></span> Reading the documents…';
    try { await P.draftBrand(WS, (s) => (btn.lastChild.textContent = " " + s)); } catch (e) { alert(e.message); }
    brand();
  };
  if (has) {
    $("#saveB").onclick = async () => {
      const lines = (id) => $(id).value.split("\n").map((s) => s.trim()).filter(Boolean);
      Object.assign(b, { brand_name: $("#bn").value, tagline: $("#tg").value, tone: $("#tone").value.split(",").map((s) => s.trim()).filter(Boolean), rules_do: lines("#do"), rules_dont: lines("#dont"), mandatories: lines("#mand") });
      w.brand = b;
      await E.put("workspaces", w);
      $("#saveB").textContent = "Saved ✓";
    };
    $("#logo").onchange = async () => { await E.saveBlob(`${WS}/logo`, $("#logo").files[0]); brand(); };
    $("#music").onchange = async () => { await E.saveBlob(`${WS}/music`, $("#music").files[0]); alert("Music saved for this brand's commercials"); };
  }
}

/* ── campaigns ── */
async function campaigns() {
  if (CID) return campaignView();
  const el = $("#t-campaigns");
  const list = (await P.campaignsFor(WS)).sort((a, b) => b.created - a.created);
  el.innerHTML = `<div class="card"><h3 style="margin-top:0">New campaign brief</h3><div class="grid2">
    <div><label>Product / area</label><input id="b_product" placeholder="e.g. DigiBank app"/></div><div><label>Objective</label><input id="b_objective" placeholder="Awareness / downloads / applications"/></div>
    <div><label>Audience</label><input id="b_audience" placeholder="Who, and why now?"/></div><div><label>Single key message</label><input id="b_message" placeholder="One benefit to remember"/></div>
    <div><label>Proof &amp; mandatories</label><input id="b_mandatories" placeholder="Facts, offer, T&Cs, CTA"/></div><div><label>Languages</label><select id="b_languages"><option>English + Urdu voice-over</option><option>English only</option></select></div>
    <div><label>Occasion / timing</label><input id="b_timing" placeholder="e.g. 14 August, Ramadan, launch"/></div><div><label>Deliverables</label><input id="b_deliverables" value="30-second TV commercial + 9:16 cutdown, 3 key visuals in 3 formats, captions, media plan"/></div>
  </div><div class="row" style="margin-top:12px"><button class="p" id="go">Generate 3 creative routes</button></div></div>
  <div class="card"><h3 style="margin-top:0">Campaigns</h3>${list.length ? `<table>${list.map((c) => `<tr><td><a href="#" data-c="${c.id}">${esc(c.title)}</a></td><td><span class="pill ${c.status === "error" ? "err" : ""}">${(running.get(c.id) || c).status}</span></td><td class="muted">${new Date(c.created).toLocaleString()}</td></tr>`).join("")}</table>` : '<p class="muted">None yet.</p>'}</div>`;
  $("#go").onclick = async () => {
    const brief = {};
    ["product", "objective", "audience", "message", "mandatories", "languages", "timing", "deliverables"].forEach((k) => (brief[k] = $("#b_" + k).value));
    if (!brief.product) return alert("Add a product / area");
    const c = await P.createCampaign(WS, brief);
    CID = c.id;
    running.set(c.id, c);
    P.strategy(c, onJob).catch((e) => { c.status = "error"; c.error = e.message; onJob(c); }).finally(() => running.delete(c.id));
    campaignView();
  };
  el.querySelectorAll("[data-c]").forEach((a) => (a.onclick = (e) => { e.preventDefault(); CID = a.dataset.c; campaignView(); }));
}
let viewTimer = null;
function onJob(c) {
  if (TAB === "campaigns" && CID === c.id) {
    clearTimeout(viewTimer);
    viewTimer = setTimeout(campaignView, 250);
  }
}

async function campaignView() {
  const el = $("#t-campaigns");
  const c = running.get(CID) || (await E.get("campaigns", CID));
  if (!c) { CID = null; return campaigns(); }
  const busy = running.has(c.id);
  const U = (k) => E.blobUrl(k);
  let h = `<div class="row" style="justify-content:space-between"><button class="s" id="back">← All campaigns</button><span class="pill ${c.status === "error" ? "err" : ""}">${c.status}</span></div>
    <h2 style="margin-top:12px">${esc(c.title)}</h2><p class="muted">${esc(Object.values(c.brief).filter(Boolean).join(" · "))}</p>`;
  if (busy) h += `<div class="card"><span class="spin"></span> <b>${esc(c.step || "Working…")}</b><p class="src">${(c.log || []).slice(-6).map((l) => esc(l.msg)).join(" → ")}</p></div>`;
  else if (["thinking", "producing", "brief"].includes(c.status)) h += `<div class="card"><span class="pill warn">interrupted</span> This job stopped when the page was closed. <button class="s" id="resume">Run this step again</button></div>`;
  if (c.error) h += `<div class="card"><span class="pill err">Error</span> ${esc(c.error)} <button class="s" id="resume">Retry this step</button></div>`;
  if (c.routes) {
    h += `<h3>Creative routes</h3><div class="grid3">${c.routes.map((r, i) => `<div class="route ${c.chosen === i ? "on" : ""}"><b>${esc(r.name)}</b><div><span class="muted">Insight:</span> ${esc(r.insight)}</div><div><span class="muted">Tension:</span> ${esc(r.tension)}</div><div><span class="muted">Idea:</span> ${esc(r.idea)}</div><div class="line">“${esc(r.line)}”</div><div class="src">Drew on ${(r.sources || []).map((n) => { const s = (c.sources || [])[n - 1]; return s ? `[${n}] ${esc(s.file)} p.${s.page}` : `[${n}]`; }).join(" · ")}</div>${!busy && (c.status === "routes" || c.status === "error" || c.status === "review" || c.status === "approved" || c.status === "revise") ? `<button class="p" data-route="${i}">${c.chosen === i ? "Produce again" : "Produce this route"}</button>` : ""}</div>`).join("")}</div>`;
  }
  if (c.qc) h += `<h3>Brand &amp; compliance QC</h3><div class="card"><table>${c.qc.map((q) => `<tr><td>${esc(q.check)}</td><td><span class="pill ${q.human ? "warn" : q.pass ? "" : "err"}">${q.human ? "human approval" : q.pass ? "pass" : "fail"}</span></td><td class="muted">${esc(q.note)}</td></tr>`).join("")}</table></div>`;
  const a = c.assets || {};
  if (a.film) {
    const m = await U(a.film);
    const v = a.film_916 ? await U(a.film_916) : "";
    const ext = (a.film_mime || "video/webm").includes("mp4") ? "mp4" : "webm";
    h += `<h3>TV commercial</h3><div class="row" style="align-items:flex-start"><div style="flex:3;min-width:300px"><video controls src="${m}" style="width:100%;border-radius:12px;background:#000"></video><p class="src">16:9 master · 1920×1080 · 25 fps · <a download="tvc_16x9.${ext}" href="${m}">Download</a></p></div>${v ? `<div style="flex:1;min-width:180px"><video controls src="${v}" style="width:100%;border-radius:12px;background:#000"></video><p class="src">9:16 cutdown · <a download="tvc_9x16.${ext}" href="${v}">Download</a></p></div>` : ""}</div>`;
  }
  if (c.tvc_report && c.tvc_report.length) {
    const imgs = await Promise.all(c.tvc_report.map((r) => U(r.key)));
    h += `<h3>TV commercial: shots</h3><div class="imgs">${c.tvc_report.map((r, i) => `<figure style="margin:0"><img src="${imgs[i]}" alt="shot ${r.n}"/><figcaption class="src">Shot ${r.n} · ${esc(r.shot || "")} · ${r.dur}s · motion: <b>${esc(r.provider)}</b>${r.super ? " · “" + esc(r.super) + "”" : ""}</figcaption></figure>`).join("")}</div>`;
  }
  if (a.statics && a.statics.length) {
    const imgs = await Promise.all(a.statics.map((s) => U(s.key)));
    h += `<h3>Key visuals</h3><div class="imgs">${a.statics.map((s, i) => `<figure style="margin:0"><img src="${imgs[i]}" alt="KV${s.kv} ${s.format}"/><figcaption class="src">KV${s.kv} · ${s.format} · <a download="kv${s.kv}_${s.format}.jpg" href="${imgs[i]}">download</a></figcaption></figure>`).join("")}</div>`;
  }
  if (c.copy) {
    const cp = c.copy;
    const fl = cp.tvc || {};
    h += `<div class="grid2" style="margin-top:14px"><div class="card"><h3 style="margin-top:0">TV script</h3><p><b>${esc(fl.title)}</b></p>${fl.hero ? `<p class="src">Hero: ${esc(fl.hero)}<br>Location: ${esc(fl.location)}</p>` : ""}${(fl.vo || []).map((l, i) => `<p class="${fl.vo_language === "ur" ? "ur" : ""}" style="margin:2px 0">${esc(l)}</p><p class="src" style="margin:0 0 6px">${esc((fl.subtitles || [])[i] || "")}</p>`).join("")}<p><b>End line:</b> ${esc(fl.end_line)}</p></div>
      <div class="card"><h3 style="margin-top:0">Social captions</h3><label>English</label><p>${esc(cp.captions?.en)}</p><label>Roman Urdu</label><p>${esc(cp.captions?.roman_ur)}</p><label>Urdu</label><p class="ur">${esc(cp.captions?.ur)}</p></div></div>
      <div class="card"><h3 style="margin-top:0">Media plan</h3><table><tr><th>Channel</th><th>Format</th><th>Why</th><th>Timing</th></tr>${(cp.media_plan || []).map((m) => `<tr><td>${esc(m.channel)}</td><td>${esc(m.format)}</td><td>${esc(m.why)}</td><td>${esc(m.timing)}</td></tr>`).join("")}</table></div>`;
  }
  if (!busy && ["review", "approved", "revise"].includes(c.status)) h += `<div class="card"><h3 style="margin-top:0">Review</h3><textarea id="note" rows="2" placeholder="Comments for the next round">${esc(c.review_note || "")}</textarea><div class="row" style="margin-top:8px"><button class="p" id="appr">Approve</button><button class="s" id="rev">Request revision</button>${c.status !== "review" ? `<span class="pill">${c.status}</span>` : ""}</div></div>`;
  el.innerHTML = h;
  $("#back").onclick = () => { CID = null; campaigns(); };
  const start = (fn) => { running.set(c.id, c); fn().catch((e) => { c.status = "error"; c.error = e.message; onJob(c); }).finally(() => { running.delete(c.id); onJob(c); }); campaignView(); };
  el.querySelectorAll("[data-route]").forEach((b) => (b.onclick = () => start(() => P.produce(c, +b.dataset.route, onJob))));
  if ($("#resume")) $("#resume").onclick = () => start(() => (c.routes && c.chosen != null ? P.produce(c, c.chosen, onJob) : P.strategy(c, onJob)));
  el.querySelectorAll(".imgs img").forEach((im) => (im.onclick = () => { $("#zoom img").src = im.src; $("#zoom").style.display = "grid"; }));
  if ($("#appr")) {
    const review = async (ok) => { c.status = ok ? "approved" : "revise"; c.review_note = $("#note").value; await E.put("campaigns", c); campaignView(); };
    $("#appr").onclick = () => review(true);
    $("#rev").onclick = () => review(false);
  }
}

/* ── settings ── */
function settings() {
  const s = E.settings();
  const el = $("#t-settings");
  const sel = (id, cur, opts) => `<select id="${id}">${opts.map(([v, l]) => `<option value="${v}" ${cur === v ? "selected" : ""}>${l}</option>`).join("")}</select>`;
  const key = (id, has) => `<input id="${id}" type="password" autocomplete="off" placeholder="${has ? "•••• saved (leave blank to keep)" : ""}"/>`;
  el.innerHTML = `<div class="card"><h3 style="margin-top:0">AI models</h3><p class="muted">Free models are used by default. Switch any task to your own provider account. Keys are stored in this browser only (localStorage) and are sent only to that provider.</p>
    <div class="grid3"><div><label>Text</label>${sel("text_p", s.text.provider, [["free", "Free (Pollinations)"], ["anthropic", "Anthropic Claude"], ["openai", "OpenAI"], ["gemini", "Google Gemini"]])}<label>Model (optional)</label><input id="text_m" value="${esc(s.text.model)}" placeholder="default"/><label>API key</label>${key("text_k", !!s.text.key)}</div>
    <div><label>Image</label>${sel("image_p", s.image.provider, [["free", "Free (Pollinations FLUX)"], ["openai", "OpenAI GPT Image"]])}<label>Model (optional)</label><input id="image_m" value="${esc(s.image.model)}" placeholder="default"/><label>API key</label>${key("image_k", !!s.image.key)}</div>
    <div><label>Video (TV commercial motion)</label>${sel("video_p", s.video.provider, [["free", "Free: Wan 2.2 (test quality)"], ["veo", "Google Veo 3.1 (broadcast)"]])}<label>Gemini API key (for Veo)</label>${key("video_k", !!s.video.key)}<label>Hugging Face token (optional: more free GPU)</label>${key("video_hf", !!s.video.hf_token)}</div></div>
    <div class="grid3" style="margin-top:6px"><div><label>Urdu voice</label>${sel("vur", s.voice.voice_ur, [["ur-PK-UzmaNeural", "Uzma (female)"], ["ur-PK-AsadNeural", "Asad (male)"]])}</div><div><label>English voice</label>${sel("ven", s.voice.voice_en, [["en-US-AvaNeural", "Ava (US)"], ["en-US-AndrewNeural", "Andrew (US)"], ["en-GB-SoniaNeural", "Sonia (UK)"], ["en-IN-NeerjaNeural", "Neerja (IN)"]])}</div></div>
    <div class="row" style="margin-top:14px"><button class="p" id="saveS">Save settings</button><button class="s" id="wipe">Delete all local data</button></div></div>`;
  $("#saveS").onclick = () => {
    const keep = (v, old) => v || old;
    E.saveSettings({
      text: { provider: $("#text_p").value, model: $("#text_m").value, key: keep($("#text_k").value, s.text.key) },
      image: { provider: $("#image_p").value, model: $("#image_m").value, key: keep($("#image_k").value, s.image.key) },
      video: { provider: $("#video_p").value, model: s.video.model, key: keep($("#video_k").value, s.video.key), hf_token: keep($("#video_hf").value, s.video.hf_token) },
      voice: { voice_ur: $("#vur").value, voice_en: $("#ven").value },
    });
    $("#saveS").textContent = "Saved ✓";
  };
  $("#wipe").onclick = async () => {
    if (!confirm("Delete every workspace, document and campaign stored in this browser?")) return;
    indexedDB.deleteDatabase("campaign-studio");
    try { localStorage.removeItem("cs-settings"); } catch {}
    location.reload();
  };
}

window.addEventListener("beforeunload", (e) => { if (running.size || indexing.size) { e.preventDefault(); e.returnValue = ""; } });
loadWorkspaces();

/**
 * Media engine (browser): the brand compositor for statics, and the TV commercial
 * finisher — shots on a 1920×1080 canvas at 25 fps with grade, grain, crossfades,
 * title-safe supers and a packshot; VO over a music bed with ducking; recorded
 * in real time with MediaRecorder, plus a 9:16 cutdown recorded alongside.
 */

const FONT = '"Inter", "Segoe UI", Arial, sans-serif';
const FORMATS = { square: [1080, 1080], portrait: [1080, 1350], story: [1080, 1920] };
const rgb = (h) => [1, 3, 5].map((i) => parseInt(h.slice(i, i + 2), 16));
const latin = (t) => String(t || "").replace(/[؀-ۿݐ-ݿﭐ-﷿ﹰ-﻿]+/g, "").replace(/\s+/g, " ").trim();
const toBlob = (cv, type = "image/jpeg", q = 0.92) => new Promise((ok) => cv.toBlob(ok, type, q));

async function bitmap(blob) { return await createImageBitmap(blob); }

/** Crop the top w×h (the free image tier stamps a mark bottom-right) and scale. */
export async function cropStill(blob, w, h) {
  const bm = await bitmap(blob);
  const cv = new OffscreenCanvas(w, h);
  const ctx = cv.getContext("2d");
  const srcH = Math.min(bm.height, Math.round((bm.width * h) / w));
  ctx.drawImage(bm, 0, 0, bm.width, srcH, 0, 0, w, h);
  return await cv.convertToBlob({ type: "image/jpeg", quality: 0.94 });
}

function cover(ctx, img, W, H, zoom = 1, dx = 0, dy = 0) {
  const iw = img.videoWidth || img.width;
  const ih = img.videoHeight || img.height;
  if (!iw || !ih) return;
  const s = Math.max(W / iw, H / ih) * zoom;
  const w = iw * s;
  const h = ih * s;
  ctx.drawImage(img, (W - w) / 2 + dx, (H - h) / 2 + dy, w, h);
}

function wrap(ctx, text, maxW) {
  const words = String(text || "").split(/\s+/).filter(Boolean);
  const lines = [];
  let cur = "";
  for (const w of words) {
    const t = cur ? cur + " " + w : w;
    if (ctx.measureText(t).width <= maxW) cur = t;
    else { if (cur) lines.push(cur); cur = w; }
  }
  if (cur) lines.push(cur);
  return lines;
}

/** One finished static. The footer band follows the artwork: light art → brand band, white type; dark art → white band, brand type. */
export async function compose(sceneBlob, fmt, copyIn, brand, cols, logoBlob) {
  const copy = Object.fromEntries(Object.entries(copyIn).map(([k, v]) => [k, typeof v === "string" ? latin(v) : v]));
  const [primary, accent] = cols;
  const [W, H] = FORMATS[fmt];
  const cv = document.createElement("canvas");
  cv.width = W;
  cv.height = H;
  const ctx = cv.getContext("2d");
  cover(ctx, await bitmap(sceneBlob), W, H);
  const m = W * 0.06;
  const band = H * (fmt === "story" ? 0.08 : 0.11);
  const g = ctx.createLinearGradient(0, 0, 0, H * 0.6);
  const [pr, pg, pb] = rgb(primary);
  g.addColorStop(0, `rgba(${pr},${pg},${pb},0.75)`);
  g.addColorStop(1, `rgba(${pr},${pg},${pb},0)`);
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, W, H * 0.6);
  let y = m;
  if (logoBlob) {
    const lg = await bitmap(logoBlob);
    const lh = W * 0.07;
    ctx.drawImage(lg, m, y, (lg.width * lh) / lg.height, lh);
    y += lh + W * 0.03;
  } else {
    ctx.fillStyle = "rgba(255,255,255,.92)";
    ctx.font = `700 ${W * 0.032}px ${FONT}`;
    ctx.textBaseline = "top";
    ctx.fillText((brand.brand_name || "").toUpperCase(), m, y);
    y += W * 0.06;
  }
  const hs = W * 0.085;
  ctx.textBaseline = "top";
  ctx.fillStyle = "#fff";
  ctx.font = `900 ${hs}px ${FONT}`;
  for (const l of wrap(ctx, copy.headline, W - 2 * m).slice(0, 3)) { ctx.fillText(l, m, y); y += hs * 1.08; }
  y += hs * 0.25;
  ctx.font = `400 ${hs * 0.42}px ${FONT}`;
  ctx.fillStyle = "rgba(255,255,255,.92)";
  for (const l of wrap(ctx, copy.subline, W - 2 * m).slice(0, 3)) { ctx.fillText(l, m, y); y += hs * 0.55; }
  // CTA pill
  ctx.font = `700 ${hs * 0.36}px ${FONT}`;
  const cta = copy.cta || "Learn more";
  const ph = hs * 0.75;
  const py = H - band - ph - m * 0.8;
  const tw = ctx.measureText(cta).width;
  ctx.fillStyle = accent;
  ctx.beginPath();
  ctx.roundRect(m, py, tw + ph, ph, ph / 2);
  ctx.fill();
  ctx.fillStyle = "#fff";
  ctx.textBaseline = "middle";
  ctx.fillText(cta, m + ph / 2, py + ph / 2);
  // footer band follows the artwork's brightness
  const sample = ctx.getImageData(0, H - band * 3, W, band * 2).data;
  let sum = 0;
  for (let i = 0; i < sample.length; i += 16) sum += 0.2126 * sample[i] + 0.7152 * sample[i + 1] + 0.0722 * sample[i + 2];
  const bright = sum / (sample.length / 16);
  ctx.fillStyle = bright > 128 ? primary : "#fff";
  ctx.fillRect(0, H - band, W, band);
  const ink = bright > 128 ? "#fff" : primary;
  ctx.fillStyle = ink;
  ctx.font = `700 ${band * 0.3}px ${FONT}`;
  ctx.textAlign = "left";
  ctx.fillText(brand.brand_name || "", m, H - band / 2);
  if (brand.tagline) {
    ctx.font = `400 ${band * 0.24}px ${FONT}`;
    ctx.textAlign = "right";
    ctx.fillText(latin(brand.tagline), W - m, H - band / 2);
    ctx.textAlign = "left";
  }
  if (copy.legal) {
    ctx.font = `400 ${W * 0.016}px ${FONT}`;
    ctx.fillStyle = "rgba(255,255,255,.8)";
    ctx.textBaseline = "bottom";
    ctx.fillText(copy.legal.slice(0, 140), m, H - band - m * 0.3);
  }
  return await toBlob(cv);
}

/* ── TV commercial ───────────────────────────────────────────────────── */

const W = 1920;
const H = 1080;
const FPS = 25;
const XF = 0.45; // crossfade
const PACK = 3.5; // packshot seconds

function pickMime() {
  const opts = ["video/mp4;codecs=avc1.640028,mp4a.40.2", "video/mp4", "video/webm;codecs=vp9,opus", "video/webm"];
  return opts.find((m) => window.MediaRecorder && MediaRecorder.isTypeSupported(m)) || "video/webm";
}

function grainTile() {
  const c = document.createElement("canvas");
  c.width = c.height = 256;
  const g = c.getContext("2d");
  const d = g.createImageData(256, 256);
  for (let i = 0; i < d.data.length; i += 4) {
    const v = 128 + (Math.random() - 0.5) * 90;
    d.data[i] = d.data[i + 1] = d.data[i + 2] = v;
    d.data[i + 3] = 255;
  }
  g.putImageData(d, 0, 0);
  return c;
}

async function loadVideo(blob) {
  const v = document.createElement("video");
  v.muted = true;
  v.playsInline = true;
  v.preload = "auto";
  v.src = URL.createObjectURL(blob);
  await new Promise((ok, no) => { v.onloadeddata = ok; v.onerror = () => no(new Error("clip decode failed")); });
  return v;
}

function packshot(ctx, brand, cols, logo, endLine, legal, a) {
  const [primary, accent] = cols;
  ctx.save();
  ctx.globalAlpha = a;
  ctx.fillStyle = primary;
  ctx.fillRect(0, 0, W, H);
  const g = ctx.createRadialGradient(W / 2, H * 0.3, 10, W / 2, H * 0.3, W * 0.55);
  g.addColorStop(0, "rgba(255,255,255,.22)");
  g.addColorStop(1, "rgba(255,255,255,0)");
  ctx.fillStyle = g;
  ctx.fillRect(0, 0, W, H);
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  if (logo) {
    const lh = H * 0.16;
    const lw = (logo.width * lh) / logo.height;
    ctx.drawImage(logo, W / 2 - lw / 2, H * 0.36 - lh / 2, lw, lh);
  } else {
    ctx.fillStyle = "#fff";
    ctx.font = `900 96px ${FONT}`;
    ctx.fillText(brand.brand_name || "", W / 2, H * 0.36);
  }
  ctx.fillStyle = accent;
  ctx.fillRect(W / 2 - 90, H * 0.5, 180, 5);
  ctx.fillStyle = "#fff";
  ctx.font = `700 54px ${FONT}`;
  wrap(ctx, latin(endLine || brand.tagline || ""), W * 0.7).slice(0, 2).forEach((l, i) => ctx.fillText(l, W / 2, H * 0.58 + i * 66));
  if (legal) {
    ctx.font = `400 24px ${FONT}`;
    ctx.fillStyle = "rgba(225,232,240,.9)";
    ctx.fillText(latin(legal).slice(0, 160), W / 2, H * 0.9);
  }
  ctx.restore();
}

/**
 * plates: [{ still: Blob, clip: Blob|null, dur, super }]
 * Returns { master: Blob, vertical: Blob, mime }.
 */
export async function renderTVC({ plates, vo, music, brand, cols, logo, endLine, legal, onProgress }) {
  const logoBm = logo ? await bitmap(logo) : null;
  const shots = [];
  for (const p of plates) {
    const still = await bitmap(p.still);
    let video = null;
    if (p.clip) { try { video = await loadVideo(p.clip); } catch {} }
    shots.push({ ...p, stillBm: still, video });
  }
  // timeline
  let t = 0;
  for (const s of shots) { s.start = t; t += s.dur - XF; }
  const packStart = t;
  const total = packStart + PACK;

  // audio graph
  const ac = new AudioContext({ sampleRate: 48000 });
  const dest = ac.createMediaStreamDestination();
  const master = ac.createDynamicsCompressor(); // gentle broadcast-style limiter
  master.threshold.value = -12;
  master.ratio.value = 4;
  master.attack.value = 0.005;
  master.release.value = 0.2;
  const out = ac.createGain();
  out.gain.value = 0.27; // lands near broadcast loudness (about -23 LUFS) for typical VO
  master.connect(out).connect(dest);
  const voBuf = await ac.decodeAudioData(await vo.arrayBuffer());
  // normalise VO to a steady level
  let rms = 0;
  const ch = voBuf.getChannelData(0);
  for (let i = 0; i < ch.length; i += 4) rms += ch[i] * ch[i];
  rms = Math.sqrt(rms / (ch.length / 4)) || 0.1;
  const voGain = ac.createGain();
  voGain.gain.value = Math.min(4, 0.16 / rms);
  const voSrc = ac.createBufferSource();
  voSrc.buffer = voBuf;
  voSrc.connect(voGain).connect(master);
  const musicGain = ac.createGain();
  musicGain.gain.value = 0;
  musicGain.connect(master);
  const startAt = ac.currentTime + 0.3;
  const voStart = startAt + 0.6;
  const voEnd = voStart + Math.min(voBuf.duration, total - 1);
  if (music) {
    const mb = ac.createBufferSource();
    mb.buffer = await ac.decodeAudioData(await music.arrayBuffer());
    mb.connect(musicGain);
    mb.start(startAt);
    mb.stop(startAt + total);
  } else {
    // temp music: a soft pad chord with slow movement (replace with a licensed track)
    const lp = ac.createBiquadFilter();
    lp.type = "lowpass";
    lp.frequency.value = 1800;
    lp.connect(musicGain);
    for (const f of [220, 277.18, 329.63, 440, 164.81]) {
      const o = ac.createOscillator();
      o.type = "triangle";
      o.frequency.value = f;
      const g = ac.createGain();
      g.gain.value = 0.05;
      const lfo = ac.createOscillator();
      lfo.frequency.value = 0.1 + Math.random() * 0.15;
      const lg = ac.createGain();
      lg.gain.value = 0.025;
      lfo.connect(lg).connect(g.gain);
      o.connect(g).connect(lp);
      o.start(startAt);
      lfo.start(startAt);
      o.stop(startAt + total);
      lfo.stop(startAt + total);
    }
  }
  // music: fade in, duck under the VO, swell on the packshot, fade out
  const mg = musicGain.gain;
  mg.setValueAtTime(0, startAt);
  mg.linearRampToValueAtTime(0.5, startAt + 1);
  mg.setTargetAtTime(0.16, voStart - 0.2, 0.15);
  mg.setTargetAtTime(0.5, voEnd + 0.1, 0.4);
  mg.setTargetAtTime(0, startAt + total - 1.2, 0.35);
  voSrc.start(voStart);

  // canvases + recorders
  const cv = document.createElement("canvas");
  cv.width = W;
  cv.height = H;
  const ctx = cv.getContext("2d");
  ctx.imageSmoothingQuality = "high";
  const vc = document.createElement("canvas");
  vc.width = 1080;
  vc.height = 1920;
  const vctx = vc.getContext("2d");
  const small = document.createElement("canvas");
  small.width = 96;
  small.height = 54;
  const sctx = small.getContext("2d");
  const grain = grainTile();
  const vignette = ctx.createRadialGradient(W / 2, H / 2, H * 0.35, W / 2, H / 2, W * 0.75);
  vignette.addColorStop(0, "rgba(0,0,0,0)");
  vignette.addColorStop(1, "rgba(0,0,0,0.38)");

  const mime = pickMime();
  const audioTrack = dest.stream.getAudioTracks()[0];
  // frames are pushed by hand at exactly 25 fps, so static moments (the packshot) are still recorded
  const trackM = cv.captureStream(0).getVideoTracks()[0];
  const trackV = vc.captureStream(0).getVideoTracks()[0];
  const recM = new MediaRecorder(new MediaStream([trackM, audioTrack]), { mimeType: mime, videoBitsPerSecond: 14e6, audioBitsPerSecond: 192e3 });
  const recV = new MediaRecorder(new MediaStream([trackV, audioTrack.clone()]), { mimeType: mime, videoBitsPerSecond: 9e6, audioBitsPerSecond: 192e3 });
  const push = () => { trackM.requestFrame && trackM.requestFrame(); trackV.requestFrame && trackV.requestFrame(); };
  const partsM = [];
  const partsV = [];
  recM.ondataavailable = (e) => e.data.size && partsM.push(e.data);
  recV.ondataavailable = (e) => e.data.size && partsV.push(e.data);
  const doneM = new Promise((ok) => (recM.onstop = ok));
  const doneV = new Promise((ok) => (recV.onstop = ok));

  function drawShot(s, local, alpha) {
    ctx.save();
    ctx.globalAlpha = alpha;
    const k = local / s.dur;
    if (s.video) {
      // slow the clip to fill the shot; hold the last frame if it runs out
      const v = s.video;
      if (v.paused && !v.ended) {
        v.playbackRate = Math.max(0.25, Math.min(4, v.duration / s.dur));
        v.play().catch(() => {});
      }
      const want = Math.min(v.duration - 0.05, (local / s.dur) * v.duration);
      if (Math.abs(v.currentTime - want) > 0.4) v.currentTime = Math.max(0, want);
      cover(ctx, s.video, W, H, 1.02 + 0.03 * k);
    } else {
      cover(ctx, s.stillBm, W, H, 1.0 + 0.08 * k, -20 * k, -8 * k); // camera move
    }
    ctx.restore();
  }

  function frame(time) {
    ctx.filter = "contrast(1.06) saturate(1.07) brightness(0.99)";
    ctx.fillStyle = "#000";
    ctx.fillRect(0, 0, W, H);
    for (const s of shots) {
      const local = time - s.start;
      if (local < 0 || local > s.dur) continue;
      const fadeIn = s.start === 0 ? 1 : Math.min(1, local / XF);
      drawShot(s, local, fadeIn);
    }
    ctx.filter = "none";
    ctx.fillStyle = vignette;
    ctx.fillRect(0, 0, W, H);
    // grain
    ctx.save();
    ctx.globalAlpha = 0.06;
    ctx.globalCompositeOperation = "overlay";
    const ox = (Math.random() * 256) | 0;
    const oy = (Math.random() * 256) | 0;
    for (let x = -ox; x < W; x += 256) for (let y = -oy; y < H; y += 256) ctx.drawImage(grain, x, y);
    ctx.restore();
    // supers: title-safe lower third
    for (const s of shots) {
      if (!s.super) continue;
      const a0 = s.start + 0.5;
      const a1 = s.start + s.dur - XF - 0.2;
      if (time < a0 || time > a1) continue;
      const a = Math.min(1, (time - a0) / 0.3, (a1 - time) / 0.3);
      ctx.save();
      ctx.globalAlpha = Math.max(0, a);
      ctx.font = `700 56px ${FONT}`;
      ctx.textAlign = "center";
      ctx.textBaseline = "middle";
      ctx.shadowColor = "rgba(0,0,0,.6)";
      ctx.shadowBlur = 18;
      ctx.fillStyle = "#fff";
      ctx.fillText(latin(s.super), W / 2, H * 0.82);
      ctx.restore();
    }
    if (time >= packStart) packshot(ctx, brand, cols, logoBm, endLine, legal, Math.min(1, (time - packStart) / XF));
    // 9:16: blurred fill + the 16:9 picture centred
    sctx.drawImage(cv, 0, 0, 96, 54);
    vctx.imageSmoothingEnabled = true;
    vctx.drawImage(small, 0, 0, 96, 54, -540, 0, 2160, 1920);
    vctx.fillStyle = "rgba(0,0,0,.25)";
    vctx.fillRect(0, 0, 1080, 1920);
    vctx.drawImage(cv, 0, 0, W, H, 0, 1920 / 2 - 304, 1080, 608);
  }

  for (const s of shots) if (s.video) { s.video.playbackRate = 1; s.video.pause(); s.video.currentTime = 0; }
  frame(0);
  recM.start(1000);
  recV.start(1000);
  push();
  let lastIdx = 0;
  await new Promise((ok) => {
    const tick = () => {
      const time = ac.currentTime - startAt;
      if (time >= total + 0.1) return ok();
      const idx = Math.floor(Math.max(0, time) * FPS);
      if (idx > lastIdx) {
        lastIdx = idx;
        frame(Math.min(time, total));
        push();
      }
      onProgress && onProgress(Math.max(0, Math.min(1, time / total)));
      requestAnimationFrame(tick);
    };
    requestAnimationFrame(tick);
  });
  recM.stop();
  recV.stop();
  await Promise.all([doneM, doneV]);
  ac.close();
  for (const s of shots) if (s.video) URL.revokeObjectURL(s.video.src);
  return { master: new Blob(partsM, { type: mime.split(";")[0] }), vertical: new Blob(partsV, { type: mime.split(";")[0] }), mime: mime.split(";")[0] };
}

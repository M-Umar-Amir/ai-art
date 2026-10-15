/**
 * Dallas Dash — 2D artwork for the 3D world: KFC posters, packaging prints,
 * shop signs, murals, flags, highway signs and the skyline band.
 *
 * Everything is drawn once at load into textures (one canvas each, or one
 * atlas for the many small signs), never per frame. Porting note (Expo): swap
 * makeCanvas() for an offscreen Skia surface or bake these to image files with
 * the same drawing code.
 */

import * as THREE from "three";

export function makeCanvas(w, h) {
  const c = document.createElement("canvas");
  c.width = w;
  c.height = h;
  return c;
}

export function toTexture(canvas, { repeat = false, srgb = true } = {}) {
  const t = new THREE.CanvasTexture(canvas);
  if (srgb) t.colorSpace = THREE.SRGBColorSpace;
  t.anisotropy = 4;
  if (repeat) t.wrapS = t.wrapT = THREE.RepeatWrapping;
  t.needsUpdate = true;
  return t;
}

const FONT = "Oswald, Impact, 'Arial Narrow', sans-serif";

/** Text squeezed to fit maxW. */
export function fitText(ctx, text, x, y, size, maxW, fill, stroke, weight = "italic 900") {
  ctx.font = `${weight} ${size}px ${FONT}`;
  ctx.textAlign = "center";
  ctx.textBaseline = "middle";
  const w = ctx.measureText(text).width;
  ctx.save();
  ctx.translate(x, y);
  if (w > maxW) ctx.scale(maxW / w, 1);
  if (stroke) {
    ctx.lineWidth = size * 0.14;
    ctx.strokeStyle = stroke;
    ctx.lineJoin = "round";
    ctx.strokeText(text, 0, 0);
  }
  ctx.fillStyle = fill;
  ctx.fillText(text, 0, 0);
  ctx.restore();
}

function star(ctx, x, y, r) {
  ctx.beginPath();
  for (let i = 0; i < 10; i += 1) {
    const a = -Math.PI / 2 + (i * Math.PI) / 5;
    const rr = i % 2 ? r * 0.42 : r;
    ctx.lineTo(x + Math.cos(a) * rr, y + Math.sin(a) * rr);
  }
  ctx.closePath();
}

/** A soft grain so flat print areas read as paper/vinyl, not CG fills. */
function grain(ctx, w, h, a = 0.05) {
  const img = ctx.getImageData(0, 0, w, h);
  const d = img.data;
  let s = 1234567;
  for (let i = 0; i < d.length; i += 4) {
    s = (s * 1103515245 + 12345) & 0x7fffffff;
    const n = ((s >> 16) / 32768 - 0.5) * 255 * a;
    d[i] += n;
    d[i + 1] += n;
    d[i + 2] += n;
  }
  ctx.putImageData(img, 0, 0);
}

/* ── KFC artwork ───────────────────────────────────────────────────── */

export const AD_LINES = ["FINGER LICKIN' GOOD", "ORIGINAL RECIPE", "GRAB A BUCKET", "FAMOUS BOWLS"];

function drawBucket(ctx, cx, top, w, h) {
  // chicken pieces
  ctx.fillStyle = "#b8641f";
  for (const [dx, dy, r] of [[-0.3, -0.05, 0.3], [0.05, -0.16, 0.34], [0.34, -0.02, 0.28], [-0.05, 0.02, 0.3]]) {
    const g = ctx.createRadialGradient(cx + dx * w - r * w * 0.3, top + dy * w - r * w * 0.3, 2, cx + dx * w, top + dy * w, r * w);
    g.addColorStop(0, "#f0a54a");
    g.addColorStop(0.7, "#c8742a");
    g.addColorStop(1, "#8a4612");
    ctx.fillStyle = g;
    ctx.beginPath();
    ctx.arc(cx + dx * w, top + dy * w, r * w, 0, Math.PI * 2);
    ctx.fill();
  }
  // the bucket
  ctx.save();
  ctx.beginPath();
  ctx.moveTo(cx - w / 2, top);
  ctx.lineTo(cx + w / 2, top);
  ctx.lineTo(cx + w * 0.38, top + h);
  ctx.lineTo(cx - w * 0.38, top + h);
  ctx.closePath();
  ctx.fillStyle = "#fff";
  ctx.fill();
  ctx.clip();
  ctx.fillStyle = "#e4002b";
  for (let k = -3; k <= 3; k += 2) ctx.fillRect(cx + (k * w) / 7 - w / 14, top, w / 7, h);
  const sh = ctx.createLinearGradient(cx - w / 2, 0, cx + w / 2, 0);
  sh.addColorStop(0, "rgba(0,0,0,0.25)");
  sh.addColorStop(0.35, "rgba(255,255,255,0.15)");
  sh.addColorStop(1, "rgba(0,0,0,0.3)");
  ctx.fillStyle = sh;
  ctx.fillRect(cx - w / 2, top, w, h);
  ctx.restore();
}

/** A KFC billboard poster, `w`×`h` pixels. */
export function paintAd(logo, text, w = 1024, h = 400) {
  const c = makeCanvas(w, h);
  const ctx = c.getContext("2d");
  const bg = ctx.createLinearGradient(0, 0, 0, h);
  bg.addColorStop(0, "#f0102f");
  bg.addColorStop(1, "#b5001f");
  ctx.fillStyle = bg;
  ctx.fillRect(0, 0, w, h);
  ctx.fillStyle = "#fff";
  for (let k = 0; k < 3; k += 1) ctx.fillRect(h * 0.06 + k * h * 0.14, 0, h * 0.07, h);
  const r = h * 0.36;
  const lx = h * 0.62 + r;
  ctx.save();
  ctx.beginPath();
  ctx.arc(lx, h / 2, r, 0, Math.PI * 2);
  ctx.fillStyle = "#fff";
  ctx.fill();
  ctx.clip();
  if (logo) ctx.drawImage(logo, lx - r, h / 2 - r, r * 2, r * 2);
  ctx.restore();
  const bx = w - h * 0.42;
  drawBucket(ctx, bx, h * 0.38, h * 0.5, h * 0.5);
  const left = lx + r + h * 0.08;
  const right = bx - h * 0.32;
  const words = text.split(" ");
  const mid = Math.ceil(words.length / 2);
  const lines = words.length > 1 ? [words.slice(0, mid).join(" "), words.slice(mid).join(" ")] : [text];
  lines.forEach((ln, i) => fitText(ctx, ln, (left + right) / 2, lines.length === 1 ? h / 2 : h * (0.36 + i * 0.3), h * 0.26, right - left, "#fff", null));
  grain(ctx, w, h, 0.04);
  return c;
}

/** The KFC wordmark on red, for restaurant fascias. */
export function paintWordmark(w = 512, h = 160) {
  const c = makeCanvas(w, h);
  const ctx = c.getContext("2d");
  ctx.fillStyle = "#e4002b";
  ctx.fillRect(0, 0, w, h);
  fitText(ctx, "KFC", w / 2, h * 0.54, h * 0.82, w * 0.6, "#fff", null);
  return c;
}

/** Red-and-white bucket stripes (packaging, awnings, the restaurant band). */
export function paintStripes(n = 8, w = 512, h = 256, logo = null) {
  const c = makeCanvas(w, h);
  const ctx = c.getContext("2d");
  for (let i = 0; i < n; i += 1) {
    ctx.fillStyle = i % 2 ? "#ffffff" : "#e4002b";
    ctx.fillRect((i * w) / n, 0, w / n + 1, h);
  }
  if (logo) {
    const r = h * 0.3;
    ctx.save();
    ctx.beginPath();
    ctx.arc(w / 2, h * 0.5, r, 0, Math.PI * 2);
    ctx.fillStyle = "#fff";
    ctx.fill();
    ctx.clip();
    ctx.drawImage(logo, w / 2 - r, h * 0.5 - r, r * 2, r * 2);
    ctx.restore();
  }
  grain(ctx, w, h, 0.03);
  return c;
}

/** The Colonel roundel alone, transparent around it (crew patches, signs). */
export function paintRoundel(logo, s = 256) {
  const c = makeCanvas(s, s);
  const ctx = c.getContext("2d");
  ctx.beginPath();
  ctx.arc(s / 2, s / 2, s / 2 - 2, 0, Math.PI * 2);
  ctx.fillStyle = "#fff";
  ctx.fill();
  ctx.clip();
  if (logo) ctx.drawImage(logo, 0, 0, s, s);
  return c;
}

/* ── street signage: one atlas of cells, chosen by UV per placement ───── */

export const ATLAS_COLS = 8;
export const ATLAS_ROWS = 16;

const MURAL_BG = ["#1b6fd1", "#f2a20c", "#16a37f", "#7a3fc4", "#e4002b", "#0b9bb5"];
const SHOP_BG = ["#1f6b5c", "#8a2c2c", "#2c4f8a", "#6b4a1f", "#4a2c6b", "#1f5a8a", "#8a5a1f"];

/**
 * Paints every sign the routes need into one texture. Returns the canvas and
 * a lookup from key ("mural:TEXT", "shop:TEXT", "ghost:TEXT", "deco:TEXT",
 * "highway:TEXT:SHIELD:ontario", "flag:texas" …) to its cell rectangle in UV.
 */
export function paintSignAtlas(entries) {
  const cw = 256;
  const ch = 128;
  const c = makeCanvas(cw * ATLAS_COLS, ch * ATLAS_ROWS);
  const ctx = c.getContext("2d");
  const map = new Map();
  entries.forEach((key, i) => {
    if (i >= ATLAS_COLS * ATLAS_ROWS) return;
    const x = (i % ATLAS_COLS) * cw;
    const y = Math.floor(i / ATLAS_COLS) * ch;
    ctx.save();
    ctx.translate(x, y);
    ctx.beginPath();
    ctx.rect(0, 0, cw, ch);
    ctx.clip();
    drawSign(ctx, key, cw, ch, i);
    ctx.restore();
    map.set(key, { u0: x / c.width, v0: 1 - (y + ch) / c.height, u1: (x + cw) / c.width, v1: 1 - y / c.height });
  });
  grain(ctx, c.width, c.height, 0.035);
  return { canvas: c, map };
}

function drawSign(ctx, key, w, h, i) {
  const [kind, text, extra, extra2] = key.split(":");
  if (kind === "mural") {
    ctx.fillStyle = MURAL_BG[i % MURAL_BG.length];
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = "rgba(255,255,255,0.16)";
    for (let k = 0; k < 12; k += 2) {
      const a0 = (k / 12) * Math.PI * 2;
      const a1 = ((k + 1) / 12) * Math.PI * 2;
      ctx.beginPath();
      ctx.moveTo(w / 2, h / 2);
      ctx.lineTo(w / 2 + Math.cos(a0) * w, h / 2 + Math.sin(a0) * w);
      ctx.lineTo(w / 2 + Math.cos(a1) * w, h / 2 + Math.sin(a1) * w);
      ctx.fill();
    }
    star(ctx, w * 0.12, h * 0.26, h * 0.14);
    ctx.fillStyle = "#fff";
    ctx.fill();
    fitText(ctx, text, w / 2, h * 0.56, h * 0.5, w * 0.86, "#ffd84a", "#202124");
  } else if (kind === "shop") {
    ctx.fillStyle = "#1a1b1e";
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = SHOP_BG[i % SHOP_BG.length];
    ctx.fillRect(6, 10, w - 12, h - 20);
    ctx.strokeStyle = "#e3c27a";
    ctx.lineWidth = 3;
    ctx.strokeRect(12, 16, w - 24, h - 32);
    fitText(ctx, text, w / 2, h / 2 + 2, h * 0.48, w * 0.82, "#f6efe0", null, "800");
  } else if (kind === "ghost") {
    ctx.fillStyle = "rgba(0,0,0,0)";
    ctx.clearRect(0, 0, w, h);
    fitText(ctx, text, w / 2, h / 2, h * 0.5, w * 0.92, "rgba(250,244,226,0.82)", null, "800");
  } else if (kind === "deco") {
    ctx.fillStyle = "#14524f";
    ctx.fillRect(0, 0, w, h);
    ctx.strokeStyle = "#e3b04b";
    ctx.lineWidth = 6;
    ctx.strokeRect(8, 12, w - 16, h - 24);
    fitText(ctx, text, w / 2, h / 2, h * 0.42, w * 0.84, "#f2cf6a", null, "800");
  } else if (kind === "marquee") {
    ctx.fillStyle = "#1a1b1e";
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = extra || "#c8102e";
    ctx.fillRect(4, 8, w - 8, h - 16);
    ctx.fillStyle = "#fff3c4";
    for (let k = 0; k < 16; k += 1) {
      ctx.beginPath();
      ctx.arc(10 + k * ((w - 20) / 15), 13, 3, 0, Math.PI * 2);
      ctx.arc(10 + k * ((w - 20) / 15), h - 13, 3, 0, Math.PI * 2);
      ctx.fill();
    }
    fitText(ctx, text, w / 2, h / 2, h * 0.46, w * 0.84, "#fff3c4", "#202124");
  } else if (kind === "highway") {
    ctx.fillStyle = "#0b6b3a";
    ctx.fillRect(0, 0, w, h);
    ctx.strokeStyle = "#fff";
    ctx.lineWidth = 4;
    ctx.strokeRect(5, 5, w - 10, h - 10);
    const ontario = extra2 === "on";
    ctx.fillStyle = "#fff";
    ctx.beginPath();
    ctx.moveTo(18, 22);
    ctx.lineTo(70, 22);
    ctx.quadraticCurveTo(72, 76, 44, 98);
    ctx.quadraticCurveTo(16, 76, 18, 22);
    ctx.fill();
    if (!ontario) {
      ctx.fillStyle = "#1b3f94";
      ctx.fillRect(22, 38, 44, 34);
      ctx.fillStyle = "#c8102e";
      ctx.fillRect(22, 26, 44, 11);
      fitText(ctx, extra, 44, 56, 26, 40, "#fff", null, "800");
    } else {
      ctx.strokeStyle = "#202124";
      ctx.lineWidth = 2;
      ctx.stroke();
      fitText(ctx, extra, 44, 60, 26, 44, "#202124", null, "800");
    }
    fitText(ctx, ontario ? "EAST" : "NORTH", 160, 32, 24, 150, "#fff", null, "800");
    fitText(ctx, text, 160, 82, 36, 170, "#fff", null, "800");
  } else if (kind === "flag") {
    if (text === "canada") {
      ctx.fillStyle = "#fff";
      ctx.fillRect(0, 0, w, h);
      ctx.fillStyle = "#d52b1e";
      ctx.fillRect(0, 0, w / 4, h);
      ctx.fillRect((w * 3) / 4, 0, w / 4, h);
      const s = h / 100;
      const leaf = [[50, 12], [56, 28], [63, 24], [61, 44], [72, 36], [75, 43], [85, 41], [79, 52], [83, 55], [66, 65], [68, 72], [53, 70], [53, 88], [47, 88], [47, 70], [32, 72], [34, 65], [17, 55], [21, 52], [15, 41], [25, 43], [28, 36], [39, 44], [37, 24], [44, 28]];
      ctx.beginPath();
      for (const [x, y] of leaf) ctx.lineTo(w / 2 + (x - 50) * s, y * s);
      ctx.closePath();
      ctx.fill();
    } else {
      ctx.fillStyle = "#fff";
      ctx.fillRect(0, 0, w, h / 2);
      ctx.fillStyle = "#bf0a30";
      ctx.fillRect(0, h / 2, w, h / 2);
      ctx.fillStyle = "#002868";
      ctx.fillRect(0, 0, w / 3, h);
      star(ctx, w / 6, h / 2, h * 0.22);
      ctx.fillStyle = "#fff";
      ctx.fill();
    }
  } else if (kind === "clearance") {
    ctx.fillStyle = "#f6c428";
    ctx.fillRect(0, 0, w, h);
    ctx.strokeStyle = "#202124";
    ctx.lineWidth = 6;
    ctx.strokeRect(8, 10, w - 16, h - 20);
    fitText(ctx, "LOW CLEARANCE", w / 2, h / 2, h * 0.36, w * 0.86, "#202124", null, "800");
  } else if (kind === "toronto") {
    const cols = ["#e4002b", "#1b6fd1", "#16a37f", "#f2a20c", "#7a3fc4", "#0b9bb5", "#e4002b"];
    for (let k = 0; k < 7; k += 1) {
      ctx.fillStyle = cols[k];
      ctx.fillRect((k * w) / 7 + 1, 6, w / 7 - 2, h - 12);
      fitText(ctx, "TORONTO"[k], (k + 0.5) * (w / 7), h / 2, h * 0.7, w / 7 - 6, "#fff", null, "900");
    }
  }
}

/* ── the distant skyline band (a texture on a far cylinder) ───────────── */

export function paintSkyline(kind, w = 2048, h = 256) {
  const c = makeCanvas(w, h);
  const ctx = c.getContext("2d");
  let s = kind === "toronto" ? 99 : 7;
  const rnd = () => {
    s = (s * 16807) % 2147483647;
    return s / 2147483647;
  };
  const far = kind === "toronto" ? "rgba(120,134,168,0.9)" : "rgba(134,128,172,0.9)";
  const near = kind === "toronto" ? "rgb(96,110,146)" : "rgb(110,104,152)";
  for (const [col, hmax, wmin] of [[far, 0.55, 24], [near, 0.8, 30]]) {
    ctx.fillStyle = col;
    let x = 0;
    while (x < w) {
      const bw = wmin + rnd() * 40;
      const bh = h * (0.12 + rnd() * hmax);
      ctx.fillRect(x, h - bh, bw, bh);
      // lit windows
      ctx.fillStyle = "rgba(255,226,170,0.35)";
      for (let wy = h - bh + 6; wy < h - 4; wy += 8) for (let wx = x + 4; wx < x + bw - 4; wx += 7) if (rnd() > 0.7) ctx.fillRect(wx, wy, 3, 3);
      ctx.fillStyle = col;
      x += bw + 2 + rnd() * 8;
    }
  }
  ctx.fillStyle = near;
  const cx = w * 0.32;
  if (kind === "toronto") {
    ctx.beginPath();
    ctx.moveTo(cx - 10, h);
    ctx.lineTo(cx - 3, h - 210);
    ctx.lineTo(cx + 3, h - 210);
    ctx.lineTo(cx + 10, h);
    ctx.fill();
    ctx.beginPath();
    ctx.ellipse(cx, h - 200, 17, 8, 0, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillRect(cx - 2, h - 252, 4, 50);
    ctx.beginPath();
    ctx.ellipse(cx + 90, h, 60, 34, 0, Math.PI, Math.PI * 2);
    ctx.fill();
  } else {
    ctx.fillRect(cx - 4, h - 160, 8, 160);
    ctx.beginPath();
    ctx.arc(cx, h - 170, 18, 0, Math.PI * 2);
    ctx.fill();
    ctx.fillStyle = "rgba(255,236,200,0.8)";
    for (let k = -2; k <= 2; k += 1) {
      ctx.beginPath();
      ctx.arc(cx + k * 6, h - 170 + Math.abs(k) * 1.5, 1.8, 0, Math.PI * 2);
      ctx.fill();
    }
    ctx.fillStyle = near;
    ctx.fillRect(cx + 120, h - 190, 30, 190);
    ctx.strokeStyle = "rgb(96,220,130)";
    ctx.lineWidth = 2;
    ctx.strokeRect(cx + 121, h - 189, 28, 189);
  }
  return c;
}

/** Fried-crust colour + bump, tileable: the drumsticks and chicken pieces. */
export function paintCrust(s = 256) {
  const c = makeCanvas(s, s);
  const ctx = c.getContext("2d");
  ctx.fillStyle = "#c27a2c";
  ctx.fillRect(0, 0, s, s);
  let seed = 42;
  const r = () => {
    seed = (seed * 16807) % 2147483647;
    return seed / 2147483647;
  };
  for (let i = 0; i < 900; i += 1) {
    const x = r() * s;
    const y = r() * s;
    const rr = 2 + r() * 7;
    const g = ctx.createRadialGradient(x - rr * 0.3, y - rr * 0.3, 0, x, y, rr);
    const light = r();
    g.addColorStop(0, light > 0.5 ? "rgba(246,190,108,0.9)" : "rgba(214,140,60,0.9)");
    g.addColorStop(1, "rgba(120,60,16,0)");
    ctx.fillStyle = g;
    for (const dx of [-s, 0, s]) for (const dy of [-s, 0, s]) {
      ctx.beginPath();
      ctx.arc(x + dx, y + dy, rr, 0, Math.PI * 2);
      ctx.fill();
    }
  }
  return c;
}

/** A cheap knit/fabric normal-ish texture for clothing (used as bump). */
export function paintKnit(s = 128) {
  const c = makeCanvas(s, s);
  const ctx = c.getContext("2d");
  ctx.fillStyle = "#808080";
  ctx.fillRect(0, 0, s, s);
  for (let y = 0; y < s; y += 4) {
    for (let x = 0; x < s; x += 4) {
      const v = 110 + ((x * 7 + y * 13) % 40);
      ctx.fillStyle = `rgb(${v},${v},${v})`;
      ctx.fillRect(x, y, 3, 3);
    }
  }
  return c;
}

/** Orange/white reflective bands for traffic drums and barriers. */
export function paintHazard(kind = "drum", w = 256, h = 256) {
  const c = makeCanvas(w, h);
  const ctx = c.getContext("2d");
  if (kind === "chevron") {
    ctx.fillStyle = "#f6c428";
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = "#202124";
    for (let x = -h; x < w + h; x += 64) {
      ctx.beginPath();
      ctx.moveTo(x, 0);
      ctx.lineTo(x + 32, 0);
      ctx.lineTo(x + 32 + h, h);
      ctx.lineTo(x + h, h);
      ctx.fill();
    }
  } else {
    ctx.fillStyle = "#f2661e";
    ctx.fillRect(0, 0, w, h);
    ctx.fillStyle = "#f4f4f2";
    for (const [y0, y1] of [[0.18, 0.3], [0.48, 0.6]]) ctx.fillRect(0, y0 * h, w, (y1 - y0) * h);
  }
  grain(ctx, w, h, 0.06);
  return c;
}

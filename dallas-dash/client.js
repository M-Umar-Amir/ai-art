/**
 * Dallas Dash — the screens, the input and the wire.
 *
 * The rules live in rules.js and the course in sim.js. This file runs the
 * course locally for the player, records every input against a tick number,
 * and submits that log for verification — so a run's points are decided by
 * the rules replaying it, never by this code.
 */

import { createRenderer, districtAt } from "./scene.js";
import { audio } from "./audio.js";
import * as SIM from "./sim.js";
import { routeById, ROUTES } from "./routes.js";
import { HERO_ART } from "./landmarks.js";

const ITEM_LABEL = {
  bucket: "Original Recipe Bucket",
  bowl: "Famous Bowl",
  sandwich: "KFC Chicken Sandwich",
  popcorn: "Popcorn Nuggets",
  fries: "Secret Recipe Fries",
  biscuit: "Biscuit",
  coleslaw: "Coleslaw",
  drink: "Drink",
};
const POWER = {
  magnet: { name: "Drumstick magnet", glyph: "🧲", color: "#e4002b" },
  jetpack: { name: "Jetpack", glyph: "🚀", color: "#ff961e" },
  sneakers: { name: "Spring kicks", glyph: "👟", color: "#3ca0f0" },
  double: { name: "2× score", glyph: "2×", color: "#e0a100" },
};
// the crew cards show the character sheets
const ROUTE_AV = { downtown: "🏙️", east: "🎡", west: "🌉", toronto: "🍁" };
// route cards show a landmark illustration where there is one
const ROUTE_ART = { downtown: HERO_ART.reunion, west: HERO_ART.archbridge, toronto: HERO_ART.cntower };
const crewImg = (id) => `<img src="${(globalThis.DD_ASSETS && globalThis.DD_ASSETS[`art/crew-${id}.webp`]) || `assets/art/crew-${id}.webp`}" alt="" loading="lazy" />`;
const routeAv = (id) => (ROUTE_ART[id] ? `<img src="${ROUTE_ART[id]}" alt="" loading="lazy" />` : ROUTE_AV[id]);
const icon = (id, cls = "ico") => `<svg class="${cls}"><use href="#${id}" /></svg>`;
let preview = null; // the runner shown on the home screen (may be locked)
// player-facing names for the board and the revive token (sim ids stay "board"/"keys")
const BOARD = "Glide board";
const TOKEN = "recipe card";

const q = (id) => document.getElementById(id);
const show = (el) => el.classList.remove("hidden");
const hide = (el) => el.classList.add("hidden");
// one shared formatter: toLocaleString builds a new one per call, and the HUD formats every frame
const NUM = new Intl.NumberFormat("en-US");
const fmt = (n) => NUM.format(Number(n || 0));
const STEP_MS = 1000 / 60;

const canvas = q("view");
// phones: no backdrop blur over the live 3D view (the WebView re-blurs it every frame)
if (window.DASH_APP || (window.matchMedia && window.matchMedia("(pointer: coarse)").matches)) document.documentElement.classList.add("lite");
const renderer = createRenderer(canvas);
/* ── settings: graphics level and sound, remembered on this device ─────── */

const SETTINGS_KEY = "dash:settings";
const QUALITY_NAME = { low: "Low", medium: "Medium", high: "High" };
let settings = { quality: null, sound: true }; // quality null = the device's recommendation
try {
  settings = { ...settings, ...JSON.parse(localStorage.getItem(SETTINGS_KEY) || "{}") };
} catch {
  /* private mode or a bad value: defaults */
}
function saveSettings() {
  try {
    localStorage.setItem(SETTINGS_KEY, JSON.stringify(settings));
  } catch {
    /* not saved; still applies for this session */
  }
}
renderer.setQuality(settings.quality || renderer.recommended, !settings.quality);
audio.setMuted(!settings.sound);

if (new URLSearchParams(location.search).has("debug")) {
  // QA scripts
  window.__renderer = renderer;
  window.__showSpot = (lm) => showSpot(lm);
}

const TIPS = [
  "Swipe up to jump, down to roll — roll in mid-air to dive.",
  "Run up a container's ramp and along the roof.",
  "Double-tap for a glide board: it takes one crash for you.",
  "Every run is replayed and verified before its points count.",
  "Finish a mission set to raise your score multiplier.",
  "Spot all 40 landmarks across Dallas and Toronto.",
];
let tipAt = 0;
function tickLoader() {
  const el = q("loader");
  if (!el || el.classList.contains("done")) return;
  q("vLoadBar").style.width = `${Math.max(3, Math.round(renderer.progress * 100))}%`;
  if (renderer.error) q("vLoadTip").textContent = "Couldn't load the 3D city — check your connection and reload.";
  else if (performance.now() - tipAt > 2600) {
    tipAt = performance.now();
    q("vLoadTip").textContent = TIPS[Math.floor(Math.random() * TIPS.length)];
  }
  if (renderer.ready && view) el.classList.add("done");
}

/* ── net (the server is authoritative; the static build runs it locally) ── */

function playerId() {
  const key = "dash:player";
  try {
    let id = localStorage.getItem(key);
    if (!id) {
      id = Math.random().toString(36).slice(2, 10);
      localStorage.setItem(key, id);
    }
    return id;
  } catch {
    return Math.random().toString(36).slice(2, 10);
  }
}

const ME = playerId();
const room = new URLSearchParams(location.search).get("room") || `dash-${ME}`;
let socket = null;
let localRoom = null;
let retry = 0;
let connected = false;

function connect() {
  const proto = location.protocol === "https:" ? "wss:" : "ws:";
  socket = new WebSocket(`${proto}//${location.host}/ws/${encodeURIComponent(room)}`);
  socket.addEventListener("open", () => {
    retry = 0;
    connected = true;
    send({ type: "join", playerId: ME });
  });
  socket.addEventListener("message", (event) => {
    let msg;
    try {
      msg = JSON.parse(event.data);
    } catch {
      return;
    }
    if (msg.type === "state") onState(msg);
    else if (msg.type === "error") onError(msg.error);
  });
  socket.addEventListener("close", () => {
    connected = false;
    retry = Math.min(retry + 1, 6);
    setTimeout(connect, 500 * 2 ** (retry - 1));
  });
}

function send(msg) {
  if (localRoom) {
    localRoom.send(msg);
    return;
  }
  if (socket && socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(msg));
}

/* ── state ─────────────────────────────────────────────────────────────── */

let view = null;
let run = null;
let idle = null;
let inputs = [];
let pending = [];
let running = false;
let paused = false;
let awaiting = false;
let claimPending = false;
let countdown = 0;
let acc = 0;
let last = performance.now();
let frames = 0;
let fpsAt = performance.now();
let fps = 0;
let stats = { polys: 0, ms: 0 };
let showStats = new URLSearchParams(location.search).has("stats");
let reviveTimer = 0;
let reviveShown = false;
let openSheet = null;
let hudCoins = 0;
let hudDistrict = "";
let hudSightings = 0;
let bestBefore = 0;
let runRoute = "downtown";

const myRoute = () => (view && view.route) || "downtown";

function onState(msg) {
  const before = view;
  view = msg.view;
  if (before && view && view.outfits.length > before.outfits.length) {
    toast("New crew member unlocked!");
    audio.coupon();
  }
  if (awaiting && view && view.lastRun && (!before || before.runs !== view.runs)) {
    awaiting = false;
    paintVerified(view.lastRun);
  }
  if (claimPending && view && before && view.coupons.length > before.coupons.length) {
    claimPending = false;
    const c = view.coupons[view.coupons.length - 1];
    q("vCode").textContent = c.code;
    q("vRewardTerms").textContent = `${c.percent}% off · minimum order $${c.minOrder} · up to $${c.maxOff} off · valid ${c.validDays} days · one use, tied to your account.`;
    closeSheets();
    showSheet(q("reward"));
    audio.coupon();
  }
  paintHome();
  if (openSheet) paintSheet(openSheet);
}

function onError(err) {
  if (awaiting) {
    awaiting = false;
    q("vVerify").textContent = `not credited: ${err}`;
    q("vVerify").className = "verify bad";
  } else {
    toast(err);
  }
}

/* ── input: swipes, keys, gamepad ──────────────────────────────────────── */

const KEYS = {
  ArrowLeft: 1,
  KeyA: 1,
  ArrowRight: 2,
  KeyD: 2,
  ArrowUp: 3,
  KeyW: 3,
  Space: 3,
  ArrowDown: 4,
  KeyS: 4,
  ShiftLeft: 5,
  ShiftRight: 5,
  KeyB: 5,
};

function press(code) {
  audio.unlock();
  if (!running || paused || !run || run.downed) return;
  pending.push(code);
}

window.addEventListener(
  "keydown",
  (event) => {
    if (event.code === "KeyF" && event.shiftKey) {
      showStats = !showStats;
      q("stats").classList.toggle("hidden", !showStats);
      return;
    }
    if (run && run.downed && reviveShown) {
      if (event.code === "Enter" || event.code === "Space") decide(true);
      if (event.code === "Escape") decide(false);
      event.preventDefault();
      return;
    }
    if ((event.code === "Escape" || event.code === "KeyP") && running) {
      event.preventDefault();
      paused ? resume() : pause();
      return;
    }
    const onHome = !q("home").classList.contains("hidden") && !openSheet;
    const onResults = !q("results").classList.contains("hidden");
    if ((event.code === "Enter" || event.code === "Space") && !running && !countdown && (onHome || onResults)) {
      event.preventDefault();
      startRun();
      return;
    }
    const code = KEYS[event.code];
    if (code && running) {
      event.preventDefault();
      if (!event.repeat) press(code);
    }
  },
  { passive: false },
);

// Swipes fire as soon as the finger has moved far enough — not on release —
// which is what makes a runner feel responsive. A quick double-tap = board.
let touch = null;
let lastTap = 0;
canvas.addEventListener("pointerdown", (event) => {
  touch = { x: event.clientX, y: event.clientY, fired: false, t: performance.now() };
});
canvas.addEventListener("pointermove", (event) => {
  if (!touch || touch.fired) return;
  const dx = event.clientX - touch.x;
  const dy = event.clientY - touch.y;
  if (Math.max(Math.abs(dx), Math.abs(dy)) < 22) return;
  touch.fired = true;
  if (Math.abs(dx) > Math.abs(dy)) press(dx > 0 ? 2 : 1);
  else press(dy > 0 ? 4 : 3);
});
canvas.addEventListener("pointerup", () => {
  if (touch && !touch.fired && performance.now() - touch.t < 250) {
    const now = performance.now();
    if (now - lastTap < 300) {
      press(5);
      lastTap = 0;
    } else lastTap = now;
  }
  touch = null;
});
canvas.addEventListener("pointercancel", () => {
  touch = null;
});

const padPrev = {};
function pollGamepad() {
  if (!navigator.getGamepads) return;
  for (const pad of navigator.getGamepads()) {
    if (!pad) continue;
    const map = { 0: 3, 1: 4, 2: 5, 12: 3, 13: 4, 14: 1, 15: 2 };
    for (const index of Object.keys(map)) {
      const btn = pad.buttons[index];
      const down = Boolean(btn && btn.pressed);
      const key = `${pad.index}:${index}`;
      if (down && !padPrev[key]) press(map[index]);
      padPrev[key] = down;
    }
  }
}

/* ── feedback ──────────────────────────────────────────────────────────── */

function toast(text) {
  const el = q("toast");
  el.textContent = text;
  show(el);
  el.classList.remove("pop");
  void el.offsetWidth;
  el.classList.add("pop");
  clearTimeout(toast._t);
  toast._t = setTimeout(() => hide(el), 900);
}

function jolt(flash) {
  canvas.classList.remove("shake");
  void canvas.offsetWidth;
  canvas.classList.add("shake");
  if (flash) {
    const f = q("flash");
    f.classList.remove("go");
    void f.offsetWidth;
    f.classList.add("go");
  }
  if (navigator.vibrate) navigator.vibrate(flash ? 120 : 40);
}

/** Restart a CSS animation without forcing a synchronous layout (costly mid-run on phones). */
function flashClass(el, cls) {
  el.classList.remove(cls);
  requestAnimationFrame(() => el.classList.add(cls));
}

function showDistrict(name) {
  q("vDistrict").textContent = name;
  const el = q("district");
  el.classList.add("on");
  clearTimeout(showDistrict._t);
  showDistrict._t = setTimeout(() => el.classList.remove("on"), 2400);
}

function showSpot(lm) {
  q("vSpot").textContent = lm.name;
  const art = HERO_ART[lm.model];
  q("spotArt").innerHTML = art ? `<img src="${art}" alt="" />` : '<svg><use href="#i-pin" /></svg>';
  q("spot").classList.toggle("art", Boolean(art));
  const el = q("spot");
  el.classList.add("on");
  clearTimeout(showSpot._t);
  showSpot._t = setTimeout(() => el.classList.remove("on"), 2600);
}

const reduceMotion = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

function confetti() {
  if (reduceMotion) return;
  const colors = ["#e4002b", "#ffc43c", "#ffffff", "#0b6b3a", "#1b6fd1"];
  for (let i = 0; i < 70; i += 1) {
    const c = document.createElement("i");
    c.className = "confetti";
    c.style.left = `${Math.random() * 100}vw`;
    c.style.background = colors[i % colors.length];
    c.style.setProperty("--dx", `${(Math.random() - 0.5) * 30}vw`);
    c.style.setProperty("--rot", `${(Math.random() - 0.5) * 1080}deg`);
    c.style.animationDuration = `${1.6 + Math.random() * 1.4}s`;
    c.style.animationDelay = `${Math.random() * 0.4}s`;
    document.body.appendChild(c);
    setTimeout(() => c.remove(), 3600);
  }
}

function countUp(el, to) {
  if (reduceMotion) {
    el.textContent = fmt(to);
    return;
  }
  const t0 = performance.now();
  const dur = Math.min(1400, 500 + to / 40);
  const step = (now) => {
    const k = Math.min(1, (now - t0) / dur);
    el.textContent = fmt(Math.round(to * (1 - Math.pow(1 - k, 3))));
    if (k < 1) requestAnimationFrame(step);
  };
  requestAnimationFrame(step);
}

/** Phone vibration, through the app shell when running inside it (dallas-dash-mobile). */
function haptic(kind) {
  if (window.ReactNativeWebView) window.ReactNativeWebView.postMessage(JSON.stringify({ haptic: kind }));
}

let coinSoundAt = 0;
function react(events) {
  for (const e of events) {
    if (e === "coin") {
      const now = performance.now();
      if (now - coinSoundAt > 45) {
        audio.coin();
        coinSoundAt = now;
      }
    } else if (e === "stumble") {
      audio.hit();
      haptic("warn");
      jolt(false);
      toast("Watch out — the parking officer's behind you!");
    } else if (e === "crashed" || e === "caught") {
      audio.crash();
      haptic("heavy");
      jolt(true);
    } else if (e === "boardbreak") {
      audio.crash();
      haptic("heavy");
      jolt(true);
      toast(`${BOARD} saved you!`);
    } else if (e === "board") {
      audio.power();
      toast(`${BOARD}!`);
    } else if (e === "revive") {
      audio.coupon();
      haptic("success");
    } else if (e.startsWith("power:")) {
      const k = e.slice(6);
      audio.power();
      haptic("light");
      toast(POWER[k].name);
    } else if (e.startsWith("item:")) {
      const k = e.slice(5);
      audio.pickup();
      toast(`+${fmt(SIM.ITEMS[k] * SIM.multiplier(run))} ${ITEM_LABEL[k]}`);
    }
  }
}

/* ── the run loop ──────────────────────────────────────────────────────── */

function stepOnce(code) {
  if (code) inputs.push([run.tick, code]);
  if (code === 3) audio.jump();
  else if (code === 4) audio.slide();
  else if (code === 1 || code === 2) audio.lane();
  SIM.stepRun(run, code);
  react(run.events);
}

function frame(now) {
  requestAnimationFrame(frame);
  const delta = Math.min(100, now - last);
  last = now;
  if (connected) pollGamepad();

  if (running && run && !paused) {
    if (run.downed) {
      if (!reviveShown) showRevive();
      else tickRevive(delta);
    } else if (!run.over) {
      acc += delta;
      let guard = 0;
      while (acc >= STEP_MS && !run.over && !run.downed && guard < 6) {
        stepOnce(pending.length ? pending.shift() : 0);
        acc -= STEP_MS;
        guard += 1;
      }
      audio.tempo(run.speed);
    }
    if (run.over) finishRun();
  }

  let drawn = run;
  if (!drawn) {
    if (!idle && view && Number.isInteger(view.nextSeed)) {
      idle = SIM.createRun(view.nextSeed, view.loadout);
      idle.chase = 0;
      for (let i = 0; i < 30; i += 1) SIM.stepRun(idle, 0);
    }
    if (idle) {
      // the home screen: the runner jogs on the spot in front of the store
      idle.tick += 1;
    }
    drawn = idle;
  }
  tickLoader();
  if (drawn) stats = renderer.draw(drawn, { outfit: run ? (view ? view.outfit : "dante") : preview || (view ? view.outfit : "dante"), route: run ? runRoute : myRoute(), now, idle: !run });
  if (run) paintHud();

  frames += 1;
  if (now - fpsAt >= 500) {
    fps = Math.round((frames * 1000) / (now - fpsAt));
    frames = 0;
    fpsAt = now;
  }
  if (showStats) q("stats").textContent = `${fps} fps · ${stats.polys} tris · ${stats.calls || 0} calls · ${stats.ms.toFixed(1)} ms · ${(stats.ratio || 1).toFixed(2)}x`;
}

/* ── run lifecycle ─────────────────────────────────────────────────────── */

function startRun() {
  if (!view || !renderer.ready) {
    toast("Loading — one moment");
    return;
  }
  if (preview && view.outfits.indexOf(preview) < 0) {
    toast("Still locked — keep running");
    return;
  }
  audio.unlock();
  closeSheets();
  idle = null;
  run = SIM.createRun(view.nextSeed, view.loadout);
  inputs = [];
  pending = [];
  acc = 0;
  running = false;
  paused = false;
  awaiting = false;
  reviveShown = false;
  hide(q("home"));
  show(q("hud"));
  hudCoins = 0;
  hudSightings = 0;
  runRoute = myRoute();
  hudDistrict = districtAt(0, runRoute).name;
  bestBefore = view.bestRun || 0;
  // the controls card shows for the first few runs, then gets out of the way
  let seen = 0;
  try {
    seen = Number(localStorage.getItem("dash:tut") || 0);
    localStorage.setItem("dash:tut", String(seen + 1));
  } catch {
    seen = 0;
  }
  if (seen < 3) {
    show(q("hint"));
    setTimeout(() => hide(q("hint")), 5200);
  } else hide(q("hint"));
  const el = q("countdown");
  let n = 3;
  el.textContent = String(n);
  el.classList.remove("go");
  show(el);
  flashClass(el, "tick");
  countdown = window.setInterval(() => {
    n -= 1;
    if (n > 0) {
      el.textContent = String(n);
      flashClass(el, "tick");
    } else if (n === 0) {
      el.innerHTML = "Giddy<br />up!";
      el.classList.add("go");
      flashClass(el, "tick");
      showDistrict(hudDistrict);
    } else {
      window.clearInterval(countdown);
      countdown = 0;
      hide(el);
      running = true;
      last = performance.now();
      audio.startMusic();
    }
  }, 420);
  paintHud();
}

function pause() {
  if (!running || paused) return;
  paused = true;
  audio.stopMusic();
  q("vPauseWhere").textContent = `Catching your breath in ${districtAt(run.distance, runRoute).name}.`;
  q("vPauseScore").textContent = fmt(Math.round(run.score));
  q("vPauseCoins").textContent = fmt(run.coins);
  q("vPauseDist").textContent = fmt(Math.round(run.distance));
  show(q("pauseScrim"));
  show(q("pause"));
  q("btnResume").focus();
}

function resume() {
  paused = false;
  hide(q("pauseScrim"));
  hide(q("pause"));
  last = performance.now();
  acc = 0;
  audio.startMusic();
}

/** Abandon: nothing is submitted, nothing credited, the course seed is untouched. */
function quitToHome() {
  if (countdown) {
    window.clearInterval(countdown);
    countdown = 0;
  }
  hide(q("countdown"));
  hide(q("pause"));
  hide(q("pauseScrim"));
  hide(q("revive"));
  running = false;
  paused = false;
  run = null;
  audio.stopMusic();
  hide(q("hud"));
  q("chaseWarn").classList.remove("on");
  show(q("home"));
  paintHome();
}

function showRevive() {
  reviveShown = true;
  audio.stopMusic();
  const cost = SIM.reviveCost(run);
  q("reviveWhy").textContent =
    run.downed === "caught" ? "The parking officer caught you." : "You crashed.";
  q("btnRevive").textContent = `Use ${cost} ${TOKEN}${cost > 1 ? "s" : ""} (${run.keysLeft} left)`;
  reviveTimer = 4000;
  show(q("revive"));
  q("btnRevive").focus();
}

function tickRevive(delta) {
  reviveTimer -= delta;
  q("reviveRing").style.setProperty("--p", Math.max(0, reviveTimer / 4000));
  if (reviveTimer <= 0) decide(false);
}

function decide(useKey) {
  if (!run || !run.downed) return;
  hide(q("revive"));
  reviveShown = false;
  stepOnce(useKey ? SIM.INPUT.REVIVE : SIM.INPUT.GIVE_UP);
  if (!run.over) {
    acc = 0;
    last = performance.now();
    audio.startMusic();
  }
}

function makeRunId() {
  const rand = Math.random().toString(36).slice(2, 8);
  return `r${run.seed.toString(36)}${Date.now().toString(36)}${rand}`.slice(0, 34);
}

function finishRun() {
  running = false;
  audio.stopMusic();
  audio.finish();
  const summary = SIM.summarize(run);
  // results first, then submit: the verdict (sync in the static build, async
  // from a server) fills in the verified line when it arrives
  showResults(summary);
  awaiting = true;
  send({
    type: "action",
    action: { kind: "submitRun", runId: makeRunId(), seed: run.seed, inputs, claimedScore: summary.score },
  });
  q("chaseWarn").classList.remove("on");
  hide(q("hud"));
}

function showResults(summary) {
  const why =
    summary.reason === "caught"
      ? "The parking officer caught you."
      : summary.reason === "crashed"
        ? "You crashed."
        : "Ten minutes up — what a run.";
  q("resTitle").textContent = summary.reason === "timeup" ? "Time!" : "Run over";
  q("vResNote").textContent = why;
  countUp(q("vResScore"), summary.score);
  const best = summary.score > bestBefore && summary.score > 0;
  q("vNewBest").classList.toggle("hidden", !best);
  if (best) setTimeout(confetti, 350);
  q("vResDistrict").textContent = districtAt(summary.distance, runRoute).name;
  q("vResCoins").textContent = fmt(summary.coins);
  q("vResDistance").textContent = fmt(summary.distance);
  q("vResBest").textContent = fmt(Math.max(view ? view.bestRun : 0, summary.score));
  q("vVerify").textContent = "verifying…";
  q("vVerify").className = "verify";
  q("vResPoints").textContent = "…";
  hide(q("btnResClaim"));
  hide(q("vSetDone"));
  paintMissions(q("vResMissions"));
  show(q("scrim"));
  show(q("results"));
  q("btnAgain").focus();
}

function paintVerified(last) {
  q("vVerify").textContent = "✓ verified";
  q("vVerify").className = "verify ok";
  q("vResPoints").textContent = `+${fmt(last.awarded)}`;
  q("vResBest").textContent = fmt(view.bestRun);
  q("vSetDone").classList.toggle("hidden", !last.setDone);
  q("btnResClaim").classList.toggle("hidden", !view.claimReady);
  paintMissions(q("vResMissions"));
}

/* ── painting ──────────────────────────────────────────────────────────── */

function paintHome() {
  if (!view) return;
  q("vCoins").textContent = fmt(view.coins);
  q("vKeys").textContent = fmt(view.keys);
  q("vMultBadge").textContent = `x${view.mult}`;
  q("vHomeBest").textContent = fmt(view.bestRun);
  q("vHomeRuns").textContent = fmt(view.runs);
  q("vClaimBadge").classList.toggle("hidden", !view.claimReady);
  const r = routeById(view.route);
  q("vTag").textContent = `${r.region === "Texas" ? "★" : "🍁"} ${r.name} · ${r.region}`;
  paintPicker();
  const chips = q("cities");
  const html = ROUTES.map((x) => `<button type="button" role="radio" aria-checked="${x.id === view.route}" data-route="${x.id}">${ROUTE_AV[x.id]} ${x.name}</button>`).join("");
  if (chips.innerHTML !== html) chips.innerHTML = html;
  const pct = view.claimReady ? 100 : Math.min(100, (view.points / view.terms.points) * 100);
  q("vBar").style.width = `${pct}%`;
  q("vPointsLine").textContent = view.claimReady
    ? "Coupon ready — tap Coupon to claim 15% off"
    : `${fmt(view.points)} / ${fmt(view.terms.points)} points to a 15% KFC coupon`;
}

function paintPicker() {
  if (!view) return;
  const list = view.outfitList;
  if (!preview || !list.some((o) => o.id === preview)) preview = view.outfit;
  const o = list.find((x) => x.id === preview) || list[0];
  const owned = view.outfits.indexOf(o.id) >= 0;
  q("vWho").textContent = o.name;
  q("vRole").textContent = o.role || "";
  q("vLock").classList.toggle("hidden", owned);
  q("vLockText").textContent = `${fmt(o.at)} lifetime pts`;
  const play = q("btnPlay");
  play.disabled = !owned;
  play.textContent = owned ? "Run" : "Locked";
}

function stepPicker(dir) {
  if (!view) return;
  audio.unlock();
  audio.ui();
  const list = view.outfitList;
  const i = Math.max(0, list.findIndex((o) => o.id === (preview || view.outfit)));
  preview = list[(i + dir + list.length) % list.length].id;
  if (view.outfits.indexOf(preview) >= 0 && preview !== view.outfit) {
    send({ type: "action", action: { kind: "setOutfit", outfit: preview } });
  }
  paintPicker();
}

function paintHud() {
  if (!run) return;
  q("vScore").textContent = fmt(Math.round(run.score));
  const m = SIM.multiplier(run);
  const mult = q("vMult");
  mult.textContent = `x${m}`;
  mult.classList.toggle("x2", run.double > 0);
  q("vRunCoins").textContent = fmt(run.coins);
  if (run.coins !== hudCoins) {
    if (run.coins > hudCoins) flashClass(q("coinChip"), "bump");
    hudCoins = run.coins;
  }
  q("vDist").textContent = fmt(Math.round(run.distance));
  const district = districtAt(run.distance, runRoute).name;
  if (district !== hudDistrict) {
    hudDistrict = district;
    showDistrict(district);
    audio.power();
  }
  if (run.sightings.length > hudSightings) {
    hudSightings = run.sightings.length;
    const spots = routeById(runRoute).landmarks;
    showSpot(spots[run.sightings[hudSightings - 1] % spots.length]);
  }
  q("vBoards").textContent = run.board > 0 ? "on" : `×${run.boardsLeft}`;
  q("btnBoard").disabled = run.board > 0 || run.boardsLeft <= 0;
  q("chaseWarn").classList.toggle("on", run.chase > 0 && run.chase < 280 && !run.downed);

  const timers = [];
  for (const k of SIM.POWERS) {
    const left = k === "jetpack" ? run.jet : run[k];
    if (left > 0) {
      const total = SIM.powerTicks(k, run.levels[k]);
      timers.push(`<div class="timer${left < 120 ? " low" : ""}"><i style="background:${POWER[k].color}">${POWER[k].glyph}</i><div class="t"><div style="width:${(left / total) * 100}%"></div></div></div>`);
    }
  }
  if (run.board > 0) {
    timers.push(`<div class="timer"><i style="background:#fff">${icon("i-board", "ic")}</i><div class="t"><div style="width:${(run.board / SIM.BOARD_TICKS) * 100}%"></div></div></div>`);
  }
  const html = timers.join("");
  if (html !== paintHud._t) {
    q("timers").innerHTML = html;
    paintHud._t = html;
  }
}

function paintMissions(el) {
  if (!view) return;
  el.innerHTML = view.missions
    .map(
      (m) => `<div class="row${m.done ? " done" : ""}"><i class="ico">${m.done ? "✅" : "🎯"}</i><div class="body"><b>${m.label}</b><small>${fmt(m.have)} / ${fmt(m.target)}</small><div class="mbar"><div style="width:${(m.have / m.target) * 100}%"></div></div></div></div>`,
    )
    .join("");
}

function paintSheet(name) {
  if (!view) return;
  if (name === "missions") {
    q("vSetNo").textContent = String(view.missionSet);
    paintMissions(q("vMissions"));
    let ladder = "";
    for (let i = 1; i <= view.maxMult; i += 1) ladder += `<span class="${i <= view.mult ? "on" : ""}">x${i}</span>`;
    q("vMultLadder").innerHTML = ladder;
  }
  if (name === "shop") {
    q("vShopCoins").textContent = fmt(view.coins);
    const rows = [
      { id: "board", ico: icon("i-board"), title: BOARD, sub: `Absorbs one crash · you own ${view.boards}`, price: view.prices.board },
      { id: "key", ico: icon("i-card"), title: "Secret recipe card", sub: `Revive after a crash · you own ${view.keys}`, price: view.prices.key },
    ];
    for (const k of SIM.POWERS) {
      const lv = view.levels[k];
      rows.push({
        id: k,
        ico: POWER[k].glyph,
        title: POWER[k].name,
        sub: `Lasts ${SIM.powerTicks(k, lv) / 60}s`,
        lv,
        price: lv >= 5 ? null : view.prices.upgrade[lv],
      });
    }
    q("vShop").innerHTML = rows
      .map((r) => {
        const pips = r.lv === undefined ? "" : `<div class="pips">${[0, 1, 2, 3, 4].map((i) => `<span class="${i < r.lv ? "on" : ""}"></span>`).join("")}</div>`;
        const btn =
          r.price === null
            ? `<button type="button" disabled>Max</button>`
            : `<button type="button" class="red buy" data-buy="${r.id}" ${view.coins < r.price ? "disabled" : ""}>${icon("i-drum", "ic")} ${fmt(r.price)}</button>`;
        return `<div class="row"><i class="ico">${r.ico}</i><div class="body"><b>${r.title}</b><small>${r.sub}</small>${pips}</div>${btn}</div>`;
      })
      .join("");
  }
  if (name === "chars") {
    q("vChars").innerHTML = view.outfitList
      .map((o) => {
        const owned = view.outfits.indexOf(o.id) >= 0;
        return `<button type="button" class="char${view.outfit === o.id ? " on" : ""}${owned ? "" : " locked"}" data-outfit="${o.id}"><div class="av">${crewImg(o.id)}</div><b>${o.name}</b><small class="role">${o.role || ""}</small><small>${owned ? (view.outfit === o.id ? "running" : "tap to pick") : `${fmt(o.at)} lifetime pts`}</small></button>`;
      })
      .join("");
    q("vLandmarkCount").textContent = `${view.landmarks.length} of ${view.landmarkTotal} spotted. Run past them to fill your album.`;
    // the album: the illustrated landmarks, in colour once you've run past them
    q("vAlbum").innerHTML = ROUTES.flatMap((r) => r.landmarks.filter((l) => HERO_ART[l.model]).map((l) => ({ ...l, city: r.name })))
      .map((l) => {
        const got = view.landmarks.indexOf(l.name) >= 0;
        return `<figure class="lm${got ? " got" : ""}"><img src="${HERO_ART[l.model]}" alt="" loading="lazy" /><figcaption><b>${l.name}</b><small>${got ? l.city : "not spotted yet"}</small></figcaption></figure>`;
      })
      .join("");
  }
  if (name === "settings") {
    for (const b of document.querySelectorAll("[data-quality]")) b.setAttribute("aria-checked", String(b.dataset.quality === renderer.quality));
    for (const b of document.querySelectorAll("[data-sound]")) b.setAttribute("aria-checked", String((b.dataset.sound === "on") === !audio.muted));
    const rec = QUALITY_NAME[renderer.recommended];
    q("vQualityNote").textContent = settings.quality
      ? `Recommended for this device: ${rec}.`
      : `Set for this device: ${rec}. Lower it if the game stutters, raise it for sharper streets.`;
  }
  if (name === "routes") {
    q("vRoutes").innerHTML = view.routeList
      .map(
        (r) => `<button type="button" class="char route${view.route === r.id ? " on" : ""}" data-route="${r.id}"><div class="av">${routeAv(r.id)}</div><b>${r.name}</b><small class="role">${r.blurb}</small><small>${view.route === r.id ? "selected" : "tap to run here"} · ${r.spotted}/10 landmarks</small></button>`,
      )
      .join("");
  }
  if (name === "rewards") {
    q("vRPoints").textContent = fmt(view.points);
    q("vRBest").textContent = fmt(view.bestRun);
    q("vRRuns").textContent = fmt(view.runs);
    const active = view.coupons.filter((c) => c.status === "active");
    q("vCoupons").innerHTML = active.length
      ? active.map((c) => `<div class="code">${c.code}</div><p class="note">${c.percent}% off · min $${c.minOrder} · up to $${c.maxOff} off · ${c.validDays} days</p>`).join("")
      : `<p>No coupon yet — ${fmt(Math.max(0, view.terms.points - view.points))} points to go.</p>`;
    q("btnClaim").disabled = !view.claimReady;
    q("btnClaim").textContent = view.claimReady ? "Claim 15% coupon" : `${fmt(Math.max(0, view.terms.points - view.points))} points to go`;
    q("vTerms").textContent = `15% off · minimum order $${view.terms.minOrder} · up to $${view.terms.maxOff} off · valid ${view.terms.validDays} days · one code per ${fmt(view.terms.points)} verified points.`;
    q("btnTestGrant").classList.toggle("hidden", !view.testMode);
    q("vTestNote").classList.toggle("hidden", !view.testMode);
    q("btnTestGrant").disabled = view.claimReady;
    q("vRuntimeNote").textContent = window.DASH_LOCAL
      ? "Test build: your account and coupons live in this browser only. In production they live on the server, every run is replayed there, and our delivery app redeems codes against the same database."
      : "";
  }
}

/* ── sheets ────────────────────────────────────────────────────────────── */

function showSheet(el) {
  show(q("scrim"));
  show(el);
}

function closeSheets() {
  for (const el of document.querySelectorAll(".sheet")) {
    if (el.id !== "pause") hide(el);
  }
  hide(q("scrim"));
  openSheet = null;
}

document.querySelectorAll("[data-sheet]").forEach((btn) => {
  btn.addEventListener("click", () => {
    audio.unlock();
    audio.ui();
    closeSheets();
    openSheet = btn.dataset.sheet;
    paintSheet(openSheet);
    showSheet(q(`sheet-${openSheet}`));
  });
});
document.querySelectorAll("[data-quality]").forEach((btn) => {
  btn.addEventListener("click", () => {
    settings.quality = btn.dataset.quality;
    saveSettings();
    renderer.setQuality(settings.quality, false);
    audio.ui();
    paintSheet("settings");
  });
});
document.querySelectorAll("[data-sound]").forEach((btn) => {
  btn.addEventListener("click", () => {
    settings.sound = btn.dataset.sound === "on";
    saveSettings();
    audio.unlock();
    audio.setMuted(!settings.sound);
    audio.ui();
    paintSheet("settings");
  });
});
document.querySelectorAll("[data-close]").forEach((btn) => {
  btn.addEventListener("click", () => {
    audio.ui();
    closeSheets();
  });
});
q("scrim").addEventListener("click", () => {
  if (!q("results").classList.contains("hidden")) return;
  closeSheets();
});
q("vShop").addEventListener("click", (event) => {
  const b = event.target.closest("[data-buy]");
  if (!b) return;
  audio.ui();
  send({ type: "action", action: { kind: "buy", item: b.dataset.buy } });
});
q("btnPrev").addEventListener("click", () => stepPicker(-1));
q("btnNext").addEventListener("click", () => stepPicker(1));
q("cities").addEventListener("click", (event) => {
  const b = event.target.closest("[data-route]");
  if (!b || !view || b.dataset.route === view.route) return;
  audio.unlock();
  audio.ui();
  send({ type: "action", action: { kind: "setRoute", route: b.dataset.route } });
});
q("vRoutes").addEventListener("click", (event) => {
  const b = event.target.closest("[data-route]");
  if (!b) return;
  audio.ui();
  idle = null;
  send({ type: "action", action: { kind: "setRoute", route: b.dataset.route } });
});
q("vChars").addEventListener("click", (event) => {
  const b = event.target.closest("[data-outfit]");
  if (!b) return;
  if (!view || view.outfits.indexOf(b.dataset.outfit) < 0) {
    toast("Still locked — keep running");
    return;
  }
  audio.ui();
  idle = null;
  preview = b.dataset.outfit;
  send({ type: "action", action: { kind: "setOutfit", outfit: b.dataset.outfit } });
});

function claim() {
  if (!view || !view.claimReady) return;
  claimPending = true;
  audio.ui();
  send({ type: "action", action: { kind: "claimCoupon" } });
}

q("btnPlay").addEventListener("click", startRun);
q("btnAgain").addEventListener("click", () => {
  closeSheets();
  startRun();
});
q("btnHome").addEventListener("click", () => {
  closeSheets();
  run = null;
  show(q("home"));
  paintHome();
});
q("btnPause").addEventListener("click", pause);
q("btnResume").addEventListener("click", resume);
q("btnQuit").addEventListener("click", quitToHome);
q("btnBoard").addEventListener("click", () => press(5));
q("btnRevive").addEventListener("click", () => decide(true));
q("btnGiveUp").addEventListener("click", () => decide(false));
q("btnClaim").addEventListener("click", claim);
q("btnResClaim").addEventListener("click", claim);
q("btnTestGrant").addEventListener("click", () => {
  audio.unlock();
  audio.ui();
  send({ type: "action", action: { kind: "grantTestPoints" } });
});
q("btnRewardClose").addEventListener("click", () => {
  closeSheets();
  run = null;
  show(q("home"));
  paintHome();
});
q("btnCopy").addEventListener("click", async () => {
  const code = q("vCode").textContent;
  try {
    await navigator.clipboard.writeText(code);
    toast("Code copied");
  } catch {
    toast(code);
  }
});

window.addEventListener("resize", () => renderer.resize());
// the app shell calls this when the phone backgrounds the game
window.__ddPause = () => {
  if (running && !run.downed) pause();
};
document.addEventListener("visibilitychange", () => {
  if (document.hidden && running && !run.downed) pause();
  last = performance.now();
  acc = 0;
});

function fatal(what) {
  q("fatalMsg").textContent = `${what} — reload the page, and send this line back if it repeats.`;
  show(q("fatal"));
}
window.addEventListener("error", (event) => fatal(String(event.message || "script error")));
window.addEventListener("unhandledrejection", (event) => {
  const reason = event.reason;
  fatal(String((reason && reason.message) || reason || "promise error"));
});

if (showStats) show(q("stats"));
if (window.DASH_LOCAL) {
  import("./local-room.js")
    .then((mod) => {
      localRoom = mod.startLocalRoom({ onState, onError, playerId: ME });
      connected = true;
    })
    .catch((err) => fatal(String((err && err.message) || err)));
} else {
  connect();
}
requestAnimationFrame(frame);

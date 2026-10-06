/**
 * Dallas Dash — the screen, the input and the wire.
 *
 * The rules live on the server (app/src/logic.js) and the course itself lives in
 * app/public/sim.js. This file runs the course locally for the player, records
 * every input against a tick number, and submits that log for verification — so
 * a run's points are decided by the server replaying it, never by this code.
 */

import { createRenderer } from "./scene.js";
import { audio } from "./audio.js";
import * as SIM from "./sim.js";

const ITEM_LABEL = {
  cup: "soda cup",
  popcorn: "popcorn bites",
  fries: "seasoned fries",
  coleslaw: "slaw",
  wrap: "crispy wrap",
  burger: "Dash burger",
  bucket: "family box",
  bowl: "loaded bowl",
};

const q = (id) => document.getElementById(id);
const show = (el) => el.classList.remove("hidden");
const hide = (el) => el.classList.add("hidden");
const fmt = (n) => Number(n || 0).toLocaleString("en-US");
const STEP_MS = 1000 / 60;

const canvas = q("view");
const renderer = createRenderer(canvas);

/* ── net (the harness the template ships; the server is authoritative) ─── */

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
    // Storage can be blocked (private mode, locked-down iframe). A throw here
    // would take the whole game down, so a fresh id per load is the fallback:
    // it plays fine, it just starts a new account instead of resuming one.
    return Math.random().toString(36).slice(2, 10);
  }
}

const ME = playerId();
const room = new URLSearchParams(location.search).get("room") || `dash-${ME}`;

const PING = "__ping";
const PONG = "__pong";
let socket = null;
let localRoom = null; // set by the static export: the room runs in the browser
let retry = 0;
let connected = false;

function setNet(text, bad) {
  const el = q("netstat");
  el.textContent = text;
  el.classList.toggle("bad", Boolean(bad));
}

function connect() {
  const proto = location.protocol === "https:" ? "wss:" : "ws:";
  socket = new WebSocket(`${proto}//${location.host}/ws/${encodeURIComponent(room)}`);
  socket.addEventListener("open", () => {
    retry = 0;
    connected = true;
    setNet("online");
    send({ type: "join", playerId: ME });
  });
  socket.addEventListener("message", (event) => {
    if (event.data === PONG) return;
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
    const wait = 500 * 2 ** (retry - 1);
    setNet(`offline — retrying in ${Math.round(wait / 1000)}s`, true);
    running = false;
    setTimeout(connect, wait);
  });
}

function send(msg) {
  // One entry point for both runtimes: the hosted room speaks WebSockets, the
  // static export runs the same protocol in the browser.
  if (localRoom) {
    localRoom.send(msg);
    return;
  }
  if (socket && socket.readyState === WebSocket.OPEN) socket.send(JSON.stringify(msg));
}

setInterval(() => {
  if (socket && socket.readyState === WebSocket.OPEN) socket.send(PING);
}, 30000);

/* ── state ─────────────────────────────────────────────────────────────── */

let view = null;
let run = null;
let idle = null;
let inputs = [];
let pending = [];
let running = false;
let awaiting = false;
let claimPending = false;
let countdown = 0;
let lastMult = 1;
let outfitCount = 0;
let acc = 0;
let last = performance.now();
let frames = 0;
let fpsAt = performance.now();
let fps = 0;
let stats = { polys: 0, ms: 0 };
let showStats = true;

function onState(msg) {
  view = msg.view;
  if (view) {
    // A new outfit is a visible reward: say so once, in passing.
    if (view.outfits.length > outfitCount && outfitCount > 0) {
      toast("New free outfit unlocked");
      audio.coupon();
    }
    outfitCount = view.outfits.length;
  }
  if (awaiting && view && view.lastRun) {
    awaiting = false;
    showResults(view.lastRun, view);
  }
  if (claimPending && view && view.coupons.length) {
    claimPending = false;
    const code = view.coupons[view.coupons.length - 1];
    q("vCode").textContent = code.code;
    q("vRewardTerms").textContent = `${code.percent}% off · minimum order $${code.minOrder} · up to $${code.maxOff} off · valid ${code.validDays} days · account-owned, not transferable.`;
    hide(q("results"));
    hide(q("menu"));
    show(q("reward"));
    audio.coupon();
  }
  if (!view) setNet(`online · spectating room ${room}`, true);
  paintMenu();
}

function onError(err) {
  setNet(err, true);
  if (awaiting) {
    awaiting = false;
    q("vResTitle").textContent = "Run not credited";
    q("vResNote").textContent = err;
    q("vResAward").textContent = "0";
    show(q("results"));
  } else {
    toast(err);
  }
}

/* ── input ─────────────────────────────────────────────────────────────── */

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
};

function press(code) {
  audio.unlock();
  if (!running) {
    pending.length = 0;
    return;
  }
  pending.push(code);
  if (code === 3) audio.jump();
  else if (code === 4) audio.slide();
  else audio.lane();
}

window.addEventListener(
  "keydown",
  (event) => {
    if (event.code === "KeyF") {
      showStats = !showStats;
      q("stats").classList.toggle("hidden", !showStats);
      return;
    }
    if (event.code === "Enter" && !running && !q("menu").classList.contains("hidden")) {
      event.preventDefault();
      startRun();
      return;
    }
    if (event.code === "Escape" && (running || countdown)) {
      event.preventDefault();
      quitToMenu();
      return;
    }
    const code = KEYS[event.code];
    if (code) {
      event.preventDefault();
      press(code);
    }
  },
  { passive: false },
);

// touch: four buttons and swipe anywhere on the canvas
q("touch").addEventListener("pointerdown", (event) => {
  const act = event.target.dataset ? event.target.dataset.act : null;
  if (act) {
    event.preventDefault();
    press(Number(act));
  }
});

let touchStart = null;
canvas.addEventListener("pointerdown", (event) => {
  touchStart = { x: event.clientX, y: event.clientY };
});
canvas.addEventListener("pointerup", (event) => {
  if (!touchStart) return;
  const dx = event.clientX - touchStart.x;
  const dy = event.clientY - touchStart.y;
  touchStart = null;
  const ax = Math.abs(dx);
  const ay = Math.abs(dy);
  if (Math.max(ax, ay) < 26) {
    press(3);
    return;
  }
  if (ax > ay) press(dx > 0 ? 2 : 1);
  else press(dy > 0 ? 4 : 3);
});

const padPrev = {};
function pollGamepad() {
  if (!navigator.getGamepads) return;
  const pads = navigator.getGamepads();
  for (const pad of pads) {
    if (!pad) continue;
    const map = { 0: 3, 1: 4, 12: 3, 13: 4, 14: 1, 15: 2 };
    for (const index of Object.keys(map)) {
      const btn = pad.buttons[index];
      const down = Boolean(btn && btn.pressed);
      const key = `${pad.index}:${index}`;
      if (down && !padPrev[key]) press(map[index]);
      padPrev[key] = down;
    }
  }
}

/* ── the run loop ──────────────────────────────────────────────────────── */

function toast(text) {
  const el = q("toast");
  el.textContent = text;
  show(el);
  clearTimeout(toast._t);
  toast._t = setTimeout(() => hide(el), 950);
}

function stepOnce(code) {
  const prevDist = run.distance;
  const prevHits = run.hits;
  const prevSight = run.sightings.length;
  const prevStores = run.stores;
  if (code) inputs.push([run.tick, code]);
  SIM.stepRun(run, code);

  for (let i = 0; i < run.objects.length; i += 1) {
    const o = run.objects[i];
    if (o.t !== 0 || o.l !== run.lane) continue;
    if (o.z - prevDist > 0 && o.z - run.distance <= 0) {
      const mult = 1 + Math.min(2, Math.floor((run.combo - 1) / 8));
      toast(`+${SIM.ITEMS[o.k] * mult} ${ITEM_LABEL[o.k]}`);
      audio.pickup(run.combo);
    }
  }
  if (run.sightings.length > prevSight) {
    toast(`Landmark logged · ${SIM.LANDMARKS[run.sightings[run.sightings.length - 1]]}`);
  } else if (run.stores > prevStores) {
    toast("Big D Fried storefront passed");
  }
  if (run.hits > prevHits) audio.hit();
}

function frame(now) {
  requestAnimationFrame(frame);
  const delta = Math.min(120, now - last);
  last = now;
  if (connected) pollGamepad();

  if (running && run && !run.over) {
    acc += delta;
    let guard = 0;
    while (acc >= STEP_MS && !run.over && guard < 6) {
      stepOnce(pending.length ? pending.shift() : 0);
      acc -= STEP_MS;
      guard += 1;
    }
    if (run.over) finishRun();
  }

  let drawn = run;
  if (!drawn) {
    if (!idle && view && Number.isInteger(view.nextSeed)) {
      idle = SIM.createRun(view.nextSeed);
      for (let i = 0; i < 240; i += 1) SIM.stepRun(idle, 0);
    }
    drawn = idle;
  }
  if (drawn) {
    const result = renderer.draw(drawn, { outfit: view ? view.outfit : "classic" });
    stats = result;
  }
  paintHud();

  frames += 1;
  if (now - fpsAt >= 500) {
    fps = Math.round((frames * 1000) / (now - fpsAt));
    frames = 0;
    fpsAt = now;
  }
  if (showStats) {
    q("stats").textContent = `${fps} fps · ${stats.polys} polys · ${stats.ms.toFixed(1)} ms`;
  }
}

/* ── screens ───────────────────────────────────────────────────────────── */

function startRun() {
  if (!view) {
    toast("Still connecting — one moment");
    return;
  }
  audio.unlock();
  idle = null;
  run = SIM.createRun(view.nextSeed);
  for (let i = 0; i < 20; i += 1) SIM.stepRun(run, 0); // identical on both sides
  inputs = [];
  pending = [];
  acc = 0;
  running = false; // the countdown holds the run still until GO
  awaiting = false;
  lastMult = 1;
  hide(q("menu"));
  hide(q("results"));
  hide(q("reward"));
  hide(q("banner"));
  show(q("hud"));
  show(q("hint"));
  const el = q("countdown");
  let n = 3;
  el.textContent = String(n);
  el.classList.remove("go");
  show(el);
  countdown = window.setInterval(() => {
    n -= 1;
    if (n > 0) {
      el.textContent = String(n);
    } else if (n === 0) {
      el.textContent = "GO";
      el.classList.add("go");
    } else {
      window.clearInterval(countdown);
      countdown = 0;
      hide(el);
      begin();
    }
  }, 620);
  paintHud();
}

function begin() {
  running = true;
  audio.startMusic();
  paintHud();
}

/**
 * Back to the account screen. A run in progress is simply abandoned: nothing is
 * submitted, so nothing is credited, and the course seed is untouched.
 */
function quitToMenu() {
  if (countdown) {
    window.clearInterval(countdown);
    countdown = 0;
  }
  hide(q("countdown"));
  running = false;
  run = null;
  audio.stopMusic();
  hide(q("hud"));
  hide(q("hint"));
  show(q("banner"));
  show(q("menu"));
  paintMenu();
}

function makeRunId() {
  const rand = Math.random().toString(36).slice(2, 8);
  return `r${run.seed.toString(36)}${Date.now().toString(36)}${rand}`.slice(0, 34);
}

function finishRun() {
  running = false;
  audio.stopMusic();
  const summary = {
    score: Math.round(run.score),
    distance: Math.round(run.distance),
    pickups: run.pickups,
    combos: run.bestCombo,
    hits: run.hits,
    reason: run.reason,
    sightings: run.sightings.slice(),
    stores: run.stores,
  };
  const runId = makeRunId();
  awaiting = true;
  send({
    type: "action",
    action: {
      kind: "submitRun",
      runId,
      seed: run.seed,
      ticks: run.tick,
      inputs,
      claimedScore: summary.score,
    },
  });
  showResults({ ...summary, awarded: null }, view);
  hide(q("hint"));
}

function showResults(summary, forView) {
  const credited = summary.awarded !== null && summary.awarded !== undefined;
  q("vResTitle").textContent = credited ? "Run credited" : "Verifying your run…";
  const why =
    summary.reason === "wrecked"
      ? "You ran out of hits — the points you already earned are still verified."
      : "Course complete. Landmark sightings and storefronts are logged too.";
  q("vResNote").textContent = credited ? why : "The server is replaying your input log on this exact course.";
  q("vResScore").textContent = fmt(summary.score);
  q("vResAward").textContent = credited ? fmt(summary.awarded) : "…";
  q("vResDistance").textContent = fmt(summary.distance);
  q("vResPickups").textContent = fmt(summary.pickups);
  q("vResCombo").textContent = String(summary.combos || 0);
  q("vResHits").textContent = fmt(summary.hits);
  const seen = (summary.sightings || []).map((i) => SIM.LANDMARKS[i]);
  q("vResLandmarks").innerHTML = seen.length
    ? seen.map((n) => `<span class="chip on">${n}</span>`).join("")
    : `<span class="chip">none this run</span>`;
  const ready = Boolean(forView && forView.claimReady);
  q("btnResClaim").classList.toggle("hidden", !ready);
  hide(q("menu"));
  show(q("banner"));
  show(q("results"));
}

function paintMenu() {
  if (!view) {
    q("vMenuAccount").textContent = "spectator";
    q("btnStart").disabled = true;
    return;
  }
  q("btnStart").disabled = false;
  q("vMenuPoints").textContent = fmt(view.points);
  q("vMenuBest").textContent = fmt(view.bestRun);
  q("vMenuRuns").textContent = fmt(view.runs);
  q("vMenuDistance").textContent = fmt(view.distance);
  q("vMenuStores").textContent = fmt(view.stores);
  q("vMenuAccount").textContent = view.account;
  q("vLandmarkCount").textContent = `${view.landmarks.length} of ${view.landmarkTotal}`;
  q("vLandmarks").innerHTML = SIM.LANDMARKS.map((n, i) => {
    const on = view.landmarks.indexOf(n) >= 0;
    return `<span class="chip${on ? " on" : ""}">${on ? "✓ " : ""}${i + 1}. ${n}</span>`;
  }).join("");
  q("vOutfits").innerHTML = OUT._list
    .map((o) => {
      const owned = view.outfits.indexOf(o.id) >= 0;
      return `<span class="chip${view.outfit === o.id ? " on" : owned ? "" : " locked"}" data-outfit="${o.id}">${owned ? o.name : `${o.name} · ${fmt(o.at)}`}</span>`;
    })
    .join("");
  const active = view.coupons.filter((c) => c.status === "active");
  q("vCoupons").innerHTML = active.length
    ? active
        .map(
          (c) =>
            `<div class="code">${c.code}</div><p class="note">${c.percent}% off · min $${c.minOrder} · up to $${c.maxOff} off · ${c.validDays} days</p>`,
        )
        .join("")
    : `<p class="note">No coupon yet. ${fmt(view.points)} of ${fmt(view.terms.points)} points earned.</p>`;
  q("btnClaim").disabled = !view.claimReady;
  q("btnClaim").textContent = view.claimReady
    ? "Claim 15% coupon"
    : `${fmt(Math.max(0, view.terms.points - view.points))} points to go`;
  q("vTerms").textContent = `15% off · minimum order $${view.terms.minOrder} · up to $${view.terms.maxOff} off · valid ${view.terms.validDays} days · one code per 100,000 verified points.`;

  // the test shortcut, only when the server reports it is switched on
  const grant = q("btnTestGrant");
  grant.classList.toggle("hidden", !view.testMode);
  q("vTestNote").classList.toggle("hidden", !view.testMode);
  const atThreshold = view.points >= view.terms.points;
  grant.disabled = atThreshold;
  grant.textContent = atThreshold
    ? "Test mode · you are at 100,000 points"
    : `Test mode: top up to ${fmt(view.terms.points)} points`;
  if (view.testCredits) {
    q("vTestNote").textContent = `Test credits applied: ${view.testCredits}. They create no run and no score — verified runs remain the only real source of points.`;
  }

  // a static export has no server: say plainly where the ledger lives
  q("vRuntimeNote").classList.toggle("hidden", !window.DASH_LOCAL);
  if (window.DASH_LOCAL) {
    q("vRuntimeNote").textContent =
      "Static build: your account, points and coupons live in this browser only, per device. The hosted build keeps them on the server, where every run is replayed before it is credited.";
  }
}

function paintHud() {
  if (!run) return;
  q("vThisRun").textContent = fmt(Math.round(run.score));
  q("vBest").textContent = view ? fmt(Math.max(view.bestRun, Math.round(run.score))) : "0";
  const mult = 1 + Math.min(2, Math.floor(run.combo / 8));
  q("vCombo").textContent = `x${mult}`;
  if (mult > lastMult) {
    const chip = q("vCombo").parentElement;
    chip.classList.remove("flare");
    void chip.offsetWidth;
    chip.classList.add("flare");
  }
  lastMult = mult;
  q("vSpeed").textContent = String(Math.round(run.speed * 0.62));
  const hearts = q("vHits").children;
  for (let i = 0; i < hearts.length; i += 1) {
    hearts[i].classList.toggle("gone", i < run.hits);
  }
  const have = view ? view.points : 0;
  const need = view ? view.terms.points : 100000;
  const pct = view && view.claimReady ? 100 : Math.min(100, ((have % need) / need) * 100);
  q("vBar").style.width = `${pct}%`;
  q("vPoints").textContent = `${fmt(have)} verified points`;
  q("vNeed").textContent = view
    ? view.claimReady
      ? "coupon ready to claim"
      : `${fmt(need - have)} to your 15% coupon`
    : "";
}

/** Outfit list, mirrored from the rules so the menu can show locked offers. */
const OUT = {
  _list: [
    { id: "classic", name: "Counter crew", at: 0 },
    { id: "tie", name: "Founder tie", at: 2000 },
    { id: "bucket", name: "Bucket hat", at: 12000 },
    { id: "apron", name: "Kitchen apron", at: 30000 },
    { id: "gold", name: "Golden bucket", at: 100000 },
  ],
};

/* ── wiring ────────────────────────────────────────────────────────────── */

for (let i = 0; i < 3; i += 1) {
  const heart = document.createElement("div");
  heart.className = "heart";
  q("vHits").append(heart);
}

q("btnStart").addEventListener("click", startRun);
q("btnQuit").addEventListener("click", quitToMenu);
q("btnAgain").addEventListener("click", () => {
  hide(q("results"));
  startRun();
});
q("btnMenu").addEventListener("click", () => {
  hide(q("results"));
  show(q("banner"));
  show(q("menu"));
  paintMenu();
});
q("btnHow").addEventListener("click", () => {
  audio.ui();
  q("menu").scrollTo({ top: q("menu").scrollHeight, behavior: "smooth" });
});
q("vOutfits").addEventListener("click", (event) => {
  const id = event.target.dataset ? event.target.dataset.outfit : null;
  if (!id) return;
  if (!view || view.outfits.indexOf(id) < 0) {
    toast("Still locked — keep running");
    return;
  }
  audio.ui();
  send({ type: "action", action: { kind: "setOutfit", outfit: id } });
});
function claim() {
  if (!view || !view.claimReady) return;
  claimPending = true;
  audio.ui();
  send({ type: "action", action: { kind: "claimCoupon" } });
}
q("btnClaim").addEventListener("click", claim);
q("btnTestGrant").addEventListener("click", () => {
  audio.unlock();
  audio.ui();
  send({ type: "action", action: { kind: "grantTestPoints" } });
});
q("btnResClaim").addEventListener("click", claim);
q("btnRewardClose").addEventListener("click", () => {
  hide(q("reward"));
  show(q("banner"));
  show(q("menu"));
  paintMenu();
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
q("btnMute").addEventListener("click", (event) => {
  const on = audio.toggle();
  if (on) audio.startMusic();
  event.target.textContent = on ? "sound on" : "sound off";
});
q("btnStats").addEventListener("click", () => {
  showStats = !showStats;
  q("stats").classList.toggle("hidden", !showStats);
});

window.addEventListener("resize", () => renderer.resize());
document.addEventListener("visibilitychange", () => {
  last = performance.now();
  acc = 0;
});

/** A crash should be readable, never a black screen. */
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
  // Static export: boot the in-browser room instead of a socket.
  import("./local-room.js")
    .then((mod) => {
      localRoom = mod.startLocalRoom({ onState, onError, playerId: ME });
      connected = true;
      setNet("local build · points stored in this browser");
      paintMenu();
    })
    .catch((err) => fatal(String((err && err.message) || err)));
} else {
  setNet("connecting…");
  connect();
}
paintMenu();
requestAnimationFrame(frame);

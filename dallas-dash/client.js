/**
 * Dallas Dash — the screens, the input and the wire.
 *
 * The rules live in rules.js and the course in sim.js. This file runs the
 * course locally for the player, records every input against a tick number,
 * and submits that log for verification — so a run's points are decided by
 * the rules replaying it, never by this code.
 */

import { createRenderer } from "./scene.js";
import { audio } from "./audio.js";
import * as SIM from "./sim.js";

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
  sneakers: { name: "Super sneakers", glyph: "👟", color: "#3ca0f0" },
  double: { name: "2× score", glyph: "2×", color: "#e0a100" },
};
const CHAR_AV = { classic: "🛵", tie: "👔", bucket: "🪣", apron: "👨‍🍳", gold: "🏆" };

const q = (id) => document.getElementById(id);
const show = (el) => el.classList.remove("hidden");
const hide = (el) => el.classList.add("hidden");
const fmt = (n) => Number(n || 0).toLocaleString("en-US");
const STEP_MS = 1000 / 60;

const canvas = q("view");
const renderer = createRenderer(canvas);

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

function onState(msg) {
  const before = view;
  view = msg.view;
  if (before && view && view.outfits.length > before.outfits.length) {
    toast("New crew outfit unlocked!");
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
      jolt(false);
      toast("Watch out — the inspector's behind you!");
    } else if (e === "crashed" || e === "caught") {
      audio.crash();
      jolt(true);
    } else if (e === "boardbreak") {
      audio.crash();
      jolt(true);
      toast("Hoverboard saved you!");
    } else if (e === "board") {
      audio.power();
      toast("Hoverboard!");
    } else if (e === "revive") {
      audio.coupon();
    } else if (e.startsWith("power:")) {
      const k = e.slice(6);
      audio.power();
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
  if (drawn) stats = renderer.draw(drawn, { outfit: view ? view.outfit : "classic", now });
  if (run) paintHud();

  frames += 1;
  if (now - fpsAt >= 500) {
    fps = Math.round((frames * 1000) / (now - fpsAt));
    frames = 0;
    fpsAt = now;
  }
  if (showStats) q("stats").textContent = `${fps} fps · ${stats.polys} polys · ${stats.ms.toFixed(1)} ms`;
}

/* ── run lifecycle ─────────────────────────────────────────────────────── */

function startRun() {
  if (!view) {
    toast("Loading — one moment");
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
  show(q("hint"));
  setTimeout(() => hide(q("hint")), 4200);
  const el = q("countdown");
  let n = 3;
  el.textContent = String(n);
  show(el);
  countdown = window.setInterval(() => {
    n -= 1;
    if (n > 0) el.textContent = String(n);
    else if (n === 0) el.textContent = "GO!";
    else {
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
    run.downed === "caught" ? "The inspector caught you." : "You crashed.";
  q("btnRevive").textContent = `Use ${cost} key${cost > 1 ? "s" : ""} (${run.keysLeft} left)`;
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
      ? "The inspector caught you."
      : summary.reason === "crashed"
        ? "You crashed."
        : "Ten minutes up — what a run.";
  q("resTitle").textContent = summary.reason === "timeup" ? "Time!" : "Run over";
  q("vResNote").textContent = why;
  q("vResScore").textContent = fmt(summary.score);
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
  q("vClaimBadge").classList.toggle("hidden", !view.claimReady);
  const pct = view.claimReady ? 100 : Math.min(100, (view.points / view.terms.points) * 100);
  q("vBar").style.width = `${pct}%`;
  q("vPointsLine").textContent = view.claimReady
    ? "Coupon ready — tap Coupon to claim 15% off"
    : `${fmt(view.points)} / ${fmt(view.terms.points)} points to a 15% KFC coupon`;
}

function paintHud() {
  if (!run) return;
  q("vScore").textContent = fmt(Math.round(run.score));
  const m = SIM.multiplier(run);
  const mult = q("vMult");
  mult.textContent = `x${m}`;
  mult.classList.toggle("x2", run.double > 0);
  q("vRunCoins").textContent = fmt(run.coins);
  q("vBoards").textContent = run.board > 0 ? "on" : `×${run.boardsLeft}`;
  q("btnBoard").disabled = run.board > 0 || run.boardsLeft <= 0;
  q("chaseWarn").classList.toggle("on", run.chase > 0 && run.chase < 280 && !run.downed);

  const timers = [];
  for (const k of SIM.POWERS) {
    const left = k === "jetpack" ? run.jet : run[k];
    if (left > 0) {
      const total = SIM.powerTicks(k, run.levels[k]);
      timers.push(`<div class="timer"><i style="background:${POWER[k].color}">${POWER[k].glyph}</i><div class="t"><div style="width:${(left / total) * 100}%"></div></div></div>`);
    }
  }
  if (run.board > 0) {
    timers.push(`<div class="timer"><i style="background:#e4002b">🛹</i><div class="t"><div style="width:${(run.board / SIM.BOARD_TICKS) * 100}%"></div></div></div>`);
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
      { id: "board", ico: "🛹", title: "Hoverboard", sub: `Absorbs one crash · you own ${view.boards}`, price: view.prices.board },
      { id: "key", ico: "🔑", title: "Key", sub: `Revive after a crash · you own ${view.keys}`, price: view.prices.key },
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
            : `<button type="button" class="red" data-buy="${r.id}" ${view.coins < r.price ? "disabled" : ""}>🍗 ${fmt(r.price)}</button>`;
        return `<div class="row"><i class="ico">${r.ico}</i><div class="body"><b>${r.title}</b><small>${r.sub}</small>${pips}</div>${btn}</div>`;
      })
      .join("");
  }
  if (name === "chars") {
    q("vChars").innerHTML = view.outfitList
      .map((o) => {
        const owned = view.outfits.indexOf(o.id) >= 0;
        return `<button type="button" class="char${view.outfit === o.id ? " on" : ""}${owned ? "" : " locked"}" data-outfit="${o.id}"><div class="av">${CHAR_AV[o.id]}</div><b>${o.name}</b><small>${owned ? (view.outfit === o.id ? "wearing" : "tap to wear") : `${fmt(o.at)} lifetime pts`}</small></button>`;
      })
      .join("");
    q("vLandmarkCount").textContent = `${view.landmarks.length} of ${view.landmarkTotal}${view.landmarks.length ? ` — ${view.landmarks.join(", ")}` : ""}`;
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
      ? "Test build: your account and coupons live in this browser only. In production they live on the server, every run is replayed there, and the KFC app redeems codes against the same database."
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
q("vChars").addEventListener("click", (event) => {
  const b = event.target.closest("[data-outfit]");
  if (!b) return;
  if (!view || view.outfits.indexOf(b.dataset.outfit) < 0) {
    toast("Still locked — keep running");
    return;
  }
  audio.ui();
  idle = null;
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

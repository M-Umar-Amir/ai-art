/**
 * DALLAS DASH — the rules: the account ledger, the shop, missions and coupons.
 *
 * The server half of the game. Pure functions over plain data: the room (the
 * in-browser one in local-room.js, or a server) persists whatever these return.
 *
 * The load-bearing idea: points cannot be fabricated. A client never sends a
 * score it claims to have earned — it sends the seed of the course it ran and
 * the inputs it pressed, and this file replays that exact course with the
 * account's own loadout (multiplier, boards, keys, upgrade levels) and credits
 * only what the replay produces. A mismatch, a re-used run id, or a course the
 * account was not issued is refused.
 *
 * The simulation is imported from sim.js — the very module the client draws
 * with — so the two can never disagree about a course.
 */

import * as SIM from "./sim.js";
import { generateCode } from "./coupon-code.js";

export const meta = { game: "Dallas Dash", minPlayers: 1, maxPlayers: 1 };

/* ── economy ────────────────────────────────────────────────────────────── */

const COUPON_POINTS = 100000;
const COUPON = { percent: 15, minOrder: 25, maxOff: 5, validDays: 30 };
const ACTIVE_LIMIT = 3;
const SEEN_CAP = 60;

const START = { boards: 2, keys: 2, coins: 0 };
const PRICES = {
  board: 300,
  key: 1200,
  upgrade: [250, 600, 1200, 2400, 4800], // to reach level 1..5
};

/**
 * TEST MODE — a testing shortcut, not part of the reward mechanic. Tops the
 * account up to exactly the coupon threshold. It fabricates no run and accepts
 * no amount from the client. Set to false for production.
 */
const TEST_GRANTS = true;

/** Characters, unlocked by lifetime points (once unlocked, kept). */
const OUTFITS = [
  { id: "classic", name: "Delivery rider", at: 0 },
  { id: "tie", name: "Crew member", at: 5000 },
  { id: "bucket", name: "Bucket hat", at: 25000 },
  { id: "apron", name: "Kitchen chef", at: 60000 },
  { id: "gold", name: "Golden bucket", at: 150000 },
];

/* ── missions: sets of three; finishing a set raises the multiplier ──────── */

const MISSION_STATS = {
  coins: "Collect {n} drumsticks",
  jumps: "Jump {n} times",
  rolls: "Roll {n} times",
  rides: "Run on {n} trailer roofs",
  powerups: "Grab {n} power-ups",
  pickups: "Collect {n} menu items",
  distance: "Run {n} m",
  score: "Score {n} in one run",
  boardsUsed: "Use {n} hoverboards",
  stores: "Pass {n} KFC stores",
};
// [stat, target, single-run?] — per set; later sets scale the targets up
const MISSION_SETS = [
  [["coins", 150, false], ["jumps", 15, false], ["distance", 800, true]],
  [["rolls", 15, false], ["powerups", 2, false], ["score", 6000, true]],
  [["rides", 3, false], ["pickups", 6, false], ["coins", 300, true]],
  [["boardsUsed", 1, false], ["jumps", 40, true], ["stores", 3, false]],
  [["powerups", 4, true], ["rides", 8, false], ["score", 15000, true]],
];

function missionsFor(setIndex) {
  const base = MISSION_SETS[setIndex % MISSION_SETS.length];
  const scale = 1 + Math.floor(setIndex / MISSION_SETS.length);
  return base.map(([stat, target, single]) => ({
    stat,
    target: stat === "boardsUsed" ? target * scale : Math.round(target * scale),
    single,
  }));
}

function missionLabel(m) {
  return MISSION_STATS[m.stat].replace("{n}", Number(m.target).toLocaleString("en-US")) + (m.single && m.stat !== "score" ? " in one run" : "");
}

/* ── helpers ─────────────────────────────────────────────────────────────── */

function hash32(s) {
  let h = 2166136261 >>> 0;
  for (let i = 0; i < s.length; i += 1) {
    h ^= s.charCodeAt(i);
    h = Math.imul(h, 16777619) >>> 0;
  }
  return h >>> 0;
}

/** The next course for this account. Deterministic, so replays always match. */
function advanceSeed(seed) {
  return (Math.imul((seed >>> 0) | 1, 1103515245) + 12345) >>> 0;
}

function blankAccount(playerId) {
  return {
    points: 0,
    lifetime: 0,
    coins: START.coins,
    boards: START.boards,
    keys: START.keys,
    levels: { magnet: 0, jetpack: 0, sneakers: 0, double: 0 },
    mult: 1,
    missionSet: 0,
    missionProgress: [0, 0, 0],
    bestRun: 0,
    runs: 0,
    distance: 0,
    stores: 0,
    sightings: [],
    coupons: [],
    claimed: 0,
    outfit: "classic",
    outfits: ["classic"],
    seed: (hash32(playerId) | 1) >>> 0,
    testCredits: 0,
    lastRun: null,
    seenRuns: [],
  };
}

/** The loadout a run is replayed with — taken from the ledger, never the client. */
function loadout(acct) {
  return { mult: acct.mult, boards: acct.boards, keys: acct.keys, levels: acct.levels };
}

function withOutfits(acct) {
  const unlocked = OUTFITS.filter(
    (o) => acct.lifetime >= o.at || acct.outfits.indexOf(o.id) >= 0,
  ).map((o) => o.id);
  return { ...acct, outfits: unlocked };
}

export function setup(players) {
  const accounts = {};
  for (const id of players) accounts[id] = blankAccount(id);
  return { v: 2, accounts };
}

/** Older saves (v1) had no wallet, missions or loadout: give them the defaults. */
export function migrate(state) {
  if (!state || state.v === 2) return state;
  const accounts = {};
  for (const [id, a] of Object.entries(state.accounts || {})) {
    accounts[id] = { ...blankAccount(id), ...a, lifetime: a.lifetime || a.points || 0 };
  }
  return { v: 2, accounts };
}

/* ── validation ──────────────────────────────────────────────────────────── */

function checkRun(acct, a) {
  const runId = a.runId;
  if (typeof runId !== "string" || !/^[A-Za-z0-9_-]{6,40}$/.test(runId)) {
    return { ok: false, error: "run id must be 6-40 letters, digits, - or _" };
  }
  if (acct.seenRuns.indexOf(runId) >= 0) {
    return { ok: false, error: "that run has already been credited" };
  }
  if (!Number.isInteger(a.seed)) return { ok: false, error: "run seed missing" };
  if (a.seed >>> 0 !== acct.seed) {
    return { ok: false, error: "this course is out of date — start a new run" };
  }
  const inputs = a.inputs;
  if (!Array.isArray(inputs) || inputs.length > SIM.MAX_INPUTS) {
    return { ok: false, error: `input log must hold at most ${SIM.MAX_INPUTS} events` };
  }
  let last = -1;
  for (const ev of inputs) {
    if (!Array.isArray(ev) || ev.length !== 2) {
      return { ok: false, error: "each input must be [tick, code]" };
    }
    const [t, c] = ev;
    if (!Number.isInteger(t) || t < 0 || t >= SIM.MAX_TICKS) {
      return { ok: false, error: "input tick out of range" };
    }
    if (!Number.isInteger(c) || c < 1 || c > 7) {
      return { ok: false, error: "input code must be 1-7" };
    }
    if (t <= last) return { ok: false, error: "input ticks must increase" };
    last = t;
  }
  if (!Number.isInteger(a.claimedScore) || a.claimedScore < 0 || a.claimedScore > 5000000) {
    return { ok: false, error: "claimed score out of range" };
  }
  const replay = SIM.simulateRun(a.seed, loadout(acct), inputs);
  if (replay.score !== a.claimedScore) {
    return { ok: false, error: "score could not be verified on this course" };
  }
  return { ok: true };
}

/** The gatekeeper: the client is untrusted and may send anything, any time. */
export function validateAction(state, playerId, action) {
  const acct = state.accounts[playerId];
  if (!acct) return { ok: false, error: "no account for this player" };
  const kind = action && action.kind;

  if (kind === "submitRun") return checkRun(acct, action);

  if (kind === "claimCoupon") {
    if (acct.points < COUPON_POINTS) {
      return { ok: false, error: `needs ${COUPON_POINTS} verified points — ${acct.points} so far` };
    }
    const active = acct.coupons.filter((c) => c.status === "active").length;
    if (active >= ACTIVE_LIMIT) {
      return { ok: false, error: `you already hold ${active} unused coupons` };
    }
    return { ok: true };
  }

  if (kind === "buy") {
    const what = action.item;
    if (what === "board") {
      return acct.coins >= PRICES.board ? { ok: true } : { ok: false, error: "not enough drumsticks" };
    }
    if (what === "key") {
      return acct.coins >= PRICES.key ? { ok: true } : { ok: false, error: "not enough drumsticks" };
    }
    if (SIM.POWERS.indexOf(what) >= 0) {
      const lv = acct.levels[what];
      if (lv >= 5) return { ok: false, error: "already fully upgraded" };
      return acct.coins >= PRICES.upgrade[lv] ? { ok: true } : { ok: false, error: "not enough drumsticks" };
    }
    return { ok: false, error: "unknown item" };
  }

  if (kind === "grantTestPoints") {
    if (!TEST_GRANTS) return { ok: false, error: "test grants are off in this build" };
    if (acct.points >= COUPON_POINTS) return { ok: false, error: "you are already at the coupon threshold" };
    return { ok: true };
  }

  if (kind === "setOutfit") {
    if (!action.outfit || acct.outfits.indexOf(action.outfit) < 0) {
      return { ok: false, error: "that character is still locked" };
    }
    return { ok: true };
  }

  return { ok: false, error: "unknown action" };
}

/* ── state changes ───────────────────────────────────────────────────────── */

function advanceMissions(acct, replay) {
  const list = missionsFor(acct.missionSet);
  const progress = acct.missionProgress.slice();
  list.forEach((m, i) => {
    const v = replay[m.stat] || 0;
    progress[i] = m.single ? Math.max(progress[i], v) : progress[i] + v;
  });
  let next = { ...acct, missionProgress: progress };
  const done = list.every((m, i) => progress[i] >= m.target);
  if (done) {
    next = {
      ...next,
      mult: Math.min(SIM.MAX_MULT, next.mult + 1),
      keys: next.keys + 1,
      coins: next.coins + 250,
      missionSet: next.missionSet + 1,
      missionProgress: [0, 0, 0],
      lastSetDone: next.missionSet + 1,
    };
  }
  return next;
}

export function applyAction(state, playerId, action) {
  let acct = { ...state.accounts[playerId] };

  if (action.kind === "submitRun") {
    const replay = SIM.simulateRun(action.seed, loadout(acct), action.inputs);
    acct.points += replay.score;
    acct.lifetime += replay.score;
    acct.coins += replay.coins;
    acct.boards -= replay.boardsUsed;
    acct.keys -= replay.keysUsed;
    acct.runs += 1;
    acct.distance += replay.distance;
    acct.stores += replay.stores;
    if (replay.score > acct.bestRun) acct.bestRun = replay.score;
    for (const n of replay.sightings) {
      if (acct.sightings.indexOf(n) < 0) acct.sightings = [...acct.sightings, n];
    }
    acct.seenRuns = [...acct.seenRuns, action.runId].slice(-SEEN_CAP);
    const setBefore = acct.missionSet;
    acct = advanceMissions(acct, replay);
    acct.lastRun = { ...replay, awarded: replay.score, setDone: acct.missionSet > setBefore };
    acct.seed = advanceSeed(acct.seed);
  }

  if (action.kind === "claimCoupon") {
    acct.points -= COUPON_POINTS;
    acct.claimed += 1;
    acct.coupons = [
      ...acct.coupons,
      {
        // In production the coupon service issues this (see coupon-service/);
        // the static build mints it locally with the same generator.
        code: generateCode(),
        percent: COUPON.percent,
        minOrder: COUPON.minOrder,
        maxOff: COUPON.maxOff,
        validDays: COUPON.validDays,
        afterRun: acct.runs,
        status: "active",
      },
    ];
  }

  if (action.kind === "buy") {
    const what = action.item;
    if (what === "board") {
      acct.coins -= PRICES.board;
      acct.boards += 1;
    } else if (what === "key") {
      acct.coins -= PRICES.key;
      acct.keys += 1;
    } else {
      const lv = acct.levels[what];
      acct.coins -= PRICES.upgrade[lv];
      acct.levels = { ...acct.levels, [what]: lv + 1 };
    }
  }

  if (action.kind === "grantTestPoints") {
    acct.lifetime += COUPON_POINTS - acct.points;
    acct.points = COUPON_POINTS;
    acct.testCredits += 1;
  }

  if (action.kind === "setOutfit") acct.outfit = action.outfit;

  acct = withOutfits(acct);
  return { ...state, accounts: { ...state.accounts, [playerId]: acct } };
}

/** An endless progression game: a run ends, the account never does. */
export function isGameOver() {
  return { over: false };
}

/** Only this is ever sent to that player. */
export function viewFor(state, playerId) {
  const acct = state.accounts[playerId];
  if (!acct) return null;
  const need = COUPON_POINTS;
  const missions = missionsFor(acct.missionSet).map((m, i) => ({
    label: missionLabel(m),
    have: Math.min(m.target, acct.missionProgress[i]),
    target: m.target,
    done: acct.missionProgress[i] >= m.target,
  }));
  return {
    account: playerId,
    points: acct.points,
    lifetime: acct.lifetime,
    coins: acct.coins,
    boards: acct.boards,
    keys: acct.keys,
    levels: acct.levels,
    mult: acct.mult,
    maxMult: SIM.MAX_MULT,
    missionSet: acct.missionSet + 1,
    missions,
    prices: PRICES,
    bestRun: acct.bestRun,
    runs: acct.runs,
    distance: acct.distance,
    stores: acct.stores,
    landmarks: acct.sightings.map((i) => SIM.LANDMARKS[i]),
    landmarkTotal: SIM.LANDMARKS.length,
    outfit: acct.outfit,
    outfits: acct.outfits,
    outfitList: OUTFITS,
    coupons: acct.coupons,
    activeCoupons: acct.coupons.filter((c) => c.status === "active").length,
    claimReady: acct.points >= need,
    testMode: TEST_GRANTS,
    testCredits: acct.testCredits,
    nextSeed: acct.seed,
    loadout: loadout(acct),
    terms: { points: need, ...COUPON },
    lastRun: acct.lastRun,
  };
}

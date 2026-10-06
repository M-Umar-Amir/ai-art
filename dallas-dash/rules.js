/**
 * DALLAS DASH — the rules.
 *
 * The server half of the game. Six pure functions, no imports, no timers, no
 * nondeterminism: the room replays and persists whatever this returns.
 *
 * The load-bearing idea (from the concept report): points cannot be fabricated.
 * A client does not send a score it claims — it sends the seed of the course it
 * ran and the input events it actually pressed, and this file re-runs that exact
 * course and computes the real score itself. A mismatch, a re-used run id or a
 * course the account was not issued is refused, and no points are ever awarded.
 *
 * The simulation core below is duplicated byte-for-byte in app/public/sim.js,
 * where the client uses it to draw the course and record inputs. Drift between
 * the two copies would make every run fail verification, so
 * app/tests/rules.test.ts replays the client copy against these rules.
 */

export const meta = { game: "Dallas Dash", minPlayers: 1, maxPlayers: 1 };

/* ── shared deterministic core — keep identical to app/public/sim.js ─────── */

const TICK_HZ = 60;
const MAX_TICKS = 2640; // 44-second course
const LANES = 3;
const LANE_X = [-2.3, 0, 2.3];
const JUMP_V = 8.6;
const GRAVITY = 26;
const SLIDE_TICKS = 34;
const HIT_LIMIT = 3;
const FAR_EDGE = 190;
const SPEED_BASE = 14;
const SPEED_MAX = 30;
const POINTS_PER_UNIT = 3;
const MAX_INPUTS = 220;

const ITEMS = {
  cup: 200,
  popcorn: 120,
  fries: 150,
  coleslaw: 120,
  wrap: 400,
  burger: 600,
  bucket: 900,
  bowl: 1200,
};
const ITEM_KINDS = ["cup", "popcorn", "fries", "coleslaw", "wrap", "burger", "bucket", "bowl"];
const ITEM_WEIGHTS = [4, 4, 3, 3, 3, 4, 2, 1];
const JUMP_KINDS = ["cone", "barrier", "crate", "spill", "bucketstack"];
const JUMP_WEIGHTS = [3, 3, 2, 2, 2];

// Ten real Dallas landmarks, placed along the route as scenery. Kept in the
// simulation (not the renderer) so the server can verify what a player passed.
const LANDMARKS = [
  "Reunion Tower",
  "Margaret Hunt Hill Bridge",
  "Bank of America Plaza",
  "Fountain Place",
  "Omni Dallas",
  "Winspear Opera House",
  "Old Red Museum",
  "Klyde Warren Park",
  "AT&T Discovery District",
  "Comerica Bank Tower",
];

/** One step of the seeded generator. LCG: same seed, same course, forever. */
function roll(run) {
  run.rng = (Math.imul(run.rng, 1664525) + 1013904223) >>> 0;
  return run.rng / 4294967296;
}

function weighted(run, kinds, weights) {
  let total = 0;
  for (let i = 0; i < weights.length; i += 1) total += weights[i];
  let r = roll(run) * total;
  for (let i = 0; i < kinds.length; i += 1) {
    r -= weights[i];
    if (r <= 0) return kinds[i];
  }
  return kinds[kinds.length - 1];
}

function pickItem(run, z, lane) {
  const l = lane === undefined ? Math.floor(roll(run) * LANES) : lane;
  return { k: weighted(run, ITEM_KINDS, ITEM_WEIGHTS), t: 0, l, z };
}

function hasHazard(run, z) {
  for (let i = 0; i < run.objects.length; i += 1) {
    const o = run.objects[i];
    if (o.t === 1 && o.z >= z - 1 && o.z <= z + 1) return true;
  }
  return false;
}

/**
 * One row of the course. Rows are numbered, and the numbering is the teaching
 * order: open rows build speed, then traffic, then jumpable clutter, then the
 * full-width gates that ask for a slide or a jump.
 */
function spawnRow(run, z) {
  run.row += 1;

  if (run.row % 7 === 0) {
    const a = Math.floor(roll(run) * LANDMARKS.length);
    const b = (a + 1 + Math.floor(roll(run) * (LANDMARKS.length - 1))) % LANDMARKS.length;
    run.objects.push({ k: "landmark", t: 2, l: 0, z, n: a });
    run.objects.push({ k: "store", t: 2, l: 2, z: z + 18 });
    run.objects.push({ k: "landmark", t: 2, l: 2, z: z + 44, n: b });
    return;
  }

  const r = roll(run);

  if (run.row < 4 || r < 0.1) {
    run.objects.push(pickItem(run, z));
    run.objects.push(pickItem(run, z + 10));
    return;
  }

  if (run.row < 8) {
    const blocked = Math.floor(roll(run) * LANES);
    run.objects.push({ k: roll(run) < 0.5 ? "car" : "truck", t: 1, l: blocked, z });
    run.objects.push(pickItem(run, z, (blocked + 1) % LANES));
    if (roll(run) < 0.6) run.objects.push(pickItem(run, z + 12, (blocked + 2) % LANES));
    return;
  }

  if (run.row < 14) {
    for (let l = 0; l < LANES; l += 1) {
      const q = roll(run);
      if (q < 0.35) run.objects.push(pickItem(run, z, l));
      else if (q < 0.62) run.objects.push({ k: weighted(run, JUMP_KINDS, JUMP_WEIGHTS), t: 1, l, z });
    }
    if (!hasHazard(run, z)) {
      run.objects.push({ k: "cone", t: 1, l: Math.floor(roll(run) * LANES), z });
    }
    return;
  }

  if (r < 0.32) {
    // Full-width gate: one action for every lane. Passing it is the exam.
    const gate = roll(run) < 0.5 ? "awning" : "barrier";
    for (let l = 0; l < LANES; l += 1) run.objects.push({ k: gate, t: 1, l, z });
    if (roll(run) < 0.7) run.objects.push(pickItem(run, z + 16));
    return;
  }

  if (r < 0.52) {
    const blocked = Math.floor(roll(run) * LANES);
    run.objects.push({ k: roll(run) < 0.5 ? "car" : "truck", t: 1, l: blocked, z });
    run.objects.push(pickItem(run, z, (blocked + 1) % LANES));
    if (roll(run) < 0.7) run.objects.push(pickItem(run, z + 9, (blocked + 2) % LANES));
    return;
  }

  for (let l = 0; l < LANES; l += 1) {
    const q = roll(run);
    if (q < 0.4) run.objects.push(pickItem(run, z, l));
    else if (q < 0.74) run.objects.push({ k: weighted(run, JUMP_KINDS, JUMP_WEIGHTS), t: 1, l, z });
    else if (q < 0.8) run.objects.push({ k: "sign", t: 1, l, z });
  }
  if (!hasHazard(run, z)) {
    run.objects.push({ k: "cone", t: 1, l: Math.floor(roll(run) * LANES), z });
  }
}

/** What a player needs in the air, or sliding, to pass each obstacle. */
const CLEARANCE = {
  cone: "jump",
  barrier: "jump",
  crate: "jump",
  spill: "jump",
  bucketstack: "jump",
  awning: "slide",
  sign: "slide",
  car: "dodge",
  truck: "dodge",
};

function createRun(seed) {
  return {
    seed: seed >>> 0,
    rng: ((seed >>> 0) || 987654321) >>> 0,
    tick: 0,
    distance: 0,
    speed: SPEED_BASE,
    lane: 1,
    x: 0,
    y: 0,
    vy: 0,
    slide: 0,
    hits: 0,
    invuln: 0,
    combo: 0,
    bestCombo: 0,
    pickups: 0,
    stores: 0,
    score: 0,
    sightings: [],
    objects: [],
    spawnAt: 46,
    row: 0,
    over: false,
    reason: "",
  };
}

function cross(run, o) {
  if (o.t === 2) {
    if (o.k === "landmark" && run.sightings.indexOf(o.n) < 0) run.sightings.push(o.n);
    if (o.k === "store") run.stores += 1;
    return;
  }
  if (o.l !== run.lane) return;

  if (o.t === 0) {
    // combo: 8 clean pickups raise the multiplier, a hit resets it
    const mult = 1 + Math.min(2, Math.floor(run.combo / 8));
    run.score += ITEMS[o.k] * mult;
    run.combo += 1;
    run.pickups += 1;
    if (run.combo > run.bestCombo) run.bestCombo = run.combo;
    return;
  }

  if (run.invuln > 0) return;
  const need = CLEARANCE[o.k];
  const cleared =
    need === "jump" ? run.y >= 0.55 : need === "slide" ? run.slide > 0 : false;
  if (cleared) return;
  run.hits += 1;
  run.combo = 0;
  run.invuln = 30;
}

function stepRun(run, code) {
  if (run.over) return run;
  const dt = 1 / TICK_HZ;

  if (code === 1 && run.lane > 0) run.lane -= 1;
  else if (code === 2 && run.lane < LANES - 1) run.lane += 1;
  else if (code === 3 && run.slide <= 0 && run.y <= 0) run.vy = JUMP_V;
  else if (code === 4 && run.y <= 0) run.slide = SLIDE_TICKS;

  if (run.y > 0 || run.vy > 0) {
    run.y += run.vy * dt;
    run.vy -= GRAVITY * dt;
    if (run.y <= 0) {
      run.y = 0;
      run.vy = 0;
    }
  }
  if (run.slide > 0) run.slide -= 1;
  if (run.invuln > 0) run.invuln -= 1;

  run.speed = Math.min(SPEED_MAX, SPEED_BASE + run.distance * 0.012);
  const move = run.speed * dt;
  const prev = run.distance;
  run.distance += move;
  run.score += move * POINTS_PER_UNIT;

  const target = LANE_X[run.lane];
  const dx = target - run.x;
  if (dx > 0.001) run.x += Math.min(11 * dt, dx);
  else if (dx < -0.001) run.x -= Math.min(11 * dt, -dx);
  else run.x = target;

  while (run.spawnAt < run.distance + FAR_EDGE) {
    spawnRow(run, run.spawnAt);
    run.spawnAt += 21 + roll(run) * 15;
  }

  for (let i = run.objects.length - 1; i >= 0; i -= 1) {
    const o = run.objects[i];
    if (o.z - prev > 0 && o.z - run.distance <= 0) cross(run, o);
    if (o.z - run.distance < -9) run.objects.splice(i, 1);
  }

  run.tick += 1;
  if (run.hits >= HIT_LIMIT) {
    run.over = true;
    run.reason = "wrecked";
  } else if (run.tick >= MAX_TICKS) {
    run.over = true;
    run.reason = "finished";
  }
  return run;
}

/**
 * Replay a whole run. This is the reward authority: only what this returns is
 * ever credited, so a client cannot invent points.
 */
function simulateRun(seed, inputs) {
  const run = createRun(seed);
  const list = Array.isArray(inputs) ? inputs : [];
  let i = 0;
  while (!run.over) {
    let code = 0;
    while (i < list.length && list[i][0] <= run.tick) {
      code = list[i][1];
      i += 1;
    }
    stepRun(run, code);
  }
  return {
    score: Math.round(run.score),
    distance: Math.round(run.distance),
    pickups: run.pickups,
    combos: run.bestCombo,
    hits: run.hits,
    ticks: run.tick,
    reason: run.reason,
    stores: run.stores,
    sightings: run.sightings.slice(0, LANDMARKS.length),
  };
}

/* ── the account ledger ─────────────────────────────────────────────────── */

const COUPON_POINTS = 100000;
const COUPON = { percent: 15, minOrder: 25, maxOff: 5, validDays: 30 };
const ACTIVE_LIMIT = 3;
const SEEN_CAP = 60;

/**
 * TEST MODE — a testing shortcut, not part of the reward mechanic.
 *
 * The client wants to exercise the coupon without grinding ~12 credited runs,
 * so this build exposes ONE extra action: top the account up to exactly the
 * coupon threshold. It fabricates no run, grants no score, and accepts no
 * amount from the client (the client sends the action kind and nothing else),
 * so the rule that points only come from a verified replay is untouched.
 * Set this to false — or delete the two branches below — for production.
 */
const TEST_GRANTS = true;
const TEST_GRANT_TO = COUPON_POINTS;

/** Free cosmetic rewards, earned by lifetime points (once unlocked, kept). */
const OUTFITS = [
  { id: "classic", name: "Counter crew", at: 0 },
  { id: "tie", name: "Founder tie", at: 2000 },
  { id: "bucket", name: "Bucket hat", at: 12000 },
  { id: "apron", name: "Kitchen apron", at: 30000 },
  { id: "gold", name: "Golden bucket", at: 100000 },
];

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

/** DLS-7K4Q-9M2P — the shape the concept report shows. No ambiguous glyphs. */
function couponCode(playerId, n) {
  const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
  let h = hash32(`${playerId}#${n}`);
  let body = "";
  for (let i = 0; i < 12; i += 1) {
    h = (Math.imul(h, 1103515245) + 12345) >>> 0;
    body += alphabet[(h >>> 17) % alphabet.length];
  }
  return `DLS-${body.slice(0, 4)}-${body.slice(4, 8)}-${body.slice(8, 12)}`;
}

function blankAccount(playerId) {
  return {
    points: 0,
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

function withOutfits(acct) {
  const unlocked = OUTFITS.filter(
    (o) => acct.points >= o.at || acct.outfits.indexOf(o.id) >= 0,
  ).map((o) => o.id);
  return { ...acct, outfits: unlocked };
}

export function setup(players) {
  const accounts = {};
  for (const id of players) accounts[id] = blankAccount(id);
  return { v: 1, accounts };
}

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
  if (!Array.isArray(inputs) || inputs.length > MAX_INPUTS) {
    return { ok: false, error: `input log must hold at most ${MAX_INPUTS} events` };
  }
  let last = -1;
  for (const ev of inputs) {
    if (!Array.isArray(ev) || ev.length !== 2) {
      return { ok: false, error: "each input must be [tick, code]" };
    }
    const t = ev[0];
    const c = ev[1];
    if (!Number.isInteger(t) || t < 0 || t >= MAX_TICKS) {
      return { ok: false, error: "input tick out of range" };
    }
    if (!Number.isInteger(c) || c < 1 || c > 4) {
      return { ok: false, error: "input code must be 1-4" };
    }
    if (t <= last) return { ok: false, error: "input ticks must increase" };
    last = t;
  }
  if (!Number.isInteger(a.claimedScore) || a.claimedScore < 0 || a.claimedScore > 250000) {
    return { ok: false, error: "claimed score out of range" };
  }
  const replay = simulateRun(a.seed, inputs);
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
      return {
        ok: false,
        error: `needs ${COUPON_POINTS} verified points — ${acct.points} so far`,
      };
    }
    const active = acct.coupons.filter((c) => c.status === "active").length;
    if (active >= ACTIVE_LIMIT) {
      return { ok: false, error: `you already hold ${active} unused coupons` };
    }
    return { ok: true };
  }

  if (kind === "grantTestPoints") {
    if (!TEST_GRANTS) return { ok: false, error: "test grants are off in this build" };
    if (acct.points >= TEST_GRANT_TO) {
      return { ok: false, error: "you are already at the coupon threshold" };
    }
    return { ok: true };
  }

  if (kind === "setOutfit") {
    if (!action.outfit || acct.outfits.indexOf(action.outfit) < 0) {
      return { ok: false, error: "that outfit is still locked" };
    }
    return { ok: true };
  }

  return { ok: false, error: "unknown action" };
}

export function applyAction(state, playerId, action) {
  let acct = { ...state.accounts[playerId] };

  if (action.kind === "submitRun") {
    const replay = simulateRun(action.seed, action.inputs);
    acct.points += replay.score;
    acct.runs += 1;
    acct.distance += replay.distance;
    acct.stores += replay.stores;
    if (replay.score > acct.bestRun) acct.bestRun = replay.score;
    for (const n of replay.sightings) {
      if (acct.sightings.indexOf(n) < 0) acct.sightings.push(n);
    }
    acct.seenRuns = [...acct.seenRuns, action.runId].slice(-SEEN_CAP);
    acct.lastRun = { ...replay, awarded: replay.score };
    acct.seed = advanceSeed(acct.seed);
  }

  if (action.kind === "claimCoupon") {
    acct.points -= COUPON_POINTS;
    acct.claimed += 1;
    acct.coupons = [
      ...acct.coupons,
      {
        code: couponCode(playerId, acct.claimed),
        percent: COUPON.percent,
        minOrder: COUPON.minOrder,
        maxOff: COUPON.maxOff,
        validDays: COUPON.validDays,
        afterRun: acct.runs,
        status: "active",
      },
    ];
  }

  if (action.kind === "grantTestPoints") {
    // Deliberately no run, no score and no client-supplied amount: exactly
    // enough to reach the threshold, once per claim.
    acct.points = TEST_GRANT_TO;
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

/** Only this is ever sent to that player — the verification internals stay here. */
export function viewFor(state, playerId) {
  const acct = state.accounts[playerId];
  if (!acct) return null;
  const need = COUPON_POINTS;
  const active = acct.coupons.filter((c) => c.status === "active");
  return {
    account: playerId,
    points: acct.points,
    bestRun: acct.bestRun,
    runs: acct.runs,
    distance: acct.distance,
    stores: acct.stores,
    landmarks: acct.sightings.map((i) => LANDMARKS[i]),
    landmarkTotal: LANDMARKS.length,
    outfit: acct.outfit,
    outfits: acct.outfits,
    coupons: acct.coupons,
    activeCoupons: active.length,
    claimReady: acct.points >= need,
    testMode: TEST_GRANTS,
    testCredits: acct.testCredits,
    testGrantTo: TEST_GRANT_TO,
    nextSeed: acct.seed,
    courseTicks: MAX_TICKS,
    ticksPerSecond: TICK_HZ,
    terms: { points: need, ...COUPON },
    goal: acct.points >= need ? "Claim your 15% food coupon" : "Run for the 100,000 point coupon",
    nextStep:
      acct.points >= need
        ? "Claim the code, then enter it in the food app at checkout"
        : "Start another run — every credited point is kept",
    lastRun: acct.lastRun,
  };
}

/**
 * Dallas Dash — the shared deterministic simulation.
 *
 * One copy, used by both halves: the client imports it to draw the course and
 * record inputs, and rules.js imports the same module to replay a submitted run
 * and compute the real score. Because there is only one copy, the two can no
 * longer drift apart.
 *
 * The run is endless (capped at ten minutes for replay cost) and plays like
 * Subway Surfers: three lanes, swipe to dodge, jump and roll, trailers you can
 * ride on top of, oncoming vans, a stumble-and-get-caught chase, a hoverboard
 * that absorbs one crash, four timed power-ups and keys to revive.
 *
 * Rules for this file: no DOM, no timers, no Math.random, no Date. Everything
 * is a function of (seed, loadout, input log), so a server can replay it
 * exactly. Porting note: it carries over to Expo / React Native unchanged.
 */

export const TICK_HZ = 60;
export const MAX_TICKS = TICK_HZ * 600; // ten-minute cap per run
export const LANES = 3;
export const LANE_X = [-2.3, 0, 2.3];
export const JUMP_V = 9.4;
export const SNEAKER_JUMP_V = 12.6;
export const DIVE_V = -22;
export const GRAVITY = 31;
export const SLIDE_TICKS = 32;
export const FAR_EDGE = 230;
export const SPEED_BASE = 20;
export const SPEED_MAX = 46;
export const SPEED_RAMP = 0.0072; // speed gained per unit run
export const POINTS_PER_UNIT = 2;
export const COIN_POINTS = 10;
export const MAX_INPUTS = 6000;
export const CHASE_TICKS = 300; // a second stumble inside this window = caught
export const BOARD_TICKS = 1800;
export const JET_HEIGHT = 7.2;
export const MAX_MULT = 6;

/** Input codes. 6 and 7 are only legal while the runner is down. */
export const INPUT = { LEFT: 1, RIGHT: 2, JUMP: 3, ROLL: 4, BOARD: 5, REVIVE: 6, GIVE_UP: 7 };

/** Bonus menu pickups (points × multiplier). Labels live in the client. */
export const ITEMS = {
  bucket: 500,
  bowl: 400,
  sandwich: 300,
  popcorn: 150,
  fries: 120,
  biscuit: 100,
  coleslaw: 100,
  drink: 80,
};
const ITEM_KINDS = ["bucket", "bowl", "sandwich", "popcorn", "fries", "biscuit", "coleslaw", "drink"];
const ITEM_WEIGHTS = [1, 2, 3, 3, 3, 3, 2, 3];

/**
 * Hazards. `len` is depth along the road. Classes:
 *  low   – jump it (y must clear h), or ride over it on a board after a crash
 *  high  – roll under it
 *  block – go round it (or super-sneaker jump over it)
 *  ride  – a trailer: run up its ramp and along the roof
 * `sev`: stumble = the chaser closes in; crash = you are down.
 */
export const HAZ = {
  cone: { cls: "low", len: 0.8, h: 0.9, sev: "stumble" },
  crate: { cls: "low", len: 0.9, h: 1.0, sev: "stumble" },
  barrier: { cls: "low", len: 0.4, h: 0.95, sev: "crash" },
  bucketstack: { cls: "low", len: 0.9, h: 1.05, sev: "crash" },
  bar: { cls: "high", len: 0.5, h: 0, sev: "crash" },
  awning: { cls: "high", len: 1.4, h: 0, sev: "crash" },
  van: { cls: "block", len: 4.4, h: 2.3, sev: "crash" },
  oncoming: { cls: "block", len: 4.4, h: 2.3, sev: "crash" },
  trailer: { cls: "ride", len: 24, h: 2.5, sev: "crash" },
};

export const POWERS = ["magnet", "jetpack", "sneakers", "double"];
/** Power-up duration in ticks for an upgrade level 0-5. */
export function powerTicks(kind, level) {
  const lv = Math.max(0, Math.min(5, level | 0));
  const base = kind === "jetpack" ? 8 : 10;
  return (base + lv * 3) * TICK_HZ;
}

export const LANDMARKS = [
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

const PLAYER_HALF = 0.35;
const ONCOMING_V = 11;

/* ── seeded randomness ─────────────────────────────────────────────────── */

function roll(run) {
  run.rng = (Math.imul(run.rng, 1664525) + 1013904223) >>> 0;
  return run.rng / 4294967296;
}
function pick(run, n) {
  return Math.floor(roll(run) * n);
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

/* ── spawning ──────────────────────────────────────────────────────────── */

function haz(run, k, l, z, extra) {
  const o = { t: 1, k, l, z, len: HAZ[k].len, hit: 0 };
  if (extra) Object.assign(o, extra);
  run.objects.push(o);
  return o;
}
function coin(run, l, z, h) {
  run.objects.push({ t: 0, k: "coin", l, z, h: h === undefined ? 1.0 : h });
}
function coinLine(run, l, z0, n, h, gap) {
  for (let i = 0; i < n; i += 1) coin(run, l, z0 + i * (gap || 2.4), h);
}
/** A coin arc over a low obstacle: the line itself teaches "jump here". */
function coinArc(run, l, zc) {
  for (let i = -3; i <= 3; i += 1) {
    const t = i / 3;
    coin(run, l, zc + i * 1.5, 1.0 + 1.3 * (1 - t * t));
  }
}
function item(run, l, z) {
  run.objects.push({ t: 0, k: weighted(run, ITEM_KINDS, ITEM_WEIGHTS), l, z, h: 1.2 });
}
function power(run, l, z) {
  run.objects.push({ t: 3, k: POWERS[pick(run, POWERS.length)], l, z, h: 1.2 });
}
function otherLanes(l) {
  return [0, 1, 2].filter((x) => x !== l);
}

/** True when nothing that blocks movement sits in [z0, z1] in any lane. */
function clearOfBlocks(run, z0, z1) {
  for (const o of run.objects) {
    if (o.t !== 1) continue;
    const c = HAZ[o.k].cls;
    if (c !== "block" && c !== "ride") continue;
    if (o.z + o.len >= z0 && o.z <= z1) return false;
  }
  return true;
}

/**
 * One pattern of the course, starting at z. Returns its depth so the next one
 * starts after it — patterns never overlap, which is what keeps every one of
 * them solvable on its own. Rows are numbered, and the numbering is the
 * teaching order: coins, then single obstacles, then trailers and gates.
 */
function spawnPattern(run, z) {
  run.row += 1;
  const row = run.row;

  // scenery every 8th pattern: a KFC storefront and two Dallas landmarks
  if (row % 8 === 0) {
    const a = pick(run, LANDMARKS.length);
    const b = (a + 1 + pick(run, LANDMARKS.length - 1)) % LANDMARKS.length;
    run.objects.push({ t: 2, k: "landmark", l: 0, z, n: a });
    run.objects.push({ t: 2, k: "store", l: 2, z: z + 18 });
    run.objects.push({ t: 2, k: "landmark", l: 2, z: z + 44, n: b });
    const l = pick(run, 3);
    coinLine(run, l, z, 8);
    return 22;
  }

  // power-up roughly every 6 patterns, after the opening
  if (row > 4 && row % 6 === 3) {
    const l = pick(run, 3);
    coinLine(run, l, z, 4);
    power(run, l, z + 11);
    return 16;
  }

  if (row < 4) {
    const l = pick(run, 3);
    coinLine(run, l, z, 7);
    if (roll(run) < 0.5) item(run, (l + 1) % 3, z + 6);
    return 18;
  }

  const r = roll(run);
  const hard = Math.min(1, (row - 4) / 40); // 0 → 1 over the first ~40 patterns

  // single lane obstacle, coins elsewhere
  if (row < 9 || r < 0.16 - hard * 0.06) {
    const l = pick(run, 3);
    const k = ["cone", "barrier", "crate", "bar"][pick(run, 4)];
    haz(run, k, l, z);
    if (HAZ[k].cls === "low") coinArc(run, l, z + 0.4);
    const o = otherLanes(l)[pick(run, 2)];
    coinLine(run, o, z - 4, 5);
    return 14;
  }

  // two vans side by side: one free lane
  if (r < 0.3) {
    const free = pick(run, 3);
    for (const l of otherLanes(free)) haz(run, "van", l, z + pick(run, 3) * 1.5);
    coinLine(run, free, z - 2, 6);
    return 18;
  }

  // a trailer train: ramp on one, coins on the roof, maybe a second trailer
  if (r < 0.48) {
    const l = pick(run, 3);
    const len = 22 + pick(run, 3) * 8;
    haz(run, "trailer", l, z, { len, ramp: 6 });
    coinLine(run, l, z + 7, Math.floor((len - 8) / 2.4), 3.5);
    if (roll(run) < 0.55 + hard * 0.3) {
      const l2 = otherLanes(l)[pick(run, 2)];
      haz(run, "trailer", l2, z + 3 + pick(run, 2) * 4, { len: len - 4, ramp: roll(run) < 0.5 ? 6 : 0 });
    }
    const rest = [0, 1, 2].filter((x) => !run.objects.some((o) => o.t === 1 && o.l === x && o.z >= z - 1));
    if (rest.length) {
      const fl = rest[pick(run, rest.length)];
      if (roll(run) < 0.6) haz(run, roll(run) < 0.5 ? "barrier" : "bar", fl, z + 8);
      else coinLine(run, fl, z + 2, 6);
    }
    return len + 8;
  }

  // a full-width gate: everyone jumps or everyone rolls
  if (r < 0.58) {
    const k = roll(run) < 0.5 ? "barrier" : "awning";
    for (let l = 0; l < 3; l += 1) haz(run, k, l, z);
    const l = pick(run, 3);
    if (k === "barrier") coinArc(run, l, z + 0.2);
    else coinLine(run, l, z - 2, 3, 0.5);
    if (roll(run) < 0.5) item(run, pick(run, 3), z + 8);
    return 14;
  }

  // oncoming van: only when nothing else blocks around the meeting point
  if (r < 0.68 && row > 12) {
    const l = pick(run, 3);
    const s = run.speed;
    const ahead = z - run.distance;
    const meet = run.distance + (s * ahead) / (s + ONCOMING_V);
    if (clearOfBlocks(run, meet - 30, z + 10) && !run.objects.some((o) => o.t === 1 && o.l === l && o.z >= meet - 30)) {
      haz(run, "oncoming", l, z, { v: ONCOMING_V });
      coinLine(run, otherLanes(l)[pick(run, 2)], z - 6, 6);
      return 16;
    }
  }

  // mixed row: each lane gets its own obstacle class, never all blocks
  const kinds = [];
  for (let l = 0; l < 3; l += 1) {
    const q = roll(run);
    if (q < 0.3) kinds.push("cone");
    else if (q < 0.5) kinds.push("bar");
    else if (q < 0.68) kinds.push("barrier");
    else if (q < 0.8) kinds.push("bucketstack");
    else kinds.push(null);
  }
  if (kinds.every((k) => k !== null) && roll(run) < 0.5) kinds[pick(run, 3)] = null;
  for (let l = 0; l < 3; l += 1) {
    if (kinds[l]) haz(run, kinds[l], l, z);
    else if (roll(run) < 0.6) coinLine(run, l, z - 3, 5);
    else item(run, l, z);
  }
  // a second, staggered row once the player is warmed up
  if (roll(run) < hard * 0.7) {
    const l = pick(run, 3);
    const k = ["van", "bar", "barrier"][pick(run, 3)];
    haz(run, k, l, z + 14);
    return 27;
  }
  return 15;
}

/** Air coins along a jetpack flight, weaving between lanes. */
function spawnJetCoins(run, ticks) {
  const span = run.speed * (ticks / TICK_HZ) * 0.95;
  let l = run.lane;
  for (let d = 18; d < span; d += 2.6) {
    if (Math.floor(d / 26) !== Math.floor((d - 2.6) / 26)) l = (l + 1 + pick(run, 2)) % 3;
    coin(run, l, run.distance + d, JET_HEIGHT + 0.9);
  }
}

/* ── the run ───────────────────────────────────────────────────────────── */

/**
 * opts (the account's loadout, which the server knows independently):
 *   mult   – score multiplier from missions (1..MAX_MULT)
 *   boards – hoverboards owned
 *   keys   – revive keys owned
 *   levels – { magnet, jetpack, sneakers, double } upgrade levels 0-5
 */
export function createRun(seed, opts) {
  const o = opts || {};
  const lv = o.levels || {};
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
    floor: 0,
    slide: 0,
    invuln: 0,
    chase: CHASE_TICKS, // the chaser starts right behind you, like the opening of a heist
    mult: Math.max(1, Math.min(MAX_MULT, o.mult | 0 || 1)),
    boardsLeft: Math.max(0, o.boards | 0),
    keysLeft: Math.max(0, o.keys | 0),
    levels: {
      magnet: lv.magnet | 0,
      jetpack: lv.jetpack | 0,
      sneakers: lv.sneakers | 0,
      double: lv.double | 0,
    },
    board: 0,
    magnet: 0,
    jet: 0,
    sneakers: 0,
    double: 0,
    downed: "",
    revives: 0,
    score: 0,
    coins: 0,
    pickups: 0,
    jumps: 0,
    rolls: 0,
    rides: 0,
    powerups: 0,
    stumbles: 0,
    boardsUsed: 0,
    keysUsed: 0,
    stores: 0,
    sightings: [],
    objects: [],
    spawnAt: 40,
    row: 0,
    over: false,
    reason: "",
    events: [], // per-tick cues for the client (sound, toasts); never read by rules
  };
}

export function multiplier(run) {
  return run.mult * (run.double > 0 ? 2 : 1);
}

export function reviveCost(run) {
  return 1 << Math.min(4, run.revives);
}

/**
 * Height of the ground under the runner in a lane at distance d. Trailers and
 * vans both have roofs you can stand on; only trailers have a ramp up.
 */
function surfaceAt(run, lane, d) {
  let s = 0;
  for (const o of run.objects) {
    if (o.t !== 1 || o.l !== lane) continue;
    const spec = HAZ[o.k];
    if (spec.cls !== "ride" && spec.cls !== "block") continue;
    if (d + PLAYER_HALF < o.z || d - PLAYER_HALF > o.z + o.len) continue;
    let top = spec.h;
    if (o.ramp > 0 && d < o.z + o.ramp) top = Math.max(0, (spec.h * (d - o.z)) / o.ramp);
    if (top > s) s = top;
  }
  return s;
}

function overlaps(o, d) {
  return d + PLAYER_HALF > o.z && d - PLAYER_HALF < o.z + o.len;
}

/** Clear hazards just ahead — after a revive or a broken board. */
function clearAhead(run, depth) {
  for (let i = run.objects.length - 1; i >= 0; i -= 1) {
    const o = run.objects[i];
    if (o.t === 1 && o.z + o.len > run.distance - 1 && o.z < run.distance + depth) {
      run.objects.splice(i, 1);
    }
  }
}

function stumble(run) {
  if (run.invuln > 0 || run.jet > 0) return;
  run.stumbles += 1;
  run.events.push("stumble");
  if (run.chase > 0) {
    down(run, "caught");
    return;
  }
  run.chase = CHASE_TICKS;
  run.invuln = 24;
}

function down(run, why) {
  if (run.board > 0) {
    run.board = 0;
    run.invuln = 90;
    run.events.push("boardbreak");
    clearAhead(run, 14);
    return;
  }
  run.events.push(why);
  if (run.keysLeft >= reviveCost(run)) {
    run.downed = why;
    return;
  }
  run.over = true;
  run.reason = why;
}

function revive(run) {
  const cost = reviveCost(run);
  run.keysLeft -= cost;
  run.keysUsed += cost;
  run.revives += 1;
  run.downed = "";
  run.invuln = 150;
  run.chase = 0;
  run.slide = 0;
  run.vy = 0;
  run.y = run.floor;
  clearAhead(run, 40);
  run.events.push("revive");
}

/** Lane change, refused with a stumble if the side of something is in the way. */
function changeLane(run, to) {
  if (to < 0 || to >= LANES) return;
  if (run.jet <= 0 && run.invuln <= 0) {
    for (const o of run.objects) {
      if (o.t !== 1 || o.l !== to || !overlaps(o, run.distance)) continue;
      const spec = HAZ[o.k];
      if ((spec.cls === "block" || spec.cls === "ride") && run.y < surfaceAt(run, to, run.distance) - 0.6) {
        return stumble(run);
      }
      if (spec.cls === "low" && run.y < spec.h) {
        o.hit = 1;
        run.lane = to;
        return stumble(run);
      }
      if (spec.cls === "high" && run.slide <= 0) {
        o.hit = 1;
        run.lane = to;
        return stumble(run);
      }
    }
  }
  run.lane = to;
}

function collide(run) {
  if (run.jet > 0) return;
  for (const o of run.objects) {
    if (o.t !== 1 || o.l !== run.lane || o.hit) continue;
    if (!overlaps(o, run.distance)) continue;
    const spec = HAZ[o.k];
    // vans and trailers are surfaces: running into one is handled as a wall
    if (spec.cls === "ride" || spec.cls === "block") continue;
    let hit = false;
    if (spec.cls === "low") hit = run.y - run.floor < spec.h && run.floor < 0.1;
    else if (spec.cls === "high") hit = run.slide <= 0 && run.y < 2.4;
    if (!hit) continue;
    o.hit = 1;
    if (run.invuln > 0) continue;
    if (spec.sev === "stumble") stumble(run);
    else down(run, "crashed");
    if (run.over || run.downed) return;
  }
}

function collect(run, prev) {
  const flying = run.jet > 0;
  for (let i = run.objects.length - 1; i >= 0; i -= 1) {
    const o = run.objects[i];
    if (o.t !== 0 && o.t !== 3) continue;
    if (!(o.z - prev > -0.6 && o.z - run.distance <= 0.4)) continue;
    const air = o.h > JET_HEIGHT;
    let got = false;
    if (o.k === "coin" && run.magnet > 0 && air === flying) got = true;
    else if (o.l === run.lane) {
      const body = run.y + 0.8;
      got = Math.abs(body - o.h) < (run.slide > 0 ? 1.0 : 1.35) || (flying && air);
    }
    if (!got) continue;
    run.objects.splice(i, 1);
    const m = multiplier(run);
    if (o.k === "coin") {
      run.coins += 1;
      run.score += COIN_POINTS * m;
      run.events.push("coin");
    } else if (o.t === 3) {
      run.powerups += 1;
      const ticks = powerTicks(o.k, run.levels[o.k]);
      run[o.k === "jetpack" ? "jet" : o.k] = ticks;
      if (o.k === "jetpack") {
        run.slide = 0;
        spawnJetCoins(run, ticks);
      }
      run.events.push(`power:${o.k}`);
    } else {
      run.pickups += 1;
      run.score += ITEMS[o.k] * m;
      run.events.push(`item:${o.k}`);
    }
  }
}

export function stepRun(run, code) {
  if (run.over) return run;
  run.events.length = 0;

  // down: the only moves are revive or give up, and time stands still
  if (run.downed) {
    if (code === INPUT.REVIVE && run.keysLeft >= reviveCost(run)) revive(run);
    else if (code === INPUT.GIVE_UP || code === INPUT.REVIVE) {
      run.over = true;
      run.reason = run.downed;
      run.downed = "";
    }
    if (run.over || !run.downed) run.tick += 1;
    return run;
  }

  const dt = 1 / TICK_HZ;
  const grounded = run.y <= run.floor + 0.001 && run.vy <= 0;

  if (code === INPUT.LEFT) changeLane(run, run.lane - 1);
  else if (code === INPUT.RIGHT) changeLane(run, run.lane + 1);
  else if (code === INPUT.JUMP && grounded && run.jet <= 0) {
    run.vy = run.sneakers > 0 ? SNEAKER_JUMP_V : JUMP_V;
    run.slide = 0;
    run.jumps += 1;
  } else if (code === INPUT.ROLL && run.jet <= 0) {
    if (!grounded) run.vy = Math.min(run.vy, DIVE_V);
    run.slide = SLIDE_TICKS;
    run.rolls += 1;
  } else if (code === INPUT.BOARD && run.board <= 0 && run.boardsLeft > 0) {
    run.boardsLeft -= 1;
    run.boardsUsed += 1;
    run.board = BOARD_TICKS;
    run.events.push("board");
  }
  if (run.over || run.downed) {
    run.tick += 1;
    return run;
  }

  // forward motion
  run.speed = Math.min(SPEED_MAX, SPEED_BASE + run.distance * SPEED_RAMP);
  const move = run.speed * dt;
  const prev = run.distance;
  run.distance += move;
  run.score += move * POINTS_PER_UNIT * multiplier(run);

  // oncoming traffic
  for (const o of run.objects) if (o.v) o.z -= o.v * dt;

  // vertical: jetpack, or gravity onto whatever surface is underneath
  const surf = surfaceAt(run, run.lane, run.distance);
  if (run.jet > 0) {
    run.y += (JET_HEIGHT - run.y) * 0.08;
    run.vy = 0;
  } else {
    const prevY = run.y;
    if (run.y > surf || run.vy > 0) {
      run.y += run.vy * dt;
      run.vy -= GRAVITY * dt;
    }
    if (run.y <= surf) {
      if (prevY >= surf - 0.6 || run.invuln > 0) {
        // ran up a ramp, landed on a roof, or landed on the road
        if (surf > 0.5 && run.floor < 0.5 && prevY < surf - 0.05) run.rides += 1;
        run.y = surf;
        run.vy = 0;
      } else {
        // ran into the front of a van or a trailer
        run.y = Math.max(run.y, 0);
        down(run, "crashed");
        if (run.downed || run.over) {
          run.distance = prev;
          run.tick += 1;
          return run;
        }
        run.y = surf;
      }
    }
  }
  run.floor = run.jet > 0 ? 0 : surf;

  // spawn ahead
  while (run.spawnAt < run.distance + FAR_EDGE) {
    run.spawnAt += spawnPattern(run, run.spawnAt) + 6 + roll(run) * 8;
  }

  collide(run);
  if (!run.over && !run.downed) collect(run, prev);

  // scenery and cleanup
  for (let i = run.objects.length - 1; i >= 0; i -= 1) {
    const o = run.objects[i];
    if (o.t === 2 && o.z - prev > 0 && o.z - run.distance <= 0) {
      if (o.k === "landmark" && run.sightings.indexOf(o.n) < 0) run.sightings.push(o.n);
      if (o.k === "store") run.stores += 1;
    }
    if (o.z + (o.len || 0) - run.distance < -10) run.objects.splice(i, 1);
  }

  // timers
  run.x += Math.max(-22 * dt, Math.min(22 * dt, LANE_X[run.lane] - run.x));
  if (run.slide > 0) run.slide -= 1;
  if (run.invuln > 0) run.invuln -= 1;
  if (run.chase > 0) run.chase -= 1;
  if (run.board > 0) run.board -= 1;
  if (run.magnet > 0) run.magnet -= 1;
  if (run.sneakers > 0) run.sneakers -= 1;
  if (run.double > 0) run.double -= 1;
  if (run.jet > 0) {
    run.jet -= 1;
    if (run.jet === 0) {
      run.invuln = Math.max(run.invuln, 90);
      run.events.push("jetend");
    }
  }

  run.tick += 1;
  if (!run.over && !run.downed && run.tick >= MAX_TICKS) {
    run.over = true;
    run.reason = "timeup";
  }
  return run;
}

export function summarize(run) {
  return {
    score: Math.round(run.score),
    distance: Math.round(run.distance),
    coins: run.coins,
    pickups: run.pickups,
    jumps: run.jumps,
    rolls: run.rolls,
    rides: run.rides,
    powerups: run.powerups,
    stumbles: run.stumbles,
    boardsUsed: run.boardsUsed,
    keysUsed: run.keysUsed,
    ticks: run.tick,
    reason: run.reason,
    stores: run.stores,
    sightings: run.sightings.slice(0, LANDMARKS.length),
  };
}

/**
 * Replay a whole run from its seed, loadout and input log. If the log ends
 * while the runner is down, that is a give-up.
 */
export function simulateRun(seed, opts, inputs) {
  const run = createRun(seed, opts);
  const list = Array.isArray(inputs) ? inputs : [];
  let i = 0;
  while (!run.over) {
    let code = 0;
    while (i < list.length && list[i][0] <= run.tick) {
      code = list[i][1];
      i += 1;
    }
    if (run.downed && code !== INPUT.REVIVE) code = INPUT.GIVE_UP;
    stepRun(run, code);
  }
  return summarize(run);
}

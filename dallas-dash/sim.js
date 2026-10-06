/**
 * Dallas Dash — the shared deterministic simulation (client copy).
 *
 * This is the same code the server runs in app/src/logic.js, with exports
 * added so the browser can drive it. The client uses it for two things:
 * drawing the course, and recording the input log that is submitted for
 * verification. If this file and app/src/logic.js ever disagree, the server
 * replays a different course and refuses every run — app/tests/rules.test.ts
 * catches that, so keep the core below byte-identical.
 *
 * Porting note: this module touches no DOM, no canvas and no timers, so it
 * carries over to Expo / React Native unchanged.
 */

export const TICK_HZ = 60;
export const MAX_TICKS = 2640;
export const LANES = 3;
export const LANE_X = [-2.3, 0, 2.3];
export const JUMP_V = 8.6;
export const GRAVITY = 26;
export const SLIDE_TICKS = 34;
export const HIT_LIMIT = 3;
export const FAR_EDGE = 190;
export const SPEED_BASE = 14;
export const SPEED_MAX = 30;
export const POINTS_PER_UNIT = 3;
export const MAX_INPUTS = 220;

export const ITEMS = {
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

export const CLEARANCE = {
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

export function createRun(seed) {
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

export function stepRun(run, code) {
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

/** Run a course to its end from a seed and an input log — used by tests. */
export function simulateRun(seed, inputs) {
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

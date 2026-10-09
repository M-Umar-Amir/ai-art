// Fairness check: a lookahead bot plays many seeds. If a course can't be
// survived by a bot that sees the future, a player can't survive it either.
import * as SIM from "../sim.js";

const clone = (r) => structuredClone(r);
const bad = (r) => r.over || r.downed || r.stumbles > 0;

function safe(run, plan, horizon) {
  const r = clone(run);
  const s0 = r.stumbles;
  let t = 0;
  for (const [at, code] of plan) {
    while (t < at) { SIM.stepRun(r, 0); t++; if (r.over || r.downed || r.stumbles > s0) return -1000 + t; }
    SIM.stepRun(r, code); t++;
    if (r.over || r.downed || r.stumbles > s0) return -1000 + t;
  }
  while (t < horizon) { SIM.stepRun(r, 0); t++; if (r.over || r.downed || r.stumbles > s0) return -1000 + t; }
  return r.coins * 2 + (r.lane === 1 ? 1 : 0);
}

function choose(run) {
  let best = null, bestScore = -Infinity;
  const acts = [0, 1, 2, 3, 4];
  for (const a of acts) for (const gap of [10, 22]) for (const b of acts) {
    const plan = [[0, a], [gap, b]];
    const sc = safe(run, plan, 70) - (a ? 0.5 : 0) - (b ? 0.3 : 0);
    if (sc > bestScore) { bestScore = sc; best = a; }
  }
  return best;
}

const seeds = Number(process.argv[2] || 12);
const maxTicks = Number(process.argv[3] || 60 * 120);
const deaths = [];
let total = 0;
for (let s = 1; s <= seeds; s++) {
  const seed = (Math.imul(s, 2654435761) >>> 0) | 1;
  const run = SIM.createRun(seed, {});
  const inputs = [];
  while (!run.over && !run.downed && run.tick < maxTicks) {
    const code = run.tick % 4 === 0 ? choose(run) : 0;
    if (code) inputs.push([run.tick, code]);
    SIM.stepRun(run, code);
  }
  const alive = !(run.over || run.downed);
  const where = Math.round(run.distance);
  const tAt = run.tick;
  // finish the run hands-off so the full log can be replayed and compared
  while (!run.over) {
    const code = run.downed ? SIM.INPUT.GIVE_UP : 0;
    if (code) inputs.push([run.tick, code]);
    SIM.stepRun(run, code);
  }
  const replay = SIM.simulateRun(seed, {}, inputs);
  const ok = replay.score === Math.round(run.score);
  total += where;
  console.log(`seed ${seed}: ${alive ? "alive" : "died"} at ${where}m t=${(tAt / 60).toFixed(0)}s replay=${ok ? "match" : "MISMATCH"}`);
  if (!alive) deaths.push(seed);
  if (!ok) process.exitCode = 1;
}
console.log(`avg distance ${Math.round(total / seeds)}, deaths ${deaths.length}/${seeds}`);

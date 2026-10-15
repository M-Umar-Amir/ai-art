import test from "node:test";
import assert from "node:assert/strict";
import * as SIM from "../sim.js";
import * as R from "../rules.js";
import { normalizeCode } from "../coupon-code.js";

const P = "tester01";

/** Play a run the way the client does: step, record inputs. */
function play(view, policy, maxTicks = 60 * 40) {
  const run = SIM.createRun(view.nextSeed, view.loadout);
  const inputs = [];
  while (!run.over && run.tick < maxTicks) {
    // time stands still while down, so a policy must decide: default give up
    const code = policy(run) || (run.downed ? SIM.INPUT.GIVE_UP : 0);
    if (code) inputs.push([run.tick, code]);
    SIM.stepRun(run, code);
  }
  // stop like a player who gives up: end the run deterministically
  if (!run.over) {
    // keep running with no input until the course ends it
    while (!run.over) {
      const code = run.downed ? SIM.INPUT.GIVE_UP : 0;
      if (code) inputs.push([run.tick, code]);
      SIM.stepRun(run, code);
    }
  }
  return { run, inputs };
}

function act(state, action) {
  const v = R.validateAction(state, P, action);
  if (!v.ok) return { state, error: v.error };
  return { state: R.applyAction(state, P, action) };
}

test("a verified run is credited; a tampered or replayed one is not", () => {
  let state = R.setup([P]);
  const view = R.viewFor(state, P);
  const { run, inputs } = play(view, (r) => (r.tick % 50 === 10 ? SIM.INPUT.JUMP : 0));
  const score = Math.round(run.score);
  const base = { kind: "submitRun", runId: "run-000001", seed: view.nextSeed, inputs, claimedScore: score };

  assert.match(act(state, { ...base, claimedScore: score + 1 }).error, /could not be verified/);
  const r1 = act(state, base);
  assert.equal(r1.error, undefined);
  state = r1.state;
  const after = R.viewFor(state, P);
  assert.equal(after.points, score);
  assert.equal(after.coins, run.coins);
  assert.equal(after.runs, 1);
  assert.notEqual(after.nextSeed, view.nextSeed);
  assert.match(act(state, base).error, /already been credited|out of date/);
  assert.match(act(state, { ...base, runId: "run-000002" }).error, /out of date/);
});

test("revives spend keys from the ledger, and only what the account owns", () => {
  let state = R.setup([P]);
  const view = R.viewFor(state, P);
  assert.equal(view.keys, 2);
  // never steer: crash, revive whenever possible
  const { run, inputs } = play(view, (r) => (r.downed ? SIM.INPUT.REVIVE : 0), 60 * 120);
  assert.ok(run.keysUsed >= 1, "expected at least one revive");
  assert.ok(run.keysUsed <= 2);
  const r = act(state, { kind: "submitRun", runId: "run-rev001", seed: view.nextSeed, inputs, claimedScore: Math.round(run.score) });
  assert.equal(r.error, undefined);
  assert.equal(R.viewFor(r.state, P).keys, 2 - run.keysUsed);
});

test("shop: boards and upgrades cost drumsticks", () => {
  let state = R.setup([P]);
  assert.match(act(state, { kind: "buy", item: "board" }).error, /not enough/);
  state.accounts[P].coins = 1000;
  state = act(state, { kind: "buy", item: "board" }).state;
  state = act(state, { kind: "buy", item: "magnet" }).state;
  const v = R.viewFor(state, P);
  assert.equal(v.boards, 3);
  assert.equal(v.levels.magnet, 1);
  assert.equal(v.coins, 1000 - 300 - 250);
  assert.match(act(state, { kind: "buy", item: "rocket" }).error, /unknown/);
});

test("coupon: threshold, points deducted, unique valid codes, active limit", () => {
  let state = R.setup([P]);
  assert.match(act(state, { kind: "claimCoupon" }).error, /needs/);
  const codes = new Set();
  for (let i = 0; i < 3; i += 1) {
    state = act(state, { kind: "grantTestPoints" }).state;
    state = act(state, { kind: "claimCoupon" }).state;
    const v = R.viewFor(state, P);
    assert.equal(v.points, 0);
    const c = v.coupons[v.coupons.length - 1];
    assert.ok(normalizeCode(c.code), c.code);
    codes.add(c.code);
  }
  assert.equal(codes.size, 3);
  state = act(state, { kind: "grantTestPoints" }).state;
  assert.match(act(state, { kind: "claimCoupon" }).error, /unused coupons/);
});

test("old v1 saves migrate", () => {
  const old = { v: 1, accounts: { [P]: { points: 4200, bestRun: 900, runs: 3, coupons: [], outfits: ["classic"], seenRuns: [], sightings: [] } } };
  const s = R.migrate(old);
  const v = R.viewFor(s, P);
  assert.equal(v.points, 4200);
  assert.equal(v.keys, 2);
  assert.equal(v.mult, 1);
});

test("routes: pick a map; landmarks are credited on the ledger's route", () => {
  let state = R.setup([P]);
  assert.equal(R.viewFor(state, P).route, "downtown");
  assert.match(act(state, { kind: "setRoute", route: "paris" }).error, /unknown route/);
  state = act(state, { kind: "setRoute", route: "toronto" }).state;
  const view = R.viewFor(state, P);
  assert.equal(view.route, "toronto");
  assert.equal(view.landmarkTotal, 40);
  // run long enough to pass scenery, never steering
  const { run, inputs } = play(view, (r) => (r.downed ? SIM.INPUT.GIVE_UP : r.tick % 40 === 5 ? SIM.INPUT.JUMP : 0), 60 * 90);
  const r = act(state, { kind: "submitRun", runId: "run-route1", seed: view.nextSeed, inputs, claimedScore: Math.round(run.score) });
  assert.equal(r.error, undefined);
  const after = R.viewFor(r.state, P);
  assert.equal(after.landmarks.length, run.sightings.length);
  const toronto = ["CN Tower", "Rogers Centre", "Toronto City Hall", "Gooderham Building", "Union Station", "Royal Ontario Museum", "Casa Loma", "Distillery District", "Kensington Market", "St. Lawrence Market"];
  for (const name of after.landmarks) assert.ok(toronto.includes(name), name);
});

test("crew: three characters; the robot unlocks with lifetime points", () => {
  let state = R.setup([P]);
  const v = R.viewFor(state, P);
  assert.deepEqual(v.outfitList.map((o) => o.id), ["dante", "maya", "r11"]);
  assert.deepEqual(v.outfits, ["dante", "maya"]);
  assert.match(act(state, { kind: "setOutfit", outfit: "r11" }).error, /locked/);
  state = act(state, { kind: "setOutfit", outfit: "maya" }).state;
  assert.equal(R.viewFor(state, P).outfit, "maya");
});

test("v2 saves migrate: old outfits fall back, Dallas sightings are re-keyed", () => {
  const old = { v: 2, accounts: { [P]: { points: 10, lifetime: 30000, outfit: "gold", outfits: ["classic", "gold"], sightings: [0, 1, 7], seenRuns: [], coupons: [] } } };
  const v = R.viewFor(R.migrate(old), P);
  assert.equal(v.outfit, "dante");
  assert.deepEqual(v.outfits, ["dante", "maya", "r11"]);
  assert.equal(v.route, "downtown");
  assert.deepEqual(v.landmarks.sort(), ["Klyde Warren Park", "Margaret Hunt Hill Bridge", "Reunion Tower"]);
});

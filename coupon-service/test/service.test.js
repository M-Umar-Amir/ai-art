import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { PGlite } from "@electric-sql/pglite";
import { createCouponService, CouponError } from "../src/service.js";

const SECRET = "test-secret-0123456789abcdef0123456789abcdef";
const schema = readFileSync(new URL("../schema.sql", import.meta.url), "utf8");

async function setup(points = { alice: 250000, bob: 100000 }) {
  const db = new PGlite();
  await db.exec(schema);
  for (const [u, p] of Object.entries(points)) {
    await db.query("INSERT INTO game.accounts (user_id, points, course_seed) VALUES ($1, $2, 1)", [u, p]);
  }
  let clock = new Date("2026-10-09T12:00:00Z");
  const svc = createCouponService({ db, secret: SECRET, now: () => clock });
  return { db, svc, tick: (ms) => (clock = new Date(clock.getTime() + ms)) };
}

const rejects = (p, code) => assert.rejects(p, (e) => e instanceof CouponError && e.code === code);

test("issue deducts points and mints unique, well-formed codes", async () => {
  const { db, svc } = await setup();
  const a = await svc.issue({ userId: "alice", issueKey: "k1" });
  const b = await svc.issue({ userId: "alice", issueKey: "k2" });
  assert.match(a.code, /^KFC-[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]{4}-[A-Z2-9]$/);
  assert.notEqual(a.code, b.code);
  const { rows } = await db.query("SELECT points FROM game.accounts WHERE user_id = 'alice'");
  assert.equal(Number(rows[0].points), 50000);
  const ledger = await db.query("SELECT sum(delta)::int AS s FROM game.point_ledger WHERE user_id = 'alice'");
  assert.equal(ledger.rows[0].s, -200000);
  // the code is never stored in the clear
  const raw = await db.query("SELECT code_hash, code_enc FROM rewards.coupons");
  for (const r of raw.rows) assert.ok(!Buffer.from(r.code_enc).toString("utf8").includes(a.code.slice(4, 8)));
});

test("a double-tapped claim issues once; not enough points issues nothing", async () => {
  const { db, svc } = await setup();
  const [x, y] = await Promise.all([svc.issue({ userId: "bob", issueKey: "tap" }), svc.issue({ userId: "bob", issueKey: "tap" })]);
  assert.equal(x.code, y.code);
  await rejects(svc.issue({ userId: "bob", issueKey: "tap2" }), "insufficient_points");
  const n = await db.query("SELECT count(*)::int AS n FROM rewards.coupons");
  assert.equal(n.rows[0].n, 1);
});

test("validate: typo, someone else's code, min order, discount maths", async () => {
  const { svc } = await setup();
  const c = await svc.issue({ userId: "alice", issueKey: "v1" });
  const typo = c.code.slice(0, -1) + (c.code.endsWith("A") ? "B" : "A");
  await rejects(svc.validate({ userId: "alice", code: typo, subtotalCents: 3000 }), "malformed");
  await rejects(svc.validate({ userId: "bob", code: c.code, subtotalCents: 3000 }), "not_found");
  await rejects(svc.validate({ userId: "alice", code: c.code, subtotalCents: 2000 }), "min_order");
  const ok = await svc.validate({ userId: "alice", code: c.code.toLowerCase().replace(/-/g, " "), subtotalCents: 3000 });
  assert.equal(ok.discountCents, 450); // 15% of $30
  const capped = await svc.validate({ userId: "alice", code: c.code, subtotalCents: 9000 });
  assert.equal(capped.discountCents, 500); // capped at $5
});

test("two checkouts racing for one code: exactly one wins", async () => {
  const { svc } = await setup();
  const c = await svc.issue({ userId: "alice", issueKey: "r1" });
  const results = await Promise.allSettled([
    svc.reserve({ userId: "alice", code: c.code, orderId: "order-A", subtotalCents: 3000 }),
    svc.reserve({ userId: "alice", code: c.code, orderId: "order-B", subtotalCents: 3000 }),
  ]);
  assert.equal(results.filter((r) => r.status === "fulfilled").length, 1);
  assert.equal(results.find((r) => r.status === "rejected").reason.code, "in_use");
});

test("reserve → redeem is once only; release and restore give it back", async () => {
  const { svc } = await setup();
  const c = await svc.issue({ userId: "alice", issueKey: "l1" });
  await svc.reserve({ userId: "alice", code: c.code, orderId: "o1", subtotalCents: 3000 });
  // retrying the same checkout is fine
  await svc.reserve({ userId: "alice", code: c.code, orderId: "o1", subtotalCents: 3000 });
  // payment failed → released → usable again
  assert.equal((await svc.release({ orderId: "o1" })).released, true);
  await svc.reserve({ userId: "alice", code: c.code, orderId: "o2", subtotalCents: 3000 });
  const r = await svc.redeem({ orderId: "o2" });
  assert.equal(r.discountCents, 450);
  assert.equal((await svc.redeem({ orderId: "o2" })).already, true); // webhook twice
  await rejects(svc.reserve({ userId: "alice", code: c.code, orderId: "o3", subtotalCents: 3000 }), "used");
  // restaurant cancels the paid order → coupon is back
  assert.equal((await svc.restore({ orderId: "o2" })).restored, true);
  const list = await svc.list("alice");
  assert.equal(list.find((x) => x.id === c.id).status, "active");
});

test("expiry, abandoned reservations and guess rate-limiting", async () => {
  const { svc, tick } = await setup();
  const c = await svc.issue({ userId: "alice", issueKey: "e1" });
  await svc.reserve({ userId: "alice", code: c.code, orderId: "slow", subtotalCents: 3000 });
  tick(31 * 60000);
  assert.equal((await svc.sweep()).released, 1);
  tick(31 * 86400000);
  await rejects(svc.validate({ userId: "alice", code: c.code, subtotalCents: 3000 }), "expired");
  for (let i = 0; i < 10; i += 1) {
    await rejects(svc.validate({ userId: "bob", code: "KFC-AAAA-AAAA-AAAA-A", subtotalCents: 3000 }), "not_found");
  }
  await rejects(svc.validate({ userId: "bob", code: "KFC-AAAA-AAAA-AAAA-A", subtotalCents: 3000 }), "rate_limited");
});

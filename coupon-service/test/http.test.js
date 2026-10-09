import test from "node:test";
import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import { PGlite } from "@electric-sql/pglite";
import { createCouponService } from "../src/service.js";
import { createHandler } from "../src/http.js";

test("http: auth, issue, validate, service-only redeem", async () => {
  const db = new PGlite();
  await db.exec(readFileSync(new URL("../schema.sql", import.meta.url), "utf8"));
  await db.query("INSERT INTO game.accounts (user_id, points, course_seed) VALUES ('u1', 100000, 1)");
  const service = createCouponService({ db, secret: "x".repeat(40) });
  const handle = createHandler({
    service,
    auth: async (req) => req.headers.get("x-test-user"),
    isService: async (req) => req.headers.get("x-service-key") === "svc",
  });
  const call = (method, path, body, headers = {}) =>
    handle(new Request(`https://api.test${path}`, { method, headers: { "content-type": "application/json", ...headers }, body: body ? JSON.stringify(body) : undefined }));

  assert.equal((await call("POST", "/coupons/issue", { issueKey: "a" })).status, 401);
  const issued = await (await call("POST", "/coupons/issue", { issueKey: "a" }, { "x-test-user": "u1" })).json();
  assert.match(issued.code, /^KFC-/);
  const v = await call("POST", "/coupons/validate", { code: issued.code, subtotalCents: 3000 }, { "x-test-user": "u2" });
  assert.equal(v.status, 404);
  const ok = await (await call("POST", "/coupons/reserve", { code: issued.code, orderId: "o1", subtotalCents: 3000 }, { "x-test-user": "u1" })).json();
  assert.equal(ok.discountCents, 450);
  assert.equal((await call("POST", "/coupons/redeem", { orderId: "o1" }, { "x-test-user": "u1" })).status, 403);
  assert.equal((await call("POST", "/coupons/redeem", { orderId: "o1" }, { "x-service-key": "svc" })).status, 200);
});

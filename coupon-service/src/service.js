/**
 * The coupon service: the only code that writes rewards.coupons.
 *
 * Lifecycle (one row per coupon):
 *
 *   issued ─► active ──reserve(order)──► reserved ──redeem(order)──► redeemed
 *               ▲  ▲                        │                          │
 *               │  └──── release(order) ────┘                          │
 *               └──────────── restore(order) (restaurant cancelled) ───┘
 *   active ──(expires_at passes)──► expired
 *
 * Every transition is a single conditional UPDATE (… WHERE status = 'x'), so
 * two checkouts racing for the same code cannot both win: Postgres serialises
 * the row, and the loser's UPDATE matches zero rows.
 */

import { createHmac, createCipheriv, createDecipheriv, randomBytes } from "node:crypto";
import { generateCode, normalizeCode } from "../../dallas-dash/coupon-code.js";

export const DEFAULT_TERMS = { percent: 15, minOrderCents: 2500, maxOffCents: 500, validDays: 30, pointsCost: 100000 };
const ACTIVE_LIMIT = 3;
const RESERVE_TTL_MIN = 30;
const ATTEMPT_WINDOW_MIN = 15;
const ATTEMPT_LIMIT = 10;
const RESTORE_GRACE_DAYS = 7;

export class CouponError extends Error {
  constructor(code, message) {
    super(message);
    this.code = code;
  }
}

/**
 * @param db      { query, transaction } — see db.js
 * @param secret  32+ random bytes (hex) from the environment, e.g. COUPON_SECRET.
 *                Derives the lookup pepper and the encryption key.
 * @param now     clock, injectable for tests
 */
export function createCouponService({ db, secret, now = () => new Date() }) {
  if (!secret || secret.length < 32) throw new Error("COUPON_SECRET must be at least 32 characters");
  const pepper = createHmac("sha256", secret).update("lookup").digest();
  const encKey = createHmac("sha256", secret).update("encrypt").digest();

  const hashCode = (code) => createHmac("sha256", pepper).update(code).digest();
  function encrypt(code) {
    const iv = randomBytes(12);
    const c = createCipheriv("aes-256-gcm", encKey, iv);
    const body = Buffer.concat([c.update(code, "utf8"), c.final()]);
    return Buffer.concat([iv, c.getAuthTag(), body]);
  }
  function decrypt(buf) {
    const b = Buffer.from(buf);
    const d = createDecipheriv("aes-256-gcm", encKey, b.subarray(0, 12));
    d.setAuthTag(b.subarray(12, 28));
    return Buffer.concat([d.update(b.subarray(28)), d.final()]).toString("utf8");
  }

  async function event(q, couponId, name, orderId, detail) {
    await q.query(
      "INSERT INTO rewards.coupon_events (coupon_id, event, order_id, detail) VALUES ($1, $2, $3, $4)",
      [couponId, name, orderId || null, detail ? JSON.stringify(detail) : null],
    );
  }

  function discountFor(row, subtotalCents) {
    return Math.min(row.max_off_cents, Math.floor((subtotalCents * row.percent) / 100));
  }

  /** Rate limit: too many failed code entries in the window locks entry for a while. */
  async function checkAttempts(userId) {
    const since = new Date(now().getTime() - ATTEMPT_WINDOW_MIN * 60000);
    const { rows } = await db.query(
      "SELECT count(*)::int AS n FROM rewards.code_attempts WHERE user_id = $1 AND ok = false AND created_at > $2",
      [userId, since],
    );
    if (rows[0].n >= ATTEMPT_LIMIT) throw new CouponError("rate_limited", "Too many wrong codes. Try again in 15 minutes.");
  }
  async function recordAttempt(userId, ok) {
    await db.query("INSERT INTO rewards.code_attempts (user_id, ok, created_at) VALUES ($1, $2, $3)", [userId, ok, now()]);
  }

  /** Find a coupon by what the customer typed, owned by this user. */
  async function lookup(q, userId, typed, { lock } = {}) {
    const code = normalizeCode(typed);
    if (!code) throw new CouponError("malformed", "That doesn't look like a Dallas Dash code. Check for typos.");
    const { rows } = await q.query(
      `SELECT * FROM rewards.coupons WHERE code_hash = $1${lock ? " FOR UPDATE" : ""}`,
      [hashCode(code)],
    );
    const row = rows[0];
    // a code that exists but belongs to someone else looks exactly like one
    // that doesn't exist: no oracle for harvesting other people's codes
    if (!row || row.user_id !== userId) throw new CouponError("not_found", "We couldn't find that code on your account.");
    return row;
  }

  function usable(row, subtotalCents, orderId) {
    if (row.status === "reserved" && orderId && row.order_id === orderId) return; // same checkout, retried
    if (row.status === "redeemed") throw new CouponError("used", "This code has already been used.");
    if (row.status === "reserved") throw new CouponError("in_use", "This code is being used on another order.");
    if (row.status !== "active") throw new CouponError("inactive", "This code is no longer valid.");
    if (new Date(row.expires_at) <= now()) throw new CouponError("expired", "This code has expired.");
    if (subtotalCents < row.min_order_cents) {
      throw new CouponError("min_order", `Add $${((row.min_order_cents - subtotalCents) / 100).toFixed(2)} more to use this code.`);
    }
  }

  return {
    /**
     * Game → coupon. Deducts the points and creates the coupon in ONE
     * transaction, so there is never a deduction without a coupon or a coupon
     * without a deduction. `issueKey` makes a double-tapped claim safe.
     */
    async issue({ userId, issueKey, terms = DEFAULT_TERMS }) {
      return db.transaction(async (tx) => {
        if (issueKey) {
          const prior = await tx.query("SELECT * FROM rewards.coupons WHERE issue_key = $1", [issueKey]);
          if (prior.rows[0]) {
            if (prior.rows[0].user_id !== userId) throw new CouponError("conflict", "Issue key already used.");
            return present(prior.rows[0]);
          }
        }
        const acct = await tx.query("SELECT points FROM game.accounts WHERE user_id = $1 FOR UPDATE", [userId]);
        if (!acct.rows[0]) throw new CouponError("no_account", "No game account.");
        if (Number(acct.rows[0].points) < terms.pointsCost) {
          throw new CouponError("insufficient_points", `Needs ${terms.pointsCost} verified points.`);
        }
        const active = await tx.query(
          "SELECT count(*)::int AS n FROM rewards.coupons WHERE user_id = $1 AND status IN ('active','reserved')",
          [userId],
        );
        if (active.rows[0].n >= ACTIVE_LIMIT) throw new CouponError("limit", `You already hold ${ACTIVE_LIMIT} unused coupons.`);

        const expires = new Date(now().getTime() + terms.validDays * 86400000);
        let row = null;
        // 60 random bits: a collision is astronomically unlikely, but the
        // UNIQUE index is the real guarantee — on a clash, mint again
        for (let attempt = 0; attempt < 5 && !row; attempt += 1) {
          const code = generateCode();
          const ins = await tx.query(
            `INSERT INTO rewards.coupons
               (user_id, code_hash, code_enc, code_last4, percent, min_order_cents, max_off_cents,
                issue_key, points_cost, expires_at, created_at)
             VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,$10,$11)
             ON CONFLICT (code_hash) DO NOTHING
             RETURNING *`,
            [userId, hashCode(code), encrypt(code), code.replace(/-/g, "").slice(-4), terms.percent,
              terms.minOrderCents, terms.maxOffCents, issueKey || null, terms.pointsCost, expires, now()],
          );
          row = ins.rows[0] || null;
        }
        if (!row) throw new CouponError("retry", "Could not mint a code, try again.");

        await tx.query("UPDATE game.accounts SET points = points - $2 WHERE user_id = $1", [userId, terms.pointsCost]);
        await tx.query(
          "INSERT INTO game.point_ledger (user_id, delta, reason, ref) VALUES ($1, $2, 'coupon', $3)",
          [userId, -terms.pointsCost, row.id],
        );
        await event(tx, row.id, "issued", null, { pointsCost: terms.pointsCost });
        return present(row);
      });
    },

    /** The user's coupons, codes decrypted for display. */
    async list(userId) {
      await expireDue();
      const { rows } = await db.query(
        "SELECT * FROM rewards.coupons WHERE user_id = $1 ORDER BY created_at DESC",
        [userId],
      );
      return rows.map(present);
    },

    /** Cart preview: is this code good for this cart, and how much does it take off? Writes nothing. */
    async validate({ userId, code, subtotalCents }) {
      await checkAttempts(userId);
      try {
        const row = await lookup(db, userId, code);
        usable(row, subtotalCents);
        await recordAttempt(userId, true);
        return { ok: true, couponId: row.id, discountCents: discountFor(row, subtotalCents), expiresAt: row.expires_at };
      } catch (err) {
        if (err instanceof CouponError && (err.code === "malformed" || err.code === "not_found")) await recordAttempt(userId, false);
        throw err;
      }
    },

    /**
     * Checkout: lock the code to this order before taking payment. Idempotent
     * for the same order (a retried request gets the same answer).
     */
    async reserve({ userId, code, orderId, subtotalCents }) {
      await checkAttempts(userId);
      return db.transaction(async (tx) => {
        let row;
        try {
          row = await lookup(tx, userId, code, { lock: true });
        } catch (err) {
          if (err.code === "malformed" || err.code === "not_found") await recordAttempt(userId, false);
          throw err;
        }
        usable(row, subtotalCents, orderId);
        const discount = discountFor(row, subtotalCents);
        if (row.status === "reserved") return { ok: true, couponId: row.id, discountCents: discount };
        const upd = await tx.query(
          `UPDATE rewards.coupons SET status = 'reserved', order_id = $2, reserved_at = $3, discount_cents = $4
           WHERE id = $1 AND status = 'active' RETURNING id`,
          [row.id, orderId, now(), discount],
        );
        if (!upd.rows[0]) throw new CouponError("in_use", "This code is being used on another order.");
        await event(tx, row.id, "reserved", orderId, { subtotalCents, discount });
        return { ok: true, couponId: row.id, discountCents: discount };
      });
    },

    /** Payment captured → the coupon is spent. Called by the order service, keyed by order. */
    async redeem({ orderId }) {
      return db.transaction(async (tx) => {
        const upd = await tx.query(
          `UPDATE rewards.coupons SET status = 'redeemed', redeemed_at = $2
           WHERE order_id = $1 AND status = 'reserved' RETURNING id, discount_cents`,
          [orderId, now()],
        );
        const row = upd.rows[0];
        if (!row) {
          const done = await tx.query("SELECT id FROM rewards.coupons WHERE order_id = $1 AND status = 'redeemed'", [orderId]);
          if (done.rows[0]) return { ok: true, already: true }; // webhook delivered twice
          throw new CouponError("not_reserved", "No coupon is reserved for this order.");
        }
        await event(tx, row.id, "redeemed", orderId, { discount: row.discount_cents });
        return { ok: true, couponId: row.id, discountCents: row.discount_cents };
      });
    },

    /** Payment failed / cart abandoned → give the code back untouched. */
    async release({ orderId, reason = "released" }) {
      return db.transaction(async (tx) => {
        const upd = await tx.query(
          `UPDATE rewards.coupons SET status = 'active', order_id = NULL, reserved_at = NULL, discount_cents = NULL
           WHERE order_id = $1 AND status = 'reserved' RETURNING id`,
          [orderId],
        );
        if (upd.rows[0]) await event(tx, upd.rows[0].id, "released", orderId, { reason });
        return { ok: true, released: Boolean(upd.rows[0]) };
      });
    },

    /**
     * Restaurant cancelled a paid order → the coupon comes back with its
     * original expiry (or a short grace period if that has already passed).
     */
    async restore({ orderId, reason = "restaurant_cancelled" }) {
      return db.transaction(async (tx) => {
        const cur = await tx.query("SELECT * FROM rewards.coupons WHERE order_id = $1 AND status = 'redeemed' FOR UPDATE", [orderId]);
        const row = cur.rows[0];
        if (!row) return { ok: true, restored: false };
        const grace = new Date(now().getTime() + RESTORE_GRACE_DAYS * 86400000);
        const expires = new Date(row.expires_at) > now() ? row.expires_at : grace;
        await tx.query(
          `UPDATE rewards.coupons SET status = 'active', order_id = NULL, reserved_at = NULL,
             redeemed_at = NULL, discount_cents = NULL, expires_at = $2 WHERE id = $1`,
          [row.id, expires],
        );
        await event(tx, row.id, "restored", orderId, { reason });
        return { ok: true, restored: true };
      });
    },

    /** Cron (every few minutes): free abandoned reservations, expire old codes. */
    async sweep() {
      const cutoff = new Date(now().getTime() - RESERVE_TTL_MIN * 60000);
      const stale = await db.query(
        `UPDATE rewards.coupons SET status = 'active', order_id = NULL, reserved_at = NULL, discount_cents = NULL
         WHERE status = 'reserved' AND reserved_at < $1 RETURNING id`,
        [cutoff],
      );
      for (const r of stale.rows) await event(db, r.id, "released", null, { reason: "reservation_timeout" });
      const expired = await expireDue();
      return { released: stale.rows.length, expired };
    },
  };

  async function expireDue() {
    const { rows } = await db.query(
      "UPDATE rewards.coupons SET status = 'expired' WHERE status = 'active' AND expires_at <= $1 RETURNING id",
      [now()],
    );
    for (const r of rows) await event(db, r.id, "expired", null, null);
    return rows.length;
  }

  function present(row) {
    return {
      id: row.id,
      code: decrypt(row.code_enc),
      status: row.status,
      percent: row.percent,
      minOrderCents: row.min_order_cents,
      maxOffCents: row.max_off_cents,
      expiresAt: row.expires_at,
      orderId: row.order_id,
    };
  }
}

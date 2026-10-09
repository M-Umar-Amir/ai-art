-- KFC Dallas Dash — one database for the game and the ordering app.
--
-- Two schemas, one Postgres (e.g. Neon via the Vercel Marketplace):
--   game.*      written only by the game API (runs, points ledger)
--   rewards.*   written only by the coupon service (coupons, their history)
-- The ordering app never touches these tables directly: it calls the coupon
-- service (validate / reserve / redeem / release / restore). One owner per
-- table is what keeps "a coupon can only be used once" true.
--
-- Users are shared: both apps sign in against the same identity provider, so
-- user_id means the same person in the game and in the KFC app.

CREATE SCHEMA IF NOT EXISTS game;
CREATE SCHEMA IF NOT EXISTS rewards;

-- ── game ────────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS game.accounts (
  user_id      text PRIMARY KEY,
  points       bigint NOT NULL DEFAULT 0 CHECK (points >= 0),
  lifetime     bigint NOT NULL DEFAULT 0,
  course_seed  bigint NOT NULL,              -- next course the server will accept
  created_at   timestamptz NOT NULL DEFAULT now()
);

-- every submitted run, kept for audit; run_id makes resubmission a no-op
CREATE TABLE IF NOT EXISTS game.runs (
  run_id       text PRIMARY KEY,
  user_id      text NOT NULL REFERENCES game.accounts(user_id),
  seed         bigint NOT NULL,
  inputs       jsonb NOT NULL,
  score        integer NOT NULL,             -- what the server's replay produced
  verified     boolean NOT NULL,
  created_at   timestamptz NOT NULL DEFAULT now()
);

-- points only ever move through this ledger; accounts.points is its running sum
CREATE TABLE IF NOT EXISTS game.point_ledger (
  id           bigserial PRIMARY KEY,
  user_id      text NOT NULL REFERENCES game.accounts(user_id),
  delta        bigint NOT NULL,
  reason       text NOT NULL CHECK (reason IN ('run', 'coupon', 'coupon_refund', 'adjustment')),
  ref          text,                         -- run_id or coupon id
  created_at   timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS point_ledger_user ON game.point_ledger (user_id, created_at);

-- ── rewards ─────────────────────────────────────────────────────────────

CREATE TABLE IF NOT EXISTS rewards.coupons (
  id             uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id        text NOT NULL,
  -- the code itself is never stored in the clear: a keyed hash for lookup,
  -- an encrypted copy so the owner can see it again, the last 4 for support
  code_hash      bytea NOT NULL UNIQUE,
  code_enc       bytea NOT NULL,
  code_last4     text NOT NULL,
  source         text NOT NULL DEFAULT 'dallas-dash',
  percent        integer NOT NULL CHECK (percent BETWEEN 1 AND 100),
  min_order_cents integer NOT NULL DEFAULT 0,
  max_off_cents  integer NOT NULL,
  status         text NOT NULL DEFAULT 'active'
                 CHECK (status IN ('active', 'reserved', 'redeemed', 'expired', 'void')),
  order_id       text,                       -- set while reserved / once redeemed
  reserved_at    timestamptz,
  redeemed_at    timestamptz,
  discount_cents integer,                    -- what was actually taken off
  issue_key      text UNIQUE,                -- idempotency key from the claim
  points_cost    integer NOT NULL,
  expires_at     timestamptz NOT NULL,
  created_at     timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS coupons_user ON rewards.coupons (user_id, status);
-- one order can hold at most one game coupon
CREATE UNIQUE INDEX IF NOT EXISTS coupons_one_per_order ON rewards.coupons (order_id)
  WHERE order_id IS NOT NULL AND status IN ('reserved', 'redeemed');

-- every state change, for support and fraud review
CREATE TABLE IF NOT EXISTS rewards.coupon_events (
  id          bigserial PRIMARY KEY,
  coupon_id   uuid NOT NULL REFERENCES rewards.coupons(id),
  event       text NOT NULL,                 -- issued / reserved / redeemed / released / restored / expired
  order_id    text,
  detail      jsonb,
  created_at  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS coupon_events_coupon ON rewards.coupon_events (coupon_id, created_at);

-- failed code entries, for rate limiting guessers
CREATE TABLE IF NOT EXISTS rewards.code_attempts (
  id          bigserial PRIMARY KEY,
  user_id     text NOT NULL,
  ok          boolean NOT NULL,
  created_at  timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS code_attempts_user ON rewards.code_attempts (user_id, created_at);

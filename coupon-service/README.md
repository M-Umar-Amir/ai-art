# Coupon service — Dallas Dash × KFC ordering app

One Postgres database, two schemas: `game.*` (runs, points ledger) and `rewards.*` (coupons).
This service is the only writer of `rewards.coupons`; the ordering app calls its endpoints.

```
npm install
npm test          # 7 tests on PGlite, a real Postgres engine running in-process
```

- `schema.sql`: the tables, constraints and indexes.
- `src/service.js`: issue, list, validate, reserve, redeem, release, restore and sweep.
- `src/http.js`: web-standard `Request → Response` handlers. They mount on Vercel Functions, Hono or Express.
- `src/db.js`: an adapter for `pg` / Neon. PGlite works directly.

In production, set `COUPON_SECRET` (32+ random characters). From it the service derives the HMAC
pepper used for code lookup and the AES-256-GCM key that encrypts stored codes. If the secret is
lost, every stored code becomes unreadable, so keep it in a secrets manager.

See the PDF for the full flow: `../dallas-dash-docs/KFC-Dallas-Dash-Plan.pdf`.

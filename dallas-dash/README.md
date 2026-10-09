# KFC Dallas Dash — static build

Live at `epical.art/dallas-dash` (noindex). Plain static files, no build step.

## Files

| File | What it is |
| --- | --- |
| `sim.js` | The deterministic course and physics. Shared by the client and the rules, so they can never disagree. No DOM, no timers, no `Math.random` — ports to React Native unchanged. |
| `rules.js` | Account ledger: replay verification, wallet (drumsticks, keys, hoverboards), shop, missions → multiplier, coupon claim. |
| `coupon-code.js` | `KFC-XXXX-XXXX-XXXX-C` codes: 60 random bits + a check symbol. Shared with `../coupon-service`. |
| `scene.js` | Canvas 2D software 3D renderer (KFC palette, logo on signs, near-plane clipping). |
| `client.js` | Screens, swipe/keyboard/gamepad input, run loop, revive prompt. |
| `local-room.js` | Runs the rules in the browser for this static build (ledger in localStorage). |
| `audio.js` | Synthesised sound; music tempo follows run speed. |
| `assets/kfc-logo.svg` | KFC Colonel roundel (public reference copy from Wikimedia Commons). Replace with the client's official brand-kit file. |
| `tests/` | `node --test tests/rules.test.mjs`, `node tests/bot.mjs 6 10800` (fairness bot), `tests/render.html?scene=roof` (visual checks). Not deployed. |

## Gameplay (Subway Surfers model)

Endless run, speed 20 → 46. Three lanes; swipe to dodge, jump, roll (roll mid-air to dive). Ride
KFC trailers up their ramps and along the roofs; vans block a lane and oncoming vans drive at you.
Small obstacles make you stumble and the health inspector closes in — stumble again
within 5 s and he catches you. Big obstacles crash you. A hoverboard (double-tap, 30 s) absorbs one
crash. Keys revive you (1, 2, 4… keys). Power-ups: drumstick magnet, jetpack, super sneakers, 2×
score, each upgradable five times in the shop. Mission sets of three raise the score multiplier
(up to x6).

## What this build proves, and what it doesn't

Every run is replayed by `rules.js` before points count, but in this static build the ledger lives in
the visitor's browser, so a determined visitor can edit it. Coupons from this build are for testing.
In production `rules.js` runs on the server, and coupons are issued and redeemed by
`../coupon-service` against the shared database. `TEST_GRANTS` in `rules.js` must be `false` for
production.

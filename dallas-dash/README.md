# KFC Dallas Dash — static build

Live at `epical.art/dallas-dash` (noindex). Plain static files, no build step.

## Files

| File | What it is |
| --- | --- |
| `sim.js` | The deterministic course and physics. Shared by the client and the rules, so they can never disagree. No DOM, no timers, no `Math.random` — ports to React Native unchanged. |
| `rules.js` | Account ledger: replay verification, wallet (drumsticks, recipe cards, glide boards), shop, missions → multiplier, crew, route, coupon claim. |
| `routes.js` | The four maps (Downtown, East and West Dallas, Toronto): districts, signage, skyline, 10 landmarks each. Cosmetic only — the course is identical on every route. |
| `coupon-code.js` | `KFC-XXXX-XXXX-XXXX-C` codes: 60 random bits + a check symbol. Shared with `../coupon-service`. |
| `scene.js` | Canvas 2D software 3D renderer: streets per route, KFC restaurants/billboards/hoardings, the three characters, 40 landmarks, near-plane clipping. |
| `client.js` | Screens, swipe/keyboard/gamepad input, run loop, revive prompt. |
| `local-room.js` | Runs the rules in the browser for this static build (ledger in localStorage). |
| `audio.js` | Synthesised sound; music tempo follows run speed. |
| `assets/kfc-logo.svg` | KFC Colonel roundel (public reference copy from Wikimedia Commons). Replace with the client's official brand-kit file. |
| `tests/` | `node --test tests/rules.test.mjs`, `node tests/bot.mjs 6 10800` (fairness bot), `tests/render.html?scene=roof` (visual checks). Not deployed. |

## Gameplay

Endless three-lane runner, speed 20 → 46. Swipe to dodge, jump and roll (roll mid-air to dive). Run
up the ramps of freight containers and along their roofs; vans block a lane and oncoming vans drive
at you. Small obstacles make you stumble and a parking officer closes in — stumble again within 5 s
and you're caught. Big obstacles crash you. A glide board (double-tap, 30 s) absorbs one crash.
Secret recipe cards revive you (1, 2, 4… cards). Power-ups: drumstick magnet, jetpack, spring kicks,
2× score, each upgradable five times in the shop. Mission sets of three raise the score multiplier
(up to x6).

**Maps** (Map button / route chip): Downtown Dallas, East Dallas, West Dallas, Toronto. Each has
five districts, its own skyline, flags, trees, highway signs and 10 real landmarks (40 to collect).

**Crew**: Dante (skater courier) and Maya (street dancer) are free; R-11 (delivery robot, modelled on
today's humanoid robots) unlocks at 25,000 lifetime points. All wear KFC-branded kit, with the
Colonel roundel on the back.

**Branding placement**: KFC appears in the *world*, not on the course: KFC restaurants between
ordinary buildings (these are what the "pass N KFC restaurants" mission counts), rooftop
billboards, and construction hoardings papered with KFC posters; pickups are KFC menu items.
Obstacles, containers, vans and curbs carry no brand.

## Originality / IP notes

The genre (three lanes, swipe controls, ride-on vehicles, chaser, revive tokens, timed power-ups,
mission multipliers) is common to many endless runners and game mechanics are not protected by
copyright. What *is* protectable is expression — characters, art, names, text, music — and all of
that here is original: our own characters (no likeness of any other game's cast), our own names
(glide board, spring kicks, recipe cards, parking officer — chosen to avoid other games' terms),
code-drawn art, synthesised audio, and no third-party assets except the KFC logo used with KFC's
permission. Don't add other games' names, characters, logos, sound or UI art, and don't market the
game by comparison to another title. Real landmarks are drawn as simplified silhouettes and named
factually; buildings and public places are fine to depict, but if KFC wants to use a specific
landmark in paid advertising, have their legal team confirm it (a few, e.g. sports venues, carry
sponsor names). This is a practical summary, not legal advice.

## What this build proves, and what it doesn't

Every run is replayed by `rules.js` before points count, but in this static build the ledger lives in
the visitor's browser, so a determined visitor can edit it. Coupons from this build are for testing.
In production `rules.js` runs on the server, and coupons are issued and redeemed by
`../coupon-service` against the shared database. `TEST_GRANTS` in `rules.js` must be `false` for
production.

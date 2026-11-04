# Dash Riders

Unity 6000.3.27f1 / URP portrait delivery runner. Based on the client concept PDF and subsequent client corrections. Powered by PAGE STUDIO INC. FOOD INC rewards remain an explicitly local dummy integration.

## Current gameplay

The runner is endless. Each successful 450 m delivery starts another delivery window; the run ends when a delivery deadline is missed. There is no officer or pursuit scene. Hurdle impacts reduce speed; missed landings cost delivery time and recover the rider.

Swipe or use arrows/WASD to change lane, jump and crouch. Q activates Delivery Dash, E the crew ability, and Touch controls are available in the HUD.

- Ace: orange courier on a hoverboard; short speed boost.
- Nova: purple courier on a hoverboard; extended jump duration.
- Bolt: magenta/silver robot on his own built-in tire; one-hit protection.

Ace and Nova share a planted hoverboard stance, balance motion, steering response and jump arc. Bolt rolls on his animated built-in tire with no added vehicle. Equipment is automatic, with no on-foot mode, purchase or vehicle toggle. Hoverboards use responsive acceleration, reduced body sway and controlled banking. Swept collision checks cover thin hurdles and rapid movement.

## Districts and routes

Neon Downtown, Skypark and Festivals have separate route dressing under neutral daylight and warm white lamps. Cyan and magenta lighting and holographic advertising have been removed. Festivals currently uses a coherent Christmas market setting: fir trees, gifts, evergreen garlands, market stalls and warm white festoons. District progression uses lifetime points. The marked fourth lane is a real, raised bypass with a sustained 35% speed benefit. It is entered through a marked lead-in and returns to the main course.

Food pickups add one Food to the saved wallet. Vehicle fuel drains during a delivery, separately from the regenerating character ability meter. Within 12 m of a district kiosk, tap CHARGE or press F; the button displays the exact Food cost. One Food buys up to 25% fuel. Charging stops movement while the delivery deadline continues. See [PLAYER-GUIDE.md](PLAYER-GUIDE.md) for solo and multiplayer rules and the narrated mechanics walkthrough.

Skypark is a high-rise rooftop course: occupied towers extend 40 m down to the city, with full-width roof gaps, terrace gardens, ventilation hurdles and suspended connections. Its playable surface uses roof panels rather than asphalt; street vehicles are excluded. Optional penthouse lines run on separate tower roofs. The showcase starts with a wider view to establish the elevation.

Signature sections repeat on a shared 960 m schedule so side routes do not intersect solid tunnel walls. Wall running and gravity transitions use a shared contact frame with surface-relative jumps and collisions.

## Art and presentation

The playable crew use the original Ace, Nova and Bolt GLB models, including in Riders City. Generic imported people and procedural crew replacements are no longer selected. These original meshes retain their source textures and fixed poses; they do not gain skeletal animation from this restoration. District promenades include planted seating areas, warm service windows, clear route edges, rooftop rails and festival lights.

The interface uses Rajdhani under the SIL Open Font License, the client's pink wordmark, dark panels, flat action controls and a prominent delivery clock. The font license is included in Assets/_Project/Fonts. Distant city buildings use simplified LODs. Static streamed scenery is merged by material; generated meshes are disposed when their blocks are culled.

## Camera and rendering

The run uses one damped camera target with jump height filtered from the support surface and a faster response during wall/ceiling transfers. Collision, pickup and landing feedback no longer add camera impulses, FOV kicks or gameplay slowdowns. The visible bot uses `-dash-tour`; it does not record screenshots.

The original crew meshes retain their imported renderers and textures. Static scenery is batched across three-block groups. Distant facades use simpler geometry, the draw distance is 160 m with fog, and shadows use one hard-shadow cascade with FXAA. Older Intel HD/UHD graphics use an 85% render scale on High.

Visible player measurements on the local Intel HD 620 at a 540x960 window: Downtown improved from 23.2 FPS to 42.5 FPS (95th-percentile frame time 50.1 ms to 30.8 ms); Skypark measured 59.4 FPS and Festivals 41.5 FPS. These are real-time runs with rendering active, not minimized-window or frame-locked recording results. Continuous route and camera-framing assertions passed in all three districts. Dense street scenes remain below a locked 60 FPS on this PC.

## Dummy coupons

Three delivery badges generate a unique local coupon. Each coupon expires exactly 24 UTC hours after issue and can be redeemed once. Used state, the serial counter and the observed expiry watermark survive restart. Codes use an installation namespace, expanding serial, random nonce and checksum. These are test coupons; there is no live FOOD INC integration or server-authoritative cross-device redemption.

## Build and verification

Open Assets/_Project/Scenes/Main.unity. Editor entry points:

- DallasDash.EditorTools.ProjectBootstrap.Run
- DallasDash.EditorTools.DashValidation.Run
- DallasDash.EditorTools.BuildTools.Windows
- DallasDash.EditorTools.BuildTools.Android

Opt-in player modes include -dash-review, -dash-playtest, -dash-film and -dash-benchmark, followed by an output directory; -district selects a district. -dash-profile followed by a JSON path records real-time frame statistics. Movie export uses fixed simulation time and is not a performance measurement.

Builds, Library, temporary files, logs, captures and videos are excluded from this source backup. The local staging workspace also contains Run.ps1 and Showcase.ps1 to apply changes, validate, record and encode the showcase.

## Verification limits

Editor assertions cover coupon lifecycle, progression, surface transitions and collision geometry. Input-driven desktop playtests cover the shortcut, rooftops, wall and ceiling routes. Their logs do not establish visual acceptance or mobile performance. Current desktop measurements are recorded above; they do not establish mobile performance. A new Android device run and iOS validation remain outstanding. This is an in-progress client review build, not a claim of production acceptance.

## Free asset replacement pass

Food pickups and complete city/service models now use selected CC0 Kenney assets.
The PDF-specific equipment, rooftop plant and festival stage use locally generated
Blender meshes. All six power-ups have modeled items rather than lettered discs.
Original Ace/Nova/Bolt meshes remain selected. Source URLs, license statements,
file hashes and generation notes ship in Assets/_Project/Art/Models/FreeAssets.
Staging ASSET-REPLACEMENTS.md records the mapping and remaining visual limits.

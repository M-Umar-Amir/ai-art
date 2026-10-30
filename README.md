# Dash Riders

Unity 6000.3.27f1 / URP portrait delivery runner. Based on the client concept PDF and subsequent client corrections. Powered by PAGE STUDIO INC. FOOD INC rewards remain an explicitly local dummy integration.

## Current gameplay

The runner is endless. Each successful 450 m delivery starts another delivery window; the run ends when a delivery deadline is missed. There is no officer or pursuit scene. Hurdle impacts reduce speed; missed landings cost delivery time and recover the rider.

Swipe or use arrows/WASD to change lane, jump and crouch. Q activates Delivery Dash, E the crew ability, and B protective equipment. Touch controls are available in the HUD.

- Ace: orange bomber jacket, black cargos, electric board, short speed boost.
- Nova: purple athletic courier, rooftop parkour, extended jump duration.
- Bolt: the PDF image's helmeted magenta/silver mono-wheel courier with delivery cargo pods and one-hit protection. The old two-legged silver/yellow model is superseded.

Running, electric boards, glide boards and the mono-wheel have distinct steering inertia, jump/gravity values and collision dimensions. Swept collision checks cover thin hurdles and rapid movement; a shield does not erase heavy vehicles. Buffered jumps and a short coyote window help roof-edge inputs.

## Districts and routes

Neon Downtown, Skypark and Festivals have separate lighting and route dressing. District progression uses lifetime points. The cyan fourth lane is a real, raised bypass with a sustained 35% speed benefit. It is entered through a marked lead-in and returns to the main course.

Skypark is a high-rise rooftop course: occupied towers extend 40 m down to the city, with full-width roof gaps, terrace gardens, ventilation hurdles and suspended connections. Its playable surface uses roof panels rather than asphalt; street vehicles are excluded. Optional penthouse lines run on separate tower roofs. The showcase starts with a wider view to establish the elevation.

Signature sections repeat on a shared 960 m schedule so side routes do not intersect solid tunnel walls. Wall running and gravity transitions use a shared contact frame with surface-relative jumps and collisions.

## Art and presentation

The current Ace, Nova and Bolt GLBs use clean reference inputs and native UV textures. Mobile geometry budgets are approximately 44k faces for Ace/Nova and 52k for Bolt. Nova uses lightweight vertex gait motion; skeletal action clips are not implemented.

The interface uses Rajdhani under the SIL Open Font License, the client's pink wordmark, dark panels, flat action controls and a prominent delivery clock. The font license is included in Assets/_Project/Fonts. Distant city buildings use simplified LODs. Static streamed scenery is merged by material; generated meshes are disposed when their blocks are culled.

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

Editor assertions cover coupon lifecycle, progression, surface transitions and collision geometry. Input-driven desktop playtests cover the shortcut, rooftops, wall and ceiling routes. Their logs do not establish visual acceptance or mobile performance. The earlier 540x960 High-tier profile measured about 41 FPS on an Intel HD 620 after initial optimization; the rebuilt high-rise district requires its own final profile. A new Android device run and iOS validation remain outstanding. This is an in-progress client review build, not a claim of production acceptance.

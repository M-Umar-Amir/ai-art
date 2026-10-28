# Food Inc Game — Delivery Chase (Unity)

Unity port of the Dallas Dash runner. It follows `ai-art/dallas-dash-docs/v3/food-inc-game-asset-pack.pdf` (24 Oct 2026). The 22 Oct pack is archived in `_archive/`, outside Assets.

- **Engine:** Unity 6000.3.27f1 (Unity 6.3 LTS), URP, Input System. Portrait, Android-first (IL2CPP, ARM64, min API 26).
- **Everything lives on D:.** Hub is at `G:\Unity\Hub (D:\Unity is a junction)`, editors are in `D:\Unity\Editors`, and downloads go to `D:\Unity\_downloads`.

## First open
1. Open Unity Hub and sign in. A free Personal license is enough. Then **Add → Add project from disk →** `D:\ai-art\dallas-dash-unity`.
2. Once the project has imported, run **Dallas Dash → Bootstrap Project**. This creates the data assets, the materials and `Scenes/Main.unity`.
3. Press Play. Steer with the arrows/WASD or swipe. Up/Space jumps and Down rolls.

Headless (after the license is activated):
```
"D:\Unity\Editors\6000.3.27f1\Editor\Unity.exe" -batchmode -quit -projectPath D:\ai-art\dallas-dash-unity -executeMethod DallasDash.EditorTools.ProjectBootstrap.Run -logFile -
```

## Layout
| Path | What |
|---|---|
| `Assets/_Project/Art/Reference/` | The 22 sheets pulled out of the asset pack: Characters, Rides, Landmarks, Scenes, Collectibles |
| `Assets/_Project/Scripts/Data/` | `CharacterDef`, `RideDef`, `LandmarkDef` and `PickupDef` ScriptableObjects |
| `Assets/_Project/Scripts/Core/` | `Tuning` (constants copied from the web `sim.js` so the pace matches), `GameManager`, `RiderInput` |
| `Assets/_Project/Scripts/Gameplay/` | Rider (3 lanes, jump/roll), `TrackSpawner` (straight track with no junctions), `ChaseController` (2 police sport bikes, second stumble = busted), camera, HUD |
| `Assets/_Project/Editor/ProjectBootstrap.cs` | One-click setup of data, materials, greybox scene and player settings |

## Rules from the pack (24 Oct 2026)
- The game is **Food Inc Game**. The map is Dallas only, dressed for Christmas: string lights, wreaths, garlands, trees and gifts. The track is straight and linear, with no junctions.
- The characters are **Finn** (the player, royal blue), **Faye** (forest green) and **Fizz-11** (cream and yellow robot, unlocks at 25,000 points). All wear a Dallas western look.
- Fizz-11 rolls on a single tire, so he needs a rolling-base rig rather than a biped rig.
- Rides: the warm-yellow **bicycle** (starter) and the **scooter**, which is earned through play and never sold. The car and the hatch have been cut.
- The chaser is **one officer on a police bicycle**: plain white POLICE saddle bag, red-and-blue bar light, hi-vis accents.
- There is **one pickup only**: Bellagreen's Wagyu Bacon Burger.
- Obstacles:
  - Jump: presents, cone, roadworks barrier, wheelie bins.
  - Change lane: Christmas tree in a planter, jaywalking shopper, parked food truck.
- Signage uses invented outlets only: CLUCK & CO, SLICE HOUSE, BURGER BARN, DONUT DEPOT, TAQUERIA SOL. No real chain logos.
- The HUD is a separate code layer, so the scenes stay clean. Busted ends the run, and the points earned are kept.

## Still open
- The art is still renders. Every model in the scene is a greybox placeholder until the meshes are built from the sheets. Drop a prefab into the matching `*Def` asset and the game uses it automatically.
- **Proposed, needs client approval:** a low garland banner as the only roll-under obstacle. The pack has nothing to roll under.

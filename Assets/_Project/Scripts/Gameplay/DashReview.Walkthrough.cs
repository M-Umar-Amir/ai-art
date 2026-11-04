using System;
using System.Collections;
using System.IO;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.Gameplay
{
    public partial class DashReview
    {
        public static bool WalkthroughActive { get; private set; }
        public static bool WalkingCity { get; private set; }
        public static Vector2 WalkthroughInput { get; private set; }
        public static string WalkthroughCaption { get; private set; } = "";
        string walkthroughRoot;

        void CheckWalkthroughLayout()
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
            {
                if (!button.gameObject.activeInHierarchy) continue;
                var safe = button.GetComponentInParent<SafeArea>();
                if (!safe) continue;
                var content = safe.transform.Find("Content") as RectTransform;
                if (!content) continue;
                var rect = button.transform as RectTransform;
                var corners = new Vector3[4]; rect.GetWorldCorners(corners);
                bool inside = true;
                foreach (var corner in corners)
                {
                    var point = content.InverseTransformPoint(corner);
                    inside &= Mathf.Abs(point.x) <= 550 && Mathf.Abs(point.y) <= 970;
                }
                Check(inside, "control stays inside safe area: " + button.name);
            }
        }

        IEnumerator WalkShot(string label, float hold = 1.8f)
        {
            if (label == "title")
            {
                var play = VisibleButton("START DELIVERY"); var explore = VisibleButton("EXPLORE RIDERS CITY") ?? VisibleButton("FREE ROAM");
                Check(play && explore, "primary title controls exist");
                var a = (RectTransform)play.transform; var b = (RectTransform)explore.transform;
                float gap = a.anchoredPosition.y - a.rect.height * .5f - (b.anchoredPosition.y + b.rect.height * .5f);
                Check(gap >= 16, "Start Delivery and Explore City have separate unobstructed hit areas");
            }
            string explanation = WalkExplanation(label);
            if (recording && explanation.Length > 0)
            {
                Guide(label.Replace('-', ' ').ToUpperInvariant(), explanation);
                bool live = GameManager.Instance.State == RunState.Running && !GameManager.Instance.Paused;
                if (!live) hold = Mathf.Max(hold, explanation.Split(' ').Length / 2.5f + 2f);
            }
            File.AppendAllText(Path.Combine(walkthroughRoot, "events.csv"), Time.realtimeSinceStartup.ToString("F2", System.Globalization.CultureInfo.InvariantCulture) + "," + label + "\n");
            Debug.Log("WALKTHROUGH_STAGE " + label);
            yield return Shot(label);
            yield return GuideHold(hold);
        }

        static string WalkExplanation(string label) => label switch
        {
            "title" => "Welcome to Dash Riders. This full walkthrough covers the menus, district courses, burgers and charge, kiosk controls, wall runs, rooftops and run results.",
            "crew-ace" => "Choose a rider in the crew menu. Ace rides his courier e-bike and has a short speed boost ability.",
            "crew-nova" => "Nova rides her purple e-bike. Her ability extends jump duration, helping with airborne sections.",
            "crew-bolt" => "Bolt rolls on his built in tire. His protective ability absorbs a hit.",
            "shop" => "The shop contains power up upgrades. This demonstration uses a temporary unlocked profile; the saved profile will be restored afterward.",
            "missions" => "Missions track delivery objectives and award rewards. Read each target and its progress before starting a run.",
            "ranks" => "Rankings show recorded runs across daily, weekly and all time boards.",
            "daily" => "Daily rewards are claimed from this calendar when the next reward becomes available.",
            "districts" => "Select a district here. Downtown is a city course, Skypark uses rooftops, and Festivals is a snowy Christmas street with snowmen, sleighs and presents to dodge.",
            "rewards" => "Burgers buy local demonstration coupons. These are test rewards, with an expiry and a one use redemption state.",
            "settings" => "Settings control graphics, audio, motion and haptics. The walkthrough visits each graphics tier and adjusts the audio controls.",
            "city-directory" => "The city directory provides rider services and destinations. Every destination opens the same panel as its menu tile.",
            "city-network" => "Open Network to host or join a local session. The host chooses an unlocked race district before starting the race.",
            "city-service-2" => "Burgers are the one currency: earned from burger pickups and delivery tips, spent in this shop, at kiosks and on unlocks.",
            "city-service-3" => "A district run starts fully charged. During the run, spend burgers at an on course kiosk to refill the vehicle; the walkthrough will demonstrate the control.",
            "delivery-intro" => "Start Delivery opens an endless solo run. Steer around hurdles, jump low barriers and slide under overhead obstacles. Deliveries come every 450 metres; the run lasts until you crash twice or run out of charge.",
            "pause" => "Pause stops riding and the on-time clock. Charging is different: it stops movement while the on-time window keeps counting.",
            "pause-settings" => "Settings can also be opened from pause. Done returns to pause, and Resume starts a short countdown.",
            "run-over-results" => "The results card after the run ends. A second crash before remounting, or six seconds on a dry battery away from a kiosk, ends an endless run.",
            "walkthrough-complete" => "The walkthrough is complete: menus, all three districts, real kiosk charging, wall runs, rooftops and results. Your original saved profile will be restored after recording.",
            // Documentary-specific explanations
            "documentary-title" => "Welcome to Dash Riders. This tour covers every menu: the crew, the shop, missions, districts, rewards, daily bonus and settings.",
            "documentary-menu-complete" => "That is the full menu tour. Start Delivery begins an endless run in the selected district.",
            "documentary-crew" => "Select your rider from the crew menu. Each character has unique abilities that shape your playstyle.",
            "documentary-crew-nova" => "Nova specializes in extended jumps, perfect for clearing large gaps and reaching high routes.",
            "documentary-crew-bolt" => "Bolt rides a magnetic wheel with a protective shield ability that absorbs one hit.",
            "documentary-missions" => "Missions provide objectives and rewards. Each completed mission pays burgers.",
            "documentary-shop" => "Upgrade power-ups here with burgers. Each upgrade extends power-up duration and effectiveness.",
            "documentary-daily" => "Daily rewards grow your streak. Log in each day to claim growing burger bonuses.",
            "documentary-settings" => "Adjust graphics quality, audio levels, haptics, and motion settings. Higher graphics add visual detail.",
            "documentary-delivery-start" => "Starting a delivery run in Neon Downtown. Navigate streets, collect burgers, and reach delivery points before time expires.",
            "documentary-downtown-gameplay" => "Downtown runs on three street lanes. Watch for autonomous pods and moving platforms, and take the rooftop ramp in the right lane for a speed boost and a shortcut over the traffic.",
            "documentary-districts" => "Pick a district before riding. Downtown is the neon street course, Skypark runs across the rooftops, and Festival Grounds threads a snowy Christmas market street market.",
            "documentary-rewards" => "Every 450 metres a delivery pays a burger tip. Burgers buy demo Food Inc coupons with an expiry and a one-use redemption state. These are local test codes, not real orders.",
            "documentary-results" => "Run complete. Your score, distance and burgers are recorded. Spend burgers on upgrades, districts, Bolt and outfits.",
            _ => ""
        };

        Button VisibleButton(string label)
        {
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.gameObject.activeInHierarchy && button.interactable)
                    foreach (var text in button.GetComponentsInChildren<Text>())
                        if (text.text == label) return button;
            return null;
        }
        void Click(string label)
        {
            var button = VisibleButton(label);
            Check(button, "usable button exists: " + label);
            button.onClick.Invoke();
        }

        IEnumerator FullWalkthrough()
        {
            walkthroughRoot = output;
            File.WriteAllText(Path.Combine(output, "events.csv"), "seconds,stage\n");
            PlayerPrefs.SetInt("coached_runs", 2);
            BurgerPrices.UnlockAllForReview();
            PlayerProfile.Burgers = Mathf.Max(5000, PlayerProfile.Burgers); BurgerPrices.MigrateOnce();
            PlayerProfile.Graphics = 2; GameSettings.ApplyGraphics();
            PlayerProfile.Character = 0;
            yield return new WaitForSecondsRealtime(6);
            var menu = MenuUI.Instance;
            var gm = GameManager.Instance;
            var run = DashRun.Instance;
            Check(menu && gm && run, "walkthrough scene dependencies");
            menu.Open("none");
            if (!string.IsNullOrEmpty(encoderPath))
            {
                filmSeconds = 1800; PrepareFilmCamera(); SaveFilmMusic(); StartFilmEncoder();
                Time.captureFramerate = 30; Application.targetFrameRate = 30; recording = true;
            }
            yield return WalkShot(mpOnly ? "mp-title" : "title");

            if (!mpOnly) {
            menu.Open("crew");
            yield return WalkShot("crew-ace");
            menu.FlipCrew(1); yield return WalkShot("crew-nova");
            if (VisibleButton("RIDE")) { Click("RIDE"); yield return WalkShot("crew-nova-selected", 1); }
            menu.FlipCrew(1); yield return WalkShot("crew-bolt");
            if (VisibleButton("RIDE")) { Click("RIDE"); yield return WalkShot("crew-bolt-selected", 1); }
            menu.FlipCrew(1);
            if (VisibleButton("RIDE")) Click("RIDE");
            menu.Open("none");
            foreach (string panel in new[] { "shop", "missions", "ranks", "daily", "districts", "rewards", "settings" })
            {
                menu.Open(panel);
                yield return WalkShot(panel);
                if (panel == "shop" && PlayerProfile.UpgradeLevel(0) < PowerUps.MaxLevel)
                {
                    int before = PlayerProfile.UpgradeLevel(0);
                    foreach (var button in menu.GetComponentsInChildren<Button>())
                        if (button.interactable && button.transform.parent.name == PowerUps.Names[0]) { button.onClick.Invoke(); break; }
                    Check(PlayerProfile.UpgradeLevel(0) == before + 1, "upgrade purchase updates its level");
                    yield return WalkShot("shop-upgraded");
                }
                if (panel == "settings")
                {
                    Click("LOW"); yield return WalkShot("settings-low", 1);
                    Click("MEDIUM"); yield return WalkShot("settings-medium", 1);
                    Click("HIGH");
                    foreach (var slider in FindObjectsByType<Slider>(FindObjectsSortMode.None))
                        if (slider.gameObject.activeInHierarchy) slider.value = .55f;
                    yield return WalkShot("settings-audio");
                    Click("DONE");
                }
                menu.Open("none");
            }
            }

            WalkingCity = true;
            menu.Open("city");
            yield return WalkShot("city-directory");
            var world = FindFirstObjectByType<RidersCityWorld>();
            if (!mpOnly) {
            for (int destination = 0; destination < RidersCityWorld.Destinations.Length; destination++)
            {
                float until = Time.realtimeSinceStartup + 20;
                while (Time.realtimeSinceStartup < until)
                {
                    var delta = RidersCityWorld.Destinations[destination] - world.Position;
                    if (delta.magnitude < 1.4f) break;
                    WalkthroughInput = new Vector2(delta.x, delta.z).normalized;
                    WalkthroughCaption = "Riding to " + RidersCityWorld.DestinationNames[destination].ToLowerInvariant();
                    yield return null;
                }
                WalkthroughInput = Vector2.zero;
                Check(world.CanVisit, "destination reached by steering");
                yield return WalkShot("city-destination-" + destination, .8f);
                Click("VISIT");
                yield return WalkShot("city-service-" + destination);
                menu.Open("none"); menu.Open("city");
            }
            }
            WalkthroughInput = Vector2.zero;
            WalkingCity = false; menu.Open("none");

            // Real pause, settings and resume callbacks before the longer input-driven routes.
            RiderInput.Override = () => RiderInput.Action.None;
            Click("START DELIVERY");
            yield return WalkShot("delivery-intro", .8f);
            float introWait = Time.realtimeSinceStartup + 30;
            while (gm.State != RunState.Running && Time.realtimeSinceStartup < introWait) yield return null;
            Check(gm.State == RunState.Running, "start delivery opens the run");
            menu.Open("pause"); yield return WalkShot("pause");
            menu.Open("pause-settings"); yield return WalkShot("pause-settings");
            Click("DONE"); Click("RESUME");
            yield return new WaitForSecondsRealtime(2.2f);
            Check(!gm.Paused, "resume countdown completes");
            menu.Open("pause"); Click("QUIT RUN");
            RiderInput.Override = null;

            yield return MechanicsWalkthrough();

            for (district = 0; district < 3; district++)
            {
                if (recording) Guide("DISTRICT " + DashRun.DistrictNames[district], DistrictIntro[district]);
                output = Path.Combine(walkthroughRoot, "district-" + district);
                Directory.CreateDirectory(output);
                continuous = true;
                yield return ContinuousRun();
                continuous = false;
                output = walkthroughRoot;
                yield return WalkShot("district-" + district + "-complete");
            }

            // A real end of an endless run: hands off the bars, the rider crashes twice and is wiped out.
            RiderInput.Override = () => RiderInput.Action.None;
            gm.StartRun(); yield return new WaitForSecondsRealtime(.5f);
            float wipeWait = Time.realtimeSinceStartup + 90;
            while (gm.State == RunState.Running && Time.realtimeSinceStartup < wipeWait) yield return null;
            Check(gm.State == RunState.Busted && DashRun.EndReason == "WIPED OUT", "two crashes end the endless run");
            yield return new WaitForSecondsRealtime(1.6f);
            yield return WalkShot("run-over-results", 3);
            gm.ToMenu(); RiderInput.Override = null;
            menu.Open("none");
            yield return WalkShot("walkthrough-complete", 2);
            if (recording)
            {
                recording = false; FinishFilmEncoder();
                File.WriteAllText(Path.Combine(walkthroughRoot, "mechanics-chapters.json"), JsonUtility.ToJson(new GuideChapters { chapters = guideChapters.ToArray() }, true));
            }
            File.WriteAllText(Path.Combine(walkthroughRoot, "walkthrough-pass.txt"),
                "PASS: menus, every city destination, pause/settings/resume, three continuous 725m district runs, kiosk close-up pit stop, real two-crash end of an endless run.\n" +
                "Temporary unlocked review profile.\n" +
                assertions + " assertions; " + shot + " screenshots. Profile restored at exit.\n");
            Debug.Log("FULL_WALKTHROUGH_PASS");
            Restore(); Application.Quit(0);
        }

        static readonly string[] DistrictIntro =
        {
            "Neon Downtown: three street lanes between lit brick shopfronts, neon traffic and autonomous delivery pods. The kicker ramp in the right lane launches a parkour climb onto the real rooftops.",
            "Skypark District: the course runs high above the city on linked tower roofs. Jump the open gaps between buildings, dodge rooftop plant and vents, and take the marked shortcut line for extra speed.",
            "Festivals: a snowy Christmas street from start to finish, with decorated firs, market chalets, string lights, wreaths, a MERRY CHRISTMAS arch and falling snow. Snowmen, sleighs, tree carts, presents, snow drifts and candy-cane barriers are the hurdles. The same street lanes, hurdles and rooftop shortcut, dressed for the season."
        };

        /// Kiosk close-up: a side angle on the food and battery kiosk while the pit stop runs.
        void KioskCamera(bool on, int index)
        {
            var follow = FindFirstObjectByType<FollowCamera>();
            if (!follow) return;
            if (!on) { follow.cinematic = false; return; }
            follow.cinematic = true;
            float z = FuelStops.StationAt(Mathf.Max(0, index));
            const float y = 0f; // every district rides at y=0 (Skypark lowers the city instead)
            var rig = follow.CinematicRig;
            rig.position = new Vector3(-0.6f, y + 3.1f, z - 9.5f);
            rig.rotation = Quaternion.LookRotation(new Vector3(5.2f, y + 1.3f, z - 0.5f) - rig.position);
        }

        /// Rides to the next kiosk with the normal bot, then a clear, narrated pit stop.
        IEnumerator DocumentaryKiosk(GameManager gm, RiderController rider, int number, bool full)
        {
            float timeout = Time.realtimeSinceStartup + 120;
            while ((!FuelStops.Instance.NearStation || rider.Airborne || rider.Traversal.Surface != RunSurface.Road && rider.Traversal.Surface != RunSurface.Rooftop) && gm.State == RunState.Running && Time.realtimeSinceStartup < timeout) yield return null;
            if (gm.State != RunState.Running) yield break;
            Check(FuelStops.Instance.NearStation, "documentary reaches kiosk " + number);
            int station = FuelStops.Instance.StationIndex;
            gm.SetPaused(true);
            KioskCamera(true, station);
            yield return GuideHold(1.2f);
            if (!full)
            {
                Guide("KIOSK: " + DashRun.DistrictNames[DashRun.District], "The same burger and battery kiosk stands beside the route in every district, every 480 metres. Tap CHARGE AT KIOSK or press F.");
                yield return Shot("documentary-kiosk-" + number);
                yield return GuideHold(10.5f);
            }
            else
            {
            Guide("BURGER & BATTERY KIOSK", "Every 480 metres a kiosk stands beside the route: the burger counter and the battery charger. Your e-bike burns charge as you ride. If the tank runs dry you have six seconds to reach a kiosk, or the run ends.");
            yield return Shot("documentary-kiosk-" + number);
            yield return GuideHold(18.5f);
            Guide("TAP CHARGE OR PRESS F", "Within 12 metres of the kiosk, tap CHARGE AT KIOSK or press F. One burger buys a quarter tank; the button shows the exact cost. No burgers? Each kiosk gives one free emergency splash.");
            yield return GuideHold(15.5f);
            }
            gm.SetPaused(false); yield return null;
            if (RidersEconomy.Fuel > .97f) RidersEconomy.Burn(12f, gm.Speed); // demo: show a real top-up, not a full tank
            int wallet = RidersEconomy.Burgers; float beforeFuel = RidersEconomy.Fuel, beforeDistance = gm.Distance;
            Check(FuelStops.Instance.TryStartPit(out string message), "kiosk charge starts: " + message);
            Guide("CHARGING", "The rider stops at the kiosk while the battery fills and the burgers are spent. Charging costs time on the delivery clock, so choose when to stop.");
            yield return Shot("documentary-kiosk-" + number + "-charging");
            int chargingFrame = filmFrame;
            float pitTimeout = Time.realtimeSinceStartup + 30;
            while (FuelStops.Instance.Pitting && Time.realtimeSinceStartup < pitTimeout) yield return null;
            gm.SetPaused(true);
            // Let the charging line finish before the next one (about 2.5 words a second).
            float spoken = 26 / 2.5f + 1f, elapsed = (filmFrame - chargingFrame) / (float)CaptureRate;
            if (recording && elapsed < spoken) yield return GuideHold(spoken - elapsed);
            Check(RidersEconomy.Fuel > beforeFuel, "kiosk adds charge");
            Check(RidersEconomy.Burgers < wallet || FuelStops.Instance.EmergencySplashes > 0, "kiosk charge is paid in burgers or is the emergency splash");
            Check(gm.Distance - beforeDistance < 3, "pit stop holds the rider at the kiosk");
            gm.SetPaused(true);
            Guide("CHARGED", $"Battery at {RidersEconomy.FuelPercent * 100f:0} percent. Burger wallet now {RidersEconomy.Burgers}. Back on the road.");
            yield return GuideHold(6);
            KioskCamera(false, station);
            yield return GuideHold(1);
            gm.SetPaused(false);
        }

        /// One district ride inside the documentary: intro, live bot riding, rooftop/wall captions, one kiosk pit stop.
        /// A bot wipe-out before the kiosk is shown is narrated and the district is ridden again (at most twice).
        /// Narrated beat during a district ride: the ride freezes while the line is spoken, then carries on. Pausing
        /// moves nothing and spawns nothing, so the proven route plays out as it did in the bot check.
        IEnumerator Explain(string title, string body, string shotLabel)
        {
            var gm = GameManager.Instance;
            gm.SetPaused(true);
            Guide(title, body);
            if (guideMode) guideMode.text = "PAUSED FOR EXPLANATION";
            if (shotLabel != null) yield return Shot(shotLabel);
            yield return GuideHold(body.Split(' ').Length / 2.5f + 1.5f);
            if (guideMode) guideMode.text = "LIVE GAMEPLAY";
            gm.SetPaused(false);
        }

        IEnumerator DocumentaryDistrict(int d, bool last, TrackSpawner track, RiderController rider)
        {
            var gm = GameManager.Instance; var run = DashRun.Instance; var menu = MenuUI.Instance;
            DashRun.District = d;
            Check(DashRun.District == d, "district " + d + " selected");
            for (int attempt = 0; ; attempt++)
            {
                // Replays the course layouts the Autopilot rode to 1000 m in the bot check (review/botcheck-20261102-195641):
                // seed 9000 + district*17 + run*101 for the runs that survived. A retry takes the next proven route.
                int[][] proven = { new[] { 9101, 9303, 9404 }, new[] { 9017, 9118, 9219, 9421 }, new[] { 9034, 9135, 9236, 9337, 9438 } };
                menu.Open("none");
                UnityEngine.Random.InitState(proven[d][Mathf.Min(attempt, proven[d].Length - 1)]);
                RiderInput.Override = () => Autopilot.Steer(track, rider);
                PlayerProfile.Burgers = 20; // the bot check's wallet, so kiosk stops match the proven runs
                gm.StartRun();
                Check(gm.State == RunState.Running, "the " + DashRun.DistrictNames[d] + " run starts");
                yield return GuideHold(1.5f);
                if (attempt == 0)
                {
                    yield return Explain(DashRun.DistrictNames[d], DistrictIntro[d], "documentary-" + d + "-gameplay");
                    if (d == 0)
                    {
                        yield return GuideHold(2);
                        yield return Explain("ENDLESS RUN", "The run never stops on its own. Deliveries come every 450 metres: arrive inside the on-time window for the full bonus, late drops still count. Speed keeps rising the further you ride.", null);
                        yield return GuideHold(2);
                        yield return Explain("BURGER PICKUPS", "Each burger adds one to your wallet. Burgers pay for charge at the kiosks and everything in the shop. There is no other currency for upgrades.", null);
                    }
                }
                else yield return Explain("NEW RUN", "The bot was wiped out, so this is a fresh run of " + DashRun.DistrictNames[d] + " on another proven route.", null);

                bool roofShown = false, wallShown = false, kioskShown = false;
                float runTimeout = Time.realtimeSinceStartup + 300;
                float target = last ? 900f : 620f;
                while (gm.State == RunState.Running && gm.Distance < target && Time.realtimeSinceStartup < runTimeout)
                {
                    if (!roofShown && run.OnRooftops && !rider.Traversal.Moving)
                    {
                        roofShown = true;
                        yield return Explain("ROOFTOP SHORTCUT", d == 1
                            ? "Even up here there is a higher line: the kicker ramp in the right lane climbs onto the next tier of roofs. Jump each gap, or take a glass skybridge. The shortcut keeps a 35 percent speed surge."
                            : "Ride the kicker ramp in the right lane: the rider climbs the facade and runs across the roofs. Jump each alley gap, or take a glass skybridge. The shortcut keeps a 35 percent speed surge.", "documentary-" + d + "-rooftops");
                    }
                    if (!wallShown && run.WallRunning && !rider.Traversal.Moving)
                    {
                        wallShown = true;
                        yield return Explain("WALL RUN", "Any street facade can be run: from an outer lane, steer into the building. Ace and Nova wall-run on foot, after a crash; Bolt's magnetic wheel rides the walls any time. Rooftops are the opposite: Ace and Nova need the e-bike for the ramp.", "documentary-" + d + "-wall-run");
                    }
                    if (!kioskShown && gm.Distance > FuelStops.FirstStation - 40f)
                    {
                        kioskShown = true;
                        yield return DocumentaryKiosk(gm, rider, d + 1, d == 0);
                        if (guideMode) guideMode.text = "LIVE GAMEPLAY";
                    }
                    yield return null;
                }
                if (gm.State == RunState.Busted && DashRun.EndReason == "WIPED OUT" && !kioskShown && attempt < MaxWipeoutRetries)
                {
                    File.AppendAllText(Path.Combine(output, "wipeouts.txt"), $"{DashRun.DistrictNames[d]}: bot wiped out at {gm.Distance:0} m; ridden again\n");
                    yield return GuideHold(3);
                    gm.ToMenu(); menu.Open("none");
                    yield return GuideHold(1);
                    continue;
                }
                Check(kioskShown, DashRun.DistrictNames[d] + " documentary shows its kiosk pit stop");
                if (last && gm.State == RunState.Running)
                {
                    yield return Explain("HOW A RUN ENDS", "Two ways: crash again before you get back on the bike, or let the battery run dry away from a kiosk. Here the rider lets go of the bars.", null);
                    RiderInput.Override = () => RiderInput.Action.None;
                    float endWait = Time.realtimeSinceStartup + 90;
                    while (gm.State == RunState.Running && Time.realtimeSinceStartup < endWait) yield return null;
                    if (gm.State == RunState.Running) run.End("RUN OVER");
                }
                if (gm.State == RunState.Busted)
                {
                    yield return GuideHold(2);
                    Guide("RESULTS: " + DashRun.EndReason, "Score, distance, deliveries made on time, shortcuts taken and mission progress. Burgers earned are kept.");
                    yield return GuideHold(2.5f);
                    yield return Shot("documentary-" + d + "-results");
                    yield return GuideHold(8);
                }
                else
                {
                    Guide("NEXT DISTRICT", DashRun.DistrictNames[d] + " done. Back to the menu to ride " + DashRun.DistrictNames[d + 1] + ".");
                    yield return GuideHold(4);
                }
                RiderInput.Override = null;
                gm.ToMenu(); menu.Open("none");
                yield return GuideHold(1);
                yield break;
            }
        }

        IEnumerator DocumentaryWalkthrough()
        {
            walkthroughRoot = output;
            File.WriteAllText(Path.Combine(output, "events.csv"), "seconds,stage\n");
            PlayerPrefs.SetInt("coached_runs", 2);
            BurgerPrices.UnlockAllForReview();
            PlayerProfile.Burgers = 5000;
            PlayerProfile.Burgers = Mathf.Max(PlayerProfile.Burgers, 12);
            PlayerProfile.Graphics = 2;
            GameSettings.ApplyGraphics();
            PlayerProfile.Character = 0;
            DashRun.District = 0;
            yield return new WaitForSecondsRealtime(6);
            var menu = MenuUI.Instance;
            var gm = GameManager.Instance;
            var run = DashRun.Instance;
            Check(menu && gm && run, "documentary scene dependencies");
            menu.Open("none");
            if (!string.IsNullOrEmpty(encoderPath))
            {
                filmSeconds = 900;
                PrepareFilmCamera();
                SaveFilmMusic();
                StartFilmEncoder();
                Time.captureFramerate = 30;
                Application.targetFrameRate = 30;
                recording = true;
            }

            // Menu tour (same as the approved Downtown documentary). -skip-menu records the district rides only,
            // to be joined after the approved menu-tour footage.
            if (!skipMenu) {
            yield return WalkShot("documentary-title");
            menu.Open("crew");
            yield return WalkShot("documentary-crew");
            menu.FlipCrew(1); yield return WalkShot("documentary-crew-nova");
            menu.FlipCrew(1); yield return WalkShot("documentary-crew-bolt");
            menu.FlipCrew(1);
            if (VisibleButton("RIDE")) Click("RIDE");
            menu.Open("none");
            foreach (string panel in new[] { "shop", "missions", "districts", "rewards", "daily", "settings" })
            {
                menu.Open(panel);
                yield return WalkShot("documentary-" + panel);
                menu.Open("none");
            }
            yield return WalkShot("documentary-menu-complete");
            }
            if (menuOnly)
            {
                if (recording)
                {
                    recording = false;
                    FinishFilmEncoder();
                    File.WriteAllText(Path.Combine(output, "mechanics-chapters.json"), JsonUtility.ToJson(new GuideChapters { chapters = guideChapters.ToArray() }, true));
                }
                File.WriteAllText(Path.Combine(output, "documentary-pass.txt"), $"PASS: menu tour (title, crew x3, shop, missions, districts, rewards, daily, settings).\nAssertions: {assertions}, Screenshots: {shot}\n");
                Debug.Log("DOCUMENTARY_WALKTHROUGH_PASS");
                Restore(); Application.Quit(0); yield break;
            }

            var track = FindFirstObjectByType<TrackSpawner>();
            var rider = FindFirstObjectByType<RiderController>();
            var canvasObject = new GameObject("Documentary explanations", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            if (recording && filmCamera)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = filmCamera;
                canvas.planeDistance = .5f;
            }
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = .5f;
            var root = SafeArea.Create(canvasObject.transform);
            var card = UiKit.Panel(root, "Documentary caption", new Vector2(0, -385), new Vector2(980, 260), UiKit.Navy);
            guideTitle = UiKit.Label(card.transform, "", 36, new Vector2(0, 86), new Vector2(930, 50), UiKit.Yellow);
            guideBody = UiKit.Label(card.transform, "", 30, new Vector2(0, 0), new Vector2(920, 130), Color.white);
            guideMode = UiKit.Label(card.transform, "", 22, new Vector2(0, -105), new Vector2(920, 36), UiKit.Muted);

            for (int d = 0; d < 3; d++)
                yield return DocumentaryDistrict(d, d == 2, track, rider);

            Destroy(canvasObject);
            guideTitle = guideBody = guideMode = null;
            if (recording)
            {
                recording = false;
                FinishFilmEncoder();
                File.WriteAllText(Path.Combine(output, "mechanics-chapters.json"),
                    JsonUtility.ToJson(new GuideChapters { chapters = guideChapters.ToArray() }, true));
            }
            File.WriteAllText(Path.Combine(output, "documentary-pass.txt"),
                $"PASS: menu tour (title, crew x3, shop, missions, districts, rewards, daily, settings), then endless runs in all three districts with bot input, one narrated kiosk pit stop per district, real end of run in Festivals ({DashRun.EndReason}).\n" +
                $"Assertions: {assertions}, Screenshots: {shot}. Wipe-out retries: see wipeouts.txt if present.\n");
            Debug.Log("DOCUMENTARY_WALKTHROUGH_PASS");
            Restore();
            Application.Quit(0);
        }
    }
}

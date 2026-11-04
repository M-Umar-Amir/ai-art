using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.Rendering;

namespace DallasDash.Gameplay
{
    // Opt-in review harness. It restores changed preferences before quitting; never active in normal play.
    [DefaultExecutionOrder(2000)]
    public partial class DashReview : MonoBehaviour
    {
        string output;
        int district, shot, assertions;
        bool continuous;
        bool walkthrough;
        bool mechanics;
        bool documentary;
        int CaptureRate => walkthrough ? 15 : 30;
        int walkthroughRenderTick;
        bool film, benchmark, recording;
        int filmFrame;
        int filmSeconds = 60;
        System.Diagnostics.Process filmEncoder;
        byte[] filmBytes;
        string encoderPath;
        readonly System.Text.StringBuilder encoderErrors = new();
        readonly List<string> filmEvents = new();
        int filmCycle = -1, previousRouteKind;
        RunSurface previousFilmSurface;

        RenderTexture filmTarget;
        Texture2D filmPixels;
        Camera filmCamera;
        Canvas[] filmCanvases;
        RenderMode[] filmModes;
        Camera[] filmCameras;
        float filmAspect;
        bool comprehensive;
        int comprehensiveChar, comprehensiveVeh;
        bool restored;
        readonly Dictionary<string, (bool exists, int value)> ints = new();
        readonly Dictionary<string, (bool exists, string value)> strings = new();
        readonly Dictionary<string, (bool exists, float value)> floats = new();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dash-review" || args[i] == "-dash-playtest" || args[i] == "-dash-film" || args[i] == "-dash-benchmark" || args[i] == "-dash-walkthrough" || args[i] == "-dash-mechanics" || args[i] == "-dash-documentary" || args[i] == "-dash-botcheck" || args[i] == "-dash-crew-reel")
            {
                var review = new GameObject("Dash Review").AddComponent<DashReview>();
                Application.runInBackground = true;
                review.output = args[i + 1];
                review.walkthrough = args[i] == "-dash-walkthrough";
                review.mechanics = args[i] == "-dash-mechanics";
                review.documentary = args[i] == "-dash-documentary";
                review.botcheck = args[i] == "-dash-botcheck";
                review.crewReel = args[i] == "-dash-crew-reel";
                WalkthroughActive = review.walkthrough || review.documentary;
                review.film = args[i] == "-dash-film";
                review.benchmark = args[i] == "-dash-benchmark";
                review.continuous = args[i] == "-dash-playtest" || review.film || review.benchmark;
                review.comprehensive = args[i] == "-dash-comprehensive";
                for (int j = 0; j < args.Length - 1; j++)
                {
                    if (args[j] == "-film-seconds" && int.TryParse(args[j + 1], out int seconds)) review.filmSeconds = Mathf.Clamp(seconds, 1, 300);
                    if (args[j] == "-encoder") review.encoderPath = args[j + 1];
                    if (args[j] == "-district") int.TryParse(args[j + 1], out review.district);
                    if (args[j] == "-skip-menu") review.skipMenu = true;
                    if (args[j] == "-menu-only") review.menuOnly = true;
                    if (args[j] == "-mp-only") review.mpOnly = true;
                    if (args[j] == "-character") int.TryParse(args[j + 1], out review.comprehensiveChar);
                    if (args[j] == "-vehicle") int.TryParse(args[j + 1], out review.comprehensiveVeh);
                    if (args[j] == "-take" && int.TryParse(args[j + 1], out int take)) review.filmTakeSeed = take; // a different course layout per film take
                }
                return;
            }
        }
        bool botcheck, skipMenu, menuOnly, mpOnly;
        bool filmPitted, filmAbility;
        int filmPitStation = -1;
        int filmTakeSeed;
        /// Fast survival check of the showcase Autopilot under endless rules: five runs per district to 1000 m.
        IEnumerator BotCheck()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            BurgerPrices.UnlockAllForReview(); PlayerProfile.Character = 0; PlayerProfile.Burgers = 20;
            yield return new WaitForSecondsRealtime(4);
            var gm = GameManager.Instance; var menu = MenuUI.Instance;
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            var report = new System.Text.StringBuilder();
            int survived = 0, total = 0;
            for (int d = 0; d < 3; d++)
                for (int k = 0; k < 5; k++)
                {
                    DashRun.District = d; menu.Open("none");
                    UnityEngine.Random.InitState(9000 + d * 17 + k * 101);
                    RiderInput.Override = () => Autopilot.Steer(track, rider);
                    gm.StartRun();
                    float until = Time.realtimeSinceStartup + 150;
                    while (gm.State == RunState.Running && gm.Distance < 1000 && Time.realtimeSinceStartup < until)
                    {
                        if (FuelStops.Instance.NearStation && !FuelStops.Instance.Pitting && RidersEconomy.Fuel < .7f) FuelStops.Instance.TryStartPit(out _);
                        yield return null;
                    }
                    total++; bool ok = gm.State == RunState.Running && gm.Distance >= 1000; if (ok) survived++;
                    string line = $"district {d} run {k}: {(ok ? "SURVIVED" : DashRun.EndReason)} at {gm.Distance:0} m";
                    report.AppendLine(line); Debug.Log("[BotCheck] " + line);
                    RiderInput.Override = null; gm.ToMenu(); yield return new WaitForSecondsRealtime(1);
                }
            report.AppendLine($"survived {survived}/{total}");
            File.WriteAllText(Path.Combine(output, "botcheck.txt"), report.ToString());
            Restore(); Application.Quit(0);
        }

        void Check(bool ok, string message)
        {
            assertions++;
            if (ok) { if (!continuous) Debug.Log("[DashReview] PASS " + message); return; }
            // While a showcase film is rolling, QA checks are logged, never fatal: a take is judged afterwards
            // from its markers (ShowcaseReel.ps1). Only the run actually ending still stops it.
            if (film && recording && !message.StartsWith("continuous run survives")) { Debug.LogWarning("[DashFilm] check missed: " + message); return; }
            Debug.LogError("[DashReview] FAIL " + message); Restore(); Application.Quit(1);
            throw new Exception(message);
        }
        void Backup()
        {
            var keys = new List<string> { "character", "total_points", "dash_district", "coins", "ride", "coached_runs", "best", "missions_done", "missions_v", "daily_last", "daily_streak" };
            keys.AddRange(new[] { "dash_fragments", "dash_outfit_tokens", "dash_event_collectibles", "dash_deliveries" });
            keys.AddRange(new[] { "food", "gfx", "muted", "haptics", "reduce_motion" });
            // The burger economy's wallet, migration flag and purchases (BurgerPrices).
            keys.AddRange(new[] { "burgers", "burger_economy_v1", "owned_district_1", "owned_district_2", "owned_crew_bolt" });
            for (int i = 0; i < 3; i++) keys.Add("owned_outfit_" + i);
            for (int i = 0; i < PowerUps.Count; i++) keys.Add("upgrade" + i);
            foreach (string key in new[] { "volume", "music_volume", "sfx_volume" })
                floats[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetFloat(key));
            for (int i = 0; i < 3; i++) { keys.Add("dash_outfit_" + i); keys.Add("dash_outfit_owned_" + i); }
            for (int i = 0; i < 3; i++) foreach (var field in new[] { "stat", "target", "progress" }) keys.Add($"mission{i}_{field}");
            foreach (var key in keys) ints[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetInt(key));
            string rewards = "dash_riders_demo_rewards_v1";
            strings[rewards] = (PlayerPrefs.HasKey(rewards), PlayerPrefs.GetString(rewards));
            strings["lb_runs"] = (PlayerPrefs.HasKey("lb_runs"), PlayerPrefs.GetString("lb_runs"));
        }
        void Restore()
        {
            if (restored) return; restored = true;
            foreach (var pair in ints) { if (pair.Value.exists) PlayerPrefs.SetInt(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            foreach (var pair in strings) { if (pair.Value.exists) PlayerPrefs.SetString(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            foreach (var pair in floats) { if (pair.Value.exists) PlayerPrefs.SetFloat(pair.Key, pair.Value.value); else PlayerPrefs.DeleteKey(pair.Key); }
            WalkthroughActive = WalkingCity = false; WalkthroughInput = Vector2.zero;
            PlayerPrefs.Save(); Time.timeScale = 1f; Time.captureFramerate = 0; recording = false; RiderInput.Override = null;
            RestoreFilmCamera();
            if (filmEncoder != null)
            {
                try { filmEncoder.StandardInput.Close(); if (!filmEncoder.WaitForExit(5000)) filmEncoder.Kill(); }
                catch (Exception) { }
                filmEncoder.Dispose(); filmEncoder = null;
            }
        }
        void OnApplicationQuit() => Restore();
        IEnumerator Shot(string label)
        {
            if (film || benchmark) { yield return null; yield break; }
            yield return new WaitForSecondsRealtime(0.25f);
            // A hidden Windows window may not present a backbuffer. Render explicitly into a texture
            // and attach overlay canvases to that camera so screenshots include the complete UI.
            var cam = Camera.main;
            if (!cam) foreach (var candidate in Camera.allCameras)
                if (candidate.enabled && !candidate.targetTexture && (!cam || candidate.depth > cam.depth)) cam = candidate;
            Check(cam, "active capture camera exists: " + label);
            if (walkthrough) CheckWalkthroughLayout();
            int cameraMask = cam.cullingMask;
            cam.cullingMask |= 1 | (1 << LayerMask.NameToLayer("UI"));
            const int width = 1080, height = 1920;
            var rt = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            var modes = new RenderMode[canvases.Length];
            var cameras = new Camera[canvases.Length];
            for (int i = 0; i < canvases.Length; i++)
            {
                modes[i] = canvases[i].renderMode; cameras[i] = canvases[i].worldCamera;
                if (modes[i] != RenderMode.ScreenSpaceOverlay) continue;
                canvases[i].renderMode = RenderMode.ScreenSpaceCamera;
                canvases[i].worldCamera = cam; canvases[i].planeDistance = 0.5f;
            }
            Canvas.ForceUpdateCanvases();
            foreach (var auxiliary in Camera.allCameras)
                if (auxiliary != cam && auxiliary.enabled && auxiliary.targetTexture)
                    RenderPipeline.SubmitRenderRequest(auxiliary, new RenderPipeline.StandardRequest { destination = auxiliary.targetTexture });
            var request = new RenderPipeline.StandardRequest { destination = rt };
            RenderPipeline.SubmitRenderRequest(cam, request);
            cam.cullingMask = cameraMask;
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
            // Read back asynchronously and encode on a worker: a synchronous ReadPixels + PNG encode froze the
            // visible game for a quarter of a second at every capture.
            var readback = UnityEngine.Rendering.AsyncGPUReadback.Request(rt, 0, TextureFormat.RGB24);
            while (!readback.done) yield return null;
            string file = Path.Combine(output, (++shot).ToString("00") + "-" + label + ".png");
            if (!readback.hasError)
            {
                var data = readback.GetData<byte>().ToArray();
                int light = 0;
                for (int i = 0; i + 2 < data.Length; i += 97 * 3) if (data[i] + data[i + 1] + data[i + 2] > 40) light++;
                Check(light > 100, "screenshot renders visible content: " + label);
                bool flip = SystemInfo.graphicsUVStartsAtTop;
                System.Threading.Tasks.Task.Run(() =>
                {
                    if (flip)
                    {
                        int row = width * 3; var line = new byte[row];
                        for (int y = 0; y < height / 2; y++)
                        {
                            System.Buffer.BlockCopy(data, y * row, line, 0, row);
                            System.Buffer.BlockCopy(data, (height - 1 - y) * row, data, y * row, row);
                            System.Buffer.BlockCopy(line, 0, data, (height - 1 - y) * row, row);
                        }
                    }
                    File.WriteAllBytes(file, ImageConversion.EncodeArrayToPNG(data, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8_UNorm, (uint)width, (uint)height));
                });
            }
            else Check(false, "screenshot readback failed: " + label);
            RenderTexture.ReleaseTemporary(rt);
            yield return null;
        }
        void Place(float distance, TrackSpawner track, RiderController rider)
        {
            typeof(GameManager).GetProperty("Distance").SetValue(GameManager.Instance, distance);
            typeof(TrackSpawner).GetField("previousDistance", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(track, distance);
            foreach (var o in track.Live) if (o && o.kind != TrackKind.Pickup) { o.consumed = true; o.gameObject.SetActive(false); }
            rider.ResetRider();
        }
        IEnumerator Start()
        {
            Directory.CreateDirectory(output); Backup();
            if (crewReel) { yield return CrewReel(); yield break; }
            if (mechanics) { yield return MechanicsWalkthrough(); yield break; }
            if (documentary) { yield return DocumentaryWalkthrough(); yield break; }
            if (botcheck) { yield return BotCheck(); yield break; }
            if (walkthrough) { yield return FullWalkthrough(); yield break; }
            if (continuous) { yield return ContinuousRun(); yield break; }
            if (comprehensive) { yield return ComprehensiveRun(); yield break; }
            PlayerPrefs.SetInt("coached_runs", 2);
            for (int i = 0; i < 3; i++) PlayerPrefs.SetInt("dash_outfit_" + i, 0);
            BurgerPrices.UnlockAllForReview();
            PlayerProfile.Character = 0; PlayerProfile.Ride = 0; DashRun.District = Mathf.Clamp(district, 0, 2);
            yield return new WaitForSeconds(1.5f);
            var menu = MenuUI.Instance; var gm = GameManager.Instance; var run = DashRun.Instance;
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            Check(menu && gm && run && track && rider, "scene dependencies");
            // The city is the default entry view now; release its camera before runner captures.
            typeof(MenuUI).GetMethod("ExitCity", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(menu, null);
            yield return null;
            menu.Open("none"); track.RefreshDistrict();
            yield return Shot("title");
            foreach (string panel in new[] { "crew", "shop", "missions", "districts", "settings" })
            {
                menu.Open(panel); yield return Shot(panel);
                if (panel == "crew") { menu.FlipCrew(1); yield return Shot("nova"); menu.FlipCrew(1); yield return Shot("bolt"); }
                menu.Open("none");
            }
            PlayerProfile.Burgers = BurgerPrices.Coupon * 2 + 7;
            var coupon = DemoRewards.Issue(); var second = DemoRewards.Issue();
            Check(coupon != null && second != null && coupon.code != second.code, "unique coupon issue");
            Check(PlayerProfile.Burgers == 7 && DemoRewards.Issue() == null && PlayerProfile.Burgers == 7, "coupons cost burgers and a short wallet buys nothing");
            menu.Open("rewards"); yield return Shot("rewards-active");
            Check(DemoRewards.Redeem(coupon.code) == "REDEEMED", "dummy order applies once");
            Check(DemoRewards.Redeem(coupon.code) == "USED", "dummy order rejects second use");
            yield return new WaitForSeconds(1.1f); yield return Shot("rewards-used"); menu.Open("none");
            RiderInput.Override = () => RiderInput.Action.None;
            gm.StartRun(); run.TryAbility();
            Check(run.Boost > 0f && run.Battery == 0f, "Ace boost uses battery");
            float boost = run.Boost; run.TryAbility(); Check(run.Boost == boost, "ability cannot spam");
            yield return new WaitForSeconds(1.3f);
            yield return Shot("ace-boost");
            gm.SetPaused(true); float distance = gm.Distance;
            yield return new WaitForSecondsRealtime(0.3f);
            Check(gm.Distance == distance, "pause freezes movement"); gm.SetPaused(false);
            PlayerProfile.Character = 1; FindFirstObjectByType<RiderLook>().Apply(1, true); gm.StartRun(); run.TryAbility();
            Check(run.FloatTime > 0f && run.JumpGravityFactor < 1f, "Nova extended air time");
            yield return Shot("nova-ability");
            PlayerProfile.Character = 2; FindFirstObjectByType<RiderLook>().Apply(2, true); gm.StartRun(); run.TryAbility();
            Check(run.Shield && run.AbsorbHit() && !run.AbsorbHit(), "Bolt shield absorbs exactly one collision");
            yield return Shot("bolt-ability");
            PlayerProfile.Character = 0; FindFirstObjectByType<RiderLook>().Apply(0, true); gm.StartRun();
            // Client rules (3 Nov): on the bike Ace cannot wall-run; on foot he can, on any street facade.
            Place(425, track, rider); rider.BounceBack(0); yield return null;
            Check(!run.TryWallRun(-1) && !run.WallRunning, "Ace on the e-bike cannot wall-run");
            run.KnockOffBike(); yield return null;
            Check(run.OnFoot && !DashRun.CanTakeRooftops, "on foot, the rooftops are closed to Ace");
            Check(run.TryWallRun(-1), "on foot, Ace wall-runs a street facade outside any zone");
            yield return new WaitForSeconds(0.5f);
            Check(run.WallRunning && run.SurfaceHeight > 4f && rider.SurfaceNormal.x > 0.99f, "wall contact on the facade, above the awnings");
            yield return Shot("wall-run");
            yield return new WaitForSeconds(run.WallRunSeconds + 0.6f);
            Check(run.Surface == RunSurface.Road, "wall run ends back in the street");
            PlayerProfile.Character = 2; FindFirstObjectByType<RiderLook>().Apply(2, true); gm.StartRun();
            Place(430, track, rider); rider.BounceBack(2); yield return null;
            Check(DashRun.CanTakeRooftops && DashRun.CanRideWalls && run.TryWallRun(1), "Bolt wall-runs and keeps rooftop access");
            yield return new WaitForSeconds(0.5f);
            Check(run.Surface == RunSurface.RightWall, "Bolt on the right-hand facade");
            yield return Shot("bolt-wall-run");
            PlayerProfile.Character = 0; FindFirstObjectByType<RiderLook>().Apply(0, true); gm.StartRun();
            run.StartSlide(); Check(run.Slide > 0, "slide input");
            yield return Shot("slide");
            gm.StartRun(); int walletBefore = PlayerProfile.Burgers; Place(451, track, rider); yield return null;
            Check(run.Deliveries == 1 && PlayerProfile.Burgers - walletBefore >= BurgerPrices.LateTip && gm.TipsEarned == PlayerProfile.Burgers - walletBefore, "delivery pays a burger tip");
            int paid = PlayerProfile.Burgers; yield return null;
            Check(PlayerProfile.Burgers == paid, "checkpoint cannot pay twice");
            yield return Shot("delivery");
            gm.StartRun();
            Check(run.TryDeliveryDash(), "all crew can activate delivery dash");
            Check(!run.TryDeliveryDash(), "delivery dash cooldown prevents spam");
            yield return new WaitForSeconds(0.25f);
            Check(gm.Speed > Tuning.SpeedBase * 1.45f, "delivery dash actually accelerates forward movement");
            yield return Shot("delivery-dash");
            gm.StartRun();
            int coinsBefore = PlayerProfile.Burgers;
            gm.AddPickup(15); gm.AddPickup(15);
            Check(gm.PickupCount == 2 && gm.BurgersCollected == 2 && PlayerProfile.Burgers == coinsBefore + 2, "each burger pickup goes straight into the wallet");
            gm.Bust();
            Check(PlayerProfile.Burgers == coinsBefore + 2 && gm.BurgersEarned == 2, "run end pays nothing twice: burgers are the only currency");
            gm.StartRun();
            foreach (float checkpoint in new[] { 451f, 901f, 1351f })
            {
                Place(checkpoint, track, rider); yield return null; yield return null;
            }
            Check(run.Deliveries == 3 && gm.State == RunState.Running, "endless runner continues after the third delivery");
            run.PenalizeTime(1000f); yield return null; yield return null;
            Check(gm.State == RunState.Running, "an expired on-time window never ends the endless run");
            Place(1801f, track, rider); yield return null; yield return null;
            Check(run.Deliveries == 4 && gm.State == RunState.Running, "late delivery still counts and the run continues");
            run.End("OUT OF CHARGE"); yield return null;
            Check(gm.State == RunState.Busted && DashRun.EndReason == "OUT OF CHARGE", "run-ending rule opens the results card");
            yield return new WaitForSeconds(1.5f);
            yield return Shot("endless-timeout");
            Check(!FindFirstObjectByType<ChaseController>(), "timed course scene contains no officer chase controller");
            gm.ToMenu(); Restore();
            File.WriteAllText(Path.Combine(output, "review-pass.txt"), $"PASS {assertions} runtime assertions; district {district}; {shot} screenshots\n");
            Debug.Log($"[DashReview] COMPLETE {assertions} assertions");
            yield return new WaitForSecondsRealtime(0.2f); Application.Quit(0);
        }

        IEnumerator ComprehensiveRun()
        {
            // Unlock all content
            PlayerPrefs.SetInt("coached_runs", 6);
            PlayerPrefs.SetInt("dash_missions_done", 6);
            BurgerPrices.UnlockAllForReview();
            PlayerProfile.Burgers = 1000;
            
            // Set specific character/vehicle/district
            PlayerProfile.Character = Mathf.Clamp(comprehensiveChar, 0, 2);
            PlayerProfile.Ride = comprehensiveVeh;
            DashRun.District = Mathf.Clamp(district, 0, 2);
            
            yield return new WaitForSeconds(1.5f);
            
            var menu = MenuUI.Instance;
            var gm = GameManager.Instance;
            var run = DashRun.Instance;
            var rider = FindFirstObjectByType<RiderController>();
            var track = FindFirstObjectByType<TrackSpawner>();
            
            Check(menu && gm && run && track && rider, "scene dependencies");
            
            string[] charNames = { "Ace", "Nova", "Bolt" };
            string[] vehNames = { "None", "Board" };
            string label = $"{charNames[comprehensiveChar]}_{vehNames[comprehensiveVeh]}_District{district}";
            
            menu.Open("none");
            track.RefreshDistrict();
            yield return Shot(label + "_menu");
            
            // Test ability
            RiderInput.Override = () => RiderInput.Action.None;
            gm.StartRun();
            
            if (comprehensiveChar == 0)
            {
                run.TryAbility();
                Check(run.Boost > 0f, "Ace boost");
                yield return new WaitForSeconds(1f);
                yield return Shot(label + "_ability");
            }
            else if (comprehensiveChar == 1)
            {
                run.TryAbility();
                Check(run.FloatTime > 0f, "Nova float");
                yield return new WaitForSeconds(1f);
                yield return Shot(label + "_ability");
            }
            else if (comprehensiveChar == 2)
            {
                run.TryAbility();
                Check(run.Shield, "Bolt shield");
                yield return new WaitForSeconds(1f);
                yield return Shot(label + "_ability");
            }
            
            // Test vehicle
            if (comprehensiveVeh == 1)
            {
                Check(VehicleDynamics.Current.kind == CourierVehicle.Bicycle, "E-bike active");
                yield return new WaitForSeconds(0.5f);
                yield return Shot(label + "_board");
            }
            
            // Test delivery dash
            gm.StartRun();
            run.TryDeliveryDash();
            yield return new WaitForSeconds(0.5f);
            yield return Shot(label + "_dash");
            
            // Short gameplay
            RiderInput.Override = () =>
            {
                float d = gm.Distance;
                if (d < 50f) return RiderInput.Action.Jump;
                if (d < 100f) return RiderInput.Action.Right;
                if (d < 150f) return RiderInput.Action.Left;
                return RiderInput.Action.None;
            };
            
            yield return new WaitForSeconds(3f);
            yield return Shot(label + "_gameplay");
            
            gm.ToMenu();
            Restore();
            
            File.WriteAllText(Path.Combine(output, "comprehensive-pass.txt"), $"PASS: {label}\n");
            Debug.Log($"[DashReview] COMPREHENSIVE COMPLETE: {label}");
            
            yield return new WaitForSecondsRealtime(0.2f); Application.Quit(0);
        }

        /// Endless rules can end a bot run early (a second crash before remounting). A wipe-out is logged and the
        /// whole attempt is ridden again on a fresh seed, at most MaxWipeoutRetries times; nothing is made invulnerable.
        const int MaxWipeoutRetries = 2;
        int continuousAttempt;
        bool WipeoutRetry(GameManager gm, ref int attempt)
        {
            if (gm.State != RunState.Busted || DashRun.EndReason != "WIPED OUT" || attempt >= MaxWipeoutRetries) return false;
            attempt++;
            Debug.Log($"[DashPlaytest] bot wiped out at {gm.Distance:0}; new run, attempt {attempt + 1}");
            File.AppendAllText(Path.Combine(output, "wipeouts.txt"), $"district {DashRun.District}: wiped out at {gm.Distance:0} m; retried\n");
            return true;
        }

        IEnumerator ContinuousRun()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            BurgerPrices.UnlockAllForReview();
            // Showcase films may star any crew member (-character 0..2); the playtest always rides Ace.
            PlayerProfile.Character = film ? Mathf.Clamp(comprehensiveChar, 0, 2) : 0; PlayerProfile.Ride = 0; DashRun.District = Mathf.Clamp(district, 0, 2);
            yield return new WaitForSeconds(1.5f);
            MenuUI.Instance.Open("none");
            var gm = GameManager.Instance; var run = DashRun.Instance;
            var rider = FindFirstObjectByType<RiderController>();
            var track = FindFirstObjectByType<TrackSpawner>();
            int wallChips = 0, ceilingChips = 0, hits = 0;
            bool wallEntered = false, ceilingSeen = false, rightSeen = false, dashed = false;
            bool knockedOff = false, remounted = false, footShot = false;
            bool wallShot = false, ceilingShot = false, climbShot = false, shortcutShot = false, rooftopShot = false, dropped = false;
            float roofPeak = 0f;
            int switches = 0, spikes = 0, phantoms = 0; float worstSpike = 0f;
            var overlapSince = new System.Collections.Generic.Dictionary<TrackObject, int>();
            float lastSwitch = -10f, startingSpeed = 0f;
            Action<TrackObject> collected = o => { if (o.surface == RunSurface.Ceiling) ceilingChips++; else if (o.surface != RunSurface.Road) wallChips++; };
            Action<TrackObject> struck = o =>
            {
                hits++;
                Debug.Log($"[DashPlaytest] Hit {o.name} ({o.kind}/{o.surface}) lane={o.lane} at={o.transform.position} rider={rider.transform.position} distance={gm.Distance:0.0}");
            };
            TrackSpawner.Collected += collected; TrackSpawner.Struck += struck;
            // This run uses the normal input path and actual course. No teleports, disabled hurdles,
            // invulnerability or direct movement/state setters are used.
            RiderInput.Override = () =>
            {
                float d = gm.Distance;
                float sectionDistance = film ? Mathf.Repeat(d, 960f) : d;
                int cycle = Mathf.FloorToInt(d / 960f);
                if (film && cycle != filmCycle) { filmCycle = cycle; wallEntered = false; switches = 0; lastSwitch = -10f; }
                if (d > 100f && !dashed) { startingSpeed = gm.Speed; dashed = run.TryDeliveryDash(); }
                // Wall runs are on foot only: the harness simulates the crash that puts Ace on foot before the
                // facade section, then rides it through normal input. (Logged; no other state is set.)
                if (sectionDistance > 392f && sectionDistance < 405f && !run.OnFoot && rider.Traversal.Surface == RunSurface.Road && !rider.Airborne)
                { run.KnockOffBike(); if (run.OnFoot) { knockedOff = true; Debug.Log("[DashPlaytest] simulated crash at " + d.ToString("0")); } }
                if (sectionDistance > 405f && sectionDistance < 423f && rider.Lane > 0 && rider.Traversal.Surface == RunSurface.Road) return RiderInput.Action.Left;
                // Facade entry is retried every frame instead of being latched on a single attempt: one
                // dropped frame (or an attempt made while airborne) used to leave the rider on the road
                // for the whole wall section and fail the run.
                bool onWall = rider.Traversal.Surface is RunSurface.LeftWall or RunSurface.RightWall;
                if (onWall) wallEntered = true;
                if (!onWall && (run.OnFoot || PlayerProfile.Character == 2) && sectionDistance >= 420f && sectionDistance < 470f && !rider.Airborne)
                {
                    // From the left lane, steering on into the building starts the wall run (no zone needed).
                    if (Time.frameCount % 20 == 0)
                        Debug.Log($"[DashPlaytest] facade entry attempt at {d:0.0}: lane={rider.Lane} foot={run.OnFoot} air={rider.Airborne} surface={rider.Traversal.Surface}");
                    return RiderInput.Action.Left;
                }
                if (onWall) return RiderInput.Action.None;
                if (rider.Traversal.Moving) return RiderInput.Action.None;
                // Take the rooftop shortcut: hold the ramp lane into the kicker, then jump the alley gaps.
                float roofStart = RooftopCourse.ZoneStartAt(d);
                if (rider.Traversal.Surface == RunSurface.Road && d > roofStart - 50f && d < roofStart + 1.5f && d > RooftopCourse.Start - 50f)
                    return rider.Lane < RooftopCourse.EntryLane ? RiderInput.Action.Right : rider.Lane > RooftopCourse.EntryLane ? RiderInput.Action.Left : RiderInput.Action.None;
                if (rider.Traversal.Surface == RunSurface.Rooftop && !rider.Airborne)
                {
                    float edge = RooftopCourse.NextEdge(d, Tuning.LaneX[rider.Lane]);
                    if (edge > -0.5f && edge < gm.Speed * 0.12f) return RiderInput.Action.Jump;
                }
                if (district==1 && rider.Traversal.Surface == RunSurface.Road && !rider.Airborne)
                    foreach(var gap in track.Live)
                        if(gap && gap.kind==TrackKind.Gap && gap.lane==rider.Lane && gap.transform.position.z-gap.halfLength-d>-.5f && gap.transform.position.z-gap.halfLength-d<gm.Speed*.25f)
                            return RiderInput.Action.Jump;
                // On foot, head for the parked e-bike's lane once the facade section is behind.
                if (run.OnFoot && run.BikeAt > d && run.BikeAt - d < 60f && rider.Traversal.Surface == RunSurface.Road)
                    return rider.Lane < run.BikeLane ? RiderInput.Action.Right : rider.Lane > run.BikeLane ? RiderInput.Action.Left : RiderInput.Action.None;
                // Low hurdles (roof vents, cones, barriers under 1 m) are hopped on time, as a player would;
                // without this the Skypark vents beat the harness once it is on foot.
                if (!rider.Airborne && !rider.Traversal.Moving)
                    foreach (var o in track.Live)
                    {
                        if (!o || o.consumed || o.kind != TrackKind.Low || o.height > 1.0f || o.lane != rider.Lane || o.surface != rider.Traversal.Surface) continue;
                        float eta = (o.transform.position.z - o.halfLength - d) / Mathf.Max(1f, gm.Speed);
                        if (eta > 0f && eta < 0.22f) return RiderInput.Action.Jump;
                    }
                // Prefer a clear nearby lane; only react to hazards on the current surface.
                float[] danger = new float[4];
                foreach (var o in track.Live)
                {
                    if (!o || o.consumed || o.kind == TrackKind.Pickup || o.kind == TrackKind.Platform || o.surface != rider.Traversal.Surface) continue;
                    float ahead = o.transform.position.z - d - o.halfLength;
                    if (ahead < -1f || ahead > gm.Speed * 1.0f) continue;
                    danger[o.lane] += 100f / Mathf.Max(1f, ahead + 2f);
                }
                int best = rider.Lane;
                for (int i = 0; i < 3; i++) if (danger[i] + Mathf.Abs(i - rider.Lane) * 0.1f < danger[best]) best = i;
                return best < rider.Lane ? RiderInput.Action.Left : best > rider.Lane ? RiderInput.Action.Right : RiderInput.Action.None;
            };
            UnityEngine.Random.InitState(7421 + district + continuousAttempt * 101 + filmTakeSeed * 7919);
            gm.StartRun();
            if (film) { PrepareFilmCamera(); FindFirstObjectByType<FollowCamera>().MovieFraming=true; SaveFilmMusic(); StartFilmEncoder(); MenuUI.Instance.Caption(DashRun.CrewNames[PlayerProfile.Character] + " / " + DashRun.DistrictNames[district], "DISTRICT RUN", 2.5f); Time.captureFramerate = 30; Application.targetFrameRate = 30; recording = true; }
            float deadline = Time.realtimeSinceStartup + (film ? 1800f : 120f);
            bool retryAttempt = false;
            try
            {
                while ((film ? filmFrame < filmSeconds * 30 : gm.Distance < 820f) && Time.realtimeSinceStartup < deadline)
                {
                    if (film && WipeoutRetry(gm, ref continuousAttempt)) { Debug.LogError("[DashFilm] FAIL wipe-out during a film take"); Restore(); Application.Quit(3); yield break; }
                    if (WipeoutRetry(gm, ref continuousAttempt)) { retryAttempt = true; break; }
                    // Films charge at the first kiosk (to show it), then at any kiosk once the battery is under 60%.
                    if (film && FuelStops.Instance && FuelStops.Instance.NearStation && !FuelStops.Instance.Pitting && FuelStops.Instance.StationIndex != filmPitStation
                        && RidersEconomy.Fuel < (filmPitted ? .6f : .95f) && gm.State == RunState.Running && !gm.Paused)
                    { bool started = FuelStops.Instance.TryStartPit(out _); if (started) { filmPitted = true; filmPitStation = FuelStops.Instance.StationIndex; } if (started) filmEvents.Add($"{filmFrame / 30f:F3},{gm.Distance:F1},KIOSK CHARGE"); }
                    if (film && !filmAbility && gm.Distance > 250f && run.Battery >= .999f && gm.State == RunState.Running && !gm.Paused)
                    { run.TryAbility(); filmAbility = true; filmEvents.Add($"{filmFrame / 30f:F3},{gm.Distance:F1},ABILITY {DashRun.AbilityNames[PlayerProfile.Character]}"); }
                    Check(gm.State == RunState.Running, "continuous run survives at " + gm.Distance.ToString("0"));
                    if (knockedOff && run.OnFoot && !footShot && !rider.Traversal.Moving && rider.Traversal.Surface == RunSurface.Road && gm.Distance > 400f)
                    { footShot = true; Check(VehicleDynamics.Current.kind == CourierVehicle.Running, "knocked-off rider runs on foot"); yield return Shot("continuous-on-foot"); }
                    if (knockedOff && !run.OnFoot) remounted = true;
                    if (rider.Traversal.Surface == RunSurface.Rooftop) roofPeak = Mathf.Max(roofPeak, rider.transform.position.y);
                    if (rider.Traversal.Surface == RunSurface.Rooftop && rider.Traversal.Moving && rider.Traversal.Progress > 0.3f && rider.Traversal.Progress < 0.6f && !climbShot)
                    {
                        climbShot = true;
                        Check(rider.transform.position.y > 2f && rider.SurfaceNormal.y > 0.9f, "rooftop parkour hops upright over real objects");
                        yield return Shot("continuous-rooftop-parkour");
                    }
                    if (run.OnRooftops && !rider.Traversal.Moving && gm.Distance > RooftopCourse.Start + 40f && !shortcutShot)
                    {
                        shortcutShot = true;
                        Check(rider.transform.position.x > SurfaceTraversal.FacadeX + 1f && rider.transform.position.y > SurfaceTraversal.RoofHeight - 0.5f, "shortcut runs on the building roofs, not a lane");
                        Check(gm.Speed > Mathf.Min(Tuning.SpeedMax, Tuning.SpeedBase + gm.Distance * Tuning.SpeedRamp) * VehicleDynamics.Current.speed * 1.2f, "rooftop shortcut reduces real travel time");
                        yield return Shot("continuous-rooftops");
                    }
                    if (run.OnRooftops && rider.Airborne && !rooftopShot && gm.Distance > RooftopCourse.Start + 40f)
                    {
                        rooftopShot = true;
                        yield return Shot("continuous-rooftop-gap-jump");
                    }
                    if (shortcutShot && rider.Traversal.Surface == RunSurface.Road && !dropped && gm.Distance < RooftopCourse.Start + RooftopCourse.Length + 40f) dropped = true;
                    // Frame spikes and pass-throughs are logged with their place in the run (the visible test's
                    // stutter and "went through a hurdle" reports).
                    if (!film && gm.RunTime > 1.5f && Time.unscaledDeltaTime > 0.045f)
                    { spikes++; worstSpike = Mathf.Max(worstSpike, Time.unscaledDeltaTime); Debug.Log($"[Spike] {Time.unscaledDeltaTime * 1000f:0} ms at {gm.Distance:0} m surface={rider.Traversal.Surface} onFoot={run.OnFoot}"); }
                    if (!rider.Traversal.Moving)
                        foreach (var o in track.Live)
                        {
                            if (!o || !o.gameObject.activeInHierarchy || o.consumed || o.struck || o.kind is TrackKind.Pickup or TrackKind.Gap or TrackKind.Platform || o.surface != rider.Traversal.Surface) continue;
                            if (Mathf.Abs(o.transform.position.z - gm.Distance) > o.halfLength) continue;
                            var local = Quaternion.Inverse(SurfaceTraversal.Orientation(o.surface)) * (rider.transform.position - SurfaceTraversal.Point(o.surface, o.lateral ? 0f : Tuning.LaneX[o.lane], 0f));
                            if (o.lateral) local.x -= o.transform.position.x;
                            if (o.surface == RunSurface.Road) local.y -= o.baseHeight;
                            bool overlapsX = Mathf.Abs(local.x) < o.halfWidth * 0.8f;
                            bool overlapsY = o.kind == TrackKind.Overhead ? local.y + VehicleDynamics.BodyHeight > o.clearance + 0.1f && !(o.GetComponent<DashRhythmGate>() is DashRhythmGate g && g.Clear) : local.y < o.height - 0.1f;
                            // Confirmed only if no hit lands within two frames (the hit test may run before the
                            // rider moves in a frame).
                            if (overlapsX && overlapsY && !o.passed)
                            {
                                if (!overlapSince.TryGetValue(o, out int since)) overlapSince[o] = Time.frameCount;
                                else if (Time.frameCount - since >= 2) { o.passed = true; phantoms++; Debug.Log($"[Phantom] passed through {o.name} ({o.kind}) lane={o.lane} at {gm.Distance:0} m local=({local.x:0.00},{local.y:0.00})"); }
                            }
                        }
                    if (dashed && run.DashTime > 1.9f && run.DashTime < 2.1f) Check(gm.Speed >= startingSpeed * 1.4f, "dash speed affects real travel before subsequent collision recovery");
                    if (!rider.Traversal.Moving && rider.Traversal.Surface != RunSurface.Road)
                    {
                        // Height off the surface plane must equal the jump height (lateral position may be mid-steer).
                        var anchor = SurfaceTraversal.Anchor(rider.Traversal.Surface);
                        var offset = rider.transform.position - anchor; offset.z = 0f;
                        Check(Mathf.Abs(Vector3.Dot(offset, rider.SurfaceNormal) - rider.JumpHeight) < 0.12f, "rider contacts actual route");
                        var viewport = Camera.main.WorldToViewportPoint(rider.transform.position + rider.SurfaceNormal);
                        if (!film) Check(viewport.z > 0 && viewport.x > 0.12f && viewport.x < 0.88f && viewport.y > 0.08f && viewport.y < 0.92f, $"rider remains inside camera frame with body margin (viewport {viewport.x:0.00},{viewport.y:0.00},{viewport.z:0.0}; surface {rider.Traversal.Surface}; rider {rider.transform.position}; camera {Camera.main.transform.position} at {gm.Distance:0.0} m)");
                        if (run.Flipped) ceilingSeen = true;
                        if (run.Surface == RunSurface.RightWall) rightSeen = true;
                        if (run.WallRunning && !wallShot) { wallShot = true; yield return Shot("continuous-wall"); }
                        if (run.Flipped && !ceilingShot) { ceilingShot = true; yield return Shot("continuous-ceiling"); }
                    }
                    if (film)
                    {
                        int routeKind = run.OnRooftops ? 2 : 0;
                        if (routeKind != previousRouteKind && routeKind > 0)
                            filmEvents.Add($"{filmFrame / 30f:F3},{gm.Distance:F1},ROOFTOP SHORTCUT");
                        previousRouteKind = routeKind;
                        if (!rider.Traversal.Moving && run.Surface != previousFilmSurface)
                        {
                            filmEvents.Add($"{filmFrame / 30f:F3},{gm.Distance:F1},{run.Surface}");
                            previousFilmSurface = run.Surface;
                        }
                    }
                    yield return null;
                }
                // Films finalise their video whatever the coverage; ShowcaseReel.ps1 judges the take from its markers.
                if (!retryAttempt) {
                if (!film) Check(gm.Distance >= 820f, "travels 820m continuously without teleporting");
                if (!film) Check(dashed && (film ? run.Deliveries >= 1 : run.Deliveries == 1), "delivery dash and delivery checkpoint completed");
                if (!film) Check(film || wallEntered && wallShot, "on foot, input runs the building facade");
                if (!film) Check(film || run.WallRuns >= 1, "wall run started by steering into a facade");
                if (!film && PlayerProfile.Character != 2) Check(knockedOff && footShot && remounted, "crash puts the rider on foot, then back on the e-bike by grabbing it");
                if (!film) Check(phantoms == 0, "no hurdle is passed through without a hit: " + phantoms);
                if (!film) Check(run.Surface == RunSurface.Road && !rider.Traversal.Moving, "exits tunnel onto road continuously");
                if (!film) Check(run.Shortcuts >= 1, "rooftop shortcut taken and counted");
                if (!film) Check(climbShot && shortcutShot, "facade climb and rooftop run captured through player input");
                if (!film) Check(roofPeak > SurfaceTraversal.RoofHeight - 0.5f, "rider reached the actual building roofs: peak " + roofPeak.ToString("0.0"));
                if (!film) Check(dropped, "rider dropped from the roofs back into the street");
                if (!film) Check(track.PooledReused > 0, "streamed items actually reused from Unity object pools");
                File.WriteAllText(Path.Combine(output, "continuous-pass.txt"), $"PASS district={district}; distance={gm.Distance:0.0}; wallChips={wallChips}; ceilingChips={ceilingChips}; hits={hits}; assertions={assertions}; pooledCreated={track.PooledCreated}; pooledReused={track.PooledReused}; spikes45ms={spikes}; worstFrameMs={worstSpike * 1000f:0}\nNo teleports, obstacle disabling or invulnerability.\n");
                Debug.Log("[DashReview] CONTINUOUS COMPLETE");
                }
            }
            finally { TrackSpawner.Collected -= collected; TrackSpawner.Struck -= struck; RiderInput.Override = null; }
            if (retryAttempt)
            {
                yield return new WaitForSecondsRealtime(1.5f);
                gm.ToMenu();
                yield return ContinuousRun();
                yield break;
            }
            continuousAttempt = 0;
            if (!walkthrough) recording = false;
            if (film)
            {
                Check(filmFrame == filmSeconds * 30, "film contains the exact requested frame count");
                FinishFilmEncoder();
                File.WriteAllText(Path.Combine(output, "recording-pass.txt"), $"{DashRun.CrewNames[PlayerProfile.Character]}; district={district}; kiosk={filmPitted}; ability={filmAbility}; wallRuns={run.WallRuns}; deliveries={run.Deliveries}; frames={filmFrame}; fps=30; duration={filmSeconds}s; distance={gm.Distance:F1}; shortcuts={run.Shortcuts}\n");
                File.WriteAllLines(Path.Combine(output, "route-events.csv"), new[] { "seconds,distance,route" });
                File.AppendAllLines(Path.Combine(output, "route-events.csv"), filmEvents);
            }
            if (film) { yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(0.5f); }
            gm.ToMenu();
            if (!walkthrough) { Restore(); Application.Quit(0); }
        }
        void LateUpdate()
        {
            if (!recording || !filmCamera || filmFrame >= filmSeconds * CaptureRate) return;
            if (walkthrough)
            {
                Application.targetFrameRate = 30;
                if (++walkthroughRenderTick % 2 != 0) return;
            }
            if (walkthrough)
            {
                var activeCamera = ActiveFilmCamera();
                if (activeCamera && activeCamera != filmCamera)
                {
                    filmCamera = activeCamera;
                    foreach (var canvas in filmCanvases) if (canvas && canvas.renderMode == RenderMode.ScreenSpaceCamera) canvas.worldCamera = filmCamera;
                }
            }
            var target = filmCamera.targetTexture;
            var active = RenderTexture.active;
            filmCamera.targetTexture = filmTarget;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(filmCamera,new RenderPipeline.StandardRequest { destination=filmTarget });
            RenderTexture.active=filmTarget;
            filmPixels.ReadPixels(new Rect(0,0,720,1280),0,0,false);
            if (filmEncoder != null)
            {
                filmPixels.GetRawTextureData<byte>().CopyTo(filmBytes);
                filmEncoder.StandardInput.BaseStream.Write(filmBytes, 0, filmBytes.Length);
            }
            else File.WriteAllBytes(Path.Combine(output,$"frame-{filmFrame:D6}.png"),filmPixels.EncodeToPNG());
            filmFrame++;
            if (filmFrame % 300 == 0) Debug.Log($"[DashFilm] district={district}; frames={filmFrame}/{filmSeconds * CaptureRate}; seconds={filmFrame / CaptureRate}; distance={GameManager.Instance.Distance:F1}");
            filmCamera.targetTexture=target; RenderTexture.active=active;
        }
        void StartFilmEncoder()
        {
            if (string.IsNullOrEmpty(encoderPath)) return;
            filmBytes = new byte[720 * 1280 * 3];
            string music = Path.Combine(output, "original-game-music.wav"), movie = Path.Combine(output, "showcase.mp4");
            var start = new System.Diagnostics.ProcessStartInfo {
                FileName = encoderPath,
                Arguments = "-hide_banner -loglevel error -y -f rawvideo -pixel_format rgb24 -video_size 720x1280 -framerate " + CaptureRate + " -i pipe:0 -stream_loop -1 -i \"" + music + "\" -map 0:v:0 -map 1:a:0 -vf vflip -c:v libx264 -threads 2 -preset veryfast -crf 19 -pix_fmt yuv420p -c:a aac -b:a 160k -t " + filmSeconds + " -shortest -movflags +frag_keyframe+empty_moov+default_base_moof \"" + movie + "\"",
                UseShellExecute = false, CreateNoWindow = true, WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                RedirectStandardInput = true, RedirectStandardError = true
            };
            filmEncoder = new System.Diagnostics.Process { StartInfo = start };
            filmEncoder.ErrorDataReceived += (_, e) => { if (e.Data != null) lock (encoderErrors) encoderErrors.AppendLine(e.Data); };
            Check(filmEncoder.Start(), "video encoder starts"); filmEncoder.BeginErrorReadLine();
        }
        void FinishFilmEncoder()
        {
            if (filmEncoder == null) return;
            filmEncoder.StandardInput.Close();
            if (!filmEncoder.WaitForExit(60000)) { filmEncoder.Kill(); throw new Exception("Video encoder did not finish"); }
            filmEncoder.WaitForExit();
            Check(filmEncoder.ExitCode == 0, "video encoder completes: " + encoderErrors);
            filmEncoder.Dispose(); filmEncoder = null; filmBytes = null;
        }
        void PrepareFilmCamera()
        {
            filmCamera=ActiveFilmCamera();filmAspect=filmCamera.aspect;filmCamera.aspect=9f/16f;
            filmTarget=new RenderTexture(720,1280,24,RenderTextureFormat.ARGB32);filmTarget.Create();
            filmPixels=new Texture2D(720,1280,TextureFormat.RGB24,false);
            filmCanvases=FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            filmModes=new RenderMode[filmCanvases.Length];filmCameras=new Camera[filmCanvases.Length];
            for(int i=0;i<filmCanvases.Length;i++)
            {
                filmModes[i]=filmCanvases[i].renderMode;filmCameras[i]=filmCanvases[i].worldCamera;
                if(filmModes[i]!=RenderMode.ScreenSpaceOverlay)continue;
                filmCanvases[i].renderMode=RenderMode.ScreenSpaceCamera;filmCanvases[i].worldCamera=filmCamera;filmCanvases[i].planeDistance=.5f;
            }
        }
        static Camera ActiveFilmCamera()
        {
            Camera selected = null;
            foreach (var camera in Camera.allCameras)
                if (camera.enabled && !camera.targetTexture && (!selected || camera.depth > selected.depth)) selected = camera;
            return selected;
        }
        void RestoreFilmCamera()
        {
            if(filmCanvases!=null)for(int i=0;i<filmCanvases.Length;i++)if(filmCanvases[i])
            {filmCanvases[i].renderMode=filmModes[i];filmCanvases[i].worldCamera=filmCameras[i];}
            if(filmCamera)filmCamera.aspect=filmAspect;
            if(filmTarget){filmTarget.Release();Destroy(filmTarget);filmTarget=null;}
            if(filmPixels){Destroy(filmPixels);filmPixels=null;}
        }
        void SaveFilmMusic()
        {
            var clip = Synth.Music(DashRun.District);
            var samples = new float[clip.samples * clip.channels]; clip.GetData(samples, 0);
            using var writer = new BinaryWriter(File.Create(Path.Combine(output, "original-game-music.wav")));
            void Tag(string s) => writer.Write(System.Text.Encoding.ASCII.GetBytes(s));
            int bytes = samples.Length * 2;
            Tag("RIFF"); writer.Write(36 + bytes); Tag("WAVE"); Tag("fmt "); writer.Write(16);
            writer.Write((short)1); writer.Write((short)clip.channels); writer.Write(clip.frequency);
            writer.Write(clip.frequency * clip.channels * 2); writer.Write((short)(clip.channels * 2)); writer.Write((short)16);
            Tag("data"); writer.Write(bytes);
            foreach (float sample in samples) writer.Write((short)(Mathf.Clamp(sample, -1f, 1f) * short.MaxValue));
            Destroy(clip);
        }
    }
}

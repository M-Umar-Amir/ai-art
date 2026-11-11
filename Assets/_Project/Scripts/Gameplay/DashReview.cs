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
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dash-review" || args[i] == "-dash-playtest" || args[i] == "-dash-film" || args[i] == "-dash-benchmark" || args[i] == "-dash-walkthrough" || args[i] == "-dash-mechanics" || args[i] == "-dash-documentary" || args[i] == "-dash-botcheck" || args[i] == "-dash-crew-reel" || args[i] == "-dash-showcase")
            {
                var review = new GameObject("Dash Review").AddComponent<DashReview>();
                Application.runInBackground = true;
                review.output = args[i + 1];
                review.walkthrough = args[i] == "-dash-walkthrough";
                review.mechanics = args[i] == "-dash-mechanics";
                review.documentary = args[i] == "-dash-documentary";
                review.botcheck = args[i] == "-dash-botcheck";
                review.crewReel = args[i] == "-dash-crew-reel";
                review.showcase = args[i] == "-dash-showcase";
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
            DashRun.AutoDeclineRevive = true;
            for (int d = 0; d < 1; d++)
                for (int k = 0; k < 5; k++)
                {
                    menu.Open("none");
                    UnityEngine.Random.InitState(9000 + d * 17 + k * 101);
                    RiderInput.Override = () => Autopilot.Steer(track, rider);
                    gm.StartRun();
                    float until = Time.realtimeSinceStartup + 150;
                    while (gm.State == RunState.Running && gm.Distance < 1000 && Time.realtimeSinceStartup < until)
                    {
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
            keys.AddRange(new[] { "burgers", "burger_economy_v1", "burger_economy_v2", "coupon_progress", "owned_district_1", "owned_district_2", "owned_crew_bolt" });
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
                // D3D readback is already in the bottom-up row order EncodeArrayToPNG expects; flipping it there
                // saved every review capture upside down (found 11 Nov 2026). Only GL-style targets need the flip.
                bool flip = !SystemInfo.graphicsUVStartsAtTop;
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
            if (showcase) { yield return Showcase(); yield break; }
            if (mechanics || documentary || walkthrough) { Debug.LogError("[DashReview] FAIL kiosk walkthroughs were retired on 4 Nov 2026"); Application.Quit(1); yield break; }
            if (botcheck) { yield return BotCheck(); yield break; }
            if (continuous) { yield return ContinuousRun(); yield break; }
            if (comprehensive) { yield return ComprehensiveRun(); yield break; }
            PlayerPrefs.SetInt("coached_runs", 2);
            PlayerPrefs.SetInt("dash_outfit_0", 0);
            BurgerPrices.UnlockAllForReview();
            yield return new WaitForSeconds(1.5f);
            var menu = MenuUI.Instance; var gm = GameManager.Instance; var run = DashRun.Instance;
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            var chase = FindFirstObjectByType<ChaseController>();
            Check(menu && gm && run && track && rider, "scene dependencies");
            Check(chase && chase.officer, "the police officer is in the scene");
            Check(DashRun.CrewNames.Length == 1 && DashRun.CrewNames[0] == "ACE", "Ace is the only rider");
            typeof(MenuUI).GetMethod("ExitCity", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(menu, null);
            yield return null;
            menu.Open("none"); track.RefreshDistrict();
            yield return Shot("title");
            foreach (string panel in new[] { "crew", "shop", "missions", "rewards", "settings" })
            { menu.Open(panel); yield return Shot(panel); menu.Open("none"); }

            RiderInput.Override = () => RiderInput.Action.None;
            // ---- Ability and pause.
            gm.StartRun(); run.TryAbility();
            Check(run.Boost > 0f && run.Battery == 0f, "Ace boost uses battery");
            float boost = run.Boost; run.TryAbility(); Check(run.Boost == boost, "ability cannot spam");
            yield return new WaitForSeconds(1.3f);
            yield return Shot("ace-boost");
            gm.SetPaused(true); float distance = gm.Distance;
            yield return new WaitForSecondsRealtime(0.3f);
            Check(gm.Distance == distance, "pause freezes movement"); gm.SetPaused(false);
            Check(DashRun.District == 0, "runs start in Downtown");

            // ---- The police chase: on the bike the gap opens, on foot it closes, a crash costs ground.
            gm.StartRun(); yield return null;
            Check(Mathf.Abs(run.PoliceGap - DashRun.StartGap) < 0.8f, "chase starts with the officer behind: " + run.PoliceGap.ToString("0.00"));
            yield return new WaitForSeconds(1f);
            Check(run.PoliceGap > DashRun.StartGap, "riding the e-bike pulls away from the officer");
            Check(!chase.officer.gameObject.activeInHierarchy, "far back, the officer stays out of the frame");
            run.PoliceGains(run.PoliceGap - 7f); yield return new WaitForSeconds(0.4f);
            Check(chase.officer.gameObject.activeInHierarchy, "closing in, the officer rides into view");
            var view = Camera.main.WorldToViewportPoint(chase.officer.position + Vector3.up);
            Check(view.z > 0f && view.y > -0.05f && view.y < 0.6f, $"the officer is on screen behind Ace (viewport {view.x:0.00},{view.y:0.00})");
            yield return Shot("police-chase");
            gm.StartRun(); yield return null;
            Place(430, track, rider); rider.BounceBack(0); track.ClearAhead(430f, 80f); yield return null;
            float before = run.PoliceGap;
            run.KnockOffBike(); run.PoliceGains(DashRun.CrashOnBike); yield return null;
            Check(run.OnFoot && run.PoliceGap < before, "a crash knocks Ace off and lets the officer gain");
            Check(!run.TryWallRun(-1) && !run.WallRunning, "wall running is removed: steering into a facade does nothing");
            float footGap = run.PoliceGap;
            yield return new WaitForSeconds(1f);
            Check(run.PoliceGap < footGap - 1f, "on foot the officer closes in");
            yield return Shot("on-foot-cop-closing");

            // ---- Busted, and the burger revive. A fresh run: on a slow PC the on-foot capture above can take long
            // enough for the officer to have caught Ace already (auto-decline is still on there).
            gm.StartRun(); yield return null;
            DashRun.AutoDeclineRevive = false;
            PlayerProfile.Burgers = BurgerPrices.Revive(0) + 500;
            run.PoliceGains(1000f); yield return null;
            Check(run.AwaitingRevive && gm.Paused && gm.State == RunState.Running && DashRun.EndReason == "BUSTED!", $"caught: the revive offer holds the run (awaiting={run.AwaitingRevive} paused={gm.Paused} state={gm.State} reason={DashRun.EndReason} gap={run.PoliceGap:0.0} grace={run.ReviveGrace:0.0})");
            yield return new WaitForSecondsRealtime(0.4f);
            yield return Shot("revive-offer");
            int wallet = PlayerProfile.Burgers;
            Check(run.Revive(), "revive pays in burgers");
            Check(PlayerProfile.Burgers == wallet - BurgerPrices.Revive(0) && !gm.Paused && !run.OnFoot && Mathf.Abs(run.PoliceGap - DashRun.MaxGap) < 0.2f, "revive: burgers spent, back on the bike, officer pushed back");
            Check(run.ReviveCost == BurgerPrices.Revive(1) && BurgerPrices.Revive(1) > BurgerPrices.Revive(0), "each revive in a run costs more");
            yield return new WaitForSeconds(0.5f);
            yield return Shot("revived");
            PlayerProfile.Burgers = 10;
            run.PoliceGains(1000f); yield return null;
            Check(!run.AwaitingRevive && gm.State == RunState.Running, "just after a revive the officer cannot catch Ace (grace)");
            yield return new WaitForSeconds(2.7f);
            run.PoliceGains(1000f); yield return null; yield return null;
            Check(run.AwaitingRevive, "the offer shows even when the wallet is short");
            yield return Shot("revive-short");
            run.FinishRun(); yield return null;
            Check(gm.State == RunState.Busted && DashRun.EndReason == "BUSTED!", "declining the revive ends the run as BUSTED");
            yield return new WaitForSeconds(1.6f);
            yield return Shot("busted-card");
            DashRun.AutoDeclineRevive = true;

            // ---- The coupon: 100,000 burgers put a 10% coupon on the track; missing it resets the count.
            gm.StartRun(); yield return null;
            int coupons = DemoRewards.Ledger.coupons.Count;
            PlayerProfile.CouponProgress = BurgerPrices.CouponThreshold - BurgerPrices.PickupValue;
            gm.AddPickup(15); yield return null; yield return null;
            Check(track.Coupon, "100,000 collected burgers put a coupon on the track");
            Check(gm.BurgersCollected == 1 && gm.BurgersEarned == BurgerPrices.PickupValue, "each burger pickup is worth " + BurgerPrices.PickupValue);
            float couponZ = track.Coupon.transform.position.z; int couponLane = track.Coupon.lane;
            Place(couponZ - 20f, track, rider); rider.BounceBack(couponLane); yield return new WaitForSeconds(0.15f);
            yield return Shot("coupon-ahead");
            for (float guard = Time.realtimeSinceStartup + 5f; DemoRewards.Ledger.coupons.Count == coupons && Time.realtimeSinceStartup < guard; ) yield return null;
            Check(DemoRewards.Ledger.coupons.Count == coupons + 1 && PlayerProfile.CouponProgress < BurgerPrices.CouponThreshold, "riding into the coupon issues a unique 10% coupon");
            yield return Shot("coupon-won");
            var won = DemoRewards.Ledger.coupons[0];
            Check(DemoRewards.Redeem(won.code) == "REDEEMED" && DemoRewards.Redeem(won.code) == "USED", "the won coupon redeems once");
            PlayerProfile.CouponProgress = BurgerPrices.CouponThreshold - BurgerPrices.PickupValue;
            gm.AddPickup(15); yield return null; yield return null;
            Check(track.Coupon, "a second coupon appears at the next 100,000");
            // Dodge the coupon.
            int dodge = 0;
            for (int l = 0; l < Tuning.Lanes; l++)
                if (l != track.Coupon.lane) dodge = l;
            Place(track.Coupon.transform.position.z - 20f, track, rider); rider.BounceBack(dodge);
            track.ClearAhead(gm.Distance, 40f); yield return null;
            RiderInput.Override = () => RiderInput.Action.None;
            for (float guard = Time.realtimeSinceStartup + 15f; track.Coupon && Time.realtimeSinceStartup < guard; ) yield return null;
            Check(PlayerProfile.CouponProgress < 2000 && DemoRewards.Ledger.coupons.Count == coupons + 1, "a missed coupon is lost and the count starts again from zero: " + PlayerProfile.CouponProgress);
            yield return Shot("coupon-missed");
            gm.Bust();
            gm.ToMenu(); Restore();
            File.WriteAllText(Path.Combine(output, "review-pass.txt"), $"PASS {assertions} runtime assertions; {shot} screenshots\n");
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
            if (gm.State != RunState.Busted || (DashRun.EndReason != "WIPED OUT" && DashRun.EndReason != "BUSTED!") || attempt >= MaxWipeoutRetries) return false;
            attempt++;
            Debug.Log($"[DashPlaytest] bot wiped out at {gm.Distance:0}; new run, attempt {attempt + 1}");
            File.AppendAllText(Path.Combine(output, "wipeouts.txt"), $"district {DashRun.District}: wiped out at {gm.Distance:0} m; retried\n");
            return true;
        }

        IEnumerator ContinuousRun()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            BurgerPrices.UnlockAllForReview();
            DashRun.AutoDeclineRevive = true;
            yield return new WaitForSeconds(1.5f);
            MenuUI.Instance.Open("none");
            var gm = GameManager.Instance; var run = DashRun.Instance;
            var rider = FindFirstObjectByType<RiderController>();
            var track = FindFirstObjectByType<TrackSpawner>();
            var chase = FindFirstObjectByType<ChaseController>();
            int wallChips = 0, hits = 0, pickups = 0;
            bool wallEntered = false, dashed = false;
            bool knockedOff = false, remounted = false, footShot = false, wallShot = false;
            bool policeShot = false, streetShot = false, portalSeen = false;
            float crashAt = -1f, minGap = float.MaxValue;
            int spikes = 0, phantoms = 0; float worstSpike = 0f;
            int framesChecked = 0, framesNoBrand = 0, framesNoStreetBrand = 0, minStreetBrands = int.MaxValue;
            var overlapSince = new System.Collections.Generic.Dictionary<TrackObject, int>();
            float startingSpeed = 0f, runStart = Time.realtimeSinceStartup;
            Action<TrackObject> collected = o => { pickups++; if (o.surface != RunSurface.Road) wallChips++; };
            Action<TrackObject> struck = o =>
            {
                hits++;
                Debug.Log($"[DashPlaytest] Hit {o.name} ({o.kind}/{o.surface}) lane={o.lane} at={o.transform.position} rider={rider.transform.position} distance={gm.Distance:0.0}");
            };
            TrackSpawner.Collected += collected; TrackSpawner.Struck += struck;
            // Normal input on the real course. The only scripted state is the crash that puts Ace on foot
            // before the facade section (logged), so the wall run and the bike grab are exercised every time.
            RiderInput.Override = () =>
            {
                float d = gm.Distance;
                if (d > 100f && !dashed) { startingSpeed = gm.Speed; dashed = run.TryDeliveryDash(); }
                if (crashAt < 0f && d > 420f && rider.Traversal.Surface == RunSurface.Road && !rider.Airborne && !rider.Traversal.Moving
                    && !TrackSpawner.NearJunction(d + 20f) && !TrackSpawner.NearJunction(d + 60f))
                {
                    run.KnockOffBike(); run.PoliceGains(DashRun.CrashOnBike);
                    if (run.OnFoot) { knockedOff = true; crashAt = d; Debug.Log("[DashPlaytest] simulated crash at " + d.ToString("0")); }
                }
                bool onWall = rider.Traversal.Surface is RunSurface.LeftWall or RunSurface.RightWall;
                if (onWall) wallEntered = true;
                if (onWall) return RiderInput.Action.None;
                if (rider.Traversal.Moving) return RiderInput.Action.None;
                return Autopilot.Steer(track, rider);
            };
            UnityEngine.Random.InitState(7421 + continuousAttempt * 101 + filmTakeSeed * 7919);
            gm.StartRun();
            if (film) { PrepareFilmCamera(); FindFirstObjectByType<FollowCamera>().MovieFraming = true; SaveFilmMusic(); StartFilmEncoder(); MenuUI.Instance.Caption("ACE / NEON DOWNTOWN", "POLICE CHASE", 2.5f); Time.captureFramerate = 30; Application.targetFrameRate = 30; recording = true; }
            float deadline = Time.realtimeSinceStartup + (film ? 1800f : 150f);
            bool retryAttempt = false;
            const float Goal = 1250f;
            try
            {
                while ((film ? filmFrame < filmSeconds * 30 : !(remounted && gm.Distance > Mathf.Max(Goal, crashAt + 150f))) && Time.realtimeSinceStartup < deadline)
                {
                    if (WipeoutRetry(gm, ref continuousAttempt)) { retryAttempt = true; break; }
                    if (film && !filmAbility && gm.Distance > 250f && run.Battery >= .999f && gm.State == RunState.Running && !gm.Paused)
                    { run.TryAbility(); filmAbility = true; filmEvents.Add($"{filmFrame / 30f:F3},{gm.Distance:F1},ABILITY"); }
                    Check(gm.State == RunState.Running, "continuous run survives at " + gm.Distance.ToString("0"));
                    // Client rule: at least one brand in view in every frame of the run.
                    if (gm.RunTime > 0.3f && !gm.Paused)
                    {
                        var seen = BrandVisibility.InView(Camera.main);
                        framesChecked++;
                        minStreetBrands = Mathf.Min(minStreetBrands, seen.street);
                        if (seen.street == 0) framesNoStreetBrand++;
                        if (seen.street + seen.rider == 0) { framesNoBrand++; Debug.Log($"[Brands] none in view at {gm.Distance:0} m onFoot={run.OnFoot}"); }
                    }
                    if (run.PoliceActive) minGap = Mathf.Min(minGap, run.PoliceGap);
                    if (!policeShot && gm.Distance > 60f && chase && chase.officer && chase.officer.gameObject.activeInHierarchy)
                    {
                        var vp = Camera.main.WorldToViewportPoint(chase.officer.position + Vector3.up);
                        if (vp.z > 0f && vp.y > 0f && vp.y < 0.6f) { policeShot = true; yield return Shot("continuous-police-chase"); }
                    }
                    // No shortcut any more (client, 11 Nov 2026): nothing named a portal may ever be streamed in.
                    if (!portalSeen && Time.frameCount % 30 == 0)
                        foreach (Transform child in track.transform)
                            if (child.gameObject.activeInHierarchy && child.name.IndexOf("portal", StringComparison.OrdinalIgnoreCase) >= 0) { portalSeen = true; Debug.LogError("[DashPlaytest] portal object streamed: " + child.name); }
                    if (!streetShot && gm.Distance > 700f && !run.OnFoot && !rider.Airborne) { streetShot = true; yield return Shot("continuous-downtown-street"); }
                    if (knockedOff && run.OnFoot && !footShot && !rider.Traversal.Moving && rider.Traversal.Surface == RunSurface.Road)
                    { footShot = true; Check(VehicleDynamics.Current.kind == CourierVehicle.Running, "knocked-off rider runs on foot"); yield return Shot("continuous-on-foot"); }
                    if (knockedOff && !run.OnFoot) remounted = true;
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
                            if (overlapsX && overlapsY && !o.passed && !(run.ReviveGrace > 0f || run.Boost > 0f || PowerUps.Active(PowerUp.Rush) || PowerUps.Active(PowerUp.Shield)))
                            {
                                if (!overlapSince.TryGetValue(o, out int since)) overlapSince[o] = Time.frameCount;
                                else if (Time.frameCount - since >= 2) { o.passed = true; phantoms++; Debug.Log($"[Phantom] passed through {o.name} ({o.kind}) lane={o.lane} at {gm.Distance:0} m local=({local.x:0.00},{local.y:0.00})"); }
                            }
                        }
                    if (dashed && run.DashTime > 1.9f && run.DashTime < 2.1f) Check(gm.Speed >= startingSpeed * 1.4f, "dash speed affects real travel before subsequent collision recovery");
                    if (!rider.Traversal.Moving && rider.Traversal.Surface != RunSurface.Road)
                    {
                        var anchor = SurfaceTraversal.Anchor(rider.Traversal.Surface);
                        var offset = rider.transform.position - anchor; offset.z = 0f;
                        Check(Mathf.Abs(Vector3.Dot(offset, rider.SurfaceNormal) - rider.JumpHeight) < 0.12f, "rider contacts actual route");
                        var viewport = Camera.main.WorldToViewportPoint(rider.transform.position + rider.SurfaceNormal);
                        if (!film) Check(viewport.z > 0 && viewport.x > 0.12f && viewport.x < 0.88f && viewport.y > 0.08f && viewport.y < 0.92f, $"rider remains inside camera frame (viewport {viewport.x:0.00},{viewport.y:0.00}) at {gm.Distance:0.0} m");
                        if (run.WallRunning && !wallShot) { wallShot = true; yield return Shot("continuous-wall"); }
                    }
                    yield return null;
                }
                if (!retryAttempt)
                {
                    if (!film) Check(gm.Distance >= Goal, $"travels {Goal:0} m continuously");
                    if (!film) Check(!portalSeen && DashRun.District == 0, "Downtown only: no shortcut portal streamed in the whole run");
                    if (!film) Check(minGap < float.MaxValue && (policeShot || minGap >= ChaseController.ShowGap), $"the officer is on screen whenever he closes in (closest {minGap:0.0} m)");
                    if (!film) Check(!wallEntered && run.WallRuns == 0, "no wall running anywhere in the run");
                    if (!film) Check(knockedOff && footShot && remounted, "crash puts Ace on foot, then back on the e-bike by grabbing it");
                    if (!film) Check(phantoms == 0, "no hurdle is passed through without a hit: " + phantoms);
                    if (!film) Check(framesChecked > 500 && framesNoBrand == 0, $"a brand is in view in every frame: {framesChecked} frames checked, {framesNoBrand} without");
                    if (!film) Check(track.PooledReused > 0, "streamed items actually reused from Unity object pools");
                    float minutes = (Time.realtimeSinceStartup - runStart) / 60f;
                    File.WriteAllText(Path.Combine(output, "continuous-pass.txt"), $"PASS distance={gm.Distance:0.0}; pickups={pickups}; burgersEarned={gm.BurgersEarned}; runSeconds={gm.RunTime:0.0}; burgersPerHour={gm.BurgersEarned / Mathf.Max(0.01f, gm.RunTime / 3600f):0}; hits={hits}; minPoliceGap={minGap:0.0}; wallRuns={run.WallRuns}; assertions={assertions}; pooledCreated={track.PooledCreated}; pooledReused={track.PooledReused}; spikes45ms={spikes}; worstFrameMs={worstSpike * 1000f:0}; brandFrames={framesChecked}; framesWithoutAnyBrand={framesNoBrand}; framesWithoutStreetBrand={framesNoStreetBrand}; minStreetBrandsInView={minStreetBrands}\nOne scripted crash (logged); no obstacle disabling or invulnerability.\n");
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
                File.WriteAllText(Path.Combine(output, "recording-pass.txt"), $"ACE; ability={filmAbility}; wallRuns={run.WallRuns}; frames={filmFrame}; fps=30; duration={filmSeconds}s; distance={gm.Distance:F1}\n");
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
        }
    }
}

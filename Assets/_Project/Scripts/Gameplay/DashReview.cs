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
    public class DashReview : MonoBehaviour
    {
        string output;
        int district, shot, assertions;
        bool continuous;
        bool film, benchmark, recording;
        int filmFrame;
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
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == "-dash-review" || args[i] == "-dash-playtest" || args[i] == "-dash-comprehensive" || args[i] == "-dash-film" || args[i] == "-dash-benchmark")
            {
                var review = new GameObject("Dash Review").AddComponent<DashReview>();
                Application.runInBackground = true;
                review.output = args[i + 1];
                review.film = args[i] == "-dash-film";
                review.benchmark = args[i] == "-dash-benchmark";
                review.continuous = args[i] == "-dash-playtest" || review.film || review.benchmark;
                review.comprehensive = args[i] == "-dash-comprehensive";
                for (int j = 0; j < args.Length - 1; j++)
                {
                    if (args[j] == "-district") int.TryParse(args[j + 1], out review.district);
                    if (args[j] == "-character") int.TryParse(args[j + 1], out review.comprehensiveChar);
                    if (args[j] == "-vehicle") int.TryParse(args[j + 1], out review.comprehensiveVeh);
                }
                return;
            }
        }
        void Check(bool ok, string message)
        {
            assertions++;
            if (ok) { if (!continuous) Debug.Log("[DashReview] PASS " + message); return; }
            Debug.LogError("[DashReview] FAIL " + message); Restore(); Application.Quit(1);
            throw new Exception(message);
        }
        void Backup()
        {
            var keys = new List<string> { "character", "total_points", "dash_district", "coins", "ride", "coached_runs", "best", "missions_done", "missions_v", "daily_last", "daily_streak" };
            keys.AddRange(new[] { "dash_fragments", "dash_outfit_tokens", "dash_event_collectibles", "dash_deliveries" });
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
            PlayerPrefs.Save(); Time.timeScale = 1f; Time.captureFramerate = 0; recording = false; RiderInput.Override = null;
            RestoreFilmCamera();
        }
        void OnApplicationQuit() => Restore();
        IEnumerator Shot(string label)
        {
            if (film || benchmark) { yield return null; yield break; }
            yield return new WaitForSecondsRealtime(0.25f);
            // A hidden Windows window may not present a backbuffer. Render explicitly into a texture
            // and attach overlay canvases to that camera so screenshots include the complete UI.
            var cam = Camera.main;
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
            var previous = RenderTexture.active; RenderTexture.active = rt;
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false);
            texture.ReadPixels(new Rect(0, 0, width, height), 0, 0); texture.Apply();
            var pixels = texture.GetPixels32(); int light = 0;
            for (int i = 0; i < pixels.Length; i += 97) if (pixels[i].r + pixels[i].g + pixels[i].b > 40) light++;
            Check(light > 100, "screenshot renders visible content: " + label);
            File.WriteAllBytes(Path.Combine(output, (++shot).ToString("00") + "-" + label + ".png"), texture.EncodeToPNG());
            RenderTexture.active = previous; Destroy(texture); RenderTexture.ReleaseTemporary(rt);
            for (int i = 0; i < canvases.Length; i++) { canvases[i].renderMode = modes[i]; canvases[i].worldCamera = cameras[i]; }
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
            if (continuous) { yield return ContinuousRun(); yield break; }
            if (comprehensive) { yield return ComprehensiveRun(); yield break; }
            PlayerPrefs.SetInt("coached_runs", 2);
            for (int i = 0; i < 3; i++) PlayerPrefs.SetInt("dash_outfit_" + i, 0);
            PlayerProfile.TotalPoints = Mathf.Max(30000, PlayerProfile.TotalPoints);
            PlayerProfile.Character = film ? 1 : 0; PlayerProfile.Ride = 0; DashRun.District = Mathf.Clamp(district, 0, 2);
            yield return new WaitForSeconds(1.5f);
            var menu = MenuUI.Instance; var gm = GameManager.Instance; var run = DashRun.Instance;
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            Check(menu && gm && run && track && rider, "scene dependencies");
            menu.Open("none"); track.RefreshDistrict();
            yield return Shot("title");
            foreach (string panel in new[] { "crew", "shop", "missions", "districts", "settings" })
            {
                menu.Open(panel); yield return Shot(panel);
                if (panel == "crew") { menu.FlipCrew(1); yield return Shot("nova"); menu.FlipCrew(1); yield return Shot("bolt"); }
                menu.Open("none");
            }
            DemoRewards.Ledger.badges += 9;
            var coupon = DemoRewards.Issue(); var second = DemoRewards.Issue();
            Check(coupon != null && second != null && coupon.code != second.code, "unique coupon issue");
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
            Place(320, track, rider); rider.BounceBack(0); run.JumpMove();
            yield return new WaitForSeconds(0.5f);
            Check(run.WallRunning && run.SurfaceHeight > 0f && rider.SurfaceNormal.x > 0.99f, "wall contact has inward gravity");
            yield return Shot("wall-run");
            Place(605, track, rider); run.JumpMove();
            yield return new WaitForSeconds(0.5f);
            Check(run.Surface == RunSurface.LeftWall, "gravity tunnel includes wall route");
            run.JumpMove(); yield return new WaitForSeconds(0.5f);
            Check(run.Flipped && run.SurfaceHeight > 6f && rider.SurfaceNormal.y < -0.99f, "gravity ceiling contact");
            yield return Shot("gravity-flip");
            Place(720, track, rider); yield return null;
            yield return new WaitForSeconds(0.5f);
            Check(!run.Flipped, "gravity ends at tunnel boundary");
            run.StartSlide(); Check(run.Slide > 0, "slide input");
            yield return Shot("slide");
            int badges = DemoRewards.Ledger.badges;
            gm.StartRun(); Place(451, track, rider); yield return null;
            Check(run.Deliveries == 1 && DemoRewards.Ledger.badges == badges + 1, "delivery pays one badge");
            int paid = DemoRewards.Ledger.badges; yield return null;
            Check(DemoRewards.Ledger.badges == paid, "checkpoint cannot pay twice");
            yield return Shot("delivery");
            gm.StartRun();
            Check(run.TryDeliveryDash(), "all crew can activate delivery dash");
            Check(!run.TryDeliveryDash(), "delivery dash cooldown prevents spam");
            yield return new WaitForSeconds(0.25f);
            Check(gm.Speed > Tuning.SpeedBase * 1.45f, "delivery dash actually accelerates forward movement");
            yield return Shot("delivery-dash");
            gm.StartRun();
            int coinsBefore = PlayerProfile.Coins;
            gm.AddPickup(15, false); gm.AddPickup(5, true);
            Check(gm.PickupCount == 2 && gm.EnergyCollected == 1, "food and currency counted separately");
            gm.Bust();
            Check(gm.CoinsEarned == 1 && PlayerProfile.Coins == coinsBefore + 1, "only energy chips pay currency at run end");
            gm.StartRun();
            foreach (float checkpoint in new[] { 451f, 901f, 1351f })
            {
                Place(checkpoint, track, rider); yield return null; yield return null;
            }
            Check(run.Deliveries == 3 && gm.State == RunState.Running, "endless runner continues after the third delivery");
            run.PenalizeTime(1000f); yield return null; yield return null;
            Check(gm.State == RunState.Busted, "exhausted delivery clock ends the endless run");
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
            PlayerProfile.TotalPoints = Mathf.Max(30000, PlayerProfile.TotalPoints);
            PlayerProfile.Coins = 1000;
            
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
                run.ToggleBoard();
                Check(run.Board, "Board active");
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

        IEnumerator ContinuousRun()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            PlayerProfile.TotalPoints = Mathf.Max(30000, PlayerProfile.TotalPoints);
            PlayerProfile.Character = film ? 1 : 0; PlayerProfile.Ride = 0; DashRun.District = Mathf.Clamp(district, 0, 2);
            yield return new WaitForSeconds(1.5f);
            MenuUI.Instance.Open("none");
            var gm = GameManager.Instance; var run = DashRun.Instance;
            var rider = FindFirstObjectByType<RiderController>();
            var track = FindFirstObjectByType<TrackSpawner>();
            int wallChips = 0, ceilingChips = 0, hits = 0;
            bool wallEntered = false, ceilingSeen = false, rightSeen = false, dashed = false;
            bool wallShot = false, ceilingShot = false, sawParkour = false, shortcutShot = false, rooftopShot = false;
            int switches = 0;
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
                if (d > 100f && !dashed) { startingSpeed = gm.Speed; dashed = run.TryDeliveryDash(); }
                if (d >= 303f && d < 320f && !wallEntered)
                {
                    if (rider.Lane > 0) return RiderInput.Action.Left;
                    wallEntered = true; return RiderInput.Action.Jump;
                }
                if (d >= 605f && d < 675f && switches < 3 && !rider.Traversal.Moving && gm.RunTime - lastSwitch > 0.8f)
                {
                    if (rider.Lane != 1) return rider.Lane < 1 ? RiderInput.Action.Right : RiderInput.Action.Left;
                    switches++; lastSwitch = gm.RunTime; return RiderInput.Action.Jump;
                }
                if (DashRun.WallZone(d) || DashRun.GravityZone(d)) return RiderInput.Action.None;
                if (district==1 && rider.Lane!=Tuning.RouteLane && !rider.Airborne)
                    foreach(var gap in track.Live)
                        if(gap && gap.kind==TrackKind.Gap && gap.lane==rider.Lane && gap.transform.position.z-gap.halfLength-d>-.5f && gap.transform.position.z-gap.halfLength-d<gm.Speed*.25f)
                            return RiderInput.Action.Jump;
                // Deliberately take the first route of each kind: be in the right-hand lane before the
                // entry window opens, then swipe onto the fourth lane as soon as it does.
                bool firstShortcut = d > DashRun.ShortcutStart - 70f && d < DashRun.ShortcutStart + DashRun.ShortcutLength;
                bool firstParkour = district == 1 && d > DashRun.ParkourStart - 70f && d < DashRun.ParkourStart + DashRun.ParkourLength;
                if (firstShortcut || firstParkour)
                {
                    if (rider.Lane != Tuning.RouteLane && DashRun.RouteEntry(d)) return RiderInput.Action.Right;
                    if (rider.Lane != Tuning.RouteLane && rider.Lane < Tuning.Lanes - 1) return RiderInput.Action.Right;
                    if (rider.Lane == Tuning.RouteLane)
                    {
                        // Rooftop hurdles are hopped, never dodged: there is no lane beside the route.
                        foreach (var o in track.Live)
                        {
                            if (!o || o.consumed || o.lane != Tuning.RouteLane || o.kind != TrackKind.Low && o.kind != TrackKind.Gap) continue;
                            float ahead = o.transform.position.z - d - o.halfLength;
                            if (ahead < -0.6f || ahead > gm.Speed * 0.55f) continue;
                            if (!rider.Airborne && ahead < gm.Speed * 0.45f) return RiderInput.Action.Jump;
                        }
                        return RiderInput.Action.None;
                    }
                }
                if (rider.Lane == Tuning.RouteLane) return RiderInput.Action.None;
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
            UnityEngine.Random.InitState(7421 + district);
            gm.StartRun();
            if (film) { PrepareFilmCamera(); FindFirstObjectByType<FollowCamera>().MovieFraming=true; SaveFilmMusic(); Time.captureFramerate = 30; Application.targetFrameRate = 30; recording = true; }
            float deadline = Time.realtimeSinceStartup + (film ? 240f : 120f);
            try
            {
                while (gm.Distance < 725f && Time.realtimeSinceStartup < deadline)
                {
                    Check(gm.State == RunState.Running, "continuous run survives at " + gm.Distance.ToString("0"));
                    if (run.OnRoute && gm.Distance > 230f && gm.Distance < 270f && !shortcutShot)
                    {
                        shortcutShot = true;
                        Check(rider.Lane == Tuning.RouteLane && rider.transform.position.x > 5.5f, "express route uses a physical fourth lane");
                        Check(gm.Speed > Mathf.Min(Tuning.SpeedMax, Tuning.SpeedBase + gm.Distance * Tuning.SpeedRamp) * 1.2f, "express route reduces real travel time");
                        yield return Shot("continuous-fourth-lane");
                    }
                    if (district == 1 && gm.Distance > 440f && gm.Distance < 490f && rider.transform.position.y > 3f && !rooftopShot)
                    {
                        rooftopShot = true;
                        yield return Shot("continuous-rooftop-parkour");
                    }
                    if (district == 1 && gm.Distance > 420f && gm.Distance < 500f && !sawParkour)
                        foreach (var o in track.Live)
                            if (o && o.kind == TrackKind.Platform) { sawParkour = true; break; }
                    if (dashed && run.DashTime > 1.9f && run.DashTime < 2.1f) Check(gm.Speed >= startingSpeed * 1.4f, "dash speed affects real travel before subsequent collision recovery");
                    if (!rider.Traversal.Moving && rider.Traversal.Surface != RunSurface.Road)
                    {
                        var expected = SurfaceTraversal.Point(rider.Traversal.Surface, Tuning.LaneX[rider.Lane], rider.JumpHeight);
                        Check(Vector2.Distance(new Vector2(rider.transform.position.x, rider.transform.position.y), new Vector2(expected.x, expected.y)) < 0.12f, "rider contacts actual route");
                        var viewport = Camera.main.WorldToViewportPoint(rider.transform.position + rider.SurfaceNormal);
                        Check(viewport.z > 0 && viewport.x > 0.12f && viewport.x < 0.88f && viewport.y > 0.08f && viewport.y < 0.92f, "rider remains inside camera frame with body margin");
                        if (run.Flipped) ceilingSeen = true;
                        if (run.Surface == RunSurface.RightWall) rightSeen = true;
                        if (run.WallRunning && !wallShot) { wallShot = true; yield return Shot("continuous-wall"); }
                        if (run.Flipped && !ceilingShot) { ceilingShot = true; yield return Shot("continuous-ceiling"); }
                    }
                    yield return null;
                }
                Check(gm.Distance >= 725f, "travels 725m continuously without teleporting");
                Check(dashed && run.Deliveries == 1, "delivery dash and delivery checkpoint completed");
                Check(wallEntered && ceilingSeen && rightSeen, "input traverses wall ceiling and opposite wall");
                Check(wallChips > 0 && ceilingChips > 0, "collects actual wall and ceiling route chips");
                Check(run.Surface == RunSurface.Road && !rider.Traversal.Moving, "exits tunnel onto road continuously");
                Check(run.Shortcuts >= 1, "fourth-lane shortcut taken and counted");
                Check(shortcutShot, "fourth lane traversed and captured through player input");
                if (district == 1) Check(rooftopShot, "rider reached actual building rooftops");
                if (district == 1) Check(sawParkour, "skypark parkour route spawned");
                Check(track.PooledReused > 0, "streamed items actually reused from Unity object pools");
                File.WriteAllText(Path.Combine(output, "continuous-pass.txt"), $"PASS district={district}; distance={gm.Distance:0.0}; wallChips={wallChips}; ceilingChips={ceilingChips}; hits={hits}; assertions={assertions}; pooledCreated={track.PooledCreated}; pooledReused={track.PooledReused}\nNo teleports, obstacle disabling or invulnerability.\n");
                Debug.Log("[DashReview] CONTINUOUS COMPLETE");
            }
            finally { TrackSpawner.Collected -= collected; TrackSpawner.Struck -= struck; RiderInput.Override = null; }
            recording = false;
            if (film) { yield return new WaitForEndOfFrame(); yield return new WaitForSecondsRealtime(0.5f); }
            gm.ToMenu(); Restore(); Application.Quit(0);
        }
        void LateUpdate()
        {
            if (!recording || !filmCamera) return;
            var target = filmCamera.targetTexture;
            var active = RenderTexture.active;
            filmCamera.targetTexture = filmTarget;
            Canvas.ForceUpdateCanvases();
            RenderPipeline.SubmitRenderRequest(filmCamera,new RenderPipeline.StandardRequest { destination=filmTarget });
            RenderTexture.active=filmTarget;
            filmPixels.ReadPixels(new Rect(0,0,720,1280),0,0); filmPixels.Apply(false);
            File.WriteAllBytes(Path.Combine(output,$"frame-{filmFrame++:D6}.png"),filmPixels.EncodeToPNG());
            filmCamera.targetTexture=target; RenderTexture.active=active;
        }
        void PrepareFilmCamera()
        {
            filmCamera=Camera.main;filmAspect=filmCamera.aspect;filmCamera.aspect=9f/16f;
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
            var clip = Synth.Music();
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

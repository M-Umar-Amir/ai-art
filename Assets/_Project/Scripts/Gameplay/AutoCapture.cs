using System.Collections;
using System.IO;
using DallasDash.Core;
using Unity.Profiling;
using UnityEngine;

namespace DallasDash.Gameplay
{
    /// Screenshot mode for review builds: launch with `-capture <dir>`. An autopilot rides a run,
    /// a numbered PNG is saved at set moments, then the autopilot lets go so the run ends in a bust, and the app quits.
    public class AutoCapture : MonoBehaviour
    {
        string dir;
        TrackSpawner track;
        RiderController rider;
        bool autopilot;
        int frames;
        float runTime, worst, lastShotAt = -1f;
        // Where the frame goes: main-thread CPU, render-thread CPU, GPU, plus draw-call counts.
        double cpuMain, cpuRender, gpu;
        long batches, setPass, tris;
        int timed;
        ProfilerRecorder batchRec, setPassRec, triRec;
        readonly FrameTiming[] timing = new FrameTiming[1];

        // Whole-run hitch tracking, including the late chase after the autopilot lets go (where speed peaks).
        int lateFrames, lateHitches;
        float lateWorst;

        void Update()
        {
            if (GameManager.Instance.State == RunState.Running && Time.unscaledTime - lastShotAt > 0.3f && frames > 30)
            {
                float ms = Time.unscaledDeltaTime * 1000f;
                if (!autopilot) { lateFrames++; lateWorst = Mathf.Max(lateWorst, ms); if (ms > 25f) lateHitches++; }
            }
            if (!autopilot || GameManager.Instance.State != RunState.Running) return;
            // Saving a double-size screenshot stalls a frame or two: leave those out of the reading.
            if (Time.unscaledTime - lastShotAt < 0.3f) return;
            frames++;
            runTime += Time.unscaledDeltaTime;
            FrameTimingManager.CaptureFrameTimings();
            if (FrameTimingManager.GetLatestTimings(1, timing) > 0)
            {
                cpuMain += timing[0].cpuMainThreadFrameTime;
                cpuRender += timing[0].cpuRenderThreadFrameTime;
                gpu += timing[0].gpuFrameTime;
                timed++;
            }
            batches += batchRec.LastValue;
            setPass += setPassRec.LastValue;
            tris += triRec.LastValue;
            if (runTime > 1f) worst = Mathf.Max(worst, Time.unscaledDeltaTime);
        }
        int shot;

        /// `-branded`: client presentation stills with the client's restaurant brands on the shop signs
        /// (TrackSpawner.BrandedPreview). Set before the scene loads so the first street is built with them.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Flags()
        {
            foreach (var a in System.Environment.GetCommandLineArgs())
                if (a == "-branded") TrackSpawner.BrandedPreview = true;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                // `-tier 0|1|2` measures a given graphics setting (Low / Medium / High).
                if (args[i] == "-tier" && int.TryParse(args[i + 1], out int tier))
                {
                    PlayerProfile.Graphics = tier;
                    GameSettings.ApplyGraphics();
                }
                if (args[i] != "-capture") continue;
                var c = new GameObject("AutoCapture").AddComponent<AutoCapture>();
                c.dir = args[i + 1];
                return;
            }
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(dir);
            batchRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count");
            setPassRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count");
            triRec = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Triangles Count");
            track = FindFirstObjectByType<TrackSpawner>();
            rider = FindFirstObjectByType<RiderController>();
            RiderInput.Override = Steer;

            yield return new WaitForSeconds(1.5f);
            var menu = DallasDash.UI.MenuUI.Instance;
            if (menu)
            {
                menu.Open("none");
                yield return new WaitForSeconds(0.3f);
                yield return Shot("title");
                menu.Open("daily");
                yield return new WaitForSeconds(0.3f);
                yield return Shot("daily-bonus");
                menu.Open("none");
                menu.Open("settings");
                yield return new WaitForSeconds(0.3f);
                yield return Shot("settings");
                menu.Open("none");
                menu.Open("crew");
                yield return new WaitForSeconds(0.3f);
                yield return Shot("crew");
                menu.FlipCrew(-1); // round to Fizz-11 (locked silhouette until 25k points)
                yield return new WaitForSeconds(0.4f);
                yield return Shot("crew-locked");
                menu.Open("none");
                menu.Open("shop");
                yield return new WaitForSeconds(0.3f);
                yield return Shot("shop");
                menu.Open("none");
                menu.Open("missions");
                yield return new WaitForSeconds(0.3f);
                yield return Shot("missions");
                menu.Open("none");
                // The earned scooter, on the title shot, without saving it to the profile.
                var look = FindFirstObjectByType<RiderLook>();
                if (look && look.scooter)
                {
                    PlayerProfile.ForceRide = 1;
                    look.Apply(PlayerProfile.Character, true);
                    yield return new WaitForSeconds(0.3f);
                    yield return Shot("scooter");
                    PlayerProfile.ForceRide = -1;
                    look.Apply(PlayerProfile.Character, true);
                }
            }
            else yield return Shot("ready");
            if (TrackSpawner.BrandedPreview) yield return HurdleGallery();

            autopilot = true;
            var story = StoryDirector.Instance;
            if (story)
            {
                story.Play();
                foreach (float t in new[] { 1.4f, 1.6f, 1.4f, 1.6f })
                {
                    yield return new WaitForSeconds(t);
                    yield return Shot("intro");
                }
                while (GameManager.Instance.State != RunState.Running) yield return null;
            }
            else GameManager.Instance.StartRun();
            // Client presentation stills: a longer look down the street with the brands on the shops, and a few
            // frozen close-ups of the storefronts from the sidewalk.
            if (TrackSpawner.BrandedPreview)
            {
                for (int i = 0; i < 8; i++)
                {
                    yield return new WaitForSeconds(1.3f);
                    yield return Shot("street");
                    if (i == 3 || i == 7) yield return Storefronts();
                }
            }
            float[] waits = { 1.5f, 2.5f, 3f, 3f, 4f };
            for (int i = 0; i < waits.Length; i++)
            {
                // Show the power-ups at work: magnet + double score, then a Sugar Rush.
                if (i == 2) { PowerUps.Activate(PowerUp.Magnet); PowerUps.Activate(PowerUp.Double); }
                if (i == 4) PowerUps.Activate(PowerUp.Rush);
                yield return new WaitForSeconds(waits[i]);
                yield return Shot(i >= 2 ? "powerup" : "run");
            }
            PowerUps.Clear();

            File.WriteAllText(Path.Combine(dir, "fps.txt"), $"run fps avg {frames / Mathf.Max(runTime, 0.01f):F1}, worst frame {worst * 1000f:F1} ms, tier {PlayerProfile.Graphics}\n" +
                $"  cpu main {cpuMain / Mathf.Max(timed, 1):F1} ms, cpu render {cpuRender / Mathf.Max(timed, 1):F1} ms, gpu {gpu / Mathf.Max(timed, 1):F1} ms; " +
                $"batches {batches / Mathf.Max(frames, 1)}, setpass {setPass / Mathf.Max(frames, 1)}, tris {tris / Mathf.Max(frames, 1) / 1000}k\n");

            // Let go of the bars: the next hits pull the officer in and end the run.
            autopilot = false;
            float wait = 0f;
            while (GameManager.Instance.State == RunState.Running && wait < 20f)
            {
                if (wait > 0f && Mathf.Repeat(wait, 1.2f) < Time.deltaTime) yield return Shot("chase");
                wait += Time.deltaTime;
                yield return null;
            }
            File.AppendAllText(Path.Combine(dir, "fps.txt"), $"late run (top speed, chase): {lateFrames} frames, worst {lateWorst:F1} ms, hitches over 25 ms: {lateHitches}" + "\n");
            yield return new WaitForSeconds(0.8f);
            yield return Shot("busted");
            // The result card (with the mission strip) slides in after the two-shot.
            yield return new WaitForSeconds(1.4f);
            yield return Shot("result");
            Application.Quit();
        }

        /// Freezes the run, hides the HUD and frames the shops on each side from the sidewalk, then hands back.
        IEnumerator Storefronts()
        {
            var gm = GameManager.Instance;
            if (gm.State != RunState.Running) yield break;
            var follow = FindFirstObjectByType<FollowCamera>();
            if (!follow) yield break;
            gm.SetPaused(true);
            follow.cinematic = true;
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases) c.enabled = false;
            var cam = follow.transform;
            Vector3 was = cam.position;
            Quaternion rot = cam.rotation;
            float d = gm.Distance;
            // Clear the lanes for the frame so nothing stands between the lens and the shops.
            var hidden = new System.Collections.Generic.List<GameObject>();
            foreach (var o in track.Live)
                if (o && o.gameObject.activeSelf) { o.gameObject.SetActive(false); hidden.Add(o.gameObject); }
            foreach (float side in new[] { 1f, -1f })
            {
                // Looking along the shop fronts so several sign boards line up in the frame.
                cam.position = new Vector3(-side * 3.4f, 3f, d - 2f);
                cam.LookAt(new Vector3(side * 6.8f, 3.2f, d + 11f));
                yield return null;
                yield return Shot("storefront");
            }
            foreach (var g in hidden) if (g) g.SetActive(true);
            cam.position = was;
            cam.rotation = rot;
            foreach (var c in canvases) c.enabled = true;
            follow.cinematic = false;
            gm.SetPaused(false);
        }

        /// One frame per hurdle, set on the street in front of the title spot and framed three-quarter on.
        IEnumerator HurdleGallery()
        {
            var follow = FindFirstObjectByType<FollowCamera>();
            if (!follow || track.obstacles == null) yield break;
            follow.cinematic = true;
            var canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
            foreach (var c in canvases) c.enabled = false;
            var cam = follow.transform;
            const float z = -22f;
            foreach (var def in track.obstacles)
            {
                if (!def || !def.prefab) continue;
                var go = Instantiate(def.prefab, new Vector3(Core.Tuning.LaneX[1], 0f, z), Quaternion.identity);
                float reach = Mathf.Max(def.size.z, def.size.y * 1.6f, 2.2f);
                cam.position = new Vector3(reach * 0.55f + 1.2f, Mathf.Max(1.5f, def.size.y * 0.8f), z - reach * 0.9f - 2.2f);
                cam.LookAt(new Vector3(0f, def.size.y * 0.42f, z));
                yield return new WaitForSeconds(0.3f);
                yield return Shot("hurdle-" + def.id);
                Destroy(go);
            }
            foreach (var c in canvases) c.enabled = true;
            follow.cinematic = false;
        }

        IEnumerator Shot(string label)
        {
            yield return new WaitForEndOfFrame();
            string path = Path.Combine(dir, $"{++shot:00}-{label}.png");
            ScreenCapture.CaptureScreenshot(path, 2);
            lastShotAt = Time.unscaledTime;
            Debug.Log("[DallasDash] Captured " + path);
            // The capture is written at the end of the next frame: hold still until then.
            yield return null;
            yield return null;
        }

        RiderInput.Action Steer() => autopilot ? Autopilot.Steer(track, rider) : RiderInput.Action.None;
    }
}

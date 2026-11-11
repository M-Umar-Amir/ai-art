using System.Collections;
using System.IO;
using System.Reflection;
using DallasDash.Core;
using DallasDash.UI;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.Gameplay
{
    /// Showcase reel, part 1 (-dash-crew-reel): no narration. Director-camera orbits of each crew member in the
    /// dusk street, then the new courier e-bike on its own (Ace orange, Nova purple) and with Ace seated.
    /// Shot list (30 fps, 720x1280, game music only):
    ///   ACE    8 s  medium orbit, front -> three-quarter -> side, name card
    ///   NOVA   8 s  medium orbit, opposite direction, name card
    ///   BOLT   8 s  medium orbit on his monowheel, name card
    ///   E-BIKE 5 s  close low orbit of Ace's bike alone (wheels, nose light bar, cargo pod)
    ///   E-BIKE 4 s  close orbit of Nova's purple bike
    ///   RIDING 5 s  medium orbit with Ace seated, ends behind the rider (the gameplay follow angle), so the
    ///               cut into the first district run keeps screen direction.
    public partial class DashReview
    {
        bool crewReel;
        Text reelTitle, reelSub;

        IEnumerator CrewReel()
        {
            PlayerPrefs.SetInt("coached_runs", 2);
            BurgerPrices.UnlockAllForReview();
            for (int i = 0; i < 3; i++) PlayerPrefs.SetInt("dash_outfit_" + i, 0);
            PlayerProfile.Character = 0;
            yield return new WaitForSeconds(2f);
            var menu = MenuUI.Instance; var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            var look = FindFirstObjectByType<RiderLook>();
            Check(menu && track && rider && look, "reel scene dependencies");
            typeof(MenuUI).GetMethod("ExitCity", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(menu, null);
            menu.Open("none"); track.RefreshDistrict();
            for (int f = 0; f < 90; f++) yield return null; // let the street stream in

            // Director camera: the gameplay follow rig is parked; the reel moves the camera itself.
            var cam = Camera.main;
            var follow = cam.GetComponent<FollowCamera>(); if (follow) follow.enabled = false;
            var brain = cam.GetComponent<CinemachineBrain>(); if (brain) brain.enabled = false;
            foreach (var canvas in FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;
            BuildReelCard();
            filmSeconds = 38; // 3 x 8 s crew + 5 + 4 + 5 s bike: must equal the shot list
            PrepareFilmCamera(); SaveFilmMusic(); StartFilmEncoder();
            Time.captureFramerate = CaptureRate; Application.targetFrameRate = CaptureRate; recording = true;

            Vector3 at = rider.transform.position;
            string[] names = { "ACE", "NOVA", "BOLT" };
            string[] roles = { "THE SPEEDSTER  /  SPEED BOOST", "PARKOUR QUEEN  /  AIR TIME", "MONO-WHEEL COURIER  /  SHIELD DRONE" };
            for (int c = 0; c < 3; c++)
            {
                PlayerProfile.Character = c;
                look.Apply(c, c == 2); // Ace and Nova standing; Bolt on his magnetic wheel
                Card(names[c], roles[c]);
                yield return Orbit(cam, at + Vector3.up * 1.05f, 3.3f, .45f, c == 1 ? 40f : -40f, c == 1 ? -70f : 70f, 8f, 40f);
            }

            // The new e-bike on its own: the rider is hidden and the parked bike model takes the spot.
            rider.visual.gameObject.SetActive(false);
            for (int b = 0; b < 2; b++)
            {
                var model = track.parkedBikes != null && track.parkedBikes.Length > b ? track.parkedBikes[b] : null;
                Check(model, "parked e-bike model " + b);
                var bike = Instantiate(model, at, Quaternion.identity);
                Card("NEW COURIER E-BIKE", b == 0 ? "ACE  /  ORANGE" : "NOVA  /  PURPLE");
                yield return Orbit(cam, at + Vector3.up * 0.7f, 2.1f, .35f, b == 0 ? 60f : -30f, b == 0 ? -80f : -150f, b == 0 ? 5f : 4f, 38f);
                Destroy(bike);
            }
            rider.visual.gameObject.SetActive(true);
            PlayerProfile.Character = 0; look.Apply(0, true);
            Card("READY TO RIDE", "ACE ON THE NEW E-BIKE");
            yield return Orbit(cam, at + Vector3.up * 1.0f, 3.4f, .7f, 120f, 180f, 5f, 42f);

            while (filmFrame < filmSeconds * CaptureRate) yield return null;
            recording = false;
            FinishFilmEncoder();
            File.WriteAllText(Path.Combine(output, "recording-pass.txt"), $"CREW REEL; frames={filmFrame}; fps={CaptureRate}; duration={filmSeconds}s\n");
            yield return new WaitForSecondsRealtime(0.5f);
            Restore(); Application.Quit(0);
        }

        /// Orbits the camera round `target` from `fromDeg` to `toDeg` (0 = in front of the rider, facing them)
        /// over exactly `seconds` of recorded film, eased in and out. `height` is the camera height above the target.
        IEnumerator Orbit(Camera cam, Vector3 target, float radius, float height, float fromDeg, float toDeg, float seconds, float fov)
        {
            int start = filmFrame, frames = Mathf.RoundToInt(seconds * CaptureRate);
            cam.fieldOfView = fov;
            while (filmFrame < start + frames)
            {
                if (filmFrame >= filmSeconds * CaptureRate) yield break; // the film is full: never wait on frames that will not come
                float k = Mathf.SmoothStep(0f, 1f, (filmFrame - start) / (float)frames);
                float a = Mathf.Lerp(fromDeg, toDeg, k) * Mathf.Deg2Rad;
                cam.transform.position = target + new Vector3(Mathf.Sin(a) * radius, height + .2f * Mathf.Sin(k * Mathf.PI), Mathf.Cos(a) * radius);
                cam.transform.LookAt(target);
                yield return null;
            }
        }

        void BuildReelCard()
        {
            var go = new GameObject("Reel card", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
            var scaler = go.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            var band = UiKit.Panel(go.transform, "Card band", new Vector2(0, -700), new Vector2(1080, 200), new Color(.04f, .05f, .12f, .72f));
            band.raycastTarget = false;
            reelTitle = UiKit.Label(band.transform, "", 92, new Vector2(0, 30), new Vector2(1040, 110), UiKit.Yellow, TextAnchor.MiddleCenter, true);
            reelSub = UiKit.Label(band.transform, "", 34, new Vector2(0, -55), new Vector2(1040, 50), Color.white);
        }

        void Card(string title, string sub) { if (reelTitle) { reelTitle.text = title; reelSub.text = sub; } }
    }
}

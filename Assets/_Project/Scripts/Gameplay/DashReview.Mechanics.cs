using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.Gameplay
{
    public partial class DashReview
    {
        [Serializable] sealed class GuideChapter { public float seconds; public string title, narration; }
        [Serializable] sealed class GuideChapters { public GuideChapter[] chapters; }
        readonly List<GuideChapter> guideChapters = new();
        Text guideTitle, guideBody, guideMode;

        void Guide(string title, string body)
        {
            if (guideTitle) guideTitle.text = title;
            if (guideBody) guideBody.text = body;
            if (guideMode) guideMode.text = FuelStops.Instance && FuelStops.Instance.Pitting ? "LIVE CHARGING" : GameManager.Instance.Paused ? "WALKTHROUGH PAUSED" : "LIVE GAMEPLAY";
            guideChapters.Add(new GuideChapter { seconds = filmFrame / (float)CaptureRate, title = title, narration = body });
            Debug.Log("MECHANICS_STAGE " + title + " distance=" + GameManager.Instance.Distance);
        }
        IEnumerator GuideHold(float seconds)
        {
            if (!recording) { yield return new WaitForSecondsRealtime(seconds); yield break; }
            int until = filmFrame + Mathf.RoundToInt(seconds * CaptureRate);
            while (filmFrame < until) yield return null;
        }
        RiderInput.Action GuideInput(TrackSpawner track, RiderController rider)
        {
            float distance = GameManager.Instance.Distance;
            float[] danger = new float[3];
            foreach (var obstacle in track.Live)
            {
                if (!obstacle || obstacle.consumed || obstacle.kind == TrackKind.Pickup || obstacle.kind == TrackKind.Platform || obstacle.lane >= 3) continue;
                float ahead = obstacle.transform.position.z - distance - obstacle.halfLength;
                if (ahead > -1 && ahead < 23) danger[obstacle.lane] += 100 / Mathf.Max(1, ahead + 2);
            }
            int best = rider.Lane;
            float bestScore = float.PositiveInfinity;
            for (int lane = 0; lane < 3; lane++)
            {
                float score = danger[lane] + Mathf.Abs(lane - rider.Lane) * .25f;
                foreach (var pickup in track.Live)
                    if (pickup && !pickup.consumed && pickup.kind == TrackKind.Pickup && pickup.lane == lane && pickup.transform.position.z > distance && pickup.transform.position.z < distance + 22) score -= .8f;
                if (score < bestScore) { bestScore = score; best = lane; }
            }
            return best < rider.Lane ? RiderInput.Action.Left : best > rider.Lane ? RiderInput.Action.Right : RiderInput.Action.None;
        }

        IEnumerator MechanicsWalkthrough()
        {
            BurgerPrices.UnlockAllForReview();
            PlayerProfile.Burgers = 4; PlayerProfile.Character = 0; DashRun.District = district = 2;
            PlayerPrefs.SetInt("coached_runs", 3);
            yield return new WaitForSecondsRealtime(3);
            var gm = GameManager.Instance; var menu = MenuUI.Instance;
            typeof(MenuUI).GetMethod("ExitCity", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(menu, null);
            menu.Open("none");
            var track = FindFirstObjectByType<TrackSpawner>(); var rider = FindFirstObjectByType<RiderController>();
            var canvasObject = new GameObject("Mechanics explanations", typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasObject.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 100;
            var scaler = canvasObject.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = .5f;
            var root = SafeArea.Create(canvasObject.transform);
            var card = UiKit.Panel(root, "Mechanics caption", new Vector2(0, -385), new Vector2(980, 260), UiKit.Navy);
            guideTitle = UiKit.Label(card.transform, "", 36, new Vector2(0, 86), new Vector2(930, 50), UiKit.Yellow);
            guideBody = UiKit.Label(card.transform, "", 30, new Vector2(0, 0), new Vector2(920, 130), Color.white);
            guideMode = UiKit.Label(card.transform, "", 22, new Vector2(0, -105), new Vector2(920, 36), UiKit.Muted);
            UnityEngine.Random.InitState(7423);
            RiderInput.Override = () => GuideInput(track, rider);
            gm.StartRun(); gm.SetPaused(true);
            yield return null;
            if (!walkthrough)
            {
                filmSeconds = 150; PrepareFilmCamera(); SaveFilmMusic(); StartFilmEncoder();
                Time.captureFramerate = CaptureRate; Application.targetFrameRate = CaptureRate; recording = true;
            }
            else if (recording)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = filmCamera; canvas.planeDistance = .5f;
            }
            Guide("CHRISTMAS DELIVERY", "Swipe or use arrows to steer. Up jumps, Down slides. Q activates Delivery Dash. The run is endless: deliveries every 450 metres, with a bonus for arriving on time.");
            yield return Shot("festival-controls"); yield return GuideHold(9);
            Guide("COLLECT BURGERS", "This demo starts with 4 burgers. Every burger you ride through goes into your wallet. Burgers are the only currency.");
            gm.SetPaused(false); guideMode.text = "LIVE GAMEPLAY";
            float timeout = Time.realtimeSinceStartup + 180;
            while (gm.Distance < 155 && Time.realtimeSinceStartup < timeout) { Check(gm.State == RunState.Running, "guide run remains alive"); yield return null; }
            Check(gm.Distance >= 155 && PlayerProfile.Burgers > 4, "actual collected burgers increase the saved burger wallet");
            gm.SetPaused(true);
            Guide("FUEL AND ABILITIES", "Riding drains the e-bike battery. On an empty tank the rider slows, and after six dry seconds away from a kiosk the run ends. The character ability meter recharges separately and does not power your vehicle.");
            yield return Shot("food-and-fuel-hud"); yield return GuideHold(9);
            Guide("APPROACH THE KIOSK", "Burger bar and battery charging kiosks sit beside district routes. Solo charging becomes available within 12 metres of a station.");
            yield return GuideHold(8);
            gm.SetPaused(false); guideMode.text = "LIVE GAMEPLAY";
            while ((!FuelStops.Instance.NearStation || rider.Airborne) && Time.realtimeSinceStartup < timeout) { Check(gm.State == RunState.Running, "guide reaches kiosk alive"); yield return null; }
            Check(FuelStops.Instance.NearStation, "real movement enters the kiosk window");
            gm.SetPaused(true); yield return null;
            int kioskIndex = FuelStops.Instance.StationIndex;
            KioskCamera(true, kioskIndex);
            yield return GuideHold(1.2f);
            Guide("THE BURGER & BATTERY KIOSK", "This is the kiosk: the burger counter on one side, the battery charger on the other, on a lit pad beside the lanes. One stands every 480 metres in every district.");
            yield return Shot("kiosk-close-up"); yield return GuideHold(9);
            Guide("TAP CHARGE OR PRESS F", "While riding near a kiosk, tap CHARGE or press F. The button shows the exact burger cost. One burger buys up to 25 percent charge.");
            yield return Shot("kiosk-activation-prompt"); yield return GuideHold(11);
            int wallet = PlayerProfile.Burgers;
            float beforeFuel = RidersEconomy.Fuel, beforeClock = DashRun.Instance.DeliveryTime, beforeDistance = gm.Distance;
            var plan = RidersEconomy.PitPlan(1 - beforeFuel, wallet);
            gm.SetPaused(false); yield return null;
            Button charge = null;
            foreach (var button in FindObjectsByType<Button>(FindObjectsSortMode.None))
                if (button.name == "CHARGE AT KIOSK Button" && button.gameObject.activeInHierarchy && button.interactable) charge = button;
            Check(charge, "visible enabled charge control exists"); charge.onClick.Invoke();
            Check(FuelStops.Instance.Pitting && PlayerProfile.Burgers == wallet - plan.cost, "HUD charge control spends the displayed burgers and starts charging");
            Guide("CHARGING IS A PIT STOP", "Movement stops while the tank fills. The on-time delivery window keeps counting down, so stopping costs bonus time.");
            while (FuelStops.Instance.Pitting) yield return null;
            Check(RidersEconomy.Fuel > beforeFuel && DashRun.Instance.DeliveryTime < beforeClock, "charging adds fuel while delivery time decreases");
            Check(gm.Distance - beforeDistance < 3, "pit stop freezes forward movement");
            gm.SetPaused(true);
            guideMode.text = "WALKTHROUGH PAUSED / CHARGE COMPLETE";
            yield return GuideHold(7);
            KioskCamera(false, kioskIndex);
            Guide("CHARGE COMPLETE", "The tank is refilled and the burgers are deducted. Riding resumes automatically after charging; this guide is paused to show the result.");
            yield return Shot("charging-complete"); yield return GuideHold(10);
            Guide("NO BURGERS? EMERGENCY CHARGE", "With no burgers, each kiosk offers one small free emergency charge. Collect more burgers for a full refill.");
            yield return GuideHold(8);
            yield return GuideHold(11);
            if (!walkthrough) { recording = false; FinishFilmEncoder(); }
            File.WriteAllText(Path.Combine(output, "mechanics-chapters.json"), JsonUtility.ToJson(new GuideChapters { chapters = guideChapters.ToArray() }, true));
            File.WriteAllText(Path.Combine(output, "mechanics-pass.txt"), $"PASS {assertions} assertions; real burger pickups, visible kiosk button, burger spend, fuel increase, stopped movement and continuing delivery deadline. Temporary 4-burger demo wallet, normal movement, labeled explanatory pauses. Frames={filmFrame}; duration={filmFrame / (float)CaptureRate:F2}s.\n");
            Debug.Log("MECHANICS_WALKTHROUGH_PASS");
            if (walkthrough)
            {
                Destroy(canvasObject); guideTitle = guideBody = guideMode = null;
                RiderInput.Override = null; gm.ToMenu(); menu.Open("none"); yield break;
            }
            Restore(); Application.Quit(0);
        }
    }
}

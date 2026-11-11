using DallasDash.Core;
using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DallasDash.UI
{
    /// Graphical in-run HUD: every value is an icon plus a gauge or a short number.
    /// Top-left: score, multiplier, combo and power-up timers.
    /// Top-right: coupon progress and the burger wallet. Top-centre: the police chase meter.
    /// Bottom: dash and Ace's ability (and the bike distance while on foot). A chip announces the coupon.
    public class DashHud : MonoBehaviour
    {
        GameObject root;
        Text deliveryNo, deliveryDistance, clock, battery, abilityLabel, move, routeText, equipment, foodText, fuelText;
                Image routeChip, moveIcon, equipmentIcon;
        Image fuelFill, deliveryBar, batteryFill;
        Button ability, dash;
        GameObject movePill;
        Image equipmentPanel;
        int lastZone = -1;
        RiderController rider;
        TrackSpawner track;

        /// Which way to steer for a lane, from the rider's lane.
        string LaneArrow(int lane)
        {
            int at = rider ? rider.Lane : 1;
            return lane < at ? "<<" : lane > at ? ">>" : "AHEAD";
        }

        static void Pin(RectTransform rt, Vector2 corner, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = offset;
        }

        void Start()
        {
            var canvasGo = new GameObject("Dash HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 8;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            root = SafeArea.Create(canvasGo.transform).gameObject;

            // ---------- Top right: coupon progress and the burger wallet
            var fuelPill = UiKit.Panel(root.transform, "Coupon", Vector2.zero, new Vector2(230, 72), new Color(.035f, .05f, .1f, .92f));
            fuelPill.raycastTarget = false;
            Pin(fuelPill.rectTransform, new Vector2(1, 1), new Vector2(-30, -36));
            UiKit.Icon(fuelPill.transform, "star", new Vector2(-78, 2), 44);
            fuelText = UiKit.Label(fuelPill.transform, "0%", 30, new Vector2(24, 10), new Vector2(118, 34), Color.white, TextAnchor.MiddleRight);
            var fuelTrack = UiKit.Panel(fuelPill.transform, "Track", new Vector2(24, -18), new Vector2(118, 8), new Color(1, 1, 1, .16f));
            fuelTrack.raycastTarget = false;
            fuelFill = UiKit.Panel(fuelTrack.transform, "Fill", Vector2.zero, new Vector2(118, 8), UiKit.Gold);
            fuelFill.raycastTarget = false;
            fuelFill.rectTransform.pivot = new Vector2(0, .5f);
            fuelFill.rectTransform.anchoredPosition = new Vector2(-59, 0);

            var foodPill = UiKit.Panel(root.transform, "Food", Vector2.zero, new Vector2(230, 72), new Color(.035f, .05f, .1f, .92f));
            foodPill.raycastTarget = false;
            Pin(foodPill.rectTransform, new Vector2(1, 1), new Vector2(-30, -116));
            UiKit.Icon(foodPill.transform, "burger", new Vector2(-78, 2), 48);
            foodText = UiKit.Label(foodPill.transform, "0", 34, new Vector2(24, 0), new Vector2(130, 52), Color.white, TextAnchor.MiddleRight);

            // ---------- Top centre: delivery clock, target and progress
            var panel = UiKit.Panel(root.transform, "Delivery", new Vector2(0, 700), new Vector2(560, 150), UiKit.Navy);
            panel.raycastTarget = false;
            var rim = UiKit.Panel(panel.transform, "Rim", Vector2.zero, new Vector2(566, 156), new Color(.22f, .3f, .45f, .55f));
            rim.raycastTarget = false;
            rim.transform.SetAsFirstSibling();
            var face = UiKit.Panel(panel.transform, "Face", Vector2.zero, new Vector2(560, 150), new Color(.045f, .06f, .12f, 1f));
            face.sprite = UiKit.Gradient2D;
            face.raycastTarget = false;

            // The chase: a siren icon, how far back the officer is, and a gauge that fills red as he closes in.
            UiKit.Icon(panel.transform, "shield", new Vector2(-242, 30), 52);
            deliveryNo = UiKit.Label(panel.transform, "POLICE", 38, new Vector2(-120, 30), new Vector2(200, 54), Color.white, TextAnchor.MiddleLeft);
            UiKit.Icon(panel.transform, "flag", new Vector2(116, 28), 44);
            deliveryDistance = UiKit.Label(panel.transform, "0 m", 34, new Vector2(200, 28), new Vector2(112, 54), UiKit.Cream, TextAnchor.MiddleLeft);
            clock = UiKit.Label(panel.transform, "", 30, new Vector2(6, 28), new Vector2(150, 54), UiKit.Cream, TextAnchor.MiddleLeft);
            clock.gameObject.SetActive(false);

            var track = UiKit.Panel(panel.transform, "Track", new Vector2(0, -48), new Vector2(500, 9), new Color(1, 1, 1, .14f));
            track.raycastTarget = false;
            deliveryBar = UiKit.Panel(track.transform, "Heat", Vector2.zero, new Vector2(500, 9), UiKit.Red);
            deliveryBar.raycastTarget = false;
            deliveryBar.rectTransform.pivot = new Vector2(0, .5f);
            deliveryBar.rectTransform.anchoredPosition = new Vector2(-250, 0);

            // ---------- Route shortcut chip: right column, under the fuel and food pills
            routeChip = UiKit.Panel(root.transform, "Route chip", Vector2.zero, new Vector2(450, 52), UiKit.Surface);
            routeChip.sprite = UiKit.Rounded;
            routeChip.raycastTarget = false;
            Pin(routeChip.rectTransform, new Vector2(1, 1), new Vector2(-30, -196));
            UiKit.Icon(routeChip.transform, "rocket", new Vector2(-188, 0), 34);
            routeText = UiKit.Label(routeChip.transform, "", 28, new Vector2(18, 0), new Vector2(380, 46), Color.white);
            routeChip.gameObject.SetActive(false);

            // ---------- Movement hint: icon + two or three words
            var hint = UiKit.Panel(root.transform, "Move hint", new Vector2(0, 498), new Vector2(320, 46), new Color(.035f, .05f, .1f, .8f));
            hint.raycastTarget = false;
            movePill = hint.gameObject;
            moveIcon = UiKit.Icon(hint.transform, "arrow", new Vector2(-128, 0), 30);
            move = UiKit.Label(hint.transform, "", 28, new Vector2(6, 0), new Vector2(250, 40), UiKit.Cream);
            movePill.SetActive(false);

            // ---------- Bottom left: ride / equipment
            equipmentPanel = UiKit.Panel(root.transform, "Equipment", new Vector2(-380, -882), new Vector2(250, 96), new Color(.06f, .12f, .2f, .92f));
            equipmentPanel.raycastTarget = false;
            equipmentIcon = UiKit.Icon(equipmentPanel.transform, "bike", new Vector2(-80, 0), 52);
            equipment = UiKit.Label(equipmentPanel.transform, "E-BIKE", 28, new Vector2(30, 0), new Vector2(160, 72), UiKit.Cream);

            // ---------- Bottom right: crew ability with a charge gauge
            // A round boost button at the right thumb (target look, 11 Nov 2026), with a cyan rim and a charge bar.
            ability = UiKit.Button(root.transform, "", new Vector2(400, -850), new Vector2(200, 200), new Color(.04f, .12f, .2f), () => DashRun.Instance?.TryAbility(), 24);
            var abilityFace = ability.transform.Find("Face");
            foreach (var part in ability.GetComponentsInChildren<Image>()) if (part.name is "Face" or "Rim") part.sprite = UiKit.Disc;
            var gloss = abilityFace.Find("Gloss"); if (gloss) gloss.gameObject.SetActive(false);
            var ring = UiKit.Panel(ability.transform, "Boost ring", Vector2.zero, new Vector2(212, 212), UiKit.Cyan);
            ring.sprite = UiKit.Disc; ring.raycastTarget = false; ring.transform.SetSiblingIndex(1);
            UiKit.Icon(abilityFace, "bolt", new Vector2(0, 42), 58);
            abilityLabel = UiKit.Label(abilityFace, "", 24, new Vector2(0, -6), new Vector2(170, 34), Color.white);
            var batteryTrack = UiKit.Panel(abilityFace, "Track", new Vector2(0, -42), new Vector2(110, 10), new Color(1, 1, 1, .16f));
            batteryTrack.raycastTarget = false;
            batteryFill = UiKit.Panel(batteryTrack.transform, "Fill", Vector2.zero, new Vector2(110, 10), UiKit.Green);
            batteryFill.raycastTarget = false;
            batteryFill.rectTransform.pivot = new Vector2(0, .5f);
            batteryFill.rectTransform.anchoredPosition = new Vector2(-55, 0);
            battery = UiKit.Label(abilityFace, "", 22, new Vector2(0, -70), new Vector2(160, 28), UiKit.Green);

            // ---------- Bottom centre: dash
            dash = UiKit.Button(root.transform, "DASH", new Vector2(0, -882), new Vector2(300, 96), UiKit.Pink, () => DashRun.Instance?.TryDeliveryDash(), 34, "rocket");
            var dashPulse = dash.transform.Find("Face").gameObject.AddComponent<UiPulse>();
            dashPulse.scale = 0.008f; dashPulse.speed = 2f;

            root.SetActive(false);
        }

        void Update()
        {
            if (!root || !DashRun.Instance || !GameManager.Instance) return;
            var gm = GameManager.Instance; var run = DashRun.Instance;
            bool running = gm.State == RunState.Running;
            root.SetActive(running);
            if (!running) { lastZone = -1; return; }

            // Coupon progress and the burger wallet.
            float progress = Mathf.Clamp01(PlayerProfile.CouponProgress / (float)BurgerPrices.CouponThreshold);
            fuelText.text = Mathf.FloorToInt(progress * 100f) + "%";
            fuelText.color = progress >= 1f ? UiKit.Gold : Color.white;
            fuelFill.rectTransform.localScale = new Vector3(progress, 1, 1);
            foodText.text = RidersEconomy.Burgers.ToString("N0");

            // Ability: name, full gauge, READY when charged.
            abilityLabel.text = DashRun.AbilityNames[0];
            batteryFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(run.Battery), 1, 1);
            batteryFill.color = run.Battery >= .999f ? UiKit.Green : UiKit.Cyan;
            battery.text = run.Battery >= .999f ? "READY" : "";
            ability.interactable = run.Battery >= .999f && !gm.Paused;

            dash.interactable = run.DashCooldown <= 0f && !gm.Paused;
            dash.GetComponentInChildren<Text>().text = run.DashTime > 0f ? "DASHING" : run.DashCooldown > 0f ? $"{run.DashCooldown:0.0}s" : "DASH";

            // On the bike there is nothing to tell the player; the pill comes up only while running on foot.
            equipmentPanel.gameObject.SetActive(run.OnFoot);
            equipmentIcon.sprite = Icons.Get(run.OnFoot ? "rider" : "bike");
            equipment.text = run.OnFoot ? (run.BikeAt > gm.Distance ? $"BIKE {run.BikeAt - gm.Distance:0} m" : "ON FOOT") : "E-BIKE";

            // The chase meter: the officer's distance and how hot the pursuit is.
            {
                float heat = run.Heat;
                deliveryNo.text = heat > 0.75f ? "COP CLOSE!" : "POLICE";
                deliveryNo.color = heat > 0.75f ? UiKit.Red : Color.white;
                deliveryDistance.text = $"{run.PoliceGap:0} m";
                deliveryBar.rectTransform.localScale = new Vector3(Mathf.Max(0.02f, heat), 1, 1);
                deliveryBar.color = heat > 0.75f ? (Mathf.Repeat(Time.unscaledTime * 4f, 1f) < 0.5f ? UiKit.Red : UiKit.Yellow) : heat > 0.45f ? UiKit.Orange : UiKit.Red;
            }

            // 1 while wall running; the wall can be run anywhere, so there is no zone to announce.
            int zone = run.WallRunning ? 1 : 0;
            if (!rider) rider = FindFirstObjectByType<RiderController>();
            if (!track) track = FindFirstObjectByType<TrackSpawner>();
            bool onRoute = false;
            var couponAhead = track ? track.Coupon : null;
            bool routeAhead = false;
            if (couponAhead && couponAhead.transform.position.z - gm.Distance < 160f)
            {
                routeAhead = true;
                routeChip.color = UiKit.Gold;
                routeText.color = UiKit.Ink;
                routeText.text = "10% COUPON  " + LaneArrow(couponAhead.lane) + "  " + Mathf.Max(0f, couponAhead.transform.position.z - gm.Distance).ToString("0") + " m";
            }
            routeChip.gameObject.SetActive(routeAhead);

            float edge = float.PositiveInfinity;
            string hintText; float iconAngle = 0f;
            if (onRoute && edge < gm.Speed * 0.9f) { hintText = "JUMP"; iconAngle = 90f; }
            else if (routeAhead) hintText = "";
            else if (zone == 1) { hintText = "WALL RUN"; iconAngle = 90f; }
            // On foot (or Bolt) in an outer lane beside a facade: show that steering into it starts a wall run.
            else if (DashRun.CanRideWalls && rider && rider.Traversal.Surface == RunSurface.Road && (rider.Lane == 0 || rider.Lane == Tuning.Lanes - 1)
                && DashRun.WallAvailable(rider.Lane == 0 ? -1 : 1, gm.Distance + 8f))
            { hintText = rider.Lane == 0 ? "< WALL RUN" : "WALL RUN >"; iconAngle = rider.Lane == 0 ? 180f : 0f; }
            else if (run.TrickCombo >= 2) { hintText = "TRICK x" + run.TrickCombo; iconAngle = 0f; }
            else hintText = "";
            movePill.SetActive(!string.IsNullOrEmpty(hintText));
            move.text = hintText;
            if (moveIcon) moveIcon.rectTransform.localRotation = Quaternion.Euler(0, 0, iconAngle);

            if (lastZone != zone && zone > 0) Haptics.Play(Haptics.Strength.Light);
            lastZone = zone;
        }
    }
}

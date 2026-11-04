using DallasDash.Core;
using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

namespace DallasDash.UI
{
    /// Graphical in-run HUD: every value is an icon plus a gauge or a short number.
    /// Top-left: score, multiplier, combo and power-up timers.
    /// Top-right: charge gauge and the burger wallet. Top-centre: delivery clock and progress.
    /// Bottom: equipment, dash, crew ability and the charging kiosk control.
    public class DashHud : MonoBehaviour
    {
        GameObject root;
        Text deliveryNo, deliveryDistance, clock, battery, abilityLabel, move, routeText, equipment, foodText, fuelText;
        Text chargeHintText, chargeText;
        Image routeChip, moveIcon, equipmentIcon, chargeHintIcon;
        Image fuelFill, deliveryBar, batteryFill, chargeFill;
        Button ability, dash, charge;
        GameObject movePill, chargeHintPill;
        int lastZone = -1;
        RiderController rider;

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

            // ---------- Top right: fuel gauge and food wallet
            var fuelPill = UiKit.Panel(root.transform, "Fuel", Vector2.zero, new Vector2(230, 72), new Color(.035f, .05f, .1f, .92f));
            fuelPill.raycastTarget = false;
            Pin(fuelPill.rectTransform, new Vector2(1, 1), new Vector2(-30, -36));
            UiKit.Icon(fuelPill.transform, "fuel", new Vector2(-78, 2), 44);
            fuelText = UiKit.Label(fuelPill.transform, "100", 32, new Vector2(24, 10), new Vector2(104, 34), Color.white, TextAnchor.MiddleRight);
            var fuelTrack = UiKit.Panel(fuelPill.transform, "Track", new Vector2(24, -18), new Vector2(118, 8), new Color(1, 1, 1, .16f));
            fuelTrack.raycastTarget = false;
            fuelFill = UiKit.Panel(fuelTrack.transform, "Fill", Vector2.zero, new Vector2(118, 8), UiKit.Green);
            fuelFill.raycastTarget = false;
            fuelFill.rectTransform.pivot = new Vector2(0, .5f);
            fuelFill.rectTransform.anchoredPosition = new Vector2(-59, 0);

            var foodPill = UiKit.Panel(root.transform, "Food", Vector2.zero, new Vector2(230, 72), new Color(.035f, .05f, .1f, .92f));
            foodPill.raycastTarget = false;
            Pin(foodPill.rectTransform, new Vector2(1, 1), new Vector2(-30, -116));
            UiKit.Icon(foodPill.transform, "burger", new Vector2(-78, 2), 48);
            foodText = UiKit.Label(foodPill.transform, "0", 40, new Vector2(24, 0), new Vector2(104, 52), Color.white, TextAnchor.MiddleRight);

            // ---------- Top centre: delivery clock, target and progress
            var panel = UiKit.Panel(root.transform, "Delivery", new Vector2(0, 700), new Vector2(560, 150), UiKit.Navy);
            panel.raycastTarget = false;
            var rim = UiKit.Panel(panel.transform, "Rim", Vector2.zero, new Vector2(566, 156), new Color(.22f, .3f, .45f, .55f));
            rim.raycastTarget = false;
            rim.transform.SetAsFirstSibling();
            var face = UiKit.Panel(panel.transform, "Face", Vector2.zero, new Vector2(560, 150), new Color(.045f, .06f, .12f, 1f));
            face.sprite = UiKit.Gradient2D;
            face.raycastTarget = false;

            // Each value sits in a fixed column after its icon, so the three rows line up.
            UiKit.Icon(panel.transform, "package", new Vector2(-242, 30), 52);
            deliveryNo = UiKit.Label(panel.transform, "01", 42, new Vector2(-170, 30), new Vector2(78, 54), Color.white, TextAnchor.MiddleLeft);
            UiKit.Icon(panel.transform, "clock", new Vector2(-84, 28), 44);
            clock = UiKit.Label(panel.transform, "0.0s", 36, new Vector2(2, 28), new Vector2(116, 54), Color.white, TextAnchor.MiddleLeft);
            UiKit.Icon(panel.transform, "flag", new Vector2(116, 28), 44);
            deliveryDistance = UiKit.Label(panel.transform, "0 m", 34, new Vector2(200, 28), new Vector2(112, 54), UiKit.Cream, TextAnchor.MiddleLeft);

            var track = UiKit.Panel(panel.transform, "Track", new Vector2(0, -48), new Vector2(500, 9), new Color(1, 1, 1, .14f));
            track.raycastTarget = false;
            deliveryBar = UiKit.Panel(track.transform, "Progress", Vector2.zero, new Vector2(500, 9), UiKit.Pink);
            deliveryBar.raycastTarget = false;
            deliveryBar.rectTransform.pivot = new Vector2(0, .5f);
            deliveryBar.rectTransform.anchoredPosition = new Vector2(-250, 0);

            // ---------- Route shortcut chip: right column, under the fuel and food pills
            routeChip = UiKit.Panel(root.transform, "Route chip", Vector2.zero, new Vector2(450, 52), UiKit.Surface);
            routeChip.sprite = UiKit.Rounded;
            routeChip.raycastTarget = false;
            Pin(routeChip.rectTransform, new Vector2(1, 1), new Vector2(-30, -196));
            UiKit.Icon(routeChip.transform, "rocket", new Vector2(-188, 0), 34);
            routeText = UiKit.Label(routeChip.transform, "", 25, new Vector2(18, 0), new Vector2(380, 46), Color.white);
            routeChip.gameObject.SetActive(false);

            // ---------- Movement hint: icon + two or three words
            var hint = UiKit.Panel(root.transform, "Move hint", new Vector2(0, 498), new Vector2(320, 46), new Color(.035f, .05f, .1f, .8f));
            hint.raycastTarget = false;
            movePill = hint.gameObject;
            moveIcon = UiKit.Icon(hint.transform, "arrow", new Vector2(-128, 0), 30);
            move = UiKit.Label(hint.transform, "", 24, new Vector2(6, 0), new Vector2(250, 40), UiKit.Cream);
            movePill.SetActive(false);

            // ---------- Bottom left: ride / equipment
            var equipmentPanel = UiKit.Panel(root.transform, "Equipment", new Vector2(-380, -882), new Vector2(250, 96), new Color(.06f, .12f, .2f, .92f));
            equipmentPanel.raycastTarget = false;
            equipmentIcon = UiKit.Icon(equipmentPanel.transform, "bike", new Vector2(-80, 0), 52);
            equipment = UiKit.Label(equipmentPanel.transform, "E-BIKE", 24, new Vector2(30, 0), new Vector2(148, 72), UiKit.Cream);

            // ---------- Bottom right: crew ability with a charge gauge
            ability = UiKit.Button(root.transform, "", new Vector2(380, -882), new Vector2(250, 96), UiKit.Blue, () => DashRun.Instance?.TryAbility(), 24);
            var abilityFace = ability.transform.Find("Face");
            UiKit.Icon(abilityFace, "bolt", new Vector2(-78, 8), 44);
            abilityLabel = UiKit.Label(abilityFace, "", 23, new Vector2(30, 22), new Vector2(140, 34), Color.white);
            var batteryTrack = UiKit.Panel(abilityFace, "Track", new Vector2(30, -18), new Vector2(140, 11), new Color(1, 1, 1, .16f));
            batteryTrack.raycastTarget = false;
            batteryFill = UiKit.Panel(batteryTrack.transform, "Fill", Vector2.zero, new Vector2(140, 11), UiKit.Green);
            batteryFill.raycastTarget = false;
            batteryFill.rectTransform.pivot = new Vector2(0, .5f);
            batteryFill.rectTransform.anchoredPosition = new Vector2(-70, 0);
            battery = UiKit.Label(abilityFace, "", 17, new Vector2(30, -42), new Vector2(140, 24), UiKit.Green);

            // ---------- Bottom centre: dash
            dash = UiKit.Button(root.transform, "DASH", new Vector2(0, -882), new Vector2(300, 96), UiKit.Pink, () => DashRun.Instance?.TryDeliveryDash(), 34, "rocket");
            var dashPulse = dash.transform.Find("Face").gameObject.AddComponent<UiPulse>();
            dashPulse.scale = 0.008f; dashPulse.speed = 2f;

            // ---------- Charging kiosk control
            charge = UiKit.Button(root.transform, "CHARGE AT KIOSK", new Vector2(0, -760), new Vector2(520, 70), UiKit.Yellow, ActivateCharge, 24, "kiosk");
            chargeText = charge.GetComponentInChildren<Text>();
            var chargeFace = charge.transform.Find("Face");
            var chargeTrack = UiKit.Panel(chargeFace, "Track", new Vector2(0, -25), new Vector2(460, 8), new Color(0, 0, 0, .22f));
            chargeTrack.raycastTarget = false;
            chargeFill = UiKit.Panel(chargeTrack.transform, "Fill", Vector2.zero, new Vector2(460, 8), Color.white);
            chargeFill.raycastTarget = false;
            chargeFill.rectTransform.pivot = new Vector2(0, .5f);
            chargeFill.rectTransform.anchoredPosition = new Vector2(-230, 0);

            var hintRow = UiKit.Panel(root.transform, "Charge hint", new Vector2(0, -690), new Vector2(430, 38), new Color(.035f, .05f, .1f, .8f));
            hintRow.raycastTarget = false;
            chargeHintPill = hintRow.gameObject;
            chargeHintIcon = UiKit.Icon(hintRow.transform, "burger", new Vector2(-118, 0), 26);
            chargeHintText = UiKit.Label(hintRow.transform, "", 22, new Vector2(43, 0), new Vector2(280, 34), UiKit.Cream, TextAnchor.MiddleLeft);
            chargeHintPill.SetActive(false);

            root.SetActive(false);
        }

        public void ActivateCharge()
        {
            if (!FuelStops.Instance) return;
            FuelStops.Instance.TryStartPit(out string message);
            MenuUI.Instance?.Shout(message);
        }

        void Update()
        {
            if (!root || !DashRun.Instance || !GameManager.Instance) return;
            var gm = GameManager.Instance; var run = DashRun.Instance;
            bool running = gm.State == RunState.Running;
            root.SetActive(running);
            if (!running) { lastZone = -1; return; }

            // Fuel and food, as gauges rather than a sentence.
            float fuel = RidersEconomy.FuelPercent;
            fuelText.text = Mathf.RoundToInt(fuel * 100f) + "%";
            fuelText.color = RidersEconomy.Low ? UiKit.Red : Color.white;
            fuelFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(fuel), 1, 1);
            fuelFill.color = RidersEconomy.Low ? UiKit.Red : UiKit.Green;
            foodText.text = RidersEconomy.Burgers.ToString();

            var stop = FuelStops.Instance;
            bool pitting = stop && stop.Pitting, near = stop && stop.NearStation;
            bool showCharge = near || pitting;
            charge.gameObject.SetActive(showCharge);
            chargeHintPill.SetActive(showCharge && !pitting);
            var plan = RidersEconomy.PitPlan(1 - RidersEconomy.Fuel, RidersEconomy.Burgers);
            charge.interactable = near && !pitting && !gm.Paused && RidersEconomy.Fuel < .99f && (plan.cost > 0 || stop.EmergencyAvailable);
            chargeText.text = pitting ? "CHARGING" : RidersEconomy.Fuel >= .99f ? "TANK FULL" : plan.cost > 0 ? "CHARGE AT KIOSK" : stop && stop.EmergencyAvailable ? "EMERGENCY CHARGE" : "CHARGE AT KIOSK";
            chargeFill.rectTransform.localScale = new Vector3(pitting ? stop.PitProgress : 0f, 1, 1);
            chargeFill.color = pitting ? UiKit.Yellow : Color.white;
            if (!pitting && near)
            {
                bool free = plan.cost <= 0 && stop.EmergencyAvailable;
                chargeHintIcon.gameObject.SetActive(plan.cost > 0);
                chargeHintText.text = (plan.cost > 0 ? plan.cost + (plan.cost == 1 ? " BURGER" : " BURGERS") : free ? "FREE" : "NO BURGERS") + "   ·   F";
            }
            if (charge.interactable && Keyboard.current?.fKey.wasPressedThisFrame == true) ActivateCharge();

            // Ability: name, full gauge, READY when charged.
            abilityLabel.text = DashRun.AbilityNames[Mathf.Clamp(PlayerProfile.Character, 0, 2)];
            batteryFill.rectTransform.localScale = new Vector3(Mathf.Clamp01(run.Battery), 1, 1);
            batteryFill.color = run.Battery >= .999f ? UiKit.Green : UiKit.Cyan;
            battery.text = run.Battery >= .999f ? "READY" : "";
            ability.interactable = run.Battery >= .999f && !gm.Paused;

            dash.interactable = run.DashCooldown <= 0f && !gm.Paused;
            dash.GetComponentInChildren<Text>().text = run.DashTime > 0f ? "DASHING" : run.DashCooldown > 0f ? $"{run.DashCooldown:0.0}s" : "DASH";

            equipmentIcon.sprite = Icons.Get(PlayerProfile.Character == 2 ? "wheel" : "bike");
            equipment.text = PlayerProfile.Character == 2 ? "MAGNETIC WHEEL"
                : run.OnFoot ? (run.BikeAt > gm.Distance ? $"BIKE {run.BikeAt - gm.Distance:0} m" : "ON FOOT")
                : "E-BIKE";

            float left = Mathf.Max(0f, (run.Deliveries + 1) * 450f - gm.Distance);
            deliveryNo.text = (run.Deliveries + 1).ToString("00");
            deliveryDistance.text = $"{left:0} m";
            // Endless run: the clock is the on-time bonus window, and a dry tank shows its countdown instead.
            if (run.DrySeconds > 0f) { clock.text = $"DRY {DashRun.DryLimit - run.DrySeconds:0.0}s"; clock.color = UiKit.Red; }
            else if (run.DeliveryTime > 0f) { clock.text = $"{run.DeliveryTime:0.0}s"; clock.color = run.DeliveryTime < 6f ? UiKit.Yellow : Color.white; }
            else { clock.text = "LATE"; clock.color = UiKit.Muted; }
            deliveryBar.rectTransform.localScale = new Vector3(run.DeliveryFraction, 1, 1);
            var barColor = deliveryBar.GetComponent<Image>();
            barColor.color = run.DeliveryTime <= 0f ? UiKit.Muted : run.DeliveryTime < 6f ? UiKit.Yellow : UiKit.Green;

            // 1 while wall running; the wall can be run anywhere, so there is no zone to announce.
            int zone = run.WallRunning ? 1 : 0;
            if (!rider) rider = FindFirstObjectByType<RiderController>();
            bool onRoute = run.OnRooftops;
            float roofStart = RooftopCourse.ZoneStartAt(gm.Distance);
            bool routeAhead = !onRoute && gm.Distance > roofStart - 70f && gm.Distance < roofStart + 1f && gm.Distance > RooftopCourse.Start - 70f;
            routeChip.gameObject.SetActive(onRoute || routeAhead);
            if (onRoute)
            {
                routeChip.color = UiKit.Purple;
                routeText.color = Color.white;
                routeText.text = "ROOFTOP   +" + Mathf.RoundToInt((DashRun.RouteSpeedFactor - 1f) * 100f) + "%"
                    + (run.Shortcuts > 1 ? "   x" + run.Shortcuts : "");
            }
            else if (routeAhead)
            {
                routeChip.color = UiKit.Yellow;
                routeText.color = UiKit.Ink;
                routeText.text = !DashRun.CanTakeRooftops ? "ROOFTOPS   /   BIKE ONLY"
                    : rider && rider.Lane == RooftopCourse.EntryLane ? "RAMP AHEAD   /   RIGHT LANE" : "ROOFTOP   /   RIGHT LANE RAMP";
            }

            float edge = onRoute && rider ? RooftopCourse.NextEdge(gm.Distance, Tuning.LaneX[rider.Lane]) : float.PositiveInfinity;
            string hintText; float iconAngle = 0f;
            if (onRoute && edge < gm.Speed * 0.9f) { hintText = "JUMP"; iconAngle = 90f; }
            else if (routeAhead) hintText = "";
            else if (zone == 1) { hintText = "WALL RUN"; iconAngle = 90f; }
            // On foot (or Bolt) in an outer lane beside a facade: show that steering into it starts a wall run.
            else if (DashRun.CanRideWalls && rider && rider.Traversal.Surface == RunSurface.Road && (rider.Lane == 0 || rider.Lane == Tuning.Lanes - 1)
                && DashRun.WallAvailable(rider.Lane == 0 ? -1 : 1, gm.Distance + 8f))
            { hintText = rider.Lane == 0 ? "< WALL RUN" : "WALL RUN >"; iconAngle = rider.Lane == 0 ? 180f : 0f; }
            else if (TrackSpawner.SkyparkVoid(gm.Distance + 10f)) { hintText = "JUMP"; iconAngle = 90f; }
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

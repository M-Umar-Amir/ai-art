using DallasDash.Core;
using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.UI
{
    public class DashHud : MonoBehaviour
    {
        GameObject root;
        Text delivery, clock, battery, abilityLabel, move;
        RectTransform deliveryBar;
        Button ability, board, dash;
        int lastZone = -1;
        void Start()
        {
            var canvasGo = new GameObject("Dash HUD", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 8;
            var scaler = canvasGo.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920); scaler.matchWidthOrHeight = 0.5f;
            root = SafeArea.Create(canvasGo.transform).gameObject;

            // Score and multiplier stay in the Hud pills; this layer owns the delivery clock and the
            // run controls, so there is only one of each on screen.

            // Enhanced delivery panel with better visual hierarchy
            var panel = UiKit.Panel(root.transform, "Delivery", new Vector2(0, 730), new Vector2(940, 128), UiKit.Navy);
            clock = UiKit.Label(panel.transform,"",64,new Vector2(-310,8),new Vector2(260,80),Color.white,TextAnchor.MiddleLeft);
            delivery = UiKit.Label(panel.transform, "", 28, new Vector2(150, 8), new Vector2(570, 76), Color.white,TextAnchor.MiddleLeft);
            var track = UiKit.Panel(panel.transform, "Track", new Vector2(0, -49), new Vector2(860, 4), new Color(1, 1, 1, 0.14f));
            var fill = UiKit.Panel(track.transform, "Progress", new Vector2(-430, 0), new Vector2(860, 4), UiKit.Pink);
            deliveryBar = fill.rectTransform; deliveryBar.pivot = new Vector2(0, 0.5f);

            // Ability button with enhanced visual feedback
            ability = UiKit.Button(root.transform, "", new Vector2(340, -800), new Vector2(270, 112), UiKit.Blue, () => DashRun.Instance?.TryAbility(), 30);
            abilityLabel = UiKit.Label(ability.transform.Find("Face"), "", 29, new Vector2(0, 17), new Vector2(260, 48), Color.white);
            battery = UiKit.Label(ability.transform.Find("Face"), "", 22, new Vector2(0, -23), new Vector2(260, 36), UiKit.Green);

            // Board button
            board = UiKit.Button(root.transform, "BOARD / 50", new Vector2(-340, -800), new Vector2(270, 112), UiKit.Blue, () => DashRun.Instance?.ToggleBoard(), 28);

            // Zone/move indicator
            move = UiKit.Label(root.transform, "", 27, new Vector2(0, 560), new Vector2(940, 65), UiKit.Cream);

            // Delivery dash button with pulse effect
            dash = UiKit.Button(root.transform, "DASH", new Vector2(0, -800), new Vector2(310, 112), UiKit.Pink, () => DashRun.Instance?.TryDeliveryDash(), 36);
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

            // Update score display
            // (Score and multiplier are drawn by Hud; this panel only adds the delivery clock.)

            abilityLabel.text = DashRun.AbilityNames[Mathf.Clamp(PlayerProfile.Character, 0, 2)];
            battery.text = run.Battery >= 0.999f ? "READY" : $"{Mathf.FloorToInt(run.Battery * 100f)}%";
            ability.interactable = run.Battery >= 0.999f && !gm.Paused;
            dash.interactable = run.DashCooldown <= 0f && !gm.Paused;
            dash.GetComponentInChildren<Text>().text = run.DashTime > 0f ? "DASHING" : run.DashCooldown > 0f ? $"{run.DashCooldown:0.0}s" : "DASH";
            board.interactable = !run.Board && PlayerProfile.Coins >= 50 && Missions.ScooterEarned && !gm.Paused;
            board.GetComponentInChildren<Text>().text = run.Board ? "PROTECTED" : Missions.ScooterEarned ? (PlayerProfile.Character==2 ? "STABILIZER / 50" : "BOARD / 50") : "BOARD LOCKED";
            float left = Mathf.Max(0f, (run.Deliveries + 1) * 450f - gm.Distance);
            // Enhanced time display with urgency indicators
            clock.text = $"{run.DeliveryTime:0.0}s";
            clock.color = run.DeliveryTime < 6f ? UiKit.Red : run.DeliveryTime < 12f ? UiKit.Yellow : Color.white;
            string districtLine = DashRun.District==1 ? "HIGH-RISE ROOFTOPS" : $"{DemoRewards.Ledger.badges} DELIVERY BADGES";
            delivery.text = $"DELIVERY {run.Deliveries + 1:00}     /     {left:0} m\n{districtLine}";
            deliveryBar.localScale = new Vector3(run.DeliveryFraction, 1, 1);
            // Bar color changes with urgency
            var barColor = deliveryBar.GetComponent<Image>();
            barColor.color = run.DeliveryTime < 6f ? UiKit.Red : run.DeliveryTime < 12f ? UiKit.Yellow : UiKit.Green;

            int zone = DashRun.GravityZone(gm.Distance) ? 2 : DashRun.WallZone(gm.Distance) ? 1 : 0;
            move.text = run.OnRoute
                ? (DashRun.ParkourZone(gm.Distance) ? "ROOFTOP LINE   /   " : "EXPRESS LINE   /   ") + Mathf.RoundToInt((DashRun.RouteSpeedFactor - 1f) * 100f) + "% FASTER"
                : DashRun.RouteOpen(gm.Distance) ? "SHORTCUT AHEAD / SWIPE RIGHT FOR THE FOURTH LANE"
                : zone == 2 ? "SWIPE UP / FLOOR > WALL > CEILING > WALL"
                : zone == 1 ? "EDGE LANE + UP / WALL RUN   |   UP / KICK OFF"
                : TrackSpawner.SkyparkVoid(gm.Distance+10f) ? "ROOF EDGE AHEAD   /   JUMP"
                : run.TrickCombo >= 2 ? "TRICK CHAIN x" + run.TrickCombo : "";
            if (lastZone != zone && zone > 0) Haptics.Play(Haptics.Strength.Light);
            lastZone = zone;
        }
    }
}

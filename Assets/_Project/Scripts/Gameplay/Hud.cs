using DallasDash.Core;
using DallasDash.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.Gameplay
{
    /// The HUD is a separate code layer over clean scenes, so numbers change without touching art.
    /// Burger count in a pill top-left, score in a pill top-right, the live multiplier as a badge under it.
    public class Hud : MonoBehaviour
    {
        public Text score;
        public Text pickups;
        public Text banner;
        RectTransform pickupPill, scorePill, badge;
        Text badgeText;
        int lastPickups, lastMult = 1;
        RectTransform[] boostPills;
        RectTransform[] boostBars;
        float[] boostPop;
        float pulse, badgePop;

        void Start()
        {
            var canvas = score.transform.parent;
            // The pills live in the safe area, clear of notches and rounded corners; the hit flash still fills the screen.
            var safe = SafeArea.Create(canvas);
            // The bootstrap's plain labels give way to the pills; keep the fields pointing at the live text.
            score.gameObject.SetActive(false);
            pickups.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);

            // === TOP SECTION ===
            // Left column: score and multiplier. The burger wallet (the only currency) is DashHud's right-hand
            // pill; this pill stays built for older call sites but is never shown, so there is one burger count.
            pickups = UiKit.Pill(safe, "burger", Vector2.zero, 230, Color.white, out pickupPill);
            pickups.alignment = TextAnchor.MiddleRight;
            Pin(pickupPill, new Vector2(0f, 1f), new Vector2(30f, -116f));

            // Score pill - top of the left column.
            score = UiKit.Pill(safe, "star", Vector2.zero, 230, Color.white, out scorePill);
            score.alignment = TextAnchor.MiddleRight;
            Pin(scorePill, new Vector2(0f, 1f), new Vector2(30f, -36f));

            // Multiplier badge - under the score, with a star marker so it reads without a sentence.
            var b = UiKit.Panel(safe, "Multiplier", Vector2.zero, new Vector2(120f, 72f), UiKit.Red);
            b.sprite = UiKit.Gradient;
            b.raycastTarget = false;
            badge = b.rectTransform;
            Pin(badge, new Vector2(0f, 1f), new Vector2(30f, -116f));
            UiKit.Icon(b.transform, "star", new Vector2(-32f, 0f), 34f);
            badgeText = UiKit.Label(b.transform, "", 40, new Vector2(18f, 2f), new Vector2(80f, 64f), Color.white);

            // Combo pill - same left stack, with a bar that fills as the chain grows.
            var cb = UiKit.Panel(safe, "Combo", Vector2.zero, new Vector2(230f, 82f), new Color(0.05f, 0.08f, 0.25f, 0.84f));
            cb.raycastTarget = false;
            comboPill = cb.rectTransform;
            Pin(comboPill, new Vector2(0f, 1f), new Vector2(30f, -282f));
            comboText = UiKit.Label(cb.transform, "", 36, new Vector2(0f, 14f), new Vector2(210f, 46f), UiKit.Yellow);
            var ctrack = UiKit.Panel(cb.transform, "Track", new Vector2(0f, -26f), new Vector2(180f, 11f), new Color(1f, 1f, 1f, 0.18f));
            ctrack.raycastTarget = false;
            var cfill = UiKit.Panel(ctrack.transform, "Fill", Vector2.zero, new Vector2(180f, 11f), UiKit.Yellow);
            cfill.raycastTarget = false;
            comboBar = cfill.rectTransform;
            comboBar.pivot = new Vector2(0f, 0.5f);
            comboBar.anchoredPosition = new Vector2(-90f, 0f);
            cb.gameObject.SetActive(false);
            GameManager.Instance.ComboMilestone += OnComboMilestone;

            // === LEFT SIDE - POWER-UPS ===
            // Active power-ups stack under the combo pill; equipment owns the bottom-left corner.
            boostPills = new RectTransform[PowerUps.Count];
            boostBars = new RectTransform[PowerUps.Count];
            boostPop = new float[PowerUps.Count];
            for (int i = 0; i < PowerUps.Count; i++)
            {
                var bg = UiKit.Panel(safe, PowerUps.Names[i], Vector2.zero, new Vector2(230f, 80f), new Color(0.05f, 0.08f, 0.25f, 0.8f));
                bg.raycastTarget = false;
                boostPills[i] = bg.rectTransform;
                UiKit.Icon(bg.transform, PowerUps.IconKeys[i], new Vector2(-72f, 2f), 78f);
                var track = UiKit.Panel(bg.transform, "Track", new Vector2(38f, 0f), new Vector2(130f, 22f), new Color(1f, 1f, 1f, 0.18f));
                track.raycastTarget = false;
                var fill = UiKit.Panel(track.transform, "Fill", Vector2.zero, new Vector2(130f, 22f), PowerUps.Colors[i]);
                fill.raycastTarget = false;
                boostBars[i] = fill.rectTransform;
                boostBars[i].pivot = new Vector2(0f, 0.5f);
                boostBars[i].anchoredPosition = new Vector2(-65f, 0f);
                bg.gameObject.SetActive(false);
            }
            PowerUps.Activated += OnPowerUp;

            flash = UiKit.Rect(canvas, "Hit flash", Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
            flash.sprite = EdgeVignette();
            flash.color = new Color(1f, 0.1f, 0.1f, 0f);
            flash.raycastTarget = false;
            flash.transform.SetAsFirstSibling();
            TrackSpawner.Struck += OnStruck;
        }

        Image flash;
        float flashT;
        RectTransform comboPill, comboBar;
        Text comboText;
        float comboPop;
        int lastCombo;

        void OnComboMilestone(int combo, int points) => comboPop = 1.6f;

        /// Clear in the middle, solid at the screen edges: the hit flash rims the view instead of tinting it.
        static Sprite EdgeVignette()
        {
            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = Mathf.Abs((x + 0.5f) / n * 2f - 1f), dy = Mathf.Abs((y + 0.5f) / n * 2f - 1f);
                float d = Mathf.Max(dx, dy * 0.9f);
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 1f, d));
                px[y * n + x] = new Color(1f, 1f, 1f, a);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
        }

        void OnStruck(TrackObject o) => flashT = 1f;

        void OnDestroy()
        {
            PowerUps.Activated -= OnPowerUp;
            TrackSpawner.Struck -= OnStruck;
            if (GameManager.Instance) GameManager.Instance.ComboMilestone -= OnComboMilestone;
        }

        void OnPowerUp(PowerUp p)
        {
            boostPop[(int)p] = 1f;
            MenuUI.Instance?.Shout(PowerUps.Names[(int)p] + "!");
        }

        static void Pin(RectTransform rt, Vector2 corner, Vector2 offset)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = corner;
            rt.anchoredPosition = offset;
        }

        void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null || !scorePill) return;
            score.text = gm.Score.ToString("N0");
            pickups.text = gm.BurgersCollected.ToString();
            // The burger pill pulses on each pickup; the badge pops whenever the multiplier steps up.
            if (gm.BurgersCollected != lastPickups) { pulse = 1f; lastPickups = gm.BurgersCollected; }
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 5f);
            pickupPill.localScale = Vector3.one * (1f + pulse * 0.12f);
            if (gm.Multiplier != lastMult) { badgePop = 1f; lastMult = gm.Multiplier; }
            badgePop = Mathf.MoveTowards(badgePop, 0f, Time.deltaTime * 3f);
            badge.localScale = Vector3.one * (1f + badgePop * badgePop * 0.5f);

            // Title, pause and busted screens live in MenuUI; the HUD only shows mid-run.
            bool running = gm.State == RunState.Running;
            pickupPill.gameObject.SetActive(false);
            scorePill.gameObject.SetActive(running);
            badge.gameObject.SetActive(running && gm.Multiplier > 1);
            badgeText.text = gm.ScoreFactor > gm.Multiplier ? $"x{gm.ScoreFactor}" : $"x{gm.Multiplier}";
            if (running && gm.ScoreFactor > 1) badge.gameObject.SetActive(true);

            bool combo = running && gm.Combo >= 3;
            comboPill.gameObject.SetActive(combo);
            if (combo)
            {
                if (gm.Combo != lastCombo) comboPop = Mathf.Max(comboPop, 0.5f);
                comboText.text = $"x{gm.Combo} COMBO";
                comboBar.localScale = new Vector3(gm.ComboFraction, 1f, 1f);
            }
            lastCombo = gm.Combo;
            comboPop = Mathf.MoveTowards(comboPop, 0f, Time.deltaTime * 4f);
            comboPill.localScale = Vector3.one * (1f + comboPop * 0.18f);

            flashT = Mathf.MoveTowards(flashT, 0f, Time.unscaledDeltaTime * 3f);
            flash.color = new Color(1f, 0.12f, 0.1f, flashT * (PlayerProfile.ReduceMotion ? 0.25f : 0.7f));

            // Power-ups stack down the right column, under the route chip and clear of the left status stack.
            float y = -280f;
            for (int i = 0; i < PowerUps.Count; i++)
            {
                bool on = running && PowerUps.Active((PowerUp)i);
                boostPills[i].gameObject.SetActive(on);
                if (!on) continue;
                Pin(boostPills[i], new Vector2(1f, 1f), new Vector2(-30f, y));
                y -= 92f;
                float frac = PowerUps.Fraction((PowerUp)i);
                boostBars[i].localScale = new Vector3(frac, 1f, 1f);
                boostPop[i] = Mathf.MoveTowards(boostPop[i], 0f, Time.deltaTime * 3f);
                bool ending = PowerUps.Remaining((PowerUp)i) < 2f && Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
                boostPills[i].localScale = Vector3.one * (1f + boostPop[i] * boostPop[i] * 0.35f) * (ending ? 0.94f : 1f);
            }
        }
    }
}

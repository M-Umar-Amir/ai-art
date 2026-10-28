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
            // The bootstrap's plain labels give way to the pills; keep the fields pointing at the live text.
            score.gameObject.SetActive(false);
            pickups.gameObject.SetActive(false);
            banner.gameObject.SetActive(false);
            pickups = UiKit.Pill(canvas, "burger", Vector2.zero, 300, Color.white, out pickupPill);
            score = UiKit.Pill(canvas, "star", Vector2.zero, 420, Color.white, out scorePill);
            score.alignment = TextAnchor.MiddleRight;
            score.rectTransform.anchoredPosition = new Vector2(10f, 2f);
            Pin(pickupPill, new Vector2(0f, 1f), new Vector2(30f, -50f));
            Pin(scorePill, new Vector2(1f, 1f), new Vector2(-30f, -50f));

            var b = UiKit.Panel(canvas, "Multiplier", Vector2.zero, new Vector2(150f, 84f), UiKit.Red);
            b.sprite = UiKit.Gradient;
            b.raycastTarget = false;
            badge = b.rectTransform;
            Pin(badge, new Vector2(1f, 1f), new Vector2(-36f, -166f));
            badgeText = UiKit.Label(b.transform, "", 56, new Vector2(0f, 2f), new Vector2(150f, 84f), Color.white);

            // Active power-ups: one pill each down the left side, its coloured bar draining as time runs out.
            boostPills = new RectTransform[PowerUps.Count];
            boostBars = new RectTransform[PowerUps.Count];
            boostPop = new float[PowerUps.Count];
            for (int i = 0; i < PowerUps.Count; i++)
            {
                var bg = UiKit.Panel(canvas, PowerUps.Names[i], Vector2.zero, new Vector2(250f, 88f), new Color(0.05f, 0.08f, 0.25f, 0.72f));
                bg.raycastTarget = false;
                boostPills[i] = bg.rectTransform;
                UiKit.Icon(bg.transform, PowerUps.IconKeys[i], new Vector2(-82f, 2f), 92f);
                var track = UiKit.Panel(bg.transform, "Track", new Vector2(40f, 0f), new Vector2(150f, 26f), new Color(1f, 1f, 1f, 0.18f));
                track.raycastTarget = false;
                var fill = UiKit.Panel(track.transform, "Fill", Vector2.zero, new Vector2(150f, 26f), PowerUps.Colors[i]);
                fill.raycastTarget = false;
                boostBars[i] = fill.rectTransform;
                boostBars[i].pivot = new Vector2(0f, 0.5f);
                boostBars[i].anchoredPosition = new Vector2(-75f, 0f);
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
            pickups.text = gm.PickupCount.ToString();
            // The burger pill pulses on each pickup; the badge pops whenever the multiplier steps up.
            if (gm.PickupCount != lastPickups) { pulse = 1f; lastPickups = gm.PickupCount; }
            pulse = Mathf.MoveTowards(pulse, 0f, Time.deltaTime * 5f);
            pickupPill.localScale = Vector3.one * (1f + pulse * 0.12f);
            if (gm.Multiplier != lastMult) { badgePop = 1f; lastMult = gm.Multiplier; }
            badgePop = Mathf.MoveTowards(badgePop, 0f, Time.deltaTime * 3f);
            badge.localScale = Vector3.one * (1f + badgePop * badgePop * 0.5f);

            // Title, pause and busted screens live in MenuUI; the HUD only shows mid-run.
            bool running = gm.State == RunState.Running;
            pickupPill.gameObject.SetActive(running);
            scorePill.gameObject.SetActive(running);
            badge.gameObject.SetActive(running && gm.Multiplier > 1);
            badgeText.text = gm.ScoreFactor > gm.Multiplier ? $"x{gm.ScoreFactor}" : $"x{gm.Multiplier}";
            if (running && gm.ScoreFactor > 1) badge.gameObject.SetActive(true);

            flashT = Mathf.MoveTowards(flashT, 0f, Time.unscaledDeltaTime * 3f);
            flash.color = new Color(1f, 0.12f, 0.1f, flashT * 0.7f);

            float y = -166f;
            for (int i = 0; i < PowerUps.Count; i++)
            {
                bool on = running && PowerUps.Active((PowerUp)i);
                boostPills[i].gameObject.SetActive(on);
                if (!on) continue;
                Pin(boostPills[i], new Vector2(0f, 1f), new Vector2(30f, y));
                y -= 100f;
                float frac = PowerUps.Fraction((PowerUp)i);
                boostBars[i].localScale = new Vector3(frac, 1f, 1f);
                boostPop[i] = Mathf.MoveTowards(boostPop[i], 0f, Time.deltaTime * 3f);
                // Pops in on pickup, and blinks for the last two seconds.
                bool ending = PowerUps.Remaining((PowerUp)i) < 2f && Mathf.Repeat(Time.time * 6f, 1f) < 0.5f;
                boostPills[i].localScale = Vector3.one * (1f + boostPop[i] * boostPop[i] * 0.35f) * (ending ? 0.94f : 1f);
            }
        }
    }
}

using DallasDash.Core;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.UI
{
    /// Run-time overlays for the client round of 4 Nov 2026: the burger revive offer, the portal teleport
    /// flash and the coupon-won card.
    public partial class MenuUI
    {
        GameObject revivePanel, couponPanel;
        /// The coupon ticket artwork (models/make_coupon.py), set by the bootstrap.
        public Texture2D couponArt;
        Text reviveTitle, reviveCost, reviveWallet, reviveHint, couponCode;
        Button reviveButton;
        Image reviveTimer, flash;
        float reviveLeft, flashTime, couponTime;
        Color flashColor;
        const float ReviveSeconds = 6f;

        void BuildChaseOverlays(Transform root)
        {
            // ---- Revive: one primary action (revive for burgers), one quiet way out, and a draining timer.
            revivePanel = UiKit.Shade(root, "Revive").gameObject;
            revivePanel.GetComponent<Image>().color = new Color(0.02f, 0.03f, 0.1f, 0.72f);
            var p = UiKit.FancyCard(revivePanel.transform, new Vector2(0, 40), new Vector2(920, 1000), UiKit.Red).transform;
            reviveTitle = UiKit.Label(p, "BUSTED!", 104, new Vector2(0, 360), new Vector2(880, 140), UiKit.Red, TextAnchor.MiddleCenter, true);
            UiKit.Label(p, "Pay burgers to shake the cop and keep riding", 36, new Vector2(0, 250), new Vector2(860, 60), UiKit.Cream);
            UiKit.Icon(p, "burger", new Vector2(0, 110), 170);
            reviveWallet = UiKit.Label(p, "", 38, new Vector2(0, -20), new Vector2(860, 56), Color.white);
            reviveButton = UiKit.Button(p, "REVIVE", new Vector2(0, -160), new Vector2(720, 170), UiKit.Green, AcceptRevive, 58, "play");
            reviveCost = reviveButton.GetComponentInChildren<Text>();
            reviveButton.transform.Find("Face").gameObject.AddComponent<UiPulse>();
            var track = UiKit.Panel(p, "Timer track", new Vector2(0, -280), new Vector2(720, 14), new Color(1, 1, 1, 0.15f));
            track.raycastTarget = false;
            reviveTimer = UiKit.Panel(track.transform, "Timer", Vector2.zero, new Vector2(720, 14), UiKit.Yellow);
            reviveTimer.raycastTarget = false;
            reviveTimer.rectTransform.pivot = new Vector2(0, 0.5f);
            reviveTimer.rectTransform.anchoredPosition = new Vector2(-360, 0);
            reviveHint = UiKit.Label(p, "", 32, new Vector2(0, -330), new Vector2(860, 50), UiKit.Muted);
            UiKit.Button(p, "NO THANKS", new Vector2(0, -420), new Vector2(440, 110), UiKit.Surface, DeclineRevive, 40);
            revivePanel.SetActive(false);

            // ---- Coupon won: the ticket itself pops in over the run, with a headline and the code under it.
            couponPanel = UiKit.Centered(root, "Coupon won", new Vector2(0, 250), new Vector2(900, 700)).gameObject;
            var cp = couponPanel.transform;
            var glow = UiKit.Centered(cp, "Glow", new Vector2(0, 30), new Vector2(1000, 640)).gameObject.AddComponent<Image>();
            glow.sprite = UiKit.Glow; glow.color = new Color(1f, 0.78f, 0.2f, 0.55f); glow.raycastTarget = false;
            UiKit.Label(cp, "COUPON WON!", 84, new Vector2(0, 305), new Vector2(900, 100), UiKit.Gold, TextAnchor.MiddleCenter, true);
            var ticketArt = UiKit.Centered(cp, "Ticket", new Vector2(0, 30), new Vector2(800, 450)).gameObject.AddComponent<RawImage>();
            ticketArt.texture = couponArt; ticketArt.raycastTarget = false;
            ticketArt.gameObject.AddComponent<UiPulse>().scale = 0.03f;
            var strip = UiKit.Panel(cp, "Code strip", new Vector2(0, -250), new Vector2(860, 110), new Color(0.04f, 0.05f, 0.12f, 0.92f));
            strip.sprite = UiKit.Rounded; strip.raycastTarget = false;
            couponCode = UiKit.Label(strip.transform, "", 24, new Vector2(0, 18), new Vector2(840, 40), Color.white);
            UiKit.Label(strip.transform, "Saved in REWARDS  /  valid 24 hours  /  one use", 26, new Vector2(0, -24), new Vector2(840, 40), UiKit.Cream);
            couponPanel.SetActive(false);

            // ---- Teleport flash: full-screen colour that fades as the new world appears.
            flash = UiKit.Panel(root, "Teleport flash", Vector2.zero, new Vector2(4000, 4000), Color.clear);
            flash.raycastTarget = false;
            flash.gameObject.SetActive(false);
        }

        /// Shows the revive offer; false when it can't be shown (the run then ends straight away).
        public bool OfferRevive(string reason, int cost)
        {
            if (!revivePanel) return false;
            pause.SetActive(false);
            reviveTitle.text = reason;
            bool afford = PlayerProfile.Burgers >= cost;
            reviveCost.text = afford ? $"REVIVE  /  {cost:N0}" : $"NEED {cost - PlayerProfile.Burgers:N0} MORE";
            reviveButton.interactable = afford;
            reviveWallet.text = $"You have {PlayerProfile.Burgers:N0} burgers";
            reviveHint.text = afford ? "Cop pushed back  /  back on the e-bike  /  road cleared" : "Collect more burgers on your runs to revive";
            reviveLeft = ReviveSeconds;
            revivePanel.SetActive(true);
            pauseButton.SetActive(false);
            return true;
        }

        void AcceptRevive()
        {
            if (!DashRun.Instance || !revivePanel.activeSelf) return;
            if (!DashRun.Instance.Revive()) { Toast("Not enough burgers"); return; }
            revivePanel.SetActive(false);
            pauseButton.SetActive(true);
        }

        void DeclineRevive()
        {
            if (!revivePanel.activeSelf) return;
            revivePanel.SetActive(false);
            DashRun.Instance?.FinishRun();
        }

        /// The portal flash, in the colour of the portal ridden through.
        public void Teleport(Color c)
        {
            if (!flash) return;
            flashColor = c;
            flashTime = 0.75f;
            flash.gameObject.SetActive(true);
            flash.color = c;
        }

        public void CouponWon(DemoCoupon coupon)
        {
            if (!couponPanel) return;
            couponCode.text = coupon != null ? coupon.code : "";
            captionTimer = 0f; captionTitle.text = captionSub.text = ""; popup.text = ""; // the card replaces "coupon ahead"
            couponTime = 3.2f;
            couponPanel.SetActive(true);
            Haptics.Play(Haptics.Strength.Heavy);
        }

        void TickChaseOverlays(float dt)
        {
            if (revivePanel && revivePanel.activeSelf)
            {
                reviveLeft -= dt;
                reviveTimer.rectTransform.localScale = new Vector3(Mathf.Clamp01(reviveLeft / ReviveSeconds), 1f, 1f);
                if (reviveLeft <= 0f) DeclineRevive();
                if (UnityEngine.InputSystem.Keyboard.current?.enterKey.wasPressedThisFrame == true) AcceptRevive();
            }
            if (flashTime > 0f)
            {
                flashTime -= dt;
                var c = flashColor; c.a = Mathf.Clamp01(flashTime / 0.75f) * 0.95f;
                flash.color = c;
                if (flashTime <= 0f) flash.gameObject.SetActive(false);
            }
            if (couponTime > 0f && (couponTime -= dt) <= 0f) couponPanel.SetActive(false);
        }
    }
}

using System;
using DallasDash.Core;
using DallasDash.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace DallasDash.UI
{
    public partial class MenuUI
    {
        GameObject districtPanel, rewardsPanel;
        Text districtChip, badgeBalance, rewardsHint, pageText;
        readonly Text[] districtButtons = new Text[3];
        readonly Text[] couponCodes = new Text[3], couponStates = new Text[3];
        readonly Button[] couponRedeem = new Button[3], couponCopy = new Button[3];
        readonly GameObject[] couponRows = new GameObject[3];
        int rewardsPage;
        float rewardsRefresh;
        void BuildDashMenus(Transform root)
        {
            UiKit.Label(title.transform, "FOOD INC  /  POWERED BY PAGE STUDIO INC.", 24, new Vector2(0, -925), new Vector2(1000, 45), UiKit.Cream);
            var city = UiKit.Button(title.transform, "", new Vector2(0, -490), new Vector2(790, 90), UiKit.Blue, ShowDistricts, 36);
            districtChip = city.GetComponentInChildren<Text>();
            districtChip.text = DashRun.DistrictNames[DashRun.District] + "  >";
            UiKit.Button(title.transform, "FOOD INC REWARDS", new Vector2(0, -825), new Vector2(650, 110), UiKit.Red, ShowRewards, 38, "star");

            districtPanel = UiKit.Shade(root, "Districts").gameObject;
            var p = UiKit.Card(districtPanel.transform, Vector2.zero, new Vector2(1000, 1580)).transform;
            UiKit.Label(p, "CHOOSE YOUR DISTRICT", 64, new Vector2(0, 670), new Vector2(900, 110), UiKit.Yellow);
            string[] blurbs = { "Holograms. Electric traffic. Street markets.\nWall-run cyan rails; flip through violet tunnels.", "Rooftop gardens above Dash City.\nJump the gaps and chase sky deliveries.", "Light installations, music and food stalls.\nSlide under stage gates; chase the beat." };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var card = UiKit.Panel(p, "District " + i, new Vector2(0, 410 - i * 420), new Vector2(900, 370), new Color(0.08f, 0.12f, 0.23f));
                UiKit.Label(card.transform, "0" + (i + 1) + "  " + DashRun.DistrictNames[i], 44, new Vector2(0, 118), new Vector2(850, 70), DashCity.Accent(i));
                UiKit.Label(card.transform, blurbs[i], 32, new Vector2(0, 20), new Vector2(850, 100), UiKit.Cream);
                districtButtons[i] = UiKit.Button(card.transform, "SELECT", new Vector2(0, -100), new Vector2(680, 100), UiKit.Blue, () => PickDistrict(index), 36).GetComponentInChildren<Text>();
            }
            UiKit.Button(p, "X", new Vector2(430, 700), new Vector2(100, 100), UiKit.Red, () => districtPanel.SetActive(false), 48);
            districtPanel.SetActive(false);

            rewardsPanel = UiKit.Shade(root, "FOOD INC demo wallet").gameObject;
            var r = UiKit.Card(rewardsPanel.transform, Vector2.zero, new Vector2(1020, 1740)).transform;
            UiKit.Label(r, "FOOD INC REWARDS", 64, new Vector2(0, 750), new Vector2(940, 110), UiKit.Yellow);
            UiKit.Label(r, "DEMO COUPONS  /  10% OFF A TEST ORDER", 32, new Vector2(0, 655), new Vector2(960, 60), UiKit.Cream);
            badgeBalance = UiKit.Label(r, "", 38, new Vector2(0, 575), new Vector2(900, 70), Color.white);
            UiKit.Button(r, "GENERATE  /  3 BADGES", new Vector2(0, 460), new Vector2(800, 120), UiKit.Green, GenerateCoupon, 42);
            UiKit.Label(r, "Deliver every 450 m to earn a badge.\nEach code expires in 24 hours and can be used once.", 30, new Vector2(0, 340), new Vector2(940, 100), UiKit.Cream);
            for (int i = 0; i < 3; i++)
            {
                int row = i;
                var card = UiKit.Panel(r, "Coupon " + i, new Vector2(0, 135 - i * 280), new Vector2(940, 260), new Color(0.08f, 0.12f, 0.23f));
                couponRows[i] = card.gameObject;
                couponCodes[i] = UiKit.Label(card.transform, "", 29, new Vector2(0, 65), new Vector2(900, 65), Color.white);
                couponStates[i] = UiKit.Label(card.transform, "", 28, new Vector2(0, 7), new Vector2(900, 50), UiKit.Yellow);
                couponCopy[i] = UiKit.Button(card.transform, "COPY", new Vector2(-250, -72), new Vector2(350, 110), UiKit.Blue, () => CopyCoupon(row), 32);
                couponRedeem[i] = UiKit.Button(card.transform, "TEST REDEEM", new Vector2(210, -72), new Vector2(470, 110), UiKit.Red, () => RedeemCoupon(row), 32);
            }
            rewardsHint = UiKit.Label(r, "", 30, new Vector2(0, -620), new Vector2(940, 70), UiKit.Cream);
            UiKit.Button(r, "<", new Vector2(-330, -720), new Vector2(150, 90), UiKit.Blue, () => { rewardsPage = Mathf.Max(0, rewardsPage - 1); RefreshRewards(); }, 42);
            UiKit.Button(r, ">", new Vector2(330, -720), new Vector2(150, 90), UiKit.Blue, () => { rewardsPage = Mathf.Min(Mathf.Max(0, (DemoRewards.Ledger.coupons.Count - 1) / 3), rewardsPage + 1); RefreshRewards(); }, 42);
            pageText = UiKit.Label(r, "", 32, new Vector2(0, -720), new Vector2(400, 70), Color.white);
            UiKit.Label(r, "Test redemption simulates FOOD INC on this device.\nThese codes do not apply to real food orders.", 27, new Vector2(0, -815), new Vector2(960, 70), UiKit.Cream);
            UiKit.Button(r, "X", new Vector2(450, 780), new Vector2(100, 100), UiKit.Red, () => { DemoRewards.Save(); rewardsPanel.SetActive(false); }, 48);
            rewardsPanel.SetActive(false);
        }
        void ShowDistricts()
        {
            for (int i = 0; i < 3; i++) districtButtons[i].text = PlayerProfile.TotalPoints < DashRun.DistrictPoints[i]
                ? $"UNLOCK AT {DashRun.DistrictPoints[i]:N0} LIFETIME POINTS" : DashRun.District == i ? "SELECTED" : "SELECT";
            districtPanel.SetActive(true);
        }
        void PickDistrict(int i)
        {
            if (PlayerProfile.TotalPoints < DashRun.DistrictPoints[i]) { Toast($"Earn {DashRun.DistrictPoints[i] - PlayerProfile.TotalPoints:N0} more points"); return; }
            DashRun.District = i;
            districtChip.text = DashRun.DistrictNames[i] + "  >";
            FindFirstObjectByType<TrackSpawner>()?.RefreshDistrict();
            districtPanel.SetActive(false);
        }
        void ShowRewards() { rewardsPage = 0; rewardsPanel.SetActive(true); RefreshRewards(); }
        void GenerateCoupon()
        {
            var coupon = DemoRewards.Issue();
            rewardsHint.text = coupon == null ? "Earn 3 delivery badges to generate a coupon." : "New unique coupon saved. Valid for the next 24 hours.";
            rewardsPage = 0; RefreshRewards();
        }
        DemoCoupon CouponAt(int row)
        {
            int i = rewardsPage * 3 + row;
            return i < DemoRewards.Ledger.coupons.Count ? DemoRewards.Ledger.coupons[i] : null;
        }
        void CopyCoupon(int row)
        {
            var c = CouponAt(row); if (c == null) return;
            GUIUtility.systemCopyBuffer = c.code; rewardsHint.text = "Coupon copied.";
        }
        void RedeemCoupon(int row)
        {
            var c = CouponAt(row); if (c == null) return;
            string result = DemoRewards.Redeem(c.code);
            rewardsHint.text = result == "REDEEMED" ? "FOOD INC dummy order: 10% applied. Coupon used." : "Cannot apply coupon: " + result.ToLowerInvariant();
            RefreshRewards();
        }
        void RefreshRewards()
        {
            badgeBalance.text = $"DELIVERY BADGES  {DemoRewards.Ledger.badges}   /   3 per coupon";
            var now = DateTime.UtcNow;
            for (int i = 0; i < 3; i++)
            {
                var c = CouponAt(i); couponRows[i].SetActive(c != null); if (c == null) continue;
                int split = (c.code.Length + 1) / 2;
                couponCodes[i].text = c.code.Substring(0, split) + "\n" + c.code.Substring(split);
                string status = DemoRewards.Status(c);
                var left = TimeSpan.FromTicks(Math.Max(0L, c.expiresUtcTicks - DemoRewards.Ledger.latestUtcTicks));
                couponStates[i].text = status == "ACTIVE" ? $"ACTIVE  /  {(int)left.TotalHours:00}:{left.Minutes:00}:{left.Seconds:00} left  /  one use" : status;
                couponRedeem[i].interactable = status == "ACTIVE";
                couponCopy[i].interactable = status == "ACTIVE";
            }
            int count = DemoRewards.Ledger.coupons.Count;
            pageText.text = count == 0 ? "No coupons yet" : $"{rewardsPage + 1} / {Mathf.Max(1, (count + 2) / 3)}";
            if (count == 0 && string.IsNullOrEmpty(rewardsHint.text)) rewardsHint.text = "Complete deliveries, then generate your first test coupon.";
        }
        void TickDashMenus(float dt)
        {
            if (!rewardsPanel || !rewardsPanel.activeSelf) return;
            rewardsRefresh -= dt;
            if (rewardsRefresh > 0f) return;
            rewardsRefresh = 1f; RefreshRewards();
        }
    }
}

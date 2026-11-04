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
            UiKit.Label(title.transform, "FOOD INC  /  POWERED BY PAGE STUDIO INC.", 26, new Vector2(0, -920), new Vector2(1040, 48), UiKit.Cream);
            // District picker beside FREE ROAM (CityHubUI), then the rewards wallet, above the tile row.
            var city = UiKit.Button(title.transform, "", new Vector2(-210, -425), new Vector2(405, 95), UiKit.Blue, ShowDistricts, 30);
            districtChip = city.GetComponentInChildren<Text>();
            districtChip.text = DashRun.DistrictNames[DashRun.District] + "  >";
            UiKit.Button(title.transform, "FOOD INC REWARDS", new Vector2(0, -550), new Vector2(830, 100), UiKit.Red, ShowRewards, 36, "star");

            districtPanel = UiKit.Shade(root, "Districts").gameObject;
            var p = UiKit.FancyCard(districtPanel.transform, Vector2.zero, new Vector2(1040, 1660), UiKit.Teal).transform;
            UiKit.Header(p, "CHOOSE YOUR DISTRICT", 710, UiKit.Teal, 66, 940);
            string[] blurbs = { "Neon blocks, cross traffic, street food.\nRamp onto the rooftops; run the building walls on foot.", "Rooftop gardens above Dash City.\nJump the gaps and chase sky deliveries.", "Light installations, music and food stalls.\nSlide under the stage trusses on the beat." };
            for (int i = 0; i < 3; i++)
            {
                int index = i;
                var card = UiKit.Panel(p, "District " + i, new Vector2(0, 430 - i * 440), new Vector2(940, 390), new Color(0.09f, 0.13f, 0.25f));
                UiKit.Label(card.transform, "0" + (i + 1) + "  " + DashRun.DistrictNames[i], 48, new Vector2(0, 125), new Vector2(890, 75), DashCity.Accent(i));
                UiKit.Label(card.transform, blurbs[i], 34, new Vector2(0, 22), new Vector2(890, 105), UiKit.Cream);
                districtButtons[i] = UiKit.Button(card.transform, "SELECT", new Vector2(0, -105), new Vector2(710, 105), UiKit.Blue, () => PickDistrict(index), 38).GetComponentInChildren<Text>();
            }
            UiKit.Button(p, "X", new Vector2(450, 740), new Vector2(105, 105), UiKit.Red, () => districtPanel.SetActive(false), 50);
            districtPanel.SetActive(false);

            rewardsPanel = UiKit.Shade(root, "FOOD INC demo wallet").gameObject;
            var r = UiKit.FancyCard(rewardsPanel.transform, Vector2.zero, new Vector2(1060, 1820), UiKit.Gold).transform;
            UiKit.Header(r, "FOOD INC REWARDS", 790, UiKit.Gold, 66, 980);
            UiKit.Label(r, "DEMO COUPONS  /  10% OFF A TEST ORDER", 34, new Vector2(0, 685), new Vector2(1000, 65), UiKit.Cream);
            badgeBalance = UiKit.Label(r, "", 40, new Vector2(0, 600), new Vector2(940, 75), Color.white);
            UiKit.Button(r, $"GENERATE  /  {BurgerPrices.Coupon} BURGERS", new Vector2(0, 480), new Vector2(840, 125), UiKit.Green, GenerateCoupon, 44);
            UiKit.Label(r, "Collect burgers on every run to pay for coupons.\nEach code expires in 24 hours and can be used once.", 32, new Vector2(0, 355), new Vector2(980, 105), UiKit.Cream);
            for (int i = 0; i < 3; i++)
            {
                int row = i;
                var card = UiKit.Panel(r, "Coupon " + i, new Vector2(0, 140 - i * 295), new Vector2(980, 275), new Color(0.09f, 0.13f, 0.25f));
                couponRows[i] = card.gameObject;
                couponCodes[i] = UiKit.Label(card.transform, "", 30, new Vector2(0, 68), new Vector2(940, 70), Color.white);
                couponStates[i] = UiKit.Label(card.transform, "", 30, new Vector2(0, 8), new Vector2(940, 52), UiKit.Yellow);
                couponCopy[i] = UiKit.Button(card.transform, "COPY", new Vector2(-260, -75), new Vector2(365, 115), UiKit.Blue, () => CopyCoupon(row), 34);
                couponRedeem[i] = UiKit.Button(card.transform, "TEST REDEEM", new Vector2(220, -75), new Vector2(490, 115), UiKit.Red, () => RedeemCoupon(row), 34);
            }
            rewardsHint = UiKit.Label(r, "", 32, new Vector2(0, -650), new Vector2(980, 75), UiKit.Cream);
            UiKit.Button(r, "<", new Vector2(-345, -755), new Vector2(155, 95), UiKit.Blue, () => { rewardsPage = Mathf.Max(0, rewardsPage - 1); RefreshRewards(); }, 44);
            UiKit.Button(r, ">", new Vector2(345, -755), new Vector2(155, 95), UiKit.Blue, () => { rewardsPage = Mathf.Min(Mathf.Max(0, (DemoRewards.Ledger.coupons.Count - 1) / 3), rewardsPage + 1); RefreshRewards(); }, 44);
            pageText = UiKit.Label(r, "", 34, new Vector2(0, -755), new Vector2(420, 75), Color.white);
            UiKit.Label(r, "Test redemption simulates FOOD INC on this device.\nThese codes do not apply to real food orders.", 28, new Vector2(0, -855), new Vector2(1000, 75), UiKit.Cream);
            UiKit.Button(r, "X", new Vector2(470, 820), new Vector2(105, 105), UiKit.Red, () => { DemoRewards.Save(); rewardsPanel.SetActive(false); }, 50);
            rewardsPanel.SetActive(false);
            BuildCityHub(root);
        }
        void ShowDistricts()
        {
            for (int i = 0; i < 3; i++) districtButtons[i].text = !BurgerPrices.DistrictOpen(i)
                ? $"UNLOCK  /  {BurgerPrices.District[i]:N0} BURGERS" : DashRun.District == i ? "SELECTED" : "SELECT";
            districtPanel.SetActive(true);
        }
        void PickDistrict(int i)
        {
            if (!BurgerPrices.DistrictOpen(i))
            {
                if (!DashRun.BuyDistrict(i)) { Toast($"Need {BurgerPrices.District[i] - PlayerProfile.Burgers:N0} more burgers"); return; }
                Toast($"{DashRun.DistrictNames[i]} UNLOCKED  /  -{BurgerPrices.District[i]:N0} BURGERS");
            }
            DashRun.District = i;
            districtChip.text = DashRun.DistrictNames[i] + "  >";
            FindFirstObjectByType<TrackSpawner>()?.RefreshDistrict();
            districtPanel.SetActive(false);
        }
        void ShowRewards() { rewardsPage = 0; rewardsPanel.SetActive(true); RefreshRewards(); }
        void GenerateCoupon()
        {
            var coupon = DemoRewards.Issue();
            rewardsHint.text = coupon == null ? $"Need {BurgerPrices.Coupon - PlayerProfile.Burgers:N0} more burgers for a coupon." : "New unique coupon saved. Valid for the next 24 hours.";
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
            badgeBalance.text = $"BURGERS  {PlayerProfile.Burgers:N0}   /   {BurgerPrices.Coupon} per coupon";
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
            if (count == 0 && string.IsNullOrEmpty(rewardsHint.text)) rewardsHint.text = "Collect burgers, then generate your first test coupon.";
        }
        void TickDashMenus(float dt)
        {
            UpdateCityDirectory(dt);
            if (!rewardsPanel || !rewardsPanel.activeSelf) return;
            rewardsRefresh -= dt;
            if (rewardsRefresh > 0f) return;
            rewardsRefresh = 1f; RefreshRewards();
        }
    }
}

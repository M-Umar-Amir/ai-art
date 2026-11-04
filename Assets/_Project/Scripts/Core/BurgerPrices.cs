using UnityEngine;

namespace DallasDash.Core
{
    /// Every price and payout in the game, in burgers: the one currency (client, 3 Nov 2026).
    /// Earning: one burger per burger pickup, plus a delivery tip every 450 m.
    /// Spending: kiosk charging (RidersEconomy), power-up upgrades, district and crew unlocks, outfits, coupons.
    /// Debt: kept as constants in one file for now; move to a ScriptableObject when designers start tuning.
    public static class BurgerPrices
    {
        /// District unlocks in DashRun.DistrictNames order. Neon Downtown is free.
        public static readonly int[] District = { 0, 150, 300 };
        public const int Bolt = 500;
        public const int Outfit = 120;
        /// One demo coupon (10% off a test order).
        public const int Coupon = 100;
        /// Power-up upgrade levels 1..5.
        public static readonly int[] Upgrade = { 40, 80, 150, 250, 400 };
        /// The 7-day login calendar.
        public static readonly int[] Daily = { 10, 15, 20, 30, 40, 60, 100 };
        /// Delivery tip paid every 450 m: more when the drop is on time.
        public const int OnTimeTip = 5, LateTip = 2;
        public static int MissionReward(int level) => 20 + 10 * Mathf.Min(level, 10);

        public static string DistrictItem(int district) => "district_" + district;
        public const string BoltItem = "crew_bolt";
        public static string OutfitItem(int crew) => "outfit_" + crew;

        public static bool DistrictOpen(int district) =>
            district <= 0 || district < District.Length && PlayerProfile.Owns(DistrictItem(district));

        /// Review and bot modes only (their profile is restored afterwards): every district and Bolt open.
        public static void UnlockAllForReview()
        {
            MigrateOnce();
            PlayerPrefs.SetInt("owned_" + DistrictItem(1), 1);
            PlayerPrefs.SetInt("owned_" + DistrictItem(2), 1);
            PlayerPrefs.SetInt("owned_" + BoltItem, 1);
            PlayerPrefs.Save();
        }

        /// Old saves unlocked things with lifetime points or crew fragments; whatever a player had open
        /// before the switch to burgers stays open. Runs once per install.
        public static void MigrateOnce()
        {
            if (PlayerPrefs.GetInt("burger_economy_v1", 0) == 1) return;
            int points = PlayerPrefs.GetInt("total_points", 0);
            if (points >= 10000) PlayerPrefs.SetInt("owned_" + DistrictItem(1), 1);
            if (points >= 25000) PlayerPrefs.SetInt("owned_" + DistrictItem(2), 1);
            if (points >= 25000 || PlayerPrefs.GetInt("dash_fragments", 0) >= 12) PlayerPrefs.SetInt("owned_" + BoltItem, 1);
            for (int crew = 0; crew < 3; crew++)
                if (PlayerPrefs.GetInt("dash_outfit_owned_" + crew, 0) == 1) PlayerPrefs.SetInt("owned_" + OutfitItem(crew), 1);
            _ = PlayerProfile.Burgers; // folds the old Food and chip balances into the wallet
            PlayerPrefs.SetInt("burger_economy_v1", 1);
            PlayerPrefs.Save();
        }
    }
}

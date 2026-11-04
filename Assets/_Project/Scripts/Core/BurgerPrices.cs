using UnityEngine;

namespace DallasDash.Core
{
    /// Every price and payout in the game, in burgers: the one currency.
    /// Rate (client, 4 Nov 2026): about one hour of continuous play earns 100,000 burgers, which is what a
    /// 10% coupon costs. Every burger pickup is worth PickupValue burgers.
    /// Spending: revives, power-up upgrades and the Ace outfit. Coupons are never bought: they appear on the
    /// track once 100,000 burgers have been collected (CouponProgress).
    /// Debt: kept as constants in one file for now; move to a ScriptableObject when designers start tuning.
    public static class BurgerPrices
    {
        /// Burgers credited per burger pickup. Calibrated from the playtest bot (4 Nov: 10 per pickup measured
        /// ~70,000 an hour; 14 measured 98k-138k), so 12 averages about 100,000 an hour of continuous play.
        public const int PickupValue = 12;
        /// Burgers collected (since the last coupon) that put a 10% coupon on the track.
        public const int CouponThreshold = 100000;
        /// Kept for older review code: coupons are no longer bought.
        public const int Coupon = CouponThreshold;
        public const int Outfit = 3000;
        /// Power-up upgrade levels 1..5.
        public static readonly int[] Upgrade = { 1000, 2000, 4000, 7000, 11000 };
        /// The 7-day login calendar.
        public static readonly int[] Daily = { 250, 400, 500, 750, 1000, 1500, 2500 };
        public static int MissionReward(int level) => 500 + 250 * Mathf.Min(level, 10);
        /// Revive price for the n-th revive of a run (0-based): 2,000, 4,000, 8,000... capped at 32,000.
        public static int Revive(int n) => 2000 << Mathf.Clamp(n, 0, 4);

        public static string OutfitItem(int crew) => "outfit_" + crew;

        /// Review and bot modes only (their profile is restored afterwards).
        public static void UnlockAllForReview() => MigrateOnce();

        /// One-off save migrations. v2 (4 Nov): the burger scale went up (x10 per pickup), so old balances
        /// are scaled to match; districts and other crew no longer exist.
        public static void MigrateOnce()
        {
            if (PlayerPrefs.GetInt("burger_economy_v1", 0) != 1)
            {
                if (PlayerPrefs.GetInt("dash_outfit_owned_0", 0) == 1) PlayerPrefs.SetInt("owned_" + OutfitItem(0), 1);
                _ = PlayerProfile.Burgers; // folds the old Food and chip balances into the wallet
                PlayerPrefs.SetInt("burger_economy_v1", 1);
            }
            if (PlayerPrefs.GetInt("burger_economy_v2", 0) != 1)
            {
                PlayerProfile.Burgers = PlayerProfile.Burgers * PickupValue;
                PlayerPrefs.SetInt("character", 0);
                PlayerPrefs.SetInt("burger_economy_v2", 1);
            }
            PlayerPrefs.Save();
        }
    }
}

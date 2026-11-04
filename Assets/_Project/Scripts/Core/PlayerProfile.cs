using System;
using UnityEngine;

namespace DallasDash.Core
{
    /// Everything the player keeps between sessions, stored in PlayerPrefs: the burger wallet (the game's
    /// only currency), what it has bought, best score, the daily login streak, and the settings choices.
    public static class PlayerProfile
    {
        public static event Action Changed;

        public static int BestScore
        {
            get => PlayerPrefs.GetInt("best", 0);
            set { PlayerPrefs.SetInt("best", value); Save(); }
        }

        /// The one rider is Ace (client, 4 Nov 2026). Kept as a property so older call sites read naturally.
        public static int Character
        {
            get => 0;
            set { }
        }

        /// Burgers collected toward the next 10% coupon (BurgerPrices.CouponThreshold). Spending burgers does not
        /// lower it; a coupon that appears on the track and is missed resets it to zero.
        public static int CouponProgress
        {
            get => PlayerPrefs.GetInt("coupon_progress", 0);
            set => PlayerPrefs.SetInt("coupon_progress", Mathf.Max(0, value)); // saved with the wallet
        }

        /// Legacy saved equipment preference; live equipment is assigned by character.
        public static int Ride
        {
            get => ForceRide >= 0 ? ForceRide : PlayerPrefs.GetInt("ride", 0) == 1 ? 1 : 0;
            set { PlayerPrefs.SetInt("ride", value); Save(); }
        }

        /// Screenshot mode overrides the travel mode without touching the saved profile; -1 otherwise.
        public static int ForceRide = -1;

        /// Burger-shop upgrade level (0..PowerUps.MaxLevel) of each power-up, in PowerUp order.
        public static int UpgradeLevel(int powerUp) => PlayerPrefs.GetInt("upgrade" + powerUp, 0);

        public static void SetUpgradeLevel(int powerUp, int level)
        {
            PlayerPrefs.SetInt("upgrade" + powerUp, Mathf.Clamp(level, 0, PowerUps.MaxLevel));
            Save();
        }

        /// Points scored across every run. Score only: nothing is bought or unlocked with points.
        public static int TotalPoints
        {
            get => PlayerPrefs.GetInt("total_points", 0);
            set { PlayerPrefs.SetInt("total_points", value); Save(); }
        }

        /// 0 Low, 1 Medium, 2 High. First launch defaults to High on desktop, Medium on phones.
        public static int Graphics
        {
            get => PlayerPrefs.GetInt("gfx", Application.isMobilePlatform ? 1 : 2);
            set { PlayerPrefs.SetInt("gfx", Mathf.Clamp(value, 0, 2)); Save(); }
        }

        public static float Volume
        {
            get => PlayerPrefs.GetFloat("volume", 0.8f);
            set { PlayerPrefs.SetFloat("volume", Mathf.Clamp01(value)); Save(); }
        }

        /// Music and sound-effect levels, set separately (accessibility guidelines: separate volume controls).
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat("music_volume", PlayerPrefs.GetFloat("volume", 0.8f));
            set { PlayerPrefs.SetFloat("music_volume", Mathf.Clamp01(value)); Save(); }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat("sfx_volume", PlayerPrefs.GetFloat("volume", 0.8f));
            set { PlayerPrefs.SetFloat("sfx_volume", Mathf.Clamp01(value)); Save(); }
        }

        /// Turns off camera shake, speed lines and most of the red hit flash.
        public static bool ReduceMotion
        {
            get => PlayerPrefs.GetInt("reduce_motion", 0) == 1;
            set { PlayerPrefs.SetInt("reduce_motion", value ? 1 : 0); Save(); }
        }

        /// Vibration taps on pickups and hits (Haptics).
        public static bool Haptics
        {
            get => PlayerPrefs.GetInt("haptics", 1) == 1;
            set { PlayerPrefs.SetInt("haptics", value ? 1 : 0); Save(); }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt("muted", 0) == 1;
            set { PlayerPrefs.SetInt("muted", value ? 1 : 0); Save(); }
        }

        // ---- The economy is burgers only. Burgers are earned from burger pickups, daily bonuses and missions,
        // and spent on revives, upgrades and the outfit.

        /// The burger wallet. Older saves are migrated once: their Food carries over and old chips convert at 20:1.
        public static int Burgers
        {
            get
            {
                if (!PlayerPrefs.HasKey("burgers"))
                    PlayerPrefs.SetInt("burgers", PlayerPrefs.GetInt("food", 25) + PlayerPrefs.GetInt("coins", 0) / 20);
                return PlayerPrefs.GetInt("burgers");
            }
            set { PlayerPrefs.SetInt("burgers", Mathf.Max(0, value)); Save(); }
        }

        /// Spends burgers if the wallet holds enough; nothing changes otherwise.
        public static bool TrySpend(int cost)
        {
            if (cost <= 0) return true;
            if (Burgers < cost) return false;
            Burgers -= cost;
            return true;
        }

        /// One-off purchases (districts, Bolt, outfits), kept as flags in PlayerPrefs.
        public static bool Owns(string item) => PlayerPrefs.GetInt("owned_" + item, 0) == 1;
        public static bool Buy(string item, int cost)
        {
            if (Owns(item)) return true;
            if (!TrySpend(cost)) return false;
            PlayerPrefs.SetInt("owned_" + item, 1);
            Save();
            return true;
        }

        // ---- Daily login bonus: a 7-day calendar. Claiming on consecutive days walks the streak;
        // missing a day starts it again from day 1. Day 7 is the big one, then it loops.

        public static int[] DailyRewards => BurgerPrices.Daily;

        static int Today => (int)(DateTime.Now.Date - new DateTime(2026, 1, 1)).TotalDays;
        static int LastClaimDay => PlayerPrefs.GetInt("daily_last", int.MinValue / 2);
        static int StoredStreak => PlayerPrefs.GetInt("daily_streak", 0);

        public static bool CanClaimDaily => Today > LastClaimDay;

        /// Days already claimed in the current streak (0-7), as the calendar should show them today.
        public static int DailyStreak
        {
            get
            {
                if (Today - LastClaimDay > 1) return 0; // missed a day: the streak is gone
                return StoredStreak % DailyRewards.Length == 0 && CanClaimDaily ? 0 : StoredStreak;
            }
        }

        /// Index (0-6) of the reward that today's claim pays, or would have paid if already claimed.
        public static int DailyIndex => CanClaimDaily ? DailyStreak % DailyRewards.Length : (DailyStreak - 1 + DailyRewards.Length) % DailyRewards.Length;

        public static int ClaimDaily()
        {
            if (!CanClaimDaily) return 0;
            int streak = DailyStreak;
            int reward = DailyRewards[streak % DailyRewards.Length];
            PlayerPrefs.SetInt("daily_streak", streak % DailyRewards.Length + 1);
            PlayerPrefs.SetInt("daily_last", Today);
            Burgers += reward;
            return reward;
        }

        static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// Lets another system that writes profile-owned PlayerPrefs keys raise the same change event.
        public static void NotifyChanged()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}

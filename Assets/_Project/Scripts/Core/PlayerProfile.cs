using System;
using UnityEngine;

namespace DallasDash.Core
{
    /// Everything the player keeps between sessions, stored in PlayerPrefs: coins, best score,
    /// the daily login streak, and the settings menu choices.
    public static class PlayerProfile
    {
        public static event Action Changed;

        public static int Coins
        {
            get => PlayerPrefs.GetInt("coins", 0);
            set { PlayerPrefs.SetInt("coins", Mathf.Max(0, value)); Save(); }
        }

        public static int BestScore
        {
            get => PlayerPrefs.GetInt("best", 0);
            set { PlayerPrefs.SetInt("best", value); Save(); }
        }

        /// Crew member riding: 0 Finn, 1 Faye, 2 Fizz-11.
        public static int Character
        {
            get => PlayerPrefs.GetInt("character", 0);
            set { PlayerPrefs.SetInt("character", value); Save(); }
        }

        /// Ride under the crew: 0 delivery bicycle, 1 courier scooter (earned through missions).
        public static int Ride
        {
            get => ForceRide >= 0 ? ForceRide : PlayerPrefs.GetInt("ride", 0) == 1 && Missions.ScooterEarned ? 1 : 0;
            set { PlayerPrefs.SetInt("ride", value); Save(); }
        }

        /// Screenshot mode shows the scooter without touching the saved profile; -1 otherwise.
        public static int ForceRide = -1;

        /// Coin-shop upgrade level (0..PowerUps.MaxLevel) of each power-up, in PowerUp order.
        public static int UpgradeLevel(int powerUp) => PlayerPrefs.GetInt("upgrade" + powerUp, 0);

        public static void SetUpgradeLevel(int powerUp, int level)
        {
            PlayerPrefs.SetInt("upgrade" + powerUp, Mathf.Clamp(level, 0, PowerUps.MaxLevel));
            Save();
        }

        /// Points scored across every run; Fizz-11 unlocks at 25,000.
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

        // ---- Daily login bonus: a 7-day calendar. Claiming on consecutive days walks the streak;
        // missing a day starts it again from day 1. Day 7 is the big one, then it loops.

        public static readonly int[] DailyRewards = { 100, 150, 200, 300, 400, 600, 1000 };

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
            Coins += reward;
            return reward;
        }

        static void Save()
        {
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}

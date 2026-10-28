using System;
using UnityEngine;

namespace DallasDash.Core
{
    public enum PowerUp { Magnet, Rush, Double }

    /// Timed run boosts picked up from tokens on the street. Magnet pulls burgers in from every lane,
    /// Sugar Rush speeds the rider up and shrugs off obstacles, Double Score doubles every point.
    /// Each lasts longer for every coin-shop upgrade (PlayerProfile.UpgradeLevel).
    public static class PowerUps
    {
        public const int Count = 3;
        public const int MaxLevel = 5;
        public static readonly string[] Names = { "MAGNET", "SUGAR RUSH", "DOUBLE SCORE" };
        public static readonly string[] Blurbs =
        {
            "Pulls in burgers from every lane.",
            "Faster, and obstacles bounce off you.",
            "Every point counts twice.",
        };
        /// Icons.Get keys, HUD pill colours and token colours, in PowerUp order.
        public static readonly string[] IconKeys = { "magnet", "bolt", "x2" };
        public static readonly Color[] Colors = { new(0.92f, 0.2f, 0.22f), new(0.2f, 0.55f, 1f), new(0.2f, 0.75f, 0.35f) };
        /// Coin price of upgrade level 1..MaxLevel.
        public static readonly int[] UpgradeCost = { 500, 1000, 2000, 4000, 7500 };
        public const float RushSpeedFactor = 1.3f;

        static readonly float[] remaining = new float[Count];

        public static event Action<PowerUp> Activated;

        public static float Duration(PowerUp p)
        {
            int lvl = PlayerProfile.UpgradeLevel((int)p);
            return p == PowerUp.Rush ? 5f + lvl * 1.5f : 8f + lvl * 2f;
        }

        public static bool Active(PowerUp p) => remaining[(int)p] > 0f;
        public static float Remaining(PowerUp p) => remaining[(int)p];
        public static float Fraction(PowerUp p) => Mathf.Clamp01(remaining[(int)p] / Duration(p));

        public static void Activate(PowerUp p)
        {
            remaining[(int)p] = Duration(p);
            Missions.Add(MissionStat.PowerUps, 1);
            Activated?.Invoke(p);
        }

        public static void Tick(float dt)
        {
            for (int i = 0; i < Count; i++) remaining[i] = Mathf.Max(0f, remaining[i] - dt);
        }

        public static void Clear() => Array.Clear(remaining, 0, Count);
    }
}

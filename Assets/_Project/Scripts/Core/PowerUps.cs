using System;
using UnityEngine;

namespace DallasDash.Core
{
    public enum PowerUp { Magnet, Rush, Double, Boots, Shield, Battery }

    /// Timed run boosts picked up from tokens on the street. Magnet pulls burgers in from every lane,
    /// Sugar Rush speeds the rider up and shrugs off obstacles, Double Score doubles every point.
    /// Each lasts longer for every coin-shop upgrade (PlayerProfile.UpgradeLevel).
    public static class PowerUps
    {
        public const int Count = 6;
        public const int MaxLevel = 5;
        public static readonly string[] Names = { "MAGNET PACK", "SPEED CORE", "DOUBLE SCORE", "GRAVITY BOOTS", "SHIELD DRONE", "DASH BATTERY" };
        public static readonly string[] Blurbs =
        {
            "Food and chips fly to you.",
            "Faster; hits bounce off.",
            "Every point x2.",
            "Longer jumps; ride gravity rails.",
            "Absorbs one collision.",
            "Recharges your crew ability.",
        };
        /// Icons.Get keys, HUD pill colours and token colours, in PowerUp order.
        public static readonly string[] IconKeys = { "magnet", "bolt", "x2", "boot", "shield", "bolt" };
        public static readonly Color[] Colors = { new(0.92f, 0.2f, 0.22f), new(0.2f, 0.55f, 1f), new(0.2f, 0.75f, 0.35f), new(0.65f, 0.2f, 1f), new(0.1f, 0.9f, 0.9f), new(1f, 0.65f, 0.1f) };
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
            if (p == PowerUp.Battery) DashRun.Instance?.Recharge();
            Missions.Add(MissionStat.PowerUps, 1);
            Activated?.Invoke(p);
        }

        public static void Tick(float dt)
        {
            for (int i = 0; i < Count; i++) remaining[i] = Mathf.Max(0f, remaining[i] - dt);
        }

        public static void Consume(PowerUp p) => remaining[(int)p] = 0f;

        public static void Clear() => Array.Clear(remaining, 0, Count);
    }
}

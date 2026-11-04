using UnityEngine;

namespace DallasDash.Core
{
    /// The run's charge tank and the burger wallet it is refilled from. Burgers are the only currency
    /// (BurgerPrices); the tank is a per-run value, not a saved balance.
    ///
    /// The tank reaches the first kiosk comfortably. A dry tank drops the rider to 55% pace and starts
    /// DashRun's six-second dry countdown. A rider with no burgers still gets one free emergency splash
    /// per kiosk.
    public static class RidersEconomy
    {
        public const float TankCapacity = 1f;
        /// Fraction of a tank burned per second at cruise. About 45 seconds from full to empty.
        public const float BurnPerSecond = 0.022f;
        /// Four burgers fill a completely empty tank: one burger buys a quarter tank.
        public const float BurgersPerTank = 4f;
        /// A rider who arrives with no burgers still gets one small free splash at each kiosk.
        public const float EmergencySplash = 0.18f;
        public const float EmptySpeedFactor = 0.55f;
        /// Below this the motor sputters and the rider loses a little pace before the tank is dry.
        public const float LowFuel = 0.18f;

        public static float Fuel { get; private set; } = TankCapacity;
        public static bool Empty => Fuel <= 0.0005f;
        public static bool Low => Fuel <= LowFuel;

        /// 1 at a healthy tank, tapering to EmptySpeedFactor when dry.
        public static float SpeedMultiplier =>
            Fuel <= 0f ? EmptySpeedFactor
            : Fuel >= LowFuel ? 1f
            : Mathf.Lerp(0.78f, 1f, Fuel / LowFuel);

        public static float FuelPercent => Mathf.Clamp01(Fuel / TankCapacity);

        public static void BeginRun() => Fuel = TankCapacity;

        /// Pure burn step, kept separate so validation can test the curve without a running game.
        public static float BurnAfter(float fuel, float dt, float speed)
        {
            if (dt <= 0f) return Mathf.Clamp01(fuel);
            float speedFactor = Mathf.Lerp(0.85f, 1.35f, Mathf.InverseLerp(Tuning.SpeedBase, Tuning.SpeedMax, speed));
            return Mathf.Clamp01(fuel - dt * BurnPerSecond * speedFactor);
        }

        public static void Burn(float dt, float speed) => Fuel = BurnAfter(Fuel, dt, speed);

        public static void AddFuel(float amount) => Fuel = Mathf.Clamp01(Fuel + Mathf.Max(0f, amount));

        public static int Burgers
        {
            get => PlayerProfile.Burgers;
            set => PlayerProfile.Burgers = value;
        }

        public static void AwardBurgers(int amount)
        {
            if (amount > 0) Burgers += amount;
        }

        public static bool TrySpendBurgers(int cost) => PlayerProfile.TrySpend(cost);

        /// What a kiosk stop would buy right now: burgers spent, charge added. Never spends more burgers than
        /// the rider has, and never buys more charge than the tank can hold.
        public static (int cost, float added) PitPlan(float missingFuel, int burgersOwned)
        {
            missingFuel = Mathf.Clamp01(missingFuel);
            int wanted = Mathf.CeilToInt(missingFuel * BurgersPerTank - 0.0001f);
            wanted = Mathf.Clamp(wanted, 0, Mathf.CeilToInt(BurgersPerTank));
            int cost = Mathf.Min(wanted, Mathf.Max(0, burgersOwned));
            return (cost, Mathf.Min(missingFuel, cost / BurgersPerTank));
        }

        /// Seconds of charging per burger spent, so a full tank takes noticeably longer than a splash.
        public static float ChargeSeconds(int burgersSpent) => 0.9f + 0.28f * Mathf.Max(0, burgersSpent);
    }
}

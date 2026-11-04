namespace DallasDash.Core
{
    /// The burger wallet (burgers are the only currency, BurgerPrices). The fuel tank and kiosks are gone
    /// (client, 4 Nov 2026).
    public static class RidersEconomy
    {
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
    }
}

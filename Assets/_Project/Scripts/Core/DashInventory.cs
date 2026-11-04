using UnityEngine;

namespace DallasDash.Core
{
    /// Crew unlocks and outfits, all bought with burgers (BurgerPrices).
    public static class DashInventory
    {
        public static int Outfit(int crew) => PlayerPrefs.GetInt("dash_outfit_" + crew, 0);
        public static bool OwnsOutfit(int crew) => PlayerProfile.Owns(BurgerPrices.OutfitItem(crew));
        public static bool BoltUnlocked => PlayerProfile.Owns(BurgerPrices.BoltItem);
        public static bool BuyBolt() => PlayerProfile.Buy(BurgerPrices.BoltItem, BurgerPrices.Bolt);

        /// Counts the drop and pays its tip in burgers.
        public static int Delivery(bool onTime)
        {
            PlayerPrefs.SetInt("dash_deliveries", PlayerPrefs.GetInt("dash_deliveries", 0) + 1);
            int tip = onTime ? BurgerPrices.OnTimeTip : BurgerPrices.LateTip;
            RidersEconomy.AwardBurgers(tip);
            return tip;
        }

        /// Equips or removes the ION outfit; the first equip buys it. False if the wallet is short.
        public static bool ToggleOutfit(int crew)
        {
            if (!OwnsOutfit(crew) && !PlayerProfile.Buy(BurgerPrices.OutfitItem(crew), BurgerPrices.Outfit)) return false;
            PlayerPrefs.SetInt("dash_outfit_" + crew, 1 - Outfit(crew));
            PlayerPrefs.Save(); return true;
        }
    }
}

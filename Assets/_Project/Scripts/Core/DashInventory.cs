using UnityEngine;

namespace DallasDash.Core
{
    /// Ace's outfit, bought with burgers (BurgerPrices).
    public static class DashInventory
    {
        public static int Outfit(int crew) => PlayerPrefs.GetInt("dash_outfit_" + crew, 0);
        public static bool OwnsOutfit(int crew) => PlayerProfile.Owns(BurgerPrices.OutfitItem(crew));

        /// Equips or removes the ION outfit; the first equip buys it. False if the wallet is short.
        public static bool ToggleOutfit(int crew)
        {
            if (!OwnsOutfit(crew) && !PlayerProfile.Buy(BurgerPrices.OutfitItem(crew), BurgerPrices.Outfit)) return false;
            PlayerPrefs.SetInt("dash_outfit_" + crew, 1 - Outfit(crew));
            PlayerPrefs.Save(); return true;
        }
    }
}

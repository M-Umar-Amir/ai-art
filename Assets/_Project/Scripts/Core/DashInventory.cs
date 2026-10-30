using UnityEngine;

namespace DallasDash.Core
{
    public static class DashInventory
    {
        public static int Fragments => PlayerPrefs.GetInt("dash_fragments", 0);
        public static int OutfitTokens => PlayerPrefs.GetInt("dash_outfit_tokens", 0);
        public static int EventCollectibles => PlayerPrefs.GetInt("dash_event_collectibles", 0);
        public static int Outfit(int crew) => PlayerPrefs.GetInt("dash_outfit_" + crew, 0);
        public static bool OwnsOutfit(int crew) => PlayerPrefs.GetInt("dash_outfit_owned_" + crew, 0) == 1;
        public static bool BoltUnlocked => PlayerProfile.TotalPoints >= 25000 || Fragments >= 12;
        public static void Delivery()
        {
            int deliveries = PlayerPrefs.GetInt("dash_deliveries", 0) + 1;
            PlayerPrefs.SetInt("dash_deliveries", deliveries);
            PlayerPrefs.SetInt("dash_fragments", Fragments + 1);
            if (deliveries % 2 == 0) PlayerPrefs.SetInt("dash_outfit_tokens", OutfitTokens + 1);
            if (DashRun.District == 2) PlayerPrefs.SetInt("dash_event_collectibles", EventCollectibles + 1);
            PlayerPrefs.Save();
        }
        public static bool ToggleOutfit(int crew)
        {
            if (!OwnsOutfit(crew))
            {
                if (OutfitTokens < 4) return false;
                PlayerPrefs.SetInt("dash_outfit_tokens", OutfitTokens - 4);
                PlayerPrefs.SetInt("dash_outfit_owned_" + crew, 1);
            }
            PlayerPrefs.SetInt("dash_outfit_" + crew, 1 - Outfit(crew));
            PlayerPrefs.Save(); return true;
        }
    }
}

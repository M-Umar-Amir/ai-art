using System;
using UnityEngine;

namespace DallasDash.Core
{
    public static class DemoRewards
    {
        const string Key = "dash_riders_demo_rewards_v1";
        static CouponLedger ledger;
        public static CouponLedger Ledger => ledger ??= Load();
        public static event Action Changed;
        static CouponLedger Load()
        {
            try { return JsonUtility.FromJson<CouponLedger>(PlayerPrefs.GetString(Key, "")) ?? new CouponLedger(); }
            catch (ArgumentException) { return new CouponLedger(); }
        }
        public static void Save()
        {
            Ledger.Observe(DateTime.UtcNow);
            PlayerPrefs.SetString(Key, JsonUtility.ToJson(Ledger));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
        /// Issues the 10% coupon the rider just grabbed on the track (earned with 100,000 collected burgers;
        /// nothing is charged here). Production must mint it on the food app's server first.
        public static DemoCoupon Award()
        {
            var coupon = Ledger.Issue(DateTime.UtcNow);
            Save();
            return coupon;
        }
        public static string Redeem(string code)
        {
            string result = Ledger.Redeem(code, DateTime.UtcNow);
            Save();
            return result;
        }
        public static string Status(DemoCoupon coupon) => Ledger.Status(coupon, DateTime.UtcNow);
    }
}

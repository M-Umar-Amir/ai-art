using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DallasDash.Core
{
    [Serializable] public class DemoCoupon
    {
        public string code;
        public long issuedUtcTicks, expiresUtcTicks, redeemedUtcTicks;
        public string issuanceId;
    }

    // Deliberately independent of Unity: the dummy food app and the game share one local ledger.
    // Production must move this transaction to FOOD INC's server.
    [Serializable] public class CouponLedger
    {
        public const int BadgeCost = 3;
        public int badges;
        public long latestUtcTicks;
        public string issuerNamespace;
        public string lastSerial = "0";
        public List<DemoCoupon> coupons = new List<DemoCoupon>();

        public long Observe(DateTime utc)
        {
            latestUtcTicks = Math.Max(latestUtcTicks, utc.ToUniversalTime().Ticks);
            return latestUtcTicks;
        }

        public DemoCoupon Issue(DateTime utc)
        {
            long now = Observe(utc);
            if (badges < BadgeCost) return null;
            // Namespace + an expanding base-36 serial is the durable identity. No integer overflow
            // and no wrap-around: Z -> 10, ZZ -> 100, and so on. Deleting old history never resets it.
            // In production the server must allocate the namespace and advance the serial atomically.
            // This offline demo creates a 128-bit random installation namespace instead.
            if (string.IsNullOrEmpty(issuerNamespace)) issuerNamespace = RandomHex();
            lastSerial = IncrementSerial(lastSerial);
            string identity = issuerNamespace + "-" + lastSerial;
            // Independent 128-bit entropy makes sequential identities unsuitable for guessing codes.
            string payload = "DR1-" + identity + "-" + RandomHex();
            string code = payload + "-" + Checksum(payload);
            var coupon = new DemoCoupon { code = code, issuedUtcTicks = now,
                expiresUtcTicks = now + TimeSpan.TicksPerDay, issuanceId = identity };
            badges -= BadgeCost;
            coupons.Insert(0, coupon);
            return coupon;
        }

        static string RandomHex()
        {
            byte[] bytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
            return BitConverter.ToString(bytes).Replace("-", "");
        }

        public static string IncrementSerial(string serial)
        {
            const string alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            char[] digits = (string.IsNullOrEmpty(serial) ? "0" : serial).ToUpperInvariant().ToCharArray();
            foreach (char digit in digits) if (alphabet.IndexOf(digit) < 0)
                throw new InvalidOperationException("Invalid coupon serial; refusing to reuse identities.");
            if (digits.Length > 1 && digits[0] == '0')
                throw new InvalidOperationException("Non-canonical coupon serial; refusing to reuse identities.");
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                int value = alphabet.IndexOf(digits[i]);
                if (value < 0) throw new InvalidOperationException("Invalid coupon serial; refusing to reuse identities.");
                if (value < 35) { digits[i] = alphabet[value + 1]; return new string(digits); }
                digits[i] = '0';
            }
            return "1" + new string(digits);
        }

        static string Checksum(string payload)
        {
            using (var hash = SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(Encoding.ASCII.GetBytes(payload)), 0, 4).Replace("-", "");
        }

        public static bool ValidFormat(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            code = code.Trim().ToUpperInvariant();
            string[] parts = code.Split('-');
            if (parts.Length != 5 || parts[0] != "DR1" || parts[1].Length != 32 || parts[2].Length == 0 || parts[3].Length != 32 || parts[4].Length != 8) return false;
            for (int i = 1; i < 5; i++) foreach (char c in parts[i])
                if ((i == 2 ? "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ" : "0123456789ABCDEF").IndexOf(c) < 0) return false;
            if (parts[2][0] == '0') return false;
            int end = code.LastIndexOf('-');
            return string.Equals(Checksum(code.Substring(0, end)), parts[4], StringComparison.Ordinal);
        }

        public string Status(DemoCoupon coupon, DateTime utc)
        {
            long now = Observe(utc);
            if (coupon == null) return "UNKNOWN";
            if (coupon.redeemedUtcTicks != 0) return "USED";
            return now >= coupon.expiresUtcTicks ? "EXPIRED" : "ACTIVE";
        }

        public string Redeem(string code, DateTime utc)
        {
            if (code?.Trim().StartsWith("DR1-", StringComparison.OrdinalIgnoreCase) == true && !ValidFormat(code)) return "INVALID";
            var coupon = coupons.Find(c => string.Equals(c.code, code?.Trim(), StringComparison.OrdinalIgnoreCase));
            string status = Status(coupon, utc);
            if (status != "ACTIVE") return status;
            coupon.redeemedUtcTicks = latestUtcTicks;
            return "REDEEMED";
        }
    }
}

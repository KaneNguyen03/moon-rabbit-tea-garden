using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace VuonTra.Core
{
    /// <summary>
    /// Gift codes (#68), with no server. Two sorts: the few public codes in <see cref="Table"/>, and signed codes made
    /// by tools/make-code.sh: four characters that say the gift and the last day, then four of HMAC-SHA256 signature
    /// with a key the repository does not hold (the build reads it from Resources/keys.json). The game checks the
    /// signature here, so a new signed code needs no new build. Without a key every signed code is refused and the
    /// table still works. Each garden can use each code once; the codes used are kept in the save.
    /// </summary>
    public static class GiftCodes
    {
        public enum Result { Ok, Invalid, Expired, Used }

        /// <summary>What a signed code gives, by its first character (the alphabet's 0th, 1st, ...).</summary>
        public enum SignedKind { Golden5, Golden10, Clovers2, Bundle }

        /// <summary>One code of the public table.</summary>
        public sealed class Entry
        {
            public string Code;
            public int Golden, Clovers;
            /// <summary>An item id, or null.</summary>
            public string Hat;
            /// <summary>The last day number on which the code works.</summary>
            public long LastDay;
        }

        /// <summary>What trying a code did. Golden counts the carrots given in a hat's place.</summary>
        public struct Redeemed
        {
            public Result Result;
            /// <summary>The code as it is remembered: capitals, no spaces or dashes.</summary>
            public string Code;
            public int Golden, Clovers;
            /// <summary>The hat put in the wardrobe, or null.</summary>
            public Item Hat;
        }

        /// <summary>2026-01-01: the day a signed code's last day is counted from.</summary>
        public const long BaseDay = 20454;
        /// <summary>Golden carrots in a hat's place when the catalogue lacks it (the Moon Bow's shop price).</summary>
        public const int HatGoldenFallback = 40;
        const string SignPrefix = "VTT1:";

        /// <summary>The public codes for the playtest. "Nơ trăng" is the shop's hat-no-trang.</summary>
        public static readonly Entry[] Table =
        {
            new Entry { Code = "TRATHO-2026", Golden = 5, Clovers = 2, LastDay = Day(2027, 3, 31) },
            new Entry { Code = "CAMON-PLAYTEST", Hat = "hat-no-trang", LastDay = Day(2027, 3, 31) },
        };

        static readonly int[,] SignedGifts = { { 5, 0 }, { 10, 0 }, { 0, 2 }, { 5, 2 } }; // golden, clovers by SignedKind

        static byte[] _key;

        /// <summary>True when signed codes can be checked.</summary>
        public static bool HasKey => _key != null;

        /// <summary>Sets the signing key; null or empty means none, so every signed code is refused.</summary>
        public static void SetKey(string key) => _key = string.IsNullOrEmpty(key) ? null : Encoding.UTF8.GetBytes(key);

        /// <summary>The "giftKey" of a keys.json text, or null.</summary>
        public static string KeyFromJson(string json)
        {
            Dictionary<string, object> root = SaveCodec.ReadObject(json);
            return root != null && root.TryGetValue("giftKey", out object key) && key is string k && k.Length > 0 ? k : null;
        }

        /// <summary>A calendar date as the game's day number (days since 1970-01-01).</summary>
        public static long Day(int year, int month, int day)
            => (long)(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;

        /// <summary>Capitals, with spaces, dashes, dots and underscores gone: "tratho 2026" and "TRATHO-2026" are one code.</summary>
        public static string Normalize(string raw)
        {
            if (raw == null) return "";
            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                if (char.IsWhiteSpace(c) || c == '-' || c == '_' || c == '.') continue;
                sb.Append(char.ToUpperInvariant(c));
            }
            return sb.ToString();
        }

        /// <summary>The table's entry for a code however it is typed, or null.</summary>
        public static Entry Find(string raw)
        {
            string code = Normalize(raw);
            if (code.Length == 0) return null;
            foreach (Entry e in Table)
                if (Normalize(e.Code) == code) return e;
            return null;
        }

        /// <summary>
        /// Makes a signed code, as tools/make-code.sh does: XXXX-XXXX. Needs the key; the last day must be within
        /// 32 767 days of <see cref="BaseDay"/>.
        /// </summary>
        public static string Make(SignedKind kind, long lastDay)
        {
            if (_key == null) throw new InvalidOperationException("No gift key");
            long offset = lastDay - BaseDay;
            if (offset < 0 || offset > 32767) throw new ArgumentOutOfRangeException(nameof(lastDay));
            string a = Profile.CodeAlphabet;
            string type = new string(new[] { a[(int)kind], a[(int)(offset >> 10) & 31], a[(int)(offset >> 5) & 31], a[(int)offset & 31] });
            return type + "-" + Sign(type);
        }

        /// <summary>The first 20 bits of HMAC-SHA256(key, "VTT1:" + type), as four characters of the alphabet.</summary>
        static string Sign(string type)
        {
            byte[] mac;
            using (var hmac = new HMACSHA256(_key)) mac = hmac.ComputeHash(Encoding.ASCII.GetBytes(SignPrefix + type));
            int v = (mac[0] << 12) | (mac[1] << 4) | (mac[2] >> 4);
            string a = Profile.CodeAlphabet;
            return new string(new[] { a[(v >> 15) & 31], a[(v >> 10) & 31], a[(v >> 5) & 31], a[v & 31] });
        }

        /// <summary>Reads a signed code: false unless it is eight characters of the alphabet, of a known kind, signed with the key.</summary>
        static bool TrySigned(string code, out SignedKind kind, out long lastDay)
        {
            kind = SignedKind.Golden5;
            lastDay = -1;
            if (_key == null || code.Length != 8) return false;
            string a = Profile.CodeAlphabet;
            var at = new int[8];
            for (int i = 0; i < 8; i++)
                if ((at[i] = a.IndexOf(code[i])) < 0) return false;
            if (at[0] >= SignedGifts.GetLength(0)) return false;
            string type = code.Substring(0, 4);
            string want = Sign(type), got = code.Substring(4);
            int diff = 0;
            for (int i = 0; i < 4; i++) diff |= want[i] ^ got[i];
            if (diff != 0) return false;
            kind = (SignedKind)at[0];
            lastDay = BaseDay + ((at[1] << 10) | (at[2] << 5) | at[3]);
            return true;
        }

        /// <summary>
        /// Tries a code the player typed on <paramref name="day"/> (the clamped day): a known, unused code within its
        /// last day pays its gift and is remembered. A hat the garden already has comes as its price in golden carrots.
        /// Anything else changes nothing.
        /// </summary>
        public static Redeemed Redeem(GameState s, string raw, long day, IReadOnlyList<ItemDef> items)
        {
            string code = Normalize(raw);
            var r = new Redeemed { Result = Result.Invalid, Code = code };
            int golden, clovers;
            string hat = null;
            long lastDay;
            Entry entry = Find(code);
            if (entry != null)
            {
                golden = entry.Golden;
                clovers = entry.Clovers;
                hat = entry.Hat;
                lastDay = entry.LastDay;
            }
            else if (TrySigned(code, out SignedKind kind, out lastDay))
            {
                golden = SignedGifts[(int)kind, 0];
                clovers = SignedGifts[(int)kind, 1];
            }
            else return r;

            if (s.CodesUsed.Contains(code)) { r.Result = Result.Used; return r; }
            if (day > lastDay) { r.Result = Result.Expired; return r; }

            if (hat != null)
            {
                ItemDef def = Items.DefOf(items, hat);
                if (def != null && !Items.Owns(s, hat)) r.Hat = Items.Grant(s, def, def.Tier);
                else golden += def != null && def.Price > 0 ? def.Price : HatGoldenFallback;
            }
            s.CodesUsed.Add(code);
            s.GoldenCarrots += golden;
            s.Clovers += clovers;
            r.Golden = golden;
            r.Clovers = clovers;
            r.Result = Result.Ok;
            return r;
        }
    }
}

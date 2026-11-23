using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace VuonTra.Core
{
    public static class GiftCodes
    {
        public enum Result { Ok, Invalid, Expired, Used }

        public enum SignedKind { Golden5, Golden10, Clovers2, Bundle }

        public sealed class Entry
        {
            public string Code;
            public int Golden, Clovers;
            public string Hat;
            public long LastDay;
        }

        public struct Redeemed
        {
            public Result Result;
            public string Code;
            public int Golden, Clovers;
            public Item Hat;
        }

        public const long BaseDay = 20454;
        public const int HatGoldenFallback = 40;
        const string SignPrefix = "VTT1:";

        public static readonly Entry[] Table =
        {
            new Entry { Code = "TRATHO-2026", Golden = 5, Clovers = 2, LastDay = Day(2027, 3, 31) },
            new Entry { Code = "CAMON-PLAYTEST", Hat = "hat-no-trang", LastDay = Day(2027, 3, 31) },
        };

        static readonly int[,] SignedGifts = { { 5, 0 }, { 10, 0 }, { 0, 2 }, { 5, 2 } };

        static byte[] _key;

        public static bool HasKey => _key != null;

        public static void SetKey(string key) => _key = string.IsNullOrEmpty(key) ? null : Encoding.UTF8.GetBytes(key);

        public static string KeyFromJson(string json)
        {
            Dictionary<string, object> root = SaveCodec.ReadObject(json);
            return root != null && root.TryGetValue("giftKey", out object key) && key is string k && k.Length > 0 ? k : null;
        }

        public static long Day(int year, int month, int day)
            => (long)(new DateTime(year, month, day, 0, 0, 0, DateTimeKind.Utc) - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalDays;

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

        public static Entry Find(string raw)
        {
            string code = Normalize(raw);
            if (code.Length == 0) return null;
            foreach (Entry e in Table)
                if (Normalize(e.Code) == code) return e;
            return null;
        }

        public static string Make(SignedKind kind, long lastDay)
        {
            if (_key == null) throw new InvalidOperationException("No gift key");
            long offset = lastDay - BaseDay;
            if (offset < 0 || offset > 32767) throw new ArgumentOutOfRangeException(nameof(lastDay));
            string a = Profile.CodeAlphabet;
            string type = new string(new[] { a[(int)kind], a[(int)(offset >> 10) & 31], a[(int)(offset >> 5) & 31], a[(int)offset & 31] });
            return type + "-" + Sign(type);
        }

        static string Sign(string type)
        {
            byte[] mac;
            using (var hmac = new HMACSHA256(_key)) mac = hmac.ComputeHash(Encoding.ASCII.GetBytes(SignPrefix + type));
            int v = (mac[0] << 12) | (mac[1] << 4) | (mac[2] >> 4);
            string a = Profile.CodeAlphabet;
            return new string(new[] { a[(v >> 15) & 31], a[(v >> 10) & 31], a[(v >> 5) & 31], a[v & 31] });
        }

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

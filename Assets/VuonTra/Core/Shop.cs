using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Shop
    {
        static readonly int[] GiftChain = { 1, 1, 2, 2, 3, 3, 5 };
        const int GiftLoopFrom = 4;

        public const int MissPenalty = 1;

        public const int WeekDays = 7;

        public const string BigGiftHat = "hat-non-la";
        public const int BigGiftClovers = 1;
        public const int BigGiftHatGolden = 20;

        public struct BigGift
        {
            public bool Given;
            public Item Hat;
            public int Golden;
            public int Clovers;
        }

        public struct GiftTile
        {
            public int Day;
            public int Golden;
            public bool Claimed, Today, Big;
        }

        public static int GiftAt(int streakDay)
        {
            if (streakDay < 1) streakDay = 1;
            if (streakDay <= GiftChain.Length) return GiftChain[streakDay - 1];
            int loop = GiftChain.Length - GiftLoopFrom;
            return GiftChain[GiftLoopFrom + (streakDay - GiftChain.Length - 1) % loop];
        }

        public static bool GiftWaiting(GameState s, long day) => day > s.GiftDay;

        public static int StreakFor(GameState s, long day)
        {
            if (s.GiftDay < 0) return 1;
            if (day <= s.GiftDay) return Math.Max(1, s.GiftStreak);
            if (day == s.GiftDay + 1) return s.GiftStreak + 1;
            long missed = day - s.GiftDay - 1;
            return (int)Math.Max(1L, s.GiftStreak - Math.Min(missed, 100000L) * MissPenalty);
        }

        public static bool IsBigDay(GameState s, int streakDay) => streakDay >= WeekDays && streakDay % WeekDays == 0 && !s.GiftBigClaimed;

        public static GiftTile[] Week(GameState s, long day)
        {
            bool waiting = GiftWaiting(s, day);
            int today = StreakFor(s, day);
            int first = (today - 1) / WeekDays * WeekDays + 1;
            bool bigShown = !s.GiftBigClaimed || (!waiting && s.GiftBigDay == s.GiftDay);
            var tiles = new GiftTile[WeekDays];
            for (int i = 0; i < WeekDays; i++)
            {
                int d = first + i;
                tiles[i] = new GiftTile
                {
                    Day = d, Golden = GiftAt(d), Today = d == today,
                    Claimed = d < today || (d == today && !waiting),
                    Big = d % WeekDays == 0 && bigShown
                };
            }
            return tiles;
        }

        public static int ClaimDailyGift(GameState s, long day) => ClaimDailyGift(s, day, null, null, out _, out _);

        public static int ClaimDailyGift(GameState s, long day, string name, out Rabbit seasonRabbit) => ClaimDailyGift(s, day, name, null, out seasonRabbit, out _);

        public static int ClaimDailyGift(GameState s, long day, string name, IReadOnlyList<ItemDef> items, out Rabbit seasonRabbit, out BigGift big)
        {
            seasonRabbit = null;
            big = default;
            if (!GiftWaiting(s, day)) return 0;
            int streak = StreakFor(s, day);
            int gift = GiftAt(streak);
            if (IsBigDay(s, streak))
            {
                big = PayBigGift(s, items, day);
                gift += big.Golden;
            }
            s.GoldenCarrots += gift;
            s.GiftDay = day;
            s.GiftStreak = streak;
            s.DailyGifts++;
            Season season = Seasons.SeasonOf(day);
            if (Seasons.IsLastDay(day) && !Seasons.HasSeasonRabbit(s, season))
                seasonRabbit = Rabbits.ArriveKind(s, Seasons.KindOf(season).Id, name ?? Seasons.KindOf(season).NameVi);
            return gift;
        }

        static BigGift PayBigGift(GameState s, IReadOnlyList<ItemDef> items, long day)
        {
            var big = new BigGift { Given = true, Clovers = BigGiftClovers };
            ItemDef hat = Items.DefOf(items, BigGiftHat);
            if (hat != null && !Items.Owns(s, BigGiftHat)) big.Hat = Items.Grant(s, hat, hat.Tier);
            else big.Golden = hat != null && hat.Price > 0 ? hat.Price : BigGiftHatGolden;
            s.Clovers += big.Clovers;
            s.GiftBigDay = day;
            s.GiftBigGolden = big.Hat == null;
            return big;
        }

        public const int GoldenSummonCost = 10;
        public static readonly double[] GoldenOdds = { 0.25, 0.35, 0.22, 0.12, 0.05, 0.01 };

        public static Rabbit SummonGolden(GameState s, double roll, string name, double kindRoll, long day)
        {
            int tier = Rabbits.RollTier(roll, GoldenOdds);
            RabbitKind seasonKind = Seasons.KindOf(Seasons.SeasonOf(day));
            if (seasonKind != null && tier == seasonKind.Tier)
            {
                if (kindRoll >= Seasons.GoldenCallShare) return SummonGolden(s, roll, name, (kindRoll - Seasons.GoldenCallShare) / (1d - Seasons.GoldenCallShare));
                if (s.GoldenCarrots < GoldenSummonCost) return null;
                s.GoldenCarrots -= GoldenSummonCost;
                s.GoldenCalls++;
                return Rabbits.ArriveKind(s, seasonKind.Id, name);
            }
            return SummonGolden(s, roll, name, kindRoll);
        }

        public static Rabbit SummonGolden(GameState s, double roll, string name, double kindRoll = 0d)
        {
            if (s.GoldenCarrots < GoldenSummonCost) return null;
            s.GoldenCarrots -= GoldenSummonCost;
            s.GoldenCalls++;
            return Rabbits.Arrive(s, Rabbits.RollTier(roll, GoldenOdds), name, kindRoll);
        }

        public static readonly double[] VisitorTierOdds = { 0.60, 0.30, 0.09, 0.01 };
        public const int VisitorFirstTier = 2;
        public static readonly int[] VisitorPrices = { 15, 40, 120, 400 };
        public const int UnseenWeight = 3;

        public struct Visitor
        {
            public long Day;
            public int Tier;
            public string Kind;
            public int Price;
            public bool Bought;
            public bool Seen;
        }

        static double Roll(long day, string code, int salt)
        {
            ulong h = 1469598103934665603UL;
            unchecked
            {
                h ^= (ulong)day; h *= 1099511628211UL;
                h ^= (ulong)salt; h *= 1099511628211UL;
                foreach (char c in code ?? "") { h ^= c; h *= 1099511628211UL; }
                h ^= h >> 29; h *= 0x9E3779B97F4A7C15UL; h ^= h >> 32;
            }
            return (h >> 11) / (double)(1UL << 53);
        }

        public static string PickKind(GameState s, int tier, double roll)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(tier);
            if (kinds.Count == 0) return RabbitKinds.DefaultFor(tier);
            double total = 0d;
            foreach (RabbitKind kind in kinds) total += s.SeenKinds.Contains(kind.Id) ? 1d : UnseenWeight;
            double at = Math.Max(0d, Math.Min(0.999999d, roll)) * total;
            foreach (RabbitKind kind in kinds)
            {
                at -= s.SeenKinds.Contains(kind.Id) ? 1d : UnseenWeight;
                if (at < 0d) return kind.Id;
            }
            return kinds[kinds.Count - 1].Id;
        }

        public static Visitor VisitorFor(GameState s, long day)
        {
            string code = s.Profile != null ? s.Profile.Code : "";
            int tier = VisitorFirstTier + Rabbits.RollTier(Roll(day, code, 1), VisitorTierOdds);
            tier = Math.Min(tier, Rabbits.TierCount - 1);
            RabbitKind seasonKind = Seasons.KindOf(Seasons.SeasonOf(day));
            bool seasonal = seasonKind != null && Roll(day, code, 3) < Seasons.VisitorShare;
            if (seasonal) tier = seasonKind.Tier;
            return new Visitor
            {
                Day = day, Tier = tier, Kind = seasonal ? seasonKind.Id : PickKind(s, tier, Roll(day, code, 2)),
                Price = VisitorPrices[Math.Min(tier - VisitorFirstTier, VisitorPrices.Length - 1)],
                Bought = s.VisitorBoughtDay == day, Seen = s.VisitorSeenDay == day
            };
        }

        public static Rabbit BuyVisitor(GameState s, long day, string name)
        {
            Visitor v = VisitorFor(s, day);
            if (v.Bought || s.GoldenCarrots < v.Price) return null;
            s.GoldenCarrots -= v.Price;
            s.VisitorBoughtDay = day;
            return Rabbits.ArriveKind(s, v.Kind, name);
        }

        public static Item BuyItem(GameState s, IReadOnlyList<ItemDef> defs, string defId)
        {
            ItemDef def = Items.DefOf(defs, defId);
            if (def == null || def.Price <= 0 || Items.Owns(s, defId) || s.GoldenCarrots < def.Price) return null;
            s.GoldenCarrots -= def.Price;
            return Items.Grant(s, def, def.Tier);
        }

        public struct Dye
        {
            public string Id;
            public string Hex;
            public Dye(string id, string hex) { Id = id; Hex = hex; }
        }

        public const int DyeCost = 8;

        public static readonly Dye[] Dyes =
        {
            new Dye("hong", "F28BB5"), new Dye("dao", "FFB088"), new Dye("vang-chanh", "F5E36B"), new Dye("bac-ha", "7FD9B0"),
            new Dye("xanh-troi", "7EC2F0"), new Dye("oai-huong", "B49BE0"), new Dye("kem", "FFF0C8"), new Dye("than", "6A6478")
        };

        public static bool DyeRabbit(GameState s, long rabbitId, int dye)
        {
            Rabbit r = Rabbits.Find(s, rabbitId);
            if (r == null || dye < 0 || dye >= Dyes.Length || r.Dye == dye || s.GoldenCarrots < DyeCost) return false;
            s.GoldenCarrots -= DyeCost;
            r.Dye = dye;
            return true;
        }

        public static bool Wash(GameState s, long rabbitId)
        {
            Rabbit r = Rabbits.Find(s, rabbitId);
            if (r == null || r.Dye < 0) return false;
            r.Dye = -1;
            return true;
        }

        public const int GoldenPerFifthLevel = 2;
        public static int GoldenForLevels(int from, int to)
        {
            int n = 0;
            for (int level = from + 1; level <= to; level++) if (level % 5 == 0) n += GoldenPerFifthLevel;
            return n;
        }
    }
}

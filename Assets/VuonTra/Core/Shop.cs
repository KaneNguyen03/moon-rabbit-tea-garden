using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The tea shop by the pond: a gift every day that grows with the streak, and calling a rabbit with
    /// golden carrots at better odds. Golden carrots are only ever earned here and in play; nothing turns
    /// them into ordinary carrots or back.
    /// </summary>
    public static class Shop
    {
        /// <summary>The daily gift by streak day: 1, 1, 2, 2, 3, 3, 5, then 3, 3, 5 again and again. A missed day starts over.</summary>
        static readonly int[] GiftChain = { 1, 1, 2, 2, 3, 3, 5 };
        const int GiftLoopFrom = 4; // after day 7 the chain repeats from day 5

        public static int GiftAt(int streakDay)
        {
            if (streakDay < 1) streakDay = 1;
            if (streakDay <= GiftChain.Length) return GiftChain[streakDay - 1];
            int loop = GiftChain.Length - GiftLoopFrom;
            return GiftChain[GiftLoopFrom + (streakDay - GiftChain.Length - 1) % loop];
        }

        /// <summary>True while today's gift is still to be had. A clock set back to a day already paid gets nothing.</summary>
        public static bool GiftWaiting(GameState s, long day) => day > s.GiftDay;

        /// <summary>The streak day today would be: one more when yesterday was paid, else back to one.</summary>
        public static int StreakFor(GameState s, long day) => s.GiftDay >= 0 && day == s.GiftDay + 1 ? s.GiftStreak + 1 : 1;

        /// <summary>Pays today's gift once. How many golden carrots it brought, 0 when today is already paid or the clock went back.</summary>
        public static int ClaimDailyGift(GameState s, long day)
        {
            if (!GiftWaiting(s, day)) return 0;
            int streak = StreakFor(s, day);
            int gift = GiftAt(streak);
            s.GoldenCarrots += gift;
            s.GiftDay = day;
            s.GiftStreak = streak;
            return gift;
        }

        /// <summary>Calling a rabbit with golden carrots: the price, and the odds that favour the rarer tiers.</summary>
        public const int GoldenSummonCost = 10;
        public static readonly double[] GoldenOdds = { 0.25, 0.35, 0.22, 0.12, 0.05, 0.01 };

        /// <summary>Spends golden carrots for one new rabbit at the shop's odds; null when there are not enough.</summary>
        public static Rabbit SummonGolden(GameState s, double roll, string name, double kindRoll = 0d)
        {
            if (s.GoldenCarrots < GoldenSummonCost) return null;
            s.GoldenCarrots -= GoldenSummonCost;
            return Rabbits.Arrive(s, Rabbits.RollTier(roll, GoldenOdds), name, kindRoll);
        }

        // ----- the rabbit that visits the shop -----

        /// <summary>The visitor's tier is Blue or rarer: 60 / 30 / 9 / 1 for Blue, Purple, Gold, Red.</summary>
        public static readonly double[] VisitorTierOdds = { 0.60, 0.30, 0.09, 0.01 };
        public const int VisitorFirstTier = 2;
        /// <summary>What the visitor costs, by tier from Blue up.</summary>
        public static readonly int[] VisitorPrices = { 15, 40, 120, 400 };
        /// <summary>A kind not yet in the album is this many times likelier to come than one that is.</summary>
        public const int UnseenWeight = 3;

        /// <summary>The rabbit at the shop today: who it is, what it costs, and whether it has been bought.</summary>
        public struct Visitor
        {
            public long Day;
            public int Tier;
            public string Kind;
            public int Price;
            public bool Bought;
            public bool Seen;
        }

        /// <summary>A number in [0, 1) from the day and the player code, the same every time it is asked for.</summary>
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

        /// <summary>The kind the visitor is, from a roll: kinds the album lacks weigh three times a kind it has.</summary>
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

        /// <summary>Who is at the shop on <paramref name="day"/>: the same answer all day for this garden.</summary>
        public static Visitor VisitorFor(GameState s, long day)
        {
            string code = s.Profile != null ? s.Profile.Code : "";
            int tier = VisitorFirstTier + Rabbits.RollTier(Roll(day, code, 1), VisitorTierOdds);
            tier = Math.Min(tier, Rabbits.TierCount - 1);
            return new Visitor
            {
                Day = day, Tier = tier, Kind = PickKind(s, tier, Roll(day, code, 2)),
                Price = VisitorPrices[Math.Min(tier - VisitorFirstTier, VisitorPrices.Length - 1)],
                Bought = s.VisitorBoughtDay == day, Seen = s.VisitorSeenDay == day
            };
        }

        /// <summary>Buys today's visitor, once, while it is still today. The rabbit, or null when it cannot be bought.</summary>
        public static Rabbit BuyVisitor(GameState s, long day, string name)
        {
            Visitor v = VisitorFor(s, day);
            if (v.Bought || s.GoldenCarrots < v.Price) return null;
            s.GoldenCarrots -= v.Price;
            s.VisitorBoughtDay = day;
            return Rabbits.ArriveKind(s, v.Kind, name);
        }

        /// <summary>Golden carrots the garden brings at every level that divides by five.</summary>
        public const int GoldenPerFifthLevel = 3;
        public static int GoldenForLevels(int from, int to)
        {
            int n = 0;
            for (int level = from + 1; level <= to; level++) if (level % 5 == 0) n += GoldenPerFifthLevel;
            return n;
        }
    }
}

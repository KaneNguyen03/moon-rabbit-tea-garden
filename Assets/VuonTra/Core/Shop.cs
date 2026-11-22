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

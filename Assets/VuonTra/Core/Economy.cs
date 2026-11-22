using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>The price and income formulas. Pure functions, no state.</summary>
    public static class Economy
    {
        public static readonly int[] Milestones = { 10, 25, 50, 100, 150, 200, 300, 400, 500 };
        /// <summary>A station goes no higher than its last milestone.</summary>
        public const int MaxLevel = 500;

        /// <summary>How many levels a station can still gain; never negative.</summary>
        public static int LevelsLeft(int level) => Math.Max(0, MaxLevel - level);

        const int MaxBuyCount = 1_000_000;

        /// <summary>Doubles at every milestone level reached.</summary>
        public static double MilestoneMultiplier(int level)
        {
            double m = 1d;
            for (int i = 0; i < Milestones.Length; i++)
                if (level >= Milestones[i]) m *= 2d;
            return m;
        }

        /// <summary>The levels at which a station visibly changes rank: bunting, stone lanterns, then a golden halo (#49).</summary>
        public static readonly int[] Ranks = { 25, 100, 300 };

        /// <summary>How many of the three visible ranks a station of this level has reached, 0 to 3.</summary>
        public static int Rank(int level)
        {
            int rank = 0;
            for (int i = 0; i < Ranks.Length; i++)
                if (level >= Ranks[i]) rank++;
            return rank;
        }

        /// <summary>The next milestone above this level, or 0 when none is left.</summary>
        public static int NextMilestone(int level)
        {
            for (int i = 0; i < Milestones.Length; i++)
                if (level < Milestones[i]) return Milestones[i];
            return 0;
        }

        /// <summary>The milestone at or below this level, or 0.</summary>
        public static int PreviousMilestone(int level)
        {
            int prev = 0;
            for (int i = 0; i < Milestones.Length; i++)
                if (level >= Milestones[i]) prev = Milestones[i];
            return prev;
        }

        /// <summary>
        /// Price of the next <paramref name="count"/> levels from <paramref name="level"/>.
        /// Unlocking (level 0, count 1) costs exactly BaseCost.
        /// </summary>
        public static BigNum CostOfNext(StationDef d, int level, int count)
        {
            if (count <= 0) return BigNum.Zero;
            var growth = new BigNum(d.CostGrowth);
            BigNum first = growth.Pow(level) * d.BaseCost;
            if (count == 1) return first;
            BigNum series = (growth.Pow(count) - BigNum.One) / (d.CostGrowth - 1d);
            return first * series;
        }

        /// <summary>The most levels these coins can buy from this level.</summary>
        public static int MaxAffordable(StationDef d, int level, BigNum coins)
        {
            int left = LevelsLeft(level);
            if (coins.IsZero || left == 0) return 0;
            BigNum first = CostOfNext(d, level, 1);
            if (coins < first) return 0;

            BigNum ratio = coins * (d.CostGrowth - 1d) / first + BigNum.One;
            double estimate = Math.Floor(ratio.Log10() / Math.Log10(d.CostGrowth));
            int n = estimate >= MaxBuyCount ? MaxBuyCount : Math.Max(1, (int)estimate);

            // The closed form can be off by one from rounding; settle it against the real price.
            while (n > 1 && CostOfNext(d, level, n) > coins) n--;
            while (n < MaxBuyCount && CostOfNext(d, level, n + 1) <= coins) n++;
            return Math.Min(n, left);
        }

        /// <summary>Price multiplier an Ingredient station gives the Brew stations it supplies.</summary>
        public static double IngredientBoost(int level)
        {
            return level <= 0 ? 1d : 1d + 0.10d * level * MilestoneMultiplier(level);
        }

        /// <summary>Sương one finished cycle pays. Zero for Ingredient or locked stations.</summary>
        public static BigNum IncomePerCycle(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            var d = defs[index];
            int level = s.Levels[index];
            if (d.Kind != StationKind.Brew || level <= 0) return BigNum.Zero;

            var income = new BigNum(d.BaseIncome * level * MilestoneMultiplier(level));
            for (int i = 0; i < defs.Count; i++)
            {
                var other = defs[i];
                if (other.Kind != StationKind.Ingredient || s.Levels[i] <= 0) continue;
                // A rabbit at an ingredient station strengthens the bonus it gives, not the base 1.
                if (other.Boosts(d.Id))
                    income *= 1d + (IngredientBoost(s.Levels[i]) - 1d) * Rabbits.StationMultiplier(defs, s, i);
            }
            return income * Rabbits.StationMultiplier(defs, s, index);
        }

        /// <summary>
        /// One station's share of the income, a second, with the two factors that shaped it: the rabbit at
        /// work there and the ingredient stations that feed it. Zero, and both factors 1, where nothing is earned.
        /// </summary>
        public static BigNum StationIncomePerSecond(IReadOnlyList<StationDef> defs, GameState s, int index, out double rabbit, out double ingredients)
        {
            rabbit = 1d;
            ingredients = 1d;
            var d = defs[index];
            if (d.Kind != StationKind.Brew || s.Levels[index] <= 0 || d.CycleSeconds <= 0d) return BigNum.Zero;
            rabbit = Rabbits.StationMultiplier(defs, s, index);
            for (int i = 0; i < defs.Count; i++)
            {
                var other = defs[i];
                if (other.Kind == StationKind.Ingredient && s.Levels[i] > 0 && other.Boosts(d.Id))
                    ingredients *= 1d + (IngredientBoost(s.Levels[i]) - 1d) * Rabbits.StationMultiplier(defs, s, i);
            }
            return IncomePerCycle(defs, s, index) / d.CycleSeconds;
        }

        public static BigNum IncomePerSecond(IReadOnlyList<StationDef> defs, GameState s)
        {
            BigNum total = BigNum.Zero;
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                if (d.Kind != StationKind.Brew || s.Levels[i] <= 0 || d.CycleSeconds <= 0d) continue;
                total += IncomePerCycle(defs, s, i) / d.CycleSeconds;
            }
            return total;
        }

        /// <summary>A station can be opened once the one before it is open.</summary>
        public static bool CanUnlock(GameState s, int index)
        {
            if (index < 0 || index >= s.Levels.Length) return false;
            if (index >= s.Plots) return false; // no plot for it yet
            return index == 0 || s.Levels[index - 1] > 0;
        }
    }
}

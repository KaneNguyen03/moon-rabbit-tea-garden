using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Economy
    {
        public static readonly int[] Milestones = { 10, 25, 50, 100, 150, 200, 300, 400, 500 };
        public const int MaxLevel = 500;

        public static int LevelsLeft(int level) => Math.Max(0, MaxLevel - level);

        const int MaxBuyCount = 1_000_000;

        public static double MilestoneMultiplier(int level)
        {
            double m = 1d;
            for (int i = 0; i < Milestones.Length; i++)
                if (level >= Milestones[i]) m *= 2d;
            return m;
        }

        public static readonly int[] Ranks = { 25, 100, 300 };

        public static int Rank(int level)
        {
            int rank = 0;
            for (int i = 0; i < Ranks.Length; i++)
                if (level >= Ranks[i]) rank++;
            return rank;
        }

        public static int NextMilestone(int level)
        {
            for (int i = 0; i < Milestones.Length; i++)
                if (level < Milestones[i]) return Milestones[i];
            return 0;
        }

        public static int PreviousMilestone(int level)
        {
            int prev = 0;
            for (int i = 0; i < Milestones.Length; i++)
                if (level >= Milestones[i]) prev = Milestones[i];
            return prev;
        }

        public static BigNum CostOfNext(StationDef d, int level, int count)
        {
            if (count <= 0) return BigNum.Zero;
            var growth = new BigNum(d.CostGrowth);
            BigNum first = growth.Pow(level) * d.BaseCost;
            if (count == 1) return first;
            BigNum series = (growth.Pow(count) - BigNum.One) / (d.CostGrowth - 1d);
            return first * series;
        }

        public static int MaxAffordable(StationDef d, int level, BigNum coins)
        {
            int left = LevelsLeft(level);
            if (coins.IsZero || left == 0) return 0;
            BigNum first = CostOfNext(d, level, 1);
            if (coins < first) return 0;

            BigNum ratio = coins * (d.CostGrowth - 1d) / first + BigNum.One;
            double estimate = Math.Floor(ratio.Log10() / Math.Log10(d.CostGrowth));
            int n = estimate >= MaxBuyCount ? MaxBuyCount : Math.Max(1, (int)estimate);

            while (n > 1 && CostOfNext(d, level, n) > coins) n--;
            while (n < MaxBuyCount && CostOfNext(d, level, n + 1) <= coins) n++;
            return Math.Min(n, left);
        }

        public static double IngredientBoost(int level)
        {
            return level <= 0 ? 1d : 1d + 0.10d * level * MilestoneMultiplier(level);
        }

        public static double IngredientBoost(StationDef d, int level)
        {
            return 1d + (IngredientBoost(level) - 1d) * d.BoostScale;
        }

        public static BigNum IncomePerCycle(IReadOnlyList<StationDef> defs, GameState s, int index) => IncomePerCycle(defs, s, index, AlbumSets.Factors(s));

        static BigNum IncomePerCycle(IReadOnlyList<StationDef> defs, GameState s, int index, in AlbumFactors album)
        {
            var d = defs[index];
            int level = s.Levels[index];
            if (d.Kind != StationKind.Brew || level <= 0 || Layout.IsStored(defs, s, index)) return BigNum.Zero;

            var income = new BigNum(d.BaseIncome * level * MilestoneMultiplier(level));
            for (int i = 0; i < defs.Count; i++)
            {
                var other = defs[i];
                if (other.Kind != StationKind.Ingredient || s.Levels[i] <= 0 || Layout.IsStored(defs, s, i)) continue;
                if (other.Boosts(d.Id))
                    income *= 1d + (IngredientBoost(other, s.Levels[i]) - 1d) * Rabbits.StationMultiplier(defs, s, i) * album.At(defs, i);
            }
            return income * Rabbits.StationMultiplier(defs, s, index) * album.At(defs, index);
        }

        public static BigNum StationIncomePerSecond(IReadOnlyList<StationDef> defs, GameState s, int index, out double rabbit, out double ingredients)
        {
            rabbit = 1d;
            ingredients = 1d;
            var d = defs[index];
            if (d.Kind != StationKind.Brew || s.Levels[index] <= 0 || d.CycleSeconds <= 0d || Layout.IsStored(defs, s, index)) return BigNum.Zero;
            rabbit = Rabbits.StationMultiplier(defs, s, index);
            AlbumFactors album = AlbumSets.Factors(s);
            for (int i = 0; i < defs.Count; i++)
            {
                var other = defs[i];
                if (other.Kind == StationKind.Ingredient && s.Levels[i] > 0 && other.Boosts(d.Id) && !Layout.IsStored(defs, s, i))
                    ingredients *= 1d + (IngredientBoost(other, s.Levels[i]) - 1d) * Rabbits.StationMultiplier(defs, s, i) * album.At(defs, i);
            }
            return IncomePerCycle(defs, s, index, album) / d.CycleSeconds;
        }

        public static BigNum IncomePerSecond(IReadOnlyList<StationDef> defs, GameState s)
        {
            BigNum total = BigNum.Zero;
            AlbumFactors album = AlbumSets.Factors(s);
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                if (d.Kind != StationKind.Brew || s.Levels[i] <= 0 || d.CycleSeconds <= 0d || Layout.IsStored(defs, s, i)) continue;
                total += IncomePerCycle(defs, s, i, album) / d.CycleSeconds;
            }
            return total;
        }

        public static bool CanUnlock(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (index < 0 || index >= s.Levels.Length || index >= defs.Count) return false;
            if (index >= s.Plots) return false;
            if (!HasGardenLevel(defs, s, index)) return false;
            return index == 0 || s.Levels[index - 1] > 0;
        }

        public static bool HasGardenLevel(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            return index < 0 || index >= defs.Count || Garden.Level(s) >= defs[index].NeedGardenLevel;
        }
    }
}

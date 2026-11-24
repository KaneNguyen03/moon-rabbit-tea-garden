using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class UpgradeStep
    {
        public int Station;
        public int Levels;
        public BigNum Cost;
    }

    public static class UpgradePlan
    {
        public const int ChunkLevels = 10;
        public const int MaxSteps = 400;
        public const int UnlockLevel = 15;

        public static bool Unlocked(GameState s) => Garden.Level(s) >= UnlockLevel;

        public static List<UpgradeStep> Plan(IReadOnlyList<StationDef> defs, GameState s, BigNum budget)
        {
            if (!Unlocked(s)) return new List<UpgradeStep>();
            var scratch = new GameState
            {
                Levels = (int[])s.Levels.Clone(), Progress = s.Progress, Rabbits = s.Rabbits, Items = s.Items,
                UpgradeLevels = s.UpgradeLevels, Plots = s.Plots, Coins = budget, DishLevels = s.DishLevels
            };
            var byStation = new Dictionary<int, UpgradeStep>();
            var order = new List<UpgradeStep>();
            BigNum left = budget;
            for (int round = 0; round < MaxSteps; round++)
            {
                double before = Economy.IncomePerSecond(defs, scratch).ToDouble();
                int best = -1, bestCount = 0;
                double bestRatio = 0d;
                BigNum bestCost = BigNum.Zero;
                for (int i = 0; i < defs.Count; i++)
                {
                    int level = scratch.Levels[i];
                    if (level <= 0) continue;
                    int affordable = Economy.MaxAffordable(defs[i], level, left);
                    if (affordable <= 0) continue;
                    int count = Math.Min(Chunk(level), affordable);
                    BigNum cost = Economy.CostOfNext(defs[i], level, count);
                    scratch.Levels[i] = level + count;
                    double gain = Economy.IncomePerSecond(defs, scratch).ToDouble() - before;
                    scratch.Levels[i] = level;
                    double ratio = gain / Math.Max(1e-300, cost.ToDouble());
                    if (ratio > bestRatio)
                    {
                        bestRatio = ratio;
                        best = i;
                        bestCount = count;
                        bestCost = cost;
                    }
                }
                if (best < 0) break;
                scratch.Levels[best] += bestCount;
                left -= bestCost;
                if (!byStation.TryGetValue(best, out UpgradeStep step))
                {
                    step = new UpgradeStep { Station = best, Cost = BigNum.Zero };
                    byStation[best] = step;
                    order.Add(step);
                }
                step.Levels += bestCount;
                step.Cost += bestCost;
            }
            return order;
        }

        static int Chunk(int level)
        {
            int next = Economy.NextMilestone(level);
            return next > 0 ? Math.Min(ChunkLevels, next - level) : ChunkLevels;
        }

        public static BigNum Total(IReadOnlyList<UpgradeStep> plan)
        {
            BigNum total = BigNum.Zero;
            foreach (UpgradeStep step in plan) total += step.Cost;
            return total;
        }

        public static bool Apply(IReadOnlyList<StationDef> defs, GameState s, IReadOnlyList<UpgradeStep> plan)
        {
            if (!Unlocked(s)) return false;
            BigNum total = BigNum.Zero;
            foreach (UpgradeStep step in plan)
            {
                if (step.Station < 0 || step.Station >= defs.Count || s.Levels[step.Station] <= 0 || step.Levels <= 0) return false;
                total += Economy.CostOfNext(defs[step.Station], s.Levels[step.Station], step.Levels);
            }
            if (plan.Count == 0 || s.Coins < total) return false;
            foreach (UpgradeStep step in plan)
            {
                int level = s.Levels[step.Station];
                s.Coins -= Economy.CostOfNext(defs[step.Station], level, step.Levels);
                s.Levels[step.Station] = level + step.Levels;
                s.LevelsBought += step.Levels;
                Garden.AddXp(s, step.Levels * Garden.XpPerLevelBought + Garden.MilestonesCrossed(level, level + step.Levels) * Garden.XpMilestone);
            }
            return true;
        }
    }
}

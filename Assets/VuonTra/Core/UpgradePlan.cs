using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>One line of an upgrade-everything plan: this station, this many levels, for this much.</summary>
    public sealed class UpgradeStep
    {
        public int Station;
        public int Levels;
        public BigNum Cost;
    }

    /// <summary>
    /// Spends a budget across the open stations, a chunk at a time, always on the station that adds the
    /// most income for each carrot. Applying the plan is the same as buying each step's levels one by one
    /// in the same order.
    /// </summary>
    public static class UpgradePlan
    {
        /// <summary>A chunk never reaches past the next doubling, and is at most this many levels.</summary>
        public const int ChunkLevels = 10;
        /// <summary>How many chunks a plan may contain; the budget usually runs out long before.</summary>
        public const int MaxSteps = 400;

        public static List<UpgradeStep> Plan(IReadOnlyList<StationDef> defs, GameState s, BigNum budget)
        {
            // A scratch garden with its own levels and purse; everything else is shared and only read.
            var scratch = new GameState
            {
                Levels = (int[])s.Levels.Clone(), Progress = s.Progress, Rabbits = s.Rabbits, Items = s.Items,
                UpgradeLevels = s.UpgradeLevels, Plots = s.Plots, Coins = budget
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

        /// <summary>Levels up to the next doubling, at most <see cref="ChunkLevels"/>.</summary>
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

        /// <summary>
        /// Buys every step, in order, at today's prices. Changes nothing and returns false when the purse
        /// cannot cover the whole plan or a station in it is not open.
        /// </summary>
        public static bool Apply(IReadOnlyList<StationDef> defs, GameState s, IReadOnlyList<UpgradeStep> plan)
        {
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

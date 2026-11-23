using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Simulation
    {
        public const double TapSeconds = 0.4;
        public const double AwayCapSeconds = 10800;
        public const double AwayMinSeconds = 30;

        public static void Tick(IReadOnlyList<StationDef> defs, GameState s, double dt, Action<int, BigNum> onPayout = null, Modifiers? mods = null,
            Func<double> roll = null, Action<int> onProduct = null)
        {
            if (!(dt > 0d)) return;
            Modifiers m = mods ?? Modifiers.None;
            double work = Energy.Advance(s, dt);
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                if (d.Kind != StationKind.Brew || s.Levels[i] <= 0 || d.CycleSeconds <= 0d || Layout.IsStored(defs, s, i)) continue;

                Tray.Advance(s, i, work * m.SpeedMultiplier, roll, onProduct);
                double p = s.Progress[i] + work * m.SpeedMultiplier / d.CycleSeconds;
                if (p >= 1d)
                {
                    double cycles = Math.Floor(p);
                    p -= cycles;
                    BigNum paid = Economy.IncomePerCycle(defs, s, i) * (cycles * m.IncomeMultiplier);
                    s.Coins += paid;
                    s.TotalEarned += paid;
                    onPayout?.Invoke(i, paid);
                }
                s.Progress[i] = (p >= 0d && p < 1d) ? p : 0d;
            }
        }

        public static bool Tap(IReadOnlyList<StationDef> defs, GameState s, int index, Modifiers? mods = null)
        {
            if (index < 0 || index >= defs.Count) return false;
            var d = defs[index];
            if (d.Kind != StationKind.Brew || s.Levels[index] <= 0 || d.CycleSeconds <= 0d) return false;
            s.Progress[index] += (mods ?? Modifiers.None).TapSeconds / d.CycleSeconds;
            s.TotalTaps++;
            return true;
        }

        public const int BuyOne = 1, BuyTen = 10, BuyMax = 0, BuyToMilestone = -1;

        public static BigNum Budget(GameState s) => s.Coins * Math.Max(0.05d, Math.Min(1d, s.SpendShare));

        public static int BuyCount(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            int level = s.Levels[index];
            if (level == 0) return 1;
            return Math.Min(Economy.LevelsLeft(level), BuyCountUncapped(defs, s, index));
        }

        static int BuyCountUncapped(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            int level = s.Levels[index];
            switch (s.BuyMode)
            {
                case BuyMax:
                    return Math.Max(1, Economy.MaxAffordable(defs[index], level, Budget(s)));
                case BuyToMilestone:
                {
                    int next = Economy.NextMilestone(level);
                    int toNext = next > 0 ? next - level : int.MaxValue;
                    return Math.Max(1, Math.Min(toNext, Economy.MaxAffordable(defs[index], level, Budget(s))));
                }
                case BuyTen:
                    return 10;
                default:
                    return 1;
            }
        }

        public static bool TryBuy(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (index < 0 || index >= defs.Count) return false;
            int level = s.Levels[index];
            if (level == 0 && !Economy.CanUnlock(defs, s, index)) return false;

            int count = BuyCount(defs, s, index);
            if (count <= 0) return false;
            BigNum cost = Economy.CostOfNext(defs[index], level, count);
            if (s.Coins < cost) return false;

            s.Coins -= cost;
            s.Levels[index] = level + count;
            s.LevelsBought += count;
            Garden.AddXp(s, (level == 0 ? Garden.XpStationOpened : count * Garden.XpPerLevelBought)
                + Garden.MilestonesCrossed(level, level + count) * Garden.XpMilestone);
            if (level == 0) s.Progress[index] = 0d;
            return true;
        }

        public static BigNum ClaimAway(IReadOnlyList<StationDef> defs, GameState s, long nowUnixSeconds, out double countedSeconds, Modifiers? mods = null)
        {
            Modifiers m = mods ?? Modifiers.None;
            long elapsed = nowUnixSeconds - s.LastSeenUnixSeconds;
            s.LastSeenUnixSeconds = nowUnixSeconds;
            countedSeconds = 0d;
            if (elapsed < AwayMinSeconds) return BigNum.Zero;

            countedSeconds = Math.Min(elapsed, m.AwayCapSeconds);
            Energy.PassAway(s, elapsed);
            Tray.FillAway(defs, s, countedSeconds);
            double awayBonus = m.AwayIncomeMultiplier > 0d ? m.AwayIncomeMultiplier : 1d;
            BigNum paid = Economy.IncomePerSecond(defs, s) * (countedSeconds * m.IncomeMultiplier * m.SpeedMultiplier * awayBonus);
            s.Coins += paid;
            s.TotalEarned += paid;
            return paid;
        }
    }
}

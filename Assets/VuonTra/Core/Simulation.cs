using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>The things that change a GameState: time passing, taps, purchases, coming back.</summary>
    public static class Simulation
    {
        /// <summary>Seconds of work one tap adds to a station.</summary>
        public const double TapSeconds = 0.4;
        /// <summary>The longest absence that still pays.</summary>
        /// <summary>Time away is paid for up to three hours (#69), more with the Long Sleep charm.</summary>
        public const double AwayCapSeconds = 10800;
        /// <summary>Absences shorter than this pay nothing.</summary>
        public const double AwayMinSeconds = 30;

        /// <summary>Advances every open Brew station and pays each finished cycle.</summary>
        /// <param name="roll">A number in [0, 1) when asked: decides golden batches. Null never makes one.</param>
        /// <param name="onProduct">A station finished a special batch for the tray.</param>
        public static void Tick(IReadOnlyList<StationDef> defs, GameState s, double dt, Action<int, BigNum> onPayout = null, Modifiers? mods = null,
            Func<double> roll = null, Action<int> onProduct = null)
        {
            if (!(dt > 0d)) return; // also rejects NaN
            Modifiers m = mods ?? Modifiers.None;
            double work = Energy.Advance(s, dt); // a running burst makes these seconds count double
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                if (d.Kind != StationKind.Brew || s.Levels[i] <= 0 || d.CycleSeconds <= 0d) continue;

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

        /// <summary>A tap nudges an open Brew station forward. Payout comes on the next Tick.</summary>
        public static bool Tap(IReadOnlyList<StationDef> defs, GameState s, int index, Modifiers? mods = null)
        {
            if (index < 0 || index >= defs.Count) return false;
            var d = defs[index];
            if (d.Kind != StationKind.Brew || s.Levels[index] <= 0 || d.CycleSeconds <= 0d) return false;
            s.Progress[index] += (mods ?? Modifiers.None).TapSeconds / d.CycleSeconds;
            s.TotalTaps++;
            return true;
        }

        /// <summary>The buy modes, as the numbers the save file has always kept them in.</summary>
        public const int BuyOne = 1, BuyTen = 10, BuyMax = 0, BuyToMilestone = -1;

        /// <summary>What a quick upgrade may spend: the share of the purse the player chose to put at risk.</summary>
        public static BigNum Budget(GameState s) => s.Coins * Math.Max(0.05d, Math.Min(1d, s.SpendShare));

        /// <summary>How many levels the buy button would purchase right now: at least one, except at the cap, where it is none.</summary>
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
                    // Just enough to reach the next doubling; what can be paid for when that is out of reach.
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

        /// <summary>Buys levels (or unlocks). Leaves the state untouched when it cannot.</summary>
        public static bool TryBuy(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (index < 0 || index >= defs.Count) return false;
            int level = s.Levels[index];
            if (level == 0 && !Economy.CanUnlock(defs, s, index)) return false;

            int count = BuyCount(defs, s, index);
            if (count <= 0) return false; // at the cap
            BigNum cost = Economy.CostOfNext(defs[index], level, count);
            if (s.Coins < cost) return false;

            s.Coins -= cost;
            s.Levels[index] = level + count;
            s.LevelsBought += count;
            // The garden grows with its stations: each level a little, opening one and passing a milestone more.
            Garden.AddXp(s, (level == 0 ? Garden.XpStationOpened : count * Garden.XpPerLevelBought)
                + Garden.MilestonesCrossed(level, level + count) * Garden.XpMilestone);
            if (level == 0) s.Progress[index] = 0d;
            return true;
        }

        /// <summary>
        /// Pays for the time since the game was last seen, up to the cap, and stamps the new time.
        /// A clock that moved backwards counts as no time at all.
        /// </summary>
        public static BigNum ClaimAway(IReadOnlyList<StationDef> defs, GameState s, long nowUnixSeconds, out double countedSeconds, Modifiers? mods = null)
        {
            Modifiers m = mods ?? Modifiers.None;
            long elapsed = nowUnixSeconds - s.LastSeenUnixSeconds;
            s.LastSeenUnixSeconds = nowUnixSeconds;
            countedSeconds = 0d;
            if (elapsed < AwayMinSeconds) return BigNum.Zero;

            countedSeconds = Math.Min(elapsed, m.AwayCapSeconds);
            Energy.PassAway(s, elapsed);
            Tray.FillAway(defs, s, countedSeconds); // the tray fills up to what it holds, and no further
            double awayBonus = m.AwayIncomeMultiplier > 0d ? m.AwayIncomeMultiplier : 1d;
            BigNum paid = Economy.IncomePerSecond(defs, s) * (countedSeconds * m.IncomeMultiplier * m.SpeedMultiplier * awayBonus);
            s.Coins += paid;
            s.TotalEarned += paid;
            return paid;
        }
    }
}

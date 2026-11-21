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
        public const double AwayCapSeconds = 7200;
        /// <summary>Absences shorter than this pay nothing.</summary>
        public const double AwayMinSeconds = 30;

        /// <summary>Advances every open Brew station and pays each finished cycle.</summary>
        public static void Tick(IReadOnlyList<StationDef> defs, GameState s, double dt, Action<int, BigNum> onPayout = null)
        {
            if (!(dt > 0d)) return; // also rejects NaN
            for (int i = 0; i < defs.Count; i++)
            {
                var d = defs[i];
                if (d.Kind != StationKind.Brew || s.Levels[i] <= 0 || d.CycleSeconds <= 0d) continue;

                double p = s.Progress[i] + dt / d.CycleSeconds;
                if (p >= 1d)
                {
                    double cycles = Math.Floor(p);
                    p -= cycles;
                    BigNum paid = Economy.IncomePerCycle(defs, s, i) * cycles;
                    s.Coins += paid;
                    onPayout?.Invoke(i, paid);
                }
                s.Progress[i] = (p >= 0d && p < 1d) ? p : 0d;
            }
        }

        /// <summary>A tap nudges an open Brew station forward. Payout comes on the next Tick.</summary>
        public static bool Tap(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (index < 0 || index >= defs.Count) return false;
            var d = defs[index];
            if (d.Kind != StationKind.Brew || s.Levels[index] <= 0 || d.CycleSeconds <= 0d) return false;
            s.Progress[index] += TapSeconds / d.CycleSeconds;
            return true;
        }

        /// <summary>How many levels the buy button would purchase right now.</summary>
        public static int BuyCount(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            int level = s.Levels[index];
            if (level == 0) return 1;
            if (s.BuyMode == 0) return Math.Max(1, Economy.MaxAffordable(defs[index], level, s.Coins));
            return s.BuyMode == 10 ? 10 : 1;
        }

        /// <summary>Buys levels (or unlocks). Leaves the state untouched when it cannot.</summary>
        public static bool TryBuy(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (index < 0 || index >= defs.Count) return false;
            int level = s.Levels[index];
            if (level == 0 && !Economy.CanUnlock(s, index)) return false;

            int count = BuyCount(defs, s, index);
            BigNum cost = Economy.CostOfNext(defs[index], level, count);
            if (s.Coins < cost) return false;

            s.Coins -= cost;
            s.Levels[index] = level + count;
            if (level == 0) s.Progress[index] = 0d;
            return true;
        }

        /// <summary>
        /// Pays for the time since the game was last seen, up to the cap, and stamps the new time.
        /// A clock that moved backwards counts as no time at all.
        /// </summary>
        public static BigNum ClaimAway(IReadOnlyList<StationDef> defs, GameState s, long nowUnixSeconds, out double countedSeconds)
        {
            long elapsed = nowUnixSeconds - s.LastSeenUnixSeconds;
            s.LastSeenUnixSeconds = nowUnixSeconds;
            countedSeconds = 0d;
            if (elapsed < AwayMinSeconds) return BigNum.Zero;

            countedSeconds = Math.Min(elapsed, AwayCapSeconds);
            BigNum paid = Economy.IncomePerSecond(defs, s) * countedSeconds;
            s.Coins += paid;
            return paid;
        }
    }
}

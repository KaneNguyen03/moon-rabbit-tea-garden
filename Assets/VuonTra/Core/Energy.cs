using System;

namespace VuonTra.Core
{
    /// <summary>
    /// "Trà lực": a bar that refills by itself and is spent on a short burst where the whole
    /// garden works at double speed. It never punishes: an empty bar just means no burst.
    /// </summary>
    public static class Energy
    {
        public const double Max = 100d;
        /// <summary>Empty to full in ten minutes.</summary>
        public const double RegenPerSecond = Max / 600d;
        public const double BoostCost = 50d;
        public const double BoostSeconds = 30d;
        public const double BoostSpeed = 2d;

        public static bool CanBoost(GameState s) => s.Energy >= BoostCost && s.BoostSecondsLeft <= 0d;

        public static bool TryBoost(GameState s)
        {
            if (!CanBoost(s)) return false;
            s.Energy -= BoostCost;
            s.BoostSecondsLeft = BoostSeconds;
            return true;
        }

        /// <summary>
        /// Moves the bar and any running burst forward by <paramref name="dt"/> seconds and returns
        /// how many seconds of station work that was (burst time counts double).
        /// </summary>
        public static double Advance(GameState s, double dt)
        {
            if (!(dt > 0d)) return 0d;
            s.Energy = Math.Min(Max, Math.Max(0d, s.Energy) + dt * RegenPerSecond);
            double boosted = Math.Min(dt, Math.Max(0d, s.BoostSecondsLeft));
            s.BoostSecondsLeft = Math.Max(0d, s.BoostSecondsLeft - dt);
            return boosted * BoostSpeed + (dt - boosted);
        }

        /// <summary>Time away refills the bar and lets any burst run out, without paying burst speed.</summary>
        public static void PassAway(GameState s, double seconds)
        {
            if (!(seconds > 0d)) return;
            s.Energy = Math.Min(Max, Math.Max(0d, s.Energy) + seconds * RegenPerSecond);
            s.BoostSecondsLeft = Math.Max(0d, s.BoostSecondsLeft - seconds);
        }
    }
}

using System;

namespace VuonTra.Core
{
    public static class Energy
    {
        public const double Max = 100d;
        public const double RegenPerSecond = Max / 600d;
        public const double BoostCost = 50d;
        public const double BoostSeconds = 30d;
        public const double BoostSpeed = 2d;

        public static bool CanBoost(GameState s) => s.Energy >= BoostCost && s.BoostSecondsLeft <= 0d;

        public static double SecondsUntilBoost(GameState s) => Math.Max(0d, (BoostCost - s.Energy) / RegenPerSecond);

        public static bool TryBoost(GameState s)
        {
            if (!CanBoost(s)) return false;
            s.Energy -= BoostCost;
            s.BoostSecondsLeft = BoostSeconds;
            s.Boosts++;
            return true;
        }

        public static double Advance(GameState s, double dt)
        {
            if (!(dt > 0d)) return 0d;
            s.Energy = Math.Min(Max, Math.Max(0d, s.Energy) + dt * RegenPerSecond);
            double boosted = Math.Min(dt, Math.Max(0d, s.BoostSecondsLeft));
            s.BoostSecondsLeft = Math.Max(0d, s.BoostSecondsLeft - dt);
            return boosted * BoostSpeed + (dt - boosted);
        }

        public static void PassAway(GameState s, double seconds)
        {
            if (!(seconds > 0d)) return;
            s.Energy = Math.Min(Max, Math.Max(0d, s.Energy) + seconds * RegenPerSecond);
            s.BoostSecondsLeft = Math.Max(0d, s.BoostSecondsLeft - seconds);
        }
    }
}

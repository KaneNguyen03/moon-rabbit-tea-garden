using System;

namespace VuonTra.Core
{
    public enum DayPhase
    {
        Dusk,
        Night,
        Dawn,
        Day
    }

    /// <summary>
    /// The time of day, from the device's clock, as a look for the garden. Night is the garden's own
    /// look and never fully leaves: by day it only brightens so far. Nothing here changes what is earned.
    /// </summary>
    public static class Daylight
    {
        public const double DuskStart = 17, NightStart = 19, DawnStart = 5, DayStart = 7;
        /// <summary>How much night stays on the garden at midday: it brightens, but it is still a moon garden.</summary>
        public const double DayFloor = 0.35;

        static double Wrap(double hour) => ((hour % 24d) + 24d) % 24d;

        public static DayPhase PhaseAt(double hour)
        {
            hour = Wrap(hour);
            if (hour >= DuskStart && hour < NightStart) return DayPhase.Dusk;
            if (hour >= NightStart || hour < DawnStart) return DayPhase.Night;
            if (hour >= DawnStart && hour < DayStart) return DayPhase.Dawn;
            return DayPhase.Day;
        }

        /// <summary>How much of the night is on the garden: 1 through the night, the floor by day, ramping through dusk and dawn.</summary>
        public static double NightAt(double hour)
        {
            hour = Wrap(hour);
            switch (PhaseAt(hour))
            {
                case DayPhase.Night: return 1d;
                case DayPhase.Day: return DayFloor;
                case DayPhase.Dusk: return DayFloor + (1d - DayFloor) * (hour - DuskStart) / (NightStart - DuskStart);
                default: return 1d - (1d - DayFloor) * (hour - DawnStart) / (DayStart - DawnStart);
            }
        }

        /// <summary>How many of <paramref name="all"/> fireflies are out: all at night, half in the half-light, a few by day.</summary>
        public static int FirefliesAt(double hour, int all)
        {
            switch (PhaseAt(hour))
            {
                case DayPhase.Night: return all;
                case DayPhase.Day: return Math.Max(2, all / 6);
                default: return all / 2;
            }
        }
    }
}

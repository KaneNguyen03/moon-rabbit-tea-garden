using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Weather
    {
        public const int ShowersPerDay = 2;
        public const double ShowerMinutes = 8d;
        public const double FirstHour = 6d, LastHour = 23d;
        public const double GapMinutes = 60d;

        public static double[] ShowerStarts(long day) => (double[])Starts(day).Clone();

        static long _day = long.MinValue;
        static double[] _starts;

        static double[] Starts(long day)
        {
            if (_starts != null && day == _day) return _starts;
            _starts = Draw(day);
            _day = day;
            return _starts;
        }

        static double[] Draw(long day)
        {
            double half = (LastHour - FirstHour) / ShowersPerDay;
            double room = half - (ShowerMinutes + GapMinutes) / 60d;
            var starts = new double[ShowersPerDay];
            for (int k = 0; k < ShowersPerDay; k++)
            {
                double at = FirstHour + k * half + Unit(day, k) * room;
                starts[k] = Math.Floor(at * 60d) / 60d;
            }
            return starts;
        }

        public static int ShowerAt(long day, double hour)
        {
            if (double.IsNaN(hour) || double.IsInfinity(hour)) return -1;
            hour = ((hour % 24d) + 24d) % 24d;
            double[] starts = Starts(day);
            for (int k = 0; k < starts.Length; k++)
                if (hour >= starts[k] && hour < starts[k] + ShowerMinutes / 60d) return k;
            return -1;
        }

        public static bool IsRaining(long day, double hour) => ShowerAt(day, hour) >= 0;

        static double Unit(long day, int shower)
        {
            ulong z = unchecked((ulong)day * 0x9E3779B97F4A7C15UL + (ulong)(shower + 1) * 0xBF58476D1CE4E5B9UL + 0x62UL);
            z = unchecked((z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL);
            z = unchecked((z ^ (z >> 27)) * 0x94D049BB133111EBUL);
            z ^= z >> 31;
            return (z >> 11) * (1d / (1UL << 53));
        }
    }

    public enum FrogEvent
    {
        None,
        Arrived,
        Left
    }

    public static class FrogRabbit
    {
        public const double StaySeconds = 60d;
        public const double RewardSeconds = 600d;
        public const double CloverChance = 0.25d;
        public const int PerDay = Weather.ShowersPerDay;
        public const int FromLevel = 15;

        public static bool Here(GameState s) => s.FrogLeft > 0d;

        public static bool CanCome(GameState s) => Tutorial.IsDone(s) && Garden.Level(s) >= FromLevel;

        public static FrogEvent Advance(GameState s, double dt, long day, double hour)
        {
            if (!(dt > 0d)) return FrogEvent.None;
            if (day < s.FrogDay)
            {
                if (!Here(s)) return FrogEvent.None;
                s.FrogLeft = 0d;
                return FrogEvent.Left;
            }
            if (day > s.FrogDay)
            {
                s.FrogDay = day;
                s.FrogShowers = 0;
                s.FrogsToday = 0;
            }
            if (Here(s))
            {
                s.FrogLeft -= dt;
                if (s.FrogLeft > 0d && Weather.IsRaining(day, hour)) return FrogEvent.None;
                s.FrogLeft = 0d;
                return FrogEvent.Left;
            }
            int shower = Weather.ShowerAt(day, hour);
            if (shower < 0 || (s.FrogShowers & (1 << shower)) != 0 || s.FrogsToday >= PerDay || !CanCome(s)) return FrogEvent.None;
            s.FrogShowers |= 1 << shower;
            s.FrogLeft = StaySeconds;
            return FrogEvent.Arrived;
        }

        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds);

        public static bool Greet(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day, double hour, double roll, out BigNum paid, out int clovers)
        {
            paid = BigNum.Zero;
            clovers = 0;
            if (!Here(s) || day != s.FrogDay || s.FrogsToday >= PerDay || !Weather.IsRaining(day, hour)) return false;
            paid = Reward(defs, s, m);
            s.Coins += paid;
            clovers = roll < CloverChance ? 1 : 0;
            s.Clovers += clovers;
            s.FrogsToday++;
            s.FrogsMet++;
            s.FrogLeft = 0d;
            return true;
        }
    }
}

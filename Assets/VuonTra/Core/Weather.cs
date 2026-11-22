using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The rain (#62): every day has two showers of eight minutes, at hours drawn from the day itself, so the same day
    /// and hour always give the same answer on every device and nothing about it is saved. The first falls between six
    /// in the morning and half past two, the second between half past two and eleven at night, so they never overlap and
    /// one often falls after dark. Rain changes only how the garden looks and sounds; nobody earns more or less in it.
    /// </summary>
    public static class Weather
    {
        public const int ShowersPerDay = 2;
        /// <summary>How long one shower lasts.</summary>
        public const double ShowerMinutes = 8d;
        /// <summary>The earliest a shower starts and the latest one is over, in hours of the day.</summary>
        public const double FirstHour = 6d, LastHour = 23d;
        /// <summary>The least time between the end of the first shower and the start of the second.</summary>
        public const double GapMinutes = 60d;

        /// <summary>When each of the day's showers starts, in hours of the day, earliest first.</summary>
        public static double[] ShowerStarts(long day) => (double[])Starts(day).Clone();

        // The screen asks every frame: the last day's hours are kept rather than drawn again.
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
            double room = half - (ShowerMinutes + GapMinutes) / 60d; // each shower ends a gap before its half does
            var starts = new double[ShowersPerDay];
            for (int k = 0; k < ShowersPerDay; k++)
            {
                // Whole minutes, so a shower starts on the minute.
                double at = FirstHour + k * half + Unit(day, k) * room;
                starts[k] = Math.Floor(at * 60d) / 60d;
            }
            return starts;
        }

        /// <summary>Which shower is falling at <paramref name="hour"/> of <paramref name="day"/>: 0 or 1, or -1 when it is dry.</summary>
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

        /// <summary>A number in [0, 1) that depends only on the day and the shower: a SplitMix64 step on both.</summary>
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
        /// <summary>A frog-hatted rabbit has just stepped out of the moon gate on its way to the pond.</summary>
        Arrived,
        /// <summary>It stayed its minute, or the rain stopped, and it has gone home ungreeted.</summary>
        Left
    }

    /// <summary>
    /// The Frog Rabbit (#62): in each shower a rabbit in a frog hat walks from the moon gate to the pond and stays a
    /// minute. A tap greets it: ten minutes of the garden's income, like a quest's reward a gift that does not count as
    /// earned, and a clover one time in four. One a shower, so two a day at most; never in dry weather, never before
    /// garden level <see cref="FromLevel"/> or during the first minute's guidance. The day is the clamped day the rest of
    /// the game goes by (#78): a clock set back brings no frog the day has not already had.
    /// </summary>
    public static class FrogRabbit
    {
        /// <summary>How long it waits at the pond.</summary>
        public const double StaySeconds = 60d;
        /// <summary>What greeting it pays: this many seconds of income, under the usual multipliers.</summary>
        public const double RewardSeconds = 600d;
        /// <summary>The chance of a clover with the carrots.</summary>
        public const double CloverChance = 0.25d;
        public const int PerDay = Weather.ShowersPerDay;
        /// <summary>
        /// The garden level it starts coming at: the level the last plots need, as for the moon jars' daily sources (#64).
        /// Ten minutes of a small income are many times a new garden's purse: with both of each day's frogs greeted, from
        /// level 1 the casual player's first-sitting frog took the share the games and guests have of day one under 15 %,
        /// and from level 8 or 10 the frogs brought all twelve plots on day two (EconomySimulationTests). From 12 the pace
        /// held; 15 keeps it clear of the race for the last plots altogether.
        /// </summary>
        public const int FromLevel = 15;

        /// <summary>True while it waits at the pond.</summary>
        public static bool Here(GameState s) => s.FrogLeft > 0d;

        public static bool CanCome(GameState s) => Tutorial.IsDone(s) && Garden.Level(s) >= FromLevel;

        /// <summary>
        /// Counts <paramref name="dt"/> seconds of open play at <paramref name="hour"/> of <paramref name="day"/>. A frog that
        /// is here waits out its minute and goes when it is up or the rain has stopped; otherwise one comes when a shower
        /// falls whose frog has not come yet, while the day has frogs left.
        /// </summary>
        public static FrogEvent Advance(GameState s, double dt, long day, double hour)
        {
            if (!(dt > 0d)) return FrogEvent.None; // also rejects NaN
            if (day < s.FrogDay)
            {
                // The clock went back: nothing comes, and a frog that was here goes home.
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

        /// <summary>What a greeting pays: ten minutes of the garden's income, under the income and speed multipliers.</summary>
        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds);

        /// <summary>
        /// The player taps the Frog Rabbit: pays the carrots and, when <paramref name="roll"/> falls under the chance, a clover;
        /// counts the meeting and sends it home. False, and nothing paid, when no frog is here today.
        /// </summary>
        public static bool Greet(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day, double hour, double roll, out BigNum paid, out int clovers)
        {
            paid = BigNum.Zero;
            clovers = 0;
            if (!Here(s) || day != s.FrogDay || s.FrogsToday >= PerDay || !Weather.IsRaining(day, hour)) return false;
            paid = Reward(defs, s, m);
            s.Coins += paid;
            clovers = roll < CloverChance ? 1 : 0; // NaN brings none
            s.Clovers += clovers;
            s.FrogsToday++;
            s.FrogsMet++;
            s.FrogLeft = 0d;
            return true;
        }
    }
}

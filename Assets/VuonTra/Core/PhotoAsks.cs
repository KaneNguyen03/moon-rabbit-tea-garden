using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum PhotoAskEvent
    {
        None,
        /// <summary>A rabbit has just put up its camera bubble.</summary>
        Asked,
        /// <summary>The bubble stayed its time (or its rabbit went) and is gone; nothing is lost for it.</summary>
        Expired
    }

    /// <summary>
    /// A rabbit asks for its picture (#56): every four minutes of open play one rabbit at work or in a seat puts up a
    /// camera bubble for a minute and a half. A picture taken while it asks pays two minutes of income on top of the
    /// photo itself, ten times a day at most. The clock runs like the guests' (<see cref="Orders"/>): only while the
    /// garden is open, never away, paused or at the title; and it holds while a bubble is up.
    /// </summary>
    public static class PhotoAsks
    {
        /// <summary>Seconds of open play between one bubble going and the next coming, and how long a bubble stays.</summary>
        public const double Interval = 240d, StaySeconds = 90d;
        /// <summary>What a picture taken for the asking rabbit pays: this many seconds of income, under the usual multipliers.</summary>
        public const double RewardSeconds = 120d;
        /// <summary>How many asks can be answered in a day.</summary>
        public const int PerDay = 10;

        /// <summary>True while a rabbit holds up its camera bubble.</summary>
        public static bool Waiting(GameState s) => s.PhotoAskRabbit > 0 && s.PhotoAskLeft > 0d;

        /// <summary>The rabbit asking, or null.</summary>
        public static Rabbit Asking(GameState s) => Waiting(s) ? Rabbits.Find(s, s.PhotoAskRabbit) : null;

        /// <summary>
        /// The rabbits that can ask: one at an open station, or one sitting in a place to rest. A rabbit resting
        /// beyond the places is nowhere to be seen, so it cannot be photographed.
        /// </summary>
        public static List<Rabbit> Candidates(IReadOnlyList<StationDef> defs, GameState s)
        {
            var list = new List<Rabbit>();
            foreach (Rabbit r in s.Rabbits)
                if (r.Station >= 0 && r.Station < s.Levels.Length && r.Station < defs.Count && s.Levels[r.Station] > 0) list.Add(r);
            list.AddRange(RestSpots.Seated(s));
            return list;
        }

        /// <summary>
        /// Counts <paramref name="dt"/> seconds of open play. A bubble that is up runs down and goes at nought (or as
        /// soon as its rabbit is gone); otherwise the clock runs towards the next ask, which comes when it reaches the
        /// interval and somebody can ask, unless the day's asks have all been answered.
        /// </summary>
        public static PhotoAskEvent Advance(IReadOnlyList<StationDef> defs, GameState s, double dt, long day, Func<double> roll)
        {
            if (!(dt > 0d)) return PhotoAskEvent.None; // also rejects NaN
            NewDay(s, day);
            if (s.PhotoAskRabbit > 0)
            {
                s.PhotoAskLeft -= dt;
                if (s.PhotoAskLeft > 0d && Rabbits.Find(s, s.PhotoAskRabbit) != null) return PhotoAskEvent.None;
                Clear(s);
                return PhotoAskEvent.Expired;
            }
            s.PhotoAskClock = Math.Min(Interval, s.PhotoAskClock + dt);
            if (s.PhotoAskClock < Interval) return PhotoAskEvent.None;
            return Ask(defs, s, day, roll) ? PhotoAskEvent.Asked : PhotoAskEvent.None;
        }

        /// <summary>A rabbit asks now, whatever the clock says (previews, tests). False when one asks already, the day's asks are answered, or nobody can ask.</summary>
        public static bool Ask(IReadOnlyList<StationDef> defs, GameState s, long day, Func<double> roll)
        {
            NewDay(s, day);
            if (s.PhotoAskRabbit > 0 || s.PhotoAsksToday >= PerDay) return false;
            List<Rabbit> can = Candidates(defs, s);
            if (can.Count == 0) return false;
            double r = roll != null ? roll() : 0.5d;
            if (double.IsNaN(r)) r = 0.5d;
            int pick = Math.Max(0, Math.Min(can.Count - 1, (int)(Math.Max(0d, r) * can.Count)));
            s.PhotoAskRabbit = can[pick].Id;
            s.PhotoAskLeft = StaySeconds;
            s.PhotoAskClock = 0d;
            return true;
        }

        /// <summary>What answering pays: the asking seconds of the garden's income, under the usual multipliers.</summary>
        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds);

        /// <summary>
        /// The picture was taken while the rabbit asked: pays the reward (counted as earned, like a sale), counts the
        /// answer for the day and the quests, and starts the clock for the next. False, and nothing paid, when nobody asks.
        /// </summary>
        public static bool Answer(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day, out BigNum paid)
        {
            paid = BigNum.Zero;
            NewDay(s, day);
            if (!Waiting(s) || s.PhotoAsksToday >= PerDay) return false;
            paid = Reward(defs, s, m);
            s.Coins += paid;
            s.TotalEarned += paid;
            s.PhotoAsksToday++;
            s.PhotoAsksAnswered++;
            Clear(s);
            return true;
        }

        /// <summary>The camera closed without a picture: that ask is gone, nothing else. False when nobody asked.</summary>
        public static bool Decline(GameState s)
        {
            if (s.PhotoAskRabbit <= 0) return false;
            Clear(s);
            return true;
        }

        static void Clear(GameState s)
        {
            s.PhotoAskRabbit = 0;
            s.PhotoAskLeft = 0d;
            s.PhotoAskClock = 0d;
        }

        static void NewDay(GameState s, long day)
        {
            if (day <= s.PhotoAskDay) return; // a clock turned back does not bring the day's asks back (#78)
            s.PhotoAskDay = day;
            s.PhotoAsksToday = 0;
        }
    }
}

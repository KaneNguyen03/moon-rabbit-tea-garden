using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum PhotoAskEvent
    {
        None,
        Asked,
        Expired
    }

    public static class PhotoAsks
    {
        public const double Interval = 240d, StaySeconds = 90d;
        public const double RewardSeconds = 120d;
        public const int PerDay = 10;

        public static bool Waiting(GameState s) => s.PhotoAskRabbit > 0 && s.PhotoAskLeft > 0d;

        public static Rabbit Asking(GameState s) => Waiting(s) ? Rabbits.Find(s, s.PhotoAskRabbit) : null;

        public static List<Rabbit> Candidates(IReadOnlyList<StationDef> defs, GameState s)
        {
            var list = new List<Rabbit>();
            foreach (Rabbit r in s.Rabbits)
                if (r.Station >= 0 && r.Station < s.Levels.Length && r.Station < defs.Count && s.Levels[r.Station] > 0) list.Add(r);
            list.AddRange(RestSpots.Seated(s));
            return list;
        }

        public static PhotoAskEvent Advance(IReadOnlyList<StationDef> defs, GameState s, double dt, long day, Func<double> roll)
        {
            if (!(dt > 0d)) return PhotoAskEvent.None;
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

        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds);

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
            if (day <= s.PhotoAskDay) return;
            s.PhotoAskDay = day;
            s.PhotoAsksToday = 0;
        }
    }
}

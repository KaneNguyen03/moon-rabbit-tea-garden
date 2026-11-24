using System;

namespace VuonTra.Core
{
    public struct AwayReport
    {
        public BigNum Paid;
        public double Seconds;
        public bool Capped;
        public int TrayAdded;
        public bool TrayFull;
        public int GuestsMissed;
        public long SleepiestId;

        public bool WorthTelling => Seconds >= Away.SheetSeconds && Paid >= BigNum.One;
    }

    public static class Away
    {
        public const double SheetSeconds = 300d;
        public const int DoubleCost = 5;

        public static AwayReport Claim(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s, long nowUnixSeconds, Modifiers? mods = null, long? day = null)
        {
            Modifiers m = mods ?? Modifiers.None;
            int trayBefore = Tray.Count(s);
            BigNum paid = Simulation.ClaimAway(defs, s, nowUnixSeconds, out double counted, m);
            return new AwayReport
            {
                Paid = paid,
                Seconds = counted,
                Capped = counted > 0d && counted >= m.AwayCapSeconds,
                TrayAdded = Math.Max(0, Tray.Count(s) - trayBefore),
                TrayFull = Tray.IsFull(s),
                GuestsMissed = GuestsWhileAway(defs, s, counted, day ?? s.OrderDay),
                SleepiestId = Sleepiest(s)
            };
        }

        public static int GuestsWhileAway(double seconds)
        {
            if (!(seconds > 0d)) return 0;
            return (int)Math.Min(Orders.PerDay, Math.Floor(seconds / (Orders.Interval + Orders.WaitSeconds)));
        }

        public static int GuestsWhileAway(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s, double seconds, long day)
        {
            int byTime = GuestsWhileAway(seconds);
            if (byTime == 0 || Orders.Make(defs, s, null) == null) return 0;
            int left = day > s.OrderDay ? Orders.PerDay : Orders.PerDay - s.OrdersToday;
            return Math.Max(0, Math.Min(byTime, left));
        }

        public static long Sleepiest(GameState s)
        {
            Rabbit best = null;
            bool bestResting = false;
            foreach (Rabbit r in s.Rabbits)
            {
                bool resting = r.Station < 0;
                if (best == null || (resting && !bestResting)
                    || (resting == bestResting && (r.Tier > best.Tier || (r.Tier == best.Tier && r.Level > best.Level))))
                {
                    best = r;
                    bestResting = resting;
                }
            }
            return best != null ? best.Id : 0;
        }

        public static bool DoubleIsFree(GameState s, long day) => day > s.AwayDoubleDay || MoonPass.ExtraDoubleFree(s, day);

        public static int DoubleCostToday(GameState s, long day) => DoubleIsFree(s, day) ? 0 : DoubleCost;

        public static bool Double(GameState s, long day, BigNum paid)
        {
            if (paid < BigNum.One) return false;
            if (day > s.AwayDoubleDay) s.AwayDoubleDay = day;
            else if (MoonPass.ExtraDoubleFree(s, day)) s.MoonPassDoubleDay = day;
            else if (s.GoldenCarrots < DoubleCost) return false;
            else s.GoldenCarrots -= DoubleCost;
            s.Coins += paid;
            return true;
        }
    }
}

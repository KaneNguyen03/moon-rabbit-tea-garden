using System;

namespace VuonTra.Core
{
    /// <summary>What the garden did while the player was away (#69): the welcome-back sheet tells it in four lines.</summary>
    public struct AwayReport
    {
        /// <summary>Carrots paid for the time away.</summary>
        public BigNum Paid;
        /// <summary>The seconds that were paid for (never more than the cap).</summary>
        public double Seconds;
        /// <summary>The time away was longer than the cap.</summary>
        public bool Capped;
        /// <summary>Products the stations put on the tray meanwhile, and whether it is now full.</summary>
        public int TrayAdded;
        public bool TrayFull;
        /// <summary>Guests who would have come by and found nobody at the counter: told, never charged.</summary>
        public int GuestsMissed;
        /// <summary>The rabbit that slept the most, to show on the sheet; 0 for none.</summary>
        public long SleepiestId;

        /// <summary>Long enough to deserve the sheet; shorter absences just fly the carrots to the purse.</summary>
        public bool WorthTelling => Seconds >= Away.SheetSeconds && Paid >= BigNum.One;
    }

    /// <summary>
    /// The time away, told (#69): what <see cref="Simulation.ClaimAway"/> pays, with the tray, the guests and the
    /// sleepiest rabbit beside it, and the once-a-day free doubling of what was paid.
    /// </summary>
    public static class Away
    {
        /// <summary>Under five minutes away there is no sheet, only carrots flying to the purse.</summary>
        public const double SheetSeconds = 300d;
        /// <summary>The second and later doublings of a day cost this many golden carrots.</summary>
        public const int DoubleCost = 5;

        /// <summary>
        /// Pays the time since the game was last seen, as <see cref="Simulation.ClaimAway"/> does, and says what happened meanwhile.
        /// <paramref name="day"/> is the clamped day the game goes by now (the guests' day); without it the guests' own day is assumed.
        /// </summary>
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

        /// <summary>How many guests the time alone had room for, one per order cycle (arrival and wait), at most a day's worth.</summary>
        public static int GuestsWhileAway(double seconds)
        {
            if (!(seconds > 0d)) return 0;
            return (int)Math.Min(Orders.PerDay, Math.Floor(seconds / (Orders.Interval + Orders.WaitSeconds)));
        }

        /// <summary>
        /// The guests the time away told of (#80): one per order cycle, but never more than the day's guests still to come,
        /// and none while no open station makes anything a guest could order (as <see cref="Orders.Place"/> decides). When
        /// <paramref name="day"/> is past the guests' day, a new day began while away and its guests count from the start; the
        /// day is the clamped one, so a clock turned back brings none.
        /// </summary>
        public static int GuestsWhileAway(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s, double seconds, long day)
        {
            int byTime = GuestsWhileAway(seconds);
            if (byTime == 0 || Orders.Make(defs, s, null) == null) return 0;
            int left = day > s.OrderDay ? Orders.PerDay : Orders.PerDay - s.OrdersToday;
            return Math.Max(0, Math.Min(byTime, left));
        }

        /// <summary>The rabbit to show sleeping: the rarest resting one, else the rarest of all; 0 when there is none.</summary>
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

        /// <summary>The day's first doubling is free. The day is the clamped one, so turning the clock back never brings it back.</summary>
        public static bool DoubleIsFree(GameState s, long day) => day > s.AwayDoubleDay;

        /// <summary>What doubling costs today in golden carrots: 0 for the first of the day.</summary>
        public static int DoubleCostToday(GameState s, long day) => DoubleIsFree(s, day) ? 0 : DoubleCost;

        /// <summary>
        /// Pays <paramref name="paid"/> once more: free the first time in a day, then for <see cref="DoubleCost"/>
        /// golden carrots. A gift, like a quest's reward, so it does not count as carrots earned. False, and nothing
        /// changes, when there is nothing to double or the golden carrots are short.
        /// </summary>
        public static bool Double(GameState s, long day, BigNum paid)
        {
            if (paid < BigNum.One) return false;
            if (DoubleIsFree(s, day)) s.AwayDoubleDay = day;
            else if (s.GoldenCarrots < DoubleCost) return false;
            else s.GoldenCarrots -= DoubleCost;
            s.Coins += paid;
            return true;
        }
    }
}

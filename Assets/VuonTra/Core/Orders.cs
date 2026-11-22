using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>So many of one station's product, as a guest asks for them.</summary>
    public sealed class OrderLine
    {
        public int Station;
        public int Count = 1;
    }

    /// <summary>A guest at the counter and what it came for. One at a time; it leaves when served or when its patience runs out.</summary>
    public sealed class Order
    {
        public long Id;
        /// <summary>The kind of rabbit the guest is (see <see cref="RabbitKinds"/>).</summary>
        public string Guest;
        public List<OrderLine> Lines = new List<OrderLine>();
        /// <summary>Seconds of open play before the guest gives up and walks away.</summary>
        public double Left;
        /// <summary>Carrots on top of the products' price: this many seconds of the garden's income.</summary>
        public double RewardSeconds;
        public int RewardClovers;
    }

    public enum OrderEvent
    {
        None,
        /// <summary>A guest has just come to the counter.</summary>
        Arrived,
        /// <summary>The guest waited long enough and left; nothing is lost for it.</summary>
        Left
    }

    /// <summary>
    /// Guests who visit the counter with a tea order (#20): a small thing to do between upgrades. A guest comes
    /// a minute after the garden opens and then every few minutes of open play while nobody is waiting, asks
    /// for one or two of the things the open stations make, and waits three minutes. Delivering the whole
    /// order from the tray pays the products' price plus a minute and a half of income, and one guest in three
    /// leaves a clover too. Time away and paused time do not count: the counter only runs while the garden is open.
    /// </summary>
    public static class Orders
    {
        /// <summary>Seconds of open play before the first guest, and between one leaving and the next coming.</summary>
        public const double FirstAfter = 60d, Interval = 240d;
        /// <summary>How long a guest waits at the counter.</summary>
        public const double WaitSeconds = 180d;
        /// <summary>The bonus on top of the products' price: this many seconds of income.</summary>
        public const double RewardSeconds = 90d;
        /// <summary>How many guests come in a day at most, and the most lines and the most of one product an order asks for.</summary>
        public const int PerDay = 8, MaxLines = 2, MaxCount = 3;
        /// <summary>The chance an order comes with a clover.</summary>
        public const double CloverChance = 1d / 3d;
        /// <summary>A kind not yet in the album is this many times likelier to come than one that is.</summary>
        public const int UnseenWeight = 3;

        /// <summary>Where the clock starts in a new garden, so the first guest comes <see cref="FirstAfter"/> seconds in.</summary>
        public static double StartingClock => Interval - FirstAfter;

        /// <summary>
        /// Counts <paramref name="dt"/> seconds of open play. A waiting guest's patience runs down and it leaves at
        /// nought; otherwise the clock runs towards the next guest, who comes when it reaches the interval, unless
        /// the day's guests have all been (then the clock holds, ready for tomorrow). <paramref name="roll"/> gives
        /// numbers in [0, 1) that decide the order.
        /// </summary>
        public static OrderEvent Advance(IReadOnlyList<StationDef> defs, GameState s, double dt, long day, Func<double> roll)
        {
            if (!(dt > 0d)) return OrderEvent.None; // also rejects NaN
            NewDay(s, day);
            if (s.Order != null)
            {
                s.Order.Left -= dt;
                if (s.Order.Left > 0d) return OrderEvent.None;
                s.Order = null;
                s.OrderClock = 0d;
                return OrderEvent.Left;
            }
            s.OrderClock += dt;
            if (s.OrderClock < Interval) return OrderEvent.None;
            s.OrderClock = Interval;
            return Place(defs, s, day, roll) != null ? OrderEvent.Arrived : OrderEvent.None;
        }

        /// <summary>Brings a guest to the counter now, whatever the clock says: the new order, or null when one is waiting already, the day's guests have all been, or nothing can be ordered.</summary>
        public static Order Place(IReadOnlyList<StationDef> defs, GameState s, long day, Func<double> roll)
        {
            NewDay(s, day);
            if (s.Order != null || s.OrdersToday >= PerDay) return null;
            Order order = Make(defs, s, roll);
            if (order == null) return null;
            order.Id = ++s.NextOrderId;
            s.Order = order;
            s.OrderClock = 0d;
            s.OrdersToday++;
            return order;
        }

        static void NewDay(GameState s, long day)
        {
            if (day <= s.OrderDay) return; // a clock turned back does not bring the day's guests back (#78)
            s.OrderDay = day;
            s.OrdersToday = 0;
        }

        /// <summary>
        /// Builds an order from what the open brew stations make, without placing it: one line, or two when the roll
        /// is high and two stations are open; one to three of each; a guest who is more often a kind the album lacks;
        /// a clover one time in three. Null when no open station makes anything.
        /// </summary>
        public static Order Make(IReadOnlyList<StationDef> defs, GameState s, Func<double> roll)
        {
            var open = new List<int>();
            for (int i = 0; i < defs.Count && i < s.Levels.Length; i++)
                if (s.Levels[i] > 0 && defs[i].Kind == StationKind.Brew && defs[i].CycleSeconds > 0d && !Layout.IsStored(defs, s, i)) open.Add(i);
            if (open.Count == 0) return null;

            int lines = open.Count >= 2 && Next(roll) >= 0.5d ? Math.Min(MaxLines, open.Count) : 1;
            var order = new Order { RewardSeconds = RewardSeconds, Left = WaitSeconds };
            for (int n = 0; n < lines; n++)
            {
                int pick = Math.Min(open.Count - 1, (int)(Next(roll) * open.Count));
                order.Lines.Add(new OrderLine { Station = open[pick] });
                open.RemoveAt(pick);
            }
            foreach (OrderLine line in order.Lines)
                line.Count = Math.Min(Tray.StackLimit, 1 + Math.Min(MaxCount - 1, (int)(Next(roll) * MaxCount)));
            order.Guest = PickGuest(s, Next(roll));
            order.RewardClovers = Next(roll) < CloverChance ? 1 : 0;
            return order;
        }

        static double Next(Func<double> roll)
        {
            double r = roll != null ? roll() : 0.5d;
            return double.IsNaN(r) ? 0.5d : Math.Max(0d, Math.Min(0.999999d, r));
        }

        /// <summary>The guest's kind from a roll, across every tier: kinds the album lacks weigh three times a kind it has.</summary>
        public static string PickGuest(GameState s, double roll)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.All;
            double total = 0d;
            foreach (RabbitKind kind in kinds) total += s.SeenKinds.Contains(kind.Id) ? 1d : UnseenWeight;
            double at = Math.Max(0d, Math.Min(0.999999d, roll)) * total;
            foreach (RabbitKind kind in kinds)
            {
                at -= s.SeenKinds.Contains(kind.Id) ? 1d : UnseenWeight;
                if (at < 0d) return kind.Id;
            }
            return kinds[kinds.Count - 1].Id;
        }

        /// <summary>How many plain products of a station the tray holds. Golden ones are not for guests: they keep their golden carrot.</summary>
        public static int Have(GameState s, int station)
        {
            int n = 0;
            foreach (Product p in s.Tray)
                if (p.Station == station && !p.Golden) n += p.Count;
            return n;
        }

        public static bool LineMet(GameState s, OrderLine line) => Have(s, line.Station) >= line.Count;

        /// <summary>True when the tray holds everything the guest asked for.</summary>
        public static bool CanDeliver(GameState s, Order order)
        {
            if (order == null || order.Lines.Count == 0) return false;
            foreach (OrderLine line in order.Lines)
                if (!LineMet(s, line)) return false;
            return true;
        }

        /// <summary>What delivering pays: the products' selling price plus the order's seconds of income, both under the usual multipliers.</summary>
        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, Order order)
        {
            BigNum total = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * order.RewardSeconds);
            foreach (OrderLine line in order.Lines) total += Tray.Price(defs, s, line.Station, m) * line.Count;
            return total;
        }

        /// <summary>
        /// Serves the waiting guest: takes the products from the tray, pays the carrots (which count as earned, like a
        /// sale) and any clover, and starts the clock for the next guest. False, and nothing changes, when there is no
        /// guest or a line is short.
        /// </summary>
        public static bool Deliver(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, out BigNum paid, out int clovers)
        {
            paid = BigNum.Zero;
            clovers = 0;
            Order order = s.Order;
            if (!CanDeliver(s, order)) return false;
            paid = Reward(defs, s, m, order);
            foreach (OrderLine line in order.Lines) Take(s, line.Station, line.Count);
            clovers = order.RewardClovers;
            s.Coins += paid;
            s.TotalEarned += paid;
            s.Clovers += clovers;
            s.OrdersDelivered++;
            s.Order = null;
            s.OrderClock = 0d;
            return true;
        }

        /// <summary>Takes so many plain products of a station out of the tray; an emptied stack leaves its slot.</summary>
        static void Take(GameState s, int station, int count)
        {
            for (int i = s.Tray.Count - 1; i >= 0 && count > 0; i--)
            {
                Product p = s.Tray[i];
                if (p.Station != station || p.Golden) continue;
                int taken = Math.Min(p.Count, count);
                p.Count -= taken;
                count -= taken;
                if (p.Count <= 0) s.Tray.RemoveAt(i);
            }
        }
    }
}

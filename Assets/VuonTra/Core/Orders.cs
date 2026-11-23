using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class OrderLine
    {
        public int Station;
        public int Count = 1;
    }

    public sealed class Order
    {
        public long Id;
        public string Guest;
        public List<OrderLine> Lines = new List<OrderLine>();
        public double Left;
        public double RewardSeconds;
        public int RewardClovers;
    }

    public enum OrderEvent
    {
        None,
        Arrived,
        Left
    }

    public static class Orders
    {
        public const double FirstAfter = 60d, Interval = 240d;
        public const double WaitSeconds = 180d;
        public const double RewardSeconds = 90d;
        public const int PerDay = 8, MaxLines = 2, MaxCount = 3;
        public const double CloverChance = 1d / 3d;
        public const int UnseenWeight = 3;

        public static double StartingClock => Interval - FirstAfter;

        public static OrderEvent Advance(IReadOnlyList<StationDef> defs, GameState s, double dt, long day, Func<double> roll)
        {
            if (!(dt > 0d)) return OrderEvent.None;
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
            if (day <= s.OrderDay) return;
            s.OrderDay = day;
            s.OrdersToday = 0;
        }

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

        public static int Have(GameState s, int station)
        {
            int n = 0;
            foreach (Product p in s.Tray)
                if (p.Station == station && !p.Golden) n += p.Count;
            return n;
        }

        public static bool LineMet(GameState s, OrderLine line) => Have(s, line.Station) >= line.Count;

        public static bool CanDeliver(GameState s, Order order)
        {
            if (order == null || order.Lines.Count == 0) return false;
            foreach (OrderLine line in order.Lines)
                if (!LineMet(s, line)) return false;
            return true;
        }

        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, Order order)
        {
            BigNum total = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * order.RewardSeconds);
            foreach (OrderLine line in order.Lines) total += Tray.Price(defs, s, line.Station, m) * line.Count;
            return total;
        }

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

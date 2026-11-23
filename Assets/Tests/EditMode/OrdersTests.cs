using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class OrdersTests
    {
        List<StationDef> defs;
        GameState s;
        const long Day = 500;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
        }

        static System.Func<double> Rolls(params double[] values)
        {
            int at = 0;
            return () => values[System.Math.Min(at++, values.Length - 1)];
        }

        OrderEvent Run(double seconds, System.Func<double> roll = null, double step = 1d)
        {
            OrderEvent last = OrderEvent.None;
            for (double t = 0; t < seconds; t += step)
            {
                OrderEvent e = Orders.Advance(defs, s, step, Day, roll ?? Rolls(0.2));
                if (e != OrderEvent.None) last = e;
            }
            return last;
        }

        [Test]
        public void The_First_Guest_Comes_A_Minute_In_And_The_Next_An_Interval_After_The_Last_Left()
        {
            Assert.IsNull(s.Order, "nobody at the counter to begin with");
            Assert.AreEqual(OrderEvent.None, Run(Orders.FirstAfter - 1d));
            Assert.IsNull(s.Order, "not yet");
            Assert.AreEqual(OrderEvent.Arrived, Run(2d));
            Assert.IsNotNull(s.Order, "the first guest is here");
            Assert.AreEqual(1, s.OrdersToday);
            Assert.AreEqual(Day, s.OrderDay);
            Assert.AreEqual(Orders.WaitSeconds, s.Order.Left, 1.5d, "the guest waits three minutes");

            Assert.AreEqual(OrderEvent.None, Run(Orders.WaitSeconds - 2d));
            Assert.IsNotNull(s.Order, "still waiting");
            Assert.AreEqual(OrderEvent.Left, Run(3d));
            Assert.IsNull(s.Order, "gone, and nothing lost for it");
            Assert.AreEqual(0, s.OrdersDelivered);

            Assert.AreEqual(OrderEvent.None, Run(Orders.Interval - 5d));
            Assert.IsNull(s.Order);
            Assert.AreEqual(OrderEvent.Arrived, Run(6d));
            Assert.IsNotNull(s.Order);
            Assert.AreEqual(2, s.OrdersToday);
        }

        [Test]
        public void An_Order_Is_Built_Only_From_Open_Brew_Stations_With_Counts_The_Tray_Can_Hold()
        {
            for (int k = 0; k < 40; k++)
            {
                double r = (k * 0.137) % 1d;
                Order o = Orders.Make(defs, s, Rolls(r, (r + 0.3) % 1d, (r + 0.6) % 1d, (r + 0.9) % 1d, r));
                Assert.IsNotNull(o);
                Assert.GreaterOrEqual(o.Lines.Count, 1);
                Assert.LessOrEqual(o.Lines.Count, Orders.MaxLines);
                var seen = new HashSet<int>();
                foreach (OrderLine line in o.Lines)
                {
                    Assert.AreNotEqual(1, line.Station, "the leaf basket makes nothing to order");
                    Assert.IsTrue(seen.Add(line.Station), "one line per station");
                    Assert.GreaterOrEqual(line.Count, 1);
                    Assert.LessOrEqual(line.Count, Orders.MaxCount);
                    Assert.LessOrEqual(line.Count, Tray.StackLimit, "never more than one stack holds");
                }
                Assert.IsNotNull(RabbitKinds.Find(o.Guest), "the guest is a kind of rabbit");
                Assert.AreEqual(Orders.RewardSeconds, o.RewardSeconds);
            }

            s.Levels = new[] { 5, 2, 0 };
            for (int k = 0; k < 10; k++)
            {
                Order o = Orders.Make(defs, s, Rolls(0.9, 0.9, 0.9, 0.9, 0.9));
                Assert.AreEqual(1, o.Lines.Count, "one open brew station: one line, however the roll goes");
                Assert.AreEqual(0, o.Lines[0].Station);
            }
            Assert.AreEqual(Orders.MaxCount, Orders.Make(defs, s, Rolls(0.9)).Lines[0].Count, "a high roll asks for the most");
            Assert.AreEqual(1, Orders.Make(defs, s, Rolls(0.0)).Lines[0].Count, "a low roll asks for one");
        }

        [Test]
        public void A_High_Roll_Asks_For_Two_Stations_And_A_Low_Roll_One()
        {
            Order two = Orders.Make(defs, s, Rolls(0.9, 0.9, 0.1, 0.5, 0.5, 0.5));
            Assert.AreEqual(2, two.Lines.Count);
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, new[] { two.Lines[0].Station, two.Lines[1].Station });
            Order one = Orders.Make(defs, s, Rolls(0.1, 0.1, 0.5, 0.5, 0.5));
            Assert.AreEqual(1, one.Lines.Count);
        }

        [Test]
        public void The_Guest_Is_More_Often_A_Kind_Not_Yet_In_The_Album()
        {
            int unseen = 0, n = 200;
            foreach (RabbitKind kind in RabbitKinds.All) s.SeenKinds.Add(kind.Id);
            s.SeenKinds.Remove(RabbitKinds.All[0].Id);
            for (int k = 0; k < n; k++)
            {
                double r = (k + 0.5) / n;
                Order o = Orders.Make(defs, s, Rolls(0.1, 0.1, 0.1, r, 0.5));
                if (o.Guest == RabbitKinds.All[0].Id) unseen++;
            }
            double share = unseen / (double)n, even = 1d / RabbitKinds.All.Count;
            Assert.Greater(share, even * 2d, "the one kind the album lacks comes about three times as often as any other");
        }

        [Test]
        public void Delivery_Needs_Every_Line_Met_By_Plain_Products_In_The_Tray()
        {
            s.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 100, RewardSeconds = Orders.RewardSeconds };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 2 });
            s.Order.Lines.Add(new OrderLine { Station = 2, Count = 1 });
            Assert.IsFalse(Orders.CanDeliver(s, s.Order), "an empty tray");
            Assert.IsFalse(Orders.Deliver(defs, s, Modifiers.None, out BigNum paid, out int clovers));
            Assert.IsTrue(paid.Equals(BigNum.Zero));
            Assert.IsNotNull(s.Order, "the guest keeps waiting");

            Tray.Add(s, 0, false); Tray.Add(s, 0, false);
            Assert.AreEqual(2, Orders.Have(s, 0));
            Assert.IsTrue(Orders.LineMet(s, s.Order.Lines[0]));
            Assert.IsFalse(Orders.LineMet(s, s.Order.Lines[1]));
            Assert.IsFalse(Orders.CanDeliver(s, s.Order), "one line short");

            Tray.Add(s, 2, true);
            Assert.AreEqual(0, Orders.Have(s, 2), "a golden batch is not for guests: it keeps its golden carrot");
            Assert.IsFalse(Orders.CanDeliver(s, s.Order));
            Tray.Add(s, 2, false);
            Assert.IsTrue(Orders.CanDeliver(s, s.Order));
        }

        [Test]
        public void Delivering_Takes_The_Products_Pays_More_Than_Selling_Them_And_Counts()
        {
            s.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 100, RewardSeconds = Orders.RewardSeconds };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 2 });
            s.Order.Lines.Add(new OrderLine { Station = 2, Count = 1 });
            Tray.Add(s, 0, false); Tray.Add(s, 0, false); Tray.Add(s, 0, false);
            Tray.Add(s, 2, false);
            Tray.Add(s, 2, true);
            BigNum sale = Tray.Price(defs, s, 0, Modifiers.None) * 2 + Tray.Price(defs, s, 2, Modifiers.None);
            BigNum bonus = Economy.IncomePerSecond(defs, s) * Orders.RewardSeconds;
            Assert.IsTrue(Orders.Reward(defs, s, Modifiers.None, s.Order).Equals(sale + bonus), "the products' price plus a minute and a half of income");

            Assert.IsTrue(Orders.Deliver(defs, s, Modifiers.None, out BigNum paid, out int clovers));
            Assert.IsTrue(paid.Equals(sale + bonus));
            Assert.IsTrue(s.Coins.Equals(paid));
            Assert.IsTrue(s.TotalEarned.Equals(paid), "counts as earned, like a sale");
            Assert.AreEqual(0, clovers, "this order promised none");
            Assert.IsNull(s.Order, "served");
            Assert.AreEqual(1, s.OrdersDelivered);
            Assert.AreEqual(0d, s.OrderClock, "the next guest's clock starts over");

            Assert.AreEqual(1, Orders.Have(s, 0));
            Assert.AreEqual(0, Orders.Have(s, 2));
            Product goldenJam = null;
            foreach (Product p in s.Tray) if (p.Station == 2) goldenJam = p;
            Assert.IsNotNull(goldenJam);
            Assert.IsTrue(goldenJam.Golden);
            Assert.AreEqual(2, s.Tray.Count, "the emptied jam stack is gone from the tray");

            Assert.IsFalse(Orders.Deliver(defs, s, Modifiers.None, out _, out _), "nobody to deliver to");
        }

        [Test]
        public void Multipliers_Raise_The_Reward_Like_Everything_Else()
        {
            s.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 100, RewardSeconds = Orders.RewardSeconds };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 1 });
            Modifiers m = Modifiers.None;
            m.IncomeMultiplier = 2d;
            BigNum plain = Orders.Reward(defs, s, Modifiers.None, s.Order), doubled = Orders.Reward(defs, s, m, s.Order);
            Assert.AreEqual(plain.ToDouble() * 2d, doubled.ToDouble(), 1e-9);
        }

        [Test]
        public void One_Order_In_Three_Promises_A_Clover_And_Delivering_Pays_It()
        {
            Assert.AreEqual(1, Orders.Make(defs, s, Rolls(0.1, 0.1, 0.1, 0.1, 0.2)).RewardClovers, "a roll under a third");
            Assert.AreEqual(0, Orders.Make(defs, s, Rolls(0.1, 0.1, 0.1, 0.1, 0.4)).RewardClovers, "a roll over it");

            int promised = 0, n = 300;
            for (int k = 0; k < n; k++)
                promised += Orders.Make(defs, s, Rolls(0.1, 0.1, 0.1, 0.1, (k + 0.5) / n)).RewardClovers;
            Assert.AreEqual(n * Orders.CloverChance, promised, 2d);

            s.Order = Orders.Make(defs, s, Rolls(0.1, 0.1, 0.1, 0.1, 0.2));
            s.Order.Left = 100;
            Tray.Add(s, 0, false);
            Assert.IsTrue(Orders.Deliver(defs, s, Modifiers.None, out _, out int clovers));
            Assert.AreEqual(1, clovers);
            Assert.AreEqual(1, s.Clovers);
        }

        [Test]
        public void At_Most_Eight_Guests_A_Day_And_A_New_Day_Starts_The_Count_Over()
        {
            int arrived = 0;
            for (int k = 0; k < Orders.PerDay + 3; k++)
            {
                for (double t = 0; t < Orders.Interval + 1d; t += 1d)
                    if (Orders.Advance(defs, s, 1d, Day, Rolls(0.2)) == OrderEvent.Arrived) arrived++;
                if (s.Order != null) Run(Orders.WaitSeconds + 1d);
            }
            Assert.AreEqual(Orders.PerDay, arrived);
            Assert.AreEqual(Orders.PerDay, s.OrdersToday);
            Assert.IsNull(s.Order);
            Assert.AreEqual(Orders.Interval, s.OrderClock, 1e-9, "the clock holds at the full interval, ready for tomorrow");

            Assert.AreEqual(OrderEvent.Arrived, Orders.Advance(defs, s, 0.5, Day + 1, Rolls(0.2)), "the first guest tomorrow comes at once");
            Assert.AreEqual(1, s.OrdersToday);
            Assert.AreEqual(Day + 1, s.OrderDay);
        }

        [Test]
        public void A_Clock_Turned_Back_Does_Not_Bring_The_Days_Guests_Back()
        {
            s.OrderDay = Day;
            s.OrdersToday = Orders.PerDay;
            s.OrderClock = Orders.Interval;
            Assert.AreEqual(OrderEvent.None, Orders.Advance(defs, s, 1d, Day - 1, Rolls(0.2)), "no guest on a day the clock went back to");
            Assert.AreEqual(Orders.PerDay, s.OrdersToday);
            Assert.AreEqual(Day, s.OrderDay);
        }

        [Test]
        public void Placing_An_Order_By_Hand_Respects_The_Cap_And_Gives_Each_Its_Own_Id()
        {
            Order a = Orders.Place(defs, s, Day, Rolls(0.2));
            Assert.IsNotNull(a);
            Assert.AreSame(a, s.Order);
            Assert.IsNull(Orders.Place(defs, s, Day, Rolls(0.2)), "one guest at a time");
            s.Order = null;
            Order b = Orders.Place(defs, s, Day, Rolls(0.2));
            Assert.AreNotEqual(a.Id, b.Id);
            s.Order = null;
            s.OrdersToday = Orders.PerDay;
            Assert.IsNull(Orders.Place(defs, s, Day, Rolls(0.2)), "the day's guests have all been");
        }

        [Test]
        public void Nothing_Happens_For_Zero_Or_Nonsense_Time()
        {
            s.OrderClock = Orders.Interval;
            Assert.AreEqual(OrderEvent.None, Orders.Advance(defs, s, 0d, Day, Rolls(0.2)));
            Assert.AreEqual(OrderEvent.None, Orders.Advance(defs, s, double.NaN, Day, Rolls(0.2)));
            Assert.IsNull(s.Order);
        }

        [Test]
        public void The_Order_And_Its_Schedule_Survive_A_Save_And_An_Old_Save_Has_The_Defaults()
        {
            s.OrderClock = 123.5;
            s.OrdersToday = 3;
            s.OrderDay = Day;
            s.OrdersDelivered = 7;
            s.DailyOrdersBase = 5;
            s.NextOrderId = 9;
            s.Order = new Order { Id = 9, Guest = RabbitKinds.All[2].Id, Left = 77.25, RewardSeconds = Orders.RewardSeconds, RewardClovers = 1 };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 2 });
            s.Order.Lines.Add(new OrderLine { Station = 2, Count = 3 });

            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(123.5, r.OrderClock, 1e-9);
            Assert.AreEqual(3, r.OrdersToday);
            Assert.AreEqual(Day, r.OrderDay);
            Assert.AreEqual(7, r.OrdersDelivered);
            Assert.AreEqual(5, r.DailyOrdersBase);
            Assert.AreEqual(9, r.NextOrderId);
            Assert.IsNotNull(r.Order);
            Assert.AreEqual(9, r.Order.Id);
            Assert.AreEqual(RabbitKinds.All[2].Id, r.Order.Guest);
            Assert.AreEqual(77.25, r.Order.Left, 1e-9);
            Assert.AreEqual(Orders.RewardSeconds, r.Order.RewardSeconds);
            Assert.AreEqual(1, r.Order.RewardClovers);
            Assert.AreEqual(2, r.Order.Lines.Count);
            Assert.AreEqual(0, r.Order.Lines[0].Station); Assert.AreEqual(2, r.Order.Lines[0].Count);
            Assert.AreEqual(2, r.Order.Lines[1].Station); Assert.AreEqual(3, r.Order.Lines[1].Count);

            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":3}]}").State;
            Assert.IsNull(old.Order);
            Assert.AreEqual(Orders.Interval - Orders.FirstAfter, old.OrderClock, 1e-9);
            Assert.AreEqual(0, old.OrdersToday);
            Assert.AreEqual(-1, old.OrderDay);
            Assert.AreEqual(0, old.OrdersDelivered);
            Assert.AreEqual(0, old.DailyOrdersBase);

            string json = SaveCodec.Encode(defs, s).Replace("\"jam\"", "\"gone\"");
            GameState trimmed = SaveCodec.Decode(defs, json).State;
            Assert.IsNotNull(trimmed.Order);
            Assert.AreEqual(1, trimmed.Order.Lines.Count, "the unknown line is dropped");
            json = SaveCodec.Encode(defs, s).Replace("\"tea\"", "\"gone\"").Replace("\"jam\"", "\"gone\"");
            Assert.IsNull(SaveCodec.Decode(defs, json).State.Order, "no lines: no order");
            json = SaveCodec.Encode(defs, s).Replace("\"left\":77.25", "\"left\":1e9").Replace("\"count\":3", "\"count\":99").Replace(RabbitKinds.All[2].Id, "nobody");
            GameState tamed = SaveCodec.Decode(defs, json).State;
            Assert.IsNotNull(tamed.Order);
            Assert.LessOrEqual(tamed.Order.Left, Orders.WaitSeconds);
            Assert.LessOrEqual(tamed.Order.Lines[1].Count, Tray.StackLimit);
            Assert.IsNotNull(RabbitKinds.Find(tamed.Order.Guest), "an unknown guest becomes a known kind");
        }

        [Test]
        public void The_Quest_Metric_Counts_Deliveries_Today_And_In_All()
        {
            var daily = new QuestDef { Id = "ngay-don", Metric = QuestMetric.OrdersDelivered, Target = 2, Daily = true, RewardCarrots = 10 };
            var ever = new QuestDef { Id = "don-20", Metric = QuestMetric.OrdersDelivered, Target = 20, RewardCarrots = 10 };
            var quests = new List<QuestDef> { daily, ever };
            s.OrdersDelivered = 18;
            Quests.RollDaily(quests, defs, s, Day, Modifiers.None);
            Assert.AreEqual(0, Quests.Progress(daily, defs, s), "the day starts at none");
            Assert.AreEqual(18, Quests.Progress(ever, defs, s));

            for (int k = 0; k < 2; k++)
            {
                s.Order = new Order { Id = k + 1, Guest = RabbitKinds.All[0].Id, Left = 100, RewardSeconds = Orders.RewardSeconds };
                s.Order.Lines.Add(new OrderLine { Station = 0, Count = 1 });
                Tray.Add(s, 0, false);
                Assert.IsTrue(Orders.Deliver(defs, s, Modifiers.None, out _, out _));
            }
            Assert.AreEqual(2, Quests.Progress(daily, defs, s));
            Assert.IsTrue(Quests.CanClaim(daily, defs, s));
            Assert.AreEqual(20, Quests.Progress(ever, defs, s));
            Assert.IsTrue(Quests.CanClaim(ever, defs, s));

            Quests.RollDaily(quests, defs, s, Day + 1, Modifiers.None);
            Assert.AreEqual(0, Quests.Progress(daily, defs, s), "tomorrow counts afresh");
            Assert.AreEqual(20, Quests.Progress(ever, defs, s));
        }
    }
}

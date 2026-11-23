using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class TrayTests
    {
        List<StationDef> defs;
        GameState s;
        readonly List<int> made = new List<int>();

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
            made.Clear();
        }

        void Run(double seconds, double step = 1d)
        {
            for (double t = 0; t < seconds; t += step) Simulation.Tick(defs, s, step, null, null, null, made.Add);
        }

        [Test]
        public void A_Brew_Station_Makes_A_Batch_After_Its_Share_Of_Work_And_Ingredients_Make_None()
        {
            Run(Tray.ProductEvery - 1d);
            Assert.AreEqual(0, s.Tray.Count, "not yet");
            Run(2d);
            Assert.AreEqual(2, s.Tray.Count, "the two brew stations each made one");
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, made);
            foreach (Product p in s.Tray) Assert.AreNotEqual(1, p.Station, "the leaf basket makes nothing to sell");
            Assert.AreEqual(1, s.Tray[0].Count);
            Assert.IsFalse(s.Tray[0].Golden, "no roll, no gold");
        }

        [Test]
        public void A_Closed_Station_Makes_Nothing_And_A_Boost_Counts_Double()
        {
            s.Levels[2] = 0;
            Run(Tray.ProductEvery + 1d);
            Assert.AreEqual(1, s.Tray.Count);
            Assert.AreEqual(0, s.Tray[0].Station);

            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 0, 0 };
            s.Energy = Energy.Max;
            Energy.TryBoost(s);
            Run(Energy.BoostSeconds + (Tray.ProductEvery - Energy.BoostSeconds * Energy.BoostSpeed) + 1d);
            Assert.AreEqual(1, s.Tray.Count, "boosted seconds count double towards a batch");
        }

        [Test]
        public void Products_Of_One_Station_Stack_Up_To_The_Limit_Then_The_Station_Waits_For_Room()
        {
            s.Levels = new[] { 5, 0, 0 };
            Run(Tray.ProductEvery * Tray.StackLimit + 1d);
            Assert.AreEqual(1, s.Tray.Count, "one slot");
            Assert.AreEqual(Tray.StackLimit, s.Tray[0].Count, "a full stack");
            Run(Tray.ProductEvery * 3d);
            Assert.AreEqual(Tray.StackLimit, s.Tray[0].Count, "no more until there is room");
            Assert.AreEqual(Tray.ProductEvery, s.TrayWork[0], 1e-9, "the next batch is held, ready");
            Assert.IsFalse(Tray.CanTake(s, 0));

            Tray.Sell(defs, s, 0, Modifiers.None, out _);
            Run(1d);
            Assert.AreEqual(1, s.Tray.Count, "the held batch lands the moment there is room");
            Assert.AreEqual(1, s.Tray[0].Count);
        }

        [Test]
        public void The_Tray_Is_Full_When_Every_Slot_Is_Taken_And_Every_Stack_At_Its_Limit()
        {
            Assert.AreEqual(Tray.StartingSlots, s.TraySlots);
            Assert.IsFalse(Tray.IsFull(s));
            for (int station = 0; station < 3; station++)
                for (int k = 0; k < Tray.StackLimit; k++) Assert.IsTrue(Tray.Add(s, station, false));
            Assert.IsFalse(Tray.Add(s, 0, false), "a stack at its limit takes no more");
            Assert.IsFalse(Tray.IsFull(s), "two slots are still free");
            Assert.IsTrue(Tray.Add(s, 0, true), "a golden one takes a slot of its own");
            Assert.IsTrue(Tray.Add(s, 2, true));
            Assert.IsTrue(Tray.IsFull(s), "every slot taken, every plain stack at its limit");
            Assert.AreEqual(3 * Tray.StackLimit + 2, Tray.Count(s));
            Assert.IsFalse(Tray.Add(s, 1, true), "a golden one with no slot and no plain room is lost");
        }

        [Test]
        public void Selling_Pays_Seconds_Of_That_Stations_Income_Per_Product_And_Counts_As_Earned()
        {
            Tray.Add(s, 0, false);
            Tray.Add(s, 0, false);
            Tray.Add(s, 2, false);
            BigNum teaPerSecond = Economy.IncomePerCycle(defs, s, 0) / defs[0].CycleSeconds;
            BigNum before = s.Coins;
            BigNum paid = Tray.Sell(defs, s, 0, Modifiers.None, out int clovers);
            Assert.AreEqual((teaPerSecond * (Tray.ProductSeconds * 2d)).ToDouble(), paid.ToDouble(), 1e-9, "two tea products");
            Assert.AreEqual(0, clovers);
            Assert.AreEqual((before + paid).ToDouble(), s.Coins.ToDouble(), 1e-9);
            Assert.AreEqual(paid.ToDouble(), s.TotalEarned.ToDouble(), 1e-9, "sales count towards the daily carrot goal");
            Assert.AreEqual(1, s.Tray.Count, "the jam is still there");
            Assert.AreEqual(2, s.Tray[0].Station);

            BigNum rest = Tray.SellAll(defs, s, Modifiers.None, out _);
            Assert.Greater(rest.ToDouble(), 0d);
            Assert.AreEqual(0, s.Tray.Count);
            Assert.AreEqual(BigNum.Zero.ToDouble(), Tray.Sell(defs, s, 0, Modifiers.None, out _).ToDouble(), "nothing to sell");
        }

        [Test]
        public void A_Working_Rabbit_Raises_The_Price_Of_Its_Stations_Products()
        {
            BigNum plain = Tray.Price(defs, s, 0, Modifiers.None);
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 2, Name = "R", Station = 0 });
            Assert.Greater(Tray.Price(defs, s, 0, Modifiers.None).ToDouble(), plain.ToDouble() * 1.4);
        }

        [Test]
        public void Time_Away_Fills_The_Tray_To_What_It_Holds_And_No_Further()
        {
            s.LastSeenUnixSeconds = 1000;
            Simulation.ClaimAway(defs, s, 1000 + 7200, out double counted);
            Assert.AreEqual(7200, counted, 1e-9);
            Assert.AreEqual(2, s.Tray.Count, "tea and jam");
            foreach (Product p in s.Tray)
            {
                Assert.AreEqual(Tray.StackLimit, p.Count, "two hours makes far more than a stack holds");
                Assert.IsFalse(p.Golden, "nothing golden while away");
            }
            Assert.AreEqual(2 * Tray.StackLimit, Tray.Count(s));
        }

        [Test]
        public void A_Short_Absence_Fills_Only_What_It_Earned()
        {
            s.Levels = new[] { 5, 0, 0 };
            s.LastSeenUnixSeconds = 1000;
            Simulation.ClaimAway(defs, s, 1000 + 190, out _);
            Assert.AreEqual(1, s.Tray.Count);
            Assert.AreEqual(2, s.Tray[0].Count);
            Assert.AreEqual(190 - 2 * Tray.ProductEvery, s.TrayWork[0], 1e-6, "the rest carries on");
        }

        [Test]
        public void Golden_Batches_Are_Rare_Capped_Per_Day_And_Pay_Clovers()
        {
            s.Levels = new[] { 5, 0, 0 };
            int rolls = 0;
            double Always() { rolls++; return 0d; }
            for (int k = 0; k < 4; k++)
            {
                Tray.Advance(s, 0, Tray.ProductEvery, Always);
                Tray.SellAll(defs, s, Modifiers.None, out int clovers);
                Assert.AreEqual(k < Tray.GoldenPerDay ? Tray.GoldenCarrotsEach : 0, clovers, "batch " + k);
            }
            Assert.AreEqual(Tray.GoldenPerDay, s.GoldenToday);
            Assert.AreEqual(Tray.GoldenPerDay, rolls, "past the day's cap the dice are not even rolled");

            Quests.RollDaily(new List<QuestDef>(), defs, s, s.DailyDay + 1, Modifiers.None);
            Assert.AreEqual(0, s.GoldenToday, "a new day starts the count over");
            Tray.Advance(s, 0, Tray.ProductEvery, () => 0.5d);
            Assert.IsFalse(s.Tray[0].Golden, "a high roll is plain");
            Tray.Advance(s, 0, Tray.ProductEvery, () => 0d);
            Assert.AreEqual(2, s.Tray.Count, "a golden one has a slot of its own");
            Assert.IsTrue(s.Tray[1].Golden);
        }

        [Test]
        public void A_Player_Who_Collects_Everything_Earns_About_A_Fifth_More()
        {
            BigNum sold = BigNum.Zero, paidOut = BigNum.Zero;
            double clock = 0;
            while (clock < 600d)
            {
                Simulation.Tick(defs, s, 1d, (i, paid) => paidOut += paid, null);
                clock += 1d;
                if ((int)clock % 90 == 0) sold += Tray.SellAll(defs, s, Modifiers.None, out _);
            }
            sold += Tray.SellAll(defs, s, Modifiers.None, out _);
            double extra = sold.ToDouble() / paidOut.ToDouble();
            Assert.GreaterOrEqual(extra, 0.15, "worth collecting");
            Assert.LessOrEqual(extra, 0.25, "but the garden still earns by itself");
        }

        [Test]
        public void The_Save_Keeps_The_Tray_And_An_Old_Save_Starts_With_An_Empty_One()
        {
            Tray.Add(s, 0, false);
            Tray.Add(s, 0, false);
            Tray.Add(s, 2, true);
            s.TraySlots = 6;
            s.TrayWork[0] = 12.5;
            s.GoldenToday = 1;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, r.Tray.Count);
            Assert.AreEqual(0, r.Tray[0].Station);
            Assert.AreEqual(2, r.Tray[0].Count);
            Assert.IsFalse(r.Tray[0].Golden);
            Assert.AreEqual(2, r.Tray[1].Station);
            Assert.IsTrue(r.Tray[1].Golden);
            Assert.AreEqual(6, r.TraySlots);
            Assert.AreEqual(12.5, r.TrayWork[0], 1e-9);
            Assert.AreEqual(1, r.GoldenToday);

            GameState old = SaveCodec.Decode(defs, "{\"version\":9,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}]}").State;
            Assert.AreEqual(0, old.Tray.Count);
            Assert.AreEqual(Tray.StartingSlots, old.TraySlots);
            Assert.AreEqual(defs.Count, old.TrayWork.Length);

            const string odd = "{\"version\":10,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}],\"traySlots\":40,\"tray\":[" +
                "{\"station\":\"tea\",\"count\":99,\"golden\":false},{\"station\":\"nope\",\"count\":1},{\"station\":\"tea\",\"count\":0}]}";
            GameState mended = SaveCodec.Decode(defs, odd).State;
            Assert.AreEqual(Tray.MaxSlots, mended.TraySlots, "never more slots than the game offers");
            Assert.AreEqual(1, mended.Tray.Count, "an unknown station and an empty stack are dropped");
            Assert.AreEqual(Tray.StackLimit, mended.Tray[0].Count, "a stack never over its limit");
        }
    }
}

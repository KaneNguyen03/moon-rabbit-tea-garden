using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class SimulationTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        [Test]
        public void Tick_Pays_Whole_Cycles_And_Keeps_Remainder()
        {
            Simulation.Tick(defs, s, 4.0);
            Assert.AreEqual(2, s.Coins.ToDouble(), 1e-9);
            Assert.AreEqual(4.0 / 1.5 - 2, s.Progress[0], 1e-9);
        }

        [Test]
        public void Tick_Ignores_Locked_And_Ingredient()
        {
            s.Levels = new[] { 1, 3, 0 };
            Simulation.Tick(defs, s, 100);
            Assert.AreEqual(0, s.Progress[1]);
            Assert.AreEqual(0, s.Progress[2]);
        }

        [Test]
        public void Tick_Reports_Payout_Per_Station()
        {
            int index = -1;
            BigNum paid = BigNum.Zero;
            Simulation.Tick(defs, s, 1.5, (i, p) => { index = i; paid = p; });
            Assert.AreEqual(0, index);
            Assert.AreEqual(1, paid.ToDouble(), 1e-9);
        }

        [Test]
        public void Tick_Negative_Or_NaN_Dt_Does_Nothing()
        {
            Simulation.Tick(defs, s, -5);
            Simulation.Tick(defs, s, double.NaN);
            Assert.AreEqual(0, s.Progress[0]);
            Assert.IsTrue(s.Coins.Equals(BigNum.Zero));
        }

        [Test]
        public void Tick_Uses_Ingredient_Boost()
        {
            s.Levels = new[] { 2, 5, 0 };
            Simulation.Tick(defs, s, 1.5);
            Assert.AreEqual(2 * 1.5, s.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Tap_Advances_By_TapSeconds()
        {
            Assert.IsTrue(Simulation.Tap(defs, s, 0));
            Assert.AreEqual(0.4 / 1.5, s.Progress[0], 1e-9);
            Assert.IsFalse(Simulation.Tap(defs, s, 2));
            Assert.IsFalse(Simulation.Tap(defs, s, -1));
        }

        [Test]
        public void Taps_Past_A_Full_Cycle_Pay_On_The_Next_Tick()
        {
            for (int i = 0; i < 4; i++) Simulation.Tap(defs, s, 0);
            Simulation.Tick(defs, s, 0.001);
            Assert.AreEqual(1, s.Coins.ToDouble(), 1e-9);
            Assert.Less(s.Progress[0], 1d);
        }

        [Test]
        public void Buy_Deducts_And_Levels()
        {
            s.Coins = new BigNum(100);
            Assert.IsTrue(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(2, s.Levels[0]);
            Assert.AreEqual(100 - 10 * 1.09, s.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Buy_Fails_Cleanly_When_Broke()
        {
            Assert.IsFalse(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(1, s.Levels[0]);
            Assert.IsTrue(s.Coins.Equals(BigNum.Zero));
        }

        [Test]
        public void Unlock_Costs_BaseCost_And_Opens_Station()
        {
            s.Coins = new BigNum(120);
            Assert.IsTrue(Simulation.TryBuy(defs, s, 1));
            Assert.AreEqual(1, s.Levels[1]);
            Assert.IsTrue(s.Coins.ToDouble() < 1e-6);
        }

        [Test]
        public void Cannot_Skip_Unlock_Order()
        {
            s.Coins = new BigNum(1e9);
            Assert.IsFalse(Simulation.TryBuy(defs, s, 2));
            Assert.AreEqual(0, s.Levels[2]);
        }

        [Test]
        public void BuyMode_Max_Buys_All_Affordable()
        {
            s.Coins = new BigNum(1000);
            s.BuyMode = 0;
            int n = Simulation.BuyCount(defs, s, 0);
            Assert.IsTrue(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(1 + n, s.Levels[0]);
            Assert.Greater(n, 1);
        }

        [Test]
        public void BuyMode_Ten_Needs_The_Full_Price()
        {
            s.BuyMode = 10;
            s.Coins = new BigNum(50);
            Assert.IsFalse(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(1, s.Levels[0]);
        }

        [Test]
        public void Away_Pays_Elapsed()
        {
            s.LastSeenUnixSeconds = 1000;
            BigNum paid = Simulation.ClaimAway(defs, s, 1600, out double counted);
            Assert.AreEqual(600, counted);
            Assert.AreEqual(600 / 1.5, paid.ToDouble(), 1e-6);
            Assert.AreEqual(600 / 1.5, s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(1600, s.LastSeenUnixSeconds);
        }

        [Test]
        public void Away_Is_Capped_At_Three_Hours()
        {
            // Three hours since #69 (two before).
            s.LastSeenUnixSeconds = 0;
            BigNum paid = Simulation.ClaimAway(defs, s, 30L * 86400, out double counted);
            Assert.AreEqual(10800, counted);
            Assert.AreEqual(10800 / 1.5, paid.ToDouble(), 1e-6);
        }

        [Test]
        public void Away_Clock_Moved_Back_Pays_Nothing()
        {
            s.LastSeenUnixSeconds = 5000;
            BigNum paid = Simulation.ClaimAway(defs, s, 1000, out double counted);
            Assert.IsTrue(paid.Equals(BigNum.Zero));
            Assert.AreEqual(0, counted);
            Assert.AreEqual(1000, s.LastSeenUnixSeconds);
        }

        [Test]
        public void Away_Under_Thirty_Seconds_Pays_Nothing()
        {
            s.LastSeenUnixSeconds = 1000;
            Assert.IsTrue(Simulation.ClaimAway(defs, s, 1020, out _).Equals(BigNum.Zero));
        }
    }
}

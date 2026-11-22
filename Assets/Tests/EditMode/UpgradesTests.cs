using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class UpgradesTests
    {
        List<StationDef> defs;
        List<UpgradeDef> ups;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            ups = new List<UpgradeDef>
            {
                new UpgradeDef { Id = "hand", Effect = UpgradeEffect.TapSeconds, Amount = 0.2, BaseCost = 80, CostGrowth = 3, MaxLevel = 10 },
                new UpgradeDef { Id = "wind", Effect = UpgradeEffect.Speed, Amount = 0.1, BaseCost = 600, CostGrowth = 5, MaxLevel = 10 },
                new UpgradeDef { Id = "moon", Effect = UpgradeEffect.IncomeMultiplier, Amount = 2, BaseCost = 3000, CostGrowth = 15, MaxLevel = 2 },
                new UpgradeDef { Id = "sleep", Effect = UpgradeEffect.AwayHours, Amount = 2, BaseCost = 5000, CostGrowth = 10, MaxLevel = 5 },
            };
            s = GameState.NewGame(defs, ups.Count);
        }

        [Test]
        public void Each_Charm_Says_What_It_Does_At_A_Level()
        {
            var tap = new UpgradeDef { Effect = UpgradeEffect.TapSeconds, Amount = 0.2 };
            var speed = new UpgradeDef { Effect = UpgradeEffect.Speed, Amount = 0.1 };
            var income = new UpgradeDef { Effect = UpgradeEffect.IncomeMultiplier, Amount = 2 };
            var away = new UpgradeDef { Effect = UpgradeEffect.AwayHours, Amount = 2 };
            Assert.AreEqual(0d, Upgrades.EffectAt(tap, 0), 1e-9);
            Assert.AreEqual(0.6, Upgrades.EffectAt(tap, 3), 1e-9);
            Assert.AreEqual(0.2, Upgrades.EffectAt(speed, 2), 1e-9);
            Assert.AreEqual(1d, Upgrades.EffectAt(income, 0), 1e-9, "no charm: income as it is");
            Assert.AreEqual(8d, Upgrades.EffectAt(income, 3), 1e-9);
            Assert.AreEqual(4d, Upgrades.EffectAt(away, 2), 1e-9);
            Assert.AreEqual(0d, Upgrades.EffectAt(away, -5), 1e-9, "never below the base game");
        }

        [Test]
        public void No_Upgrades_Is_The_Base_Game()
        {
            Modifiers m = Modifiers.From(ups, s);
            Assert.AreEqual(1, m.IncomeMultiplier);
            Assert.AreEqual(1, m.SpeedMultiplier);
            Assert.AreEqual(Simulation.TapSeconds, m.TapSeconds);
            Assert.AreEqual(Simulation.AwayCapSeconds, m.AwayCapSeconds);
        }

        [Test]
        public void Levels_Add_Up()
        {
            s.UpgradeLevels = new[] { 3, 2, 2, 1 };
            Modifiers m = Modifiers.From(ups, s);
            Assert.AreEqual(0.4 + 0.6, m.TapSeconds, 1e-12);
            Assert.AreEqual(1.2, m.SpeedMultiplier, 1e-12);
            Assert.AreEqual(4, m.IncomeMultiplier, 1e-12);
            Assert.AreEqual(10800 + 7200, m.AwayCapSeconds, 1e-9);
        }

        [Test]
        public void Buying_Costs_More_Each_Level_And_Stops_At_Max()
        {
            s.Coins = new BigNum(1e9);
            Assert.AreEqual(3000, Upgrades.CostOfNext(ups[2], 0).ToDouble(), 1e-6);
            Assert.AreEqual(45000, Upgrades.CostOfNext(ups[2], 1).ToDouble(), 1e-6);
            Assert.IsTrue(Upgrades.TryBuy(ups, s, 2));
            Assert.IsTrue(Upgrades.TryBuy(ups, s, 2));
            Assert.IsFalse(Upgrades.TryBuy(ups, s, 2), "maxed");
            Assert.AreEqual(2, s.UpgradeLevels[2]);
            Assert.AreEqual(1e9 - 48000, s.Coins.ToDouble(), 1e-3);
        }

        [Test]
        public void Cannot_Buy_When_Broke_Or_Out_Of_Range()
        {
            Assert.IsFalse(Upgrades.TryBuy(ups, s, 0));
            Assert.IsFalse(Upgrades.TryBuy(ups, s, 9));
            Assert.AreEqual(0, s.UpgradeLevels[0]);
        }

        [Test]
        public void Tick_Uses_Speed_And_Income()
        {
            s.UpgradeLevels = new[] { 0, 10, 1, 0 }; // twice as fast, double income
            Simulation.Tick(defs, s, 1.5, null, Modifiers.From(ups, s));
            Assert.AreEqual(4, s.Coins.ToDouble(), 1e-9); // two cycles × 1 × 2
            Assert.AreEqual(4, s.TotalEarned.ToDouble(), 1e-9);
        }

        [Test]
        public void Tap_Uses_The_Upgraded_Tap_And_Counts()
        {
            s.UpgradeLevels = new[] { 3, 0, 0, 0 };
            Simulation.Tap(defs, s, 0, Modifiers.From(ups, s));
            Assert.AreEqual(1.0 / 1.5, s.Progress[0], 1e-9);
            Assert.AreEqual(1, s.TotalTaps);
        }

        [Test]
        public void Away_Uses_The_Longer_Cap_And_Multipliers()
        {
            s.UpgradeLevels = new[] { 0, 0, 1, 1 };
            s.LastSeenUnixSeconds = 0;
            BigNum paid = Simulation.ClaimAway(defs, s, 30L * 86400, out double counted, Modifiers.From(ups, s));
            Assert.AreEqual(18000, counted, "three hours (#69) plus the charm's two");
            Assert.AreEqual(18000 / 1.5 * 2, paid.ToDouble(), 1e-6);
        }

        [Test]
        public void Buying_Station_Levels_Is_Counted()
        {
            s.Coins = new BigNum(1e6);
            s.BuyMode = 10;
            Simulation.TryBuy(defs, s, 0);
            Assert.AreEqual(10, s.LevelsBought);
        }
    }
}

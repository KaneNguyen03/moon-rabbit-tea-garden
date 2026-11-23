using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class StationCapTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 1, 1, 1 };
            s.Coins = new BigNum(1e300);
        }

        [Test]
        public void The_Cap_Is_The_Last_Milestone()
        {
            Assert.AreEqual(500, Economy.MaxLevel);
            Assert.AreEqual(Economy.Milestones[Economy.Milestones.Length - 1], Economy.MaxLevel);
            Assert.AreEqual(5, Economy.LevelsLeft(495));
            Assert.AreEqual(0, Economy.LevelsLeft(500));
            Assert.AreEqual(0, Economy.LevelsLeft(777), "a save from a wilder build does not go negative");
        }

        [TestCase(Simulation.BuyOne, 499, 1)]
        [TestCase(Simulation.BuyTen, 495, 5)]
        [TestCase(Simulation.BuyMax, 490, 10)]
        [TestCase(Simulation.BuyToMilestone, 480, 20)]
        [TestCase(Simulation.BuyTen, 500, 0)]
        [TestCase(Simulation.BuyMax, 500, 0)]
        public void Every_Buy_Mode_Stops_At_The_Cap(int mode, int level, int expected)
        {
            s.BuyMode = mode;
            s.Levels[0] = level;
            Assert.AreEqual(expected, Simulation.BuyCount(defs, s, 0));
        }

        [Test]
        public void Buying_Lands_On_The_Cap_And_Then_Refuses()
        {
            s.BuyMode = Simulation.BuyTen;
            s.Levels[0] = 497;
            BigNum before = s.Coins;
            Assert.IsTrue(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(500, s.Levels[0]);
            Assert.AreEqual(Economy.CostOfNext(defs[0], 497, 3).ToDouble(), (before - s.Coins).ToDouble(), 1e280, "paid for three levels, not ten");
            before = s.Coins;
            Assert.IsFalse(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(500, s.Levels[0]);
            Assert.AreEqual(before, s.Coins, "nothing spent at the cap");
        }

        [Test]
        public void Max_Affordable_Never_Reaches_Past_The_Cap()
        {
            Assert.AreEqual(3, Economy.MaxAffordable(defs[0], 497, s.Coins));
            Assert.AreEqual(0, Economy.MaxAffordable(defs[0], 500, s.Coins));
            Assert.AreEqual(1, Economy.MaxAffordable(defs[0], 1, Economy.CostOfNext(defs[0], 1, 1)), "the usual rule below the cap");
        }

        [Test]
        public void Upgrade_Everything_Stops_Every_Station_At_The_Cap()
        {
            s.Levels = new[] { 480, 495, 1 };
            List<UpgradeStep> plan = UpgradePlan.Plan(defs, s, s.Coins);
            foreach (UpgradeStep step in plan)
                Assert.LessOrEqual(s.Levels[step.Station] + step.Levels, Economy.MaxLevel, "station " + step.Station);
        }
    }
}

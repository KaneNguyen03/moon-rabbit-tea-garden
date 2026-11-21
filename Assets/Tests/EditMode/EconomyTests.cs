using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class EconomyTests
    {
        List<StationDef> defs;
        StationDef tea, leaf;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            tea = defs[0];
            leaf = defs[1];
            s = GameState.NewGame(defs);
        }

        [TestCase(1, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 2)]
        [TestCase(25, 4)]
        [TestCase(500, 512)]
        [TestCase(9999, 512)]
        public void Milestone(int level, double multiplier)
        {
            Assert.AreEqual(multiplier, Economy.MilestoneMultiplier(level));
        }

        [Test]
        public void Next_And_Previous_Milestone()
        {
            Assert.AreEqual(10, Economy.NextMilestone(1));
            Assert.AreEqual(25, Economy.NextMilestone(10));
            Assert.AreEqual(0, Economy.NextMilestone(500));
            Assert.AreEqual(0, Economy.PreviousMilestone(9));
            Assert.AreEqual(10, Economy.PreviousMilestone(24));
        }

        [Test]
        public void NewGame_Opens_Only_The_First_Station()
        {
            CollectionAssert.AreEqual(new[] { 1, 0, 0 }, s.Levels);
            Assert.IsTrue(s.Coins.Equals(BigNum.Zero));
            Assert.AreEqual(1, s.BuyMode);
        }

        [Test]
        public void Unlock_Costs_BaseCost()
        {
            Assert.AreEqual(120, Economy.CostOfNext(leaf, 0, 1).ToDouble(), 1e-9);
        }

        [Test]
        public void Cost_Of_Ten_Is_Geometric_Sum()
        {
            double expected = 0;
            for (int k = 1; k <= 10; k++) expected += 10 * Math.Pow(1.09, k);
            Assert.AreEqual(expected, Economy.CostOfNext(tea, 1, 10).ToDouble(), 1e-6);
        }

        [Test]
        public void MaxAffordable_Is_Exact()
        {
            var coins = new BigNum(1000);
            int n = Economy.MaxAffordable(tea, 1, coins);
            Assert.IsTrue(Economy.CostOfNext(tea, 1, n) <= coins);
            Assert.IsTrue(Economy.CostOfNext(tea, 1, n + 1) > coins);
        }

        [Test]
        public void MaxAffordable_Zero_When_Broke()
        {
            Assert.AreEqual(0, Economy.MaxAffordable(tea, 1, BigNum.Zero));
            Assert.AreEqual(0, Economy.MaxAffordable(tea, 1, new BigNum(5)));
        }

        [Test]
        public void Ingredient_Boosts_Only_Its_Targets()
        {
            s.Levels = new[] { 4, 5, 2 };
            Assert.AreEqual(1 * 4 * 1.5, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
            Assert.AreEqual(80 * 2, Economy.IncomePerCycle(defs, s, 2).ToDouble(), 1e-9);
            Assert.IsTrue(Economy.IncomePerCycle(defs, s, 1).Equals(BigNum.Zero));
        }

        [Test]
        public void Ingredient_Boost_Doubles_At_Milestones()
        {
            Assert.AreEqual(1, Economy.IngredientBoost(0));
            Assert.AreEqual(1.9, Economy.IngredientBoost(9), 1e-12);
            Assert.AreEqual(3, Economy.IngredientBoost(10), 1e-12);
        }

        [Test]
        public void IncomePerSecond_Sums_Brew_Stations()
        {
            s.Levels = new[] { 3, 0, 1 };
            Assert.AreEqual(3 / 1.5 + 80 / 5.0, Economy.IncomePerSecond(defs, s).ToDouble(), 1e-9);
        }

        [Test]
        public void CanUnlock_Requires_Previous()
        {
            Assert.IsTrue(Economy.CanUnlock(s, 1));
            Assert.IsFalse(Economy.CanUnlock(s, 2));
            Assert.IsFalse(Economy.CanUnlock(s, 99));
        }

        [Test]
        public void Cost_At_High_Level_Is_Finite()
        {
            BigNum cost = Economy.CostOfNext(tea, 20000, 1);
            Assert.IsTrue(cost > BigNum.FromParts(1, 308));
            StringAssert.DoesNotContain("Infinity", cost.ToShortString());
            StringAssert.DoesNotContain("NaN", cost.ToShortString());
        }
    }
}

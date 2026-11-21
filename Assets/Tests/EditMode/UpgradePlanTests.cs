using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>Quick upgrades: the "to milestone" mode, the spend share, upgrading everything, and rabbits to the cap.</summary>
    public class UpgradePlanTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
        }

        [Test]
        public void To_Milestone_Buys_Just_Enough_To_Reach_The_Next_Doubling()
        {
            s.Coins = new BigNum(1e9);
            s.BuyMode = Simulation.BuyToMilestone;
            Assert.AreEqual(5, Simulation.BuyCount(defs, s, 0), "level 5 to 10");
            Assert.IsTrue(Simulation.TryBuy(defs, s, 0));
            Assert.AreEqual(10, s.Levels[0]);
            Assert.AreEqual(15, Simulation.BuyCount(defs, s, 0), "then 10 to 25");
        }

        [Test]
        public void To_Milestone_Buys_What_It_Can_When_The_Purse_Is_Short()
        {
            s.BuyMode = Simulation.BuyToMilestone;
            s.Coins = Economy.CostOfNext(defs[0], 5, 2);
            Assert.AreEqual(2, Simulation.BuyCount(defs, s, 0));
            s.Coins = BigNum.Zero;
            Assert.AreEqual(1, Simulation.BuyCount(defs, s, 0), "never less than one, so the button still says what one costs");
        }

        [Test]
        public void Past_The_Last_Milestone_To_Milestone_Behaves_Like_Max()
        {
            s.Levels[0] = 600;
            s.Coins = new BigNum(1e40);
            s.BuyMode = Simulation.BuyToMilestone;
            Assert.AreEqual(Economy.MaxAffordable(defs[0], 600, s.Coins), Simulation.BuyCount(defs, s, 0));
        }

        [Test]
        public void The_Spend_Share_Caps_Max_And_To_Milestone_But_Not_Plain_Counts()
        {
            s.Coins = new BigNum(1000);
            s.SpendShare = 0.5;
            s.BuyMode = Simulation.BuyMax;
            Assert.AreEqual(Economy.MaxAffordable(defs[0], 5, new BigNum(500)), Simulation.BuyCount(defs, s, 0));
            Assert.Less(Simulation.BuyCount(defs, s, 0), Economy.MaxAffordable(defs[0], 5, new BigNum(1000)));
            s.BuyMode = Simulation.BuyTen;
            Assert.AreEqual(10, Simulation.BuyCount(defs, s, 0));
            s.SpendShare = 1d;
            s.BuyMode = Simulation.BuyMax;
            Assert.AreEqual(Economy.MaxAffordable(defs[0], 5, new BigNum(1000)), Simulation.BuyCount(defs, s, 0));
        }

        [Test]
        public void A_Plan_Never_Spends_More_Than_Its_Budget_And_Equals_Buying_One_Level_At_A_Time()
        {
            s.Coins = new BigNum(5000);
            var budget = new BigNum(2500);
            List<UpgradeStep> plan = UpgradePlan.Plan(defs, s, budget);
            Assert.Greater(plan.Count, 0);
            Assert.LessOrEqual(UpgradePlan.Total(plan).ToDouble(), budget.ToDouble());

            // Applied in one go.
            GameState applied = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.IsTrue(UpgradePlan.Apply(defs, applied, plan));

            // The same purchases, one level at a time, in the same order.
            GameState oneByOne = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            oneByOne.BuyMode = Simulation.BuyOne;
            foreach (UpgradeStep step in plan)
                for (int k = 0; k < step.Levels; k++)
                    Assert.IsTrue(Simulation.TryBuy(defs, oneByOne, step.Station));

            CollectionAssert.AreEqual(oneByOne.Levels, applied.Levels);
            Assert.AreEqual(oneByOne.Coins.ToDouble(), applied.Coins.ToDouble(), 1e-6 * s.Coins.ToDouble());
            Assert.AreEqual(oneByOne.LevelsBought, applied.LevelsBought);
            Assert.AreEqual(oneByOne.GardenXp, applied.GardenXp, 1e-9);
            Assert.GreaterOrEqual(applied.Coins.ToDouble(), 2500 - 1e-6, "the half kept back is still there");
        }

        [Test]
        public void A_Plan_Puts_The_Carrots_Where_They_Earn_Most()
        {
            // Tea pays 0,67 a second per level for about 10 carrots; jam pays 16 a second per level for about 1500. Tea first.
            s.Coins = new BigNum(100);
            List<UpgradeStep> plan = UpgradePlan.Plan(defs, s, s.Coins);
            Assert.AreEqual(0, plan[0].Station);
            Assert.AreEqual(1, plan.Count, "nothing else can be paid for");
        }

        [Test]
        public void A_Plan_Leaves_Closed_Stations_Alone_And_Refuses_An_Unpayable_Plan()
        {
            s.Levels = new[] { 5, 2, 0 };
            List<UpgradeStep> plan = UpgradePlan.Plan(defs, s, new BigNum(1e7));
            foreach (UpgradeStep step in plan) Assert.AreNotEqual(2, step.Station);
            s.Coins = BigNum.Zero;
            Assert.IsFalse(UpgradePlan.Apply(defs, s, plan));
            CollectionAssert.AreEqual(new[] { 5, 2, 0 }, s.Levels);
            Assert.IsFalse(UpgradePlan.Apply(defs, s, new List<UpgradeStep>()), "an empty plan buys nothing");
        }

        [Test]
        public void A_Plan_With_Nothing_Affordable_Is_Empty()
        {
            Assert.AreEqual(0, UpgradePlan.Plan(defs, s, BigNum.Zero).Count);
        }

        [Test]
        public void A_Rabbit_Levels_Up_To_The_Cap_And_No_Further_Than_The_Budget_Allows()
        {
            var r = new Rabbit { Id = 1, Tier = 0, Name = "R", Level = 8 };
            s.Rabbits.Add(r);
            s.Coins = new BigNum(1e9);
            Assert.AreEqual(2, Rabbits.LevelsAffordable(r, s.Coins, out BigNum cost), "silver caps at 10");
            Assert.AreEqual((Rabbits.LevelCost(new Rabbit { Tier = 0, Level = 8 }) + Rabbits.LevelCost(new Rabbit { Tier = 0, Level = 9 })).ToDouble(), cost.ToDouble(), 1e-6);
            Assert.AreEqual(2, Rabbits.LevelUpMax(s, 1, s.Coins));
            Assert.AreEqual(10, r.Level);
            Assert.AreEqual(0, Rabbits.LevelUpMax(s, 1, s.Coins), "at the cap");

            var g = new Rabbit { Id = 2, Tier = 1, Name = "G", Level = 1 };
            s.Rabbits.Add(g);
            BigNum two = Rabbits.LevelCost(new Rabbit { Tier = 1, Level = 1 }) + Rabbits.LevelCost(new Rabbit { Tier = 1, Level = 2 });
            Assert.AreEqual(2, Rabbits.LevelUpMax(s, 2, two));
            Assert.AreEqual(3, g.Level);
            Assert.AreEqual(0, Rabbits.LevelUpMax(s, 2, BigNum.Zero));
        }

        [Test]
        public void The_Save_Keeps_The_Milestone_Mode_And_The_Spend_Share()
        {
            s.BuyMode = Simulation.BuyToMilestone;
            s.SpendShare = 0.25;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(Simulation.BuyToMilestone, r.BuyMode);
            Assert.AreEqual(0.25, r.SpendShare);
            GameState old = SaveCodec.Decode(defs, "{\"version\":8,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}],\"buyMode\":10}").State;
            Assert.AreEqual(1d, old.SpendShare, "an older save spends freely, as it always did");
            Assert.AreEqual(Simulation.BuyTen, old.BuyMode);
            GameState odd = SaveCodec.Decode(defs, "{\"version\":9,\"stations\":[],\"spendShare\":0.7}").State;
            Assert.AreEqual(1d, odd.SpendShare, "a share the game never offers is not kept");
        }
    }
}

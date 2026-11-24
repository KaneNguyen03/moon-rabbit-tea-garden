using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class RecipesTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel);
        }

        Rabbit Add(int tier, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Name = "R" + s.NextRabbitId, Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        [Test]
        public void Raising_A_Dish_From_K_Costs_Five_K_Squared_Leaves_And_The_Whole_Way_Is_1425()
        {
            Assert.AreEqual(5, Recipes.CostOfNext(1));
            Assert.AreEqual(20, Recipes.CostOfNext(2));
            Assert.AreEqual(405, Recipes.CostOfNext(9));
            Assert.AreEqual(0, Recipes.CostOfNext(Recipes.MaxLevel), "nothing past level ten");
            Assert.AreEqual(0, Recipes.CostOfNext(0));
            int total = 0;
            for (int k = 1; k < Recipes.MaxLevel; k++) total += Recipes.CostOfNext(k);
            Assert.AreEqual(1425, total);
        }

        [Test]
        public void A_Dish_Doubles_Its_Station_From_Level_One_To_Ten_In_Even_Steps()
        {
            Assert.AreEqual(1d, Recipes.LevelMultiplier(1), 1e-12, "level one changes nothing, so old gardens earn as before");
            Assert.AreEqual(2d, Recipes.LevelMultiplier(Recipes.MaxLevel), 1e-12);
            for (int k = 1; k < Recipes.MaxLevel; k++)
                Assert.AreEqual(1d / 9d, Recipes.LevelMultiplier(k + 1) - Recipes.LevelMultiplier(k), 1e-12, "level " + k);
            Assert.AreEqual(1d, Recipes.LevelMultiplier(0), 1e-12);
            Assert.AreEqual(2d, Recipes.LevelMultiplier(99), 1e-12);
        }

        [Test]
        public void Part_A_Opens_One_Dish_And_The_Station_Cap_Is_Two_Times_A_Quarter_Squared()
        {
            Assert.AreEqual(3, Recipes.Dishes);
            Assert.AreEqual(1, Recipes.DishesOpen(s, 0));
            Assert.AreEqual(0, Recipes.CurrentDish(s, 0));
            Assert.AreEqual(2d * 1.25d * 1.25d, Recipes.MaxStationMultiplier, 1e-12);
            Assert.AreEqual(1d, Recipes.StationMultiplier(s, 0), 1e-12);
        }

        [Test]
        public void The_Book_Opens_At_The_First_New_Moons_Garden_Level_Or_After_A_New_Moon()
        {
            Assert.AreEqual(NewMoon.FirstLevel, Recipes.UnlockLevel);
            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel - 1);
            Assert.IsFalse(Recipes.Unlocked(s));
            Add(0);
            Add(0);
            Simulation.Tick(defs, s, 3600d);
            s.LastSeenUnixSeconds = 1_000_000;
            Simulation.ClaimAway(defs, s, 1_000_000 + 3600, out _);
            Assert.AreEqual(0, s.RecipeLeaves, "no leaves before the book opens");
            Assert.AreEqual(0d, s.RestLeafSeconds, "nor time saved up towards one");
            s.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 100, RewardSeconds = Orders.RewardSeconds };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 1 });
            Tray.Add(s, 0, false);
            Assert.IsTrue(Orders.Deliver(defs, s, Modifiers.None, out _, out _));
            Assert.AreEqual(0, s.RecipeLeaves, "an order pays as before");
            s.RecipeLeaves = 100;
            Assert.IsFalse(Recipes.Raise(s, 0), "and nothing to raise");

            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel);
            Assert.IsTrue(Recipes.Unlocked(s));
            s.GardenXp = 0;
            s.Season = 1;
            Assert.IsTrue(Recipes.Unlocked(s), "after a new moon the garden starts low again, and the book stays open");
            Assert.IsTrue(Recipes.Raise(s, 0));
        }

        [Test]
        public void Raising_Spends_The_Leaves_Needs_An_Open_Station_And_Stops_At_Ten()
        {
            Assert.IsFalse(Recipes.Raise(s, 0), "no leaves yet");
            s.RecipeLeaves = 30;
            Assert.IsTrue(Recipes.Raise(s, 0));
            Assert.AreEqual(25, s.RecipeLeaves);
            Assert.AreEqual(2, Recipes.Level(s, 0));
            Assert.IsTrue(Recipes.Raise(s, 0));
            Assert.AreEqual(5, s.RecipeLeaves);
            Assert.AreEqual(3, Recipes.Level(s, 0));
            Assert.IsFalse(Recipes.Raise(s, 0), "45 leaves for the next, 5 in hand");
            Assert.AreEqual(5, s.RecipeLeaves);

            s.Levels[2] = 0;
            Assert.IsFalse(Recipes.Raise(s, 2), "a station not open has no dish to raise");
            Assert.IsFalse(Recipes.Raise(s, -1));
            Assert.IsFalse(Recipes.Raise(s, 9));
            Assert.AreEqual(1, Recipes.Level(s, 1), "only the raised station moved");

            s.RecipeLeaves = 10_000;
            for (int k = 0; k < 20; k++) Recipes.Raise(s, 1);
            Assert.AreEqual(Recipes.MaxLevel, Recipes.Level(s, 1));
            Assert.AreEqual(10_000 - 1425, s.RecipeLeaves);
            Assert.AreEqual(0, Recipes.RaiseCost(s, 1));
        }

        [Test]
        public void A_Dish_Level_Multiplies_Only_Its_Own_Stations_Income()
        {
            BigNum tea = Economy.IncomePerCycle(defs, s, 0), jam = Economy.IncomePerCycle(defs, s, 2);
            s.RecipeLeaves = 100;
            Recipes.Raise(s, 0);
            Recipes.Raise(s, 0);
            double three = Recipes.LevelMultiplier(3);
            Assert.AreEqual((tea * three).ToDouble(), Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-6);
            Assert.AreEqual(jam.ToDouble(), Economy.IncomePerCycle(defs, s, 2).ToDouble(), 1e-9, "the jam pot earns as before");
            Assert.AreEqual(three, Recipes.StationMultiplier(s, 0), 1e-12);
        }

        [Test]
        public void An_Ingredient_Dish_Strengthens_Its_Boost_The_Way_A_Rabbit_Does()
        {
            double plain = Economy.IngredientBoost(defs[1], s.Levels[1]) - 1d;
            BigNum before = Economy.IncomePerCycle(defs, s, 0);
            s.RecipeLeaves = 5;
            Assert.IsTrue(Recipes.Raise(s, 1));
            double raised = plain * Recipes.LevelMultiplier(2);
            Assert.AreEqual((before / (1d + plain) * (1d + raised)).ToDouble(), Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-6);
        }

        [Test]
        public void Each_Seated_Rabbit_Makes_A_Leaf_Every_Ten_Minutes_And_Workers_Make_None()
        {
            Add(0);
            Add(3);
            Add(1, 0);
            Simulation.Tick(defs, s, 300d);
            Assert.AreEqual(1, s.RecipeLeaves, "two resting rabbits, five minutes each: one leaf");
            for (int i = 0; i < 270; i++) Simulation.Tick(defs, s, 1d);
            Assert.AreEqual(1, s.RecipeLeaves);
            for (int i = 0; i < 30; i++) Simulation.Tick(defs, s, 1d);
            Assert.AreEqual(2, s.RecipeLeaves, "ten minutes for both: two leaves");

            Add(4);
            Assert.AreEqual(RestSpots.Starting, RestSpots.SeatedCount(s), "a third resting rabbit has no place");
            Simulation.Tick(defs, s, 600d);
            Assert.AreEqual(4, s.RecipeLeaves, "only the seated count");
        }

        [Test]
        public void Nobody_Resting_Makes_No_Leaves()
        {
            Add(2, 0);
            Simulation.Tick(defs, s, 3600d);
            Assert.AreEqual(0, s.RecipeLeaves);
        }

        [Test]
        public void Leaves_Grow_While_Away_Up_To_The_Away_Cap()
        {
            Add(0);
            Add(0);
            s.LastSeenUnixSeconds = 1_000_000;
            Simulation.ClaimAway(defs, s, 1_000_000 + 3600, out _);
            Assert.AreEqual(12, s.RecipeLeaves, "an hour, two rabbits: twelve leaves");
            Simulation.ClaimAway(defs, s, 1_000_000 + 3600 + 10 * 3600, out double counted);
            Assert.AreEqual(Simulation.AwayCapSeconds, counted, 1e-9);
            Assert.AreEqual(12 + 2 * (int)(Simulation.AwayCapSeconds / Recipes.RestSecondsPerLeaf), s.RecipeLeaves, "ten hours away count as the three-hour cap");
        }

        [Test]
        public void A_Clock_Turned_Back_Makes_No_Leaves()
        {
            Add(0);
            s.LastSeenUnixSeconds = 2_000_000;
            Simulation.ClaimAway(defs, s, 1_000_000, out _);
            Assert.AreEqual(0, s.RecipeLeaves);
            Simulation.ClaimAway(defs, s, 1_000_000 + 1200, out _);
            Assert.AreEqual(2, s.RecipeLeaves, "time counts again from where the clock was set back to");
        }

        [Test]
        public void A_Delivered_Order_Brings_Two_Leaves()
        {
            s.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 100, RewardSeconds = Orders.RewardSeconds };
            s.Order.Lines.Add(new OrderLine { Station = 0, Count = 1 });
            Assert.IsFalse(Orders.Deliver(defs, s, Modifiers.None, out _, out _));
            Assert.AreEqual(0, s.RecipeLeaves, "an order not delivered brings nothing");
            Tray.Add(s, 0, false);
            Assert.IsTrue(Orders.Deliver(defs, s, Modifiers.None, out _, out _));
            Assert.AreEqual(Recipes.LeavesPerOrder, s.RecipeLeaves);
            Assert.AreEqual(2, Recipes.LeavesPerOrder);
        }

        [Test]
        public void A_Ten_Heart_Rabbit_Turns_A_Third_Tier_Fish_Or_Better_Into_A_Leaf()
        {
            Rabbit full = Add(0, 0);
            full.Affection = Rabbits.AffectionMax;
            s.Fish[1] = 1;
            s.Fish[2] = 1;
            s.Fish[5] = 1;
            s.GoldenCarrots = 0;

            Assert.IsFalse(Fishing.Feed(s, full, 1).Fed, "a second-tier fish is not worth a leaf");
            Assert.AreEqual(1, s.Fish[1]);

            FeedResult fed = Fishing.Feed(s, full, 2);
            Assert.IsTrue(fed.Fed);
            Assert.AreEqual(1, fed.Leaves);
            Assert.AreEqual(0, fed.Points, "no bond points past ten hearts");
            Assert.IsFalse(fed.Bond.TenHearts);
            Assert.AreEqual(Rabbits.AffectionMax, full.Affection);
            Assert.AreEqual(0, s.Fish[2]);
            Assert.AreEqual(1, s.RecipeLeaves);
            Assert.AreEqual(0, s.GoldenCarrots, "the ten-heart gift is not paid again");

            Assert.AreEqual(5, Fishing.BestInBasket(s));
            Assert.IsTrue(Fishing.Feed(s, full, 5).Fed);
            Assert.AreEqual(2, s.RecipeLeaves);

            Rabbit young = Add(0, 1);
            s.Fish[3] = 1;
            FeedResult bond = Fishing.Feed(s, young, 3);
            Assert.IsTrue(bond.Fed);
            Assert.AreEqual(0, bond.Leaves, "below ten hearts a fish is bond points, not a leaf");
            Assert.AreEqual(2, s.RecipeLeaves);
        }

        [Test]
        public void A_New_Moon_Keeps_The_Leaves_And_The_Dish_Levels()
        {
            s.RecipeLeaves = 30;
            Recipes.Raise(s, 0);
            s.GardenXp = Garden.XpForLevel(NewMoon.LevelNeeded(0));
            Assert.IsTrue(NewMoon.Begin(defs, s, "M", out _));
            Assert.AreEqual(25, s.RecipeLeaves);
            Assert.AreEqual(2, Recipes.Level(s, 0));
        }

        [Test]
        public void Leaves_And_Dish_Levels_Round_Trip_In_The_Save()
        {
            s.RecipeLeaves = 500;
            Recipes.Raise(s, 0);
            Recipes.Raise(s, 0);
            Recipes.Raise(s, 2);
            s.RestLeafSeconds = 123.5;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.RecipeLeaves, r.RecipeLeaves);
            Assert.AreEqual(123.5, r.RestLeafSeconds, 1e-9);
            Assert.AreEqual(3, Recipes.Level(r, 0));
            Assert.AreEqual(1, Recipes.Level(r, 1));
            Assert.AreEqual(2, Recipes.Level(r, 2));
            Assert.AreEqual(SaveCodec.Encode(defs, s), SaveCodec.Encode(defs, r));
        }

        [Test]
        public void An_Old_Save_Starts_With_No_Leaves_And_Every_Dish_At_Level_One()
        {
            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":40}]}").State;
            Assert.AreEqual(0, old.RecipeLeaves);
            Assert.AreEqual(0d, old.RestLeafSeconds);
            for (int i = 0; i < defs.Count; i++)
                for (int d = 0; d < Recipes.Dishes; d++)
                    Assert.AreEqual(1, Recipes.DishLevel(old, i, d), "station " + i + " dish " + d);
        }

        [Test]
        public void A_Broken_Recipe_Block_Loads_Within_Bounds()
        {
            const string json = "{\"version\":14,\"stations\":[],\"recipes\":{\"leaves\":-7,\"rest\":99999,\"dishes\":["
                + "{\"id\":\"tea\",\"levels\":[42,0,\"x\"]},{\"id\":\"gone\",\"levels\":[5]},{\"id\":\"jam\",\"levels\":[4,2,3,9]}]}}";
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(0, r.RecipeLeaves);
            Assert.That(r.RestLeafSeconds, Is.InRange(0d, Recipes.RestSecondsPerLeaf));
            Assert.AreEqual(Recipes.MaxLevel, Recipes.DishLevel(r, 0, 0));
            Assert.AreEqual(1, Recipes.DishLevel(r, 0, 1));
            Assert.AreEqual(1, Recipes.DishLevel(r, 0, 2));
            Assert.AreEqual(4, Recipes.DishLevel(r, 2, 0));
            Assert.AreEqual(3, Recipes.DishLevel(r, 2, 2));
            Assert.AreEqual(1, Recipes.DishLevel(r, 1, 0));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class AquariumTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
        }

        void ReleaseMany(int fish, int count)
        {
            s.Fish[fish] += count;
            for (int i = 0; i < count; i++) Assert.IsTrue(Aquarium.Release(s, fish).Released, "fish " + fish + " number " + (i + 1));
        }

        [Test]
        public void The_Numbers_Are_The_Issues_Defaults()
        {
            Assert.AreEqual(new[] { 1, 5, 20, 50 }, Aquarium.Milestones);
            Assert.AreEqual(new[] { 0.0025, 0.0025, 0.005, 0.01 }, Aquarium.MilestoneBonus);
            Assert.AreEqual(0.02, Aquarium.BonusOf(50), 1e-12, "every milestone of one kind");
            Assert.AreEqual(0.12, Aquarium.MaxBonus, 1e-12);
            Assert.AreEqual(6, Aquarium.Kinds);
            Assert.AreEqual(10, Aquarium.SetGolden);
            Assert.AreEqual(1, Aquarium.SetJars);
        }

        [Test]
        public void Releasing_Takes_One_Fish_From_The_Basket_And_Puts_It_In_The_Tank()
        {
            s.Fish[2] = 3;
            s.Fish[0] = 1;
            ReleaseResult r = Aquarium.Release(s, 2);
            Assert.IsTrue(r.Released);
            Assert.AreEqual(2, r.Fish);
            Assert.AreEqual(2, s.Fish[2]);
            Assert.AreEqual(1, s.Fish[0], "only the chosen kind leaves the basket");
            Assert.AreEqual(1, Aquarium.Released(s, 2));
            Assert.AreEqual(0, Aquarium.Released(s, 0));
            Assert.AreEqual(3, Fishing.InBasket(s));
        }

        [Test]
        public void Nothing_Is_Released_From_An_Empty_Basket_Or_For_A_Kind_That_Is_Not_There()
        {
            Assert.IsFalse(Aquarium.CanRelease(s, 0));
            Assert.IsFalse(Aquarium.Release(s, 0).Released);
            Assert.IsFalse(Aquarium.Release(s, -1).Released);
            Assert.IsFalse(Aquarium.Release(s, Aquarium.Kinds).Released);
            Assert.AreEqual(0, Aquarium.Released(s, 0));
            Assert.AreEqual(1d, Aquarium.Multiplier(s));
        }

        [Test]
        public void Released_Fish_Cannot_Be_Taken_Back()
        {
            ReleaseMany(1, 4);
            Assert.AreEqual(0, s.Fish[1]);
            Assert.AreEqual(4, Aquarium.Released(s, 1));
            Assert.IsFalse(Aquarium.CanRelease(s, 1), "the basket is empty, and the tank gives nothing back");
            Assert.AreEqual(0, Fishing.InBasket(s));
        }

        [Test]
        public void Each_Milestone_Adds_Its_Share_Once()
        {
            s.Fish[0] = 60;
            var newAt = new List<int>();
            var added = new List<double>();
            for (int n = 1; n <= 60; n++)
            {
                double before = Aquarium.Bonus(s);
                ReleaseResult r = Aquarium.Release(s, 0);
                Assert.That(r.Milestones, Is.InRange(0, 1));
                Assert.AreEqual(Aquarium.Bonus(s) - before, r.Bonus, 1e-12);
                if (r.Milestones > 0) { newAt.Add(n); added.Add(r.Bonus); }
            }
            Assert.AreEqual(new[] { 1, 5, 20, 50 }, newAt, "a milestone pays on the release that reaches it, and never again");
            Assert.AreEqual(new[] { 0.0025, 0.0025, 0.005, 0.01 }, added.ToArray());
            Assert.AreEqual(4, Aquarium.Reached(s));
            Assert.AreEqual(0.02, Aquarium.Bonus(s), 1e-12);
            Assert.AreEqual(0, Aquarium.NextMilestone(Aquarium.Released(s, 0)), "no milestone after fifty");
        }

        [Test]
        public void The_Next_Milestone_Is_Shown_For_Each_Count()
        {
            Assert.AreEqual(1, Aquarium.NextMilestone(0));
            Assert.AreEqual(5, Aquarium.NextMilestone(1));
            Assert.AreEqual(5, Aquarium.NextMilestone(4));
            Assert.AreEqual(20, Aquarium.NextMilestone(5));
            Assert.AreEqual(50, Aquarium.NextMilestone(49));
            Assert.AreEqual(0, Aquarium.NextMilestone(50));
            Assert.AreEqual(0.0025, Aquarium.NextBonus(4));
            Assert.AreEqual(0.01, Aquarium.NextBonus(49));
            Assert.AreEqual(0d, Aquarium.NextBonus(50));
            Assert.AreEqual(0, Aquarium.MilestonesOf(0));
            Assert.AreEqual(2, Aquarium.MilestonesOf(19));
            Assert.AreEqual(4, Aquarium.MilestonesOf(500));
        }

        [Test]
        public void Every_Milestone_Of_Every_Kind_Makes_Twelve_Percent_And_No_More()
        {
            for (int k = 0; k < Aquarium.Kinds; k++) ReleaseMany(k, 70);
            Assert.AreEqual(24, Aquarium.Reached(s));
            Assert.AreEqual(0.12, Aquarium.Bonus(s), 1e-12);
            Assert.AreEqual(1.12, Aquarium.Multiplier(s), 1e-12);
        }

        [Test]
        public void The_Tank_Multiplies_Modifiers_IncomeMultiplier_After_The_New_Moon_And_The_Rest_Spots()
        {
            s.Season = 2;
            var rabbit = new Rabbit { Id = ++s.NextRabbitId, Tier = 3, Kind = RabbitKinds.DefaultFor(3), Name = "R", Station = -1 };
            s.Rabbits.Add(rabbit);
            double before = Modifiers.From(null, s).IncomeMultiplier;
            Assert.AreEqual(NewMoon.Bonus(s) * RestSpots.Multiplier(s), before, 1e-12);
            ReleaseMany(0, 5);
            ReleaseMany(3, 1);
            Assert.AreEqual(0.0075, Aquarium.Bonus(s), 1e-12);
            Assert.AreEqual(NewMoon.Bonus(s) * RestSpots.Multiplier(s) * Aquarium.Multiplier(s), Modifiers.From(null, s).IncomeMultiplier, 1e-12);
            Assert.AreEqual(Modifiers.GardenMultiplier(s), Modifiers.From(null, s).IncomeMultiplier, 1e-12);
            var ups = new List<UpgradeDef> { new UpgradeDef { Id = "x", Effect = UpgradeEffect.IncomeMultiplier, Amount = 2d, MaxLevel = 5 } };
            s.UpgradeLevels = new[] { 1 };
            Assert.AreEqual(2d * Modifiers.GardenMultiplier(s), Modifiers.From(ups, s).IncomeMultiplier, 1e-12, "the charms multiply on top");
        }

        [Test]
        public void The_Tank_Does_Not_Touch_A_Station_s_Own_Income_Or_Its_Recipe_Dish()
        {
            s.Levels[0] = 30;
            Recipes.SetLevel(s, 0, 0, 4);
            BigNum station = Economy.IncomePerSecond(defs, s);
            ReleaseMany(0, 20);
            Assert.AreEqual(station.ToDouble(), Economy.IncomePerSecond(defs, s).ToDouble(), 1e-9, "the tank is garden-wide, in the modifiers; the dish stays per station");
            Assert.AreEqual(1.01, Modifiers.From(null, s).IncomeMultiplier, 1e-12);
        }

        [Test]
        public void The_First_Fish_Of_A_Kind_Is_New_Until_The_Tank_Is_Seen()
        {
            ReleaseMany(4, 1);
            Assert.IsTrue(Aquarium.IsFresh(s, 4));
            Assert.IsFalse(Aquarium.IsFresh(s, 0));
            s.Fish[4] = 1;
            ReleaseResult again = Aquarium.Release(s, 4);
            Assert.IsFalse(again.FirstOfKind);
            Aquarium.SeeFresh(s);
            Assert.IsFalse(Aquarium.IsFresh(s, 4));
            s.Fish[4] = 1;
            Aquarium.Release(s, 4);
            Assert.IsFalse(Aquarium.IsFresh(s, 4), "only the very first of a kind is new");
            s.Fish[0] = 1;
            Assert.IsTrue(Aquarium.Release(s, 0).FirstOfKind);
            Assert.IsTrue(Aquarium.IsFresh(s, 0));
        }

        [Test]
        public void All_Six_Kinds_The_First_Time_Give_Ten_Golden_Carrots_And_A_Jar_Once()
        {
            for (int k = 0; k < Aquarium.Kinds - 1; k++)
            {
                s.Fish[k] = 1;
                Assert.IsFalse(Aquarium.Release(s, k).FullSet);
            }
            Assert.IsFalse(Aquarium.FullSet(s));
            Assert.AreEqual(5, Aquarium.KindsIn(s));
            s.Fish[5] = 2;
            ReleaseResult r = Aquarium.Release(s, 5);
            Assert.IsTrue(r.FullSet);
            Assert.AreEqual(10, r.Golden);
            Assert.AreEqual(1, r.Jars);
            Assert.AreEqual(0, r.Clovers);
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.AreEqual(1, s.MoonJars);
            Assert.IsTrue(Aquarium.FullSet(s));
            Assert.IsTrue(Aquarium.CardWaiting(s));

            ReleaseResult second = Aquarium.Release(s, 5);
            Assert.IsFalse(second.FullSet, "the gift comes once");
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.AreEqual(1, s.MoonJars);
            Aquarium.SeeCard(s);
            Assert.IsFalse(Aquarium.CardWaiting(s));
        }

        [Test]
        public void With_The_Jar_Shelf_Full_The_Jar_Comes_As_A_Clover_Like_The_Chests()
        {
            s.MoonJars = MoonJars.Cap;
            for (int k = 0; k < Aquarium.Kinds; k++) s.Fish[k] = 1;
            ReleaseResult last = default;
            for (int k = 0; k < Aquarium.Kinds; k++) last = Aquarium.Release(s, k);
            Assert.IsTrue(last.FullSet);
            Assert.AreEqual(0, last.Jars);
            Assert.AreEqual(1, last.Clovers);
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.AreEqual(0, s.Aquarium.SetJarsGiven);
        }

        [Test]
        public void The_Tank_Is_Closed_Until_The_Pond_Opens()
        {
            s.GardenXp = 0;
            s.Fish[0] = 1;
            Assert.AreEqual(Fishing.Unlocked(s), Aquarium.CanRelease(s, 0));
        }

        [Test]
        public void The_Tank_Survives_A_Save_And_A_New_Moon()
        {
            ReleaseMany(0, 22);
            ReleaseMany(1, 5);
            ReleaseMany(5, 1);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(22, Aquarium.Released(back, 0));
            Assert.AreEqual(5, Aquarium.Released(back, 1));
            Assert.AreEqual(1, Aquarium.Released(back, 5));
            Assert.IsTrue(Aquarium.IsFresh(back, 5));
            Assert.AreEqual(Aquarium.Bonus(s), Aquarium.Bonus(back), 1e-12);
            Assert.AreEqual(SaveCodec.Encode(defs, s), SaveCodec.Encode(defs, back));
            Assert.AreEqual(14, SaveCodec.CurrentVersion);

            for (int k = 2; k < 5; k++) ReleaseMany(k, 1);
            Assert.IsTrue(Aquarium.CardWaiting(s));
            back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.IsTrue(back.Aquarium.SetGiven);
            Assert.AreEqual(1, back.Aquarium.SetJarsGiven, "the card still says a jar, not a clover");
            Assert.IsTrue(Aquarium.CardWaiting(back));
        }

        [Test]
        public void An_Old_Save_Starts_With_An_Empty_Tank()
        {
            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":40}],\"fishing\":{\"basket\":[3,2,1,0,0,0]}}").State;
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                Assert.AreEqual(0, Aquarium.Released(old, k));
                Assert.IsFalse(Aquarium.IsFresh(old, k));
            }
            Assert.AreEqual(3, old.Fish[0], "the basket stays as it was");
            Assert.AreEqual(1d, Aquarium.Multiplier(old));
            Assert.IsFalse(old.Aquarium.SetGiven);
            Assert.IsFalse(Aquarium.CardWaiting(old));
        }

        [Test]
        public void A_Broken_Tank_Block_Loads_Within_Bounds()
        {
            const string json = "{\"version\":14,\"stations\":[],\"aquarium\":{\"released\":[-4,7,\"x\",3,1e12,2,9,9],\"fresh\":-1,\"set\":\"yes\",\"cardSeen\":false}}";
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(0, Aquarium.Released(r, 0));
            Assert.AreEqual(7, Aquarium.Released(r, 1));
            Assert.AreEqual(0, Aquarium.Released(r, 2));
            Assert.AreEqual(3, Aquarium.Released(r, 3));
            Assert.AreEqual(int.MaxValue, Aquarium.Released(r, 4));
            Assert.AreEqual(2, Aquarium.Released(r, 5));
            Assert.AreEqual(Aquarium.Kinds, r.Aquarium.Released.Length);
            Assert.AreEqual((1 << Aquarium.Kinds) - 1, r.Aquarium.Fresh);
            Assert.IsFalse(r.Aquarium.SetGiven);
            Assert.IsFalse(Aquarium.CardWaiting(r));
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class RestSpotsTests
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

        Rabbit Add(int tier, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Name = "R" + s.NextRabbitId, Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        [Test]
        public void A_New_Garden_Has_Two_Places_To_Rest()
        {
            Assert.AreEqual(2, RestSpots.Starting);
            Assert.AreEqual(RestSpots.Starting, s.RestSpots);
            Assert.GreaterOrEqual(RestSpots.Max, 4);
        }

        [Test]
        public void Only_Resting_Rabbits_Take_A_Place_Best_First_And_No_More_Than_There_Are_Places()
        {
            Rabbit worker = Add(5, 0);
            Rabbit silver = Add(0), gold = Add(4), green = Add(1);
            List<Rabbit> seated = RestSpots.Seated(s);
            Assert.AreEqual(2, seated.Count);
            Assert.AreSame(gold, seated[0]);
            Assert.AreSame(green, seated[1]);
            CollectionAssert.DoesNotContain(seated, worker);
            CollectionAssert.DoesNotContain(seated, silver);

            s.RestSpots = 3;
            Assert.AreEqual(3, RestSpots.Seated(s).Count);
            Assert.AreSame(silver, RestSpots.Seated(s)[2]);
        }

        [Test]
        public void Nobody_Resting_Means_Nobody_Seated()
        {
            Add(2, 0);
            Assert.IsEmpty(RestSpots.Seated(s));
        }

        [Test]
        public void Places_To_Rest_Leave_Each_Stations_Own_Income_Alone()
        {
            Add(3, 0); Add(1);
            double before = Economy.IncomePerSecond(defs, s).ToDouble();
            s.RestSpots = RestSpots.Max;
            Assert.AreEqual(before, Economy.IncomePerSecond(defs, s).ToDouble(), 1e-12);
        }

        [Test]
        public void More_Places_Than_The_Garden_Can_Hold_Are_Ignored()
        {
            for (int i = 0; i < 10; i++) Add(0);
            s.RestSpots = 50;
            Assert.AreEqual(RestSpots.Max, RestSpots.Seated(s).Count);
            s.RestSpots = -3;
            Assert.IsEmpty(RestSpots.Seated(s));
        }

        [Test]
        public void Places_Round_Trip_And_Old_Saves_Start_With_Two()
        {
            s.RestSpots = 4;
            Assert.AreEqual(4, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.RestSpots);
            Assert.AreEqual(2, SaveCodec.Decode(defs, "{\"version\":6,\"stations\":[]}").State.RestSpots);
            Assert.AreEqual(RestSpots.Max, SaveCodec.Decode(defs, "{\"version\":7,\"stations\":[],\"restSpots\":999}").State.RestSpots);
            Assert.AreEqual(2, SaveCodec.Decode(defs, "{\"version\":7,\"stations\":[],\"restSpots\":-5}").State.RestSpots);
        }

        // ----- resting rabbits lend a hand (#60) -----

        [Test]
        public void Nobody_Resting_Leaves_The_Garden_As_It_Is()
        {
            Add(5, 0); Add(2, 1);
            Assert.AreEqual(0d, RestSpots.Bonus(s), 1e-12);
            Assert.AreEqual(1d, RestSpots.Multiplier(s), 1e-12);
            Assert.AreEqual(1d, Modifiers.From(null, s).IncomeMultiplier, 1e-12);
        }

        [Test]
        public void Six_Silver_Rabbits_Resting_Make_The_Garden_Earn_Twelve_Percent_More()
        {
            s.RestSpots = RestSpots.Max;
            for (int i = 0; i < 6; i++) Add(0);
            Assert.AreEqual(1.12, RestSpots.Multiplier(s), 1e-12);
            Assert.AreEqual(1.12, Modifiers.From(null, s).IncomeMultiplier, 1e-12);
        }

        [Test]
        public void A_Red_Rabbit_Resting_Adds_Ten_Percent_And_Each_Tier_Its_Own_Share()
        {
            double[] expected = { 0.02, 0.025, 0.03, 0.04, 0.06, 0.10 };
            for (int tier = 0; tier < Rabbits.TierCount; tier++)
                Assert.AreEqual(expected[tier], RestSpots.BonusOf(new Rabbit { Tier = tier }), 1e-12, "tier " + tier);
            Add(5);
            Assert.AreEqual(1.10, RestSpots.Multiplier(s), 1e-12);
            // Above silver the share is the tier's work multiplier over fifty.
            for (int tier = 1; tier < Rabbits.TierCount; tier++)
                Assert.AreEqual(Rabbits.Multipliers[tier] * RestSpots.RestBonus, RestSpots.BonusOf(new Rabbit { Tier = tier }), 1e-12);
        }

        [Test]
        public void Rabbits_At_Stations_And_Rabbits_Without_A_Seat_Add_Nothing()
        {
            Add(5, 0); Add(4, 1);
            Rabbit gold = Add(4), blue = Add(2), silver = Add(0);
            Assert.AreEqual(2, s.RestSpots);
            Assert.AreEqual(0.06 + 0.03, RestSpots.Bonus(s), 1e-12, "only the two seated: gold and blue");
            Assert.IsTrue(RestSpots.IsSeated(s, gold));
            Assert.IsTrue(RestSpots.IsSeated(s, blue));
            Assert.IsFalse(RestSpots.IsSeated(s, silver), "no third place");
            Assert.IsFalse(RestSpots.IsSeated(s, s.Rabbits[0]), "a worker is not resting");
        }

        [Test]
        public void The_Resting_Share_Multiplies_With_Charms_And_The_New_Moon()
        {
            var ups = new List<UpgradeDef> { new UpgradeDef { Id = "moon", Effect = UpgradeEffect.IncomeMultiplier, Amount = 2, MaxLevel = 5 } };
            s.UpgradeLevels = new[] { 1 };
            Add(5);
            Assert.AreEqual(2d * 1.10, Modifiers.From(ups, s).IncomeMultiplier, 1e-12);
        }

        [Test]
        public void An_Old_Save_With_Resting_Rabbits_Reads_And_Pays_Their_Share()
        {
            // Version 7 came before the resting share: nothing new is saved, the share follows from who rests.
            const string old = "{\"version\":7,\"stations\":[{\"id\":\"tea\",\"level\":3}],\"restSpots\":3,"
                + "\"rabbits\":[{\"id\":1,\"tier\":1},{\"id\":2,\"tier\":0,\"station\":\"tea\"}]}";
            LoadResult loaded = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            Assert.AreEqual(2, loaded.State.Rabbits.Count);
            Assert.AreEqual(0, loaded.State.Rabbits[1].Station, "the silver one is at work");
            Assert.AreEqual(1.025, Modifiers.From(null, loaded.State).IncomeMultiplier, 1e-12);
        }

        [Test]
        public void Six_Rabbits_Resting_Together_Is_An_Achievement_Of_Two_Clovers()
        {
            var six = new QuestDef { Id = "nghi-6", Metric = QuestMetric.RabbitsResting, Target = 6, RewardClovers = 2 };
            for (int i = 0; i < 6; i++) Add(0);
            Add(1, 0);
            Assert.AreEqual(2d, Quests.Progress(six, defs, s), "only the seated count: two places");
            Assert.IsFalse(Quests.CanClaim(six, defs, s));
            s.RestSpots = RestSpots.Max;
            Assert.AreEqual(6d, Quests.Progress(six, defs, s));
            Assert.IsTrue(Quests.CanClaim(six, defs, s));
            int clovers = s.Clovers;
            Assert.IsTrue(Quests.TryClaim(six, defs, s, Modifiers.None, out _));
            Assert.AreEqual(clovers + 2, s.Clovers);
            Assert.Greater((int)QuestMetric.RabbitsResting, (int)QuestMetric.PhotoAsksAnswered, "new metrics go at the end: assets store the number");
        }
    }
}

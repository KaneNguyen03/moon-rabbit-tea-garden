using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class GardenLevelTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = new List<StationDef>();
            for (int i = 0; i < 12; i++)
                defs.Add(new StationDef { Id = "s" + i, Name = "S" + i, Kind = StationKind.Brew, BaseCost = 10 * System.Math.Pow(10, i), CostGrowth = 1.1, BaseIncome = 1, CycleSeconds = 1 });
            s = GameState.NewGame(defs);
        }

        [TestCase(0, 1)]
        [TestCase(113, 1)]
        [TestCase(114, 2)]   // 100 + 14 x 1
        [TestCase(241, 2)]
        [TestCase(242, 3)]   // + 100 + 14 x 2
        [TestCase(540, 5)]
        [TestCase(1530, 10)]
        [TestCase(2870, 15)]
        [TestCase(-5, 1)]
        public void Level_Follows_Experience(double xp, int level)
        {
            Assert.AreEqual(level, Garden.LevelOf(xp));
        }

        [Test]
        public void Each_Level_Takes_More_Than_The_Last()
        {
            for (int level = 1; level < 40; level++)
            {
                Assert.AreEqual(Garden.XpForLevel(level) + Garden.XpToNext(level), Garden.XpForLevel(level + 1), 1e-9);
                Assert.AreEqual(level, Garden.LevelOf(Garden.XpForLevel(level)));
                Assert.AreEqual(level, Garden.LevelOf(Garden.XpForLevel(level + 1) - 0.001));
                if (level > 1) Assert.Greater(Garden.XpToNext(level), Garden.XpToNext(level - 1));
            }
            Assert.AreEqual(Garden.MaxLevel, Garden.LevelOf(1e12));
        }

        [Test]
        public void A_Level_Gained_Brings_A_Clover()
        {
            Assert.AreEqual(0, Garden.AddXp(s, 113));
            Assert.AreEqual(0, s.Clovers);
            Assert.AreEqual(1, Garden.AddXp(s, 1));
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(2, Garden.Level(s));
            Assert.AreEqual(2, Garden.AddXp(s, 300), "two levels in one go bring two clovers"); // 414: level 4 begins at 384
            Assert.AreEqual(3, s.Clovers);
            Assert.AreEqual(0, Garden.AddXp(s, 0));
            Assert.AreEqual(0, Garden.AddXp(s, -50));
            Assert.AreEqual(0, Garden.AddXp(s, double.NaN));
            Assert.AreEqual(414, s.GardenXp, 1e-9);
        }

        [Test]
        public void Progress_Runs_From_Nought_To_One_Within_A_Level()
        {
            Assert.AreEqual(0, Garden.Progress(s), 1e-9);
            s.GardenXp = 178; // level 2 runs from 114 to 242
            Assert.AreEqual(0.5, Garden.Progress(s), 1e-9);
        }

        [Test]
        public void Growing_A_Station_Earns_Experience()
        {
            s.Coins = new BigNum(1e9);
            Assert.IsTrue(Simulation.TryBuy(defs, s, 0)); // level 1 → 2
            Assert.AreEqual(Garden.XpPerLevelBought, s.GardenXp, 1e-9);
            s.BuyMode = 10;
            Assert.IsTrue(Simulation.TryBuy(defs, s, 0)); // 2 → 12, past the milestone at 10
            Assert.AreEqual(1 + 10 + Garden.XpMilestone, s.GardenXp, 1e-9);
            Assert.IsTrue(Simulation.TryBuy(defs, s, 1)); // opening the second station
            Assert.AreEqual(1 + 10 + Garden.XpMilestone + Garden.XpStationOpened, s.GardenXp, 1e-9);
        }

        [Test]
        public void Rabbits_And_Quests_Earn_Experience()
        {
            s.Clovers = 2;
            Rabbits.Summon(s, 0.1, "A");
            Assert.AreEqual(Garden.XpSummon + Garden.XpNewKind, s.GardenXp, 1e-9);
            double before = s.GardenXp;
            Rabbits.Summon(s, 0.1, "B"); // the same kind again
            Assert.AreEqual(before + Garden.XpSummon, s.GardenXp, 1e-9);

            var quest = new QuestDef { Id = "q", Metric = QuestMetric.StationsOpen, Target = 1, RewardCarrots = 5 };
            before = s.GardenXp;
            Assert.IsTrue(Quests.TryClaim(quest, defs, s, Modifiers.None, out _));
            Assert.AreEqual(before + Garden.XpQuest, s.GardenXp, 1e-9);
        }

        // ----- expansion -----

        void Rabbit(int n)
        {
            for (int i = 0; i < n; i++) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" });
        }

        [Test]
        public void The_First_Ring_Needs_Level_And_Carrots_Both()
        {
            ForestRing ring = Expansion.Next(defs, s);
            Assert.AreEqual(4, ring.RadiusAfter);
            Assert.AreEqual(0, ring.NeedRabbits, "the first ring asks for no rabbits (#53)");
            BigNum cost = Expansion.CostOfNext(defs, s);
            Assert.AreEqual((1e5 + 1e6) * Plots.CostShare, cost.ToDouble(), 1e-3, "the land of the two stations it makes room for, each a quarter of its station");

            s.Coins = cost;
            s.GardenXp = Garden.XpForLevel(3) - 1;
            Assert.IsFalse(Expansion.Expand(defs, s), "one level short");
            s.GardenXp = Garden.XpForLevel(3);
            s.Coins = cost - BigNum.One;
            Assert.IsFalse(Expansion.Expand(defs, s), "one carrot short");
            Assert.AreEqual(4, s.Plots);
            Assert.AreEqual(3, s.Grid.Radius);

            s.Coins = cost;
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(6, s.Plots);
            Assert.AreEqual(4, s.Grid.Radius, "the forest stepped back a ring");
            Assert.AreEqual(3, s.RestSpots, "a ring also opens a place to rest");
            Assert.IsTrue(s.Coins.Equals(BigNum.Zero));
        }

        [Test]
        public void The_Second_Ring_Needs_Rabbits_Too()
        {
            s.Coins = BigNum.FromParts(1, 40);
            s.GardenXp = Garden.XpForLevel(6);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Rabbit(5);
            Assert.IsFalse(Expansion.Expand(defs, s), "one rabbit short");
            Rabbit(1);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(5, s.Grid.Radius);
        }

        [Test]
        public void The_Rings_Go_Four_Five_Six_Seven_And_Then_Stop()
        {
            s.Coins = BigNum.FromParts(1, 40);
            s.GardenXp = Garden.XpForLevel(20);
            Rabbit(12);
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < 8) s.SeenKinds.Add(kind.Id);
            int[] expected = { 6, 8, 10, 12 };
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.IsTrue(Expansion.Expand(defs, s), "ring " + i);
                Assert.AreEqual(expected[i], s.Plots);
                Assert.AreEqual(4 + i, s.Grid.Radius);
                Assert.AreEqual(3 + i, s.RestSpots);
            }
            Assert.IsNull(Expansion.Next(defs, s), "twelve stations, twelve plots: the forest stays where it is");
            Assert.IsFalse(Expansion.Expand(defs, s));
            Assert.IsTrue(Expansion.CostOfNext(defs, s).Equals(BigNum.Zero));
        }

        [Test]
        public void The_Last_Ring_Asks_For_Kinds_Met_Not_For_A_Head_Count()
        {
            s.Plots = 10;
            GardenGrid.Settle(defs, s);
            s.Coins = BigNum.FromParts(1, 40);
            s.GardenXp = Garden.XpForLevel(15);
            Assert.AreEqual(7, Expansion.Next(defs, s).RadiusAfter);
            Assert.IsFalse(Expansion.CanExpand(defs, s));
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < 8) s.SeenKinds.Add(kind.Id);
            Assert.IsTrue(Expansion.CanExpand(defs, s), "no rabbits owned right now, but eight kinds met");
        }

        [TestCase(4, 0, 1)]
        [TestCase(5, 1, 3)]
        [TestCase(6, 1, 3)]
        [TestCase(7, 2, 6)]
        [TestCase(10, 3, 10)]
        [TestCase(12, 4, 15)]
        public void Land_Already_Held_Says_Which_Waves_Were_Taken(int plots, int waves, int level)
        {
            Assert.AreEqual(waves, Expansion.WavesTaken(plots));
            Assert.AreEqual(level, Expansion.LevelForPlots(defs, plots));
        }

        // ----- saves -----

        [Test]
        public void Experience_Round_Trips()
        {
            s.GardenXp = 431.5;
            Assert.AreEqual(431.5, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.GardenXp, 1e-9);
        }

        [Test]
        public void An_Old_Save_Keeps_Its_Land_And_Is_Given_The_Level_That_Land_Needs()
        {
            // Seven plots bought one at a time under the old rule, and almost nothing else done.
            const string v7 = "{\"version\":7,\"stations\":[{\"id\":\"s0\",\"level\":1,\"progress\":0}],\"plots\":7}";
            GameState r = SaveCodec.Decode(defs, v7).State;
            Assert.AreEqual(7, r.Plots, "no plot is ever taken back");
            Assert.AreEqual(6, Garden.Level(r), "seven plots means the second wave: level 6");
            Assert.AreEqual(0, r.Clovers, "the catch-up hands out no clovers");
        }

        [TestCase(4, 2, 2)]
        [TestCase(6, 2, 3)]
        [TestCase(7, 2, 4)]
        [TestCase(12, 2, 6)]
        [TestCase(6, 5, 5)]   // more places than its land explains: it keeps them
        public void An_Old_Save_Has_At_Least_The_Places_To_Rest_Its_Land_Brings(int plots, int saved, int expected)
        {
            s.Plots = plots;
            s.RestSpots = saved;
            Assert.AreEqual(expected, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.RestSpots);
        }

        [Test]
        public void An_Old_Save_With_Real_Progress_Gets_Credit_For_It()
        {
            s.Levels = new[] { 30, 12, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            s.LevelsBought = 40;
            s.Claimed.Add("a"); s.Claimed.Add("b");
            Rabbit(2);
            s.SeenKinds.Add("tho-bong");
            // 40 levels + two more stations opened (20) + milestones 10 and 25 on the first, 10 on the second (45)
            // + 2 rabbits (10) + 1 kind (20) + 2 quests (20)
            Assert.AreEqual(40 + 20 + 45 + 10 + 20 + 20, Garden.BackfillXp(defs, s), 1e-9);
        }
    }
}

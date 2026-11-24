using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class AchievementTiersTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        static QuestTier Tier(double target, int golden, int clovers = 0, int jars = 0, bool merged = false) =>
            new QuestTier { Target = target, RewardSeconds = 10, RewardCarrots = 50, RewardGolden = golden, RewardClovers = clovers, RewardJars = jars, Merged = merged };

        static QuestDef Fish() => new QuestDef
        {
            Id = "ca", Metric = QuestMetric.FishCaught, Merged = new[] { "ca-50" },
            Tiers = new[] { Tier(5, 2), Tier(15, 3), Tier(50, 4, merged: true), Tier(150, 5) }
        };

        static QuestDef Taps() => new QuestDef
        {
            Id = "cham", Metric = QuestMetric.Taps, Merged = new[] { "cham-100", "cham-1000" },
            Tiers = new[] { Tier(100, 2, merged: true), Tier(300, 3), Tier(1000, 4, merged: true), Tier(3000, 5) }
        };

        static QuestDef Games() => new QuestDef
        {
            Id = "van", Metric = QuestMetric.MiniGames, Merged = new[] { "gia-30", "la-30", "dom-30" },
            Tiers = new[] { Tier(5, 0), Tier(30, 0, 3, merged: true), Tier(60, 0, 3, merged: true), Tier(90, 0, 3, merged: true), Tier(150, 2) }
        };

        Rabbit Add(int affection = 0, int tier = 0)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Kind = RabbitKinds.DefaultFor(tier), Name = "R", Affection = affection };
            s.Rabbits.Add(r);
            return r;
        }

        [Test]
        public void Tiers_Open_In_Order_And_Each_Is_Paid_Once()
        {
            QuestDef fish = Fish();
            s.FishCaught = 20;
            Assert.AreEqual(0, Quests.TiersClaimed(fish, s));
            Assert.AreEqual(5d, Quests.TargetOf(fish, s));
            Assert.IsTrue(Quests.CanClaim(fish, defs, s));

            Assert.IsTrue(Quests.TryClaim(fish, defs, s, Modifiers.None, out _));
            Assert.AreEqual(2, s.GoldenCarrots, "tier one pays two");
            Assert.AreEqual(1, Quests.TiersClaimed(fish, s));
            Assert.AreEqual(15d, Quests.TargetOf(fish, s), "the row moves on to the next tier");
            Assert.IsTrue(Quests.CanClaim(fish, defs, s));

            Assert.IsTrue(Quests.TryClaim(fish, defs, s, Modifiers.None, out _));
            Assert.AreEqual(5, s.GoldenCarrots, "tier two pays three");
            Assert.AreEqual(50d, Quests.TargetOf(fish, s));
            Assert.IsFalse(Quests.CanClaim(fish, defs, s), "twenty fish do not reach tier three");
            Assert.IsFalse(Quests.TryClaim(fish, defs, s, Modifiers.None, out _));
            Assert.AreEqual(5, s.GoldenCarrots);
            Assert.IsFalse(Quests.IsClaimed(fish, s), "the chain is not finished");
            CollectionAssert.AreEquivalent(new[] { "ca.1", "ca.2" }, s.Claimed);
        }

        [Test]
        public void A_Tier_Claimed_Out_Of_Order_Does_Not_Skip_The_One_Before()
        {
            QuestDef fish = Fish();
            s.FishCaught = 60;
            s.Claimed.Add("ca.2");
            Assert.AreEqual(0, Quests.TiersClaimed(fish, s), "tier one still waits");
            Assert.AreEqual(5d, Quests.TargetOf(fish, s));
            Assert.IsTrue(Quests.TryClaim(fish, defs, s, Modifiers.None, out _));
            Assert.AreEqual(2, Quests.TiersClaimed(fish, s), "tier two was already taken");
            Assert.AreEqual(50d, Quests.TargetOf(fish, s));
            Assert.AreEqual(2, s.GoldenCarrots, "only tier one was paid");
        }

        [Test]
        public void The_Last_Tier_Leaves_The_Chain_Finished()
        {
            QuestDef fish = Fish();
            s.FishCaught = 1000;
            int claims = 0;
            while (Quests.TryClaim(fish, defs, s, Modifiers.None, out _)) claims++;
            Assert.AreEqual(4, claims);
            Assert.AreEqual(2 + 3 + 4 + 5, s.GoldenCarrots);
            Assert.IsTrue(Quests.IsClaimed(fish, s));
            Assert.IsTrue(Quests.IsComplete(fish, defs, s));
            Assert.IsFalse(Quests.CanClaim(fish, defs, s));
            Assert.AreEqual(150d, Quests.TargetOf(fish, s), "a finished chain shows its last tier");
            Assert.AreEqual(4, Quests.TiersClaimed(fish, s));
        }

        [Test]
        public void An_Old_Save_Keeps_What_It_Claimed_And_Is_Never_Paid_Twice()
        {
            s.Claimed.Add("ca-50");
            s.Claimed.Add("cham-100");
            s.Claimed.Add("cham-1000");
            s.FishCaught = 60;
            s.TotalTaps = 1200;
            GameState old = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            QuestDef fish = Fish(), taps = Taps();

            Assert.AreEqual(0, Quests.TiersClaimed(fish, old), "the fifty-fish achievement is the tier of fifty; the tiers below it are new");
            Assert.AreEqual(1, Quests.TiersClaimed(taps, old), "the hundred taps are tier one");
            Assert.AreEqual(5d, Quests.TargetOf(fish, old));
            Assert.AreEqual(300d, Quests.TargetOf(taps, old), "the new tier between the two old ones");

            int golden = old.GoldenCarrots;
            Assert.IsTrue(Quests.TryClaim(fish, defs, old, Modifiers.None, out _));
            Assert.IsTrue(Quests.TryClaim(fish, defs, old, Modifiers.None, out _));
            Assert.AreEqual(3, Quests.TiersClaimed(fish, old), "tier two leads straight past the fifty already paid");
            Assert.AreEqual(150d, Quests.TargetOf(fish, old));
            Assert.IsFalse(Quests.TryClaim(fish, defs, old, Modifiers.None, out _), "sixty fish stop below tier four");
            Assert.AreEqual(golden + 2 + 3, old.GoldenCarrots, "tiers one and two only; the tier of fifty was the old achievement");
            Assert.IsTrue(Quests.TryClaim(taps, defs, old, Modifiers.None, out _));
            Assert.AreEqual(golden + 2 + 3 + 3, old.GoldenCarrots, "the new tier of three hundred taps");
            Assert.AreEqual(3, Quests.TiersClaimed(taps, old), "the thousand taps were paid before");
            Assert.IsFalse(Quests.CanClaim(taps, defs, old));

            GameState again = SaveCodec.Decode(defs, SaveCodec.Encode(defs, old)).State;
            Assert.AreEqual(3, Quests.TiersClaimed(fish, again));
            Assert.AreEqual(3, Quests.TiersClaimed(taps, again));
            Assert.IsFalse(Quests.CanClaim(fish, defs, again));
            Assert.IsFalse(Quests.CanClaim(taps, defs, again));
        }

        [Test]
        public void Old_Achievements_Of_One_Chain_Fill_Its_Old_Tiers_In_Order_Whichever_Were_Claimed()
        {
            QuestDef games = Games();
            s.Claimed.Add("la-30");
            s.Leaves.Games = 30;
            s.PoundingGames = 12;
            Assert.AreEqual(0, Quests.TiersClaimed(games, s));
            int clovers = s.Clovers;
            Assert.IsTrue(Quests.TryClaim(games, defs, s, Modifiers.None, out _));
            Assert.AreEqual(2, Quests.TiersClaimed(games, s), "one old game achievement stands for the first old tier, thirty rounds");
            Assert.AreEqual(60d, Quests.TargetOf(games, s));
            Assert.IsFalse(Quests.CanClaim(games, defs, s));
            Assert.AreEqual(clovers, s.Clovers, "the thirty-round tier is not paid again");
            s.Claimed.Add("gia-30");
            s.Claimed.Add("dom-30");
            Assert.AreEqual(4, Quests.TiersClaimed(games, s));
            Assert.AreEqual(150d, Quests.TargetOf(games, s));
        }

        [Test]
        public void An_Old_Achievement_Not_Yet_Claimed_Merges_Into_Nothing()
        {
            s.FishCaught = 60;
            QuestDef fish = Fish();
            Assert.AreEqual(0, Quests.TiersClaimed(fish, s));
            int claims = 0;
            while (Quests.TryClaim(fish, defs, s, Modifiers.None, out _)) claims++;
            Assert.AreEqual(3, claims, "a new garden climbs from tier one");
        }

        [Test]
        public void A_Tier_Pays_The_Days_Free_Moon_Jar_Or_A_Clover_When_It_Has_Come()
        {
            var jar = new QuestDef { Id = "anh", Metric = QuestMetric.PhotosTaken, Tiers = new[] { Tier(1, 0, 3, 1), Tier(2, 0, 3, 1), Tier(3, 0, 3, 1) } };
            s.PhotosTaken = 5;
            s.DailyDay = 40;
            Assert.AreEqual(1, Quests.JarsNow(jar, s));
            Assert.AreEqual(3, Quests.CloversNow(jar, s));
            Assert.IsTrue(Quests.TryClaim(jar, defs, s, Modifiers.None, out _));
            Assert.AreEqual(1, s.MoonJars);
            Assert.AreEqual(3, s.Clovers);
            Assert.AreEqual(40, s.MoonJarFreeDay, "the tier's jar is the day's free jar");

            Assert.AreEqual(0, Quests.JarsNow(jar, s), "one free jar a day");
            Assert.AreEqual(4, Quests.CloversNow(jar, s), "the jar comes as a clover");
            Assert.IsTrue(Quests.TryClaim(jar, defs, s, Modifiers.None, out _));
            Assert.AreEqual(1, s.MoonJars);
            Assert.AreEqual(7, s.Clovers);

            s.DailyDay = 41;
            s.MoonJars = MoonJars.Cap;
            Assert.AreEqual(0, Quests.JarsNow(jar, s), "a full shelf takes no jar");
            Assert.IsTrue(Quests.TryClaim(jar, defs, s, Modifiers.None, out _));
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            Assert.AreEqual(11, s.Clovers);
        }

        [Test]
        public void A_Tier_Pays_Its_Own_Carrots_And_Golden_Carrots()
        {
            QuestDef fish = Fish();
            s.FishCaught = 20;
            Assert.AreEqual(50d, Quests.RewardOf(fish, defs, s, Modifiers.None).ToDouble(), 1e-9);
            Assert.AreEqual(2, Quests.GoldenNow(fish, s));
            Quests.TryClaim(fish, defs, s, Modifiers.None, out BigNum paid);
            Assert.AreEqual(50d, paid.ToDouble(), 1e-9);
            Assert.AreEqual(3, Quests.GoldenNow(fish, s));
            var once = new QuestDef { Id = "dat-12", Metric = QuestMetric.PlotsOwned, Target = 12, RewardGolden = 3 };
            Assert.AreEqual(3, Quests.GoldenNow(once, s), "a one-time achievement pays what it always did");
            Assert.AreEqual(1, Quests.TiersOf(once));
            Assert.AreEqual(4, Quests.TiersOf(fish));
        }

        [Test]
        public void The_Total_Counts_Every_Claimed_Tier_Of_Every_Chain()
        {
            var quests = new List<QuestDef> { Fish(), Taps(), new QuestDef { Id = "dat-12", Metric = QuestMetric.PlotsOwned, Target = 12 }, new QuestDef { Id = "ngay-cham", Metric = QuestMetric.Taps, Target = 50, Daily = true } };
            s.Claimed.Add("cham-100");
            s.Claimed.Add("ca.1");
            s.Claimed.Add("ca.2");
            s.Claimed.Add("dat-12");
            s.Claimed.Add("ngay-cham");
            Assert.AreEqual(3, Quests.TiersDone(quests, s), "chain tiers only: one merged tap tier and two fish tiers");
        }

        [Test]
        public void The_New_Counts_Are_Read_Back_From_The_Save()
        {
            s.PoundingGames = 7;
            s.Leaves.Games = 4;
            s.Fireflies.Games = 2;
            Add(Rabbits.AffectionMax);
            Add(Rabbits.AffectionMax);
            Add(Rabbits.PointsFor(9));
            s.GardenXp = Garden.XpForLevel(41);
            s.Season = 2;

            var games = new QuestDef { Id = "van", Metric = QuestMetric.MiniGames };
            var hearts = new QuestDef { Id = "tim", Metric = QuestMetric.RabbitsFullHearts };
            var level = new QuestDef { Id = "cap", Metric = QuestMetric.GardenLevelReached };
            var moons = new QuestDef { Id = "mua", Metric = QuestMetric.NewMoons };

            GameState loaded = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            foreach (GameState state in new[] { s, loaded })
            {
                Assert.AreEqual(13d, Quests.Progress(games, defs, state));
                Assert.AreEqual(2d, Quests.Progress(hearts, defs, state));
                Assert.AreEqual(41d, Quests.Progress(level, defs, state));
                Assert.AreEqual(2d, Quests.Progress(moons, defs, state));
            }
        }

        [Test]
        public void The_Garden_Level_Tier_Remembers_The_Level_Passed_Before_A_New_Moon()
        {
            var level = new QuestDef { Id = "cap", Metric = QuestMetric.GardenLevelReached };
            s.Season = 1;
            s.GardenXp = Garden.XpForLevel(4);
            Assert.AreEqual((double)NewMoon.LevelPassed(s), Quests.Progress(level, defs, s), "a new moon does not take the climb back");
            s.GardenXp = Garden.XpForLevel(40);
            Assert.AreEqual(40d, Quests.Progress(level, defs, s));
        }
    }
}

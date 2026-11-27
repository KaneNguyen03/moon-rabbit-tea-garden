using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class LeavesTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
            s.GardenXp = Garden.XpForLevel(Leaves.UnlockLevel);
        }

        static Leaf At(double lands, double x, LeafKind kind) => new Leaf { Lands = lands, Drop = lands - 2d, X = x, Kind = kind };

        static LeavesRun Play(long seed, int wanted = int.MaxValue)
        {
            var run = LeavesRun.FromSeed(seed);
            foreach (Leaf leaf in run.Schedule)
                run.Advance(leaf.Lands, leaf.Kind != LeafKind.Cone && run.Score < wanted ? leaf.X : (leaf.X > 0.5 ? leaf.X - 0.4 : leaf.X + 0.4));
            return run;
        }

        [Test]
        public void The_Schedule_Is_The_Same_For_A_Seed_And_Another_For_Another_Seed()
        {
            IReadOnlyList<Leaf> a = Leaves.ScheduleFor(7), again = Leaves.ScheduleFor(7), b = Leaves.ScheduleFor(8);
            Assert.AreEqual(Leaves.Drops, a.Count);
            Assert.AreEqual(a.Count, again.Count);
            bool differs = false;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].X, again[i].X);
                Assert.AreEqual(a[i].Drop, again[i].Drop);
                Assert.AreEqual(a[i].Lands, again[i].Lands);
                Assert.AreEqual(a[i].Kind, again[i].Kind);
                Assert.AreEqual(i, a[i].Index);
                if (a[i].X != b[i].X || a[i].Kind != b[i].Kind) differs = true;
            }
            Assert.IsTrue(differs, "another seed is another game");
        }

        [Test]
        public void Every_Schedule_Has_The_Same_Counts_And_Fits_In_Thirty_Seconds()
        {
            for (long seed = 0; seed < 40; seed++)
            {
                IReadOnlyList<Leaf> list = Leaves.ScheduleFor(seed);
                int plain = 0, golden = 0, cones = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    Leaf leaf = list[i];
                    Assert.That(leaf.X, Is.InRange(0d, 1d));
                    Assert.GreaterOrEqual(leaf.Drop, Leaves.Lead);
                    Assert.That(leaf.Lands - leaf.Drop, Is.InRange(Leaves.MinFall, Leaves.MaxFall), "a fall takes two to three seconds");
                    Assert.LessOrEqual(leaf.Lands, Leaves.Seconds, "everything lands before the clock runs out");
                    if (i > 0) Assert.Greater(leaf.Drop, list[i - 1].Drop, "drops come one after another");
                    if (i > 0) Assert.GreaterOrEqual(leaf.Lands, list[i - 1].Lands, "and land in the same order");
                    switch (leaf.Kind)
                    {
                        case LeafKind.Leaf: plain++; break;
                        case LeafKind.Golden: golden++; break;
                        default: cones++; break;
                    }
                }
                Assert.AreEqual(Leaves.LeafCount, plain, "seed " + seed);
                Assert.AreEqual(Leaves.GoldenCount, golden, "seed " + seed);
                Assert.AreEqual(Leaves.ConeCount, cones, "seed " + seed);
                Assert.AreNotEqual(LeafKind.Cone, list[0].Kind, "the first drop is never a pine cone");
            }
            Assert.AreEqual(Leaves.LeafCount * Leaves.LeafPoints + Leaves.GoldenCount * Leaves.GoldenPoints, Leaves.MaxScore);
        }

        [Test]
        public void The_Seed_Comes_From_The_Day_And_The_Games_Played_So_Far()
        {
            Assert.AreEqual(Leaves.SeedFor(100, 0), Leaves.SeedFor(100, 0));
            Assert.AreNotEqual(Leaves.SeedFor(100, 0), Leaves.SeedFor(100, 1), "the second game of the day is another game");
            Assert.AreNotEqual(Leaves.SeedFor(100, 0), Leaves.SeedFor(101, 0), "and so is tomorrow's first");
        }

        [TestCase(0.5, 0.5, true)]
        [TestCase(0.5, 0.62, true)]
        [TestCase(0.5, 0.38, true)]
        [TestCase(0.5, 0.63, false)]
        [TestCase(0.5, 0.37, false)]
        [TestCase(0.0, 0.1, true)]
        [TestCase(1.0, 0.8, false)]
        public void A_Leaf_Is_Caught_When_It_Lands_Within_The_Basket(double leafX, double basketX, bool caught)
        {
            Assert.AreEqual(caught, Leaves.InReach(leafX, basketX));
        }

        [Test]
        public void A_Leaf_Is_One_A_Golden_Leaf_Two_A_Pine_Cone_Minus_Three_Never_Below_Zero_And_A_Cone_Stuns_The_Basket()
        {
            var run = new LeavesRun(new[]
            {
                At(2.0, 0.5, LeafKind.Cone),
                At(3.0, 0.5, LeafKind.Leaf),
                At(4.0, 0.5, LeafKind.Golden),
                At(5.0, 0.5, LeafKind.Cone),
                At(5.0 + Leaves.StunSeconds - 0.1, 0.5, LeafKind.Leaf),
                At(5.0 + Leaves.StunSeconds, 0.5, LeafKind.Leaf),
            });
            IReadOnlyList<Drop> first = run.Advance(2.0, 0.5);
            Assert.AreEqual(1, first.Count);
            Assert.IsTrue(first[0].Caught, "the cone fell in");
            Assert.AreEqual(0, run.Score, "a cone at zero stays at zero");
            Assert.AreEqual(1, run.Cones);
            Assert.AreEqual(2.0 + Leaves.StunSeconds, run.StunnedUntil, 1e-9);
            Assert.IsTrue(run.Stunned(2.2));
            Assert.IsFalse(run.Stunned(2.6));

            run.Advance(4.5, 0.5);
            Assert.AreEqual(3, run.Score, "one for the leaf, two for the golden one");
            Assert.AreEqual(2, run.Caught);
            Assert.AreEqual(1, run.Golden);

            IReadOnlyList<Drop> rest = run.Advance(10.0, 0.5);
            Assert.AreEqual(3, rest.Count);
            Assert.IsTrue(rest[0].Caught, "the second cone");
            Assert.IsFalse(rest[1].Caught, "the leaf that lands while the basket is stunned slips past");
            Assert.IsTrue(rest[2].Caught, "the one that lands as the stun ends is caught");
            Assert.AreEqual(3 - Leaves.ConePenalty + 1, run.Score);
            Assert.AreEqual(1, run.Missed);
            Assert.AreEqual(2, run.Cones);
            Assert.IsTrue(run.Finished);
            Assert.AreEqual(0, run.Advance(20.0, 0.5).Count, "nothing more falls");
        }

        [Test]
        public void Drops_Are_Settled_In_Landing_Order_As_The_Clock_Passes_Them()
        {
            var run = new LeavesRun(new[] { At(2.0, 0.2, LeafKind.Leaf), At(3.0, 0.8, LeafKind.Leaf), At(4.0, 0.5, LeafKind.Leaf) });
            Assert.AreEqual(0, run.Advance(1.9, 0.2).Count, "nothing has landed yet");
            Assert.AreEqual(0, run.Settled);
            IReadOnlyList<Drop> one = run.Advance(2.0, 0.2);
            Assert.AreEqual(1, one.Count);
            Assert.AreEqual(0, one[0].Leaf.Index);
            Assert.IsTrue(one[0].Caught);
            Assert.AreEqual(1, run.Settled);
            IReadOnlyList<Drop> two = run.Advance(4.5, 0.2);
            Assert.AreEqual(2, two.Count, "two landed while the basket stayed left");
            Assert.IsFalse(two[0].Caught);
            Assert.IsFalse(two[1].Caught);
            Assert.AreEqual(2, run.Missed);
            Assert.AreEqual(1, run.Score);
            Assert.IsTrue(run.Finished);
        }

        [Test]
        public void Catching_Every_Leaf_And_No_Cone_Is_The_Most_A_Game_Can_Score_And_Earns_Three_Stars()
        {
            LeavesRun run = Play(3);
            Assert.AreEqual(3, run.Seed);
            Assert.IsTrue(run.Finished);
            Assert.AreEqual(Leaves.MaxScore, run.Score);
            Assert.AreEqual(Leaves.LeafCount + Leaves.GoldenCount, run.Caught);
            Assert.AreEqual(0, run.Cones);
            Assert.AreEqual(Leaves.ConeCount, run.Missed, "the cones went past");
            Assert.AreEqual(3, Leaves.StarsFor(run.Score));
        }

        [Test]
        public void Stars_Come_At_About_A_Third_Two_Thirds_And_Seven_Eighths_Of_The_Most()
        {
            Assert.AreEqual(0, Leaves.StarsFor(0));
            Assert.AreEqual(0, Leaves.StarsFor(Leaves.OneStar - 1));
            Assert.AreEqual(1, Leaves.StarsFor(Leaves.OneStar));
            Assert.AreEqual(1, Leaves.StarsFor(Leaves.TwoStars - 1));
            Assert.AreEqual(2, Leaves.StarsFor(Leaves.TwoStars));
            Assert.AreEqual(2, Leaves.StarsFor(Leaves.ThreeStars - 1));
            Assert.AreEqual(3, Leaves.StarsFor(Leaves.ThreeStars));
            Assert.AreEqual(3, Leaves.StarsFor(Leaves.MaxScore));
            Assert.That(Leaves.OneStar / (double)Leaves.MaxScore, Is.InRange(0.3, 0.37));
            Assert.That(Leaves.TwoStars / (double)Leaves.MaxScore, Is.InRange(0.63, 0.7));
            Assert.That(Leaves.ThreeStars / (double)Leaves.MaxScore, Is.InRange(0.85, 0.9));
        }

        [Test]
        public void Three_Plays_A_Day_That_Come_Back_Tomorrow_But_Not_When_The_Clock_Goes_Back()
        {
            Assert.AreEqual(3, Leaves.PlaysLeft(s, 100));
            Assert.IsTrue(Leaves.Start(s, 100));
            Assert.IsTrue(Leaves.Start(s, 100));
            Assert.IsTrue(Leaves.Start(s, 100));
            Assert.AreEqual(0, Leaves.PlaysLeft(s, 100));
            Assert.IsFalse(Leaves.Start(s, 100), "the day's plays are spent");
            Assert.AreEqual(0, Leaves.PlaysLeft(s, 99), "a clock turned back brings nothing");
            Assert.IsFalse(Leaves.Start(s, 99));
            Assert.AreEqual(3, Leaves.PlaysLeft(s, 101), "tomorrow they are back");
            Assert.IsTrue(Leaves.Start(s, 101));
            Assert.AreEqual(2, Leaves.PlaysLeft(s, 101));
            Assert.AreEqual(3, Pounding.PlaysLeft(s, 101), "the mooncake game keeps its own plays");
        }

        [Test]
        public void The_Basket_Waits_For_Garden_Level_Seven()
        {
            Assert.AreEqual(7, Leaves.UnlockLevel);
            s.GardenXp = Garden.XpForLevel(Leaves.UnlockLevel - 1);
            Assert.IsFalse(Leaves.Unlocked(s));
            Assert.IsFalse(Leaves.Start(s, 5));
            Assert.IsTrue(Pounding.Unlocked(s), "a level the mortar is already open at");
            s.GardenXp = Garden.XpForLevel(Leaves.UnlockLevel);
            Assert.IsTrue(Leaves.Unlocked(s));
            Assert.IsTrue(Leaves.Start(s, 5));
        }

        [Test]
        public void A_Save_That_Already_Knew_The_Basket_Below_Level_Seven_Keeps_It()
        {
            Assert.AreEqual(4, Leaves.KeptFromLevel);
            s.GardenXp = Garden.XpForLevel(Leaves.KeptFromLevel);
            Assert.IsFalse(Leaves.Unlocked(s), "a new garden at level four waits for seven");
            s.Leaves.Games = 1;
            Assert.IsTrue(Leaves.Unlocked(s), "a garden that played it keeps it");
            Assert.IsTrue(Leaves.Start(s, 5));
            s.Leaves.Games = 0;
            s.IntrosSeen.Add(Intros.Leaves);
            Assert.IsTrue(Leaves.Unlocked(s), "a garden that was shown its card keeps it");
            s.GardenXp = Garden.XpForLevel(Leaves.KeptFromLevel - 1);
            Assert.IsFalse(Leaves.Unlocked(s), "below the old level it was shut before too");
        }

        [Test]
        public void A_Full_Score_Pays_A_Minute_Of_Income_By_The_Quest_Rule()
        {
            Modifiers m = Modifiers.None;
            BigNum minute = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * 60d);
            Assert.IsFalse(minute.IsZero);
            Assert.AreEqual(minute.ToDouble(), Leaves.Reward(defs, s, m, Leaves.MaxScore).ToDouble(), 1e-9);
            Assert.AreEqual(minute.ToDouble() / 2d, Leaves.Reward(defs, s, m, Leaves.MaxScore / 2).ToDouble(), 1e-9);
            Assert.AreEqual(minute.ToDouble(), Leaves.Reward(defs, s, m, Leaves.MaxScore + 50).ToDouble(), 1e-9, "never more than the most");
            Assert.IsTrue(Leaves.Reward(defs, s, m, 0).IsZero);
        }

        [Test]
        public void Finishing_Pays_And_The_First_Starred_Game_Of_The_Day_Brings_One_Clover_But_No_Golden_Carrot()
        {
            Modifiers m = Modifiers.None;
            LeavesRun full = Play(5);
            Assert.IsTrue(Leaves.Start(s, 10));
            BigNum before = s.Coins;
            s.PoundingStarToday = true;
            LeavesResult first = Leaves.Finish(defs, s, m, full);
            Assert.AreEqual(3, first.Stars);
            Assert.AreEqual(Leaves.MaxScore, first.Score);
            Assert.AreEqual(1, first.Clovers, "the first starred game of the day brings a clover");
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(0, s.GoldenCarrots, "carrots are this game's reward; no golden one");
            Assert.AreEqual((before + first.Carrots).ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(1, s.Leaves.Games);
            Assert.AreEqual(3, s.Leaves.BestStars);

            Assert.IsTrue(Leaves.Start(s, 10));
            LeavesResult second = Leaves.Finish(defs, s, m, Play(6));
            Assert.AreEqual(0, second.Clovers, "only once a day");
            Assert.AreEqual(2, s.Leaves.Games);

            Assert.IsTrue(Leaves.Start(s, 11));
            var poor = new LeavesRun(new[] { At(2.0, 0.5, LeafKind.Leaf) });
            poor.Advance(3.0, 0.0);
            LeavesResult zero = Leaves.Finish(defs, s, m, poor);
            Assert.AreEqual(0, zero.Stars);
            Assert.AreEqual(0, zero.Clovers, "no star, no clover");
            Assert.IsTrue(zero.Carrots.IsZero);
            Assert.AreEqual(3, s.Leaves.BestStars, "the best stays");
            Assert.AreEqual(1, s.Clovers, "tomorrow's clover is still to come");
            Assert.AreEqual(0, s.PoundingGames, "the mooncake game's count is its own");
        }

        [Test]
        public void Finishing_Reads_The_Stars_From_The_Score()
        {
            int wanted = Leaves.TwoStars;
            LeavesRun run = Play(11, wanted);
            Assert.That(run.Score, Is.InRange(wanted, wanted + 1), "a golden leaf may carry it one past");
            Leaves.Start(s, 1);
            LeavesResult result = Leaves.Finish(defs, s, Modifiers.None, run);
            Assert.AreEqual(2, result.Stars);
            Assert.AreEqual(run.Score, result.Score);
            Assert.AreEqual(2, s.Leaves.BestStars);
        }

        [Test]
        public void The_Game_Round_Trips_Through_The_Save_And_An_Older_Save_Has_All_Its_Plays()
        {
            Leaves.Start(s, 42);
            Leaves.Start(s, 42);
            s.Leaves.StarToday = true;
            s.Leaves.Games = 7;
            s.Leaves.BestStars = 2;
            s.PoundingGames = 3;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(42, r.Leaves.Day);
            Assert.AreEqual(2, r.Leaves.Plays);
            Assert.AreEqual(1, Leaves.PlaysLeft(r, 42));
            Assert.IsTrue(r.Leaves.StarToday);
            Assert.AreEqual(7, r.Leaves.Games);
            Assert.AreEqual(2, r.Leaves.BestStars);
            Assert.AreEqual(3, r.PoundingGames, "the mooncake block is untouched");
            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"pounding\":{\"day\":42,\"plays\":3,\"games\":5,\"best\":1}}").State;
            Assert.AreEqual(3, Leaves.PlaysLeft(old, 42), "a save from before the game has its plays");
            Assert.AreEqual(0, old.Leaves.Games);
            Assert.AreEqual(0, old.Leaves.BestStars);
            Assert.IsFalse(old.Leaves.StarToday);
            Assert.AreEqual(0, Pounding.PlaysLeft(old, 42), "and the mooncake game's spent plays stay spent");
            GameState clipped = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"leaves\":{\"day\":42,\"plays\":99,\"games\":-4,\"best\":9}}").State;
            Assert.AreEqual(0, Leaves.PlaysLeft(clipped, 42));
            Assert.AreEqual(0, clipped.Leaves.Games);
            Assert.AreEqual(3, clipped.Leaves.BestStars);
        }

        [Test]
        public void Two_Achievements_Read_The_Best_Stars_And_The_Games_Played()
        {
            var three = new QuestDef { Id = "la-3sao", Metric = QuestMetric.LeavesThreeStar, Target = 1 };
            var thirty = new QuestDef { Id = "la-30", Metric = QuestMetric.LeavesGames, Target = 30 };
            Assert.AreEqual(0, Quests.Progress(three, defs, s));
            Assert.AreEqual(0, Quests.Progress(thirty, defs, s));
            s.Leaves.BestStars = 2;
            s.Leaves.Games = 12;
            s.PoundingGames = 40;
            Assert.AreEqual(0, Quests.Progress(three, defs, s));
            Assert.AreEqual(12, Quests.Progress(thirty, defs, s), "the mooncake games do not count");
            s.Leaves.BestStars = 3;
            Assert.AreEqual(1, Quests.Progress(three, defs, s));
            Assert.Greater((int)QuestMetric.LeavesThreeStar, (int)QuestMetric.RabbitsAtCap, "new metrics go at the end: assets store the number");
            Assert.AreEqual((int)QuestMetric.LeavesThreeStar + 1, (int)QuestMetric.LeavesGames);
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The firefly night: its swarm, the catching, the scoring, the plays per day and the capped pay.</summary>
    public class FirefliesTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
            s.GardenXp = Garden.XpForLevel(5);
        }

        /// <summary>A fly that crosses from left to right at height <paramref name="y"/> between <paramref name="appears"/> and five seconds later, without wobble.</summary>
        static Firefly Crossing(int index, double appears, double y, bool golden = false) =>
            new Firefly { Index = index, Appears = appears, Crossing = 5d, FromX = 0d, ToX = 1d, Y = y, Wobble = 0d, Waves = 1d, Phase = 0d, Golden = golden };

        /// <summary>Plays a seeded game through: every fly tapped where it is just after it appears, until the score reaches <paramref name="wanted"/>.</summary>
        static FirefliesRun Play(long seed, int wanted = int.MaxValue)
        {
            var run = FirefliesRun.FromSeed(seed);
            foreach (Firefly fly in run.Schedule)
            {
                if (run.Score >= wanted) break;
                double at = fly.Appears + 0.2;
                FlySpot spot = Fireflies.PositionAt(fly, at);
                run.Catch(spot.X, spot.Y, at);
            }
            return run;
        }

        [Test]
        public void The_Swarm_Is_The_Same_For_A_Seed_And_Another_For_Another_Seed()
        {
            IReadOnlyList<Firefly> a = Fireflies.ScheduleFor(7), again = Fireflies.ScheduleFor(7), b = Fireflies.ScheduleFor(8);
            Assert.AreEqual(Fireflies.Count, a.Count);
            Assert.AreEqual(a.Count, again.Count);
            bool differs = false;
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Appears, again[i].Appears);
                Assert.AreEqual(a[i].Crossing, again[i].Crossing);
                Assert.AreEqual(a[i].Y, again[i].Y);
                Assert.AreEqual(a[i].FromX, again[i].FromX);
                Assert.AreEqual(a[i].Golden, again[i].Golden);
                Assert.AreEqual(i, a[i].Index);
                if (a[i].Y != b[i].Y || a[i].Golden != b[i].Golden) differs = true;
            }
            Assert.IsTrue(differs, "another seed is another night");
        }

        [Test]
        public void Every_Swarm_Has_Three_Golden_Flies_And_Fits_In_Twenty_Seconds()
        {
            for (long seed = 0; seed < 40; seed++)
            {
                IReadOnlyList<Firefly> list = Fireflies.ScheduleFor(seed);
                int golden = 0, fromLeft = 0;
                for (int i = 0; i < list.Count; i++)
                {
                    Firefly fly = list[i];
                    Assert.GreaterOrEqual(fly.Appears, Fireflies.Lead, "seed " + seed);
                    Assert.That(fly.Crossing, Is.InRange(Fireflies.MinCrossing, Fireflies.MaxCrossing), "a crossing takes a few seconds");
                    Assert.LessOrEqual(fly.Appears + fly.Crossing, Fireflies.Seconds, "every fly is gone before the clock runs out");
                    if (i > 0) Assert.GreaterOrEqual(fly.Appears, list[i - 1].Appears, "flies come one after another");
                    Assert.That(fly.Y, Is.InRange(0.1, 0.9));
                    Assert.IsTrue((fly.FromX <= 0.05 && fly.ToX >= 0.95) || (fly.FromX >= 0.95 && fly.ToX <= 0.05), "from one side to the other");
                    if (fly.FromX < 0.5) fromLeft++;
                    if (fly.Golden) golden++;
                    for (double t = fly.Appears; t <= fly.Appears + fly.Crossing; t += 0.1)
                    {
                        FlySpot spot = Fireflies.PositionAt(fly, t);
                        Assert.That(spot.X, Is.InRange(0d, 1d), "seed " + seed + " fly " + i);
                        Assert.That(spot.Y, Is.InRange(0d, 1d), "seed " + seed + " fly " + i);
                    }
                }
                Assert.AreEqual(Fireflies.GoldenCount, golden, "seed " + seed);
                Assert.That(fromLeft, Is.InRange(3, Fireflies.Count - 3), "both sides send flies, seed " + seed);
            }
            Assert.AreEqual((Fireflies.Count - Fireflies.GoldenCount) * Fireflies.PlainPoints + Fireflies.GoldenCount * Fireflies.GoldenPoints, Fireflies.MaxScore);
        }

        [Test]
        public void The_Seed_Comes_From_The_Day_And_The_Games_Played_So_Far()
        {
            Assert.AreEqual(Fireflies.SeedFor(100, 0), Fireflies.SeedFor(100, 0));
            Assert.AreNotEqual(Fireflies.SeedFor(100, 0), Fireflies.SeedFor(100, 1), "the second game of the night is another game");
            Assert.AreNotEqual(Fireflies.SeedFor(100, 0), Fireflies.SeedFor(101, 0), "and so is tomorrow's first");
        }

        [Test]
        public void A_Fly_Is_Seen_Only_While_It_Crosses_And_Moves_From_One_Side_To_The_Other()
        {
            Firefly fly = Crossing(0, 2.0, 0.5);
            Assert.IsFalse(Fireflies.Visible(fly, 1.9));
            Assert.IsTrue(Fireflies.Visible(fly, 2.0));
            Assert.IsTrue(Fireflies.Visible(fly, 7.0));
            Assert.IsFalse(Fireflies.Visible(fly, 7.1));
            Assert.AreEqual(0d, Fireflies.PositionAt(fly, 2.0).X, 1e-9);
            Assert.AreEqual(0.5, Fireflies.PositionAt(fly, 4.5).X, 1e-9);
            Assert.AreEqual(1d, Fireflies.PositionAt(fly, 7.0).X, 1e-9);
            Assert.AreEqual(0.5, Fireflies.PositionAt(fly, 4.5).Y, 1e-9, "no wobble, so it holds its height");
            Assert.AreEqual(0d, Fireflies.PositionAt(fly, 0.0).X, 1e-9, "before it appears it waits at its start");
            Assert.AreEqual(1d, Fireflies.PositionAt(fly, 20.0).X, 1e-9, "and after, it rests at its end");

            Firefly wobbly = Crossing(1, 2.0, 0.5);
            wobbly.Wobble = 0.1;
            wobbly.Waves = 1d;
            Assert.AreEqual(0.5, Fireflies.PositionAt(wobbly, 2.0).Y, 1e-9);
            Assert.AreEqual(0.6, Fireflies.PositionAt(wobbly, 2.0 + 1.25).Y, 1e-9, "a quarter wave up");
            Assert.AreEqual(0.4, Fireflies.PositionAt(wobbly, 2.0 + 3.75).Y, 1e-9, "three quarters down");
        }

        [Test]
        public void A_Tap_Catches_The_Nearest_Visible_Fly_Within_Reach_And_Only_Once()
        {
            var run = new FirefliesRun(new[] { Crossing(0, 0.0, 0.3), Crossing(1, 0.0, 0.5), Crossing(2, 10.0, 0.5) });
            // At 2.5 s both early flies are at x = 0.5; the tap is nearer the one at 0.5 high.
            Assert.AreEqual(1, run.Catch(0.5, 0.45, 2.5));
            Assert.AreEqual(1, run.Score);
            Assert.AreEqual(1, run.Caught);
            Assert.IsTrue(run.IsCaught(1));
            Assert.IsFalse(run.IsCaught(0));
            Assert.AreEqual(-1, run.Catch(0.5, 0.5, 2.5), "the caught one is gone; the other is too far");
            Assert.AreEqual(1, run.Misses, "a miss is counted but costs nothing");
            Assert.AreEqual(1, run.Score);
            Assert.AreEqual(-1, run.Catch(0.5, 0.5, 2.5), "the fly not yet out does not count");
            Assert.AreEqual(0, run.Catch(0.5 + Fireflies.CatchRadius - 0.001, 0.3, 2.5), "just within reach");
            Assert.AreEqual(2, run.Score);
            Assert.AreEqual(-1, run.Catch(0.5, 0.5, 9.0), "nothing is out between the two waves");
            Assert.AreEqual(-1, run.Catch(0.5, 0.5 + Fireflies.CatchRadius + 0.01, 12.5), "just out of reach");
            Assert.AreEqual(2, run.Catch(0.5, 0.5, 12.5));
            Assert.AreEqual(3, run.Caught);
            Assert.AreEqual(4, run.Misses);
        }

        [Test]
        public void A_Golden_Fly_Is_Worth_Three()
        {
            var run = new FirefliesRun(new[] { Crossing(0, 0.0, 0.3), Crossing(1, 0.0, 0.7, true) });
            Assert.AreEqual(Fireflies.PlainPoints, Fireflies.Points(run.Schedule[0]));
            Assert.AreEqual(Fireflies.GoldenPoints, Fireflies.Points(run.Schedule[1]));
            Assert.AreEqual(1, run.Catch(0.5, 0.7, 2.5));
            Assert.AreEqual(3, run.Score);
            Assert.AreEqual(1, run.Golden);
            Assert.AreEqual(1, run.Caught);
            Assert.AreEqual(0, run.Catch(0.5, 0.3, 2.5));
            Assert.AreEqual(4, run.Score);
            Assert.AreEqual(1, run.Golden);
        }

        [Test]
        public void Catching_Every_Fly_Is_The_Most_A_Game_Can_Score_And_Earns_Three_Stars()
        {
            FirefliesRun run = Play(3);
            Assert.AreEqual(3, run.Seed);
            Assert.AreEqual(Fireflies.MaxScore, run.Score);
            Assert.AreEqual(Fireflies.Count, run.Caught);
            Assert.AreEqual(Fireflies.GoldenCount, run.Golden);
            Assert.AreEqual(0, run.Misses);
            Assert.AreEqual(3, Fireflies.StarsFor(run.Score));
        }

        [Test]
        public void Stars_Come_At_About_A_Third_Two_Thirds_And_Seven_Eighths_Of_The_Most()
        {
            Assert.AreEqual(0, Fireflies.StarsFor(0));
            Assert.AreEqual(0, Fireflies.StarsFor(Fireflies.OneStar - 1));
            Assert.AreEqual(1, Fireflies.StarsFor(Fireflies.OneStar));
            Assert.AreEqual(1, Fireflies.StarsFor(Fireflies.TwoStars - 1));
            Assert.AreEqual(2, Fireflies.StarsFor(Fireflies.TwoStars));
            Assert.AreEqual(2, Fireflies.StarsFor(Fireflies.ThreeStars - 1));
            Assert.AreEqual(3, Fireflies.StarsFor(Fireflies.ThreeStars));
            Assert.AreEqual(3, Fireflies.StarsFor(Fireflies.MaxScore));
            Assert.That(Fireflies.OneStar / (double)Fireflies.MaxScore, Is.InRange(0.3, 0.37));
            Assert.That(Fireflies.TwoStars / (double)Fireflies.MaxScore, Is.InRange(0.63, 0.7));
            Assert.That(Fireflies.ThreeStars / (double)Fireflies.MaxScore, Is.InRange(0.85, 0.9));
        }

        [Test]
        public void Three_Plays_A_Day_That_Come_Back_Tomorrow_But_Not_When_The_Clock_Goes_Back()
        {
            Assert.AreEqual(3, Fireflies.PlaysLeft(s, 100));
            Assert.IsTrue(Fireflies.Start(s, 100));
            Assert.IsTrue(Fireflies.Start(s, 100));
            Assert.IsTrue(Fireflies.Start(s, 100));
            Assert.AreEqual(0, Fireflies.PlaysLeft(s, 100));
            Assert.IsFalse(Fireflies.Start(s, 100), "the day's plays are spent");
            Assert.AreEqual(0, Fireflies.PlaysLeft(s, 99), "a clock turned back brings nothing");
            Assert.IsFalse(Fireflies.Start(s, 99));
            Assert.AreEqual(3, Fireflies.PlaysLeft(s, 101), "tomorrow they are back");
            Assert.IsTrue(Fireflies.Start(s, 101));
            Assert.AreEqual(2, Fireflies.PlaysLeft(s, 101));
            Assert.AreEqual(3, Leaves.PlaysLeft(s, 101), "the leaf game keeps its own plays");
            Assert.AreEqual(3, Pounding.PlaysLeft(s, 101), "and so does the mooncake game");
        }

        [Test]
        public void The_Jar_Waits_For_Garden_Level_Five()
        {
            s.GardenXp = Garden.XpForLevel(4);
            Assert.IsFalse(Fireflies.Unlocked(s));
            Assert.IsFalse(Fireflies.Start(s, 5));
            Assert.IsTrue(Leaves.Unlocked(s), "a level the basket is already out at");
            s.GardenXp = Garden.XpForLevel(5);
            Assert.IsTrue(Fireflies.Unlocked(s));
            Assert.IsTrue(Fireflies.Start(s, 5));
        }

        [Test]
        public void A_Full_Score_Pays_Twenty_Seconds_Of_Income_By_The_Quest_Rule()
        {
            Modifiers m = Modifiers.None;
            BigNum twenty = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * Fireflies.RewardSeconds);
            Assert.IsFalse(twenty.IsZero);
            Assert.AreEqual(20d, Fireflies.RewardSeconds);
            Assert.AreEqual(twenty.ToDouble(), Fireflies.Reward(defs, s, m, Fireflies.MaxScore).ToDouble(), 1e-9);
            Assert.AreEqual(twenty.ToDouble() / 2d, Fireflies.Reward(defs, s, m, Fireflies.MaxScore / 2).ToDouble(), 1e-9);
            Assert.AreEqual(twenty.ToDouble(), Fireflies.Reward(defs, s, m, Fireflies.MaxScore + 50).ToDouble(), 1e-9, "never more than the most");
            Assert.IsTrue(Fireflies.Reward(defs, s, m, 0).IsZero);
        }

        [Test]
        public void Clovers_Come_One_Per_Ten_Points_Up_To_Three()
        {
            Assert.AreEqual(0, Fireflies.CloversFor(0));
            Assert.AreEqual(0, Fireflies.CloversFor(Fireflies.PointsPerClover - 1));
            Assert.AreEqual(1, Fireflies.CloversFor(Fireflies.PointsPerClover));
            Assert.AreEqual(2, Fireflies.CloversFor(2 * Fireflies.PointsPerClover));
            Assert.AreEqual(Fireflies.CloversMost, Fireflies.CloversFor(Fireflies.MaxScore));
            Assert.AreEqual(Fireflies.CloversMost, Fireflies.CloversFor(Fireflies.MaxScore * 2));
        }

        [Test]
        public void Finishing_Pays_Carrots_Clovers_By_The_Score_And_A_Golden_Carrot_For_Three_Stars_Under_The_Days_Caps()
        {
            Modifiers m = Modifiers.None;
            Assert.IsTrue(Fireflies.Start(s, 10));
            BigNum before = s.Coins;
            s.Leaves.StarToday = true; // the other games' gifts today have nothing to do with this one
            s.PoundingThreeToday = true;
            FirefliesResult first = Fireflies.Finish(defs, s, m, Play(5));
            Assert.AreEqual(3, first.Stars);
            Assert.AreEqual(Fireflies.MaxScore, first.Score);
            Assert.AreEqual(Fireflies.CloversMost, first.Clovers, "a full game brings the most clovers");
            Assert.AreEqual(1, first.Golden, "three stars bring a golden carrot");
            Assert.IsFalse(first.CloversCapped);
            Assert.IsFalse(first.GoldenCapped);
            Assert.AreEqual(Fireflies.CloversMost, s.Clovers);
            Assert.AreEqual(1, s.GoldenCarrots);
            Assert.AreEqual((before + first.Carrots).ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(1, s.Fireflies.Games);
            Assert.AreEqual(3, s.Fireflies.BestStars);
            Assert.AreEqual(Fireflies.CloversMost, s.Fireflies.CloversToday);
            Assert.AreEqual(1, s.Fireflies.GoldenToday);

            // The second full game of the day: the caps are reached, so carrots only, and the card says so.
            Assert.IsTrue(Fireflies.Start(s, 10));
            before = s.Coins;
            FirefliesResult second = Fireflies.Finish(defs, s, m, Play(6));
            Assert.AreEqual(3, second.Stars);
            Assert.AreEqual(0, second.Clovers, "the day's clovers are given");
            Assert.AreEqual(0, second.Golden, "and so is the day's golden carrot");
            Assert.IsTrue(second.CloversCapped);
            Assert.IsTrue(second.GoldenCapped);
            Assert.Greater(s.Coins.ToDouble(), before.ToDouble(), "a capped game still pays carrots");
            Assert.AreEqual(Fireflies.CloversMost, s.Clovers);
            Assert.AreEqual(1, s.GoldenCarrots);
            Assert.AreEqual(2, s.Fireflies.Games);

            // Tomorrow the caps are back: a two-star game brings its clovers and no golden carrot, uncapped.
            Assert.IsTrue(Fireflies.Start(s, 11));
            Assert.AreEqual(0, s.Fireflies.CloversToday, "a new day starts the day's gifts over");
            Assert.AreEqual(0, s.Fireflies.GoldenToday);
            FirefliesRun two = Play(7, Fireflies.TwoStars);
            Assert.AreEqual(2, Fireflies.StarsFor(two.Score));
            FirefliesResult third = Fireflies.Finish(defs, s, m, two);
            Assert.AreEqual(Fireflies.CloversFor(two.Score), third.Clovers);
            Assert.Greater(third.Clovers, 0);
            Assert.AreEqual(0, third.Golden, "two stars: no golden carrot");
            Assert.IsFalse(third.CloversCapped);
            Assert.IsFalse(third.GoldenCapped, "nothing was earned, so nothing was withheld");
            Assert.AreEqual(Fireflies.CloversMost + third.Clovers, s.Clovers);
            Assert.AreEqual(3, s.Fireflies.BestStars, "the best stays");

            // A game with nothing caught pays nothing at all.
            Assert.IsTrue(Fireflies.Start(s, 11));
            var poor = new FirefliesRun(new[] { Crossing(0, 1.0, 0.5) });
            FirefliesResult zero = Fireflies.Finish(defs, s, m, poor);
            Assert.AreEqual(0, zero.Stars);
            Assert.AreEqual(0, zero.Clovers);
            Assert.AreEqual(0, zero.Golden);
            Assert.IsTrue(zero.Carrots.IsZero);
            Assert.AreEqual(0, s.Leaves.Games, "the leaf game's count is its own");
            Assert.AreEqual(0, s.PoundingGames, "and so is the mooncake game's");
        }

        [Test]
        public void A_Partial_Days_Cap_Gives_What_Room_Is_Left()
        {
            Modifiers m = Modifiers.None;
            Assert.IsTrue(Fireflies.Start(s, 10));
            s.Fireflies.CloversToday = Fireflies.CloverCapPerDay - 1;
            FirefliesResult result = Fireflies.Finish(defs, s, m, Play(5));
            Assert.AreEqual(1, result.Clovers, "one clover of room was left");
            Assert.IsTrue(result.CloversCapped);
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(Fireflies.CloverCapPerDay, s.Fireflies.CloversToday);
        }

        [Test]
        public void The_Game_Round_Trips_Through_The_Save_And_An_Older_Save_Has_All_Its_Plays()
        {
            Fireflies.Start(s, 42);
            Fireflies.Start(s, 42);
            s.Fireflies.CloversToday = 2;
            s.Fireflies.GoldenToday = 1;
            s.Fireflies.Games = 7;
            s.Fireflies.BestStars = 2;
            s.Leaves.Games = 3;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(42, r.Fireflies.Day);
            Assert.AreEqual(2, r.Fireflies.Plays);
            Assert.AreEqual(1, Fireflies.PlaysLeft(r, 42));
            Assert.AreEqual(2, r.Fireflies.CloversToday);
            Assert.AreEqual(1, r.Fireflies.GoldenToday);
            Assert.AreEqual(7, r.Fireflies.Games);
            Assert.AreEqual(2, r.Fireflies.BestStars);
            Assert.AreEqual(3, r.Leaves.Games, "the leaf block is untouched");
            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"leaves\":{\"day\":42,\"plays\":3,\"games\":5,\"best\":1}}").State;
            Assert.AreEqual(3, Fireflies.PlaysLeft(old, 42), "a save from before the game has its plays");
            Assert.AreEqual(0, old.Fireflies.Games);
            Assert.AreEqual(0, old.Fireflies.BestStars);
            Assert.AreEqual(0, old.Fireflies.CloversToday);
            Assert.AreEqual(0, old.Fireflies.GoldenToday);
            Assert.AreEqual(0, Leaves.PlaysLeft(old, 42), "and the leaf game's spent plays stay spent");
            GameState clipped = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"fireflies\":{\"day\":42,\"plays\":99,\"games\":-4,\"best\":9,\"clovers\":50,\"golden\":-2}}").State;
            Assert.AreEqual(0, Fireflies.PlaysLeft(clipped, 42));
            Assert.AreEqual(0, clipped.Fireflies.Games);
            Assert.AreEqual(3, clipped.Fireflies.BestStars);
            Assert.AreEqual(Fireflies.CloverCapPerDay, clipped.Fireflies.CloversToday);
            Assert.AreEqual(0, clipped.Fireflies.GoldenToday);
        }

        [Test]
        public void Two_Achievements_Read_The_Best_Stars_And_The_Games_Played()
        {
            var three = new QuestDef { Id = "dom-3sao", Metric = QuestMetric.FirefliesThreeStar, Target = 1 };
            var thirty = new QuestDef { Id = "dom-30", Metric = QuestMetric.FirefliesGames, Target = 30 };
            Assert.AreEqual(0, Quests.Progress(three, defs, s));
            Assert.AreEqual(0, Quests.Progress(thirty, defs, s));
            s.Fireflies.BestStars = 2;
            s.Fireflies.Games = 12;
            s.Leaves.Games = 40;
            Assert.AreEqual(0, Quests.Progress(three, defs, s));
            Assert.AreEqual(12, Quests.Progress(thirty, defs, s), "the leaf games do not count");
            s.Fireflies.BestStars = 3;
            Assert.AreEqual(1, Quests.Progress(three, defs, s));
            Assert.Greater((int)QuestMetric.FirefliesThreeStar, (int)QuestMetric.LeavesGames, "new metrics go at the end: assets store the number");
            Assert.AreEqual((int)QuestMetric.FirefliesThreeStar + 1, (int)QuestMetric.FirefliesGames);
        }
    }
}

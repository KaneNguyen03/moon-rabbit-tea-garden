using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class PoundingTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
            s.GardenXp = Garden.XpForLevel(3);
        }

        [Test]
        public void Thirty_Beats_Fit_In_About_Twenty_Five_Seconds_And_Quicken_Every_Five()
        {
            Assert.AreEqual(1.0, Pounding.Interval(0), 1e-9);
            Assert.AreEqual(1.0, Pounding.Interval(4), 1e-9);
            Assert.AreEqual(0.9, Pounding.Interval(5), 1e-9);
            Assert.AreEqual(0.9 * 0.9, Pounding.Interval(10), 1e-9);
            Assert.AreEqual(Pounding.Lead, Pounding.BeatTime(0), 1e-9);
            Assert.AreEqual(Pounding.Lead + 1.0, Pounding.BeatTime(1), 1e-9);
            for (int b = 1; b < Pounding.Beats; b++) Assert.Greater(Pounding.BeatTime(b), Pounding.BeatTime(b - 1));
            Assert.That(Pounding.Length, Is.InRange(24d, 26d), "about twenty-five seconds in all");
        }

        [TestCase(0.0, Hit.Perfect)]
        [TestCase(0.079, Hit.Perfect)]
        [TestCase(-0.079, Hit.Perfect)]
        [TestCase(0.1, Hit.Good)]
        [TestCase(-0.159, Hit.Good)]
        [TestCase(0.2, Hit.Miss)]
        [TestCase(-0.5, Hit.Miss)]
        public void A_Tap_Is_Judged_By_How_Far_It_Is_From_The_Beat(double offset, Hit expected)
        {
            Assert.AreEqual(expected, Pounding.Judge(Pounding.BeatTime(7) + offset, 7));
        }

        [Test]
        public void Perfects_Score_Three_Goods_One_And_Five_Perfects_In_A_Row_Make_It_Chewy()
        {
            var run = new PoundingRun();
            for (int b = 0; b < 4; b++) Assert.AreEqual(Hit.Perfect, run.Tap(Pounding.BeatTime(b)));
            Assert.AreEqual(12, run.Score);
            Assert.IsFalse(run.Chewy);
            run.Tap(Pounding.BeatTime(4));
            Assert.IsTrue(run.Chewy, "the fifth perfect in a row");
            Assert.AreEqual(15 + Pounding.ChewyBonus, run.Score);
            run.Tap(Pounding.BeatTime(5));
            Assert.AreEqual(15 + Pounding.ChewyBonus + 4, run.Score, "chewy beats earn four");
            Assert.AreEqual(Hit.Good, run.Tap(Pounding.BeatTime(6) + 0.12));
            Assert.AreEqual(15 + Pounding.ChewyBonus + 4 + 1, run.Score, "a good earns one and keeps the dough chewy");
            Assert.IsTrue(run.Chewy);
            Assert.AreEqual(Hit.Miss, run.Tap(Pounding.BeatTime(7) + 0.4));
            Assert.IsFalse(run.Chewy, "a miss breaks the streak");
            Assert.AreEqual(0, run.PerfectStreak);
            Assert.AreEqual(8, run.Beat);
        }

        [Test]
        public void Every_Beat_Perfect_Is_The_Most_A_Game_Can_Score_And_Earns_Three_Stars()
        {
            var run = new PoundingRun();
            for (int b = 0; b < Pounding.Beats; b++) run.Tap(Pounding.BeatTime(b));
            Assert.IsTrue(run.Finished);
            Assert.AreEqual(Pounding.MaxScore, run.Score);
            Assert.AreEqual(3, Pounding.StarsFor(run.Score));
            Assert.AreEqual(Hit.Miss, run.Tap(100d), "nothing more to tap");
            Assert.AreEqual(Pounding.MaxScore, run.Score);
        }

        [TestCase(0, 0)]
        [TestCase(39, 0)]
        [TestCase(40, 1)]
        [TestCase(74, 1)]
        [TestCase(75, 2)]
        [TestCase(104, 2)]
        [TestCase(105, 3)]
        public void Stars_Come_At_Forty_Seventy_Five_And_A_Hundred_And_Five(int score, int stars)
        {
            Assert.AreEqual(stars, Pounding.StarsFor(score));
        }

        [Test]
        public void Beats_Left_Untouched_Become_Misses_As_Time_Passes()
        {
            var run = new PoundingRun();
            Assert.AreEqual(0, run.Advance(Pounding.BeatTime(0) + 0.1), "the window is still open");
            Assert.AreEqual(1, run.Advance(Pounding.BeatTime(0) + 0.2));
            Assert.AreEqual(1, run.Misses);
            Assert.AreEqual(2, run.Advance(Pounding.BeatTime(2) + 0.2), "two more went by");
            Assert.AreEqual(3, run.Beat);
            run.Advance(1000d);
            Assert.IsTrue(run.Finished);
            Assert.AreEqual(Pounding.Beats, run.Misses);
            Assert.AreEqual(0, run.Score);
        }

        [Test]
        public void Three_Plays_A_Day_That_Come_Back_Tomorrow_But_Not_When_The_Clock_Goes_Back()
        {
            Assert.AreEqual(3, Pounding.PlaysLeft(s, 100));
            Assert.IsTrue(Pounding.Start(s, 100));
            Assert.IsTrue(Pounding.Start(s, 100));
            Assert.IsTrue(Pounding.Start(s, 100));
            Assert.AreEqual(0, Pounding.PlaysLeft(s, 100));
            Assert.IsFalse(Pounding.Start(s, 100), "the day's plays are spent");
            Assert.AreEqual(0, Pounding.PlaysLeft(s, 99), "a clock turned back brings nothing");
            Assert.IsFalse(Pounding.Start(s, 99));
            Assert.AreEqual(3, Pounding.PlaysLeft(s, 101), "tomorrow they are back");
            Assert.IsTrue(Pounding.Start(s, 101));
            Assert.AreEqual(2, Pounding.PlaysLeft(s, 101));
        }

        [Test]
        public void The_Mortar_Waits_For_Garden_Level_Three()
        {
            s.GardenXp = Garden.XpForLevel(2);
            Assert.IsFalse(Pounding.Unlocked(s));
            Assert.IsFalse(Pounding.Start(s, 5));
            s.GardenXp = Garden.XpForLevel(3);
            Assert.IsTrue(Pounding.Start(s, 5));
        }

        [Test]
        public void A_Full_Score_Pays_A_Minute_Of_Income_By_The_Quest_Rule_And_Half_Again_With_A_Baker()
        {
            Modifiers m = Modifiers.None;
            BigNum minute = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * 60d);
            Assert.AreEqual(minute.ToDouble(), Pounding.Reward(defs, s, m, 120, false).ToDouble(), 1e-9);
            Assert.AreEqual(minute.ToDouble() / 2d, Pounding.Reward(defs, s, m, 60, false).ToDouble(), 1e-9);
            Assert.AreEqual(minute.ToDouble() * 1.5, Pounding.Reward(defs, s, m, 120, true).ToDouble(), 1e-9);
            Assert.IsTrue(Pounding.Reward(defs, s, m, 0, true).IsZero);
        }

        [Test]
        public void The_Baker_Who_Pounds_Along_Is_The_Highest_Working_One()
        {
            string baker = null, brewer = "tho-bong";
            foreach (RabbitKind k in RabbitKinds.OfTier(0)) if (k.Trade == RabbitTrade.Baker) { baker = k.Id; break; }
            Assert.IsNotNull(baker, "the kind table has a silver baker");
            Assert.IsNull(Pounding.Helper(s));
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = brewer, Name = "A", Level = 9, Station = 0 });
            s.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Kind = baker, Name = "B", Level = 2, Station = -1 });
            Assert.IsNull(Pounding.Helper(s), "a resting baker does not come");
            s.Rabbits[1].Station = 1;
            Assert.AreEqual(2, Pounding.Helper(s).Id);
            s.Rabbits.Add(new Rabbit { Id = 3, Tier = 0, Kind = baker, Name = "C", Level = 6, Station = 2 });
            Assert.AreEqual(3, Pounding.Helper(s).Id, "the higher-level baker");
        }

        [Test]
        public void Finishing_Pays_Once_Per_Day_For_The_First_Star_And_The_First_Three_Stars()
        {
            Modifiers m = Modifiers.None;
            var full = new PoundingRun();
            for (int b = 0; b < Pounding.Beats; b++) full.Tap(Pounding.BeatTime(b));
            Assert.IsTrue(Pounding.Start(s, 10));
            BigNum before = s.Coins;
            PoundingResult first = Pounding.Finish(defs, s, m, full);
            Assert.AreEqual(3, first.Stars);
            Assert.AreEqual(1, first.Clovers, "the first starred game of the day brings a clover");
            Assert.AreEqual(1, first.Golden, "and the first three-star game a golden carrot");
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(1, s.GoldenCarrots);
            Assert.AreEqual((before + first.Carrots).ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(1, s.PoundingGames);
            Assert.AreEqual(3, s.PoundingBestStars);

            Assert.IsTrue(Pounding.Start(s, 10));
            PoundingResult second = Pounding.Finish(defs, s, m, full);
            Assert.AreEqual(0, second.Clovers, "only once a day");
            Assert.AreEqual(0, second.Golden);
            Assert.AreEqual(2, s.PoundingGames);

            Assert.IsTrue(Pounding.Start(s, 11));
            var poor = new PoundingRun();
            poor.Advance(1000d);
            PoundingResult zero = Pounding.Finish(defs, s, m, poor);
            Assert.AreEqual(0, zero.Stars);
            Assert.AreEqual(0, zero.Clovers, "no star, no clover");
            Assert.IsTrue(zero.Carrots.IsZero);
            Assert.AreEqual(3, s.PoundingBestStars, "the best stays");
        }

        [Test]
        public void The_Game_Round_Trips_Through_The_Save()
        {
            Pounding.Start(s, 42);
            Pounding.Start(s, 42);
            s.PoundingStarToday = true;
            s.PoundingGames = 7;
            s.PoundingBestStars = 2;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(42, r.PoundingDay);
            Assert.AreEqual(2, r.PoundingPlays);
            Assert.AreEqual(1, Pounding.PlaysLeft(r, 42));
            Assert.IsTrue(r.PoundingStarToday);
            Assert.IsFalse(r.PoundingThreeToday);
            Assert.AreEqual(7, r.PoundingGames);
            Assert.AreEqual(2, r.PoundingBestStars);
            GameState old = SaveCodec.Decode(defs, "{\"version\":13,\"stations\":[]}").State;
            Assert.AreEqual(3, Pounding.PlaysLeft(old, 42), "an older save has its plays");
            Assert.AreEqual(0, old.PoundingGames);
        }

        [Test]
        public void Two_Achievements_Read_The_Best_Stars_And_The_Games_Played()
        {
            var three = new QuestDef { Id = "gia-3sao", Metric = QuestMetric.PoundingThreeStar, Target = 1 };
            var thirty = new QuestDef { Id = "gia-30", Metric = QuestMetric.PoundingGames, Target = 30 };
            Assert.AreEqual(0, Quests.Progress(three, defs, s));
            Assert.AreEqual(0, Quests.Progress(thirty, defs, s));
            s.PoundingBestStars = 2;
            s.PoundingGames = 12;
            Assert.AreEqual(0, Quests.Progress(three, defs, s));
            Assert.AreEqual(12, Quests.Progress(thirty, defs, s));
            s.PoundingBestStars = 3;
            Assert.AreEqual(1, Quests.Progress(three, defs, s));
        }
    }
}

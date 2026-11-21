using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class QuestsTests
    {
        List<StationDef> defs;
        List<QuestDef> quests;
        GameState s;
        QuestDef dailyTaps, dailyCarrots, open2, teaLevel, earned;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            dailyTaps = new QuestDef { Id = "d-taps", Metric = QuestMetric.Taps, Target = 5, Daily = true, RewardSeconds = 60, RewardCarrots = 30, RewardClovers = 1 };
            dailyCarrots = new QuestDef { Id = "d-carrots", Metric = QuestMetric.CarrotsEarned, Target = 0, Daily = true, RewardSeconds = 600, RewardCarrots = 60 };
            open2 = new QuestDef { Id = "open-2", Metric = QuestMetric.StationsOpen, Target = 2, RewardSeconds = 0, RewardCarrots = 50 };
            teaLevel = new QuestDef { Id = "tea-10", Metric = QuestMetric.StationLevel, StationId = "tea", Target = 10, RewardSeconds = 100, RewardCarrots = 1 };
            earned = new QuestDef { Id = "earn-1k", Metric = QuestMetric.CarrotsEarned, Target = 1000, RewardCarrots = 10 };
            quests = new List<QuestDef> { dailyTaps, dailyCarrots, open2, teaLevel, earned };
            s = GameState.NewGame(defs);
            Quests.RollDaily(quests, defs, s, 100, Modifiers.None);
        }

        [Test]
        public void Progress_Reads_The_Right_Number()
        {
            s.Levels = new[] { 7, 1, 0 };
            s.TotalTaps = 3;
            Assert.AreEqual(2, Quests.Progress(open2, defs, s));
            Assert.AreEqual(7, Quests.Progress(teaLevel, defs, s));
            Assert.AreEqual(3, Quests.Progress(dailyTaps, defs, s));
            Assert.AreEqual(0, Quests.Progress(new QuestDef { Metric = QuestMetric.StationLevel, StationId = "nope" }, defs, s));
        }

        [Test]
        public void Cannot_Claim_Before_It_Is_Done()
        {
            Assert.IsFalse(Quests.TryClaim(open2, defs, s, Modifiers.None, out BigNum paid));
            Assert.IsTrue(paid.Equals(BigNum.Zero));
            Assert.IsFalse(Quests.IsClaimed(open2, s));
        }

        [Test]
        public void Claim_Pays_Once()
        {
            s.Levels = new[] { 1, 1, 0 };
            Assert.IsTrue(Quests.TryClaim(open2, defs, s, Modifiers.None, out BigNum paid));
            Assert.AreEqual(50, paid.ToDouble(), 1e-9);
            Assert.AreEqual(50, s.Coins.ToDouble(), 1e-9);
            Assert.IsFalse(Quests.TryClaim(open2, defs, s, Modifiers.None, out _), "second claim");
            Assert.AreEqual(50, s.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Reward_Is_The_Larger_Of_Flat_And_Income_Based()
        {
            s.Levels = new[] { 30, 0, 0 }; // 30 × 4 (two milestones) per 1.5 s = 80/s
            Assert.AreEqual(8000, Quests.RewardOf(teaLevel, defs, s, Modifiers.None).ToDouble(), 1e-6);
            var doubled = Modifiers.None;
            doubled.IncomeMultiplier = 2;
            Assert.AreEqual(16000, Quests.RewardOf(teaLevel, defs, s, doubled).ToDouble(), 1e-6);
            Assert.AreEqual(50, Quests.RewardOf(open2, defs, s, Modifiers.None).ToDouble(), 1e-9);
        }

        [Test]
        public void Rewards_Do_Not_Count_As_Earned()
        {
            s.Levels = new[] { 1, 1, 0 };
            Quests.TryClaim(open2, defs, s, Modifiers.None, out _);
            Assert.IsTrue(s.TotalEarned.Equals(BigNum.Zero));
        }

        [Test]
        public void Claim_Gives_Clovers()
        {
            s.TotalTaps = 5;
            Quests.TryClaim(dailyTaps, defs, s, Modifiers.None, out _);
            Assert.AreEqual(1, s.Clovers);
        }

        [Test]
        public void Daily_Counts_From_The_Start_Of_The_Day()
        {
            s.TotalTaps = 40;
            Quests.RollDaily(quests, defs, s, 101, Modifiers.None);
            Assert.AreEqual(0, Quests.Progress(dailyTaps, defs, s));
            s.TotalTaps = 44;
            Assert.AreEqual(4, Quests.Progress(dailyTaps, defs, s));
            Assert.IsFalse(Quests.IsComplete(dailyTaps, defs, s));
        }

        [Test]
        public void New_Day_Brings_Dailies_Back_But_Not_Achievements()
        {
            s.Levels = new[] { 1, 1, 0 };
            s.TotalTaps = 5;
            Quests.TryClaim(dailyTaps, defs, s, Modifiers.None, out _);
            Quests.TryClaim(open2, defs, s, Modifiers.None, out _);

            Assert.IsFalse(Quests.RollDaily(quests, defs, s, 100, Modifiers.None), "same day");
            Assert.IsTrue(Quests.IsClaimed(dailyTaps, s));

            Assert.IsTrue(Quests.RollDaily(quests, defs, s, 101, Modifiers.None));
            Assert.IsFalse(Quests.IsClaimed(dailyTaps, s));
            Assert.IsTrue(Quests.IsClaimed(open2, s));
        }

        [Test]
        public void Clock_Going_Backwards_Starts_A_Fresh_Day_Without_Crashing()
        {
            s.TotalTaps = 5;
            Quests.TryClaim(dailyTaps, defs, s, Modifiers.None, out _);
            Assert.IsTrue(Quests.RollDaily(quests, defs, s, 90, Modifiers.None));
            Assert.AreEqual(0, Quests.Progress(dailyTaps, defs, s));
        }

        [Test]
        public void Daily_Carrot_Goal_Follows_Income_With_A_Floor()
        {
            Assert.AreEqual(600 / 1.5, Quests.TargetOf(dailyCarrots, s), 1e-6); // 0.67/s × 600 s = 400
            var tiny = GameState.NewGame(new List<StationDef>());
            Quests.RollDaily(quests, new List<StationDef>(), tiny, 1, Modifiers.None);
            Assert.AreEqual(100, Quests.TargetOf(dailyCarrots, tiny), 1e-9);
        }

        [Test]
        public void Daily_Carrot_Quest_Completes_From_Play()
        {
            Simulation.Tick(defs, s, 1.5 * 399);
            Assert.IsFalse(Quests.IsComplete(dailyCarrots, defs, s));
            Simulation.Tick(defs, s, 1.5);
            Assert.IsTrue(Quests.IsComplete(dailyCarrots, defs, s));
        }

        [Test]
        public void ClaimableCount_Counts_Only_Ready_Ones()
        {
            Assert.AreEqual(0, Quests.ClaimableCount(quests, defs, s));
            s.Levels = new[] { 10, 1, 0 };
            Assert.AreEqual(2, Quests.ClaimableCount(quests, defs, s));
            Quests.TryClaim(open2, defs, s, Modifiers.None, out _);
            Assert.AreEqual(1, Quests.ClaimableCount(quests, defs, s));
        }
    }
}

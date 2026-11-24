using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class WelcomeBackTests
    {
        const long D = 20_600L;

        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            Rabbits.GiftFirst(s, "Bông");
            Tutorial.Skip(s);
            s.Newcomer.Closed = true;
        }

        WelcomeBackGift Open(long day) => WelcomeBack.Check(s, day);

        void PlayedTwoDays()
        {
            Assert.IsFalse(Open(D).Ok);
            Assert.IsFalse(Open(D + 1).Ok);
        }

        [Test]
        public void The_Numbers_Are_The_Issues_Defaults()
        {
            Assert.AreEqual(3, WelcomeBack.MinAwayDays);
            Assert.AreEqual(2, WelcomeBack.MinDaysPlayed);
            Assert.AreEqual(30, WelcomeBack.DaysBetween);
            Assert.AreEqual(2, WelcomeBack.GiftJars);
            Assert.AreEqual(3, WelcomeBack.GiftClovers);
            Assert.AreEqual(5, WelcomeBack.GiftGolden);
            Assert.AreEqual(14, WelcomeBack.BigAwayDays);
            Assert.AreEqual(2, WelcomeBack.BigAwayTimes);
            Assert.AreEqual(7, WelcomeBack.Days);
            Assert.AreEqual(new[] { 1, 0, 0, 1, 0, 0, 0 }, WelcomeBack.DayJars);
            Assert.AreEqual(new[] { 0, 2, 0, 0, 2, 0, 0 }, WelcomeBack.DayClovers);
            Assert.AreEqual(new[] { 0, 0, 3, 0, 0, 5, 0 }, WelcomeBack.DayGolden);
            Assert.AreEqual(new[] { 0, 0, 0, 0, 0, 0, 1 }, WelcomeBack.DayPurple);
            Assert.AreEqual(3, WelcomeBack.PurpleTier, "Purple, Tím, is tier index 3 in code");
            Assert.AreEqual(2, WelcomeBack.QuestTimes);
        }

        [Test]
        public void Two_Days_Away_Gives_Nothing()
        {
            PlayedTwoDays();
            WelcomeBackGift gift = Open(D + 3);
            Assert.IsFalse(gift.Ok);
            Assert.IsFalse(WelcomeBack.Active(s, D + 3));
            Assert.AreEqual(0, s.GoldenCarrots);
        }

        [Test]
        public void Three_Days_Away_Starts_It_Once_With_The_Three_Instant_Gifts()
        {
            PlayedTwoDays();
            WelcomeBackGift gift = Open(D + 4);
            Assert.IsTrue(gift.Ok);
            Assert.AreEqual(3, gift.AwayDays);
            Assert.AreEqual(2, gift.Jars);
            Assert.AreEqual(3, gift.Clovers);
            Assert.AreEqual(5, gift.Golden);
            Assert.AreEqual(2, s.MoonJars);
            Assert.AreEqual(3, s.Clovers);
            Assert.AreEqual(5, s.GoldenCarrots);
            Assert.IsTrue(WelcomeBack.Active(s, D + 4));
            Assert.IsTrue(WelcomeBack.CardWaiting(s));

            Assert.IsFalse(Open(D + 4).Ok, "opening again the same day");
            Assert.IsFalse(Open(D + 5).Ok, "nor the next");
            Assert.AreEqual(5, s.GoldenCarrots);
        }

        [Test]
        public void A_Player_Who_Played_One_Day_Only_Is_Not_Welcomed_Back()
        {
            Assert.IsFalse(Open(D).Ok);
            Assert.IsFalse(Open(D + 6).Ok);
        }

        [Test]
        public void Not_During_The_Guidance()
        {
            s.TutorialStep = 0;
            PlayedTwoDays();
            Assert.IsFalse(Open(D + 5).Ok);
        }

        [Test]
        public void Not_A_Second_Time_Within_Thirty_Days()
        {
            PlayedTwoDays();
            Assert.IsTrue(Open(D + 4).Ok);
            Assert.IsFalse(Open(D + 20).Ok, "sixteen days away, but only sixteen days after the first");
            Assert.IsFalse(Open(D + 33).Ok, "thirteen days away, twenty-nine after the first");
            WelcomeBackGift again = Open(D + 37);
            Assert.IsTrue(again.Ok, "thirty-three days after the first and four away");
            Assert.AreEqual(4, again.AwayDays);
        }

        [Test]
        public void Moving_The_Clock_Forward_And_Back_Never_Makes_Two()
        {
            PlayedTwoDays();
            Assert.IsTrue(Open(D + 11).Ok);
            Assert.IsFalse(Open(D + 2).Ok, "the clock set back");
            Assert.IsFalse(Open(D + 1).Ok);
            Assert.IsFalse(Open(D + 14).Ok, "forward again past three days from the real day");
            Assert.IsFalse(Open(D + 2).Ok);
            Assert.AreEqual(5, s.GoldenCarrots, "one set of gifts");
            Assert.AreEqual(D + 11, s.WelcomeBack.StartDay);
        }

        [Test]
        public void Fourteen_Days_Away_Doubles_The_Instant_Gifts()
        {
            PlayedTwoDays();
            WelcomeBackGift gift = Open(D + 14);
            Assert.IsTrue(gift.Ok);
            Assert.AreEqual(13, gift.AwayDays);
            Assert.AreEqual(5, gift.Golden, "thirteen days is not yet fourteen");

            SetUp();
            PlayedTwoDays();
            gift = Open(D + 15);
            Assert.AreEqual(14, gift.AwayDays);
            Assert.AreEqual(4, gift.Jars);
            Assert.AreEqual(6, gift.Clovers);
            Assert.AreEqual(10, gift.Golden);
        }

        [Test]
        public void Jars_Stop_At_The_Shelf_Cap()
        {
            s.MoonJars = MoonJars.Cap - 1;
            PlayedTwoDays();
            WelcomeBackGift gift = Open(D + 4);
            Assert.AreEqual(1, gift.Jars);
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
        }

        [Test]
        public void A_Player_Still_In_The_First_Week_Gets_The_First_Week_Only()
        {
            s.Newcomer.Closed = false;
            PlayedTwoDays();
            WelcomeBackGift gift = Open(D + 5);
            Assert.IsTrue(Newcomer.ShowPill(s), "the first-week pill is up");
            Assert.IsFalse(gift.Ok, "the first week wins");
            Assert.IsFalse(WelcomeBack.Active(s, D + 5));
            Assert.IsFalse(Open(D + 9).Ok, "and nothing comes later in the first week either");
            Assert.AreEqual(0, s.GoldenCarrots);

            Assert.IsFalse(Open(D + 13).Ok);
            WelcomeBackGift after = Open(D + 17);
            Assert.IsFalse(Newcomer.ShowPill(s), "past day fourteen the first week is over");
            Assert.IsTrue(after.Ok, "a player away after the first week is welcomed back");
            Assert.IsFalse(Newcomer.ShowPill(s) && WelcomeBack.Active(s, D + 17), "never both at once");
        }

        [Test]
        public void A_Newcomer_Who_Claimed_The_Whole_Week_Can_Be_Welcomed_Back()
        {
            s.Newcomer.Closed = false;
            s.Newcomer.Claimed = (1 << Newcomer.Tasks) - 1;
            PlayedTwoDays();
            Assert.IsTrue(Open(D + 5).Ok);
        }

        [Test]
        public void Each_Check_In_Is_Claimed_Once_A_Day_And_Day_Seven_Brings_A_Purple_Rabbit_Of_An_Unmet_Kind()
        {
            PlayedTwoDays();
            long start = D + 4;
            Assert.IsTrue(Open(start).Ok);
            int jars = s.MoonJars, clovers = s.Clovers, golden = s.GoldenCarrots, rabbits = s.Rabbits.Count;
            Assert.AreEqual(1, WelcomeBack.PillDay(s, start));
            Assert.IsTrue(WelcomeBack.CanCheckIn(s, start));
            WelcomeBackGift first = WelcomeBack.CheckIn(s, start, "Tím", 0d);
            Assert.IsTrue(first.Ok);
            Assert.AreEqual(1, first.Day);
            Assert.AreEqual(1, first.Jars);
            Assert.IsFalse(WelcomeBack.CheckIn(s, start, "Tím", 0d).Ok, "once a day");
            Assert.AreEqual(1, WelcomeBack.PillDay(s, start));

            for (int day = 2; day <= WelcomeBack.Days; day++)
            {
                long today = start + day - 1;
                Open(today);
                Assert.AreEqual(day, WelcomeBack.PillDay(s, today));
                WelcomeBackGift gift = WelcomeBack.CheckIn(s, today, "Tím", 0d);
                Assert.IsTrue(gift.Ok, "day " + day);
                Assert.AreEqual(day, gift.Day);
                Assert.IsFalse(WelcomeBack.CanCheckIn(s, today));
            }
            Assert.AreEqual(jars + 2, s.MoonJars);
            Assert.AreEqual(clovers + 4, s.Clovers);
            Assert.AreEqual(golden + 8, s.GoldenCarrots);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Rabbit purple = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual(WelcomeBack.PurpleTier, purple.Tier);
            Assert.IsTrue(purple.FirstOfKind, "a kind the player had not met");

            long last = start + WelcomeBack.Days - 1;
            Assert.IsTrue(WelcomeBack.Active(s, last), "the pill stays up on the seventh day");
            Assert.AreEqual(7, WelcomeBack.PillDay(s, last));
            Assert.IsFalse(WelcomeBack.Active(s, last + 1), "and is gone the day after");
            Assert.IsFalse(WelcomeBack.CheckIn(s, last + 1, "Tím", 0d).Ok);
        }

        [Test]
        public void A_Missed_Day_Keeps_The_Next_Gift_Waiting()
        {
            PlayedTwoDays();
            long start = D + 4;
            Open(start);
            WelcomeBack.CheckIn(s, start, "Tím", 0d);
            Open(start + 2);
            Assert.AreEqual(2, WelcomeBack.PillDay(s, start + 2));
            Assert.AreEqual(2, WelcomeBack.CheckIn(s, start + 2, "Tím", 0d).Day);
        }

        [Test]
        public void The_Check_In_Ends_After_The_Window_Even_If_Not_Finished()
        {
            PlayedTwoDays();
            long start = D + 4;
            Open(start);
            Assert.IsTrue(WelcomeBack.Active(s, start + WelcomeBack.WindowDays - 1));
            Assert.IsFalse(WelcomeBack.Active(s, start + WelcomeBack.WindowDays));
            Assert.IsFalse(WelcomeBack.CanCheckIn(s, start + WelcomeBack.WindowDays));
        }

        [Test]
        public void Daily_Quest_Rewards_Are_Doubled_While_The_Pill_Is_Up()
        {
            var daily = new QuestDef { Id = "d", Metric = QuestMetric.Taps, Target = 1, Daily = true, RewardCarrots = 30, RewardClovers = 1 };
            var lasting = new QuestDef { Id = "l", Metric = QuestMetric.Taps, Target = 1, RewardCarrots = 30, RewardClovers = 1 };
            var quests = new List<QuestDef> { daily, lasting };
            PlayedTwoDays();
            Assert.IsTrue(Open(D + 4).Ok);
            Quests.RollDaily(quests, defs, s, D + 4, Modifiers.None);
            s.TotalTaps = 5;
            Assert.AreEqual(2, Quests.CloversNow(daily, s));
            Assert.AreEqual(60d, Quests.RewardOf(daily, defs, s, Modifiers.None).ToDouble(), 1e-6);
            Assert.AreEqual(30d, Quests.RewardOf(lasting, defs, s, Modifiers.None).ToDouble(), 1e-6, "only the daily quests");
            int clovers = s.Clovers;
            Assert.IsTrue(Quests.TryClaim(daily, defs, s, Modifiers.None, out BigNum paid));
            Assert.AreEqual(60d, paid.ToDouble(), 1e-6);
            Assert.AreEqual(clovers + 2, s.Clovers);

            Open(D + 4 + WelcomeBack.WindowDays);
            Quests.RollDaily(quests, defs, s, D + 4 + WelcomeBack.WindowDays, Modifiers.None);
            Assert.AreEqual(1, Quests.CloversNow(daily, s), "back to once when the pill is gone");
            Assert.AreEqual(30d, Quests.RewardOf(daily, defs, s, Modifiers.None).ToDouble(), 1e-6);
        }

        [Test]
        public void The_Card_Waits_Until_It_Is_Seen()
        {
            Assert.IsFalse(WelcomeBack.CardWaiting(s), "a new garden has no card");
            PlayedTwoDays();
            Open(D + 4);
            Assert.IsTrue(WelcomeBack.CardWaiting(s));
            WelcomeBack.SeeCard(s);
            Assert.IsFalse(WelcomeBack.CardWaiting(s));
        }

        [Test]
        public void The_Most_Loved_Rabbit_Speaks()
        {
            Rabbit first = s.Rabbits[0];
            Rabbit loved = Rabbits.ArriveKind(s, "tho-me", "Mây");
            Rabbits.AddBond(s, loved, Rabbits.PointsFor(4));
            Rabbits.AddBond(s, first, Rabbits.PointsFor(2));
            Assert.AreSame(loved, WelcomeBack.MostLoved(s));
            s.Rabbits.Clear();
            Assert.IsNull(WelcomeBack.MostLoved(s));
        }

        [Test]
        public void The_Record_Survives_A_Save_And_An_Old_Save_Is_Welcomed_Back_From_Its_Last_Day()
        {
            PlayedTwoDays();
            Open(D + 4);
            WelcomeBack.CheckIn(s, D + 4, "Tím", 0d);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.WelcomeBack.SeenDay, back.WelcomeBack.SeenDay);
            Assert.AreEqual(D + 4, back.WelcomeBack.StartDay);
            Assert.AreEqual(3, back.WelcomeBack.AwayDays);
            Assert.AreEqual(1, back.WelcomeBack.CheckIns);
            Assert.AreEqual(D + 4, back.WelcomeBack.CheckInDay);
            Assert.IsTrue(WelcomeBack.CardWaiting(back));
            Assert.AreEqual(14, SaveCodec.CurrentVersion);

            SetUp();
            Seasons.Clamp(s, D);
            Seasons.Clamp(s, D + 1);
            string json = SaveCodec.Encode(defs, s);
            int at = json.IndexOf(",\"welcomeBack\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0, "the record is written");
            string old = json.Substring(0, at) + json.Substring(json.IndexOf('}', at) + 1);
            LoadResult loaded = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            Assert.AreEqual(-1L, loaded.State.WelcomeBack.StartDay);
            Assert.IsFalse(WelcomeBack.CardWaiting(loaded.State));
            loaded.State.Newcomer.Closed = true;
            Assert.IsTrue(WelcomeBack.Check(loaded.State, D + 5).Ok, "a save from before the welcome back counts from its last day");

            string bad = json.Replace("\"checkIns\":0", "\"checkIns\":-4");
            Assert.AreEqual(0, SaveCodec.Decode(defs, bad).State.WelcomeBack.CheckIns);
        }

        [Test]
        public void The_Welcome_Back_Is_Logged_With_The_Days_Away()
        {
            var log = new MemoryAnalytics();
            log.LogWelcomeBack(default);
            Assert.AreEqual(0, log.Count(AnalyticsEvents.WelcomeBack), "nothing when it did not start");
            PlayedTwoDays();
            log.LogWelcomeBack(Open(D + 9));
            Assert.AreEqual(1, log.Count(AnalyticsEvents.WelcomeBack));
            Assert.AreEqual(8, log.Last(AnalyticsEvents.WelcomeBack)[AnalyticsEvents.DaysAway].Number);
            CollectionAssert.Contains(AnalyticsEvents.All, AnalyticsEvents.WelcomeBack);
        }
    }
}

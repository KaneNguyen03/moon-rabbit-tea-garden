using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class NewcomerTests
    {
        const long FirstDay = 20600;

        List<StationDef> defs;
        GameState s;
        Modifiers mods;

        [SetUp]
        public void SetUp()
        {
            defs = new List<StationDef>();
            for (int i = 0; i < 8; i++)
            {
                StationDef d = Fixture.Tea();
                d.Id = i == 0 ? Newcomer.TeapotId : "station-" + i;
                d.Trade = i == 0 ? RabbitTrade.Brewer : RabbitTrade.None;
                defs.Add(d);
            }
            s = GameState.NewGame(defs, 1);
            Rabbits.GiftFirst(s, "Bông");
            s.TutorialStep = Tutorial.Steps;
            mods = Modifiers.None;
            Seasons.Clamp(s, FirstDay);
        }

        void PlayTo(int day) => Seasons.Clamp(s, FirstDay + day - 1);

        void EverythingDone()
        {
            for (int i = 0; i < s.Levels.Length; i++) s.Levels[i] = 30;
            s.Plots = defs.Count;
            s.GardenXp = Garden.XpForLevel(12);
            s.UpgradeLevels[0] = 3;
            s.CloverCalls = 2;
            s.OrdersDelivered = 4;
            s.FishCaught = 3;
            s.FishFed = 1;
            s.BestFish = 3;
            s.PoundingGames = 1;
            s.Leaves.Games = 1;
            s.Fireflies.Games = 1;
            s.PhotosTaken = 1;
            s.JarsOpened = 1;
            s.Pets = 3;
            s.LayoutMoves = 1;
            s.Boosts = 3;
            s.DailyGifts = 1;
            s.Merges = 1;
            s.DailyQuestsToday = 4;
            s.LevelsBought = 100;
            s.RestSpots = 6;
            s.Grid.Segments = 6;
            string[] kinds = { "tho-bong", "tho-me", "tho-cup", "tho-matcha", "tho-suong", "tho-o-long", "tho-mat-ong", "tho-sao", "tho-dau-do", "tho-ca-phe", "tho-tro", "tho-cat" };
            foreach (string kind in kinds) s.SeenKinds.Add(kind);
            s.Rabbits[0].Station = 0;
            for (int i = 0; i < 3; i++) Rabbits.ArriveKind(s, "tho-me", "Mè");
        }

        void ClaimDay(int day)
        {
            for (int slot = 0; slot < Newcomer.TasksPerDay; slot++)
                Assert.IsTrue(Newcomer.Claim(defs, s, mods, day, slot, "Tím", 0d).Ok, "day " + day + " task " + slot);
        }

        [Test]
        public void The_Table_Has_Seven_Days_Of_Four_Tasks()
        {
            Assert.AreEqual(28, Newcomer.All.Count);
            var bits = new HashSet<int>();
            foreach (NewcomerTask task in Newcomer.All)
            {
                Assert.AreSame(task, Newcomer.At(task.Day, task.Slot));
                Assert.IsTrue(bits.Add(task.Bit), task.Id);
                Assert.Greater(task.Target, 0d, task.Id);
            }
        }

        [Test]
        public void A_Day_Opens_Only_When_The_Days_Played_Reach_It()
        {
            EverythingDone();
            for (int day = 1; day <= Newcomer.Days; day++)
            {
                for (int later = day + 1; later <= Newcomer.Days; later++)
                {
                    Assert.IsFalse(Newcomer.DayOpen(s, later), "day " + later + " on day " + day);
                    Assert.IsFalse(Newcomer.CanClaim(Newcomer.At(later, 0), defs, s));
                    Assert.IsFalse(Newcomer.Claim(defs, s, mods, later, 0, "Tím", 0d).Ok);
                    Assert.AreEqual(later - day, Newcomer.DaysUntil(s, later));
                }
                Assert.AreEqual(day - 1, Seasons.DaysPlayed(s));
                ClaimDay(day);
                PlayTo(day + 1);
            }
            Assert.IsTrue(Newcomer.AllClaimed(s));
        }

        [Test]
        public void Turning_The_Clock_Back_Opens_No_Day()
        {
            EverythingDone();
            PlayTo(3);
            Seasons.Clamp(s, FirstDay - 30);
            Seasons.Clamp(s, FirstDay + 1);
            Assert.AreEqual(2, Seasons.DaysPlayed(s));
            Assert.IsTrue(Newcomer.DayOpen(s, 3));
            Assert.IsFalse(Newcomer.DayOpen(s, 4), "a clock set back and forward again opens nothing new");
            Assert.IsFalse(Newcomer.Claim(defs, s, mods, 4, 0, "Tím", 0d).Ok);
        }

        [Test]
        public void Each_Task_And_Each_Day_Pays_Once()
        {
            EverythingDone();
            s.Coins = BigNum.Zero;
            NewcomerClaim first = Newcomer.Claim(defs, s, mods, 1, 0, "Tím", 0d);
            Assert.IsTrue(first.Ok);
            Assert.AreEqual(Newcomer.TaskClovers[0], first.Clovers);
            Assert.Greater(first.Carrots.ToDouble(), 0d, "two minutes of income");
            int clovers = s.Clovers;
            BigNum coins = s.Coins;
            Assert.IsFalse(Newcomer.Claim(defs, s, mods, 1, 0, "Tím", 0d).Ok, "claimed already");
            Assert.AreEqual(clovers, s.Clovers);
            Assert.AreEqual(coins.ToDouble(), s.Coins.ToDouble());

            int jars = s.MoonJars;
            Assert.IsFalse(Newcomer.Claim(defs, s, mods, 1, 1, "Tím", 0d).DayPaid);
            Assert.IsFalse(Newcomer.Claim(defs, s, mods, 1, 2, "Tím", 0d).DayPaid);
            NewcomerClaim last = Newcomer.Claim(defs, s, mods, 1, 3, "Tím", 0d);
            Assert.IsTrue(last.DayPaid, "the fourth task of a day brings the day's gift");
            Assert.AreEqual(jars + Newcomer.DayJars[0], s.MoonJars);
            Assert.IsTrue(Newcomer.DayComplete(s, 1));
            Assert.IsTrue(Newcomer.DayPaid(s, 1));
            Assert.AreEqual(0, Newcomer.Claimable(defs, s), "nothing left on day one, the rest not open");
        }

        [Test]
        public void A_Task_Not_Done_Cannot_Be_Claimed()
        {
            Assert.IsFalse(Newcomer.CanClaim(Newcomer.At(1, 0), defs, s));
            Assert.IsFalse(Newcomer.Claim(defs, s, mods, 1, 0, "Tím", 0d).Ok);
            s.Levels[0] = 25;
            Assert.IsTrue(Newcomer.Claim(defs, s, mods, 1, 0, "Tím", 0d).Ok);
        }

        [Test]
        public void Day_Seven_Gives_One_Purple_Rabbit_Of_A_Kind_Not_Yet_Met()
        {
            EverythingDone();
            PlayTo(7);
            for (int day = 1; day <= 6; day++) ClaimDay(day);
            int rabbits = s.Rabbits.Count, golden = s.GoldenCarrots;
            Assert.IsFalse(s.SeenKinds.Contains("tho-oai-huong"));
            for (int slot = 0; slot < 3; slot++) Assert.IsNull(Newcomer.Claim(defs, s, mods, 7, slot, "Tím", 0.99).Rabbit);
            NewcomerClaim last = Newcomer.Claim(defs, s, mods, 7, 3, "Tím", 0.99);
            Assert.IsTrue(last.DayPaid);
            Assert.IsNotNull(last.Rabbit);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count, "exactly one rabbit");
            Assert.AreEqual(Newcomer.PurpleTier, last.Rabbit.Tier);
            Assert.IsTrue(last.Rabbit.FirstOfKind, "a kind not met before");
            Assert.AreEqual(golden + Newcomer.DayGolden[6], s.GoldenCarrots);
            Assert.IsTrue(Newcomer.AllClaimed(s));
            Assert.IsFalse(Newcomer.ShowPill(s), "all claimed: the pill goes");
        }

        [Test]
        public void The_Purple_Gift_Picks_An_Unseen_Kind_Then_Any_Purple_Kind()
        {
            IReadOnlyList<RabbitKind> purple = RabbitKinds.OfTier(Newcomer.PurpleTier);
            Assert.GreaterOrEqual(purple.Count, 2);
            s.SeenKinds.Add(purple[0].Id);
            Assert.AreEqual(purple[1].Id, Newcomer.PurpleKind(s, 0d));
            Assert.AreEqual(purple[purple.Count - 1].Id, Newcomer.PurpleKind(s, 0.99));
            foreach (RabbitKind k in purple) s.SeenKinds.Add(k.Id);
            Assert.AreEqual(purple[0].Id, Newcomer.PurpleKind(s, 0d));
            Assert.AreEqual(purple[purple.Count - 1].Id, Newcomer.PurpleKind(s, 0.99));
            foreach (RabbitKind k in purple) Assert.AreEqual(Newcomer.PurpleTier, k.Tier);
        }

        [Test]
        public void A_Task_Whose_Feature_Is_Shut_Becomes_Raise_Stations_Thirty_Times()
        {
            NewcomerTask pounding = Newcomer.At(2, 1);
            Assert.Greater(pounding.NeedLevel, Garden.Level(s));
            Assert.IsTrue(Newcomer.IsFallback(pounding, s));
            Assert.AreEqual(QuestMetric.LevelsBought, Newcomer.Shown(pounding, s).Quest.Metric);
            Assert.AreEqual(Newcomer.FallbackLevels, Newcomer.TargetOf(pounding, s));
            PlayTo(2);
            s.LevelsBought = 29;
            Assert.IsFalse(Newcomer.CanClaim(pounding, defs, s));
            s.LevelsBought = 30;
            Assert.IsTrue(Newcomer.Claim(defs, s, mods, 2, 1, "Tím", 0d).Ok);
            s.GardenXp = Garden.XpForLevel(pounding.NeedLevel);
            Assert.IsFalse(Newcomer.IsFallback(pounding, s), "once open, the task is the game itself");
            Assert.IsTrue(Newcomer.IsClaimed(pounding, s), "and what was claimed stays claimed");
        }

        [Test]
        public void The_Pill_Shows_From_The_End_Of_The_Guidance_Until_Day_Fourteen()
        {
            s.TutorialStep = 2;
            Assert.IsFalse(Newcomer.ShowPill(s), "not while the guidance runs");
            s.TutorialStep = Tutorial.Steps;
            Assert.IsTrue(Newcomer.ShowPill(s));
            Assert.AreEqual(1, Newcomer.PillDay(s));
            PlayTo(9);
            Assert.IsTrue(Newcomer.ShowPill(s), "a week behind still has its tasks");
            Assert.AreEqual(7, Newcomer.PillDay(s));
            PlayTo(15);
            Assert.IsFalse(Newcomer.ShowPill(s), "day fourteen over: gone");
        }

        [Test]
        public void After_Day_Fourteen_The_Tasks_Done_But_Not_Claimed_Come_As_One_Letter()
        {
            EverythingDone();
            PlayTo(7);
            for (int day = 1; day <= 5; day++) ClaimDay(day);
            Newcomer.Claim(defs, s, mods, 6, 0, "Tím", 0d);
            PlayTo(14);
            Assert.IsNull(Newcomer.Expire(defs, s, FirstDay + 13), "day fourteen still counts");
            PlayTo(15);
            Letter letter = Newcomer.Expire(defs, s, FirstDay + 14);
            Assert.IsNotNull(letter);
            Assert.AreEqual(LetterKind.Newcomer, letter.Kind);
            Assert.AreEqual(3 * Newcomer.TaskGolden[5] + Newcomer.DayGolden[5] + Newcomer.DayGolden[6], letter.Golden);
            Assert.AreEqual(3 * Newcomer.TaskClovers[5] + 4 * Newcomer.TaskClovers[6], letter.Clovers);
            Assert.AreEqual(Newcomer.DayJars[5] + Newcomer.DayJars[6], letter.Jars);
            Assert.AreEqual(1, letter.Purple);
            Assert.IsTrue(s.Newcomer.Closed);
            Assert.IsFalse(Newcomer.ShowPill(s));
            Assert.AreEqual(0, Newcomer.Claimable(defs, s));
            Assert.IsNull(Newcomer.Expire(defs, s, FirstDay + 15), "one letter only");

            int rabbits = s.Rabbits.Count, golden = s.GoldenCarrots;
            Letter paid = Mailbox.Claim(s, letter.Id, FirstDay + 15, "Tím", 0d, out Rabbit arrived);
            Assert.IsNotNull(paid);
            Assert.IsNotNull(arrived);
            Assert.AreEqual(Newcomer.PurpleTier, arrived.Tier);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Assert.AreEqual(golden + letter.Golden, s.GoldenCarrots);
        }

        [Test]
        public void Nothing_Done_And_Nothing_Claimed_Sends_No_Letter()
        {
            s.Rabbits[0].Station = -1;
            PlayTo(20);
            Assert.IsNull(Newcomer.Expire(defs, s, FirstDay + 19));
            Assert.IsTrue(s.Newcomer.Closed);
            Assert.AreEqual(0, s.Letters.Count);
        }

        [Test]
        public void An_Old_Save_Past_Day_Seven_Has_No_Pill_And_One_On_Day_Three_Has_Days_One_And_Two_Open()
        {
            PlayTo(9);
            string old = StripNewcomer(SaveCodec.Encode(defs, s));
            GameState late = SaveCodec.Decode(defs, old).State;
            Assert.IsTrue(late.Newcomer.Closed);
            Assert.IsFalse(Newcomer.ShowPill(late), "a save from before the first week came in, past day seven");
            late.MaxDaySeen += 10;
            Assert.IsNull(Newcomer.Expire(defs, late, late.MaxDaySeen), "and no letter later either");

            SetUp();
            PlayTo(3);
            GameState mid = SaveCodec.Decode(defs, StripNewcomer(SaveCodec.Encode(defs, s))).State;
            Assert.IsFalse(mid.Newcomer.Closed);
            Assert.IsTrue(Newcomer.ShowPill(mid));
            Assert.AreEqual(3, Newcomer.PillDay(mid));
            Assert.IsTrue(Newcomer.DayOpen(mid, 1));
            Assert.IsTrue(Newcomer.DayOpen(mid, 2));
            Assert.IsTrue(Newcomer.DayOpen(mid, 3));
            Assert.IsFalse(Newcomer.DayOpen(mid, 4));
        }

        static string StripNewcomer(string json)
        {
            int at = json.IndexOf(",\"tally\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0);
            return json.Substring(0, at) + "}";
        }

        [Test]
        public void The_Week_And_Its_Counts_Survive_A_Save()
        {
            EverythingDone();
            PlayTo(2);
            ClaimDay(1);
            Newcomer.Claim(defs, s, mods, 2, 0, "Tím", 0d);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.Newcomer.Claimed, back.Newcomer.Claimed);
            Assert.AreEqual(s.Newcomer.DaysPaid, back.Newcomer.DaysPaid);
            Assert.AreEqual(s.Newcomer.Closed, back.Newcomer.Closed);
            Assert.AreEqual(s.Pets, back.Pets);
            Assert.AreEqual(s.Boosts, back.Boosts);
            Assert.AreEqual(s.LayoutMoves, back.LayoutMoves);
            Assert.AreEqual(s.Merges, back.Merges);
            Assert.AreEqual(s.CloverCalls, back.CloverCalls);
            Assert.AreEqual(s.JarsOpened, back.JarsOpened);
            Assert.AreEqual(s.DailyGifts, back.DailyGifts);
            Assert.AreEqual(s.DailyQuestsToday, back.DailyQuestsToday);
            Assert.AreEqual(s.BestFish, back.BestFish);

            s.Letters.Add(new Letter { Id = "newcomer", Kind = LetterKind.Newcomer, Day = FirstDay, GiftGolden = 7, GiftClovers = 3, GiftJars = 2, GiftPurple = 1 });
            Letter letter = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.Letters.Find(l => l.Id == "newcomer");
            Assert.IsNotNull(letter);
            Assert.AreEqual(7, letter.Golden);
            Assert.AreEqual(3, letter.Clovers);
            Assert.AreEqual(2, letter.Jars);
            Assert.AreEqual(1, letter.Purple);
        }

        [Test]
        public void The_Actions_The_Tasks_Count_Are_Counted()
        {
            Rabbit r = s.Rabbits[0];
            Rabbits.Pet(s, r, FirstDay);
            Rabbits.Pet(s, r, FirstDay);
            Assert.AreEqual(1, s.Pets, "one pet a rabbit a day");
            s.Energy = Energy.Max;
            Assert.IsTrue(Energy.TryBoost(s));
            Assert.AreEqual(1, s.Boosts);
            s.Clovers = Rabbits.SummonCost;
            Assert.IsNotNull(Rabbits.Summon(s, 0d, "A"));
            s.GoldenCarrots = Shop.GoldenSummonCost;
            Assert.IsNotNull(Shop.SummonGolden(s, 0d, "B"));
            Assert.AreEqual(2d, Quests.Progress(new QuestDef { Metric = QuestMetric.RabbitsCalled }, defs, s));
            Assert.AreEqual(1d, Quests.Progress(new QuestDef { Metric = QuestMetric.MergesOrGoldenCalls }, defs, s));
            s.MoonJars = 1;
            MoonJars.Open(defs, s, mods);
            Assert.AreEqual(1, s.JarsOpened);
            Assert.Greater(Shop.ClaimDailyGift(s, FirstDay), 0);
            Assert.AreEqual(1, s.DailyGifts);
            var daily = new QuestDef { Id = "d", Metric = QuestMetric.Taps, Target = 1, Daily = true };
            s.TotalTaps = 5;
            Assert.IsTrue(Quests.TryClaim(daily, defs, s, mods, out _));
            Assert.AreEqual(1, s.DailyQuestsToday);
            Quests.RollDaily(new List<QuestDef> { daily }, defs, s, s.DailyDay + 1, mods);
            Assert.AreEqual(0, s.DailyQuestsToday, "a new day starts the count again");
            s.Rabbits[0].Station = 0;
            Assert.AreEqual(1d, Quests.Progress(new QuestDef { Metric = QuestMetric.RabbitsInTrade }, defs, s));
        }

        [Test]
        public void Claims_Are_Logged_As_Newcomer_Task_And_Newcomer_Day()
        {
            EverythingDone();
            var log = new MemoryAnalytics();
            for (int slot = 0; slot < Newcomer.TasksPerDay; slot++)
                log.LogNewcomer(Newcomer.Claim(defs, s, mods, 1, slot, "Tím", 0d), false);
            log.LogNewcomer(Newcomer.Claim(defs, s, mods, 1, 0, "Tím", 0d), false);
            Assert.AreEqual(4, log.Count(AnalyticsEvents.NewcomerTask), "a refused claim logs nothing");
            Assert.AreEqual(1, log.Count(AnalyticsEvents.NewcomerDay));
            MemoryAnalytics.Event task = log.All(AnalyticsEvents.NewcomerTask)[2];
            Assert.AreEqual(1, task[AnalyticsEvents.Day].Number);
            Assert.AreEqual(2, task[AnalyticsEvents.Slot].Number);
            Assert.AreEqual("call-2", task[AnalyticsEvents.Task].Text);
            Assert.AreEqual(0, task[AnalyticsEvents.Fallback].Number);
            Assert.AreEqual(1, log.Last(AnalyticsEvents.NewcomerDay)[AnalyticsEvents.Day].Number);
        }
    }
}

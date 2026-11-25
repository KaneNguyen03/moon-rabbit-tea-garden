using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class AnalyticsFunnelTests
    {
        List<StationDef> defs;
        GameState s;
        MemoryAnalytics mem;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            Rabbits.GiftFirst(s, "Bông");
            Tutorial.Skip(s);
            mem = new MemoryAnalytics();
        }

        static readonly Regex Snake = new Regex("^[a-z][a-z0-9_]*$");

        MemoryAnalytics.Event Only(string name)
        {
            Assert.AreEqual(1, mem.Count(name), name + " once");
            MemoryAnalytics.Event e = mem.Last(name);
            Assert.LessOrEqual(e.Params.Length, 10, name + " keeps to ten parameters");
            Assert.LessOrEqual(name.Length, 40, name);
            foreach (AnalyticsParam p in e.Params)
            {
                StringAssert.IsMatch(Snake.ToString(), p.Name);
                Assert.LessOrEqual(p.Name.Length, 40, p.Name);
                if (p.Type == AnalyticsParam.Kind.Text) Assert.LessOrEqual(p.Text.Length, 100, p.Name);
            }
            CollectionAssert.Contains(AnalyticsEvents.All, name, "listed in All");
            return e;
        }

        [Test]
        public void The_Round_Two_Events_Are_In_The_List_Under_Their_Issue_Names()
        {
            var names = new[]
            {
                "intro_shown", "intro_try", "intro_later", "tank_release", "tank_milestone", "tank_full", "recipe_leaf", "dish_unlocked", "dish_level",
                "fair_start", "fair_coins", "fair_trade", "fair_rabbit", "daily_set_done", "week_chest", "achievement_tier", "feature_first_use",
            };
            foreach (string name in names) CollectionAssert.Contains(AnalyticsEvents.All, name);
        }

        [Test]
        public void An_Intro_Card_Logs_Shown_Try_And_Later_With_Its_Id_And_Where_From()
        {
            mem.LogIntroShown(Intros.Fishing);
            mem.LogIntroTry(Intros.Fishing, AnalyticsEvents.FromCard);
            mem.LogIntroLater(Intros.Pounding, AnalyticsEvents.FromCovered);
            Assert.AreEqual(Intros.Fishing, Only(AnalyticsEvents.IntroShown)[AnalyticsEvents.Id].Text);
            MemoryAnalytics.Event tried = Only(AnalyticsEvents.IntroTry);
            Assert.AreEqual(Intros.Fishing, tried[AnalyticsEvents.Id].Text);
            Assert.AreEqual("card", tried[AnalyticsEvents.From].Text);
            MemoryAnalytics.Event later = Only(AnalyticsEvents.IntroLater);
            Assert.AreEqual(Intros.Pounding, later[AnalyticsEvents.Id].Text);
            Assert.AreEqual("covered", later[AnalyticsEvents.From].Text);
            Assert.AreEqual("dot", AnalyticsEvents.FromDot);
        }

        [Test]
        public void A_Fish_Released_Logs_Its_Kind_And_Count_A_Milestone_Its_Step_And_The_Sixth_Kind_The_Full_Tank()
        {
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            s.Fish[2] = 6;
            for (int i = 0; i < 4; i++) mem.LogTankRelease(s, Aquarium.Release(s, 2));
            Assert.AreEqual(4, mem.Count(AnalyticsEvents.TankRelease), "one per fish let go");
            Assert.AreEqual(new[] { 1L, 2L, 3L, 4L }, mem.All(AnalyticsEvents.TankRelease).ConvertAll(e => e[AnalyticsEvents.Count].Number).ToArray());
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.TankRelease)[AnalyticsEvents.Kind].Number);
            MemoryAnalytics.Event first = Only(AnalyticsEvents.TankMilestone);
            Assert.AreEqual(1L, first[AnalyticsEvents.Step].Number, "the first fish of a kind is milestone one");
            Assert.AreEqual(2L, first[AnalyticsEvents.Kind].Number);
            mem.LogTankRelease(s, Aquarium.Release(s, 2));
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.TankMilestone));
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.TankMilestone)[AnalyticsEvents.Step].Number, "five of a kind is milestone two");
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.TankFull));

            mem.LogTankRelease(s, Aquarium.Release(s, 0));
            Assert.AreEqual(5, mem.Count(AnalyticsEvents.TankRelease), "an empty basket releases nothing to log");
            for (int k = 0; k < Aquarium.Kinds; k++) if (k != 2) { s.Fish[k] = 1; mem.LogTankRelease(s, Aquarium.Release(s, k)); }
            MemoryAnalytics.Event full = Only(AnalyticsEvents.TankFull);
            Assert.AreEqual(10L, full[AnalyticsEvents.Count].Number, "fish in the tank when the sixth kind came");
            s.Fish[3] = 1;
            mem.LogTankRelease(s, Aquarium.Release(s, 3));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TankFull), "the full tank comes once");
        }

        [Test]
        public void Recipe_Leaves_Log_Their_Count_And_Source_And_Nothing_For_None()
        {
            mem.LogRecipeLeaves(0, AnalyticsEvents.FromOrder);
            mem.LogRecipeLeaves(-1, AnalyticsEvents.FromOrder);
            Assert.AreEqual(0, mem.Events.Count);
            mem.LogRecipeLeaves(Recipes.LeavesPerOrder, AnalyticsEvents.FromOrder);
            MemoryAnalytics.Event e = Only(AnalyticsEvents.RecipeLeaf);
            Assert.AreEqual(2L, e[AnalyticsEvents.Count].Number);
            Assert.AreEqual("order", e[AnalyticsEvents.From].Text);
        }

        [Test]
        public void Raising_A_Dish_Logs_The_Station_Dish_And_New_Level()
        {
            mem.LogDishLevel(defs, 1, 0, 3);
            MemoryAnalytics.Event e = Only(AnalyticsEvents.DishLevel);
            Assert.AreEqual(defs[1].Id, e[AnalyticsEvents.Station].Text);
            Assert.AreEqual(1L, e[AnalyticsEvents.Dish].Number, "dishes count from one");
            Assert.AreEqual(3L, e[AnalyticsEvents.Level].Number);
        }

        [Test]
        public void The_Dish_Watch_Logs_Each_New_Dish_Once_And_Not_The_First_Or_A_Moon_Going_Back()
        {
            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel);
            var watch = new DishWatch();
            watch.Reset(s);
            watch.Note(defs, s, mem);
            Assert.AreEqual(0, mem.Events.Count, "nothing moved");

            s.Levels[1] = Economy.Ranks[0];
            watch.Note(defs, s, mem);
            MemoryAnalytics.Event e = Only(AnalyticsEvents.DishUnlocked);
            Assert.AreEqual(defs[1].Id, e[AnalyticsEvents.Station].Text);
            Assert.AreEqual(2L, e[AnalyticsEvents.Dish].Number, "the second dish");
            Assert.AreEqual(0L, e[AnalyticsEvents.Moon].Number);
            watch.Note(defs, s, mem);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.DishUnlocked), "a second look logs nothing more");

            s.Levels[2] = Economy.Ranks[1];
            watch.Note(defs, s, mem);
            Assert.AreEqual(new[] { 2L, 3L }, mem.All(AnalyticsEvents.DishUnlocked).GetRange(1, 2).ConvertAll(x => x[AnalyticsEvents.Dish].Number).ToArray(),
                "two dishes opened at once, each once");

            s.Levels[1] = 1;
            s.Season = 1;
            watch.Note(defs, s, mem);
            Assert.AreEqual(3, mem.Count(AnalyticsEvents.DishUnlocked), "a new moon takes the dish back: nothing to log");
            s.Levels[1] = Economy.Ranks[0];
            watch.Note(defs, s, mem);
            Assert.AreEqual(4, mem.Count(AnalyticsEvents.DishUnlocked));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.DishUnlocked)[AnalyticsEvents.Moon].Number, "with the moon it happened on");
        }

        static long Day(int month, int day) => GiftCodes.Day(2027, month, day);

        [Test]
        public void A_Fair_Logs_Its_Start_Once_The_Coins_Earned_And_Each_Trade_With_The_Rabbit_Apart()
        {
            long day = Day(3, Fairs.StartDay + 1);
            s.MaxDaySeen = day;
            long before = s.Fair.Start;
            Fairs.Roll(defs, s, Modifiers.None, day);
            mem.LogFairStart(s, before);
            MemoryAnalytics.Event start = Only(AnalyticsEvents.FairStart);
            Assert.AreEqual("hoi-hoa-mo", start[AnalyticsEvents.Fair].Text);
            before = s.Fair.Start;
            Fairs.Roll(defs, s, Modifiers.None, day);
            mem.LogFairStart(s, before);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.FairStart), "the same fair starts once");

            mem.LogFairCoins(0, AnalyticsEvents.FromOrder);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.FairCoins), "no coins, nothing to log");
            int earned = Fairs.Earn(defs, s, Modifiers.None, day, Fairs.OrderCoins);
            mem.LogFairCoins(earned, AnalyticsEvents.FromOrder);
            MemoryAnalytics.Event coins = Only(AnalyticsEvents.FairCoins);
            Assert.AreEqual((long)earned, coins[AnalyticsEvents.Coins].Number);
            Assert.AreEqual("order", coins[AnalyticsEvents.From].Text);

            s.Fair.Coins = 1000;
            mem.LogFairTrade(s, Fairs.Buy(defs, s, Modifiers.None, day, 1, "Mây"));
            MemoryAnalytics.Event trade = Only(AnalyticsEvents.FairTrade);
            Assert.AreEqual("clovers", trade[AnalyticsEvents.Item].Text);
            Assert.AreEqual(30L, trade[AnalyticsEvents.Cost].Number);
            Assert.AreEqual("hoi-hoa-mo", trade[AnalyticsEvents.Fair].Text);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.FairRabbit));

            mem.LogFairTrade(s, Fairs.Buy(defs, s, Modifiers.None, day, 5, "Mây"));
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.FairTrade));
            Assert.AreEqual("rabbit", mem.Last(AnalyticsEvents.FairTrade)[AnalyticsEvents.Item].Text);
            MemoryAnalytics.Event rabbit = Only(AnalyticsEvents.FairRabbit);
            Assert.AreEqual("hoi-hoa-mo", rabbit[AnalyticsEvents.Fair].Text);
            Assert.AreEqual("tho-anh-dao", rabbit[AnalyticsEvents.Kind].Text);

            mem.LogFairTrade(s, Fairs.Buy(defs, s, Modifiers.None, day, 5, "Mây"));
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.FairTrade), "sold out: no trade to log");
        }

        [Test]
        public void The_Last_Daily_Quest_Logs_The_Set_Once_And_A_Chain_Logs_Each_Tier()
        {
            var quests = new List<QuestDef>
            {
                new QuestDef { Id = "d-1", Metric = QuestMetric.Taps, Target = 1, Daily = true, RewardCarrots = 1 },
                new QuestDef { Id = "d-2", Metric = QuestMetric.Taps, Target = 1, Daily = true, RewardCarrots = 1 },
                new QuestDef { Id = "van", Metric = QuestMetric.Taps, Tiers = new[] { new QuestTier { Target = 1 }, new QuestTier { Target = 2 } } },
            };
            s.FirstDaySeen = 20_000;
            s.MaxDaySeen = 20_002;
            Quests.RollDaily(quests, defs, s, s.MaxDaySeen, Modifiers.None);
            s.TotalTaps += 5;

            Claim(quests, 0);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.DailySetDone), "one of two is not the set");
            Claim(quests, 1);
            MemoryAnalytics.Event set = Only(AnalyticsEvents.DailySetDone);
            Assert.AreEqual(3L, set[AnalyticsEvents.Day].Number, "the third day played");
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.AchievementTier), "daily quests are not a chain");

            Claim(quests, 2);
            MemoryAnalytics.Event tier = Only(AnalyticsEvents.AchievementTier);
            Assert.AreEqual("van", tier[AnalyticsEvents.Chain].Text);
            Assert.AreEqual(1L, tier[AnalyticsEvents.Tier].Number);
            Claim(quests, 2);
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.AchievementTier)[AnalyticsEvents.Tier].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.DailySetDone), "the set is done once a day");
        }

        void Claim(List<QuestDef> quests, int i)
        {
            bool setBefore = FunnelLog.DailySetDone(quests, s);
            Assert.IsTrue(Quests.TryClaim(quests[i], defs, s, Modifiers.None, out _), quests[i].Id);
            mem.LogQuestClaimed(quests, quests[i], s, setBefore);
        }

        [Test]
        public void A_Week_Chest_Logs_Which_Chest_And_Whether_It_Came_By_Mail()
        {
            mem.LogWeekChest(2, AnalyticsEvents.FromWeek);
            MemoryAnalytics.Event e = Only(AnalyticsEvents.WeekChestOpened);
            Assert.AreEqual(3L, e[AnalyticsEvents.Chest].Number, "chests count from one");
            Assert.AreEqual("week", e[AnalyticsEvents.From].Text);

            mem.LogWeekChest(new Letter { Kind = LetterKind.FairEnd });
            mem.LogWeekChest((Letter)null);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.WeekChestOpened), "other letters are not chests");
            mem.LogWeekChest(new Letter { Kind = LetterKind.WeekChest, Chest = 0 });
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.WeekChestOpened)[AnalyticsEvents.Chest].Number);
            Assert.AreEqual("mail", mem.Last(AnalyticsEvents.WeekChestOpened)[AnalyticsEvents.From].Text);
        }

        [Test]
        public void The_Round_Two_Features_Log_Their_First_Use_By_Name()
        {
            var expected = new Dictionary<string, string>
            {
                { Intros.RecipeBook, "recipe_book" }, { Intros.Aquarium, "tank" }, { Intros.Trunk, "trunk" },
                { Intros.Fair, "fair" }, { Intros.MonthPass, "month_pass" }, { Intros.WeekChest, "week_chest" },
            };
            foreach (KeyValuePair<string, string> pair in expected)
            {
                mem.Clear();
                mem.LogIntroFirstUse(pair.Key);
                MemoryAnalytics.Event used = Only(AnalyticsEvents.FeatureFirstUse);
                Assert.AreEqual(pair.Value, used[AnalyticsEvents.Feature].Text, pair.Key);
                Assert.IsFalse(used.Has(AnalyticsEvents.Game), pair.Key);
            }
        }

        [Test]
        public void A_Feature_First_Used_Logs_Its_Name_And_Mini_Games_Their_Game()
        {
            mem.LogIntroFirstUse(Intros.Fishing);
            Assert.AreEqual("fishing", Only(AnalyticsEvents.FeatureFirstUse)[AnalyticsEvents.Feature].Text);
            mem.Clear();
            mem.LogIntroFirstUse(Intros.Leaves);
            MemoryAnalytics.Event leaves = Only(AnalyticsEvents.FeatureFirstUse);
            Assert.AreEqual("minigame", leaves[AnalyticsEvents.Feature].Text);
            Assert.AreEqual("leaves", leaves[AnalyticsEvents.Game].Text);
            mem.Clear();
            foreach (string id in new[] { Intros.Orders, Intros.Shop, Intros.MoonJar, Intros.Rain, Intros.NewMoon }) mem.LogIntroFirstUse(id);
            Assert.AreEqual(0, mem.Events.Count, "only the five features of the issue");
            var expected = new Dictionary<string, string>
            {
                { Intros.Fishing, "fishing" }, { Intros.Pounding, "pounding" }, { Intros.Leaves, "minigame" }, { Intros.Fireflies, "minigame" }, { Intros.Layout, "layout" },
            };
            foreach (KeyValuePair<string, string> pair in expected) Assert.AreEqual(pair.Value, AnalyticsEvents.FeatureOf(pair.Key), pair.Key);

            mem.LogPhotoTaken(1);
            Assert.AreEqual(0, mem.Events.Count, "the second photo is not a first use");
            mem.LogPhotoTaken(0);
            MemoryAnalytics.Event photo = Only(AnalyticsEvents.FeatureFirstUse);
            Assert.AreEqual("photo", photo[AnalyticsEvents.Feature].Text);
            Assert.IsFalse(photo.Has(AnalyticsEvents.Game));
        }
    }
}

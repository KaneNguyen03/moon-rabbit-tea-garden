using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;
using Object = UnityEngine.Object;

namespace VuonTra.Core.Tests
{
    public class AnalyticsFunnelScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics mem;
        long today = GiftCodes.Day(2027, 5, Fairs.StartDay + 1);

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            Motion.SetReduced(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState S => runner.State;

        void Open(Action<GameState> shape = null)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            mem = new MemoryAnalytics();
            runner.Analytics = mem;
            runner.DayClock = () => today;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            s.Levels[1] = 12; s.Levels[2] = 9; s.Levels[3] = 4;
            s.GardenXp = Garden.XpForLevel(Pounding.UnlockLevel);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.PlaySeconds = Intros.EarliestSeconds;
            s.Grid.Segments = Intros.FishingSegments;
            s.FirstDaySeen = today - 20;
            s.MaxDaySeen = today;
            s.WelcomeBack.SeenDay = today;
            s.MailDay = today;
            s.GiftDay = today;
            s.Newcomer.Closed = true;
            shape?.Invoke(s);
            runner.Init(s);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        void Wait(int seconds)
        {
            for (int k = 0; k < seconds; k++) screen.Advance(1f);
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        [Test]
        public void Try_Now_Later_And_The_Gold_Dot_Each_Log_Once_With_The_Cards_Id()
        {
            today = GiftCodes.Day(2027, 2, 1);
            Open();
            S.IntrosSeen.Clear();
            S.IntrosTried.Clear();
            S.IntrosLater.Clear();
            mem.Clear();

            Step(0.4f);
            Assert.AreEqual(Intros.Fishing, screen.IntroCard.Showing);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.IntroShown));
            Assert.AreEqual(Intros.Fishing, mem.Last(AnalyticsEvents.IntroShown)[AnalyticsEvents.Id].Text);
            Step(0.4f);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.IntroShown), "shown once, not once a frame");
            Press("Canvas/Intro/Try");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.IntroTry));
            MemoryAnalytics.Event tried = mem.Last(AnalyticsEvents.IntroTry);
            Assert.AreEqual(Intros.Fishing, tried[AnalyticsEvents.Id].Text);
            Assert.AreEqual(AnalyticsEvents.FromCard, tried[AnalyticsEvents.From].Text);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.IntroLater));

            Step(0.4f);
            Wait((int)Intros.GapSeconds + 1);
            Assert.AreEqual(Intros.Pounding, screen.IntroCard.Showing);
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.IntroShown));
            Press("Canvas/Intro/Later");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.IntroLater));
            MemoryAnalytics.Event later = mem.Last(AnalyticsEvents.IntroLater);
            Assert.AreEqual(Intros.Pounding, later[AnalyticsEvents.Id].Text);
            Assert.AreEqual(AnalyticsEvents.FromCard, later[AnalyticsEvents.From].Text);

            screen.Refresh();
            Step(0.3f);
            RectTransform dot = screen.IntroDot(Intros.Pounding);
            Assert.IsNotNull(dot, "a gold dot waits on the mortar");
            dot.GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.IntroTry));
            Assert.AreEqual(AnalyticsEvents.FromDot, mem.Last(AnalyticsEvents.IntroTry)[AnalyticsEvents.From].Text);
            Assert.AreEqual(Intros.Pounding, mem.Last(AnalyticsEvents.IntroTry)[AnalyticsEvents.Id].Text);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.IntroLater), "touching the dot is not another later");
        }

        [Test]
        public void Releasing_A_Fish_From_The_Pond_Logs_The_Release_And_Its_Milestone_Once()
        {
            today = GiftCodes.Day(2027, 2, 1);
            Open(s => { s.IntrosSeen.UnionWith(Intros.All); s.IntrosTried.UnionWith(Intros.All); s.IntrosKnown = true; });
            S.Fish[2] = 2;
            Press("Canvas/Garden/Content/PondTap");
            screen.Refresh();
            Step(0.3f);
            Assert.IsTrue(screen.FishingCard.IsOpen);
            mem.Clear();

            Press("Canvas/Pond/Card/Basket/Fish 2");
            Press("Canvas/Pond/Card/Release");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TankRelease));
            MemoryAnalytics.Event released = mem.Last(AnalyticsEvents.TankRelease);
            Assert.AreEqual(2L, released[AnalyticsEvents.Kind].Number);
            Assert.AreEqual(1L, released[AnalyticsEvents.Count].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TankMilestone));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.TankMilestone)[AnalyticsEvents.Step].Number);

            Press("Canvas/Pond/Card/Release");
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.TankRelease));
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.TankRelease)[AnalyticsEvents.Count].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TankMilestone), "the second fish of a kind is no milestone");
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.TankFull));
        }

        [Test]
        public void Opening_On_A_Fair_Day_Logs_The_Start_Once_And_Trading_At_The_Stall_Logs_Each_Trade()
        {
            Open(s => { s.GardenXp = Garden.XpForLevel(6); s.IntrosSeen.UnionWith(Intros.All); s.IntrosTried.UnionWith(Intros.All); s.IntrosKnown = true; });
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.FairStart), "opening the garden joins the fair");
            Assert.AreEqual("hoi-tra-sen", mem.Last(AnalyticsEvents.FairStart)[AnalyticsEvents.Fair].Text);
            runner.OfferCardsOn = true;
            screen.Refresh();
            Assert.IsTrue(screen.FairCard.IsOpen);
            S.Fair.Coins = 480;
            mem.Clear();

            Press("Canvas/FairCard/Card/Stall 0");
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.FairTrade), "the first tap only asks");
            Press("Canvas/FairCard/Card/Stall 0");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.FairTrade));
            MemoryAnalytics.Event trade = mem.Last(AnalyticsEvents.FairTrade);
            Assert.AreEqual("jars", trade[AnalyticsEvents.Item].Text);
            Assert.AreEqual(40L, trade[AnalyticsEvents.Cost].Number);
            Assert.AreEqual("hoi-tra-sen", trade[AnalyticsEvents.Fair].Text);

            Press("Canvas/FairCard/Card/Stall 5");
            Press("Canvas/FairCard/Card/Stall 5");
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.FairTrade));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.FairRabbit));
            Assert.AreEqual("tho-hoa-nhai", mem.Last(AnalyticsEvents.FairRabbit)[AnalyticsEvents.Kind].Text);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.FairStart), "the fair started once");
        }

        [Test]
        public void Raising_A_Dish_And_Claiming_A_Week_Chest_Log_Once_Each()
        {
            today = GiftCodes.Day(2027, 2, 1);
            Open(s => { s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel); s.RecipeLeaves = 100; s.IntrosSeen.UnionWith(Intros.All); s.IntrosTried.UnionWith(Intros.All); s.IntrosKnown = true; });
            mem.Clear();
            Assert.IsTrue(runner.RaiseDish(1));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.DishLevel));
            MemoryAnalytics.Event raised = mem.Last(AnalyticsEvents.DishLevel);
            Assert.AreEqual(runner.Defs[1].Id, raised[AnalyticsEvents.Station].Text);
            Assert.AreEqual(2L, raised[AnalyticsEvents.Level].Number);

            S.Coins = new BigNum(1e30);
            int level = S.Levels[3];
            while (S.Levels[3] < Economy.Ranks[0] && runner.Buy(3)) { }
            Assert.GreaterOrEqual(S.Levels[3], Economy.Ranks[0], "from level " + level);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.DishUnlocked), "the second dish opened once");
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.DishUnlocked)[AnalyticsEvents.Dish].Number);

            QuestChests.AddPoints(S, runner.TodayNumber, QuestChests.Chests[0].Points);
            Assert.IsTrue(runner.ClaimWeekChest(0, out _));
            Assert.IsFalse(runner.ClaimWeekChest(0, out _));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.WeekChestOpened));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.WeekChestOpened)[AnalyticsEvents.Chest].Number);
            Assert.AreEqual(AnalyticsEvents.FromWeek, mem.Last(AnalyticsEvents.WeekChestOpened)[AnalyticsEvents.From].Text);
        }

        [Test]
        public void The_First_Photo_And_The_First_Mini_Game_Log_A_First_Use_And_Never_Again()
        {
            today = GiftCodes.Day(2027, 2, 1);
            Open(s => { s.IntrosSeen.UnionWith(Intros.All); s.IntrosTried.UnionWith(Intros.All); s.IntrosKnown = true; });
            mem.Clear();
            runner.TakePhoto();
            runner.TakePhoto();
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.FeatureFirstUse));
            Assert.AreEqual(AnalyticsEvents.FeaturePhoto, mem.Last(AnalyticsEvents.FeatureFirstUse)[AnalyticsEvents.Feature].Text);

            S.IntrosTried.Remove(Intros.Pounding);
            Assert.IsTrue(runner.TryIntro(Intros.Pounding));
            Assert.IsFalse(runner.TryIntro(Intros.Pounding));
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.FeatureFirstUse));
            Assert.AreEqual(AnalyticsEvents.FeaturePounding, mem.Last(AnalyticsEvents.FeatureFirstUse)[AnalyticsEvents.Feature].Text);
        }
    }
}

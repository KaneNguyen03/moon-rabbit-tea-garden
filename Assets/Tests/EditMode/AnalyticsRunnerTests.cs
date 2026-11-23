using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;
using VuonTra.Game.Platform;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;
using Object = UnityEngine.Object;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The runner's analytics hooks (#100): each event goes out once, at its moment, with its parameters; nothing goes
    /// out without a provider; player builds have none while the switch is off; caught exceptions are forwarded.
    /// </summary>
    public class AnalyticsRunnerTests
    {
        GameObject root;
        GameRunner runner;
        GameCatalog catalog;
        MemoryAnalytics mem;
        long now = 1_800_000_000L;

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            mem = new MemoryAnalytics();
            runner.Analytics = mem;
            runner.SessionClock = () => now;
            runner.DayClock = () => 20_800L;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        /// <summary>A new garden, with whatever <paramref name="shape"/> does to it first, opened by the runner.</summary>
        GameState Open(Action<GameState> shape = null)
        {
            GameState s = GameState.NewGame(catalog.BuildDefs());
            shape?.Invoke(s);
            runner.Init(s);
            return s;
        }

        void Engine(string message, params object[] args) =>
            typeof(GameRunner).GetMethod(message, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(runner, args);

        [Test]
        public void Opening_The_Game_Starts_A_Session_And_Going_Away_Ends_It_With_Its_Length()
        {
            Open(s => { s.GardenXp = Garden.XpForLevel(7); s.Season = 2; });
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.GameOpen));
            Assert.AreEqual(0L, mem.Last(AnalyticsEvents.GameOpen)[AnalyticsEvents.NewGarden].Number, "a garden handed in, not a new save");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.SessionStart));
            MemoryAnalytics.Event start = mem.Last(AnalyticsEvents.SessionStart);
            Assert.AreEqual(2L, start[AnalyticsEvents.Moon].Number);
            Assert.AreEqual(7L, start[AnalyticsEvents.Level].Number);
            Assert.AreEqual("2", mem.UserProperties[AnalyticsEvents.UserMoon]);
            Assert.AreEqual("7", mem.UserProperties[AnalyticsEvents.UserGardenLevel]);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.GardenLevelUp), "the levels the save already had are not level-ups");

            Engine("OnApplicationPause", false); // the engine says "not paused" once at launch too
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.SessionStart), "still the one session");

            now += 125;
            Engine("OnApplicationPause", true);
            Engine("OnApplicationPause", true);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.SessionEnd), "one end per session");
            Assert.AreEqual(125L, mem.Last(AnalyticsEvents.SessionEnd)[AnalyticsEvents.Seconds].Number);

            now += 3600; // an hour in the background is no play
            Engine("OnApplicationPause", false);
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.SessionStart));
            now += 40;
            Engine("OnApplicationQuit");
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.SessionEnd));
            Assert.AreEqual(40L, mem.Last(AnalyticsEvents.SessionEnd)[AnalyticsEvents.Seconds].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.GameOpen), "the game opened once");
        }

        [Test]
        public void Each_Tutorial_Step_Goes_Out_Once_When_Done_And_A_Skip_Once()
        {
            Open();
            mem.Clear();
            Assert.IsTrue(runner.Tap(0));
            Assert.IsTrue(runner.Tap(0));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TutorialStep), "the first tap does the step; the second is just a tap");
            MemoryAnalytics.Event step = mem.Last(AnalyticsEvents.TutorialStep);
            Assert.AreEqual((long)Tutorial.TapRabbit, step[AnalyticsEvents.Step].Number);
            Assert.AreEqual("tap_rabbit", step[AnalyticsEvents.StepName].Text);

            runner.SkipTutorial();
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TutorialSkip));
            Assert.AreEqual((long)Tutorial.RaiseTeapot, mem.Last(AnalyticsEvents.TutorialSkip)[AnalyticsEvents.Step].Number, "skipped at the step it was on");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TutorialStep), "the steps skipped are not steps done");
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.TutorialComplete));
        }

        [Test]
        public void Calling_A_Rabbit_With_Clovers_Logs_Its_Tier_And_Kind_And_The_Level_It_Brings()
        {
            Open(s => { s.GardenXp = Garden.XpForLevel(5) - 1d; s.Clovers = Rabbits.SummonCost; });
            mem.Clear();
            Rabbit rabbit = runner.SummonRabbit();
            Assert.IsNotNull(rabbit);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.RabbitCalled));
            MemoryAnalytics.Event called = mem.Last(AnalyticsEvents.RabbitCalled);
            Assert.AreEqual(AnalyticsEvents.ViaClover, called[AnalyticsEvents.Via].Text);
            Assert.AreEqual((long)rabbit.Tier, called[AnalyticsEvents.Tier].Number);
            Assert.AreEqual(rabbit.Kind, called[AnalyticsEvents.Kind].Text);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.GardenLevelUp), "the call's experience reached level 5");
            Assert.AreEqual(5L, mem.Last(AnalyticsEvents.GardenLevelUp)[AnalyticsEvents.Level].Number);

            runner.State.Clovers = 0; // the level brought one
            Assert.IsNull(runner.SummonRabbit(), "no clovers left");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.RabbitCalled), "a call that brought nobody is not logged");
        }

        [Test]
        public void Calling_A_Rabbit_With_Golden_Carrots_Logs_Golden_And_Its_Tier()
        {
            Open(s => s.GoldenCarrots = 10_000);
            mem.Clear();
            Rabbit rabbit = runner.SummonGoldenRabbit();
            Assert.IsNotNull(rabbit);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.RabbitCalled));
            MemoryAnalytics.Event called = mem.Last(AnalyticsEvents.RabbitCalled);
            Assert.AreEqual(AnalyticsEvents.ViaGolden, called[AnalyticsEvents.Via].Text);
            Assert.AreEqual((long)rabbit.Tier, called[AnalyticsEvents.Tier].Number);
        }

        [Test]
        public void Opening_A_Station_And_A_Segment_Of_Mist_Logs_Each_Once()
        {
            GameState s = Open(g => { g.GardenXp = Garden.XpForLevel(20); g.Coins = new BigNum(1e30); });
            mem.Clear();
            Assert.IsTrue(runner.Buy(1));
            Assert.IsTrue(runner.Buy(1));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.StationUnlocked), "opened once; the next level is not an opening");
            MemoryAnalytics.Event opened = mem.Last(AnalyticsEvents.StationUnlocked);
            Assert.AreEqual(runner.Defs[1].Id, opened[AnalyticsEvents.Station].Text);
            Assert.AreEqual(1L, opened[AnalyticsEvents.StationIndex].Number);

            Assert.AreEqual(0, Expansion.Segments(s));
            Assert.IsTrue(runner.BuyPlot());
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.SegmentOpened));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.SegmentOpened)[AnalyticsEvents.Segments].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.StationUnlocked), "new land is not an open station yet");
        }

        [Test]
        public void A_New_Moon_Logs_Once_With_Its_Number_And_Nothing_For_The_Garden_Going_Back()
        {
            Open(s => { s.GardenXp = Garden.XpForLevel(NewMoon.FirstLevel); s.Levels[1] = 3; });
            mem.Clear();
            Assert.IsTrue(runner.BeginNewMoon(out _));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.NewMoon));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.NewMoon)[AnalyticsEvents.Moon].Number);
            Assert.AreEqual("1", mem.UserProperties[AnalyticsEvents.UserMoon]);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.GardenLevelUp));
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.StationUnlocked));
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.SegmentOpened));
        }

        [Test]
        public void A_Purchase_Logs_Success_With_The_Stores_Price_Or_Failure_With_Its_Reason()
        {
            GameState s = Open();
            var store = new FakeStorefront();
            runner.Store = store;
            mem.Clear();

            runner.BuyGolden(Storefront.Medium, null);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.PurchaseSuccess));
            MemoryAnalytics.Event paid = mem.Last(AnalyticsEvents.PurchaseSuccess);
            Assert.AreEqual(Storefront.Medium, paid[AnalyticsEvents.ProductId].Text);
            Assert.AreEqual(store.PriceOf(Storefront.Medium), paid[AnalyticsEvents.Price].Text, "the store's own words, no amount of ours");
            Assert.AreEqual(0L, paid[AnalyticsEvents.Late].Number);

            store.NextOutcome = PurchaseOutcome.Cancelled;
            runner.BuyGolden(Storefront.Small, null);
            store.NextOutcome = PurchaseOutcome.Failed;
            runner.BuyGolden(Storefront.Small, null);
            s.PaidTransactions.Add("fake-2"); // the fake's next transaction was paid out already
            runner.BuyGolden(Storefront.Large, null);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.PurchaseSuccess));
            Assert.AreEqual(new[] { "cancelled", "failed", "duplicate" },
                mem.All(AnalyticsEvents.PurchaseFailed).ConvertAll(e => e[AnalyticsEvents.Reason].Text).ToArray());
            Assert.AreEqual(Storefront.Large, mem.Last(AnalyticsEvents.PurchaseFailed)[AnalyticsEvents.ProductId].Text);

            store.Deliver(Storefront.Small, "late-1");
            store.Deliver(Storefront.Small, "late-1");
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.PurchaseSuccess), "a purchase the store handed over late counts once");
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.PurchaseSuccess)[AnalyticsEvents.Late].Number);
        }

        [Test]
        public void A_Rewarded_Video_Logs_Its_Place_And_Whether_It_Was_Watched()
        {
            Open();
            var ads = new FakeRewardedAds { Seconds = 0d };
            runner.AdProvider = ads;
            mem.Clear();

            // A callback each time: the claim is paid inside it (null would skip the claim, not the video).
            runner.WatchAd(Ads.ShopGolden, null, _ => { });
            ads.NextResult = false;
            runner.WatchAd(Ads.ShopGolden, null, _ => { });
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.RewardedAd));
            Assert.AreEqual(Ads.ShopGolden, mem.Events[0][AnalyticsEvents.Placement].Text);
            Assert.AreEqual(AnalyticsEvents.AdWatched, mem.Events[0][AnalyticsEvents.Result].Text);
            Assert.AreEqual(AnalyticsEvents.AdClosed, mem.Events[1][AnalyticsEvents.Result].Text);

            Ads.GrantPass(runner.State, runner.TodayNumber);
            runner.WatchAd(Ads.ShopGolden, null, _ => { });
            Assert.AreEqual(AnalyticsEvents.AdPass, mem.Last(AnalyticsEvents.RewardedAd)[AnalyticsEvents.Result].Text);

            runner.WatchAd(Ads.ShopGolden, null, _ => { });
            Assert.AreEqual(3, mem.Count(AnalyticsEvents.RewardedAd), "no button, no video: nothing to log once the day's two are claimed");
        }

        [Test]
        public void Without_A_Provider_Nothing_Goes_Out()
        {
            GameState s = Open(g => { g.Clovers = Rabbits.SummonCost; g.GardenXp = Garden.XpForLevel(NewMoon.FirstLevel) - 1d; g.GoldenCarrots = 10_000; });
            runner.Analytics = new NoAnalytics();
            Assert.IsInstanceOf<NoAnalytics>(runner.Analytics);
            mem.Clear();

            runner.Tap(0);
            runner.SummonRabbit();
            runner.SummonGoldenRabbit();
            runner.BuyGolden(Storefront.Small, null);
            runner.AdProvider = new FakeRewardedAds { Seconds = 0d };
            runner.WatchAd(Ads.ShopGolden, null, null);
            runner.BeginNewMoon(out _);
            runner.RecordException(new InvalidOperationException("x"));
            Engine("OnApplicationPause", true);
            Assert.AreEqual(0, mem.Events.Count + mem.Exceptions.Count + mem.UserProperties.Count, "nothing reached the old provider");
            Assert.Greater(s.Rabbits.Count, 1, "the game itself went on as before");

            runner.Analytics = null;
            Assert.IsInstanceOf<NoAnalytics>(runner.Analytics, "no provider is none, never a crash");
        }

        [Test]
        public void Player_Builds_Send_Nothing_While_The_Switch_Is_Off_And_The_Editor_Keeps_Events_In_Memory()
        {
            Assert.IsFalse(GameRunner.AnalyticsInPlayerBuilds, "no analytics in any player build until the owner switches it on");
            Assert.IsInstanceOf<NoAnalytics>(GameRunner.AnalyticsFor(true));
            Assert.IsInstanceOf<MemoryAnalytics>(GameRunner.AnalyticsFor(false), "the editor keeps them, sends nothing");
            Assert.IsNull(FirebaseAnalyticsAdapter.Create(), "built without VUONTRA_FIREBASE: no Firebase in this build");
            try
            {
                GameRunner.AnalyticsInPlayerBuilds = true;
                Assert.IsInstanceOf<NoAnalytics>(GameRunner.AnalyticsFor(true), "the switch alone, without the SDK, still sends nothing");
            }
            finally { GameRunner.AnalyticsInPlayerBuilds = false; }

            var plain = new GameObject("Plain").AddComponent<GameRunner>();
            try { Assert.IsInstanceOf<MemoryAnalytics>(plain.Analytics, "an editor runner starts on memory"); }
            finally { Object.DestroyImmediate(plain.gameObject); }
        }

        [Test]
        public void Caught_And_Uncaught_Exceptions_Are_Forwarded_To_The_Crash_Reports()
        {
            Open();
            var caught = new InvalidOperationException("save failed");
            runner.RecordException(caught);
            Assert.AreSame(caught, mem.Exceptions[0]);

            runner.OnLogMessage("NullReferenceException: boom", "GardenScreen.Refresh ()", LogType.Exception);
            runner.OnLogMessage("just a warning", "", LogType.Warning);
            runner.OnLogMessage("an error line", "", LogType.Error);
            Assert.AreEqual(2, mem.Exceptions.Count, "only exceptions, not every log line");
            Assert.IsInstanceOf<GameRunner.LoggedException>(mem.Exceptions[1]);
            Assert.AreEqual("NullReferenceException: boom", mem.Exceptions[1].Message);
            Assert.AreEqual("GardenScreen.Refresh ()", mem.Exceptions[1].StackTrace);
        }
    }

    /// <summary>The one hook on the screen (#100): opening the golden packs card from the shop.</summary>
    public class AnalyticsScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics mem;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            mem = new MemoryAnalytics();
            runner.Analytics = mem;
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        [Test]
        public void Opening_The_Golden_Packs_Card_From_The_Shop_Logs_Once()
        {
            screen.OpenShop();
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.GoldenPacksOpened), "the shop alone is not the packs card");
            root.transform.Find("Canvas/Shop/Card/Have/BuyMore").GetComponent<UnityEngine.UI.Button>().onClick.Invoke();
            Assert.IsTrue(screen.GoldenPacks.IsOpen);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.GoldenPacksOpened));
            Assert.AreEqual(AnalyticsEvents.FromShop, mem.Last(AnalyticsEvents.GoldenPacksOpened)[AnalyticsEvents.From].Text);
        }
    }
}

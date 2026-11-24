using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.Platform;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;
using Object = UnityEngine.Object;

namespace VuonTra.Core.Tests
{
    public class ReviewScreenTests
    {
        const long Today = 20_800L;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics mem;
        FakeReviewPrompt prompt;
        string opened;
        Action<string> openBefore;

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
            runner.DayClock = () => Today;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Tutorial.Skip(s);
            Rabbits.GiftFirst(s, "Bông");
            s.FirstDaySeen = s.MaxDaySeen = Today - ReviewAsk.MinDaysPlayed;
            s.PlaySeconds = ReviewAsk.MinPlaySeconds;
            runner.Init(s);
            prompt = new FakeReviewPrompt();
            runner.ReviewPrompt = prompt;
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            openBefore = ProfileView.OpenUrl;
            opened = null;
            ProfileView.OpenUrl = url => opened = url;
        }

        [TearDown]
        public void TearDown()
        {
            ProfileView.OpenUrl = openBefore;
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        T Find<T>(string path) where T : Component
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<T>();
        }

        void TenthHeart()
        {
            Rabbit r = runner.State.Rabbits[0];
            r.Affection = Rabbits.PointsFor(Rabbits.HeartMax) - 1;
            r.PetDay = -1;
            Assert.IsTrue(runner.PetRabbit(r.Id).TenHearts);
        }

        [Test]
        public void The_Runner_Picks_The_Logging_Fake_Without_A_Native_Plugin()
        {
            Assert.IsNull(NativeReviewPrompt.Create(), "no native review plugin in this build");
            var plain = new GameObject("Plain").AddComponent<GameRunner>();
            try { Assert.IsInstanceOf<FakeReviewPrompt>(plain.ReviewPrompt); }
            finally { Object.DestroyImmediate(plain.gameObject); }
            runner.ReviewPrompt = null;
            Assert.IsInstanceOf<FakeReviewPrompt>(runner.ReviewPrompt);
        }

        [Test]
        public void A_Tenth_Heart_On_Day_Three_Asks_And_Love_It_Opens_The_Stores_Own_Box_Once_For_Good()
        {
            Assert.IsFalse(screen.ReviewCard.IsOpen);
            TenthHeart();
            screen.Refresh();
            Assert.IsTrue(screen.ReviewCard.IsOpen);
            Assert.IsFalse(screen.ReviewCard.AskingFeedback);
            Assert.AreEqual(L10n.T("review.title"), Find<Text>("Canvas/Review/Card/Ask/Title").text);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.ReviewAsked));
            Assert.AreEqual("ten_hearts", mem.Last(AnalyticsEvents.ReviewAsked)[AnalyticsEvents.Moment].Text);
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.ReviewAsked)[AnalyticsEvents.Asks].Number);

            Find<PressDip>("Canvas/Review/Card/Ask/Yes").Click();
            Assert.IsFalse(screen.ReviewCard.IsOpen);
            Assert.AreEqual(1, prompt.Requests);
            Assert.AreEqual(0, prompt.StorePages, "the in-game box, not the store page");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.ReviewLiked));
            Assert.IsTrue(runner.State.Review.Liked);
            Assert.IsNull(opened, "nothing leaves the game but the store's own box");
        }

        [Test]
        public void Not_Really_Asks_For_One_Line_And_Mails_It_To_Support_With_The_Player_Code()
        {
            TenthHeart();
            screen.Refresh();
            int golden = runner.State.GoldenCarrots;
            Find<PressDip>("Canvas/Review/Card/Ask/No").Click();
            Assert.IsTrue(screen.ReviewCard.IsOpen);
            Assert.IsTrue(screen.ReviewCard.AskingFeedback);
            StringAssert.Contains(runner.State.Profile.Code, Find<Text>("Canvas/Review/Card/Feedback/Note").text);

            screen.ReviewCard.Input.text = "Muốn thêm nhạc đêm";
            Find<PressDip>("Canvas/Review/Card/Feedback/Send").Click();
            Assert.IsFalse(screen.ReviewCard.IsOpen);
            Assert.IsNotNull(opened);
            StringAssert.StartsWith("mailto:" + AppInfo.SupportEmail, opened);
            StringAssert.Contains(runner.State.Profile.Code, opened);
            StringAssert.Contains(Uri.EscapeDataString("Muốn thêm nhạc đêm"), opened);
            Assert.AreEqual(0, prompt.Requests + prompt.StorePages, "never the store");
            MemoryAnalytics.Event e = mem.Last(AnalyticsEvents.ReviewFeedback);
            Assert.AreEqual(1L, e[AnalyticsEvents.Sent].Number);
            Assert.AreEqual(18L, e[AnalyticsEvents.Chars].Number);
            Assert.IsFalse(runner.State.Review.Liked);
            Assert.AreEqual(golden, runner.State.GoldenCarrots, "no reward either way");
        }

        [Test]
        public void Not_Now_Sends_Nothing_And_The_Card_Waits_While_Another_Card_Is_Up()
        {
            screen.Profile.Open(runner.Defs, runner.State, runner.Account);
            TenthHeart();
            screen.Refresh();
            Assert.IsFalse(screen.ReviewCard.IsOpen, "the profile is up");
            Assert.AreEqual(0, runner.State.Review.Asks);
            screen.Profile.Close();
            screen.Refresh();
            Assert.IsTrue(screen.ReviewCard.IsOpen);

            Find<PressDip>("Canvas/Review/Card/Ask/No").Click();
            Find<PressDip>("Canvas/Review/Card/Feedback/Cancel").Click();
            Assert.IsFalse(screen.ReviewCard.IsOpen);
            Assert.IsNull(opened);
            Assert.AreEqual(0L, mem.Last(AnalyticsEvents.ReviewFeedback)[AnalyticsEvents.Sent].Number);
        }

        [Test]
        public void A_Crash_Or_A_Failed_Purchase_This_Session_Keeps_The_Card_Away()
        {
            runner.RecordException(new InvalidOperationException("boom"));
            TenthHeart();
            screen.Refresh();
            Assert.IsFalse(screen.ReviewCard.IsOpen);

            runner.Review.NewSession();
            runner.Store = new FakeStorefront { NextOutcome = PurchaseOutcome.Failed };
            runner.BuyGolden(Storefront.Packs[0].Id, _ => { });
            Assert.IsTrue(runner.Review.PurchaseFailed);
            Rabbit r = runner.State.Rabbits[0];
            r.Affection = 0;
            TenthHeart();
            screen.Refresh();
            Assert.IsFalse(screen.ReviewCard.IsOpen);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.ReviewAsked));
        }

        [Test]
        public void Too_Early_In_The_Garden_Nothing_Is_Asked()
        {
            runner.State.FirstDaySeen = Today - 1;
            TenthHeart();
            screen.Refresh();
            Assert.IsFalse(screen.ReviewCard.IsOpen);
            Assert.AreEqual(0, runner.State.Review.Asks);
        }

        [Test]
        public void The_Profile_Has_A_Rate_Row_That_Opens_The_Store_Page_Any_Time()
        {
            runner.State.FirstDaySeen = Today;
            screen.Profile.Open(runner.Defs, runner.State, runner.Account);
            Assert.AreEqual(L10n.T("review.rate"), Find<Text>("Canvas/Profile/Card/Rate/Text").text);
            Find<Button>("Canvas/Profile/Card/Rate").onClick.Invoke();
            Assert.AreEqual(1, prompt.StorePages);
            Assert.AreEqual(0, prompt.Requests);
            Assert.IsNull(opened);
        }
    }
}

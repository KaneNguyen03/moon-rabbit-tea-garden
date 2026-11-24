using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class TrunkScreenTests
    {
        const long Today = 20_780L;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics log;
        long clock;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            log = new MemoryAnalytics();
            runner.Analytics = log;
            runner.DayClock = () => Today;
            clock = Today * 86400L + 19 * 3600L + 600L;
            runner.LocalClock = () => clock;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            s.TutorialStep = Tutorial.Steps;
            s.HintSeen = true;
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel) + 1;
            s.Newcomer.Closed = true;
            s.GoldenCarrots = 500;
            s.FirstDaySeen = s.MaxDaySeen = Today - 3;
            s.WelcomeBack.SeenDay = s.MaxDaySeen;
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
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
            if (root != null) Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        [Test]
        public void The_Trunk_Stands_Beside_The_Shop_And_Opens_A_Card_Of_Four()
        {
            Assert.IsNotNull(screen.TrunkProp);
            Assert.AreEqual(L10n.T("trunk.tag.open"), screen.TrunkTag);
            screen.TrunkProp.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.TrunkCard.IsOpen);
            Assert.IsTrue(screen.TrunkCard.ShowsGoods);
            Assert.AreEqual(L10n.T("trunk.open", 22), screen.TrunkCard.Sub);
            int sales = 0;
            for (int i = 0; i < Trunk.Shown; i++)
            {
                Assert.IsTrue(screen.TrunkCard.TileShown(i));
                Assert.AreEqual(screen.TrunkCard.Shelf.Offers[i].OldSeason, screen.TrunkCard.OldShown(i));
                if (screen.TrunkCard.SaleShown(i)) sales++;
            }
            Assert.AreEqual(1, sales, "one item a day shows the sale");
            Assert.AreEqual(1, log.Count(AnalyticsEvents.TrunkOpen));
            Assert.IsFalse(screen.OfferCardFree, "no other card opens over the trunk");
            Assert.IsTrue(screen.ReviewBusy);
        }

        [Test]
        public void Two_Taps_Buy_An_Item_And_It_Then_Reads_Bought()
        {
            screen.OpenTrunk();
            TrunkOffer offer = screen.TrunkCard.Shelf.Offers[2];
            int before = runner.State.GoldenCarrots;
            Press("Canvas/TrunkCard/Card/Goods/Item 3/Buy");
            Assert.AreEqual(before, runner.State.GoldenCarrots, "the first tap asks again");
            Assert.AreEqual(L10n.T("shop.hats.confirm"), screen.TrunkCard.LabelOf(2));
            Press("Canvas/TrunkCard/Card/Goods/Item 3/Buy");
            Assert.AreEqual(before - offer.Price, runner.State.GoldenCarrots);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.TrunkBuy));
            Assert.AreEqual(offer.Code, log.Last(AnalyticsEvents.TrunkBuy)[AnalyticsEvents.Item].Text);
            if (screen.TrunkCard.IsOpen) Assert.AreEqual(L10n.T("trunk.bought"), screen.TrunkCard.LabelOf(2));
            Assert.IsTrue(runner.TrunkShelf.Offers[2].Bought);
        }

        [Test]
        public void Outside_The_Hours_The_Trunk_Is_Closed_And_Says_When_It_Opens()
        {
            clock = Today * 86400L + 14 * 3600L;
            screen.Refresh();
            Assert.AreEqual(L10n.T("trunk.closed", 19), screen.TrunkTag);
            Assert.AreEqual("Mở lúc 19:00", screen.TrunkTag);
            screen.OpenTrunk();
            Assert.IsTrue(screen.TrunkCard.IsOpen);
            Assert.IsFalse(screen.TrunkCard.ShowsGoods);
            Assert.AreEqual("closed", log.Last(AnalyticsEvents.TrunkOpen)[AnalyticsEvents.Window].Text);
        }
    }
}

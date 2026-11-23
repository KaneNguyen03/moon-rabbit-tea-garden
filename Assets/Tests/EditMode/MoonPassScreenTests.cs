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
    public class MoonPassScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics analytics;

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
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.GoldenCarrots = 12;
            runner.Init(s);
            analytics = new MemoryAnalytics();
            runner.Analytics = analytics;
            runner.Store = new FakeStorefront();
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

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        static float Top(RectTransform rt) => rt.anchoredPosition.y + rt.sizeDelta.y / 2f;
        static float Bottom(RectTransform rt) => rt.anchoredPosition.y - rt.sizeDelta.y / 2f;
        static float Left(RectTransform rt) => rt.anchoredPosition.x - rt.sizeDelta.x / 2f;
        static float Right(RectTransform rt) => rt.anchoredPosition.x + rt.sizeDelta.x / 2f;

        [Test]
        public void The_Moon_Pass_Card_Sits_Below_The_Packs_And_Says_What_It_Gives()
        {
            screen.OpenShop();
            Find("Canvas/Shop/Card/Have/BuyMore").onClick.Invoke();
            Transform card = root.transform.Find("Canvas/GoldenPacks/Card");
            Assert.IsTrue(screen.GoldenPacks.MoonPassShown);
            Assert.AreEqual(L10n.T("moonpass.title"), screen.GoldenPacks.MoonPassLabel);
            StringAssert.Contains("100 ngay + 15 mỗi ngày = 550 cà rốt vàng", screen.GoldenPacks.MoonPassSub);
            Assert.AreEqual("109.000 ₫", card.Find("MoonPass/Face/PriceChip/Price").GetComponent<Text>().text);

            var moon = (RectTransform)card.Find("MoonPass");
            var lastPack = (RectTransform)card.Find("Pack " + (Storefront.Packs.Length - 1));
            var status = (RectTransform)card.Find("Status");
            var restore = (RectTransform)card.Find("Restore");
            Assert.Less(Top(moon), Bottom(lastPack), "below the packs");
            Assert.AreEqual(-420f - (Storefront.Packs.Length - 1) * 150f, lastPack.anchoredPosition.y, "the packs keep their places");
            Assert.Less(Top(status), Bottom(moon), "the status line is below the card");
            float cardHeight = ((RectTransform)card).sizeDelta.y;
            Assert.Greater(Bottom(status), -(cardHeight - (restore.anchoredPosition.y + restore.sizeDelta.y / 2f)), "and above Restore");
            var noAds = (RectTransform)card.Find("NoAds");
            if (noAds.gameObject.activeSelf) Assert.Less(Top(moon), Bottom(noAds), "and below the no-ads pass");
            Assert.GreaterOrEqual(moon.sizeDelta.y, UiKit.MinTouch);
        }

        [Test]
        public void Buying_Pays_A_Hundred_Shows_The_Pill_And_Mails_Day_One()
        {
            screen.OpenShop();
            Find("Canvas/Shop/Card/Have/BuyMore").onClick.Invoke();
            Find("Canvas/GoldenPacks/Card/MoonPass").onClick.Invoke();
            Assert.AreEqual(12 + MoonPass.GoldenNow, runner.State.GoldenCarrots);
            Assert.AreEqual(MoonPass.Days, runner.MoonPassDaysLeft);
            Assert.AreEqual(L10n.T("moonpass.paid", MoonPass.GoldenNow, MoonPass.Days), screen.GoldenPacks.Status);
            Assert.AreEqual(L10n.T("moonpass.active", MoonPass.Days), screen.GoldenPacks.MoonPassLabel);
            Assert.AreEqual(1, analytics.Count(AnalyticsEvents.PassBought));
            screen.GoldenPacks.Close();
            screen.Shop.Close();
            screen.Refresh();

            Assert.IsTrue(screen.MoonPassPill.gameObject.activeSelf);
            Assert.AreEqual("Vé trăng · còn 30 ngày", screen.MoonPassPillText);

            Letter day = runner.Letters[0];
            Assert.AreEqual(LetterKind.MoonPassDay, day.Kind);
            Assert.AreEqual("Vé trăng ngày 1/30", MailView.TitleOf(day));
            runner.ClaimLetter(day.Id);
            Assert.AreEqual(12 + MoonPass.GoldenNow + MoonPass.GoldenPerDay, runner.State.GoldenCarrots);
            Assert.AreEqual(1, analytics.Count(AnalyticsEvents.PassDayClaimed));
        }

        [Test]
        public void The_Pill_Stands_Beside_The_Golden_Chips_And_Clear_Of_The_Bars_Above()
        {
            Assert.IsFalse(screen.MoonPassPill.gameObject.activeSelf, "no pass, no pill");
            MoonPass.Grant(runner.State, runner.TodayNumber);
            screen.Refresh();
            RectTransform pill = screen.MoonPassPill;
            var golden = (RectTransform)root.transform.Find("Canvas/Hud/Golden");
            var jars = (RectTransform)root.transform.Find("Canvas/Hud/Jars");
            var energy = (RectTransform)root.transform.Find("Canvas/Hud/Energy");
            Assert.IsTrue(pill.gameObject.activeSelf);
            Assert.GreaterOrEqual(Left(pill), Right(golden), "right of the golden carrots");
            Assert.Less(Top(pill), Bottom(energy), "under the tea-power bar");
            Assert.AreEqual(240f, golden.anchoredPosition.x, "the golden chip keeps its place");

            runner.State.MoonJars = 2;
            runner.State.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            screen.Refresh();
            Assert.IsTrue(jars.gameObject.activeSelf);
            Assert.GreaterOrEqual(Left(pill), Right(jars), "and right of the moon jars when they show");
            Assert.LessOrEqual(Right(pill), 1080f);
            Assert.AreEqual(392f, jars.anchoredPosition.x, "the jar chip keeps its place");

            L10n.SetLocale("en");
            screen.Refresh();
            Assert.AreEqual("Moon pass · 30 days left", screen.MoonPassPillText);
        }
    }
}

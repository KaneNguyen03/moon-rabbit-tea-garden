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
    public class AdSeamsScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        FakeRewardedAds fake;

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
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            fake = new FakeRewardedAds();
            runner.AdProvider = fake;
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

        [Test]
        public void The_Editor_Runs_On_Pretend_Videos_And_Player_Builds_Have_None_Until_Switched_On()
        {
            var plain = new GameObject("Plain").AddComponent<GameRunner>();
            try
            {
                Assert.IsInstanceOf<FakeRewardedAds>(plain.AdProvider, "the editor tries the three places on pretend videos");
                Assert.IsFalse(GameRunner.AdsInPlayerBuilds, "player builds show no ad button until the owner switches ads on");
            }
            finally { Object.DestroyImmediate(plain.gameObject); }
        }

        AwayReport ComeBackAfterTheFreeDoubleIsSpent()
        {
            GameState s = runner.State;
            s.AwayDoubleDay = runner.TodayNumber;
            var away = new AwayReport { Paid = new BigNum(5000), Seconds = 3 * 3600 };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            return away;
        }

        [Test]
        public void The_Welcome_Back_Sheet_Offers_A_Video_For_Times_Two_Once_The_Free_One_Is_Spent()
        {
            GameState s = runner.State;
            var first = new AwayReport { Paid = new BigNum(5000), Seconds = 3 * 3600 };
            runner.ShowAwayForTest(first);
            screen.ShowAway(first);
            Assert.AreEqual(L10n.T("away.double.free"), screen.AwayCard.DoubleLabel);
            Assert.IsFalse(screen.AwayCard.Ad.Shown, "the free ×2 comes first; no video beside it");
            screen.AwayCard.Close();

            s.GoldenCarrots = 9;
            ComeBackAfterTheFreeDoubleIsSpent();
            Assert.AreEqual(L10n.T("away.double.cost", Away.DoubleCost), screen.AwayCard.DoubleLabel, "the golden ×2 stays the main button");
            Assert.IsTrue(screen.AwayCard.Ad.Shown);
            Assert.AreEqual(L10n.T("ad.away"), screen.AwayCard.Ad.Label);
            Assert.IsNull(screen.AwayCard.Ad.Root.GetComponent<PressDip>(), "a secondary button, not a chunky main one");

            BigNum before = s.Coins;
            Find("Canvas/Away/Card/Ad").onClick.Invoke();
            Assert.AreEqual(L10n.T("ad.busy"), screen.AwayCard.Ad.Label, "the video plays for a moment");
            Assert.AreEqual(before.ToDouble(), s.Coins.ToDouble(), 1e-6, "nothing until it was watched");
            fake.Finish();
            Assert.AreEqual((before + new BigNum(5000)).ToDouble(), s.Coins.ToDouble(), 1e-6, "paid once more");
            Assert.AreEqual(9, s.GoldenCarrots, "no golden carrots spent");
            Assert.IsFalse(screen.AwayCard.IsOpen);
            Assert.IsFalse(runner.DoubleAway(), "one doubling per time away");

            ComeBackAfterTheFreeDoubleIsSpent();
            Assert.IsFalse(screen.AwayCard.Ad.Shown);
            Assert.IsTrue(screen.AwayCard.DoubleEnabled);
        }

        [Test]
        public void A_Minute_Away_While_The_Sheet_Is_Up_Leaves_Its_Video_Times_Two_On_The_Report_It_Shows()
        {
            GameState s = runner.State;
            s.MaxDaySeen = runner.TodayNumber + 1;
            AwayReport shown = ComeBackAfterTheFreeDoubleIsSpent();
            var minute = new AwayReport { Paid = new BigNum(40), Seconds = 60 };
            runner.ShowAwayForTest(minute);
            screen.ShowAway(minute);
            Assert.IsTrue(screen.AwayCard.IsOpen);
            Assert.IsTrue(screen.AwayCard.Ad.Shown, "the video ×2 is still offered for the shown report");

            BigNum before = s.Coins;
            Find("Canvas/Away/Card/Ad").onClick.Invoke();
            fake.Finish();
            Assert.AreEqual((before + shown.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6, "the video pays the report the sheet shows");
            Assert.IsFalse(screen.AwayCard.IsOpen);

            runner.ShowAwayForTest(minute);
            screen.ShowAway(minute);
            Assert.AreEqual(AdOffer.None, runner.AdOfferFor(Ads.AwayDouble), "no second video ×2 for the same report");
            Assert.IsFalse(runner.DoubleAway(), "nor a golden one");
            Assert.AreEqual((before + shown.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6);
        }

        [Test]
        public void A_Video_Closed_Before_The_End_Pays_Nothing()
        {
            ComeBackAfterTheFreeDoubleIsSpent();
            BigNum before = runner.State.Coins;
            fake.NextResult = false;
            Find("Canvas/Away/Card/Ad").onClick.Invoke();
            fake.Finish();
            Assert.AreEqual(before.ToDouble(), runner.State.Coins.ToDouble(), 1e-6);
            Assert.IsTrue(screen.AwayCard.IsOpen);
            Assert.IsTrue(screen.AwayCard.Ad.Shown, "the day's one is not used up");
        }

        [Test]
        public void The_Shop_Gives_A_Golden_Carrot_For_A_Video_Twice_A_Day()
        {
            screen.OpenShop();
            screen.Refresh();
            Assert.IsTrue(screen.Shop.Ad.Shown);
            Assert.AreEqual(L10n.T("ad.shop"), screen.Shop.Ad.Label);
            for (int i = 0; i < Ads.ShopGoldenPerDay; i++)
            {
                Find("Canvas/Shop/Card/Have/Ad").onClick.Invoke();
                fake.Finish();
            }
            Assert.AreEqual(Ads.ShopGoldenPerDay, runner.State.GoldenCarrots);
            Assert.IsFalse(screen.Shop.Ad.Shown, "two a day, then the pill goes");
        }

        [Test]
        public void A_Game_Whose_Rounds_Are_Spent_Offers_One_More_Round_Once()
        {
            GameState s = runner.State;
            s.GardenXp = Garden.XpForLevel(12);
            s.PoundingDay = runner.TodayNumber;
            s.PoundingPlays = Pounding.PlaysPerDay;
            screen.Refresh();
            Assert.IsTrue(screen.PoundAd.Shown);
            Assert.AreEqual(L10n.T("ad.round"), screen.PoundAd.Label);
            Assert.IsFalse(screen.LeavesAd.Shown, "the basket still has its rounds");

            screen.PoundAd.Button.onClick.Invoke();
            fake.Finish();
            Assert.IsTrue(screen.Pound.IsOpen, "the extra round starts at once");
            Assert.AreEqual(0, Pounding.PlaysLeft(s, runner.TodayNumber));
            screen.Refresh();
            Assert.IsFalse(screen.PoundAd.Shown, "one extra round a day");
        }

        [Test]
        public void Without_A_Provider_No_Ad_Button_And_No_Pass_Is_Shown()
        {
            runner.AdProvider = new NoAds();
            GameState s = runner.State;
            s.GardenXp = Garden.XpForLevel(12);
            s.PoundingDay = runner.TodayNumber;
            s.PoundingPlays = Pounding.PlaysPerDay;
            screen.OpenShop();
            screen.Refresh();
            ComeBackAfterTheFreeDoubleIsSpent();
            Assert.IsFalse(screen.AwayCard.Ad.Shown);
            Assert.IsFalse(screen.Shop.Ad.Shown);
            Assert.IsFalse(screen.PoundAd.Shown);
            screen.AwayCard.Close();
            screen.GoldenPacks.Open(runner.Store);
            Assert.IsFalse(screen.GoldenPacks.PassShown, "nothing to remove");
            Assert.AreEqual(GoldenPacksView.CardHeight + GoldenPacksView.MoonExtra, ((RectTransform)root.transform.Find("Canvas/GoldenPacks/Card")).sizeDelta.y, "only the moon pass card below the packs");
        }

        [Test]
        public void The_No_Ads_Pass_Is_Bought_At_The_Stores_Price_And_Turns_Every_Button_Into_Get_Now()
        {
            screen.OpenShop();
            Find("Canvas/Shop/Card/Have/BuyMore").onClick.Invoke();
            Assert.IsTrue(screen.GoldenPacks.PassShown);
            Transform card = root.transform.Find("Canvas/GoldenPacks/Card");
            Assert.AreEqual(runner.Store.PriceOf(Storefront.NoAdsPass), card.Find("NoAds/Face/PriceChip/Price").GetComponent<Text>().text, "the store's own price");
            Assert.AreEqual(L10n.T("noads.title"), screen.GoldenPacks.PassLabel);

            Find("Canvas/GoldenPacks/Card/NoAds").onClick.Invoke();
            Assert.AreEqual(runner.TodayNumber + Ads.PassDays, runner.State.NoAdsUntilDay);
            Assert.AreEqual(0, runner.State.GoldenCarrots, "no golden carrots in a pass");
            Assert.AreEqual(L10n.T("noads.paid", Ads.PassDays), screen.GoldenPacks.Status);
            Assert.AreEqual(L10n.T("noads.active", Ads.PassDays), screen.GoldenPacks.PassLabel);
            screen.GoldenPacks.Close();

            screen.Refresh();
            Assert.AreEqual(L10n.T("ad.shop.now"), screen.Shop.Ad.Label);
            Find("Canvas/Shop/Card/Have/Ad").onClick.Invoke();
            Assert.AreEqual(1, runner.State.GoldenCarrots, "at once, no video");
            Assert.IsEmpty(fake.Shown);
            Find("Canvas/Shop/Card/Have/Ad").onClick.Invoke();
            Assert.AreEqual(2, runner.State.GoldenCarrots);
            Assert.IsFalse(screen.Shop.Ad.Shown, "the same two a day");

            runner.AdProvider = new NoAds();
            ComeBackAfterTheFreeDoubleIsSpent();
            Assert.IsTrue(screen.AwayCard.Ad.Shown);
            Assert.AreEqual(L10n.T("ad.away.now"), screen.AwayCard.Ad.Label);
        }

        [Test]
        public void Every_Ad_Button_Is_Big_Enough_To_Touch()
        {
            ComeBackAfterTheFreeDoubleIsSpent();
            GameState s = runner.State;
            s.GardenXp = Garden.XpForLevel(12);
            s.PoundingDay = runner.TodayNumber;
            s.PoundingPlays = Pounding.PlaysPerDay;
            screen.Refresh();
            foreach (AdButton button in new[] { screen.AwayCard.Ad, screen.Shop.Ad, screen.PoundAd, screen.LeavesAd, screen.JarAd })
            {
                Vector2 size = button.Root.rect.size;
                Assert.GreaterOrEqual(Mathf.Min(size.x, size.y), UiKit.MinTouch, button.Root.name);
            }
        }
    }
}

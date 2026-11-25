using System.Collections.Generic;
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
    public class MonetizationFlagScreenTests
    {
        const long Hour = 3600L;
        const string Pass = "Canvas/Sheet/Panel/Quests/Content/MonthPassRow/";
        static readonly long Tuesday = GiftCodes.Day(2026, 11, 24);
        static readonly string[] MoneyWords =
        {
            "₫", "gói", "Gói", "Vé trăng", "quảng cáo", "Xem 30 giây", "Khôi phục giao dịch", "Mua thêm",
            "pack", "Pack", "Moon pass", "No ads", "Watch 30", "Restore purchases", "Buy more"
        };

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics mem;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Open(int level, System.Action<GameState> before = null)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            mem = new MemoryAnalytics();
            runner.Analytics = mem;
            runner.DayClock = () => Tuesday;
            runner.OfferWallClock = () => Tuesday * Offers.DaySeconds + 10 * Hour;
            runner.OfferCardsOn = true;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.GardenXp = Garden.XpForLevel(level);
            Rabbits.GiftFirst(s, "Bông");
            before?.Invoke(s);
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
            return s;
        }

        Transform At(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t;
        }

        void NoMoneyOnScreen(string where)
        {
            Canvas.ForceUpdateCanvases();
            foreach (Text text in root.GetComponentsInChildren<Text>(false))
                foreach (string word in MoneyWords)
                    StringAssert.DoesNotContain(word, text.text, where + ": " + text.transform.name + " says \"" + text.text + "\"");
        }

        void NothingSold()
        {
            Assert.IsFalse(runner.StoreMade, "no storefront is made, so no store SDK starts");
            Assert.IsFalse(runner.AdsMade, "no video provider is made");
            foreach (MemoryAnalytics.Event e in mem.Events)
                CollectionAssert.DoesNotContain(AnalyticsEvents.Monetization, e.Name, "no purchase, offer, ad or pass event");
        }

        [Test, MonetizationOn(false)]
        public void Off_The_Shop_Has_No_Packs_Card_No_Pack_Offer_No_Video_And_No_Price()
        {
            GameState s = Open(10);
            Assert.IsFalse(screen.OfferCard.IsOpen, "no newcomer or level pack card comes");
            Assert.IsNull(runner.LeadingOffer);
            Assert.IsEmpty(s.OfferRecords, "no pack clock starts, so turning selling on later starts them fresh");

            screen.OpenShop();
            screen.Refresh();
            Assert.IsTrue(screen.Shop.IsOpen);
            Assert.IsFalse(screen.Shop.BuyMoreShown, "no Buy more card for golden carrot packs");
            Assert.IsFalse(screen.Shop.OfferShown, "no pack waiting at the top of the shop");
            Assert.IsFalse(screen.Shop.Ad.Shown, "no video for a golden carrot");
            Assert.IsTrue(At("Canvas/Shop/Card/Golden/Call").gameObject.activeInHierarchy, "golden carrots earned in play still call rabbits");
            NoMoneyOnScreen("shop");

            screen.GoldenPacks.Open(null);
            Assert.IsFalse(screen.GoldenPacks.IsOpen, "the packs, moon pass, no-ads pass and Restore never open");
            NothingSold();
        }

        [Test, MonetizationOn(false)]
        public void Off_Nothing_On_Screen_Makes_A_Store_Or_A_Video_Provider()
        {
            Open(10);
            screen.OpenShop();
            screen.Refresh();
            screen.Shop.Close();
            screen.OpenSheet(2);
            screen.Refresh();
            screen.CloseSheet();
            screen.Shell.OpenPause();
            screen.Refresh();
            screen.Shell.Resume();
            screen.Advance(1f);
            NothingSold();
        }

        [Test, MonetizationOn(false)]
        public void Off_The_Month_Pass_Shows_Only_Its_Free_Row()
        {
            GameState s = Open(8, g => { g.PassMonth = MonthPass.MonthOf(Tuesday); g.PassPoints = 2 * MonthPass.PointsPerTier; });
            screen.OpenSheet(2);
            screen.Refresh();
            Assert.IsTrue(At(Pass + "Free").gameObject.activeSelf);
            Assert.IsFalse(At(Pass + "Premium").gameObject.activeSelf, "no paid row");
            Assert.IsFalse(At(Pass + "PremiumLabel").gameObject.activeSelf);
            Assert.IsFalse(At(Pass + "Buy").gameObject.activeSelf, "no Unlock button and no price");
            Assert.AreEqual(MonthPassView.FreeOnlyHeight, At(Pass.TrimEnd('/')).GetComponent<LayoutElement>().preferredHeight, "the card shrinks to its one row");
            NoMoneyOnScreen("month pass");

            At(Pass + "Free/Tier 1").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(MonthPass.Claimed(s, MonthRow.Free, 1), "the free row pays as before");
            NothingSold();
        }

        [Test, MonetizationOn(false)]
        public void Off_A_Save_That_Bought_The_Moon_Pass_And_The_Month_Pass_Keeps_Both()
        {
            GameState s = Open(8, g =>
            {
                g.MaxDaySeen = Tuesday;
                MoonPass.Grant(g, Tuesday - 2);
                g.PassMonth = MonthPass.MonthOf(Tuesday);
                MonthPass.Buy(g, Tuesday);
                g.PassPoints = 2 * MonthPass.PointsPerTier;
            });
            Assert.AreEqual(MoonPass.Days - 2, runner.MoonPassDaysLeft, "the moon pass keeps its days");
            Assert.IsTrue(screen.MoonPassPill.gameObject.activeSelf, "and its pill");
            Assert.IsTrue(MonthPass.Premium(s, Tuesday));

            screen.OpenSheet(2);
            screen.Refresh();
            Assert.IsTrue(At(Pass + "Premium").gameObject.activeSelf, "the paid row stays for who paid");
            Assert.IsFalse(At(Pass + "Premium/Lock").gameObject.activeSelf);
            Assert.IsTrue(At(Pass + "Premium/Tier 2/Badge").gameObject.activeSelf);
            Assert.IsFalse(At(Pass + "Buy").gameObject.activeSelf);
            At(Pass + "Premium/Tier 2").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(MonthPass.Claimed(s, MonthRow.Premium, 2), "and still pays");

            int letters = Mailbox.Check(s, Tuesday + 1);
            Assert.Greater(letters, 0);
            Assert.IsNotNull(s.Letters.Find(l => l.Kind == LetterKind.MoonPassDay && l.Day == Tuesday + 1), "the paid day's carrots still come by mail");
            Assert.IsNull(s.Letters.Find(l => l.Kind == LetterKind.MoonPassRenew));
            NothingSold();
        }

        [Test, MonetizationOn(false)]
        public void Off_A_Save_That_Bought_The_No_Ads_Pass_Keeps_Its_Get_Now_Buttons_Without_Any_Video()
        {
            GameState s = Open(8, g => Ads.GrantPass(g, Tuesday));
            screen.OpenShop();
            screen.Refresh();
            Assert.IsTrue(screen.Shop.Ad.Shown, "the paid pass still pays at once");
            Assert.AreEqual(L10n.T("ad.shop.now"), screen.Shop.Ad.Label);
            Assert.IsFalse(screen.Shop.BuyMoreShown);
            At("Canvas/Shop/Card/Have/Ad").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(Ads.ShopGoldenReward, s.GoldenCarrots);
            NoMoneyOnScreen("shop with a no-ads pass");
            NothingSold();
        }

        [Test, MonetizationOn(false)]
        public void Off_No_Video_Button_Anywhere_Even_With_A_Video_Provider_At_Hand()
        {
            GameState s = Open(12);
            runner.AdProvider = new FakeRewardedAds();
            s.PoundingDay = runner.TodayNumber;
            s.PoundingPlays = Pounding.PlaysPerDay;
            s.AwayDoubleDay = runner.TodayNumber;
            var away = new AwayReport { Paid = new BigNum(5000), Seconds = 3 * 3600 };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            screen.OpenShop();
            screen.Refresh();
            foreach (string placement in Ads.Placements)
                Assert.AreEqual(AdOffer.None, runner.AdOfferFor(placement, Ads.GamePounding), placement);
            foreach (AdButton button in new[] { screen.AwayCard.Ad, screen.Shop.Ad, screen.PoundAd, screen.LeavesAd, screen.JarAd })
                Assert.IsFalse(button.Shown, button.Root.name);
            Assert.IsTrue(screen.AwayCard.IsOpen, "time away still pays");
            Assert.IsFalse(runner.AdsOn);
        }

        [Test, MonetizationOn(false)]
        public void Off_Buying_And_Restoring_Do_Nothing_And_Log_Nothing()
        {
            GameState s = Open(8);
            int golden = s.GoldenCarrots;
            var results = new List<PurchaseResult>();
            runner.BuyGolden(Storefront.Medium, results.Add);
            runner.BuyGolden(Offers.Starter, results.Add);
            runner.BuyMoonPass(results.Add);
            runner.BuyMonthPass(results.Add);
            runner.BuyNoAdsPass(results.Add);
            bool? restored = null;
            runner.RestorePurchases(ok => restored = ok);
            Assert.AreEqual(5, results.Count);
            foreach (PurchaseResult r in results) Assert.AreEqual(PurchaseOutcome.Unavailable, r.Outcome, r.PackId);
            Assert.AreEqual(false, restored);
            Assert.AreEqual(golden, s.GoldenCarrots);
            Assert.AreEqual(0, runner.MoonPassDaysLeft);
            Assert.IsFalse(MonthPass.Premium(s, Tuesday));
            Assert.IsNull(runner.PriceOf(MonthPass.ProductId), "no price anywhere");
            NothingSold();
        }

        [Test, MonetizationOn(false)]
        public void Off_The_Pause_Card_And_Its_Settings_Name_No_Pack_Pass_Or_Price()
        {
            Open(8);
            screen.Shell.OpenPause();
            screen.Refresh();
            NoMoneyOnScreen("pause");
            At("Canvas/Pause/Card/Profile").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.Profile.IsOpen);
            NoMoneyOnScreen("settings");
            L10n.SetLocale("en");
            screen.RebuildUi();
            screen.Shell.OpenPause();
            screen.Refresh();
            NoMoneyOnScreen("pause en");
        }

        [Test, MonetizationOn(false)]
        public void Off_Intro_Cards_And_Quests_Name_No_Pack_Pass_Or_Price()
        {
            Open(8);
            foreach (string locale in new[] { "vi", "en" })
            {
                L10n.SetLocale(locale);
                foreach (string id in Intros.All)
                    foreach (string word in MoneyWords)
                    {
                        StringAssert.DoesNotContain(word, L10n.T("intro." + id + ".title"), locale + " " + id);
                        StringAssert.DoesNotContain(word, L10n.T("intro." + id + ".body"), locale + " " + id);
                    }
                foreach (QuestDef q in runner.QuestDefs)
                    foreach (string word in MoneyWords)
                        StringAssert.DoesNotContain(word, L10n.T("quest." + q.Id), locale + " " + q.Id);
            }
        }

        [Test, MonetizationOn]
        public void On_Everything_Is_Sold_As_Before()
        {
            Open(Offers.StarterLevel, g => { g.PassMonth = MonthPass.MonthOf(Tuesday); g.PlaySeconds = Offers.StarterQuietSeconds; });
            screen.Refresh();
            screen.Advance(0.1f);
            Assert.IsTrue(screen.OfferCard.IsOpen, "the starter pack card comes");
            screen.OfferCard.Close();
            screen.OpenShop();
            screen.Refresh();
            Assert.IsTrue(screen.Shop.BuyMoreShown);
            Assert.IsTrue(screen.Shop.OfferShown);
            Assert.IsTrue(screen.Shop.Ad.Shown);
            At("Canvas/Shop/Card/Have/BuyMore").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.GoldenPacks.IsOpen);
            Assert.IsTrue(At("Canvas/GoldenPacks/Card/Restore").gameObject.activeInHierarchy);
            screen.GoldenPacks.Close();
            screen.Shop.Close();
            screen.OpenSheet(2);
            screen.Refresh();
            Assert.IsTrue(At(Pass + "Premium").gameObject.activeSelf);
            Assert.IsTrue(At(Pass + "Buy").gameObject.activeSelf);
            Assert.AreEqual(MonthPassView.Height, At(Pass.TrimEnd('/')).GetComponent<LayoutElement>().preferredHeight);
            Assert.IsTrue(runner.StoreMade);
            Assert.IsTrue(runner.AdsMade);
            Assert.IsNotNull(mem.Last(AnalyticsEvents.OfferShown));
        }

        [Test]
        public void The_Default_Video_Provider_Follows_The_Flag()
        {
            bool before = Features.Monetization;
            try
            {
                Features.Monetization = false;
                var off = new GameObject("Off").AddComponent<GameRunner>();
                Assert.IsInstanceOf<NoAds>(off.AdProvider);
                Object.DestroyImmediate(off.gameObject);
                Features.Monetization = true;
                var on = new GameObject("On").AddComponent<GameRunner>();
                Assert.IsInstanceOf<FakeRewardedAds>(on.AdProvider);
                Object.DestroyImmediate(on.gameObject);
            }
            finally { Features.Monetization = before; }
        }
    }
}

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
    public class OfferScreenTests
    {
        const long Hour = 3600L;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics mem;
        FakeStorefront store;
        long wall;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
            wall = 20_800L * Offers.DaySeconds + 10 * Hour;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Open(int level, bool withScreen = true, bool cards = true, bool tutorialDone = true)
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
            runner.DayClock = () => wall / Offers.DaySeconds;
            runner.OfferWallClock = () => wall;
            runner.OfferCardsOn = cards;
            store = new FakeStorefront();
            runner.Store = store;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.TutorialStep = tutorialDone ? Tutorial.Steps : 0;
            s.PlaySeconds = Offers.StarterQuietSeconds;
            s.HintSeen = true;
            s.GardenXp = Garden.XpForLevel(level);
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            if (!withScreen) return s;
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            return s;
        }

        void NoteOffers() =>
            typeof(GameRunner).GetMethod("NoteOffers", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(runner, null);

        Button Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            Button b = t.GetComponent<Button>();
            b.onClick.Invoke();
            return b;
        }

        [Test]
        public void The_Starter_Card_Comes_Once_In_The_Middle_And_Then_Waits_At_The_Top_Of_The_Shop()
        {
            Open(Offers.StarterLevel);
            Assert.IsTrue(screen.OfferCard.IsOpen, "level 4 on a clear garden: the card comes");
            Assert.AreEqual(Offers.Starter, screen.OfferCard.Offer.Id);
            Assert.AreEqual(L10n.T("offer.left", "72:00:00"), screen.OfferCard.Left);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferShown));
            Assert.AreEqual(Offers.Starter, mem.Last(AnalyticsEvents.OfferShown)[AnalyticsEvents.ProductId].Text);
            Transform card = root.transform.Find("Canvas/Offer/Card");
            Assert.AreEqual(L10n.T("offer.once"), card.Find("Once/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.golden", 80), card.Find("Contents/Golden/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.jars", 3), card.Find("Contents/Jars/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.clovers", 3), card.Find("Contents/Clovers/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.hat", L10n.T("item." + Offers.StarterHat)), card.Find("Contents/Hat/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.buy", store.PriceOf(Offers.Starter)), card.Find("Buy/Face/Text").GetComponent<Text>().text, "the store's own price");

            Press("Canvas/Offer/Card/Close");
            Assert.IsFalse(screen.OfferCard.IsOpen);
            wall += Hour;
            screen.Refresh();
            Assert.IsFalse(screen.OfferCard.IsOpen, "once is once");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferShown));

            screen.OpenShop();
            screen.Refresh();
            Assert.IsTrue(screen.Shop.OfferShown);
            Assert.AreEqual(L10n.T("offer.starter.title"), screen.Shop.OfferText);
            Assert.AreEqual(L10n.T("offer.left", "71:00:00"), screen.Shop.OfferLeft);
            Assert.AreEqual(L10n.T("offer.once"), root.transform.Find("Canvas/Shop/Card/Offer/Face/Once/Text").GetComponent<Text>().text);
            Press("Canvas/Shop/Card/Offer");
            Assert.IsTrue(screen.OfferCard.IsOpen, "the shop's row opens the card");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferShown), "seen already");

            screen.OfferCard.Close();
            wall += 71 * Hour;
            NoteOffers();
            screen.Refresh();
            Assert.IsFalse(screen.Shop.OfferShown, "ended: gone from the shop for good");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferExpired));
        }

        [Test]
        public void The_Card_Never_Comes_Over_The_Shop_Another_Card_Or_The_Guidance()
        {
            GameState s = Open(Offers.StarterLevel - 1);
            screen.OpenShop();
            s.GardenXp = Garden.XpForLevel(Offers.StarterLevel);
            NoteOffers();
            screen.Refresh();
            Assert.IsFalse(screen.OfferCard.IsOpen, "not over the shop");
            Assert.IsTrue(screen.Shop.OfferShown, "but it is at the top of the shop already");
            screen.Shop.Close();
            screen.GoldenPacks.Open(runner.Store);
            screen.Refresh();
            Assert.IsFalse(screen.OfferCard.IsOpen, "not over another card");
            screen.GoldenPacks.Close();
            screen.Refresh();
            Assert.IsTrue(screen.OfferCard.IsOpen, "the first clear moment");
            Object.DestroyImmediate(root);

            Open(Offers.StarterLevel, tutorialDone: false);
            Assert.IsFalse(screen.OfferCard.IsOpen, "not during the guidance");
            Object.DestroyImmediate(root);

            Open(Offers.StarterLevel, cards: false);
            Assert.IsFalse(screen.OfferCard.IsOpen, "a garden that is not running the game shows no card");
            screen.OpenShop();
            screen.Refresh();
            Assert.IsTrue(screen.Shop.OfferShown);
        }

        [Test]
        public void Buying_The_Starter_Delivers_It_Once_And_A_Restore_Adds_Nothing()
        {
            GameState s = Open(Offers.StarterLevel);
            int jars = s.MoonJars, clovers = s.Clovers;
            Press("Canvas/Offer/Card/Buy");
            Assert.AreEqual(80, s.GoldenCarrots);
            Assert.AreEqual(jars + 3, s.MoonJars);
            Assert.AreEqual(clovers + 3, s.Clovers);
            Assert.IsTrue(Items.Owns(s, Offers.StarterHat));
            Assert.AreEqual(1, s.PacksBought);
            Assert.AreEqual(L10n.T("offer.paid"), screen.OfferCard.Status);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferBought));
            Assert.AreEqual(0L, mem.Last(AnalyticsEvents.OfferBought)[AnalyticsEvents.Late].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.PurchaseSuccess));

            PurchaseResult again = default;
            runner.BuyGolden(Offers.Starter, r => again = r);
            Assert.AreEqual(PurchaseOutcome.Unavailable, again.Outcome, "bought: not for sale again");
            Assert.AreEqual(1, store.Bought.Count, "the store was not even asked");
            store.Deliver(Offers.Starter, "GPA.restore");
            Assert.AreEqual(80, s.GoldenCarrots, "a restore hands back what was already given");
            Assert.AreEqual(1, s.PacksBought);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferBought));

            screen.OfferCard.Close();
            screen.OpenShop();
            screen.Refresh();
            Assert.IsFalse(screen.Shop.OfferShown, "bought: gone from the shop");
        }

        [Test]
        public void Cancel_A_Network_Error_And_Pending_Deliver_Nothing()
        {
            GameState s = Open(Offers.StarterLevel);
            var expected = new[]
            {
                (PurchaseOutcome.Cancelled, "packs.cancelled"), (PurchaseOutcome.Failed, "packs.failed"), (PurchaseOutcome.Pending, "offer.pending"),
            };
            foreach ((PurchaseOutcome outcome, string key) in expected)
            {
                store.NextOutcome = outcome;
                Press("Canvas/Offer/Card/Buy");
                Assert.AreEqual(L10n.T(key), screen.OfferCard.Status);
                Assert.AreEqual(0, s.GoldenCarrots, outcome + " gives nothing");
                Assert.AreEqual(0, s.PacksBought);
            }
            Assert.AreEqual(OfferState.Active, Offers.StateOf(s, Offers.Starter, runner.OfferNow), "still on sale");
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.OfferBought));
            Assert.AreEqual(new[] { "cancelled", "failed", "pending" },
                mem.All(AnalyticsEvents.PurchaseFailed).ConvertAll(e => e[AnalyticsEvents.Reason].Text).ToArray());
        }

        [Test]
        public void An_Ended_Pack_Cannot_Be_Bought_But_A_Payment_The_Store_Confirms_Later_Still_Arrives()
        {
            GameState s = Open(Offers.StarterLevel, withScreen: false);
            store.NextOutcome = PurchaseOutcome.Pending;
            PurchaseResult got = default;
            runner.BuyGolden(Offers.Starter, r => got = r);
            Assert.AreEqual(PurchaseOutcome.Pending, got.Outcome);
            Assert.AreEqual(0, s.GoldenCarrots);

            wall += 72 * Hour;
            NoteOffers();
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferExpired));
            Assert.AreEqual(Offers.Starter, mem.Last(AnalyticsEvents.OfferExpired)[AnalyticsEvents.ProductId].Text);
            runner.BuyGolden(Offers.Starter, r => got = r);
            Assert.AreEqual(PurchaseOutcome.Unavailable, got.Outcome);
            Assert.AreEqual(0, store.Bought.Count);

            store.Deliver(Offers.Starter, "GPA.pending-cleared");
            Assert.AreEqual(80, s.GoldenCarrots, "the player paid: the pack comes, ended or not");
            Assert.AreEqual(1, s.PacksBought);
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.OfferBought)[AnalyticsEvents.Late].Number);
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.PurchaseSuccess)[AnalyticsEvents.Late].Number);
            store.Deliver(Offers.Starter, "GPA.pending-cleared");
            Assert.AreEqual(80, s.GoldenCarrots);

            wall -= 100 * Hour;
            NoteOffers();
            Assert.AreEqual(OfferState.Bought, Offers.StateOf(s, Offers.Starter, runner.OfferNow));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.OfferExpired), "turning the clock back reopens and re-ends nothing");
        }

        [Test]
        public void A_Milestone_Pack_Shows_First_And_Its_Purple_Rabbit_Steps_Out_Of_The_Moon_Gate()
        {
            GameState s = Open(10);
            Assert.IsTrue(screen.OfferCard.IsOpen);
            Assert.AreEqual(Offers.Milestone10, screen.OfferCard.Offer.Id, "the 48-hour pack ends first, so it comes first");
            Transform card = root.transform.Find("Canvas/Offer/Card");
            Assert.AreEqual(L10n.T("offer.milestone.title", 10), card.Find("Title").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.rabbit", L10n.Rarity(Offers.MilestoneRabbitTier)), card.Find("Contents/Rabbit/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("offer.buy", store.PriceOf(Offers.Milestone10)), card.Find("Buy/Face/Text").GetComponent<Text>().text);
            int rabbits = s.Rabbits.Count;

            Press("Canvas/Offer/Card/Buy");
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Rabbit arrived = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual(Offers.MilestoneRabbitTier, arrived.Tier);
            Assert.IsTrue(arrived.FirstOfKind);
            Assert.AreEqual(150, s.GoldenCarrots);
            Assert.IsTrue(screen.Arrival.IsOpen, "the rabbit gets its moment");
            Assert.IsFalse(screen.OfferCard.IsOpen);
        }
    }
}

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
    public class WelcomeBackScreenTests
    {
        const long Hour = 3600L;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics log;
        long today;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
            today = 20_800L;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            Prefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Open(int awayDays, int level = 2, bool newcomer = false, bool start = true)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            log = new MemoryAnalytics();
            runner.Analytics = log;
            runner.DayClock = () => today;
            runner.OfferWallClock = () => today * Offers.DaySeconds + 10 * Hour;
            runner.OfferCardsOn = true;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            Rabbit loved = Rabbits.ArriveKind(s, "tho-me", "Mè");
            Rabbits.AddBond(s, loved, Rabbits.PointsFor(4));
            s.TutorialStep = Tutorial.Steps;
            s.PlaySeconds = Offers.StarterQuietSeconds;
            s.HintSeen = true;
            s.GardenXp = Garden.XpForLevel(level);
            s.FirstDaySeen = today - awayDays - 5;
            s.MaxDaySeen = today - awayDays;
            s.WelcomeBack.SeenDay = s.MaxDaySeen;
            s.Newcomer.Closed = !newcomer;
            if (newcomer) s.FirstDaySeen = s.MaxDaySeen - 1;
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            if (start) screen.Shell.StartGame();
            return s;
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        void Away()
        {
            var away = new AwayReport { Paid = new BigNum(5000), Seconds = 3 * Hour, Capped = true };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Assert.IsTrue(screen.AwayCard.IsOpen);
        }

        [Test]
        public void After_Four_Days_Away_The_Card_Follows_The_Away_Sheet_With_The_Most_Loved_Rabbit_And_Three_Gifts()
        {
            GameState s = Open(4, start: false);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.WelcomeBack));
            Assert.AreEqual(4, log.Last(AnalyticsEvents.WelcomeBack)[AnalyticsEvents.DaysAway].Number);
            Assert.AreEqual(5, s.GoldenCarrots);
            Away();
            screen.Refresh();
            Assert.IsFalse(screen.WelcomeCard.IsOpen, "not over the title");
            screen.Shell.StartGame();
            screen.Refresh();
            Assert.IsFalse(screen.WelcomeCard.IsOpen, "the away sheet first");
            screen.AwayCard.Close();
            screen.Refresh();
            Assert.IsTrue(screen.WelcomeCard.IsOpen, "then the card");
            Assert.AreEqual(L10n.T("welcome.sub", 4), screen.WelcomeCard.Sub);
            Assert.AreEqual(L10n.T("welcome.line", 4), screen.WelcomeCard.Line);
            Assert.IsNotNull(root.transform.Find("Canvas/WelcomeBackCard/Card/Rabbit/PortraitHolder").GetChild(0), "the most-loved rabbit's portrait");
            Assert.AreEqual("Mè", root.transform.Find("Canvas/WelcomeBackCard/Card/Rabbit/Name").GetComponent<Text>().text);
            StringAssert.Contains("+2", screen.WelcomeCard.GiftText);
            StringAssert.Contains("+3", screen.WelcomeCard.GiftText);
            StringAssert.Contains("+5", screen.WelcomeCard.GiftText);
            Assert.IsTrue(screen.ReviewBusy, "no review ask over it");
            Assert.IsFalse(screen.OfferCardFree, "no offer over it");

            screen.WelcomeCard.Close();
            screen.Refresh();
            Assert.IsFalse(screen.WelcomeCard.IsOpen, "once seen it does not come back by itself");
        }

        [Test, MonetizationOn]
        public void The_Card_Comes_Before_The_Starter_Offer_And_The_Offer_Waits_Its_Turn()
        {
            Open(5, Offers.StarterLevel);
            screen.Refresh();
            Assert.IsTrue(screen.WelcomeCard.IsOpen);
            Assert.IsFalse(screen.OfferCard.IsOpen, "one card at a time");
            screen.WelcomeCard.Close();
            screen.Refresh();
            Assert.IsTrue(screen.OfferCard.IsOpen, "the offer after it");
            Assert.IsFalse(screen.WelcomeCard.IsOpen);
        }

        [Test]
        public void The_Pill_Sits_In_The_First_Week_Slot_Counts_The_Days_And_Checks_In_Once_A_Day()
        {
            GameState s = Open(6);
            screen.Refresh();
            screen.WelcomeCard.Close();
            screen.Refresh();
            Assert.IsTrue(screen.WelcomeCard.PillShown);
            Assert.IsFalse(screen.FirstWeek.PillShown, "never both");
            Assert.AreEqual("Mừng bạn về · ngày 1/7", screen.WelcomeCard.PillText);
            Assert.IsTrue(screen.WelcomeCard.PillBadge);
            var pill = (RectTransform)root.transform.Find("Canvas/Hud/WelcomeBack");
            var first = (RectTransform)root.transform.Find("Canvas/Hud/Newcomer");
            Assert.AreEqual(first.anchoredPosition, pill.anchoredPosition, "the first-week pill's slot");
            Assert.AreEqual(first.sizeDelta, pill.sizeDelta);
            var corner = (RectTransform)root.transform.Find("Canvas/Hud/Corner");
            Assert.Less(pill.anchoredPosition.y + pill.sizeDelta.y / 2f, corner.anchoredPosition.y - corner.sizeDelta.y / 2f, "below the corner plate");

            Press("Canvas/Hud/WelcomeBack");
            Assert.IsTrue(screen.WelcomeCard.IsOpen);
            Assert.AreEqual(L10n.T("welcome.claim", 1), screen.WelcomeCard.ClaimLabel);
            int jars = s.MoonJars;
            Press("Canvas/WelcomeBackCard/Card/Claim");
            Assert.AreEqual(jars + 1, s.MoonJars);
            Assert.AreEqual(L10n.T("welcome.tomorrow"), screen.WelcomeCard.ClaimLabel);
            Assert.IsFalse(screen.WelcomeCard.ClaimEnabled);
            Press("Canvas/WelcomeBackCard/Card/Claim");
            Assert.AreEqual(jars + 1, s.MoonJars, "once a day");
            Assert.IsFalse(screen.WelcomeCard.PillBadge);
            Assert.AreEqual("Mừng bạn về · ngày 1/7", screen.WelcomeCard.PillText);

            today += 2;
            screen.Refresh();
            Assert.AreEqual("Mừng bạn về · ngày 2/7", screen.WelcomeCard.PillText);
            Assert.IsTrue(screen.WelcomeCard.PillBadge);
        }

        [Test]
        public void A_Player_In_The_First_Week_Sees_Only_The_First_Week_Pill()
        {
            Open(4, 2, newcomer: true);
            screen.Refresh();
            Assert.AreEqual(0, log.Count(AnalyticsEvents.WelcomeBack));
            Assert.IsTrue(screen.FirstWeek.PillShown);
            Assert.IsFalse(screen.WelcomeCard.PillShown);
            Assert.IsFalse(screen.WelcomeCard.IsOpen);
        }

        [Test]
        public void The_Card_And_Pill_Read_In_English_And_Fit_At_Large_Text()
        {
            L10n.SetLocale("en");
            UiKit.SetLargeText(true);
            Open(15);
            screen.Refresh();
            Assert.IsTrue(screen.WelcomeCard.IsOpen);
            StringAssert.Contains("+10 golden carrots", screen.WelcomeCard.GiftText, "fourteen days or more doubles the gifts");
            Assert.AreEqual("Welcome home · day 1/7", screen.WelcomeCard.PillText);
            Text line = root.transform.Find("Canvas/WelcomeBackCard/Card/Rabbit/Line").GetComponent<Text>();
            Assert.IsTrue(line.resizeTextForBestFit);
        }
    }
}

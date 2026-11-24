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
    public class NewcomerScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        MemoryAnalytics log;
        long today = GiftCodes.Day(2026, 11, 24);

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
            runner.DayClock = () => today;
            log = new MemoryAnalytics();
            runner.Analytics = log;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
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
        public void The_Pill_Waits_For_The_Guidance_Then_Counts_What_Can_Be_Claimed()
        {
            Assert.IsFalse(screen.FirstWeek.PillShown, "not during the guidance");
            runner.State.TutorialStep = Tutorial.Steps;
            screen.Refresh();
            Assert.IsTrue(screen.FirstWeek.PillShown);
            Assert.AreEqual("Tuần đầu · ngày 1", screen.FirstWeek.PillText);
            Assert.AreEqual(0, screen.FirstWeek.PillBadge);
            runner.State.Levels[0] = 25;
            screen.Refresh();
            Assert.AreEqual(1, screen.FirstWeek.PillBadge, "the teapot at 25 waits to be claimed");
            var pill = (RectTransform)root.transform.Find("Canvas/Hud/Newcomer");
            Assert.IsNotNull(pill);
            var corner = (RectTransform)root.transform.Find("Canvas/Hud/Corner");
            Assert.AreEqual(corner.anchoredPosition.x, pill.anchoredPosition.x, 0.01f, "it hangs under the corner cluster");
            Assert.Less(pill.anchoredPosition.y + pill.sizeDelta.y / 2f, corner.anchoredPosition.y - corner.sizeDelta.y / 2f, "below the plate, not on it");
        }

        [Test]
        public void Claiming_From_The_Panel_Pays_Logs_And_Leaves_Later_Days_Shut()
        {
            GameState s = runner.State;
            s.TutorialStep = Tutorial.Steps;
            s.Levels[0] = 25;
            screen.Refresh();
            Find("Canvas/Hud/Newcomer").onClick.Invoke();
            Assert.IsTrue(screen.FirstWeek.IsOpen);
            Assert.AreEqual(1, screen.FirstWeek.Day);
            int clovers = s.Clovers;
            Find("Canvas/NewcomerSheet/Card/Task 0/Claim").onClick.Invoke();
            Assert.IsTrue(Newcomer.IsClaimed(Newcomer.At(1, 0), s));
            Assert.AreEqual(clovers + Newcomer.TaskClovers[0], s.Clovers);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.NewcomerTask));
            Assert.IsFalse(root.transform.Find("Canvas/NewcomerSheet/Card/Task 0/Claim").gameObject.activeSelf, "claimed once");
            Assert.IsTrue(root.transform.Find("Canvas/NewcomerSheet/Card/Task 0/Done").gameObject.activeSelf);

            screen.FirstWeek.Select(3);
            Text locked = root.transform.Find("Canvas/NewcomerSheet/Card/Task 0/Locked").GetComponent<Text>();
            Assert.IsTrue(locked.gameObject.activeSelf);
            Assert.AreEqual("Mở sau 2 ngày", locked.text);
            Assert.IsFalse(root.transform.Find("Canvas/NewcomerSheet/Card/Task 0/Claim").gameObject.activeSelf);
            Assert.AreEqual(0.55f, root.transform.Find("Canvas/NewcomerSheet/Card/Day 3").GetComponent<CanvasGroup>().alpha, 0.01f, "a day not yet come is faded");
        }

        [Test]
        public void The_Day_Seven_Rabbit_Is_The_Kind_The_Panel_Shows()
        {
            GameState s = runner.State;
            s.TutorialStep = Tutorial.Steps;
            s.FirstDaySeen = today - 6;
            s.Newcomer.Claimed = (1 << Newcomer.Tasks) - 1 - Newcomer.At(7, 3).Bit;
            s.Newcomer.DaysPaid = (1 << 6) - 1;
            s.DailyQuestsToday = 4;
            screen.Refresh();
            string shown = Newcomer.PurpleKind(s, Newcomer.ShownKindRoll);
            Find("Canvas/Hud/Newcomer").onClick.Invoke();
            Assert.AreEqual(7, screen.FirstWeek.Day);
            int rabbits = s.Rabbits.Count;
            Find("Canvas/NewcomerSheet/Card/Task 3/Claim").onClick.Invoke();
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Rabbit gift = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual(shown, gift.Kind, "the portrait is a promise");
            Assert.AreEqual(Newcomer.PurpleTier, gift.Tier);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.NewcomerDay));
            Assert.IsFalse(screen.FirstWeek.PillShown, "all 28 claimed: the pill goes");
        }

        [Test]
        public void The_Pill_Moves_Below_A_Waiting_Guest_And_Leaves_After_Day_Fourteen()
        {
            GameState s = runner.State;
            s.TutorialStep = Tutorial.Steps;
            screen.Refresh();
            var pill = (RectTransform)root.transform.Find("Canvas/Hud/Newcomer");
            Assert.AreEqual(NewcomerView.PillY, pill.anchoredPosition.y, 0.01f);
            Assert.IsNotNull(runner.PlaceOrder());
            screen.Refresh();
            Assert.AreEqual(NewcomerView.PillYUnderOrder, pill.anchoredPosition.y, 0.01f, "clear of the guest's pill");
            today += Newcomer.MailAfterDays;
            Assert.AreEqual(today, runner.TodayNumber);
            Assert.AreEqual(Newcomer.MailAfterDays, Seasons.DaysPlayed(s));
            screen.Refresh();
            Assert.IsFalse(screen.FirstWeek.PillShown);
        }
    }
}

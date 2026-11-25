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
    public class QuestChestsScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        GameState s;

        static readonly long Tuesday = GiftCodes.Day(2026, 11, 24);
        const string Quests = "Canvas/Sheet/Panel/Quests/Content/";

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
            runner.DayClock = () => Tuesday;
            s = GameState.NewGame(catalog.BuildDefs());
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            Rabbits.GiftFirst(s, "Bông");
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.OpenSheet(2);
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        void ClaimDailies(int count)
        {
            int n = 0;
            foreach (QuestDef q in runner.QuestDefs)
                if (q.Daily && n < count) { s.Claimed.Add(q.Id); n++; }
        }

        Text T(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Text>();
        }

        [Test]
        public void The_Two_Rows_Stand_At_The_Top_Of_The_Quests_Tab()
        {
            Transform content = root.transform.Find(Quests);
            Assert.AreEqual("QuestBoxRow", content.GetChild(0).name);
            Assert.AreEqual("QuestWeekRow", content.GetChild(1).name);
            ClaimDailies(3);
            screen.Refresh();
            Assert.AreEqual("Hôm nay 3/5", T(Quests + "QuestBoxRow/Title").text);
            Assert.IsFalse(root.transform.Find(Quests + "QuestBoxRow/Box/Badge").gameObject.activeSelf);
            for (int i = 0; i < QuestChests.Chests.Length; i++)
                Assert.IsNotNull(root.transform.Find(Quests + "QuestWeekRow/Track/Chest " + i));
        }

        [Test]
        public void A_Full_Set_Opens_The_Box_Once_And_Three_Prizes_Turn_Over_In_Turn()
        {
            ClaimDailies(3);
            screen.Refresh();
            root.transform.Find(Quests + "QuestBoxRow/Box").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(screen.Prizes.IsOpen, "three of five opens nothing");

            ClaimDailies(5);
            screen.Refresh();
            Assert.IsTrue(screen.ChestRows.BoxReady);
            Assert.IsTrue(root.transform.Find(Quests + "QuestBoxRow/Box/Badge").gameObject.activeSelf);
            int points = QuestChests.Points(s, runner.TodayNumber);
            root.transform.Find(Quests + "QuestBoxRow/Box").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.Prizes.IsOpen);
            Assert.AreEqual(3, screen.Prizes.Flipped, "with reduced motion all three are face up as the card opens");
            Assert.AreEqual(points + QuestChests.BoxPoints, QuestChests.Points(s, runner.TodayNumber));
            Assert.IsTrue(QuestChests.BoxOpened(s));
            screen.Prizes.Close();
            root.transform.Find(Quests + "QuestBoxRow/Box").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(screen.Prizes.IsOpen, "once a day");
        }

        [Test]
        public void A_Chest_With_Its_Points_Wears_A_Badge_And_Opens_With_Its_Gifts()
        {
            s.WeekPoints = 120;
            screen.Refresh();
            Assert.IsTrue(root.transform.Find(Quests + "QuestWeekRow/Track/Chest 0/Badge").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find(Quests + "QuestWeekRow/Track/Chest 1/Badge").gameObject.activeSelf);
            int clovers = s.Clovers;
            root.transform.Find(Quests + "QuestWeekRow/Track/Chest 1").GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(screen.Prizes.IsOpen);
            root.transform.Find(Quests + "QuestWeekRow/Track/Chest 0").GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.Prizes.IsOpen);
            Assert.IsTrue(QuestChests.Claimed(s, 0));
            Assert.Greater(s.Clovers, clovers);
            screen.Refresh();
            Assert.IsFalse(root.transform.Find(Quests + "QuestWeekRow/Track/Chest 0/Badge").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find(Quests + "QuestWeekRow/Track/Chest 0/Tick").gameObject.activeSelf);
        }
    }
}

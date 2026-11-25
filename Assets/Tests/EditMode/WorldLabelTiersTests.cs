using System.Collections.Generic;
using System.Linq;
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
    public class WorldLabelTiersTests
    {
        const long Today = 20_780L;
        const long Now = Today * 86400L + 14 * 3600L;

        GameObject root;
        GameRunner runner;
        GardenScreen screen;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.DayClock = () => Today;
            runner.LocalClock = () => Now;
            runner.FishingClock = () => Now;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            int[] levels = { 27, 12, 9, 4 };
            for (int i = 0; i < levels.Length; i++) s.Levels[i] = levels[i];
            s.GardenXp = Garden.XpForLevel(6);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Newcomer.Closed = true;
            s.Coins = new BigNum(84000);
            Rabbits.Assign(s, Rabbits.ArriveKind(s, "tho-bong", "Bông").Id, 0);
            s.FreshKinds.Clear();
            s.FirstDaySeen = s.MaxDaySeen = Today - 3;
            s.WelcomeBack.SeenDay = s.MaxDaySeen;
            s.Baits = 4;
            s.BaitAt = Now - 300;
            s.IntrosSeen.UnionWith(Intros.All);
            s.IntrosTried.UnionWith(Intros.All);
            runner.Init(s);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        RectTransform World => (RectTransform)root.transform.Find("Canvas/Garden/Content");

        static float Shown(Graphic graphic)
        {
            float alpha = graphic.color.a;
            for (Transform t = graphic.transform; t != null; t = t.parent)
            {
                var group = t.GetComponent<CanvasGroup>();
                if (group != null) alpha *= group.alpha;
            }
            return graphic.enabled && graphic.gameObject.activeInHierarchy ? alpha : 0f;
        }

        Rect OnScreen(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var view = (RectTransform)World.parent;
            Vector2 a = (Vector2)view.InverseTransformPoint(corners[0]) - view.rect.min, b = (Vector2)view.InverseTransformPoint(corners[2]) - view.rect.min;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        List<Text> VisibleWorldLabels()
        {
            var screenRect = new Rect(0f, 0f, 1080f, 1920f);
            return World.GetComponentsInChildren<Text>(true)
                .Where(t => !string.IsNullOrWhiteSpace(t.text) && Shown(t) > 0.01f && OnScreen(t.rectTransform).Overlaps(screenRect))
                .ToList();
        }

        static string PathOf(Transform t, Transform top)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && p != top; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        void AssertCalmLabels(string where)
        {
            List<Text> labels = VisibleWorldLabels();
            List<Text> worded = labels.Where(t => t.text.Any(char.IsLetter)).ToList();
            string list = string.Join(", ", worded.Select(t => PathOf(t.transform, World) + " \"" + t.text + "\""));
            Assert.LessOrEqual(worded.Count, 4, where + ": at most four worded labels in the garden, saw " + list);
            for (int i = 0; i < labels.Count; i++)
                for (int j = i + 1; j < labels.Count; j++)
                    Assert.IsFalse(OnScreen(labels[i].rectTransform).Overlaps(OnScreen(labels[j].rectTransform)), where + ": " + PathOf(labels[i].transform, World) + " overlaps " + PathOf(labels[j].transform, World));
        }

        Text Sentence(string prop) => World.Find(prop + "/Plate/Text").GetComponent<Text>();
        Text Count(string prop) => World.Find(prop + "/Chip/Text").GetComponent<Text>();
        Text LevelWords(int station) => World.Find("Station " + runner.Catalog.Stations[station].Id + " Front/Level/Text").GetComponent<Text>();
        Text TrunkWords => World.Find("Trunk/Tag/Text").GetComponent<Text>();

        static readonly string[] Games = { "Mortar", "Basket", "Jar", "PondTap" };

        [Test]
        public void At_The_Default_Zoom_The_Garden_Shows_At_Most_Four_Labels_And_None_Overlap()
        {
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            AssertCalmLabels("the opening view");
            screen.LookAt(GardenScreen.TankSpot + new Vector2(-60f, -120f));
            screen.Advance(0.5f);
            AssertCalmLabels("by the fish tank");
            screen.LookAt(screen.MortarSpot);
            screen.Advance(0.5f);
            AssertCalmLabels("by the mortar");
        }

        [Test]
        public void At_The_Default_Zoom_Each_Game_Shows_A_Round_Chip_With_Its_Plays_Left()
        {
            foreach (string game in Games)
            {
                Assert.AreEqual(0f, Shown(Sentence(game)), game + ": the sentence waits for a closer look");
                Assert.Greater(Shown(Count(game)), 0.99f, game + ": the chip reads");
            }
            Assert.AreEqual("3", Count("Mortar").text);
            Assert.AreEqual("3", Count("Basket").text);
            Assert.AreEqual("3", Count("Jar").text);
            Assert.AreEqual("4", Count("PondTap").text, "the baits on hand");
            Assert.AreEqual(L10n.T("mortar.plays", 3), Sentence("Mortar").text, "the sentence is still there to be read out");
            Assert.AreEqual(L10n.T("pond.plate", 4, Fishing.BaitCap), Sentence("PondTap").text);
        }

        [Test]
        public void A_Game_With_No_Plays_Left_Shows_No_Chip()
        {
            GameState s = runner.State;
            s.PoundingDay = Today;
            s.PoundingPlays = Pounding.PlaysPerDay;
            s.Baits = 0;
            screen.Refresh();
            screen.Advance(0.5f);
            Assert.AreEqual(0f, Shown(Count("Mortar")), "no plays left: nothing to call the player over");
            Assert.AreEqual(0f, Shown(Count("PondTap")), "no baits: nothing either");
            Assert.Greater(Shown(Count("Basket")), 0.99f, "the other games still count theirs");
            Assert.AreEqual(L10n.T("mortar.tomorrow"), Sentence("Mortar").text);
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.Advance(0.5f);
            Assert.Greater(Shown(Sentence("Mortar")), 0.99f, "close up the sentence says when it is back");
        }

        [Test]
        public void At_Near_Zoom_The_Full_Sentences_And_Station_Levels_Show()
        {
            Assert.AreEqual(0f, Shown(LevelWords(0)), "the level's words wait for a closer look");
            Assert.AreEqual(0f, Shown(TrunkWords), "and so do the trunk's hours");
            Assert.Greater(Shown(World.Find("Station " + runner.Catalog.Stations[0].Id + " Front/Level/Bar/Fill").GetComponent<Image>()), 0.99f, "the cycle bar stays");
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            screen.Advance(0.05f);
            Assert.That(Shown(Sentence("Mortar")), Is.InRange(0.01f, 0.99f), "it fades in");
            screen.Advance(0.5f);
            foreach (string game in Games)
            {
                Assert.Greater(Shown(Sentence(game)), 0.99f, game + ": the whole sentence reads up close");
                Assert.AreEqual(0f, Shown(Count(game)), game + ": the chip gives way to it");
            }
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(L10n.StationLevel(runner.State.Levels[i]), LevelWords(i).text);
                Assert.Greater(Shown(LevelWords(i)), 0.99f, "station " + i + "'s level reads up close");
            }
            Assert.AreEqual(L10n.T("trunk.closed", Trunk.NextHour(runner.State, Now)), TrunkWords.text);
            Assert.Greater(Shown(TrunkWords), 0.99f, "the trunk's hours read up close");
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.Advance(0.5f);
            Assert.AreEqual(0f, Shown(Sentence("Mortar")), "back out, the chip again");
            Assert.Greater(Shown(Count("Mortar")), 0.99f);
        }

        [Test]
        public void Under_Reduced_Motion_The_Labels_Switch_At_Once()
        {
            Motion.SetReduced(true);
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            Assert.AreEqual(1f, Shown(Sentence("Jar")), "no fade: the sentence is there");
            Assert.AreEqual(0f, Shown(Count("Jar")));
            Assert.AreEqual(1f, Shown(LevelWords(0)));
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            Assert.AreEqual(0f, Shown(Sentence("Jar")));
            Assert.AreEqual(1f, Shown(Count("Jar")));
            Assert.AreEqual(0f, Shown(LevelWords(0)));
        }

        [Test]
        public void A_Price_Sign_Reads_At_Every_Zoom()
        {
            runner.State.Plots = 6;
            screen.Refresh();
            Text sign = LevelWords(4);
            Assert.IsNotEmpty(sign.text);
            Assert.Greater(Shown(sign), 0.99f, "an unbuilt plot's price is something to act on");
        }
    }
}

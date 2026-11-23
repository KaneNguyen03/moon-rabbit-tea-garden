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
    /// <summary>The forest steps back ring by ring on the garden screen (#53): the sign at the lawn's edge, the new grass, the rim and the view.</summary>
    public class ForestRingScreenTests
    {
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
            runner.Init(GameState.NewGame(catalog.BuildDefs())); // preset: never touches the save file
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false; // the real clock may fall in a shower (#62): these tests are about a dry garden
            screen.Build();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            L10n.SetLocale("vi");
        }

        Transform Sign => root.transform.Find("Canvas/Garden/Content/Forest Sign");
        Transform World => root.transform.Find("Canvas/Garden/Content");

        /// <summary>A garden that has everything the next ring asks for: the level, rabbits and kinds, and the carrots.</summary>
        void ReadyForTheNextRing()
        {
            GameState s = runner.State;
            MistSegment ring = Expansion.Next(runner.Defs, s);
            s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(ring.NeedLevel));
            while (s.Rabbits.Count < ring.NeedRabbits) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" + s.NextRabbitId });
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < ring.NeedKinds) s.SeenKinds.Add(kind.Id);
            if (!Expansion.HasDays(ring, s)) { s.MaxDaySeen = System.Math.Max(s.MaxDaySeen, 20000); s.FirstDaySeen = s.MaxDaySeen - ring.NeedDays; } // the days played it asks (#99)
            s.Coins = Expansion.CostOfNext(runner.Defs, s) + new BigNum(1);
            for (int i = 0; i < s.Plots; i++) if (s.Levels[i] <= 0) s.Levels[i] = 1;
            screen.Refresh();
        }

        static Rect GardenRect(RectTransform rt, RectTransform space)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 a = space.InverseTransformPoint(corners[0]), b = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        [Test]
        public void The_Sign_Stands_At_The_Lawns_Edge_Only_While_The_Garden_Can_Widen_And_A_Press_Opens_The_Next_Ring()
        {
            Assert.IsNotNull(Sign, "the sign is built");
            Assert.IsFalse(Sign.gameObject.activeSelf, "a new garden cannot widen yet");
            ReadyForTheNextRing();
            Assert.IsTrue(Sign.gameObject.activeSelf, "everything the first ring asks for is there");
            Assert.AreEqual("Mở rộng vườn", Sign.Find("Board/Face/Text").GetComponent<Text>().text);
            Vector2 at = screen.ForestSignSpot;
            float heart = screen.StationSpot(0).x, edge = (GardenGrid.StartHalfWidth + 1) * (float)GardenGrid.PitchX / 2f;
            Assert.AreEqual(heart + edge - 125f, at.x, 1f, "just inside the open lawn's end on the side the first segment opens, where the mist begins: " + at);
            Assert.AreEqual(at, ((RectTransform)Sign).anchoredPosition);
            Assert.AreEqual(screen.StationSpot(runner.State.Plots).x < heart, at.x < heart, "on the side of the land it opens");

            Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(1, runner.State.Grid.Segments, "a segment came out of the mist");
            Assert.AreEqual(6, runner.State.Plots);
            screen.Refresh();
            Assert.IsFalse(Sign.gameObject.activeSelf, "the next ring asks for more: the sign goes until then");
            Assert.IsTrue(World.Find("Segment Grass 1").gameObject.activeSelf, "new grass on the segment just opened");
            Assert.IsFalse(World.Find("Segment Grass 2").gameObject.activeSelf, "and none yet on the segment after");

            // The second ring: the sign comes back once the garden has grown enough, and opens it the same way.
            ReadyForTheNextRing();
            Assert.IsTrue(Sign.gameObject.activeSelf);
            Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(2, runner.State.Grid.Segments);
            Assert.AreEqual(8, runner.State.Plots);
            Assert.Less(screen.ForestSignSpot.x, heart, "the second segment opens on the left");
        }

        [Test]
        public void The_New_Rings_Grass_Comes_Up_Tile_By_Tile_And_Stays()
        {
            ReadyForTheNextRing();
            Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
            screen.Refresh();
            Transform grass = World.Find("Segment Grass 1");
            Assert.Greater(grass.childCount, 4, "a tuft on each open tile of the ring");
            bool growing = false;
            foreach (Transform tuft in grass) if (tuft.localScale.x < 0.99f) growing = true;
            Assert.IsTrue(growing, "the grass is still coming up");
            for (int i = 0; i < 30; i++) screen.Advance(0.1f);
            foreach (Transform tuft in grass) Assert.AreEqual(1f, tuft.localScale.x, 1e-3f, tuft.name + " has come up");
            Assert.IsFalse(World.Find("Ring 4").gameObject.activeSelf, "the dashed ring of the fifth plot is gone");
            Assert.IsTrue(World.Find("Bloom 4").gameObject.activeSelf, "and its grass and flowers are there");
        }

        [Test]
        public void Under_Reduced_Motion_The_Ring_Opens_At_Once()
        {
            Motion.SetReduced(true);
            ReadyForTheNextRing();
            Assert.AreEqual(Vector3.one, Sign.localScale, "the sign is simply there");
            Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
            screen.Refresh(); // no time passes
            Transform grass = World.Find("Segment Grass 1");
            Assert.IsTrue(grass.gameObject.activeSelf);
            foreach (Transform tuft in grass) Assert.AreEqual(1f, tuft.localScale.x, 1e-6f, tuft.name + " is there at once");
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            foreach (Transform child in canopy)
                if (child.name.StartsWith("Rim ") && child.name != "Rim Shade" && child.gameObject.activeSelf)
                    Assert.AreEqual(1f, child.GetComponent<Image>().color.a, 1e-3f, child.name + " in full at once");
        }

        [Test]
        public void At_Every_Ring_The_Grass_Keeps_Off_Every_Station_And_The_Sign_Covers_No_Plate()
        {
            Motion.SetReduced(true);
            var street = (RectTransform)World;
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            for (int ring = 0; ring < Expansion.Table.Length; ring++)
            {
                ReadyForTheNextRing();
                Assert.IsTrue(Sign.gameObject.activeSelf, "ring " + ring);
                Rect sign = GardenRect((RectTransform)Sign, canopy);
                Rect board = GardenRect((RectTransform)Sign.Find("Board"), canopy);
                Assert.GreaterOrEqual(Mathf.Min(board.width, board.height), UiKit.MinTouch, "big enough to touch");
                foreach (RectTransform rt in street.GetComponentsInChildren<RectTransform>(false))
                {
                    if (rt.name != "Level" && rt.name != "Plate") continue;
                    Rect plate = GardenRect(rt, street);
                    Assert.IsFalse(plate.Overlaps(sign), "the sign at " + sign + " covers " + rt.parent.name + "/" + rt.name + " at " + plate + ", ring " + ring);
                }
                Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
                screen.Refresh();
                Transform grass = World.Find("Segment Grass " + runner.State.Grid.Segments);
                foreach (Transform tuft in grass)
                {
                    Rect at = GardenRect((RectTransform)tuft, street);
                    for (int i = 0; i < runner.Defs.Count; i++)
                    {
                        Rect level = GardenRect((RectTransform)street.Find("Station " + runner.Defs[i].Id + " Front/Level"), street);
                        Assert.IsFalse(at.Overlaps(level), tuft.name + " lies on " + runner.Defs[i].Id + "'s plate");
                    }
                }
            }
            Assert.IsNull(Expansion.Next(runner.Defs, runner.State), "every station has its land");
            screen.Refresh();
            Assert.IsFalse(Sign.gameObject.activeSelf, "nothing left to open: no sign");
        }

        [Test]
        public void The_View_Reaches_The_New_Edge_After_Every_Ring()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            var world = (RectTransform)World;
            for (int ring = 0; ring < Expansion.Table.Length; ring++)
            {
                ReadyForTheNextRing();
                Vector2 sign = screen.ForestSignSpot;
                Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
                screen.Refresh();
                var spots = new List<Vector2> { sign };
                foreach (Transform tuft in World.Find("Segment Grass " + runner.State.Grid.Segments)) spots.Add(((RectTransform)tuft).anchoredPosition);
                foreach (GardenScreen.ZoomLevel zoom in new[] { GardenScreen.ZoomLevel.Default, GardenScreen.ZoomLevel.Near, GardenScreen.ZoomLevel.Far })
                {
                    screen.SetZoom(zoom, false);
                    foreach (Vector2 spot in spots)
                    {
                        screen.LookAt(spot);
                        Vector2 at = spot * world.localScale.x + world.anchoredPosition;
                        Assert.IsTrue(at.x > -40f && at.x < 1120f && at.y > 340f && at.y < 1920f - 160f, "ring " + (ring + 4) + ": " + spot + " comes into view at " + zoom + ": " + at);
                    }
                }
            }
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Signs_Words_Fit_On_Its_Board(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            ReadyForTheNextRing();
            Text text = Sign.Find("Board/Face/Text").GetComponent<Text>();
            Assert.AreEqual(L10n.T("garden.expand"), text.text);
            Vector2 size = text.rectTransform.rect.size;
            var gen = new TextGenerator();
            gen.Populate(text.text, text.GetGenerationSettings(size));
            Assert.AreEqual(text.text.Length, gen.characterCountVisible, "every letter shows in " + size);
            Assert.GreaterOrEqual(gen.fontSizeUsedForBestFit / text.pixelsPerUnit, 18f, "and at a size that reads");
        }

        [Test]
        public void The_Stations_Tab_Names_The_Ring_Not_A_Wave()
        {
            screen.OpenSheet(0);
            screen.Refresh();
            Text title = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Plot/Title").GetComponent<Text>();
            Assert.AreEqual("Mở rộng vườn · đoạn 1", title.text);
            ReadyForTheNextRing();
            Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
            screen.Refresh();
            Assert.AreEqual("Mở rộng vườn · đoạn 2", title.text);
        }
    }
}

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
    public class MapViewScreenTests
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
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
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
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        RectTransform World => (RectTransform)root.transform.Find("Canvas/Garden/Content");
        Transform Sign => World.Find("Forest Sign");
        static Vector2 Teapot => GardenScreen.CellToWorld(new Hex(0, 0));

        Vector2 ShownY() => new Vector2(-World.anchoredPosition.y / World.localScale.y, (1920f - World.anchoredPosition.y) / World.localScale.y);

        void HavePlots(int plots)
        {
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = i < plots - 2 || i < Plots.Starting ? 1 : 0;
            runner.State.Plots = plots;
            screen.Refresh();
        }

        void ReadyForTheNextSegment()
        {
            GameState s = runner.State;
            MistSegment next = Expansion.Next(runner.Defs, s);
            s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(next.NeedLevel));
            while (s.Rabbits.Count < next.NeedRabbits) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" + s.NextRabbitId });
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < next.NeedKinds) s.SeenKinds.Add(kind.Id);
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

        static float Clearance(Rect a, Rect b) => Mathf.Max(Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax), Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax));

        void Settle()
        {
            for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);
        }

        [Test]
        public void The_View_Stops_At_The_Open_Lands_Rows_Top_And_Bottom_And_Springs_Back()
        {
            screen.Shell.StartGame();
            Settle();
            var scroll = World.parent.GetComponent<ScrollRect>();
            Assert.IsTrue(scroll.vertical, "the garden drags up and down at the usual zoom");
            Vector2 rows = screen.OpenRows();
            Assert.AreEqual(GardenScreen.LawnEdgeY(0, GardenSide.Up) + GardenScreen.ViewMarginY, rows.y, 1f);
            Assert.AreEqual(GardenScreen.LawnEdgeY(0, GardenSide.Down) - GardenScreen.ViewMarginY, rows.x, 1f);
            Assert.AreEqual(Teapot.y + 700f, GardenScreen.LawnEdgeY(0, GardenSide.Up), 1f, "half a row past the middle's top row");
            foreach (float push in new[] { -3000f, 3000f })
            {
                screen.SimulateGardenDrag(new Vector2(0f, push));
                Settle();
                Vector2 shown = ShownY();
                Assert.GreaterOrEqual(shown.x, rows.x - 1f, "let go past the bottom, it comes back inside the open rows");
                Assert.LessOrEqual(shown.y, rows.y + 1f, "and past the top too");
            }
            screen.LookAt(Teapot + new Vector2(0f, 5000f));
            Assert.LessOrEqual(ShownY().y, rows.y + 1f, "a glide to far above stops at the open rows' top");
        }

        [Test]
        public void Each_Band_Up_Or_Down_Widens_What_The_View_May_Show_That_Way()
        {
            HavePlots(Expansion.PlotsForSegments(runner.Defs, 4));
            Vector2 sides = screen.OpenRows();
            Assert.AreEqual(GardenScreen.LawnEdgeY(0, GardenSide.Up) + GardenScreen.ViewMarginY, sides.y, 1f, "the bands to the side leave the rows as they were");

            HavePlots(Expansion.PlotsForSegments(runner.Defs, 5));
            Vector2 up = screen.OpenRows();
            Assert.AreEqual(Mathf.Min(World.sizeDelta.y, sides.y + 8 * (float)GardenGrid.PitchY), up.y, 1f, "the band up opens eight rows above, as far as the world goes");
            Assert.AreEqual(sides.x, up.x, 1f, "and nothing below");
            screen.LookAt(Teapot);
            float atTeapot = World.anchoredPosition.y;
            screen.LookAt(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(Expansion.PlotsForSegments(runner.Defs, 5) - 1)));
            Assert.Less(World.anchoredPosition.y, atTeapot - 300f, "the view goes up to the band's station");

            HavePlots(Expansion.PlotsForSegments(runner.Defs, 6));
            Vector2 down = screen.OpenRows();
            Assert.AreEqual(Mathf.Max(0f, sides.x - 8 * (float)GardenGrid.PitchY), down.x, 1f, "the band down opens eight rows below");
            Assert.Greater(screen.ZoomScale * (down.y - down.x), 2f * 1920f, "the whole map is more than two screens tall at the usual zoom");
        }

        [TestCase(5, GardenSide.Up)]
        [TestCase(6, GardenSide.Down)]
        public void The_Sign_For_A_Band_Up_Or_Down_Stands_At_That_Edge_Of_The_Lawn(int segment, GardenSide side)
        {
            Motion.SetReduced(true);
            HavePlots(Expansion.PlotsForSegments(runner.Defs, segment - 1));
            ReadyForTheNextSegment();
            Assert.IsTrue(Sign.gameObject.activeSelf, "the garden can widen");
            Vector2 at = screen.ForestSignSpot;
            float edge = GardenScreen.LawnEdgeY(segment - 1, side);
            if (side == GardenSide.Up)
                Assert.That(at.y, Is.InRange(edge - 420f, edge - 180f), "the board's top just inside the lawn's top edge: " + at);
            else
                Assert.That(at.y, Is.InRange(edge, edge + 260f), "the post's foot just inside the lawn's bottom edge: " + at);
            Vector2 span = GardenScreen.SegmentSpan(segment);
            Assert.That(at.x, Is.InRange(span.x + 200f, span.y - 200f), "along the edge, not at a side");
            var street = World;
            Rect board = GardenRect((RectTransform)Sign, street);
            foreach (RectTransform rt in street.GetComponentsInChildren<RectTransform>(false))
                if (rt.name == "Level" || rt.name == "Plate")
                    Assert.IsFalse(GardenRect(rt, street).Overlaps(board), "the sign covers " + rt.parent.name + "/" + rt.name);

            Sign.Find("Board").GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(segment, runner.State.Grid.Segments, "a press opens the band");
        }

        [TestCase(5, 90f)]
        [TestCase(6, 270f)]
        public void A_Shut_Band_Up_Or_Down_Has_A_Thick_Bank_Of_Mist_Along_The_Lawns_Edge(int segment, float turn)
        {
            CanvasGroup mist = screen.MistOf(segment);
            Assert.IsNotNull(mist);
            var edge = (RectTransform)mist.transform.Find("Edge");
            Assert.IsNotNull(edge, "a bank along the edge it shares with the open lawn");
            Assert.AreEqual(turn, edge.localEulerAngles.z, 0.5f, "turned to lie along the edge, clear on the lawn's side");
            GardenSide side = GardenGrid.SideOf(segment);
            float lawn = GardenScreen.LawnEdgeY(segment - 1, side), inward = side == GardenSide.Up ? 1f : -1f;
            Assert.That((edge.anchoredPosition.y - lawn) * inward, Is.InRange(0f, 200f), "on the band's side of the lawn's edge");
            Vector2 span = GardenScreen.SegmentSpan(segment);
            Assert.AreEqual((span.x + span.y) / 2f, edge.anchoredPosition.x, 1f, "the whole width of the band");
            Assert.Greater(edge.sizeDelta.y, span.y - span.x, "and past both ends");
        }

        List<KeyValuePair<string, Rect>> WoodPieces()
        {
            var list = new List<KeyValuePair<string, Rect>>();
            foreach (Transform child in World)
                if (child.name.StartsWith("Wood ") && child.gameObject.activeSelf)
                {
                    var img = child.GetComponent<Image>();
                    if (img != null && img.color.a > 0.5f) list.Add(new KeyValuePair<string, Rect>(child.name, GardenRect((RectTransform)child, World)));
                }
            return list;
        }

        [TestCase(0, false, false)]
        [TestCase(4, false, false)]
        [TestCase(5, true, false)]
        [TestCase(6, true, true)]
        public void The_Wood_Above_And_Below_Hugs_The_Open_Lawn_Once_That_Band_Is_Open_And_Keeps_Off_Every_Open_Station(int segments, bool wantAbove, bool wantBelow)
        {
            Motion.SetReduced(true);
            int plots = Expansion.PlotsForSegments(runner.Defs, segments);
            HavePlots(plots);
            Settle();
            List<KeyValuePair<string, Rect>> wood = WoodPieces();
            float top = GardenScreen.LawnEdgeY(segments, GardenSide.Up), bottom = GardenScreen.LawnEdgeY(segments, GardenSide.Down);
            int above = 0, below = 0;
            foreach (KeyValuePair<string, Rect> piece in wood)
            {
                Rect r = piece.Value;
                if (r.yMin > Teapot.y)
                {
                    above++;
                    Assert.That(r.yMin, Is.InRange(top - 1f, top + 520f), piece.Key + " stands on the wood's side of the lawn's top edge, near it: " + r);
                }
                else
                {
                    below++;
                    Assert.That(r.yMax, Is.InRange(bottom - 420f, bottom + 1f), piece.Key + " stands on the wood's side of the lawn's bottom edge, near it: " + r);
                }
            }
            if (wantAbove) Assert.Greater(above, 6, "a row of wood above the lawn once the band up is open");
            else Assert.AreEqual(0, above, "while the band up is shut its mist bank is the edge, with no wood standing on the mist");
            if (wantBelow) Assert.Greater(below, 6, "and below it once the band down is open");
            else Assert.AreEqual(0, below, "while the band down is shut its mist bank is the edge");
            for (int i = 0; i < plots && i < runner.Defs.Count; i++)
            {
                Transform station = World.Find("Station " + runner.Defs[i].Id), front = World.Find("Station " + runner.Defs[i].Id + " Front");
                foreach (string part in new[] { "Level", "Pad", "Floor", "Prop", "Rabbit" })
                {
                    Rect at = GardenRect((RectTransform)(part == "Level" || part == "Pad" ? front : station).Find(part), World);
                    foreach (KeyValuePair<string, Rect> piece in wood)
                        Assert.GreaterOrEqual(Clearance(at, piece.Value), 200f, runner.Defs[i].Id + "/" + part + " keeps 200 units from " + piece.Key + " at " + piece.Value);
                }
            }
        }

        [Test]
        public void The_Bands_Up_And_Down_Grow_Grass_When_They_Open()
        {
            Motion.SetReduced(true);
            HavePlots(Expansion.PlotsForSegments(runner.Defs, 6));
            foreach (int segment in new[] { 5, 6 })
            {
                Transform grass = World.Find("Segment Grass " + segment);
                Assert.IsTrue(grass.gameObject.activeSelf);
                Assert.Greater(grass.childCount, 10, "tufts across band " + segment);
            }
        }
    }
}

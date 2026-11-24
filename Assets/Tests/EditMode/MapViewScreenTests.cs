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
            UiKit.SetLargeText(false);
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
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        RectTransform World => (RectTransform)root.transform.Find("Canvas/Garden/Content");
        Transform Sign => World.Find("Forest Sign");
        static Vector2 Teapot => GardenScreen.CellToWorld(new Hex(0, 0));

        Vector2 ShownY() => new Vector2((16f + 170f + 10f + GardenScreen.ReadMargin - World.anchoredPosition.y) / World.localScale.y, (1920f - 160f - GardenScreen.ReadMargin - World.anchoredPosition.y) / World.localScale.y);

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
            if (!Expansion.HasDays(next, s)) { s.MaxDaySeen = System.Math.Max(s.MaxDaySeen, 20000); s.FirstDaySeen = s.MaxDaySeen - next.NeedDays; }
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
            RectTransform Canopy => (RectTransform)root.transform.Find("Canvas/Garden/Canopy");
        Vector2 OnView(Vector2 garden) => garden * World.localScale.x + World.anchoredPosition;

        void OpenEveryStation()
        {
            HavePlots(runner.Defs.Count);
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = 1;
            screen.Refresh();
        }

        void Glide(int frames = 60)
        {
            for (int i = 0; i < frames; i++) screen.Advance(1f / 30f);
        }

        [Test]
        public void The_Far_Wood_Follows_The_Ground_Up_And_Down_At_Nine_Tenths_Of_Its_Pace()
        {
            HavePlots(runner.Defs.Count);
            screen.LookAt(Teapot + new Vector2(0f, 1800f));
            screen.Advance(0f);
            Vector2 street0 = World.anchoredPosition, canopy0 = Canopy.anchoredPosition;
            screen.LookAt(Teapot - new Vector2(0f, 1800f));
            screen.Advance(0f);
            float groundMoved = World.anchoredPosition.y - street0.y, canopyMoved = Canopy.anchoredPosition.y - canopy0.y;
            Assert.Greater(Mathf.Abs(groundMoved), 1500f, "the view really went from the band up to the band down");
            Assert.AreEqual(groundMoved * GardenScreen.CanopyDrift, canopyMoved, 1f, "the far wood up and down at nine tenths of the pace too");
            screen.LookAt(GardenScreen.CellToWorld(new Hex(0, 0)));
            screen.Advance(0f);
            Assert.AreEqual(World.anchoredPosition.y, Canopy.anchoredPosition.y, 0.5f, "resting on the teapot the far wood stands where it was drawn");
        }

        [Test]
        public void Under_Reduced_Motion_The_Far_Wood_Keeps_With_The_Ground_Up_And_Down()
        {
            Motion.SetReduced(true);
            HavePlots(runner.Defs.Count);
            screen.LookAt(Teapot + new Vector2(0f, 1800f));
            screen.Advance(0f);
            Vector2 street0 = World.anchoredPosition, canopy0 = Canopy.anchoredPosition;
            screen.LookAt(Teapot - new Vector2(0f, 1800f));
            screen.Advance(0f);
            Assert.Greater(Mathf.Abs(World.anchoredPosition.y - street0.y), 1500f, "the view really moved");
            Assert.AreEqual(World.anchoredPosition.y - street0.y, Canopy.anchoredPosition.y - canopy0.y, 0.5f, "no parallax: the wood moves with the ground");
        }

        [TestCase(0f, 3000f)]
        [TestCase(0f, -3000f)]
        [TestCase(-2500f, 0f)]
        [TestCase(2500f, 0f)]
        [TestCase(-2500f, 3000f)]
        public void The_Way_Home_Brings_The_Tea_Shop_Back_From_Up_Down_Left_Or_Right(float dx, float dy)
        {
            OpenEveryStation();
            screen.Shell.StartGame();
            Glide();
            screen.LookAt(Teapot + new Vector2(dx, dy));
            screen.Advance(1f / 30f);
            GameObject home = root.transform.Find("Canvas/Home").gameObject;
            Assert.IsTrue(home.activeSelf, "the teapot is out of view: the way home shows");
            home.GetComponent<Button>().onClick.Invoke();
            Glide();
            foreach (Vector2 spot in new[] { Teapot, screen.ShopSpot, screen.HomeSpot })
            {
                Vector2 at = OnView(spot);
                Assert.That(at.x, Is.InRange(GardenScreen.ReadMargin + 100f, 1080f - GardenScreen.ReadMargin - 100f), spot + " is back in view sideways");
                Assert.That(at.y, Is.InRange(300f, 1920f - 160f - 100f), spot + " is back in view up and down, under the HUD and over the tab bar");
            }
            Assert.IsFalse(home.activeSelf, "home: the button goes");
        }

        [TestCase(13, -3000f)]
        [TestCase(12, -3000f)]
        [TestCase(15, 3000f)]
        public void A_Station_Row_Slides_The_View_Up_Or_Down_To_A_Station_On_A_Band(int index, float startDy)
        {
            OpenEveryStation();
            screen.Shell.StartGame();
            Glide();
            GardenSide side = GardenGrid.SideOf(index == 15 ? 6 : 5);
            Assert.AreEqual(side == GardenSide.Up, screen.StationSpot(index).y > GardenScreen.LawnEdgeY(4, GardenSide.Up), "station " + index + " stands on the band " + side);
            Assert.AreEqual(side == GardenSide.Down, screen.StationSpot(index).y < GardenScreen.LawnEdgeY(4, GardenSide.Down));
            screen.LookAt(Teapot + new Vector2(0f, startDy));
            screen.OpenSheet(0);
            Glide(10);
            Assert.That(OnView(screen.StationSpot(index)).y, Is.Not.InRange(screen.SheetHeight, 1920f), "it starts out of view");
            Find("Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[index].Id).onClick.Invoke();
            Glide(30);
            Vector2 at = OnView(screen.StationSpot(index));
            Assert.That(at.x, Is.InRange(GardenScreen.ReadMargin + 120f, 1080f - GardenScreen.ReadMargin - 120f), "station " + index + " slid into view sideways");
            Assert.Greater(at.y, screen.SheetHeight + 60f, "above the sheet");
            Assert.Less(at.y + 150f * World.localScale.y, 1920f - 160f, "and its top under the HUD");
        }

        Button Find(string path) => root.transform.Find(path).GetComponent<Button>();

        [Test]
        public void The_Widen_Sign_Has_An_Ink_Outline_Like_The_Drawn_Props()
        {
            foreach (string part in new[] { "Board", "Post" })
            {
                Transform piece = Sign.Find(part), ink = Sign.Find(part + " Ink");
                Assert.IsNotNull(ink, "an ink line round the " + part);
                var line = ink.GetComponent<Image>();
                Assert.AreEqual(Palette.Ink.r, line.color.r, 0.01f);
                Assert.AreEqual(Palette.Ink.g, line.color.g, 0.01f);
                Assert.AreEqual(Palette.Ink.b, line.color.b, 0.01f);
                Assert.Greater(line.color.a, 0.6f, "dark enough to read as a line");
                Assert.IsFalse(line.raycastTarget, "a press still reaches the board");
                Assert.Less(ink.GetSiblingIndex(), piece.GetSiblingIndex(), "drawn behind the " + part);
                Rect a = GardenRect((RectTransform)piece, (RectTransform)Sign), b = GardenRect((RectTransform)ink, (RectTransform)Sign);
                foreach (float rim in new[] { a.xMin - b.xMin, b.xMax - a.xMax, a.yMin - b.yMin, b.yMax - a.yMax })
                    Assert.That(rim, Is.InRange(2f, 6f), "a thin line all round the " + part + ": " + a + " in " + b);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(4)]
        public void The_Next_Plots_Widen_Label_Is_Never_Cut_By_The_Screen_Edge(int segments)
        {
            Motion.SetReduced(true);
            int plots = Expansion.PlotsForSegments(runner.Defs, segments);
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = i < plots ? 1 : 0;
            runner.State.Plots = plots;
            screen.Shell.StartGame();
            screen.Refresh();
            Glide();
            var label = (RectTransform)World.Find("Station " + runner.Defs[plots].Id + " Front/Level");
            Assert.IsTrue(label.gameObject.activeInHierarchy, "the next plot shows its label");
            Assert.AreEqual(L10n.ExpandSign(runner.Defs, runner.State), label.GetComponentInChildren<Text>().text, "the label says what the next segment still needs (#99)");
            Vector2 spot = screen.StationSpot(plots);
            float toward = Mathf.Sign(spot.x - Teapot.x);
            foreach (Vector2 look in new[] { spot, new Vector2(spot.x + toward * 5000f, spot.y), new Vector2(spot.x + toward * 5000f, spot.y + 5000f), new Vector2(spot.x + toward * 5000f, spot.y - 5000f) })
            {
                screen.LookAt(look);
                Glide(5);
                Rect view = ViewRect(GardenRect(label, World));
                Assert.GreaterOrEqual(view.xMin, 0f, "looking at " + look + " as far as the view goes, the label of plot " + plots + " is whole on the left: " + view);
                Assert.LessOrEqual(view.xMax, 1080f, "looking at " + look + " and on the right: " + view);
            }
        }

        const float HudBottom = 1920f - 160f, TabTop = 16f + 170f;

        void OpenSegmentsWithEverythingToRaise(int segments)
        {
            GameState s = runner.State;
            HavePlots(Expansion.PlotsForSegments(runner.Defs, segments));
            s.Grid.Segments = segments;
            Rabbits.GiftFirst(s, "Bông");
            s.Rabbits[0].Station = 0;
            if (Expansion.Next(runner.Defs, s) != null) ReadyForTheNextSegment();
            s.Coins = new BigNum(1e300);
            PhotoAsks.Decline(s);
            StandOnTheOutermostRow(1, -Mathf.Min(GardenGrid.Reach(segments, GardenSide.Up), HighestRowInTheWorld));
            StandOnTheOutermostRow(2, GardenGrid.Reach(segments, GardenSide.Down));
        }

        static int HighestRowInTheWorld => Mathf.FloorToInt((5400f - Teapot.y - 100f) / (float)GardenGrid.PitchY);

        void StandOnTheOutermostRow(int station, int r)
        {
            GardenGrid grid = runner.State.Grid;
            string id = GardenGrid.StationId(runner.Defs[station]);
            for (int col = 0; col < 12; col++)
                foreach (int c in new[] { -col, col })
                {
                    if ((c - r) % 2 != 0) continue;
                    int q = (c - r) / 2;
                    if (!grid.InLawn(q, r) || grid.OccupantAt(q, r) != null || GardenGrid.IsReserved(new Hex(q, r))) continue;
                    Hex? home = grid.TileOf(id);
                    grid.Remove(id);
                    if (grid.Place(id, q, r)) return;
                    if (home.HasValue) Assert.IsTrue(grid.Place(id, home.Value.Q, home.Value.R), id + " goes back home");
                }
            Assert.Fail("no free tile on row " + r);
        }

        List<KeyValuePair<string, RectTransform>> Readables(bool wood)
        {
            var list = new List<KeyValuePair<string, RectTransform>>();
            foreach (Transform child in World)
            {
                if (!child.gameObject.activeInHierarchy || child.name.StartsWith("Spot ")) continue;
                Transform plate = child.name.StartsWith("Station ") ? child.Find("Level") : child.name == "Forest Sign" ? child.Find("Board") : child.Find("Plate");
                if (plate != null && plate.gameObject.activeInHierarchy) list.Add(new KeyValuePair<string, RectTransform>(child.name + "/" + plate.name, (RectTransform)plate));
                if (child.name == "ChatLine") list.Add(new KeyValuePair<string, RectTransform>("ChatLine", (RectTransform)child));
                if (wood && child.name.StartsWith("Wood "))
                {
                    var img = child.GetComponent<Image>();
                    if (img != null && img.color.a > 0.5f) list.Add(new KeyValuePair<string, RectTransform>(child.name, (RectTransform)child));
                }
            }
            return list;
        }

        static bool OnScreen(Rect view) => view.xMax > 0f && view.xMin < 1080f && view.yMax > 0f && view.yMin < 1920f;

        static Rect SidePillRect(RectTransform pill) => new Rect(new Vector2(pill.anchorMin.x * 1080f, pill.anchorMin.y * 1920f) + pill.anchoredPosition - pill.sizeDelta / 2f, pill.sizeDelta);

        [TestCase(0, GardenScreen.ZoomLevel.Far)]
        [TestCase(0, GardenScreen.ZoomLevel.Default)]
        [TestCase(0, GardenScreen.ZoomLevel.Near)]
        [TestCase(4, GardenScreen.ZoomLevel.Far)]
        [TestCase(4, GardenScreen.ZoomLevel.Default)]
        [TestCase(4, GardenScreen.ZoomLevel.Near)]
        [TestCase(5, GardenScreen.ZoomLevel.Far)]
        [TestCase(5, GardenScreen.ZoomLevel.Default)]
        [TestCase(5, GardenScreen.ZoomLevel.Near)]
        [TestCase(6, GardenScreen.ZoomLevel.Far)]
        [TestCase(6, GardenScreen.ZoomLevel.Default)]
        [TestCase(6, GardenScreen.ZoomLevel.Near)]
        [TestCase(9, GardenScreen.ZoomLevel.Far)]
        [TestCase(9, GardenScreen.ZoomLevel.Default)]
        [TestCase(9, GardenScreen.ZoomLevel.Near)]
        public void Across_The_Views_Whole_Range_No_Pill_Covers_A_Plate_And_Nothing_To_Read_Hides_Under_The_Bars_Or_Off_The_Screen(int segments, GardenScreen.ZoomLevel zoom)
        {
            Motion.SetReduced(true);
            var faults = new List<string>();
            int spoke = 0, pills = 0;
            foreach (KeyValuePair<string, bool> text in new[] { new KeyValuePair<string, bool>("vi", false), new KeyValuePair<string, bool>("en", true) })
            {
                L10n.SetLocale(text.Key);
                UiKit.SetLargeText(text.Value);
                OpenSegmentsWithEverythingToRaise(segments);
                screen.RebuildUi();
                screen.Shell.StartGame();
                screen.Refresh();
                screen.SetZoom(zoom, false);
                long bong = runner.State.Rabbits[0].Id;
                string where = text.Key + (text.Value ? " large" : "") + ", " + segments + " segments, " + zoom;

                screen.LookAt(new Vector2(-99999f, -99999f));
                Vector2 leftBottom = World.anchoredPosition;
                screen.LookAt(new Vector2(99999f, 99999f));
                Vector2 rightTop = World.anchoredPosition;
                screen.LookAt(screen.StationSpot(0) + new Vector2(0f, 500f));
                screen.Advance(0f);
                if (screen.ChatterBy(bong))
                {
                    spoke++;
                    Rect b = ViewRect(GardenRect((RectTransform)World.Find("ChatLine"), World));
                    if (b.xMin < -0.5f || b.xMax > 1080.5f || b.yMax > HudBottom + 0.5f || b.yMin < TabTop - 0.5f) faults.Add("4 " + where + ", looking above the teapot: Bông's line " + b + " is not whole in view");
                }
                Vector2 span = screen.OpenSpan(), rows = screen.OpenRows();
                float[] steps = { 0f, 0.25f, 0.5f, 0.75f, 1f };
                foreach (float fx in steps)
                    foreach (float fy in new[] { 0f, 1f / 6f, 2f / 6f, 0.5f, 4f / 6f, 5f / 6f, 1f })
                    {
                        float x = fx == 0f ? -99999f : fx == 1f ? 99999f : Mathf.Lerp(span.x, span.y, fx);
                        float y = fy == 0f ? -99999f : fy == 1f ? 99999f : Mathf.Lerp(rows.x, rows.y, fy);
                        screen.LookAt(new Vector2(x, y));
                        screen.Advance(0f);
                        Vector2 at = World.anchoredPosition;
                        string look = where + ", looking at " + new Vector2(fx, fy) + " (" + at + "): ";
                        bool left = Mathf.Abs(at.x - leftBottom.x) < 0.5f, right = Mathf.Abs(at.x - rightTop.x) < 0.5f;
                        bool bottom = Mathf.Abs(at.y - leftBottom.y) < 0.5f, top = Mathf.Abs(at.y - rightTop.y) < 0.5f;

                        foreach (string edge in new[] { "EdgeLeft", "EdgeRight" })
                        {
                            var pill = (RectTransform)root.transform.Find("Canvas/" + edge);
                            if (!pill.gameObject.activeSelf) continue;
                            pills++;
                            Rect p = SidePillRect(pill);
                            foreach (KeyValuePair<string, RectTransform> plate in Readables(false))
                            {
                                if (plate.Key == "ChatLine") continue;
                                Rect r = ViewRect(GardenRect(plate.Value, World));
                                if (r.Overlaps(p)) faults.Add("1 " + look + edge + " " + p + " covers " + plate.Key + " " + r);
                            }
                            if (p.yMax > HudBottom || p.yMin < TabTop) faults.Add("1 " + look + edge + " " + p + " is under a bar");
                        }

                        foreach (KeyValuePair<string, RectTransform> thing in Readables(true))
                        {
                            Rect r = ViewRect(GardenRect(thing.Value, World));
                            if (!OnScreen(r)) continue;
                            bool tree = thing.Key.StartsWith("Wood ");
                            if (top && !tree && r.yMax > HudBottom + 0.5f) faults.Add("2 " + look + thing.Key + " " + r + " is under the HUD at the top of the range");
                            if (bottom && r.yMin < TabTop - 0.5f) faults.Add((tree ? "3 " : "2 ") + look + thing.Key + " " + r + " is under the tab bar at the bottom of the range");
                            if (tree) continue;
                            if (left && r.xMin < -0.5f) faults.Add("2 " + look + thing.Key + " " + r + " is cut by the left edge at the left of the range");
                            if (right && r.xMax > 1080.5f) faults.Add("2 " + look + thing.Key + " " + r + " is cut by the right edge at the right of the range");
                        }

                        if (screen.ChatterBy(bong))
                        {
                            spoke++;
                            Rect b = ViewRect(GardenRect((RectTransform)World.Find("ChatLine"), World));
                            if (b.xMin < -0.5f || b.xMax > 1080.5f || b.yMax > HudBottom + 0.5f || b.yMin < TabTop - 0.5f) faults.Add("4 " + look + "Bông's line " + b + " is not whole in view");
                        }
                    }

                foreach (KeyValuePair<string, RectTransform> thing in Readables(false))
                {
                    if (thing.Key == "ChatLine") continue;
                    Rect g = GardenRect(thing.Value, World);
                    screen.LookAt(g.center);
                    screen.Advance(0f);
                    Rect r = ViewRect(GardenRect(thing.Value, World));
                    if (r.xMin < -0.5f || r.xMax > 1080.5f || r.yMax > HudBottom + 0.5f || r.yMin < TabTop - 0.5f)
                        faults.Add((thing.Key.StartsWith("Forest Sign") || thing.Key.StartsWith("Station ") && thing.Value.GetComponentInChildren<Text>() != null && thing.Value.GetComponentInChildren<Text>().text == L10n.ExpandSign(runner.Defs, runner.State) ? "5 " : "2 ")
                            + where + ", looking at " + thing.Key + ": it stands at " + r + ", not whole between the bars");
                }
            }
            if (zoom != GardenScreen.ZoomLevel.Far && segments >= 4) Assert.Greater(pills, 0, "the side pills showed somewhere in the sweep");
            if (zoom != GardenScreen.ZoomLevel.Near) Assert.Greater(spoke, 0, "Bông spoke somewhere in the sweep");
            var byItem = new SortedDictionary<string, int>();
            foreach (string fault in faults) byItem[fault.Substring(0, 1)] = byItem.TryGetValue(fault.Substring(0, 1), out int n) ? n + 1 : 1;
            var counts = new List<string>();
            foreach (KeyValuePair<string, int> item in byItem) counts.Add("item " + item.Key + ": " + item.Value);
            var shown = new List<string>();
            foreach (string item in byItem.Keys) shown.AddRange(faults.FindAll(f => f.StartsWith(item)).GetRange(0, Mathf.Min(8, byItem[item])));
            Assert.IsEmpty(faults, faults.Count + " faults (" + string.Join(", ", counts) + "):\n" + string.Join("\n", shown));
        }

        Rect ViewRect(Rect garden)
        {
            Vector2 a = OnView(garden.min), b = OnView(garden.max);
            return Rect.MinMaxRect(a.x, a.y, b.x, b.y);
        }

        static Rect PinnedRect(RectTransform rt) => new Rect(new Vector2(rt.anchorMin.x * 1080f, rt.anchorMin.y * 1920f) + rt.anchoredPosition - rt.sizeDelta / 2f, rt.sizeDelta);

        [Test]
        public void The_Down_Pill_Steps_Aside_So_It_Never_Covers_The_Tea_Shop()
        {
            Motion.SetReduced(true);
            OpenEveryStation();
            runner.State.Coins = new BigNum(1e300);
            screen.Shell.StartGame();
            screen.Refresh();
            Glide();
            var pill = (RectTransform)root.transform.Find("Canvas/EdgeDown");
            var shop = (RectTransform)World.Find("Shop");
            float pillY = pill.anchoredPosition.y;
            float shopMiddle = screen.ShopSpot.y + shop.sizeDelta.y / 2f;
            screen.LookAt(new Vector2(screen.ShopSpot.x, shopMiddle + (1920f * 0.6f - pillY) / World.localScale.y));
            screen.Advance(1f / 30f);
            screen.Advance(1f / 30f);
            Assert.IsTrue(pill.gameObject.activeSelf, "stations wait below");
            Rect shopOn = ViewRect(GardenRect(shop, World)), pillOn = PinnedRect(pill);
            var middlePill = new Rect(540f - pillOn.width / 2f, pillOn.y, pillOn.width, pillOn.height);
            Assert.IsTrue(middlePill.Overlaps(shopOn), "the shop stands where the pill would stand: " + shopOn + " " + middlePill);
            Assert.IsFalse(pillOn.Overlaps(shopOn), "the pill steps aside off the shop: " + pillOn + " " + shopOn);
            Assert.AreEqual(pillY, pill.anchoredPosition.y, 0.5f, "at the same height");
            Assert.GreaterOrEqual(pillOn.xMin, GardenScreen.ReadMargin, "inside the screen");
            Assert.LessOrEqual(pillOn.xMax, 1080f - GardenScreen.ReadMargin);
            var home = (RectTransform)root.transform.Find("Canvas/Home");
            if (home.gameObject.activeSelf) Assert.IsFalse(pillOn.Overlaps(PinnedRect(home)), "and clear of the way home");

            screen.LookAt(Teapot);
            screen.Advance(1f / 30f);
            Assert.IsFalse(ViewRect(GardenRect(shop, World)).Overlaps(middlePill), "the shop is away from the pill's place");
            Assert.AreEqual(0f, pill.anchoredPosition.x, 0.5f, "the pill is back in the middle");
        }
    }
}

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
    public class LayoutModeTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey(GardenScreen.LayoutHintKey);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.Levels[1] = 12; s.Levels[2] = 9; s.Levels[3] = 4;
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            runner.Init(s);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.2f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            PlayerPrefs.DeleteKey(GardenScreen.LayoutHintKey);
            L10n.SetLocale("vi");
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        GameState S => runner.State;
        string Id(int station) => GardenGrid.StationId(runner.Defs[station]);
        Hex TileOf(string id) => S.Grid.TileOf(id).Value;
        RectTransform Actor(int station) => (RectTransform)root.transform.Find("Canvas/Garden/Content/Station " + runner.Defs[station].Id);

        static void Near(Vector2 expected, Vector2 actual, string message)
        {
            Assert.Less(Vector2.Distance(expected, actual), 0.01f, message + ": expected " + expected.ToString("F3") + " but was " + actual.ToString("F3"));
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        Hex EmptyTileNear(Hex near)
        {
            Hex best = default;
            int bestD = int.MaxValue;
            foreach (Hex h in screen.LayoutEmptyTiles())
            {
                int d = GardenGrid.Distance(h, near);
                if (d < bestD) { bestD = d; best = h; }
            }
            Assert.Less(bestD, int.MaxValue, "the lawn has an empty tile");
            return best;
        }

        Hex ApartTileNear(Hex near, string carried)
        {
            Hex best = default;
            int bestD = int.MaxValue;
            foreach (Hex h in screen.LayoutEmptyTiles())
            {
                int d = GardenGrid.Distance(h, near);
                if (d < bestD && S.Grid.KeepsStationsApart(carried, h)) { bestD = d; best = h; }
            }
            Assert.Less(bestD, int.MaxValue, "the lawn has a tile two from every station");
            return best;
        }

        GameObject DropTarget => root.transform.Find("Canvas/Garden/Content/LayoutRings/Target").gameObject;

        void HoldDragDrop(Hex from, Vector2 to)
        {
            screen.SimulateLayoutPress(screen.LayoutViewPoint(from));
            Step(GardenScreen.LayoutHoldSeconds + 0.05f);
            screen.SimulateLayoutDrag((screen.LayoutViewPoint(from) + to) / 2f);
            screen.SimulateLayoutDrag(to);
            screen.SimulateLayoutRelease(to);
            Step(0.5f);
        }

        [Test]
        public void The_Button_Beside_The_Stations_Tab_Opens_The_Mode_And_Done_Closes_It()
        {
            var bar = (RectTransform)root.transform.Find("Canvas/TabBar");
            var button = (RectTransform)bar.Find("Layout");
            var stations = (RectTransform)bar.Find("Open Trạm");
            Assert.IsNotNull(button.GetComponent<Button>());
            Assert.GreaterOrEqual(button.rect.width, UiKit.MinTouch);
            Assert.GreaterOrEqual(button.rect.height, UiKit.MinTouch);
            Assert.Less(button.anchorMax.x, stations.anchorMin.x + 0.001f, "it stands just left of the stations tab");
            Assert.AreEqual(L10n.T("layout.button"), button.Find("Label").GetComponent<Text>().text);

            Transform rings = root.transform.Find("Canvas/Garden/Content/LayoutRings");
            Assert.IsFalse(rings.gameObject.activeSelf, "no rings while the mode is off");
            button.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.LayoutMode);
            Assert.IsTrue(rings.gameObject.activeSelf);
            int empty = screen.LayoutEmptyTiles().Count, shown = 0;
            foreach (Transform ring in rings) if (ring.gameObject.activeSelf && ring.name.StartsWith("Ring ")) shown++;
            Assert.Greater(empty, 10, "a young garden has room");
            Assert.AreEqual(empty, shown, "one dashed ring on every empty tile");
            foreach (Hex h in screen.LayoutEmptyTiles()) Assert.IsNull(S.Grid.OccupantAt(h.Q, h.R));
            Assert.IsFalse(bar.gameObject.activeSelf, "the tab bar steps aside");
            Assert.IsFalse(root.transform.Find("Canvas/Hud").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/LayoutBar").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/LayoutCatcher").gameObject.activeSelf, "touches on the garden are the mode's");

            Find("Canvas/LayoutBar/Done").onClick.Invoke();
            Assert.IsFalse(screen.LayoutMode);
            Assert.IsFalse(rings.gameObject.activeSelf);
            Assert.IsTrue(bar.gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/Hud").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/LayoutBar").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/LayoutCatcher").gameObject.activeSelf);
        }

        [Test]
        public void A_One_Line_Hint_Shows_The_First_Time_Only()
        {
            screen.EnterLayoutMode();
            Transform hint = root.transform.Find("Canvas/LayoutHint");
            Assert.IsTrue(hint.gameObject.activeSelf);
            Assert.AreEqual(L10n.T("layout.hint"), hint.Find("Text").GetComponent<Text>().text);
            screen.ExitLayoutMode();
            Assert.IsFalse(hint.gameObject.activeSelf);
            screen.EnterLayoutMode();
            Assert.IsFalse(hint.gameObject.activeSelf, "once is enough");
        }

        [Test]
        public void Holding_A_Station_Lifts_It_And_Dropping_It_On_An_Empty_Tile_Stands_It_There()
        {
            screen.EnterLayoutMode();
            Hex from = TileOf(Id(1));
            Hex to = ApartTileNear(from, Id(1));
            screen.LookAt(GardenScreen.CellToWorld(from));
            RectTransform actor = Actor(1);
            float rest = actor.localScale.x;

            screen.SimulateLayoutPress(screen.LayoutViewPoint(from));
            Step(0.15f);
            Assert.IsNull(screen.LayoutLifted, "not yet: a hold is 0.3 s");
            Step(0.2f);
            Assert.AreEqual(Id(1), screen.LayoutLifted);
            Assert.AreEqual(rest * 1.06f, actor.localScale.x, 0.01f, "lifted: a little larger");
            Assert.IsNotNull(actor.Find("LiftShadow"), "with a shadow under it");
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(to) + new Vector2(30f, 10f));
            Assert.AreEqual(GardenScreen.CellToWorld(to).x + 30f / screen.ZoomScale, actor.anchoredPosition.x, 1f, "it follows the finger");
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(to) + new Vector2(30f, 10f));
            Step(0.5f);
            Assert.IsNull(screen.LayoutLifted);
            Assert.AreEqual(to, TileOf(Id(1)), "it stands on the tile it was dropped on");
            Near(GardenScreen.CellToWorld(to), actor.anchoredPosition, "and is drawn there");
            Assert.AreEqual(rest, actor.localScale.x, 0.01f);
            Assert.IsNull(actor.Find("LiftShadow"));
            Assert.AreEqual(12, S.Levels[1], "moving costs and changes nothing else");
            Assert.IsFalse(screen.LayoutEmptyTiles().Contains(to), "the tile is taken now");
            Assert.IsTrue(screen.LayoutEmptyTiles().Contains(from), "and the old one has a ring");
        }

        [Test]
        public void A_Short_Touch_Lifts_Nothing_And_Station_Taps_Are_Off()
        {
            screen.EnterLayoutMode();
            Hex tea = TileOf(Id(0));
            screen.SimulateLayoutPress(screen.LayoutViewPoint(tea));
            Step(0.1f);
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(tea));
            Step(0.4f);
            Assert.IsNull(screen.LayoutLifted);
            Assert.AreEqual(-1, screen.Selected, "no card opens in the mode");
            Assert.AreEqual(tea, TileOf(Id(0)));
            screen.SimulateLayoutPress(screen.LayoutViewPoint(tea));
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(tea) + new Vector2(0, 120f));
            Step(0.5f);
            Assert.IsNull(screen.LayoutLifted);
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(tea) + new Vector2(0, 120f));
        }

        [Test]
        public void Dropping_On_Another_Station_Swaps_Them()
        {
            screen.EnterLayoutMode();
            Hex a = TileOf(Id(1)), b = TileOf(Id(0));
            screen.LookAt((GardenScreen.CellToWorld(a) + GardenScreen.CellToWorld(b)) / 2f);
            HoldDragDrop(a, screen.LayoutViewPoint(b));
            Assert.AreEqual(b, TileOf(Id(1)));
            Assert.AreEqual(a, TileOf(Id(0)));
            Near(GardenScreen.CellToWorld(a), Actor(0).anchoredPosition, "the other one went to the free tile");
            Near(GardenScreen.CellToWorld(b), Actor(1).anchoredPosition, "and the carried one stands where the other stood");
        }

        [Test]
        public void A_Station_Dropped_Beside_Another_Springs_Back_And_No_Tile_Lights_Up_There()
        {
            Motion.SetReduced(true);
            screen.EnterLayoutMode();
            Hex from = TileOf(Id(1)), jam = TileOf(Id(2));
            Hex beside = default;
            bool found = false;
            foreach (Hex h in screen.LayoutEmptyTiles())
                if (GardenGrid.Distance(h, jam) == 1 && GardenGrid.Distance(h, from) > 1) { beside = h; found = true; break; }
            Assert.IsTrue(found, "an empty tile with a ring next to the jam pot");
            Hex apart = ApartTileNear(beside, Id(1));
            screen.LookAt((GardenScreen.CellToWorld(from) + GardenScreen.CellToWorld(beside)) / 2f);
            Step(0.1f);
            Vector2 home = Actor(1).anchoredPosition;

            screen.SimulateLayoutPress(screen.LayoutViewPoint(from));
            Step(GardenScreen.LayoutHoldSeconds + 0.05f);
            Assert.AreEqual(Id(1), screen.LayoutLifted);
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(apart));
            Assert.IsTrue(DropTarget.activeSelf, "a tile two from every station lights up");
            Near(GardenScreen.CellToWorld(apart), ((RectTransform)DropTarget.transform).anchoredPosition, "under the finger");
            screen.SimulateLayoutDrag(screen.LayoutViewPoint(beside));
            Assert.IsFalse(DropTarget.activeSelf, "the tile next to the jam pot does not light up");
            screen.SimulateLayoutRelease(screen.LayoutViewPoint(beside));
            Assert.IsNull(screen.LayoutLifted);
            Assert.AreEqual(from, TileOf(Id(1)), "it springs back to its own tile");
            Assert.IsNull(S.Grid.OccupantAt(beside.Q, beside.R));
            Near(home, Actor(1).anchoredPosition, "and is drawn there");
            Assert.IsFalse(DropTarget.activeSelf);

            Assert.AreEqual(LayoutDrop.Back, runner.DropInLayout(from, beside), "the garden's rules say the same");
        }

        [Test]
        public void Dropping_Outside_The_Lawn_Sends_It_Back_At_Once_Under_Reduced_Motion()
        {
            Motion.SetReduced(true);
            screen.EnterLayoutMode();
            Hex from = TileOf(Id(2));
            screen.LookAt(GardenScreen.CellToWorld(from));
            Vector2 at = Actor(2).anchoredPosition;
            screen.SimulateLayoutPress(screen.LayoutViewPoint(from));
            Step(GardenScreen.LayoutHoldSeconds + 0.05f);
            Vector2 outside = screen.LayoutViewPoint(new Hex(from.Q, from.R + GardenGrid.HalfRows * 3));
            screen.SimulateLayoutDrag(outside);
            screen.SimulateLayoutRelease(outside);
            Assert.AreEqual(from, TileOf(Id(2)));
            Near(at, Actor(2).anchoredPosition, "back on its tile with no tween to wait for");
            Assert.IsNull(screen.LayoutLifted);
        }

        [Test]
        public void Dropping_On_The_Storage_Slot_Puts_The_Station_Away_And_The_Stations_Tab_Takes_It_Out()
        {
            Rabbit r = Rabbits.Arrive(S, 2, "Lam", 0d);
            Rabbits.Assign(S, r.Id, 2);
            screen.Refresh();
            screen.EnterLayoutMode();
            Hex from = TileOf(Id(2));
            screen.LookAt(GardenScreen.CellToWorld(from));
            HoldDragDrop(from, screen.LayoutStorePoint);
            Assert.IsTrue(S.Grid.IsStored(Id(2)), "in storage");
            Assert.AreEqual(9, S.Levels[2], "keeping its level");
            Assert.AreEqual(-1, r.Station, "its rabbit rests");
            Assert.IsFalse(Actor(2).gameObject.activeSelf, "no longer in the garden");
            StringAssert.Contains("1", root.transform.Find("Canvas/LayoutBar/Store/Count").GetComponent<Text>().text);
            Assert.IsTrue(screen.LayoutEmptyTiles().Contains(from));

            screen.ExitLayoutMode();
            screen.OpenSheet(0);
            screen.Refresh();
            Transform row = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Stored " + runner.Defs[2].Id);
            Assert.IsTrue(row.gameObject.activeSelf, "the stations tab lists it");
            Find("Canvas/Sheet/Panel/Scroll/Content/Stored " + runner.Defs[2].Id + "/TakeOut").onClick.Invoke();
            screen.Refresh();
            Assert.IsFalse(S.Grid.IsStored(Id(2)));
            Assert.IsNotNull(S.Grid.TileOf(Id(2)), "back on the lawn");
            Assert.IsFalse(row.gameObject.activeSelf);
            Assert.IsTrue(Actor(2).gameObject.activeSelf);
            Near(screen.StationSpot(2), Actor(2).anchoredPosition, "standing on its new tile");
        }

        [Test]
        public void The_Teapot_And_A_Place_To_Rest_Go_Back_From_The_Storage_Slot()
        {
            screen.EnterLayoutMode();
            Hex tea = TileOf(Id(0));
            screen.LookAt(GardenScreen.CellToWorld(tea));
            HoldDragDrop(tea, screen.LayoutStorePoint);
            Assert.IsFalse(S.Grid.IsStored(Id(0)));
            Assert.AreEqual(tea, TileOf(Id(0)));
        }

        [Test]
        public void A_Place_To_Rest_Can_Be_Moved_Too()
        {
            screen.EnterLayoutMode();
            Hex seat = TileOf(GardenGrid.RestId(0));
            Hex to = EmptyTileNear(seat);
            screen.LookAt(GardenScreen.CellToWorld(seat));
            HoldDragDrop(seat, screen.LayoutViewPoint(to));
            Assert.AreEqual(to, TileOf(GardenGrid.RestId(0)));
            Near(GardenScreen.CellToWorld(to), screen.RestSpot(0), "the place to rest is on its new tile");
        }

        [Test]
        public void Done_After_A_Change_Builds_The_Garden_Again_Around_The_New_Places()
        {
            screen.EnterLayoutMode();
            Hex from = TileOf(Id(3)), to = ApartTileNear(TileOf(Id(0)), Id(3));
            screen.LookAt(GardenScreen.CellToWorld(from));
            HoldDragDrop(from, screen.LayoutViewPoint(to));
            screen.ExitLayoutMode();
            Assert.IsFalse(screen.LayoutMode);
            Assert.AreEqual(to, TileOf(Id(3)));
            Near(GardenScreen.CellToWorld(to), Actor(3).anchoredPosition, "the rebuilt garden stands it on its tile");
            Assert.IsTrue(root.transform.Find("Canvas/TabBar").gameObject.activeSelf);
            Assert.IsNull(root.transform.Find("Canvas (old)"), "the old canvas is gone");
        }

        [Test]
        public void A_Decoration_Cannot_Be_Dropped_By_The_Firefly_Jar_And_No_Ring_Is_Drawn_On_A_Reserved_Tile()
        {
            Motion.SetReduced(true);
            S.Plots = 8;
            GardenGrid.Settle(runner.Defs, S);
            S.GardenXp = Garden.XpForLevel(9);
            S.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(S, "den-da") && Decor.Place(S, 7, "den-da"), "a stone lantern on the spot by the tea shop");
            screen.RebuildUi();
            screen.Refresh();
            Hex jar = GardenScreen.WorldToCell(GardenScreen.FireflyJarSpot), lantern = TileOf(GardenGrid.DecorId(7));
            Assert.AreEqual(new Hex(-3, -1), jar, "the jar's tile on the strip");
            Assert.IsTrue(S.Grid.InLawn(jar) && S.Grid.OccupantAt(jar.Q, jar.R) == null, "on the lawn and empty");
            screen.EnterLayoutMode();
            screen.LookAt((GardenScreen.CellToWorld(lantern) + GardenScreen.CellToWorld(jar)) / 2f);
            Step(0.1f);
            Transform rings = root.transform.Find("Canvas/Garden/Content/LayoutRings");
            List<Hex> offered = screen.LayoutEmptyTiles();
            foreach (Hex h in GardenGrid.ReservedTiles)
            {
                Assert.IsFalse(offered.Contains(h), h + " is not offered");
                Transform ring = rings.Find("Ring " + h.Q + "," + h.R);
                Assert.IsTrue(ring == null || !ring.gameObject.activeSelf, "no dashed ring on " + h);
            }
            var spot = (RectTransform)root.transform.Find("Canvas/Garden/Content/Spot 7");
            foreach (Hex kept in new[] { jar, new Hex(-3, 0), new Hex(3, -1), new Hex(1, -2), new Hex(-1, -1), new Hex(-2, -2) })
            {
                HoldDragDrop(lantern, screen.LayoutViewPoint(kept));
                Assert.IsNull(screen.LayoutLifted);
                Assert.AreEqual(lantern, TileOf(GardenGrid.DecorId(7)), "dropped on " + kept + ": back where it stood");
                Near(GardenScreen.CellToWorld(lantern), spot.anchoredPosition, "and drawn there");
            }
            Hex beside = EmptyTileNear(jar);
            Assert.AreEqual(1, GardenGrid.Distance(beside, jar));
            HoldDragDrop(lantern, screen.LayoutViewPoint(beside));
            Assert.AreEqual(beside, TileOf(GardenGrid.DecorId(7)), "a free tile beside the jar still takes it");
        }

        [Test]
        public void A_Station_Dropped_On_An_Empty_Decoration_Spot_Springs_Back_And_The_Plus_Stays_On_Its_Tile()
        {
            Motion.SetReduced(true);
            S.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(S, "den-da"), "one stone lantern bought and not placed yet");
            screen.Refresh();
            Hex spot = GardenGrid.DefaultDecorTile(3), jam = TileOf(Id(2));
            Assert.AreEqual(new Hex(3, 1), spot);
            screen.EnterLayoutMode();
            screen.LookAt(GardenScreen.CellToWorld(spot));
            Step(0.1f);
            Assert.IsFalse(screen.LayoutEmptyTiles().Contains(spot), "no ring is drawn on the plus");
            Transform ring = root.transform.Find("Canvas/Garden/Content/LayoutRings/Ring " + spot.Q + "," + spot.R);
            Assert.IsTrue(ring == null || !ring.gameObject.activeSelf);
            HoldDragDrop(jam, screen.LayoutViewPoint(spot));
            Assert.IsNull(screen.LayoutLifted);
            Assert.AreEqual(jam, TileOf(Id(2)), "the jam pot springs back");
            Near(GardenScreen.CellToWorld(jam), Actor(2).anchoredPosition, "and is drawn there");
            screen.ExitLayoutMode();
            var plus = (RectTransform)root.transform.Find("Canvas/Garden/Content/Spot 3");
            Assert.IsTrue(plus.Find("Plate").gameObject.activeInHierarchy, "the plus is out");
            Near(GardenScreen.CellToWorld(spot), plus.anchoredPosition, "on its own tile");
            Assert.AreEqual(GardenGrid.DecorId(3), S.Grid.OccupantAt(spot.Q, spot.R));
        }

        [Test]
        public void The_Leaf_Basket_Goes_With_Its_Station_And_Its_Old_Tile_Lights_Up()
        {
            Motion.SetReduced(true);
            S.GardenXp = Garden.XpForLevel(Leaves.UnlockLevel);
            screen.Refresh();
            int leaf = Leaves.StationIndex(runner.Defs);
            Hex home = TileOf(GardenGrid.BasketId), plate = TileOf(GardenGrid.BasketPlateId);
            var basket = (RectTransform)root.transform.Find("Canvas/Garden/Content/Basket");
            Assert.IsTrue(basket.gameObject.activeSelf, "the basket is out from garden level four");
            Vector2 before = basket.anchoredPosition;
            screen.EnterLayoutMode();
            Assert.IsFalse(screen.LayoutEmptyTiles().Contains(home), "no ring under the basket");
            Assert.IsFalse(screen.LayoutEmptyTiles().Contains(plate), "nor under its plate");
            Hex from = TileOf(Id(leaf)), to = ApartTileNear(TileOf(Id(0)), Id(leaf));
            screen.LookAt(GardenScreen.CellToWorld(from));
            HoldDragDrop(from, screen.LayoutViewPoint(to));
            Assert.AreEqual(to, TileOf(Id(leaf)));
            Hex now = TileOf(GardenGrid.BasketId);
            Assert.AreEqual(1, GardenGrid.Distance(now, to), "the basket stands beside its station");
            Assert.IsTrue(screen.LayoutEmptyTiles().Contains(home), "its old tile is offered");
            Vector2 shift = GardenScreen.CellToWorld(now) - GardenScreen.CellToWorld(home);
            Near(before + shift, basket.anchoredPosition, "and it is drawn there while arranging");
            screen.ExitLayoutMode();
            basket = (RectTransform)root.transform.Find("Canvas/Garden/Content/Basket");
            Near(before + shift, basket.anchoredPosition, "and after Done");
        }

        static string OneLine(Text text)
        {
            Vector2 size = text.rectTransform.rect.size;
            var gen = new TextGenerator();
            TextGenerationSettings settings = text.GetGenerationSettings(size);
            gen.Populate(text.text, settings);
            int used = text.resizeTextForBestFit ? gen.fontSizeUsedForBestFit : settings.fontSize;
            settings.fontSize = used;
            settings.resizeTextForBestFit = false;
            settings.horizontalOverflow = HorizontalWrapMode.Overflow;
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            float ppu = text.pixelsPerUnit;
            float w = gen.GetPreferredWidth(text.text, settings) / ppu, h = TextFit.Height(gen, text.text, settings) / ppu;
            if (w <= size.x + 1f && h <= size.y + 1f && used / ppu >= 18f) return null;
            return text.name + " \"" + text.text + "\" at " + Mathf.RoundToInt(used / ppu) + "px is " + Mathf.RoundToInt(w) + "x" + Mathf.RoundToInt(h) + " in " + Mathf.RoundToInt(size.x) + "x" + Mathf.RoundToInt(size.y);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Mode_s_Words_Fit_In_Both_Languages_And_Large_Text(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            Layout.Store(runner.Defs, S, Id(2));
            screen.RebuildUi();
            Canvas.ForceUpdateCanvases();
            var wrong = new List<string>();
            var button = (RectTransform)root.transform.Find("Canvas/TabBar/Layout");
            Assert.GreaterOrEqual(Mathf.Min(button.rect.width, button.rect.height), UiKit.MinTouch);
            Check(wrong, "Canvas/TabBar/Layout/Label");
            screen.EnterLayoutMode();
            Canvas.ForceUpdateCanvases();
            foreach (string path in new[] { "Canvas/LayoutBar/Store/Label", "Canvas/LayoutBar/Store/Count", "Canvas/LayoutBar/Done/Face/Text", "Canvas/LayoutHint/Text" })
                Check(wrong, path);
            foreach (Button b in root.transform.Find("Canvas/LayoutBar").GetComponentsInChildren<Button>())
                Assert.GreaterOrEqual(Mathf.Min(((RectTransform)b.transform).rect.width, ((RectTransform)b.transform).rect.height), UiKit.MinTouch, b.name);
            screen.ExitLayoutMode();
            screen.OpenSheet(0);
            screen.Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
            Canvas.ForceUpdateCanvases();
            string row = "Canvas/Sheet/Panel/Scroll/Content/Stored " + runner.Defs[2].Id;
            Check(wrong, row + "/Name");
            Check(wrong, row + "/TakeOut/Face/Text");
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }

        void Check(List<string> wrong, string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing " + path);
            string line = OneLine(t.GetComponent<Text>());
            if (line != null) wrong.Add(path + ": " + line);
        }
    }
}

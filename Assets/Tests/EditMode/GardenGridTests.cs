using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class GardenGridTests
    {
        List<StationDef> defs;

        [SetUp]
        public void SetUp() { defs = Fixture.Defs(); }

        [Test]
        public void The_Strip_Starts_With_Fifty_Three_Tiles_In_Seven_Rows_And_Each_Segment_Adds_Twenty_One()
        {
            Assert.AreEqual(53, GardenGrid.TileCount(0));
            Assert.AreEqual(74, GardenGrid.TileCount(1));
            Assert.AreEqual(53 + 6 * 21, GardenGrid.TileCount(6));
            var start = new GardenGrid();
            Assert.AreEqual(0, start.Segments);
            Assert.AreEqual(53, start.LawnTiles);
            var seen = new HashSet<Hex>();
            var rows = new Dictionary<int, int>();
            foreach (TileData t in start.Tiles())
            {
                Assert.IsTrue(seen.Add(t.Hex), "every tile once");
                Assert.AreEqual(GardenGrid.IsReserved(t.Hex) ? TileState.Reserved : TileState.UnlockedEmpty, t.State);
                Assert.IsNull(t.OccupantId);
                Assert.LessOrEqual(System.Math.Abs(GardenGrid.Column(t.Hex)), GardenGrid.StartHalfWidth);
                rows[t.R] = rows.TryGetValue(t.R, out int n) ? n + 1 : 1;
            }
            Assert.AreEqual(53, seen.Count);
            CollectionAssert.AreEquivalent(new[] { -3, -2, -1, 0, 1, 2, 3 }, rows.Keys, "seven rows, the teapot's in the middle");
            for (int r = -3; r <= 3; r++) Assert.AreEqual(r % 2 == 0 ? 7 : 8, rows[r], "row " + r);
            Assert.AreEqual(new Hex(0, 0), start.Tiles()[0].Hex, "the teapot's tile first");
            Assert.AreEqual(TileState.ForestBorder, start.Tile(0, -4).State, "above the top row is wood");
            Assert.AreEqual(TileState.ForestBorder, start.Tile(4, 0).State, "past the middle is mist until a segment opens");
            for (int n = 1; n <= GardenGrid.MaxSegments; n++)
            {
                List<Hex> segment = GardenGrid.SegmentTiles(n);
                Assert.AreEqual(GardenGrid.TilesPerSegment, segment.Count, "segment " + n);
                var perRow = new Dictionary<int, int>();
                foreach (Hex h in segment)
                {
                    Assert.AreEqual(n, GardenGrid.SegmentOf(h));
                    Assert.AreEqual(GardenGrid.SideOf(n) == GardenSide.Right, GardenGrid.Column(h) > 0, h + " on the side segment " + n + " opens");
                    perRow[h.R] = perRow.TryGetValue(h.R, out int k) ? k + 1 : 1;
                }
                foreach (KeyValuePair<int, int> row in perRow) Assert.AreEqual(3, row.Value, "three tiles a row, row " + row.Key + " of segment " + n);
                Assert.AreEqual(7, perRow.Count);
            }
        }

        [Test]
        public void Segments_Open_Right_Then_Left_Each_Next_To_The_Last_On_Its_Side()
        {
            Assert.AreEqual(GardenSide.Right, GardenGrid.SideOf(1));
            Assert.AreEqual(GardenSide.Left, GardenGrid.SideOf(2));
            Assert.AreEqual(GardenSide.Right, GardenGrid.SideOf(5));
            Assert.AreEqual(GardenSide.Left, GardenGrid.SideOf(6));
            var g = new GardenGrid();
            for (int n = 1; n <= 6; n++)
            {
                g.Segments = n;
                Assert.AreEqual(GardenGrid.TileCount(n), g.LawnTiles);
                Assert.AreEqual(GardenGrid.TileCount(n), GardenGrid.Strip(n).Count);
                for (int r = -3; r <= 3; r++)
                {
                    int lo = int.MaxValue, hi = int.MinValue, count = 0;
                    foreach (Hex h in GardenGrid.Strip(n)) if (h.R == r) { lo = System.Math.Min(lo, h.Q); hi = System.Math.Max(hi, h.Q); count++; }
                    Assert.AreEqual(hi - lo + 1, count, "row " + r + " with " + n + " segments open has no gap");
                }
                var reached = new HashSet<Hex> { new Hex(0, 0) };
                var queue = new Queue<Hex>(reached);
                while (queue.Count > 0) foreach (Hex o in g.LawnNeighbours(queue.Dequeue())) if (reached.Add(o)) queue.Enqueue(o);
                Assert.AreEqual(g.LawnTiles, reached.Count, n + " segments open: one lawn");
                foreach (Hex h in GardenGrid.SegmentTiles(n + 1)) Assert.IsFalse(g.InLawn(h), h + " is still in the mist");
            }
            Assert.AreEqual(-1, GardenGrid.SegmentOf(new Hex(0, 4)), "off the strip's rows");
            Assert.AreEqual(-1, GardenGrid.SegmentOf(new Hex(40, 0)), "past the last segment");
        }

        [Test]
        public void The_Teapot_Has_Six_Neighbours_And_An_Edge_Tile_Keeps_Only_Those_On_The_Lawn()
        {
            var g = new GardenGrid();
            Hex[] around = GardenGrid.Neighbours(new Hex(0, 0));
            Assert.AreEqual(6, around.Length);
            foreach (Hex h in around) Assert.AreEqual(1, GardenGrid.Distance(h), h + " touches the teapot's tile");
            Assert.AreEqual(6, g.LawnNeighbours(new Hex(0, 0)).Count);
            Assert.AreEqual(4, g.LawnNeighbours(new Hex(1, -3)).Count, "the top row");
            Assert.AreEqual(3, g.LawnNeighbours(new Hex(4, -1)).Count, "the right end of an odd row");
            Assert.IsFalse(g.InLawn(4, 0));
            g.Segments = 1;
            Assert.IsTrue(g.InLawn(4, 0), "the first segment opens to the right");
            Assert.IsFalse(g.InLawn(-4, 0), "and not to the left");
        }

        [Test]
        public void Distance_To_The_Centre_Is_The_Hex_Distance()
        {
            Assert.AreEqual(0, GardenGrid.Distance(new Hex(0, 0)));
            Assert.AreEqual(1, GardenGrid.Distance(new Hex(1, -1)));
            Assert.AreEqual(2, GardenGrid.Distance(new Hex(2, 0)));
            Assert.AreEqual(2, GardenGrid.Distance(new Hex(-1, -1)));
            Assert.AreEqual(3, GardenGrid.Distance(new Hex(-3, 2)));
            Assert.AreEqual(5, GardenGrid.Distance(new Hex(2, 3), new Hex(-2, 2)));
            foreach (Hex h in GardenGrid.Ring(4)) Assert.AreEqual(4, GardenGrid.Distance(h));
            Assert.AreEqual(24, GardenGrid.Ring(4).Count);
        }

        [Test]
        public void Placing_And_Removing_An_Occupant_And_No_Two_On_One_Tile()
        {
            var g = new GardenGrid(2);
            Assert.IsTrue(g.Place("station:tea", 0, 0));
            Assert.AreEqual(TileState.Occupied, g.Tile(0, 0).State);
            Assert.AreEqual("station:tea", g.Tile(0, 0).OccupantId);
            Assert.AreEqual(new Hex(0, 0), g.TileOf("station:tea"));
            Assert.IsFalse(g.Place("rest:0", 0, 0), "the tile is taken");
            Assert.IsNull(g.TileOf("rest:0"));
            Assert.IsTrue(g.Place("rest:0", 1, 0));
            Assert.IsTrue(g.Place("rest:0", 1, -1), "an occupant placed again moves");
            Assert.AreEqual(TileState.UnlockedEmpty, g.Tile(1, 0).State, "and its old tile is free");
            Assert.IsFalse(g.Place("rest:1", 0, 4), "nothing is placed off the strip");
            Assert.IsFalse(g.Place("rest:1", -7, 0), "nor in the mist of a segment not open");
            Assert.AreEqual(2, g.OccupantCount);
            Assert.IsTrue(g.Remove("rest:0"));
            Assert.IsFalse(g.Remove("rest:0"));
            Assert.AreEqual(TileState.UnlockedEmpty, g.Tile(1, -1).State);
            Assert.AreEqual(1, g.OccupantCount);
            int occupied = 0;
            foreach (TileData t in g.Tiles()) if (t.State == TileState.Occupied) occupied++;
            Assert.AreEqual(1, occupied);
        }

        [Test]
        public void The_Nearest_Empty_Tile_To_The_Teapot_Is_Found_Ring_By_Ring()
        {
            var g = new GardenGrid();
            Assert.IsTrue(g.NearestEmpty(out Hex first));
            Assert.AreEqual(new Hex(0, 0), first);
            g.Place("a", 0, 0);
            Assert.IsTrue(g.NearestEmpty(out Hex second));
            Assert.AreEqual(1, GardenGrid.Distance(second));
            int k = 0;
            foreach (Hex h in GardenGrid.Strip(0)) if (g.OccupantAt(h.Q, h.R) == null) g.Place("decor:" + (100 + k++), h.Q, h.R, true);
            Assert.IsFalse(g.NearestEmpty(out _), "a full lawn has no empty tile");
            g.Segments = 1;
            Assert.IsTrue(g.NearestEmpty(out Hex third));
            Assert.AreEqual(1, GardenGrid.SegmentOf(third), "a segment out of the mist has room again");
            Assert.IsTrue(g.NearestEmpty(new Hex(5, 0), out Hex near));
            Assert.AreEqual(new Hex(5, 0), near, "an empty tile asked for is its own nearest");
            IReadOnlyList<Hex> strip = GardenGrid.Strip(3);
            for (int i = 1; i < strip.Count; i++) Assert.LessOrEqual(GardenGrid.Distance(strip[i - 1]), GardenGrid.Distance(strip[i]));
        }

        [Test]
        public void Each_Land_Wave_Opens_One_Segment_And_The_Default_Table_Fits_The_Strip()
        {
            Assert.AreEqual(0, GardenGrid.SegmentsForPlots(0));
            Assert.AreEqual(0, GardenGrid.SegmentsForPlots(4));
            Assert.AreEqual(1, GardenGrid.SegmentsForPlots(6));
            Assert.AreEqual(2, GardenGrid.SegmentsForPlots(8));
            Assert.AreEqual(3, GardenGrid.SegmentsForPlots(10));
            Assert.AreEqual(4, GardenGrid.SegmentsForPlots(12));
            Assert.AreEqual(6, GardenGrid.SegmentsForPlots(16));
            var taken = new HashSet<Hex>();
            for (int i = 0; i < 16; i++)
            {
                Hex h = GardenGrid.DefaultStationTile(i);
                Assert.IsTrue(taken.Add(h), "station " + i);
                int segment = GardenGrid.SegmentOf(h);
                Assert.That(segment, Is.InRange(0, GardenGrid.SegmentsForPlots(i + 1)), "station " + i + " stands on land open once its wave is taken");
                Assert.That(h.R, Is.InRange(-2, 2), "station " + i + " stands off the strip's top and bottom rows");
            }
            for (int n = 0; n < RestSpots.Max; n++)
            {
                Hex h = GardenGrid.DefaultRestTile(n);
                Assert.IsTrue(taken.Add(h), "rest " + n);
                Assert.That(GardenGrid.SegmentOf(h), Is.InRange(0, System.Math.Max(0, n - RestSpots.Starting + 1)), "rest " + n + " on land open once it comes");
            }
            for (int n = 0; n < Decor.Spots; n++) { Hex h = GardenGrid.DefaultDecorTile(n); Assert.IsTrue(taken.Add(h), "decor " + n); Assert.AreEqual(0, GardenGrid.SegmentOf(h), "decor " + n + " on the starting lawn"); }
            foreach (string id in new[] { GardenGrid.MortarId, GardenGrid.ShopId, GardenGrid.CounterId })
            {
                Hex h = GardenGrid.DefaultTile(id, defs);
                Assert.IsTrue(taken.Add(h), id);
                Assert.AreEqual(0, GardenGrid.SegmentOf(h), id + " on the starting lawn");
            }
            Assert.GreaterOrEqual(GardenGrid.TileCount(6), taken.Count + GardenGrid.ReservedTiles.Count, "the final lawn has a tile for everything");
            for (int i = 0; i < 16; i++)
                for (int j = i + 1; j < 16; j++)
                    Assert.GreaterOrEqual(GardenGrid.Distance(GardenGrid.DefaultStationTile(i), GardenGrid.DefaultStationTile(j)), Expansion.StationSpacing, i + " and " + j);
            var more = new HashSet<Hex>(taken);
            for (int i = 16; i < 24; i++)
            {
                Hex h = GardenGrid.DefaultStationTile(i);
                Assert.IsTrue(more.Add(h), "station " + i);
                Assert.IsFalse(GardenGrid.IsReserved(h));
            }
            Assert.Greater(GardenGrid.Column(GardenGrid.DefaultStationTile(16)), GardenGrid.Column(GardenGrid.DefaultStationTile(14)), "the seventh segment opens right, past the fifth");
            Assert.Less(GardenGrid.Column(GardenGrid.DefaultStationTile(18)), GardenGrid.Column(GardenGrid.DefaultStationTile(15)), "the eighth left, past the sixth");
        }

        [Test]
        public void Ingredient_Stations_Stand_By_What_They_Feed_On_The_Strip()
        {
            var feeds = new Dictionary<int, int[]> { { 1, new[] { 0, 6 } }, { 3, new[] { 2, 7 } }, { 5, new[] { 4, 2 } }, { 8, new[] { 6, 9 } }, { 10, new[] { 7, 11 } }, { 12, new[] { 9, 11 } }, { 14, new[] { 13, 6 } } };
            float Apart(int a, int b)
            {
                Hex x = GardenGrid.DefaultStationTile(a), y = GardenGrid.DefaultStationTile(b);
                double dx = GardenGrid.LocalX(x.Q, x.R) - GardenGrid.LocalX(y.Q, y.R), dy = GardenGrid.LocalY(x.Q, x.R) - GardenGrid.LocalY(y.Q, y.R);
                return (float)System.Math.Sqrt(dx * dx + dy * dy);
            }
            foreach (KeyValuePair<int, int[]> f in feeds)
            {
                float nearest = float.MaxValue;
                foreach (int target in f.Value)
                {
                    nearest = System.Math.Min(nearest, Apart(f.Key, target));
                    if (f.Key < 12) Assert.Less(Apart(f.Key, target), 520f, f.Key + " stands by " + target);
                }
                Assert.Less(nearest, 520f, f.Key + " stands by one station it feeds");
            }
            foreach (int left in new[] { 7, 10, 11, 15 }) Assert.Less(GardenGrid.Column(GardenGrid.DefaultStationTile(left)), 0, "station " + left + " on the pond side");
            foreach (int right in new[] { 4, 5, 9, 13 }) Assert.Greater(GardenGrid.Column(GardenGrid.DefaultStationTile(right)), 0, "station " + right + " on the oven side");
        }

        [Test]
        public void A_New_Game_Stands_Its_Starting_Stations_Seats_And_Props_On_The_Grid()
        {
            GameState s = GameState.NewGame(defs);
            GardenGrid g = s.Grid;
            Assert.AreEqual(0, g.Segments);
            Assert.AreEqual(new Hex(0, 0), g.TileOf("station:tea"), "the teapot is the heart");
            Assert.AreEqual(GardenGrid.DefaultStationTile(1), g.TileOf("station:leaf"));
            Assert.AreEqual(GardenGrid.DefaultStationTile(2), g.TileOf("station:jam"));
            Assert.AreEqual(GardenGrid.DefaultRestTile(0), g.TileOf("rest:0"));
            Assert.AreEqual(GardenGrid.DefaultRestTile(1), g.TileOf("rest:1"));
            Assert.IsNull(g.TileOf("rest:2"), "two places to rest at the start");
            Assert.IsNotNull(g.TileOf(GardenGrid.MortarId));
            Assert.IsNotNull(g.TileOf(GardenGrid.ShopId));
            Assert.IsNotNull(g.TileOf(GardenGrid.CounterId));
            Assert.IsNull(g.TileOf("decor:0"), "an empty decoration spot holds nothing");
            Assert.AreEqual(3 + 2 + 3, g.OccupantCount);
        }

        [Test]
        public void Land_Waves_Decorations_And_The_New_Moon_Keep_The_Grid_In_Step()
        {
            var six = new List<StationDef>(defs);
            for (int i = 3; i < 6; i++) six.Add(new StationDef { Id = "s" + i, Name = "S" + i, Kind = StationKind.Brew, BaseCost = 1e4 * i, CostGrowth = 1.1, BaseIncome = 1, CycleSeconds = 1 });
            GameState land = GameState.NewGame(six);
            Assert.IsNull(land.Grid.TileOf("station:s4"));
            land.Coins = new BigNum(1e9); land.GardenXp = Garden.XpForLevel(50);
            Assert.IsTrue(Expansion.Expand(six, land));
            Assert.AreEqual(6, land.Plots);
            Assert.AreEqual(GardenGrid.DefaultStationTile(4), land.Grid.TileOf("station:s4"), "the new plot's station has its tile");
            Assert.AreEqual(GardenGrid.DefaultStationTile(5), land.Grid.TileOf("station:s5"));
            Assert.AreEqual(GardenGrid.DefaultRestTile(2), land.Grid.TileOf("rest:2"), "and so has the new place to rest");
            Assert.AreEqual(1, land.Grid.Segments);
            GameState s = GameState.NewGame(defs);
            s.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(s, "den-da"));
            Assert.IsTrue(Decor.Place(s, 3, "den-da"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(3), s.Grid.TileOf("decor:3"));
            Assert.IsTrue(Decor.Buy(s, "bon-hoa"));
            Assert.IsTrue(Decor.Swap(s, 3, "bon-hoa"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(3), s.Grid.TileOf("decor:3"), "swapping keeps the spot's tile");
            Assert.IsTrue(Decor.Store(s, 3));
            Assert.IsNull(s.Grid.TileOf("decor:3"));
            Assert.IsTrue(Decor.Place(s, 5, "den-da"));
            s.RestSpots = 5; s.Grid.Segments = 3; GardenGrid.Settle(defs, s);
            Assert.IsNotNull(s.Grid.TileOf("rest:4"));
            s.Season = 0; s.GardenXp = Garden.XpForLevel(NewMoon.LevelNeeded(0));
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));
            Assert.AreEqual(0, s.Grid.Segments);
            Assert.AreEqual(GardenGrid.DefaultStationTile(2), s.Grid.TileOf("station:jam"), "three stations are within the starting plots");
            Assert.IsNull(s.Grid.TileOf("rest:2"));
            Assert.IsNull(s.Grid.TileOf("rest:4"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(5), s.Grid.TileOf("decor:5"));
        }

        static List<Hex> FarTiles(GardenGrid g, int segment, int count)
        {
            var far = new List<Hex>();
            foreach (Hex h in GardenGrid.SegmentTiles(segment))
                if (!GardenGrid.IsReserved(h) && g.OccupantAt(h.Q, h.R) == null && far.Count < count) far.Add(h);
            return far;
        }

        static void AssertAllOnTheLawn(GardenGrid g)
        {
            foreach (KeyValuePair<string, Hex> o in g.Occupants)
                Assert.IsTrue(g.InLawn(o.Value), o.Key + " stands at " + o.Value + ", under the mist of segment " + GardenGrid.SegmentOf(o.Value));
            var tiles = new HashSet<Hex>();
            foreach (KeyValuePair<string, Hex> o in g.Occupants) Assert.IsTrue(tiles.Add(o.Value), "nothing shares a tile: " + o.Key);
        }

        [Test]
        public void After_A_New_Moon_The_Teapot_And_Decorations_Moved_Onto_Far_Land_Come_Back_To_The_Lawn()
        {
            GameState s = GameState.NewGame(defs);
            s.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(s, "den-da")); Assert.IsTrue(Decor.Place(s, 0, "den-da"));
            Assert.IsTrue(Decor.Buy(s, "bon-hoa")); Assert.IsTrue(Decor.Place(s, 4, "bon-hoa"));
            s.Grid.Segments = 3;
            List<Hex> far = FarTiles(s.Grid, 3, 3);
            Assert.IsTrue(s.Grid.Move(new Hex(0, 0), far[0]), "the teapot is dragged onto the third segment's land");
            Assert.IsTrue(s.Grid.Move(GardenGrid.DefaultDecorTile(0), far[1]));
            Assert.IsTrue(s.Grid.Move(GardenGrid.DefaultDecorTile(4), far[2]));
            Assert.IsTrue(s.Grid.Move(GardenGrid.DefaultStationTile(1), new Hex(0, 0)), "and the leaf basket onto the teapot's home");

            s.Season = 0; s.GardenXp = Garden.XpForLevel(NewMoon.LevelNeeded(0));
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));

            Assert.AreEqual(0, s.Grid.Segments);
            AssertAllOnTheLawn(s.Grid);
            Assert.AreEqual(new Hex(0, 0), s.Grid.TileOf("station:tea"), "the teapot is home");
            Assert.AreEqual(GardenGrid.DefaultStationTile(1), s.Grid.TileOf("station:leaf"), "the leaf basket made way, to its own tile");
            Assert.AreEqual(GardenGrid.DefaultDecorTile(0), s.Grid.TileOf("decor:0"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(4), s.Grid.TileOf("decor:4"));

            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            AssertAllOnTheLawn(back.Grid);
            Assert.AreEqual(new Hex(0, 0), back.Grid.TileOf("station:tea"), "and stays home after a save");
        }

        [Test]
        public void After_A_New_Moon_The_Seats_Moved_Onto_Far_Land_Come_Back_To_The_Lawn()
        {
            GameState s = GameState.NewGame(defs);
            s.Grid.Segments = 4;
            List<Hex> right = FarTiles(s.Grid, 3, 1), left = FarTiles(s.Grid, 4, 1);
            Assert.IsTrue(s.Grid.Move(GardenGrid.DefaultRestTile(0), right[0]), "a stump goes to the third segment, on the right");
            Assert.IsTrue(s.Grid.Move(GardenGrid.DefaultRestTile(1), left[0]), "the other to the fourth, on the left");

            s.Season = 0; s.GardenXp = Garden.XpForLevel(NewMoon.LevelNeeded(0));
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));

            AssertAllOnTheLawn(s.Grid);
            Assert.AreEqual(GardenGrid.DefaultRestTile(0), s.Grid.TileOf("rest:0"));
            Assert.AreEqual(GardenGrid.DefaultRestTile(1), s.Grid.TileOf("rest:1"));
            Assert.AreEqual(new Hex(0, 0), s.Grid.TileOf("station:tea"));
        }

        [Test]
        public void An_Old_Save_With_Things_Beyond_Its_Open_Land_Stands_Them_On_The_Lawn_And_Keeps_The_Store()
        {
            var g0 = new GardenGrid();
            List<Hex> far = FarTiles(g0, 3, 3), farLeft = FarTiles(g0, 2, 1);
            string Tile(Hex h, string id) => "{\"q\":" + h.Q + ",\"r\":" + h.R + ",\"id\":\"" + id + "\"}";
            string json = "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":30},{\"id\":\"leaf\",\"level\":5},{\"id\":\"jam\",\"level\":2}],"
                + "\"plots\":3,\"restSpots\":2,"
                + "\"decor\":{\"owned\":{\"den-da\":1},\"spots\":[\"den-da\",null,null,null,null,null,null,null],\"lantern\":0},"
                + "\"grid\":{\"segments\":0,\"tiles\":[" + Tile(far[0], "station:tea") + "," + Tile(far[1], "station:jam") + "," + Tile(far[2], "decor:0") + ","
                + Tile(farLeft[0], "rest:0") + "],\"stored\":[\"station:leaf\"]}}";
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            GardenGrid g = r.State.Grid;
            Assert.AreEqual(0, g.Segments);
            AssertAllOnTheLawn(g);
            Assert.AreEqual(new Hex(0, 0), g.TileOf("station:tea"));
            Assert.AreEqual(GardenGrid.DefaultStationTile(2), g.TileOf("station:jam"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(0), g.TileOf("decor:0"));
            Assert.AreEqual(GardenGrid.DefaultRestTile(0), g.TileOf("rest:0"));
            Assert.IsTrue(g.IsStored("station:leaf"), "what was put away stays put away");
            Assert.IsNull(g.TileOf("station:leaf"));
        }

        [Test]
        public void The_Grid_Survives_A_Save_Round_Trip()
        {
            GameState s = GameState.NewGame(defs);
            s.Plots = 3; s.RestSpots = 4; GardenGrid.Settle(defs, s);
            s.GoldenCarrots = 100; Decor.Buy(s, "den-da"); Decor.Place(s, 6, "den-da");
            Assert.IsTrue(s.Grid.Place("station:jam", 1, 1), "the jam pot moved to another tile");
            s.Grid.Segments = 2;
            string json = SaveCodec.Encode(defs, s);
            StringAssert.Contains("\"grid\":{\"segments\":2,", json);
            GameState back = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(2, back.Grid.Segments);
            Assert.AreEqual(s.Grid.OccupantCount, back.Grid.OccupantCount);
            foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants) Assert.AreEqual(o.Value, back.Grid.TileOf(o.Key), o.Key);
            Assert.AreEqual(new Hex(1, 1), back.Grid.TileOf("station:jam"), "a moved station stays moved");
            Assert.AreEqual(GardenGrid.DefaultDecorTile(6), back.Grid.TileOf("decor:6"));
        }

        [Test]
        public void An_Old_Save_Without_A_Grid_Is_Given_One_With_Everything_It_Had()
        {
            string json = "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":30},{\"id\":\"leaf\",\"level\":5},{\"id\":\"jam\",\"level\":0}],"
                + "\"plots\":3,\"restSpots\":3,\"gardenXp\":500,"
                + "\"decor\":{\"owned\":{\"den-da\":1,\"bon-hoa\":1},\"spots\":[null,\"den-da\",null,null,null,null,\"bon-hoa\",null],\"lantern\":0}}";
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            GardenGrid g = r.State.Grid;
            Assert.AreEqual(GardenGrid.SegmentsForPlots(3), g.Segments);
            int stations = 0, rests = 0, decors = 0;
            foreach (KeyValuePair<string, Hex> o in g.Occupants)
            {
                if (o.Key.StartsWith("station:")) stations++;
                else if (o.Key.StartsWith("rest:")) rests++;
                else if (o.Key.StartsWith("decor:")) decors++;
                Assert.AreEqual(TileState.Occupied, g.Tile(o.Value.Q, o.Value.R).State);
            }
            Assert.AreEqual(3, stations, "every plot's station, open or not");
            Assert.AreEqual(3, rests);
            Assert.AreEqual(2, decors, "the two pieces standing, not the empty spots");
            Assert.AreEqual(GardenGrid.DefaultDecorTile(1), g.TileOf("decor:1"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(6), g.TileOf("decor:6"));
            Assert.AreEqual(3 + 3 + 2 + 3, g.OccupantCount, "plus the mortar, the shop and the counter");
            var tiles = new HashSet<Hex>();
            foreach (KeyValuePair<string, Hex> o in g.Occupants) Assert.IsTrue(tiles.Add(o.Value), "nothing shares a tile: " + o.Key);
        }

        [Test]
        public void A_Saved_Tile_That_Is_Taken_Twice_Or_Unknown_Is_Dropped_And_The_Thing_Still_Gets_A_Tile()
        {
            string json = "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":1},{\"id\":\"leaf\",\"level\":0},{\"id\":\"jam\",\"level\":0}],\"plots\":2,"
                + "\"grid\":{\"segments\":99,\"tiles\":[{\"q\":0,\"r\":0,\"id\":\"station:tea\"},{\"q\":0,\"r\":0,\"id\":\"station:leaf\"},{\"q\":1,\"r\":1,\"id\":\"ghost\"},{\"q\":2,\"r\":2,\"id\":\"station:jam\"}]}}";
            GameState s = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(new Hex(0, 0), s.Grid.TileOf("station:tea"));
            Assert.IsNotNull(s.Grid.TileOf("station:leaf"), "the second claim on the centre is dropped, and the leaf basket is seated elsewhere");
            Assert.AreNotEqual(new Hex(0, 0), s.Grid.TileOf("station:leaf"));
            Assert.IsNull(s.Grid.TileOf("ghost"), "nothing the game does not know");
            Assert.AreEqual(new Hex(2, 2), s.Grid.TileOf("station:jam"), "a saved tile is kept (three stations always have their three plots)");
            s.Plots = 2; GardenGrid.Settle(defs, s);
            Assert.IsNull(s.Grid.TileOf("station:jam"), "no plot, no tile");
            Assert.LessOrEqual(s.Grid.Segments, GardenGrid.MaxSegments);
        }

        [Test]
        public void Cell_Positions_Are_A_Pointy_Hex_Lattice_Stretched_Tall()
        {
            Assert.AreEqual(0d, GardenGrid.LocalX(0, 0));
            Assert.AreEqual(GardenGrid.PitchX, GardenGrid.LocalX(1, 0));
            Assert.AreEqual(GardenGrid.PitchX / 2d, GardenGrid.LocalX(0, 1), "the next row is shifted half a tile");
            Assert.AreEqual(-GardenGrid.PitchY, GardenGrid.LocalY(0, 1), "r grows downward");
            Assert.AreEqual(GardenGrid.PitchY * 2d, GardenGrid.LocalY(3, -2));
        }

        [Test]
        public void The_Tiles_Of_The_Jar_The_Basket_And_The_Mortar_s_Plate_Are_Reserved_And_No_Home_Tile_Is()
        {
            var expected = new[]
            {
                new Hex(0, -2), new Hex(1, -2), new Hex(2, -2),
                new Hex(2, -1), new Hex(3, -1), new Hex(4, -1),
                new Hex(-3, -1), new Hex(-2, -1), new Hex(-3, 0), new Hex(-2, 0), new Hex(-4, 0),
                new Hex(-2, -3), new Hex(-1, -3), new Hex(0, -3), new Hex(-3, -2), new Hex(-2, -2), new Hex(-1, -2), new Hex(-1, -1), new Hex(0, -1),
            };
            CollectionAssert.AreEquivalent(expected, GardenGrid.ReservedTiles);
            var g = new GardenGrid(GardenGrid.MaxSegments);
            foreach (Hex h in expected)
            {
                Assert.IsTrue(GardenGrid.IsReserved(h), h + " is kept clear");
                Assert.AreEqual(TileState.Reserved, g.Tile(h.Q, h.R).State, h + " reads as reserved on the lawn");
            }
            Assert.AreEqual(TileState.ForestBorder, new GardenGrid(0).Tile(-3, -2).State, "the pond's far bank reaches the first segment on the left, still in the mist");
            var big = new List<StationDef>();
            for (int i = 0; i < 24; i++) big.Add(new StationDef { Id = "s" + i });
            foreach (string id in new[] { GardenGrid.MortarId, GardenGrid.ShopId, GardenGrid.CounterId })
                Assert.IsFalse(GardenGrid.IsReserved(GardenGrid.DefaultTile(id, big)), id + " keeps its own tile");
            for (int i = 0; i < big.Count; i++) Assert.IsFalse(GardenGrid.IsReserved(GardenGrid.DefaultStationTile(i)), "station " + i);
            for (int n = 0; n < RestSpots.Max + 2; n++) Assert.IsFalse(GardenGrid.IsReserved(GardenGrid.DefaultRestTile(n)), "rest " + n);
            for (int n = 0; n < Decor.Spots; n++) Assert.IsFalse(GardenGrid.IsReserved(GardenGrid.DefaultDecorTile(n)), "decor " + n);
            GameState s = GameState.NewGame(big);
            s.Plots = big.Count;
            for (int n = 0; n < Decor.Spots; n++) s.DecorSpots[n] = Decor.Kinds[0].Id;
            GardenGrid.Settle(big, s);
            foreach (Hex h in expected) Assert.IsNull(s.Grid.OccupantAt(h.Q, h.R), h + " stays free");
            Assert.AreEqual(GardenGrid.DefaultTile(GardenGrid.MortarId, big), s.Grid.TileOf(GardenGrid.MortarId));
            Assert.AreEqual(GardenGrid.DefaultTile(GardenGrid.ShopId, big), s.Grid.TileOf(GardenGrid.ShopId));
            Assert.AreEqual(GardenGrid.DefaultTile(GardenGrid.CounterId, big), s.Grid.TileOf(GardenGrid.CounterId));
        }

        [Test]
        public void Nothing_Is_Placed_Moved_Swapped_Dropped_Or_Settled_On_A_Reserved_Tile()
        {
            GameState s = GameState.NewGame(defs);
            s.Levels[1] = 5; s.Levels[2] = 3;
            GardenGrid g = s.Grid;
            g.Segments = 2;
            Hex jar = new Hex(-3, -1), plate = new Hex(1, -2);
            Hex leaf = g.TileOf("station:leaf").Value;
            Assert.IsFalse(g.Place("decor:0", jar.Q, jar.R), "a decoration on the jar's plate");
            Assert.IsFalse(g.Place("station:jam", plate.Q, plate.R), "a station on the mortar's plate");
            Assert.IsFalse(g.Place("decor:0", -1, -1), "a decoration on the pond's \"Câu cá\" plate (#58)");
            Assert.IsFalse(g.Place(GardenGrid.RestId(2), -1, -1), "nor a place to rest");
            Assert.IsFalse(g.Place("decor:0", -2, -2), "nor on the pond itself");
            Assert.IsFalse(g.Move(leaf, jar), "moved onto the jar");
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, leaf, jar), "dropped in the arranging mode: back where it was");
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, leaf, new Hex(3, -1)), "nor on the leaf basket");
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, leaf, new Hex(-1, -1)), "nor on the pond's plate");
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, leaf, new Hex(-2, -2)), "nor in the pond");
            Assert.AreEqual(leaf, g.TileOf("station:leaf").Value);
            Assert.IsTrue(g.Place("decor:1", -3, 0, true));
            Assert.IsFalse(g.Swap(leaf, new Hex(-3, 0)));
            Assert.IsFalse(g.Swap(new Hex(-3, 0), leaf));
            g.Remove("decor:1");
            foreach (Hex h in g.EmptyTiles()) Assert.IsFalse(GardenGrid.IsReserved(h), h + " offered as empty");
            Assert.IsTrue(g.NearestEmpty(jar, out Hex near));
            Assert.IsFalse(GardenGrid.IsReserved(near));
            Assert.AreEqual(1, GardenGrid.Distance(near, jar), "a free neighbour of the jar's tile, not the tile");
            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            int k = 0;
            foreach (Hex h in GardenGrid.Strip(g.Segments))
                if (g.OccupantAt(h.Q, h.R) == null && !GardenGrid.IsReserved(h)) Assert.IsTrue(g.Place("decor:" + (100 + k++), h.Q, h.R));
            Assert.IsEmpty(g.EmptyTiles());
            Assert.IsFalse(g.NearestEmpty(out _));
            Assert.IsFalse(g.NearestEmptyApart(2, out _));
            Assert.IsFalse(Layout.TakeOut(defs, s, 2), "the jam pot stays in storage rather than stand on a reserved tile");
        }

        [Test]
        public void An_Old_Save_With_Things_On_Reserved_Tiles_Loads_With_Them_On_The_Nearest_Empty_Tile()
        {
            GameState s = GameState.NewGame(defs);
            s.Levels[1] = 5; s.Levels[2] = 3;
            s.Plots = 8;
            GardenGrid.Settle(defs, s);
            s.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(s, Decor.Kinds[0].Id) && Decor.Place(s, 0, Decor.Kinds[0].Id));
            Assert.IsTrue(s.Grid.Place(GardenGrid.DecorId(0), -3, 0, true));
            Assert.IsTrue(s.Grid.Place("station:jam", 3, -1, true));
            Assert.IsTrue(s.Grid.Place(GardenGrid.RestId(0), 1, -2, true));
            Assert.IsTrue(Decor.Buy(s, Decor.Kinds[1].Id) && Decor.Place(s, 1, Decor.Kinds[1].Id));
            Assert.IsTrue(s.Grid.Place(GardenGrid.RestId(1), -1, -1, true));
            Assert.IsTrue(s.Grid.Place(GardenGrid.DecorId(1), -2, -2, true));
            string json = SaveCodec.Encode(defs, s);
            StringAssert.Contains("\"version\":14", json, "the save stays version 14");
            GameState back = SaveCodec.Decode(defs, json).State;
            var was = new Dictionary<string, Hex>
            {
                { GardenGrid.DecorId(0), new Hex(-3, 0) }, { "station:jam", new Hex(3, -1) }, { GardenGrid.RestId(0), new Hex(1, -2) },
                { GardenGrid.RestId(1), new Hex(-1, -1) }, { GardenGrid.DecorId(1), new Hex(-2, -2) },
            };
            foreach (KeyValuePair<string, Hex> moved in was)
            {
                Hex? now = back.Grid.TileOf(moved.Key);
                Assert.IsNotNull(now, moved.Key + " still stands");
                Assert.IsFalse(GardenGrid.IsReserved(now.Value), moved.Key + " left " + moved.Value + " for " + now.Value);
                Assert.IsTrue(back.Grid.InLawn(now.Value), moved.Key + " on the lawn");
                foreach (Hex h in back.Grid.EmptyTiles())
                    Assert.GreaterOrEqual(GardenGrid.Distance(h, moved.Value), GardenGrid.Distance(now.Value, moved.Value), moved.Key + ": " + h + " was nearer");
            }
            Assert.AreEqual(Decor.Kinds[0].Id, back.DecorSpots[0], "the piece is kept");
            Assert.AreEqual(Decor.Kinds[1].Id, back.DecorSpots[1], "and the one from the pond");
            Assert.AreEqual(3, back.Levels[2]);
            foreach (KeyValuePair<string, Hex> o in back.Grid.Occupants)
                if (GardenGrid.IsReserved(o.Value)) Assert.Fail(o.Key + " still on reserved " + o.Value);
            Assert.AreEqual(SaveCodec.Encode(defs, back), SaveCodec.Encode(defs, SaveCodec.Decode(defs, SaveCodec.Encode(defs, back)).State), "settled once, it stays");
        }

        const string OldVerticalSave = "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":40},{\"id\":\"leaf\",\"level\":12},{\"id\":\"jam\",\"level\":9},{\"id\":\"flower\",\"level\":4},"
            + "{\"id\":\"oven\",\"level\":3},{\"id\":\"hive\",\"level\":2},{\"id\":\"milk\",\"level\":1},{\"id\":\"star\",\"level\":0},{\"id\":\"grape\",\"level\":0},{\"id\":\"bakery\",\"level\":0}],"
            + "\"plots\":8,\"restSpots\":4,\"gardenXp\":5000,"
            + "\"view\":{\"x\":540,\"y\":3100,\"zoom\":1.5},"
            + "\"decor\":{\"owned\":{\"den-da\":1,\"bon-hoa\":1},\"spots\":[null,\"den-da\",null,null,null,null,\"bon-hoa\",null],\"lantern\":0},"
            + "\"grid\":{\"radius\":5,\"tiles\":["
            + "{\"q\":0,\"r\":0,\"id\":\"station:tea\"},{\"q\":2,\"r\":0,\"id\":\"station:leaf\"},{\"q\":-1,\"r\":1,\"id\":\"station:jam\"},{\"q\":-1,\"r\":3,\"id\":\"station:flower\"},"
            + "{\"q\":-2,\"r\":0,\"id\":\"station:hive\"},{\"q\":3,\"r\":-2,\"id\":\"station:milk\"},{\"q\":1,\"r\":2,\"id\":\"station:star\"},"
            + "{\"q\":-1,\"r\":5,\"id\":\"rest:0\"},{\"q\":-3,\"r\":6,\"id\":\"rest:1\"},{\"q\":-2,\"r\":2,\"id\":\"rest:2\"},{\"q\":0,\"r\":5,\"id\":\"rest:3\"},"
            + "{\"q\":-5,\"r\":7,\"id\":\"decor:1\"},{\"q\":-2,\"r\":-1,\"id\":\"decor:6\"},"
            + "{\"q\":1,\"r\":-1,\"id\":\"mortar\"},{\"q\":2,\"r\":-4,\"id\":\"shop\"},{\"q\":3,\"r\":-3,\"id\":\"counter\"}],"
            + "\"stored\":[\"station:oven\"]}}";

        static List<StationDef> TenStations()
        {
            var ten = new List<StationDef>();
            string[] ids = { "tea", "leaf", "jam", "flower", "oven", "hive", "milk", "star", "grape", "bakery" };
            for (int i = 0; i < ids.Length; i++)
                ten.Add(new StationDef { Id = ids[i], Name = ids[i], Kind = StationKind.Brew, BaseCost = 10 * System.Math.Pow(10, i), CostGrowth = 1.1, BaseIncome = 1, CycleSeconds = 1 });
            return ten;
        }

        [Test]
        public void An_Old_Vertical_Garden_Save_Loads_Onto_The_Strip_With_Nothing_Lost()
        {
            List<StationDef> ten = TenStations();
            LoadResult r = SaveCodec.Decode(ten, OldVerticalSave);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            GameState s = r.State;
            GardenGrid g = s.Grid;
            Assert.AreEqual(8, s.Plots, "no plot is taken back");
            Assert.AreEqual(2, g.Segments, "R 5 had two rings past the first three: two segments open");
            Assert.AreEqual(2, Expansion.Segments(s));
            Assert.AreEqual(3, Expansion.Next(ten, s).SegmentsAfter, "the next is the one R 6 was");
            Assert.IsTrue(g.IsStored("station:oven"));
            Assert.IsNull(g.TileOf("station:oven"));
            Assert.AreEqual(3, s.Levels[4], "and keeps its level");
            for (int i = 0; i < 8; i++)
            {
                if (i == 4) continue;
                Assert.AreEqual(GardenGrid.DefaultStationTile(i), g.TileOf(GardenGrid.StationId(ten[i])), ten[i].Id + " on its default tile, not its old column tile");
            }
            Assert.AreEqual(9, s.Levels[2]);
            for (int k = 0; k < 4; k++) Assert.AreEqual(GardenGrid.DefaultRestTile(k), g.TileOf(GardenGrid.RestId(k)), "rest " + k);
            Assert.IsNull(g.TileOf(GardenGrid.RestId(4)));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(1), g.TileOf(GardenGrid.DecorId(1)));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(6), g.TileOf(GardenGrid.DecorId(6)));
            Assert.AreEqual("den-da", s.DecorSpots[1]);
            Assert.AreEqual("bon-hoa", s.DecorSpots[6]);
            foreach (string id in new[] { GardenGrid.MortarId, GardenGrid.ShopId, GardenGrid.CounterId }) Assert.AreEqual(GardenGrid.DefaultTile(id, ten), g.TileOf(id), id);
            Assert.AreEqual(7 + 4 + 2 + 3, g.OccupantCount, "seven stations out, four seats, two decorations, three props");
            var tiles = new HashSet<Hex>();
            foreach (KeyValuePair<string, Hex> o in g.Occupants)
            {
                Assert.IsTrue(tiles.Add(o.Value), o.Key + " has a tile of its own");
                Assert.IsFalse(GardenGrid.IsReserved(o.Value), o.Key + " on a reserved tile");
                Assert.AreNotEqual(-1, GardenGrid.SegmentOf(o.Value), o.Key + " on the strip");
            }
            Assert.AreEqual(0d, s.ViewZoom);
            Assert.AreEqual(0d, s.ViewX);
            Assert.AreEqual(0d, s.ViewY);
            string again = SaveCodec.Encode(ten, s);
            StringAssert.Contains("\"version\":14", again, "the save stays version 14");
            StringAssert.Contains("\"grid\":{\"segments\":2,", again);
            StringAssert.DoesNotContain("\"radius\"", again);
            Assert.AreEqual(again, SaveCodec.Encode(ten, SaveCodec.Decode(ten, again).State), "migrated once, it stays");
        }

        [Test]
        public void An_Old_Save_Whose_Default_Tile_Is_Taken_Goes_To_The_Nearest_Free_One_And_Its_Land_Follows_The_Plots()
        {
            List<StationDef> ten = TenStations();
            string json = OldVerticalSave.Replace("\"radius\":5", "\"radius\":3").Replace("\"plots\":8", "\"plots\":10");
            GameState s = SaveCodec.Decode(ten, json).State;
            Assert.AreEqual(10, s.Plots);
            Assert.AreEqual(3, s.Grid.Segments, "ten plots are three land waves");
            Assert.AreEqual(GardenGrid.MaxSegments, SaveCodec.Decode(ten, OldVerticalSave.Replace("\"radius\":5", "\"radius\":40")).State.Grid.Segments);
            Assert.AreEqual(2, SaveCodec.Decode(ten, OldVerticalSave.Replace("\"radius\":5", "\"radius\":1")).State.Grid.Segments, "never fewer than the plots call for");
            GameState strip = SaveCodec.Decode(ten, OldVerticalSave).State;
            Hex jamHome = GardenGrid.DefaultStationTile(2);
            Assert.IsTrue(strip.Grid.Place("station:leaf", 0, 1));
            Assert.IsTrue(strip.Grid.Remove("station:jam"));
            Assert.IsTrue(strip.Grid.Place(GardenGrid.RestId(0), jamHome.Q, jamHome.R));
            GardenGrid.Settle(ten, strip);
            Hex jam = strip.Grid.TileOf("station:jam").Value;
            Assert.AreNotEqual(jamHome, jam);
            Assert.AreEqual(1, GardenGrid.Distance(jam, jamHome), "the nearest free tile");
        }
    }
}

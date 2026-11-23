using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class ForestRingTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = new List<StationDef>();
            for (int i = 0; i < 12; i++)
                defs.Add(new StationDef { Id = "s" + i, Name = "S" + i, Kind = StationKind.Brew, BaseCost = 10 * System.Math.Pow(10, i), CostGrowth = 1.1, BaseIncome = 1, CycleSeconds = 1 });
            s = GameState.NewGame(defs);
        }

        void Grown()
        {
            s.Coins = BigNum.FromParts(1, 40);
            s.GardenXp = Garden.XpForLevel(30);
            for (int i = 0; i < 12; i++) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" });
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < 8) s.SeenKinds.Add(kind.Id);
        }

        [Test]
        public void A_New_Garden_Has_The_Middle_Open_And_Each_Segment_Opens_Its_Band_On_Its_Side()
        {
            Assert.AreEqual(0, Expansion.Segments(s));
            Assert.AreEqual(GardenGrid.StartTiles, s.Grid.LawnTiles);
            Grown();
            for (int n = 1; n <= 4; n++)
            {
                int before = s.Grid.LawnTiles;
                Assert.IsTrue(Expansion.Expand(defs, s));
                Assert.AreEqual(n, s.Grid.Segments);
                Assert.AreEqual(n <= 2 ? 21 : 42, s.Grid.LawnTiles - before, "a segment opens three tiles a row on its side, then six (#91)");
                Assert.AreEqual(s.Grid.LawnTiles - before, Expansion.Segment(n).Tiles);
                Assert.AreEqual(n % 2 == 1 ? GardenSide.Right : GardenSide.Left, Expansion.Segment(n).Side, "right, then left, in turn");
                foreach (Hex h in GardenGrid.SegmentTiles(n)) Assert.AreNotEqual(TileState.ForestBorder, s.Grid.Tile(h.Q, h.R).State, "the new segment is lawn");
                foreach (Hex h in GardenGrid.SegmentTiles(n + 1)) Assert.AreNotEqual(TileState.UnlockedEmpty, s.Grid.Tile(h.Q, h.R).State, "the mist stands one segment further out");
            }
        }

        [TestCase(1, 4, 3, 0, 0, 4)]
        [TestCase(2, 5, 6, 6, 0, 6)]
        [TestCase(3, 6, 10, 10, 0, 8)]
        [TestCase(4, 7, 16, 0, 8, 10)]
        public void Each_Segment_Asks_What_The_Ring_It_Replaces_Asked(int segment, int ring, int level, int rabbits, int kinds, int firstStation)
        {
            MistSegment next = Expansion.Segment(segment);
            Assert.AreEqual(segment, next.SegmentsAfter);
            Assert.AreEqual(level, next.NeedLevel, "R " + ring + "'s level");
            Assert.AreEqual(rabbits, next.NeedRabbits);
            Assert.AreEqual(kinds, next.NeedKinds);
            s.Plots = firstStation;
            GardenGrid.Settle(defs, s);
            Assert.AreEqual(segment, Expansion.Next(defs, s).SegmentsAfter);
            double price = (defs[firstStation].BaseCost + defs[firstStation + 1].BaseCost) * Plots.CostShare;
            Assert.AreEqual(price, Expansion.CostOfNext(defs, s).ToDouble(), price * 1e-9, "a quarter of the next two stations' price, as R " + ring);
            Assert.AreEqual(Expansion.CostOfNext(defs, s).ToDouble(), Plots.CostOfNext(defs, s).ToDouble(), 1e-9);
        }

        [Test]
        public void Past_The_Table_Every_Segment_Asks_Five_Levels_More()
        {
            Assert.AreEqual(16, Expansion.Segment(5).NeedLevel);
            Assert.AreEqual(20, Expansion.Segment(6).NeedLevel);
            Assert.AreEqual(25, Expansion.Segment(7).NeedLevel);
            Assert.AreEqual(30, Expansion.Segment(8).NeedLevel);
            Assert.AreEqual(8, Expansion.Segment(7).NeedKinds);
            Assert.IsNull(Expansion.Segment(0), "the middle is open from the start");
            Assert.IsNull(Expansion.Segment(GardenGrid.MaxSegments + 1));
            Assert.AreEqual(4, Expansion.LastSegments(defs), "twelve stations: four segments");
        }

        [Test]
        public void Can_Expand_Says_No_Until_Every_Condition_Is_Met()
        {
            s.Plots = 6; GardenGrid.Settle(defs, s);
            s.Coins = Expansion.CostOfNext(defs, s);
            s.GardenXp = Garden.XpForLevel(6);
            for (int i = 0; i < 5; i++) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" });
            Assert.IsFalse(Expansion.CanExpand(defs, s), "five rabbits of six");
            s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" });
            Assert.IsTrue(Expansion.CanExpand(defs, s));
            s.Coins -= BigNum.One;
            Assert.IsFalse(Expansion.CanExpand(defs, s), "a carrot short");
            Assert.IsFalse(Expansion.Expand(defs, s));
            Assert.AreEqual(1, s.Grid.Segments, "nothing changed");
        }

        [Test]
        public void The_Segments_Are_Saved_And_Come_Back()
        {
            Grown();
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.IsTrue(Expansion.Expand(defs, s));
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, back.Grid.Segments);
            Assert.AreEqual(2, Expansion.Segments(back));
            Assert.AreEqual(8, back.Plots);
            Assert.AreEqual(3, Expansion.Next(defs, back).SegmentsAfter);
        }

        [TestCase(4, 0)]
        [TestCase(5, 1)]
        [TestCase(6, 1)]
        [TestCase(7, 2)]
        [TestCase(8, 2)]
        [TestCase(10, 3)]
        [TestCase(12, 4)]
        public void An_Old_Save_Without_A_Grid_Comes_To_The_Segments_Its_Land_Matches(int plots, int segments)
        {
            var stations = new List<string>();
            for (int i = 0; i < 12; i++) stations.Add("{\"id\":\"s" + i + "\",\"level\":" + (i < plots ? 1 : 0) + "}");
            string json = "{\"version\":14,\"stations\":[" + string.Join(",", stations) + "],\"plots\":" + plots + "}";
            GameState old = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(Expansion.PlotsForSegments(defs, segments), old.Plots, "no plot is taken back, and a half-bought wave is whole (#86)");
            Assert.AreEqual(segments, old.Grid.Segments);
            Assert.AreEqual(segments, Expansion.Segments(old));
            MistSegment next = Expansion.Next(defs, old);
            if (segments < 4) Assert.AreEqual(segments + 1, next.SegmentsAfter); else Assert.IsNull(next);
        }

        [TestCase(5)]
        [TestCase(7)]
        [TestCase(9)]
        [TestCase(11)]
        public void An_Old_Save_With_An_Odd_Plot_Count_Opens_The_Next_Station_On_Its_Own_Conditions(int plots)
        {
            int need = Expansion.LevelForPlots(defs, plots) + 1;
            defs[plots].NeedGardenLevel = need;
            var stations = new List<string>();
            for (int i = 0; i < 12; i++) stations.Add("{\"id\":\"s" + i + "\",\"level\":" + (i < plots ? 1 : 0) + "}");
            string json = "{\"version\":14,\"stations\":[" + string.Join(",", stations) + "],\"plots\":" + plots + "}";
            GameState old = SaveCodec.Decode(defs, json).State;
            int segments = Expansion.WavesTaken(plots);
            Assert.AreEqual(segments, Expansion.Segments(old), "the half-bought wave still counts as its segment");
            Assert.AreEqual(plots + 1, old.Plots, "the wave's other plot comes with it, and no more");
            Assert.IsNotNull(old.Grid.TileOf("station:s" + plots), "the next station's plot has its tile");
            Assert.IsTrue(old.Grid.InLawn(old.Grid.TileOf("station:s" + plots).Value), "on the open lawn");

            Assert.AreEqual(need - 1, Garden.Level(old));
            Assert.IsFalse(Economy.CanUnlock(defs, old, plots), "the station still asks its own garden level");
            old.GardenXp = Garden.XpForLevel(need);
            Assert.IsTrue(Economy.CanUnlock(defs, old, plots), "with its own conditions met it opens, with no new segment");
            Assert.AreEqual(segments, Expansion.Segments(old));
            MistSegment next = Expansion.Next(defs, old);
            if (segments < Expansion.LastSegments(defs)) Assert.AreEqual(segments + 1, next.SegmentsAfter, "the next segment is for the station after it");
        }

        [TestCase(3, 4, 0)]
        [TestCase(4, 6, 1)]
        [TestCase(5, 8, 2)]
        [TestCase(6, 10, 3)]
        [TestCase(7, 12, 4)]
        public void An_Old_Save_With_The_Vertical_Garden_s_Rings_Opens_As_Many_Segments(int radius, int plots, int segments)
        {
            var stations = new List<string>();
            for (int i = 0; i < 12; i++) stations.Add("{\"id\":\"s" + i + "\",\"level\":" + (i < plots ? 1 : 0) + "}");
            string json = "{\"version\":14,\"stations\":[" + string.Join(",", stations) + "],\"plots\":" + plots
                + ",\"grid\":{\"radius\":" + radius + ",\"tiles\":[{\"q\":2,\"r\":-6,\"id\":\"station:s3\"}]}}";
            GameState old = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(segments, old.Grid.Segments);
            Assert.AreEqual(GardenGrid.DefaultStationTile(3), old.Grid.TileOf("station:s3"), "an old column tile means nothing on the map");
            for (int i = 0; i < plots; i++) Assert.IsTrue(old.Grid.InLawn(old.Grid.TileOf("station:s" + i).Value), "station " + i + " on the open lawn");
        }

        [Test]
        public void New_Stations_Stand_On_Their_Own_Tiles_On_The_New_Lawn()
        {
            Grown();
            for (int n = 0; n < 4; n++)
            {
                Assert.IsTrue(Expansion.Expand(defs, s));
                for (int i = 0; i < s.Plots; i++)
                {
                    Hex? tile = s.Grid.TileOf(GardenGrid.StationId(defs[i]));
                    Assert.IsNotNull(tile, "station " + i + " has a tile");
                    Assert.AreEqual(GardenGrid.DefaultStationTile(i), tile.Value, "station " + i + " on its own tile");
                    Assert.IsTrue(s.Grid.InLawn(tile.Value), "station " + i + " stands on the lawn");
                }
            }
        }

        [Test]
        public void A_New_Station_Whose_Tile_Is_Taken_Goes_To_The_Empty_Tile_Nearest_The_Teapot_Clear_Of_The_Others()
        {
            Grown();
            Assert.IsTrue(Expansion.Expand(defs, s));
            Hex home = GardenGrid.DefaultStationTile(6);
            Assert.IsTrue(s.Grid.Place("rest:0", home.Q, home.R));
            Assert.IsTrue(Expansion.Expand(defs, s));
            Hex? at = s.Grid.TileOf("station:s6");
            Assert.IsNotNull(at);
            Assert.AreNotEqual(home, at.Value);
            Assert.AreEqual(home, s.Grid.TileOf("rest:0").Value, "what the player put there stays");
            foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants)
                if (o.Key.StartsWith("station:") && o.Key != "station:s6")
                    Assert.GreaterOrEqual(GardenGrid.Distance(o.Value, at.Value), Expansion.StationSpacing, "clear of " + o.Key);
            foreach (Hex h in s.Grid.EmptyTiles())
            {
                if (GardenGrid.Distance(h) >= GardenGrid.Distance(at.Value)) break;
                bool apart = true;
                foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants)
                    if (o.Key.StartsWith("station:") && o.Key != "station:s6" && GardenGrid.Distance(o.Value, h) < Expansion.StationSpacing) apart = false;
                Assert.IsFalse(apart, h + " is nearer the teapot and clear: it should have been taken");
            }
        }

        [Test]
        public void Empty_Tiles_Go_Nearest_The_Teapot_First_And_Skip_What_Stands()
        {
            var g = new GardenGrid();
            Assert.IsTrue(g.Place("station:a", 0, 0));
            List<Hex> empty = g.EmptyTiles();
            Assert.AreEqual(GardenGrid.StartTiles - 1 - CountReserved(0), empty.Count);
            CollectionAssert.DoesNotContain(empty, new Hex(0, 0));
            for (int i = 1; i < empty.Count; i++) Assert.LessOrEqual(GardenGrid.Distance(empty[i - 1]), GardenGrid.Distance(empty[i]));
            Assert.IsTrue(g.NearestEmptyApart(2, out Hex apart));
            Assert.AreEqual(2, GardenGrid.Distance(apart), "the first empty tile two steps out");
        }

        static int CountReserved(int segments)
        {
            int n = 0;
            foreach (Hex h in GardenGrid.Strip(segments)) if (GardenGrid.IsReserved(h)) n++;
            return n;
        }
    }
}

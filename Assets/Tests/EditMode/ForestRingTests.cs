using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The garden widens ring by ring (#53): conditions and prices of the rings, the lawn's tiles, saves, and where new stations stand.</summary>
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
        public void A_New_Garden_Is_Three_Rings_Wide_And_Each_Ring_Opens_Six_R_Plus_Six_Tiles()
        {
            Assert.AreEqual(3, Expansion.Radius(s));
            Assert.AreEqual(0, Expansion.RingsTaken(s));
            Assert.AreEqual(37, s.Grid.LawnTiles);
            Grown();
            for (int r = 3; r < 7; r++)
            {
                int before = s.Grid.LawnTiles;
                Assert.IsTrue(Expansion.Expand(defs, s));
                Assert.AreEqual(r + 1, s.Grid.Radius);
                Assert.AreEqual(1 + 3 * (r + 1) * (r + 2), s.Grid.LawnTiles, "1 + 3R(R + 1) tiles");
                Assert.AreEqual(6 * r + 6, s.Grid.LawnTiles - before, "R to R + 1 opens 6R + 6 tiles");
                Assert.AreEqual(6 * r + 6, Expansion.Ring(r + 1).Tiles);
                foreach (Hex h in GardenGrid.Ring(r + 1)) Assert.AreNotEqual(TileState.ForestBorder, s.Grid.Tile(h.Q, h.R).State, "the new ring is lawn");
                // One ring further out is forest, but for the seats and props that came with the garden's frame (#52).
                foreach (Hex h in GardenGrid.Ring(r + 2)) Assert.AreNotEqual(TileState.UnlockedEmpty, s.Grid.Tile(h.Q, h.R).State, "the forest stands one ring further out");
            }
        }

        // R after, level, rabbits, kinds, the stations its land is for.
        [TestCase(4, 3, 0, 0, 4)]
        [TestCase(5, 6, 6, 0, 6)]
        [TestCase(6, 10, 10, 0, 8)]
        [TestCase(7, 15, 0, 8, 10)]
        public void The_First_Four_Rings_Ask_What_The_Land_Waves_Asked(int radius, int level, int rabbits, int kinds, int firstStation)
        {
            ForestRing ring = Expansion.Ring(radius);
            Assert.AreEqual(radius, ring.RadiusAfter);
            Assert.AreEqual(level, ring.NeedLevel);
            Assert.AreEqual(rabbits, ring.NeedRabbits);
            Assert.AreEqual(kinds, ring.NeedKinds);
            s.Plots = firstStation;
            GardenGrid.Settle(defs, s);
            Assert.AreEqual(radius, Expansion.Next(defs, s).RadiusAfter);
            double price = (defs[firstStation].BaseCost + defs[firstStation + 1].BaseCost) * Plots.CostShare;
            Assert.AreEqual(price, Expansion.CostOfNext(defs, s).ToDouble(), price * 1e-9, "a quarter of the next two stations' price");
            Assert.AreEqual(Expansion.CostOfNext(defs, s).ToDouble(), Plots.CostOfNext(defs, s).ToDouble(), 1e-9);
        }

        [Test]
        public void Past_The_Table_Every_Ring_Asks_Five_Levels_More()
        {
            // R 8 and R 9 are in the table (#67b): they ask for the levels of the stations they make room for, 16 and 20.
            Assert.AreEqual(16, Expansion.Ring(8).NeedLevel);
            Assert.AreEqual(20, Expansion.Ring(9).NeedLevel);
            Assert.AreEqual(25, Expansion.Ring(10).NeedLevel);
            Assert.AreEqual(30, Expansion.Ring(11).NeedLevel);
            Assert.AreEqual(8, Expansion.Ring(10).NeedKinds);
            Assert.IsNull(Expansion.Ring(3), "the lawn starts three rings wide");
            Assert.IsNull(Expansion.Ring(GardenGrid.MaxRadius + 1));
            Assert.AreEqual(7, Expansion.LastRadius(defs), "twelve stations: the forest steps back to seven rings");
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
            Assert.AreEqual(4, s.Grid.Radius, "nothing changed");
        }

        [Test]
        public void The_Ring_Is_Saved_And_Comes_Back()
        {
            Grown();
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.IsTrue(Expansion.Expand(defs, s));
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(5, back.Grid.Radius);
            Assert.AreEqual(5, Expansion.Radius(back));
            Assert.AreEqual(8, back.Plots);
            Assert.AreEqual(6, Expansion.Next(defs, back).RadiusAfter);
        }

        // Plots an old save held, and the rings that land comes to.
        [TestCase(4, 3)]
        [TestCase(5, 4)]
        [TestCase(6, 4)]
        [TestCase(7, 5)]
        [TestCase(8, 5)]
        [TestCase(10, 6)]
        [TestCase(12, 7)]
        public void An_Old_Save_Without_A_Grid_Comes_To_The_Ring_Its_Land_Matches(int plots, int radius)
        {
            var stations = new List<string>();
            for (int i = 0; i < 12; i++) stations.Add("{\"id\":\"s" + i + "\",\"level\":" + (i < plots ? 1 : 0) + "}");
            string json = "{\"version\":14,\"stations\":[" + string.Join(",", stations) + "],\"plots\":" + plots + "}";
            GameState old = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(plots, old.Plots, "no plot is taken back");
            Assert.AreEqual(radius, old.Grid.Radius);
            Assert.AreEqual(radius, Expansion.Radius(old));
            ForestRing next = Expansion.Next(defs, old);
            if (radius < 7) Assert.AreEqual(radius + 1, next.RadiusAfter); else Assert.IsNull(next);
        }

        [Test]
        public void New_Stations_Stand_On_Their_Own_Tiles_On_The_New_Lawn()
        {
            Grown();
            for (int ring = 0; ring < 4; ring++)
            {
                Assert.IsTrue(Expansion.Expand(defs, s));
                for (int i = 0; i < s.Plots; i++)
                {
                    Hex? tile = s.Grid.TileOf(GardenGrid.StationId(defs[i]));
                    Assert.IsNotNull(tile, "station " + i + " has a tile");
                    Assert.IsTrue(s.Grid.InLawn(tile.Value), "station " + i + " stands on the lawn");
                }
            }
        }

        [Test]
        public void A_New_Station_Whose_Tile_Is_Taken_Goes_To_The_Empty_Tile_Nearest_The_Centre_Clear_Of_The_Others()
        {
            Grown();
            // Something the player moved (#54) stands where the fifth station would.
            Hex home = GardenGrid.DefaultStationTile(4);
            Assert.IsTrue(s.Grid.Remove("rest:0"));
            Assert.IsTrue(s.Grid.Place("rest:0", home.Q, home.R));
            Assert.IsTrue(Expansion.Expand(defs, s));
            Hex? at = s.Grid.TileOf("station:s4");
            Assert.IsNotNull(at);
            Assert.AreNotEqual(home, at.Value);
            Assert.AreEqual(home, s.Grid.TileOf("rest:0").Value, "what the player put there stays");
            foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants)
                if (o.Key.StartsWith("station:") && o.Key != "station:s4")
                    Assert.GreaterOrEqual(GardenGrid.Distance(o.Value, at.Value), Expansion.StationSpacing, "clear of " + o.Key);
            // No nearer empty tile would have kept clear of the stations.
            foreach (Hex h in s.Grid.EmptyTiles())
            {
                if (GardenGrid.Distance(h) >= GardenGrid.Distance(at.Value)) break;
                bool apart = true;
                foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants)
                    if (o.Key.StartsWith("station:") && o.Key != "station:s4" && GardenGrid.Distance(o.Value, h) < Expansion.StationSpacing) apart = false;
                Assert.IsFalse(apart, h + " is nearer the centre and clear: it should have been taken");
            }
        }

        [Test]
        public void Empty_Tiles_Go_Nearest_The_Centre_First_And_Skip_What_Stands()
        {
            var g = new GardenGrid(1);
            Assert.IsTrue(g.Place("station:a", 0, 0));
            List<Hex> empty = g.EmptyTiles();
            Assert.AreEqual(6, empty.Count);
            CollectionAssert.DoesNotContain(empty, new Hex(0, 0));
            Assert.IsTrue(g.NearestEmptyApart(2, out Hex none) && GardenGrid.Distance(none) == 1, "no tile is two apart on a lawn this small: the nearest empty one");
            g.Radius = 2;
            Assert.IsTrue(g.NearestEmptyApart(2, out Hex apart));
            Assert.AreEqual(2, GardenGrid.Distance(apart));
            Assert.AreEqual(GardenGrid.Ring(2)[0], apart, "the first tile two rings out, in spiral order");
        }
    }
}

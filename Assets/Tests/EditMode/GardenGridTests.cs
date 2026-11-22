using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The hex grid under the garden (#52): tiles, neighbours, what stands where, and how it is saved.</summary>
    public class GardenGridTests
    {
        List<StationDef> defs;

        [SetUp]
        public void SetUp() { defs = Fixture.Defs(); }

        [Test]
        public void A_Lawn_Of_Radius_Two_Has_Nineteen_Tiles_And_Radius_Three_Thirty_Seven()
        {
            Assert.AreEqual(1, GardenGrid.TileCount(0));
            Assert.AreEqual(7, GardenGrid.TileCount(1));
            Assert.AreEqual(19, GardenGrid.TileCount(2));
            Assert.AreEqual(37, GardenGrid.TileCount(3));
            var two = new GardenGrid(2);
            var three = new GardenGrid(3);
            Assert.AreEqual(19, two.LawnTiles);
            Assert.AreEqual(37, three.LawnTiles);
            int n = 0;
            var seen = new HashSet<Hex>();
            foreach (TileData t in two.Tiles()) { n++; Assert.IsTrue(seen.Add(new Hex(t.Q, t.R)), "every tile once"); Assert.AreEqual(TileState.UnlockedEmpty, t.State); Assert.IsNull(t.OccupantId); }
            Assert.AreEqual(19, n);
            n = 0;
            foreach (TileData t in three.Tiles()) n++;
            Assert.AreEqual(37, n);
            Assert.AreEqual(new Hex(0, 0), two.Tiles()[0].Hex, "the spiral starts at the centre");
        }

        [Test]
        public void The_Centre_Has_Six_Neighbours_And_An_Edge_Tile_Keeps_Only_Those_On_The_Lawn()
        {
            var g = new GardenGrid(2);
            Hex[] around = GardenGrid.Neighbours(new Hex(0, 0));
            Assert.AreEqual(6, around.Length);
            foreach (Hex h in around) Assert.AreEqual(1, GardenGrid.Distance(h), h + " touches the centre");
            Assert.AreEqual(6, g.LawnNeighbours(new Hex(0, 0)).Count);
            // A corner of the lawn touches three tiles on the lawn and three in the forest.
            Assert.AreEqual(3, g.LawnNeighbours(new Hex(2, 0)).Count);
            Assert.AreEqual(4, g.LawnNeighbours(new Hex(2, -1)).Count, "an edge tile that is not a corner");
            Assert.AreEqual(TileState.ForestBorder, g.Tile(3, 0).State);
            Assert.IsFalse(g.InLawn(3, -1));
            Assert.IsTrue(g.InLawn(2, -2));
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
            Assert.IsFalse(g.Place("rest:1", 3, 0), "nothing is placed in the forest");
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
        public void The_Nearest_Empty_Tile_To_The_Centre_Is_Found_In_Spiral_Order()
        {
            var g = new GardenGrid(1);
            Assert.IsTrue(g.NearestEmpty(out Hex first));
            Assert.AreEqual(new Hex(0, 0), first);
            g.Place("a", 0, 0);
            Assert.IsTrue(g.NearestEmpty(out Hex second));
            Assert.AreEqual(1, GardenGrid.Distance(second));
            foreach (Hex h in GardenGrid.Ring(1)) g.Place("n" + h.Q + "," + h.R, h.Q, h.R);
            Assert.IsFalse(g.NearestEmpty(out _), "a full lawn has no empty tile");
            g.Radius = 2;
            Assert.IsTrue(g.NearestEmpty(out Hex third));
            Assert.AreEqual(2, GardenGrid.Distance(third), "a wider lawn has room again");
            Assert.IsTrue(g.NearestEmpty(new Hex(2, 0), out Hex near));
            Assert.AreEqual(new Hex(2, 0), near, "an empty tile asked for is its own nearest");
        }

        [Test]
        public void The_Lawn_Starts_At_Three_Rings_And_Each_Land_Wave_Adds_One()
        {
            Assert.AreEqual(3, GardenGrid.StartingRadius);
            Assert.AreEqual(3, GardenGrid.RadiusForPlots(0));
            Assert.AreEqual(3, GardenGrid.RadiusForPlots(4));
            Assert.AreEqual(4, GardenGrid.RadiusForPlots(6));
            Assert.AreEqual(5, GardenGrid.RadiusForPlots(8));
            Assert.AreEqual(6, GardenGrid.RadiusForPlots(10));
            Assert.AreEqual(7, GardenGrid.RadiusForPlots(12));
            // Everything the full garden holds has a tile of its own on the default table, inside the final lawn.
            var taken = new HashSet<Hex>();
            for (int i = 0; i < 12; i++) { Hex h = GardenGrid.DefaultStationTile(i); Assert.IsTrue(taken.Add(h), "station " + i); Assert.LessOrEqual(GardenGrid.Distance(h), GardenGrid.RadiusForPlots(i + 1), "station " + i + " stands on the lawn its wave opens"); }
            for (int n = 0; n < RestSpots.Max; n++) { Hex h = GardenGrid.DefaultRestTile(n); Assert.IsTrue(taken.Add(h), "rest " + n); Assert.LessOrEqual(GardenGrid.Distance(h), GardenGrid.RadiusForPlots(12)); }
            for (int n = 0; n < Decor.Spots; n++) { Hex h = GardenGrid.DefaultDecorTile(n); Assert.IsTrue(taken.Add(h), "decor " + n); Assert.LessOrEqual(GardenGrid.Distance(h), GardenGrid.RadiusForPlots(12)); }
            foreach (string id in new[] { GardenGrid.MortarId, GardenGrid.ShopId, GardenGrid.CounterId }) Assert.IsTrue(taken.Add(GardenGrid.DefaultTile(id, defs)), id);
            Assert.LessOrEqual(GardenGrid.TileCount(0), 1);
            Assert.GreaterOrEqual(GardenGrid.TileCount(GardenGrid.RadiusForPlots(12)), taken.Count, "the final lawn has a tile for everything");
            // The first four stations stand on the starting lawn, and later stations carry on downward without meeting.
            for (int i = 0; i < 4; i++) Assert.LessOrEqual(GardenGrid.Distance(GardenGrid.DefaultStationTile(i)), GardenGrid.StartingRadius);
            Assert.AreNotEqual(GardenGrid.DefaultStationTile(12), GardenGrid.DefaultStationTile(13));
            Assert.Greater(GardenGrid.DefaultStationTile(12).R, GardenGrid.DefaultStationTile(11).R);
        }

        [Test]
        public void A_New_Game_Stands_Its_Starting_Stations_Seats_And_Props_On_The_Grid()
        {
            GameState s = GameState.NewGame(defs);
            GardenGrid g = s.Grid;
            Assert.AreEqual(GardenGrid.StartingRadius, g.Radius);
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
            GameState s = GameState.NewGame(defs);
            // Land: the only wave three stations allow opens the third plot's station and a third place to rest.
            s.Plots = 2; GardenGrid.Settle(defs, s);
            Assert.IsNull(s.Grid.TileOf("station:jam"));
            s.Coins = new BigNum(1e9); s.GardenXp = Garden.XpForLevel(50);
            for (int i = 0; i < 3; i++) s.Rabbits.Add(new Rabbit { Id = i + 1, Tier = 0 });
            Assert.IsTrue(Expansion.TryExpand(defs, s));
            Assert.AreEqual(3, s.Plots);
            Assert.AreEqual(GardenGrid.DefaultStationTile(2), s.Grid.TileOf("station:jam"), "the new plot's station has its tile");
            Assert.AreEqual(GardenGrid.DefaultRestTile(2), s.Grid.TileOf("rest:2"), "and so has the new place to rest");
            Assert.AreEqual(GardenGrid.RadiusForPlots(3), s.Grid.Radius);
            // Decorations: placed, the spot's tile is taken; stored, it is free again.
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
            // The new moon: the garden is back to its first plots and seats; what the player placed stays.
            s.RestSpots = 5; s.Grid.Radius = 6; GardenGrid.Settle(defs, s);
            Assert.IsNotNull(s.Grid.TileOf("rest:4"));
            s.Season = 0; s.GardenXp = Garden.XpForLevel(NewMoon.LevelNeeded(0));
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));
            Assert.AreEqual(GardenGrid.StartingRadius, s.Grid.Radius);
            Assert.AreEqual(GardenGrid.DefaultStationTile(2), s.Grid.TileOf("station:jam"), "three stations are within the starting plots");
            Assert.IsNull(s.Grid.TileOf("rest:2"));
            Assert.IsNull(s.Grid.TileOf("rest:4"));
            Assert.AreEqual(GardenGrid.DefaultDecorTile(5), s.Grid.TileOf("decor:5"));
        }

        [Test]
        public void The_Grid_Survives_A_Save_Round_Trip()
        {
            GameState s = GameState.NewGame(defs);
            s.Plots = 3; s.RestSpots = 4; GardenGrid.Settle(defs, s);
            s.GoldenCarrots = 100; Decor.Buy(s, "den-da"); Decor.Place(s, 6, "den-da");
            Assert.IsTrue(s.Grid.Place("station:jam", 1, 1), "the jam pot moved to another tile");
            s.Grid.Radius = 5;
            string json = SaveCodec.Encode(defs, s);
            StringAssert.Contains("\"grid\":{\"radius\":5,", json);
            GameState back = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(5, back.Grid.Radius);
            Assert.AreEqual(s.Grid.OccupantCount, back.Grid.OccupantCount);
            foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants) Assert.AreEqual(o.Value, back.Grid.TileOf(o.Key), o.Key);
            Assert.AreEqual(new Hex(1, 1), back.Grid.TileOf("station:jam"), "a moved station stays moved");
            Assert.AreEqual(GardenGrid.DefaultDecorTile(6), back.Grid.TileOf("decor:6"));
        }

        [Test]
        public void An_Old_Save_Without_A_Grid_Is_Given_One_With_Everything_It_Had()
        {
            // A version 14 save: six plots (the first wave taken), three places to rest, two decorations standing, no "grid" block.
            string json = "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":30},{\"id\":\"leaf\",\"level\":5},{\"id\":\"jam\",\"level\":0}],"
                + "\"plots\":3,\"restSpots\":3,\"gardenXp\":500,"
                + "\"decor\":{\"owned\":{\"den-da\":1,\"bon-hoa\":1},\"spots\":[null,\"den-da\",null,null,null,null,\"bon-hoa\",null],\"lantern\":0}}";
            LoadResult r = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            GardenGrid g = r.State.Grid;
            Assert.AreEqual(GardenGrid.RadiusForPlots(3), g.Radius);
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
                + "\"grid\":{\"radius\":99,\"tiles\":[{\"q\":0,\"r\":0,\"id\":\"station:tea\"},{\"q\":0,\"r\":0,\"id\":\"station:leaf\"},{\"q\":1,\"r\":1,\"id\":\"ghost\"},{\"q\":2,\"r\":2,\"id\":\"station:jam\"}]}}";
            GameState s = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(new Hex(0, 0), s.Grid.TileOf("station:tea"));
            Assert.IsNotNull(s.Grid.TileOf("station:leaf"), "the second claim on the centre is dropped, and the leaf basket is seated elsewhere");
            Assert.AreNotEqual(new Hex(0, 0), s.Grid.TileOf("station:leaf"));
            Assert.IsNull(s.Grid.TileOf("ghost"), "nothing the game does not know");
            Assert.AreEqual(new Hex(2, 2), s.Grid.TileOf("station:jam"), "a saved tile is kept (three stations always have their three plots)");
            s.Plots = 2; GardenGrid.Settle(defs, s);
            Assert.IsNull(s.Grid.TileOf("station:jam"), "no plot, no tile");
            Assert.LessOrEqual(s.Grid.Radius, GardenGrid.MaxRadius);
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
    }
}

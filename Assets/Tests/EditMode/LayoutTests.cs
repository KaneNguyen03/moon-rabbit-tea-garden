using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>Arranging the garden (#54): moving things to empty tiles, swapping two, putting a station away and taking it out.</summary>
    public class LayoutTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels[1] = 5; s.Levels[2] = 3; // tea, leaf and jam all open
        }

        static Hex Tile(GameState s, string id) => s.Grid.TileOf(id).Value;

        [Test]
        public void Move_Takes_A_Thing_To_An_Empty_Lawn_Tile_Only()
        {
            GardenGrid g = s.Grid;
            Hex jam = Tile(s, "station:jam");
            Assert.IsNull(g.OccupantAt(-1, 2), "the test's empty tile, two from the teapot (#88)");
            Assert.IsTrue(g.Move(jam, new Hex(-1, 2)));
            Assert.AreEqual(new Hex(-1, 2), Tile(s, "station:jam"));
            Assert.IsNull(g.OccupantAt(jam.Q, jam.R), "the old tile is free again");

            Assert.IsFalse(g.Move(new Hex(-1, 2), Tile(s, "station:tea")), "a taken tile is a swap, not a move");
            Assert.IsFalse(g.Move(new Hex(-1, 2), new Hex(9, 0)), "off the lawn");
            Assert.IsFalse(g.Move(new Hex(0, 2), new Hex(0, 1)), "nothing stands on the tile to move from");
            Assert.IsFalse(g.Move(Tile(s, GardenGrid.MortarId), new Hex(0, 1)), "the props that come with the garden stay where they are");
            Assert.IsFalse(g.Move(new Hex(-1, 2), new Hex(-1, 2)), "the same tile is no move");
            Assert.AreEqual(new Hex(-1, 2), Tile(s, "station:jam"));
        }

        [Test]
        public void Swap_Exchanges_Two_Things_That_Can_Be_Moved()
        {
            GardenGrid g = s.Grid;
            Hex tea = Tile(s, "station:tea"), leaf = Tile(s, "station:leaf"), seat = Tile(s, "rest:0");
            Assert.IsTrue(g.Swap(tea, leaf));
            Assert.AreEqual(leaf, Tile(s, "station:tea"));
            Assert.AreEqual(tea, Tile(s, "station:leaf"));
            Assert.IsTrue(g.Swap(leaf, seat), "a station and a place to rest");
            Assert.AreEqual(seat, Tile(s, "station:tea"));
            Assert.AreEqual(leaf, Tile(s, "rest:0"));
            Assert.IsFalse(g.Swap(seat, Tile(s, GardenGrid.ShopId)), "not with a prop");
            Assert.IsFalse(g.Swap(seat, new Hex(0, 1)), "an empty tile is a move");
            Assert.IsFalse(g.Swap(seat, seat));
            Assert.AreEqual(g.OccupantCount, new HashSet<Hex>(TilesOf(g)).Count, "nothing shares a tile");
        }

        static IEnumerable<Hex> TilesOf(GardenGrid g) { foreach (KeyValuePair<string, Hex> o in g.Occupants) yield return o.Value; }

        [Test]
        public void Drop_Moves_On_An_Empty_Tile_Swaps_On_A_Taken_One_And_Goes_Back_Otherwise()
        {
            Hex jam = Tile(s, "station:jam"), tea = Tile(s, "station:tea");
            Assert.AreEqual(LayoutDrop.Placed, Layout.Drop(defs, s, jam, new Hex(-1, 2)));
            Assert.AreEqual(LayoutDrop.Swapped, Layout.Drop(defs, s, new Hex(-1, 2), tea));
            Assert.AreEqual(tea, Tile(s, "station:jam"));
            Assert.AreEqual(new Hex(-1, 2), Tile(s, "station:tea"));
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, tea, new Hex(20, 0)), "outside the lawn: back where it was");
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, tea, tea));
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, tea, Tile(s, GardenGrid.MortarId)));
            Assert.AreEqual(tea, Tile(s, "station:jam"));
        }

        // ----- Stations keep apart (#88) -----

        [Test]
        public void A_Station_Cannot_Be_Moved_Onto_A_Tile_Next_To_Another_Station()
        {
            // #88: a station dropped right beside another was drawn over it, its level plate on the other's rabbit.
            GardenGrid g = s.Grid;
            Hex jam = Tile(s, "station:jam"), tea = Tile(s, "station:tea"), leaf = Tile(s, "station:leaf");
            foreach (Hex h in new[] { new Hex(-1, 1), new Hex(0, 1), new Hex(1, 1), new Hex(3, 0) })
            {
                Assert.IsTrue(g.InLawn(h) && !GardenGrid.IsReserved(h) && g.OccupantAt(h.Q, h.R) == null, h + " is an empty lawn tile");
                Assert.IsTrue(GardenGrid.Distance(h, tea) == 1 || GardenGrid.Distance(h, leaf) == 1, h + " is next to the teapot or the leaf basket");
                Assert.IsFalse(g.Move(jam, h), "the jam pot is not moved to " + h);
                Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, jam, h), "dropped on " + h + ": back where it was");
                Assert.AreEqual(jam, Tile(s, "station:jam"));
            }
            Assert.AreEqual(Expansion.StationSpacing, GardenGrid.Distance(new Hex(-1, 2), tea), "the spacing the game keeps for the stations it stands itself");
            Assert.AreEqual(LayoutDrop.Placed, Layout.Drop(defs, s, jam, new Hex(-1, 2)), "two tiles from every station is fine");
            Assert.AreEqual(new Hex(-1, 2), Tile(s, "station:jam"));
        }

        [Test]
        public void A_Swap_Checks_Where_Both_Stations_Land()
        {
            GardenGrid g = s.Grid;
            Hex tea = Tile(s, "station:tea");
            // A save from before #88 may already have the jam pot touching the leaf basket.
            Assert.IsTrue(g.Place("station:jam", 2, 1));
            Hex jam = new Hex(2, 1);
            Assert.AreEqual(1, GardenGrid.Distance(jam, Tile(s, "station:leaf")));
            Assert.IsFalse(g.Swap(tea, jam), "the teapot would land beside the leaf basket");
            Assert.IsFalse(g.Swap(jam, tea), "whichever of the two is carried");
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, tea, jam));
            Assert.AreEqual(LayoutDrop.Back, Layout.Drop(defs, s, jam, tea));
            Assert.AreEqual(tea, Tile(s, "station:tea"));
            Assert.AreEqual(jam, Tile(s, "station:jam"));
            // A place to rest beside a station is fine, and the station that takes its tile lands two from the others.
            Hex seat = Tile(s, "rest:0");
            Assert.AreEqual(LayoutDrop.Swapped, Layout.Drop(defs, s, jam, seat), "the jam pot goes to the stump, the stump beside the leaf basket");
            Assert.AreEqual(seat, Tile(s, "station:jam"));
            Assert.AreEqual(jam, Tile(s, "rest:0"));
            // Two stations that each land two from the rest swap as before.
            Assert.IsTrue(g.Swap(tea, Tile(s, "station:leaf")));
        }

        [Test]
        public void A_Decoration_Or_A_Place_To_Rest_May_Stand_Next_To_A_Station()
        {
            GardenGrid g = s.Grid;
            Hex tea = Tile(s, "station:tea");
            Assert.IsTrue(g.Move(Tile(s, "rest:0"), new Hex(-1, 1)), "a stump beside the teapot");
            Assert.AreEqual(1, GardenGrid.Distance(Tile(s, "rest:0"), tea));
            s.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(s, "den-da") && Decor.Place(s, 2, "den-da"));
            Hex lantern = Tile(s, GardenGrid.DecorId(2));
            Assert.AreEqual(LayoutDrop.Placed, Layout.Drop(defs, s, lantern, new Hex(0, 1)), "a lantern beside the teapot");
            Assert.AreEqual(LayoutDrop.Swapped, Layout.Drop(defs, s, new Hex(0, 1), new Hex(-1, 1)), "and the two swap beside it");
            Assert.AreEqual(new Hex(-1, 1), Tile(s, GardenGrid.DecorId(2)));
            Assert.AreEqual(new Hex(0, 1), Tile(s, "rest:0"));
        }

        [Test]
        public void A_Stored_Station_Keeps_Its_Level_Earns_Nothing_And_Its_Rabbit_Rests()
        {
            Rabbit r = Rabbits.Arrive(s, 2, "Lam", 0d);
            Assert.IsTrue(Rabbits.Assign(s, r.Id, 2));
            BigNum before = Economy.IncomePerSecond(defs, s);
            BigNum teaOnly = Economy.IncomePerCycle(defs, s, 0) / defs[0].CycleSeconds;
            Hex jamTile = Tile(s, "station:jam");

            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            Assert.IsTrue(Layout.IsStored(defs, s, 2));
            Assert.IsTrue(s.Grid.IsStored("station:jam"));
            Assert.IsNull(s.Grid.TileOf("station:jam"), "it leaves the lawn");
            Assert.IsNull(s.Grid.OccupantAt(jamTile.Q, jamTile.R));
            Assert.AreEqual(3, s.Levels[2], "it keeps its level");
            Assert.AreEqual(-1, r.Station, "its rabbit goes to rest");
            Assert.AreNotEqual(2, Rabbits.BestStationFor(defs, s, r), "nobody is sent to a stored station");
            Assert.AreEqual(Rabbits.StationsWithoutWorker(s) - 1, Rabbits.StationsWithoutWorker(defs, s), "and it does not wait for a worker");
            Assert.IsTrue(Economy.IncomePerCycle(defs, s, 2).IsZero);
            Assert.IsTrue(Economy.StationIncomePerSecond(defs, s, 2, out _, out _).IsZero);
            Assert.Less(Economy.IncomePerSecond(defs, s).ToDouble(), before.ToDouble());
            Assert.AreEqual(teaOnly.ToDouble(), Economy.IncomePerSecond(defs, s).ToDouble(), 1e-9, "only the teapot earns");
            Assert.IsEmpty(Rabbits.SuitedStations(defs, s, r).FindAll(i => i == 2));
            Rabbits.AutoAssign(defs, s);
            Assert.AreNotEqual(2, r.Station, "filling the stations passes it by");

            double progress = s.Progress[2];
            BigNum coins = s.Coins;
            Simulation.Tick(defs, s, 30d);
            Assert.AreEqual(progress, s.Progress[2], "it does not work while stored");
            Assert.AreEqual(0, CountFrom(s, 2), "and makes nothing for the tray");
            Assert.Greater(s.Coins.ToDouble(), coins.ToDouble(), "the rest of the garden goes on earning");
        }

        static int CountFrom(GameState s, int station)
        {
            int n = 0;
            foreach (Product p in s.Tray) if (p.Station == station) n += p.Count;
            return n;
        }

        [Test]
        public void A_Stored_Ingredient_Station_No_Longer_Boosts_What_It_Feeds()
        {
            BigNum boosted = Economy.IncomePerCycle(defs, s, 0);
            Assert.IsTrue(Layout.Store(defs, s, "station:leaf"));
            Assert.Less(Economy.IncomePerCycle(defs, s, 0).ToDouble(), boosted.ToDouble());
            Assert.IsTrue(Layout.TakeOut(defs, s, 1));
            Assert.AreEqual(boosted.ToDouble(), Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
        }

        [Test]
        public void Only_Open_Stations_And_Decorations_Can_Be_Stored()
        {
            s.Levels[2] = 0;
            Assert.IsFalse(Layout.Store(defs, s, "station:jam"), "a station not built yet");
            Assert.IsFalse(Layout.Store(defs, s, "station:tea"), "the teapot is the heart of the garden and stays out");
            Assert.IsFalse(Layout.Store(defs, s, "rest:0"), "a place to rest");
            Assert.IsFalse(Layout.Store(defs, s, GardenGrid.ShopId));
            Assert.IsFalse(Layout.Store(defs, s, "station:nothing"));
            // A decoration goes back to the decorations' own storage (#11).
            s.GoldenCarrots = 100;
            Assert.IsTrue(Decor.Buy(s, "den-da"));
            Assert.IsTrue(Decor.Place(s, 2, "den-da"));
            Assert.IsTrue(Layout.Store(defs, s, "decor:2"));
            Assert.IsNull(s.DecorSpots[2]);
            Assert.AreEqual(1, Decor.Available(s, "den-da"));
            Assert.IsNull(s.Grid.TileOf("decor:2"));
            Assert.IsFalse(s.Grid.IsStored("decor:2"), "the grid's own store is for stations");
        }

        [Test]
        public void Taking_Out_Stands_The_Station_On_The_Empty_Tile_Nearest_The_Centre()
        {
            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            Assert.IsTrue(s.Grid.Move(Tile(s, "station:tea"), new Hex(0, 1)), "the heart is free");
            Assert.IsTrue(Layout.TakeOut(defs, s, 2));
            Assert.IsFalse(s.Grid.IsStored("station:jam"));
            Assert.AreEqual(new Hex(0, 0), Tile(s, "station:jam"), "nearest the centre");
            Assert.IsFalse(Layout.IsStored(defs, s, 2));
            Assert.AreEqual(3, s.Levels[2]);
            Assert.IsFalse(Layout.TakeOut(defs, s, 2), "it is out already");
        }

        [Test]
        public void A_Full_Lawn_Keeps_The_Station_In_Storage()
        {
            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            GardenGrid g = s.Grid;
            int n = 0;
            foreach (Hex h in GardenGrid.Strip(g.Segments)) if (g.OccupantAt(h.Q, h.R) == null) g.Place("decor:" + (100 + n++), h.Q, h.R, true);
            Assert.IsFalse(g.NearestEmpty(out _));
            Assert.IsFalse(Layout.TakeOut(defs, s, 2));
            Assert.IsTrue(g.IsStored("station:jam"));
        }

        [Test]
        public void Settle_Leaves_A_Stored_Station_Off_The_Lawn_Until_Its_Level_Is_Gone()
        {
            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            GardenGrid.Settle(defs, s);
            Assert.IsNull(s.Grid.TileOf("station:jam"), "settling does not stand it back");
            Assert.IsTrue(s.Grid.IsStored("station:jam"));
            // A new moon: every station starts again at nothing, and the store is emptied with it.
            s.GardenXp = Garden.XpForLevel(NewMoon.LevelNeeded(0));
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));
            Assert.IsFalse(s.Grid.IsStored("station:jam"));
            Assert.AreEqual(0, s.Grid.StoredCount);
            Assert.IsNotNull(s.Grid.TileOf("station:jam"), "its plot has its tile again");
        }

        [Test]
        public void Place_Refuses_A_Stored_Station()
        {
            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            Assert.IsFalse(s.Grid.Place("station:jam", 0, 1), "a stored station comes out from the stations tab");
            Assert.IsNull(s.Grid.TileOf("station:jam"));
        }

        [Test]
        public void The_Store_Survives_A_Save_And_An_Old_Save_Stores_Nothing()
        {
            Assert.IsTrue(Layout.Store(defs, s, "station:jam"));
            Assert.IsTrue(s.Grid.Move(Tile(s, "station:leaf"), new Hex(-1, 2)));
            string json = SaveCodec.Encode(defs, s);
            StringAssert.Contains("\"stored\":[\"station:jam\"]", json);
            GameState back = SaveCodec.Decode(defs, json).State;
            Assert.IsTrue(back.Grid.IsStored("station:jam"));
            Assert.IsNull(back.Grid.TileOf("station:jam"));
            Assert.AreEqual(new Hex(-1, 2), back.Grid.TileOf("station:leaf"));
            Assert.AreEqual(3, back.Levels[2]);
            Assert.AreEqual(SaveCodec.Encode(defs, s), SaveCodec.Encode(defs, back), "the same garden writes the same text");

            GameState plain = GameState.NewGame(defs);
            StringAssert.DoesNotContain("stored", SaveCodec.Encode(defs, plain), "nothing stored, nothing written: the text is as before");
            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":3},{\"id\":\"leaf\",\"level\":2},{\"id\":\"jam\",\"level\":1}],\"plots\":3}").State;
            Assert.AreEqual(0, old.Grid.StoredCount);
            // A stored id the game cannot keep (not a station, or not built) is dropped and the station stands.
            GameState odd = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[{\"id\":\"tea\",\"level\":3},{\"id\":\"leaf\",\"level\":0},{\"id\":\"jam\",\"level\":1}],\"plots\":3,"
                + "\"grid\":{\"radius\":3,\"tiles\":[],\"stored\":[\"station:leaf\",\"rest:0\",\"ghost\",\"station:jam\"]}}").State;
            Assert.AreEqual(1, odd.Grid.StoredCount);
            Assert.IsTrue(odd.Grid.IsStored("station:jam"));
            Assert.IsNotNull(odd.Grid.TileOf("station:leaf"));
        }

        [Test]
        public void Nothing_Stored_Changes_Nothing_In_Income()
        {
            GameState other = GameState.NewGame(defs);
            other.Levels[1] = 5; other.Levels[2] = 3;
            Assert.IsTrue(other.Grid.Move(other.Grid.TileOf("station:jam").Value, new Hex(-1, 2)), "only moved");
            Assert.AreEqual(Economy.IncomePerSecond(defs, s).ToDouble(), Economy.IncomePerSecond(defs, other).ToDouble(), 1e-12, "where a station stands does not change what it earns");
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class GardenStandsTests
    {
        [Test]
        public void The_Scale_Table_Measures_Everything_Against_A_Rabbit_As_The_Art_Bible_Says()
        {
            Assert.AreEqual(1d, GardenStands.Range(StandRole.Rabbit).Low);
            Assert.AreEqual(1d, GardenStands.Range(StandRole.Rabbit).High);
            Assert.AreEqual(0.8d, GardenStands.Range(StandRole.Station).Low);
            Assert.AreEqual(1.3d, GardenStands.Range(StandRole.Station).High);
            Assert.AreEqual(1.8d, GardenStands.Range(StandRole.Counter).Low);
            Assert.AreEqual(2.2d, GardenStands.Range(StandRole.Counter).High);
            Assert.AreEqual(1.4d, GardenStands.Range(StandRole.LampPost).Middle);
            Assert.AreEqual(0.9d, GardenStands.Range(StandRole.Aquarium).Middle);
            Assert.AreEqual(0.7d, GardenStands.Range(StandRole.Mortar).Middle);
            Assert.AreEqual(2.5d, GardenStands.Range(StandRole.Tree).Low);
            Assert.AreEqual(3.5d, GardenStands.Range(StandRole.Tree).High);
        }

        [Test]
        public void A_Height_Fits_Its_Role_Within_Fifteen_Percent_Of_The_Table()
        {
            StandRange counter = GardenStands.Range(StandRole.Counter);
            Assert.IsTrue(counter.Holds(2d));
            Assert.IsTrue(counter.Holds(1.8d * 0.85d + 1e-9));
            Assert.IsTrue(counter.Holds(2.2d * 1.15d - 1e-9));
            Assert.IsFalse(counter.Holds(1.8d * 0.85d - 1e-6));
            Assert.IsFalse(counter.Holds(2.2d * 1.15d + 1e-6));
            Assert.IsFalse(GardenStands.Range(StandRole.Mortar).Holds(1d), "a mortar as tall as a rabbit is too big");
        }

        [Test]
        public void A_Wanted_Height_Outside_Its_Role_Is_Pulled_Back_Into_The_Table()
        {
            Assert.AreEqual(2.2d * 1.15d, GardenStands.Metres(StandRole.Counter, 3d, 1.15d), 1e-9);
            Assert.AreEqual(0.8d * 1.15d, GardenStands.Metres(StandRole.Station, 0.2d, 1.15d), 1e-9);
            Assert.AreEqual(0.9d * 2d, GardenStands.Metres(StandRole.Station, 0.9d, 2d), 1e-9);
            Assert.AreEqual(1.15d, GardenStands.Metres(StandRole.Rabbit, 5d, 1.15d), 1e-9);
        }

        [Test]
        public void The_Garden_Shows_The_Same_Stations_As_The_Two_D_Street()
        {
            List<StationDef> defs = SixStations();
            GameState s = GameState.NewGame(defs);
            s.Plots = 3;
            Assert.AreEqual(3, GardenStands.Shown(defs, s), "the teapot and the outlines of the next two");
            s.Levels[1] = 1;
            Assert.AreEqual(4, GardenStands.Shown(defs, s));
            s.Levels[2] = 1;
            Assert.AreEqual(4, GardenStands.Shown(defs, s), "a plot short: one station waits on its land");
            s.Plots = 6;
            Assert.AreEqual(5, GardenStands.Shown(defs, s));
            for (int i = 0; i < defs.Count; i++) s.Levels[i] = 1;
            Assert.AreEqual(6, GardenStands.Shown(defs, s));
        }

        [Test]
        public void A_Station_Looks_Open_Unbuilt_Or_Like_Land_Waiting_For_Its_Plot()
        {
            List<StationDef> defs = SixStations();
            GameState s = GameState.NewGame(defs);
            s.Plots = 2;
            Assert.AreEqual(StandLook.Open, GardenStands.Look(s, 0));
            Assert.AreEqual(StandLook.Unbuilt, GardenStands.Look(s, 1));
            Assert.AreEqual(StandLook.Land, GardenStands.Look(s, 2));
        }

        [Test]
        public void A_Station_Stands_On_Its_Grid_Tile_Or_On_Its_Default_Tile_Before_The_Grid_Seats_It()
        {
            List<StationDef> defs = SixStations();
            GameState s = GameState.NewGame(defs);
            Assert.IsTrue(s.Grid.TryTileOf(defs[0].GridId, out Hex teapot));
            Assert.AreEqual(teapot, GardenStands.StationTile(defs, s, 0));
            s.Grid.Remove(defs[0].GridId);
            Assert.AreEqual(GardenGrid.DefaultStationTile(0), GardenStands.StationTile(defs, s, 0));
            Assert.AreEqual(GardenGrid.DefaultTile(GardenGrid.MortarId, null), GardenStands.PropTile(new GameState(), GardenGrid.MortarId));
        }

        [Test]
        public void A_Stored_Station_Does_Not_Stand_In_The_Garden()
        {
            List<StationDef> defs = SixStations();
            GameState s = GameState.NewGame(defs);
            s.Levels[1] = 1;
            s.Plots = 3;
            int shown = GardenStands.Shown(defs, s);
            Assert.IsTrue(GardenStands.Standing(defs, s, shown, 1));
            Assert.IsTrue(s.Grid.Store(defs[1].GridId));
            Assert.IsFalse(GardenStands.Standing(defs, s, shown, 1));
            Assert.IsFalse(GardenStands.Standing(defs, s, shown, shown));
            Assert.IsFalse(GardenStands.Standing(defs, s, shown, -1));
        }

        [Test]
        public void Each_Station_Names_The_First_Rabbit_Working_There_And_Zero_When_Nobody_Is()
        {
            List<StationDef> defs = SixStations();
            GameState s = GameState.NewGame(defs);
            s.Rabbits.Add(new Rabbit { Id = 7, Station = 2 });
            s.Rabbits.Add(new Rabbit { Id = 9, Station = 2 });
            s.Rabbits.Add(new Rabbit { Id = 4, Station = -1 });
            s.Rabbits.Add(new Rabbit { Id = 5, Station = 0 });
            s.Rabbits.Add(new Rabbit { Id = 6, Station = 99 });
            var workers = new long[defs.Count];
            workers[3] = 42;
            GardenStands.Workers(s, workers);
            Assert.AreEqual(new long[] { 5, 0, 7, 0, 0, 0 }, workers);
            Assert.AreEqual(Rabbits.AtStation(s, 2).Id, workers[2]);
        }

        [Test]
        public void Small_Dressing_Stands_Between_A_Third_And_Two_Thirds_Of_A_Rabbit_As_In_The_Two_D_Garden()
        {
            Assert.AreEqual(0.3d, GardenStands.Range(StandRole.Dressing).Low);
            Assert.AreEqual(0.7d, GardenStands.Range(StandRole.Dressing).High);
        }

        [Test]
        public void A_Path_Between_Two_Feet_Is_Laid_From_Whole_Pieces_Stretched_Evenly_End_To_End()
        {
            var pieces = new List<GroundPoint>();
            Assert.AreEqual(3, GardenStands.PathPieces(new GroundPoint(0d, 0d), new GroundPoint(4d, 0d), 1.6d, pieces), "4 m of path takes three 1.6 m pieces, each stretched to 1.33 m");
            Assert.AreEqual(4d / 6d, pieces[0].X, 1e-9);
            Assert.AreEqual(2d, pieces[1].X, 1e-9);
            Assert.AreEqual(4d - 4d / 6d, pieces[2].X, 1e-9);
            pieces.Clear();
            Assert.AreEqual(2, GardenStands.PathPieces(new GroundPoint(0d, 0d), new GroundPoint(0d, -2d), 1.6d, pieces));
            Assert.AreEqual(-0.5d, pieces[0].Z, 1e-9);
            Assert.AreEqual(0, GardenStands.PathPieces(new GroundPoint(1d, 1d), new GroundPoint(1d, 1d), 1.6d, pieces), "no path to itself");
        }

        [Test]
        public void A_Lamp_Stands_Beside_The_Middle_Of_A_Path_On_The_Side_Away_From_The_Teapot()
        {
            GroundPoint lamp = GardenStands.Aside(new GroundPoint(2d, -1d), new GroundPoint(2d, 1d), 0.9d);
            Assert.AreEqual(2.9d, lamp.X, 1e-9);
            Assert.AreEqual(0d, lamp.Z, 1e-9);
            lamp = GardenStands.Aside(new GroundPoint(-2d, 1d), new GroundPoint(-2d, -1d), 0.9d);
            Assert.AreEqual(-2.9d, lamp.X, 1e-9);
        }

        [Test]
        public void The_Garden_Paths_Join_The_Teapot_To_The_First_Ring_And_On_To_The_Far_Corners()
        {
            Assert.AreEqual(6, GardenStands.Paths.Length);
            foreach (StationPair pair in GardenStands.Paths) Assert.AreNotEqual(pair.From, pair.To);
            Assert.AreEqual(0, GardenStands.Paths[0].From);
        }

        [Test]
        public void Trees_Stand_In_A_Staggered_Row_Behind_The_Back_Of_The_Island_Across_Its_Width()
        {
            var shore = new GroundRect(-6d, 6d, -4d, 4d);
            var trees = new List<GroundPoint>();
            GardenStands.TreeRow(shore, 3d, 1.2d, trees);
            Assert.AreEqual(5, trees.Count);
            Assert.AreEqual(-6d, trees[0].X, 1e-9);
            Assert.AreEqual(6d, trees[4].X, 1e-9);
            foreach (GroundPoint t in trees) Assert.Greater(t.Z, shore.MaxZ, "behind the island, never on the open land");
            Assert.AreNotEqual(trees[0].Z, trees[1].Z, "staggered, not a fence");
        }

        [Test]
        public void The_Mortar_Waits_For_Its_Garden_Level_The_Pond_Tank_For_Fishing_And_The_Shop_Is_Always_There()
        {
            List<StationDef> defs = SixStations();
            GameState s = GameState.NewGame(defs);
            Assert.IsTrue(GardenStands.FixtureShown(s, GardenFixture.Shop));
            Assert.AreEqual(Pounding.Unlocked(s), GardenStands.FixtureShown(s, GardenFixture.Mortar));
            Assert.AreEqual(Fishing.Unlocked(s), GardenStands.FixtureShown(s, GardenFixture.Aquarium));
            Assert.AreEqual(GardenGrid.TankTile, GardenStands.FixtureTile(s, GardenFixture.Aquarium));
            Assert.AreEqual(GardenStands.PropTile(s, GardenGrid.MortarId), GardenStands.FixtureTile(s, GardenFixture.Mortar));
            Assert.AreEqual(GardenStands.PropTile(s, GardenGrid.ShopId), GardenStands.FixtureTile(s, GardenFixture.Shop));
        }

        static List<StationDef> SixStations()
        {
            var defs = new List<StationDef>();
            for (int i = 0; i < 6; i++) defs.Add(new StationDef { Id = "s" + i, Name = "S" + i, Kind = StationKind.Brew, BaseCost = 10, CostGrowth = 1.1, BaseIncome = 1, CycleSeconds = 2 });
            return defs;
        }
    }
}

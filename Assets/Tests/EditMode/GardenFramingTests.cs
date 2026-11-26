using System;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class GardenFramingTests
    {
        const double Portrait = 1080d / 1920d;
        static readonly CameraSpec Spec = new CameraSpec();

        [Test]
        public void The_Camera_Is_A_Fixed_Twenty_Five_Degree_Lens_Tilted_Forty_Degrees_With_No_Turn()
        {
            Assert.AreEqual(25d, Spec.FovDegrees);
            Assert.AreEqual(40d, Spec.TiltDegrees);
            CameraPose pose = GardenFraming.Pose(new GroundPoint(2d, -3d), Spec.DefaultDistance, Spec);
            Assert.AreEqual(2d, pose.X, 1e-9, "the camera never turns, so it stays straight behind the focus");
            Assert.AreEqual(40d, pose.PitchDegrees, 1e-9);
            double back = -3d - pose.Z;
            Assert.AreEqual(Math.Tan(40d * Math.PI / 180d), pose.Y / back, 1e-9, "the focus sits on the camera's centre line");
            Assert.AreEqual(Spec.DefaultDistance, Math.Sqrt(pose.Y * pose.Y + back * back), 1e-9);
        }

        [Test]
        public void The_Default_Zoom_Shows_As_Much_Garden_Across_As_The_Two_D_Default_Zoom()
        {
            double twoDHalfWidthMetres = 1080d / 2d / CameraSpec.ZoomDefault / GardenGrid.PitchX * Spec.TileWidth;
            Assert.AreEqual(twoDHalfWidthMetres, GardenFraming.HalfWidthAt(Spec.DefaultDistance, Portrait, Spec), twoDHalfWidthMetres * 0.05);
        }

        [Test]
        public void The_Two_D_Zoom_Levels_Become_Camera_Distances()
        {
            Assert.AreEqual(Spec.DefaultDistance, GardenFraming.DistanceOf(CameraSpec.ZoomDefault, Spec), 1e-9);
            Assert.AreEqual(Spec.NearDistance, GardenFraming.DistanceOf(CameraSpec.ZoomNear, Spec), 1e-9);
            Assert.AreEqual(Spec.DefaultDistance * CameraSpec.ZoomDefault / CameraSpec.ZoomNear, Spec.NearDistance, 1e-9);
            Assert.Less(Spec.NearDistance, Spec.DefaultDistance);
            Assert.Less(Spec.DefaultDistance, Spec.FarDistance);
            Assert.AreEqual(1.165d, CameraSpec.DetailZoom, 1e-9);
            Assert.AreEqual(CameraSpec.ZoomNear, GardenFraming.ZoomOf(Spec.NearDistance, Spec), 1e-9);
            Assert.IsTrue(GardenFraming.ShowsDetail(GardenFraming.DistanceOf(1.2d, Spec), Spec));
            Assert.IsFalse(GardenFraming.ShowsDetail(Spec.DefaultDistance, Spec));
        }

        [Test]
        public void Every_Lawn_Tile_Stands_Inside_The_Island_And_Neighbours_Are_One_Tile_Apart()
        {
            for (int segments = 0; segments <= GardenGrid.MaxSegments; segments++)
            {
                GroundRect island = GardenFraming.Island(segments, Spec);
                foreach (Hex h in GardenGrid.Strip(segments))
                {
                    GroundPoint p = GardenFraming.Ground(h, Spec);
                    Assert.IsTrue(island.Contains(p), "segments " + segments + " tile " + h);
                    Assert.GreaterOrEqual(p.X - island.MinX, Spec.TileWidth / 2d - 1e-9, "a rim of half a tile around the edge tiles");
                }
            }
            GroundPoint o = GardenFraming.Ground(new Hex(0, 0), Spec), right = GardenFraming.Ground(new Hex(1, 0), Spec), back = GardenFraming.Ground(new Hex(0, -1), Spec);
            Assert.AreEqual(Spec.TileWidth, right.X - o.X, 1e-9);
            Assert.AreEqual(Spec.RowDepth, back.Z - o.Z, 1e-9, "a row up the 2D screen is a row further from the camera");
        }

        [Test]
        public void Opening_A_Segment_Grows_The_Island_Toward_Its_Side()
        {
            for (int n = 1; n <= GardenGrid.MaxSegments; n++)
            {
                GroundRect before = GardenFraming.Island(n - 1, Spec), after = GardenFraming.Island(n, Spec);
                GardenSide side = GardenGrid.SideOf(n);
                Assert.AreEqual(side == GardenSide.Right, after.MaxX > before.MaxX, "segment " + n);
                Assert.AreEqual(side == GardenSide.Left, after.MinX < before.MinX, "segment " + n);
                Assert.AreEqual(side == GardenSide.Up, after.MaxZ > before.MaxZ, "segment " + n);
                Assert.AreEqual(side == GardenSide.Down, after.MinZ < before.MinZ, "segment " + n);
            }
        }

        [Test]
        public void Dragging_To_Any_Corner_Keeps_The_View_Centre_On_The_Island_Whatever_The_Zoom()
        {
            foreach (int segments in new[] { 0, 6 })
            {
                GroundRect island = GardenFraming.Island(segments, Spec);
                foreach (var corner in new[] { (-1d, -1d), (-1d, 1d), (1d, -1d), (1d, 1d) })
                {
                    var far = new GroundPoint(corner.Item1 * 500d, corner.Item2 * 500d);
                    GroundPoint held = GardenFraming.Drag(far, island, Spec);
                    GroundPoint rest = GardenFraming.Settle(held, island);
                    Assert.IsTrue(island.Contains(rest), "segments " + segments + " corner " + corner);
                    Assert.LessOrEqual(Math.Abs(held.X - rest.X), Spec.EdgeGive + 1e-9);
                    Assert.LessOrEqual(Math.Abs(held.Z - rest.Z), Spec.EdgeGive + 1e-9);
                }
            }
        }

        [Test]
        public void Pulling_Past_The_Edge_Gives_Less_And_Less()
        {
            GroundRect island = GardenFraming.Island(0, Spec);
            double last = island.MaxX, lastStep = double.MaxValue;
            for (int i = 1; i <= 20; i++)
            {
                double x = GardenFraming.Drag(new GroundPoint(island.MaxX + i * 0.5d, 0d), island, Spec).X;
                Assert.Greater(x, last);
                Assert.Less(x - last, lastStep);
                lastStep = x - last;
                last = x;
            }
            Assert.AreEqual(1.5d, GardenFraming.Drag(new GroundPoint(1.5d, 0d), island, Spec).X, 1e-9, "inside the island a drag is exact");
        }

        [Test]
        public void A_Flick_Glides_The_Same_At_Any_Frame_Rate_Fades_In_About_A_Third_Of_A_Second_And_Stops()
        {
            double once = GardenFraming.Glide(4d, 0.1d, Spec, false);
            double twice = GardenFraming.Glide(GardenFraming.Glide(4d, 0.05d, Spec, false), 0.05d, Spec, false);
            Assert.AreEqual(once, twice, 1e-9);
            Assert.AreEqual(-once, GardenFraming.Glide(-4d, 0.1d, Spec, false), 1e-9);
            Assert.LessOrEqual(GardenFraming.Glide(4d, 0.35d, Spec, false), 4d * 0.05d + 1e-9);
            double v = 4d;
            int frames = 0;
            while (v != 0d && frames < 600) { v = GardenFraming.Glide(v, 1d / 60d, Spec, false); frames++; }
            Assert.AreEqual(0d, v);
            Assert.Less(frames, 40, "a flick dies within two thirds of a second");
        }

        [Test]
        public void Reduced_Motion_Stops_The_Garden_The_Moment_The_Finger_Lifts()
        {
            Assert.AreEqual(0d, GardenFraming.Glide(4d, 1d / 60d, Spec, true));
        }

        [Test]
        public void Pinching_Scales_The_Distance_And_Stops_At_Near_And_Far()
        {
            Assert.AreEqual(Spec.DefaultDistance / 1.25d, GardenFraming.Pinch(Spec.DefaultDistance, 200d, 250d, Spec), 1e-9);
            Assert.AreEqual(Spec.NearDistance, GardenFraming.Pinch(Spec.DefaultDistance, 100d, 1000d, Spec));
            Assert.AreEqual(Spec.FarDistance, GardenFraming.Pinch(Spec.DefaultDistance, 1000d, 100d, Spec));
            Assert.AreEqual(Spec.DefaultDistance, GardenFraming.Pinch(Spec.DefaultDistance, 0d, 100d, Spec), "a pinch with no start span does nothing");
        }

        [Test]
        public void Zooming_Keeps_The_Ground_Under_The_Fingers_Under_The_Fingers()
        {
            var focus = new GroundPoint(1d, 2d);
            var anchor = new GroundPoint(4d, -1d);
            GroundPoint closer = GardenFraming.ZoomAbout(focus, anchor, 30d, 15d);
            Assert.AreEqual(2.5d, closer.X, 1e-9, "half the distance halves the gap between focus and the pinched ground");
            Assert.AreEqual(0.5d, closer.Z, 1e-9);
            GroundPoint same = GardenFraming.ZoomAbout(focus, anchor, 30d, 30d);
            Assert.AreEqual(focus.X, same.X, 1e-9);
            Assert.AreEqual(focus.Z, same.Z, 1e-9);
        }

        [Test]
        public void A_Ground_Point_Picks_Its_Own_Tile_And_Never_The_Neighbour()
        {
            foreach (Hex h in GardenGrid.Strip(6))
            {
                GroundPoint c = GardenFraming.Ground(h, Spec);
                Assert.AreEqual(h, GardenFraming.TileAt(c, Spec));
                foreach (Hex n in GardenGrid.Neighbours(h))
                {
                    GroundPoint towards = GardenFraming.Ground(n, Spec);
                    var near = new GroundPoint(c.X + (towards.X - c.X) * 0.45d, c.Z + (towards.Z - c.Z) * 0.45d);
                    var past = new GroundPoint(c.X + (towards.X - c.X) * 0.55d, c.Z + (towards.Z - c.Z) * 0.55d);
                    Assert.AreEqual(h, GardenFraming.TileAt(near, Spec), "from " + h + " toward " + n);
                    Assert.AreEqual(n, GardenFraming.TileAt(past, Spec), "from " + h + " toward " + n);
                }
            }
        }

        [Test]
        public void The_Map_Camera_Frames_The_Whole_Island_Seen_Straight_Down()
        {
            foreach (int segments in new[] { 0, 6, GardenGrid.MaxSegments })
                foreach (double aspect in new[] { Portrait, 1d, 16d / 9d })
                {
                    GroundRect island = GardenFraming.Island(segments, Spec);
                    MapFrame map = GardenFraming.Map(island, aspect);
                    Assert.AreEqual((island.MinX + island.MaxX) / 2d, map.Centre.X, 1e-9);
                    Assert.AreEqual((island.MinZ + island.MaxZ) / 2d, map.Centre.Z, 1e-9);
                    Assert.GreaterOrEqual(map.HalfHeight, island.Depth / 2d - 1e-9);
                    Assert.GreaterOrEqual(map.HalfHeight * aspect, island.Width / 2d - 1e-9);
                    Assert.IsTrue(map.HalfHeight <= island.Depth / 2d + 1e-9 || map.HalfHeight * aspect <= island.Width / 2d + 1e-9, "no wasted border on both axes");
                }
        }
    }
}

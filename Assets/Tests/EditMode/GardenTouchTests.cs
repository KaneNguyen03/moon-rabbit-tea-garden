using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class GardenTouchTests
    {
        static readonly CameraSpec Spec = new CameraSpec();

        [Test]
        public void A_Finger_That_Moves_Twelve_Canvas_Units_Or_Less_Is_Still_A_Tap()
        {
            Assert.AreEqual(12d, GardenFraming.TapSlopPixels(1080d, 1920d), 1e-9);
            Assert.IsTrue(GardenFraming.IsTap(12d, 0d, 1080d, 1920d));
            Assert.IsTrue(GardenFraming.IsTap(-7d, 9d, 1080d, 1920d));
            Assert.IsFalse(GardenFraming.IsTap(12.1d, 0d, 1080d, 1920d));
            Assert.IsFalse(GardenFraming.IsTap(9d, 9d, 1080d, 1920d), "the slop is a circle, not a square");
        }

        [Test]
        public void The_Tap_Slop_Grows_With_The_Canvas_Scale_On_Bigger_Screens()
        {
            Assert.AreEqual(24d, GardenFraming.TapSlopPixels(2160d, 3840d), 1e-9);
            Assert.AreEqual(6d, GardenFraming.TapSlopPixels(540d, 960d), 1e-9);
            Assert.IsTrue(GardenFraming.IsTap(20d, 0d, 2160d, 3840d));
            Assert.IsFalse(GardenFraming.IsTap(20d, 0d, 1080d, 1920d));
        }

        [Test]
        public void Two_Taps_Count_As_A_Double_Tap_Only_When_Quick_And_Close_Together()
        {
            Assert.IsTrue(GardenFraming.IsDoubleTap(0.2d, 30d, 0d, 1080d, 1920d));
            Assert.IsTrue(GardenFraming.IsDoubleTap(0.35d, 0d, 48d, 1080d, 1920d));
            Assert.IsFalse(GardenFraming.IsDoubleTap(0.36d, 0d, 0d, 1080d, 1920d), "too slow: two single taps");
            Assert.IsFalse(GardenFraming.IsDoubleTap(0.1d, 49d, 0d, 1080d, 1920d), "too far apart: two single taps");
            Assert.IsTrue(GardenFraming.IsDoubleTap(0.1d, 90d, 0d, 2160d, 3840d), "the reach grows with the canvas scale");
            Assert.IsFalse(GardenFraming.IsDoubleTap(-0.1d, 0d, 0d, 1080d, 1920d));
        }

        [Test]
        public void A_Double_Tap_Goes_From_Default_To_Near_And_From_Near_Back_To_Default()
        {
            Assert.AreEqual(Spec.NearDistance, GardenFraming.DoubleTapDistance(Spec.DefaultDistance, Spec), 1e-9);
            Assert.AreEqual(Spec.DefaultDistance, GardenFraming.DoubleTapDistance(Spec.NearDistance, Spec), 1e-9);
            Assert.AreEqual(Spec.NearDistance, GardenFraming.DoubleTapDistance(Spec.FarDistance, Spec), 1e-9, "from far away a double tap comes in close");
            double pastDetail = GardenFraming.DistanceOf(CameraSpec.DetailZoom + 0.01d, Spec);
            Assert.AreEqual(Spec.DefaultDistance, GardenFraming.DoubleTapDistance(pastDetail, Spec), 1e-9, "once the labels show in full the garden counts as near");
        }

        [Test]
        public void The_Zoom_Button_Steps_Far_Then_Default_Then_Near_And_Round_Again()
        {
            Assert.AreEqual(Spec.DefaultDistance, GardenFraming.NextZoomStep(Spec.FarDistance, Spec), 1e-9);
            Assert.AreEqual(Spec.NearDistance, GardenFraming.NextZoomStep(Spec.DefaultDistance, Spec), 1e-9);
            Assert.AreEqual(Spec.FarDistance, GardenFraming.NextZoomStep(Spec.NearDistance, Spec), 1e-9);
            Assert.AreEqual(Spec.NearDistance, GardenFraming.NextZoomStep(Spec.DefaultDistance * 0.97d, Spec), 1e-9, "a pinch left near a level steps from that level");
        }

        [Test]
        public void The_Map_Leaves_A_Strip_Of_Night_Sea_Round_The_Island()
        {
            GroundRect island = GardenFraming.Island(0, Spec);
            GroundRect seen = GardenFraming.Pad(island, 1d);
            Assert.AreEqual(island.Width + 2d, seen.Width, 1e-9);
            Assert.AreEqual(island.Depth + 2d, seen.Depth, 1e-9);
            Assert.AreEqual(island.Centre.X, seen.Centre.X, 1e-9);
            Assert.AreEqual(island.Centre.Z, seen.Centre.Z, 1e-9);
        }

        [Test]
        public void A_Tap_On_The_Map_Names_The_Ground_Straight_Under_It()
        {
            GroundRect island = GardenFraming.Island(3, Spec);
            const double aspect = 1080d / 1500d;
            MapFrame frame = GardenFraming.Map(island, aspect);
            GroundPoint middle = GardenFraming.MapPoint(frame, aspect, 0.5d, 0.5d);
            Assert.AreEqual(frame.Centre.X, middle.X, 1e-9);
            Assert.AreEqual(frame.Centre.Z, middle.Z, 1e-9);
            GroundPoint topRight = GardenFraming.MapPoint(frame, aspect, 1d, 1d);
            Assert.AreEqual(frame.Centre.X + frame.HalfHeight * aspect, topRight.X, 1e-9, "right on the map is +X on the ground");
            Assert.AreEqual(frame.Centre.Z + frame.HalfHeight, topRight.Z, 1e-9, "up on the map is the far side of the garden");
            foreach (Hex h in new[] { new Hex(0, 0), new Hex(4, -2), new Hex(-3, 3) })
            {
                GroundPoint g = GardenFraming.Ground(h, Spec);
                double u = 0.5d + (g.X - frame.Centre.X) / (2d * frame.HalfHeight * aspect), v = 0.5d + (g.Z - frame.Centre.Z) / (2d * frame.HalfHeight);
                Assert.AreEqual(h, GardenFraming.TileAt(GardenFraming.MapPoint(frame, aspect, u, v), Spec));
            }
        }
    }
}

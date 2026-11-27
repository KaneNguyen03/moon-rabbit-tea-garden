using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class IslandOutlineTests
    {
        static readonly CameraSpec Spec = new CameraSpec();
        static readonly GroundRect Island = GardenFraming.Island(3, Spec);

        [Test]
        public void The_Island_Corners_Are_Rounded_With_The_Radius_Asked_For()
        {
            List<GroundPoint> ring = IslandShape.Outline(Island, 4d, 0d);
            double nearest = double.MaxValue;
            foreach (GroundPoint p in ring)
            {
                Assert.IsTrue(Island.Contains(p), p.ToString());
                double dx = p.X - Island.MinX, dz = p.Z - Island.MinZ;
                nearest = Math.Min(nearest, Math.Sqrt(dx * dx + dz * dz));
            }
            Assert.AreEqual(4d * (Math.Sqrt(2d) - 1d), nearest, 1e-6, "the arc passes no closer to the old corner than a 4 m round corner does");
            Assert.AreEqual(4 * (IslandShape.ArcSteps + 1), ring.Count);
        }

        [Test]
        public void A_Radius_Larger_Than_The_Island_Shrinks_To_Half_Its_Shorter_Side()
        {
            var small = new GroundRect(0d, 6d, 0d, 3d);
            foreach (GroundPoint p in IslandShape.Outline(small, 10d, 0d)) Assert.IsTrue(small.Contains(new GroundPoint(p.X, p.Z)));
            List<GroundPoint> ring = IslandShape.Outline(small, 10d, 0d);
            Assert.AreEqual(0d, ring[IslandShape.ArcSteps].Z, 1e-9, "the front stays on the island's front line");
        }

        [Test]
        public void The_Front_Edge_Waves_Gently_By_At_Most_The_Wave_Height_And_The_Other_Edges_Stay_Straight()
        {
            List<GroundPoint> ring = IslandShape.Outline(Island, 4d, 0.4d);
            double low = double.MaxValue, high = double.MinValue;
            foreach (GroundPoint p in ring)
            {
                Assert.GreaterOrEqual(p.Z, Island.MinZ - 0.4d - 1e-9);
                Assert.LessOrEqual(p.Z, Island.MaxZ + 1e-9);
                if (p.X > Island.MinX + 4d && p.X < Island.MaxX - 4d && p.Z < Island.Centre.Z) { low = Math.Min(low, p.Z); high = Math.Max(high, p.Z); }
            }
            Assert.AreEqual(Island.MinZ - 0.4d, low, 1e-6);
            Assert.AreEqual(Island.MinZ + 0.4d, high, 1e-6);
        }

        [Test]
        public void A_Rounded_Waved_Slab_Still_Has_Its_Lawn_Up_Its_Underside_Down_And_Every_Side_Facing_Out()
        {
            var bands = new SlabSpec { Radius = 4d, Wave = 0.4d };
            SlabMesh slab = IslandShape.Slab(Island, bands);
            GroundPoint centre = Island.Centre;
            int up = 0, down = 0, outward = 0;
            for (int t = 0; t < slab.Triangles.Length; t += 3)
            {
                double[] a = At(slab, slab.Triangles[t]), b = At(slab, slab.Triangles[t + 1]), c = At(slab, slab.Triangles[t + 2]);
                double[] n = Cross(Sub(b, a), Sub(c, a));
                if (n[1] > 1e-9) { up++; continue; }
                if (n[1] < -1e-9) { down++; continue; }
                double mx = (a[0] + b[0] + c[0]) / 3d - centre.X, mz = (a[2] + b[2] + c[2]) / 3d - centre.Z;
                Assert.Greater(n[0] * mx + n[2] * mz, 0d, "triangle " + t / 3 + " faces out of the island");
                outward++;
            }
            int edges = IslandShape.Outline(Island, 4d, 0.4d).Count;
            Assert.AreEqual(edges, up, "a fan from the middle covers the waved lawn");
            Assert.AreEqual(edges, down);
            Assert.AreEqual(edges * 3 * 2, outward, "three bands on every edge");
            Assert.AreEqual(slab.Part(SlabPart.Lawn).Length, edges * 3);
        }

        [Test]
        public void At_Every_Zoom_The_Camera_Stops_Where_The_Front_Edge_Sits_Fifteen_Percent_Up_The_Screen()
        {
            foreach (double distance in new[] { Spec.NearDistance, Spec.DefaultDistance, Spec.FarDistance })
            {
                GroundRect area = GardenFraming.FocusArea(Island, Island.MinZ, distance, Spec);
                Assert.AreEqual(Island.MinX, area.MinX);
                Assert.AreEqual(Island.MaxX, area.MaxX);
                Assert.AreEqual(Island.MaxZ, area.MaxZ);
                Assert.Greater(area.MinZ, Island.MinZ, "the front can no longer be pulled to the middle of the screen");
                CameraPose pose = GardenFraming.Pose(new GroundPoint(0d, area.MinZ), distance, Spec);
                double toEdge = Math.Atan2(pose.Y, Island.MinZ - pose.Z) * 180d / Math.PI;
                double offAxis = (toEdge - Spec.TiltDegrees) * Math.PI / 180d;
                double share = 0.5d - Math.Tan(offAxis) / Math.Tan(Spec.FovDegrees * Math.PI / 360d) / 2d;
                Assert.AreEqual(Spec.FrontEdgeShare, share, 1e-6, "at " + distance + " m");
            }
        }

        [Test]
        public void An_Island_Too_Shallow_For_The_Margin_Keeps_The_Focus_On_Its_Back_Line()
        {
            var shallow = new GroundRect(-5d, 5d, 0d, 1d);
            GroundRect area = GardenFraming.FocusArea(shallow, shallow.MinZ, Spec.FarDistance, Spec);
            Assert.AreEqual(1d, area.MinZ);
            Assert.AreEqual(1d, area.MaxZ);
        }

        [Test]
        public void Rounding_The_Shore_Never_Cuts_Away_A_Tile_Of_Open_Land()
        {
            for (int segments = 0; segments <= GardenGrid.MaxSegments; segments++)
            {
                GroundRect island = GardenFraming.Island(segments, Spec);
                GroundRect shore = IslandShape.Shore(island, 4d);
                List<GroundPoint> ring = IslandShape.Outline(shore, 4d, 0.4d);
                foreach (Hex tile in GardenGrid.Strip(segments))
                {
                    GroundPoint at = GardenFraming.Ground(tile, Spec);
                    Assert.IsTrue(Inside(ring, at), "segment " + segments + ": tile " + tile + " at " + at + " stays on the island");
                }
            }
        }

        static bool Inside(List<GroundPoint> ring, GroundPoint p)
        {
            bool inside = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
                if ((ring[i].Z > p.Z) != (ring[j].Z > p.Z) && p.X < (ring[j].X - ring[i].X) * (p.Z - ring[i].Z) / (ring[j].Z - ring[i].Z) + ring[i].X) inside = !inside;
            return inside;
        }

        static double[] At(SlabMesh m, int i) => new[] { m.Positions[i * 3], m.Positions[i * 3 + 1], m.Positions[i * 3 + 2] };
        static double[] Sub(double[] p, double[] q) => new[] { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
        static double[] Cross(double[] p, double[] q) => new[] { p[1] * q[2] - p[2] * q[1], p[2] * q[0] - p[0] * q[2], p[0] * q[1] - p[1] * q[0] };
    }
}

using System;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class IslandShapeTests
    {
        static readonly GroundRect Island = GardenFraming.Island(0, new CameraSpec());

        [Test]
        public void The_Two_Point_Five_D_Garden_Is_Off_Until_The_Owner_Turns_It_On()
        {
            Assert.IsFalse(Features.Garden25DByDefault);
        }

        [Test]
        public void The_Slab_Is_A_Lawn_On_Top_And_Four_Thick_Earth_Sides()
        {
            SlabMesh slab = IslandShape.Slab(Island, 0.8d, 2d);
            Assert.AreEqual(20, slab.VertexCount);
            Assert.AreEqual(30, slab.Triangles.Length);
            Assert.AreEqual(slab.VertexCount * 2, slab.Uvs.Length);
            for (int v = 0; v < slab.VertexCount; v++)
            {
                double x = slab.Positions[v * 3], y = slab.Positions[v * 3 + 1], z = slab.Positions[v * 3 + 2];
                Assert.IsTrue(Island.Contains(new GroundPoint(x, z)));
                Assert.IsTrue(y == 0d || y == -0.8d, "every corner is on the lawn or at the slab's foot");
            }
        }

        [Test]
        public void The_Lawn_Faces_Up_And_Every_Side_Faces_Out()
        {
            SlabMesh slab = IslandShape.Slab(Island, 0.8d, 2d);
            GroundPoint centre = Island.Centre;
            int up = 0, outward = 0;
            for (int t = 0; t < slab.Triangles.Length; t += 3)
            {
                double[] a = At(slab, slab.Triangles[t]), b = At(slab, slab.Triangles[t + 1]), c = At(slab, slab.Triangles[t + 2]);
                double[] n = Cross(Sub(b, a), Sub(c, a));
                if (n[1] > 1e-9) { up++; continue; }
                Assert.AreEqual(0d, n[1], 1e-9, "a side stands straight");
                double mx = (a[0] + b[0] + c[0]) / 3d - centre.X, mz = (a[2] + b[2] + c[2]) / 3d - centre.Z;
                Assert.Greater(n[0] * mx + n[2] * mz, 0d, "triangle " + t / 3 + " faces out of the island");
                outward++;
            }
            Assert.AreEqual(2, up);
            Assert.AreEqual(8, outward);
            CollectionAssert.AreEqual(new[] { slab.Triangles[0], slab.Triangles[1], slab.Triangles[2], slab.Triangles[3], slab.Triangles[4], slab.Triangles[5] }, slab.LawnTriangles, "the lawn is its own submesh for the grass");
            Assert.AreEqual(24, slab.EarthTriangles.Length);
        }

        [Test]
        public void Grass_Repeats_Every_Grass_Tile_Across_The_Lawn()
        {
            SlabMesh slab = IslandShape.Slab(Island, 0.8d, 2d);
            for (int v = 0; v < 4; v++)
            {
                Assert.AreEqual(slab.Positions[v * 3] / 2d, slab.Uvs[v * 2], 1e-9);
                Assert.AreEqual(slab.Positions[v * 3 + 2] / 2d, slab.Uvs[v * 2 + 1], 1e-9);
            }
        }

        [Test]
        public void A_Flick_Travels_As_Far_As_Its_Glide_Adds_Up_To()
        {
            var spec = new CameraSpec();
            double v = 4d, travelled = 0d, dt = 1d / 2000d;
            while (v != 0d) { double next = GardenFraming.Glide(v, dt, spec, false); travelled += (v + next) / 2d * dt; v = next; }
            Assert.AreEqual(travelled, GardenFraming.GlideReach(4d, spec, false), travelled * 0.01d);
            Assert.AreEqual(-GardenFraming.GlideReach(4d, spec, false), GardenFraming.GlideReach(-4d, spec, false), 1e-12);
            Assert.AreEqual(0d, GardenFraming.GlideReach(spec.StopSpeed / 2d, spec, false));
            Assert.AreEqual(0d, GardenFraming.GlideReach(4d, spec, true));
        }

        static double[] At(SlabMesh m, int i) => new[] { m.Positions[i * 3], m.Positions[i * 3 + 1], m.Positions[i * 3 + 2] };
        static double[] Sub(double[] p, double[] q) => new[] { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
        static double[] Cross(double[] p, double[] q) => new[] { p[1] * q[2] - p[2] * q[1], p[2] * q[0] - p[0] * q[2], p[0] * q[1] - p[1] * q[0] };
    }
}

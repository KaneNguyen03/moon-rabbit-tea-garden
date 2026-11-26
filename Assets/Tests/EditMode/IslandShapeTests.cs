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
        public void The_Slab_Is_A_Lawn_On_Top_Four_Banded_Earth_Sides_And_An_Underside()
        {
            var bands = new SlabSpec();
            SlabMesh slab = IslandShape.Slab(Island, bands);
            Assert.AreEqual(4 + 4 * 3 * 4 + 4, slab.VertexCount);
            Assert.AreEqual(6 + 4 * 3 * 6 + 6, slab.Triangles.Length);
            Assert.AreEqual(slab.VertexCount * 2, slab.Uvs.Length);
            double[] heights = { 0d, -bands.Lip, -bands.Lip - bands.Gap, -bands.Thickness };
            for (int v = 0; v < slab.VertexCount; v++)
            {
                double x = slab.Positions[v * 3], y = slab.Positions[v * 3 + 1], z = slab.Positions[v * 3 + 2];
                Assert.IsTrue(Island.Contains(new GroundPoint(x, z)));
                Assert.IsTrue(System.Array.Exists(heights, h => Math.Abs(h - y) < 1e-12), "corner " + v + " sits on the lawn, a band line or the foot");
            }
        }

        [Test]
        public void The_Lawn_Faces_Up_The_Underside_Down_And_Every_Side_Faces_Out()
        {
            SlabMesh slab = IslandShape.Slab(Island, new SlabSpec());
            GroundPoint centre = Island.Centre;
            int up = 0, down = 0, outward = 0;
            for (int t = 0; t < slab.Triangles.Length; t += 3)
            {
                double[] n = Normal(slab, slab.Triangles, t);
                if (n[1] > 1e-9) { up++; continue; }
                if (n[1] < -1e-9) { down++; continue; }
                double[] a = At(slab, slab.Triangles[t]), b = At(slab, slab.Triangles[t + 1]), c = At(slab, slab.Triangles[t + 2]);
                double mx = (a[0] + b[0] + c[0]) / 3d - centre.X, mz = (a[2] + b[2] + c[2]) / 3d - centre.Z;
                Assert.Greater(n[0] * mx + n[2] * mz, 0d, "triangle " + t / 3 + " faces out of the island");
                outward++;
            }
            Assert.AreEqual(2, up);
            Assert.AreEqual(2, down);
            Assert.AreEqual(24, outward);
            Assert.AreEqual(6, slab.Part(SlabPart.Lawn).Length);
            Assert.AreEqual(6, slab.Part(SlabPart.Underside).Length);
            Assert.Greater(Normal(slab, slab.Part(SlabPart.Lawn), 0)[1], 0d, "the lawn is its own submesh for the grass");
            Assert.Less(Normal(slab, slab.Part(SlabPart.Underside), 0)[1], 0d);
            int parts = 0;
            foreach (SlabPart part in Enum.GetValues(typeof(SlabPart))) parts += slab.Part(part).Length;
            Assert.AreEqual(slab.Triangles.Length, parts, "every triangle belongs to exactly one part");
        }

        [Test]
        public void Each_Side_Has_The_Grass_Lip_On_Top_Then_The_Shadow_Gap_Then_Earth()
        {
            var bands = new SlabSpec();
            SlabMesh slab = IslandShape.Slab(Island, bands);
            AssertBand(slab, SlabPart.Lip, -bands.Lip, 0d);
            AssertBand(slab, SlabPart.Gap, -bands.Lip - bands.Gap, -bands.Lip);
            AssertBand(slab, SlabPart.Earth, -bands.Thickness, -bands.Lip - bands.Gap);
            AssertBand(slab, SlabPart.EarthShade, -bands.Thickness, -bands.Lip - bands.Gap);
            Assert.AreEqual(4 * 6, slab.Part(SlabPart.Lip).Length);
            Assert.AreEqual(4 * 6, slab.Part(SlabPart.Gap).Length);
        }

        [TestCase(-1d, -1d)]
        [TestCase(1d, -0.5d)]
        [TestCase(0d, 1d)]
        public void The_Earth_Facing_The_Moon_Is_Lit_And_The_Earth_Facing_Away_Is_In_Shade(double moonX, double moonZ)
        {
            SlabMesh slab = IslandShape.Slab(Island, new SlabSpec { MoonX = moonX, MoonZ = moonZ });
            int[] lit = slab.Part(SlabPart.Earth), shade = slab.Part(SlabPart.EarthShade);
            Assert.AreEqual(4 * 6, lit.Length + shade.Length);
            for (int t = 0; t < lit.Length; t += 3)
            {
                double[] n = Normal(slab, lit, t);
                Assert.GreaterOrEqual(n[0] * moonX + n[2] * moonZ, 0d);
            }
            for (int t = 0; t < shade.Length; t += 3)
            {
                double[] n = Normal(slab, shade, t);
                Assert.Less(n[0] * moonX + n[2] * moonZ, 0d);
            }
        }

        [Test]
        public void Under_The_Default_Moon_Up_And_To_The_Left_The_Front_Earth_Faces_The_Camera_Lit()
        {
            var bands = new SlabSpec();
            Assert.Less(bands.MoonX, 0d);
            SlabMesh slab = IslandShape.Slab(Island, bands);
            int[] lit = slab.Part(SlabPart.Earth);
            bool front = false;
            for (int t = 0; t < lit.Length; t += 3) front |= Normal(slab, lit, t)[2] < -1e-9;
            Assert.IsTrue(front, "the side nearest the tilted camera is the moon-lit earth");
        }

        [Test]
        public void Grass_Repeats_Every_Grass_Tile_Across_The_Lawn()
        {
            SlabMesh slab = IslandShape.Slab(Island, new SlabSpec { GrassMetres = 2d });
            int[] lawn = slab.Part(SlabPart.Lawn);
            foreach (int v in lawn)
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

        static void AssertBand(SlabMesh slab, SlabPart part, double low, double high)
        {
            foreach (int v in slab.Part(part))
            {
                double y = slab.Positions[v * 3 + 1];
                Assert.IsTrue(y >= low - 1e-12 && y <= high + 1e-12, part + " corner at " + y + " is outside " + low + ".." + high);
            }
        }

        static double[] Normal(SlabMesh m, int[] triangles, int t)
        {
            double[] a = At(m, triangles[t]), b = At(m, triangles[t + 1]), c = At(m, triangles[t + 2]);
            return Cross(Sub(b, a), Sub(c, a));
        }

        static double[] At(SlabMesh m, int i) => new[] { m.Positions[i * 3], m.Positions[i * 3 + 1], m.Positions[i * 3 + 2] };
        static double[] Sub(double[] p, double[] q) => new[] { p[0] - q[0], p[1] - q[1], p[2] - q[2] };
        static double[] Cross(double[] p, double[] q) => new[] { p[1] * q[2] - p[2] * q[1], p[2] * q[0] - p[0] * q[2], p[0] * q[1] - p[1] * q[0] };
    }
}

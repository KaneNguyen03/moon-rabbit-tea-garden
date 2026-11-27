using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum SlabPart { Lawn, Lip, Gap, Earth, EarthShade, Underside }

    public sealed class SlabSpec
    {
        public double Thickness = 0.8d;
        public double Lip = 0.12d;
        public double Gap = 0.05d;
        public double GrassMetres = 2d;
        public double MoonX = -1d;
        public double MoonZ = -1d;
        public double Radius;
        public double Wave;
    }

    public sealed class SlabMesh
    {
        public static readonly int PartCount = Enum.GetValues(typeof(SlabPart)).Length;

        public readonly double[] Positions;
        public readonly double[] Uvs;
        public readonly int[] Triangles;
        readonly int[][] _parts;

        public SlabMesh(double[] positions, double[] uvs, int[][] parts)
        {
            Positions = positions;
            Uvs = uvs;
            _parts = parts;
            var all = new List<int>();
            foreach (int[] part in parts) all.AddRange(part);
            Triangles = all.ToArray();
        }

        public int VertexCount => Positions.Length / 3;

        public int[] Part(SlabPart part) => _parts[(int)part];
    }

    public static class IslandShape
    {
        public const int ArcSteps = 6, WaveSteps = 12;

        public static GroundRect Shore(GroundRect island, double radius) =>
            radius <= 0d ? island : GardenFraming.Pad(island, radius * (1d - Math.Sqrt(0.5d)));

        public static List<GroundPoint> Outline(GroundRect island, double radius, double wave)
        {
            double r = Math.Max(0d, Math.Min(radius, Math.Min(island.Width, island.Depth) / 2d));
            var ring = new List<GroundPoint>();
            void Arc(double cx, double cz, double fromDegrees)
            {
                if (r <= 0d)
                {
                    ring.Add(new GroundPoint(cx, cz));
                    return;
                }
                for (int k = 0; k <= ArcSteps; k++)
                {
                    double a = (fromDegrees + 90d * k / ArcSteps) * Math.PI / 180d;
                    ring.Add(new GroundPoint(cx + r * Math.Cos(a), cz + r * Math.Sin(a)));
                }
            }
            Arc(island.MinX + r, island.MinZ + r, 180d);
            if (wave != 0d)
                for (int k = 1; k < WaveSteps; k++)
                {
                    double t = (double)k / WaveSteps;
                    double x = island.MinX + r + (island.Width - 2d * r) * t;
                    ring.Add(new GroundPoint(x, island.MinZ + wave * Math.Sin(2d * Math.PI * t)));
                }
            Arc(island.MaxX - r, island.MinZ + r, 270d);
            Arc(island.MaxX - r, island.MaxZ - r, 0d);
            Arc(island.MinX + r, island.MaxZ - r, 90d);
            return ring;
        }

        public static SlabMesh Slab(GroundRect island, SlabSpec spec)
        {
            island = Shore(island, spec.Radius);
            List<GroundPoint> ring = Outline(island, spec.Radius, spec.Wave);
            var positions = new List<double>();
            var uvs = new List<double>();
            var parts = new List<int>[SlabMesh.PartCount];
            for (int p = 0; p < parts.Length; p++) parts[p] = new List<int>();
            double foot = -spec.Thickness;
            double lipFoot = Math.Max(foot, -spec.Lip);
            double gapFoot = Math.Max(foot, -spec.Lip - spec.Gap);

            int Corner(double x, double y, double z, double u, double w)
            {
                positions.Add(x); positions.Add(y); positions.Add(z);
                uvs.Add(u / spec.GrassMetres); uvs.Add(w / spec.GrassMetres);
                return positions.Count / 3 - 1;
            }

            void Triangle(SlabPart part, int a, int b, int c)
            {
                List<int> into = parts[(int)part];
                into.Add(a); into.Add(b); into.Add(c);
            }

            void Band(GroundPoint left, GroundPoint right, double fromU, double toU, double top, double bottom, SlabPart part)
            {
                int a = Corner(left.X, bottom, left.Z, fromU, bottom);
                int b = Corner(left.X, top, left.Z, fromU, top);
                int c = Corner(right.X, top, right.Z, toU, top);
                int d = Corner(right.X, bottom, right.Z, toU, bottom);
                Triangle(part, a, b, c);
                Triangle(part, a, c, d);
            }

            void Cap(double y, SlabPart part, bool up)
            {
                bool star = spec.Wave != 0d;
                int hub = star ? Corner(island.Centre.X, y, island.Centre.Z, island.Centre.X, island.Centre.Z) : -1;
                int first = positions.Count / 3;
                foreach (GroundPoint p in ring) Corner(p.X, y, p.Z, p.X, p.Z);
                int n = ring.Count;
                for (int i = star ? 0 : 1; i < (star ? n : n - 1); i++)
                {
                    int a = star ? hub : first, b = first + i, c = first + (i + 1) % n;
                    if (up) Triangle(part, a, c, b);
                    else Triangle(part, a, b, c);
                }
            }

            Cap(0d, SlabPart.Lawn, true);
            double around = 0d;
            for (int i = 0; i < ring.Count; i++)
            {
                GroundPoint left = ring[i], right = ring[(i + 1) % ring.Count];
                double dx = right.X - left.X, dz = right.Z - left.Z, along = Math.Sqrt(dx * dx + dz * dz);
                if (along <= 0d) continue;
                bool lit = dz * spec.MoonX - dx * spec.MoonZ >= 0d;
                Band(left, right, around, around + along, 0d, lipFoot, SlabPart.Lip);
                Band(left, right, around, around + along, lipFoot, gapFoot, SlabPart.Gap);
                Band(left, right, around, around + along, gapFoot, foot, lit ? SlabPart.Earth : SlabPart.EarthShade);
                around += along;
            }
            Cap(foot, SlabPart.Underside, false);

            var built = new int[parts.Length][];
            for (int p = 0; p < parts.Length; p++) built[p] = parts[p].ToArray();
            return new SlabMesh(positions.ToArray(), uvs.ToArray(), built);
        }
    }
}

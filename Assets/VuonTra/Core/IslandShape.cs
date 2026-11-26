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
        public static SlabMesh Slab(GroundRect island, SlabSpec spec)
        {
            var positions = new List<double>();
            var uvs = new List<double>();
            var parts = new List<int>[SlabMesh.PartCount];
            for (int p = 0; p < parts.Length; p++) parts[p] = new List<int>();
            double foot = -spec.Thickness;
            double lipFoot = Math.Max(foot, -spec.Lip);
            double gapFoot = Math.Max(foot, -spec.Lip - spec.Gap);

            void Corner(double x, double y, double z, double u, double w)
            {
                positions.Add(x); positions.Add(y); positions.Add(z);
                uvs.Add(u / spec.GrassMetres); uvs.Add(w / spec.GrassMetres);
            }

            void Quad(SlabPart part)
            {
                int first = positions.Count / 3 - 4;
                List<int> into = parts[(int)part];
                into.Add(first); into.Add(first + 1); into.Add(first + 2);
                into.Add(first); into.Add(first + 2); into.Add(first + 3);
            }

            void Band(double leftX, double leftZ, double rightX, double rightZ, double along, double top, double bottom, SlabPart part)
            {
                Corner(leftX, bottom, leftZ, 0d, bottom);
                Corner(leftX, top, leftZ, 0d, top);
                Corner(rightX, top, rightZ, along, top);
                Corner(rightX, bottom, rightZ, along, bottom);
                Quad(part);
            }

            void Side(double leftX, double leftZ, double rightX, double rightZ, double along, double outX, double outZ)
            {
                bool lit = outX * spec.MoonX + outZ * spec.MoonZ >= 0d;
                Band(leftX, leftZ, rightX, rightZ, along, 0d, lipFoot, SlabPart.Lip);
                Band(leftX, leftZ, rightX, rightZ, along, lipFoot, gapFoot, SlabPart.Gap);
                Band(leftX, leftZ, rightX, rightZ, along, gapFoot, foot, lit ? SlabPart.Earth : SlabPart.EarthShade);
            }

            Corner(island.MinX, 0d, island.MinZ, island.MinX, island.MinZ);
            Corner(island.MinX, 0d, island.MaxZ, island.MinX, island.MaxZ);
            Corner(island.MaxX, 0d, island.MaxZ, island.MaxX, island.MaxZ);
            Corner(island.MaxX, 0d, island.MinZ, island.MaxX, island.MinZ);
            Quad(SlabPart.Lawn);
            Side(island.MinX, island.MinZ, island.MaxX, island.MinZ, island.Width, 0d, -1d);
            Side(island.MaxX, island.MaxZ, island.MinX, island.MaxZ, island.Width, 0d, 1d);
            Side(island.MinX, island.MaxZ, island.MinX, island.MinZ, island.Depth, -1d, 0d);
            Side(island.MaxX, island.MinZ, island.MaxX, island.MaxZ, island.Depth, 1d, 0d);
            Corner(island.MinX, foot, island.MinZ, island.MinX, island.MinZ);
            Corner(island.MaxX, foot, island.MinZ, island.MaxX, island.MinZ);
            Corner(island.MaxX, foot, island.MaxZ, island.MaxX, island.MaxZ);
            Corner(island.MinX, foot, island.MaxZ, island.MinX, island.MaxZ);
            Quad(SlabPart.Underside);

            var built = new int[parts.Length][];
            for (int p = 0; p < parts.Length; p++) built[p] = parts[p].ToArray();
            return new SlabMesh(positions.ToArray(), uvs.ToArray(), built);
        }
    }
}

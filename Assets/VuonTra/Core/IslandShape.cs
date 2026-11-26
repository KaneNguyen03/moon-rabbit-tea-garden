namespace VuonTra.Core
{
    public sealed class SlabMesh
    {
        public readonly double[] Positions;
        public readonly double[] Uvs;
        public readonly int[] Triangles;
        public readonly int[] LawnTriangles;
        public readonly int[] EarthTriangles;

        public SlabMesh(double[] positions, double[] uvs, int[] triangles, int lawnIndices)
        {
            Positions = positions;
            Uvs = uvs;
            Triangles = triangles;
            LawnTriangles = new int[lawnIndices];
            EarthTriangles = new int[triangles.Length - lawnIndices];
            System.Array.Copy(triangles, LawnTriangles, lawnIndices);
            System.Array.Copy(triangles, lawnIndices, EarthTriangles, 0, EarthTriangles.Length);
        }

        public int VertexCount => Positions.Length / 3;
    }

    public static class IslandShape
    {
        public static SlabMesh Slab(GroundRect island, double thickness, double grassMetres)
        {
            var positions = new double[20 * 3];
            var uvs = new double[20 * 2];
            var triangles = new int[30];
            int v = 0, t = 0;
            double foot = -thickness;

            void Corner(double x, double y, double z, double u, double w)
            {
                positions[v * 3] = x; positions[v * 3 + 1] = y; positions[v * 3 + 2] = z;
                uvs[v * 2] = u / grassMetres; uvs[v * 2 + 1] = w / grassMetres;
                v++;
            }

            void Quad()
            {
                int first = v - 4;
                triangles[t++] = first; triangles[t++] = first + 1; triangles[t++] = first + 2;
                triangles[t++] = first; triangles[t++] = first + 2; triangles[t++] = first + 3;
            }

            void Side(double leftX, double leftZ, double rightX, double rightZ, double along)
            {
                Corner(leftX, foot, leftZ, 0d, foot);
                Corner(leftX, 0d, leftZ, 0d, 0d);
                Corner(rightX, 0d, rightZ, along, 0d);
                Corner(rightX, foot, rightZ, along, foot);
                Quad();
            }

            Corner(island.MinX, 0d, island.MinZ, island.MinX, island.MinZ);
            Corner(island.MinX, 0d, island.MaxZ, island.MinX, island.MaxZ);
            Corner(island.MaxX, 0d, island.MaxZ, island.MaxX, island.MaxZ);
            Corner(island.MaxX, 0d, island.MinZ, island.MaxX, island.MinZ);
            Quad();
            Side(island.MinX, island.MinZ, island.MaxX, island.MinZ, island.Width);
            Side(island.MaxX, island.MaxZ, island.MinX, island.MaxZ, island.Width);
            Side(island.MinX, island.MaxZ, island.MinX, island.MinZ, island.Depth);
            Side(island.MaxX, island.MinZ, island.MaxX, island.MaxZ, island.Depth);
            return new SlabMesh(positions, uvs, triangles, 6);
        }
    }
}

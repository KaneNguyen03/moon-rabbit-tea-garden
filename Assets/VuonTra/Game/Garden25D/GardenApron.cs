using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    public readonly struct ApronBand
    {
        public readonly double Sides, Back;

        public ApronBand(double sides, double back)
        {
            Sides = sides;
            Back = back;
        }
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
    public sealed class GardenApron : MonoBehaviour
    {
        public const float Below = 0.01f, Under = 0.005f, TuckUnder = 0.05f;

        readonly List<Vector3> _corners = new List<Vector3>();
        readonly List<Vector2> _uvs = new List<Vector2>();
        readonly List<int> _band = new List<int>();
        readonly List<int> _outer = new List<int>();
        readonly List<int> _front = new List<int>();
        Mesh _mesh;

        public GroundRect Area { get; private set; }
        public GroundRect BandArea { get; private set; }
        public GroundRect FrontArea { get; private set; }
        public float Top { get; private set; }

        public void Build(GroundRect island, SlabSpec slab, ApronBand band, double reach, Material bandPaint, Material outerPaint, Material frontPaint)
        {
            GroundRect shore = IslandShape.Shore(island, slab.Radius);
            BandArea = new GroundRect(shore.MinX - band.Sides, shore.MaxX + band.Sides, shore.MinZ, shore.MaxZ + band.Back);
            Area = GardenFraming.Pad(shore, reach);
            FrontArea = new GroundRect(Area.MinX, Area.MaxX, Area.MinZ, shore.MinZ + System.Math.Abs(slab.Wave) + TuckUnder);
            Top = -(float)slab.Thickness - Below;
            float tile = (float)slab.GrassMetres;
            _corners.Clear();
            _uvs.Clear();
            _band.Clear();
            _outer.Clear();
            _front.Clear();

            Quad(FrontArea, Top, tile, _front);

            float bandLevel = Top - Under;
            var ring = IslandShape.Outline(new GroundRect(BandArea.MinX, BandArea.MaxX, shore.MinZ - band.Sides, BandArea.MaxZ), slab.Radius + band.Sides, 0d);
            int hub = Add(BandArea.Centre.X, bandLevel, BandArea.Centre.Z, tile);
            for (int i = 0; i < ring.Count; i++) Add(ring[i].X, bandLevel, ring[i].Z, tile);
            for (int i = 0; i < ring.Count; i++)
            {
                _band.Add(hub);
                _band.Add(hub + 1 + (i + 1) % ring.Count);
                _band.Add(hub + 1 + i);
            }

            float low = Top - 2f * Under;
            Quad(Area, low, tile, _outer);

            if (_mesh == null) _mesh = new Mesh { name = "Locked Land" };
            _mesh.Clear();
            _mesh.SetVertices(_corners);
            _mesh.SetUVs(0, _uvs);
            _mesh.subMeshCount = 3;
            _mesh.SetTriangles(_band, 0);
            _mesh.SetTriangles(_outer, 1);
            _mesh.SetTriangles(_front, 2);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterials = new[] { bandPaint, outerPaint, frontPaint };
            var box = GetComponent<BoxCollider>();
            box.center = new Vector3((float)Area.Centre.X, low, (float)Area.Centre.Z);
            box.size = new Vector3((float)Area.Width, 0.01f, (float)Area.Depth);
        }

        void Quad(GroundRect area, float y, float tile, List<int> into)
        {
            int a = Add(area.MinX, y, area.MinZ, tile);
            Add(area.MinX, y, area.MaxZ, tile);
            Add(area.MaxX, y, area.MaxZ, tile);
            Add(area.MaxX, y, area.MinZ, tile);
            into.Add(a); into.Add(a + 1); into.Add(a + 2);
            into.Add(a); into.Add(a + 2); into.Add(a + 3);
        }

        int Add(double x, float y, double z, float tile)
        {
            _corners.Add(new Vector3((float)x, y, (float)z));
            _uvs.Add(new Vector2((float)x / tile, (float)z / tile));
            return _corners.Count - 1;
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}

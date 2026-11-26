using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider))]
    public sealed class GardenApron : MonoBehaviour
    {
        public const float Below = 0.01f;

        Mesh _mesh;

        public GroundRect Area { get; private set; }
        public float Top { get; private set; }

        public void Build(GroundRect island, SlabSpec slab, double reach, Material paint)
        {
            Area = GardenFraming.Pad(island, reach);
            Top = -(float)slab.Thickness - Below;
            float minX = (float)Area.MinX, maxX = (float)Area.MaxX, minZ = (float)Area.MinZ, maxZ = (float)Area.MaxZ, tile = (float)slab.GrassMetres;
            if (_mesh == null) _mesh = new Mesh { name = "Locked Land" };
            _mesh.Clear();
            _mesh.SetVertices(new[] { new Vector3(minX, Top, minZ), new Vector3(minX, Top, maxZ), new Vector3(maxX, Top, maxZ), new Vector3(maxX, Top, minZ) });
            _mesh.SetUVs(0, new[] { new Vector2(minX / tile, minZ / tile), new Vector2(minX / tile, maxZ / tile), new Vector2(maxX / tile, maxZ / tile), new Vector2(maxX / tile, minZ / tile) });
            _mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterial = paint;
            var box = GetComponent<BoxCollider>();
            box.center = new Vector3((minX + maxX) / 2f, Top, (minZ + maxZ) / 2f);
            box.size = new Vector3(maxX - minX, 0.01f, maxZ - minZ);
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}

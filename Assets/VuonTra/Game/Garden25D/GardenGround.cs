using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VuonTra.Core;

namespace VuonTra.Game
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class GardenGround : MonoBehaviour, IPointerClickHandler
    {
        CameraSpec _spec;
        int _segments;
        Mesh _mesh;

        public event Action<Hex> TileTapped;

        public void Build(int segments, CameraSpec spec, float thickness, float grassMetres, Material lawn, Material earth)
        {
            _spec = spec;
            _segments = segments;
            SlabMesh slab = IslandShape.Slab(GardenFraming.Island(segments, spec), thickness, grassMetres);
            var corners = new Vector3[slab.VertexCount];
            var uvs = new Vector2[slab.VertexCount];
            for (int v = 0; v < slab.VertexCount; v++)
            {
                corners[v] = new Vector3((float)slab.Positions[v * 3], (float)slab.Positions[v * 3 + 1], (float)slab.Positions[v * 3 + 2]);
                uvs[v] = new Vector2((float)slab.Uvs[v * 2], (float)slab.Uvs[v * 2 + 1]);
            }
            Mesh old = _mesh;
            _mesh = new Mesh { name = "Island" };
            _mesh.SetVertices(corners);
            _mesh.SetUVs(0, uvs);
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(slab.LawnTriangles, 0);
            _mesh.SetTriangles(slab.EarthTriangles, 1);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterials = new[] { lawn, earth };
            GetComponent<MeshCollider>().sharedMesh = _mesh;
            if (old != null) Destroy(old);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector3 at = eventData.pointerCurrentRaycast.worldPosition;
            Hex tile = GardenFraming.TileAt(new GroundPoint(at.x, at.z), _spec);
            int segment = GardenGrid.SegmentOf(tile);
            if (segment >= 0 && segment <= _segments) TileTapped?.Invoke(tile);
        }
    }
}

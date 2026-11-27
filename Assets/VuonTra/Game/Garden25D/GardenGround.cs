using System;
using UnityEngine;
using UnityEngine.EventSystems;
using VuonTra.Core;

namespace VuonTra.Game
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public sealed class GardenGround : MonoBehaviour
    {
        CameraSpec _spec;
        int _segments;
        Mesh _mesh;

        public event Action<Hex> TileTapped;
        public event Action Touched;

        public void Build(int segments, CameraSpec spec, SlabSpec bands, Material[] paints)
        {
            _spec = spec;
            _segments = segments;
            SlabMesh slab = IslandShape.Slab(GardenFraming.Island(segments, spec), bands);
            var corners = new Vector3[slab.VertexCount];
            var uvs = new Vector2[slab.VertexCount];
            for (int v = 0; v < slab.VertexCount; v++)
            {
                corners[v] = new Vector3((float)slab.Positions[v * 3], (float)slab.Positions[v * 3 + 1], (float)slab.Positions[v * 3 + 2]);
                uvs[v] = new Vector2((float)slab.Uvs[v * 2], (float)slab.Uvs[v * 2 + 1]);
            }
            if (_mesh == null) _mesh = new Mesh { name = "Island" };
            _mesh.Clear();
            _mesh.SetVertices(corners);
            _mesh.SetUVs(0, uvs);
            _mesh.subMeshCount = SlabMesh.PartCount;
            for (int part = 0; part < SlabMesh.PartCount; part++) _mesh.SetTriangles(slab.Part((SlabPart)part), part);
            _mesh.RecalculateNormals();
            _mesh.RecalculateBounds();
            GetComponent<MeshFilter>().sharedMesh = _mesh;
            GetComponent<MeshRenderer>().sharedMaterials = paints;
            var collider = GetComponent<MeshCollider>();
            collider.sharedMesh = null;
            collider.sharedMesh = _mesh;
        }

        public void Tap(PointerEventData eventData)
        {
            Touched?.Invoke();
            Vector3 at = eventData.pointerCurrentRaycast.worldPosition;
            Hex tile = GardenFraming.TileAt(new GroundPoint(at.x, at.z), _spec);
            int segment = GardenGrid.SegmentOf(tile);
            if (segment >= 0 && segment <= _segments) TileTapped?.Invoke(tile);
        }

        void OnDestroy()
        {
            if (_mesh != null) Destroy(_mesh);
        }
    }
}

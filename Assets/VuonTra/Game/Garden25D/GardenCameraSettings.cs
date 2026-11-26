using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [CreateAssetMenu(menuName = "VuonTra/Garden Camera Settings", fileName = "GardenCameraSettings")]
    public sealed class GardenCameraSettings : ScriptableObject
    {
        [SerializeField, Range(10f, 60f)] float fovDegrees = 25f;
        [SerializeField, Range(20f, 70f)] float tiltDegrees = 40f;
        [SerializeField, Min(1f)] float defaultDistance = 35f;
        [SerializeField, Min(0f)] float edgeGive = 0.6f;
        [SerializeField, Min(0.05f)] float glideFadeSeconds = 0.35f;
        [SerializeField, Min(0f)] float stopSpeed = 0.05f;
        [SerializeField, Min(0.1f)] float islandThickness = 0.8f;
        [SerializeField, Min(0.25f)] float grassTileMetres = 2f;

        public float IslandThickness => islandThickness;
        public float GrassTileMetres => grassTileMetres;

        public CameraSpec Spec() => new CameraSpec
        {
            FovDegrees = fovDegrees,
            TiltDegrees = tiltDegrees,
            DefaultDistance = defaultDistance,
            EdgeGive = edgeGive,
            GlideFadeSeconds = glideFadeSeconds,
            StopSpeed = stopSpeed,
        };
    }
}

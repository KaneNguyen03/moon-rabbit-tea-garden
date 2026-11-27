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
        [SerializeField, Min(0f)] float grassLipMetres = 0.12f;
        [SerializeField, Min(0f)] float lipShadowMetres = 0.05f;
        [SerializeField] Vector2 moonFrom = new Vector2(-1f, -1f);
        [SerializeField] Color grassLip;
        [SerializeField] Color lipShadow;
        [SerializeField] Color earthLit;
        [SerializeField] Color earthShade;
        [SerializeField] Color underside;
        [SerializeField, Min(0f)] float islandCornerMetres = 4f;
        [SerializeField, Min(0f)] float frontWaveMetres = 0.4f;
        [SerializeField, Range(0f, 0.5f)] float frontEdgeShare = 0.15f;
        [SerializeField, Min(0f)] float lockedReach = 60f;
        [SerializeField, Min(0f)] float lockedBandMetres = 1.5f;
        [SerializeField, Min(0f)] float lockedBackMetres = 2.5f;
        [SerializeField] Color lockedBand = new Color(0.19216f, 0.37647f, 0.36078f, 1f);
        [SerializeField] Color lockedGrass = new Color(0.13725f, 0.21569f, 0.30588f, 1f);

        public double LockedReach => lockedReach;
        public double LockedBandMetres => lockedBandMetres;
        public double LockedBackMetres => lockedBackMetres;
        public Color LockedBand => lockedBand;
        public Color LockedGrass => lockedGrass;

        public CameraSpec Spec() => new CameraSpec
        {
            FovDegrees = fovDegrees,
            TiltDegrees = tiltDegrees,
            DefaultDistance = defaultDistance,
            EdgeGive = edgeGive,
            GlideFadeSeconds = glideFadeSeconds,
            StopSpeed = stopSpeed,
            FrontEdgeShare = frontEdgeShare,
        };

        public SlabSpec Slab() => new SlabSpec
        {
            Thickness = islandThickness,
            Lip = grassLipMetres,
            Gap = lipShadowMetres,
            GrassMetres = grassTileMetres,
            MoonX = moonFrom.x,
            MoonZ = moonFrom.y,
            Radius = islandCornerMetres,
            Wave = frontWaveMetres,
        };

        public Color Tint(SlabPart part) => part switch
        {
            SlabPart.Lip => grassLip,
            SlabPart.Gap => lipShadow,
            SlabPart.Earth => earthLit,
            SlabPart.EarthShade => earthShade,
            SlabPart.Underside => underside,
            _ => Color.white,
        };
    }
}

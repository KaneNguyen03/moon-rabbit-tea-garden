using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [Serializable]
    public sealed class StationStand
    {
        public string Id;
        public StandRole Role = StandRole.Station;
        [Min(0f)] public float Height = 1f;
        public Sprite Art;
        public RabbitWork Work;
        public Vector2 RabbitSpot = new Vector2(-0.45f, -0.3f);
    }

    [Serializable]
    public sealed class BreedRig
    {
        public string Kind;
        public RabbitActor Rig;
    }

    [Serializable]
    public sealed class DressingPiece
    {
        public Sprite Art;
        public StandRole Role = StandRole.Dressing;
        [Min(0f)] public float Height = 0.5f;
        public Vector2 Offset;
    }

    [CreateAssetMenu(menuName = "VuonTra/Garden Stand Settings", fileName = "GardenStands")]
    public sealed class GardenStandSettings : ScriptableObject
    {
        [SerializeField, Min(0.1f)] float rabbitMetres = 1.15f;
        [SerializeField] BreedRig[] rigs = new BreedRig[0];
        [Header("Contact shadow under every standing piece")]
        [SerializeField] Sprite shadow;
        [SerializeField] Color shadowTint = Color.white;
        [SerializeField, Range(0.1f, 2f)] float shadowShare = 0.9f;
        [SerializeField, Min(0f)] float shadowMinWidth = 0.3f;
        [SerializeField, Min(0.05f)] float shadowDepth = 0.55f;
        [SerializeField] Vector2 shadowFall = new Vector2(0.04f, 0.03f);
        [Header("Flat base under every station")]
        [SerializeField] Sprite[] pads = new Sprite[0];
        [SerializeField] Color padTint = Color.white;
        [SerializeField, Min(0.5f)] float padShare = 1.5f;
        [SerializeField, Min(0f)] float padMinWidth = 1.4f;
        [SerializeField, Min(0.05f)] float padDepth = 0.85f;
        [Header("Around open stations and along the paths")]
        [SerializeField] DressingPiece[] dressing = new DressingPiece[0];
        [SerializeField] Sprite lampPost;
        [SerializeField, Min(0f)] float lampPostHeight = 1.4f;
        [SerializeField, Min(0f)] float lampPostAside = 0.9f;
        [SerializeField] Sprite stone;
        [SerializeField] Color stoneTint = Color.white;
        [SerializeField, Min(0.05f)] float stoneWidth = 0.3f;
        [SerializeField, Min(0.1f)] float stoneStep = 0.45f;
        [SerializeField, Min(0f)] float stoneClear = 1.1f;
        [SerializeField, Min(0f)] float keepClear = 1.1f;
        [SerializeField] Color unbuiltTint = new Color(0.55f, 0.5f, 0.62f, 0.65f);
        [SerializeField] StationStand[] stations = new StationStand[0];

        static readonly StationStand Usual = new StationStand();

        public double RabbitMetres => rabbitMetres;
        public BreedRig[] Rigs => rigs;

        public RabbitActor RigFor(string kind)
        {
            if (string.IsNullOrEmpty(kind)) return null;
            foreach (BreedRig rig in rigs)
                if (rig.Kind == kind && rig.Rig != null) return rig.Rig;
            return null;
        }
        public Sprite ShadowSprite => shadow;
        public Color ShadowTint => shadowTint;
        public float ShadowShare => shadowShare;
        public float ShadowMinWidth => shadowMinWidth;
        public float ShadowDepth => shadowDepth;
        public Vector2 ShadowFall => shadowFall;
        public Color PadTint => padTint;
        public float PadShare => padShare;
        public float PadMinWidth => padMinWidth;
        public float PadDepth => padDepth;
        public DressingPiece[] Dressing => dressing;
        public Sprite LampPost => lampPost;
        public float LampPostHeight => lampPostHeight;
        public float LampPostAside => lampPostAside;
        public Sprite Stone => stone;
        public Color StoneTint => stoneTint;
        public float StoneWidth => stoneWidth;
        public float StoneStep => stoneStep;
        public float StoneClear => stoneClear;
        public float KeepClear => keepClear;
        public Color UnbuiltTint => unbuiltTint;

        public Sprite PadFor(int index) => pads == null || pads.Length == 0 ? null : pads[index % pads.Length];

        public StationStand Station(string id)
        {
            foreach (StationStand stand in stations)
                if (stand.Id == id) return stand;
            return Usual;
        }

        public double Metres(StationStand stand) => GardenStands.Metres(stand.Role, stand.Height, rabbitMetres);
    }
}

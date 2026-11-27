using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.U2D.Animation;
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
        public PropLayer[] Layers = new PropLayer[0];
        public Sprite Dish;
        public Vector3 DishSpot;
        [Min(0f)] public float DishHeight = 0.35f;
        [Min(0f)] public float DishFrom;
        public ParticleBeat[] Particles = new ParticleBeat[0];
        public SpriteLibraryAsset Library;
        public RuntimeAnimatorController Loop;
    }

    [Serializable]
    public sealed class PropLayer
    {
        public string Category = StandLoop.ArtCategory;
        public Vector3 Spot;
        [Min(0f)] public float Height = 0.5f;
        public PropState[] States = new PropState[0];
        public StateBeat[] Beats = new StateBeat[0];
    }

    [Serializable]
    public sealed class ParticleLook
    {
        public Sprite Art;
        public Material Look;
    }

    [Serializable]
    public sealed class PropState
    {
        public string Label;
        public Sprite Art;
    }

    [Serializable]
    public sealed class StateBeat
    {
        [Min(0f)] public float At;
        public string Label;
    }

    [Serializable]
    public sealed class ParticleBeat
    {
        public Sprite Art;
        [Min(0f)] public float At;
        [Min(1)] public int Count = 3;
        public Vector3 From;
        [Min(0.05f)] public float Life = 0.8f;
        [Min(0f)] public float Speed = 0.6f;
        [Min(0.01f)] public float Size = 0.1f;
        public float Gravity = 0.5f;
    }

    [Serializable]
    public sealed class BreedRig
    {
        public string Kind;
        public RabbitActor Rig;
    }

    [Serializable]
    public sealed class FixtureStand
    {
        public GardenFixture Fixture;
        public StandRole Role = StandRole.Station;
        [Min(0f)] public float Height = 1f;
        public Sprite Art;
        public Color Tint = Color.white;
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
        [SerializeField, Range(0.1f, 2f)] float shadowShare = 1.3f;
        [SerializeField, Min(0f)] float shadowMinWidth = 0.3f;
        [SerializeField, Min(0.05f)] float shadowDepth = 0.55f;
        [SerializeField, Range(0f, 1f), FormerlySerializedAs("shadowForward")] float shadowInFront = 0.35f;
        [SerializeField, Range(0f, 1f)] float padShadowAlpha = 0.45f;
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
        [SerializeField, Min(0f)] float lampPostShadow = 0.35f;
        [SerializeField, Min(0f)] float lampKeepClear = 1.8f;
        [SerializeField] Sprite pathPiece;
        [SerializeField, Min(0.05f)] float pathWidth = 0.55f;
        [SerializeField, Range(0.1f, 1f)] float pathBandShare = 0.816f;
        [SerializeField, Min(0f)] float keepClear = 1.1f;
        [Header("Mortar, shop, fish tank and the trees behind the island")]
        [SerializeField] FixtureStand[] fixtures = new FixtureStand[0];
        [SerializeField] Sprite tree;
        [SerializeField, Min(0f)] float treeHeight = 3f;
        [SerializeField, Min(0.5f)] float treeSpacing = 3f;
        [SerializeField, Min(0f)] float treeBehind = 1.2f;
        [SerializeField] ParticleLook[] particleLooks = new ParticleLook[0];
        [SerializeField, Min(0.5f)] float loopSeconds = 3.28f;
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
        public float ShadowInFront => shadowInFront;
        public float PadShadowAlpha => padShadowAlpha;
        public Color PadTint => padTint;
        public float PadShare => padShare;
        public float PadMinWidth => padMinWidth;
        public float PadDepth => padDepth;
        public DressingPiece[] Dressing => dressing;
        public Sprite LampPost => lampPost;
        public float LampPostHeight => lampPostHeight;
        public float LampPostAside => lampPostAside;
        public float LampPostShadow => lampPostShadow;
        public float LampKeepClear => lampKeepClear;
        public Sprite PathPiece => pathPiece;
        public float PathWidth => pathWidth;
        public float PathSpriteDepth => pathWidth / pathBandShare;
        public float PathPieceLength => pathPiece == null ? 0f : PathSpriteDepth * pathPiece.bounds.size.x / pathPiece.bounds.size.y;
        public float KeepClear => keepClear;
        public Color UnbuiltTint => unbuiltTint;
        public ParticleLook[] ParticleLooks => particleLooks;

        public Material ParticleLookFor(Sprite art)
        {
            foreach (ParticleLook look in particleLooks)
                if (look.Art == art) return look.Look;
            return null;
        }
        public float LoopSeconds => loopSeconds;
        public StationStand[] Stations => stations;
        public Sprite Tree => tree;
        public float TreeHeight => treeHeight;
        public float TreeSpacing => treeSpacing;
        public float TreeBehind => treeBehind;

        public FixtureStand FixtureFor(GardenFixture fixture)
        {
            foreach (FixtureStand stand in fixtures)
                if (stand.Fixture == fixture) return stand;
            return new FixtureStand { Fixture = fixture };
        }

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

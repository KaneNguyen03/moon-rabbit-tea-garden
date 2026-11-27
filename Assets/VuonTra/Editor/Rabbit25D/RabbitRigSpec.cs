using System.Collections.Generic;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.EditorTools
{
    public readonly struct RigBone
    {
        public readonly string Name;
        public readonly string Parent;
        public readonly Vector2 From;
        public readonly Vector2 To;

        public RigBone(string name, string parent, Vector2 from, Vector2 to)
        {
            Name = name;
            Parent = parent;
            From = from;
            To = to;
        }
    }

    public readonly struct RigPart
    {
        public readonly string Layer;
        public readonly int Order;
        public readonly string Bone;
        public readonly string LowerBone;
        public readonly float BlendFrom;
        public readonly float BlendTo;
        public readonly string UpperBone;
        public readonly float UpperFrom;

        public RigPart(string layer, int order, string bone, string lowerBone = null, float blendFrom = 0f, float blendTo = 0f,
            string upperBone = null, float upperFrom = 0f)
        {
            Layer = layer;
            Order = order;
            Bone = bone;
            LowerBone = lowerBone;
            BlendFrom = blendFrom;
            BlendTo = blendTo;
            UpperBone = upperBone;
            UpperFrom = upperFrom;
        }
    }

    public readonly struct RigHand
    {
        public readonly string Hand;
        public readonly string Grip;
        public readonly int Order;

        public RigHand(string hand, string grip, int order)
        {
            Hand = hand;
            Grip = grip;
            Order = order;
        }
    }

    public sealed class RigView
    {
        public RigViewKind Kind;
        public string Name;
        public string Key;
        public string Psb;
        public RigBone[] Bones;
        public RigPart[] Parts;
        public RigHand[] Hands;
    }

    public readonly struct ToolArt
    {
        public readonly string Label;
        public readonly Vector2 Grip;

        public ToolArt(string label, Vector2 grip)
        {
            Label = label;
            Grip = grip;
        }
    }

    public readonly struct PoseKey
    {
        public readonly string Target;
        public readonly string Channel;
        public readonly float Value;

        public PoseKey(string target, string channel, float value)
        {
            Target = target;
            Channel = channel;
            Value = value;
        }
    }

    public readonly struct Wiggle
    {
        public readonly string Target;
        public readonly string Channel;
        public readonly float Amplitude;
        public readonly int Cycles;

        public Wiggle(string target, string channel, float amplitude, int cycles)
        {
            Target = target;
            Channel = channel;
            Amplitude = amplitude;
            Cycles = cycles;
        }
    }

    public readonly struct ToolSwap
    {
        public readonly float At;
        public readonly string Hand;
        public readonly string Label;

        public ToolSwap(float at, string hand, string label)
        {
            At = at;
            Hand = hand;
            Label = label;
        }
    }

    public sealed class Beat
    {
        public string Name;
        public float Length;
        public PoseKey[] Pose = new PoseKey[0];
        public Wiggle[] Wiggles = new Wiggle[0];
        public ToolSwap[] Tools = new ToolSwap[0];
    }

    public sealed class WorkLoop
    {
        public RabbitWork Work;
        public bool Snappy;
        public float ReturnLength;
        public Beat[] Beats;
    }

    public sealed class RabbitKind
    {
        public readonly string Id;
        public readonly string Art;
        public readonly bool Lop;

        public RabbitKind(string id, string art, bool lop)
        {
            Id = id;
            Art = art;
            Lop = lop;
        }

        public string Folder => $"{RabbitRigSpec.ArtRoot}/{Id}";
        public string Psb(RigViewKind view) => $"{Folder}/{Id}{view}.psb";
        public string Pivots(RigViewKind view) => $"{Folder}/{Id}{view}.pivots.json";
        public string Prefab => $"{Folder}/Rabbit{Id}.prefab";
        public string Library => $"{Folder}/Rabbit{Id}_Library.asset";
        public string Clip(WorkLoop loop) => $"{Folder}/{Id}_{loop.Work}.anim";
    }

    public enum RigViewKind
    {
        Front,
        Side,
    }

    public static class RabbitRigSpec
    {
        public const string ArtRoot = "Assets/VuonTra/Art/Rabbit25D";
        public const string Shared = ArtRoot + "/Shared";
        public const string ToolFolder = Shared + "/Tools";
        public const string ShadowPath = Shared + "/Shadow.png";
        public const string ControllerPath = Shared + "/Rabbit.controller";
        public const string ToolCategory = "Tool";
        public const string ShadowCategory = "Shadow";
        public const int CanvasPx = 512;
        public const float Ppu = 100f;
        public const float ToolPpu = 470f;
        public const float ShadowWidthPx = 170f;
        public const float EarBlendPx = 36f;

        public const string FrontKey = "F";
        public const string SideKey = "S";
        public const string ShadowTarget = "Shadow";
        public const string FrontObject = "Front";
        public const string SideObject = "ThreeQuarter";

        public static readonly RabbitKind[] Kinds =
        {
            new RabbitKind("Kem", "kem", false),
            new RabbitKind("TaiCup", "taicup", true),
            new RabbitKind("DomXam", "domxam", false),
            new RabbitKind("Tim", "tim", false),
        };

        public static readonly RigViewKind[] ViewKinds = { RigViewKind.Front, RigViewKind.Side };

        public static string ViewObject(RigViewKind view) => view == RigViewKind.Front ? FrontObject : SideObject;

        public static string ViewKey(RigViewKind view) => view == RigViewKind.Front ? FrontKey : SideKey;

        public static readonly ToolArt[] ToolArts =
        {
            new ToolArt("SpoonLeaves", new Vector2(0.86f, 0.14f)),
            new ToolArt("Kettle", new Vector2(0.62f, 0.93f)),
            new ToolArt("Teapot", new Vector2(0.9f, 0.62f)),
            new ToolArt("Cup", new Vector2(0.5f, 0.45f)),
            new ToolArt("MilkBottle", new Vector2(0.5f, 0.58f)),
            new ToolArt("ShakerOpen", new Vector2(0.5f, 0.55f)),
            new ToolArt("ShakerClosed", new Vector2(0.5f, 0.45f)),
            new ToolArt("BubbleTea", new Vector2(0.5f, 0.42f)),
            new ToolArt("LeafBunch", new Vector2(0.5f, 0.14f)),
            new ToolArt("BasketEmpty", new Vector2(0.5f, 0.94f)),
            new ToolArt("BasketHalf", new Vector2(0.5f, 0.94f)),
            new ToolArt("BasketFull", new Vector2(0.5f, 0.94f)),
        };

        static PoseKey P(string target, string channel, float value) => new PoseKey(target, channel, value);
        static Wiggle W(string target, string channel, float amplitude, int cycles) => new Wiggle(target, channel, amplitude, cycles);
        static ToolSwap T(float at, string hand, string label) => new ToolSwap(at, hand, label);

        const float BeatLength = 0.72f;

        public static readonly WorkLoop[] Loops =
        {
            new WorkLoop
            {
                Work = RabbitWork.Idle,
                Snappy = false,
                ReturnLength = 1.3f,
                Beats = new[]
                {
                    new Beat
                    {
                        Name = "inhale",
                        Length = 1.3f,
                        Pose = new[]
                        {
                            P("F:Spine", "SY", 0.022f), P("F:Spine", "SX", -0.01f), P("F:Neck", "Y", 0.012f),
                            P("F:EarRoot", "R", 1.5f), P("F:ShoulderL", "R", 3f), P("F:ShoulderR", "R", -3f),
                            P("S:Spine", "SY", 0.022f), P("S:Spine", "SX", -0.01f), P("S:Neck", "R", -1.5f),
                            P("S:EarRoot", "R", -3f), P("S:ShoulderR", "R", 3f),
                            P(ShadowTarget, "SX", -0.03f),
                        },
                    },
                },
            },
            new WorkLoop
            {
                Work = RabbitWork.MilkTea,
                Snappy = true,
                ReturnLength = 0.4f,
                Beats = new[]
                {
                    new Beat
                    {
                        Name = "pour milk into shaker",
                        Length = BeatLength,
                        Tools = new[] { T(0f, "F:R", "MilkBottle"), T(0f, "F:L", "ShakerOpen"), T(0f, "S:R", "MilkBottle") },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", -58f), P("F:GripR", "R", 128f), P("F:ShoulderL", "R", 34f), P("F:GripL", "R", -34f),
                            P("F:Neck", "R", -4f), P("F:EarRoot", "R", 2.5f),
                            P("S:ShoulderR", "R", -58f), P("S:GripR", "R", 128f), P("S:Neck", "R", 5f), P("S:EarRoot", "R", -4f),
                        },
                    },
                    new Beat
                    {
                        Name = "lid on, shake by the head",
                        Length = BeatLength,
                        Tools = new[] { T(0.12f, "F:R", null), T(0.12f, "F:L", "ShakerClosed"), T(0.12f, "S:R", "ShakerClosed") },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", -22f), P("F:GripR", "R", 0f), P("F:ShoulderL", "R", -128f), P("F:GripL", "R", 128f),
                            P("F:Neck", "R", -3f), P("F:EarRoot", "R", 0.0f),
                            P("S:ShoulderR", "R", -118f), P("S:GripR", "R", 118f), P("S:Neck", "R", -2f), P("S:EarRoot", "R", 0f),
                        },
                        Wiggles = new[]
                        {
                            W("F:ShoulderL", "R", 9f, 3), W("F:EarRoot", "R", 2.0f, 3), W("F:Spine", "SY", 0.012f, 3),
                            W("S:ShoulderR", "R", 9f, 3), W("S:EarRoot", "R", 4f, 3), W("S:Spine", "SY", 0.012f, 3),
                        },
                    },
                    new Beat
                    {
                        Name = "pour into the pearl cup",
                        Length = BeatLength,
                        Tools = new[] { T(0.1f, "F:L", "ShakerOpen"), T(0.1f, "F:R", "BubbleTea"), T(0.1f, "S:R", "ShakerOpen") },
                        Pose = new[]
                        {
                            P("F:ShoulderL", "R", 48f), P("F:GripL", "R", -142f), P("F:ShoulderR", "R", -34f), P("F:GripR", "R", 34f),
                            P("F:Neck", "R", 3f), P("F:EarRoot", "R", -2.0f),
                            P("S:ShoulderR", "R", -64f), P("S:GripR", "R", 150f), P("S:Neck", "R", 6f), P("S:EarRoot", "R", -5f),
                        },
                    },
                    new Beat
                    {
                        Name = "paws together, smile",
                        Length = BeatLength,
                        Tools = new[] { T(0.12f, "F:L", null), T(0.12f, "F:R", null), T(0.12f, "S:R", null) },
                        Pose = new[]
                        {
                            P("F:ShoulderL", "R", 40f), P("F:GripL", "R", 0f), P("F:ShoulderR", "R", -40f), P("F:GripR", "R", 0f),
                            P("F:Neck", "R", 0f), P("F:EarRoot", "R", -3.0f), P("F:Hips", "Y", 0.035f), P("F:Spine", "SY", 0.02f),
                            P("S:ShoulderR", "R", -28f), P("S:GripR", "R", 0f), P("S:Neck", "R", -2f), P("S:EarRoot", "R", 6f),
                            P("S:Hips", "Y", 0.035f), P("S:Spine", "SY", 0.02f),
                            P(ShadowTarget, "SX", -0.1f), P(ShadowTarget, "SY", -0.1f),
                        },
                    },
                },
            },
            new WorkLoop
            {
                Work = RabbitWork.TeaPicking,
                Snappy = true,
                ReturnLength = 0.4f,
                Beats = new[]
                {
                    new Beat
                    {
                        Name = "bend and pick young buds",
                        Length = BeatLength,
                        Tools = new[] { T(0f, "F:L", "BasketEmpty"), T(0.62f, "F:R", "LeafBunch"), T(0.62f, "S:R", "LeafBunch") },
                        Pose = new[]
                        {
                            P("F:Spine", "SY", -0.07f), P("F:Spine", "SX", 0.03f), P("F:Neck", "Y", -0.05f), P("F:Neck", "R", 4f),
                            P("F:ShoulderR", "R", 6f), P("F:ShoulderL", "R", 12f), P("F:GripL", "R", -12f), P("F:EarRoot", "R", -2.5f),
                            P("S:Spine", "R", 12f), P("S:Neck", "R", 8f), P("S:ShoulderR", "R", -18f), P("S:EarRoot", "R", -6f),
                        },
                    },
                    new Beat
                    {
                        Name = "drop the leaves in the basket",
                        Length = BeatLength,
                        Tools = new[] { T(0.66f, "F:R", null), T(0.7f, "F:L", "BasketHalf"), T(0.66f, "S:R", null) },
                        Pose = new[]
                        {
                            P("F:Spine", "SY", 0f), P("F:Spine", "SX", 0f), P("F:Neck", "Y", 0f), P("F:Neck", "R", 5f),
                            P("F:ShoulderR", "R", -52f), P("F:GripR", "R", 52f), P("F:ShoulderL", "R", 22f), P("F:GripL", "R", -22f),
                            P("F:EarRoot", "R", 1.5f),
                            P("S:Spine", "R", 3f), P("S:Neck", "R", 6f), P("S:ShoulderR", "R", -50f), P("S:GripR", "R", 50f),
                            P("S:EarRoot", "R", 2f),
                        },
                    },
                    new Beat
                    {
                        Name = "basket full, dust the paws",
                        Length = BeatLength,
                        Tools = new[] { T(0.08f, "F:L", "BasketFull") },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", -26f), P("F:GripR", "R", 0f), P("F:Neck", "R", -2f),
                            P("S:ShoulderR", "R", -32f), P("S:GripR", "R", 0f), P("S:Spine", "R", 0f), P("S:Neck", "R", 0f),
                        },
                        Wiggles = new[] { W("F:ShoulderR", "R", 11f, 3), W("F:EarRoot", "R", 1.5f, 3), W("S:ShoulderR", "R", 11f, 3), W("S:EarRoot", "R", 3f, 3) },
                    },
                    new Beat
                    {
                        Name = "set the full basket down",
                        Length = BeatLength,
                        Tools = new[] { T(0.05f, "S:R", "BasketFull"), T(0.82f, "F:L", null), T(0.82f, "S:R", null) },
                        Pose = new[]
                        {
                            P("F:Spine", "SY", -0.05f), P("F:Neck", "Y", -0.035f), P("F:Neck", "R", 0f), P("F:ShoulderL", "R", 4f),
                            P("F:GripL", "R", -4f), P("F:ShoulderR", "R", -8f), P("F:EarRoot", "R", -2.0f),
                            P("S:Spine", "R", 9f), P("S:Neck", "R", 4f), P("S:ShoulderR", "R", -12f), P("S:EarRoot", "R", -4f),
                        },
                    },
                },
            },
            new WorkLoop
            {
                Work = RabbitWork.TeaBrewing,
                Snappy = true,
                ReturnLength = 0.4f,
                Beats = new[]
                {
                    new Beat
                    {
                        Name = "spoon leaves into the pot",
                        Length = BeatLength,
                        Tools = new[] { T(0f, "F:R", "SpoonLeaves"), T(0f, "S:R", "SpoonLeaves") },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", -56f), P("F:GripR", "R", 10f), P("F:Neck", "R", -4f), P("F:EarRoot", "R", 2.0f),
                            P("S:ShoulderR", "R", -54f), P("S:GripR", "R", 14f), P("S:Neck", "R", 6f), P("S:EarRoot", "R", -5f),
                        },
                        Wiggles = new[] { W("F:GripR", "R", 10f, 2), W("S:GripR", "R", 10f, 2) },
                    },
                    new Beat
                    {
                        Name = "pour hot water from the kettle",
                        Length = BeatLength,
                        Tools = new[] { T(0.1f, "F:R", "Kettle"), T(0.1f, "S:R", "Kettle") },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", -46f), P("F:GripR", "R", 88f), P("F:Neck", "R", -5f), P("F:EarRoot", "R", 2.5f),
                            P("S:ShoulderR", "R", -50f), P("S:GripR", "R", 84f), P("S:Neck", "R", 7f), P("S:EarRoot", "R", -6f),
                        },
                    },
                    new Beat
                    {
                        Name = "wait while the tea steeps",
                        Length = BeatLength,
                        Tools = new[] { T(0.1f, "F:R", null), T(0.1f, "S:R", null) },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", 6f), P("F:GripR", "R", 0f), P("F:ShoulderL", "R", -6f), P("F:Neck", "R", 3f),
                            P("F:EarRoot", "R", -2.0f),
                            P("S:ShoulderR", "R", 22f), P("S:GripR", "R", 0f), P("S:Neck", "R", -3f), P("S:EarRoot", "R", 4f),
                        },
                        Wiggles = new[] { W("F:Spine", "R", 2.5f, 1), W("S:Spine", "R", 2f, 1) },
                    },
                    new Beat
                    {
                        Name = "pour tea into the cup",
                        Length = BeatLength,
                        Tools = new[] { T(0.1f, "F:R", "Teapot"), T(0.1f, "F:L", "Cup"), T(0.1f, "S:R", "Teapot") },
                        Pose = new[]
                        {
                            P("F:ShoulderR", "R", -48f), P("F:GripR", "R", 76f), P("F:ShoulderL", "R", 36f), P("F:GripL", "R", -36f),
                            P("F:Neck", "R", -3f), P("F:EarRoot", "R", 1.5f),
                            P("S:ShoulderR", "R", -50f), P("S:GripR", "R", 76f), P("S:Neck", "R", 6f), P("S:EarRoot", "R", -4f),
                        },
                    },
                },
            },
        };

        public static IEnumerable<string> ToolLabels()
        {
            foreach (ToolArt tool in ToolArts) yield return tool.Label;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D.Animation;
using VuonTra.Core;
using VuonTra.Game.Bunny;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Builds the layered paper-doll rabbit (owner's review 23 Nov, Unity 2D best practice): import settings for the
    /// layers in Art/Bunny (512 by 512 canvases, 100 pixels per unit, pivot at the bottom middle; tools pivot at their
    /// grip), the sprite library (BunnySpriteLibrary.asset: Body, Head, Ears, Arm, Paw labelled by kind id; Hat and
    /// Charm by item id; Prop by trade), the animation clips and controller (Idle, Brew, Bake, Gather, Stargaze,
    /// Doze, Squish, Hop), and the BunnyCharacter prefab (SortingGroup, Animator, SpriteLibrary, SpriteResolvers,
    /// particle systems). All movement is keyframed here, once; at run time code only picks labels and states.
    /// </summary>
    public static class BunnyBuilder
    {
        public const string ArtFolder = "Assets/VuonTra/Art/Bunny", OutFolder = "Assets/VuonTra/Content/Bunny";
        public const string LibraryPath = OutFolder + "/BunnySpriteLibrary.asset", ControllerPath = OutFolder + "/BunnyAnimator.controller",
            PrefabPath = OutFolder + "/BunnyCharacter.prefab", DyePath = OutFolder + "/BunnyDyeFur.mat", ParticlePath = OutFolder + "/BunnyParticle.mat";
        const float Ppu = 100f, Canvas = 512f;

        // The master rabbit's joints on the 512 canvas (drawing pixels from its top left), as cut by cat_lop.py:
        // 320x343 master points scaled by 1.2 and moved by (64, 89).
        static Vector2 M(float x, float y) => new Vector2(x * 1.2f + 64f, y * 1.2f + 89f);
        /// <summary>A canvas point in units from the canvas's bottom middle (the sprites' pivot).</summary>
        static Vector3 U(Vector2 px) => new Vector3((px.x - Canvas / 2f) / Ppu, (Canvas - px.y) / Ppu, 0f);
        static readonly Vector3 Feet = U(M(160f, 343f)), Neck = U(M(148f, 196f)), EarBase = U(M(167f, 100f)),
            Shoulder = U(M(192f, 216f)), Hand = U(M(192f, 270f)), Eyepiece = U(M(170f, 150f));

        [MenuItem("VuonTra/Build Bunny Character")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder)) return;
            Directory.CreateDirectory(OutFolder);
            AssetDatabase.Refresh();
            ImportLayers();
            SpriteLibraryAsset library = BuildLibrary();
            AnimatorController controller = BuildController();
            BuildPrefab(library, controller);
            AssetDatabase.SaveAssets();
        }

        static void ImportLayers()
        {
            var grips = new Dictionary<string, float>();
            string gripFile = ArtFolder + "/Props/grip.json";
            if (File.Exists(gripFile))
                foreach (string pair in File.ReadAllText(gripFile).Trim('{', '}', ' ', '\n').Split(','))
                {
                    string[] kv = pair.Split(':');
                    if (kv.Length == 2) grips[kv[0].Trim(' ', '"')] = float.Parse(kv[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                }
            WriteNone();
            foreach (string file in Directory.GetFiles(ArtFolder, "*.png", SearchOption.AllDirectories))
            {
                string path = file.Replace('\\', '/');
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;
                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.textureType = TextureImporterType.Sprite;
                settings.spriteMode = (int)SpriteImportMode.Single;
                settings.spritePixelsPerUnit = Ppu;
                settings.alphaIsTransparency = true;
                settings.mipmapEnabled = false;
                settings.filterMode = FilterMode.Bilinear;
                settings.spriteMeshType = SpriteMeshType.FullRect;
                string name = Path.GetFileNameWithoutExtension(path);
                if (path.Contains("/Props/") && grips.TryGetValue(name, out float grip))
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    settings.spritePivot = new Vector2(0.5f, 1f - grip);
                }
                else settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
                importer.SetTextureSettings(settings);
                importer.textureCompression = TextureImporterCompression.CompressedHQ;
                importer.SaveAndReimport();
            }
        }

        static void WriteNone()
        {
            string path = ArtFolder + "/none.png";
            if (File.Exists(path)) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            tex.SetPixels32(new Color32[16]);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

        static SpriteLibraryAsset BuildLibrary()
        {
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            Sprite none = Load(ArtFolder + "/none.png");
            foreach (string cat in new[] { BunnyFigure.BodyCategory, BunnyFigure.HeadCategory, BunnyFigure.EarsCategory, BunnyFigure.ArmCategory,
                         BunnyFigure.PawCategory, BunnyFigure.HatCategory, BunnyFigure.CharmCategory, BunnyFigure.PropCategory })
                library.AddCategoryLabel(none, cat, BunnyFigure.NoneLabel);
            foreach (RabbitKind kind in RabbitKinds.All)
            {
                string dir = ArtFolder + "/" + kind.Id + "/";
                Add(library, Load(dir + "body.png"), BunnyFigure.BodyCategory, kind.Id);
                Add(library, Load(dir + "head.png"), BunnyFigure.HeadCategory, kind.Id);
                Add(library, Load(dir + "ears.png"), BunnyFigure.EarsCategory, kind.Id);
                Add(library, Load(dir + "arm.png"), BunnyFigure.ArmCategory, kind.Id);
                Add(library, Load(dir + "paw.png"), BunnyFigure.PawCategory, kind.Id);
            }
            foreach (string file in Directory.GetFiles(ArtFolder + "/Wear", "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                bool charm = name.StartsWith("charm-");
                Add(library, Load(file.Replace('\\', '/')), charm ? BunnyFigure.CharmCategory : BunnyFigure.HatCategory, charm ? name.Substring(6) : name);
            }
            foreach (RabbitTrade trade in new[] { RabbitTrade.Brewer, RabbitTrade.Baker, RabbitTrade.Gatherer, RabbitTrade.Stargazer })
                Add(library, Load(ArtFolder + "/Props/" + trade.ToString().ToLowerInvariant() + ".png"), BunnyFigure.PropCategory, trade.ToString());
            AssetDatabase.DeleteAsset(LibraryPath);
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        static void Add(SpriteLibraryAsset library, Sprite sprite, string category, string label)
        {
            if (sprite != null) library.AddCategoryLabel(sprite, category, label);
        }

        // ---- Animation: the poses approved on the GIF (docs/anh/do-hoa-tho/hoat-anh), one key every eighth of a loop.
        // Columns: time (share of the loop), body angle, body scale x, body scale y, lift (units), head angle, ears angle,
        // arm angle, arm stretch, tool angle (in the paw; at the eye for Stargaze).
        static readonly Dictionary<string, float[][]> Loops = new Dictionary<string, float[][]>
        {
            ["Idle"] = new[]
            {
                new[] { 0.000f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, -0.8470f, 2.3301f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.125f, 0.5657f, 0.9915f, 1.0177f, 0.0000f, 0.2765f, 1.0071f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.250f, 0.8000f, 0.9880f, 1.0250f, 0.0000f, 1.2380f, -0.9059f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.375f, 0.5657f, 0.9915f, 1.0177f, 0.0000f, 1.4743f, -2.2882f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.500f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, 0.8470f, -2.3301f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.625f, -0.5657f, 1.0085f, 0.9823f, 0.0000f, -0.2765f, -1.0071f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.750f, -0.8000f, 1.0120f, 0.9750f, 0.0000f, -1.2380f, 0.9059f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 0.875f, -0.5657f, 1.0085f, 0.9823f, 0.0000f, -1.4743f, 2.2882f, 0.0000f, 1.0000f, 0.0000f },
                new[] { 1.000f, -0.0000f, 1.0000f, 1.0000f, 0.0000f, -0.8470f, 2.3301f, 0.0000f, 1.0000f, 0.0000f }
            },
            ["Brew"] = new[]
            {
                new[] { 0.000f, -0.0000f, 1.0075f, 0.9875f, 0.0000f, -7.4383f, -9.5243f, -22.0000f, 1.0000f, 236.0000f },
                new[] { 0.125f, -1.0607f, 1.0219f, 0.9636f, 0.0000f, -5.1554f, -5.8198f, -12.1005f, 1.0000f, 222.0000f },
                new[] { 0.250f, -1.5000f, 1.0300f, 0.9500f, 0.0000f, -3.3673f, -0.4635f, -8.0000f, 1.0000f, 208.0000f },
                new[] { 0.375f, -1.0607f, 1.0219f, 0.9636f, 0.0000f, -3.1214f, 3.4069f, -12.1005f, 1.0000f, 202.2010f },
                new[] { 0.500f, -0.0000f, 1.0075f, 0.9875f, 0.0000f, -4.5617f, 3.5243f, -22.0000f, 1.0000f, 208.0000f },
                new[] { 0.625f, 1.0607f, 1.0006f, 0.9989f, 0.0000f, -6.8446f, -0.1802f, -31.8995f, 1.0000f, 222.0000f },
                new[] { 0.750f, 1.5000f, 1.0000f, 1.0000f, 0.0000f, -8.6327f, -5.5365f, -36.0000f, 1.0000f, 236.0000f },
                new[] { 0.875f, 1.0607f, 1.0006f, 0.9989f, 0.0000f, -8.8786f, -9.4069f, -31.8995f, 1.0000f, 241.7990f },
                new[] { 1.000f, 0.0000f, 1.0075f, 0.9875f, 0.0000f, -7.4383f, -9.5243f, -22.0000f, 1.0000f, 236.0000f }
            },
            ["Bake"] = new[]
            {
                new[] { 0.000f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, -3.0000f, 0.0000f, -30.0000f, 1.0000f, 120.0000f },
                new[] { 0.125f, -2.0000f, 1.0250f, 0.9600f, 0.0000f, -3.4756f, 0.0000f, -26.0000f, 1.0000f, 118.1213f },
                new[] { 0.250f, -4.0000f, 1.0500f, 0.9200f, 0.0000f, -7.6209f, 2.9193f, -22.0000f, 1.0000f, 115.0000f },
                new[] { 0.375f, -2.0000f, 1.0250f, 0.9600f, 0.0000f, -8.5244f, 9.5465f, -26.0000f, 1.0000f, 118.1213f },
                new[] { 0.500f, -0.0000f, 1.0000f, 1.0000f, 0.0000f, -4.3791f, 7.0807f, -30.0000f, 1.0000f, 120.0000f },
                new[] { 0.625f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, -3.0000f, 0.4535f, -30.0000f, 1.0000f, 117.8787f },
                new[] { 0.750f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, -3.0000f, 0.0000f, -30.0000f, 1.0000f, 117.0000f },
                new[] { 0.875f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, -3.0000f, 0.0000f, -30.0000f, 1.0000f, 117.8787f },
                new[] { 1.000f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, -3.0000f, 0.0000f, -30.0000f, 1.0000f, 120.0000f }
            },
            ["Gather"] = new[]
            {
                new[] { 0.000f, -0.0000f, 1.0000f, 1.0000f, 0.0000f, 4.0000f, 5.0000f, 142.0000f, 1.0000f, -152.5171f },
                new[] { 0.125f, -2.1213f, 0.9956f, 1.0073f, 0.0211f, 5.1716f, 2.9497f, 145.2218f, 1.0000f, -149.7471f },
                new[] { 0.250f, -3.0000f, 0.9850f, 1.0250f, 0.0720f, 8.0000f, -2.0000f, 153.0000f, 1.0000f, -152.3973f },
                new[] { 0.375f, -2.1213f, 0.9744f, 1.0427f, 0.1229f, 10.8284f, -6.9497f, 160.7782f, 1.0000f, -158.9153f },
                new[] { 0.500f, -0.0000f, 0.9700f, 1.0500f, 0.1440f, 12.0000f, -9.0000f, 164.0000f, 1.0000f, -165.4829f },
                new[] { 0.625f, 2.1213f, 0.9744f, 1.0427f, 0.1229f, 10.8284f, -6.9497f, 160.7782f, 1.0000f, -168.2529f },
                new[] { 0.750f, 3.0000f, 0.9850f, 1.0250f, 0.0720f, 8.0000f, -2.0000f, 153.0000f, 1.0000f, -165.6027f },
                new[] { 0.875f, 2.1213f, 0.9956f, 1.0073f, 0.0211f, 5.1716f, 2.9497f, 145.2218f, 1.0000f, -159.0847f },
                new[] { 1.000f, 0.0000f, 1.0000f, 1.0000f, 0.0000f, 4.0000f, 5.0000f, 142.0000f, 1.0000f, -152.5171f }
            },
            ["Stargaze"] = new[]
            {
                new[] { 0.000f, 4.0000f, 1.0000f, 1.0000f, 0.0000f, 9.7663f, 2.7961f, -173.8202f, 1.4500f, -55.0000f },
                new[] { 0.125f, 4.3536f, 1.0000f, 1.0085f, 0.0000f, 10.2256f, 1.2085f, -173.2022f, 1.4500f, -53.5858f },
                new[] { 0.250f, 4.5000f, 1.0000f, 1.0120f, 0.0000f, 10.5526f, -1.0871f, -172.8462f, 1.4500f, -53.0000f },
                new[] { 0.375f, 4.3536f, 1.0000f, 1.0085f, 0.0000f, 10.5560f, -2.7458f, -172.9632f, 1.4500f, -53.5858f },
                new[] { 0.500f, 4.0000f, 1.0000f, 1.0000f, 0.0000f, 10.2337f, -2.7961f, -173.4813f, 1.4500f, -55.0000f },
                new[] { 0.625f, 3.6464f, 1.0000f, 0.9915f, 0.0000f, 9.7744f, -1.2085f, -174.0943f, 1.4500f, -56.4142f },
                new[] { 0.750f, 3.5000f, 1.0000f, 0.9880f, 0.0000f, 9.4474f, 1.0871f, -174.4466f, 1.4500f, -57.0000f },
                new[] { 0.875f, 3.6464f, 1.0000f, 0.9915f, 0.0000f, 9.4440f, 2.7458f, -174.3346f, 1.4500f, -56.4142f },
                new[] { 1.000f, 4.0000f, 1.0000f, 1.0000f, 0.0000f, 9.7663f, 2.7961f, -173.8202f, 1.4500f, -55.0000f }
            },
            ["Doze"] = new[]
            {
                new[] { 0.000f, -9.0000f, 1.0000f, 1.0000f, 0.0000f, -9.0411f, 4.3171f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.125f, -7.4444f, 1.0000f, 0.9929f, 0.0000f, -10.5631f, 5.5741f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.250f, -6.8000f, 1.0000f, 0.9900f, 0.0000f, -11.7552f, 7.0806f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.375f, -7.4444f, 1.0000f, 0.9929f, 0.0000f, -11.9191f, 7.9541f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.500f, -9.0000f, 1.0000f, 1.0000f, 0.0000f, -10.9589f, 7.6829f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.625f, -10.5556f, 1.0000f, 1.0071f, 0.0000f, -9.4369f, 6.4259f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.750f, -11.2000f, 1.0000f, 1.0100f, 0.0000f, -8.2448f, 4.9194f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 0.875f, -10.5556f, 1.0000f, 1.0071f, 0.0000f, -8.0809f, 4.0459f, -8.0000f, 1.0000f, -15.0000f },
                new[] { 1.000f, -9.0000f, 1.0000f, 1.0000f, 0.0000f, -9.0411f, 4.3171f, -8.0000f, 1.0000f, -15.0000f }
            },

        };
        static readonly Dictionary<string, float> LoopSeconds = new Dictionary<string, float>
        {
            ["Idle"] = 3f, ["Brew"] = 1f, ["Bake"] = 1f, ["Gather"] = 1.2f, ["Stargaze"] = 3f, ["Doze"] = 3.5f
        };

        const string BodyPath = "BodyPivot", HeadPath = "BodyPivot/HeadPivot", EarPath = "BodyPivot/HeadPivot/EarPivot",
            ArmPath = "BodyPivot/HandPivot", HandPath = "BodyPivot/HandPivot/Hand", EyePropPath = "BodyPivot/HeadPivot/EyeProp",
            OffPawPath = "BodyPivot/HandPivot/Hand/OffPaw", DropsPath = "BodyPivot/HandPivot/Hand/FX Drops", FlourPath = "BodyPivot/HandPivot/Hand/FX Flour",
            LeavesPath = "BodyPivot/HandPivot/Hand/FX Leaves", StarsPath = "BodyPivot/HeadPivot/EyeProp/FX Stars";

        static AnimationClip LoopClip(string name)
        {
            float[][] keys = Loops[name];
            float length = LoopSeconds[name];
            var clip = new AnimationClip { name = name, frameRate = 30f };
            Curve(clip, BodyPath, "localEulerAnglesRaw.z", keys, length, 1);
            Curve(clip, BodyPath, "m_LocalScale.x", keys, length, 2);
            Curve(clip, BodyPath, "m_LocalScale.y", keys, length, 3);
            Curve(clip, BodyPath, "m_LocalPosition.y", keys, length, 4, Feet.y);
            Curve(clip, HeadPath, "localEulerAnglesRaw.z", keys, length, 5);
            Curve(clip, EarPath, "localEulerAnglesRaw.z", keys, length, 6);
            Curve(clip, ArmPath, "localEulerAnglesRaw.z", keys, length, 7);
            Curve(clip, ArmPath, "m_LocalScale.y", keys, length, 8);
            bool star = name == "Stargaze", working = name == "Brew" || name == "Bake" || name == "Gather";
            Curve(clip, star ? EyePropPath : HandPath, "localEulerAnglesRaw.z", keys, length, 9);
            Flag(clip, HandPath, !star, length);
            Flag(clip, EyePropPath, star, length);
            Flag(clip, OffPawPath, name == "Bake", length);
            Flag(clip, DropsPath, name == "Brew", length);
            Flag(clip, FlourPath, name == "Bake", length);
            Flag(clip, LeavesPath, name == "Gather", length);
            Flag(clip, StarsPath, star, length);
            Loop(clip, true);
            return clip;
        }

        static void Curve(AnimationClip clip, string path, string property, float[][] keys, float length, int column, float offset = 0f)
        {
            var curve = new AnimationCurve();
            foreach (float[] k in keys) curve.AddKey(k[0] * length, k[column] + offset);
            for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        static void Flag(AnimationClip clip, string path, bool on, float length)
        {
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"),
                AnimationCurve.Constant(0f, length, on ? 1f : 0f));
        }

        static void Loop(AnimationClip clip, bool loop)
        {
            AnimationClipSettings s = AnimationUtility.GetAnimationClipSettings(clip);
            s.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, s);
        }

        /// <summary>A short one-off: squash on a tap, or a jump for joy with a squash on landing.</summary>
        static AnimationClip ShotClip(string name, float[][] keys, float length)
        {
            var clip = new AnimationClip { name = name, frameRate = 30f };
            Curve(clip, BodyPath, "m_LocalScale.x", keys, length, 1);
            Curve(clip, BodyPath, "m_LocalScale.y", keys, length, 2);
            Curve(clip, BodyPath, "m_LocalPosition.y", keys, length, 3, Feet.y);
            Loop(clip, false);
            return clip;
        }

        static AnimatorController BuildController()
        {
            string animFolder = OutFolder + "/Anim";
            Directory.CreateDirectory(animFolder);
            var clips = new Dictionary<string, AnimationClip>();
            foreach (string name in LoopSeconds.Keys) clips[name] = Save(LoopClip(name), animFolder);
            clips["Squish"] = Save(ShotClip("Squish", new[]
            {
                new[] { 0f, 1f, 1f, 0f }, new[] { 0.3f, 1.12f, 0.82f, 0f }, new[] { 0.7f, 0.97f, 1.04f, 0f }, new[] { 1f, 1f, 1f, 0f }
            }, 0.25f), animFolder);
            clips["Hop"] = Save(ShotClip("Hop", new[]
            {
                new[] { 0f, 1.06f, 0.9f, 0f }, new[] { 0.2f, 0.94f, 1.1f, 0.35f }, new[] { 0.45f, 0.97f, 1.05f, 0.6f },
                new[] { 0.8f, 1.06f, 0.9f, 0.02f }, new[] { 0.9f, 1.1f, 0.86f, 0f }, new[] { 1f, 1f, 1f, 0f }
            }, 0.5f), animFolder);

            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController c = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            c.AddParameter("Job", AnimatorControllerParameterType.Int);
            c.AddParameter("Doze", AnimatorControllerParameterType.Bool);
            c.AddParameter("Squish", AnimatorControllerParameterType.Trigger);
            c.AddParameter("Hop", AnimatorControllerParameterType.Trigger);
            AnimatorStateMachine sm = c.layers[0].stateMachine;
            AnimatorState idle = sm.AddState("Idle");
            idle.motion = clips["Idle"];
            sm.defaultState = idle;
            string[] jobs = { "Brew", "Bake", "Gather", "Stargaze" };
            for (int j = 0; j < jobs.Length; j++)
            {
                AnimatorState s = sm.AddState(jobs[j]);
                s.motion = clips[jobs[j]];
                AnimatorStateTransition go = idle.AddTransition(s);
                go.hasExitTime = false; go.duration = 0.15f;
                go.AddCondition(AnimatorConditionMode.Equals, j + 1, "Job");
                AnimatorStateTransition back = s.AddTransition(idle);
                back.hasExitTime = false; back.duration = 0.15f;
                back.AddCondition(AnimatorConditionMode.NotEqual, j + 1, "Job");
            }
            AnimatorState doze = sm.AddState("Doze");
            doze.motion = clips["Doze"];
            AnimatorStateTransition toDoze = sm.AddAnyStateTransition(doze);
            toDoze.hasExitTime = false; toDoze.duration = 0.6f; toDoze.canTransitionToSelf = false;
            toDoze.AddCondition(AnimatorConditionMode.If, 0f, "Doze");
            AnimatorStateTransition wake = doze.AddTransition(idle);
            wake.hasExitTime = false; wake.duration = 0.3f;
            wake.AddCondition(AnimatorConditionMode.IfNot, 0f, "Doze");
            foreach (string shot in new[] { "Squish", "Hop" })
            {
                AnimatorState s = sm.AddState(shot);
                s.motion = clips[shot];
                AnimatorStateTransition any = sm.AddAnyStateTransition(s);
                any.hasExitTime = false; any.duration = 0.02f;
                any.AddCondition(AnimatorConditionMode.If, 0f, shot);
                AnimatorStateTransition done = s.AddTransition(idle);
                done.hasExitTime = true; done.exitTime = 1f; done.duration = 0.05f;
            }
            return c;
        }

        static AnimationClip Save(AnimationClip clip, string folder)
        {
            string path = folder + "/" + clip.name + ".anim";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        // ---- The prefab.
        static void BuildPrefab(SpriteLibraryAsset library, AnimatorController controller)
        {
            Material unlit = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            Material dye = AssetDatabase.LoadAssetAtPath<Material>(DyePath);
            if (dye == null)
            {
                dye = new Material(Shader.Find("VuonTra/Sprite Dye Fur"));
                AssetDatabase.CreateAsset(dye, DyePath);
            }
            Material particle = AssetDatabase.LoadAssetAtPath<Material>(ParticlePath);
            if (particle == null)
            {
                particle = new Material(unlit != null ? unlit.shader : Shader.Find("Sprites/Default"));
                particle.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/VuonTra/Art/Placeholder/glow.png");
                AssetDatabase.CreateAsset(particle, ParticlePath);
            }

            var root = new GameObject("BunnyCharacter");
            root.AddComponent<SortingGroup>();
            var animator = root.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            var lib = root.AddComponent<SpriteLibrary>();
            lib.spriteLibraryAsset = library;
            var figure = root.AddComponent<BunnyFigure>();

            Transform body = Node("BodyPivot", root.transform, Feet);
            SpriteResolver bodyR = Layer("Body", body, -Feet, BunnyFigure.BodyCategory, 0, unlit, out SpriteRenderer bodyS);
            SpriteResolver charmR = Layer("Charm", body, -Feet, BunnyFigure.CharmCategory, 2, unlit, out _);
            SpriteResolver hatBodyR = Layer("HatOnBody", body, -Feet, BunnyFigure.HatCategory, 6, unlit, out _);
            Transform head = Node("HeadPivot", body, Neck - Feet);
            Transform ear = Node("EarPivot", head, EarBase - Neck);
            SpriteResolver earsR = Layer("Ears", ear, -EarBase, BunnyFigure.EarsCategory, 3, unlit, out SpriteRenderer earsS);
            SpriteResolver headR = Layer("Head", head, -Neck, BunnyFigure.HeadCategory, 4, unlit, out SpriteRenderer headS);
            Transform eye = Node("EyeProp", head, Eyepiece - Neck);
            Layer("Telescope", eye, Vector3.zero, BunnyFigure.PropCategory, 5, unlit, out _).SetCategoryAndLabel(BunnyFigure.PropCategory, RabbitTrade.Stargazer.ToString());
            SpriteResolver hatR = Layer("Hat", head, -Neck, BunnyFigure.HatCategory, 6, unlit, out SpriteRenderer hatS);
            Transform arm = Node("HandPivot", body, Shoulder - Feet);
            Transform hand = Node("Hand", arm, Hand - Shoulder);
            SpriteResolver propR = Layer("Prop", hand, Vector3.zero, BunnyFigure.PropCategory, 8, unlit, out _);
            SpriteResolver armR = Layer("Arm", arm, -Shoulder, BunnyFigure.ArmCategory, 9, unlit, out SpriteRenderer armS);
            // The other paw on a rolling pin's far end: along the tool, which points up its own picture from the grip.
            Transform off = Node("OffPaw", hand, new Vector3(0f, (0.82f - 0.18f) * 150f * 1.2f / Ppu, 0f));
            SpriteResolver offR = Layer("Paw", off, -Hand, BunnyFigure.PawCategory, 10, unlit, out SpriteRenderer offS);
            SpriteResolver pawR = offR;

            Particles("FX Drops", hand, new Vector3(0f, -0.95f, 0f), particle, new Color(0.67f, 0.43f, 0.24f, 0.9f), 0.07f, 0.5f, 6f, new Vector3(0f, 0.8f, 0f), 1.2f);
            Particles("FX Flour", hand, new Vector3(-0.4f, -0.2f, 0f), particle, new Color(1f, 0.98f, 0.94f, 0.95f), 0.08f, 0.6f, 10f, new Vector3(0f, 0.4f, 0f), 0.6f);
            Particles("FX Leaves", hand, new Vector3(0.45f, 0.6f, 0f), particle, new Color(0.48f, 0.71f, 0.38f, 1f), 0.11f, 1.4f, 3f, new Vector3(-0.2f, -0.6f, 0f), 0.15f);
            Particles("FX Stars", eye, new Vector3(0.5f, 1.4f, 0f), particle, new Color(1f, 0.78f, 0.4f, 1f), 0.12f, 1.2f, 3f, new Vector3(0.1f, 0.2f, 0f), 0f);

            var so = new SerializedObject(figure);
            so.FindProperty("_animator").objectReferenceValue = animator;
            so.FindProperty("_library").objectReferenceValue = lib;
            so.FindProperty("_body").objectReferenceValue = bodyR;
            so.FindProperty("_head").objectReferenceValue = headR;
            so.FindProperty("_ears").objectReferenceValue = earsR;
            so.FindProperty("_arm").objectReferenceValue = armR;
            so.FindProperty("_paw").objectReferenceValue = pawR;
            so.FindProperty("_offPaw").objectReferenceValue = offR;
            so.FindProperty("_hat").objectReferenceValue = hatR;
            so.FindProperty("_hatOnBody").objectReferenceValue = hatBodyR;
            so.FindProperty("_hatHome").vector3Value = -Neck;
            so.FindProperty("_hatOnBodyHome").vector3Value = -Feet;
            so.FindProperty("_charmHome").vector3Value = -Feet;
            so.FindProperty("_charm").objectReferenceValue = charmR;
            so.FindProperty("_prop").objectReferenceValue = propR;
            so.FindProperty("_hatRenderer").objectReferenceValue = hatS;
            so.FindProperty("_earsRenderer").objectReferenceValue = earsS;
            so.FindProperty("_earsBehind").intValue = 3;
            so.FindProperty("_earsOver").intValue = 7;
            so.FindProperty("_defaultMaterial").objectReferenceValue = unlit;
            SerializedProperty fur = so.FindProperty("_fur");
            SpriteRenderer[] furs = { bodyS, headS, earsS, armS, offS };
            fur.arraySize = furs.Length;
            for (int i = 0; i < furs.Length; i++) fur.GetArrayElementAtIndex(i).objectReferenceValue = furs[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
        }

        static Transform Node(string name, Transform parent, Vector3 local)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            return go.transform;
        }

        static SpriteResolver Layer(string name, Transform parent, Vector3 local, string category, int order, Material material, out SpriteRenderer renderer)
        {
            Transform t = Node(name, parent, local);
            renderer = t.gameObject.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = order;
            if (material != null) renderer.sharedMaterial = material;
            var resolver = t.gameObject.AddComponent<SpriteResolver>();
            resolver.SetCategoryAndLabel(category, BunnyFigure.NoneLabel);
            return resolver;
        }

        static void Particles(string name, Transform parent, Vector3 local, Material material, Color color, float size, float life, float rate, Vector3 velocity, float spread)
        {
            Transform t = Node(name, parent, local);
            var ps = t.gameObject.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = ps.main;
            main.loop = true; main.playOnAwake = true; main.startLifetime = life; main.startSpeed = 0f;
            main.startSize = size; main.startColor = color; main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 20; main.scalingMode = ParticleSystemScalingMode.Hierarchy;
            ParticleSystem.EmissionModule emission = ps.emission;
            emission.rateOverTime = rate;
            ParticleSystem.ShapeModule shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = Mathf.Max(0.01f, spread * 0.3f);
            ParticleSystem.VelocityOverLifetimeModule vel = ps.velocityOverLifetime;
            vel.enabled = true; vel.space = ParticleSystemSimulationSpace.Local;
            vel.x = velocity.x; vel.y = velocity.y; vel.z = 0f;
            ParticleSystem.ColorOverLifetimeModule col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.2f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            var r = t.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = material;
            r.sortingOrder = 11;
            t.gameObject.SetActive(false);
        }
    }
}

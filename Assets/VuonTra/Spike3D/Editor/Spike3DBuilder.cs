using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VuonTra.Game.UI;

namespace VuonTra.Spike3D
{
    public static class Spike3DBuilder
    {
        const string Root = "Assets/VuonTra/Spike3D";
        const string Generated = Root + "/Generated";
        const string ScenePath = Root + "/Garden3DSpike.unity";
        const string ToonPath = Root + "/Spike3DToon.shader";
        const string LitName = "Universal Render Pipeline/Lit";
        const string GrassPath = "Assets/VuonTra/Art/Placeholder/bg_grass.png";
        const string ShotFolder = "Logs/spike3d";
        const int Width = 1080;
        const int Height = 1920;
        const float Pitch = 40f;
        const float Breath = 2.4f;
        const float EarTilt = 12f;
        const float EarSway = 5f;
        const float GardenDistance = 36f;
        const float StationDistance = 11f;
        const float RabbitDistance = 4.5f;

        static readonly Color Earth = Palette.Hex("7A5238");
        static readonly Color PathSand = Palette.Hex("E3CFA4");
        static readonly Color Wood = Palette.Hex("A8714A");
        static readonly Color Stone = Palette.Hex("A49FB8");
        static readonly Color Teal = Palette.Hex("3F9C9A");
        static readonly Color TeaBush = Palette.Mix(Palette.Moss, Palette.Grass, 0.45f);
        static readonly Color Iron = Palette.Mix(Palette.Ink, Earth, 0.3f);
        static readonly Color MoonLight = new Color(0.82f, 0.9f, 1f);

        enum Look { Lit, Toon }

        sealed class Paint
        {
            public Material Material;
            public Color Emission;
            public float Outline;
        }

        static readonly List<Paint> Paints = new List<Paint>();

        public static void BuildAndShoot()
        {
            Paints.Clear();
            Directory.CreateDirectory(ShotFolder);
            AssetDatabase.DeleteAsset(Generated);
            AssetDatabase.CreateFolder(Root, "Generated");

            var pipeline = CreatePipeline();
            var previousDefault = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            bool previousAsync = ShaderUtil.allowAsyncCompilation;
            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;
            ShaderUtil.allowAsyncCompilation = false;
            try
            {
                BuildSceneAndShoot();
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = previousAsync;
                QualitySettings.renderPipeline = previousQuality;
                GraphicsSettings.defaultRenderPipeline = previousDefault;
            }
        }

        static void BuildSceneAndShoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpEnvironment();
            var garden = new GameObject("Garden").transform;
            BuildIsland(garden);
            Vector3 deckTop = BuildStation(garden, new Vector3(0.3f, 0f, 3f));
            Animator rabbit = BuildRabbit(garden, deckTop + new Vector3(-0.45f, 0f, -0.2f));
            AnimationClip idle = CreateIdle(rabbit);
            BuildPostFx();
            Camera cam = BuildCamera();

            var toon = AssetDatabase.LoadAssetAtPath<Shader>(ToonPath);
            bool toonReady = toon != null && !ShaderUtil.ShaderHasError(toon);
            if (!toonReady) Debug.LogError("Spike3D: toon shader is missing or failed to compile, shooting the Lit look only");

            Vector3 gardenFocus = new Vector3(0f, -0.3f, -0.4f);
            Vector3 stationFocus = deckTop + new Vector3(0f, 0.35f, 0f);
            Vector3 rabbitFocus = rabbit.transform.position + new Vector3(0f, 0.38f, 0f);

            ApplyLook(Look.Lit, toon);
            string litSuffix = toonReady ? "-lit" : "";
            Shoot(cam, stationFocus, StationDistance, "station3d" + litSuffix);
            Shoot(cam, gardenFocus, GardenDistance, "garden3d" + litSuffix);

            if (toonReady)
            {
                ApplyLook(Look.Toon, toon);
                for (int frame = 0; frame < 3; frame++)
                {
                    idle.SampleAnimation(rabbit.gameObject, frame * Breath / 4f);
                    Shoot(cam, rabbitFocus, RabbitDistance, "rabbit-idle-" + frame);
                }
                idle.SampleAnimation(rabbit.gameObject, 0f);
                Shoot(cam, stationFocus, StationDistance, "station3d");
                Shoot(cam, gardenFocus, GardenDistance, "garden3d");
            }

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("Spike3D: shots written to " + Path.GetFullPath(ShotFolder));
        }

        static UniversalRenderPipelineAsset CreatePipeline()
        {
            var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
            renderer.renderingMode = UnityEngine.Rendering.Universal.RenderingMode.ForwardPlus;
            renderer.postProcessData = AssetDatabase.LoadAssetAtPath<PostProcessData>(
                UniversalRenderPipelineAsset.packagePath + "/Runtime/Data/PostProcessData.asset");
            AssetDatabase.CreateAsset(renderer, Generated + "/Spike3DRenderer.asset");

            var pipeline = UniversalRenderPipelineAsset.Create(renderer);
            pipeline.shadowDistance = 60f;
            pipeline.shadowCascadeCount = 1;
            pipeline.mainLightShadowmapResolution = 2048;
            pipeline.msaaSampleCount = 4;
            AssetDatabase.CreateAsset(pipeline, Generated + "/Spike3DPipeline.asset");

            var serialized = new SerializedObject(pipeline);
            serialized.FindProperty("m_MainLightShadowsSupported").boolValue = true;
            serialized.FindProperty("m_SoftShadowsSupported").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return pipeline;
        }

        static void SetUpEnvironment()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = false;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = Palette.Night;

            var moon = new GameObject("Moon").AddComponent<Light>();
            moon.type = LightType.Directional;
            moon.color = MoonLight;
            moon.intensity = 1.2f;
            moon.shadows = LightShadows.Soft;
            moon.shadowStrength = 0.85f;
            moon.transform.rotation = Quaternion.Euler(50f, 60f, 0f);
            RenderSettings.sun = moon;
        }

        static void BuildIsland(Transform garden)
        {
            var grassMap = AssetDatabase.LoadAssetAtPath<Texture2D>(GrassPath);
            Material grass = MakePaint("Grass", grassMap != null ? Color.white : Palette.Grass);
            if (grassMap != null)
            {
                grass.SetTexture("_BaseMap", grassMap);
                grass.SetTextureScale("_BaseMap", new Vector2(4f, 8f));
            }

            Part(PrimitiveType.Cylinder, "IslandEarth", garden, new Vector3(0f, -0.65f, 0f), new Vector3(7.4f, 0.6f, 16f), MakePaint("Earth", Earth));
            Part(PrimitiveType.Cylinder, "IslandGrass", garden, new Vector3(0f, -0.05f, 0f), new Vector3(7.8f, 0.1f, 16.4f), grass);
            Part(PrimitiveType.Capsule, "Path", garden, new Vector3(0f, 0.06f, -3f), new Vector3(1.1f, 4.2f, 0.04f), MakePaint("Path", PathSand, 1.5f), new Vector3(90f, 0f, 0f));

            Material stone = MakePaint("Stone", Stone, 1.5f);
            Part(PrimitiveType.Cylinder, "StoneA", garden, new Vector3(-1.0f, 0.07f, -4.2f), new Vector3(0.5f, 0.03f, 0.4f), stone, new Vector3(0f, 15f, 0f));
            Part(PrimitiveType.Cylinder, "StoneB", garden, new Vector3(-1.6f, 0.07f, -3.5f), new Vector3(0.46f, 0.03f, 0.38f), stone, new Vector3(0f, -20f, 0f));
            Part(PrimitiveType.Cylinder, "StoneC", garden, new Vector3(-2.1f, 0.07f, -2.7f), new Vector3(0.5f, 0.03f, 0.42f), stone, new Vector3(0f, 30f, 0f));

            Material bush = MakePaint("TeaBush", TeaBush);
            Part(PrimitiveType.Sphere, "BushA", garden, new Vector3(-2.2f, 0.3f, 1.5f), new Vector3(1.1f, 0.8f, 1.0f), bush);
            Part(PrimitiveType.Sphere, "BushB", garden, new Vector3(-2.5f, 0.25f, -0.4f), new Vector3(0.9f, 0.65f, 0.85f), bush);
            Part(PrimitiveType.Sphere, "BushC", garden, new Vector3(2.3f, 0.3f, -1.5f), new Vector3(1.0f, 0.75f, 0.95f), bush);
            Part(PrimitiveType.Sphere, "BushD", garden, new Vector3(2.0f, 0.25f, -4.5f), new Vector3(0.8f, 0.6f, 0.8f), bush);
            Part(PrimitiveType.Sphere, "BushE", garden, new Vector3(-1.8f, 0.25f, -6.0f), new Vector3(0.8f, 0.6f, 0.75f), bush);
        }

        static Vector3 BuildStation(Transform garden, Vector3 centre)
        {
            const float top = 0.23f;
            Transform station = Node("TeaStation", garden, centre);
            Material wood = MakePaint("Wood", Wood);
            Material iron = MakePaint("Iron", Iron);
            Material teapot = MakePaint("Teapot", Teal);
            Part(PrimitiveType.Cylinder, "Deck", station, new Vector3(0f, 0.14f, 0f), new Vector3(2.4f, 0.09f, 2.4f), wood);

            Transform brazier = Node("Brazier", station, new Vector3(0.45f, top, 0.15f));
            Part(PrimitiveType.Cylinder, "Bowl", brazier, new Vector3(0f, 0.14f, 0f), new Vector3(0.46f, 0.14f, 0.46f), iron);
            Part(PrimitiveType.Sphere, "Coals", brazier, new Vector3(0f, 0.28f, 0f), new Vector3(0.36f, 0.08f, 0.36f), MakePaint("Coals", Palette.Carrot, 1f, Palette.Carrot * 2.5f));

            Transform pot = Node("Teapot", brazier, new Vector3(0f, 0.47f, 0f));
            Part(PrimitiveType.Sphere, "Body", pot, Vector3.zero, new Vector3(0.44f, 0.38f, 0.44f), teapot);
            Part(PrimitiveType.Cylinder, "Spout", pot, new Vector3(0.25f, 0.05f, 0f), new Vector3(0.07f, 0.12f, 0.07f), teapot, new Vector3(0f, 0f, -55f));
            Part(PrimitiveType.Cylinder, "Lid", pot, new Vector3(0f, 0.18f, 0f), new Vector3(0.22f, 0.025f, 0.22f), teapot);
            Part(PrimitiveType.Sphere, "Knob", pot, new Vector3(0f, 0.22f, 0f), new Vector3(0.08f, 0.08f, 0.08f), teapot);
            Part(PrimitiveType.Capsule, "Handle", pot, new Vector3(-0.24f, 0.02f, 0f), new Vector3(0.06f, 0.1f, 0.06f), teapot, new Vector3(0f, 0f, 15f));

            Transform post = Node("LanternPost", station, new Vector3(-0.85f, top, 0.75f));
            Part(PrimitiveType.Cylinder, "Pole", post, new Vector3(0f, 0.6f, 0f), new Vector3(0.07f, 0.6f, 0.07f), wood);
            Part(PrimitiveType.Cylinder, "Arm", post, new Vector3(0.15f, 1.18f, 0f), new Vector3(0.04f, 0.16f, 0.04f), wood, new Vector3(0f, 0f, 90f));
            Part(PrimitiveType.Cylinder, "Cap", post, new Vector3(0.28f, 1.13f, 0f), new Vector3(0.14f, 0.02f, 0.14f), iron);
            Part(PrimitiveType.Sphere, "Lantern", post, new Vector3(0.28f, 0.98f, 0f), new Vector3(0.22f, 0.28f, 0.22f), MakePaint("LanternPaper", Palette.Lantern, 1.5f, Palette.Lantern * 1.8f));

            var glow = Node("LanternLight", post, new Vector3(0.28f, 0.98f, 0f)).gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.color = Palette.Lantern;
            glow.range = 3f;
            glow.intensity = 2.5f;
            glow.shadows = LightShadows.None;

            return centre + new Vector3(0f, top, 0f);
        }

        static Animator BuildRabbit(Transform garden, Vector3 feet)
        {
            Material fur = MakePaint("Fur", Palette.Paper);
            Material innerEar = MakePaint("InnerEar", Palette.Blush, 0f);
            Material eye = MakePaint("Eye", Palette.Ink, 0f);

            Transform rabbit = Node("Rabbit", garden, feet, new Vector3(0f, -25f, 0f));
            Transform body = Node("Body", rabbit, Vector3.zero);
            Part(PrimitiveType.Capsule, "Torso", body, new Vector3(0f, 0.22f, 0f), new Vector3(0.38f, 0.22f, 0.34f), fur);
            Part(PrimitiveType.Sphere, "FootL", body, new Vector3(-0.1f, 0.03f, -0.1f), new Vector3(0.12f, 0.06f, 0.16f), fur);
            Part(PrimitiveType.Sphere, "FootR", body, new Vector3(0.1f, 0.03f, -0.1f), new Vector3(0.12f, 0.06f, 0.16f), fur);
            Part(PrimitiveType.Sphere, "Tail", body, new Vector3(0f, 0.14f, 0.18f), new Vector3(0.13f, 0.13f, 0.13f), fur);

            Transform head = Node("Head", body, new Vector3(0f, 0.55f, -0.02f));
            Part(PrimitiveType.Sphere, "Skull", head, Vector3.zero, new Vector3(0.4f, 0.35f, 0.36f), fur);
            Part(PrimitiveType.Sphere, "EyeL", head, new Vector3(-0.085f, 0.02f, -0.165f), new Vector3(0.05f, 0.06f, 0.03f), eye);
            Part(PrimitiveType.Sphere, "EyeR", head, new Vector3(0.085f, 0.02f, -0.165f), new Vector3(0.05f, 0.06f, 0.03f), eye);
            Part(PrimitiveType.Sphere, "Nose", head, new Vector3(0f, -0.035f, -0.178f), new Vector3(0.035f, 0.025f, 0.02f), innerEar);
            Part(PrimitiveType.Sphere, "CheekL", head, new Vector3(-0.12f, -0.05f, -0.14f), new Vector3(0.07f, 0.04f, 0.02f), innerEar);
            Part(PrimitiveType.Sphere, "CheekR", head, new Vector3(0.12f, -0.05f, -0.14f), new Vector3(0.07f, 0.04f, 0.02f), innerEar);
            Ear(head, "EarL", -1f, fur, innerEar);
            Ear(head, "EarR", 1f, fur, innerEar);

            return rabbit.gameObject.AddComponent<Animator>();
        }

        static void Ear(Transform head, string name, float side, Material fur, Material inner)
        {
            Transform ear = Node(name, head, new Vector3(0.08f * side, 0.12f, 0.02f), new Vector3(0f, 0f, -EarTilt * side));
            Part(PrimitiveType.Capsule, "Outer", ear, new Vector3(0f, 0.17f, 0f), new Vector3(0.1f, 0.17f, 0.06f), fur);
            Part(PrimitiveType.Capsule, "Inner", ear, new Vector3(0f, 0.17f, -0.022f), new Vector3(0.055f, 0.13f, 0.025f), inner);
        }

        static AnimationClip CreateIdle(Animator rabbit)
        {
            var clip = new AnimationClip { name = "Idle", frameRate = 30f };
            Key(clip, "Body", "m_LocalScale.x", 1f, 1f);
            Key(clip, "Body", "m_LocalScale.y", 1f, 1.03f);
            Key(clip, "Body", "m_LocalScale.z", 1f, 1f);
            foreach (var (path, side) in new[] { ("Body/Head/EarL", -1f), ("Body/Head/EarR", 1f) })
            {
                Key(clip, path, "localEulerAnglesRaw.x", 0f, 0f);
                Key(clip, path, "localEulerAnglesRaw.y", 0f, 0f);
                Key(clip, path, "localEulerAnglesRaw.z", -EarTilt * side, -(EarTilt + EarSway) * side);
            }

            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, Generated + "/RabbitIdle.anim");
            rabbit.runtimeAnimatorController = AnimatorController.CreateAnimatorControllerAtPathWithClip(Generated + "/Rabbit.controller", clip);
            return clip;
        }

        static void Key(AnimationClip clip, string path, string property, float rest, float peak)
        {
            var curve = new AnimationCurve(new Keyframe(0f, rest), new Keyframe(Breath / 2f, peak), new Keyframe(Breath, rest));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
        }

        static void BuildPostFx()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Generated + "/Spike3DPostFx.asset");
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 1f;
            bloom.intensity.value = 0.8f;
            bloom.scatter.value = 0.6f;
            var vignette = profile.Add<Vignette>(true);
            vignette.intensity.value = 0.2f;
            vignette.color.value = Palette.Night;
            foreach (var component in profile.components) AssetDatabase.AddObjectToAsset(component, profile);

            var volume = new GameObject("PostFx").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;
        }

        static Camera BuildCamera()
        {
            var cam = new GameObject("Camera").AddComponent<Camera>();
            cam.gameObject.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Palette.Night;
            cam.fieldOfView = 25f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 200f;
            cam.allowHDR = true;
            cam.allowMSAA = true;
            cam.aspect = Width / (float)Height;
            cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            return cam;
        }

        static void Shoot(Camera cam, Vector3 focus, float distance, string name)
        {
            Vector3 forward = Quaternion.Euler(Pitch, 0f, 0f) * Vector3.forward;
            cam.transform.SetPositionAndRotation(focus - forward * distance, Quaternion.LookRotation(forward));

            var rt = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(ShotFolder, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        static void ApplyLook(Look look, Shader toon)
        {
            var lit = Shader.Find(LitName);
            foreach (var paint in Paints)
            {
                Material m = paint.Material;
                m.shader = look == Look.Toon ? toon : lit;
                m.SetColor("_EmissionColor", paint.Emission);
                if (look == Look.Toon)
                {
                    m.SetColor("_ShadeColor", Palette.Night);
                    m.SetColor("_OutlineColor", Palette.Ink);
                    m.SetFloat("_OutlineWidth", paint.Outline);
                }
                else
                {
                    m.SetFloat("_Smoothness", 0.1f);
                    if (paint.Emission.maxColorComponent > 0f) m.EnableKeyword("_EMISSION");
                }
                EditorUtility.SetDirty(m);
            }
        }

        static Material MakePaint(string name, Color color, float outline = 2f, Color emission = default)
        {
            var material = new Material(Shader.Find(LitName)) { name = name };
            material.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(material, Generated + "/" + name + ".mat");
            Paints.Add(new Paint { Material = material, Emission = emission, Outline = outline });
            return material;
        }

        static Transform Node(string name, Transform parent, Vector3 position, Vector3 euler = default)
        {
            var node = new GameObject(name).transform;
            node.SetParent(parent, false);
            node.localPosition = position;
            node.localEulerAngles = euler;
            return node;
        }

        static Transform Part(PrimitiveType shape, string name, Transform parent, Vector3 position, Vector3 scale, Material material, Vector3 euler = default)
        {
            var part = GameObject.CreatePrimitive(shape);
            part.name = name;
            Transform t = part.transform;
            t.SetParent(parent, false);
            t.localPosition = position;
            t.localEulerAngles = euler;
            t.localScale = scale;
            part.GetComponent<MeshRenderer>().sharedMaterial = material;
            return t;
        }
    }
}

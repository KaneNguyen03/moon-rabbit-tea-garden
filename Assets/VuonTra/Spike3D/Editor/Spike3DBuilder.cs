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
        const string ScenePath = Root + "/Garden25DSpike.unity";
        const string Final = "Assets/VuonTra/Art/Final/";
        const string Placeholder = "Assets/VuonTra/Art/Placeholder/";
        const string PathArt = Root + "/Path/";
        const string GrassPath = "Assets/VuonTra/Art/Ground/bg_grass.png";
        const string ShotFolder = "Logs/spike3d";
        const int Width = 1080;
        const int Height = 1920;
        const float Pitch = 40f;
        const float Breath = 2.4f;
        const float IslandX = 3.3f;
        const float IslandZ = 4.8f;
        const int RimSteps = 96;
        const float GrassTileMetres = 1.4f;
        const int FlatQueue = 2460;

        static readonly Vector3 GardenFocus = new Vector3(0f, 0.3f, 0.2f);
        const float GardenDistance = 28f;
        static readonly Vector3 StationFocus = new Vector3(-0.3f, 0.5f, 0.6f);
        const float StationDistance = 8f;

        static readonly Color Earth = Palette.Hex("7A5238");
        static readonly Color EarthDeep = Palette.Mix(Earth, Palette.Night, 0.45f);
        static readonly Color MoonLight = new Color(0.82f, 0.9f, 1f);
        static readonly float CardStretch = 1f / Mathf.Cos(Pitch * Mathf.Deg2Rad);
        static readonly float PaintedFlatStretch = 1f / Mathf.Sin(Pitch * Mathf.Deg2Rad);

        static Shader _sprite;
        static Mesh _card;
        static Mesh _flat;
        static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

        public static void BuildAndShoot()
        {
            Materials.Clear();
            Directory.CreateDirectory(ShotFolder);
            AssetDatabase.DeleteAsset(Generated);
            AssetDatabase.CreateFolder(Root, "Generated");

            var pipeline = CreatePipeline();
            var previousDefault = GraphicsSettings.defaultRenderPipeline;
            var previousQuality = QualitySettings.renderPipeline;
            int previousMsaa = QualitySettings.antiAliasing;
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
                QualitySettings.antiAliasing = previousMsaa;
            }
        }

        static void BuildSceneAndShoot()
        {
            _sprite = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Spike3DSprite.shader");
            foreach (var shader in new[] { _sprite, AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Spike3DToon.shader"), AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Spike3DHalo.shader") })
                if (ShaderUtil.ShaderHasError(shader)) Debug.LogError("Spike3D: shader failed to compile: " + shader.name);
            _card = Quad("Card", new Vector3(-0.5f, 0f, 0f), Vector3.right, Vector3.up);
            _flat = Quad("Flat", new Vector3(-0.5f, 0f, -0.5f), Vector3.right, Vector3.forward);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SetUpEnvironment();
            var garden = new GameObject("Garden").transform;
            BuildIsland(garden);

            Flat(garden, "Deck", Final + "pad_san-go.png", new Vector3(-0.3f, 0.03f, 0.6f), 2.7f, 0f, true, FlatQueue + 5);
            Flat(garden, "Pond", Placeholder + "decor_pond.png", new Vector3(1.55f, 0.02f, -1.5f), 2.3f, 0f, true, FlatQueue);
            Flat(garden, "PathEnd", PathArt + "loi_dau.png", new Vector3(-0.2f, 0.015f, -3.2f), 2.0f, 90f, false, FlatQueue);
            Flat(garden, "PathMid", PathArt + "loi_thang.png", new Vector3(-0.2f, 0.016f, -1.25f), 2.0f, 90f, false, FlatQueue + 1);

            Transform rabbit = Card(garden, "Rabbit", Final + "rabbit_am-tra.png", new Vector3(-0.7f, 0.03f, 0.55f), 0.85f);
            Card(garden, "Teapot", Final + "prop_am-tra.png", new Vector3(0.15f, 0.03f, 0.8f), 0.8f);
            Transform lantern = Card(garden, "LanternPost", Placeholder + "decor_lantern.png", new Vector3(-1.75f, 0f, 0.95f), 1.8f);
            Card(garden, "Stall", Final + "prop_tiem.png", new Vector3(1.4f, 0f, 2.5f), 1.6f);
            Card(garden, "StallRabbit", Final + "kind_tho-matcha.png", new Vector3(0.45f, 0f, 1.95f), 0.75f);
            Card(garden, "FlowerBox", Final + "decor_bon-hoa.png", new Vector3(1.25f, 0f, 0.35f), 0.5f);
            Card(garden, "StoneLantern", Final + "decor_den-da.png", new Vector3(-0.9f, 0f, 3.9f), 0.9f);
            Card(garden, "TreeBack", Placeholder + "decor_tree.png", new Vector3(-2.2f, 0f, 2.9f), 2.4f);
            Card(garden, "TreeFront", Placeholder + "decor_tree.png", new Vector3(-2.0f, 0f, -3.5f), 1.9f);
            Card(garden, "BushLeft", Placeholder + "decor_bush.png", new Vector3(-2.5f, 0f, 1.0f), 0.6f);
            Card(garden, "BushFront", Placeholder + "decor_bush.png", new Vector3(2.0f, 0f, -3.5f), 0.6f);
            Card(garden, "BushRight", Placeholder + "decor_bush.png", new Vector3(2.6f, 0f, 0.9f), 0.55f);
            Card(garden, "BushBack", Placeholder + "decor_bush.png", new Vector3(0.6f, 0f, 3.9f), 0.5f);
            Card(garden, "StonesPath", Placeholder + "decor_stones.png", new Vector3(1.0f, 0f, -3.1f), 0.32f);
            Card(garden, "StonesLeft", Placeholder + "decor_stones.png", new Vector3(-1.3f, 0f, -2.0f), 0.28f);
            Card(garden, "Mushrooms", Placeholder + "decor_mushrooms.png", new Vector3(-2.2f, 0f, -0.9f), 0.45f);

            CreateIdle(rabbit);
            Transform halo = BuildLantern(lantern);
            BuildPostFx();
            Camera cam = BuildCamera();

            Shoot(cam, halo, StationFocus, StationDistance, "station25d");
            Shoot(cam, halo, GardenFocus, GardenDistance, "garden25d");

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
            pipeline.shadowDistance = 50f;
            pipeline.shadowCascadeCount = 1;
            pipeline.mainLightShadowmapResolution = 4096;
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
            moon.shadowStrength = 1f;
            moon.transform.rotation = Quaternion.Euler(45f, 120f, 0f);
            RenderSettings.sun = moon;
        }

        static void BuildIsland(Transform garden)
        {
            var toon = AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Spike3DToon.shader");
            var grass = new Material(toon) { name = "IslandGrass" };
            grass.SetTexture("_BaseMap", Imported(GrassPath, TextureWrapMode.Repeat));
            grass.SetTextureScale("_BaseMap", Vector2.one / GrassTileMetres);
            grass.SetFloat("_Threshold", 0.1f);
            Save(grass);
            var earth = new Material(toon) { name = "IslandEarth" };
            earth.SetFloat("_Threshold", 0.1f);
            Save(earth);

            Rim[] top =
            {
                new Rim(0f, 0f, 0f, 0f),
                new Rim(0.09f, -0.07f, 0f, 0f),
                new Rim(0.06f, -0.12f, 0.07f, 0f),
                new Rim(-0.05f, -0.12f, 0f, 0f),
            };
            Rim[] side =
            {
                new Rim(-0.05f, -0.1f, 0f, 0f),
                new Rim(-0.15f, -0.55f, 0f, 0.3f),
                new Rim(-0.3f, -1.2f, 0f, 1f),
                new Rim(-0.9f, -1.75f, 0f, 1f),
            };
            Solid(garden, "IslandTop", top, 0f, grass, false);
            Solid(garden, "IslandEarth", side, -1.85f, earth, true);
        }

        readonly struct Rim
        {
            public readonly float Grow;
            public readonly float Height;
            public readonly float Drip;
            public readonly float Deep;

            public Rim(float grow, float height, float drip, float deep)
            {
                Grow = grow;
                Height = height;
                Drip = drip;
                Deep = deep;
            }
        }

        static void Solid(Transform garden, string name, Rim[] rims, float capHeight, Material material, bool capBelow)
        {
            var vertices = new List<Vector3> { new Vector3(0f, capHeight, 0f) };
            var colors = new List<Color> { capBelow ? EarthDeep.linear : Color.white };
            var triangles = new List<int>();
            foreach (var rim in rims)
            {
                for (int i = 0; i < RimSteps; i++)
                {
                    float angle = 2f * Mathf.PI * i / RimSteps;
                    float wobble = 1f + 0.025f * Mathf.Sin(5f * angle) + 0.015f * Mathf.Sin(9f * angle + 1.3f);
                    float drop = rim.Drip * Mathf.Abs(Mathf.Sin(7f * angle));
                    vertices.Add(new Vector3((IslandX * wobble + rim.Grow) * Mathf.Cos(angle), rim.Height - drop, (IslandZ * wobble + rim.Grow) * Mathf.Sin(angle)));
                    colors.Add(capBelow ? Palette.Mix(Earth, EarthDeep, rim.Deep).linear : Color.white);
                }
            }

            int ringStart(int ring) => 1 + ring * RimSteps;
            int capRing = capBelow ? rims.Length - 1 : 0;
            for (int i = 0; i < RimSteps; i++)
            {
                int a = ringStart(capRing) + i;
                int b = ringStart(capRing) + (i + 1) % RimSteps;
                triangles.AddRange(capBelow ? new[] { 0, a, b } : new[] { 0, b, a });
            }
            for (int ring = 0; ring + 1 < rims.Length; ring++)
            {
                for (int i = 0; i < RimSteps; i++)
                {
                    int upper = ringStart(ring) + i;
                    int upperNext = ringStart(ring) + (i + 1) % RimSteps;
                    int lower = ringStart(ring + 1) + i;
                    int lowerNext = ringStart(ring + 1) + (i + 1) % RimSteps;
                    triangles.AddRange(new[] { upper, upperNext, lowerNext, upper, lowerNext, lower });
                }
            }

            var uvs = new List<Vector2>();
            foreach (var v in vertices) uvs.Add(new Vector2(v.x, v.z));
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetColors(colors);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Generated + "/" + name + ".asset");

            var go = new GameObject(name);
            go.transform.SetParent(garden, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        static Transform Card(Transform garden, string name, string texturePath, Vector3 feet, float height)
        {
            Texture2D texture = Imported(texturePath, TextureWrapMode.Clamp);
            var root = new GameObject(name).transform;
            root.SetParent(garden, false);
            root.localPosition = feet;
            var breath = new GameObject("Breath").transform;
            breath.SetParent(root, false);
            var card = new GameObject("Card");
            card.transform.SetParent(breath, false);
            card.transform.localScale = new Vector3(height * texture.width / texture.height, height * CardStretch, 1f);
            card.AddComponent<MeshFilter>().sharedMesh = _card;
            var renderer = card.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SpriteMaterial(texture, false);
            renderer.shadowCastingMode = ShadowCastingMode.On;
            return root;
        }

        static void Flat(Transform garden, string name, string texturePath, Vector3 centre, float width, float yaw, bool paintedInPerspective, int queue)
        {
            Texture2D texture = Imported(texturePath, TextureWrapMode.Clamp);
            float depth = width * texture.height / texture.width * (paintedInPerspective ? PaintedFlatStretch : 1f);
            var flat = new GameObject(name);
            flat.transform.SetParent(garden, false);
            flat.transform.localPosition = centre;
            flat.transform.localEulerAngles = new Vector3(0f, yaw, 0f);
            flat.transform.localScale = new Vector3(width, 1f, depth);
            flat.AddComponent<MeshFilter>().sharedMesh = _flat;
            var renderer = flat.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = SpriteMaterial(texture, true);
            renderer.sharedMaterial.renderQueue = queue;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        static Material SpriteMaterial(Texture2D texture, bool flat)
        {
            string key = texture.name + (flat ? "_flat" : "_card");
            if (Materials.TryGetValue(key, out var cached)) return cached;
            var material = new Material(_sprite) { name = key };
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_ReceiveShadow", flat ? 1f : 0f);
            material.SetColor("_ShadeColor", Palette.Night);
            material.SetFloat("_LanternGain", 0.35f);
            Save(material);
            Materials[key] = material;
            return material;
        }

        static Texture2D Imported(string path, TextureWrapMode wrap)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (!importer.mipmapEnabled || importer.wrapMode != wrap)
            {
                importer.mipmapEnabled = true;
                importer.wrapMode = wrap;
                importer.anisoLevel = 4;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Mesh Quad(string name, Vector3 corner, Vector3 across, Vector3 up)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(new List<Vector3> { corner, corner + across, corner + up, corner + across + up });
            mesh.SetUVs(0, new List<Vector2> { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f) });
            mesh.SetTriangles(new[] { 0, 2, 1, 1, 2, 3 }, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh, Generated + "/" + name + "Quad.asset");
            return mesh;
        }

        static void CreateIdle(Transform rabbit)
        {
            var clip = new AnimationClip { name = "Idle", frameRate = 30f };
            Key(clip, "m_LocalScale.x", 1f, 1f);
            Key(clip, "m_LocalScale.y", 1f, 1.03f);
            Key(clip, "m_LocalScale.z", 1f, 1f);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, Generated + "/RabbitIdle.anim");
            rabbit.gameObject.AddComponent<Animator>().runtimeAnimatorController =
                AnimatorController.CreateAnimatorControllerAtPathWithClip(Generated + "/Rabbit.controller", clip);
        }

        static void Key(AnimationClip clip, string property, float rest, float peak)
        {
            var curve = new AnimationCurve(new Keyframe(0f, rest), new Keyframe(Breath / 2f, peak), new Keyframe(Breath, rest));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("Breath", typeof(Transform), property), curve);
        }

        static Transform BuildLantern(Transform post)
        {
            Transform card = post.Find("Breath/Card");
            Vector3 head = post.position + new Vector3((0.71f - 0.5f) * card.localScale.x, 0.625f * card.localScale.y, -0.1f);

            var glow = new GameObject("LanternLight").AddComponent<Light>();
            glow.transform.SetParent(post, true);
            glow.transform.position = head;
            glow.type = LightType.Point;
            glow.color = Palette.Lantern;
            glow.range = 3.5f;
            glow.intensity = 3f;
            glow.shadows = LightShadows.None;

            var halo = new Material(AssetDatabase.LoadAssetAtPath<Shader>(Root + "/Spike3DHalo.shader")) { name = "LanternHalo" };
            halo.SetTexture("_BaseMap", Imported(Placeholder + "glow.png", TextureWrapMode.Clamp));
            halo.SetColor("_BaseColor", Palette.Lantern);
            halo.SetFloat("_Intensity", 0.6f);
            Save(halo);

            var quad = new GameObject("LanternHalo");
            quad.transform.SetParent(post, true);
            quad.transform.position = head;
            quad.transform.localScale = new Vector3(1.6f, 1.6f, 1f);
            var flat = Quad("Halo", new Vector3(-0.5f, -0.5f, 0f), Vector3.right, Vector3.up);
            quad.AddComponent<MeshFilter>().sharedMesh = flat;
            var renderer = quad.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = halo;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            return quad.transform;
        }

        static void BuildPostFx()
        {
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, Generated + "/Spike3DPostFx.asset");
            profile.Add<Tonemapping>(true).mode.value = TonemappingMode.Neutral;
            var bloom = profile.Add<Bloom>(true);
            bloom.threshold.value = 1f;
            bloom.intensity.value = 1f;
            bloom.scatter.value = 0.65f;
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

        static void Shoot(Camera cam, Transform halo, Vector3 focus, float distance, string name)
        {
            Quaternion look = Quaternion.Euler(Pitch, 0f, 0f);
            cam.transform.SetPositionAndRotation(focus - look * Vector3.forward * distance, look);
            halo.rotation = look;

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

        static void Save(Material material)
        {
            AssetDatabase.CreateAsset(material, Generated + "/" + material.name + ".mat");
        }
    }
}

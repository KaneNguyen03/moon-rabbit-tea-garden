using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.PSD;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

public static class CreamRig
{
    const string PsbPath = "Assets/Cream/Breeds/Cream.psb";
    const string BreedFolder = "Assets/Cream/Breeds";
    const string Folder = "Assets/Cream";
    const string PotPath = "Assets/Cream/Props/prop_tea_pot.png";
    const string PuffPath = "Assets/Cream/Props/steam_puff.png";
    const string SteamMaterialPath = "Assets/Cream/Props/Steam.mat";
    const string PrefabPath = "Assets/Cream/BunnyCharacter.prefab";
    const int Canvas = 512;
    const float Ppu = 100f;

    static readonly Vector2 PotMouth = new Vector2(215, 404);
    const float PotWidthPx = 150f;
    const float PotMouthFromBottom = 0.655f;

    struct BoneSpec
    {
        public string Name;
        public int Parent;
        public Vector2 From;
        public Vector2 To;
    }

    static readonly BoneSpec[] Bones =
    {
        new BoneSpec { Name = "Root", Parent = -1, From = new Vector2(256, 461), To = new Vector2(256, 400) },
        new BoneSpec { Name = "Spine", Parent = 0, From = new Vector2(256, 400), To = new Vector2(238, 288) },
        new BoneSpec { Name = "Neck", Parent = 1, From = new Vector2(235.6f, 279.6f), To = new Vector2(236, 170) },
        new BoneSpec { Name = "Ears", Parent = 2, From = new Vector2(277, 150), To = new Vector2(270, 50) },
        new BoneSpec { Name = "Shoulder", Parent = 1, From = new Vector2(301.6f, 307.7f), To = new Vector2(300, 365) },
    };

    const int Root = 0, Spine = 1, Neck = 2, Ears = 3, Shoulder = 4;

    static readonly string[] Arms = { "ArmIdle", "ArmBrew", "ArmBake", "ArmGather", "ArmStargaze", "ArmDoze" };

    static readonly (string category, string[] labels)[] Groups =
    {
        (BunnyCharacter.OutfitCategory, new[] { "Apron" }),
        (BunnyCharacter.HeadCategory, new[] { "HeadAwake", "HeadDoze" }),
        (BunnyCharacter.HatCategory, new[] { "Crown", "Beanie", "NonLa", "Bow" }),
        (BunnyCharacter.ArmCategory, Arms),
    };

    static readonly (string name, string file, bool lop)[] Breeds =
    {
        ("Cream", "Cream", false),
        ("BrownLop", "BrownLop", true),
        ("StarryNight", "StarryNight", false),
        ("Spotted", "Spotted", false),
    };

    const string BodyCategory = "Body", EarsBackCategory = "EarsBack", EarsLopCategory = "EarsLop";

    static Vector2 ToCharacter(Vector2 psd) => new Vector2(psd.x, Canvas - psd.y);

    static float WorldAngle(BoneSpec bone)
    {
        Vector2 d = ToCharacter(bone.To) - ToCharacter(bone.From);
        return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
    }

    static SpriteBone[] SkeletonIn(Vector2 origin)
    {
        var result = new SpriteBone[Bones.Length];
        for (int i = 0; i < Bones.Length; i++)
        {
            BoneSpec bone = Bones[i];
            Vector2 world = ToCharacter(bone.From);
            float angle = WorldAngle(bone);
            Vector2 local = world - origin;
            float localAngle = angle;
            if (bone.Parent >= 0)
            {
                BoneSpec parent = Bones[bone.Parent];
                float parentAngle = WorldAngle(parent);
                local = Quaternion.Euler(0, 0, -parentAngle) * (world - ToCharacter(parent.From));
                localAngle = angle - parentAngle;
            }
            result[i] = new SpriteBone
            {
                name = bone.Name,
                guid = GUID.Generate().ToString(),
                position = new Vector3(local.x, local.y, 0),
                rotation = Quaternion.Euler(0, 0, localAngle),
                length = Vector2.Distance(bone.From, bone.To),
                parentId = bone.Parent,
                color = Color.HSVToRGB(i / (float)Bones.Length, 0.7f, 1f),
            };
        }
        return result;
    }

    static BoneWeight Single(int bone) => new BoneWeight { boneIndex0 = bone, weight0 = 1f };

    static BoneWeight WeightFor(string layer, Vector2 psd)
    {
        switch (layer)
        {
            case "Body_Full":
            case "Apron":
                float towardRoot = Mathf.Clamp01((psd.y - 380f) / 60f);
                if (towardRoot <= 0f) return Single(Spine);
                if (towardRoot >= 1f) return Single(Root);
                return new BoneWeight { boneIndex0 = Spine, weight0 = 1f - towardRoot, boneIndex1 = Root, weight1 = towardRoot };
            case "Ears_Back":
            case "Ears_Lop":
                return Single(Ears);
            default:
                return layer.StartsWith("Arm") ? Single(Shoulder) : Single(Neck);
        }
    }

    static void GridMesh(int width, int height, Vector2 spritePosition, string layer,
        out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges)
    {
        const float step = 10f;
        int cols = Mathf.Max(1, Mathf.CeilToInt(width / step));
        int rows = Mathf.Max(1, Mathf.CeilToInt(height / step));
        vertices = new Vertex2DMetaData[(cols + 1) * (rows + 1)];
        for (int r = 0; r <= rows; r++)
        for (int c = 0; c <= cols; c++)
        {
            var local = new Vector2(width * c / (float)cols, height * r / (float)rows);
            Vector2 character = local + spritePosition;
            vertices[r * (cols + 1) + c] = new Vertex2DMetaData
            {
                position = local,
                boneWeight = WeightFor(layer, new Vector2(character.x, Canvas - character.y)),
            };
        }
        var tris = new List<int>();
        for (int r = 0; r < rows; r++)
        for (int c = 0; c < cols; c++)
        {
            int a = r * (cols + 1) + c, b = a + 1, d = a + cols + 1, e = d + 1;
            tris.AddRange(new[] { a, d, b, b, d, e });
        }
        indices = tris.ToArray();
        var outline = new List<int>();
        for (int c = 0; c < cols; c++) outline.Add(c);
        for (int r = 0; r < rows; r++) outline.Add(r * (cols + 1) + cols);
        for (int c = cols; c > 0; c--) outline.Add(rows * (cols + 1) + c);
        for (int r = rows; r > 0; r--) outline.Add(r * (cols + 1));
        edges = outline.Select((v, i) => new Vector2Int(v, outline[(i + 1) % outline.Count])).ToArray();
    }

    static ISpriteEditorDataProvider ProviderFor(PSDImporter importer)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        return provider;
    }

    [MenuItem("Cream/1 Rig PSBs")]
    public static void RigAll()
    {
        foreach ((string _, string file, bool _) in Breeds) Rig($"{BreedFolder}/{file}.psb");
    }

    static void Rig(string psbPath)
    {
        var importer = (PSDImporter)AssetImporter.GetAtPath(psbPath);
        importer.useMosaicMode = true;
        importer.useCharacterMode = true;
        importer.spritePixelsPerUnit = Ppu;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.SaveAndReimport();

        var serialized = new SerializedObject(importer);
        SerializedProperty categories = serialized.FindProperty("m_SpriteCategoryList.m_Categories");
        categories.ClearArray();
        SpriteRect[] named = ProviderFor(importer).GetSpriteRects();
        int written = 0;
        foreach ((string categoryName, string[] wanted) in Groups)
        {
            string[] present = wanted.Where(l => named.Any(r => r.name == l)).ToArray();
            if (present.Length == 0) continue;
            categories.InsertArrayElementAtIndex(written);
            SerializedProperty category = categories.GetArrayElementAtIndex(written++);
            category.FindPropertyRelative("m_Name").stringValue = categoryName;
            SerializedProperty labels = category.FindPropertyRelative("m_Labels");
            labels.ClearArray();
            for (int i = 0; i < present.Length; i++)
            {
                labels.InsertArrayElementAtIndex(i);
                SerializedProperty label = labels.GetArrayElementAtIndex(i);
                label.FindPropertyRelative("m_Name").stringValue = present[i];
                label.FindPropertyRelative("m_SpriteId").stringValue = named.First(r => r.name == present[i]).spriteID.ToString();
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        importer.SaveAndReimport();

        importer = (PSDImporter)AssetImporter.GetAtPath(psbPath);
        ISpriteEditorDataProvider provider = ProviderFor(importer);
        var characterProvider = provider.GetDataProvider<ICharacterDataProvider>();
        var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
        var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
        CharacterData character = characterProvider.GetCharacterData();
        SpriteRect[] rects = provider.GetSpriteRects();
        int[] allBones = Enumerable.Range(0, Bones.Length).ToArray();
        for (int i = 0; i < character.parts.Length; i++)
        {
            CharacterPart part = character.parts[i];
            SpriteRect rect = rects.First(r => r.spriteID.ToString() == part.spriteId);
            Vector2 origin = part.spritePosition.position;
            part.bones = allBones;
            character.parts[i] = part;
            boneProvider.SetBones(rect.spriteID, SkeletonIn(origin).ToList());
            GridMesh(part.spritePosition.width, part.spritePosition.height, origin, rect.name,
                out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges);
            meshProvider.SetVertices(rect.spriteID, vertices);
            meshProvider.SetIndices(rect.spriteID, indices);
            meshProvider.SetEdges(rect.spriteID, edges);
        }
        character.bones = SkeletonIn(Vector2.zero);
        characterProvider.SetCharacterData(character);
        provider.Apply();
        importer = (PSDImporter)AssetImporter.GetAtPath(psbPath);
        importer.SaveAndReimport();
        Debug.Log("[cream] rigged " + psbPath);
    }

    static Transform Find(Transform root, string name) =>
        root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)
        ?? throw new InvalidOperationException($"{name} missing under {root.name}");

    static void Key(AnimationClip clip, Transform root, string bone, string property, float duration, params float[] values)
    {
        Transform t = Find(root, bone);
        float rest = property switch
        {
            "localEulerAnglesRaw.z" => t.localEulerAngles.z,
            "m_LocalPosition.x" => t.localPosition.x,
            "m_LocalPosition.y" => t.localPosition.y,
            "m_LocalScale.x" => t.localScale.x,
            "m_LocalScale.y" => t.localScale.y,
            _ => 0f,
        };
        var keys = new Keyframe[values.Length];
        for (int i = 0; i < values.Length; i++)
            keys[i] = new Keyframe(duration * i / (values.Length - 1), rest + values[i]);
        var curve = new AnimationCurve(keys);
        for (int i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0f);
        string path = AnimationUtility.CalculateTransformPath(t, root);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
    }

    static void Active(AnimationClip clip, Transform root, string child, float duration, bool on)
    {
        string path = AnimationUtility.CalculateTransformPath(Find(root, child), root);
        var curve = AnimationCurve.Constant(0f, duration, on ? 1f : 0f);
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(GameObject), "m_IsActive"), curve);
    }

    static AnimationClip Save(AnimationClip clip, string name)
    {
        clip.name = name;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = $"{Folder}/{name}.anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static Vector3 CharacterPoint(Transform root, Vector2 psd)
    {
        Transform rootBone = Find(root, Bones[Root].Name);
        Vector2 offset = (ToCharacter(psd) - ToCharacter(Bones[Root].From)) / Ppu;
        return rootBone.position + (Vector3)offset;
    }

    static Material SteamMaterial()
    {
        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), Vector2.one * size / 2f) / (size / 2f);
            float a = Mathf.Clamp01(1f - d);
            texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
        }
        File.WriteAllBytes(PuffPath, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(PuffPath);
        var puffImporter = (TextureImporter)AssetImporter.GetAtPath(PuffPath);
        puffImporter.alphaIsTransparency = true;
        puffImporter.SaveAndReimport();
        var material = new Material(Shader.Find("Sprites/Default")) { mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(PuffPath) };
        AssetDatabase.DeleteAsset(SteamMaterialPath);
        AssetDatabase.CreateAsset(material, SteamMaterialPath);
        return material;
    }

    static GameObject Steam(Transform root)
    {
        var go = new GameObject("BrewSteam");
        go.transform.SetParent(root, false);
        go.transform.position = CharacterPoint(root, PotMouth + new Vector2(0, -6));
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.duration = 1.2f;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.4f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.26f, 0.4f);
        main.startColor = new Color(0.93f, 0.97f, 1f, 1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = -0.02f;
        main.maxParticles = 30;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 9f;
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 12f;
        shape.radius = 0.12f;
        shape.rotation = new Vector3(-90f, 0f, 0f);
        ParticleSystem.ColorOverLifetimeModule fade = ps.colorOverLifetime;
        fade.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(0.85f, 0.2f), new GradientAlphaKey(0f, 1f) });
        fade.color = gradient;
        ParticleSystem.SizeOverLifetimeModule grow = ps.sizeOverLifetime;
        grow.enabled = true;
        grow.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.8f));
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.12f;
        noise.frequency = 0.6f;
        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = SteamMaterial();
        renderer.sortingOrder = 50;
        go.SetActive(false);
        return go;
    }

    static SpriteResolver ResolverFor(Transform root, string category) =>
        root.GetComponentsInChildren<SpriteResolver>(true).First(r => r.GetCategory() == category);

    static Sprite[] SpritesOf(string psb) => AssetDatabase.LoadAllAssetsAtPath(psb).OfType<Sprite>().ToArray();

    static string LibraryPath(string breed) => $"{BreedFolder}/{breed}_Library.asset";

    static string BreedPath(string breed) => $"{BreedFolder}/{breed}.asset";

    [MenuItem("Cream/2 Breed Libraries")]
    public static void BuildBreeds()
    {
        Sprite[] shared = SpritesOf(PsbPath);
        foreach ((string name, string file, bool lop) in Breeds)
        {
            Sprite[] own = SpritesOf($"{BreedFolder}/{file}.psb");
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            void Add(Sprite[] from, string sprite, string category, string label)
            {
                Sprite found = from.FirstOrDefault(x => x.name == sprite);
                if (found == null) throw new InvalidOperationException($"{name}: sprite {sprite} missing");
                library.AddCategoryLabel(found, category, label);
            }
            Add(own, "Body_Full", BodyCategory, "Body_Full");
            Add(own, "Ears_Back", EarsBackCategory, "Ears_Back");
            Add(own.Any(x => x.name == "Ears_Lop") ? own : shared, "Ears_Lop", EarsLopCategory, "Ears_Lop");
            Add(own, "HeadAwake", BunnyCharacter.HeadCategory, "HeadAwake");
            Add(own, "HeadDoze", BunnyCharacter.HeadCategory, "HeadDoze");
            foreach (string arm in Arms) Add(own, arm, BunnyCharacter.ArmCategory, arm);
            foreach (string hat in Groups.First(g => g.category == BunnyCharacter.HatCategory).labels)
            {
                bool fitted = lop && own.Any(x => x.name == hat + "Lop");
                Add(fitted ? own : shared, fitted ? hat + "Lop" : hat, BunnyCharacter.HatCategory, hat);
            }
            Add(shared, "Apron", BunnyCharacter.OutfitCategory, "Apron");
            AssetDatabase.DeleteAsset(LibraryPath(name));
            AssetDatabase.CreateAsset(library, LibraryPath(name));
            var breed = ScriptableObject.CreateInstance<BunnyBreed>();
            breed.Configure(name, library, lop);
            AssetDatabase.DeleteAsset(BreedPath(name));
            AssetDatabase.CreateAsset(breed, BreedPath(name));
        }
        AssetDatabase.SaveAssets();
        Debug.Log("[cream] breed libraries saved");
    }

    static SpriteResolver AddResolver(Transform root, string child, string category, string label)
    {
        var resolver = Find(root, child).gameObject.AddComponent<SpriteResolver>();
        resolver.SetCategoryAndLabel(category, label);
        return resolver;
    }

    static readonly (string name, BunnyJob job, float length)[] Clips =
    {
        ("Idle_Breathe", BunnyJob.Idle, 2.4f),
        ("Brew_Stir", BunnyJob.Brew, 1.2f),
        ("Bake_Roll", BunnyJob.Bake, 1.2f),
        ("Gather_Pick", BunnyJob.Gather, 1.6f),
        ("Stargaze_Look", BunnyJob.Stargaze, 2.4f),
        ("Doze_Sleep", BunnyJob.Doze, 3.0f),
    };

    static AnimationClip MakeClip(Transform root, BunnyJob job, float d)
    {
        var c = new AnimationClip();
        switch (job)
        {
            case BunnyJob.Idle:
                Key(c, root, "Spine", "m_LocalScale.x", d, 0, 0.012f, 0);
                Key(c, root, "Spine", "m_LocalScale.y", d, 0, -0.018f, 0);
                Key(c, root, "Neck", "localEulerAnglesRaw.z", d, 0, -1.5f, 0);
                Key(c, root, "Ears", "localEulerAnglesRaw.z", d, 0, 2.5f, 0, -2f, 0);
                Key(c, root, "Shoulder", "localEulerAnglesRaw.z", d, 0, -2.5f, 0);
                break;
            case BunnyJob.Brew:
                Key(c, root, "Shoulder", "localEulerAnglesRaw.z", d, -4, 1, 5, 1, -4);
                Key(c, root, "Shoulder", "m_LocalPosition.x", d, 0, 0.015f, 0, -0.015f, 0);
                Key(c, root, "Shoulder", "m_LocalPosition.y", d, 0.012f, 0, -0.012f, 0, 0.012f);
                Key(c, root, "Spine", "localEulerAnglesRaw.z", d, 0, -1.5f, 0, 1.5f, 0);
                Key(c, root, "Spine", "m_LocalScale.y", d, 0, -0.01f, 0, -0.01f, 0);
                Key(c, root, "Neck", "localEulerAnglesRaw.z", d, 1.5f, 0, -2.5f, 0, 1.5f);
                Key(c, root, "Ears", "localEulerAnglesRaw.z", d, -3, 2, 5, -1, -3);
                break;
            case BunnyJob.Bake:
                Key(c, root, "Shoulder", "m_LocalPosition.x", d, 0, -0.035f, 0, 0.035f, 0);
                Key(c, root, "Shoulder", "m_LocalPosition.y", d, 0, 0);
                Key(c, root, "Shoulder", "localEulerAnglesRaw.z", d, 0, -3, 0, 3, 0);
                Key(c, root, "Spine", "localEulerAnglesRaw.z", d, 0, 2, 0, -1, 0);
                Key(c, root, "Spine", "m_LocalScale.y", d, 0, -0.014f, 0, -0.014f, 0);
                Key(c, root, "Neck", "localEulerAnglesRaw.z", d, 1, 2.5f, 1, -1, 1);
                Key(c, root, "Ears", "localEulerAnglesRaw.z", d, -2, 3, 5, 1, -2);
                break;
            case BunnyJob.Gather:
                Key(c, root, "Shoulder", "localEulerAnglesRaw.z", d, 0, 5, 8, 4, 0);
                Key(c, root, "Shoulder", "m_LocalPosition.x", d, 0, 0);
                Key(c, root, "Shoulder", "m_LocalPosition.y", d, 0, 0.02f, 0.03f, 0.01f, 0);
                Key(c, root, "Neck", "localEulerAnglesRaw.z", d, 0, 2, 3, 1, 0);
                Key(c, root, "Ears", "localEulerAnglesRaw.z", d, 0, 4, -3, 2, 0);
                Key(c, root, "Spine", "m_LocalScale.y", d, 0, -0.012f, 0, -0.012f, 0);
                break;
            case BunnyJob.Stargaze:
                Key(c, root, "Neck", "localEulerAnglesRaw.z", d, -3, -5, -6, -4, -3);
                Key(c, root, "Shoulder", "localEulerAnglesRaw.z", d, -3, -5, -6, -4, -3);
                Key(c, root, "Ears", "localEulerAnglesRaw.z", d, 2, 4, 6, 3, 2);
                Key(c, root, "Spine", "localEulerAnglesRaw.z", d, 0, -1, -1.5f, -1, 0);
                break;
            case BunnyJob.Doze:
                Key(c, root, "Neck", "localEulerAnglesRaw.z", d, 5, 8, 9, 7, 5);
                Key(c, root, "Ears", "localEulerAnglesRaw.z", d, -6, -9, -10, -8, -6);
                Key(c, root, "Spine", "m_LocalScale.y", d, 0, -0.025f, -0.03f, -0.015f, 0);
                Key(c, root, "Spine", "m_LocalScale.x", d, 0, 0.012f, 0.015f, 0.008f, 0);
                Key(c, root, "Shoulder", "localEulerAnglesRaw.z", d, 0, -2, -3, -2, 0);
                break;
        }
        Active(c, root, "BrewSteam", d, job == BunnyJob.Brew);
        return Save(c, Clips.First(x => x.job == job).name);
    }

    [MenuItem("Cream/3 Clips and Prefab")]
    public static void BuildPrefab()
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(PsbPath));
        PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        instance.name = "BunnyCharacter";
        Transform root = instance.transform;
        Steam(root);

        var cream = AssetDatabase.LoadAssetAtPath<BunnyBreed>(BreedPath("Cream"));
        SpriteLibrary library = instance.GetComponentInChildren<SpriteLibrary>(true);
        library.spriteLibraryAsset = cream.Library;
        AddResolver(root, "Body_Full", BodyCategory, "Body_Full");
        SpriteResolver earsBack = AddResolver(root, "Ears_Back", EarsBackCategory, "Ears_Back");
        SpriteResolver earsLop = AddResolver(root, "Ears_Lop", EarsLopCategory, "Ears_Lop");

        string controllerPath = $"{Folder}/BunnyCharacter.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Job", AnimatorControllerParameterType.Int);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        foreach ((string clipName, BunnyJob job, float length) in Clips)
        {
            AnimatorState state = machine.AddState(job.ToString());
            state.motion = MakeClip(root, job, length);
            if (job == BunnyJob.Idle) machine.defaultState = state;
            AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
            enter.AddCondition(AnimatorConditionMode.Equals, (int)job, "Job");
            enter.canTransitionToSelf = false;
            enter.hasExitTime = false;
            enter.duration = 0.18f;
        }

        instance.AddComponent<SortingGroup>();
        if (!instance.TryGetComponent(out Animator animator)) animator = instance.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        var bunny = instance.AddComponent<BunnyCharacter>();
        var wiring = new SerializedObject(bunny);
        wiring.FindProperty("animator").objectReferenceValue = animator;
        wiring.FindProperty("library").objectReferenceValue = library;
        wiring.FindProperty("armResolver").objectReferenceValue = ResolverFor(root, BunnyCharacter.ArmCategory);
        wiring.FindProperty("headResolver").objectReferenceValue = ResolverFor(root, BunnyCharacter.HeadCategory);
        wiring.FindProperty("hatResolver").objectReferenceValue = ResolverFor(root, BunnyCharacter.HatCategory);
        wiring.FindProperty("outfitResolver").objectReferenceValue = ResolverFor(root, BunnyCharacter.OutfitCategory);
        wiring.FindProperty("earsBack").objectReferenceValue = earsBack.GetComponent<SpriteRenderer>();
        wiring.FindProperty("earsLop").objectReferenceValue = earsLop.GetComponent<SpriteRenderer>();
        wiring.FindProperty("breed").objectReferenceValue = cream;
        wiring.ApplyModifiedPropertiesWithoutUndo();
        bunny.SetBreed(cream);
        bunny.SetHat(BunnyCharacter.Nothing);
        bunny.SetOutfit(BunnyCharacter.Nothing);

        AssetDatabase.DeleteAsset(PrefabPath);
        PrefabUtility.SaveAsPrefabAsset(instance, PrefabPath);
        UnityEngine.Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("[cream] prefab, clips and controller saved");
    }

    static void Deform()
    {
        Type manager = typeof(SpriteSkin).Assembly.GetType("UnityEngine.U2D.Animation.DeformationManager");
        object instance = manager.GetProperty("instance").GetValue(null);
        manager.GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            .Invoke(instance, null);
    }

    static string Hierarchy(Transform t, string indent = "")
    {
        var sb = new StringBuilder();
        string components = string.Join(", ", t.GetComponents<Component>().Where(c => c is not Transform).Select(c => c.GetType().Name));
        sb.AppendLine($"{indent}{t.name}{(components.Length > 0 ? "  [" + components + "]" : "")}");
        foreach (Transform child in t) sb.Append(Hierarchy(child, indent + "  "));
        return sb.ToString();
    }

    static void ImportPot()
    {
        var pot = (TextureImporter)AssetImporter.GetAtPath(PotPath);
        pot.textureType = TextureImporterType.Sprite;
        pot.spriteImportMode = SpriteImportMode.Single;
        pot.alphaIsTransparency = true;
        pot.mipmapEnabled = true;
        pot.filterMode = FilterMode.Trilinear;
        var settings = new TextureImporterSettings();
        pot.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.BottomCenter;
        pot.SetTextureSettings(settings);
        Texture2D source = AssetDatabase.LoadAssetAtPath<Texture2D>(PotPath);
        pot.spritePixelsPerUnit = source.width / PotWidthPx * Ppu;
        pot.SaveAndReimport();
    }

    [MenuItem("Cream/3 Capture")]
    public static void Capture()
    {
        ImportPot();
        string outDir = Path.GetFullPath("Captures/cream-frames");
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        File.WriteAllText(Path.GetFullPath("Captures/cream-hierarchy.txt"), Hierarchy(prefab.transform));
        Sprite potSprite = AssetDatabase.LoadAssetAtPath<Sprite>(PotPath);
        string[] hats = { BunnyCharacter.Nothing, "Crown", "Beanie", "NonLa", "Bow", BunnyCharacter.Nothing };
        var actors = new List<(GameObject go, AnimationClip clip, ParticleSystem steam)>();
        for (int row = 0; row < Breeds.Length; row++)
        for (int col = 0; col < Clips.Length; col++)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(col * 3.1f, -row * 5.0f, 0);
            go.GetComponent<Animator>().enabled = false;
            var bunny = go.GetComponent<BunnyCharacter>();
            bunny.SetBreed(AssetDatabase.LoadAssetAtPath<BunnyBreed>(BreedPath(Breeds[row].name)));
            BunnyJob job = Clips[col].job;
            bunny.SetJob(job);
            bunny.SetHat(hats[(col + row) % hats.Length]);
            bunny.SetOutfit((col + row) % 3 == 1 ? "Apron" : BunnyCharacter.Nothing);
            foreach (SpriteResolver resolver in go.GetComponentsInChildren<SpriteResolver>(true)) resolver.ResolveSpriteToSpriteRenderer();
            foreach (SpriteSkin skin in go.GetComponentsInChildren<SpriteSkin>(true))
            {
                skin.alwaysUpdate = true;
                skin.forceCpuDeformation = true;
            }
            if (job == BunnyJob.Brew)
            {
                var pot = new GameObject("TeaPot").AddComponent<SpriteRenderer>();
                pot.sprite = potSprite;
                pot.sortingOrder = 10;
                Vector3 mouth = CharacterPoint(go.transform, PotMouth);
                pot.transform.position = mouth - new Vector3(0, potSprite.bounds.size.y * PotMouthFromBottom, 0);
            }
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{Folder}/{Clips[col].name}.anim");
            actors.Add((go, clip, go.GetComponentInChildren<ParticleSystem>(true)));
        }

        Deform();
        Bounds bounds = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude)
            .Where(r => r is SpriteRenderer)
            .Select(r => r.bounds)
            .Aggregate((a, b) => { a.Encapsulate(b); return a; });
        var camera = new GameObject("CaptureCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = bounds.extents.y * 1.12f;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y + bounds.extents.y * 0.05f, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.239f, 0.514f, 0.412f, 1f);
        const int height = 1180;
        int width = Mathf.RoundToInt(height * bounds.size.x * 1.06f / (bounds.size.y * 1.12f)) / 2 * 2;
        camera.aspect = width / (float)height;
        Debug.Log($"[cream] bounds {bounds} frame {width}x{height}");
        var target = new RenderTexture(width, height, 24) { antiAliasing = 4 };
        camera.targetTexture = target;
        var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);
        const int fps = 24;
        const int frames = fps * 3;
        for (int f = 0; f < frames; f++)
        {
            float t = f / (float)fps;
            foreach ((GameObject go, AnimationClip clip, ParticleSystem steam) in actors)
            {
                clip.SampleAnimation(go, t % clip.length);
                if (steam.gameObject.activeInHierarchy) steam.Simulate(t + 3f, true, true);
            }
            Deform();
            camera.Render();
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readback.Apply();
            File.WriteAllBytes(Path.Combine(outDir, $"f{f:000}.png"), readback.EncodeToPNG());
        }
        RenderTexture.active = null;
        Debug.Log($"[cream] captured {frames} frames to {outDir}");
    }

    public static void All()
    {
        RigAll();
        BuildBreeds();
        BuildPrefab();
        Capture();
    }
}

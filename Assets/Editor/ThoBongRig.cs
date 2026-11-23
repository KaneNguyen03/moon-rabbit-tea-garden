using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Animation;
using UnityEditor.U2D.PSD;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.U2D;
using UnityEngine.U2D.Animation;

public static class ThoBongRig
{
    const string PsbPath = "Assets/Rig/Art/ThoBong.psb";
    const string RigFolder = "Assets/Rig";
    const int Canvas = 512;

    struct BoneSpec
    {
        public string Name;
        public int Parent;
        public Vector2 From;
        public Vector2 To;
    }

    static readonly BoneSpec[] Bones =
    {
        new BoneSpec { Name = "Hip", Parent = -1, From = new Vector2(255, 480), To = new Vector2(252, 420) },
        new BoneSpec { Name = "Spine", Parent = 0, From = new Vector2(252, 420), To = new Vector2(248, 325) },
        new BoneSpec { Name = "Neck", Parent = 1, From = new Vector2(248, 325), To = new Vector2(245, 205) },
        new BoneSpec { Name = "EarBackRoot", Parent = 2, From = new Vector2(232, 212), To = new Vector2(222, 100) },
        new BoneSpec { Name = "EarFrontRoot", Parent = 2, From = new Vector2(292, 218), To = new Vector2(328, 112) },
        new BoneSpec { Name = "Shoulder", Parent = 1, From = new Vector2(300, 355), To = new Vector2(296, 409) },
        new BoneSpec { Name = "Grip", Parent = 5, From = new Vector2(296, 409), To = new Vector2(295, 440) },
    };

    static readonly (string category, string[] labels)[] Library =
    {
        ("Hat", new[] { "VuongMien", "VongHoa" }),
        ("Tool", new[] { "Spoon", "RollingPin", "Basket", "Telescope" }),
    };

    static readonly (string state, string tool, string hat)[] Trades =
    {
        ("Brew", "Spoon", "VuongMien"),
        ("Bake", "RollingPin", "VongHoa"),
        ("Gather", "Basket", "VuongMien"),
        ("Stargaze", "Telescope", "VongHoa"),
    };

    const int Hip = 0, Spine = 1, Head = 2, EarBack = 3, EarFront = 4, Arm = 5, Grip = 6;

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
            Vector2 local;
            float localAngle;
            if (bone.Parent < 0)
            {
                local = world - origin;
                localAngle = angle;
            }
            else
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

    static float Ramp(float value, float from, float to) => Mathf.Clamp01((value - from) / (to - from));

    static BoneWeight Blend(int a, int b, float towardB)
    {
        if (towardB <= 0f) return new BoneWeight { boneIndex0 = a, weight0 = 1f };
        if (towardB >= 1f) return new BoneWeight { boneIndex0 = b, weight0 = 1f };
        return new BoneWeight { boneIndex0 = a, weight0 = 1f - towardB, boneIndex1 = b, weight1 = towardB };
    }

    static BoneWeight WeightFor(string layer, Vector2 psd)
    {
        switch (layer)
        {
            case "Ears":
                return new BoneWeight { boneIndex0 = psd.x < 272 ? EarBack : EarFront, weight0 = 1f };
            case "Body":
                if (psd.y < 340) return Blend(Head, Spine, Ramp(psd.y, 300, 340));
                return Blend(Spine, Hip, Ramp(psd.y, 420, 460));
            case "Arm":
                return new BoneWeight { boneIndex0 = Arm, weight0 = 1f };
            case "Paw":
            case "Spoon":
            case "RollingPin":
            case "Basket":
            case "Telescope":
                return new BoneWeight { boneIndex0 = Grip, weight0 = 1f };
            default:
                return new BoneWeight { boneIndex0 = Head, weight0 = 1f };
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
            var psd = new Vector2(character.x, Canvas - character.y);
            vertices[r * (cols + 1) + c] = new Vertex2DMetaData { position = local, boneWeight = WeightFor(layer, psd) };
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

    static string SpriteIdOf(PSDImporter importer, string layer)
    {
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        return provider.GetSpriteRects().First(r => r.name == layer).spriteID.ToString();
    }

    [MenuItem("Tho Bong/1 Rig PSB")]
    public static void Rig()
    {
        var importer = (PSDImporter)AssetImporter.GetAtPath(PsbPath);
        importer.useMosaicMode = true;
        importer.useCharacterMode = true;
        importer.spritePixelsPerUnit = 100;
        importer.SaveAndReimport();

        var serialized = new SerializedObject(importer);
        SerializedProperty categories = serialized.FindProperty("m_SpriteCategoryList.m_Categories");
        categories.ClearArray();
        for (int c = 0; c < Library.Length; c++)
        {
            categories.InsertArrayElementAtIndex(c);
            SerializedProperty category = categories.GetArrayElementAtIndex(c);
            category.FindPropertyRelative("m_Name").stringValue = Library[c].category;
            SerializedProperty labels = category.FindPropertyRelative("m_Labels");
            labels.ClearArray();
            for (int i = 0; i < Library[c].labels.Length; i++)
            {
                labels.InsertArrayElementAtIndex(i);
                SerializedProperty label = labels.GetArrayElementAtIndex(i);
                label.FindPropertyRelative("m_Name").stringValue = Library[c].labels[i];
                label.FindPropertyRelative("m_SpriteId").stringValue = SpriteIdOf(importer, Library[c].labels[i]);
            }
        }
        serialized.ApplyModifiedPropertiesWithoutUndo();
        importer.SaveAndReimport();
        importer = (PSDImporter)AssetImporter.GetAtPath(PsbPath);
        var factories = new SpriteDataProviderFactories();
        factories.Init();
        ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

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
            Debug.Log($"[rig] part {rect.name} at {part.spritePosition}");
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

        importer = (PSDImporter)AssetImporter.GetAtPath(PsbPath);
        importer.SaveAndReimport();
        CharacterData saved = ((ISpriteEditorDataProvider)importer).GetDataProvider<ICharacterDataProvider>().GetCharacterData();
        Debug.Log($"[rig] saved bones {saved.bones?.Length ?? 0}");
        Debug.Log("[rig] rigged " + PsbPath);
    }

    static Transform BoneTransform(Transform root, string bone)
    {
        Transform found = root.GetComponentsInChildren<Transform>(true).FirstOrDefault(x => x.name == bone);
        if (found == null)
            throw new InvalidOperationException($"bone {bone} missing; hierarchy: " +
                string.Join(", ", root.GetComponentsInChildren<Transform>(true).Select(x => AnimationUtility.CalculateTransformPath(x, root))));
        return found;
    }

    static void Key(AnimationClip clip, Transform root, string bone, string property, float duration, params float[] values)
    {
        Transform t = BoneTransform(root, bone);
        float rest = property switch
        {
            "localEulerAnglesRaw.z" => t.localEulerAngles.z,
            "m_LocalPosition.x" => t.localPosition.x,
            "m_LocalPosition.y" => t.localPosition.y,
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

    static AnimationClip SaveClip(AnimationClip clip, string name)
    {
        clip.name = name;
        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = $"{RigFolder}/{name}.anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    [MenuItem("Tho Bong/2 Clips and Prefab")]
    public static void BuildClipsAndPrefab()
    {
        GameObject psb = AssetDatabase.LoadAssetAtPath<GameObject>(PsbPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(psb);
        Transform root = instance.transform;

        var brew = new AnimationClip();
        Key(brew, root, "Shoulder", "localEulerAnglesRaw.z", 1f, 0, 16, 4, -12, 0);
        Key(brew, root, "Shoulder", "m_LocalPosition.x", 1f, 0, 0.03f, 0, -0.03f, 0);
        Key(brew, root, "Shoulder", "m_LocalPosition.y", 1f, 0.03f, 0, -0.03f, 0, 0.03f);
        Key(brew, root, "Grip", "localEulerAnglesRaw.z", 1f, 0, -8, -2, 6, 0);
        Key(brew, root, "Spine", "localEulerAnglesRaw.z", 1f, 0, -2.5f, 0, 2.5f, 0);
        Key(brew, root, "Neck", "localEulerAnglesRaw.z", 1f, 2, 0, -3, 0, 2);
        Key(brew, root, "EarBackRoot", "localEulerAnglesRaw.z", 1f, -4, 3, 6, -2, -4);
        Key(brew, root, "EarFrontRoot", "localEulerAnglesRaw.z", 1f, -3, 4, 7, -3, -3);
        Key(brew, root, "Hip", "m_LocalScale.x", 1f, 1, 1.015f, 1, 1.015f, 1);
        Key(brew, root, "Hip", "m_LocalScale.y", 1f, 1, 0.975f, 1, 0.975f, 1);
        SaveClip(brew, "ThoBong_Brew");

        var bake = new AnimationClip();
        Key(bake, root, "Shoulder", "localEulerAnglesRaw.z", 1f, 8, 20, 8, -4, 8);
        Key(bake, root, "Shoulder", "m_LocalPosition.x", 1f, 0, 0.05f, 0.09f, 0.04f, 0);
        Key(bake, root, "Grip", "localEulerAnglesRaw.z", 1f, -8, -20, -8, 4, -8);
        Key(bake, root, "Spine", "localEulerAnglesRaw.z", 1f, 0, -4, -6, -2, 0);
        Key(bake, root, "Neck", "localEulerAnglesRaw.z", 1f, 2, -3, -5, 0, 2);
        Key(bake, root, "EarBackRoot", "localEulerAnglesRaw.z", 1f, -6, 2, 8, 4, -6);
        Key(bake, root, "EarFrontRoot", "localEulerAnglesRaw.z", 1f, -5, 3, 9, 3, -5);
        Key(bake, root, "Hip", "m_LocalScale.x", 1f, 1, 1.03f, 1.05f, 1.02f, 1);
        Key(bake, root, "Hip", "m_LocalScale.y", 1f, 1, 0.95f, 0.92f, 0.97f, 1);
        SaveClip(bake, "ThoBong_Bake");

        var gather = new AnimationClip();
        Key(gather, root, "Shoulder", "localEulerAnglesRaw.z", 1f, 150, 168, 175, 160, 150);
        Key(gather, root, "Grip", "localEulerAnglesRaw.z", 1f, 0, -10, 4, 10, 0);
        Key(gather, root, "Spine", "localEulerAnglesRaw.z", 1f, 4, 7, 8, 5, 4);
        Key(gather, root, "Neck", "localEulerAnglesRaw.z", 1f, -6, -10, -12, -8, -6);
        Key(gather, root, "EarBackRoot", "localEulerAnglesRaw.z", 1f, 4, -3, -6, 2, 4);
        Key(gather, root, "EarFrontRoot", "localEulerAnglesRaw.z", 1f, 5, -2, -7, 1, 5);
        Key(gather, root, "Hip", "m_LocalPosition.y", 1f, 0, 0.08f, 0.12f, 0.05f, 0);
        Key(gather, root, "Hip", "m_LocalScale.x", 1f, 1, 0.98f, 0.97f, 0.99f, 1);
        Key(gather, root, "Hip", "m_LocalScale.y", 1f, 1, 1.03f, 1.04f, 1.01f, 1);
        SaveClip(gather, "ThoBong_Gather");

        var stargaze = new AnimationClip();
        Key(stargaze, root, "Shoulder", "localEulerAnglesRaw.z", 2f, 150, 154, 148, 153, 150);
        Key(stargaze, root, "Grip", "localEulerAnglesRaw.z", 2f, 18, 12, 20, 14, 18);
        Key(stargaze, root, "Spine", "localEulerAnglesRaw.z", 2f, 9, 11, 10, 12, 9);
        Key(stargaze, root, "Neck", "localEulerAnglesRaw.z", 2f, 8, 11, 9, 12, 8);
        Key(stargaze, root, "EarBackRoot", "localEulerAnglesRaw.z", 2f, 6, 8, 5, 9, 6);
        Key(stargaze, root, "EarFrontRoot", "localEulerAnglesRaw.z", 2f, 4, 7, 3, 8, 4);
        Key(stargaze, root, "Hip", "m_LocalScale.y", 2f, 1, 0.99f, 1, 0.99f, 1);
        SaveClip(stargaze, "ThoBong_Stargaze");

        var idle = new AnimationClip();
        Key(idle, root, "Hip", "m_LocalScale.x", 2f, 1, 1.012f, 1);
        Key(idle, root, "Hip", "m_LocalScale.y", 2f, 1, 0.982f, 1);
        Key(idle, root, "Neck", "localEulerAnglesRaw.z", 2f, 0, -1.5f, 0);
        Key(idle, root, "EarBackRoot", "localEulerAnglesRaw.z", 2f, 0, 0, 0, 9, 0, 0, 0, 0, 0);
        Key(idle, root, "EarFrontRoot", "localEulerAnglesRaw.z", 2f, 0, 2, 0, -2, 0);
        Key(idle, root, "Shoulder", "localEulerAnglesRaw.z", 2f, 0, -3, 0);
        SaveClip(idle, "ThoBong_Idle");

        string controllerPath = $"{RigFolder}/ThoBong.controller";
        AssetDatabase.DeleteAsset(controllerPath);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        controller.AddParameter("Working", AnimatorControllerParameterType.Bool);
        controller.AddParameter("Trade", AnimatorControllerParameterType.Int);
        AnimatorStateMachine machine = controller.layers[0].stateMachine;
        AnimatorState idleState = machine.AddState("Idle");
        idleState.motion = idle;
        machine.defaultState = idleState;
        AnimationClip[] work = { brew, bake, gather, stargaze };
        for (int i = 0; i < Trades.Length; i++)
        {
            AnimatorState state = machine.AddState(Trades[i].state);
            state.motion = work[i];
            AnimatorStateTransition enter = idleState.AddTransition(state);
            enter.AddCondition(AnimatorConditionMode.If, 0, "Working");
            enter.AddCondition(AnimatorConditionMode.Equals, i, "Trade");
            enter.duration = 0.15f;
            enter.hasExitTime = false;
            AnimatorStateTransition leave = state.AddTransition(idleState);
            leave.AddCondition(AnimatorConditionMode.IfNot, 0, "Working");
            leave.duration = 0.2f;
            leave.hasExitTime = false;
        }

        instance.name = "ThoBong";
        instance.AddComponent<SortingGroup>();
        instance.AddComponent<Animator>().runtimeAnimatorController = controller;
        PrefabUtility.SaveAsPrefabAsset(instance, $"{RigFolder}/ThoBong.prefab");
        UnityEngine.Object.DestroyImmediate(instance);
        AssetDatabase.SaveAssets();
        Debug.Log("[rig] clips, controller and prefab saved");
    }

    static void Deform()
    {
        Type manager = typeof(SpriteSkin).Assembly.GetType("UnityEngine.U2D.Animation.DeformationManager");
        object instance = manager.GetProperty("instance").GetValue(null);
        manager.GetMethod("Update", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public)
            .Invoke(instance, null);
    }

    [MenuItem("Tho Bong/3 Capture Frames")]
    public static void CaptureFrames()
    {
        string outDir = Path.GetFullPath("Captures/frames");
        if (Directory.Exists(outDir)) Directory.Delete(outDir, true);
        Directory.CreateDirectory(outDir);

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{RigFolder}/ThoBong.prefab");
        var actors = new List<(GameObject go, AnimationClip clip)>();
        for (int i = 0; i < Trades.Length; i++)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{RigFolder}/ThoBong_{Trades[i].state}.anim");
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = new Vector3(i * 3.6f, 0, 0);
            go.GetComponent<Animator>().enabled = false;
            foreach (SpriteResolver resolver in go.GetComponentsInChildren<SpriteResolver>(true))
            {
                string category = resolver.GetCategory();
                resolver.SetCategoryAndLabel(category, category == "Hat" ? Trades[i].hat : Trades[i].tool);
                resolver.ResolveSpriteToSpriteRenderer();
            }
            foreach (SpriteSkin skin in go.GetComponentsInChildren<SpriteSkin>(true))
            {
                skin.alwaysUpdate = true;
                skin.forceCpuDeformation = true;
            }
            actors.Add((go, clip));
        }

        Bounds? union = null;
        for (float probe = 0f; probe < 2f; probe += 0.125f)
        {
            foreach ((GameObject go, AnimationClip clip) in actors) clip.SampleAnimation(go, probe % clip.length);
            Deform();
            foreach (SpriteRenderer renderer in actors.SelectMany(a => a.go.GetComponentsInChildren<SpriteRenderer>()))
            {
                Bounds b = renderer.bounds;
                if (union == null) union = b;
                else { Bounds u = union.Value; u.Encapsulate(b); union = u; }
            }
        }
        Bounds bounds = union.Value;
        Debug.Log($"[rig] bounds {bounds}; resolvers {actors[0].go.GetComponentsInChildren<SpriteResolver>(true).Length}; skins {actors[0].go.GetComponentsInChildren<SpriteSkin>(true).Length}");
        var camera = new GameObject("CaptureCamera").AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = bounds.extents.y * 1.1f;
        camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, -10);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.953f, 0.925f, 0.867f, 1f);
        const int height = 520;
        int width = Mathf.RoundToInt(height * (bounds.size.x * 1.05f) / (bounds.size.y * 1.1f));
        camera.aspect = width / (float)height;
        var target = new RenderTexture(width, height, 24) { antiAliasing = 4 };
        camera.targetTexture = target;
        var readback = new Texture2D(width, height, TextureFormat.RGBA32, false);

        const int fps = 24;
        const int frames = fps * 2;
        for (int f = 0; f < frames; f++)
        {
            float t = f / (float)fps;
            foreach ((GameObject go, AnimationClip clip) in actors) clip.SampleAnimation(go, t % clip.length);
            Deform();
            camera.Render();
            RenderTexture.active = target;
            readback.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            readback.Apply();
            File.WriteAllBytes(Path.Combine(outDir, $"f{f:000}.png"), readback.EncodeToPNG());
        }
        RenderTexture.active = null;
        Debug.Log($"[rig] captured {frames} frames {width}x{height} to {outDir}");
    }

    public static void All()
    {
        Rig();
        BuildClipsAndPrefab();
        CaptureFrames();
    }
}

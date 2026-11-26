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
using VuonTra.Game;
using static VuonTra.EditorTools.RabbitRigSpec;

namespace VuonTra.EditorTools
{
    public static class RabbitRigBuilder
    {
        public const string PrefabPath = Folder + "/RabbitCream.prefab";
        public const string ControllerPath = Folder + "/RabbitCream.controller";
        public const string LibraryPath = Folder + "/RabbitCream_Library.asset";
        const string ShadowPath = Folder + "/Shadow.png";
        const string Label = Kind;
        const string ShadowLabel = "Soft";

        static Vector2 ToCharacter(Vector2 psd) => new Vector2(psd.x, CanvasPx - psd.y);

        static float WorldAngle(RigBone bone)
        {
            Vector2 d = ToCharacter(bone.To) - ToCharacter(bone.From);
            return Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
        }

        static int IndexOf(RigView view, string bone) => Array.FindIndex(view.Bones, b => b.Name == bone);

        static SpriteBone[] SkeletonIn(RigView view, Vector2 origin)
        {
            var result = new SpriteBone[view.Bones.Length];
            for (int i = 0; i < view.Bones.Length; i++)
            {
                RigBone bone = view.Bones[i];
                int parent = bone.Parent == null ? -1 : IndexOf(view, bone.Parent);
                Vector2 world = ToCharacter(bone.From);
                float angle = WorldAngle(bone);
                Vector2 local = world - origin;
                float localAngle = angle;
                if (parent >= 0)
                {
                    RigBone up = view.Bones[parent];
                    float parentAngle = WorldAngle(up);
                    local = Quaternion.Euler(0, 0, -parentAngle) * (world - ToCharacter(up.From));
                    localAngle = angle - parentAngle;
                }
                result[i] = new SpriteBone
                {
                    name = bone.Name,
                    guid = GUID.Generate().ToString(),
                    position = new Vector3(local.x, local.y, 0),
                    rotation = Quaternion.Euler(0, 0, localAngle),
                    length = Vector2.Distance(bone.From, bone.To),
                    parentId = parent,
                    color = Color.HSVToRGB(i / (float)view.Bones.Length, 0.7f, 1f),
                };
            }
            return result;
        }

        static BoneWeight WeightFor(RigView view, RigPart part, Vector2 psd)
        {
            int upper = IndexOf(view, part.Bone);
            if (part.LowerBone == null) return new BoneWeight { boneIndex0 = upper, weight0 = 1f };
            int lower = IndexOf(view, part.LowerBone);
            float towardLower = Mathf.Clamp01((psd.y - part.BlendFrom) / (part.BlendTo - part.BlendFrom));
            if (towardLower <= 0f) return new BoneWeight { boneIndex0 = upper, weight0 = 1f };
            if (towardLower >= 1f) return new BoneWeight { boneIndex0 = lower, weight0 = 1f };
            return new BoneWeight { boneIndex0 = upper, weight0 = 1f - towardLower, boneIndex1 = lower, weight1 = towardLower };
        }

        static void GridMesh(RigView view, RigPart part, int width, int height, Vector2 spritePosition,
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
                    boneWeight = WeightFor(view, part, new Vector2(character.x, CanvasPx - character.y)),
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

        static void Rig(RigView view)
        {
            var importer = (PSDImporter)AssetImporter.GetAtPath(view.Psb);
            importer.useMosaicMode = true;
            importer.useCharacterMode = true;
            importer.spritePixelsPerUnit = Ppu;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.SaveAndReimport();

            importer = (PSDImporter)AssetImporter.GetAtPath(view.Psb);
            ISpriteEditorDataProvider provider = ProviderFor(importer);
            var characterProvider = provider.GetDataProvider<ICharacterDataProvider>();
            var boneProvider = provider.GetDataProvider<ISpriteBoneDataProvider>();
            var meshProvider = provider.GetDataProvider<ISpriteMeshDataProvider>();
            CharacterData character = characterProvider.GetCharacterData();
            SpriteRect[] rects = provider.GetSpriteRects();
            int[] allBones = Enumerable.Range(0, view.Bones.Length).ToArray();
            for (int i = 0; i < character.parts.Length; i++)
            {
                CharacterPart characterPart = character.parts[i];
                SpriteRect rect = rects.First(r => r.spriteID.ToString() == characterPart.spriteId);
                RigPart part = view.Parts.First(p => p.Layer == rect.name);
                Vector2 origin = characterPart.spritePosition.position;
                characterPart.bones = allBones;
                character.parts[i] = characterPart;
                boneProvider.SetBones(rect.spriteID, SkeletonIn(view, origin).ToList());
                GridMesh(view, part, characterPart.spritePosition.width, characterPart.spritePosition.height, origin,
                    out Vertex2DMetaData[] vertices, out int[] indices, out Vector2Int[] edges);
                meshProvider.SetVertices(rect.spriteID, vertices);
                meshProvider.SetIndices(rect.spriteID, indices);
                meshProvider.SetEdges(rect.spriteID, edges);
            }
            character.bones = SkeletonIn(view, Vector2.zero);
            characterProvider.SetCharacterData(character);
            provider.Apply();
            importer = (PSDImporter)AssetImporter.GetAtPath(view.Psb);
            importer.SaveAndReimport();
        }

        static string ToolPath(string label) => $"{ToolFolder}/{label}.png";

        static void ImportSprite(string path, float ppu, Vector2 pivot)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = true;
            importer.filterMode = FilterMode.Trilinear;
            importer.spritePixelsPerUnit = ppu;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = pivot;
            settings.spriteMeshType = SpriteMeshType.Tight;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static void ImportTools()
        {
            foreach (ToolArt tool in ToolArts) ImportSprite(ToolPath(tool.Label), ToolPpu, tool.Grip);
        }

        static void MakeShadow()
        {
            const int width = 128, height = 48;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                float dx = (x + 0.5f - width / 2f) / (width / 2f);
                float dy = (y + 0.5f - height / 2f) / (height / 2f);
                float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, a * a * (3f - 2f * a)));
            }
            File.WriteAllBytes(ShadowPath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(ShadowPath);
            ImportSprite(ShadowPath, width * Ppu / ShadowWidthPx, new Vector2(0.5f, 0.5f));
        }

        static Sprite[] SpritesOf(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();

        static SpriteLibraryAsset BuildLibrary()
        {
            var library = ScriptableObject.CreateInstance<SpriteLibraryAsset>();
            foreach (RigView view in Views)
            {
                Sprite[] sprites = SpritesOf(view.Psb);
                foreach (RigPart part in view.Parts)
                    library.AddCategoryLabel(sprites.First(s => s.name == part.Layer), part.Layer, Label);
            }
            foreach (ToolArt tool in ToolArts)
                library.AddCategoryLabel(AssetDatabase.LoadAssetAtPath<Sprite>(ToolPath(tool.Label)), ToolCategory, tool.Label);
            library.AddCategoryLabel(AssetDatabase.LoadAssetAtPath<Sprite>(ShadowPath), ShadowCategory, ShadowLabel);
            AssetDatabase.DeleteAsset(LibraryPath);
            AssetDatabase.CreateAsset(library, LibraryPath);
            return library;
        }

        public static Transform Find(Transform root, string name) =>
            root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == name)
            ?? throw new InvalidOperationException($"{name} missing under {root.name}");

        public static Transform Target(Transform root, string target)
        {
            if (target == ShadowTarget) return Find(root, ShadowTarget);
            string[] split = target.Split(':');
            RigView view = Views.First(v => v.Key == split[0]);
            return Find(Find(root, view.Name), split[1]);
        }

        public static string HandObject(RigView view, string hand) => $"{view.Name}Tool{hand}";

        public static Transform Hand(Transform root, string hand)
        {
            string[] split = hand.Split(':');
            RigView view = Views.First(v => v.Key == split[0]);
            return Find(root, HandObject(view, split[1]));
        }

        static GameObject BuildView(Transform root, RigView view, SpriteLibraryAsset library)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(view.Psb));
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = view.Name;
            foreach (Component extra in instance.GetComponents<Component>().Where(c => c is SpriteLibrary || c is SortingGroup || c is Animator))
                UnityEngine.Object.DestroyImmediate(extra);
            instance.transform.SetParent(root, false);
            Transform hips = Find(instance.transform, view.Bones[0].Name);
            instance.transform.position -= hips.position;

            foreach (RigPart part in view.Parts)
            {
                Transform t = Find(instance.transform, part.Layer);
                var resolver = t.gameObject.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel(part.Layer, Label);
                t.GetComponent<SpriteRenderer>().sortingOrder = part.Order;
            }

            foreach (RigHand hand in view.Hands)
            {
                Transform grip = Find(instance.transform, hand.Grip);
                var tool = new GameObject(HandObject(view, hand.Hand));
                tool.transform.SetParent(grip, false);
                tool.transform.position = grip.position;
                tool.transform.rotation = Quaternion.identity;
                var renderer = tool.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = hand.Order;
                var resolver = tool.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel(ToolCategory, ToolArts[0].Label);
                renderer.sprite = library.GetSprite(ToolCategory, ToolArts[0].Label);
                renderer.enabled = false;
            }

            foreach (SpriteSkin skin in instance.GetComponentsInChildren<SpriteSkin>(true)) skin.alwaysUpdate = true;
            return instance;
        }

        static void Wire(UnityEngine.Object target, params (string field, UnityEngine.Object value)[] fields)
        {
            var serialized = new SerializedObject(target);
            foreach ((string field, UnityEngine.Object value) in fields)
                serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static AnimatorController BuildController(Transform root)
        {
            AssetDatabase.DeleteAsset(ControllerPath);
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            controller.AddParameter(RabbitActor.WorkParameter, AnimatorControllerParameterType.Int);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            foreach (WorkLoop loop in Loops)
            {
                AnimatorState state = machine.AddState(loop.Work.ToString());
                state.motion = RabbitClipWriter.Write(root, loop);
                if (loop.Work == RabbitWork.Idle) machine.defaultState = state;
                AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
                enter.AddCondition(AnimatorConditionMode.Equals, (int)loop.Work, RabbitActor.WorkParameter);
                enter.canTransitionToSelf = false;
                enter.hasExitTime = false;
                enter.duration = 0.2f;
            }
            return controller;
        }

        [MenuItem("VuonTra/Rabbit 2.5D/Build Cream")]
        public static void Build()
        {
            foreach (RigView view in Views) Rig(view);
            ImportTools();
            MakeShadow();
            SpriteLibraryAsset library = BuildLibrary();

            var rabbit = new GameObject("RabbitCream");
            Transform root = rabbit.transform;
            rabbit.AddComponent<SortingGroup>();
            var spriteLibrary = rabbit.AddComponent<SpriteLibrary>();
            spriteLibrary.spriteLibraryAsset = library;
            var animator = rabbit.AddComponent<Animator>();

            var shadow = new GameObject(ShadowTarget);
            shadow.transform.SetParent(root, false);
            var shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sortingOrder = -10;
            shadowRenderer.color = new Color(0.12f, 0.2f, 0.16f, 0.38f);
            shadow.AddComponent<SpriteResolver>().SetCategoryAndLabel(ShadowCategory, ShadowLabel);
            shadowRenderer.sprite = library.GetSprite(ShadowCategory, ShadowLabel);

            GameObject front = BuildView(root, Front, library);
            GameObject side = BuildView(root, Side, library);
            side.SetActive(false);

            animator.runtimeAnimatorController = BuildController(root);
            var actor = rabbit.AddComponent<RabbitActor>();
            Wire(actor, ("animator", animator), ("front", front), ("threeQuarter", side), ("shadow", shadow));

            AssetDatabase.DeleteAsset(PrefabPath);
            PrefabUtility.SaveAsPrefabAsset(rabbit, PrefabPath);
            UnityEngine.Object.DestroyImmediate(rabbit);
            AssetDatabase.SaveAssets();
            Debug.Log("[rabbit25d] built " + PrefabPath);
        }

        public static void BuildAndCapture()
        {
            Build();
            RabbitCapture.Capture();
        }
    }
}

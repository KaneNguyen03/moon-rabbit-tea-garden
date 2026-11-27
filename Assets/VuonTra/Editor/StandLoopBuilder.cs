using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.U2D.Animation;
using VuonTra.Game;

namespace VuonTra.EditorTools
{
    public static class StandLoopBuilder
    {
        public const string Folder = "Assets/VuonTra/Art/Garden25D/Loops";
        public const string SettingsPath = "Assets/Resources/Garden25D/GardenStands.asset";
        const string Work = RabbitActor.WorkParameter;

        [MenuItem("VuonTra/Build 2.5D Station Loops")]
        public static void Build()
        {
            var settings = AssetDatabase.LoadAssetAtPath<GardenStandSettings>(SettingsPath);
            if (settings == null) return;
            Directory.CreateDirectory(Folder);
            var serialized = new SerializedObject(settings);
            SerializedProperty stations = serialized.FindProperty("stations");
            for (int i = 0; i < settings.Stations.Length; i++)
            {
                StationStand stand = settings.Stations[i];
                if (stand.Layers.Length == 0 && stand.Dish == null) continue;
                SpriteLibraryAsset library = Library(stand);
                SerializedProperty entry = stations.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("Library").objectReferenceValue = library;
                entry.FindPropertyRelative("Loop").objectReferenceValue = Controller(stand, library, settings.LoopSeconds);
            }
            var sprites = new List<Sprite>();
            foreach (StationStand stand in settings.Stations)
                foreach (ParticleBeat beat in stand.Particles)
                    if (beat.Art != null && !sprites.Contains(beat.Art)) sprites.Add(beat.Art);
            SerializedProperty looks = serialized.FindProperty("particleLooks");
            looks.arraySize = sprites.Count;
            for (int n = 0; n < sprites.Count; n++)
            {
                SerializedProperty look = looks.GetArrayElementAtIndex(n);
                look.FindPropertyRelative("Art").objectReferenceValue = sprites[n];
                look.FindPropertyRelative("Look").objectReferenceValue = Particles(sprites[n]);
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        static T Kept<T>(string path, Func<T> make) where T : UnityEngine.Object
        {
            var kept = AssetDatabase.LoadAssetAtPath<T>(path);
            if (kept != null) return kept;
            T made = make();
            AssetDatabase.CreateAsset(made, path);
            return made;
        }

        static SpriteLibraryAsset Library(StationStand stand)
        {
            SpriteLibraryAsset library = Kept($"{Folder}/{stand.Id}_Library.asset", ScriptableObject.CreateInstance<SpriteLibraryAsset>);
            foreach (string category in new List<string>(library.GetCategoryNames()))
                foreach (string label in new List<string>(library.GetCategoryLabelNames(category)))
                    library.RemoveCategoryLabel(category, label, true);
            foreach (PropLayer layer in stand.Layers)
                foreach (PropState state in layer.States)
                    if (state.Art != null) library.AddCategoryLabel(state.Art, layer.Category, state.Label);
            EditorUtility.SetDirty(library);
            return library;
        }

        public static int HashOf(SpriteLibraryAsset library, string category, string label)
        {
            var holder = new GameObject("hash", typeof(SpriteRenderer));
            try
            {
                holder.AddComponent<SpriteLibrary>().spriteLibraryAsset = library;
                var resolver = holder.AddComponent<SpriteResolver>();
                resolver.SetCategoryAndLabel(category, label);
                return new SerializedObject(resolver).FindProperty("m_SpriteHash").intValue;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }
        }

        static Keyframe Step(float time, float value) => new Keyframe(time, value, float.PositiveInfinity, float.PositiveInfinity);

        static float Key(SpriteLibraryAsset library, string category, string label) => BitConverter.Int32BitsToSingle(HashOf(library, category, label));

        static AnimationClip Clip(StationStand stand, SpriteLibraryAsset library, float seconds, bool working)
        {
            string name = stand.Id + (working ? "_Work" : "_Idle");
            AnimationClip clip = Kept($"{Folder}/{name}.anim", () => new AnimationClip { name = name });
            clip.ClearCurves();
            clip.frameRate = 30f;
            foreach (PropLayer layer in stand.Layers)
            {
                var keys = new List<Keyframe> { Step(0f, Key(library, layer.Category, StandLoop.IdleLabel)) };
                if (working)
                    foreach (StateBeat beat in layer.Beats)
                    {
                        if (beat.At <= 0f) keys.RemoveAt(0);
                        keys.Add(Step(beat.At, Key(library, layer.Category, beat.Label)));
                    }
                keys.Add(Step(seconds, keys[keys.Count - 1].value));
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.DiscreteCurve(StandLoop.PathOf(layer), typeof(SpriteResolver), "m_SpriteHash"), new AnimationCurve(keys.ToArray()));
            }
            if (stand.Dish != null)
            {
                Keyframe[] dish = working
                    ? new[] { Step(0f, 0f), Step(stand.DishFrom, 1f), Step(seconds, 1f) }
                    : new[] { Step(0f, 0f), Step(seconds, 0f) };
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(StandLoop.DishName, typeof(SpriteRenderer), "m_Enabled"), new AnimationCurve(dish));
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(StandLoop.DishShadowName, typeof(SpriteRenderer), "m_Enabled"), new AnimationCurve(dish));
            }
            AnimationClipSettings loop = AnimationUtility.GetAnimationClipSettings(clip);
            loop.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, loop);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        static AnimatorController Controller(StationStand stand, SpriteLibraryAsset library, float seconds)
        {
            AnimationClip idleClip = Clip(stand, library, seconds, false), workClip = Clip(stand, library, seconds, true);
            string path = $"{Folder}/{stand.Id}.controller";
            var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller != null) return controller;
            controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter(Work, AnimatorControllerParameterType.Int);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            idle.motion = idleClip;
            machine.defaultState = idle;
            AnimatorState work = machine.AddState("Work");
            work.motion = workClip;
            Join(machine.AddAnyStateTransition(work), AnimatorConditionMode.Greater);
            Join(machine.AddAnyStateTransition(idle), AnimatorConditionMode.Equals);
            return controller;
        }

        static void Join(AnimatorStateTransition transition, AnimatorConditionMode mode)
        {
            transition.AddCondition(mode, 0f, Work);
            transition.canTransitionToSelf = false;
            transition.hasExitTime = false;
            transition.duration = 0f;
        }

        static Material Particles(Sprite sprite)
        {
            Material material = Kept($"{Folder}/Particles_{sprite.name}.mat", () => new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")));
            material.name = "Particles " + sprite.name;
            material.SetTexture("_BaseMap", sprite.texture);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            EditorUtility.SetDirty(material);
            return material;
        }
    }
}

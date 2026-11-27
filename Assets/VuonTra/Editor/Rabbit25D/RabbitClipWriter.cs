using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D.Animation;
using static VuonTra.EditorTools.RabbitRigSpec;

namespace VuonTra.EditorTools
{
    public static class RabbitClipWriter
    {
        const float Anticipation = 0.2f;
        const float Overshoot = 0.12f;

        static string Property(string channel) => channel switch
        {
            "R" => "localEulerAnglesRaw.z",
            "X" => "m_LocalPosition.x",
            "Y" => "m_LocalPosition.y",
            "SX" => "m_LocalScale.x",
            "SY" => "m_LocalScale.y",
            _ => throw new ArgumentException(channel),
        };

        static float Rest(Transform t, string channel) => channel switch
        {
            "R" => t.localEulerAngles.z,
            "X" => t.localPosition.x,
            "Y" => t.localPosition.y,
            "SX" => t.localScale.x,
            "SY" => t.localScale.y,
            _ => throw new ArgumentException(channel),
        };

        public static float Length(WorkLoop loop) => loop.Beats.Sum(b => b.Length) + loop.ReturnLength;

        public static float BeatStart(WorkLoop loop, int index) => loop.Beats.Take(index).Sum(b => b.Length);

        static float ValueIn(Beat beat, string target, string channel, float previous)
        {
            foreach (PoseKey key in beat.Pose)
                if (key.Target == target && key.Channel == channel) return key.Value;
            return previous;
        }

        static List<Keyframe> Channel(WorkLoop loop, string target, string channel)
        {
            var keys = new SortedDictionary<float, float>();
            void Add(float time, float value) => keys[Mathf.Round(time * 1000f) / 1000f] = value;
            float previous = 0f;
            float start = 0f;
            Add(0f, 0f);
            foreach (Beat beat in loop.Beats)
            {
                float next = ValueIn(beat, target, channel, previous);
                float length = beat.Length;
                Wiggle[] wiggles = beat.Wiggles.Where(w => w.Target == target && w.Channel == channel).ToArray();
                float travel = next - previous;
                if (!loop.Snappy)
                {
                    Add(start + length, next);
                }
                else if (wiggles.Length > 0)
                {
                    Wiggle wiggle = wiggles[0];
                    Add(start, previous);
                    Add(start + length * 0.12f, previous - travel * Anticipation);
                    Add(start + length * 0.3f, next);
                    int count = wiggle.Cycles * 2;
                    for (int i = 0; i < count; i++)
                        Add(start + length * (0.3f + 0.6f * (i + 1) / (count + 1)), next + (i % 2 == 0 ? wiggle.Amplitude : -wiggle.Amplitude));
                    Add(start + length * 0.95f, next);
                }
                else if (Mathf.Abs(travel) > 1e-5f)
                {
                    Add(start, previous);
                    Add(start + length * 0.12f, previous - travel * Anticipation);
                    Add(start + length * 0.5f, next + travel * Overshoot);
                    Add(start + length * 0.7f, next);
                }
                previous = next;
                start += length;
            }
            float back = loop.ReturnLength;
            if (loop.Snappy && Mathf.Abs(previous) > 1e-5f)
            {
                Add(start, previous);
                Add(start + back * 0.6f, -previous * Overshoot);
            }
            Add(start + back, 0f);
            return keys.Select(k => new Keyframe(k.Key, k.Value)).ToList();
        }

        static void WriteChannel(AnimationClip clip, Transform root, WorkLoop loop, string target, string channel)
        {
            Transform t = RabbitRigBuilder.Target(root, target);
            if (t == null) return;
            float rest = Rest(t, channel);
            Keyframe[] keys = Channel(loop, target, channel).Select(k => new Keyframe(k.time, rest + k.value)).ToArray();
            var curve = new AnimationCurve(keys);
            for (int i = 0; i < keys.Length; i++) curve.SmoothTangents(i, 0f);
            string path = AnimationUtility.CalculateTransformPath(t, root);
            string property = Property(channel);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform), property), curve);
            HoldSiblings(clip, path, t, property, keys[keys.Length - 1].time);
        }

        static void HoldSiblings(AnimationClip clip, string path, Transform t, string property, float end)
        {
            string vector = property.Substring(0, property.Length - 2);
            Vector3 rest = vector switch
            {
                "m_LocalPosition" => t.localPosition,
                "m_LocalScale" => t.localScale,
                _ => t.localEulerAngles,
            };
            string[] axes = { "x", "y", "z" };
            for (int i = 0; i < 3; i++)
            {
                var binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), $"{vector}.{axes[i]}");
                if (AnimationUtility.GetEditorCurve(clip, binding) != null) continue;
                AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0f, end, rest[i]));
            }
        }

        public static int HashOf(SpriteResolver resolver, string label)
        {
            string category = resolver.GetCategory();
            string previous = resolver.GetLabel();
            resolver.SetCategoryAndLabel(category, label);
            int hash = new SerializedObject(resolver).FindProperty("m_SpriteHash").intValue;
            resolver.SetCategoryAndLabel(category, previous);
            return hash;
        }

        static Keyframe Step(float time, float value) => new Keyframe(time, value, float.PositiveInfinity, float.PositiveInfinity);

        static void WriteHand(AnimationClip clip, Transform root, WorkLoop loop, string hand)
        {
            var events = new List<(float time, string label)>();
            for (int i = 0; i < loop.Beats.Length; i++)
                foreach (ToolSwap swap in loop.Beats[i].Tools.Where(s => s.Hand == hand))
                    events.Add((BeatStart(loop, i) + loop.Beats[i].Length * swap.At, swap.Label));
            float end = Length(loop);
            if (events.Count == 0)
                events.Add((0f, null));
            events.Add((BeatStart(loop, loop.Beats.Length), null));
            events = events.OrderBy(e => e.time).ToList();
            if (events[0].time > 0f) events.Insert(0, (0f, null));

            Transform tool = RabbitRigBuilder.Hand(root, hand);
            if (tool == null) return;
            var resolver = tool.GetComponent<SpriteResolver>();
            string path = AnimationUtility.CalculateTransformPath(tool, root);
            var enabled = new List<Keyframe>();
            var sprite = new List<Keyframe>();
            string held = ToolArts[0].Label;
            foreach ((float time, string label) in events)
            {
                enabled.Add(Step(time, label == null ? 0f : 1f));
                if (label != null) held = label;
                sprite.Add(Step(time, BitConverter.Int32BitsToSingle(HashOf(resolver, held))));
            }
            enabled.Add(Step(end, 0f));
            sprite.Add(Step(end, BitConverter.Int32BitsToSingle(HashOf(resolver, held))));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(SpriteRenderer), "m_Enabled"),
                new AnimationCurve(Dedupe(enabled)));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.DiscreteCurve(path, typeof(SpriteResolver), "m_SpriteHash"),
                new AnimationCurve(Dedupe(sprite)));
        }

        static Keyframe[] Dedupe(List<Keyframe> keys) =>
            keys.GroupBy(k => Mathf.Round(k.time * 1000f)).Select(g => g.Last()).OrderBy(k => k.time).ToArray();

        static IEnumerable<string> Hands()
        {
            yield return FrontKey + ":L";
            yield return FrontKey + ":R";
            yield return SideKey + ":R";
        }

        public static AnimationClip Write(Transform root, WorkLoop loop, string assetPath)
        {
            var clip = new AnimationClip { name = System.IO.Path.GetFileNameWithoutExtension(assetPath), frameRate = 30f };
            var channels = loop.Beats.SelectMany(b => b.Pose.Select(p => (p.Target, p.Channel)))
                .Concat(loop.Beats.SelectMany(b => b.Wiggles.Select(w => (w.Target, w.Channel))))
                .Distinct();
            foreach ((string target, string channel) in channels) WriteChannel(clip, root, loop, target, channel);
            foreach (string hand in Hands()) WriteHand(clip, root, loop, hand);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.DeleteAsset(assetPath);
            AssetDatabase.CreateAsset(clip, assetPath);
            return clip;
        }
    }
}

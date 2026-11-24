using System;
using System.Collections.Generic;
using UnityEngine;

namespace VuonTra.Game.UI
{
    public static class Ease
    {
        public static float Linear(float t) => t;

        public static float OutCubic(float t)
        {
            float u = 1f - t;
            return 1f - u * u * u;
        }

        public static float OutBack(float t)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float OutBackLight(float t)
        {
            const float c1 = 1f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float InCubic(float t) => t * t * t;

        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        public static float Pop(float t) => Mathf.Sin(t * Mathf.PI);
    }

    public static class Motion
    {
        public const float Quick = 0.16f;
        public const float Tap = 0.18f;
        public const float Panel = 0.25f;
        public const float PanelOut = 0.2f;
        public const float Travel = 0.3f;
        public const float Glide = 1f;
        public const float Stagger = 0.035f;
        public const float Yawn = 0.6f;
        public const float Speech = 3f;

        const string ReducedKey = "motion.reduced";

        public static bool Reduced;

        public static void Load() => Reduced = PlayerPrefs.GetInt(ReducedKey, 0) == 1;

        public static void SetReduced(bool on)
        {
            Reduced = on;
            PlayerPrefs.SetInt(ReducedKey, on ? 1 : 0);
        }

        public static float Time(float seconds) => Reduced ? 0.0001f : seconds;

        public static Func<float, float> Curve(Func<float, float> curve) => Reduced ? Ease.OutCubic : curve;
    }

    public sealed class Tweens
    {
        struct Item
        {
            public float T, Duration, Delay;
            public Action<float> Apply;
            public Func<float, float> Curve;
            public Action Done;
        }

        readonly List<Item> _items = new List<Item>();

        public int Count => _items.Count;

        public void Clear() => _items.Clear();

        public void Add(float duration, Action<float> apply, Func<float, float> curve = null, float delay = 0f, Action done = null)
        {
            _items.Add(new Item
            {
                Duration = Mathf.Max(0.0001f, duration), Delay = delay,
                Apply = apply, Curve = curve ?? Ease.OutCubic, Done = done
            });
        }

        public void Grow(Transform target, float from, float seconds, Func<float, float> curve)
        {
            target.localScale = Vector3.one * from;
            Add(seconds, t => { if (target != null) target.localScale = Vector3.one * (from + (1f - from) * t); }, curve);
        }

        public void Update(float dt)
        {
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                Item item = _items[i];
                if (item.Delay > 0f)
                {
                    item.Delay -= dt;
                    _items[i] = item;
                    continue;
                }

                item.T += dt / item.Duration;
                if (item.T >= 1f)
                {
                    _items.RemoveAt(i);
                    item.Apply(item.Curve(1f));
                    item.Done?.Invoke();
                }
                else
                {
                    _items[i] = item;
                    item.Apply(item.Curve(item.T));
                }
            }
        }
    }
}

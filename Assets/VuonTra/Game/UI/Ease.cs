using System;
using System.Collections.Generic;
using UnityEngine;

namespace VuonTra.Game.UI
{
    /// <summary>The three curves the feel recipes use (standard formulas, see easings.net).</summary>
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

        public static float OutElastic(float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            return Mathf.Pow(2f, -10f * t) * Mathf.Sin((t * 10f - 0.75f) * (2f * Mathf.PI / 3f)) + 1f;
        }

        /// <summary>0 → 1 → 0 over the tween, for pops that return to rest.</summary>
        public static float Pop(float t) => Mathf.Sin(t * Mathf.PI);
    }

    /// <summary>
    /// A tiny tween runner stepped by its owner with unscaled time, so UI motion keeps going while
    /// the game is paused. Stepped by hand (not coroutines) so tests and editor previews can run it.
    /// </summary>
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

        public void Add(float duration, Action<float> apply, Func<float, float> curve = null, float delay = 0f, Action done = null)
        {
            _items.Add(new Item
            {
                Duration = Mathf.Max(0.0001f, duration), Delay = delay,
                Apply = apply, Curve = curve ?? Ease.OutCubic, Done = done
            });
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

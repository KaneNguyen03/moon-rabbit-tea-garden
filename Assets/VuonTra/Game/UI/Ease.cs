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

        /// <summary>Like OutBack but only a few percent past the end: for big panels, where 10% would be a lurch.</summary>
        public static float OutBackLight(float t)
        {
            const float c1 = 1f, c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }

        public static float InCubic(float t) => t * t * t;

        /// <summary>Gathers speed, then slows to a stop: for the camera and anything that travels a distance.</summary>
        public static float InOutCubic(float t) => t < 0.5f ? 4f * t * t * t : 1f - Mathf.Pow(-2f * t + 2f, 3f) / 2f;

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
    /// The one table of how long things take, so the whole game moves to the same beat.
    /// Nothing here is longer than 0.3 s unless the player can act while it plays (the opening glide).
    /// </summary>
    public static class Motion
    {
        /// <summary>A colour or size settling: tab highlights, a row appearing.</summary>
        public const float Quick = 0.16f;
        /// <summary>A button springing back, a small pop.</summary>
        public const float Tap = 0.18f;
        /// <summary>A sheet or card sliding in.</summary>
        public const float Panel = 0.25f;
        /// <summary>A sheet or card leaving: a little faster than it came.</summary>
        public const float PanelOut = 0.2f;
        /// <summary>The camera changing zoom or flying to a station.</summary>
        public const float Travel = 0.3f;
        /// <summary>The camera's glide from the moon pond to the teapot when the game opens. Touching the garden stops it.</summary>
        public const float Glide = 1f;
        /// <summary>The gap between one list row appearing and the next.</summary>
        public const float Stagger = 0.035f;
        /// <summary>A rabbit's stretch as it nods off at its post. Ambient: the player can act all through it.</summary>
        public const float Yawn = 0.6f;
        /// <summary>How long a line a rabbit says stays over its head before it leaves by itself.</summary>
        public const float Speech = 3f;

        const string ReducedKey = "motion.reduced";

        /// <summary>The player asked for less movement: slides, glides and bounces are skipped, things simply arrive.</summary>
        public static bool Reduced;

        public static void Load() => Reduced = PlayerPrefs.GetInt(ReducedKey, 0) == 1;

        public static void SetReduced(bool on)
        {
            Reduced = on;
            PlayerPrefs.SetInt(ReducedKey, on ? 1 : 0);
        }

        /// <summary>The time a movement takes: as given, or next to nothing when movement is reduced.</summary>
        public static float Time(float seconds) => Reduced ? 0.0001f : seconds;

        /// <summary>The curve a movement follows: as given, or a plain one without overshoot when movement is reduced.</summary>
        public static Func<float, float> Curve(Func<float, float> curve) => Reduced ? Ease.OutCubic : curve;
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

        /// <summary>Drops every running tween without finishing it (their targets are going away).</summary>
        public void Clear() => _items.Clear();

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

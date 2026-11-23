using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using TweenEase = DG.Tweening.Ease;

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

        public static TweenEase Curve(TweenEase curve) => Reduced ? TweenEase.OutCubic : curve;

        public static T OnUiClock<T>(this T tween, Component owner, LinkBehaviour link = LinkBehaviour.KillOnDestroy) where T : Tween =>
            tween.SetUpdate(UpdateType.Manual).SetLink(owner.gameObject, link);

        public static Tween After(float seconds, Component owner, TweenCallback then) =>
            DOVirtual.DelayedCall(seconds, then).OnUiClock(owner);

        public static Tween PopIn(Transform card, float from = 0.85f, float seconds = Panel)
        {
            card.DOKill();
            card.localScale = Vector3.one * from;
            return card.DOScale(1f, Time(seconds)).SetEase(Curve(TweenEase.OutBack)).OnUiClock(card);
        }

        public static Sequence ShakeX(RectTransform rect, float homeX, float amplitude, float seconds, int halfSwings) =>
            Shake(rect, homeX, amplitude, seconds, halfSwings, (x, d) => rect.DOAnchorPosX(x, d));

        public static Sequence ShakeAngle(Transform target, float amplitude, float seconds, int halfSwings) =>
            Shake(target, 0f, amplitude, seconds, halfSwings, (z, d) => target.DOLocalRotate(new Vector3(0f, 0f, z), d));

        static Sequence Shake(Component target, float home, float amplitude, float seconds, int halfSwings, Func<float, float, Tweener> toward)
        {
            target.DOKill();
            float step = seconds / halfSwings;
            Sequence shake = DOTween.Sequence().SetTarget(target);
            for (int k = 0; k < halfSwings; k++)
            {
                float peak = (k + 0.5f) / halfSwings;
                float value = home + (k % 2 == 0 ? 1f : -1f) * amplitude * (1f - peak);
                shake.Append(toward(value, k == 0 ? step / 2f : step).SetEase(k == 0 ? TweenEase.OutSine : TweenEase.InOutSine));
            }
            shake.Append(toward(home, step / 2f).SetEase(TweenEase.InSine));
            return shake.OnUiClock(target);
        }
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
            DOTween.ManualUpdate(dt, dt);
        }
    }
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed class FeelLayer
    {
        const int MaxParticles = 90;

        struct Particle
        {
            public RectTransform Rect;
            public Graphic Graphic;
            public Vector2 Velocity;
            public float Age, Life, Spin, Gravity, StartScale;
        }

        readonly RectTransform _root;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly List<Particle> _particles = new List<Particle>();
        readonly Image _flash;
        readonly RectTransform _banner;
        readonly Text _bannerTitle, _bannerSub;
        int _bannerToken;

        public int ParticleCount => _particles.Count;
        public int Flying { get; private set; }
        public const float FlyTime = 0.55f;
        public bool BannerVisible => _banner.gameObject.activeSelf;

        public FeelLayer(RectTransform canvas, GameCatalog catalog, Tweens tweens)
        {
            _catalog = catalog;
            _tweens = tweens;
            _root = UiKit.Rect("Feel", canvas).Stretch(0, 0, 1, 1);

            _flash = UiKit.Rect("Flash", _root).Stretch(0, 0, 1, 1).AddImage(null, Palette.Alpha(Palette.Carrot, 0f));
            _flash.gameObject.SetActive(false);

            _banner = UiKit.Rect("Banner", _root).Place(0.5f, 0.7f, 0, 0, 820, 210);
            UiKit.Rect("Lip", _banner).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Card", _banner).Stretch(0, 0, 1, 1, 0, 10, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            _bannerTitle = UiKit.Rect("Title", _banner).Stretch(0, 0.45f, 1, 1, 30, 0, 30, 24)
                .AddText("", 52, Palette.Carrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _bannerSub = UiKit.Rect("Sub", _banner).Stretch(0, 0, 1, 0.5f, 30, 30, 30, 0)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter);
            _banner.gameObject.SetActive(false);
        }

        public void BringToFront() => _root.SetAsLastSibling();

        public Vector2 PointOf(RectTransform target, Vector2 offset = default)
        {
            Vector3 local = _root.InverseTransformPoint(target.TransformPoint(offset));
            return new Vector2(local.x, local.y);
        }

        public void Flash(Color color, float seconds = 0.1f)
        {
            _flash.gameObject.SetActive(true);
            _tweens.Add(Motion.Time(seconds), t => _flash.color = Palette.Alpha(color, 0.85f * (1f - t)), Ease.OutCubic,
                done: () => _flash.gameObject.SetActive(false));
        }

        public void FloatText(Vector2 at, string text, Color color, int size = 40)
        {
            if (_particles.Count >= MaxParticles) return;
            var rt = UiKit.Rect("Gain", _root).Place(0.5f, 0.5f, at.x, at.y, 360, 64);
            Text label = rt.AddText(text, size, color, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.AddShadow(0.7f);
            _particles.Add(new Particle
            {
                Rect = rt, Graphic = label, Velocity = new Vector2(0, 150f),
                Life = 0.75f, Gravity = -90f, StartScale = 1f
            });
        }

        public void Fly(Vector2 from, Vector2 to, Sprite sprite, Color color, int count, float size, System.Action onArrive = null)
        {
            if (Motion.Reduced)
            {
                onArrive?.Invoke();
                return;
            }
            count = Mathf.Clamp(count, 1, 12);
            for (int i = 0; i < count; i++)
            {
                RectTransform rt = UiKit.Rect("Fly", _root).Place(0.5f, 0.5f, from.x, from.y, size, size);
                Image img = rt.AddImage(sprite, color);
                img.preserveAspect = true;
                Vector2 start = from + new Vector2(Random.Range(-30f, 30f), Random.Range(-14f, 14f));
                Vector2 mid = (start + to) * 0.5f + new Vector2(Random.Range(-60f, 60f), 150f + Random.Range(0f, 90f));
                bool last = i == count - 1;
                Flying++;
                _tweens.Add(Motion.Time(FlyTime), t =>
                {
                    if (rt == null) return;
                    float u = 1f - t;
                    rt.anchoredPosition = u * u * start + 2f * u * t * mid + t * t * to;
                    rt.localScale = Vector3.one * (1f + 0.35f * Mathf.Sin(t * Mathf.PI));
                }, Ease.InOutCubic, Motion.Time(i * 0.05f), () =>
                {
                    if (rt != null) Destroy(rt.gameObject);
                    Flying--;
                    if (last) onArrive?.Invoke();
                });
            }
        }

        public void Burst(Vector2 at, Sprite sprite, Color[] colors, int count, float speed = 520f, float size = 34f)
        {
            if (Motion.Reduced) return;
            for (int i = 0; i < count && _particles.Count < MaxParticles; i++)
            {
                float angle = Mathf.Lerp(25f, 155f, (i + Random.value * 0.8f) / count) * Mathf.Deg2Rad;
                float push = speed * Random.Range(0.55f, 1f);
                var rt = UiKit.Rect("Bit", _root).Place(0.5f, 0.5f, at.x, at.y, size, size);
                Image img = rt.AddImage(sprite, colors[i % colors.Length]);
                img.preserveAspect = true;
                _particles.Add(new Particle
                {
                    Rect = rt, Graphic = img,
                    Velocity = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * push,
                    Life = Random.Range(0.55f, 0.95f), Spin = Random.Range(-320f, 320f),
                    Gravity = 1500f, StartScale = Random.Range(0.7f, 1.15f)
                });
            }
        }

        public void Moment(string title, string sub)
        {
            int token = ++_bannerToken;

            _flash.gameObject.SetActive(true);
            _tweens.Add(0.1f, t => _flash.color = Palette.Alpha(Palette.Carrot, 0.6f * (1f - t)), Ease.OutCubic,
                done: () => _flash.gameObject.SetActive(false));

            _bannerTitle.text = title;
            _bannerSub.text = sub;
            _banner.gameObject.SetActive(true);
            _banner.localScale = Vector3.zero;
            _tweens.Add(0.35f, t => _banner.localScale = Vector3.one * t, Ease.OutBack, delay: 0.05f);
            _tweens.Add(0.2f, t =>
            {
                if (token == _bannerToken) _banner.localScale = Vector3.one * (1f - t);
            }, Ease.OutCubic, delay: 1.5f, done: () =>
            {
                if (token == _bannerToken) _banner.gameObject.SetActive(false);
            });

            Vector2 centre = PointOf(_banner);
            Burst(centre + new Vector2(0, -40), _catalog.Round,
                new[] { Palette.Carrot, Palette.Blush, Palette.Moss, Palette.Lantern }, 40, 900f, 22f);
        }

        public void Update(float dt)
        {
            for (int i = _particles.Count - 1; i >= 0; i--)
            {
                Particle p = _particles[i];
                p.Age += dt;
                if (p.Age >= p.Life || p.Rect == null)
                {
                    if (p.Rect != null) Destroy(p.Rect.gameObject);
                    _particles.RemoveAt(i);
                    continue;
                }

                p.Velocity.y -= p.Gravity * dt;
                p.Rect.anchoredPosition += p.Velocity * dt;
                p.Rect.localRotation = Quaternion.Euler(0, 0, p.Spin * p.Age);
                float life = p.Age / p.Life;
                float pop = p.Age < 0.12f ? Ease.OutBack(p.Age / 0.12f) : 1f;
                p.Rect.localScale = Vector3.one * (p.StartScale * pop);
                Color c = p.Graphic.color;
                c.a = 1f - life * life;
                p.Graphic.color = c;
                _particles[i] = p;
            }
        }

        static void Destroy(GameObject go)
        {
            if (Application.isPlaying) Object.Destroy(go);
            else Object.DestroyImmediate(go);
        }
    }
}

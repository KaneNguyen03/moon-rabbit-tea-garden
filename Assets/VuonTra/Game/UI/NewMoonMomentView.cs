using System;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The moment a new moon begins (#1): night falls over the garden, the full moon sinks behind the hills,
    /// and a thin new moon rises in its place with the season's number. Three seconds; a tap ends it early.
    /// </summary>
    public sealed class NewMoonMomentView
    {
        public const float Length = 3f;
        const float SetEnd = 1.3f, RiseStart = 1.7f, Linger = 0.8f;
        const float High = 420f, Low = -520f;

        readonly GameObject _root;
        readonly RectTransform _moon, _shade;
        readonly CanvasGroup _veil, _words;
        readonly Text _title;
        float _t;
        bool _done;
        Action _onDone;

        public bool IsOpen => _root.activeSelf;
        public float Progress => Mathf.Clamp01(_t / Length);

        public NewMoonMomentView(RectTransform canvas, GameCatalog catalog)
        {
            var overlay = UiKit.Rect("NewMoonMoment", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            _veil = _root.AddComponent<CanvasGroup>();
            overlay.AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Color.black, 0.5f), 0.97f)).raycastTarget = true;
            var button = _root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Tap);

            // A few stars, fixed, so the moon has a sky to cross.
            for (int i = 0; i < 14; i++)
            {
                float x = -480f + (i * 173f) % 960f, y = 120f + (i * 311f) % 640f;
                UiKit.Rect("Star", overlay).Place(0.5f, 0.5f, x, y, 10 + (i % 3) * 4, 10 + (i % 3) * 4).AddImage(catalog.Round, Palette.Alpha(Palette.Paper, 0.7f));
            }
            _moon = UiKit.Rect("Moon", overlay).Place(0.5f, 0.5f, 0, High, 300, 300);
            _moon.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            UiKit.Rect("Disc", _moon).Place(0.5f, 0.5f, 0, 0, 170, 170).AddImage(catalog.Round, Palette.Lantern);
            // The shade that turns the full disc into a crescent as it rises again.
            _shade = UiKit.Rect("Shade", _moon).Place(0.5f, 0.5f, 0, 0, 160, 160);
            _shade.AddImage(catalog.Round, Palette.Mix(Palette.Night, Color.black, 0.5f));
            // The hills the moon sinks behind: drawn after the moon so they cover it.
            UiKit.Rect("Hills", overlay).Stretch(-0.1f, 0f, 1.1f, 0.36f).AddImage(catalog.Panel, Palette.Mix(Palette.Night, Palette.Ink, 0.35f), true);

            var words = UiKit.Rect("Words", overlay).Place(0.5f, 0.5f, 0, -60, 900, 300);
            _words = words.gameObject.AddComponent<CanvasGroup>();
            _title = UiKit.Rect("Title", words).Place(0.5f, 1f, 0, 0, 900, 120).AddText("", 84, Palette.Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            _title.AddShadow(0.6f, 6f);
            UiKit.Rect("Sub", words).Place(0.5f, 1f, 0, -130, 900, 70).AddText(L10n.T("newmoon.moment.sub"), UiKit.TextTitle, Palette.Paper, TextAnchor.MiddleCenter);
            UiKit.Rect("Hint", overlay).Place(0.5f, 0f, 0, 160, 700, 50).AddText(L10n.T("newmoon.moment.hint"), UiKit.TextBody, Palette.Alpha(Palette.Paper, 0.7f));
            _root.SetActive(false);
        }

        public void Open(int season, Action onDone)
        {
            _onDone = onDone;
            _done = false;
            _t = 0f;
            _title.text = L10n.T("newmoon.moment.title", season);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Place();
        }

        void Place()
        {
            float t = Motion.Reduced ? Length : _t;
            float y, shade;
            if (t < SetEnd) { y = Mathf.Lerp(High, Low, Ease.InCubic(t / SetEnd)); shade = 0f; }
            else if (t < RiseStart) { y = Low; shade = 1f; }
            else { y = Mathf.Lerp(Low, High * 0.8f, Ease.OutCubic(Mathf.Clamp01((t - RiseStart) / (Length - RiseStart)))); shade = 1f; }
            _moon.anchoredPosition = new Vector2(0f, y);
            _shade.gameObject.SetActive(shade > 0f);
            _shade.anchoredPosition = new Vector2(48f, 18f);
            _words.alpha = Mathf.Clamp01((t - SetEnd * 0.8f) / 0.6f);
            _veil.alpha = Motion.Reduced ? 1f : Mathf.Clamp01(_t / 0.3f);
        }

        public void Update(float dt)
        {
            if (!IsOpen) return;
            _t += dt;
            Place();
            float stay = Motion.Reduced ? 0.6f : Length + Linger;
            if (_t >= stay) Finish();
        }

        /// <summary>A tap ends the moment at once.</summary>
        public void Tap() => Finish();

        void Finish()
        {
            if (_done) return;
            _done = true;
            _root.SetActive(false);
            Action done = _onDone;
            _onDone = null;
            done?.Invoke();
        }
    }
}

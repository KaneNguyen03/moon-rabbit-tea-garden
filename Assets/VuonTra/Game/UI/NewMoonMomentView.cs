using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using TweenEase = DG.Tweening.Ease;

namespace VuonTra.Game.UI
{
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

            for (int i = 0; i < 14; i++)
            {
                float x = -480f + (i * 173f) % 960f, y = 120f + (i * 311f) % 640f;
                UiKit.Rect("Star", overlay).Place(0.5f, 0.5f, x, y, 10 + (i % 3) * 4, 10 + (i % 3) * 4).AddImage(catalog.Round, Palette.Alpha(Palette.Paper, 0.7f));
            }
            _moon = UiKit.Rect("Moon", overlay).Place(0.5f, 0.5f, 0, High, 300, 300);
            _moon.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            UiKit.Rect("Disc", _moon).Place(0.5f, 0.5f, 0, 0, 170, 170).AddImage(catalog.Round, Palette.Lantern);
            _shade = UiKit.Rect("Shade", _moon).Place(0.5f, 0.5f, 0, 0, 160, 160);
            _shade.AddImage(catalog.Round, Palette.Mix(Palette.Night, Color.black, 0.5f));
            _shade.anchoredPosition = new Vector2(48f, 18f);
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
            Play();
        }

        void Play()
        {
            Transform stage = _root.transform;
            stage.DOKill();
            if (Motion.Reduced)
            {
                _moon.anchoredPosition = new Vector2(0f, High * 0.8f);
                _shade.gameObject.SetActive(true);
                _words.alpha = 1f;
                _veil.alpha = 1f;
                return;
            }
            _moon.anchoredPosition = new Vector2(0f, High);
            _shade.gameObject.SetActive(false);
            _words.alpha = 0f;
            _veil.alpha = 0f;
            DOTween.Sequence()
                .Insert(0f, _veil.DOFade(1f, 0.3f).SetEase(TweenEase.Linear))
                .Insert(0f, _moon.DOAnchorPosY(Low, SetEnd).SetEase(TweenEase.InCubic))
                .InsertCallback(SetEnd, () => _shade.gameObject.SetActive(true))
                .Insert(SetEnd * 0.8f, _words.DOFade(1f, 0.6f).SetEase(TweenEase.Linear))
                .Insert(RiseStart, _moon.DOAnchorPosY(High * 0.8f, Length - RiseStart).SetEase(TweenEase.OutCubic))
                .SetTarget(stage).OnUiClock(stage);
        }

        public void Update(float dt)
        {
            if (!IsOpen) return;
            _t += dt;
            float stay = Motion.Reduced ? 0.6f : Length + Linger;
            if (_t >= stay) Finish();
        }

        public void Tap() => Finish();

        void Finish()
        {
            if (_done) return;
            _done = true;
            _root.transform.DOKill();
            _root.SetActive(false);
            Action done = _onDone;
            _onDone = null;
            done?.Invoke();
        }
    }
}

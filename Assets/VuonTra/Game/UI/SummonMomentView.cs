using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;
using TweenEase = DG.Tweening.Ease;

namespace VuonTra.Game.UI
{
    public sealed class SummonMomentView
    {
        public const float Length = 2.3f;
        const float GateIn = 0.45f, FlashEnd = 1.15f, StepEnd = 1.85f;
        const float Flash = FlashEnd - GateIn, StepStart = FlashEnd - 0.15f, Glide = StepEnd - StepStart, Reveal = Length - StepEnd;
        const float TintHandOver = 0.846f, TintAtHandOver = 0.465f, TintHeld = 0.55f;
        const float SparkleFloor = 0.35f, SparkleRise = 0.114f;
        const float Hops = 3f, HopHeight = 14f;
        const float SwayAngle = 2.5f, SwayHalfPeriod = Mathf.PI / 1.6f;

        readonly GameObject _root, _newBadge, _hint;
        readonly RectTransform _gate, _rabbit, _body, _newRect, _texts, _gateDecor;
        readonly Image _gateDecorImage;
        readonly Image _burst, _tint, _rabbitImage, _plate;
        readonly Text _name, _kind, _newText, _hintText;
        readonly CanvasGroup _textGroup;
        readonly RectTransform[] _sparkles = new RectTransform[10];
        readonly Image[] _sparkleImages = new Image[10];
        readonly CanvasGroup[] _sparkleGlows = new CanvasGroup[10];
        float _t;
        bool _silver;
        Color _tier;
        bool _isNew;

        public bool IsOpen => _root.activeSelf;
        public bool Revealed => _t >= Length;
        public bool ShowsNew => _newBadge.activeSelf;

        public SummonMomentView(RectTransform canvas, GameCatalog catalog)
        {
            var overlay = UiKit.Rect("SummonMoment", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Color.black, 0.45f), 0.95f)).raycastTarget = true;
            var button = _root.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(Tap);

            UiKit.Rect("Veil", overlay).Stretch(0, 0, 1, 1).AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Color.black, 0.45f), 0.8f));
            _burst = UiKit.Rect("Burst", overlay).Place(0.5f, 0.5f, 0, 170, 1300, 1300).AddImage(catalog.Glow, Color.white);
            _gate = UiKit.Rect("Gate", overlay).Place(0.5f, 0.5f, 0, 170, 640, 648);
            if (catalog.MoonGate != null) _gate.AddImage(catalog.MoonGate, Color.white).preserveAspect = true;
            else _gate.AddImage(catalog.Round, Palette.Lantern);
            _tint = UiKit.Rect("Tint", _gate).Place(0.5f, 0.5f, 4, -18, 372, 430).AddImage(catalog.Round, Color.white);
            _gateDecor = UiKit.Rect("GateDecor", _gate).Place(0.5f, 1f, 0, -40, 520, 200);
            _gateDecor.pivot = new Vector2(0.5f, 1f);
            _gateDecorImage = _gateDecor.AddImage(null, Color.white);
            _gateDecorImage.preserveAspect = true;
            _gateDecor.gameObject.SetActive(false);

            for (int i = 0; i < _sparkles.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / _sparkles.Length + 0.3f, reach = 300f + (i % 3) * 46f;
                var dot = UiKit.Rect("Sparkle", overlay).Place(0.5f, 0.5f, Mathf.Cos(angle) * reach, 170f + Mathf.Sin(angle) * reach, 54, 54);
                RectTransform glow = UiKit.Rect("Glow", dot).Stretch(0, 0, 1, 1);
                _sparkleImages[i] = glow.AddImage(catalog.Glow, Color.white);
                _sparkleGlows[i] = glow.gameObject.AddComponent<CanvasGroup>();
                UiKit.Rect("Core", dot).Place(0.5f, 0.5f, 0, 0, 10, 10).AddImage(catalog.Round, Color.white);
                _sparkles[i] = dot;
            }

            _rabbit = UiKit.Rect("Rabbit", overlay).Place(0.5f, 0.5f, 0, 60, 460, 460);
            _rabbit.pivot = new Vector2(0.5f, 0f);
            _body = UiKit.Rect("Body", _rabbit).Stretch(0, 0, 1, 1);
            _rabbitImage = _body.AddImage(null, Color.black);
            _rabbitImage.preserveAspect = true;

            _newRect = UiKit.Rect("New", overlay).Place(0.5f, 0.5f, 210, 470, 190, 76);
            _newRect.localRotation = Quaternion.Euler(0, 0, -10);
            _newRect.AddImage(catalog.Panel, Palette.Lantern, true);
            _newText = UiKit.Rect("Text", _newRect).Stretch(0, 0, 1, 1).AddText("", 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _newBadge = _newRect.gameObject;

            _texts = UiKit.Rect("Texts", overlay).Place(0.5f, 0.5f, 0, -290, 980, 260);
            _textGroup = _texts.gameObject.AddComponent<CanvasGroup>();
            _textGroup.blocksRaycasts = false;
            _name = UiKit.Rect("Name", _texts).Stretch(0, 0.5f, 1, 1).AddText("", 76, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _name.resizeTextForBestFit = true;
            _name.resizeTextMinSize = 40;
            _name.resizeTextMaxSize = 76;
            var plate = UiKit.Rect("Kind", _texts).Place(0.5f, 0.5f, 0, -44, 620, 64);
            _plate = plate.AddImage(catalog.Panel, Color.white, true);
            _kind = UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1, 16, 0, 16, 0).AddText("", 34, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _kind.resizeTextForBestFit = true;
            _kind.resizeTextMinSize = 20;
            _kind.resizeTextMaxSize = 34;

            var hint = UiKit.Rect("Hint", overlay).Place(0.5f, 0f, 0, 380, 900, 50);
            _hintText = hint.AddText("", 28, Palette.Alpha(Palette.Paper, 0.75f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint = hint.gameObject;

            _root.SetActive(false);
        }

        public void Show(Sprite art, string name, string kindLine, Color tier, bool isNew, string newLabel, string hint, bool silver = false)
        {
            _silver = silver;
            _t = 0f;
            _tier = tier;
            _isNew = isNew;
            _rabbitImage.sprite = art;
            _name.text = name;
            _kind.text = kindLine;
            _plate.color = tier;
            _newText.text = newLabel;
            _hintText.text = hint;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Play();
            ShowSwitches();
        }

        public void Tap()
        {
            if (!IsOpen) return;
            if (!Revealed)
            {
                _t = Length;
                _root.transform.DOGoto(Length, true);
                ShowSwitches();
            }
            else Close();
        }

        public void Close()
        {
            StopAll();
            _root.SetActive(false);
        }

        public void SetSeasonDecor(Sprite decor)
        {
            bool show = decor != null;
            if (show) _gateDecorImage.sprite = decor;
            if (_gateDecor.gameObject.activeSelf != show) _gateDecor.gameObject.SetActive(show);
            _gateDecor.DOKill();
            _gateDecor.localRotation = Quaternion.identity;
        }

        public void Update(float dt)
        {
            if (!IsOpen) return;
            Sway(!Motion.Reduced && _gateDecor.gameObject.activeSelf);
            if (_t >= Length + 1f) return;
            _t += dt;
            ShowSwitches();
        }

        void Sway(bool on)
        {
            bool swaying = DOTween.IsTweening(_gateDecor);
            if (on == swaying) return;
            _gateDecor.DOKill();
            if (!on)
            {
                _gateDecor.localRotation = Quaternion.identity;
                return;
            }
            _gateDecor.localRotation = Quaternion.Euler(0f, 0f, -SwayAngle);
            _gateDecor.DOLocalRotate(new Vector3(0f, 0f, SwayAngle), SwayHalfPeriod).SetEase(TweenEase.InOutSine)
                .SetLoops(-1, LoopType.Yoyo).OnUiClock(_gateDecor).Goto(SwayHalfPeriod / 2f, true);
        }

        void StopAll()
        {
            _root.transform.DOKill();
            _gateDecor.DOKill();
            foreach (RectTransform dot in _sparkles) dot.DOKill();
            foreach (Image glow in _sparkleImages) glow.DOKill();
        }

        void ShowSwitches()
        {
            int lit = _silver ? _sparkles.Length : 4;
            for (int i = 0; i < _sparkles.Length; i++)
            {
                bool on = i < lit && _t > GateIn * 0.6f;
                if (_sparkles[i].gameObject.activeSelf != on) _sparkles[i].gameObject.SetActive(on);
            }
            bool stepping = _t >= StepStart;
            if (_rabbit.gameObject.activeSelf != stepping) _rabbit.gameObject.SetActive(stepping);
            bool shown = _isNew && _t >= Length - 0.12f;
            if (_newBadge.activeSelf != shown) _newBadge.SetActive(shown);
            bool done = _t >= Length + 0.4f;
            if (_hint.activeSelf != done) _hint.SetActive(done);
        }

        static float WhenGlideReaches(float share) => Glide * (1f - Mathf.Pow(1f - share, 1f / 3f));

        void Play()
        {
            StopAll();
            Color light = _silver ? Color.white : _tier;
            float tint = _silver ? 0.95f : 0.85f, burst = _silver ? 0.6f : 0.45f;
            _gate.localScale = Vector3.one * 0.3f;
            _burst.color = Palette.Alpha(light, 0.10f);
            _burst.rectTransform.localScale = Vector3.one * 0.7f;
            _tint.color = Palette.Alpha(light, 0f);
            _rabbit.anchoredPosition = new Vector2(0f, 210f);
            _rabbit.localScale = Vector3.one * 0.42f;
            _body.anchoredPosition = Vector2.zero;
            _rabbitImage.color = new Color(0.05f, 0.03f, 0.1f, 0.95f);
            _textGroup.alpha = 0f;
            _texts.anchoredPosition = new Vector2(0f, -320f);
            _newRect.localScale = Vector3.one;
            float half = Flash / 2f;

            Sequence timeline = DOTween.Sequence()
                .Insert(0f, _gate.DOScale(1f, GateIn).SetEase(TweenEase.OutBack))
                .Insert(GateIn, _burst.DOFade(0.10f + burst, half).SetEase(TweenEase.OutSine))
                .Insert(GateIn + half, _burst.DOFade(0.10f, half).SetEase(TweenEase.InSine))
                .Insert(GateIn, _burst.rectTransform.DOScale(1.2f, half).SetEase(TweenEase.OutSine))
                .Insert(GateIn + half, _burst.rectTransform.DOScale(0.7f, half).SetEase(TweenEase.InSine))
                .Insert(GateIn, _tint.DOFade(tint, half).SetEase(TweenEase.OutSine))
                .Insert(GateIn + half, _tint.DOFade(tint * TintAtHandOver, Flash * (TintHandOver - 0.5f)).SetEase(TweenEase.InSine))
                .Insert(GateIn + Flash * TintHandOver, _tint.DOFade(tint * TintHeld, Flash * (1f - TintHandOver)).SetEase(TweenEase.Linear))
                .Insert(GateIn + Flash * SparkleRise, Envelope(1f, half - Flash * SparkleRise, TweenEase.OutSine))
                .Insert(GateIn + half, Envelope(SparkleFloor, half - Flash * SparkleRise, TweenEase.InSine))
                .Insert(StepStart, _rabbit.DOAnchorPosY(20f, Glide).SetEase(TweenEase.OutCubic))
                .Insert(StepStart, _rabbit.DOScale(1f, Glide).SetEase(TweenEase.OutCubic))
                .Insert(StepEnd, _rabbitImage.DOColor(Color.white, Reveal).SetEase(TweenEase.OutCubic))
                .Insert(StepEnd, _textGroup.DOFade(1f, Reveal).SetEase(TweenEase.Linear))
                .Insert(StepEnd, _texts.DOAnchorPosY(-290f, Reveal).SetEase(TweenEase.OutCubic))
                .Insert(Length - 0.12f, _newRect.DOScale(1.25f, 0.21f).SetEase(TweenEase.OutSine))
                .Insert(Length + 0.09f, _newRect.DOScale(1f, 0.21f).SetEase(TweenEase.InSine));
            for (int hop = 0; hop < Hops; hop++)
            {
                float peak = (hop + 0.5f) / Hops;
                float leave = WhenGlideReaches(hop / Hops), top = WhenGlideReaches(peak), land = WhenGlideReaches((hop + 1f) / Hops);
                timeline.Insert(StepStart + leave, _body.DOAnchorPosY(HopHeight * (1f - peak), top - leave).SetEase(TweenEase.OutSine))
                    .Insert(StepStart + top, _body.DOAnchorPosY(0f, land - top).SetEase(TweenEase.InSine));
            }
            timeline.SetTarget(_root.transform).OnUiClock(_root.transform);

            Color sparkle = _silver ? Color.white : Palette.Mix(_tier, Color.white, 0.5f);
            float grow = _silver ? 1.1f : 0.6f;
            for (int i = 0; i < _sparkles.Length; i++)
            {
                float speed = 5f + i % 4, halfPeriod = Mathf.PI / speed;
                float phase = Mathf.Repeat(i * 1.7f + Mathf.PI / 2f, 2f * Mathf.PI) / speed;
                _sparkleGlows[i].alpha = SparkleFloor;
                _sparkleImages[i].color = Palette.Alpha(sparkle, 0.25f);
                _sparkles[i].localScale = Vector3.one * 0.6f;
                _sparkleImages[i].DOFade(1f, halfPeriod).SetEase(TweenEase.InOutSine).SetLoops(-1, LoopType.Yoyo).OnUiClock(_sparkles[i]).Goto(phase, true);
                _sparkles[i].DOScale(0.6f + grow, halfPeriod).SetEase(TweenEase.InOutSine).SetLoops(-1, LoopType.Yoyo).OnUiClock(_sparkles[i]).Goto(phase, true);
            }
        }

        Tween Envelope(float to, float seconds, TweenEase ease) =>
            DOTween.To(() => _sparkleGlows[0].alpha, a => { foreach (CanvasGroup glow in _sparkleGlows) glow.alpha = a; }, to, seconds).SetEase(ease);
    }
}

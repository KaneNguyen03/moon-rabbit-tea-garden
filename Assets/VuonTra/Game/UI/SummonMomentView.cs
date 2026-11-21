using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The moment a rabbit arrives: the moon gate appears, its light turns the colour of the rabbit's tier, a
    /// shadow steps out, and then it is revealed with its name and kind. A tap skips to the end; another closes.
    /// </summary>
    public sealed class SummonMomentView
    {
        /// <summary>Seconds from the gate appearing to the rabbit fully revealed.</summary>
        public const float Length = 2.3f;
        const float GateIn = 0.45f, FlashEnd = 1.15f, StepEnd = 1.85f;

        readonly GameObject _root, _newBadge, _hint;
        readonly RectTransform _gate, _rabbit, _newRect, _texts;
        readonly Image _burst, _tint, _rabbitImage, _plate;
        readonly Text _name, _kind, _newText, _hintText;
        readonly CanvasGroup _textGroup;
        readonly RectTransform[] _sparkles = new RectTransform[10];
        readonly Image[] _sparkleImages = new Image[10];
        float _t;
        bool _silver;
        Color _tier;
        bool _isNew;

        public bool IsOpen => _root.activeSelf;
        /// <summary>True once the rabbit has been revealed (or the player skipped to it).</summary>
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

            // A second veil: one alone still lets the page behind show through the name.
            UiKit.Rect("Veil", overlay).Stretch(0, 0, 1, 1).AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Color.black, 0.45f), 0.8f));
            _burst = UiKit.Rect("Burst", overlay).Place(0.5f, 0.5f, 0, 170, 1300, 1300).AddImage(catalog.Glow, Color.white);
            _gate = UiKit.Rect("Gate", overlay).Place(0.5f, 0.5f, 0, 170, 640, 648);
            if (catalog.MoonGate != null) _gate.AddImage(catalog.MoonGate, Color.white).preserveAspect = true;
            else _gate.AddImage(catalog.Round, Palette.Lantern);
            // The light inside the gate takes the tier's colour.
            _tint = UiKit.Rect("Tint", _gate).Place(0.5f, 0.5f, 4, -18, 372, 430).AddImage(catalog.Round, Color.white);

            // Glints round the gate. Every arrival has a few; silver, whose colour is nearly the gate's own, has many and bright.
            for (int i = 0; i < _sparkles.Length; i++)
            {
                float angle = i * Mathf.PI * 2f / _sparkles.Length + 0.3f, reach = 300f + (i % 3) * 46f;
                var dot = UiKit.Rect("Sparkle", overlay).Place(0.5f, 0.5f, Mathf.Cos(angle) * reach, 170f + Mathf.Sin(angle) * reach, 54, 54);
                _sparkleImages[i] = dot.AddImage(catalog.Glow, Color.white);
                UiKit.Rect("Core", dot).Place(0.5f, 0.5f, 0, 0, 10, 10).AddImage(catalog.Round, Color.white);
                _sparkles[i] = dot;
            }

            _rabbit = UiKit.Rect("Rabbit", overlay).Place(0.5f, 0.5f, 0, 60, 460, 460);
            _rabbit.pivot = new Vector2(0.5f, 0f);
            _rabbitImage = _rabbit.AddImage(null, Color.black);
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

        /// <param name="kindLine">The kind and tier, e.g. "Thỏ Sao · bậc Vàng".</param>
        /// <param name="silver">The commonest tier: its light is white with a shower of glints instead of a colour.</param>
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
            Apply();
        }

        /// <summary>A tap skips straight to the rabbit; a tap after that closes the moment.</summary>
        public void Tap()
        {
            if (!IsOpen) return;
            if (!Revealed)
            {
                _t = Length;
                Apply();
            }
            else Close();
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        public void Update(float dt)
        {
            if (!IsOpen || _t >= Length + 1f) return;
            _t += dt;
            Apply();
        }

        static float Span(float t, float from, float to) => Mathf.Clamp01((t - from) / (to - from));

        void Apply()
        {
            // The gate pops in.
            float gate = Ease.OutBack(Span(_t, 0f, GateIn));
            _gate.localScale = Vector3.one * Mathf.LerpUnclamped(0.3f, 1f, gate);

            // Its light swells to the tier's colour, then settles.
            float flash = Span(_t, GateIn, FlashEnd), pulse = Mathf.Sin(flash * Mathf.PI);
            float held = Mathf.Max(flash * 0.55f, pulse);
            Color light = _silver ? Color.white : _tier;
            _tint.color = Palette.Alpha(light, held * (_silver ? 0.95f : 0.85f));
            _burst.color = Palette.Alpha(light, 0.10f + pulse * (_silver ? 0.6f : 0.45f));
            int lit = _silver ? _sparkles.Length : 4;
            for (int i = 0; i < _sparkles.Length; i++)
            {
                bool on = i < lit && _t > GateIn * 0.6f;
                if (_sparkles[i].gameObject.activeSelf != on) _sparkles[i].gameObject.SetActive(on);
                if (!on) continue;
                float twinkle = 0.5f + 0.5f * Mathf.Sin(_t * (5f + i % 4) + i * 1.7f);
                _sparkleImages[i].color = Palette.Alpha(_silver ? Color.white : Palette.Mix(_tier, Color.white, 0.5f), (0.25f + 0.75f * twinkle) * Mathf.Max(0.35f, pulse));
                _sparkles[i].localScale = Vector3.one * (0.6f + twinkle * (_silver ? 1.1f : 0.6f));
            }
            _burst.rectTransform.localScale = Vector3.one * (0.7f + pulse * 0.5f);

            // A shadow steps out of the gate towards us.
            float step = Ease.OutCubic(Span(_t, FlashEnd - 0.15f, StepEnd));
            _rabbit.gameObject.SetActive(_t >= FlashEnd - 0.15f);
            _rabbit.anchoredPosition = new Vector2(0, Mathf.Lerp(210f, 20f, step) + Mathf.Abs(Mathf.Sin(step * Mathf.PI * 3f)) * 14f * (1f - step));
            _rabbit.localScale = Vector3.one * Mathf.Lerp(0.42f, 1f, step);

            // Then the shadow lifts and it is itself.
            float reveal = Span(_t, StepEnd, Length);
            _rabbitImage.color = Color.Lerp(new Color(0.05f, 0.03f, 0.1f, 0.95f), Color.white, Ease.OutCubic(reveal));
            _textGroup.alpha = reveal;
            _texts.anchoredPosition = new Vector2(0, -290f - 30f * (1f - Ease.OutCubic(reveal)));

            bool shown = _isNew && _t >= Length - 0.12f;
            if (_newBadge.activeSelf != shown) _newBadge.SetActive(shown);
            if (shown) _newRect.localScale = Vector3.one * (1f + 0.25f * Ease.Pop(Span(_t, Length - 0.12f, Length + 0.3f)));
            bool done = _t >= Length + 0.4f;
            if (_hint.activeSelf != done) _hint.SetActive(done);
        }
    }
}

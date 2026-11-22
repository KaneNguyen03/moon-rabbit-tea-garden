using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The compare card before a new moon (#1): what the save keeps, what goes back to the start and what the
    /// season brings, with the save's own numbers. Beginning takes two steps: hold the button a while to unlock
    /// it, then press it once more. A finger that slides off unlocks nothing.
    /// </summary>
    public sealed class NewMoonView
    {
        public const float HoldSeconds = 1.5f;

        /// <summary>The only way in: a finger down, up, or gone. The button under it is for size and looks.</summary>
        sealed class Sensor : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
        {
            public NewMoonView View;
            public void OnPointerDown(PointerEventData e) => View.Hold();
            public void OnPointerUp(PointerEventData e) => View.Release();
            public void OnPointerExit(PointerEventData e) => View.Cancel();
        }

        readonly GameObject _root;
        readonly RectTransform _card, _fill;
        readonly Tweens _tweens;
        readonly Text _keep, _lose, _gain, _beginLabel;
        readonly ChunkyButton _begin;
        readonly Action _onBegin;
        float _held;
        bool _holding, _armed;
        int _seasonAfter;

        public bool IsOpen => _root.activeSelf;
        /// <summary>The hold is done: the next press begins the season.</summary>
        public bool Armed => _armed;
        public float HoldProgress => Mathf.Clamp01(_held / HoldSeconds);

        public NewMoonView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onBegin)
        {
            _tweens = tweens;
            _onBegin = onBegin;
            var overlay = UiKit.Rect("NewMoon", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 900, 1460);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            // The moon over the title: the thing the card is about.
            var moon = UiKit.Rect("Moon", _card).Place(0.5f, 1f, 0, -70, 150, 150);
            moon.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.5f));
            UiKit.Rect("Disc", moon).Place(0.5f, 0.5f, 0, 0, 86, 86).AddImage(catalog.Round, Palette.Lantern);
            UiKit.Rect("Shade", moon).Place(0.5f, 0.5f, 24, 10, 72, 72).AddImage(catalog.Round, Palette.Paper);
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -180, 800, 70)
                .AddText(L10n.T("newmoon.title"), 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -275, 800, 90)
                .AddText(L10n.T("newmoon.sub"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;

            _keep = Panel(catalog, "Keep", -470, L10n.T("newmoon.keep"), Palette.Mix(Palette.Moss, Palette.Paper, 0.72f));
            _lose = Panel(catalog, "Lose", -720, L10n.T("newmoon.lose"), Palette.Mix(Palette.Blush, Palette.Paper, 0.6f));
            _gain = Panel(catalog, "Gain", -970, L10n.T("newmoon.gain"), Palette.Mix(Palette.Lantern, Palette.Paper, 0.6f));

            // The button that needs a hold, then a press. Its face fills while the finger rests on it.
            var begin = UiKit.Rect("Begin", _card).Place(0.5f, 0f, 0, 190, 760, 124);
            _begin = UiKit.Chunky(begin, catalog.Panel, null);
            _begin.Lip.color = Palette.Mix(Palette.Track, Palette.Ink, 0.25f);
            _begin.Face.color = Palette.Track;
            _fill = UiKit.Rect("Fill", _begin.FaceRect).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Lantern, true);
            _fill.SetSiblingIndex(0);
            _beginLabel = UiKit.Rect("Text", _begin.FaceRect).Stretch(0, 0, 1, 1, 16, 0, 16, 0)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _beginLabel.resizeTextForBestFit = true;
            _beginLabel.resizeTextMinSize = 18;
            _beginLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);
            var sensor = begin.gameObject.AddComponent<Sensor>();
            sensor.View = this;

            var cancel = UiKit.Rect("Cancel", _card).Place(0.5f, 0f, 0, 70, 700, UiKit.MinTouch);
            Image cancelHit = cancel.AddImage(null, new Color(0, 0, 0, 0));
            cancelHit.raycastTarget = true;
            var cancelButton = cancel.gameObject.AddComponent<Button>();
            cancelButton.transition = Selectable.Transition.None;
            cancelButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", cancel).Stretch(0, 0, 1, 1).AddText(L10n.T("newmoon.cancel"), UiKit.TextTitle, Palette.InkSoft);

            _root.SetActive(false);
        }

        Text Panel(GameCatalog catalog, string id, float y, string heading, Color tint)
        {
            var panel = UiKit.Rect(id, _card).Place(0.5f, 1f, 0, y, 800, 230);
            panel.AddImage(catalog.Panel, tint, true);
            // Place puts the rect's middle at the point given, so the heading's middle sits half its width in from the left edge.
            UiKit.Rect("Heading", panel).Place(0f, 1f, 24 + 250, -41, 500, 50).AddText(heading, UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text body = UiKit.Rect("Body", panel).Stretch(0, 0, 1, 1, 24, 16, 24, 70).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.UpperLeft);
            body.horizontalOverflow = HorizontalWrapMode.Wrap;
            body.resizeTextForBestFit = true;
            body.resizeTextMinSize = 16;
            body.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            return body;
        }

        static int Percent(double bonus) => (int)Math.Round((bonus - 1d) * 100d);

        public void Open(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s)
        {
            NewMoonPreview p = NewMoon.Compare(defs, s);
            _seasonAfter = p.SeasonAfter;
            _keep.text = L10n.T("newmoon.keep.list", p.Rabbits, p.Items, p.GoldenCarrots, p.Clovers);
            _lose.text = L10n.T("newmoon.lose.list", p.Coins.ToShortString(), p.StationLevels, p.StationsOpen, p.Plots, Math.Min(defs.Count, Plots.Starting), p.RestSpots, RestSpots.Starting, p.GardenLevel);
            _gain.text = L10n.T("newmoon.gain.list", Percent(p.BonusBefore), Percent(p.BonusAfter), p.GiftClovers, p.GiftGolden) + (p.MoonRabbit ? L10n.T("newmoon.gain.rabbit") : "");
            _holding = false;
            _armed = false;
            _held = 0f;
            ShowButton();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
        }

        void ShowButton()
        {
            _fill.anchorMax = new Vector2(_armed ? 1f : HoldProgress, 1f);
            _fill.gameObject.SetActive(_armed || _held > 0f);
            _begin.Face.color = _armed ? Palette.Carrot : Palette.Track;
            _begin.Lip.color = _armed ? Palette.CarrotLip : Palette.Mix(Palette.Track, Palette.Ink, 0.25f);
            _beginLabel.color = _armed ? Palette.Card : Palette.Ink;
            _beginLabel.text = _armed ? L10n.T("newmoon.armed", _seasonAfter) : L10n.T("newmoon.hold");
        }

        /// <summary>A finger lands on the button: an armed button begins the season on release; otherwise the hold starts.</summary>
        public void Hold()
        {
            if (!IsOpen) return;
            _holding = true;
            if (!_armed) _held = 0f;
            _begin.Press(_tweens);
        }

        /// <summary>The finger lifts: a finished hold arms the button; an armed button begins the season.</summary>
        public void Release()
        {
            if (!IsOpen || !_holding) return;
            _holding = false;
            if (_armed)
            {
                Sfx.Sting();
                Close();
                _onBegin?.Invoke();
                return;
            }
            if (_held >= HoldSeconds)
            {
                _armed = true;
                Sfx.Blip();
                Haptics.Medium();
            }
            else _held = 0f;
            ShowButton();
        }

        /// <summary>The finger slid off: whatever hold was building is dropped.</summary>
        public void Cancel()
        {
            if (!_holding) return;
            _holding = false;
            if (!_armed) _held = 0f;
            ShowButton();
        }

        public void Update(float dt)
        {
            if (!IsOpen || !_holding || _armed) return;
            float before = _held;
            _held += dt;
            if (before < HoldSeconds && _held >= HoldSeconds) Haptics.Light();
            ShowButton();
        }

        public void Close()
        {
            _holding = false;
            _root.SetActive(false);
        }
    }
}

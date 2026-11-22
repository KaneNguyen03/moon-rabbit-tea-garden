using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// Where the carrots come from: one row per brewing station with what it earns a second and the rabbit
    /// and ingredient factors behind it, then the garden-wide multipliers and the total. Opens from the purse.
    /// </summary>
    public sealed class IncomeView
    {
        readonly GameObject _root;
        readonly RectTransform _card, _content;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly List<GameObject> _rows = new List<GameObject>();

        public bool IsOpen => _root.activeSelf;

        public IncomeView(RectTransform canvas, GameCatalog catalog, Tweens tweens)
        {
            _catalog = catalog;
            _tweens = tweens;

            var overlay = UiKit.Rect("Income", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.56f, 0, 0, 980, 900);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 40, -120, 130, 20)
                .AddText(L10n.T("income.title"), 42, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, 80, 80);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var scrollRt = UiKit.Rect("Scroll", _card).Stretch(0, 0, 1, 1, 0, 28, 0, 130);
            scrollRt.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            _content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            _content.pivot = new Vector2(0.5f, 1f);
            var list = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset((int)UiKit.CardPad, (int)UiKit.CardPad, 4, 30);
            list.spacing = UiKit.CardGap;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
            scroll.viewport = scrollRt;

            _root.SetActive(false);
        }

        public void Open(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
            Refresh(defs, s, m);
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            if (!IsOpen) return;
            foreach (GameObject go in _rows)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }
            _rows.Clear();

            int shown = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                BigNum perSecond = Economy.StationIncomePerSecond(defs, s, i, out double rabbit, out double ingredients);
                if (perSecond.IsZero) continue;
                shown++;
                var row = UiKit.Rect("Station " + defs[i].Id, _content);
                row.AddImage(_catalog.Panel, Palette.Card, true);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 118;
                Sprite prop = _catalog.Stations[i].Prop;
                if (prop != null) UiKit.Rect("Icon", row).Place(0f, 0.5f, 62, 0, 84, 84).AddImage(prop, Color.white).preserveAspect = true;
                Text name = UiKit.Rect("Name", row).Stretch(0, 0.5f, 0.6f, 1, 120, 0, 0, 14)
                    .AddText(L10n.Station(defs[i].Id), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 16;
                name.resizeTextMaxSize = UiKit.TextBody;
                Text factors = UiKit.Rect("Factors", row).Stretch(0, 0, 0.6f, 0.5f, 120, 12, 0, 0)
                    .AddText(L10n.T("income.factors", UiKit.Times(rabbit), UiKit.Times(ingredients)), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
                factors.resizeTextForBestFit = true;
                factors.resizeTextMinSize = 14;
                factors.resizeTextMaxSize = UiKit.TextSmall;
                UiKit.Rect("Amount", row).Stretch(0.6f, 0, 1, 1, 0, 0, 28, 0)
                    .AddText(L10n.T("income.row", perSecond.Short()), UiKit.TextTitle, Palette.CarrotLip, TextAnchor.MiddleRight, FontStyle.Bold);
                _rows.Add(row.gameObject);
            }
            if (shown == 0) AddNote("Empty", L10n.T("income.empty"), Palette.InkSoft);

            // Garden-wide: charms, speed, the boost, and the total the purse shows.
            bool boosting = s.BoostSecondsLeft > 0d;
            AddNote("Charms", L10n.T("income.charms", UiKit.Times(m.IncomeMultiplier)), Palette.Ink);
            AddNote("Speed", L10n.T("income.speed", UiKit.Times(m.SpeedMultiplier)), Palette.Ink);
            if (boosting) AddNote("Boost", L10n.T("income.boost", UiKit.Times(Energy.BoostSpeed)), Palette.Moss);
            BigNum total = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * (boosting ? Energy.BoostSpeed : 1d));
            AddNote("Total", L10n.T("income.total", total.Short()), Palette.CarrotLip, true);
        }

        void AddNote(string name, string text, Color color, bool big = false)
        {
            var note = UiKit.Rect(name, _content);
            note.gameObject.AddComponent<LayoutElement>().preferredHeight = big ? 70 : 44;
            note.AddText(text, big ? UiKit.TextTitle : UiKit.TextBody, color, TextAnchor.MiddleLeft, big ? FontStyle.Bold : FontStyle.Normal);
            _rows.Add(note.gameObject);
        }
    }
}

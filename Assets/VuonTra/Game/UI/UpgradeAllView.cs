using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class UpgradeAllView
    {
        static readonly double[] Shares = { 1d, 0.5d, 0.25d };

        readonly GameObject _root;
        readonly RectTransform _card, _content;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action _onConfirm;
        readonly Action<double> _onShare;
        readonly Text _total;
        readonly ChunkyButton _confirm;
        readonly Dictionary<double, Image> _shareButtons = new Dictionary<double, Image>();
        readonly Dictionary<double, Text> _shareLabels = new Dictionary<double, Text>();
        readonly List<GameObject> _rows = new List<GameObject>();
        string _signature;

        public bool IsOpen => _root.activeSelf;
        public List<UpgradeStep> Plan { get; private set; } = new List<UpgradeStep>();

        public UpgradeAllView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<double> onShare, Action onConfirm)
        {
            _catalog = catalog;
            _tweens = tweens;
            _onShare = onShare;
            _onConfirm = onConfirm;

            var overlay = UiKit.Rect("UpgradeAll", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrimButton = _root.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 980, 1000);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 40, -110, 130, 20)
                .AddText(L10n.T("upgradeall.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -66, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            UiKit.Rect("KeepLabel", _card).Stretch(0, 1, 0.42f, 1, 40, -186, 0, 126)
                .AddText(L10n.T("upgradeall.keep"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            var track = UiKit.Rect("Shares", _card).Place(1f, 1f, -230, -156, 400, 64);
            track.AddImage(catalog.Panel, Palette.Track, true);
            for (int i = 0; i < Shares.Length; i++)
            {
                double share = Shares[i];
                var rt = UiKit.Rect("Share " + share.ToString(System.Globalization.CultureInfo.InvariantCulture), track).Place(0f, 0.5f, 66 + i * 134f, 0, 124, 52);
                UiKit.Touchable(rt, 124, 52);
                Image img = rt.AddImage(catalog.Panel, Palette.Carrot, true);
                img.raycastTarget = true;
                var button = rt.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = img;
                button.onClick.AddListener(() => { Sfx.Click(); _onShare(share); });
                _shareButtons[share] = img;
                _shareLabels[share] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(Mathf.RoundToInt((float)(share * 100d)) + "%", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            var scrollRt = UiKit.Rect("Scroll", _card).Stretch(0, 0, 1, 1, 0, 230, 0, 200);
            scrollRt.AddTapCatcher();
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            _content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            _content.pivot = new Vector2(0.5f, 1f);
            var list = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(40, 40, 4, 20);
            list.spacing = 12;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
            scroll.viewport = scrollRt;

            _total = UiKit.Rect("Total", _card).Stretch(0, 0, 1, 0, 40, 160, 40, -60)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _total.resizeTextForBestFit = true;
            _total.resizeTextMinSize = 20;
            _total.resizeTextMaxSize = UiKit.TextTitle;

            var cancel = UiKit.Rect("Cancel", _card).Place(0f, 0f, 190, 86, 260, UiKit.MinTouch);
            UiKit.Secondary(cancel, catalog.Panel, L10n.T("upgradeall.cancel"), Close);
            var confirm = UiKit.Rect("Confirm", _card).Place(1f, 0f, -250, 86, 420, 96);
            _confirm = UiKit.Chunky(confirm, catalog.Panel, () => _onConfirm());
            UiKit.Rect("Text", _confirm.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("upgradeall.confirm"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);

            _root.SetActive(false);
        }

        public void Open(IReadOnlyList<StationDef> defs, GameState s)
        {
            _signature = null;
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(Motion.Panel), t => _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t), Ease.OutBack);
            Refresh(defs, s);
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!IsOpen) return;
            var sb = new System.Text.StringBuilder();
            sb.Append(L10n.Code).Append('|').Append(s.Coins.Short()).Append('|').Append(s.SpendShare).Append('|');
            foreach (int level in s.Levels) sb.Append(level).Append(',');
            string signature = sb.ToString();
            if (signature == _signature) return;
            _signature = signature;

            foreach (KeyValuePair<double, Image> pair in _shareButtons)
            {
                bool on = pair.Key == s.SpendShare;
                pair.Value.color = on ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _shareLabels[pair.Key].color = on ? Palette.OnCarrot : Palette.InkSoft;
            }

            foreach (GameObject go in _rows)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
            _rows.Clear();

            Plan = UpgradePlan.Plan(defs, s, Simulation.Budget(s));
            BigNum total = UpgradePlan.Total(Plan);
            if (Plan.Count == 0)
            {
                var note = UiKit.Rect("Empty", _content);
                note.gameObject.AddComponent<LayoutElement>().preferredHeight = 100;
                string empty = UpgradePlan.Unlocked(s) ? L10n.T("upgradeall.empty") : L10n.T("upgradeall.locked", UpgradePlan.UnlockLevel);
                note.AddText(empty, UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
                _rows.Add(note.gameObject);
            }
            foreach (UpgradeStep step in Plan) AddRow(defs, step);
            _total.text = L10n.T("upgradeall.total", total.Short(), (s.Coins - total).Short());
            _confirm.SetEnabled(Plan.Count > 0);
        }

        void AddRow(IReadOnlyList<StationDef> defs, UpgradeStep step)
        {
            StationData data = _catalog.Stations[step.Station];
            var root = UiKit.Rect("Step " + defs[step.Station].Id, _content);
            root.AddImage(_catalog.Panel, Palette.Card, true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 92;
            var icon = UiKit.Rect("Icon", root).Place(0f, 0.5f, 50, 0, 68, 68);
            icon.AddImage(_catalog.Round, Palette.Mix(data.Accent, Palette.Paper, 0.62f));
            if (data.Prop != null) UiKit.Rect("Prop", icon).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(data.Prop, Color.white).preserveAspect = true;
            Text name = UiKit.Rect("Name", root).Stretch(0, 0, 0.5f, 1, 100, 0, 0, 0)
                .AddText(L10n.Station(defs[step.Station].Id), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 16;
            name.resizeTextMaxSize = UiKit.TextBody;
            UiKit.Rect("Levels", root).Stretch(0.5f, 0, 0.75f, 1, 0, 0, 0, 0)
                .AddText(L10n.T("upgradeall.levels", step.Levels), UiKit.TextBody, Palette.Mix(Palette.Moss, Palette.Ink, 0.45f), TextAnchor.MiddleRight, FontStyle.Bold);
            UiKit.Rect("Cost", root).Stretch(0.75f, 0, 1, 1, 0, 0, 24, 0)
                .AddText(step.Cost.Short(), UiKit.TextBody, Palette.CarrotLip, TextAnchor.MiddleRight, FontStyle.Bold);
            _rows.Add(root.gameObject);
        }
    }
}

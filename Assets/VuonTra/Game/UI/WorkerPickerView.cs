using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class WorkerPickerView
    {
        readonly GameObject _root;
        readonly RectTransform _card, _content;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action<long, int> _onAssign;
        readonly Text _title;
        readonly List<GameObject> _rows = new List<GameObject>();
        int _station = -1;
        string _signature;

        public bool IsOpen => Sheet.IsOpen;
        public BottomSheet Sheet { get; private set; }

        public WorkerPickerView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<long, int> onAssign)
        {
            _catalog = catalog;
            _tweens = tweens;
            _onAssign = onAssign;

            Sheet = new BottomSheet(canvas, "WorkerPicker", catalog, tweens, 900f, OnClosed);
            _root = Sheet.Root;
            _card = Sheet.Card;
            _title = UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 40, -120, 130, 20)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _title.resizeTextForBestFit = true;
            _title.resizeTextMinSize = 22;
            _title.resizeTextMaxSize = UiKit.TextTitle;

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var scrollRt = UiKit.Rect("Scroll", _card).Stretch(0, 0, 1, 1, 0, 28, 0, 130);
            scrollRt.AddTapCatcher();
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

        public void Open(IReadOnlyList<StationDef> defs, GameState s, int station)
        {
            if (station < 0 || station >= defs.Count || s.Levels[station] <= 0) return;
            _station = station;
            _signature = null;
            _title.text = L10n.T("worker.title", L10n.Station(defs[station].Id));
            Sheet.Open();
            Refresh(defs, s);
        }

        public void Close() => Sheet.Close();

        void OnClosed() { }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!IsOpen) return;
            if (_station >= defs.Count || s.Levels[_station] <= 0)
            {
                Close();
                return;
            }

            var sb = new System.Text.StringBuilder();
            sb.Append(L10n.Code).Append('|');
            foreach (Rabbit r in s.Rabbits)
                sb.Append(r.Id).Append(':').Append(r.Station).Append(':').Append(r.Level).Append(':').Append(r.Kind).Append(':').Append(r.Name).Append(';');
            string signature = sb.ToString();
            if (signature == _signature) return;
            _signature = signature;

            foreach (GameObject go in _rows)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
            _rows.Clear();

            if (s.Rabbits.Count == 0)
            {
                var note = UiKit.Rect("Empty", _content);
                note.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
                note.AddText(L10n.T("worker.empty"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
                _rows.Add(note.gameObject);
                return;
            }

            var rabbits = new List<Rabbit>(s.Rabbits);
            rabbits.Sort((a, b) =>
            {
                bool sa = Rabbits.Suits(defs, a, _station), sb2 = Rabbits.Suits(defs, b, _station);
                if (sa != sb2) return sa ? -1 : 1;
                double wa = Rabbits.MultiplierAt(defs, s, a, _station), wb = Rabbits.MultiplierAt(defs, s, b, _station);
                return wa != wb ? wb.CompareTo(wa) : a.Id.CompareTo(b.Id);
            });
            foreach (Rabbit r in rabbits) AddRow(defs, s, r);
        }

        void AddRow(IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            Color tier = Palette.Tiers[r.Tier];
            bool here = r.Station == _station, suits = Rabbits.Suits(defs, r, _station);
            var root = UiKit.Rect("Rabbit " + r.Id, _content);
            root.AddImage(_catalog.Panel, Palette.Mix(tier, Palette.Ink, 0.25f), true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0)
                .AddImage(_catalog.Panel, here ? Palette.Mix(Palette.Moss, Palette.Paper, 0.45f) : (suits ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.55f) : Palette.Card), true);

            var tile = UiKit.Rect("Tile", root).Place(0f, 0.5f, 86, 4, 112, 112);
            tile.AddImage(_catalog.Round, Palette.Mix(tier, Palette.Paper, 0.3f));
            UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 10, 8, 10, 8).AddImage(_catalog.RabbitArt(r), Color.white).preserveAspect = true;
            UiKit.Stars(tile, r.Tier, 0.5f, 0f, 0, -4, 14);

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.5f, 1, 1, 164, 0, 290, 22)
                .AddText(r.Name + " · " + L10n.Kind(r), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 18;
            name.resizeTextMaxSize = UiKit.TextTitle;

            string where = here ? L10n.T("worker.here")
                : (r.Station >= 0 && r.Station < defs.Count ? L10n.T("detail.working", L10n.Station(defs[r.Station].Id)) : L10n.T("worker.resting"));
            string worth = L10n.Trade(Rabbits.TradeOf(r)) + (suits ? L10n.T("rabbits.intrade", UiKit.Times(Rabbits.TradeBonus)) : "");
            Text sub = UiKit.Rect("Sub", root).Stretch(0, 0.12f, 1, 0.5f, 164, 0, 290, 4)
                .AddText(worth + " · " + where, UiKit.TextBody, suits ? Palette.Mix(Palette.Moss, Palette.Ink, 0.45f) : Palette.InkSoft, TextAnchor.UpperLeft, suits ? FontStyle.Bold : FontStyle.Normal);
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 14;
            sub.resizeTextMaxSize = UiKit.TextBody;

            long id = r.Id;
            int station = _station;
            var button = UiKit.Rect("Put", root).Place(1f, 0.5f, -140, 4, 230, 100);
            ChunkyButton chunky = UiKit.Chunky(button, _catalog.Panel, () =>
            {
                _onAssign(id, station);
                Close();
            });
            chunky.SetEnabled(!here);
            UiKit.Rect("Text", chunky.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(here ? L10n.T("worker.here") : L10n.T("worker.put"), UiKit.TextBody, ChunkyButton.InkFor(!here), TextAnchor.MiddleCenter, FontStyle.Bold);
            _rows.Add(root.gameObject);
        }
    }
}

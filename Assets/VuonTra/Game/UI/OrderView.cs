using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class OrderView
    {
        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        public const float Height = 940f;
        const int LineRows = Orders.MaxLines;

        readonly GameCatalog _catalog;
        readonly Text _guestName, _left, _reward, _deliverLabel;
        readonly RectTransform _portraitHolder;
        readonly RectTransform[] _lines = new RectTransform[LineRows];
        readonly Image[] _lineIcons = new Image[LineRows], _lineMarks = new Image[LineRows];
        readonly Text[] _lineNames = new Text[LineRows], _lineHave = new Text[LineRows];
        readonly ChunkyButton _deliver;
        public RectTransform DeliverRect => _deliver.Root;
        GameObject _portrait;
        string _shownGuest;

        public OrderView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Func<int, Sprite> productSprite, Action onDeliver)
        {
            _catalog = catalog;
            Sheet = new BottomSheet(canvas, "Order", catalog, tweens, Height, null);
            RectTransform card = Sheet.Card;
            UiKit.Rect("Title", card).Stretch(0, 1, 1, 1, 40, -120, 40, 40)
                .AddText(L10n.T("order.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var guest = UiKit.Rect("Guest", card).Stretch(0, 1, 1, 1, 40, -330, 40, 130);
            guest.AddImage(catalog.Panel, Palette.Card, true);
            _portraitHolder = UiKit.Rect("PortraitHolder", guest).Place(0f, 0.5f, 100, 0, 160, 160);
            _guestName = UiKit.Rect("Name", guest).Stretch(0, 0.5f, 1, 1, 200, 0, 30, 24).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _guestName.resizeTextForBestFit = true;
            _guestName.resizeTextMinSize = 15;
            _guestName.resizeTextMaxSize = UiKit.TextBody;
            _left = UiKit.Rect("Left", guest).Stretch(0, 0, 1, 0.5f, 200, 24, 30, 0).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);

            UiKit.Rect("Wants", card).Stretch(0, 1, 1, 1, 40, -400, 40, 350).AddText(L10n.T("order.wants"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);
            for (int i = 0; i < LineRows; i++)
            {
                float top = 410 + i * 120;
                var row = UiKit.Rect("Line " + i, card).Stretch(0, 1, 1, 1, 40, -(top + 108), 40, top);
                row.AddImage(catalog.Panel, Palette.Card, true);
                _lines[i] = row;
                UiKit.Rect("Plate", row).Place(0f, 0.5f, 64, 0, 84, 84).AddImage(catalog.Round, Palette.Track);
                _lineIcons[i] = UiKit.Rect("Icon", row).Place(0f, 0.5f, 64, 0, 66, 66).AddImage(null, Color.white);
                _lineIcons[i].preserveAspect = true;
                _lineNames[i] = UiKit.Rect("Name", row).Stretch(0, 0, 1, 1, 124, 0, 240, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                _lineNames[i].resizeTextForBestFit = true;
                _lineNames[i].resizeTextMinSize = 14;
                _lineNames[i].resizeTextMaxSize = UiKit.TextBody;
                _lineHave[i] = UiKit.Rect("Have", row).Place(1f, 0.5f, -130, 0, 150, 60).AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
                _lineMarks[i] = UiKit.Rect("Met", row).Place(1f, 0.5f, -36, 0, 40, 40).AddImage(catalog.Round, Palette.Moss);
                UiKit.Rect("Text", _lineMarks[i].rectTransform).Stretch(0, 0, 1, 1).AddText("✓", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            _reward = UiKit.Rect("Reward", card).Stretch(0, 1, 1, 1, 40, -720, 40, 660).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _reward.resizeTextForBestFit = true;
            _reward.resizeTextMinSize = 14;
            _reward.resizeTextMaxSize = UiKit.TextBody;

            var deliverButton = UiKit.Rect("Deliver", card).Place(0.5f, 1f, 0, -800, 460, 110);
            _deliver = UiKit.Chunky(deliverButton, catalog.Panel, () => { Sfx.Blip(); onDeliver(); });
            _deliverLabel = UiKit.Rect("Text", _deliver.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _deliverLabel.resizeTextForBestFit = true;
            _deliverLabel.resizeTextMinSize = 16;
            _deliverLabel.resizeTextMaxSize = UiKit.TextTitle;
            _productSprite = productSprite;
        }

        readonly Func<int, Sprite> _productSprite;

        public void Open(GameState s, Modifiers m, System.Collections.Generic.IReadOnlyList<StationDef> defs)
        {
            Sheet.Open();
            Refresh(s, m, defs);
        }

        public void Close() => Sheet.Close();

        public static string Clock(double seconds)
        {
            int whole = Mathf.Max(0, Mathf.CeilToInt((float)seconds));
            return (whole / 60) + ":" + (whole % 60).ToString("00");
        }

        public void Refresh(GameState s, Modifiers m, System.Collections.Generic.IReadOnlyList<StationDef> defs)
        {
            if (!IsOpen) return;
            Order o = s.Order;
            if (o == null)
            {
                _guestName.text = L10n.T("order.gone");
                _left.text = "";
                for (int i = 0; i < LineRows; i++) if (_lines[i].gameObject.activeSelf) _lines[i].gameObject.SetActive(false);
                _reward.text = "";
                _deliverLabel.text = L10n.T("order.deliver");
                _deliverLabel.color = ChunkyButton.InkFor(false);
                _deliver.SetEnabled(false);
                return;
            }
            if (_shownGuest != o.Guest || _portrait == null)
            {
                if (_portrait != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(_portrait); else UnityEngine.Object.DestroyImmediate(_portrait); }
                RabbitKind kind = RabbitKinds.Find(o.Guest);
                var shown = new Rabbit { Tier = kind != null ? kind.Tier : 0, Kind = o.Guest, Name = "" };
                _portrait = RabbitGridView.Portrait(_portraitHolder, _catalog, s, shown, 0, 0, 160).gameObject;
                _shownGuest = o.Guest;
            }
            _guestName.text = L10n.T("order.guest", L10n.T("kind." + o.Guest));
            _left.text = L10n.T("order.left", Clock(o.Left));

            int missing = 0;
            for (int i = 0; i < LineRows; i++)
            {
                bool has = i < o.Lines.Count;
                if (_lines[i].gameObject.activeSelf != has) _lines[i].gameObject.SetActive(has);
                if (!has) continue;
                OrderLine line = o.Lines[i];
                int have = Orders.Have(s, line.Station);
                bool met = have >= line.Count;
                if (!met) missing += line.Count - have;
                _lineIcons[i].sprite = _productSprite(line.Station);
                _lineIcons[i].enabled = _lineIcons[i].sprite != null;
                _lineNames[i].text = line.Station >= 0 && line.Station < defs.Count ? L10n.Station(defs[line.Station].Id) : "";
                _lineHave[i].text = Mathf.Min(have, line.Count) + "/" + line.Count;
                _lineHave[i].color = met ? Palette.Moss : Palette.Ink;
                if (_lineMarks[i].gameObject.activeSelf != met) _lineMarks[i].gameObject.SetActive(met);
            }
            string carrots = Orders.Reward(defs, s, m, o).Short();
            _reward.text = o.RewardClovers > 0 ? L10n.T("order.reward.clover", carrots) : L10n.T("order.reward", carrots);
            bool can = missing == 0;
            _deliverLabel.text = can ? L10n.T("order.deliver") : L10n.T("order.missing", missing);
            _deliverLabel.color = ChunkyButton.InkFor(can);
            _deliver.SetEnabled(can);
        }
    }
}

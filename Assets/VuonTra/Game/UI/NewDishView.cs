using System;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed class NewDishView
    {
        const float IconSize = 220f;

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Text _name, _sub, _more;
        readonly Action _onBook;
        readonly DishIcon _icon;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Card => _card;

        public NewDishView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onBook)
        {
            _catalog = catalog;
            _tweens = tweens;
            _onBook = onBook;

            var overlay = UiKit.Rect("NewDish", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.56f, 0, 0, 860, 760);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;

            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -70, 780, 70)
                .AddText(L10n.T("dish.new.title"), 46, Palette.Mix(Palette.Carrot, Palette.Ink, 0.35f), TextAnchor.MiddleCenter, FontStyle.Bold)
                .FitOneLine(780, 28);
            _icon = DishIcon.Build(_card, catalog, IconSize);
            _icon.Root.anchorMin = _icon.Root.anchorMax = new Vector2(0.5f, 1f);
            _icon.Root.anchoredPosition = new Vector2(0, -125 - IconSize * 0.5f);
            _name = UiKit.Rect("Name", _card).Place(0.5f, 1f, 0, -405, 780, 64)
                .AddText("", 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _name.FitOneLine(780, 22);
            _sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -462, 780, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            _sub.FitOneLine(780, 16);
            Text bonus = UiKit.Rect("Bonus", _card).Place(0.5f, 1f, 0, -514, 780, 44)
                .AddText(L10n.T("dish.new.bonus"), UiKit.TextSmall, Palette.MossInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            bonus.FitOneLine(780, 14);
            _more = UiKit.Rect("More", _card).Place(0.5f, 1f, 0, -560, 780, 40)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _more.FitOneLine(780, 14);

            Button(_card, "Book", -200, false, L10n.T("dish.new.book"), () =>
            {
                Close();
                _onBook();
            });
            Button(_card, "Ok", 200, true, L10n.T("dish.new.ok"), Close);

            _root.SetActive(false);
        }

        void Button(RectTransform parent, string name, float x, bool main, string label, Action onPress)
        {
            var rt = UiKit.Rect(name, parent).Place(0.5f, 0f, x, 100, 340, 120);
            ChunkyButton button = null;
            button = UiKit.Chunky(rt, _catalog.Panel, () =>
            {
                button.Press(_tweens);
                Sfx.Click();
                onPress();
            });
            if (!main)
            {
                button.Lip.color = Palette.OffLip;
                button.Face.color = Palette.Track;
            }
            UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 12, 0, 12, 0)
                .AddText(label, UiKit.TextTitle, main ? Palette.OnCarrot : Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .FitOneLine(316, 20);
        }

        public void Open(StationData data, int dish, int more)
        {
            _icon.Show(data, dish);
            _name.text = L10n.Dish(data.Id, dish);
            _sub.text = L10n.T("dish.new.sub", L10n.Station(data.Id));
            _more.text = more > 0 ? L10n.T("dish.new.more", more) : "";
            _more.gameObject.SetActive(more > 0);
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(Motion.Panel), t => { if (_card != null) _card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);
    }
}

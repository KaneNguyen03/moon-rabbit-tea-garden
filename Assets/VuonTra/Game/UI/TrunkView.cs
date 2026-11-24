using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class TrunkView
    {
        public const float CardWidth = 1010f, CardHeight = 1220f;
        public const float TileWidth = 450f, TileHeight = 440f, TileGap = 24f;
        public const float ConfirmSeconds = ShopView.ConfirmSeconds;

        sealed class Tile
        {
            public RectTransform Root, IconHolder;
            public Image Plate;
            public Text Name, Sub, Label, Was;
            public GameObject Old, Sale, WasLine, Carrot;
            public ChunkyButton Buy;
            public GameObject Icon;
            public string Code;
        }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action<int> _onBuy;
        readonly GameObject _root, _grid, _closed;
        readonly RectTransform _card;
        readonly Text _sub, _have, _closedSub, _note;
        readonly Tile[] _tiles = new Tile[Trunk.Shown];
        TrunkShelf _shelf;
        int _armed = -1;
        float _armedAt;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Card => _card;
        public bool ShowsGoods => _grid.activeSelf;
        public string Sub => _sub.text;
        public string Note => _note.text;
        public TrunkShelf Shelf => _shelf;
        public string LabelOf(int slot) => _tiles[slot].Label.text;
        public string NameOf(int slot) => _tiles[slot].Name.text;
        public bool OldShown(int slot) => _tiles[slot].Old.activeSelf;
        public bool SaleShown(int slot) => _tiles[slot].Sale.activeSelf;
        public bool TileShown(int slot) => _tiles[slot].Root.gameObject.activeSelf;

        public TrunkView(RectTransform canvas, GameRunner runner, Tweens tweens, Action<int> onBuy)
        {
            _runner = runner;
            _catalog = runner.Catalog;
            _tweens = tweens;
            _onBuy = onBuy;
            GameCatalog catalog = _catalog;

            var overlay = UiKit.Rect("TrunkCard", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, CardWidth, CardHeight);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Lantern, 0.08f), true).raycastTarget = true;

            if (catalog.ShopProp != null) TrunkArt(UiKit.Rect("Art", _card).Place(0f, 1f, 110, -96, 120, 120), catalog);
            Fit(UiKit.Rect("Title", _card).Place(0.5f, 1f, 30, -80, 640, 70)
                .AddText(L10n.T("trunk.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 52, 28);
            _sub = Fit(UiKit.Rect("Sub", _card).Place(0.5f, 1f, 30, -142, 700, 46)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextBody, 14);

            var purse = UiKit.Rect("Have", _card).Place(0.5f, 1f, 0, -214, 900, 56);
            if (catalog.GoldenCarrot != null) UiKit.Rect("Icon", purse).Place(0f, 0.5f, 24, 0, 44, 44).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
            _have = Fit(UiKit.Rect("Text", purse).Stretch(0, 0, 0.5f, 1, 58, 0, 0, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextBody, 14);
            _note = Fit(UiKit.Rect("Note", purse).Stretch(0.5f, 0, 1, 1, 10, 0, 0, 0)
                .AddText("", UiKit.TextBody, Palette.Mix(Palette.Moss, Palette.Ink, 0.45f), TextAnchor.MiddleRight, FontStyle.Bold), UiKit.TextBody, 12);

            var grid = UiKit.Rect("Goods", _card).Stretch(0, 0, 1, 1, 0, 40, 0, 260);
            _grid = grid.gameObject;
            for (int i = 0; i < Trunk.Shown; i++) _tiles[i] = BuildTile(grid, i);

            var closed = UiKit.Rect("Closed", _card).Stretch(0, 0, 1, 1, 60, 120, 60, 260);
            _closed = closed.gameObject;
            if (catalog.ShopProp != null) TrunkArt(UiKit.Rect("Art", closed).Place(0.5f, 0.5f, 0, 170, 340, 340), catalog);
            Fit(UiKit.Rect("Title", closed).Place(0.5f, 0.5f, 0, -60, 860, 70)
                .AddText(L10n.T("trunk.closed.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 18);
            _closedSub = Fit(UiKit.Rect("Sub", closed).Place(0.5f, 0.5f, 0, -170, 860, 120)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextBody, 14);
            _closedSub.horizontalOverflow = HorizontalWrapMode.Wrap;

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _root.SetActive(false);
        }

        public static Image TrunkArt(RectTransform rt, GameCatalog catalog)
        {
            Image image = rt.AddImage(catalog.ShopProp, TrunkTint);
            image.preserveAspect = true;
            return image;
        }

        public static readonly Color TrunkTint = new Color(0.78f, 0.6f, 0.46f, 1f);

        static Text Fit(Text text, int size, int least)
        {
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(size);
            return text;
        }

        Tile BuildTile(RectTransform grid, int i)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (TileWidth + TileGap) / 2f;
            float y = -TileHeight / 2f - (i / 2) * (TileHeight + TileGap);
            var tile = new Tile { Root = UiKit.Rect("Item " + (i + 1), grid).Place(0.5f, 1f, x, y, TileWidth, TileHeight) };
            UiKit.Rect("Lip", tile.Root).Stretch(0, 0, 1, 1).AddImage(_catalog.Panel, Palette.CardLip, true);
            tile.Plate = UiKit.Rect("Face", tile.Root).Stretch(0, 0, 1, 1, 0, 6, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            tile.IconHolder = UiKit.Rect("IconHolder", tile.Root).Place(0.5f, 1f, 0, -120, 150, 150);
            tile.Name = Fit(UiKit.Rect("Name", tile.Root).Place(0.5f, 1f, 0, -228, TileWidth - 36f, 46)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);
            tile.Sub = Fit(UiKit.Rect("Sub", tile.Root).Place(0.5f, 1f, 0, -270, TileWidth - 36f, 38)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextSmall, 12);

            tile.Old = Badge(tile.Root, "Old", 0f, L10n.T("trunk.old"), Palette.Mix(Palette.Tiers[4], Palette.Ink, 0.45f));
            tile.Sale = Badge(tile.Root, "Sale", 1f, L10n.T("trunk.sale", Trunk.SalePercent), Palette.Carrot);

            int slot = i;
            var buy = UiKit.Rect("Buy", tile.Root).Place(0.5f, 0f, 0, 62, TileWidth - 80f, 100);
            tile.Buy = UiKit.Chunky(buy, _catalog.Panel, () => { tile.Buy.Press(_tweens); OnTap(slot); });
            var carrot = UiKit.Rect("Carrot", tile.Buy.FaceRect).Place(0f, 0.5f, 46, 0, 44, 44);
            if (_catalog.GoldenCarrot != null) carrot.AddImage(_catalog.GoldenCarrot, Color.white).preserveAspect = true;
            tile.Carrot = carrot.gameObject;
            tile.Label = Fit(UiKit.Rect("Text", tile.Buy.FaceRect).Stretch(0, 0, 1, 1, 76, 0, 20, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);
            var was = UiKit.Rect("Was", tile.Buy.FaceRect).Place(1f, 0.5f, -60, 0, 84, 40);
            tile.Was = Fit(was.AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);
            var line = UiKit.Rect("Line", was).Place(0.5f, 0.5f, 0, 0, 64, 4);
            line.AddImage(null, Palette.Ink);
            tile.WasLine = line.gameObject;
            return tile;
        }

        GameObject Badge(RectTransform parent, string name, float side, string text, Color color)
        {
            float width = (TileWidth - 36f) / 2f;
            var badge = UiKit.Rect(name, parent).Place(side, 1f, side < 0.5f ? 14f + width / 2f : -14f - width / 2f, -30, width, 40);
            badge.AddImage(_catalog.Panel, color, true);
            Fit(UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText(text, UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 10);
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        void OnTap(int slot)
        {
            bool confirmed = _armed == slot && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            _armed = confirmed ? -1 : slot;
            _armedAt = Time.realtimeSinceStartup;
            Sfx.Click();
            if (confirmed) _onBuy?.Invoke(slot);
            else Refresh();
        }

        public void ShowNote(string text) => _note.text = text;

        public void Open(TrunkShelf shelf)
        {
            _armed = -1;
            _note.text = "";
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Show(shelf);
            _tweens.Grow(_card, 0.85f, Motion.Time(0.25f), Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);

        public void Refresh()
        {
            if (!IsOpen) return;
            Show(_runner.TrunkShelf);
        }

        void Show(TrunkShelf shelf)
        {
            GameState s = _runner.State;
            _shelf = shelf;
            _have.text = L10n.T("shop.have", s.GoldenCarrots);
            _sub.text = shelf.Open ? L10n.T("trunk.open", shelf.CloseHour) : L10n.T("trunk.closed", shelf.NextHour);
            if (_grid.activeSelf != shelf.Open) _grid.SetActive(shelf.Open);
            if (_closed.activeSelf == shelf.Open) _closed.SetActive(!shelf.Open);
            _closedSub.text = L10n.T("trunk.closed", shelf.NextHour) + "\n" + L10n.T("trunk.closed.sub");
            for (int i = 0; i < _tiles.Length; i++)
            {
                bool on = shelf.Offers != null && i < shelf.Offers.Length;
                if (_tiles[i].Root.gameObject.activeSelf != on) _tiles[i].Root.gameObject.SetActive(on);
                if (on) ShowTile(_tiles[i], shelf.Offers[i], s);
            }
        }

        void ShowTile(Tile tile, TrunkOffer offer, GameState s)
        {
            if (tile.Code != offer.Code)
            {
                tile.Code = offer.Code;
                if (tile.Icon != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(tile.Icon); else UnityEngine.Object.DestroyImmediate(tile.Icon); }
                tile.Icon = BuildIcon(tile.IconHolder, offer);
            }
            tile.Name.text = NameOf(offer);
            tile.Sub.text = SubOf(offer);
            if (tile.Old.activeSelf != offer.OldSeason) tile.Old.SetActive(offer.OldSeason);
            if (tile.Sale.activeSelf != offer.Sale) tile.Sale.SetActive(offer.Sale);
            bool affordable = s.GoldenCarrots >= offer.Price;
            bool can = offer.CanBuy && affordable;
            bool armed = can && _armed == offer.Slot && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            bool showWas = offer.Sale && offer.CanBuy && !armed;
            tile.Was.text = showWas ? offer.FullPrice.ToString() : "";
            if (tile.WasLine.activeSelf != showWas) tile.WasLine.SetActive(showWas);
            if (tile.Carrot.activeSelf != offer.CanBuy) tile.Carrot.SetActive(offer.CanBuy);
            tile.Label.rectTransform.offsetMin = new Vector2(offer.CanBuy ? 76f : 20f, 0f);
            tile.Label.rectTransform.offsetMax = new Vector2(showWas ? -110f : -20f, 0f);
            tile.Label.text = offer.Bought ? L10n.T("trunk.bought") : offer.Owned ? L10n.T("trunk.owned") : armed ? L10n.T("shop.hats.confirm") : offer.Price.ToString();
            tile.Label.color = ChunkyButton.InkFor(can);
            tile.Buy.SetEnabled(can);
            tile.Plate.color = offer.Bought || offer.Owned ? Palette.Mix(Palette.Paper, Palette.Moss, 0.25f)
                : armed ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : offer.Sale ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.22f) : Palette.Card;
        }

        GameObject BuildIcon(RectTransform holder, TrunkOffer offer)
        {
            var rt = UiKit.Rect("Icon", holder).Stretch(0, 0, 1, 1);
            switch (offer.Kind)
            {
                case TrunkKind.SeasonRabbit:
                    UiKit.Rect("Halo", rt).Stretch(0, 0, 1, 1, -14, -14, -14, -14).AddImage(_catalog.Glow, Palette.Alpha(Palette.Tiers[4], 0.5f));
                    UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(_catalog.KindSprite(offer.Id), Color.white).preserveAspect = true;
                    break;
                case TrunkKind.Hat:
                    rt.AddImage(_catalog.ItemIcon(offer.Id), Color.white).preserveAspect = true;
                    break;
                case TrunkKind.Jars:
                    IncomeView.JarIcon(rt, _catalog, "Jar", 130f, 0.5f, 0.5f, 0, 0);
                    break;
                case TrunkKind.Clovers:
                    rt.AddImage(_catalog.Clover, Color.white).preserveAspect = true;
                    break;
                case TrunkKind.Lantern:
                    UiKit.Rect("Halo", rt).Stretch(0, 0, 1, 1, -10, -10, -10, -10).AddImage(_catalog.Glow, Palette.Alpha(Palette.Hex(LanternHex(offer)), 0.6f));
                    UiKit.Rect("Rim", rt).Place(0.5f, 0.5f, 0, 0, 112, 112).AddImage(_catalog.Round, Palette.Mix(Palette.Paper, Palette.Ink, 0.35f));
                    UiKit.Rect("Fill", rt).Place(0.5f, 0.5f, 0, 0, 104, 104).AddImage(_catalog.Round, Palette.Hex(LanternHex(offer)));
                    break;
                default:
                    UiKit.Rect("Carrot", rt).Place(0.5f, 0.5f, -22, 6, 120, 120).AddImage(_catalog.GoldenCarrot, Color.white).preserveAspect = true;
                    UiKit.Rect("Times", rt).Place(1f, 0f, -24, 24, 80, 56).AddText("×" + offer.Amount, UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    break;
            }
            return rt.gameObject;
        }

        static int LanternMonth(TrunkOffer offer)
        {
            int colon = offer.Id.LastIndexOf(':');
            return colon >= 0 && int.TryParse(offer.Id.Substring(colon + 1), out int cal) ? cal : 0;
        }

        static string LanternHex(TrunkOffer offer) => MonthPass.LanternColors[Mathf.Clamp(LanternMonth(offer), 0, MonthPass.Months - 1)];

        public static string NameOf(TrunkOffer offer)
        {
            switch (offer.Kind)
            {
                case TrunkKind.SeasonRabbit: return L10n.T("kind." + offer.Id);
                case TrunkKind.Hat: return L10n.T("item." + offer.Id);
                case TrunkKind.Jars: return L10n.T("trunk.jars", offer.Amount);
                case TrunkKind.Clovers: return L10n.T("trunk.clovers", offer.Amount);
                case TrunkKind.Lantern: return L10n.T("monthpass.lantern", MonthPassView.MonthName(LanternMonth(offer)));
                default: return L10n.T("trunk.golden", offer.Amount);
            }
        }

        static string SubOf(TrunkOffer offer)
        {
            switch (offer.Kind)
            {
                case TrunkKind.SeasonRabbit:
                    RabbitKind kind = RabbitKinds.Find(offer.Id);
                    return kind != null ? L10n.T("trunk.season", L10n.Season(kind.Season)) : "";
                case TrunkKind.Hat: return L10n.T("trunk.hat.sub");
                case TrunkKind.Jars: return L10n.T("trunk.pack.sub");
                case TrunkKind.Lantern: return L10n.T("trunk.lantern.sub");
                case TrunkKind.GoldenCall: return L10n.T("trunk.golden.sub", offer.Amount);
                default: return "";
            }
        }
    }
}

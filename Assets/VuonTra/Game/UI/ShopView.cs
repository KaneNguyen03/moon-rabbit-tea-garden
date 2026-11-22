using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The tea shop's sheet: today's gift with its streak, and the golden call with its odds beside the
    /// clover call's. Everything it shows is read off the state each refresh.
    /// </summary>
    public sealed class ShopView
    {
        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;

        readonly GameCatalog _catalog;
        readonly Text _have, _giftTitle, _giftSub, _giftLabel, _goldenLabel, _odds, _visitorName, _visitorSub, _visitorLabel, _visitorNew;
        readonly ChunkyButton _gift, _golden, _buy;
        readonly RectTransform _visitorPortrait;
        readonly Func<long> _today;
        readonly Func<double> _hoursLeft;
        GameObject _portrait;

        /// <summary>One hat on sale: its tile and the parts that change.</summary>
        sealed class HatTile { public ItemDef Def; public Button Button; public Image Plate, PriceChip; public Text Price; public GameObject Owned; }
        readonly List<HatTile> _hats = new List<HatTile>();
        readonly Action<string> _onBuyItem;
        /// <summary>The hat whose tile was tapped once, waiting for the second tap that buys, and when.</summary>
        string _armed;
        float _armedAt;
        /// <summary>A tap that asks is forgotten after this long.</summary>
        public const float ConfirmSeconds = 2f;
        GameState _last;

        /// <summary>One decoration on the shelf (#11): its tile, the price chip that doubles as the ask, and the "owned" badge.</summary>
        sealed class DecorTile { public DecorKind Kind; public Button Button; public Image Plate, PriceChip; public Text Price, OwnedText; public GameObject Owned; }
        readonly List<DecorTile> _decor = new List<DecorTile>();
        readonly Action<string> _onBuyDecor;
        readonly Action<int> _onLanternColor;
        /// <summary>The lantern colour dots: each a button, each with a ring that shows when its colour is hung.</summary>
        readonly List<Button> _lanternDots = new List<Button>();
        readonly List<GameObject> _lanternRings = new List<GameObject>();
        /// <summary>How tall the sheet stands: everything down to the note on where golden carrots come from, under the HUD.</summary>
        public const float Height = 1770f;

        public ShopView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Func<long> today, Func<double> hoursLeft, Action onGift, Action onGoldenSummon, Action onBuyVisitor,
            IReadOnlyList<ItemDef> items = null, Action<string> onBuyItem = null, Action<string> onBuyDecor = null, Action<int> onLanternColor = null, Action onBuyMore = null)
        {
            _catalog = catalog;
            _today = today;
            _hoursLeft = hoursLeft;
            _onBuyItem = onBuyItem;
            _onBuyDecor = onBuyDecor;
            _onLanternColor = onLanternColor;
            Sheet = new BottomSheet(canvas, "Shop", catalog, tweens, Height, null);
            RectTransform card = Sheet.Card;
            UiKit.Rect("Title", card).Stretch(0, 1, 1, 1, 40, -120, 40, 40)
                .AddText(L10n.T("shop.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var purse = UiKit.Rect("Have", card).Stretch(0, 1, 1, 1, 40, -170, 40, 120);
            if (catalog.GoldenCarrot != null) UiKit.Rect("Icon", purse).Place(0f, 0.5f, 22, 0, 40, 40).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
            _have = UiKit.Rect("Text", purse).Stretch(0, 0, 1, 1, 56, 0, 300, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            // Golden carrots for money (#9): a small warm button at the end of the purse row opens the packs card.
            var buyMore = UiKit.Rect("BuyMore", purse).Place(1f, 0.5f, -130, 0, 260, UiKit.MinTouch);
            ChunkyButton buyMoreButton = null;
            buyMoreButton = UiKit.Chunky(buyMore, catalog.Panel, () => { buyMoreButton.Press(tweens); Sfx.Click(); onBuyMore?.Invoke(); });
            buyMoreButton.Face.color = Palette.Lantern;
            buyMoreButton.Lip.color = Palette.Mix(Palette.Lantern, Palette.Ink, 0.3f);
            Text buyMoreText = UiKit.Rect("Text", buyMoreButton.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText(L10n.T("shop.buymore"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            buyMoreText.resizeTextForBestFit = true;
            buyMoreText.resizeTextMinSize = 14;
            buyMoreText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            // Today: the gift, its streak, and the button that takes it.
            var today_ = UiKit.Rect("Today", card).Stretch(0, 1, 1, 1, 40, -420, 40, 190);
            today_.AddImage(catalog.Panel, Palette.Card, true);
            if (catalog.GiftBox != null) UiKit.Rect("Icon", today_).Place(0f, 0.5f, 90, 0, 120, 120).AddImage(catalog.GiftBox, Color.white).preserveAspect = true;
            _giftTitle = UiKit.Rect("Heading", today_).Stretch(0, 0.5f, 1, 1, 170, 0, 320, 24).AddText(L10n.T("shop.today"), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _giftSub = UiKit.Rect("Sub", today_).Stretch(0, 0, 1, 0.5f, 170, 24, 320, 0).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            _giftSub.resizeTextForBestFit = true;
            _giftSub.resizeTextMinSize = 14;
            _giftSub.resizeTextMaxSize = UiKit.TextSmall;
            var giftButton = UiKit.Rect("Gift", today_).Place(1f, 0.5f, -150, 0, 260, 100);
            _gift = UiKit.Chunky(giftButton, catalog.Panel, () => { Sfx.Blip(); onGift(); });
            _giftLabel = UiKit.Rect("Text", _gift.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _giftLabel.resizeTextForBestFit = true;
            _giftLabel.resizeTextMinSize = 14;
            _giftLabel.resizeTextMaxSize = UiKit.TextBody;

            // Today's visitor: a rabbit of Blue or rarer, here until midnight, for golden carrots.
            var visitor = UiKit.Rect("Visitor", card).Stretch(0, 1, 1, 1, 40, -700, 40, 450);
            visitor.AddImage(catalog.Panel, Palette.Card, true);
            _visitorPortrait = UiKit.Rect("PortraitHolder", visitor).Place(0f, 0.5f, 110, 0, 200, 200);
            UiKit.Rect("Heading", visitor).Stretch(0, 0.66f, 1, 1, 230, 0, 320, 20).AddText(L10n.T("shop.visitor.title"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.LowerLeft, FontStyle.Bold);
            _visitorName = UiKit.Rect("Name", visitor).Stretch(0, 0.36f, 1, 0.68f, 230, 0, 320, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _visitorName.resizeTextForBestFit = true;
            _visitorName.resizeTextMinSize = 15;
            _visitorName.resizeTextMaxSize = UiKit.TextBody;
            _visitorSub = UiKit.Rect("Sub", visitor).Stretch(0, 0.08f, 1, 0.38f, 230, 0, 320, 0).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            _visitorSub.resizeTextForBestFit = true;
            _visitorSub.resizeTextMinSize = 13;
            _visitorSub.resizeTextMaxSize = UiKit.TextSmall;
            _visitorNew = UiKit.Rect("New", visitor).Place(0f, 1f, 110, -22, 190 * UiKit.TextScale, 36 * UiKit.TextScale).AddText(L10n.T("shop.visitor.new"), 18, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _visitorNew.resizeTextForBestFit = true;
            _visitorNew.resizeTextMinSize = 14;
            _visitorNew.resizeTextMaxSize = UiKit.Scaled(18);
            var newPlate = UiKit.Rect("Plate", _visitorNew.rectTransform).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Tiers[5], true);
            newPlate.transform.SetAsFirstSibling();
            var buyButton = UiKit.Rect("Buy", visitor).Place(1f, 0.5f, -150, 0, 260, 100);
            _buy = UiKit.Chunky(buyButton, catalog.Panel, () => { Sfx.Blip(); onBuyVisitor(); });
            _visitorLabel = UiKit.Rect("Text", _buy.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _visitorLabel.resizeTextForBestFit = true;
            _visitorLabel.resizeTextMinSize = 14;
            _visitorLabel.resizeTextMaxSize = UiKit.TextBody;

            // The golden call.
            var golden = UiKit.Rect("Golden", card).Stretch(0, 1, 1, 1, 40, -980, 40, 730);
            golden.AddImage(catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.6f), true);
            if (catalog.GoldenCarrot != null) UiKit.Rect("Icon", golden).Place(0f, 0.5f, 90, 10, 110, 110).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
            UiKit.Rect("Heading", golden).Stretch(0, 0.55f, 1, 1, 170, 0, 320, 24).AddText(L10n.T("shop.golden.title"), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            Text sub = UiKit.Rect("Sub", golden).Stretch(0, 0.2f, 1, 0.55f, 170, 0, 320, 4).AddText(L10n.T("shop.golden.sub"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 13;
            sub.resizeTextMaxSize = UiKit.TextSmall;
            var goldenButton = UiKit.Rect("Call", golden).Place(1f, 0.5f, -150, 10, 260, 100);
            _golden = UiKit.Chunky(goldenButton, catalog.Panel, () => { Sfx.Blip(); onGoldenSummon(); });
            _goldenLabel = UiKit.Rect("Text", _golden.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _goldenLabel.resizeTextForBestFit = true;
            _goldenLabel.resizeTextMinSize = 14;
            _goldenLabel.resizeTextMaxSize = UiKit.TextBody;
            // Both calls' odds, side by side, as the design promised.
            _odds = UiKit.Rect("Odds", golden).Stretch(0, 0, 1, 0.2f, 24, 10, 24, 0).AddText(OddsText(), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleLeft);
            _odds.resizeTextForBestFit = true;
            _odds.resizeTextMinSize = 12;
            _odds.resizeTextMaxSize = UiKit.TextSmall;

            // Things for rabbits (#44 round 3): six hats for looks, one tile each; a tap asks, a second tap within two seconds buys.
            var hats = UiKit.Rect("Hats", card).Stretch(0, 1, 1, 1, 40, -1320, 40, 1000);
            hats.AddImage(catalog.Panel, Palette.Card, true);
            UiKit.Rect("Heading", hats).Stretch(0, 1, 1, 1, 24, -56, 24, 8).AddText(L10n.T("shop.hats.title"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text hatsSub = UiKit.Rect("Sub", hats).Stretch(0, 1, 1, 1, 24, -100, 24, 54).AddText(L10n.T("shop.hats.sub"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            hatsSub.verticalOverflow = VerticalWrapMode.Truncate; // best fit only shrinks for height when overflow is not allowed
            hatsSub.resizeTextForBestFit = true;
            hatsSub.resizeTextMinSize = 12;
            hatsSub.resizeTextMaxSize = UiKit.TextSmall;
            var hatRow = UiKit.Rect("Row", hats).Stretch(0, 0, 1, 1, 0, 8, 0, 102);
            const float tileW = 150f, tileH = 210f, gap = 14f;
            int n = 0;
            if (items != null)
                foreach (ItemDef def in items)
                {
                    if (def.Price <= 0) continue;
                    var tile = new HatTile { Def = def };
                    var rt = UiKit.Rect("Hat " + def.Id, hatRow).Place(0f, 0.5f, 15f + n * (tileW + gap) + tileW / 2f, 0, tileW, tileH);
                    tile.Plate = rt.AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Color.white, 0.35f), true);
                    tile.Plate.raycastTarget = true;
                    tile.Button = rt.gameObject.AddComponent<Button>();
                    tile.Button.transition = Selectable.Transition.None;
                    tile.Button.targetGraphic = tile.Plate;
                    string defId = def.Id;
                    tile.Button.onClick.AddListener(() => OnHatTap(defId));
                    Image icon = UiKit.Rect("Icon", rt).Place(0.5f, 1f, 0, -64, 108, 108).AddImage(catalog.ItemIcon(def.Id), Color.white);
                    icon.preserveAspect = true;
                    Text name = UiKit.Rect("Name", rt).Place(0.5f, 1f, 0, -140, tileW - 12f, 44).AddText(L10n.T("item." + def.Id), 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    name.resizeTextForBestFit = true;
                    name.resizeTextMinSize = 11;
                    name.resizeTextMaxSize = UiKit.Scaled(18);
                    name.horizontalOverflow = HorizontalWrapMode.Wrap;
                    var price = UiKit.Rect("Price", rt).Place(0.5f, 0f, 0, 30, tileW - 24f, 40);
                    tile.PriceChip = price.AddImage(catalog.Panel, Palette.Lantern, true);
                    float priceInset = 8f;
                    if (catalog.GoldenCarrot != null)
                    {
                        UiKit.Rect("Carrot", price).Place(0f, 0.5f, 20, 0, 26, 26).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
                        priceInset = 34f;
                    }
                    tile.Price = UiKit.Rect("Text", price).Stretch(0, 0, 1, 1, priceInset, 0, 6, 0).AddText(def.Price.ToString(), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    tile.Price.resizeTextForBestFit = true;
                    tile.Price.resizeTextMinSize = 11;
                    tile.Price.resizeTextMaxSize = UiKit.TextSmall;
                    var owned = UiKit.Rect("Owned", rt).Place(0.5f, 0f, 0, 30, tileW - 24f, 40);
                    owned.AddImage(catalog.Panel, Palette.Moss, true);
                    Text ownedText = UiKit.Rect("Text", owned).Stretch(0, 0, 1, 1, 6, 0, 6, 0).AddText(L10n.T("shop.hats.owned"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    ownedText.resizeTextForBestFit = true;
                    ownedText.resizeTextMinSize = 11;
                    ownedText.resizeTextMaxSize = UiKit.TextSmall;
                    tile.Owned = owned.gameObject;
                    tile.Owned.SetActive(false);
                    _hats.Add(tile);
                    n++;
                }

            // Decorating the garden (#11): four things to stand in the garden, bought as often as there is room, and the
            // lantern colour as a row of dots beside the heading. The same two-tap buy as the hats.
            var decorPanel = UiKit.Rect("Decor", card).Stretch(0, 1, 1, 1, 40, -1620, 40, 1320);
            decorPanel.AddImage(catalog.Panel, Palette.Card, true);
            Text decorHeading = UiKit.Rect("Heading", decorPanel).Stretch(0, 1, 0, 1, 24, -72, -326, 8).AddText(L10n.T("shop.decor.title"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            decorHeading.resizeTextForBestFit = true;
            decorHeading.resizeTextMinSize = 15;
            decorHeading.resizeTextMaxSize = UiKit.TextBody;
            Text lanternLabel = UiKit.Rect("LanternLabel", decorPanel).Place(1f, 1f, -24 - 352 - 12 - 95, -40, 190, 56).AddText(L10n.T("shop.lantern.title"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold);
            lanternLabel.resizeTextForBestFit = true;
            lanternLabel.resizeTextMinSize = 12;
            lanternLabel.resizeTextMaxSize = UiKit.TextSmall;
            var lanterns = UiKit.Rect("Lanterns", decorPanel).Place(1f, 1f, -24 - 176, -40, 352, 64);
            for (int i = 0; i < Decor.LanternColors.Length; i++)
            {
                int color = i;
                var dot = UiKit.Rect("Dot " + i, lanterns).Place(0f, 0.5f, 44 + i * 88, 0, 64, 64);
                Image hit = dot.AddImage(null, new Color(0, 0, 0, 0));
                hit.raycastTarget = true;
                var dotButton = dot.gameObject.AddComponent<Button>();
                dotButton.transition = Selectable.Transition.None;
                dotButton.targetGraphic = hit;
                dotButton.onClick.AddListener(() => { Sfx.Click(); if (_onLanternColor != null) _onLanternColor(color); });
                UiKit.Touchable(dot, 64, 64);
                var ring = UiKit.Rect("Ring", dot).Stretch(0, 0, 1, 1, -7, -7, -7, -7);
                ring.AddImage(catalog.Round, Palette.Ink);
                // A thin rim under every dot, so the paper one still reads on the cream panel.
                UiKit.Rect("Rim", dot).Stretch(0, 0, 1, 1, -2, -2, -2, -2).AddImage(catalog.Round, Palette.Mix(Palette.Paper, Palette.Ink, 0.35f));
                UiKit.Rect("Fill", dot).Stretch(0, 0, 1, 1).AddImage(catalog.Round, Palette.Hex(Decor.LanternColors[i]));
                _lanternDots.Add(dotButton);
                _lanternRings.Add(ring.gameObject);
            }
            var decorRow = UiKit.Rect("Row", decorPanel).Stretch(0, 0, 1, 1, 0, 10, 0, 80);
            for (int i = 0; i < Decor.Kinds.Length; i++)
            {
                DecorKind kind = Decor.Kinds[i];
                var tile = new DecorTile { Kind = kind };
                var rt = UiKit.Rect("Decor " + kind.Id, decorRow).Place(0f, 0.5f, 15f + i * (tileW + gap) + tileW / 2f, 0, tileW, tileH);
                tile.Plate = rt.AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Color.white, 0.35f), true);
                tile.Plate.raycastTarget = true;
                tile.Button = rt.gameObject.AddComponent<Button>();
                tile.Button.transition = Selectable.Transition.None;
                tile.Button.targetGraphic = tile.Plate;
                string kindId = kind.Id;
                tile.Button.onClick.AddListener(() => OnDecorTap(kindId));
                Image icon = UiKit.Rect("Icon", rt).Place(0.5f, 1f, 0, -64, 112, 108).AddImage(catalog.DecorArt(kind.Id), Color.white);
                icon.preserveAspect = true;
                Text name = UiKit.Rect("Name", rt).Place(0.5f, 1f, 0, -140, tileW - 12f, 44).AddText(L10n.T("decor." + kind.Id), 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 11;
                name.resizeTextMaxSize = UiKit.Scaled(18);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                var price = UiKit.Rect("Price", rt).Place(0.5f, 0f, 0, 30, tileW - 24f, 40);
                tile.PriceChip = price.AddImage(catalog.Panel, Palette.Lantern, true);
                float priceInset = 8f;
                if (catalog.GoldenCarrot != null)
                {
                    UiKit.Rect("Carrot", price).Place(0f, 0.5f, 20, 0, 26, 26).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
                    priceInset = 34f;
                }
                tile.Price = UiKit.Rect("Text", price).Stretch(0, 0, 1, 1, priceInset, 0, 6, 0).AddText(kind.Price.ToString(), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                tile.Price.resizeTextForBestFit = true;
                tile.Price.resizeTextMinSize = 11;
                tile.Price.resizeTextMaxSize = UiKit.TextSmall;
                // How many are owned already, as a small moss badge over the top corner: the price stays, since more can be bought.
                var owned = UiKit.Rect("Owned", rt).Place(0f, 1f, 50, -22, 96, 32);
                owned.AddImage(catalog.Panel, Palette.Moss, true);
                tile.OwnedText = UiKit.Rect("Text", owned).Stretch(0, 0, 1, 1, 4, 0, 4, 0).AddText("", 16, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                tile.OwnedText.resizeTextForBestFit = true;
                tile.OwnedText.resizeTextMinSize = 10;
                tile.OwnedText.resizeTextMaxSize = UiKit.Scaled(16);
                tile.Owned = owned.gameObject;
                tile.Owned.SetActive(false);
                _decor.Add(tile);
            }

            UiKit.Rect("Earn", card).Stretch(0, 1, 1, 1, 40, -1700, 40, 1620).AddText(L10n.T("shop.earn"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
        }

        /// <summary>A hat tile tapped: the first tap asks, a second on the same tile soon after buys.</summary>
        void OnHatTap(string defId)
        {
            bool confirmed = _armed == defId && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            _armed = confirmed ? null : defId;
            _armedAt = Time.realtimeSinceStartup;
            Sfx.Click();
            if (confirmed) { if (_onBuyItem != null) _onBuyItem(defId); }
            else if (_last != null) RefreshHats(_last);
        }

        /// <summary>A decoration tile tapped: the same two taps as a hat. The hats and the shelf share the one asking tile.</summary>
        void OnDecorTap(string kindId)
        {
            string key = "decor:" + kindId;
            bool confirmed = _armed == key && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            _armed = confirmed ? null : key;
            _armedAt = Time.realtimeSinceStartup;
            Sfx.Click();
            if (confirmed) { if (_onBuyDecor != null) _onBuyDecor(kindId); }
            else if (_last != null) RefreshDecor(_last);
        }

        void RefreshDecor(GameState s)
        {
            _last = s;
            bool full = Decor.TotalOwned(s) >= Decor.Spots;
            foreach (DecorTile tile in _decor)
            {
                int owned = Decor.Owned(s, tile.Kind.Id);
                bool affordable = !full && s.GoldenCarrots >= tile.Kind.Price;
                bool armed = affordable && _armed == "decor:" + tile.Kind.Id && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
                tile.Button.interactable = affordable;
                if (tile.Owned.activeSelf != (owned > 0)) tile.Owned.SetActive(owned > 0);
                tile.OwnedText.text = L10n.T("shop.decor.owned", owned);
                tile.Price.text = full ? L10n.T("shop.decor.full") : (armed ? L10n.T("shop.hats.confirm") : tile.Kind.Price.ToString());
                tile.PriceChip.color = armed ? Palette.Carrot : (affordable ? Palette.Lantern : Palette.Track);
                tile.Plate.color = armed ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : (owned > 0 ? Palette.Mix(Palette.Paper, Palette.Moss, 0.12f) : Palette.Mix(Palette.Paper, Color.white, 0.35f));
            }
            bool canHang = s.GoldenCarrots >= Decor.LanternColorCost;
            for (int i = 0; i < _lanternDots.Count; i++)
            {
                bool hung = s.LanternColor == i;
                _lanternDots[i].interactable = canHang && !hung;
                if (_lanternRings[i].activeSelf != hung) _lanternRings[i].SetActive(hung);
            }
        }

        void RefreshHats(GameState s)
        {
            _last = s;
            foreach (HatTile tile in _hats)
            {
                bool owned = Items.Owns(s, tile.Def.Id);
                bool affordable = s.GoldenCarrots >= tile.Def.Price;
                bool armed = !owned && _armed == tile.Def.Id && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
                tile.Button.interactable = !owned && affordable;
                if (tile.Owned.activeSelf != owned) tile.Owned.SetActive(owned);
                if (tile.PriceChip.gameObject.activeSelf == owned) tile.PriceChip.gameObject.SetActive(!owned);
                tile.Price.text = armed ? L10n.T("shop.hats.confirm") : tile.Def.Price.ToString();
                tile.PriceChip.color = armed ? Palette.Carrot : (affordable ? Palette.Lantern : Palette.Track);
                tile.Plate.color = owned ? Palette.Mix(Palette.Paper, Palette.Moss, 0.2f) : (armed ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : Palette.Mix(Palette.Paper, Color.white, 0.35f));
            }
        }

        /// <summary>"Bạc 60 % · Xanh lá 25 % · …" for a table of odds.</summary>
        public static string Odds(double[] odds)
        {
            var sb = new System.Text.StringBuilder();
            for (int tier = 0; tier < odds.Length; tier++)
            {
                if (tier > 0) sb.Append(" · ");
                sb.Append(L10n.Tier(tier)).Append(' ').Append(UiKit.Times(odds[tier] * 100d).Replace(",0", "")).Append('%');
            }
            return sb.ToString();
        }

        static string OddsText() => L10n.T("shop.odds.clover", Odds(Rabbits.Odds)) + "\n" + L10n.T("shop.odds.golden", Odds(Shop.GoldenOdds));

        public void Open(GameState s)
        {
            Sheet.Open();
            Refresh(s);
        }

        public void Close() => Sheet.Close();

        public void Refresh(GameState s)
        {
            if (!IsOpen) return;
            long day = _today();
            _have.text = L10n.T("shop.have", s.GoldenCarrots);
            bool waiting = Shop.GiftWaiting(s, day);
            int streak = Shop.StreakFor(s, day);
            if (waiting)
            {
                _giftTitle.text = L10n.T("shop.gift.claim", streak);
                _giftSub.text = L10n.T("shop.gift.streak", streak);
                _giftLabel.text = L10n.T("shop.gift.amount", Shop.GiftAt(streak));
            }
            else
            {
                _giftTitle.text = L10n.T("shop.today");
                _giftSub.text = L10n.T("shop.gift.streak", s.GiftStreak);
                _giftLabel.text = L10n.T("shop.gift.done", Shop.GiftAt(s.GiftStreak + 1));
            }
            _gift.SetEnabled(waiting);
            _giftLabel.color = ChunkyButton.InkFor(waiting);
            // The visitor: who, what it costs, how long it stays, and whether it has already gone home with you.
            Shop.Visitor v = Shop.VisitorFor(s, day);
            var shown = new Rabbit { Tier = v.Tier, Kind = v.Kind, Name = "" };
            if (_portrait != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(_portrait); else UnityEngine.Object.DestroyImmediate(_portrait); }
            _portrait = RabbitGridView.Portrait(_visitorPortrait, _catalog, s, shown, 0, 0, 200).gameObject;
            _visitorName.text = L10n.Kind(shown);
            double hours = _hoursLeft();
            string left = hours >= 1d ? L10n.T("shop.visitor.hours", (int)hours) : L10n.T("shop.visitor.minutes", Math.Max(1, (int)(hours * 60d)));
            _visitorSub.text = L10n.T("shop.visitor.sub", L10n.Rarity(v.Tier), L10n.Trade(RabbitKinds.Find(v.Kind).Trade), left);
            bool unmet = !s.SeenKinds.Contains(v.Kind);
            if (_visitorNew.gameObject.activeSelf != unmet) _visitorNew.gameObject.SetActive(unmet);
            bool affordable = s.GoldenCarrots >= v.Price;
            _visitorLabel.text = v.Bought ? L10n.T("shop.visitor.bought") : (affordable ? L10n.T("shop.visitor.buy", v.Price) : L10n.T("shop.visitor.need", v.Price - s.GoldenCarrots));
            _visitorLabel.color = ChunkyButton.InkFor(!v.Bought && affordable);
            _buy.SetEnabled(!v.Bought && affordable);

            bool can = s.GoldenCarrots >= Shop.GoldenSummonCost;
            _goldenLabel.text = can ? L10n.T("shop.golden.button", Shop.GoldenSummonCost) : L10n.T("shop.golden.need", Shop.GoldenSummonCost - s.GoldenCarrots);
            _goldenLabel.color = ChunkyButton.InkFor(can);
            _golden.SetEnabled(can);
            _odds.text = OddsText();
            RefreshHats(s);
            RefreshDecor(s);
        }
    }
}

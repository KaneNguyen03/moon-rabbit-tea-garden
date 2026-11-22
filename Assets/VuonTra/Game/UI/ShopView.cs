using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The tea shop's sheet: today's gift with its streak and the seven-day calendar around it (#57), and the golden call with its odds beside the
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

        /// <summary>One tile of the gift calendar (#57): its plate, the bright ring of today, the tick of a day taken, and what it brings.</summary>
        sealed class DayTile { public Image Plate, Ring, Golden, Hat, Clover; public Text Day, Amount; public GameObject Tick; }
        readonly DayTile[] _days = new DayTile[Shop.WeekDays];
        /// <summary>A tile's size in the calendar row, and the gap between tiles.</summary>
        public const float DayTileWidth = 124f, DayTileHeight = 172f, DayTileGap = 9f;

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
        /// <summary>The moon jar on the shelf (#64), at the far end of the decorations' row: the same two taps buy it.</summary>
        sealed class JarTile { public Button Button; public Image Plate, PriceChip; public Text Price, LeftText; public GameObject Left; }
        JarTile _jar;
        readonly Action _onBuyJar;
        public const string JarKey = "jar";
        /// <summary>How tall the sheet stands: everything down to the note on where golden carrots come from, under the HUD.</summary>
        public const float Height = 1770f;

        public ShopView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Func<long> today, Func<double> hoursLeft, Action onGift, Action onGoldenSummon, Action onBuyVisitor,
            IReadOnlyList<ItemDef> items = null, Action<string> onBuyItem = null, Action<string> onBuyDecor = null, Action<int> onLanternColor = null, Action onBuyMore = null,
            Action onBuyJar = null)
        {
            _onBuyJar = onBuyJar;
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

            // Today: the gift, its streak, and the button that takes it, over the seven-day calendar (#57).
            var today_ = UiKit.Rect("Today", card).Stretch(0, 1, 1, 1, 40, -510, 40, 190);
            today_.AddImage(catalog.Panel, Palette.Card, true);
            if (catalog.GiftBox != null) UiKit.Rect("Icon", today_).Place(0f, 1f, 74, -64, 96, 96).AddImage(catalog.GiftBox, Color.white).preserveAspect = true;
            _giftTitle = UiKit.Rect("Heading", today_).Stretch(0, 1, 1, 1, 136, -64, 320, 12).AddText(L10n.T("shop.today"), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _giftSub = UiKit.Rect("Sub", today_).Stretch(0, 1, 1, 1, 136, -126, 320, 66).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            _giftSub.resizeTextForBestFit = true;
            _giftSub.resizeTextMinSize = 14;
            _giftSub.resizeTextMaxSize = UiKit.TextSmall;
            var giftButton = UiKit.Rect("Gift", today_).Place(1f, 1f, -150, -66, 260, 100);
            _gift = UiKit.Chunky(giftButton, catalog.Panel, () => { Sfx.Blip(); onGift(); });
            _giftLabel = UiKit.Rect("Text", _gift.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _giftLabel.resizeTextForBestFit = true;
            _giftLabel.resizeTextMinSize = 14;
            _giftLabel.resizeTextMaxSize = UiKit.TextBody;
            BuildDays(UiKit.Rect("Days", today_).Band(0, 0, 10, DayTileHeight + 8f), catalog);

            // Today's visitor: a rabbit of Blue or rarer, here until midnight, for golden carrots.
            var visitor = UiKit.Rect("Visitor", card).Stretch(0, 1, 1, 1, 40, -760, 40, 530);
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
            var golden = UiKit.Rect("Golden", card).Stretch(0, 1, 1, 1, 40, -1010, 40, 780);
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
            var hats = UiKit.Rect("Hats", card).Stretch(0, 1, 1, 1, 40, -1350, 40, 1030);
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
                    // One line between the picture and the price: too short a box for two, so a long name such as "Mushroom stools"
                    // shrinks instead of wrapping onto the price chip (#76).
                    Text name = UiKit.Rect("Name", rt).Place(0.5f, 1f, 0, -138, tileW - 12f, 32).AddText(L10n.T("item." + def.Id), 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                    name.resizeTextForBestFit = true;
                    name.resizeTextMinSize = 11;
                    name.resizeTextMaxSize = UiKit.Scaled(18);
                    name.horizontalOverflow = HorizontalWrapMode.Wrap;
                    name.verticalOverflow = VerticalWrapMode.Truncate;
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
            var decorPanel = UiKit.Rect("Decor", card).Stretch(0, 1, 1, 1, 40, -1650, 40, 1350);
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
                // One line between the picture and the price: too short a box for two, so a long name such as "Mushroom stools"
                // shrinks instead of wrapping onto the price chip (#76).
                Text name = UiKit.Rect("Name", rt).Place(0.5f, 1f, 0, -138, tileW - 12f, 32).AddText(L10n.T("decor." + kind.Id), 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 11;
                name.resizeTextMaxSize = UiKit.Scaled(18);
                name.horizontalOverflow = HorizontalWrapMode.Wrap;
                name.verticalOverflow = VerticalWrapMode.Truncate;
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

            _jar = BuildJar(decorRow, catalog, tileW, tileH);

            UiKit.Rect("Earn", card).Stretch(0, 1, 1, 1, 40, -1730, 40, 1650).AddText(L10n.T("shop.earn"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
        }

        /// <summary>The moon jar's tile (#64): the jar, its name, its price chip, and how many the shop still sells today.</summary>
        JarTile BuildJar(RectTransform row, GameCatalog catalog, float tileW, float tileH)
        {
            var tile = new JarTile();
            var rt = UiKit.Rect("Jar", row).Place(1f, 0.5f, -15f - tileW / 2f, 0, tileW, tileH);
            tile.Plate = rt.AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Lantern, 0.25f), true);
            tile.Plate.raycastTarget = true;
            tile.Button = rt.gameObject.AddComponent<Button>();
            tile.Button.transition = Selectable.Transition.None;
            tile.Button.targetGraphic = tile.Plate;
            tile.Button.onClick.AddListener(OnJarTap);
            IncomeView.JarIcon(rt, catalog, "Icon", 100f, 0.5f, 1f, 0, -66);
            Text name = UiKit.Rect("Name", rt).Place(0.5f, 1f, 0, -138, tileW - 12f, 32).AddText(L10n.T("shop.jar.name"), 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 11;
            name.resizeTextMaxSize = UiKit.Scaled(18);
            name.horizontalOverflow = HorizontalWrapMode.Wrap;
            name.verticalOverflow = VerticalWrapMode.Truncate;
            var price = UiKit.Rect("Price", rt).Place(0.5f, 0f, 0, 30, tileW - 24f, 40);
            tile.PriceChip = price.AddImage(catalog.Panel, Palette.Lantern, true);
            float priceInset = 8f;
            if (catalog.GoldenCarrot != null)
            {
                UiKit.Rect("Carrot", price).Place(0f, 0.5f, 20, 0, 26, 26).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
                priceInset = 34f;
            }
            tile.Price = UiKit.Rect("Text", price).Stretch(0, 0, 1, 1, priceInset, 0, 6, 0).AddText(MoonJars.Price.ToString(), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            tile.Price.resizeTextForBestFit = true;
            tile.Price.resizeTextMinSize = 11;
            tile.Price.resizeTextMaxSize = UiKit.TextSmall;
            // How many the shop still sells today, as a small moss badge over the top corner, like the decorations' count.
            var left = UiKit.Rect("Left", rt).Place(0f, 1f, 50, -22, 96, 32);
            left.AddImage(catalog.Panel, Palette.Moss, true);
            tile.LeftText = UiKit.Rect("Text", left).Stretch(0, 0, 1, 1, 4, 0, 4, 0).AddText("", 16, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            tile.LeftText.resizeTextForBestFit = true;
            tile.LeftText.resizeTextMinSize = 10;
            tile.LeftText.resizeTextMaxSize = UiKit.Scaled(16);
            tile.Left = left.gameObject;
            return tile;
        }

        /// <summary>The jar's tile tapped: the same two taps as a hat or a decoration, sharing the one asking tile.</summary>
        void OnJarTap()
        {
            bool confirmed = _armed == JarKey && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            _armed = confirmed ? null : JarKey;
            _armedAt = Time.realtimeSinceStartup;
            Sfx.Click();
            if (confirmed) { if (_onBuyJar != null) _onBuyJar(); }
            else if (_last != null) RefreshJar(_last, _today());
        }

        /// <summary>The price while one can be bought, "Kệ đầy" while the shelf is full, "Mai mua tiếp" once today's three are sold; hidden below garden level 3.</summary>
        void RefreshJar(GameState s, long day)
        {
            _last = s;
            // Like the purse's button, the jar waits for garden level 3: nothing strange for a new player.
            bool shown = MoonJars.Shown(s);
            if (_jar.Button.gameObject.activeSelf != shown) _jar.Button.gameObject.SetActive(shown);
            if (!shown) return;
            MoonJars.BuyCheck check = MoonJars.CanBuy(s, day);
            bool can = check == MoonJars.BuyCheck.Ok;
            bool armed = can && _armed == JarKey && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            _jar.Button.interactable = can;
            _jar.LeftText.text = L10n.T("shop.jar.left", MoonJars.BuyLeft(s, day), MoonJars.BuyPerDay);
            _jar.Price.text = check == MoonJars.BuyCheck.Full ? L10n.T("shop.jar.full")
                : check == MoonJars.BuyCheck.SoldOut ? L10n.T("shop.jar.soldout")
                : armed ? L10n.T("shop.hats.confirm") : MoonJars.Price.ToString();
            _jar.PriceChip.color = armed ? Palette.Carrot : (can ? Palette.Lantern : Palette.Track);
            _jar.Plate.color = armed ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : Palette.Mix(Palette.Paper, Palette.Lantern, 0.25f);
        }

        /// <summary>
        /// The seven tiles of the gift calendar: the day, its golden carrots, and on the first week's seventh day the hat and
        /// the clover beside them. The look of each (taken, today's, still to come) is set by <see cref="RefreshDays"/>.
        /// </summary>
        void BuildDays(RectTransform row, GameCatalog catalog)
        {
            float span = Shop.WeekDays * DayTileWidth + (Shop.WeekDays - 1) * DayTileGap;
            for (int i = 0; i < Shop.WeekDays; i++)
            {
                var tile = new DayTile();
                var rt = UiKit.Rect("Day " + (i + 1), row).Place(0.5f, 0.5f, -span / 2f + DayTileWidth / 2f + i * (DayTileWidth + DayTileGap), 0, DayTileWidth, DayTileHeight);
                tile.Ring = UiKit.Rect("Ring", rt).Stretch(0, 0, 1, 1, -5, -5, -5, -5).AddImage(catalog.Panel, Palette.Carrot, true);
                tile.Plate = UiKit.Rect("Plate", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Card, true);
                tile.Day = UiKit.Rect("Day", rt).Place(0.5f, 1f, 0, -22, DayTileWidth - 10f, 32).AddText("", 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                tile.Day.resizeTextForBestFit = true;
                tile.Day.resizeTextMinSize = 11;
                tile.Day.resizeTextMaxSize = UiKit.Scaled(20);
                tile.Golden = UiKit.Rect("Golden", rt).Place(0.5f, 0.5f, 0, 2, 64, 64).AddImage(catalog.GoldenCarrot, Color.white);
                tile.Golden.preserveAspect = true;
                // The big gift: the hat in the middle, a golden carrot and the clover under it.
                tile.Hat = UiKit.Rect("Hat", rt).Place(0.5f, 0.5f, 0, 14, 62, 62).AddImage(catalog.ItemIcon(Shop.BigGiftHat), Color.white);
                tile.Hat.preserveAspect = true;
                tile.Clover = UiKit.Rect("Clover", rt).Place(0.5f, 0.5f, 32, -24, 40, 40).AddImage(catalog.Clover, Color.white);
                tile.Clover.preserveAspect = true;
                tile.Amount = UiKit.Rect("Amount", rt).Place(0.5f, 0f, 0, 24, DayTileWidth - 10f, 34).AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                tile.Amount.resizeTextForBestFit = true;
                tile.Amount.resizeTextMinSize = 11;
                tile.Amount.resizeTextMaxSize = UiKit.TextSmall;
                // The tick sits on the picture's upper corner like a stamp, clear of the day above and the amount below.
                var tick = UiKit.Rect("Tick", rt).Place(0.5f, 0.5f, 38, 26, 36, 36);
                tick.AddImage(catalog.Round, Palette.Moss);
                UiKit.Rect("Text", tick).Stretch(0, 0, 1, 1).AddText("✓", 22, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                tile.Tick = tick.gameObject;
                _days[i] = tile;
            }
        }

        /// <summary>Taken days solid with a tick, today's ringed bright, later ones faded but readable with their gift.</summary>
        void RefreshDays(GameState s, long day)
        {
            Shop.GiftTile[] week = Shop.Week(s, day);
            for (int i = 0; i < _days.Length; i++)
            {
                DayTile tile = _days[i];
                Shop.GiftTile t = week[i];
                bool later = !t.Claimed && !t.Today;
                tile.Day.text = L10n.T("shop.gift.day", t.Day);
                tile.Amount.text = "+" + t.Golden;
                if (tile.Ring.gameObject.activeSelf != t.Today) tile.Ring.gameObject.SetActive(t.Today);
                if (tile.Tick.activeSelf != t.Claimed) tile.Tick.SetActive(t.Claimed);
                tile.Plate.color = t.Claimed ? Palette.Mix(Palette.Paper, Palette.Moss, 0.35f)
                    : t.Today ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.55f) : Palette.Mix(Palette.Paper, Color.white, 0.5f);
                Color ink = later ? Palette.InkSoft : Palette.Ink;
                tile.Day.color = ink;
                tile.Amount.color = ink;
                Color picture = later ? new Color(1f, 1f, 1f, 0.55f) : Color.white;
                tile.Golden.color = tile.Hat.color = tile.Clover.color = picture;
                // On the big day the golden carrot steps down to the left, to make room for the hat over it.
                RectTransform golden = tile.Golden.rectTransform;
                golden.anchoredPosition = t.Big ? new Vector2(-32, -24) : new Vector2(0, 2);
                golden.sizeDelta = t.Big ? new Vector2(40, 40) : new Vector2(64, 64);
                if (tile.Hat.gameObject.activeSelf != t.Big) tile.Hat.gameObject.SetActive(t.Big);
                if (tile.Clover.gameObject.activeSelf != t.Big) tile.Clover.gameObject.SetActive(t.Big);
            }
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

        /// <summary>"Bạc 60% · Xanh lá 25% · … · Vàng 0,9%" for a table of odds: whole numbers without a trailing ",0" in either language (#72).</summary>
        public static string Odds(double[] odds)
        {
            var sb = new System.Text.StringBuilder();
            for (int tier = 0; tier < odds.Length; tier++)
            {
                if (tier > 0) sb.Append(" · ");
                string percent = (odds[tier] * 100d).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');
                sb.Append(L10n.Tier(tier)).Append(' ').Append(L10n.Number(percent)).Append('%');
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
            string hatName = L10n.T("item." + Shop.BigGiftHat);
            if (waiting)
            {
                bool big = Shop.IsBigDay(s, streak);
                _giftTitle.text = L10n.T("shop.gift.claim", streak);
                _giftSub.text = big ? L10n.T("shop.gift.big.sub", hatName, Shop.BigGiftClovers) : L10n.T("shop.gift.streak", streak);
                _giftLabel.text = big ? L10n.T("shop.gift.big", Shop.GiftAt(streak)) : L10n.T("shop.gift.amount", Shop.GiftAt(streak));
            }
            else
            {
                _giftTitle.text = L10n.T("shop.today");
                // The day the big gift came, the line says what it was: the hat, or golden carrots in its place.
                bool bigToday = s.GiftBigClaimed && s.GiftBigDay == s.GiftDay;
                _giftSub.text = !bigToday ? L10n.T("shop.gift.streak", s.GiftStreak)
                    : s.GiftBigGolden ? L10n.T("shop.gift.big.golden", hatName, Shop.BigGiftHatGolden, Shop.BigGiftClovers)
                    : L10n.T("shop.gift.big.hat", hatName, Shop.BigGiftClovers);
                _giftLabel.text = L10n.T("shop.gift.done", Shop.GiftAt(s.GiftStreak + 1));
            }
            RefreshDays(s, day);
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
            RefreshJar(s, day);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class MonthPassView
    {
        public const float Height = 576f;
        public const float FreeOnlyHeight = FreeTop + RowHeight + 44f;
        public const int PerPage = 10;
        public const int Pages = MonthPass.Tiers / PerPage;
        const float LabelWidth = 156f, CellsRight = 24f;
        const float NumbersTop = 128f, TrackTop = 166f, FreeTop = 186f, RowHeight = 118f, RowGap = 14f;

        sealed class Cell
        {
            public RectTransform Root;
            public Image Plate, Icon, FrameOuter, FrameInner;
            public RectTransform Jar, Frame;
            public Text Amount;
            public GameObject Badge, Tick, Bonus;
        }

        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Text _title, _sub, _page, _bought, _buyLabel;
        readonly Text[] _numbers = new Text[PerPage];
        readonly Cell[] _free = new Cell[PerPage], _premium = new Cell[PerPage];
        readonly RectTransform _fill;
        readonly GameObject _buyRoot, _lockShade, _premiumLabel, _premiumRow;
        readonly LayoutElement _layout;
        readonly Button _prev, _next;
        readonly Text _prevGlyph, _nextGlyph;
        long _iconsMonth = long.MinValue;
        bool _pageChosen;

        public RectTransform Root { get; }
        public int Page { get; private set; }
        public RectTransform BuyButton => (RectTransform)_buyRoot.transform;
        public RectTransform CellOf(MonthRow row, int tier) => (row == MonthRow.Free ? _free : _premium)[(tier - 1) % PerPage].Root;

        public MonthPassView(RectTransform list, GameCatalog catalog, Tweens tweens, Action<MonthRow, int> onCell, Action onBuy)
        {
            _catalog = catalog;
            _tweens = tweens;
            Root = UiKit.Rect("MonthPassRow", list);
            Root.AddImage(catalog.Panel, Palette.CardLip, true);
            _layout = Root.gameObject.AddComponent<LayoutElement>();
            _layout.preferredHeight = _layout.minHeight = Height;
            UiKit.Rect("Card", Root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Card, true);

            _title = UiKit.Rect("Title", Root).Stretch(0, 1, 1, 1, 36, -76, 236, 20)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            QuestChestsView.OneLine(_title, UiKit.TextTitle);
            _sub = UiKit.Rect("Sub", Root).Stretch(0, 1, 1, 1, 36, -116, 236, 76)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft);
            QuestChestsView.OneLine(_sub, UiKit.TextSmall);

            _prev = Arrow("Prev", "‹", -176, () => Turn(-1));
            _next = Arrow("Next", "›", -60, () => Turn(1));
            _prevGlyph = _prev.GetComponentInChildren<Text>();
            _nextGlyph = _next.GetComponentInChildren<Text>();
            _page = UiKit.Rect("Page", Root).Place(1f, 1f, -118, -104, 150, 30)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            QuestChestsView.OneLine(_page, UiKit.TextSmall);

            var numbers = UiKit.Rect("Numbers", Root).Stretch(0, 1, 1, 1, LabelWidth, -(NumbersTop + 34), CellsRight, NumbersTop);
            for (int i = 0; i < PerPage; i++)
            {
                _numbers[i] = UiKit.Rect("N" + i, numbers).Stretch(i / (float)PerPage, 0, (i + 1) / (float)PerPage, 1)
                    .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            var track = UiKit.Rect("Track", Root).Stretch(0, 1, 1, 1, LabelWidth + 8, -(TrackTop + 14), CellsRight + 8, TrackTop);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _fill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Moss, true);

            float premiumTop = FreeTop + RowHeight + RowGap;
            RowLabel("FreeLabel", L10n.T("monthpass.free"), FreeTop);
            _premiumLabel = RowLabel("PremiumLabel", L10n.T("monthpass.premium"), premiumTop).gameObject;
            var freeRow = UiKit.Rect("Free", Root).Stretch(0, 1, 1, 1, LabelWidth, -(FreeTop + RowHeight), CellsRight, FreeTop);
            var premiumRow = UiKit.Rect("Premium", Root).Stretch(0, 1, 1, 1, LabelWidth, -(premiumTop + RowHeight), CellsRight, premiumTop);
            _premiumRow = premiumRow.gameObject;
            for (int i = 0; i < PerPage; i++)
            {
                int column = i;
                _free[i] = BuildCell(freeRow, i, () => onCell?.Invoke(MonthRow.Free, Page * PerPage + column + 1));
                _premium[i] = BuildCell(premiumRow, i, () => onCell?.Invoke(MonthRow.Premium, Page * PerPage + column + 1));
            }
            var shade = UiKit.Rect("Lock", premiumRow).Stretch(0, 0, 1, 1, -4, -4, -4, -4);
            shade.AddImage(catalog.Panel, Palette.Alpha(Palette.Paper, 0.55f), true);
            _lockShade = shade.gameObject;

            var buy = UiKit.Rect("Buy", Root).Place(0.5f, 0f, 0, 70, 640, UiKit.MinTouch);
            ChunkyButton buyButton = null;
            buyButton = UiKit.Chunky(buy, catalog.Panel, () => { buyButton.Press(_tweens); onBuy?.Invoke(); });
            _buyLabel = UiKit.Rect("Text", buyButton.FaceRect).Stretch(0, 0, 1, 1, 16, 0, 16, 0)
                .AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            QuestChestsView.OneLine(_buyLabel, UiKit.TextBody);
            _buyRoot = buy.gameObject;
            _bought = UiKit.Rect("Bought", Root).Place(0.5f, 0f, 0, 66, 900, 60)
                .AddText("", UiKit.TextBody, Palette.MossInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            QuestChestsView.OneLine(_bought, UiKit.TextBody);
        }

        Button Arrow(string name, string glyph, float x, Action turn)
        {
            var rt = UiKit.Rect(name, Root).Place(1f, 1f, x, -50, 96, 64);
            Button button = UiKit.Secondary(rt, _catalog.Panel, glyph, () => { Sfx.Click(); turn(); }, UiKit.TextTitle);
            UiKit.Touchable(rt, 96, 64);
            return button;
        }

        Text RowLabel(string name, string text, float top)
        {
            Text label = UiKit.Rect(name, Root).Stretch(0, 1, 0, 1, 28, -(top + RowHeight), -LabelWidth + 8, top)
                .AddText(text, UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            return label;
        }

        Cell BuildCell(RectTransform row, int i, Action tap)
        {
            var c = new Cell();
            c.Root = UiKit.Rect("Tier " + (i + 1), row).Stretch(i / (float)PerPage, 0, (i + 1) / (float)PerPage, 1, 4, 0, 4, 0);
            c.Plate = c.Root.AddImage(_catalog.Panel, Palette.Paper, true);
            c.Plate.raycastTarget = true;
            var button = c.Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = c.Plate;
            button.onClick.AddListener(() => tap());
            c.Icon = UiKit.Rect("Icon", c.Root).Place(0.5f, 1f, 0, -42, 58, 58).AddImage(null, Color.white);
            c.Icon.preserveAspect = true;
            c.Jar = IncomeView.JarIcon(c.Root, _catalog, "Jar", 54f, 0.5f, 1f, 0, -42);
            c.Frame = UiKit.Rect("Frame", c.Root).Place(0.5f, 1f, 0, -42, 52, 52);
            c.FrameOuter = c.Frame.AddImage(null, Color.white);
            c.FrameInner = UiKit.Rect("Inside", c.Frame).Stretch(0, 0, 1, 1, 9, 9, 9, 9).AddImage(null, Palette.Paper);
            c.Amount = UiKit.Rect("Amount", c.Root).Stretch(0, 0, 1, 0, 2, 6, 2, -36)
                .AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            QuestChestsView.OneLine(c.Amount, UiKit.TextSmall);
            var bonus = UiKit.Rect("Bonus", c.Root).Place(0f, 1f, 18, -14, 44, 30);
            bonus.AddImage(_catalog.Round, Palette.Lantern);
            UiKit.Rect("Text", bonus).Stretch(0, 0, 1, 1).AddText("+" + MonthPass.BonusGolden, 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            c.Bonus = bonus.gameObject;
            var badge = UiKit.Rect("Badge", c.Root).Place(1f, 1f, -10, -10, 34, 34);
            badge.AddImage(_catalog.Round, Palette.CarrotLip);
            UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("!", 22, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            c.Badge = badge.gameObject;
            var tick = UiKit.Rect("Tick", c.Root).Place(1f, 1f, -10, -10, 34, 34);
            tick.AddImage(_catalog.Round, Palette.Moss);
            UiKit.Rect("Text", tick).Stretch(0, 0, 1, 1).AddText("✓", 22, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            c.Tick = tick.gameObject;
            return c;
        }

        void Turn(int step)
        {
            Page = Mathf.Clamp(Page + step, 0, Pages - 1);
            _pageChosen = true;
            _iconsMonth = long.MinValue;
            Changed?.Invoke();
        }

        public event Action Changed;

        public void ShowPage(int page)
        {
            Page = Mathf.Clamp(page, 0, Pages - 1);
            _pageChosen = true;
            _iconsMonth = long.MinValue;
        }

        public static int PageFor(GameState s, long day)
        {
            for (int t = 1; t <= MonthPass.Tiers; t++)
                if (MonthPass.Ready(s, MonthRow.Free, t, day) || MonthPass.Ready(s, MonthRow.Premium, t, day)) return (t - 1) / PerPage;
            return Mathf.Clamp(MonthPass.Tier(s, day) / PerPage, 0, Pages - 1);
        }

        public void Refresh(GameState s, long day, string price)
        {
            long month = MonthPass.MonthOf(day);
            if (!_pageChosen) Page = PageFor(s, day);
            int tier = MonthPass.Tier(s, day), points = MonthPass.Points(s, day);
            bool premium = MonthPass.Premium(s, day);
            string name = MonthName(month);
            _title.text = L10n.T("monthpass.title", name, tier, MonthPass.Tiers);
            _sub.text = L10n.T("monthpass.sub", MonthPass.DaysLeft(day), MonthPass.PointsPerTier);
            int first = Page * PerPage + 1;
            _page.text = L10n.T("monthpass.page", first, first + PerPage - 1);
            Shade(_prev, _prevGlyph, Page > 0);
            Shade(_next, _nextGlyph, Page < Pages - 1);
            for (int i = 0; i < PerPage; i++) _numbers[i].text = (first + i).ToString();
            float filled = Mathf.Clamp01((points / (float)MonthPass.PointsPerTier - Page * PerPage) / PerPage);
            _fill.anchorMax = new Vector2(filled, 1f);
            if (_iconsMonth != month)
            {
                _iconsMonth = month;
                for (int i = 0; i < PerPage; i++)
                {
                    Dress(_free[i], MonthPass.GiftOf(MonthRow.Free, first + i), month);
                    Dress(_premium[i], MonthPass.GiftOf(MonthRow.Premium, first + i), month);
                }
            }
            for (int i = 0; i < PerPage; i++)
            {
                State(_free[i], s, MonthRow.Free, first + i, day, true);
                State(_premium[i], s, MonthRow.Premium, first + i, day, premium);
            }
            bool sold = Features.Monetization;
            bool lower = premium || sold;
            if (_premiumRow.activeSelf != lower) _premiumRow.SetActive(lower);
            if (_premiumLabel.activeSelf != lower) _premiumLabel.SetActive(lower);
            float height = lower ? Height : FreeOnlyHeight;
            if (_layout.preferredHeight != height) _layout.preferredHeight = _layout.minHeight = height;
            if (_lockShade.activeSelf == premium) _lockShade.SetActive(!premium);
            bool buy = sold && !premium;
            if (_buyRoot.activeSelf != buy) _buyRoot.SetActive(buy);
            _bought.gameObject.SetActive(premium);
            _buyLabel.text = L10n.T("monthpass.buy", string.IsNullOrEmpty(price) ? MonthPass.ReferencePrice : price);
            _bought.text = L10n.T("monthpass.bought", name);
        }

        static void Shade(Button arrow, Text glyph, bool on)
        {
            arrow.interactable = on;
            glyph.color = Palette.Alpha(Palette.Ink, on ? 1f : 0.3f);
        }

        public static string MonthName(long month) => L10n.T("monthpass.month." + MonthPass.MonthNumber(month));

        void State(Cell c, GameState s, MonthRow row, int tier, long day, bool open)
        {
            bool claimed = MonthPass.Claimed(s, row, tier);
            bool reached = tier <= MonthPass.Tier(s, day);
            bool ready = open && reached && !claimed;
            if (c.Badge.activeSelf != ready) c.Badge.SetActive(ready);
            if (c.Tick.activeSelf != claimed) c.Tick.SetActive(claimed);
            c.Plate.color = ready ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.45f)
                : claimed ? Palette.Mix(Palette.Paper, Palette.Moss, 0.18f)
                : reached ? Palette.Paper : Palette.Mix(Palette.Paper, Palette.Track, 0.45f);
            float alpha = claimed ? 0.55f : 1f;
            c.Icon.color = Palette.Alpha(c.Icon.color, alpha);
            c.Amount.color = Palette.Alpha(Palette.Ink, claimed ? 0.6f : 1f);
        }

        void Dress(Cell c, MonthGift gift, long month)
        {
            int cal = MonthPass.CalendarMonth(month);
            bool jar = gift.Kind == MonthGiftKind.Jar, frame = gift.Kind == MonthGiftKind.Frame;
            c.Jar.gameObject.SetActive(jar);
            c.Frame.gameObject.SetActive(frame);
            c.Icon.gameObject.SetActive(!jar && !frame);
            c.Bonus.SetActive(gift.Golden > 0);
            c.Icon.rectTransform.sizeDelta = new Vector2(58, 58);
            switch (gift.Kind)
            {
                case MonthGiftKind.Income:
                    Icon(c, _catalog.Currency, Color.white);
                    c.Amount.text = L10n.T("monthpass.cell.minutes", gift.Amount / 60);
                    break;
                case MonthGiftKind.Clover:
                    Icon(c, _catalog.Clover != null ? _catalog.Clover : _catalog.Round, _catalog.Clover != null ? Color.white : Palette.Moss);
                    c.Amount.text = "+" + gift.Amount;
                    break;
                case MonthGiftKind.Jar:
                    c.Amount.text = "+" + gift.Amount;
                    break;
                case MonthGiftKind.Golden:
                    Icon(c, _catalog.GoldenCarrot != null ? _catalog.GoldenCarrot : _catalog.Currency, _catalog.GoldenCarrot != null ? Color.white : Palette.Lantern);
                    c.Amount.text = "+" + gift.Amount;
                    break;
                case MonthGiftKind.Lantern:
                    Icon(c, LanternSwatch.SpriteFor(_catalog), LanternSwatch.TintFor(_catalog, MonthPass.LanternIndex(cal)));
                    c.Icon.rectTransform.sizeDelta = new Vector2(48, 48);
                    c.Amount.text = L10n.T("monthpass.cell.lantern");
                    break;
                case MonthGiftKind.Frame:
                    c.FrameOuter.color = Palette.Hex(MonthPass.FrameColors[cal]);
                    c.FrameInner.color = Palette.Mix(Palette.Paper, Color.white, 0.4f);
                    c.Amount.text = L10n.T("monthpass.cell.frame");
                    break;
                case MonthGiftKind.Rabbit:
                    string[] kinds = MonthPass.KindChoices(month);
                    Sprite art = kinds.Length > 0 ? _catalog.KindSprite(kinds[0]) : null;
                    Icon(c, art != null ? art : _catalog.RabbitFor(MonthPass.RabbitRarity), Color.white);
                    c.Amount.text = L10n.T("monthpass.cell.rabbit", kinds.Length);
                    break;
            }
        }

        static void Icon(Cell c, Sprite sprite, Color tint)
        {
            c.Icon.sprite = sprite;
            c.Icon.color = tint;
        }

        public void Pop(MonthRow row, int tier)
        {
            RectTransform cell = CellOf(row, tier);
            _tweens.Add(Motion.Tap, t => { if (cell != null) cell.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (cell != null) cell.localScale = Vector3.one; });
        }
    }

    public sealed class MonthKindChooser
    {
        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly GameCatalog _catalog;
        readonly Text _title, _sub;
        readonly Image[] _art = new Image[MonthPass.Choices];
        readonly Text[] _names = new Text[MonthPass.Choices];
        readonly string[] _kinds = new string[MonthPass.Choices];
        Action<string> _picked;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Root => _card;

        public MonthKindChooser(RectTransform canvas, GameCatalog catalog, Tweens tweens)
        {
            _tweens = tweens;
            _catalog = catalog;
            var overlay = UiKit.Rect("MonthKinds", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 900, 700);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            _title = UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -84, 800, 70).AddText("", 46, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            QuestChestsView.OneLine(_title, 46);
            _sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -148, 800, 48).AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            QuestChestsView.OneLine(_sub, UiKit.TextBody);
            const float tileW = 250f, gap = 24f;
            for (int i = 0; i < MonthPass.Choices; i++)
            {
                int index = i;
                var tile = UiKit.Rect("Kind " + i, _card).Place(0.5f, 0.5f, (i - 1) * (tileW + gap), -20, tileW, 330);
                Image plate = tile.AddImage(catalog.Panel, Palette.Mix(Palette.Tiers[MonthPass.RabbitRarity], Palette.Paper, 0.6f), true);
                plate.raycastTarget = true;
                var button = tile.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = plate;
                button.onClick.AddListener(() => Pick(index));
                _art[i] = UiKit.Rect("Art", tile).Place(0.5f, 1f, 0, -120, 190, 190).AddImage(null, Color.white);
                _art[i].preserveAspect = true;
                _names[i] = UiKit.Rect("Name", tile).Place(0.5f, 0f, 0, 70, tileW - 16, 56).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                QuestChestsView.OneLine(_names[i], UiKit.TextBody);
                UiKit.Stars(tile, MonthPass.RabbitRarity, 0.5f, 0f, 0, 30, 22);
            }
            var later = UiKit.Rect("Later", _card).Place(0.5f, 0f, 0, 70, 300, UiKit.MinTouch);
            UiKit.TextButton(later, L10n.T("monthpass.choose.later"), () => { Sfx.Click(); Close(); }, UiKit.TextBody);
            _root.SetActive(false);
        }

        public void Open(long month, Action<string> picked)
        {
            _picked = picked;
            string[] kinds = MonthPass.KindChoices(month);
            _title.text = L10n.T("monthpass.choose.title");
            _sub.text = L10n.T("monthpass.choose.sub", MonthPassView.MonthName(month));
            for (int i = 0; i < _kinds.Length; i++)
            {
                _kinds[i] = i < kinds.Length ? kinds[i] : null;
                Sprite art = _kinds[i] != null ? _catalog.KindSprite(_kinds[i]) : null;
                _art[i].sprite = art != null ? art : _catalog.RabbitFor(MonthPass.RabbitRarity);
                _names[i].text = _kinds[i] != null ? L10n.T("kind." + _kinds[i]) : "";
            }
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            RectTransform card = _card;
            _tweens.Add(Motion.Time(Motion.Panel), t => { if (card != null) card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
        }

        public void Pick(int index)
        {
            string kind = index >= 0 && index < _kinds.Length ? _kinds[index] : null;
            if (kind == null) return;
            Action<string> picked = _picked;
            Close();
            picked?.Invoke(kind);
        }

        public void Close()
        {
            _root.SetActive(false);
            _picked = null;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class FairView
    {
        public const float CardWidth = 1010f, CardHeight = 1450f;
        public const float ChipX = 671f, ChipY = -154f, ChipWidth = 118f, ChipHeight = 52f;
        public const float TileWidth = 290f, TileHeight = 300f, TileStep = 310f;
        public const float ConfirmSeconds = 2f;

        sealed class Coin
        {
            public Image Disc;
            public Text Letter;
        }

        sealed class Tile
        {
            public RectTransform Root;
            public Image Plate, PriceChip, Icon;
            public Text Name, Left, Price;
            public RectTransform RabbitHolder;
            public GameObject Portrait;
        }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action<int> _onStall;
        readonly GameObject _root;
        readonly RectTransform _card, _chip, _banner, _weekend;
        readonly Image _bannerFace, _gate, _weekendFace, _chipFace;
        readonly Text _title, _days, _weekendText, _have, _sources, _end, _chipText;
        readonly GameObject _chipDouble;
        readonly List<Coin> _coins = new List<Coin>();
        readonly Tile[] _tiles = new Tile[Fairs.Stall.Length];
        int _armed = -1;
        float _armedAt;
        int _portraitFair = -1;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Card => _card;
        public RectTransform Chip => _chip;
        public bool ChipShown => _chip.gameObject.activeSelf;
        public string ChipText => _chipText.text;
        public bool ChipDouble => _chipDouble.activeSelf;
        public string Title => _title.text;
        public string Days => _days.text;
        public string Weekend => _weekendText.text;
        public string Have => _have.text;
        public RectTransform TileOf(int stall) => _tiles[stall].Root;
        public string PriceOf(int stall) => _tiles[stall].Price.text;
        public string LeftOf(int stall) => _tiles[stall].Left.text;

        public FairView(RectTransform canvas, RectTransform hud, GameRunner runner, Tweens tweens, Action<int> onStall)
        {
            _runner = runner;
            _catalog = runner.Catalog;
            _tweens = tweens;
            _onStall = onStall;
            GameCatalog catalog = _catalog;

            _chip = UiKit.Rect("Fair", hud).Place(0f, 1f, ChipX, ChipY, ChipWidth, ChipHeight);
            UiKit.Touchable(_chip, ChipWidth, ChipHeight);
            _chip.AddShadow(10f, 6f, 0.25f);
            _chipFace = _chip.AddImage(catalog.Panel, Palette.Paper, true);
            _chipFace.raycastTarget = true;
            var chipButton = _chip.gameObject.AddComponent<Button>();
            chipButton.transition = Selectable.Transition.None;
            chipButton.targetGraphic = _chipFace;
            chipButton.onClick.AddListener(() => { Sfx.Click(); Open(); });
            CoinIcon(_chip, "Icon", 38f, 0f, 0.5f, 28f, 0f);
            _chipText = UiKit.Rect("Text", _chip).Stretch(0, 0, 1, 1, 52, 0, 8, 0).AddText("0", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _chipText.resizeTextForBestFit = true;
            _chipText.resizeTextMinSize = 14;
            _chipText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            var twice = UiKit.Rect("Double", _chip).Place(1f, 1f, -4, 2, 44, 30);
            twice.AddImage(catalog.Panel, Palette.Carrot, true);
            UiKit.Rect("Text", twice).Stretch(0, 0, 1, 1).AddText("×" + Fairs.WeekendTimes, 18, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _chipDouble = twice.gameObject;
            _chip.gameObject.SetActive(false);

            var overlay = UiKit.Rect("FairCard", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, CardWidth, CardHeight);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;

            _banner = UiKit.Rect("Banner", _card).Place(0.5f, 1f, 0, -200, 940, 330);
            _bannerFace = _banner.AddImage(catalog.Panel, Palette.Card, true);
            _gate = UiKit.Rect("Gate", _banner).Place(0.5f, 1f, 0, -96, 560, 210).AddImage(catalog.GateMidAutumn, Color.white);
            _gate.preserveAspect = true;
            _title = Fit(UiKit.Rect("Title", _banner).Place(0.5f, 0f, 0, 104, 860, 72)
                .AddText("", 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 52, 26);
            _title.AddShadow(0.25f, 2f);
            _days = Fit(UiKit.Rect("Days", _banner).Place(0.5f, 0f, 0, 44, 860, 46)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);

            _weekend = UiKit.Rect("Weekend", _card).Place(0.5f, 1f, 0, -410, 820, 60);
            _weekendFace = _weekend.AddImage(catalog.Panel, Palette.Carrot, true);
            _weekendText = Fit(UiKit.Rect("Text", _weekend).Stretch(0, 0, 1, 1, 20, 0, 20, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);

            var have = UiKit.Rect("Have", _card).Place(0.5f, 1f, 0, -496, 700, 70);
            CoinIcon(have, "Icon", 56f, 0f, 0.5f, 40f, 0f);
            _have = Fit(UiKit.Rect("Text", have).Stretch(0, 0, 1, 1, 84, 0, 10, 0)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextTitle, 16);

            for (int i = 0; i < _tiles.Length; i++) _tiles[i] = BuildTile(i);

            _sources = Fit(UiKit.Rect("Sources", _card).Place(0.5f, 0f, 0, 200, 900, 76)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextSmall, 12);
            _sources.horizontalOverflow = HorizontalWrapMode.Wrap;
            _end = Fit(UiKit.Rect("End", _card).Place(0.5f, 0f, 0, 118, 900, 70)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextSmall, 12);
            _end.horizontalOverflow = HorizontalWrapMode.Wrap;

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

        static Text Fit(Text text, int size, int least)
        {
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(size);
            return text;
        }

        public static string CoinLetter(string coinName) => string.IsNullOrEmpty(coinName) ? "" : coinName.Substring(0, 1).ToUpperInvariant();

        RectTransform CoinIcon(RectTransform parent, string name, float size, float anchorX, float anchorY, float x, float y)
        {
            var rt = UiKit.Rect(name, parent).Place(anchorX, anchorY, x, y, size, size);
            UiKit.Rect("Rim", rt).Stretch(0, 0, 1, 1).AddImage(_catalog.Round, Palette.Mix(Palette.Ink, Palette.Paper, 0.25f));
            var coin = new Coin { Disc = UiKit.Rect("Disc", rt).Stretch(0, 0, 1, 1, 3, 3, 3, 3).AddImage(_catalog.Round, Palette.Lantern) };
            UiKit.Rect("Shine", rt).Stretch(0.18f, 0.5f, 0.62f, 0.86f).AddImage(_catalog.Round, Palette.Alpha(Color.white, 0.35f));
            coin.Letter = UiKit.Rect("Letter", rt).Stretch(0, 0, 1, 1).AddText("", Mathf.RoundToInt(size * 0.55f), Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _coins.Add(coin);
            return rt;
        }

        Tile BuildTile(int stall)
        {
            int column = stall % 3, row = stall / 3;
            var tile = new Tile();
            tile.Root = UiKit.Rect("Stall " + stall, _card).Place(0.5f, 1f, (column - 1) * TileStep, -706 - row * (TileHeight + 16f), TileWidth, TileHeight);
            tile.Plate = tile.Root.AddImage(_catalog.Panel, Palette.Card, true);
            tile.Plate.raycastTarget = true;
            var button = tile.Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = tile.Plate;
            button.onClick.AddListener(() => OnTap(stall));
            FairWare ware = Fairs.Stall[stall].Ware;
            if (ware == FairWare.Jars) IncomeView.JarIcon(tile.Root, _catalog, "Icon", 104f, 0.5f, 1f, 0, -80);
            else if (ware == FairWare.Rabbit) tile.RabbitHolder = UiKit.Rect("Rabbit", tile.Root).Place(0.5f, 1f, 0, -86, 140, 140);
            else if (ware == FairWare.Frame)
            {
                var frame = UiKit.Rect("Icon", tile.Root).Place(0.5f, 1f, 0, -80, 120, 100);
                tile.Icon = frame.AddImage(_catalog.Panel, Palette.Paper, true);
                UiKit.Rect("Photo", frame).Stretch(0, 0, 1, 1, 16, 16, 16, 16).AddImage(_catalog.Panel, Palette.Mix(Palette.Night, Palette.Grass, 0.35f), true);
            }
            else
            {
                Sprite art = ware == FairWare.Clovers ? _catalog.Clover : ware == FairWare.Golden ? _catalog.GoldenCarrot : _catalog.Round;
                tile.Icon = UiKit.Rect("Icon", tile.Root).Place(0.5f, 1f, 0, -80, ware == FairWare.Lantern ? 84 : 104, ware == FairWare.Lantern ? 84 : 104).AddImage(art, Color.white);
                tile.Icon.preserveAspect = true;
            }
            tile.Name = Fit(UiKit.Rect("Name", tile.Root).Place(0.5f, 1f, 0, -178, TileWidth - 20f, 40)
                .AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 12);
            tile.Left = Fit(UiKit.Rect("Left", tile.Root).Place(0.5f, 1f, 0, -214, TileWidth - 20f, 32)
                .AddText("", 18, Palette.InkSoft, TextAnchor.MiddleCenter), 18, 11);
            var price = UiKit.Rect("Price", tile.Root).Place(0.5f, 0f, 0, 36, TileWidth - 40f, 50);
            tile.PriceChip = price.AddImage(_catalog.Panel, Palette.Lantern, true);
            CoinIcon(price, "Coin", 34f, 0f, 0.5f, 26f, 0f);
            tile.Price = Fit(UiKit.Rect("Text", price).Stretch(0, 0, 1, 1, 48, 0, 10, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 12);
            return tile;
        }

        public static string WareName(FairWare ware) => L10n.T("fair.ware." + ware);

        static string NameOf(FairRun run, FairWare ware) => ware == FairWare.Rabbit ? L10n.T("kind." + run.Def.RabbitKind) : WareName(ware);

        void OnTap(int stall)
        {
            Sfx.Click();
            GameState s = _runner.State;
            FairRun? now = _runner.FairNow;
            if (s == null || now == null) return;
            FairStall ware = now.Value.Def.Stalls[stall];
            bool owned = Owned(s, now.Value, ware.Ware);
            bool confirmed = owned || (_armed == stall && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds);
            _armed = confirmed ? -1 : stall;
            _armedAt = Time.realtimeSinceStartup;
            if (confirmed) _onStall?.Invoke(stall);
            Refresh();
        }

        static bool Owned(GameState s, FairRun run, FairWare ware)
            => (ware == FairWare.Lantern && Fairs.OwnsLantern(s, run.Index)) || (ware == FairWare.Frame && Fairs.OwnsFrame(s, run.Index));

        public void Open()
        {
            if (_runner.State == null || _runner.FairNow == null) return;
            _armed = -1;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Refresh();
            _tweens.Grow(_card, 0.85f, Motion.Time(0.25f), Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);

        public static int CoinsOf(GameState s, FairRun run) => s.Fair.Start == run.Start ? s.Fair.Coins : 0;

        public void RefreshChip()
        {
            GameState s = _runner.State;
            FairRun? now = s == null ? null : _runner.FairNow;
            bool show = now != null && Tutorial.IsDone(s);
            if (_chip.gameObject.activeSelf != show) _chip.gameObject.SetActive(show);
            if (!show)
            {
                if (_root.activeSelf) Close();
                return;
            }
            FairRun run = now.Value;
            Paint(run);
            _chipText.text = CoinsOf(s, run).ToString();
            bool twice = Fairs.Weekend(_runner.TodayNumber);
            if (_chipDouble.activeSelf != twice) _chipDouble.SetActive(twice);
        }

        void Paint(FairRun run)
        {
            Color fair = Palette.Hex(run.Def.Hex);
            string letter = CoinLetter(L10n.T(run.Def.CoinKey));
            foreach (Coin coin in _coins)
            {
                coin.Disc.color = fair;
                coin.Letter.text = letter;
            }
            _chipFace.color = Palette.Mix(Palette.Paper, fair, 0.18f);
        }

        public void Refresh()
        {
            GameState s = _runner.State;
            FairRun? now = s == null ? null : _runner.FairNow;
            if (now == null || !_root.activeSelf) return;
            FairRun run = now.Value;
            long today = _runner.TodayNumber;
            Color fair = Palette.Hex(run.Def.Hex);
            string coinName = L10n.T(run.Def.CoinKey);
            Paint(run);
            _bannerFace.color = Palette.Mix(Palette.Paper, fair, 0.4f);
            _gate.color = Palette.Mix(Color.white, fair, 0.85f);
            _title.text = L10n.T(run.Def.NameKey);
            _days.text = L10n.T("fair.days", run.DaysLeft(today), run.DayOf(today), run.Def.Days);
            bool twice = Fairs.Weekend(today);
            _weekendText.text = L10n.T(twice ? "fair.weekend" : "fair.weekday", coinName, Fairs.WeekendTimes);
            _weekendFace.color = twice ? Palette.Carrot : Palette.Mix(Palette.Paper, fair, 0.25f);
            int coins = CoinsOf(s, run);
            _have.text = L10n.T("fair.have", coins, coinName);
            _sources.text = L10n.T("fair.sources", coinName, Fairs.OrderCoins, Fairs.StarCoins[1], Fairs.StarCoins[2], Fairs.StarCoins[3],
                Fairs.FishCoins[0], Fairs.FishCoins[Fairs.FishCoins.Length - 1], Fairs.PhotoAskCoins);
            _end.text = L10n.T("fair.end", coinName, Fairs.CarrotSecondsPerCoin);
            for (int i = 0; i < _tiles.Length; i++) RefreshTile(s, run, i, coins, today, fair);
        }

        void RefreshTile(GameState s, FairRun run, int stall, int coins, long today, Color fair)
        {
            Tile tile = _tiles[stall];
            FairStall ware = run.Def.Stalls[stall];
            tile.Name.text = NameOf(run, ware.Ware);
            if (ware.Ware == FairWare.Lantern) tile.Icon.color = fair;
            if (ware.Ware == FairWare.Frame) tile.Icon.color = Palette.Hex(Fairs.FrameHex(Fairs.FrameIndex(run.Index)));
            if (ware.Ware == FairWare.Rabbit && _portraitFair != run.Index) ShowRabbit(s, run);
            bool owned = Owned(s, run, ware.Ware);
            FairBuyCheck check = Fairs.CanBuy(s, today, stall);
            bool can = check == FairBuyCheck.Ok || (check == FairBuyCheck.Closed && coins >= ware.Price && Fairs.Bought(s, stall) < ware.Max);
            bool armed = can && _armed == stall && Time.realtimeSinceStartup - _armedAt < ConfirmSeconds;
            int left = Math.Max(0, ware.Max - Fairs.Bought(s, stall));
            tile.Left.text = owned ? L10n.T("fair.owned") : L10n.T("fair.left", left, ware.Max);
            tile.Price.text = owned ? L10n.T(ware.Ware == FairWare.Lantern ? "fair.hang" : "fair.wear")
                : left == 0 ? L10n.T("fair.soldout")
                : armed ? L10n.T("shop.hats.confirm") : ware.Price.ToString();
            tile.PriceChip.color = armed ? Palette.Carrot : owned ? Palette.Moss : can ? Palette.Lantern : Palette.Track;
            tile.Plate.color = armed ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : owned ? Palette.Mix(Palette.Paper, Palette.Moss, 0.2f) : Palette.Card;
        }

        void ShowRabbit(GameState s, FairRun run)
        {
            Tile tile = _tiles[Array.FindIndex(run.Def.Stalls, w => w.Ware == FairWare.Rabbit)];
            if (tile.Portrait != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(tile.Portrait); else UnityEngine.Object.DestroyImmediate(tile.Portrait); }
            var shown = new Rabbit { Tier = RabbitKinds.Find(run.Def.RabbitKind).Tier, Kind = run.Def.RabbitKind, Name = "", Dye = Fairs.Dye(run.Index) };
            tile.Portrait = RabbitGridView.Portrait(tile.RabbitHolder, _catalog, s, shown, 0, 0, 140).gameObject;
            _portraitFair = run.Index;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class RabbitGridView
    {
        public const int Columns = 3;
        const float CellW = 318f, CellGap = 15f, CellH = 322f, AlbumH = 290f;
        const float SetLineH = 56f, SetPad = 10f, SetOutline = 8f;
        const float SummonH = 124f, SummonLuckyH = 176f;

        public static readonly Color Quiet = Palette.Mix(Palette.Ink, Palette.Paper, 0.34f);

        readonly RectTransform _content;
        readonly GameCatalog _catalog;
        readonly Action _onSummon, _onAuto;
        readonly Action<int> _onMerge;
        readonly Action<long> _onOpen;
        readonly Action<string> _onKind;
        readonly Func<string, bool> _isFresh;
        readonly List<GameObject> _dynamic = new List<GameObject>();
        Text _count, _clovers, _summonLabel, _luckyLabel;
        RectTransform _lucky;
        LayoutElement _summonCard;
        bool? _luckyShown;
        readonly Image[] _lamps = new Image[Rabbits.LuckyAfter];
        Image _mineTab, _albumTab;
        Text _mineLabel, _albumLabel;
        string _signature;

        public ChunkyButton Summon { get; private set; }
        public bool Album { get; private set; }

        public RabbitGridView(RectTransform content, GameCatalog catalog, Action onSummon, Action onAuto, Action<int> onMerge,
            Action<long> onOpen, Action<string> onKind, Action onBuySpot, Func<string, bool> isFresh = null)
        {
            _isFresh = isFresh;
            _content = content;
            _catalog = catalog;
            _onSummon = onSummon;
            _onAuto = onAuto;
            _onMerge = onMerge;
            _onOpen = onOpen;
            _onKind = onKind;
            BuildHeader();
        }

        void BuildHeader()
        {
            var top = UiKit.Rect("Count", _content);
            top.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;
            _count = UiKit.Rect("Text", top).Stretch(0, 0, 1, 1, 8, 0, 90, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var auto = UiKit.Rect("Auto", top).Place(1f, 0.5f, -36, 0, 64, 64);
            UiKit.Touchable(auto, 64, 64);
            Image autoImage = auto.AddImage(_catalog.Round, Palette.Track);
            autoImage.raycastTarget = true;
            var autoButton = auto.gameObject.AddComponent<Button>();
            autoButton.transition = Selectable.Transition.None;
            autoButton.targetGraphic = autoImage;
            autoButton.onClick.AddListener(() => _onAuto());
            for (int i = 0; i < 3; i++)
                UiKit.Rect("Bar", auto).Place(0.5f, 0.5f, -6 + i * 3, 12 - i * 12, 30 - i * 6, 7).AddImage(_catalog.Panel, Palette.Ink, true);

            var tabs = UiKit.Rect("SubTabs", _content);
            tabs.gameObject.AddComponent<LayoutElement>().preferredHeight = UiKit.MinTouch + 12f;
            UiKit.Rect("Track", tabs).Stretch(0, 0, 1, 1, 0, 2, 0, 2).AddImage(_catalog.Panel, Palette.Track, true);
            _mineTab = AddSubTab(tabs, "Mine", 0f, L10n.T("rabbits.tab.mine"), false, out _mineLabel);
            _albumTab = AddSubTab(tabs, "Album", 0.5f, L10n.T("rabbits.tab.album"), true, out _albumLabel);

            var root = UiKit.Rect("Summon", _content);
            root.AddImage(_catalog.Panel, Palette.CardLip, true);
            _summonCard = root.gameObject.AddComponent<LayoutElement>();
            _summonCard.preferredHeight = SummonH;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            Image shade = UiKit.Rect("Shade", root).Place(0f, 0.5f, 68, 6, 92, 100).AddImage(_catalog.Rabbit, Palette.Alpha(Palette.Ink, 0.5f));
            shade.preserveAspect = true;
            UiKit.Rect("Mark", root).Place(0f, 0.5f, 68, 8, 60, 60).AddText("?", 44, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _clovers = UiKit.Rect("Clovers", root).Stretch(0, 0, 1, 1, 130, 8, 330, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _clovers.resizeTextForBestFit = true;
            _clovers.resizeTextMinSize = 18;
            _clovers.resizeTextMaxSize = 26;
            BuildLucky(root);
            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -164, 4, 290, 92);
            Summon = UiKit.Chunky(button, _catalog.Panel, () => _onSummon());
            var odds = UiKit.Rect("Odds", _content);
            odds.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            Text oddsText = odds.AddText(L10n.T("shop.odds.clover", ShopView.Odds(Rabbits.Odds)) + "\n" + L10n.T("shop.odds.golden", ShopView.Odds(Shop.GoldenOdds)), UiKit.TextSmall, Quiet, TextAnchor.MiddleLeft);
            oddsText.resizeTextForBestFit = true;
            oddsText.resizeTextMinSize = 12;
            oddsText.resizeTextMaxSize = UiKit.TextSmall;
            _summonLabel = UiKit.Rect("Text", Summon.FaceRect).Stretch(0, 0, 1, 1)
                .AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        void BuildLucky(RectTransform root)
        {
            _lucky = UiKit.Rect("Lucky", root).Stretch(0, 0, 1, 0.56f, 130, 16, 330, 0);
            for (int i = 0; i < _lamps.Length; i++)
            {
                var lamp = UiKit.Rect("Lamp " + i, _lucky).Place(0f, 1f, 15 + i * 36, -15, 30, 30);
                lamp.AddImage(_catalog.Round, Palette.OffLip);
                _lamps[i] = UiKit.Rect("Light", lamp).Stretch(0, 0, 1, 1, 4, 4, 4, 4).AddImage(_catalog.Round, Palette.Card);
            }
            _luckyLabel = UiKit.Rect("Text", _lucky).Stretch(0, 0, 1, 1, 0, 0, 0, 34)
                .AddText("", UiKit.TextSmall, Quiet, TextAnchor.MiddleLeft, FontStyle.Bold);
            _luckyLabel.resizeTextForBestFit = true;
            _luckyLabel.resizeTextMinSize = 14;
            _luckyLabel.resizeTextMaxSize = UiKit.TextSmall;
        }

        void RefreshLucky(GameState s)
        {
            bool promise = Rabbits.LuckyCanPromise(s);
            if (_luckyShown != promise)
            {
                _luckyShown = promise;
                _lucky.gameObject.SetActive(promise);
                _summonCard.preferredHeight = promise ? SummonLuckyH : SummonH;
                if (promise) _clovers.rectTransform.Stretch(0, 0.56f, 1, 1, 130, 0, 330, 8);
                else _clovers.rectTransform.Stretch(0, 0, 1, 1, 130, 8, 330, 0);
            }
            if (!promise) return;
            int lit = Rabbits.LuckyLamps(s);
            bool ready = Rabbits.LuckyReady(s);
            for (int i = 0; i < _lamps.Length; i++)
                _lamps[i].color = i >= lit ? Palette.Card : ready ? Palette.Carrot : Palette.Lantern;
            bool sure = ready && Rabbits.LuckySure(s);
            _luckyLabel.text = sure ? L10n.T("rabbits.lucky.ready") : ready ? L10n.T("rabbits.lucky.near") : L10n.T("rabbits.lucky", lit, Rabbits.LuckyAfter);
            _luckyLabel.color = sure ? Palette.CarrotInk : Quiet;
        }

        Image AddSubTab(RectTransform tabs, string name, float from, string label, bool album, out Text text)
        {
            var rt = UiKit.Rect(name, tabs).Stretch(from, 0, from + 0.5f, 1, 6, 6, 6, 6);
            Image img = rt.AddImage(_catalog.Panel, Palette.Carrot, true);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = img;
            b.onClick.AddListener(() => ShowAlbum(album));
            text = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(label, UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            return img;
        }

        public void ShowAlbum(bool album)
        {
            if (Album == album) return;
            Album = album;
            _signature = null;
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            bool can = s.Clovers >= Rabbits.SummonCost;
            _count.text = L10n.T("rabbits.count", s.Rabbits.Count, AlbumSets.KindsMet(s), AlbumSets.EveryDayKinds);
            _clovers.text = L10n.T("rabbits.clovers.short", s.Clovers);
            _summonLabel.text = can ? L10n.T("rabbits.summon.button", Rabbits.SummonCost) : L10n.T("need.clover", Rabbits.SummonCost - s.Clovers);
            _summonLabel.color = ChunkyButton.InkFor(can);
            Summon.SetEnabled(can);
            RefreshLucky(s);
            _mineTab.color = Album ? Palette.Alpha(Palette.Carrot, 0f) : Palette.Carrot;
            _albumTab.color = Album ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
            _mineLabel.color = Album ? Quiet : Palette.Card;
            _albumLabel.color = Album ? Palette.OnCarrot : Quiet;

            string signature = Signature(defs, s);
            if (signature == _signature) return;
            _signature = signature;
            Rebuild(defs, s);
        }

        string Signature(IReadOnlyList<StationDef> defs, GameState s)
        {
            var sb = new StringBuilder(Album ? "album|" : "mine|");
            foreach (Rabbit r in s.Rabbits)
                sb.Append(r.Id).Append(':').Append(r.Tier).Append(':').Append(r.Kind).Append(':').Append(r.Level).Append(':').Append(r.Station).Append(':').Append(r.Name).Append(':').Append(r.Affection).Append(':').Append(r.Dye).Append(';');
            sb.Append('|');
            foreach (Item item in s.Items) sb.Append(item.Id).Append('>').Append(item.WornBy).Append(';');
            if (_isFresh != null && Album)
                foreach (string seen in s.SeenKinds) if (_isFresh(seen)) sb.Append('*').Append(seen);
            sb.Append('|').Append(s.SeenKinds.Count).Append('|').Append(s.RestSpots).Append('|').Append(s.AlbumMilestonesClaimed.Count);
            return sb.ToString();
        }

        void Rebuild(IReadOnlyList<StationDef> defs, GameState s)
        {
            foreach (GameObject go in _dynamic)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
            _dynamic.Clear();

            if (Album)
            {
                BuildAlbum(s);
                return;
            }

            for (int tier = 0; tier < Rabbits.TierCount - 1; tier++)
                if (Rabbits.CanMerge(s, tier)) AddMerge(tier, Rabbits.CountOfTier(s, tier));

            if (s.Rabbits.Count > 0) AddRest(s);

            if (s.Rabbits.Count == 0)
            {
                var note = UiKit.Rect("Empty", _content);
                note.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
                note.AddText(L10n.T("rabbits.empty"), UiKit.TextBody, Quiet, TextAnchor.MiddleLeft);
                _dynamic.Add(note.gameObject);
                return;
            }

            var sorted = new List<Rabbit>(s.Rabbits);
            sorted.Sort((a, b) => b.Tier != a.Tier ? b.Tier.CompareTo(a.Tier) : (b.Level != a.Level ? b.Level.CompareTo(a.Level) : a.Id.CompareTo(b.Id)));
            RectTransform row = null;
            for (int i = 0; i < sorted.Count; i++)
            {
                if (i % Columns == 0) row = AddRow("Row " + i / Columns, CellH);
                AddCell(row, i % Columns, defs, s, sorted[i]);
            }
        }

        RectTransform AddRow(string name, float height)
        {
            var row = UiKit.Rect(name, _content);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = height;
            _dynamic.Add(row.gameObject);
            return row;
        }

        static float CellX(int column) => CellW / 2f + column * (CellW + CellGap);

        void AddMerge(int tier, int count)
        {
            var root = UiKit.Rect("Merge " + tier, _content);
            root.AddImage(_catalog.Panel, Palette.Mix(Palette.Tiers[tier + 1], Palette.Paper, 0.5f), true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            Text text = UiKit.Rect("Text", root).Stretch(0, 0, 1, 1, 24, 0, 220, 0)
                .AddText(L10n.T("rabbits.merge", L10n.Rarity(tier), L10n.Rarity(tier + 1), count), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 24;
            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -108, 2, 190, 60);
            UiKit.Secondary(button, _catalog.Panel, L10n.T("rabbits.merge.button"), () => _onMerge(tier));
            _dynamic.Add(root.gameObject);
        }

        void AddRest(GameState s)
        {
            var root = UiKit.Rect("Rest", _content);
            root.AddImage(_catalog.Panel, Palette.Track, true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            int seated = RestSpots.SeatedCount(s);
            string line = seated > 0
                ? L10n.T("rest.row.share", seated, s.RestSpots, Percent(RestSpots.Bonus(s)))
                : L10n.T("rest.row", seated, s.RestSpots);
            Text text = UiKit.Rect("Text", root).Stretch(0, 0, 1, 1, 24, 0, 24, 0)
                .AddText(line, UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 24;
            _dynamic.Add(root.gameObject);
        }

        void AddCell(RectTransform row, int column, IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            long id = r.Id;
            var cell = UiKit.Rect("Rabbit " + r.Id, row).Place(0f, 0.5f, CellX(column), 0, CellW, CellH - 12f);
            Image hit = cell.AddImage(_catalog.Panel, Palette.Card, true);
            hit.raycastTarget = true;
            var button = cell.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            button.onClick.AddListener(() => _onOpen(id));

            RectTransform tile = Portrait(cell, _catalog, s, r, 0, 40, 214);
            if (Rabbits.FullHearts(r)) TenHeartBadge(tile, _catalog);

            bool inTrade = Rabbits.InTrade(defs, r);
            var trade = UiKit.Rect("Trade", cell).Place(0f, 1f, 34, -34, 50, 50);
            trade.AddImage(_catalog.Round, inTrade ? Palette.Lantern : Palette.Mix(Palette.Ink, Palette.Paper, 0.55f));
            Image tradeIcon = UiKit.Rect("Icon", trade).Stretch(0, 0, 1, 1, 10, 10, 10, 10).AddImage(_catalog.TradeIcon(Rabbits.TradeOf(r)), inTrade ? Palette.Ink : Palette.Paper);
            tradeIcon.preserveAspect = true;

            bool working = r.Station >= 0 && r.Station < defs.Count;
            bool seated = !working && RestSpots.IsSeated(s, r);
            var status = seated
                ? UiKit.Rect("Status", cell).Place(1f, 1f, -84, -34, 150, 54)
                : UiKit.Rect("Status", cell).Place(1f, 1f, -34, -34, 54, 54);
            if (seated) status.AddImage(_catalog.Panel, Palette.Paper, true);
            else status.AddImage(_catalog.Round, Palette.Paper);
            if (working && _catalog.Stations[r.Station].Prop != null)
                UiKit.Rect("Icon", status).Stretch(0, 0, 1, 1, 7, 7, 7, 7).AddImage(_catalog.Stations[r.Station].Prop, Color.white).preserveAspect = true;
            else if (seated)
            {
                Text share = UiKit.Rect("Share", status).Stretch(0, 0, 1, 1, 10, 0, 10, 0)
                    .AddText(L10n.T("rest.share.short", Percent(RestSpots.BonusOf(r))), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                share.resizeTextForBestFit = true;
                share.resizeTextMinSize = 14;
                share.resizeTextMaxSize = UiKit.TextSmall;
            }
            else
                UiKit.Rect("Nap", status).Stretch(0, 0, 1, 1).AddText("zz", UiKit.TextSmall, Quiet, TextAnchor.MiddleCenter, FontStyle.Bold);

            Text name = UiKit.Rect("Name", cell).Place(0.5f, 0f, 0, 62, CellW - 24f, 38)
                .AddText(r.Name, UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 18;
            name.resizeTextMaxSize = 28;
            UiKit.Rect("Level", cell).Place(0.5f, 0f, 0, 28, CellW - 24f, 30)
                .AddText(L10n.T("station.level", r.Level) + L10n.LevelStars(r) + L10n.Hearts(r), UiKit.TextSmall, Quiet, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        public static RectTransform TenHeartBadge(RectTransform tile, GameCatalog catalog)
        {
            var badge = UiKit.Rect("TenHearts", tile).Place(0f, 1f, 40, -34, 68, 42);
            badge.AddImage(catalog.Panel, Palette.Blush, true);
            Text text = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1, 4, 0, 4, 0).AddText("♥10", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 14;
            text.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            return badge;
        }

        public static RectTransform Portrait(RectTransform parent, GameCatalog catalog, GameState s, Rabbit r, float x, float y, float size)
        {
            var tile = UiKit.Rect("Tile", parent).Place(0.5f, 0.5f, x, y, size, size);
            tile.AddImage(catalog.Panel, Palette.Tiers[r.Tier], true);
            float inset = Mathf.Max(5f, size * 0.035f);
            UiKit.Rect("Back", tile).Stretch(0, 0, 1, 1, inset, inset, inset, inset).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Color.white, 0.35f), true);
            float art = size - inset * 2f - 6f, foot = inset + 3f;
            UiKit.Rect("Art", tile).Place(0.5f, 0f, 0, foot + art / 2f, art, art).AddImage(catalog.RabbitArt(r), Fur(r)).preserveAspect = true;

            RabbitKind kind = RabbitKinds.Find(Rabbits.KindOf(r));
            float across = (GameCatalog.KindHeadX - 0.5f + kind.HatX) * art * GameCatalog.KindAspect;
            Item hat = Items.Worn(s, r.Id, ItemSlot.Hat), charm = Items.Worn(s, r.Id, ItemSlot.Charm);
            Sprite hatIcon = hat != null ? catalog.ItemIcon(hat.DefId) : null, charmIcon = charm != null ? catalog.ItemIcon(charm.DefId) : null;
            if (hatIcon != null && !kind.NoHat)
            {
                var hatOn = UiKit.Rect("HatOn", tile);
                PlaceHat(hatOn, hatIcon, art, across, foot + (GameCatalog.KindHeadTop + kind.HatY) * art, catalog.ItemHatLift(hat.DefId));
                hatOn.AddImage(hatIcon, Color.white).preserveAspect = true;
            }
            if (charmIcon != null)
                UiKit.Rect("CharmOn", tile).Place(0.5f, 0f, across, foot + (GameCatalog.CharmHeight + kind.HatY) * art, art * GameCatalog.CharmSize, art * GameCatalog.CharmSize)
                    .AddImage(charmIcon, Color.white).preserveAspect = true;
            UiKit.Stars(tile, r.Tier, 0.5f, 0f, 0, inset + 14f, Mathf.RoundToInt(Mathf.Clamp(size * 0.085f, 14f, 22f)));
            return tile;
        }

        public static string Percent(double share)
        {
            return L10n.Number((share * 100d).ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','));
        }

        public static Color Fur(Rabbit r)
        {
            string hex = r == null ? null : Fairs.DyeHex(r.Dye);
            return hex == null ? Color.white : Palette.Mix(Color.white, Palette.Hex(hex), 0.6f);
        }

        public static void PlaceHat(RectTransform rt, Sprite icon, float art, float across, float headLine, float lift)
        {
            float w = art * GameCatalog.HatSize, h = w;
            if (icon != null && icon.rect.width > 0f && icon.rect.height > 0f)
            {
                float ratio = icon.rect.height / icon.rect.width;
                if (ratio <= 1f) h = w * ratio;
                else w = h / ratio;
            }
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(across, headLine + (GameCatalog.HatLift - GameCatalog.HatSize / 2f + lift) * art);
        }

        void BuildAlbum(GameState s)
        {
            if (AlbumSets.HasFullFrame(s)) AddFullFrame();
            for (int tier = 0; tier < Rabbits.TierCount; tier++)
            {
                IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(tier);
                int met = 0;
                foreach (RabbitKind k in kinds) if (s.SeenKinds.Contains(k.Id)) met++;

                var head = UiKit.Rect("Tier " + tier, _content);
                head.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
                head.AddImage(_catalog.Panel, Palette.Mix(Palette.Tiers[tier], Palette.Paper, 0.25f), true);
                UiKit.Rect("Text", head).Stretch(0, 0, 1, 1, 22, 0, 22, 0)
                    .AddText(L10n.T("album.tier", L10n.Rarity(tier), met, kinds.Count), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                _dynamic.Add(head.gameObject);

                foreach (AlbumSet set in AlbumSets.OfTier(tier)) AddSet(s, set);
            }

            IReadOnlyList<RabbitKind> seasonal = RabbitKinds.Special();
            if (seasonal.Count > 0)
            {
                int metSeasonal = 0;
                foreach (RabbitKind k in seasonal) if (s.SeenKinds.Contains(k.Id)) metSeasonal++;
                var head = UiKit.Rect("Season", _content);
                head.gameObject.AddComponent<LayoutElement>().preferredHeight = 58;
                head.AddImage(_catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.35f), true);
                UiKit.Rect("Text", head).Stretch(0, 0, 1, 1, 22, 0, 22, 0)
                    .AddText(L10n.T("album.season", metSeasonal, seasonal.Count), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                _dynamic.Add(head.gameObject);
                RectTransform row = null;
                for (int i = 0; i < seasonal.Count; i++)
                {
                    if (i % Columns == 0) row = AddRow("Kinds season-" + i / Columns, AlbumH);
                    bool met = s.SeenKinds.Contains(seasonal[i].Id);
                    AddKind(row, i % Columns, seasonal[i], met);
                    if (!met)
                    {
                        Transform cell = row.Find("Kind " + seasonal[i].Id);
                        cell.Find("Name").GetComponent<Text>().text = seasonal[i].NewMoonOnly
                            ? L10n.T("album.newmoon.only")
                            : L10n.T("album.season.only", L10n.Season(seasonal[i].Season));
                    }
                }
            }
        }

        void AddSet(GameState s, AlbumSet set)
        {
            int met = AlbumSets.Met(s, set);
            bool done = met == set.Kinds.Count;
            int rows = (set.Kinds.Count + Columns - 1) / Columns;
            var group = UiKit.Rect("Set " + set.Key, _content);
            group.gameObject.AddComponent<LayoutElement>().preferredHeight = SetLineH + rows * AlbumH + SetPad;
            _dynamic.Add(group.gameObject);
            float width = Columns * CellW + (Columns - 1) * CellGap;
            if (done)
            {
                var outline = UiKit.Rect("Outline", group);
                outline.anchorMin = new Vector2(0f, 0f);
                outline.anchorMax = new Vector2(0f, 1f);
                outline.offsetMin = new Vector2(-SetOutline, 0f);
                outline.offsetMax = new Vector2(width + SetOutline, 0f);
                outline.AddImage(_catalog.Panel, Palette.Lantern, true);
                UiKit.Rect("Inside", outline).Stretch(0, 0, 1, 1, 6, 6, 6, 6).AddImage(_catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.72f), true);
            }
            string percent = RabbitGridView.Percent(AlbumSets.BonusPerSet);
            Text line = UiKit.Rect("Line", group).Stretch(0, 1, 0, 1, 14, -SetLineH, -width + 14, 0)
                .AddText(done ? L10n.T("album.set.done", L10n.Trade(set.Trade), percent) : L10n.T("album.set", L10n.Trade(set.Trade), met, set.Kinds.Count, percent),
                    UiKit.TextBody, done ? Palette.Ink : InkFor(met), TextAnchor.MiddleLeft, FontStyle.Bold);
            line.horizontalOverflow = HorizontalWrapMode.Overflow;
            for (int i = 0; i < set.Kinds.Count; i++)
                AddKind(group, i % Columns, set.Kinds[i], s.SeenKinds.Contains(set.Kinds[i].Id), 1f, -(SetLineH + (i / Columns) * AlbumH + AlbumH / 2f));
        }

        static Color InkFor(int met) => met > 0 ? Palette.Ink : Quiet;

        void AddFullFrame()
        {
            var frame = UiKit.Rect("FullFrame", _content);
            frame.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            frame.AddImage(_catalog.Panel, Palette.Lantern, true);
            UiKit.Rect("Inside", frame).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(_catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.6f), true);
            UiKit.Rect("Text", frame).Stretch(0, 0, 1, 1, 24, 0, 24, 0)
                .AddText(L10n.T("album.frame", AlbumSets.EveryDayKinds), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dynamic.Add(frame.gameObject);
        }

        void AddKind(RectTransform row, int column, RabbitKind kind, bool met, float anchorY = 0.5f, float y = 0f)
        {
            string id = kind.Id;
            var cell = UiKit.Rect("Kind " + kind.Id, row).Place(0f, anchorY, CellX(column), y, CellW, AlbumH - 12f);
            Image hit = cell.AddImage(_catalog.Panel, met ? Palette.Card : Palette.Track, true);
            hit.raycastTarget = met;
            if (met)
            {
                var button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                button.onClick.AddListener(() => _onKind(id));
            }
            var tile = UiKit.Rect("Tile", cell).Place(0.5f, 0.5f, 0, 28, 196, 196);
            tile.AddImage(_catalog.Panel, met ? Palette.Tiers[kind.Tier] : Palette.Mix(Palette.Ink, Palette.Paper, 0.6f), true);
            UiKit.Rect("Back", tile).Stretch(0, 0, 1, 1, 6, 6, 6, 6).AddImage(_catalog.Panel, met ? Palette.Mix(Palette.Paper, Color.white, 0.35f) : Palette.Mix(Palette.Paper, Palette.Ink, 0.2f), true);
            Image art = UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 12, 10, 12, 10).AddImage(_catalog.KindSprite(kind.Id), met ? Color.white : Palette.Alpha(Color.black, 0.78f));
            art.preserveAspect = true;
            UiKit.Stars(tile, kind.Tier, 0.5f, 0f, 0, 18, 16);
            Text name = UiKit.Rect("Name", cell).Place(0.5f, 0f, 0, 30, CellW - 20f, 40)
                .AddText(met ? L10n.T("kind." + kind.Id) : "???", UiKit.TextBody, met ? Palette.Ink : Quiet, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 15;
            name.resizeTextMaxSize = 24;

            if (met && _isFresh != null && _isFresh(kind.Id))
            {
                tile.GetComponent<Image>().color = Palette.Lantern;
                var fresh = UiKit.Rect("Fresh", cell).Place(1f, 1f, -52, -28, 92, 40);
                fresh.localRotation = Quaternion.Euler(0, 0, -8);
                fresh.AddImage(_catalog.Panel, Palette.Lantern, true);
                UiKit.Rect("Text", fresh).Stretch(0, 0, 1, 1).AddText(L10n.T("album.fresh"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
        }
    }
}

namespace VuonTra.Game.UI
{
    public static class AlbumBanner
    {
        static GameRunner _runner;
        static Action<AlbumSet> _onSet;
        static Action<int, int> _onMilestone;

        public static void Attach(GardenScreen screen)
        {
            if (_runner != null)
            {
                _runner.AlbumSetCompleted -= _onSet;
                _runner.AlbumMilestoneReached -= _onMilestone;
            }
            _runner = screen.Runner;
            if (_runner == null) return;
            _onSet = set =>
            {
                if (screen == null || screen.Feel == null) return;
                Sfx.Milestone();
                screen.Feel.Moment(SetTitle(set), L10n.T("album.set.banner.sub", L10n.Trade(set.Trade), RabbitGridView.Percent(AlbumSets.BonusPerSet)));
            };
            _onMilestone = (milestone, golden) =>
            {
                if (screen == null || screen.Feel == null) return;
                Sfx.Milestone();
                string sub = milestone == AlbumSets.FrameMilestone ? L10n.T("album.milestone.frame")
                    : golden > 0 ? L10n.T("album.milestone.hat.golden", golden) : L10n.T("album.milestone.hat");
                screen.Feel.Moment(L10n.T("album.milestone.title", milestone), sub);
            };
            _runner.AlbumSetCompleted += _onSet;
            _runner.AlbumMilestoneReached += _onMilestone;
        }

        public static string SetTitle(AlbumSet set) => L10n.T("album.set.banner", L10n.Trade(set.Trade), L10n.Tier(set.Tier));
    }
}

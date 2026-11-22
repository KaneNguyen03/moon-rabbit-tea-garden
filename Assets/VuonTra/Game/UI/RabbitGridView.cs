using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The rabbit tab: a count, a low strip to call a rabbit, and either the player's rabbits as a grid of
    /// portraits or the album of every kind. A cell carries no buttons; tapping it opens the rabbit's own sheet.
    /// </summary>
    public sealed class RabbitGridView
    {
        public const int Columns = 3;
        const float CellW = 318f, CellGap = 15f, CellH = 322f, AlbumH = 290f;

        /// <summary>Secondary text: never paler than this, so it stays readable on cream.</summary>
        public static readonly Color Quiet = Palette.Mix(Palette.Ink, Palette.Paper, 0.34f);

        readonly RectTransform _content;
        readonly GameCatalog _catalog;
        readonly Action _onSummon, _onAuto;
        readonly Action<int> _onMerge;
        readonly Action<long> _onOpen;
        readonly Action<string> _onKind;
        readonly Func<string, bool> _isFresh;
        readonly List<GameObject> _dynamic = new List<GameObject>();
        Text _count, _clovers, _summonLabel;
        Image _mineTab, _albumTab;
        Text _mineLabel, _albumLabel;
        string _signature;

        public ChunkyButton Summon { get; private set; }
        /// <summary>False shows the player's rabbits, true the album of kinds.</summary>
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
            // How many rabbits, how much of the album, and one small button that puts everyone to work.
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
            // Three small rabbits-to-stations bars stand in for "sort everyone out".
            for (int i = 0; i < 3; i++)
                UiKit.Rect("Bar", auto).Place(0.5f, 0.5f, -6 + i * 3, 12 - i * 12, 30 - i * 6, 7).AddImage(_catalog.Panel, Palette.Ink, true);

            var tabs = UiKit.Rect("SubTabs", _content);
            tabs.gameObject.AddComponent<LayoutElement>().preferredHeight = UiKit.MinTouch + 12f; // each tab a full touch target
            UiKit.Rect("Track", tabs).Stretch(0, 0, 1, 1, 0, 2, 0, 2).AddImage(_catalog.Panel, Palette.Track, true);
            _mineTab = AddSubTab(tabs, "Mine", 0f, L10n.T("rabbits.tab.mine"), false, out _mineLabel);
            _albumTab = AddSubTab(tabs, "Album", 0.5f, L10n.T("rabbits.tab.album"), true, out _albumLabel);

            // Calling a rabbit: a low strip, so the rabbits themselves start high on the page.
            var root = UiKit.Rect("Summon", _content);
            root.AddImage(_catalog.Panel, Palette.CardLip, true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 124;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            Image shade = UiKit.Rect("Shade", root).Place(0f, 0.5f, 68, 6, 92, 100).AddImage(_catalog.Rabbit, Palette.Alpha(Palette.Ink, 0.5f));
            shade.preserveAspect = true;
            UiKit.Rect("Mark", root).Place(0f, 0.5f, 68, 8, 60, 60).AddText("?", 44, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _clovers = UiKit.Rect("Clovers", root).Stretch(0, 0, 1, 1, 130, 8, 330, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _clovers.resizeTextForBestFit = true;
            _clovers.resizeTextMinSize = 18;
            _clovers.resizeTextMaxSize = 26;
            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -164, 4, 290, 92);
            Summon = UiKit.Chunky(button, _catalog.Panel, () => _onSummon());
            // The odds, for both ways of calling, right where a rabbit is called.
            var odds = UiKit.Rect("Odds", _content);
            odds.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            Text oddsText = odds.AddText(L10n.T("shop.odds.clover", ShopView.Odds(Rabbits.Odds)) + "\n" + L10n.T("shop.odds.golden", ShopView.Odds(Shop.GoldenOdds)), UiKit.TextSmall, Quiet, TextAnchor.MiddleLeft);
            oddsText.resizeTextForBestFit = true;
            oddsText.resizeTextMinSize = 12;
            oddsText.resizeTextMaxSize = UiKit.TextSmall;
            _summonLabel = UiKit.Rect("Text", Summon.FaceRect).Stretch(0, 0, 1, 1)
                .AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            _signature = null; // the next refresh draws the other page
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            bool can = s.Clovers >= Rabbits.SummonCost;
            _count.text = L10n.T("rabbits.count", s.Rabbits.Count, s.SeenKinds.Count, RabbitKinds.All.Count);
            _clovers.text = L10n.T("rabbits.clovers.short", s.Clovers);
            _summonLabel.text = can ? L10n.T("rabbits.summon.button", Rabbits.SummonCost) : L10n.T("need.clover", Rabbits.SummonCost - s.Clovers);
            _summonLabel.color = ChunkyButton.InkFor(can);
            Summon.SetEnabled(can);
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
            sb.Append('|').Append(s.SeenKinds.Count).Append('|').Append(s.RestSpots);
            return sb.ToString();
        }

        void Rebuild(IReadOnlyList<StationDef> defs, GameState s)
        {
            foreach (GameObject go in _dynamic)
            {
                if (go == null) continue;
                go.SetActive(false); // out of the layout now; destroyed at end of frame in play
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
            _dynamic.Clear();

            if (Album)
            {
                BuildAlbum(s);
                return;
            }

            // Merging is offered only when it is possible, as a slim strip under the call strip.
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

            // Best first: tier, then level, then the order they arrived in.
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
            // Merging is a second thing you can do here; calling a rabbit is the first.
            UiKit.Secondary(button, _catalog.Panel, L10n.T("rabbits.merge.button"), () => _onMerge(tier));
            _dynamic.Add(root.gameObject);
        }

        /// <summary>How many resting rabbits can sit out in the garden. More places come with widening the garden.</summary>
        void AddRest(GameState s)
        {
            var root = UiKit.Rect("Rest", _content);
            root.AddImage(_catalog.Panel, Palette.Track, true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
            int resting = 0;
            foreach (Rabbit r in s.Rabbits) if (r.Station < 0) resting++;
            Text text = UiKit.Rect("Text", root).Stretch(0, 0, 1, 1, 24, 0, 24, 0)
                .AddText(L10n.T("rest.row", Mathf.Min(resting, s.RestSpots), s.RestSpots), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = 24;
            _dynamic.Add(root.gameObject);
        }

        /// <summary>One rabbit: a big portrait framed in its tier's colour, its name, its level, its trade and whether it is at work.</summary>
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

            bool inTrade = Rabbits.InTrade(defs, r);
            var trade = UiKit.Rect("Trade", cell).Place(0f, 1f, 34, -34, 50, 50);
            trade.AddImage(_catalog.Round, inTrade ? Palette.Lantern : Palette.Mix(Palette.Ink, Palette.Paper, 0.55f));
            Image tradeIcon = UiKit.Rect("Icon", trade).Stretch(0, 0, 1, 1, 10, 10, 10, 10).AddImage(_catalog.TradeIcon(Rabbits.TradeOf(r)), inTrade ? Palette.Ink : Palette.Paper);
            tradeIcon.preserveAspect = true;

            // Working shows the station's own thing; resting shows a nap.
            bool working = r.Station >= 0 && r.Station < defs.Count;
            var status = UiKit.Rect("Status", cell).Place(1f, 1f, -34, -34, 54, 54);
            status.AddImage(_catalog.Round, Palette.Paper);
            if (working && _catalog.Stations[r.Station].Prop != null)
                UiKit.Rect("Icon", status).Stretch(0, 0, 1, 1, 7, 7, 7, 7).AddImage(_catalog.Stations[r.Station].Prop, Color.white).preserveAspect = true;
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

        /// <summary>
        /// A rabbit's portrait: a square framed in its tier's colour, cream inside, with what it wears set on
        /// the top of its head and at its neck. Shared by the grid and by the rabbit's own sheet.
        /// </summary>
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

        /// <summary>
        /// The colour a rabbit's drawing is tinted with: white for its own coat, else its dye mixed six parts in ten
        /// over white, so the drawing's shading shows through (#44).
        /// </summary>
        public static Color Fur(Rabbit r)
        {
            if (r == null || r.Dye < 0 || r.Dye >= Shop.Dyes.Length) return Color.white;
            return Palette.Mix(Color.white, Palette.Hex(Shop.Dyes[r.Dye].Hex), 0.6f);
        }

        /// <summary>
        /// Sizes and places a worn hat from the shape of its picture: a wide hat is HatSize of the drawing across and
        /// as tall as its picture makes it, a tall one HatSize high; its foot stands on the line hats sit on, raised by
        /// the hat's own lift. So a wreath or a bow sits on the head while a tall hat rises above it. A square picture
        /// lands exactly where hats always did.
        /// </summary>
        /// <param name="art">The height of the rabbit drawing's box.</param>
        /// <param name="across">Where the middle of the head is, from the box's middle.</param>
        /// <param name="headLine">Where the top of the head is, from the parent's bottom edge.</param>
        /// <param name="lift">The hat kind's own lift, in shares of <paramref name="art"/>.</param>
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

        /// <summary>Every kind, tier by tier. A kind you have met shows itself; one you have not is a shadow with a question.</summary>
        void BuildAlbum(GameState s)
        {
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

                RectTransform row = null;
                for (int i = 0; i < kinds.Count; i++)
                {
                    if (i % Columns == 0) row = AddRow("Kinds " + tier + "-" + i / Columns, AlbumH);
                    AddKind(row, i % Columns, kinds[i], s.SeenKinds.Contains(kinds[i].Id));
                }
            }

            // The seasons' kinds (#8), after the tiers: met ones kept for good, the others marked with their season.
            IReadOnlyList<RabbitKind> seasonal = RabbitKinds.Seasonal();
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
                        cell.Find("Name").GetComponent<Text>().text = L10n.T("album.season.only", L10n.Season(seasonal[i].Season));
                    }
                }
            }
        }

        void AddKind(RectTransform row, int column, RabbitKind kind, bool met)
        {
            string id = kind.Id;
            var cell = UiKit.Rect("Kind " + kind.Id, row).Place(0f, 0.5f, CellX(column), 0, CellW, AlbumH - 12f);
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
            // Unmet: the same drawing with every colour pulled to ink, so only the outline of the rabbit shows.
            Image art = UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 12, 10, 12, 10).AddImage(_catalog.KindSprite(kind.Id), met ? Color.white : Palette.Alpha(Color.black, 0.78f));
            art.preserveAspect = true;
            UiKit.Stars(tile, kind.Tier, 0.5f, 0f, 0, 18, 16);
            Text name = UiKit.Rect("Name", cell).Place(0.5f, 0f, 0, 30, CellW - 20f, 40)
                .AddText(met ? L10n.T("kind." + kind.Id) : "???", UiKit.TextBody, met ? Palette.Ink : Quiet, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 15;
            name.resizeTextMaxSize = 24;

            // A kind just met for the first time is lit until its page has been opened.
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

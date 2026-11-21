using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The rabbit tab: call a rabbit, merge three of a tier, train them, dress them and send
    /// them to work. The list is rebuilt whenever the collection changes.
    /// </summary>
    public sealed class RabbitListView
    {
        sealed class Card
        {
            public long Id;
            public ChunkyButton Level;
            public Text LevelTop, LevelCost;
        }

        readonly RectTransform _content;
        readonly GameCatalog _catalog;
        readonly Action _onSummon, _onAuto;
        readonly Action<int> _onMerge;
        readonly Action<long> _onToggle, _onLevelUp, _onRename;
        readonly Action<long, ItemSlot> _onSlot;
        readonly List<GameObject> _dynamic = new List<GameObject>();
        readonly List<Card> _cards = new List<Card>();
        Text _clovers, _summonLabel;
        string _signature;

        public ChunkyButton Summon { get; private set; }

        public RabbitListView(RectTransform content, GameCatalog catalog, Action onSummon, Action onAuto, Action<int> onMerge,
            Action<long> onToggle, Action<long> onLevelUp, Action<long, ItemSlot> onSlot, Action<long> onRename)
        {
            _onRename = onRename;
            _content = content;
            _catalog = catalog;
            _onSummon = onSummon;
            _onAuto = onAuto;
            _onMerge = onMerge;
            _onToggle = onToggle;
            _onLevelUp = onLevelUp;
            _onSlot = onSlot;
            BuildHeader();
        }

        void BuildHeader()
        {
            var root = UiKit.Rect("Summon", _content);
            root.AddImage(_catalog.Panel, Palette.CardLip, true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 170;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            UiKit.Rect("Title", root).Stretch(0, 0.56f, 1, 1, 36, 0, 340, 22)
                .AddText(L10n.T("rabbits.summon.title"), 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _clovers = UiKit.Rect("Clovers", root).Stretch(0, 0.16f, 1, 0.56f, 36, 0, 340, 6)
                .AddText("", 26, Palette.InkSoft, TextAnchor.UpperLeft);

            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -170, 4, 290, 120);
            Summon = UiKit.Chunky(button, _catalog.Panel, () => _onSummon());
            _summonLabel = UiKit.Rect("Text", Summon.FaceRect).Stretch(0, 0, 1, 1)
                .AddText("", 32, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            bool can = s.Clovers >= Rabbits.SummonCost;
            _clovers.text = L10n.T("rabbits.clovers", s.Clovers);
            _summonLabel.text = L10n.T("rabbits.summon.button", Rabbits.SummonCost);
            _summonLabel.color = can ? Palette.Card : Palette.OffInk;
            Summon.SetEnabled(can);

            string signature = Signature(s);
            if (signature != _signature)
            {
                _signature = signature;
                Rebuild(defs, s);
            }

            // Prices and affordability change every moment, without the list changing.
            foreach (Card card in _cards)
            {
                Rabbit r = Rabbits.Find(s, card.Id);
                if (r == null) continue;
                bool maxed = Rabbits.IsMaxLevel(r);
                BigNum cost = Rabbits.LevelCost(r);
                bool afford = !maxed && s.Coins >= cost;
                card.LevelTop.text = maxed ? L10n.T("rabbits.maxlevel") : L10n.T("rabbits.levelup");
                card.LevelCost.text = maxed ? "—" : cost.Short();
                card.Level.SetEnabled(afford);
                Color ink = afford ? Palette.Card : Palette.OffInk;
                card.LevelTop.color = ink;
                card.LevelCost.color = ink;
            }
        }

        static string Signature(GameState s)
        {
            var sb = new StringBuilder();
            foreach (Rabbit r in s.Rabbits)
                sb.Append(r.Id).Append(':').Append(r.Tier).Append(':').Append(r.Kind).Append(':').Append(r.Level).Append(':').Append(r.Station).Append(':').Append(r.Name).Append(';');
            sb.Append('|');
            foreach (Item item in s.Items) sb.Append(item.Id).Append('>').Append(item.WornBy).Append(';');
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
            _cards.Clear();

            if (s.Rabbits.Count == 0)
            {
                AddNote(L10n.T("rabbits.empty"));
                return;
            }

            for (int tier = 0; tier < Rabbits.TierCount - 1; tier++)
                if (Rabbits.CanMerge(s, tier)) AddMerge(tier, Rabbits.CountOfTier(s, tier));

            if (s.Rabbits.Count > 1) AddAuto();

            var sorted = new List<Rabbit>(s.Rabbits);
            sorted.Sort((a, b) => b.Tier != a.Tier ? b.Tier.CompareTo(a.Tier) : a.Id.CompareTo(b.Id));
            foreach (Rabbit r in sorted) AddCard(defs, s, r);
        }

        void AddNote(string text)
        {
            var rt = UiKit.Rect("Empty", _content);
            rt.gameObject.AddComponent<LayoutElement>().preferredHeight = 90;
            rt.AddText(text, 26, Palette.InkSoft, TextAnchor.MiddleLeft);
            _dynamic.Add(rt.gameObject);
        }

        void AddMerge(int tier, int count)
        {
            var root = UiKit.Rect("Merge " + tier, _content);
            root.AddImage(_catalog.Panel, Palette.Mix(Palette.Tiers[tier + 1], Palette.Paper, 0.55f), true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
            UiKit.Rect("Text", root).Stretch(0, 0, 1, 1, 36, 0, 320, 0)
                .AddText(L10n.T("rabbits.merge", L10n.Tier(tier), L10n.Tier(tier + 1), count),
                    26, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -160, 2, 250, 92);
            ChunkyButton chunky = UiKit.Chunky(button, _catalog.Panel, () => _onMerge(tier));
            UiKit.Rect("Text", chunky.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("rabbits.merge.button"), 32, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dynamic.Add(root.gameObject);
        }

        void AddAuto()
        {
            var root = UiKit.Rect("Auto", _content);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 84;
            var button = UiKit.Rect("Button", root).Place(0.5f, 0.5f, 0, 0, 620, 80);
            Image img = button.AddImage(_catalog.Panel, Palette.Track, true);
            img.raycastTarget = true;
            var b = button.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = img;
            b.onClick.AddListener(() => _onAuto());
            UiKit.Rect("Text", button).Stretch(0, 0, 1, 1).AddText(L10n.T("rabbits.auto"), 26, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dynamic.Add(root.gameObject);
        }

        void AddCard(IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            Color tier = Palette.Tiers[r.Tier];
            long id = r.Id;
            var root = UiKit.Rect("Rabbit " + r.Id, _content);
            root.AddImage(_catalog.Panel, Palette.Mix(tier, Palette.Ink, 0.25f), true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 216;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);

            var tile = UiKit.Rect("Tile", root).Place(0f, 0.5f, 108, 4, 156, 156);
            // The tier colours the frame only; the rabbit stands on cream so no coat sinks into its own tier colour.
            tile.AddImage(_catalog.Panel, tier, true);
            UiKit.Rect("Back", tile).Stretch(0, 0, 1, 1, 6, 6, 6, 6).AddImage(_catalog.Panel, Palette.Mix(Palette.Paper, Color.white, 0.35f), true);
            UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(_catalog.RabbitArt(r), Color.white).preserveAspect = true;
            // What it wears shows on it: the hat between the ears, the charm at the neck.
            Item hat = Items.Worn(s, r.Id, ItemSlot.Hat), charm = Items.Worn(s, r.Id, ItemSlot.Charm);
            Sprite hatIcon = hat != null ? _catalog.ItemIcon(hat.DefId) : null, charmIcon = charm != null ? _catalog.ItemIcon(charm.DefId) : null;
            // The same anchor as in the garden: the hat sits on the top of the head, whatever the ears do.
            const float art = 140f;
            float across = (GameCatalog.KindHeadX - 0.5f) * art * GameCatalog.KindAspect;
            if (hatIcon != null)
                UiKit.Rect("HatOn", tile).Place(0.5f, 0f, across, 8f + GameCatalog.KindHeadTop * art + 17f, 62, 62).AddImage(hatIcon, Color.white).preserveAspect = true;
            if (charmIcon != null)
                UiKit.Rect("CharmOn", tile).Place(0.5f, 0f, across + 1f, 8f + art * 0.37f, 28, 28).AddImage(charmIcon, Color.white).preserveAspect = true;

            var rename = UiKit.Rect("Rename", root).Place(0f, 0.5f, 108, -66, 132, 38); // on the foot of the portrait, clear of the name
            Image renameImage = rename.AddImage(_catalog.Panel, Palette.Track, true);
            renameImage.raycastTarget = true;
            var renameButton = rename.gameObject.AddComponent<Button>();
            renameButton.transition = Selectable.Transition.None;
            renameButton.targetGraphic = renameImage;
            renameButton.onClick.AddListener(() => _onRename(id));
            UiKit.Rect("Text", rename).Stretch(0, 0, 1, 1).AddText(L10n.T("rename.button"), 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.71f, 1, 1, 208, 0, 560, 22)
                .AddText(r.Name, 33, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            // Fixed rows, top to bottom: name, kind plate, level and workplace, trade, and a hint when it is out of its trade.
            var chip = UiKit.Rect("Tier", root).Place(0f, 0.5f, 334, 20, 252, 38);
            chip.AddImage(_catalog.Panel, tier, true);
            UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText(L10n.T("rabbits.tier", L10n.Kind(r), UiKit.Times(Rabbits.WorkMultiplier(defs, s, r))),
                20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            bool working = r.Station >= 0 && r.Station < defs.Count;
            bool inTrade = Rabbits.InTrade(defs, r);
            RabbitTrade trade = Rabbits.TradeOf(r);
            Text where = UiKit.Rect("Where", root).Place(0f, 0.5f, 334, -15, 252, 26)
                .AddText(working ? L10n.T("rabbits.where.working", r.Level, Rabbits.LevelCaps[r.Tier], L10n.Station(defs[r.Station].Id)) : L10n.T("rabbits.where.resting", r.Level, Rabbits.LevelCaps[r.Tier]),
                    20, Palette.InkSoft, TextAnchor.MiddleLeft);
            where.resizeTextForBestFit = true; // one line, however long the station's name
            where.resizeTextMinSize = 13;
            where.resizeTextMaxSize = 20;
            where.horizontalOverflow = HorizontalWrapMode.Wrap;
            where.verticalOverflow = VerticalWrapMode.Truncate;

            // The trade: its picture and name, gold with the bonus when the rabbit is where its trade counts.
            var tradeChip = UiKit.Rect("Trade", root).Place(0f, 0.5f, 312, -48, 208, 32);
            tradeChip.AddImage(_catalog.Panel, inTrade ? Palette.Lantern : Palette.Track, true);
            Image tradeIcon = UiKit.Rect("Icon", tradeChip).Place(0f, 0.5f, 22, 0, 26, 26).AddImage(_catalog.TradeIcon(trade), inTrade ? Palette.Ink : Palette.InkSoft);
            tradeIcon.preserveAspect = true;
            UiKit.Rect("Text", tradeChip).Stretch(0, 0, 1, 1, 40, 0, 8, 0)
                .AddText(L10n.Trade(trade) + (inTrade ? L10n.T("rabbits.intrade", UiKit.Times(Rabbits.TradeBonus)) : ""),
                    19, inTrade ? Palette.Ink : Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);

            if (!inTrade)
            {
                // Out of its trade (or resting): say where it would do best, among the stations that are open.
                List<int> suited = Rabbits.SuitedStations(defs, s, r);
                if (suited.Count > 0)
                {
                    var names = new StringBuilder();
                    for (int k = 0; k < suited.Count && k < 2; k++)
                        names.Append(k > 0 ? ", " : "").Append(L10n.Station(defs[suited[k]].Id));
                    Text hint = UiKit.Rect("Suits", root).Place(0f, 0.5f, 454, -80, 492, 22)
                        .AddText(L10n.T("rabbits.suits", names.ToString()), 17, Palette.InkSoft, TextAnchor.MiddleLeft);
                    hint.resizeTextForBestFit = true;
                    hint.resizeTextMinSize = 12;
                    hint.resizeTextMaxSize = 17;
                }
            }

            // What the rabbit wears: tap a slot to choose.
            AddSlot(root, s, r, ItemSlot.Hat, -492);
            AddSlot(root, s, r, ItemSlot.Charm, -384);

            bool canWork = working || Rabbits.BestStationFor(defs, s, r) >= 0;
            var work = UiKit.Rect("Work", root).Place(1f, 0.5f, -166, 54, 284, 86);
            ChunkyButton workButton = UiKit.Chunky(work, _catalog.Panel, () => _onToggle(id));
            Text workLabel = UiKit.Rect("Text", workButton.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(working ? L10n.T("rabbits.rest") : (canWork ? L10n.T("rabbits.work") : L10n.T("rabbits.full")), 28, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            workButton.SetEnabled(canWork);
            if (!canWork) workLabel.color = Palette.OffInk;
            if (working)
            {
                workButton.Lip.color = Palette.OffLip;
                workButton.Face.color = Palette.Track;
                workLabel.color = Palette.Ink;
            }

            var level = UiKit.Rect("LevelUp", root).Place(1f, 0.5f, -166, -46, 284, 86);
            ChunkyButton levelButton = UiKit.Chunky(level, _catalog.Panel, () => _onLevelUp(id));
            Text top = UiKit.Rect("Top", levelButton.FaceRect).Stretch(0, 0, 0.5f, 1, 14, 0, 0, 0)
                .AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text cost = UiKit.Rect("Cost", levelButton.FaceRect).Stretch(0.5f, 0, 1, 1, 0, 0, 14, 0)
                .AddText("", 30, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _cards.Add(new Card { Id = id, Level = levelButton, LevelTop = top, LevelCost = cost });

            _dynamic.Add(root.gameObject);
        }

        void AddSlot(RectTransform root, GameState s, Rabbit r, ItemSlot slot, float x)
        {
            Item worn = Items.Worn(s, r.Id, slot);
            long id = r.Id;
            var rt = UiKit.Rect(slot == ItemSlot.Hat ? "Hat" : "Charm", root).Place(1f, 0.5f, x, 4, 96, 96);
            Image img = rt.AddImage(_catalog.Round, worn != null ? Palette.Mix(Palette.Tiers[worn.Tier], Palette.Paper, 0.25f) : Palette.Track);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = img;
            button.onClick.AddListener(() => _onSlot(id, slot));
            Sprite icon = worn != null ? _catalog.ItemIcon(worn.DefId) : null;
            if (icon != null)
                UiKit.Rect("Icon", rt).Stretch(0, 0, 1, 1, 14, 14, 14, 14).AddImage(icon, Color.white).preserveAspect = true;
            else
                UiKit.Rect("Glyph", rt).Stretch(0, 0, 1, 1).AddText(worn != null ? ItemPickerView.Glyph(worn) : "+",
                    worn != null ? 40 : 46, worn != null ? Palette.Ink : Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Label", rt).Place(0.5f, 0f, 0, -16, 120, 30)
                .AddText(ItemPickerView.SlotName(slot), 19, Palette.InkSoft, TextAnchor.MiddleCenter);
        }
    }
}

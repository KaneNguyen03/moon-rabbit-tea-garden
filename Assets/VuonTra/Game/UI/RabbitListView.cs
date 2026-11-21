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
        readonly Action<long> _onToggle, _onLevelUp;
        readonly Action<long, ItemSlot> _onSlot;
        readonly List<GameObject> _dynamic = new List<GameObject>();
        readonly List<Card> _cards = new List<Card>();
        Text _clovers, _summonLabel;
        string _signature;

        public ChunkyButton Summon { get; private set; }

        public RabbitListView(RectTransform content, GameCatalog catalog, Action onSummon, Action onAuto, Action<int> onMerge,
            Action<long> onToggle, Action<long> onLevelUp, Action<long, ItemSlot> onSlot)
        {
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
                .AddText("Gọi bé thỏ mới", 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
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
            _clovers.text = "Bạn có " + s.Clovers + " cỏ bốn lá. Nhiệm vụ cho thêm cỏ.";
            _summonLabel.text = "Gọi · " + Rabbits.SummonCost + " cỏ";
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
                card.LevelTop.text = maxed ? "Cấp tối đa" : "Lên cấp";
                card.LevelCost.text = maxed ? "—" : cost.ToShortString();
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
                sb.Append(r.Id).Append(':').Append(r.Tier).Append(':').Append(r.Level).Append(':').Append(r.Station).Append(';');
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
                AddNote("Chưa có bé thỏ nào. Làm nhiệm vụ để lấy cỏ bốn lá rồi gọi bé đầu tiên nhé.");
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
                .AddText("Ghép 3 bé " + Palette.TierNames[tier] + " thành 1 bé " + Palette.TierNames[tier + 1] + " (đang có " + count + ")",
                    26, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -160, 2, 250, 92);
            ChunkyButton chunky = UiKit.Chunky(button, _catalog.Panel, () => _onMerge(tier));
            UiKit.Rect("Text", chunky.FaceRect).Stretch(0, 0, 1, 1).AddText("Ghép", 32, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            UiKit.Rect("Text", button).Stretch(0, 0, 1, 1).AddText("Xếp tự động: bé giỏi nhất vào trạm lời nhất", 26, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            tile.AddImage(_catalog.Panel, Palette.Mix(tier, Palette.Paper, 0.35f), true);
            UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(_catalog.RabbitFor(r.Tier), Color.white).preserveAspect = true;

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.64f, 1, 1, 208, 0, 560, 26)
                .AddText(r.Name, 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var chip = UiKit.Rect("Tier", root).Place(0f, 0.5f, 318, -6, 220, 44);
            chip.AddImage(_catalog.Panel, tier, true);
            UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText(Palette.TierNames[r.Tier] + " ×" + UiKit.Times(Rabbits.Multiplier(s, r)),
                23, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            bool working = r.Station >= 0 && r.Station < defs.Count;
            UiKit.Rect("Where", root).Band(208, 548, 22, 56)
                .AddText("Cấp " + r.Level + "/" + Rabbits.LevelCaps[r.Tier] + " · " + (working ? "ở " + defs[r.Station].Name : "đang nghỉ"),
                    22, Palette.InkSoft, TextAnchor.MiddleLeft);

            // What the rabbit wears: tap a slot to choose.
            AddSlot(root, s, r, ItemSlot.Hat, -492);
            AddSlot(root, s, r, ItemSlot.Charm, -384);

            bool canWork = working || Rabbits.BestStationFor(defs, s, r) >= 0;
            var work = UiKit.Rect("Work", root).Place(1f, 0.5f, -166, 54, 284, 86);
            ChunkyButton workButton = UiKit.Chunky(work, _catalog.Panel, () => _onToggle(id));
            Text workLabel = UiKit.Rect("Text", workButton.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(working ? "Cho nghỉ" : (canWork ? "Vào làm" : "Hết chỗ"), 28, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            UiKit.Rect("Glyph", rt).Stretch(0, 0, 1, 1).AddText(worn != null ? ItemPickerView.Glyph(worn) : "+",
                worn != null ? 40 : 46, worn != null ? Palette.Ink : Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Label", rt).Place(0.5f, 0f, 0, -16, 120, 30)
                .AddText(slot == ItemSlot.Hat ? "mũ" : "phụ kiện", 19, Palette.InkSoft, TextAnchor.MiddleCenter);
        }
    }
}

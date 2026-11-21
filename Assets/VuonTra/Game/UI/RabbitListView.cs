using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The rabbit tab: call a rabbit, merge three of a tier, and send rabbits to work.
    /// The list is rebuilt whenever the collection changes.
    /// </summary>
    public sealed class RabbitListView
    {
        readonly RectTransform _content;
        readonly GameCatalog _catalog;
        readonly Action _onSummon, _onAuto;
        readonly Action<int> _onMerge;
        readonly Action<long> _onToggle;
        readonly List<GameObject> _dynamic = new List<GameObject>();
        Text _clovers, _summonLabel;
        string _signature;

        public ChunkyButton Summon { get; private set; }

        public RabbitListView(RectTransform content, GameCatalog catalog, Action onSummon, Action onAuto, Action<int> onMerge, Action<long> onToggle)
        {
            _content = content;
            _catalog = catalog;
            _onSummon = onSummon;
            _onAuto = onAuto;
            _onMerge = onMerge;
            _onToggle = onToggle;
            BuildHeader();
        }

        void BuildHeader()
        {
            var root = UiKit.Rect("Summon", _content);
            root.AddImage(_catalog.Panel, Palette.CardLip, true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 190;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            UiKit.Rect("Title", root).Stretch(0, 0.6f, 1, 1, 36, 0, 340, 22)
                .AddText("Gọi bé thỏ mới", 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _clovers = UiKit.Rect("Clovers", root).Stretch(0, 0.2f, 1, 0.6f, 36, 0, 340, 6)
                .AddText("", 26, Palette.InkSoft, TextAnchor.UpperLeft);

            var button = UiKit.Rect("Button", root).Place(1f, 0.5f, -170, 4, 290, 132);
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
            if (signature == _signature) return;
            _signature = signature;
            Rebuild(defs, s);
        }

        static string Signature(GameState s)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < s.Rabbits.Count; i++)
                sb.Append(s.Rabbits[i].Id).Append(':').Append(s.Rabbits[i].Tier).Append(':').Append(s.Rabbits[i].Station).Append(';');
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
            var root = UiKit.Rect("Rabbit " + r.Id, _content);
            root.AddImage(_catalog.Panel, Palette.Mix(tier, Palette.Ink, 0.25f), true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 176;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);

            var tile = UiKit.Rect("Tile", root).Place(0f, 0.5f, 96, 4, 132, 132);
            tile.AddImage(_catalog.Panel, Palette.Mix(tier, Palette.Paper, 0.35f), true);
            UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(_catalog.RabbitFor(r.Tier), Color.white).preserveAspect = true;

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.58f, 1, 1, 184, 4, 300, 20)
                .AddText(r.Name, 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var chip = UiKit.Rect("Tier", root).Place(0f, 0f, 276, 62, 184, 44);
            chip.AddImage(_catalog.Panel, tier, true);
            UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText(Palette.TierNames[r.Tier] + " ×" + UiKit.Times(Rabbits.Multipliers[r.Tier]),
                23, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            bool working = r.Station >= 0 && r.Station < defs.Count;
            UiKit.Rect("Where", root).Band(384, 300, 40, 44)
                .AddText(working ? "ở " + defs[r.Station].Name : "đang nghỉ", 24, Palette.InkSoft, TextAnchor.MiddleLeft);

            bool canWork = working || Rabbits.BestStationFor(defs, s, r) >= 0;
            long id = r.Id;
            var button = UiKit.Rect("Work", root).Place(1f, 0.5f, -150, 4, 244, 112);
            ChunkyButton chunky = UiKit.Chunky(button, _catalog.Panel, () => _onToggle(id));
            Text label = UiKit.Rect("Text", chunky.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(working ? "Cho nghỉ" : (canWork ? "Vào làm" : "Hết chỗ"), 30, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            chunky.SetEnabled(canWork);
            if (!canWork) label.color = Palette.OffInk;
            if (working)
            {
                chunky.Lip.color = Palette.OffLip;
                chunky.Face.color = Palette.Track;
                label.color = Palette.Ink;
            }
            _dynamic.Add(root.gameObject);
        }
    }
}

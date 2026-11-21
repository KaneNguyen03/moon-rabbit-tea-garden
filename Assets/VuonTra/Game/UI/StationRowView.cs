using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>One card in the upgrade list: what the station earns and the button to raise it.</summary>
    public sealed class StationRowView
    {
        readonly int _index;
        Text _level, _sub, _buyTop, _buyCost;
        Image _levelChip, _face;
        RectTransform _milestoneFill;
        float _shownMilestone;

        public ChunkyButton Buy { get; private set; }

        public StationRowView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onBuy)
        {
            // The card is a lip with a lighter face on top, like the buttons.
            var root = UiKit.Rect("Row " + data.Id, parent);
            root.AddImage(catalog.Panel, Palette.CardLip, true);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 200;
            layout.minHeight = 200;
            var face = UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0);
            _face = face.AddImage(catalog.Panel, Palette.Card, true);

            var icon = UiKit.Rect("Icon", root).Place(0f, 0.5f, 100, 4, 136, 136);
            icon.AddImage(catalog.Panel, Palette.Mix(data.Accent, Palette.Paper, 0.62f), true);
            UiKit.Rect("Prop", icon).Stretch(0, 0, 1, 1, 12, 12, 12, 12).AddImage(data.Prop, Color.white).preserveAspect = true;

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.62f, 1, 1, 190, 4, 480, 18)
                .AddText(data.DisplayName, 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var chip = UiKit.Rect("Level", root).Place(1f, 1f, -392, -52, 152, 46);
            _levelChip = chip.AddImage(catalog.Panel, Palette.Moss, true);
            _level = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            _sub = UiKit.Rect("Sub", root).Stretch(0, 0.25f, 1, 0.62f, 190, 0, 330, 10)
                .AddText("", 25, Palette.InkSoft, TextAnchor.UpperLeft);

            var track = UiKit.Rect("Milestone", root).Band(190, 330, 30, 12);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _milestoneFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _milestoneFill.AddImage(catalog.Panel, Palette.Moss, true);

            var buy = UiKit.Rect("Buy", root).Place(1f, 0.5f, -166, 4, 284, 132);
            Buy = UiKit.Chunky(buy, catalog.Panel, () => onBuy(_index));
            _buyTop = UiKit.Rect("Top", Buy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 12)
                .AddText("", 23, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _buyCost = UiKit.Rect("Cost", Buy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 12, 8, 0)
                .AddText("", 38, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <param name="deltaTime">Seconds since the last refresh; negative jumps bars straight to their value.</param>
        public void Refresh(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, float deltaTime)
        {
            StationDef d = defs[_index];
            int level = s.Levels[_index];
            bool locked = level <= 0;
            bool canAfford;
            float milestone;

            if (locked)
            {
                bool previousOpen = Economy.CanUnlock(s, _index);
                BigNum cost = Economy.CostOfNext(d, 0, 1);
                _level.text = "Chưa mở";
                _levelChip.color = Palette.OffLip;
                _sub.text = previousOpen ? Describe(defs, s, d, 1, m) : "Mở trạm phía trên trước";
                _buyTop.text = "Mở trạm";
                _buyCost.text = cost.ToShortString();
                canAfford = previousOpen && s.Coins >= cost;
                milestone = 0f;
            }
            else
            {
                int count = Simulation.BuyCount(defs, s, _index);
                BigNum cost = Economy.CostOfNext(d, level, count);
                int next = Economy.NextMilestone(level);
                int prev = Economy.PreviousMilestone(level);
                _level.text = "Cấp " + level;
                _levelChip.color = Palette.Moss;
                _sub.text = Describe(defs, s, d, level, m) + (next > 0 ? " · ×2 ở cấp " + next : "");
                _buyTop.text = "Nâng +" + count;
                _buyCost.text = cost.ToShortString();
                canAfford = s.Coins >= cost;
                milestone = next > 0 ? (level - prev) / (float)(next - prev) : 1f;
            }

            Buy.SetEnabled(canAfford);
            Color ink = canAfford ? Palette.Card : Palette.OffInk;
            _buyTop.color = ink;
            _buyCost.color = ink;
            _face.color = locked ? Palette.Mix(Palette.Card, Palette.CardLip, 0.45f) : Palette.Card;

            // Bars animate, never snap.
            _shownMilestone = deltaTime < 0f ? milestone : Mathf.Lerp(_shownMilestone, milestone, Mathf.Clamp01(deltaTime * 8f));
            _milestoneFill.anchorMax = new Vector2(Mathf.Clamp01(_shownMilestone), 1f);
        }

        string Describe(IReadOnlyList<StationDef> defs, GameState s, StationDef d, int level, Modifiers m)
        {
            if (d.Kind == StationKind.Brew)
            {
                BigNum income = s.Levels[_index] > 0
                    ? Economy.IncomePerCycle(defs, s, _index)
                    : new BigNum(d.BaseIncome);
                return (income * m.IncomeMultiplier).ToShortString() + " cà rốt / " + UiKit.Seconds(d.CycleSeconds / m.SpeedMultiplier) + " giây";
            }

            var names = new StringBuilder();
            for (int i = 0; i < defs.Count; i++)
            {
                if (!d.Boosts(defs[i].Id)) continue;
                if (names.Length > 0) names.Append(", ");
                names.Append(defs[i].Name);
            }
            return "Tăng giá ×" + UiKit.Times(Economy.IngredientBoost(level)) + " cho " + names;
        }
    }
}

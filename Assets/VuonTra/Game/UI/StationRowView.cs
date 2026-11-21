using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>One row of the upgrade list: what the station earns and the button to raise it.</summary>
    public sealed class StationRowView
    {
        readonly int _index;
        Text _name, _level, _sub, _buyTop, _buyCost;
        Button _buy;
        Image _buyImage, _rowImage;
        RectTransform _milestoneFill;

        public StationRowView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onBuy)
        {
            var root = UiKit.Rect("Row " + data.Id, parent);
            _rowImage = root.AddImage(catalog.Panel, UiKit.Panel2, true);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 190;
            layout.minHeight = 190;

            var icon = UiKit.Rect("Icon", root).Place(0f, 0.5f, 92, 0, 124, 124);
            icon.AddImage(catalog.Panel, UiKit.Night, true);
            UiKit.Rect("Prop", icon).Stretch(0, 0, 1, 1, 10, 10, 10, 10).AddImage(data.Prop, Color.white).preserveAspect = true;

            _name = UiKit.Rect("Name", root).Stretch(0, 0.62f, 1, 1, 176, 0, 320, 14)
                .AddText(data.DisplayName, 36, UiKit.Cream, TextAnchor.LowerLeft, FontStyle.Bold);
            _name.horizontalOverflow = HorizontalWrapMode.Overflow;
            _level = UiKit.Rect("Level", root).Stretch(0, 0.62f, 1, 1, 176, 0, 320, 14)
                .AddText("", 26, UiKit.Lantern, TextAnchor.LowerRight, FontStyle.Bold);
            _sub = UiKit.Rect("Sub", root).Stretch(0, 0.2f, 1, 0.62f, 176, 0, 320, 4)
                .AddText("", 25, UiKit.Muted, TextAnchor.UpperLeft);

            var track = UiKit.Rect("Milestone", root).Stretch(0, 0, 1, 0, 176, 18, 320, 0);
            track.sizeDelta = new Vector2(track.sizeDelta.x, 8);
            track.AddImage(catalog.Panel, UiKit.Night, true);
            _milestoneFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _milestoneFill.AddImage(catalog.Panel, UiKit.Moss, true);

            var buy = UiKit.Rect("Buy", root).Place(1f, 0.5f, -160, 0, 272, 120);
            _buyImage = buy.AddImage(catalog.Panel, UiKit.Lantern, true);
            _buyImage.raycastTarget = true;
            _buy = buy.gameObject.AddComponent<Button>();
            _buy.targetGraphic = _buyImage;
            _buy.onClick.AddListener(() => onBuy(_index));
            _buyTop = UiKit.Rect("Top", buy).Stretch(0, 0.55f, 1, 1, 8, 0, 8, 10)
                .AddText("", 26, UiKit.Ink, TextAnchor.LowerCenter, FontStyle.Bold);
            _buyCost = UiKit.Rect("Cost", buy).Stretch(0, 0, 1, 0.58f, 8, 10, 8, 0)
                .AddText("", 38, UiKit.Ink, TextAnchor.UpperCenter, FontStyle.Bold);
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            StationDef d = defs[_index];
            int level = s.Levels[_index];
            bool locked = level <= 0;
            bool canAfford;

            if (locked)
            {
                bool previousOpen = Economy.CanUnlock(s, _index);
                BigNum cost = Economy.CostOfNext(d, 0, 1);
                _level.text = "Chưa mở";
                _sub.text = previousOpen ? Describe(defs, s, d, 1) : "Mở trạm phía trên trước";
                _buyTop.text = "Mở trạm";
                _buyCost.text = cost.ToShortString();
                canAfford = previousOpen && s.Coins >= cost;
                SetMilestone(0f);
            }
            else
            {
                int count = Simulation.BuyCount(defs, s, _index);
                BigNum cost = Economy.CostOfNext(d, level, count);
                int next = Economy.NextMilestone(level);
                int prev = Economy.PreviousMilestone(level);
                _level.text = "Cấp " + level;
                _sub.text = Describe(defs, s, d, level) + (next > 0 ? " · ×2 ở cấp " + next : "");
                _buyTop.text = "Nâng +" + count;
                _buyCost.text = cost.ToShortString();
                canAfford = s.Coins >= cost;
                SetMilestone(next > 0 ? (level - prev) / (float)(next - prev) : 1f);
            }

            _buy.interactable = canAfford;
            _buyImage.color = canAfford ? UiKit.Lantern : UiKit.Line;
            Color ink = canAfford ? UiKit.Ink : UiKit.Muted;
            _buyTop.color = ink;
            _buyCost.color = ink;
            _rowImage.color = locked ? new Color(UiKit.Panel2.r, UiKit.Panel2.g, UiKit.Panel2.b, 0.6f) : UiKit.Panel2;
        }

        void SetMilestone(float fraction)
        {
            _milestoneFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        string Describe(IReadOnlyList<StationDef> defs, GameState s, StationDef d, int level)
        {
            if (d.Kind == StationKind.Brew)
            {
                BigNum income = s.Levels[_index] > 0
                    ? Economy.IncomePerCycle(defs, s, _index)
                    : new BigNum(d.BaseIncome);
                return income.ToShortString() + " cà rốt / " + UiKit.Seconds(d.CycleSeconds) + " giây";
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

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

        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onBuy, Action<int> onOpen = null)
        {
            // The card is a lip with a lighter face on top, like the buttons.
            var root = UiKit.Rect("Row " + data.Id, parent);
            Image lip = root.AddImage(catalog.Panel, Palette.CardLip, true);
            if (onOpen != null)
            {
                // Touching the card itself (not its button) shows the station in the garden.
                lip.raycastTarget = true;
                var open = root.gameObject.AddComponent<Button>();
                open.transition = Selectable.Transition.None;
                open.targetGraphic = lip;
                open.onClick.AddListener(() => onOpen(_index));
            }
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 200;
            layout.minHeight = 200;
            var face = UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0);
            _face = face.AddImage(catalog.Panel, Palette.Card, true);

            var icon = UiKit.Rect("Icon", root).Place(0f, 0.5f, 100, 4, 136, 136);
            icon.AddImage(catalog.Panel, Palette.Mix(data.Accent, Palette.Paper, 0.62f), true);
            UiKit.Rect("Prop", icon).Stretch(0, 0, 1, 1, 12, 12, 12, 12).AddImage(data.Prop, Color.white).preserveAspect = true;

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.62f, 1, 1, 190, 4, 480, 18)
                .AddText(L10n.Station(data.Id), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var chip = UiKit.Rect("Level", root).Place(1f, 1f, -392, -52, 152, 46);
            _levelChip = chip.AddImage(catalog.Panel, Palette.Moss, true);
            _level = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            _sub = UiKit.Rect("Sub", root).Stretch(0, 0.25f, 1, 0.62f, 190, 0, 330, 10)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperLeft);

            var track = UiKit.Rect("Milestone", root).Band(190, 330, 30, 12);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _milestoneFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _milestoneFill.AddImage(catalog.Panel, Palette.Moss, true);

            var buy = UiKit.Rect("Buy", root).Place(1f, 0.5f, -166, 4, 284, 132);
            Buy = UiKit.Chunky(buy, catalog.Panel, () => onBuy(_index));
            _buyTop = UiKit.Rect("Top", Buy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 12)
                .AddText("", UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _buyCost = UiKit.Rect("Cost", Buy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 12, 8, 0)
                .AddText("", UiKit.TextTitle, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                _level.text = L10n.T("row.locked");
                _levelChip.color = Palette.OffLip;
                _sub.text = previousOpen ? Describe(defs, s, _index, 1, m) : (_index >= s.Plots ? L10n.T("row.needplot") : L10n.T("row.needprev"));
                canAfford = previousOpen && s.Coins >= cost;
                bool shortOnly = previousOpen && !canAfford; // only carrots are missing: say how many
                _buyTop.text = shortOnly ? L10n.T("need.more") : L10n.T("row.open");
                _buyCost.text = shortOnly ? (cost - s.Coins).Short() : cost.Short();
                milestone = 0f;
            }
            else
            {
                int count = Simulation.BuyCount(defs, s, _index);
                BigNum cost = Economy.CostOfNext(d, level, count);
                int next = Economy.NextMilestone(level);
                int prev = Economy.PreviousMilestone(level);
                _level.text = L10n.T("station.level", level);
                _levelChip.color = Palette.Moss;
                _sub.text = Describe(defs, s, _index, level, m) + (next > 0 ? L10n.T("row.milestone", next) : "");
                canAfford = s.Coins >= cost;
                _buyTop.text = canAfford ? L10n.T("row.upgrade", count) : L10n.T("need.more");
                _buyCost.text = canAfford ? cost.Short() : (cost - s.Coins).Short();
                milestone = next > 0 ? (level - prev) / (float)(next - prev) : 1f;
            }

            Buy.SetEnabled(canAfford);
            Color ink = ChunkyButton.InkFor(canAfford);
            _buyTop.color = ink;
            _buyCost.color = ink;
            _face.color = locked ? Palette.Mix(Palette.Card, Palette.CardLip, 0.45f) : Palette.Card;

            // Bars animate, never snap.
            _shownMilestone = deltaTime < 0f ? milestone : Mathf.Lerp(_shownMilestone, milestone, Mathf.Clamp01(deltaTime * 8f));
            _milestoneFill.anchorMax = new Vector2(Mathf.Clamp01(_shownMilestone), 1f);
        }

        /// <summary>One line on what a station does at a level: what it earns, or whose prices it raises.</summary>
        public static string Describe(IReadOnlyList<StationDef> defs, GameState s, int index, int level, Modifiers m)
        {
            StationDef d = defs[index];
            if (d.Kind == StationKind.Brew)
            {
                BigNum income = s.Levels[index] > 0
                    ? Economy.IncomePerCycle(defs, s, index)
                    : new BigNum(d.BaseIncome);
                return L10n.T("row.income", (income * m.IncomeMultiplier).Short(), UiKit.Seconds(d.CycleSeconds / m.SpeedMultiplier));
            }

            var names = new StringBuilder();
            for (int i = 0; i < defs.Count; i++)
            {
                if (!d.Boosts(defs[i].Id)) continue;
                if (names.Length > 0) names.Append(", ");
                names.Append(L10n.Station(defs[i].Id));
            }
            return L10n.T("row.boost", UiKit.Times(Economy.IngredientBoost(level)), names);
        }
    }
}

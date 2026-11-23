using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// One card in the station list: the picture, what the station earns each second as the main number,
    /// how far it is to the next doubling, and the button to raise it.
    /// </summary>
    public sealed class StationRowView
    {
        readonly int _index;
        Text _level, _main, _sub, _milestoneText, _buyTop, _buyCost;
        Image _levelChip, _face;
        RectTransform _milestoneFill;
        GameObject _milestone, _feeds;
        float _shownMilestone;

        public RectTransform Root { get; private set; }
        public ChunkyButton Buy { get; private set; }

        public StationRowView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onBuy, Action<int> onOpen = null)
        {
            // The card is a lip with a lighter face on top, like the buttons.
            Root = UiKit.Rect("Row " + data.Id, parent);
            Image lip = Root.AddImage(catalog.Panel, Palette.CardLip, true);
            if (onOpen != null)
            {
                // Touching the card itself (not its button) shows the station in the garden.
                lip.raycastTarget = true;
                var open = Root.gameObject.AddComponent<Button>();
                open.transition = Selectable.Transition.None;
                open.targetGraphic = lip;
                open.onClick.AddListener(() => onOpen(_index));
            }
            var layout = Root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 214;
            layout.minHeight = 214;
            var face = UiKit.Rect("Card", Root).Stretch(0, 0, 1, 1, 0, 8, 0, 0);
            _face = face.AddImage(catalog.Panel, Palette.Card, true);

            var icon = UiKit.Rect("Icon", Root).Place(0f, 0.5f, 106, 4, 164, 164);
            icon.AddImage(catalog.Panel, Palette.Mix(data.Accent, Palette.Paper, 0.62f), true);
            UiKit.Rect("Prop", icon).Stretch(0, 0, 1, 1, 12, 12, 12, 12).AddImage(data.Prop, Color.white).preserveAspect = true;

            const float left = 212f, right = 330f;
            Text name = UiKit.Rect("Name", Root).Stretch(0, 0.70f, 1, 1, left, 0, 480, 16)
                .AddText(L10n.Station(data.Id), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            // The number that matters: carrots a second, or how much this station raises prices by.
            _main = UiKit.Rect("PerSec", Root).Stretch(0, 0.44f, 1, 0.72f, left, 0, right, 0)
                .AddText("", UiKit.TextTitle, Palette.CarrotLip, TextAnchor.MiddleLeft, FontStyle.Bold);
            _main.horizontalOverflow = HorizontalWrapMode.Overflow;

            _sub = UiKit.Rect("Sub", Root).Stretch(0, 0.26f, 1, 0.46f, left, 0, right, 0)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft);
            if (data.Kind == StationKind.Ingredient)
            {
                // An ingredient station feeds other stations: say which with their own small pictures.
                var feeds = UiKit.Rect("Feeds", Root).Stretch(0, 0.26f, 1, 0.46f, left + 150f, 0, right, 0);
                _feeds = feeds.gameObject;
                int n = 0;
                foreach (StationData fed in data.Boosts)
                {
                    if (fed == null || fed.Prop == null) continue;
                    var tile = UiKit.Rect("Fed " + fed.Id, feeds).Place(0f, 0.5f, 22 + n * 50f, 0, 44, 44);
                    tile.AddImage(catalog.Round, Palette.Mix(fed.Accent, Palette.Paper, 0.62f));
                    UiKit.Rect("Prop", tile).Stretch(0, 0, 1, 1, 5, 5, 5, 5).AddImage(fed.Prop, Color.white).preserveAspect = true;
                    n++;
                }
            }

            var chip = UiKit.Rect("Level", Root).Place(1f, 1f, -392, -52, 152, 46);
            _levelChip = chip.AddImage(catalog.Panel, Palette.Moss, true);
            _level = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            // "Cấp 500 ✦✦✦" is wider than the pill at full size: the words shrink to stay on one line (#73).
            _level.resizeTextForBestFit = true;
            _level.resizeTextMinSize = 14;
            _level.resizeTextMaxSize = _level.fontSize;
            _level.verticalOverflow = VerticalWrapMode.Truncate;

            // The way to the next doubling: a bar that says how many levels are left.
            var track = UiKit.Rect("Milestone", Root).Band(left, right, 22, 34);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _milestone = track.gameObject;
            _milestoneFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _milestoneFill.AddImage(catalog.Panel, Palette.Mix(Palette.Moss, Palette.Paper, 0.35f), true);
            _milestoneText = UiKit.Rect("Text", track).Stretch(0, 0, 1, 1, 14, 0, 14, 0)
                .AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var buy = UiKit.Rect("Buy", Root).Place(1f, 0.5f, -166, 4, 284, 132);
            Buy = UiKit.Chunky(buy, catalog.Panel, () => onBuy(_index));
            _buyTop = UiKit.Rect("Top", Buy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 12)
                .AddText("", UiKit.TextSmall, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _buyCost = UiKit.Rect("Cost", Buy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 12, 8, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <param name="deltaTime">Seconds since the last refresh; negative jumps bars straight to their value.</param>
        public void Refresh(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, float deltaTime)
        {
            StationDef d = defs[_index];
            int level = s.Levels[_index];
            bool locked = level <= 0;
            bool canAfford, next = false;
            float milestone;

            _main.text = MainNumber(defs, s, _index, Math.Max(1, level), m);
            _sub.text = d.Kind == StationKind.Brew
                ? L10n.T("row.income", (PerCycle(defs, s, _index, Math.Max(1, level)) * m.IncomeMultiplier).Short(), UiKit.Seconds(d.CycleSeconds / m.SpeedMultiplier))
                : L10n.T("row.feeds");

            if (locked)
            {
                next = Economy.CanUnlock(defs, s, _index);
                BigNum cost = Economy.CostOfNext(d, 0, 1);
                _level.text = L10n.T("row.locked");
                _levelChip.color = Palette.OffLip;
                if (!next) _sub.text = LockedReason(defs, s, _index);
                canAfford = next && s.Coins >= cost;
                bool shortOnly = next && !canAfford; // only carrots are missing: say how many
                _buyTop.text = shortOnly ? L10n.T("need.more") : L10n.T("row.open");
                _buyCost.text = shortOnly ? (cost - s.Coins).Short() : cost.Short();
                milestone = 0f;
            }
            else
            {
                int count = Simulation.BuyCount(defs, s, _index);
                BigNum cost = Economy.CostOfNext(d, level, count);
                int toNext = Economy.NextMilestone(level);
                int prev = Economy.PreviousMilestone(level);
                _level.text = L10n.StationLevel(level);
                _levelChip.color = Palette.Moss;
                bool capped = count <= 0; // level 500: nothing left to buy
                canAfford = !capped && s.Coins >= cost;
                _buyTop.text = capped ? L10n.T("row.maxlevel") : (canAfford ? L10n.T("row.upgrade", count) : L10n.T("need.more"));
                _buyCost.text = capped ? "" : (canAfford ? cost.Short() : (cost - s.Coins).Short());
                milestone = toNext > 0 ? (level - prev) / (float)(toNext - prev) : 1f;
                _milestoneText.text = toNext > 0 ? L10n.T("row.tonext", toNext - level) : L10n.T("row.topmilestone");
            }
            if (_milestone.activeSelf == locked) _milestone.SetActive(!locked);
            // The fed stations' pictures sit after "feeds"; a row that says something else instead hides them.
            bool feeds = _feeds != null && (!locked || next);
            if (_feeds != null && _feeds.activeSelf != feeds) _feeds.SetActive(feeds);

            Buy.SetEnabled(canAfford);
            Color ink = ChunkyButton.InkFor(canAfford);
            _buyTop.color = ink;
            _buyCost.color = ink;
            _main.color = locked ? Palette.InkSoft : Palette.CarrotLip;
            // The next station to open is lit like an invitation; the ones after it wait in the shade.
            _face.color = !locked ? Palette.Card : (next ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.62f) : Palette.Mix(Palette.Card, Palette.CardLip, 0.45f));

            // Bars animate, never snap.
            _shownMilestone = deltaTime < 0f ? milestone : Mathf.Lerp(_shownMilestone, milestone, Mathf.Clamp01(deltaTime * 8f));
            _milestoneFill.anchorMax = new Vector2(Mathf.Clamp01(_shownMilestone), 1f);
        }

        /// <summary>
        /// Why a closed station cannot be opened yet: the garden level it asks for (#67), then its plot, then the station
        /// before it. Empty when it can be opened.
        /// </summary>
        public static string LockedReason(IReadOnlyList<StationDef> defs, GameState s, int index)
        {
            if (!Economy.HasGardenLevel(defs, s, index)) return L10n.T("row.needlevel", defs[index].NeedGardenLevel);
            if (index >= s.Plots) return L10n.T("row.needplot");
            return Economy.CanUnlock(defs, s, index) ? "" : L10n.T("row.needprev");
        }

        /// <summary>What one cycle pays at a level: the real figure when the station is open, the base figure before.</summary>
        static BigNum PerCycle(IReadOnlyList<StationDef> defs, GameState s, int index, int level)
        {
            return s.Levels[index] > 0 ? Economy.IncomePerCycle(defs, s, index) : new BigNum(defs[index].BaseIncome * level);
        }

        /// <summary>The row's main number: "+3,2K/giây" for a brew station, "×3,4" for an ingredient station.</summary>
        public static string MainNumber(IReadOnlyList<StationDef> defs, GameState s, int index, int level, Modifiers m)
        {
            StationDef d = defs[index];
            if (d.Kind != StationKind.Brew) return "×" + UiKit.Times(Economy.IngredientBoost(d, level));
            BigNum perSecond = PerCycle(defs, s, index, level) / d.CycleSeconds * (m.IncomeMultiplier * m.SpeedMultiplier);
            return L10n.T("persec", perSecond < new BigNum(10) ? UiKit.Seconds(perSecond.ToDouble()) : perSecond.Short());
        }

        /// <summary>One line on what a station does at a level: what it earns, or whose prices it raises.</summary>
        public static string Describe(IReadOnlyList<StationDef> defs, GameState s, int index, int level, Modifiers m)
        {
            StationDef d = defs[index];
            if (d.Kind == StationKind.Brew)
                return L10n.T("row.income", (PerCycle(defs, s, index, level) * m.IncomeMultiplier).Short(), UiKit.Seconds(d.CycleSeconds / m.SpeedMultiplier));

            var names = new StringBuilder();
            for (int i = 0; i < defs.Count; i++)
            {
                if (!d.Boosts(defs[i].Id)) continue;
                if (names.Length > 0) names.Append(", ");
                names.Append(L10n.Station(defs[i].Id));
            }
            return L10n.T("row.boost", UiKit.Times(Economy.IngredientBoost(d, level)), names);
        }
    }
}

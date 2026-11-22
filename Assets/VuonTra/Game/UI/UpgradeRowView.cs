using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>One garden-wide upgrade ("bùa") card.</summary>
    public sealed class UpgradeRowView
    {
        readonly int _index;
        Text _effect, _buyTop, _buyCost;
        readonly List<Image> _dots = new List<Image>();

        public ChunkyButton Buy { get; private set; }

        public UpgradeRowView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, UpgradeData data, Action<int> onBuy)
        {
            var root = UiKit.Rect("Upgrade " + data.Id, parent);
            root.AddImage(catalog.Panel, Palette.CardLip, true);
            var layout = root.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 200;
            layout.minHeight = 200;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Card, true);

            // The drawn charm fills the tile on its own; only a charm with no picture yet gets a coloured disc.
            var icon = UiKit.Rect("Icon", root).Place(0f, 0.5f, 100, 4, 148, 148);
            if (data.Icon != null)
                UiKit.Rect("Art", icon).Stretch(0, 0, 1, 1).AddImage(data.Icon, Color.white).preserveAspect = true;
            else
            {
                icon.AddImage(catalog.Round, Palette.Mix(data.Accent, Palette.Paper, 0.35f));
                UiKit.Rect("Glyph", icon).Stretch(0, 0, 1, 1).AddText(data.Glyph, 54, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.62f, 1, 1, 190, 4, 480, 18)
                .AddText(L10n.T("upgrade." + data.Id + ".name"), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            // One dot per level the charm can reach, lit as they are learnt: the level reads at a glance.
            var dots = UiKit.Rect("Dots", root).Place(1f, 1f, -392, -52, 190, 24);
            float pitch = Mathf.Min(22f, 190f / Mathf.Max(1, data.MaxLevel));
            for (int i = 0; i < data.MaxLevel; i++)
            {
                var dot = UiKit.Rect("Dot " + i, dots).Place(1f, 0.5f, -(data.MaxLevel - 1 - i) * pitch - 8f, 0, 16, 16);
                _dots.Add(dot.AddImage(catalog.Round, Palette.Track));
            }

            Text sub = UiKit.Rect("Sub", root).Stretch(0, 0.36f, 1, 0.62f, 190, 0, 330, 6)
                .AddText(L10n.T("upgrade." + data.Id + ".desc"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            sub.resizeTextForBestFit = true;
            sub.verticalOverflow = VerticalWrapMode.Truncate;
            sub.resizeTextMinSize = 14;
            sub.resizeTextMaxSize = UiKit.TextSmall;
            // What it does now, and what one more level would make it.
            _effect = UiKit.Rect("Effect", root).Stretch(0, 0.08f, 1, 0.36f, 190, 0, 330, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Bold);
            _effect.resizeTextForBestFit = true;
            _effect.verticalOverflow = VerticalWrapMode.Truncate;
            _effect.resizeTextMinSize = 16;
            _effect.resizeTextMaxSize = UiKit.TextBody;

            var buy = UiKit.Rect("Buy", root).Place(1f, 0.5f, -166, 4, 284, 132);
            Buy = UiKit.Chunky(buy, catalog.Panel, () => onBuy(_index));
            _buyTop = UiKit.Rect("Top", Buy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 12)
                .AddText("", UiKit.TextSmall, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _buyCost = UiKit.Rect("Cost", Buy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 12, 8, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>The charm's effect at a level in the player's words: "+0,4 giây mỗi chạm", "×4 cà rốt".</summary>
        public static string EffectText(UpgradeDef u, int level)
        {
            double value = Upgrades.EffectAt(u, level);
            switch (u.Effect)
            {
                case UpgradeEffect.TapSeconds: return L10n.T("upgrade.effect.tap", Decimal(value));
                case UpgradeEffect.Speed: return L10n.T("upgrade.effect.speed", Mathf.RoundToInt((float)(value * 100d)));
                case UpgradeEffect.IncomeMultiplier: return L10n.T("upgrade.effect.income", Decimal(value));
                default: return L10n.T("upgrade.effect.away", Decimal(value));
            }
        }

        /// <summary>A number with at most one decimal, written with the comma Vietnamese uses; English gets its point.</summary>
        static string Decimal(double value) => L10n.Number(value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','));

        public void Refresh(IReadOnlyList<UpgradeDef> upgrades, GameState s)
        {
            UpgradeDef u = upgrades[_index];
            int level = s.UpgradeLevels[_index];
            bool maxed = Upgrades.IsMaxed(u, level);
            BigNum cost = Upgrades.CostOfNext(u, level);
            bool canBuy = !maxed && s.Coins >= cost;

            for (int i = 0; i < _dots.Count; i++) _dots[i].color = i < level ? Palette.Carrot : Palette.Track;
            string now = EffectText(u, level);
            _effect.text = maxed ? L10n.T("upgrade.full", now) : L10n.T("upgrade.next", now, EffectText(u, level + 1));
            _buyTop.text = maxed ? L10n.T("upgrade.maxed") : (canBuy ? L10n.T("upgrade.learn") : L10n.T("need.more"));
            _buyCost.text = maxed ? "—" : (canBuy ? cost.Short() : (cost - s.Coins).Short());
            Buy.SetEnabled(canBuy);
            Color ink = ChunkyButton.InkFor(canBuy);
            _buyTop.color = ink;
            _buyCost.color = ink;
        }
    }
}

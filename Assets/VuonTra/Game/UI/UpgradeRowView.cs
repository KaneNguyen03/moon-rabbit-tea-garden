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
        Text _level, _buyTop, _buyCost;
        Image _levelChip;

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

            var icon = UiKit.Rect("Icon", root).Place(0f, 0.5f, 100, 4, 136, 136);
            icon.AddImage(catalog.Round, Palette.Mix(data.Accent, Palette.Paper, 0.35f));
            if (data.Icon != null)
                UiKit.Rect("Art", icon).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(data.Icon, Color.white).preserveAspect = true;
            else
                UiKit.Rect("Glyph", icon).Stretch(0, 0, 1, 1).AddText(data.Glyph, 54, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            Text name = UiKit.Rect("Name", root).Stretch(0, 0.62f, 1, 1, 190, 4, 480, 18)
                .AddText(L10n.T("upgrade." + data.Id + ".name"), 36, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            name.horizontalOverflow = HorizontalWrapMode.Overflow;

            var chip = UiKit.Rect("Level", root).Place(1f, 1f, -392, -52, 152, 46);
            _levelChip = chip.AddImage(catalog.Panel, Palette.Moss, true);
            _level = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            UiKit.Rect("Sub", root).Stretch(0, 0.2f, 1, 0.62f, 190, 0, 330, 10)
                .AddText(L10n.T("upgrade." + data.Id + ".desc"), 26, Palette.InkSoft, TextAnchor.UpperLeft);

            var buy = UiKit.Rect("Buy", root).Place(1f, 0.5f, -166, 4, 284, 132);
            Buy = UiKit.Chunky(buy, catalog.Panel, () => onBuy(_index));
            _buyTop = UiKit.Rect("Top", Buy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 12)
                .AddText("", 23, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _buyCost = UiKit.Rect("Cost", Buy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 12, 8, 0)
                .AddText("", 38, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        public void Refresh(IReadOnlyList<UpgradeDef> upgrades, GameState s)
        {
            UpgradeDef u = upgrades[_index];
            int level = s.UpgradeLevels[_index];
            bool maxed = Upgrades.IsMaxed(u, level);
            BigNum cost = Upgrades.CostOfNext(u, level);
            bool canBuy = !maxed && s.Coins >= cost;

            _level.text = L10n.T("upgrade.level", level, u.MaxLevel);
            _levelChip.color = level > 0 ? Palette.Moss : Palette.OffLip;
            _buyTop.text = maxed ? L10n.T("upgrade.maxed") : L10n.T("upgrade.learn");
            _buyCost.text = maxed ? "—" : cost.Short();
            Buy.SetEnabled(canBuy);
            Color ink = canBuy ? Palette.Card : Palette.OffInk;
            _buyTop.color = ink;
            _buyCost.color = ink;
        }
    }
}

using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public enum Fanfare { Gift, Chest, Milestone, Purchase }

    public struct Haul
    {
        public BigNum Carrots;
        public int Clovers, Golden, Jars;

        public int Kinds => (Carrots > BigNum.Zero ? 1 : 0) + (Clovers > 0 ? 1 : 0) + (Golden > 0 ? 1 : 0) + (Jars > 0 ? 1 : 0);
    }

    public sealed partial class GardenScreen
    {
        public const int HaulFlyers = 8, ChestBits = 14, GiftBits = 8;
        static readonly Color[] RewardBits = { Palette.Carrot, Palette.Blush, Palette.Moss, Palette.Lantern };

        public void Rewarded(Vector2 from, Haul haul, Fanfare fanfare)
        {
            Cheer(fanfare);
            Feel.BringToFront();
            bool big = fanfare == Fanfare.Chest || fanfare == Fanfare.Milestone;
            if (big) Feel.Burst(from, Runner.Catalog.Round, RewardBits, ChestBits);
            else if (haul.Kinds == 0) Feel.Burst(from, Runner.Catalog.Round, RewardBits, GiftBits, 420f, 26f);
            FlyHaul(from, haul);
        }

        static void Cheer(Fanfare fanfare)
        {
            switch (fanfare)
            {
                case Fanfare.Chest: Sfx.Chest(); Haptics.Medium(); break;
                case Fanfare.Milestone: Sfx.Milestone(); Haptics.Medium(); break;
                case Fanfare.Purchase: Sfx.Sting(); Haptics.Medium(); break;
                default: Sfx.Gift(); Haptics.Light(); break;
            }
        }

        static void PurchaseRefused(PurchaseOutcome outcome)
        {
            if (outcome == PurchaseOutcome.Failed || outcome == PurchaseOutcome.Unavailable || outcome == PurchaseOutcome.Duplicate) Sfx.Deny();
        }

        void FlyHaul(Vector2 from, Haul haul)
        {
            int kinds = haul.Kinds;
            if (kinds == 0) return;
            int share = Mathf.Max(1, HaulFlyers / kinds);
            GameCatalog catalog = Runner.Catalog;
            if (haul.Carrots > BigNum.Zero)
                FlyTo(from, _purse, catalog.Currency, Color.white, share, "+" + haul.Carrots.Short(), Palette.Carrot, () => PopChip(_purse, 0.08f));
            if (haul.Golden > 0)
            {
                if (!_goldenChip.gameObject.activeSelf) _goldenChip.gameObject.SetActive(true);
                FlyTo(from, _goldenChip, catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, Mathf.Min(haul.Golden, share),
                    L10n.T("mail.golden", haul.Golden), Palette.Lantern, () => PopChip(_goldenChip, 0.12f));
            }
            if (haul.Jars > 0)
            {
                RectTransform shelf = MoonJars.Shown(Runner.State) ? _jarChip : _purse;
                FlyTo(from, shelf, catalog.Round, Palette.Lantern, Mathf.Min(haul.Jars, share), L10n.T("mail.jars", haul.Jars), Palette.Lantern, () => PopChip(shelf, 0.12f));
            }
            if (haul.Clovers > 0)
            {
                RectTransform tab = _tabIcons[3].rectTransform;
                FlyTo(from, tab, catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, Mathf.Min(haul.Clovers, share),
                    L10n.T("mail.clovers", haul.Clovers), Palette.Moss, PopCloverTab);
            }
        }

        void FlyTo(Vector2 from, RectTransform chip, Sprite sprite, Color tint, int count, string label, Color labelColor, Action land)
        {
            Vector2 to = Feel.PointOf(chip == _purse ? _currency : chip);
            Feel.Fly(from, to, sprite, tint, count, 44f, () =>
            {
                if (chip == null) return;
                if (chip == _tabIcons[3].rectTransform) Feel.FloatText(to + new Vector2(0, 90), label, labelColor, 34);
                else Feel.FloatBelow(UnderHud(chip), label, labelColor, 34);
                land();
            });
        }

        public const float HudGainLane = 212f + FeelLayer.GainHeight / 2f;
        public const float HudGainFoot = HudGainLane + FeelLayer.GainHeight / 2f + FeelLayer.GainSink * FeelLayer.GainLife;

        Vector2 UnderHud(RectTransform chip) => new Vector2(Feel.PointOf(chip).x, Feel.PointOf(_hud, new Vector2(0f, -HudGainLane)).y);

        void UnderFronts(Transform rt)
        {
            int lowest = int.MaxValue;
            foreach (StationActorView actor in _actors) if (actor.Front != null && actor.Front.parent == rt.parent) lowest = Mathf.Min(lowest, actor.Front.GetSiblingIndex());
            if (lowest == int.MaxValue) return;
            if (rt.GetSiblingIndex() < lowest) lowest--;
            rt.SetSiblingIndex(lowest);
        }

        Vector2 BannerBand()
        {
            float top = _canvas.rect.height - SafeInsets().y - HudGainFoot - FeelLayer.BannerGap;
            float floor = (SheetOpen ? SheetHeight : ProductTray.Root.anchoredPosition.y + TrayView.Height) + FeelLayer.BannerGap;
            if (!SheetOpen) return new Vector2(floor, floor + FeelLayer.BannerHeight);
            if (top - floor >= FeelLayer.BannerHeight * FeelLayer.BannerLeastFit) return new Vector2(floor, Mathf.Min(top, floor + FeelLayer.BannerHeight));
            float bar = SafeBottom() + TabBarGap;
            return new Vector2(bar, bar + Mathf.Max(TabBarHeight, TabLiftY + TabLiftSize / 2f));
        }

        void PopChip(RectTransform chip, float swell)
        {
            if (Motion.Reduced || chip == null) return;
            Tweens.Add(Motion.Tap, t => { if (chip != null) chip.localScale = Vector3.one * (1f + swell * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (chip != null) chip.localScale = Vector3.one; });
        }

        void PopCloverTab()
        {
            if (Motion.Reduced) return;
            Tweens.Add(0.22f, t => _tabPop[3] = 0.14f * Ease.Pop(t), Ease.Linear, 0f, () => _tabPop[3] = 0f);
        }
    }
}

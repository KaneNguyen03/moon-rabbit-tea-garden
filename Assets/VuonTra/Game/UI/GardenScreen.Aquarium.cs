using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        const float TankPropWidth = 130f, TankPropHeight = 100f;

        public AquariumView TankSheet { get; private set; }
        public AquariumSetView TankCard { get; private set; }
        RectTransform _tankProp;
        GameObject _tankFresh;
        readonly Image[] _tankFish = new Image[Aquarium.Kinds];

        public static Vector2 TankSpot => CellToWorld(GardenGrid.TankTile) + new Vector2(35f, 32f);

        public RectTransform TankProp => _tankProp;
        public bool TankFreshShown => _tankFresh != null && _tankFresh.activeSelf;
        bool TankPanelsOpen => TankSheet != null && (TankSheet.IsOpen || TankCard.IsOpen);

        void BuildTankProp(GameCatalog catalog, List<KeyValuePair<float, Transform>> standing)
        {
            Vector2 at = TankSpot;
            _tankProp = UiKit.Rect("Tank", _street).Place(0f, 0f, at.x, at.y + TankPropHeight / 2f, TankPropWidth, TankPropHeight);
            Image hit = _tankProp.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = _tankProp.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            button.onClick.AddListener(OpenTank);
            RectTransform water = AquariumView.DrawTank(UiKit.Rect("Art", _tankProp).Place(0.5f, 0.5f, 0, 0, TankPropWidth, TankPropHeight), catalog);
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                _tankFish[k] = AquariumView.FishArt(UiKit.Rect("Fish " + k, water).Place(0.5f, 0.5f, -40f + 16f * k, k % 2 == 0 ? 10f : -10f, 30, 22), catalog, k);
                _tankFish[k].raycastTarget = false;
            }
            var fresh = UiKit.Rect("Fresh", _tankProp).Place(1f, 1f, -6, -2, 64, 32);
            fresh.AddImage(catalog.Panel, Palette.CarrotLip, true).raycastTarget = false;
            Text freshText = UiKit.Rect("Text", fresh).Stretch(0, 0, 1, 1, 4, 0, 4, 0).AddText(L10n.T("tank.new"), 18, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            freshText.resizeTextForBestFit = true;
            freshText.resizeTextMinSize = 12;
            freshText.resizeTextMaxSize = 18;
            _tankFresh = fresh.gameObject;
            _tankFresh.SetActive(false);
            _tankProp.gameObject.SetActive(false);
            standing.Add(new KeyValuePair<float, Transform>(at.y, _tankProp));
        }

        void BuildTankPanels(GameCatalog catalog)
        {
            TankSheet = new AquariumView(_canvas, catalog, Tweens, Runner);
            TankCard = new AquariumSetView(_canvas, catalog, Tweens);
            FishingCard.Released = OnFishReleased;
        }

        void RefreshTank()
        {
            if (_tankProp == null || TankSheet == null) return;
            GameState s = Runner.State;
            bool open = Runner.TankOpen;
            if (_tankProp.gameObject.activeSelf != open) _tankProp.gameObject.SetActive(open);
            if (!open) return;
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                bool shown = Aquarium.Released(s, k) > 0;
                if (_tankFish[k].gameObject.activeSelf != shown) _tankFish[k].gameObject.SetActive(shown);
            }
            bool fresh = Aquarium.AnyFresh(s) && !TankSheet.IsOpen;
            if (_tankFresh.activeSelf != fresh) _tankFresh.SetActive(fresh);
            if (TankSheet.IsOpen) TankSheet.Draw();
            if (!TankCard.IsOpen && Runner.TankCardWaiting && OfferCardFree)
            {
                Runner.SeeTankCard();
                Sfx.Milestone();
                Haptics.Medium();
                TankCard.Open(s);
            }
        }

        public void OpenTank()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || LayoutMode || !Runner.TankOpen) return;
            if (TankSheet.IsOpen || FishingCard.IsOpen || Pound.IsOpen || Basket.IsOpen || Jar.IsOpen) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            TankSheet.Open();
            Refresh(-1f);
        }

        void OnFishReleased(ReleaseResult result)
        {
            Refresh(-1f);
            GameCatalog catalog = Runner.Catalog;
            Vector2 from = Feel.PointOf(FishingCard.IconOf(result.Fish));
            RectTransform tank = FishingCard.TankSpot;
            Vector2 to = Feel.PointOf(tank);
            Sfx.Sink();
            Feel.BringToFront();
            Sprite art = catalog.FishSprite(result.Fish);
            string gain = result.Milestones > 0 ? L10n.T("tank.float", AquariumView.Percent(result.Bonus)) : null;
            Feel.Fly(from, to, art != null ? art : catalog.Round, art != null ? Color.white : Palette.Blush, 1, 64f, () =>
            {
                if (tank == null) return;
                if (gain != null) Feel.FloatText(to + new Vector2(0f, 70f), gain, Palette.Lantern, 36);
            });
            var haul = new Haul { Golden = result.Golden, Jars = result.Jars, Clovers = result.Clovers };
            if (result.Milestones > 0 || result.FullSet) Rewarded(to, haul, Fanfare.Milestone);
            else Haptics.Light();
        }
    }
}

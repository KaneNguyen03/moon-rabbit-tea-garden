using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public const float TrunkScale = 0.5f, TrunkGap = 20f;

        public TrunkView TrunkCard { get; private set; }
        RectTransform _trunkProp;
        Text _trunkTag;
        readonly ShownText _trunkShown = new ShownText();
        Image _trunkTagPlate;
        WorldLabel _trunkLabel;

        public RectTransform TrunkProp => _trunkProp;
        public string TrunkTag => _trunkTag != null ? _trunkTag.text : null;

        void BuildTrunk() => TrunkCard = new TrunkView(_canvas, Runner, Tweens, OnBuyFromTrunk);

        void BuildTrunkProp(GameCatalog catalog, Vector2 shopAt, float shopWidth, float shopHeight, List<KeyValuePair<float, Transform>> standing)
        {
            float height = Mathf.Max(shopHeight * TrunkScale, UiKit.MinTouch), width = Mathf.Max(shopWidth * TrunkScale, UiKit.MinTouch);
            Vector2 at = new Vector2(shopAt.x - shopWidth / 2f - width / 2f - TrunkGap, shopAt.y - 12f);
            _trunkProp = UiKit.Rect("Trunk", _street).Place(0f, 0f, at.x, at.y + height / 2f, width, height);
            Image art = TrunkView.TrunkArt(_trunkProp, catalog);
            art.raycastTarget = true;
            var button = _trunkProp.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = art;
            button.onClick.AddListener(() => { Sfx.Click(); OpenTrunk(); });
            var tag = UiKit.Rect("Tag", _trunkProp).Place(0.5f, 0f, -20, -20, 200, 40);
            _trunkTagPlate = tag.AddImage(catalog.Panel, Palette.Paper, true);
            _trunkTag = UiKit.Rect("Text", tag).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", 18, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _trunkTag.resizeTextForBestFit = true;
            _trunkTag.resizeTextMinSize = 12;
            _trunkTag.resizeTextMaxSize = 18;
            _trunkLabel = AddWorldLabel(tag);
            standing.Add(new KeyValuePair<float, Transform>(at.y, _trunkProp));
        }

        void RefreshTrunk()
        {
            if (_trunkTag != null && Runner.State != null)
            {
                long clock = Runner.LocalClock();
                bool open = Trunk.IsOpen(Runner.State, clock);
                if (open) _trunkShown.Line(_trunkTag, "trunk.tag.open");
                else _trunkShown.Line(_trunkTag, "trunk.closed", Trunk.NextHour(Runner.State, clock));
                _trunkTagPlate.color = open ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.45f) : Palette.Paper;
                _trunkLabel.Always = open;
                ShowLabel(_trunkLabel);
            }
            TrunkCard?.Refresh();
        }

        public void OpenTrunk()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || TrunkCard == null) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            TrunkCard.Open(Runner.OpenTrunk());
        }

        static string NoteOf(TrunkBuy buy)
        {
            switch (buy.Offer.Kind)
            {
                case TrunkKind.Hat: return L10n.T("float.hat", L10n.ItemName(buy.Hat));
                case TrunkKind.Jars: return buy.JarClovers <= 0 ? L10n.T("mail.jars", buy.Jars) : buy.Jars <= 0 ? L10n.T("mail.clovers", buy.JarClovers) : L10n.T("mail.jars", buy.Jars) + " " + L10n.T("mail.clovers", buy.JarClovers);
                case TrunkKind.Clovers: return L10n.T("mail.clovers", buy.Offer.Amount);
                case TrunkKind.Lantern: return L10n.T("trunk.float.lantern");
                default: return L10n.T("trunk.float.bought");
            }
        }

        void OnBuyFromTrunk(int slot)
        {
            TrunkBuy buy = Runner.BuyFromTrunk(slot);
            if (!buy.Ok)
            {
                Sfx.Deny();
                return;
            }
            TrunkCard.ShowNote(NoteOf(buy));
            Refresh(-1f);
            RectTransform tile = TrunkCard.TileOf(slot);
            PopChip(tile, 0.08f);
            Rewarded(Feel.PointOf(tile), new Haul { Jars = buy.Jars, Clovers = buy.Offer.Kind == TrunkKind.Clovers ? buy.Offer.Amount : buy.JarClovers }, Fanfare.Chest);
            if (buy.Rabbit == null) return;
            TrunkCard.Close();
            Rabbit shown = buy.Second != null && buy.Second.Tier > buy.Rabbit.Tier ? buy.Second : buy.Rabbit;
            Sfx.Summon(shown.Tier);
            Welcome(shown);
            if (buy.Second != null) Feel.FloatText(new Vector2(0, 420), L10n.T("trunk.float.two", buy.Rabbit.Name, buy.Second.Name), Palette.Lantern, 40);
        }
    }
}

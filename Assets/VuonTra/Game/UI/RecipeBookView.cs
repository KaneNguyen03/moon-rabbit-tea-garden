using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class RecipeBookView
    {
        const float CardHeight = 1500f;
        const float RowHeight = 390f;
        const float IconSize = 118f;

        sealed class Cell
        {
            public DishIcon Icon;
            public Text Name, Level;
            public ChunkyButton Raise;
            public Text RaiseText;
        }

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Text _leaves;
        readonly Action<int, int> _onRaise;
        readonly List<Cell[]> _cells = new List<Cell[]>();
        readonly List<string> _ids = new List<string>();
        string _signature;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Card => _card;

        public RecipeBookView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<int, int> onRaise)
        {
            _tweens = tweens;
            _onRaise = onRaise;

            var overlay = UiKit.Rect("RecipeBook", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrimButton = _root.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 980, CardHeight);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 40, -110, 130, 20)
                .AddText(L10n.T("recipes.book.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold).FitOneLine(810, 24);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -66, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _leaves = UiKit.Rect("Leaves", _card).Stretch(0, 1, 1, 1, 40, -176, 40, 116)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            _leaves.resizeTextForBestFit = true;
            _leaves.resizeTextMinSize = 14;
            _leaves.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            var scrollRt = UiKit.Rect("Scroll", _card).Stretch(0, 0, 1, 1, 0, 40, 0, 196);
            scrollRt.AddTapCatcher();
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(32, 32, 4, 20);
            list.spacing = 14;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = scrollRt;

            for (int i = 0; i < catalog.Stations.Length; i++) AddStation(content, catalog, i);

            _root.SetActive(false);
        }

        void AddStation(RectTransform content, GameCatalog catalog, int station)
        {
            StationData data = catalog.Stations[station];
            _ids.Add(data.Id);
            var row = UiKit.Rect(data.Id, content);
            row.AddImage(catalog.Panel, Palette.Card, true);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = RowHeight;
            Text name = UiKit.Rect("Name", row).Stretch(0, 1, 1, 1, 28, -62, 28, 8)
                .AddText(L10n.Station(data.Id), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            name.FitOneLine(860, 16);
            var cells = new Cell[Recipes.Dishes];
            for (int d = 0; d < Recipes.Dishes; d++)
            {
                int dish = d;
                float x0 = (float)d / Recipes.Dishes, x1 = (float)(d + 1) / Recipes.Dishes;
                var cell = UiKit.Rect("Dish " + d, row).Stretch(x0, 0, x1, 1, 8, 10, 8, 62);
                var c = new Cell { Icon = DishIcon.Build(cell, catalog, IconSize) };
                c.Icon.Show(data, d);
                c.Icon.Root.anchorMin = c.Icon.Root.anchorMax = new Vector2(0.5f, 1f);
                c.Icon.Root.anchoredPosition = new Vector2(0, -IconSize * 0.5f);
                c.Name = UiKit.Rect("Name", cell).Stretch(0, 1, 1, 1, 4, -IconSize - 62, 4, IconSize + 4)
                    .AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                c.Name.resizeTextForBestFit = true;
                c.Name.resizeTextMinSize = 12;
                c.Name.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                c.Name.verticalOverflow = VerticalWrapMode.Truncate;
                c.Level = UiKit.Rect("Level", cell).Stretch(0, 1, 1, 1, 4, -IconSize - 98, 4, IconSize + 62)
                    .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
                c.Level.resizeTextForBestFit = true;
                c.Level.resizeTextMinSize = 12;
                c.Level.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                c.Level.verticalOverflow = VerticalWrapMode.Truncate;
                var raise = UiKit.Rect("Raise", cell).Place(0.5f, 0f, 0, 34, 270, 64);
                UiKit.Touchable(raise, 270, 64);
                c.Raise = UiKit.Chunky(raise, catalog.Panel, () => _onRaise(station, dish));
                c.RaiseText = UiKit.Rect("Text", c.Raise.FaceRect).Stretch(0, 0, 1, 1, 10, 0, 10, 0)
                    .AddText("", UiKit.TextSmall, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
                c.RaiseText.resizeTextForBestFit = true;
                c.RaiseText.resizeTextMinSize = 12;
                c.RaiseText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                c.RaiseText.verticalOverflow = VerticalWrapMode.Truncate;
                cells[d] = c;
            }
            _cells.Add(cells);
        }

        public void Open(IReadOnlyList<StationDef> defs, GameState s)
        {
            _signature = null;
            _root.SetActive(true);
            Refresh(defs, s);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(Motion.Panel), t => { if (_card != null) _card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!IsOpen) return;
            bool unlocked = Recipes.Unlocked(s);
            var sb = new StringBuilder();
            sb.Append(L10n.Code).Append('|').Append(s.RecipeLeaves).Append('|').Append(unlocked).Append('|');
            for (int i = 0; i < s.Levels.Length; i++) sb.Append(Recipes.DishesOpen(s, i)).Append(s.Levels[i] > 0 ? '+' : '-').Append(',');
            for (int i = 0; i < s.DishLevels.Length; i++) sb.Append(s.DishLevels[i]).Append(',');
            string signature = sb.ToString();
            if (signature == _signature) return;
            _signature = signature;

            _leaves.text = unlocked ? L10n.T("recipes.book.leaves", s.RecipeLeaves) : L10n.T("dish.locked", Recipes.UnlockLevel);
            for (int i = 0; i < _cells.Count && i < defs.Count; i++)
            {
                bool open = s.Levels[i] > 0;
                int dishes = open ? Recipes.DishesOpen(s, i) : 0, sold = Recipes.CurrentDish(s, i);
                for (int d = 0; d < Recipes.Dishes; d++) RefreshCell(_cells[i][d], defs[i], s, i, d, d < dishes, open && d == sold);
            }
        }

        void RefreshCell(Cell c, StationDef def, GameState s, int station, int dish, bool known, bool sold)
        {
            c.Icon.SetHidden(!known);
            c.Name.text = known ? L10n.Dish(def.Id, dish) : L10n.T("recipes.book.hidden");
            c.Name.color = known ? Palette.Ink : Palette.InkSoft;
            int level = Recipes.KnownLevel(s, station, dish);
            if (!known) c.Level.text = dish == 0 ? L10n.T("dish.closed") : L10n.T("recipes.book.opens", Economy.Ranks[dish - 1]);
            else c.Level.text = sold ? L10n.T("recipes.book.selling", level) : L10n.T("recipes.book.level", level);
            c.Level.color = sold ? Palette.MossInk : Palette.InkSoft;
            c.Level.fontStyle = sold ? FontStyle.Bold : FontStyle.Normal;

            bool shown = sold && Recipes.Unlocked(s);
            if (c.Raise.Root.gameObject.activeSelf != shown) c.Raise.Root.gameObject.SetActive(shown);
            if (!shown) return;
            int cost = Recipes.CostOfNext(level);
            bool can = Recipes.CanRaise(s, station, dish);
            c.RaiseText.text = cost > 0 ? L10n.T("recipes.book.raise", cost) : L10n.T("recipes.book.max");
            c.Raise.SetEnabled(can);
            if (can) c.Raise.Face.color = GardenScreen.LeafGold;
            c.RaiseText.color = ChunkyButton.InkFor(can);
        }
    }
}

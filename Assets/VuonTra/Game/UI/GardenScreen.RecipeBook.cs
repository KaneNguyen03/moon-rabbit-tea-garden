using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public RecipeBookView Book { get; private set; }
        public NewDishView NewDishCard { get; private set; }

        void BuildRecipeBook(GameCatalog catalog)
        {
            Book = new RecipeBookView(_canvas, catalog, Tweens, OnRaiseBookDish);
            NewDishCard = new NewDishView(_canvas, catalog, Tweens, OpenBook);
            Image chip = _leafChip.GetComponent<Image>();
            chip.raycastTarget = true;
            UiKit.Touchable(_leafChip, _leafChip.sizeDelta.x, _leafChip.sizeDelta.y);
            var button = _leafChip.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = chip;
            button.onClick.AddListener(() => { Sfx.Click(); OpenBook(); });
        }

        void BuildBookRow(RectTransform list, GameCatalog catalog)
        {
            var row = UiKit.Rect("RecipeBook", list);
            row.AddImage(catalog.Panel, Palette.CardLip, true);
            row.gameObject.AddComponent<LayoutElement>().preferredHeight = 170;
            UiKit.Rect("Card", row).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Mix(LeafGold, Palette.Paper, 0.72f), true);
            UiKit.Rect("Title", row).Stretch(0, 0.5f, 1, 1, 36, 8, 330, 10)
                .AddText(L10n.T("recipes.book.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold).FitOneLine(560, 20);
            Text sub = UiKit.Rect("Sub", row).Stretch(0, 0, 1, 0.5f, 36, 16, 330, 4)
                .AddText(L10n.T("recipes.book.row", Economy.Ranks[0], Economy.Ranks[1]), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 12;
            sub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            sub.verticalOverflow = VerticalWrapMode.Truncate;
            var open = UiKit.Rect("Button", row).Place(1f, 0.5f, -166, 4, 284, UiKit.MinTouch);
            ChunkyButton chunky = UiKit.Chunky(open, catalog.Panel, () => { Sfx.Click(); OpenBook(); });
            chunky.Face.color = LeafGold;
            UiKit.Rect("Text", chunky.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("recipes.book.open"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        void OpenBook()
        {
            if (Book == null) return;
            Book.Open(Runner.Defs, Runner.State);
            Runner.OpenedFeature(Intros.RecipeBook);
        }

        void RefreshRecipeBook(GameState s)
        {
            if (Book == null) return;
            Book.Refresh(Runner.Defs, s);
            if (NewDishCard.IsOpen || Recipes.NewDishes(s) <= 0 || !OfferCardFree) return;
            int station = Recipes.NewestDish(s);
            int dish = Recipes.DishesOpen(s, station) - 1;
            int more = Recipes.NewDishes(s) - 1;
            Runner.SeeNewDishes();
            NewDishCard.Open(Runner.Catalog.Stations[station], dish, more);
            Rewarded(Feel.PointOf(NewDishCard.Card), new Haul(), Fanfare.Milestone);
        }

        void OnRaiseBookDish(int station, int dish)
        {
            if (!Runner.RaiseDish(station, dish))
            {
                Sfx.Deny();
                return;
            }
            DishRaised(station, dish, Feel.PointOf(Book.Card, new Vector2(0f, 300f)));
            Refresh(-1f);
        }
    }
}

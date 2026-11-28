using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class RecipeBookScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
            UiKit.SetLargeText(false);
            Prefs.DeleteKey("text.large");
            Motion.SetReduced(false);
            L10n.SetLocale("vi");
        }

        GameState Open(int gardenLevel = 30, bool start = true)
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.OfferCardsOn = false;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            s.TutorialStep = Tutorial.Steps;
            s.HintSeen = true;
            s.Newcomer.Closed = true;
            s.GardenXp = Garden.XpForLevel(gardenLevel);
            s.Levels[0] = 24;
            s.Levels[1] = 10;
            runner.Init(s);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            if (start) screen.Shell.StartGame();
            return s;
        }

        Text T(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Text>();
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        [Test]
        public void A_Station_Reaching_25_Shows_The_New_Dish_Card_Once_And_Sells_The_New_Dish()
        {
            GameState s = Open();
            screen.Refresh();
            Assert.IsFalse(screen.NewDishCard.IsOpen, "nothing new yet");

            s.Levels[0] = 25;
            screen.Refresh();
            Assert.IsTrue(screen.NewDishCard.IsOpen);
            Assert.AreEqual(L10n.T("dish.new.title"), T("Canvas/NewDish/Card/Title").text);
            Assert.AreEqual(L10n.Dish("am-tra", 1), T("Canvas/NewDish/Card/Name").text);
            Assert.AreEqual(L10n.T("dish.new.sub", L10n.Station("am-tra")), T("Canvas/NewDish/Card/Sub").text);
            Assert.AreEqual("II", T("Canvas/NewDish/Card/Icon/Tag/Numeral").text);
            Assert.IsFalse(root.transform.Find("Canvas/NewDish/Card/More").gameObject.activeSelf);
            Assert.AreEqual(2, Recipes.Seen(s, 0), "seen as it opens");
            Assert.IsFalse(screen.OfferCardFree, "no offer over it");
            Assert.IsTrue(screen.ReviewBusy, "no review ask over it");

            Press("Canvas/NewDish/Card/Ok");
            Assert.IsFalse(screen.NewDishCard.IsOpen);
            screen.Refresh();
            Assert.IsFalse(screen.NewDishCard.IsOpen, "once is once");

            screen.FocusStation(0);
            screen.Refresh();
            Assert.AreEqual(L10n.T("dish.line", L10n.Dish("am-tra", 1), 1), T("Canvas/Card/Dish/Line").text, "the station card sells the new dish");
        }

        [Test]
        public void The_New_Dish_Card_Waits_For_The_Book_The_Title_And_Other_Cards()
        {
            GameState s = Open(Recipes.UnlockLevel - 1, start: false);
            s.Levels[0] = 100;
            s.Levels[1] = 25;
            screen.Refresh();
            Assert.IsFalse(screen.NewDishCard.IsOpen, "the book is closed in week one");
            s.GardenXp = Garden.XpForLevel(Recipes.UnlockLevel);
            screen.Refresh();
            Assert.IsFalse(screen.NewDishCard.IsOpen, "not over the title");
            screen.UpgradeAll.Open(runner.Defs, s);
            screen.Shell.StartGame();
            screen.Refresh();
            Assert.IsFalse(screen.NewDishCard.IsOpen, "not over another card");
            screen.UpgradeAll.Close();
            screen.Refresh();
            Assert.IsTrue(screen.NewDishCard.IsOpen, "the first clear moment");
            Assert.AreEqual(L10n.Dish("am-tra", 2), T("Canvas/NewDish/Card/Name").text, "the highest dish first");
            Assert.AreEqual("III", T("Canvas/NewDish/Card/Icon/Tag/Numeral").text);
            Assert.IsTrue(root.transform.Find("Canvas/NewDish/Card/More").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("dish.new.more", 2), T("Canvas/NewDish/Card/More").text, "the tea's second and the leaf basket's second wait in the book");
            Assert.AreEqual(0, Recipes.NewDishes(s), "all of them seen with one card");

            Press("Canvas/NewDish/Card/Book");
            Assert.IsFalse(screen.NewDishCard.IsOpen);
            Assert.IsTrue(screen.Book.IsOpen, "the card's second button opens the book");
        }

        [Test]
        public void The_Book_Lists_Every_Station_With_Three_Dishes_And_Unopened_Ones_As_Shadows()
        {
            GameState s = Open();
            s.Levels[0] = 100;
            s.Levels[1] = 25;
            Recipes.SeeDishes(s);
            screen.Refresh();
            screen.OpenSheet(0);
            Press("Canvas/Sheet/Panel/Scroll/Content/RecipeBook/Button");
            Assert.IsTrue(screen.Book.IsOpen);
            Transform content = root.transform.Find("Canvas/RecipeBook/Card/Scroll/Content");
            Assert.AreEqual(runner.Defs.Count, content.childCount);
            Assert.AreEqual(L10n.T("recipes.book.title"), T("Canvas/RecipeBook/Card/Title").text);

            string tea = "Canvas/RecipeBook/Card/Scroll/Content/am-tra/";
            Assert.AreEqual(L10n.Station("am-tra"), T(tea + "Name").text);
            for (int d = 0; d < Recipes.Dishes; d++)
            {
                Assert.AreEqual(L10n.Dish("am-tra", d), T(tea + "Dish " + d + "/Name").text);
                Assert.IsFalse(root.transform.Find(tea + "Dish " + d + "/Icon/Shadow").gameObject.activeSelf);
            }
            Assert.AreEqual(L10n.T("recipes.book.selling", 1), T(tea + "Dish 2/Level").text);
            Assert.AreEqual(L10n.T("recipes.book.level", 1), T(tea + "Dish 0/Level").text);
            Assert.AreEqual("II", T(tea + "Dish 1/Icon/Tag/Numeral").text);
            Assert.AreEqual("", T(tea + "Dish 0/Icon/Tag/Numeral").text);

            string leaf = "Canvas/RecipeBook/Card/Scroll/Content/gio-la/";
            Assert.IsFalse(root.transform.Find(leaf + "Dish 1/Icon/Shadow").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find(leaf + "Dish 2/Icon/Shadow").gameObject.activeSelf, "the third dish is a shadow");
            Assert.AreEqual(L10n.T("recipes.book.hidden"), T(leaf + "Dish 2/Name").text, "its name stays hidden");
            Assert.AreEqual(L10n.T("recipes.book.opens", Economy.Ranks[1]), T(leaf + "Dish 2/Level").text);

            string jam = "Canvas/RecipeBook/Card/Scroll/Content/noi-mut/";
            Assert.IsTrue(root.transform.Find(jam + "Dish 0/Icon/Shadow").gameObject.activeSelf, "a station not opened shows only shadows");
            Assert.AreEqual(L10n.T("recipes.book.opens", Economy.Ranks[0]), T(jam + "Dish 1/Level").text);
        }

        bool Floats(string text)
        {
            foreach (Text label in root.GetComponentsInChildren<Text>(false))
                if (label.text == text && label.gameObject.name == "Gain") return true;
            return false;
        }

        [Test]
        public void Raising_A_Dish_To_The_Top_Level_In_The_Book_Plays_The_Reward_With_A_Firm_Buzz_And_A_Gold_Float()
        {
            GameState s = Open();
            s.Levels[0] = 25;
            Recipes.SeeDishes(s);
            Recipes.SetLevel(s, 0, 1, Recipes.MaxLevel - 2);
            s.RecipeLeaves = Recipes.CostOfNext(Recipes.MaxLevel - 2) + Recipes.CostOfNext(Recipes.MaxLevel - 1);
            screen.Refresh();
            screen.Book.Open(runner.Defs, s);
            string raise = "Canvas/RecipeBook/Card/Scroll/Content/am-tra/Dish 1/Raise";
            Press(raise);
            Assert.AreEqual("milestone", Sfx.Last, "a plain level keeps the milestone chime");
            Assert.IsFalse(Floats(L10n.T("dish.top", UiKit.Times(Recipes.StationMultiplier(s, 0)))));
            int buzzes = Haptics.Count;
            Press(raise);
            Assert.IsTrue(Recipes.IsTop(s, 0, 1));
            string top = L10n.T("dish.top", UiKit.Times(Recipes.StationMultiplier(s, 0)));
            Assert.AreEqual(L10n.T("dish.top", "2,5"), top, "the second dish at the top: the station sells at ×2 × 1,25");
            Assert.AreEqual("reward", Sfx.Last, "the top level plays the reward");
            Assert.Greater(Haptics.Count, buzzes);
            Assert.AreEqual(Haptics.MillisecondsOf(HapticStrength.Medium), Haptics.LastMilliseconds, "a firm buzz");
            Assert.IsTrue(Floats(top), "the gold float names the top level");
        }

        [Test]
        public void Raising_A_Dish_To_The_Top_Level_On_The_Station_Card_Plays_The_Same_Moment()
        {
            GameState s = Open();
            Recipes.SetLevel(s, 0, 0, Recipes.MaxLevel - 1);
            s.RecipeLeaves = Recipes.CostOfNext(Recipes.MaxLevel - 1);
            screen.FocusStation(0);
            screen.Refresh();
            int buzzes = Haptics.Count;
            Press("Canvas/Card/Dish/Raise");
            Assert.IsTrue(Recipes.IsTop(s, 0, 0));
            Assert.AreEqual("reward", Sfx.Last);
            Assert.Greater(Haptics.Count, buzzes);
            Assert.AreEqual(Haptics.MillisecondsOf(HapticStrength.Medium), Haptics.LastMilliseconds);
            Assert.IsTrue(Floats(L10n.T("dish.top", UiKit.Times(Recipes.StationMultiplier(s, 0)))));
        }

        [Test]
        public void The_Book_Raises_The_Sold_Dish_With_Leaves()
        {
            GameState s = Open();
            s.Levels[0] = 25;
            Recipes.SeeDishes(s);
            s.RecipeLeaves = 7;
            screen.Refresh();
            screen.Book.Open(runner.Defs, s);
            string tea = "Canvas/RecipeBook/Card/Scroll/Content/am-tra/";
            Assert.IsFalse(root.transform.Find(tea + "Dish 0/Raise").gameObject.activeSelf, "only the sold dish is raised");
            Assert.AreEqual(L10n.T("recipes.book.raise", 5), T(tea + "Dish 1/Raise/Face/Text").text);
            Assert.AreEqual(L10n.T("recipes.book.leaves", 7), T("Canvas/RecipeBook/Card/Leaves").text);
            Press(tea + "Dish 1/Raise");
            Assert.AreEqual(2, s.RecipeLeaves);
            Assert.AreEqual(2, Recipes.DishLevel(s, 0, 1));
            Assert.AreEqual(1, Recipes.DishLevel(s, 0, 0));
            Assert.AreEqual(L10n.T("recipes.book.selling", 2), T(tea + "Dish 1/Level").text);
            Assert.AreEqual(L10n.T("recipes.book.leaves", 2), T("Canvas/RecipeBook/Card/Leaves").text);
            Assert.IsFalse(root.transform.Find(tea + "Dish 1/Raise").GetComponent<Button>().interactable, "20 leaves for the next");
        }

        [Test]
        public void The_Leaf_Chip_Opens_The_Book_And_A_Closed_Book_Says_When_It_Opens()
        {
            GameState s = Open(Recipes.UnlockLevel - 1);
            s.RecipeLeaves = 3;
            screen.Refresh();
            Press("Canvas/Hud/RecipeLeaves");
            Assert.IsTrue(screen.Book.IsOpen);
            Assert.AreEqual(L10n.T("dish.locked", Recipes.UnlockLevel), T("Canvas/RecipeBook/Card/Leaves").text);
            Assert.IsFalse(root.transform.Find("Canvas/RecipeBook/Card/Scroll/Content/am-tra/Dish 0/Raise").gameObject.activeSelf, "nothing to raise before the book opens");
            Press("Canvas/RecipeBook/Card/Close");
            Assert.IsFalse(screen.Book.IsOpen);

            L10n.SetLocale("en");
            screen.RebuildUi();
            screen.Book.Open(runner.Defs, s);
            Assert.AreEqual("Recipe book", T("Canvas/RecipeBook/Card/Title").text);
            Assert.AreEqual(L10n.Dish("am-tra", 0), T("Canvas/RecipeBook/Card/Scroll/Content/am-tra/Dish 0/Name").text);
        }
    }
}

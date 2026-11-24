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
    public class AquariumScreenTests
    {
        const string Pond = "Canvas/Pond/Card";
        const string Tank = "Canvas/Aquarium/Card";

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        readonly long now = 1_800_000_000L;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            UiKit.SetLargeText(false);
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.Levels[1] = 12; s.Levels[2] = 9; s.Levels[3] = 4;
            s.GardenXp = Garden.XpForLevel(5);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            s.Newcomer.Closed = true;
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            runner.FishingClock = () => now;
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            screen.Refresh();
            Step(1.2f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            UiKit.SetLargeText(false);
            PlayerPrefs.DeleteKey("text.large");
            L10n.SetLocale("vi");
        }

        GameState S => runner.State;

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        string TextAt(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Text>().text;
        }

        void OpenPond()
        {
            Find("Canvas/Garden/Content/PondTap").onClick.Invoke();
            screen.Refresh();
            Step(0.3f);
            Assert.IsTrue(screen.FishingCard.IsOpen);
        }

        void ClosePond()
        {
            Find(Pond + "/Close").onClick.Invoke();
            Step(0.4f);
            screen.Refresh();
            Step(0.2f);
        }

        void OpenTank()
        {
            Find("Canvas/Garden/Content/Tank").onClick.Invoke();
            screen.Refresh();
            Step(0.3f);
            Assert.IsTrue(screen.TankSheet.IsOpen, "the tank's sheet is up");
        }

        [Test]
        public void The_Tank_Stands_By_The_Pond_On_Its_Reserved_Tile()
        {
            var tank = (RectTransform)root.transform.Find("Canvas/Garden/Content/Tank");
            Assert.IsTrue(tank.gameObject.activeInHierarchy, "the tank is out as soon as the pond is");
            Vector2 at = tank.anchoredPosition - new Vector2(0f, tank.rect.height / 2f);
            Assert.AreEqual(GardenScreen.TankSpot.x, at.x, 0.5f);
            Assert.AreEqual(GardenScreen.TankSpot.y, at.y, 0.5f);
            Assert.AreEqual(GardenGrid.TankTile, GardenScreen.WorldToCell(tank.anchoredPosition), "the tank's middle is on its tile");
            Assert.IsTrue(GardenGrid.IsReserved(GardenGrid.TankTile));
            Assert.Less(Vector2.Distance(tank.anchoredPosition, GardenScreen.PondSpot), 400f, "next to the pond");
        }

        [Test]
        public void Release_To_Tank_Sits_In_The_Basket_And_Takes_The_Chosen_Fish()
        {
            S.Fish[0] = 2; S.Fish[2] = 1;
            OpenPond();
            Assert.AreEqual(L10n.T("tank.release"), TextAt(Pond + "/Release/Face/Text"));
            Assert.IsTrue(screen.FishingCard.ReleaseEnabled);
            Assert.AreEqual(0, screen.FishingCard.Chosen, "the first kind in the basket is picked");
            Assert.AreEqual(L10n.T("tank.basket", 0, 6, "0"), screen.FishingCard.TankLine);

            Find(Pond + "/Basket/Fish 2").onClick.Invoke();
            Assert.AreEqual(2, screen.FishingCard.Chosen);
            Find(Pond + "/Release").onClick.Invoke();
            Assert.AreEqual(0, S.Fish[2]);
            Assert.AreEqual(2, S.Fish[0]);
            Assert.AreEqual(1, Aquarium.Released(S, 2));
            Assert.AreEqual("×0", TextAt(Pond + "/Basket/Fish 2/Count"));
            Assert.AreEqual(L10n.T("tank.milestone", "0,25"), screen.FishingCard.Status);
            Assert.AreEqual(0, screen.FishingCard.Chosen, "an empty kind hands the pick to the next fish in the basket");
            Assert.AreEqual(1.0025, runner.Mods.IncomeMultiplier, 1e-9, "the bonus is in the modifiers at once");

            Find(Pond + "/Release").onClick.Invoke();
            Assert.AreEqual(L10n.T("tank.milestone", "0,25"), screen.FishingCard.Status, "the first of a kind is a milestone too");
            Find(Pond + "/Release").onClick.Invoke();
            Assert.AreEqual(L10n.T("tank.released", L10n.T("fish.0"), 2), screen.FishingCard.Status);
            Assert.AreEqual(0, Fishing.InBasket(S));
            Assert.IsFalse(screen.FishingCard.ReleaseEnabled);
            Assert.AreEqual(L10n.T("tank.release.none"), TextAt(Pond + "/Release/Face/Text"));
            Assert.AreEqual(L10n.T("tank.basket", 2, 6, "0,5"), screen.FishingCard.TankLine);
        }

        [Test]
        public void The_Tank_Sheet_Shows_Each_Kind_s_Count_Next_Milestone_And_New_Until_Seen()
        {
            S.Fish[0] = 6; S.Fish[3] = 1;
            for (int i = 0; i < 6; i++) Aquarium.Release(S, 0);
            Aquarium.Release(S, 3);
            screen.Refresh();
            Assert.IsTrue(screen.TankFreshShown, "the tank in the garden says something new is inside");
            OpenTank();
            Assert.AreEqual(L10n.T("tank.title"), TextAt(Tank + "/Title"));
            Assert.AreEqual(L10n.T("tank.sub", 2, 6, "0,75", "12"), screen.TankSheet.Sub);
            Assert.AreEqual("×6", screen.TankSheet.CountOf(0));
            Assert.AreEqual(L10n.T("tank.next", 20, "0,5"), screen.TankSheet.NextOf(0));
            Assert.AreEqual(L10n.T("tank.next", 5, "0,25"), screen.TankSheet.NextOf(3));
            Assert.AreEqual(L10n.T("tank.none"), screen.TankSheet.NextOf(1));
            Assert.IsTrue(screen.TankSheet.FreshShown(0));
            Assert.IsTrue(screen.TankSheet.FreshShown(3));
            Assert.IsFalse(screen.TankSheet.FreshShown(1));
            Assert.IsTrue(screen.TankSheet.SwimmerShown(3));
            Assert.IsFalse(screen.TankSheet.SwimmerShown(5));
            Assert.AreEqual(L10n.T("tank.set.todo", 6, 10, 1), screen.TankSheet.SetLine);
            Assert.IsFalse(screen.TankFreshShown);

            Find(Tank + "/Close").onClick.Invoke();
            Step(0.4f);
            screen.Refresh();
            Assert.IsFalse(Aquarium.AnyFresh(S), "seen once, no longer new");
            Assert.IsFalse(screen.TankFreshShown);
            OpenTank();
            Assert.IsFalse(screen.TankSheet.FreshShown(0));
        }

        [Test]
        public void The_Full_Set_Card_Waits_For_The_Pond_To_Close_And_Shows_Once()
        {
            for (int k = 0; k < Aquarium.Kinds; k++) S.Fish[k] = 1;
            OpenPond();
            int golden = S.GoldenCarrots, jars = S.MoonJars;
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                Find(Pond + "/Basket/Fish " + k).onClick.Invoke();
                Find(Pond + "/Release").onClick.Invoke();
            }
            Assert.AreEqual(golden + 10, S.GoldenCarrots);
            Assert.AreEqual(jars + 1, S.MoonJars);
            screen.Refresh();
            Assert.IsFalse(screen.TankCard.IsOpen, "one card at a time: not over the pond's sheet");
            ClosePond();
            Assert.IsTrue(screen.TankCard.IsOpen, "the full set's card");
            Assert.AreEqual(L10n.T("tank.full.title"), TextAt("Canvas/AquariumSetCard/Card/Title"));
            StringAssert.Contains(L10n.T("mail.golden", 10), screen.TankCard.Gifts);
            StringAssert.Contains(L10n.T("mail.jars", 1), screen.TankCard.Gifts);
            Assert.IsFalse(runner.TankCardWaiting);
            Assert.IsFalse(screen.OfferCardFree, "nothing else opens over it");

            Find("Canvas/AquariumSetCard/Card/Ok").onClick.Invoke();
            Assert.IsFalse(screen.TankCard.IsOpen);
            screen.Refresh();
            Step(0.5f);
            Assert.IsFalse(screen.TankCard.IsOpen, "once");
        }

        [Test]
        public void The_Tank_Does_Not_Open_While_Arranging()
        {
            screen.EnterLayoutMode();
            Assert.IsTrue(screen.LayoutMode);
            Find("Canvas/Garden/Content/Tank").onClick.Invoke();
            Assert.IsFalse(screen.TankSheet.IsOpen);
            screen.ExitLayoutMode();
            Step(0.4f);
            OpenTank();
        }

        [Test]
        public void The_Income_Card_Lists_The_Tank_And_Keeps_It_Out_Of_The_Charms()
        {
            S.Fish[0] = 5;
            for (int i = 0; i < 5; i++) runner.ReleaseFish(0);
            screen.Refresh();
            Find("Canvas/Hud/Purse").onClick.Invoke();
            Assert.IsTrue(screen.Income.IsOpen);
            Assert.AreEqual(L10n.T("income.tank", 1, 6, "0,5"), TextAt("Canvas/Income/Card/Scroll/Content/Tank"));
            Assert.AreEqual(L10n.T("income.charms", UiKit.Times(1d)), TextAt("Canvas/Income/Card/Scroll/Content/Charms"));
        }
    }
}

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
    public class IntrosScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;

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
            s.GardenXp = Garden.XpForLevel(Pounding.UnlockLevel);
            s.HintSeen = true;
            s.TutorialStep = Tutorial.Steps;
            runner.Init(s);
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

        void Wait(int seconds)
        {
            for (int k = 0; k < seconds; k++) screen.Advance(1f);
        }

        void Forget()
        {
            S.IntrosSeen.Clear();
            S.IntrosTried.Clear();
            S.IntrosLater.Clear();
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        [Test]
        public void A_Garden_Loaded_Past_The_Thresholds_Shows_No_Card()
        {
            Assert.IsTrue(S.IntrosSeen.Contains(Intros.Fishing), "the preset counts as an old save: open cards are marked seen");
            Assert.IsTrue(S.IntrosSeen.Contains(Intros.Pounding));
            Step(3f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
        }

        [Test]
        public void The_Card_Slides_Up_With_Name_Two_Lines_And_The_First_Try_Gift()
        {
            Forget();
            Step(0.6f);
            Assert.IsTrue(screen.IntroCard.IsOpen);
            Assert.AreEqual(Intros.Fishing, screen.IntroCard.Showing);
            Assert.IsTrue(root.transform.Find("Canvas/Intro").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("intro.fishing.title"), screen.IntroCard.Title);
            Assert.AreEqual(2, screen.IntroCard.Body.Split('\n').Length, "two lines of explanation");
            StringAssert.StartsWith("Thử lần đầu", screen.IntroCard.RewardLine);
            Assert.IsTrue(S.IntrosSeen.Contains(Intros.Fishing));
        }

        [Test]
        public void No_Card_Interrupts_A_Mini_Game()
        {
            Forget();
            screen.OpenPounding();
            Assert.IsTrue(screen.Pound.IsOpen);
            Step(2f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "the mooncake game is left alone");
            Assert.AreEqual(0, S.IntrosSeen.Count, "and the card still waits its turn");
            screen.Pound.Close();
            Step(0.4f);
            Assert.IsTrue(screen.IntroCard.IsOpen, "once the game is over the card comes");
        }

        [Test]
        public void No_Card_Interrupts_The_Guidance()
        {
            Forget();
            S.TutorialStep = 0;
            Step(3f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Assert.AreEqual(0, S.IntrosSeen.Count);
            runner.SkipTutorial();
            Step(0.4f);
            Assert.IsTrue(screen.IntroCard.IsOpen);
        }

        [Test]
        public void No_Card_Over_Photo_Mode_Or_The_Summon_Moment()
        {
            Forget();
            screen.EnterPhotoMode();
            Step(1f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            screen.ExitPhotoMode();
            screen.Arrival.Show(runner.Catalog.Rabbit, "Bông", "", Color.white, true, "", "");
            Step(0.2f);
            Assert.IsTrue(screen.Arrival.IsOpen);
            Assert.IsFalse(screen.IntroCard.IsOpen);
        }

        [Test]
        public void Two_Features_Opened_At_Once_Show_One_Card_Then_The_Second_Ninety_Seconds_Later()
        {
            Forget();
            Step(0.4f);
            Assert.AreEqual(Intros.Fishing, screen.IntroCard.Showing);
            Find("Canvas/Intro/Later").onClick.Invoke();
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Wait(80);
            Assert.IsFalse(screen.IntroCard.IsOpen, "the second card waits");
            Assert.IsFalse(S.IntrosSeen.Contains(Intros.Pounding));
            Wait(11);
            Assert.IsTrue(screen.IntroCard.IsOpen);
            Assert.AreEqual(Intros.Pounding, screen.IntroCard.Showing);
        }

        [Test]
        public void Later_Leaves_A_Gold_Dot_On_The_Pond_Until_It_Is_Touched()
        {
            Forget();
            Step(0.4f);
            Find("Canvas/Intro/Later").onClick.Invoke();
            screen.Refresh();
            Step(0.3f);
            RectTransform dot = screen.IntroDot(Intros.Fishing);
            Assert.IsNotNull(dot, "a gold dot waits on the pond");
            Assert.AreEqual("PondTap", dot.parent.name);
            Assert.IsTrue(Intros.HasDot(S, Intros.Fishing));
            dot.GetComponent<Button>().onClick.Invoke();
            screen.Refresh();
            Assert.IsNull(screen.IntroDot(Intros.Fishing), "touched: the dot goes");
            Assert.IsFalse(Intros.HasDot(S, Intros.Fishing));
            Assert.IsNotNull(screen.IntroGlow, "and the pond lights up");
        }

        [Test]
        public void Opening_The_Feature_Also_Takes_The_Dot_Away()
        {
            Forget();
            Step(0.4f);
            Find("Canvas/Intro/Later").onClick.Invoke();
            screen.Refresh();
            Assert.IsNotNull(screen.IntroDot(Intros.Fishing));
            screen.OpenFishing();
            screen.Refresh();
            Assert.IsNull(screen.IntroDot(Intros.Fishing));
        }

        [Test]
        public void Try_Now_Slides_The_View_To_The_Pond_And_Lights_It_For_Three_Seconds()
        {
            Forget();
            screen.LookAt(screen.StationSpot(0));
            Step(0.4f);
            float before = Vector2.Distance(new Vector2((float)S.ViewX, (float)S.ViewY), GardenScreen.PondSpot);
            Find("Canvas/Intro/Try").onClick.Invoke();
            Step(0.2f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Assert.IsNotNull(screen.IntroGlow);
            Assert.AreEqual(root.transform.Find("Canvas/Garden/Content/PondTap").parent, screen.IntroGlow.parent, "the glow lies under the pond in the garden");
            Assert.Less(screen.IntroGlow.GetSiblingIndex(), root.transform.Find("Canvas/Garden/Content/PondTap").GetSiblingIndex() - 1, "behind the pond's water, so it rims the pond instead of greying it");
            Step(1.5f);
            float after = Vector2.Distance(new Vector2((float)S.ViewX, (float)S.ViewY), GardenScreen.PondSpot);
            Assert.Less(after, before, "the view went to the pond");
            Assert.IsNotNull(screen.IntroGlow, "still lit at two seconds");
            Step(1.5f);
            Assert.IsNull(screen.IntroGlow, "dark again after three seconds");
            Assert.IsFalse(Intros.HasDot(S, Intros.Fishing));
        }

        [Test]
        public void A_Sheet_Opened_Over_The_Card_Puts_It_Away_With_A_Dot()
        {
            Forget();
            Step(0.4f);
            Assert.IsTrue(screen.IntroCard.IsOpen);
            screen.OpenSheet(0);
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Assert.IsTrue(Intros.HasDot(S, Intros.Fishing));
        }

        [Test]
        public void The_First_Catch_Pays_The_First_Try_Gift_Once()
        {
            Forget();
            Intros.Show(S, Intros.Fishing);
            int clovers = S.Clovers;
            BigNum coins = S.Coins;
            runner.FishingClock = () => 1_800_000_000L;
            Assert.IsNotNull(runner.StartCast());
            for (int k = 0; k < 2000 && runner.Cast != null; k++) runner.AdvanceCast(0.1d);
            Assert.IsTrue(S.IntrosTried.Contains(Intros.Fishing));
            Assert.AreEqual(clovers + Intros.RewardClovers, S.Clovers);
            Assert.Greater(S.Coins.ToDouble(), coins.ToDouble());
            Assert.IsNotNull(runner.StartCast());
            for (int k = 0; k < 2000 && runner.Cast != null; k++) runner.AdvanceCast(0.1d);
            Assert.AreEqual(clovers + Intros.RewardClovers, S.Clovers, "once only");
        }

        [Test]
        public void The_Profile_Lists_The_Opened_Cards_To_Read_Again()
        {
            Forget();
            Step(0.4f);
            Find("Canvas/Intro/Later").onClick.Invoke();
            screen.Profile.Open(runner.Defs, S, runner.Account, runner.CloudStatus);
            Step(0.3f);
            Assert.AreEqual(L10n.T("profile.intros.count", 1, Intros.All.Length), root.transform.Find("Canvas/Profile/Card/Intros/Count").GetComponent<Text>().text);
            Find("Canvas/Profile/Card/Intros").onClick.Invoke();
            Step(0.3f);
            Assert.IsTrue(screen.IntroList.IsOpen);
            Assert.AreEqual(1, screen.IntroList.RowCount);
            Transform row = root.transform.Find("Canvas/Intros/Card/Rows/Row " + Intros.Fishing);
            Assert.IsTrue(row.gameObject.activeSelf);
            Assert.AreEqual(L10n.T("intro.fishing.title"), row.Find("Title").GetComponent<Text>().text);
            Assert.IsFalse(root.transform.Find("Canvas/Intros/Card/Rows/Row " + Intros.Pounding).gameObject.activeSelf);
        }
    }
}

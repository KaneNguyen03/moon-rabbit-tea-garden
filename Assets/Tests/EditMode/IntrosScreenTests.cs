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

        [Test, MonetizationOn]
        public void An_Intro_Card_Waits_While_The_Offer_Card_Or_The_First_Week_Panel_Is_Open()
        {
            Forget();
            screen.OpenOffer(Offers.Find(Offers.Starter));
            Assert.IsTrue(screen.OfferCard.IsOpen);
            Step(2f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "one card at a time: the pack card is up");
            Assert.AreEqual(0, S.IntrosSeen.Count, "the intro card waits its turn");
            screen.OfferCard.Close();
            screen.FirstWeek.Open();
            Step(1f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "nor over the first-week panel");
            screen.FirstWeek.Close();
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "both gone, but the pack card closed moments ago");
            Wait((int)CardPacer.GapSeconds);
            Assert.IsTrue(screen.IntroCard.IsOpen, "twenty seconds of garden later the card comes");
            Assert.IsFalse(screen.OfferCardFree, "and while it is up no pack card may open");
            Assert.IsTrue(screen.ReviewBusy, "nor the review card");
        }

        [Test]
        public void An_Intro_Card_Waits_While_The_Month_Kind_Chooser_Is_Open()
        {
            Forget();
            Assert.IsFalse(screen.IntroBlocked);
            screen.KindChooser.Open(MonthPass.MonthOf(runner.TodayNumber), null);
            Assert.IsTrue(screen.IntroBlocked, "the month pass's kind chooser counts as an open card");
            Step(1f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Assert.AreEqual(0, S.IntrosSeen.Count);
        }

        [Test]
        public void Opening_The_First_Week_Panel_Over_An_Intro_Card_Puts_It_Away_With_A_Dot()
        {
            Forget();
            Step(0.4f);
            Assert.IsTrue(screen.IntroCard.IsOpen);
            screen.FirstWeek.Open();
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "only one card is ever open");
            Assert.IsTrue(Intros.HasDot(S, Intros.Fishing));
        }

        [Test]
        public void Two_Features_Opened_At_Once_Show_One_Card_Then_The_Second_One_Gap_Later()
        {
            Forget();
            Step(0.4f);
            Assert.AreEqual(Intros.Fishing, screen.IntroCard.Showing);
            Find("Canvas/Intro/Later").onClick.Invoke();
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Wait((int)Intros.GapSeconds - 10);
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
            Transform row = root.transform.Find("Canvas/Intros/Card/Rows/Content/Row " + Intros.Fishing);
            Assert.IsTrue(row.gameObject.activeSelf);
            Assert.AreEqual(L10n.T("intro.fishing.title"), row.Find("Title").GetComponent<Text>().text);
            Assert.IsFalse(root.transform.Find("Canvas/Intros/Card/Rows/Content/Row " + Intros.Pounding).gameObject.activeSelf);
        }

        void OnlyDue(string id)
        {
            Forget();
            S.IntrosSeen.UnionWith(Intros.All);
            S.IntrosSeen.Remove(id);
            switch (id)
            {
                case Intros.RecipeBook: S.Season = 1; break;
                case Intros.Aquarium: S.Fish[0] = Intros.TankFish; break;
                case Intros.Fair:
                    long fair = Fairs.StartOf(Fairs.All[0], 2026);
                    runner.DayClock = () => fair;
                    S.MaxDaySeen = fair;
                    S.FirstDaySeen = fair - 20;
                    break;
                default: S.FirstDaySeen = S.MaxDaySeen - Intros.MonthPassDay; break;
            }
            Assert.IsTrue(Intros.IsOpen(S, id), id);
            screen.IntroQueue.Reset();
        }

        void ShowCard(string id)
        {
            OnlyDue(id);
            Step(0.4f);
            Assert.IsTrue(screen.IntroCard.IsOpen, id);
            Assert.AreEqual(id, screen.IntroCard.Showing);
            Assert.AreEqual(L10n.T("intro." + id + ".title"), screen.IntroCard.Title);
            Assert.AreEqual(2, screen.IntroCard.Body.Split('\n').Length, id + ": two lines");
            StringAssert.StartsWith("Thử lần đầu", screen.IntroCard.RewardLine, id + " carries the first-try gift");
        }

        static bool InView(RectTransform row)
        {
            var view = (RectTransform)row.parent.parent;
            var rowCorners = new Vector3[4];
            var viewCorners = new Vector3[4];
            row.GetWorldCorners(rowCorners);
            view.GetWorldCorners(viewCorners);
            return rowCorners[1].y <= viewCorners[1].y + 1f && rowCorners[1].y > viewCorners[0].y;
        }

        [TestCase(Intros.RecipeBook)]
        [TestCase(Intros.Trunk)]
        [TestCase(Intros.Fair)]
        public void Try_Now_On_A_Round_Two_Card_Opens_Its_Panel_And_Pays_The_First_Try(string id)
        {
            ShowCard(id);
            int clovers = S.Clovers;
            Find("Canvas/Intro/Try").onClick.Invoke();
            Step(0.3f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            switch (id)
            {
                case Intros.RecipeBook: Assert.IsTrue(screen.Book.IsOpen, "the recipe book opens"); break;
                case Intros.Trunk: Assert.IsTrue(screen.TrunkCard.IsOpen, "the trunk opens"); break;
                case Intros.Fair: Assert.IsTrue(screen.FairCard.IsOpen, "the fair's card opens"); break;
            }
            Assert.IsTrue(S.IntrosTried.Contains(id), "opening it is the first try");
            Assert.AreEqual(clovers + Intros.RewardClovers, S.Clovers);
            Assert.IsFalse(screen.IntroCard.IsOpen, "and no card comes over the panel");
            Wait((int)Intros.GapSeconds + 1);
            Assert.IsFalse(screen.IntroCard.IsOpen);
        }

        [Test]
        public void Try_Now_On_The_Tank_Card_Opens_The_Tank_And_The_First_Fish_Released_Pays()
        {
            ShowCard(Intros.Aquarium);
            int clovers = S.Clovers;
            Find("Canvas/Intro/Try").onClick.Invoke();
            Step(0.3f);
            Assert.IsTrue(screen.TankSheet.IsOpen, "the tank opens on its release list");
            Assert.IsFalse(S.IntrosTried.Contains(Intros.Aquarium), "looking is not trying");
            Assert.IsTrue(runner.ReleaseFish(0).Released);
            Assert.IsTrue(S.IntrosTried.Contains(Intros.Aquarium));
            Assert.AreEqual(clovers + Intros.RewardClovers, S.Clovers);
            Assert.IsTrue(runner.ReleaseFish(0).Released);
            Assert.AreEqual(clovers + Intros.RewardClovers, S.Clovers, "once only");
        }

        [Test, MonetizationOn]
        public void Try_Now_On_The_Month_Pass_Card_Opens_Quests_At_Its_Row_And_Lights_It() => TryQuestRow(Intros.MonthPass);

        [Test]
        public void Try_Now_On_The_Week_Chest_Card_Opens_Quests_At_Its_Row_And_Lights_It() => TryQuestRow(Intros.WeekChest);

        [Test, MonetizationOn(false)]
        public void With_Monetization_Off_No_Month_Pass_Card_Comes_On_Day_Four()
        {
            OnlyDue(Intros.WeekChest);
            S.IntrosSeen.Add(Intros.WeekChest);
            S.IntrosSeen.Remove(Intros.MonthPass);
            screen.IntroQueue.Reset();
            Wait((int)Intros.GapSeconds + 1);
            Assert.IsFalse(screen.IntroCard.IsOpen, "no card for a pass the garden does not sell");
            Assert.IsFalse(S.IntrosSeen.Contains(Intros.MonthPass));
        }

        void TryQuestRow(string id)
        {
            ShowCard(id);
            Find("Canvas/Intro/Try").onClick.Invoke();
            Step(0.3f);
            RectTransform row = id == Intros.MonthPass ? screen.MonthPassRows.Root : screen.ChestRows.WeekRow;
            Assert.IsTrue(screen.SheetOpen);
            Assert.AreEqual(GardenScreen.QuestTab, screen.Tab, "the quests tab");
            Assert.IsTrue(InView(row), "scrolled so its row is in view");
            Assert.IsNotNull(screen.IntroGlow, "and the row lights up");
            Assert.AreEqual(row.parent, screen.IntroGlow.parent);
            Assert.IsTrue(screen.IntroGlow.GetComponent<LayoutElement>().ignoreLayout, "the glow does not push the rows apart");
            Assert.IsFalse(screen.IntroGlow.Find("Frame").GetComponent<Image>().fillCenter, "a gold border round the row, never a plate over it");
            Assert.IsFalse(S.IntrosTried.Contains(id), "the gift comes with the first claim");
            Step(3.2f);
            Assert.IsNull(screen.IntroGlow);
        }

        [Test, MonetizationOn]
        public void The_First_Week_Chest_And_Month_Tier_Claimed_Pay_Their_First_Try_Gift()
        {
            OnlyDue(Intros.WeekChest);
            Intros.Show(S, Intros.WeekChest);
            S.IntrosSeen.Remove(Intros.MonthPass);
            Intros.Show(S, Intros.MonthPass);
            int clovers = S.Clovers;
            QuestChests.AddPoints(S, runner.TodayNumber, QuestChests.Chests[0].Points);
            Assert.IsTrue(runner.ClaimWeekChest(0, out _));
            Assert.IsTrue(S.IntrosTried.Contains(Intros.WeekChest));
            int afterChest = S.Clovers;
            Assert.GreaterOrEqual(afterChest, clovers + Intros.RewardClovers);
            Assert.GreaterOrEqual(MonthPass.Tier(S, runner.TodayNumber), 1, "the chest's points also move the month pass");
            Assert.IsTrue(runner.ClaimMonthTier(MonthRow.Free, 1, null, out _));
            Assert.IsTrue(S.IntrosTried.Contains(Intros.MonthPass));
            Assert.GreaterOrEqual(S.Clovers, afterChest + Intros.RewardClovers);
        }

        [Test]
        public void An_Intro_Card_Waits_While_The_Fair_Card_Is_Open_And_No_Fair_Card_Opens_Over_It()
        {
            OnlyDue(Intros.Fair);
            screen.FairCard.Open();
            Assert.IsTrue(screen.FairCard.IsOpen);
            Assert.IsTrue(screen.IntroBlocked, "the fair's card counts as an open card");
            Step(1f);
            Assert.IsFalse(screen.IntroCard.IsOpen);
            Assert.IsFalse(S.IntrosSeen.Contains(Intros.Fair));
            screen.FairCard.Close();
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "a card that comes by itself rests after the last one closed");
            Wait((int)CardPacer.GapSeconds + 1);
            Assert.IsTrue(screen.IntroCard.IsOpen);
            Assert.IsFalse(screen.OfferCardFree, "while it is up no other card may open");
            Assert.IsTrue(screen.ReviewBusy);
        }

        [TestCase(Intros.Trunk)]
        [TestCase(Intros.Aquarium)]
        [TestCase(Intros.RecipeBook)]
        public void A_Round_Two_Panel_Opened_Over_Its_Card_Puts_The_Card_Away_With_A_Dot(string id)
        {
            ShowCard(id);
            if (id == Intros.Trunk) screen.TrunkCard.Open(runner.TrunkShelf);
            else if (id == Intros.Aquarium) screen.TankSheet.Open();
            else screen.Book.Open(runner.Defs, S);
            Step(0.4f);
            Assert.IsFalse(screen.IntroCard.IsOpen, "only one card is ever open");
            Assert.IsTrue(Intros.HasDot(S, id));
        }

        [Test]
        public void Later_On_The_Trunk_Card_Leaves_A_Dot_On_The_Trunk_Until_It_Is_Opened()
        {
            ShowCard(Intros.Trunk);
            Find("Canvas/Intro/Later").onClick.Invoke();
            screen.Refresh();
            RectTransform dot = screen.IntroDot(Intros.Trunk);
            Assert.IsNotNull(dot);
            Assert.AreEqual(screen.TrunkProp, dot.parent);
            screen.OpenTrunk();
            screen.Refresh();
            Assert.IsNull(screen.IntroDot(Intros.Trunk));
            Assert.IsTrue(S.IntrosTried.Contains(Intros.Trunk));
        }

        [Test]
        public void Later_On_The_Week_Chest_Card_Leaves_A_Dot_On_Its_Row_Until_The_Quests_Open()
        {
            ShowCard(Intros.WeekChest);
            Find("Canvas/Intro/Later").onClick.Invoke();
            screen.Refresh();
            Assert.IsTrue(Intros.HasDot(S, Intros.WeekChest));
            screen.OpenSheet(GardenScreen.QuestTab);
            screen.Refresh();
            Assert.IsFalse(Intros.HasDot(S, Intros.WeekChest), "opening the quests takes the dot away");
        }

        [Test]
        public void The_Read_Again_List_Scrolls_When_All_Sixteen_Cards_Are_Open()
        {
            S.IntrosSeen.UnionWith(Intros.All);
            screen.IntroList.Open(S);
            Step(0.3f);
            Assert.AreEqual(Intros.All.Length, screen.IntroList.RowCount);
            Assert.LessOrEqual(screen.IntroList.Card.rect.height, IntroListView.MaxHeight, "the card stays inside the screen");
            Assert.Greater(screen.IntroList.Scroll.content.rect.height, screen.IntroList.Scroll.viewport.rect.height, "the rows scroll");
            Assert.IsTrue(screen.IntroList.Scroll.vertical);
        }
    }
}

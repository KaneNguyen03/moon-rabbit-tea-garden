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
    /// <summary>Builds the real screen from the real catalog and presses its buttons.</summary>
    public class GardenScreenTests
    {
        GameObject root;
        GameRunner runner;
        GardenScreen screen;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Init(GameState.NewGame(catalog.BuildDefs())); // preset: never touches the save file
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Build();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            Haptics.SetOn(true);
            L10n.SetLocale("vi");
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        [Test]
        public void Runner_Starts_With_Twelve_Stations_And_First_Open()
        {
            Assert.AreEqual(12, runner.Defs.Count);
            Assert.AreEqual(1, runner.State.Levels[0]);
        }

        [Test]
        public void Buy_Button_Is_Disabled_When_Broke_And_Works_When_Rich()
        {
            Button buy = Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy");
            Assert.IsFalse(buy.interactable);

            runner.State.Coins = new BigNum(1000);
            screen.Refresh();
            Assert.IsTrue(buy.interactable);
            buy.onClick.Invoke();
            Assert.AreEqual(2, runner.State.Levels[0]);
        }

        [Test]
        public void Second_Station_Unlocks_From_Its_Row()
        {
            runner.State.Coins = new BigNum(120);
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            Assert.AreEqual(1, runner.State.Levels[1]);
        }

        [Test]
        public void Locked_Station_Further_Down_Stays_Disabled()
        {
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Sheet/Panel/Scroll/Content/Row noi-mut/Buy").interactable);
        }

        [Test]
        public void Tapping_A_Rabbit_Speeds_Its_Station()
        {
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(Simulation.TapSeconds / 1.5, runner.State.Progress[0], 1e-9);
            Assert.IsTrue(runner.State.HintSeen);
        }

        [Test]
        public void Tapping_An_Unbuilt_Plot_Does_Nothing_But_Show_Its_Card()
        {
            Find("Canvas/Garden/Content/Station noi-mut").onClick.Invoke();
            Assert.AreEqual(0, runner.State.Progress[2]);
            Assert.AreEqual(2, screen.Selected);
        }

        [Test]
        public void Buy_Mode_Buttons_Change_The_Mode()
        {
            Find("Canvas/Sheet/Panel/Bar/Modes/Mode ×10").onClick.Invoke();
            Assert.AreEqual(10, runner.State.BuyMode);
            Find("Canvas/Sheet/Panel/Bar/Modes/Mode Tối đa").onClick.Invoke();
            Assert.AreEqual(0, runner.State.BuyMode);
        }

        [Test]
        public void Title_Hides_The_Hud_Until_Play_Starts()
        {
            Transform hud = root.transform.Find("Canvas/Hud");
            Assert.AreEqual(ShellState.Title, screen.Shell.State);
            Assert.IsFalse(hud.gameObject.activeSelf);

            Find("Canvas/Title").onClick.Invoke();
            for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);

            Assert.AreEqual(ShellState.Playing, screen.Shell.State);
            Assert.IsTrue(hud.gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Title").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/TabBar").gameObject.activeSelf);
            Assert.IsFalse(screen.SheetOpen, "the lists stay closed until asked for");
        }

        [Test]
        public void Pause_Stops_The_Garden_And_Resume_Restarts_It()
        {
            screen.Shell.StartGame();
            Find("Canvas/Hud/Settings").onClick.Invoke();
            Assert.AreEqual(ShellState.Paused, screen.Shell.State);
            Assert.IsTrue(runner.Paused);
            Assert.IsTrue(root.transform.Find("Canvas/Pause").gameObject.activeSelf);

            Find("Canvas/Pause/Card/Resume").onClick.Invoke();
            Assert.AreEqual(ShellState.Playing, screen.Shell.State);
            Assert.IsFalse(runner.Paused);
        }

        [Test]
        public void Reset_Needs_Two_Presses()
        {
            runner.State.Coins = new BigNum(1000);
            runner.State.Levels[0] = 30;
            screen.Shell.StartGame();
            screen.Shell.OpenPause();

            Button reset = Find("Canvas/Pause/Card/Reset");
            reset.onClick.Invoke();
            Assert.AreEqual(30, runner.State.Levels[0], "one press must not wipe progress");
            reset.onClick.Invoke();
            Assert.AreEqual(1, runner.State.Levels[0]);
            Assert.IsTrue(runner.State.Coins.Equals(BigNum.Zero));
        }

        [Test]
        public void Unlocking_A_Station_Plays_The_Moment()
        {
            screen.Shell.StartGame();
            runner.State.Coins = new BigNum(120);
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);

            Assert.IsTrue(screen.Feel.BannerVisible);
            Assert.Greater(screen.Feel.ParticleCount, 10);
            StringAssert.Contains("Giỏ lá trà", root.transform.Find("Canvas/Feel/Banner/Sub").GetComponent<Text>().text);

            for (int i = 0; i < 90; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(screen.Feel.BannerVisible, "the banner must leave on its own");
            Assert.AreEqual(0, screen.Feel.ParticleCount);
        }

        [Test]
        public void Coin_Counter_Counts_Up_Instead_Of_Snapping()
        {
            screen.Shell.StartGame();
            Text coins = root.transform.Find("Canvas/Hud/Purse/Coins").GetComponent<Text>();
            runner.State.Coins = new BigNum(900);
            screen.Advance(1f / 30f);
            Assert.AreNotEqual("900", coins.text);
            for (int i = 0; i < 90; i++) screen.Advance(1f / 30f);
            Assert.AreEqual("900", coins.text);
        }

        [Test]
        public void Tabs_Switch_Lists_And_Hide_The_Buy_Modes()
        {
            Transform panel = root.transform.Find("Canvas/Sheet/Panel");
            Assert.IsTrue(panel.Find("Scroll").gameObject.activeSelf);
            Assert.IsFalse(panel.Find("Upgrades").gameObject.activeSelf);

            screen.OpenSheet(1);
            Assert.AreEqual(1, screen.Tab);
            Assert.IsFalse(panel.Find("Scroll").gameObject.activeSelf);
            Assert.IsTrue(panel.Find("Upgrades").gameObject.activeSelf);
            Assert.IsFalse(panel.Find("Bar/Modes").gameObject.activeSelf);

            screen.OpenSheet(2);
            Assert.IsTrue(panel.Find("Quests").gameObject.activeSelf);
            screen.OpenSheet(0);
            Assert.IsTrue(panel.Find("Bar/Modes").gameObject.activeSelf);
        }

        [Test]
        public void Buying_An_Upgrade_Changes_What_Stations_Earn()
        {
            Assert.AreEqual(4, runner.UpgradeDefs.Count);
            Button buy = Find("Canvas/Sheet/Panel/Upgrades/Content/Upgrade trang-ram/Buy");
            Assert.IsFalse(buy.interactable);

            runner.State.Coins = new BigNum(3000);
            screen.Refresh();
            Assert.IsTrue(buy.interactable);
            buy.onClick.Invoke();

            Assert.AreEqual(1, runner.State.UpgradeLevels[2]);
            Assert.AreEqual(2, runner.Mods.IncomeMultiplier);
            screen.Refresh(); // in play the runner's StateChanged event does this
            StringAssert.StartsWith("2 cà rốt", root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Sub").GetComponent<Text>().text);
        }

        [Test]
        public void Quest_Badge_Appears_And_Claiming_Pays()
        {
            GameObject badge = root.transform.Find("Canvas/TabBar/Badge").gameObject; // the one badge left: on the tab bar
            Assert.IsFalse(badge.activeSelf);
            Button claim = Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2/Claim");
            Assert.IsFalse(claim.interactable);

            runner.State.Coins = new BigNum(120);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            screen.Refresh();
            Assert.IsTrue(badge.activeSelf);
            Assert.IsTrue(claim.interactable);

            double before = runner.State.Coins.ToDouble();
            claim.onClick.Invoke();
            Assert.Greater(runner.State.Coins.ToDouble(), before);
            Assert.IsTrue(runner.State.Claimed.Contains("mo-2"));
            screen.Refresh();
            Assert.IsFalse(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2").gameObject.activeSelf, "a claimed achievement leaves the list");
        }

        [Test]
        public void Daily_Quest_Stays_Visible_After_Claiming()
        {
            for (int i = 0; i < 50; i++) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Refresh();
            Button claim = Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim");
            Assert.IsTrue(claim.interactable);
            claim.onClick.Invoke();
            screen.Refresh();
            Assert.AreEqual(1, runner.State.Clovers);
            Assert.IsTrue(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham").gameObject.activeSelf);
            Assert.IsFalse(claim.interactable);
        }

        [Test]
        public void Rabbit_Tab_Calls_Merges_And_Sends_Rabbits_To_Work()
        {
            screen.OpenSheet(3);
            Assert.AreEqual(3, screen.Tab);
            Button summon = Find("Canvas/Sheet/Panel/Rabbits/Content/Summon/Button");
            Assert.IsFalse(summon.interactable, "no clovers yet");

            runner.State.Clovers = 3;
            screen.Refresh();
            Assert.IsTrue(summon.interactable);
            summon.onClick.Invoke();
            Assert.AreEqual(1, runner.State.Rabbits.Count);
            Assert.AreEqual(3 - 1 + (Garden.Level(runner.State) - 1), runner.State.Clovers, "one spent, and one back for each garden level gained");
            screen.Refresh();

            Rabbit first = runner.State.Rabbits[0];
            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit " + first.Id).onClick.Invoke();
            Assert.IsTrue(screen.Detail.IsOpen, "tapping a rabbit opens its own sheet");
            Find("Canvas/RabbitSheet/Card/Body/Stations/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(0, first.Station, "the only open station");
            screen.Refresh();
            Transform chip = root.transform.Find("Canvas/Garden/Content/Station am-tra/Tier");
            Assert.IsTrue(chip.gameObject.activeSelf);
            Assert.AreEqual(first.Name, chip.Find("Text").GetComponent<Text>().text);

            // Three silvers merge into one green that keeps the station.
            first.Tier = 0;
            runner.State.Rabbits.Add(new Rabbit { Id = 50, Tier = 0, Name = "B" });
            runner.State.Rabbits.Add(new Rabbit { Id = 51, Tier = 0, Name = "C" });
            runner.State.NextRabbitId = 51;
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Rabbits/Content/Merge 0/Button").onClick.Invoke();
            Assert.AreEqual(1, runner.State.Rabbits.Count);
            Assert.AreEqual(1, runner.State.Rabbits[0].Tier);
            Assert.AreEqual(0, runner.State.Rabbits[0].Station);
            screen.Refresh();
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Merge 0"), "merge offer goes away");
        }

        [Test]
        public void The_Fifth_Station_Waits_For_The_Garden_To_Widen()
        {
            for (int i = 0; i < 4; i++) runner.State.Levels[i] = 1;
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            Button openFifth = Find("Canvas/Sheet/Panel/Scroll/Content/Row lo-banh/Buy");
            Assert.IsFalse(openFifth.interactable, "no plot yet");
            Transform plot = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Plot");
            Assert.AreEqual(4, plot.GetSiblingIndex(), "right after the fourth station");
            Button widen = Find("Canvas/Sheet/Panel/Scroll/Content/Plot/Buy");
            Assert.IsFalse(widen.interactable, "carrots alone are not enough");
            StringAssert.Contains("B4532A", plot.Find("Sub").GetComponent<Text>().text, "an unmet condition is marked");

            // The garden reaches level 3 and has three rabbits: now it can widen.
            runner.State.GardenXp = Garden.XpForLevel(3);
            for (int i = 1; i <= 3; i++) runner.State.Rabbits.Add(new Rabbit { Id = i, Tier = 0, Name = "R" + i });
            runner.State.NextRabbitId = 3;
            screen.Refresh();
            Assert.IsTrue(widen.interactable);
            StringAssert.DoesNotContain("B4532A", plot.Find("Sub").GetComponent<Text>().text, "every condition met");
            widen.onClick.Invoke();
            Assert.AreEqual(6, runner.State.Plots, "two plots at once");
            Assert.AreEqual(3, runner.State.RestSpots, "and one more place to rest");
            screen.Refresh();
            Assert.IsTrue(openFifth.interactable);
            Assert.AreEqual(6, plot.GetSiblingIndex());

            runner.State.Plots = runner.Defs.Count;
            screen.Refresh();
            Assert.IsFalse(plot.gameObject.activeSelf, "nothing left to open");
        }

        [Test]
        public void The_Hud_Shows_The_Garden_Level_And_Mist_Lies_On_Land_Not_Yet_Opened()
        {
            Transform hud = root.transform.Find("Canvas/Hud");
            Assert.AreEqual("1", hud.Find("Level/Text").GetComponent<Text>().text);
            runner.State.GardenXp = Garden.XpForLevel(4) + Garden.XpToNext(4) / 2;
            screen.Refresh();
            Assert.AreEqual("4", hud.Find("Level/Text").GetComponent<Text>().text);
            Assert.AreEqual(0.5f, hud.Find("Level/Ring").GetComponent<Image>().fillAmount, 0.01f);

            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.IsNull(world.Find("Mist 3"), "the first four plots are the garden's own");
            Assert.IsTrue(world.Find("Mist 4").gameObject.activeSelf);
            Assert.IsTrue(world.Find("Mist 11").gameObject.activeSelf);
            Assert.IsTrue(world.Find("Grove 5").gameObject.activeSelf, "trees stand where no station is drawn yet");
            for (int i = 0; i < 5; i++) runner.State.Levels[i] = 1;
            runner.State.Plots = 6;
            screen.Refresh();
            for (int i = 0; i < 40; i++) screen.Advance(0.1f); // the mist takes a moment to lift
            Assert.IsFalse(world.Find("Mist 4").gameObject.activeSelf);
            Assert.IsFalse(world.Find("Grove 5").gameObject.activeSelf, "the sixth station is drawn now, so its trees are gone");
            Assert.IsTrue(world.Find("Mist 6").gameObject.activeSelf);
            Assert.IsTrue(world.Find("Grove 8").gameObject.activeSelf);
        }

        [Test]
        public void Tab_Bar_Opens_And_Closes_The_Sheet()
        {
            screen.Shell.StartGame();
            Assert.IsFalse(screen.SheetOpen);
            Find("Canvas/TabBar/Open Bùa").onClick.Invoke();
            Assert.IsTrue(screen.SheetOpen);
            Assert.AreEqual(1, screen.Tab);
            Find("Canvas/TabBar/Open Thỏ").onClick.Invoke();
            Assert.IsTrue(screen.SheetOpen, "another tab switches, it does not close");
            Assert.AreEqual(3, screen.Tab);
            Find("Canvas/TabBar/Open Thỏ").onClick.Invoke();
            Assert.IsFalse(screen.SheetOpen, "the same tab again closes it");
            screen.OpenSheet(0);
            Find("Canvas/Sheet/Panel/Bar/Close").onClick.Invoke();
            Assert.IsFalse(screen.SheetOpen);
        }

        [Test]
        public void Sheet_Slides_Up_To_Sixty_Percent_And_Leaves_The_Garden_And_Hud_Alone()
        {
            screen.Shell.StartGame();
            screen.OpenSheet(0);
            var panel = (RectTransform)root.transform.Find("Canvas/Sheet/Panel");
            Assert.Less(panel.anchoredPosition.y, -1000f, "it starts below the screen");
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(0f, panel.anchoredPosition.y, 0.5f, "0.25 s later it stands on the bottom edge");
            Assert.AreEqual(1920f * 0.6f, screen.SheetHeight, 0.5f);
            Assert.AreEqual(screen.SheetHeight, panel.sizeDelta.y, 0.5f);

            Transform sheet = root.transform.Find("Canvas/Sheet");
            Assert.IsNull(sheet.GetComponent<Image>(), "nothing greys the whole screen");
            Assert.Less(sheet.GetSiblingIndex(), root.transform.Find("Canvas/Hud").GetSiblingIndex(), "the HUD is drawn over the sheet, not under a scrim");
            Assert.Less(sheet.GetSiblingIndex(), root.transform.Find("Canvas/TabBar").GetSiblingIndex());
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Bar/Grab"), "there is a handle to grab");
            var shade = (RectTransform)root.transform.Find("Canvas/Sheet/Panel/Shade");
            Assert.LessOrEqual(shade.sizeDelta.y, 260f, "only a strip above the edge darkens");
        }

        [Test]
        public void Sheet_Closes_On_A_Touch_Outside_And_Is_Gone_After_The_Slide()
        {
            screen.Shell.StartGame();
            screen.OpenSheet(2);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Find("Canvas/Sheet/Outside").onClick.Invoke();
            Assert.IsFalse(screen.SheetOpen);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(root.transform.Find("Canvas/Sheet").gameObject.activeSelf);
        }

        [Test]
        public void Sheet_Handle_Drags_Up_To_Full_And_Down_To_Closed()
        {
            screen.Shell.StartGame();
            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);

            screen.DragSheet(400f);
            screen.ReleaseSheet();
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(GardenScreen.SheetFull, screen.SheetHeight, 0.5f, "pulled past the middle it opens all the way");
            Assert.Less(GardenScreen.SheetFull, 1920f - 150f, "and still stops under the HUD");

            screen.DragSheet(5000f);
            Assert.AreEqual(GardenScreen.SheetFull, screen.SheetHeight, 0.5f, "it cannot be pulled over the HUD");

            screen.DragSheet(-60f);
            screen.ReleaseSheet(-2000f);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.IsTrue(screen.SheetOpen, "a flick down from full only drops it to 60%");
            Assert.AreEqual(GardenScreen.SheetHalf, screen.SheetHeight, 0.5f);

            screen.DragSheet(-80f);
            screen.ReleaseSheet(-2000f);
            Assert.IsFalse(screen.SheetOpen, "one more flick down closes it");

            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(GardenScreen.SheetHalf, screen.SheetHeight, 0.5f, "it always opens at 60% again");
            screen.DragSheet(-400f);
            screen.ReleaseSheet();
            Assert.IsFalse(screen.SheetOpen, "dragged well down and let go closes it too");
        }

        [Test]
        public void Touching_A_Station_Row_Slides_The_Garden_To_That_Station_Above_The_Sheet()
        {
            runner.State.Plots = runner.Defs.Count;
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = 1;
            screen.Shell.StartGame();
            screen.Refresh();
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);

            // Station 11 is the lowest in the garden: without lifting the view it would stay behind the sheet.
            foreach (int index in new[] { 11, 9, 0 })
            {
                Find("Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[index].Id).onClick.Invoke();
                for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
                float y = GardenScreen.StationSpot(index).y * world.localScale.y + world.anchoredPosition.y;
                Assert.Greater(y, screen.SheetHeight + 60f, "station " + index + " stands above the sheet");
                Assert.Less(y, 1920f - 160f, "station " + index + " stands under the HUD");
            }

            screen.CloseSheet();
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.LessOrEqual(world.anchoredPosition.y, 0.5f, "closed again, the garden settles back inside its edges");
        }

        [Test]
        public void Charm_Picture_Fills_Its_Tile_Without_A_Coloured_Disc()
        {
            Transform icon = root.transform.Find("Canvas/Sheet/Panel/Upgrades/Content/Upgrade trang-ram/Icon");
            Transform art = icon.Find("Art");
            Assert.IsNotNull(art, "this charm has a drawn picture");
            Assert.IsNull(icon.GetComponent<Image>(), "no disc behind a drawn charm");
            var rt = (RectTransform)art;
            Assert.AreEqual(Vector2.zero, rt.offsetMin);
            Assert.AreEqual(Vector2.zero, rt.offsetMax);
        }

        [Test]
        public void Tab_Bar_Is_One_Plate_And_The_Chosen_Tab_Stands_Out()
        {
            screen.Shell.StartGame();
            for (int i = 0; i < 30; i++) screen.Advance(1f / 30f);
            var bar = (RectTransform)root.transform.Find("Canvas/TabBar");
            Assert.IsNotNull(bar.Find("Plate").GetComponent<Image>(), "one plate behind all four tabs");
            float screenWidth = ((RectTransform)bar.parent).rect.width;
            Assert.AreEqual(screenWidth - 40f, bar.rect.width, 0.5f, "the plate runs the width of the screen, less a small margin");
            Assert.GreaterOrEqual(bar.anchoredPosition.y, 0f, "inside the bottom edge");

            string[] names = { "Trạm", "Bùa", "Nhiệm vụ", "Thỏ" };
            foreach (string n in names)
            {
                Text label = bar.Find("Open " + n + "/Label").GetComponent<Text>();
                Assert.GreaterOrEqual(label.fontSize, 26, "readable at arm's length");
                Assert.AreEqual(FontStyle.Normal, label.fontStyle, "nothing is chosen while no sheet is open");
                Assert.AreEqual(1f, bar.Find("Open " + n + "/Icon").GetComponent<Image>().color.a, 0.01f);
            }

            screen.OpenSheet(1);
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            var chosen = (RectTransform)bar.Find("Open Bùa/Icon");
            var other = (RectTransform)bar.Find("Open Thỏ/Icon");
            Assert.Greater(chosen.sizeDelta.x, other.sizeDelta.x + 15f, "the chosen icon is bigger");
            Assert.AreEqual(FontStyle.Bold, bar.Find("Open Bùa/Label").GetComponent<Text>().fontStyle);
            Assert.AreEqual(FontStyle.Normal, bar.Find("Open Thỏ/Label").GetComponent<Text>().fontStyle);
            Assert.AreEqual(1f, bar.Find("Open Bùa/Lift").GetComponent<CanvasGroup>().alpha, 0.01f, "it stands on a raised disc");
            Assert.AreEqual(0f, bar.Find("Open Thỏ/Lift").GetComponent<CanvasGroup>().alpha, 0.01f);
            Assert.Less(other.GetComponent<Image>().color.a, 0.7f, "the others step back");

            screen.CloseSheet();
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(84f, chosen.sizeDelta.x, 0.5f, "closed again, it returns to its resting size exactly");
        }

        [Test]
        public void Tab_Badge_Shows_Only_When_Something_Waits_And_Bounces_When_The_Count_Grows()
        {
            screen.Shell.StartGame();
            screen.Refresh();
            Transform badge = root.transform.Find("Canvas/TabBar/Badge");
            int before = Quests.ClaimableCount(runner.QuestDefs, runner.Defs, runner.State);
            Assert.AreEqual(before > 0, badge.gameObject.activeSelf);

            runner.State.Coins = new BigNum(1e9);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke(); // finishes "open 2 stations"
            screen.Refresh();
            Assert.Greater(Quests.ClaimableCount(runner.QuestDefs, runner.Defs, runner.State), before);
            Assert.IsTrue(badge.gameObject.activeSelf);
            for (int i = 0; i < 4; i++) screen.Advance(1f / 30f);
            Assert.Greater(badge.localScale.x, 1.1f, "it bounces when the number goes up");
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(1f, badge.localScale.x, 0.001f, "and comes back to its own size exactly");
        }

        [Test]
        public void No_Shared_Duration_Holds_The_Player_Longer_Than_Three_Tenths_Of_A_Second()
        {
            foreach (float seconds in new[] { Motion.Quick, Motion.Tap, Motion.Panel, Motion.PanelOut, Motion.Travel })
                Assert.LessOrEqual(seconds, 0.3f);
            Assert.AreEqual(1f, Motion.Glide, 0.25f, "the opening glide takes about a second (and can be interrupted)");
        }

        [Test]
        public void Opening_The_Game_Glides_From_The_Moon_Pond_Down_To_The_Teapot()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            float PondY() => GardenScreen.PondSpot.y * world.localScale.y + world.anchoredPosition.y;
            float PotY() => GardenScreen.StationSpot(0).y * world.localScale.y + world.anchoredPosition.y;
            Assert.That(PondY(), Is.InRange(300f, 1700f), "behind the title the pond is in view");

            screen.Shell.StartGame();
            for (int i = 0; i < 15; i++) screen.Advance(1f / 30f);
            float midway = world.anchoredPosition.y;
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.AreNotEqual(midway, world.anchoredPosition.y, "half a second in it is still travelling");
            Assert.AreEqual(1920f * 0.6f, PotY(), 1f, "after about a second the teapot stands where it always does");
        }

        [Test]
        public void Looking_Somewhere_Else_Takes_Over_From_The_Opening_Glide()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.Shell.StartGame();
            for (int i = 0; i < 6; i++) screen.Advance(1f / 30f);
            screen.LookAt(GardenScreen.StationSpot(2));
            Vector2 chosen = world.anchoredPosition;
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(chosen, world.anchoredPosition, "the glide does not drag the view back");
        }

        [Test]
        public void Reduce_Motion_Is_A_Setting_And_Things_Then_Simply_Arrive()
        {
            screen.Shell.StartGame();
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            screen.Shell.OpenPause();
            Text label = root.transform.Find("Canvas/Pause/Card/Motion/Text").GetComponent<Text>();
            Assert.AreEqual("Tắt", label.text);
            Find("Canvas/Pause/Card/Motion").onClick.Invoke();
            Assert.IsTrue(Motion.Reduced);
            Assert.AreEqual("Bật", label.text);
            Find("Canvas/Pause/Card/Resume").onClick.Invoke();

            screen.OpenSheet(0);
            screen.Advance(1f / 30f);
            var panel = (RectTransform)root.transform.Find("Canvas/Sheet/Panel");
            Assert.AreEqual(0f, panel.anchoredPosition.y, 0.5f, "no slide: the sheet is there on the next frame");
            Transform row = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra");
            Assert.IsTrue(row.GetComponent<CanvasGroup>() == null || row.GetComponent<CanvasGroup>().alpha == 1f, "rows do not fade in one by one");
        }

        [Test]
        public void List_Rows_Arrive_One_After_Another()
        {
            screen.Shell.StartGame();
            screen.OpenSheet(0);
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content");
            CanvasGroup first = content.GetChild(0).GetComponent<CanvasGroup>(), third = content.GetChild(2).GetComponent<CanvasGroup>();
            Assert.AreEqual(0f, first.alpha);
            for (int i = 0; i < 2; i++) screen.Advance(1f / 30f);
            Assert.Greater(first.alpha, third.alpha, "the top row is ahead of the third");
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(1f, first.alpha);
            Assert.AreEqual(1f, third.alpha);
            Assert.AreEqual(Vector3.one, content.GetChild(2).localScale);
        }

        [Test]
        public void A_Raised_Button_Sinks_Under_The_Finger_And_Springs_Back_To_Rest()
        {
            var buy = (RectTransform)root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy");
            runner.State.Coins = new BigNum(1e9);
            screen.Refresh();
            var dip = buy.GetComponent<PressDip>();
            var face = (RectTransform)buy.Find("Face");
            Vector2 restMin = face.offsetMin, restMax = face.offsetMax;
            dip.Hold();
            Assert.Less(face.offsetMax.y, restMax.y - 4f, "the face goes down while held");
            dip.Release();
            for (int i = 0; i < 12; i++) dip.Step(1f / 30f);
            Assert.AreEqual(restMin, face.offsetMin);
            Assert.AreEqual(restMax, face.offsetMax);
            Assert.IsFalse(dip.Busy);
        }

        [Test]
        public void Each_Kind_Of_Touch_Has_Its_Own_Sound()
        {
            screen.Shell.StartGame();
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();

            screen.OpenSheet(0);
            Assert.AreEqual("sheet-up", Sfx.Last);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
            Assert.AreEqual("upgrade", Sfx.Last);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            Assert.AreEqual("unlock", Sfx.Last, "opening a station");
            runner.State.Levels[0] = 9;
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
            Assert.AreEqual("milestone", Sfx.Last, "level 10 is a milestone");
            screen.SetTab(2);
            Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2/Claim").onClick.Invoke();
            Assert.AreEqual("reward", Sfx.Last);
            screen.CloseSheet();
            Assert.AreEqual("sheet-down", Sfx.Last);
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual("pet", Sfx.Last, "patting a rabbit");
        }

        [Test]
        public void A_Buy_Button_You_Cannot_Afford_Shakes_And_The_Purse_Flashes_Red()
        {
            screen.Shell.StartGame();
            runner.State.Coins = BigNum.Zero;
            screen.OpenSheet(0);
            screen.Refresh();
            var buy = (RectTransform)root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy");
            Assert.IsFalse(buy.GetComponent<Button>().interactable);
            var face = (RectTransform)buy.Find("Face");
            Text coins = root.transform.Find("Canvas/Hud/Purse/Coins").GetComponent<Text>();
            Color ink = coins.color;
            int level = runner.State.Levels[0];

            buy.GetComponent<PressDip>().Click();
            Assert.AreEqual("deny", Sfx.Last);
            Assert.AreEqual(level, runner.State.Levels[0], "nothing was bought");
            for (int i = 0; i < 2; i++) screen.Advance(1f / 30f);
            Assert.AreNotEqual(0f, face.offsetMin.x, "the button is shaking");
            Assert.Greater(coins.color.r - coins.color.b, ink.r - ink.b + 0.1f, "the carrot count has turned towards red");
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(0f, face.offsetMin.x, "and comes to rest exactly where it was");
            Assert.AreEqual(ink, coins.color);
            Assert.AreEqual(Vector3.one, root.transform.Find("Canvas/Hud/Purse").localScale);

            runner.State.Coins = new BigNum(1e9);
            screen.Refresh();
            Sfx.Click();
            buy.GetComponent<PressDip>().Click();
            Assert.AreNotEqual("deny", Sfx.Last, "a button that works never says no");
        }

        [Test]
        public void Haptics_Fire_For_Upgrades_And_Milestones_And_Can_Be_Switched_Off()
        {
            screen.Shell.StartGame();
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            int before = Haptics.Count;
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
            Assert.AreEqual(before + 1, Haptics.Count);
            int light = Haptics.LastMilliseconds;
            runner.State.Levels[0] = 9;
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
            Assert.Greater(Haptics.LastMilliseconds, light, "a milestone is felt more than a plain upgrade");

            screen.Shell.OpenPause();
            Assert.AreEqual("Bật", root.transform.Find("Canvas/Pause/Card/Haptics/Text").GetComponent<Text>().text);
            Find("Canvas/Pause/Card/Haptics").onClick.Invoke();
            Assert.IsFalse(Haptics.On);
            Find("Canvas/Pause/Card/Resume").onClick.Invoke();
            int quiet = Haptics.Count;
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
            Assert.AreEqual(quiet, Haptics.Count, "switched off: nothing buzzes");
        }

        [Test]
        public void Tapping_A_Station_Opens_Its_Card_And_The_Card_Buys()
        {
            Assert.AreEqual(-1, screen.Selected);
            Transform card = root.transform.Find("Canvas/Card");
            Assert.IsFalse(card.gameObject.activeSelf, "no card until a station is tapped");

            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(0, screen.Selected);
            Assert.IsTrue(card.gameObject.activeSelf);
            Assert.AreEqual("Ấm trà sương", card.Find("Name").GetComponent<Text>().text);
            Button buy = Find("Canvas/Card/Buy");
            Assert.IsFalse(buy.interactable, "broke");

            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            Assert.IsTrue(buy.interactable);
            buy.onClick.Invoke();
            Assert.AreEqual(2, runner.State.Levels[0]);

            // Tapping the empty ground closes it.
            Find("Canvas/Garden").onClick.Invoke();
            Assert.AreEqual(-1, screen.Selected);
            Assert.IsFalse(card.gameObject.activeSelf);
        }

        [Test]
        public void The_Card_Of_Empty_Land_Widens_The_Garden_And_Then_Opens_The_Station()
        {
            for (int i = 0; i < 4; i++) runner.State.Levels[i] = 1;
            runner.State.Coins = new BigNum(1e12);
            runner.State.GardenXp = Garden.XpForLevel(3);
            for (int i = 1; i <= 3; i++) runner.State.Rabbits.Add(new Rabbit { Id = i, Tier = 0, Name = "R" + i });
            runner.State.NextRabbitId = 3;
            screen.Refresh();

            // The fifth station stands in the garden as empty land; the one after it is not there yet.
            Transform fifth = root.transform.Find("Canvas/Garden/Content/Station lo-banh");
            Assert.IsTrue(fifth.gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station to-mat").gameObject.activeSelf);

            Find("Canvas/Garden/Content/Station lo-banh").onClick.Invoke();
            Transform card = root.transform.Find("Canvas/Card");
            Assert.AreEqual("Mở rộng", card.Find("Buy/Face/Top").GetComponent<Text>().text);
            Find("Canvas/Card/Buy").onClick.Invoke();
            Assert.AreEqual(6, runner.State.Plots);
            screen.Refresh();
            Assert.AreEqual("Mở trạm", card.Find("Buy/Face/Top").GetComponent<Text>().text);
            Find("Canvas/Card/Buy").onClick.Invoke();
            Assert.AreEqual(1, runner.State.Levels[4]);
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Station to-mat").gameObject.activeSelf);
        }

        [Test]
        public void The_World_Scrolls_Only_Up_And_Down_And_Stations_Do_Not_Overlap()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Assert.AreEqual(1080f, world.sizeDelta.x, 0.5f, "as wide as the portrait screen");
            Assert.Greater(world.sizeDelta.y, 1920f * 1.2f, "taller than the portrait screen");
            var scroll = world.parent.GetComponent<UnityEngine.UI.ScrollRect>();
            Assert.IsFalse(scroll.horizontal);
            Assert.IsTrue(scroll.vertical);
            for (int a = 0; a < runner.Defs.Count; a++)
            {
                Vector2 p = GardenScreen.StationSpot(a);
                Assert.IsTrue(p.x > 200 && p.x < world.sizeDelta.x - 200 && p.y > 200 && p.y < world.sizeDelta.y - 200, "station " + a + " is inside the world");
                for (int b = a + 1; b < runner.Defs.Count; b++)
                    Assert.Greater(Vector2.Distance(p, GardenScreen.StationSpot(b)), 280f, a + " and " + b);
            }
        }

        [Test]
        public void Zooming_Out_Shows_The_Whole_Garden_And_Hides_The_Small_Print()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var plate = world.Find("Station am-tra/Level").gameObject;
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Assert.IsTrue(plate.activeSelf);

            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Assert.AreEqual(GardenScreen.ZoomLevel.Far, screen.Zoom);
            Assert.LessOrEqual(world.sizeDelta.y * world.localScale.y, 1920f + 0.5f, "the whole garden fits top to bottom");
            Assert.IsFalse(plate.activeSelf, "no level plates from far away");
            // Smaller than the screen sideways: it sits in the middle, not against an edge.
            Assert.AreEqual((1080f - world.sizeDelta.x * world.localScale.x) / 2f, world.anchoredPosition.x, 0.5f);

            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            Assert.IsTrue(plate.activeSelf);
            Assert.AreEqual(1f, world.localScale.x, 1e-4f);
        }

        [Test]
        public void Zooming_In_Never_Shows_Past_The_Edge_Of_The_Garden()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false, new Vector2(0f, 0f));
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            float w = world.sizeDelta.x * world.localScale.x, h = world.sizeDelta.y * world.localScale.y;
            Assert.LessOrEqual(world.anchoredPosition.x, 0.5f);
            Assert.GreaterOrEqual(world.anchoredPosition.x, 1080f - w - 0.5f);
            Assert.LessOrEqual(world.anchoredPosition.y, 0.5f);
            Assert.GreaterOrEqual(world.anchoredPosition.y, 1920f - h - 0.5f);
        }

        [Test]
        public void The_Zoom_Button_Goes_Round_The_Three_Levels()
        {
            Button zoom = Find("Canvas/Hud/Zoom");
            zoom.onClick.Invoke(); screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            zoom.onClick.Invoke(); screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Far, screen.Zoom);
            zoom.onClick.Invoke(); screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
        }

        [Test]
        public void The_Hud_Is_One_Row()
        {
            var purse = (RectTransform)root.transform.Find("Canvas/Hud/Purse");
            var energy = (RectTransform)root.transform.Find("Canvas/Hud/Energy");
            Assert.AreEqual(purse.anchoredPosition.y, energy.anchoredPosition.y, 0.5f);
            Assert.LessOrEqual(purse.anchoredPosition.x + purse.sizeDelta.x / 2f, energy.anchoredPosition.x - energy.sizeDelta.x / 2f, "they do not overlap");
        }

        [Test]
        public void The_Station_Card_Is_Docked_And_The_Station_Scrolls_Clear_Of_It()
        {
            for (int i = 0; i < runner.Defs.Count; i++) if (runner.State.Levels[i] <= 0) runner.State.Levels[i] = 1;
            runner.State.Plots = runner.Defs.Count;
            screen.Refresh();
            var card = (RectTransform)root.transform.Find("Canvas/Card");
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Assert.AreEqual(root.transform.Find("Canvas"), card.parent, "the card does not move with the garden");
            float cardTop = card.anchoredPosition.y + card.sizeDelta.y / 2f;
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                screen.SelectStation(-1);
                screen.SelectStation(i);
                screen.Advance(1f); // let the slide finish
                float feet = (GardenScreen.StationSpot(i).y - 150f) * world.localScale.y + world.anchoredPosition.y;
                Assert.GreaterOrEqual(feet, cardTop - 1f, "station " + i + " stands above the card");
            }
        }

        [Test]
        public void Every_Kind_Has_A_Picture_And_A_Name_In_The_Current_Language()
        {
            foreach (RabbitKind kind in RabbitKinds.All)
            {
                Sprite art = runner.Catalog.KindSprite(kind.Id);
                Assert.IsNotNull(art, kind.Id);
                Assert.AreEqual("kind_" + kind.Id, art.name, "its own picture, not a fallback");
                Assert.AreNotEqual("kind." + kind.Id, L10n.T("kind." + kind.Id), kind.Id + " has no name in the table");
            }
        }

        [Test]
        public void The_Rabbit_Tab_Is_A_Grid_Of_Three_And_The_Album_Lists_Every_Kind()
        {
            for (int i = 1; i <= 7; i++)
                runner.State.Rabbits.Add(new Rabbit { Id = i, Tier = i % 3, Kind = RabbitKinds.OfTier(i % 3)[0].Id, Name = "R" + i });
            runner.State.NextRabbitId = 7;
            foreach (Rabbit r in runner.State.Rabbits) Rabbits.See(runner.State, r.Kind);
            screen.OpenSheet(3);
            screen.Refresh();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content");
            Assert.AreEqual(3, content.Find("Row 0").childCount);
            Assert.AreEqual(3, content.Find("Row 1").childCount);
            Assert.AreEqual(1, content.Find("Row 2").childCount);
            // Best tier first.
            StringAssert.StartsWith("Rabbit ", content.Find("Row 0").GetChild(0).name);
            long firstId = long.Parse(content.Find("Row 0").GetChild(0).name.Substring(7));
            Assert.AreEqual(2, Rabbits.Find(runner.State, firstId).Tier);
            StringAssert.Contains("3/" + RabbitKinds.All.Count, content.Find("Count/Text").GetComponent<Text>().text);

            Find("Canvas/Sheet/Panel/Rabbits/Content/SubTabs/Album").onClick.Invoke();
            screen.Refresh();
            int cells = 0, met = 0;
            foreach (RabbitKind kind in RabbitKinds.All)
            {
                Transform cell = null;
                foreach (Transform row in content) { cell = row.Find("Kind " + kind.Id); if (cell != null) break; }
                Assert.IsNotNull(cell, kind.Id + " has a place in the album");
                cells++;
                bool seen = runner.State.SeenKinds.Contains(kind.Id);
                if (seen) met++;
                Assert.AreEqual(seen ? L10n.T("kind." + kind.Id) : "???", cell.Find("Name").GetComponent<Text>().text);
            }
            Assert.AreEqual(RabbitKinds.All.Count, cells);
            Assert.AreEqual(3, met);
            Assert.IsNull(content.Find("Row 0"), "the album replaces the grid");
        }

        [Test]
        public void Tapping_A_Station_From_Far_Away_Comes_Back_To_The_Usual_Zoom()
        {
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Assert.AreEqual(0, screen.Selected);
        }

        [Test]
        public void A_Kind_With_Antlers_Wears_No_Hat_But_The_Hat_Still_Counts()
        {
            RabbitKind antlers = RabbitKinds.Find("tho-gac");
            Assert.IsTrue(antlers.NoHat);
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = antlers.Tier, Kind = antlers.Id, Name = "Gạc", Station = 0 });
            runner.State.NextRabbitId = 1;
            double bare = Rabbits.Multiplier(runner.State, runner.State.Rabbits[0]);
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 2); // straw hat: a station bonus
            Item charm = Items.Grant(runner.State, runner.ItemDefs[3], 0);
            runner.EquipItem(hat.Id, 1);
            runner.EquipItem(charm.Id, 1);
            screen.Refresh();
            Transform rabbit = root.transform.Find("Canvas/Garden/Content/Station am-tra/Rabbit");
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf, "no hat drawn over the antlers");
            Assert.IsTrue(rabbit.Find("CharmOn").gameObject.activeSelf, "the charm still shows");
            Assert.Greater(Rabbits.Multiplier(runner.State, runner.State.Rabbits[0]), bare, "the hat's bonus is not lost");
            screen.OpenSheet(3);
            screen.Refresh();
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/HatOn"));
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/CharmOn"));
        }

        [Test]
        public void Resting_Rabbits_Sit_In_The_Garden_As_Themselves_Up_To_The_Places_There_Are()
        {
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.IsFalse(world.Find("Seat 0").gameObject.activeSelf, "nobody resting: nobody seated");
            Assert.IsFalse(world.Find("Walker").gameObject.activeSelf, "and nobody out walking");

            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 4, Kind = "tho-sao", Name = "Sao" });
            runner.State.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Kind = "tho-me", Name = "Mè" });
            runner.State.Rabbits.Add(new Rabbit { Id = 3, Tier = 0, Kind = "tho-bong", Name = "Bông" });
            runner.State.NextRabbitId = 3;
            screen.Refresh();

            // Each of the first seats is either sat on or its rabbit is out walking: never both, never neither.
            int out0 = 0;
            for (int k = 0; k < 2; k++)
            {
                bool sitting = world.Find("Seat " + k).gameObject.activeSelf, walking = world.GetChild(0) != null && GardenScreen.Strolling(k, 0f);
                Assert.AreNotEqual(sitting, walking, "seat " + k);
                if (walking) out0++;
            }
            Assert.AreEqual("kind_tho-sao", world.Find("Seat 0/Art").GetComponent<Image>().sprite.name, "the rarest takes the first seat");
            Assert.AreEqual("kind_tho-me", world.Find("Seat 1/Art").GetComponent<Image>().sprite.name);
            Assert.IsFalse(world.Find("Seat 2").gameObject.activeSelf, "two places only: the third rabbit is not out");

            runner.State.RestSpots = 3;
            screen.Refresh();
            Assert.AreEqual("kind_tho-bong", world.Find("Seat 2/Art").GetComponent<Image>().sprite.name);

            // Tapping a seated rabbit opens its own sheet.
            int sat = GardenScreen.Strolling(1, 0f) ? 2 : 1;
            Find("Canvas/Garden/Content/Seat " + sat).onClick.Invoke();
            Assert.IsTrue(screen.Detail.IsOpen);
        }

        [Test]
        public void A_Collected_Rabbit_Stands_At_Its_Station_Wearing_Its_Hat()
        {
            Transform rabbit = root.transform.Find("Canvas/Garden/Content/Station am-tra/Rabbit");
            Assert.IsFalse(rabbit.gameObject.activeSelf, "the station has no rabbit of its own");
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);

            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 4, Kind = "tho-sao", Name = "Sao", Station = 0 });
            runner.State.NextRabbitId = 1;
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 0);
            runner.EquipItem(hat.Id, 1);
            screen.Refresh();
            Assert.IsTrue(rabbit.gameObject.activeSelf);
            Assert.AreEqual("kind_tho-sao", rabbit.GetComponent<Image>().sprite.name, "the collected rabbit itself");
            Assert.IsTrue(rabbit.Find("HatOn").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Station am-tra/Halo").gameObject.activeSelf, "gold and red rabbits get a warm light");
            for (int i = 0; i < 40; i++) screen.Advance(0.1f);
            Assert.AreEqual("kind_tho-sao", rabbit.GetComponent<Image>().sprite.name, "the station's work frames do not take over");

            runner.ToggleRabbitWork(1); // off to rest
            screen.Refresh();
            Assert.IsFalse(rabbit.gameObject.activeSelf, "nobody stands there now: no rabbit is drawn");
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);
        }

        [Test]
        public void A_Double_Tap_On_A_Station_Does_Not_Zoom_But_One_On_Open_Ground_Does()
        {
            var station = Find("Canvas/Garden/Content/Station am-tra");
            station.onClick.Invoke();
            station.onClick.Invoke();
            screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "tapping a rabbit fast is how you speed it up, not how you zoom");

            var ground = Find("Canvas/Garden");
            ground.onClick.Invoke();
            ground.onClick.Invoke();
            screen.Advance(1f); // let the zoom settle
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            ground.onClick.Invoke();
            ground.onClick.Invoke();
            screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "and again comes back out");
        }

        // ----- issue 35: fast taps must never leave anything bigger than it started -----

        [Test]
        public void Upgrading_A_Station_Fast_Leaves_It_The_Size_It_Was()
        {
            runner.State.Coins = BigNum.FromParts(1, 30);
            screen.Refresh();
            Transform station = root.transform.Find("Canvas/Garden/Content/Station am-tra");
            float home = GardenScreen.StationSize(0);
            Assert.AreEqual(home, station.localScale.x, 1e-4f);
            Button buy = Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy");
            float biggest = 0f;
            for (int i = 0; i < 30; i++)
            {
                buy.onClick.Invoke();
                screen.Advance(0.05f); // well inside the 0.2 s pop of the press before
                biggest = Mathf.Max(biggest, station.localScale.x);
            }
            for (int i = 0; i < 30; i++) screen.Advance(0.05f);
            Assert.AreEqual(31, runner.State.Levels[0]);
            Assert.AreEqual(home, station.localScale.x, 1e-4f, "back to the size it started at");
            Assert.AreEqual(home, station.localScale.y, 1e-4f);
            Assert.LessOrEqual(biggest, home * 1.2f, "and never much bigger on the way");
        }

        [Test]
        public void Tapping_A_Rabbit_Fast_Neither_Zooms_Nor_Swells_It()
        {
            var station = Find("Canvas/Garden/Content/Station am-tra");
            Transform actor = station.transform, rabbit = actor.Find("Rabbit");
            float home = GardenScreen.StationSize(0), biggest = 0f;
            for (int i = 0; i < 30; i++)
            {
                station.onClick.Invoke();
                screen.Advance(0.1f);
                Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "tap " + i);
                biggest = Mathf.Max(biggest, Mathf.Max(rabbit.localScale.x, rabbit.localScale.y));
                Assert.AreEqual(home, actor.localScale.x, home * 0.2f);
            }
            Assert.LessOrEqual(biggest, 1.2f, "the squish stays a squish");
            for (int i = 0; i < 30; i++) screen.Advance(0.1f);
            Assert.AreEqual(home, actor.localScale.x, 1e-4f);
            Assert.AreEqual(1f, rabbit.localScale.x, 0.06f, "only breathing is left");
            Assert.AreEqual(1f, rabbit.localScale.y, 0.06f);
        }

        [Test]
        public void Taps_And_Upgrades_Mixed_Together_Fast_Leave_Everything_As_It_Was()
        {
            runner.State.Coins = BigNum.FromParts(1, 30);
            screen.Refresh();
            var station = Find("Canvas/Garden/Content/Station am-tra");
            Button buy = Find("Canvas/Card/Buy");
            float home = GardenScreen.StationSize(0), biggest = 0f;
            for (int i = 0; i < 40; i++)
            {
                if (i % 2 == 0) station.onClick.Invoke(); else buy.onClick.Invoke();
                screen.Advance(0.04f);
                biggest = Mathf.Max(biggest, station.transform.localScale.x);
            }
            for (int i = 0; i < 40; i++) screen.Advance(0.05f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Assert.AreEqual(home, station.transform.localScale.x, 1e-4f);
            Assert.LessOrEqual(biggest, home * 1.2f);
            var card = (RectTransform)root.transform.Find("Canvas/Card");
            Assert.AreEqual(1f, card.localScale.x, 1e-3f, "the card is its own size too");
        }

        [Test]
        public void Only_The_Next_Two_Locked_Stations_Show()
        {
            var world = root.transform.Find("Canvas/Garden/Content");
            int open = 0;
            for (int i = 0; i < runner.Defs.Count; i++) if (runner.State.Levels[i] > 0) open = i + 1;
            runner.State.Plots = runner.Defs.Count;
            screen.Refresh();
            for (int i = 0; i < runner.Defs.Count; i++)
                Assert.AreEqual(i < open + 2, world.Find("Station " + runner.Defs[i].Id).gameObject.activeSelf, runner.Defs[i].Id);
        }

        [Test]
        public void The_Hint_Goes_Away_For_Good_After_The_First_Drag()
        {
            runner.State.HintSeen = false;
            screen.DismissHint();
            Assert.IsTrue(runner.State.HintSeen);
        }

        [Test]
        public void The_Teapot_Is_The_Centrepiece_And_Ingredients_Stand_By_What_They_Supply()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Assert.AreEqual(world.sizeDelta.x / 2f, GardenScreen.StationSpot(0).x, 0.5f, "teapot in the middle");
            float teapot = world.Find("Station am-tra").localScale.x;
            for (int i = 1; i < runner.Defs.Count; i++)
                Assert.Greater(teapot, world.Find("Station " + runner.Defs[i].Id).localScale.x * 1.3f, "teapot is clearly the largest");

            for (int i = 0; i < runner.Defs.Count; i++)
            {
                if (runner.Defs[i].Kind != VuonTra.Core.StationKind.Ingredient) continue;
                for (int j = 0; j < runner.Defs.Count; j++)
                    if (runner.Defs[i].Boosts(runner.Defs[j].Id))
                        Assert.Less(Vector2.Distance(GardenScreen.StationSpot(i), GardenScreen.StationSpot(j)), 520f, runner.Defs[i].Id + " is near " + runner.Defs[j].Id);
            }
        }

        [Test]
        public void Ingredients_Travel_To_The_Stations_That_Use_Them()
        {
            screen.Shell.StartGame();
            runner.State.Levels[1] = 3; // the leaf basket supplies the teapot
            var world = root.transform.Find("Canvas/Garden/Content");
            for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);
            Assert.IsNotNull(world.Find("Courier"), "a courier sets off once both ends are open");
            for (int i = 0; i < 600; i++) screen.Advance(1f / 30f);
            int couriers = 0;
            foreach (Transform child in world) if (child.name == "Courier" && child.gameObject.activeSelf) couriers++;
            Assert.LessOrEqual(couriers, 8, "they arrive and leave; they do not pile up");
        }

        [Test]
        public void Energy_Button_Starts_A_Boost_And_Then_Waits()
        {
            Button energy = Find("Canvas/Hud/Energy");
            screen.Refresh();
            Assert.IsTrue(energy.interactable, "the bar starts full");
            energy.onClick.Invoke();
            Assert.AreEqual(Energy.BoostSeconds, runner.State.BoostSecondsLeft);
            Assert.AreEqual(50, runner.State.Energy, 1e-9);
            screen.Refresh();
            Assert.IsFalse(energy.interactable);
            StringAssert.StartsWith("×2", root.transform.Find("Canvas/Hud/Energy/Face/Action").GetComponent<Text>().text);
        }

        [Test]
        public void Rabbit_Card_Trains_And_Dresses_A_Rabbit()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "Bông" });
            runner.State.NextRabbitId = 1;
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 2);
            runner.State.Coins = new BigNum(1000);
            screen.Refresh();

            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1").onClick.Invoke();
            Find("Canvas/RabbitSheet/Card/Body/LevelUp").onClick.Invoke();
            Assert.AreEqual(2, runner.State.Rabbits[0].Level);
            Assert.AreEqual(900, runner.State.Coins.ToDouble(), 1e-6);

            screen.Refresh();
            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1").onClick.Invoke();
            Find("Canvas/RabbitSheet/Card/Body/Hat").onClick.Invoke();
            Assert.IsTrue(screen.Picker.IsOpen);
            Find("Canvas/Picker/Card/Scroll/Content/Item " + hat.Id + "/Use").onClick.Invoke();
            Assert.AreEqual(1, hat.WornBy);

            screen.Refresh();
            Find("Canvas/Picker/Card/Scroll/Content/Item " + hat.Id + "/Use").onClick.Invoke();
            Assert.AreEqual(0, hat.WornBy, "the same button takes it off again");
            Find("Canvas/Picker/Card/Close").onClick.Invoke();
            Assert.IsFalse(screen.Picker.IsOpen);
        }

        [Test]
        public void Daily_Quest_Gives_Something_To_Wear()
        {
            for (int i = 0; i < 50; i++) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim").onClick.Invoke();
            Assert.AreEqual(1, runner.State.Items.Count);
            Assert.IsNotNull(runner.LastQuestItem);
        }

        [Test]
        public void Switching_Language_Rewrites_The_Screen_And_Keeps_The_Game()
        {
            screen.Shell.StartGame();
            runner.State.Coins = new BigNum(12345);
            runner.State.Levels[0] = 7;
            screen.OpenSheet(1);
            screen.Shell.OpenPause();
            Assert.AreEqual("Tạm nghỉ", root.transform.Find("Canvas/Pause/Card/Heading").GetComponent<Text>().text);

            L10n.SetLocale("en");
            screen.RebuildUi(); // in play the language-changed event calls this

            Assert.AreEqual("Paused", root.transform.Find("Canvas/Pause/Card/Heading").GetComponent<Text>().text);
            Assert.AreEqual(ShellState.Paused, screen.Shell.State, "back on the card with the language buttons");
            Assert.IsTrue(screen.SheetOpen);
            Assert.AreEqual(1, screen.Tab);
            Assert.AreEqual("Dew Teapot", root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Name").GetComponent<Text>().text);
            Assert.AreEqual("Lv 7", root.transform.Find("Canvas/Garden/Content/Station am-tra/Level/Text").GetComponent<Text>().text);
            Assert.AreEqual("Warm Hands", root.transform.Find("Canvas/Sheet/Panel/Upgrades/Content/Upgrade tay-am/Name").GetComponent<Text>().text);
            StringAssert.StartsWith("Tap rabbits", root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Title").GetComponent<Text>().text);
            Assert.AreEqual(7, runner.State.Levels[0]);
            for (int i = 0; i < 30; i++) screen.Advance(1f / 30f);
            Assert.AreEqual("12.3K", root.transform.Find("Canvas/Hud/Purse/Coins").GetComponent<Text>().text, "English decimal point");
            Assert.AreEqual(1, root.transform.childCount - 1 >= 0 ? CountCanvases() : 0, "the old screen is gone");
        }

        int CountCanvases()
        {
            int n = 0;
            foreach (Transform child in root.transform)
                if (child.GetComponent<Canvas>() != null) n++;
            return n;
        }

        [Test]
        public void Language_Buttons_Switch_Through_The_Package()
        {
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            Find("Canvas/Pause/Card/Language en").onClick.Invoke();
            Assert.AreEqual("en", L10n.Code);
        }

        [Test]
        public void An_Arrow_Marks_Stations_That_Can_Be_Afforded()
        {
            GameObject arrow = root.transform.Find("Canvas/Garden/Content/Station am-tra/CanUp").gameObject;
            screen.Refresh();
            Assert.IsFalse(arrow.activeSelf, "broke");
            runner.State.Coins = new BigNum(50);
            screen.Refresh();
            Assert.IsTrue(arrow.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station gio-la/CanUp").gameObject.activeSelf, "120 to open, only 50 in hand");
        }

        [Test]
        public void Catching_The_Golden_Firefly_Pays()
        {
            screen.Shell.StartGame();
            Transform gold = root.transform.Find("Canvas/GoldFirefly");
            Assert.IsFalse(gold.gameObject.activeSelf);
            screen.SpawnGoldFirefly();
            Assert.IsTrue(gold.gameObject.activeSelf);
            gold.GetComponent<Button>().onClick.Invoke();
            Assert.IsFalse(gold.gameObject.activeSelf);
            Assert.AreEqual(30, runner.State.Coins.ToDouble(), 1e-6, "45 seconds of the starting income");
            gold.GetComponent<Button>().onClick.Invoke();
            Assert.AreEqual(30, runner.State.Coins.ToDouble(), 1e-6, "one catch, one prize");
        }

        [Test]
        public void An_Uncaught_Firefly_Leaves_On_Its_Own()
        {
            screen.Shell.StartGame();
            screen.SpawnGoldFirefly();
            for (int i = 0; i < 30 * 13; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(root.transform.Find("Canvas/GoldFirefly").gameObject.activeSelf);
        }

        [Test]
        public void Rabbits_Doze_And_Waking_One_Gives_Tea_Power()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 10;
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Transform bubble = root.transform.Find("Canvas/Garden/Content/Station am-tra/Doze");
            Assert.IsFalse(bubble.gameObject.activeSelf);
            for (int i = 0; i < 30 * 20; i++) screen.Advance(1f / 30f); // the first nap comes within 20 seconds
            Assert.IsTrue(bubble.gameObject.activeSelf, "the only open station is the one that dozes");

            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.IsFalse(bubble.gameObject.activeSelf);
            Assert.AreEqual(18, runner.State.Energy, 1e-6);
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(18, runner.State.Energy, 1e-6, "no reward for tapping an awake rabbit");
        }

        [Test]
        public void A_Rabbit_Can_Be_Renamed_From_Its_Card()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();

            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1").onClick.Invoke();
            Find("Canvas/RabbitSheet/Card/Body/Rename").onClick.Invoke();
            Assert.IsTrue(screen.Rename.IsOpen);
            var input = root.transform.Find("Canvas/Rename/Card/Input").GetComponent<InputField>();
            Assert.AreEqual("Bông", input.text, "starts from the current name");

            input.text = "   ";
            Find("Canvas/Rename/Card/Save").onClick.Invoke();
            Assert.IsTrue(screen.Rename.IsOpen, "an empty name is not accepted");
            Assert.AreEqual("Bông", runner.State.Rabbits[0].Name);

            input.text = "  Bánh Trôi  ";
            Find("Canvas/Rename/Card/Save").onClick.Invoke();
            Assert.IsFalse(screen.Rename.IsOpen);
            Assert.AreEqual("Bánh Trôi", runner.State.Rabbits[0].Name);
            screen.Refresh();
            Assert.AreEqual("Bánh Trôi", root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Name").GetComponent<Text>().text);
            Assert.AreEqual("Bánh Trôi", root.transform.Find("Canvas/Garden/Content/Station am-tra/Tier/Text").GetComponent<Text>().text);

            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1").onClick.Invoke();
            Find("Canvas/RabbitSheet/Card/Body/Rename").onClick.Invoke();
            Find("Canvas/Rename/Card/Cancel").onClick.Invoke();
            Assert.IsFalse(screen.Rename.IsOpen);
            Assert.AreEqual("Bánh Trôi", runner.State.Rabbits[0].Name);
        }

        [Test]
        public void Worn_Items_Show_On_The_Rabbit_In_The_Garden_And_In_The_List()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 0);    // straw hat
            Item charm = Items.Grant(runner.State, runner.ItemDefs[3], 0);  // silver bell
            screen.Refresh();
            Transform rabbit = root.transform.Find("Canvas/Garden/Content/Station am-tra/Rabbit");
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/HatOn"));

            runner.EquipItem(hat.Id, 1);
            runner.EquipItem(charm.Id, 1);
            screen.Refresh();
            Assert.IsTrue(rabbit.Find("HatOn").gameObject.activeSelf);
            Assert.IsTrue(rabbit.Find("CharmOn").gameObject.activeSelf);
            Assert.AreEqual("item_mu-rom", rabbit.Find("HatOn").GetComponent<Image>().sprite.name);
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/HatOn"));
            screen.Detail.Open(runner.Defs, runner.State, 1);
            Assert.IsNotNull(root.transform.Find("Canvas/RabbitSheet/Card/Body/Hat/Icon"), "the slot on the rabbit's sheet shows the item's picture");
            Assert.IsNotNull(root.transform.Find("Canvas/RabbitSheet/Card/Body/Portrait/Tile/HatOn"), "and the big portrait wears it");
            screen.Detail.Close();

            runner.UnequipItem(hat.Id);
            screen.Refresh();
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);
        }

        // ----- #43: a rabbit is drawn only where one of the player's rabbits really stands -----

        Transform Station(string id) => root.transform.Find("Canvas/Garden/Content/Station " + id);

        [Test]
        public void Nobody_Stands_At_A_Station_Until_One_Of_Your_Rabbits_Is_Put_There()
        {
            // Open, nobody there: the prop works in full colour and an empty pad with a plus marks the rabbit's place.
            Transform teapot = Station("am-tra");
            Assert.IsFalse(teapot.Find("Rabbit").gameObject.activeSelf);
            Assert.IsTrue(teapot.Find("Pad").gameObject.activeSelf);
            Assert.IsFalse(teapot.Find("Ground").gameObject.activeSelf);
            Assert.AreEqual(Color.white, teapot.Find("Prop").GetComponent<Image>().color);
            Assert.AreEqual("Cấp 1", teapot.Find("Level/Text").GetComponent<Text>().text);

            // A plot you own with nothing built: bare ground with a sign, the prop as a shadow, no rabbit, no pad.
            Transform basket = Station("gio-la");
            Assert.IsFalse(basket.Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(basket.Find("Pad").gameObject.activeSelf);
            Assert.IsTrue(basket.Find("Ground").gameObject.activeSelf);
            Assert.IsTrue(basket.Find("Prop").gameObject.activeSelf);
            Assert.Less(basket.Find("Prop").GetComponent<Image>().color.a, 0.6f, "only a shadow of what will stand here");
            Assert.IsFalse(basket.Find("Doze").gameObject.activeSelf, "no sleeping rabbit on empty land");
            Text sign = basket.Find("Level/Text").GetComponent<Text>();
            Assert.AreEqual("Còn thiếu 120", sign.text, "short of carrots: the sign says how many");
            runner.State.Coins = new BigNum(1000);
            screen.Refresh();
            Assert.AreEqual("Xây · 120", sign.text);
            Assert.AreEqual(Palette.Carrot, basket.Find("Level").GetComponent<Image>().color, "lit when it can be paid for");

            // Land the garden has not grown into: only a sign under the mist, nothing else.
            for (int i = 1; i < 4; i++) runner.State.Levels[i] = 1;
            screen.Refresh();
            Transform oven = Station("lo-banh");
            Assert.IsTrue(oven.gameObject.activeSelf, "the plot after the last one owned stands as land");
            Assert.IsFalse(oven.Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(oven.Find("Pad").gameObject.activeSelf);
            Assert.IsFalse(oven.Find("Prop").gameObject.activeSelf);
            Assert.AreEqual("Mở rộng vườn", oven.Find("Level/Text").GetComponent<Text>().text);

            // One of your rabbits arrives: the rabbit shows and the pad goes.
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Assert.IsTrue(teapot.Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(teapot.Find("Pad").gameObject.activeSelf);
        }

        [Test]
        public void A_Station_With_Nobody_Working_Earns_The_Same_As_Before()
        {
            Assert.AreEqual(1d, Rabbits.StationMultiplier(runner.Defs, runner.State, 0), 1e-12);
            Assert.AreEqual(1d / 1.5, Economy.IncomePerSecond(runner.Defs, runner.State).ToDouble(), 1e-9, "the teapot's base income");
        }

        [Test]
        public void The_Pad_Lists_Your_Rabbits_With_Its_Own_Trade_First_And_Puts_One_To_Work()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 1, Kind = "tho-cup", Name = "Cụp" });    // gatherer
            runner.State.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Kind = "tho-bong", Name = "Bông" });  // brewer: suits the teapot
            runner.State.NextRabbitId = 2;
            screen.Refresh();

            Transform picker = root.transform.Find("Canvas/WorkerPicker");
            Assert.IsFalse(picker.gameObject.activeSelf);
            Find("Canvas/Garden/Content/Station am-tra/Pad").onClick.Invoke();
            Assert.IsTrue(picker.gameObject.activeSelf);
            StringAssert.Contains("Ấm trà sương", picker.Find("Card/Title").GetComponent<Text>().text);
            Transform content = picker.Find("Card/Scroll/Content");
            Assert.AreEqual("Rabbit 2", content.GetChild(0).name, "the rabbit of the station's trade comes first");
            StringAssert.Contains("×1,5", content.GetChild(0).Find("Sub").GetComponent<Text>().text);
            Assert.AreEqual("Rabbit 1", content.GetChild(1).name);
            StringAssert.DoesNotContain("×1,5", content.GetChild(1).Find("Sub").GetComponent<Text>().text);

            Find("Canvas/WorkerPicker/Card/Scroll/Content/Rabbit 2/Put").onClick.Invoke();
            Assert.AreEqual(0, Rabbits.Find(runner.State, 2).Station);
            Assert.IsFalse(picker.gameObject.activeSelf, "done: the list closes");
            screen.Refresh();
            Assert.IsTrue(Station("am-tra").Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(Station("am-tra").Find("Pad").gameObject.activeSelf);
        }

        [Test]
        public void The_Pad_Says_So_When_You_Have_No_Rabbits_Yet()
        {
            Find("Canvas/Garden/Content/Station am-tra/Pad").onClick.Invoke();
            Transform picker = root.transform.Find("Canvas/WorkerPicker");
            Assert.IsTrue(picker.gameObject.activeSelf);
            Assert.IsNotNull(picker.Find("Card/Scroll/Content/Empty"));
            Find("Canvas/WorkerPicker/Card/Close").onClick.Invoke();
            Assert.IsFalse(picker.gameObject.activeSelf);
        }

        [Test]
        public void The_Who_Works_Here_Slot_On_The_Card_Opens_The_Same_List()
        {
            screen.SelectStation(0);
            Find("Canvas/Card/Worker").onClick.Invoke();
            Assert.IsTrue(root.transform.Find("Canvas/WorkerPicker").gameObject.activeSelf);
        }

        [Test]
        public void Only_A_Station_With_A_Rabbit_Can_Doze()
        {
            screen.Shell.StartGame();
            Assert.IsFalse(screen.DozeSomeone(), "nobody is there to nod off");
            Assert.IsFalse(Station("am-tra").Find("Doze").gameObject.activeSelf);
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Assert.IsTrue(screen.DozeSomeone());
            Assert.IsTrue(Station("am-tra").Find("Doze").gameObject.activeSelf);
        }

        [Test]
        public void The_Rabbits_Tab_Counts_The_Stations_Still_Missing_A_Rabbit()
        {
            screen.Shell.StartGame();
            Transform badge = root.transform.Find("Canvas/TabBar/RabbitsBadge");
            Assert.IsTrue(badge.gameObject.activeSelf);
            Assert.AreEqual("1", badge.Find("Text").GetComponent<Text>().text, "the teapot has nobody");
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Assert.IsFalse(badge.gameObject.activeSelf);
            runner.State.Levels[1] = 1;
            screen.Refresh();
            Assert.IsTrue(badge.gameObject.activeSelf);
            Assert.AreEqual("1", badge.Find("Text").GetComponent<Text>().text);
        }

        [Test]
        public void A_Fresh_Garden_Starts_With_One_Rabbit_At_The_Teapot_But_An_Old_Save_Gets_None()
        {
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-test-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                var go = new GameObject("Fresh");
                var fresh = go.AddComponent<GameRunner>();
                fresh.Catalog = runner.Catalog;
                fresh.SaveDirectory = dir;
                fresh.Init();
                Assert.AreEqual(1, fresh.State.Rabbits.Count);
                Assert.AreEqual("tho-bong", fresh.State.Rabbits[0].Kind);
                Assert.AreEqual(0, fresh.State.Rabbits[0].Station);
                Object.DestroyImmediate(go);

                // An older garden with no rabbits stays that way.
                GameState old = GameState.NewGame(runner.Defs, runner.UpgradeDefs.Count);
                old.Levels[1] = 3;
                System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "save.json"), SaveCodec.Encode(runner.Defs, old, runner.UpgradeDefs));
                go = new GameObject("Old");
                var loaded = go.AddComponent<GameRunner>();
                loaded.Catalog = runner.Catalog;
                loaded.SaveDirectory = dir;
                loaded.Init();
                Assert.AreEqual(0, loaded.State.Rabbits.Count);
                Assert.AreEqual(3, loaded.State.Levels[1]);
                Object.DestroyImmediate(go);
            }
            finally
            {
                System.IO.Directory.Delete(dir, true);
            }
        }

        // ----- #25: the station list leads with what each station earns and upgrades under a held finger -----

        [Test]
        public void A_Station_Row_Leads_With_Carrots_A_Second_And_Says_How_Far_To_The_Next_Doubling()
        {
            Transform row = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra");
            Assert.GreaterOrEqual(((RectTransform)row.Find("Icon")).sizeDelta.x, 160f, "a bigger picture");
            Assert.AreEqual("+0,7/giây", row.Find("PerSec").GetComponent<Text>().text, "1 carrot every 1,5 s");
            Assert.Greater(row.Find("PerSec").GetComponent<Text>().fontSize, row.Find("Name").GetComponent<Text>().fontSize, "the number is the main thing");
            Assert.IsTrue(row.Find("Milestone").gameObject.activeSelf);
            Assert.AreEqual("Còn 9 cấp tới ×2", row.Find("Milestone/Text").GetComponent<Text>().text);

            runner.State.Levels[0] = 27;
            screen.Refresh();
            Assert.AreEqual("Còn 23 cấp tới ×2", row.Find("Milestone/Text").GetComponent<Text>().text);
            Assert.AreEqual((27 - 25) / 25f, ((RectTransform)row.Find("Milestone/Fill")).anchorMax.x, 1e-4);
            StringAssert.StartsWith("+", row.Find("PerSec").GetComponent<Text>().text);

            runner.State.Levels[0] = 500;
            screen.Refresh();
            Assert.AreEqual("Đã qua mọi mốc", row.Find("Milestone/Text").GetComponent<Text>().text);
            Assert.AreEqual(1f, ((RectTransform)row.Find("Milestone/Fill")).anchorMax.x, 1e-4);
        }

        [Test]
        public void An_Ingredient_Row_Shows_The_Stations_It_Feeds_As_Pictures()
        {
            runner.State.Levels[1] = 1;
            screen.Refresh();
            Transform row = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la");
            Assert.AreEqual("×1,1", row.Find("PerSec").GetComponent<Text>().text, "what it does to prices is its number");
            Assert.AreEqual("Tiếp sức cho", row.Find("Sub").GetComponent<Text>().text);
            Transform feeds = row.Find("Feeds");
            Assert.AreEqual(2, feeds.childCount, "the teapot and the milk tea stand");
            Assert.AreEqual(runner.Catalog.Stations[0].Prop, feeds.GetChild(0).Find("Prop").GetComponent<Image>().sprite);
            Assert.AreEqual(runner.Catalog.Stations[6].Prop, feeds.GetChild(1).Find("Prop").GetComponent<Image>().sprite);
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Feeds"), "a brew station feeds nobody");
        }

        [Test]
        public void Stations_Not_Yet_Open_Wait_At_The_Bottom_And_The_Next_One_Is_Lit()
        {
            runner.State.Levels[1] = 1;
            runner.State.Coins = new BigNum(1e9);
            screen.Refresh();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content");
            Assert.AreEqual("Row am-tra", content.GetChild(0).name);
            Assert.AreEqual("Row gio-la", content.GetChild(1).name);
            Assert.AreEqual("Row noi-mut", content.GetChild(2).name, "the next one to open comes right after the open ones");
            Color next = content.Find("Row noi-mut/Card").GetComponent<Image>().color;
            Color later = content.Find("Row coi-hoa/Card").GetComponent<Image>().color;
            Color open = content.Find("Row gio-la/Card").GetComponent<Image>().color;
            Assert.AreNotEqual(later, next, "the next station stands out from the ones after it");
            Assert.AreNotEqual(open, next);
            Assert.AreEqual("Mở trạm", content.Find("Row noi-mut/Buy/Face/Top").GetComponent<Text>().text);
            Assert.IsFalse(content.Find("Row noi-mut/Milestone").gameObject.activeSelf, "no milestone bar before it is open");
        }

        [Test]
        public void Holding_The_Upgrade_Button_Keeps_Buying_Faster_And_Faster()
        {
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            var dip = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").GetComponent<PressDip>();
            dip.Hold();
            for (int i = 0; i < 3; i++) dip.Step(0.1f);
            Assert.AreEqual(1, runner.State.Levels[0], "a short press is not a hold yet");
            for (int i = 0; i < 7; i++) dip.Step(0.1f); // one second held
            int afterOne = runner.State.Levels[0];
            Assert.Greater(afterOne, 1, "held long enough: it buys");
            for (int i = 0; i < 10; i++) dip.Step(0.1f); // a second more
            int afterTwo = runner.State.Levels[0];
            Assert.Greater(afterTwo - afterOne, afterOne - 1, "and buys quicker the longer it is held");

            dip.Release();
            dip.Click(); // the click that comes with lifting the finger
            Assert.AreEqual(afterTwo, runner.State.Levels[0], "the release is not one more purchase");
            dip.Click();
            Assert.AreEqual(afterTwo + 1, runner.State.Levels[0], "a plain tap still buys one");
            for (int i = 0; i < 12; i++) dip.Step(1f / 30f);
            Assert.IsFalse(dip.Busy);
        }

        [Test]
        public void Away_Popup_Shows_And_Closes()
        {
            Transform away = root.transform.Find("Canvas/Away");
            Assert.IsFalse(away.gameObject.activeSelf);
            screen.ShowAway(new BigNum(4000), 600);
            Assert.IsTrue(away.gameObject.activeSelf);
            StringAssert.Contains("10 phút", away.Find("Card/Time").GetComponent<Text>().text);
            Find("Canvas/Away/Card/Ok").onClick.Invoke();
            Assert.IsFalse(away.gameObject.activeSelf);
        }
    }
}

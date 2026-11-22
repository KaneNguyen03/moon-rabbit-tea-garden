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
            Assert.AreEqual(5, plot.GetSiblingIndex(), "right after the fourth station (the upgrade-all row comes first)");
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
            Assert.AreEqual(7, plot.GetSiblingIndex());

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
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Outside"), "nothing covers the garden above the sheet");
            Find("Canvas/Garden").onClick.Invoke(); // a touch on open ground
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
                // An unmet everyday kind is a question; an unmet seasonal kind says when it can be met (#8).
                string unmet = kind.Season != Season.None ? L10n.T("album.season.only", L10n.Season(kind.Season)) : "???";
                Assert.AreEqual(seen ? L10n.T("kind." + kind.Id) : unmet, cell.Find("Name").GetComponent<Text>().text);
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
            Assert.IsFalse(world.Find("Walker 0").gameObject.activeSelf, "and nobody out walking");

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
            for (int k = 0; k < 8; k++) screen.Advance(0.5f); // the newcomer walks over first
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
            Assert.AreEqual(0, Items.DefOf(runner.ItemDefs, runner.LastQuestItem.DefId).Price, "the shop's hats are bought, never given");
            Assert.AreNotEqual(ItemEffect.None, runner.LastQuestItem.Effect);
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
            for (int k = 0; k < 8; k++) screen.Advance(0.5f); // the newcomer walks over first
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
            Assert.IsFalse(screen.Workers.IsOpen, "done: the list closes");
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(picker.gameObject.activeSelf, "and is gone once it has slid away");
            screen.Refresh();
            for (int k = 0; k < 8; k++) screen.Advance(0.5f); // the newcomer walks over first
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
            Assert.IsFalse(screen.Workers.IsOpen);
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
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
            Assert.AreEqual("UpgradeAll", content.GetChild(0).name);
            Assert.AreEqual("Row am-tra", content.GetChild(1).name);
            Assert.AreEqual("Row gio-la", content.GetChild(2).name);
            Assert.AreEqual("Row noi-mut", content.GetChild(3).name, "the next one to open comes right after the open ones");
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

        // ----- #27: the quests tab puts what you can collect first -----

        [Test]
        public void Finished_Quests_Rise_To_The_Top_Under_One_Claim_All_Button()
        {
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            Transform ready = content.Find("Ready");
            Assert.IsFalse(ready.gameObject.activeSelf, "nothing to collect yet");
            Assert.AreEqual(0, ready.GetSiblingIndex());

            runner.State.Coins = new BigNum(120);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke(); // finishes "wake the second rabbit"
            for (int i = 0; i < 50; i++) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke(); // and today's taps
            screen.Refresh();
            Assert.IsTrue(ready.gameObject.activeSelf);
            Assert.AreEqual("Ready", content.GetChild(0).name);
            Assert.AreEqual("Quest ngay-cham", content.GetChild(1).name, "today's finished quest");
            Assert.AreEqual("Quest mo-2", content.GetChild(2).name, "and the finished achievement, both above the headings");
            Color lit = content.Find("Quest mo-2/Card").GetComponent<Image>().color;
            Color plain = content.Find("Quest ngay-nang/Card").GetComponent<Image>().color;
            Assert.AreNotEqual(plain, lit, "a finished quest has a brighter card");

            Find("Canvas/Sheet/Panel/Quests/Content/Ready/ClaimAll").onClick.Invoke();
            Assert.IsTrue(runner.State.Claimed.Contains("mo-2"));
            Assert.IsTrue(runner.State.Claimed.Contains("ngay-cham"));
            Assert.AreEqual(1, runner.State.Clovers);
            screen.Refresh();
            Assert.IsFalse(ready.gameObject.activeSelf);
            Assert.IsFalse(content.Find("Quest mo-2").gameObject.activeSelf, "a collected achievement leaves");
            Assert.Greater(content.Find("Quest ngay-cham").GetSiblingIndex(), content.Find("Heading").GetSiblingIndex(), "a collected daily goes back under Today");
        }

        [Test]
        public void An_Unfinished_Quest_Has_No_Button_Only_A_Bar_And_A_Count()
        {
            Transform row = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-nang");
            Assert.IsFalse(row.Find("Claim").gameObject.activeSelf);
            Assert.IsTrue(row.Find("Track").gameObject.activeSelf);
            Assert.AreEqual("0 / 20", row.Find("Count").GetComponent<Text>().text);
            Assert.IsFalse(row.Find("Done").gameObject.activeSelf);

            for (int i = 0; i < 50; i++) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Refresh();
            Transform done = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham");
            Assert.IsTrue(done.Find("Claim").gameObject.activeSelf, "finished: the button");
            Assert.IsFalse(done.Find("Track").gameObject.activeSelf, "and nothing left to measure");
            done.Find("Claim").GetComponent<Button>().onClick.Invoke();
            screen.Refresh();
            Assert.IsFalse(done.Find("Claim").gameObject.activeSelf);
            Assert.IsTrue(done.Find("Done").gameObject.activeSelf, "collected: a note, no button");
            Assert.AreEqual("Xong", done.Find("Count").GetComponent<Text>().text);
        }

        [Test]
        public void Rewards_Are_Chips_With_Pictures()
        {
            Transform row = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham");
            Assert.AreEqual(runner.Catalog.Currency, row.Find("Carrots/Icon").GetComponent<Image>().sprite);
            StringAssert.StartsWith("+", row.Find("Carrots/Text").GetComponent<Text>().text);
            Assert.IsTrue(row.Find("Clovers").gameObject.activeSelf);
            Assert.IsNotNull(runner.Catalog.Clover, "the clover has a picture of its own");
            Assert.AreEqual(runner.Catalog.Clover, row.Find("Clovers/Icon").GetComponent<Image>().sprite);
            Assert.AreEqual("+1", row.Find("Clovers/Text").GetComponent<Text>().text);
            Assert.GreaterOrEqual(row.Find("Carrots/Text").GetComponent<Text>().fontSize, UiKit.TextBody, "big enough to read");
            Assert.IsFalse(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2/Clovers").gameObject.activeSelf, "no clover chip on a quest that pays none");
        }

        [Test]
        public void Today_Says_When_It_Comes_Back()
        {
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            StringAssert.StartsWith("Hôm nay · làm mới sau", content.Find("Heading").GetComponent<Text>().text);
            var offset = System.TimeSpan.FromHours(7);
            Assert.AreEqual(30 * 60, GardenScreen.SecondsToNextDay(new System.DateTimeOffset(2026, 11, 22, 23, 30, 0, offset)), 1e-6);
            Assert.AreEqual(24 * 3600, GardenScreen.SecondsToNextDay(new System.DateTimeOffset(2026, 11, 22, 0, 0, 0, offset)), 1e-6);
            Assert.AreEqual(5 * 3600 + 12 * 60, GardenScreen.SecondsToNextDay(new System.DateTimeOffset(2026, 11, 22, 18, 48, 0, offset)), 1e-6);
        }

        [Test]
        public void A_Collected_Reward_Flies_From_Its_Row_To_The_Purse()
        {
            screen.Shell.StartGame();
            for (int i = 0; i < 50; i++) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Refresh();
            Transform purse = root.transform.Find("Canvas/Hud/Purse");
            Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham/Claim").onClick.Invoke();
            Assert.Greater(screen.Feel.Flying, 0, "carrots and a clover are on their way");
            Transform fly = root.transform.Find("Canvas/Feel/Fly");
            Assert.IsNotNull(fly);
            screen.Advance(0.2f);
            Assert.Greater(screen.Feel.Flying, 0, "still flying after a fifth of a second");
            for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(0, screen.Feel.Flying, "all landed within two seconds");
            Assert.IsNull(root.transform.Find("Canvas/Feel/Fly"));
            Assert.AreEqual(Vector3.one, purse.localScale, "the purse swelled and is back to its size");
        }

        // ----- #36: quick upgrades -----

        [Test]
        public void The_Station_Card_Has_The_Same_Buy_Modes_As_The_Tab_And_They_Remember_Each_Other()
        {
            runner.State.Coins = new BigNum(1e9);
            screen.SelectStation(0);
            Find("Canvas/Card/Modes/Mode Tới mốc").onClick.Invoke();
            Assert.AreEqual(Simulation.BuyToMilestone, runner.State.BuyMode);
            screen.Refresh();
            Assert.AreEqual("Nâng +9", root.transform.Find("Canvas/Card/Buy/Face/Top").GetComponent<Text>().text, "level 1 to the ×2 at 10");
            Assert.AreEqual(Palette.Carrot, root.transform.Find("Canvas/Sheet/Panel/Bar/Modes/Mode Tới mốc").GetComponent<Image>().color, "the tab shows the same choice");
            Assert.AreEqual("Nâng +9", root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy/Face/Top").GetComponent<Text>().text);

            Find("Canvas/Sheet/Panel/Bar/Modes/Mode ×10").onClick.Invoke();
            screen.Refresh();
            Assert.AreEqual("Nâng +10", root.transform.Find("Canvas/Card/Buy/Face/Top").GetComponent<Text>().text, "and the card follows the tab");
            Find("Canvas/Card/Buy").onClick.Invoke();
            Assert.AreEqual(11, runner.State.Levels[0]);
        }

        [Test]
        public void Holding_The_Card_Button_Keeps_Upgrading_Too()
        {
            runner.State.Coins = new BigNum(1e12);
            screen.SelectStation(0);
            var dip = root.transform.Find("Canvas/Card/Buy").GetComponent<PressDip>();
            dip.Hold();
            for (int i = 0; i < 15; i++) dip.Step(0.1f);
            Assert.Greater(runner.State.Levels[0], 3);
            dip.Release();
            dip.Click();
            int after = runner.State.Levels[0];
            for (int i = 0; i < 12; i++) dip.Step(1f / 30f);
            Assert.AreEqual(after, runner.State.Levels[0], "the release bought nothing more");
        }

        [Test]
        public void A_Big_Upgrade_Bumps_Once_And_The_Level_Number_Runs_Up()
        {
            screen.Shell.StartGame();
            runner.State.Coins = new BigNum(1e12);
            runner.SetBuyMode(Simulation.BuyMax);
            screen.Refresh();
            Text chip = root.transform.Find("Canvas/Garden/Content/Station am-tra/Level/Text").GetComponent<Text>();
            Assert.AreEqual("Cấp 1", chip.text);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
            int level = runner.State.Levels[0];
            Assert.Greater(level, 100);
            screen.Refresh();
            Assert.AreEqual("Cấp 1", chip.text, "the number has not jumped");
            screen.Advance(0.1f);
            string midway = chip.text;
            Assert.AreNotEqual("Cấp 1", midway);
            Assert.AreNotEqual(L10n.StationLevel(level), midway, "it is on its way");
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.StationLevel(level), chip.text, "the number lands, with the rank marks the level has earned");
        }

        [Test]
        public void Upgrade_All_Shows_A_Summary_First_And_Spends_Only_The_Share_You_Chose()
        {
            runner.State.Levels[1] = 1;
            runner.State.Coins = new BigNum(1e6);
            screen.Refresh();
            Transform overlay = root.transform.Find("Canvas/UpgradeAll");
            Assert.IsFalse(overlay.gameObject.activeSelf);
            Find("Canvas/Sheet/Panel/Scroll/Content/UpgradeAll/Button").onClick.Invoke();
            Assert.IsTrue(overlay.gameObject.activeSelf);
            Assert.AreEqual(1, runner.State.Levels[0], "nothing bought yet: this is a summary");
            Transform content = overlay.Find("Card/Scroll/Content");
            Assert.Greater(content.childCount, 0);
            Assert.IsNotNull(content.Find("Step am-tra"));
            StringAssert.StartsWith("+", content.Find("Step am-tra/Levels").GetComponent<Text>().text);
            StringAssert.StartsWith("Tổng", overlay.Find("Card/Total").GetComponent<Text>().text);

            Find("Canvas/UpgradeAll/Card/Shares/Share 0.5").onClick.Invoke();
            Assert.AreEqual(0.5, runner.State.SpendShare);
            screen.Refresh();
            double total = UpgradePlan.Total(screen.UpgradeAll.Plan).ToDouble();
            Assert.LessOrEqual(total, 5e5 + 1e-6, "half is kept back");
            Assert.Greater(total, 1e5);

            Find("Canvas/UpgradeAll/Card/Confirm").onClick.Invoke();
            Assert.IsFalse(overlay.gameObject.activeSelf);
            Assert.Greater(runner.State.Levels[0], 1);
            Assert.GreaterOrEqual(runner.State.Coins.ToDouble(), 5e5 - 1e-6);
            Assert.AreEqual(Simulation.BuyOne, runner.State.BuyMode, "the buy mode is untouched");
        }

        [Test]
        public void Upgrade_All_With_Nothing_Affordable_Offers_Nothing()
        {
            Find("Canvas/Sheet/Panel/Scroll/Content/UpgradeAll/Button").onClick.Invoke();
            Transform overlay = root.transform.Find("Canvas/UpgradeAll");
            Assert.IsNotNull(overlay.Find("Card/Scroll/Content/Empty"));
            Assert.IsFalse(overlay.Find("Card/Confirm").GetComponent<Button>().interactable);
            Find("Canvas/UpgradeAll/Card/Cancel").onClick.Invoke();
            Assert.IsFalse(overlay.gameObject.activeSelf);
        }

        [Test]
        public void A_Rabbit_Levels_Up_To_Its_Cap_With_One_Press()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Level = 1 });
            runner.State.NextRabbitId = 1;
            runner.State.Coins = new BigNum(1e9);
            screen.Refresh();
            screen.Detail.Open(runner.Defs, runner.State, 1);
            Button max = Find("Canvas/RabbitSheet/Card/Body/LevelMax");
            Assert.IsTrue(max.interactable);
            StringAssert.StartsWith("+9 cấp", root.transform.Find("Canvas/RabbitSheet/Card/Body/LevelMax/Face/Sub").GetComponent<Text>().text);
            max.onClick.Invoke();
            Assert.AreEqual(10, runner.State.Rabbits[0].Level, "silver stops at 10");
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/RabbitSheet/Card/Body/LevelMax").interactable, "nothing left to buy");
        }

        // ----- #37: the product tray -----

        [Test]
        public void The_Tray_Shows_What_The_Stations_Made_And_A_Touch_Sells_A_Stack()
        {
            screen.Shell.StartGame();
            Transform tray = root.transform.Find("Canvas/Tray");
            screen.Refresh();
            Assert.IsFalse(tray.gameObject.activeSelf, "nothing made yet: no tray");
            Tray.Add(runner.State, 0, false);
            Tray.Add(runner.State, 0, false);
            screen.Refresh();
            Assert.IsTrue(tray.gameObject.activeSelf);
            Assert.IsTrue(tray.Find("Slot 0/Icon").gameObject.activeSelf);
            Assert.AreEqual("product_am-tra", tray.Find("Slot 0/Icon").GetComponent<Image>().sprite.name, "the teapot's own product, now that it is drawn");
            Assert.AreEqual("khay_go", tray.Find("Slot 0").GetComponent<Image>().sprite.name, "on a wooden dish");
            Assert.IsTrue(tray.Find("Slot 0/Count").gameObject.activeSelf);
            Assert.AreEqual("×2", tray.Find("Slot 0/Count/Text").GetComponent<Text>().text);
            Assert.IsFalse(tray.Find("Slot 0/Gold").gameObject.activeSelf);
            Assert.IsFalse(tray.Find("Slot 1/Icon").gameObject.activeSelf);
            Assert.IsFalse(tray.Find("Slot 5").gameObject.activeSelf, "five slots to begin with");
            Assert.IsFalse(tray.Find("Full").gameObject.activeSelf);

            double before = runner.State.Coins.ToDouble();
            double price = Tray.Price(runner.Defs, runner.State, 0, runner.Mods).ToDouble();
            Find("Canvas/Tray/Slot 0").onClick.Invoke();
            Assert.AreEqual(before + price * 2, runner.State.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(0, runner.State.Tray.Count);
            Assert.Greater(screen.Feel.Flying, 0, "the carrots fly to the purse");
            Assert.AreEqual("reward", Sfx.Last);
            screen.Refresh();
            Assert.IsFalse(tray.gameObject.activeSelf);
        }

        [Test]
        public void A_Sweep_Across_The_Tray_Sells_Everything_And_A_Golden_Batch_Pays_A_Clover()
        {
            screen.Shell.StartGame();
            Tray.Add(runner.State, 0, false);
            Tray.Add(runner.State, 0, true);
            screen.Refresh();
            Transform tray = root.transform.Find("Canvas/Tray");
            Assert.IsTrue(tray.Find("Slot 1/Gold").gameObject.activeSelf, "the golden one wears a ring");
            screen.SweepTray(60f);
            Assert.AreEqual(2, runner.State.Tray.Count, "too short to be a sweep");
            screen.SweepTray(-200f);
            Assert.AreEqual(0, runner.State.Tray.Count);
            Assert.AreEqual(1, runner.State.GoldenCarrots, "a golden batch pays a golden carrot");
            Assert.IsTrue(root.transform.Find("Canvas/Hud/Golden").gameObject.activeSelf, "the chip shows once there is one");
        }

        [Test]
        public void The_Tray_Hides_Under_The_Sheet_And_The_Card_And_Says_When_It_Is_Full()
        {
            screen.Shell.StartGame();
            Tray.Add(runner.State, 0, false);
            screen.Refresh();
            Transform tray = root.transform.Find("Canvas/Tray");
            Assert.IsTrue(tray.gameObject.activeSelf);
            screen.OpenSheet(0);
            screen.Refresh();
            Assert.IsFalse(tray.gameObject.activeSelf, "the sheet covers its place");
            screen.CloseSheet();
            screen.Refresh();
            Assert.IsTrue(tray.gameObject.activeSelf);
            screen.SelectStation(0);
            screen.Refresh();
            Assert.IsFalse(tray.gameObject.activeSelf, "so does the station card");
            screen.SelectStation(-1);

            for (int station = 0; station < Tray.StartingSlots; station++)
                for (int k = 0; k < Tray.StackLimit; k++) Tray.Add(runner.State, station, false);
            screen.Refresh();
            Assert.IsTrue(tray.gameObject.activeSelf);
            Assert.IsTrue(tray.Find("Full").gameObject.activeSelf);
            Assert.AreEqual("Khay đầy", tray.Find("Full/Text").GetComponent<Text>().text);
        }

        [Test]
        public void A_Finished_Batch_Flies_From_Its_Station_Down_To_The_Tray()
        {
            screen.Shell.StartGame();
            Tray.Add(runner.State, 0, false);
            screen.Refresh();
            Tray.Add(runner.State, 0, false); // what the tick would have added
            screen.OnProductMade(0);
            Assert.Greater(screen.Feel.Flying, 0);
            Assert.AreEqual("×2", root.transform.Find("Canvas/Tray/Slot 0/Count/Text").GetComponent<Text>().text);
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(0, screen.Feel.Flying);
            Assert.AreEqual(Vector3.one, root.transform.Find("Canvas/Tray/Slot 0").localScale, "the slot popped and settled");
        }

        // ----- #18: photo mode (the Mac part) -----

        [Test]
        public void Photo_Mode_Hides_The_Hud_Snaps_A_Picture_To_A_File_And_Gives_Todays_Gift_Once()
        {
            screen.Shell.StartGame();
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Tray.Add(runner.State, 0, false);
            screen.Refresh();
            Transform hud = root.transform.Find("Canvas/Hud"), bar = root.transform.Find("Canvas/TabBar"), tray = root.transform.Find("Canvas/Tray");
            Transform photoBar = root.transform.Find("Canvas/PhotoBar"), review = root.transform.Find("Canvas/PhotoReview");
            Assert.IsTrue(hud.gameObject.activeSelf);
            Assert.IsFalse(photoBar.gameObject.activeSelf);

            Find("Canvas/Hud/Camera").onClick.Invoke();
            Assert.IsTrue(screen.PhotoMode);
            Assert.IsFalse(hud.gameObject.activeSelf, "nothing in the way of the garden");
            Assert.IsFalse(bar.gameObject.activeSelf);
            Assert.IsFalse(tray.gameObject.activeSelf);
            Assert.IsTrue(photoBar.gameObject.activeSelf);
            screen.Refresh();
            Assert.IsFalse(tray.gameObject.activeSelf, "and it stays away while framing");

            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-photo-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                screen.PhotoFolder = dir;
                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                Assert.AreEqual("shutter", Sfx.Last);
                Assert.IsNotNull(screen.LastPhotoPath);
                Assert.IsTrue(System.IO.File.Exists(screen.LastPhotoPath));
                Assert.Greater(new System.IO.FileInfo(screen.LastPhotoPath).Length, 1000, "a real picture");
                StringAssert.EndsWith(".png", screen.LastPhotoPath);
                Assert.IsTrue(review.gameObject.activeSelf);
                Assert.IsNotNull(review.Find("Card/Frame/Picture").GetComponent<Image>().sprite);
                Assert.AreEqual(1, runner.State.GoldenCarrots, "the day's gift");
                StringAssert.Contains("+1", review.Find("Card/Gift").GetComponent<Text>().text);

                Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
                Assert.IsFalse(review.gameObject.activeSelf);
                Assert.IsTrue(photoBar.gameObject.activeSelf, "back to framing");
                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                Assert.AreEqual(1, runner.State.GoldenCarrots, "one gift a day");
                Assert.AreEqual("Hôm nay đã nhận quà chụp ảnh rồi", review.Find("Card/Gift").GetComponent<Text>().text);
                Assert.AreEqual(2, System.IO.Directory.GetFiles(dir, "*.png").Length);
                Assert.AreEqual(2, runner.State.PhotosTaken);
            }
            finally
            {
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }

            Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
            Find("Canvas/PhotoBar/Done").onClick.Invoke();
            Assert.IsFalse(screen.PhotoMode);
            Assert.IsTrue(hud.gameObject.activeSelf);
            Assert.IsTrue(bar.gameObject.activeSelf);
            Assert.IsTrue(tray.gameObject.activeSelf, "the tray is back with the rest");
        }

        [Test]
        public void Photo_Mode_Is_For_Playing_Only_And_Closes_Whatever_Was_Open()
        {
            screen.EnterPhotoMode();
            Assert.IsFalse(screen.PhotoMode, "not from the title");
            screen.Shell.StartGame();
            screen.OpenSheet(0);
            screen.SelectStation(0);
            screen.EnterPhotoMode();
            Assert.IsTrue(screen.PhotoMode);
            Assert.IsFalse(screen.SheetOpen);
            Assert.AreEqual(-1, screen.Selected);
            screen.ExitPhotoMode();
            Assert.IsFalse(screen.PhotoMode);
        }

        [Test]
        public void A_Link_That_Succeeds_Pays_The_Gift_Once_Through_The_Runner()
        {
            runner.Account = new FakeLinkedAccount();
            int calls = 0; bool lastOk = false, lastGift = false;
            runner.LinkAccount((ok, gift) => { calls++; lastOk = ok; lastGift = gift; });
            Assert.AreEqual(1, calls);
            Assert.IsTrue(lastOk);
            Assert.IsTrue(lastGift);
            Assert.AreEqual(VuonTra.Core.Profile.LinkRewardClovers, runner.State.Clovers);
            runner.LinkAccount((ok, gift) => { lastOk = ok; lastGift = gift; });
            Assert.IsTrue(lastOk);
            Assert.IsFalse(lastGift, "the gift came once");
            Assert.AreEqual(VuonTra.Core.Profile.LinkRewardClovers, runner.State.Clovers);
        }

        /// <summary>A provider that links at once, for the gift to be tested without a store.</summary>
        sealed class FakeLinkedAccount : IAccountService
        {
            public AccountProvider Provider => AccountProvider.Apple;
            public BackupState State { get; private set; } = BackupState.LocalOnly;
            public bool CanLink => true;
            public string LinkedName => State == BackupState.Linked ? "Test" : "";
            public event System.Action Changed { add { } remove { } }
            public void Link(System.Action<bool> onDone) { State = BackupState.Linked; onDone(true); }
            public void Unlink() { State = BackupState.LocalOnly; }
        }

        [Test]
        public void Photo_Options_Put_A_Frame_Round_The_Picture_And_Can_Hide_Names_And_Fireflies()
        {
            GardenScreen.PhotoFrameOn = true;
            GardenScreen.PhotoNamesOn = true;
            GardenScreen.PhotoFirefliesOn = true;
            screen.Shell.StartGame();
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            runner.State.Profile.GardenName = "Vườn Mây";
            screen.Refresh();
            Transform frame = root.transform.Find("Canvas/PhotoFrame");
            Transform fireflies = root.transform.Find("Canvas/Garden/Content/Fireflies");
            Transform chip = root.transform.Find("Canvas/Garden/Content/Station am-tra/Tier");
            Assert.IsFalse(frame.gameObject.activeSelf, "no frame outside photo mode");
            Assert.IsTrue(fireflies.gameObject.activeSelf);

            screen.EnterPhotoMode();
            Assert.IsTrue(frame.gameObject.activeSelf);
            Assert.AreEqual("Vườn Mây", frame.Find("GardenName").GetComponent<Text>().text);
            StringAssert.Contains("/", frame.Find("Date").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("photo.logo"), frame.Find("Logo").GetComponent<Text>().text);
            Assert.IsTrue(chip.gameObject.activeSelf, "the name plate is in the picture by default");

            Find("Canvas/PhotoBar/Options/Names").onClick.Invoke();
            Assert.IsFalse(GardenScreen.PhotoNamesOn);
            Assert.IsFalse(chip.gameObject.activeSelf, "names off: the plate goes");
            Find("Canvas/PhotoBar/Options/Fireflies").onClick.Invoke();
            Assert.IsFalse(fireflies.gameObject.activeSelf);
            Find("Canvas/PhotoBar/Options/Frame").onClick.Invoke();
            Assert.IsFalse(frame.gameObject.activeSelf);

            screen.ExitPhotoMode();
            Assert.IsTrue(fireflies.gameObject.activeSelf, "the garden has its fireflies back");
            screen.Refresh();
            Assert.IsTrue(chip.gameObject.activeSelf, "and its name plates");
            GardenScreen.PhotoFrameOn = true;
            GardenScreen.PhotoNamesOn = true;
            GardenScreen.PhotoFirefliesOn = true;
        }

        [Test]
        public void The_Album_Keeps_The_Last_Pictures_And_Opens_One()
        {
            screen.Shell.StartGame();
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-album-" + System.Guid.NewGuid().ToString("N"));
            System.IO.Directory.CreateDirectory(dir);
            try
            {
                screen.PhotoFolder = dir;
                screen.EnterPhotoMode();
                Find("Canvas/PhotoBar/Album").onClick.Invoke();
                Transform album = root.transform.Find("Canvas/PhotoAlbum");
                Assert.IsTrue(album.gameObject.activeSelf);
                Assert.IsNotNull(album.Find("Card/Grid/Empty"), "nothing yet");
                screen.CloseAlbum();

                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
                Assert.AreEqual(2, runner.State.Album.Count);
                string newest = runner.State.Album[1];

                Find("Canvas/PhotoBar/Album").onClick.Invoke();
                Assert.IsNull(album.Find("Card/Grid/Empty"));
                Assert.IsNotNull(album.Find("Card/Grid/Photo 0"));
                Assert.IsNotNull(album.Find("Card/Grid/Photo 1"));
                StringAssert.Contains("2", album.Find("Card/Title").GetComponent<Text>().text);
                Find("Canvas/PhotoAlbum/Card/Grid/Photo 0").onClick.Invoke();
                Assert.IsFalse(album.gameObject.activeSelf);
                Assert.IsTrue(root.transform.Find("Canvas/PhotoReview").gameObject.activeSelf, "the newest picture opens for a second look");
                Assert.AreEqual(newest, screen.LastPhotoPath);

                // A file that went missing leaves the album when it is opened again.
                System.IO.File.Delete(runner.State.Album[0]);
                Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
                Find("Canvas/PhotoBar/Album").onClick.Invoke();
                Assert.AreEqual(1, runner.State.Album.Count);
                Assert.IsNull(album.Find("Card/Grid/Photo 1"));
            }
            finally
            {
                screen.ExitPhotoMode();
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }
        }

        [Test]
        public void The_Three_Photo_Quests_Are_In_The_List_And_The_First_Finishes_With_A_Snap()
        {
            screen.Shell.StartGame();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            Assert.IsNotNull(content.Find("Quest anh-1"));
            Assert.IsNotNull(content.Find("Quest anh-vang"));
            Assert.IsNotNull(content.Find("Quest anh-nghi"));
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-quest-" + System.Guid.NewGuid().ToString("N"));
            try
            {
                screen.PhotoFolder = dir;
                screen.EnterPhotoMode();
                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                screen.ExitPhotoMode();
                screen.Refresh();
                Assert.IsTrue(content.Find("Quest anh-1/Claim").gameObject.activeSelf, "the first photo quest is ready to collect");
                Assert.IsFalse(content.Find("Quest anh-vang/Claim").gameObject.activeSelf);
            }
            finally
            {
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }
        }

        // ----- #30: the first minute's guidance -----

        [Test]
        public void The_Guidance_Points_At_One_Thing_At_A_Time_And_Follows_The_Player_Through_Six_Steps()
        {
            Rabbits.GiftFirst(runner.State, "Bông");
            screen.Shell.StartGame();
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            Transform guide = root.transform.Find("Canvas/Tutorial");
            Text text = guide.Find("Plate/Text").GetComponent<Text>();
            Assert.IsTrue(guide.gameObject.activeSelf);
            Assert.AreEqual("1/6", guide.Find("Plate/Progress").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("tut.0"), text.text);
            Assert.AreEqual("Station am-tra", screen.Guide.Target.name, "the hand points at the teapot");
            Assert.IsTrue(guide.Find("Hand").gameObject.activeSelf);
            Assert.IsTrue(guide.Find("DimTop").gameObject.activeSelf, "the rest is dimmed");
            Assert.IsFalse(guide.Find("DimTop").GetComponent<Image>().raycastTarget, "but nothing is locked");
            Assert.IsFalse(root.transform.Find("Canvas/Hint").gameObject.activeSelf, "the old one-line hint stays away");

            // 1: a tap on the teapot.
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.1"), text.text);
            Assert.AreEqual("Buy", screen.Guide.Target.name, "the card opened with the tap: the hand moves to its button");

            // 2: raise it to level 2.
            runner.State.Coins = new BigNum(1000);
            Find("Canvas/Card/Buy").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.2"), text.text);
            Assert.AreEqual("Icon", screen.Guide.Target.name, "the sheet is closed: the hand points at the Stations tab");
            screen.OpenSheet(0);
            screen.Advance(1f / 30f);
            Assert.AreEqual("Row gio-la", screen.Guide.Target.parent.name, "open: the hand points at the second station's button");

            // 3: open the second station.
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.3"), text.text);
            screen.OpenSheet(2);
            screen.Advance(1f / 30f);
            Assert.AreEqual("Quest mo-2", screen.Guide.Target.parent.name, "the finished quest's button");

            // 4: collect it.
            Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2/Claim").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.4"), text.text);
            Assert.AreEqual(1, runner.State.Clovers, "the call step brought the clover it needs");
            Assert.IsTrue(runner.State.TutorialCloverGiven);
            screen.OpenSheet(3);
            screen.Advance(1f / 30f);
            Assert.AreEqual("Button", screen.Guide.Target.name);
            Assert.AreEqual("Summon", screen.Guide.Target.parent.name);

            // 5: call a rabbit.
            Find("Canvas/Sheet/Panel/Rabbits/Content/Summon/Button").onClick.Invoke();
            screen.Arrival.Close();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.5"), text.text);
            Assert.AreEqual("Pad", screen.Guide.Target.name, "the empty pad of the open station");
            Assert.AreEqual("Station gio-la", screen.Guide.Target.parent.name);

            // 6: put it to work.
            long called = runner.State.Rabbits[runner.State.Rabbits.Count - 1].Id;
            runner.AssignRabbit(called, 1);
            screen.Advance(1f / 30f);
            Assert.IsTrue(Tutorial.IsDone(runner.State));
            Assert.IsFalse(guide.gameObject.activeSelf);
            Assert.IsTrue(screen.Feel.BannerVisible, "a word of congratulation");
        }

        [Test]
        public void The_Guidance_Can_Be_Skipped_And_Never_Comes_Back()
        {
            screen.Shell.StartGame();
            screen.Advance(1f / 30f);
            Transform guide = root.transform.Find("Canvas/Tutorial");
            Assert.IsTrue(guide.gameObject.activeSelf);
            Find("Canvas/Tutorial/Plate/Skip").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.IsFalse(guide.gameObject.activeSelf);
            Assert.IsTrue(Tutorial.IsDone(runner.State));
            Assert.IsFalse(screen.Feel.BannerVisible, "skipping is not finishing");
            GameState r = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, runner.State)).State;
            Assert.IsTrue(Tutorial.IsDone(r));
        }

        [Test]
        public void The_Guidance_Stays_Out_Of_Photo_Mode_And_The_Pause_Card()
        {
            screen.Shell.StartGame();
            screen.Advance(1f / 30f);
            Transform guide = root.transform.Find("Canvas/Tutorial");
            Assert.IsTrue(guide.gameObject.activeSelf);
            screen.EnterPhotoMode();
            screen.Advance(1f / 30f);
            Assert.IsFalse(guide.gameObject.activeSelf);
            screen.ExitPhotoMode();
            screen.Advance(1f / 30f);
            Assert.IsTrue(guide.gameObject.activeSelf);
            Find("Canvas/Hud/Settings").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.IsFalse(guide.gameObject.activeSelf);
        }

        // ----- #13: the summon moment -----

        [Test]
        public void Each_Tier_Arrives_To_Its_Own_Bells_And_The_New_Mark_Survives_A_Restart()
        {
            screen.Shell.StartGame();
            runner.State.Clovers = 1;
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Rabbits/Content/Summon/Button").onClick.Invoke();
            Rabbit called = runner.State.Rabbits[runner.State.Rabbits.Count - 1];
            Assert.AreEqual("summon-" + called.Tier, Sfx.Last, "the bells of its tier");
            Assert.IsTrue(screen.IsFreshKind(called.Kind));
            screen.Arrival.Close();

            // Three silver rabbits merge into a green one: the green bells.
            for (int i = 0; i < 3; i++) runner.State.Rabbits.Add(new Rabbit { Id = 100 + i, Tier = 0, Kind = "tho-bong", Name = "S" + i });
            runner.State.NextRabbitId = 102;
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Rabbits/Content/Merge 0/Button").onClick.Invoke();
            Assert.AreEqual("summon-1", Sfx.Last, "a merge arrives to the bells of the tier it made");
            screen.Arrival.Close();

            // The game closes and opens again: the mark is still there, until the kind's page is opened.
            GameState again = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, runner.State), runner.UpgradeDefs).State;
            Assert.IsTrue(again.FreshKinds.Contains(called.Kind));
            runner.LookAtKind(called.Kind);
            Assert.IsFalse(screen.IsFreshKind(called.Kind));
            GameState after = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, runner.State), runner.UpgradeDefs).State;
            Assert.IsFalse(after.FreshKinds.Contains(called.Kind));
        }

        // ----- #21: the time of day, from the clock, as a look -----

        [Test]
        public void The_Garden_Is_Lit_For_The_Hour_And_Night_Is_Its_Own_Look()
        {
            screen.Shell.StartGame();
            Transform fireflies = root.transform.Find("Canvas/Garden/Content/Fireflies");
            Image wash = root.transform.Find("Canvas/Daylight").GetComponent<Image>();
            Image moon = root.transform.Find("Canvas/MoonGlow").GetComponent<Image>();
            int all = fireflies.childCount;
            int Out() { int n = 0; foreach (Transform f in fireflies) if (f.gameObject.activeSelf) n++; return n; }

            screen.ClockHours = () => 22.0;
            screen.Refresh();
            Assert.AreEqual(0f, wash.color.a, 1e-6, "at night nothing lies over the garden");
            Assert.AreEqual(0.26f, moon.color.a, 1e-6);
            Assert.AreEqual(all, Out());
            double before = runner.State.Coins.ToDouble();

            screen.ClockHours = () => 13.0;
            screen.Refresh();
            Assert.Greater(wash.color.a, 0.1f, "a pale warmth by day");
            Assert.Less(moon.color.a, 0.26f, "the moon's glow fades by day");
            Assert.AreEqual(Daylight.FirefliesAt(13, all), Out(), "only a few fireflies by day");
            Assert.AreEqual(before, runner.State.Coins.ToDouble(), 1e-9, "the hour changes the look, never the carrots");

            screen.ClockHours = () => 18.0;
            screen.Refresh();
            Assert.Greater(wash.color.r, wash.color.b, "dusk is amber");
            Assert.AreEqual(all / 2, Out());

            screen.ClockHours = () => 6.0;
            screen.Refresh();
            Assert.Greater(wash.color.a, 0f, "dawn has its wash too");
            screen.ClockHours = () => 3.0;
            screen.Refresh();
            Assert.AreEqual(0f, wash.color.a, 1e-6);
            Assert.AreEqual(all, Out());
        }

        [Test]
        public void A_Speed_Burst_Rims_The_Screen_With_Light_Until_It_Runs_Out()
        {
            screen.Shell.StartGame();
            Transform rim = root.transform.Find("Canvas/BoostEdge");
            Assert.IsFalse(rim.gameObject.activeSelf);
            runner.State.Energy = Energy.Max;
            Assert.IsTrue(runner.Boost());
            for (int i = 0; i < 15; i++) screen.Advance(1f / 30f);
            Assert.IsTrue(rim.gameObject.activeSelf);
            Image bottom = rim.Find("Bottom").GetComponent<Image>();
            Assert.Greater(bottom.color.a, 0.3f, "lit after half a second");
            Assert.AreEqual(4, rim.childCount, "one band on every edge");

            runner.State.BoostSecondsLeft = 0d;
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(rim.gameObject.activeSelf, "gone within half a second of the burst ending");
        }

        // ----- #44 round 1: golden carrots and the shop -----

        [Test]
        public void The_Shop_By_The_Pond_Gives_Todays_Gift_And_Calls_With_Gold()
        {
            screen.Shell.StartGame();
            screen.Refresh();
            Transform shop = root.transform.Find("Canvas/Garden/Content/Shop");
            Assert.IsNotNull(shop, "the shop stands in the garden from the first day");
            Assert.IsTrue(shop.Find("Badge").gameObject.activeSelf, "a gift waits");
            Assert.IsFalse(root.transform.Find("Canvas/Hud/Golden").gameObject.activeSelf, "no golden carrots yet: no chip");

            Find("Canvas/Garden/Content/Shop").onClick.Invoke();
            Assert.IsTrue(screen.Shop.IsOpen);
            Transform card = root.transform.Find("Canvas/Shop/Card");
            StringAssert.Contains("60%", card.Find("Golden/Odds").GetComponent<Text>().text, "the clover odds");
            StringAssert.Contains("35%", card.Find("Golden/Odds").GetComponent<Text>().text, "and the golden odds, side by side");
            Assert.IsTrue(Find("Canvas/Shop/Card/Today/Gift").interactable);
            Assert.IsFalse(Find("Canvas/Shop/Card/Golden/Call").interactable, "ten golden carrots are needed");
            Find("Canvas/Shop/Card/Today/Gift").onClick.Invoke();
            Assert.AreEqual(1, runner.State.GoldenCarrots);
            Assert.AreEqual(1, runner.State.GiftStreak);
            Assert.Greater(screen.Feel.Flying, 0, "the gift flies to its chip");
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Shop/Card/Today/Gift").interactable, "once a day");
            Assert.IsFalse(shop.Find("Badge").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/Hud/Golden").gameObject.activeSelf);
            Assert.AreEqual("1", root.transform.Find("Canvas/Hud/Golden/Text").GetComponent<Text>().text);

            runner.State.GoldenCarrots = Shop.GoldenSummonCost;
            screen.Refresh();
            Assert.IsTrue(Find("Canvas/Shop/Card/Golden/Call").interactable);
            int before = runner.State.Rabbits.Count;
            Find("Canvas/Shop/Card/Golden/Call").onClick.Invoke();
            Assert.AreEqual(before + 1, runner.State.Rabbits.Count);
            Assert.AreEqual(0, runner.State.GoldenCarrots);
            StringAssert.StartsWith("summon-", Sfx.Last);
            Assert.IsFalse(screen.Shop.IsOpen, "the moon gate takes over");
        }

        [Test]
        public void The_Rabbits_Tab_Shows_Both_Calls_Odds_And_Golden_Quests_Show_A_Golden_Chip()
        {
            Transform odds = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Odds");
            Assert.IsNotNull(odds);
            StringAssert.Contains("25%", odds.GetComponent<Text>().text);
            Transform quest = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest loai-tim");
            Assert.IsNotNull(quest, "the golden achievements are in the list");
            Assert.IsTrue(quest.Find("Golden").gameObject.activeSelf);
            Assert.AreEqual("+3", quest.Find("Golden/Text").GetComponent<Text>().text);
            Assert.IsFalse(quest.Find("Clovers").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2/Golden").gameObject.activeSelf);
        }

        [Test]
        public void The_Shop_Shows_Todays_Visitor_And_Opening_The_Shop_Clears_Its_Mark()
        {
            screen.Shell.StartGame();
            runner.ClaimDailyGift();
            screen.Refresh();
            Transform badge = root.transform.Find("Canvas/Garden/Content/Shop/Badge");
            Assert.IsTrue(badge.gameObject.activeSelf, "a visitor not yet looked at");
            screen.OpenShop();
            screen.Refresh();
            Assert.IsFalse(badge.gameObject.activeSelf, "looked at now");
            Transform visitor = root.transform.Find("Canvas/Shop/Card/Visitor");
            Shop.Visitor v = runner.VisitorToday;
            Assert.AreEqual(L10n.T("kind." + v.Kind), visitor.Find("Name").GetComponent<Text>().text);
            Assert.IsNotNull(visitor.Find("PortraitHolder/Tile"));
            Assert.IsFalse(Find("Canvas/Shop/Card/Visitor/Buy").interactable, "no golden carrots");
            runner.State.GoldenCarrots = v.Price;
            screen.Refresh();
            Assert.IsTrue(Find("Canvas/Shop/Card/Visitor/Buy").interactable);
            int before = runner.State.Rabbits.Count;
            Find("Canvas/Shop/Card/Visitor/Buy").onClick.Invoke();
            Assert.AreEqual(before + 1, runner.State.Rabbits.Count);
            Assert.AreEqual(v.Kind, runner.State.Rabbits[before].Kind);
            Assert.AreEqual(0, runner.State.GoldenCarrots);
            screen.Arrival.Close();
            screen.OpenShop();
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Shop/Card/Visitor/Buy").interactable, "gone home already");
            Assert.AreEqual(L10n.T("shop.visitor.bought"), visitor.Find("Buy/Face/Text").GetComponent<Text>().text);
        }

        // ----- #44 round 3: hats for looks, and fur dye -----

        [Test]
        public void The_Shop_Sells_Six_Hats_For_Looks_And_A_Second_Tap_Buys_One()
        {
            screen.Shell.StartGame();
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "Bông" });
            runner.State.NextRabbitId = 1;
            screen.OpenShop();
            screen.Refresh();
            Transform hats = root.transform.Find("Canvas/Shop/Card/Hats");
            Assert.IsNotNull(hats, "a section of things for rabbits");
            var sold = new System.Collections.Generic.List<ItemDef>();
            foreach (ItemDef def in runner.ItemDefs) if (def.Price > 0) sold.Add(def);
            Assert.AreEqual(6, sold.Count, "six hats for sale");
            foreach (ItemDef def in sold)
            {
                Assert.AreEqual(ItemEffect.None, def.Effect, def.Id + " is for looks only");
                Assert.AreEqual(ItemSlot.Hat, def.Slot, def.Id);
                Assert.IsTrue(def.Price >= 20 && def.Price <= 60, def.Id + " costs " + def.Price);
                Transform tile = hats.Find("Row/Hat " + def.Id);
                Assert.IsNotNull(tile, "a tile for " + def.Id);
                Assert.AreEqual(def.Price.ToString(), tile.Find("Price/Text").GetComponent<Text>().text);
                Assert.IsNotNull(tile.Find("Icon").GetComponent<Image>().sprite, def.Id + " has its picture");
                Assert.IsFalse(tile.GetComponent<Button>().interactable, "no golden carrots yet: " + def.Id);
                Assert.IsFalse(tile.Find("Owned").gameObject.activeSelf, def.Id);
            }
            ItemDef first = sold[0];
            runner.State.GoldenCarrots = first.Price + 5;
            screen.Refresh();
            string path = "Canvas/Shop/Card/Hats/Row/Hat " + first.Id;
            Button buy = Find(path);
            Assert.IsTrue(buy.interactable);
            buy.onClick.Invoke();
            Assert.AreEqual(0, runner.State.Items.Count, "the first tap only asks");
            Assert.AreEqual(L10n.T("shop.hats.confirm"), root.transform.Find(path + "/Price/Text").GetComponent<Text>().text);
            buy.onClick.Invoke();
            Assert.AreEqual(1, runner.State.Items.Count, "the second tap buys");
            Item hat = runner.State.Items[0];
            Assert.AreEqual(first.Id, hat.DefId);
            Assert.AreEqual(ItemEffect.None, hat.Effect);
            Assert.AreEqual(0, hat.WornBy, "in the wardrobe, not on a head yet");
            Assert.AreEqual(5, runner.State.GoldenCarrots, "the price was paid");
            screen.Refresh();
            Transform bought = root.transform.Find(path);
            Assert.IsTrue(bought.Find("Owned").gameObject.activeSelf, "one of each");
            Assert.IsFalse(bought.GetComponent<Button>().interactable);
            Assert.AreEqual(L10n.T("item.effect.none"), ItemPickerView.Describe(hat));

            screen.Shop.Close();
            screen.Picker.Open(runner.State, 1, ItemSlot.Hat);
            Transform row = root.transform.Find("Canvas/Picker/Card/Scroll/Content/Item " + hat.Id);
            Assert.IsNotNull(row, "the hat waits in the picker like any item");
            StringAssert.Contains(L10n.T("item.effect.none"), row.Find("Sub").GetComponent<Text>().text);
            Find("Canvas/Picker/Card/Scroll/Content/Item " + hat.Id + "/Use").onClick.Invoke();
            Assert.AreEqual(1, hat.WornBy);
            screen.Picker.Close();
            screen.Refresh();
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/HatOn"), "worn on the head");
        }

        [Test]
        public void Dye_Dots_On_The_Rabbit_Sheet_Tint_The_Rabbit_Everywhere_And_Washing_Brings_The_White_Back()
        {
            Motion.SetReduced(true);
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Image actor = root.transform.Find("Canvas/Garden/Content/Station am-tra/Rabbit").GetComponent<Image>();
            Assert.AreEqual(Color.white, actor.color, "white fur to begin with");
            screen.Detail.Open(runner.Defs, runner.State, 1);
            Transform dye = root.transform.Find("Canvas/RabbitSheet/Card/Body/Dye");
            Assert.IsNotNull(dye, "a row to dye the fur");
            Assert.AreEqual(Shop.Dyes.Length, dye.Find("Dots").childCount, "one dot per colour");
            StringAssert.Contains(L10n.T("need.more"), dye.Find("Label").GetComponent<Text>().text, "no golden carrots");
            Assert.IsFalse(Find("Canvas/RabbitSheet/Card/Body/Dye/Dots/Dot 3").interactable);
            Assert.IsFalse(Find("Canvas/RabbitSheet/Card/Body/Dye/Wash").interactable, "white already");

            runner.State.GoldenCarrots = Shop.DyeCost;
            screen.Refresh();
            Button dot = Find("Canvas/RabbitSheet/Card/Body/Dye/Dots/Dot 3");
            Assert.IsTrue(dot.interactable);
            dot.onClick.Invoke();
            Assert.AreEqual(3, runner.State.Rabbits[0].Dye);
            Assert.AreEqual(0, runner.State.GoldenCarrots, "eight golden carrots paid");
            screen.Refresh();
            Color fur = RabbitGridView.Fur(runner.State.Rabbits[0]);
            Assert.AreNotEqual(Color.white, fur);
            Assert.AreEqual(fur, root.transform.Find("Canvas/RabbitSheet/Card/Body/Portrait/Tile/Art").GetComponent<Image>().color, "the portrait on its sheet");
            Assert.AreEqual(fur, actor.color, "the rabbit at its station");
            Assert.AreEqual(fur, root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/Art").GetComponent<Image>().color, "its tile in the list");
            Assert.IsTrue(root.transform.Find("Canvas/RabbitSheet/Card/Body/Dye/Dots/Dot 3/Ring").gameObject.activeSelf, "the colour it wears is ringed");
            Assert.IsFalse(root.transform.Find("Canvas/RabbitSheet/Card/Body/Dye/Dots/Dot 5/Ring").gameObject.activeSelf);
            Assert.IsFalse(Find("Canvas/RabbitSheet/Card/Body/Dye/Dots/Dot 5").interactable, "another colour costs again");

            Button wash = Find("Canvas/RabbitSheet/Card/Body/Dye/Wash");
            Assert.IsTrue(wash.interactable);
            wash.onClick.Invoke();
            Assert.AreEqual(-1, runner.State.Rabbits[0].Dye);
            Assert.AreEqual(0, runner.State.GoldenCarrots, "washing is free");
            screen.Refresh();
            Assert.AreEqual(Color.white, actor.color);
            Assert.AreEqual(Color.white, root.transform.Find("Canvas/RabbitSheet/Card/Body/Portrait/Tile/Art").GetComponent<Image>().color);
            Assert.IsFalse(Find("Canvas/RabbitSheet/Card/Body/Dye/Wash").interactable);
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

        [Test]
        public void The_Pause_Card_Leads_To_A_Profile_With_Name_Code_Avatar_And_A_Local_Only_Backup()
        {
            string second = RabbitKinds.OfTier(2)[0].Id;
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.Rabbits.Add(new Rabbit { Id = 2, Tier = 2, Kind = second, Name = "Mây" });
            runner.State.NextRabbitId = 2;
            runner.State.GardenXp = Garden.XpForLevel(4);
            string code = runner.State.Profile.Code;
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            screen.Refresh();

            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }
            Assert.AreEqual(L10n.T("profile.unnamed"), T("Canvas/Pause/Card/Profile/Name").text, "an unnamed garden still has a name to show");
            Assert.AreEqual(code, T("Canvas/Pause/Card/Profile/Code").text);

            Find("Canvas/Pause/Card/Profile").onClick.Invoke();
            Assert.IsTrue(screen.Profile.IsOpen);
            Assert.AreEqual(L10n.T("profile.unnamed"), T("Canvas/Profile/Card/Name").text);
            Assert.AreEqual(code, T("Canvas/Profile/Card/Code").text);
            Assert.AreEqual(L10n.T("profile.stats", 4, 2, 1), T("Canvas/Profile/Card/Stats").text, "garden level, rabbits, stations open");
            Image art = root.transform.Find("Canvas/Profile/Card/Avatar/Art").GetComponent<Image>();
            Assert.AreEqual("kind_tho-bong", art.sprite.name, "the first rabbit stands in until one is chosen");

            Find("Canvas/Profile/Card/Avatar").onClick.Invoke();
            Assert.AreEqual(2, runner.State.Profile.AvatarRabbitId, "a touch on the picture picks the next rabbit");
            Assert.AreEqual("kind_" + second, art.sprite.name);

            string copied = null;
            ProfileView.Clipboard = text => copied = text;
            Find("Canvas/Profile/Card/Copy").onClick.Invoke();
            Assert.AreEqual(code, copied);
            Assert.AreEqual(L10n.T("profile.copied"), T("Canvas/Profile/Card/Copy/Face/Text").text);

            Assert.AreEqual(L10n.T("backup.local"), T("Canvas/Profile/Card/Backup").text);
            Assert.IsFalse(Find("Canvas/Profile/Card/Link").interactable, "nothing to link to yet");
            StringAssert.StartsWith(L10n.T("backup.soon"), T("Canvas/Profile/Card/LinkNote").text);
            StringAssert.Contains(L10n.T("backup.reward", VuonTra.Core.Profile.LinkRewardClovers), T("Canvas/Profile/Card/LinkNote").text, "what linking will bring");

            Find("Canvas/Profile/Card/Rename").onClick.Invoke();
            Assert.IsTrue(screen.Rename.IsOpen);
            Assert.AreEqual(L10n.T("profile.rename"), T("Canvas/Rename/Card/Title").text, "the card says it is the garden being named");
            var input = root.transform.Find("Canvas/Rename/Card/Input").GetComponent<InputField>();
            input.text = "  Trà   Trăng ";
            Find("Canvas/Rename/Card/Save").onClick.Invoke();
            Assert.IsFalse(screen.Rename.IsOpen);
            Assert.AreEqual("Trà Trăng", runner.State.Profile.GardenName);
            Assert.AreEqual("Trà Trăng", T("Canvas/Profile/Card/Name").text);

            Find("Canvas/Profile/Card/Close").onClick.Invoke();
            Assert.IsFalse(screen.Profile.IsOpen);
            Assert.AreEqual(ShellState.Paused, screen.Shell.State, "back on the pause card");
            screen.Refresh();
            Assert.AreEqual("Trà Trăng", T("Canvas/Pause/Card/Profile/Name").text);
            Assert.AreEqual("kind_" + second, root.transform.Find("Canvas/Pause/Card/Profile/Avatar/Art").GetComponent<Image>().sprite.name);
        }

        [Test]
        public void Rarity_Reads_By_Stars_As_Well_As_By_Colour_On_Rabbits_Items_And_The_Album()
        {
            string blue = RabbitKinds.OfTier(2)[0].Id, red = RabbitKinds.OfTier(5)[0].Id;
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 2, Kind = blue, Name = "Mây" });
            runner.State.NextRabbitId = 1;
            Item charm = Items.Grant(runner.State, runner.ItemDefs[0], 1);
            screen.OpenSheet(3);
            screen.Refresh();
            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }

            Assert.AreEqual("Xanh dương ★★★", L10n.Rarity(2));
            L10n.SetLocale("en");
            Assert.AreEqual("Blue ★★★", L10n.Rarity(2));
            L10n.SetLocale("vi");

            Assert.AreEqual("★★★", T("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Tile/Stars/Text").text, "the grid tile");
            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1").onClick.Invoke();
            Assert.AreEqual("★★★", T("Canvas/RabbitSheet/Card/Body/Portrait/Tile/Stars/Text").text, "the rabbit's own sheet");
            StringAssert.Contains("★★★", T("Canvas/RabbitSheet/Card/Body/Kind/Text").text, "and its kind line");
            screen.Detail.Close();

            screen.Picker.Open(runner.State, 1, runner.ItemDefs[0].Slot);
            string row = "Canvas/Picker/Card/Scroll/Content/Item " + charm.Id;
            Assert.AreEqual("★★", T(row + "/Tile/Stars/Text").text, "an item of the second tier");
            StringAssert.Contains("★★", T(row + "/Sub").text, "the tier line under the name");
            screen.Picker.Close();

            screen.ShowRabbitAlbum(true);
            screen.Refresh();
            Transform redCell = null;
            foreach (Transform albumRow in root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content")) { redCell = albumRow.Find("Kind " + red); if (redCell != null) break; }
            Assert.IsNotNull(redCell);
            Assert.AreEqual("★★★★★★", redCell.Find("Tile/Stars/Text").GetComponent<Text>().text, "a kind not met yet still shows how rare it is");
        }

        [Test]
        public void A_Station_At_Level_500_Says_So_And_Offers_No_More_Levels()
        {
            runner.State.Levels[0] = Economy.MaxLevel;
            runner.State.Coins = new BigNum(1e300);
            screen.Shell.StartGame();
            screen.OpenSheet(0);
            screen.Refresh();
            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }

            Assert.IsFalse(Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").interactable);
            Assert.AreEqual(L10n.T("row.maxlevel"), T("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy/Face/Top").text);
            Assert.AreEqual("", T("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy/Face/Cost").text);
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station am-tra/CanUp").gameObject.activeSelf, "no 'tap me' arrow at the cap");

            screen.CloseSheet();
            screen.FocusStation(0);
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Card/Buy").interactable);
            Assert.AreEqual(L10n.T("row.maxlevel"), T("Canvas/Card/Buy/Face/Top").text);
            Assert.AreEqual(L10n.StationLevel(Economy.MaxLevel), T("Canvas/Card/Level/Text").text);
        }

        [Test]
        public void A_Rabbit_Wears_A_Hollow_Star_For_Every_Ten_Levels_And_Passing_A_Mark_Is_A_Moment()
        {
            string blue = RabbitKinds.OfTier(2)[0].Id;
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 2, Kind = blue, Name = "Mây", Level = 23, Station = 0 });
            runner.State.NextRabbitId = 1;
            runner.State.Coins = new BigNum(1e12);
            screen.Shell.StartGame();
            screen.OpenSheet(3);
            screen.Refresh();
            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }

            Assert.AreEqual("Cấp 23 ☆☆", T("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Level").text, "the grid cell");
            Assert.AreEqual("Mây ☆☆", T("Canvas/Garden/Content/Station am-tra/Tier/Text").text, "the name plate in the garden");
            Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1").onClick.Invoke();
            Assert.AreEqual("Cấp 23/30 ☆☆", T("Canvas/RabbitSheet/Card/Body/Level").text, "the rabbit's own sheet");

            runner.State.Rabbits[0].Level = 29;
            screen.Refresh();
            Find("Canvas/RabbitSheet/Card/Body/LevelUp").onClick.Invoke();
            Assert.AreEqual(30, runner.State.Rabbits[0].Level);
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.IsTrue(screen.Feel.BannerVisible, "the third star is an occasion");
            StringAssert.Contains("Mây", root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);
            screen.Refresh();
            Assert.AreEqual("Cấp 30/30 ☆☆☆", T("Canvas/RabbitSheet/Card/Body/Level").text);
        }

        [Test]
        public void A_Charm_Card_Says_Its_Effect_Now_And_After_One_More_Level_And_Counts_Levels_In_Dots()
        {
            runner.State.UpgradeLevels[0] = 1; // warm hands
            runner.State.UpgradeLevels[3] = 5; // long sleep, at its cap
            screen.OpenSheet(1);
            screen.Refresh();
            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }
            string list = "Canvas/Sheet/Panel/Upgrades/Content/";

            Assert.AreEqual("+0,2 giây mỗi chạm → +0,4 giây mỗi chạm", T(list + "Upgrade tay-am/Effect").text);
            Assert.AreEqual("×1 cà rốt → ×2 cà rốt", T(list + "Upgrade trang-ram/Effect").text, "not learnt yet: the base game first");
            Assert.AreEqual("+10 giờ vắng mặt · đã đầy", T(list + "Upgrade giac-ngu/Effect").text);
            Assert.IsNull(root.transform.Find(list + "Upgrade tay-am/Level"), "the level chip is gone");

            Transform dots = root.transform.Find(list + "Upgrade tay-am/Dots");
            Assert.AreEqual(10, dots.childCount, "one dot per level the charm can reach");
            Assert.AreEqual(Palette.Carrot, dots.GetChild(0).GetComponent<Image>().color, "the first is lit");
            Assert.AreEqual(Palette.Track, dots.GetChild(1).GetComponent<Image>().color);
            Transform sleepDots = root.transform.Find(list + "Upgrade giac-ngu/Dots");
            Assert.AreEqual(5, sleepDots.childCount);
            for (int i = 0; i < 5; i++) Assert.AreEqual(Palette.Carrot, sleepDots.GetChild(i).GetComponent<Image>().color);

            L10n.SetLocale("en");
            screen.RebuildUi();
            screen.OpenSheet(1);
            screen.Refresh();
            Assert.AreEqual("+0.2 s per tap → +0.4 s per tap", T(list + "Upgrade tay-am/Effect").text);
        }

        [Test]
        public void A_Touch_On_The_Purse_Lists_Where_The_Carrots_Come_From()
        {
            runner.State.Levels[0] = 5;
            runner.State.Levels[1] = 2;
            runner.State.Levels[2] = 3;
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 2, Kind = RabbitKinds.OfTier(2)[0].Id, Name = "Mây", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Shell.StartGame();
            screen.Refresh();
            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }

            Find("Canvas/Hud/Purse").onClick.Invoke();
            Assert.IsTrue(screen.Income.IsOpen);
            string rows = "Canvas/Income/Card/Scroll/Content/";
            BigNum tea = Economy.StationIncomePerSecond(runner.Defs, runner.State, 0, out double rabbit, out double ingredients);
            Assert.AreEqual(L10n.T("income.row", tea.Short()), T(rows + "Station am-tra/Amount").text);
            Assert.AreEqual(L10n.T("income.factors", UiKit.Times(rabbit), UiKit.Times(ingredients)), T(rows + "Station am-tra/Factors").text);
            Assert.IsNotNull(root.transform.Find(rows + "Station noi-mut"), "the second brewing station has a row");
            Assert.IsNull(root.transform.Find(rows + "Station gio-la"), "an ingredient station earns nothing of its own");
            Assert.IsNull(root.transform.Find(rows + "Boost"), "no boost line while none runs");
            BigNum total = Economy.IncomePerSecond(runner.Defs, runner.State) * (runner.Mods.IncomeMultiplier * runner.Mods.SpeedMultiplier);
            Assert.AreEqual(L10n.T("income.total", total.Short()), T(rows + "Total").text);

            Find("Canvas/Income/Card/Close").onClick.Invoke();
            Assert.IsFalse(screen.Income.IsOpen);
        }

        [Test]
        public void A_Tea_Bar_Short_Of_Power_Says_How_Long_Until_A_Burst_And_A_Full_One_Breathes()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 20d;
            screen.Refresh();
            Button energy = Find("Canvas/Hud/Energy");
            Assert.IsFalse(energy.interactable);
            int before = screen.Feel.ParticleCount;
            energy.GetComponent<PressDip>().Click();
            Assert.Greater(screen.Feel.ParticleCount, before, "a line floats up saying how long to wait");
            Assert.AreEqual(20d, runner.State.Energy, 1e-9, "nothing was spent");
            for (int i = 0; i < 10; i++) screen.Advance(0.1f);
            Assert.AreEqual(1f, energy.transform.Find("Face").localScale.x, 1e-4f, "short of power the bar holds still");

            runner.State.Energy = 100d;
            screen.Refresh();
            float min = 1f, max = 1f;
            for (int i = 0; i < 40; i++)
            {
                screen.Advance(0.05f);
                float sx = energy.transform.Find("Face").localScale.x;
                min = Mathf.Min(min, sx);
                max = Mathf.Max(max, sx);
            }
            Assert.Greater(max, 1.01f, "a full bar breathes");
            Assert.Less(min, 0.99f);
            Assert.Less(max, 1.05f, "but only a little");
        }

        [Test]
        public void Edge_Pills_Count_What_Waits_Out_Of_View_And_A_Touch_Flies_There()
        {
            Motion.SetReduced(true); // moves arrive at once, so the test reads positions right after a touch
            for (int i = 0; i < runner.State.Levels.Length; i++) runner.State.Levels[i] = 1 + i;
            runner.State.Plots = runner.State.Levels.Length;
            runner.State.Coins = new BigNum(1e300); // every station can be raised
            screen.Shell.StartGame();
            screen.Refresh();
            GameObject up = root.transform.Find("Canvas/EdgeUp").gameObject, down = root.transform.Find("Canvas/EdgeDown").gameObject, home = root.transform.Find("Canvas/Home").gameObject;
            int Count(GameObject pill) => pill.activeSelf ? int.Parse(pill.transform.Find("Text").GetComponent<Text>().text.Substring(2)) : 0;

            // The usual zoom, looking at the top of the garden: the lowest stations are out of view below.
            screen.LookAt(new Vector2(540f, 2900f));
            screen.Advance(1f / 30f);
            Assert.IsTrue(down.activeSelf, "stations that can be raised wait below");
            StringAssert.StartsWith("▼ ", down.transform.Find("Text").GetComponent<Text>().text);
            int waiting = Count(down);
            Assert.GreaterOrEqual(waiting, 2, "the spring and the observatory");
            Assert.IsFalse(home.activeSelf, "the teapot is still in view at this zoom");

            Find("Canvas/EdgeDown").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.Less(Count(down), waiting, "the touch flew to the nearest waiting station, which is now in view");

            // Close in and look at the foot of the garden: the teapot has scrolled away, the way home shows.
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(new Vector2(540f, 300f));
            screen.Advance(1f / 30f);
            Assert.IsTrue(home.activeSelf, "the teapot is out of view");
            Assert.IsTrue(up.activeSelf, "and the stations above wait");
            Find("Canvas/Home").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.IsFalse(home.activeSelf, "home again: the teapot is on screen");

            runner.State.Coins = BigNum.Zero; // nothing to raise, nobody dozing: nothing to point at
            screen.Refresh();
            screen.LookAt(new Vector2(540f, 300f));
            screen.Advance(1f / 30f);
            Assert.IsFalse(down.activeSelf);
            Assert.IsFalse(up.activeSelf);
            Assert.IsTrue(home.activeSelf, "but the way home still shows far from the teapot");

            screen.OpenSheet(0);
            screen.Advance(1f / 30f);
            Assert.IsFalse(home.activeSelf, "nothing at the edges while a sheet is up");
        }

        [Test]
        public void From_Far_Away_One_Touch_On_Open_Ground_Comes_In_To_The_Usual_Zoom()
        {
            screen.Shell.StartGame();
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Assert.AreEqual(GardenScreen.ZoomLevel.Far, screen.Zoom);
            Find("Canvas/Garden").onClick.Invoke();
            screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "one touch, not two, when the whole garden is in view");
            Find("Canvas/Garden").onClick.Invoke();
            screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "at the usual zoom a single touch still only closes the card");
        }

        [Test]
        public void The_Game_Opens_Where_The_Player_Last_Looked_And_Keeps_Noting_Where_That_Is()
        {
            runner.State.ViewX = 540d;
            runner.State.ViewY = 800d;
            runner.State.ViewZoom = 1.5d;
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f); // the opening glide is over
            Assert.AreEqual(1.5f, screen.ZoomScale, 1e-3f, "the zoom comes back too");
            Assert.AreEqual(540f, (1080f / 2f - world.anchoredPosition.x) / world.localScale.x, 1f);
            Assert.AreEqual(800f, (1920f / 2f - world.anchoredPosition.y) / world.localScale.y, 1f, "the spot under the middle of the screen is the one saved");

            screen.LookAt(new Vector2(540f, 2000f)); // LookAt puts its point at 60% of the height, so the middle sits a little below it
            screen.Advance(1f / 30f);
            Assert.AreEqual(2000d - 0.1d * 1920d / 1.5d, runner.State.ViewY, 1d, "looking elsewhere is noted at once, so the next save keeps it");
            Assert.AreEqual(1.5d, runner.State.ViewZoom, 1e-3d);
        }

        [Test]
        public void A_Swipe_Across_The_Station_Card_Walks_To_The_Next_Station_And_The_Garden_Follows()
        {
            for (int i = 0; i < 6; i++) runner.State.Levels[i] = 1 + i;
            runner.State.Plots = 6;
            screen.Shell.StartGame();
            screen.Refresh();
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Text name = root.transform.Find("Canvas/Card/Name").GetComponent<Text>();
            screen.FocusStation(2);
            Assert.AreEqual(2, screen.Selected);

            var swipe = root.transform.Find("Canvas/Card/Face").GetComponent<CardSwipe>();
            Assert.IsNotNull(swipe, "the card's face catches swipes");
            var left = new UnityEngine.EventSystems.PointerEventData(null) { pressPosition = new Vector2(600f, 300f), position = new Vector2(380f, 310f) };
            swipe.OnEndDrag(left);
            Assert.AreEqual(3, screen.Selected, "a swipe to the left goes to the next station");
            Assert.AreEqual(L10n.Station(runner.Defs[3].Id), name.text);
            float before = world.anchoredPosition.y;
            var right = new UnityEngine.EventSystems.PointerEventData(null) { pressPosition = new Vector2(300f, 300f), position = new Vector2(520f, 290f) };
            swipe.OnEndDrag(right);
            swipe.OnEndDrag(right);
            Assert.AreEqual(1, screen.Selected, "two swipes to the right go back two");
            Assert.AreNotEqual(before, world.anchoredPosition.y, "the garden followed the card");

            var tooShort = new UnityEngine.EventSystems.PointerEventData(null) { pressPosition = new Vector2(300f, 300f), position = new Vector2(340f, 300f) };
            swipe.OnEndDrag(tooShort);
            Assert.AreEqual(1, screen.Selected, "a nudge is not a swipe");
            var mostlyUp = new UnityEngine.EventSystems.PointerEventData(null) { pressPosition = new Vector2(300f, 300f), position = new Vector2(200f, 500f) };
            swipe.OnEndDrag(mostlyUp);
            Assert.AreEqual(1, screen.Selected, "nor is a drag that mostly goes up");

            swipe.OnEndDrag(right);
            swipe.OnEndDrag(right);
            Assert.AreEqual(0, screen.Selected, "it stops at the first station instead of wrapping round");
            screen.SelectStation(-1);
            swipe.OnEndDrag(left);
            Assert.AreEqual(-1, screen.Selected, "no card, nothing to step");
        }

        [Test]
        public void A_Pull_Down_Anywhere_On_A_List_At_Its_Top_Drags_The_Sheet_And_Can_Close_It()
        {
            screen.Shell.StartGame();
            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            var list = root.transform.Find("Canvas/Sheet/Panel/Scroll").GetComponent<ScrollRect>();
            var pull = list.GetComponent<SheetPull>();
            Assert.IsNotNull(pull, "every list can pull the sheet");
            pull.Scale = () => 1f;

            // Scrolled down into the list, a drag is the list's own.
            list.verticalNormalizedPosition = 0.5f;
            pull.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -6f) });
            pull.OnDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -300f) });
            pull.OnEndDrag(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.IsFalse(pull.Pulling);
            Assert.AreEqual(GardenScreen.SheetHalf, screen.SheetHeight, 0.5f, "the sheet did not move");
            Assert.IsTrue(screen.SheetOpen);

            // At the top of the list, the same drag takes the sheet down with it and closes it when let go low.
            list.verticalNormalizedPosition = 1f;
            pull.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -6f) });
            Assert.IsTrue(pull.Pulling);
            Assert.IsFalse(list.enabled, "the list holds still while the sheet moves");
            pull.OnDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -400f) });
            Assert.AreEqual(GardenScreen.SheetHalf - 400f, screen.SheetHeight, 0.5f);
            pull.OnEndDrag(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.IsTrue(list.enabled, "and scrolls again afterwards");
            Assert.IsFalse(screen.SheetOpen, "let go well down, the sheet closes");

            // An upward drag at the top is still the list's (nothing to pull up).
            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            list.verticalNormalizedPosition = 1f;
            pull.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, 6f) });
            Assert.IsFalse(pull.Pulling);
        }

        [Test]
        public void The_Garden_Above_The_Sheet_Stays_Live_And_A_Touched_Station_Takes_Over_From_The_Sheet()
        {
            runner.State.Levels[1] = 1;
            screen.Shell.StartGame();
            screen.Refresh();
            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.IsTrue(root.transform.Find("Canvas/Garden").GetComponent<ScrollRect>().enabled, "the garden can be dragged while the sheet is up");

            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.IsFalse(screen.SheetOpen, "the sheet gives way");
            Assert.AreEqual(0, screen.Selected, "to the station's own card");
        }

        [Test]
        public void The_Station_Card_Stands_On_The_Tab_Bar_And_The_Other_Tabs_Stay_Readable()
        {
            screen.Shell.StartGame();
            screen.FocusStation(0);
            var card = (RectTransform)root.transform.Find("Canvas/Card");
            var bar = (RectTransform)root.transform.Find("Canvas/TabBar");
            float cardBottom = card.anchoredPosition.y - card.sizeDelta.y / 2f, barTop = bar.anchoredPosition.y + bar.sizeDelta.y;
            Assert.That(cardBottom - barTop, Is.InRange(0f, 16f), "a thin seam, not a gap");

            screen.SelectStation(-1);
            screen.OpenSheet(1);
            for (int i = 0; i < 30; i++) screen.Advance(1f / 30f);
            float other = bar.Find("Open Thỏ/Icon").GetComponent<Image>().color.a;
            Assert.That(other, Is.InRange(0.6f, 0.7f), "the other tabs step back but keep their colour");
        }

        [Test]
        public void The_Stations_Tab_Counts_What_Can_Be_Opened_And_The_Rabbits_Tab_Counts_Merges_Too()
        {
            screen.Shell.StartGame();
            screen.Refresh();
            Transform stations = root.transform.Find("Canvas/TabBar/StationsBadge"), rabbits = root.transform.Find("Canvas/TabBar/RabbitsBadge");
            Assert.IsFalse(stations.gameObject.activeSelf, "nothing can be opened with an empty purse");
            runner.State.Coins = new BigNum(120);
            screen.Refresh();
            Assert.IsTrue(stations.gameObject.activeSelf);
            Assert.AreEqual("1", stations.Find("Text").GetComponent<Text>().text, "the leaf basket is within reach; the jam pot still needs the basket open first");

            Assert.AreEqual("1", rabbits.Find("Text").GetComponent<Text>().text, "the teapot has nobody");
            for (int i = 1; i <= 3; i++) runner.State.Rabbits.Add(new Rabbit { Id = i, Tier = 0, Kind = "tho-bong", Name = "R" + i });
            runner.State.NextRabbitId = 3;
            screen.Refresh();
            Assert.AreEqual("2", rabbits.Find("Text").GetComponent<Text>().text, "and three silver rabbits can be merged");
        }

        [Test]
        public void A_Finger_Resting_On_The_Purse_Shows_Every_Digit_And_Is_Not_A_Tap()
        {
            runner.State.Coins = new BigNum(13912345);
            screen.Shell.StartGame();
            screen.Refresh();
            Assert.AreEqual("13.912.345", runner.State.Coins.Full());
            L10n.SetLocale("en");
            Assert.AreEqual("13,912,345", runner.State.Coins.Full());
            L10n.SetLocale("vi");

            int before = screen.Feel.ParticleCount;
            screen.PursePointer(true);
            screen.Advance(0.2f);
            Assert.AreEqual(before, screen.Feel.ParticleCount, "a short touch shows nothing yet");
            screen.Advance(0.3f);
            Assert.Greater(screen.Feel.ParticleCount, before, "held long enough, the whole number floats up");
            Transform gain = root.transform.Find("Canvas/Feel/Gain");
            Assert.IsNotNull(gain);
            StringAssert.Contains("13.912.345", gain.GetComponent<Text>().text);
            screen.PursePointer(false);
            Find("Canvas/Hud/Purse").onClick.Invoke(); // letting go after a hold
            Assert.IsFalse(screen.Income.IsOpen, "a hold is not a tap");
            Find("Canvas/Hud/Purse").onClick.Invoke(); // a plain tap
            Assert.IsTrue(screen.Income.IsOpen);
            screen.Income.Close();

            Assert.IsNotNull(root.transform.Find("Canvas/Hud/Corner"), "the corner buttons stand on one plate");
        }

        [Test]
        public void A_Touched_Rabbit_Jumps_And_Shows_A_Heart()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Shell.StartGame();
            screen.Refresh();
            for (int k = 0; k < 8; k++) screen.Advance(0.5f); // the newcomer walks over first
            var rabbit = (RectTransform)root.transform.Find("Canvas/Garden/Content/Station am-tra/Rabbit");
            float home = rabbit.anchoredPosition.y;
            int before = screen.Feel.ParticleCount;
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.Greater(screen.Feel.ParticleCount, before);
            Assert.AreEqual("♥", root.transform.Find("Canvas/Feel/Gain").GetComponent<Text>().text);
            screen.Advance(0.12f);
            Assert.Greater(rabbit.anchoredPosition.y, home + 5f, "it jumps");
            for (int i = 0; i < 20; i++) screen.Advance(0.1f);
            Assert.AreEqual(home, rabbit.anchoredPosition.y, 0.5f, "and lands where it stood");
        }

        [Test]
        public void Carrots_Fly_From_The_Station_To_The_Purse_And_The_Purse_Swells_When_They_Land()
        {
            screen.Shell.StartGame();
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f); // the opening glide is over, the teapot is in view
            var currency = (RectTransform)root.transform.Find("Canvas/Hud/Purse/Currency");
            screen.PlayPayout(0, new BigNum(5));
            Assert.Greater(screen.Feel.Flying, 0, "carrots are on their way");
            float biggest = 1f;
            for (int i = 0; i < 40; i++)
            {
                screen.Advance(1f / 30f);
                biggest = Mathf.Max(biggest, currency.localScale.x);
            }
            Assert.AreEqual(0, screen.Feel.Flying, "they have all landed");
            Assert.Greater(biggest, 1.1f, "and the purse swelled when they did");
            Assert.AreEqual(1f, currency.localScale.x, 0.01f, "then settled");
        }

        [Test]
        public void An_Upgrade_Blooms_The_Station_Light_And_A_Doubling_Rings_It_With_Stars()
        {
            runner.State.Coins = new BigNum(1e9);
            runner.State.Levels[0] = 9;
            screen.Shell.StartGame();
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            screen.Refresh();
            var glow = (RectTransform)root.transform.Find("Canvas/Garden/Content/Station am-tra/Glow");
            Assert.AreEqual(1f, glow.localScale.x, 1e-3f);
            int before = screen.Feel.ParticleCount;
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke(); // level 10: the first doubling
            Assert.AreEqual(10, runner.State.Levels[0]);
            screen.Advance(0.05f);
            Assert.Greater(glow.localScale.x, 1.1f, "the light behind the station blooms");
            Assert.GreaterOrEqual(screen.Feel.ParticleCount - before, 14, "a ring of stars on top of the banner's confetti");
            for (int i = 0; i < 20; i++) screen.Advance(0.1f);
            Assert.AreEqual(1f, glow.localScale.x, 1e-3f, "and settles back exactly");
        }

        [Test]
        public void The_Rabbit_Sheet_And_The_Pickers_Slide_Up_From_The_Bottom_And_Pull_Down_To_Close()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Shell.StartGame();
            screen.Refresh();

            screen.Detail.Open(runner.Defs, runner.State, 1);
            var card = (RectTransform)root.transform.Find("Canvas/RabbitSheet/Card");
            Assert.IsTrue(screen.Detail.IsOpen);
            Assert.AreEqual(0f, card.pivot.y, "anchored to the bottom edge");
            Assert.Less(card.anchoredPosition.y, -600f, "it starts below the screen");
            Assert.IsNotNull(root.transform.Find("Canvas/RabbitSheet/Card/Handle/Bar"), "with a grab handle");
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(0f, card.anchoredPosition.y, 1f, "and stands on the edge after the slide");

            screen.Detail.Sheet.Drag(-100f);
            screen.Detail.Sheet.Release();
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.IsTrue(screen.Detail.IsOpen, "a short pull springs back");
            Assert.AreEqual(0f, card.anchoredPosition.y, 1f);

            screen.Detail.Sheet.Drag(-500f);
            screen.Detail.Sheet.Release();
            Assert.IsFalse(screen.Detail.IsOpen, "pulled well down, it closes");
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(root.transform.Find("Canvas/RabbitSheet").gameObject.activeSelf, "and is gone after the slide");

            screen.Picker.Open(runner.State, 1, ItemSlot.Hat);
            Assert.IsTrue(screen.Picker.IsOpen);
            Assert.AreEqual(0f, ((RectTransform)root.transform.Find("Canvas/Picker/Card")).pivot.y);
            Find("Canvas/Picker").onClick.Invoke(); // a touch outside
            Assert.IsFalse(screen.Picker.IsOpen);

            screen.Workers.Open(runner.Defs, runner.State, 0);
            Assert.IsTrue(screen.Workers.IsOpen);
            screen.Workers.Sheet.Drag(-20f);
            screen.Workers.Sheet.Release(-2000f);
            Assert.IsFalse(screen.Workers.IsOpen, "a flick down closes it too");
        }

        [Test]
        public void The_Mortar_Opens_A_Game_From_Garden_Level_Three_And_A_Finished_Game_Is_Paid()
        {
            runner.State.Coins = new BigNum(1000);
            screen.Shell.StartGame();
            screen.Refresh();
            GameObject mortar = root.transform.Find("Canvas/Garden/Content/Mortar").gameObject;
            Assert.IsFalse(mortar.activeSelf, "no mortar before garden level three");
            runner.State.GardenXp = Garden.XpForLevel(3);
            screen.Refresh();
            Assert.IsTrue(mortar.activeSelf);
            Assert.AreEqual(L10n.T("mortar.plays", 3), mortar.transform.Find("Plate/Text").GetComponent<Text>().text);

            Find("Canvas/Garden/Content/Mortar").onClick.Invoke();
            Assert.IsTrue(screen.Pound.IsOpen);
            Assert.AreEqual(2, Pounding.PlaysLeft(runner.State, runner.TodayNumber), "a play is spent on starting");
            Assert.AreEqual(0, screen.Pound.Run.Beat);

            // Tap right on the first beat, anywhere on the screen.
            screen.Advance((float)Pounding.BeatTime(0) - (float)screen.Pound.Time);
            Find("Canvas/Pound").onClick.Invoke();
            Assert.AreEqual(1, screen.Pound.Run.Beat);
            Assert.AreEqual(3, screen.Pound.Run.Score, "a perfect pound");
            Assert.AreEqual(L10n.T("pound.perfect"), root.transform.Find("Canvas/Pound/Shake/Judge").GetComponent<Text>().text);

            // Let the rest go by: misses, a shake, then the result and the pay.
            Motion.SetReduced(true);
            screen.Advance(0.5f);
            Assert.AreEqual(Vector2.zero, ((RectTransform)root.transform.Find("Canvas/Pound/Shake")).anchoredPosition, "no shake with reduced motion");
            BigNum before = runner.State.Coins;
            for (int i = 0; i < 60; i++) screen.Advance(0.5f);
            Assert.IsTrue(screen.Pound.Run.Finished);
            Assert.IsTrue(root.transform.Find("Canvas/Pound/Result").gameObject.activeSelf, "the result card is up");
            Assert.AreEqual(1, runner.State.PoundingGames, "paid once");
            Assert.Greater(runner.State.Coins.ToDouble(), before.ToDouble(), "three points still pay something");
            StringAssert.Contains("☆☆☆", root.transform.Find("Canvas/Pound/Result/Card/Stars").GetComponent<Text>().text);
            Find("Canvas/Pound/Result/Card/Done").onClick.Invoke();
            Assert.IsFalse(screen.Pound.IsOpen);
            Assert.AreEqual(L10n.T("mortar.plays", 2), mortar.transform.Find("Plate/Text").GetComponent<Text>().text);
        }

        [Test]
        public void The_Basket_Opens_The_Leaf_Game_From_Garden_Level_Four_And_A_Finished_Game_Is_Paid()
        {
            runner.State.Coins = new BigNum(1000);
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(3);
            screen.Refresh();
            GameObject basket = root.transform.Find("Canvas/Garden/Content/Basket").gameObject;
            Assert.IsFalse(basket.activeSelf, "no basket at garden level three, where the mortar already stands");
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Mortar").gameObject.activeSelf);
            runner.State.GardenXp = Garden.XpForLevel(4);
            screen.Refresh();
            Assert.IsTrue(basket.activeSelf);
            Assert.AreEqual(L10n.T("basket.plays", 3), basket.transform.Find("Plate/Text").GetComponent<Text>().text);

            Find("Canvas/Garden/Content/Basket").onClick.Invoke();
            Assert.IsTrue(screen.Basket.IsOpen);
            Assert.IsFalse(screen.Pound.IsOpen);
            Assert.AreEqual(2, Leaves.PlaysLeft(runner.State, runner.TodayNumber), "a play is spent on starting");
            Assert.AreEqual(3, Pounding.PlaysLeft(runner.State, runner.TodayNumber), "and none of the mortar's");
            LeavesRun run = screen.Basket.Run;
            Assert.AreEqual(Leaves.SeedFor(runner.TodayNumber, 0), run.Seed, "the first game of the day");
            Assert.AreEqual(0.5f, screen.Basket.BasketX, 1e-6f, "the basket starts in the middle");

            // A drag puts the basket where the finger is; a tap slides it there (at once under reduced motion).
            screen.Basket.SetBasket(0.9f);
            Assert.AreEqual(0.9f, screen.Basket.BasketX, 1e-6f);
            Motion.SetReduced(true);
            screen.Basket.MoveBasket(0.2f);
            screen.Advance(0.05f);
            Assert.AreEqual(0.2f, screen.Basket.BasketX, 1e-6f);

            // Play it through: the basket under every leaf, away from every cone, each at the moment it lands.
            BigNum before = runner.State.Coins;
            foreach (Leaf leaf in run.Schedule)
            {
                screen.Basket.SetBasket(leaf.Kind == LeafKind.Cone ? (leaf.X > 0.5 ? (float)leaf.X - 0.4f : (float)leaf.X + 0.4f) : (float)leaf.X);
                screen.Advance((float)(leaf.Lands - screen.Basket.Time) + 0.001f);
                Assert.AreEqual(Quaternion.identity, ((RectTransform)root.transform.Find("Canvas/Leaves/Basket")).localRotation, "no wobble with reduced motion");
            }
            Assert.IsTrue(run.Finished);
            Assert.AreEqual(Leaves.MaxScore, run.Score);
            Assert.AreEqual(L10n.T("leaves.score", Leaves.MaxScore), root.transform.Find("Canvas/Leaves/Score").GetComponent<Text>().text);
            Assert.IsFalse(root.transform.Find("Canvas/Leaves/Result").gameObject.activeSelf, "the clock still runs");
            for (int i = 0; i < 20; i++) screen.Advance(0.5f);
            Assert.IsTrue(root.transform.Find("Canvas/Leaves/Result").gameObject.activeSelf, "the result card is up");
            Assert.AreEqual(1, runner.State.Leaves.Games, "paid once");
            Assert.AreEqual(0, runner.State.PoundingGames);
            Assert.Greater(runner.State.Coins.ToDouble(), before.ToDouble());
            Assert.AreEqual(1, runner.State.Clovers, "the day's first starred game");
            Assert.AreEqual(0, runner.State.GoldenCarrots, "no golden carrot from this game");
            Assert.AreEqual("★★★", root.transform.Find("Canvas/Leaves/Result/Card/Stars").GetComponent<Text>().text);
            Find("Canvas/Leaves/Result/Card/Done").onClick.Invoke();
            Assert.IsFalse(screen.Basket.IsOpen);
            Assert.AreEqual(L10n.T("basket.plays", 2), basket.transform.Find("Plate/Text").GetComponent<Text>().text);
        }

        [Test]
        public void The_Jar_Opens_The_Firefly_Night_From_Garden_Level_Five_And_A_Finished_Night_Is_Paid_Under_The_Days_Caps()
        {
            runner.State.Coins = new BigNum(1000);
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(4);
            screen.Refresh();
            GameObject jar = root.transform.Find("Canvas/Garden/Content/Jar").gameObject;
            Assert.IsFalse(jar.activeSelf, "no jar at garden level four, where the basket is already out");
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Basket").gameObject.activeSelf);
            runner.State.GardenXp = Garden.XpForLevel(5);
            screen.Refresh();
            Assert.IsTrue(jar.activeSelf);
            Assert.AreEqual(L10n.T("jar.plays", 3), jar.transform.Find("Plate/Text").GetComponent<Text>().text);

            Find("Canvas/Garden/Content/Jar").onClick.Invoke();
            Assert.IsTrue(screen.Jar.IsOpen);
            Assert.IsFalse(screen.Basket.IsOpen);
            Assert.IsFalse(screen.Pound.IsOpen);
            Assert.AreEqual(2, Fireflies.PlaysLeft(runner.State, runner.TodayNumber), "a play is spent on starting");
            Assert.AreEqual(3, Leaves.PlaysLeft(runner.State, runner.TodayNumber), "and none of the basket's");
            FirefliesRun run = screen.Jar.Run;
            Assert.AreEqual(Fireflies.SeedFor(runner.TodayNumber, 0), run.Seed, "the first night");
            Transform field = root.transform.Find("Canvas/Fireflies/Field");
            Assert.IsFalse(field.Find("Fly 0").gameObject.activeSelf, "nothing is out before the lead");

            // Play it through under reduced motion: every fly tapped where it is just after it appears.
            Motion.SetReduced(true);
            BigNum before = runner.State.Coins;
            foreach (Firefly fly in run.Schedule)
            {
                screen.Advance((float)(fly.Appears + 0.2 - screen.Jar.Time));
                Assert.IsTrue(field.Find("Fly " + fly.Index).gameObject.activeSelf, "the fly is drawn while it is out");
                Assert.AreEqual(Vector3.one, field.Find("Fly " + fly.Index).localScale, "no pulse with reduced motion");
                FlySpot spot = Fireflies.PositionAt(fly, screen.Jar.Time);
                screen.Jar.Tap((float)spot.X, (float)spot.Y);
                Assert.IsTrue(run.IsCaught(fly.Index));
            }
            Assert.AreEqual(Fireflies.MaxScore, run.Score);
            Assert.AreEqual(L10n.T("fireflies.score", Fireflies.MaxScore), root.transform.Find("Canvas/Fireflies/Score").GetComponent<Text>().text);
            Assert.IsFalse(root.transform.Find("Canvas/Fireflies/Result").gameObject.activeSelf, "the clock still runs");
            for (int i = 0; i < 20; i++) screen.Advance(0.5f);
            Assert.IsTrue(root.transform.Find("Canvas/Fireflies/Result").gameObject.activeSelf, "the result card is up");
            Assert.AreEqual(1, runner.State.Fireflies.Games, "paid once");
            Assert.AreEqual(0, runner.State.Leaves.Games);
            Assert.Greater(runner.State.Coins.ToDouble(), before.ToDouble());
            Assert.AreEqual(Fireflies.CloversMost, runner.State.Clovers, "a full night brings the most clovers");
            Assert.AreEqual(1, runner.State.GoldenCarrots, "and three stars a golden carrot");
            Assert.AreEqual("★★★", root.transform.Find("Canvas/Fireflies/Result/Card/Stars").GetComponent<Text>().text);
            string extra = root.transform.Find("Canvas/Fireflies/Result/Card/Extra").GetComponent<Text>().text;
            StringAssert.Contains(L10n.T("fireflies.clovers", Fireflies.CloversMost), extra);
            StringAssert.Contains(L10n.T("fireflies.goldenCarrot"), extra);
            StringAssert.DoesNotContain(L10n.T("fireflies.capped"), extra, "nothing was held back");
            Find("Canvas/Fireflies/Result/Card/Done").onClick.Invoke();
            Assert.IsFalse(screen.Jar.IsOpen);
            Assert.AreEqual(L10n.T("jar.plays", 2), jar.transform.Find("Plate/Text").GetComponent<Text>().text);

            // The second full night of the day: the caps hold the clovers and the golden carrot back, and the card says so.
            Find("Canvas/Garden/Content/Jar").onClick.Invoke();
            Assert.IsTrue(screen.Jar.IsOpen);
            run = screen.Jar.Run;
            foreach (Firefly fly in run.Schedule)
            {
                screen.Advance((float)(fly.Appears + 0.2 - screen.Jar.Time));
                FlySpot spot = Fireflies.PositionAt(fly, screen.Jar.Time);
                screen.Jar.Tap((float)spot.X, (float)spot.Y);
            }
            Assert.AreEqual(Fireflies.MaxScore, run.Score);
            for (int i = 0; i < 20; i++) screen.Advance(0.5f);
            Assert.IsTrue(root.transform.Find("Canvas/Fireflies/Result").gameObject.activeSelf);
            Assert.AreEqual(Fireflies.CloversMost, runner.State.Clovers, "the day's clovers are given");
            Assert.AreEqual(1, runner.State.GoldenCarrots, "and so is the day's golden carrot");
            StringAssert.Contains(L10n.T("fireflies.capped"), root.transform.Find("Canvas/Fireflies/Result/Card/Extra").GetComponent<Text>().text);
            Find("Canvas/Fireflies/Result/Card/Done").onClick.Invoke();
            Assert.AreEqual(L10n.T("jar.plays", 1), jar.transform.Find("Plate/Text").GetComponent<Text>().text);
        }

        [Test]
        public void A_Rabbit_Put_To_Work_Walks_From_Its_Seat_To_The_Station_Unless_Motion_Is_Reduced()
        {
            screen.Shell.StartGame();
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-me", Name = "Mè" });
            runner.State.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Kind = "tho-bong", Name = "Bông" });
            runner.State.NextRabbitId = 2;
            screen.Refresh();
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Transform station = world.Find("Station " + runner.Defs[0].Id);
            Assert.IsNull(world.Find("Traveler"));

            Assert.IsTrue(runner.AssignRabbit(1, 0));
            screen.Refresh();
            var traveler = (RectTransform)world.Find("Traveler");
            Assert.IsNotNull(traveler, "the rabbit sets off");
            Assert.AreEqual("kind_tho-me", traveler.Find("Art").GetComponent<Image>().sprite.name);
            Assert.IsFalse(station.Find("Rabbit").gameObject.activeSelf, "not at the station until it gets there");
            Assert.IsFalse(station.Find("Pad").gameObject.activeSelf, "and the pad does not show either");
            Assert.AreEqual(world.Find("Seat 0").GetComponent<RectTransform>().anchoredPosition, traveler.anchoredPosition, "from its seat");
            screen.Advance(0.5f);
            Assert.AreNotEqual(world.Find("Seat 0").GetComponent<RectTransform>().anchoredPosition, traveler.anchoredPosition, "on its way");
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
            Assert.IsNull(world.Find("Traveler"), "arrived");
            Assert.IsTrue(station.Find("Rabbit").gameObject.activeSelf, "walking=" + screen.TravelersWalking + " pad=" + station.Find("Pad").gameObject.activeSelf + " at=" + Rabbits.Find(runner.State, 1).Station + " name=" + station.name);

            // Under reduced motion the newcomer is simply there: the other rabbit takes over the teapot at once.
            Motion.SetReduced(true);
            Assert.IsTrue(runner.AssignRabbit(2, 0));
            screen.Refresh();
            Assert.IsNull(world.Find("Traveler"));
            Assert.IsTrue(station.Find("Rabbit").gameObject.activeSelf);
            Assert.AreEqual("kind_tho-bong", station.Find("Rabbit").GetComponent<Image>().sprite.name);
        }

        [Test]
        public void A_Strolling_Rabbit_Leaves_From_Its_Seat_And_Walks_Rather_Than_Appearing()
        {
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-me", Name = "Mè" });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.IsTrue(GardenScreen.Strolling(0, 0f), "at the start of its period the first rabbit is out");
            var walker = (RectTransform)world.Find("Walker 0");
            var seat = (RectTransform)world.Find("Seat 0");
            screen.Advance(0f);
            Assert.IsTrue(walker.gameObject.activeSelf, "clock=" + screen.Clock + " seated=" + screen.SeatedCount + " strolling=" + GardenScreen.Strolling(0, screen.Clock) + " seatShown=" + seat.gameObject.activeSelf);
            Assert.IsFalse(seat.gameObject.activeSelf, "the seat is empty while its rabbit is out");
            Assert.AreEqual(seat.anchoredPosition, walker.anchoredPosition, "it starts on the seat, not in the lane");
            screen.Advance(2f);
            Vector2 later = walker.anchoredPosition;
            Assert.Greater(later.y, seat.anchoredPosition.y + 100f, "two seconds on it has walked away from the seat");
            Assert.Less(Vector2.Distance(later, seat.anchoredPosition), 2f * 300f, "at a walk, not a jump");
        }

        [Test]
        public void The_Scenery_Sways_Between_Fixed_Bounds_And_Holds_Still_Under_Reduced_Motion()
        {
            Transform world = root.transform.Find("Canvas/Garden/Content"), canopy = root.transform.Find("Canvas/Garden/Canopy");
            Transform tree = canopy.Find("Tree"), lantern = world.Find("Lantern"), glow = world.Find("LanternGlow"), ripple = world.Find("Ripple");
            Assert.IsNotNull(tree); Assert.IsNotNull(lantern); Assert.IsNotNull(ripple);
            Assert.AreEqual(0f, ((RectTransform)tree).pivot.y, "a tree leans from its root");
            Assert.AreEqual(1f, ((RectTransform)lantern).pivot.y, "a lantern swings from its hook");
            float maxTree = 0f, maxLantern = 0f, minGlow = 1f, maxGlow = 0f;
            for (int k = 0; k < 120; k++)
            {
                screen.Advance(0.1f);
                maxTree = Mathf.Max(maxTree, Mathf.Abs(Mathf.DeltaAngle(0f, tree.localRotation.eulerAngles.z)));
                maxLantern = Mathf.Max(maxLantern, Mathf.Abs(Mathf.DeltaAngle(0f, lantern.localRotation.eulerAngles.z)));
                float a = glow.GetComponent<Image>().color.a;
                minGlow = Mathf.Min(minGlow, a); maxGlow = Mathf.Max(maxGlow, a);
            }
            Assert.Greater(maxTree, 0.3f, "the tree moves"); Assert.LessOrEqual(maxTree, 2.05f, "but only a little");
            Assert.Greater(maxLantern, 0.5f); Assert.LessOrEqual(maxLantern, 3.05f);
            Assert.Greater(maxGlow - minGlow, 0.02f, "the lantern flickers"); Assert.GreaterOrEqual(minGlow, 0.6f * 0.7f); Assert.LessOrEqual(maxGlow, 0.6f + 0.001f);
            Assert.Greater(ripple.GetComponent<Image>().color.a, 0f, "the pond ripples");

            Motion.SetReduced(true);
            screen.Advance(0.1f);
            Assert.AreEqual(Quaternion.identity, tree.localRotation);
            Assert.AreEqual(Quaternion.identity, lantern.localRotation);
            Assert.AreEqual(0.6f, glow.GetComponent<Image>().color.a, 0.001f);
            Assert.AreEqual(0f, ripple.GetComponent<Image>().color.a);
        }
        // ----- #19: touching and stroking rabbits -----

        /// <summary>Fires every entry of one kind on an EventTrigger, as a finger would.</summary>
        void Trigger(string path, UnityEngine.EventSystems.EventTriggerType type)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            var trigger = t.GetComponent<UnityEngine.EventSystems.EventTrigger>();
            Assert.IsNotNull(trigger, "Nothing to stroke at " + path);
            var data = new UnityEngine.EventSystems.PointerEventData(null) { button = UnityEngine.EventSystems.PointerEventData.InputButton.Left };
            foreach (UnityEngine.EventSystems.EventTrigger.Entry e in trigger.triggers)
                if (e.eventID == type) e.callback.Invoke(data);
        }

        Rabbit GiveWorker(long id = 1, int station = 0, int affection = 0)
        {
            var r = new Rabbit { Id = id, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = station, Affection = affection };
            runner.State.Rabbits.Add(r);
            runner.State.NextRabbitId = id;
            screen.Refresh();
            return r;
        }

        [Test]
        public void Touching_A_Working_Rabbit_Pays_Tea_Power_Once_A_Day_And_Its_Card_Shows_The_Heart()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 10;
            Rabbit r = GiveWorker();
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6, "the first stroke of the day pays tea power");
            Assert.AreEqual(1, r.Affection);
            Assert.AreEqual(runner.TodayNumber, r.PetDay);
            Assert.AreEqual(1, runner.State.TotalTaps, "a touch on the rabbit is still a tap on its station");

            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6, "the second stroke of the day is for love alone");
            Assert.AreEqual(1, r.Affection);

            screen.Detail.Open(runner.Defs, runner.State, 1);
            Text hearts = root.transform.Find("Canvas/RabbitSheet/Card/Body/Bond/Text").GetComponent<Text>();
            Assert.AreEqual("♥♡♡♡♡", hearts.text, "one heart of five on the rabbit's own sheet");
            r.Affection = 4;
            screen.Refresh();
            Assert.AreEqual("♥♥♥♥♡", root.transform.Find("Canvas/RabbitSheet/Card/Body/Bond/Text").GetComponent<Text>().text, "the sheet follows the bond");
            Assert.AreEqual("Cấp 1 ♥♥♥♥", root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Level").GetComponent<Text>().text, "and so does the grid cell");
        }

        [Test]
        public void Stroking_A_Rabbit_Counts_Once_Per_Stroke_And_The_Touch_That_Ends_It_Is_Not_A_Tap()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 10;
            Rabbit r = GiveWorker();
            const string rabbit = "Canvas/Garden/Content/Station am-tra/Rabbit";
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.BeginDrag);
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.Drag);
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.Drag);
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.Drag);
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6, "one stroke, however far the finger goes");
            Assert.AreEqual(1, r.Affection);
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.PointerClick); // the finger lifting off the rabbit
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.EndDrag);
            Assert.AreEqual(0, runner.State.TotalTaps, "letting go after a stroke is not a tap on the station");

            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.PointerClick);
            Assert.AreEqual(1, runner.State.TotalTaps, "a plain touch on the rabbit is");
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6, "and pays nothing more today");
        }

        [Test]
        public void Waking_A_Dozing_Rabbit_Is_The_Days_Stroke_And_Pays_The_Doze_Reward()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 10;
            Rabbit r = GiveWorker();
            Assert.IsTrue(screen.DozeSomeone());
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(10 + Rabbits.DozeEnergy, runner.State.Energy, 1e-6, "a wake pays the doze reward in place of the stroke's");
            Assert.AreEqual(1, r.Affection);
            Assert.AreEqual(runner.TodayNumber, r.PetDay);
        }

        [Test]
        public void A_Resting_Rabbit_Can_Be_Petted_At_Its_Seat_Or_Out_Walking()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 10;
            Rabbit r = GiveWorker(1, -1);
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.IsNotNull(world.Find("Seat 0/Art").GetComponent<UnityEngine.EventSystems.EventTrigger>(), "a seated rabbit can be stroked");
            Assert.IsNotNull(world.Find("Walker 0/Art").GetComponent<UnityEngine.EventSystems.EventTrigger>(), "and so can one out for a stroll");

            Find("Canvas/Garden/Content/Seat 0").onClick.Invoke();
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6);
            Assert.AreEqual(1, r.Affection);
            Assert.IsTrue(screen.Detail.IsOpen, "a touch still opens its sheet");
            Assert.AreEqual(1, screen.Detail.RabbitId);
            screen.Detail.Close();

            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.BeginDrag);
            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.Drag);
            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.EndDrag);
            Assert.AreEqual(1, r.Affection, "one heart a day, wherever it is");
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6);
            Assert.IsFalse(screen.Detail.IsOpen, "a stroke does not open the sheet");
        }

        [Test]
        public void Close_Friends_Say_A_Line_In_The_Garden_And_Give_A_Clover()
        {
            screen.Shell.StartGame();
            Rabbit r = GiveWorker(1, 0, Rabbits.AffectionLine - 1);
            Transform line = root.transform.Find("Canvas/Garden/Content/BondLine");
            Assert.IsTrue(line == null || !line.gameObject.activeSelf, "nothing to say yet");
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(Rabbits.AffectionLine, r.Affection);
            line = root.transform.Find("Canvas/Garden/Content/BondLine");
            Assert.IsNotNull(line, "the line is spoken over the rabbit");
            Assert.IsTrue(line.gameObject.activeSelf);
            Assert.AreEqual(L10n.T("bond.line.Brewer"), line.Find("Text").GetComponent<Text>().text, "a brewer's line for a brewer");
            Assert.AreNotEqual("", L10n.T("bond.line.Brewer"));
            for (int i = 0; i < 30 * 5; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(line.gameObject.activeSelf, "the line leaves on its own");

            runner.State.Clovers = 0;
            r.Affection = Rabbits.AffectionMax - 1;
            r.PetDay = -1;
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(Rabbits.AffectionMax, r.Affection);
            Assert.AreEqual(Rabbits.AffectionGiftClovers, runner.State.Clovers, "the closest friend gives a clover");
            Assert.IsTrue(screen.Feel.BannerVisible, "and it is a moment");
            StringAssert.Contains("Bông", root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);
        }

        static int CountNamed(Transform parent, string name)
        {
            int n = 0;
            for (int k = 0; k < parent.childCount; k++) if (parent.GetChild(k).name == name) n++;
            return n;
        }

        // ----- #20: guests and their tea orders -----

        [Test]
        public void A_Guest_Comes_To_The_Counter_A_Minute_Into_Play_With_A_Bubble_And_A_Pill_Then_Leaves()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            screen.Refresh();
            Transform guest = root.transform.Find("Canvas/Garden/Content/Guest");
            Transform pill = root.transform.Find("Canvas/Hud/Order");
            Assert.IsNotNull(guest); Assert.IsNotNull(pill);
            Assert.IsFalse(guest.gameObject.activeSelf, "nobody yet");
            Assert.IsFalse(pill.gameObject.activeSelf);

            for (int i = 0; i < 59; i++) screen.Advance(1f);
            Assert.IsNull(runner.State.Order, "not before a minute of play");
            for (int i = 0; i < 2; i++) screen.Advance(1f);
            Order order = runner.State.Order;
            Assert.IsNotNull(order, "the first guest comes a minute in");
            Assert.IsTrue(guest.gameObject.activeSelf);
            Assert.AreEqual("blip", Sfx.Last);
            screen.Advance(0.1f); // under reduced motion the walk is over in a frame
            Assert.IsTrue(screen.GuestAtCounter);
            Transform bubble = guest.Find("Bubble");
            Assert.IsTrue(bubble.gameObject.activeSelf, "the order is up in a bubble");
            Assert.IsTrue(bubble.Find("Item 0").gameObject.activeSelf);
            Assert.AreEqual(order.Lines.Count > 1, bubble.Find("Item 1").gameObject.activeSelf);
            Assert.AreEqual("×" + order.Lines[0].Count, bubble.Find("Item 0/Count/Text").GetComponent<Text>().text);
            Assert.IsTrue(pill.gameObject.activeSelf, "the HUD says a guest waits");

            pill.GetComponent<Button>().onClick.Invoke();
            Assert.IsTrue(screen.OrderCard.IsOpen, "the pill opens the card");
            screen.OrderCard.Close();

            for (int i = 0; i < (int)Orders.WaitSeconds + 1; i++) screen.Advance(1f);
            Assert.IsNull(runner.State.Order, "patience ran out");
            Assert.AreEqual(0, runner.State.OrdersDelivered, "and nothing is lost for it");
            for (int i = 0; i < 3; i++) screen.Advance(0.1f);
            Assert.IsFalse(guest.gameObject.activeSelf, "gone");
            Assert.IsFalse(pill.gameObject.activeSelf);
        }

        [Test]
        public void The_Order_Card_Delivers_Only_When_The_Tray_Has_Everything_And_Pays_Carrots()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            runner.State.Levels[2] = 1; // the jam pot too, so an order can have two lines
            Order order = runner.PlaceOrder();
            Assert.IsNotNull(order);
            screen.Refresh();
            screen.Advance(0.1f);
            Assert.IsTrue(screen.GuestAtCounter);

            Find("Canvas/Garden/Content/Guest").onClick.Invoke();
            Assert.IsTrue(screen.OrderCard.IsOpen, "a touch on the guest opens the card");
            Button deliver = Find("Canvas/Order/Card/Deliver");
            Assert.IsFalse(deliver.interactable, "an empty tray");
            StringAssert.Contains(L10n.T("kind." + order.Guest), root.transform.Find("Canvas/Order/Card/Guest/Name").GetComponent<Text>().text);
            Assert.IsNotNull(root.transform.Find("Canvas/Order/Card/Guest/PortraitHolder/Tile"));
            Transform line0 = root.transform.Find("Canvas/Order/Card/Line 0");
            Assert.IsTrue(line0.gameObject.activeSelf);
            Assert.AreEqual("0/" + order.Lines[0].Count, line0.Find("Have").GetComponent<Text>().text);
            Assert.IsFalse(line0.Find("Met").gameObject.activeSelf);
            Assert.AreEqual(order.Lines.Count > 1, root.transform.Find("Canvas/Order/Card/Line 1").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("order.missing", TotalCount(order)), deliver.transform.Find("Face/Text").GetComponent<Text>().text);

            // One short still does not do.
            Tray.Add(runner.State, order.Lines[0].Station, false);
            screen.Refresh();
            Assert.AreEqual(order.Lines[0].Count == 1, line0.Find("Met").gameObject.activeSelf);
            Assert.AreEqual(order.Lines.Count == 1 && order.Lines[0].Count == 1, deliver.interactable);
            // Everything the guest asked for, and a golden batch that is not for it.
            foreach (OrderLine line in order.Lines)
                while (Orders.Have(runner.State, line.Station) < line.Count) Tray.Add(runner.State, line.Station, false);
            Tray.Add(runner.State, 0, true);
            screen.Refresh();
            Assert.IsTrue(deliver.interactable);
            Assert.IsTrue(line0.Find("Met").gameObject.activeSelf);
            Assert.AreEqual(order.Lines[0].Count + "/" + order.Lines[0].Count, line0.Find("Have").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("order.deliver"), deliver.transform.Find("Face/Text").GetComponent<Text>().text);

            BigNum expected = Orders.Reward(runner.Defs, runner.State, runner.Mods, order);
            int cloversBefore = runner.State.Clovers;
            deliver.onClick.Invoke();
            Assert.IsNull(runner.State.Order, "served");
            Assert.IsTrue(runner.State.Coins.Equals(expected), "the products' price and the bonus");
            Assert.AreEqual(cloversBefore + order.RewardClovers, runner.State.Clovers);
            Assert.AreEqual(1, runner.State.OrdersDelivered);
            Assert.AreEqual(1, runner.State.Tray.Count, "only the golden batch is left");
            Assert.IsTrue(runner.State.Tray[0].Golden);
            Assert.IsFalse(screen.OrderCard.IsOpen);
            Assert.Greater(screen.Feel.Flying, 0, "carrots fly to the purse");
            Assert.IsTrue(screen.Feel.BannerVisible, "a happy guest");
            Assert.AreEqual("reward", Sfx.Last);
            screen.Refresh(); // in play the runner's StateChanged does this
            Assert.IsFalse(root.transform.Find("Canvas/Hud/Order").gameObject.activeSelf);

            // The guest bows and goes home.
            for (int i = 0; i < 20; i++) screen.Advance(0.1f);
            Assert.IsFalse(screen.GuestShown);
            Assert.IsFalse(screen.GuestAtCounter);
        }

        static int TotalCount(Order order)
        {
            int n = 0;
            foreach (OrderLine line in order.Lines) n += line.Count;
            return n;
        }

        [Test]
        public void The_Ground_Has_Layers_That_Lie_Under_Everything_That_Stands()
        {
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.GreaterOrEqual(CountNamed(world, "Path"), 10, "a worn band of earth under the stepping stones");
            Assert.GreaterOrEqual(CountNamed(world, "Flagstone"), 12, "flagstones around the teapot");
            Assert.GreaterOrEqual(CountNamed(world, "Flowers"), 2, "carpets of flowers on open grass");
            Transform beam = world.Find("Moonbeam"), pool = world.Find("MoonPool"), stone = world.Find("Stone"), station = world.Find("Station am-tra");
            Assert.IsNotNull(beam); Assert.IsNotNull(pool);
            Assert.Less(world.Find("Path").GetSiblingIndex(), stone.GetSiblingIndex(), "earth under the stones");
            Assert.Less(beam.GetSiblingIndex(), station.GetSiblingIndex(), "the moonlight is on the ground, under the teapot");
            Assert.Less(world.Find("Flagstone").GetSiblingIndex(), station.GetSiblingIndex());
            Assert.Greater(pool.GetComponent<Image>().color.a, 0.2f);
            // Every flower carpet keeps clear of every station.
            for (int k = 0; k < world.childCount; k++)
            {
                Transform child = world.GetChild(k);
                if (child.name != "Flowers") continue;
                Vector2 at = ((RectTransform)child).anchoredPosition;
                for (int i = 0; i < runner.Defs.Count; i++)
                {
                    Vector2 spot = GardenScreen.StationSpot(i);
                    Assert.IsTrue(Mathf.Abs(spot.x - at.x) >= 230f || Mathf.Abs(spot.y - at.y) >= 230f, "flowers off station " + i);
                }
            }
        }

        [Test]
        public void Scenery_Stands_On_A_Soft_Shadow_Drawn_Just_Behind_It()
        {
            Transform world = root.transform.Find("Canvas/Garden/Content"), canopy = root.transform.Find("Canvas/Garden/Canopy");
            Assert.GreaterOrEqual(CountNamed(canopy, "Shade"), CountNamed(canopy, "Tree"), "every tree at the edge has its shadow");
            Assert.GreaterOrEqual(CountNamed(world, "Shade"), CountNamed(world, "Lantern") + CountNamed(world, "Planter"));
            Transform tree = canopy.Find("Tree");
            Vector2 foot = ((RectTransform)tree).anchoredPosition;
            Transform shade = null;
            float nearest = float.MaxValue;
            for (int k = 0; k < canopy.childCount; k++)
            {
                Transform child = canopy.GetChild(k);
                if (child.name != "Shade") continue;
                float d = Vector2.Distance(foot, ((RectTransform)child).anchoredPosition);
                if (d < nearest) { nearest = d; shade = child; }
            }
            string around = "";
            for (int k = Mathf.Max(0, tree.GetSiblingIndex() - 3); k <= Mathf.Min(canopy.childCount - 1, tree.GetSiblingIndex() + 1); k++)
                around += canopy.GetChild(k).name + "@" + ((RectTransform)canopy.GetChild(k)).anchoredPosition + " ";
            Assert.IsNotNull(shade);
            Assert.Less(nearest, 12f, "a shadow lies at the tree's foot; around it: " + around);
            Assert.Less(shade.GetSiblingIndex(), tree.GetSiblingIndex(), "and is drawn behind it");
            Assert.Less(shade.GetComponent<Image>().color.a, 0.4f, "soft");
        }

        [Test]
        public void The_Canopy_Slides_Slower_Than_The_Ground()
        {
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Canopy");
            Assert.IsNotNull(canopy.Find("Tree"), "the trees at the edge live on the canopy");
            Assert.IsNull(canopy.Find("Lantern"), "the lanterns stay on the ground");
            screen.LookAt(new Vector2(540f, 2800f));
            screen.Advance(0f);
            Vector2 street0 = street.anchoredPosition, canopy0 = canopy.anchoredPosition;
            screen.LookAt(new Vector2(540f, 400f));
            screen.Advance(0f);
            float groundMoved = street.anchoredPosition.y - street0.y, canopyMoved = canopy.anchoredPosition.y - canopy0.y;
            Assert.Greater(Mathf.Abs(groundMoved), 500f, "the view really moved");
            Assert.AreEqual(groundMoved * GardenScreen.CanopyDrift, canopyMoved, 1f, "the canopy at nine tenths of the pace");
            Assert.AreEqual(street.localScale, canopy.localScale);
        }

        [Test]
        public void Each_Station_Is_Dressed_With_Intent_Where_There_Is_Room()
        {
            Transform world = root.transform.Find("Canvas/Garden/Content");
            int planters = CountNamed(world, "Planter");
            Assert.GreaterOrEqual(planters, 4, "a planter by most stations");
            Assert.GreaterOrEqual(CountNamed(world, "StoneLantern"), 1, "a stone lantern by the resting corner");
            for (int k = 0; k < world.childCount; k++)
            {
                Transform child = world.GetChild(k);
                if (child.name != "Planter") continue;
                Vector2 at = ((RectTransform)child).anchoredPosition;
                float nearest = float.MaxValue;
                for (int i = 0; i < runner.Defs.Count; i++) nearest = Mathf.Min(nearest, Vector2.Distance(at, GardenScreen.StationSpot(i)));
                Assert.That(nearest, Is.InRange(200f, 320f), "each planter belongs to a station, beside it rather than on it");
            }
            // The mortar no longer sits on the leaf basket's plate: it stands above the teapot's name.
            Vector2 mortar = ((RectTransform)world.Find("Mortar")).anchoredPosition, basket = GardenScreen.StationSpot(1);
            Assert.Greater(Mathf.Abs(mortar.x - basket.x), 200f);
        }

        [Test]
        public void The_Profile_Says_Where_The_Cloud_Stands_And_Save_Now_Sends_The_Garden_Up()
        {
            Text T(string path) { Transform x = root.transform.Find(path); Assert.IsNotNull(x, "Missing UI object: " + path); return x.GetComponent<Text>(); }
            screen.Shell.StartGame();
            Find("Canvas/Hud/Settings").onClick.Invoke();
            Find("Canvas/Pause/Card/Profile").onClick.Invoke();
            Assert.AreEqual(L10n.T("cloud.status.off"), T("Canvas/Profile/Card/CloudStatus").text, "no cloud on this build");
            Assert.IsFalse(root.transform.Find("Canvas/Profile/Card/RestoreLocal").gameObject.activeSelf, "nothing backed up on the device yet");
            screen.Profile.Close();

            var cloud = new MemoryCloudSave();
            runner.Cloud = cloud;
            Find("Canvas/Pause/Card/Profile").onClick.Invoke();
            Assert.AreEqual(L10n.T("cloud.status.never"), T("Canvas/Profile/Card/CloudStatus").text);
            Find("Canvas/Profile/Card/CloudPush").onClick.Invoke();
            Assert.IsNotNull(cloud.Text, "the garden went up");
            Assert.AreEqual(EnvelopeStatus.Ok, CloudSave.Open(cloud.Text, out _));
            Assert.AreEqual(L10n.T("cloud.status.at", ProfileView.Clock(runner.Sync.LastPushedAt)), T("Canvas/Profile/Card/CloudStatus").text);

            cloud.Online = false;
            Find("Canvas/Profile/Card/CloudPush").onClick.Invoke();
            Assert.AreEqual(L10n.T("cloud.status.offline"), T("Canvas/Profile/Card/CloudStatus").text, "no network: said plainly, nothing lost");
        }

        [Test]
        public void Two_Copies_That_Differ_Bring_Up_The_Choice_Card_And_Taking_The_Cloud_Copy_Replaces_The_Garden()
        {
            Text T(string path) { Transform x = root.transform.Find(path); Assert.IsNotNull(x, "Missing UI object: " + path); return x.GetComponent<Text>(); }
            screen.Shell.StartGame();
            GameState ahead = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, runner.State, runner.UpgradeDefs), runner.UpgradeDefs).State;
            ahead.GardenXp = Garden.XpForLevel(4);
            ahead.Rabbits.Add(new Rabbit { Id = 50, Tier = 1, Kind = "tho-bong", Name = "Bông" });
            ahead.LastSeenUnixSeconds = runner.State.LastSeenUnixSeconds + 60;
            var cloud = new MemoryCloudSave { Text = CloudSave.Wrap(SaveCodec.Encode(runner.Defs, ahead, runner.UpgradeDefs), SaveSummary.Of(ahead), 9000) };
            runner.Cloud = cloud;
            int rabbitsBefore = runner.State.Rabbits.Count;

            runner.SyncNow(SyncReason.Open);
            Assert.IsTrue(screen.CloudChoice.IsOpen, "the choice is the player's");
            Assert.AreEqual(L10n.T("cloud.level", 4), T("Canvas/CloudChoice/Card/Remote/Level").text);
            Assert.AreEqual(L10n.T("cloud.rabbits", ahead.Rabbits.Count, ahead.SeenKinds.Count), T("Canvas/CloudChoice/Card/Remote/Rabbits").text);
            Assert.IsTrue(root.transform.Find("Canvas/CloudChoice/Card/Remote/Suggested").gameObject.activeSelf, "the copy further along is put forward");
            Assert.IsFalse(root.transform.Find("Canvas/CloudChoice/Card/Local/Suggested").gameObject.activeSelf);
            Assert.AreEqual(rabbitsBefore, runner.State.Rabbits.Count, "nothing replaced yet");

            Find("Canvas/CloudChoice/Card/KeepRemote").onClick.Invoke();
            Assert.IsFalse(screen.CloudChoice.IsOpen);
            Assert.AreEqual(ahead.Rabbits.Count, runner.State.Rabbits.Count, "the cloud copy is the garden now");
            Assert.AreEqual(4, Garden.Level(runner.State));
            Assert.IsNull(runner.Sync.Pending);
            Assert.AreEqual(SyncOutcome.TookRemote, runner.Sync.Last.Outcome);

            // Keeping the device's copy instead sends it up.
            cloud.Text = CloudSave.Wrap(SaveCodec.Encode(runner.Defs, ahead, runner.UpgradeDefs), SaveSummary.Of(ahead), 9000);
            runner.State.Rabbits.Clear();
            runner.State.GardenXp = Garden.XpForLevel(7);
            runner.SyncNow(SyncReason.Milestone);
            Assert.IsTrue(screen.CloudChoice.IsOpen);
            Assert.IsTrue(root.transform.Find("Canvas/CloudChoice/Card/Local/Suggested").gameObject.activeSelf, "the higher garden level is put forward");
            Find("Canvas/CloudChoice/Card/KeepLocal").onClick.Invoke();
            Assert.AreEqual(SyncOutcome.Pushed, runner.Sync.Last.Outcome);
            CloudSave.Open(cloud.Text, out CloudEnvelope env);
            Assert.AreEqual(7, env.Summary.GardenLevel);
        }

        [Test]
        public void A_Gold_Rabbit_Or_More_Land_Sends_The_Save_Up_At_Once_But_An_Upgrade_Does_Not()
        {
            var cloud = new MemoryCloudSave();
            runner.Cloud = cloud;
            runner.State.Coins = new BigNum(1e12);
            runner.Buy(0);
            Assert.AreEqual(0, cloud.Pushes, "an upgrade is not a milestone");
            runner.State.Rabbits.Add(new Rabbit { Id = 9, Tier = 4, Kind = "tho-sao", Name = "Sao" });
            runner.Buy(0);
            Assert.AreEqual(1, cloud.Pushes, "a gold rabbit is");
            runner.State.Plots++;
            runner.Buy(0);
            Assert.AreEqual(2, cloud.Pushes, "so is more land");
        }

        [Test]
        public void A_Guest_From_A_Loaded_Save_Already_Stands_At_The_Counter_And_The_Daily_Quest_Counts_Deliveries()
        {
            runner.State.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 40, RewardSeconds = Orders.RewardSeconds };
            runner.State.Order.Lines.Add(new OrderLine { Station = 0, Count = 1 });
            screen.Shell.StartGame();
            screen.Refresh();
            Assert.IsTrue(screen.GuestShown);
            Assert.IsTrue(screen.GuestAtCounter, "no walk: it has been waiting a while");
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Guest/Bubble").gameObject.activeSelf);

            Transform quest = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-don");
            Assert.IsNotNull(quest, "the daily order quest is in the list");
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest don-20"), "and the achievement");
            Tray.Add(runner.State, 0, false);
            Assert.IsTrue(runner.DeliverOrder(out _, out _));
            screen.Refresh();
            StringAssert.StartsWith("1 / 2", quest.Find("Count").GetComponent<Text>().text);
        }

        [Test]
        public void A_Station_Wears_Its_Rank_From_25_100_And_300_And_Every_Level_Text_Says_So()
        {
            Transform station = root.transform.Find("Canvas/Garden/Content/Station am-tra");
            Text plate = station.Find("Level/Text").GetComponent<Text>();
            Motion.SetReduced(true); // the level number lands in one frame instead of running up
            void Show() { screen.Refresh(); screen.Advance(0.1f); }
            runner.State.Levels[0] = 24;
            Show();
            Assert.IsFalse(station.Find("Bunting").gameObject.activeSelf, "nothing before 25");
            Assert.AreEqual(L10n.T("station.level", 24), plate.text);

            runner.State.Levels[0] = 25;
            Show();
            Assert.IsTrue(station.Find("Bunting").gameObject.activeSelf, "bunting from 25");
            Assert.IsFalse(station.Find("RankLanterns").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("station.level", 25) + " ✦", plate.text);
            Assert.AreEqual(Palette.Paper, station.Find("Level").GetComponent<Image>().color);

            runner.State.Levels[0] = 100;
            Show();
            Assert.IsTrue(station.Find("RankLanterns").gameObject.activeSelf, "stone lanterns from 100");
            Assert.AreNotEqual(Palette.Paper, station.Find("Level").GetComponent<Image>().color, "and a golden plate");
            Assert.IsFalse(station.Find("RankHalo").gameObject.activeSelf);
            StringAssert.EndsWith("✦✦", plate.text);

            runner.State.Levels[0] = 300;
            Show();
            Assert.IsTrue(station.Find("RankHalo").gameObject.activeSelf, "a halo from 300");
            StringAssert.EndsWith("✦✦✦", plate.text);
            screen.Advance(0.3f);
            Assert.AreEqual(Vector3.one, station.Find("RankHalo").localScale, "the halo holds still under reduced motion");

            screen.FocusStation(0);
            Show();
            StringAssert.EndsWith("✦✦✦", root.transform.Find("Canvas/Card/Level/Text").GetComponent<Text>().text, "the card carries the marks");
            StringAssert.EndsWith("✦✦✦", root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Level/Text").GetComponent<Text>().text, "and so does the row");

            // A closed station wears nothing, whatever its number.
            runner.State.Levels[1] = 0;
            screen.Refresh();
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station gio-la/Bunting").gameObject.activeSelf);
        }

        [Test]
        public void Reaching_A_Rank_Is_A_Moment_That_Names_It()
        {
            screen.Shell.StartGame();
            runner.State.Levels[0] = 24;
            runner.State.Coins = new BigNum(1e15);
            runner.State.BuyMode = 1;
            screen.FocusStation(0);
            screen.Refresh();
            Find("Canvas/Card/Buy").onClick.Invoke();
            Assert.AreEqual(25, runner.State.Levels[0]);
            StringAssert.Contains(L10n.T("moment.rank.title", 1), root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Station am-tra/Bunting").gameObject.activeSelf, "and the bunting is up at once");
        }

        [Test]
        public void The_Mooncake_Game_Shows_The_Dough_Being_Worked_A_Pestle_That_Drops_And_The_Cake_At_The_End()
        {
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(3);
            screen.Refresh();
            screen.OpenPounding();
            Transform pound = root.transform.Find("Canvas/Pound/Shake");
            Image cake = pound.Find("Cake").GetComponent<Image>();
            Assert.AreEqual("banh_bot", cake.sprite.name, "raw dough to begin with");
            Assert.AreEqual("prop_coi-trong", pound.Find("Mortar").GetComponent<Image>().sprite.name, "an empty mortar: the pestle is its own");
            var pestle = (RectTransform)pound.Find("Pestle");
            Assert.AreEqual("prop_chay", pestle.GetComponent<Image>().sprite.name);
            float rest = pestle.anchoredPosition.y;

            // Fifteen pounds on the beat: the dough is being worked, and the pestle is down right after a pound.
            for (int b = 0; b < 15; b++)
            {
                screen.Advance((float)(Pounding.BeatTime(b) - screen.Pound.Time));
                screen.Pound.Tap();
            }
            screen.Advance(0.02f);
            Assert.Less(pestle.anchoredPosition.y, rest, "the pestle comes down");
            Assert.AreEqual("banh_dang-gia", cake.sprite.name);
            screen.Advance(0.4f);
            Assert.AreEqual(rest, pestle.anchoredPosition.y, 0.01f, "and back up to where it rests");

            // A beat left to pass dents the cake for a moment.
            screen.Advance((float)(Pounding.BeatTime(15) + Pounding.GoodWindow + 0.05 - screen.Pound.Time));
            Assert.AreEqual("banh_meo", cake.sprite.name, "dented after a miss");
            screen.Advance(0.7f);
            Assert.AreEqual("banh_dang-gia", cake.sprite.name, "then worked on again");

            for (int k = 0; k < 60; k++) screen.Advance(0.5f);
            Assert.IsTrue(root.transform.Find("Canvas/Pound/Result").gameObject.activeSelf);
            Assert.AreEqual("banh_xong", root.transform.Find("Canvas/Pound/Result/Card/Cake").GetComponent<Image>().sprite.name, "fifteen perfects earn a star, so the cake came out");
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void While_A_Boost_Runs_The_Tea_Bar_Name_And_Its_Countdown_Keep_To_Their_Own_Halves(string locale, bool large)
        {
            L10n.SetLocale(locale);
            UiKit.SetLargeText(large);
            screen.RebuildUi(); // in play the text-changed event calls this
            screen.Shell.StartGame();
            runner.State.Energy = 28;
            runner.State.BoostSecondsLeft = 30;
            screen.Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
            Canvas.ForceUpdateCanvases();
            Text label = root.transform.Find("Canvas/Hud/Energy/Face/Label").GetComponent<Text>();
            Text action = root.transform.Find("Canvas/Hud/Energy/Face/Action").GetComponent<Text>();
            Assert.AreEqual(L10n.T("energy.boosting", 30), action.text);
            // Two boxes that do not meet…
            Assert.LessOrEqual(label.rectTransform.anchorMax.x, action.rectTransform.anchorMin.x, "the name's box ends where the countdown's begins");
            // …each wide enough for its words on one line at the smallest size the text may shrink to.
            foreach (Text text in new[] { label, action })
            {
                int size = text.fontSize; bool fit = text.resizeTextForBestFit;
                text.resizeTextForBestFit = false;
                text.fontSize = text.resizeTextMinSize;
                Assert.LessOrEqual(text.preferredWidth, text.rectTransform.rect.width + 1f, text.name + " \"" + text.text + "\" fits its half at " + text.resizeTextMinSize + "px (" + locale + (large ? ", large" : "") + ")");
                text.fontSize = size; text.resizeTextForBestFit = fit;
            }
            UiKit.SetLargeText(false);
            L10n.SetLocale("vi");
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Firefly_Jar_And_Its_Plate_Stay_Inside_The_Screen_And_Clear_Of_The_Canopy(string locale, bool large)
        {
            L10n.SetLocale(locale);
            UiKit.SetLargeText(large);
            screen.RebuildUi(); // in play the text-changed event calls this
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(5);
            screen.Refresh();
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var jar = (RectTransform)street.Find("Jar");
            Assert.IsTrue(jar.gameObject.activeSelf, "the jar is out from garden level five");
            var plate = (RectTransform)jar.Find("Plate");
            // Where the plate's edges land on the garden; the garden is as wide as the screen, so at zoom 1 these are screen units too.
            float centre = jar.anchoredPosition.x + plate.anchoredPosition.x;
            float left = centre - plate.rect.width / 2f, right = centre + plate.rect.width / 2f;
            Assert.GreaterOrEqual(left, 24f, "inside the left edge");
            Assert.LessOrEqual(right, 1080f - 24f, "inside the right edge");
            Assert.LessOrEqual(right, GardenScreen.ReadableRight, "and short of the trees at the edge, which are drawn over it");
            Text text = plate.Find("Text").GetComponent<Text>();
            Assert.AreEqual(L10n.T("jar.plays", 3), text.text);
            int size = text.fontSize; bool fit = text.resizeTextForBestFit;
            text.resizeTextForBestFit = false;
            text.fontSize = text.resizeTextMinSize;
            Assert.LessOrEqual(text.preferredWidth, text.rectTransform.rect.width + 1f, "\"" + text.text + "\" fits the plate on one line at " + text.resizeTextMinSize + "px (" + locale + (large ? ", large" : "") + ")");
            text.fontSize = size; text.resizeTextForBestFit = fit;
            UiKit.SetLargeText(false);
            L10n.SetLocale("vi");
        }

        [Test]
        public void A_Working_Rabbit_Holds_Its_Stations_Tool_And_Works_It_Between_Fixed_Bounds()
        {
            screen.Shell.StartGame();
            Transform station = root.transform.Find("Canvas/Garden/Content/Station am-tra");
            Assert.IsFalse(station.Find("Rabbit/Tool").gameObject.activeSelf, "no tool without a rabbit");
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            Motion.SetReduced(true);
            screen.Refresh();
            for (int k = 0; k < 8; k++) screen.Advance(0.5f); // the newcomer walks over first
            var tool = (RectTransform)station.Find("Rabbit/Tool");
            Assert.IsTrue(tool.gameObject.activeSelf, "a tool in the paw");
            Assert.AreEqual(runner.Catalog.TradeIcon(runner.Defs[0].Trade), tool.GetComponent<Image>().sprite, "the station's trade, not the rabbit's");
            Assert.AreEqual(Quaternion.identity, tool.localRotation, "held still under reduced motion");

            Motion.SetReduced(false);
            float maxAngle = 0f; bool moved = false; Vector2 home = tool.anchoredPosition;
            for (int k = 0; k < 40; k++)
            {
                screen.Advance(0.05f);
                float a = Mathf.Abs(Mathf.DeltaAngle(0f, tool.localRotation.eulerAngles.z));
                maxAngle = Mathf.Max(maxAngle, a);
                if (Vector2.Distance(tool.anchoredPosition, home) > 1f || a > 0.5f) moved = true;
            }
            Assert.IsTrue(moved, "the tool is worked");
            Assert.LessOrEqual(maxAngle, 25f, "between fixed bounds");

            runner.State.Rabbits[0].Station = -1;
            screen.Refresh();
            Assert.IsFalse(tool.gameObject.activeSelf, "and goes with the rabbit");
        }

        [Test]
        public void Resting_Rabbits_Squat_On_Their_Seats_Lie_In_The_Hammock_And_Wear_Their_Hat_And_Name()
        {
            runner.State.RestSpots = RestSpots.Max;
            for (int i = 1; i <= 5; i++) runner.State.Rabbits.Add(new Rabbit { Id = i, Tier = 5 - i, Kind = RabbitKinds.OfTier(5 - i)[0].Id, Name = "T" + i });
            runner.State.NextRabbitId = 5;
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 0);
            runner.EquipItem(hat.Id, 1); // the rarest, who takes the first seat
            screen.Refresh();
            Transform world = root.transform.Find("Canvas/Garden/Content");
            var stump = (RectTransform)world.Find("Seat 0/Art");
            Assert.Less(stump.localScale.y, 1f, "a squat on the stump");
            Assert.Greater(stump.localScale.x, 1f);
            Assert.AreEqual(0f, Mathf.DeltaAngle(0f, stump.localRotation.eulerAngles.z), 0.01f);
            var hammock = (RectTransform)world.Find("Seat 4/Art");
            Assert.AreEqual(-72f, Mathf.DeltaAngle(0f, hammock.localRotation.eulerAngles.z), 0.01f, "lying along the hammock");
            Assert.AreEqual("T1", world.Find("Seat 0/Name/Text").GetComponent<Text>().text, "the name under the seat");
            Assert.AreEqual("T5", world.Find("Seat 4/Name/Text").GetComponent<Text>().text);
            Transform hatOn = world.Find("Seat 0/Art/HatOn");
            bool noHat = RabbitKinds.Find(runner.State.Rabbits[0].Kind).NoHat;
            Assert.AreEqual(!noHat, hatOn.gameObject.activeSelf, "the hat comes along to the seat");
            if (!noHat) Assert.AreEqual(runner.Catalog.ItemIcon(hat.DefId), hatOn.GetComponent<Image>().sprite);
            Assert.IsFalse(world.Find("Seat 1/Art/HatOn").gameObject.activeSelf, "no hat on a rabbit that wears none");
        }

        [Test]
        public void The_Profile_Offers_The_Privacy_Policy_Support_And_Deleting_Everything_After_Two_Confirmations()
        {
            screen.Shell.StartGame();
            runner.State.Rabbits.Add(new Rabbit { Id = 7, Tier = 2, Kind = "tho-bong", Name = "Bông" });
            runner.State.Coins = new BigNum(12345);
            runner.State.GardenXp = Garden.XpForLevel(6);
            var cloud = new MemoryCloudSave();
            runner.Cloud = cloud;
            runner.PushToCloud();
            Assert.IsNotNull(cloud.Text);
            string opened = null;
            ProfileView.OpenUrl = url => opened = url;
            int rabbitsBefore = runner.State.Rabbits.Count;

            Find("Canvas/Hud/Settings").onClick.Invoke();
            Find("Canvas/Pause/Card/Profile").onClick.Invoke();
            Find("Canvas/Profile/Card/Privacy").onClick.Invoke();
            Assert.AreEqual(AppInfo.PrivacyUrl, opened, "the policy opens at its address");
            StringAssert.Contains(AppInfo.SupportEmail, root.transform.Find("Canvas/Profile/Card/Support").GetComponent<Text>().text);

            Text label = root.transform.Find("Canvas/Profile/Card/DeleteData/Text").GetComponent<Text>();
            Assert.AreEqual(L10n.T("privacy.delete"), label.text);
            Find("Canvas/Profile/Card/DeleteData").onClick.Invoke();
            Assert.AreEqual(L10n.T("privacy.delete.once"), label.text, "first confirmation asked");
            Assert.AreEqual(rabbitsBefore, runner.State.Rabbits.Count, "nothing deleted yet");
            Find("Canvas/Profile/Card/DeleteData").onClick.Invoke();
            Assert.AreEqual(L10n.T("privacy.delete.twice"), label.text, "second confirmation asked");
            Assert.IsNotNull(cloud.Text, "still nothing deleted");
            Find("Canvas/Profile/Card/DeleteData").onClick.Invoke();
            Assert.IsNull(cloud.Text, "the cloud copy is gone");
            Assert.AreEqual(1, cloud.Deletes);
            Assert.AreEqual(1, runner.State.Rabbits.Count, "a new garden with its first rabbit");
            Assert.IsNull(Rabbits.Find(runner.State, 7), "the old rabbit is gone");
            Assert.AreEqual(0d, runner.State.Coins.ToDouble());
            Assert.AreEqual(1, Garden.Level(runner.State));
            Assert.IsFalse(screen.Profile.IsOpen, "the profile closes on the fresh garden");
            Assert.AreEqual(ShellState.Playing, screen.Shell.State);
            ProfileView.OpenUrl = url => Application.OpenURL(url);
        }
    }
}

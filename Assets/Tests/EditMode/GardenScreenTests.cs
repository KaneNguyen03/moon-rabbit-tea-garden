using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;

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
        public void Tapping_A_Sleeping_Rabbit_Does_Nothing()
        {
            Find("Canvas/Garden/Content/Station noi-mut").onClick.Invoke();
            Assert.AreEqual(0, runner.State.Progress[2]);
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

            Find("Canvas/Sheet/Panel/Bar/Tab Bùa").onClick.Invoke();
            Assert.AreEqual(1, screen.Tab);
            Assert.IsFalse(panel.Find("Scroll").gameObject.activeSelf);
            Assert.IsTrue(panel.Find("Upgrades").gameObject.activeSelf);
            Assert.IsFalse(panel.Find("Bar/Modes").gameObject.activeSelf);

            Find("Canvas/Sheet/Panel/Bar/Tab Nhiệm vụ").onClick.Invoke();
            Assert.IsTrue(panel.Find("Quests").gameObject.activeSelf);
            Find("Canvas/Sheet/Panel/Bar/Tab Trạm").onClick.Invoke();
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
            GameObject badge = root.transform.Find("Canvas/Sheet/Panel/Bar/Badge").gameObject;
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
            Find("Canvas/Sheet/Panel/Bar/Tab Thỏ").onClick.Invoke();
            Assert.AreEqual(3, screen.Tab);
            Button summon = Find("Canvas/Sheet/Panel/Rabbits/Content/Summon/Button");
            Assert.IsFalse(summon.interactable, "no clovers yet");

            runner.State.Clovers = 3;
            screen.Refresh();
            Assert.IsTrue(summon.interactable);
            summon.onClick.Invoke();
            Assert.AreEqual(1, runner.State.Rabbits.Count);
            Assert.AreEqual(2, runner.State.Clovers);
            screen.Refresh();

            Rabbit first = runner.State.Rabbits[0];
            Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit " + first.Id + "/Work").onClick.Invoke();
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
        public void Fifth_Station_Needs_A_Plot_Bought_From_Its_Card()
        {
            for (int i = 0; i < 4; i++) runner.State.Levels[i] = 1;
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            Button openFifth = Find("Canvas/Sheet/Panel/Scroll/Content/Row lo-banh/Buy");
            Assert.IsFalse(openFifth.interactable, "no plot yet");
            Transform plot = root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Plot");
            Assert.AreEqual(4, plot.GetSiblingIndex(), "right after the fourth station");

            Find("Canvas/Sheet/Panel/Scroll/Content/Plot/Buy").onClick.Invoke();
            Assert.AreEqual(5, runner.State.Plots);
            screen.Refresh();
            Assert.IsTrue(openFifth.interactable);
            Assert.AreEqual(5, plot.GetSiblingIndex());

            runner.State.Plots = runner.Defs.Count;
            screen.Refresh();
            Assert.IsFalse(plot.gameObject.activeSelf, "nothing left to sell");
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
        public void Quick_Button_Buys_At_The_Station_And_Sells_The_Next_Plot()
        {
            Button quick = Find("Canvas/Garden/Content/Station am-tra/Quick");
            Assert.IsFalse(quick.interactable);
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();
            Assert.IsTrue(quick.interactable);
            quick.onClick.Invoke();
            Assert.AreEqual(2, runner.State.Levels[0]);

            for (int i = 1; i < 4; i++) Find("Canvas/Garden/Content/Station " + runner.Defs[i].Id + "/Quick").onClick.Invoke();
            Assert.AreEqual(1, runner.State.Levels[3]);
            screen.Refresh();

            // The fifth station stands on the street as empty land, and its button sells the plot.
            Transform fifth = root.transform.Find("Canvas/Garden/Content/Station lo-banh");
            Assert.IsTrue(fifth.gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station to-mat").gameObject.activeSelf, "beyond the next plot stays hidden");
            StringAssert.StartsWith("Mua đất", fifth.Find("Quick/Face/Text").GetComponent<Text>().text);
            Find("Canvas/Garden/Content/Station lo-banh/Quick").onClick.Invoke();
            Assert.AreEqual(5, runner.State.Plots);
            screen.Refresh();
            StringAssert.StartsWith("Mở", fifth.Find("Quick/Face/Text").GetComponent<Text>().text);
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Station to-mat").gameObject.activeSelf);
        }

        [Test]
        public void Street_Gets_Longer_As_Plots_Are_Bought()
        {
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            float before = street.sizeDelta.x;
            runner.State.Plots = 9;
            screen.Refresh();
            Assert.Greater(street.sizeDelta.x, before + 1000f);
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

            Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/LevelUp").onClick.Invoke();
            Assert.AreEqual(2, runner.State.Rabbits[0].Level);
            Assert.AreEqual(900, runner.State.Coins.ToDouble(), 1e-6);

            screen.Refresh();
            Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Hat").onClick.Invoke();
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

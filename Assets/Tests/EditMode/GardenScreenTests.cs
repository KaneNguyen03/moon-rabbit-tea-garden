using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;

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
        public void The_Card_Opens_A_Sleeping_Station_And_Sells_The_Next_Plot()
        {
            for (int i = 0; i < 4; i++) runner.State.Levels[i] = 1;
            runner.State.Coins = new BigNum(1e12);
            screen.Refresh();

            // The fifth station stands in the garden as empty land; the one after it is not there yet.
            Transform fifth = root.transform.Find("Canvas/Garden/Content/Station lo-banh");
            Assert.IsTrue(fifth.gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station to-mat").gameObject.activeSelf);

            Find("Canvas/Garden/Content/Station lo-banh").onClick.Invoke();
            Transform card = root.transform.Find("Canvas/Card");
            Assert.AreEqual("Mua đất", card.Find("Buy/Face/Top").GetComponent<Text>().text);
            Find("Canvas/Card/Buy").onClick.Invoke();
            Assert.AreEqual(5, runner.State.Plots);
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
        public void A_Collected_Rabbit_Stands_At_Its_Station_Wearing_Its_Hat()
        {
            Transform rabbit = root.transform.Find("Canvas/Garden/Content/Station am-tra/Rabbit");
            Sprite own = rabbit.GetComponent<Image>().sprite;
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);

            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 4, Kind = "tho-sao", Name = "Sao", Station = 0 });
            runner.State.NextRabbitId = 1;
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 0);
            runner.EquipItem(hat.Id, 1);
            screen.Refresh();
            Assert.AreEqual("kind_tho-sao", rabbit.GetComponent<Image>().sprite.name, "the collected rabbit, not the station's own");
            Assert.IsTrue(rabbit.Find("HatOn").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Station am-tra/Halo").gameObject.activeSelf, "gold and red rabbits get a warm light");
            for (int i = 0; i < 40; i++) screen.Advance(0.1f);
            Assert.AreEqual("kind_tho-sao", rabbit.GetComponent<Image>().sprite.name, "the station's work frames do not take over");

            runner.ToggleRabbitWork(1); // off to rest
            screen.Refresh();
            Assert.AreEqual(own, rabbit.GetComponent<Image>().sprite, "the station's own rabbit is back");
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);
        }

        [Test]
        public void A_Double_Tap_On_A_Station_Flies_In_Close()
        {
            var station = Find("Canvas/Garden/Content/Station am-tra");
            station.onClick.Invoke();
            station.onClick.Invoke();
            screen.Advance(1f); // let the flight finish
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
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

            Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Rename").onClick.Invoke();
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
            Assert.AreEqual("Bánh Trôi", root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Name").GetComponent<Text>().text);
            Assert.AreEqual("Bánh Trôi", root.transform.Find("Canvas/Garden/Content/Station am-tra/Tier/Text").GetComponent<Text>().text);

            Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Rename").onClick.Invoke();
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
            Assert.IsNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Tile/HatOn"));

            runner.EquipItem(hat.Id, 1);
            runner.EquipItem(charm.Id, 1);
            screen.Refresh();
            Assert.IsTrue(rabbit.Find("HatOn").gameObject.activeSelf);
            Assert.IsTrue(rabbit.Find("CharmOn").gameObject.activeSelf);
            Assert.AreEqual("item_mu-rom", rabbit.Find("HatOn").GetComponent<Image>().sprite.name);
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Tile/HatOn"));
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Rabbit 1/Hat/Icon"), "the slot shows the item's picture");

            runner.UnequipItem(hat.Id);
            screen.Refresh();
            Assert.IsFalse(rabbit.Find("HatOn").gameObject.activeSelf);
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

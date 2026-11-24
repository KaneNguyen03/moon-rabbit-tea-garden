using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.Platform;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
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
            runner.Init(GameState.NewGame(catalog.BuildDefs()));
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            Haptics.SetOn(true);
            Notifications.Notifier = new FakeNotifier();
            Notifications.SetOn(false);
            Notifications.Notifier = new LogNotifier();
            L10n.SetLocale("vi");
        }

        Button Find(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<Button>();
        }

        void GrowToLevel(int level)
        {
            screen.Shell.SkipTitle();
            GameState s = runner.State;
            s.GardenXp = Garden.XpForLevel(level);
            s.Coins = new BigNum(50000);
            s.Levels[1] = 40; s.Levels[2] = 12;
            s.Plots = 6;
            s.RestSpots = 3;
            Rabbits.Assign(s, Rabbits.Arrive(s, 2, "Lam", 0d).Id, 2);
            screen.Refresh();
        }

        [Test]
        public void The_Pause_Card_Offers_The_New_Moon_Only_Once_The_Garden_Is_Far_Enough()
        {
            screen.Shell.SkipTitle();
            screen.Shell.OpenPause();
            Transform locked = root.transform.Find("Canvas/Pause/Card/NewMoonLocked");
            Assert.IsTrue(locked.gameObject.activeSelf, "a new garden sees the level it needs");
            StringAssert.Contains(NewMoon.FirstLevel.ToString(), locked.GetComponent<Text>().text);
            Assert.IsFalse(root.transform.Find("Canvas/Pause/Card/NewMoon").gameObject.activeSelf);
            screen.Shell.Resume();
            screen.Advance(0.5f);

            GrowToLevel(NewMoon.FirstLevel);
            screen.Shell.OpenPause();
            Assert.IsTrue(root.transform.Find("Canvas/Pause/Card/NewMoon").gameObject.activeSelf, "far enough: the button is there");
            Assert.IsFalse(locked.gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/Pause/Card/NewMoon/Face/Badge").gameObject.activeSelf);
        }

        [Test]
        public void The_Compare_Card_Shows_The_Saves_Own_Numbers_And_A_Short_Hold_Arms_Nothing()
        {
            GrowToLevel(NewMoon.FirstLevel);
            screen.Shell.OpenPause();
            Find("Canvas/Pause/Card/NewMoon").onClick.Invoke();
            Assert.IsTrue(screen.NewMoonCard.IsOpen);
            string keep = root.transform.Find("Canvas/NewMoon/Card/Keep/Body").GetComponent<Text>().text;
            string lose = root.transform.Find("Canvas/NewMoon/Card/Lose/Body").GetComponent<Text>().text;
            string gain = root.transform.Find("Canvas/NewMoon/Card/Gain/Body").GetComponent<Text>().text;
            StringAssert.Contains(runner.State.Rabbits.Count + " bé thỏ", keep);
            StringAssert.Contains("Ô đất 6 → 4", lose);
            StringAssert.Contains("chỗ nghỉ 3 → 2", lose);
            StringAssert.Contains("cấp vườn 30 → 1", lose);
            StringAssert.Contains("+0% → +25%", gain);
            StringAssert.Contains("Thỏ Trăng Non", gain);

            screen.NewMoonCard.Hold();
            screen.Advance(0.8f);
            screen.NewMoonCard.Release();
            Assert.IsFalse(screen.NewMoonCard.Armed, "too short a hold");
            screen.NewMoonCard.Hold();
            screen.Advance(1.2f);
            screen.NewMoonCard.Cancel();
            Assert.IsFalse(screen.NewMoonCard.Armed, "a finger that slid off");
            Assert.AreEqual(0, runner.State.Season);
            StringAssert.Contains("1,5", root.transform.Find("Canvas/NewMoon/Card/Begin/Face/Text").GetComponent<Text>().text);
        }

        [Test]
        public void A_Full_Hold_Then_A_Press_Begins_The_Season_Shows_The_Moment_And_Brings_The_Moon_Rabbit()
        {
            Motion.SetReduced(true);
            GrowToLevel(NewMoon.FirstLevel);
            int rabbits = runner.State.Rabbits.Count;
            screen.Shell.OpenPause();
            Find("Canvas/Pause/Card/NewMoon").onClick.Invoke();
            screen.NewMoonCard.Hold();
            screen.Advance(NewMoonView.HoldSeconds + 0.1f);
            screen.NewMoonCard.Release();
            Assert.IsTrue(screen.NewMoonCard.Armed);
            StringAssert.Contains("Trăng 1", root.transform.Find("Canvas/NewMoon/Card/Begin/Face/Text").GetComponent<Text>().text);
            Assert.AreEqual(0, runner.State.Season, "armed is not begun");

            screen.NewMoonCard.Hold();
            screen.NewMoonCard.Release();
            Assert.IsFalse(screen.NewMoonCard.IsOpen);
            Assert.AreEqual(1, runner.State.Season);
            Assert.AreEqual(ShellState.Playing, screen.Shell.State);
            Assert.IsTrue(screen.NewMoonMoment.IsOpen, "the moon sets and rises");
            Assert.AreEqual(4, runner.State.Plots);
            Assert.AreEqual(1, runner.State.Levels[0]);
            Assert.AreEqual(0, runner.State.Levels[1]);
            Assert.AreEqual(rabbits + 1, runner.State.Rabbits.Count);

            screen.Advance(0.5f);
            screen.Advance(0.5f);
            Assert.IsFalse(screen.NewMoonMoment.IsOpen, "with reduced motion the moment is short");
            Assert.IsTrue(screen.Arrival.IsOpen, "the moon rabbit arrives");
            Rabbit moon = runner.State.Rabbits[runner.State.Rabbits.Count - 1];
            Assert.AreEqual(NewMoon.MoonKind, moon.Kind);
            Transform chip = root.transform.Find("Canvas/Hud/Season");
            Assert.IsTrue(chip.gameObject.activeSelf);
            Assert.AreEqual("Trăng 1", chip.Find("Text").GetComponent<Text>().text);
            Assert.AreEqual(1.25d * RestSpots.Multiplier(runner.State), runner.Mods.IncomeMultiplier, 1e-9, "the season's quarter, times the seated rabbits' share (#60)");
        }

        [Test]
        public void A_Tap_Ends_The_Moment_Early()
        {
            GrowToLevel(NewMoon.FirstLevel);
            screen.Shell.OpenPause();
            Find("Canvas/Pause/Card/NewMoon").onClick.Invoke();
            screen.NewMoonCard.Hold();
            screen.Advance(NewMoonView.HoldSeconds + 0.1f);
            screen.NewMoonCard.Release();
            screen.NewMoonCard.Hold();
            screen.NewMoonCard.Release();
            screen.Advance(0.5f);
            Assert.IsTrue(screen.NewMoonMoment.IsOpen);
            Assert.Less(screen.NewMoonMoment.Progress, 1f);
            Find("Canvas/NewMoonMoment").onClick.Invoke();
            Assert.IsFalse(screen.NewMoonMoment.IsOpen);
            Assert.IsTrue(screen.Arrival.IsOpen);
        }

        [Test]
        public void The_Shop_Sells_Golden_Packs_Through_The_Store_And_Pays_Out_Once()
        {
            screen.Shell.StartGame();
            screen.Refresh();
            var store = (FakeStorefront)runner.Store;
            Find("Canvas/Garden/Content/Shop").onClick.Invoke();
            Find("Canvas/Shop/Card/Have/BuyMore").onClick.Invoke();
            Assert.IsTrue(screen.GoldenPacks.IsOpen);
            Transform card = root.transform.Find("Canvas/GoldenPacks/Card");
            StringAssert.Contains("150", card.Find("Pack 1/Face/Text").GetComponent<Text>().text);
            Assert.AreEqual(store.PriceOf(Storefront.Medium), card.Find("Pack 1/Face/PriceChip/Price").GetComponent<Text>().text, "the store's own price");
            Assert.IsTrue(card.Find("Pack 1/Best").gameObject.activeSelf, "the middle pack is the best deal");
            Assert.IsTrue(Find("Canvas/GoldenPacks/Card/Pack 0").interactable);

            int before = runner.State.GoldenCarrots;
            Find("Canvas/GoldenPacks/Card/Pack 0").onClick.Invoke();
            Assert.AreEqual(before + 50, runner.State.GoldenCarrots);
            Assert.AreEqual(1, runner.State.PacksBought);
            StringAssert.Contains("50", screen.GoldenPacks.Status);
            Assert.Greater(screen.Feel.Flying, 0, "the carrots fly to their chip");
            Assert.AreEqual("" + (before + 50), root.transform.Find("Canvas/Hud/Golden/Text").GetComponent<Text>().text);

            store.NextOutcome = PurchaseOutcome.Cancelled;
            Find("Canvas/GoldenPacks/Card/Pack 2").onClick.Invoke();
            Assert.AreEqual(before + 50, runner.State.GoldenCarrots, "a cancelled purchase pays nothing");
            Assert.AreEqual(L10n.T("packs.cancelled"), screen.GoldenPacks.Status);

            store.Deliver(Storefront.Large, "GPA.again");
            Assert.AreEqual(before + 550, runner.State.GoldenCarrots);
            store.Deliver(Storefront.Large, "GPA.again");
            Assert.AreEqual(before + 550, runner.State.GoldenCarrots);

            Find("Canvas/GoldenPacks/Card/Restore").onClick.Invoke();
            Assert.AreEqual(1, store.Restores);
            Assert.AreEqual(L10n.T("packs.restored"), screen.GoldenPacks.Status);
        }

        [Test]
        public void Without_A_Store_The_Packs_Are_Greyed_And_The_Card_Says_So()
        {
            screen.Shell.StartGame();
            var store = (FakeStorefront)runner.Store;
            store.Ready = false;
            screen.GoldenPacks.Open(runner.Store);
            Assert.IsFalse(Find("Canvas/GoldenPacks/Card/Pack 0").interactable);
            Assert.IsFalse(Find("Canvas/GoldenPacks/Card/Restore").interactable);
            Assert.AreEqual(L10n.T("packs.notready"), screen.GoldenPacks.Status);
            Assert.AreEqual(L10n.T("packs.price.unknown"), root.transform.Find("Canvas/GoldenPacks/Card/Pack 0/Face/PriceChip/Price").GetComponent<Text>().text);
            int before = runner.State.GoldenCarrots;
            Find("Canvas/GoldenPacks/Card/Pack 0").onClick.Invoke();
            Assert.AreEqual(before, runner.State.GoldenCarrots);
        }

        [Test]
        public void Runner_Starts_With_Sixteen_Stations_And_First_Open()
        {
            Assert.AreEqual(16, runner.Defs.Count);
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
            screen.Refresh();
            StringAssert.StartsWith("2 cà rốt", root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Sub").GetComponent<Text>().text);
        }

        [Test]
        public void Quest_Badge_Appears_And_Claiming_Pays()
        {
            GameObject badge = root.transform.Find("Canvas/TabBar/Badge").gameObject;
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
        public void The_Hud_Shows_The_Garden_Level_And_Land_Not_Yet_Opened_Is_A_Dashed_Ring_On_Clear_Grass()
        {
            Transform hud = root.transform.Find("Canvas/Hud");
            Assert.AreEqual("1", hud.Find("Level/Text").GetComponent<Text>().text);
            runner.State.GardenXp = Garden.XpForLevel(4) + Garden.XpToNext(4) / 2;
            screen.Refresh();
            Assert.AreEqual("4", hud.Find("Level/Text").GetComponent<Text>().text);
            Assert.AreEqual(0.5f, hud.Find("Level/Ring").GetComponent<Image>().fillAmount, 0.01f);

            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.IsNull(world.Find("Ring 3"), "the first four plots are the garden's own");
            for (int i = Plots.Starting; i < runner.Defs.Count; i++)
            {
                Assert.IsNull(world.Find("Mist " + i), "no mist on plot " + i);
                Assert.IsNull(world.Find("Grove " + i), "nothing grows on plot " + i);
                Transform ring = world.Find("Ring " + i);
                Assert.IsNotNull(ring, "plot " + i + " is marked");
                Assert.IsTrue(ring.gameObject.activeSelf);
                Assert.Greater(ring.childCount, 12, "a ring of dashes");
                Assert.IsFalse(world.Find("Bloom " + i).gameObject.activeSelf, "no fresh grass before the land is bought");
            }
            for (int i = 0; i < 5; i++) runner.State.Levels[i] = 1;
            runner.State.Plots = 6;
            screen.Refresh();
            for (int i = 0; i < 20; i++) screen.Advance(0.1f);
            foreach (int i in new[] { 4, 5 })
            {
                Assert.IsFalse(world.Find("Ring " + i).gameObject.activeSelf, "plot " + i + " is the garden's now");
                Transform bloom = world.Find("Bloom " + i);
                Assert.IsTrue(bloom.gameObject.activeSelf, "fresh grass on plot " + i);
                Assert.AreEqual(1f, bloom.localScale.x, 1e-3f);
                Assert.AreEqual(1f, bloom.GetComponent<CanvasGroup>().alpha, 1e-3f);
            }
            Assert.IsTrue(world.Find("Ring 6").gameObject.activeSelf);
            Assert.IsTrue(world.Find("Ring 11").gameObject.activeSelf);
        }

        List<KeyValuePair<string, Rect>> RimPieces()
        {
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var rim = new List<KeyValuePair<string, Rect>>();
            for (int k = 0; k < canopy.childCount; k++)
            {
                var rt = (RectTransform)canopy.GetChild(k);
                if (!rt.gameObject.activeSelf || !rt.name.StartsWith("Rim ") || rt.name == "Rim Shade") continue;
                Vector2 at = rt.anchoredPosition, size = rt.rect.size;
                rim.Add(new KeyValuePair<string, Rect>(rt.name, new Rect(at.x - rt.pivot.x * size.x, at.y - rt.pivot.y * size.y, size.x, size.y)));
            }
            return rim;
        }

        void HavePlots(int plots)
        {
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = i < plots - 2 || i < Plots.Starting ? 1 : 0;
            runner.State.Plots = plots;
            screen.Refresh();
        }

        [TestCase(4)]
        [TestCase(6)]
        [TestCase(8)]
        [TestCase(10)]
        [TestCase(12)]
        public void The_Forest_Rim_Stands_On_No_Plot_And_Two_Hundred_Units_From_Every_Open_Station(int plots)
        {
            Motion.SetReduced(true);
            HavePlots(plots);
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            List<KeyValuePair<string, Rect>> rim = RimPieces();
            Assert.Greater(rim.Count, 20, "a rim of forest around the lawn");
            var kinds = new HashSet<string>();
            foreach (KeyValuePair<string, Rect> piece in rim) kinds.Add(piece.Key);
            Assert.GreaterOrEqual(kinds.Count, 4, "built from four or five kinds of piece: " + string.Join(", ", kinds));

            int onPlots = 0;
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Transform station = street.Find("Station " + runner.Defs[i].Id);
                var parts = new List<RectTransform>();
                Transform front = street.Find("Station " + runner.Defs[i].Id + " Front");
                foreach (string part in new[] { "Level", "Pad", "Floor", "Prop", "Rabbit", "Tier", "Ground" }) parts.Add((RectTransform)(part == "Level" || part == "Pad" ? front : station).Find(part));
                if (i >= Plots.Starting) parts.Add((RectTransform)street.Find("Ring " + i));
                bool open = i < plots;
                foreach (RectTransform part in parts)
                {
                    Rect at = GardenRect(part, street);
                    foreach (KeyValuePair<string, Rect> piece in rim)
                    {
                        float c = Clearance(at, piece.Value);
                        if (c < 0f) onPlots++;
                        if (open) Assert.GreaterOrEqual(c, 200f, runner.Defs[i].Id + "/" + part.name + " is open and keeps 200 units from the rim; " + piece.Key + " at " + piece.Value);
                    }
                }
            }
            Assert.AreEqual(0, onPlots, "no piece of the rim stands on a plot, bought or not, at " + plots + " plots");
            foreach (string path in new[] { "Basket", "Basket/Plate", "Jar", "Jar/Plate", "Mortar", "Mortar/Plate" })
            {
                Rect at = GardenRect((RectTransform)street.Find(path), street);
                foreach (KeyValuePair<string, Rect> piece in rim)
                    Assert.GreaterOrEqual(Clearance(at, piece.Value), 200f, path + " keeps 200 units from the rim; " + piece.Key + " at " + piece.Value);
            }
            float heart = GardenScreen.CellToWorld(new Hex(0, 0)).x, left = heart - (GardenGrid.Reach(GardenGrid.SegmentsForPlots(plots), GardenSide.Left) + 1) * 75f,
                right = heart + (GardenGrid.Reach(GardenGrid.SegmentsForPlots(plots), GardenSide.Right) + 1) * 75f;
            foreach (KeyValuePair<string, Rect> piece in rim)
                Assert.IsTrue(piece.Value.xMax <= left + 0.5f || piece.Value.xMin >= right - 0.5f, piece.Key + " at " + piece.Value + " stands at the lawn's end, not on it");
        }

        [Test]
        public void No_Mist_Lies_On_The_Garden_At_Any_Land_Wave()
        {
            Sprite mist = runner.Catalog.Mist;
            Transform garden = root.transform.Find("Canvas/Garden");
            for (int plots = Plots.Starting; plots <= runner.Defs.Count; plots += 2)
            {
                HavePlots(plots);
                for (int i = 0; i < 20; i++) screen.Advance(0.1f);
                foreach (Image img in garden.GetComponentsInChildren<Image>(false))
                    Assert.IsTrue(mist == null || img.sprite != mist, img.name + " is mist over the garden at " + plots + " plots");
            }
        }

        [Test]
        public void Buying_A_Wave_Moves_The_Rim_Out_On_That_Side_And_The_Trees_Fade_Over()
        {
            HavePlots(4);
            List<KeyValuePair<string, Rect>> before = RimPieces();
            runner.State.Levels[4] = 0;
            runner.State.Plots = 6;
            screen.Refresh();
            screen.Advance(0.15f);
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            bool fading = false;
            for (int k = 0; k < canopy.childCount; k++)
            {
                Image img = canopy.GetChild(k).GetComponent<Image>();
                if (img != null && canopy.GetChild(k).name.StartsWith("Rim ") && img.color.a > 0.02f && img.color.a < 0.98f) fading = true;
            }
            Assert.IsTrue(fading, "the pieces that step back fade out and in again");
            for (int i = 0; i < 20; i++) screen.Advance(0.1f);
            List<KeyValuePair<string, Rect>> after = RimPieces();
            Assert.AreEqual(before.Count, after.Count, "the same pieces, further out");
            int moved = 0;
            for (int k = 0; k < before.Count; k++)
            {
                Rect a = before[k].Value, b = after[k].Value;
                Assert.AreEqual(a.y, b.y, 0.01f, "a piece only steps sideways");
                bool left = a.xMax < GardenScreen.CellToWorld(new Hex(0, 0)).x;
                if (left) Assert.LessOrEqual(b.x, a.x + 0.01f, "the rim on the left only steps back"); else Assert.GreaterOrEqual(b.x, a.x - 0.01f, "the rim on the right only steps back");
                if (Mathf.Abs(b.x - a.x) > 1f)
                {
                    moved++;
                    bool newThere = false;
                    for (int i = 4; i < 6; i++) if ((screen.StationSpot(i).x < GardenScreen.CellToWorld(new Hex(0, 0)).x) == left) newThere = true;
                    Assert.IsTrue(newThere, before[k].Key + " at " + a + " moved, on the side with no new plot");
                }
            }
            Assert.GreaterOrEqual(moved, 2, "the rim beside the two new plots stepped back");
            foreach (Transform child in canopy)
                if (child.name.StartsWith("Rim ") && child.name != "Rim Shade" && child.gameObject.activeSelf)
                    Assert.AreEqual(1f, child.GetComponent<Image>().color.a, 1e-3f, child.name + " back in full");
        }

        [Test]
        public void Under_Reduced_Motion_A_New_Wave_Puts_The_Rim_And_The_Grass_In_Place_At_Once()
        {
            Motion.SetReduced(true);
            HavePlots(4);
            List<KeyValuePair<string, Rect>> before = RimPieces();
            runner.State.Plots = 6;
            screen.Refresh();
            List<KeyValuePair<string, Rect>> now = RimPieces();
            for (int i = 0; i < 20; i++) screen.Advance(0.1f);
            List<KeyValuePair<string, Rect>> settled = RimPieces();
            Assert.AreEqual(settled.Count, now.Count);
            bool changed = false;
            for (int k = 0; k < now.Count; k++)
            {
                Assert.AreEqual(settled[k].Value.x, now[k].Value.x, 0.01f, now[k].Key + " is already where it ends up");
                if (k < before.Count && Mathf.Abs(before[k].Value.x - now[k].Value.x) > 1f) changed = true;
            }
            Assert.IsTrue(changed, "the rim did step back");
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            foreach (Transform child in canopy)
                if (child.name.StartsWith("Rim ") && child.name != "Rim Shade" && child.gameObject.activeSelf)
                    Assert.AreEqual(1f, child.GetComponent<Image>().color.a, 1e-3f, child.name + " in full at once");
            Transform world = root.transform.Find("Canvas/Garden/Content");
            screen.Refresh();
            foreach (int i in new[] { 4, 5 })
            {
                Assert.IsFalse(world.Find("Ring " + i).gameObject.activeSelf);
                Transform bloom = world.Find("Bloom " + i);
                Assert.IsTrue(bloom.gameObject.activeSelf);
                Assert.AreEqual(1f, bloom.localScale.x, 1e-3f, "the grass is simply there");
                Assert.AreEqual(1f, bloom.GetComponent<CanvasGroup>().alpha, 1e-3f);
            }
        }

        [Test]
        public void The_View_Reaches_The_Outermost_Open_Plot_After_Every_Wave()
        {
            screen.Shell.StartGame();
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            for (int plots = Plots.Starting; plots <= runner.Defs.Count; plots += 2)
            {
                HavePlots(plots);
                foreach (GardenScreen.ZoomLevel zoom in new[] { GardenScreen.ZoomLevel.Default, GardenScreen.ZoomLevel.Near, GardenScreen.ZoomLevel.Far })
                {
                    screen.SetZoom(zoom, false);
                    for (int i = 0; i < plots; i++)
                    {
                        screen.LookAt(screen.StationSpot(i));
                        Vector2 at = screen.StationSpot(i) * world.localScale.x + world.anchoredPosition;
                        Assert.IsTrue(at.x > 0f && at.x < 1080f && at.y > 340f && at.y < 1920f - 160f,
                            "plot " + i + " of " + plots + " comes into view at " + zoom + ": " + at);
                    }
                }
            }
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
            Find("Canvas/Garden").onClick.Invoke();
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

            foreach (int index in new[] { 11, 9, 0 })
            {
                Find("Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[index].Id).onClick.Invoke();
                for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
                float y = screen.StationSpot(index).y * world.localScale.y + world.anchoredPosition.y;
                Assert.Greater(y, screen.SheetHeight + 60f, "station " + index + " stands above the sheet");
                Assert.Less(y, 1920f - 160f, "station " + index + " stands under the HUD");
            }

            screen.CloseSheet();
            for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
            Assert.That(world.anchoredPosition.y, Is.InRange(1920f - world.sizeDelta.y * world.localScale.y - 0.5f, 0.5f), "closed again, the view stays inside the garden");
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
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
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
            Vector2 Pond() => GardenScreen.PondSpot * world.localScale.y + world.anchoredPosition;
            Vector2 Pot() => screen.StationSpot(0) * world.localScale.y + world.anchoredPosition;
            Assert.That(Pond().y, Is.InRange(300f, 1700f), "behind the title the pond is in view");
            Assert.That(Pond().x, Is.InRange(0f, 1080f));

            screen.Shell.StartGame();
            for (int i = 0; i < 15; i++) screen.Advance(1f / 30f);
            Vector2 midway = world.anchoredPosition;
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            Assert.AreNotEqual(midway, world.anchoredPosition, "half a second in it is still travelling");
            Assert.AreEqual(540f, Pot().x, 1f, "after about a second the teapot stands in the middle");
            Assert.That(Pot().y, Is.InRange(340f, 1920f - 160f), "clear of the HUD and the tab bar");
        }

        [Test]
        public void Looking_Somewhere_Else_Takes_Over_From_The_Opening_Glide()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.Shell.StartGame();
            for (int i = 0; i < 6; i++) screen.Advance(1f / 30f);
            screen.LookAt(screen.StationSpot(2));
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

        sealed class FakeNotifier : INotifier
        {
            public bool Grant = true;
            public int Asked, Cancelled;
            public List<Notice> Scheduled = new List<Notice>();
            public void RequestPermission(System.Action<bool> done) { Asked++; done(Grant); }
            public void Schedule(IReadOnlyList<Notice> notices) => Scheduled.AddRange(notices);
            public void CancelAll() { Cancelled++; Scheduled.Clear(); }
        }

        void AppPause(bool paused) =>
            typeof(GameRunner).GetMethod("OnApplicationPause", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Invoke(runner, new object[] { paused });

        [Test]
        public void Remind_Me_Is_Off_At_First_And_Asks_Permission_Only_When_Turned_On()
        {
            var fake = new FakeNotifier();
            Notifications.Notifier = fake;
            Notifications.SetOn(false);
            fake.Asked = 0;
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            Assert.AreEqual("Nhắc tôi", root.transform.Find("Canvas/Pause/Card/Remind Label").GetComponent<Text>().text);
            Text pill = root.transform.Find("Canvas/Pause/Card/Remind/Text").GetComponent<Text>();
            Assert.AreEqual("Tắt", pill.text, "off until the player turns it on");
            Assert.AreEqual(0, fake.Asked, "opening the game and the card asks nothing");

            Find("Canvas/Pause/Card/Remind").onClick.Invoke();
            Assert.IsTrue(Notifications.On);
            Assert.AreEqual(1, fake.Asked, "turning it on asks once");
            Assert.AreEqual("Bật", pill.text);
            Assert.AreEqual(1, PlayerPrefs.GetInt("remind.on", 0), "kept for the next launch");

            Find("Canvas/Pause/Card/Remind").onClick.Invoke();
            Assert.IsFalse(Notifications.On);
            Assert.AreEqual(1, fake.Asked, "turning it off asks nothing");
            Assert.AreEqual("Tắt", pill.text);
        }

        [Test]
        public void Remind_Me_Goes_Back_Off_When_Permission_Is_Refused()
        {
            var fake = new FakeNotifier { Grant = false };
            Notifications.Notifier = fake;
            Notifications.SetOn(false);
            screen.Shell.StartGame();
            screen.Shell.OpenPause();
            Find("Canvas/Pause/Card/Remind").onClick.Invoke();
            Assert.AreEqual(1, fake.Asked);
            Assert.IsFalse(Notifications.On, "refused: the switch is off again");
            Assert.AreEqual("Tắt", root.transform.Find("Canvas/Pause/Card/Remind/Text").GetComponent<Text>().text);
        }

        [Test]
        public void Going_To_The_Background_Registers_The_Reminders_In_The_Players_Language_And_Coming_Back_Cancels_Them()
        {
            var fake = new FakeNotifier();
            Notifications.Notifier = fake;
            Notifications.SetOn(true);
            runner.State.LastSeenUnixSeconds = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

            AppPause(true);
            Assert.That(fake.Scheduled.Count, Is.InRange(2, Reminders.MaxCount), "the list is handed over");
            var bodies = new List<string>();
            foreach (Notice n in fake.Scheduled)
            {
                Assert.AreEqual("Vườn Trà Thỏ Trăng", n.Title);
                bodies.Add(n.Body);
            }
            CollectionAssert.Contains(bodies, L10n.T("remind.gift"));
            CollectionAssert.Contains(bodies, L10n.T("remind.missyou"));

            int cancelled = fake.Cancelled;
            AppPause(false);
            Assert.AreEqual(cancelled + 1, fake.Cancelled, "back in front: all taken away");
            Assert.IsEmpty(fake.Scheduled);

            L10n.SetLocale("en");
            AppPause(true);
            Assert.AreEqual("Moon Rabbit Tea Garden", fake.Scheduled[0].Title);
            bodies.Clear();
            foreach (Notice n in fake.Scheduled) bodies.Add(n.Body);
            CollectionAssert.Contains(bodies, "We haven't seen you for a while. The rabbits miss you.");

            Notifications.SetOn(false);
            AppPause(true);
            Assert.IsEmpty(fake.Scheduled, "switched off: nothing is registered");
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
        public void The_World_Is_A_Map_That_Scrolls_Both_Ways_And_Stations_Do_Not_Overlap()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            float visible = 1080f / GardenScreen.ZoomDefault, tall = 1920f / GardenScreen.ZoomDefault;
            Assert.That(world.sizeDelta.x / visible, Is.InRange(3f, 4f), "three to four screens wide at the usual zoom");
            Assert.That(world.sizeDelta.y / tall, Is.InRange(2f, 2.5f), "two to two and a half screens tall (#91: the bands up and down)");
            var scroll = world.parent.GetComponent<UnityEngine.UI.ScrollRect>();
            Assert.IsTrue(scroll.horizontal);
            for (int a = 0; a < runner.Defs.Count; a++)
            {
                Vector2 p = screen.StationSpot(a);
                Assert.IsTrue(p.x > 200 && p.x < world.sizeDelta.x - 200 && p.y > 200 && p.y < world.sizeDelta.y - 200, "station " + a + " is inside the world");
                for (int b = a + 1; b < runner.Defs.Count; b++)
                    Assert.Greater(Vector2.Distance(p, screen.StationSpot(b)), 280f, a + " and " + b);
            }
        }

        [Test]
        public void Zooming_Out_Shows_The_Whole_Garden_And_Hides_The_Small_Print()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var plate = world.Find("Station am-tra Front/Level").gameObject;
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Assert.IsTrue(plate.activeSelf);

            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Assert.AreEqual(GardenScreen.ZoomLevel.Far, screen.Zoom);
            Assert.AreEqual(1920f, 2970f * world.localScale.y, 0.5f, "far spans 2970 garden units top to bottom");
            Assert.Less(1620f * world.localScale.y, 1920f - 160f - 196f, "the middle's rows between the HUD and the tab bar");
            Assert.IsFalse(plate.activeSelf, "no level plates from far away");
            Assert.That(world.anchoredPosition.y, Is.InRange(1920f - world.sizeDelta.y * world.localScale.y - 0.5f, 0.5f), "the view inside the garden");

            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            Assert.IsTrue(plate.activeSelf);
            Assert.AreEqual(GardenScreen.ZoomDefault, world.localScale.x, 1e-4f);
        }

        [Test]
        public void Far_Shows_The_Whole_Strip_Top_To_Bottom_Clear_Of_The_HUD_And_The_Tab_Bar()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Assert.AreEqual(GardenScreen.ZoomFar, screen.ZoomScale, 1e-4f);

            GameState s = runner.State;
            s.Plots = runner.Defs.Count;
            GardenGrid.Settle(runner.Defs, s);
            screen.Refresh();
            screen.SetZoom(GardenScreen.ZoomLevel.Default, false);
            screen.SetZoom(GardenScreen.ZoomLevel.Far, false);
            Assert.AreEqual(GardenScreen.ZoomLevel.Far, screen.Zoom);
            Assert.AreEqual(GardenScreen.ZoomFar, screen.ZoomScale, 1e-4f, "the same far for every garden");
            Assert.LessOrEqual(1620f * world.localScale.y, 1920f - 160f - 196f, "the middle's rows fit between the HUD and the tab bar (#91: the map is taller)");
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                screen.LookAt(screen.StationSpot(i));
                Vector2 at = screen.StationSpot(i) * world.localScale.x + world.anchoredPosition;
                Assert.That(at.y, Is.InRange(196f, 1920f - 160f), "station " + i + " is clear of the HUD and the tab bar from far away");
                Assert.That(at.x, Is.InRange(0f, 1080f), "station " + i + " can be brought into view from far away");
            }
            Assert.AreEqual(screen.ZoomScale, GardenScreen.PinchZoom(screen.ZoomScale, -0.5f, screen.FarZoom), 1e-5f, "a pinch closes to far, no further");
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

        static Rect GardenRect(RectTransform rt, RectTransform space)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 a = space.InverseTransformPoint(corners[0]), b = space.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        static float Clearance(Rect a, Rect b)
        {
            float dx = Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax), dy = Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax);
            return Mathf.Max(dx, dy);
        }

        [Test]
        public void The_Usual_Zoom_Stands_Back_So_Three_Station_Columns_Fit_And_Far_Is_A_Step_Beyond()
        {
            Assert.That(GardenScreen.ZoomDefault, Is.InRange(0.80f, 0.85f), "the usual view is pulled back 15 to 20 percent");
            Assert.AreEqual(1.5f, GardenScreen.ZoomNear, 1e-5f, "the nearest zoom stays as it was, so taps stay easy");
            Assert.LessOrEqual(GardenScreen.ZoomFar, 1920f / 2970f + 1e-4f, "far away the whole garden still fits top to bottom");
            Assert.Less(GardenScreen.ZoomFar, GardenScreen.ZoomDefault * 0.8f, "and far is a real step beyond the usual view");
            float visible = 1080f / GardenScreen.ZoomDefault;
            Assert.GreaterOrEqual(visible, 1300f, "at the usual zoom the screen spans 1300 garden units");
            float left = screen.StationSpot(5).x - 220f * GardenScreen.StationSize(5), right = screen.StationSpot(1).x + 220f * GardenScreen.StationSize(1);
            Assert.Less(right - left, visible, "the widest row of three fits at once");
            float lowest = float.MaxValue, highest = float.MinValue;
            for (int i = 0; i < Plots.Starting; i++)
            {
                lowest = Mathf.Min(lowest, screen.StationSpot(i).x - 220f * GardenScreen.StationSize(i));
                highest = Mathf.Max(highest, screen.StationSpot(i).x + 220f * GardenScreen.StationSize(i));
            }
            Assert.Less(highest - lowest, visible, "the four stations of a new garden fit across the screen");

            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(GardenScreen.ZoomDefault, screen.ZoomScale, 1e-4f, "a new player opens at the usual zoom");
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom);
            Assert.AreEqual(540f, screen.StationSpot(0).x * world.localScale.x + world.anchoredPosition.x, 0.5f, "the teapot in the middle of the screen");
            Assert.IsTrue(world.parent.GetComponent<ScrollRect>().horizontal, "the strip is dragged sideways");
        }

        [Test]
        public void The_Opening_View_Shows_At_Least_Five_Plots_Around_The_Teapot()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            int shown = 0;
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Vector2 at = screen.StationSpot(i) * world.localScale.x + world.anchoredPosition;
                if (at.x > 0f && at.x < 1080f && at.y > 340f && at.y < 1920f - 160f) shown++;
            }
            Assert.GreaterOrEqual(shown, 5, "five or more plots are in view after opening, the teapot among them");
            Vector2 pot = screen.StationSpot(0) * world.localScale.x + world.anchoredPosition;
            Assert.AreEqual(540f, pot.x, 1f, "the teapot in the middle");
            Assert.That(pot.y, Is.InRange(340f, 1920f - 160f), "and clear of the HUD and the tray (#91: the map is taller than the screen, so the strip no longer sits in the middle)");
        }

        [Test]
        public void No_Big_Tree_Stands_On_The_Lawn()
        {
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            int trees = 0;
            foreach (RectTransform rt in street.GetComponentsInChildren<RectTransform>(true))
            {
                if (rt.name != "Tree") continue;
                trees++;
                Assert.LessOrEqual(rt.rect.height, 120f, "only the small tea tree by the leaf basket stands on the lawn, at " + rt.anchoredPosition);
            }
            Assert.LessOrEqual(trees, 1, "nothing grows on the land not yet opened either (#51)");
        }

        [Test]
        public void The_Canopy_Keeps_Two_Hundred_Units_From_Every_Station_And_From_The_Props_That_Must_Be_Read()
        {
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Canopy");
            var wood = new System.Collections.Generic.List<Rect>();
            for (int k = 0; k < canopy.childCount; k++)
            {
                var rt = (RectTransform)canopy.GetChild(k);
                if (rt.name == "Tree" || rt.name == "Bush") wood.Add(GardenRect(rt, canopy));
            }
            Assert.Greater(wood.Count, 20, "the wood still rings the garden");
            var read = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, Rect>>();
            foreach (string[] parts in new[] { new[] { "Level" }, new[] { "Pad" }, new[] { "Pad", "Tag" }, new[] { "Floor" }, new[] { "Prop" }, new[] { "Rabbit" }, new[] { "Tier" } })
                for (int i = 0; i < runner.Defs.Count; i++)
                {
                    Transform t = street.Find("Station " + runner.Defs[i].Id + (parts[0] == "Level" || parts[0] == "Pad" ? " Front" : ""));
                    foreach (string part in parts) t = t.Find(part);
                    Assert.IsNotNull(t, runner.Defs[i].Id + " has " + string.Join("/", parts));
                    read.Add(new System.Collections.Generic.KeyValuePair<string, Rect>(runner.Defs[i].Id + " " + string.Join("/", parts), GardenRect((RectTransform)t, street)));
                }
            foreach (string path in new[] { "Basket", "Basket/Plate", "Jar", "Jar/Plate", "Mortar", "Mortar/Plate" })
            {
                Transform t = street.Find(path);
                Assert.IsNotNull(t, path);
                read.Add(new System.Collections.Generic.KeyValuePair<string, Rect>(path, GardenRect((RectTransform)t, street)));
            }
            foreach (System.Collections.Generic.KeyValuePair<string, Rect> item in read)
            {
                float nearest = float.MaxValue;
                Rect near = default;
                foreach (Rect tree in wood)
                {
                    float c = Clearance(item.Value, tree);
                    if (c < nearest) { nearest = c; near = tree; }
                }
                Assert.GreaterOrEqual(nearest, 200f, item.Key + " at " + item.Value + " keeps 200 units from the wood; nearest tree " + near);
            }
        }

        [Test]
        public void The_Game_Opens_Where_The_Player_Last_Looked_Even_At_The_Old_Usual_Zoom()
        {
            runner.State.ViewX = 540d;
            runner.State.ViewY = 1500d;
            runner.State.ViewZoom = 1d;
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(1f, screen.ZoomScale, 1e-3f, "a remembered zoom is kept as it is");
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "and still counts as the usual view, so the zoom button goes on to Near");
        }

        [Test]
        public void A_Trackpad_Pinch_Zooms_By_Its_Magnification_And_Stops_At_Far_And_Near()
        {
            Assert.AreEqual(1f, GardenScreen.PinchZoom(1f, 0f), 1e-5f, "no fingers, no change");
            Assert.AreEqual(1f + 0.1f * GardenScreen.PinchGain, GardenScreen.PinchZoom(1f, 0.1f), 1e-5f, "spreading zooms in");
            Assert.AreEqual(1f - 0.1f * GardenScreen.PinchGain, GardenScreen.PinchZoom(1f, -0.1f), 1e-5f, "closing zooms out");
            Assert.AreEqual(GardenScreen.ZoomNear, GardenScreen.PinchZoom(GardenScreen.ZoomNear, 0.5f), 1e-5f, "no closer than Near");
            Assert.AreEqual(GardenScreen.ZoomFar, GardenScreen.PinchZoom(GardenScreen.ZoomFar, -0.5f), 1e-5f, "no further than Far");
            Assert.GreaterOrEqual(GardenScreen.PinchZoom(1f, -5f), GardenScreen.ZoomFar, "a wild negative sum never flips the garden");
            float zoom = GardenScreen.ZoomFar;
            for (int i = 0; i < 10; i++) zoom = GardenScreen.PinchZoom(zoom, 0.1f);
            Assert.AreEqual(GardenScreen.ZoomNear, zoom, 1e-5f);
        }

        [Test]
        public void The_Trackpad_Is_Silent_In_The_Editor()
        {
            Assert.IsFalse(TrackpadPinch.Available);
            TrackpadPinch.Start();
            Assert.IsFalse(TrackpadPinch.Read(out float magnification, out Vector2 at, out bool smart));
            Assert.AreEqual(0f, magnification);
            Assert.AreEqual(Vector2.zero, at);
            Assert.IsFalse(smart);
            TrackpadPinch.Stop();
        }

        [Test]
        public void A_Simulated_Pinch_Zooms_Toward_The_Point_Under_The_Fingers_And_Clamps()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            Vector2 at = new Vector2(540f, 960f);
            Vector2 under = (at - world.anchoredPosition) / screen.ZoomScale;
            float before = screen.ZoomScale;

            screen.SimulatePinch(0.1f, at);
            Assert.AreEqual(before * (1f + 0.1f * GardenScreen.PinchGain), screen.ZoomScale, 1e-4f);
            Assert.AreEqual(screen.ZoomScale, world.localScale.x, 1e-4f);
            Vector2 after = (at - world.anchoredPosition) / screen.ZoomScale;
            Assert.AreEqual(under.x, after.x, 0.5f, "the point under the fingers stays put");
            Assert.AreEqual(under.y, after.y, 0.5f);

            for (int i = 0; i < 20; i++) screen.SimulatePinch(0.3f, at);
            Assert.AreEqual(GardenScreen.ZoomNear, screen.ZoomScale, 1e-4f, "spreading on and on stops at Near");
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            float w = world.sizeDelta.x * world.localScale.x, h = world.sizeDelta.y * world.localScale.y;
            Assert.LessOrEqual(world.anchoredPosition.x, 0.5f);
            Assert.GreaterOrEqual(world.anchoredPosition.x, 1080f - w - 0.5f);
            Assert.LessOrEqual(world.anchoredPosition.y, 0.5f);
            Assert.GreaterOrEqual(world.anchoredPosition.y, 1920f - h - 0.5f);

            for (int i = 0; i < 20; i++) screen.SimulatePinch(-0.3f, at);
            Assert.AreEqual(GardenScreen.ZoomFar, screen.ZoomScale, 1e-4f, "closing on and on stops at Far");
            Assert.AreEqual(GardenScreen.ZoomLevel.Far, screen.Zoom);
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
                screen.Advance(1f);
                float feet = (screen.StationSpot(i).y - 150f) * world.localScale.y + world.anchoredPosition.y;
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
            StringAssert.StartsWith("Rabbit ", content.Find("Row 0").GetChild(0).name);
            long firstId = long.Parse(content.Find("Row 0").GetChild(0).name.Substring(7));
            Assert.AreEqual(2, Rabbits.Find(runner.State, firstId).Tier);
            StringAssert.Contains("3/" + AlbumSets.EveryDayKinds, content.Find("Count/Text").GetComponent<Text>().text, "out of the everyday kinds the album milestones count (#92)");

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
                string unmet = kind.NewMoonOnly ? L10n.T("album.newmoon.only") : kind.Season != Season.None ? L10n.T("album.season.only", L10n.Season(kind.Season)) : "???";
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
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 2);
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
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
            Assert.IsTrue(rabbit.gameObject.activeSelf);
            Assert.AreEqual("kind_tho-sao", rabbit.GetComponent<Image>().sprite.name, "the collected rabbit itself");
            Assert.IsTrue(rabbit.Find("HatOn").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find("Canvas/Garden/Content/Station am-tra/Halo").gameObject.activeSelf, "gold and red rabbits get a warm light");
            for (int i = 0; i < 40; i++) screen.Advance(0.1f);
            Assert.AreEqual("kind_tho-sao", rabbit.GetComponent<Image>().sprite.name, "the station's work frames do not take over");

            runner.ToggleRabbitWork(1);
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
            screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            ground.onClick.Invoke();
            ground.onClick.Invoke();
            screen.Advance(1f);
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "and again comes back out");
        }

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
                screen.Advance(0.05f);
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
            Assert.AreEqual(world.sizeDelta.x / 2f, screen.StationSpot(0).x, 0.5f, "teapot in the middle");
            float teapot = world.Find("Station am-tra").localScale.x;
            for (int i = 1; i < runner.Defs.Count; i++)
                Assert.Greater(teapot, world.Find("Station " + runner.Defs[i].Id).localScale.x * 1.3f, "teapot is clearly the largest");

            for (int i = 0; i < runner.Defs.Count; i++)
            {
                if (runner.Defs[i].Kind != VuonTra.Core.StationKind.Ingredient) continue;
                float nearest = float.MaxValue;
                for (int j = 0; j < runner.Defs.Count; j++)
                    if (runner.Defs[i].Boosts(runner.Defs[j].Id))
                    {
                        float apart = Vector2.Distance(screen.StationSpot(i), screen.StationSpot(j));
                        nearest = Mathf.Min(nearest, apart);
                        if (i < 12) Assert.Less(apart, 520f, runner.Defs[i].Id + " is near " + runner.Defs[j].Id);
                    }
                Assert.Less(nearest, 520f, runner.Defs[i].Id + " is near one station it supplies");
            }
        }

        [Test]
        public void Ingredients_Travel_To_The_Stations_That_Use_Them()
        {
            screen.Shell.StartGame();
            runner.State.Levels[1] = 3;
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
            screen.RebuildUi();

            Assert.AreEqual("Paused", root.transform.Find("Canvas/Pause/Card/Heading").GetComponent<Text>().text);
            Assert.AreEqual(ShellState.Paused, screen.Shell.State, "back on the card with the language buttons");
            Assert.IsTrue(screen.SheetOpen);
            Assert.AreEqual(1, screen.Tab);
            Assert.AreEqual("Dew Teapot", root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Name").GetComponent<Text>().text);
            Assert.AreEqual("Lv 7", root.transform.Find("Canvas/Garden/Content/Station am-tra Front/Level/Text").GetComponent<Text>().text);
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
            GameObject arrow = root.transform.Find("Canvas/Garden/Content/Station am-tra Front/CanUp").gameObject;
            screen.Refresh();
            Assert.IsFalse(arrow.activeSelf, "broke");
            runner.State.Coins = new BigNum(50);
            screen.Refresh();
            Assert.IsTrue(arrow.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station gio-la Front/CanUp").gameObject.activeSelf, "120 to open, only 50 in hand");
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
            for (int i = 0; i < 30 * 20; i++) screen.Advance(1f / 30f);
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
            Item hat = Items.Grant(runner.State, runner.ItemDefs[0], 0);
            Item charm = Items.Grant(runner.State, runner.ItemDefs[3], 0);
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

        Transform Station(string id) => root.transform.Find("Canvas/Garden/Content/Station " + id);
        Transform Front(string id) => root.transform.Find("Canvas/Garden/Content/Station " + id + " Front");

        [Test]
        public void Nobody_Stands_At_A_Station_Until_One_Of_Your_Rabbits_Is_Put_There()
        {
            Transform teapot = Station("am-tra");
            Assert.IsFalse(teapot.Find("Rabbit").gameObject.activeSelf);
            Assert.IsTrue(Front("am-tra").Find("Pad").gameObject.activeSelf);
            Assert.IsFalse(teapot.Find("Ground").gameObject.activeSelf);
            Assert.AreEqual(Color.white, teapot.Find("Prop").GetComponent<Image>().color);
            Assert.AreEqual("Cấp 1", Front("am-tra").Find("Level/Text").GetComponent<Text>().text);

            Transform basket = Station("gio-la");
            Assert.IsFalse(basket.Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(Front("gio-la").Find("Pad").gameObject.activeSelf);
            Assert.IsTrue(basket.Find("Ground").gameObject.activeSelf);
            Assert.IsTrue(basket.Find("Prop").gameObject.activeSelf);
            Assert.Less(basket.Find("Prop").GetComponent<Image>().color.a, 0.6f, "only a shadow of what will stand here");
            Assert.IsFalse(basket.Find("Doze").gameObject.activeSelf, "no sleeping rabbit on empty land");
            Text sign = Front("gio-la").Find("Level/Text").GetComponent<Text>();
            Assert.AreEqual("Còn thiếu 120", sign.text, "short of carrots: the sign says how many");
            runner.State.Coins = new BigNum(1000);
            screen.Refresh();
            Assert.AreEqual("Xây · 120", sign.text);
            Assert.AreEqual(Palette.Carrot, Front("gio-la").Find("Level").GetComponent<Image>().color, "lit when it can be paid for");

            for (int i = 1; i < 4; i++) runner.State.Levels[i] = 1;
            screen.Refresh();
            Transform oven = Station("lo-banh");
            Assert.IsTrue(oven.gameObject.activeSelf, "the plot after the last one owned stands as land");
            Assert.IsFalse(oven.Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(Front("lo-banh").Find("Pad").gameObject.activeSelf);
            Assert.IsFalse(oven.Find("Prop").gameObject.activeSelf);
            Assert.AreEqual("Cần cấp vườn 3", Front("lo-banh").Find("Level/Text").GetComponent<Text>().text);

            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
            Assert.IsTrue(teapot.Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(Front("am-tra").Find("Pad").gameObject.activeSelf);
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
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 1, Kind = "tho-cup", Name = "Cụp" });
            runner.State.Rabbits.Add(new Rabbit { Id = 2, Tier = 0, Kind = "tho-bong", Name = "Bông" });
            runner.State.NextRabbitId = 2;
            screen.Refresh();

            Transform picker = root.transform.Find("Canvas/WorkerPicker");
            Assert.IsFalse(picker.gameObject.activeSelf);
            Find("Canvas/Garden/Content/Station am-tra Front/Pad").onClick.Invoke();
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
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
            Assert.IsTrue(Station("am-tra").Find("Rabbit").gameObject.activeSelf);
            Assert.IsFalse(Front("am-tra").Find("Pad").gameObject.activeSelf);
        }

        [Test]
        public void The_Pad_Says_So_When_You_Have_No_Rabbits_Yet()
        {
            Find("Canvas/Garden/Content/Station am-tra Front/Pad").onClick.Invoke();
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
            for (int i = 0; i < 7; i++) dip.Step(0.1f);
            int afterOne = runner.State.Levels[0];
            Assert.Greater(afterOne, 1, "held long enough: it buys");
            for (int i = 0; i < 10; i++) dip.Step(0.1f);
            int afterTwo = runner.State.Levels[0];
            Assert.Greater(afterTwo - afterOne, afterOne - 1, "and buys quicker the longer it is held");

            dip.Release();
            dip.Click();
            Assert.AreEqual(afterTwo, runner.State.Levels[0], "the release is not one more purchase");
            dip.Click();
            Assert.AreEqual(afterTwo + 1, runner.State.Levels[0], "a plain tap still buys one");
            for (int i = 0; i < 12; i++) dip.Step(1f / 30f);
            Assert.IsFalse(dip.Busy);
        }

        [Test]
        public void Finished_Quests_Rise_To_The_Top_Under_One_Claim_All_Button()
        {
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            Transform ready = content.Find("Ready");
            Assert.IsFalse(ready.gameObject.activeSelf, "nothing to collect yet");
            Assert.AreEqual("QuestBoxRow", content.GetChild(0).name, "the day's box and the week's chests stand first (#103)");
            Assert.AreEqual("QuestWeekRow", content.GetChild(1).name);
            Assert.AreEqual(2, ready.GetSiblingIndex());

            runner.State.Coins = new BigNum(120);
            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            for (int i = 0; i < 50; i++) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Refresh();
            Assert.IsTrue(ready.gameObject.activeSelf);
            Assert.AreEqual("Ready", content.GetChild(2).name);
            Assert.AreEqual("Quest ngay-cham", content.GetChild(3).name, "today's finished quest");
            Assert.AreEqual("Quest mo-2", content.GetChild(4).name, "and the finished achievement, both above the headings");
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
            Text chip = root.transform.Find("Canvas/Garden/Content/Station am-tra Front/Level/Text").GetComponent<Text>();
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
            runner.State.GardenXp = Garden.XpForLevel(UpgradePlan.UnlockLevel);
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
        public void Upgrade_All_Before_Its_Garden_Level_Says_When_It_Opens_And_Buys_Nothing()
        {
            runner.State.Coins = new BigNum(1e9);
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Scroll/Content/UpgradeAll/Button").onClick.Invoke();
            Transform overlay = root.transform.Find("Canvas/UpgradeAll");
            Assert.IsTrue(overlay.gameObject.activeSelf);
            Text note = overlay.Find("Card/Scroll/Content/Empty").GetComponent<Text>();
            Assert.AreEqual(L10n.T("upgradeall.locked", UpgradePlan.UnlockLevel), note.text);
            StringAssert.Contains(UpgradePlan.UnlockLevel.ToString(), note.text);
            Assert.IsFalse(overlay.Find("Card/Confirm").GetComponent<Button>().interactable);
            Assert.AreEqual(1, runner.State.Levels[0]);
        }

        [Test]
        public void Upgrade_All_With_Nothing_Affordable_Offers_Nothing()
        {
            runner.State.GardenXp = Garden.XpForLevel(UpgradePlan.UnlockLevel);
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
        public void A_Sweep_Across_The_Tray_Sells_Everything_And_A_Golden_Batch_Pays_A_Golden_Carrot()
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
            Tray.Add(runner.State, 0, false);
            screen.OnProductMade(0);
            Assert.Greater(screen.Feel.Flying, 0);
            Assert.AreEqual("×2", root.transform.Find("Canvas/Tray/Slot 0/Count/Text").GetComponent<Text>().text);
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(0, screen.Feel.Flying);
            Assert.AreEqual(Vector3.one, root.transform.Find("Canvas/Tray/Slot 0").localScale, "the slot popped and settled");
        }

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
        public void A_Tiered_Achievement_Row_Moves_To_Its_Next_Tier_In_Place_And_Turns_Gold_When_Complete()
        {
            screen.Shell.StartGame();
            runner.State.OrdersDelivered = 7;
            screen.OpenSheet(2);
            screen.Refresh();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            Transform row = content.Find("Quest don");
            Text title = row.Find("Title").GetComponent<Text>();
            Assert.AreEqual("Đơn khách · nấc 1/8", title.text);
            Assert.IsTrue(row.Find("Claim").gameObject.activeSelf);
            int index = -1;
            for (int i = 0; i < runner.QuestDefs.Count; i++) if (runner.QuestDefs[i].Id == "don") index = i;

            Assert.IsTrue(runner.ClaimQuest(index, out _));
            screen.Refresh();
            Assert.AreSame(row, content.Find("Quest don"), "the same row");
            Assert.AreEqual("Đơn khách · nấc 2/8", title.text, "now on the next tier");
            Assert.AreEqual("7/20 đơn", row.Find("Count").GetComponent<Text>().text);
            Assert.IsFalse(row.Find("Claim").gameObject.activeSelf);
            Assert.IsFalse(row.Find("GoldEdge").gameObject.activeSelf);
            bool total = false;
            foreach (Transform child in content)
            {
                Text text = child.GetComponent<Text>();
                total |= text != null && text.text == "Tổng thành tựu: 1 nấc";
            }
            Assert.IsTrue(total, "the achievements open with the tiers reached in all");

            runner.State.OrdersDelivered = 5000;
            while (runner.ClaimQuest(index, out _)) { }
            screen.Refresh();
            Assert.AreEqual("Đơn khách · nấc 8/8", title.text);
            string last = new BigNum(2000).Short();
            Assert.AreEqual(last + "/" + last + " đơn", row.Find("Count").GetComponent<Text>().text);
            Assert.IsTrue(row.Find("GoldEdge").gameObject.activeSelf, "a finished chain wears a gold edge");
            Assert.IsTrue(row.Find("Done").gameObject.activeSelf);
            Assert.AreEqual("Hoàn tất", row.Find("Done/Text").GetComponent<Text>().text);
            Assert.IsFalse(row.Find("Carrots").gameObject.activeSelf, "nothing left to pay");
        }

        [Test]
        public void The_Three_Photo_Quests_Are_In_The_List_And_The_First_Finishes_With_A_Snap()
        {
            screen.Shell.StartGame();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Quests/Content");
            Assert.IsNotNull(content.Find("Quest anh"), "the photo achievement is a tiered row (#111)");
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
                Assert.IsTrue(content.Find("Quest anh/Claim").gameObject.activeSelf, "the first photo tier is ready to collect");
                Assert.IsFalse(content.Find("Quest anh-vang/Claim").gameObject.activeSelf);
            }
            finally
            {
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }
        }

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

            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.1"), text.text);
            Assert.AreEqual("Buy", screen.Guide.Target.name, "the card opened with the tap: the hand moves to its button");

            runner.State.Coins = new BigNum(1000);
            Find("Canvas/Card/Buy").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.2"), text.text);
            Assert.AreEqual("Icon", screen.Guide.Target.name, "the sheet is closed: the hand points at the Stations tab");
            screen.OpenSheet(0);
            screen.Advance(1f / 30f);
            Assert.AreEqual("Row gio-la", screen.Guide.Target.parent.name, "open: the hand points at the second station's button");

            Find("Canvas/Sheet/Panel/Scroll/Content/Row gio-la/Buy").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.3"), text.text);
            screen.OpenSheet(2);
            screen.Advance(1f / 30f);
            Assert.AreEqual("Quest mo-2", screen.Guide.Target.parent.name, "the finished quest's button");

            Find("Canvas/Sheet/Panel/Quests/Content/Quest mo-2/Claim").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.4"), text.text);
            Assert.AreEqual(1, runner.State.Clovers, "the call step brought the clover it needs");
            Assert.IsTrue(runner.State.TutorialCloverGiven);
            screen.OpenSheet(3);
            screen.Advance(1f / 30f);
            Assert.AreEqual("Button", screen.Guide.Target.name);
            Assert.AreEqual("Summon", screen.Guide.Target.parent.name);

            Find("Canvas/Sheet/Panel/Rabbits/Content/Summon/Button").onClick.Invoke();
            screen.Arrival.Close();
            screen.Advance(1f / 30f);
            Assert.AreEqual(L10n.T("tut.5"), text.text);
            Assert.AreEqual("Pad", screen.Guide.Target.name, "the empty pad of the open station");
            Assert.AreEqual("Station gio-la Front", screen.Guide.Target.parent.name);

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

            for (int i = 0; i < 3; i++) runner.State.Rabbits.Add(new Rabbit { Id = 100 + i, Tier = 0, Kind = "tho-bong", Name = "S" + i });
            runner.State.NextRabbitId = 102;
            screen.Refresh();
            Find("Canvas/Sheet/Panel/Rabbits/Content/Merge 0/Button").onClick.Invoke();
            Assert.AreEqual("summon-1", Sfx.Last, "a merge arrives to the bells of the tier it made");
            screen.Arrival.Close();

            GameState again = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, runner.State), runner.UpgradeDefs).State;
            Assert.IsTrue(again.FreshKinds.Contains(called.Kind));
            runner.LookAtKind(called.Kind);
            Assert.IsFalse(screen.IsFreshKind(called.Kind));
            GameState after = SaveCodec.Decode(runner.Defs, SaveCodec.Encode(runner.Defs, runner.State), runner.UpgradeDefs).State;
            Assert.IsFalse(after.FreshKinds.Contains(called.Kind));
        }

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
        public void The_Shop_Shows_Seven_Gift_Days_Ahead_A_Missed_Day_Steps_Back_One_And_Day_Seven_Brings_The_Hat_And_A_Clover()
        {
            screen.Shell.StartGame();
            screen.OpenShop();
            screen.Refresh();
            Transform days = root.transform.Find("Canvas/Shop/Card/Today/Days");
            Assert.IsNotNull(days, "the calendar sits under today's gift");
            Assert.AreEqual(Shop.WeekDays, days.childCount);
            Transform d1 = days.Find("Day 1"), d4 = days.Find("Day 4"), d5 = days.Find("Day 5"), d7 = days.Find("Day 7");
            Assert.AreEqual("Ngày 1", d1.Find("Day").GetComponent<Text>().text);
            Assert.AreEqual("+1", d1.Find("Amount").GetComponent<Text>().text);
            Assert.IsTrue(d1.Find("Ring").gameObject.activeSelf, "today's tile is ringed");
            Assert.IsFalse(d1.Find("Tick").gameObject.activeSelf);
            Assert.AreEqual("+5", d7.Find("Amount").GetComponent<Text>().text);
            Assert.IsTrue(d7.Find("Hat").gameObject.activeSelf, "day seven shows the hat");
            Assert.AreEqual(runner.Catalog.ItemIcon(Shop.BigGiftHat), d7.Find("Hat").GetComponent<Image>().sprite);
            Assert.IsNotNull(d7.Find("Hat").GetComponent<Image>().sprite);
            Assert.IsTrue(d7.Find("Clover").gameObject.activeSelf, "and the clover");
            Assert.AreEqual(runner.Catalog.Clover, d7.Find("Clover").GetComponent<Image>().sprite);
            Assert.IsFalse(d4.Find("Hat").gameObject.activeSelf, "an ordinary day shows golden carrots only");

            long today = runner.TodayNumber;
            runner.State.GiftDay = today - 1;
            runner.State.GiftStreak = 3;
            screen.Refresh();
            for (int i = 1; i <= 3; i++)
            {
                Transform d = days.Find("Day " + i);
                Assert.IsTrue(d.Find("Tick").gameObject.activeSelf, "day " + i + " was taken");
                Assert.IsFalse(d.Find("Ring").gameObject.activeSelf);
            }
            Assert.IsTrue(d4.Find("Ring").gameObject.activeSelf);
            Assert.IsFalse(d4.Find("Tick").gameObject.activeSelf);
            Assert.AreEqual(Palette.Ink, d4.Find("Amount").GetComponent<Text>().color);
            Assert.IsFalse(d5.Find("Ring").gameObject.activeSelf || d5.Find("Tick").gameObject.activeSelf);
            Assert.AreEqual(Palette.InkSoft, d5.Find("Amount").GetComponent<Text>().color, "faded, still readable");
            Assert.Less(d5.Find("Golden").GetComponent<Image>().color.a, 1f);
            Assert.AreEqual("+3", d5.Find("Amount").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("shop.gift.claim", 4), root.transform.Find("Canvas/Shop/Card/Today/Heading").GetComponent<Text>().text);

            runner.State.GiftDay = today - 2;
            runner.State.GiftStreak = 5;
            screen.Refresh();
            Assert.AreEqual(L10n.T("shop.gift.claim", 4), root.transform.Find("Canvas/Shop/Card/Today/Heading").GetComponent<Text>().text);
            Assert.IsTrue(d4.Find("Ring").gameObject.activeSelf);

            runner.State.GiftDay = today - 1;
            runner.State.GiftStreak = 6;
            screen.Refresh();
            Text sub = root.transform.Find("Canvas/Shop/Card/Today/Sub").GetComponent<Text>();
            Assert.AreEqual(L10n.T("shop.gift.big.sub", "Nón lá", 1), sub.text);
            int golden = runner.State.GoldenCarrots, clovers = runner.State.Clovers;
            Find("Canvas/Shop/Card/Today/Gift").onClick.Invoke();
            Assert.AreEqual(golden + 5, runner.State.GoldenCarrots);
            Assert.AreEqual(clovers + 1, runner.State.Clovers);
            Assert.IsTrue(Items.Owns(runner.State, "hat-non-la"), "the hat is given");
            Assert.IsTrue(runner.State.GiftBigClaimed);
            screen.Refresh();
            Assert.AreEqual(L10n.T("shop.gift.big.hat", "Nón lá", 1), sub.text, "and the shop says so");
            Assert.IsTrue(d7.Find("Tick").gameObject.activeSelf);
            Assert.IsTrue(d7.Find("Hat").gameObject.activeSelf, "today's big gift still shows what it was");
            Assert.IsFalse(Find("Canvas/Shop/Card/Today/Gift").interactable);
            Assert.AreEqual(0, runner.ClaimDailyGift(), "once");
            Assert.AreEqual(clovers + 1, runner.State.Clovers);
        }

        [Test]
        public void The_Big_Gift_Pays_The_Hats_Price_When_The_Hat_Is_Owned_And_Says_So()
        {
            screen.Shell.StartGame();
            runner.State.GoldenCarrots = 20;
            Assert.IsNotNull(runner.BuyItem("hat-non-la"));
            runner.State.GiftDay = runner.TodayNumber - 1;
            runner.State.GiftStreak = 6;
            screen.OpenShop();
            screen.Refresh();
            Find("Canvas/Shop/Card/Today/Gift").onClick.Invoke();
            Assert.AreEqual(5 + 20, runner.State.GoldenCarrots, "five, and twenty in the hat's place");
            int hats = 0;
            foreach (Item item in runner.State.Items) if (item.DefId == "hat-non-la") hats++;
            Assert.AreEqual(1, hats, "no second hat");
            screen.Refresh();
            Assert.AreEqual(L10n.T("shop.gift.big.golden", "Nón lá", 20, 1), root.transform.Find("Canvas/Shop/Card/Today/Sub").GetComponent<Text>().text);
        }

        [Test]
        public void The_Summon_Odds_Read_Without_A_Trailing_Zero_In_Both_Languages()
        {
            Assert.AreEqual("Bạc 60% · Xanh lá 25% · Xanh dương 10% · Tím 4% · Vàng 0,9% · Đỏ 0,1%", ShopView.Odds(Rabbits.Odds));
            Assert.AreEqual("Bạc 25% · Xanh lá 35% · Xanh dương 22% · Tím 12% · Vàng 5% · Đỏ 1%", ShopView.Odds(Shop.GoldenOdds));
            L10n.SetLocale("en");
            Assert.AreEqual("Silver 60% · Green 25% · Blue 10% · Purple 4% · Gold 0.9% · Red 0.1%", ShopView.Odds(Rabbits.Odds));
            Assert.AreEqual("Silver 25% · Green 35% · Blue 22% · Purple 12% · Gold 5% · Red 1%", ShopView.Odds(Shop.GoldenOdds));
            screen.RebuildUi();
            StringAssert.Contains("Silver 60% · Green 25%", root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Odds").GetComponent<Text>().text, "and so the Rabbits tab reads");
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
            StringAssert.Contains("10 phút", away.Find("Card/Carrots/Time").GetComponent<Text>().text);
            Find("Canvas/Away/Card/Ok").onClick.Invoke();
            for (int i = 0; i < 20; i++) screen.Advance(0.05f);
            Assert.IsFalse(away.gameObject.activeSelf);
        }

        [Test]
        public void Every_Daily_Thing_Goes_By_The_Day_That_Never_Goes_Back()
        {
            long today = runner.TodayNumber;
            runner.State.MaxDaySeen = today + 2;
            Assert.AreEqual(today + 2, runner.TodayNumber, "the game keeps going by the latest day it saw");
            runner.State.PhotoDay = today + 2;
            Assert.IsFalse(runner.PhotoGiftWaiting, "today's photo gift was already taken on that day");
            runner.State.VisitorSeenDay = today + 2;
            Assert.IsFalse(runner.VisitorUnseen);
        }

        [Test]
        public void The_Day_Seven_Big_Gift_Is_A_Moment_That_Names_The_Hat_And_The_Clover()
        {
            screen.Shell.StartGame();
            GameState s = runner.State;
            long today = runner.TodayNumber;
            s.GiftStreak = 6;
            s.GiftDay = today - 1;
            screen.Refresh();
            screen.OpenShop();
            screen.Refresh();
            Find("Canvas/Shop/Card/Today/Gift").onClick.Invoke();
            Assert.IsTrue(screen.Feel.BannerVisible, "the big gift is a moment");
            StringAssert.Contains(L10n.T("item." + Shop.BigGiftHat), root.transform.Find("Canvas/Feel/Banner/Sub").GetComponent<Text>().text);
        }

        [Test]
        public void The_Welcome_Back_Sheet_Tells_Four_Lines_And_Doubles_Once_Free_Then_For_Golden()
        {
            screen.Shell.StartGame();
            GameState s = runner.State;
            if (s.Rabbits.Count == 0) Rabbits.GiftFirst(s, "Bông");
            var away = new AwayReport { Paid = new BigNum(5000), Seconds = 3 * 3600, Capped = true, TrayAdded = 7, TrayFull = true, GuestsMissed = 3, SleepiestId = s.Rabbits[0].Id };
            BigNum before = s.Coins;
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Transform card = root.transform.Find("Canvas/Away/Card");
            Assert.IsTrue(screen.AwayCard.IsOpen);
            Assert.AreEqual("+" + new BigNum(5000).Short(), card.Find("Carrots/Amount").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("away.tray", 7) + L10n.T("away.tray.full"), card.Find("Tray/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("away.guests", 3), card.Find("Guests/Text").GetComponent<Text>().text);
            Assert.AreEqual(L10n.T("away.rabbit", s.Rabbits[0].Name), card.Find("Rabbit/Text").GetComponent<Text>().text);
            Assert.IsNotNull(card.Find("Rabbit/PortraitHolder/Tile"), "the sleepy rabbit's picture");
            Assert.AreEqual(L10n.T("away.double.free"), screen.AwayCard.DoubleLabel);

            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual((before + new BigNum(5000)).ToDouble(), s.Coins.ToDouble(), 1e-6, "paid once more");
            Assert.AreEqual(runner.TodayNumber, s.AwayDoubleDay);

            s.GoldenCarrots = 2;
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Assert.AreEqual(L10n.T("away.double.short", Away.DoubleCost - 2), screen.AwayCard.DoubleLabel);
            Assert.IsFalse(screen.AwayCard.DoubleEnabled);
            s.GoldenCarrots = 9;
            screen.AwayCard.RefreshDouble();
            Assert.AreEqual(L10n.T("away.double.cost", Away.DoubleCost), screen.AwayCard.DoubleLabel);
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual(9 - Away.DoubleCost, s.GoldenCarrots);
            Assert.IsFalse(runner.DoubleAway(), "one doubling per time away");
        }

        [Test]
        public void The_Welcome_Back_Sheet_Comes_Up_Over_The_Title_And_Its_Tap_To_Start_Line()
        {
            Assert.AreEqual(ShellState.Title, screen.Shell.State);
            var away = new AwayReport { Paid = new BigNum(5000), Seconds = 3 * 3600, Capped = true, GuestsMissed = 3 };
            runner.ShowAwayForTest(away);
            screen.ShowAway(away);
            Transform canvas = root.transform.Find("Canvas");
            Transform title = canvas.Find("Title"), sheet = canvas.Find("Away");
            Assert.IsTrue(title.gameObject.activeSelf && title.Find("Prompt").gameObject.activeSelf, "the title and its blinking line are still up");
            Assert.IsTrue(sheet.gameObject.activeSelf);
            Assert.Greater(sheet.GetSiblingIndex(), title.GetSiblingIndex(), "the sheet draws over the title, so \"tap to start\" is under it");

            Find("Canvas/Away/Card/Ok").onClick.Invoke();
            Assert.IsFalse(screen.AwayCard.IsOpen);
            Assert.AreEqual(ShellState.Title, screen.Shell.State);
        }

        [Test]
        public void Under_Five_Minutes_Away_There_Is_No_Sheet_Only_Carrots_To_The_Purse()
        {
            screen.Shell.StartGame();
            var brief = new AwayReport { Paid = new BigNum(300), Seconds = 120 };
            runner.ShowAwayForTest(brief);
            screen.ShowAway(brief);
            Assert.IsFalse(screen.AwayCard.IsOpen);
            Assert.IsFalse(root.transform.Find("Canvas/Away").gameObject.activeSelf);
        }

        AwayReport SheetUpThenAMinuteAway()
        {
            screen.Shell.StartGame();
            runner.State.MaxDaySeen = runner.TodayNumber + 1;
            var twoHours = new AwayReport { Paid = new BigNum(8000), Seconds = 2 * 3600 };
            runner.ShowAwayForTest(twoHours);
            screen.ShowAway(twoHours);
            Assert.IsTrue(screen.AwayCard.IsOpen);
            var minute = new AwayReport { Paid = new BigNum(40), Seconds = 60 };
            runner.ShowAwayForTest(minute);
            screen.ShowAway(minute);
            Assert.IsTrue(screen.AwayCard.IsOpen, "the minute away leaves the sheet up");
            Assert.AreEqual("+" + new BigNum(8000).Short(), root.transform.Find("Canvas/Away/Card/Carrots/Amount").GetComponent<Text>().text);
            return twoHours;
        }

        [Test]
        public void A_Minute_Away_While_The_Sheet_Is_Up_Leaves_Its_Free_Times_Two_On_The_Two_Hours_It_Shows()
        {
            AwayReport shown = SheetUpThenAMinuteAway();
            GameState s = runner.State;
            Assert.AreEqual(L10n.T("away.double.free"), screen.AwayCard.DoubleLabel, "the free ×2 is still there");
            BigNum before = s.Coins;
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual((before + shown.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6, "×2 pays the two hours the sheet shows, not the minute");
            Assert.AreEqual(runner.TodayNumber, s.AwayDoubleDay, "the day's free ×2 is used, once");
            Assert.IsFalse(screen.AwayCard.IsOpen);

            var again = new AwayReport { Paid = new BigNum(40), Seconds = 60 };
            runner.ShowAwayForTest(again);
            screen.ShowAway(again);
            Assert.IsTrue(runner.LastAwayDoubled, "a short absence does not give the ×2 back");
            Assert.IsFalse(runner.DoubleAway(), "one doubling per time away");
            Assert.AreEqual((before + shown.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6);
        }

        [Test]
        public void A_Minute_Away_While_The_Sheet_Is_Up_Leaves_Its_Golden_Times_Two_On_The_Two_Hours_It_Shows()
        {
            AwayReport shown = SheetUpThenAMinuteAway();
            GameState s = runner.State;
            s.AwayDoubleDay = runner.TodayNumber;
            s.GoldenCarrots = 9;
            screen.AwayCard.RefreshDouble();
            Assert.AreEqual(L10n.T("away.double.cost", Away.DoubleCost), screen.AwayCard.DoubleLabel);
            BigNum before = s.Coins;
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual((before + shown.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6, "five golden carrots buy the two hours again");
            Assert.AreEqual(9 - Away.DoubleCost, s.GoldenCarrots);
            Assert.IsFalse(runner.DoubleAway(), "one doubling per time away");
            Assert.AreEqual(9 - Away.DoubleCost, s.GoldenCarrots, "a refused ×2 costs nothing");
        }

        [Test]
        public void A_New_Long_Time_Away_After_The_Sheet_Closed_Brings_A_Fresh_Report_And_Times_Two()
        {
            SheetUpThenAMinuteAway();
            GameState s = runner.State;
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.IsFalse(screen.AwayCard.IsOpen);
            Assert.IsTrue(runner.LastAwayDoubled);

            s.GoldenCarrots = 9;
            var night = new AwayReport { Paid = new BigNum(12000), Seconds = 6 * 3600 };
            runner.ShowAwayForTest(night);
            screen.ShowAway(night);
            Assert.IsTrue(screen.AwayCard.IsOpen);
            Assert.AreEqual("+" + new BigNum(12000).Short(), root.transform.Find("Canvas/Away/Card/Carrots/Amount").GetComponent<Text>().text);
            Assert.IsFalse(runner.LastAwayDoubled, "a fresh report, a fresh ×2");
            Assert.AreEqual(L10n.T("away.double.cost", Away.DoubleCost), screen.AwayCard.DoubleLabel, "the day's free one stays spent");
            BigNum before = s.Coins;
            Find("Canvas/Away/Card/Double").onClick.Invoke();
            Assert.AreEqual((before + night.Paid).ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(9 - Away.DoubleCost, s.GoldenCarrots);
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
            Assert.IsFalse(root.transform.Find("Canvas/Garden/Content/Station am-tra Front/CanUp").gameObject.activeSelf, "no 'tap me' arrow at the cap");

            screen.CloseSheet();
            screen.FocusStation(0);
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Card/Buy").interactable);
            Assert.AreEqual(L10n.T("row.maxlevel"), T("Canvas/Card/Buy/Face/Top").text);
            Assert.AreEqual(L10n.StationLevel(Economy.MaxLevel), T("Canvas/Card/Level/Text").text);
        }

        [Test]
        public void The_Card_Button_Of_A_Station_At_Its_Cap_Shows_No_Carrot()
        {
            runner.State.Levels[0] = Economy.MaxLevel - 1;
            runner.State.Coins = new BigNum(1e300);
            screen.Shell.StartGame();
            screen.FocusStation(0);
            screen.Refresh();
            Transform carrot = root.transform.Find("Canvas/Card/Buy/Face/Carrot");
            Assert.IsNotNull(carrot);
            Assert.IsTrue(carrot.gameObject.activeSelf, "one level left: the button still costs carrots");

            runner.State.Levels[0] = Economy.MaxLevel;
            screen.Refresh();
            Assert.AreEqual(L10n.T("row.maxlevel"), root.transform.Find("Canvas/Card/Buy/Face/Top").GetComponent<Text>().text);
            Assert.IsFalse(carrot.gameObject.activeSelf, "at the cap nothing is paid, so no carrot either");

            runner.State.Levels[0] = Economy.MaxLevel - 1;
            screen.Refresh();
            Assert.IsTrue(carrot.gameObject.activeSelf, "and with it the carrot");
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
            runner.State.UpgradeLevels[0] = 1;
            runner.State.UpgradeLevels[3] = 5;
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

        void SeatTwoOfThree()
        {
            runner.State.Levels[0] = 5;
            runner.State.Levels[1] = 2;
            runner.State.Levels[2] = 3;
            runner.State.RestSpots = 3;
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = RabbitKinds.OfTier(0)[0].Id, Name = "Mây", Station = 0 });
            runner.State.Rabbits.Add(new Rabbit { Id = 2, Tier = 2, Kind = RabbitKinds.OfTier(2)[0].Id, Name = "Sương" });
            runner.State.Rabbits.Add(new Rabbit { Id = 3, Tier = 1, Kind = RabbitKinds.OfTier(1)[0].Id, Name = "Mè", Station = 2 });
            runner.State.Rabbits.Add(new Rabbit { Id = 4, Tier = 4, Kind = RabbitKinds.OfTier(4)[0].Id, Name = "Sao", Station = 1 });
            runner.State.NextRabbitId = 4;
            screen.Shell.StartGame();
            Assert.IsTrue(runner.AssignRabbit(3, -1), "the green one is sent to rest");
            screen.Refresh();
        }

        [Test]
        public void The_Purse_Card_Shows_What_The_Resting_Rabbits_Add_To_The_Whole_Garden()
        {
            SeatTwoOfThree();
            Text T(string path) { Transform t = root.transform.Find(path); Assert.IsNotNull(t, "Missing UI object: " + path); return t.GetComponent<Text>(); }
            Assert.AreEqual(1.055, runner.Mods.IncomeMultiplier, 1e-9, "blue +3 % and green +2,5 % are in the garden's modifiers");

            Find("Canvas/Hud/Purse").onClick.Invoke();
            string rows = "Canvas/Income/Card/Scroll/Content/";
            Assert.AreEqual("Thỏ đang nghỉ (2) +5,5%", T(rows + "Resting").text);
            Assert.AreEqual(L10n.T("income.charms", UiKit.Times(1d)), T(rows + "Charms").text, "the charms line keeps to the charms");
            BigNum total = Economy.IncomePerSecond(runner.Defs, runner.State) * (runner.Mods.IncomeMultiplier * runner.Mods.SpeedMultiplier);
            Assert.AreEqual(L10n.T("income.total", total.Short()), T(rows + "Total").text, "the total includes the share");
            Find("Canvas/Income/Card/Close").onClick.Invoke();

            runner.State.Levels[3] = 1;
            Assert.IsTrue(runner.AssignRabbit(3, 2));
            Assert.IsTrue(runner.AssignRabbit(2, 3));
            Assert.AreEqual(1d, runner.Mods.IncomeMultiplier, 1e-9);
            Find("Canvas/Hud/Purse").onClick.Invoke();
            Assert.IsNull(root.transform.Find(rows + "Resting"), "no resting line with nobody seated");
        }

        [Test]
        public void A_Seated_Rabbits_Sheet_And_Card_Say_What_It_Adds_And_A_Rabbit_Without_A_Seat_Does_Not()
        {
            SeatTwoOfThree();
            screen.Detail.Open(runner.Defs, runner.State, 2);
            Assert.AreEqual("Đang nghỉ · +3% vườn", root.transform.Find("Canvas/RabbitSheet/Card/Body/Where").GetComponent<Text>().text);
            screen.Detail.Open(runner.Defs, runner.State, 3);
            Assert.AreEqual("Đang nghỉ · +2,5% vườn", root.transform.Find("Canvas/RabbitSheet/Card/Body/Where").GetComponent<Text>().text);
            screen.Detail.Close();

            screen.OpenSheet(3);
            screen.Refresh();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content");
            Transform Cell(long id)
            {
                foreach (Transform row in content)
                {
                    Transform c = row.Find("Rabbit " + id);
                    if (c != null) return c;
                }
                Assert.Fail("no card for rabbit " + id);
                return null;
            }
            Assert.AreEqual("+3% vườn", Cell(2).Find("Status/Share").GetComponent<Text>().text, "the nap badge of a seated rabbit says its share");
            Assert.IsNull(Cell(1).Find("Status/Share"), "a working rabbit shows its station");
            StringAssert.Contains("+5,5%", content.Find("Rest/Text").GetComponent<Text>().text, "the places row adds them up");

            runner.State.RestSpots = 2;
            runner.AssignRabbit(4, -1);
            Assert.AreEqual(1d + 0.06 + 0.03, runner.Mods.IncomeMultiplier, 1e-9, "gold and blue sit; green has no place");
            screen.Refresh();
            Assert.IsNull(Cell(3).Find("Status/Share"));
            screen.Detail.Open(runner.Defs, runner.State, 3);
            Assert.AreEqual(L10n.T("detail.resting"), root.transform.Find("Canvas/RabbitSheet/Card/Body/Where").GetComponent<Text>().text);
        }

        [Test]
        public void A_Rabbit_Nodding_Off_In_Its_Seat_Still_Adds_Its_Share()
        {
            SeatTwoOfThree();
            double share = runner.Mods.IncomeMultiplier;
            Transform world = root.transform.Find("Canvas/Garden/Content");
            bool dozed = false;
            for (int i = 0; i < 400 && !dozed; i++)
            {
                screen.Advance(0.1f);
                for (int k = 0; k < 2; k++) dozed |= world.Find("Seat " + k + "/Nap").gameObject.activeInHierarchy;
            }
            Assert.IsTrue(dozed, "a seated rabbit dozes now and then");
            Assert.AreEqual(share, runner.Mods.IncomeMultiplier, 1e-12);
            Assert.AreEqual(share, Modifiers.From(runner.UpgradeDefs, runner.State).IncomeMultiplier, 1e-12);
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
            Motion.SetReduced(true);
            for (int i = 0; i < runner.State.Levels.Length; i++) runner.State.Levels[i] = 1 + i;
            runner.State.Plots = runner.State.Levels.Length;
            runner.State.Coins = new BigNum(1e300);
            screen.Shell.StartGame();
            screen.Refresh();
            GameObject up = root.transform.Find("Canvas/EdgeUp").gameObject, down = root.transform.Find("Canvas/EdgeDown").gameObject, home = root.transform.Find("Canvas/Home").gameObject;
            int Count(GameObject pill) => pill.activeSelf ? int.Parse(pill.transform.Find("Text").GetComponent<Text>().text.Substring(2)) : 0;

            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(GardenScreen.CellToWorld(new Hex(0, 0)) + new Vector2(0f, 600f));
            screen.Advance(1f / 30f);
            Assert.IsTrue(down.activeSelf, "stations that can be raised wait below");
            StringAssert.StartsWith("▼ ", down.transform.Find("Text").GetComponent<Text>().text);
            int waiting = Count(down);
            Assert.GreaterOrEqual(waiting, 2, "the stations of the bottom rows");
            Assert.IsFalse(home.activeSelf, "the teapot is still in view at this zoom");

            Find("Canvas/EdgeDown").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.Less(Count(down), waiting, "the touch flew to the nearest waiting station, which is now in view");

            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            screen.LookAt(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(15)) + new Vector2(0f, -200f));
            screen.Advance(1f / 30f);
            Assert.IsTrue(home.activeSelf, "the teapot is out of view");
            Assert.IsTrue(up.activeSelf, "and the stations above wait");
            Find("Canvas/Home").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.IsFalse(home.activeSelf, "home again: the teapot is on screen");

            runner.State.Coins = BigNum.Zero;
            screen.Refresh();
            screen.LookAt(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(15)) + new Vector2(0f, -200f));
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
            runner.State.ViewX = 2600d;
            runner.State.ViewY = 2400d;
            runner.State.ViewZoom = 1.5d;
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(1.5f, screen.ZoomScale, 1e-3f, "the zoom comes back too");
            Assert.AreEqual(2600f, (1080f / 2f - world.anchoredPosition.x) / world.localScale.x, 1f);
            Assert.AreEqual(2400f, (1920f / 2f - world.anchoredPosition.y) / world.localScale.y, 1f, "the spot under the middle of the screen is the one saved");

            screen.LookAt(new Vector2(2400f, 2900f));
            screen.Advance(1f / 30f);
            Assert.AreEqual(2900d - 0.1d * 1920d / 1.5d, runner.State.ViewY, 1d, "looking elsewhere is noted at once, so the next save keeps it");
            Assert.AreEqual(2400d, runner.State.ViewX, 1d);
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
            Vector2 before = world.anchoredPosition;
            var right = new UnityEngine.EventSystems.PointerEventData(null) { pressPosition = new Vector2(300f, 300f), position = new Vector2(520f, 290f) };
            swipe.OnEndDrag(right);
            swipe.OnEndDrag(right);
            Assert.AreEqual(1, screen.Selected, "two swipes to the right go back two");
            Assert.AreNotEqual(before, world.anchoredPosition, "the garden followed the card");

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

            list.verticalNormalizedPosition = 0.5f;
            pull.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -6f) });
            pull.OnDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -300f) });
            pull.OnEndDrag(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.IsFalse(pull.Pulling);
            Assert.AreEqual(GardenScreen.SheetHalf, screen.SheetHeight, 0.5f, "the sheet did not move");
            Assert.IsTrue(screen.SheetOpen);

            list.verticalNormalizedPosition = 1f;
            pull.OnBeginDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -6f) });
            Assert.IsTrue(pull.Pulling);
            Assert.IsFalse(list.enabled, "the list holds still while the sheet moves");
            pull.OnDrag(new UnityEngine.EventSystems.PointerEventData(null) { delta = new Vector2(0, -400f) });
            Assert.AreEqual(GardenScreen.SheetHalf - 400f, screen.SheetHeight, 0.5f);
            pull.OnEndDrag(new UnityEngine.EventSystems.PointerEventData(null));
            Assert.IsTrue(list.enabled, "and scrolls again afterwards");
            Assert.IsFalse(screen.SheetOpen, "let go well down, the sheet closes");

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
            Find("Canvas/Hud/Purse").onClick.Invoke();
            Assert.IsFalse(screen.Income.IsOpen, "a hold is not a tap");
            Find("Canvas/Hud/Purse").onClick.Invoke();
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
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
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
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
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
            Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy").onClick.Invoke();
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
            Find("Canvas/Picker").onClick.Invoke();
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

            screen.Advance((float)Pounding.BeatTime(0) - (float)screen.Pound.Time);
            Find("Canvas/Pound").onClick.Invoke();
            Assert.AreEqual(1, screen.Pound.Run.Beat);
            Assert.AreEqual(3, screen.Pound.Run.Score, "a perfect pound");
            Assert.AreEqual(L10n.T("pound.perfect"), root.transform.Find("Canvas/Pound/Shake/Judge").GetComponent<Text>().text);

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

            screen.Basket.SetBasket(0.9f);
            Assert.AreEqual(0.9f, screen.Basket.BasketX, 1e-6f);
            Motion.SetReduced(true);
            screen.Basket.MoveBasket(0.2f);
            screen.Advance(0.05f);
            Assert.AreEqual(0.2f, screen.Basket.BasketX, 1e-6f);

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
            Transform station = world.Find("Station " + runner.Defs[0].Id), front = world.Find("Station " + runner.Defs[0].Id + " Front");
            Assert.IsNull(world.Find("Traveler"));

            Assert.IsTrue(runner.AssignRabbit(1, 0));
            screen.Refresh();
            var traveler = (RectTransform)world.Find("Traveler");
            Assert.IsNotNull(traveler, "the rabbit sets off");
            Assert.AreEqual("kind_tho-me", traveler.Find("Art").GetComponent<Image>().sprite.name);
            Assert.IsFalse(station.Find("Rabbit").gameObject.activeSelf, "not at the station until it gets there");
            Assert.IsFalse(front.Find("Pad").gameObject.activeSelf, "and the pad does not show either");
            Assert.AreEqual(world.Find("Seat 0").GetComponent<RectTransform>().anchoredPosition, traveler.anchoredPosition, "from its seat");
            screen.Advance(0.5f);
            Assert.AreNotEqual(world.Find("Seat 0").GetComponent<RectTransform>().anchoredPosition, traveler.anchoredPosition, "on its way");
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
            Assert.IsNull(world.Find("Traveler"), "arrived");
            Assert.IsTrue(station.Find("Rabbit").gameObject.activeSelf, "walking=" + screen.TravelersWalking + " pad=" + front.Find("Pad").gameObject.activeSelf + " at=" + Rabbits.Find(runner.State, 1).Station + " name=" + station.name);

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
            Assert.Greater(Vector2.Distance(later, seat.anchoredPosition), 40f, "two seconds on it has walked away from the seat");
            Assert.Less(Vector2.Distance(later, seat.anchoredPosition), 2f * 300f, "at a walk, not a jump");
        }

        bool StepUntilAtSpot(int seat, float most = GardenScreen.StrollPeriod)
        {
            for (float t = 0f; t < most; t += 0.25f)
            {
                screen.Advance(0.25f);
                if (screen.AtSpot(seat)) return true;
            }
            return false;
        }

        [Test]
        public void A_Resting_Rabbit_Strolls_To_An_Empty_Spot_Does_Something_There_And_Comes_Back_Every_Period()
        {
            screen.Shell.StartGame();
            runner.State.Energy = 10;
            Rabbit r = GiveWorker(1, -1);
            Transform world = root.transform.Find("Canvas/Garden/Content");
            var walker = (RectTransform)world.Find("Walker 0");
            var seat = (RectTransform)world.Find("Seat 0");
            screen.Advance(0f);
            Assert.IsTrue(walker.gameObject.activeSelf, "out at the start of its window");
            Assert.AreEqual(seat.anchoredPosition, walker.anchoredPosition, "from its seat");
            Assert.IsFalse(screen.AtSpot(0), "still on the way");

            Assert.IsTrue(StepUntilAtSpot(0), "it reaches its spot");
            int spot = screen.StrollSpotOf(0);
            Assert.GreaterOrEqual(spot, 0);
            Vector2 at = screen.LawnSpots[spot];
            Assert.LessOrEqual(Vector2.Distance(at, seat.anchoredPosition), GardenScreen.StrollRadius, "an empty spot within reach of its seat");
            Assert.LessOrEqual(Vector2.Distance(at, walker.anchoredPosition), 16f, "standing on it, give or take a hop");
            Assert.IsTrue(screen.ActivityOf(0).HasValue, "doing something there");
            Transform bubble = walker.Find("Bubble");
            Assert.IsTrue(bubble.gameObject.activeSelf, "with a bubble over its head");
            Assert.That(bubble.Find("Text").GetComponent<Text>().text, Is.EqualTo("♪").Or.EqualTo("♥").Or.EqualTo("zZ"));
            Assert.IsFalse(seat.gameObject.activeSelf, "its seat stands empty meanwhile");

            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.BeginDrag);
            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.Drag);
            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.EndDrag);
            Assert.AreEqual(Rabbits.PetPoints, r.Affection, "a stroke on the lawn counts");
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6);

            float stayed = 0f;
            while (screen.AtSpot(0) && stayed < 10f) { screen.Advance(0.25f); stayed += 0.25f; }
            Assert.That(stayed, Is.GreaterThanOrEqualTo(Strolls.LingerLeast - 0.5f).And.LessThanOrEqualTo(Strolls.LingerMost + 0.5f), "stayed " + stayed);
            Assert.IsFalse(bubble.gameObject.activeSelf, "the bubble goes when it sets off");
            Assert.IsTrue(walker.gameObject.activeSelf, "walking home");
            Assert.Greater(Vector2.Distance(walker.anchoredPosition, seat.anchoredPosition), 20f);
            while (GardenScreen.Strolling(0, screen.Clock)) screen.Advance(0.25f);
            Assert.IsFalse(walker.gameObject.activeSelf, "home");
            Assert.IsTrue(seat.gameObject.activeSelf);

            screen.Advance(GardenScreen.StrollPeriod - screen.Clock % GardenScreen.StrollPeriod + 0.1f);
            Assert.IsTrue(GardenScreen.Strolling(0, screen.Clock));
            Assert.IsTrue(StepUntilAtSpot(0), "out again the next period");
        }

        [Test]
        public void At_The_Spot_A_Rabbit_Drinks_Tea_Chases_A_Firefly_By_Night_Or_Dozes()
        {
            screen.Shell.StartGame();
            screen.ClockHours = () => 22.0;
            screen.Refresh();
            GiveWorker(1, -1);
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Transform walker = world.Find("Walker 0");
            var seen = new HashSet<StrollActivity>();
            for (int period = 0; period < 24 && seen.Count < 3; period++)
            {
                if (!StepUntilAtSpot(0)) continue;
                StrollActivity a = screen.ActivityOf(0).Value;
                seen.Add(a);
                string bubble = walker.Find("Bubble/Text").GetComponent<Text>().text;
                switch (a)
                {
                    case StrollActivity.Tea:
                        Assert.IsTrue(walker.Find("Cup").gameObject.activeSelf, "a cup in its paws");
                        Assert.IsTrue(walker.Find("Cup/Steam").gameObject.activeSelf, "with a little steam");
                        Assert.AreEqual("♪", bubble);
                        break;
                    case StrollActivity.Firefly:
                        Assert.IsTrue(walker.Find("Firefly").gameObject.activeSelf, "one firefly circles it");
                        Assert.IsFalse(walker.Find("Cup").gameObject.activeSelf);
                        Assert.AreEqual("♥", bubble);
                        break;
                    default:
                        Assert.AreNotEqual(0f, Mathf.DeltaAngle(0f, walker.Find("Art").localRotation.eulerAngles.z), "lying down");
                        Assert.IsFalse(walker.Find("Firefly").gameObject.activeSelf);
                        Assert.AreEqual("zZ", bubble);
                        break;
                }
                while (GardenScreen.Strolling(0, screen.Clock)) screen.Advance(0.25f);
                Assert.AreEqual(0f, Mathf.DeltaAngle(0f, walker.Find("Art").localRotation.eulerAngles.z), 0.01f, "up again on the way home");
            }
            Assert.AreEqual(3, seen.Count, "all three in two dozen strolls: " + string.Join(", ", seen));

            screen.ClockHours = () => 13.0;
            for (int period = 0; period < 12; period++)
            {
                if (!StepUntilAtSpot(0)) continue;
                Assert.AreNotEqual(StrollActivity.Firefly, screen.ActivityOf(0).Value, "no firefly by day");
                while (GardenScreen.Strolling(0, screen.Clock)) screen.Advance(0.25f);
            }
        }

        [Test]
        public void Under_Reduced_Motion_The_Rabbit_Is_At_Its_Spot_And_Back_On_Its_Seat_Without_Walking()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            GiveWorker(1, -1);
            Transform world = root.transform.Find("Canvas/Garden/Content");
            var walker = (RectTransform)world.Find("Walker 0");
            var seat = (RectTransform)world.Find("Seat 0");
            screen.Advance(0f);
            Assert.IsFalse(walker.gameObject.activeSelf, "no walk out: it sits until it is at the spot");
            Assert.IsTrue(seat.gameObject.activeSelf);
            Assert.IsTrue(StepUntilAtSpot(0));
            Vector2 at = walker.anchoredPosition;
            Assert.AreEqual(screen.LawnSpots[screen.StrollSpotOf(0)], at, "simply there");
            Assert.IsFalse(seat.gameObject.activeSelf);
            float stayed = 0f;
            while (screen.AtSpot(0) && stayed < 10f)
            {
                screen.Advance(0.25f); stayed += 0.25f;
                if (screen.AtSpot(0)) Assert.AreEqual(at, walker.anchoredPosition, "it does not move while it is there");
            }
            Assert.IsFalse(walker.gameObject.activeSelf, "and then it is back");
            Assert.IsTrue(seat.gameObject.activeSelf);
        }

        [Test]
        public void No_More_Than_One_Rabbit_Per_Three_Empty_Spots_Is_Out_And_No_Two_Share_A_Spot()
        {
            screen.Shell.StartGame();
            runner.State.RestSpots = RestSpots.Max;
            for (int i = 1; i <= RestSpots.Max; i++) runner.State.Rabbits.Add(new Rabbit { Id = i, Tier = 0, Kind = "tho-bong", Name = "T" + i });
            runner.State.NextRabbitId = RestSpots.Max;
            screen.Refresh();
            int cap = Strolls.MaxAtOnce(screen.LawnSpots.Count);
            Assert.GreaterOrEqual(cap, 2, "room for a few at once");
            int most = 0;
            for (float t = 0f; t < 3f * GardenScreen.StrollPeriod; t += 0.5f)
            {
                screen.Advance(0.5f);
                int outNow = screen.StrollersOut;
                most = Mathf.Max(most, outNow);
                Assert.LessOrEqual(outNow, cap, "at " + screen.Clock);
                var taken = new HashSet<int>();
                for (int k = 0; k < RestSpots.Max; k++)
                {
                    int spot = screen.StrollSpotOf(k);
                    if (spot < 0) continue;
                    Assert.IsTrue(taken.Add(spot), "two rabbits on spot " + spot + " at " + screen.Clock);
                }
            }
            Assert.GreaterOrEqual(most, 2, "and more than one does go out");
        }

        [Test]
        public void Every_Lawn_Spot_Keeps_Clear_Of_The_Stations_The_Props_And_The_Seats()
        {
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Assert.GreaterOrEqual(screen.LawnSpots.Count, 6);
            foreach (Vector2 spot in screen.LawnSpots)
            {
                for (int i = 0; i < runner.Defs.Count; i++) Assert.GreaterOrEqual(Vector2.Distance(spot, screen.StationSpot(i)), Strolls.Clearance, spot + " by station " + i);
                for (int i = 0; i < Decor.Spots; i++) Assert.GreaterOrEqual(Vector2.Distance(spot, screen.DecorSpotAt(i)), 150f, spot + " by decoration spot " + i);
                Assert.GreaterOrEqual(Vector2.Distance(spot, GardenScreen.PondSpot), 300f, spot + " in the pond");
                Assert.GreaterOrEqual(Vector2.Distance(spot, screen.GuestSpot), 150f, spot + " on the guest");
                for (int k = 0; k < RestSpots.Max; k++)
                    Assert.GreaterOrEqual(Vector2.Distance(spot, ((RectTransform)world.Find("Seat " + k)).anchoredPosition), 150f, spot + " on seat " + k);
                Assert.IsTrue(spot.x > 150f && spot.x < GardenScreen.ReadableRight - 80f, spot + " under the wood at the edge");
            }
            for (int k = 0; k < RestSpots.Max; k++)
            {
                if (!runner.State.Grid.InLawn(GardenGrid.DefaultRestTile(k))) continue;
                Vector2 seat = ((RectTransform)world.Find("Seat " + k)).anchoredPosition;
                bool any = false;
                foreach (Vector2 spot in screen.LawnSpots) any |= Vector2.Distance(spot, seat) <= GardenScreen.StrollRadius;
                Assert.IsTrue(any, "seat " + k + " has a spot within reach");
            }
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

        float[] Spreads(int frames, params System.Func<Vector4>[] samples)
        {
            var first = new Vector4[samples.Length];
            var spread = new float[samples.Length];
            for (int k = 0; k < samples.Length; k++) first[k] = samples[k]();
            for (int i = 0; i < frames; i++)
            {
                screen.Advance(1f / 30f);
                for (int k = 0; k < samples.Length; k++) spread[k] = Mathf.Max(spread[k], (samples[k]() - first[k]).magnitude);
            }
            return spread;
        }

        static Vector4 Pose(Transform t) => new Vector4(t.localPosition.x, t.localPosition.y, t.localScale.y + t.localScale.x * 10f, t.localRotation.eulerAngles.z);

        [Test]
        public void Under_Reduced_Motion_The_Looping_Garden_Motions_Hold_Still()
        {
            screen.Shell.StartGame();
            GameState s = runner.State;
            s.Coins = new BigNum(1e12);
            s.Levels[1] = 1;
            s.RestSpots = 3;
            for (long id = 2; id <= 4; id++) s.Rabbits.Add(new Rabbit { Id = id, Tier = 0, Kind = "tho-bong", Name = "Nghỉ " + id });
            GiveWorker();
            for (int i = 0; i < 90; i++) screen.Advance(1f / 30f);

            Transform world = root.transform.Find("Canvas/Garden/Content");
            Transform rabbit = world.Find("Station am-tra/Rabbit");
            Transform pad = world.Find("Station gio-la Front/Pad");
            Transform arrow = world.Find("Station am-tra Front/CanUp");
            Transform sign = world.Find("Station " + runner.Defs[2].Id + " Front/Level");
            Transform seat = null;
            for (int k = 0; k < RestSpots.Max && seat == null; k++)
            {
                Transform candidate = world.Find("Seat " + k);
                if (candidate != null && candidate.gameObject.activeSelf) seat = candidate;
            }
            Transform fireflies = world.Find("Fireflies");
            Transform gold = root.transform.Find("Canvas/GoldFirefly");
            Assert.IsTrue(rabbit.gameObject.activeSelf, "a rabbit works at the first station");
            Assert.IsTrue(pad.gameObject.activeSelf, "the second station waits for one");
            Assert.IsTrue(arrow.gameObject.activeSelf, "the first station can be raised");
            Assert.IsNotNull(seat, "a resting rabbit sits in the garden");
            Assert.Greater(fireflies.childCount, 0);
            Transform firefly = fireflies.GetChild(0);
            Image fireflyGlow = firefly.GetComponent<Image>();
            screen.SpawnGoldFirefly();

            System.Func<Vector4>[] loops =
            {
                () => Pose(rabbit),
                () => Pose(pad),
                () => Pose(sign),
                () => Pose(arrow),
                () => Pose(seat),
                () => Pose(firefly) + new Vector4(0f, 0f, fireflyGlow.color.a, 0f),
                () => Pose(gold),
            };
            string[] names = { "station rabbit", "empty pad", "price sign", "upgrade arrow", "rest-spot rabbit", "firefly", "golden firefly" };
            float[] moving = Spreads(45, loops);
            for (int k = 0; k < loops.Length; k++) Assert.Greater(moving[k], 0.001f, "the " + names[k] + " moves with full motion");

            Motion.SetReduced(true);
            screen.SpawnGoldFirefly();
            screen.DozeSomeone();
            screen.PlayPayout(0, new BigNum(5));
            screen.Advance(1f / 30f);
            float[] still = Spreads(60, loops);
            for (int k = 0; k < loops.Length; k++) Assert.AreEqual(0f, still[k], 1e-4f, "the " + names[k] + " holds still under reduced motion");
            Assert.AreEqual(-9f, Mathf.DeltaAngle(0f, rabbit.localRotation.eulerAngles.z), 0.01f, "a dozing rabbit rests at its nodding lean");
            Assert.AreEqual(Vector3.one, rabbit.localScale, "no breathing");
            Assert.AreEqual(Quaternion.identity, seat.localRotation);
            Assert.Greater(((RectTransform)gold).anchoredPosition.x, 0f, "the golden firefly waits where it can be seen");
            Assert.IsTrue(gold.gameObject.activeSelf);
        }

        [Test]
        public void Under_Reduced_Motion_Bursts_Flights_And_Shakes_Stay_Still()
        {
            screen.Shell.StartGame();
            Motion.SetReduced(true);
            Transform feel = root.transform.Find("Canvas/Feel");
            int before = screen.Feel.ParticleCount;
            screen.Feel.Burst(Vector2.zero, runner.Catalog.Round, new[] { Palette.Lantern }, 12);
            Assert.AreEqual(before, screen.Feel.ParticleCount, "no bits burst out");

            bool arrived = false;
            screen.Feel.Fly(new Vector2(-200, -300), new Vector2(300, 700), runner.Catalog.Currency, Color.white, 5, 40f, () => arrived = true);
            for (int i = 0; i < feel.childCount; i++)
                Assert.IsFalse(feel.GetChild(i).name == "Fly" && feel.GetChild(i).gameObject.activeSelf, "no carrot crosses the screen");
            for (int i = 0; i < 3; i++) screen.Advance(1f / 30f);
            Assert.IsTrue(arrived, "the purse still hears that they came");
            Assert.AreEqual(0, screen.Feel.Flying);

            runner.State.Coins = BigNum.Zero;
            screen.OpenSheet(0);
            screen.Refresh();
            var buy = (RectTransform)root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row am-tra/Buy");
            var face = (RectTransform)buy.Find("Face");
            buy.GetComponent<PressDip>().Click();
            Assert.AreEqual("deny", Sfx.Last, "it still says no");
            for (int i = 0; i < 12; i++)
            {
                screen.Advance(1f / 30f);
                Assert.AreEqual(0f, face.offsetMin.x, "the button does not shake");
            }
        }

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
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);
            Assert.AreEqual(runner.TodayNumber, r.PetDay);
            Assert.AreEqual(1, runner.State.TotalTaps, "a touch on the rabbit is still a tap on its station");

            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6, "the second stroke of the day is for love alone");
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);

            screen.Detail.Open(runner.Defs, runner.State, 1);
            Text hearts = root.transform.Find("Canvas/RabbitSheet/Card/Body/Bond/Text").GetComponent<Text>();
            Assert.AreEqual("♥♡♡♡♡♡♡♡♡♡", hearts.text, "one heart of ten on the rabbit's own sheet (#58)");
            r.Affection = Rabbits.PointsFor(4);
            screen.Refresh();
            Assert.AreEqual("♥♥♥♥♡♡♡♡♡♡", root.transform.Find("Canvas/RabbitSheet/Card/Body/Bond/Text").GetComponent<Text>().text, "the sheet follows the bond");
            Assert.AreEqual("Cấp 1 ♥♥♥♥", root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Level").GetComponent<Text>().text, "and so does the grid cell");
            r.Affection = Rabbits.PointsFor(7);
            screen.Refresh();
            Assert.AreEqual("Cấp 1 ♥7", root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/Row 0/Rabbit 1/Level").GetComponent<Text>().text, "past five, one heart and the count");
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
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);
            Trigger(rabbit, UnityEngine.EventSystems.EventTriggerType.PointerClick);
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
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);
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
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);
            Assert.IsTrue(screen.Detail.IsOpen, "a touch still opens its sheet");
            Assert.AreEqual(1, screen.Detail.RabbitId);
            screen.Detail.Close();

            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.BeginDrag);
            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.Drag);
            Trigger("Canvas/Garden/Content/Walker 0/Art", UnityEngine.EventSystems.EventTriggerType.EndDrag);
            Assert.AreEqual(Rabbits.PetPoints, r.Affection, "one day's points, wherever it is");
            Assert.AreEqual(10 + Rabbits.PetEnergy, runner.State.Energy, 1e-6);
            Assert.IsFalse(screen.Detail.IsOpen, "a stroke does not open the sheet");
        }

        [Test]
        public void Close_Friends_Say_A_Line_In_The_Garden_And_Give_A_Clover()
        {
            screen.Shell.StartGame();
            Rabbit r = GiveWorker(1, 0, Rabbits.PointsFor(Rabbits.LineHearts) - Rabbits.PetPoints);
            Transform line = root.transform.Find("Canvas/Garden/Content/BondLine");
            Assert.IsTrue(line == null || !line.gameObject.activeSelf, "nothing to say yet");
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(Rabbits.LineHearts, Rabbits.HeartsOf(r));
            line = root.transform.Find("Canvas/Garden/Content/BondLine");
            Assert.IsNotNull(line, "the line is spoken over the rabbit");
            Assert.IsTrue(line.gameObject.activeSelf);
            Assert.AreEqual(L10n.T("bond.line.Brewer"), line.Find("Text").GetComponent<Text>().text, "a brewer's line for a brewer");
            Assert.AreNotEqual("", L10n.T("bond.line.Brewer"));
            for (int i = 0; i < 30 * 5; i++) screen.Advance(1f / 30f);
            Assert.IsFalse(line.gameObject.activeSelf, "the line leaves on its own");

            runner.State.Clovers = 0;
            r.Affection = Rabbits.PointsFor(Rabbits.GiftHearts) - Rabbits.PetPoints;
            r.PetDay = -1;
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Assert.AreEqual(Rabbits.GiftHearts, Rabbits.HeartsOf(r));
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
            screen.Advance(0.1f);
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
            runner.State.Levels[2] = 1;
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

            Tray.Add(runner.State, order.Lines[0].Station, false);
            screen.Refresh();
            Assert.AreEqual(order.Lines[0].Count == 1, line0.Find("Met").gameObject.activeSelf);
            Assert.AreEqual(order.Lines.Count == 1 && order.Lines[0].Count == 1, deliver.interactable);
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
            screen.Refresh();
            Assert.IsFalse(root.transform.Find("Canvas/Hud/Order").gameObject.activeSelf);

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
            for (int k = 0; k < world.childCount; k++)
            {
                Transform child = world.GetChild(k);
                if (child.name != "Flowers") continue;
                Vector2 at = ((RectTransform)child).anchoredPosition;
                for (int i = 0; i < runner.Defs.Count; i++)
                {
                    Vector2 spot = screen.StationSpot(i);
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
            screen.LookAt(new Vector2(800f, 1100f));
            screen.Advance(0f);
            Vector2 street0 = street.anchoredPosition, canopy0 = canopy.anchoredPosition;
            screen.LookAt(new Vector2(4000f, 1100f));
            screen.Advance(0f);
            float groundMoved = street.anchoredPosition.x - street0.x, canopyMoved = canopy.anchoredPosition.x - canopy0.x;
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
                for (int i = 0; i < runner.Defs.Count; i++) nearest = Mathf.Min(nearest, Vector2.Distance(at, screen.StationSpot(i)));
                Assert.That(nearest, Is.InRange(200f, 320f), "each planter belongs to a station, beside it rather than on it");
            }
            Vector2 mortar = ((RectTransform)world.Find("Mortar")).anchoredPosition, basket = screen.StationSpot(1);
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
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest don"), "and the tiered orders achievement (#111)");
            Tray.Add(runner.State, 0, false);
            Assert.IsTrue(runner.DeliverOrder(out _, out _));
            screen.Refresh();
            StringAssert.StartsWith("1 / 2", quest.Find("Count").GetComponent<Text>().text);
        }

        [Test]
        public void A_Station_Wears_Its_Rank_From_25_100_And_300_And_Every_Level_Text_Says_So()
        {
            Transform station = root.transform.Find("Canvas/Garden/Content/Station am-tra"), front = Front("am-tra");
            Text plate = front.Find("Level/Text").GetComponent<Text>();
            Motion.SetReduced(true);
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
            Assert.AreEqual(Palette.Paper, front.Find("Level").GetComponent<Image>().color);

            runner.State.Levels[0] = 100;
            Show();
            Assert.IsTrue(station.Find("RankLanterns").gameObject.activeSelf, "stone lanterns from 100");
            Assert.AreNotEqual(Palette.Paper, front.Find("Level").GetComponent<Image>().color, "and a golden plate");
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
            screen.RebuildUi();
            screen.Shell.StartGame();
            runner.State.Energy = 28;
            runner.State.BoostSecondsLeft = 30;
            screen.Refresh();
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)root.transform.Find("Canvas"));
            Canvas.ForceUpdateCanvases();
            Text label = root.transform.Find("Canvas/Hud/Energy/Face/Label").GetComponent<Text>();
            Text action = root.transform.Find("Canvas/Hud/Energy/Face/Action").GetComponent<Text>();
            Assert.AreEqual(L10n.T("energy.boosting", 30), action.text);
            Assert.LessOrEqual(label.rectTransform.anchorMax.x, action.rectTransform.anchorMin.x, "the name's box ends where the countdown's begins");
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
            screen.RebuildUi();
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(5);
            screen.Refresh();
            var street = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var jar = (RectTransform)street.Find("Jar");
            Assert.IsTrue(jar.gameObject.activeSelf, "the jar is out from garden level five");
            var plate = (RectTransform)jar.Find("Plate");
            float centre = jar.anchoredPosition.x + plate.anchoredPosition.x;
            float left = centre - plate.rect.width / 2f, right = centre + plate.rect.width / 2f;
            Assert.GreaterOrEqual(left, 24f, "inside the left edge");
            Assert.LessOrEqual(right, street.sizeDelta.x - 24f, "inside the right edge");
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
            for (int k = 0; k < 8; k++) screen.Advance(0.5f);
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
            runner.EquipItem(hat.Id, 1);
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

        [Test]
        public void Decoration_Spots_Stay_Hidden_Until_Something_Is_Owned_And_Stand_Clear_Of_Everything_Else()
        {
            screen.Shell.StartGame();
            screen.Refresh();
            Transform world = root.transform.Find("Canvas/Garden/Content");
            for (int i = 0; i < Decor.Spots; i++)
            {
                Transform spot = world.Find("Spot " + i);
                Assert.IsNotNull(spot, "spot " + i + " is built with the garden");
                Assert.IsFalse(spot.gameObject.activeSelf, "a newcomer sees no spot " + i);
                Vector2 at = screen.DecorSpotAt(i);
                Assert.AreEqual(at, ((RectTransform)spot).anchoredPosition);
                for (int j = 0; j < runner.Defs.Count; j++) Assert.GreaterOrEqual(Vector2.Distance(at, screen.StationSpot(j)), 200f, "spot " + i + " clear of station " + j);
                Assert.GreaterOrEqual(Vector2.Distance(at, GardenScreen.PondSpot), 200f, "spot " + i + " clear of the pond");
                for (int j = 0; j < i; j++) Assert.GreaterOrEqual(Vector2.Distance(at, screen.DecorSpotAt(j)), 200f, "spots " + i + " and " + j + " apart");
                Assert.That(at.x, Is.InRange(90f, GardenScreen.ReadableRight - 40f), "spot " + i + " inside the garden, short of the trees at the edge");
            }
            for (int k = 0; k < world.childCount; k++)
            {
                Transform child = world.GetChild(k);
                if (child.name != "Planter" && child.name != "Mushrooms" && child.name != "Stones" && child.name != "Bush" && child.name != "Tree") continue;
                var prop = (RectTransform)child;
                Vector2 foot = prop.anchoredPosition - new Vector2(0f, prop.rect.height * prop.pivot.y);
                for (int i = 0; i < Decor.Spots; i++) Assert.GreaterOrEqual(Vector2.Distance(foot, screen.DecorSpotAt(i)), 140f, child.name + " standing at " + foot + " is off spot " + i);
            }

            runner.State.GoldenCarrots = 15;
            screen.OpenShop();
            screen.Refresh();
            Button tile = Find("Canvas/Shop/Card/Decor/Row/Decor den-da");
            Assert.IsTrue(tile.interactable);
            Assert.IsFalse(Find("Canvas/Shop/Card/Decor/Row/Decor bon-hoa").interactable, "twenty are needed");
            Assert.IsFalse(root.transform.Find("Canvas/Shop/Card/Decor/Row/Decor den-da/Owned").gameObject.activeSelf);
            tile.onClick.Invoke();
            Assert.AreEqual(0, Decor.TotalOwned(runner.State), "the first tap only asks");
            Assert.AreEqual(L10n.T("shop.hats.confirm"), root.transform.Find("Canvas/Shop/Card/Decor/Row/Decor den-da/Price/Text").GetComponent<Text>().text);
            tile.onClick.Invoke();
            Assert.AreEqual(1, Decor.Owned(runner.State, "den-da"));
            Assert.AreEqual(0, runner.State.GoldenCarrots);
            screen.Refresh();
            Assert.IsTrue(root.transform.Find("Canvas/Shop/Card/Decor/Row/Decor den-da/Owned").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("shop.decor.owned", 1), root.transform.Find("Canvas/Shop/Card/Decor/Row/Decor den-da/Owned/Text").GetComponent<Text>().text);
            Assert.AreEqual("15", root.transform.Find("Canvas/Shop/Card/Decor/Row/Decor den-da/Price/Text").GetComponent<Text>().text, "the price stays: it can be bought again");
            screen.Shop.Close();
            for (int i = 0; i < Decor.Spots; i++)
            {
                Transform spot = world.Find("Spot " + i);
                Assert.IsTrue(spot.gameObject.activeSelf, "spot " + i + " shows now");
                Assert.IsTrue(spot.Find("Plate").gameObject.activeSelf, "as a '+'");
                Assert.IsFalse(spot.Find("Item").gameObject.activeSelf);
            }
            runner.State.GoldenCarrots = 1000;
            for (int n = 1; n < Decor.Spots; n++) Decor.Buy(runner.State, "bon-hoa");
            screen.OpenShop();
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Shop/Card/Decor/Row/Decor den-da").interactable);
            Assert.AreEqual(L10n.T("shop.decor.full"), root.transform.Find("Canvas/Shop/Card/Decor/Row/Decor den-da/Price/Text").GetComponent<Text>().text);
        }

        [Test]
        public void Tapping_A_Spot_Opens_The_Card_Placing_Stands_The_Piece_There_Swapping_Changes_It_And_Storing_Takes_It_Back()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            runner.State.GoldenCarrots = 100;
            Decor.Buy(runner.State, "den-da");
            Decor.Buy(runner.State, "bon-hoa");
            screen.Refresh();
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Transform spot = world.Find("Spot 2");
            Assert.IsTrue(spot.Find("Plate").gameObject.activeSelf);

            Find("Canvas/Garden/Content/Spot 2").onClick.Invoke();
            Assert.IsTrue(screen.DecorCard.IsOpen, "the card lists what is in storage");
            Assert.AreEqual(2, screen.DecorSpotOpen);
            Assert.IsTrue(Find("Canvas/Decor/Card/Row den-da/Use").interactable);
            Assert.IsTrue(Find("Canvas/Decor/Card/Row bon-hoa/Use").interactable);
            Assert.IsFalse(Find("Canvas/Decor/Card/Row ghe-nam/Use").interactable, "none owned");
            Assert.IsFalse(root.transform.Find("Canvas/Decor/Card/Store").gameObject.activeSelf, "nothing to put away on an empty spot");
            Assert.AreEqual(L10n.T("decor.left", 1), root.transform.Find("Canvas/Decor/Card/Row den-da/Count").GetComponent<Text>().text);
            Find("Canvas/Decor/Card/Row den-da/Use").onClick.Invoke();
            Assert.AreEqual("den-da", runner.State.DecorSpots[2]);
            Assert.IsFalse(screen.DecorCard.IsOpen, "placed: the card goes");
            Assert.AreEqual("blip", Sfx.Last);
            screen.Refresh();
            Assert.IsFalse(spot.Find("Plate").gameObject.activeSelf, "the '+' is gone from this spot");
            Assert.IsTrue(spot.Find("Item").gameObject.activeSelf);
            Assert.AreEqual("decor_den-da", spot.Find("Item").GetComponent<Image>().sprite.name);
            Assert.AreEqual(0f, ((RectTransform)spot.Find("Item")).pivot.y, "it stands on its feet");
            Assert.IsTrue(world.Find("Spot 3/Plate").gameObject.activeSelf, "the planter still waits in storage: the other spots keep their '+'");

            Find("Canvas/Garden/Content/Spot 2").onClick.Invoke();
            Assert.IsTrue(screen.DecorCard.IsOpen);
            Assert.IsTrue(root.transform.Find("Canvas/Decor/Card/Store").gameObject.activeSelf);
            Assert.IsFalse(Find("Canvas/Decor/Card/Row den-da/Use").interactable, "the one lantern is the one standing here");
            Assert.IsTrue(Find("Canvas/Decor/Card/Row bon-hoa/Use").interactable);
            Find("Canvas/Decor/Card/Row bon-hoa/Use").onClick.Invoke();
            Assert.AreEqual("bon-hoa", runner.State.DecorSpots[2]);
            Assert.AreEqual(1, Decor.Available(runner.State, "den-da"), "the lantern went back to storage");
            screen.Refresh();
            Assert.AreEqual("decor_bon-hoa", spot.Find("Item").GetComponent<Image>().sprite.name);
            Assert.IsFalse(world.Find("Spot 3/Item").gameObject.activeSelf);

            Find("Canvas/Garden/Content/Spot 2").onClick.Invoke();
            Find("Canvas/Decor/Card/Store").onClick.Invoke();
            Assert.IsNull(runner.State.DecorSpots[2]);
            Assert.IsFalse(screen.DecorCard.IsOpen);
            screen.Refresh();
            Assert.IsFalse(spot.Find("Item").gameObject.activeSelf);
            Assert.IsTrue(spot.Find("Plate").gameObject.activeSelf, "empty again, and there is something to place");
            Assert.AreEqual(100 - 15 - 20, runner.State.GoldenCarrots, "placing, swapping and storing cost nothing");

            Decor.Place(runner.State, 0, "den-da");
            Decor.Place(runner.State, 1, "bon-hoa");
            screen.Refresh();
            Assert.IsFalse(spot.gameObject.activeSelf, "nothing left to place: the empty spot hides");
            Assert.IsTrue(world.Find("Spot 0").gameObject.activeSelf);
            Assert.IsFalse(world.Find("Spot 0/Plate").gameObject.activeSelf);
        }

        [Test]
        public void The_Lantern_Colour_Row_In_The_Shop_Tints_Every_Paper_Lantern_In_The_Garden()
        {
            screen.Shell.StartGame();
            screen.Refresh();
            Transform world = root.transform.Find("Canvas/Garden/Content");
            Image lantern = world.Find("Lantern").GetComponent<Image>();
            Assert.AreEqual(Color.white, lantern.color, "paper to begin with");
            screen.OpenShop();
            screen.Refresh();
            Assert.AreEqual(Decor.LanternColors.Length, root.transform.Find("Canvas/Shop/Card/Decor/Lanterns").childCount, "one dot per colour");
            Assert.IsTrue(root.transform.Find("Canvas/Shop/Card/Decor/Lanterns/Dot 0/Ring").gameObject.activeSelf, "paper is ringed as the colour hung");
            Assert.IsFalse(Find("Canvas/Shop/Card/Decor/Lanterns/Dot 2").interactable, "ten golden carrots are needed");
            runner.State.GoldenCarrots = Decor.LanternColorCost;
            screen.Refresh();
            Assert.IsFalse(Find("Canvas/Shop/Card/Decor/Lanterns/Dot 0").interactable, "the colour already hung is not sold");
            Button mint = Find("Canvas/Shop/Card/Decor/Lanterns/Dot 2");
            Assert.IsTrue(mint.interactable);
            mint.onClick.Invoke();
            Assert.AreEqual(2, runner.State.LanternColor);
            Assert.AreEqual(0, runner.State.GoldenCarrots);
            screen.Refresh();
            Assert.IsTrue(root.transform.Find("Canvas/Shop/Card/Decor/Lanterns/Dot 2/Ring").gameObject.activeSelf);
            Assert.IsFalse(root.transform.Find("Canvas/Shop/Card/Decor/Lanterns/Dot 0/Ring").gameObject.activeSelf);
            Color tint = GardenScreen.LanternTint(2);
            Assert.AreNotEqual(Color.white, tint);
            int lanterns = 0;
            for (int k = 0; k < world.childCount; k++)
            {
                Transform child = world.GetChild(k);
                if (child.name != "Lantern") continue;
                Assert.AreEqual(tint, child.GetComponent<Image>().color, "every lantern wears the colour");
                lanterns++;
            }
            Assert.GreaterOrEqual(lanterns, 2);
            Assert.AreEqual(1f, lantern.color.a, "a tint, never a fade");
        }

        [Test]
        public void A_Season_Beginning_Is_A_Banner_And_The_Gate_Wears_The_Seasons_Dressing_Only_When_It_Has_One()
        {
            screen.Shell.StartGame();
            long tet = runner.TodayNumber;
            runner.State.SeasonGreetedDay = -1;
            bool raised = false;
            runner.SeasonBegan += s => raised = true;
            bool greeted = runner.GreetSeason();
            Assert.AreEqual(Seasons.SeasonOf(tet) != Season.None, greeted, "greeted exactly when a season runs today");
            Assert.AreEqual(greeted, raised);
            if (greeted) StringAssert.Contains(L10n.Season(Seasons.SeasonOf(tet)), root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);

            Transform decor = root.transform.Find("Canvas/SummonMoment/Gate/GateDecor");
            Assert.IsNotNull(decor, "the hook on the arch");
            screen.Arrival.SetSeasonDecor(runner.Catalog.Round);
            Assert.IsTrue(decor.gameObject.activeSelf);
            screen.Arrival.Show(runner.Catalog.Rabbit, "Mai", "", Palette.Lantern, false, "", "");
            Motion.SetReduced(false);
            float maxAngle = 0f; bool moved = false;
            for (int k = 0; k < 30; k++) { screen.Advance(0.1f); float a = Mathf.Abs(Mathf.DeltaAngle(0f, decor.localRotation.eulerAngles.z)); maxAngle = Mathf.Max(maxAngle, a); if (a > 0.2f) moved = true; }
            Assert.IsTrue(moved, "it swings"); Assert.LessOrEqual(maxAngle, 2.6f, "between fixed bounds");
            Motion.SetReduced(true);
            screen.Advance(0.1f);
            Assert.AreEqual(Quaternion.identity, decor.localRotation, "still under reduced motion");
            screen.Arrival.SetSeasonDecor(null);
            Assert.IsFalse(decor.gameObject.activeSelf, "nothing out of season or without a picture");
        }

        [Test]
        public void A_New_Moon_Does_Not_Hand_Todays_Daily_Quests_Out_Again()
        {
            screen.Shell.StartGame();
            runner.State.GardenXp = Garden.XpForLevel(NewMoon.FirstLevel);
            runner.State.Claimed.Add("ngay-cham");
            long day = runner.State.DailyDay;
            runner.State.DailyCarrotTarget = new BigNum(1e12);
            Assert.IsTrue(runner.BeginNewMoon(out _));
            Assert.AreEqual(day, runner.State.DailyDay, "still the same day");
            Assert.IsTrue(runner.State.Claimed.Contains("ngay-cham"), "the daily claimed this morning stays claimed");
            Assert.Less(runner.State.DailyCarrotTarget.ToDouble(), 1e12, "but the carrot goal fits the small garden");
        }

        Rabbit RabbitAtTheTeapot()
        {
            runner.State.Levels[0] = 12;
            Rabbit r = Rabbits.Arrive(runner.State, 0, "Bông", 0d);
            Assert.IsTrue(Rabbits.Assign(runner.State, r.Id, 0));
            screen.Refresh();
            return r;
        }

        [Test]
        public void A_Rabbit_Puts_Up_A_Camera_Bubble_After_Four_Minutes_Of_Play_Never_While_Paused_And_It_Goes_After_Ninety_Seconds()
        {
            Motion.SetReduced(true);
            Rabbit bong = RabbitAtTheTeapot();
            Transform bubble = root.transform.Find("Canvas/Garden/Content/PhotoAsk");
            Assert.IsNotNull(bubble);
            for (int i = 0; i < 100; i++) screen.Advance(1f);
            Assert.AreEqual(0d, runner.State.PhotoAskClock, "the title does not count");
            screen.Shell.StartGame();
            for (int i = 0; i < (int)PhotoAsks.Interval - 1; i++) screen.Advance(1f);
            Assert.IsFalse(PhotoAsks.Waiting(runner.State), "not before four minutes of play");
            Assert.IsFalse(bubble.gameObject.activeSelf);
            screen.Advance(1f);
            screen.Advance(0.5f);
            Assert.IsTrue(PhotoAsks.Waiting(runner.State), "four minutes in");
            Assert.AreEqual(bong.Id, runner.State.PhotoAskRabbit);
            Assert.IsTrue(screen.PhotoAskShown);
            Assert.AreEqual(bubble.parent.childCount - 1, bubble.GetSiblingIndex(), "over everything in the garden");
            Vector2 over = ((RectTransform)bubble).anchoredPosition, station = screen.StationSpot(0);
            Assert.Greater(over.y, station.y, "above the rabbit");
            Assert.Less(Mathf.Abs(over.x - station.x), 200f, "at its station");

            double left = runner.State.PhotoAskLeft;
            screen.Shell.OpenPause();
            for (int i = 0; i < 200; i++) screen.Advance(1f);
            Assert.AreEqual(left, runner.State.PhotoAskLeft, 1e-9, "a pause holds the bubble");
            screen.Shell.Resume();
            for (int i = 0; i < (int)PhotoAsks.StaySeconds + 1; i++) screen.Advance(1f);
            Assert.IsFalse(PhotoAsks.Waiting(runner.State), "a minute and a half, then it goes");
            Assert.IsFalse(bubble.gameObject.activeSelf);
            Assert.AreEqual(0, runner.State.PhotosTaken);
        }

        [Test]
        public void Tapping_The_Bubble_Frames_That_Rabbit_Up_Close_And_The_Picture_Pays_Two_Minutes_That_Fly_To_The_Purse()
        {
            Motion.SetReduced(true);
            Rabbit bong = RabbitAtTheTeapot();
            Rabbits.Arrive(runner.State, 1, "Mây", 0d);
            screen.Shell.StartGame();
            screen.Refresh();
            Assert.IsTrue(PhotoAsks.Ask(runner.Defs, runner.State, runner.TodayNumber, () => 0d));
            Assert.AreEqual(bong.Id, runner.State.PhotoAskRabbit);
            runner.State.PhotoAskLeft = 2d;
            screen.Advance(0.1f);
            Assert.IsTrue(screen.PhotoAskShown);

            Find("Canvas/Garden/Content/PhotoAsk").onClick.Invoke();
            Assert.IsTrue(screen.PhotoMode, "the camera comes up");
            Assert.IsTrue(screen.PhotoAskFraming);
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom, "close in");
            Assert.IsFalse(root.transform.Find("Canvas/Hud").gameObject.activeSelf);
            Assert.IsFalse(screen.PhotoAskShown, "the bubble is not in the picture");
            Vector2? onScreen = screen.AskingRabbitOnScreen;
            Assert.IsTrue(onScreen.HasValue);
            Assert.Less(Vector2.Distance(onScreen.Value, new Vector2(540f, 1920f * 0.55f)), 2f, "the rabbit that asked is in the middle");
            for (int i = 0; i < 5; i++) screen.Advance(1f);
            Assert.IsTrue(PhotoAsks.Waiting(runner.State), "the bubble's time stands still while framing");

            BigNum expected = PhotoAsks.Reward(runner.Defs, runner.State, Modifiers.From(runner.UpgradeDefs, runner.State));
            Assert.Greater(expected.ToDouble(), 0d);
            BigNum before = runner.State.Coins;
            string dir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "vuontra-ask-" + System.Guid.NewGuid().ToString("N"));
            Transform review = root.transform.Find("Canvas/PhotoReview");
            try
            {
                screen.PhotoFolder = dir;
                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                Assert.IsTrue(System.IO.File.Exists(screen.LastPhotoPath), "a picture as always");
                Assert.IsTrue(runner.State.Coins.Equals(before + expected), "exactly two minutes of income");
                Assert.AreEqual(1L, runner.State.PhotosTaken, "it counts for the photo quests");
                Assert.AreEqual(1, runner.State.GoldenCarrots, "and for the day's first-photo gift");
                Assert.AreEqual(1L, runner.State.PhotoAsksAnswered);
                Assert.AreEqual(1, runner.State.PhotoAsksToday);
                Assert.IsFalse(PhotoAsks.Waiting(runner.State), "answered");
                Assert.IsFalse(screen.PhotoAskFraming);
                Text ask = review.Find("Card/Ask").GetComponent<Text>();
                Assert.IsTrue(ask.gameObject.activeSelf);
                Assert.AreEqual(L10n.T("photo.ask.paid", "Bông", expected.Short()), ask.text);
                StringAssert.Contains("+1", review.Find("Card/Gift").GetComponent<Text>().text);

                Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
                Find("Canvas/PhotoBar/Snap").onClick.Invoke();
                Assert.IsFalse(review.Find("Card/Ask").gameObject.activeSelf);
                Assert.IsTrue(runner.State.Coins.Equals(before + expected), "paid once");
            }
            finally
            {
                if (System.IO.Directory.Exists(dir)) System.IO.Directory.Delete(dir, true);
            }

            Find("Canvas/PhotoReview/Card/Close").onClick.Invoke();
            Assert.AreEqual(0, screen.Feel.Flying, "nothing flies at a hidden purse");
            Find("Canvas/PhotoBar/Done").onClick.Invoke();
            Assert.IsFalse(screen.PhotoMode);
            Assert.IsTrue(root.transform.Find("Canvas/Hud").gameObject.activeSelf);
            Assert.Greater(screen.Feel.Flying, 0, "the carrots fly from the rabbit to the purse");
            Assert.AreEqual("reward", Sfx.Last);
            Assert.IsTrue(runner.State.Coins.Equals(before + expected), "and nothing more is paid for the thanks");
        }

        [Test]
        public void Closing_The_Camera_Without_A_Picture_Loses_That_Invitation_And_Nothing_Else()
        {
            Motion.SetReduced(true);
            RabbitAtTheTeapot();
            screen.Shell.StartGame();
            Assert.IsTrue(runner.AskForPhoto());
            screen.Advance(0.1f);
            BigNum coins = runner.State.Coins;
            int golden = runner.State.GoldenCarrots, clovers = runner.State.Clovers;
            Find("Canvas/Garden/Content/PhotoAsk").onClick.Invoke();
            Assert.IsTrue(screen.PhotoMode);
            Find("Canvas/PhotoBar/Done").onClick.Invoke();
            Assert.IsFalse(screen.PhotoMode);
            Assert.IsFalse(PhotoAsks.Waiting(runner.State), "that invitation is gone");
            screen.Advance(0.1f);
            Assert.IsFalse(screen.PhotoAskShown);
            Assert.IsTrue(runner.State.Coins.Equals(coins));
            Assert.AreEqual(golden, runner.State.GoldenCarrots);
            Assert.AreEqual(clovers, runner.State.Clovers);
            Assert.AreEqual(0L, runner.State.PhotosTaken);
            Assert.AreEqual(0, runner.State.PhotoAsksToday, "nor one of the day's ten");
            Assert.AreEqual(0, screen.Feel.Flying);

            Find("Canvas/Hud/Camera").onClick.Invoke();
            Assert.IsTrue(screen.PhotoMode);
            Assert.IsFalse(screen.PhotoAskFraming);
            screen.ExitPhotoMode();
        }

        [Test]
        public void A_Resting_Rabbit_In_Its_Seat_Can_Ask_And_The_Daily_Quest_Is_In_The_List()
        {
            Motion.SetReduced(true);
            runner.State.RestSpots = 2;
            Rabbit resting = Rabbits.Arrive(runner.State, 1, "Mây", 0d);
            screen.Shell.StartGame();
            screen.Refresh();
            Assert.IsTrue(runner.AskForPhoto(), "nobody works: the seated rabbit asks");
            Assert.AreEqual(resting.Id, runner.State.PhotoAskRabbit);
            screen.Advance(0.1f);
            Assert.IsTrue(screen.PhotoAskShown);
            Find("Canvas/Garden/Content/PhotoAsk").onClick.Invoke();
            Assert.IsTrue(screen.PhotoAskFraming);
            Assert.AreEqual(GardenScreen.ZoomLevel.Near, screen.Zoom);
            Assert.IsTrue(screen.AskingRabbitOnScreen.HasValue);
            screen.ExitPhotoMode();

            Transform quest = root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-anh");
            Assert.IsNotNull(quest, "the daily photo quest is in the list");
            Assert.AreEqual("0 / 3", quest.Find("Count").GetComponent<Text>().text);
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest ngay-cham"), "the taps daily stays beside it");
        }

        Transform ChatLine => root.transform.Find("Canvas/Garden/Content/ChatLine");

        void KeepTeapotAwake()
        {
            Transform z = root.transform.Find("Canvas/Garden/Content/Station am-tra/Doze");
            if (z != null && z.gameObject.activeSelf) Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
        }

        int BubblesOver(float seconds, List<float> stays = null, bool keepAwake = true, List<float> came = null)
        {
            int shown = 0;
            bool was = screen.ChatShown;
            float since = 0f;
            for (float t = 0f; t < seconds; t += 0.1f)
            {
                if (keepAwake) KeepTeapotAwake();
                screen.Advance(0.1f);
                since += 0.1f;
                bool now = screen.ChatShown;
                if (now && !was) { shown++; since = 0f; came?.Add(screen.Clock); }
                if (!now && was && stays != null) stays.Add(since);
                was = now;
            }
            return shown;
        }

        static List<string> Allowed(RabbitTrade trade, bool resting, double hour, GameRunner runner, bool boosted, bool dressed) =>
            Chatter.KeysFor(trade, resting, Daylight.PhaseAt(hour), Seasons.SeasonOf(runner.TodayNumber), boosted, dressed);

        [Test]
        public void After_Seventy_Seconds_A_Working_Rabbit_Has_Said_Exactly_One_Line_Of_Its_Trade_Or_The_Hour()
        {
            screen.Shell.StartGame();
            runner.State.OrderClock = 0d;
            screen.ClockHours = () => 13.0;
            Rabbit r = GiveWorker(1, 0);
            Assert.IsFalse(screen.ChatShown);
            var stays = new List<float>();
            var came = new List<float>();
            float start = screen.Clock;
            Assert.AreEqual(1, BubblesOver(70.5f, stays, true, came), "one line in the first seventy seconds");
            Assert.That(came[0] - start, Is.InRange(Chatter.WaitLeast - 0.2f, Chatter.WaitMost + 0.2f));
            Assert.AreEqual(1, screen.ChatLines);
            Assert.AreEqual(r.Id, screen.ChatRabbitId);
            CollectionAssert.Contains(Allowed(RabbitTrade.Brewer, false, 13.0, runner, Chatter.BoosterOf(runner.Defs, runner.State, 0) >= 0, false), screen.ChatKey);
            BubblesOver(3f, stays);
            Assert.AreEqual(1, stays.Count, "and it has gone again");
            Assert.AreEqual(Chatter.Seconds, stays[0], 0.15f, "two and a half seconds");
            Assert.AreEqual(1, CountNamed(root.transform.Find("Canvas/Garden/Content"), "ChatLine"), "one bubble, used again and again");

            BubblesOver(150f, null, true, came);
            Assert.GreaterOrEqual(came.Count, 3);
            for (int i = 1; i < came.Count; i++)
                Assert.That(came[i] - came[i - 1], Is.InRange(Chatter.WaitLeast - 0.2f, Chatter.WaitMost + 0.2f), "gap " + i);
        }

        [Test]
        public void A_Working_Rabbit_Says_Lines_Of_Its_Own_Trade_And_Of_The_Hour_Never_Of_Another()
        {
            screen.Shell.StartGame();
            runner.State.OrderClock = 0d;
            GiveWorker(1, 0);
            foreach (double hour in new[] { 13.0, 18.0, 22.0, 6.0 })
            {
                screen.ClockHours = () => hour;
                DayPhase phase = Daylight.PhaseAt(hour);
                List<string> allowed = Allowed(RabbitTrade.Brewer, false, hour, runner, Chatter.BoosterOf(runner.Defs, runner.State, 0) >= 0, false);
                bool trade = false, ofTheHour = false;
                for (int i = 0; i < 60; i++)
                {
                    screen.Advance(0.37f);
                    KeepTeapotAwake();
                    Assert.IsTrue(screen.ChatterNow(), "the brewer is there to talk");
                    string key = screen.ChatKey;
                    CollectionAssert.Contains(allowed, key, hour + "h");
                    Assert.AreEqual(key == Chatter.BoostKey ? L10n.T(key, L10n.Station(runner.Defs[Chatter.BoosterOf(runner.Defs, runner.State, 0)].Id)) : L10n.T(key),
                        ChatLine.Find("Text").GetComponent<Text>().text);
                    if (key.StartsWith("chatter.trade.Brewer.")) trade = true;
                    if (key.StartsWith("chatter.phase." + phase + ".")) ofTheHour = true;
                }
                Assert.IsTrue(trade, "it talks about brewing at " + hour + "h");
                Assert.IsTrue(ofTheHour, "and about the " + phase);
            }
        }

        [Test]
        public void A_Resting_Rabbit_Never_Says_A_Trade_Line_And_By_Night_Talks_Of_The_Night()
        {
            screen.Shell.StartGame();
            runner.State.OrderClock = 0d;
            screen.ClockHours = () => 22.0;
            screen.Refresh();
            Rabbit r = GiveWorker(1, -1);
            Assert.AreEqual(1, BubblesOver(70.5f, null, false), "a resting rabbit talks too");
            Assert.AreEqual(r.Id, screen.ChatRabbitId);
            bool night = false;
            for (int i = 0; i < 60; i++)
            {
                screen.Advance(0.37f);
                Assert.IsTrue(screen.ChatterNow());
                string key = screen.ChatKey;
                Assert.IsFalse(Chatter.IsTradeLine(key), "a resting rabbit never talks shop: " + key);
                CollectionAssert.Contains(Allowed(RabbitTrade.Brewer, true, 22.0, runner, false, false), key);
                if (key.StartsWith("chatter.phase.Night.")) night = true;
            }
            Assert.IsTrue(night, "it talks of the night");
        }

        [Test]
        public void An_Idle_Line_Waits_While_A_Guest_Is_In_The_Garden_And_Gives_Way_To_A_Bond_Line()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            Rabbit r = GiveWorker(1, 0);
            runner.State.Order = new Order { Id = 1, Guest = RabbitKinds.All[0].Id, Left = 170, RewardSeconds = Orders.RewardSeconds };
            runner.State.Order.Lines.Add(new OrderLine { Station = 0, Count = 1 });
            screen.Refresh();
            Assert.IsTrue(screen.GuestShown);
            Assert.IsFalse(screen.ChatterNow(), "not over a guest");
            Assert.AreEqual(0, BubblesOver(75f), "the line waits while the guest stands at the counter");
            Assert.IsTrue(screen.GuestShown);

            runner.State.Order = null;
            screen.Refresh();
            for (int i = 0; i < 100 && screen.GuestShown; i++) { KeepTeapotAwake(); screen.Advance(0.1f); }
            Assert.IsFalse(screen.GuestShown);
            screen.Advance(0.1f);
            Assert.IsTrue(screen.ChatShown, "the waiting line comes as soon as the guest has gone");
            Assert.AreEqual(1, screen.ChatLines);

            r.Affection = Rabbits.PointsFor(Rabbits.LineHearts) - Rabbits.PetPoints;
            r.PetDay = -1;
            Find("Canvas/Garden/Content/Station am-tra").onClick.Invoke();
            Transform bond = root.transform.Find("Canvas/Garden/Content/BondLine");
            Assert.IsTrue(bond.gameObject.activeSelf);
            screen.Advance(0.05f);
            Assert.IsFalse(screen.ChatShown, "the idle line gives way");
            Assert.IsFalse(screen.ChatterNow(), "and waits for the bond line");
            for (int i = 0; i < 60 && bond.gameObject.activeSelf; i++) screen.Advance(0.1f);
            Assert.IsFalse(bond.gameObject.activeSelf);
            KeepTeapotAwake();
            Assert.IsTrue(screen.ChatterNow(), "then it may talk again");
        }

        [Test]
        public void Under_Reduced_Motion_The_Line_Appears_And_Goes_Without_A_Pop()
        {
            screen.Shell.StartGame();
            runner.State.OrderClock = 0d;
            GiveWorker(1, 0);
            Assert.IsTrue(screen.ChatterNow());
            screen.Advance(0.05f);
            Assert.Less(ChatLine.localScale.x, 1f, "it springs in when motion is not reduced");

            Motion.SetReduced(true);
            screen.Advance(3f);
            Assert.IsFalse(screen.ChatShown);
            Assert.IsTrue(screen.ChatterNow());
            Assert.AreEqual(1f, ChatLine.localScale.x, 1e-4, "it is simply there");
            for (int i = 0; i < 24; i++)
            {
                screen.Advance(0.1f);
                Assert.AreEqual(1f, ChatLine.localScale.x, 1e-4, "no pop and no shrink at " + (i + 1) * 0.1f + "s");
            }
            screen.Advance(0.2f);
            Assert.IsFalse(screen.ChatShown, "it is simply gone");
        }

        [Test]
        public void A_Boosted_Rabbit_Names_Its_Booster_And_A_Dyed_One_Shows_Off()
        {
            screen.Shell.StartGame();
            runner.State.OrderClock = 0d;
            runner.State.Levels[1] = 3;
            Rabbit r = GiveWorker(1, 0);
            Assert.AreEqual(1, Chatter.BoosterOf(runner.Defs, runner.State, 0));
            var keys = new HashSet<string>();
            for (int i = 0; i < 200 && !keys.Contains(Chatter.BoostKey); i++)
            {
                screen.Advance(0.37f);
                KeepTeapotAwake();
                Assert.IsTrue(screen.ChatterNow());
                keys.Add(screen.ChatKey);
            }
            Assert.IsTrue(keys.Contains(Chatter.BoostKey), "the booster's line comes up");
            StringAssert.Contains(L10n.Station("gio-la"), ChatLine.Find("Text").GetComponent<Text>().text, "naming the leaf basket");
            Assert.IsFalse(keys.Contains(Chatter.DressedKey), "nothing to show off yet");

            r.Dye = 0;
            bool showedOff = false;
            for (int i = 0; i < 200 && !showedOff; i++)
            {
                screen.Advance(0.37f);
                KeepTeapotAwake();
                Assert.IsTrue(screen.ChatterNow());
                showedOff = screen.ChatKey == Chatter.DressedKey;
            }
            Assert.IsTrue(showedOff, "a dyed rabbit shows its coat off");
        }

        [Test]
        public void The_Album_Shows_Each_Set_With_Its_Progress_And_Outlines_A_Complete_One_In_Gold()
        {
            AlbumSet partial = AlbumSets.Find(3, RabbitTrade.Baker), done = AlbumSets.Find(0, RabbitTrade.Stargazer);
            Rabbits.See(runner.State, partial.Kinds[0].Id);
            Rabbits.See(runner.State, partial.Kinds[1].Id);
            foreach (RabbitKind k in done.Kinds) Rabbits.See(runner.State, k.Id);
            screen.OpenSheet(3);
            screen.ShowRabbitAlbum(true);
            screen.Refresh();
            Transform content = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content");
            foreach (AlbumSet set in AlbumSets.All) Assert.IsNotNull(content.Find("Set " + set.Key), set.Key + " has its group");

            Transform half = content.Find("Set " + partial.Key);
            Assert.AreEqual(L10n.T("album.set", L10n.Trade(RabbitTrade.Baker), 2, partial.Kinds.Count, "3"), half.Find("Line").GetComponent<Text>().text);
            StringAssert.StartsWith("Bộ Bếp bánh 2/" + partial.Kinds.Count + " · +3% khi đủ", half.Find("Line").GetComponent<Text>().text);
            Assert.IsNull(half.Find("Outline"), "not complete: no gold");
            foreach (RabbitKind k in partial.Kinds) Assert.IsNotNull(half.Find("Kind " + k.Id), k.Id + " sits in its set");

            Transform full = content.Find("Set " + done.Key);
            Assert.IsNotNull(full.Find("Outline"), "complete: outlined in gold");
            Assert.AreEqual(Palette.Lantern, full.Find("Outline").GetComponent<Image>().color);
            Assert.AreEqual(L10n.T("album.set.done", L10n.Trade(RabbitTrade.Stargazer), "3"), full.Find("Line").GetComponent<Text>().text);
            Assert.IsNull(content.Find("FullFrame"), "the gold frame waits for every kind");
        }

        [Test]
        public void A_Set_Just_Completed_Raises_Its_Banner_Once()
        {
            AlbumSet set = AlbumSets.Find(3, RabbitTrade.Brewer);
            for (int i = 0; i < set.Kinds.Count - 1; i++) Rabbits.See(runner.State, set.Kinds[i].Id);
            runner.AutoAssignRabbits();
            Text title = root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>();
            title.text = "";
            Rabbits.See(runner.State, set.Kinds[set.Kinds.Count - 1].Id);
            runner.AutoAssignRabbits();
            Assert.AreEqual("Đủ bộ Pha chế bậc Tím!", title.text);
            Assert.AreEqual(AlbumBanner.SetTitle(set), title.text);
            Assert.IsTrue(screen.Feel.BannerVisible);
            StringAssert.Contains("3%", root.transform.Find("Canvas/Feel/Banner/Sub").GetComponent<Text>().text);
            title.text = "";
            runner.AutoAssignRabbits();
            Assert.AreEqual("", title.text, "a set already complete says nothing again");
        }

        [Test]
        public void Meeting_Forty_Kinds_Gives_The_Starry_Hat_And_Seventy_Two_The_Gold_Frame()
        {
            int n = 0;
            foreach (RabbitKind k in RabbitKinds.All)
                if (k.Season == Season.None && !k.NewMoonOnly && n < 40) { Rabbits.See(runner.State, k.Id); n++; }
            runner.AutoAssignRabbits();
            Assert.IsTrue(Items.Owns(runner.State, AlbumSets.MilestoneHat));
            Assert.AreEqual(L10n.T("album.milestone.title", 40), root.transform.Find("Canvas/Feel/Banner/Title").GetComponent<Text>().text);
            screen.OpenSheet(2);
            screen.Refresh();
            Assert.IsNotNull(root.transform.Find("Canvas/Sheet/Panel/Quests/Content/Quest loai"), "the milestone is a tier of the kinds row (#111)");

            foreach (RabbitKind k in RabbitKinds.All) if (k.Season == Season.None && !k.NewMoonOnly) Rabbits.See(runner.State, k.Id);
            runner.AutoAssignRabbits();
            Assert.IsTrue(AlbumSets.HasFullFrame(runner.State));
            Assert.AreEqual(1, runner.State.Items.FindAll(i => i.DefId == AlbumSets.MilestoneHat).Count, "one hat, given once");
            screen.OpenSheet(3);
            screen.ShowRabbitAlbum(true);
            screen.Refresh();
            Transform frame = root.transform.Find("Canvas/Sheet/Panel/Rabbits/Content/FullFrame");
            Assert.IsNotNull(frame, "the gold frame tops the album");
            StringAssert.Contains("Đủ bộ", frame.Find("Text").GetComponent<Text>().text);
        }

        [Test]
        public void The_Purse_Card_Opens_A_Moon_Jar_From_Garden_Level_Three_And_The_Carrots_Fly_To_The_Purse()
        {
            screen.Shell.StartGame();
            runner.State.MoonJars = 2;
            screen.Refresh();
            Find("Canvas/Hud/Purse").onClick.Invoke();
            Assert.IsTrue(screen.Income.IsOpen);
            Assert.IsFalse(root.transform.Find("Canvas/Income/Card/Jars").gameObject.activeSelf, "a new player does not see the jars");
            Assert.IsFalse(root.transform.Find("Canvas/Hud/Jars").gameObject.activeSelf);
            screen.Income.Close();

            GrowToLevel(MoonJars.ShowLevel);
            Find("Canvas/Hud/Purse").onClick.Invoke();
            Text label = root.transform.Find("Canvas/Income/Card/Jars/Open/Face/Text").GetComponent<Text>();
            Assert.IsTrue(root.transform.Find("Canvas/Income/Card/Jars").gameObject.activeSelf);
            Assert.AreEqual(L10n.T("income.jar", 2), label.text);
            BigNum value = runner.MoonJarValue;
            Assert.AreEqual(L10n.T("income.jar.sub", value.Short(), 2, MoonJars.Cap), root.transform.Find("Canvas/Income/Card/Jars/Sub").GetComponent<Text>().text);
            Assert.IsTrue(root.transform.Find("Canvas/Hud/Jars").gameObject.activeSelf, "the jars on the shelf show under the purse");
            Assert.AreEqual("2", root.transform.Find("Canvas/Hud/Jars/Text").GetComponent<Text>().text);
            Assert.AreEqual((Economy.IncomePerSecond(runner.Defs, runner.State) * (runner.Mods.IncomeMultiplier * runner.Mods.SpeedMultiplier * 3600d)).ToDouble(), value.ToDouble(), 1e-6);

            BigNum before = runner.State.Coins;
            int flying = screen.Feel.Flying;
            Find("Canvas/Income/Card/Jars/Open").onClick.Invoke();
            Assert.AreEqual((before + value).ToDouble(), runner.State.Coins.ToDouble(), 1e-6, "an hour of income");
            Assert.AreEqual(1, runner.State.MoonJars);
            Assert.Greater(screen.Feel.Flying, flying, "the carrots fly from the button to the purse");
            Assert.AreEqual(L10n.T("income.jar", 1), label.text, "the card counts down at once");
            screen.Refresh();
            Assert.AreEqual("1", root.transform.Find("Canvas/Hud/Jars/Text").GetComponent<Text>().text);

            Find("Canvas/Income/Card/Jars/Open").onClick.Invoke();
            Assert.AreEqual(0, runner.State.MoonJars);
            screen.Refresh();
            Assert.AreEqual(L10n.T("income.jar.none"), label.text);
            Assert.IsFalse(Find("Canvas/Income/Card/Jars/Open").interactable, "nothing to open");
            Assert.IsFalse(root.transform.Find("Canvas/Hud/Jars").gameObject.activeSelf, "no jars, no chip");
        }

        [Test]
        public void The_Shop_Shelf_Sells_A_Jar_On_The_Second_Tap_Three_A_Day_And_Not_Onto_A_Full_Shelf()
        {
            screen.Shell.StartGame();
            runner.State.GoldenCarrots = 100;
            screen.OpenShop();
            screen.Refresh();
            Assert.IsFalse(root.transform.Find("Canvas/Shop/Card/Decor/Row/Jar").gameObject.activeSelf, "not before garden level 3");
            screen.Shop.Close();

            GrowToLevel(MoonJars.ShowLevel);
            screen.OpenShop();
            screen.Refresh();
            string jar = "Canvas/Shop/Card/Decor/Row/Jar";
            Text price = root.transform.Find(jar + "/Price/Text").GetComponent<Text>();
            Text left = root.transform.Find(jar + "/Left/Text").GetComponent<Text>();
            Assert.IsTrue(root.transform.Find(jar).gameObject.activeSelf);
            Assert.AreEqual(MoonJars.Price.ToString(), price.text);
            Assert.AreEqual(L10n.T("shop.jar.left", 3, 3), left.text);
            for (int n = 1; n <= MoonJars.BuyPerDay; n++)
            {
                Find(jar).onClick.Invoke();
                Assert.AreEqual(n - 1, runner.State.MoonJars, "the first tap only asks");
                Assert.AreEqual(L10n.T("shop.hats.confirm"), price.text);
                Find(jar).onClick.Invoke();
                Assert.AreEqual(n, runner.State.MoonJars);
                Assert.AreEqual(100 - n * MoonJars.Price, runner.State.GoldenCarrots);
                screen.Refresh();
                Assert.AreEqual(L10n.T("shop.jar.left", 3 - n, 3), left.text);
            }
            Assert.AreEqual(runner.TodayNumber, runner.State.MoonJarBuyDay, "counted by the clamped day");
            Assert.AreEqual(L10n.T("shop.jar.soldout"), price.text);
            Assert.IsFalse(Find(jar).interactable, "three a day");

            runner.State.MoonJarBuyDay = runner.TodayNumber - 1;
            runner.State.MoonJars = MoonJars.Cap;
            screen.Refresh();
            Assert.AreEqual(L10n.T("shop.jar.full"), price.text);
            Assert.IsFalse(Find(jar).interactable);
        }

        [Test]
        public void From_Garden_Level_Fifteen_The_Daily_Harvest_Shows_A_Moon_Jar_In_Place_Of_Its_Clover_And_The_Clover_Once_The_Days_Jar_Has_Come()
        {
            GameState s = runner.State;
            s.TotalEarned = s.DailyEarnedBase + s.DailyCarrotTarget * 2d;
            screen.Refresh();
            string row = "Canvas/Sheet/Panel/Quests/Content/Quest ngay-thu";
            Assert.IsFalse(root.transform.Find(row + "/Jars").gameObject.activeSelf, "the harvest's jar waits for garden level 15");
            Assert.IsTrue(root.transform.Find(row + "/Clovers").gameObject.activeSelf, "until then it pays its clover");

            GrowToLevel(MoonJars.DailyLevel);
            s.TotalEarned = s.DailyEarnedBase + s.DailyCarrotTarget * 2d;
            screen.Refresh();
            Assert.IsTrue(root.transform.Find(row + "/Jars").gameObject.activeSelf, "the jar in the clover's place");
            Assert.IsFalse(root.transform.Find(row + "/Clovers").gameObject.activeSelf);
            Assert.AreEqual("+1", root.transform.Find(row + "/Jars/Text").GetComponent<Text>().text);

            s.MoonJarFreeDay = s.DailyDay;
            screen.Refresh();
            Assert.IsFalse(root.transform.Find(row + "/Jars").gameObject.activeSelf);
            Assert.IsTrue(root.transform.Find(row + "/Clovers").gameObject.activeSelf, "the clover it replaced");
            int clovers = s.Clovers;
            Find(row + "/Claim").onClick.Invoke();
            Assert.AreEqual(clovers + 1, s.Clovers);
            Assert.AreEqual(0, s.MoonJars);
        }

        [Test]
        public void Every_Station_Seat_Prop_And_Decoration_Spot_Stands_On_Its_Own_Tile_Of_The_Grid()
        {
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            GardenGrid grid = runner.State.Grid;
            var taken = new Dictionary<Hex, string>();
            void Claim(Hex tile, string who)
            {
                Assert.IsFalse(taken.ContainsKey(tile), who + " shares tile " + tile + " with " + (taken.ContainsKey(tile) ? taken[tile] : ""));
                taken[tile] = who;
            }
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Hex tile = grid.TileOf(GardenGrid.StationId(runner.Defs[i])) ?? GardenGrid.DefaultStationTile(i);
                if (i < runner.State.Plots) Assert.AreEqual(tile, grid.TileOf(GardenGrid.StationId(runner.Defs[i])), "a plot's station is on the grid");
                Vector2 at = ((RectTransform)world.Find("Station " + runner.Defs[i].Id)).anchoredPosition;
                Assert.AreEqual(GardenScreen.CellToWorld(tile), at, "station " + i + " stands on its tile");
                Assert.AreEqual(at, screen.StationSpot(i));
                Claim(tile, "station " + i);
            }
            for (int k = 0; k < RestSpots.Max; k++)
            {
                Hex tile = grid.TileOf(GardenGrid.RestId(k)) ?? GardenGrid.DefaultRestTile(k);
                Assert.AreEqual(GardenScreen.CellToWorld(tile), screen.RestSpot(k), "seat " + k);
                Vector2 seat = ((RectTransform)world.Find("Seat " + k)).anchoredPosition;
                Assert.Less(Vector2.Distance(seat, screen.RestSpot(k)), 80f, "the rabbit sits on its seat's tile, lifted onto the furniture at most");
                Claim(tile, "seat " + k);
            }
            Assert.AreEqual(GardenScreen.CellToWorld(grid.TileOf(GardenGrid.MortarId).Value), screen.MortarSpot);
            Assert.AreEqual(screen.MortarSpot + new Vector2(0, 80f), ((RectTransform)world.Find("Mortar")).anchoredPosition, "the mortar on its tile");
            Claim(grid.TileOf(GardenGrid.MortarId).Value, "mortar");
            Assert.AreEqual(GardenScreen.CellToWorld(grid.TileOf(GardenGrid.ShopId).Value), screen.ShopSpot);
            var shop = (RectTransform)world.Find("Shop");
            Assert.AreEqual(screen.ShopSpot.x, shop.anchoredPosition.x, 0.5f, "the shop on its tile");
            Assert.AreEqual(screen.ShopSpot.y + shop.rect.height / 2f, shop.anchoredPosition.y, 0.5f, "standing on the tile's foot");
            Claim(grid.TileOf(GardenGrid.ShopId).Value, "shop");
            Assert.AreEqual(GardenScreen.CellToWorld(grid.TileOf(GardenGrid.CounterId).Value), screen.GuestSpot, "the guest's counter on its tile");
            Claim(grid.TileOf(GardenGrid.CounterId).Value, "counter");
            for (int i = 0; i < Decor.Spots; i++)
            {
                Hex tile = grid.TileOf(GardenGrid.DecorId(i)) ?? GardenGrid.DefaultDecorTile(i);
                Assert.AreEqual(GardenScreen.CellToWorld(tile), screen.DecorSpotAt(i));
                Assert.AreEqual(screen.DecorSpotAt(i), ((RectTransform)world.Find("Spot " + i)).anchoredPosition, "decoration spot " + i + " on its tile");
                Claim(tile, "decor " + i);
            }
            Assert.AreEqual(runner.Defs.Count + RestSpots.Max + 3 + Decor.Spots, taken.Count, "one tile each, nothing shared");
            Assert.AreEqual(GardenScreen.CellToWorld(new Hex(0, 0)), screen.StationSpot(0), "the teapot is the heart of the grid");
            Assert.AreEqual(GardenScreen.CellToWorld(new Hex(1, 0)).x - GardenScreen.CellToWorld(new Hex(0, 0)).x, (float)GardenGrid.PitchX, 0.01f);
        }

        [Test]
        public void A_Garden_With_Every_Station_Open_Has_Everything_On_The_Grid_Where_The_Screen_Shows_It()
        {
            GameState s = runner.State;
            s.Plots = runner.Defs.Count;
            for (int i = 0; i < s.Levels.Length; i++) if (s.Levels[i] <= 0) s.Levels[i] = 1;
            s.RestSpots = RestSpots.Max;
            GardenGrid.Settle(runner.Defs, s);
            screen.Refresh();
            Assert.AreEqual(GardenGrid.SegmentsForPlots(runner.Defs.Count), s.Grid.Segments);
            Assert.AreEqual(runner.Defs.Count + RestSpots.Max + 3 + 2 + GardenGrid.ShopRoofIds.Length, s.Grid.OccupantCount, "every station, every seat, the mortar, the shop, the counter, the leaf basket and its plate, and the tiles under the shop's roof");
            var world = (RectTransform)root.transform.Find("Canvas/Garden/Content");
            var tiles = new HashSet<Hex>();
            foreach (KeyValuePair<string, Hex> o in s.Grid.Occupants)
            {
                Assert.IsTrue(tiles.Add(o.Value), o.Key + " has a tile of its own");
                Assert.IsTrue(s.Grid.InLawn(o.Value), o.Key + " stands on the lawn");
            }
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Hex tile = s.Grid.TileOf(GardenGrid.StationId(runner.Defs[i])).Value;
                Assert.AreEqual(GardenScreen.CellToWorld(tile), ((RectTransform)world.Find("Station " + runner.Defs[i].Id)).anchoredPosition, runner.Defs[i].Id);
                Assert.IsTrue(world.Find("Station " + runner.Defs[i].Id).gameObject.activeSelf, runner.Defs[i].Id + " is open and shown");
            }
            for (int k = 0; k < RestSpots.Max; k++) Assert.IsNotNull(s.Grid.TileOf(GardenGrid.RestId(k)), "seat " + k);
        }

        [Test]
        public void A_Station_Moved_On_The_Grid_Is_Looked_Up_Where_It_Moved_To()
        {
            GameState s = runner.State;
            Hex home = s.Grid.TileOf(GardenGrid.StationId(runner.Defs[1])).Value;
            Assert.AreEqual(GardenScreen.CellToWorld(home), screen.StationSpot(1));
            Assert.IsTrue(s.Grid.NearestEmpty(out Hex empty));
            Assert.IsTrue(s.Grid.Place(GardenGrid.StationId(runner.Defs[1]), empty.Q, empty.R));
            Assert.AreEqual(GardenScreen.CellToWorld(empty), screen.StationSpot(1), "the screen reads the grid, not a table");
            Assert.AreEqual(TileState.UnlockedEmpty, s.Grid.Tile(home.Q, home.R).State, "its old tile is free");
            Assert.AreEqual(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(runner.Defs.Count)), screen.StationSpot(runner.Defs.Count), "a station beyond the catalog still has a place");
        }

        RectTransform World => (RectTransform)root.transform.Find("Canvas/Garden/Content");
        Vector2 OnView(Vector2 garden) => garden * World.localScale.x + World.anchoredPosition;

        static Vector2 Teapot => GardenScreen.CellToWorld(new Hex(0, 0));

        Vector2 ShownX() => new Vector2(-World.anchoredPosition.x / World.localScale.x, (1080f - World.anchoredPosition.x) / World.localScale.x);

        [Test]
        public void The_Strip_Drags_Sideways_With_Inertia_And_Springs_Back_Inside_The_Open_Lawn()
        {
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            var scroll = World.parent.GetComponent<ScrollRect>();
            Assert.IsTrue(scroll.horizontal, "dragged sideways");
            Assert.IsTrue(scroll.inertia, "and it carries on a little when let go");
            Assert.IsTrue(scroll.vertical, "at the usual zoom the map is taller than the screen: it drags up and down too (#91)");
            foreach (float push in new[] { -2400f, 2400f })
            {
                screen.SimulateGardenDrag(new Vector2(push, 0f));
                for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);
                Vector2 span = screen.OpenSpan(), shown = ShownX();
                Assert.GreaterOrEqual(shown.x, span.x - 1f, "let go past the left end, it comes back inside the open lawn and its margin");
                Assert.LessOrEqual(shown.y, span.y + 1f, "and past the right end too");
            }
            Vector2 before = screen.OpenSpan();
            Assert.AreEqual(Teapot.x - 600f - GardenScreen.ViewMargin, before.x, 1f);
            HavePlots(runner.Defs.Count);
            Vector2 after = screen.OpenSpan();
            Assert.Less(after.x, before.x - 1000f);
            Assert.Greater(after.y, before.y + 1000f);
            screen.SimulateGardenDrag(new Vector2(-1500f, 0f));
            for (int i = 0; i < 60; i++) screen.Advance(1f / 30f);
            Assert.Greater(ShownX().y, before.y + 400f, "with every segment open the view goes further right");
            screen.SetZoom(GardenScreen.ZoomLevel.Near, false);
            Assert.IsTrue(scroll.vertical, "close in, the strip is taller than the screen and scrolls up and down too");
        }

        [Test]
        public void The_Home_Button_Glides_Back_To_The_Tea_Shop()
        {
            HavePlots(runner.Defs.Count);
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            screen.LookAt(Teapot + new Vector2(1700f, 0f));
            screen.Advance(1f / 30f);
            GameObject home = root.transform.Find("Canvas/Home").gameObject;
            Assert.IsTrue(home.activeSelf, "far to the right the way home shows");
            Find("Canvas/Home").onClick.Invoke();
            screen.Advance(1f / 30f);
            Assert.AreNotEqual(540f, OnView(Teapot).x, "it glides, it does not jump");
            for (int i = 0; i < 40; i++) screen.Advance(1f / 30f);
            Assert.That(OnView(Teapot).x, Is.InRange(200f, 880f), "the teapot is back in the middle");
            Assert.That(OnView(screen.ShopSpot).x, Is.InRange(100f, 980f), "with the tea shop above it");
            Assert.That(OnView(screen.ShopSpot).y, Is.LessThan(1920f - 160f), "under the HUD");
            Assert.AreEqual(GardenScreen.ZoomLevel.Default, screen.Zoom, "at the zoom it had");
            Assert.IsFalse(home.activeSelf, "home: the button goes");
        }

        [Test]
        public void A_Row_Tap_And_A_Card_Swipe_Bring_A_Station_At_Either_End_Into_View()
        {
            HavePlots(runner.Defs.Count);
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = 1;
            screen.Shell.StartGame();
            screen.Refresh();
            screen.OpenSheet(0);
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            foreach (int index in new[] { 15, 14, 0 })
            {
                Find("Canvas/Sheet/Panel/Scroll/Content/Row " + runner.Defs[index].Id).onClick.Invoke();
                for (int i = 0; i < 12; i++) screen.Advance(1f / 30f);
                Vector2 at = OnView(screen.StationSpot(index));
                Assert.That(at.x, Is.InRange(200f, 880f), "station " + index + " glided to the middle of the screen sideways");
                Assert.Greater(at.y, screen.SheetHeight + 60f, "and above the sheet");
            }
            screen.CloseSheet();
            for (int i = 0; i < 20; i++) screen.Advance(1f / 30f);
            screen.FocusStation(4);
            var swipe = root.transform.Find("Canvas/Card/Face").GetComponent<CardSwipe>();
            var left = new UnityEngine.EventSystems.PointerEventData(null) { pressPosition = new Vector2(600f, 300f), position = new Vector2(380f, 310f) };
            for (int next = 5; next <= 9; next++)
            {
                swipe.OnEndDrag(left);
                Assert.AreEqual(next, screen.Selected);
                Vector2 at = OnView(screen.StationSpot(next));
                Assert.That(at.x, Is.InRange(0f, 1080f), "the card's station " + next + " is on the screen, wherever on the strip it stands");
            }
        }

        [Test]
        public void Side_Pills_Count_The_Stations_Off_To_Either_Side_And_A_Touch_Flies_There()
        {
            Motion.SetReduced(true);
            HavePlots(runner.Defs.Count);
            for (int i = 0; i < runner.Defs.Count; i++) runner.State.Levels[i] = 1;
            runner.State.Coins = new BigNum(1e300);
            screen.Shell.StartGame();
            screen.Refresh();
            screen.LookAt(Teapot);
            screen.Advance(1f / 30f);
            GameObject leftPill = root.transform.Find("Canvas/EdgeLeft").gameObject, rightPill = root.transform.Find("Canvas/EdgeRight").gameObject;
            Assert.IsTrue(leftPill.activeSelf, "stations wait off to the left");
            Assert.IsTrue(rightPill.activeSelf, "and to the right");
            string leftText = leftPill.transform.Find("Text").GetComponent<Text>().text, rightText = rightPill.transform.Find("Text").GetComponent<Text>().text;
            StringAssert.StartsWith("◀ ", leftText);
            StringAssert.EndsWith(" ▶", rightText);
            int offLeft = 0, offRight = 0, offUp = 0;
            for (int i = 0; i < runner.Defs.Count; i++)
            {
                Vector2 on = OnView(screen.StationSpot(i));
                float x = on.x - 540f, y = on.y - 960f;
                if (Mathf.Abs(y) >= 960f + 160f) { if (y > 0f) offUp++; continue; }
                if (x <= -540f - 120f) offLeft++;
                if (x >= 540f + 120f) offRight++;
            }
            Assert.AreEqual(offLeft, int.Parse(leftText.Substring(2)), "the left pill counts the stations off to the left");
            Assert.AreEqual(offRight, int.Parse(rightText.Substring(0, rightText.Length - 2)), "and the right one those off to the right");
            GameObject upPill = root.transform.Find("Canvas/EdgeUp").gameObject;
            Assert.AreEqual(offUp > 0, upPill.activeSelf, "the stations of the band up wait above, if any");
            Find("Canvas/EdgeRight").onClick.Invoke();
            screen.Advance(1f / 30f);
            int now = rightPill.activeSelf ? int.Parse(rightPill.transform.Find("Text").GetComponent<Text>().text.Split(' ')[0]) : 0;
            Assert.Less(now, offRight, "the touch flew to the nearest station on the right, now in view");
        }

        [Test]
        public void Under_Reduced_Motion_The_Far_Wood_Keeps_With_The_Ground()
        {
            Motion.SetReduced(true);
            HavePlots(runner.Defs.Count);
            var canopy = (RectTransform)root.transform.Find("Canvas/Garden/Canopy");
            screen.LookAt(new Vector2(800f, 1100f));
            screen.Advance(0f);
            Vector2 street0 = World.anchoredPosition, canopy0 = canopy.anchoredPosition;
            screen.LookAt(new Vector2(4000f, 1100f));
            screen.Advance(0f);
            Assert.Greater(Mathf.Abs(World.anchoredPosition.x - street0.x), 500f, "the view really moved");
            Assert.AreEqual(World.anchoredPosition.x - street0.x, canopy.anchoredPosition.x - canopy0.x, 0.5f, "no parallax: the wood moves with the ground");
            Assert.IsNull(canopy.Find("Forest Sign"), "the sign stands on the ground, where it reads");
            Assert.IsNull(canopy.Find("Rim Tree"), "and so does the rim at the lawn's end");
        }

        [Test]
        public void Mist_Lies_On_Exactly_The_Segments_Still_To_Open()
        {
            Motion.SetReduced(true);
            int last = Expansion.LastSegments(runner.Defs);
            for (int plots = Plots.Starting; plots <= runner.Defs.Count; plots += 2)
            {
                HavePlots(plots);
                int open = Expansion.Segments(runner.State);
                for (int n = 1; n <= last; n++)
                {
                    CanvasGroup mist = screen.MistOf(n);
                    Assert.IsNotNull(mist, "segment " + n + " has its mist");
                    Assert.AreEqual(n > open, mist.gameObject.activeSelf, "segment " + n + " with " + open + " open: mist only while it is shut");
                    if (!mist.gameObject.activeSelf) continue;
                    Vector2 span = GardenScreen.SegmentSpan(n);
                    var washes = new List<Rect>();
                    foreach (RectTransform wash in mist.transform)
                    {
                        Vector2 c = wash.anchoredPosition;
                        Assert.That(c.x, Is.InRange(span.x, span.y), "every wash of segment " + n + " is centred on its own land");
                        washes.Add(new Rect(c - wash.sizeDelta * 0.4f, wash.sizeDelta * 0.8f));
                    }
                    foreach (Hex h in GardenGrid.SegmentTiles(n))
                    {
                        Vector2 at = GardenScreen.CellToWorld(h);
                        bool covered = false;
                        foreach (Rect r in washes) covered |= r.Contains(at);
                        Assert.IsTrue(covered, h + " of segment " + n + " lies under its mist");
                    }
                }
            }
            Assert.IsFalse(screen.MistOf(1).gameObject.activeSelf);
        }

        [Test]
        public void Opening_A_Segment_Clears_Its_Mist_Over_A_Moment()
        {
            screen.Shell.StartGame();
            HavePlots(Plots.Starting);
            for (int i = 0; i < 5; i++) screen.Advance(0.1f);
            CanvasGroup mist = screen.MistOf(1);
            Assert.IsTrue(mist.gameObject.activeSelf);
            HavePlots(Plots.Starting + 2);
            for (float t = 0f; t < GardenScreen.RimStep / 3f + GardenScreen.MistClear * 0.5f; t += 0.05f) screen.Advance(0.05f);
            Assert.IsTrue(mist.gameObject.activeSelf, "halfway it is still thinning");
            Assert.That(mist.alpha, Is.InRange(0.05f, 0.95f));
            for (int i = 0; i < 30; i++) screen.Advance(0.1f);
            Assert.IsFalse(mist.gameObject.activeSelf, "then it is gone");
            Assert.IsTrue(screen.MistOf(2).gameObject.activeSelf, "the next segment's mist stays");
        }

        [Test]
        public void The_Pond_s_Plate_Reads_In_The_Opening_View_On_The_Bank_Towards_The_Teapot()
        {
            runner.State.GardenXp = Garden.XpForLevel(5);
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            screen.Refresh();
            var plate = (RectTransform)World.Find("PondTap/Plate");
            Assert.IsTrue(plate.gameObject.activeInHierarchy, "the plate is out");
            var corners = new Vector3[4];
            plate.GetWorldCorners(corners);
            var view = (RectTransform)World.parent;
            Vector2 a = (Vector2)view.InverseTransformPoint(corners[0]) - view.rect.min, b = (Vector2)view.InverseTransformPoint(corners[2]) - view.rect.min;
            Assert.That(Mathf.Min(a.x, b.x), Is.GreaterThanOrEqualTo(0f), "inside the left edge of the opening view");
            Assert.That(Mathf.Max(a.x, b.x), Is.LessThanOrEqualTo(1080f));
            Assert.That(Mathf.Min(a.y, b.y), Is.GreaterThan(196f), "above the tab bar");
            Assert.That(Mathf.Max(a.y, b.y), Is.LessThan(1920f - 160f), "under the HUD");
            Vector2 centre = plate.anchoredPosition + ((RectTransform)plate.parent).anchoredPosition;
            Assert.Greater(centre.x, GardenScreen.PondSpot.x, "on the right bank");
            Assert.IsTrue(GardenGrid.IsReserved(GardenScreen.WorldToCell(centre)), "and its tile is kept clear");
        }

        [Test]
        public void A_Guest_Walks_To_The_Counter_Along_The_Lawn_Away_From_The_Pond()
        {
            for (float t = 0f; t <= 1.001f; t += 0.02f)
            {
                Vector2 at = screen.GuestPathAt(t);
                Assert.GreaterOrEqual(Vector2.Distance(at, GardenScreen.PondSpot), 330f, "at " + t + " the guest is clear of the pond");
                Assert.That(at.y, Is.InRange(Teapot.y - 700f, Teapot.y + 700f), "on the strip's rows");
            }
            Assert.Greater(screen.GuestEntry.x, screen.GuestSpot.x, "in from the right, the side away from the pond");
        }

        [Test]
        public void Strollers_Go_Only_To_Empty_Tiles_Of_Open_Land()
        {
            GameState s = runner.State;
            int before = screen.LawnSpots.Count;
            Assert.GreaterOrEqual(before, 3, "the middle of the strip has room to stroll");
            foreach (int plots in new[] { Plots.Starting, runner.Defs.Count })
            {
                HavePlots(plots);
                foreach (Vector2 spot in screen.LawnSpots)
                {
                    Hex h = GardenScreen.WorldToCell(spot);
                    Assert.AreEqual(GardenScreen.CellToWorld(h), spot, "a spot is a tile's point");
                    Assert.That(GardenGrid.SegmentOf(h), Is.InRange(0, Expansion.Segments(s)), spot + " is on open land");
                    Assert.IsNull(s.Grid.OccupantAt(h.Q, h.R), spot + " is an empty tile");
                    Assert.IsFalse(GardenGrid.IsReserved(h), spot + " is not kept for a prop");
                }
            }
            Assert.Greater(screen.LawnSpots.Count, before, "every segment open offers more");
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(6)]
        public void A_Guest_Walks_To_The_Counter_Only_Over_Open_Land(int segments)
        {
            HavePlots(Expansion.PlotsForSegments(runner.Defs, segments));
            Assert.AreEqual(segments, Expansion.Segments(runner.State));
            for (float t = 0f; t <= 1.001f; t += 0.02f)
            {
                Vector2 at = screen.GuestPathAt(t);
                int segment = GardenGrid.SegmentOf(GardenScreen.WorldToCell(at));
                Assert.That(segment, Is.InRange(0, segments), "with " + segments + " open, at " + t + " the guest at " + at + " is on open land, not in the mist of segment " + segment);
            }
            Assert.Greater(screen.GuestEntry.x - screen.GuestSpot.x, 200f, "it still walks a little way in from the right");
        }

        Rect PondPlateOnScreen()
        {
            var plate = (RectTransform)World.Find("PondTap/Plate");
            Assert.IsTrue(plate.gameObject.activeInHierarchy, "the plate is out");
            var corners = new Vector3[4];
            plate.GetWorldCorners(corners);
            var view = (RectTransform)World.parent;
            Vector2 a = (Vector2)view.InverseTransformPoint(corners[0]) - view.rect.min, b = (Vector2)view.InverseTransformPoint(corners[2]) - view.rect.min;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        void AssertPondPlateReads(string where)
        {
            Rect r = PondPlateOnScreen();
            Assert.GreaterOrEqual(r.xMin, 24f, where + ": the plate " + r + " keeps 24 from the left edge");
            Assert.LessOrEqual(r.xMax, 1080f - 24f, where + ": and from the right edge");
        }

        [Test]
        public void The_Pond_s_Plate_Keeps_24_From_The_Edge_In_The_Opening_View_And_After_The_First_Segment_Opens()
        {
            Motion.SetReduced(true);
            GameState s = runner.State;
            screen.Shell.StartGame();
            for (int i = 0; i < 45; i++) screen.Advance(1f / 30f);
            screen.Refresh();
            AssertPondPlateReads("the opening view");
            screen.LookAt(GardenScreen.PondSpot + new Vector2(-120f, -300f));
            AssertPondPlateReads("looking at the pond");
            screen.LookAt(Teapot);
            AssertPondPlateReads("looking at the teapot");

            MistSegment next = Expansion.Next(runner.Defs, s);
            s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(next.NeedLevel));
            s.Coins = Expansion.CostOfNext(runner.Defs, s) + new BigNum(1);
            screen.Refresh();
            screen.LookAt(screen.ForestSignSpot + new Vector2(0f, 120f));
            AssertPondPlateReads("looking at the sign");
            Find("Canvas/Garden/Content/Forest Sign/Board").onClick.Invoke();
            screen.Refresh();
            for (int i = 0; i < 10; i++) screen.Advance(1f / 30f);
            Assert.AreEqual(1, Expansion.Segments(s), "the first segment came out of the mist");
            AssertPondPlateReads("after the first segment opens");
        }

        readonly Dictionary<Texture, Texture2D> _readable = new Dictionary<Texture, Texture2D>();

        float AlphaOf(Sprite sprite, Vector2 uv)
        {
            if (sprite == null) return 1f;
            if (!_readable.TryGetValue(sprite.texture, out Texture2D copy))
            {
                if (sprite.texture.isReadable) copy = sprite.texture;
                else
                {
                    copy = new Texture2D(2, 2);
                    copy.LoadImage(System.IO.File.ReadAllBytes(AssetDatabase.GetAssetPath(sprite.texture)));
                }
                _readable[sprite.texture] = copy;
            }
            Rect r = sprite.textureRect;
            float w = sprite.texture.width, h = sprite.texture.height;
            return copy.GetPixelBilinear((r.x + uv.x * r.width) / w, (r.y + uv.y * r.height) / h).a;
        }

        float MistCoverAt(Vector2 at)
        {
            float clear = 1f;
            for (int n = 1; n <= Expansion.LastSegments(runner.Defs); n++)
            {
                CanvasGroup mist = screen.MistOf(n);
                if (mist == null || !mist.gameObject.activeInHierarchy) continue;
                foreach (Image wash in mist.GetComponentsInChildren<Image>(false))
                {
                    RectTransform rt = wash.rectTransform;
                    var c = new Vector3[4];
                    rt.GetWorldCorners(c);
                    Vector2 a = World.InverseTransformPoint(c[0]), b = World.InverseTransformPoint(c[2]);
                    Rect r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                    if (!r.Contains(at)) continue;
                    Vector2 uv = new Vector2((at.x - r.xMin) / r.width, (at.y - r.yMin) / r.height);
                    clear *= 1f - mist.alpha * wash.color.a * AlphaOf(wash.sprite, uv);
                }
            }
            return 1f - clear;
        }

        public const float MistEdgeLeast = 0.6f;

        [Test]
        public void The_Mist_Reads_Thick_Where_A_Shut_Segment_Meets_The_Open_Lawn()
        {
            Motion.SetReduced(true);
            int last = Expansion.LastSegments(runner.Defs);
            var thin = new List<string>();
            float least = 1f;
            for (int open = 0; open < last; open++)
            {
                HavePlots(Expansion.PlotsForSegments(runner.Defs, open));
                for (int n = open + 1; n <= Mathf.Min(last, open + 2); n++)
                {
                    if (GardenGrid.SideOf(n) != GardenSide.Right && GardenGrid.SideOf(n) != GardenSide.Left) continue;
                    Vector2 span = GardenScreen.SegmentSpan(n);
                    bool right = GardenGrid.SideOf(n) == GardenSide.Right;
                    float edge = right ? span.x : span.y, inward = right ? 1f : -1f;
                    foreach (float depth in new[] { 20f, 60f })
                        for (float y = Teapot.y - 600f; y <= Teapot.y + 600f; y += 25f)
                        {
                            var at = new Vector2(edge + inward * depth, y);
                            float cover = MistCoverAt(at);
                            least = Mathf.Min(least, cover);
                            if (cover < MistEdgeLeast) thin.Add("segment " + n + " (" + open + " open) at " + at + ": " + cover.ToString("0.00"));
                        }
                }
            }
            Assert.IsEmpty(thin, "the least is " + least.ToString("0.00") + "; thin at " + thin.Count + " points:\n" + string.Join("\n", thin.GetRange(0, Mathf.Min(20, thin.Count))));
        }

        [Test]
        public void No_Plate_Or_Sign_Of_The_Open_Lawn_Lies_Under_The_Mist()
        {
            Motion.SetReduced(true);
            GameState s = runner.State;
            s.GardenXp = Garden.XpForLevel(9);
            int last = Expansion.LastSegments(runner.Defs);
            var under = new List<string>();
            int measured = 0;
            for (int open = 0; open < last; open++)
            {
                HavePlots(Expansion.PlotsForSegments(runner.Defs, open));
                MistSegment next = Expansion.Next(runner.Defs, s);
                s.GardenXp = System.Math.Max(s.GardenXp, Garden.XpForLevel(next.NeedLevel));
                while (s.Rabbits.Count < next.NeedRabbits) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" + s.NextRabbitId });
                foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < next.NeedKinds) s.SeenKinds.Add(kind.Id);
                if (!Expansion.HasDays(next, s)) { s.MaxDaySeen = System.Math.Max(s.MaxDaySeen, 20000); s.FirstDaySeen = s.MaxDaySeen - next.NeedDays; }
                s.Coins = Expansion.CostOfNext(runner.Defs, s) + new BigNum(1);
                screen.Refresh();
                var readable = new List<RectTransform>();
                foreach (Transform child in World)
                {
                    if (!child.gameObject.activeInHierarchy) continue;
                    Transform plate = child.name.StartsWith("Station ") ? child.Find("Level") : (child.name == "Forest Sign" ? child.Find("Board") : (child.name.StartsWith("Spot ") ? null : child.Find("Plate")));
                    if (plate != null && plate.gameObject.activeInHierarchy) readable.Add((RectTransform)plate);
                }
                Assert.IsTrue(World.Find("Forest Sign").gameObject.activeSelf, "the sign is up with " + open + " open");
                int mistTop = -1;
                for (int n = open + 1; n <= last; n++) mistTop = Mathf.Max(mistTop, screen.MistOf(n).transform.GetSiblingIndex());
                foreach (RectTransform plate in readable)
                {
                    var c = new Vector3[4];
                    plate.GetWorldCorners(c);
                    Vector2 a = World.InverseTransformPoint(c[0]), b = World.InverseTransformPoint(c[2]);
                    Rect r = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
                    int segment = GardenGrid.SegmentOf(GardenScreen.WorldToCell(r.center));
                    bool sign = plate.parent.name == "Forest Sign";
                    if (!sign && (segment < 0 || segment > open)) continue;
                    measured++;
                    Transform top = plate;
                    while (top.parent != World) top = top.parent;
                    if (top.GetSiblingIndex() > mistTop) continue;
                    float most = 0f;
                    for (float x = r.xMin; x <= r.xMax; x += 10f)
                        for (float y = r.yMin; y <= r.yMax; y += 10f) most = Mathf.Max(most, MistCoverAt(new Vector2(x, y)));
                    if (most > 0.05f) under.Add(top.name + "/" + plate.name + " at " + r + " with " + open + " open: mist " + most.ToString("0.00"));
                }
            }
            Assert.Greater(measured, 20, "the plates of the open lawn were measured");
            Assert.IsEmpty(under, under.Count + " under the mist:\n" + string.Join("\n", under));
        }

        [Test]
        public void Under_Reduced_Motion_Opening_A_Segment_Clears_Its_Mist_Edge_And_All_At_Once()
        {
            Motion.SetReduced(true);
            screen.Shell.StartGame();
            HavePlots(Plots.Starting);
            CanvasGroup mist = screen.MistOf(1);
            Assert.IsTrue(mist.gameObject.activeSelf);
            Vector2 edge = new Vector2(GardenScreen.SegmentSpan(1).x + 20f, Teapot.y);
            Assert.Greater(MistCoverAt(edge), MistEdgeLeast);
            HavePlots(Plots.Starting + 2);
            Assert.IsFalse(mist.gameObject.activeSelf, "no time passes: the mist is simply gone");
            Assert.AreEqual(0f, MistCoverAt(edge), 1e-4f, "its thick edge with it");
            Assert.IsTrue(screen.MistOf(2).gameObject.activeSelf, "the next segment's mist stays");
        }

        [Test]
        public void The_Station_Card_Names_Its_Dish_And_Recipe_Leaves_Raise_It()
        {
            screen.Shell.StartGame();
            screen.FocusStation(0);
            screen.Refresh();
            Text T(string path) { Transform x = root.transform.Find(path); Assert.IsNotNull(x, "Missing UI object: " + path); return x.GetComponent<Text>(); }
            Assert.AreEqual("Món: Trà sương · cấp 1", T("Canvas/Card/Dish/Line").text);
            Button raise = Find("Canvas/Card/Dish/Raise");
            Assert.AreEqual("Mở sổ ở cấp vườn 30", T("Canvas/Card/Dish/Raise/Face/Text").text);
            Assert.IsFalse(raise.interactable);
            runner.State.Season = 1;
            screen.Refresh();
            Assert.IsFalse(raise.interactable, "no leaves yet");
            Assert.AreEqual("Nâng món (5 lá)", T("Canvas/Card/Dish/Raise/Face/Text").text);
            Transform chip = root.transform.Find("Canvas/Hud/RecipeLeaves");
            Assert.IsFalse(chip.gameObject.activeSelf, "nothing strange for a new player");

            runner.State.RecipeLeaves = 7;
            screen.Refresh();
            Assert.IsTrue(chip.gameObject.activeSelf);
            Assert.AreEqual("7", T("Canvas/Hud/RecipeLeaves/Text").text);
            Assert.IsTrue(raise.interactable);
            BigNum before = Economy.IncomePerCycle(runner.Defs, runner.State, 0);
            raise.onClick.Invoke();
            Assert.AreEqual(2, Recipes.Level(runner.State, 0));
            Assert.AreEqual(2, runner.State.RecipeLeaves);
            Assert.AreEqual("Món: Trà sương · cấp 2", T("Canvas/Card/Dish/Line").text);
            Assert.AreEqual("Nâng món (20 lá)", T("Canvas/Card/Dish/Raise/Face/Text").text);
            Assert.IsFalse(raise.interactable, "20 leaves for the next level, 2 in hand");
            Assert.AreEqual((before * Recipes.LevelMultiplier(2)).ToDouble(), Economy.IncomePerCycle(runner.Defs, runner.State, 0).ToDouble(), 1e-6);
            Assert.AreEqual("2", T("Canvas/Hud/RecipeLeaves/Text").text);

            L10n.SetLocale("en");
            screen.RebuildUi();
            screen.FocusStation(0);
            screen.Refresh();
            Assert.AreEqual("Dish: Dew Tea · level 2", T("Canvas/Card/Dish/Line").text);
            Assert.AreEqual("Improve dish (20 leaves)", T("Canvas/Card/Dish/Raise/Face/Text").text);
        }

        [Test]
        public void Every_Station_Has_Three_Dishes_Named_In_Both_Languages()
        {
            foreach (string locale in new[] { "vi", "en" })
            {
                L10n.SetLocale(locale);
                var names = new HashSet<string>();
                foreach (StationDef d in runner.Defs)
                    for (int dish = 0; dish < Recipes.Dishes; dish++)
                    {
                        string name = L10n.Dish(d.Id, dish);
                        Assert.AreNotEqual("dish." + d.Id + "." + (dish + 1), name, locale + ": " + d.Id);
                        Assert.IsTrue(names.Add(name), locale + ": two dishes share " + name);
                    }
                Assert.AreEqual(16 * Recipes.Dishes, names.Count);
            }
        }

        [Test]
        public void The_Station_Card_Keeps_Its_Old_Places_And_The_Dish_Row_Sits_Between_Them()
        {
            screen.Shell.StartGame();
            screen.FocusStation(0);
            screen.Refresh();
            RectTransform Rt(string path) => (RectTransform)root.transform.Find(path);
            RectTransform card = Rt("Canvas/Card"), dish = Rt("Canvas/Card/Dish"), modes = Rt("Canvas/Card/Modes"), buy = Rt("Canvas/Card/Buy"), worker = Rt("Canvas/Card/Worker");
            Assert.AreEqual(-202f, modes.offsetMin.y, 1e-3f, "the buy modes keep their place under the card's top");
            Assert.AreEqual(74f, buy.anchoredPosition.y, 1e-3f, "and the buy button its place over the card's foot");
            Assert.AreEqual(74f, worker.anchoredPosition.y, 1e-3f);
            float modesBottom = card.sizeDelta.y + modes.offsetMin.y, buyTop = buy.anchoredPosition.y + buy.sizeDelta.y / 2f;
            Assert.GreaterOrEqual(dish.offsetMin.y, buyTop, "the dish row clears the buttons below");
            Assert.LessOrEqual(dish.offsetMax.y, modesBottom, "and the buy modes above");
            Assert.GreaterOrEqual(dish.offsetMax.y - dish.offsetMin.y, UiKit.MinTouch, "a full touch target");
        }
    }
}

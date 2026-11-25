using System;
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
    [MonetizationOn]
    public class JuiceTests
    {
        const long Hour = 3600L;
        const string Quests = "Canvas/Sheet/Panel/Quests/Content/";
        const string Pass = Quests + "MonthPassRow/";
        static readonly long Tuesday = GiftCodes.Day(2026, 11, 24);

        public static readonly string[] Places =
        {
            "QuestBox", "WeekChest", "MonthTier", "MonthTierThirty", "MoonPassLetter", "NewcomerTask", "WelcomeBackDay",
            "TrunkBuy", "FairStall", "FishMilestone", "FishPlain", "AchievementTier", "NewDish",
            "GoldenPack", "MoonPass", "MonthPass", "Offer"
        };

        struct Felt
        {
            public string Sound;
            public HapticStrength Strength;
            public bool Chest;
            public bool Flies;
        }

        GameObject root;
        GameRunner runner;
        GardenScreen screen;
        FakeStorefront store;
        FakeHapticMotor motor;
        long today;
        int hapticsBefore;

        [SetUp]
        public void SetUp()
        {
            L10n.SetLocale("vi");
            Motion.SetReduced(false);
            Haptics.SetOn(true);
            motor = new FakeHapticMotor();
            Haptics.Motor = motor;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            Motion.SetReduced(false);
            Haptics.Motor = null;
            L10n.SetLocale("vi");
        }

        GameState Open(long day, int level = 6, Action<GameState> prepare = null, bool cards = false)
        {
            today = day;
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            var cam = new GameObject("Cam", typeof(Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Analytics = new MemoryAnalytics();
            runner.DayClock = () => today;
            runner.OfferWallClock = () => today * Offers.DaySeconds + 10 * Hour;
            runner.LocalClock = () => today * 86400L + 19 * Hour + 600L;
            runner.OfferCardsOn = cards;
            store = new FakeStorefront();
            runner.Store = store;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            Rabbits.GiftFirst(s, "Bông");
            s.TutorialStep = Tutorial.Steps;
            s.PlaySeconds = Offers.StarterQuietSeconds;
            s.HintSeen = true;
            s.GardenXp = Garden.XpForLevel(level);
            s.FirstDaySeen = today - 20;
            s.MaxDaySeen = today;
            s.WelcomeBack.SeenDay = today;
            s.MailDay = today;
            s.GiftDay = today;
            s.Newcomer.Closed = true;
            prepare?.Invoke(s);
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Shell.StartGame();
            screen.Refresh();
            return runner.State;
        }

        void Press(string path)
        {
            Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            t.GetComponent<Button>().onClick.Invoke();
        }

        void Step(float seconds)
        {
            for (float t = 0f; t < seconds; t += 1f / 30f) screen.Advance(1f / 30f);
        }

        void Watch()
        {
            Sfx.ClearRecent();
            motor.Pulses.Clear();
            hapticsBefore = Haptics.Count;
        }

        static Felt Small() => new Felt { Sound = "reward", Strength = HapticStrength.Light, Flies = true };
        static Felt Chest(bool flies) => new Felt { Sound = "unlock", Strength = HapticStrength.Medium, Chest = true, Flies = flies };
        static Felt Big(bool flies) => new Felt { Sound = "milestone", Strength = HapticStrength.Medium, Chest = true, Flies = flies };
        static Felt Bought() => new Felt { Sound = "sting", Strength = HapticStrength.Medium, Flies = true };

        Felt Receive(string place)
        {
            switch (place)
            {
                case "QuestBox":
                {
                    GameState s = Open(Tuesday);
                    screen.OpenSheet(2);
                    int n = 0;
                    foreach (QuestDef q in runner.QuestDefs)
                        if (q.Daily && n < 5) { s.Claimed.Add(q.Id); n++; }
                    screen.Refresh();
                    Assert.IsTrue(screen.ChestRows.BoxReady);
                    Watch();
                    Press(Quests + "QuestBoxRow/Box");
                    Assert.IsTrue(screen.Prizes.IsOpen);
                    return Small();
                }
                case "WeekChest":
                {
                    GameState s = Open(Tuesday);
                    screen.OpenSheet(2);
                    s.WeekPoints = 120;
                    screen.Refresh();
                    Watch();
                    Press(Quests + "QuestWeekRow/Track/Chest 0");
                    Assert.IsTrue(QuestChests.Claimed(s, 0));
                    return Chest(true);
                }
                case "MonthTier":
                {
                    GameState s = Open(Tuesday);
                    screen.OpenSheet(2);
                    s.PassPoints = 2 * MonthPass.PointsPerTier;
                    screen.Refresh();
                    Watch();
                    Press(Pass + "Free/Tier 2");
                    Assert.IsTrue(screen.Prizes.IsOpen);
                    return Small();
                }
                case "MonthTierThirty":
                {
                    GameState s = Open(Tuesday);
                    screen.OpenSheet(2);
                    s.PassPoints = 30 * MonthPass.PointsPerTier;
                    MonthPass.Buy(s, Tuesday);
                    screen.Refresh();
                    Press(Pass + "Next");
                    Press(Pass + "Next");
                    Press(Pass + "Premium/Tier 10");
                    Assert.IsTrue(screen.KindChooser.IsOpen);
                    Watch();
                    screen.KindChooser.Pick(0);
                    return Chest(false);
                }
                case "MoonPassLetter":
                {
                    Open(Tuesday);
                    runner.BuyMoonPass(_ => { });
                    screen.Refresh();
                    screen.Shell.OpenPause();
                    Press("Canvas/Pause/Card/Mail");
                    Assert.IsTrue(screen.Mail.IsOpen);
                    Assert.AreEqual(LetterKind.MoonPassDay, runner.Letters[0].Kind);
                    Watch();
                    Press("Canvas/Mail/Card/Letter 0/Claim");
                    return Small();
                }
                case "NewcomerTask":
                {
                    Open(Tuesday, 2, s =>
                    {
                        s.Newcomer.Closed = false;
                        s.FirstDaySeen = Tuesday;
                        s.Levels[0] = 25;
                    });
                    Press("Canvas/Hud/Newcomer");
                    Assert.IsTrue(screen.FirstWeek.IsOpen);
                    Watch();
                    Press("Canvas/NewcomerSheet/Card/Task 0/Claim");
                    return Small();
                }
                case "WelcomeBackDay":
                {
                    Open(Tuesday, 2, s =>
                    {
                        Rabbit loved = Rabbits.ArriveKind(s, "tho-me", "Mè");
                        Rabbits.AddBond(s, loved, Rabbits.PointsFor(4));
                        s.FirstDaySeen = Tuesday - 9;
                        s.MaxDaySeen = Tuesday - 4;
                        s.WelcomeBack.SeenDay = s.MaxDaySeen;
                    });
                    Press("Canvas/Hud/WelcomeBack");
                    Assert.IsTrue(screen.WelcomeCard.IsOpen);
                    Watch();
                    Press("Canvas/WelcomeBackCard/Card/Claim");
                    return Small();
                }
                case "TrunkBuy":
                {
                    Open(Tuesday, MoonJars.ShowLevel + 1, s => s.GoldenCarrots = 500);
                    screen.OpenTrunk();
                    Assert.IsTrue(screen.TrunkCard.ShowsGoods);
                    Press("Canvas/TrunkCard/Card/Goods/Item 2/Buy");
                    Watch();
                    Press("Canvas/TrunkCard/Card/Goods/Item 2/Buy");
                    Assert.IsTrue(runner.TrunkShelf.Offers[1].Bought);
                    return Chest(false);
                }
                case "FairStall":
                {
                    FairRun may = Fairs.RunOn(GiftCodes.Day(2027, 5, Fairs.StartDay)).Value;
                    long day = may.Start;
                    while (Fairs.Weekend(day) && day < may.End) day++;
                    GameState s = Open(day, 6, null, true);
                    Assert.IsTrue(screen.FairCard.IsOpen);
                    s.Fair.Coins = 480;
                    Press("Canvas/FairCard/Card/Stall 0");
                    Watch();
                    Press("Canvas/FairCard/Card/Stall 0");
                    Assert.AreEqual(440, s.Fair.Coins);
                    return Small();
                }
                case "FishMilestone":
                case "FishPlain":
                {
                    GameState s = Open(Tuesday, 5, st =>
                    {
                        st.Levels[1] = 12; st.Levels[2] = 9; st.Levels[3] = 4;
                        st.Fish[2] = 2;
                    });
                    Step(1.2f);
                    Press("Canvas/Garden/Content/PondTap");
                    screen.Refresh();
                    Step(0.3f);
                    Assert.IsTrue(screen.FishingCard.IsOpen);
                    Press("Canvas/Pond/Card/Basket/Fish 2");
                    if (place == "FishPlain")
                    {
                        Press("Canvas/Pond/Card/Release");
                        Assert.AreEqual(1, Aquarium.Released(s, 2));
                        Step(1f);
                    }
                    Watch();
                    Press("Canvas/Pond/Card/Release");
                    CollectionAssert.Contains(Sfx.Recent, "sink", "the fish drops into the water");
                    return place == "FishMilestone" ? Big(true) : new Felt { Sound = "sink", Strength = HapticStrength.Light, Flies = true };
                }
                case "AchievementTier":
                {
                    GameState s = Open(Tuesday, 6, st => st.TotalTaps = 1_000_000_000L);
                    QuestDef chained = null;
                    foreach (QuestDef q in runner.QuestDefs)
                        if (q.Chained && q.Metric == QuestMetric.Taps && Ready(q, s)) { chained = q; break; }
                    Assert.IsNotNull(chained, "a tiered taps achievement is ready");
                    screen.OpenSheet(2);
                    screen.Refresh();
                    Watch();
                    Press(Quests + "Quest " + chained.Id + "/Claim");
                    Assert.AreEqual(1, VuonTra.Core.Quests.TiersClaimed(chained, s));
                    return Small();
                }
                case "NewDish":
                {
                    GameState s = Open(Tuesday, 30, st => { st.Levels[0] = 24; st.Levels[1] = 10; });
                    Assert.IsFalse(screen.NewDishCard.IsOpen);
                    s.Levels[0] = 25;
                    Watch();
                    screen.Refresh();
                    Assert.IsTrue(screen.NewDishCard.IsOpen);
                    return Big(false);
                }
                case "GoldenPack":
                {
                    Open(Tuesday);
                    OpenPacks();
                    Watch();
                    Press("Canvas/GoldenPacks/Card/Pack 0");
                    Assert.AreEqual(1, store.Bought.Count);
                    return Bought();
                }
                case "MoonPass":
                {
                    Open(Tuesday);
                    OpenPacks();
                    Watch();
                    Press("Canvas/GoldenPacks/Card/MoonPass");
                    Assert.AreEqual(MoonPass.Days, runner.MoonPassDaysLeft);
                    return Bought();
                }
                case "MonthPass":
                {
                    GameState s = Open(Tuesday);
                    screen.OpenSheet(2);
                    s.PassPoints = 12 * MonthPass.PointsPerTier;
                    screen.Refresh();
                    Watch();
                    Press(Pass + "Buy");
                    Assert.IsTrue(MonthPass.Premium(s, Tuesday));
                    return Bought();
                }
                case "Offer":
                {
                    Open(Tuesday, Offers.StarterLevel, null, true);
                    Assert.IsTrue(screen.OfferCard.IsOpen);
                    Watch();
                    Press("Canvas/Offer/Card/Buy");
                    Assert.AreEqual(1, runner.State.PacksBought);
                    return Bought();
                }
                default:
                    throw new ArgumentException(place);
            }
        }

        bool Ready(QuestDef q, GameState s) => VuonTra.Core.Quests.CanClaim(q, runner.Defs, s);

        void OpenPacks()
        {
            screen.OpenShop();
            Press("Canvas/Shop/Card/Have/BuyMore");
            Assert.IsTrue(screen.GoldenPacks.IsOpen);
        }

        [TestCaseSource(nameof(Places))]
        public void Every_Gift_Sounds_Buzzes_And_Flies_To_Its_Chip(string place)
        {
            Felt felt = Receive(place);
            CollectionAssert.Contains(Sfx.Recent, felt.Sound, place + ": its own sound");
            Assert.Greater(Haptics.Count, hapticsBefore, place + ": a buzz");
            CollectionAssert.Contains(motor.Pulses, felt.Strength, place + ": through the haptics seam, at the right strength");
            if (felt.Flies) Assert.Greater(screen.Feel.Flying, 0, place + ": the gift flies to its chip");
            if (felt.Chest) Assert.Greater(screen.Feel.ParticleCount, GardenScreen.ChestBits - 1, place + ": the chest bursts");
            Step(2f);
            Assert.AreEqual(0, screen.Feel.Flying, place + ": everything has landed");
        }

        [TestCaseSource(nameof(Places))]
        public void Under_Reduced_Motion_Nothing_Flies_But_The_Sound_And_The_Buzz_Stay(string place)
        {
            Motion.SetReduced(true);
            Felt felt = Receive(place);
            CollectionAssert.Contains(Sfx.Recent, felt.Sound, place);
            Assert.Greater(Haptics.Count, hapticsBefore, place);
            CollectionAssert.Contains(motor.Pulses, felt.Strength, place);
            Assert.AreEqual(0, screen.Feel.Flying, place + ": nothing flies");
        }

        [Test]
        public void Under_Reduced_Motion_The_Amount_Still_Rises_At_The_Chip()
        {
            Motion.SetReduced(true);
            Receive("GoldenPack");
            Assert.AreEqual(0, screen.Feel.Flying);
            Assert.Greater(screen.Feel.ParticleCount, 0, "the number floats up where the carrots would have landed");
        }

        [TestCase(PurchaseOutcome.Failed)]
        [TestCase(PurchaseOutcome.Unavailable)]
        public void A_Pack_That_Fails_Says_No_Without_A_Buzz_Or_A_Flight(PurchaseOutcome outcome)
        {
            Open(Tuesday);
            OpenPacks();
            store.NextOutcome = outcome;
            Watch();
            Press("Canvas/GoldenPacks/Card/Pack 0");
            Assert.AreEqual(0, store.Bought.Count);
            CollectionAssert.Contains(Sfx.Recent, "deny");
            CollectionAssert.DoesNotContain(Sfx.Recent, "sting");
            Assert.AreEqual(hapticsBefore, Haptics.Count, "no buzz");
            Assert.IsEmpty(motor.Pulses);
            Assert.AreEqual(0, screen.Feel.Flying, "nothing flies");
        }

        [Test]
        public void A_Failed_Month_Pass_Or_Offer_Says_No_Without_A_Buzz()
        {
            GameState s = Open(Tuesday);
            screen.OpenSheet(2);
            store.NextOutcome = PurchaseOutcome.Failed;
            Watch();
            Press(Pass + "Buy");
            Assert.IsFalse(MonthPass.Premium(s, Tuesday));
            CollectionAssert.Contains(Sfx.Recent, "deny");
            Assert.AreEqual(hapticsBefore, Haptics.Count);
            Assert.AreEqual(0, screen.Feel.Flying);
        }

        [Test]
        public void A_Cancelled_Pack_Is_Quiet()
        {
            Open(Tuesday);
            OpenPacks();
            store.NextOutcome = PurchaseOutcome.Cancelled;
            Watch();
            Press("Canvas/GoldenPacks/Card/Pack 0");
            CollectionAssert.DoesNotContain(Sfx.Recent, "deny", "the player said no, the game does not");
            Assert.AreEqual(hapticsBefore, Haptics.Count);
        }

        [Test]
        public void Switched_Off_Haptics_Reach_No_Motor()
        {
            Haptics.SetOn(false);
            try
            {
                Receive("WeekChest");
                Assert.IsEmpty(motor.Pulses);
                Assert.AreEqual(hapticsBefore, Haptics.Count);
                CollectionAssert.Contains(Sfx.Recent, "unlock", "the sound stays");
            }
            finally
            {
                Haptics.SetOn(true);
            }
        }

        [Test]
        public void The_Editor_Uses_A_Fake_Motor_That_Counts()
        {
            Haptics.Motor = null;
            Assert.IsInstanceOf<FakeHapticMotor>(Haptics.Motor, "no native vibrator in the Editor or on a Mac");
            var fake = (FakeHapticMotor)Haptics.Motor;
            Haptics.Light();
            Haptics.Medium();
            Haptics.Medium();
            Assert.AreEqual(1, fake.Light);
            Assert.AreEqual(2, fake.Medium);
            Assert.AreEqual(Haptics.MediumMilliseconds, Haptics.LastMilliseconds);
        }
    }
}

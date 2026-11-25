using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using UnityEngine.TestTools.Constraints;
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace VuonTra.Core.Tests
{
    public class AllocationTests
    {
        List<StationDef> defs;
        List<UpgradeDef> ups;
        static object sink;
        static UnityEngine.Vector2 spot;
        static int count;
        UnityEngine.GameObject root;
        GameRunner runner;
        long clock;

        static FairRun Fair => Fairs.RunOn(GiftCodes.Day(2027, 5, Fairs.StartDay)).Value;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
            ups = catalog.BuildUpgrades();
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) UnityEngine.Object.DestroyImmediate(root);
            root = null;
            L10n.SetLocale("vi");
        }

        GameState FullGarden(bool storeOne)
        {
            var s = GameState.NewGame(defs, ups.Count);
            s.Plots = defs.Count;
            for (int i = 0; i < defs.Count; i++) s.Levels[i] = 120 + 7 * i;
            GardenGrid.Settle(defs, s);
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.All;
            for (int n = 0; n < 160; n++) Rabbits.ArriveKind(s, kinds[n % kinds.Count].Id, "r" + n);
            for (int i = 0; i < defs.Count; i++) Rabbits.Assign(s, s.Rabbits[i].Id, i);
            for (int k = 0; k < kinds.Count; k++) s.SeenKinds.Add(kinds[k].Id);
            if (storeOne) Assert.IsTrue(Layout.Store(defs, s, GardenGrid.StationId(defs[5])));
            return s;
        }

        static long BytesPerCall(Action call, int calls)
        {
            for (int k = 0; k < 20; k++) call();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int k = 0; k < calls; k++) call();
            return (GC.GetAllocatedBytesForCurrentThread() - before) / calls;
        }

        static long HeapBytesPerCall(Action call, int calls)
        {
            for (int k = 0; k < 3; k++) call();
            long bytes = 0L;
            for (int attempt = 0; attempt < 5; attempt++)
            {
                int collections = GC.CollectionCount(0);
                long before = GC.GetTotalMemory(false);
                for (int k = 0; k < calls; k++) call();
                bytes = Math.Max(0L, GC.GetTotalMemory(false) - before);
                if (GC.CollectionCount(0) == collections) break;
            }
            return bytes / calls;
        }

        static int AllocationsIn(Action call)
        {
            UnityEngine.Profiling.Recorder recorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                call();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }
            return recorder.sampleBlockCount;
        }

        [Test]
        public void A_Full_Garden_Earns_And_Ticks_Without_Allocating([Values(false, true)] bool storeOne)
        {
            GameState s = FullGarden(storeOne);
            Assert.AreEqual(16, defs.Count, "the full garden of the issue: sixteen stations");
            Assert.AreEqual(storeOne ? 1 : 0, s.Grid.StoredCount);
            Modifiers mods = Modifiers.From(ups, s);
            var rng = new Random(97);
            Func<double> roll = rng.NextDouble;

            Action income = () => Economy.IncomePerSecond(defs, s);
            Action shares = () => { for (int i = 0; i < defs.Count; i++) Economy.StationIncomePerSecond(defs, s, i, out _, out _); };
            Action tenSeconds = () => { for (int k = 0; k < 600; k++) Simulation.Tick(defs, s, 1d / 60d, null, mods, roll); };
            var questDefs = new List<QuestDef>();
            foreach (QuestMetric metric in Enum.GetValues(typeof(QuestMetric))) questDefs.Add(new QuestDef { Id = "q", Metric = metric, StationId = defs[2].Id, Target = 1 });
            Action progress = () => { for (int i = 0; i < questDefs.Count; i++) Quests.Progress(questDefs[i], defs, s); };
            var chain = new QuestDef { Id = "chain", Metric = QuestMetric.Taps, Merged = new[] { "old" }, Tiers = new[] { new QuestTier { Target = 1 }, new QuestTier { Target = 2 }, new QuestTier { Target = 3 } } };
            s.Claimed.Add("old");
            Action tiers = () => { count = Quests.TiersClaimed(chain, s); sink = Quests.CurrentTier(chain, s); };
            tiers();
            progress();

            TestContext.WriteLine("IncomePerSecond: " + BytesPerCall(income, 1000) + " B a call; 16 StationIncomePerSecond: " + BytesPerCall(shares, 200)
                + " B; 600 ticks: " + BytesPerCall(tenSeconds, 5) + " B (stored " + s.Grid.StoredCount + ")");

            Assert.That(() => { sink = new object[4]; }, AllocIs.AllocatingGCMemory(), "the GC.Alloc recorder works here");
            Assert.That(() => income(), AllocIs.Not.AllocatingGCMemory(), "Economy.IncomePerSecond on a full garden");
            Assert.That(() => shares(), AllocIs.Not.AllocatingGCMemory(), "each station's share of the income");
            Assert.That(() => tenSeconds(), AllocIs.Not.AllocatingGCMemory(), "ten seconds of Simulation.Tick");
            Assert.That(() => progress(), AllocIs.Not.AllocatingGCMemory(), "every quest's progress");
            Assert.That(() => tiers(), AllocIs.Not.AllocatingGCMemory(), "a tiered achievement's claimed tiers (#111)");
            Assert.That(() => { count = AlbumSets.KindsMet(s); }, AllocIs.Not.AllocatingGCMemory(), "the album's every-day kinds");
            Assert.That(() => { sink = Seasons.KindOf(Season.MidAutumn); }, AllocIs.Not.AllocatingGCMemory(), "a season's kind");
            Assert.That(() => { count = RestSpots.SeatedCount(s); }, AllocIs.Not.AllocatingGCMemory(), "how many rabbits are seated");
        }

        [Test]
        public void A_Full_Garden_Screen_Finds_Its_Stations_Each_Frame_Without_Allocating()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            var root = new UnityEngine.GameObject("Game");
            try
            {
                GameState s = FullGarden(false);
                s.RestSpots = RestSpots.Max;
                var cam = new UnityEngine.GameObject("Cam", typeof(UnityEngine.Camera));
                cam.transform.SetParent(root.transform);
                var runner = root.AddComponent<GameRunner>();
                runner.Catalog = catalog;
                runner.Init(s);
                var screen = root.AddComponent<GardenScreen>();
                screen.Runner = runner;
                screen.Cam = cam.GetComponent<UnityEngine.Camera>();
                screen.Raining = () => false;
                screen.Build();
                screen.Refresh();
                screen.Shell.StartGame();
                for (int i = 0; i < 90; i++) screen.Advance(1f / 30f);
                Assert.AreEqual(16, runner.Defs.Count);
                int walking = 0;
                for (int k = 0; k < RestSpots.Max; k++) if (GardenScreen.Strolling(k, 3f)) walking++;
                Assert.Greater(walking, 0, "rabbits are out strolling, so every walker is sorted against every station");

                Action spots = () => { for (int i = 0; i < runner.Defs.Count; i++) spot = screen.StationSpot(i); };
                Action frame = () => screen.Advance(1f / 60f);
                TestContext.WriteLine("allocations in 16 StationSpot: " + AllocationsIn(spots) + "; in one full-garden frame: " + AllocationsIn(frame));
                Assert.That(() => spots(), AllocIs.Not.AllocatingGCMemory(), "every station's spot on a full garden");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        GardenScreen RoundTwoGarden()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            L10n.SetLocale("vi");
            long today = Fair.Start + 2;
            clock = today * 86400L + 19 * 3600L + 600L;
            GameState s = FullGarden(false);
            s.RestSpots = RestSpots.Max;
            s.TutorialStep = Tutorial.Steps;
            s.HintSeen = true;
            s.Newcomer.Closed = true;
            s.GardenXp = Garden.XpForLevel(40);
            s.FirstDaySeen = today - 40;
            s.MaxDaySeen = today;
            s.WelcomeBack.SeenDay = today;
            s.MailDay = today;
            s.GiftDay = today;
            s.Coins = BigNum.FromParts(4.56, 33);
            s.GoldenCarrots = 480;
            s.MoonJars = 3;
            s.RecipeLeaves = 7;
            s.Clovers = 12;
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                s.Fish[k] = 2;
                Aquarium.Release(s, k);
            }
            s.Aquarium.CardSeen = true;
            MoonPass.Grant(s, today);
            Assert.IsTrue(MonthPass.Buy(s, today), "the month pass is bought");
            s.Fair.Start = Fair.Start;
            s.Fair.Seen = Fair.Start;
            s.Fair.Coins = 14;

            root = new UnityEngine.GameObject("Game");
            var cam = new UnityEngine.GameObject("Cam", typeof(UnityEngine.Camera));
            cam.transform.SetParent(root.transform);
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            runner.Analytics = new MemoryAnalytics();
            runner.DayClock = () => today;
            runner.LocalClock = () => clock;
            runner.OfferWallClock = () => clock;
            runner.Init(s);
            runner.State.IntrosSeen.UnionWith(Intros.All);
            runner.State.IntrosTried.UnionWith(Intros.All);
            foreach (OfferRecord record in runner.State.OfferRecords) record.Shown = true;
            var screen = root.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam.GetComponent<UnityEngine.Camera>();
            screen.Raining = () => false;
            screen.Build();
            screen.Refresh();
            screen.Shell.StartGame();
            for (int i = 0; i < 120; i++) screen.Advance(1f / 30f);

            Assert.AreEqual(16, runner.Defs.Count);
            Assert.AreEqual(160, runner.State.Rabbits.Count);
            Assert.IsTrue(screen.TankProp.gameObject.activeSelf, "the aquarium stands in the garden");
            Assert.AreEqual(Aquarium.Kinds, Aquarium.KindsIn(runner.State), "every fish swims in it");
            Assert.IsTrue(screen.FairCard.ChipShown, "a fair runs");
            Assert.IsTrue(screen.MoonPassPill.gameObject.activeSelf, "the moon pass is bought");
            Assert.IsTrue(MonthPass.Premium(runner.State, today), "the month pass is bought");
            Assert.IsTrue(runner.TrunkOpenNow, "the cart chest is open");
            Assert.IsNotNull(runner.LeadingOffer, "a pack counts down");
            Assert.IsTrue(screen.Shop.OfferShown, "the shop's pack pill counts down");
            Assert.IsTrue(root.transform.Find("Canvas/Hud/Golden").gameObject.activeSelf, "golden carrots in the purse");
            return screen;
        }

        [Test, MonetizationOn]
        public void A_Full_Garden_With_Every_Round_Two_Feature_Stands_Still_Without_Allocating()
        {
            GardenScreen screen = RoundTwoGarden();
            Action second = () => { for (int f = 0; f < 60; f++) screen.Advance(1f / 60f); };
            Assert.That(() => second(), AllocIs.Not.AllocatingGCMemory(), "sixty frames of GardenScreen.Advance on a full garden with every round-2 feature on");
            TestContext.WriteLine("one second standing still: " + AllocationsIn(second) + " allocations, " + HeapBytesPerCall(second, 3) + " B");
        }

        UnityEngine.UI.Text TextAt(string path)
        {
            UnityEngine.Transform t = root.transform.Find(path);
            Assert.IsNotNull(t, "Missing UI object: " + path);
            return t.GetComponent<UnityEngine.UI.Text>();
        }

        UnityEngine.UI.Text TextEndingWith(string tail)
        {
            UnityEngine.UI.Text found = null;
            foreach (UnityEngine.UI.Text text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            {
                string path = text.name;
                for (UnityEngine.Transform p = text.transform.parent; p != null; p = p.parent) path = p.name + "/" + path;
                if (!path.EndsWith(tail, StringComparison.Ordinal) || path.Contains("(old)")) continue;
                Assert.IsNull(found, "two labels end with " + tail);
                found = text;
            }
            Assert.IsNotNull(found, "Missing label ending with " + tail);
            return found;
        }

        [Test, MonetizationOn]
        public void The_Wallet_And_The_Countdown_Pills_Leave_Their_Text_Alone_Until_The_Shown_String_Changes()
        {
            GardenScreen screen = RoundTwoGarden();
            var labels = new Dictionary<string, UnityEngine.UI.Text>
            {
                { "wallet", TextAt("Canvas/Hud/Purse/Coins") },
                { "golden", TextAt("Canvas/Hud/Golden/Text") },
                { "moon pass", TextAt("Canvas/Hud/MoonPass/Text") },
                { "fair", TextAt("Canvas/Hud/Fair/Text") },
                { "cart chest", screen.TrunkProp.Find("Tag/Text").GetComponent<UnityEngine.UI.Text>() },
            };
            var dirtied = new Dictionary<string, int>();
            var shown = new Dictionary<string, string>();
            foreach (KeyValuePair<string, UnityEngine.UI.Text> label in labels)
            {
                string name = label.Key;
                dirtied[name] = 0;
                shown[name] = label.Value.text;
                label.Value.RegisterDirtyVerticesCallback(() => dirtied[name]++);
            }
            StringAssert.StartsWith("4,56", shown["wallet"]);

            BigNum crumb = BigNum.FromParts(1d, 28);
            Action half = () =>
            {
                for (int f = 0; f < 30; f++)
                {
                    runner.State.Coins += crumb;
                    screen.Advance(1f / 60f);
                }
            };
            Assert.That(() => half(), AllocIs.Not.AllocatingGCMemory(), "the purse grows by less than the digit it shows and the clock stays in one second");
            foreach (KeyValuePair<string, UnityEngine.UI.Text> label in labels)
            {
                Assert.AreEqual(0, dirtied[label.Key], label.Key + " was not written");
                Assert.AreEqual(shown[label.Key], label.Value.text, label.Key);
            }

            runner.State.Coins = BigNum.FromParts(4.57d, 33);
            for (int f = 0; f < 60; f++) screen.Advance(1f / 60f);
            StringAssert.StartsWith("4,57", labels["wallet"].text, "the wallet shows the next digit");
            Assert.AreEqual(1, dirtied["wallet"], "written once for the new digit, not once a frame");

            screen.OpenShop();
            for (int f = 0; f < 30; f++) screen.Advance(1f / 60f);
            UnityEngine.UI.Text pack = TextEndingWith("Offer/Face/Left");
            string before = pack.text;
            int packDirtied = 0;
            pack.RegisterDirtyVerticesCallback(() => packDirtied++);
            for (int f = 0; f < 50; f++) screen.Advance(1f / 60f);
            Assert.AreEqual(before, pack.text);
            Assert.AreEqual(0, packDirtied, "the pack pill waits for the next second");
            clock += 1L;
            for (int f = 0; f < 60; f++) screen.Advance(1f / 60f);
            Assert.AreNotEqual(before, pack.text, "a second later it counts down");
            Assert.AreEqual(1, packDirtied, "once for that second, not once a frame");
        }

        [Test]
        public void The_Album_Read_Once_Gives_Each_Station_The_Same_Factor()
        {
            GameState s = GameState.NewGame(defs, ups.Count);
            IReadOnlyList<AlbumSet> sets = AlbumSets.All;
            for (int n = 0; n <= sets.Count; n++)
            {
                AlbumFactors album = AlbumSets.Factors(s);
                for (int i = -1; i <= defs.Count; i++)
                    Assert.AreEqual(AlbumSets.StationFactor(defs, s, i), album.At(defs, i), "station " + i + " with " + n + " sets complete");
                Assert.AreEqual(1d, album.Of(RabbitTrade.None));
                Assert.AreEqual(1d, album.At(null, 0));
                if (n < sets.Count) foreach (RabbitKind k in sets[n].Kinds) s.SeenKinds.Add(k.Id);
            }
            Assert.Greater(AlbumSets.Factors(s).Of(RabbitTrade.Brewer), 1d, "a full album pays");
        }

        [Test]
        public void A_Station_Keeps_Its_Grid_Id_And_Follows_A_New_One()
        {
            var d = new StationDef { Id = "am-tra" };
            Assert.AreEqual(GardenGrid.StationId(d), d.GridId);
            Assert.AreSame(d.GridId, d.GridId, "made once");
            d.Id = "lo-banh";
            Assert.AreEqual(GardenGrid.StationId(d), d.GridId, "a changed id makes a new one");
        }

        [Test]
        public void The_Seated_Count_Is_The_Seated_List_Counted()
        {
            GameState s = FullGarden(false);
            for (int spots = 0; spots <= RestSpots.Max + 1; spots++)
            {
                s.RestSpots = spots;
                Assert.AreEqual(RestSpots.Seated(s).Count, RestSpots.SeatedCount(s), spots + " places to rest");
            }
            s.Rabbits.RemoveRange(defs.Count + 2, s.Rabbits.Count - defs.Count - 2);
            s.RestSpots = RestSpots.Max;
            Assert.AreEqual(2, RestSpots.SeatedCount(s), "fewer resting rabbits than places");
            Assert.AreEqual(RestSpots.Seated(s).Count, RestSpots.SeatedCount(s));
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using VuonTra.Game;
using UnityEngine.TestTools.Constraints; // ConstraintExpression.AllocatingGCMemory()
using AllocIs = UnityEngine.TestTools.Constraints.Is;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// A full garden earns without making garbage (#97). The income sums asked the album again for every pair of stations,
    /// and each ask boxed a list's enumerator; whether a station was stored joined a new id string each time. On sixteen
    /// stations that came to 5–9 KB a call and about 5 MB a second, and the GC ran nearly every second.
    /// </summary>
    public class AllocationTests
    {
        List<StationDef> defs;
        List<UpgradeDef> ups;
        static object sink;
        static int count;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
            ups = catalog.BuildUpgrades();
        }

        /// <summary>Every station open, 160 rabbits (one at each station, the rest resting), every kind in the album; one station stored if asked.</summary>
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

        /// <summary>Bytes a call by the runtime's own counter, after a warm-up. Logged; the recorder below is what asserts.</summary>
        static long BytesPerCall(Action call, int calls)
        {
            for (int k = 0; k < 20; k++) call();
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int k = 0; k < calls; k++) call();
            return (GC.GetAllocatedBytesForCurrentThread() - before) / calls;
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
            // Ten seconds at 60 frames a second: cycles finish and pay, the tray fills.
            Action tenSeconds = () => { for (int k = 0; k < 600; k++) Simulation.Tick(defs, s, 1d / 60d, null, mods, roll); };
            var questDefs = new List<QuestDef>();
            foreach (QuestMetric metric in Enum.GetValues(typeof(QuestMetric))) questDefs.Add(new QuestDef { Id = "q", Metric = metric, StationId = defs[2].Id, Target = 1 });
            Action progress = () => { for (int i = 0; i < questDefs.Count; i++) Quests.Progress(questDefs[i], defs, s); };

            TestContext.WriteLine("IncomePerSecond: " + BytesPerCall(income, 1000) + " B a call; 16 StationIncomePerSecond: " + BytesPerCall(shares, 200)
                + " B; 600 ticks: " + BytesPerCall(tenSeconds, 5) + " B (stored " + s.Grid.StoredCount + ")");

            // The recorder must see a plain allocation, or the checks below would prove nothing.
            Assert.That(() => { sink = new object[4]; }, AllocIs.AllocatingGCMemory(), "the GC.Alloc recorder works here");
            Assert.That(() => income(), AllocIs.Not.AllocatingGCMemory(), "Economy.IncomePerSecond on a full garden");
            Assert.That(() => shares(), AllocIs.Not.AllocatingGCMemory(), "each station's share of the income");
            Assert.That(() => tenSeconds(), AllocIs.Not.AllocatingGCMemory(), "ten seconds of Simulation.Tick");
            Assert.That(() => progress(), AllocIs.Not.AllocatingGCMemory(), "every quest's progress");
            Assert.That(() => { count = AlbumSets.KindsMet(s); }, AllocIs.Not.AllocatingGCMemory(), "the album's every-day kinds");
            Assert.That(() => { sink = Seasons.KindOf(Season.MidAutumn); }, AllocIs.Not.AllocatingGCMemory(), "a season's kind");
            Assert.That(() => { count = RestSpots.SeatedCount(s); }, AllocIs.Not.AllocatingGCMemory(), "how many rabbits are seated");
        }

        /// <summary>The album read once (#97) gives every station the very factor it gave before, as the album fills.</summary>
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

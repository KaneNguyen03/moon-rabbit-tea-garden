using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.Core.Tests
{
    public class FishingRunnerTests
    {
        GameObject root;
        GameRunner runner;
        long now = 1_800_000_000L;

        [SetUp]
        public void SetUp()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            root = new GameObject("Game");
            runner = root.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            GameState s = GameState.NewGame(catalog.BuildDefs());
            s.GardenXp = Garden.XpForLevel(Fishing.UnlockLevel);
            runner.Init(s);
            runner.FishingClock = () => now;
        }

        [TearDown]
        public void TearDown()
        {
            if (root != null) Object.DestroyImmediate(root);
        }

        [Test]
        public void A_Cast_From_Bait_To_Basket_To_A_Rabbits_Hearts()
        {
            Assert.IsTrue(runner.PondOpen);
            Assert.AreEqual(Fishing.BaitCap, runner.Baits);
            Assert.AreEqual(0, runner.NextBaitIn);

            FishingCast cast = runner.StartCast();
            Assert.IsNotNull(cast);
            Assert.AreSame(cast, runner.Cast);
            Assert.AreEqual(Fishing.BaitCap - 1, runner.Baits);
            Assert.AreEqual(Fishing.BaitSeconds, runner.NextBaitIn);
            Assert.IsNull(runner.StartCast(), "one line in the water at a time");
            Assert.AreEqual(Fishing.BaitCap - 1, runner.Baits, "and no bait spent on the refused one");

            Assert.AreEqual(CastTap.Early, runner.TapCast(), "a tap while it bobs is too early");
            Assert.AreEqual(CastPhase.Sinking, runner.AdvanceCast(cast.BobSeconds + 0.1));
            Assert.AreEqual(CastTap.Caught, runner.TapCast());
            Assert.IsNull(runner.Cast, "the cast is over");
            Assert.AreEqual(1, runner.Basket[cast.Fish]);
            Assert.AreEqual(1, runner.State.FishCaught);

            Rabbit r = runner.State.Rabbits.Count > 0 ? runner.State.Rabbits[0] : Rabbits.GiftFirst(runner.State, "Bông");
            FeedResult fed = runner.FeedBest(r.Id);
            Assert.IsTrue(fed.Fed);
            Assert.AreEqual(Fishing.Points[cast.Fish], r.Affection);
            Assert.AreEqual(0, Fishing.InBasket(runner.State));
            Assert.IsFalse(runner.FeedBest(r.Id).Fed, "an empty basket feeds nobody");
            Assert.IsFalse(runner.Feed(r.Id + 99, 0).Fed, "nor a rabbit that is not there");
        }

        [Test]
        public void A_Float_Left_To_Sink_Loses_The_Bait_And_The_Line_Comes_Back()
        {
            FishingCast cast = runner.StartCast();
            Assert.AreEqual(CastPhase.Lost, runner.AdvanceCast(cast.BobSeconds + Fishing.SinkSeconds + 0.1));
            Assert.IsNull(runner.Cast);
            Assert.AreEqual(CastTap.None, runner.TapCast());
            Assert.AreEqual(0, Fishing.InBasket(runner.State));
            Assert.IsNotNull(runner.StartCast(), "a new cast once the old one is over");
            Assert.AreEqual(Fishing.BaitCap - 2, runner.Baits);

            now += 2 * 3600;
            runner.AdvanceCast(10d);
            Assert.AreEqual(Fishing.BaitCap, runner.Baits, "two hours later the line is full again");
        }

        [Test]
        public void The_Fish_And_The_Full_Hearts_Are_Seeded_As_Tiered_Achievements()
        {
            QuestDef fish = null, hearts = null;
            foreach (QuestDef q in runner.QuestDefs)
            {
                if (q.Id == "ca") fish = q;
                if (q.Id == "tim") hearts = q;
                Assert.AreNotEqual("ca-50", q.Id, "the fifty fish are a tier of the fishing chain now (#111)");
                Assert.AreNotEqual("tim-10", q.Id, "the ten hearts are a tier of the full-hearts chain now (#111)");
            }
            Assert.IsNotNull(fish, "Câu cá");
            Assert.AreEqual(QuestMetric.FishCaught, fish.Metric);
            Assert.AreEqual(8, Quests.TiersOf(fish));
            Assert.AreEqual(5d, fish.Tiers[0].Target);
            Assert.AreEqual(2000d, fish.Tiers[7].Target);
            CollectionAssert.Contains(fish.Merged, "ca-50");
            Assert.IsTrue(fish.Tiers[2].Merged, "the tier of fifty fish is the old achievement");
            Assert.AreEqual(50d, fish.Tiers[2].Target);
            Assert.AreEqual(2, fish.Tiers[2].RewardClovers, "and pays what it always paid");
            Assert.IsFalse(fish.Daily);
            Assert.IsNotNull(hearts, "Tim đầy");
            Assert.AreEqual(QuestMetric.RabbitsFullHearts, hearts.Metric);
            Assert.AreEqual(1d, hearts.Tiers[0].Target);
            Assert.AreEqual(30d, hearts.Tiers[7].Target);
            CollectionAssert.Contains(hearts.Merged, "tim-10");
            Assert.AreEqual("Câu cá", L10n.T("quest.ca"));
            Assert.AreEqual("Tim đầy", L10n.T("quest.tim"));
            Assert.AreEqual("42/100 con", L10n.T("quest.ca.count", "42", "100", 100d));
        }
    }
}

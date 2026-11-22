using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>The album pays (#61): sets by trade within a tier add to that trade's stations, and milestones pay once.</summary>
    public class AlbumSetsTests
    {
        List<StationDef> defs;
        GameState s;
        List<ItemDef> items;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            defs[0].Trade = RabbitTrade.Brewer;   // tea
            defs[1].Trade = RabbitTrade.Gatherer; // leaf, feeds the tea
            defs[2].Trade = RabbitTrade.Baker;    // jam
            s = GameState.NewGame(defs);
            items = new List<ItemDef>
            {
                new ItemDef { Id = "hat-non-la", Name = "Nón lá", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 20, Tier = 1 },
                new ItemDef { Id = AlbumSets.MilestoneHat, Name = "Mũ sao", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 50, Tier = 3 },
            };
        }

        void SeeAll(AlbumSet set) { foreach (RabbitKind k in set.Kinds) Rabbits.See(s, k.Id); }

        /// <summary>Meets the first <paramref name="n"/> every-day kinds in table order.</summary>
        void MeetEveryDay(int n)
        {
            foreach (RabbitKind k in RabbitKinds.All)
            {
                if (n <= 0) break;
                if (k.Season != Season.None || k.NewMoonOnly) continue;
                Rabbits.See(s, k.Id);
                n--;
            }
        }

        [Test]
        public void Every_Tier_And_Trade_With_Kinds_Is_A_Set_Of_Every_Day_Kinds_Only()
        {
            Assert.AreEqual(24, AlbumSets.All.Count, "six tiers by four trades, none of them empty");
            var seen = new HashSet<string>();
            foreach (AlbumSet set in AlbumSets.All)
            {
                Assert.Greater(set.Kinds.Count, 0);
                foreach (RabbitKind k in set.Kinds)
                {
                    Assert.AreEqual(set.Tier, k.Tier);
                    Assert.AreEqual(set.Trade, k.Trade);
                    Assert.AreEqual(Season.None, k.Season, k.Id + ": a set never waits on a season");
                    Assert.IsFalse(k.NewMoonOnly, k.Id + ": nor on a new moon");
                    Assert.IsTrue(seen.Add(k.Id), k.Id + " is in one set only");
                }
            }
            Assert.AreEqual(72, seen.Count, "every every-day kind belongs to a set");
            Assert.AreEqual(72, AlbumSets.EveryDayKinds);
            Assert.AreEqual(4, AlbumSets.OfTier(3).Count);
            Assert.AreSame(AlbumSets.Find(3, RabbitTrade.Brewer), AlbumSets.OfTier(3)[0], "trades in the order Brewer, Baker, Gatherer, Stargazer");
        }

        [Test]
        public void A_Set_Completes_When_Every_One_Of_Its_Kinds_Is_In_The_Album()
        {
            AlbumSet set = AlbumSets.Find(0, RabbitTrade.Stargazer);
            Assert.IsFalse(AlbumSets.IsComplete(s, set));
            Rabbits.See(s, set.Kinds[0].Id);
            Assert.AreEqual(1, AlbumSets.Met(s, set));
            Assert.IsFalse(AlbumSets.IsComplete(s, set), "one kind short");
            for (int i = 1; i < set.Kinds.Count; i++) Rabbits.See(s, set.Kinds[i].Id);
            Assert.IsTrue(AlbumSets.IsComplete(s, set));
            CollectionAssert.AreEqual(new[] { set }, AlbumSets.Completed(s));
            // A seasonal kind of the same tier and trade does not belong to it: meeting the Gold stargazers leaves room for the star lantern.
            AlbumSet gold = AlbumSets.Find(4, RabbitTrade.Stargazer);
            SeeAll(gold);
            Assert.IsFalse(s.SeenKinds.Contains("tho-den-ong-sao"));
            Assert.IsTrue(AlbumSets.IsComplete(s, gold), "complete without the Mid-Autumn kind");
        }

        [Test]
        public void A_Rabbit_Merged_Away_Still_Counts_For_Its_Set()
        {
            AlbumSet set = AlbumSets.Find(0, RabbitTrade.Stargazer);
            Rabbits.ArriveKind(s, set.Kinds[0].Id, "A");
            Rabbits.ArriveKind(s, set.Kinds[1].Id, "B");
            Rabbits.ArriveKind(s, set.Kinds[0].Id, "C");
            Assert.IsTrue(AlbumSets.IsComplete(s, set));
            Assert.IsNotNull(Rabbits.Merge(s, 0), "the three become one of the next tier");
            foreach (Rabbit r in s.Rabbits) Assert.AreNotEqual(0, r.Tier, "no rabbit of the set is left");
            Assert.IsTrue(AlbumSets.IsComplete(s, set), "the album remembers them");
        }

        [Test]
        public void Each_Complete_Set_Adds_Three_Percent_To_Its_Own_Trade_Only_Up_To_Eighteen()
        {
            Assert.AreEqual(1d, AlbumSets.TradeFactor(s, RabbitTrade.Brewer), 1e-12);
            SeeAll(AlbumSets.Find(0, RabbitTrade.Brewer));
            Assert.AreEqual(1.03d, AlbumSets.TradeFactor(s, RabbitTrade.Brewer), 1e-12);
            Assert.AreEqual(1d, AlbumSets.TradeFactor(s, RabbitTrade.Baker), 1e-12, "another trade gains nothing");
            SeeAll(AlbumSets.Find(2, RabbitTrade.Brewer));
            Assert.AreEqual(1.06d, AlbumSets.TradeFactor(s, RabbitTrade.Brewer), 1e-12, "sets of one trade add up");
            foreach (AlbumSet set in AlbumSets.All) SeeAll(set);
            foreach (RabbitTrade trade in new[] { RabbitTrade.Brewer, RabbitTrade.Baker, RabbitTrade.Gatherer, RabbitTrade.Stargazer })
                Assert.AreEqual(1.18d, AlbumSets.TradeFactor(s, trade), 1e-12, trade + ": six sets at most, +18 %");
            Assert.AreEqual(1d, AlbumSets.TradeFactor(s, RabbitTrade.None), 1e-12);
        }

        [Test]
        public void The_Factor_Lands_On_The_Income_Of_The_Stations_Of_That_Trade()
        {
            s.Levels = new[] { 10, 4, 6 };
            double tea = Economy.IncomePerCycle(defs, s, 0).ToDouble(), jam = Economy.IncomePerCycle(defs, s, 2).ToDouble();

            SeeAll(AlbumSets.Find(1, RabbitTrade.Brewer));
            Assert.AreEqual(tea * 1.03d, Economy.IncomePerCycle(defs, s, 0).ToDouble(), tea * 1e-9, "the tea is a brewer's station");
            Assert.AreEqual(jam, Economy.IncomePerCycle(defs, s, 2).ToDouble(), jam * 1e-9, "the jam is not");

            SeeAll(AlbumSets.Find(1, RabbitTrade.Baker));
            Assert.AreEqual(jam * 1.03d, Economy.IncomePerCycle(defs, s, 2).ToDouble(), jam * 1e-9);

            // A gatherers' set strengthens what the leaf basket adds to the tea, as a rabbit at the basket would.
            double before = Economy.IncomePerCycle(defs, s, 0).ToDouble();
            SeeAll(AlbumSets.Find(1, RabbitTrade.Gatherer));
            double boost = Economy.IngredientBoost(4);
            Assert.AreEqual(before / boost * (1d + (boost - 1d) * 1.03d), Economy.IncomePerCycle(defs, s, 0).ToDouble(), before * 1e-9);
            Assert.AreEqual(1.03d, AlbumSets.StationFactor(defs, s, 1), 1e-12);
            Assert.AreEqual(1d, AlbumSets.StationFactor(defs, s, 7), 1e-12, "no such station");
        }

        [Test]
        public void The_Milestones_Count_Every_Day_Kinds_And_Their_Gifts_Come_Once()
        {
            MeetEveryDay(9);
            Rabbits.See(s, "tho-noel");
            Rabbits.See(s, NewMoon.MoonKind);
            Assert.AreEqual(9, AlbumSets.KindsMet(s), "the seasons' and the new moon's kinds are not counted");
            Assert.IsEmpty(AlbumSets.ClaimMilestones(s, items));

            MeetEveryDay(40);
            CollectionAssert.AreEqual(new[] { 10, 20, 40 }, AlbumSets.ClaimMilestones(s, items));
            Assert.IsTrue(Items.Owns(s, AlbumSets.MilestoneHat), "the Starry Hat at 40");
            Assert.AreEqual(1, s.Items.Count);
            Assert.AreEqual(3, s.Items[0].Tier, "at its shop tier");
            Assert.AreEqual(0, s.GoldenCarrots, "the golden carrots are the quests' to pay, not the album's");
            Assert.IsFalse(AlbumSets.HasFullFrame(s));

            Assert.IsEmpty(AlbumSets.ClaimMilestones(s, items), "nothing twice");
            Assert.AreEqual(1, s.Items.Count);

            MeetEveryDay(72);
            CollectionAssert.AreEqual(new[] { 72 }, AlbumSets.ClaimMilestones(s, items));
            Assert.IsTrue(AlbumSets.HasFullFrame(s), "the gold frame at 72");
            Assert.IsEmpty(AlbumSets.ClaimMilestones(s, items));
        }

        [Test]
        public void A_Player_Who_Has_The_Starry_Hat_Gets_Its_Price_In_Golden_Carrots()
        {
            Items.Grant(s, items[1], 3);
            MeetEveryDay(40);
            AlbumSets.ClaimMilestones(s, items);
            Assert.AreEqual(1, s.Items.Count, "no second hat");
            Assert.AreEqual(50, s.GoldenCarrots, "its price instead");
        }

        [Test]
        public void The_Kinds_Met_Quest_Reads_The_Album()
        {
            var q = new QuestDef { Id = "loai-10", Metric = QuestMetric.KindsMet, Target = 10, RewardCarrots = 300, RewardGolden = 3 };
            var quests = new List<QuestDef> { q };
            Quests.RollDaily(quests, defs, s, 1, Modifiers.None);
            MeetEveryDay(9);
            Assert.AreEqual(9, Quests.Progress(q, defs, s));
            Assert.IsFalse(Quests.CanClaim(q, defs, s));
            MeetEveryDay(10);
            Assert.IsTrue(Quests.TryClaim(q, defs, s, Modifiers.None, out _));
            Assert.AreEqual(3, s.GoldenCarrots);
            Assert.IsFalse(Quests.TryClaim(q, defs, s, Modifiers.None, out _), "paid once");
            Assert.AreEqual(3, s.GoldenCarrots);
            Assert.AreEqual(QuestMetric.RabbitsResting + 1, QuestMetric.KindsMet, "appended at the end, so saved quests keep their meaning");
        }

        [Test]
        public void The_Milestones_Given_Are_Saved_And_Read_Back()
        {
            MeetEveryDay(40);
            AlbumSets.ClaimMilestones(s, items);
            LoadResult back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s));
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            CollectionAssert.AreEquivalent(new[] { 10, 20, 40 }, back.State.AlbumMilestonesClaimed);
            Assert.IsEmpty(AlbumSets.ClaimMilestones(back.State, items), "a save loaded again gives nothing twice");

            // An older save has given nothing; numbers that are not milestones are dropped.
            string json = SaveCodec.Encode(defs, s).Replace("\"albumMilestones\":[", "\"albumMilestones\":[7,");
            CollectionAssert.DoesNotContain(SaveCodec.Decode(defs, json).State.AlbumMilestonesClaimed, 7);
            string old = SaveCodec.Encode(defs, GameState.NewGame(defs)).Replace(",\"albumMilestones\":[]", "");
            StringAssert.DoesNotContain("albumMilestones", old);
            Assert.IsEmpty(SaveCodec.Decode(defs, old).State.AlbumMilestonesClaimed);
            StringAssert.Contains("\"version\":14", SaveCodec.Encode(defs, s), "an additive field: the version stays");
        }
    }
}

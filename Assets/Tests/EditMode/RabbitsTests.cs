using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class RabbitsTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
        }

        Rabbit Add(int tier, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Name = "R" + s.NextRabbitId, Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        [TestCase("Bông", "Bông")]
        [TestCase("  Mây  ", "Mây")]
        [TestCase("Bánh\nTrôi", "Bánh Trôi")]
        [TestCase("a   b", "a b")]
        [TestCase("Một cái tên rất là dài", "Một cái tên")]
        [TestCase("\"quoted\"", "\"quoted\"")]
        public void CleanName_Tidies(string raw, string expected)
        {
            Assert.AreEqual(expected, Rabbits.CleanName(raw));
        }

        [TestCase(null)]
        [TestCase("")]
        [TestCase("   ")]
        [TestCase("\n\t")]
        public void CleanName_Refuses_Empty(string raw)
        {
            Assert.IsNull(Rabbits.CleanName(raw));
        }

        [Test]
        public void Rename_Keeps_The_Old_Name_When_The_New_One_Is_Empty()
        {
            Rabbit r = Add(0);
            r.Name = "Bông";
            Assert.IsFalse(Rabbits.Rename(s, r.Id, "   "));
            Assert.AreEqual("Bông", r.Name);
            Assert.IsTrue(Rabbits.Rename(s, r.Id, " Mochi "));
            Assert.AreEqual("Mochi", r.Name);
            Assert.IsFalse(Rabbits.Rename(s, 999, "X"));
            Assert.AreEqual("Mochi", SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.Rabbits[0].Name);
        }

        [Test]
        public void Odds_Add_Up_To_One()
        {
            double sum = 0;
            foreach (double o in Rabbits.Odds) sum += o;
            Assert.AreEqual(1, sum, 1e-12);
            Assert.AreEqual(Rabbits.TierCount, Rabbits.Odds.Length);
            Assert.AreEqual(Rabbits.TierCount, Rabbits.Multipliers.Length);
        }

        [TestCase(0.0, 0)]
        [TestCase(0.5999, 0)]
        [TestCase(0.60, 1)]
        [TestCase(0.849, 1)]
        [TestCase(0.85, 2)]
        [TestCase(0.95, 3)]
        [TestCase(0.99, 4)]
        [TestCase(0.9995, 5)]
        [TestCase(1.0, 0)]
        [TestCase(-3.0, 0)]
        [TestCase(double.NaN, 0)]
        public void RollTier(double roll, int tier)
        {
            Assert.AreEqual(tier, Rabbits.RollTier(roll));
        }

        [Test]
        public void Summon_Spends_A_Clover_And_Needs_One()
        {
            Assert.IsNull(Rabbits.Summon(s, 0.1, "A"));
            s.Clovers = 2;
            Rabbit r = Rabbits.Summon(s, 0.9, "Bông");
            Assert.AreEqual(2, r.Tier);
            Assert.AreEqual("Bông", r.Name);
            Assert.AreEqual(-1, r.Station);
            // One spent; any garden level the arrival brought would give one back.
            Assert.AreEqual(2 - Rabbits.SummonCost + (Garden.Level(s) - 1) * Garden.CloversPerLevel, s.Clovers);
            Assert.AreEqual(1, s.Rabbits.Count);
            Rabbit second = Rabbits.Summon(s, 0.1, "Mây");
            Assert.AreNotEqual(r.Id, second.Id);
        }

        [Test]
        public void Merge_Needs_Three_Of_A_Tier()
        {
            Add(0); Add(0); Add(1);
            Assert.IsFalse(Rabbits.CanMerge(s, 0));
            Assert.IsNull(Rabbits.Merge(s, 0));
            Assert.AreEqual(3, s.Rabbits.Count);
        }

        [Test]
        public void Merge_Makes_One_Of_The_Next_Tier()
        {
            Rabbit first = Add(0); Add(0); Add(0); Add(0);
            Rabbit merged = Rabbits.Merge(s, 0);
            Assert.AreEqual(1, merged.Tier);
            Assert.AreEqual(first.Name, merged.Name);
            Assert.AreEqual(2, s.Rabbits.Count);
            Assert.AreEqual(1, Rabbits.CountOfTier(s, 0));
        }

        [Test]
        public void Merge_Uses_Resting_Rabbits_First_And_Keeps_A_Station()
        {
            Rabbit working = Add(0, 0);
            Add(0); Add(0); Add(0);
            Rabbits.Merge(s, 0);
            Assert.IsTrue(s.Rabbits.Contains(working), "the working rabbit is left alone when three rest");

            Add(0); Add(0); // now: working + 2 resting silvers
            Rabbit merged = Rabbits.Merge(s, 0);
            Assert.AreEqual(0, merged.Station, "the new rabbit takes over the station");
            Assert.AreSame(merged, Rabbits.AtStation(s, 0));
        }

        [Test]
        public void The_Kind_Table_Is_Sound()
        {
            var ids = new HashSet<string>();
            foreach (RabbitKind k in RabbitKinds.All)
            {
                Assert.IsTrue(ids.Add(k.Id), k.Id + " twice");
                Assert.IsTrue(k.Tier >= 0 && k.Tier < Rabbits.TierCount, k.Id + " tier");
                Assert.AreNotEqual(RabbitTrade.None, k.Trade, k.Id + " has a trade");
                Assert.IsFalse(string.IsNullOrWhiteSpace(k.NameVi), k.Id + " Vietnamese name");
                Assert.IsFalse(string.IsNullOrWhiteSpace(k.NameEn), k.Id + " English name");
            }
            for (int tier = 0; tier < Rabbits.TierCount; tier++)
            {
                Assert.GreaterOrEqual(RabbitKinds.OfTier(tier).Count, 2, "tier " + tier + " has a choice");
                Assert.AreEqual(tier, RabbitKinds.Find(RabbitKinds.DefaultFor(tier)).Tier);
            }
            Assert.IsNull(RabbitKinds.Find("tho-la"));
            Assert.IsNull(RabbitKinds.Find(null));
        }

        [Test]
        public void Old_Saves_Keep_The_Same_Default_Kind_For_Each_Tier()
        {
            // Adding kinds must never change what a rabbit saved without one turns into.
            CollectionAssert.AreEqual(
                new[] { "tho-bong", "tho-cup", "tho-suong", "tho-oai-huong", "tho-mat-ong", "tho-hong-tra" },
                new[] { RabbitKinds.DefaultFor(0), RabbitKinds.DefaultFor(1), RabbitKinds.DefaultFor(2), RabbitKinds.DefaultFor(3), RabbitKinds.DefaultFor(4), RabbitKinds.DefaultFor(5) });
        }

        [TestCase(0.1, 0)]
        [TestCase(0.9, 2)]
        [TestCase(0.9995, 5)]
        public void Summon_Picks_Evenly_Among_The_Kinds_Of_The_Tier_It_Rolled(double roll, int tier)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(tier);
            for (int k = 0; k < kinds.Count; k++)
            {
                s.Clovers = 1;
                Rabbit r = Rabbits.Summon(s, roll, "A", (k + 0.5) / kinds.Count); // the middle of the k-th share
                Assert.AreEqual(tier, r.Tier);
                Assert.AreEqual(kinds[k].Id, r.Kind);
            }
        }

        [Test]
        public void A_Kind_Roll_Off_The_Ends_Still_Lands_Inside_The_Tier()
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(0);
            s.Clovers = 3;
            Assert.AreEqual(kinds[kinds.Count - 1].Id, Rabbits.Summon(s, 0.1, "A", 1.5).Kind);
            Assert.AreEqual(kinds[0].Id, Rabbits.Summon(s, 0.1, "A", double.NaN).Kind);
            Assert.AreEqual(kinds[0].Id, Rabbits.Summon(s, 0.1, "A", -3).Kind);
        }

        [Test]
        public void Summon_Without_A_Kind_Roll_Gives_The_Tier_Default()
        {
            s.Clovers = 1;
            Assert.AreEqual("tho-bong", Rabbits.Summon(s, 0.1, "A").Kind);
        }

        [Test]
        public void Merge_Gives_A_Kind_Of_The_Next_Tier()
        {
            IReadOnlyList<RabbitKind> next = RabbitKinds.OfTier(1);
            Add(0); Add(0); Add(0);
            Assert.AreEqual(next[0].Id, Rabbits.Merge(s, 0, 0.0).Kind);
            Add(0); Add(0); Add(0);
            Rabbit merged = Rabbits.Merge(s, 0, 0.999);
            Assert.AreEqual(1, merged.Tier);
            Assert.AreEqual(next[next.Count - 1].Id, merged.Kind);
        }

        [Test]
        public void Calling_A_Rabbit_Remembers_Its_Kind_And_Says_When_It_Is_New()
        {
            s.Clovers = 2;
            Assert.AreEqual(0, s.SeenKinds.Count);
            Rabbit first = Rabbits.Summon(s, 0.1, "A");
            Assert.IsTrue(first.FirstOfKind);
            Assert.IsTrue(s.SeenKinds.Contains("tho-bong"));
            Rabbit second = Rabbits.Summon(s, 0.1, "B");
            Assert.IsFalse(second.FirstOfKind, "the same kind again is not new");
            Assert.AreEqual(1, s.SeenKinds.Count);
        }

        [Test]
        public void A_Kind_Stays_Seen_After_Its_Rabbits_Are_Merged_Away()
        {
            s.Clovers = 3;
            Rabbits.Summon(s, 0.1, "A"); Rabbits.Summon(s, 0.1, "B"); Rabbits.Summon(s, 0.1, "C");
            Rabbit merged = Rabbits.Merge(s, 0, 0.0);
            Assert.IsTrue(merged.FirstOfKind);
            Assert.AreEqual(0, Rabbits.CountOfTier(s, 0), "all three silver rabbits are gone");
            Assert.IsTrue(s.SeenKinds.Contains("tho-bong"), "but the kind was met");
            Assert.IsTrue(s.SeenKinds.Contains(merged.Kind));
            Assert.AreEqual(2, s.SeenKinds.Count);
        }

        [Test]
        public void KindOf_Falls_Back_To_The_Tier_Default()
        {
            Assert.AreEqual("tho-sao", Rabbits.KindOf(new Rabbit { Tier = 4, Kind = "tho-sao" }));
            Assert.AreEqual("tho-mat-ong", Rabbits.KindOf(new Rabbit { Tier = 4 }), "no kind yet");
            Assert.AreEqual("tho-mat-ong", Rabbits.KindOf(new Rabbit { Tier = 4, Kind = "tho-bong" }), "a kind of another tier");
        }

        List<StationDef> TradeDefs()
        {
            var d = Fixture.Defs();
            d[0].Trade = RabbitTrade.Brewer;   // tea
            d[1].Trade = RabbitTrade.Gatherer; // leaf, the ingredient station
            d[2].Trade = RabbitTrade.Baker;    // jam
            return d;
        }

        Rabbit AddKind(string kind, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = RabbitKinds.Find(kind).Tier, Kind = kind, Name = kind, Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        [TestCase("tho-bong", RabbitTrade.Brewer)] [TestCase("tho-matcha", RabbitTrade.Brewer)] [TestCase("tho-hong-tra", RabbitTrade.Brewer)]
        [TestCase("tho-me", RabbitTrade.Baker)] [TestCase("tho-khoai-mon", RabbitTrade.Baker)] [TestCase("tho-mat-ong", RabbitTrade.Baker)]
        [TestCase("tho-cup", RabbitTrade.Gatherer)] [TestCase("tho-o-long", RabbitTrade.Gatherer)] [TestCase("tho-oai-huong", RabbitTrade.Gatherer)]
        [TestCase("tho-suong", RabbitTrade.Stargazer)] [TestCase("tho-sao", RabbitTrade.Stargazer)] [TestCase("tho-trang", RabbitTrade.Stargazer)]
        public void Every_Kind_Has_Its_Trade(string kind, RabbitTrade trade)
        {
            Assert.AreEqual(trade, RabbitKinds.Find(kind).Trade);
        }

        [Test]
        public void A_Rabbit_In_Its_Trade_Is_Worth_Half_Again()
        {
            var d = TradeDefs();
            Rabbit bong = AddKind("tho-bong", 0); // a brewer at the tea station
            Assert.IsTrue(Rabbits.InTrade(d, bong));
            Assert.AreEqual(1.1 * 1.5, Rabbits.StationMultiplier(d, s, 0), 1e-12);
            Assert.AreEqual(5 * 1.2 * 1.1 * 1.5, Economy.IncomePerCycle(d, s, 0).ToDouble(), 1e-9);

            Rabbits.Assign(s, bong.Id, 2); // the same rabbit at the jam pot: it works, without the bonus
            Assert.IsFalse(Rabbits.InTrade(d, bong));
            Assert.AreEqual(1.1, Rabbits.StationMultiplier(d, s, 2), 1e-12);
            Assert.AreEqual(1.1, Rabbits.WorkMultiplier(d, s, bong), 1e-12);

            Rabbits.Assign(s, bong.Id, -1);
            Assert.IsFalse(Rabbits.InTrade(d, bong), "resting");
        }

        [Test]
        public void A_Station_Without_A_Trade_Gives_No_Bonus()
        {
            Rabbit bong = AddKind("tho-bong", 0);
            Assert.IsFalse(Rabbits.InTrade(defs, bong));
            Assert.AreEqual(1.1, Rabbits.StationMultiplier(defs, s, 0), 1e-12);
        }

        [Test]
        public void A_Gatherer_Strengthens_What_Its_Ingredient_Station_Gives()
        {
            var d = TradeDefs();
            double plain = Economy.IncomePerCycle(d, s, 0).ToDouble();          // 5 x (1 + 0.2)
            AddKind("tho-cup", 1); // green gatherer at the leaf basket
            double boosted = Economy.IncomePerCycle(d, s, 0).ToDouble();
            Assert.AreEqual(5 * 1.2, plain, 1e-9);
            Assert.AreEqual(5 * (1 + 0.2 * 1.25 * 1.5), boosted, 1e-9);
        }

        [Test]
        public void AutoAssign_Counts_The_Trade_Bonus()
        {
            var d = TradeDefs();
            Rabbit matcha = AddKind("tho-matcha"), cup = AddKind("tho-cup"), me = AddKind("tho-me");
            Rabbits.AutoAssign(d, s);
            // jam earns most: the silver baker (1.1 x 1.5 = 1.65) beats either green rabbit there (1.25)
            Assert.AreEqual(2, me.Station);
            Assert.AreEqual(0, matcha.Station, "the brewer takes the tea");
            Assert.AreEqual(1, cup.Station, "the gatherer takes the leaves");
        }

        [Test]
        public void BestStationFor_Prefers_An_Empty_Station_Of_Its_Trade()
        {
            var d = TradeDefs();
            Rabbit cup = AddKind("tho-cup");
            Assert.AreEqual(1, Rabbits.BestStationFor(d, s, cup), "the leaf basket, though jam earns more");
            AddKind("tho-o-long", 1);
            Assert.AreEqual(2, Rabbits.BestStationFor(d, s, cup), "its own station is taken: the best empty one");
        }

        [Test]
        public void Top_Tier_Cannot_Merge()
        {
            Add(5); Add(5); Add(5);
            Assert.IsFalse(Rabbits.CanMerge(s, 5));
            Assert.IsNull(Rabbits.Merge(s, 5));
            Assert.IsFalse(Rabbits.CanMerge(s, -1));
        }

        [Test]
        public void Assign_Swaps_Out_Whoever_Was_There()
        {
            Rabbit a = Add(0), b = Add(3);
            Assert.IsTrue(Rabbits.Assign(s, a.Id, 0));
            Assert.IsTrue(Rabbits.Assign(s, b.Id, 0));
            Assert.AreEqual(-1, a.Station);
            Assert.AreSame(b, Rabbits.AtStation(s, 0));
            Assert.IsTrue(Rabbits.Assign(s, b.Id, -1));
            Assert.IsNull(Rabbits.AtStation(s, 0));
        }

        [Test]
        public void Cannot_Assign_To_A_Closed_Or_Missing_Station()
        {
            s.Levels = new[] { 1, 0, 0 };
            Rabbit a = Add(0);
            Assert.IsFalse(Rabbits.Assign(s, a.Id, 1));
            Assert.IsFalse(Rabbits.Assign(s, a.Id, 99));
            Assert.IsFalse(Rabbits.Assign(s, 12345, 0));
            Assert.AreEqual(-1, a.Station);
        }

        [Test]
        public void Working_Rabbit_Multiplies_A_Brew_Station()
        {
            s.Levels = new[] { 4, 0, 0 };
            Add(3, 0); // purple ×2
            Assert.AreEqual(8, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
        }

        [Test]
        public void Rabbit_At_An_Ingredient_Station_Strengthens_Its_Bonus()
        {
            s.Levels = new[] { 4, 5, 0 }; // leaf level 5: boost 1.5
            Add(3, 1); // ×2 on the bonus part: 1 + 0.5 × 2 = 2
            Assert.AreEqual(4 * 2.0, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
        }

        [Test]
        public void No_Rabbits_Changes_Nothing()
        {
            Assert.AreEqual(1, Rabbits.StationMultiplier(s, 0));
            Assert.AreEqual(5 * 1.2, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
        }

        [Test]
        public void AutoAssign_Puts_The_Best_Rabbit_On_The_Biggest_Earner()
        {
            // jam (80 per 5 s) out-earns tea (5 per 1.5 s); leaf is an ingredient station
            Rabbit silver = Add(0), gold = Add(4), blue = Add(2), extra = Add(0);
            Rabbits.AutoAssign(defs, s);
            Assert.AreEqual(2, gold.Station);
            Assert.AreEqual(0, blue.Station);
            Assert.AreEqual(1, silver.Station);
            Assert.AreEqual(-1, extra.Station);
        }

        [Test]
        public void BestStationFor_Prefers_Empty_Then_Weakest()
        {
            Rabbit a = Add(1);
            Assert.AreEqual(2, Rabbits.BestStationFor(defs, s, a), "empty, biggest earner first");
            Add(0, 2); Add(2, 0); Add(1, 1);
            Assert.AreEqual(2, Rabbits.BestStationFor(defs, s, a), "replaces the silver");
            Rabbit weak = Add(0);
            Assert.AreEqual(-1, Rabbits.BestStationFor(defs, s, weak), "nobody weaker to replace");
        }

        [Test]
        public void Save_RoundTrip_Keeps_Rabbits_And_Their_Stations()
        {
            Rabbit a = Add(4, 2);
            Add(0);
            a.Name = "Bánh \"Trôi\"";
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, r.Rabbits.Count);
            Assert.AreEqual(4, r.Rabbits[0].Tier);
            Assert.AreEqual("Bánh \"Trôi\"", r.Rabbits[0].Name);
            Assert.AreEqual(2, r.Rabbits[0].Station);
            Assert.AreEqual(-1, r.Rabbits[1].Station);
            Assert.AreEqual(s.NextRabbitId, r.NextRabbitId);
        }

        [Test]
        public void Load_Repairs_Bad_Rabbit_Data()
        {
            const string json = "{\"version\":3,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}],\"rabbits\":[" +
                "{\"id\":1,\"tier\":99,\"name\":\"A\",\"station\":\"tea\"}," +
                "{\"id\":2,\"tier\":1,\"name\":\"B\",\"station\":\"tea\"}," +     // second rabbit at the same station
                "{\"id\":2,\"tier\":1,\"name\":\"dup\",\"station\":\"\"}," +       // duplicate id
                "{\"id\":3,\"tier\":-4,\"name\":7,\"station\":\"jam\"}," +         // closed station, bad name
                "\"junk\"]}";
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(3, r.Rabbits.Count);
            Assert.AreEqual(5, r.Rabbits[0].Tier);
            Assert.AreEqual(0, r.Rabbits[0].Station);
            Assert.AreEqual(-1, r.Rabbits[1].Station);
            Assert.AreEqual(0, r.Rabbits[2].Tier);
            Assert.AreEqual("", r.Rabbits[2].Name);
            Assert.AreEqual(-1, r.Rabbits[2].Station);
            Assert.AreEqual(3, r.NextRabbitId);
        }
    }
}

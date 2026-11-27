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

            Add(0); Add(0);
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
            CollectionAssert.AreEqual(
                new[] { "tho-bong", "tho-cup", "tho-suong", "tho-oai-huong", "tho-mat-ong", "tho-hong-tra" },
                new[] { RabbitKinds.DefaultFor(0), RabbitKinds.DefaultFor(1), RabbitKinds.DefaultFor(2), RabbitKinds.DefaultFor(3), RabbitKinds.DefaultFor(4), RabbitKinds.DefaultFor(5) });
        }

        [TestCase(0.1, 0)]
        [TestCase(0.9, 2)]
        [TestCase(0.9995, 5)]
        public void Summon_Picks_Evenly_Among_The_Kinds_Of_The_Tier_It_Rolled_Once_All_Are_Met(double roll, int tier)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(tier);
            foreach (RabbitKind kind in kinds) s.SeenKinds.Add(kind.Id);
            for (int k = 0; k < kinds.Count; k++)
            {
                s.Clovers = 1;
                s.DupStreak = 0;
                Rabbit r = Rabbits.Summon(s, roll, "A", (k + 0.5) / kinds.Count);
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
            d[0].Trade = RabbitTrade.Brewer;
            d[1].Trade = RabbitTrade.Gatherer;
            d[2].Trade = RabbitTrade.Baker;
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
            Rabbit bong = AddKind("tho-bong", 0);
            Assert.IsTrue(Rabbits.InTrade(d, bong));
            Assert.AreEqual(1.1 * 1.5, Rabbits.StationMultiplier(d, s, 0), 1e-12);
            Assert.AreEqual(5 * 1.2 * 1.1 * 1.5, Economy.IncomePerCycle(d, s, 0).ToDouble(), 1e-9);

            Rabbits.Assign(s, bong.Id, 2);
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
            double plain = Economy.IncomePerCycle(d, s, 0).ToDouble();
            AddKind("tho-cup", 1);
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
            Add(3, 0);
            Assert.AreEqual(8, Economy.IncomePerCycle(defs, s, 0).ToDouble(), 1e-9);
        }

        [Test]
        public void Rabbit_At_An_Ingredient_Station_Strengthens_Its_Bonus()
        {
            s.Levels = new[] { 4, 5, 0 };
            Add(3, 1);
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
        public void A_New_Player_Is_Given_One_Cotton_Rabbit_At_The_Teapot()
        {
            s = GameState.NewGame(defs);
            Rabbit gift = Rabbits.GiftFirst(s, "Bông");
            Assert.IsNotNull(gift);
            Assert.AreEqual(1, s.Rabbits.Count);
            Assert.AreEqual("tho-bong", gift.Kind);
            Assert.AreEqual(0, gift.Tier);
            Assert.AreEqual(0, gift.Station, "it already stands at the teapot");
            Assert.AreEqual("Bông", gift.Name);
            Assert.AreEqual(gift.Id, s.NextRabbitId);
            Assert.IsTrue(s.SeenKinds.Contains("tho-bong"), "and is in the album");
            Assert.IsNull(Rabbits.GiftFirst(s, "Mây"), "one gift only");
            Assert.AreEqual(1, s.Rabbits.Count);
        }

        [Test]
        public void A_Garden_That_Already_Has_Rabbits_Gets_No_Gift()
        {
            Add(2);
            Assert.IsNull(Rabbits.GiftFirst(s, "Bông"));
            Assert.AreEqual(1, s.Rabbits.Count);
        }

        [Test]
        public void An_Old_Save_With_No_Rabbits_Loads_With_None()
        {
            const string json = "{\"version\":8,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}],\"rabbits\":[]}";
            GameState r = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(0, r.Rabbits.Count, "the gift is for a new garden, not an old one");
        }

        [TestCase(1, 0)]
        [TestCase(9, 0)]
        [TestCase(10, 1)]
        [TestCase(23, 2)]
        [TestCase(60, 6)]
        public void A_Rabbit_Earns_A_Star_Every_Ten_Levels(int level, int stars)
        {
            Assert.AreEqual(stars, Rabbits.Stars(level));
            Assert.AreEqual(stars, Rabbits.Stars(new Rabbit { Level = level }));
        }

        [Test]
        public void Stars_Stop_At_The_Tier_Cap_And_A_Climb_Counts_The_Marks_It_Passes()
        {
            Assert.AreEqual(1, Rabbits.Stars(Rabbits.LevelCaps[0]), "silver caps at ten: one star at most");
            Assert.AreEqual(6, Rabbits.Stars(Rabbits.LevelCaps[5]), "red caps at sixty: six");
            Assert.AreEqual(2, Rabbits.StarsEarned(1, 23));
            Assert.AreEqual(0, Rabbits.StarsEarned(10, 19));
            Assert.AreEqual(1, Rabbits.StarsEarned(9, 10));
            Assert.AreEqual(0, Rabbits.StarsEarned(30, 20), "never negative");
        }

        [Test]
        public void A_Kind_Arriving_For_The_First_Time_Is_Marked_New_Until_It_Is_Looked_At_And_The_Save_Keeps_The_Mark()
        {
            s.Clovers = 3;
            Rabbit first = Rabbits.Summon(s, 0d, "A", 0d);
            Assert.IsTrue(first.FirstOfKind);
            Assert.IsTrue(s.FreshKinds.Contains(first.Kind), "new in the album");
            Rabbits.Summon(s, 0d, "B", 0d);
            Assert.AreEqual(1, s.FreshKinds.Count, "the same kind again is not new again");

            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.IsTrue(r.FreshKinds.Contains(first.Kind), "closing the game does not take the mark off");
            Assert.IsTrue(Rabbits.LookAt(r, first.Kind));
            Assert.IsFalse(r.FreshKinds.Contains(first.Kind));
            Assert.IsFalse(Rabbits.LookAt(r, first.Kind), "nothing left to take off");

            GameState odd = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[],\"seenKinds\":[\"tho-bong\"],\"freshKinds\":[\"tho-bong\",\"tho-me\"]}").State;
            CollectionAssert.AreEquivalent(new[] { "tho-bong" }, odd.FreshKinds, "a mark only on a kind that is in the album");
        }

        [Test]
        public void Load_Repairs_Bad_Rabbit_Data()
        {
            const string json = "{\"version\":3,\"stations\":[{\"id\":\"tea\",\"level\":3,\"progress\":0}],\"rabbits\":[" +
                "{\"id\":1,\"tier\":99,\"name\":\"A\",\"station\":\"tea\"}," +
                "{\"id\":2,\"tier\":1,\"name\":\"B\",\"station\":\"tea\"}," +
                "{\"id\":2,\"tier\":1,\"name\":\"dup\",\"station\":\"\"}," +
                "{\"id\":3,\"tier\":-4,\"name\":7,\"station\":\"jam\"}," +
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
        [Test]
        public void Petting_Pays_Tea_Power_Once_A_Day_And_Raises_Affection()
        {
            Rabbit r = Add(0, 0);
            s.Energy = 10;
            PetResult first = Rabbits.Pet(s, r, 100);
            Assert.IsTrue(first.Found);
            Assert.IsTrue(first.FirstToday);
            Assert.AreEqual(Rabbits.PetEnergy, first.Energy, 1e-9);
            Assert.AreEqual(10 + Rabbits.PetEnergy, s.Energy, 1e-9);
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);
            Assert.AreEqual(100, r.PetDay);

            PetResult again = Rabbits.Pet(s, r, 100);
            Assert.IsTrue(again.Found);
            Assert.IsFalse(again.FirstToday, "the second stroke of the day is for love alone");
            Assert.AreEqual(0d, again.Energy, 1e-9);
            Assert.AreEqual(10 + Rabbits.PetEnergy, s.Energy, 1e-9);
            Assert.AreEqual(Rabbits.PetPoints, r.Affection);

            Assert.IsTrue(Rabbits.Pet(s, r, 101).FirstToday, "a new day");
            Assert.AreEqual(2 * Rabbits.PetPoints, r.Affection);
            double energy = s.Energy;
            Assert.IsFalse(Rabbits.Pet(s, r, 50).FirstToday, "a clock that went backwards is not another day");
            Assert.AreEqual(2 * Rabbits.PetPoints, r.Affection);
            Assert.AreEqual(101, r.PetDay);
            Assert.AreEqual(energy, s.Energy, 1e-9);
        }

        [Test]
        public void Affection_Stops_At_Its_Cap_And_Nobody_Pets_A_Rabbit_That_Is_Not_There()
        {
            Rabbit r = Add(1);
            r.Affection = Rabbits.AffectionMax - Rabbits.PetPoints - 3;
            for (long day = 1; day <= 4; day++) Rabbits.Pet(s, r, day);
            Assert.AreEqual(Rabbits.AffectionMax, r.Affection);
            Assert.IsTrue(Rabbits.Pet(s, r, 99).FirstToday, "still a first stroke of the day, and still worth tea power");
            Assert.AreEqual(Rabbits.AffectionMax, r.Affection);

            s.Energy = 10;
            PetResult none = Rabbits.Pet(s, null, 5);
            Assert.IsFalse(none.Found);
            Assert.IsFalse(none.FirstToday);
            Assert.AreEqual(0d, none.Energy, 1e-9);
            Assert.AreEqual(10, s.Energy, 1e-9);
        }

        [Test]
        public void Waking_A_Dozing_Rabbit_Pays_The_Doze_Reward_However_Often_It_Naps()
        {
            Rabbit r = Add(0, 0);
            s.Energy = 10;
            PetResult wake = Rabbits.Pet(s, r, 1, wake: true);
            Assert.IsTrue(wake.FirstToday);
            Assert.AreEqual(Rabbits.DozeEnergy, wake.Energy, 1e-9, "a wake pays the doze reward, not the stroke's");
            Assert.AreEqual(Rabbits.DozeEnergy, Rabbits.Pet(s, r, 1, wake: true).Energy, 1e-9, "the second nap of the day is still worth waking");
            Assert.AreEqual(10 + 2 * Rabbits.DozeEnergy, s.Energy, 1e-9);
            Assert.AreEqual(Rabbits.PetPoints, r.Affection, "one day's points, however many naps");

            s.Energy = Energy.Max - 1;
            Rabbits.Pet(s, r, 2, wake: true);
            Assert.AreEqual(Energy.Max, s.Energy, 1e-9, "never past the top of the bar");
        }

        [Test]
        public void Close_Friends_Say_A_Line_At_Three_Hearts_And_Give_A_Clover_At_Five_Once()
        {
            Rabbit r = Add(2);
            s.Clovers = 0;
            long lineDay = Rabbits.PointsFor(Rabbits.LineHearts) / Rabbits.PetPoints, giftDay = Rabbits.PointsFor(Rabbits.GiftHearts) / Rabbits.PetPoints;
            for (long day = 1; day <= giftDay + 2; day++)
            {
                PetResult pet = Rabbits.Pet(s, r, day);
                Assert.AreEqual(day == lineDay, pet.LineUnlocked, "the line comes on day " + lineDay + ", not " + day);
                Assert.AreEqual(day == giftDay, pet.GiftGiven, "the gift comes on day " + giftDay + ", not " + day);
                Assert.IsFalse(Rabbits.Pet(s, r, day).LineUnlocked, "never twice on one day");
                Assert.IsFalse(Rabbits.Pet(s, r, day).GiftGiven);
            }
            Assert.AreEqual(Rabbits.GiftHearts, Rabbits.HeartsOf(r));
            Assert.AreEqual(Rabbits.AffectionGiftClovers, s.Clovers, "one clover, paid exactly once");
        }

        [Test]
        public void Merging_Keeps_The_Strongest_Bond()
        {
            Rabbit a = Add(0), b = Add(0), c = Add(0);
            a.Affection = 1; b.Affection = 4; b.PetDay = 30; c.Affection = 2; c.PetDay = 31;
            Rabbit merged = Rabbits.Merge(s, 0);
            Assert.AreEqual(4, merged.Affection, "the love is not lost");
            Assert.AreEqual(31, merged.PetDay);
        }
    }
}

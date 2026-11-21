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
            Assert.AreEqual(1, s.Clovers);
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

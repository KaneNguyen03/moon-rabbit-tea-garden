using System.Collections.Generic;
using NUnit.Framework;
using VuonTra.EditorTools;

namespace VuonTra.Core.Tests
{
    public class ChatterTests
    {
        static readonly RabbitTrade[] Trades = { RabbitTrade.Brewer, RabbitTrade.Baker, RabbitTrade.Gatherer, RabbitTrade.Stargazer };
        static readonly DayPhase[] Phases = { DayPhase.Dusk, DayPhase.Night, DayPhase.Dawn, DayPhase.Day };
        static readonly Season[] AllSeasons = { Season.None, Season.MidAutumn, Season.Tet, Season.Christmas };

        [Test]
        public void A_Working_Brewer_By_Day_Has_Its_Four_Trade_Lines_The_Four_For_Anyone_And_The_Two_For_The_Day()
        {
            List<string> keys = Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, Season.None, false, false);
            CollectionAssert.AreEquivalent(new[]
            {
                "chatter.trade.Brewer.0", "chatter.trade.Brewer.1", "chatter.trade.Brewer.2", "chatter.trade.Brewer.3",
                "chatter.any.0", "chatter.any.1", "chatter.any.2", "chatter.any.3",
                "chatter.phase.Day.0", "chatter.phase.Day.1",
            }, keys);
        }

        [Test]
        public void The_Phase_Lines_Are_Always_The_Hours_Own()
        {
            foreach (DayPhase phase in Phases)
            foreach (bool resting in new[] { false, true })
            {
                List<string> keys = Chatter.KeysFor(RabbitTrade.Baker, resting, phase, Season.None, false, false);
                var phaseKeys = keys.FindAll(k => k.StartsWith("chatter.phase."));
                CollectionAssert.AreEquivalent(new[] { "chatter.phase." + phase + ".0", "chatter.phase." + phase + ".1" }, phaseKeys, phase + (resting ? " resting" : " working"));
            }
        }

        [Test]
        public void A_Resting_Rabbit_Only_Says_The_Lines_For_Anyone_And_The_Hours()
        {
            foreach (RabbitTrade trade in Trades)
            foreach (DayPhase phase in Phases)
            foreach (Season season in AllSeasons)
            foreach (bool flag in new[] { false, true })
            {
                List<string> keys = Chatter.KeysFor(trade, true, phase, season, flag, flag);
                Assert.AreEqual(Chatter.AnyLines + Chatter.PhaseLines, keys.Count, trade + " " + phase + " " + season);
                foreach (string key in keys)
                {
                    Assert.IsFalse(Chatter.IsTradeLine(key), "a resting rabbit never talks shop: " + key);
                    Assert.IsTrue(key.StartsWith("chatter.any.") || key.StartsWith("chatter.phase."), key);
                }
            }
        }

        [Test]
        public void Each_Trade_Has_Its_Own_Four_And_A_Rabbit_Of_No_Trade_Has_None()
        {
            foreach (RabbitTrade trade in Trades)
            {
                List<string> keys = Chatter.KeysFor(trade, false, DayPhase.Night, Season.None, false, false);
                var mine = keys.FindAll(Chatter.IsTradeLine);
                Assert.AreEqual(Chatter.TradeLines, mine.Count, trade.ToString());
                foreach (string key in mine) StringAssert.StartsWith("chatter.trade." + trade + ".", key);
            }
            Assert.IsEmpty(Chatter.KeysFor(RabbitTrade.None, false, DayPhase.Night, Season.None, false, false).FindAll(Chatter.IsTradeLine));
        }

        [Test]
        public void A_Season_Line_Only_In_Its_Season()
        {
            Assert.IsEmpty(Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, Season.None, false, false).FindAll(k => k.StartsWith("chatter.season.")));
            foreach (Season season in new[] { Season.MidAutumn, Season.Tet, Season.Christmas })
            {
                var lines = Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, season, false, false).FindAll(k => k.StartsWith("chatter.season."));
                CollectionAssert.AreEqual(new[] { "chatter.season." + season }, lines);
            }
        }

        [Test]
        public void A_Boosted_Station_Names_Its_Booster_And_A_Dressed_Rabbit_Shows_Off()
        {
            List<string> plain = Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, Season.None, false, false);
            CollectionAssert.DoesNotContain(plain, Chatter.BoostKey);
            CollectionAssert.DoesNotContain(plain, Chatter.DressedKey);
            List<string> boosted = Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, Season.None, true, false);
            CollectionAssert.Contains(boosted, Chatter.BoostKey);
            Assert.AreEqual(plain.Count + 1, boosted.Count);
            List<string> dressed = Chatter.KeysFor(RabbitTrade.Brewer, false, DayPhase.Day, Season.None, false, true);
            CollectionAssert.Contains(dressed, Chatter.DressedKey);
            Assert.AreEqual(plain.Count + 1, dressed.Count);
        }

        [Test]
        public void Pick_Reaches_Every_Line_And_Stays_In_The_List()
        {
            List<string> keys = Chatter.KeysFor(RabbitTrade.Gatherer, false, DayPhase.Dawn, Season.Tet, true, true);
            var seen = new HashSet<string>();
            for (int i = 0; i < 1000; i++) seen.Add(Chatter.Pick(keys, i / 1000.0));
            Assert.AreEqual(keys.Count, seen.Count, "every line can come up");
            Assert.AreEqual(keys[0], Chatter.Pick(keys, 0d));
            Assert.AreEqual(keys[keys.Count - 1], Chatter.Pick(keys, 0.99999));
            Assert.AreEqual(keys[keys.Count - 1], Chatter.Pick(keys, 1d), "a roll of one is still in the list");
            Assert.IsNull(Chatter.Pick(new List<string>(), 0.5));
        }

        [Test]
        public void The_Wait_Between_Lines_Is_Forty_To_Seventy_Seconds_And_A_Line_Stays_Two_And_A_Half()
        {
            Assert.AreEqual(40f, Chatter.Wait(0d), 1e-4);
            Assert.AreEqual(70f, Chatter.Wait(1d), 1e-4);
            for (double r = 0; r <= 1d; r += 0.05) Assert.That(Chatter.Wait(r), Is.InRange(40f, 70f));
            Assert.AreEqual(2.5f, Chatter.Seconds, 1e-6);
        }

        [Test]
        public void The_Booster_Is_An_Open_Ingredient_Station_That_Feeds_This_One()
        {
            List<StationDef> defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 0, 3 };
            Assert.AreEqual(-1, Chatter.BoosterOf(defs, s, 0), "the leaf station is not open yet");
            s.Levels[1] = 1;
            Assert.AreEqual(1, Chatter.BoosterOf(defs, s, 0), "the leaf station feeds the tea");
            Assert.AreEqual(-1, Chatter.BoosterOf(defs, s, 2), "nothing feeds the jam");
            Assert.AreEqual(-1, Chatter.BoosterOf(defs, s, 1), "an ingredient station is not boosted itself");
            Assert.AreEqual(-1, Chatter.BoosterOf(defs, s, 7), "a station that is not there");
        }

        [Test]
        public void Every_Line_Is_Written_In_Both_Languages()
        {
            var table = new Dictionary<string, string[]>();
            string[,] all = L10nStrings.All;
            for (int i = 0; i < all.GetLength(0); i++) table[all[i, 0]] = new[] { all[i, 1], all[i, 2] };
            int count = 0;
            foreach (string key in Chatter.AllKeys)
            {
                count++;
                Assert.IsTrue(table.ContainsKey(key), "missing " + key);
                Assert.IsNotEmpty(table[key][0], key + " vi");
                Assert.IsNotEmpty(table[key][1], key + " en");
            }
            Assert.AreEqual(16 + 4 + 8 + 3 + 2, count);
            StringAssert.Contains("{0}", table[Chatter.BoostKey][0], "the booster's name goes in the line");
            StringAssert.Contains("{0}", table[Chatter.BoostKey][1]);
            var known = new HashSet<string>(Chatter.AllKeys);
            foreach (RabbitTrade trade in Trades)
            foreach (DayPhase phase in Phases)
            foreach (Season season in AllSeasons)
            foreach (bool resting in new[] { false, true })
                foreach (string key in Chatter.KeysFor(trade, resting, phase, season, true, true))
                    Assert.IsTrue(known.Contains(key), key);
        }
    }
}

using System;
using System.Collections.Generic;
using NUnit.Framework;
using VuonTra.Core;

namespace VuonTra.Core.Tests
{
    public class SeasonTests
    {
        static bool Has(IReadOnlyList<RabbitKind> list, RabbitKind kind) { foreach (RabbitKind k in list) if (k == kind) return true; return false; }

        static long Day(int year, int month, int day) => (long)Math.Floor(new DateTimeOffset(year, month, day, 0, 0, 0, TimeSpan.Zero).ToUnixTimeSeconds() / 86400d);

        [TestCase(2026, 8, 31, Season.None)]
        [TestCase(2026, 9, 1, Season.MidAutumn)]
        [TestCase(2026, 9, 30, Season.MidAutumn)]
        [TestCase(2026, 10, 1, Season.None)]
        [TestCase(2026, 12, 1, Season.Christmas)]
        [TestCase(2026, 12, 31, Season.Christmas)]
        [TestCase(2027, 1, 1, Season.None)]
        [TestCase(2027, 1, 14, Season.None)]
        [TestCase(2027, 1, 15, Season.Tet)]
        [TestCase(2027, 2, 28, Season.Tet)]
        [TestCase(2028, 2, 29, Season.Tet)]
        [TestCase(2028, 3, 1, Season.None)]
        public void The_Seasons_Run_Between_Their_Dates(int y, int m, int d, Season expected)
        {
            Assert.AreEqual(expected, Seasons.SeasonOf(Day(y, m, d)));
        }

        [Test]
        public void The_Last_Day_Of_A_Season_Is_Known()
        {
            Assert.IsTrue(Seasons.IsLastDay(Day(2026, 9, 30)));
            Assert.IsFalse(Seasons.IsLastDay(Day(2026, 9, 29)));
            Assert.IsTrue(Seasons.IsLastDay(Day(2028, 2, 29)), "a leap year's Tết ends on the 29th");
            Assert.IsFalse(Seasons.IsLastDay(Day(2026, 5, 5)), "no season, no last day");
        }

        [Test]
        public void Each_Season_Has_One_Gold_Kind_That_Only_Its_Season_Brings()
        {
            foreach (Season season in new[] { Season.MidAutumn, Season.Tet, Season.Christmas })
            {
                RabbitKind kind = Seasons.KindOf(season);
                Assert.IsNotNull(kind, season.ToString());
                Assert.AreEqual(4, kind.Tier, "gold, no stronger");
                Assert.IsFalse(Has(RabbitKinds.OfTier(4), kind), "not among the everyday kinds");
                Assert.IsTrue(Has(RabbitKinds.OfTier(4, season), kind), "but there in its season");
            }
            Assert.AreEqual(3, RabbitKinds.Seasonal().Count);
            Assert.AreEqual(RabbitTrade.Stargazer, Seasons.KindOf(Season.MidAutumn).Trade);
            Assert.AreEqual(RabbitTrade.Brewer, Seasons.KindOf(Season.Tet).Trade);
            Assert.AreEqual(RabbitTrade.Baker, Seasons.KindOf(Season.Christmas).Trade);
            for (int i = 0; i < 200; i++) Assert.IsNull(RabbitKinds.Find(RabbitKinds.Pick(4, i / 200d)).Season == Season.None ? null : "x");
        }

        [Test]
        public void The_Shop_Visitor_Is_The_Season_Kind_About_A_Third_Of_The_Days_In_Season_And_Never_Outside()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            s.Profile.Code = "THO-7K3M9Q";
            int seasonal = 0, days = 0;
            for (long day = Day(2026, 9, 1); day <= Day(2026, 9, 30); day++)
            {
                days++;
                if (Shop.VisitorFor(s, day).Kind == "tho-den-ong-sao") seasonal++;
            }
            Assert.That(seasonal, Is.InRange(4, 16), "about a third of thirty days: " + seasonal);
            for (long day = Day(2026, 10, 1); day <= Day(2026, 11, 30); day++)
                Assert.AreNotEqual("tho-den-ong-sao", Shop.VisitorFor(s, day).Kind, "out of season");
            int hits = 0, tries = 0;
            foreach (string code in new[] { "THO-AAAAAA", "THO-BBBBBB", "THO-CCCCCC", "THO-DDDDDD", "THO-EEEEEE", "THO-FFFFFF", "THO-GGGGGG", "THO-HHHHHH", "THO-JJJJJJ", "THO-KKKKKK" })
            {
                s.Profile.Code = code;
                for (long day = Day(2026, 9, 1); day <= Day(2026, 9, 30); day++) { tries++; if (Shop.VisitorFor(s, day).Kind == "tho-den-ong-sao") hits++; }
            }
            Assert.That(hits / (double)tries, Is.InRange(0.22, 0.45), "share " + hits + "/" + tries);
        }

        [Test]
        public void The_Last_Day_Gift_Brings_The_Season_Kind_Once_To_A_Garden_Without_It()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            long last = Day(2026, 9, 30);
            Shop.ClaimDailyGift(s, last - 1, "A", out Rabbit none);
            Assert.IsNull(none, "not before the last day");
            int gift = Shop.ClaimDailyGift(s, last, "Sao", out Rabbit rabbit);
            Assert.Greater(gift, 0);
            Assert.IsNotNull(rabbit);
            Assert.AreEqual("tho-den-ong-sao", Rabbits.KindOf(rabbit));
            Assert.IsTrue(Seasons.HasSeasonRabbit(s, Season.MidAutumn));
            Assert.AreEqual(0, Shop.ClaimDailyGift(s, last, "Sao", out Rabbit again), "the day is paid");
            Assert.IsNull(again);
            Shop.ClaimDailyGift(s, Day(2027, 9, 30), "Sao", out Rabbit next);
            Assert.IsNull(next);
            Assert.AreEqual(1, s.Rabbits.Count);
        }

        [Test]
        public void A_Golden_Call_In_Season_Brings_The_Season_Kind_Half_The_Time_Its_Tier_Comes_Up()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            long inSeason = Day(2026, 12, 10), outSeason = Day(2026, 7, 10);
            double goldRoll = 0.001;
            for (double r = 0d; r < 1d; r += 0.001) if (Rabbits.RollTier(r, Shop.GoldenOdds) == 4) { goldRoll = r; break; }
            Assert.AreEqual(4, Rabbits.RollTier(goldRoll, Shop.GoldenOdds));
            s.GoldenCarrots = 1000;
            Rabbit a = Shop.SummonGolden(s, goldRoll, "A", 0.25, inSeason);
            Assert.AreEqual("tho-noel", Rabbits.KindOf(a), "kind roll under a half: the season kind");
            Rabbit b = Shop.SummonGolden(s, goldRoll, "B", 0.75, inSeason);
            Assert.AreNotEqual("tho-noel", Rabbits.KindOf(b), "over a half: an everyday gold kind");
            Rabbit c = Shop.SummonGolden(s, goldRoll, "C", 0.25, outSeason);
            Assert.AreNotEqual("tho-noel", Rabbits.KindOf(c), "out of season, never");
            Assert.AreEqual(1000 - 3 * Shop.GoldenSummonCost, s.GoldenCarrots);
        }

        [Test]
        public void A_Golden_Call_In_Season_That_Misses_The_Season_Kind_Can_Bring_Every_Everyday_Gold_Kind()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            long inSeason = Day(2026, 12, 10);
            double goldRoll = 0.001;
            for (double r = 0d; r < 1d; r += 0.001) if (Rabbits.RollTier(r, Shop.GoldenOdds) == 4) { goldRoll = r; break; }
            s.GoldenCarrots = 1000000;
            var kinds = new HashSet<string>();
            for (double k = Seasons.GoldenCallShare; k < 1d; k += 0.001) kinds.Add(Rabbits.KindOf(Shop.SummonGolden(s, goldRoll, "G", k, inSeason)));
            var everyday = new HashSet<string>();
            for (double k = 0d; k < 1d; k += 0.001) everyday.Add(RabbitKinds.Pick(4, k));
            Assert.AreEqual(10, everyday.Count, "ten everyday gold kinds");
            CollectionAssert.AreEquivalent(everyday, kinds, "the other half brings all ten, not only the upper five");
            CollectionAssert.DoesNotContain(kinds, "tho-noel");
        }

        [Test]
        public void The_Day_The_Game_Goes_By_Never_Goes_Back()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            Assert.AreEqual(100, Seasons.Clamp(s, 100));
            Assert.AreEqual(105, Seasons.Clamp(s, 105));
            Assert.AreEqual(105, Seasons.Clamp(s, 90), "a clock set back stays at the latest day seen");
            Assert.AreEqual(105, s.MaxDaySeen);
            Assert.AreEqual(106, Seasons.Clamp(s, 106));
            GameState back = SaveCodec.Decode(Fixture.Defs(), SaveCodec.Encode(Fixture.Defs(), s)).State;
            Assert.AreEqual(106, back.MaxDaySeen, "kept in the save");
            Assert.AreEqual(-1, SaveCodec.Decode(Fixture.Defs(), "{\"version\":14,\"stations\":[]}").State.MaxDaySeen, "an old save starts with the first day it sees");
        }

        [Test]
        public void A_Season_Is_Greeted_Once_A_Year_On_The_First_Day_The_Game_Sees_Of_It()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            Assert.IsFalse(Seasons.ShouldGreet(s, Day(2026, 8, 20)), "no season, no greeting");
            Assert.IsTrue(Seasons.ShouldGreet(s, Day(2026, 9, 3)), "first day seen of Mid-Autumn");
            Assert.IsFalse(Seasons.ShouldGreet(s, Day(2026, 9, 4)), "not again this season");
            Assert.IsFalse(Seasons.ShouldGreet(s, Day(2026, 9, 30)));
            Assert.IsTrue(Seasons.ShouldGreet(s, Day(2026, 12, 1)), "a new season is greeted");
            Assert.IsTrue(Seasons.ShouldGreet(s, Day(2027, 1, 20)), "Tết straddles the year end but is one season");
            Assert.IsFalse(Seasons.ShouldGreet(s, Day(2027, 2, 10)));
            Assert.IsTrue(Seasons.ShouldGreet(s, Day(2027, 9, 1)), "next year's Mid-Autumn is greeted again");
            GameState back = SaveCodec.Decode(Fixture.Defs(), SaveCodec.Encode(Fixture.Defs(), s)).State;
            Assert.AreEqual(s.SeasonGreetedDay, back.SeasonGreetedDay, "kept in the save");
        }

        [Test]
        public void Season_Quests_Show_Only_In_Season_And_Read_The_Season_Rabbit()
        {
            IReadOnlyList<StationDef> defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            var met = new QuestDef { Id = "mua-gap-tet", Metric = QuestMetric.SeasonKindMet, Target = 1, Season = Season.Tet, RewardGolden = 3 };
            var any = new QuestDef { Id = "x", Metric = QuestMetric.Taps, Target = 1 };
            Assert.IsTrue(Quests.IsAvailable(met, Day(2027, 2, 1)));
            Assert.IsFalse(Quests.IsAvailable(met, Day(2027, 6, 1)));
            Assert.IsTrue(Quests.IsAvailable(any, Day(2027, 6, 1)));
            Seasons.Clamp(s, Day(2027, 2, 1));
            Assert.AreEqual(0, Quests.Progress(met, defs, s));
            Rabbits.ArriveKind(s, "tho-ao-dai", "Mai");
            Assert.AreEqual(1, Quests.Progress(met, defs, s));
            Assert.IsTrue(Quests.CanClaim(met, defs, s));
            Seasons.Clamp(s, Day(2027, 6, 1));
            Assert.IsFalse(Quests.CanClaim(met, defs, s), "gone with the season, unclaimed");
            Seasons.Clamp(s, Day(2028, 2, 1));
            var trade = new QuestDef { Id = "mua-nghe-tet", Metric = QuestMetric.SeasonKindInTrade, Target = 1, Season = Season.Tet };
            Assert.AreEqual(0, Quests.Progress(trade, defs, s));
            Rabbit mai = Rabbits.Find(s, s.Rabbits[s.Rabbits.Count - 1].Id);
            int brew = -1; for (int i = 0; i < defs.Count; i++) if (defs[i].Trade == RabbitTrade.Brewer) { brew = i; break; }
            Assume.That(brew, Is.GreaterThanOrEqualTo(0));
            s.Levels[brew] = Math.Max(1, s.Levels[brew]);
            mai.Station = brew;
            Assert.AreEqual(1, Quests.Progress(trade, defs, s));
            var photo = new QuestDef { Id = "mua-anh-tet", Metric = QuestMetric.PhotosWithSeasonRabbit, Target = 1, Season = Season.Tet };
            Photos.Take(s, Day(2028, 2, 2), null);
            Assert.AreEqual(1, Quests.Progress(photo, defs, s));
        }
    }
}

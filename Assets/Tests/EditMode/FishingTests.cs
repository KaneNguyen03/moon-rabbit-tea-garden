using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The moon pond and the bond in points (#58): five baits back one every 24 minutes and none for a clock turned
    /// back, the half-second window of a cast, the six fish and their odds, feeding them to a rabbit, hearts from
    /// points, what hearts multiply a rabbit's worth by, an old save's hearts as points, and the ten-heart gift.
    /// </summary>
    public class FishingTests
    {
        const long T0 = 1_800_000_000L; // a unix second in 2027
        const long Bait = Fishing.BaitSeconds;
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.GardenXp = Garden.XpForLevel(Fishing.UnlockLevel);
        }

        static System.Func<double> Rolls(params double[] values)
        {
            int i = 0;
            return () => values[System.Math.Min(i++, values.Length - 1)];
        }

        Rabbit Add(int tier = 0, int station = -1)
        {
            var r = new Rabbit { Id = ++s.NextRabbitId, Tier = tier, Kind = RabbitKinds.DefaultFor(tier), Name = "R", Station = station };
            s.Rabbits.Add(r);
            return r;
        }

        // ----- baits -----

        [Test]
        public void A_New_Garden_Has_Five_Baits_And_Each_Cast_Takes_One()
        {
            Assert.AreEqual(Fishing.BaitCap, Fishing.Baits(s, T0));
            Assert.AreEqual(0, Fishing.NextBaitIn(s, T0), "nothing to wait for while all five are on hand");
            for (int n = Fishing.BaitCap - 1; n >= 0; n--)
            {
                Assert.IsNotNull(Fishing.Cast(s, T0, Rolls(0.5)));
                Assert.AreEqual(n, Fishing.Baits(s, T0));
            }
            Assert.IsNull(Fishing.Cast(s, T0, Rolls(0.5)), "no bait, no cast");
            Assert.AreEqual(0, Fishing.Baits(s, T0));
            Assert.AreEqual(Fishing.BaitCap, s.Casts);
        }

        [Test]
        public void Baits_Come_Back_One_Every_24_Minutes_Up_To_Five_In_Two_Hours()
        {
            Assert.AreEqual(24 * 60, Bait);
            for (int i = 0; i < Fishing.BaitCap; i++) Fishing.Cast(s, T0, Rolls(0.5));
            Assert.AreEqual(0, Fishing.Baits(s, T0 + Bait - 1));
            Assert.AreEqual(1, Fishing.NextBaitIn(s, T0 + Bait - 1));
            Assert.AreEqual(1, Fishing.Baits(s, T0 + Bait));
            Assert.AreEqual(Bait, Fishing.NextBaitIn(s, T0 + Bait));
            Assert.AreEqual(3, Fishing.Baits(s, T0 + 3 * Bait + 100));
            Assert.AreEqual(Bait - 100, Fishing.NextBaitIn(s, T0 + 3 * Bait + 100));
            Assert.AreEqual(5, Fishing.Baits(s, T0 + 2 * 3600), "all five back after two hours");
            Assert.AreEqual(5, Fishing.Baits(s, T0 + 30 * 86400L), "and never more than five");
            Assert.AreEqual(2 * 3600, Fishing.FullIn(s, T0));
        }

        [Test]
        public void A_Cast_Mid_Refill_Keeps_The_Minutes_Already_Waited()
        {
            for (int i = 0; i < Fishing.BaitCap; i++) Fishing.Cast(s, T0, Rolls(0.5));
            long now = T0 + 2 * Bait + 600; // two back, ten minutes towards the third
            Assert.AreEqual(2, Fishing.Baits(s, now));
            Fishing.Cast(s, now, Rolls(0.5));
            Assert.AreEqual(1, Fishing.Baits(s, now));
            Assert.AreEqual(Bait - 600, Fishing.NextBaitIn(s, now), "the third still comes fourteen minutes later");
            Assert.AreEqual(2, Fishing.Baits(s, T0 + 3 * Bait));
        }

        [Test]
        public void A_Cast_From_A_Full_Line_Starts_The_Wait_From_Then()
        {
            Fishing.Cast(s, T0, Rolls(0.5));
            Assert.AreEqual(4, Fishing.Baits(s, T0 + Bait - 1));
            Assert.AreEqual(5, Fishing.Baits(s, T0 + Bait));
            // Hours later, still five: the next cast starts its own wait, no time banked beyond the cap.
            long later = T0 + 10 * 3600;
            Fishing.Cast(s, later, Rolls(0.5));
            Assert.AreEqual(4, Fishing.Baits(s, later + Bait - 1));
            Assert.AreEqual(5, Fishing.Baits(s, later + Bait));
        }

        [Test]
        public void A_Clock_Turned_Back_Brings_No_Bait()
        {
            for (int i = 0; i < Fishing.BaitCap; i++) Fishing.Cast(s, T0, Rolls(0.5));
            Assert.AreEqual(0, Fishing.Baits(s, T0 - 5 * 3600), "back five hours: nothing");
            Assert.AreEqual(0, Fishing.Baits(s, T0 - 30 * 86400L), "back a month: nothing");
            Assert.IsNull(Fishing.Cast(s, T0 - 3600, Rolls(0.5)));
            Assert.AreEqual(Bait + 3600, Fishing.NextBaitIn(s, T0 - 3600), "the hour it took away is waited for too");
            Assert.AreEqual(1, Fishing.Baits(s, T0 + Bait), "and the real clock still counts");

            // Back again after a bait came: none is added, and casting never moves the clock backwards.
            Fishing.Cast(s, T0 + Bait, Rolls(0.5));
            Assert.AreEqual(0, Fishing.Baits(s, T0 - 7200));
            Assert.GreaterOrEqual(s.BaitAt, T0 + Bait);
        }

        [Test]
        public void The_Pond_Waits_For_Its_Garden_Level()
        {
            if (Fishing.UnlockLevel <= 1)
            {
                Assert.IsTrue(Fishing.Unlocked(GameState.NewGame(defs)), "open from the start");
                return;
            }
            s.GardenXp = Garden.XpForLevel(Fishing.UnlockLevel - 1);
            Assert.IsFalse(Fishing.Unlocked(s));
            Assert.IsNull(Fishing.Cast(s, T0, Rolls(0.5)));
            Assert.AreEqual(Fishing.BaitCap, Fishing.Baits(s, T0), "nothing spent");
            s.GardenXp = Garden.XpForLevel(Fishing.UnlockLevel);
            Assert.IsNotNull(Fishing.Cast(s, T0, Rolls(0.5)));
        }

        // ----- one cast -----

        [Test]
        public void The_Float_Bobs_Two_To_Six_Seconds_Then_Sinks_For_Half_A_Second()
        {
            Assert.AreEqual(2d, Fishing.BobFor(0d), 1e-9);
            Assert.AreEqual(6d, Fishing.BobFor(0.999999), 1e-5);
            Assert.AreEqual(4d, Fishing.BobFor(0.5), 1e-9);
            Assert.AreEqual(2d, Fishing.BobFor(double.NaN), 1e-9);
            Assert.AreEqual(6d, Fishing.BobFor(7d), 1e-9);

            FishingCast cast = Fishing.Cast(s, T0, Rolls(0.1, 0.5)); // the fish's roll, then a 4 s bob
            Assert.AreEqual(4d, cast.BobSeconds, 1e-9);
            Assert.AreEqual(CastPhase.Bobbing, cast.Advance(3.99));
            Assert.AreEqual(CastPhase.Sinking, cast.Advance(0.02));
            Assert.AreEqual(CastPhase.Sinking, cast.Advance(0.47));
            Assert.AreEqual(CastPhase.Lost, cast.Advance(0.03), "no tap in the half second: the bait is lost");
            Assert.AreEqual(CastTap.None, cast.Tap(), "a tap after the cast is over does nothing");
            Assert.IsFalse(Fishing.Land(s, cast));
            Assert.AreEqual(0, Fishing.InBasket(s));
        }

        [Test]
        public void A_Tap_While_It_Sinks_Lands_The_Fish_Once()
        {
            FishingCast cast = Fishing.Cast(s, T0, Rolls(0.99, 0d)); // the rarest fish, a 2 s bob
            Assert.AreEqual(5, cast.Fish);
            cast.Advance(2.25);
            Assert.AreEqual(CastPhase.Sinking, cast.Phase);
            Assert.AreEqual(CastTap.Caught, cast.Tap());
            Assert.AreEqual(CastPhase.Caught, cast.Phase);
            Assert.AreEqual(CastPhase.Caught, cast.Advance(10d), "a caught fish stays caught");
            Assert.IsTrue(Fishing.Land(s, cast));
            Assert.IsFalse(Fishing.Land(s, cast), "into the basket once");
            Assert.AreEqual(1, s.Fish[5]);
            Assert.AreEqual(1, s.FishCaught);
        }

        [Test]
        public void A_Tap_Too_Early_Brings_The_Float_Back_Up_With_The_Bait_Still_On()
        {
            FishingCast cast = Fishing.Cast(s, T0, Rolls(0d, 1d, 0d)); // a 6 s bob, then 2 s after the early tap
            Assert.AreEqual(4, Fishing.Baits(s, T0));
            cast.Advance(5d);
            Assert.AreEqual(CastTap.Early, cast.Tap());
            Assert.AreEqual(CastPhase.Bobbing, cast.Phase);
            Assert.AreEqual(0d, cast.Elapsed, 1e-9, "the float bobs again from the start");
            Assert.AreEqual(2d, cast.BobSeconds, 1e-9, "for a new time");
            Assert.AreEqual(1, cast.EarlyTaps);
            Assert.AreEqual(4, Fishing.Baits(s, T0), "no second bait taken");
            Assert.AreEqual(CastPhase.Bobbing, cast.Advance(1.9));
            Assert.AreEqual(CastPhase.Sinking, cast.Advance(0.2));
            Assert.AreEqual(CastTap.Caught, cast.Tap());
            Assert.AreEqual(CastPhase.Bobbing, Fishing.Cast(s, T0, Rolls(0d)).Advance(double.NaN), "no time, no change");
        }

        // ----- the six fish -----

        [Test]
        public void Six_Fish_Worth_1_3_5_8_12_30_Points_With_Odds_45_25_15_9_5_1()
        {
            CollectionAssert.AreEqual(new[] { 1, 3, 5, 8, 12, 30 }, Fishing.Points);
            double sum = 0d;
            foreach (double o in Fishing.Odds) sum += o;
            Assert.AreEqual(1d, sum, 1e-9);
            Assert.AreEqual(0, Fishing.RollFish(0d));
            Assert.AreEqual(0, Fishing.RollFish(0.4499));
            Assert.AreEqual(1, Fishing.RollFish(0.45));
            Assert.AreEqual(2, Fishing.RollFish(0.70));
            Assert.AreEqual(3, Fishing.RollFish(0.85));
            Assert.AreEqual(4, Fishing.RollFish(0.94));
            Assert.AreEqual(5, Fishing.RollFish(0.99));
            Assert.AreEqual(0, Fishing.RollFish(1d), "a roll out of range never hands out the rarest");
            Assert.AreEqual(0, Fishing.RollFish(double.NaN));
        }

        [Test]
        public void Two_Thousand_Casts_Catch_Each_Fish_About_As_Often_As_Its_Odds()
        {
            var rng = new System.Random(58);
            var counts = new int[Fishing.Kinds];
            for (int i = 0; i < 2000; i++)
            {
                s.BaitAt = -1; // a full line every time
                FishingCast cast = Fishing.Cast(s, T0, rng.NextDouble);
                cast.Advance(cast.BobSeconds + 0.1);
                Assert.AreEqual(CastTap.Caught, cast.Tap());
                Assert.IsTrue(Fishing.Land(s, cast));
                counts[cast.Fish]++;
            }
            for (int k = 0; k < Fishing.Kinds; k++)
            {
                double expected = 2000 * Fishing.Odds[k];
                double sigma = System.Math.Sqrt(2000 * Fishing.Odds[k] * (1 - Fishing.Odds[k]));
                Assert.AreEqual(expected, counts[k], 4 * sigma + 1, "fish " + k);
                Assert.AreEqual(counts[k], s.Fish[k]);
            }
            Assert.Greater(counts[5], 0, "the rarest does come");
            Assert.AreEqual(2000, s.FishCaught);
        }

        // ----- hearts from points -----

        [Test]
        public void Hearts_Follow_The_Points_With_Steps_At_10_30_80_180_380_880_1880_3880_8880_18880()
        {
            CollectionAssert.AreEqual(new[] { 0, 10, 30, 80, 180, 380, 880, 1880, 3880, 8880, 18880 }, Rabbits.HeartPoints);
            Assert.AreEqual(10, Rabbits.HeartMax);
            Assert.AreEqual(Rabbits.HeartPoints[Rabbits.HeartMax], Rabbits.AffectionMax);
            for (int h = 0; h <= Rabbits.HeartMax; h++)
            {
                Assert.AreEqual(h, Rabbits.HeartsOf(Rabbits.HeartPoints[h]), "exactly at the step for " + h);
                if (h > 0) Assert.AreEqual(h - 1, Rabbits.HeartsOf(Rabbits.HeartPoints[h] - 1), "one point short of " + h);
                Assert.AreEqual(Rabbits.HeartPoints[h], Rabbits.PointsFor(h));
            }
            Assert.AreEqual(0, Rabbits.HeartsOf(-5));
            Assert.AreEqual(10, Rabbits.HeartsOf(int.MaxValue));
            Assert.AreEqual("♥♥♥♡♡♡♡♡♡♡", Rabbits.Hearts(80));
            var r = new Rabbit { Affection = 100 };
            Assert.AreEqual(80, Rabbits.PointsToNext(r));
            r.Affection = Rabbits.AffectionMax;
            Assert.AreEqual(0, Rabbits.PointsToNext(r));
        }

        [Test]
        public void Each_Heart_Multiplies_The_Rabbits_Worth_By_Its_Own_Step()
        {
            double[] expected = { 1d, 1.05, 1.1, 1.2, 1.3, 1.45, 1.6, 1.8, 2d, 2.3, 2.6 };
            CollectionAssert.AreEqual(expected, Rabbits.HeartMultipliers);
            Rabbit r = Add(1, 0);
            double plain = Rabbits.Multiplier(s, r);
            Assert.AreEqual(Rabbits.Multipliers[1], plain, 1e-9);
            for (int h = 0; h <= Rabbits.HeartMax; h++)
            {
                r.Affection = Rabbits.PointsFor(h);
                Assert.AreEqual(plain * expected[h], Rabbits.Multiplier(s, r), 1e-9, h + " hearts");
                Assert.AreEqual(expected[h], Rabbits.HeartMultiplier(r), 1e-9);
            }
            // It reaches the station's income through the rabbit's multiplier.
            r.Affection = 0;
            BigNum before = Economy.IncomePerSecond(defs, s);
            r.Affection = Rabbits.PointsFor(10);
            Assert.AreEqual(2.6, (Economy.IncomePerSecond(defs, s) / before).ToDouble(), 1e-6);
        }

        [Test]
        public void A_Days_First_Stroke_Is_Ten_Points_And_The_Old_Line_And_Clover_Come_At_Three_And_Five_Hearts()
        {
            Rabbit r = Add();
            Assert.AreEqual(10, Rabbits.PetPoints);
            Rabbits.Pet(s, r, 1);
            Assert.AreEqual(10, r.Affection);
            Assert.AreEqual(1, Rabbits.HeartsOf(r), "the first stroke still brings the first heart");
            Rabbits.Pet(s, r, 1);
            Assert.AreEqual(10, r.Affection, "once a day");

            r.Affection = Rabbits.PointsFor(Rabbits.LineHearts) - 1;
            Assert.IsTrue(Rabbits.Pet(s, r, 2).LineUnlocked);
            s.Clovers = 0;
            r.Affection = Rabbits.PointsFor(Rabbits.GiftHearts) - 1;
            PetResult gift = Rabbits.Pet(s, r, 3);
            Assert.IsTrue(gift.GiftGiven);
            Assert.AreEqual(Rabbits.AffectionGiftClovers, s.Clovers);
            Assert.IsFalse(Rabbits.Pet(s, r, 4).GiftGiven, "once");
            Assert.AreEqual(Rabbits.AffectionGiftClovers, s.Clovers);
        }

        // ----- feeding -----

        [Test]
        public void Feeding_A_Fish_Adds_Its_Points_And_Takes_It_From_The_Basket()
        {
            Rabbit r = Add(0, 0);
            Assert.AreEqual(-1, Fishing.BestInBasket(s), "an empty basket has nothing to give");
            s.Fish[2] = 1;
            Assert.AreEqual(2, Fishing.BestInBasket(s));
            FeedResult fed = Fishing.Feed(s, r, 2);
            Assert.IsTrue(fed.Fed);
            Assert.AreEqual(5, fed.Points);
            Assert.AreEqual(5, r.Affection);
            Assert.AreEqual(0, s.Fish[2]);
            Assert.AreEqual(1, s.FishFed);
            Assert.IsFalse(Fishing.Feed(s, r, 2).Fed, "none of that kind left");
            Assert.IsFalse(Fishing.Feed(s, r, 9).Fed, "no such fish");
            Assert.IsFalse(Fishing.Feed(s, null, 0).Fed, "nobody to feed");
            Assert.AreEqual(5, r.Affection);

            s.Fish[5] = 1;
            r.Affection = 20;
            fed = Fishing.Feed(s, r, 5);
            Assert.AreEqual(1, fed.HeartsBefore);
            Assert.AreEqual(2, fed.HeartsAfter, "50 points: two hearts");
        }

        [Test]
        public void Ten_Hearts_Give_Three_Golden_Carrots_Once_And_The_Badge()
        {
            Rabbit r = Add(0, 0);
            s.GoldenCarrots = 0;
            r.Affection = Rabbits.AffectionMax - 20;
            Assert.IsFalse(Rabbits.FullHearts(r));
            s.Fish[5] = 3;
            FeedResult fed = Fishing.Feed(s, r, 5);
            Assert.IsTrue(fed.Bond.TenHearts);
            Assert.AreEqual(Rabbits.AffectionMax, r.Affection, "never past the cap");
            Assert.AreEqual(3, Rabbits.TenHeartGolden);
            Assert.AreEqual(Rabbits.TenHeartGolden, s.GoldenCarrots);
            Assert.IsTrue(Rabbits.FullHearts(r), "it wears the heart badge");

            Assert.IsFalse(Fishing.Feed(s, r, 5).Fed, "a full bond takes no more fish");
            Assert.AreEqual(2, s.Fish[5], "and the fish stays in the basket");
            Assert.IsFalse(Rabbits.Pet(s, r, 99).TenHearts, "nor does stroking pay the gift again");
            Assert.AreEqual(Rabbits.TenHeartGolden, s.GoldenCarrots, "paid exactly once");
        }

        [Test]
        public void Quests_Count_Fish_Caught_And_The_Most_Hearts_Any_Rabbit_Holds()
        {
            var catch50 = new QuestDef { Id = "ca-50", Metric = QuestMetric.FishCaught, Target = 50 };
            var tenHearts = new QuestDef { Id = "tim-10", Metric = QuestMetric.MostHearts, Target = 10 };
            s.FishCaught = 49;
            Assert.AreEqual(49d, Quests.Progress(catch50, defs, s), 1e-9);
            Assert.IsFalse(Quests.IsComplete(catch50, defs, s));
            s.FishCaught = 50;
            Assert.IsTrue(Quests.IsComplete(catch50, defs, s));

            Add().Affection = Rabbits.PointsFor(4);
            Rabbit best = Add();
            best.Affection = Rabbits.PointsFor(9);
            Assert.AreEqual(9d, Quests.Progress(tenHearts, defs, s), 1e-9);
            best.Affection = Rabbits.AffectionMax;
            Assert.IsTrue(Quests.IsComplete(tenHearts, defs, s));
        }

        // ----- the save -----

        [Test]
        public void The_Pond_And_The_Bond_Points_Round_Trip_In_The_Save()
        {
            for (int i = 0; i < 3; i++) Fishing.Cast(s, T0, Rolls(0.5));
            s.Fish[0] = 4;
            s.Fish[5] = 1;
            s.FishCaught = 12;
            s.FishFed = 7;
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A", Affection = 4321 });
            s.NextRabbitId = 1;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, r.Baits);
            Assert.AreEqual(T0, r.BaitAt);
            Assert.AreEqual(3, r.Casts);
            CollectionAssert.AreEqual(new[] { 4, 0, 0, 0, 0, 1 }, r.Fish);
            Assert.AreEqual(12, r.FishCaught);
            Assert.AreEqual(7, r.FishFed);
            Assert.AreEqual(4321, r.Rabbits[0].Affection);
            Assert.AreEqual(Fishing.Baits(s, T0 + Bait), Fishing.Baits(r, T0 + Bait));
        }

        [Test]
        public void An_Old_Saves_Hearts_Become_The_Points_Those_Hearts_Start_At_And_It_Has_All_Five_Baits()
        {
            var json = new System.Text.StringBuilder("{\"version\":14,\"stations\":[],\"rabbits\":[");
            for (int h = 0; h <= 5; h++)
                json.Append(h > 0 ? "," : "").Append("{\"id\":").Append(h + 1).Append(",\"tier\":0,\"name\":\"R\",\"affection\":").Append(h).Append('}');
            json.Append(",{\"id\":7,\"tier\":0,\"name\":\"X\",\"affection\":99}]}");
            GameState old = SaveCodec.Decode(defs, json.ToString()).State;
            int[] expected = { 0, 10, 30, 80, 180, 380 };
            for (int h = 0; h <= 5; h++)
            {
                Assert.AreEqual(expected[h], old.Rabbits[h].Affection, h + " old hearts: the points those hearts start at");
                Assert.AreEqual(h, Rabbits.HeartsOf(old.Rabbits[h]), "the same hearts as before");
            }
            Assert.AreEqual(380, old.Rabbits[6].Affection, "an odd old value is still at most five hearts");
            Assert.AreEqual(Fishing.BaitCap, Fishing.Baits(old, T0), "never fished: all five on hand");
            Assert.AreEqual(0, Fishing.InBasket(old));
            Assert.AreEqual(0, old.FishCaught);

            GameState bad = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"fishing\":{\"baits\":99,\"baitAt\":-50,\"basket\":[-3,\"x\",2],\"caught\":-1},"
                + "\"rabbits\":[{\"id\":1,\"tier\":0,\"name\":\"R\",\"bond\":999999}]}").State;
            Assert.AreEqual(Fishing.BaitCap, bad.Baits);
            Assert.AreEqual(-1, bad.BaitAt);
            CollectionAssert.AreEqual(new[] { 0, 0, 2, 0, 0, 0 }, bad.Fish);
            Assert.AreEqual(0, bad.FishCaught);
            Assert.AreEqual(Rabbits.AffectionMax, bad.Rabbits[0].Affection, "never past ten hearts");
        }
    }
}

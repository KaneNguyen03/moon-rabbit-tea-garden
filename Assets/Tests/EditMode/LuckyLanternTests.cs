using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class LuckyLanternTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Clovers = int.MaxValue / 2;
            s.GardenXp = Garden.XpForLevel(Rabbits.FavourFromLevel);
        }

        static double RollFor(int tier)
        {
            double edge = 0d;
            for (int t = 0; t < tier; t++) edge += Rabbits.Odds[t];
            return edge + Rabbits.Odds[tier] / 2d;
        }

        void SeeTier(int tier)
        {
            foreach (RabbitKind kind in RabbitKinds.OfTier(tier)) s.SeenKinds.Add(kind.Id);
        }

        [Test]
        public void Shop_And_Summon_Share_One_Unseen_Weight_Of_Three()
        {
            Assert.AreEqual(3, RabbitKinds.UnseenWeight);
        }

        [Test]
        public void Below_Garden_Level_Ten_Calls_Pick_Evenly_And_The_Lantern_Stays_Hidden()
        {
            s.GardenXp = Garden.XpForLevel(Rabbits.FavourFromLevel - 1);
            Assert.IsFalse(Rabbits.Favours(s));
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(0);
            for (int i = 1; i < kinds.Count; i++) s.SeenKinds.Add(kinds[i].Id);
            s.DupStreak = Rabbits.LuckyAfter;
            Assert.IsFalse(Rabbits.LuckyReady(s));
            Assert.AreEqual(0, Rabbits.LuckyLamps(s));
            for (int k = 1; k < kinds.Count; k++)
            {
                Rabbit called = Rabbits.Call(s, 0, "A", (k + 0.5) / kinds.Count);
                Assert.AreEqual(kinds[k].Id, called.Kind, "an even pick, as before #173");
                s.GardenXp = Garden.XpForLevel(Rabbits.FavourFromLevel - 1);
            }
            Assert.AreEqual(Rabbits.LuckyAfter, s.DupStreak, "calls below level ten leave the count alone");
        }

        [Test]
        public void Over_Ten_Thousand_Picks_An_Unseen_Kind_Comes_About_Three_Times_As_Often_As_A_Seen_One_Of_The_Same_Tier()
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(0);
            var seen = new HashSet<string>();
            for (int i = 0; i < kinds.Count; i += 2) seen.Add(kinds[i].Id);
            int seenKinds = seen.Count, unseenKinds = kinds.Count - seenKinds;
            var rng = new Random(173);
            int seenHits = 0, unseenHits = 0;
            for (int i = 0; i < 10000; i++)
            {
                if (seen.Contains(RabbitKinds.Pick(0, rng.NextDouble(), seen))) seenHits++;
                else unseenHits++;
            }
            double ratio = (unseenHits / (double)unseenKinds) / (seenHits / (double)seenKinds);
            TestContext.WriteLine("tier 0: " + unseenKinds + " unseen kinds got " + unseenHits + ", " + seenKinds + " seen kinds got " + seenHits + "; per kind unseen/seen = " + ratio.ToString("0.000"));
            Assert.That(ratio, Is.InRange(3d * 0.95, 3d * 1.05));
        }

        [Test]
        public void Summon_Weighs_Unseen_Kinds_By_Three_Within_The_Rolled_Tier()
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(1);
            for (int i = 1; i < kinds.Count; i++) s.SeenKinds.Add(kinds[i].Id);
            var rng = new Random(7);
            int first = 0, n = 10000;
            for (int i = 0; i < n; i++)
            {
                s.DupStreak = 0;
                Rabbit r = Rabbits.Summon(s, RollFor(1), "A", rng.NextDouble());
                Assert.AreEqual(1, r.Tier);
                if (r.Kind == kinds[0].Id) first++;
                s.SeenKinds.Remove(kinds[0].Id);
            }
            double expected = 3d / (3d + kinds.Count - 1);
            TestContext.WriteLine("one unseen tier-1 kind came " + first + " of " + n + ", expected share " + expected.ToString("0.000"));
            Assert.That(first / (double)n, Is.InRange(expected * 0.95, expected * 1.05));
        }

        [Test]
        public void After_Eight_Repeats_In_A_Row_The_Ninth_Call_Always_Brings_An_Unseen_Kind_At_Tier_Three_Or_Below()
        {
            for (int tier = 0; tier <= Rabbits.LuckyTopTier; tier++) SeeTier(tier);
            IReadOnlyList<RabbitKind> purple = RabbitKinds.OfTier(Rabbits.LuckyTopTier);
            s.SeenKinds.Remove(purple[purple.Count - 1].Id);
            var rng = new Random(9);
            for (int trial = 0; trial < 500; trial++)
            {
                s.DupStreak = 0;
                for (int call = 1; call <= Rabbits.LuckyAfter; call++)
                {
                    Rabbit dup = Rabbits.Summon(s, RollFor(rng.Next(0, Rabbits.LuckyTopTier)), "D", rng.NextDouble());
                    Assert.IsFalse(dup.FirstOfKind);
                    Assert.AreEqual(call, s.DupStreak);
                }
                Assert.IsTrue(Rabbits.LuckyReady(s));
                Assert.AreEqual(Rabbits.LuckyAfter, Rabbits.LuckyLamps(s));
                int rolled = rng.Next(0, Rabbits.TierCount);
                Rabbit ninth = Rabbits.Summon(s, RollFor(rolled), "N", rng.NextDouble());
                Assert.IsTrue(ninth.FirstOfKind, "trial " + trial + ": the ninth call is new");
                if (rolled <= Rabbits.LuckyTopTier) Assert.AreEqual(Rabbits.LuckyTopTier, ninth.Tier, "trial " + trial + ": only Purple has an unseen kind left");
                else Assert.AreEqual(rolled, ninth.Tier, "trial " + trial + ": a rolled tier with unseen kinds keeps its own new kind");
                Assert.AreEqual(0, s.DupStreak, "a new kind puts the lantern back to zero");
                s.SeenKinds.Remove(ninth.Kind);
            }
        }

        [Test]
        public void The_Ninth_Call_Rolled_Into_A_Full_Tier_Goes_To_The_Lowest_Tier_With_An_Unseen_Kind()
        {
            for (int tier = 0; tier < Rabbits.TierCount; tier++) SeeTier(tier);
            IReadOnlyList<RabbitKind> blue = RabbitKinds.OfTier(2);
            IReadOnlyList<RabbitKind> purple = RabbitKinds.OfTier(3);
            s.SeenKinds.Remove(blue[3].Id);
            s.SeenKinds.Remove(purple[1].Id);
            s.DupStreak = Rabbits.LuckyAfter;
            Rabbit r = Rabbits.Summon(s, RollFor(0), "A", 0.5);
            Assert.AreEqual(blue[3].Id, r.Kind);
            Assert.IsTrue(r.FirstOfKind);
            Assert.AreEqual(0, s.DupStreak);
        }

        [Test]
        public void A_Lit_Lantern_With_The_Top_Tier_Full_Does_Not_Stick()
        {
            SeeTier(5);
            s.DupStreak = Rabbits.LuckyAfter;
            Rabbit r = Rabbits.Summon(s, RollFor(5), "A", 0.3);
            Assert.IsTrue(r.FirstOfKind, "a full gold tier sends the promise to a lower tier");
            Assert.AreEqual(0, r.Tier, "the lowest tier still has unseen kinds");
            Assert.AreEqual(0, s.DupStreak);
            Assert.AreEqual(0, Rabbits.LuckyLamps(s));
        }

        [Test]
        public void With_Every_Kind_Up_To_Purple_Met_The_Lantern_Goes_Dark_Instead_Of_Promising()
        {
            for (int tier = 0; tier <= Rabbits.LuckyTopTier; tier++) SeeTier(tier);
            SeeTier(5);
            s.DupStreak = Rabbits.LuckyAfter;
            Assert.IsFalse(Rabbits.LuckyCanPromise(s));
            Assert.IsFalse(Rabbits.LuckyReady(s));
            Assert.AreEqual(0, Rabbits.LuckyLamps(s));
            Rabbit r = Rabbits.Summon(s, RollFor(5), "A", 0.3);
            Assert.IsFalse(r.FirstOfKind);
            Assert.AreEqual(0, s.DupStreak, "nothing left to promise: the count stays at zero");
        }

        [Test]
        public void A_Repeat_Lights_One_More_Lamp_And_A_New_Kind_Puts_Them_Out()
        {
            SeeTier(0);
            Rabbits.Summon(s, RollFor(0), "A", 0.4);
            Assert.AreEqual(1, s.DupStreak);
            Assert.AreEqual(1, Rabbits.LuckyLamps(s));
            Rabbit fresh = Rabbits.Summon(s, RollFor(1), "B", 0.4);
            Assert.IsTrue(fresh.FirstOfKind);
            Assert.AreEqual(0, s.DupStreak);
        }

        [Test]
        public void Golden_Calls_Light_And_Use_The_Same_Lantern()
        {
            SeeTier(0);
            SeeTier(1);
            s.GoldenCarrots = 1000;
            Rabbit dup = Shop.SummonGolden(s, 0.1, "G", 0.5);
            Assert.AreEqual(0, dup.Tier);
            Assert.AreEqual(1, s.DupStreak);
            s.DupStreak = Rabbits.LuckyAfter;
            Rabbit lucky = Shop.SummonGolden(s, 0.1, "G", 0.5);
            Assert.IsTrue(lucky.FirstOfKind);
            Assert.AreEqual(2, lucky.Tier);
            Assert.AreEqual(0, s.DupStreak);
        }

        [Test]
        public void The_Repeat_Count_Survives_Save_And_Load()
        {
            s.DupStreak = 5;
            Assert.AreEqual(5, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.DupStreak);
            s.DupStreak = Rabbits.LuckyAfter;
            Assert.AreEqual(Rabbits.LuckyAfter, SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State.DupStreak);
        }

        [Test]
        public void An_Old_Save_Without_The_Count_Reads_Zero()
        {
            s.DupStreak = 6;
            string json = SaveCodec.Encode(defs, s);
            StringAssert.Contains("\"dupStreak\":6", json);
            string old = Regex.Replace(json, ",\"dupStreak\":6", "");
            StringAssert.DoesNotContain("dupStreak", old);
            LoadResult r = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            Assert.AreEqual(0, r.State.DupStreak);
        }

        [Test]
        public void Over_A_Hundred_Thousand_Calls_The_Tier_Odds_Stay_As_Published()
        {
            var rng = new Random(2611);
            int n = 100000;
            var counts = new int[Rabbits.TierCount];
            for (int i = 0; i < n; i++) counts[Rabbits.Summon(s, rng.NextDouble(), "R", rng.NextDouble()).Tier]++;
            for (int tier = 0; tier < Rabbits.TierCount; tier++)
            {
                double share = counts[tier] * 100d / n, odds = Rabbits.Odds[tier] * 100d;
                TestContext.WriteLine("tier " + tier + ": " + share.ToString("0.000") + " % (odds " + odds.ToString("0.0##") + " %)");
                Assert.That(share, Is.InRange(odds - 0.5d, odds + 0.5d), "tier " + tier);
            }
        }
    }
}

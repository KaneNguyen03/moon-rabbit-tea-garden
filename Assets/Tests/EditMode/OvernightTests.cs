using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class OvernightTests
    {
        const long T = 1_780_012_800L;
        const long Minute = 60, Hour = 3600, Day = 86400;

        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = Garden(Fixture.CodeFor(7));
        }

        GameState Garden(string code)
        {
            GameState g = GameState.NewGame(defs);
            g.Profile.Code = code;
            Rabbits.GiftFirst(g, "Bông");
            Rabbits.ArriveKind(g, "tho-me", "Mè");
            Rabbits.ArriveKind(g, "tho-cup", "Cụp");
            g.Season = 1;
            g.LastSeenUnixSeconds = T;
            return g;
        }

        AwayReport Back(GameState g, long now) => Away.Claim(defs, g, now);

        static int Count(AwayReport r) => r.Stories == null ? 0 : r.Stories.Count;

        [Test]
        public void The_Numbers_Are_The_Issues_Defaults()
        {
            Assert.AreEqual(30 * Minute, Overnight.MinSeconds);
            Assert.AreEqual(6 * Hour, Overnight.LongNightSeconds);
            Assert.AreEqual(2, Overnight.LeavesLeast);
            Assert.AreEqual(5, Overnight.LeavesMost);
            Assert.AreEqual(1, Overnight.CloverGift);
            Assert.AreEqual(7, Overnight.WeekCloverCap);
            Assert.AreEqual(0, Overnight.WeekGoldenCap);
            Assert.AreEqual(2, Overnight.StrangerTopTier);
            Assert.AreEqual(3, Overnight.StrangerEveryDays);
            Assert.AreEqual(10, Overnight.BondPoints);
        }

        [Test]
        public void Under_Half_An_Hour_Tells_Nothing_Up_To_Six_Hours_One_And_Six_Hours_Or_More_Two()
        {
            Assert.AreEqual(0, Count(Back(s, T + 29 * Minute + 59)), "a little under half an hour");
            Assert.AreEqual(0, s.Overnight.Waiting.Count);

            long[] one = { 30 * Minute, 2 * Hour, 6 * Hour - 1 };
            foreach (long gap in one)
            {
                SetUp();
                Assert.AreEqual(1, Count(Back(s, T + gap)), gap + " seconds away");
                Assert.AreEqual(1, s.Overnight.Waiting.Count);
            }
            long[] two = { 6 * Hour, 9 * Hour, 3 * Day };
            foreach (long gap in two)
            {
                SetUp();
                AwayReport r = Back(s, T + gap);
                Assert.AreEqual(2, Count(r), gap + " seconds away");
                Assert.AreNotEqual(r.Stories[0].Kind, r.Stories[1].Kind, "two different stories on a long night");
            }
        }

        [Test]
        public void The_Same_Night_And_The_Same_Garden_Always_Tell_The_Same_Stories()
        {
            var seen = new HashSet<string>();
            for (int night = 0; night < 40; night++)
            {
                GameState a = Garden(Fixture.CodeFor(7)), b = Garden(Fixture.CodeFor(7));
                long left = T + night * 7919;
                a.LastSeenUnixSeconds = b.LastSeenUnixSeconds = left;
                string told = Tell(Back(a, left + 7 * Hour));
                Assert.AreEqual(told, Tell(Back(b, left + 7 * Hour)), "night " + night);
                GameState c = Garden(Fixture.CodeFor(7));
                c.LastSeenUnixSeconds = left;
                Assert.AreEqual(told, Tell(Back(c, left + 9 * Hour)), "a longer night from the same hour tells the same");
                seen.Add(told);
            }
            Assert.Greater(seen.Count, 4, "different nights tell different stories");
            var codes = new HashSet<string>();
            for (int seed = 0; seed < 20; seed++) codes.Add(Tell(Back(Garden(Fixture.CodeFor(seed)), T + 7 * Hour)));
            Assert.Greater(codes.Count, 2, "different gardens tell different stories on the same night");
        }

        static string Tell(AwayReport r)
        {
            var parts = new List<string>();
            foreach (Story story in r.Stories) parts.Add(Overnight.IdOf(story.Kind) + ":" + story.Rabbit + ":" + story.Friend + ":" + story.Guest + ":" + story.Amount + ":" + story.Line);
            return string.Join("|", parts);
        }

        [Test]
        public void Moving_The_Clock_Forward_And_Back_Never_Tells_More()
        {
            Assert.AreEqual(2, Count(Back(s, T + 7 * Hour)), "forward seven hours");
            Assert.AreEqual(0, Count(Back(s, T + Hour)), "the clock set back");
            Assert.AreEqual(0, Count(Back(s, T + 7 * Hour)), "forward again to the same hour");
            Assert.AreEqual(0, Count(Back(s, T + 2 * Hour)), "back again");
            Assert.AreEqual(0, Count(Back(s, T + 7 * Hour + 29 * Minute)), "forward again, under half an hour past the furthest");
            Assert.AreEqual(2, s.Overnight.Waiting.Count, "only the first night's two");
            Assert.AreEqual(T + 7 * Hour + 29 * Minute, s.Overnight.Mark);

            Assert.AreEqual(1, Count(Back(s, T + 8 * Hour)), "real time past the furthest clock tells again");
        }

        [Test]
        public void A_Long_Session_Then_The_Clock_Set_Back_And_Forward_Again_Tells_Nothing()
        {
            Assert.AreEqual(0, Count(Back(s, T + 10)), "open the garden at T");
            s.LastSeenUnixSeconds = T + 7 * Hour;
            Assert.AreEqual(0, Count(Back(s, T + 5 * Minute)), "after seven hours of play, the clock set back");
            Assert.AreEqual(T + 7 * Hour, s.Overnight.Mark, "the furthest time the garden saw is kept");
            Assert.AreEqual(0, Count(Back(s, T + 7 * Hour + Minute)), "a minute after the session ended is no night");
            Assert.AreEqual(0, s.Overnight.Waiting.Count);
            Assert.AreEqual(1, Count(Back(s, T + 8 * Hour)), "a real hour after the session tells one");
        }

        [Test]
        public void A_Garden_Never_Left_Tells_Nothing()
        {
            s.LastSeenUnixSeconds = 0;
            Assert.AreEqual(0, Count(Back(s, T)));
            Assert.AreEqual(T, s.Overnight.Mark);
        }

        [Test]
        public void The_Stranger_At_The_Gate_Is_An_Unmet_Kind_Of_Tier_Two_Or_Less_At_Most_Once_In_Three_Days()
        {
            var strangerDays = new List<long>();
            long now = T;
            for (int visit = 0; visit < 3 * 60; visit++)
            {
                s.LastSeenUnixSeconds = now;
                now += 8 * Hour;
                var unmet = new HashSet<string>();
                foreach (RabbitKind kind in Overnight.UnmetGuests(s)) unmet.Add(kind.Id);
                AwayReport r = Back(s, now);
                foreach (Story story in r.Stories)
                {
                    if (story.Kind != StoryKind.Stranger) continue;
                    RabbitKind guest = RabbitKinds.Find(story.Guest);
                    Assert.IsNotNull(guest);
                    Assert.LessOrEqual(guest.Tier, 2);
                    Assert.IsTrue(unmet.Contains(guest.Id), guest.Id + " was not met yet");
                    strangerDays.Add(Overnight.DayOf(now));
                }
                foreach (Story story in new List<Story>(s.Overnight.Waiting))
                {
                    StoryClaim claim = Overnight.Claim(s, story, "Lạ");
                    if (story.Kind == StoryKind.Stranger) Assert.IsTrue(claim.Arrived.FirstOfKind);
                }
            }
            Assert.Greater(strangerDays.Count, 1, "the gate has visitors over sixty days");
            for (int i = 1; i < strangerDays.Count; i++)
                Assert.GreaterOrEqual(strangerDays[i] - strangerDays[i - 1], Overnight.StrangerEveryDays, "at most once in three days");
        }

        [Test]
        public void No_Stranger_Comes_When_Every_Kind_Up_To_Tier_Two_Is_Met()
        {
            foreach (RabbitKind kind in RabbitKinds.All) if (kind.Tier <= 2) Rabbits.See(s, kind.Id);
            long now = T;
            for (int visit = 0; visit < 60; visit++)
            {
                s.LastSeenUnixSeconds = now;
                now += 7 * Hour;
                foreach (Story story in Back(s, now).Stories) Assert.AreNotEqual(StoryKind.Stranger, story.Kind);
            }
        }

        [Test]
        public void Stories_About_Rabbits_Only_Name_Rabbits_In_The_Garden()
        {
            var ids = new HashSet<long>();
            foreach (Rabbit r in s.Rabbits) ids.Add(r.Id);
            long now = T;
            for (int visit = 0; visit < 200; visit++)
            {
                s.LastSeenUnixSeconds = now;
                now += 7 * Hour;
                foreach (Story story in Back(s, now).Stories)
                {
                    if (story.Kind == StoryKind.Leaves || story.Kind == StoryKind.Doze || story.Kind == StoryKind.Bond) Assert.IsTrue(ids.Contains(story.Rabbit), story.Kind + " names " + story.Rabbit);
                    if (story.Kind == StoryKind.Bond) Assert.IsTrue(ids.Contains(story.Friend) && story.Friend != story.Rabbit);
                }
                s.Overnight.Waiting.Clear();
            }

            GameState empty = GameState.NewGame(defs);
            empty.Profile.Code = Fixture.CodeFor(3);
            empty.Season = 1;
            now = T;
            for (int visit = 0; visit < 60; visit++)
            {
                empty.LastSeenUnixSeconds = now;
                now += 7 * Hour;
                foreach (Story story in Back(empty, now).Stories)
                    Assert.IsTrue(story.Kind == StoryKind.Clover || story.Kind == StoryKind.Stranger, "no rabbits, so " + story.Kind + " cannot be told");
                empty.Overnight.Waiting.Clear();
            }
        }

        [Test]
        public void Two_Rabbits_Grow_Closer_Only_When_Both_Work_Or_Both_Rest()
        {
            s.Rabbits[0].Station = 0;
            s.Rabbits[1].Station = -1;
            s.Rabbits[2].Station = -1;
            List<long[]> pairs = Overnight.Pairs(s);
            Assert.AreEqual(1, pairs.Count);
            CollectionAssert.AreEquivalent(new[] { s.Rabbits[1].Id, s.Rabbits[2].Id }, pairs[0]);

            s.Rabbits[1].Affection = Rabbits.PointsFor(Rabbits.GiftHearts) - 5;
            Assert.AreEqual(0, Overnight.Pairs(s).Count, "a bond that would cross the clover heart is left for petting");
            s.Rabbits[1].Affection = Rabbits.PointsFor(Rabbits.HeartMax) - 5;
            Assert.AreEqual(0, Overnight.Pairs(s).Count, "and the golden ten-heart gift too");
        }

        [Test]
        public void Clovers_Stop_At_Seven_A_Week_And_No_Golden_Carrots_Come()
        {
            var perWeek = new Dictionary<long, int>();
            long now = T;
            int golden = s.GoldenCarrots;
            for (int visit = 0; visit < 4 * 7 * 6; visit++)
            {
                s.LastSeenUnixSeconds = now;
                now += 6 * Hour;
                Back(s, now);
                foreach (Story story in new List<Story>(s.Overnight.Waiting))
                {
                    StoryClaim claim = Overnight.Claim(s, story, "N");
                    long week = Overnight.WeekOf(now);
                    perWeek.TryGetValue(week, out int n);
                    perWeek[week] = n + claim.Clovers;
                }
            }
            Assert.GreaterOrEqual(perWeek.Count, 4);
            foreach (KeyValuePair<long, int> week in perWeek) Assert.LessOrEqual(week.Value, Overnight.WeekCloverCap, "week " + week.Key);
            Assert.AreEqual(golden, s.GoldenCarrots, "no golden carrots");
        }

        [Test]
        public void Each_Story_Is_Claimed_Once_Into_The_Right_Purse()
        {
            int clovers = s.Clovers, leaves = s.RecipeLeaves, rabbits = s.Rabbits.Count;
            var clover = new Story { Kind = StoryKind.Clover, Amount = 1 };
            var leaf = new Story { Kind = StoryKind.Leaves, Rabbit = s.Rabbits[0].Id, Amount = 4 };
            var guest = new Story { Kind = StoryKind.Stranger, Guest = "tho-suong" };
            var bond = new Story { Kind = StoryKind.Bond, Rabbit = s.Rabbits[1].Id, Friend = s.Rabbits[2].Id, Amount = 10 };
            var doze = new Story { Kind = StoryKind.Doze, Rabbit = s.Rabbits[0].Id };
            s.Overnight.Waiting.AddRange(new[] { clover, leaf, guest, bond, doze });

            Assert.AreEqual(1, Overnight.Claim(s, clover, "x").Clovers);
            Assert.AreEqual(clovers + 1, s.Clovers);
            Assert.IsFalse(Overnight.Claim(s, clover, "x").Ok, "once");
            Assert.AreEqual(clovers + 1, s.Clovers);

            Assert.AreEqual(4, Overnight.Claim(s, leaf, "x").Leaves);
            Assert.AreEqual(leaves + 4, s.RecipeLeaves);

            StoryClaim met = Overnight.Claim(s, guest, "Sương");
            Assert.AreEqual("tho-suong", met.Arrived.Kind);
            Assert.AreEqual("Sương", met.Arrived.Name);
            Assert.IsTrue(met.Arrived.FirstOfKind);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);

            int a = s.Rabbits[1].Affection, b = s.Rabbits[2].Affection;
            Assert.AreEqual(10, Overnight.Claim(s, bond, "x").Bond);
            Assert.AreEqual(a + 10, s.Rabbits[1].Affection);
            Assert.AreEqual(b + 10, s.Rabbits[2].Affection);

            StoryClaim told = Overnight.Claim(s, doze, "x");
            Assert.IsTrue(told.Ok);
            Assert.AreEqual(0, told.Clovers + told.Leaves + told.Bond);
            Assert.AreEqual(0, s.Overnight.Waiting.Count);
        }

        [Test]
        public void Unclaimed_Stories_Wait_And_The_Oldest_Make_Room()
        {
            Back(s, T + 7 * Hour);
            List<Story> first = new List<Story>(s.Overnight.Waiting);
            s.LastSeenUnixSeconds = T + 8 * Hour;
            AwayReport r = Back(s, T + 15 * Hour);
            Assert.AreEqual(Overnight.MaxWaiting, s.Overnight.Waiting.Count);
            CollectionAssert.AreEqual(first, r.Dropped, "the two older ones make room");
            CollectionAssert.AreEqual(r.Stories, s.Overnight.Waiting);
        }

        [Test]
        public void The_Record_And_Waiting_Stories_Survive_A_Save_And_An_Old_Save_Has_None()
        {
            Back(s, T + 7 * Hour);
            s.Overnight.Waiting.Add(new Story { Kind = StoryKind.Stranger, Guest = "tho-suong", Night = T });
            s.Overnight.Waiting.RemoveAt(0);
            string json = SaveCodec.Encode(defs, s);
            GameState back = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(s.Overnight.Mark, back.Overnight.Mark);
            Assert.AreEqual(s.Overnight.StrangerDay, back.Overnight.StrangerDay);
            Assert.AreEqual(s.Overnight.Week, back.Overnight.Week);
            Assert.AreEqual(s.Overnight.WeekClovers, back.Overnight.WeekClovers);
            Assert.AreEqual(s.Overnight.Waiting.Count, back.Overnight.Waiting.Count);
            for (int i = 0; i < s.Overnight.Waiting.Count; i++)
            {
                Story a = s.Overnight.Waiting[i], b = back.Overnight.Waiting[i];
                Assert.AreEqual(a.Kind, b.Kind);
                Assert.AreEqual(a.Rabbit, b.Rabbit);
                Assert.AreEqual(a.Friend, b.Friend);
                Assert.AreEqual(a.Guest, b.Guest);
                Assert.AreEqual(a.Amount, b.Amount);
                Assert.AreEqual(a.Line, b.Line);
                Assert.AreEqual(a.Night, b.Night);
            }
            Assert.AreEqual(0, Count(Back(back, T + 7 * Hour)), "the loaded garden remembers the furthest clock");

            int at = json.IndexOf(",\"overnight\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0);
            int end = json.IndexOf("]}", at, System.StringComparison.Ordinal) + 2;
            LoadResult old = SaveCodec.Decode(defs, json.Substring(0, at) + json.Substring(end));
            Assert.AreEqual(LoadStatus.Ok, old.Status);
            Assert.AreEqual(-1L, old.State.Overnight.Mark);
            Assert.AreEqual(0, old.State.Overnight.Waiting.Count);

            string night = json.Substring(at, end - at);
            string bad = json.Substring(0, at) + night.Replace("\"kind\":\"stranger\"", "\"kind\":\"dragon\"").Replace("\"amount\":", "\"amount\":9") + json.Substring(end);
            GameState cleaned = SaveCodec.Decode(defs, bad).State;
            foreach (Story story in cleaned.Overnight.Waiting) Assert.LessOrEqual(story.Amount, Overnight.Most(story.Kind));
            Assert.AreEqual(s.Overnight.Waiting.Count - 1, cleaned.Overnight.Waiting.Count, "an unknown kind is dropped");
        }

        [Test]
        public void Each_Story_Is_Logged_With_Its_Kind_And_Whether_It_Was_Claimed()
        {
            var log = new MemoryAnalytics();
            log.LogOvernightStory(null, true);
            Assert.AreEqual(0, log.Count(AnalyticsEvents.OvernightStory));
            log.LogOvernightStory(new Story { Kind = StoryKind.Stranger }, true);
            log.LogOvernightStory(new Story { Kind = StoryKind.Clover }, false);
            Assert.AreEqual(2, log.Count(AnalyticsEvents.OvernightStory));
            MemoryAnalytics.Event last = log.Last(AnalyticsEvents.OvernightStory);
            Assert.AreEqual("clover", last[AnalyticsEvents.Kind].Text);
            Assert.AreEqual(0, last[AnalyticsEvents.Claimed].Number);
            CollectionAssert.Contains(AnalyticsEvents.All, AnalyticsEvents.OvernightStory);
        }
    }
}

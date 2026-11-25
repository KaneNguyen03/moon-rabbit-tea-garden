using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class StrollsTests
    {
        static Vec2 At(float x, float y) => new Vec2(x, y);

        readonly List<Vec2> spots = new List<Vec2> { At(100, 0), At(300, 0), At(0, 400), At(900, 900), At(-250, 0) };

        [Test]
        public void A_Spot_Is_Picked_Within_The_Radius_And_Never_Beside_A_Blocked_Point()
        {
            var blocked = new List<Vec2> { At(300, 120) };
            for (int k = 0; k < 20; k++)
            {
                int pick = Strolls.PickSpot(spots, blocked, At(0, 0), 450f, k / 20.0);
                Assert.That(pick, Is.EqualTo(0).Or.EqualTo(2).Or.EqualTo(4), "roll " + k + " picked " + pick);
            }
        }

        [Test]
        public void The_Roll_Decides_And_The_Same_Roll_Picks_The_Same_Spot()
        {
            var none = new List<Vec2>();
            Assert.AreEqual(Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.37), Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.37));
            Assert.AreEqual(0, Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.0));
            Assert.AreEqual(2, Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.5));
            Assert.AreEqual(4, Strolls.PickSpot(spots, none, At(0, 0), 450f, 0.999));
            Assert.AreEqual(4, Strolls.PickSpot(spots, none, At(0, 0), 450f, 1.0), "a roll of one still picks the last");
        }

        [Test]
        public void No_Spot_In_Reach_Means_No_Pick()
        {
            Assert.AreEqual(-1, Strolls.PickSpot(spots, new List<Vec2>(), At(0, 0), 50f, 0.5), "all too far");
            var onEach = new List<Vec2> { At(150, 0), At(0, 400), At(-250, 0) };
            Assert.AreEqual(-1, Strolls.PickSpot(spots, onEach, At(0, 0), 450f, 0.5), "every near spot stands within the clearance of a blocked point");
            Assert.AreEqual(-1, Strolls.PickSpot(new List<Vec2>(), new List<Vec2>(), At(0, 0), 450f, 0.5), "no spots at all");
        }

        [Test]
        public void The_Path_To_A_Spot_Keeps_Clear_Of_Blocked_Points()
        {
            var blocked = new List<Vec2> { At(40, 200) };
            for (int k = 0; k < 20; k++)
                Assert.AreNotEqual(2, Strolls.PickSpot(spots, blocked, At(0, 0), 450f, k / 20.0), "the way is blocked");
            Assert.AreEqual(0, Strolls.PickSpot(new List<Vec2> { At(0, 400) }, new List<Vec2> { At(0, 0) }, At(0, 0), 450f, 0.5),
                "a blocked point at the start itself does not count against the way out");
        }

        [Test]
        public void At_Most_One_Rabbit_Out_For_Every_Three_Empty_Spots()
        {
            Assert.AreEqual(0, Strolls.MaxAtOnce(0));
            Assert.AreEqual(0, Strolls.MaxAtOnce(2));
            Assert.AreEqual(1, Strolls.MaxAtOnce(3));
            Assert.AreEqual(1, Strolls.MaxAtOnce(5));
            Assert.AreEqual(3, Strolls.MaxAtOnce(11));
        }

        [Test]
        public void Fireflies_Are_Chased_Only_At_Night_And_Every_Activity_Comes_Up()
        {
            var day = new HashSet<StrollActivity>();
            var night = new HashSet<StrollActivity>();
            for (int k = 0; k < 30; k++)
            {
                day.Add(Strolls.ActivityFor(k / 30.0, false));
                night.Add(Strolls.ActivityFor(k / 30.0, true));
            }
            Assert.IsFalse(day.Contains(StrollActivity.Firefly), "no fireflies by day");
            Assert.IsTrue(day.Contains(StrollActivity.Tea) && day.Contains(StrollActivity.Doze));
            Assert.IsTrue(night.Contains(StrollActivity.Firefly) && night.Contains(StrollActivity.Tea) && night.Contains(StrollActivity.Doze));
            Assert.AreEqual(Strolls.ActivityFor(0.2, true), Strolls.ActivityFor(0.2, true), "the same roll, the same thing");
        }

        [Test]
        public void A_Roll_Is_The_Same_For_The_Same_Rabbit_And_Stroll_And_Differs_Between_Them()
        {
            double a = Strolls.Roll(0, 3, 0);
            Assert.AreEqual(a, Strolls.Roll(0, 3, 0));
            Assert.GreaterOrEqual(a, 0d); Assert.Less(a, 1d);
            Assert.AreNotEqual(a, Strolls.Roll(0, 4, 0), "the next stroll rolls again");
            Assert.AreNotEqual(a, Strolls.Roll(1, 3, 0), "and so does the next rabbit");
            Assert.AreNotEqual(a, Strolls.Roll(0, 3, 1), "and the next question");
        }

        [Test]
        public void The_Rest_At_The_Spot_Lasts_Four_To_Six_Seconds()
        {
            Assert.AreEqual(4f, Strolls.LingerFor(0.0), 1e-5);
            Assert.AreEqual(6f, Strolls.LingerFor(1.0), 1e-5);
            Assert.AreEqual(5f, Strolls.LingerFor(0.5), 1e-5);
        }

        static List<BreakPost> Garden(int stations, out List<Vec2> blocked, out List<Vec2> spots)
        {
            var posts = new List<BreakPost>();
            blocked = new List<Vec2>();
            spots = new List<Vec2>();
            for (int i = 0; i < stations; i++)
            {
                Vec2 at = At(i % 4 * 500f, i / 4 * 500f);
                blocked.Add(at);
                posts.Add(new BreakPost(i, 100 + i, At(at.X, at.Y - 80f)));
                spots.Add(At(at.X + 250f, at.Y));
                spots.Add(At(at.X, at.Y + 250f));
            }
            return posts;
        }

        static string Schedule(float step, int stations, float minutes)
        {
            List<BreakPost> posts = Garden(stations, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            var seen = new SortedSet<string>();
            double clock = 0d;
            while (clock < minutes * 60f)
            {
                double before = clock;
                clock += step;
                plan.Plan(clock, posts, spots, blocked, false, false);
                for (double t = before; t <= clock; t += 0.05)
                    foreach (BreakPost p in posts)
                    {
                        Break b = plan.Of(p.Station, p.Rabbit, t);
                        if (b != null) seen.Add(b.Station + "/" + b.Number + "/" + b.Start.ToString("R") + "/" + b.Spot + "/" + b.Linger.ToString("R") + "/" + b.Activity);
                    }
            }
            return string.Join("\n", seen);
        }

        [Test]
        public void The_Break_Schedule_Is_The_Same_Every_Run_And_At_Any_Frame_Rate()
        {
            string fine = Schedule(0.25f, 16, 30f);
            Assert.IsNotEmpty(fine, "rabbits do take breaks");
            Assert.AreEqual(fine, Schedule(0.25f, 16, 30f), "the same garden, the same breaks");
            Assert.AreEqual(fine, Schedule(1.7f, 16, 30f), "a slow frame rate changes nothing");
        }

        [Test]
        public void At_Most_Two_Rabbits_Are_On_A_Break_And_One_Working_Rabbit_Always_Stays_At_Its_Station()
        {
            foreach (int stations in new[] { 1, 2, 3, 16 })
            {
                List<BreakPost> posts = Garden(stations, out List<Vec2> blocked, out List<Vec2> spots);
                var plan = new Breaks();
                int most = 0;
                for (double clock = 0d; clock < 60d * 60d; clock += 0.5d)
                {
                    plan.Plan(clock, posts, spots, blocked, false, false);
                    int away = plan.OutAt(clock);
                    most = System.Math.Max(most, away);
                    Assert.LessOrEqual(away, Breaks.AtOnce, stations + " stations at " + clock);
                    Assert.GreaterOrEqual(posts.Count - away, 1, stations + " stations at " + clock + ": someone stays at work");
                    var taken = new HashSet<int>();
                    foreach (BreakPost p in posts)
                    {
                        Break b = plan.Of(p.Station, p.Rabbit, clock);
                        if (b != null) Assert.IsTrue(taken.Add(b.Spot), "two rabbits on spot " + b.Spot + " at " + clock);
                    }
                }
                Assert.AreEqual(System.Math.Min(stations - 1, Breaks.AtOnce), most, stations + " stations");
            }
        }

        [Test]
        public void A_Rabbit_Takes_About_One_Break_Every_Three_Minutes_And_Never_Two_In_One()
        {
            List<BreakPost> posts = Garden(16, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            var taken = new Dictionary<int, HashSet<int>>();
            const int periods = 20;
            for (double clock = 0d; clock < periods * Breaks.Every; clock += 0.5d)
            {
                plan.Plan(clock, posts, spots, blocked, false, false);
                foreach (BreakPost p in posts)
                {
                    Break b = plan.Of(p.Station, p.Rabbit, clock);
                    if (b == null) continue;
                    if (!taken.TryGetValue(p.Station, out HashSet<int> numbers)) taken[p.Station] = numbers = new HashSet<int>();
                    numbers.Add(b.Number);
                    Assert.AreEqual(b.Number, Breaks.NumberAt(b.Start), "a break starts in its own three minutes");
                    Assert.LessOrEqual(b.Back, (b.Number + 1) * (double)Breaks.Every, "and is back before the next");
                    Assert.That(b.Linger, Is.InRange(Strolls.LingerLeast, Strolls.LingerMost));
                }
            }
            int all = 0;
            foreach (BreakPost p in posts)
            {
                int n = taken.TryGetValue(p.Station, out HashSet<int> numbers) ? numbers.Count : 0;
                all += n;
                Assert.That(n, Is.InRange(periods / 2, periods), "station " + p.Station);
            }
            Assert.GreaterOrEqual(all, posts.Count * periods * 3 / 4, "most breaks are let out");
        }

        [Test]
        public void A_Break_Goes_To_A_Spot_At_Most_Three_Hundred_Away_Clear_Of_Every_Station()
        {
            List<BreakPost> posts = Garden(16, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            int checkedBreaks = 0;
            for (double clock = 0d; clock < 30d * 60d; clock += 0.5d)
            {
                plan.Plan(clock, posts, spots, blocked, false, false);
                foreach (BreakPost p in posts)
                {
                    Break b = plan.Of(p.Station, p.Rabbit, clock);
                    if (b == null) continue;
                    checkedBreaks++;
                    Vec2 spot = spots[b.Spot];
                    Assert.LessOrEqual(2f * Vec2.Distance(p.Home, spot), 600f, "the way there and back");
                    foreach (Vec2 station in blocked) Assert.GreaterOrEqual(Vec2.Distance(station, spot), Strolls.Clearance);
                    Assert.AreEqual(100 + p.Station, b.Rabbit);
                }
            }
            Assert.Greater(checkedBreaks, 0);
        }

        [Test]
        public void Under_Reduced_Motion_Nobody_Leaves_Work()
        {
            List<BreakPost> posts = Garden(16, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            for (double clock = 0d; clock < 20d * 60d; clock += 0.5d)
            {
                plan.Plan(clock, posts, spots, blocked, false, true);
                Assert.AreEqual(0, plan.OutAt(clock), "at " + clock);
            }
        }

        [Test]
        public void A_Break_Rolls_Apart_From_A_Stroll_Of_The_Same_Number()
        {
            var starts = new HashSet<double>();
            for (int station = 0; station < 16; station++)
            {
                double start = Breaks.StartOf(station, 3);
                Assert.That(start, Is.InRange(3d * Breaks.Every, 4d * Breaks.Every - Breaks.Longest));
                starts.Add(start);
            }
            Assert.Greater(starts.Count, 12, "stations take their breaks at their own times");
        }

        [Test]
        public void A_Rabbit_That_Comes_To_Work_After_Its_Break_Was_Due_Waits_For_The_Next()
        {
            List<BreakPost> posts = Garden(3, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            double start = Breaks.StartOf(0, 0);
            plan.Plan(start + Breaks.Late + 0.5d, posts, spots, blocked, false, false);
            Assert.IsNull(plan.Of(0, posts[0].Rabbit, start + Breaks.Late + 0.5d), "it does not appear halfway down the path");
            double next = Breaks.StartOf(0, 1);
            plan.Plan(next, posts, spots, blocked, false, false);
            Assert.IsNotNull(plan.Of(0, posts[0].Rabbit, next), "the next break it takes");
            Assert.IsNull(plan.Of(0, posts[0].Rabbit + 1, next), "a break belongs to the rabbit that took it");
        }

        [Test]
        public void A_Break_Walks_Out_Lingers_And_Walks_Back_Along_Its_Own_Path()
        {
            List<BreakPost> posts = Garden(16, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            Break b = null;
            BreakPost who = default;
            for (double clock = 0d; clock < 20d * 60d && b == null; clock += 0.5d)
            {
                plan.Plan(clock, posts, spots, blocked, false, false);
                foreach (BreakPost p in posts)
                {
                    b = plan.Of(p.Station, p.Rabbit, clock);
                    if (b != null) { who = p; break; }
                }
            }
            Assert.IsNotNull(b, "someone takes a break");
            Assert.AreEqual(who.Home.X, b.From.X); Assert.AreEqual(who.Home.Y, b.From.Y);
            Assert.AreEqual(spots[b.Spot].X, b.To.X); Assert.AreEqual(spots[b.Spot].Y, b.To.Y);
            Assert.AreEqual(BreakLeg.Home, b.LegAt(b.Start - 0.01, out _));
            Assert.AreEqual(BreakLeg.Out, b.LegAt(b.Start, out double along)); Assert.AreEqual(0d, along, 1e-9);
            Assert.AreEqual(BreakLeg.Out, b.LegAt(b.Start + Breaks.Walk / 2d, out along)); Assert.AreEqual(0.5d, along, 1e-9);
            Assert.AreEqual(BreakLeg.There, b.LegAt(b.Start + Breaks.Walk, out along)); Assert.AreEqual(0d, along, 1e-9);
            Assert.AreEqual(BreakLeg.Back, b.LegAt(b.Start + Breaks.Walk + b.Linger, out along)); Assert.AreEqual(0d, along, 1e-9);
            Assert.AreEqual(BreakLeg.Back, b.LegAt(b.Back - 1e-6, out along)); Assert.AreEqual(1d, along, 1e-5);
            Assert.AreEqual(BreakLeg.Home, b.LegAt(b.Back, out _), "home again at the end");
        }

        [Test]
        public void The_Way_To_A_Break_Spot_Keeps_Clear_Of_Every_Other_Station()
        {
            List<BreakPost> posts = Garden(16, out List<Vec2> blocked, out List<Vec2> spots);
            var plan = new Breaks();
            var walked = new HashSet<Break>();
            for (double clock = 0d; clock < 30d * 60d; clock += 0.5d)
            {
                plan.Plan(clock, posts, spots, blocked, false, false);
                foreach (BreakPost p in posts)
                {
                    Break b = plan.Of(p.Station, p.Rabbit, clock);
                    if (b == null || !walked.Add(b)) continue;
                    foreach (Vec2 station in blocked)
                    {
                        if (Vec2.Distance(station, b.From) < Strolls.Clearance) continue;
                        for (int k = 0; k <= 20; k++)
                        {
                            var at = new Vec2(b.From.X + (b.To.X - b.From.X) * k / 20f, b.From.Y + (b.To.Y - b.From.Y) * k / 20f);
                            Assert.GreaterOrEqual(Vec2.Distance(at, station), Strolls.PathClearance - 1f, "station " + p.Station + "'s way passes a station");
                        }
                    }
                }
            }
            Assert.Greater(walked.Count, 10);
        }

        [Test]
        public void Only_Built_Stations_With_A_Rabbit_At_Work_Send_One_On_A_Break()
        {
            List<StationDef> defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            s.Levels = new[] { 3, 2, 0 };
            Rabbit a = Rabbits.ArriveKind(s, Rabbits.FirstKind, "A"), b = Rabbits.ArriveKind(s, Rabbits.FirstKind, "B"), c = Rabbits.ArriveKind(s, Rabbits.FirstKind, "C");
            Assert.IsTrue(Rabbits.Assign(s, a.Id, 0));
            Assert.IsTrue(Rabbits.Assign(s, c.Id, -1));
            var posts = new List<BreakPost>();
            Breaks.PostsOf(defs, s, i => new Vec2(i * 500f, 0f), posts);
            Assert.AreEqual(1, posts.Count, "the empty station and the unbuilt one send nobody, nor does a resting rabbit");
            Assert.AreEqual(0, posts[0].Station);
            Assert.AreEqual(a.Id, posts[0].Rabbit);
            Assert.IsTrue(Rabbits.Assign(s, b.Id, 1));
            Breaks.PostsOf(defs, s, i => new Vec2(i * 500f, 0f), posts);
            Assert.AreEqual(2, posts.Count);
            Assert.AreEqual(500f, posts[1].Home.X);
        }

        static string PlayedGarden(int seed, bool withBreaks, out int breaksTaken)
        {
            List<StationDef> defs = Fixture.Defs();
            GameState s = GameState.NewGame(defs);
            s.Profile.Code = Fixture.CodeFor(seed);
            s.Levels = new[] { 4, 3, 2 };
            for (int i = 0; i < 3; i++) Rabbits.Assign(s, Rabbits.ArriveKind(s, Rabbits.FirstKind, "R" + i).Id, i);
            var rng = new System.Random(seed);
            System.Func<double> roll = rng.NextDouble;
            var plan = new Breaks();
            var posts = new List<BreakPost>();
            var spots = new List<Vec2> { At(-50f, -330f), At(450f, -330f), At(950f, -330f), At(-280f, 40f) };
            var stations = new List<Vec2> { At(0f, 0f), At(500f, 0f), At(1000f, 0f) };
            var seen = new HashSet<string>();
            for (double clock = 0d; clock < 30d * 60d; clock += 0.5d)
            {
                Simulation.Tick(defs, s, 0.5d, null, null, roll);
                if (!withBreaks) continue;
                Breaks.PostsOf(defs, s, i => new Vec2(stations[i].X - 50f, stations[i].Y - 80f), posts);
                plan.Plan(clock, posts, spots, stations, false, false);
                foreach (BreakPost p in posts) if (plan.Of(p.Station, p.Rabbit, clock) != null) seen.Add(p.Station + "/" + Breaks.NumberAt(clock));
            }
            breaksTaken = seen.Count;
            return SaveCodec.Encode(defs, s);
        }

        [Test]
        public void Breaks_Change_Not_One_Byte_Of_The_Garden_Over_Seventeen_Seeds()
        {
            for (int seed = 0; seed < 17; seed++)
            {
                string without = PlayedGarden(seed, false, out _);
                string with = PlayedGarden(seed, true, out int taken);
                Assert.Greater(taken, 0, "seed " + seed + ": rabbits did take breaks");
                Assert.AreEqual(System.Text.Encoding.UTF8.GetBytes(without), System.Text.Encoding.UTF8.GetBytes(with), "seed " + seed);
            }
        }
    }
}

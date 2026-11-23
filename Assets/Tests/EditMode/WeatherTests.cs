using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class WeatherTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 30, 0, 0 };
            s.TutorialStep = Tutorial.Steps;
            s.GardenXp = Garden.XpForLevel(FrogRabbit.FromLevel);
        }

        static double InShower(long day, int shower) => Weather.ShowerStarts(day)[shower] + Weather.ShowerMinutes / 2d / 60d;

        static double Dry(long day) => Weather.ShowerStarts(day)[0] - 0.5d;

        [Test]
        public void The_Same_Day_And_Hour_Always_Give_The_Same_Answer()
        {
            for (long day = 20000; day < 20400; day++)
            {
                double[] a = Weather.ShowerStarts(day), b = Weather.ShowerStarts(day);
                CollectionAssert.AreEqual(a, b, "day " + day);
                for (double hour = 0; hour < 24d; hour += 0.37d)
                    Assert.AreEqual(Weather.IsRaining(day, hour), Weather.IsRaining(day, hour), "day " + day + " hour " + hour);
            }
            var firsts = new HashSet<double>();
            for (long day = 20000; day < 20030; day++) firsts.Add(Weather.ShowerStarts(day)[0]);
            Assert.Greater(firsts.Count, 20, "the hours change from day to day");
        }

        [Test]
        public void Each_Day_Has_Two_Eight_Minute_Showers_That_Never_Overlap()
        {
            int nights = 0, days = 0;
            for (long day = -5; day < 3000; day++)
            {
                double[] starts = Weather.ShowerStarts(day);
                Assert.AreEqual(Weather.ShowersPerDay, starts.Length);
                Assert.AreEqual(2, starts.Length);
                Assert.Less(starts[0], starts[1], "in order");
                Assert.GreaterOrEqual(starts[0], Weather.FirstHour);
                Assert.LessOrEqual(starts[1] + Weather.ShowerMinutes / 60d, Weather.LastHour, "over before the last hour");
                Assert.GreaterOrEqual((starts[1] - starts[0]) * 60d, Weather.ShowerMinutes + Weather.GapMinutes - 1e-9, "never overlapping, and well apart");

                int rainy = 0, showers = 0;
                bool was = false;
                for (int minute = 0; minute < 24 * 60; minute++)
                {
                    bool now = Weather.IsRaining(day, (minute + 0.5d) / 60d);
                    if (now) rainy++;
                    if (now && !was) showers++;
                    was = now;
                }
                Assert.AreEqual(2 * (int)Weather.ShowerMinutes, rainy, "day " + day);
                Assert.AreEqual(2, showers, "day " + day);
                foreach (double start in starts)
                {
                    if (start >= Daylight.NightStart || start < Daylight.DawnStart) nights++;
                    else days++;
                }
            }
            Assert.Greater(nights, 100, "it rains at night too");
            Assert.Greater(days, 100);
        }

        [Test]
        public void A_Shower_Begins_At_Its_Start_And_Is_Over_Eight_Minutes_Later()
        {
            const long day = 20500;
            for (int k = 0; k < 2; k++)
            {
                double start = Weather.ShowerStarts(day)[k];
                Assert.IsFalse(Weather.IsRaining(day, start - 1d / 3600d), "a second before");
                Assert.IsTrue(Weather.IsRaining(day, start));
                Assert.AreEqual(k, Weather.ShowerAt(day, start));
                Assert.IsTrue(Weather.IsRaining(day, start + 7.99d / 60d));
                Assert.IsFalse(Weather.IsRaining(day, start + 8d / 60d), "eight minutes, no more");
                Assert.AreEqual(-1, Weather.ShowerAt(day, start + 8d / 60d));
            }
            Assert.IsFalse(Weather.IsRaining(day, double.NaN));
        }

        [Test]
        public void The_Frog_Rabbit_Comes_Only_In_The_Rain_And_Pays_Ten_Minutes_Of_Income_When_Greeted()
        {
            const long day = 20600;
            Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 1d, day, Dry(day)));
            Assert.IsFalse(FrogRabbit.Here(s), "never outside the rain");
            Assert.IsFalse(FrogRabbit.Greet(defs, s, Modifiers.None, day, Dry(day), 0d, out _, out _));
            for (double hour = 0; hour < 24d; hour += 1d / 60d)
            {
                if (Weather.IsRaining(day, hour)) continue;
                FrogRabbit.Advance(s, 1d, day, hour);
                Assert.IsFalse(FrogRabbit.Here(s), "dry at " + hour);
            }

            Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 1d, day, InShower(day, 0)));
            Assert.IsTrue(FrogRabbit.Here(s));
            Assert.AreEqual(FrogRabbit.StaySeconds, s.FrogLeft, 1e-9);

            var m = Modifiers.None;
            m.IncomeMultiplier = 2d;
            m.SpeedMultiplier = 1.5d;
            m.AwayIncomeMultiplier = 3d;
            Assert.AreEqual(80d * 600d * 3d, FrogRabbit.Reward(defs, s, m).ToDouble(), 1e-6);
            BigNum earned = s.TotalEarned;
            Assert.IsTrue(FrogRabbit.Greet(defs, s, m, day, InShower(day, 0), 0.9d, out BigNum paid, out int clovers));
            Assert.AreEqual(80d * 600d * 3d, paid.ToDouble(), 1e-6);
            Assert.AreEqual(paid.ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(earned.ToDouble(), s.TotalEarned.ToDouble(), 1e-9, "a gift, like a quest's reward: not counted as earned");
            Assert.AreEqual(0, clovers, "a roll of 0.9 brings no clover");
            Assert.AreEqual(0, s.Clovers);
            Assert.IsFalse(FrogRabbit.Here(s), "greeted, it hops home");
            Assert.AreEqual(1, s.FrogsToday);
            Assert.AreEqual(1, s.FrogsMet);
            Assert.IsFalse(FrogRabbit.Greet(defs, s, m, day, InShower(day, 0), 0d, out _, out _), "once");

            Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 1d, day, InShower(day, 1)));
            Assert.IsTrue(FrogRabbit.Greet(defs, s, m, day, InShower(day, 1), FrogRabbit.CloverChance - 0.01d, out _, out clovers));
            Assert.AreEqual(1, clovers);
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(2, s.FrogsMet);
        }

        [Test]
        public void The_Frog_Rabbit_Stays_A_Minute_And_Goes_When_The_Rain_Stops()
        {
            const long day = 20700;
            double start = Weather.ShowerStarts(day)[0];
            Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 0.5d, day, start + 1d / 60d));
            Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 59d, day, start + 2d / 60d));
            Assert.IsTrue(FrogRabbit.Here(s));
            Assert.AreEqual(FrogEvent.Left, FrogRabbit.Advance(s, 1.5d, day, start + 3d / 60d), "a minute, then home");
            Assert.IsFalse(FrogRabbit.Here(s));
            Assert.IsFalse(FrogRabbit.Greet(defs, s, Modifiers.None, day, start + 3d / 60d, 0d, out _, out _), "gone: nothing to greet");
            Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 1d, day, start + 4d / 60d), "one frog a shower, greeted or not");

            double second = Weather.ShowerStarts(day)[1];
            Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 1d, day, second + 7.5d / 60d));
            Assert.AreEqual(FrogEvent.Left, FrogRabbit.Advance(s, 1d, day, second + 8.1d / 60d), "the rain is over");
            Assert.IsFalse(FrogRabbit.Here(s));
            Assert.IsFalse(FrogRabbit.Greet(defs, s, Modifiers.None, day, second + 8.1d / 60d, 0d, out _, out _));
            Assert.AreEqual(0, s.FrogsMet);
            Assert.AreEqual(0d, s.Coins.ToDouble(), 1e-9);
        }

        [Test]
        public void Two_Frog_Rabbits_A_Day_At_Most_And_A_Clock_Set_Back_Brings_Nothing_More()
        {
            const long day = 20800;
            for (int k = 0; k < 2; k++)
            {
                Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 1d, day, InShower(day, k)));
                Assert.IsTrue(FrogRabbit.Greet(defs, s, Modifiers.None, day, InShower(day, k), 0.5d, out _, out _));
            }
            Assert.AreEqual(FrogRabbit.PerDay, s.FrogsToday);
            Assert.AreEqual(2, FrogRabbit.PerDay);
            BigNum coins = s.Coins;

            for (int k = 0; k < 2; k++)
            {
                Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 1d, day, InShower(day, k)));
                Assert.IsFalse(FrogRabbit.Here(s));
            }
            Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 1d, day - 1, InShower(day - 1, 0)));
            Assert.IsFalse(FrogRabbit.Here(s));
            Assert.IsFalse(FrogRabbit.Greet(defs, s, Modifiers.None, day - 1, InShower(day - 1, 0), 0d, out _, out _));
            Assert.AreEqual(day, s.FrogDay, "yesterday does not start the count over");
            Assert.AreEqual(2, s.FrogsToday);
            Assert.AreEqual(coins.ToDouble(), s.Coins.ToDouble(), 1e-9);

            SetUp();
            FrogRabbit.Advance(s, 1d, day, InShower(day, 0));
            Assert.IsTrue(FrogRabbit.Here(s));
            Assert.AreEqual(FrogEvent.Left, FrogRabbit.Advance(s, 1d, day - 1, InShower(day - 1, 0)));
            Assert.IsFalse(FrogRabbit.Greet(defs, s, Modifiers.None, day - 1, InShower(day - 1, 0), 0d, out _, out _));

            Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 1d, day + 1, InShower(day + 1, 0)));
            Assert.AreEqual(0, s.FrogsToday);
            Assert.IsTrue(FrogRabbit.Greet(defs, s, Modifiers.None, day + 1, InShower(day + 1, 0), 0.5d, out _, out _));
            Assert.AreEqual(1, s.FrogsToday);
        }

        [Test]
        public void The_Frog_Rabbit_Waits_For_Its_Garden_Level()
        {
            const long day = 20900;
            s.GardenXp = FrogRabbit.FromLevel > 1 ? Garden.XpForLevel(FrogRabbit.FromLevel - 1) : 0d;
            if (FrogRabbit.FromLevel > 1)
            {
                Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 1d, day, InShower(day, 0)), "not before garden level " + FrogRabbit.FromLevel);
                Assert.IsFalse(FrogRabbit.Here(s));
            }
            s.GardenXp = Garden.XpForLevel(FrogRabbit.FromLevel);
            Assert.AreEqual(FrogEvent.Arrived, FrogRabbit.Advance(s, 1d, day, InShower(day, 0)), "the shower's frog was not used up by the wait");
            s = GameState.NewGame(defs);
            s.GardenXp = Garden.XpForLevel(FrogRabbit.FromLevel);
            Assert.AreEqual(FrogEvent.None, FrogRabbit.Advance(s, 1d, day, InShower(day, 0)));
        }

        [Test]
        public void Meeting_Five_Frog_Rabbits_Is_An_Achievement()
        {
            var q = new QuestDef { Id = "ech-5", Metric = QuestMetric.FrogsMet, Target = 5, RewardClovers = 2 };
            for (long day = 21000; day < 21003; day++)
                for (int k = 0; k < 2; k++)
                {
                    FrogRabbit.Advance(s, 1d, day, InShower(day, k));
                    FrogRabbit.Greet(defs, s, Modifiers.None, day, InShower(day, k), 0.9d, out _, out _);
                    if (s.FrogsMet == 4) Assert.IsFalse(Quests.IsComplete(q, defs, s));
                }
            Assert.AreEqual(6, s.FrogsMet);
            Assert.AreEqual(6d, Quests.Progress(q, defs, s), 1e-9);
            Assert.IsTrue(Quests.IsComplete(q, defs, s));
            int clovers = s.Clovers;
            Assert.IsTrue(Quests.TryClaim(q, defs, s, Modifiers.None, out _));
            Assert.AreEqual(clovers + 2, s.Clovers);
        }

        [Test]
        public void The_Save_Keeps_The_Frogs_And_An_Old_Save_Has_Met_None()
        {
            s.FrogDay = 21100;
            s.FrogShowers = 1;
            s.FrogsToday = 1;
            s.FrogsMet = 7;
            s.FrogLeft = 42.5d;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(21100, r.FrogDay);
            Assert.AreEqual(1, r.FrogShowers);
            Assert.AreEqual(1, r.FrogsToday);
            Assert.AreEqual(7, r.FrogsMet);
            Assert.AreEqual(42.5d, r.FrogLeft, 1e-9);

            string old = SaveCodec.Encode(defs, GameState.NewGame(defs));
            int at = old.IndexOf(",\"frog\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0);
            int end = old.IndexOf('}', at);
            old = old.Remove(at, end - at + 1);
            Assert.AreEqual(-1, old.IndexOf("\"frog\"", System.StringComparison.Ordinal));
            LoadResult fromOld = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, fromOld.Status);
            Assert.AreEqual(-1, fromOld.State.FrogDay);
            Assert.AreEqual(0, fromOld.State.FrogsMet);
            Assert.AreEqual(0d, fromOld.State.FrogLeft, 1e-9);

            string edited = SaveCodec.Encode(defs, s).Replace("\"left\":42.5", "\"left\":4000").Replace("\"today\":1,\"met\"", "\"today\":9,\"met\"");
            GameState e = SaveCodec.Decode(defs, edited).State;
            Assert.AreEqual(FrogRabbit.StaySeconds, e.FrogLeft, 1e-9);
            Assert.AreEqual(FrogRabbit.PerDay, e.FrogsToday);
        }
    }
}

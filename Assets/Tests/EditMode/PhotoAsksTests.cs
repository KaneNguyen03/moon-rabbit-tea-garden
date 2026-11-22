using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>#56: every four minutes of open play a rabbit asks for its picture; answering pays two minutes of income, ten times a day at most.</summary>
    public class PhotoAsksTests
    {
        List<StationDef> defs;
        GameState s;
        Rabbit worker;
        const long Day = 500;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 0 }; // tea and leaf open, jam not yet
            worker = Rabbits.Arrive(s, 0, "Bông", 0d);
            Rabbits.Assign(s, worker.Id, 0);
        }

        PhotoAskEvent Run(double seconds, long day = Day, double step = 1d)
        {
            PhotoAskEvent last = PhotoAskEvent.None;
            for (double t = 0; t < seconds; t += step)
            {
                PhotoAskEvent e = PhotoAsks.Advance(defs, s, step, day, () => 0.2);
                if (e != PhotoAskEvent.None) last = e;
            }
            return last;
        }

        [Test]
        public void A_Rabbit_Asks_After_Four_Minutes_Of_Open_Play_And_The_Bubble_Goes_After_Ninety_Seconds()
        {
            Assert.IsFalse(PhotoAsks.Waiting(s), "nobody asks to begin with");
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.Interval - 1d));
            Assert.IsFalse(PhotoAsks.Waiting(s), "not before four minutes");
            Assert.AreEqual(PhotoAskEvent.Asked, Run(2d));
            Assert.IsTrue(PhotoAsks.Waiting(s));
            Assert.AreEqual(worker.Id, s.PhotoAskRabbit, "the one rabbit at work asks");
            Assert.AreEqual(PhotoAsks.StaySeconds, s.PhotoAskLeft, 1.5d);

            BigNum coins = s.Coins;
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.StaySeconds - 2d));
            Assert.IsTrue(PhotoAsks.Waiting(s), "still asking");
            Assert.AreEqual(PhotoAskEvent.Expired, Run(3d));
            Assert.IsFalse(PhotoAsks.Waiting(s), "the bubble went by itself");
            Assert.AreEqual(0L, s.PhotoAskRabbit);
            Assert.IsTrue(s.Coins.Equals(coins), "and nothing was paid or lost");
            Assert.AreEqual(0, s.PhotoAsksToday, "a missed invitation does not use up the day's ten");

            // The clock starts again when the bubble goes: the next ask is a full four minutes away.
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.Interval - 5d));
            Assert.IsFalse(PhotoAsks.Waiting(s));
            Assert.AreEqual(PhotoAskEvent.Asked, Run(6d));
        }

        [Test]
        public void No_Second_Invitation_Comes_While_One_Is_Waiting()
        {
            Run(PhotoAsks.Interval + 1d);
            long asking = s.PhotoAskRabbit;
            Assert.AreNotEqual(0L, asking);
            Assert.IsFalse(PhotoAsks.Ask(defs, s, Day, () => 0.9), "one at a time");
            Assert.AreEqual(asking, s.PhotoAskRabbit);
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.StaySeconds - 5d), "the clock holds while one waits");
            Assert.AreEqual(asking, s.PhotoAskRabbit);
        }

        [Test]
        public void A_Working_Or_Seated_Rabbit_Asks_Never_One_At_A_Closed_Station_Or_Without_A_Seat()
        {
            Rabbit closed = Rabbits.Arrive(s, 0, "Đóng", 0d);
            closed.Station = 2; // the jam pot is not open yet
            Rabbit seatA = Rabbits.Arrive(s, 1, "Nghỉ A", 0d);
            Rabbit seatB = Rabbits.Arrive(s, 1, "Nghỉ B", 0d);
            Rabbit unseated = Rabbits.Arrive(s, 0, "Không chỗ", 0d);
            s.RestSpots = 2;
            List<Rabbit> asked = PhotoAsks.Candidates(defs, s);
            CollectionAssert.AreEquivalent(new[] { worker, seatA, seatB }, asked);
            CollectionAssert.DoesNotContain(asked, closed);
            CollectionAssert.DoesNotContain(asked, unseated, "a rabbit resting where it cannot be seen cannot be photographed");

            var picked = new HashSet<long>();
            for (int i = 0; i < 3; i++)
            {
                double roll = (i + 0.5) / 3d;
                s.PhotoAskRabbit = 0;
                Assert.IsTrue(PhotoAsks.Ask(defs, s, Day, () => roll));
                picked.Add(s.PhotoAskRabbit);
            }
            Assert.AreEqual(3, picked.Count, "the roll decides which");
        }

        [Test]
        public void A_Garden_With_Nobody_To_Ask_Holds_The_Clock_Until_Somebody_Is()
        {
            s.Rabbits.Clear();
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.Interval + 30d));
            Assert.IsFalse(PhotoAsks.Waiting(s));
            Assert.AreEqual(PhotoAsks.Interval, s.PhotoAskClock, 1e-9, "ready the moment a rabbit is there");
            Rabbits.Assign(s, Rabbits.Arrive(s, 0, "Mây", 0d).Id, 1);
            Assert.AreEqual(PhotoAskEvent.Asked, Run(1d));
        }

        [Test]
        public void Answering_Pays_Exactly_Two_Minutes_Of_Income_Under_The_Multipliers()
        {
            Modifiers m = Modifiers.None;
            m.IncomeMultiplier = 2d;
            m.SpeedMultiplier = 1.5d;
            Run(PhotoAsks.Interval + 1d);
            BigNum expected = Economy.IncomePerSecond(defs, s) * (2d * 1.5d * PhotoAsks.RewardSeconds);
            Assert.AreEqual(120d, PhotoAsks.RewardSeconds);
            Assert.IsTrue(PhotoAsks.Reward(defs, s, m).Equals(expected));
            Assert.Greater(expected.ToDouble(), 0d);

            BigNum before = s.Coins, earned = s.TotalEarned;
            Assert.IsTrue(PhotoAsks.Answer(defs, s, m, Day, out BigNum paid));
            Assert.IsTrue(paid.Equals(expected), "exactly 120 s × multipliers");
            Assert.IsTrue(s.Coins.Equals(before + expected));
            Assert.IsTrue(s.TotalEarned.Equals(earned + expected), "it counts as earned, like a guest's order");
            Assert.AreEqual(1, s.PhotoAsksToday);
            Assert.AreEqual(1L, s.PhotoAsksAnswered);
            Assert.IsFalse(PhotoAsks.Waiting(s), "answered");
            Assert.AreEqual(0d, s.PhotoAskClock, "and the next is four minutes away");

            Assert.IsFalse(PhotoAsks.Answer(defs, s, m, Day, out paid), "nobody asking: nothing to pay");
            Assert.IsTrue(paid.Equals(BigNum.Zero));
        }

        [Test]
        public void Ten_A_Day_By_The_Clamped_Day_And_A_Clock_Turned_Back_Gives_Nothing_Back()
        {
            for (int i = 0; i < PhotoAsks.PerDay; i++)
            {
                Assert.AreEqual(PhotoAskEvent.Asked, Run(PhotoAsks.Interval + 1d), "ask " + (i + 1));
                Assert.IsTrue(PhotoAsks.Answer(defs, s, Modifiers.None, Day, out _));
            }
            Assert.AreEqual(10, PhotoAsks.PerDay);
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.Interval * 2d), "the day's ten are done");
            Assert.IsFalse(PhotoAsks.Ask(defs, s, Day, () => 0.5));
            Assert.IsFalse(PhotoAsks.Waiting(s));

            // #78: the clock turned back to yesterday does not start the count over.
            Assert.AreEqual(PhotoAskEvent.None, Run(PhotoAsks.Interval * 2d, Day - 1));
            Assert.IsFalse(PhotoAsks.Ask(defs, s, Day - 1, () => 0.5));
            Assert.AreEqual(Day, s.PhotoAskDay);
            Assert.AreEqual(PhotoAsks.PerDay, s.PhotoAsksToday);

            // Tomorrow asks again.
            Assert.AreEqual(PhotoAskEvent.Asked, Run(1d, Day + 1));
            Assert.AreEqual(0, s.PhotoAsksToday);
            Assert.AreEqual(Day + 1, s.PhotoAskDay);
        }

        [Test]
        public void Closing_Without_A_Shot_Loses_That_Invitation_And_Nothing_Else()
        {
            Run(PhotoAsks.Interval + 1d);
            BigNum coins = s.Coins;
            long photos = s.PhotosTaken;
            int clovers = s.Clovers, golden = s.GoldenCarrots;
            Assert.IsTrue(PhotoAsks.Decline(s));
            Assert.IsFalse(PhotoAsks.Waiting(s));
            Assert.IsTrue(s.Coins.Equals(coins));
            Assert.AreEqual(photos, s.PhotosTaken);
            Assert.AreEqual(clovers, s.Clovers);
            Assert.AreEqual(golden, s.GoldenCarrots);
            Assert.AreEqual(0, s.PhotoAsksToday, "nor one of the day's ten");
            Assert.AreEqual(0d, s.PhotoAskClock, "the next is a full interval away");
            Assert.IsFalse(PhotoAsks.Decline(s), "nothing left to decline");
        }

        [Test]
        public void An_Invitation_From_A_Rabbit_That_Is_No_Longer_Here_Goes()
        {
            Run(PhotoAsks.Interval + 1d);
            s.Rabbits.Clear();
            Assert.AreEqual(PhotoAskEvent.Expired, Run(1d));
            Assert.IsFalse(PhotoAsks.Waiting(s));
        }

        [Test]
        public void Bad_Time_Steps_Change_Nothing()
        {
            Assert.AreEqual(PhotoAskEvent.None, PhotoAsks.Advance(defs, s, double.NaN, Day, () => 0.2));
            Assert.AreEqual(PhotoAskEvent.None, PhotoAsks.Advance(defs, s, -5d, Day, () => 0.2));
            Assert.AreEqual(0d, s.PhotoAskClock);
        }

        [Test]
        public void The_Daily_Quest_Counts_Answers_From_The_Start_Of_The_Day()
        {
            var daily = new QuestDef { Id = "ngay-anh", Metric = QuestMetric.PhotoAsksAnswered, Target = 3, Daily = true, RewardClovers = 1 };
            var quests = new List<QuestDef> { daily };
            s.PhotoAsksAnswered = 7; // yesterday's
            Quests.RollDaily(quests, defs, s, Day, Modifiers.None);
            Assert.AreEqual(0d, Quests.Progress(daily, defs, s));
            for (int i = 0; i < 3; i++)
            {
                Run(PhotoAsks.Interval + 1d);
                Assert.IsTrue(PhotoAsks.Answer(defs, s, Modifiers.None, Day, out _));
            }
            Assert.AreEqual(3d, Quests.Progress(daily, defs, s));
            Assert.IsTrue(Quests.CanClaim(daily, defs, s));
            Assert.Greater((int)QuestMetric.PhotoAsksAnswered, (int)QuestMetric.PhotosWithSeasonRabbit, "new metrics go at the end: assets store the number");
        }

        [Test]
        public void The_Save_Keeps_The_Invitation_The_Clock_And_The_Counts()
        {
            Run(PhotoAsks.Interval + 1d);
            PhotoAsks.Answer(defs, s, Modifiers.None, Day, out _);
            Run(PhotoAsks.Interval + 11d);
            s.DailyPhotoAsksBase = 1;
            string json = SaveCodec.Encode(defs, s);
            GameState back = SaveCodec.Decode(defs, json).State;
            Assert.AreEqual(s.PhotoAskRabbit, back.PhotoAskRabbit);
            Assert.AreEqual(s.PhotoAskLeft, back.PhotoAskLeft, 1e-9);
            Assert.AreEqual(s.PhotoAskClock, back.PhotoAskClock, 1e-9);
            Assert.AreEqual(1, back.PhotoAsksToday);
            Assert.AreEqual(Day, back.PhotoAskDay);
            Assert.AreEqual(1L, back.PhotoAsksAnswered);
            Assert.AreEqual(1L, back.DailyPhotoAsksBase);

            // A save from before #56 starts the clock from nothing; numbers out of range are brought back in.
            GameState old = SaveCodec.Decode(defs, json.Replace("\"photoAsk\"", "\"somethingElse\"")).State;
            Assert.AreEqual(0L, old.PhotoAskRabbit);
            Assert.AreEqual(0d, old.PhotoAskClock);
            Assert.AreEqual(-1L, old.PhotoAskDay);
            Assert.AreEqual(0L, old.PhotoAsksAnswered);
            string wild = json.Replace("\"today\":1,\"day\":" + Day, "\"today\":99,\"day\":" + Day)
                              .Replace("\"left\":" + s.PhotoAskLeft.ToString("R", System.Globalization.CultureInfo.InvariantCulture), "\"left\":9999");
            GameState clamped = SaveCodec.Decode(defs, wild).State;
            Assert.AreEqual(PhotoAsks.PerDay, clamped.PhotoAsksToday);
            Assert.LessOrEqual(clamped.PhotoAskLeft, PhotoAsks.StaySeconds);
        }
    }
}

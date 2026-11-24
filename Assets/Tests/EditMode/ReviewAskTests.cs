using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class ReviewAskTests
    {
        const long Start = 20_000L;

        List<StationDef> defs;
        GameState s;
        ReviewWatch watch;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            Tutorial.Skip(s);
            Seasons.Clamp(s, Start);
            s.PlaySeconds = ReviewAsk.MinPlaySeconds;
            watch = new ReviewWatch();
        }

        long PlayUntil(long day)
        {
            Seasons.Clamp(s, day);
            return day;
        }

        ReviewMoment Ask(long day, ReviewMoment moment = ReviewMoment.GoldenRabbit, bool busy = false)
        {
            watch.Note(s, day, moment);
            return watch.Take(s, day, busy);
        }

        [Test]
        public void The_Numbers_Are_The_Issues_Defaults()
        {
            Assert.AreEqual(3L, ReviewAsk.MinDaysPlayed);
            Assert.AreEqual(30d * 60d, ReviewAsk.MinPlaySeconds);
            Assert.AreEqual(3, ReviewAsk.MaxAsks);
            Assert.AreEqual(60L, ReviewAsk.DaysBetweenAsks);
            Assert.AreEqual(4, ReviewAsk.HappyTier, "Vàng is tier 4");
        }

        [Test]
        public void Never_Before_The_Third_Day()
        {
            Assert.AreEqual(ReviewMoment.None, Ask(PlayUntil(Start + 1)), "day 2 of play");
            Assert.AreEqual(ReviewMoment.None, Ask(PlayUntil(Start + 2)), "two days played is not three");
            Assert.AreEqual(0, s.Review.Asks);
            Assert.AreEqual(ReviewMoment.GoldenRabbit, Ask(PlayUntil(Start + 3)));
            Assert.AreEqual(1, s.Review.Asks);
            Assert.AreEqual(Start + 3, s.Review.LastAskDay);
        }

        [Test]
        public void Never_Before_Half_An_Hour_Of_Play()
        {
            long day = PlayUntil(Start + 5);
            s.PlaySeconds = ReviewAsk.MinPlaySeconds - 1d;
            Assert.AreEqual(ReviewMoment.None, Ask(day));
            s.PlaySeconds = ReviewAsk.MinPlaySeconds;
            Assert.AreEqual(ReviewMoment.ThreeStars, Ask(day, ReviewMoment.ThreeStars));
        }

        [Test]
        public void Never_Twice_Within_Sixty_Days_And_Three_Times_At_Most()
        {
            long day = PlayUntil(Start + 3);
            Assert.AreEqual(ReviewMoment.GoldenRabbit, Ask(day));
            Assert.AreEqual(ReviewMoment.None, Ask(day), "the same day");
            Assert.AreEqual(ReviewMoment.None, Ask(PlayUntil(day + 59)), "59 days later");
            Assert.AreEqual(ReviewMoment.LandOpened, Ask(PlayUntil(day + 60), ReviewMoment.LandOpened));
            Assert.AreEqual(ReviewMoment.TenHearts, Ask(PlayUntil(day + 120), ReviewMoment.TenHearts));
            Assert.AreEqual(3, s.Review.Asks);
            Assert.AreEqual(ReviewMoment.None, Ask(PlayUntil(day + 400)), "three in a lifetime");
            Assert.IsTrue(ReviewAsk.Done(s));
        }

        [Test]
        public void A_Thumbs_Up_Ends_The_Asking_For_Good()
        {
            long day = PlayUntil(Start + 3);
            Assert.AreEqual(ReviewMoment.GoldenRabbit, Ask(day));
            ReviewAsk.Like(s);
            Assert.IsTrue(s.Review.Liked);
            Assert.IsTrue(ReviewAsk.Done(s));
            Assert.AreEqual(ReviewMoment.None, Ask(PlayUntil(day + 61)));
            Assert.AreEqual(ReviewMoment.None, Ask(PlayUntil(day + 500)));
            Assert.AreEqual(1, s.Review.Asks);
        }

        [Test]
        public void Not_During_A_Mini_Game_But_Right_After_It()
        {
            long day = PlayUntil(Start + 4);
            Assert.IsTrue(watch.Note(s, day, ReviewMoment.ThreeStars));
            Assert.AreEqual(ReviewMoment.None, watch.Take(s, day, true), "the result card of the mini game is still up");
            Assert.AreEqual(0, s.Review.Asks, "waiting is not asking");
            Assert.AreEqual(ReviewMoment.ThreeStars, watch.Waiting);
            Assert.AreEqual(ReviewMoment.ThreeStars, watch.Take(s, day, false));
            Assert.AreEqual(ReviewMoment.None, watch.Waiting);
            Assert.AreEqual(1, s.Review.Asks);
        }

        [Test]
        public void Not_During_The_Guidance()
        {
            long day = PlayUntil(Start + 4);
            s.TutorialStep = 2;
            Assert.IsFalse(watch.Note(s, day, ReviewMoment.GoldenRabbit));
            Assert.AreEqual(ReviewMoment.None, watch.Take(s, day, false));
            Assert.IsFalse(watch.CanAsk(s, day, false));
            Tutorial.Skip(s);
            Assert.IsTrue(watch.CanAsk(s, day, false));
            Assert.IsFalse(watch.CanAsk(s, day, true), "a mini game or another card is up");
        }

        [Test]
        public void Not_After_A_Crash_Or_A_Purchase_That_Did_Not_Go_Through()
        {
            long day = PlayUntil(Start + 4);
            watch.Crashed = true;
            Assert.AreEqual(ReviewMoment.None, Ask(day));
            watch.Crashed = false;
            watch.PurchaseFailed = true;
            Assert.AreEqual(ReviewMoment.None, Ask(day));
            Assert.IsTrue(watch.Note(s, day, ReviewMoment.None) == false, "no moment, nothing to wait for");
            watch.PurchaseFailed = false;
            Assert.IsTrue(watch.Note(s, day, ReviewMoment.GoldenRabbit));
            watch.PurchaseFailed = true;
            Assert.AreEqual(ReviewMoment.None, watch.Take(s, day, false), "a purchase failed between the moment and the card");
            Assert.AreEqual(0, s.Review.Asks);
        }

        [Test]
        public void Only_A_Happy_Moment_Opens_The_Card_And_It_Goes_Stale()
        {
            long day = PlayUntil(Start + 4);
            Assert.AreEqual(ReviewMoment.None, watch.Take(s, day, false), "nothing happy happened");
            Assert.IsTrue(watch.Note(s, day, ReviewMoment.LandOpened));
            s.PlaySeconds += ReviewAsk.FreshSeconds + 1d;
            Assert.AreEqual(ReviewMoment.None, watch.Take(s, day, false), "a card long after the moment is not about it");
            Assert.AreEqual(ReviewMoment.None, watch.Waiting);
            Assert.AreEqual(0, s.Review.Asks);
        }

        [Test]
        public void Golden_Or_Better_Summons_And_Three_Stars_Are_Happy()
        {
            Assert.IsFalse(ReviewAsk.IsHappy(new Rabbit { Tier = 3 }));
            Assert.IsTrue(ReviewAsk.IsHappy(new Rabbit { Tier = 4 }));
            Assert.IsTrue(ReviewAsk.IsHappy(new Rabbit { Tier = 5 }));
            Assert.IsFalse(ReviewAsk.IsHappy((Rabbit)null));
            Assert.IsFalse(ReviewAsk.IsHappy(2));
            Assert.IsTrue(ReviewAsk.IsHappy(3));
            Assert.AreEqual("golden_rabbit", ReviewAsk.NameOf(ReviewMoment.GoldenRabbit));
            Assert.AreEqual("three_stars", ReviewAsk.NameOf(ReviewMoment.ThreeStars));
            Assert.AreEqual("land_opened", ReviewAsk.NameOf(ReviewMoment.LandOpened));
            Assert.AreEqual("ten_hearts", ReviewAsk.NameOf(ReviewMoment.TenHearts));
        }

        [Test]
        public void The_Feedback_Is_One_Line_In_A_Mail_To_Support_With_The_Player_Code()
        {
            Assert.AreEqual("hai dòng thành một", ReviewAsk.OneLine("  hai dòng\nthành   một \r\n"));
            Assert.AreEqual(ReviewAsk.FeedbackMaxLength, ReviewAsk.OneLine(new string('a', 500)).Length);
            Assert.AreEqual("", ReviewAsk.OneLine(null));
            string mail = ReviewAsk.MailTo("hotro@vuontratho.game", "Góp ý THO-ABC123", "chậm quá & khó\nMã: THO-ABC123");
            StringAssert.StartsWith("mailto:hotro@vuontratho.game?subject=", mail);
            StringAssert.Contains("THO-ABC123", mail);
            StringAssert.Contains("%26", mail, "an & in the line does not cut the mail short");
            StringAssert.DoesNotContain(" ", mail);
            StringAssert.DoesNotContain("store", mail.ToLowerInvariant());
        }

        [Test]
        public void The_Record_Survives_A_Save_And_Old_Saves_Start_Clean()
        {
            s.Review.Asks = 2;
            s.Review.LastAskDay = Start + 70;
            s.Review.Liked = true;
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(2, back.Review.Asks);
            Assert.AreEqual(Start + 70, back.Review.LastAskDay);
            Assert.IsTrue(back.Review.Liked);

            string json = SaveCodec.Encode(defs, GameState.NewGame(defs));
            int at = json.IndexOf(",\"review\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0, "the record is written");
            string old = json.Substring(0, at) + json.Substring(json.IndexOf('}', at) + 1);
            LoadResult loaded = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            Assert.AreEqual(0, loaded.State.Review.Asks);
            Assert.AreEqual(-1L, loaded.State.Review.LastAskDay);
            Assert.IsFalse(loaded.State.Review.Liked);
            Assert.AreEqual(14, SaveCodec.CurrentVersion);

            string bad = json.Replace("\"review\":{\"asks\":0", "\"review\":{\"asks\":-9");
            Assert.AreEqual(0, SaveCodec.Decode(defs, bad).State.Review.Asks);
        }

        [Test]
        public void The_Fake_Prompt_Counts_And_Says_What_It_Would_Open()
        {
            var said = new List<string>();
            var fake = new FakeReviewPrompt { Said = said.Add };
            fake.RequestReview();
            fake.OpenStorePage();
            fake.OpenStorePage();
            Assert.AreEqual(1, fake.Requests);
            Assert.AreEqual(2, fake.StorePages);
            Assert.AreEqual(3, said.Count);
        }
    }
}

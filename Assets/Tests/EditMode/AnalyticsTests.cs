using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class AnalyticsTests
    {
        static GameState NewGarden() => GameState.NewGame(Fixture.Defs());

        [Test]
        public void Memory_Keeps_Every_Event_Property_And_Exception_And_None_Keeps_Nothing()
        {
            var mem = new MemoryAnalytics();
            mem.Log(AnalyticsEvents.NewMoon, new AnalyticsParam(AnalyticsEvents.Moon, 2), new AnalyticsParam(AnalyticsEvents.Via, "golden"),
                new AnalyticsParam(AnalyticsEvents.Seconds, 1.5d), new AnalyticsParam(AnalyticsEvents.Late, true));
            mem.SetUserProperty(AnalyticsEvents.UserMoon, "2");
            var e = new System.InvalidOperationException("save");
            mem.RecordException(e);

            Assert.AreEqual(1, mem.Count(AnalyticsEvents.NewMoon));
            MemoryAnalytics.Event logged = mem.Last(AnalyticsEvents.NewMoon);
            Assert.AreEqual(2L, logged[AnalyticsEvents.Moon].Number);
            Assert.AreEqual(AnalyticsParam.Kind.Text, logged[AnalyticsEvents.Via].Type);
            Assert.AreEqual("golden", logged[AnalyticsEvents.Via].Text);
            Assert.AreEqual(1.5d, logged[AnalyticsEvents.Seconds].Real);
            Assert.AreEqual(1L, logged[AnalyticsEvents.Late].Number, "a flag goes out as 0 or 1");
            Assert.IsFalse(logged.Has(AnalyticsEvents.Reason));
            Assert.AreEqual("2", mem.UserProperties[AnalyticsEvents.UserMoon]);
            Assert.AreSame(e, mem.Exceptions[0]);

            IAnalytics none = new NoAnalytics();
            Assert.DoesNotThrow(() =>
            {
                none.Log(AnalyticsEvents.GameOpen);
                none.SetUserProperty(AnalyticsEvents.UserMoon, "1");
                none.RecordException(e);
                none.LogSessionStart(NewGarden());
            });
        }

        [Test]
        public void Every_Name_Is_Stable_Snake_Case_Short_Unique_And_None_Firebase_Keeps_For_Itself()
        {
            var snake = new Regex("^[a-z][a-z0-9_]*$");
            var reserved = new HashSet<string>
            {
                "ad_activeview", "ad_click", "ad_exposure", "ad_impression", "ad_query", "ad_reward", "adunit_exposure", "app_background",
                "app_clear_data", "app_exception", "app_remove", "app_store_refund", "app_store_subscription_cancel",
                "app_store_subscription_convert", "app_store_subscription_renew", "app_update", "app_upgrade", "dynamic_link_app_open",
                "dynamic_link_app_update", "dynamic_link_first_open", "error", "first_open", "first_visit", "in_app_purchase",
                "notification_dismiss", "notification_foreground", "notification_open", "notification_receive", "os_update",
                "session_start", "session_start_with_rollout", "user_engagement",
            };
            var seen = new HashSet<string>();
            foreach (string name in AnalyticsEvents.All)
            {
                StringAssert.IsMatch(snake.ToString(), name);
                Assert.LessOrEqual(name.Length, 40, name);
                Assert.IsFalse(reserved.Contains(name), name + " is Firebase's own");
                Assert.IsFalse(name.StartsWith("firebase_") || name.StartsWith("google_") || name.StartsWith("ga_"), name);
                Assert.IsTrue(seen.Add(name), name + " twice");
            }
            Assert.AreEqual(21, AnalyticsEvents.All.Length, "add a new event to All as well");
            foreach (string step in AnalyticsEvents.StepNames) StringAssert.IsMatch(snake.ToString(), step);
            Assert.AreEqual(Tutorial.Steps, AnalyticsEvents.StepNames.Length, "one name per step of the guidance");
            Assert.AreEqual("cancelled", AnalyticsEvents.ReasonOf(PurchaseOutcome.Cancelled));
            Assert.AreEqual("failed", AnalyticsEvents.ReasonOf(PurchaseOutcome.Failed));
            Assert.AreEqual("unavailable", AnalyticsEvents.ReasonOf(PurchaseOutcome.Unavailable));
            Assert.AreEqual("duplicate", AnalyticsEvents.ReasonOf(PurchaseOutcome.Duplicate));
        }

        [Test]
        public void The_Watch_Logs_Each_Garden_Level_Once_And_Nothing_When_The_Garden_Goes_Back()
        {
            GameState s = NewGarden();
            var mem = new MemoryAnalytics();
            var watch = new AnalyticsWatch();
            watch.Reset(s);
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(0, mem.Events.Count, "nothing moved");

            s.GardenXp = Garden.XpForLevel(4);
            watch.Note(Fixture.Defs(), s, mem);
            List<MemoryAnalytics.Event> ups = mem.All(AnalyticsEvents.GardenLevelUp);
            Assert.AreEqual(3, ups.Count, "levels 2, 3 and 4, each once");
            Assert.AreEqual(new[] { 2L, 3L, 4L }, ups.ConvertAll(e => e[AnalyticsEvents.Level].Number).ToArray());
            Assert.AreEqual("4", mem.UserProperties[AnalyticsEvents.UserGardenLevel]);
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(3, mem.Count(AnalyticsEvents.GardenLevelUp), "a second look logs nothing more");

            s.GardenXp = 0d;
            s.Season = 1;
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(3, mem.Count(AnalyticsEvents.GardenLevelUp), "a new moon takes the level back: nothing to log");
            s.GardenXp = Garden.XpForLevel(2);
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(4, mem.Count(AnalyticsEvents.GardenLevelUp));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.GardenLevelUp)[AnalyticsEvents.Moon].Number, "with the moon it happened on");
        }

        [Test]
        public void The_Watch_Logs_Each_Tutorial_Step_Once_Then_The_Whole_Guidance_Once()
        {
            GameState s = NewGarden();
            var mem = new MemoryAnalytics();
            var watch = new AnalyticsWatch();
            watch.Reset(s);
            s.TutorialStep = 3;
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(new[] { 0L, 1L, 2L }, mem.All(AnalyticsEvents.TutorialStep).ConvertAll(e => e[AnalyticsEvents.Step].Number).ToArray());
            Assert.AreEqual("tap_rabbit", mem.All(AnalyticsEvents.TutorialStep)[0][AnalyticsEvents.StepName].Text);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.TutorialComplete));

            s.TutorialStep = Tutorial.Steps;
            watch.Note(Fixture.Defs(), s, mem);
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(Tutorial.Steps, mem.Count(AnalyticsEvents.TutorialStep), "every step once");
            Assert.AreEqual("place_rabbit", mem.Last(AnalyticsEvents.TutorialStep)[AnalyticsEvents.StepName].Text);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TutorialComplete));
        }

        [Test]
        public void Skipping_The_Guidance_Logs_One_Skip_At_The_Step_And_No_Steps_Left()
        {
            GameState s = NewGarden();
            var mem = new MemoryAnalytics();
            var watch = new AnalyticsWatch();
            s.TutorialStep = 2;
            watch.Reset(s);
            watch.SkipTutorial(s, mem);
            watch.Note(Fixture.Defs(), s, mem);
            Assert.IsTrue(Tutorial.IsDone(s));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TutorialSkip));
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.TutorialSkip)[AnalyticsEvents.Step].Number);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.TutorialStep));
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.TutorialComplete), "skipped, not completed");
            watch.SkipTutorial(s, mem);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.TutorialSkip), "nothing left to skip");
        }

        [Test]
        public void The_Watch_Logs_A_Station_Opened_Once_With_Its_Id()
        {
            GameState s = NewGarden();
            var mem = new MemoryAnalytics();
            var watch = new AnalyticsWatch();
            watch.Reset(s);
            s.Levels[2] = 1;
            watch.Note(Fixture.Defs(), s, mem);
            s.Levels[2] = 5;
            watch.Note(Fixture.Defs(), s, mem);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.StationUnlocked), "opened once; levels after are not openings");
            MemoryAnalytics.Event e = mem.Last(AnalyticsEvents.StationUnlocked);
            Assert.AreEqual("jam", e[AnalyticsEvents.Station].Text);
            Assert.AreEqual(2L, e[AnalyticsEvents.StationIndex].Number);
            Assert.AreEqual(0, mem.Count(AnalyticsEvents.GardenLevelUp), "the watch logs what changed, nothing it made up");
        }

        [Test]
        public void A_Purchase_Logs_The_Product_And_The_Stores_Price_Or_Why_It_Did_Not_Pay()
        {
            var mem = new MemoryAnalytics();
            mem.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Paid, Storefront.Medium, "t1"), "69.000 ₫");
            mem.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Cancelled, Storefront.Small), "25.000 ₫");
            MemoryAnalytics.Event paid = mem.Last(AnalyticsEvents.PurchaseSuccess);
            Assert.AreEqual(Storefront.Medium, paid[AnalyticsEvents.ProductId].Text);
            Assert.AreEqual("69.000 ₫", paid[AnalyticsEvents.Price].Text);
            Assert.AreEqual(0L, paid[AnalyticsEvents.Late].Number);
            Assert.IsFalse(paid.Has("transaction_id"), "no receipt leaves the device");
            MemoryAnalytics.Event failed = mem.Last(AnalyticsEvents.PurchaseFailed);
            Assert.AreEqual(Storefront.Small, failed[AnalyticsEvents.ProductId].Text);
            Assert.AreEqual("cancelled", failed[AnalyticsEvents.Reason].Text);
            Assert.IsFalse(failed.Has(AnalyticsEvents.Price));
        }
    }
}

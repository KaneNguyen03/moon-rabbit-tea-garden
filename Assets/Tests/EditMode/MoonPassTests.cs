using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class MoonPassTests
    {
        List<StationDef> defs;
        GameState s;
        const long Bought = 20400;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.MaxDaySeen = Bought;
        }

        List<Letter> PassLetters(LetterKind kind = LetterKind.MoonPassDay) => s.Letters.FindAll(l => l.Kind == kind);

        int ClaimAll(long day)
        {
            int golden = 0;
            foreach (Letter l in new List<Letter>(s.Letters)) golden += Mailbox.Claim(s, l.Id, day).Golden;
            return golden;
        }

        [Test]
        public void The_Pass_Is_A_One_Time_Store_Product_Worth_Five_Hundred_And_Fifty_Golden_Carrots()
        {
            Assert.AreEqual("vuontra.moonpass.30", MoonPass.ProductId);
            Assert.IsTrue(Storefront.Sells(MoonPass.ProductId));
            Assert.IsNull(Storefront.Find(MoonPass.ProductId), "it is no pack of golden carrots");
            Assert.AreEqual(30, MoonPass.Days);
            Assert.AreEqual(100, MoonPass.GoldenNow);
            Assert.AreEqual(15, MoonPass.GoldenPerDay);
            Assert.AreEqual(550, MoonPass.Total);
            Assert.AreEqual(3600d, MoonPass.AwayBonusSeconds);
            Assert.AreEqual("109.000 ₫", new FakeStorefront().PriceOf(MoonPass.ProductId));
        }

        [Test]
        public void Buying_Pays_A_Hundred_Once_And_Brings_The_First_Day_Letter_At_Once()
        {
            Assert.IsTrue(Storefront.Grant(s, MoonPass.ProductId, "GPA.moon.1"));
            Assert.AreEqual(MoonPass.GoldenNow, s.GoldenCarrots);
            Assert.AreEqual(0, s.PacksBought, "the pass is not a pack");
            Assert.IsTrue(MoonPass.Active(s, Bought));
            Assert.AreEqual(30, MoonPass.DaysLeft(s, Bought));
            List<Letter> letters = PassLetters();
            Assert.AreEqual(1, letters.Count);
            Assert.AreEqual(1, letters[0].PassDay);
            Assert.AreEqual(30, letters[0].PassDays);
            Assert.AreEqual(MoonPass.GoldenPerDay, letters[0].Golden);

            Assert.IsFalse(Storefront.Grant(s, MoonPass.ProductId, "GPA.moon.1"), "the store delivering it again pays nothing");
            Assert.AreEqual(MoonPass.GoldenNow, s.GoldenCarrots);
            Assert.AreEqual(1, PassLetters().Count);
            Assert.AreEqual(0, MoonPass.Deliver(s, Bought), "the same day brings no second letter");
        }

        [Test]
        public void Each_Day_Opened_Brings_Exactly_One_Letter_And_A_Missed_Day_Is_Lost()
        {
            MoonPass.Grant(s, Bought);
            Assert.AreEqual(1, MoonPass.Deliver(s, Bought + 1));
            Assert.AreEqual(0, MoonPass.Deliver(s, Bought + 1), "once a day");
            Assert.AreEqual(1, MoonPass.Deliver(s, Bought + 3), "day three was missed");
            List<Letter> letters = PassLetters();
            Assert.AreEqual(3, letters.Count, "no letter for the missed day");
            CollectionAssert.AreEqual(new[] { 1, 2, 4 }, letters.ConvertAll(l => l.PassDay));
            Assert.AreEqual(3 * MoonPass.GoldenPerDay, ClaimAll(Bought + 3));
            Assert.AreEqual(MoonPass.GoldenNow + 3 * MoonPass.GoldenPerDay, s.GoldenCarrots);
        }

        [Test]
        public void Opening_Every_Day_Pays_Thirty_Letters_A_Reminder_Three_Days_Before_And_Then_It_Ends()
        {
            MoonPass.Grant(s, Bought);
            int reminders = 0;
            for (long day = Bought; day < Bought + 40; day++)
            {
                MoonPass.Deliver(s, day);
                reminders += PassLetters(LetterKind.MoonPassRenew).Count;
                foreach (Letter r in PassLetters(LetterKind.MoonPassRenew))
                {
                    Assert.AreEqual(Bought + 27, day, "the reminder comes with three days left");
                    Assert.AreEqual(3, r.PassDay);
                    Assert.AreEqual(0, r.Golden);
                }
                ClaimAll(day);
            }
            Assert.AreEqual(1, reminders, "one reminder");
            Assert.AreEqual(MoonPass.Total, s.GoldenCarrots, "100 at once and 15 on each of the thirty days");
            Assert.IsTrue(MoonPass.Active(s, Bought + 29));
            Assert.AreEqual(1, MoonPass.DaysLeft(s, Bought + 29));
            Assert.IsFalse(MoonPass.Active(s, Bought + 30));
            Assert.AreEqual(0, MoonPass.DaysLeft(s, Bought + 30));
        }

        [Test]
        public void Buying_Again_While_It_Runs_Adds_Thirty_Days_And_After_It_Ends_Starts_Afresh()
        {
            MoonPass.Grant(s, Bought);
            MoonPass.Deliver(s, Bought + 10);
            MoonPass.Grant(s, Bought + 10);
            Assert.AreEqual(2 * MoonPass.GoldenNow, s.GoldenCarrots);
            Assert.AreEqual(50, MoonPass.DaysLeft(s, Bought + 10));
            Assert.AreEqual(2, PassLetters().Count, "day eleven was already delivered");
            Assert.AreEqual(1, MoonPass.Deliver(s, Bought + 11));
            Letter twelfth = PassLetters()[2];
            Assert.AreEqual(12, twelfth.PassDay);
            Assert.AreEqual(60, twelfth.PassDays);

            MoonPass.Deliver(s, Bought + 57);
            Assert.AreEqual(1, PassLetters(LetterKind.MoonPassRenew).Count);
            MoonPass.Grant(s, Bought + 57);
            Assert.AreEqual(0, PassLetters(LetterKind.MoonPassRenew).Count, "a stale reminder goes once more days are bought");
            Assert.AreEqual(33, MoonPass.DaysLeft(s, Bought + 57));

            MoonPass.Grant(s, Bought + 200);
            Assert.AreEqual(30, MoonPass.DaysLeft(s, Bought + 200), "bought after it ran out: thirty from today");
            Letter fresh = s.Letters.Find(l => l.Id == "moonpass-" + (Bought + 200));
            Assert.IsNotNull(fresh);
            Assert.AreEqual(1, fresh.PassDay);
            Assert.AreEqual(30, fresh.PassDays);
        }

        [Test]
        public void Turning_The_Clock_Back_Brings_No_Extra_Letter_Or_Free_Double()
        {
            MoonPass.Grant(s, Bought);
            Seasons.Clamp(s, Bought + 5);
            Assert.AreEqual(1, MoonPass.Deliver(s, Seasons.Clamp(s, Bought + 5)));
            Assert.AreEqual(0, MoonPass.Deliver(s, Seasons.Clamp(s, Bought + 3)), "the clamped day is still day five");
            Assert.AreEqual(0, MoonPass.Deliver(s, Bought + 3), "an earlier day than the last letter brings nothing even unclamped");
            Assert.AreEqual(2, PassLetters().Count);

            Assert.IsTrue(Away.Double(s, Bought + 5, BigNum.One));
            Assert.IsTrue(Away.Double(s, Bought + 5, BigNum.One));
            Assert.IsFalse(Away.DoubleIsFree(s, Bought + 4), "no free double on a day the clock went back to");
            Assert.IsFalse(Away.DoubleIsFree(s, Bought + 5));
        }

        [Test]
        public void The_Away_Cap_Grows_An_Hour_After_Long_Sleep_Only_While_The_Pass_Runs()
        {
            var ups = new List<UpgradeDef> { new UpgradeDef { Id = "giac-ngu", Effect = UpgradeEffect.AwayHours, Amount = 2, BaseCost = 5000, CostGrowth = 10, MaxLevel = 5 } };
            s.UpgradeLevels = new[] { 1 };
            Assert.AreEqual(10800 + 7200, Modifiers.From(ups, s).AwayCapSeconds, 1e-9);
            MoonPass.Grant(s, Bought);
            Assert.AreEqual(10800 + 7200 + 3600, Modifiers.From(ups, s).AwayCapSeconds, 1e-9);
            Assert.AreEqual(10800 + 3600, Modifiers.From(null, s).AwayCapSeconds, 1e-9);
            s.MaxDaySeen = Bought + MoonPass.Days;
            Assert.AreEqual(10800 + 7200, Modifiers.From(ups, s).AwayCapSeconds, 1e-9, "gone once the pass has ended");
        }

        [Test]
        public void The_Pass_Gives_One_More_Free_Double_A_Day_Only_While_It_Runs()
        {
            long day = Bought + 2;
            Assert.IsTrue(Away.Double(s, day, BigNum.One));
            Assert.IsFalse(Away.DoubleIsFree(s, day), "without the pass one a day");
            Assert.AreEqual(Away.DoubleCost, Away.DoubleCostToday(s, day));

            MoonPass.Grant(s, Bought);
            Assert.IsTrue(Away.DoubleIsFree(s, day), "the pass adds a second");
            Assert.AreEqual(0, Away.DoubleCostToday(s, day));
            int golden = s.GoldenCarrots;
            Assert.IsTrue(Away.Double(s, day, BigNum.One));
            Assert.AreEqual(golden, s.GoldenCarrots, "free");
            Assert.IsFalse(Away.DoubleIsFree(s, day), "and only one more");
            Assert.AreEqual(Away.DoubleCost, Away.DoubleCostToday(s, day));

            Assert.IsTrue(Away.Double(s, day + 1, BigNum.One));
            Assert.IsTrue(Away.Double(s, day + 1, BigNum.One));
            Assert.AreEqual(golden, s.GoldenCarrots, "two free the next day too");

            long after = Bought + MoonPass.Days;
            Assert.IsTrue(Away.Double(s, after, BigNum.One));
            Assert.IsFalse(Away.DoubleIsFree(s, after), "back to one a day once it ends");
        }

        [Test]
        public void The_Mailbox_Check_Brings_The_Day_Letter_On_A_New_Day()
        {
            MoonPass.Grant(s, Bought);
            s.MailDay = Bought;
            Mailbox.Check(s, Bought + 1);
            Assert.AreEqual(2, PassLetters().Count);
            Assert.AreEqual(2, PassLetters()[1].PassDay);
            Assert.IsTrue(Mailbox.Expired(PassLetters()[1], Bought + 1 + Mailbox.KeepDays + 1), "kept as long as any letter");
        }

        [Test]
        public void The_Save_Keeps_The_Pass_And_Its_Letters_And_An_Old_Save_Loads_Without_One()
        {
            MoonPass.Grant(s, Bought);
            MoonPass.Deliver(s, Bought + 27);
            Away.Double(s, Bought + 27, BigNum.One);
            Away.Double(s, Bought + 27, BigNum.One);
            LoadResult back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s));
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            GameState b = back.State;
            Assert.AreEqual(s.MoonPassFromDay, b.MoonPassFromDay);
            Assert.AreEqual(s.MoonPassUntilDay, b.MoonPassUntilDay);
            Assert.AreEqual(s.MoonPassMailDay, b.MoonPassMailDay);
            Assert.AreEqual(s.MoonPassRemindedUntil, b.MoonPassRemindedUntil);
            Assert.AreEqual(s.MoonPassDoubleDay, b.MoonPassDoubleDay);
            Assert.AreEqual(3, b.Letters.Count);
            Letter day28 = b.Letters.Find(l => l.Kind == LetterKind.MoonPassDay && l.PassDay == 28);
            Assert.IsNotNull(day28);
            Assert.AreEqual(30, day28.PassDays);
            Assert.IsNotNull(b.Letters.Find(l => l.Kind == LetterKind.MoonPassRenew && l.PassDay == 3));
            Assert.AreEqual(0, MoonPass.Deliver(b, Bought + 27), "no second letter after a reload");

            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[]}").State;
            Assert.AreEqual(-1, old.MoonPassUntilDay);
            Assert.IsFalse(MoonPass.Active(old, Bought));
            Assert.AreEqual(0, MoonPass.DaysLeft(old, Bought));
        }

        [Test]
        public void Saved_Letter_Kind_Numbers_Never_Move_And_The_Pass_Kinds_Come_After_Four()
        {
            Assert.AreEqual(0, (int)LetterKind.MakeUp);
            Assert.AreEqual(1, (int)LetterKind.Season);
            Assert.AreEqual(2, (int)LetterKind.WeekChest);
            Assert.AreEqual(3, (int)LetterKind.Newcomer);
            Assert.AreEqual(4, (int)LetterKind.MonthPass);
            Assert.AreEqual(5, (int)LetterKind.MoonPassDay, "4 is the month pass's (#107)");
            Assert.AreEqual(6, (int)LetterKind.MoonPassRenew);
            string json = "{\"version\":14,\"stations\":[],\"mail\":{\"day\":5,\"letters\":[{\"id\":\"a\",\"kind\":5,\"season\":0,\"day\":5,\"n\":2,\"of\":30},{\"id\":\"b\",\"kind\":6,\"season\":0,\"day\":5,\"n\":3,\"of\":30},{\"id\":\"c\",\"kind\":9,\"season\":0,\"day\":5}]}}";
            List<Letter> letters = SaveCodec.Decode(defs, json).State.Letters;
            Assert.AreEqual(2, letters.Count, "an unknown kind is dropped");
            Assert.AreEqual(LetterKind.MoonPassDay, letters[0].Kind);
            Assert.AreEqual(2, letters[0].PassDay);
            Assert.AreEqual(LetterKind.MoonPassRenew, letters[1].Kind);
        }

        [Test]
        public void Analytics_Log_The_Pass_Bought_And_Each_Day_Claimed()
        {
            var mem = new MemoryAnalytics();
            mem.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Paid, MoonPass.ProductId, "t1"), MoonPass.ReferencePrice);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.PurchaseSuccess));
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.PassBought));
            Assert.AreEqual(MoonPass.ProductId, mem.Last(AnalyticsEvents.PassBought)[AnalyticsEvents.ProductId].Text);
            mem.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Cancelled, MoonPass.ProductId), null);
            mem.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Paid, Storefront.Small, "t2"), "25.000 ₫");
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.PassBought), "only a paid pass");

            MoonPass.Grant(s, Bought);
            MoonPass.Deliver(s, Bought + 27);
            foreach (Letter l in new List<Letter>(s.Letters)) mem.LogLetterClaimed(Mailbox.Claim(s, l.Id, Bought + 27));
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.PassDayClaimed), "the reminder is no day");
            MemoryAnalytics.Event last = mem.Last(AnalyticsEvents.PassDayClaimed);
            Assert.AreEqual(28, last[AnalyticsEvents.Day].Number);
            Assert.AreEqual(30, last[AnalyticsEvents.Days].Number);
        }
    }
}

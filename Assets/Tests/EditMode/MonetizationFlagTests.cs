using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class MonetizationFlagTests
    {
        const long Bought = 20400;

        GameState Fresh()
        {
            GameState s = GameState.NewGame(Fixture.Defs());
            s.MaxDaySeen = Bought;
            return s;
        }

        [Test]
        public void Selling_Is_Off_Unless_The_Build_Says_Otherwise()
        {
            Assert.IsFalse(Features.MonetizationByDefault, "the owner asked for the flag to start off (25/11)");
        }

        [Test, MonetizationOn(false)]
        public void Off_A_Bought_Moon_Pass_Still_Mails_Its_Daily_Carrots_But_Never_A_Reminder_To_Buy_Again()
        {
            GameState s = Fresh();
            Features.Monetization = true;
            MoonPass.Grant(s, Bought);
            Features.Monetization = false;
            long late = s.MoonPassUntilDay - MoonPass.RemindAtDaysLeft;
            int delivered = 0;
            for (long day = Bought + 1; day <= late; day++) delivered += MoonPass.Deliver(s, day);
            Assert.AreEqual(late - Bought, delivered, "every paid day still comes, and nothing else");
            Assert.AreEqual(0, s.Letters.FindAll(l => l.Kind == LetterKind.MoonPassRenew).Count, "no letter asks to buy again");
            Assert.AreEqual(MoonPass.RemindAtDaysLeft, MoonPass.DaysLeft(s, late), "the pass keeps its days");
            Assert.IsTrue(MoonPass.Active(s, late), "and its away bonus");
        }

        [Test, MonetizationOn(false)]
        public void Off_A_Reminder_Already_In_The_Mailbox_Is_Taken_Out()
        {
            GameState s = Fresh();
            Features.Monetization = true;
            MoonPass.Grant(s, Bought);
            long late = s.MoonPassUntilDay - MoonPass.RemindAtDaysLeft;
            MoonPass.Deliver(s, late);
            Assert.AreEqual(1, s.Letters.FindAll(l => l.Kind == LetterKind.MoonPassRenew).Count);
            Features.Monetization = false;
            MoonPass.Deliver(s, late + 1);
            Assert.AreEqual(0, s.Letters.FindAll(l => l.Kind == LetterKind.MoonPassRenew).Count);
            Assert.IsNotNull(s.Letters.Find(l => l.Kind == LetterKind.MoonPassDay && l.Day == late + 1), "the paid day still comes");
        }

        [Test, MonetizationOn(false)]
        public void Off_A_Month_Pass_Bought_Before_Keeps_Its_Lower_Row()
        {
            GameState s = Fresh();
            long day = GiftCodes.Day(2026, 11, 24);
            s.PassMonth = MonthPass.MonthOf(day);
            Assert.IsTrue(MonthPass.Buy(s, day));
            s.PassPoints = 3 * MonthPass.PointsPerTier;
            Assert.IsTrue(MonthPass.Premium(s, day));
            Assert.IsTrue(MonthPass.Ready(s, MonthRow.Premium, 3, day), "what was paid for stays claimable");
        }

        static void LogEveryMonetizedEvent(IAnalytics a)
        {
            a.LogGoldenPacksOpened(AnalyticsEvents.FromShop);
            a.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Paid, MoonPass.ProductId, "t1"), "109.000 ₫");
            a.LogPurchase(PurchaseResult.Of(PurchaseOutcome.Cancelled, Storefront.Small), "25.000 ₫");
            a.LogRewardedAd(Ads.ShopGolden, null, AnalyticsEvents.AdWatched);
            a.LogOffer(AnalyticsEvents.OfferShown, Offers.Find(Offers.Starter));
            a.LogOffer(AnalyticsEvents.OfferBought, Offers.Find(Offers.Starter));
            a.LogOffer(AnalyticsEvents.OfferExpired, Offers.Find(Offers.Starter));
            a.LogMonthPassBought(11, 3);
            a.LogLetterClaimed(new Letter { Kind = LetterKind.MoonPassDay, PassDay = 2, PassDays = 30 });
        }

        [Test, MonetizationOn(false)]
        public void Off_No_Purchase_Ad_Offer_Or_Pass_Event_Is_Sent_And_Everything_Else_Is()
        {
            var mem = new MemoryAnalytics();
            LogEveryMonetizedEvent(mem);
            Assert.AreEqual(0, mem.Events.Count, string.Join(", ", mem.Events));
            mem.LogGameOpen(false);
            mem.LogMonthPassTier(3, false, 11);
            mem.LogIntroShown(Intros.Shop);
            Assert.AreEqual(3, mem.Events.Count, "play events still go");
        }

        [Test, MonetizationOn]
        public void On_Every_Monetized_Event_Is_Sent_As_Before()
        {
            var mem = new MemoryAnalytics();
            LogEveryMonetizedEvent(mem);
            var names = new HashSet<string>();
            foreach (MemoryAnalytics.Event e in mem.Events) names.Add(e.Name);
            CollectionAssert.AreEquivalent(AnalyticsEvents.Monetization, names);
        }

        [Test]
        public void Every_Monetized_Event_Is_A_Known_Event()
        {
            foreach (string name in AnalyticsEvents.Monetization) CollectionAssert.Contains(AnalyticsEvents.All, name);
        }
    }
}

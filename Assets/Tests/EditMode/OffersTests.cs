using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class OffersTests
    {
        const long Hour = 3600L;
        const long Start = 20_800L * Offers.DaySeconds + 9 * Hour;

        List<StationDef> defs;
        List<ItemDef> items;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            items = new List<ItemDef>
            {
                new ItemDef { Id = Offers.StarterHat, Name = "Nơ trăng", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 40, Tier = 2 },
            };
            s = GameState.NewGame(defs);
            s.MaxDaySeen = s.FirstDaySeen = 20_800L;
        }

        void ReachLevel(int level) => s.GardenXp = Garden.XpForLevel(level);

        OfferNews Tick(long clock) => Offers.Tick(s, clock);

        bool Grant(string id, string transaction) => Storefront.Grant(s, id, transaction, items, "Bé Mới", 0d, out _);

        [Test]
        public void The_Starter_Pack_Opens_At_Garden_Level_Four_For_Seventy_Two_Hours()
        {
            ReachLevel(Offers.StarterLevel - 1);
            Assert.AreEqual(0, Tick(Start).Started.Count);
            Assert.AreEqual(OfferState.Unseen, Offers.StateOf(s, Offers.Starter, Start));

            ReachLevel(Offers.StarterLevel);
            OfferNews news = Tick(Start + 10);
            CollectionAssert.AreEqual(new[] { Offers.Starter }, news.Started);
            Assert.AreEqual(OfferState.Active, Offers.StateOf(s, Offers.Starter, Start + 10));
            Assert.AreEqual(72 * Hour, Offers.SecondsLeft(s, Offers.Starter, Start + 10));
            Assert.AreEqual(Hour, Offers.SecondsLeft(s, Offers.Starter, Start + 10 + 71 * Hour));

            news = Tick(Start + 10 + 72 * Hour);
            CollectionAssert.AreEqual(new[] { Offers.Starter }, news.Expired);
            Assert.AreEqual(OfferState.Expired, Offers.StateOf(s, Offers.Starter, Start + 10 + 72 * Hour));
            Assert.AreEqual(0, Tick(Start + 80 * Hour).Expired.Count, "an end is told once");
            Assert.AreEqual(0, Offers.Active(s, Start + 80 * Hour).Count);
        }

        [Test]
        public void Milestone_Packs_Open_At_The_First_Reach_Of_Ten_Twenty_And_Thirty_For_Forty_Eight_Hours()
        {
            ReachLevel(9);
            Tick(Start);
            ReachLevel(10);
            OfferNews news = Tick(Start + Hour);
            CollectionAssert.AreEqual(new[] { Offers.Milestone10 }, news.Started);
            Assert.AreEqual(48 * Hour, Offers.SecondsLeft(s, Offers.Milestone10, Start + Hour));

            ReachLevel(20);
            CollectionAssert.AreEqual(new[] { Offers.Milestone20 }, Tick(Start + 2 * Hour).Started);
            ReachLevel(30);
            CollectionAssert.AreEqual(new[] { Offers.Milestone30 }, Tick(Start + 3 * Hour).Started);

            s.GardenXp = 0d;
            ReachLevel(30);
            Assert.AreEqual(0, Tick(Start + 100 * Hour).Started.Count, "a new moon's second climb opens nothing again");
            foreach (OfferDef def in Offers.All) Assert.AreEqual(1, s.OfferRecords.FindAll(r => r.Id == def.Id).Count);
        }

        [Test]
        public void A_Garden_Already_Past_Several_Milestones_Gets_Only_The_Highest_And_The_Starter()
        {
            ReachLevel(25);
            OfferNews news = Tick(Start);
            CollectionAssert.AreEquivalent(new[] { Offers.Starter, Offers.Milestone20 }, news.Started);
            Assert.AreEqual(OfferState.Expired, Offers.StateOf(s, Offers.Milestone10, Start), "passed before it existed: never offered");
            Assert.AreEqual(0, news.Expired.Count, "nothing was shown, so nothing ends");
            List<OfferDef> active = Offers.Active(s, Start);
            Assert.AreEqual(Offers.Milestone20, active[0].Id, "the one ending first leads");
            Assert.AreEqual(Offers.Milestone20, Offers.Leading(s, Start).Id);
            Assert.IsNull(Offers.Leading(s, Start + 80 * 3600L));
        }

        [Test]
        public void Turning_The_Clock_Back_Never_Reopens_Or_Lengthens_An_Offer()
        {
            ReachLevel(Offers.StarterLevel);
            long t0 = Offers.Clock(s, Start);
            Tick(t0);
            long late = Offers.Clock(s, Start + 73 * Hour);
            Tick(late);
            Assert.AreEqual(OfferState.Expired, Offers.StateOf(s, Offers.Starter, late));

            long back = Offers.Clock(s, Start);
            Assert.AreEqual(late, back, "the clock never runs backwards");
            Assert.AreEqual(OfferState.Expired, Offers.StateOf(s, Offers.Starter, back));
            Assert.IsFalse(Offers.CanBuy(s, Offers.Starter, back));
        }

        [Test]
        public void Playing_On_A_Clock_Set_Back_Still_Counts_Down_By_The_Seconds_Played()
        {
            ReachLevel(Offers.StarterLevel);
            Tick(Offers.Clock(s, Start));
            s.PlaySeconds += 2 * Hour;
            long now = Offers.Clock(s, Start - 5 * Hour);
            Assert.AreEqual(Start + 2 * Hour, now);
            Assert.AreEqual(70 * Hour, Offers.SecondsLeft(s, Offers.Starter, now));
        }

        [Test]
        public void The_Clock_Never_Stands_Before_The_Latest_Day_The_Garden_Has_Seen()
        {
            s.MaxDaySeen = 20_900L;
            Assert.AreEqual(20_900L * Offers.DaySeconds, Offers.Clock(s, Start));
        }

        [Test]
        public void The_Starter_Pack_Delivers_Its_Contents_Exactly_Once_Even_When_Restored()
        {
            ReachLevel(Offers.StarterLevel);
            Tick(Start);
            int jars = s.MoonJars;
            Assert.IsTrue(Grant(Offers.Starter, "GPA.s1"));
            Assert.AreEqual(80, s.GoldenCarrots);
            Assert.AreEqual(jars + 3, s.MoonJars);
            Assert.AreEqual(3, s.Clovers);
            Assert.IsTrue(Items.Owns(s, Offers.StarterHat));
            Assert.AreEqual(1, s.PacksBought);
            Assert.AreEqual(OfferState.Bought, Offers.StateOf(s, Offers.Starter, Start));
            Assert.IsFalse(Offers.CanBuy(s, Offers.Starter, Start), "only once");

            Assert.IsFalse(Grant(Offers.Starter, "GPA.s1"), "the same receipt again");
            Assert.IsFalse(Grant(Offers.Starter, "GPA.restored"), "a restore hands the same pack back under another id");
            Assert.AreEqual(80, s.GoldenCarrots);
            Assert.AreEqual(1, s.PacksBought);
            Assert.AreEqual(1, s.Items.FindAll(i => i.DefId == Offers.StarterHat).Count);

            LoadResult back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s));
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.IsFalse(Storefront.Grant(back.State, Offers.Starter, "GPA.after-reload", items, "x", 0d, out _), "still once after a reload");
        }

        [Test]
        public void A_Milestone_Pack_Brings_A_Purple_Rabbit_Of_A_Kind_Not_Yet_Met()
        {
            ReachLevel(10);
            Tick(Start);
            var tier4 = new List<string>();
            foreach (RabbitKind k in RabbitKinds.OfTier(Offers.MilestoneRabbitTier)) tier4.Add(k.Id);
            for (int i = 0; i < tier4.Count - 1; i++) s.SeenKinds.Add(tier4[i]);

            Assert.IsTrue(Storefront.Grant(s, Offers.Milestone10, "GPA.m10", items, "Bé Tím", 0d, out OfferGift gift));
            Assert.AreEqual(150, gift.Golden);
            Assert.AreEqual(150, s.GoldenCarrots);
            Assert.AreEqual(5, gift.Jars);
            Assert.IsNotNull(gift.Rabbit);
            Assert.AreEqual(Offers.MilestoneRabbitTier, gift.Rabbit.Tier);
            Assert.AreEqual(tier4[tier4.Count - 1], gift.Rabbit.Kind, "the only purple kind not met yet");
            Assert.IsTrue(gift.Rabbit.FirstOfKind);
            Assert.AreEqual(1, s.PacksBought);
        }

        [Test]
        public void A_Milestone_Rabbit_Still_Comes_When_Every_Purple_Kind_Is_Met()
        {
            ReachLevel(10);
            Tick(Start);
            foreach (RabbitKind k in RabbitKinds.OfTier(Offers.MilestoneRabbitTier)) s.SeenKinds.Add(k.Id);
            Assert.IsTrue(Storefront.Grant(s, Offers.Milestone10, "GPA.m10", items, "Bé Tím", 0.99d, out OfferGift gift));
            Assert.AreEqual(Offers.MilestoneRabbitTier, gift.Rabbit.Tier);
        }

        [Test]
        public void Jars_That_Do_Not_Fit_The_Shelf_Come_As_Golden_Carrots_At_The_Shop_Price()
        {
            ReachLevel(10);
            Tick(Start);
            s.MoonJars = MoonJars.Cap - 2;
            Assert.IsTrue(Storefront.Grant(s, Offers.Milestone10, "GPA.m10", items, "x", 0d, out OfferGift gift));
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            Assert.AreEqual(2, gift.Jars);
            Assert.AreEqual(150 + 3 * MoonJars.Price, s.GoldenCarrots);
        }

        [Test]
        public void An_Owned_Moon_Bow_Comes_As_Its_Shop_Price_In_Golden_Carrots()
        {
            ReachLevel(Offers.StarterLevel);
            Tick(Start);
            Items.Grant(s, items[0], 2);
            Assert.IsTrue(Grant(Offers.Starter, "GPA.s1"));
            Assert.AreEqual(80 + 40, s.GoldenCarrots);
            Assert.AreEqual(1, s.Items.FindAll(i => i.DefId == Offers.StarterHat).Count);
        }

        [Test]
        public void An_Expired_Pack_Cannot_Be_Bought_But_A_Payment_Confirmed_After_The_End_Still_Delivers()
        {
            ReachLevel(Offers.StarterLevel);
            Tick(Start);
            long after = Start + 72 * Hour;
            Tick(after);
            Assert.IsFalse(Offers.CanBuy(s, Offers.Starter, after));
            Assert.IsTrue(Grant(Offers.Starter, "GPA.pending-then-paid"), "the player paid: the pack comes");
            Assert.AreEqual(80, s.GoldenCarrots);
            Assert.AreEqual(1, s.PacksBought);
            Assert.AreEqual(OfferState.Bought, Offers.StateOf(s, Offers.Starter, after));
        }

        [Test]
        public void A_Pack_Paid_On_Another_Device_Before_It_Opened_Here_Still_Delivers_Once()
        {
            Assert.IsTrue(Grant(Offers.Milestone20, "GPA.elsewhere"));
            Assert.AreEqual(150, s.GoldenCarrots);
            ReachLevel(20);
            Assert.AreEqual(0, Tick(Start).Started.FindAll(id => id == Offers.Milestone20).Count, "a bought pack never opens again");
            Assert.AreEqual(OfferState.Bought, Offers.StateOf(s, Offers.Milestone20, Start));
        }

        [Test]
        public void The_Four_Packs_Are_Sold_Through_The_Store_With_The_Issues_Contents()
        {
            Assert.AreEqual(4, Offers.All.Length);
            OfferDef starter = Offers.Find(Offers.Starter);
            Assert.AreEqual("vuontra.starter", starter.Id);
            Assert.AreEqual(4, starter.Level);
            Assert.AreEqual(72 * Hour, starter.Seconds);
            Assert.AreEqual(80, starter.Golden);
            Assert.AreEqual(3, starter.Jars);
            Assert.AreEqual(3, starter.Clovers);
            Assert.AreEqual("hat-no-trang", starter.Hat);
            Assert.AreEqual(-1, starter.RabbitTier);
            int[] levels = { 10, 20, 30 };
            for (int i = 0; i < levels.Length; i++)
            {
                OfferDef m = Offers.Find("vuontra.milestone." + levels[i]);
                Assert.IsNotNull(m);
                Assert.AreEqual(levels[i], m.Level);
                Assert.AreEqual(48 * Hour, m.Seconds);
                Assert.AreEqual(150, m.Golden);
                Assert.AreEqual(5, m.Jars);
                Assert.AreEqual(3, m.RabbitTier, "bậc Tím, four stars");
                Assert.IsNull(m.Hat);
            }
            foreach (OfferDef def in Offers.All) Assert.IsTrue(Storefront.Sells(def.Id));
            Assert.AreEqual(3, Storefront.Packs.Length, "the three golden packs stay as they were");

            var store = new FakeStorefront();
            Assert.AreEqual(store.PriceOf(Storefront.Small), store.PriceOf(Offers.Starter));
            Assert.AreEqual(store.PriceOf(Storefront.Medium), store.PriceOf(Offers.Milestone10));
            Assert.AreEqual(store.PriceOf(Storefront.Medium), store.PriceOf(Offers.Milestone30));
        }

        [Test]
        public void The_Fake_Store_Delivers_Nothing_On_Cancel_Network_Error_Or_Pending()
        {
            ReachLevel(Offers.StarterLevel);
            Tick(Start);
            var store = new FakeStorefront();
            foreach (PurchaseOutcome outcome in new[] { PurchaseOutcome.Cancelled, PurchaseOutcome.Failed, PurchaseOutcome.Pending })
            {
                store.NextOutcome = outcome;
                PurchaseResult got = default;
                store.Buy(Offers.Starter, r => got = r);
                Assert.AreEqual(outcome, got.Outcome);
                Assert.IsNull(got.TransactionId);
            }
            Assert.AreEqual(0, store.Bought.Count);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(0, s.PacksBought);
            Assert.AreEqual(OfferState.Active, Offers.StateOf(s, Offers.Starter, Start));
            Assert.AreEqual("pending", AnalyticsEvents.ReasonOf(PurchaseOutcome.Pending));
        }

        [Test]
        public void The_Offer_Card_Is_Shown_Once_Per_Offer()
        {
            ReachLevel(10);
            Tick(Start);
            OfferDef first = Offers.ToShow(s, Start);
            Assert.IsNotNull(first);
            Offers.MarkShown(s, first.Id);
            OfferDef second = Offers.ToShow(s, Start);
            Assert.IsNotNull(second);
            Assert.AreNotEqual(first.Id, second.Id);
            Offers.MarkShown(s, second.Id);
            Assert.IsNull(Offers.ToShow(s, Start));
        }

        [Test]
        public void Offers_Survive_A_Save_And_An_Older_Save_Loads_Without_Them()
        {
            ReachLevel(10);
            s.PlaySeconds = 500d;
            long clock = Offers.Clock(s, Start);
            Tick(clock);
            Offers.MarkShown(s, Offers.Starter);
            Grant(Offers.Milestone10, "GPA.m10");

            LoadResult back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s));
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            GameState b = back.State;
            Assert.AreEqual(s.OfferClock, b.OfferClock);
            Assert.AreEqual(500d, b.OfferPlayMark);
            Assert.AreEqual(2, b.OfferRecords.Count);
            Assert.AreEqual(OfferState.Active, Offers.StateOf(b, Offers.Starter, clock));
            Assert.AreEqual(72 * Hour, Offers.SecondsLeft(b, Offers.Starter, clock));
            Assert.IsNull(Offers.ToShow(b, clock));
            Assert.AreEqual(OfferState.Bought, Offers.StateOf(b, Offers.Milestone10, clock));

            string old = SaveCodec.Encode(defs, GameState.NewGame(defs));
            int at = old.IndexOf(",\"offers\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0);
            int end = old.IndexOf("]}", at, System.StringComparison.Ordinal) + 2;
            string withoutOffers = old.Remove(at, end - at);
            LoadResult older = SaveCodec.Decode(defs, withoutOffers);
            Assert.AreEqual(LoadStatus.Ok, older.Status);
            Assert.AreEqual(0, older.State.OfferRecords.Count);
            Assert.AreEqual(-1L, older.State.OfferClock);
        }
    }
}

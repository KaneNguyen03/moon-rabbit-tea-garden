using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class StorefrontTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        [Test]
        public void Three_Packs_Of_Golden_Carrots_Are_For_Sale_And_The_Middle_One_Is_The_Best_Deal()
        {
            Assert.AreEqual(3, Storefront.Packs.Length);
            Assert.AreEqual(50, Storefront.Find(Storefront.Small).Golden);
            Assert.AreEqual(150, Storefront.Find(Storefront.Medium).Golden);
            Assert.AreEqual(500, Storefront.Find(Storefront.Large).Golden);
            Assert.IsTrue(Storefront.Find(Storefront.Medium).Best);
            Assert.IsNull(Storefront.Find("vuontra.carrots.1000"), "nothing but golden carrots is for sale");
        }

        [Test]
        public void A_Paid_Pack_Goes_Into_The_Purse_Once_Per_Transaction()
        {
            Assert.IsTrue(Storefront.Grant(s, Storefront.Small, "GPA.1"));
            Assert.AreEqual(50, s.GoldenCarrots);
            Assert.AreEqual(1, s.PacksBought);
            Assert.IsFalse(Storefront.Grant(s, Storefront.Small, "GPA.1"), "the store delivering it again pays nothing");
            Assert.AreEqual(50, s.GoldenCarrots);
            Assert.IsTrue(Storefront.Grant(s, Storefront.Large, "GPA.2"));
            Assert.AreEqual(550, s.GoldenCarrots);
            Assert.IsFalse(Storefront.Grant(s, "nope", "GPA.3"));
            Assert.AreEqual(550, s.GoldenCarrots);
        }

        [Test]
        public void The_Save_Remembers_The_Last_Sixty_Four_Transactions()
        {
            for (int i = 0; i < Storefront.ReceiptMemory + 10; i++) Storefront.Grant(s, Storefront.Small, "tx" + i);
            Assert.AreEqual(Storefront.ReceiptMemory, s.PaidTransactions.Count);
            Assert.IsFalse(s.PaidTransactions.Contains("tx0"), "the oldest is forgotten");
            Assert.IsTrue(s.PaidTransactions.Contains("tx" + (Storefront.ReceiptMemory + 9)));

            string json = SaveCodec.Encode(defs, s);
            LoadResult back = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(Storefront.ReceiptMemory, back.State.PaidTransactions.Count);
            Assert.AreEqual(Storefront.ReceiptMemory + 10, back.State.PacksBought);
            Assert.IsFalse(Storefront.Grant(back.State, Storefront.Small, "tx" + (Storefront.ReceiptMemory + 9)), "still not paid twice after a reload");
        }

        [Test]
        public void The_Fake_Store_Pays_At_Once_And_Can_Be_Told_To_Cancel_Or_Fail()
        {
            var store = new FakeStorefront();
            Assert.IsTrue(store.Ready);
            Assert.IsNotNull(store.PriceOf(Storefront.Medium));
            PurchaseResult got = default;
            store.Buy(Storefront.Medium, r => got = r);
            Assert.AreEqual(PurchaseOutcome.Paid, got.Outcome);
            Assert.AreEqual(Storefront.Medium, got.PackId);
            Assert.IsFalse(string.IsNullOrEmpty(got.TransactionId));
            store.Buy(Storefront.Medium, r => Assert.AreNotEqual(got.TransactionId, r.TransactionId, "every purchase has its own id"));

            store.NextOutcome = PurchaseOutcome.Cancelled;
            store.Buy(Storefront.Small, r => got = r);
            Assert.AreEqual(PurchaseOutcome.Cancelled, got.Outcome);
            store.Buy(Storefront.Small, r => got = r);
            Assert.AreEqual(PurchaseOutcome.Paid, got.Outcome, "only the next one");

            store.Buy("nope", r => got = r);
            Assert.AreEqual(PurchaseOutcome.Unavailable, got.Outcome);
            store.Ready = false;
            Assert.IsNull(store.PriceOf(Storefront.Small));
            store.Buy(Storefront.Small, r => got = r);
            Assert.AreEqual(PurchaseOutcome.Unavailable, got.Outcome);

            bool restored = false;
            store.Ready = true;
            store.Restore(ok => restored = ok);
            Assert.IsTrue(restored);
            Assert.AreEqual(1, store.Restores);

            PurchaseResult delivered = default;
            store.Delivered += r => delivered = r;
            store.Deliver(Storefront.Large, "GPA.late");
            Assert.AreEqual(PurchaseOutcome.Paid, delivered.Outcome);
            Assert.AreEqual("GPA.late", delivered.TransactionId);
        }
    }
}

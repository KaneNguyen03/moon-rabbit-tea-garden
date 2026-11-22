using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// The rewarded-ad seams (#66): three places, each with its own daily cap by the clamped day, the fake and the
    /// absent provider, and the no-ads pass that turns every watch into an instant claim with the same caps.
    /// </summary>
    public class AdsTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
        }

        /// <summary>A garden far enough along for all three games, with every play of the day spent.</summary>
        void AllGamesPlayedOut(long day)
        {
            s.GardenXp = 1e9; // every game unlocked
            while (Pounding.Start(s, day)) { }
            while (Leaves.Start(s, day)) { }
            while (Fireflies.Start(s, day)) { }
        }

        [Test]
        public void Exactly_Three_Places_With_Their_Daily_Caps()
        {
            Assert.AreEqual(1, Ads.Cap(Ads.AwayDouble));
            Assert.AreEqual(1, Ads.Cap(Ads.ExtraRound), "one extra round per game per day");
            Assert.AreEqual(2, Ads.Cap(Ads.ShopGolden));
            Assert.AreEqual(3, Ads.Placements.Length);
            Assert.AreEqual(0, Ads.Cap("rabbit_call"), "no ads at the rabbit call");
        }

        [Test]
        public void The_Shop_Gives_One_Golden_Carrot_Twice_A_Day_And_Again_Tomorrow()
        {
            Assert.AreEqual(2, Ads.Left(s, 10, Ads.ShopGolden));
            Assert.IsTrue(Ads.ClaimShopGolden(s, 10));
            Assert.IsTrue(Ads.ClaimShopGolden(s, 10));
            Assert.IsFalse(Ads.ClaimShopGolden(s, 10), "two a day");
            Assert.AreEqual(2, s.GoldenCarrots);
            Assert.AreEqual(0, Ads.Left(s, 10, Ads.ShopGolden));
            Assert.AreEqual(2, Ads.Left(s, 11, Ads.ShopGolden), "a new day starts the count over");
            Assert.IsTrue(Ads.ClaimShopGolden(s, 11));
            Assert.AreEqual(3, s.GoldenCarrots);
        }

        [Test]
        public void A_Clock_Turned_Back_Brings_No_Extra_Claims()
        {
            Assert.IsTrue(Ads.ClaimShopGolden(s, 10));
            Assert.IsTrue(Ads.ClaimShopGolden(s, 10));
            // The game goes by the clamped day: a day before the stored one counts as the stored one.
            long back = Seasons.Clamp(s, 9);
            Assert.AreEqual(Ads.Left(s, 10, Ads.ShopGolden), Ads.Left(s, 9, Ads.ShopGolden), "an earlier day is no fresh day");
            Assert.IsFalse(Ads.ClaimShopGolden(s, 9));
            Assert.IsFalse(Ads.ClaimShopGolden(s, back));
            Assert.AreEqual(2, s.GoldenCarrots);
        }

        [Test]
        public void The_Away_Doubling_By_Ad_Comes_Only_Once_The_Free_One_Is_Spent_And_Once_A_Day()
        {
            var paid = new BigNum(1000);
            Assert.IsFalse(Ads.Eligible(s, 20, Ads.AwayDouble), "the free doubling comes first");
            Assert.IsFalse(Ads.ClaimAwayDouble(s, 20, paid));
            Assert.IsTrue(Away.Double(s, 20, paid), "the free one");
            BigNum before = s.Coins;
            Assert.IsTrue(Ads.Eligible(s, 20, Ads.AwayDouble));
            Assert.IsTrue(Ads.ClaimAwayDouble(s, 20, paid));
            Assert.AreEqual((before + paid).ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.IsFalse(Ads.ClaimAwayDouble(s, 20, paid), "once a day");
            Assert.IsFalse(Ads.ClaimAwayDouble(s, 21, paid), "a new day brings the free one back first");
            Assert.IsFalse(Ads.ClaimAwayDouble(s, 20, BigNum.Zero), "nothing to double");
            Assert.AreEqual(0, s.GoldenCarrots, "the paid doubling's golden carrots are untouched");
        }

        [Test]
        public void Each_Game_Gets_One_Extra_Round_A_Day_Once_Its_Rounds_Are_Spent()
        {
            s.GardenXp = 1e9;
            Assert.IsFalse(Ads.Eligible(s, 5, Ads.ExtraRound, Ads.GamePounding), "plays are left");
            AllGamesPlayedOut(5);
            foreach (string game in Ads.Games)
            {
                Assert.IsTrue(Ads.Eligible(s, 5, Ads.ExtraRound, game), game);
                Assert.IsTrue(Ads.ClaimExtraRound(s, 5, game), game);
                Assert.IsFalse(Ads.Eligible(s, 5, Ads.ExtraRound, game), game + ": a round is waiting now");
            }
            Assert.AreEqual(1, Pounding.PlaysLeft(s, 5));
            Assert.AreEqual(1, Leaves.PlaysLeft(s, 5));
            Assert.AreEqual(1, Fireflies.PlaysLeft(s, 5));
            Assert.IsTrue(Pounding.Start(s, 5));
            Assert.IsTrue(Leaves.Start(s, 5));
            Assert.IsTrue(Fireflies.Start(s, 5));
            foreach (string game in Ads.Games)
            {
                Assert.AreEqual(0, Ads.Left(s, 5, Ads.ExtraRound, game), game);
                Assert.IsFalse(Ads.ClaimExtraRound(s, 5, game), game + ": one a day");
            }
            Assert.AreEqual(Pounding.PlaysPerDay, Pounding.PlaysLeft(s, 6), "tomorrow is a normal day");
            Assert.AreEqual(1, Ads.Left(s, 6, Ads.ExtraRound, Ads.GameLeaves));
        }

        [Test]
        public void A_Locked_Game_Has_No_Extra_Round()
        {
            Assert.IsFalse(Pounding.Unlocked(s));
            Assert.IsFalse(Ads.ClaimExtraRound(s, 5, Ads.GamePounding));
            Assert.IsFalse(Ads.ClaimExtraRound(s, 5, "chess"), "an unknown game");
        }

        [Test]
        public void The_Fake_Provider_Is_Always_Ready_And_Answers_After_A_Second()
        {
            var fake = new FakeRewardedAds();
            Assert.IsTrue(fake.Ready(Ads.ShopGolden));
            bool? result = null;
            fake.Show(Ads.ShopGolden, ok => result = ok);
            Assert.IsNull(result, "not at once");
            Assert.IsTrue(fake.Playing);
            fake.Advance(0.5d);
            Assert.IsNull(result);
            fake.Advance(0.6d);
            Assert.AreEqual(true, result);
            Assert.IsFalse(fake.Playing);
            CollectionAssert.AreEqual(new[] { Ads.ShopGolden }, fake.Shown);

            fake.NextResult = false; // closed before the end
            fake.Show(Ads.ShopGolden, ok => result = ok);
            fake.Finish();
            Assert.AreEqual(false, result);
        }

        [Test]
        public void Without_A_Provider_Nothing_Is_Ever_Offered()
        {
            var none = new NoAds();
            Assert.IsFalse(none.Ready(Ads.ShopGolden));
            bool? result = null;
            none.Show(Ads.ShopGolden, ok => result = ok);
            Assert.AreEqual(false, result);
            Assert.AreEqual(AdOffer.None, Ads.Offer(s, 3, none, Ads.ShopGolden));
            Assert.AreEqual(AdOffer.Watch, Ads.Offer(s, 3, new FakeRewardedAds(), Ads.ShopGolden));
            Assert.AreEqual(AdOffer.None, Ads.Offer(s, 3, new FakeRewardedAds { IsReady = false }, Ads.ShopGolden), "shown only when ready");
        }

        [Test]
        public void The_Pass_Turns_Every_Watch_Into_An_Instant_Claim_With_The_Same_Caps()
        {
            Assert.IsFalse(Ads.HasPass(s, 100));
            Ads.GrantPass(s, 100);
            Assert.IsTrue(Ads.HasPass(s, 100));
            Assert.AreEqual(Ads.PassDays, Ads.PassDaysLeft(s, 100));
            Assert.AreEqual(AdOffer.Instant, Ads.Offer(s, 100, new FakeRewardedAds(), Ads.ShopGolden));
            Assert.AreEqual(AdOffer.Instant, Ads.Offer(s, 100, new NoAds(), Ads.ShopGolden), "a pass works whatever the provider");
            Assert.IsTrue(Ads.ClaimShopGolden(s, 100));
            Assert.IsTrue(Ads.ClaimShopGolden(s, 100));
            Assert.AreEqual(AdOffer.None, Ads.Offer(s, 100, new FakeRewardedAds(), Ads.ShopGolden), "the same two a day");
            Assert.AreEqual(AdOffer.None, Ads.Offer(s, 100, new FakeRewardedAds(), Ads.AwayDouble), "the free doubling still comes first");
        }

        [Test]
        public void The_Pass_Lasts_Thirty_Days_By_The_Clamped_Day_And_A_Second_One_Adds_On()
        {
            Ads.GrantPass(s, 100);
            Assert.IsTrue(Ads.HasPass(s, 129));
            Assert.IsFalse(Ads.HasPass(s, 130), "thirty days: 100 to 129");
            Assert.AreEqual(1, Ads.PassDaysLeft(s, 129));
            Ads.GrantPass(s, 110);
            Assert.AreEqual(160, s.NoAdsUntilDay, "bought again while running: thirty more after the first ends");
            Ads.GrantPass(s, 200);
            Assert.AreEqual(230, s.NoAdsUntilDay, "bought after it ran out: thirty from today");
        }

        [Test]
        public void The_Pass_Is_Sold_Through_The_Store_And_Granted_Once_Per_Transaction()
        {
            var store = new FakeStorefront();
            Assert.IsNotNull(store.PriceOf(Storefront.NoAdsPass), "the store words its price");
            Assert.IsNull(Storefront.Find(Storefront.NoAdsPass), "it is no pack of golden carrots");
            Assert.IsTrue(Storefront.Sells(Storefront.NoAdsPass));
            PurchaseResult bought = default;
            store.Buy(Storefront.NoAdsPass, r => bought = r);
            Assert.AreEqual(PurchaseOutcome.Paid, bought.Outcome);

            Seasons.Clamp(s, 40);
            Assert.IsTrue(Storefront.Grant(s, bought.PackId, bought.TransactionId));
            Assert.AreEqual(40 + Ads.PassDays, s.NoAdsUntilDay);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(0, s.PacksBought, "a pass is not a golden pack");
            Assert.IsFalse(Storefront.Grant(s, bought.PackId, bought.TransactionId), "delivered again: no second month");
            Assert.AreEqual(40 + Ads.PassDays, s.NoAdsUntilDay);
        }

        [Test]
        public void The_Day_Counts_And_The_Pass_Survive_The_Save()
        {
            Ads.ClaimShopGolden(s, 7);
            s.GardenXp = 1e9;
            AllGamesPlayedOut(7);
            Ads.ClaimExtraRound(s, 7, Ads.GameLeaves);
            Ads.GrantPass(s, 7);
            LoadResult back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s));
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(SaveCodec.CurrentVersion, 14, "the save stays version 14: the block is additive");
            Assert.AreEqual(1, Ads.Left(back.State, 7, Ads.ShopGolden));
            Assert.AreEqual(0, Ads.Left(back.State, 7, Ads.ExtraRound, Ads.GameLeaves));
            Assert.AreEqual(1, Ads.Left(back.State, 7, Ads.ExtraRound, Ads.GamePounding));
            Assert.AreEqual(7 + Ads.PassDays, back.State.NoAdsUntilDay);
        }

        [Test]
        public void An_Old_Save_Has_No_Pass_And_Every_Claim_Of_The_Day()
        {
            string json = SaveCodec.Encode(defs, s);
            int at = json.IndexOf(",\"ads\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0, "the save writes the ads block");
            int end = json.IndexOf('}', json.IndexOf('}', at) + 1); // the block holds one nested object
            string old = json.Remove(at, end + 1 - at);
            StringAssert.DoesNotContain("\"ads\"", old);
            LoadResult back = SaveCodec.Decode(defs, old);
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(-1, back.State.NoAdsUntilDay);
            Assert.IsFalse(Ads.HasPass(back.State, 0));
            Assert.AreEqual(2, Ads.Left(back.State, 0, Ads.ShopGolden));
        }

        [Test]
        public void A_Save_That_Claims_Too_Much_Is_Kept_To_The_Caps()
        {
            string json = SaveCodec.Encode(defs, s);
            int at = json.IndexOf(",\"ads\":", System.StringComparison.Ordinal);
            int end = json.IndexOf('}', json.IndexOf('}', at) + 1);
            string bent = json.Remove(at, end + 1 - at).Insert(at, ",\"ads\":{\"day\":9,\"used\":{\"shop_golden\":-5,\"extra_round.leaves\":40,\"nope\":1},\"passUntil\":-20}");
            LoadResult back = SaveCodec.Decode(defs, bent);
            Assert.AreEqual(LoadStatus.Ok, back.Status);
            Assert.AreEqual(2, Ads.Left(back.State, 9, Ads.ShopGolden), "a negative count is none");
            Assert.AreEqual(0, Ads.Left(back.State, 9, Ads.ExtraRound, Ads.GameLeaves), "never more than the cap");
            Assert.AreEqual(-1, back.State.NoAdsUntilDay);
        }
    }
}

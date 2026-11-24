using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class MonthPassTests
    {
        List<StationDef> defs;
        List<QuestDef> quests;
        GameState s;

        static readonly long Dec1 = GiftCodes.Day(2026, 12, 1);
        static readonly long Dec10 = GiftCodes.Day(2026, 12, 10);
        static readonly long Jan1 = GiftCodes.Day(2027, 1, 1);

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Profile.Code = Fixture.CodeFor(11);
            quests = new List<QuestDef>();
            foreach (string id in new[] { "ngay-cham", "ngay-nang", "ngay-thu", "ngay-don", "ngay-anh" })
                quests.Add(new QuestDef { Id = id, Metric = QuestMetric.Taps, Target = 1, Daily = true, RewardCarrots = 1 });
        }

        void Open(long day)
        {
            Seasons.Clamp(s, day);
            Quests.RollDaily(quests, defs, s, s.MaxDaySeen, Modifiers.None);
        }

        void PlayFullDay(long day)
        {
            Open(day);
            s.TotalTaps = s.DailyTapsBase + 1;
            foreach (QuestDef q in quests) Quests.TryClaim(q, defs, s, Modifiers.None, out _);
            Assert.IsNotNull(QuestChests.OpenBox(quests, defs, s, Modifiers.None));
        }

        void ReachTier(long day, int tier)
        {
            Open(day);
            MonthPass.AddPoints(s, day, tier * MonthPass.PointsPerTier);
            Assert.AreEqual(tier, MonthPass.Tier(s, day));
        }

        bool Claim(MonthRow row, int tier, long day, string kind = null)
            => MonthPass.Claim(defs, s, Modifiers.None, row, tier, day, kind, "Vé", out _);

        [Test]
        public void Thirty_Tiers_Of_Forty_Points_With_The_Rewards_The_Issue_Lists()
        {
            Assert.AreEqual(30, MonthPass.Tiers);
            Assert.AreEqual(40, MonthPass.PointsPerTier);
            Assert.AreEqual("vuontra.monthpass", MonthPass.ProductId);
            Assert.AreEqual("129.000 ₫", MonthPass.ReferencePrice);

            Assert.AreEqual(MonthGiftKind.Income, MonthPass.GiftOf(MonthRow.Free, 1).Kind);
            Assert.AreEqual(120, MonthPass.GiftOf(MonthRow.Free, 1).Amount);
            Assert.AreEqual(MonthGiftKind.Clover, MonthPass.GiftOf(MonthRow.Free, 2).Kind);
            Assert.AreEqual(MonthGiftKind.Jar, MonthPass.GiftOf(MonthRow.Free, 3).Kind);
            Assert.AreEqual(MonthGiftKind.Income, MonthPass.GiftOf(MonthRow.Free, 4).Kind);
            int bonus = 0;
            for (int t = 1; t <= MonthPass.Tiers; t++) bonus += MonthPass.GiftOf(MonthRow.Free, t).Golden;
            Assert.AreEqual(15, bonus, "tiers 10, 20 and 30 add five golden carrots each");
            Assert.AreEqual(5, MonthPass.GiftOf(MonthRow.Free, 20).Golden);

            Assert.AreEqual(MonthGiftKind.Golden, MonthPass.GiftOf(MonthRow.Premium, 1).Kind);
            Assert.AreEqual(5, MonthPass.GiftOf(MonthRow.Premium, 1).Amount);
            Assert.AreEqual(MonthGiftKind.Jar, MonthPass.GiftOf(MonthRow.Premium, 2).Kind);
            Assert.AreEqual(2, MonthPass.GiftOf(MonthRow.Premium, 2).Amount);
            Assert.AreEqual(MonthGiftKind.Lantern, MonthPass.GiftOf(MonthRow.Premium, 10).Kind);
            Assert.AreEqual(MonthGiftKind.Frame, MonthPass.GiftOf(MonthRow.Premium, 20).Kind);
            Assert.AreEqual(MonthGiftKind.Rabbit, MonthPass.GiftOf(MonthRow.Premium, 30).Kind);
            Assert.AreEqual(Photos.GoldTier, MonthPass.GiftOf(MonthRow.Premium, 30).Amount, "the Gold tier, the fifth of six");
            Assert.AreEqual("Vàng", new[] { "Bạc", "Xanh lá", "Xanh dương", "Tím", "Vàng", "Đỏ" }[MonthPass.RabbitRarity]);
        }

        [Test]
        public void The_Months_Follow_The_Calendar()
        {
            Assert.AreEqual(MonthPass.MonthOf(Dec1), MonthPass.MonthOf(GiftCodes.Day(2026, 12, 31)));
            Assert.AreEqual(MonthPass.MonthOf(Dec1) + 1, MonthPass.MonthOf(Jan1));
            Assert.AreEqual(12, MonthPass.MonthNumber(MonthPass.MonthOf(Dec1)));
            Assert.AreEqual(1, MonthPass.MonthNumber(MonthPass.MonthOf(Jan1)));
            Assert.AreEqual(31, MonthPass.DaysLeft(Dec1));
            Assert.AreEqual(1, MonthPass.DaysLeft(GiftCodes.Day(2026, 12, 31)));
        }

        [Test]
        public void The_Daily_Quest_Points_Fill_The_Month_Bar_As_They_Fill_The_Week()
        {
            PlayFullDay(Dec1);
            Assert.AreEqual(60, QuestChests.Points(s, Dec1));
            Assert.AreEqual(60, MonthPass.Points(s, Dec1), "the same points: five dailies and the box");
            Assert.AreEqual(1, MonthPass.Tier(s, Dec1));
            PlayFullDay(Dec1 + 1);
            Assert.AreEqual(3, MonthPass.Tier(s, Dec1 + 1));
            for (int d = 2; d < 31; d++) PlayFullDay(Dec1 + d);
            Assert.AreEqual(MonthPass.Tiers * MonthPass.PointsPerTier, MonthPass.Points(s, Dec1 + 30), "the bar stops at the top");
            Assert.AreEqual(MonthPass.Tiers, MonthPass.Tier(s, Dec1 + 30));
        }

        [Test]
        public void Each_Tier_Pays_Once_On_Each_Row()
        {
            ReachTier(Dec10, 3);
            Assert.IsFalse(Claim(MonthRow.Free, 4, Dec10), "tier four is not reached");
            Assert.AreEqual(3, MonthPass.ReadyCount(s, Dec10));
            int clovers = s.Clovers;
            Assert.IsTrue(Claim(MonthRow.Free, 2, Dec10));
            Assert.AreEqual(clovers + 1, s.Clovers);
            Assert.IsFalse(Claim(MonthRow.Free, 2, Dec10), "once");
            Assert.AreEqual(clovers + 1, s.Clovers);
            Assert.IsTrue(MonthPass.Claim(defs, s, Modifiers.None, MonthRow.Free, 1, Dec10, null, "", out MonthClaim income));
            Assert.AreEqual(new BigNum(120), income.Coins, "two minutes of an empty garden's income is its floor of 120");
            Assert.AreEqual(1, MonthPass.ReadyCount(s, Dec10));

            Assert.IsFalse(Claim(MonthRow.Premium, 1, Dec10), "the lower row is locked until the pass is bought");
            Assert.IsTrue(MonthPass.Buy(s, Dec10));
            int golden = s.GoldenCarrots;
            Assert.IsFalse(Claim(MonthRow.Premium, 1, Dec10), "buying paid it already");
            Assert.AreEqual(golden, s.GoldenCarrots);
            Assert.IsTrue(MonthPass.Claimed(s, MonthRow.Premium, 1));
            Assert.IsFalse(MonthPass.Claimed(s, MonthRow.Free, 3), "buying pays the lower row, not the free one");
        }

        [Test]
        public void Buying_Mid_Month_Pays_Every_Premium_Tier_Already_Passed()
        {
            s.Plots = 6;
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel);
            ReachTier(Dec10, 12);
            int golden = s.GoldenCarrots, jars = s.MoonJars, clovers = s.Clovers;
            Assert.IsTrue(MonthPass.CanBuy(s, Dec10));
            Assert.IsTrue(MonthPass.Buy(s, Dec10));
            Assert.IsTrue(MonthPass.Premium(s, Dec10));
            Assert.AreEqual(golden + 6 * 5, s.GoldenCarrots, "tiers 1, 3, 5, 7, 9 and 11 bring five golden carrots each");
            Assert.AreEqual(5 * 2, s.MoonJars - jars + s.Clovers - clovers, "tiers 2, 4, 6, 8 and 12 bring two jars each, clovers past the shelf");
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            int cal = MonthPass.CalendarMonth(MonthPass.MonthOf(Dec10));
            Assert.IsTrue(MonthPass.OwnsLantern(s, cal), "tier 10's lantern colour is the player's");
            Assert.AreEqual(MonthPass.LanternIndex(cal), s.LanternColor, "and it is hung at once");
            for (int t = 1; t <= 12; t++) Assert.IsTrue(MonthPass.Claimed(s, MonthRow.Premium, t), "tier " + t);
            Assert.IsFalse(MonthPass.Claimed(s, MonthRow.Premium, 13));
            Assert.AreEqual(12, MonthPass.ReadyCount(s, Dec10), "the twelve free tiers still wait");

            MonthPass.AddPoints(s, Dec10, MonthPass.PointsPerTier);
            Assert.IsTrue(MonthPass.Ready(s, MonthRow.Premium, 13, Dec10), "later tiers are claimed one by one");
            Assert.IsTrue(Claim(MonthRow.Premium, 13, Dec10));
            Assert.IsFalse(MonthPass.CanBuy(s, Dec10), "one pass a month");
            Assert.IsFalse(MonthPass.Buy(s, Dec10));
        }

        [Test]
        public void Tier_Thirty_Brings_A_Gold_Rabbit_Of_One_Of_Three_Kinds()
        {
            ReachTier(Dec10, 30);
            MonthPass.Buy(s, Dec10);
            Assert.IsFalse(MonthPass.Claimed(s, MonthRow.Premium, 30), "the rabbit waits for the player's choice");
            string[] kinds = MonthPass.KindChoices(MonthPass.MonthOf(Dec10));
            Assert.AreEqual(3, kinds.Length);
            Assert.AreEqual(3, new HashSet<string>(kinds).Count);
            foreach (string k in kinds)
            {
                RabbitKind kind = RabbitKinds.Find(k);
                Assert.AreEqual(Photos.GoldTier, kind.Tier, k);
                Assert.AreEqual(Season.None, kind.Season, k);
                Assert.IsFalse(kind.NewMoonOnly, k);
            }
            CollectionAssert.AreNotEqual(kinds, MonthPass.KindChoices(MonthPass.MonthOf(Jan1)), "each month offers its own three");

            Assert.IsFalse(Claim(MonthRow.Premium, 30, Dec10, "tho-bong"), "a kind not offered is refused");
            Assert.IsFalse(Claim(MonthRow.Premium, 30, Dec10), "so is no kind");
            int rabbits = s.Rabbits.Count;
            Assert.IsTrue(MonthPass.Claim(defs, s, Modifiers.None, MonthRow.Premium, 30, Dec10, kinds[1], "Ngọc", out MonthClaim got));
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            Assert.AreEqual(kinds[1], got.Rabbit.Kind);
            Assert.AreEqual(Photos.GoldTier, got.Rabbit.Tier);
            Assert.AreEqual("Ngọc", got.Rabbit.Name);
            Assert.IsFalse(Claim(MonthRow.Premium, 30, Dec10, kinds[0]), "one rabbit");
        }

        [Test]
        public void The_Month_Lantern_And_Frame_Are_Kept_And_Can_Be_Worn_Again()
        {
            ReachTier(Dec10, 20);
            MonthPass.Buy(s, Dec10);
            int cal = MonthPass.CalendarMonth(MonthPass.MonthOf(Dec10));
            Assert.AreEqual(cal, s.PhotoFrame, "tier 20's frame goes on at once");
            Assert.IsTrue(MonthPass.OwnsFrame(s, cal));
            Assert.AreEqual(MonthPass.LanternColors[cal], MonthPass.LanternHex(s.LanternColor));
            Assert.AreEqual(Decor.LanternColors[2], MonthPass.LanternHex(2));

            s.GoldenCarrots = 20;
            Assert.IsTrue(Decor.SetLanternColor(s, 1), "the shop's colours still buy as before");
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.IsTrue(MonthPass.HangLantern(s, cal), "the month colour hangs again for free");
            Assert.AreEqual(10, s.GoldenCarrots);
            Assert.IsFalse(MonthPass.HangLantern(s, (cal + 1) % 12), "a colour not won stays in the shop window");
            Assert.IsTrue(MonthPass.WearFrame(s, -1));
            Assert.AreEqual(-1, s.PhotoFrame, "back to plain paper");
            Assert.IsFalse(MonthPass.WearFrame(s, (cal + 1) % 12));
            Assert.IsTrue(MonthPass.WearFrame(s, cal));
            Assert.AreEqual(12, MonthPass.LanternColors.Length);
            Assert.AreEqual(12, MonthPass.FrameColors.Length);
        }

        [Test]
        public void A_New_Month_Mails_What_Was_Left_For_Seven_Days_And_Starts_The_Bar_Again()
        {
            ReachTier(Dec10, 30);
            Assert.IsTrue(Claim(MonthRow.Free, 1, Dec10));
            MonthPass.Buy(s, Dec10);
            Assert.AreEqual(0, s.Letters.Count);

            Open(Jan1);
            Assert.AreEqual(0, MonthPass.Points(s, Jan1));
            Assert.AreEqual(0, MonthPass.Tier(s, Jan1));
            Assert.AreEqual(0, MonthPass.ReadyCount(s, Jan1));
            Assert.IsFalse(MonthPass.Claimed(s, MonthRow.Free, 2));
            Assert.AreEqual(2, s.Letters.Count, "one letter for the free row, one for the pass row");
            Letter free = s.Letters.Find(l => l.Kind == LetterKind.MonthPass && !l.Premium);
            Letter premium = s.Letters.Find(l => l.Kind == LetterKind.MonthPass && l.Premium);
            Assert.IsNotNull(free);
            Assert.IsNotNull(premium);
            Assert.AreEqual(MonthPass.KeepDays, free.KeepDays);
            Assert.AreEqual(29, MonthPass.TierCount(free.Tiers), "29 free tiers left unclaimed");
            Assert.AreEqual(15, free.Golden);
            Assert.AreEqual(10, free.Clovers);
            Assert.AreEqual(10, free.Jars);
            Assert.AreEqual(1, MonthPass.TierCount(premium.Tiers), "the rabbit was the only pass gift left");
            Assert.AreEqual(Photos.GoldTier, premium.RabbitTier);

            BigNum coins = s.Coins;
            int clovers = s.Clovers, golden = s.GoldenCarrots;
            Assert.IsNotNull(Mailbox.Claim(s, free.Id, Jan1 + 1));
            Assert.AreEqual(coins.ToDouble() + 9 * 120, s.Coins.ToDouble(), 1e-6, "nine income tiers at the floor of an empty garden");
            Assert.AreEqual(clovers + 10 + 10, s.Clovers, "ten clovers, and ten jars as clovers before the shelf shows");
            Assert.AreEqual(golden + 15, s.GoldenCarrots);

            int rabbits = s.Rabbits.Count;
            Mailbox.Claim(s, premium.Id, Jan1 + 1, "Thư", 0.5, out Rabbit rabbit);
            Assert.AreEqual(rabbits + 1, s.Rabbits.Count);
            CollectionAssert.Contains(MonthPass.KindChoices(MonthPass.MonthOf(Dec10)), rabbit.Kind, "one of December's three");

            ReachTier(Jan1 + 2, 5);
            Assert.IsFalse(MonthPass.Premium(s, Jan1 + 2), "December's pass does not open January's row");
            Assert.IsFalse(Claim(MonthRow.Premium, 1, Jan1 + 2));
            Assert.IsTrue(MonthPass.CanBuy(s, Jan1 + 2));

            Open(GiftCodes.Day(2027, 2, 1));
            Assert.AreEqual(1, s.Letters.FindAll(l => l.Kind == LetterKind.MonthPass).Count, "January left only free tiers");
            Mailbox.Open(s, GiftCodes.Day(2027, 2, 1) + MonthPass.KeepDays + 1);
            Assert.AreEqual(0, s.Letters.FindAll(l => l.Kind == LetterKind.MonthPass).Count, "unclaimed for more than seven days, it is gone");
        }

        [Test]
        public void Nothing_Left_Means_No_Letter()
        {
            ReachTier(Dec10, 2);
            Claim(MonthRow.Free, 1, Dec10);
            Claim(MonthRow.Free, 2, Dec10);
            Open(Jan1);
            Assert.AreEqual(0, s.Letters.Count);
        }

        [Test]
        public void Turning_The_Clock_Back_Gives_Nothing()
        {
            ReachTier(Dec10, 4);
            Open(Jan1);
            Assert.AreEqual(0, MonthPass.Tier(s, Jan1));
            int letters = s.Letters.Count;
            Open(Dec10);
            Assert.AreEqual(Jan1, s.MaxDaySeen, "the clamped day stays in January");
            Assert.AreEqual(0, MonthPass.Tier(s, Dec10), "an earlier day reads no bar");
            Assert.IsFalse(Claim(MonthRow.Free, 3, Dec10), "and claims nothing");
            MonthPass.Roll(s, Dec10, defs, Modifiers.None);
            Assert.AreEqual(MonthPass.MonthOf(Jan1), s.PassMonth, "the month never goes back");
            Assert.AreEqual(letters, s.Letters.Count, "nor mails December twice");
            MonthPass.AddPoints(s, Dec10, 400);
            Assert.AreEqual(0, MonthPass.Points(s, Jan1), "points stamped with an old day are not counted");
        }

        [Test]
        public void A_Restored_Or_Redelivered_Purchase_Keeps_The_Month_It_Was_Bought_In()
        {
            ReachTier(Dec10, 2);
            Assert.IsTrue(Storefront.Sells(MonthPass.ProductId));
            Assert.AreEqual(MonthPass.ReferencePrice, new FakeStorefront().PriceOf(MonthPass.ProductId));
            Assert.IsTrue(Storefront.Grant(s, MonthPass.ProductId, "GPA.month.1"));
            Assert.AreEqual(MonthPass.MonthOf(Dec10), s.PassBoughtMonth);
            int golden = s.GoldenCarrots;
            Assert.IsFalse(Storefront.Grant(s, MonthPass.ProductId, "GPA.month.1"), "the same receipt again pays nothing");
            Assert.IsFalse(Storefront.Grant(s, MonthPass.ProductId, "GPA.month.2"), "a second pass in the same month is refused");
            Assert.AreEqual(golden, s.GoldenCarrots);

            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(MonthPass.MonthOf(Dec10), back.PassBoughtMonth, "the save keeps the month");
            Assert.IsTrue(MonthPass.Premium(back, Dec10));
            Seasons.Clamp(back, Jan1);
            Quests.RollDaily(quests, defs, back, Jan1, Modifiers.None);
            Assert.IsFalse(MonthPass.Premium(back, Jan1), "restored in January, December's pass stays December's");
            Assert.IsFalse(Storefront.Grant(back, MonthPass.ProductId, "GPA.month.1"), "and its receipt does not buy January");
            Assert.IsFalse(MonthPass.Premium(back, Jan1));
            Assert.IsTrue(Storefront.Grant(back, MonthPass.ProductId, "GPA.month.3"), "a new purchase does");
            Assert.IsTrue(MonthPass.Premium(back, Jan1));
        }

        [Test]
        public void The_Pass_Round_Trips_And_An_Old_Save_Starts_With_An_Empty_Bar()
        {
            ReachTier(Dec10, 22);
            Claim(MonthRow.Free, 5, Dec10);
            MonthPass.Buy(s, Dec10);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.PassMonth, r.PassMonth);
            Assert.AreEqual(22 * MonthPass.PointsPerTier, r.PassPoints);
            Assert.AreEqual(s.PassFreeClaimed, r.PassFreeClaimed);
            Assert.AreEqual(s.PassPremiumClaimed, r.PassPremiumClaimed);
            Assert.AreEqual(s.PassBoughtMonth, r.PassBoughtMonth);
            Assert.AreEqual(s.MonthLanterns, r.MonthLanterns);
            Assert.AreEqual(s.MonthFrames, r.MonthFrames);
            Assert.AreEqual(s.PhotoFrame, r.PhotoFrame);
            Assert.AreEqual(s.LanternColor, r.LanternColor, "the month colour stays hung");

            Open(Jan1);
            Letter letter = s.Letters.Find(l => l.Kind == LetterKind.MonthPass && !l.Premium);
            GameState m = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Letter back = m.Letters.Find(l => l.Id == letter.Id);
            Assert.IsNotNull(back);
            Assert.AreEqual(letter.Tiers, back.Tiers);
            Assert.AreEqual(letter.Month, back.Month);
            Assert.AreEqual(letter.Coins, back.Coins);

            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"decor\":{\"lantern\":3}}").State;
            Assert.AreEqual(-1, old.PassMonth);
            Assert.AreEqual(0, old.PassPoints);
            Assert.AreEqual(-1, old.PassBoughtMonth);
            Assert.AreEqual(-1, old.PhotoFrame);
            Assert.AreEqual(3, old.LanternColor);

            GameState bad = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"decor\":{\"lantern\":9},"
                + "\"monthPass\":{\"month\":5,\"points\":99999,\"free\":-1,\"premium\":\"x\",\"extra\":0,\"bought\":-7,\"lanterns\":1,\"frames\":-1,\"frame\":40},"
                + "\"mail\":{\"day\":1,\"letters\":[{\"id\":\"month-1-free\",\"kind\":" + (int)LetterKind.MonthPass + ",\"month\":1,\"tiers\":-5,\"day\":1}]}}").State;
            Assert.AreEqual(MonthPass.Tiers * MonthPass.PointsPerTier, bad.PassPoints);
            Assert.AreEqual(0, bad.PassFreeClaimed, "marks below nothing are none");
            Assert.AreEqual(0, bad.PassPremiumClaimed);
            Assert.AreEqual(-1, bad.PassBoughtMonth);
            Assert.AreEqual(1, bad.MonthLanterns);
            Assert.AreEqual(0, bad.MonthFrames);
            Assert.AreEqual(-1, bad.PhotoFrame, "a frame past December is plain paper");
            Assert.AreEqual(0, bad.LanternColor, "a month colour the garden does not own is not hung");
            Assert.AreEqual(0, bad.Letters.Count, "a month letter with no tiers is no letter");
        }

        [Test]
        public void Analytics_Note_Each_New_Tier_And_The_Purchase()
        {
            var mem = new MemoryAnalytics();
            var watch = new MonthPassWatch();
            Open(Dec10);
            watch.Reset(s, Dec10);
            MonthPass.AddPoints(s, Dec10, 2 * MonthPass.PointsPerTier + 10);
            watch.Note(s, Dec10, mem);
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.MonthPassTier));
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.MonthPassTier)[AnalyticsEvents.Tier].Number);
            Assert.AreEqual(0L, mem.Last(AnalyticsEvents.MonthPassTier)[AnalyticsEvents.Premium].Number);
            watch.Note(s, Dec10, mem);
            Assert.AreEqual(2, mem.Count(AnalyticsEvents.MonthPassTier), "a tier is noted once");

            MonthPass.Buy(s, Dec10);
            watch.Note(s, Dec10, mem);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.MonthPassBought));
            Assert.AreEqual(2L, mem.Last(AnalyticsEvents.MonthPassBought)[AnalyticsEvents.Tier].Number);
            Assert.AreEqual(12L, mem.Last(AnalyticsEvents.MonthPassBought)[AnalyticsEvents.Month].Number);
            MonthPass.AddPoints(s, Dec10, MonthPass.PointsPerTier);
            watch.Note(s, Dec10, mem);
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.MonthPassTier)[AnalyticsEvents.Premium].Number);
            Assert.AreEqual(1, mem.Count(AnalyticsEvents.MonthPassBought));

            Open(Jan1);
            watch.Note(s, Jan1, mem);
            Assert.AreEqual(3, mem.Count(AnalyticsEvents.MonthPassTier), "a new month's empty bar notes nothing");
            MonthPass.AddPoints(s, Jan1, MonthPass.PointsPerTier);
            watch.Note(s, Jan1, mem);
            Assert.AreEqual(4, mem.Count(AnalyticsEvents.MonthPassTier));
            Assert.AreEqual(1L, mem.Last(AnalyticsEvents.MonthPassTier)[AnalyticsEvents.Tier].Number, "January counts from tier one again");
            CollectionAssert.Contains(AnalyticsEvents.All, AnalyticsEvents.MonthPassTier);
            CollectionAssert.Contains(AnalyticsEvents.All, AnalyticsEvents.MonthPassBought);
        }
    }
}

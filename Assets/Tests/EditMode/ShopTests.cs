using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>Golden carrots and the shop: the daily gift's chain, the golden call, where golden carrots come from, and the save.</summary>
    public class ShopTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 5, 2, 1 };
        }

        [Test]
        public void The_Daily_Gift_Follows_The_Chain_And_Repeats_Its_Tail()
        {
            int[] expected = { 1, 1, 2, 2, 3, 3, 5, 3, 3, 5, 3, 3, 5 };
            for (int day = 1; day <= expected.Length; day++) Assert.AreEqual(expected[day - 1], Shop.GiftAt(day), "day " + day);
            Assert.AreEqual(1, Shop.GiftAt(0), "never below the first day");
        }

        [Test]
        public void A_Gift_Comes_Once_A_Day_A_Missed_Day_Steps_Back_One_And_A_Clock_Set_Back_Gets_Nothing()
        {
            Assert.IsTrue(Shop.GiftWaiting(s, 100));
            Assert.AreEqual(1, Shop.ClaimDailyGift(s, 100));
            Assert.AreEqual(0, Shop.ClaimDailyGift(s, 100), "already paid today");
            Assert.IsFalse(Shop.GiftWaiting(s, 100));
            Assert.AreEqual(1, Shop.ClaimDailyGift(s, 101));
            Assert.AreEqual(2, Shop.ClaimDailyGift(s, 102));
            Assert.AreEqual(3, s.GiftStreak);
            Assert.AreEqual(4, s.GoldenCarrots);

            Assert.AreEqual(0, Shop.ClaimDailyGift(s, 101), "the clock went back: nothing twice");
            Assert.IsFalse(Shop.GiftWaiting(s, 99));
            Assert.AreEqual(3, s.GiftStreak, "and the streak is untouched");
            Assert.AreEqual(4, s.GoldenCarrots);
        }

        [Test]
        public void One_Missed_Day_Steps_The_Streak_Back_Exactly_One_And_Each_Further_Missed_Day_One_More()
        {
            Assert.AreEqual(1, Shop.MissPenalty);
            for (long day = 100; day <= 104; day++) Shop.ClaimDailyGift(s, day);
            Assert.AreEqual(5, s.GiftStreak);
            Assert.AreEqual(4, Shop.StreakFor(s, 106), "day 5, a day missed: the next gift is day 4's");
            Assert.AreEqual(Shop.GiftAt(4), Shop.ClaimDailyGift(s, 106));
            Assert.AreEqual(4, s.GiftStreak);
            Assert.AreEqual(5, Shop.StreakFor(s, 107), "then on as usual");

            Assert.AreEqual(2, Shop.StreakFor(s, 109), "two days missed: two back");
            Assert.AreEqual(1, Shop.StreakFor(s, 120), "never below day one");
            Assert.AreEqual(1, Shop.StreakFor(s, long.MaxValue / 2), "however long away");
            Shop.ClaimDailyGift(s, 109);
            Assert.AreEqual(2, s.GiftStreak);
        }

        [Test]
        public void The_First_Weeks_Seventh_Day_Brings_The_Conical_Hat_And_A_Clover_Exactly_Once()
        {
            List<ItemDef> items = HatDefs();
            for (long day = 100; day <= 105; day++) Assert.AreEqual(0, ClaimBig(day, items).Clovers);
            Assert.AreEqual(1 + 1 + 2 + 2 + 3 + 3, s.GoldenCarrots);
            Assert.IsFalse(s.GiftBigClaimed);
            Assert.IsTrue(Shop.IsBigDay(s, 7));

            int golden = Shop.ClaimDailyGift(s, 106, "A", items, out _, out Shop.BigGift big);
            Assert.IsTrue(big.Given);
            Assert.AreEqual(5, golden, "five golden carrots as every seventh day");
            Assert.AreEqual(17, s.GoldenCarrots, "a whole week brings seventeen");
            Assert.IsNotNull(big.Hat);
            Assert.AreEqual(Shop.BigGiftHat, big.Hat.DefId);
            Assert.AreEqual("hat-non-la", Shop.BigGiftHat, "the first hat the shop sells");
            Assert.AreEqual(0, big.Golden);
            Assert.AreEqual(1, big.Clovers);
            Assert.AreEqual(1, s.Clovers);
            Assert.IsTrue(Items.Owns(s, "hat-non-la"), "given, not sold");
            Assert.AreEqual(1, s.Items.Count);
            Assert.IsTrue(s.GiftBigClaimed);
            Assert.AreEqual(106, s.GiftBigDay);
            Assert.IsFalse(s.GiftBigGolden);
            Assert.IsFalse(Shop.IsBigDay(s, 7));

            // The second week's seventh day is five golden carrots, nothing more.
            for (long day = 107; day <= 112; day++) ClaimBig(day, items);
            Assert.AreEqual(13, s.GiftStreak);
            Shop.BigGift again = ClaimBig(113, items);
            Assert.AreEqual(14, s.GiftStreak);
            Assert.IsFalse(again.Given);
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(1, s.Items.Count);
            // Nor does stepping back to day seven bring it again.
            s.GiftStreak = 7;
            ClaimBig(115, items);
            Assert.AreEqual(6, s.GiftStreak);
            Assert.IsFalse(ClaimBig(116, items).Given);
            Assert.AreEqual(7, s.GiftStreak);
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(1, s.Items.Count);
        }

        [Test]
        public void A_Player_Who_Owns_The_Conical_Hat_Gets_Its_Price_In_Golden_Carrots_Instead()
        {
            List<ItemDef> items = HatDefs();
            s.GoldenCarrots = 20;
            Shop.BuyItem(s, items, "hat-non-la");
            for (long day = 100; day <= 105; day++) ClaimBig(day, items);
            int before = s.GoldenCarrots;
            int golden = Shop.ClaimDailyGift(s, 106, "A", items, out _, out Shop.BigGift big);
            Assert.IsTrue(big.Given);
            Assert.IsNull(big.Hat, "one of each: no second hat");
            Assert.AreEqual(20, big.Golden, "the hat's price instead");
            Assert.AreEqual(5 + 20, golden);
            Assert.AreEqual(before + 25, s.GoldenCarrots);
            Assert.AreEqual(1, s.Clovers);
            Assert.AreEqual(1, s.Items.Count);
            Assert.IsTrue(s.GiftBigGolden);
        }

        [Test]
        public void A_Clock_Set_Back_On_The_Big_Day_Gives_Nothing_Twice()
        {
            List<ItemDef> items = HatDefs();
            for (long day = 100; day <= 106; day++) ClaimBig(day, items);
            Assert.IsTrue(s.GiftBigClaimed);
            int golden = s.GoldenCarrots, clovers = s.Clovers, owned = s.Items.Count;
            for (long day = 100; day <= 106; day++)
            {
                Assert.AreEqual(0, Shop.ClaimDailyGift(s, day, "A", items, out _, out Shop.BigGift big), "day " + day);
                Assert.IsFalse(big.Given);
            }
            Assert.AreEqual(golden, s.GoldenCarrots);
            Assert.AreEqual(clovers, s.Clovers);
            Assert.AreEqual(owned, s.Items.Count);
            Assert.AreEqual(7, s.GiftStreak);
        }

        [Test]
        public void The_Week_Row_Shows_Claimed_Today_And_Later_Days_And_Repeats_From_Day_Eight()
        {
            Shop.GiftTile[] week = Shop.Week(s, 100);
            Assert.AreEqual(7, week.Length);
            Assert.IsTrue(week[0].Today);
            Assert.IsFalse(week[0].Claimed);
            for (int i = 1; i < 7; i++) Assert.IsFalse(week[i].Today || week[i].Claimed, "later: day " + (i + 1));
            Assert.IsTrue(week[6].Big, "the seventh tile is the big gift");
            for (int i = 0; i < 6; i++) Assert.IsFalse(week[i].Big);
            CollectionAssert.AreEqual(new[] { 1, 1, 2, 2, 3, 3, 5 }, System.Array.ConvertAll(week, t => t.Golden));

            for (long day = 100; day <= 102; day++) Shop.ClaimDailyGift(s, day);
            week = Shop.Week(s, 103);
            Assert.IsTrue(week[0].Claimed && week[1].Claimed && week[2].Claimed);
            Assert.IsTrue(week[3].Today && !week[3].Claimed, "day four is today's");
            Assert.IsFalse(week[4].Claimed || week[4].Today);
            week = Shop.Week(s, 102);
            Assert.IsTrue(week[2].Today && week[2].Claimed, "today's, already taken");
            Assert.IsFalse(week[3].Claimed || week[3].Today);

            for (long day = 103; day <= 106; day++) Shop.ClaimDailyGift(s, day, "A", HatDefs(), out _, out _);
            week = Shop.Week(s, 106);
            Assert.IsTrue(week[6].Today && week[6].Claimed && week[6].Big, "the big gift taken today still shows what it was");
            week = Shop.Week(s, 107);
            CollectionAssert.AreEqual(new[] { 8, 9, 10, 11, 12, 13, 14 }, System.Array.ConvertAll(week, t => t.Day));
            CollectionAssert.AreEqual(new[] { 3, 3, 5, 3, 3, 5, 3 }, System.Array.ConvertAll(week, t => t.Golden), "the rule as it is");
            Assert.IsTrue(week[0].Today);
            foreach (Shop.GiftTile t in week) Assert.IsFalse(t.Big, "no second big gift");
        }

        Shop.BigGift ClaimBig(long day, List<ItemDef> items)
        {
            Shop.ClaimDailyGift(s, day, "A", items, out _, out Shop.BigGift big);
            return big;
        }

        [Test]
        public void The_Golden_Call_Costs_Ten_Uses_Its_Own_Odds_And_Those_Odds_Add_Up()
        {
            double sum = 0d;
            foreach (double p in Shop.GoldenOdds) sum += p;
            Assert.AreEqual(1d, sum, 1e-9);
            Assert.AreEqual(Rabbits.TierCount, Shop.GoldenOdds.Length);
            Assert.Greater(Shop.GoldenOdds[3], Rabbits.Odds[3], "purple is likelier with gold");
            Assert.Less(Shop.GoldenOdds[0], Rabbits.Odds[0], "silver is not");

            s.GoldenCarrots = 9;
            Assert.IsNull(Shop.SummonGolden(s, 0.5, "A"), "nine is not enough");
            s.GoldenCarrots = 10;
            Rabbit r = Shop.SummonGolden(s, 0.999, "A", 0.5);
            Assert.IsNotNull(r);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(5, r.Tier, "the last percent is red");
            Assert.AreEqual(1, s.Rabbits.Count);
            Assert.AreEqual(0, Rabbits.RollTier(0.2, Shop.GoldenOdds));
            Assert.AreEqual(1, Rabbits.RollTier(0.3, Shop.GoldenOdds));
            Assert.AreEqual(2, Rabbits.RollTier(0.7, Shop.GoldenOdds));
            Assert.AreEqual(3, Rabbits.RollTier(0.9, Shop.GoldenOdds));
            Assert.AreEqual(4, Rabbits.RollTier(0.97, Shop.GoldenOdds));
        }

        [Test]
        public void Every_Fifth_Garden_Level_Brings_Two_Golden_Carrots()
        {
            Assert.AreEqual(0, Shop.GoldenForLevels(1, 4));
            Assert.AreEqual(2, Shop.GoldenForLevels(4, 5));
            Assert.AreEqual(4, Shop.GoldenForLevels(3, 11));
            s.GardenXp = Garden.XpForLevel(4);
            Garden.AddXp(s, Garden.XpForLevel(6) - Garden.XpForLevel(4));
            Assert.AreEqual(6, Garden.Level(s));
            Assert.AreEqual(2, s.GoldenCarrots, "level 5 passed on the way");
            Assert.AreEqual(2, s.Clovers, "and the clovers per level still come");
        }

        [Test]
        public void Golden_Batches_And_The_Days_First_Photo_Pay_Golden_Carrots_Now()
        {
            Tray.Add(s, 0, true);
            Tray.Sell(defs, s, 0, Modifiers.None, out int golden);
            Assert.AreEqual(1, golden);
            Assert.AreEqual(1, s.GoldenCarrots);
            Assert.AreEqual(0, s.Clovers);
            Photos.Take(s, 5);
            Assert.AreEqual(2, s.GoldenCarrots);
            Assert.AreEqual(0, s.Clovers);
        }

        [Test]
        public void Achievements_Can_Pay_Golden_Carrots_And_The_New_Metrics_Read_The_Album_Plots_And_Caps()
        {
            var purple = new QuestDef { Id = "loai-tim", Metric = QuestMetric.RarestKindMet, Target = 4, RewardGolden = 5 };
            var plots = new QuestDef { Id = "dat-12", Metric = QuestMetric.PlotsOwned, Target = 3, RewardGolden = 5 };
            var cap = new QuestDef { Id = "tho-max", Metric = QuestMetric.RabbitsAtCap, Target = 1, RewardGolden = 5 };
            Assert.IsFalse(Quests.IsComplete(purple, defs, s));
            Rabbits.See(s, "tho-oai-huong"); // a purple kind
            Assert.IsTrue(Quests.IsComplete(purple, defs, s));
            Assert.IsTrue(Quests.TryClaim(purple, defs, s, Modifiers.None, out _));
            Assert.AreEqual(5, s.GoldenCarrots);
            s.Plots = 3;
            Assert.IsTrue(Quests.IsComplete(plots, defs, s));
            Assert.IsFalse(Quests.IsComplete(cap, defs, s));
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "R", Level = Rabbits.LevelCaps[0] });
            Assert.IsTrue(Quests.IsComplete(cap, defs, s));
        }

        [Test]
        public void The_Visitor_Is_The_Same_All_Day_For_A_Garden_And_Another_Day_Or_Garden_Brings_Another()
        {
            s.Profile.Code = "THO-ABCDEF";
            Shop.Visitor a = Shop.VisitorFor(s, 300), b = Shop.VisitorFor(s, 300);
            Assert.AreEqual(a.Kind, b.Kind);
            Assert.AreEqual(a.Tier, b.Tier);
            Assert.GreaterOrEqual(a.Tier, Shop.VisitorFirstTier, "blue or rarer");
            Assert.AreEqual(Shop.VisitorPrices[a.Tier - Shop.VisitorFirstTier], a.Price);
            Assert.IsFalse(a.Bought);
            Assert.IsFalse(a.Seen);

            var kinds = new HashSet<string>();
            for (long day = 300; day < 360; day++) kinds.Add(Shop.VisitorFor(s, day).Kind);
            Assert.Greater(kinds.Count, 3, "the days bring different rabbits");
            var other = GameState.NewGame(defs);
            other.Profile.Code = "THO-ZZZZZZ";
            int same = 0;
            for (long day = 300; day < 360; day++) if (Shop.VisitorFor(other, day).Kind == Shop.VisitorFor(s, day).Kind) same++;
            Assert.Less(same, 60, "another garden has its own visitors");
        }

        [Test]
        public void Visitor_Tiers_Follow_Their_Odds_And_Unmet_Kinds_Come_Three_Times_As_Often()
        {
            double sum = 0d;
            foreach (double p in Shop.VisitorTierOdds) sum += p;
            Assert.AreEqual(1d, sum, 1e-9);
            int[] tiers = new int[Rabbits.TierCount];
            for (long day = 0; day < 2000; day++) tiers[Shop.VisitorFor(s, day).Tier]++;
            Assert.AreEqual(0, tiers[0] + tiers[1], "never silver or green");
            Assert.Greater(tiers[2], tiers[3]);
            Assert.Greater(tiers[3], tiers[4]);
            Assert.Greater(tiers[4], tiers[5]);

            // Every blue kind but one is in the album: the one that is not should be picked about three times as often.
            IReadOnlyList<RabbitKind> blue = RabbitKinds.OfTier(2);
            foreach (RabbitKind kind in blue) Rabbits.See(s, kind.Id);
            string missing = blue[0].Id;
            s.SeenKinds.Remove(missing);
            int hits = 0, n = 10000;
            for (int i = 0; i < n; i++) if (Shop.PickKind(s, 2, i / (double)n) == missing) hits++;
            double expected = (double)Shop.UnseenWeight / (Shop.UnseenWeight + blue.Count - 1);
            Assert.AreEqual(expected, hits / (double)n, 0.01);
        }

        [Test]
        public void The_Visitor_Can_Be_Bought_Once_Today_Only_For_Its_Price()
        {
            Shop.Visitor v = Shop.VisitorFor(s, 400);
            s.GoldenCarrots = v.Price - 1;
            Assert.IsNull(Shop.BuyVisitor(s, 400, "V"), "one short");
            s.GoldenCarrots = v.Price;
            Rabbit bought = Shop.BuyVisitor(s, 400, "V");
            Assert.IsNotNull(bought);
            Assert.AreEqual(v.Kind, bought.Kind);
            Assert.AreEqual(v.Tier, bought.Tier);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.IsTrue(Shop.VisitorFor(s, 400).Bought);
            s.GoldenCarrots = 1000;
            Assert.IsNull(Shop.BuyVisitor(s, 400, "V"), "once a day");
            Assert.IsFalse(Shop.VisitorFor(s, 401).Bought, "tomorrow's is for sale again");
            Assert.AreEqual(1000, s.GoldenCarrots);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(400, r.VisitorBoughtDay);
        }

        [Test]
        public void The_Save_Keeps_Golden_Carrots_And_The_Gift_And_An_Old_Save_Has_None()
        {
            s.GoldenCarrots = 7;
            Shop.ClaimDailyGift(s, 200);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(8, r.GoldenCarrots);
            Assert.AreEqual(200, r.GiftDay);
            Assert.AreEqual(1, r.GiftStreak);
            Assert.IsFalse(r.GiftBigClaimed);
            GameState old = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[]}").State;
            Assert.AreEqual(0, old.GoldenCarrots);
            Assert.IsTrue(Shop.GiftWaiting(old, 200), "an old garden's first gift is still to come");
            Assert.IsFalse(old.GiftBigClaimed, "and so is its big gift");
            Assert.AreEqual(-1, old.GiftBigDay);

            s.GiftBigDay = 206;
            s.GiftBigGolden = true;
            r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(206, r.GiftBigDay, "the big gift is remembered");
            Assert.IsTrue(r.GiftBigClaimed);
            Assert.IsTrue(r.GiftBigGolden);
        }
        // ----- #44 round 3: hats for looks, and fur dye -----

        static List<ItemDef> HatDefs() => new List<ItemDef>
        {
            new ItemDef { Id = "mu-rom", Name = "Mũ rơm", Slot = ItemSlot.Hat, Effect = ItemEffect.StationIncome, BaseAmount = 0.1 },
            new ItemDef { Id = "hat-non-la", Name = "Nón lá", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 20, Tier = 1 },
            new ItemDef { Id = "hat-vuong-mien", Name = "Vương miện", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 60, Tier = 4 },
        };

        [Test]
        public void A_Hat_For_Looks_Is_Sold_Once_For_Its_Price_And_Only_With_Enough_Golden_Carrots()
        {
            List<ItemDef> items = HatDefs();
            s.GoldenCarrots = 19;
            Assert.IsNull(Shop.BuyItem(s, items, "hat-non-la"), "one short");
            Assert.AreEqual(0, s.Items.Count);
            s.GoldenCarrots = 20;
            Item hat = Shop.BuyItem(s, items, "hat-non-la");
            Assert.IsNotNull(hat);
            Assert.AreEqual(0, s.GoldenCarrots, "the price was paid");
            Assert.AreEqual("hat-non-la", hat.DefId);
            Assert.AreEqual(ItemSlot.Hat, hat.Slot);
            Assert.AreEqual(ItemEffect.None, hat.Effect, "for looks only");
            Assert.AreEqual(0d, hat.Amount, "no bonus at all");
            Assert.AreEqual(1, hat.Tier, "the tier the shop sells it at");
            Assert.AreEqual(0, hat.WornBy, "bought, not worn yet");
            Assert.AreSame(hat, s.Items[0]);
            Assert.IsTrue(Items.Owns(s, "hat-non-la"));
            Assert.IsFalse(Items.Owns(s, "hat-vuong-mien"));

            s.GoldenCarrots = 100;
            Assert.IsNull(Shop.BuyItem(s, items, "hat-non-la"), "one of each: it cannot be bought twice");
            Assert.AreEqual(100, s.GoldenCarrots);
            Assert.IsNull(Shop.BuyItem(s, items, "mu-rom"), "items that come from quests are not for sale");
            Assert.IsNull(Shop.BuyItem(s, items, "no-such-hat"));
            Assert.AreEqual(100, s.GoldenCarrots);
            Assert.AreEqual(4, Shop.BuyItem(s, items, "hat-vuong-mien").Tier);
            Assert.AreEqual(40, s.GoldenCarrots);

            // Worn, a hat for looks changes nothing about what the rabbit is worth.
            var r = new Rabbit { Id = 1, Tier = 0, Name = "A", Level = 1 };
            s.Rabbits.Add(r);
            double before = Rabbits.Multiplier(s, r);
            Assert.IsTrue(Items.Equip(s, hat.Id, 1));
            Assert.AreEqual(before, Rabbits.Multiplier(s, r), 1e-12);
            Assert.AreEqual(0d, Items.GardenBonus(s, ItemEffect.None));
        }

        [Test]
        public void Dye_Costs_Eight_Golden_Carrots_Each_Time_Washing_Is_Free_And_Both_Are_Kept_Per_Rabbit()
        {
            Assert.AreEqual(8, Shop.DyeCost);
            Assert.AreEqual(8, Shop.Dyes.Length, "a palette of eight");
            var ids = new HashSet<string>();
            foreach (Shop.Dye dye in Shop.Dyes)
            {
                Assert.IsTrue(ids.Add(dye.Id), "each dye has its own id: " + dye.Id);
                Assert.AreEqual(6, dye.Hex.Length, "a six-digit colour: " + dye.Hex);
            }
            s.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Name = "A" });
            s.Rabbits.Add(new Rabbit { Id = 2, Tier = 1, Name = "B" });
            Assert.AreEqual(-1, s.Rabbits[0].Dye, "white fur to begin with");

            s.GoldenCarrots = 7;
            Assert.IsFalse(Shop.DyeRabbit(s, 1, 3), "one short");
            Assert.AreEqual(-1, s.Rabbits[0].Dye);
            s.GoldenCarrots = 8;
            Assert.IsFalse(Shop.DyeRabbit(s, 1, 8), "no such colour");
            Assert.IsFalse(Shop.DyeRabbit(s, 1, -1));
            Assert.IsFalse(Shop.DyeRabbit(s, 9, 3), "no such rabbit");
            Assert.AreEqual(8, s.GoldenCarrots, "nothing was paid for nothing");
            Assert.IsTrue(Shop.DyeRabbit(s, 1, 3));
            Assert.AreEqual(3, s.Rabbits[0].Dye);
            Assert.AreEqual(-1, s.Rabbits[1].Dye, "its own colour, not the other rabbit's");
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.IsFalse(Shop.DyeRabbit(s, 1, 5), "another colour costs again");
            s.GoldenCarrots = 16;
            Assert.IsTrue(Shop.DyeRabbit(s, 1, 5));
            Assert.AreEqual(5, s.Rabbits[0].Dye);
            Assert.AreEqual(8, s.GoldenCarrots);
            Assert.IsFalse(Shop.DyeRabbit(s, 1, 5), "the colour it already has is not sold again");
            Assert.AreEqual(8, s.GoldenCarrots);

            Assert.IsTrue(Shop.Wash(s, 1), "washing is free");
            Assert.AreEqual(-1, s.Rabbits[0].Dye);
            Assert.AreEqual(8, s.GoldenCarrots);
            Assert.IsFalse(Shop.Wash(s, 1), "nothing left to wash out");
            Assert.IsFalse(Shop.Wash(s, 9));

            // The save keeps each rabbit's dye and a bought hat for looks; a save from before has white rabbits.
            Shop.DyeRabbit(s, 2, 7);
            s.GoldenCarrots = 60;
            Item crown = Shop.BuyItem(s, HatDefs(), "hat-vuong-mien");
            Items.Equip(s, crown.Id, 2);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(-1, back.Rabbits[0].Dye);
            Assert.AreEqual(7, back.Rabbits[1].Dye);
            Assert.AreEqual(1, back.Items.Count);
            Assert.AreEqual(ItemEffect.None, back.Items[0].Effect, "still for looks only after a round trip");
            Assert.AreEqual(4, back.Items[0].Tier);
            Assert.AreEqual(2, back.Items[0].WornBy);
            GameState old = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[],\"rabbits\":[{\"id\":1,\"tier\":0,\"name\":\"A\"}]}").State;
            Assert.AreEqual(-1, old.Rabbits[0].Dye, "an old save: no dye");
            GameState odd = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"rabbits\":[{\"id\":1,\"tier\":0,\"name\":\"A\",\"dye\":99}]}").State;
            Assert.AreEqual(-1, odd.Rabbits[0].Dye, "a colour the palette lacks is washed out");
        }
    }
}

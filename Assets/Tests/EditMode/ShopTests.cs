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
        public void A_Gift_Comes_Once_A_Day_A_Missed_Day_Starts_The_Chain_Over_And_A_Clock_Set_Back_Gets_Nothing()
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
            Assert.AreEqual(1, Shop.ClaimDailyGift(s, 104), "a day was missed: back to day one");
            Assert.AreEqual(1, s.GiftStreak);

            for (int day = 105; day <= 110; day++) Shop.ClaimDailyGift(s, day);
            Assert.AreEqual(7, s.GiftStreak);
            Assert.AreEqual(4 + 1 + 1 + 2 + 2 + 3 + 3 + 5, s.GoldenCarrots, "a whole week brings seventeen");
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
            GameState old = SaveCodec.Decode(defs, "{\"version\":12,\"stations\":[]}").State;
            Assert.AreEqual(0, old.GoldenCarrots);
            Assert.IsTrue(Shop.GiftWaiting(old, 200), "an old garden's first gift is still to come");
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

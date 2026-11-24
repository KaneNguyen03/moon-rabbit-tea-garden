using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class TrunkTests
    {
        const long Nov = 20_780L;

        List<StationDef> defs;
        List<ItemDef> items;
        GameState s;
        int names;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            Rabbits.GiftFirst(s, "Bông");
            Tutorial.Skip(s);
            s.GardenXp = Garden.XpForLevel(MoonJars.ShowLevel) + 1;
            items = new List<ItemDef>
            {
                new ItemDef { Id = "mu-rom", Name = "Mũ rơm", Slot = ItemSlot.Hat, Effect = ItemEffect.StationIncome, BaseAmount = 0.1 },
                new ItemDef { Id = "hat-non-la", Name = "Nón lá", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 20, Tier = 1 },
                new ItemDef { Id = "hat-mu-sao", Name = "Mũ sao", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 50, Tier = 3 },
                new ItemDef { Id = "hat-vuong-mien", Name = "Vương miện", Slot = ItemSlot.Hat, Effect = ItemEffect.None, Price = 60, Tier = 4 },
            };
            names = 0;
        }

        static long At(long day, int hour, int minute = 0) => day * 86400L + hour * 3600L + minute * 60L;

        TrunkShelf Visit(long clock) => Trunk.Visit(s, items, clock);

        TrunkBuy Buy(long clock, int slot) => Trunk.Buy(s, items, clock, slot, () => "Thỏ " + (++names), () => 0.5d);

        static List<string> Codes(TrunkShelf shelf)
        {
            var codes = new List<string>();
            foreach (TrunkOffer o in shelf.Offers) codes.Add(o.Code);
            return codes;
        }

        int SlotOf(TrunkShelf shelf, TrunkKind kind)
        {
            foreach (TrunkOffer o in shelf.Offers) if (o.Kind == kind) return o.Slot;
            return -1;
        }

        long WindowWith(TrunkKind kind, out int slot)
        {
            for (long day = Nov; day < Nov + 60; day++)
                foreach (int hour in new[] { 11, 19 })
                {
                    if (Trunk.WindowAt(At(day, hour, 5)) <= s.Trunk.Window) continue;
                    var probe = new GameState { Profile = s.Profile, Rabbits = s.Rabbits, Items = s.Items, GardenXp = s.GardenXp, Levels = s.Levels };
                    TrunkShelf shelf = Trunk.Visit(probe, items, At(day, hour, 5));
                    foreach (TrunkOffer o in shelf.Offers)
                        if (o.Kind == kind)
                        {
                            slot = o.Slot;
                            return At(day, hour, 5);
                        }
                }
            slot = -1;
            return -1;
        }

        [Test]
        public void The_Numbers_Are_The_Issues_Defaults()
        {
            Assert.AreEqual(new[] { 11, 19 }, Trunk.OpenHours);
            Assert.AreEqual(new[] { 13, 22 }, Trunk.CloseHours);
            Assert.AreEqual(4, Trunk.Shown);
            Assert.AreEqual(30, Trunk.SalePercent);
            Assert.AreEqual(60, Trunk.RabbitPrice);
            Assert.AreEqual(30, Trunk.JarsPrice);
            Assert.AreEqual(3, Trunk.JarsAmount);
            Assert.AreEqual(25, Trunk.CloversPrice);
            Assert.AreEqual(5, Trunk.CloversAmount);
            Assert.AreEqual(40, Trunk.LanternPrice);
            Assert.AreEqual(18, Trunk.GoldenCallPrice);
            Assert.AreEqual(2, Trunk.GoldenCallAmount);
            Assert.Less(Trunk.GoldenCallPrice, Trunk.GoldenCallAmount * Shop.GoldenSummonCost, "cheaper than two calls in the shop");
        }

        [Test]
        public void The_Trunk_Opens_From_Eleven_To_One_And_From_Seven_To_Ten()
        {
            Assert.IsFalse(Visit(At(Nov, 10, 59)).Open);
            Assert.AreEqual(11, Visit(At(Nov, 10, 59)).NextHour);
            Assert.IsTrue(Visit(At(Nov, 11)).Open);
            Assert.IsTrue(Visit(At(Nov, 12, 59)).Open);
            Assert.AreEqual(13, Visit(At(Nov, 12, 59)).CloseHour);
            Assert.IsFalse(Visit(At(Nov, 13)).Open);
            Assert.AreEqual(19, Visit(At(Nov, 13)).NextHour, "closed in the afternoon says it opens at 19:00");
            Assert.IsFalse(Visit(At(Nov, 18, 59)).Open);
            Assert.IsTrue(Visit(At(Nov, 19)).Open);
            Assert.IsTrue(Visit(At(Nov, 21, 59)).Open);
            Assert.AreEqual(22, Visit(At(Nov, 21, 59)).CloseHour);
            Assert.IsFalse(Visit(At(Nov, 22)).Open);
            Assert.AreEqual(11, Visit(At(Nov, 23, 30)).NextHour);
            Assert.AreEqual(0, Visit(At(Nov, 22)).Offers.Length, "a closed trunk shows nothing");
        }

        [Test]
        public void Four_Items_Stay_The_Same_Within_A_Window_And_Change_At_The_Next()
        {
            for (long day = Nov; day < Nov + 10; day++)
            {
                List<string> noon = Codes(Visit(At(day, 11, 1)));
                Assert.AreEqual(Trunk.Shown, noon.Count);
                Assert.AreEqual(noon, Codes(Visit(At(day, 12, 40))), "every open in the window shows the same four");
                List<string> evening = Codes(Visit(At(day, 19, 2)));
                Assert.AreEqual(Trunk.Shown, evening.Count);
                Assert.AreNotEqual(noon, evening, "the evening window draws again");
                Assert.AreEqual(evening, Codes(Visit(At(day, 21, 58))));
            }
        }

        [Test]
        public void The_Draw_Comes_From_The_Day_And_The_Window()
        {
            List<string> first = Codes(Visit(At(Nov + 3, 19, 5)));
            var other = new GameState { Profile = s.Profile, Rabbits = s.Rabbits, Items = s.Items, GardenXp = s.GardenXp, Levels = s.Levels };
            Assert.AreEqual(first, Codes(Trunk.Visit(other, items, At(Nov + 3, 21, 0))), "the same garden on another open of the window sees the same four");
        }

        [Test]
        public void One_Item_In_Four_Is_Thirty_Percent_Off()
        {
            for (long day = Nov; day < Nov + 10; day++)
            {
                TrunkShelf shelf = Visit(At(day, 19, 30));
                int sales = 0;
                foreach (TrunkOffer o in shelf.Offers)
                {
                    if (!o.Sale)
                    {
                        Assert.AreEqual(o.FullPrice, o.Price);
                        continue;
                    }
                    sales++;
                    Assert.AreEqual(Trunk.SalePrice(o.FullPrice), o.Price);
                    Assert.Less(o.Price, o.FullPrice);
                }
                Assert.AreEqual(1, sales);
            }
            Assert.AreEqual(42, Trunk.SalePrice(60));
            Assert.AreEqual(21, Trunk.SalePrice(30));
            Assert.AreEqual(18, Trunk.SalePrice(25));
            Assert.AreEqual(28, Trunk.SalePrice(40));
            Assert.AreEqual(13, Trunk.SalePrice(18));
        }

        [Test]
        public void An_Item_Bought_Is_Not_Sold_Again_In_The_Window()
        {
            long clock = At(Nov, 19, 10);
            TrunkShelf shelf = Visit(clock);
            TrunkOffer offer = shelf.Offers[0];
            s.GoldenCarrots = 500;
            TrunkBuy buy = Buy(clock, offer.Slot);
            Assert.IsTrue(buy.Ok);
            Assert.AreEqual(500 - offer.Price, s.GoldenCarrots);
            Assert.IsTrue(Visit(At(Nov, 21)).Offers[0].Bought);
            Assert.IsFalse(Buy(At(Nov, 21), offer.Slot).Ok, "once per window");
            Assert.AreEqual(500 - offer.Price, s.GoldenCarrots);
            Assert.AreEqual(Codes(shelf), Codes(Visit(At(Nov, 21, 30))), "buying keeps the other three where they were");
            TrunkShelf next = Visit(At(Nov + 1, 11, 5));
            foreach (TrunkOffer o in next.Offers) Assert.IsFalse(o.Bought, "the next window starts fresh");
        }

        [Test]
        public void Buying_Needs_The_Golden_Carrots_And_An_Open_Trunk()
        {
            long clock = At(Nov, 11, 30);
            TrunkOffer offer = Visit(clock).Offers[1];
            s.GoldenCarrots = offer.Price - 1;
            Assert.IsFalse(Buy(clock, offer.Slot).Ok);
            Assert.AreEqual(offer.Price - 1, s.GoldenCarrots);
            s.GoldenCarrots = 500;
            Assert.IsFalse(Buy(At(Nov, 14), offer.Slot).Ok, "closed in the afternoon");
            Assert.IsFalse(Buy(clock, 9).Ok);
            Assert.IsFalse(Buy(clock, -1).Ok);
            Assert.AreEqual(500, s.GoldenCarrots);
        }

        [Test]
        public void Turning_The_Clock_Back_Does_Not_Reopen_A_Window_Already_Bought_From()
        {
            long clock = At(Nov + 1, 19, 30);
            s.GoldenCarrots = 500;
            TrunkOffer offer = Visit(clock).Offers[2];
            Assert.IsTrue(Buy(clock, offer.Slot).Ok);
            int left = s.GoldenCarrots;
            Assert.IsFalse(Visit(At(Nov + 1, 12)).Open, "the noon window of the same day is behind the one bought from");
            Assert.IsFalse(Visit(At(Nov, 19, 30)).Open, "yesterday evening does not come back");
            Assert.IsFalse(Buy(At(Nov, 19, 30), offer.Slot).Ok);
            Assert.AreEqual(19, Visit(At(Nov, 19, 30)).NextHour);
            TrunkShelf again = Visit(At(Nov + 1, 20));
            Assert.IsTrue(again.Open);
            Assert.IsTrue(again.Offers[2].Bought, "the bought item stays bought when the clock comes forward again");
            Assert.IsFalse(Buy(At(Nov + 1, 20), offer.Slot).Ok);
            Assert.AreEqual(left, s.GoldenCarrots);
        }

        [Test]
        public void A_Running_Festivals_Rabbit_Never_Goes_In_The_Trunk()
        {
            long christmas = GiftCodes.Day(2026, 12, 10);
            Assert.AreEqual(Season.Christmas, Seasons.SeasonOf(christmas));
            foreach (Trunk.Candidate c in Trunk.Pool(s, items, christmas)) Assert.AreNotEqual("kind:tho-noel", c.Code);
            CollectionAssert.Contains(Codes(Trunk.Pool(s, items, christmas)), "kind:tho-den-ong-sao");
            CollectionAssert.Contains(Codes(Trunk.Pool(s, items, christmas)), "kind:tho-ao-dai");
            CollectionAssert.Contains(Codes(Trunk.Pool(s, items, Nov)), "kind:tho-noel", "outside December the Noel rabbit can come back");
            for (long day = christmas; day < christmas + 15; day++)
                foreach (int hour in new[] { 11, 19 })
                    foreach (TrunkOffer o in Visit(At(day, hour, 1)).Offers) Assert.AreNotEqual("kind:tho-noel", o.Code);
        }

        [Test]
        public void Only_What_The_Player_Lacks_Goes_In_The_Trunk()
        {
            Rabbits.ArriveKind(s, "tho-ao-dai", "Áo Dài");
            Items.Grant(s, items[1], 1);
            MonthPass.HangLantern(s, 0);
            s.MonthLanterns |= 1 << 2;
            List<string> pool = Codes(Trunk.Pool(s, items, Nov));
            CollectionAssert.DoesNotContain(pool, "kind:tho-ao-dai", "a festival rabbit the player has");
            CollectionAssert.Contains(pool, "kind:tho-noel");
            CollectionAssert.DoesNotContain(pool, "hat:hat-non-la", "a hat the player has");
            CollectionAssert.DoesNotContain(pool, "hat:mu-rom", "quest items are not sold");
            CollectionAssert.Contains(pool, "hat:hat-mu-sao");
            CollectionAssert.DoesNotContain(pool, "lantern:month:2", "a lantern colour the player has");
            int now = MonthPass.CalendarMonth(MonthPass.MonthOf(Nov));
            CollectionAssert.DoesNotContain(pool, "lantern:month:" + now, "this month's colour belongs to this month's pass");
            CollectionAssert.Contains(pool, "lantern:month:" + (now + 11) % 12);
            CollectionAssert.Contains(pool, "jars");
            CollectionAssert.Contains(pool, "clovers");
            CollectionAssert.Contains(pool, "golden");
        }

        [Test]
        public void Each_Kind_Of_Item_Gives_What_It_Says()
        {
            s.GoldenCarrots = 1000;
            int slot;
            long clock = WindowWith(TrunkKind.Jars, out slot);
            Assert.Greater(clock, 0);
            int jars = s.MoonJars;
            TrunkBuy buy = Buy(clock, slot);
            Assert.IsTrue(buy.Ok);
            Assert.AreEqual(jars + Trunk.JarsAmount, s.MoonJars);

            clock = WindowWith(TrunkKind.Clovers, out slot);
            int clovers = s.Clovers;
            Assert.IsTrue(Buy(clock, slot).Ok);
            Assert.AreEqual(clovers + Trunk.CloversAmount, s.Clovers);

            clock = WindowWith(TrunkKind.GoldenCall, out slot);
            int rabbits = s.Rabbits.Count;
            long calls = s.GoldenCalls;
            buy = Buy(clock, slot);
            Assert.IsTrue(buy.Ok);
            Assert.IsNotNull(buy.Rabbit);
            Assert.IsNotNull(buy.Second);
            Assert.AreEqual(rabbits + 2, s.Rabbits.Count, "two golden calls");
            Assert.AreEqual(calls + 2, s.GoldenCalls);

            clock = WindowWith(TrunkKind.Hat, out slot);
            buy = Buy(clock, slot);
            Assert.IsTrue(buy.Ok);
            Assert.IsNotNull(buy.Hat);
            Assert.IsTrue(Items.Owns(s, buy.Offer.Id));

            clock = WindowWith(TrunkKind.Lantern, out slot);
            buy = Buy(clock, slot);
            Assert.IsTrue(buy.Ok);
            int cal = int.Parse(buy.Offer.Id.Substring("month:".Length));
            Assert.IsTrue(MonthPass.OwnsLantern(s, cal));
            Assert.AreEqual(MonthPass.LanternIndex(cal), s.LanternColor, "the new colour is hung");

            clock = WindowWith(TrunkKind.SeasonRabbit, out slot);
            buy = Buy(clock, slot);
            Assert.IsTrue(buy.Ok);
            Assert.IsTrue(buy.Offer.OldSeason);
            Assert.AreEqual(buy.Offer.Id, Rabbits.KindOf(buy.Rabbit));
        }

        [Test]
        public void Something_Got_Elsewhere_In_The_Window_Is_Shown_Owned_And_Not_Sold_Twice()
        {
            s.GoldenCarrots = 1000;
            long clock = WindowWith(TrunkKind.Hat, out int slot);
            TrunkOffer hat = Visit(clock).Offers[slot];
            Assert.IsFalse(hat.Owned);
            Assert.IsNotNull(Shop.BuyItem(s, items, hat.Id));
            hat = Visit(clock).Offers[slot];
            Assert.IsTrue(hat.Owned);
            int left = s.GoldenCarrots;
            Assert.IsFalse(Buy(clock, slot).Ok);
            Assert.AreEqual(left, s.GoldenCarrots);
        }

        [Test]
        public void The_Trunk_Never_Takes_Real_Money()
        {
            s.GoldenCarrots = 1000;
            long clock = At(Nov, 11, 15);
            foreach (TrunkOffer o in Visit(clock).Offers) Assert.IsTrue(Buy(clock, o.Slot).Ok);
            Assert.AreEqual(0, s.PaidTransactions.Count);
            Assert.AreEqual(0, s.PacksBought);
        }

        [Test]
        public void The_Trunk_Is_Saved_And_An_Old_Save_Loads_Without_It()
        {
            long clock = At(Nov, 19, 30);
            s.GoldenCarrots = 500;
            TrunkShelf shelf = Visit(clock);
            Assert.IsTrue(Buy(clock, shelf.Offers[1].Slot).Ok);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.Trunk.Window, back.Trunk.Window);
            Assert.AreEqual(s.Trunk.Codes, back.Trunk.Codes);
            Assert.AreEqual(s.Trunk.Sale, back.Trunk.Sale);
            Assert.AreEqual(s.Trunk.Bought, back.Trunk.Bought);
            Assert.IsTrue(Trunk.Visit(back, items, At(Nov, 21)).Offers[1].Bought);
            Assert.IsFalse(Trunk.Visit(back, items, At(Nov, 12)).Open);

            string json = SaveCodec.Encode(defs, GameState.NewGame(defs));
            int at = json.IndexOf(",\"trunk\":", System.StringComparison.Ordinal);
            Assert.Greater(at, 0);
            int end = json.IndexOf('}', at) + 1;
            LoadResult old = SaveCodec.Decode(defs, json.Remove(at, end - at));
            Assert.AreEqual(LoadStatus.Ok, old.Status);
            Assert.AreEqual(-1, old.State.Trunk.Window);
            Assert.AreEqual(0, old.State.Trunk.Codes.Count);
            Assert.AreEqual(0, old.State.Trunk.Bought);
            Assert.IsTrue(Trunk.Visit(old.State, items, At(Nov, 11, 30)).Open);
        }

        [Test]
        public void A_Bad_Trunk_Block_Loads_As_No_Trunk()
        {
            string json = SaveCodec.Encode(defs, GameState.NewGame(defs));
            int at = json.IndexOf(",\"trunk\":", System.StringComparison.Ordinal);
            int end = json.IndexOf('}', at) + 1;
            string bad = json.Remove(at, end - at).Insert(at, ",\"trunk\":{\"window\":\"x\",\"items\":[3,\"nope\"],\"sale\":99,\"bought\":-4}");
            LoadResult r = SaveCodec.Decode(defs, bad);
            Assert.AreEqual(LoadStatus.Ok, r.Status);
            Assert.AreEqual(-1, r.State.Trunk.Window);
            Assert.AreEqual(0, r.State.Trunk.Bought);
            Assert.AreEqual(Trunk.Shown, Trunk.Visit(r.State, items, At(Nov, 11, 30)).Offers.Length);
        }

        [Test]
        public void Opening_And_Buying_Are_Logged()
        {
            var log = new MemoryAnalytics();
            TrunkShelf shelf = Visit(At(Nov, 19, 30));
            log.LogTrunkOpen(shelf);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.TrunkOpen));
            Assert.AreEqual("evening", log.Last(AnalyticsEvents.TrunkOpen)[AnalyticsEvents.Window].Text);
            Assert.AreEqual(Trunk.Shown, log.Last(AnalyticsEvents.TrunkOpen)[AnalyticsEvents.Items].Number);
            log.LogTrunkOpen(Visit(At(Nov, 15)));
            Assert.AreEqual("closed", log.Last(AnalyticsEvents.TrunkOpen)[AnalyticsEvents.Window].Text);

            s.GoldenCarrots = 500;
            TrunkOffer offer = shelf.Offers[0];
            TrunkBuy buy = Buy(At(Nov, 19, 31), offer.Slot);
            log.LogTrunkBuy(buy);
            MemoryAnalytics.Event e = log.Last(AnalyticsEvents.TrunkBuy);
            Assert.AreEqual(offer.Code, e[AnalyticsEvents.Item].Text);
            Assert.AreEqual(offer.Price, e[AnalyticsEvents.Price].Number);
            Assert.AreEqual(offer.Sale ? 1L : 0L, e[AnalyticsEvents.Sale].Number);
            log.LogTrunkBuy(default);
            Assert.AreEqual(1, log.Count(AnalyticsEvents.TrunkBuy), "a failed buy logs nothing");
            CollectionAssert.Contains(AnalyticsEvents.All, AnalyticsEvents.TrunkOpen);
            CollectionAssert.Contains(AnalyticsEvents.All, AnalyticsEvents.TrunkBuy);
        }

        static List<string> Codes(IReadOnlyList<Trunk.Candidate> pool)
        {
            var codes = new List<string>();
            foreach (Trunk.Candidate c in pool) codes.Add(c.Code);
            return codes;
        }
    }
}

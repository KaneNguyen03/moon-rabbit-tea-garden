using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Core.Tests
{
    public class FourStationsTests
    {
        GameCatalog catalog;
        List<StationDef> defs;

        static readonly string[] NewIds = { "ho-sen", "lo-keo", "ruong-nep", "thuyen-tra" };

        [SetUp]
        public void SetUp()
        {
            catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>("Assets/VuonTra/Content/GameCatalog.asset");
            Assert.IsNotNull(catalog, "Run VuonTra > Seed Content first.");
            defs = catalog.BuildDefs();
        }

        [TearDown]
        public void TearDown()
        {
            L10n.SetLocale("vi");
        }

        int IndexOf(string id) => defs.FindIndex(d => d.Id == id);

        GameState TwelveOpen(int gardenLevel, int plots = 16)
        {
            GameState s = GameState.NewGame(defs);
            for (int i = 0; i < 12; i++) s.Levels[i] = 50;
            s.Plots = plots;
            s.Coins = new BigNum(1e30);
            s.GardenXp = Garden.XpForLevel(gardenLevel);
            GardenGrid.Settle(defs, s);
            return s;
        }

        [Test]
        public void The_Four_New_Stations_Come_After_The_Star_Observatory_In_Order()
        {
            Assert.AreEqual(16, defs.Count);
            Assert.AreEqual("dai-sao", defs[11].Id, "the old twelve keep their places");
            for (int k = 0; k < 4; k++) Assert.AreEqual(NewIds[k], defs[12 + k].Id);

            StationDef lotus = defs[12], candy = defs[13], rice = defs[14], boat = defs[15];
            Assert.AreEqual(StationKind.Ingredient, lotus.Kind);
            Assert.AreEqual(3e14, lotus.BaseCost, 1);
            Assert.AreEqual(StationKind.Brew, candy.Kind);
            Assert.AreEqual(4.5e15, candy.BaseCost, 1);
            Assert.AreEqual(60, candy.CycleSeconds, 1e-9);
            Assert.AreEqual(1.2e10, candy.BaseIncome, 1);
            Assert.AreEqual(RabbitTrade.Baker, candy.Trade, "Làm bánh");
            Assert.AreEqual(StationKind.Ingredient, rice.Kind);
            Assert.AreEqual(6.5e16, rice.BaseCost, 1);
            Assert.AreEqual(StationKind.Brew, boat.Kind);
            Assert.AreEqual(1e18, boat.BaseCost, 1);
            Assert.AreEqual(75, boat.CycleSeconds, 1e-9);
            Assert.AreEqual(1.8e11, boat.BaseIncome, 1);
            Assert.AreEqual(RabbitTrade.Brewer, boat.Trade, "Pha trà");
            foreach (StationDef d in new[] { lotus, rice }) Assert.AreEqual(RabbitTrade.Gatherer, d.Trade, d.Id);
            foreach (StationDef d in new[] { lotus, candy, rice, boat })
            {
                Assert.That(d.CostGrowth, Is.InRange(1.16, 1.17), d.Id);
                Assert.Greater(d.BaseCost, defs[IndexOf(d.Id) - 1].BaseCost, d.Id + " costs more than the one before");
            }
        }

        [Test]
        public void The_Ingredient_Stations_Feed_Exactly_The_Stations_They_Name()
        {
            CollectionAssert.AreEquivalent(new[] { "tiem-banh", "dai-sao" }, defs[12].BoostsIds);
            CollectionAssert.AreEquivalent(new[] { "lo-keo", "tra-sua" }, defs[14].BoostsIds);

            GameState s = TwelveOpen(30);
            s.Levels[13] = 1;
            var before = new BigNum[defs.Count];
            for (int i = 0; i < defs.Count; i++) before[i] = Economy.IncomePerCycle(defs, s, i);
            s.Levels[12] = 10;
            s.Levels[14] = 10;
            for (int i = 0; i < defs.Count; i++)
            {
                bool fed = defs[i].Id == "tiem-banh" || defs[i].Id == "dai-sao" || defs[i].Id == "lo-keo" || defs[i].Id == "tra-sua";
                BigNum after = Economy.IncomePerCycle(defs, s, i);
                if (fed) Assert.Greater(after.ToDouble(), before[i].ToDouble(), defs[i].Id + " is fed");
                else Assert.AreEqual(before[i].ToDouble(), after.ToDouble(), System.Math.Abs(before[i].ToDouble()) * 1e-12, defs[i].Id + " is not");
            }
        }

        [Test]
        public void The_Two_New_Ingredient_Stations_Give_Seven_Percent_Of_The_Usual_Boost()
        {
            for (int i = 0; i < 12; i++) Assert.AreEqual(1d, defs[i].BoostScale, 1e-12, defs[i].Id);
            Assert.AreEqual(0.07, defs[12].BoostScale, 1e-12, "the lotus garden");
            Assert.AreEqual(0.07, defs[14].BoostScale, 1e-12, "the rice paddy");
            Assert.AreEqual(1d + 2d * 0.07, Economy.IngredientBoost(defs[12], 10), 1e-12, "level 10: ×3 for an old one, ×1.14 here");
            Assert.AreEqual(Economy.IngredientBoost(10), Economy.IngredientBoost(defs[10], 10), 1e-12, "the old ones as before");

            GameState s = TwelveOpen(30);
            BigNum before = Economy.IncomePerCycle(defs, s, 11);
            s.Levels[12] = 10;
            double rabbit = Rabbits.StationMultiplier(defs, s, 12) * AlbumSets.StationFactor(defs, s, 12);
            Assert.AreEqual(before.ToDouble() * (1d + 2d * 0.07 * rabbit), Economy.IncomePerCycle(defs, s, 11).ToDouble(), before.ToDouble() * 1e-9, "the star observatory, fed by the lotus garden");
            Assert.AreEqual("×" + UiKit.Times(1.14), StationRowView.MainNumber(defs, s, 12, 10, Modifiers.None));
        }

        [Test]
        public void Each_New_Station_Asks_For_A_Higher_Garden_Level_And_The_Old_Ones_For_None()
        {
            for (int i = 0; i < 12; i++) Assert.AreEqual(0, defs[i].NeedGardenLevel, defs[i].Id);
            for (int i = 13; i < 16; i++) Assert.Greater(defs[i].NeedGardenLevel, defs[i - 1].NeedGardenLevel, defs[i].Id);
            Assert.GreaterOrEqual(defs[12].NeedGardenLevel, Expansion.Segment(4).NeedLevel, "not before the segment that gives the twelfth station its land");
            Assert.LessOrEqual(defs[15].NeedGardenLevel, NewMoon.FirstLevel, "the last one comes before the first new moon can");
        }

        [Test]
        public void The_Garden_Level_Blocks_Opening_Until_The_Garden_Has_Grown()
        {
            int need = defs[12].NeedGardenLevel;
            GameState s = TwelveOpen(need - 1);
            Assert.IsFalse(Economy.HasGardenLevel(defs, s, 12));
            Assert.IsFalse(Economy.CanUnlock(defs, s, 12), "carrots, land and the station before are not enough");
            Assert.IsFalse(Simulation.TryBuy(defs, s, 12));
            Assert.AreEqual(0, s.Levels[12]);

            s.GardenXp = Garden.XpForLevel(need);
            Assert.IsTrue(Economy.CanUnlock(defs, s, 12));
            Assert.IsTrue(Simulation.TryBuy(defs, s, 12));
            Assert.AreEqual(1, s.Levels[12]);

            s.GardenXp = Garden.XpForLevel(defs[13].NeedGardenLevel - 1);
            Assert.IsFalse(Economy.CanUnlock(defs, s, 13));
            s.GardenXp = Garden.XpForLevel(defs[15].NeedGardenLevel);
            Assert.IsTrue(Economy.CanUnlock(defs, s, 13));
            Assert.IsFalse(Economy.CanUnlock(defs, s, 14), "the station before is still closed");
        }

        [Test]
        public void Without_A_Plot_No_Garden_Level_Opens_Them()
        {
            GameState s = TwelveOpen(Garden.MaxLevel, 12);
            for (int i = 12; i < 16; i++) Assert.IsFalse(Economy.CanUnlock(defs, s, i), defs[i].Id);
            Assert.AreEqual(5, Expansion.Next(defs, s).SegmentsAfter, "their land is the next segment of the strip");
        }

        GameState AtRingSeven(int gardenLevel)
        {
            GameState s = TwelveOpen(gardenLevel, 12);
            s.Grid.Segments = 4;
            for (int i = 0; i < 12; i++) s.Rabbits.Add(new Rabbit { Id = ++s.NextRabbitId, Tier = 0, Name = "R" });
            foreach (RabbitKind kind in RabbitKinds.All) if (s.SeenKinds.Count < 8) s.SeenKinds.Add(kind.Id);
            GardenGrid.Settle(defs, s);
            Played(s, Expansion.Segment6Days);
            return s;
        }

        static void Played(GameState s, long days)
        {
            s.FirstDaySeen = 20000;
            s.MaxDaySeen = 20000 + days;
        }

        [Test]
        public void The_Fifth_And_Sixth_Segments_Make_Room_For_Two_Stations_Each_At_The_Level_Of_The_First()
        {
            Assert.AreEqual(6, Expansion.LastSegments(defs), "sixteen stations: six segments");
            Assert.AreEqual(defs[12].NeedGardenLevel, Expansion.Segment(5).NeedLevel, "the fifth (R 8) asks what the lotus garden asks");
            Assert.AreEqual(defs[14].NeedGardenLevel, Expansion.Segment(6).NeedLevel, "the sixth (R 9) asks what the rice paddy asks");
            Assert.AreEqual(Expansion.Segment(6).NeedLevel + Expansion.LevelStepBeyond, Expansion.Segment(7).NeedLevel, "past them, five more a segment");

            GameState s = AtRingSeven(Expansion.Segment(5).NeedLevel - 1);
            double price8 = (defs[12].BaseCost + defs[13].BaseCost) * Plots.CostShare * Expansion.Segment56CostTimes;
            Assert.AreEqual(price8, Expansion.CostOfNext(defs, s).ToDouble(), price8 * 1e-9, "a quarter of the two stations' price, times the segment's own factor (#99)");
            Assert.IsFalse(Expansion.CanExpand(defs, s), "a level short");
            s.GardenXp = Garden.XpForLevel(Expansion.Segment(5).NeedLevel);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(14, s.Plots);
            Assert.AreEqual(5, s.Grid.Segments);
            for (int i = 12; i < 14; i++) Assert.AreEqual(GardenGrid.DefaultStationTile(i), s.Grid.TileOf(GardenGrid.StationId(defs[i])), defs[i].Id + " on its own tile");
            Assert.IsNull(s.Grid.TileOf(GardenGrid.StationId(defs[14])), "the rice paddy waits for the sixth");
            Assert.IsTrue(Simulation.TryBuy(defs, s, 12), "the lotus garden can be opened");

            double price9 = (defs[14].BaseCost + defs[15].BaseCost) * Plots.CostShare * Expansion.Segment56CostTimes;
            Assert.AreEqual(price9, Expansion.CostOfNext(defs, s).ToDouble(), price9 * 1e-9);
            Assert.IsFalse(Expansion.Expand(defs, s), "the sixth asks for more");
            s.GardenXp = Garden.XpForLevel(Expansion.Segment(6).NeedLevel);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(16, s.Plots);
            Assert.AreEqual(6, s.Grid.Segments);
            for (int i = 14; i < 16; i++) Assert.AreEqual(GardenGrid.DefaultStationTile(i), s.Grid.TileOf(GardenGrid.StationId(defs[i])), defs[i].Id + " on its own tile");
            Assert.IsNull(Expansion.Next(defs, s), "every station has its land");
        }

        [Test]
        public void The_First_Four_Segments_Ask_No_Days_And_The_Plain_Share()
        {
            for (int n = 1; n <= 4; n++)
            {
                Assert.AreEqual(0, Expansion.Segment(n).NeedDays, "segment " + n);
                Assert.AreEqual(1d, Expansion.Segment(n).CostTimes, "segment " + n);
            }
            Assert.AreEqual(4, Expansion.Segment(5).NeedDays, "the fifth opens on day five at the soonest");
            Assert.AreEqual(7, Expansion.Segment(6).NeedDays, "the sixth on day eight at the soonest");
            Assert.That(Expansion.Segment56CostTimes, Is.InRange(2d, 3d), "about two to three times the plain share");
            Assert.AreEqual(Expansion.Segment56CostTimes, Expansion.Segment(5).CostTimes);
            Assert.AreEqual(Expansion.Segment56CostTimes, Expansion.Segment(6).CostTimes);
        }

        [Test]
        public void The_Fifth_Segment_Waits_For_Its_Level_Its_Days_And_Its_Price_Each()
        {
            int level = Expansion.Segment(5).NeedLevel, days = Expansion.Segment(5).NeedDays;
            GameState s = AtRingSeven(level);
            Played(s, days - 1);
            s.Coins = Expansion.CostOfNext(defs, s);
            Assert.IsFalse(Expansion.CanExpand(defs, s), "a day short");
            Assert.AreEqual(ExpandNeed.Days, Expansion.Missing(defs, s));
            Assert.AreEqual(1, Expansion.DaysLeft(Expansion.Next(defs, s), s));
            Assert.IsFalse(Expansion.Expand(defs, s));
            Assert.AreEqual(4, s.Grid.Segments, "nothing changed");

            Played(s, days);
            Assert.IsTrue(Expansion.HasDays(Expansion.Next(defs, s), s));
            s.GardenXp = Garden.XpForLevel(level - 1);
            Assert.AreEqual(ExpandNeed.Level, Expansion.Missing(defs, s), "a level short, with days enough");
            Assert.IsFalse(Expansion.CanExpand(defs, s));

            s.GardenXp = Garden.XpForLevel(level);
            s.Coins = Expansion.CostOfNext(defs, s) - BigNum.One;
            Assert.AreEqual(ExpandNeed.Carrots, Expansion.Missing(defs, s), "a carrot short of the dearer price");
            Assert.IsFalse(Expansion.CanExpand(defs, s));
            double plain = (defs[12].BaseCost + defs[13].BaseCost) * Plots.CostShare;
            Assert.Greater(s.Coins.ToDouble(), plain, "the plain quarter would have been paid already");

            s.Coins += BigNum.One;
            Assert.AreEqual(ExpandNeed.Nothing, Expansion.Missing(defs, s));
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(5, s.Grid.Segments);
            Assert.IsTrue(s.Coins.Equals(BigNum.Zero), "the dearer price was paid");
        }

        [Test]
        public void The_Sixth_Segment_Waits_For_Its_Own_Days()
        {
            GameState s = AtRingSeven(Expansion.Segment(6).NeedLevel);
            Played(s, Expansion.Segment(6).NeedDays - 1);
            Assert.IsTrue(Expansion.Expand(defs, s), "the fifth's days have passed");
            Assert.AreEqual(ExpandNeed.Days, Expansion.Missing(defs, s), "the sixth's have not");
            Assert.AreEqual(1, Expansion.DaysLeft(Expansion.Next(defs, s), s));
            Assert.IsFalse(Expansion.Expand(defs, s));
            Played(s, Expansion.Segment(6).NeedDays);
            Assert.IsTrue(Expansion.Expand(defs, s));
            Assert.AreEqual(ExpandNeed.AllOpen, Expansion.Missing(defs, s));
        }

        [Test]
        public void Days_Played_Count_From_The_First_Day_Seen_And_A_Clock_Turned_Back_Adds_None()
        {
            GameState s = GameState.NewGame(defs);
            Assert.AreEqual(-1, s.FirstDaySeen, "a new garden has not seen a day yet");
            Assert.AreEqual(0, Seasons.DaysPlayed(s));
            Seasons.Clamp(s, 20000);
            Assert.AreEqual(20000, s.FirstDaySeen, "the first day the game asks is the first day played");
            Assert.AreEqual(0, Seasons.DaysPlayed(s));
            Seasons.Clamp(s, 20003);
            Assert.AreEqual(3, Seasons.DaysPlayed(s));
            Assert.AreEqual(20003, Seasons.Clamp(s, 19990), "the clock set back: the game keeps the latest day");
            Assert.AreEqual(3, Seasons.DaysPlayed(s), "and the days played do not change");
            Assert.AreEqual(20000, s.FirstDaySeen, "nor does the first day");
            Seasons.Clamp(s, 20001);
            Assert.AreEqual(3, Seasons.DaysPlayed(s), "turning it forward again to a day already seen adds none");

            GameState fresh = GameState.NewGame(defs);
            Seasons.Clamp(fresh, 20000);
            Seasons.Clamp(fresh, 19000);
            Assert.AreEqual(0, Seasons.DaysPlayed(fresh));
            Assert.AreEqual(20000, fresh.FirstDaySeen);

            GameState g = AtRingSeven(Expansion.Segment(5).NeedLevel);
            g.FirstDaySeen = g.MaxDaySeen = -1;
            Seasons.Clamp(g, 20000);
            Seasons.Clamp(g, 20000 + Expansion.Segment(5).NeedDays - 1);
            Seasons.Clamp(g, 19000);
            Assert.AreEqual(ExpandNeed.Days, Expansion.Missing(defs, g));
        }

        [Test]
        public void The_First_Day_Is_Saved_And_Comes_Back()
        {
            GameState s = TwelveOpen(16, 12);
            Played(s, 2);
            GameState back = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(20000, back.FirstDaySeen);
            Assert.AreEqual(2, Seasons.DaysPlayed(back));
            GameState unseen = SaveCodec.Decode(defs, SaveCodec.Encode(defs, GameState.NewGame(defs))).State;
            Assert.AreEqual(-1, unseen.FirstDaySeen, "a garden that has not seen a day yet starts counting on its first");
        }

        GameState OldSave(int segments, int plots, long maxDay)
        {
            var stations = new List<string>();
            for (int i = 0; i < defs.Count; i++) stations.Add("{\"id\":\"" + defs[i].Id + "\",\"level\":" + (i < plots ? 50 : 0) + "}");
            string json = "{\"version\":14,\"stations\":[" + string.Join(",", stations) + "],\"plots\":" + plots + ",\"maxDay\":" + maxDay
                + ",\"coinsMantissa\":1,\"coinsExponent\":30,\"grid\":{\"segments\":" + segments + ",\"tiles\":[]}}";
            LoadResult loaded = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            return loaded.State;
        }

        [Test]
        public void An_Old_Save_Keeps_The_Land_It_Opened_And_Waits_On_No_Days()
        {
            GameState s = OldSave(5, 14, 20410);
            Assert.AreEqual(SaveCodec.LegacyFirstDay, s.FirstDaySeen, "a save from before #99 counts as played long since");
            Assert.AreEqual(5, Expansion.Segments(s), "the fifth segment stays open");
            Assert.AreEqual(14, s.Plots);
            Assert.IsTrue(s.Grid.InLawn(s.Grid.TileOf(GardenGrid.StationId(defs[12])).Value), "the lotus garden's land is still open lawn");
            MistSegment next = Expansion.Next(defs, s);
            Assert.AreEqual(6, next.SegmentsAfter);
            Assert.IsTrue(Expansion.HasDays(next, s), "the sixth waits on no days");
            Seasons.Clamp(s, 20000);
            Assert.IsTrue(Expansion.HasDays(next, s), "nor after the clock is turned back");

            GameState all = OldSave(6, 16, 20410);
            Assert.AreEqual(6, Expansion.Segments(all));
            Assert.AreEqual(16, all.Plots);
            Assert.IsNull(Expansion.Next(defs, all), "all its land stays open");

            GameState twelve = OldSave(4, 12, -1);
            Seasons.Clamp(twelve, 20410);
            Assert.AreEqual(SaveCodec.LegacyFirstDay, twelve.FirstDaySeen);
            Assert.IsTrue(Expansion.HasDays(Expansion.Next(defs, twelve), twelve), "the fifth waits on no days either");

            GameState saved = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(SaveCodec.LegacyFirstDay, saved.FirstDaySeen, "once saved again it keeps counting as long since");
        }

        [Test]
        public void The_Widen_Sign_Says_The_One_Thing_Still_Missing()
        {
            L10n.SetLocale("vi");
            MistSegment five = Expansion.Segment(5);
            GameState s = AtRingSeven(five.NeedLevel - 1);
            Assert.AreEqual(L10n.T("expand.sign.level", five.NeedLevel), L10n.ExpandSign(defs, s), "a level short first");
            Assert.AreEqual("Cần cấp vườn " + five.NeedLevel, L10n.ExpandSign(defs, s));

            s.GardenXp = Garden.XpForLevel(five.NeedLevel);
            Played(s, five.NeedDays - 3);
            Assert.AreEqual("Còn 3 ngày", L10n.ExpandSign(defs, s));
            Played(s, five.NeedDays - 1);
            Assert.AreEqual("Còn 1 ngày", L10n.ExpandSign(defs, s));

            Played(s, five.NeedDays);
            BigNum price = Expansion.CostOfNext(defs, s);
            s.Coins = price * 0.4;
            Assert.AreEqual(L10n.T("expand.sign.carrots", (price - s.Coins).Short()), L10n.ExpandSign(defs, s), "what is still to find, not the whole price");
            StringAssert.StartsWith("Còn ", L10n.ExpandSign(defs, s));
            StringAssert.EndsWith(" cà rốt", L10n.ExpandSign(defs, s));

            s.Coins = Expansion.CostOfNext(defs, s);
            Assert.AreEqual(L10n.T("garden.expand"), L10n.ExpandSign(defs, s), "all met: the sign's own words");

            GameState early = GameState.NewGame(defs);
            early.Plots = 6; GardenGrid.Settle(defs, early);
            early.GardenXp = Garden.XpForLevel(6);
            for (int i = 0; i < 4; i++) early.Rabbits.Add(new Rabbit { Id = ++early.NextRabbitId, Tier = 0, Name = "R" });
            Assert.AreEqual("Còn 2 bé thỏ", L10n.ExpandSign(defs, early), "the second segment asks six rabbits");

            GameState kinds = GameState.NewGame(defs);
            kinds.Plots = 10; GardenGrid.Settle(defs, kinds);
            kinds.GardenXp = Garden.XpForLevel(Expansion.Segment(4).NeedLevel);
            foreach (RabbitKind kind in RabbitKinds.All) if (kinds.SeenKinds.Count < 7) kinds.SeenKinds.Add(kind.Id);
            Assert.AreEqual("Còn 1 loài thỏ", L10n.ExpandSign(defs, kinds));
        }

        [Test]
        public void In_English_The_Widen_Sign_Reads_One_Day_And_Several_Days()
        {
            Assert.IsTrue(L10n.SetLocale("en"));
            MistSegment five = Expansion.Segment(5);
            GameState s = AtRingSeven(five.NeedLevel);
            Played(s, five.NeedDays - 1);
            Assert.AreEqual("1 more day", L10n.ExpandSign(defs, s));
            Played(s, five.NeedDays - 2);
            Assert.AreEqual("2 more days", L10n.ExpandSign(defs, s));
            s.GardenXp = Garden.XpForLevel(five.NeedLevel - 1);
            Assert.AreEqual("Needs garden level " + five.NeedLevel, L10n.ExpandSign(defs, s));
            Played(s, five.NeedDays);
            s.GardenXp = Garden.XpForLevel(five.NeedLevel);
            Assert.AreEqual("Widen the garden", L10n.ExpandSign(defs, s));
        }

        [Test]
        public void The_Four_Default_Tiles_Stand_Apart_From_Everything_On_The_Lawn_Of_Their_Segment()
        {
            var others = new List<Hex>();
            for (int i = 0; i < 12; i++) others.Add(GardenGrid.DefaultStationTile(i));
            var things = new List<Hex>();
            for (int n = 0; n < RestSpots.Max; n++) things.Add(GardenGrid.DefaultRestTile(n));
            for (int n = 0; n < Decor.Spots; n++) things.Add(GardenGrid.DefaultDecorTile(n));
            foreach (string id in new[] { GardenGrid.MortarId, GardenGrid.ShopId, GardenGrid.CounterId }) things.Add(GardenGrid.DefaultTile(id, defs));
            for (int i = 12; i < 16; i++)
            {
                Hex h = GardenGrid.DefaultStationTile(i);
                Assert.That(GardenGrid.SegmentOf(h), Is.InRange(0, GardenGrid.SegmentsForPlots(i + 1)), defs[i].Id + " stands on land open once its segment is");
                foreach (Hex o in others) Assert.GreaterOrEqual(GardenGrid.Distance(h, o), Expansion.StationSpacing, defs[i].Id + " keeps two tiles from the station at " + o);
                foreach (Hex t in things) Assert.AreNotEqual(t, h, defs[i].Id + " takes no seat, decoration or prop tile");
                others.Add(h);
            }
        }

        [Test]
        public void An_Old_Save_Of_Twelve_Stations_Gains_The_Four_At_Level_Zero_With_Nothing_Shifted()
        {
            List<StationDef> old = defs.GetRange(0, 12);
            GameState was = GameState.NewGame(old);
            was.Plots = 12;
            for (int i = 0; i < 12; i++)
            {
                was.Levels[i] = 10 + i;
                was.Progress[i] = 0.05 * i;
                was.TrayWork[i] = i;
            }
            Rabbit worker = Rabbits.Arrive(was, 1, "Lam", 0d);
            Assert.IsTrue(Rabbits.Assign(was, worker.Id, 11));
            Tray.Add(was, 9, false);
            string json = SaveCodec.Encode(old, was);
            StringAssert.Contains("\"version\":14", json);

            LoadResult loaded = SaveCodec.Decode(defs, json);
            Assert.AreEqual(LoadStatus.Ok, loaded.Status);
            GameState s = loaded.State;
            Assert.AreEqual(16, s.Levels.Length);
            Assert.AreEqual(16, s.Progress.Length);
            Assert.AreEqual(16, s.TrayWork.Length);
            for (int i = 0; i < 12; i++)
            {
                Assert.AreEqual(10 + i, s.Levels[i], defs[i].Id);
                Assert.AreEqual(0.05 * i, s.Progress[i], 1e-9, defs[i].Id);
                Assert.AreEqual(i, s.TrayWork[i], 1e-9, defs[i].Id);
            }
            for (int i = 12; i < 16; i++)
            {
                Assert.AreEqual(0, s.Levels[i], defs[i].Id);
                Assert.AreEqual(0d, s.Progress[i], defs[i].Id);
                Assert.AreEqual(0d, s.TrayWork[i], defs[i].Id);
                Assert.IsNull(s.Grid.TileOf(GardenGrid.StationId(defs[i])), defs[i].Id + " has no plot, so no tile");
            }
            Assert.AreEqual(12, s.Plots);
            Assert.AreEqual(11, s.Rabbits.Find(r => r.Id == worker.Id).Station, "the rabbit stays at the observatory");
            Assert.AreEqual(9, s.Tray[0].Station, "the tray keeps the bakery's batch");

            GameState again = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            CollectionAssert.AreEqual(s.Levels, again.Levels);
        }

        [Test]
        public void A_New_Moon_Puts_The_Four_New_Stations_Back_To_Zero_Like_The_Rest()
        {
            GameState s = TwelveOpen(NewMoon.FirstLevel);
            for (int i = 12; i < 16; i++) { s.Levels[i] = 30; s.Progress[i] = 0.5; s.TrayWork[i] = 3; }
            Assert.IsTrue(NewMoon.Begin(defs, s, "Trăng", out _));
            Assert.AreEqual(1, s.Levels[0]);
            for (int i = 1; i < 16; i++) Assert.AreEqual(0, s.Levels[i], defs[i].Id);
            for (int i = 12; i < 16; i++)
            {
                Assert.AreEqual(0d, s.Progress[i], defs[i].Id);
                Assert.AreEqual(0d, s.TrayWork[i], defs[i].Id);
            }
            Assert.IsFalse(Economy.CanUnlock(defs, s, 12), "and they ask for their garden level again");
        }

        [Test]
        public void The_Two_Makers_Have_Their_Own_Product_And_Every_New_Station_Its_Prop()
        {
            foreach (StationData data in catalog.Stations)
            {
                if (System.Array.IndexOf(NewIds, data.Id) < 0) continue;
                Assert.IsNotNull(data.Prop, data.Id);
                Assert.AreEqual("prop_" + data.Id, data.Prop.name, data.Id + " is drawn");
                if (data.Kind == StationKind.Brew)
                {
                    Assert.IsNotNull(data.Product, data.Id + " has something for the tray");
                    Assert.AreEqual("product_" + data.Id, data.Product.name);
                }
            }
        }

        [Test]
        public void Twelve_And_Sixteen_Stations_Open_Are_Quests_Paying_Five_Clovers()
        {
            List<QuestDef> quests = catalog.BuildQuests();
            foreach ((string id, int target) in new[] { ("mo-12", 12), ("mo-16", 16) })
            {
                QuestDef q = quests.Find(x => x.Id == id);
                Assert.IsNotNull(q, id);
                Assert.AreEqual(QuestMetric.StationsOpen, q.Metric, id);
                Assert.AreEqual(target, q.Target, 1e-9, id);
                Assert.AreEqual(5, q.RewardClovers, id);
                Assert.AreEqual(0, q.RewardGolden, id + ": golden carrots would push the casual week past six golden calls");
                Assert.IsFalse(q.Daily, id);
            }
            GameState s = TwelveOpen(30);
            QuestDef sixteen = quests.Find(x => x.Id == "mo-16");
            Assert.AreEqual(12, Quests.Progress(sixteen, defs, s), 1e-9);
            for (int i = 12; i < 16; i++) s.Levels[i] = 1;
            Assert.AreEqual(16, Quests.Progress(sixteen, defs, s), 1e-9);
        }

        [Test]
        public void Each_New_Station_Plays_A_Loop_And_Has_A_Name_In_Both_Languages()
        {
            foreach (string id in NewIds) Assert.IsNotNull(Sfx.StationLoopOf(id), id);
            var vi = new[] { "Vườn hoa sen", "Lò kẹo mạch nha", "Ruộng lúa nếp", "Thuyền trà đêm" };
            L10n.SetLocale("vi");
            for (int k = 0; k < 4; k++) Assert.AreEqual(vi[k], L10n.Station(NewIds[k]));
            Assert.AreEqual("Có 12 trạm đang hoạt động", L10n.T("quest.mo-12"));
            L10n.SetLocale("en");
            foreach (string id in NewIds)
            {
                string name = L10n.Station(id);
                Assert.IsFalse(string.IsNullOrEmpty(name), id);
                Assert.AreNotEqual("station." + id, name, id + " has an English name");
            }
            Assert.AreEqual("Needs garden level 16", L10n.T("row.needlevel", 16));
        }

        sealed class LiveScreen : System.IDisposable
        {
            public readonly GameObject Root;
            public readonly GameRunner Runner;
            public readonly GardenScreen View;

            public LiveScreen(GameCatalog catalog, GameState state)
            {
                Motion.SetReduced(true);
                Root = new GameObject("Game");
                var cam = new GameObject("Cam", typeof(Camera));
                cam.transform.SetParent(Root.transform);
                Runner = Root.AddComponent<GameRunner>();
                Runner.Catalog = catalog;
                Runner.Init(state);
                View = Root.AddComponent<GardenScreen>();
                View.Runner = Runner;
                View.Cam = cam.GetComponent<Camera>();
                View.Build();
                View.Shell.StartGame();
                View.Refresh();
            }

            public Text Text(string path)
            {
                Transform t = Root.transform.Find(path);
                Assert.IsNotNull(t, "Missing UI object: " + path);
                return t.GetComponent<Text>();
            }

            public void Dispose()
            {
                Object.DestroyImmediate(Root);
                Motion.SetReduced(false);
            }
        }

        [Test]
        public void The_Stations_Tab_Lists_Sixteen_Rows_And_Says_Which_Garden_Level_A_New_One_Asks_For()
        {
            L10n.SetLocale("vi");
            GameState s = TwelveOpen(10, 12);
            using (var screen = new LiveScreen(catalog, s))
            {
                screen.View.OpenSheet(0);
                screen.View.Refresh();
                Transform content = screen.Root.transform.Find("Canvas/Sheet/Panel/Scroll/Content");
                foreach (StationDef d in defs) Assert.IsNotNull(content.Find("Row " + d.Id), d.Id + " has a row");
                Assert.AreEqual(L10n.T("row.needlevel", defs[12].NeedGardenLevel), screen.Text("Canvas/Sheet/Panel/Scroll/Content/Row ho-sen/Sub").text);
                Assert.AreEqual(L10n.T("row.needlevel", defs[15].NeedGardenLevel), screen.Text("Canvas/Sheet/Panel/Scroll/Content/Row thuyen-tra/Sub").text);

                s.GardenXp = Garden.XpForLevel(defs[12].NeedGardenLevel);
                screen.View.Refresh();
                Assert.AreEqual(L10n.T("row.needplot"), screen.Text("Canvas/Sheet/Panel/Scroll/Content/Row ho-sen/Sub").text);
            }
        }

        [Test]
        public void With_Their_Segments_Open_The_Four_Stand_On_Their_Tiles_Inside_The_Garden()
        {
            GameState s = TwelveOpen(30);
            for (int i = 12; i < 16; i++) s.Levels[i] = 1;
            using (var screen = new LiveScreen(catalog, s))
            {
                var street = (RectTransform)screen.Root.transform.Find("Canvas/Garden/Content");
                for (int i = 12; i < 16; i++)
                {
                    Transform actor = street.Find("Station " + defs[i].Id);
                    Assert.IsTrue(actor.gameObject.activeSelf, defs[i].Id + " stands in the garden");
                    Vector2 spot = screen.View.StationSpot(i);
                    Assert.AreEqual(GardenScreen.CellToWorld(GardenGrid.DefaultStationTile(i)), spot, defs[i].Id + " on its tile");
                    Assert.IsTrue(spot.x > 200f && spot.x < street.sizeDelta.x - 200f && spot.y > 200f && spot.y < street.sizeDelta.y - 300f,
                        defs[i].Id + " at " + spot + " stands well inside the garden of " + street.sizeDelta);
                }
            }
        }

        [Test]
        public void Touching_A_New_Stations_Row_Slides_The_Garden_To_Its_Tile()
        {
            GameState s = TwelveOpen(30);
            for (int i = 12; i < 16; i++) s.Levels[i] = 1;
            using (var screen = new LiveScreen(catalog, s))
            {
                var world = (RectTransform)screen.Root.transform.Find("Canvas/Garden/Content");
                screen.View.OpenSheet(0);
                for (int f = 0; f < 10; f++) screen.View.Advance(1f / 30f);
                for (int i = 12; i < 16; i++)
                {
                    screen.Root.transform.Find("Canvas/Sheet/Panel/Scroll/Content/Row " + defs[i].Id).GetComponent<Button>().onClick.Invoke();
                    for (int f = 0; f < 12; f++) screen.View.Advance(1f / 30f);
                    Vector2 at = screen.View.StationSpot(i) * world.localScale.x + world.anchoredPosition;
                    Assert.Greater(at.y, screen.View.SheetHeight + 60f, defs[i].Id + " stands above the sheet");
                    Assert.Less(at.y, 1920f - 160f, defs[i].Id + " stands under the HUD");
                    Assert.That(at.x, Is.InRange(0f, 1080f), defs[i].Id + " is on the screen");
                }
            }
        }

        [Test]
        public void A_Batch_From_The_Candy_Kiln_Shows_Its_Own_Product_In_The_Tray()
        {
            GameState s = TwelveOpen(30);
            s.Levels[12] = 1; s.Levels[13] = 1;
            using (var screen = new LiveScreen(catalog, s))
            {
                Tray.Add(s, 13, false);
                screen.View.Refresh();
                Transform tray = screen.Root.transform.Find("Canvas/Tray");
                Assert.IsTrue(tray.gameObject.activeSelf);
                Assert.AreEqual("product_lo-keo", tray.Find("Slot 0/Icon").GetComponent<Image>().sprite.name);
            }
        }
    }
}

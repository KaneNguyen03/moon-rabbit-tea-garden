using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    /// <summary>
    /// Moon jars (#64): an hour of the garden's income kept in a jar and opened from the purse. What one is worth,
    /// the shelf of nine, the shop's three a day, the one free jar a day the daily quest, the firefly night and every
    /// tenth garden level share, and the save.
    /// </summary>
    public class MoonJarsTests
    {
        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            s.Levels = new[] { 30, 0, 0 }; // 30 × 4 (two milestones) per 1.5 s = 80 a second
        }

        [Test]
        public void A_Jar_Is_An_Hour_Of_Income_Times_The_Income_And_Speed_Multipliers_At_The_Moment_It_Is_Opened()
        {
            var m = Modifiers.None;
            m.IncomeMultiplier = 2d;
            m.SpeedMultiplier = 1.5d;
            m.AwayIncomeMultiplier = 3d; // time away's own bonus has nothing to do with a jar
            Assert.AreEqual(80d * 3600d * 2d * 1.5d, MoonJars.Value(defs, s, m).ToDouble(), 1e-6);

            s.MoonJars = 2;
            BigNum earned = s.TotalEarned;
            BigNum paid = MoonJars.Open(defs, s, m);
            Assert.AreEqual(80d * 3600d * 3d, paid.ToDouble(), 1e-6);
            Assert.AreEqual(paid.ToDouble(), s.Coins.ToDouble(), 1e-6);
            Assert.AreEqual(1, s.MoonJars);
            Assert.AreEqual(earned.ToDouble(), s.TotalEarned.ToDouble(), 1e-9, "a jar is a gift: it does not count towards the day's carrot goal");

            // The garden grew before the second jar was opened: the jar follows the income of the moment it is opened.
            s.Levels[0] = 60;
            BigNum later = MoonJars.Value(defs, s, m);
            Assert.Greater(later.ToDouble(), paid.ToDouble());
            Assert.AreEqual(later.ToDouble(), MoonJars.Open(defs, s, m).ToDouble(), 1e-6);
            Assert.AreEqual(0, s.MoonJars);

            BigNum coins = s.Coins;
            Assert.IsTrue(MoonJars.Open(defs, s, m).IsZero, "no jar, nothing");
            Assert.AreEqual(coins.ToDouble(), s.Coins.ToDouble(), 1e-9);
            Assert.AreEqual(0, s.MoonJars);
        }

        [Test]
        public void The_Shop_Sells_A_Jar_For_Twelve_Golden_Carrots_Three_A_Day_By_The_Day_It_Is_Given()
        {
            s.GoldenCarrots = 100;
            for (int i = 0; i < MoonJars.BuyPerDay; i++)
            {
                Assert.AreEqual(MoonJars.BuyCheck.Ok, MoonJars.CanBuy(s, 50));
                Assert.IsTrue(MoonJars.Buy(s, 50));
            }
            Assert.AreEqual(3, s.MoonJars);
            Assert.AreEqual(100 - 3 * MoonJars.Price, s.GoldenCarrots);
            Assert.AreEqual(0, MoonJars.BuyLeft(s, 50));
            Assert.AreEqual(MoonJars.BuyCheck.SoldOut, MoonJars.CanBuy(s, 50));
            Assert.IsFalse(MoonJars.Buy(s, 50), "three a day");
            Assert.AreEqual(3, s.MoonJars);

            // The day the game goes by is the clamped day (#78): a clock turned back to yesterday buys nothing more.
            Assert.IsFalse(MoonJars.Buy(s, 49), "yesterday again: nothing");
            Assert.AreEqual(MoonJars.BuyCheck.SoldOut, MoonJars.CanBuy(s, 49));

            Assert.AreEqual(MoonJars.BuyPerDay, MoonJars.BuyLeft(s, 51), "a new day, three more");
            Assert.IsTrue(MoonJars.Buy(s, 51));
            Assert.AreEqual(2, MoonJars.BuyLeft(s, 51));

            s.GoldenCarrots = MoonJars.Price - 1;
            Assert.AreEqual(MoonJars.BuyCheck.Short, MoonJars.CanBuy(s, 51));
            Assert.IsFalse(MoonJars.Buy(s, 51));
            Assert.AreEqual(MoonJars.Price - 1, s.GoldenCarrots, "nothing taken");
        }

        [Test]
        public void Nine_Jars_Fill_The_Shelf_And_The_Shop_Will_Not_Sell_A_Tenth()
        {
            s.GoldenCarrots = 1000;
            s.MoonJars = MoonJars.Cap - 1;
            Assert.IsTrue(MoonJars.Buy(s, 7));
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            Assert.IsTrue(MoonJars.Full(s));
            Assert.AreEqual(MoonJars.BuyCheck.Full, MoonJars.CanBuy(s, 7));
            Assert.IsFalse(MoonJars.Buy(s, 7), "the shop refuses while the shelf is full");
            Assert.AreEqual(1000 - MoonJars.Price, s.GoldenCarrots, "and keeps its price");
            Assert.IsFalse(MoonJars.GiveFree(s, 8), "a free jar has no room either");
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
        }

        [Test]
        public void One_Free_Jar_A_Day_And_A_Clock_Set_Back_Gets_None_And_The_Daily_Sources_Wait_For_Garden_Level_Fifteen()
        {
            s.GardenXp = Garden.XpForLevel(MoonJars.DailyLevel - 1);
            Assert.IsTrue(MoonJars.FreeWaiting(s, 20));
            Assert.IsFalse(MoonJars.DailyWaiting(s, 20), "the quest and the firefly night wait for all the garden's land");
            s.GardenXp = Garden.XpForLevel(MoonJars.DailyLevel);
            Assert.IsTrue(MoonJars.DailyWaiting(s, 20));
            Assert.IsTrue(MoonJars.FreeWaiting(s, 20));
            Assert.IsTrue(MoonJars.GiveFree(s, 20));
            Assert.IsFalse(MoonJars.FreeWaiting(s, 20));
            Assert.IsFalse(MoonJars.DailyWaiting(s, 20));
            Assert.IsFalse(MoonJars.GiveFree(s, 20), "one a day");
            Assert.IsFalse(MoonJars.GiveFree(s, 19), "the clock went back");
            Assert.IsTrue(MoonJars.GiveFree(s, 21));
            Assert.AreEqual(2, s.MoonJars);
        }

        [Test]
        public void The_Daily_Harvest_Quest_Pays_A_Jar_In_Place_Of_Its_Clover_And_The_Clover_When_No_Jar_Can_Come()
        {
            var harvest = new QuestDef { Id = "ngay-thu", Metric = QuestMetric.CarrotsEarned, Target = 0, Daily = true, RewardSeconds = 60, RewardCarrots = 60, RewardJars = 1 };
            var quests = new List<QuestDef> { harvest };
            Quests.RollDaily(quests, defs, s, 29, Modifiers.None);
            s.TotalEarned = s.DailyEarnedBase + s.DailyCarrotTarget * 2d; // clear of rounding in the subtraction
            Assert.AreEqual(0, Quests.JarsNow(harvest, s), "below garden level 15 the harvest pays its clover");
            Assert.IsTrue(Quests.TryClaim(harvest, defs, s, Modifiers.None, out _));
            Assert.AreEqual(0, s.MoonJars);
            Assert.AreEqual(1, s.Clovers);
            s.Clovers = 0;

            s.GardenXp = Garden.XpForLevel(MoonJars.DailyLevel);
            Quests.RollDaily(quests, defs, s, 30, Modifiers.None);
            s.TotalEarned = s.DailyEarnedBase + s.DailyCarrotTarget * 2d; // clear of rounding in the subtraction
            Assert.AreEqual(1, Quests.JarsNow(harvest, s));
            Assert.AreEqual(0, Quests.CloversNow(harvest, s));
            Assert.IsTrue(Quests.TryClaim(harvest, defs, s, Modifiers.None, out _));
            Assert.AreEqual(1, s.MoonJars);
            Assert.AreEqual(0, s.Clovers);

            // Tomorrow, after the firefly night already gave the day's free jar: the quest pays its old clover.
            Quests.RollDaily(quests, defs, s, 31, Modifiers.None);
            Assert.IsTrue(MoonJars.GiveFree(s, 31));
            s.TotalEarned = s.DailyEarnedBase + s.DailyCarrotTarget * 2d; // clear of rounding in the subtraction
            Assert.AreEqual(0, Quests.JarsNow(harvest, s), "the row shows the clover");
            Assert.AreEqual(1, Quests.CloversNow(harvest, s));
            Assert.IsTrue(Quests.TryClaim(harvest, defs, s, Modifiers.None, out _));
            Assert.AreEqual(2, s.MoonJars);
            Assert.AreEqual(1, s.Clovers);

            // A full shelf: the clover again, and nothing is lost.
            Quests.RollDaily(quests, defs, s, 32, Modifiers.None);
            s.MoonJars = MoonJars.Cap;
            s.TotalEarned = s.DailyEarnedBase + s.DailyCarrotTarget * 2d; // clear of rounding in the subtraction
            Assert.AreEqual(0, Quests.JarsNow(harvest, s));
            Assert.IsTrue(Quests.TryClaim(harvest, defs, s, Modifiers.None, out _));
            Assert.AreEqual(MoonJars.Cap, s.MoonJars);
            Assert.AreEqual(2, s.Clovers);
            Assert.AreEqual(31, s.MoonJarFreeDay, "the day's free jar is still to be had by another source");
        }

        [Test]
        public void A_Three_Star_Firefly_Night_Brings_The_Days_Free_Jar_In_Place_Of_Its_Golden_Carrot()
        {
            s.GardenXp = Garden.XpForLevel(MoonJars.DailyLevel);
            Assert.IsTrue(Fireflies.Start(s, 40));
            FirefliesResult first = Fireflies.Finish(defs, s, Modifiers.None, Fireflies.MaxScore);
            Assert.AreEqual(3, first.Stars);
            Assert.AreEqual(1, first.Jars, "the day's three-star prize is a jar");
            Assert.AreEqual(0, first.Golden, "in place of the golden carrot");
            Assert.AreEqual(1, s.MoonJars);
            Assert.AreEqual(0, s.GoldenCarrots);
            Assert.AreEqual(1, s.Fireflies.GoldenToday, "the day's three-star prize is taken");

            Assert.IsTrue(Fireflies.Start(s, 40));
            FirefliesResult second = Fireflies.Finish(defs, s, Modifiers.None, Fireflies.MaxScore);
            Assert.AreEqual(0, second.Jars);
            Assert.AreEqual(0, second.Golden);
            Assert.IsTrue(second.GoldenCapped, "one prize a day, as before");

            // The next day the harvest quest came first: the night pays its golden carrot as it always did.
            Assert.IsTrue(MoonJars.GiveFree(s, 41));
            Assert.IsTrue(Fireflies.Start(s, 41));
            FirefliesResult third = Fireflies.Finish(defs, s, Modifiers.None, Fireflies.MaxScore);
            Assert.AreEqual(0, third.Jars);
            Assert.AreEqual(1, third.Golden);
            Assert.AreEqual(2, s.MoonJars);
            Assert.AreEqual(1, s.GoldenCarrots);
        }

        [Test]
        public void Every_Tenth_Garden_Level_Brings_The_Days_Free_Jar()
        {
            s.DailyDay = 60;
            Garden.AddXp(s, Garden.XpForLevel(9));
            Assert.AreEqual(9, Garden.Level(s));
            Assert.AreEqual(0, s.MoonJars, "no jar before level 10");
            Garden.AddXp(s, Garden.XpForLevel(10) - s.GardenXp);
            Assert.AreEqual(10, Garden.Level(s));
            Assert.AreEqual(1, s.MoonJars, "level 10");
            Assert.AreEqual(60, s.MoonJarFreeDay);

            // Level 20 on the same day: the day's free jar is given, so the level brings its clover and golden carrots only.
            Garden.AddXp(s, Garden.XpForLevel(20) - s.GardenXp);
            Assert.AreEqual(20, Garden.Level(s));
            Assert.AreEqual(1, s.MoonJars);
            Assert.AreEqual(2, MoonJars.TenthLevels(9, 20), "two tenth levels crossed, still one free jar a day");

            s.DailyDay = 61;
            Garden.AddXp(s, Garden.XpForLevel(30) - s.GardenXp);
            Assert.AreEqual(2, s.MoonJars, "level 30 on a new day");
        }

        [Test]
        public void The_Button_Waits_For_Garden_Level_Three()
        {
            Assert.IsFalse(MoonJars.Shown(s));
            Garden.AddXp(s, Garden.XpForLevel(MoonJars.ShowLevel));
            Assert.IsTrue(MoonJars.Shown(s));
        }

        [Test]
        public void Jars_And_The_Days_Counts_Survive_The_Save_And_An_Old_Save_Has_None()
        {
            s.MoonJars = 4;
            s.MoonJarBuyDay = 77;
            s.MoonJarsBoughtToday = 2;
            s.MoonJarFreeDay = 76;
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(4, r.MoonJars);
            Assert.AreEqual(77, r.MoonJarBuyDay);
            Assert.AreEqual(2, r.MoonJarsBoughtToday);
            Assert.AreEqual(76, r.MoonJarFreeDay);

            string old = SaveCodec.Encode(defs, GameState.NewGame(defs));
            old = System.Text.RegularExpressions.Regex.Replace(old, ",\"moonJars\":\\{[^}]*\\}", "");
            StringAssert.DoesNotContain("moonJars", old);
            GameState fromOld = SaveCodec.Decode(defs, old).State;
            Assert.AreEqual(0, fromOld.MoonJars);
            Assert.AreEqual(-1, fromOld.MoonJarBuyDay);
            Assert.AreEqual(0, fromOld.MoonJarsBoughtToday);
            Assert.AreEqual(-1, fromOld.MoonJarFreeDay);

            // A save edited by hand cannot hold more than the shelf.
            string edited = SaveCodec.Encode(defs, s).Replace("\"held\":4", "\"held\":400");
            Assert.AreEqual(MoonJars.Cap, SaveCodec.Decode(defs, edited).State.MoonJars);
        }
    }
}

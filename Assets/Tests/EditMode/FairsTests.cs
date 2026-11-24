using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace VuonTra.Core.Tests
{
    public class FairsTests
    {
        const int Year = 2027;

        List<StationDef> defs;
        GameState s;

        [SetUp]
        public void SetUp()
        {
            defs = Fixture.Defs();
            s = GameState.NewGame(defs);
            Rabbits.GiftFirst(s, "Bông");
            Tutorial.Skip(s);
        }

        static long Day(int month, int day, int year = Year) => GiftCodes.Day(year, month, day);

        long Clamp(long day) => Seasons.Clamp(s, day);

        FairRun Fair(int month, int year = Year) => Fairs.RunOn(Day(month, Fairs.StartDay, year)).Value;

        long Weekday(FairRun run, bool weekend)
        {
            for (long d = run.Start; d <= run.End; d++)
                if (Fairs.Weekend(d) == weekend) return d;
            throw new InvalidOperationException("a ten-day fair has both");
        }

        [Test]
        public void The_Numbers_Are_The_Issues_Defaults()
        {
            Assert.AreEqual(10, Fairs.StartDay);
            Assert.AreEqual(10, Fairs.Days);
            Assert.AreEqual(3, Fairs.OrderCoins);
            Assert.AreEqual(2, Fairs.PhotoAskCoins);
            Assert.AreEqual(new[] { 0, 1, 3, 5 }, Fairs.StarCoins);
            Assert.AreEqual(1, Fairs.FishCoins[0]);
            Assert.AreEqual(4, Fairs.FishCoins[Fishing.Kinds - 1]);
            Assert.AreEqual(Fishing.Kinds, Fairs.FishCoins.Length);
            Assert.AreEqual(2, Fairs.WeekendTimes);
            Assert.AreEqual(30, Fairs.CarrotSecondsPerCoin);
            var stall = new[]
            {
                new FairStall(FairWare.Jars, 40, 1, 5), new FairStall(FairWare.Clovers, 30, 1, 10), new FairStall(FairWare.Golden, 80, 5, 4),
                new FairStall(FairWare.Lantern, 150, 1, 1), new FairStall(FairWare.Frame, 200, 1, 1), new FairStall(FairWare.Rabbit, 400, 1, 1),
            };
            Assert.AreEqual(stall, Fairs.Stall);
        }

        [Test]
        public void Eight_Fairs_Fill_The_Months_Without_A_Festival_Season()
        {
            Assert.AreEqual(8, Fairs.All.Count);
            var months = new List<int>();
            foreach (FairDef def in Fairs.All) months.Add(def.Month);
            Assert.AreEqual(new[] { 3, 4, 5, 6, 7, 8, 10, 11 }, months.ToArray());
        }

        [Test]
        public void Every_Row_Of_The_Table_Opens_On_Its_Day_And_Never_Inside_A_Festival_Season()
        {
            var ids = new HashSet<string>();
            var hexes = new HashSet<string>();
            for (int i = 0; i < Fairs.All.Count; i++)
            {
                FairDef def = Fairs.All[i];
                Assert.IsTrue(ids.Add(def.Id), "one id per fair: " + def.Id);
                Assert.IsTrue(hexes.Add(def.Hex), "one colour per fair: " + def.Hex);
                Assert.AreEqual(6, def.Hex.Length, def.Id);
                Assert.AreEqual("fair." + def.Id, def.NameKey);
                Assert.AreEqual("fair.coin." + def.Id, def.CoinKey);
                Assert.AreEqual(i, Fairs.IndexOf(def.Id));
                RabbitKind kind = RabbitKinds.Find(def.RabbitKind);
                Assert.IsNotNull(kind, def.Id + " names a rabbit kind that exists");
                Assert.AreEqual(3, kind.Tier, def.Id + "'s rabbit is Purple, tier index 3");
                Assert.AreEqual(Season.None, kind.Season);
                Assert.IsFalse(kind.NewMoonOnly);
                Assert.AreEqual(Fairs.Stall, def.Stalls, "the stall list comes with the row");
                Assert.AreEqual(i, Fairs.LanternFair(Fairs.LanternIndex(i)));
                Assert.AreEqual(i, Fairs.FrameFair(Fairs.FrameIndex(i)));
                Assert.AreEqual(i, Fairs.DyeFair(Fairs.Dye(i)));
                for (int year = 2026; year <= 2030; year++)
                {
                    long start = Fairs.StartOf(def, year);
                    Assert.IsFalse(Fairs.RunOn(start - 1) is FairRun before && before.Def == def, def.Id + " is shut the day before");
                    for (long d = start; d < start + def.Days; d++)
                    {
                        FairRun? run = Fairs.RunOn(d);
                        Assert.IsTrue(run.HasValue && run.Value.Def == def && run.Value.Index == i && run.Value.Start == start, def.Id + " is open on day " + (d - start + 1));
                    }
                    Assert.IsFalse(Fairs.RunOn(start + def.Days).HasValue, def.Id + " is shut the day after");
                }
            }
            for (long d = Day(1, 1, 2026); d < Day(1, 1, 2031); d++)
                if (Fairs.RunOn(d).HasValue)
                    Assert.AreEqual(Season.None, Seasons.SeasonOf(d), "no fair on " + Seasons.DateOf(d).ToString("yyyy-MM-dd") + " inside a festival season");
        }

        [Test]
        public void The_Fair_Opens_On_The_Clamped_Day()
        {
            FairRun may = Fair(5);
            Clamp(may.Start + 2);
            Assert.IsTrue(Fairs.Open(s, may.Start - 3).HasValue, "a clock set back before the fair still sees the fair the garden has reached");
            Assert.AreEqual(may.Start, Fairs.Open(s, may.Start - 3).Value.Start);
            Assert.IsFalse(Fairs.Open(s, may.End + 1).HasValue);
            Assert.IsFalse(Fairs.Open(s, Day(9, 15)).HasValue, "none in Mid-Autumn");
            FairRun? next = Fairs.Next(Day(8, 25));
            Assert.AreEqual(Day(10, 10), next.Value.Start, "after August's fair and the September season, October's");
        }

        [Test]
        public void Drops_Count_The_Issues_Amounts_And_Saturdays_And_Sundays_Pay_Double()
        {
            FairRun june = Fair(6);
            long weekday = Weekday(june, false), weekend = Weekday(june, true);
            Clamp(weekday);
            Assert.AreEqual(Fairs.OrderCoins, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.OrderCoins));
            Assert.AreEqual(5, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.ForStars(3)));
            Assert.AreEqual(3, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.ForStars(2)));
            Assert.AreEqual(1, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.ForStars(1)));
            Assert.AreEqual(0, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.ForStars(0)));
            Assert.AreEqual(1, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.ForFish(0)));
            Assert.AreEqual(4, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.ForFish(Fishing.Kinds - 1)));
            Assert.AreEqual(2, Fairs.Earn(defs, s, Modifiers.None, weekday, Fairs.PhotoAskCoins));
            Assert.AreEqual(19, s.Fair.Coins);
            Assert.AreEqual(1, Fairs.Times(weekday));
            Clamp(weekend);
            Assert.AreEqual(2, Fairs.Times(weekend));
            Assert.AreEqual(6, Fairs.Earn(defs, s, Modifiers.None, weekend, Fairs.OrderCoins), "a weekend order pays double");
            Assert.AreEqual(25, s.Fair.Coins);
            Assert.AreEqual(0, Fairs.Earn(defs, s, Modifiers.None, Day(7, 1), Fairs.OrderCoins), "nothing drops between fairs");
        }

        [Test]
        public void Every_Stall_Has_Its_Cap()
        {
            FairRun july = Fair(7);
            long day = Clamp(july.Start);
            Fairs.Roll(defs, s, Modifiers.None, day);
            s.Fair.Coins = 10_000;
            int held = s.MoonJars + s.Clovers;
            for (int stall = 0; stall < Fairs.Stall.Length; stall++)
            {
                FairStall ware = Fairs.Stall[stall];
                for (int n = 0; n < ware.Max; n++)
                    Assert.IsTrue(Fairs.Buy(defs, s, Modifiers.None, day, stall, "Hội").Ok, ware.Ware + " #" + (n + 1));
                FairBuy over = Fairs.Buy(defs, s, Modifiers.None, day, stall, "Hội");
                Assert.IsFalse(over.Ok, ware.Ware + " stops at " + ware.Max);
                Assert.IsTrue(over.Check == FairBuyCheck.SoldOut || over.Check == FairBuyCheck.Owned);
                Assert.AreEqual(ware.Max, Fairs.Bought(s, stall));
            }
            int spent = 0;
            foreach (FairStall ware in Fairs.Stall) spent += ware.Price * ware.Max;
            Assert.AreEqual(10_000 - spent, s.Fair.Coins);
            Assert.AreEqual(held + 5 + 10, s.MoonJars + s.Clovers, "five jars (clovers while the shelf is hidden) and ten clovers");
            Assert.AreEqual(20, s.GoldenCarrots, "four bags of five golden carrots");
            Assert.AreEqual(Fairs.LanternIndex(july.Index), s.LanternColor, "the fair lantern colour is hung at once");
            Assert.AreEqual(july.Def.Hex, MonthPass.LanternHex(s.LanternColor));
            Assert.AreEqual(Fairs.FrameIndex(july.Index), s.PhotoFrame, "the fair frame goes on at once");
            Rabbit fair = s.Rabbits[s.Rabbits.Count - 1];
            Assert.AreEqual(3, fair.Tier, "the fair rabbit is Purple");
            Assert.AreEqual(july.Def.RabbitKind, fair.Kind);
            Assert.AreEqual(Fairs.Dye(july.Index), fair.Dye, "in the fair's own tint");
            Assert.AreEqual(july.Def.Hex, Fairs.DyeHex(fair.Dye));
        }

        [Test]
        public void The_Stall_Asks_For_Enough_Coins_And_An_Open_Fair()
        {
            FairRun march = Fair(3);
            long day = Clamp(march.Start);
            Fairs.Roll(defs, s, Modifiers.None, day);
            s.Fair.Coins = 39;
            Assert.AreEqual(FairBuyCheck.Short, Fairs.Buy(defs, s, Modifiers.None, day, 0, "H").Check);
            Assert.AreEqual(FairBuyCheck.NoStall, Fairs.CanBuy(s, day, 99));
            s.Fair.Coins = 40;
            Assert.IsTrue(Fairs.Buy(defs, s, Modifiers.None, day, 0, "H").Ok);
            Assert.AreEqual(0, s.Fair.Coins);
            Assert.AreEqual(FairBuyCheck.Closed, Fairs.CanBuy(s, Day(3, 25), 0));
        }

        [Test]
        public void Fair_Lanterns_And_Frames_Can_Be_Worn_Again_After_Taking_Them_Off()
        {
            FairRun aug = Fair(8);
            long day = Clamp(aug.Start);
            Fairs.Roll(defs, s, Modifiers.None, day);
            s.Fair.Coins = 350;
            Assert.IsTrue(Fairs.Buy(defs, s, Modifiers.None, day, 3, "H").Ok);
            Assert.IsTrue(Fairs.Buy(defs, s, Modifiers.None, day, 4, "H").Ok);
            Assert.IsTrue(MonthPass.WearFrame(s, MonthPass.PlainFrame));
            s.LanternColor = 0;
            Assert.IsTrue(Fairs.HangLantern(s, aug.Index));
            Assert.IsTrue(Fairs.WearFrame(s, aug.Index));
            Assert.IsFalse(Fairs.HangLantern(s, (aug.Index + 1) % Fairs.All.Count), "a fair colour not bought stays in the stall");
            Assert.IsFalse(Fairs.WearFrame(s, (aug.Index + 1) % Fairs.All.Count));
        }

        [Test]
        public void At_The_End_Leftover_Coins_Turn_Into_Carrots_Once_With_A_Letter()
        {
            FairRun oct = Fair(10);
            Clamp(oct.Start);
            s.Fair.Coins = 0;
            Fairs.Earn(defs, s, Modifiers.None, oct.Start, 7);
            int coins = s.Fair.Coins;
            Assert.AreEqual(Fairs.Weekend(oct.Start) ? 14 : 7, coins);
            BigNum income = Economy.IncomePerSecond(defs, s) * (double)(coins * Fairs.CarrotSecondsPerCoin);
            BigNum floor = new BigNum(coins * Fairs.CarrotSecondsPerCoin);
            long after = Clamp(oct.End + 2);
            Letter letter = Fairs.Roll(defs, s, Modifiers.None, after);
            Assert.IsNotNull(letter, "the fair's end leaves a letter");
            Assert.AreEqual(LetterKind.FairEnd, letter.Kind);
            Assert.AreEqual(coins, letter.FairCoins);
            Assert.AreEqual(income > floor ? income : floor, letter.Coins, "each coin is thirty seconds of income");
            Assert.AreEqual(0, s.Fair.Coins);
            Assert.AreEqual(oct.Start, s.Fair.Done);
            Assert.IsNull(Fairs.Roll(defs, s, Modifiers.None, after + 1), "only once");
            Assert.AreEqual(1, s.Letters.FindAll(l => l.Kind == LetterKind.FairEnd).Count);
            BigNum before = s.Coins;
            Assert.IsNotNull(Mailbox.Claim(s, letter.Id, after));
            Assert.AreEqual(before + letter.Coins, s.Coins);
            Assert.AreEqual(0, s.GoldenCarrots, "carrots, nothing golden");
            Assert.IsNull(Mailbox.Claim(s, letter.Id, after));
        }

        [Test]
        public void A_Fair_That_Ends_With_Nothing_Left_Sends_No_Letter()
        {
            FairRun apr = Fair(4);
            Clamp(apr.Start);
            Fairs.Roll(defs, s, Modifiers.None, apr.Start);
            Assert.IsNull(Fairs.Roll(defs, s, Modifiers.None, Clamp(apr.End + 1)));
            Assert.AreEqual(0, s.Letters.Count);
            Assert.AreEqual(apr.Start, s.Fair.Done);
        }

        [Test]
        public void Rolling_The_Clock_Back_Never_Reopens_An_Old_Fair()
        {
            FairRun may = Fair(5);
            Clamp(may.Start);
            Fairs.Earn(defs, s, Modifiers.None, may.Start, 3);
            Clamp(may.End + 1);
            Fairs.Roll(defs, s, Modifiers.None, may.End + 1);
            Assert.AreEqual(0, Fairs.Earn(defs, s, Modifiers.None, may.Start + 1, 3), "the clock set back into May's fair earns nothing");
            Assert.IsFalse(Fairs.Open(s, may.Start + 1).HasValue);
            var fresh = GameState.NewGame(defs);
            fresh.Fair.Done = may.Start;
            Assert.IsFalse(Fairs.Open(fresh, may.Start + 4).HasValue, "even with a clamp that was reset, a fair already settled stays shut");
            Assert.IsTrue(Fairs.Open(fresh, Fair(5, Year + 1).Start).HasValue, "next year's fair opens");
        }

        [Test]
        public void The_Fair_Round_Trips_And_An_Old_Save_Has_No_Fair()
        {
            FairRun nov = Fair(11);
            long day = Clamp(nov.Start + 1);
            Fairs.Earn(defs, s, Modifiers.None, day, 300);
            Assert.IsTrue(Fairs.Buy(defs, s, Modifiers.None, day, 0, "H").Ok);
            Assert.IsTrue(Fairs.Buy(defs, s, Modifiers.None, day, 3, "H").Ok);
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.AreEqual(s.Fair.Start, r.Fair.Start);
            Assert.AreEqual(s.Fair.Done, r.Fair.Done);
            Assert.AreEqual(s.Fair.Coins, r.Fair.Coins);
            Assert.AreEqual(s.Fair.Bought, r.Fair.Bought);
            Assert.AreEqual(s.Fair.Lanterns, r.Fair.Lanterns);
            Assert.AreEqual(s.LanternColor, r.LanternColor, "the fair colour stays hung");

            Clamp(nov.End + 1);
            Letter letter = Fairs.Roll(defs, s, Modifiers.None, nov.End + 1);
            GameState m = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Letter back = m.Letters.Find(l => l.Id == letter.Id);
            Assert.IsNotNull(back);
            Assert.AreEqual(LetterKind.FairEnd, back.Kind);
            Assert.AreEqual(letter.Coins, back.Coins);
            Assert.AreEqual(letter.FairCoins, back.FairCoins);
            Assert.AreEqual(letter.Fair, back.Fair);
            Assert.AreEqual(nov.Start, m.Fair.Done);

            GameState old = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"decor\":{\"lantern\":3}}").State;
            Assert.AreEqual(-1, old.Fair.Start);
            Assert.AreEqual(-1, old.Fair.Done);
            Assert.AreEqual(0, old.Fair.Coins);
            Assert.AreEqual(0, old.Fair.Lanterns);
            Assert.AreEqual(3, old.LanternColor);

            GameState bad = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"decor\":{\"lantern\":" + Fairs.LanternIndex(2) + "},"
                + "\"fair\":{\"start\":" + (nov.Start + 3) + ",\"coins\":-9,\"bought\":[99,\"x\"],\"lanterns\":-1,\"frames\":1}}").State;
            Assert.AreEqual(-1, bad.Fair.Start, "a start that is no fair's first day is no fair");
            Assert.AreEqual(0, bad.Fair.Coins);
            Assert.AreEqual(0, bad.Fair.Lanterns);
            Assert.AreEqual(1, bad.Fair.Frames);
            Assert.AreEqual(0, bad.LanternColor, "a fair colour the garden does not own is not hung");

            GameState capped = SaveCodec.Decode(defs, "{\"version\":14,\"stations\":[],\"fair\":{\"start\":" + nov.Start + ",\"coins\":12,\"bought\":[99,2]}}").State;
            Assert.AreEqual(Fairs.Stall[0].Max, capped.Fair.Bought[0], "bought counts stay under the stall's cap");
            Assert.AreEqual(2, capped.Fair.Bought[1]);
            Assert.AreEqual(12, capped.Fair.Coins);
        }

        [Test]
        public void The_Fair_Card_Waits_Once_Per_Fair_And_The_Save_Keeps_That()
        {
            FairRun june = Fair(6);
            long day = Clamp(june.Start + 1);
            Assert.IsTrue(Fairs.CardWaiting(s, day));
            Fairs.SeeCard(s, day);
            Assert.IsFalse(Fairs.CardWaiting(s, day));
            GameState r = SaveCodec.Decode(defs, SaveCodec.Encode(defs, s)).State;
            Assert.IsFalse(Fairs.CardWaiting(r, day), "a reload does not show it again");
            Assert.IsFalse(Fairs.CardWaiting(s, Clamp(june.End + 3)), "no card between fairs");
            Assert.IsTrue(Fairs.CardWaiting(s, Clamp(Fair(7).Start)), "July's fair has its own");
        }

        [Test]
        public void The_Fair_Letter_Takes_Kind_Seven_After_Mains_Kinds()
        {
            Assert.AreEqual(4, (int)LetterKind.MonthPass);
            Assert.AreEqual(7, (int)LetterKind.FairEnd);
        }
    }
}

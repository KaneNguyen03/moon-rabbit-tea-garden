using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// Moon jars (#64): an hour of the garden's income kept in a jar, opened from the purse whenever the player likes.
    /// A jar is worth what an hour earns at the moment it is opened, with the charms and the speed but never time
    /// away's own bonus, so it stays a fair prize however far the garden has come. The shop sells three a day for
    /// golden carrots from garden level 3. Every tenth garden level brings one free, and from garden level 15 (one short
    /// of the last plots) the daily harvest quest and a three-star firefly night do too, each in place of the prize it
    /// used to give; the free sources together give one a day at most. Nine fit on the shelf.
    /// </summary>
    public static class MoonJars
    {
        /// <summary>Seconds of income in one jar.</summary>
        public const double Seconds = 3600d;
        /// <summary>The most jars the shelf holds.</summary>
        public const int Cap = 9;
        /// <summary>What the shop asks for one, in golden carrots, and how many it sells a day.</summary>
        public const int Price = 12, BuyPerDay = 3;
        /// <summary>Jars the free sources give in one day, all together.</summary>
        public const int FreePerDay = 1;
        /// <summary>The garden level the purse starts showing its jars at: nothing strange for a new player.</summary>
        public const int ShowLevel = 3;
        /// <summary>Every garden level that divides by this brings the day's free jar.</summary>
        public const int LevelStep = 10;
        /// <summary>
        /// The garden level the daily sources (the harvest quest, the firefly night) start paying jars at: the level the
        /// last plots needed until R 7 moved to 16 (#83). Earlier, an hour of a small income is many times the purse: from the start, the harvest's jar
        /// at minute two brought all twelve plots a day early, and from level 10 the casual player's second-morning jar
        /// still did (EconomySimulationTests). Before it they pay their clover and golden carrot as they always did.
        /// </summary>
        public const int DailyLevel = 15;

        public enum BuyCheck { Ok, Full, SoldOut, Short }

        public static bool Shown(GameState s) => Garden.Level(s) >= ShowLevel;

        public static bool Full(GameState s) => s.MoonJars >= Cap;

        /// <summary>An hour of the income of this moment, times the charms and the speed.</summary>
        public static BigNum Value(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * Seconds);

        /// <summary>Opens one jar into the purse. The carrots it paid, nothing when there is no jar. A gift: it does not count as earned.</summary>
        public static BigNum Open(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            if (s.MoonJars <= 0) return BigNum.Zero;
            BigNum paid = Value(defs, s, m);
            s.MoonJars--;
            s.Coins += paid;
            return paid;
        }

        /// <summary>Jars the shop has sold on <paramref name="day"/>. A day before the last sale (a clock turned back) counts as sold out.</summary>
        public static int BoughtOn(GameState s, long day)
        {
            if (day == s.MoonJarBuyDay) return s.MoonJarsBoughtToday;
            return day < s.MoonJarBuyDay ? BuyPerDay : 0;
        }

        public static int BuyLeft(GameState s, long day) => Math.Max(0, BuyPerDay - BoughtOn(s, day));

        /// <summary>Whether the shop sells a jar now, and if not, why: the shelf is full, the day's three are sold, or the purse is short.</summary>
        public static BuyCheck CanBuy(GameState s, long day)
        {
            if (Full(s)) return BuyCheck.Full;
            if (BuyLeft(s, day) <= 0) return BuyCheck.SoldOut;
            return s.GoldenCarrots < Price ? BuyCheck.Short : BuyCheck.Ok;
        }

        /// <summary>Buys one jar at the shop. False, with nothing taken, unless <see cref="CanBuy"/> says yes.</summary>
        public static bool Buy(GameState s, long day)
        {
            if (CanBuy(s, day) != BuyCheck.Ok) return false;
            if (day != s.MoonJarBuyDay)
            {
                s.MoonJarBuyDay = day;
                s.MoonJarsBoughtToday = 0;
            }
            s.GoldenCarrots -= Price;
            s.MoonJarsBoughtToday++;
            s.MoonJars++;
            return true;
        }

        /// <summary>True while the day's free jar can still come: it has not come today and the shelf has room. A clock set back gets none.</summary>
        public static bool FreeWaiting(GameState s, long day) => !Full(s) && day > s.MoonJarFreeDay;

        /// <summary>The same for the daily sources, which also wait for garden level <see cref="DailyLevel"/>.</summary>
        public static bool DailyWaiting(GameState s, long day) => FreeWaiting(s, day) && Garden.Level(s) >= DailyLevel;

        /// <summary>
        /// Gives the day's free jar. False when it came today already or the shelf is full; the source then pays what it
        /// paid before jars (the quest its clover, the firefly night its golden carrot, the level nothing more).
        /// </summary>
        public static bool GiveFree(GameState s, long day)
        {
            if (!FreeWaiting(s, day)) return false;
            s.MoonJars++;
            s.MoonJarFreeDay = day;
            return true;
        }

        /// <summary>How many levels in (from, to] divide by <see cref="LevelStep"/>.</summary>
        public static int TenthLevels(int from, int to)
        {
            int n = 0;
            for (int level = from + 1; level <= to; level++) if (level % LevelStep == 0) n++;
            return n;
        }
    }
}

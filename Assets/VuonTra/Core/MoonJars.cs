using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class MoonJars
    {
        public const double Seconds = 3600d;
        public const int Cap = 9;
        public const int Price = 12, BuyPerDay = 3;
        public const int FreePerDay = 1;
        public const int ShowLevel = 3;
        public const int LevelStep = 10;
        public const int DailyLevel = 15;

        public enum BuyCheck { Ok, Full, SoldOut, Short }

        public static bool Shown(GameState s) => Garden.Level(s) >= ShowLevel;

        public static bool Full(GameState s) => s.MoonJars >= Cap;

        public static BigNum Value(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * Seconds);

        public static BigNum Open(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            if (s.MoonJars <= 0) return BigNum.Zero;
            BigNum paid = Value(defs, s, m);
            s.MoonJars--;
            s.JarsOpened++;
            s.Coins += paid;
            return paid;
        }

        public static int BoughtOn(GameState s, long day)
        {
            if (day == s.MoonJarBuyDay) return s.MoonJarsBoughtToday;
            return day < s.MoonJarBuyDay ? BuyPerDay : 0;
        }

        public static int BuyLeft(GameState s, long day) => Math.Max(0, BuyPerDay - BoughtOn(s, day));

        public static BuyCheck CanBuy(GameState s, long day)
        {
            if (Full(s)) return BuyCheck.Full;
            if (BuyLeft(s, day) <= 0) return BuyCheck.SoldOut;
            return s.GoldenCarrots < Price ? BuyCheck.Short : BuyCheck.Ok;
        }

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

        public static bool FreeWaiting(GameState s, long day) => !Full(s) && day > s.MoonJarFreeDay;

        public static bool DailyWaiting(GameState s, long day) => FreeWaiting(s, day) && Garden.Level(s) >= DailyLevel;

        public static bool GiveFree(GameState s, long day)
        {
            if (!FreeWaiting(s, day)) return false;
            s.MoonJars++;
            s.MoonJarFreeDay = day;
            return true;
        }

        public static int TenthLevels(int from, int to)
        {
            int n = 0;
            for (int level = from + 1; level <= to; level++) if (level % LevelStep == 0) n++;
            return n;
        }
    }
}

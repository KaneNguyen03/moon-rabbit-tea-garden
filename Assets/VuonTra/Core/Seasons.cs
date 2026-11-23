using System;

namespace VuonTra.Core
{
    public enum Season { None, MidAutumn, Tet, Christmas }

    public static class Seasons
    {
        public static DateTime DateOf(long day) => DateTimeOffset.FromUnixTimeSeconds(day * 86400L).UtcDateTime.Date;

        public static Season SeasonOf(long day)
        {
            DateTime d = DateOf(day);
            int m = d.Month, dd = d.Day;
            if (m == 9) return Season.MidAutumn;
            if (m == 12) return Season.Christmas;
            if ((m == 1 && dd >= 15) || m == 2) return Season.Tet;
            return Season.None;
        }

        public static bool IsLastDay(long day) => SeasonOf(day) != Season.None && SeasonOf(day + 1) != SeasonOf(day);

        public static long Clamp(GameState s, long day)
        {
            if (s == null) return day;
            if (day > s.MaxDaySeen) s.MaxDaySeen = day;
            if (s.FirstDaySeen < 0) s.FirstDaySeen = s.MaxDaySeen;
            return s.MaxDaySeen;
        }

        public static long DaysPlayed(GameState s) => s == null || s.FirstDaySeen < 0 || s.MaxDaySeen < s.FirstDaySeen ? 0 : s.MaxDaySeen - s.FirstDaySeen;

        public static bool ShouldGreet(GameState s, long day)
        {
            Season now = SeasonOf(day);
            if (now == Season.None) return false;
            if (s.SeasonGreetedDay >= 0 && SeasonOf(s.SeasonGreetedDay) == now && SeasonYear(s.SeasonGreetedDay) == SeasonYear(day)) return false;
            s.SeasonGreetedDay = day;
            return true;
        }

        static int SeasonYear(long day) => DateOf(day).Year;

        public static RabbitKind KindOf(Season season)
        {
            if (season == Season.None) return null;
            System.Collections.Generic.IReadOnlyList<RabbitKind> all = RabbitKinds.All;
            for (int i = 0; i < all.Count; i++) if (all[i].Season == season) return all[i];
            return null;
        }

        public static bool HasSeasonRabbit(GameState s, Season season)
        {
            RabbitKind kind = KindOf(season);
            if (kind == null) return false;
            foreach (Rabbit r in s.Rabbits) if (Rabbits.KindOf(r) == kind.Id) return true;
            return false;
        }

        public static bool SeasonRabbitInTrade(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s, Season season)
        {
            RabbitKind kind = KindOf(season);
            if (kind == null) return false;
            foreach (Rabbit r in s.Rabbits) if (Rabbits.KindOf(r) == kind.Id && Rabbits.InTrade(defs, r)) return true;
            return false;
        }

        public const double GoldenCallShare = 0.5;
        public const double VisitorShare = 1d / 3d;
    }
}

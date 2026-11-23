using System;

namespace VuonTra.Core
{
    /// <summary>A stretch of the year a kind of rabbit belongs to (#8). None is every day.</summary>
    public enum Season { None, MidAutumn, Tet, Christmas }

    /// <summary>
    /// The seasons by the calendar on the device, counted in the same day numbers the daily quests use, and the
    /// guard against a clock turned back: the largest day a save has seen is the day the game believes in.
    /// </summary>
    public static class Seasons
    {
        /// <summary>The day number's calendar date: day numbers count local midnights, so the UTC date of that instant is the local date.</summary>
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

        /// <summary>True on the last day of a season: tomorrow is out of it.</summary>
        public static bool IsLastDay(long day) => SeasonOf(day) != Season.None && SeasonOf(day + 1) != SeasonOf(day);

        /// <summary>
        /// The day the game goes by: never earlier than the latest day this save has seen, so a clock set back does
        /// not reopen a season that has passed or pay a gift again. Records the day as it goes.
        /// </summary>
        public static long Clamp(GameState s, long day)
        {
            if (s == null) return day;
            if (day > s.MaxDaySeen) s.MaxDaySeen = day;
            return s.MaxDaySeen;
        }

        /// <summary>
        /// Whether a season's greeting is due today: once per season per year, the first day the game sees of it.
        /// Marks the greeting as given. False out of season or when this season has been greeted already.
        /// </summary>
        public static bool ShouldGreet(GameState s, long day)
        {
            Season now = SeasonOf(day);
            if (now == Season.None) return false;
            if (s.SeasonGreetedDay >= 0 && SeasonOf(s.SeasonGreetedDay) == now && SeasonYear(s.SeasonGreetedDay) == SeasonYear(day)) return false;
            s.SeasonGreetedDay = day;
            return true;
        }

        /// <summary>The year a season's day belongs to; Tết's February days count with its January.</summary>
        static int SeasonYear(long day) => DateOf(day).Year;

        /// <summary>The kind that belongs to a season, or null.</summary>
        public static RabbitKind KindOf(Season season)
        {
            if (season == Season.None) return null;
            System.Collections.Generic.IReadOnlyList<RabbitKind> all = RabbitKinds.All;
            for (int i = 0; i < all.Count; i++) if (all[i].Season == season) return all[i]; // by index: a foreach boxes the enumerator (#97)
            return null;
        }

        /// <summary>True when one of the player's rabbits is of the season's kind.</summary>
        public static bool HasSeasonRabbit(GameState s, Season season)
        {
            RabbitKind kind = KindOf(season);
            if (kind == null) return false;
            foreach (Rabbit r in s.Rabbits) if (Rabbits.KindOf(r) == kind.Id) return true;
            return false;
        }

        /// <summary>True when a rabbit of the season's kind works at a station of its own trade.</summary>
        public static bool SeasonRabbitInTrade(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s, Season season)
        {
            RabbitKind kind = KindOf(season);
            if (kind == null) return false;
            foreach (Rabbit r in s.Rabbits) if (Rabbits.KindOf(r) == kind.Id && Rabbits.InTrade(defs, r)) return true;
            return false;
        }

        /// <summary>The share of golden calls at the season kind's tier that bring the season kind while its season runs.</summary>
        public const double GoldenCallShare = 0.5;
        /// <summary>The share of shop visitors that are the season kind while its season runs.</summary>
        public const double VisitorShare = 1d / 3d;
    }
}

using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum ReminderKind { SeasonBegins, DailyGift, TrayFull, MissYou }

    public struct Reminder
    {
        public long AtUnixSeconds;
        public ReminderKind Kind;
        public string Key;
        public Season Season;
    }

    public static class Reminders
    {
        public const int MaxCount = 4;
        public const long MinGapSeconds = 4 * 3600;
        public const int MorningHour = 9;
        public const int NightHour = 21;
        public const long MissYouSeconds = 3 * 86400;

        const int SeasonSearchDays = 400;

        public static List<Reminder> Plan(GameState s, long nowUnixSeconds, int localOffsetSeconds, Modifiers mods, bool on)
        {
            var result = new List<Reminder>();
            if (!on || s == null) return result;

            long today = LocalDay(nowUnixSeconds, localOffsetSeconds);
            long believed = Math.Max(today, s.MaxDaySeen);

            var candidates = new List<Reminder>(MaxCount);

            long season = NextSeasonStart(s, believed, nowUnixSeconds, localOffsetSeconds);
            if (season >= 0)
                candidates.Add(new Reminder
                {
                    Kind = ReminderKind.SeasonBegins, Key = "remind.season", Season = Seasons.SeasonOf(season),
                    AtUnixSeconds = Morning(season, localOffsetSeconds)
                });

            long giftDay = Math.Max(believed, s.GiftDay + 1);
            if (Morning(giftDay, localOffsetSeconds) <= nowUnixSeconds) giftDay++;
            candidates.Add(new Reminder { Kind = ReminderKind.DailyGift, Key = "remind.gift", AtUnixSeconds = Morning(giftDay, localOffsetSeconds) });

            double cap = mods.AwayCapSeconds > 0d ? mods.AwayCapSeconds : Simulation.AwayCapSeconds;
            long full = s.LastSeenUnixSeconds + (long)Math.Ceiling(cap);
            if (full > nowUnixSeconds)
                candidates.Add(new Reminder { Kind = ReminderKind.TrayFull, Key = "remind.tray", AtUnixSeconds = Daytime(full, localOffsetSeconds) });

            long missed = Math.Max(s.LastSeenUnixSeconds, nowUnixSeconds - MissYouSeconds + 1) + MissYouSeconds;
            candidates.Add(new Reminder { Kind = ReminderKind.MissYou, Key = "remind.missyou", AtUnixSeconds = Daytime(missed, localOffsetSeconds) });

            candidates.Sort((a, b) => a.Kind != b.Kind ? a.Kind.CompareTo(b.Kind) : a.AtUnixSeconds.CompareTo(b.AtUnixSeconds));
            foreach (Reminder c in candidates)
            {
                if (result.Count >= MaxCount) break;
                bool clear = true;
                foreach (Reminder r in result)
                    if (Math.Abs(r.AtUnixSeconds - c.AtUnixSeconds) < MinGapSeconds) { clear = false; break; }
                if (clear) result.Add(c);
            }
            result.Sort((a, b) => a.AtUnixSeconds.CompareTo(b.AtUnixSeconds));
            return result;
        }

        public static long LocalDay(long unixSeconds, int localOffsetSeconds) => (long)Math.Floor((unixSeconds + (double)localOffsetSeconds) / 86400d);

        public static long Morning(long day, int localOffsetSeconds) => day * 86400L + MorningHour * 3600L - localOffsetSeconds;

        static long Daytime(long unixSeconds, int localOffsetSeconds)
        {
            long day = LocalDay(unixSeconds, localOffsetSeconds);
            long secondOfDay = unixSeconds + localOffsetSeconds - day * 86400L;
            if (secondOfDay < MorningHour * 3600L) return Morning(day, localOffsetSeconds);
            if (secondOfDay >= NightHour * 3600L) return Morning(day + 1, localOffsetSeconds);
            return unixSeconds;
        }

        static long NextSeasonStart(GameState s, long fromDay, long nowUnixSeconds, int localOffsetSeconds)
        {
            for (long d = fromDay; d < fromDay + SeasonSearchDays; d++)
            {
                Season season = Seasons.SeasonOf(d);
                if (season == Season.None || Seasons.SeasonOf(d - 1) == season) continue;
                if (Morning(d, localOffsetSeconds) <= nowUnixSeconds) continue;
                if (s.SeasonGreetedDay >= d) continue;
                return d;
            }
            return -1;
        }
    }
}

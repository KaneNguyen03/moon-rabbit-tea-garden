using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// What a reminder is about, in order of importance: when two would come within <see cref="Reminders.MinGapSeconds"/>,
    /// the one listed first is kept. A season's first day comes once a year, the gift every day, the tray every visit,
    /// and "we miss you" only matters when nothing else has brought the player back.
    /// </summary>
    public enum ReminderKind { SeasonBegins, DailyGift, TrayFull, MissYou }

    /// <summary>One reminder the device shows by itself: when, what about, and the text key (with the season for its name).</summary>
    public struct Reminder
    {
        public long AtUnixSeconds;
        public ReminderKind Kind;
        public string Key;
        /// <summary>The season that begins, for <see cref="ReminderKind.SeasonBegins"/>; None otherwise.</summary>
        public Season Season;
    }

    /// <summary>
    /// The device reminders (#65), planned each time the game goes to the background: the tray full and the rabbits
    /// rested at the away cap, the daily gift and the new shop visitor at nine the next morning, a season's first
    /// morning at nine, and "we haven't seen you for a while" three days on. At most four, never two within four
    /// hours, none in the night (21:00 to 9:00 waits for nine), and none at all while the switch is off.
    /// Times are the player's own clock: <c>localOffsetSeconds</c> is the zone's offset from UTC right now.
    /// </summary>
    public static class Reminders
    {
        public const int MaxCount = 4;
        public const long MinGapSeconds = 4 * 3600;
        /// <summary>The local hour the morning reminders come at, and the end of the quiet night.</summary>
        public const int MorningHour = 9;
        /// <summary>From this local hour on it is night: a reminder then waits for the morning.</summary>
        public const int NightHour = 21;
        public const long MissYouSeconds = 3 * 86400;

        /// <summary>How far ahead a season's first day is looked for: a little over a year.</summary>
        const int SeasonSearchDays = 400;

        public static List<Reminder> Plan(GameState s, long nowUnixSeconds, int localOffsetSeconds, Modifiers mods, bool on)
        {
            var result = new List<Reminder>();
            if (!on || s == null) return result;

            long today = LocalDay(nowUnixSeconds, localOffsetSeconds);
            // The day the game believes in: a clock set back does not bring back a gift or a season already had.
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

            // The most important first; each one after it only if it keeps its distance from those already in.
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

        /// <summary>The local calendar day of an instant, counted the way the daily things count it (local midnights).</summary>
        public static long LocalDay(long unixSeconds, int localOffsetSeconds) => (long)Math.Floor((unixSeconds + (double)localOffsetSeconds) / 86400d);

        /// <summary>Nine in the morning of a local day, as an instant.</summary>
        public static long Morning(long day, int localOffsetSeconds) => day * 86400L + MorningHour * 3600L - localOffsetSeconds;

        /// <summary>The instant itself in the day; in the night, the next nine in the morning.</summary>
        static long Daytime(long unixSeconds, int localOffsetSeconds)
        {
            long day = LocalDay(unixSeconds, localOffsetSeconds);
            long secondOfDay = unixSeconds + localOffsetSeconds - day * 86400L;
            if (secondOfDay < MorningHour * 3600L) return Morning(day, localOffsetSeconds);
            if (secondOfDay >= NightHour * 3600L) return Morning(day + 1, localOffsetSeconds);
            return unixSeconds;
        }

        /// <summary>The next first day of a season whose nine o'clock is still ahead and that has not been greeted; -1 when none.</summary>
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

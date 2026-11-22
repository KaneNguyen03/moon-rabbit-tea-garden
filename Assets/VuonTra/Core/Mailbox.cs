using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>What a letter in the mailbox is for.</summary>
    public enum LetterKind
    {
        /// <summary>The save on the device could not be read and the garden began again: a gift to say sorry.</summary>
        MakeUp,
        /// <summary>A season came and went while the player was away, its banner never seen.</summary>
        Season
    }

    /// <summary>One unclaimed gift in the mailbox. What it pays follows from its kind, never from the save.</summary>
    public sealed class Letter
    {
        public string Id;
        public LetterKind Kind;
        /// <summary>The season a season letter is for; None otherwise.</summary>
        public Season Season;
        /// <summary>The day the letter came.</summary>
        public long Day;

        public int Golden => Kind == LetterKind.MakeUp ? Mailbox.MakeUpGolden : Mailbox.SeasonGolden;
        public int Clovers => Kind == LetterKind.MakeUp ? Mailbox.MakeUpClovers : 0;
    }

    /// <summary>
    /// The mailbox (#68): gifts the game writes itself, with no server. Ten letters at most (a new one lets the oldest
    /// go), each kept thirty days. Days are the clamped day the game goes by, and the box only looks at days after the
    /// last it looked at, so a clock set back brings nothing.
    /// </summary>
    public static class Mailbox
    {
        public const int MaxLetters = 10;
        /// <summary>A letter can be claimed until this many days after it came, that day included.</summary>
        public const int KeepDays = 30;
        /// <summary>The most days back a return looks for missed seasons.</summary>
        public const int LookBackDays = 365;
        /// <summary>The make-up gift for a save that could not be read.</summary>
        public const int MakeUpGolden = 10, MakeUpClovers = 3;
        /// <summary>The gift for a season missed altogether.</summary>
        public const int SeasonGolden = 5;

        public static bool Expired(Letter letter, long day) => day > letter.Day + KeepDays;

        /// <summary>Days left to claim, 0 on the last day.</summary>
        public static int DaysLeft(Letter letter, long day) => (int)Math.Max(0L, letter.Day + KeepDays - day);

        /// <summary>The letters that can still be claimed, oldest first; expired ones are let go.</summary>
        public static IReadOnlyList<Letter> Open(GameState s, long day)
        {
            s.Letters.RemoveAll(l => Expired(l, day));
            return s.Letters;
        }

        static bool Add(GameState s, Letter letter)
        {
            foreach (Letter l in s.Letters) if (l.Id == letter.Id) return false;
            while (s.Letters.Count >= MaxLetters) s.Letters.RemoveAt(0);
            s.Letters.Add(letter);
            return true;
        }

        /// <summary>The save could not be read: a make-up letter, one a day at most. True when one was written.</summary>
        public static bool Compensate(GameState s, long day)
            => Add(s, new Letter { Id = "makeup-" + day, Kind = LetterKind.MakeUp, Day = day });

        /// <summary>
        /// Looks at the days between the last look and <paramref name="today"/>: every season (of a year) that ran only
        /// on those days, was never greeted and is not running today leaves a letter. A new garden only starts counting.
        /// Returns the letters written.
        /// </summary>
        public static int Check(GameState s, long today)
        {
            Open(s, today);
            if (today <= s.MailDay) return 0;
            long from = s.MailDay;
            s.MailDay = today;
            if (from < 0) return 0;
            from = Math.Max(from, today - LookBackDays);

            Season now = Seasons.SeasonOf(today);
            int nowYear = Seasons.DateOf(today).Year;
            var missed = new List<KeyValuePair<Season, int>>();
            for (long d = from + 1; d < today; d++)
            {
                Season season = Seasons.SeasonOf(d);
                if (season == Season.None) continue;
                int year = Seasons.DateOf(d).Year;
                var key = new KeyValuePair<Season, int>(season, year);
                if (missed.Contains(key) || (season == now && year == nowYear) || Greeted(s, season, year)) continue;
                missed.Add(key);
            }
            int added = 0;
            foreach (KeyValuePair<Season, int> m in missed)
                if (Add(s, new Letter { Id = "season-" + m.Key + "-" + m.Value, Kind = LetterKind.Season, Season = m.Key, Day = today })) added++;
            return added;
        }

        static bool Greeted(GameState s, Season season, int year)
            => s.SeasonGreetedDay >= 0 && Seasons.SeasonOf(s.SeasonGreetedDay) == season && Seasons.DateOf(s.SeasonGreetedDay).Year == year;

        /// <summary>Claims one letter: pays it and takes it out of the box. Null, with nothing paid, when it is not there or has expired.</summary>
        public static Letter Claim(GameState s, string id, long day)
        {
            Open(s, day);
            Letter letter = s.Letters.Find(l => l.Id == id);
            if (letter == null) return null;
            s.Letters.Remove(letter);
            s.GoldenCarrots += letter.Golden;
            s.Clovers += letter.Clovers;
            return letter;
        }
    }
}

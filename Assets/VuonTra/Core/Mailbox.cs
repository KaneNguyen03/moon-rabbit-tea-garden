using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum LetterKind
    {
        MakeUp,
        Season,
        MoonPassDay,
        MoonPassRenew
    }

    public sealed class Letter
    {
        public string Id;
        public LetterKind Kind;
        public Season Season;
        public long Day;
        public int PassDay, PassDays;

        public int Golden
        {
            get
            {
                switch (Kind)
                {
                    case LetterKind.MakeUp: return Mailbox.MakeUpGolden;
                    case LetterKind.MoonPassDay: return MoonPass.GoldenPerDay;
                    case LetterKind.MoonPassRenew: return 0;
                    default: return Mailbox.SeasonGolden;
                }
            }
        }
        public int Clovers => Kind == LetterKind.MakeUp ? Mailbox.MakeUpClovers : 0;
    }

    public static class Mailbox
    {
        public const int MaxLetters = 10;
        public const int KeepDays = 30;
        public const int LookBackDays = 365;
        public const int MakeUpGolden = 10, MakeUpClovers = 3;
        public const int SeasonGolden = 5;

        public static bool Expired(Letter letter, long day) => day > letter.Day + KeepDays;

        public static int DaysLeft(Letter letter, long day) => (int)Math.Max(0L, letter.Day + KeepDays - day);

        public static IReadOnlyList<Letter> Open(GameState s, long day)
        {
            s.Letters.RemoveAll(l => Expired(l, day));
            return s.Letters;
        }

        internal static bool Add(GameState s, Letter letter)
        {
            foreach (Letter l in s.Letters) if (l.Id == letter.Id) return false;
            while (s.Letters.Count >= MaxLetters) s.Letters.RemoveAt(0);
            s.Letters.Add(letter);
            return true;
        }

        public static bool Compensate(GameState s, long day)
            => Add(s, new Letter { Id = "makeup-" + day, Kind = LetterKind.MakeUp, Day = day });

        public static int Check(GameState s, long today)
        {
            Open(s, today);
            int pass = MoonPass.Deliver(s, today);
            if (today <= s.MailDay) return pass;
            long from = s.MailDay;
            s.MailDay = today;
            if (from < 0) return pass;
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
            return added + pass;
        }

        static bool Greeted(GameState s, Season season, int year)
            => s.SeasonGreetedDay >= 0 && Seasons.SeasonOf(s.SeasonGreetedDay) == season && Seasons.DateOf(s.SeasonGreetedDay).Year == year;

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

using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum LetterKind
    {
        MakeUp,
        Season,
        WeekChest,
        MonthPass
    }

    public sealed class Letter
    {
        public string Id;
        public LetterKind Kind;
        public Season Season;
        public long Day;
        public int Chest;
        public long Month;
        public bool Premium;
        public int Tiers;
        public BigNum Coins;

        WeekChest ChestDef => Kind == LetterKind.WeekChest ? QuestChests.Chests[Chest] : null;

        MonthBundle Gifts => MonthPass.BundleOf(this);

        public int Golden => Kind == LetterKind.MonthPass ? Gifts.Golden : Kind == LetterKind.MakeUp ? Mailbox.MakeUpGolden : Kind == LetterKind.WeekChest ? ChestDef.Golden : Mailbox.SeasonGolden;
        public int Clovers => Kind == LetterKind.MonthPass ? Gifts.Clovers : Kind == LetterKind.MakeUp ? Mailbox.MakeUpClovers : Kind == LetterKind.WeekChest ? ChestDef.Clovers : 0;
        public int Jars => Kind == LetterKind.MonthPass ? Gifts.Jars : Kind == LetterKind.WeekChest ? ChestDef.Jars : 0;
        public int RabbitTier => Kind == LetterKind.MonthPass ? (Gifts.Rabbit ? MonthPass.RabbitRarity : QuestChests.NoRabbit) : Kind == LetterKind.WeekChest ? ChestDef.RabbitTier : QuestChests.NoRabbit;
        public int KeepDays => Kind == LetterKind.MonthPass ? MonthPass.KeepDays : Kind == LetterKind.WeekChest ? QuestChests.ChestKeepDays : Mailbox.KeepDays;
    }

    public static class Mailbox
    {
        public const int MaxLetters = 10;
        public const int KeepDays = 30;
        public const int LookBackDays = 365;
        public const int MakeUpGolden = 10, MakeUpClovers = 3;
        public const int SeasonGolden = 5;

        public static bool Expired(Letter letter, long day) => day > letter.Day + letter.KeepDays;

        public static int DaysLeft(Letter letter, long day) => (int)Math.Max(0L, letter.Day + letter.KeepDays - day);

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

        public static bool Compensate(GameState s, long day)
            => Add(s, new Letter { Id = "makeup-" + day, Kind = LetterKind.MakeUp, Day = day });

        public static bool AddChest(GameState s, int chest, long week, long day)
            => chest >= 0 && chest < QuestChests.Chests.Length
               && Add(s, new Letter { Id = "chest-" + week + "-" + chest, Kind = LetterKind.WeekChest, Chest = chest, Day = day });

        public static bool AddMonth(GameState s, Letter letter)
            => letter.Kind == LetterKind.MonthPass && letter.Tiers != 0 && Add(s, letter);

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

        public static Letter Claim(GameState s, string id, long day) => Claim(s, id, day, null, 0d, out _);

        public static Letter Claim(GameState s, string id, long day, string name, double kindRoll, out Rabbit rabbit)
        {
            rabbit = null;
            Open(s, day);
            Letter letter = s.Letters.Find(l => l.Id == id);
            if (letter == null) return null;
            s.Letters.Remove(letter);
            if (letter.Kind == LetterKind.MonthPass)
            {
                rabbit = MonthPass.GrantLetter(s, letter, name, kindRoll);
                return letter;
            }
            if (letter.Kind == LetterKind.WeekChest)
            {
                rabbit = QuestChests.Grant(s, QuestChests.Chests[letter.Chest], name, kindRoll);
                return letter;
            }
            s.GoldenCarrots += letter.Golden;
            s.Clovers += letter.Clovers;
            return letter;
        }
    }
}

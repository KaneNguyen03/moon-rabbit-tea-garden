using System;

namespace VuonTra.Core
{
    public static class MoonPass
    {
        public const string ProductId = "vuontra.moonpass.30";
        public const string ReferencePrice = "109.000 ₫";
        public const int Days = 30;
        public const int GoldenNow = 100;
        public const int GoldenPerDay = 15;
        public const int Total = GoldenNow + GoldenPerDay * Days;
        public const double AwayBonusSeconds = 3600d;
        public const int RemindAtDaysLeft = 3;

        public static bool Active(GameState s, long day) => s != null && s.MoonPassFromDay >= 0 && day >= s.MoonPassFromDay && day < s.MoonPassUntilDay;

        public static int DaysLeft(GameState s, long day) => Active(s, day) ? (int)(s.MoonPassUntilDay - day) : 0;

        public static bool ExtraDoubleFree(GameState s, long day) => Active(s, day) && day > s.MoonPassDoubleDay;

        static int RunDays(GameState s) => (int)(s.MoonPassUntilDay - s.MoonPassFromDay);

        public static void Grant(GameState s, long day)
        {
            day = Math.Max(0L, day);
            if (Active(s, day)) s.MoonPassUntilDay += Days;
            else
            {
                s.MoonPassFromDay = day;
                s.MoonPassUntilDay = day + Days;
            }
            s.Letters.RemoveAll(l => l.Kind == LetterKind.MoonPassRenew);
            s.GoldenCarrots += GoldenNow;
            Deliver(s, day);
        }

        public static int Deliver(GameState s, long today)
        {
            int added = 0;
            if (Active(s, today) && today > s.MoonPassMailDay)
            {
                s.MoonPassMailDay = today;
                var letter = new Letter { Id = "moonpass-" + today, Kind = LetterKind.MoonPassDay, Day = today, PassDay = (int)(today - s.MoonPassFromDay + 1), PassDays = RunDays(s) };
                if (Mailbox.Add(s, letter)) added++;
            }
            if (!Features.Monetization)
            {
                s.Letters.RemoveAll(l => l.Kind == LetterKind.MoonPassRenew);
                return added;
            }
            int left = DaysLeft(s, today);
            if (left > 0 && left <= RemindAtDaysLeft && s.MoonPassRemindedUntil != s.MoonPassUntilDay)
            {
                s.MoonPassRemindedUntil = s.MoonPassUntilDay;
                var letter = new Letter { Id = "moonpass-renew-" + s.MoonPassUntilDay, Kind = LetterKind.MoonPassRenew, Day = today, PassDay = left, PassDays = RunDays(s) };
                if (Mailbox.Add(s, letter)) added++;
            }
            return added;
        }
    }
}

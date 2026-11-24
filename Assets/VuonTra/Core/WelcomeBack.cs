using System;

namespace VuonTra.Core
{
    public sealed class WelcomeBackRecord
    {
        public long SeenDay = -1;
        public long StartDay = -1;
        public int AwayDays;
        public int CheckIns;
        public long CheckInDay = -1;
        public bool CardSeen = true;
    }

    public struct WelcomeBackGift
    {
        public bool Ok;
        public int AwayDays;
        public int Day;
        public int Jars, Clovers, Golden;
        public Rabbit Rabbit;
    }

    public static class WelcomeBack
    {
        public const long MinAwayDays = 3;
        public const long MinDaysPlayed = 2;
        public const long DaysBetween = 30;
        public const long BigAwayDays = 14;
        public const int BigAwayTimes = 2;
        public const int GiftJars = 2, GiftClovers = 3, GiftGolden = 5;

        public const int Days = 7;
        public const long WindowDays = 14;
        public static readonly int[] DayJars = { 1, 0, 0, 1, 0, 0, 0 };
        public static readonly int[] DayClovers = { 0, 2, 0, 0, 2, 0, 0 };
        public static readonly int[] DayGolden = { 0, 0, 3, 0, 0, 5, 0 };
        public static readonly int[] DayPurple = { 0, 0, 0, 0, 0, 0, 1 };
        public const int PurpleTier = Newcomer.PurpleTier;

        public const int QuestTimes = 2;
        public const int MaxAwayDays = 9999;

        public static int Times(int awayDays) => awayDays >= BigAwayDays ? BigAwayTimes : 1;

        public static int InstantJars(int awayDays) => GiftJars * Times(awayDays);

        public static int InstantClovers(int awayDays) => GiftClovers * Times(awayDays);

        public static int InstantGolden(int awayDays) => GiftGolden * Times(awayDays);

        public static WelcomeBackGift Check(GameState s, long day)
        {
            WelcomeBackRecord r = s.WelcomeBack;
            long before = r.SeenDay >= 0 ? r.SeenDay : s.MaxDaySeen;
            long today = Seasons.Clamp(s, day);
            r.SeenDay = today;
            if (before < 0 || !Due(s, before, today)) return default;
            return Start(s, today, (int)Math.Min(MaxAwayDays, today - before));
        }

        static bool Due(GameState s, long before, long today)
        {
            if (today - before < MinAwayDays || before - s.FirstDaySeen + 1 < MinDaysPlayed) return false;
            if (s.WelcomeBack.StartDay >= 0 && today - s.WelcomeBack.StartDay < DaysBetween) return false;
            return Tutorial.IsDone(s) && !Newcomer.ShowPill(s);
        }

        static WelcomeBackGift Start(GameState s, long today, int away)
        {
            WelcomeBackRecord r = s.WelcomeBack;
            r.StartDay = today;
            r.AwayDays = away;
            r.CheckIns = 0;
            r.CheckInDay = -1;
            r.CardSeen = false;
            var gift = new WelcomeBackGift
            {
                Ok = true,
                AwayDays = away,
                Jars = JarsRoom(s, InstantJars(away)),
                Clovers = InstantClovers(away),
                Golden = InstantGolden(away)
            };
            Give(s, gift);
            return gift;
        }

        static int JarsRoom(GameState s, int jars) => Math.Min(jars, Math.Max(0, MoonJars.Cap - s.MoonJars));

        static void Give(GameState s, WelcomeBackGift gift)
        {
            s.MoonJars += gift.Jars;
            s.Clovers += gift.Clovers;
            s.GoldenCarrots += gift.Golden;
        }

        public static bool Active(GameState s, long day)
        {
            WelcomeBackRecord r = s.WelcomeBack;
            if (r.StartDay < 0 || day < r.StartDay || day - r.StartDay >= WindowDays) return false;
            return r.CheckIns < Days || r.CheckInDay == day;
        }

        public static bool CanCheckIn(GameState s, long day) => Active(s, day) && s.WelcomeBack.CheckIns < Days && s.WelcomeBack.CheckInDay < day;

        public static int PillDay(GameState s, long day)
            => Math.Max(1, Math.Min(Days, s.WelcomeBack.CheckIns + (CanCheckIn(s, day) ? 1 : 0)));

        public static WelcomeBackGift CheckIn(GameState s, long day, string name, double roll)
        {
            if (!CanCheckIn(s, day)) return default;
            WelcomeBackRecord r = s.WelcomeBack;
            int i = r.CheckIns;
            var gift = new WelcomeBackGift
            {
                Ok = true,
                AwayDays = r.AwayDays,
                Day = i + 1,
                Jars = JarsRoom(s, DayJars[i]),
                Clovers = DayClovers[i],
                Golden = DayGolden[i]
            };
            Give(s, gift);
            if (DayPurple[i] > 0) gift.Rabbit = Rabbits.ArriveKind(s, Newcomer.PurpleKind(s, roll), name);
            r.CheckIns = i + 1;
            r.CheckInDay = day;
            return gift;
        }

        public static int QuestTimesFor(QuestDef q, GameState s) => q.Daily && Active(s, s.DailyDay) ? QuestTimes : 1;

        public static bool CardWaiting(GameState s) => s.WelcomeBack.StartDay >= 0 && !s.WelcomeBack.CardSeen;

        public static void SeeCard(GameState s) => s.WelcomeBack.CardSeen = true;

        public static Rabbit MostLoved(GameState s)
        {
            Rabbit best = null;
            foreach (Rabbit r in s.Rabbits)
                if (best == null || r.Affection > best.Affection || (r.Affection == best.Affection && r.Tier > best.Tier))
                    best = r;
            return best;
        }
    }
}

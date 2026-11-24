using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class NewcomerTask
    {
        public readonly string Id;
        public readonly int Day, Slot;
        public readonly QuestDef Quest;
        public readonly int NeedLevel, NeedRestSpots;

        public NewcomerTask(string id, int day, int slot, QuestMetric metric, double target, int needLevel = 0, string stationId = null, int needRestSpots = 0)
        {
            Id = id;
            Day = day;
            Slot = slot;
            NeedLevel = needLevel;
            NeedRestSpots = needRestSpots;
            Quest = new QuestDef { Id = "newcomer-" + id, Title = id, Metric = metric, StationId = stationId, Target = target };
        }

        public int Bit => 1 << ((Day - 1) * Newcomer.TasksPerDay + Slot);

        public double Target => Quest.Target;
    }

    public sealed class NewcomerRecord
    {
        public int Claimed;
        public int DaysPaid;
        public bool Closed;
    }

    public struct NewcomerClaim
    {
        public bool Ok;
        public NewcomerTask Task;
        public BigNum Carrots;
        public int Clovers, Golden;
        public bool DayPaid;
        public int DayJars, DayGolden;
        public Rabbit Rabbit;
    }

    public static class Newcomer
    {
        public const int Days = 7, TasksPerDay = 4;
        public const int Tasks = Days * TasksPerDay;
        public const int MailAfterDays = 14;
        public const int PurpleTier = 3;
        public const string TeapotId = "am-tra";

        public const double TaskSeconds = 120d;
        public static readonly int[] TaskClovers = { 0, 1, 1, 2, 2, 2, 2 };
        public static readonly int[] TaskGolden = { 0, 0, 0, 0, 0, 0, 0 };
        public static readonly int[] DayJars = { 1, 1, 1, 1, 1, 1, 3 };
        public static readonly int[] DayGolden = { 0, 0, 0, 0, 0, 0, 0 };
        public static readonly int[] DayPurple = { 0, 0, 0, 0, 0, 0, 1 };

        public const double FallbackLevels = 30d;
        public const double ShownKindRoll = 0d;

        static readonly NewcomerTask[] Table =
        {
            new NewcomerTask("teapot-25", 1, 0, QuestMetric.StationLevel, 25, stationId: TeapotId),
            new NewcomerTask("stations-4", 1, 1, QuestMetric.StationsOpen, 4),
            new NewcomerTask("call-2", 1, 2, QuestMetric.RabbitsCalled, 2),
            new NewcomerTask("order-1", 1, 3, QuestMetric.OrdersDelivered, 1),

            new NewcomerTask("fish-3", 2, 0, QuestMetric.FishCaught, 3, Fishing.UnlockLevel),
            new NewcomerTask("pounding-1", 2, 1, QuestMetric.PoundingGames, 1, Pounding.UnlockLevel),
            new NewcomerTask("feed-1", 2, 2, QuestMetric.RabbitsFed, 1, Fishing.UnlockLevel),
            new NewcomerTask("photo-1", 2, 3, QuestMetric.PhotosTaken, 1),

            new NewcomerTask("charms-3", 3, 0, QuestMetric.UpgradesOwned, 3),
            new NewcomerTask("level-8", 3, 1, QuestMetric.GardenLevel, 8),
            new NewcomerTask("trade-1", 3, 2, QuestMetric.RabbitsInTrade, 1),
            new NewcomerTask("jar-1", 3, 3, QuestMetric.JarsOpened, 1, MoonJars.ShowLevel),

            new NewcomerTask("orders-4", 4, 0, QuestMetric.OrdersDelivered, 4),
            new NewcomerTask("pet-3", 4, 1, QuestMetric.Pets, 3),
            new NewcomerTask("leaves-1", 4, 2, QuestMetric.LeavesGames, 1, Leaves.UnlockLevel),
            new NewcomerTask("stations-6", 4, 3, QuestMetric.StationsOpen, 6),

            new NewcomerTask("layout-1", 5, 0, QuestMetric.LayoutMoves, 1),
            new NewcomerTask("kinds-12", 5, 1, QuestMetric.KindsMet, 12),
            new NewcomerTask("boost-3", 5, 2, QuestMetric.Boosts, 3),
            new NewcomerTask("gift-1", 5, 3, QuestMetric.DailyGifts, 1),

            new NewcomerTask("level-12", 6, 0, QuestMetric.GardenLevel, 12),
            new NewcomerTask("fireflies-1", 6, 1, QuestMetric.FirefliesGames, 1, Fireflies.UnlockLevel),
            new NewcomerTask("merge-or-golden-1", 6, 2, QuestMetric.MergesOrGoldenCalls, 1),
            new NewcomerTask("rest-3", 6, 3, QuestMetric.RabbitsResting, 3, needRestSpots: 3),

            new NewcomerTask("segment-5", 7, 0, QuestMetric.SegmentsOpened, 5),
            new NewcomerTask("fish-tier-3", 7, 1, QuestMetric.BestFish, 3, Fishing.UnlockLevel),
            new NewcomerTask("stations-8", 7, 2, QuestMetric.StationsOpen, 8),
            new NewcomerTask("daily-4", 7, 3, QuestMetric.DailyQuestsToday, 4),
        };

        static readonly NewcomerTask[] Fallbacks = BuildFallbacks();

        static NewcomerTask[] BuildFallbacks()
        {
            var list = new NewcomerTask[Tasks];
            for (int i = 0; i < Tasks; i++)
                list[i] = new NewcomerTask("levels-30", Table[i].Day, Table[i].Slot, QuestMetric.LevelsBought, FallbackLevels);
            return list;
        }

        public static IReadOnlyList<NewcomerTask> All => Table;

        public static NewcomerTask At(int day, int slot) => Table[(day - 1) * TasksPerDay + slot];

        public static NewcomerTask FallbackOf(NewcomerTask task) => Fallbacks[(task.Day - 1) * TasksPerDay + task.Slot];

        public static bool Unlocked(NewcomerTask task, GameState s) => Garden.Level(s) >= task.NeedLevel && s.RestSpots >= task.NeedRestSpots;

        public static NewcomerTask Shown(NewcomerTask task, GameState s) => Unlocked(task, s) ? task : FallbackOf(task);

        public static bool IsFallback(NewcomerTask task, GameState s) => !Unlocked(task, s);

        public static int DayNow(GameState s) => (int)Math.Min(MailAfterDays + 1L, Seasons.DaysPlayed(s) + 1L);

        public static int PillDay(GameState s) => Math.Min(Days, DayNow(s));

        public static bool DayOpen(GameState s, int day) => !s.Newcomer.Closed && day >= 1 && day <= Days && Seasons.DaysPlayed(s) >= day - 1;

        public static int DaysUntil(GameState s, int day) => (int)Math.Max(0L, day - 1L - Seasons.DaysPlayed(s));

        public static bool IsClaimed(NewcomerTask task, GameState s) => (s.Newcomer.Claimed & task.Bit) != 0;

        public static double Progress(NewcomerTask task, IReadOnlyList<StationDef> defs, GameState s)
            => Quests.Progress(Shown(task, s).Quest, defs, s);

        public static double TargetOf(NewcomerTask task, GameState s) => Shown(task, s).Target;

        public static bool IsDone(NewcomerTask task, IReadOnlyList<StationDef> defs, GameState s)
            => IsClaimed(task, s) || Progress(task, defs, s) >= TargetOf(task, s);

        public static bool CanClaim(NewcomerTask task, IReadOnlyList<StationDef> defs, GameState s)
            => DayOpen(s, task.Day) && !IsClaimed(task, s) && Progress(task, defs, s) >= TargetOf(task, s);

        public static bool DayComplete(GameState s, int day)
        {
            int mask = DayMask(day);
            return (s.Newcomer.Claimed & mask) == mask;
        }

        public static bool DayPaid(GameState s, int day) => (s.Newcomer.DaysPaid & (1 << (day - 1))) != 0;

        static int DayMask(int day) => ((1 << TasksPerDay) - 1) << ((day - 1) * TasksPerDay);

        public static bool AllClaimed(GameState s) => s.Newcomer.Claimed == (1 << Tasks) - 1;

        public static int Claimable(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (s.Newcomer.Closed) return 0;
            int n = 0;
            for (int i = 0; i < Table.Length; i++) if (CanClaim(Table[i], defs, s)) n++;
            return n;
        }

        public static bool ShowPill(GameState s)
            => !s.Newcomer.Closed && Tutorial.IsDone(s) && Seasons.DaysPlayed(s) < MailAfterDays && !AllClaimed(s);

        public static BigNum TaskCarrots(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * TaskSeconds);

        public static string PurpleKind(GameState s, double roll)
        {
            IReadOnlyList<RabbitKind> kinds = RabbitKinds.OfTier(PurpleTier);
            var unseen = new List<RabbitKind>(kinds.Count);
            for (int i = 0; i < kinds.Count; i++) if (!s.SeenKinds.Contains(kinds[i].Id)) unseen.Add(kinds[i]);
            IReadOnlyList<RabbitKind> pool = unseen.Count > 0 ? unseen : kinds;
            if (pool.Count == 0) return RabbitKinds.DefaultFor(PurpleTier);
            if (double.IsNaN(roll) || roll < 0d) roll = 0d;
            return pool[Math.Min(pool.Count - 1, (int)Math.Floor(roll * pool.Count))].Id;
        }

        public static NewcomerClaim Claim(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int day, int slot, string name, double roll)
        {
            var result = new NewcomerClaim();
            if (day < 1 || day > Days || slot < 0 || slot >= TasksPerDay) return result;
            NewcomerTask task = At(day, slot);
            result.Task = task;
            if (!CanClaim(task, defs, s)) return result;
            result.Ok = true;
            result.Carrots = TaskCarrots(defs, s, m);
            result.Clovers = TaskClovers[day - 1];
            result.Golden = TaskGolden[day - 1];
            s.Coins += result.Carrots;
            s.Clovers += result.Clovers;
            s.GoldenCarrots += result.Golden;
            s.Newcomer.Claimed |= task.Bit;
            if (DayComplete(s, day) && !DayPaid(s, day)) PayDay(s, day, name, roll, ref result);
            return result;
        }

        static void PayDay(GameState s, int day, string name, double roll, ref NewcomerClaim result)
        {
            s.Newcomer.DaysPaid |= 1 << (day - 1);
            result.DayPaid = true;
            result.DayJars = Math.Min(DayJars[day - 1], Math.Max(0, MoonJars.Cap - s.MoonJars));
            result.DayGolden = DayGolden[day - 1];
            s.MoonJars += result.DayJars;
            s.GoldenCarrots += result.DayGolden;
            if (DayPurple[day - 1] > 0) result.Rabbit = Rabbits.ArriveKind(s, PurpleKind(s, roll), name);
        }

        public static Letter Expire(IReadOnlyList<StationDef> defs, GameState s, long today)
        {
            if (s.Newcomer.Closed || Seasons.DaysPlayed(s) < MailAfterDays) return null;
            s.Newcomer.Closed = true;
            int clovers = 0, golden = 0, jars = 0, purple = 0;
            for (int i = 0; i < Table.Length; i++)
            {
                NewcomerTask task = Table[i];
                if (IsClaimed(task, s) || Progress(task, defs, s) < TargetOf(task, s)) continue;
                s.Newcomer.Claimed |= task.Bit;
                clovers += TaskClovers[task.Day - 1];
                golden += TaskGolden[task.Day - 1];
            }
            for (int day = 1; day <= Days; day++)
            {
                if (!DayComplete(s, day) || DayPaid(s, day)) continue;
                s.Newcomer.DaysPaid |= 1 << (day - 1);
                jars += DayJars[day - 1];
                golden += DayGolden[day - 1];
                purple += DayPurple[day - 1];
            }
            if (clovers + golden + jars + purple == 0) return null;
            var letter = new Letter { Id = "newcomer", Kind = LetterKind.Newcomer, Day = today, GiftClovers = clovers, GiftGolden = golden, GiftJars = jars, GiftPurple = purple };
            return Mailbox.Send(s, letter) ? letter : null;
        }
    }
}

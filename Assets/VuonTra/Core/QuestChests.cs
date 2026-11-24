using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum BoxPrizeKind { Income, Clover, Jar, Golden }

    public sealed class BoxSlot
    {
        public readonly BoxPrizeKind Kind;
        public readonly int Amount;
        public readonly int Weight;

        public BoxSlot(BoxPrizeKind kind, int amount, int weight)
        {
            Kind = kind;
            Amount = amount;
            Weight = weight;
        }
    }

    public struct BoxPrize
    {
        public BoxPrizeKind Kind;
        public int Amount;
        public BigNum Coins;
    }

    public sealed class WeekChest
    {
        public readonly int Points, Jars, Clovers, Golden, RabbitTier;

        public WeekChest(int points, int jars, int clovers, int golden, int rabbitTier)
        {
            Points = points;
            Jars = jars;
            Clovers = clovers;
            Golden = golden;
            RabbitTier = rabbitTier;
        }
    }

    public static class QuestChests
    {
        public const int QuestPoints = 10;
        public const int BoxPoints = 10;
        public const int BoxPrizes = 3;
        public const int ChestKeepDays = 7;
        public const int DailyQuests = 5;
        public const int MaxPoints = 7 * (DailyQuests * QuestPoints + BoxPoints);
        const int DrawSalt = 1030;

        public static readonly BoxSlot[] Table =
        {
            new BoxSlot(BoxPrizeKind.Income, 300, 40),
            new BoxSlot(BoxPrizeKind.Clover, 1, 25),
            new BoxSlot(BoxPrizeKind.Jar, 1, 15),
            new BoxSlot(BoxPrizeKind.Golden, 2, 15),
            new BoxSlot(BoxPrizeKind.Golden, 5, 5),
        };

        public const int NoRabbit = -1;

        public static readonly WeekChest[] Chests =
        {
            new WeekChest(100, 2, 2, 0, NoRabbit),
            new WeekChest(250, 0, 0, 5, NoRabbit),
            new WeekChest(400, 0, 0, 10, 2),
        };

        public static long WeekOf(long day)
        {
            long shifted = day + 3;
            return shifted >= 0 ? shifted / 7 : (shifted - 6) / 7;
        }

        public static long MondayOf(long week) => week * 7 - 3;

        public static int DailyTotal(IReadOnlyList<QuestDef> quests, GameState s)
        {
            int n = 0;
            foreach (QuestDef q in quests) if (q.Daily && Quests.IsAvailable(q, s.MaxDaySeen)) n++;
            return n;
        }

        public static int DailyDone(IReadOnlyList<QuestDef> quests, GameState s)
        {
            int n = 0;
            foreach (QuestDef q in quests) if (q.Daily && Quests.IsAvailable(q, s.MaxDaySeen) && Quests.IsClaimed(q, s)) n++;
            return n;
        }

        public static bool BoxOpened(GameState s) => s.BoxDay >= 0 && s.BoxDay == s.DailyDay;

        public static bool BoxReady(IReadOnlyList<QuestDef> quests, GameState s)
        {
            int total = DailyTotal(quests, s);
            return !BoxOpened(s) && s.BoxDay < s.DailyDay && total > 0 && DailyDone(quests, s) >= total;
        }

        public static BoxSlot[] Draw(long day, string code)
        {
            var box = new BoxSlot[BoxPrizes];
            bool golden = false;
            for (int i = 0; i < BoxPrizes; i++)
            {
                int total = 0;
                foreach (BoxSlot slot in Table) if (!(golden && slot.Kind == BoxPrizeKind.Golden)) total += slot.Weight;
                double at = Shop.Roll(day, code, DrawSalt + i) * total;
                BoxSlot picked = null;
                foreach (BoxSlot slot in Table)
                {
                    if (golden && slot.Kind == BoxPrizeKind.Golden) continue;
                    picked = slot;
                    at -= slot.Weight;
                    if (at < 0d) break;
                }
                box[i] = picked;
                golden |= picked.Kind == BoxPrizeKind.Golden;
            }
            return box;
        }

        public static BigNum IncomeOf(BoxSlot slot, IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            BigNum bySeconds = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * slot.Amount);
            var floor = new BigNum(slot.Amount);
            return bySeconds > floor ? bySeconds : floor;
        }

        public static BoxPrize[] OpenBox(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            if (!BoxReady(quests, s)) return null;
            BoxSlot[] box = Draw(s.DailyDay, s.Profile != null ? s.Profile.Code : "");
            var prizes = new BoxPrize[box.Length];
            for (int i = 0; i < box.Length; i++)
            {
                BoxSlot slot = box[i];
                var prize = new BoxPrize { Kind = slot.Kind, Amount = slot.Amount, Coins = BigNum.Zero };
                switch (slot.Kind)
                {
                    case BoxPrizeKind.Income:
                        prize.Coins = IncomeOf(slot, defs, s, m);
                        s.Coins += prize.Coins;
                        break;
                    case BoxPrizeKind.Clover:
                        s.Clovers += slot.Amount;
                        break;
                    case BoxPrizeKind.Jar:
                        int jars = GiveJars(s, slot.Amount);
                        if (jars < slot.Amount)
                        {
                            prize.Kind = BoxPrizeKind.Clover;
                            prize.Amount = slot.Amount - jars;
                        }
                        break;
                    case BoxPrizeKind.Golden:
                        s.GoldenCarrots += slot.Amount;
                        break;
                }
                prizes[i] = prize;
            }
            s.BoxDay = s.DailyDay;
            AddPoints(s, s.DailyDay, BoxPoints);
            return prizes;
        }

        public static int GiveJars(GameState s, int count)
        {
            int fit = MoonJars.Shown(s) ? Math.Max(0, Math.Min(count, MoonJars.Cap - s.MoonJars)) : 0;
            s.MoonJars += fit;
            s.Clovers += count - fit;
            return fit;
        }

        public static void Roll(GameState s, long day)
        {
            long week = WeekOf(day);
            if (week <= s.WeekIndex) return;
            if (s.WeekIndex >= 0)
                for (int i = 0; i < Chests.Length; i++)
                    if (s.WeekPoints >= Chests[i].Points && !Claimed(s, i)) Mailbox.AddChest(s, i, s.WeekIndex, day);
            s.WeekIndex = week;
            s.WeekPoints = 0;
            s.WeekChestsClaimed = 0;
        }

        public static void AddPoints(GameState s, long day, int points)
        {
            MonthPass.AddPoints(s, day, points);
            Roll(s, day);
            if (WeekOf(day) != s.WeekIndex) return;
            s.WeekPoints = Math.Min(MaxPoints, s.WeekPoints + points);
        }

        public static int Points(GameState s, long day) => WeekOf(day) == s.WeekIndex ? s.WeekPoints : 0;

        public static bool Claimed(GameState s, int chest) => (s.WeekChestsClaimed & (1 << chest)) != 0;

        public static bool ChestReady(GameState s, int chest, long day)
            => chest >= 0 && chest < Chests.Length && Points(s, day) >= Chests[chest].Points && !Claimed(s, chest);

        public static int ReadyCount(GameState s, long day)
        {
            int n = 0;
            for (int i = 0; i < Chests.Length; i++) if (ChestReady(s, i, day)) n++;
            return n;
        }

        public static bool ClaimChest(GameState s, int chest, long day, string name, double kindRoll, out Rabbit rabbit)
        {
            rabbit = null;
            Roll(s, day);
            if (!ChestReady(s, chest, day)) return false;
            s.WeekChestsClaimed |= 1 << chest;
            rabbit = Grant(s, Chests[chest], name, kindRoll);
            return true;
        }

        public static Rabbit Grant(GameState s, WeekChest chest, string name, double kindRoll)
        {
            GiveJars(s, chest.Jars);
            s.Clovers += chest.Clovers;
            s.GoldenCarrots += chest.Golden;
            return chest.RabbitTier == NoRabbit ? null : Rabbits.Arrive(s, chest.RabbitTier, name, kindRoll);
        }
    }
}

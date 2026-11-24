using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum FairWare { Jars, Clovers, Golden, Lantern, Frame, Rabbit }

    public readonly struct FairStall
    {
        public readonly FairWare Ware;
        public readonly int Price, Amount, Max;

        public FairStall(FairWare ware, int price, int amount, int max)
        {
            Ware = ware;
            Price = price;
            Amount = amount;
            Max = max;
        }
    }

    public sealed class FairDef
    {
        public readonly string Id;
        public readonly int Month, StartDay, Days;
        public readonly string Hex;
        public readonly string RabbitKind;
        public readonly FairStall[] Stalls;

        public FairDef(string id, int month, string hex, string rabbitKind, FairStall[] stalls = null, int startDay = Fairs.StartDay, int days = Fairs.Days)
        {
            Id = id;
            Month = month;
            Hex = hex;
            RabbitKind = rabbitKind;
            Stalls = stalls ?? Fairs.Stall;
            StartDay = startDay;
            Days = days;
        }

        public string NameKey => "fair." + Id;
        public string CoinKey => "fair.coin." + Id;
    }

    public readonly struct FairRun
    {
        public readonly FairDef Def;
        public readonly int Index;
        public readonly long Start;

        public FairRun(FairDef def, int index, long start)
        {
            Def = def;
            Index = index;
            Start = start;
        }

        public long End => Start + Def.Days - 1;
        public int DayOf(long day) => (int)(day - Start) + 1;
        public int DaysLeft(long day) => (int)Math.Max(0L, End - day + 1);
    }

    public sealed class FairRecord
    {
        public long Start = -1;
        public long Done = -1;
        public long Seen = -1;
        public int Coins;
        public int[] Bought = new int[0];
        public int Lanterns, Frames;
    }

    public enum FairBuyCheck { Ok, Closed, NoStall, SoldOut, Owned, Short }

    public struct FairBuy
    {
        public FairBuyCheck Check;
        public FairStall Stall;
        public int Jars, JarClovers;
        public Rabbit Rabbit;
        public bool Ok => Check == FairBuyCheck.Ok;
    }

    public static class Fairs
    {
        public const int StartDay = 10, Days = 10;
        public const int OrderCoins = 3, PhotoAskCoins = 2;
        public static readonly int[] StarCoins = { 0, 1, 3, 5 };
        public static readonly int[] FishCoins = { 1, 2, 2, 3, 3, 4 };
        public const int WeekendTimes = 2;
        public const int CarrotSecondsPerCoin = 30;

        public static readonly FairStall[] Stall =
        {
            new FairStall(FairWare.Jars, 40, 1, 5),
            new FairStall(FairWare.Clovers, 30, 1, 10),
            new FairStall(FairWare.Golden, 80, 5, 4),
            new FairStall(FairWare.Lantern, 150, 1, 1),
            new FairStall(FairWare.Frame, 200, 1, 1),
            new FairStall(FairWare.Rabbit, 400, 1, 1),
        };

        static readonly FairDef[] Table =
        {
            new FairDef("hoi-hoa-mo", 3, "F7C6D4", "tho-anh-dao"),
            new FairDef("hoi-dieu", 4, "7EC2F0", "tho-may"),
            new FairDef("hoi-tra-sen", 5, "F28BB5", "tho-hoa-nhai"),
            new FairDef("hoi-dom-dom", 6, "C9E265", "tho-luu-ly"),
            new FairDef("hoi-mua-ha", 7, "5FB8B0", "tho-reu"),
            new FairDef("hoi-den-giay", 8, "F4A259", "tho-oai-huong"),
            new FairDef("hoi-com-moi", 10, "8DBF5A", "tho-khoai-mon"),
            new FairDef("hoi-hoa-cuc", 11, "F2C14E", "tho-sim"),
        };

        public static IReadOnlyList<FairDef> All => Table;

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Table.Length; i++)
                if (Table[i].Id == id) return i;
            return -1;
        }

        public static long StartOf(FairDef def, int year) => GiftCodes.Day(year, def.Month, def.StartDay);

        public static FairRun? RunOn(long day)
        {
            int year = Seasons.DateOf(day).Year;
            for (int i = 0; i < Table.Length; i++)
                for (int y = year - 1; y <= year; y++)
                {
                    long start = StartOf(Table[i], y);
                    if (day >= start && day < start + Table[i].Days) return new FairRun(Table[i], i, start);
                }
            return null;
        }

        public static FairRun? Next(long day)
        {
            FairRun? best = null;
            int year = Seasons.DateOf(day).Year;
            for (int i = 0; i < Table.Length; i++)
                for (int y = year; y <= year + 1; y++)
                {
                    long start = StartOf(Table[i], y);
                    if (start > day && (best == null || start < best.Value.Start)) best = new FairRun(Table[i], i, start);
                }
            return best;
        }

        static long Today(GameState s, long day) => Math.Max(day, s.MaxDaySeen);

        public static bool Weekend(long day)
        {
            DayOfWeek d = Seasons.DateOf(day).DayOfWeek;
            return d == DayOfWeek.Saturday || d == DayOfWeek.Sunday;
        }

        public static int Times(long day) => Weekend(day) ? WeekendTimes : 1;

        public static FairRun? Open(GameState s, long day)
        {
            long today = Today(s, day);
            FairRun? run = RunOn(today);
            if (run == null || run.Value.Start <= s.Fair.Done) return null;
            return run;
        }

        public static bool CardWaiting(GameState s, long day) => Open(s, day) is FairRun run && s.Fair.Seen < run.Start;

        public static void SeeCard(GameState s, long day)
        {
            if (Open(s, day) is FairRun run) s.Fair.Seen = Math.Max(s.Fair.Seen, run.Start);
        }

        public static int ForStars(int stars) => StarCoins[Math.Max(0, Math.Min(StarCoins.Length - 1, stars))];

        public static int ForFish(int fish) => FishCoins[Math.Max(0, Math.Min(FishCoins.Length - 1, fish))];

        public static Letter Roll(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day)
        {
            long today = Today(s, day);
            Letter letter = null;
            FairRecord f = s.Fair;
            if (f.Start >= 0)
            {
                FairRun? joined = RunOn(f.Start);
                long end = joined != null ? joined.Value.End : f.Start;
                if (today > end) letter = Settle(defs, s, m, today);
            }
            FairRun? run = Open(s, today);
            if (run != null && run.Value.Start > f.Start)
            {
                f.Start = run.Value.Start;
                f.Coins = 0;
                f.Bought = new int[run.Value.Def.Stalls.Length];
            }
            return letter;
        }

        static Letter Settle(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long today)
        {
            FairRecord f = s.Fair;
            int coins = f.Coins;
            long start = f.Start;
            f.Done = Math.Max(f.Done, start);
            f.Start = -1;
            f.Coins = 0;
            f.Bought = new int[0];
            if (coins <= 0) return null;
            var letter = new Letter { Id = "fair-" + start, Kind = LetterKind.FairEnd, Day = today, Fair = start, FairCoins = coins, Coins = CarrotsFor(defs, s, m, coins) };
            return Mailbox.Send(s, letter) ? letter : null;
        }

        public static BigNum CarrotsFor(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int coins)
        {
            double seconds = (double)Math.Max(0, coins) * CarrotSecondsPerCoin;
            var floor = new BigNum(seconds);
            if (defs == null) return floor;
            BigNum bySeconds = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * seconds);
            return bySeconds > floor ? bySeconds : floor;
        }

        public static FairRun? Joined(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day)
        {
            Roll(defs, s, m, day);
            FairRun? run = Open(s, day);
            return run != null && run.Value.Start == s.Fair.Start ? run : null;
        }

        public static int Earn(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day, int coins)
        {
            if (coins <= 0 || Joined(defs, s, m, day) == null) return 0;
            int earned = coins * Times(Today(s, day));
            s.Fair.Coins += earned;
            return earned;
        }

        public static int Bought(GameState s, int stall) => s.Fair.Bought != null && stall >= 0 && stall < s.Fair.Bought.Length ? s.Fair.Bought[stall] : 0;

        public static int LanternIndex(int fair) => Decor.LanternColors.Length + MonthPass.Months + fair;

        public static int LanternFair(int color)
        {
            int fair = color - Decor.LanternColors.Length - MonthPass.Months;
            return fair >= 0 && fair < Table.Length ? fair : -1;
        }

        public static int FrameIndex(int fair) => MonthPass.Months + fair;

        public static int FrameFair(int frame)
        {
            int fair = frame - MonthPass.Months;
            return fair >= 0 && fair < Table.Length ? fair : -1;
        }

        public static int Dye(int fair) => Shop.Dyes.Length + fair;

        public static int DyeFair(int dye)
        {
            int fair = dye - Shop.Dyes.Length;
            return fair >= 0 && fair < Table.Length ? fair : -1;
        }

        public static string DyeHex(int dye)
        {
            if (dye >= 0 && dye < Shop.Dyes.Length) return Shop.Dyes[dye].Hex;
            int fair = DyeFair(dye);
            return fair >= 0 ? Table[fair].Hex : null;
        }

        public static string FrameHex(int frame)
        {
            if (frame >= 0 && frame < MonthPass.Months) return MonthPass.FrameColors[frame];
            int fair = FrameFair(frame);
            return fair >= 0 ? Pastel(Table[fair].Hex) : null;
        }

        public static string Pastel(string hex)
        {
            const string paper = "FFF4E0";
            var sb = new System.Text.StringBuilder(6);
            for (int i = 0; i < 6; i += 2)
            {
                int a = Convert.ToInt32(hex.Substring(i, 2), 16), b = Convert.ToInt32(paper.Substring(i, 2), 16);
                sb.Append(((a * 45 + b * 55) / 100).ToString("X2"));
            }
            return sb.ToString();
        }

        public static bool OwnsLantern(GameState s, int fair) => fair >= 0 && fair < Table.Length && (s.Fair.Lanterns & (1 << fair)) != 0;

        public static bool OwnsFrame(GameState s, int fair) => fair >= 0 && fair < Table.Length && (s.Fair.Frames & (1 << fair)) != 0;

        public static int AllFairs => (1 << Table.Length) - 1;

        public static bool HangLantern(GameState s, int fair)
        {
            if (!OwnsLantern(s, fair)) return false;
            s.LanternColor = LanternIndex(fair);
            return true;
        }

        public static bool WearFrame(GameState s, int fair)
        {
            if (!OwnsFrame(s, fair)) return false;
            s.PhotoFrame = FrameIndex(fair);
            return true;
        }

        public static FairBuyCheck CanBuy(GameState s, long day, int stall)
        {
            FairRun? run = Open(s, day);
            if (run == null || run.Value.Start != s.Fair.Start) return FairBuyCheck.Closed;
            FairStall[] stalls = run.Value.Def.Stalls;
            if (stall < 0 || stall >= stalls.Length) return FairBuyCheck.NoStall;
            FairStall ware = stalls[stall];
            if (ware.Ware == FairWare.Lantern && OwnsLantern(s, run.Value.Index)) return FairBuyCheck.Owned;
            if (ware.Ware == FairWare.Frame && OwnsFrame(s, run.Value.Index)) return FairBuyCheck.Owned;
            if (Bought(s, stall) >= ware.Max) return FairBuyCheck.SoldOut;
            if (s.Fair.Coins < ware.Price) return FairBuyCheck.Short;
            return FairBuyCheck.Ok;
        }

        public static FairBuy Buy(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, long day, int stall, string name)
        {
            var result = new FairBuy();
            FairRun? joined = Joined(defs, s, m, day);
            result.Check = CanBuy(s, day, stall);
            if (!result.Ok) return result;
            FairRun run = joined.Value;
            FairStall ware = run.Def.Stalls[stall];
            result.Stall = ware;
            s.Fair.Coins -= ware.Price;
            s.Fair.Bought[stall]++;
            switch (ware.Ware)
            {
                case FairWare.Jars:
                    result.Jars = QuestChests.GiveJars(s, ware.Amount);
                    result.JarClovers = ware.Amount - result.Jars;
                    break;
                case FairWare.Clovers:
                    s.Clovers += ware.Amount;
                    break;
                case FairWare.Golden:
                    s.GoldenCarrots += ware.Amount;
                    break;
                case FairWare.Lantern:
                    s.Fair.Lanterns |= 1 << run.Index;
                    s.LanternColor = LanternIndex(run.Index);
                    break;
                case FairWare.Frame:
                    s.Fair.Frames |= 1 << run.Index;
                    s.PhotoFrame = FrameIndex(run.Index);
                    break;
                case FairWare.Rabbit:
                    result.Rabbit = Rabbits.ArriveKind(s, run.Def.RabbitKind, name);
                    result.Rabbit.Dye = Dye(run.Index);
                    break;
            }
            return result;
        }
    }
}

using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>A thing a station made, waiting in the tray: so many of one station's product, plain or golden.</summary>
    public sealed class Product
    {
        public int Station;
        public int Count = 1;
        public bool Golden;
    }

    /// <summary>
    /// The product tray: now and then a working brew station makes a special batch that lands in the tray,
    /// and the player sells it with a touch. Income keeps flowing by itself; the tray is extra for whoever
    /// is there to collect it, about a fifth more.
    /// </summary>
    public static class Tray
    {
        public const int StartingSlots = 5, MaxSlots = 8;
        /// <summary>How many of one station's product pile up in one slot before the station waits.</summary>
        public const int StackLimit = 5;
        /// <summary>Seconds of work between one special batch and the next, at any station.</summary>
        public const double ProductEvery = 75d;
        /// <summary>A product sells for this many seconds of its station's income, so collecting everything adds a fifth.</summary>
        public const double ProductSeconds = 15d;
        /// <summary>The chance a batch comes out golden, and how many golden ones a day can bring at most.</summary>
        public const double GoldenChance = 0.04;
        public const int GoldenPerDay = 2;
        /// <summary>What a golden product pays on top of its carrots: golden carrots.</summary>
        public const int GoldenCarrotsEach = 1;

        static void Size(GameState s)
        {
            if (s.TrayWork == null || s.TrayWork.Length != s.Levels.Length) Array.Resize(ref s.TrayWork, s.Levels.Length);
            if (s.TraySlots < StartingSlots) s.TraySlots = StartingSlots;
            if (s.TraySlots > MaxSlots) s.TraySlots = MaxSlots;
        }

        /// <summary>The slot holding this station's plain products, or null.</summary>
        static Product StackOf(GameState s, int station, bool golden)
        {
            foreach (Product p in s.Tray)
                if (p.Station == station && p.Golden == golden) return p;
            return null;
        }

        /// <summary>True when a plain product of this station has somewhere to go.</summary>
        public static bool CanTake(GameState s, int station)
        {
            Size(s);
            Product stack = StackOf(s, station, false);
            return stack != null ? stack.Count < StackLimit : s.Tray.Count < s.TraySlots;
        }

        /// <summary>True when no station could add anything: every slot is taken and every stack is at its limit.</summary>
        public static bool IsFull(GameState s)
        {
            Size(s);
            if (s.Tray.Count < s.TraySlots) return false;
            foreach (Product p in s.Tray)
                if (!p.Golden && p.Count < StackLimit) return false;
            return true;
        }

        /// <summary>Puts one product in the tray. False, and nothing changes, when there is no room for it.</summary>
        public static bool Add(GameState s, int station, bool golden)
        {
            Size(s);
            Product stack = StackOf(s, station, golden);
            if (stack != null)
            {
                if (stack.Count < StackLimit)
                {
                    stack.Count++;
                    return true;
                }
                if (!golden) return false;
            }
            else if (s.Tray.Count < s.TraySlots)
            {
                s.Tray.Add(new Product { Station = station, Golden = golden });
                return true;
            }
            // No room for it as golden: it goes in as a plain product instead, if there is room for that.
            return golden && Add(s, station, false);
        }

        /// <summary>
        /// Counts <paramref name="seconds"/> of work at a station towards its next batch and makes any
        /// batches that are due. When the tray has no room the station holds its batch, ready for when
        /// there is. <paramref name="roll"/> decides golden batches; null never makes one.
        /// </summary>
        public static void Advance(GameState s, int station, double seconds, Func<double> roll = null, Action<int> onProduct = null)
        {
            Size(s);
            if (!(seconds > 0d) || station < 0 || station >= s.TrayWork.Length) return;
            s.TrayWork[station] += seconds;
            while (s.TrayWork[station] >= ProductEvery)
            {
                if (!CanTake(s, station))
                {
                    s.TrayWork[station] = ProductEvery; // held: the next batch is ready the moment there is room
                    return;
                }
                s.TrayWork[station] -= ProductEvery;
                bool golden = roll != null && s.GoldenToday < GoldenPerDay && roll() < GoldenChance;
                if (golden) s.GoldenToday++;
                Add(s, station, golden);
                onProduct?.Invoke(station);
            }
        }

        /// <summary>What one product of this station sells for right now.</summary>
        public static BigNum Price(IReadOnlyList<StationDef> defs, GameState s, int station, Modifiers m)
        {
            StationDef d = defs[station];
            if (d.Kind != StationKind.Brew || d.CycleSeconds <= 0d) return BigNum.Zero;
            return Economy.IncomePerCycle(defs, s, station) / d.CycleSeconds * (ProductSeconds * m.IncomeMultiplier * m.SpeedMultiplier);
        }

        /// <summary>Sells the whole stack in a slot. Returns the carrots paid; golden stacks also pay golden carrots.</summary>
        public static BigNum Sell(IReadOnlyList<StationDef> defs, GameState s, int slot, Modifiers m, out int golden)
        {
            golden = 0;
            if (slot < 0 || slot >= s.Tray.Count) return BigNum.Zero;
            Product p = s.Tray[slot];
            BigNum paid = Price(defs, s, p.Station, m) * p.Count;
            if (p.Golden) golden = GoldenCarrotsEach * p.Count;
            s.Coins += paid;
            s.TotalEarned += paid;
            s.GoldenCarrots += golden;
            s.Tray.RemoveAt(slot);
            return paid;
        }

        /// <summary>Sells everything in the tray at once.</summary>
        public static BigNum SellAll(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, out int golden)
        {
            golden = 0;
            BigNum total = BigNum.Zero;
            while (s.Tray.Count > 0)
            {
                total += Sell(defs, s, s.Tray.Count - 1, m, out int some);
                golden += some;
            }
            return total;
        }

        /// <summary>Time away makes batches at every open brew station, as many as fit and no more; none golden.</summary>
        public static void FillAway(IReadOnlyList<StationDef> defs, GameState s, double seconds)
        {
            if (!(seconds > 0d)) return;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i].Kind != StationKind.Brew || s.Levels[i] <= 0 || defs[i].CycleSeconds <= 0d || Layout.IsStored(defs, s, i)) continue;
                Advance(s, i, seconds);
            }
        }

        /// <summary>How many things the tray holds, every stack counted.</summary>
        public static int Count(GameState s)
        {
            int n = 0;
            foreach (Product p in s.Tray) n += p.Count;
            return n;
        }
    }
}

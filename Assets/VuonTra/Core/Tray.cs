using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class Product
    {
        public int Station;
        public int Count = 1;
        public bool Golden;
    }

    public static class Tray
    {
        public const int StartingSlots = 5, MaxSlots = 8;
        public const int StackLimit = 5;
        public const double ProductEvery = 75d;
        public const double ProductSeconds = 15d;
        public const double GoldenChance = 0.04;
        public const int GoldenPerDay = 2;
        public const int GoldenCarrotsEach = 1;

        static void Size(GameState s)
        {
            if (s.TrayWork == null || s.TrayWork.Length != s.Levels.Length) Array.Resize(ref s.TrayWork, s.Levels.Length);
            if (s.TraySlots < StartingSlots) s.TraySlots = StartingSlots;
            if (s.TraySlots > MaxSlots) s.TraySlots = MaxSlots;
        }

        static Product StackOf(GameState s, int station, bool golden)
        {
            foreach (Product p in s.Tray)
                if (p.Station == station && p.Golden == golden) return p;
            return null;
        }

        public static bool CanTake(GameState s, int station)
        {
            Size(s);
            Product stack = StackOf(s, station, false);
            return stack != null ? stack.Count < StackLimit : s.Tray.Count < s.TraySlots;
        }

        public static bool IsFull(GameState s)
        {
            Size(s);
            if (s.Tray.Count < s.TraySlots) return false;
            foreach (Product p in s.Tray)
                if (!p.Golden && p.Count < StackLimit) return false;
            return true;
        }

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
            return golden && Add(s, station, false);
        }

        public static void Advance(GameState s, int station, double seconds, Func<double> roll = null, Action<int> onProduct = null)
        {
            Size(s);
            if (!(seconds > 0d) || station < 0 || station >= s.TrayWork.Length) return;
            s.TrayWork[station] += seconds;
            while (s.TrayWork[station] >= ProductEvery)
            {
                if (!CanTake(s, station))
                {
                    s.TrayWork[station] = ProductEvery;
                    return;
                }
                s.TrayWork[station] -= ProductEvery;
                bool golden = roll != null && s.GoldenToday < GoldenPerDay && roll() < GoldenChance;
                if (golden) s.GoldenToday++;
                Add(s, station, golden);
                onProduct?.Invoke(station);
            }
        }

        public static BigNum Price(IReadOnlyList<StationDef> defs, GameState s, int station, Modifiers m)
        {
            StationDef d = defs[station];
            if (d.Kind != StationKind.Brew || d.CycleSeconds <= 0d) return BigNum.Zero;
            return Economy.IncomePerCycle(defs, s, station) / d.CycleSeconds * (ProductSeconds * m.IncomeMultiplier * m.SpeedMultiplier);
        }

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

        public static void FillAway(IReadOnlyList<StationDef> defs, GameState s, double seconds)
        {
            if (!(seconds > 0d)) return;
            for (int i = 0; i < defs.Count; i++)
            {
                if (defs[i].Kind != StationKind.Brew || s.Levels[i] <= 0 || defs[i].CycleSeconds <= 0d || Layout.IsStored(defs, s, i)) continue;
                Advance(s, i, seconds);
            }
        }

        public static int Count(GameState s)
        {
            int n = 0;
            foreach (Product p in s.Tray) n += p.Count;
            return n;
        }
    }
}

using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Intros
    {
        public const string Fishing = "fishing", Pounding = "pounding", Leaves = "leaves", Fireflies = "fireflies", Orders = "orders",
            Shop = "shop", MoonJar = "moonjar", Layout = "layout", Rain = "rain", NewMoon = "newmoon";

        public static readonly string[] All = { Fishing, Pounding, Leaves, Fireflies, Orders, Shop, MoonJar, Layout, Rain, NewMoon };

        public const double RewardSeconds = 60d;
        public const int RewardClovers = 1;
        public const double GapSeconds = 90d;

        public static bool Known(string id) => Array.IndexOf(All, id) >= 0;

        public static bool HasReward(string id) => Known(id) && id != NewMoon;

        public static bool IsOpen(GameState s, string id)
        {
            switch (id)
            {
                case Fishing: return VuonTra.Core.Fishing.Unlocked(s);
                case Pounding: return VuonTra.Core.Pounding.Unlocked(s);
                case Leaves: return VuonTra.Core.Leaves.Unlocked(s);
                case Fireflies: return VuonTra.Core.Fireflies.Unlocked(s);
                case Orders: return s.NextOrderId > 0;
                case Shop: return Tutorial.IsDone(s);
                case MoonJar: return MoonJars.Shown(s);
                case Layout: return Tutorial.IsDone(s) && s.Grid != null && s.Grid.Segments >= 1;
                case Rain: return FrogRabbit.CanCome(s);
                case NewMoon: return s.Season > 0 || VuonTra.Core.NewMoon.CanBegin(s);
                default: return false;
            }
        }

        public static string Next(GameState s)
        {
            foreach (string id in All)
                if (!s.IntrosSeen.Contains(id) && IsOpen(s, id)) return id;
            return null;
        }

        public static bool Show(GameState s, string id)
        {
            if (!Known(id) || s.IntrosSeen.Contains(id) || !IsOpen(s, id)) return false;
            s.IntrosSeen.Add(id);
            return true;
        }

        public static List<string> Opened(GameState s)
        {
            var opened = new List<string>();
            foreach (string id in All) if (s.IntrosSeen.Contains(id)) opened.Add(id);
            return opened;
        }

        public static bool Later(GameState s, string id)
        {
            if (!s.IntrosSeen.Contains(id) || s.IntrosTried.Contains(id)) return false;
            return s.IntrosLater.Add(id);
        }

        public static bool HasDot(GameState s, string id) => s.IntrosLater.Contains(id);

        public static bool Touch(GameState s, string id) => s.IntrosLater.Remove(id);

        public static BigNum Reward(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
            => Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * RewardSeconds);

        public static bool Use(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, string id, out BigNum paid, out int clovers)
        {
            paid = BigNum.Zero;
            clovers = 0;
            if (!Known(id) || s.IntrosTried.Contains(id) || !IsOpen(s, id)) return false;
            s.IntrosTried.Add(id);
            s.IntrosSeen.Add(id);
            s.IntrosLater.Remove(id);
            if (!HasReward(id)) return false;
            paid = Reward(defs, s, m);
            clovers = RewardClovers;
            s.Coins += paid;
            s.Clovers += clovers;
            return true;
        }

        public static void Settle(GameState s)
        {
            if (s.IntrosKnown) return;
            s.IntrosKnown = true;
            foreach (string id in All)
            {
                if (!IsOpen(s, id)) continue;
                s.IntrosSeen.Add(id);
                s.IntrosTried.Add(id);
            }
        }
    }

    public sealed class IntroQueue
    {
        public double Wait { get; private set; }

        public void Reset() => Wait = 0d;

        public string Tick(GameState s, double dt, bool blocked)
        {
            if (dt > 0d) Wait = Math.Max(0d, Wait - dt);
            if (blocked || Wait > 0d) return null;
            string id = Intros.Next(s);
            if (id == null || !Intros.Show(s, id)) return null;
            Wait = Intros.GapSeconds;
            return id;
        }
    }
}

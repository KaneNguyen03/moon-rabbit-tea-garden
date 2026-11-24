using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum UpgradeEffect
    {
        TapSeconds,
        Speed,
        IncomeMultiplier,
        AwayHours
    }

    public sealed class UpgradeDef
    {
        public string Id;
        public string Name;
        public string Description;
        public UpgradeEffect Effect;
        public double Amount;
        public double BaseCost;
        public double CostGrowth;
        public int MaxLevel;
    }

    public struct Modifiers
    {
        public double IncomeMultiplier;
        public double SpeedMultiplier;
        public double TapSeconds;
        public double AwayCapSeconds;
        public double AwayIncomeMultiplier;

        public static readonly Modifiers None = new Modifiers
        {
            IncomeMultiplier = 1d, SpeedMultiplier = 1d,
            TapSeconds = Simulation.TapSeconds, AwayCapSeconds = Simulation.AwayCapSeconds,
            AwayIncomeMultiplier = 1d
        };

        public static double GardenMultiplier(GameState s) => s == null ? 1d : NewMoon.Bonus(s) * RestSpots.Multiplier(s) * Aquarium.Multiplier(s);

        public static Modifiers From(IReadOnlyList<UpgradeDef> upgrades, GameState s)
        {
            Modifiers m = None;
            m.SpeedMultiplier += Items.GardenBonus(s, ItemEffect.GardenSpeed);
            m.AwayIncomeMultiplier += Items.GardenBonus(s, ItemEffect.AwayIncome);
            m.IncomeMultiplier *= GardenMultiplier(s);
            if (upgrades == null || s.UpgradeLevels == null) return m;
            for (int i = 0; i < upgrades.Count && i < s.UpgradeLevels.Length; i++)
            {
                int level = s.UpgradeLevels[i];
                if (level <= 0) continue;
                UpgradeDef u = upgrades[i];
                switch (u.Effect)
                {
                    case UpgradeEffect.TapSeconds: m.TapSeconds += u.Amount * level; break;
                    case UpgradeEffect.Speed: m.SpeedMultiplier += u.Amount * level; break;
                    case UpgradeEffect.IncomeMultiplier: m.IncomeMultiplier *= Math.Pow(u.Amount, level); break;
                    case UpgradeEffect.AwayHours: m.AwayCapSeconds += u.Amount * 3600d * level; break;
                }
            }
            return m;
        }
    }

    public static class Upgrades
    {
        public static BigNum CostOfNext(UpgradeDef u, int level)
        {
            return new BigNum(u.CostGrowth).Pow(level) * u.BaseCost;
        }

        public static bool IsMaxed(UpgradeDef u, int level) => level >= u.MaxLevel;

        public static double EffectAt(UpgradeDef u, int level)
        {
            int n = Math.Max(0, level);
            switch (u.Effect)
            {
                case UpgradeEffect.IncomeMultiplier: return Math.Pow(u.Amount, n);
                default: return u.Amount * n;
            }
        }

        public static bool TryBuy(IReadOnlyList<UpgradeDef> upgrades, GameState s, int index)
        {
            if (index < 0 || index >= upgrades.Count || index >= s.UpgradeLevels.Length) return false;
            UpgradeDef u = upgrades[index];
            int level = s.UpgradeLevels[index];
            if (IsMaxed(u, level)) return false;
            BigNum cost = CostOfNext(u, level);
            if (s.Coins < cost) return false;
            s.Coins -= cost;
            s.UpgradeLevels[index] = level + 1;
            return true;
        }

        public static int OwnedLevels(GameState s)
        {
            int total = 0;
            if (s.UpgradeLevels != null)
                for (int i = 0; i < s.UpgradeLevels.Length; i++) total += s.UpgradeLevels[i];
            return total;
        }
    }
}

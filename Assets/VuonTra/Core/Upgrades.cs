using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum UpgradeEffect
    {
        /// <summary>Each level adds Amount seconds of work to one tap.</summary>
        TapSeconds,
        /// <summary>Each level makes every station Amount (0.1 = 10%) faster.</summary>
        Speed,
        /// <summary>Each level multiplies every station's income by Amount.</summary>
        IncomeMultiplier,
        /// <summary>Each level adds Amount hours to how long an absence keeps paying.</summary>
        AwayHours
    }

    /// <summary>A garden-wide upgrade ("bùa") bought with carrots.</summary>
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

    /// <summary>What the owned upgrades add up to. Without upgrades it is the base game.</summary>
    public struct Modifiers
    {
        public double IncomeMultiplier;
        public double SpeedMultiplier;
        public double TapSeconds;
        public double AwayCapSeconds;
        /// <summary>Extra on top of normal income for time away (1 = none).</summary>
        public double AwayIncomeMultiplier;

        public static readonly Modifiers None = new Modifiers
        {
            IncomeMultiplier = 1d, SpeedMultiplier = 1d,
            TapSeconds = Simulation.TapSeconds, AwayCapSeconds = Simulation.AwayCapSeconds,
            AwayIncomeMultiplier = 1d
        };

        public static Modifiers From(IReadOnlyList<UpgradeDef> upgrades, GameState s)
        {
            Modifiers m = None;
            // Items worn by working rabbits count whether or not there are upgrades.
            m.SpeedMultiplier += Items.GardenBonus(s, ItemEffect.GardenSpeed);
            m.AwayIncomeMultiplier += Items.GardenBonus(s, ItemEffect.AwayIncome);
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

        /// <summary>
        /// What the charm does at a level, as the number its effect is measured in: seconds per tap, the share
        /// of extra speed, the income multiplier, or hours of away time. Level 0 is the base game (0, 0, 1, 0).
        /// </summary>
        public static double EffectAt(UpgradeDef u, int level)
        {
            int n = Math.Max(0, level);
            switch (u.Effect)
            {
                case UpgradeEffect.IncomeMultiplier: return Math.Pow(u.Amount, n);
                default: return u.Amount * n;
            }
        }

        /// <summary>Buys one level. Leaves the state untouched when maxed or too expensive.</summary>
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

using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// The garden's own level: one number that says how far the player has come. Experience comes from
    /// growing stations, passing their milestones, calling rabbits, meeting new kinds and finishing quests.
    /// Every level reached brings one four-leaf clover, so a growing garden can always call another rabbit.
    /// </summary>
    public static class Garden
    {
        public const int XpPerLevelBought = 1, XpStationOpened = 10, XpMilestone = 15, XpSummon = 5, XpNewKind = 20, XpQuest = 10;
        /// <summary>
        /// Going from level L to L + 1 takes XpBase + XpStep * L. Gentle at the start and steeper after: experience comes
        /// fastest in the first minutes, when everything is cheap, so a steep curve would hand out the first levels in
        /// seconds; with these numbers a casual player sees level 5 around minute 7, level 10 on the first evening and
        /// level 15 (the last plots) on day three (EconomySimulationTests).
        /// </summary>
        public const int XpBase = 100, XpStep = 14;
        public const int MaxLevel = 99;
        public const int CloversPerLevel = 1;

        /// <summary>Experience needed to go from this level to the next.</summary>
        public static double XpToNext(int level) => XpBase + XpStep * (double)Math.Max(1, level);

        /// <summary>Total experience at which this level begins (level 1 begins at 0).</summary>
        public static double XpForLevel(int level)
        {
            int l = Math.Max(1, Math.Min(MaxLevel, level));
            return XpBase * (double)(l - 1) + XpStep * (l - 1) * (double)l / 2d;
        }

        public static int LevelOf(double xp)
        {
            if (!(xp > 0d)) return 1;
            int level = 1;
            while (level < MaxLevel && XpForLevel(level + 1) <= xp) level++;
            return level;
        }

        public static int Level(GameState s) => LevelOf(s.GardenXp);

        /// <summary>How far through the current level, from 0 to 1.</summary>
        public static double Progress(GameState s)
        {
            int level = Level(s);
            if (level >= MaxLevel) return 1d;
            double into = s.GardenXp - XpForLevel(level);
            return Math.Max(0d, Math.Min(1d, into / XpToNext(level)));
        }

        /// <summary>Adds experience; each level gained brings its clover. Returns how many levels were gained.</summary>
        public static int AddXp(GameState s, double amount)
        {
            if (!(amount > 0d)) return 0;
            int before = Level(s);
            s.GardenXp += amount;
            int gained = Level(s) - before;
            if (gained > 0)
            {
                s.Clovers += gained * CloversPerLevel;
                s.GoldenCarrots += Shop.GoldenForLevels(before, Level(s)); // every fifth level brings golden carrots
            }
            return gained;
        }

        /// <summary>How many of a station's milestones lie in (from, to].</summary>
        public static int MilestonesCrossed(int from, int to)
        {
            int n = 0;
            for (int i = 0; i < Economy.Milestones.Length; i++)
                if (Economy.Milestones[i] > from && Economy.Milestones[i] <= to) n++;
            return n;
        }

        /// <summary>
        /// For a save from before gardens had levels: the experience its progress would have earned, and never
        /// less than the level its land already needs. No clovers are handed out for it.
        /// </summary>
        public static double BackfillXp(IReadOnlyList<StationDef> defs, GameState s)
        {
            double xp = s.LevelsBought * (double)XpPerLevelBought;
            for (int i = 0; i < s.Levels.Length; i++)
            {
                if (s.Levels[i] <= 0) continue;
                if (i > 0) xp += XpStationOpened; // the first station is open from the start
                xp += MilestonesCrossed(0, s.Levels[i]) * XpMilestone;
            }
            xp += s.Rabbits.Count * XpSummon + s.SeenKinds.Count * XpNewKind + s.Claimed.Count * XpQuest;
            return Math.Max(xp, XpForLevel(Expansion.LevelForPlots(defs, s.Plots)));
        }
    }

    /// <summary>One widening of the garden: two more plots and one more place to rest, for a garden that has grown enough.</summary>
    public sealed class ExpansionWave
    {
        /// <summary>How many plots the garden has once this wave is taken.</summary>
        public int PlotsAfter;
        public int NeedLevel, NeedRabbits, NeedKinds;
    }

    /// <summary>
    /// The garden grows in waves from the middle outward. Each wave asks for a garden level, for rabbits
    /// (or kinds met), and for carrots: the price of the plots it opens, by the plot rule.
    /// </summary>
    public static class Expansion
    {
        public static readonly ExpansionWave[] Waves =
        {
            new ExpansionWave { PlotsAfter = 6,  NeedLevel = 3,  NeedRabbits = 3 },
            new ExpansionWave { PlotsAfter = 8,  NeedLevel = 6,  NeedRabbits = 6 },
            new ExpansionWave { PlotsAfter = 10, NeedLevel = 10, NeedRabbits = 10 },
            new ExpansionWave { PlotsAfter = 12, NeedLevel = 15, NeedKinds = 8 },
        };

        /// <summary>Index of the next wave this garden can take, or -1 when it has all its land.</summary>
        public static int NextIndex(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (s.Plots >= defs.Count) return -1;
            for (int i = 0; i < Waves.Length; i++)
                if (Waves[i].PlotsAfter > s.Plots) return i;
            return -1;
        }

        public static ExpansionWave Next(IReadOnlyList<StationDef> defs, GameState s)
        {
            int i = NextIndex(defs, s);
            return i < 0 ? null : Waves[i];
        }

        /// <summary>How many waves a garden with this many plots has already taken.</summary>
        public static int WavesTaken(int plots)
        {
            // Holding any plot beyond the wave before means this wave was taken (old saves could buy plots one at a time).
            int n = 0, before = Plots.Starting;
            for (int i = 0; i < Waves.Length; i++)
            {
                if (plots > before) n = i + 1;
                before = Waves[i].PlotsAfter;
            }
            return n;
        }

        /// <summary>The garden level a garden with this many plots must have had.</summary>
        public static int LevelForPlots(IReadOnlyList<StationDef> defs, int plots)
        {
            int taken = WavesTaken(plots);
            return taken <= 0 ? 1 : Waves[taken - 1].NeedLevel;
        }

        /// <summary>Carrots for the next wave: every plot it opens, each at a quarter of its station's price.</summary>
        public static BigNum Cost(IReadOnlyList<StationDef> defs, GameState s)
        {
            ExpansionWave wave = Next(defs, s);
            if (wave == null) return BigNum.Zero;
            BigNum cost = BigNum.Zero;
            for (int i = s.Plots; i < wave.PlotsAfter && i < defs.Count; i++) cost += new BigNum(defs[i].BaseCost * Plots.CostShare);
            return cost;
        }

        public static bool HasLevel(ExpansionWave w, GameState s) => Garden.Level(s) >= w.NeedLevel;
        public static bool HasRabbits(ExpansionWave w, GameState s) => s.Rabbits.Count >= w.NeedRabbits && s.SeenKinds.Count >= w.NeedKinds;
        public static bool HasCarrots(IReadOnlyList<StationDef> defs, GameState s) => s.Coins >= Cost(defs, s);

        public static bool CanExpand(IReadOnlyList<StationDef> defs, GameState s)
        {
            ExpansionWave wave = Next(defs, s);
            return wave != null && HasLevel(wave, s) && HasRabbits(wave, s) && HasCarrots(defs, s);
        }

        /// <summary>Takes the next wave: pays, opens its plots, and opens one more place to rest.</summary>
        public static bool TryExpand(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!CanExpand(defs, s)) return false;
            int index = NextIndex(defs, s);
            s.Coins -= Cost(defs, s);
            s.Plots = Math.Min(defs.Count, Waves[index].PlotsAfter);
            s.RestSpots = Math.Max(s.RestSpots, Math.Min(RestSpots.Max, RestSpots.Starting + index + 1));
            GardenGrid.Settle(defs, s); // the new plots' stations and the new place to rest take their tiles; the lawn gains a ring (#52)
            return true;
        }
    }
}

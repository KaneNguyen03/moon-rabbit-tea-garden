using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Garden
    {
        public const int XpPerLevelBought = 1, XpStationOpened = 10, XpMilestone = 15, XpSummon = 5, XpNewKind = 20, XpQuest = 10;
        public const int XpBase = 100, XpStep = 14;
        public const int MaxLevel = 99;
        public const int CloversPerLevel = 1;

        public static double XpToNext(int level) => XpBase + XpStep * (double)Math.Max(1, level);

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

        public static double Progress(GameState s)
        {
            int level = Level(s);
            if (level >= MaxLevel) return 1d;
            double into = s.GardenXp - XpForLevel(level);
            return Math.Max(0d, Math.Min(1d, into / XpToNext(level)));
        }

        public static int AddXp(GameState s, double amount)
        {
            if (!(amount > 0d)) return 0;
            amount *= NewMoon.Bonus(s);
            int before = Level(s);
            s.GardenXp += amount;
            int gained = Level(s) - before;
            if (gained > 0)
            {
                s.Clovers += gained * CloversPerLevel;
                s.GoldenCarrots += Shop.GoldenForLevels(Math.Max(before, NewMoon.LevelPassed(s)), Level(s));
                if (MoonJars.TenthLevels(before, Level(s)) > 0) MoonJars.GiveFree(s, s.DailyDay);
            }
            return gained;
        }

        public static int MilestonesCrossed(int from, int to)
        {
            int n = 0;
            for (int i = 0; i < Economy.Milestones.Length; i++)
                if (Economy.Milestones[i] > from && Economy.Milestones[i] <= to) n++;
            return n;
        }

        public static double BackfillXp(IReadOnlyList<StationDef> defs, GameState s)
        {
            double xp = s.LevelsBought * (double)XpPerLevelBought;
            for (int i = 0; i < s.Levels.Length; i++)
            {
                if (s.Levels[i] <= 0) continue;
                if (i > 0) xp += XpStationOpened;
                xp += MilestonesCrossed(0, s.Levels[i]) * XpMilestone;
            }
            xp += s.Rabbits.Count * XpSummon + s.SeenKinds.Count * XpNewKind + s.Claimed.Count * XpQuest;
            return Math.Max(xp, XpForLevel(Expansion.LevelForPlots(defs, s.Plots)));
        }
    }

    public sealed class MistSegment
    {
        public int SegmentsAfter;
        public int NeedLevel, NeedRabbits, NeedKinds;
        public GardenSide Side => GardenGrid.SideOf(SegmentsAfter);
        public int Tiles => GardenGrid.TilesPerSegment;
    }

    public static class Expansion
    {
        public const int PlotsPerSegment = 2;
        public const int LevelStepBeyond = 5;

        public static readonly MistSegment[] Table =
        {
            new MistSegment { SegmentsAfter = 1, NeedLevel = 3 },
            new MistSegment { SegmentsAfter = 2, NeedLevel = 6,  NeedRabbits = 6 },
            new MistSegment { SegmentsAfter = 3, NeedLevel = 10, NeedRabbits = 10 },
            new MistSegment { SegmentsAfter = 4, NeedLevel = 16, NeedKinds = 8 },
            new MistSegment { SegmentsAfter = 5, NeedLevel = 16, NeedKinds = 8 },
            new MistSegment { SegmentsAfter = 6, NeedLevel = 20, NeedKinds = 8 },
        };

        public static MistSegment Segment(int segmentsAfter)
        {
            if (segmentsAfter < 1 || segmentsAfter > GardenGrid.MaxSegments) return null;
            if (segmentsAfter <= Table.Length) return Table[segmentsAfter - 1];
            MistSegment last = Table[Table.Length - 1];
            return new MistSegment
            {
                SegmentsAfter = segmentsAfter,
                NeedLevel = last.NeedLevel + LevelStepBeyond * (segmentsAfter - last.SegmentsAfter),
                NeedRabbits = last.NeedRabbits,
                NeedKinds = last.NeedKinds,
            };
        }

        public static int Segments(GameState s)
        {
            int byPlots = GardenGrid.SegmentsForPlots(s.Plots);
            return s.Grid != null ? Math.Max(s.Grid.Segments, byPlots) : byPlots;
        }

        public static int PlotsForSegments(IReadOnlyList<StationDef> defs, int segments) =>
            Math.Min(defs.Count, Plots.Starting + PlotsPerSegment * Math.Max(0, segments));

        public static int LastSegments(IReadOnlyList<StationDef> defs) => GardenGrid.SegmentsForPlots(defs.Count);

        public static MistSegment Next(IReadOnlyList<StationDef> defs, GameState s)
        {
            int open = Segments(s);
            return open >= LastSegments(defs) ? null : Segment(open + 1);
        }

        public static int WavesTaken(int plots) => plots <= Plots.Starting ? 0 : (plots - Plots.Starting + PlotsPerSegment - 1) / PlotsPerSegment;

        public static int LevelForPlots(IReadOnlyList<StationDef> defs, int plots)
        {
            MistSegment segment = Segment(WavesTaken(plots));
            return segment == null ? 1 : segment.NeedLevel;
        }

        public static BigNum CostOfNext(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (Next(defs, s) == null) return BigNum.Zero;
            BigNum cost = BigNum.Zero;
            for (int i = s.Plots; i < PlotsForSegments(defs, Segments(s) + 1); i++) cost += new BigNum(defs[i].BaseCost * Plots.CostShare);
            return cost;
        }

        public static bool HasLevel(MistSegment segment, GameState s) => Garden.Level(s) >= segment.NeedLevel;
        public static bool HasRabbits(MistSegment segment, GameState s) => s.Rabbits.Count >= segment.NeedRabbits && s.SeenKinds.Count >= segment.NeedKinds;
        public static bool HasCarrots(IReadOnlyList<StationDef> defs, GameState s) => s.Coins >= CostOfNext(defs, s);

        public static bool CanExpand(IReadOnlyList<StationDef> defs, GameState s)
        {
            MistSegment segment = Next(defs, s);
            return segment != null && HasLevel(segment, s) && HasRabbits(segment, s) && HasCarrots(defs, s);
        }

        public static bool Expand(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!CanExpand(defs, s)) return false;
            s.Coins -= CostOfNext(defs, s);
            GardenGrid grid = s.Grid ?? (s.Grid = new GardenGrid());
            int before = s.Plots;
            grid.Segments = Segments(s) + 1;
            s.Plots = Math.Max(s.Plots, PlotsForSegments(defs, grid.Segments));
            s.RestSpots = Math.Max(s.RestSpots, Math.Min(RestSpots.Max, RestSpots.Starting + Segments(s)));
            for (int i = before; i < s.Plots && i < defs.Count; i++)
            {
                string id = GardenGrid.StationId(defs[i]);
                Hex home = GardenGrid.DefaultStationTile(i);
                if (grid.TileOf(id) == null && grid.InLawn(home) && grid.OccupantAt(home.Q, home.R) == null) grid.Place(id, home.Q, home.R);
            }
            for (int i = before; i < s.Plots && i < defs.Count; i++)
            {
                string id = GardenGrid.StationId(defs[i]);
                if (grid.TileOf(id) == null && grid.NearestEmptyApart(StationSpacing, out Hex spare)) grid.Place(id, spare.Q, spare.R);
            }
            GardenGrid.Settle(defs, s);
            return true;
        }

        public const int StationSpacing = 2;
    }
}

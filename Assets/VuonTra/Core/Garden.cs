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
        /// level 16 (the last of the first twelve plots) on day three (EconomySimulationTests, on the median of seventeen seeds).
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

        /// <summary>Adds experience; each level gained brings its clover, every fifth golden carrots, every tenth a moon jar. Returns how many levels were gained.</summary>
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
                // Every tenth level brings the day's free moon jar (#64), when it has not come today and the shelf has room.
                if (MoonJars.TenthLevels(before, Level(s)) > 0) MoonJars.GiveFree(s, s.DailyDay);
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

    /// <summary>
    /// One segment of the strip coming out of the mist (the horizontal garden, which replaced the forest's rings of #53): the
    /// lawn widens by <see cref="GardenGrid.TilesPerSegment"/> tiles on one side, opening the land of the next two stations and
    /// one more place to rest, for a garden that has grown enough. Segment n asks what the ring R (3 + n) of the vertical
    /// garden asked, so the pace is the same.
    /// </summary>
    public sealed class MistSegment
    {
        /// <summary>How many segments are open once this one is: 1 for the first.</summary>
        public int SegmentsAfter;
        public int NeedLevel, NeedRabbits, NeedKinds;
        /// <summary>Which side of the strip it opens: right for the odd ones, left for the even ones.</summary>
        public GardenSide Side => GardenGrid.SideOf(SegmentsAfter);
        /// <summary>The tiles it opens: three a row on its side.</summary>
        public int Tiles => GardenGrid.TilesPerSegment;
    }

    /// <summary>
    /// The garden widens segment by segment: when it has grown enough the player presses "Mở rộng rừng", the mist at one end
    /// of the strip clears and the lawn is three tiles a row wider there. Each segment asks for a garden level, for rabbits (or
    /// kinds met), and for carrots: <see cref="Plots.CostShare"/> of the price of the stations its land makes room for. The lawn
    /// starts as the middle of the strip with <see cref="Plots.Starting"/> plots, and each segment brings
    /// <see cref="PlotsPerSegment"/> more, so the segments are the old land waves and the forest's rings (#53): the first
    /// around minute four, the fourth the last of the first twelve stations, on day three. Past the table every segment asks
    /// <see cref="LevelStepBeyond"/> levels more; segments open as far as the catalog has stations for (<see cref="LastSegments"/>).
    /// </summary>
    public static class Expansion
    {
        public const int PlotsPerSegment = 2;
        public const int LevelStepBeyond = 5;

        /// <summary>
        /// The segments the garden opens first, with the levels, rabbits and kinds of the rings R 4 … R 9 they replace (#53): the
        /// first asks for no rabbits (the owner's note on #53); three come in the first minute anyway, so it still opens around
        /// minute four.
        /// </summary>
        public static readonly MistSegment[] Table =
        {
            new MistSegment { SegmentsAfter = 1, NeedLevel = 3 },                    // was R 4
            new MistSegment { SegmentsAfter = 2, NeedLevel = 6,  NeedRabbits = 6 },  // was R 5
            new MistSegment { SegmentsAfter = 3, NeedLevel = 10, NeedRabbits = 10 }, // was R 6
            // The fourth asks for level 16, not the 15 of the old land wave (#83): at 15 a casual player had all twelve plots on
            // day two on about half the seeds; at 16 on day three on every one.
            new MistSegment { SegmentsAfter = 4, NeedLevel = 16, NeedKinds = 8 },    // was R 7
            // The segments for the four stations after the star observatory (#67b): each asks for the garden level of the first
            // station it makes room for (16 for the lotus garden, 20 for the rice paddy), not the five more of the rule below
            // (20 and 25), so a casual player opens the thirteenth station on day four or five and the sixteenth by day nine to
            // twelve. Past these the rule takes over again: the seventh asks for 25, the eighth for 30.
            new MistSegment { SegmentsAfter = 5, NeedLevel = 16, NeedKinds = 8 },    // was R 8
            new MistSegment { SegmentsAfter = 6, NeedLevel = 20, NeedKinds = 8 },    // was R 9
        };

        /// <summary>The segment that brings the open count to <paramref name="segmentsAfter"/>; past the table, five levels more each time. Null for no such segment.</summary>
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

        /// <summary>How many segments are open: the grid's own count, and never fewer than the plots held call for.</summary>
        public static int Segments(GameState s)
        {
            int byPlots = GardenGrid.SegmentsForPlots(s.Plots);
            return s.Grid != null ? Math.Max(s.Grid.Segments, byPlots) : byPlots;
        }

        /// <summary>How many plots a lawn with this many segments open holds: four to start, two more a segment, never more than there are stations.</summary>
        public static int PlotsForSegments(IReadOnlyList<StationDef> defs, int segments) =>
            Math.Min(defs.Count, Plots.Starting + PlotsPerSegment * Math.Max(0, segments));

        /// <summary>The most segments open for now: the one that gives every station of the catalog its land.</summary>
        public static int LastSegments(IReadOnlyList<StationDef> defs) => GardenGrid.SegmentsForPlots(defs.Count);

        /// <summary>The next segment this garden can open, or null when it has all its land.</summary>
        public static MistSegment Next(IReadOnlyList<StationDef> defs, GameState s)
        {
            int open = Segments(s);
            return open >= LastSegments(defs) ? null : Segment(open + 1);
        }

        /// <summary>
        /// How many land waves a garden with this many plots has taken, which is how many segments it has open (an old save's
        /// land converts this way: 4 plots is none, 6 is one, 8 two, 10 three, 12 four). Holding any plot beyond the wave before
        /// means the wave was taken (old saves could buy plots one at a time).
        /// </summary>
        public static int WavesTaken(int plots) => plots <= Plots.Starting ? 0 : (plots - Plots.Starting + PlotsPerSegment - 1) / PlotsPerSegment;

        /// <summary>The garden level a garden with this many plots must have had.</summary>
        public static int LevelForPlots(IReadOnlyList<StationDef> defs, int plots)
        {
            MistSegment segment = Segment(WavesTaken(plots));
            return segment == null ? 1 : segment.NeedLevel;
        }

        /// <summary>
        /// Carrots for the next segment: <see cref="Plots.CostShare"/> of what each station it makes room for costs, so the land
        /// for the next two stations costs a quarter of their price, as a land wave and a ring did (the pace stays as it was).
        /// </summary>
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

        /// <summary>
        /// Opens the next segment: pays, clears the mist from three more tiles a row on its side, opens the land of the next
        /// stations and one more place to rest. Each new station stands on its own tile from the garden's layout; if that tile
        /// is taken, on the empty tile nearest the teapot that keeps clear of the other stations.
        /// </summary>
        public static bool Expand(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!CanExpand(defs, s)) return false;
            s.Coins -= CostOfNext(defs, s);
            GardenGrid grid = s.Grid ?? (s.Grid = new GardenGrid());
            int before = s.Plots;
            grid.Segments = Segments(s) + 1;
            s.Plots = Math.Max(s.Plots, PlotsForSegments(defs, grid.Segments));
            s.RestSpots = Math.Max(s.RestSpots, Math.Min(RestSpots.Max, RestSpots.Starting + Segments(s)));
            // The stations whose own tile is free stand first, so one that must look elsewhere keeps clear of them too.
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
            GardenGrid.Settle(defs, s); // the new place to rest takes its tile, and anything still without one
            return true;
        }

        /// <summary>A station's art is about two tiles wide, so a station placed for the player keeps this many tiles from every other.</summary>
        public const int StationSpacing = 2;
    }
}

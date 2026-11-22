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
    /// One ring of the forest stepping back (#53): the lawn widens from <see cref="RadiusAfter"/> - 1 rings to
    /// <see cref="RadiusAfter"/>, opening that ring's 6 × <see cref="RadiusAfter"/> tiles, the land of the next two
    /// stations and one more place to rest, for a garden that has grown enough.
    /// </summary>
    public sealed class ForestRing
    {
        /// <summary>How many rings wide the lawn is once this ring is opened.</summary>
        public int RadiusAfter;
        public int NeedLevel, NeedRabbits, NeedKinds;
        /// <summary>The tiles this ring opens: going from R to R + 1 opens 6R + 6.</summary>
        public int Tiles => 6 * RadiusAfter;
    }

    /// <summary>
    /// The garden widens ring by ring (#53): when it has grown enough the player presses "Mở rộng rừng", the forest
    /// at the lawn's edge steps back and the lawn is one ring wider. Each ring asks for a garden level, for rabbits
    /// (or kinds met), and for carrots: <see cref="Plots.CostShare"/> of the price of the stations its land makes room for.
    /// The lawn starts <see cref="GardenGrid.StartingRadius"/> rings wide with <see cref="Plots.Starting"/> plots, and each
    /// ring brings <see cref="PlotsPerRing"/> more, so the rings are the old land waves: R 3 → 4 is the first, around
    /// minute four, R 6 → 7 the last of today's twelve stations, on day three. Past the table every ring asks
    /// <see cref="LevelStepBeyond"/> levels more; rings open as far as the catalog has stations for (<see cref="LastRadius"/>).
    /// </summary>
    public static class Expansion
    {
        public const int PlotsPerRing = 2;
        public const int LevelStepBeyond = 5;

        /// <summary>
        /// The rings the garden opens first, re-based on the hex grid's three starting rings (#52): the issue's
        /// R 2 → 3 … 5 → 6 are R 3 → 4 … 6 → 7 here, with the levels and rabbits of the land waves they replace. The first asks
        /// for no rabbits (the owner's note on #53); three come in the first minute anyway, so it still opens around minute four.
        /// </summary>
        public static readonly ForestRing[] Rings =
        {
            new ForestRing { RadiusAfter = 4, NeedLevel = 3 },
            new ForestRing { RadiusAfter = 5, NeedLevel = 6,  NeedRabbits = 6 },
            new ForestRing { RadiusAfter = 6, NeedLevel = 10, NeedRabbits = 10 },
            new ForestRing { RadiusAfter = 7, NeedLevel = 15, NeedKinds = 8 },
        };

        /// <summary>The ring that widens the lawn to <paramref name="radiusAfter"/> rings; past the table, five levels more each time. Null for no such ring.</summary>
        public static ForestRing Ring(int radiusAfter)
        {
            if (radiusAfter <= GardenGrid.StartingRadius || radiusAfter > GardenGrid.MaxRadius) return null;
            int i = radiusAfter - GardenGrid.StartingRadius - 1;
            if (i < Rings.Length) return Rings[i];
            ForestRing last = Rings[Rings.Length - 1];
            return new ForestRing
            {
                RadiusAfter = radiusAfter,
                NeedLevel = last.NeedLevel + LevelStepBeyond * (radiusAfter - last.RadiusAfter),
                NeedRabbits = last.NeedRabbits,
                NeedKinds = last.NeedKinds,
            };
        }

        /// <summary>How many rings wide the lawn is: the grid's own count, and never less than the plots held call for.</summary>
        public static int Radius(GameState s)
        {
            int byPlots = GardenGrid.RadiusForPlots(s.Plots);
            return s.Grid != null ? Math.Max(s.Grid.Radius, byPlots) : byPlots;
        }

        /// <summary>How many rings the garden has opened since it was new.</summary>
        public static int RingsTaken(GameState s) => Math.Max(0, Radius(s) - GardenGrid.StartingRadius);

        /// <summary>How many plots a lawn this many rings wide holds: four to start, two more for every ring, never more than there are stations.</summary>
        public static int PlotsForRadius(IReadOnlyList<StationDef> defs, int radius) =>
            Math.Min(defs.Count, Plots.Starting + PlotsPerRing * Math.Max(0, radius - GardenGrid.StartingRadius));

        /// <summary>The widest the lawn goes for now: the ring that gives every station of the catalog its land.</summary>
        public static int LastRadius(IReadOnlyList<StationDef> defs) => Math.Min(GardenGrid.MaxRadius, GardenGrid.RadiusForPlots(defs.Count));

        /// <summary>The next ring this garden can open, or null when it has all its land.</summary>
        public static ForestRing Next(IReadOnlyList<StationDef> defs, GameState s)
        {
            int radius = Radius(s);
            return radius >= LastRadius(defs) ? null : Ring(radius + 1);
        }

        /// <summary>
        /// How many rings a garden with this many plots has already opened (an old save's land converts to rings this way:
        /// 4 plots is R 3, 6 is R 4, 8 is R 5, 10 is R 6, 12 is R 7). Holding any plot beyond the ring before means the ring
        /// was opened (old saves could buy plots one at a time).
        /// </summary>
        public static int WavesTaken(int plots) => plots <= Plots.Starting ? 0 : (plots - Plots.Starting + PlotsPerRing - 1) / PlotsPerRing;

        /// <summary>The garden level a garden with this many plots must have had.</summary>
        public static int LevelForPlots(IReadOnlyList<StationDef> defs, int plots)
        {
            ForestRing ring = Ring(GardenGrid.StartingRadius + WavesTaken(plots));
            return ring == null ? 1 : ring.NeedLevel;
        }

        /// <summary>
        /// Carrots for the next ring: <see cref="Plots.CostShare"/> of what each station it makes room for costs, so the land
        /// for the next two stations costs a quarter of their price, as a land wave did (the pace stays as it was, #53).
        /// </summary>
        public static BigNum CostOfNext(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (Next(defs, s) == null) return BigNum.Zero;
            BigNum cost = BigNum.Zero;
            for (int i = s.Plots; i < PlotsForRadius(defs, Radius(s) + 1); i++) cost += new BigNum(defs[i].BaseCost * Plots.CostShare);
            return cost;
        }

        public static bool HasLevel(ForestRing ring, GameState s) => Garden.Level(s) >= ring.NeedLevel;
        public static bool HasRabbits(ForestRing ring, GameState s) => s.Rabbits.Count >= ring.NeedRabbits && s.SeenKinds.Count >= ring.NeedKinds;
        public static bool HasCarrots(IReadOnlyList<StationDef> defs, GameState s) => s.Coins >= CostOfNext(defs, s);

        public static bool CanExpand(IReadOnlyList<StationDef> defs, GameState s)
        {
            ForestRing ring = Next(defs, s);
            return ring != null && HasLevel(ring, s) && HasRabbits(ring, s) && HasCarrots(defs, s);
        }

        /// <summary>
        /// Opens the next ring: pays, widens the lawn by one ring, opens the land of the next stations and one more place to
        /// rest. Each new station stands on its own tile from the garden's layout; if that tile is taken, on the empty tile
        /// nearest the centre that keeps clear of the other stations (until the player can lay the garden out, #54).
        /// </summary>
        public static bool Expand(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!CanExpand(defs, s)) return false;
            s.Coins -= CostOfNext(defs, s);
            GardenGrid grid = s.Grid ?? (s.Grid = new GardenGrid());
            int before = s.Plots;
            grid.Radius = Radius(s) + 1;
            s.Plots = Math.Max(s.Plots, PlotsForRadius(defs, grid.Radius));
            s.RestSpots = Math.Max(s.RestSpots, Math.Min(RestSpots.Max, RestSpots.Starting + RingsTaken(s)));
            for (int i = before; i < s.Plots && i < defs.Count; i++)
            {
                string id = GardenGrid.StationId(defs[i]);
                if (grid.TileOf(id) != null) continue;
                Hex home = GardenGrid.DefaultStationTile(i);
                if (grid.InLawn(home) && grid.OccupantAt(home.Q, home.R) == null) { grid.Place(id, home.Q, home.R); continue; }
                if (grid.NearestEmptyApart(StationSpacing, out Hex spare)) grid.Place(id, spare.Q, spare.R);
            }
            GardenGrid.Settle(defs, s); // the new place to rest takes its tile, and anything still without one
            return true;
        }

        /// <summary>A station's art is about two tiles wide, so a station placed for the player keeps this many tiles from every other.</summary>
        public const int StationSpacing = 2;
    }
}

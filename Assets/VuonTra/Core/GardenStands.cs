using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum StandRole
    {
        Rabbit,
        Station,
        Counter,
        LampPost,
        Aquarium,
        Mortar,
        Tree,
        Dressing,
    }

    public enum StandLook
    {
        Land,
        Unbuilt,
        Open,
    }

    public readonly struct StandRange
    {
        public readonly double Low, High;
        public StandRange(double low, double high) { Low = low; High = high; }
        public double Middle => (Low + High) / 2d;
        public double Clamp(double ratio) => ratio < Low ? Low : ratio > High ? High : ratio;
        public bool Holds(double ratio) => ratio >= Low * (1d - GardenStands.Give) && ratio <= High * (1d + GardenStands.Give);
    }

    public readonly struct StationPair
    {
        public readonly int From, To;
        public StationPair(int from, int to) { From = from; To = to; }
    }

    public static class GardenStands
    {
        public const double Give = 0.15d;

        public static readonly StationPair[] Paths =
        {
            new StationPair(0, 4), new StationPair(0, 6), new StationPair(0, 2), new StationPair(0, 7),
            new StationPair(4, 9), new StationPair(2, 11),
        };

        public static StandRange Range(StandRole role) => role switch
        {
            StandRole.Station => new StandRange(0.8d, 1.3d),
            StandRole.Counter => new StandRange(1.8d, 2.2d),
            StandRole.LampPost => new StandRange(1.4d, 1.4d),
            StandRole.Aquarium => new StandRange(0.9d, 0.9d),
            StandRole.Mortar => new StandRange(0.7d, 0.7d),
            StandRole.Tree => new StandRange(2.5d, 3.5d),
            StandRole.Dressing => new StandRange(0.3d, 0.7d),
            _ => new StandRange(1d, 1d),
        };

        public static double Metres(StandRole role, double ratio, double rabbitMetres) => Range(role).Clamp(ratio) * rabbitMetres;

        public static int Shown(IReadOnlyList<StationDef> defs, GameState s)
        {
            int open = 0;
            for (int i = 0; i < defs.Count; i++) if (s.Levels[i] > 0) open = i + 1;
            return Math.Min(defs.Count, Math.Min(s.Plots + 1, open + 2));
        }

        public static bool Standing(IReadOnlyList<StationDef> defs, GameState s, int shown, int index) =>
            index >= 0 && index < shown && !Layout.IsStored(defs, s, index);

        public static StandLook Look(GameState s, int index) =>
            index >= s.Plots ? StandLook.Land : s.Levels[index] <= 0 ? StandLook.Unbuilt : StandLook.Open;

        public static Hex StationTile(IReadOnlyList<StationDef> defs, GameState s, int index) =>
            s.Grid != null && s.Grid.TryTileOf(defs[index].GridId, out Hex tile) ? tile : GardenGrid.DefaultStationTile(index);

        public static Hex PropTile(GameState s, string id) =>
            s.Grid != null && s.Grid.TryTileOf(id, out Hex tile) ? tile : GardenGrid.DefaultTile(id, null);

        public static void Workers(GameState s, long[] into)
        {
            Array.Clear(into, 0, into.Length);
            foreach (Rabbit rabbit in s.Rabbits)
                if (rabbit.Station >= 0 && rabbit.Station < into.Length && into[rabbit.Station] == 0) into[rabbit.Station] = rabbit.Id;
        }

        public static GroundPoint Aside(GroundPoint from, GroundPoint to, double aside)
        {
            double dx = to.X - from.X, dz = to.Z - from.Z, length = Math.Sqrt(dx * dx + dz * dz);
            var mid = new GroundPoint((from.X + to.X) / 2d, (from.Z + to.Z) / 2d);
            if (length <= 0d) return mid;
            double px = -dz / length, pz = dx / length;
            if (px * mid.X + pz * mid.Z < 0d) { px = -px; pz = -pz; }
            return new GroundPoint(mid.X + px * aside, mid.Z + pz * aside);
        }

        public static void Stones(GroundPoint from, GroundPoint to, double step, double clear, List<GroundPoint> into)
        {
            double dx = to.X - from.X, dz = to.Z - from.Z, length = Math.Sqrt(dx * dx + dz * dz);
            if (step <= 0d || length <= 2d * clear) return;
            int count = (int)Math.Floor((length - 2d * clear) / step) + 1;
            double used = (count - 1) * step, start = (length - used) / 2d;
            for (int n = 0; n < count; n++)
            {
                double t = (start + n * step) / length;
                into.Add(new GroundPoint(from.X + dx * t, from.Z + dz * t));
            }
        }
    }
}

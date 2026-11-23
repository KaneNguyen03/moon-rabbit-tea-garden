using System.Collections.Generic;

namespace VuonTra.Core
{
    public static class Plots
    {
        public const int Starting = 4;
        public const double CostShare = 0.25;

        public static bool CanBuy(IReadOnlyList<StationDef> defs, GameState s) => Expansion.Next(defs, s) != null;

        public static BigNum CostOfNext(IReadOnlyList<StationDef> defs, GameState s) => Expansion.CostOfNext(defs, s);

        public static bool TryBuy(IReadOnlyList<StationDef> defs, GameState s) => Expansion.Expand(defs, s);
    }
}

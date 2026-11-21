using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>
    /// Garden plots: a station can only be opened on a plot the player owns. The garden starts with a few
    /// and widens in waves (see <see cref="Expansion"/>); these are the plain "buy land" questions the
    /// screens ask, answered by the wave that is next.
    /// </summary>
    public static class Plots
    {
        public const int Starting = 4;
        /// <summary>A plot costs this share of what the station that will stand on it costs.</summary>
        public const double CostShare = 0.25;

        public static bool CanBuy(IReadOnlyList<StationDef> defs, GameState s) => Expansion.Next(defs, s) != null;

        public static BigNum CostOfNext(IReadOnlyList<StationDef> defs, GameState s) => Expansion.Cost(defs, s);

        public static bool TryBuy(IReadOnlyList<StationDef> defs, GameState s) => Expansion.TryExpand(defs, s);
    }
}

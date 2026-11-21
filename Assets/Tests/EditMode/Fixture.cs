using System.Collections.Generic;

namespace VuonTra.Core.Tests
{
    /// <summary>Three small stations shared by the tests: a brew, an ingredient that feeds it, a second brew.</summary>
    static class Fixture
    {
        public static StationDef Tea() => new StationDef
        {
            Id = "tea", Name = "Tea", Kind = StationKind.Brew,
            BaseCost = 10, CostGrowth = 1.09, BaseIncome = 1, CycleSeconds = 1.5
        };

        public static StationDef Leaf() => new StationDef
        {
            Id = "leaf", Name = "Leaf", Kind = StationKind.Ingredient,
            BaseCost = 120, CostGrowth = 1.11, BoostsIds = new[] { "tea" }
        };

        public static StationDef Jam() => new StationDef
        {
            Id = "jam", Name = "Jam", Kind = StationKind.Brew,
            BaseCost = 1400, CostGrowth = 1.12, BaseIncome = 80, CycleSeconds = 5
        };

        public static List<StationDef> Defs() => new List<StationDef> { Tea(), Leaf(), Jam() };
    }
}

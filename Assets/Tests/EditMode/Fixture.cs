using System.Collections.Generic;

namespace VuonTra.Core.Tests
{
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

        public static List<StationDef> TradeDefs()
        {
            List<StationDef> defs = Defs();
            defs[0].Trade = RabbitTrade.Brewer;
            defs[1].Trade = RabbitTrade.Gatherer;
            defs[2].Trade = RabbitTrade.Baker;
            return defs;
        }

        public static string CodeFor(int seed)
        {
            var rng = new System.Random(seed ^ 0x5eed);
            var chars = new char[Profile.CodeLength];
            for (int i = 0; i < chars.Length; i++) chars[i] = Profile.CodeAlphabet[rng.Next(Profile.CodeAlphabet.Length)];
            return Profile.CodePrefix + new string(chars);
        }
    }
}

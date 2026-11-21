using System;

namespace VuonTra.Core
{
    public enum StationKind
    {
        /// <summary>Makes something to sell: has an income and a cycle time.</summary>
        Brew,
        /// <summary>Sells nothing; raises the price of the Brew stations it supplies.</summary>
        Ingredient
    }

    /// <summary>The fixed description of one station. Player progress lives in GameState.</summary>
    public sealed class StationDef
    {
        public string Id;
        public string Name;
        public StationKind Kind;
        public double BaseCost;
        public double CostGrowth;
        /// <summary>Brew only: sương per cycle at level 1.</summary>
        public double BaseIncome;
        /// <summary>Brew only: seconds per cycle.</summary>
        public double CycleSeconds;
        /// <summary>Ingredient only: ids of the Brew stations it boosts.</summary>
        public string[] BoostsIds = Array.Empty<string>();
        /// <summary>The trade this station belongs to; a rabbit of that trade is worth more here. None gives nobody a bonus.</summary>
        public RabbitTrade Trade;

        public bool Boosts(string brewId)
        {
            if (BoostsIds == null) return false;
            for (int i = 0; i < BoostsIds.Length; i++)
                if (BoostsIds[i] == brewId) return true;
            return false;
        }
    }
}

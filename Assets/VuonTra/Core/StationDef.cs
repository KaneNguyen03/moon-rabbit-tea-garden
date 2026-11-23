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
        /// <summary>
        /// Ingredient only: how much of <see cref="Economy.IngredientBoost(int)"/>'s rise this station gives, 1 for all of it.
        /// The two after the star observatory give 7 % (#83): at full strength they stacked onto the top brews and made
        /// day seven's income over a hundred times what it was before them.
        /// </summary>
        public double BoostScale = 1d;
        /// <summary>The trade this station belongs to; a rabbit of that trade is worth more here. None gives nobody a bonus.</summary>
        public RabbitTrade Trade;
        /// <summary>The garden level a station asks for before it can be opened, besides its price and its plot (#67); 0 asks for none.</summary>
        public int NeedGardenLevel;

        public bool Boosts(string brewId)
        {
            if (BoostsIds == null) return false;
            for (int i = 0; i < BoostsIds.Length; i++)
                if (BoostsIds[i] == brewId) return true;
            return false;
        }
    }
}

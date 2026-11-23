using System;

namespace VuonTra.Core
{
    public enum StationKind
    {
        Brew,
        Ingredient
    }

    public sealed class StationDef
    {
        public string Id;
        public string Name;
        public StationKind Kind;
        public double BaseCost;
        public double CostGrowth;
        public double BaseIncome;
        public double CycleSeconds;
        public string[] BoostsIds = Array.Empty<string>();
        public double BoostScale = 1d;
        public RabbitTrade Trade;
        public int NeedGardenLevel;

        string _gridId, _gridIdOf;

        public string GridId
        {
            get
            {
                if (_gridId == null || !ReferenceEquals(_gridIdOf, Id))
                {
                    _gridId = GardenGrid.StationId(this);
                    _gridIdOf = Id;
                }
                return _gridId;
            }
        }

        public bool Boosts(string brewId)
        {
            if (BoostsIds == null) return false;
            for (int i = 0; i < BoostsIds.Length; i++)
                if (BoostsIds[i] == brewId) return true;
            return false;
        }
    }
}

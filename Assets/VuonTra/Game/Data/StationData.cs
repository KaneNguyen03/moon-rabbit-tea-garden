using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    /// <summary>One station as an asset: the rules the core needs plus how it looks.</summary>
    [CreateAssetMenu(menuName = "VuonTra/Station", fileName = "Station")]
    public sealed class StationData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public StationKind Kind;
        public double BaseCost = 10;
        public double CostGrowth = 1.1;
        public double BaseIncome = 1;
        public double CycleSeconds = 2;
        [Tooltip("Ingredient only: the Brew stations this one makes more valuable.")]
        public StationData[] Boosts = new StationData[0];
        public Sprite Prop;
        [Tooltip("This station's own rabbit, already dressed. Empty: the shared rabbit with a scarf in the accent colour.")]
        public Sprite Rabbit;
        [Tooltip("Optional: frames of this rabbit at work, played in a loop. Empty: the still rabbit.")]
        public Sprite[] WorkFrames = new Sprite[0];
        public Color Accent = Color.white;

        public StationDef ToDef()
        {
            var ids = new string[Boosts == null ? 0 : Boosts.Length];
            for (int i = 0; i < ids.Length; i++) ids[i] = Boosts[i] != null ? Boosts[i].Id : null;
            return new StationDef
            {
                Id = Id, Name = DisplayName, Kind = Kind,
                BaseCost = BaseCost, CostGrowth = CostGrowth,
                BaseIncome = BaseIncome, CycleSeconds = CycleSeconds,
                BoostsIds = ids
            };
        }
    }
}

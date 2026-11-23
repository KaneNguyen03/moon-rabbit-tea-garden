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
        public RabbitTrade Trade;
        public double BaseCost = 10;
        public double CostGrowth = 1.1;
        public double BaseIncome = 1;
        public double CycleSeconds = 2;
        [Tooltip("The garden level this station asks for before it can be opened (#67); 0 asks for none.")]
        public int NeedGardenLevel;
        [Tooltip("Ingredient only: the Brew stations this one makes more valuable.")]
        public StationData[] Boosts = new StationData[0];
        [Tooltip("Ingredient only: how much of the usual boost this station gives, 1 for all of it (#83).")]
        public double BoostScale = 1;
        public Sprite Prop;
        [Tooltip("What this station makes for the tray. Empty: the prop stands in for it.")]
        public Sprite Product;
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
                BoostsIds = ids,
                BoostScale = BoostScale,
                Trade = Trade,
                NeedGardenLevel = NeedGardenLevel
            };
        }
    }
}

using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    [CreateAssetMenu(menuName = "VuonTra/Upgrade", fileName = "Upgrade")]
    public sealed class UpgradeData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        [TextArea] public string Description;
        public UpgradeEffect Effect;
        public double Amount = 1;
        public double BaseCost = 100;
        public double CostGrowth = 2;
        public int MaxLevel = 10;
        [Tooltip("One or two characters shown on the card until it has real art.")]
        public string Glyph = "?";
        [Tooltip("The charm's picture; the glyph is only used while there is none.")]
        public Sprite Icon;
        public Color Accent = Color.white;

        public UpgradeDef ToDef() => new UpgradeDef
        {
            Id = Id, Name = DisplayName, Description = Description, Effect = Effect,
            Amount = Amount, BaseCost = BaseCost, CostGrowth = CostGrowth, MaxLevel = MaxLevel
        };
    }
}

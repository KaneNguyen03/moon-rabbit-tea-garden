using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    /// <summary>A kind of wearable item as an asset.</summary>
    [CreateAssetMenu(menuName = "VuonTra/Item", fileName = "Item")]
    public sealed class ItemData : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public ItemSlot Slot;
        public ItemEffect Effect;
        [Tooltip("Bonus at the lowest tier; 0.1 means +10%.")]
        public double BaseAmount = 0.1;
        public Sprite Icon;
        [Tooltip("Golden carrots the shop asks; 0 when it is not for sale.")]
        public int Price;
        [Tooltip("The tier a bought copy has.")]
        public int Tier;
        [Tooltip("How much higher than the usual line a worn hat of this kind sits, in shares of the rabbit drawing's height.")]
        public float HatLift;

        public ItemDef ToDef() => new ItemDef { Id = Id, Name = DisplayName, Slot = Slot, Effect = Effect, BaseAmount = BaseAmount, Price = Price, Tier = Tier, HatLift = HatLift };
    }
}

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

        public ItemDef ToDef() => new ItemDef { Id = Id, Name = DisplayName, Slot = Slot, Effect = Effect, BaseAmount = BaseAmount };
    }
}

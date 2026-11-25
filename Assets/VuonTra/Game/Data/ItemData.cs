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
        [Tooltip("The hat as it is worn, when that differs from the shop picture (the conical hat without its chin strap); empty uses Icon.")]
        public Sprite Worn;
        [Tooltip("How a worn hat sits on a head: its width, how far it comes down over the top of the skull and how far to the side, " +
                 "all in head widths; its lean in degrees; and whether the ears are drawn over it (they come through it).")]
        public HatSeat Seat = HatSeat.Default;

        public ItemDef ToDef() => new ItemDef { Id = Id, Name = DisplayName, Slot = Slot, Effect = Effect, BaseAmount = BaseAmount, Price = Price, Tier = Tier };
    }

    /// <summary>
    /// How a hat sits on a rabbit's head (owner's review 23 Nov: hats looked pasted on). Everything is measured from
    /// the kind's own head (<see cref="RabbitKind.HeadX"/>, <see cref="RabbitKind.HeadTop"/>, <see cref="RabbitKind.HeadWidth"/>),
    /// so one hat fits a dwarf rabbit and a lionhead alike.
    /// </summary>
    [System.Serializable]
    public struct HatSeat
    {
        [Tooltip("Width of the hat, in head widths.")]
        public float Width;
        [Tooltip("How far the hat's bottom edge comes below the top of the skull, in head widths.")]
        public float Sink;
        [Tooltip("Sideways from the middle of the head, in head widths; right is positive.")]
        public float Shift;
        [Tooltip("Lean in degrees, anticlockwise positive; most kinds look to the right, so hats lean that way (negative).")]
        public float Tilt;
        [Tooltip("The ears are drawn over the hat, as if they came through it (a knitted hat); otherwise they stand behind it.")]
        public bool EarsOver;

        public HatSeat(float width, float sink, float shift = 0f, float tilt = 0f, bool earsOver = false)
        {
            Width = width; Sink = sink; Shift = shift; Tilt = tilt; EarsOver = earsOver;
        }

        /// <summary>A plain hat sitting on the crown, the ears behind it.</summary>
        public static readonly HatSeat Default = new HatSeat(0.95f, 0.18f, 0f, -6f);
    }
}

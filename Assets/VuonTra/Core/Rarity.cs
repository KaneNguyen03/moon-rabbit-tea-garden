using System;

namespace VuonTra.Core
{
    /// <summary>
    /// The one scale of rarity that rabbits and items share: six tiers, common to rare, and for each a
    /// count of stars so the tier reads without its colour (one star for silver, six for red).
    /// </summary>
    public static class Rarity
    {
        public const int Count = 6;
        public const char Star = '★';

        public static int Clamp(int tier) => Math.Max(0, Math.Min(Count - 1, tier));

        /// <summary>How many stars a tier wears: its index plus one.</summary>
        public static int Stars(int tier) => Clamp(tier) + 1;

        /// <summary>The stars as text, "★★★" for the third tier.</summary>
        public static string Marks(int tier) => new string(Star, Stars(tier));
    }
}

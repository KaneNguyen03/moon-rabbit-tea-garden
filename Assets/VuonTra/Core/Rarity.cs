using System;

namespace VuonTra.Core
{
    public static class Rarity
    {
        public const int Count = 6;
        public const char Star = '★';

        public static int Clamp(int tier) => Math.Max(0, Math.Min(Count - 1, tier));

        public static int Stars(int tier) => Clamp(tier) + 1;

        public static string Marks(int tier) => new string(Star, Stars(tier));
    }
}

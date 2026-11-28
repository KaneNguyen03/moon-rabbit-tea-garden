using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public const string FavouriteKey = "pet.favourite";
        public const int FavouriteSize = 32;
        static readonly Vector2 FavouriteRise = new Vector2(70f, 150f);
        static readonly Vector2 FavouriteHeart = new Vector2(150f, 40f);

        string BondLine(Rabbit r) => Runner.Likes.TryGet(Rabbits.KindOf(r), out Liking like)
            ? L10n.T(Personalities.HeartKey(like.Personality))
            : L10n.T("bond.line." + Rabbits.TradeOf(r));

        void FavouriteCue(Rabbit r, Vector2 at)
        {
            if (!Runner.Likes.LovesStation(Runner.Defs, r)) return;
            Feel.FloatText(at + FavouriteHeart, "♥", Palette.Blush, 42);
            Feel.FloatText(at + FavouriteRise, L10n.T(FavouriteKey), Palette.Blush, FavouriteSize);
        }
    }
}

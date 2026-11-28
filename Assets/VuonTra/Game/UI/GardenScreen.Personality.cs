using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        string BondLine(Rabbit r) => Runner.Likes.TryGet(Rabbits.KindOf(r), out Liking like)
            ? L10n.T(Personalities.HeartKey(like.Personality))
            : L10n.T("bond.line." + Rabbits.TradeOf(r));
    }
}

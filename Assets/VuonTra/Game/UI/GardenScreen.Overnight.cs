using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public const float StoryGainRise = 90f;

        void OnStory(Story story, RectTransform row)
        {
            StoryClaim claim = Runner.ClaimStory(story);
            if (!claim.Ok)
            {
                Sfx.Deny();
                return;
            }
            AwayCard.MarkTaken(story);
            Refresh(-1f);
            Vector2 from = Feel.PointOf(row);
            Rewarded(from, new Haul { Clovers = claim.Clovers }, Fanfare.Gift);
            GameCatalog catalog = Runner.Catalog;
            if (claim.Leaves > 0)
            {
                RectTransform chip = _leafChip;
                Feel.Fly(from, Feel.PointOf(chip), LeafIcon(catalog), LeafGold, claim.Leaves, 34f, () =>
                {
                    if (chip == null) return;
                    Feel.FloatBelow(UnderHud(chip), L10n.T("overnight.leaves.gain", claim.Leaves), LeafGold, 34);
                    PopChip(chip, 0.12f);
                });
            }
            if (claim.Bond > 0) Feel.FloatText(from + new Vector2(0f, StoryGainRise), L10n.T("overnight.bond.gain", claim.Bond), Palette.Blush, 40);
            if (claim.Kind == StoryKind.Doze) Sfx.Pet();
            if (claim.Arrived == null) return;
            Sfx.Summon(claim.Arrived.Tier);
            Welcome(claim.Arrived);
        }
    }
}

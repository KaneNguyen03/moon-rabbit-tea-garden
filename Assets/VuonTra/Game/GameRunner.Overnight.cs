using System.Collections.Generic;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        public IReadOnlyList<Story> OvernightWaiting => State.Overnight.Waiting;

        void TellDropped(AwayReport away)
        {
            if (away.Dropped == null) return;
            foreach (Story story in away.Dropped) Analytics.LogOvernightStory(story, false);
        }

        public StoryClaim ClaimStory(Story story)
        {
            StoryClaim claim = Overnight.Claim(State, story, PickName());
            if (!claim.Ok) return claim;
            Analytics.LogOvernightStory(story, true);
            Analytics.LogRecipeLeaves(claim.Leaves, AnalyticsEvents.FromOvernight);
            if (claim.Arrived != null)
            {
                Analytics.LogRabbitCalled(AnalyticsEvents.ViaOvernight, claim.Arrived);
                Rabbits.AutoAssign(Defs, State);
            }
            Changed();
            return claim;
        }
    }
}

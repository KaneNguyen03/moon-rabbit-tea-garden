using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        readonly DishWatch _dishWatch = new DishWatch();

        AwayReport ClaimAway()
        {
            int leaves = State.RecipeLeaves;
            AwayReport away = Away.Claim(Defs, State, Now, Mods, TodayNumber);
            Analytics.LogRecipeLeaves(State.RecipeLeaves - leaves, AnalyticsEvents.FromAway);
            return away;
        }

        public void ChooseIntroTry(string id)
        {
            Analytics.LogIntroTry(id, AnalyticsEvents.FromCard);
            TouchIntro(id);
        }

        public bool TapIntroDot(string id)
        {
            if (!Intros.HasDot(State, id)) return false;
            Analytics.LogIntroTry(id, AnalyticsEvents.FromDot);
            return TouchIntro(id);
        }
    }
}

using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        public bool TankOpen => Fishing.Unlocked(State);

        public bool CanReleaseFish(int fish) => Aquarium.CanRelease(State, fish);

        public ReleaseResult ReleaseFish(int fish)
        {
            ReleaseResult result = Aquarium.Release(State, fish);
            Analytics.LogTankRelease(State, result);
            if (result.Released) Changed();
            return result;
        }

        public void SeeTankFresh()
        {
            if (!Aquarium.AnyFresh(State)) return;
            Aquarium.SeeFresh(State);
            Save();
        }

        public bool TankCardWaiting => Aquarium.CardWaiting(State);

        public void SeeTankCard()
        {
            if (!Aquarium.CardWaiting(State)) return;
            Aquarium.SeeCard(State);
            Save();
        }
    }
}

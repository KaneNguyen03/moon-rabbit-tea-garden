using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        public WelcomeBackGift LastWelcomeBack { get; private set; }

        bool CheckWelcomeBack()
        {
            WelcomeBackGift gift = WelcomeBack.Check(State, DayClock());
            if (!gift.Ok) return false;
            LastWelcomeBack = gift;
            Analytics.LogWelcomeBack(gift);
            return true;
        }

        public bool WelcomeBackActive => WelcomeBack.Active(State, TodayNumber);

        public bool WelcomeBackCanCheckIn => WelcomeBack.CanCheckIn(State, TodayNumber);

        public int WelcomeBackPillDay => WelcomeBack.PillDay(State, TodayNumber);

        public bool WelcomeBackCardWaiting => WelcomeBack.CardWaiting(State) && WelcomeBackActive;

        public void SeeWelcomeBackCard()
        {
            if (!WelcomeBack.CardWaiting(State)) return;
            WelcomeBack.SeeCard(State);
            Save();
        }

        public WelcomeBackGift CheckInWelcomeBack()
        {
            WelcomeBackGift gift = WelcomeBack.CheckIn(State, TodayNumber, PickName(), Newcomer.ShownKindRoll);
            if (gift.Ok) Changed();
            return gift;
        }
    }
}

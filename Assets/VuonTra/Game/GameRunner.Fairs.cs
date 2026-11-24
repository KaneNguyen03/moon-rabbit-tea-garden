using System;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        public event Action<int> FairCoinsEarned;

        public FairRun? FairNow => Fairs.Open(State, TodayNumber);

        public bool FairJoinedNow => FairNow is FairRun run && run.Start == State.Fair.Start;

        public bool FairCardWaiting => Fairs.CardWaiting(State, TodayNumber);

        public void SeeFairCard()
        {
            Fairs.SeeCard(State, TodayNumber);
            Save();
        }

        bool CheckFair()
        {
            long before = State.Fair.Start;
            Letter letter = Fairs.Roll(Defs, State, Mods, TodayNumber);
            return letter != null || State.Fair.Start != before;
        }

        void EarnFair(int coins)
        {
            int earned = Fairs.Earn(Defs, State, Mods, TodayNumber, coins);
            if (earned > 0) FairCoinsEarned?.Invoke(earned);
        }

        public FairBuy BuyAtFair(int stall)
        {
            FairBuy result = Fairs.Buy(Defs, State, Mods, TodayNumber, stall, PickName());
            if (!result.Ok) return result;
            if (result.Rabbit != null) Rabbits.AutoAssign(Defs, State);
            Changed();
            return result;
        }

        public bool HangFairLantern(int fair)
        {
            bool ok = Fairs.HangLantern(State, fair);
            if (ok) Changed();
            return ok;
        }

        public bool WearFairFrame(int fair)
        {
            bool ok = Fairs.WearFrame(State, fair);
            if (ok) Changed();
            return ok;
        }
    }
}

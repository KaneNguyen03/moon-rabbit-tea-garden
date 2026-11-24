using System;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        public Func<long> LocalClock = () => Trunk.LocalClock(DateTimeOffset.Now);

        public TrunkShelf TrunkShelf
        {
            get
            {
                long window = State.Trunk.Window;
                TrunkShelf shelf = Trunk.Visit(State, ItemDefs, LocalClock());
                if (State.Trunk.Window != window) Save();
                return shelf;
            }
        }

        public bool TrunkOpenNow => Trunk.IsOpen(State, LocalClock());

        public TrunkShelf OpenTrunk()
        {
            TrunkShelf shelf = TrunkShelf;
            Analytics.LogTrunkOpen(shelf);
            return shelf;
        }

        public TrunkBuy BuyFromTrunk(int slot)
        {
            TrunkBuy buy = Trunk.Buy(State, ItemDefs, LocalClock(), slot, PickName, _rng.NextDouble);
            if (!buy.Ok) return buy;
            Analytics.LogTrunkBuy(buy);
            if (buy.Rabbit != null) Analytics.LogRabbitCalled(buy.Offer.Kind == TrunkKind.GoldenCall ? AnalyticsEvents.ViaGolden : AnalyticsEvents.ViaTrunk, buy.Rabbit);
            if (buy.Second != null) Analytics.LogRabbitCalled(AnalyticsEvents.ViaGolden, buy.Second);
            _shopBought = buy.Rabbit != null;
            Changed();
            return buy;
        }
    }
}

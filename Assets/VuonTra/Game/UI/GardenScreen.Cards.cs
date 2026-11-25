using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        readonly CardPacer _cardPacer = new CardPacer();

        public bool CardShowing =>
            OfferCard.IsOpen || (ReviewCard != null && ReviewCard.IsOpen) || IntroShowing || NewDishCard.IsOpen || TankCard.IsOpen
            || FairCard.IsOpen || WelcomeCard.IsOpen || TrunkCard.IsOpen || AwayCard.IsOpen || Arrival.IsOpen || NewMoonMoment.IsOpen;

        public bool CardsRested => _cardPacer.Rested(_clock);

        public float CardWait => (float)_cardPacer.Wait(_clock);

        public void RestCards() => _cardPacer.Reset();

        void NoteCards()
        {
            if (OfferCard != null) _cardPacer.Note(_clock, CardShowing);
        }
    }
}

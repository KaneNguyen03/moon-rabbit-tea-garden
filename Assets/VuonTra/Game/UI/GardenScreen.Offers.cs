using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public OfferView OfferCard { get; private set; }
        Action<OfferGift> _onOfferDelivered;

        void BuildOffers(GameCatalog catalog)
        {
            OfferCard = new OfferView(_canvas, catalog, Tweens, (id, done) => Runner.BuyGolden(id, result =>
            {
                PurchaseRefused(result.Outcome);
                done?.Invoke(result);
            }));
            Shop.AttachOffer(OpenLeadingOffer);
            if (_onOfferDelivered != null) Runner.OfferDelivered -= _onOfferDelivered;
            _onOfferDelivered = OfferArrived;
            Runner.OfferDelivered += _onOfferDelivered;
        }

        public bool OfferCardFree =>
            Shell != null && Shell.State == ShellState.Playing && !PhotoMode && !LayoutMode && !SheetOpen && Selected < 0
            && Tutorial.IsDone(Runner.State) && !OfferCard.IsOpen
            && !Shop.IsOpen && !GoldenPacks.IsOpen && !Pound.IsOpen && !Basket.IsOpen && !Jar.IsOpen && !FishingCard.IsOpen
            && !AwayCard.IsOpen && !Arrival.IsOpen && !NewMoonCard.IsOpen && !NewMoonMoment.IsOpen && !CloudChoice.IsOpen
            && !Mail.IsOpen && !Profile.IsOpen && !Rename.IsOpen && !Picker.IsOpen && !Workers.IsOpen && !Detail.IsOpen
            && !UpgradeAll.IsOpen && !Income.IsOpen && !DecorCard.IsOpen && !OrderCard.IsOpen && !ReviewCard.IsOpen && !FirstWeek.IsOpen && !Prizes.IsOpen && !KindChooser.IsOpen && !WelcomeCard.IsOpen && !FairCard.IsOpen && !TrunkCard.IsOpen && !IntroShowing && !TankPanelsOpen
            && !NewDishCard.IsOpen && !Book.IsOpen;

        void RefreshOffers()
        {
            if (OfferCard == null) return;
            OfferDef leading = Runner.LeadingOffer;
            Shop.RefreshOffer(leading, leading != null ? Runner.OfferSecondsLeft(leading.Id) : 0L);
            if (OfferCard.IsOpen) OfferCard.Tick(Runner.OfferSecondsLeft(OfferCard.Offer.Id));
            else if (Runner.OfferCardsOn && leading != null && OfferCardFree)
            {
                OfferDef offer = Runner.TakeOfferToShow();
                if (offer != null) OpenOffer(offer);
            }
        }

        public void OpenOffer(OfferDef offer)
        {
            Runner.SeeOffer(offer.Id);
            OfferCard.Open(offer, Runner.OfferSecondsLeft(offer.Id), Runner.Store);
        }

        void OpenLeadingOffer()
        {
            OfferDef offer = Runner.LeadingOffer;
            if (offer != null) OpenOffer(offer);
        }

        void OfferArrived(OfferGift gift)
        {
            if (OfferCard == null) return;
            bool here = OfferCard.IsOpen && OfferCard.Offer != null && OfferCard.Offer.Id == gift.Id;
            if (here) OfferCard.ShowPaid();
            Vector2 from = here ? Feel.PointOf(OfferCard.Card) : new Vector2(0, 200);
            GiftArrived(from, new Haul { Golden = gift.Golden, Jars = gift.Jars, Clovers = gift.Clovers }, gift.Hat, Fanfare.Purchase);
            if (gift.Rabbit != null)
            {
                OfferCard.Close();
                if (Shop.IsOpen) Shop.Close();
                Welcome(gift.Rabbit);
            }
            Refresh(-1f);
        }
    }
}

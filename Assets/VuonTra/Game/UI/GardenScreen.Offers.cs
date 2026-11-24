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
            OfferCard = new OfferView(_canvas, catalog, Tweens, (id, done) => Runner.BuyGolden(id, done));
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
            && !UpgradeAll.IsOpen && !Income.IsOpen && !DecorCard.IsOpen && !OrderCard.IsOpen && !ReviewCard.IsOpen && !FirstWeek.IsOpen && !Prizes.IsOpen && !KindChooser.IsOpen && !WelcomeCard.IsOpen && !TrunkCard.IsOpen && !IntroShowing
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
            GiftArrived(from, gift.Golden, 0, gift.Hat);
            Vector2 below = Feel.PointOf(_goldenChip, new Vector2(0, -130));
            if (gift.Clovers > 0) Feel.FloatText(below, L10n.T("mail.clovers", gift.Clovers), Palette.Lantern, 34);
            if (gift.Jars > 0) Feel.FloatText(below + new Vector2(0, gift.Clovers > 0 ? -60 : 0), L10n.T("offer.jars.float", gift.Jars), Palette.Lantern, 34);
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

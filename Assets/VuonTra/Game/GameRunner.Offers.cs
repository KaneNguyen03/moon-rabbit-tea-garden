using System;
using System.Collections.Generic;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed partial class GameRunner
    {
        public event Action<OfferGift> OfferDelivered;

        public Func<long> OfferWallClock = () => LocalNow;

        static long LocalNow
        {
            get
            {
                DateTimeOffset local = DateTimeOffset.Now;
                return local.ToUnixTimeSeconds() + (long)local.Offset.TotalSeconds;
            }
        }

        public long OfferNow
        {
            get
            {
                Seasons.Clamp(State, DayClock());
                return Offers.Clock(State, OfferWallClock());
            }
        }

        public bool OfferCardsOn { get; set; }

        public List<OfferDef> ActiveOffers => Features.Monetization ? Offers.Active(State, OfferNow) : new List<OfferDef>();

        public OfferDef LeadingOffer => Features.Monetization ? Offers.Leading(State, OfferNow) : null;

        public long OfferSecondsLeft(string id) => Offers.SecondsLeft(State, id, OfferNow);

        public OfferDef TakeOfferToShow()
        {
            if (!Features.Monetization) return null;
            OfferDef offer = Offers.ToShow(State, OfferNow);
            return offer != null && SeeOffer(offer.Id) ? offer : null;
        }

        public bool SeeOffer(string id)
        {
            if (!Features.Monetization || !Offers.MarkShown(State, id)) return false;
            Analytics.LogOffer(AnalyticsEvents.OfferShown, Offers.Find(id));
            Save();
            return true;
        }

        bool NoteOffers()
        {
            if (State == null || !Features.Monetization) return false;
            OfferNews news = Offers.Tick(State, OfferNow);
            foreach (string id in news.Expired) Analytics.LogOffer(AnalyticsEvents.OfferExpired, Offers.Find(id));
            return news.Started.Count > 0 || news.Expired.Count > 0;
        }

        bool GrantPaid(PurchaseResult r, bool late)
        {
            if (!Storefront.Grant(State, r.PackId, r.TransactionId, ItemDefs, PickName(), _rng.NextDouble(), out OfferGift gift)) return false;
            _shopBought = true;
            OfferDef offer = Offers.Find(r.PackId);
            if (offer == null) return true;
            Analytics.LogOffer(AnalyticsEvents.OfferBought, offer, late);
            OfferDelivered?.Invoke(gift);
            return true;
        }
    }
}

using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public WelcomeBackView WelcomeCard { get; private set; }

        void BuildWelcomeBack() => WelcomeCard = new WelcomeBackView(_canvas, _hud, Runner, Tweens, OnWelcomeCheckIn);

        void RefreshWelcomeBack()
        {
            if (WelcomeCard == null) return;
            WelcomeCard.RefreshPill(_orderBadge.activeSelf);
            if (WelcomeCard.IsOpen) WelcomeCard.Refresh();
            else if (Runner.WelcomeBackCardWaiting && OfferCardFree)
            {
                Runner.SeeWelcomeBackCard();
                Sfx.Reward();
                WelcomeCard.Open();
            }
        }

        void OnWelcomeCheckIn()
        {
            WelcomeBackGift gift = Runner.CheckInWelcomeBack();
            if (!gift.Ok)
            {
                Sfx.Deny();
                return;
            }
            WelcomeCard.ShowClaimed(L10n.T("mail.claimed", WelcomeBackView.DayGiftOf(gift.Day)));
            Refresh(-1f);
            Rewarded(Feel.PointOf(WelcomeCard.Card), new Haul { Jars = gift.Jars, Clovers = gift.Clovers, Golden = gift.Golden }, Fanfare.Gift);
            if (gift.Rabbit == null) return;
            WelcomeCard.Close();
            Sfx.Summon(gift.Rabbit.Tier);
            Welcome(gift.Rabbit);
        }
    }
}

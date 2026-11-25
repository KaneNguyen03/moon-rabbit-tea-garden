using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        public FairView FairCard { get; private set; }
        Action<int> _onFairCoins;

        void BuildFair()
        {
            FairCard = new FairView(_canvas, _hud, Runner, Tweens, OnFairStall);
            Shop.AttachFair(() => { Shop.Close(); OpenFair(); });
            if (_onFairCoins != null) Runner.FairCoinsEarned -= _onFairCoins;
            _onFairCoins = FairCoinsArrived;
            Runner.FairCoinsEarned += _onFairCoins;
        }

        void RefreshFair()
        {
            if (FairCard == null) return;
            FairCard.RefreshChip();
            FairRun? now = Runner.FairNow;
            Shop.RefreshFair(now.HasValue ? now.Value.Def.NameKey : null);
            if (FairCard.IsOpen) FairCard.Refresh();
            else if (Runner.OfferCardsOn && Runner.FairCardWaiting && OfferCardFree)
            {
                Runner.SeeFairCard();
                Sfx.Reward();
                FairCard.Open();
            }
        }

        public void OpenFair()
        {
            FairCard.Open();
            if (FairCard.IsOpen) Runner.OpenedFeature(Intros.Fair);
        }

        void OpenFairFromIntro()
        {
            if (Runner.FairCardWaiting) Runner.SeeFairCard();
            OpenFair();
        }

        void FairCoinsArrived(int coins)
        {
            if (FairCard == null || !FairCard.ChipShown || !(Runner.FairNow is FairRun run)) return;
            Feel.FloatText(Feel.PointOf(FairCard.Chip, new Vector2(0, -70)), L10n.T("fair.earned", coins, L10n.T(run.Def.CoinKey)), Palette.Hex(run.Def.Hex), 30);
            RectTransform chip = FairCard.Chip;
            Tweens.Add(Motion.Tap, t => { if (chip != null) chip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (chip != null) chip.localScale = Vector3.one; });
        }

        void OnFairStall(int stall)
        {
            if (!(Runner.FairNow is FairRun run)) return;
            GameState s = Runner.State;
            FairStall ware = run.Def.Stalls[stall];
            Vector2 at = Feel.PointOf(FairCard.TileOf(stall), new Vector2(0, 170));
            string fairName = L10n.T(run.Def.NameKey);
            if (ware.Ware == FairWare.Lantern && Fairs.OwnsLantern(s, run.Index))
            {
                if (Runner.HangFairLantern(run.Index)) Feel.FloatText(at, L10n.T("fair.hung", fairName), Palette.Ink, 30);
                Refresh(-1f);
                return;
            }
            if (ware.Ware == FairWare.Frame && Fairs.OwnsFrame(s, run.Index))
            {
                if (Runner.WearFairFrame(run.Index)) Feel.FloatText(at, L10n.T("fair.framed", fairName), Palette.Ink, 30);
                Refresh(-1f);
                return;
            }
            FairBuy bought = Runner.BuyAtFair(stall);
            if (!bought.Ok)
            {
                Sfx.Deny();
                if (bought.Check == FairBuyCheck.Short)
                    Feel.FloatText(at, L10n.T("fair.short", ware.Price - FairView.CoinsOf(s, run), L10n.T(run.Def.CoinKey)), Palette.Ink, 30);
                FairCard.Refresh();
                return;
            }
            Rewarded(at, HaulOf(ware, bought), Fanfare.Gift);
            string name = ware.Ware == FairWare.Rabbit ? bought.Rabbit.Name : FairView.WareName(ware.Ware);
            Feel.FloatText(at, L10n.T("fair.bought", name), Palette.Hex(run.Def.Hex), 34);
            Refresh(-1f);
            if (bought.Rabbit == null) return;
            FairCard.Close();
            Sfx.Summon(bought.Rabbit.Tier);
            Welcome(bought.Rabbit);
        }

        static Haul HaulOf(FairStall ware, FairBuy bought)
        {
            switch (ware.Ware)
            {
                case FairWare.Jars: return new Haul { Jars = bought.Jars, Clovers = bought.JarClovers };
                case FairWare.Clovers: return new Haul { Clovers = ware.Amount };
                case FairWare.Golden: return new Haul { Golden = ware.Amount };
                default: return new Haul();
            }
        }

        void DecorateGate()
        {
            Sprite decor = Runner.Catalog.GateDecor(Runner.SeasonNow);
            if (decor == null && Runner.FairNow is FairRun run)
                Arrival.SetSeasonDecor(Runner.Catalog.GateMidAutumn, Palette.Mix(Color.white, Palette.Hex(run.Def.Hex), 0.85f));
            else Arrival.SetSeasonDecor(decor);
        }
    }
}

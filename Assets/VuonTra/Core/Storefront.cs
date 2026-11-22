using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    /// <summary>One pack of golden carrots sold for money (#9). The store's product id is the pack's id.</summary>
    public sealed class GoldenPack
    {
        public string Id;
        public int Golden;
        /// <summary>The pack the shelf marks as the best deal.</summary>
        public bool Best;
    }

    public enum PurchaseOutcome
    {
        /// <summary>The store took the money; the pack can be paid out.</summary>
        Paid,
        Cancelled,
        Failed,
        /// <summary>The store is not ready, or the pack is not for sale here.</summary>
        Unavailable,
        /// <summary>The store handed over a transaction the purse was already paid for.</summary>
        Duplicate
    }

    /// <summary>What a purchase came to: the pack, the store's transaction id (what keeps a re-delivered purchase from paying twice), and the outcome.</summary>
    public struct PurchaseResult
    {
        public PurchaseOutcome Outcome;
        public string PackId;
        public string TransactionId;

        public static PurchaseResult Of(PurchaseOutcome outcome, string packId, string transactionId = null)
            => new PurchaseResult { Outcome = outcome, PackId = packId, TransactionId = transactionId };
    }

    /// <summary>
    /// The seam to the platform store. The game asks for a price, asks to buy, asks to restore; the store answers
    /// later. Purchases the store delivers on its own (one left unfinished when the app died, say) come through
    /// <see cref="Delivered"/>. The game pays out through <see cref="Storefront.Grant"/>, never here.
    /// </summary>
    public interface IStorefront
    {
        /// <summary>The store is connected and the packs' prices are known.</summary>
        bool Ready { get; }
        /// <summary>The pack's price as the store words it, or null when unknown.</summary>
        string PriceOf(string packId);
        void Buy(string packId, Action<PurchaseResult> done);
        /// <summary>Asks the store for earlier purchases (App Store rule); consumable packs already paid out never come back.</summary>
        void Restore(Action<bool> done);
        /// <summary>A purchase the store hands over without being asked right now.</summary>
        event Action<PurchaseResult> Delivered;
    }

    /// <summary>
    /// A store in memory: every purchase succeeds at once, with a transaction id of its own, unless told to fail
    /// or cancel the next one. What the editor, the tests and every build without a real store run on.
    /// </summary>
    public sealed class FakeStorefront : IStorefront
    {
        public bool Ready { get; set; } = true;
        /// <summary>The next purchase ends this way instead of being paid.</summary>
        public PurchaseOutcome? NextOutcome;
        public readonly List<string> Bought = new List<string>();
        public int Restores;
        int _next;
        /// <summary>Placeholder prices, so the shelf can be looked at; the real ones are set on the stores by the owner.</summary>
        public readonly Dictionary<string, string> Prices = new Dictionary<string, string>
        {
            { Storefront.Small, "25.000 ₫" }, { Storefront.Medium, "69.000 ₫" }, { Storefront.Large, "199.000 ₫" },
            { Storefront.NoAdsPass, "75.000 ₫" } // the no-ads pass (#66), about 2,99 $
        };

        public event Action<PurchaseResult> Delivered;

        public string PriceOf(string packId) => Ready && Prices.TryGetValue(packId, out string p) ? p : null;

        public void Buy(string packId, Action<PurchaseResult> done)
        {
            if (!Ready || !Storefront.Sells(packId)) { done?.Invoke(PurchaseResult.Of(PurchaseOutcome.Unavailable, packId)); return; }
            if (NextOutcome.HasValue && NextOutcome.Value != PurchaseOutcome.Paid)
            {
                PurchaseOutcome outcome = NextOutcome.Value;
                NextOutcome = null;
                done?.Invoke(PurchaseResult.Of(outcome, packId));
                return;
            }
            NextOutcome = null;
            Bought.Add(packId);
            done?.Invoke(PurchaseResult.Of(PurchaseOutcome.Paid, packId, "fake-" + (++_next)));
        }

        public void Restore(Action<bool> done)
        {
            Restores++;
            done?.Invoke(Ready);
        }

        /// <summary>Hands over a purchase as a real store would after a restart: the tests use it.</summary>
        public void Deliver(string packId, string transactionId) => Delivered?.Invoke(PurchaseResult.Of(PurchaseOutcome.Paid, packId, transactionId));
    }

    /// <summary>
    /// The golden carrot packs (#9): three sizes, bought with money through the platform store and paid out once
    /// per transaction. Golden carrots are the only thing for sale; carrots, rabbits and clovers never are.
    /// </summary>
    public static class Storefront
    {
        public const string Small = "vuontra.golden.50", Medium = "vuontra.golden.150", Large = "vuontra.golden.500";

        public static readonly GoldenPack[] Packs =
        {
            new GoldenPack { Id = Small, Golden = 50 },
            new GoldenPack { Id = Medium, Golden = 150, Best = true },
            new GoldenPack { Id = Large, Golden = 500 },
        };

        /// <summary>
        /// The no-ads pass (#66): thirty days in which every rewarded-ad button pays at once, with the same daily caps.
        /// Sold only while ads are switched on (see GameRunner.AdsInPlayerBuilds); not a pack, it pays no golden carrots.
        /// </summary>
        public const string NoAdsPass = "vuontra.noads.30";

        /// <summary>Whether the store sells this id here: one of the golden packs, or the no-ads pass.</summary>
        public static bool Sells(string productId) => Find(productId) != null || productId == NoAdsPass;

        /// <summary>How many paid transaction ids the save remembers, so a purchase the store delivers again is not paid out twice.</summary>
        public const int ReceiptMemory = 64;

        public static GoldenPack Find(string packId)
        {
            for (int i = 0; i < Packs.Length; i++)
                if (Packs[i].Id == packId) return Packs[i];
            return null;
        }

        /// <summary>
        /// Pays a bought pack into the purse, once: false when the pack is unknown or that transaction was paid
        /// out already. A transaction without an id is paid out every time (the fake store always has one).
        /// </summary>
        public static bool Grant(GameState s, string packId, string transactionId)
        {
            GoldenPack pack = Find(packId);
            if (pack == null && packId != NoAdsPass) return false;
            if (!string.IsNullOrEmpty(transactionId))
            {
                if (s.PaidTransactions.Contains(transactionId)) return false;
                s.PaidTransactions.Add(transactionId);
                while (s.PaidTransactions.Count > ReceiptMemory) s.PaidTransactions.RemoveAt(0);
            }
            if (pack == null)
            {
                // The no-ads pass, from the clamped day the save last saw (#66).
                Ads.GrantPass(s, s.MaxDaySeen);
                return true;
            }
            s.GoldenCarrots += pack.Golden;
            s.PacksBought++;
            return true;
        }
    }
}

using System;
using System.Collections.Generic;

namespace VuonTra.Core
{
    public sealed class GoldenPack
    {
        public string Id;
        public int Golden;
        public bool Best;
    }

    public enum PurchaseOutcome
    {
        Paid,
        Cancelled,
        Failed,
        Unavailable,
        Duplicate
    }

    public struct PurchaseResult
    {
        public PurchaseOutcome Outcome;
        public string PackId;
        public string TransactionId;

        public static PurchaseResult Of(PurchaseOutcome outcome, string packId, string transactionId = null)
            => new PurchaseResult { Outcome = outcome, PackId = packId, TransactionId = transactionId };
    }

    public interface IStorefront
    {
        bool Ready { get; }
        string PriceOf(string packId);
        void Buy(string packId, Action<PurchaseResult> done);
        void Restore(Action<bool> done);
        event Action<PurchaseResult> Delivered;
    }

    public sealed class FakeStorefront : IStorefront
    {
        public bool Ready { get; set; } = true;
        public PurchaseOutcome? NextOutcome;
        public readonly List<string> Bought = new List<string>();
        public int Restores;
        int _next;
        public readonly Dictionary<string, string> Prices = new Dictionary<string, string>
        {
            { Storefront.Small, "25.000 ₫" }, { Storefront.Medium, "69.000 ₫" }, { Storefront.Large, "199.000 ₫" },
            { Storefront.NoAdsPass, "75.000 ₫" }
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

        public void Deliver(string packId, string transactionId) => Delivered?.Invoke(PurchaseResult.Of(PurchaseOutcome.Paid, packId, transactionId));
    }

    public static class Storefront
    {
        public const string Small = "vuontra.golden.50", Medium = "vuontra.golden.150", Large = "vuontra.golden.500";

        public static readonly GoldenPack[] Packs =
        {
            new GoldenPack { Id = Small, Golden = 50 },
            new GoldenPack { Id = Medium, Golden = 150, Best = true },
            new GoldenPack { Id = Large, Golden = 500 },
        };

        public const string NoAdsPass = "vuontra.noads.30";

        public static bool Sells(string productId) => Find(productId) != null || productId == NoAdsPass;

        public const int ReceiptMemory = 64;

        public static GoldenPack Find(string packId)
        {
            for (int i = 0; i < Packs.Length; i++)
                if (Packs[i].Id == packId) return Packs[i];
            return null;
        }

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
                Ads.GrantPass(s, s.MaxDaySeen);
                return true;
            }
            s.GoldenCarrots += pack.Golden;
            s.PacksBought++;
            return true;
        }
    }
}

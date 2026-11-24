#if UNITY_PURCHASING && (UNITY_IOS || UNITY_ANDROID)
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using VuonTra.Core;

namespace VuonTra.Game.Store
{
    public sealed class UnityStorefront : IStorefront, IDetailedStoreListener
    {
        public interface IReceiptCheck { bool Accept(Product product); }

        public sealed class AcceptAllReceipts : IReceiptCheck { public bool Accept(Product product) => product != null && product.hasReceipt; }

        readonly IReceiptCheck _check;
        IStoreController _controller;
        IExtensionProvider _extensions;
        readonly Dictionary<string, Action<PurchaseResult>> _waiting = new Dictionary<string, Action<PurchaseResult>>();
        readonly List<Product> _pending = new List<Product>();

        public bool Ready => _controller != null;
        public event Action<PurchaseResult> Delivered;

        public UnityStorefront(IReceiptCheck check = null)
        {
            _check = check ?? new AcceptAllReceipts();
            var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
            foreach (GoldenPack pack in Storefront.Packs) builder.AddProduct(pack.Id, ProductType.Consumable);
            builder.AddProduct(Storefront.NoAdsPass, ProductType.Consumable);
            foreach (OfferDef offer in Offers.All) builder.AddProduct(offer.Id, ProductType.NonConsumable);
            builder.AddProduct(MoonPass.ProductId, ProductType.Consumable);
            UnityPurchasing.Initialize(this, builder);
        }

        public string PriceOf(string packId)
        {
            Product p = _controller?.products.WithID(packId);
            return p != null && p.availableToPurchase ? p.metadata.localizedPriceString : null;
        }

        public void Buy(string packId, Action<PurchaseResult> done)
        {
            Product p = _controller?.products.WithID(packId);
            if (p == null || !p.availableToPurchase) { done?.Invoke(PurchaseResult.Of(PurchaseOutcome.Unavailable, packId)); return; }
            _waiting[packId] = done;
            _controller.InitiatePurchase(p);
        }

        public void Restore(Action<bool> done)
        {
            if (_extensions == null) { done?.Invoke(false); return; }
#if UNITY_IOS
            _extensions.GetExtension<IAppleExtensions>().RestoreTransactions((ok, message) => done?.Invoke(ok));
#elif UNITY_ANDROID
            _extensions.GetExtension<IGooglePlayStoreExtensions>().RestoreTransactions((ok, message) => done?.Invoke(ok));
#else
            done?.Invoke(false);
#endif
        }

        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _controller = controller;
            _extensions = extensions;
            foreach (Product p in _pending) Finish(p);
            _pending.Clear();
        }

        public void OnInitializeFailed(InitializationFailureReason error) => OnInitializeFailed(error, null);

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            Debug.LogWarning("Store: " + error + " " + message);
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            Product p = args.purchasedProduct;
            if (!_check.Accept(p))
            {
                Debug.LogWarning("Store: receipt refused for " + p.definition.id + "; left pending");
                Tell(p.definition.id, PurchaseResult.Of(PurchaseOutcome.Failed, p.definition.id));
                return PurchaseProcessingResult.Pending;
            }
            if (Delivered == null && !_waiting.ContainsKey(p.definition.id))
            {
                _pending.Add(p);
                return PurchaseProcessingResult.Pending;
            }
            Finish(p);
            return PurchaseProcessingResult.Complete;
        }

        void Finish(Product p)
        {
            var result = PurchaseResult.Of(PurchaseOutcome.Paid, p.definition.id, p.transactionID);
            if (!Tell(p.definition.id, result)) Delivered?.Invoke(result);
            if (_controller != null && _pending.Contains(p)) _controller.ConfirmPendingPurchase(p);
        }

        bool Tell(string packId, PurchaseResult result)
        {
            if (!_waiting.TryGetValue(packId, out Action<PurchaseResult> done)) return false;
            _waiting.Remove(packId);
            done?.Invoke(result);
            return true;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failure)
        {
            if (product == null || failure == null) { Debug.LogWarning("Store: purchase failed for an unknown product"); return; }
            PurchaseOutcome outcome = failure.reason == PurchaseFailureReason.UserCancelled ? PurchaseOutcome.Cancelled
                : failure.reason == PurchaseFailureReason.PurchasingUnavailable || failure.reason == PurchaseFailureReason.ProductUnavailable ? PurchaseOutcome.Unavailable
                : PurchaseOutcome.Failed;
            Tell(product.definition.id, PurchaseResult.Of(outcome, product.definition.id));
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason reason)
            => OnPurchaseFailed(product, product == null ? null : new PurchaseFailureDescription(product.definition.id, reason, null));
    }

#if RECEIPT_VALIDATION
    public sealed class TangleReceiptCheck : UnityStorefront.IReceiptCheck
    {
        readonly UnityEngine.Purchasing.Security.CrossPlatformValidator _validator =
            new UnityEngine.Purchasing.Security.CrossPlatformValidator(GooglePlayTangle.Data(), AppleTangle.Data(), Application.identifier);

        public bool Accept(Product product)
        {
            try { return _validator.Validate(product.receipt).Length > 0; }
            catch (UnityEngine.Purchasing.Security.IAPSecurityException) { return false; }
        }
    }
#endif
}
#endif

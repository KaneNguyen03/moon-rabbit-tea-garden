using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class GoldenPacksView
    {
        sealed class PackRow { public GoldenPack Pack; public ChunkyButton Button; public Text Label, Price; }

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly PackRow[] _rows = new PackRow[Storefront.Packs.Length];
        readonly Text _status;
        readonly Button _restore;
        readonly Action<string, Action<PurchaseResult>> _onBuy;
        readonly Action<Action<bool>> _onRestore;
        IStorefront _store;
        bool _busy;

        public bool IsOpen => _root.activeSelf;
        public string Status => _status.text;

        public GoldenPacksView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<string, Action<PurchaseResult>> onBuy, Action<Action<bool>> onRestore)
        {
            _tweens = tweens;
            _onBuy = onBuy;
            _onRestore = onRestore;
            var overlay = UiKit.Rect("GoldenPacks", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 900, 1180);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            if (catalog.GoldenCarrot != null) UiKit.Rect("Icon", _card).Place(0.5f, 1f, 0, -80, 110, 110).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -180, 800, 70)
                .AddText(L10n.T("packs.title"), 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -275, 800, 90)
                .AddText(L10n.T("packs.sub"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            sub.horizontalOverflow = HorizontalWrapMode.Wrap;
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 16;
            sub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            for (int i = 0; i < _rows.Length; i++)
            {
                GoldenPack pack = Storefront.Packs[i];
                var row = new PackRow { Pack = pack };
                var rt = UiKit.Rect("Pack " + i, _card).Place(0.5f, 1f, 0, -420 - i * 150, 760, 124);
                ChunkyButton button = null;
                int index = i;
                button = UiKit.Chunky(rt, catalog.Panel, () => { button.Press(_tweens); OnPack(index); });
                button.Face.color = pack.Best ? Palette.Lantern : Palette.Card;
                button.Lip.color = pack.Best ? Palette.Mix(Palette.Lantern, Palette.Ink, 0.3f) : Palette.CardLip;
                if (catalog.GoldenCarrot != null) UiKit.Rect("Icon", button.FaceRect).Place(0f, 0.5f, 60, 0, 64, 64).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
                row.Label = UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 0.62f, 1, 110, 0, 0, 0)
                    .AddText(L10n.T("packs.pack", pack.Golden), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                row.Label.resizeTextForBestFit = true;
                row.Label.resizeTextMinSize = 16;
                row.Label.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);
                var priceChip = UiKit.Rect("PriceChip", button.FaceRect).Place(1f, 0.5f, -120, 0, 200, 64);
                priceChip.AddImage(catalog.Panel, Palette.Paper, true);
                row.Price = UiKit.Rect("Price", priceChip).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                row.Price.resizeTextForBestFit = true;
                row.Price.resizeTextMinSize = 14;
                row.Price.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
                if (pack.Best)
                {
                    var best = UiKit.Rect("Best", rt).Place(0f, 1f, 90, 10, 150, 40);
                    best.AddImage(catalog.Panel, Palette.Carrot, true);
                    Text bestText = UiKit.Rect("Text", best).Stretch(0, 0, 1, 1, 6, 0, 6, 0).AddText(L10n.T("packs.best"), UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                    bestText.resizeTextForBestFit = true;
                    bestText.resizeTextMinSize = 12;
                    bestText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                }
                row.Button = button;
                _rows[i] = row;
            }

            _status = UiKit.Rect("Status", _card).Place(0.5f, 1f, 0, -900, 800, 70).AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            _status.horizontalOverflow = HorizontalWrapMode.Wrap;
            _status.resizeTextForBestFit = true;
            _status.resizeTextMinSize = 14;
            _status.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            var restore = UiKit.Rect("Restore", _card).Place(0.5f, 0f, 0, 180, 700, UiKit.MinTouch);
            Image restoreHit = restore.AddImage(null, new Color(0, 0, 0, 0));
            restoreHit.raycastTarget = true;
            _restore = restore.gameObject.AddComponent<Button>();
            _restore.transition = Selectable.Transition.None;
            _restore.onClick.AddListener(OnRestore);
            UiKit.Rect("Text", restore).Stretch(0, 0, 1, 1).AddText(L10n.T("packs.restore"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var close = UiKit.Rect("Close", _card).Place(0.5f, 0f, 0, 80, 700, UiKit.MinTouch);
            Image closeHit = close.AddImage(null, new Color(0, 0, 0, 0));
            closeHit.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText(L10n.T("packs.close"), UiKit.TextTitle, Palette.InkSoft);

            _root.SetActive(false);
        }

        public void Open(IStorefront store)
        {
            _store = store;
            _busy = false;
            _status.text = store != null && store.Ready ? "" : L10n.T("packs.notready");
            LayoutPass();
            ShowRows();
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
        }

        void ShowRows()
        {
            foreach (PackRow row in _rows)
            {
                string price = _store != null ? _store.PriceOf(row.Pack.Id) : null;
                bool can = !_busy && _store != null && _store.Ready && price != null;
                row.Price.text = price ?? L10n.T("packs.price.unknown");
                row.Button.SetEnabled(can);
                row.Label.color = can ? Palette.Ink : Palette.InkSoft;
            }
            _restore.interactable = !_busy && _store != null && _store.Ready;
            ShowPass();
        }

        void OnPack(int index)
        {
            if (_busy || _store == null || !_store.Ready) return;
            Sfx.Blip();
            _busy = true;
            _status.text = L10n.T("packs.buying");
            ShowRows();
            GoldenPack pack = _rows[index].Pack;
            _onBuy(pack.Id, result =>
            {
                _busy = false;
                switch (result.Outcome)
                {
                    case PurchaseOutcome.Paid: _status.text = L10n.T("packs.paid", pack.Golden); break;
                    case PurchaseOutcome.Cancelled: _status.text = L10n.T("packs.cancelled"); break;
                    case PurchaseOutcome.Duplicate: _status.text = L10n.T("packs.duplicate"); break;
                    case PurchaseOutcome.Unavailable: _status.text = L10n.T("packs.notready"); break;
                    default: _status.text = L10n.T("packs.failed"); break;
                }
                ShowRows();
            });
        }

        void OnRestore()
        {
            if (_busy || _store == null || !_store.Ready) return;
            Sfx.Click();
            _busy = true;
            _status.text = L10n.T("packs.restoring");
            ShowRows();
            _onRestore(ok =>
            {
                _busy = false;
                _status.text = L10n.T(ok ? "packs.restored" : "packs.restore.failed");
                ShowRows();
            });
        }

        public void Close() => _root.SetActive(false);

        public const float CardHeight = 1180f, PassExtra = 150f, PassY = -870f, StatusY = -900f;
        RectTransform _passRow;
        ChunkyButton _passButton;
        Text _passLabel, _passPrice;
        Func<bool> _passShown;
        Func<int> _passDays;
        Action<Action<PurchaseResult>> _onBuyPass;

        public bool PassShown => _passRow != null && _passRow.gameObject.activeSelf;
        public string PassLabel => _passLabel != null ? _passLabel.text : null;

        public void AttachPass(GameCatalog catalog, Func<bool> shown, Func<int> daysLeft, Action<Action<PurchaseResult>> onBuy)
        {
            _passShown = shown;
            _passDays = daysLeft;
            _onBuyPass = onBuy;
            _passRow = UiKit.Rect("NoAds", _card).Place(0.5f, 1f, 0, PassY, 760, 124);
            _passButton = UiKit.Chunky(_passRow, catalog.Panel, () => { _passButton.Press(_tweens); OnPass(); });
            _passButton.Face.color = Palette.Card;
            _passButton.Lip.color = Palette.CardLip;
            _passLabel = UiKit.Rect("Text", _passButton.FaceRect).Stretch(0, 0, 0.66f, 1, 30, 0, 0, 0)
                .AddText(L10n.T("noads.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _passLabel.resizeTextForBestFit = true;
            _passLabel.resizeTextMinSize = 16;
            _passLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);
            var chip = UiKit.Rect("PriceChip", _passButton.FaceRect).Place(1f, 0.5f, -120, 0, 200, 64);
            chip.AddImage(catalog.Panel, Palette.Paper, true);
            _passPrice = UiKit.Rect("Price", chip).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _passPrice.resizeTextForBestFit = true;
            _passPrice.resizeTextMinSize = 14;
            _passPrice.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _passRow.gameObject.SetActive(false);
        }

        void LayoutPass()
        {
            bool on = _passRow != null && _passShown != null && _passShown();
            if (_passRow != null && _passRow.gameObject.activeSelf != on) _passRow.gameObject.SetActive(on);
            _card.sizeDelta = new Vector2(_card.sizeDelta.x, CardHeight + (on ? PassExtra : 0f));
            Vector2 at = _status.rectTransform.anchoredPosition;
            _status.rectTransform.anchoredPosition = new Vector2(at.x, StatusY - (on ? PassExtra : 0f));
        }

        void ShowPass()
        {
            if (!PassShown) return;
            string price = _store != null ? _store.PriceOf(Storefront.NoAdsPass) : null;
            bool can = !_busy && _store != null && _store.Ready && price != null;
            int days = _passDays != null ? _passDays() : 0;
            _passLabel.text = days > 0 ? L10n.T("noads.active", days) : L10n.T("noads.title");
            _passPrice.text = price ?? L10n.T("packs.price.unknown");
            _passButton.SetEnabled(can);
            _passLabel.color = can ? Palette.Ink : Palette.InkSoft;
        }

        void OnPass()
        {
            if (_busy || _store == null || !_store.Ready || _onBuyPass == null) return;
            Sfx.Blip();
            _busy = true;
            _status.text = L10n.T("packs.buying");
            ShowRows();
            _onBuyPass(result =>
            {
                _busy = false;
                switch (result.Outcome)
                {
                    case PurchaseOutcome.Paid: _status.text = L10n.T("noads.paid", _passDays != null ? _passDays() : Ads.PassDays); break;
                    case PurchaseOutcome.Cancelled: _status.text = L10n.T("packs.cancelled"); break;
                    case PurchaseOutcome.Duplicate: _status.text = L10n.T("packs.duplicate"); break;
                    case PurchaseOutcome.Unavailable: _status.text = L10n.T("packs.notready"); break;
                    default: _status.text = L10n.T("packs.failed"); break;
                }
                ShowRows();
            });
        }
    }
}

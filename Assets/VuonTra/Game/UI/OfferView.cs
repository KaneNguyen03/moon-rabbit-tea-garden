using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class OfferView
    {
        public const float CardWidth = 900f, CardHeight = 1140f, RowHeight = 92f, ContentsTop = 362f, ContentsPad = 16f;

        readonly GameObject _root;
        readonly RectTransform _card, _contents;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Text _title, _sub, _left, _status, _buyLabel, _closeLabel;
        readonly ChunkyButton _buy;
        readonly Action<string, Action<PurchaseResult>> _onBuy;
        IStorefront _store;
        bool _busy, _ended, _bought;

        public bool IsOpen => _root.activeSelf;
        public OfferDef Offer { get; private set; }
        public string Status => _status.text;
        public string Left => _left.text;
        public RectTransform Card => _card;

        public OfferView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<string, Action<PurchaseResult>> onBuy)
        {
            _catalog = catalog;
            _tweens = tweens;
            _onBuy = onBuy;
            var overlay = UiKit.Rect("Offer", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, CardWidth, CardHeight);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Lantern, 0.12f), true).raycastTarget = true;

            var once = UiKit.Rect("Once", _card).Place(0.5f, 1f, 0, 0, 260 * UiKit.TextScale, 56);
            once.AddImage(catalog.Panel, Palette.Carrot, true);
            Text onceText = UiKit.Rect("Text", once).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText(L10n.T("offer.once"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            onceText.resizeTextForBestFit = true;
            onceText.verticalOverflow = VerticalWrapMode.Truncate;
            onceText.resizeTextMinSize = 14;
            onceText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            if (catalog.GiftBox != null) UiKit.Rect("Icon", _card).Place(0.5f, 1f, 0, -110, 120, 120).AddImage(catalog.GiftBox, Color.white).preserveAspect = true;
            _title = UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -215, 820, 70).AddText("", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _title.resizeTextForBestFit = true;
            _title.verticalOverflow = VerticalWrapMode.Truncate;
            _title.resizeTextMinSize = 24;
            _title.resizeTextMaxSize = UiKit.Scaled(48);
            _sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -268, 820, 40).AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            _sub.resizeTextForBestFit = true;
            _sub.verticalOverflow = VerticalWrapMode.Truncate;
            _sub.resizeTextMinSize = 14;
            _sub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            var leftChip = UiKit.Rect("LeftChip", _card).Place(0.5f, 1f, 0, -318, 360, 48);
            leftChip.AddImage(catalog.Panel, Palette.Card, true);
            _left = UiKit.Rect("Left", leftChip).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _left.resizeTextForBestFit = true;
            _left.verticalOverflow = VerticalWrapMode.Truncate;
            _left.resizeTextMinSize = 14;
            _left.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            _contents = UiKit.Rect("Contents", _card).Stretch(0, 1, 1, 1, 120, -ContentsTop - 2f * ContentsPad - 4f * RowHeight, 120, ContentsTop);
            _contents.AddImage(catalog.Panel, Palette.Card, true);

            var buy = UiKit.Rect("Buy", _card).Place(0.5f, 0f, 0, 270, 700, 120);
            _buy = UiKit.Chunky(buy, catalog.Panel, () => { _buy.Press(_tweens); OnBuy(); });
            _buy.Face.color = Palette.Lantern;
            _buy.Lip.color = Palette.Mix(Palette.Lantern, Palette.Ink, 0.3f);
            _buyLabel = UiKit.Rect("Text", _buy.FaceRect).Stretch(0, 0, 1, 1, 20, 0, 20, 0).AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _buyLabel.resizeTextForBestFit = true;
            _buyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _buyLabel.resizeTextMinSize = 16;
            _buyLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);

            _status = UiKit.Rect("Status", _card).Place(0.5f, 0f, 0, 165, 800, 64).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _status.horizontalOverflow = HorizontalWrapMode.Wrap;
            _status.resizeTextForBestFit = true;
            _status.verticalOverflow = VerticalWrapMode.Truncate;
            _status.resizeTextMinSize = 12;
            _status.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            var close = UiKit.Rect("Close", _card).Place(0.5f, 0f, 0, 76, 600, UiKit.MinTouch);
            Image closeHit = close.AddImage(null, new Color(0, 0, 0, 0));
            closeHit.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            _closeLabel = UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText(L10n.T("offer.later"), UiKit.TextTitle, Palette.InkSoft);

            _root.SetActive(false);
        }

        public static string Countdown(long seconds)
        {
            seconds = Math.Max(0L, seconds);
            return (seconds / 3600L).ToString("00") + ":" + (seconds / 60L % 60L).ToString("00") + ":" + (seconds % 60L).ToString("00");
        }

        public static string TitleOf(OfferDef offer) => offer.IsStarter ? L10n.T("offer.starter.title") : L10n.T("offer.milestone.title", offer.Level);

        public void Open(OfferDef offer, long secondsLeft, IStorefront store)
        {
            Offer = offer;
            _store = store;
            _busy = false;
            _ended = false;
            _bought = false;
            _closeLabel.text = L10n.T("offer.later");
            _title.text = TitleOf(offer);
            _sub.text = offer.IsStarter ? L10n.T("offer.starter.sub") : L10n.T("offer.milestone.sub", offer.Level);
            _status.text = store != null && store.Ready ? "" : L10n.T("packs.notready");
            BuildContents(offer);
            Tick(secondsLeft);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _tweens.Grow(_card, 0.85f, Motion.Time(0.25f), Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);

        public void Tick(long secondsLeft)
        {
            if (Offer == null) return;
            if (secondsLeft <= 0L && !_ended && !_busy)
            {
                _ended = true;
                _status.text = L10n.T("offer.ended");
            }
            _left.text = _bought ? L10n.T("offer.bought.short") : _ended ? L10n.T("offer.ended.short") : L10n.T("offer.left", Countdown(secondsLeft));
            ShowBuy();
        }

        public void ShowPaid()
        {
            _busy = false;
            _ended = true;
            _bought = true;
            _status.text = L10n.T("offer.paid");
            _left.text = L10n.T("offer.bought.short");
            _closeLabel.text = L10n.T("packs.close");
            ShowBuy();
        }

        void ShowBuy()
        {
            string price = _store != null ? _store.PriceOf(Offer.Id) : null;
            bool can = !_busy && !_ended && _store != null && _store.Ready && price != null;
            _buyLabel.text = L10n.T("offer.buy", price ?? L10n.T("packs.price.unknown"));
            _buy.SetEnabled(can);
            _buyLabel.color = can ? Palette.Ink : Palette.InkSoft;
        }

        void OnBuy()
        {
            if (_busy || _ended || _store == null || !_store.Ready || Offer == null) return;
            Sfx.Blip();
            _busy = true;
            _status.text = L10n.T("packs.buying");
            ShowBuy();
            string id = Offer.Id;
            _onBuy(id, result =>
            {
                if (Offer == null || Offer.Id != id) return;
                _busy = false;
                switch (result.Outcome)
                {
                    case PurchaseOutcome.Paid: ShowPaid(); return;
                    case PurchaseOutcome.Cancelled: _status.text = L10n.T("packs.cancelled"); break;
                    case PurchaseOutcome.Pending: _status.text = L10n.T("offer.pending"); break;
                    case PurchaseOutcome.Duplicate: _ended = true; _status.text = L10n.T("packs.duplicate"); break;
                    case PurchaseOutcome.Unavailable: _status.text = _ended ? L10n.T("offer.ended") : L10n.T("packs.notready"); break;
                    default: _status.text = L10n.T("packs.failed"); break;
                }
                ShowBuy();
            });
        }

        void BuildContents(OfferDef offer)
        {
            for (int i = _contents.childCount - 1; i >= 0; i--)
            {
                GameObject child = _contents.GetChild(i).gameObject;
                if (Application.isPlaying) UnityEngine.Object.Destroy(child); else UnityEngine.Object.DestroyImmediate(child);
            }
            int row = 0;
            Row(row++, "Golden", L10n.T("offer.golden", offer.Golden)).AddImage(_catalog.GoldenCarrot, Color.white).preserveAspect = true;
            RectTransform jarHolder = Row(row++, "Jars", L10n.T("offer.jars", offer.Jars));
            IncomeView.JarIcon(jarHolder, _catalog, "Jar", 60f, 0.5f, 0.5f, 0, 0);
            if (offer.Clovers > 0) Row(row++, "Clovers", L10n.T("offer.clovers", offer.Clovers)).AddImage(_catalog.Clover, Color.white).preserveAspect = true;
            if (offer.Hat != null) Row(row++, "Hat", L10n.T("offer.hat", L10n.T("item." + offer.Hat))).AddImage(_catalog.ItemIcon(offer.Hat), Color.white).preserveAspect = true;
            if (offer.RabbitTier >= 0)
            {
                RectTransform icon = Row(row++, "Rabbit", L10n.T("offer.rabbit", L10n.Rarity(offer.RabbitTier)));
                UiKit.Rect("Glow", icon).Stretch(0, 0, 1, 1, -10, -10, -10, -10).AddImage(_catalog.Round, Palette.Alpha(Palette.Tiers[offer.RabbitTier], 0.45f));
                UiKit.Rect("Art", icon).Stretch(0, 0, 1, 1).AddImage(_catalog.Rabbit, Color.white).preserveAspect = true;
            }
            _contents.offsetMin = new Vector2(_contents.offsetMin.x, -ContentsTop - 2f * ContentsPad - row * RowHeight);
        }

        RectTransform Row(int index, string name, string text)
        {
            var row = UiKit.Rect(name, _contents).Stretch(0, 1, 1, 1, 0, -ContentsPad - (index + 1) * RowHeight, 0, ContentsPad + index * RowHeight);
            Text label = UiKit.Rect("Text", row).Stretch(0, 0, 1, 1, 130, 0, 24, 0).AddText(text, UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.resizeTextForBestFit = true;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextMinSize = 16;
            label.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);
            return UiKit.Rect("Icon", row).Place(0f, 0.5f, 74, 0, 68, 68);
        }
    }
}

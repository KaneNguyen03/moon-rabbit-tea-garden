using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class AdButton
    {
        public readonly RectTransform Root;
        readonly Button _button;
        readonly Text _label;
        readonly ShownText _labelShown = new ShownText();
        readonly string _watchKey, _nowKey;
        readonly Func<AdOffer> _offer;
        readonly Func<bool> _busy;

        public bool Shown => Root.gameObject.activeSelf;
        public string Label => _label.text;
        public Button Button => _button;

        public AdButton(RectTransform root, GameCatalog catalog, string watchKey, string nowKey, Func<AdOffer> offer, Func<bool> busy, Action onPress)
        {
            Root = root;
            _watchKey = watchKey;
            _nowKey = nowKey;
            _offer = offer;
            _busy = busy;
            _button = UiKit.Secondary(root, catalog.Panel, "", () =>
            {
                if (_busy != null && _busy()) return;
                Sfx.Click();
                onPress?.Invoke();
            });
            _label = root.Find("Text").GetComponent<Text>();
            _label.horizontalOverflow = HorizontalWrapMode.Wrap;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 14;
            _label.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            Refresh();
        }

        public void Refresh()
        {
            AdOffer offer = _offer != null ? _offer() : AdOffer.None;
            bool show = offer != AdOffer.None;
            if (Root.gameObject.activeSelf != show) Root.gameObject.SetActive(show);
            if (!show) return;
            bool busy = _busy != null && _busy();
            _labelShown.Line(_label, busy ? "ad.busy" : offer == AdOffer.Instant ? _nowKey : _watchKey);
            _button.interactable = !busy;
        }
    }
}

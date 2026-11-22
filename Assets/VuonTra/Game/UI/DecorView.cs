using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The card for one decoration spot (#11): what stands there now, with a button to put it away, and a row for
    /// each kind the shop sells with how many wait in storage and a button that stands one here (or swaps it for
    /// what is there). Everything it shows is read off the state each refresh.
    /// </summary>
    public sealed class DecorView
    {
        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        /// <summary>The spot the card is open for, or -1.</summary>
        public int Spot { get; private set; } = -1;
        public const float Height = 900f;

        readonly Text _here, _storeLabel;
        readonly ChunkyButton _store;
        readonly Text[] _counts = new Text[Decor.Kinds.Length];
        readonly Text[] _useLabels = new Text[Decor.Kinds.Length];
        readonly ChunkyButton[] _uses = new ChunkyButton[Decor.Kinds.Length];
        readonly Image[] _plates = new Image[Decor.Kinds.Length];

        public DecorView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<int, string> onPlace, Action<int> onStore)
        {
            Sheet = new BottomSheet(canvas, "Decor", catalog, tweens, Height, () => Spot = -1);
            RectTransform card = Sheet.Card;
            UiKit.Rect("Title", card).Stretch(0, 1, 1, 1, 40, -120, 40, 40)
                .AddText(L10n.T("decor.title"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text sub = UiKit.Rect("Sub", card).Stretch(0, 1, 1, 1, 40, -170, 40, 120).AddText(L10n.T("decor.sub"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            sub.verticalOverflow = VerticalWrapMode.Truncate;
            sub.resizeTextForBestFit = true;
            sub.resizeTextMinSize = 12;
            sub.resizeTextMaxSize = UiKit.TextSmall;

            // What stands here now, and the button that takes it back into storage.
            var here = UiKit.Rect("Here", card).Stretch(0, 1, 1, 1, 40, -290, 40, 180);
            here.AddImage(catalog.Panel, Palette.Card, true);
            _here = UiKit.Rect("Text", here).Stretch(0, 0, 1, 1, 24, 0, 320, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _here.resizeTextForBestFit = true;
            _here.resizeTextMinSize = 14;
            _here.resizeTextMaxSize = UiKit.TextBody;
            var storeButton = UiKit.Rect("Store", card).Place(1f, 1f, -40 - 140, -235, 280, 88);
            _store = UiKit.Chunky(storeButton, catalog.Panel, () => { if (Spot >= 0) onStore(Spot); });
            _storeLabel = UiKit.Rect("Text", _store.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText(L10n.T("decor.store"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _storeLabel.resizeTextForBestFit = true;
            _storeLabel.resizeTextMinSize = 13;
            _storeLabel.resizeTextMaxSize = UiKit.TextBody;

            // One row per kind: its picture, its name, how many wait in storage, and the button that stands one here.
            for (int i = 0; i < Decor.Kinds.Length; i++)
            {
                DecorKind kind = Decor.Kinds[i];
                float top = 310 + i * 124;
                var row = UiKit.Rect("Row " + kind.Id, card).Stretch(0, 1, 1, 1, 40, -(top + 112), 40, top);
                _plates[i] = row.AddImage(catalog.Panel, Palette.Card, true);
                Image icon = UiKit.Rect("Icon", row).Place(0f, 0.5f, 66, 0, 92, 88).AddImage(catalog.DecorArt(kind.Id), Color.white);
                icon.preserveAspect = true;
                icon.enabled = icon.sprite != null;
                Text name = UiKit.Rect("Name", row).Stretch(0, 0.5f, 1, 1, 130, 0, 260, 12).AddText(L10n.T("decor." + kind.Id), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 14;
                name.resizeTextMaxSize = UiKit.TextBody;
                _counts[i] = UiKit.Rect("Count", row).Stretch(0, 0, 1, 0.5f, 130, 12, 260, 0).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
                _counts[i].resizeTextForBestFit = true;
                _counts[i].resizeTextMinSize = 12;
                _counts[i].resizeTextMaxSize = UiKit.TextSmall;
                var useButton = UiKit.Rect("Use", row).Place(1f, 0.5f, -118, 0, 212, 88);
                string kindId = kind.Id;
                _uses[i] = UiKit.Chunky(useButton, catalog.Panel, () => { if (Spot >= 0) onPlace(Spot, kindId); });
                _useLabels[i] = UiKit.Rect("Text", _uses[i].FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                _useLabels[i].resizeTextForBestFit = true;
                _useLabels[i].resizeTextMinSize = 13;
                _useLabels[i].resizeTextMaxSize = UiKit.TextBody;
            }
        }

        public void Open(GameState s, int spot)
        {
            Spot = spot;
            Sheet.Open();
            Refresh(s);
        }

        public void Close() => Sheet.Close();

        public void Refresh(GameState s)
        {
            if (!IsOpen || Spot < 0 || Spot >= s.DecorSpots.Length) return;
            string standing = s.DecorSpots[Spot];
            _here.text = standing != null ? L10n.T("decor.here", L10n.T("decor." + standing)) : L10n.T("decor.empty");
            if (_store.Root.gameObject.activeSelf != (standing != null)) _store.Root.gameObject.SetActive(standing != null);
            for (int i = 0; i < Decor.Kinds.Length; i++)
            {
                string kindId = Decor.Kinds[i].Id;
                int left = Decor.Available(s, kindId);
                bool can = left > 0 && standing != kindId;
                _counts[i].text = left > 0 ? L10n.T("decor.left", left) : L10n.T("decor.none");
                _useLabels[i].text = L10n.T(standing != null ? "decor.swap" : "decor.place");
                _useLabels[i].color = ChunkyButton.InkFor(can);
                _uses[i].SetEnabled(can);
                _plates[i].color = standing == kindId ? Palette.Mix(Palette.Card, Palette.Moss, 0.18f) : Palette.Card;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The mailbox (#68), opened from the pause card: the gifts the game wrote itself (a make-up gift when a save could
    /// not be read, a season's gift when its banner was missed), each with what it holds, the days it has left and a
    /// button that claims it. Ten letters at most, thirty days each.
    /// </summary>
    public sealed class MailView
    {
        sealed class Row
        {
            public GameObject Go;
            public Text Title, Sub;
            public string Id;
        }

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Text _empty, _note;
        readonly List<Row> _rows = new List<Row>();
        readonly Action<string> _onClaim;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Root => _card;

        public MailView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<string> onClaim)
        {
            _tweens = tweens;
            _onClaim = onClaim;

            var overlay = UiKit.Rect("Mail", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 860, 1560);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -80, 600, 70)
                .AddText(L10n.T("mail.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _note = UiKit.Rect("Note", _card).Place(0.5f, 1f, 0, -160, 740, 50)
                .AddText("", UiKit.TextBody, Palette.Carrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _note.resizeTextForBestFit = true;
            _note.resizeTextMinSize = 14;
            _note.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            for (int i = 0; i < Mailbox.MaxLetters; i++)
            {
                var plate = UiKit.Rect("Letter " + i, _card).Place(0.5f, 1f, 0, -250 - i * 118, 760, 108);
                plate.AddImage(catalog.Panel, Palette.Track, true);
                var row = new Row { Go = plate.gameObject };
                row.Title = UiKit.Rect("Title", plate).Stretch(0, 0.5f, 1, 1, 28, 0, 236, 6)
                    .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                row.Title.resizeTextForBestFit = true;
                row.Title.resizeTextMinSize = 14;
                row.Title.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
                row.Sub = UiKit.Rect("Sub", plate).Stretch(0, 0, 1, 0.5f, 28, 6, 236, 0)
                    .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
                row.Sub.resizeTextForBestFit = true;
                row.Sub.resizeTextMinSize = 12;
                row.Sub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                var claim = UiKit.Rect("Claim", plate).Place(1f, 0.5f, -116, 0, 200, UiKit.MinTouch);
                ChunkyButton claimButton = null;
                claimButton = UiKit.Chunky(claim, catalog.Panel, () =>
                {
                    claimButton.Press(_tweens);
                    if (row.Id != null) _onClaim?.Invoke(row.Id);
                });
                Text claimLabel = UiKit.Rect("Text", claimButton.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0)
                    .AddText(L10n.T("mail.claim"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
                claimLabel.resizeTextForBestFit = true;
                claimLabel.resizeTextMinSize = 14;
                claimLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
                plate.gameObject.SetActive(false);
                _rows.Add(row);
            }

            _empty = UiKit.Rect("Empty", _card).Place(0.5f, 1f, 0, -420, 700, 220)
                .AddText(L10n.T("mail.empty"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            _empty.resizeTextForBestFit = true;
            _empty.resizeTextMinSize = 14;
            _empty.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            Text rules = UiKit.Rect("Rules", _card).Place(0.5f, 0f, 0, 70, 740, 50)
                .AddText(L10n.T("mail.rules", Mailbox.MaxLetters, Mailbox.KeepDays), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            rules.resizeTextForBestFit = true;
            rules.resizeTextMinSize = 12;
            rules.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _root.SetActive(false);
        }

        /// <summary>A letter's heading: a make-up gift, or the season it is for.</summary>
        public static string TitleOf(Letter letter)
            => letter.Kind == LetterKind.MakeUp ? L10n.T("mail.makeup") : L10n.T("mail.season", L10n.Season(letter.Season));

        /// <summary>What a gift holds, as "+10 cà rốt vàng · +3 cỏ bốn lá".</summary>
        public static string GiftOf(int golden, int clovers)
        {
            var parts = new List<string>(2);
            if (golden > 0) parts.Add(L10n.T("mail.golden", golden));
            if (clovers > 0) parts.Add(L10n.T("mail.clovers", clovers));
            return string.Join(" · ", parts);
        }

        /// <summary>A letter's second line: what it holds and the days left to claim it.</summary>
        public static string SubOf(Letter letter, long today)
        {
            int left = Mailbox.DaysLeft(letter, today);
            return GiftOf(letter.Golden, letter.Clovers) + " · " + (left > 0 ? L10n.T("mail.days", left) : L10n.T("mail.lastday"));
        }

        public void Open(IReadOnlyList<Letter> letters, long today)
        {
            _note.text = "";
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
            Refresh(letters, today);
        }

        /// <summary>One row per letter, oldest first; the empty line when there are none.</summary>
        public void Refresh(IReadOnlyList<Letter> letters, long today)
        {
            int n = letters == null ? 0 : Math.Min(letters.Count, _rows.Count);
            for (int i = 0; i < _rows.Count; i++)
            {
                Row row = _rows[i];
                bool show = i < n;
                if (row.Go.activeSelf != show) row.Go.SetActive(show);
                row.Id = show ? letters[i].Id : null;
                if (!show) continue;
                row.Title.text = TitleOf(letters[i]);
                row.Sub.text = SubOf(letters[i], today);
            }
            _empty.gameObject.SetActive(n == 0);
        }

        /// <summary>The line under the title after a claim: what just came.</summary>
        public void ShowClaimed(string text) => _note.text = text;

        public void Close()
        {
            _root.SetActive(false);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class ReviewView
    {
        public const int FeedbackChars = ReviewAsk.FeedbackMaxLength;

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Sprite _panel;
        readonly GameObject _ask, _feedback;
        readonly InputField _input;
        readonly Text _note;
        readonly Action _onLike;
        readonly Action<string> _onFeedback;

        public bool IsOpen => _root.activeSelf;
        public bool AskingFeedback => _feedback.activeSelf;
        public RectTransform Root => (RectTransform)_root.transform;
        public InputField Input => _input;

        public ReviewView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onLike, Action<string> onFeedback)
        {
            _tweens = tweens;
            _panel = catalog.Panel;
            _onLike = onLike;
            _onFeedback = onFeedback;

            var overlay = UiKit.Rect("Review", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.58f, 0, 0, 860, 540);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;

            var ask = UiKit.Rect("Ask", _card).Stretch(0, 0, 1, 1);
            _ask = ask.gameObject;
            Image rabbit = UiKit.Rect("Rabbit", ask).Place(0.5f, 1f, 0, -125, 180, 180).AddImage(catalog.Rabbit, Color.white);
            rabbit.preserveAspect = true;
            Text title = UiKit.Rect("Title", ask).Place(0.5f, 1f, 0, -290, 780, 80)
                .AddText(L10n.T("review.title"), 46, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.FitOneLine(780, 28);
            Button(ask, "No", -200, false, L10n.T("review.no"), ShowFeedback);
            Button(ask, "Yes", 200, true, L10n.T("review.yes"), Like);

            var feedback = UiKit.Rect("Feedback", _card).Stretch(0, 0, 1, 1);
            _feedback = feedback.gameObject;
            Text question = UiKit.Rect("Title", feedback).Place(0.5f, 1f, 0, -90, 780, 80)
                .AddText(L10n.T("review.feedback.title"), 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            question.FitOneLine(780, 24);
            var field = UiKit.Rect("Input", feedback).Place(0.5f, 1f, 0, -220, 760, 110);
            Image fieldImage = field.AddImage(catalog.Panel, Palette.Card, true);
            fieldImage.raycastTarget = true;
            Text text = UiKit.Rect("Text", field).Stretch(0, 0, 1, 1, 32, 0, 32, 0)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft);
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            Text hint = UiKit.Rect("Hint", field).Stretch(0, 0, 1, 1, 32, 0, 32, 0)
                .AddText(L10n.T("review.feedback.hint"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            hint.FitOneLine(696, 14);
            _input = field.gameObject.AddComponent<InputField>();
            _input.targetGraphic = fieldImage;
            _input.textComponent = text;
            _input.placeholder = hint;
            _input.lineType = InputField.LineType.SingleLine;
            _input.characterLimit = FeedbackChars;
            _note = UiKit.Rect("Note", feedback).Place(0.5f, 1f, 0, -310, 760, 44)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _note.resizeTextForBestFit = true;
            _note.resizeTextMinSize = 12;
            _note.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _note.verticalOverflow = VerticalWrapMode.Truncate;
            Button(feedback, "Cancel", -200, false, L10n.T("review.cancel"), () => Answer(""));
            Button(feedback, "Send", 200, true, L10n.T("review.send"), () => Answer(_input.text));

            _root.SetActive(false);
        }

        void Button(RectTransform parent, string name, float x, bool main, string label, Action onPress)
        {
            var rt = UiKit.Rect(name, parent).Place(0.5f, 0f, x, 110, 340, 120);
            ChunkyButton button = null;
            button = UiKit.Chunky(rt, _panel, () =>
            {
                button.Press(_tweens);
                onPress();
            });
            if (!main)
            {
                button.Lip.color = Palette.OffLip;
                button.Face.color = Palette.Track;
            }
            UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 12, 0, 12, 0)
                .AddText(label, UiKit.TextTitle, main ? Palette.OnCarrot : Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)
                .FitOneLine(316, 20);
        }

        public void Open(string playerCode)
        {
            _note.text = L10n.T("review.feedback.note", playerCode);
            _input.text = "";
            _ask.SetActive(true);
            _feedback.SetActive(false);
            _root.SetActive(true);
            Pop();
        }

        public void ShowFeedback()
        {
            Sfx.Click();
            _ask.SetActive(false);
            _feedback.SetActive(true);
            Pop();
            if (Application.isPlaying) _input.ActivateInputField();
        }

        public void Like()
        {
            if (!IsOpen) return;
            Close();
            _onLike();
        }

        public void Answer(string typed)
        {
            if (!IsOpen) return;
            Close();
            _onFeedback(typed ?? "");
        }

        public void Close() => _root.SetActive(false);

        void Pop()
        {
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(Motion.Panel), t => { if (_card != null) _card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
        }
    }
}

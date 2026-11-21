using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>The small overlay where the player types a new name for a rabbit.</summary>
    public sealed class RenameView
    {
        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly InputField _input;
        readonly Action<long, string> _onSave;
        long _rabbitId;

        public bool IsOpen => _root.activeSelf;

        public RenameView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<long, string> onSave)
        {
            _tweens = tweens;
            _onSave = onSave;

            var overlay = UiKit.Rect("Rename", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.62f, 0, 0, 860, 520);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -84, 760, 70)
                .AddText(L10n.T("rename.title"), 46, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var field = UiKit.Rect("Input", _card).Place(0.5f, 1f, 0, -210, 700, 110);
            Image fieldImage = field.AddImage(catalog.Panel, Palette.Card, true);
            fieldImage.raycastTarget = true;
            Text text = UiKit.Rect("Text", field).Stretch(0, 0, 1, 1, 32, 0, 32, 0)
                .AddText("", 44, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.supportRichText = false; // a typed "<b>" is just text
            Text hint = UiKit.Rect("Hint", field).Stretch(0, 0, 1, 1, 32, 0, 32, 0)
                .AddText(L10n.T("rename.hint", Rabbits.MaxNameLength), 30, Palette.OffInk, TextAnchor.MiddleLeft);
            _input = field.gameObject.AddComponent<InputField>();
            _input.targetGraphic = fieldImage;
            _input.textComponent = text;
            _input.placeholder = hint;
            _input.lineType = InputField.LineType.SingleLine;
            _input.characterLimit = Rabbits.MaxNameLength;
            _input.onEndEdit.AddListener(_ => { if (Application.isPlaying && Input_SubmitPressed()) Save(); });

            var save = UiKit.Rect("Save", _card).Place(0.5f, 0f, 150, 110, 360, 120);
            ChunkyButton saveButton = null;
            saveButton = UiKit.Chunky(save, catalog.Panel, () =>
            {
                saveButton.Press(_tweens);
                Save();
            });
            UiKit.Rect("Text", saveButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("rename.save"), 40, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            var cancel = UiKit.Rect("Cancel", _card).Place(0.5f, 0f, -230, 110, 300, 120);
            ChunkyButton cancelButton = UiKit.Chunky(cancel, catalog.Panel, Close);
            cancelButton.Lip.color = Palette.OffLip;
            cancelButton.Face.color = Palette.Track;
            UiKit.Rect("Text", cancelButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("rename.cancel"), 36, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _root.SetActive(false);
        }

        /// <summary>Whether the edit ended because Enter was pressed (not because focus moved away).</summary>
        static bool Input_SubmitPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
        }

        public void Open(GameState s, long rabbitId)
        {
            Rabbit rabbit = Rabbits.Find(s, rabbitId);
            if (rabbit == null) return;
            _rabbitId = rabbitId;
            _input.text = rabbit.Name ?? "";
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(0.25f, t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Ease.OutBack);
            if (Application.isPlaying) _input.ActivateInputField();
        }

        public void Save()
        {
            if (!IsOpen) return;
            // An empty name is not saved; the card stays open so the player can type one or cancel.
            if (Rabbits.CleanName(_input.text) == null) return;
            _onSave(_rabbitId, _input.text);
            Close();
        }

        public void Close()
        {
            _root.SetActive(false);
        }
    }
}

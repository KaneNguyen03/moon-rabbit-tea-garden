using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed partial class GardenScreen
    {
        sealed class WorldLabel
        {
            public CanvasGroup Sentence, Chip;
            public Text Count;
            public int Plays;
            public readonly ShownText Shown = new ShownText();
            public bool Always;
        }

        public const float PlaysChipSize = 52f;
        const int PlaysChipText = 26;

        readonly List<WorldLabel> _worldLabels = new List<WorldLabel>();
        WorldLabel _mortarLabel, _basketLabel, _jarLabel, _pondLabel;
        bool _labelsNear;

        WorldLabel AddWorldLabel(RectTransform sentence)
        {
            var label = new WorldLabel { Sentence = Hidden(sentence) };
            _worldLabels.Add(label);
            return label;
        }

        WorldLabel AddGameLabel(GameCatalog catalog, RectTransform sentence, Vector2 chipAnchor, Vector2 chipAt)
        {
            WorldLabel label = AddWorldLabel(sentence);
            float size = PlaysChipSize * UiKit.TextScale;
            var chip = UiKit.Rect("Chip", (RectTransform)sentence.parent).Place(chipAnchor.x, chipAnchor.y, chipAt.x, chipAt.y, size, size);
            chip.AddImage(catalog.Round, ActivityPlate).raycastTarget = true;
            label.Count = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", PlaysChipText, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.Chip = Hidden(chip);
            return label;
        }

        static CanvasGroup Hidden(RectTransform rt)
        {
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            return group;
        }

        void SetPlays(WorldLabel label, int plays)
        {
            label.Plays = plays;
            label.Shown.Number(label.Count, plays);
            ShowLabel(label);
        }

        void ShowLabel(WorldLabel label)
        {
            bool sentence = _labelsNear || label.Always;
            Tweens.Fade(label.Sentence, sentence ? 1f : 0f);
            if (label.Chip != null) Tweens.Fade(label.Chip, !sentence && label.Plays > 0 ? 1f : 0f);
        }

        void ShowWorldLabels(bool near)
        {
            _labelsNear = near;
            foreach (WorldLabel label in _worldLabels) ShowLabel(label);
            foreach (StationActorView actor in _actors) actor.ShowWords(near, Tweens);
        }
    }
}

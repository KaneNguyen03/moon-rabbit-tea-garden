using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;
using VuonTra.Game.UI;

namespace VuonTra.Game
{
    public sealed class StandLabel : MonoBehaviour
    {
        public const int TextUnits = 26;
        public const float PlateHeight = 44f, PlateWidth = 168f, Gap = 6f, Above = 0.12f;
        static readonly Color Plate = Palette.Mix(Palette.Ink, Palette.Night, 0.35f);

        Canvas _canvas;
        Text _pad, _level;
        GameObject _padPlate, _levelPlate;
        int _shownLevel = -1;
        float _unit;

        public Canvas Canvas => _canvas;
        public Text PadText => _pad;
        public Text LevelText => _level;
        public bool PadShown => _padPlate.activeSelf;
        public bool LevelShown => _levelPlate.activeSelf;
        public float Width => PlateWidth * _unit;

        public static float UnitMetres(CameraSpec spec) =>
            (float)(2d * spec.DefaultDistance * System.Math.Tan(spec.FovDegrees * System.Math.PI / 360d) / GardenFraming.CanvasHeight);

        public float HeightOf(bool pad, bool level) => ((pad ? PlateHeight : 0f) + (pad && level ? Gap : 0f) + (level ? PlateHeight : 0f)) * _unit;

        public static StandLabel Attach(GardenStand stand, CameraSpec spec)
        {
            var go = new GameObject("Label", typeof(RectTransform), typeof(Canvas));
            go.transform.SetParent(stand.transform, false);
            go.transform.localRotation = Quaternion.Euler((float)spec.TiltDegrees, 0f, 0f);
            StandLabel label = go.AddComponent<StandLabel>();
            label._canvas = go.GetComponent<Canvas>();
            label._canvas.renderMode = RenderMode.WorldSpace;
            label._canvas.overrideSorting = true;
            label._canvas.sortingOrder = GardenStand.WorkerOrder + 3;
            var rect = (RectTransform)go.transform;
            rect.sizeDelta = new Vector2(PlateWidth, PlateHeight * 2f + Gap);
            rect.pivot = new Vector2(0.5f, 0f);
            label._unit = UnitMetres(spec);
            rect.localScale = Vector3.one * label._unit;
            label._padPlate = Pill(rect, "Pad", out label._pad);
            label._levelPlate = Pill(rect, "Level", out label._level);
            label._pad.text = L10n.T("pad.hint") + " +";
            label._padPlate.SetActive(false);
            label._levelPlate.SetActive(false);
            foreach (Transform part in go.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = stand.gameObject.layer;
            return label;
        }

        static GameObject Pill(RectTransform parent, string name, out Text text)
        {
            RectTransform pill = UiKit.Rect(name, parent).Place(0.5f, 0f, 0f, PlateHeight / 2f, PlateWidth, PlateHeight);
            pill.AddImage(null, Plate).raycastTarget = false;
            text = UiKit.Rect("Text", pill).Stretch(0, 0, 1, 1, 6, 0, 6, 0).AddText("", TextUnits, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.raycastTarget = false;
            return pill.gameObject;
        }

        public void PlaceAbove(double metres) => transform.localPosition = new Vector3(0f, (float)metres + Above, 0f);

        public void Show(bool pad, bool level, int number)
        {
            if (_padPlate.activeSelf != pad) _padPlate.SetActive(pad);
            if (_levelPlate.activeSelf != level) _levelPlate.SetActive(level);
            ((RectTransform)_levelPlate.transform).anchoredPosition = new Vector2(0f, (pad ? PlateHeight + Gap : 0f) + PlateHeight / 2f);
            if (level && number != _shownLevel)
            {
                _shownLevel = number;
                _level.text = L10n.StationLevel(number);
            }
        }
    }
}

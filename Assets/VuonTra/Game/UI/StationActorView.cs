using System;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>One station in the garden: a rabbit, its prop, a cycle bar and a level chip.</summary>
    public sealed class StationActorView
    {
        public RectTransform Root { get; private set; }

        readonly int _index;
        RectTransform _rabbit, _fill, _prop;
        Image _rabbitImage, _scarfImage, _propImage, _chip;
        Text _label;
        GameObject _bar, _glow;
        float _squish, _shownProgress, _propPop;
        bool _locked;
        Color _accent;

        public StationActorView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onTap)
        {
            _accent = data.Accent;
            Root = UiKit.Rect("Station " + data.Id, parent);

            Image hit = Root.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onTap(_index));

            var glow = UiKit.Rect("Glow", Root).Place(0.5f, 0.5f, 10, -20, 560, 470);
            glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            _glow = glow.gameObject;

            UiKit.Rect("Shadow", Root).Place(0.5f, 0.5f, 10, -112, 330, 62)
                .AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.4f));

            _prop = UiKit.Rect("Prop", Root).Place(0.5f, 0.5f, 112, -122, 168, 168);
            _prop.pivot = new Vector2(0.5f, 0f);
            _propImage = _prop.AddImage(data.Prop, Color.white);
            _propImage.preserveAspect = true;

            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, 236, 236);
            _rabbit.pivot = new Vector2(0.5f, 0f); // squash from the feet
            _rabbitImage = _rabbit.AddImage(catalog.Rabbit, Color.white);
            _rabbitImage.preserveAspect = true;
            _scarfImage = UiKit.Rect("Scarf", _rabbit).Stretch(0, 0, 1, 1).AddImage(catalog.Scarf, data.Accent);
            _scarfImage.preserveAspect = true;

            var barBg = UiKit.Rect("Bar", Root).Place(0.5f, 0.5f, -74, -144, 170, 24);
            barBg.AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.75f), true);
            _bar = barBg.gameObject;
            _fill = UiKit.Rect("Fill", barBg).Stretch(0, 0, 0, 1, 4, 4, 4, 4);
            _fill.AddImage(catalog.Panel, Palette.Carrot, true);

            var chip = UiKit.Rect("Level", Root).Place(0.5f, 0.5f, 112, -144, 150, 46);
            _chip = chip.AddImage(catalog.Panel, Palette.Paper, true);
            _label = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1)
                .AddText("", 27, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Switches between the working look and the asleep look.</summary>
        public void SetLevel(int level, bool isBrew)
        {
            _locked = level <= 0;
            _label.text = _locked ? "z z z" : "Cấp " + level;
            _label.color = _locked ? Palette.Mix(Palette.Paper, Palette.Night, 0.25f) : Palette.Ink;
            _chip.color = _locked ? Palette.Alpha(Palette.Mix(Palette.Night, Palette.Paper, 0.2f), 0.85f) : Palette.Paper;
            _rabbitImage.color = _locked ? Palette.Asleep : Color.white;
            _scarfImage.color = _locked ? Palette.Asleep : _accent;
            _propImage.color = _locked ? Palette.Alpha(Palette.Asleep, 0.55f) : Color.white;
            _glow.SetActive(!_locked);
            _bar.SetActive(!_locked && isBrew);
        }

        /// <summary>A tap landed: squash the rabbit.</summary>
        public void Squish()
        {
            _squish = 1f;
        }

        /// <summary>A cycle finished: the prop hops.</summary>
        public void PopProp()
        {
            _propPop = 1f;
        }

        /// <summary>Per-frame: cycle bar, idle breathing, tap squish, payout hop.</summary>
        public void Animate(double progress, float time, float deltaTime)
        {
            if (!_locked)
            {
                float target = Mathf.Clamp01((float)progress);
                // Ease towards the real value; a finished cycle snaps back to the start.
                _shownProgress = target < _shownProgress ? target : Mathf.Lerp(_shownProgress, target, Mathf.Clamp01(deltaTime * 14f));
                _fill.anchorMax = new Vector2(Mathf.Max(0.08f, _shownProgress), 1f);
            }

            _squish = Mathf.Max(0f, _squish - deltaTime * 5f);
            _propPop = Mathf.Max(0f, _propPop - deltaTime * 4f);
            float breathe = _locked ? Mathf.Sin(time * 1.2f + _index) * 0.012f : Mathf.Sin(time * 3f + _index * 1.7f) * 0.025f;
            _rabbit.localScale = new Vector3(1f + _squish * 0.12f - breathe * 0.5f, 1f - _squish * 0.18f + breathe, 1f);
            float hop = Ease.Pop(1f - _propPop) * (_propPop > 0f ? 1f : 0f);
            _prop.localScale = new Vector3(1f - hop * 0.08f, 1f + hop * 0.14f, 1f);
        }
    }
}

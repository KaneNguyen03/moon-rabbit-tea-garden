using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>One station in the garden: a rabbit, its prop, a cycle bar and a level label.</summary>
    public sealed class StationActorView
    {
        public RectTransform Root { get; private set; }

        readonly int _index;
        RectTransform _rabbit, _fill, _glow;
        Image _rabbitImage, _scarfImage, _propImage;
        Text _label;
        GameObject _bar;
        float _squish;
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

            var hit = Root.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() =>
            {
                _squish = 1f;
                onTap(_index);
            });

            _glow = UiKit.Rect("Glow", Root).Place(0.5f, 0.5f, 10, -20, 560, 470);
            _glow.AddImage(catalog.Glow != null ? catalog.Glow : catalog.Round, new Color(1f, 0.8f, 0.45f, catalog.Glow != null ? 0.55f : 0.08f));

            UiKit.Rect("Shadow", Root).Place(0.5f, 0.5f, 10, -112, 330, 62)
                .AddImage(catalog.Round, new Color(0.03f, 0.05f, 0.14f, 0.38f));

            _propImage = UiKit.Rect("Prop", Root).Place(0.5f, 0.5f, 112, -38, 168, 168).AddImage(data.Prop, Color.white);
            _propImage.preserveAspect = true;

            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, 236, 236);
            _rabbit.pivot = new Vector2(0.5f, 0f); // squash from the feet
            _rabbitImage = _rabbit.AddImage(catalog.Rabbit, Color.white);
            _rabbitImage.preserveAspect = true;
            _scarfImage = UiKit.Rect("Scarf", _rabbit).Stretch(0, 0, 1, 1).AddImage(catalog.Scarf, data.Accent);
            _scarfImage.preserveAspect = true;

            var barBg = UiKit.Rect("Bar", Root).Place(0.5f, 0.5f, 10, -140, 250, 22);
            barBg.AddImage(catalog.Panel, new Color(0.06f, 0.08f, 0.19f, 0.85f), true);
            _bar = barBg.gameObject;
            _fill = UiKit.Rect("Fill", barBg).Stretch(0, 0, 0, 1, 3, 3, 3, 3);
            _fill.AddImage(catalog.Panel, UiKit.Lantern, true);

            _label = UiKit.Rect("Level", Root).Place(0.5f, 0.5f, 10, -176, 320, 44)
                .AddText("", 30, UiKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            var outline = _label.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.06f, 0.08f, 0.19f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
        }

        /// <summary>Switches between the working look and the asleep look.</summary>
        public void SetLevel(int level, bool isBrew)
        {
            _locked = level <= 0;
            _label.text = _locked ? "Đang ngủ… z z" : "Cấp " + level;
            _label.color = _locked ? UiKit.Muted : UiKit.Cream;
            _rabbitImage.color = _locked ? new Color(0.45f, 0.5f, 0.75f, 0.75f) : Color.white;
            _scarfImage.color = _locked ? new Color(0.35f, 0.4f, 0.65f, 0.75f) : _accent;
            _propImage.color = _locked ? new Color(0.35f, 0.4f, 0.65f, 0.55f) : Color.white;
            _glow.gameObject.SetActive(!_locked);
            _bar.SetActive(!_locked && isBrew);
        }

        /// <summary>Per-frame: cycle bar, idle bob, tap squish.</summary>
        public void Animate(double progress, float time, float deltaTime)
        {
            if (!_locked)
            {
                float p = Mathf.Clamp01((float)progress);
                _fill.anchorMax = new Vector2(Mathf.Max(0.06f, p), 1f);
            }

            _squish = Mathf.Max(0f, _squish - deltaTime * 5f);
            float bob = _locked ? 0f : Mathf.Sin(time * 3f + _index * 1.7f) * 0.025f;
            _rabbit.localScale = new Vector3(1f + _squish * 0.12f - bob * 0.5f, 1f - _squish * 0.18f + bob, 1f);
        }
    }
}

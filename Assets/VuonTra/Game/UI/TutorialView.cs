using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The first minute's guidance: a line at the top saying what to do, a hand pointing at the one thing
    /// to touch, and a dim over everything else with a hole round that thing. Nothing is locked: every
    /// touch still goes through, and Skip ends it.
    /// </summary>
    public sealed class TutorialView
    {
        public RectTransform Root { get; private set; }
        /// <summary>What the hand points at right now, or null.</summary>
        public RectTransform Target { get; private set; }

        readonly RectTransform _canvas, _hand, _plate;
        readonly RectTransform[] _dim = new RectTransform[4];
        readonly Text _text, _progress;
        readonly FeelLayer _feel;
        int _step = -1;
        float _clock;
        Vector2 _handAt;
        const float Hole = 26f;

        public TutorialView(RectTransform canvas, GameCatalog catalog, FeelLayer feel, Action onSkip)
        {
            _canvas = canvas;
            _feel = feel;
            Root = UiKit.Rect("Tutorial", canvas).Stretch(0, 0, 1, 1);
            // The dim: four panels round a hole. None of them catches a touch.
            string[] names = { "DimTop", "DimBottom", "DimLeft", "DimRight" };
            for (int i = 0; i < 4; i++)
            {
                _dim[i] = UiKit.Rect(names[i], Root);
                _dim[i].AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.45f));
            }
            _hand = UiKit.Rect("Hand", Root).Place(0.5f, 0.5f, 0, 0, 110, 110); // placed from the centre, like the hole
            _hand.pivot = new Vector2(0.15f, 0.9f); // the fingertip
            _hand.AddImage(catalog.Hand, Color.white).preserveAspect = true;

            // What to do, at the top, with the way out beside it.
            _plate = UiKit.Rect("Plate", Root).Place(0.5f, 1f, 0, -196, 1000, 88);
            _plate.AddShadow(14f, 8f, 0.28f);
            UiKit.Rect("Body", _plate).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Paper, true);
            _progress = UiKit.Rect("Progress", _plate).Place(0f, 0.5f, 56, 0, 80, 40).AddText("", UiKit.TextSmall, Palette.CarrotLip, TextAnchor.MiddleCenter, FontStyle.Bold);
            _text = UiKit.Rect("Text", _plate).Stretch(0, 0, 1, 1, 100, 0, 170, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _text.resizeTextForBestFit = true;
            _text.resizeTextMinSize = 16;
            _text.resizeTextMaxSize = UiKit.TextBody;
            var skip = UiKit.Rect("Skip", _plate).Place(1f, 0.5f, -80, 0, 130, 56);
            UiKit.Touchable(skip, 130, 56);
            UiKit.TextButton(skip, L10n.T("tut.skip"), () => { Sfx.Click(); onSkip(); });
            Root.gameObject.SetActive(false);
        }

        /// <summary>Shows a step; the same step again changes nothing.</summary>
        public void Show(int step)
        {
            if (!Root.gameObject.activeSelf) Root.gameObject.SetActive(true);
            if (step == _step) return;
            _step = step;
            _progress.text = L10n.T("tut.progress", step + 1, Tutorial.Steps);
            _text.text = L10n.T("tut." + step);
        }

        public void Hide()
        {
            if (Root.gameObject.activeSelf) Root.gameObject.SetActive(false);
            _step = -1;
            Target = null;
        }

        /// <summary>Points the hand at <paramref name="target"/> (null: nothing to point at, no dim) and keeps up with it.</summary>
        public void Follow(RectTransform target, float dt)
        {
            _clock += dt;
            Target = target;
            bool has = target != null && target.gameObject.activeInHierarchy;
            if (_hand.gameObject.activeSelf != has) _hand.gameObject.SetActive(has);
            foreach (RectTransform dim in _dim) if (dim.gameObject.activeSelf != has) dim.gameObject.SetActive(has);
            if (!has) return;

            // The target's box in the layer's own units; the hole follows it wherever it scrolls.
            Vector3[] corners = new Vector3[4];
            target.GetWorldCorners(corners);
            Vector2 min = Local(corners[0]), max = Local(corners[2]);
            min -= new Vector2(Hole, Hole);
            max += new Vector2(Hole, Hole);
            float w = _canvas.rect.width, h = _canvas.rect.height;
            Vector2 centre = new Vector2(0f, 0f);
            Box(_dim[0], -w / 2f, max.y, w / 2f, h / 2f);
            Box(_dim[1], -w / 2f, -h / 2f, w / 2f, min.y);
            Box(_dim[2], -w / 2f, min.y, min.x, max.y);
            Box(_dim[3], max.x, min.y, w / 2f, max.y);

            // The hand rests at the lower right of the target and taps towards it.
            float bob = Mathf.Abs(Mathf.Sin(_clock * 3.2f)) * 18f;
            _handAt = new Vector2(max.x - Hole + 10f, min.y + Hole - 10f) + new Vector2(bob, -bob);
            _hand.anchoredPosition = _handAt;
        }

        Vector2 Local(Vector3 world)
        {
            Vector3 local = Root.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }

        static void Box(RectTransform rt, float x0, float y0, float x1, float y1)
        {
            if (x1 < x0) x1 = x0;
            if (y1 < y0) y1 = y0;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0f, 0f);
            rt.anchoredPosition = new Vector2(x0, y0);
            rt.sizeDelta = new Vector2(x1 - x0, y1 - y0);
        }
    }
}

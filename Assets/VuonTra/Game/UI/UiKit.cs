using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>Helpers for building the uGUI tree in code: rects, images, text, chunky buttons, sliders.</summary>
    public static class UiKit
    {
        const int UiLayer = 5;
        /// <summary>How far a chunky button's face sits above its lip.</summary>
        public const float LipHeight = 10f;

        static Font _font;

        /// <summary>
        /// A system font that has Vietnamese glyphs. Temporary: a bundled rounded font replaces
        /// this in the art milestone so every device shows the same letters.
        /// </summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Avenir Next", "Helvetica Neue", "Arial", "Roboto", "Noto Sans", "Segoe UI" }, 32);
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        public static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform)) { layer = UiLayer };
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        /// <summary>Stretch between two anchor corners, with pixel insets from each edge.</summary>
        public static RectTransform Stretch(this RectTransform rt, float x0, float y0, float x1, float y1,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        /// <summary>Fixed size, centred on a point given relative to an anchor.</summary>
        public static RectTransform Place(this RectTransform rt, float anchorX, float anchorY, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        /// <summary>Full width minus insets, a fixed height, measured up from the parent's bottom edge.</summary>
        public static RectTransform Band(this RectTransform rt, float left, float right, float bottom, float height)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
            return rt;
        }

        public static Image AddImage(this RectTransform rt, Sprite sprite, Color color, bool sliced = false)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        public static Text AddText(this RectTransform rt, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        /// <summary>A soft drop shadow in the bible's ink, so text reads on any backdrop.</summary>
        public static void AddShadow(this Text text, float alpha = 0.55f, float distance = 3f)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Palette.Alpha(Palette.Ink, alpha);
            shadow.effectDistance = new Vector2(0, -distance);
        }

        /// <summary>The sprite used for soft drop shadows; set once by the screen when it builds.</summary>
        public static Sprite ShadowSprite;

        /// <summary>
        /// A soft shadow under a panel or button, which is what lifts it off the scene. It is the
        /// first child, so it draws behind everything else the caller adds afterwards.
        /// </summary>
        public static void AddShadow(this RectTransform rt, float spread = 18f, float drop = 12f, float alpha = 0.34f)
        {
            if (ShadowSprite == null) return;
            var shadow = Rect("Shadow", rt).Stretch(0, 0, 1, 1, -spread, -spread - drop, -spread, -spread + drop);
            shadow.AddImage(ShadowSprite, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), alpha), true);
            shadow.SetAsFirstSibling();
        }

        /// <summary>
        /// A candy-style button: a darker lip with a raised face on top. The returned view lets the
        /// caller restyle it (enabled / disabled) and pop it on press.
        /// </summary>
        public static ChunkyButton Chunky(RectTransform root, Sprite panel, Action onClick)
        {
            var view = new ChunkyButton { Root = root };
            view.Lip = root.AddImage(panel, Palette.CarrotLip, true);
            view.Lip.raycastTarget = true;
            view.Button = root.gameObject.AddComponent<Button>();
            view.Button.transition = Selectable.Transition.None;
            view.Button.targetGraphic = view.Lip;
            view.FaceRect = Rect("Face", root).Stretch(0, 0, 1, 1, 0, LipHeight, 0, 0);
            view.Face = view.FaceRect.AddImage(panel, Palette.Carrot, true);
            // A lighter band across the top half reads as gloss.
            Rect("Shine", view.FaceRect).Stretch(0, 0.5f, 1, 1, 5, 0, 5, 5)
                .AddImage(panel, new Color(1f, 1f, 1f, 0.2f), true);
            view.Button.onClick.AddListener(() => onClick());
            return view;
        }

        /// <summary>A horizontal 0–1 slider in the game's style.</summary>
        public static Slider AddSlider(RectTransform root, Sprite panel, Sprite round, float value, Action<float> onChange)
        {
            var slider = root.gameObject.AddComponent<Slider>();
            var track = Rect("Track", root).Stretch(0, 0.5f, 1, 0.5f, 0, -9, 0, -9);
            track.AddImage(panel, Palette.Track, true).raycastTarget = true;

            var fillArea = Rect("Fill Area", root).Stretch(0, 0.5f, 1, 0.5f, 0, -9, 0, -9);
            var fill = Rect("Fill", fillArea).Stretch(0, 0, 0, 1);
            fill.AddImage(panel, Palette.Carrot, true);

            var handleArea = Rect("Handle Area", root).Stretch(0, 0.5f, 1, 0.5f, 26, -26, 26, -26);
            var handle = Rect("Handle", handleArea);
            handle.anchorMin = new Vector2(0, 0.5f);
            handle.anchorMax = new Vector2(0, 0.5f);
            handle.sizeDelta = new Vector2(52, 52);
            Image knob = handle.AddImage(round, Palette.Card);
            knob.raycastTarget = true;
            knob.preserveAspect = true;
            Rect("Dot", handle).Place(0.5f, 0.5f, 0, 0, 24, 24).AddImage(round, Palette.Carrot);

            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = knob;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;
            slider.onValueChanged.AddListener(v => onChange(v));
            return slider;
        }

        public static string Seconds(double value)
        {
            return L10n.Number(value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ','));
        }

        public static string Times(double value)
        {
            return L10n.Number(value.ToString(value >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture).Replace('.', ','));
        }
    }

    public sealed class ChunkyButton
    {
        public RectTransform Root, FaceRect;
        public Image Lip, Face;
        public Button Button;

        /// <summary>Carrot when it can be pressed, washed-out paper when it cannot.</summary>
        public void SetEnabled(bool on)
        {
            Button.interactable = on;
            Lip.color = on ? Palette.CarrotLip : Palette.OffLip;
            Face.color = on ? Palette.Carrot : Palette.Off;
        }

        /// <summary>The face dips onto the lip and springs back.</summary>
        public void Press(Tweens tweens)
        {
            RectTransform face = FaceRect;
            tweens.Add(0.16f, t =>
            {
                if (face == null) return;
                float dip = Ease.Pop(t) * UiKit.LipHeight * 0.8f;
                face.offsetMin = new Vector2(0, UiKit.LipHeight - dip);
                face.offsetMax = new Vector2(0, -dip);
            }, Ease.Linear);
        }
    }
}

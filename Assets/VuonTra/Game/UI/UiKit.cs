using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>Small helpers for building the uGUI tree in code, plus the game's palette.</summary>
    public static class UiKit
    {
        public static readonly Color Night = new Color32(0x16, 0x1a, 0x3a, 0xff);
        public static readonly Color Panel = new Color32(0x20, 0x25, 0x4f, 0xff);
        public static readonly Color Panel2 = new Color32(0x2b, 0x31, 0x66, 0xff);
        public static readonly Color Line = new Color32(0x3d, 0x44, 0x80, 0xff);
        public static readonly Color Lantern = new Color32(0xff, 0xc8, 0x66, 0xff);
        public static readonly Color Cream = new Color32(0xff, 0xf1, 0xd2, 0xff);
        public static readonly Color Muted = new Color32(0xaa, 0xb0, 0xda, 0xff);
        public static readonly Color Ink = new Color32(0x2a, 0x21, 0x40, 0xff);
        public static readonly Color Moss = new Color32(0x3f, 0x8a, 0x6c, 0xff);

        const int UiLayer = 5;
        static Font _font;

        /// <summary>
        /// A system font that has Vietnamese glyphs. Temporary: a bundled font replaces this
        /// in the art milestone so every device shows the same letters.
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

        public static string Seconds(double value)
        {
            return value.ToString("0.0", CultureInfo.InvariantCulture).Replace('.', ',');
        }

        public static string Times(double value)
        {
            return value.ToString(value >= 100 ? "0" : "0.0", CultureInfo.InvariantCulture).Replace('.', ',');
        }
    }
}

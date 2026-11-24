using VuonTra.Core;
using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public static class UiKit
    {
        const int UiLayer = 5;
        public const float LipHeight = 10f;

        static Font _font;

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

        public static RectTransform Stretch(this RectTransform rt, float x0, float y0, float x1, float y1,
            float left = 0, float bottom = 0, float right = 0, float top = 0)
        {
            rt.anchorMin = new Vector2(x0, y0);
            rt.anchorMax = new Vector2(x1, y1);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
            return rt;
        }

        public static RectTransform Place(this RectTransform rt, float anchorX, float anchorY, float x, float y, float width, float height)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(anchorX, anchorY);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(width, height);
            return rt;
        }

        public static RectTransform Band(this RectTransform rt, float left, float right, float bottom, float height)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 0);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, bottom + height);
            return rt;
        }

        public static Image AddTapCatcher(this RectTransform rt)
        {
            Image catcher = rt.AddImage(null, new Color(0, 0, 0, 0));
            catcher.raycastTarget = true;
            return catcher;
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

        const string LargeTextKey = "text.large";
        public const float LargeScale = 1.2f;
        public static bool LargeText { get; private set; }
        public static float TextScale => LargeText ? LargeScale : 1f;
        public static event Action TextChanged;

        public static void LoadTextSize() => LargeText = PlayerPrefs.GetInt(LargeTextKey, 0) == 1;

        public static void SetLargeText(bool on)
        {
            if (LargeText == on) return;
            LargeText = on;
            PlayerPrefs.SetInt(LargeTextKey, on ? 1 : 0);
            TextChanged?.Invoke();
        }

        public static int Scaled(int size) => Mathf.RoundToInt(size * TextScale);

        public const float MinTouch = 88f;

        public static void Touchable(RectTransform rt, float width, float height)
        {
            float dx = Mathf.Max(0f, (MinTouch - width) / 2f), dy = Mathf.Max(0f, (MinTouch - height) / 2f);
            if (dx <= 0f && dy <= 0f) return;
            Image hit = Rect("Hit", rt).Stretch(0, 0, 1, 1, -dx, -dy, -dx, -dy).AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
        }

        public static Text AddText(this RectTransform rt, string text, int size, Color color,
            TextAnchor anchor = TextAnchor.MiddleCenter, FontStyle style = FontStyle.Normal)
        {
            var t = rt.gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.fontSize = Scaled(size);
            t.color = color;
            t.alignment = anchor;
            t.fontStyle = style;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            return t;
        }

        public static Text FitOneLine(this Text text, float width, int least)
        {
            text.resizeTextForBestFit = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            while (text.fontSize > least && text.preferredWidth > width) text.fontSize--;
            return text;
        }

        public static void Strokable(RectTransform rt, ScrollRect scroll, Action onStroke, Action onTap)
        {
            Image hit = rt.GetComponent<Image>();
            if (hit == null) hit = rt.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var trigger = rt.gameObject.AddComponent<EventTrigger>();
            bool stroked = false;
            Entry(trigger, EventTriggerType.InitializePotentialDrag, e => { if (scroll != null && e is PointerEventData p) scroll.OnInitializePotentialDrag(p); });
            Entry(trigger, EventTriggerType.BeginDrag, e =>
            {
                stroked = false;
                if (scroll != null && e is PointerEventData p) scroll.OnBeginDrag(p);
            });
            Entry(trigger, EventTriggerType.Drag, e =>
            {
                if (scroll != null && e is PointerEventData p) scroll.OnDrag(p);
                if (stroked) return;
                stroked = true;
                onStroke?.Invoke();
            });
            Entry(trigger, EventTriggerType.EndDrag, e =>
            {
                stroked = false;
                if (scroll != null && e is PointerEventData p) scroll.OnEndDrag(p);
            });
            Entry(trigger, EventTriggerType.PointerClick, _ =>
            {
                if (stroked) { stroked = false; return; }
                onTap?.Invoke();
            });
        }

        static void Entry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(call);
            trigger.triggers.Add(entry);
        }

        public static Text Stars(RectTransform parent, int tier, float anchorX, float anchorY, float x, float y, int size = 18)
        {
            int stars = Rarity.Stars(tier);
            var pill = Rect("Stars", parent).Place(anchorX, anchorY, x, y, stars * size * 0.95f + 16f, size + 10f);
            pill.AddImage(null, Palette.Alpha(Palette.Paper, 0.92f));
            return Rect("Text", pill).Stretch(0, 0, 1, 1).AddText(Rarity.Marks(tier), size, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        public static void AddShadow(this Text text, float alpha = 0.55f, float distance = 3f)
        {
            var shadow = text.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Palette.Alpha(Palette.Ink, alpha);
            shadow.effectDistance = new Vector2(0, -distance);
        }

        static Sprite _fadeUp;

        public static Sprite FadeUp
        {
            get
            {
                if (_fadeUp != null) return _fadeUp;
                var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
                for (int y = 0; y < 64; y++)
                {
                    float a = 1f - y / 63f;
                    tex.SetPixel(0, y, new Color(1, 1, 1, a * a));
                }
                tex.Apply();
                _fadeUp = Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f), 100f);
                _fadeUp.hideFlags = HideFlags.HideAndDontSave;
                return _fadeUp;
            }
        }

        public static Sprite ShadowSprite;

        public static void AddShadow(this RectTransform rt, float spread = 18f, float drop = 12f, float alpha = 0.34f)
        {
            if (ShadowSprite == null) return;
            var shadow = Rect("Shadow", rt).Stretch(0, 0, 1, 1, -spread, -spread - drop, -spread, -spread + drop);
            shadow.AddImage(ShadowSprite, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), alpha), true);
            shadow.SetAsFirstSibling();
        }

        public const int TextSmall = 22;
        public const int TextBody = 26;
        public const int TextTitle = 36;
        public const float CardGap = 18f, CardPad = 28f;

        public static Button Secondary(RectTransform root, Sprite panel, string label, Action onClick, int size = TextBody)
        {
            Image edge = root.AddImage(panel, Palette.Mix(Palette.Ink, Palette.Paper, 0.25f), true);
            edge.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = edge;
            button.onClick.AddListener(() => onClick());
            Rect("Face", root).Stretch(0, 0, 1, 1, 3, 3, 3, 3).AddImage(panel, Palette.Card, true);
            Rect("Text", root).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText(label, size, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        public static Button TextButton(RectTransform root, string label, Action onClick, int size = TextSmall)
        {
            Image hit = root.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            button.onClick.AddListener(() => onClick());
            Rect("Text", root).Stretch(0, 0, 1, 1).AddText(label, size, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            Rect("Rule", root).Band(10, 10, 4, 2).AddImage(null, Palette.Alpha(Palette.Ink, 0.55f));
            return button;
        }

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
            view.Shine = Rect("Shine", view.FaceRect).Stretch(0, 0.5f, 1, 1, 5, 0, 5, 5)
                .AddImage(panel, new Color(1f, 1f, 1f, 0.2f), true).gameObject;
            view.Dip = root.gameObject.AddComponent<PressDip>();
            view.Dip.Face = view.FaceRect;
            view.Dip.Button = view.Button;
            view.Dip.Pressed = onClick;
            view.Button.onClick.AddListener(view.Dip.Fire);
            return view;
        }

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
        public GameObject Shine;
        public PressDip Dip;

        public static Color InkFor(bool on) => on ? Palette.OnCarrot : Palette.Ink;

        public void SetEnabled(bool on)
        {
            Button.interactable = on;
            Lip.color = on ? Palette.CarrotLip : new Color(0, 0, 0, 0);
            Face.color = on ? Palette.Carrot : Palette.Track;
            if (Shine != null && Shine.activeSelf != on) Shine.SetActive(on);
        }

        public void Shake(Tweens tweens)
        {
            if (Motion.Reduced) return;
            RectTransform face = FaceRect;
            tweens.Add(0.28f, t =>
            {
                if (face == null) return;
                float x = Mathf.Sin(t * Mathf.PI * 5f) * (1f - t) * 12f;
                face.offsetMin = new Vector2(x, face.offsetMin.y);
                face.offsetMax = new Vector2(x, face.offsetMax.y);
            }, Ease.Linear, 0f, () =>
            {
                if (face == null) return;
                face.offsetMin = new Vector2(0f, face.offsetMin.y);
                face.offsetMax = new Vector2(0f, face.offsetMax.y);
            });
        }

        public void Press(Tweens tweens)
        {
            if (Dip != null && Dip.Busy) return;
            RectTransform face = FaceRect;
            tweens.Add(Motion.Tap, t =>
            {
                if (face == null) return;
                float dip = Ease.Pop(t) * UiKit.LipHeight * 0.8f;
                face.offsetMin = new Vector2(0, UiKit.LipHeight - dip);
                face.offsetMax = new Vector2(0, -dip);
            }, Ease.Linear);
        }
    }
}

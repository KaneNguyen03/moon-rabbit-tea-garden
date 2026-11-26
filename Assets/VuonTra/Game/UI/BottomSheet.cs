using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed class BottomSheet
    {
        public const float Fling = 1100f;

        public readonly GameObject Root;
        public readonly RectTransform Card;
        readonly Tweens _tweens;
        readonly float _height;
        readonly Action _onClosed;
        int _run;
        float _dragSpeed;

        public bool IsOpen { get; private set; }

        public BottomSheet(RectTransform canvas, string name, GameCatalog catalog, Tweens tweens, float height, Action onClosed, float lipScale = 1f)
        {
            _tweens = tweens;
            _height = height;
            _onClosed = onClosed;

            var overlay = UiKit.Rect(name, canvas).Stretch(0, 0, 1, 1);
            Root = overlay.gameObject;
            overlay.AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.4f), 0.18f)).raycastTarget = true;
            var outside = Root.AddComponent<Button>();
            outside.transition = Selectable.Transition.None;
            outside.onClick.AddListener(Close);

            Card = UiKit.Rect("Card", overlay).Stretch(0, 0, 1, 0, 20, 0, 20, 0);
            Card.pivot = new Vector2(0.5f, 0f);
            Card.sizeDelta = new Vector2(-40, height);
            Card.anchoredPosition = new Vector2(0, -height);
            var shade = UiKit.Rect("Shade", Card).Stretch(0, 1, 1, 1);
            shade.pivot = new Vector2(0.5f, 0f);
            shade.sizeDelta = new Vector2(0, 240);
            shade.anchoredPosition = new Vector2(0, -30);
            shade.AddImage(catalog.Glow, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.4f), 0.35f)).raycastTarget = false;
            Image face = UiFrames.AddCard(Card, FrameKind.Card, catalog.Panel, 12f, "Face", lipScale);
            face.raycastTarget = true;

            var handle = UiKit.Rect("Handle", Card).Place(0.5f, 1f, 0, -24, 300, 48);
            handle.AddTapCatcher();
            UiKit.Rect("Bar", handle).Place(0.5f, 0.5f, 0, 0, 96, 12).AddImage(catalog.Round, Palette.Track);
            var grab = handle.gameObject.AddComponent<EventTrigger>();
            Add(grab, EventTriggerType.BeginDrag, _ => _dragSpeed = 0f);
            Add(grab, EventTriggerType.Drag, e =>
            {
                Canvas c = canvas.GetComponent<Canvas>();
                float dy = ((PointerEventData)e).delta.y / Mathf.Max(0.01f, c != null ? c.scaleFactor : 1f);
                _dragSpeed = Mathf.Lerp(_dragSpeed, dy / Mathf.Max(0.001f, Time.unscaledDeltaTime), 0.5f);
                Drag(dy);
            });
            Add(grab, EventTriggerType.EndDrag, _ => Release(_dragSpeed));

            Root.SetActive(false);
        }

        static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(call);
            trigger.triggers.Add(entry);
        }

        public void Open()
        {
            IsOpen = true;
            Root.SetActive(true);
            Slide(0f, Motion.Time(Motion.Panel), Motion.Curve(Ease.OutBackLight), null);
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            Slide(-_height - 60f, Motion.Time(Motion.PanelOut), Ease.InCubic, () => Root.SetActive(false));
            _onClosed?.Invoke();
        }

        public void Drag(float dy)
        {
            if (!IsOpen) return;
            _run++;
            Card.anchoredPosition = new Vector2(0, Mathf.Clamp(Card.anchoredPosition.y + dy, -_height, 0f));
        }

        public void Release(float speed = 0f)
        {
            if (!IsOpen) return;
            if (speed < -Fling || Card.anchoredPosition.y < -_height * 0.3f) Close();
            else Slide(0f, Motion.Time(Motion.Panel), Motion.Curve(Ease.OutBackLight), null);
        }

        void Slide(float toY, float seconds, Func<float, float> curve, Action done)
        {
            int run = ++_run;
            float fromY = Card.anchoredPosition.y;
            _tweens.Add(seconds, t => { if (run == _run && Card != null) Card.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(fromY, toY, t)); },
                curve, 0f, () => { if (run == _run) done?.Invoke(); });
        }
    }
}

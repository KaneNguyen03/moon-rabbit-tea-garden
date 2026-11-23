using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed class PressDip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public RectTransform Face;
        public Button Button;
        public System.Action Pressed;
        public System.Action Denied;
        public System.Action Repeat;

        public const float HoldDelay = 0.4f, FirstInterval = 0.26f, LeastInterval = 0.07f, QuickerBy = 0.82f;

        bool _held, _swallowClick;
        float _sinceRelease = -1f, _heldFor, _nextRepeat;
        int _repeats;

        public bool Busy => _held || _sinceRelease >= 0f;

        public void OnPointerDown(PointerEventData e) => Hold();
        public void OnPointerUp(PointerEventData e) => Release();

        public void OnPointerClick(PointerEventData e)
        {
            if (Button != null && Button.interactable) return;
            if (_swallowClick) { _swallowClick = false; return; }
            Denied?.Invoke();
        }

        public void Click()
        {
            if (Button != null && !Button.interactable) Denied?.Invoke();
            else Fire();
        }

        public void Fire()
        {
            if (_swallowClick)
            {
                _swallowClick = false;
                return;
            }
            Pressed?.Invoke();
        }

        public void Hold()
        {
            _swallowClick = false;
            if (Button != null && !Button.interactable) return;
            _held = true;
            _sinceRelease = -1f;
            _heldFor = 0f;
            _repeats = 0;
            _nextRepeat = HoldDelay;
            Apply(UiKit.LipHeight * 0.8f);
        }

        public void Release()
        {
            if (!_held) return;
            _held = false;
            _sinceRelease = 0f;
            _swallowClick = _repeats > 0;
        }

        void Update() => Step(Time.unscaledDeltaTime);

        public void Step(float dt)
        {
            if (_held && Repeat != null)
            {
                _heldFor += dt;
                while (_heldFor >= _nextRepeat)
                {
                    Repeat();
                    _repeats++;
                    _nextRepeat += Mathf.Max(LeastInterval, FirstInterval * Mathf.Pow(QuickerBy, _repeats - 1));
                }
            }
            if (_sinceRelease < 0f) return;
            _sinceRelease += dt;
            float t = Mathf.Clamp01(_sinceRelease / Motion.Tap);
            Apply(UiKit.LipHeight * 0.8f * (1f - Ease.OutBack(t)));
            if (t >= 1f)
            {
                _sinceRelease = -1f;
                Apply(0f);
            }
        }

        void Apply(float dip)
        {
            if (Face == null) return;
            Face.offsetMin = new Vector2(Face.offsetMin.x, UiKit.LipHeight - dip);
            Face.offsetMax = new Vector2(Face.offsetMax.x, -dip);
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// A raised button sinks onto its lip while a finger is on it and springs back when the finger lifts.
    /// The face always moves between two fixed places (resting and sunk), whatever was happening before.
    /// A button with <see cref="Repeat"/> set also fires again and again while held, faster and faster.
    /// </summary>
    public sealed class PressDip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        public RectTransform Face;
        public Button Button;
        /// <summary>What a finished touch does when the button can be used.</summary>
        public System.Action Pressed;
        /// <summary>Called when the button is touched while it cannot be used (not enough carrots, say).</summary>
        public System.Action Denied;
        /// <summary>Set on a button that keeps working while held: fired after <see cref="HoldDelay"/>, then quicker each time.</summary>
        public System.Action Repeat;

        /// <summary>How long a finger rests before the first repeat; the first repeat's interval; the quickest; how much quicker each one gets.</summary>
        public const float HoldDelay = 0.4f, FirstInterval = 0.26f, LeastInterval = 0.07f, QuickerBy = 0.82f;

        bool _held, _swallowClick;
        float _sinceRelease = -1f, _heldFor, _nextRepeat;
        int _repeats;

        /// <summary>True while the finger is down or the face is still springing back.</summary>
        public bool Busy => _held || _sinceRelease >= 0f;

        public void OnPointerDown(PointerEventData e) => Hold();
        public void OnPointerUp(PointerEventData e) => Release();

        /// <summary>The Button fires Pressed by itself through <see cref="Fire"/>; here only a refusal is left to say.</summary>
        public void OnPointerClick(PointerEventData e)
        {
            if (Button != null && Button.interactable) return;
            if (_swallowClick) { _swallowClick = false; return; }
            Denied?.Invoke();
        }

        /// <summary>A finished touch. A usable button does its thing; an unusable one says no.</summary>
        public void Click()
        {
            if (Button != null && !Button.interactable) Denied?.Invoke();
            else Fire();
        }

        /// <summary>The Button's own click. After a hold that already repeated, the click on release is not one more.</summary>
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

        /// <summary>One frame: repeats while held, the spring back after. Public so tests can step it.</summary>
        public void Step(float dt)
        {
            if (_held && Repeat != null)
            {
                _heldFor += dt;
                // Each repeat is scheduled from the one before, never from where the finger happens to be now.
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

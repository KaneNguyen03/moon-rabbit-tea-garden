using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// A raised button sinks onto its lip while a finger is on it and springs back when the finger lifts.
    /// The face always moves between two fixed places (resting and sunk), whatever was happening before.
    /// </summary>
    public sealed class PressDip : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public RectTransform Face;
        public Button Button;

        bool _held;
        float _sinceRelease = -1f;

        /// <summary>True while the finger is down or the face is still springing back.</summary>
        public bool Busy => _held || _sinceRelease >= 0f;

        public void OnPointerDown(PointerEventData e) => Hold();
        public void OnPointerUp(PointerEventData e) => Release();

        public void Hold()
        {
            if (Button != null && !Button.interactable) return;
            _held = true;
            _sinceRelease = -1f;
            Apply(UiKit.LipHeight * 0.8f);
        }

        public void Release()
        {
            if (!_held) return;
            _held = false;
            _sinceRelease = 0f;
        }

        void Update() => Step(Time.unscaledDeltaTime);

        /// <summary>One frame of the spring back. Public so tests can step it.</summary>
        public void Step(float dt)
        {
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

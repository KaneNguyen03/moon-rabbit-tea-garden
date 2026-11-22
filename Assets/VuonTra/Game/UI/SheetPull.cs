using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// Lets a list inside the sliding sheet pull the sheet down: when the list already sits at its top and
    /// the finger moves down, the drag goes to the sheet instead of the list until it is let go.
    /// </summary>
    public sealed class SheetPull : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ScrollRect List;
        /// <summary>Screen pixels per canvas unit, read when a drag starts.</summary>
        public Func<float> Scale = () => 1f;
        /// <summary>The sheet moved by this many canvas units (up is positive).</summary>
        public Action<float> Dragged;
        /// <summary>The finger let go at this speed, canvas units a second.</summary>
        public Action<float> Released;

        /// <summary>True while a drag is moving the sheet rather than the list.</summary>
        public bool Pulling { get; private set; }
        float _speed, _scale;

        public void OnBeginDrag(PointerEventData e)
        {
            bool atTop = List == null || List.verticalNormalizedPosition >= 0.999f;
            Pulling = atTop && e.delta.y < 0f;
            if (!Pulling) return;
            _scale = Mathf.Max(0.01f, Scale());
            _speed = 0f;
            if (List != null) { List.StopMovement(); List.enabled = false; } // one gesture at a time
        }

        public void OnDrag(PointerEventData e)
        {
            if (!Pulling) return;
            float dy = e.delta.y / _scale;
            _speed = Mathf.Lerp(_speed, dy / Mathf.Max(0.001f, Time.unscaledDeltaTime), 0.5f);
            Dragged?.Invoke(dy);
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (!Pulling) return;
            Pulling = false;
            if (List != null) List.enabled = true;
            Released?.Invoke(_speed);
        }
    }
}

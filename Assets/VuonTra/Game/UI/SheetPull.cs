using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public sealed class SheetPull : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public ScrollRect List;
        public Func<float> Scale = () => 1f;
        public Action<float> Dragged;
        public Action<float> Released;

        public bool Pulling { get; private set; }
        float _speed, _scale;

        public void OnBeginDrag(PointerEventData e)
        {
            bool atTop = List == null || List.verticalNormalizedPosition >= 0.999f;
            Pulling = atTop && e.delta.y < 0f;
            if (!Pulling) return;
            _scale = Mathf.Max(0.01f, Scale());
            _speed = 0f;
            if (List != null) { List.StopMovement(); List.enabled = false; }
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

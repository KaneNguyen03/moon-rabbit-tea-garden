using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VuonTra.Game.UI
{
    public sealed class CardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const float Threshold = 80f;

        public Action<int> Stepped;

        public void OnBeginDrag(PointerEventData e) { }

        public void OnDrag(PointerEventData e) { }

        public void OnEndDrag(PointerEventData e)
        {
            Vector2 travel = e.position - e.pressPosition;
            if (Mathf.Abs(travel.x) < Threshold || Mathf.Abs(travel.x) < Mathf.Abs(travel.y)) return;
            Stepped?.Invoke(travel.x < 0f ? 1 : -1);
        }
    }
}

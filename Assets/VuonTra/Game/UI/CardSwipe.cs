using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// A sideways swipe across a card: left means "next", right means "previous". Only the end of the
    /// drag counts, and only when it travelled far enough sideways to be meant.
    /// </summary>
    public sealed class CardSwipe : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>How far sideways, in screen pixels, a drag must go before it is a swipe.</summary>
        public const float Threshold = 80f;

        /// <summary>+1 for a swipe to the left (next), -1 for a swipe to the right (previous).</summary>
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

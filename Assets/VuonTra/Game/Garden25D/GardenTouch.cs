using UnityEngine;
using UnityEngine.EventSystems;

namespace VuonTra.Game
{
    public sealed class GardenTouch : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler, IPointerClickHandler
    {
        GardenPanZoom _panZoom;

        public void Attach(GardenPanZoom panZoom) => _panZoom = panZoom;

        public void OnPointerDown(PointerEventData eventData) => _panZoom.OnPointerDown(eventData);
        public void OnPointerUp(PointerEventData eventData) => _panZoom.OnPointerUp(eventData);
        public void OnDrag(PointerEventData eventData) => _panZoom.OnDrag(eventData);
        public void OnScroll(PointerEventData eventData) => _panZoom.OnScroll(eventData);
        public void OnPointerClick(PointerEventData eventData) => _panZoom.OnPointerClick(eventData);
    }
}

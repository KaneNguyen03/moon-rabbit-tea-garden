using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VuonTra.Core;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Game
{
    public sealed class GardenPanZoom : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler
    {
        const float WheelStep = 0.1f;
        static readonly Plane Floor = new Plane(Vector3.up, Vector3.zero);

        readonly Dictionary<int, Vector2> _fingers = new Dictionary<int, Vector2>();
        GardenCameraRig _rig;
        Tweens _tweens;
        GroundRect _island;
        GroundPoint _wanted;
        double _speedX, _speedZ, _pinchSpan, _pinchDistance;

        public void Attach(GardenCameraRig rig, Tweens tweens, GroundRect island)
        {
            _rig = rig;
            _tweens = tweens;
            _island = island;
        }

        public void SetIsland(GroundRect island) => _island = island;

        public void OnPointerDown(PointerEventData eventData)
        {
            _tweens.Clear();
            _fingers[eventData.pointerId] = eventData.position;
            _wanted = _rig.Focus;
            _speedX = _speedZ = 0d;
            if (_fingers.Count == 2) StartPinch();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_fingers.TryGetValue(eventData.pointerId, out Vector2 before)) return;
            _fingers[eventData.pointerId] = eventData.position;
            if (_fingers.Count >= 2)
            {
                Pinch();
                return;
            }
            GroundPoint from = GroundUnder(before), to = GroundUnder(eventData.position);
            double dx = from.X - to.X, dz = from.Z - to.Z;
            _wanted = new GroundPoint(_wanted.X + dx, _wanted.Z + dz);
            float seconds = Time.unscaledDeltaTime;
            if (seconds > 0f)
            {
                _speedX = dx / seconds;
                _speedZ = dz / seconds;
            }
            _rig.Frame(GardenFraming.Drag(_wanted, _island, _rig.Spec), _rig.Distance);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _fingers.Remove(eventData.pointerId);
            _wanted = _rig.Focus;
            if (_fingers.Count == 0) LetGo();
        }

        public void OnScroll(PointerEventData eventData)
        {
            double next = GardenFraming.Pinch(_rig.Distance, 1d, 1d + eventData.scrollDelta.y * WheelStep, _rig.Spec);
            ZoomTo(next, eventData.position);
            GroundPoint rest = GardenFraming.Settle(_rig.Focus, _island);
            _rig.Frame(rest, _rig.Distance);
        }

        void StartPinch()
        {
            using var two = _fingers.Values.GetEnumerator();
            two.MoveNext();
            Vector2 a = two.Current;
            two.MoveNext();
            _pinchSpan = Vector2.Distance(a, two.Current);
            _pinchDistance = _rig.Distance;
        }

        void Pinch()
        {
            using var two = _fingers.Values.GetEnumerator();
            two.MoveNext();
            Vector2 a = two.Current;
            two.MoveNext();
            Vector2 b = two.Current;
            ZoomTo(GardenFraming.Pinch(_pinchDistance, _pinchSpan, Vector2.Distance(a, b), _rig.Spec), (a + b) / 2f);
        }

        void ZoomTo(double distance, Vector2 around)
        {
            GroundPoint anchor = GroundUnder(around);
            _wanted = GardenFraming.ZoomAbout(_rig.Focus, anchor, _rig.Distance, distance);
            _rig.Frame(GardenFraming.Drag(_wanted, _island, _rig.Spec), distance);
        }

        void LetGo()
        {
            bool still = Motion.Reduced;
            GroundPoint from = _rig.Focus;
            var thrown = new GroundPoint(
                from.X + GardenFraming.GlideReach(_speedX, _rig.Spec, still),
                from.Z + GardenFraming.GlideReach(_speedZ, _rig.Spec, still));
            GroundPoint to = GardenFraming.Settle(thrown, _island);
            double seconds = Math.Max(GardenFraming.GlideSeconds(Math.Sqrt(_speedX * _speedX + _speedZ * _speedZ), _rig.Spec, still), Motion.Travel);
            if (still || (to.X == from.X && to.Z == from.Z))
            {
                _rig.Frame(to, _rig.Distance);
                return;
            }
            double distance = _rig.Distance;
            _tweens.Add((float)seconds, t => _rig.Frame(new GroundPoint(from.X + (to.X - from.X) * t, from.Z + (to.Z - from.Z) * t), distance), Ease.OutCubic);
        }

        GroundPoint GroundUnder(Vector2 screen)
        {
            Ray ray = _rig.View.ScreenPointToRay(screen);
            if (!Floor.Raycast(ray, out float along)) return _rig.Focus;
            Vector3 at = ray.GetPoint(along);
            return new GroundPoint(at.x, at.z);
        }
    }
}

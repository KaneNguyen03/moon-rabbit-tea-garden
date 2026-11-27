using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VuonTra.Core;
using VuonTra.Game.UI;
using Motion = VuonTra.Game.UI.Motion;

namespace VuonTra.Game
{
    public sealed class GardenPanZoom : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler, IScrollHandler, IPointerClickHandler
    {
        const float WheelStep = 0.1f;
        static readonly Plane Floor = new Plane(Vector3.up, Vector3.zero);

        readonly Dictionary<int, Vector2> _fingers = new Dictionary<int, Vector2>();
        GardenCameraRig _rig;
        Tweens _tweens;
        GroundRect _island;
        double _front;
        GroundPoint _wanted;
        Vector2 _pressedAt, _lastTapAt;
        double _lastTapTime = double.NegativeInfinity;
        bool _panning, _pinched;

        public event Action<PointerEventData> Tapped;
        public Func<double> Clock = () => Time.realtimeSinceStartupAsDouble;
        double _speedX, _speedZ, _pinchSpan, _pinchDistance;

        public void Attach(GardenCameraRig rig, Tweens tweens, GroundRect island)
        {
            _rig = rig;
            _tweens = tweens;
            _island = island;
            _front = island.MinZ;
        }

        public void SetIsland(GroundRect island, double front)
        {
            _island = island;
            _front = front;
        }

        public GroundRect Area(double distance) => GardenFraming.FocusArea(_island, _front, distance, _rig.Spec);

        public void OnPointerDown(PointerEventData eventData)
        {
            _tweens.Clear();
            _fingers[eventData.pointerId] = eventData.position;
            _wanted = _rig.Focus;
            _speedX = _speedZ = 0d;
            if (_fingers.Count == 1)
            {
                _pressedAt = eventData.position;
                _panning = _pinched = false;
            }
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
            if (!_panning)
            {
                Vector2 moved = eventData.position - _pressedAt;
                if (GardenFraming.IsTap(moved.x, moved.y, Screen.width, Screen.height)) return;
                _panning = true;
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
            _rig.Frame(GardenFraming.Drag(_wanted, Area(_rig.Distance), _rig.Spec), _rig.Distance);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _fingers.Remove(eventData.pointerId);
            _wanted = _rig.Focus;
            if (_fingers.Count == 0) LetGo();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Vector2 moved = eventData.position - eventData.pressPosition;
            if (_panning || _pinched || !GardenFraming.IsTap(moved.x, moved.y, Screen.width, Screen.height)) return;
            double now = Clock();
            Vector2 apart = eventData.position - _lastTapAt;
            if (GardenFraming.IsDoubleTap(now - _lastTapTime, apart.x, apart.y, Screen.width, Screen.height))
            {
                _lastTapTime = double.NegativeInfinity;
                GlideZoom(GardenFraming.DoubleTapDistance(_rig.Distance, _rig.Spec), GroundUnder(eventData.position));
                return;
            }
            _lastTapTime = now;
            _lastTapAt = eventData.position;
            Tapped?.Invoke(eventData);
        }

        public void OnScroll(PointerEventData eventData)
        {
            double next = GardenFraming.Pinch(_rig.Distance, 1d, 1d + eventData.scrollDelta.y * WheelStep, _rig.Spec);
            ZoomTo(next, eventData.position);
            GroundPoint rest = GardenFraming.Settle(_rig.Focus, Area(_rig.Distance));
            _rig.Frame(rest, _rig.Distance);
        }

        public void StepZoom() => GlideZoom(GardenFraming.NextZoomStep(_rig.Distance, _rig.Spec), _rig.Focus);

        public void TravelTo(GroundPoint spot)
        {
            _tweens.Clear();
            GroundPoint from = _rig.Focus, to = GardenFraming.Settle(spot, Area(_rig.Distance));
            double distance = _rig.Distance;
            if (Motion.Reduced)
            {
                _rig.Frame(to, distance);
                return;
            }
            _tweens.Add(Motion.Travel, t => _rig.Frame(new GroundPoint(from.X + (to.X - from.X) * t, from.Z + (to.Z - from.Z) * t), distance), Ease.InOutCubic);
        }

        void GlideZoom(double toDistance, GroundPoint anchor)
        {
            _tweens.Clear();
            GroundPoint from = _rig.Focus;
            double fromDistance = _rig.Distance;
            GroundPoint Along(double distance) => GardenFraming.Settle(GardenFraming.ZoomAbout(from, anchor, fromDistance, distance), Area(distance));
            if (Motion.Reduced)
            {
                _rig.Frame(Along(toDistance), toDistance);
                return;
            }
            _tweens.Add(Motion.Travel, t =>
            {
                double distance = fromDistance + (toDistance - fromDistance) * t;
                _rig.Frame(Along(distance), distance);
            }, Ease.InOutCubic);
        }

        void StartPinch()
        {
            using var two = _fingers.Values.GetEnumerator();
            two.MoveNext();
            Vector2 a = two.Current;
            two.MoveNext();
            _pinchSpan = Vector2.Distance(a, two.Current);
            _pinchDistance = _rig.Distance;
            _pinched = true;
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
            _rig.Frame(GardenFraming.Drag(_wanted, Area(distance), _rig.Spec), distance);
        }

        void LetGo()
        {
            bool still = Motion.Reduced;
            GroundPoint from = _rig.Focus;
            var thrown = new GroundPoint(
                from.X + GardenFraming.GlideReach(_speedX, _rig.Spec, still),
                from.Z + GardenFraming.GlideReach(_speedZ, _rig.Spec, still));
            GroundPoint to = GardenFraming.Settle(thrown, Area(_rig.Distance));
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

using System.Runtime.InteropServices;
using UnityEngine;

namespace VuonTra.Game
{
    public static class TrackpadPinch
    {
#if UNITY_STANDALONE_OSX && !UNITY_EDITOR
        public static bool Available => true;

        [DllImport("PinchZoom")] static extern void VuonTra_PinchStart();
        [DllImport("PinchZoom")] static extern int VuonTra_PinchRead(out float magnification, out float x, out float y, out int smartTaps);
        [DllImport("PinchZoom")] static extern void VuonTra_PinchStop();

        static bool _started;

        public static void Start()
        {
            if (_started) return;
            try { VuonTra_PinchStart(); _started = true; }
            catch (System.Exception e) { Debug.LogWarning("[VuonTra] Trackpad pinch is off: " + e.Message); }
        }

        public static void Stop()
        {
            if (!_started) return;
            _started = false;
            try { VuonTra_PinchStop(); }
            catch (System.Exception e) { Debug.LogWarning("[VuonTra] Trackpad pinch did not stop cleanly: " + e.Message); }
        }

        public static bool Read(out float magnification, out Vector2 screenPos, out bool smartMagnify)
        {
            magnification = 0f; screenPos = default; smartMagnify = false;
            if (!_started) return false;
            if (VuonTra_PinchRead(out float m, out float x, out float y, out int taps) == 0) return false;
            magnification = m;
            smartMagnify = taps > 0;
            screenPos = new Vector2(Mathf.Clamp01(x) * Screen.width, Mathf.Clamp01(y) * Screen.height);
            return true;
        }
#else
        public static bool Available => false;
        public static void Start() { }
        public static void Stop() { }
        public static bool Read(out float magnification, out Vector2 screenPos, out bool smartMagnify)
        {
            magnification = 0f; screenPos = default; smartMagnify = false;
            return false;
        }
#endif
    }
}

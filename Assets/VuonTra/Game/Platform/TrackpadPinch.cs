using System.Runtime.InteropServices;
using UnityEngine;

namespace VuonTra.Game
{
    /// <summary>
    /// The pinch on a Mac trackpad. Unity's Input System never turns it into touches, so the Mac player
    /// loads a small native bundle (Assets/Plugins/macOS/PinchZoom) that watches the window's magnify
    /// events, and the screen polls it once a frame. Everywhere else <see cref="Available"/> is false and
    /// <see cref="Read"/> finds nothing.
    /// </summary>
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
            catch (System.Exception e) { Debug.LogWarning("[VuonTra] Trackpad pinch is off: " + e.Message); } // the bundle did not load; the wheel still zooms
        }

        public static void Stop()
        {
            if (!_started) return;
            _started = false;
            try { VuonTra_PinchStop(); } catch (System.Exception) { }
        }

        /// <summary>
        /// What the fingers did since the last call: the summed magnification (about ±1 for a full pinch),
        /// the pointer in Unity screen pixels (y up), and whether a two-finger double tap (smart magnify) came.
        /// </summary>
        public static bool Read(out float magnification, out Vector2 screenPos, out bool smartMagnify)
        {
            magnification = 0f; screenPos = default; smartMagnify = false;
            if (!_started) return false;
            if (VuonTra_PinchRead(out float m, out float x, out float y, out int taps) == 0) return false;
            magnification = m;
            smartMagnify = taps > 0;
            // The bundle reports the pointer as a fraction of the window's content area; Unity's screen is that
            // area in pixels, whatever the backing scale, so the fraction maps straight onto it.
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

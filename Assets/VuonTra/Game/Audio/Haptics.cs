using UnityEngine;

namespace VuonTra.Game
{
    /// <summary>
    /// Short buzzes on a phone for the moments that matter (an upgrade, a milestone, a new rabbit).
    /// The player can switch them off in Settings. On a desktop this does nothing.
    /// </summary>
    public static class Haptics
    {
        const string Key = "haptics.on";

        public static bool On { get; private set; } = true;
        /// <summary>How many buzzes were asked for while switched on, and the last strength (for tests).</summary>
        public static int Count { get; private set; }
        public static int LastMilliseconds { get; private set; }

        public static void Load() => On = PlayerPrefs.GetInt(Key, 1) == 1;

        public static void SetOn(bool on)
        {
            On = on;
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
        }

        /// <summary>A light touch: an upgrade landed.</summary>
        public static void Light() => Buzz(12);
        /// <summary>A firmer one: a milestone, a new rabbit.</summary>
        public static void Medium() => Buzz(28);

        static void Buzz(int milliseconds)
        {
            if (!On) return;
            Count++;
            LastMilliseconds = milliseconds;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null) vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception)
            {
                // No vibrator on this device: the game goes on without it.
            }
#endif
            // iPhone: a short tap needs a small native bridge that is not written yet; nothing happens there for now.
        }
    }
}

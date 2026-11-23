using UnityEngine;

namespace VuonTra.Game
{
    public static class Haptics
    {
        const string Key = "haptics.on";

        public static bool On { get; private set; } = true;
        public static int Count { get; private set; }
        public static int LastMilliseconds { get; private set; }

#if UNITY_ANDROID && !UNITY_EDITOR
        static bool _deviceCannotVibrate;
#endif

        public static void Load() => On = PlayerPrefs.GetInt(Key, 1) == 1;

        public static void SetOn(bool on)
        {
            On = on;
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
        }

        public static void Light() => Buzz(12);
        public static void Medium() => Buzz(28);

        static void Buzz(int milliseconds)
        {
            if (!On) return;
            Count++;
            LastMilliseconds = milliseconds;
#if UNITY_ANDROID && !UNITY_EDITOR
            if (_deviceCannotVibrate) return;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                {
                    if (vibrator != null) vibrator.Call("vibrate", (long)milliseconds);
                }
            }
            catch (System.Exception e)
            {
                _deviceCannotVibrate = true;
                Debug.LogWarning("[VuonTra] Haptics are off for this run, the device cannot vibrate: " + e.Message);
            }
#endif
        }
    }
}

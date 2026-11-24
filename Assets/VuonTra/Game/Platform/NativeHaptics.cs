using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public static class NativeHaptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        public static IHapticMotor Create() => new AndroidHapticMotor();

        sealed class AndroidHapticMotor : IHapticMotor
        {
            bool _deviceCannotVibrate;

            public void Pulse(HapticStrength strength)
            {
                if (_deviceCannotVibrate) return;
                try
                {
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    using (var activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                    using (var vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator"))
                    {
                        if (vibrator != null) vibrator.Call("vibrate", (long)Haptics.MillisecondsOf(strength));
                    }
                }
                catch (System.Exception e)
                {
                    _deviceCannotVibrate = true;
                    Debug.LogWarning("[VuonTra] Haptics are off for this run, the device cannot vibrate: " + e.Message);
                }
            }
        }
#else
        public static IHapticMotor Create() => null;
#endif
    }
}

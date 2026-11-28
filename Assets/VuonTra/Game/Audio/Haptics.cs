using UnityEngine;
using VuonTra.Core;
using VuonTra.Game.Platform;

namespace VuonTra.Game
{
    public static class Haptics
    {
        const string Key = "haptics.on";
        public const int LightMilliseconds = 12, MediumMilliseconds = 28;

        public static bool On { get; private set; } = true;
        public static int Count { get; private set; }
        public static int LastMilliseconds { get; private set; }

        static IHapticMotor _motor;
        public static IHapticMotor Motor
        {
            get { if (_motor == null) _motor = MotorFor(NativeHaptics.Create()); return _motor; }
            set => _motor = value ?? MotorFor(null);
        }

        public static IHapticMotor MotorFor(IHapticMotor native) => native ?? new FakeHapticMotor();

        public static int MillisecondsOf(HapticStrength strength) => strength == HapticStrength.Medium ? MediumMilliseconds : LightMilliseconds;

        public static void Load() => On = Prefs.GetInt(Key, 1) == 1;

        public static void SetOn(bool on)
        {
            On = on;
            Prefs.SetInt(Key, on ? 1 : 0);
        }

        public static void Light() => Pulse(HapticStrength.Light);
        public static void Medium() => Pulse(HapticStrength.Medium);

        static void Pulse(HapticStrength strength)
        {
            if (!On) return;
            Count++;
            LastMilliseconds = MillisecondsOf(strength);
            Motor.Pulse(strength);
        }
    }
}

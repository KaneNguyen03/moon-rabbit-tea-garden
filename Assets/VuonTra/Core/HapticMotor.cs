using System.Collections.Generic;

namespace VuonTra.Core
{
    public enum HapticStrength { Light, Medium }

    public interface IHapticMotor
    {
        void Pulse(HapticStrength strength);
    }

    public sealed class FakeHapticMotor : IHapticMotor
    {
        public readonly List<HapticStrength> Pulses = new List<HapticStrength>();

        public int Light => Count(HapticStrength.Light);
        public int Medium => Count(HapticStrength.Medium);

        public void Pulse(HapticStrength strength) => Pulses.Add(strength);

        int Count(HapticStrength strength)
        {
            int n = 0;
            foreach (HapticStrength pulse in Pulses)
                if (pulse == strength) n++;
            return n;
        }
    }
}

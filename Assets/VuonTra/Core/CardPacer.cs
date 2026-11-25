using System;

namespace VuonTra.Core
{
    public sealed class CardPacer
    {
        public const double GapSeconds = 20d;

        double _lastOpen = double.NegativeInfinity;

        public void Note(double now, bool open)
        {
            if (open) _lastOpen = now;
        }

        public bool Rested(double now) => now - _lastOpen >= GapSeconds;

        public double Wait(double now) => Math.Max(0d, GapSeconds - (now - _lastOpen));

        public void Reset() => _lastOpen = double.NegativeInfinity;
    }
}

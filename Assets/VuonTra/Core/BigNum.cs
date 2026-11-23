using System;
using System.Globalization;
using System.Text;

namespace VuonTra.Core
{
    public readonly struct BigNum : IComparable<BigNum>, IEquatable<BigNum>
    {
        const int AlignLimit = 17;

        public readonly double Mantissa;
        public readonly long Exponent;

        public static readonly BigNum Zero = default;
        public static readonly BigNum One = new BigNum(1d, 0L, true);

        BigNum(double mantissa, long exponent, bool normalized)
        {
            Mantissa = mantissa;
            Exponent = exponent;
        }

        public BigNum(double value)
        {
            this = FromParts(value, 0);
        }

        public bool IsZero => Mantissa <= 0d;

        public static BigNum FromParts(double mantissa, long exponent)
        {
            if (double.IsNaN(mantissa) || double.IsInfinity(mantissa) || mantissa <= 0d) return Zero;
            int shift = (int)Math.Floor(Math.Log10(mantissa));
            if (shift != 0)
            {
                mantissa /= Math.Pow(10d, shift);
                exponent += shift;
            }
            if (mantissa >= 10d) { mantissa /= 10d; exponent++; }
            else if (mantissa < 1d) { mantissa *= 10d; exponent--; }
            if (double.IsNaN(mantissa) || double.IsInfinity(mantissa) || mantissa <= 0d) return Zero;
            return new BigNum(mantissa, exponent, true);
        }

        public static BigNum operator +(BigNum a, BigNum b)
        {
            if (a.IsZero) return b;
            if (b.IsZero) return a;
            if (a.Exponent < b.Exponent) { var t = a; a = b; b = t; }
            long diff = a.Exponent - b.Exponent;
            if (diff > AlignLimit) return a;
            return FromParts(a.Mantissa + b.Mantissa / Math.Pow(10d, diff), a.Exponent);
        }

        public static BigNum operator -(BigNum a, BigNum b)
        {
            if (b.IsZero) return a;
            if (a.CompareTo(b) <= 0) return Zero;
            long diff = a.Exponent - b.Exponent;
            if (diff > AlignLimit) return a;
            return FromParts(a.Mantissa - b.Mantissa / Math.Pow(10d, diff), a.Exponent);
        }

        public static BigNum operator *(BigNum a, BigNum b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            return FromParts(a.Mantissa * b.Mantissa, a.Exponent + b.Exponent);
        }

        public static BigNum operator /(BigNum a, BigNum b)
        {
            if (a.IsZero || b.IsZero) return Zero;
            return FromParts(a.Mantissa / b.Mantissa, a.Exponent - b.Exponent);
        }

        public static BigNum operator *(BigNum a, double b) => a * new BigNum(b);
        public static BigNum operator *(double a, BigNum b) => new BigNum(a) * b;
        public static BigNum operator /(BigNum a, double b) => a / new BigNum(b);

        public static bool operator <(BigNum a, BigNum b) => a.CompareTo(b) < 0;
        public static bool operator >(BigNum a, BigNum b) => a.CompareTo(b) > 0;
        public static bool operator <=(BigNum a, BigNum b) => a.CompareTo(b) <= 0;
        public static bool operator >=(BigNum a, BigNum b) => a.CompareTo(b) >= 0;
        public static bool operator ==(BigNum a, BigNum b) => a.Equals(b);
        public static bool operator !=(BigNum a, BigNum b) => !a.Equals(b);

        public BigNum Pow(double power)
        {
            if (double.IsNaN(power)) return Zero;
            if (IsZero) return power == 0d ? One : Zero;
            if (power == 0d) return One;
            double log = Log10() * power;
            if (double.IsNaN(log) || double.IsInfinity(log)) return Zero;
            double whole = Math.Floor(log);
            return FromParts(Math.Pow(10d, log - whole), (long)whole);
        }

        public double Log10()
        {
            return IsZero ? double.NegativeInfinity : Math.Log10(Mantissa) + Exponent;
        }

        public double ToDouble()
        {
            if (IsZero) return 0d;
            if (Exponent > 308) return double.MaxValue;
            if (Exponent < -320) return 0d;
            double v = Mantissa * Math.Pow(10d, Exponent);
            return double.IsInfinity(v) ? double.MaxValue : v;
        }

        public int CompareTo(BigNum other)
        {
            if (IsZero) return other.IsZero ? 0 : -1;
            if (other.IsZero) return 1;
            if (Exponent != other.Exponent) return Exponent < other.Exponent ? -1 : 1;
            return Mantissa.CompareTo(other.Mantissa);
        }

        public bool Equals(BigNum other) => Mantissa == other.Mantissa && Exponent == other.Exponent;
        public override bool Equals(object obj) => obj is BigNum other && Equals(other);
        public override int GetHashCode() => unchecked(Mantissa.GetHashCode() * 397 ^ Exponent.GetHashCode());
        public override string ToString() => ToShortString();

        static readonly string[] SmallSuffixes = { "", "K", "M", "B", "T" };

        public string ToShortString()
        {
            if (IsZero || Exponent < 0) return "0";
            if (Exponent < 3)
            {
                double small = Math.Floor(Mantissa * Math.Pow(10d, Exponent) + 1e-9);
                return small.ToString("0", CultureInfo.InvariantCulture);
            }

            long group = Exponent / 3;
            int rem = (int)(Exponent % 3);
            double v = Mantissa * Math.Pow(10d, rem);
            string digits;
            if (v >= 100d) digits = Math.Floor(v + 1e-9).ToString("0", CultureInfo.InvariantCulture);
            else if (v >= 10d) digits = (Math.Floor(v * 10d + 1e-9) / 10d).ToString("0.0", CultureInfo.InvariantCulture);
            else digits = (Math.Floor(v * 100d + 1e-9) / 100d).ToString("0.00", CultureInfo.InvariantCulture);
            return digits.Replace('.', ',') + Suffix(group);
        }

        public string ToFullString(char group)
        {
            if (IsZero || Exponent < 0) return "0";
            if (Exponent >= 15) return ToShortString();
            string digits = Math.Floor(Mantissa * Math.Pow(10d, Exponent) + 1e-6).ToString("0", CultureInfo.InvariantCulture);
            var sb = new System.Text.StringBuilder(digits.Length + digits.Length / 3);
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && (digits.Length - i) % 3 == 0) sb.Append(group);
                sb.Append(digits[i]);
            }
            return sb.ToString();
        }

        static readonly string[] TwoLetterSuffixes = new string[26 * 26];

        static string Suffix(long group)
        {
            if (group < SmallSuffixes.Length) return SmallSuffixes[group];
            long index = group - SmallSuffixes.Length;
            if (index < TwoLetterSuffixes.Length)
                return TwoLetterSuffixes[index] ?? (TwoLetterSuffixes[index] = Letters(index));
            return Letters(index);
        }

        static string Letters(long index)
        {
            int letters = 2;
            long span = 26L * 26L;
            while (index >= span)
            {
                index -= span;
                letters++;
                span *= 26L;
            }
            var sb = new StringBuilder(letters);
            for (int i = 0; i < letters; i++)
            {
                sb.Insert(0, (char)('a' + (int)(index % 26L)));
                index /= 26L;
            }
            return sb.ToString();
        }
    }
}

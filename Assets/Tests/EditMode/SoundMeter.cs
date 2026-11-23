using System;
using UnityEngine;

namespace VuonTra.Core.Tests
{
    public readonly struct SoundMeter
    {
        public readonly float PeakDb, RmsDb, CentroidHz, Above4k;

        SoundMeter(float peakDb, float rmsDb, float centroidHz, float above4k)
        {
            PeakDb = peakDb; RmsDb = rmsDb; CentroidHz = centroidHz; Above4k = above4k;
        }

        public override string ToString() =>
            $"peak {PeakDb:0.0} dBFS, RMS {RmsDb:0.0} dBFS, centroid {CentroidHz / 1000f:0.00} kHz, above 4 kHz {Above4k * 100f:0} %";

        public static float Db(double amplitude) => (float)(20d * Math.Log10(Math.Max(amplitude, 1e-9)));

        public static float[] Mono(AudioClip clip)
        {
            var data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);
            if (clip.channels == 1) return data;
            var mono = new float[clip.samples];
            for (int i = 0; i < clip.samples; i++)
            {
                float sum = 0f;
                for (int c = 0; c < clip.channels; c++) sum += data[i * clip.channels + c];
                mono[i] = sum / clip.channels;
            }
            return mono;
        }

        public static SoundMeter Of(AudioClip clip, float volume = 1f) => Of(Mono(clip), clip.frequency, volume);

        public static SoundMeter Of(float[] samples, int rate, float volume = 1f)
        {
            double sum = 0d;
            float peak = 0f;
            foreach (float v in samples) { sum += (double)v * v; peak = Math.Max(peak, Math.Abs(v)); }
            double rms = samples.Length > 0 ? Math.Sqrt(sum / samples.Length) : 0d;

            const int size = 4096;
            var power = new double[size / 2 + 1];
            var re = new double[size];
            var im = new double[size];
            int hop = size / 2;
            for (int start = 0; start == 0 || start + size <= samples.Length; start += hop)
            {
                for (int i = 0; i < size; i++)
                {
                    int at = start + i;
                    double hann = 0.5d - 0.5d * Math.Cos(2d * Math.PI * i / (size - 1));
                    re[i] = at < samples.Length ? samples[at] * hann : 0d;
                    im[i] = 0d;
                }
                Fft(re, im);
                for (int k = 0; k <= size / 2; k++) power[k] += re[k] * re[k] + im[k] * im[k];
            }

            double total = 0d, weighted = 0d, high = 0d;
            for (int k = 1; k <= size / 2; k++)
            {
                double hz = k * (double)rate / size;
                total += power[k];
                weighted += power[k] * hz;
                if (hz > 4000d) high += power[k];
            }
            float centroid = total > 0d ? (float)(weighted / total) : 0f;
            float above = total > 0d ? (float)(high / total) : 0f;
            return new SoundMeter(Db(peak * volume), Db(rms * volume), centroid, above);
        }

        static void Fft(double[] re, double[] im)
        {
            int n = re.Length;
            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; (j & bit) != 0; bit >>= 1) j ^= bit;
                j ^= bit;
                if (i < j)
                {
                    (re[i], re[j]) = (re[j], re[i]);
                    (im[i], im[j]) = (im[j], im[i]);
                }
            }
            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2d * Math.PI / len;
                double wr = Math.Cos(angle), wi = Math.Sin(angle);
                for (int i = 0; i < n; i += len)
                {
                    double cr = 1d, ci = 0d;
                    for (int k = 0; k < len / 2; k++)
                    {
                        int a = i + k, b = i + k + len / 2;
                        double tr = re[b] * cr - im[b] * ci, ti = re[b] * ci + im[b] * cr;
                        re[b] = re[a] - tr; im[b] = im[a] - ti;
                        re[a] += tr; im[a] += ti;
                        double next = cr * wr - ci * wi;
                        ci = cr * wi + ci * wr;
                        cr = next;
                    }
                }
            }
        }
    }
}

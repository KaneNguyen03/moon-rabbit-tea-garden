using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.EditorTools
{
    public static class TuneRecorder
    {
        public const string Folder = "docs/am-thanh";
        public const float Seconds = 20f;

        public static readonly string[] Files =
        {
            Folder + "/nhac-trung-thu.wav",
            Folder + "/nhac-tet.wav",
            Folder + "/nhac-noel.wav",
            Folder + "/tieng-cho-hoi.wav",
        };

        static AudioClip[] Clips() => new[]
        {
            Sfx.TuneClipOf(Sfx.Tune.MidAutumn),
            Sfx.TuneClipOf(Sfx.Tune.Tet),
            Sfx.TuneClipOf(Sfx.Tune.Christmas),
            Sfx.MarketClip,
        };

        [MenuItem("VuonTra/Record Season Tunes")]
        public static void Record()
        {
            string root = Path.GetDirectoryName(Application.dataPath);
            Directory.CreateDirectory(Path.Combine(root, Folder));
            AudioClip[] clips = Clips();
            for (int i = 0; i < clips.Length; i++)
            {
                string path = Path.Combine(root, Files[i]);
                File.WriteAllBytes(path, Wave(clips[i], Seconds));
                Debug.Log("TuneRecorder: wrote " + path);
            }
        }

        public static byte[] Wave(AudioClip clip, float seconds)
        {
            var source = new float[clip.samples * clip.channels];
            clip.GetData(source, 0);
            int frames = clip.samples, channels = clip.channels;
            int count = Mathf.RoundToInt(clip.frequency * seconds);
            int bytes = count * 2;
            using (var stream = new MemoryStream(44 + bytes))
            using (var w = new BinaryWriter(stream))
            {
                w.Write(new[] { 'R', 'I', 'F', 'F' });
                w.Write(36 + bytes);
                w.Write(new[] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' });
                w.Write(16);
                w.Write((short)1);
                w.Write((short)1);
                w.Write(clip.frequency);
                w.Write(clip.frequency * 2);
                w.Write((short)2);
                w.Write((short)16);
                w.Write(new[] { 'd', 'a', 't', 'a' });
                w.Write(bytes);
                for (int i = 0; i < count; i++)
                {
                    int frame = i % frames;
                    float sum = 0f;
                    for (int c = 0; c < channels; c++) sum += source[frame * channels + c];
                    float v = Math.Max(-1f, Math.Min(1f, sum / channels));
                    w.Write((short)Math.Round(v * short.MaxValue));
                }
                w.Flush();
                return stream.ToArray();
            }
        }
    }
}

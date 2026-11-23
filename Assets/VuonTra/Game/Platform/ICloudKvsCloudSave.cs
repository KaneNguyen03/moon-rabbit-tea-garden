using System;
using System.Runtime.InteropServices;
using System.Text;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public sealed class ICloudKvsCloudSave : ICloudSave
    {
        public const string Key = "vuontra.save";
        public const int Limit = 1000000;

        public bool Available => Native.Available();
        public int MaxBytes => Limit;

        public void Start() => Native.Start();

        public bool TakeExternalChange() => Native.TakeChanged();

        public void Push(string json, Action<bool> onDone)
        {
            if (!CloudSave.Fits(json, Limit)) { onDone?.Invoke(false); return; }
            bool ok = Native.Set(Key, json) && Native.Synchronize();
            onDone?.Invoke(ok);
        }

        public void Pull(Action<string> onDone) => onDone?.Invoke(Native.Get(Key));

        public void Delete(Action<bool> onDone)
        {
            Native.Remove(Key);
            Native.Synchronize();
            onDone?.Invoke(Native.Get(Key) == null);
        }

        static byte[] Utf8(string s) => Encoding.UTF8.GetBytes((s ?? "") + "\0");

        static string FromUtf8(IntPtr p)
        {
            if (p == IntPtr.Zero) return null;
            int n = 0;
            while (Marshal.ReadByte(p, n) != 0) n++;
            var bytes = new byte[n];
            Marshal.Copy(p, bytes, 0, n);
            return Encoding.UTF8.GetString(bytes);
        }

        static class Native
        {
#if UNITY_IOS && !UNITY_EDITOR
            [DllImport("__Internal")] static extern void VuonTraCloudKvs_Start();
            [DllImport("__Internal")] static extern int VuonTraCloudKvs_Available();
            [DllImport("__Internal")] static extern int VuonTraCloudKvs_Set(byte[] key, byte[] value);
            [DllImport("__Internal")] static extern IntPtr VuonTraCloudKvs_Get(byte[] key);
            [DllImport("__Internal")] static extern void VuonTraCloudKvs_Free(IntPtr text);
            [DllImport("__Internal")] static extern void VuonTraCloudKvs_Remove(byte[] key);
            [DllImport("__Internal")] static extern int VuonTraCloudKvs_Synchronize();
            [DllImport("__Internal")] static extern int VuonTraCloudKvs_TakeChanged();

            public static void Start() => VuonTraCloudKvs_Start();
            public static bool Available() => VuonTraCloudKvs_Available() != 0;
            public static bool Set(string key, string value) => VuonTraCloudKvs_Set(Utf8(key), Utf8(value)) != 0;
            public static string Get(string key)
            {
                IntPtr p = VuonTraCloudKvs_Get(Utf8(key));
                if (p == IntPtr.Zero) return null;
                string text = FromUtf8(p);
                VuonTraCloudKvs_Free(p);
                return text;
            }
            public static void Remove(string key) => VuonTraCloudKvs_Remove(Utf8(key));
            public static bool Synchronize() => VuonTraCloudKvs_Synchronize() != 0;
            public static bool TakeChanged() => VuonTraCloudKvs_TakeChanged() != 0;
#else
            public static void Start() { }
            public static bool Available() => false;
            public static bool Set(string key, string value) => false;
            public static string Get(string key) => null;
            public static void Remove(string key) { }
            public static bool Synchronize() => false;
            public static bool TakeChanged() => false;
#endif
        }
    }
}

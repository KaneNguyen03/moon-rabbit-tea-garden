using System;
using System.Runtime.InteropServices;
using System.Text;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// The cloud behind the save on iOS (#39): Apple's iCloud key-value store, through the small native plugin in
    /// Assets/Plugins/iOS/CloudKvs.mm. One key holds the whole envelope. Apple allows 1 MB in all and 1 MB per key,
    /// and silently drops a value over that, so a push is measured first and refused rather than tried. Off an
    /// iPhone the class compiles to a store that has nothing and takes nothing.
    /// </summary>
    public sealed class ICloudKvsCloudSave : ICloudSave
    {
        /// <summary>The key the envelope sits under in the player's iCloud key-value store.</summary>
        public const string Key = "vuontra.save";
        /// <summary>Apple's cap on the whole store and on one key: 1 MB (1,000,000 bytes, as Apple counts it).</summary>
        public const int Limit = 1000000;

        public bool Available => Native.Available();
        public int MaxBytes => Limit;

        /// <summary>Starts listening for writes from the player's other devices and pulls the latest from iCloud; call once at open.</summary>
        public void Start() => Native.Start();

        /// <summary>True once per write another device made since the last call: time to sync.</summary>
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

        /// <summary>A C string's bytes, with the terminator the native side expects.</summary>
        static byte[] Utf8(string s) => Encoding.UTF8.GetBytes((s ?? "") + "\0");

        /// <summary>The text at a C string the native side handed over, or null for a null pointer.</summary>
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

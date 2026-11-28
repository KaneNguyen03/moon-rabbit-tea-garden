using System.Collections.Generic;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public static class TestPrefs
    {
        public static readonly MemoryPrefs Store = new MemoryPrefs();

        public static void Fresh()
        {
            Store.Clear();
            Prefs.Store = Store;
        }

        public static void GiveBack()
        {
            Prefs.Store = Prefs.Device;
            Store.Clear();
        }
    }

    public sealed class MemoryPrefs : IPrefsStore
    {
        readonly Dictionary<string, object> _values = new Dictionary<string, object>();

        public int Saves { get; private set; }

        public void Clear()
        {
            _values.Clear();
            Saves = 0;
        }

        public bool HasKey(string key) => _values.ContainsKey(key);
        public int GetInt(string key, int fallback) => _values.TryGetValue(key, out object v) && v is int i ? i : fallback;
        public void SetInt(string key, int value) => _values[key] = value;
        public float GetFloat(string key, float fallback) => _values.TryGetValue(key, out object v) && v is float f ? f : fallback;
        public void SetFloat(string key, float value) => _values[key] = value;
        public string GetString(string key, string fallback) => _values.TryGetValue(key, out object v) && v is string s ? s : fallback;
        public void SetString(string key, string value) => _values[key] = value;
        public void DeleteKey(string key) => _values.Remove(key);
        public void Save() => Saves++;
    }
}

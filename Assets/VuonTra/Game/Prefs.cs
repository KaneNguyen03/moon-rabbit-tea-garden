using UnityEngine;

namespace VuonTra.Game
{
    public interface IPrefsStore
    {
        bool HasKey(string key);
        int GetInt(string key, int fallback);
        void SetInt(string key, int value);
        float GetFloat(string key, float fallback);
        void SetFloat(string key, float value);
        string GetString(string key, string fallback);
        void SetString(string key, string value);
        void DeleteKey(string key);
        void Save();
    }

    public static class Prefs
    {
        public static readonly IPrefsStore Device = new PlayerPrefsStore();

        static IPrefsStore _store = Device;

        public static IPrefsStore Store
        {
            get => _store;
            set => _store = value ?? Device;
        }

        public static bool HasKey(string key) => _store.HasKey(key);
        public static int GetInt(string key, int fallback = 0) => _store.GetInt(key, fallback);
        public static void SetInt(string key, int value) => _store.SetInt(key, value);
        public static float GetFloat(string key, float fallback = 0f) => _store.GetFloat(key, fallback);
        public static void SetFloat(string key, float value) => _store.SetFloat(key, value);
        public static string GetString(string key, string fallback = "") => _store.GetString(key, fallback);
        public static void SetString(string key, string value) => _store.SetString(key, value);
        public static void DeleteKey(string key) => _store.DeleteKey(key);
        public static void Save() => _store.Save();

        sealed class PlayerPrefsStore : IPrefsStore
        {
            public bool HasKey(string key) => PlayerPrefs.HasKey(key);
            public int GetInt(string key, int fallback) => PlayerPrefs.GetInt(key, fallback);
            public void SetInt(string key, int value) => PlayerPrefs.SetInt(key, value);
            public float GetFloat(string key, float fallback) => PlayerPrefs.GetFloat(key, fallback);
            public void SetFloat(string key, float value) => PlayerPrefs.SetFloat(key, value);
            public string GetString(string key, string fallback) => PlayerPrefs.GetString(key, fallback);
            public void SetString(string key, string value) => PlayerPrefs.SetString(key, value);
            public void DeleteKey(string key) => PlayerPrefs.DeleteKey(key);
            public void Save() => PlayerPrefs.Save();
        }
    }
}

using System.Collections.Generic;
using NUnit.Framework.Interfaces;
using UnityEngine.TestRunner;
using VuonTra.Core.Tests;
using VuonTra.Game;

[assembly: TestRunCallback(typeof(TestPrefs))]

namespace VuonTra.Core.Tests
{
    public sealed class TestPrefs : ITestRunCallback
    {
        public static readonly MemoryPrefs Store = new MemoryPrefs();

        public void RunStarted(ITest testsToRun)
        {
            Store.Clear();
            Prefs.Store = Store;
        }

        public void TestStarted(ITest test)
        {
            if (test.IsSuite) return;
            Store.Clear();
            Prefs.Store = Store;
        }

        public void TestFinished(ITestResult result) { }

        public void RunFinished(ITestResult testResults)
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

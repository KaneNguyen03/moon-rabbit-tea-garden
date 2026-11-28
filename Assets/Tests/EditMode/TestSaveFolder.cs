using System;
using System.IO;
using System.Text;
using NUnit.Framework.Interfaces;
using UnityEngine;
using UnityEngine.TestRunner;
using VuonTra.Core.Tests;
using VuonTra.Game;

[assembly: TestRunCallback(typeof(TestSaveFolder))]

namespace VuonTra.Core.Tests
{
    public sealed class TestSaveFolder : ITestRunCallback
    {
        const int LongestName = 160;
        const string BetweenTests = "_between-tests";

        static Func<string> _player;

        public static string Root => Path.Combine(Application.temporaryCachePath, "tests");

        public static string For(string testName) => Path.Combine(Root, Safe(testName));

        static string Safe(string name)
        {
            var safe = new StringBuilder(name.Length);
            foreach (char c in name) safe.Append(char.IsLetterOrDigit(c) || c == '.' || c == '_' || c == '-' ? c : '_');
            return safe.Length > LongestName ? safe.ToString(safe.Length - LongestName, LongestName) : safe.ToString();
        }

        static void Use(string folder) => SaveStore.DefaultDirectory = () => folder;

        static void Clear(string folder)
        {
            if (Directory.Exists(folder)) Directory.Delete(folder, true);
        }

        public void RunStarted(ITest testsToRun)
        {
            if (_player == null) _player = SaveStore.DefaultDirectory;
            Use(For(BetweenTests));
            TestPrefs.Fresh();
        }

        public void TestStarted(ITest test)
        {
            if (test.IsSuite) return;
            Use(For(test.FullName));
            TestPrefs.Fresh();
        }

        public void TestFinished(ITestResult result)
        {
            if (result.Test.IsSuite) return;
            Use(For(BetweenTests));
            Clear(For(result.Test.FullName));
        }

        public void RunFinished(ITestResult testResults)
        {
            if (_player != null) SaveStore.DefaultDirectory = _player;
            _player = null;
            Clear(Root);
            TestPrefs.GiveBack();
        }
    }
}

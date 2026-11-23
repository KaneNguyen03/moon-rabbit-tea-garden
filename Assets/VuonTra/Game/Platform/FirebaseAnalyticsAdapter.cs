using VuonTra.Core;
#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
using System;
using System.Collections.Generic;
using Firebase;
using Firebase.Analytics;
using Firebase.Crashlytics;
using Firebase.Extensions;
#endif

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// Firebase Analytics and Crashlytics behind the analytics seam (#100). Compiled in only with the VUONTRA_FIREBASE
    /// define on an iOS or Android target; without it this class is an empty shell and <see cref="Create"/> gives null, so
    /// no build references a Firebase assembly. To switch it on the owner adds the Firebase Unity SDK (Analytics and
    /// Crashlytics), GoogleService-Info.plist and google-services.json under Assets/, the define in Player Settings, and
    /// sets <see cref="GameRunner.AnalyticsInPlayerBuilds"/> to true. The Game assembly references Firebase's DLLs by
    /// itself (they are auto-referenced precompiled assemblies), so the asmdef needs no change.
    /// </summary>
    public sealed class FirebaseAnalyticsAdapter
#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
        : IAnalytics
#endif
    {
        /// <summary>The Firebase provider, or null when this build was made without it.</summary>
        public static IAnalytics Create()
        {
#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
            return new FirebaseAnalyticsAdapter();
#else
            return null;
#endif
        }

#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
        /// <summary>Calls made before Firebase said it was ready; played back in order once it is, dropped if it never is.</summary>
        readonly List<Action> _waiting = new List<Action>();
        bool _ready, _failed;

        FirebaseAnalyticsAdapter()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    _failed = true;
                    _waiting.Clear();
                    UnityEngine.Debug.LogWarning("[VuonTra] Firebase is not available: " + task.Result);
                    return;
                }
                // Unity's uncaught exceptions are reported by Crashlytics itself, as fatal.
                Crashlytics.ReportUncaughtExceptionsAsFatal = true;
                _ready = true;
                foreach (Action call in _waiting) call();
                _waiting.Clear();
            });
        }

        void Run(Action call)
        {
            if (_failed) return;
            if (_ready) call();
            else _waiting.Add(call);
        }

        public void Log(string name, params AnalyticsParam[] parameters)
        {
            var list = new Parameter[parameters?.Length ?? 0];
            for (int i = 0; i < list.Length; i++)
            {
                AnalyticsParam p = parameters[i];
                switch (p.Type)
                {
                    case AnalyticsParam.Kind.Text: list[i] = new Parameter(p.Name, p.Text); break;
                    case AnalyticsParam.Kind.Number: list[i] = new Parameter(p.Name, p.Number); break;
                    default: list[i] = new Parameter(p.Name, p.Real); break;
                }
            }
            Run(() => FirebaseAnalytics.LogEvent(name, list));
        }

        public void SetUserProperty(string name, string value) => Run(() => FirebaseAnalytics.SetUserProperty(name, value));

        public void RecordException(Exception e)
        {
            // An exception Unity's log reported was uncaught: Crashlytics has it already.
            if (e == null || e is GameRunner.LoggedException) return;
            Run(() => Crashlytics.LogException(e));
        }
#endif
    }
}

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
    public sealed class FirebaseAnalyticsAdapter
#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
        : IAnalytics
#endif
    {
        public static IAnalytics Create()
        {
#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
            return new FirebaseAnalyticsAdapter();
#else
            return null;
#endif
        }

#if VUONTRA_FIREBASE && (UNITY_IOS || UNITY_ANDROID)
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
            if (e == null || e is GameRunner.LoggedException) return;
            Run(() => Crashlytics.LogException(e));
        }
#endif
    }
}

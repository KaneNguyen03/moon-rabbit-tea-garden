using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VuonTra.Core;
#if VUONTRA_NOTIFICATIONS && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
using Unity.Notifications;
#endif

namespace VuonTra.Game.Platform
{
    public struct Notice
    {
        public long AtUnixSeconds;
        public ReminderKind Kind;
        public string Title, Body;
    }

    public interface INotifier
    {
        void RequestPermission(Action<bool> done);
        void Schedule(IReadOnlyList<Notice> notices);
        void CancelAll();
    }

    public static class Notifications
    {
        const string Key = "remind.on";

        static bool? _on;
        public static bool On => _on ?? (_on = Prefs.GetInt(Key, 0) == 1).Value;

        public static INotifier Notifier = Device();

        public static IReadOnlyList<Notice> Last { get; private set; } = new List<Notice>();

        static GameObject _watchOwner;
        static Action _watch;

        public static void SetOn(bool on)
        {
            Store(on);
            if (!on) { Notifier.CancelAll(); return; }
            Notifier.RequestPermission(granted =>
            {
                if (granted) return;
                Store(false);
                if (_watchOwner != null) _watch?.Invoke();
            });
        }

        public static void Watch(GameObject owner, Action show)
        {
            _watchOwner = owner;
            _watch = show;
        }

        static void Store(bool on)
        {
            _on = on;
            Prefs.SetInt(Key, on ? 1 : 0);
            Prefs.Save();
        }

        public static IReadOnlyList<Notice> GoingAway(GameState state, Modifiers mods, DateTimeOffset localNow)
        {
            var notices = new List<Notice>();
            foreach (Reminder r in Reminders.Plan(state, localNow.ToUnixTimeSeconds(), (int)localNow.Offset.TotalSeconds, mods, On))
                notices.Add(new Notice
                {
                    AtUnixSeconds = r.AtUnixSeconds, Kind = r.Kind,
                    Title = L10n.T("remind.app"),
                    Body = r.Kind == ReminderKind.SeasonBegins ? L10n.T(r.Key, L10n.Season(r.Season)) : L10n.T(r.Key)
                });
            Notifier.CancelAll();
            Notifier.Schedule(notices);
            Last = notices;
            return notices;
        }

        public static void CameBack() => Notifier.CancelAll();

        static INotifier Device()
        {
#if VUONTRA_NOTIFICATIONS && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            return new MobileNotifier();
#else
            return new LogNotifier();
#endif
        }
    }

    public sealed class LogNotifier : INotifier
    {
        public void RequestPermission(Action<bool> done) => done(true);

        public void Schedule(IReadOnlyList<Notice> notices)
        {
            if (notices.Count == 0) { Debug.Log("[VuonTra] Reminders: none (switched off)"); return; }
            var parts = new List<string>();
            foreach (Notice n in notices)
                parts.Add(n.Kind + " " + DateTimeOffset.FromUnixTimeSeconds(n.AtUnixSeconds).ToLocalTime().ToString("yyyy-MM-dd HH:mm") + " \"" + n.Body + "\"");
            Debug.Log("[VuonTra] Reminders (only logged on this platform): " + string.Join("; ", parts));
        }

        public void CancelAll() { }
    }

#if VUONTRA_NOTIFICATIONS && (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
    sealed class MobileNotifier : INotifier
    {
        const string ChannelId = "reminders";
        bool _ready;

        void Init()
        {
            if (_ready) return;
            NotificationCenter.Initialize(new NotificationCenterArgs
            {
                PresentationOptions = NotificationPresentation.Alert | NotificationPresentation.Badge | NotificationPresentation.Sound,
                AndroidChannelId = ChannelId,
                AndroidChannelName = L10n.T("remind.channel"),
                AndroidChannelDescription = L10n.T("remind.channel.sub")
            });
            _ready = true;
        }

        public void RequestPermission(Action<bool> done)
        {
            Init();
            NotificationsPermissionRequest request = NotificationCenter.RequestPermission();
            Waiter.Run(Wait(request, done));
        }

        static IEnumerator Wait(NotificationsPermissionRequest request, Action<bool> done)
        {
            yield return request;
            done(request.Status == NotificationsPermissionStatus.Granted);
        }

        public void Schedule(IReadOnlyList<Notice> notices)
        {
            if (notices.Count == 0) return;
            Init();
            foreach (Notice n in notices)
            {
                var notification = new Notification { Title = n.Title, Text = n.Body, ShowInForeground = false };
                DateTime at = DateTimeOffset.FromUnixTimeSeconds(n.AtUnixSeconds).LocalDateTime;
                NotificationCenter.ScheduleNotification(notification, new NotificationDateTimeSchedule(at));
            }
        }

        public void CancelAll()
        {
            Init();
            NotificationCenter.CancelAllScheduledNotifications();
            NotificationCenter.CancelAllDeliveredNotifications();
            NotificationCenter.ClearBadge();
        }

        sealed class Waiter : MonoBehaviour
        {
            static Waiter _instance;

            public static void Run(IEnumerator routine)
            {
                if (_instance == null)
                {
                    var go = new GameObject("Notifications");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<Waiter>();
                }
                _instance.StartCoroutine(routine);
            }
        }
    }
#endif
}

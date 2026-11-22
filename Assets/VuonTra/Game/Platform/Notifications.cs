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
    /// <summary>One reminder ready for the device: when, and its words in the language the player has chosen.</summary>
    public struct Notice
    {
        public long AtUnixSeconds;
        public ReminderKind Kind;
        public string Title, Body;
    }

    /// <summary>What the device's notification centre is asked to do. Tests use a fake; the Mac and the editor only log.</summary>
    public interface INotifier
    {
        /// <summary>Asks the player whether the game may post reminders; <paramref name="done"/> gets the answer.</summary>
        void RequestPermission(Action<bool> done);
        void Schedule(IReadOnlyList<Notice> notices);
        void CancelAll();
    }

    /// <summary>
    /// The "Nhắc tôi" switch on the pause card and the reminders behind it (#65). Off until the player turns it on;
    /// turning it on is the only time permission is asked, never at the first launch. Each time the game goes to the
    /// background the whole list is planned again (<see cref="Reminders.Plan"/>) and registered; when it comes back
    /// every reminder is taken away.
    /// </summary>
    public static class Notifications
    {
        const string Key = "remind.on";

        static bool? _on;
        /// <summary>The switch, as saved in PlayerPrefs; off for a new player.</summary>
        public static bool On => _on ?? (_on = PlayerPrefs.GetInt(Key, 0) == 1).Value;

        /// <summary>Where the reminders go: the phone's notification centre on iOS and Android, a log line anywhere else.</summary>
        public static INotifier Notifier = Device();

        /// <summary>The latest reminders handed to the device, for tests and the log.</summary>
        public static IReadOnlyList<Notice> Last { get; private set; } = new List<Notice>();

        static GameObject _watchOwner;
        static Action _watch;

        /// <summary>
        /// Turns the switch. Turning it on asks for permission; a refusal turns it back off and tells the row on the
        /// pause card (<see cref="Watch"/>), as the answer can come after the touch.
        /// </summary>
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

        /// <summary>The row that shows the switch: it is shown again when a late answer changes it. One row at a time; the newest wins.</summary>
        public static void Watch(GameObject owner, Action show)
        {
            _watchOwner = owner;
            _watch = show;
        }

        static void Store(bool on)
        {
            _on = on;
            PlayerPrefs.SetInt(Key, on ? 1 : 0);
            PlayerPrefs.Save();
        }

        /// <summary>The game goes to the background: the device is given the whole list again, or nothing when switched off.</summary>
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

        /// <summary>The game is back in front: reminders it no longer needs are taken away, and any already shown cleared.</summary>
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

    /// <summary>The Mac, WebGL and the editor: nothing is posted; one line in the log says what a phone would get.</summary>
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
    /// <summary>iOS and Android through Unity Mobile Notifications 2.x (the unified NotificationCenter).</summary>
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

        /// <summary>Runs the wait for the permission answer, which is a coroutine in the package.</summary>
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

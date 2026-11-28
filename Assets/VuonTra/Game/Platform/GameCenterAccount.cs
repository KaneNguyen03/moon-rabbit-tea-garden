using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public sealed class GameCenterAccount : IAccountService
    {
        public delegate void Authenticator(Action<bool, string> onDone);

        public interface IPrefs
        {
            string Get(string key);
            void Set(string key, string value);
            void Remove(string key);
        }

        public const string LinkedKey = "vuontra.gamecenter.linked";
        public const string NameKey = "vuontra.gamecenter.name";

        readonly Authenticator _auth;
        readonly IPrefs _prefs;
        BackupState _state = BackupState.LocalOnly;
        string _name = "";
        bool _remembered;

        public GameCenterAccount(Authenticator auth, IPrefs prefs)
        {
            _auth = auth;
            _prefs = prefs;
        }

        public static GameCenterAccount Device() => new GameCenterAccount(DeviceAuthenticate, new DevicePrefs());

        public AccountProvider Provider => AccountProvider.GameCenter;
        public BackupState State => _state;
        public bool CanLink => _state == BackupState.LocalOnly || _state == BackupState.Failed;
        public string LinkedName => _name;
        public event Action Changed;

        public void Open()
        {
            if (_prefs.Get(LinkedKey) == "1")
            {
                _remembered = true;
                _state = BackupState.Linked;
                _name = _prefs.Get(NameKey) ?? "";
                Changed?.Invoke();
            }
            Link(null);
        }

        public void Link(Action<bool> onDone)
        {
            if (!_remembered) Set(BackupState.Linking, _name);
            _auth((ok, name) =>
            {
                if (ok)
                {
                    _remembered = true;
                    _prefs.Set(LinkedKey, "1");
                    _prefs.Set(NameKey, name ?? "");
                    Set(BackupState.Linked, name ?? "");
                }
                else if (!_remembered) Set(BackupState.Failed, "");
                onDone?.Invoke(ok);
            });
        }

        public void Unlink()
        {
            _remembered = false;
            _prefs.Remove(LinkedKey);
            _prefs.Remove(NameKey);
            Set(BackupState.LocalOnly, "");
        }

        void Set(BackupState state, string name)
        {
            _state = state;
            _name = name;
            Changed?.Invoke();
        }

        public static string NameFor(PlayerProfile profile, string gcName)
        {
            if (!string.IsNullOrEmpty(profile.GardenName)) return null;
            return Profile.CleanName(gcName);
        }

        static void DeviceAuthenticate(Action<bool, string> onDone)
        {
#if UNITY_IOS && !UNITY_EDITOR
            Social.localUser.Authenticate(ok => onDone(ok, ok ? Social.localUser.userName : ""));
#else
            onDone(false, "");
#endif
        }

        sealed class DevicePrefs : IPrefs
        {
            public string Get(string key) => Prefs.HasKey(key) ? Prefs.GetString(key) : null;
            public void Set(string key, string value) { Prefs.SetString(key, value); Prefs.Save(); }
            public void Remove(string key) { Prefs.DeleteKey(key); Prefs.Save(); }
        }
    }
}

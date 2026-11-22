using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// Sign-in through Game Center on iOS (#39). Game Center signs the player in by itself when the game opens;
    /// a success is remembered on the device so the profile says "linked" at once the next time, before Game
    /// Center has answered again. Game Center has no sign-out, so unlinking only forgets what was remembered here.
    /// The device call is behind a delegate so the rest can be exercised off a device.
    /// </summary>
    public sealed class GameCenterAccount : IAccountService
    {
        /// <summary>Asks Game Center; answers with whether it worked and the player's display name.</summary>
        public delegate void Authenticator(Action<bool, string> onDone);

        /// <summary>Where the remembered sign-in lives: PlayerPrefs on a device, memory in tests.</summary>
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

        /// <summary>The account over the real Game Center and PlayerPrefs; off an iPhone the sign-in simply says no.</summary>
        public static GameCenterAccount Device() => new GameCenterAccount(DeviceAuthenticate, new PlayerPrefsStore());

        public AccountProvider Provider => AccountProvider.GameCenter;
        public BackupState State => _state;
        /// <summary>The button does something unless a sign-in is in flight or already done.</summary>
        public bool CanLink => _state == BackupState.LocalOnly || _state == BackupState.Failed;
        public string LinkedName => _name;
        public event Action Changed;

        /// <summary>The game opened: say what was remembered, then ask Game Center (it shows its own sheet the first time).</summary>
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
                // A remembered link stays: one refused check (offline, say) does not undo what the player did.
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

        /// <summary>
        /// The rule for a garden with no name yet: it takes the Game Center name, cleaned like any name, once.
        /// Null when the garden already has a name (the player's own, or one taken earlier) or Game Center gave none.
        /// </summary>
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

        sealed class PlayerPrefsStore : IPrefs
        {
            public string Get(string key) => PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
            public void Set(string key, string value) { PlayerPrefs.SetString(key, value); PlayerPrefs.Save(); }
            public void Remove(string key) { PlayerPrefs.DeleteKey(key); PlayerPrefs.Save(); }
        }
    }
}

using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public sealed class PlayGamesAccount : IAccountService
    {
        public const string RememberKey = "playgames.linked";

        readonly IPlayGames _games;
        readonly Func<bool> _loadRemembered;
        readonly Action<bool> _saveRemembered;
        BackupState _state = BackupState.LocalOnly;

        public PlayGamesAccount(IPlayGames games, Func<bool> loadRemembered, Action<bool> saveRemembered)
        {
            _games = games;
            _loadRemembered = loadRemembered;
            _saveRemembered = saveRemembered;
        }

        public static PlayGamesAccount WithPlayerPrefs(IPlayGames games) => new PlayGamesAccount(games,
            () => PlayerPrefs.GetInt(RememberKey, 0) == 1,
            v => { PlayerPrefs.SetInt(RememberKey, v ? 1 : 0); PlayerPrefs.Save(); });

        public AccountProvider Provider => AccountProvider.PlayGames;
        public BackupState State => _state;
        public bool CanLink => true;
        public string LinkedName => _state == BackupState.Linked ? (_games.DisplayName ?? "") : "";
        public event Action Changed;

        public void SignInAtOpen() => Attempt(false, null);

        public void Link(Action<bool> onDone) => Attempt(true, onDone);

        public void Unlink()
        {
            _saveRemembered(false);
            Set(BackupState.LocalOnly);
        }

        void Attempt(bool interactive, Action<bool> onDone)
        {
            Set(BackupState.Linking);
            _games.SignIn(interactive, ok =>
            {
                if (ok)
                {
                    _saveRemembered(true);
                    Set(BackupState.Linked);
                }
                else Set(_loadRemembered() ? BackupState.Failed : BackupState.LocalOnly);
                onDone?.Invoke(ok);
            });
        }

        void Set(BackupState state)
        {
            if (_state == state) return;
            _state = state;
            Changed?.Invoke();
        }

        public static string NameFor(PlayerProfile profile, string displayName)
        {
            if (!string.IsNullOrEmpty(profile.GardenName)) return profile.GardenName;
            return Profile.CleanName(displayName) ?? "";
        }
    }
}

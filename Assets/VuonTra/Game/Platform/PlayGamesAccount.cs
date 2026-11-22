using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// The player's Google Play Games account as the profile screen sees it (#40). The game signs in quietly when it
    /// opens; the link button asks Play Games out loud. A sign-in that worked is remembered, so a morning without
    /// network reads as "could not get back in" rather than "never linked", and the player plays as a guest either way.
    /// </summary>
    public sealed class PlayGamesAccount : IAccountService
    {
        /// <summary>The PlayerPrefs key that remembers a sign-in that worked.</summary>
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

        /// <summary>The account for a device: what it remembers lives in PlayerPrefs.</summary>
        public static PlayGamesAccount WithPlayerPrefs(IPlayGames games) => new PlayGamesAccount(games,
            () => PlayerPrefs.GetInt(RememberKey, 0) == 1,
            v => { PlayerPrefs.SetInt(RememberKey, v ? 1 : 0); PlayerPrefs.Save(); });

        public AccountProvider Provider => AccountProvider.PlayGames;
        public BackupState State => _state;
        public bool CanLink => true;
        public string LinkedName => _state == BackupState.Linked ? (_games.DisplayName ?? "") : "";
        public event Action Changed;

        /// <summary>The quiet sign-in at open: nothing is asked of the player.</summary>
        public void SignInAtOpen() => Attempt(false, null);

        /// <summary>The link button: Play Games may show its own screens.</summary>
        public void Link(Action<bool> onDone) => Attempt(true, onDone);

        /// <summary>Play Games v2 has no sign-out; the game stops using the account and forgets that it was linked.</summary>
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

        /// <summary>
        /// The garden's name once the player is signed in: the one the player chose if there is one, else the Play Games
        /// name cleaned like any garden name, else empty (the garden stays unnamed).
        /// </summary>
        public static string NameFor(PlayerProfile profile, string displayName)
        {
            if (!string.IsNullOrEmpty(profile.GardenName)) return profile.GardenName;
            return Profile.CleanName(displayName) ?? "";
        }
    }
}

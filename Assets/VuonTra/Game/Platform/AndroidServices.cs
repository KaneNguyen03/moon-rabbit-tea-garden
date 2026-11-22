using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// Puts Google Play Games behind the runner's account and cloud seams on an Android device (#40), and does
    /// nothing anywhere else. The iOS side (#39) has a file of its own, so the two merge without touching each other.
    /// </summary>
    public static class AndroidServices
    {
        /// <summary>
        /// Wires the real plugin in. True only on an Android device with the Play Games package installed; false in the
        /// editor, on other platforms and on an Android build made without the package, where the runner keeps its local-only services.
        /// </summary>
        public static bool Install(GameRunner runner)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUONTRA_PLAY_GAMES
            Install(runner, new PlayGamesPlugin(),
                () => PlayerPrefs.GetInt(PlayGamesAccount.RememberKey, 0) == 1,
                v => { PlayerPrefs.SetInt(PlayGamesAccount.RememberKey, v ? 1 : 0); PlayerPrefs.Save(); });
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// The wiring itself, over whatever stands for Play Games: the account and the cloud go in, the quiet sign-in
        /// starts, and each time the player comes to be signed in the garden takes their name if it has none and the
        /// open sync runs, so the cloud copy is looked at as soon as it can be reached.
        /// </summary>
        public static void Install(GameRunner runner, IPlayGames games, Func<bool> loadRemembered, Action<bool> saveRemembered)
        {
            var account = new PlayGamesAccount(games, loadRemembered, saveRemembered);
            runner.Account = account;
            runner.Cloud = new SavedGamesCloudSave(games, () => SavedGamesCloudSave.LatestPhoto(runner.State.Album));
            account.Changed += () =>
            {
                if (account.State != BackupState.Linked) return;
                string name = PlayGamesAccount.NameFor(runner.State.Profile, games.DisplayName);
                if (name.Length > 0 && name != runner.State.Profile.GardenName) runner.RenameGarden(name);
                runner.SyncNow(SyncReason.Open);
            };
            account.SignInAtOpen();
        }
    }
}

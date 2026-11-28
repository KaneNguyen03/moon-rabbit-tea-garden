using System;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public static class AndroidServices
    {
        public static bool Install(GameRunner runner)
        {
#if UNITY_ANDROID && !UNITY_EDITOR && VUONTRA_PLAY_GAMES
            Install(runner, new PlayGamesPlugin(),
                () => Prefs.GetInt(PlayGamesAccount.RememberKey, 0) == 1,
                v => { Prefs.SetInt(PlayGamesAccount.RememberKey, v ? 1 : 0); Prefs.Save(); });
            return true;
#else
            return false;
#endif
        }

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

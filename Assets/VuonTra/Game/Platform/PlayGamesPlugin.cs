// The Google Play Games plugin for Unity behind IPlayGames. Compiled only for an Android device with the package
// installed (VUONTRA_PLAY_GAMES comes from the package's presence, see VuonTra.Game.asmdef); the plugin's own classes
// are empty in the editor and on every other platform, and this file is untested on a device as of #40.
#if UNITY_ANDROID && !UNITY_EDITOR && VUONTRA_PLAY_GAMES
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;

namespace VuonTra.Game.Platform
{
    public sealed class PlayGamesPlugin : IPlayGames
    {
        /// <summary>
        /// When two devices wrote the same snapshot, Play Games keeps the copy with the longer play time: that time is
        /// the save's own PlaySeconds, so the garden that was played more wins, in the spirit of SaveSync's rule.
        /// The next open then goes through CloudSync, where the player still sees both copies if they differ.
        /// </summary>
        public const ConflictResolutionStrategy Strategy = ConflictResolutionStrategy.UseLongestPlaytime;

        /// <summary>A snapshot opened by Pull and not yet written: Push writes into it rather than opening the file twice.</summary>
        ISavedGameMetadata _open;

        static PlayGamesPlatform Games => PlayGamesPlatform.Instance;

        public bool SignedIn => Games.IsAuthenticated();

        public string DisplayName => SignedIn ? Games.GetUserDisplayName() ?? "" : "";

        public void SignIn(bool interactive, Action<bool> onDone)
        {
            Action<SignInStatus> done = status => onDone(status == SignInStatus.Success);
            if (interactive) Games.ManuallyAuthenticate(done);
            else Games.Authenticate(done);
        }

        public void Open(string file, Action<object> onDone)
        {
            if (_open != null && _open.IsOpen && _open.Filename == file) { onDone(_open); return; }
            _open = null;
            Games.SavedGame.OpenWithAutomaticConflictResolution(file, DataSource.ReadCacheOrNetwork, Strategy, (status, meta) =>
            {
                if (status == SavedGameRequestStatus.Success) _open = meta;
                onDone(status == SavedGameRequestStatus.Success ? meta : null);
            });
        }

        public void Read(object snapshot, Action<byte[]> onDone)
        {
            Games.SavedGame.ReadBinaryData((ISavedGameMetadata)snapshot, (status, data) => onDone(status == SavedGameRequestStatus.Success ? data : null));
        }

        public void Commit(object snapshot, byte[] data, string description, byte[] coverPng, TimeSpan played, Action<bool> onDone)
        {
            SavedGameMetadataUpdate.Builder update = new SavedGameMetadataUpdate.Builder().WithUpdatedDescription(description).WithUpdatedPlayedTime(played);
            if (coverPng != null) update = update.WithUpdatedPngCoverImage(coverPng);
            _open = null;
            Games.SavedGame.CommitUpdate((ISavedGameMetadata)snapshot, update.Build(), data, (status, meta) => onDone(status == SavedGameRequestStatus.Success));
        }

        public void Delete(object snapshot, Action<bool> onDone)
        {
            _open = null;
            Games.SavedGame.Delete((ISavedGameMetadata)snapshot); // the plugin gives no answer back
            onDone(true);
        }
    }
}
#endif

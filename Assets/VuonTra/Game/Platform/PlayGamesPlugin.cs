#if UNITY_ANDROID && !UNITY_EDITOR && VUONTRA_PLAY_GAMES
using System;
using GooglePlayGames;
using GooglePlayGames.BasicApi;
using GooglePlayGames.BasicApi.SavedGame;

namespace VuonTra.Game.Platform
{
    public sealed class PlayGamesPlugin : IPlayGames
    {
        public const ConflictResolutionStrategy Strategy = ConflictResolutionStrategy.UseLongestPlaytime;

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
            Games.SavedGame.Delete((ISavedGameMetadata)snapshot);
            onDone(true);
        }
    }
}
#endif

using System;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// The few things the game asks of Google Play Games (#40): a sign-in, the player's name, and one snapshot
    /// in Saved Games that can be opened, read, written and removed. The real plugin stands behind this on a
    /// device (<c>PlayGamesPlugin</c>); the tests stand a fake behind it, so every rule above the plugin runs in the editor.
    /// Every callback is called exactly once, on the game thread.
    /// </summary>
    public interface IPlayGames
    {
        /// <summary>True once a sign-in has succeeded in this run.</summary>
        bool SignedIn { get; }
        /// <summary>The name Play Games shows for the player, or empty while not signed in.</summary>
        string DisplayName { get; }
        /// <summary>Signs in: quietly at open, or with Play Games' own screens when the player asked (<paramref name="interactive"/>).</summary>
        void SignIn(bool interactive, Action<bool> onDone);
        /// <summary>Opens the snapshot called <paramref name="file"/>, creating it when there is none; the handle is null when it could not be opened.</summary>
        void Open(string file, Action<object> onDone);
        /// <summary>The bytes in an open snapshot: empty for one just created, null when they could not be read.</summary>
        void Read(object snapshot, Action<byte[]> onDone);
        /// <summary>Writes the bytes with their description, cover picture (PNG, or null for none) and play time, and closes the snapshot.</summary>
        void Commit(object snapshot, byte[] data, string description, byte[] coverPng, TimeSpan played, Action<bool> onDone);
        /// <summary>Removes an open snapshot.</summary>
        void Delete(object snapshot, Action<bool> onDone);
    }
}

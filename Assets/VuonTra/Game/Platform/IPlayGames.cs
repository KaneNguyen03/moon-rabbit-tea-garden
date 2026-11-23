using System;

namespace VuonTra.Game.Platform
{
    public interface IPlayGames
    {
        bool SignedIn { get; }
        string DisplayName { get; }
        void SignIn(bool interactive, Action<bool> onDone);
        void Open(string file, Action<object> onDone);
        void Read(object snapshot, Action<byte[]> onDone);
        void Commit(object snapshot, byte[] data, string description, byte[] coverPng, TimeSpan played, Action<bool> onDone);
        void Delete(object snapshot, Action<bool> onDone);
    }
}

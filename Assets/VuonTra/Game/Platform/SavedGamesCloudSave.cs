using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    public sealed class SavedGamesCloudSave : ICloudSave
    {
        public const string FileName = "vuon";
        public const int Limit = 3 * 1024 * 1024;
        public int MaxBytes => Limit;
        public const int MaxCoverBytes = 800 * 1024;

        readonly IPlayGames _games;
        readonly Func<byte[]> _cover;
        bool _unseen;

        public SavedGamesCloudSave(IPlayGames games, Func<byte[]> cover = null)
        {
            _games = games;
            _cover = cover;
        }

        public bool LastPushTooLarge { get; private set; }

        public bool Available => _games.SignedIn;

        public static string Describe(SaveSummary summary) => L10n.T("playgames.description", summary.GardenLevel, summary.Rabbits);

        public void Push(string json, Action<bool> onDone)
        {
            LastPushTooLarge = false;
            if (!Available || _unseen) { onDone?.Invoke(false); return; }
            byte[] data = Encoding.UTF8.GetBytes(json);
            if (data.Length > Limit)
            {
                LastPushTooLarge = true;
                Debug.LogWarning(L10n.T("playgames.toolarge") + " (" + data.Length + " B)");
                onDone?.Invoke(false);
                return;
            }
            SaveSummary summary = CloudSave.Open(json, out CloudEnvelope env) == EnvelopeStatus.Corrupt ? default : env.Summary;
            byte[] cover = null;
            try { cover = _cover?.Invoke(); }
            catch (Exception e) { Debug.LogWarning("Không đọc được ảnh bìa cho Saved Games: " + e.Message); }
            _games.Open(FileName, snapshot =>
            {
                if (snapshot == null) { onDone?.Invoke(false); return; }
                _games.Commit(snapshot, data, Describe(summary), cover, TimeSpan.FromSeconds(summary.PlaySeconds), ok => onDone?.Invoke(ok));
            });
        }

        public void Pull(Action<string> onDone)
        {
            if (!Available) { onDone?.Invoke(null); return; }
            _games.Open(FileName, snapshot =>
            {
                if (snapshot == null) { _unseen = true; onDone?.Invoke(null); return; }
                _games.Read(snapshot, bytes =>
                {
                    if (bytes == null) { _unseen = true; onDone?.Invoke(null); return; }
                    _unseen = false;
                    onDone?.Invoke(bytes.Length == 0 ? null : Encoding.UTF8.GetString(bytes));
                });
            });
        }

        public void Delete(Action<bool> onDone)
        {
            if (!Available) { onDone?.Invoke(true); return; }
            _games.Open(FileName, snapshot =>
            {
                if (snapshot == null) { onDone?.Invoke(false); return; }
                _games.Delete(snapshot, ok => onDone?.Invoke(ok));
            });
        }

        public static byte[] LatestPhoto(IReadOnlyList<string> album)
        {
            for (int i = album.Count - 1; i >= 0; i--)
            {
                try
                {
                    var file = new FileInfo(album[i]);
                    if (!file.Exists || file.Length > MaxCoverBytes) continue;
                    return File.ReadAllBytes(album[i]);
                }
                catch (Exception e) { Debug.LogWarning("Không đọc được ảnh " + album[i] + ": " + e.Message); }
            }
            return null;
        }
    }
}

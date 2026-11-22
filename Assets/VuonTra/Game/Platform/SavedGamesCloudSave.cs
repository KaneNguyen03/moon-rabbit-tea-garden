using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game.Platform
{
    /// <summary>
    /// The save kept in Google Play Games' Saved Games (#40): one snapshot, holding the same envelope
    /// <see cref="CloudSync"/> hands every cloud, with the latest garden photo as its cover and
    /// "Cấp vườn N · M thỏ" as its description. Which copy wins stays with <see cref="CloudSync"/> and the player;
    /// this class only moves bytes, and never over a copy it has not been able to read.
    /// </summary>
    public sealed class SavedGamesCloudSave : ICloudSave
    {
        /// <summary>The one snapshot's name in Saved Games.</summary>
        public const string FileName = "vuon";
        /// <summary>Saved Games takes at most 3 MB per snapshot.</summary>
        public const int MaxBytes = 3 * 1024 * 1024;
        /// <summary>A cover bigger than this is left off rather than risk the write.</summary>
        public const int MaxCoverBytes = 800 * 1024;

        readonly IPlayGames _games;
        readonly Func<byte[]> _cover;
        /// <summary>A copy up there could not be read: nothing is written over it until it has been seen.</summary>
        bool _unseen;

        public SavedGamesCloudSave(IPlayGames games, Func<byte[]> cover = null)
        {
            _games = games;
            _cover = cover;
        }

        /// <summary>The last push was refused for being over the limit; the save on the device is untouched.</summary>
        public bool LastPushTooLarge { get; private set; }

        public bool Available => _games.SignedIn;

        /// <summary>What Play Games shows next to the snapshot, in the player's language.</summary>
        public static string Describe(SaveSummary summary) => L10n.T("playgames.description", summary.GardenLevel, summary.Rabbits);

        public void Push(string json, Action<bool> onDone)
        {
            LastPushTooLarge = false;
            if (!Available || _unseen) { onDone?.Invoke(false); return; }
            byte[] data = Encoding.UTF8.GetBytes(json);
            if (data.Length > MaxBytes)
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
                    // A snapshot Play Games has just created is empty: nothing up there yet.
                    onDone?.Invoke(bytes.Length == 0 ? null : Encoding.UTF8.GetString(bytes));
                });
            });
        }

        public void Delete(Action<bool> onDone)
        {
            // Not signed in: there is no copy of ours to reach, and nothing to report as left behind.
            if (!Available) { onDone?.Invoke(true); return; }
            _games.Open(FileName, snapshot =>
            {
                if (snapshot == null) { onDone?.Invoke(false); return; }
                _games.Delete(snapshot, ok => onDone?.Invoke(ok));
            });
        }

        /// <summary>The newest album picture that is still on disk and small enough to be a cover, as PNG bytes; null for none.</summary>
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

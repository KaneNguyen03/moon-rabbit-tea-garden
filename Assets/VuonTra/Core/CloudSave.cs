using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VuonTra.Core
{
    /// <summary>Why a sync is being tried; the timer one is the only kind that is rationed.</summary>
    public enum SyncReason
    {
        Open, Background, Milestone, Timer, Manual,
        /// <summary>The cloud said another device wrote its copy (#39).</summary>
        Remote
    }

    public enum SyncOutcome
    {
        /// <summary>Nowhere to sync to: the game has no cloud behind it on this build.</summary>
        Unavailable,
        /// <summary>A timer sync that came too soon after the last attempt.</summary>
        Skipped,
        /// <summary>The cloud could not be reached; the save on the device is untouched and the next attempt will try again.</summary>
        Offline,
        /// <summary>The copy up there is not a save this game wrote (or its check failed): ignored, and the device's copy sent up in its place.</summary>
        Corrupt,
        /// <summary>The copy up there came from a newer build: left alone, nothing sent up.</summary>
        NewerVersion,
        /// <summary>The two copies are the same garden at the same point.</summary>
        Same,
        /// <summary>The device's copy went up.</summary>
        Pushed,
        /// <summary>Each copy is ahead in something, or the cloud is ahead: the player decides, shown both.</summary>
        NeedsChoice,
        /// <summary>The cloud's copy replaced the device's, after the device's was backed up.</summary>
        TookRemote,
        /// <summary>Nothing is up there yet.</summary>
        Nothing,
        /// <summary>The save is bigger than the cloud holds (#39): nothing was sent and the device's copy is untouched.</summary>
        TooLarge
    }

    /// <summary>What one sync attempt found and did.</summary>
    public sealed class SyncReport
    {
        public SyncOutcome Outcome;
        public SaveSummary Local, Remote;
        /// <summary>The save text from the cloud, when it was readable.</summary>
        public string RemoteSave;
        /// <summary>For a choice: which copy the game suggests, by level, then rabbits, then time.</summary>
        public bool SuggestRemote;
        /// <summary>The copy up there was unreadable and has been replaced by this one.</summary>
        public bool RemoteWasCorrupt;
        public long At;
    }

    /// <summary>What the cloud holds: the save text, the save's version, when it was sent, and a check on all of it.</summary>
    public struct CloudEnvelope
    {
        public int Version;
        public long SavedAt;
        public SaveSummary Summary;
        public string Save;
    }

    public enum EnvelopeStatus { Ok, Corrupt, NewerVersion }

    /// <summary>
    /// The text that goes up to the cloud: the save wrapped with its version, the time it was sent, the few numbers a
    /// player needs to compare two copies, and a check. A copy that fails the check is ignored, never loaded.
    /// </summary>
    public static class CloudSave
    {
        public const int EnvelopeVersion = 1;

        /// <summary>How many bytes a text takes up there, as UTF-8: what a store's limit is measured in.</summary>
        public static int Bytes(string text) => text == null ? 0 : Encoding.UTF8.GetByteCount(text);

        /// <summary>True when a text fits under a store's limit, where 0 means no limit.</summary>
        public static bool Fits(string text, int maxBytes) => maxBytes <= 0 || Bytes(text) <= maxBytes;

        public static string Wrap(string save, SaveSummary summary, long now)
        {
            var sb = new StringBuilder(save.Length + 300);
            sb.Append("{\"cloud\":").Append(EnvelopeVersion);
            sb.Append(",\"version\":").Append(SaveCodec.CurrentVersion);
            sb.Append(",\"savedAt\":").Append(now.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"level\":").Append(summary.GardenLevel);
            sb.Append(",\"rabbits\":").Append(summary.Rabbits);
            sb.Append(",\"kinds\":").Append(summary.Kinds);
            sb.Append(",\"played\":").Append(summary.PlaySeconds.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"seen\":").Append(summary.LastSeenUnixSeconds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"code\":").Append(SaveCodec.Quote(summary.Code ?? ""));
            sb.Append(",\"check\":").Append(SaveCodec.Quote(Check(save, SaveCodec.CurrentVersion, now)));
            sb.Append(",\"save\":").Append(SaveCodec.Quote(save));
            sb.Append('}');
            return sb.ToString();
        }

        public static EnvelopeStatus Open(string text, out CloudEnvelope env)
        {
            env = default;
            Dictionary<string, object> root = string.IsNullOrWhiteSpace(text) ? null : SaveCodec.ReadObject(text);
            if (root == null) return EnvelopeStatus.Corrupt;
            if (!SaveCodec.TryNumber(root, "version", out double version) || !SaveCodec.TryNumber(root, "savedAt", out double savedAt)) return EnvelopeStatus.Corrupt;
            if (!root.TryGetValue("save", out object saveObj) || !(saveObj is string save) || !root.TryGetValue("check", out object checkObj) || !(checkObj is string check)) return EnvelopeStatus.Corrupt;
            env.Version = (int)version;
            env.SavedAt = (long)savedAt;
            env.Save = save;
            if (!string.Equals(check, Check(save, env.Version, env.SavedAt), StringComparison.Ordinal)) return EnvelopeStatus.Corrupt;
            env.Summary = new SaveSummary
            {
                GardenLevel = (int)Number(root, "level"), Rabbits = (int)Number(root, "rabbits"), Kinds = (int)Number(root, "kinds"),
                PlaySeconds = Number(root, "played"), LastSeenUnixSeconds = (long)Number(root, "seen"),
                Code = root.TryGetValue("code", out object code) && code is string c ? c : ""
            };
            if (env.Version > SaveCodec.CurrentVersion) return EnvelopeStatus.NewerVersion;
            return EnvelopeStatus.Ok;
        }

        static double Number(Dictionary<string, object> root, string key) => SaveCodec.TryNumber(root, key, out double v) ? v : 0d;

        /// <summary>FNV-1a over the version, the time and the save text: catches a truncated or edited copy, not a determined one.</summary>
        public static string Check(string save, int version, long savedAt)
        {
            const ulong Prime = 1099511628211UL;
            ulong h = 14695981039346656037UL;
            void Mix(string s) { foreach (char ch in s) { h ^= ch; h *= Prime; } h ^= '|'; h *= Prime; }
            Mix(version.ToString(CultureInfo.InvariantCulture));
            Mix(savedAt.ToString(CultureInfo.InvariantCulture));
            Mix(save ?? "");
            return h.ToString("x16", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// The sync itself, with no idea of disks or screens: it reads the device's copy and the cloud's, decides with
    /// <see cref="SaveSync.Choose"/>, sends the device's copy up when it is safe, and otherwise hands the player the
    /// choice. The device's copy is never replaced here except through <see cref="Resolve"/> or <see cref="Restore"/>,
    /// and then only through the replace callback, which is expected to back the old copy up first.
    /// </summary>
    public sealed class CloudSync
    {
        /// <summary>Timer syncs come at most this often; every other reason goes at once.</summary>
        public const int MinSecondsBetweenTimerSyncs = 180;

        readonly ICloudSave _cloud;
        readonly Func<long> _now;

        public CloudSync(ICloudSave cloud, Func<long> now)
        {
            _cloud = cloud;
            _now = now;
        }

        public ICloudSave Cloud => _cloud;
        /// <summary>When the device's copy last went up, in Unix seconds; -1 for never.</summary>
        public long LastPushedAt { get; private set; } = -1;
        public long LastAttemptAt { get; private set; } = -1;
        /// <summary>The last attempt that reached the cloud (or failed to), for the settings screen to show.</summary>
        public SyncReport Last { get; private set; }
        /// <summary>A choice waiting on the player, or null.</summary>
        public SyncReport Pending { get; private set; }

        public bool Due(SyncReason reason, long now) => reason != SyncReason.Timer || LastAttemptAt < 0 || now - LastAttemptAt >= MinSecondsBetweenTimerSyncs;

        /// <summary>One attempt. <paramref name="localSave"/> is the device's copy as text; <paramref name="local"/> its summary.</summary>
        public void Sync(SyncReason reason, string localSave, SaveSummary local, Action<SyncReport> onDone)
        {
            var r = new SyncReport { Local = local, At = _now() };
            if (!_cloud.Available) { r.Outcome = SyncOutcome.Unavailable; onDone?.Invoke(r); return; }
            if (Pending != null) { onDone?.Invoke(Pending); return; }
            if (!Due(reason, r.At)) { r.Outcome = SyncOutcome.Skipped; onDone?.Invoke(r); return; }
            LastAttemptAt = r.At;
            _cloud.Pull(text =>
            {
                if (text == null) { Push(localSave, r, onDone); return; }
                EnvelopeStatus status = CloudSave.Open(text, out CloudEnvelope env);
                if (status == EnvelopeStatus.Corrupt)
                {
                    r.RemoteWasCorrupt = true;
                    Push(localSave, r, onDone);
                    return;
                }
                if (status == EnvelopeStatus.NewerVersion)
                {
                    r.Outcome = SyncOutcome.NewerVersion;
                    Last = r;
                    onDone?.Invoke(r);
                    return;
                }
                r.Remote = env.Summary;
                r.RemoteSave = env.Save;
                switch (SaveSync.Choose(local, env.Summary))
                {
                    case SaveChoice.Same:
                        // The same garden at the same point by the summary; still sent up when the text itself differs
                        // (two milestones inside one second, say), so the cloud never trails the device.
                        if (!string.Equals(localSave, env.Save, StringComparison.Ordinal)) { Push(localSave, r, onDone); break; }
                        r.Outcome = SyncOutcome.Same;
                        if (LastPushedAt < 0) LastPushedAt = env.SavedAt;
                        Last = r;
                        onDone?.Invoke(r);
                        break;
                    case SaveChoice.KeepLocal:
                        Push(localSave, r, onDone);
                        break;
                    default:
                        r.Outcome = SyncOutcome.NeedsChoice;
                        r.SuggestRemote = Suggest(local, env.Summary);
                        Pending = r;
                        Last = r;
                        onDone?.Invoke(r);
                        break;
                }
            });
        }

        /// <summary>Which copy to put forward: the higher garden level, then more rabbits, then the one seen later.</summary>
        public static bool Suggest(SaveSummary local, SaveSummary remote)
        {
            if (remote.GardenLevel != local.GardenLevel) return remote.GardenLevel > local.GardenLevel;
            if (remote.Rabbits != local.Rabbits) return remote.Rabbits > local.Rabbits;
            return remote.LastSeenUnixSeconds > local.LastSeenUnixSeconds;
        }

        /// <summary>The player chose. Taking the cloud's copy goes through <paramref name="replace"/>; keeping the device's sends it up.</summary>
        public void Resolve(bool takeRemote, string localSave, Func<string, bool> replace, Action<SyncReport> onDone)
        {
            SyncReport p = Pending;
            if (p == null) return;
            Pending = null;
            if (takeRemote)
            {
                p.Outcome = replace(p.RemoteSave) ? SyncOutcome.TookRemote : SyncOutcome.Corrupt;
                if (p.Outcome == SyncOutcome.TookRemote) LastPushedAt = p.At;
                Last = p;
                onDone?.Invoke(p);
            }
            else Push(localSave, p, onDone);
        }

        /// <summary>"Save now": the device's copy goes up whatever is there.</summary>
        public void PushNow(string localSave, SaveSummary local, Action<SyncReport> onDone)
        {
            var r = new SyncReport { Local = local, At = _now() };
            if (!_cloud.Available) { r.Outcome = SyncOutcome.Unavailable; onDone?.Invoke(r); return; }
            Pending = null;
            LastAttemptAt = r.At;
            Push(localSave, r, onDone);
        }

        /// <summary>"Restore from the cloud": whatever is up there replaces the device's copy, through <paramref name="replace"/>, which backs the old one up.</summary>
        public void Restore(SaveSummary local, Func<string, bool> replace, Action<SyncReport> onDone)
        {
            var r = new SyncReport { Local = local, At = _now() };
            if (!_cloud.Available) { r.Outcome = SyncOutcome.Unavailable; onDone?.Invoke(r); return; }
            Pending = null;
            LastAttemptAt = r.At;
            _cloud.Pull(text =>
            {
                if (text == null) { r.Outcome = SyncOutcome.Nothing; Last = r; onDone?.Invoke(r); return; }
                EnvelopeStatus status = CloudSave.Open(text, out CloudEnvelope env);
                if (status == EnvelopeStatus.Corrupt) r.Outcome = SyncOutcome.Corrupt;
                else if (status == EnvelopeStatus.NewerVersion) r.Outcome = SyncOutcome.NewerVersion;
                else
                {
                    r.Remote = env.Summary;
                    r.RemoteSave = env.Save;
                    r.Outcome = replace(env.Save) ? SyncOutcome.TookRemote : SyncOutcome.Corrupt;
                }
                Last = r;
                onDone?.Invoke(r);
            });
        }

        void Push(string localSave, SyncReport r, Action<SyncReport> onDone)
        {
            string text = CloudSave.Wrap(localSave, r.Local, r.At);
            if (!CloudSave.Fits(text, _cloud.MaxBytes))
            {
                r.Outcome = SyncOutcome.TooLarge;
                Last = r;
                onDone?.Invoke(r);
                return;
            }
            _cloud.Push(text, ok =>
            {
                r.Outcome = ok ? SyncOutcome.Pushed : SyncOutcome.Offline;
                if (ok) LastPushedAt = r.At;
                Last = r;
                onDone?.Invoke(r);
            });
        }
    }

    /// <summary>What the settings screen shows about the cloud: whether there is one, when the save last went up, the last attempt, and the backup on the device.</summary>
    public struct CloudStatus
    {
        public bool Available;
        /// <summary>There is a cloud on this build but the device is not signed in to it (iCloud off in the settings), so the row says that rather than "no cloud".</summary>
        public bool SignedOut;
        public long LastPushedAt;
        public SyncReport Last;
        public bool HasLocalBackup;
        public long LocalBackupAt;
    }

    /// <summary>A cloud that lives in memory: for tests, previews and a build with no store behind it yet. It can be switched off to play "no network".</summary>
    public sealed class MemoryCloudSave : ICloudSave
    {
        public bool Online = true;
        public string Text;
        public int Pushes, Pulls;
        /// <summary>A cap like a real store's, or 0 for none; a push over it fails like iCloud's would.</summary>
        public int MaxBytes;

        public bool Available => true;

        int ICloudSave.MaxBytes => MaxBytes;

        public void Push(string json, Action<bool> onDone)
        {
            Pushes++;
            bool ok = Online && CloudSave.Fits(json, MaxBytes);
            if (ok) Text = json;
            onDone?.Invoke(ok);
        }

        public void Pull(Action<string> onDone)
        {
            Pulls++;
            onDone?.Invoke(Online ? Text : null);
        }

        public int Deletes;

        public void Delete(Action<bool> onDone)
        {
            Deletes++;
            if (Online) Text = null;
            onDone?.Invoke(Online);
        }
    }
}

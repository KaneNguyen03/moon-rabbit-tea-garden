using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VuonTra.Core
{
    public enum SyncReason
    {
        Open, Background, Milestone, Timer, Manual,
        Remote
    }

    public enum SyncOutcome
    {
        Unavailable,
        Skipped,
        Offline,
        Corrupt,
        NewerVersion,
        Same,
        Pushed,
        NeedsChoice,
        TookRemote,
        Nothing,
        TooLarge
    }

    public sealed class SyncReport
    {
        public SyncOutcome Outcome;
        public SaveSummary Local, Remote;
        public string RemoteSave;
        public bool SuggestRemote;
        public bool RemoteWasCorrupt;
        public long At;
    }

    public struct CloudEnvelope
    {
        public int Version;
        public long SavedAt;
        public SaveSummary Summary;
        public string Save;
    }

    public enum EnvelopeStatus { Ok, Corrupt, NewerVersion }

    public static class CloudSave
    {
        public const int EnvelopeVersion = 1;

        public static int Bytes(string text) => text == null ? 0 : Encoding.UTF8.GetByteCount(text);

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

    public sealed class CloudSync
    {
        public const int MinSecondsBetweenTimerSyncs = 180;

        readonly ICloudSave _cloud;
        readonly Func<long> _now;

        public CloudSync(ICloudSave cloud, Func<long> now)
        {
            _cloud = cloud;
            _now = now;
        }

        public ICloudSave Cloud => _cloud;
        public long LastPushedAt { get; private set; } = -1;
        public long LastAttemptAt { get; private set; } = -1;
        public SyncReport Last { get; private set; }
        public SyncReport Pending { get; private set; }

        public bool Due(SyncReason reason, long now) => reason != SyncReason.Timer || LastAttemptAt < 0 || now - LastAttemptAt >= MinSecondsBetweenTimerSyncs;

        public bool InFlight { get; private set; }

        public void Sync(SyncReason reason, string localSave, SaveSummary local, Action<SyncReport> onDone)
        {
            var r = new SyncReport { Local = local, At = _now() };
            if (!_cloud.Available) { r.Outcome = SyncOutcome.Unavailable; onDone?.Invoke(r); return; }
            if (Pending != null) { onDone?.Invoke(Pending); return; }
            if (!Due(reason, r.At)) { r.Outcome = SyncOutcome.Skipped; onDone?.Invoke(r); return; }
            if (InFlight) { r.Outcome = SyncOutcome.Skipped; onDone?.Invoke(r); return; }
            LastAttemptAt = r.At;
            InFlight = true;
            Action<SyncReport> finish = report => { InFlight = false; onDone?.Invoke(report); };
            _cloud.Pull(text =>
            {
                if (text == null) { Push(localSave, r, finish); return; }
                EnvelopeStatus status = CloudSave.Open(text, out CloudEnvelope env);
                if (status == EnvelopeStatus.Corrupt)
                {
                    r.RemoteWasCorrupt = true;
                    Push(localSave, r, finish);
                    return;
                }
                if (status == EnvelopeStatus.NewerVersion)
                {
                    r.Outcome = SyncOutcome.NewerVersion;
                    Last = r;
                    finish?.Invoke(r);
                    return;
                }
                r.Remote = env.Summary;
                r.RemoteSave = env.Save;
                switch (SaveSync.Choose(local, env.Summary))
                {
                    case SaveChoice.Same:
                        if (!string.Equals(localSave, env.Save, StringComparison.Ordinal)) { Push(localSave, r, finish); break; }
                        r.Outcome = SyncOutcome.Same;
                        if (LastPushedAt < 0) LastPushedAt = env.SavedAt;
                        Last = r;
                        finish?.Invoke(r);
                        break;
                    case SaveChoice.KeepLocal:
                        Push(localSave, r, finish);
                        break;
                    default:
                        r.Outcome = SyncOutcome.NeedsChoice;
                        r.SuggestRemote = Suggest(local, env.Summary);
                        Pending = r;
                        Last = r;
                        finish?.Invoke(r);
                        break;
                }
            });
        }

        public static bool Suggest(SaveSummary local, SaveSummary remote)
        {
            if (remote.GardenLevel != local.GardenLevel) return remote.GardenLevel > local.GardenLevel;
            if (remote.Rabbits != local.Rabbits) return remote.Rabbits > local.Rabbits;
            return remote.LastSeenUnixSeconds > local.LastSeenUnixSeconds;
        }

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

        public void PushNow(string localSave, SaveSummary local, Action<SyncReport> onDone)
        {
            var r = new SyncReport { Local = local, At = _now() };
            if (!_cloud.Available) { r.Outcome = SyncOutcome.Unavailable; onDone?.Invoke(r); return; }
            Pending = null;
            LastAttemptAt = r.At;
            Push(localSave, r, onDone);
        }

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

    public struct CloudStatus
    {
        public bool Available;
        public bool SignedOut;
        public long LastPushedAt;
        public SyncReport Last;
        public bool HasLocalBackup;
        public long LocalBackupAt;
    }

    public sealed class MemoryCloudSave : ICloudSave
    {
        public bool Online = true;
        public string Text;
        public int Pushes, Pulls;
        public int MaxBytes;

        public bool Available => true;

        int ICloudSave.MaxBytes => MaxBytes;

        public bool Hold;
        readonly System.Collections.Generic.Queue<Action> _held = new System.Collections.Generic.Queue<Action>();

        public void Release()
        {
            while (_held.Count > 0) _held.Dequeue()();
        }

        void Answer(Action deliver)
        {
            if (Hold) _held.Enqueue(deliver); else deliver();
        }

        public void Push(string json, Action<bool> onDone)
        {
            Pushes++;
            bool ok = Online && CloudSave.Fits(json, MaxBytes);
            if (ok) Text = json;
            Answer(() => onDone?.Invoke(ok));
        }

        public void Pull(Action<string> onDone)
        {
            Pulls++;
            string text = Online ? Text : null;
            Answer(() => onDone?.Invoke(text));
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

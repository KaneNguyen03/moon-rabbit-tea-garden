using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    /// <summary>Reads and writes the save file. Never destroys a file it could not understand.</summary>
    public sealed class SaveStore
    {
        readonly string _dir;
        bool _readOnly;

        public SaveStore(string directory = null)
        {
            _dir = directory ?? Application.persistentDataPath;
        }

        string MainPath => Path.Combine(_dir, "save.json");
        string TempPath => Path.Combine(_dir, "save.tmp");
        string BadPath => Path.Combine(_dir, "save.bad.json");
        string BackupPath => Path.Combine(_dir, "save.backup.json");

        /// <summary>The save text as it is on disk, or null.</summary>
        public string ReadRaw()
        {
            try { return File.Exists(MainPath) ? File.ReadAllText(MainPath) : null; }
            catch (Exception e) { Debug.LogWarning("Không đọc được file lưu: " + e.Message); return null; }
        }

        public bool HasBackup => File.Exists(BackupPath);

        /// <summary>When the backup was written, in Unix seconds; -1 without one.</summary>
        public long BackupUnixSeconds => HasBackup ? new DateTimeOffset(File.GetLastWriteTimeUtc(BackupPath)).ToUnixTimeSeconds() : -1;

        public string ReadBackup()
        {
            try { return HasBackup ? File.ReadAllText(BackupPath) : null; }
            catch (Exception e) { Debug.LogWarning("Không đọc được bản sao lưu: " + e.Message); return null; }
        }

        /// <summary>Keeps a copy of the save on disk as the backup, before anything replaces it. False when there is nothing to keep.</summary>
        public bool Backup()
        {
            string current = ReadRaw();
            return current != null && WriteBackup(current);
        }

        /// <summary>Removes every file this store wrote: the save, its temp, the backup and any kept corrupt copy (#42). True when nothing is left.</summary>
        public bool DeleteAll()
        {
            bool ok = true;
            foreach (string path in new[] { MainPath, TempPath, BackupPath, BadPath })
            {
                try { if (File.Exists(path)) File.Delete(path); }
                catch (Exception e) { Debug.LogWarning("Không xoá được " + path + ": " + e.Message); ok = false; }
            }
            return ok;
        }

        public bool WriteBackup(string json)
        {
            try
            {
                Directory.CreateDirectory(_dir);
                File.WriteAllText(BackupPath, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Không ghi được bản sao lưu: " + e.Message);
                return false;
            }
        }

        /// <summary>True when the file on disk came from a newer build, so saving is off.</summary>
        public bool ReadOnly => _readOnly;

        public LoadResult Load(IReadOnlyList<StationDef> defs, IReadOnlyList<UpgradeDef> upgrades = null)
        {
            string json = null;
            try
            {
                if (File.Exists(MainPath)) json = File.ReadAllText(MainPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Không đọc được file lưu: " + e.Message);
                _readOnly = true; // do not overwrite what we could not read
                return new LoadResult { Status = LoadStatus.Corrupt, State = GameState.NewGame(defs, upgrades == null ? 0 : upgrades.Count) };
            }

            LoadResult result = SaveCodec.Decode(defs, json, upgrades);
            if (result.Status == LoadStatus.Corrupt)
            {
                try { File.Copy(MainPath, BadPath, true); }
                catch (Exception e) { Debug.LogWarning("Không giữ lại được file lưu hỏng: " + e.Message); }
            }
            else if (result.Status == LoadStatus.NewerVersion)
            {
                _readOnly = true;
            }
            return result;
        }

        public bool Save(IReadOnlyList<StationDef> defs, GameState state, IReadOnlyList<UpgradeDef> upgrades = null)
        {
            if (_readOnly) return false;
            try
            {
                Directory.CreateDirectory(_dir);
                File.WriteAllText(TempPath, SaveCodec.Encode(defs, state, upgrades));
                if (File.Exists(MainPath)) File.Delete(MainPath);
                File.Move(TempPath, MainPath);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Không lưu được game: " + e.Message);
                return false;
            }
        }
    }
}

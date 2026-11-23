using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed class SaveStore
    {
        readonly string _dir;
        bool _readOnly;

        public static Func<string> DefaultDirectory = () => Application.persistentDataPath;

        public string Folder => _dir;

        public SaveStore(string directory = null)
        {
            _dir = string.IsNullOrWhiteSpace(directory) ? DefaultDirectory() : directory;
        }

        string MainPath => Path.Combine(_dir, "save.json");
        string TempPath => Path.Combine(_dir, "save.tmp");
        string BadPath => Path.Combine(_dir, "save.bad.json");
        string BackupPath => Path.Combine(_dir, "save.backup.json");

        public string ReadRaw()
        {
            try { return File.Exists(MainPath) ? File.ReadAllText(MainPath) : null; }
            catch (Exception e) { Debug.LogWarning("Không đọc được file lưu: " + e.Message); return null; }
        }

        public bool HasBackup => File.Exists(BackupPath);

        public long BackupUnixSeconds => HasBackup ? new DateTimeOffset(File.GetLastWriteTimeUtc(BackupPath)).ToUnixTimeSeconds() : -1;

        public string ReadBackup()
        {
            try { return HasBackup ? File.ReadAllText(BackupPath) : null; }
            catch (Exception e) { Debug.LogWarning("Không đọc được bản sao lưu: " + e.Message); return null; }
        }

        public bool Backup()
        {
            string current = ReadRaw();
            return current != null && WriteBackup(current);
        }

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
                _readOnly = true;
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

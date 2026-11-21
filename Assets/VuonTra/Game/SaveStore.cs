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

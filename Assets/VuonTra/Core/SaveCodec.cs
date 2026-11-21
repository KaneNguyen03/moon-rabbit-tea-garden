using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VuonTra.Core
{
    public enum LoadStatus
    {
        Ok,
        /// <summary>Nothing saved yet.</summary>
        Empty,
        /// <summary>The text is not a save this game wrote.</summary>
        Corrupt,
        /// <summary>Written by a newer build; must not be overwritten.</summary>
        NewerVersion
    }

    public struct LoadResult
    {
        public LoadStatus Status;
        /// <summary>The loaded state, or a new game for every status except Ok.</summary>
        public GameState State;
    }

    /// <summary>
    /// GameState to and from JSON text. Stations are stored by id, so adding or reordering
    /// stations in a later build keeps the player's progress.
    /// </summary>
    public static class SaveCodec
    {
        /// <summary>1 = stations only. 2 adds upgrades, quest counters, daily state and clovers. 3 adds rabbits. 4 adds plots, rabbit levels, items and energy. 5 adds rabbit kinds. 6 adds the album of kinds seen. 7 adds places to rest.</summary>
        public const int CurrentVersion = 7;

        public static string Encode(IReadOnlyList<StationDef> defs, GameState s, IReadOnlyList<UpgradeDef> upgrades = null)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"version\":").Append(CurrentVersion);
            sb.Append(",\"coinsMantissa\":").Append(s.Coins.Mantissa.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"coinsExponent\":").Append(s.Coins.Exponent.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"stations\":[");
            for (int i = 0; i < defs.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, defs[i].Id);
                sb.Append(",\"level\":").Append(s.Levels[i].ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"progress\":").Append(Sane(s.Progress[i]).ToString("R", CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            sb.Append("],\"buyMode\":").Append(s.BuyMode.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"lastSeen\":").Append(s.LastSeenUnixSeconds.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"hintSeen\":").Append(s.HintSeen ? "true" : "false");

            sb.Append(",\"upgrades\":[");
            int upgradeCount = upgrades == null ? 0 : Math.Min(upgrades.Count, s.UpgradeLevels.Length);
            for (int i = 0; i < upgradeCount; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":");
                WriteString(sb, upgrades[i].Id);
                sb.Append(",\"level\":").Append(s.UpgradeLevels[i].ToString(CultureInfo.InvariantCulture)).Append('}');
            }
            sb.Append("],\"clovers\":").Append(s.Clovers.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"taps\":").Append(s.TotalTaps.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"levelsBought\":").Append(s.LevelsBought.ToString(CultureInfo.InvariantCulture));
            WriteBig(sb, "earned", s.TotalEarned);
            sb.Append(",\"dailyDay\":").Append(s.DailyDay.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyTaps\":").Append(s.DailyTapsBase.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"dailyLevels\":").Append(s.DailyLevelsBase.ToString(CultureInfo.InvariantCulture));
            WriteBig(sb, "dailyEarned", s.DailyEarnedBase);
            WriteBig(sb, "dailyTarget", s.DailyCarrotTarget);
            sb.Append(",\"claimed\":[");
            bool first = true;
            foreach (string id in s.Claimed)
            {
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, id);
            }
            sb.Append("],\"restSpots\":").Append(s.RestSpots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"seenKinds\":[");
            first = true;
            foreach (string id in s.SeenKinds)
            {
                if (!first) sb.Append(',');
                first = false;
                WriteString(sb, id);
            }
            sb.Append("],\"nextRabbitId\":").Append(s.NextRabbitId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"rabbits\":[");
            for (int i = 0; i < s.Rabbits.Count; i++)
            {
                Rabbit r = s.Rabbits[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(r.Id.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"tier\":").Append(r.Tier.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"level\":").Append(r.Level.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"kind\":");
                WriteString(sb, Rabbits.KindOf(r));
                sb.Append(",\"name\":");
                WriteString(sb, r.Name);
                // Stations by id, like everywhere else in the save, so reordering them is safe.
                sb.Append(",\"station\":");
                WriteString(sb, r.Station >= 0 && r.Station < defs.Count ? defs[r.Station].Id : "");
                sb.Append('}');
            }
            sb.Append("],\"plots\":").Append(s.Plots.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"energy\":").Append(Sane(s.Energy).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"boost\":").Append(Sane(s.BoostSecondsLeft).ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"nextItemId\":").Append(s.NextItemId.ToString(CultureInfo.InvariantCulture));
            sb.Append(",\"items\":[");
            for (int i = 0; i < s.Items.Count; i++)
            {
                Item item = s.Items[i];
                if (i > 0) sb.Append(',');
                sb.Append("{\"id\":").Append(item.Id.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"def\":");
                WriteString(sb, item.DefId);
                sb.Append(",\"name\":");
                WriteString(sb, item.Name);
                sb.Append(",\"slot\":").Append(((int)item.Slot).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"effect\":").Append(((int)item.Effect).ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"tier\":").Append(item.Tier.ToString(CultureInfo.InvariantCulture));
                sb.Append(",\"amount\":").Append(Sane(item.Amount).ToString("R", CultureInfo.InvariantCulture));
                sb.Append(",\"wornBy\":").Append(item.WornBy.ToString(CultureInfo.InvariantCulture));
                sb.Append('}');
            }
            sb.Append("]}");
            return sb.ToString();
        }

        public static LoadResult Decode(IReadOnlyList<StationDef> defs, string json, IReadOnlyList<UpgradeDef> upgrades = null)
        {
            var result = new LoadResult { State = GameState.NewGame(defs, upgrades == null ? 0 : upgrades.Count) };
            if (string.IsNullOrWhiteSpace(json))
            {
                result.Status = LoadStatus.Empty;
                return result;
            }

            Dictionary<string, object> root;
            try
            {
                root = new Reader(json).ReadDocument() as Dictionary<string, object>;
            }
            catch (FormatException)
            {
                root = null;
            }

            if (root == null || !TryNumber(root, "version", out double version) || version < 1d)
            {
                result.Status = LoadStatus.Corrupt;
                return result;
            }
            if (version > CurrentVersion)
            {
                result.Status = LoadStatus.NewerVersion;
                return result;
            }

            var s = result.State;
            TryNumber(root, "coinsMantissa", out double mantissa);
            TryNumber(root, "coinsExponent", out double exponent);
            s.Coins = BigNum.FromParts(mantissa, ToLong(exponent));

            if (root.TryGetValue("stations", out object stationsObj) && stationsObj is List<object> stations)
            {
                foreach (object item in stations)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    if (!(entry.TryGetValue("id", out object idObj) && idObj is string id)) continue;
                    int index = IndexOf(defs, id);
                    if (index < 0) continue; // a station this build no longer has

                    TryNumber(entry, "level", out double level);
                    TryNumber(entry, "progress", out double progress);
                    s.Levels[index] = level > 0d ? (int)Math.Min(level, int.MaxValue) : 0;
                    s.Progress[index] = (progress >= 0d && progress < 1d) ? progress : 0d;
                }
            }
            if (s.Levels.Length > 0 && s.Levels[0] < 1) s.Levels[0] = 1;

            s.BuyMode = 1;
            if (TryNumber(root, "buyMode", out double buyMode))
            {
                if (buyMode == 0d) s.BuyMode = 0;
                else if (buyMode == 10d) s.BuyMode = 10;
            }
            TryNumber(root, "lastSeen", out double lastSeen);
            s.LastSeenUnixSeconds = ToLong(lastSeen);
            s.HintSeen = root.TryGetValue("hintSeen", out object hint) && hint is bool b && b;

            // Everything below arrived in version 2; a version 1 save simply has none of it.
            if (upgrades != null && root.TryGetValue("upgrades", out object upgradesObj) && upgradesObj is List<object> savedUpgrades)
            {
                foreach (object item in savedUpgrades)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    if (!(entry.TryGetValue("id", out object idObj) && idObj is string id)) continue;
                    for (int i = 0; i < upgrades.Count; i++)
                    {
                        if (upgrades[i].Id != id) continue;
                        TryNumber(entry, "level", out double level);
                        s.UpgradeLevels[i] = (int)Math.Max(0d, Math.Min(level, upgrades[i].MaxLevel));
                    }
                }
            }
            s.Clovers = (int)Math.Max(0d, Math.Min(ReadNumber(root, "clovers"), int.MaxValue));
            s.TotalTaps = Math.Max(0L, ToLong(ReadNumber(root, "taps")));
            s.LevelsBought = Math.Max(0L, ToLong(ReadNumber(root, "levelsBought")));
            s.TotalEarned = ReadBig(root, "earned");
            s.DailyDay = ToLong(ReadNumber(root, "dailyDay"));
            s.DailyTapsBase = Math.Max(0L, ToLong(ReadNumber(root, "dailyTaps")));
            s.DailyLevelsBase = Math.Max(0L, ToLong(ReadNumber(root, "dailyLevels")));
            s.DailyEarnedBase = ReadBig(root, "dailyEarned");
            s.DailyCarrotTarget = ReadBig(root, "dailyTarget");
            if (root.TryGetValue("claimed", out object claimedObj) && claimedObj is List<object> claimed)
            {
                foreach (object item in claimed)
                    if (item is string claimedId) s.Claimed.Add(claimedId);
            }

            // Version 3: the rabbit collection.
            if (root.TryGetValue("rabbits", out object rabbitsObj) && rabbitsObj is List<object> rabbits)
            {
                var taken = new HashSet<int>();
                var ids = new HashSet<long>();
                foreach (object item in rabbits)
                {
                    if (!(item is Dictionary<string, object> entry)) continue;
                    long rabbitId = ToLong(ReadNumber(entry, "id"));
                    if (rabbitId <= 0 || !ids.Add(rabbitId)) continue; // no id, or a duplicate
                    var rabbit = new Rabbit
                    {
                        Id = rabbitId,
                        Tier = (int)Math.Max(0d, Math.Min(ReadNumber(entry, "tier"), Rabbits.TierCount - 1)),
                        Level = 1,
                        Name = entry.TryGetValue("name", out object nameObj) && nameObj is string name ? name : "",
                    };
                    if (entry.TryGetValue("station", out object stationObj) && stationObj is string stationId && stationId.Length > 0)
                    {
                        int index = IndexOf(defs, stationId);
                        // One rabbit per station, and only at stations that are open.
                        if (index >= 0 && s.Levels[index] > 0 && taken.Add(index)) rabbit.Station = index;
                    }
                    rabbit.Level = (int)Math.Max(1d, Math.Min(ReadNumber(entry, "level"), Rabbits.LevelCaps[rabbit.Tier]));
                    // Version 5: the kind. Older saves have none, and a kind must belong to the rabbit's tier.
                    rabbit.Kind = entry.TryGetValue("kind", out object kindObj) && kindObj is string kindId ? kindId : null;
                    rabbit.Kind = Rabbits.KindOf(rabbit);
                    s.Rabbits.Add(rabbit);
                    if (rabbitId > s.NextRabbitId) s.NextRabbitId = rabbitId;
                }
            }
            // Version 7: places to rest. Older saves start with the usual two.
            s.RestSpots = root.ContainsKey("restSpots")
                ? (int)Math.Max(RestSpots.Starting, Math.Min(ReadNumber(root, "restSpots"), RestSpots.Max))
                : RestSpots.Starting;

            // Version 6: the album. Whatever the save says, every rabbit still here has certainly been seen.
            if (root.TryGetValue("seenKinds", out object seenObj) && seenObj is List<object> seen)
                foreach (object item in seen)
                    if (item is string seenId) Rabbits.See(s, seenId);
            foreach (Rabbit r in s.Rabbits) Rabbits.See(s, Rabbits.KindOf(r));

            long savedNext = ToLong(ReadNumber(root, "nextRabbitId"));
            if (savedNext > s.NextRabbitId) s.NextRabbitId = savedNext;

            // Version 4. A save from before plots existed keeps every station it had already opened.
            int highestOpen = 0;
            for (int i = 0; i < s.Levels.Length; i++)
                if (s.Levels[i] > 0) highestOpen = i + 1;
            int plots = TryNumber(root, "plots", out double savedPlots) ? (int)Math.Min(savedPlots, defs.Count) : 0;
            s.Plots = Math.Min(defs.Count, Math.Max(Math.Max(plots, highestOpen), Math.Min(defs.Count, Plots.Starting)));

            s.Energy = TryNumber(root, "energy", out double energy) ? Math.Max(0d, Math.Min(energy, Energy.Max)) : Energy.Max;
            s.BoostSecondsLeft = Math.Max(0d, Math.Min(ReadNumber(root, "boost"), Energy.BoostSeconds));

            if (root.TryGetValue("items", out object itemsObj) && itemsObj is List<object> items)
            {
                var itemIds = new HashSet<long>();
                var slots = new HashSet<string>();
                foreach (object raw in items)
                {
                    if (!(raw is Dictionary<string, object> entry)) continue;
                    long itemId = ToLong(ReadNumber(entry, "id"));
                    if (itemId <= 0 || !itemIds.Add(itemId)) continue;
                    double slot = ReadNumber(entry, "slot"), effect = ReadNumber(entry, "effect");
                    var item = new Item
                    {
                        Id = itemId,
                        DefId = entry.TryGetValue("def", out object defObj) && defObj is string defId ? defId : "",
                        Name = entry.TryGetValue("name", out object itemNameObj) && itemNameObj is string itemName ? itemName : "",
                        Slot = slot == 1d ? ItemSlot.Charm : ItemSlot.Hat,
                        Effect = effect == 1d ? ItemEffect.GardenSpeed : (effect == 2d ? ItemEffect.AwayIncome : ItemEffect.StationIncome),
                        Tier = (int)Math.Max(0d, Math.Min(ReadNumber(entry, "tier"), Rabbits.TierCount - 1)),
                        Amount = Math.Max(0d, Math.Min(ReadNumber(entry, "amount"), 1000d)),
                    };
                    long wornBy = ToLong(ReadNumber(entry, "wornBy"));
                    // Worn only by a rabbit that exists, one item per slot.
                    if (wornBy > 0 && Rabbits.Find(s, wornBy) != null && slots.Add(wornBy + ":" + (int)item.Slot)) item.WornBy = wornBy;
                    s.Items.Add(item);
                    if (itemId > s.NextItemId) s.NextItemId = itemId;
                }
            }
            long savedNextItem = ToLong(ReadNumber(root, "nextItemId"));
            if (savedNextItem > s.NextItemId) s.NextItemId = savedNextItem;

            result.Status = LoadStatus.Ok;
            return result;
        }

        static int IndexOf(IReadOnlyList<StationDef> defs, string id)
        {
            for (int i = 0; i < defs.Count; i++)
                if (defs[i].Id == id) return i;
            return -1;
        }

        static bool TryNumber(Dictionary<string, object> obj, string key, out double value)
        {
            if (obj.TryGetValue(key, out object raw) && raw is double d && !double.IsNaN(d) && !double.IsInfinity(d))
            {
                value = d;
                return true;
            }
            value = 0d;
            return false;
        }

        static double ReadNumber(Dictionary<string, object> obj, string key)
        {
            TryNumber(obj, key, out double value);
            return value;
        }

        static BigNum ReadBig(Dictionary<string, object> obj, string key)
        {
            return BigNum.FromParts(ReadNumber(obj, key + "Mantissa"), ToLong(ReadNumber(obj, key + "Exponent")));
        }

        static void WriteBig(StringBuilder sb, string key, BigNum value)
        {
            sb.Append(",\"").Append(key).Append("Mantissa\":").Append(value.Mantissa.ToString("R", CultureInfo.InvariantCulture));
            sb.Append(",\"").Append(key).Append("Exponent\":").Append(value.Exponent.ToString(CultureInfo.InvariantCulture));
        }

        static long ToLong(double v)
        {
            if (double.IsNaN(v)) return 0L;
            if (v >= 9.2e18) return long.MaxValue;
            if (v <= -9.2e18) return long.MinValue;
            return (long)v;
        }

        static double Sane(double v) => (double.IsNaN(v) || double.IsInfinity(v)) ? 0d : v;

        static void WriteString(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (char c in value ?? string.Empty)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        /// <summary>
        /// A small strict JSON reader. Core cannot use Unity's JsonUtility, and the save has to
        /// tolerate unknown fields. Objects become Dictionary, arrays List, numbers double.
        /// Anything malformed throws FormatException.
        /// </summary>
        sealed class Reader
        {
            const int MaxDepth = 32;
            readonly string _text;
            int _pos;

            public Reader(string text) { _text = text; }

            public object ReadDocument()
            {
                object value = ReadValue(0);
                SkipSpace();
                if (_pos != _text.Length) throw new FormatException("Trailing text");
                return value;
            }

            object ReadValue(int depth)
            {
                if (depth > MaxDepth) throw new FormatException("Too deep");
                SkipSpace();
                if (_pos >= _text.Length) throw new FormatException("Unexpected end");
                char c = _text[_pos];
                if (c == '{') return ReadObject(depth);
                if (c == '[') return ReadArray(depth);
                if (c == '"') return ReadString();
                if (c == 't') { Expect("true"); return true; }
                if (c == 'f') { Expect("false"); return false; }
                if (c == 'n') { Expect("null"); return null; }
                return ReadNumber();
            }

            Dictionary<string, object> ReadObject(int depth)
            {
                var obj = new Dictionary<string, object>();
                _pos++; // {
                SkipSpace();
                if (Peek() == '}') { _pos++; return obj; }
                while (true)
                {
                    SkipSpace();
                    if (Peek() != '"') throw new FormatException("Expected key");
                    string key = ReadString();
                    SkipSpace();
                    if (Peek() != ':') throw new FormatException("Expected ':'");
                    _pos++;
                    obj[key] = ReadValue(depth + 1);
                    SkipSpace();
                    char c = Peek();
                    _pos++;
                    if (c == '}') return obj;
                    if (c != ',') throw new FormatException("Expected ',' or '}'");
                }
            }

            List<object> ReadArray(int depth)
            {
                var list = new List<object>();
                _pos++; // [
                SkipSpace();
                if (Peek() == ']') { _pos++; return list; }
                while (true)
                {
                    list.Add(ReadValue(depth + 1));
                    SkipSpace();
                    char c = Peek();
                    _pos++;
                    if (c == ']') return list;
                    if (c != ',') throw new FormatException("Expected ',' or ']'");
                }
            }

            string ReadString()
            {
                var sb = new StringBuilder();
                _pos++; // opening quote
                while (true)
                {
                    if (_pos >= _text.Length) throw new FormatException("Unterminated string");
                    char c = _text[_pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }

                    if (_pos >= _text.Length) throw new FormatException("Unterminated escape");
                    char e = _text[_pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (_pos + 4 > _text.Length) throw new FormatException("Bad \\u escape");
                            if (!int.TryParse(_text.Substring(_pos, 4), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out int code))
                                throw new FormatException("Bad \\u escape");
                            sb.Append((char)code);
                            _pos += 4;
                            break;
                        default: throw new FormatException("Bad escape");
                    }
                }
            }

            double ReadNumber()
            {
                int start = _pos;
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if ((c >= '0' && c <= '9') || c == '-' || c == '+' || c == '.' || c == 'e' || c == 'E') _pos++;
                    else break;
                }
                if (_pos == start) throw new FormatException("Unexpected character");
                if (!double.TryParse(_text.Substring(start, _pos - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                    throw new FormatException("Bad number");
                return value;
            }

            void Expect(string word)
            {
                if (_pos + word.Length > _text.Length || string.CompareOrdinal(_text, _pos, word, 0, word.Length) != 0)
                    throw new FormatException("Unexpected token");
                _pos += word.Length;
            }

            char Peek()
            {
                if (_pos >= _text.Length) throw new FormatException("Unexpected end");
                return _text[_pos];
            }

            void SkipSpace()
            {
                while (_pos < _text.Length)
                {
                    char c = _text[_pos];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') _pos++;
                    else break;
                }
            }
        }
    }
}

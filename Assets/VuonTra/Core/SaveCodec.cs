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
        public const int CurrentVersion = 1;

        public static string Encode(IReadOnlyList<StationDef> defs, GameState s)
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
            sb.Append('}');
            return sb.ToString();
        }

        public static LoadResult Decode(IReadOnlyList<StationDef> defs, string json)
        {
            var result = new LoadResult { State = GameState.NewGame(defs) };
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

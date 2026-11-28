using System;
using System.Collections.Generic;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;
using VuonTra.Core;

namespace VuonTra.Game
{
    public static class L10n
    {
        public const string Table = "Game";

        public static event Action Changed;

        static bool _hooked;

        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => WhenReady(() => Changed?.Invoke());
        }

        public static bool LoadsAsync =>
#if UNITY_WEBGL && !UNITY_EDITOR
            true;
#else
            false;
#endif

        public static bool Ready => !LoadsAsync || (LocalizationSettings.InitializationOperation.IsDone && SelectedTable.IsDone);

        static AsyncOperationHandle<StringTable> SelectedTable => LocalizationSettings.StringDatabase.GetTableAsync(Table);

        public static void WhenReady(Action ready)
        {
            if (Ready)
            {
                ready();
                return;
            }
            LocalizationSettings.InitializationOperation.Completed += _ => SelectedTable.Completed += __ => ready();
        }

        public static string Code
        {
            get
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : "vi";
            }
        }

        public static string T(string key, params object[] args)
        {
            Hook();
            string text = Ready ? Find(key, args) : ShownBefore(key);
            if (text == null) return key;
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }

        static readonly Dictionary<string, string> _shownBefore = new Dictionary<string, string>();

        static string ShownBefore(string key) => _shownBefore.TryGetValue(key, out string text) ? text : null;

        static string Find(string key, object[] args)
        {
            string text = null;
            if (args != null && args.Length > 0 && Code.StartsWith("en", StringComparison.Ordinal) && FirstCountIsOne(args))
                text = EntryOrNull(key + OneSuffix);
            if (text == null) text = LookUp(key) ?? LookUpAfterLoadingTable(key);
            if (LoadsAsync && text != null) _shownBefore[key] = text;
            return text;
        }

        static string LookUp(string key) => LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);

        static string LookUpAfterLoadingTable(string key)
        {
            LoadSelectedTable();
            return LookUp(key);
        }

        static void LoadSelectedTable()
        {
            if (!LoadsAsync) LocalizationSettings.StringDatabase.GetTable(Table);
        }

        public const string OneSuffix = ".one";

        public static bool FirstCountIsOne(object[] args)
        {
            foreach (object arg in args)
            {
                switch (arg)
                {
                    case int v: return v == 1;
                    case long v: return v == 1;
                    case short v: return v == 1;
                    case byte v: return v == 1;
                    case uint v: return v == 1;
                    case ulong v: return v == 1;
                    case float v: return v == 1f;
                    case double v: return v == 1d;
                    case decimal v: return v == 1m;
                }
            }
            return false;
        }

        static string EntryOrNull(string key)
        {
            StringTable table = LocalizationSettings.StringDatabase.GetTable(Table);
            StringTableEntry entry = table != null ? table.GetEntry(key) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : null;
        }

        public static bool SetLocale(string code)
        {
            Hook();
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null) return false;
            LocalizationSettings.SelectedLocale = locale;
            LoadSelectedTable();
            return true;
        }

        public static string Number(string formatted)
        {
            return Code.StartsWith("en", StringComparison.Ordinal) ? formatted.Replace(',', '.') : formatted;
        }

        public static string Short(this BigNum value) => Number(value.ToShortString());
        public static string Full(this BigNum value) => value.ToFullString(Code.StartsWith("en", StringComparison.Ordinal) ? ',' : '.');

        public static string Station(string id) => T("station." + id);

        public static string Dish(string stationId, int dish) => T("dish." + stationId + "." + (dish + 1));

        public static string Season(VuonTra.Core.Season season)
        {
            switch (season)
            {
                case VuonTra.Core.Season.MidAutumn: return T("season.midautumn");
                case VuonTra.Core.Season.Tet: return T("season.tet");
                case VuonTra.Core.Season.Christmas: return T("season.christmas");
                default: return "";
            }
        }

        public static string StationLevel(int level) => T("station.level", level) + RankMarks(level);

        public static string RankMarks(int level)
        {
            int rank = Economy.Rank(level);
            return rank > 0 ? " " + new string('✦', rank) : "";
        }
        public static string Tier(int tier) => T("tier." + tier);
        public static string Rarity(int tier) => Tier(tier) + " " + VuonTra.Core.Rarity.Marks(tier);
        public static string Trade(RabbitTrade trade) => T("trade." + trade);
        public static string Temper(Personality personality) => T("temper." + personality);
        public static string Kind(Rabbit rabbit) => T("kind." + Rabbits.KindOf(rabbit));
        public static string LevelStars(Rabbit rabbit)
        {
            int n = Rabbits.Stars(rabbit);
            return n == 0 ? "" : " " + new string('☆', n);
        }
        public static string Hearts(Rabbit rabbit)
        {
            int n = Rabbits.HeartsOf(rabbit);
            if (n == 0) return "";
            return n <= Rabbits.GiftHearts ? " " + new string('♥', n) : " ♥" + n;
        }
        public static string ItemName(Item item) => T("item." + item.DefId);

        public static string ExpandSign(System.Collections.Generic.IReadOnlyList<StationDef> defs, GameState s)
        {
            MistSegment next = Expansion.Next(defs, s);
            switch (Expansion.Missing(defs, s))
            {
                case ExpandNeed.Level: return T("expand.sign.level", next.NeedLevel);
                case ExpandNeed.Rabbits: return T("expand.sign.rabbits", next.NeedRabbits - s.Rabbits.Count);
                case ExpandNeed.Kinds: return T("expand.sign.kinds", next.NeedKinds - s.SeenKinds.Count);
                case ExpandNeed.Days: return T("expand.sign.days", Expansion.DaysLeft(next, s));
                case ExpandNeed.Carrots: return T("expand.sign.carrots", (Expansion.CostOfNext(defs, s) - s.Coins).Short());
                default: return T("garden.expand");
            }
        }
    }
}

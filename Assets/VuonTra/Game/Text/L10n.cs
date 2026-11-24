using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using VuonTra.Core;

namespace VuonTra.Game
{
    /// <summary>
    /// The one place game code asks for player-facing text. It reads the "Game" string table of
    /// Unity's Localization package; the package owns which language is selected.
    /// </summary>
    public static class L10n
    {
        public const string Table = "Game";

        /// <summary>Raised after the selected language changes.</summary>
        public static event Action Changed;

        static bool _hooked;

        static void Hook()
        {
            if (_hooked) return;
            _hooked = true;
            LocalizationSettings.SelectedLocaleChanged += _ => Changed?.Invoke();
        }

        /// <summary>"vi", "en", … of the language in use.</summary>
        public static string Code
        {
            get
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                return locale != null ? locale.Identifier.Code : "vi";
            }
        }

        /// <summary>
        /// The text for a key, with {0}-style arguments filled in. In English a count of one reads
        /// "key.one" when the table has it ("+1 golden carrot"); Vietnamese has no plural and never asks.
        /// </summary>
        public static string T(string key, params object[] args)
        {
            Hook();
            string text = null;
            if (args != null && args.Length > 0 && Code.StartsWith("en", StringComparison.Ordinal) && FirstCountIsOne(args))
                text = Entry(key + OneSuffix);
            if (text == null) text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            if (text == null)
            {
                // The table was not in memory yet: load it now and ask again.
                LocalizationSettings.StringDatabase.GetTable(Table);
                text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            }
            if (text == null) return key; // a missing key shows as its name rather than crashing
            return args == null || args.Length == 0 ? text : string.Format(text, args);
        }

        /// <summary>The ending of the English line used when the count is one.</summary>
        public const string OneSuffix = ".one";

        /// <summary>True when the first numeric argument is exactly 1; text arguments (names, "1,5K") are passed over.</summary>
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

        /// <summary>The selected language's line for a key, or null when the table has none (no "missing" message).</summary>
        static string Entry(string key)
        {
            StringTable table = LocalizationSettings.StringDatabase.GetTable(Table);
            StringTableEntry entry = table != null ? table.GetEntry(key) : null;
            return entry != null && !string.IsNullOrEmpty(entry.Value) ? entry.Value : null;
        }

        /// <summary>Switches language through the package, which then notifies everything bound to it.</summary>
        public static bool SetLocale(string code)
        {
            Hook();
            Locale locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(code));
            if (locale == null) return false;
            LocalizationSettings.SelectedLocale = locale;
            LocalizationSettings.StringDatabase.GetTable(Table); // have the new language ready before anything asks
            return true;
        }

        /// <summary>Numbers are formatted with a decimal comma; English wants a point.</summary>
        public static string Number(string formatted)
        {
            return Code.StartsWith("en", StringComparison.Ordinal) ? formatted.Replace(',', '.') : formatted;
        }

        public static string Short(this BigNum value) => Number(value.ToShortString());
        /// <summary>Every digit with the language's own group mark: "13.912.345" in Vietnamese, "13,912,345" in English.</summary>
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

        /// <summary>"Cấp 120 ✦✦": the level, then one mark per rank reached (25, 100, 300), the same on the plate, the card and the row.</summary>
        public static string StationLevel(int level) => T("station.level", level) + RankMarks(level);

        public static string RankMarks(int level)
        {
            int rank = Economy.Rank(level);
            return rank > 0 ? " " + new string('✦', rank) : "";
        }
        public static string Tier(int tier) => T("tier." + tier);
        /// <summary>The tier's name followed by its stars, "Vàng ★★★★★": rarity that reads without colour.</summary>
        public static string Rarity(int tier) => Tier(tier) + " " + VuonTra.Core.Rarity.Marks(tier);
        public static string Trade(RabbitTrade trade) => T("trade." + trade);
        public static string Kind(Rabbit rabbit) => T("kind." + Rabbits.KindOf(rabbit));
        /// <summary>The rabbit's level stars (hollow, one per ten levels) with a leading space, or nothing while it has none.</summary>
        public static string LevelStars(Rabbit rabbit)
        {
            int n = Rabbits.Stars(rabbit);
            return n == 0 ? "" : " " + new string('☆', n);
        }
        /// <summary>The rabbit's hearts (full ones only) with a leading space, or nothing while it has none; past five, one heart and the count (#58).</summary>
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

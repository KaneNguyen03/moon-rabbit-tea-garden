using System;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
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

        /// <summary>The text for a key, with {0}-style arguments filled in.</summary>
        public static string T(string key, params object[] args)
        {
            Hook();
            string text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            if (text == null)
            {
                // The table was not in memory yet: load it now and ask again.
                LocalizationSettings.StringDatabase.GetTable(Table);
                text = LocalizationSettings.StringDatabase.GetLocalizedString(Table, key);
            }
            if (text == null) return key; // a missing key shows as its name rather than crashing
            return args == null || args.Length == 0 ? text : string.Format(text, args);
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
        /// <summary>The rabbit's hearts (full ones only) with a leading space, or nothing while it has none.</summary>
        public static string Hearts(Rabbit rabbit)
        {
            int n = Math.Max(0, Math.Min(Rabbits.AffectionMax, rabbit.Affection));
            return n == 0 ? "" : " " + new string('♥', n);
        }
        public static string ItemName(Item item) => T("item." + item.DefId);
    }
}

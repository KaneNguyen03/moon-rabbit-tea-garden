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

        public static string Station(string id) => T("station." + id);
        public static string Tier(int tier) => T("tier." + tier);
        public static string Trade(RabbitTrade trade) => T("trade." + trade);
        public static string Kind(Rabbit rabbit) => T("kind." + Rabbits.KindOf(rabbit));
        public static string ItemName(Item item) => T("item." + item.DefId);
    }
}

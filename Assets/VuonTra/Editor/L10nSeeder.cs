using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Sets up Unity Localization for the game (settings, Vietnamese and English locales, the
    /// "Game" string table) and fills the table from L10nStrings. Safe to run again at any time.
    /// </summary>
    public static class L10nSeeder
    {
        const string Root = "Assets/Localization";
        const string TableName = "Game";

        [MenuItem("VuonTra/Seed Localization")]
        public static void Seed()
        {
            Directory.CreateDirectory(Root + "/Locales");
            Directory.CreateDirectory(Root + "/Tables");
            AssetDatabase.Refresh();

            LocalizationSettings settings = LocalizationEditorSettings.ActiveLocalizationSettings;
            if (settings == null)
            {
                string[] found = AssetDatabase.FindAssets("t:LocalizationSettings", new[] { "Assets" });
                if (found.Length > 0)
                {
                    settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(AssetDatabase.GUIDToAssetPath(found[0]));
                }
                else
                {
                    settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                    AssetDatabase.CreateAsset(settings, Root + "/LocalizationSettings.asset");
                }
                LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            }

            var codes = new[] { "vi", "en" };
            foreach (string code in codes)
            {
                if (LocalizationEditorSettings.GetLocale(new LocaleIdentifier(code)) != null) continue;
                Locale locale = Locale.CreateLocale(new LocaleIdentifier(code));
                AssetDatabase.CreateAsset(locale, Root + "/Locales/" + code + ".asset");
                LocalizationEditorSettings.AddLocale(locale);
            }

            // Which language the game starts in: the player's own choice if they made one, else
            // the device language, else Vietnamese.
            List<IStartupLocaleSelector> selectors = settings.GetStartupLocaleSelectors();
            selectors.Clear();
            selectors.Add(new PlayerPrefLocaleSelector { PlayerPreferenceKey = "language" });
            selectors.Add(new SystemLocaleSelector());
            selectors.Add(new SpecificLocaleSelector { LocaleId = new LocaleIdentifier("vi") });
            EditorUtility.SetDirty(settings);

            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(TableName)
                ?? LocalizationEditorSettings.CreateStringTableCollection(TableName, Root + "/Tables");

            string[,] rows = L10nStrings.All;
            for (int column = 0; column < codes.Length; column++)
            {
                var id = new LocaleIdentifier(codes[column]);
                // Match the table by locale code: the order of tables is not guaranteed.
                var table = collection.GetTable(id) as StringTable ?? collection.AddNewTable(id) as StringTable;
                for (int r = 0; r < rows.GetLength(0); r++) table.AddEntry(rows[r, 0], rows[r, column + 1]);
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                EditorUtility.SetDirty(table);
            }
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
            AssetDatabase.SaveAssets();

            Debug.Log("[VuonTra] Localization: " + Check());
        }

        /// <summary>Every key needs a non-empty value in every language, or the game shows a developer message.</summary>
        public static string Check()
        {
            var gaps = new List<string>();
            int checkedCount = 0;
            foreach (StringTableCollection col in LocalizationEditorSettings.GetStringTableCollections())
                foreach (SharedTableData.SharedTableEntry key in col.SharedData.Entries)
                    foreach (StringTable table in col.StringTables)
                    {
                        checkedCount++;
                        StringTableEntry entry = table.GetEntry(key.Id);
                        if (entry == null || string.IsNullOrWhiteSpace(entry.Value))
                            gaps.Add(col.TableCollectionName + " / " + table.LocaleIdentifier.Code + " / " + key.Key);
                    }
            if (checkedCount == 0) return "INCONCLUSIVE: no table entries found.";
            return gaps.Count == 0
                ? "COMPLETE: " + checkedCount + " entries checked, no gaps"
                : "GAPS (" + gaps.Count + " of " + checkedCount + "): " + string.Join("; ", gaps);
        }
    }
}

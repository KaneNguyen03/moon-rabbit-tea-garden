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
                var table = collection.GetTable(id) as StringTable ?? collection.AddNewTable(id) as StringTable;
                for (int r = 0; r < rows.GetLength(0); r++) table.AddEntry(rows[r, 0], rows[r, column + 1]);
                if (codes[column] == "en")
                    for (int r = 0; r < L10nStrings.EnglishOne.GetLength(0); r++) table.AddEntry(L10nStrings.EnglishOne[r, 0], L10nStrings.EnglishOne[r, 1]);
                foreach (VuonTra.Core.RabbitKind kind in VuonTra.Core.RabbitKinds.All)
                    table.AddEntry("kind." + kind.Id, codes[column] == "en" ? kind.NameEn : kind.NameVi);
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
                EditorUtility.SetDirty(table);
            }
            RemoveKeysNoLongerListed(collection);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(null, collection);
            AssetDatabase.SaveAssets();

            Debug.Log("[VuonTra] Localization: " + Check());
        }

        static void RemoveKeysNoLongerListed(StringTableCollection collection)
        {
            var listed = new HashSet<string>();
            for (int r = 0; r < L10nStrings.All.GetLength(0); r++) listed.Add(L10nStrings.All[r, 0]);
            for (int r = 0; r < L10nStrings.EnglishOne.GetLength(0); r++) listed.Add(L10nStrings.EnglishOne[r, 0]);
            foreach (VuonTra.Core.RabbitKind kind in VuonTra.Core.RabbitKinds.All) listed.Add("kind." + kind.Id);
            foreach (SharedTableData.SharedTableEntry entry in new List<SharedTableData.SharedTableEntry>(collection.SharedData.Entries))
                if (!listed.Contains(entry.Key)) collection.RemoveEntry(entry.Key);
        }

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
                        if (entry == null && key.Key.EndsWith(VuonTra.Game.L10n.OneSuffix, System.StringComparison.Ordinal) && table.LocaleIdentifier.Code != "en") continue;
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

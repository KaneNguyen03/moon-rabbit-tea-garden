using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using VuonTra.Core;
using VuonTra.Game;
using VuonTra.Game.UI;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// Creates the game's starting content from code so it can be rebuilt at any time:
    /// placeholder art, the eight stations, the catalog, the Garden scene and player settings.
    /// </summary>
    public static class ContentSeeder
    {
        public const string StationFolder = "Assets/VuonTra/Content/Stations";
        public const string CatalogPath = "Assets/VuonTra/Content/GameCatalog.asset";
        public const string ScenePath = "Assets/VuonTra/Scenes/Garden.unity";

        struct Row
        {
            public string Id, Name, Accent;
            public StationKind Kind;
            public double Cost, Growth, Income, Cycle;
            public string[] Boosts;
        }

        // Unlock order. Numbers are a first pass; they get tuned through playtests.
        static readonly Row[] Rows =
        {
            new Row { Id = "am-tra",   Name = "Ấm trà sương",       Kind = StationKind.Brew,       Cost = 10,     Growth = 1.09, Income = 1,      Cycle = 1.5, Accent = "6fb7b0" },
            new Row { Id = "gio-la",   Name = "Giỏ lá trà",         Kind = StationKind.Ingredient, Cost = 120,    Growth = 1.11, Accent = "6fcf8a", Boosts = new[] { "am-tra", "tra-sua" } },
            new Row { Id = "noi-mut",  Name = "Nồi mứt dâu",        Kind = StationKind.Brew,       Cost = 1400,   Growth = 1.12, Income = 80,     Cycle = 5,   Accent = "e0577a" },
            new Row { Id = "coi-hoa",  Name = "Cối giã hoa",        Kind = StationKind.Ingredient, Cost = 16000,  Growth = 1.13, Accent = "c58be0", Boosts = new[] { "noi-mut", "binh-sao" } },
            new Row { Id = "lo-banh",  Name = "Lò bánh hạt dẻ",     Kind = StationKind.Brew,       Cost = 190000, Growth = 1.13, Income = 6500,   Cycle = 12,  Accent = "b98a5e" },
            new Row { Id = "to-mat",   Name = "Tổ mật ong",         Kind = StationKind.Ingredient, Cost = 2.4e6,  Growth = 1.14, Accent = "f0a93a", Boosts = new[] { "lo-banh", "noi-mut" } },
            new Row { Id = "tra-sua",  Name = "Quầy trà sữa trăng", Kind = StationKind.Brew,       Cost = 3.2e7,  Growth = 1.14, Income = 600000, Cycle = 24,  Accent = "8fb4ff" },
            new Row { Id = "binh-sao", Name = "Bình ủ sao",         Kind = StationKind.Brew,       Cost = 4.5e8,  Growth = 1.15, Income = 6e6,    Cycle = 32,  Accent = "ffd98a" },
        };

        [MenuItem("VuonTra/Seed Content")]
        public static void Seed()
        {
            PlaceholderArt.Generate();
            Directory.CreateDirectory(StationFolder);
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            AssetDatabase.Refresh();

            var byId = new Dictionary<string, StationData>();
            var ordered = new List<StationData>();
            foreach (Row row in Rows)
            {
                string path = StationFolder + "/" + row.Id + ".asset";
                var data = AssetDatabase.LoadAssetAtPath<StationData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<StationData>();
                    AssetDatabase.CreateAsset(data, path);
                }
                data.Id = row.Id;
                data.DisplayName = row.Name;
                data.Kind = row.Kind;
                data.BaseCost = row.Cost;
                data.CostGrowth = row.Growth;
                data.BaseIncome = row.Income;
                data.CycleSeconds = row.Cycle;
                data.Accent = PlaceholderArt.Hex(row.Accent);
                data.Prop = PlaceholderArt.Load("prop_" + row.Id);
                EditorUtility.SetDirty(data);
                byId[row.Id] = data;
                ordered.Add(data);
            }
            foreach (Row row in Rows)
            {
                var targets = new List<StationData>();
                if (row.Boosts != null)
                    foreach (string id in row.Boosts) targets.Add(byId[id]);
                byId[row.Id].Boosts = targets.ToArray();
                EditorUtility.SetDirty(byId[row.Id]);
            }

            var catalog = AssetDatabase.LoadAssetAtPath<GameCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Stations = ordered.ToArray();
            catalog.Rabbit = PlaceholderArt.Load("rabbit");
            catalog.Scarf = PlaceholderArt.Load("scarf");
            catalog.Panel = PlaceholderArt.Load("panel");
            catalog.Round = PlaceholderArt.Load("round");
            catalog.Glow = PlaceholderArt.Load("glow");
            catalog.Background = PlaceholderArt.Load("background");
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            BuildScene(catalog);
            ApplyPlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[VuonTra] Seeded " + ordered.Count + " stations, catalog and scene.");
        }

        static void BuildScene(GameCatalog catalog)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)) { tag = "MainCamera" };
            var cam = camGo.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = UiKit.Night;
            camGo.transform.position = new Vector3(0, 0, -10);

            var game = new GameObject("Game");
            var runner = game.AddComponent<GameRunner>();
            runner.Catalog = catalog;
            var screen = game.AddComponent<GardenScreen>();
            screen.Runner = runner;
            screen.Cam = cam;

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = "VuonTraTho";
            PlayerSettings.productName = "Vườn Trà Thỏ Trăng";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
        }
    }
}

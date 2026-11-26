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
    public static class ContentSeeder
    {
        public const string StationFolder = "Assets/VuonTra/Content/Stations";
        public const string CatalogPath = "Assets/VuonTra/Content/GameCatalog.asset";
        public const string FontFolder = "Assets/VuonTra/Fonts/";
        public const string BodyFontPath = FontFolder + "Nunito-Regular.ttf";
        public const string TitleFontPath = FontFolder + "Baloo2-Regular.ttf";
        public static readonly string[] SymbolFontPaths = { FontFolder + "NotoSansSymbols2-Subset.ttf", FontFolder + "NotoSansSymbols-Subset.ttf" };
        public const string ScenePath = "Assets/VuonTra/Scenes/Garden.unity";

        struct Row
        {
            public string Id, Name, Accent;
            public StationKind Kind;
            public double Cost, Growth, Income, Cycle;
            public string[] Boosts;
            public int NeedLevel;
            public double BoostScale;
        }

        static RabbitTrade TradeOf(Row row)
        {
            if (row.Kind == StationKind.Ingredient) return RabbitTrade.Gatherer;
            switch (row.Id)
            {
                case "am-tra": case "tra-sua": case "thuyen-tra": return RabbitTrade.Brewer;
                case "noi-mut": case "lo-banh": case "tiem-banh": case "lo-keo": return RabbitTrade.Baker;
                case "binh-sao": case "dai-sao": return RabbitTrade.Stargazer;
                default: return RabbitTrade.None;
            }
        }

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
            new Row { Id = "gian-nho", Name = "Giàn nho trăng",     Kind = StationKind.Ingredient, Cost = 6.5e9,  Growth = 1.15, Accent = "b48be8", Boosts = new[] { "tra-sua", "tiem-banh" } },
            new Row { Id = "tiem-banh",Name = "Tiệm bánh trung thu",Kind = StationKind.Brew,       Cost = 9e10,   Growth = 1.15, Income = 7e7,    Cycle = 40,  Accent = "f0a93a" },
            new Row { Id = "suoi-hoa", Name = "Suối khoáng hoa",    Kind = StationKind.Ingredient, Cost = 1.3e12, Growth = 1.16, Accent = "6fa8f0", Boosts = new[] { "binh-sao", "dai-sao" } },
            new Row { Id = "dai-sao",  Name = "Đài ngắm sao",       Kind = StationKind.Brew,       Cost = 2e13,   Growth = 1.16, Income = 9e8,    Cycle = 50,  Accent = "ffc866" },
            new Row { Id = "ho-sen",   Name = "Vườn hoa sen",       Kind = StationKind.Ingredient, Cost = 3e14,   Growth = 1.16, Accent = "f2a7c3", Boosts = new[] { "tiem-banh", "dai-sao" }, NeedLevel = 16, BoostScale = 0.07 },
            new Row { Id = "lo-keo",   Name = "Lò kẹo mạch nha",    Kind = StationKind.Brew,       Cost = 4.5e15, Growth = 1.17, Income = 1.2e10, Cycle = 60,  Accent = "e3a857", NeedLevel = 18 },
            new Row { Id = "ruong-nep",Name = "Ruộng lúa nếp",      Kind = StationKind.Ingredient, Cost = 6.5e16, Growth = 1.17, Accent = "b9cf6a", Boosts = new[] { "lo-keo", "tra-sua" }, NeedLevel = 20, BoostScale = 0.07 },
            new Row { Id = "thuyen-tra",Name = "Thuyền trà đêm",    Kind = StationKind.Brew,       Cost = 1e18,   Growth = 1.17, Income = 1.8e11, Cycle = 75,  Accent = "6f8fd6", NeedLevel = 23 },
        };

        [MenuItem("VuonTra/Seed Content")]
        public static void Seed()
        {
            L10nSeeder.Seed();
            PlaceholderArt.Generate();
            FinalArt.Import();
            StreetArt.ImportGround();
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
                data.Trade = TradeOf(row);
                data.BaseCost = row.Cost;
                data.CostGrowth = row.Growth;
                data.BaseIncome = row.Income;
                data.CycleSeconds = row.Cycle;
                data.NeedGardenLevel = row.NeedLevel;
                data.BoostScale = row.BoostScale > 0 ? row.BoostScale : 1d;
                data.Accent = PlaceholderArt.Hex(row.Accent);
                data.Prop = FinalArt.Load("prop_" + row.Id);
                data.Product = FinalArt.Find("product_" + row.Id);
                data.Rabbit = FinalArt.Find("rabbit_" + row.Id);
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
            catalog.Rabbit = FinalArt.Load("rabbit");
            catalog.Scarf = FinalArt.Find("rabbit") != null ? FinalArt.Find("scarf") : PlaceholderArt.Load("scarf");
            catalog.Panel = FinalArt.Load("panel");
            catalog.Round = FinalArt.Load("round");
            catalog.Glow = FinalArt.Load("glow");
            catalog.Currency = FinalArt.Load("carrot");
            catalog.Gear = FinalArt.Load("gear");
            catalog.Background = FinalArt.Load("background");
            catalog.BodyFont = AssetDatabase.LoadAssetAtPath<Font>(BodyFontPath);
            catalog.TitleFont = AssetDatabase.LoadAssetAtPath<Font>(TitleFontPath);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();

            MetaSeeder.Seed(catalog);
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
            cam.backgroundColor = Palette.Night;
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
            PlayerSettings.productName = BuildScript.ProductName;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.defaultScreenWidth = 540;
            PlayerSettings.defaultScreenHeight = 960;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;
            PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, BuildScript.BundleId);
            ApplyMobileSettings();
        }

        static void ApplyMobileSettings()
        {
            var ios = UnityEditor.Build.NamedBuildTarget.iOS;
            var android = UnityEditor.Build.NamedBuildTarget.Android;
            PlayerSettings.SetApplicationIdentifier(ios, BuildScript.BundleId);
            PlayerSettings.SetApplicationIdentifier(android, BuildScript.BundleId);
            PlayerSettings.bundleVersion = BuildScript.Version;
            PlayerSettings.iOS.buildNumber = BuildScript.BuildNumber.ToString();
            PlayerSettings.Android.bundleVersionCode = BuildScript.BuildNumber;
            PlayerSettings.macOS.buildNumber = BuildScript.BuildNumber.ToString();

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.statusBarHidden = true;
            PlayerSettings.iOS.targetDevice = iOSTargetDevice.iPhoneAndiPad;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetScriptingBackend(ios, ScriptingImplementation.IL2CPP);
            PlayerSettings.SetScriptingBackend(android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(FinalArt.Folder + "/icon_app.png");
            if (icon != null)
            {
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
                foreach (UnityEditor.Build.NamedBuildTarget platform in new[] { ios, android })
                {
                    try
                    {
                        foreach (PlatformIconKind kind in PlayerSettings.GetSupportedIconKinds(platform))
                        {
                            PlatformIcon[] icons = PlayerSettings.GetPlatformIcons(platform, kind);
                            foreach (PlatformIcon slot in icons) slot.SetTextures(icon);
                            PlayerSettings.SetPlatformIcons(platform, kind, icons);
                        }
                    }
                    catch (System.Exception e) { Debug.Log("[VuonTra] Icon " + platform.TargetName + ": " + e.Message + " (cài module rồi seed lại)"); }
                }
            }

            var splash = AssetDatabase.LoadAssetAtPath<Sprite>(FinalArt.Folder + "/icon_app.png");
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.backgroundColor = VuonTra.Game.UI.Palette.Night;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.LightOnDark;
            if (splash != null) PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(2f, splash) };
        }
    }
}

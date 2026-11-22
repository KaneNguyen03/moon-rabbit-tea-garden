using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VuonTra.EditorTools
{
    /// <summary>
    /// The players, built from the command line:
    ///   Unity -batchmode -projectPath . -executeMethod VuonTra.EditorTools.BuildScript.BuildMac -quit
    ///   Unity -batchmode -projectPath . -executeMethod VuonTra.EditorTools.BuildScript.BuildIOS -quit
    ///   Unity -batchmode -projectPath . -executeMethod VuonTra.EditorTools.BuildScript.BuildAndroid -quit
    /// iOS gives an Xcode project to archive; Android gives an APK for internal testing. Both need their
    /// Unity module (Unity Hub → Installs → Add modules) and the platform's own tools (Xcode; the Android SDK,
    /// NDK and JDK that the module installs) on the machine that builds. Without the module the build stops
    /// at once and says so, rather than failing halfway.
    /// </summary>
    public static class BuildScript
    {
        public const string MacPath = "Build/Mac/VuonTraTho.app";
        public const string IOSPath = "Build/iOS";
        public const string AndroidPath = "Build/Android/VuonTraTho.apk";

        /// <summary>The app's name on the stores, the window and the log folder; the title screen keeps its Vietnamese name.</summary>
        public const string ProductName = "Moon Rabbit Tea Garden";
        /// <summary>One bundle id for every store, so the same account and cloud save follow the game across platforms.</summary>
        public const string BundleId = "com.vuontratho.game";
        /// <summary>The version the stores show; the build number counts every upload and never goes backwards.</summary>
        public const string Version = "0.1.0";
        public const int BuildNumber = 1;

        [MenuItem("VuonTra/Build Mac")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, MacPath);

        [MenuItem("VuonTra/Build iOS (Xcode project)")]
        public static void BuildIOS() => Build(BuildTarget.iOS, BuildTargetGroup.iOS, IOSPath);

        [MenuItem("VuonTra/Build Android (APK)")]
        public static void BuildAndroid() => Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidPath);

        static void Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                Debug.LogError("[VuonTra] Thiếu module " + target + ": cài qua Unity Hub (Installs → Add modules), kèm Xcode cho iOS hoặc Android SDK/NDK/OpenJDK cho Android, rồi chạy lại.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            // The language tables are Addressables content: build it first or the player has no text.
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ContentSeeder.ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[VuonTra] Build " + target + " " + report.summary.result + ", " + report.summary.totalSize + " bytes");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}

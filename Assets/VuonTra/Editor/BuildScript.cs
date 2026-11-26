using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;

namespace VuonTra.EditorTools
{
    public static class BuildScript
    {
        public const string MacPath = "Build/Mac/VuonTraTho.app";
        public const string IOSPath = "Build/iOS";
        public const string AndroidPath = "Build/Android/VuonTraTho.apk";
        public const string WebGLPath = "Build/WebGL";
        public const string WebGLTemplate = "PROJECT:VuonTra";

        public const string ProductName = "Moon Rabbit Tea Garden";
        public const string BundleId = "com.vuontratho.game";
        public const string Version = "0.1.0";
        public const int BuildNumber = 1;

        [MenuItem("VuonTra/Build Mac")]
        public static void BuildMac() => Build(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, MacPath);

        [MenuItem("VuonTra/Build iOS (Xcode project)")]
        public static void BuildIOS() => Build(BuildTarget.iOS, BuildTargetGroup.iOS, IOSPath);

        [MenuItem("VuonTra/Build Android (APK)")]
        public static void BuildAndroid() => Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidPath);

        [MenuItem("VuonTra/Build WebGL")]
        public static void BuildWebGL()
        {
            BuildTarget previous = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup previousGroup = BuildPipeline.GetBuildTargetGroup(previous);
            ConfigureWebGL();
            if (previous != BuildTarget.WebGL && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[VuonTra] Build WebGL: không chuyển được build target sang WebGL; kiểm tra module WebGL trong Unity Hub.");
                if (Application.isBatchMode) EditorApplication.Exit(2);
                return;
            }
            int exitCode = BuildAndReport(BuildTarget.WebGL, BuildTargetGroup.WebGL, WebGLPath);
            if (previous != BuildTarget.WebGL) EditorUserBuildSettings.SwitchActiveBuildTarget(previousGroup, previous);
            if (Application.isBatchMode && exitCode != 0) EditorApplication.Exit(exitCode);
        }

        public static void ConfigureWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = WebGLTemplate;
            PlayerSettings.WebGL.dataCaching = true;
        }

        static void Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            int exitCode = BuildAndReport(target, group, path);
            if (Application.isBatchMode && exitCode != 0) EditorApplication.Exit(exitCode);
        }

        static int BuildAndReport(BuildTarget target, BuildTargetGroup group, string path)
        {
            if (!BuildPipeline.IsBuildTargetSupported(group, target))
            {
                Debug.LogError("[VuonTra] Thiếu module " + target + ": cài qua Unity Hub (Installs → Add modules), kèm Xcode cho iOS hoặc Android SDK/NDK/OpenJDK cho Android, rồi chạy lại.");
                return 2;
            }
            UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent();

            var options = new BuildPlayerOptions
            {
                scenes = new[] { ContentSeeder.ScenePath },
                locationPathName = path,
                target = target,
                options = BuildOptions.None,
                extraScriptingDefines = ExtraDefines()
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[VuonTra] Build " + target + " " + report.summary.result + ", " + report.summary.totalSize + " bytes");
            return report.summary.result == BuildResult.Succeeded ? 0 : 1;
        }

        public const string MonetizationDefine = "VUONTRA_MONETIZATION";

        public static string[] ExtraDefines() =>
            System.Environment.GetEnvironmentVariable(MonetizationDefine) == "1" ? new[] { MonetizationDefine } : new string[0];

        public const string IOSEntitlements = "Unity-iPhone/VuonTraTho.entitlements";
    }

    public sealed class IOSCapabilities : IPostprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPostprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            string pbx = PBXProject.GetPBXProjectPath(report.summary.outputPath);
            var project = new PBXProject();
            project.ReadFromFile(pbx);
            var capabilities = new ProjectCapabilityManager(pbx, BuildScript.IOSEntitlements, null, project.GetUnityMainTargetGuid());
            capabilities.AddGameCenter();
            capabilities.AddiCloud(true, false, null);
            capabilities.WriteToFile();
            Debug.Log("[VuonTra] Xcode: Game Center + iCloud key-value store added, entitlements at " + BuildScript.IOSEntitlements);
        }
    }
}

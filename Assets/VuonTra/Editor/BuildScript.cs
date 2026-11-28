using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.iOS.Xcode;
using UnityEngine;
using UnityEngine.Rendering;
using VuonTra.Release;

namespace VuonTra.EditorTools
{
    public static class BuildScript
    {
        public const string MacPath = "Build/Mac/VuonTraTho.app";
        public const string IOSPath = "Build/iOS";
        public const string AndroidBundlePath = "Build/Android/VuonTraTho.aab";
        public const string AndroidApkPath = "Build/Android/VuonTraTho.apk";
        public const string WebGLPath = "Build/WebGL";
        public const string WebGLTemplate = "PROJECT:VuonTra";
        public static readonly GraphicsDeviceType[] WebGraphicsApis = { GraphicsDeviceType.WebGPU, GraphicsDeviceType.OpenGLES3 };

        public const int BuildFailedExitCode = 1;
        public const int MissingModuleExitCode = 2;
        public const int MissingSigningExitCode = 3;

        public const string ProductName = "Moon Rabbit Tea Garden";
        public const string BundleId = "com.vuontratho.game";
        public const string Version = "0.1.0";
        public const int BuildNumber = 1;

        [MenuItem("VuonTra/Build Mac")]
        public static void BuildMac() => Finish(Build(BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, MacPath));

        [MenuItem("VuonTra/Build iOS (Xcode project)")]
        public static void BuildIOS() => Finish(Build(BuildTarget.iOS, BuildTargetGroup.iOS, IOSPath));

        [MenuItem("VuonTra/Build Android (AAB, upload key)")]
        public static void BuildAndroid() => Finish(BuildSignedBundle());

        [MenuItem("VuonTra/Build WebGL")]
        public static void BuildWebGL()
        {
            BuildTarget previous = EditorUserBuildSettings.activeBuildTarget;
            BuildTargetGroup previousGroup = BuildPipeline.GetBuildTargetGroup(previous);
            ConfigureWebGL();
            if (previous != BuildTarget.WebGL && !EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[VuonTra] Build WebGL: không chuyển được build target sang WebGL; kiểm tra module WebGL trong Unity Hub.");
                Finish(MissingModuleExitCode);
                return;
            }
            int exitCode = Build(BuildTarget.WebGL, BuildTargetGroup.WebGL, WebGLPath);
            if (previous != BuildTarget.WebGL) EditorUserBuildSettings.SwitchActiveBuildTarget(previousGroup, previous);
            Finish(exitCode);
        }

        public static void ConfigureWebGL()
        {
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.ExplicitlyThrownExceptionsOnly;
            PlayerSettings.WebGL.template = WebGLTemplate;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.WebGL, WebGraphicsApis);
        }

        [MenuItem("VuonTra/Build Android (APK, debug key)")]
        public static void BuildAndroidApk()
        {
            ForgetAndroidSigning();
            EditorUserBuildSettings.buildAppBundle = false;
            Finish(Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidApkPath));
        }

        static int BuildSignedBundle()
        {
            if (!HasModule(BuildTarget.Android, BuildTargetGroup.Android)) return MissingModuleExitCode;
            AndroidSigning signing = AndroidSigning.Read(System.Environment.GetEnvironmentVariable, File.Exists, out string problem);
            if (signing == null)
            {
                Debug.LogError(problem);
                return MissingSigningExitCode;
            }
            try
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = signing.Keystore;
                PlayerSettings.Android.keystorePass = signing.KeystorePassword;
                PlayerSettings.Android.keyaliasName = signing.KeyAlias;
                PlayerSettings.Android.keyaliasPass = signing.KeyPassword;
                EditorUserBuildSettings.buildAppBundle = true;
                return Build(BuildTarget.Android, BuildTargetGroup.Android, AndroidBundlePath);
            }
            finally
            {
                ForgetAndroidSigning();
            }
        }

        static void ForgetAndroidSigning()
        {
            PlayerSettings.Android.useCustomKeystore = false;
            PlayerSettings.Android.keystoreName = "";
            PlayerSettings.Android.keystorePass = "";
            PlayerSettings.Android.keyaliasName = "";
            PlayerSettings.Android.keyaliasPass = "";
        }

        static void Finish(int exitCode)
        {
            if (Application.isBatchMode && exitCode != 0) EditorApplication.Exit(exitCode);
        }

        static bool HasModule(BuildTarget target, BuildTargetGroup group)
        {
            if (BuildPipeline.IsBuildTargetSupported(group, target)) return true;
            Debug.LogError("[VuonTra] Thiếu module " + target + ": cài qua Unity Hub (Installs → Add modules), kèm Xcode cho iOS hoặc Android SDK/NDK/OpenJDK cho Android, rồi chạy lại.");
            return false;
        }

        static int Build(BuildTarget target, BuildTargetGroup group, string path)
        {
            if (!HasModule(target, group)) return MissingModuleExitCode;
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
            return report.summary.result == BuildResult.Succeeded ? 0 : BuildFailedExitCode;
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

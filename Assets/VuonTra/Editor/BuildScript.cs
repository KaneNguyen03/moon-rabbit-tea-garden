using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace VuonTra.EditorTools
{
    public static class BuildScript
    {
        public const string MacPath = "Build/Mac/VuonTraTho.app";

        [MenuItem("VuonTra/Build Mac")]
        public static void BuildMac()
        {
            var options = new BuildPlayerOptions
            {
                scenes = new[] { ContentSeeder.ScenePath },
                locationPathName = MacPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            Debug.Log("[VuonTra] Build " + report.summary.result + ", " + report.summary.totalSize + " bytes");
            if (Application.isBatchMode && report.summary.result != BuildResult.Succeeded) EditorApplication.Exit(1);
        }
    }
}

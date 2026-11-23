using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Unity.Notifications;

namespace VuonTra.EditorTools
{
    sealed class NotificationsBuildSettings : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform != BuildTarget.iOS) return;
            if (NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch)
                NotificationSettings.iOSSettings.RequestAuthorizationOnAppLaunch = false;
            Debug.Log("[VuonTra] Notifications: no permission request at launch (asked from the pause card)");
        }
    }
}

using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using VuonTra.EditorTools;

namespace VuonTra.Core.Tests
{
    /// <summary>What the seeder sets for the phone builds (#41): read back from the project, so a build on another machine starts from the same numbers.</summary>
    public class BuildSettingsTests
    {
        [Test]
        public void Every_Platform_Shares_One_Bundle_Id_And_One_Version()
        {
            Assert.AreEqual(BuildScript.BundleId, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone));
            Assert.AreEqual(BuildScript.BundleId, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS));
            Assert.AreEqual(BuildScript.BundleId, PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android));
            Assert.AreEqual(BuildScript.Version, PlayerSettings.bundleVersion);
            Assert.AreEqual("Moon Rabbit Tea Garden", PlayerSettings.productName, "the English name is the app's name (#15)");
            Assert.AreEqual("VuonTraTho", PlayerSettings.companyName);
            Assert.AreEqual(BuildScript.BuildNumber.ToString(), PlayerSettings.iOS.buildNumber);
            Assert.AreEqual(BuildScript.BuildNumber, PlayerSettings.Android.bundleVersionCode);
        }

        [Test]
        public void The_Phone_Builds_Are_Portrait_Only()
        {
            Assert.AreEqual(UIOrientation.Portrait, PlayerSettings.defaultInterfaceOrientation);
            Assert.IsTrue(PlayerSettings.allowedAutorotateToPortrait);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToPortraitUpsideDown);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeLeft);
            Assert.IsFalse(PlayerSettings.allowedAutorotateToLandscapeRight);
        }

        [Test]
        public void The_Icon_And_The_Splash_Come_From_The_Game_Art()
        {
            Texture2D[] icons = PlayerSettings.GetIcons(NamedBuildTarget.Unknown, IconKind.Any);
            Assert.IsTrue(icons.Length > 0 && icons[0] != null, "a default icon for every platform");
            Assert.AreEqual("icon_app", icons[0].name);
            Assert.AreEqual(1024, icons[0].width);
            Assert.IsTrue(PlayerSettings.SplashScreen.show);
            Assert.AreEqual(VuonTra.Game.UI.Palette.Night, PlayerSettings.SplashScreen.backgroundColor);
            Assert.AreEqual(1, PlayerSettings.SplashScreen.logos.Length);
            Assert.AreEqual("icon_app", PlayerSettings.SplashScreen.logos[0].logo.name);
        }

        [Test]
        public void The_Build_Paths_Are_Under_Build_And_Named_For_The_Game()
        {
            StringAssert.StartsWith("Build/", BuildScript.MacPath);
            StringAssert.StartsWith("Build/", BuildScript.IOSPath);
            StringAssert.StartsWith("Build/", BuildScript.AndroidPath);
            StringAssert.EndsWith(".apk", BuildScript.AndroidPath);
        }
    }
}

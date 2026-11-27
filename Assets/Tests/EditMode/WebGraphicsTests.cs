using NUnit.Framework;
using UnityEditor;
using UnityEngine.Rendering;
using VuonTra.EditorTools;

namespace VuonTra.Core.Tests
{
    public class WebGraphicsTests
    {
        [Test]
        public void The_Web_Build_Picks_Its_Own_Graphics_Apis()
        {
            Assert.IsFalse(PlayerSettings.GetUseDefaultGraphicsAPIs(BuildTarget.WebGL),
                "Auto Graphics API gives WebGL 2 only; WebGPU must be chosen by hand (Refs #150)");
        }

        [Test]
        public void The_Web_Build_Renders_With_WebGPU_First_And_Falls_Back_To_WebGL2()
        {
            GraphicsDeviceType[] apis = PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL);
            Assert.AreEqual(new[] { GraphicsDeviceType.WebGPU, GraphicsDeviceType.OpenGLES3 }, apis,
                "WebGPU first, WebGL 2 next so a browser without WebGPU still runs the game");
        }

        [Test]
        public void The_Build_Script_Sets_The_Same_List_It_Is_Tested_Against()
        {
            CollectionAssert.AreEqual(PlayerSettings.GetGraphicsAPIs(BuildTarget.WebGL), BuildScript.WebGraphicsApis);
        }
    }
}

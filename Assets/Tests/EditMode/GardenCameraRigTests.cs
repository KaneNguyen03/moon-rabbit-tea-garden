using NUnit.Framework;
using UnityEngine;
using VuonTra.Game;

namespace VuonTra.Core.Tests
{
    public class GardenCameraRigTests
    {
        GameObject _host;
        GardenCameraSettings _settings;

        [TearDown]
        public void TearDown()
        {
            if (_host != null) Object.DestroyImmediate(_host);
            if (_settings != null) Object.DestroyImmediate(_settings);
        }

        [Test]
        public void The_Settings_Asset_Starts_On_The_Agreed_Lens_Tilt_And_Distance()
        {
            _settings = ScriptableObject.CreateInstance<GardenCameraSettings>();
            CameraSpec spec = _settings.Spec();
            var agreed = new CameraSpec();
            Assert.AreEqual(agreed.FovDegrees, spec.FovDegrees, 1e-6);
            Assert.AreEqual(agreed.TiltDegrees, spec.TiltDegrees, 1e-6);
            Assert.AreEqual(agreed.DefaultDistance, spec.DefaultDistance, 1e-6);
            Assert.AreEqual(agreed.EdgeGive, spec.EdgeGive, 1e-6);
            Assert.AreEqual(agreed.GlideFadeSeconds, spec.GlideFadeSeconds, 1e-6);
        }

        [Test]
        public void Framing_A_Ground_Point_Puts_It_In_The_Middle_Of_A_Perspective_View_That_Never_Turns()
        {
            _host = new GameObject("garden camera");
            Camera view = _host.AddComponent<Camera>();
            GardenCameraRig rig = _host.AddComponent<GardenCameraRig>();
            rig.Attach(view, new CameraSpec());
            rig.Frame(new GroundPoint(2d, -3d), 35d);
            Assert.IsFalse(view.orthographic);
            Assert.AreEqual(25f, view.fieldOfView, 1e-4f);
            Assert.AreEqual(0f, view.transform.eulerAngles.y, 1e-4f);
            Assert.AreEqual(0f, view.transform.eulerAngles.z, 1e-4f);
            Assert.AreEqual(40f, view.transform.eulerAngles.x, 1e-3f);
            var centre = new Ray(view.transform.position, view.transform.forward);
            Assert.IsTrue(new Plane(Vector3.up, Vector3.zero).Raycast(centre, out float along));
            Vector3 hit = centre.GetPoint(along);
            Assert.AreEqual(2f, hit.x, 1e-3f);
            Assert.AreEqual(-3f, hit.z, 1e-3f);
            Assert.AreEqual(35f, along, 1e-3f);
            Assert.AreEqual(2d, rig.Focus.X, 1e-9);
            Assert.AreEqual(35d, rig.Distance, 1e-9);
        }
    }
}

using UnityEngine;
using VuonTra.Core;

namespace VuonTra.Game
{
    public sealed class GardenCameraRig : MonoBehaviour
    {
        [SerializeField] Camera view;
        [SerializeField] GardenCameraSettings settings;

        CameraSpec _spec;

        public Camera View => view;
        public CameraSpec Spec => _spec ??= settings != null ? settings.Spec() : new CameraSpec();
        public GroundPoint Focus { get; private set; }
        public double Distance { get; private set; }

        public void Attach(Camera camera, CameraSpec spec)
        {
            view = camera;
            _spec = spec;
        }

        public void Frame(GroundPoint focus, double distance)
        {
            Focus = focus;
            Distance = distance;
            CameraPose pose = GardenFraming.Pose(focus, distance, Spec);
            view.orthographic = false;
            view.fieldOfView = (float)Spec.FovDegrees;
            view.transform.SetPositionAndRotation(new Vector3((float)pose.X, (float)pose.Y, (float)pose.Z), Quaternion.Euler((float)pose.PitchDegrees, 0f, 0f));
        }
    }
}

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VuonTra.Core;
using VuonTra.Game.UI;

namespace VuonTra.Game
{
    public sealed class GardenMapCamera : MonoBehaviour
    {
        const float Height = 100f;

        Camera _view;
        RenderTexture _shot;

        public Camera View => _view;
        public RenderTexture Shot => _shot;

        public void Attach(Camera view)
        {
            _view = view;
            view.enabled = false;
            view.orthographic = true;
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Palette.Night;
            view.nearClipPlane = 1f;
            view.farClipPlane = Height * 2f;
        }

        public RenderTexture Shoot(GroundRect island, int width, int height)
        {
            Release();
            _shot = RenderTexture.GetTemporary(width, height, 24);
            MapFrame frame = GardenFraming.Map(island, (double)width / height);
            _view.orthographicSize = (float)frame.HalfHeight;
            _view.transform.SetPositionAndRotation(new Vector3((float)frame.Centre.X, Height, (float)frame.Centre.Z), Quaternion.Euler(90f, 0f, 0f));
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = _shot };
            if (RenderPipeline.SupportsRenderRequest(_view, request)) RenderPipeline.SubmitRenderRequest(_view, request);
            return _shot;
        }

        public void Release()
        {
            if (_shot == null) return;
            RenderTexture.ReleaseTemporary(_shot);
            _shot = null;
        }

        void OnDestroy() => Release();
    }
}

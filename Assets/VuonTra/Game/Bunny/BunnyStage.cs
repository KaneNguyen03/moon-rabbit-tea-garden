using System.Collections.Generic;
using UnityEngine;

namespace VuonTra.Game.Bunny
{
    /// <summary>
    /// Where the paper-doll rabbits stand (owner's review 23 Nov): far from the garden, in a row of slots seen by one
    /// camera of their own that draws them into one render texture. The garden is a UI canvas, so each station shows
    /// its slot of that texture (RawImage.uvRect). Slots are 512 by 512 drawing pixels, 5.12 units at 100 pixels per
    /// unit, in two rows of eight.
    /// </summary>
    public sealed class BunnyStage
    {
        public const int Columns = 8, Rows = 2, SlotPixels = 256;
        public const float SlotUnits = 5.12f;
        static readonly Vector3 Origin = new Vector3(10000f, 0f, 0f);

        readonly GameObject _prefab;
        readonly Transform _root;
        readonly Camera _camera;
        readonly List<BunnyFigure> _figures = new List<BunnyFigure>();

        /// <summary>What the stage's camera draws: Columns × Rows slots.</summary>
        public RenderTexture Texture { get; }

        static BunnyStage _shared;

        /// <summary>The one stage every station draws from, made on first use; null without a prefab.</summary>
        public static BunnyStage Shared(GameObject prefab)
        {
            if (prefab == null) return null;
            if (_shared == null || _shared._root == null || _shared._prefab != prefab) _shared = new BunnyStage(prefab);
            return _shared;
        }

        public BunnyStage(GameObject prefab)
        {
            _prefab = prefab;
            _root = new GameObject("BunnyStage").transform;
            _root.position = Origin;
            Texture = new RenderTexture(Columns * SlotPixels, Rows * SlotPixels, 16, RenderTextureFormat.ARGB32) { name = "BunnyStage" };
            var cam = new GameObject("BunnyCamera");
            cam.transform.SetParent(_root, false);
            cam.transform.localPosition = new Vector3(0f, 0f, -10f);
            _camera = cam.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.orthographicSize = Rows * SlotUnits / 2f;
            _camera.aspect = Columns / (float)Rows;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.targetTexture = Texture;
            _camera.nearClipPlane = 0.1f;
            _camera.farClipPlane = 50f;
        }

        /// <summary>The figure in slot <paramref name="slot"/>, made the first time it is asked for; null past the last slot or without a prefab.</summary>
        public BunnyFigure Figure(int slot)
        {
            if (_prefab == null || slot < 0 || slot >= Columns * Rows) return null;
            while (_figures.Count <= slot) _figures.Add(null);
            if (_figures[slot] == null)
            {
                GameObject go = Object.Instantiate(_prefab, _root);
                go.name = "Bunny " + slot;
                go.transform.localPosition = SlotFeet(slot);
                _figures[slot] = go.GetComponent<BunnyFigure>();
            }
            return _figures[slot];
        }

        /// <summary>Where a slot's canvas stands (its bottom middle), from the stage's centre.</summary>
        public static Vector3 SlotFeet(int slot)
        {
            int col = slot % Columns, row = slot / Columns;
            return new Vector3((col + 0.5f - Columns / 2f) * SlotUnits, (Rows / 2f - row - 1) * SlotUnits, 0f);
        }

        /// <summary>The part of <see cref="Texture"/> that shows slot <paramref name="slot"/>.</summary>
        public static Rect SlotRect(int slot)
        {
            int col = slot % Columns, row = slot / Columns;
            return new Rect(col / (float)Columns, (Rows - 1 - row) / (float)Rows, 1f / Columns, 1f / Rows);
        }

        public void Dispose()
        {
            if (_camera != null) _camera.targetTexture = null;
            if (Texture != null) Texture.Release();
            if (_root == null) return;
            if (Application.isPlaying) Object.Destroy(_root.gameObject);
            else Object.DestroyImmediate(_root.gameObject);
        }
    }
}

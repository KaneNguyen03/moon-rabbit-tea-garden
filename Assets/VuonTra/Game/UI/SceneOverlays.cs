using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public static class SceneOverlays
    {
        public const int Budget = 2;
        public const float VignetteStrength = 0.22f;
        public const float MoonlightStrength = 0.10f;
        public const float GrassTileStrength = 0.40f;
        public const float LanternPoolAlpha = 0.45f;

        public static Color OnScreen(Color c) => (Color32)c;

        public static Color Washed(Color under, Color wash)
        {
            if (wash.a <= 0f) return under;
            Color tone = Palette.Mix(under.linear, wash.linear, wash.a).gamma;
            return Palette.Alpha(tone, under.a);
        }

        public static List<Graphic> Over(RectTransform canvas, Transform world, Transform hud)
        {
            var found = new List<Graphic>();
            var canvasCorners = new Vector3[4];
            canvas.GetWorldCorners(canvasCorners);
            var corners = new Vector3[4];
            for (int i = world.GetSiblingIndex() + 1; i < hud.GetSiblingIndex(); i++)
                foreach (Graphic g in canvas.GetChild(i).GetComponentsInChildren<Graphic>(true))
                {
                    if (!g.enabled || !g.gameObject.activeInHierarchy || g.color.a <= 0f || Hidden(g.transform)) continue;
                    g.rectTransform.GetWorldCorners(corners);
                    if (Covers(corners, canvasCorners)) found.Add(g);
                }
            return found;
        }

        static bool Hidden(Transform t)
        {
            for (; t != null; t = t.parent)
            {
                var group = t.GetComponent<CanvasGroup>();
                if (group != null && group.alpha <= 0f) return true;
            }
            return false;
        }

        static bool Covers(Vector3[] rect, Vector3[] screen)
        {
            const float Slack = 1f;
            return rect[0].x <= screen[0].x + Slack && rect[0].y <= screen[0].y + Slack
                && rect[2].x >= screen[2].x - Slack && rect[2].y >= screen[2].y - Slack;
        }
    }
}

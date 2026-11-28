using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Core.Tests
{
    public static class TextFit
    {
        public static float Height(TextGenerator generator, string value, TextGenerationSettings settings)
        {
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            return Drawn(generator, value, settings);
        }

        public static float Height(Text text)
        {
            return Drawn(new TextGenerator(), text.text, text.GetGenerationSettings(text.rectTransform.rect.size)) / text.pixelsPerUnit;
        }

        static float Drawn(TextGenerator generator, string value, TextGenerationSettings settings)
        {
            generator.Populate(value, settings);
            float box = settings.generationExtents.y * settings.scaleFactor;
            float bottom = -settings.pivot.y * box, top = bottom + box;
            if (!InkSpan(generator.verts, out float inkBottom, out float inkTop)) return 0f;
            float cut = generator.characterCountVisible < value.TrimEnd().Length ? box : 0f;
            return box + Mathf.Max(0f, inkTop - top) + Mathf.Max(0f, bottom - inkBottom) + cut;
        }

        static bool InkSpan(IList<UIVertex> verts, out float bottom, out float top)
        {
            bottom = float.MaxValue;
            top = float.MinValue;
            for (int i = 0; i + 3 < verts.Count; i += 4)
            {
                float low = float.MaxValue, high = float.MinValue, left = float.MaxValue, right = float.MinValue;
                for (int k = 0; k < 4; k++)
                {
                    Vector3 at = verts[i + k].position;
                    low = Mathf.Min(low, at.y);
                    high = Mathf.Max(high, at.y);
                    left = Mathf.Min(left, at.x);
                    right = Mathf.Max(right, at.x);
                }
                if (high - low <= 0f || right - left <= 0f) continue;
                bottom = Mathf.Min(bottom, low);
                top = Mathf.Max(top, high);
            }
            return top > bottom;
        }
    }
}

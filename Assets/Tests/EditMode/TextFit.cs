using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Core.Tests
{
    public static class TextFit
    {
        const string OneLine = "A";

        public static float Height(TextGenerator generator, string value, TextGenerationSettings settings)
        {
            float oneLine = generator.GetPreferredHeight(OneLine, settings);
            float leading = Mathf.Max(0f, oneLine - settings.fontSize * settings.scaleFactor);
            return generator.GetPreferredHeight(value, settings) - leading;
        }

        public static float Height(Text text)
        {
            var generator = new TextGenerator();
            TextGenerationSettings settings = text.GetGenerationSettings(text.rectTransform.rect.size);
            if (text.resizeTextForBestFit)
            {
                generator.Populate(text.text, settings);
                settings.fontSize = generator.fontSizeUsedForBestFit;
                settings.resizeTextForBestFit = false;
            }
            float oneLine = generator.GetPreferredHeight(OneLine, settings);
            float leading = Mathf.Max(0f, oneLine - settings.fontSize * settings.scaleFactor);
            return text.preferredHeight - leading / text.pixelsPerUnit;
        }
    }
}

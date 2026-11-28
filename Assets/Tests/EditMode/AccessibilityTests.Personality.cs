using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Game;
using VuonTra.Game.UI;
using L10n = VuonTra.Game.L10n;

namespace VuonTra.Core.Tests
{
    public partial class AccessibilityTests
    {
        static Rect CanvasRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            return Rect.MinMaxRect(corners[0].x, corners[0].y, corners[2].x, corners[2].y);
        }

        string Overlaps(Text line, params string[] neighbours)
        {
            Rect mine = CanvasRect(line.rectTransform);
            float width = line.preferredWidth * line.rectTransform.lossyScale.x;
            Rect drawn = line.alignment == TextAnchor.MiddleCenter
                ? new Rect(mine.center.x - width / 2f, mine.y, width, mine.height)
                : new Rect(mine.x, mine.y, width, mine.height);
            foreach (string path in neighbours)
            {
                Transform other = root.transform.Find(path);
                if (other == null || !other.gameObject.activeInHierarchy) continue;
                if (drawn.Overlaps(CanvasRect((RectTransform)other))) return Path(line.transform) + " \"" + line.text + "\" runs into " + path;
            }
            return null;
        }

        const string Sheet = "Canvas/RabbitSheet/Card/Body/";

        static int DrawnSize(Text text)
        {
            if (!text.resizeTextForBestFit) return text.fontSize;
            var gen = new TextGenerator();
            gen.Populate(text.text, text.GetGenerationSettings(text.rectTransform.rect.size));
            return Mathf.RoundToInt(gen.fontSizeUsedForBestFit / text.pixelsPerUnit);
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void The_Personality_Line_Fits_On_One_Line_On_The_Rabbit_Sheet_And_The_Album_Page_For_Every_Kind(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.Shell.StartGame();
            GameState s = runner.State;
            var wrong = new List<string>();
            int lines = 0;
            string longestKind = "";
            foreach (RabbitKind kind in RabbitKinds.All)
                if (L10n.T("kind." + kind.Id).Length > longestKind.Length) longestKind = L10n.T("kind." + kind.Id);
            foreach (RabbitKind kind in RabbitKinds.All)
            {
                Rabbit r = Rabbits.ArriveKind(s, kind.Id, "Thỏ Rất Dài");
                r.Station = 0;
                screen.Detail.Open(runner.Defs, s, r.Id);
                Layout();
                Text own = root.transform.Find(Sheet + "Temper").GetComponent<Text>();
                Assert.AreEqual(RabbitDetailView.TemperText(runner.Likes, kind.Id), own.text);
                Assert.GreaterOrEqual(DrawnSize(own), RabbitDetailView.TemperLeast, own.text);
                if (!large) Assert.GreaterOrEqual(DrawnSize(own), RabbitDetailView.TemperSize - 1, "full size where there is room, give or take the canvas scale's rounding: " + own.text);
                string line = OneLine(own) ?? Overlaps(own, Sheet + "LevelUp", Sheet + "LevelMax", Sheet + "Rest", Sheet + "Where", Sheet + "LevelBar", Sheet + "Level");
                if (line != null) wrong.Add(line);
                screen.Detail.OpenKind(runner.Defs, s, kind.Id);
                Layout();
                Text page = root.transform.Find(Sheet + "Temper").GetComponent<Text>();
                Assert.GreaterOrEqual(DrawnSize(page), RabbitDetailView.TemperLeast, page.text);
                line = OneLine(page) ?? Overlaps(page, Sheet + "Trade");
                if (line != null) wrong.Add(line);
                Text about = root.transform.Find(Sheet + "About").GetComponent<Text>();
                if (CanvasRect(about.rectTransform).yMax > CanvasRect(page.rectTransform).yMin + 0.5f) wrong.Add(Path(about.transform) + " \"" + about.text + "\" sits on the personality line");
                lines += 2;
                s.Rabbits.Remove(r);
            }
            screen.Detail.Close();
            Assert.AreEqual(2 * RabbitKinds.All.Count, lines);
            Assert.That(longestKind.Length, Is.GreaterThan(14), "the longest kind name was among them: " + longestKind);
            Assert.IsEmpty(wrong, wrong.Count + " personality lines run over in " + code + (large ? " (large text)" : "") + ":\n" + string.Join("\n", wrong));
        }

        [TestCase("vi", false)]
        [TestCase("en", false)]
        [TestCase("vi", true)]
        [TestCase("en", true)]
        public void Every_Personality_And_Kind_Line_Fits_Its_Bubble(string code, bool large)
        {
            L10n.SetLocale(code);
            UiKit.SetLargeText(large);
            screen.RebuildUi();
            screen.Shell.StartGame();
            runner.State.Rabbits.Add(new Rabbit { Id = 1, Tier = 0, Kind = "tho-bong", Name = "Bông", Station = 0 });
            runner.State.NextRabbitId = 1;
            screen.Refresh();
            Assert.IsTrue(screen.ChatterNow());
            Layout();
            Text text = root.transform.Find("Canvas/Garden/Content/ChatLine/Text").GetComponent<Text>();
            var wrong = new List<string>();
            int lines = 0;
            foreach (string key in Personalities.AllKeys)
            {
                lines++;
                text.text = L10n.T(key);
                Assert.AreNotEqual(key, text.text, "the table has " + key);
                string line = Wrapped(text, 18);
                if (line != null) wrong.Add(line);
            }
            Assert.AreEqual(8 * 4 + 49, lines);
            Assert.IsEmpty(wrong, string.Join("\n", wrong));
        }
    }
}

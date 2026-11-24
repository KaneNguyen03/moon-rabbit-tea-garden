using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.EditorTools
{
    public sealed class PreviewLanguageCheck
    {
        public const string ReportName = "english-in-vi.txt";
        const int SegmentLength = 10;
        const int ShapeAnchorLength = 3;

        static readonly Regex Placeholder = new Regex(@"\{\d+(:[^}]*)?\}");
        static readonly Regex Markup = new Regex(@"<[^>]+>");

        readonly HashSet<string> _vietnamese = new HashSet<string>(StringComparer.Ordinal);
        readonly HashSet<string> _english = new HashSet<string>(StringComparer.Ordinal);
        readonly List<string> _englishSegments = new List<string>();
        readonly List<Shape> _englishShapes = new List<Shape>();
        readonly List<Shape> _vietnameseShapes = new List<Shape>();
        readonly Dictionary<string, bool> _seen = new Dictionary<string, bool>(StringComparer.Ordinal);
        readonly List<string> _found = new List<string>();

        public IReadOnlyList<string> Found => _found;

        sealed class Shape
        {
            public string Anchor;
            public Regex Pattern;
        }

        public PreviewLanguageCheck(string[,] rows, string[,] englishOne)
        {
            var allVietnamese = new StringBuilder();
            for (int r = 0; r < rows.GetLength(0); r++)
            {
                string vi = rows[r, 1];
                allVietnamese.Append(vi).Append('\u0001');
                if (Placeholder.IsMatch(vi)) AddShape(_vietnameseShapes, vi);
                else _vietnamese.Add(Clean(vi));
            }
            string vietnameseText = allVietnamese.ToString();

            var english = new List<string>();
            for (int r = 0; r < rows.GetLength(0); r++) if (rows[r, 2] != rows[r, 1]) english.Add(rows[r, 2]);
            for (int r = 0; r < englishOne.GetLength(0); r++) english.Add(englishOne[r, 1]);

            foreach (string en in english)
            {
                if (Placeholder.IsMatch(en)) AddShape(_englishShapes, en);
                else if (!_vietnamese.Contains(Clean(en))) _english.Add(Clean(en));
                foreach (string piece in Placeholder.Split(en))
                    foreach (string line in piece.Split('\n'))
                    {
                        string segment = Clean(line);
                        if (segment.Length >= SegmentLength && segment.IndexOf(' ') >= 0 && vietnameseText.IndexOf(segment, StringComparison.Ordinal) < 0 && !_englishSegments.Contains(segment))
                            _englishSegments.Add(segment);
                    }
            }
        }

        static string Clean(string text) => Markup.Replace(text ?? "", "").Trim();

        static void AddShape(List<Shape> shapes, string line)
        {
            string anchor = "";
            var pattern = new StringBuilder("^");
            int at = 0;
            foreach (Match m in Placeholder.Matches(line))
            {
                string literal = line.Substring(at, m.Index - at);
                pattern.Append(Regex.Escape(literal)).Append("(.*?)");
                if (literal.Trim().Length > anchor.Length) anchor = literal.Trim();
                at = m.Index + m.Length;
            }
            string tail = line.Substring(at);
            pattern.Append(Regex.Escape(tail)).Append('$');
            if (tail.Trim().Length > anchor.Length) anchor = tail.Trim();
            if (anchor.Length < ShapeAnchorLength) return;
            shapes.Add(new Shape { Anchor = Clean(anchor), Pattern = new Regex(pattern.ToString(), RegexOptions.Singleline) });
        }

        static bool Fits(List<Shape> shapes, string text)
        {
            foreach (Shape shape in shapes)
                if (text.IndexOf(shape.Anchor, StringComparison.Ordinal) >= 0 && shape.Pattern.IsMatch(text)) return true;
            return false;
        }

        public bool IsEnglish(string text)
        {
            string t = Clean(text);
            if (t.Length == 0) return false;
            if (_seen.TryGetValue(t, out bool known)) return known;
            bool english;
            if (_vietnamese.Contains(t) || Fits(_vietnameseShapes, t)) english = false;
            else if (_english.Contains(t)) english = true;
            else
            {
                english = false;
                foreach (string segment in _englishSegments)
                    if (t.IndexOf(segment, StringComparison.Ordinal) >= 0) { english = true; break; }
                if (!english) english = Fits(_englishShapes, t);
            }
            _seen[t] = english;
            return english;
        }

        public void Look(string frame, Transform root)
        {
            foreach (Text text in root.GetComponentsInChildren<Text>(false))
                if (text.enabled && IsEnglish(text.text))
                    _found.Add(frame + ": " + PathOf(text.transform, root) + " = " + text.text.Replace("\n", "\\n"));
        }

        static string PathOf(Transform t, Transform root)
        {
            string path = t.name;
            for (Transform p = t.parent; p != null && p != root; p = p.parent) path = p.name + "/" + path;
            return path;
        }

        public void Write(string folder)
        {
            File.WriteAllLines(Path.Combine(folder, ReportName), _found);
        }
    }
}

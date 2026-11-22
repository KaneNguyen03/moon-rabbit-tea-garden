using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// Where the carrots come from: one row per brewing station with what it earns a second and the rabbit
    /// and ingredient factors behind it, then the garden-wide multipliers (the seated rabbits' share among them) and the total. Opens from the purse.
    /// Under the list, from garden level 3, the moon jars (#64): what one is worth now and the button that opens one.
    /// </summary>
    public sealed class IncomeView
    {
        readonly GameObject _root;
        readonly RectTransform _card, _content;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly List<GameObject> _rows = new List<GameObject>();
        readonly RectTransform _scroll, _jars;
        readonly ChunkyButton _openJar;
        readonly Text _jarLabel, _jarSub;
        /// <summary>The moon jars' bar under the list, and the room it takes from the list while it shows.</summary>
        public const float JarBarHeight = 170f, ListBottom = 28f;

        /// <summary>Where a jar's carrots start their flight to the purse.</summary>
        public RectTransform JarButton => _openJar.Root;

        public bool IsOpen => _root.activeSelf;

        public IncomeView(RectTransform canvas, GameCatalog catalog, Tweens tweens, System.Action onOpenJar = null)
        {
            _catalog = catalog;
            _tweens = tweens;

            var overlay = UiKit.Rect("Income", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.56f, 0, 0, 980, 900);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 40, -120, 130, 20)
                .AddText(L10n.T("income.title"), 42, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var scrollRt = UiKit.Rect("Scroll", _card).Stretch(0, 0, 1, 1, 0, ListBottom, 0, 130);
            _scroll = scrollRt;
            scrollRt.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            _content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            _content.pivot = new Vector2(0.5f, 1f);
            var list = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset((int)UiKit.CardPad, (int)UiKit.CardPad, 4, 30);
            list.spacing = UiKit.CardGap;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
            scroll.viewport = scrollRt;

            // The moon jars (#64): a jar, what one pays now and how many are on the shelf, and the button that opens one.
            _jars = UiKit.Rect("Jars", _card).Band(UiKit.CardPad, UiKit.CardPad, ListBottom, JarBarHeight - 16f);
            _jars.AddImage(catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.6f), true);
            JarIcon(_jars, catalog, "Icon", 104f, 0f, 0.5f, 72f, 0f);
            _jarSub = UiKit.Rect("Sub", _jars).Stretch(0, 0, 1, 1, 136, 12, 420, 12).AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _jarSub.resizeTextForBestFit = true;
            _jarSub.resizeTextMinSize = 13;
            _jarSub.resizeTextMaxSize = UiKit.TextSmall;
            var open = UiKit.Rect("Open", _jars).Place(1f, 0.5f, -200, 4, 380, 110);
            _openJar = UiKit.Chunky(open, catalog.Panel, () => { _openJar.Press(tweens); onOpenJar?.Invoke(); });
            _jarLabel = UiKit.Rect("Text", _openJar.FaceRect).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _jarLabel.resizeTextForBestFit = true;
            _jarLabel.resizeTextMinSize = 14;
            _jarLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _jars.gameObject.SetActive(false);

            _root.SetActive(false);
        }

        public void Open(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
            Refresh(defs, s, m);
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            if (!IsOpen) return;
            foreach (GameObject go in _rows)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) Object.Destroy(go);
                else Object.DestroyImmediate(go);
            }
            _rows.Clear();

            int shown = 0;
            for (int i = 0; i < defs.Count; i++)
            {
                BigNum perSecond = Economy.StationIncomePerSecond(defs, s, i, out double rabbit, out double ingredients);
                if (perSecond.IsZero) continue;
                shown++;
                var row = UiKit.Rect("Station " + defs[i].Id, _content);
                row.AddImage(_catalog.Panel, Palette.Card, true);
                row.gameObject.AddComponent<LayoutElement>().preferredHeight = 118;
                Sprite prop = _catalog.Stations[i].Prop;
                if (prop != null) UiKit.Rect("Icon", row).Place(0f, 0.5f, 62, 0, 84, 84).AddImage(prop, Color.white).preserveAspect = true;
                Text name = UiKit.Rect("Name", row).Stretch(0, 0.5f, 0.6f, 1, 120, 0, 0, 14)
                    .AddText(L10n.Station(defs[i].Id), UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
                name.resizeTextForBestFit = true;
                name.resizeTextMinSize = 16;
                name.resizeTextMaxSize = UiKit.TextBody;
                Text factors = UiKit.Rect("Factors", row).Stretch(0, 0, 0.6f, 0.5f, 120, 12, 0, 0)
                    .AddText(L10n.T("income.factors", UiKit.Times(rabbit), UiKit.Times(ingredients)), UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
                factors.resizeTextForBestFit = true;
                factors.resizeTextMinSize = 14;
                factors.resizeTextMaxSize = UiKit.TextSmall;
                UiKit.Rect("Amount", row).Stretch(0.6f, 0, 1, 1, 0, 0, 28, 0)
                    .AddText(L10n.T("income.row", perSecond.Short()), UiKit.TextTitle, Palette.CarrotLip, TextAnchor.MiddleRight, FontStyle.Bold);
                _rows.Add(row.gameObject);
            }
            if (shown == 0) AddNote("Empty", L10n.T("income.empty"), Palette.InkSoft);

            // Garden-wide: the seated rabbits' share, charms, speed, the boost, and the total the purse shows.
            bool boosting = s.BoostSecondsLeft > 0d;
            int seated = RestSpots.Seated(s).Count;
            double rest = RestSpots.Multiplier(s);
            if (seated > 0) AddNote("Resting", L10n.T("income.resting", seated, RabbitGridView.Percent(rest - 1d)), Palette.Ink);
            AddNote("Charms", L10n.T("income.charms", UiKit.Times(m.IncomeMultiplier / rest)), Palette.Ink);
            AddNote("Speed", L10n.T("income.speed", UiKit.Times(m.SpeedMultiplier)), Palette.Ink);
            if (boosting) AddNote("Boost", L10n.T("income.boost", UiKit.Times(Energy.BoostSpeed)), Palette.Moss);
            BigNum total = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * (boosting ? Energy.BoostSpeed : 1d));
            AddNote("Total", L10n.T("income.total", total.Short()), Palette.CarrotLip, true);
            RefreshJars(defs, s, m);
        }

        /// <summary>The jars' bar: shown from garden level 3, the button live while there is a jar to open.</summary>
        void RefreshJars(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            bool shown = MoonJars.Shown(s);
            if (_jars.gameObject.activeSelf != shown) _jars.gameObject.SetActive(shown);
            _scroll.offsetMin = new Vector2(_scroll.offsetMin.x, shown ? ListBottom + JarBarHeight : ListBottom);
            if (!shown) return;
            bool any = s.MoonJars > 0;
            _jarSub.text = L10n.T("income.jar.sub", MoonJars.Value(defs, s, m).Short(), s.MoonJars, MoonJars.Cap);
            _jarLabel.text = any ? L10n.T("income.jar", s.MoonJars) : L10n.T("income.jar.none");
            _jarLabel.color = ChunkyButton.InkFor(any);
            _openJar.SetEnabled(any);
        }

        /// <summary>
        /// A moon jar drawn from the game's own shapes (#64): a glass body with a full moon glowing inside and a cork on top,
        /// <paramref name="size"/> tall. Used on the purse, in this card, on the shop's shelf and on quest rewards.
        /// </summary>
        public static RectTransform JarIcon(RectTransform parent, GameCatalog catalog, string name, float size, float anchorX, float anchorY, float x, float y)
        {
            var jar = UiKit.Rect(name, parent).Place(anchorX, anchorY, x, y, size * 0.8f, size);
            UiKit.Rect("Glow", jar).Place(0.5f, 0.42f, 0, 0, size * 1.1f, size * 1.1f).AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            if (catalog.MoonJar != null)
            {
                // The hand-drawn jar, glowing on its own; the code-drawn one below stays for a catalog without it.
                Image art = UiKit.Rect("Art", jar).Stretch(0, 0, 1, 1).AddImage(catalog.MoonJar, Color.white);
                art.preserveAspect = true;
                foreach (Image image in jar.GetComponentsInChildren<Image>()) image.raycastTarget = false;
                return jar;
            }
            UiKit.Rect("Glass", jar).Stretch(0, 0, 1, 0.84f).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Night, 0.22f), true);
            UiKit.Rect("Moon", jar).Place(0.5f, 0.4f, 0, 0, size * 0.44f, size * 0.44f).AddImage(catalog.Round, Palette.Lantern);
            UiKit.Rect("Shine", jar).Place(0.22f, 0.5f, 0, 0, size * 0.1f, size * 0.42f).AddImage(catalog.Round, Palette.Alpha(Color.white, 0.55f));
            UiKit.Rect("Cork", jar).Stretch(0.2f, 0.8f, 0.8f, 1f).AddImage(catalog.Panel, Palette.Mix(Palette.Carrot, Palette.Ink, 0.45f), true);
            foreach (Image image in jar.GetComponentsInChildren<Image>()) image.raycastTarget = false;
            return jar;
        }

        void AddNote(string name, string text, Color color, bool big = false)
        {
            var note = UiKit.Rect(name, _content);
            note.gameObject.AddComponent<LayoutElement>().preferredHeight = big ? 70 : 44;
            note.AddText(text, big ? UiKit.TextTitle : UiKit.TextBody, color, TextAnchor.MiddleLeft, big ? FontStyle.Bold : FontStyle.Normal);
            _rows.Add(note.gameObject);
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class WelcomeBackView
    {
        public const float CardWidth = 1010f, CardHeight = 1310f;
        const float PillTextRight = 36f;
        const float CellWidth = 128f, CellStep = 134f;

        sealed class DayCell
        {
            public Image Face;
            public CanvasGroup Group;
            public GameObject Tick;
        }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action _onCheckIn;
        readonly GameObject _root;
        readonly RectTransform _card, _portraitHolder, _pill;
        readonly Text _sub, _name, _hearts, _line, _head, _claimLabel, _note, _pillText, _pillBadgeText;
        readonly Text _jars, _clovers, _golden;
        readonly ChunkyButton _claim;
        readonly GameObject _pillBadge;
        readonly DayCell[] _days = new DayCell[WelcomeBack.Days];
        GameObject _portrait;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Card => _card;
        public RectTransform Pill => _pill;
        public bool PillShown => _pill.gameObject.activeSelf;
        public string PillText => _pillText.text;
        public bool PillBadge => _pillBadge.activeSelf;
        public string Line => _line.text;
        public string Sub => _sub.text;
        public string ClaimLabel => _claimLabel.text;
        public bool ClaimEnabled => _claim.Button.interactable;
        public string GiftText => _jars.text + " " + _clovers.text + " " + _golden.text;

        public WelcomeBackView(RectTransform canvas, RectTransform hud, GameRunner runner, Tweens tweens, Action onCheckIn)
        {
            _runner = runner;
            _catalog = runner.Catalog;
            _tweens = tweens;
            _onCheckIn = onCheckIn;
            GameCatalog catalog = _catalog;

            _pill = UiKit.Rect("WelcomeBack", hud).Place(1f, 1f, NewcomerView.PillX, NewcomerView.PillY, NewcomerView.PillWidth, NewcomerView.PillHeight);
            UiKit.Touchable(_pill, NewcomerView.PillWidth, NewcomerView.PillHeight);
            _pill.AddShadow(12f, 8f, 0.3f);
            Image pillBack = _pill.AddImage(catalog.Panel, Palette.Mix(Palette.Blush, Palette.Paper, 0.45f), true);
            pillBack.raycastTarget = true;
            var pillButton = _pill.gameObject.AddComponent<Button>();
            pillButton.transition = Selectable.Transition.None;
            pillButton.targetGraphic = pillBack;
            pillButton.onClick.AddListener(() => { Sfx.Click(); Open(); });
            UiKit.Rect("Icon", _pill).Place(0f, 0.5f, 36, 2, 44, 44).AddImage(catalog.GiftBox != null ? catalog.GiftBox : catalog.Round, catalog.GiftBox != null ? Color.white : Palette.Blush).preserveAspect = true;
            _pillText = UiKit.Rect("Text", _pill).Stretch(0, 0, 1, 1, 64, 0, PillTextRight, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var badge = UiKit.Rect("Badge", _pill).Place(1f, 1f, -6, -6, 44, 44);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _pillBadgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("1", UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _pillBadge = badge.gameObject;
            _pillBadge.SetActive(false);
            _pill.gameObject.SetActive(false);

            var overlay = UiKit.Rect("WelcomeBackCard", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, CardWidth, CardHeight);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Blush, 0.1f), true).raycastTarget = true;

            Fit(UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -80, 760, 70)
                .AddText(L10n.T("welcome.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 52, 28);
            _sub = Fit(UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -142, 860, 46)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextBody, 14);

            var rabbit = UiKit.Rect("Rabbit", _card).Place(0.5f, 1f, 0, -310, 920, 250);
            rabbit.AddImage(catalog.Panel, Palette.Card, true);
            UiKit.Rect("Halo", rabbit).Place(0f, 0.5f, 130, 0, 210, 210).AddImage(catalog.Glow, Palette.Alpha(Palette.Blush, 0.55f));
            _portraitHolder = UiKit.Rect("PortraitHolder", rabbit).Place(0f, 0.5f, 130, 0, 190, 190);
            _name = Fit(UiKit.Rect("Name", rabbit).Stretch(0, 1, 1, 1, 250, -64, 30, 16)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextTitle, 16);
            _hearts = Fit(UiKit.Rect("Hearts", rabbit).Stretch(0, 1, 1, 1, 250, -104, 30, 64)
                .AddText("", UiKit.TextBody, Palette.CarrotLip, TextAnchor.MiddleLeft), UiKit.TextBody, 12);
            _line = Fit(UiKit.Rect("Line", rabbit).Stretch(0, 0, 1, 1, 250, 18, 30, 110)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.UpperLeft, FontStyle.Italic), UiKit.TextBody, 14);
            _line.horizontalOverflow = HorizontalWrapMode.Wrap;

            Fit(UiKit.Rect("GiftsHead", _card).Place(0.5f, 1f, 0, -478, 900, 50)
                .AddText(L10n.T("welcome.gifts"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextTitle, 16);
            var gifts = UiKit.Rect("Gifts", _card).Place(0.5f, 1f, 0, -590, 920, 160);
            RectTransform jarTile = GiftTile(gifts, "Jars", -310f, out _jars);
            IncomeView.JarIcon(jarTile, catalog, "Icon", 80f, 0.5f, 1f, 0, -52);
            TileIcon(GiftTile(gifts, "Clovers", 0f, out _clovers), catalog.Clover);
            TileIcon(GiftTile(gifts, "Golden", 310f, out _golden), catalog.GoldenCarrot);

            _head = Fit(UiKit.Rect("Head", _card).Place(0.5f, 1f, 0, -730, 900, 50)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextTitle, 16);
            for (int d = 0; d < WelcomeBack.Days; d++) _days[d] = BuildDay(d + 1);
            Fit(UiKit.Rect("Quests", _card).Place(0.5f, 1f, 0, -1010, 900, 50)
                .AddText(L10n.T("welcome.quests"), UiKit.TextBody, Palette.Carrot, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);

            var claim = UiKit.Rect("Claim", _card).Place(0.5f, 0f, 0, 180, 640, 120);
            _claim = UiKit.Chunky(claim, catalog.Panel, () => { _claim.Press(_tweens); _onCheckIn?.Invoke(); });
            _claimLabel = Fit(UiKit.Rect("Text", _claim.FaceRect).Stretch(0, 0, 1, 1, 20, 0, 20, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);
            _note = Fit(UiKit.Rect("Note", _card).Place(0.5f, 0f, 0, 70, 880, 56)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextBody, 12);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _root.SetActive(false);
        }

        static Text Fit(Text text, int size, int least)
        {
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(size);
            return text;
        }

        RectTransform GiftTile(RectTransform parent, string id, float x, out Text amount)
        {
            var tile = UiKit.Rect(id, parent).Place(0.5f, 0.5f, x, 0, 290, 160);
            tile.AddImage(_catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.6f), true);
            amount = Fit(UiKit.Rect("Amount", tile).Stretch(0, 0, 1, 0, 10, 10, 10, -56)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);
            return tile;
        }

        static void TileIcon(RectTransform tile, Sprite sprite) =>
            UiKit.Rect("Icon", tile).Place(0.5f, 1f, 0, -52, 80, 80).AddImage(sprite, Color.white).preserveAspect = true;

        DayCell BuildDay(int day)
        {
            var rt = UiKit.Rect("Day " + day, _card).Place(0.5f, 1f, (day - 4) * CellStep, -870, CellWidth, 190);
            var cell = new DayCell();
            UiKit.Rect("Lip", rt).Stretch(0, 0, 1, 1).AddImage(_catalog.Panel, Palette.CardLip, true);
            cell.Face = UiKit.Rect("Face", rt).Stretch(0, 0, 1, 1, 0, 6, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            cell.Group = rt.gameObject.AddComponent<CanvasGroup>();
            Fit(UiKit.Rect("Label", rt).Stretch(0, 1, 1, 1, 6, -48, 6, -8)
                .AddText(L10n.T("newcomer.day", day), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 12);
            int i = day - 1;
            string amount;
            if (WelcomeBack.DayPurple[i] > 0)
            {
                UiKit.Rect("Halo", rt).Place(0.5f, 0.5f, 0, -8, 110, 110).AddImage(_catalog.Glow, Palette.Alpha(Palette.Tiers[WelcomeBack.PurpleTier], 0.55f));
                UiKit.Rect("Rabbit", rt).Place(0.5f, 0.5f, 0, -8, 92, 92).AddImage(_catalog.RabbitFor(WelcomeBack.PurpleTier), Color.white).preserveAspect = true;
                amount = L10n.Tier(WelcomeBack.PurpleTier);
            }
            else if (WelcomeBack.DayJars[i] > 0)
            {
                IncomeView.JarIcon(rt, _catalog, "Jar", 64f, 0.5f, 0.5f, 0, -8);
                amount = "+" + WelcomeBack.DayJars[i];
            }
            else if (WelcomeBack.DayClovers[i] > 0)
            {
                UiKit.Rect("Icon", rt).Place(0.5f, 0.5f, 0, -8, 64, 64).AddImage(_catalog.Clover, Color.white).preserveAspect = true;
                amount = "+" + WelcomeBack.DayClovers[i];
            }
            else
            {
                UiKit.Rect("Icon", rt).Place(0.5f, 0.5f, 0, -8, 64, 64).AddImage(_catalog.GoldenCarrot, Color.white).preserveAspect = true;
                amount = "+" + WelcomeBack.DayGolden[i];
            }
            Fit(UiKit.Rect("Amount", rt).Stretch(0, 0, 1, 0, 4, 8, 4, -44)
                .AddText(amount, UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 12);
            var tick = UiKit.Rect("Tick", rt).Place(1f, 0.5f, -24, 24, 40, 40);
            tick.AddImage(_catalog.Round, Palette.Moss);
            UiKit.Rect("Mark", tick).Stretch(0, 0, 1, 1).AddText("✓", 30, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            cell.Tick = tick.gameObject;
            return cell;
        }

        public static string DayGiftOf(int day)
        {
            int i = day - 1;
            if (i < 0 || i >= WelcomeBack.Days) return "";
            if (WelcomeBack.DayPurple[i] > 0) return L10n.T("newcomer.purple");
            if (WelcomeBack.DayJars[i] > 0) return L10n.T("mail.jars", WelcomeBack.DayJars[i]);
            if (WelcomeBack.DayClovers[i] > 0) return L10n.T("mail.clovers", WelcomeBack.DayClovers[i]);
            return L10n.T("mail.golden", WelcomeBack.DayGolden[i]);
        }

        public void Open()
        {
            if (_runner.State == null) return;
            GameState s = _runner.State;
            _note.text = "";
            int away = s.WelcomeBack.AwayDays;
            _sub.text = L10n.T("welcome.sub", away);
            _jars.text = L10n.T("mail.jars", WelcomeBack.InstantJars(away));
            _clovers.text = L10n.T("mail.clovers", WelcomeBack.InstantClovers(away));
            _golden.text = L10n.T("mail.golden", WelcomeBack.InstantGolden(away));
            ShowRabbit(s);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Refresh();
            _tweens.Grow(_card, 0.85f, Motion.Time(0.25f), Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);

        public void ShowClaimed(string text) => _note.text = text;

        public void RefreshPill(bool orderShown)
        {
            if (_runner.State == null) return;
            bool show = Tutorial.IsDone(_runner.State) && _runner.WelcomeBackActive;
            if (_pill.gameObject.activeSelf != show) _pill.gameObject.SetActive(show);
            if (!show)
            {
                if (_root.activeSelf) Close();
                return;
            }
            _pill.anchoredPosition = new Vector2(NewcomerView.PillX, orderShown ? NewcomerView.PillYUnderOrder : NewcomerView.PillY);
            string text = L10n.T("welcome.pill", _runner.WelcomeBackPillDay, WelcomeBack.Days);
            if (_pillText.text != text)
            {
                _pillText.fontSize = UiKit.Scaled(UiKit.TextBody);
                _pillText.text = text;
                _pillText.FitOneLine(NewcomerView.PillWidth - 64f - PillTextRight, 14);
            }
            bool waiting = _runner.WelcomeBackCanCheckIn;
            if (_pillBadge.activeSelf != waiting) _pillBadge.SetActive(waiting);
        }

        public void Refresh()
        {
            GameState s = _runner.State;
            if (s == null || !_root.activeSelf) return;
            bool can = _runner.WelcomeBackCanCheckIn;
            int pillDay = _runner.WelcomeBackPillDay;
            _head.text = L10n.T("welcome.checkin", pillDay, WelcomeBack.Days);
            for (int d = 1; d <= WelcomeBack.Days; d++)
            {
                DayCell cell = _days[d - 1];
                bool done = d <= s.WelcomeBack.CheckIns;
                bool next = can && d == s.WelcomeBack.CheckIns + 1;
                if (cell.Tick.activeSelf != done) cell.Tick.SetActive(done);
                cell.Group.alpha = done || next ? 1f : 0.6f;
                cell.Face.color = next ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.35f) : done ? Palette.Mix(Palette.Moss, Palette.Paper, 0.72f) : Palette.Card;
            }
            _claimLabel.text = can ? L10n.T("welcome.claim", s.WelcomeBack.CheckIns + 1) : L10n.T("welcome.tomorrow");
            _claim.SetEnabled(can);
            _claimLabel.color = ChunkyButton.InkFor(can);
        }

        void ShowRabbit(GameState s)
        {
            Rabbit loved = WelcomeBack.MostLoved(s);
            if (_portrait != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(_portrait); else UnityEngine.Object.DestroyImmediate(_portrait); }
            _portrait = null;
            if (loved == null)
            {
                _name.text = "";
                _hearts.text = "";
                _line.text = L10n.T("welcome.line.none");
                return;
            }
            _portrait = RabbitGridView.Portrait(_portraitHolder, _catalog, s, loved, 0, 0, 190).gameObject;
            _name.text = loved.Name;
            _hearts.text = Rabbits.Hearts(loved.Affection);
            _line.text = L10n.T("welcome.line", s.WelcomeBack.AwayDays);
        }
    }
}

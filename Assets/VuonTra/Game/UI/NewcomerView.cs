using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class NewcomerView
    {
        public const float PillWidth = 330f, PillHeight = 64f;
        public const float PillX = -184f, PillY = -176f, PillYUnderOrder = -244f;
        const float PillTextRight = 36f;

        sealed class DayCell
        {
            public RectTransform Rect;
            public Image Face;
            public CanvasGroup Group;
            public Text Label;
            public GameObject Tick, Dot;
            public Text DotText;
        }

        sealed class TaskRow
        {
            public GameObject Go;
            public Image Face;
            public Text Title, Sub, Count, Locked;
            public RectTransform Fill;
            public GameObject Track, Done;
            public ChunkyButton Claim;
            public Text ClaimLabel;
            public int Slot;
        }

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly GameCatalog _catalog;
        readonly Action<int, int> _onClaim;
        readonly Text _sub, _head, _dayGift, _bigText, _note;
        readonly Image _dayGiftFace, _bigPortrait, _bigFace;
        readonly GameObject _dayGiftTick;
        readonly DayCell[] _days = new DayCell[Newcomer.Days];
        readonly TaskRow[] _rows = new TaskRow[Newcomer.TasksPerDay];
        readonly RectTransform _pill;
        readonly Text _pillText, _pillBadgeText;
        readonly GameObject _pillBadge;
        readonly GameRunner _runner;
        IReadOnlyList<StationDef> _defs;
        GameState _state;
        Modifiers _mods;

        public int Day { get; private set; } = 1;
        public bool IsOpen => _root.activeSelf;
        public RectTransform Root => _card;
        public RectTransform Pill => _pill;
        public bool PillShown => _pill.gameObject.activeSelf;
        public string PillText => _pillText.text;
        public int PillBadge { get; private set; }

        public NewcomerView(RectTransform canvas, RectTransform hud, GameRunner runner, Tweens tweens, Action<int, int> onClaim)
        {
            _runner = runner;
            GameCatalog catalog = runner.Catalog;
            _catalog = catalog;
            _tweens = tweens;
            _onClaim = onClaim;

            _pill = UiKit.Rect("Newcomer", hud).Place(1f, 1f, PillX, PillY, PillWidth, PillHeight);
            UiKit.Touchable(_pill, PillWidth, PillHeight);
            _pill.AddShadow(12f, 8f, 0.3f);
            Image pillBack = _pill.AddImage(catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.35f), true);
            pillBack.raycastTarget = true;
            var pillButton = _pill.gameObject.AddComponent<Button>();
            pillButton.transition = Selectable.Transition.None;
            pillButton.targetGraphic = pillBack;
            pillButton.onClick.AddListener(() => { Sfx.Click(); Open(); });
            UiKit.Rect("Icon", _pill).Place(0f, 0.5f, 36, 2, 44, 44).AddImage(catalog.GiftBox != null ? catalog.GiftBox : catalog.Round, catalog.GiftBox != null ? Color.white : Palette.Carrot).preserveAspect = true;
            _pillText = UiKit.Rect("Text", _pill).Stretch(0, 0, 1, 1, 64, 0, PillTextRight, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var badge = UiKit.Rect("Badge", _pill).Place(1f, 1f, -6, -6, 44, 44);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _pillBadgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("", UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _pillBadge = badge.gameObject;
            _pillBadge.SetActive(false);
            _pill.gameObject.SetActive(false);

            var overlay = UiKit.Rect("NewcomerSheet", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, -20, 1010, 1620);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            Text title = UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -74, 700, 70)
                .AddText(L10n.T("newcomer.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 28;
            title.resizeTextMaxSize = UiKit.Scaled(52);
            _sub = Fit(UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -138, 860, 46)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextSmall, 12);

            for (int d = 0; d < Newcomer.Days; d++) _days[d] = BuildDay(d + 1);

            _head = Fit(UiKit.Rect("Head", _card).Place(0.5f, 1f, 0, -400, 900, 56)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextTitle, 16);

            for (int i = 0; i < Newcomer.TasksPerDay; i++) _rows[i] = BuildRow(i);

            var gift = UiKit.Rect("DayGift", _card).Place(0.5f, 1f, 0, -1220, 920, 96);
            _dayGiftFace = gift.AddImage(catalog.Panel, Palette.Track, true);
            IncomeView.JarIcon(gift, catalog, "Jar", 64f, 0f, 0.5f, 52, 2);
            _dayGift = Fit(UiKit.Rect("Text", gift).Stretch(0, 0, 1, 1, 100, 0, 110, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextBody, 14);
            var tick = UiKit.Rect("Tick", gift).Place(1f, 0.5f, -56, 0, 60, 60);
            tick.AddImage(catalog.Round, Palette.Moss);
            UiKit.Rect("Mark", tick).Stretch(0, 0, 1, 1).AddText("✓", 38, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dayGiftTick = tick.gameObject;

            var big = UiKit.Rect("BigGift", _card).Place(0.5f, 1f, 0, -1420, 920, 230);
            big.AddShadow(14f, 8f, 0.25f);
            _bigFace = big.AddImage(catalog.Panel, Palette.Mix(Palette.Tiers[Newcomer.PurpleTier], Palette.Paper, 0.72f), true);
            UiKit.Rect("Halo", big).Place(0f, 0.5f, 120, 0, 210, 210).AddImage(catalog.Glow, Palette.Alpha(Palette.Tiers[Newcomer.PurpleTier], 0.6f));
            _bigPortrait = UiKit.Rect("Portrait", big).Place(0f, 0.5f, 120, 0, 180, 180).AddImage(catalog.RabbitFor(Newcomer.PurpleTier), Color.white);
            _bigPortrait.preserveAspect = true;
            UiKit.Rect("Head", big).Stretch(0, 0.62f, 1, 1, 240, 0, 30, 18)
                .AddText(L10n.T("newcomer.big"), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _bigText = Fit(UiKit.Rect("Text", big).Stretch(0, 0, 1, 0.62f, 240, 18, 30, 4)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.UpperLeft), UiKit.TextBody, 14);

            _note = Fit(UiKit.Rect("Note", _card).Place(0.5f, 0f, 0, 62, 900, 50)
                .AddText("", UiKit.TextBody, Palette.Carrot, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);

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
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(size);
            return text;
        }

        DayCell BuildDay(int day)
        {
            const float width = 128f, step = 134f;
            var rt = UiKit.Rect("Day " + day, _card).Place(0.5f, 1f, (day - 4) * step, -260, width, 170);
            var cell = new DayCell { Rect = rt };
            UiKit.Rect("Lip", rt).Stretch(0, 0, 1, 1).AddImage(_catalog.Panel, Palette.CardLip, true);
            cell.Face = UiKit.Rect("Face", rt).Stretch(0, 0, 1, 1, 0, 6, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            cell.Face.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = cell.Face;
            button.onClick.AddListener(() => { Sfx.Click(); Select(day); });
            cell.Group = rt.gameObject.AddComponent<CanvasGroup>();
            cell.Label = Fit(UiKit.Rect("Label", rt).Stretch(0, 1, 1, 1, 6, -50, 6, -8)
                .AddText(L10n.T("newcomer.day", day), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 12);
            if (day == Newcomer.Days)
            {
                UiKit.Rect("Halo", rt).Place(0.5f, 0.5f, 0, -18, 110, 110).AddImage(_catalog.Glow, Palette.Alpha(Palette.Tiers[Newcomer.PurpleTier], 0.55f));
                UiKit.Rect("Rabbit", rt).Place(0.5f, 0.5f, 0, -18, 92, 92).AddImage(_catalog.RabbitFor(Newcomer.PurpleTier), Color.white).preserveAspect = true;
            }
            else IncomeView.JarIcon(rt, _catalog, "Jar", 70f, 0.5f, 0.5f, 0, -18);
            var tick = UiKit.Rect("Tick", rt).Place(1f, 0f, -24, 26, 42, 42);
            tick.AddImage(_catalog.Round, Palette.Moss);
            UiKit.Rect("Mark", tick).Stretch(0, 0, 1, 1).AddText("✓", 30, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            cell.Tick = tick.gameObject;
            var dot = UiKit.Rect("Dot", rt).Place(0f, 0f, 24, 26, 42, 42);
            dot.AddImage(_catalog.Round, Palette.CarrotLip);
            cell.DotText = UiKit.Rect("Text", dot).Stretch(0, 0, 1, 1).AddText("", UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            cell.Dot = dot.gameObject;
            return cell;
        }

        TaskRow BuildRow(int slot)
        {
            var plate = UiKit.Rect("Task " + slot, _card).Place(0.5f, 1f, 0, -520 - slot * 172, 920, 160);
            var row = new TaskRow { Go = plate.gameObject, Slot = slot };
            plate.AddImage(_catalog.Panel, Palette.CardLip, true);
            row.Face = UiKit.Rect("Face", plate).Stretch(0, 0, 1, 1, 0, 6, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);
            row.Title = Fit(UiKit.Rect("Title", plate).Stretch(0, 0.6f, 1, 1, 30, 0, 280, 10)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold), UiKit.TextBody, 14);
            row.Sub = Fit(UiKit.Rect("Sub", plate).Stretch(0, 0.36f, 1, 0.6f, 30, 0, 280, 0)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft), UiKit.TextSmall, 12);
            var track = UiKit.Rect("Track", plate).Band(30, 420, 26, 16);
            track.AddImage(_catalog.Panel, Palette.Track, true);
            row.Track = track.gameObject;
            row.Fill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            row.Fill.AddImage(_catalog.Panel, Palette.Moss, true);
            row.Count = Fit(UiKit.Rect("Count", plate).Place(1f, 0f, -350, 34, 120, 40)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold), UiKit.TextSmall, 12);

            var claim = UiKit.Rect("Claim", plate).Place(1f, 0.5f, -136, 3, 220, 104);
            ChunkyButton button = null;
            button = UiKit.Chunky(claim, _catalog.Panel, () =>
            {
                button.Press(_tweens);
                _onClaim?.Invoke(Day, row.Slot);
            });
            row.Claim = button;
            row.ClaimLabel = Fit(UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0)
                .AddText(L10n.T("quest.claim"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);

            var done = UiKit.Rect("Done", plate).Place(1f, 0.5f, -136, 3, 220, 60);
            done.AddImage(_catalog.Panel, Palette.Track, true);
            Fit(UiKit.Rect("Text", done).Stretch(0, 0, 1, 1, 8, 0, 8, 0)
                .AddText(L10n.T("quest.claimed"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);
            row.Done = done.gameObject;

            row.Locked = Fit(UiKit.Rect("Locked", plate).Place(1f, 0.5f, -136, 3, 230, 90)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 12);
            return row;
        }

        public static int OpenDay(GameState s)
        {
            int today = Newcomer.PillDay(s);
            for (int day = 1; day <= today; day++)
                for (int slot = 0; slot < Newcomer.TasksPerDay; slot++)
                    if (!Newcomer.IsClaimed(Newcomer.At(day, slot), s)) return day;
            return today;
        }

        void Read()
        {
            _defs = _runner.Defs;
            _state = _runner.State;
            _mods = _runner.Mods;
        }

        public void Open()
        {
            Read();
            if (_state == null) return;
            Day = OpenDay(_state);
            _note.text = "";
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
            Refresh();
        }

        public void Select(int day)
        {
            Day = Mathf.Clamp(day, 1, Newcomer.Days);
            _note.text = "";
            Refresh();
        }

        public void ShowClaimed(string text) => _note.text = text;

        public void Close() => _root.SetActive(false);

        public void RefreshPill(bool orderShown)
        {
            Read();
            if (_state == null) return;
            bool show = Newcomer.ShowPill(_state);
            if (_pill.gameObject.activeSelf != show) _pill.gameObject.SetActive(show);
            if (!show)
            {
                if (_root.activeSelf) Close();
                return;
            }
            _pill.anchoredPosition = new Vector2(PillX, orderShown ? PillYUnderOrder : PillY);
            string pill = L10n.T("newcomer.pill", Newcomer.PillDay(_state));
            if (_pillText.text != pill)
            {
                _pillText.fontSize = UiKit.Scaled(UiKit.TextBody);
                _pillText.text = pill;
                _pillText.FitOneLine(PillWidth - 64f - PillTextRight, 14);
            }
            PillBadge = Newcomer.Claimable(_defs, _state);
            if (_pillBadge.activeSelf != PillBadge > 0) _pillBadge.SetActive(PillBadge > 0);
            _pillBadgeText.text = PillBadge.ToString();
        }

        public static string TaskTitle(NewcomerTask task, GameState s)
        {
            NewcomerTask shown = Newcomer.Shown(task, s);
            return L10n.T("newcomer.task." + shown.Id, (int)shown.Target);
        }

        public static string RewardOf(IReadOnlyList<StationDef> defs, GameState s, Modifiers m, int day)
        {
            var parts = new List<string>(3) { L10n.T("newcomer.carrots", Newcomer.TaskCarrots(defs, s, m).Short()) };
            if (Newcomer.TaskClovers[day - 1] > 0) parts.Add(L10n.T("mail.clovers", Newcomer.TaskClovers[day - 1]));
            if (Newcomer.TaskGolden[day - 1] > 0) parts.Add(L10n.T("mail.golden", Newcomer.TaskGolden[day - 1]));
            return string.Join(" · ", parts);
        }

        public static string DayGiftOf(int day, bool withRabbit = true)
        {
            var parts = new List<string>(3);
            if (withRabbit && Newcomer.DayPurple[day - 1] > 0) parts.Add(L10n.T("newcomer.purple"));
            if (Newcomer.DayJars[day - 1] > 0) parts.Add(L10n.T("newcomer.jars", Newcomer.DayJars[day - 1]));
            if (Newcomer.DayGolden[day - 1] > 0) parts.Add(L10n.T("mail.golden", Newcomer.DayGolden[day - 1]));
            return string.Join(" + ", parts);
        }

        public void Refresh()
        {
            Read();
            if (_state == null || !_root.activeSelf) return;
            GameState s = _state;
            int today = Newcomer.DayNow(s);
            _sub.text = L10n.T("newcomer.sub", Newcomer.MailAfterDays);
            for (int d = 1; d <= Newcomer.Days; d++)
            {
                DayCell cell = _days[d - 1];
                bool open = Newcomer.DayOpen(s, d);
                bool paid = Newcomer.DayPaid(s, d);
                int ready = 0;
                for (int slot = 0; slot < Newcomer.TasksPerDay; slot++) if (Newcomer.CanClaim(Newcomer.At(d, slot), _defs, s)) ready++;
                cell.Group.alpha = open ? 1f : 0.55f;
                cell.Face.color = d == Day ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.35f) : (d == today ? Palette.Mix(Palette.Lantern, Palette.Card, 0.75f) : Palette.Card);
                cell.Label.text = L10n.T("newcomer.day", d);
                if (cell.Tick.activeSelf != paid) cell.Tick.SetActive(paid);
                if (cell.Dot.activeSelf != ready > 0) cell.Dot.SetActive(ready > 0);
                cell.DotText.text = ready.ToString();
            }

            bool dayOpen = Newcomer.DayOpen(s, Day);
            int claimed = 0;
            for (int slot = 0; slot < Newcomer.TasksPerDay; slot++) if (Newcomer.IsClaimed(Newcomer.At(Day, slot), s)) claimed++;
            _head.text = L10n.T("newcomer.day", Day) + " · " + (dayOpen ? L10n.T("newcomer.done", claimed, Newcomer.TasksPerDay) : L10n.T("newcomer.opens", Newcomer.DaysUntil(s, Day)));

            for (int slot = 0; slot < Newcomer.TasksPerDay; slot++) RefreshRow(_rows[slot], Newcomer.At(Day, slot), dayOpen);

            bool dayPaid = Newcomer.DayPaid(s, Day);
            _dayGift.text = L10n.T("newcomer.daygift", DayGiftOf(Day));
            if (_dayGiftTick.activeSelf != dayPaid) _dayGiftTick.SetActive(dayPaid);
            _dayGiftFace.color = dayPaid ? Palette.Mix(Palette.Moss, Palette.Paper, 0.7f) : Palette.Track;

            string kind = Newcomer.PurpleKind(s, Newcomer.ShownKindRoll);
            Sprite face = _catalog.KindSprite(kind);
            if (face != null) _bigPortrait.sprite = face;
            _bigText.text = L10n.T("newcomer.big.sub", L10n.T("kind." + kind), DayGiftOf(Newcomer.Days, false));
            bool bigPaid = Newcomer.DayPaid(s, Newcomer.Days);
            _bigFace.color = bigPaid ? Palette.Mix(Palette.Moss, Palette.Paper, 0.7f) : Palette.Mix(Palette.Tiers[Newcomer.PurpleTier], Palette.Paper, 0.72f);
        }

        void RefreshRow(TaskRow row, NewcomerTask task, bool dayOpen)
        {
            GameState s = _state;
            bool claimed = Newcomer.IsClaimed(task, s);
            bool can = Newcomer.CanClaim(task, _defs, s);
            double target = Newcomer.TargetOf(task, s);
            double progress = Math.Min(Newcomer.Progress(task, _defs, s), target);
            row.Title.text = TaskTitle(task, s);
            row.Sub.text = Newcomer.IsFallback(task, s) && !claimed
                ? L10n.T("newcomer.fallback", L10n.T("newcomer.task." + task.Id, (int)task.Target), task.NeedLevel)
                : RewardOf(_defs, s, _mods, task.Day);
            float fraction = claimed ? 1f : (target > 0d ? (float)(progress / target) : 0f);
            row.Fill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
            row.Count.text = new BigNum(progress).Short() + " / " + new BigNum(target).Short();
            bool locked = !dayOpen && !claimed;
            bool showTrack = !claimed && !can;
            if (row.Track.activeSelf != showTrack) row.Track.SetActive(showTrack);
            if (row.Count.gameObject.activeSelf != showTrack) row.Count.gameObject.SetActive(showTrack);
            if (row.Claim.Root.gameObject.activeSelf != can) row.Claim.Root.gameObject.SetActive(can);
            row.Claim.SetEnabled(can);
            row.ClaimLabel.color = ChunkyButton.InkFor(can);
            if (row.Done.activeSelf != claimed) row.Done.SetActive(claimed);
            row.Locked.text = locked ? L10n.T("newcomer.opens", Newcomer.DaysUntil(s, task.Day)) : "";
            if (row.Locked.gameObject.activeSelf != locked) row.Locked.gameObject.SetActive(locked);
            row.Face.color = can ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.55f)
                : claimed ? Palette.Mix(Palette.Card, Palette.CardLip, 0.45f)
                : locked ? Palette.Mix(Palette.Card, Palette.Track, 0.6f) : Palette.Card;
        }
    }
}

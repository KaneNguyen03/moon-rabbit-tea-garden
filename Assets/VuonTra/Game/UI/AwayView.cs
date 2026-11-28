using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class StoryRowView
    {
        public RectTransform Root;
        public Story Story;
        public Text Text, Label;
        public ChunkyButton Button;
        public bool Done;
    }

    public sealed class AwayView
    {
        public const float Height = 1180f;
        public const float CountSeconds = 0.8f;
        public const int MaxTicks = 12;
        public const float StoryTop = -440f, StoryHead = 64f, StoryRow = 150f, StoryPad = 14f, StoryGap = 20f, StoryPortrait = 118f;
        public const float StoryButtonWidth = 200f, StoryButtonPad = 22f;
        public const float PairScale = 0.52f, PairOffset = 31f, PairLift = 12f;
        public const char KeepTogether = '\u00A0';

        public static float StoriesHeight(int count) => count <= 0 ? 0f : StoryHead + count * StoryRow + StoryPad;

        public static float StoriesShift(int count) => count <= 0 ? 0f : StoriesHeight(count) + StoryGap;

        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Func<int> _doubleCost;
        readonly Func<int> _golden;
        readonly Func<bool> _doubled;
        readonly Action _onCollect;
        readonly Action _onDouble;
        readonly Action<Story, RectTransform> _onStory;
        readonly RectTransform _stories;
        readonly RectTransform[] _shifted;
        readonly float[] _shiftedTop, _shiftedBottom;
        readonly List<StoryRowView> _storyRows = new List<StoryRowView>();
        GameState _state;
        readonly Text _amount, _time, _tray, _guests, _rabbit, _doubleLabel;
        readonly GameObject _rabbitRow;
        readonly RectTransform _portraitHolder;
        readonly ChunkyButton _double;
        GameObject _portrait;
        int _countToken;

        public BottomSheet Sheet { get; }
        public AwayReport Report { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        public string DoubleLabel => _doubleLabel.text;
        public bool DoubleEnabled => _double.Button.interactable;
        public RectTransform CarrotIcon { get; }
        public int Ticks { get; private set; }
        public RectTransform Stories => _stories;
        public IReadOnlyList<StoryRowView> StoryRows => _storyRows;
        public float SheetHeight { get; private set; } = Height;

        public AwayView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Func<int> doubleCost, Func<int> golden, Func<bool> doubled,
            Action onCollect, Action onDouble, Action<Story, RectTransform> onStory = null)
        {
            _onStory = onStory;
            _catalog = catalog;
            _tweens = tweens;
            _doubleCost = doubleCost;
            _golden = golden;
            _doubled = doubled;
            _onCollect = onCollect;
            _onDouble = onDouble;
            Sheet = new BottomSheet(canvas, "Away", catalog, tweens, Height, null);
            RectTransform card = Sheet.Card;

            UiKit.Rect("Title", card).Stretch(0, 1, 1, 1, 40, -150, 40, 60)
                .AddText(L10n.T("away.title"), 54, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var carrots = Row(card, "Carrots", -310, 220);
            CarrotIcon = UiKit.Rect("Icon", carrots).Place(0f, 0.5f, 90, 10, 110, 110);
            CarrotIcon.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _amount = UiKit.Rect("Amount", carrots).Stretch(0, 0.42f, 1, 1, 170, 0, 30, 10)
                .AddText("+0", 72, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _amount.resizeTextForBestFit = true;
            _amount.resizeTextMinSize = 30;
            _amount.resizeTextMaxSize = UiKit.Scaled(72);
            _time = UiKit.Rect("Time", carrots).Stretch(0, 0, 1, 0.44f, 170, 14, 30, 0)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperLeft);
            _time.resizeTextForBestFit = true;
            _time.resizeTextMinSize = 16;
            _time.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            StationData teapot = catalog.Stations.Length > 0 ? catalog.Stations[0] : null;
            Sprite product = teapot == null ? null : (teapot.Product != null ? teapot.Product : teapot.Prop);
            _tray = Line(card, "Tray", -500, product, Color.white);
            _guests = Line(card, "Guests", -640, catalog.Rabbit, Color.white);
            var rabbit = Row(card, "Rabbit", -800, 150);
            _rabbitRow = rabbit.gameObject;
            _portraitHolder = UiKit.Rect("PortraitHolder", rabbit).Place(0f, 0.5f, 90, 0, 130, 130);
            _rabbit = UiKit.Rect("Text", rabbit).Stretch(0, 0, 1, 1, 170, 0, 30, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _rabbit.resizeTextForBestFit = true;
            _rabbit.resizeTextMinSize = 16;
            _rabbit.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            _stories = UiKit.Rect("Stories", card).Stretch(0, 1, 1, 1, 40, StoryTop, 40, -StoryTop);
            _stories.AddImage(catalog.Panel, Palette.Mix(Palette.Card, Palette.Night, 0.07f), true);
            Text head = UiKit.Rect("Head", _stories).Stretch(0, 1, 1, 1, 30, -StoryHead, 30, 0)
                .AddText(L10n.T("overnight.title"), UiKit.TextBody, Palette.Night, TextAnchor.MiddleLeft, FontStyle.BoldAndItalic);
            head.resizeTextForBestFit = true;
            head.resizeTextMinSize = 16;
            head.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _stories.gameObject.SetActive(false);
            _shifted = new[] { (RectTransform)_tray.transform.parent, (RectTransform)_guests.transform.parent, rabbit };
            _shiftedTop = new float[_shifted.Length];
            _shiftedBottom = new float[_shifted.Length];
            for (int i = 0; i < _shifted.Length; i++)
            {
                _shiftedTop[i] = _shifted[i].offsetMax.y;
                _shiftedBottom[i] = _shifted[i].offsetMin.y;
            }

            var ok = UiKit.Rect("Ok", card).Place(0f, 0f, 220, 150, 320, UiKit.MinTouch + 20);
            UiKit.Secondary(ok, catalog.Panel, L10n.T("away.ok"), () =>
            {
                ShowAmount(Report.Paid);
                Sfx.Reward();
                _onCollect?.Invoke();
                Close();
            }, UiKit.TextTitle);
            var twice = UiKit.Rect("Double", card).Place(1f, 0f, -300, 150, 520, 130);
            _double = UiKit.Chunky(twice, catalog.Panel, () =>
            {
                _double.Press(_tweens);
                _onDouble?.Invoke();
            });
            _doubleLabel = UiKit.Rect("Text", _double.FaceRect).Stretch(0, 0, 1, 1, 16, 0, 16, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _doubleLabel.resizeTextForBestFit = true;
            _doubleLabel.resizeTextMinSize = 16;
            _doubleLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);
        }

        RectTransform Row(RectTransform card, string id, float y, float height)
        {
            var row = UiKit.Rect(id, card).Stretch(0, 1, 1, 1, 40, y - height / 2f, 40, -(y + height / 2f));
            row.AddImage(_catalog.Panel, Palette.Card, true);
            return row;
        }

        Text Line(RectTransform card, string id, float y, Sprite icon, Color tint)
        {
            var row = Row(card, id, y, 120);
            if (icon != null) UiKit.Rect("Icon", row).Place(0f, 0.5f, 90, 0, 80, 80).AddImage(icon, tint).preserveAspect = true;
            Text text = UiKit.Rect("Text", row).Stretch(0, 0, 1, 1, 170, 0, 30, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 16;
            text.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            return text;
        }

        public void Open(AwayReport away, GameState s)
        {
            Report = away;
            _state = s;
            LayStories(s != null ? s.Overnight.Waiting : null);
            CountUp(away.Paid);
            _time.text = TimeLine(away, s);
            _tray.text = away.TrayAdded > 0
                ? L10n.T("away.tray", away.TrayAdded) + (away.TrayFull ? L10n.T("away.tray.full") : "")
                : L10n.T("away.tray.none");
            _guests.text = away.GuestsMissed > 0 ? L10n.T("away.guests", away.GuestsMissed) : L10n.T("away.guests.none");

            Rabbit sleepy = away.SleepiestId > 0 ? Rabbits.Find(s, away.SleepiestId) : null;
            _rabbitRow.SetActive(sleepy != null);
            if (_portrait != null) { if (Application.isPlaying) UnityEngine.Object.Destroy(_portrait); else UnityEngine.Object.DestroyImmediate(_portrait); }
            _portrait = null;
            if (sleepy != null)
            {
                RectTransform tile = RabbitGridView.Portrait(_portraitHolder, _catalog, s, sleepy, 0, 0, 130);
                _portrait = tile.gameObject;
                _rabbit.text = L10n.T("away.rabbit", sleepy.Name);
                if (!Motion.Reduced)
                    _tweens.Add(Motion.Yawn, t =>
                    {
                        if (tile == null) return;
                        float k = Mathf.Sin(t * Mathf.PI);
                        tile.localScale = new Vector3(1f - 0.04f * k, 1f + 0.09f * k, 1f);
                    });
            }
            RefreshDouble();
            Sheet.Root.transform.SetAsLastSibling();
            Sheet.Open();
        }

        void CountUp(BigNum paid)
        {
            if (Motion.Reduced) { ShowAmount(paid); return; }
            int token = ++_countToken, reached = 0;
            Ticks = 0;
            _amount.text = "+0";
            _tweens.Add(CountSeconds, t =>
            {
                if (token != _countToken || _amount == null) return;
                _amount.text = "+" + (t >= 1f ? paid : paid * t).Short();
                int step = Mathf.Min(MaxTicks, Mathf.FloorToInt(t * MaxTicks));
                if (step <= reached) return;
                reached = step;
                Ticks++;
                Sfx.Tick();
            }, Ease.OutCubic);
        }

        void ShowAmount(BigNum paid)
        {
            _countToken++;
            _amount.text = "+" + paid.Short();
        }

        static string TimeLine(AwayReport away, GameState s)
        {
            if (!away.Capped) return L10n.T("away.time", GardenScreen.Span(away.Seconds));
            int hours = (int)Math.Round(away.Seconds / 3600d);
            bool moon = s != null && MoonPass.Active(s, s.MaxDaySeen);
            return L10n.T(moon ? "away.time.capped.moon" : "away.time.capped", hours);
        }

        public void RefreshDouble()
        {
            bool done = _doubled != null && _doubled();
            int cost = _doubleCost != null ? _doubleCost() : 0;
            int golden = _golden != null ? _golden() : 0;
            bool can = !done && golden >= cost;
            _doubleLabel.text = done ? L10n.T("away.double.done")
                : cost == 0 ? L10n.T("away.double.free")
                : golden >= cost ? L10n.T("away.double.cost", cost)
                : L10n.T("away.double.short", cost - golden);
            _double.SetEnabled(can);
            _double.SetShort(!done && golden < cost);
            _doubleLabel.color = ChunkyButton.InkFor(can);
            Ad?.Refresh();
        }

        public void Close() => Sheet.Close();

        void LayStories(IReadOnlyList<Story> waiting)
        {
            foreach (StoryRowView row in _storyRows) Drop(row.Root.gameObject);
            _storyRows.Clear();
            int count = waiting != null ? Math.Min(waiting.Count, Overnight.MaxWaiting) : 0;
            float height = StoriesHeight(count), shift = StoriesShift(count);
            _stories.gameObject.SetActive(count > 0);
            _stories.offsetMin = new Vector2(_stories.offsetMin.x, StoryTop - height);
            for (int i = 0; i < _shifted.Length; i++)
            {
                _shifted[i].offsetMax = new Vector2(_shifted[i].offsetMax.x, _shiftedTop[i] - shift);
                _shifted[i].offsetMin = new Vector2(_shifted[i].offsetMin.x, _shiftedBottom[i] - shift);
            }
            SheetHeight = Height + shift;
            Sheet.Resize(SheetHeight);
            for (int i = 0; i < count; i++) _storyRows.Add(BuildStoryRow(waiting[i], i));
        }

        StoryRowView BuildStoryRow(Story story, int index)
        {
            float top = StoryHead + index * StoryRow;
            var row = UiKit.Rect("Story" + index, _stories).Stretch(0, 1, 1, 1, 16, -(top + StoryRow), 16, top);
            var view = new StoryRowView { Root = row, Story = story };
            var holder = UiKit.Rect("Portrait", row).Place(0f, 0.5f, 14f + StoryPortrait / 2f, 0, StoryPortrait, StoryPortrait);
            Portrait(holder, story);
            view.Text = UiKit.Rect("Text", row).Stretch(0, 0, 1, 1, 28f + StoryPortrait, 8, StoryButtonWidth + 24f, 8)
                .AddText(Line(story, _state), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            view.Text.resizeTextForBestFit = true;
            view.Text.resizeTextMinSize = 16;
            view.Text.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            var take = UiKit.Rect("Take", row).Place(1f, 0.5f, -(StoryButtonWidth / 2f + 6f), 0, StoryButtonWidth, UiKit.MinTouch + 12f);
            view.Button = UiKit.Chunky(take, _catalog.Panel, () =>
            {
                if (view.Done) return;
                view.Button.Press(_tweens);
                _onStory?.Invoke(story, row);
            });
            view.Label = UiKit.Rect("Text", view.Button.FaceRect).Stretch(0, 0, 1, 1, StoryButtonPad, 0, StoryButtonPad, 0)
                .AddText(L10n.T(ButtonKey(story.Kind)), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            view.Label.resizeTextForBestFit = true;
            view.Label.resizeTextMinSize = 16;
            view.Label.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);
            return view;
        }

        void Portrait(RectTransform holder, Story story)
        {
            Rabbit first = _state != null && story.Rabbit > 0 ? Rabbits.Find(_state, story.Rabbit) : null;
            Rabbit friend = _state != null && story.Friend > 0 ? Rabbits.Find(_state, story.Friend) : null;
            if (story.Kind == StoryKind.Bond && first != null && friend != null)
            {
                RabbitGridView.Portrait(holder, _catalog, _state, friend, PairOffset, PairLift, StoryPortrait * PairScale);
                RabbitGridView.Portrait(holder, _catalog, _state, first, -PairOffset, -PairLift, StoryPortrait * PairScale);
                return;
            }
            if (first != null)
            {
                RabbitGridView.Portrait(holder, _catalog, _state, first, 0, 0, StoryPortrait);
                return;
            }
            holder.AddImage(_catalog.Panel, Palette.Card, true);
            Sprite art = story.Kind == StoryKind.Clover ? _catalog.Clover : story.Kind == StoryKind.Stranger ? _catalog.KindSprite(story.Guest) : _catalog.Rabbit;
            if (art == null) art = _catalog.Round;
            UiKit.Rect("Art", holder).Stretch(0, 0, 1, 1, 10, 10, 10, 10).AddImage(art, Color.white).preserveAspect = true;
        }

        public static string ButtonKey(StoryKind kind) => kind == StoryKind.Stranger ? "overnight.welcome" : kind == StoryKind.Doze ? "overnight.pat" : "overnight.take";

        public static string Line(Story story, GameState s)
        {
            Rabbit first = s != null && story.Rabbit > 0 ? Rabbits.Find(s, story.Rabbit) : null;
            Rabbit friend = s != null && story.Friend > 0 ? Rabbits.Find(s, story.Friend) : null;
            string name = first != null ? first.Name : L10n.T("overnight.someone");
            switch (story.Kind)
            {
                case StoryKind.Leaves: return L10n.T("overnight.leaves", name, story.Amount);
                case StoryKind.Clover: return L10n.T("overnight.clover");
                case StoryKind.Stranger: return L10n.T("overnight.stranger", L10n.T("kind." + story.Guest).Replace(' ', KeepTogether));
                case StoryKind.Bond: return L10n.T("overnight.bond", name, friend != null ? friend.Name : L10n.T("overnight.someone"));
                default: return L10n.T(DozeKey(first != null ? Rabbits.TradeOf(first) : RabbitTrade.None, story.Line), name);
            }
        }

        static readonly string[,] DozeKeys =
        {
            { "overnight.doze.brewer.0", "overnight.doze.brewer.1" },
            { "overnight.doze.baker.0", "overnight.doze.baker.1" },
            { "overnight.doze.gatherer.0", "overnight.doze.gatherer.1" },
            { "overnight.doze.stargazer.0", "overnight.doze.stargazer.1" },
        };

        public static string DozeKey(RabbitTrade trade, int line)
        {
            int row = trade == RabbitTrade.Baker ? 1 : trade == RabbitTrade.Gatherer ? 2 : trade == RabbitTrade.Stargazer ? 3 : 0;
            return DozeKeys[row, Math.Max(0, Math.Min(DozeKeys.GetLength(1) - 1, line))];
        }

        public StoryRowView RowOf(Story story) => _storyRows.Find(r => r.Story == story);

        public void MarkTaken(Story story)
        {
            StoryRowView row = RowOf(story);
            if (row == null) return;
            row.Done = true;
            row.Button.SetEnabled(false);
            row.Label.text = L10n.T("overnight.done");
            row.Label.color = ChunkyButton.InkFor(false);
        }

        static void Drop(GameObject go)
        {
            if (go == null) return;
            if (Application.isPlaying) UnityEngine.Object.Destroy(go);
            else UnityEngine.Object.DestroyImmediate(go);
        }

        public AdButton Ad { get; private set; }

        public AdButton AttachAd(Func<AdOffer> offer, Func<bool> busy, Action onWatch)
        {
            var rt = UiKit.Rect("Ad", Sheet.Card).Place(1f, 0f, -300, 259, 520, UiKit.MinTouch);
            Ad = new AdButton(rt, _catalog, "ad.away", "ad.away.now", offer, busy, onWatch);
            return Ad;
        }
    }
}

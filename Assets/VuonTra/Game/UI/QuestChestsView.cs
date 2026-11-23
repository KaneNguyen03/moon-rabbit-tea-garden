using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class QuestChestsView
    {
        public const float TodayHeight = 132f, WeekHeight = 232f;
        const float ShakeEvery = 1.6f;
        const float TrackLeft = 36f, TrackRight = 60f, TrackY = 96f;

        public static readonly Color[] ChestTints =
        {
            Palette.Mix(Palette.Tiers[1], Color.white, 0.25f),
            Palette.Mix(Palette.Tiers[2], Color.white, 0.25f),
            Palette.Mix(Palette.Tiers[3], Palette.Lantern, 0.35f),
        };

        readonly Tweens _tweens;
        readonly Image _todayFace, _boxLip;
        readonly Text _todayLabel, _todaySub, _weekLabel, _weekSub;
        readonly RectTransform _box, _boxArt, _weekFill;
        readonly RectTransform[] _chests = new RectTransform[QuestChests.Chests.Length];
        readonly RectTransform[] _chestArt = new RectTransform[QuestChests.Chests.Length];
        readonly GameObject[] _chestBadges = new GameObject[QuestChests.Chests.Length];
        readonly GameObject[] _chestTicks = new GameObject[QuestChests.Chests.Length];
        readonly GameObject _boxBadge;
        float _sinceShake;
        bool _shaking;

        public RectTransform TodayRow { get; }
        public RectTransform WeekRow { get; }
        public RectTransform Box => _box;
        public bool BoxReady { get; private set; }
        public RectTransform Chest(int i) => _chests[i];

        public QuestChestsView(RectTransform list, GameCatalog catalog, Tweens tweens, Action onBox, Action<int> onChest)
        {
            _tweens = tweens;
            Sprite gift = catalog.GiftBox != null ? catalog.GiftBox : catalog.Round;

            TodayRow = UiKit.Rect("QuestBoxRow", list);
            TodayRow.AddImage(catalog.Panel, Palette.CardLip, true);
            var todayLayout = TodayRow.gameObject.AddComponent<LayoutElement>();
            todayLayout.preferredHeight = todayLayout.minHeight = TodayHeight;
            _todayFace = UiKit.Rect("Card", TodayRow).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Card, true);
            _todayLabel = UiKit.Rect("Title", TodayRow).Stretch(0, 0.5f, 1, 1, 36, 0, 200, 10)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            OneLine(_todayLabel, UiKit.TextTitle);
            _todaySub = UiKit.Rect("Sub", TodayRow).Stretch(0, 0, 1, 0.5f, 36, 14, 200, 4)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
            _todaySub.resizeTextForBestFit = true;
            _todaySub.resizeTextMinSize = 12;
            _todaySub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            _box = UiKit.Rect("Box", TodayRow).Place(1f, 0.5f, -100, 4, 120, 108);
            _boxLip = _box.AddImage(catalog.Round, Palette.Alpha(Palette.Lantern, 0f));
            _boxLip.raycastTarget = true;
            var boxButton = _box.gameObject.AddComponent<Button>();
            boxButton.transition = Selectable.Transition.None;
            boxButton.targetGraphic = _boxLip;
            boxButton.onClick.AddListener(() => onBox?.Invoke());
            _boxArt = UiKit.Rect("Art", _box).Place(0.5f, 0.5f, 0, 0, 96, 96);
            _boxArt.AddImage(gift, Color.white).preserveAspect = true;
            _boxBadge = Badge(_box, catalog, 40, 40);

            WeekRow = UiKit.Rect("QuestWeekRow", list);
            WeekRow.AddImage(catalog.Panel, Palette.CardLip, true);
            var weekLayout = WeekRow.gameObject.AddComponent<LayoutElement>();
            weekLayout.preferredHeight = weekLayout.minHeight = WeekHeight;
            UiKit.Rect("Card", WeekRow).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Card, true);
            _weekLabel = UiKit.Rect("Title", WeekRow).Stretch(0, 1, 1, 1, 36, -70, 36, 18)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            OneLine(_weekLabel, UiKit.TextTitle);
            _weekSub = UiKit.Rect("Sub", WeekRow).Stretch(0, 0, 1, 0, TrackLeft, 16, TrackRight, -50)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft);
            _weekSub.resizeTextForBestFit = true;
            _weekSub.resizeTextMinSize = 12;
            _weekSub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            var track = UiKit.Rect("Track", WeekRow).Band(TrackLeft, TrackRight, TrackY - 9, 18);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _weekFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _weekFill.AddImage(catalog.Panel, Palette.Moss, true);

            for (int i = 0; i < QuestChests.Chests.Length; i++)
            {
                int index = i;
                float at = QuestChests.Chests[i].Points / (float)QuestChests.MaxPoints;
                var chest = UiKit.Rect("Chest " + i, track).Place(at, 0.5f, 0, 8, 92, 92);
                Image hit = chest.AddImage(null, new Color(0, 0, 0, 0));
                hit.raycastTarget = true;
                var button = chest.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                button.onClick.AddListener(() => onChest?.Invoke(index));
                _chestArt[i] = UiKit.Rect("Art", chest).Place(0.5f, 0.5f, 0, 0, 76, 76);
                _chestArt[i].AddImage(gift, ChestTints[i]).preserveAspect = true;
                UiKit.Rect("Points", chest).Place(0.5f, 0f, 0, -4, 120, 30)
                    .AddText(QuestChests.Chests[i].Points.ToString(), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow(0.35f, 2f);
                var tick = UiKit.Rect("Tick", chest).Place(1f, 1f, -14, -14, 40, 40);
                tick.AddImage(catalog.Round, Palette.Moss);
                UiKit.Rect("Text", tick).Stretch(0, 0, 1, 1).AddText("✓", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                _chestTicks[i] = tick.gameObject;
                _chestBadges[i] = Badge(chest, catalog, 36, 36);
                _chests[i] = chest;
            }
        }

        public static void OneLine(Text text, int size)
        {
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = 12;
            text.resizeTextMaxSize = UiKit.Scaled(size);
        }

        static GameObject Badge(RectTransform parent, GameCatalog catalog, float x, float y)
        {
            var badge = UiKit.Rect("Badge", parent).Place(1f, 1f, -x / 2f, -y / 2f, 44, 44);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("!", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            badge.gameObject.SetActive(false);
            return badge.gameObject;
        }

        public void Refresh(IReadOnlyList<QuestDef> quests, GameState s, long today, float dt)
        {
            int done = QuestChests.DailyDone(quests, s), total = QuestChests.DailyTotal(quests, s);
            bool opened = QuestChests.BoxOpened(s);
            BoxReady = QuestChests.BoxReady(quests, s);
            _todayLabel.text = L10n.T("quests.box.title", done, total);
            _todaySub.text = opened ? L10n.T("quests.box.opened") : BoxReady ? L10n.T("quests.box.ready") : L10n.T("quests.box.hint", QuestChests.BoxPrizes);
            _todayFace.color = BoxReady ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.55f) : Palette.Card;
            if (_boxBadge.activeSelf != BoxReady) _boxBadge.SetActive(BoxReady);
            _boxArt.GetComponent<Image>().color = opened ? Palette.Alpha(Color.white, 0.45f) : Color.white;
            Shake(dt);

            int points = QuestChests.Points(s, today);
            _weekLabel.text = L10n.T("quests.week.title", points, QuestChests.MaxPoints);
            _weekSub.text = L10n.T("quests.week.sub", WeekdaysLeft(today), QuestChests.QuestPoints, QuestChests.BoxPoints);
            _weekFill.anchorMax = new Vector2(Mathf.Clamp01(points / (float)QuestChests.MaxPoints), 1f);
            for (int i = 0; i < _chests.Length; i++)
            {
                bool ready = QuestChests.ChestReady(s, i, today);
                bool claimed = QuestChests.Claimed(s, i) && QuestChests.WeekOf(today) == s.WeekIndex;
                if (_chestBadges[i].activeSelf != ready) _chestBadges[i].SetActive(ready);
                if (_chestTicks[i].activeSelf != claimed) _chestTicks[i].SetActive(claimed);
                _chestArt[i].GetComponent<Image>().color = claimed ? Palette.Alpha(ChestTints[i], 0.45f) : ChestTints[i];
                if (dt < 0f) _chestArt[i].localScale = Vector3.one;
            }
        }

        static int WeekdaysLeft(long today) => (int)(QuestChests.MondayOf(QuestChests.WeekOf(today) + 1) - today);

        void Shake(float dt)
        {
            if (!BoxReady || dt <= 0f)
            {
                _sinceShake = 0f;
                if (!_shaking) _boxArt.localRotation = Quaternion.identity;
                return;
            }
            _sinceShake += dt;
            if (_shaking || _sinceShake < ShakeEvery) return;
            _sinceShake = 0f;
            _shaking = true;
            RectTransform art = _boxArt;
            _tweens.Add(Motion.Time(0.14f), t => { if (art != null) art.localRotation = Quaternion.Euler(0, 0, 12f * Ease.Pop(t)); }, Ease.Linear);
            _tweens.Add(Motion.Time(0.14f), t => { if (art != null) art.localRotation = Quaternion.Euler(0, 0, -12f * Ease.Pop(t)); }, Ease.Linear, Motion.Time(0.14f));
            _tweens.Add(Motion.Time(0.18f), t => { if (art != null) art.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, Motion.Time(0.28f), () =>
            {
                _shaking = false;
                if (art == null) return;
                art.localRotation = Quaternion.identity;
                art.localScale = Vector3.one;
            });
        }

        public void Pop(int chest)
        {
            RectTransform art = chest < 0 ? _boxArt : _chestArt[chest];
            _tweens.Add(Motion.Tap, t => { if (art != null) art.localScale = Vector3.one * (1f + 0.2f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (art != null) art.localScale = Vector3.one; });
        }
    }

    public sealed class QuestPrizeView
    {
        public struct Tile
        {
            public Sprite Icon;
            public Color Tint;
            public bool Jar;
            public string Amount;
            public string Name;
        }

        const int MaxTiles = QuestChests.BoxPrizes;
        const float TileWidth = 220f, TileGap = 24f;
        const float FlipFirst = 0.35f, FlipEvery = 0.45f, FlipHalf = 0.16f;

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Text _title, _sub;
        readonly RectTransform[] _backs = new RectTransform[MaxTiles];
        readonly RectTransform[] _fronts = new RectTransform[MaxTiles];
        readonly RectTransform[] _slots = new RectTransform[MaxTiles];
        readonly Image[] _icons = new Image[MaxTiles];
        readonly RectTransform[] _jars = new RectTransform[MaxTiles];
        readonly Text[] _amounts = new Text[MaxTiles];
        readonly Text[] _names = new Text[MaxTiles];
        readonly Image[] _backArt = new Image[MaxTiles];
        int _shown;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Root => _card;
        public int Flipped { get; private set; }

        public QuestPrizeView(RectTransform canvas, GameCatalog catalog, Tweens tweens)
        {
            _tweens = tweens;
            Sprite gift = catalog.GiftBox != null ? catalog.GiftBox : catalog.Round;
            var overlay = UiKit.Rect("QuestPrizes", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 860, 720);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            _title = UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -84, 740, 70)
                .AddText("", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            QuestChestsView.OneLine(_title, 48);
            _sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -150, 740, 48)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            _sub.resizeTextForBestFit = true;
            _sub.resizeTextMinSize = 14;
            _sub.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);

            for (int i = 0; i < MaxTiles; i++)
            {
                var slot = UiKit.Rect("Prize " + i, _card).Place(0.5f, 0.5f, 0, -10, TileWidth, 280);
                _slots[i] = slot;
                var back = UiKit.Rect("Back", slot).Stretch(0, 0, 1, 1);
                back.AddImage(catalog.Panel, Palette.Mix(Palette.Lantern, Palette.CardLip, 0.35f), true);
                _backArt[i] = UiKit.Rect("Art", back).Place(0.5f, 0.5f, 0, 0, 140, 140).AddImage(gift, Color.white);
                _backArt[i].preserveAspect = true;
                _backs[i] = back;

                var front = UiKit.Rect("Front", slot).Stretch(0, 0, 1, 1);
                front.AddImage(catalog.Panel, Palette.Card, true);
                _icons[i] = UiKit.Rect("Icon", front).Place(0.5f, 1f, 0, -94, 128, 128).AddImage(null, Color.white);
                _icons[i].preserveAspect = true;
                _jars[i] = IncomeView.JarIcon(front, catalog, "Jar", 116f, 0.5f, 1f, 0, -94);
                _amounts[i] = UiKit.Rect("Amount", front).Place(0.5f, 0f, 0, 92, TileWidth - 16, 56)
                    .AddText("", 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                QuestChestsView.OneLine(_amounts[i], 40);
                _names[i] = UiKit.Rect("Name", front).Place(0.5f, 0f, 0, 40, TileWidth - 20, 52)
                    .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
                _names[i].resizeTextForBestFit = true;
                _names[i].resizeTextMinSize = 12;
                _names[i].resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                _fronts[i] = front;
            }

            var close = UiKit.Rect("Close", _card).Place(0.5f, 0f, 0, 84, 300, UiKit.MinTouch);
            ChunkyButton closeButton = null;
            closeButton = UiKit.Chunky(close, catalog.Panel, () => { closeButton.Press(_tweens); Close(); });
            Text label = UiKit.Rect("Text", closeButton.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0)
                .AddText(L10n.T("quests.prize.ok"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            label.FitOneLine(280f, 16);
            _root.SetActive(false);
        }

        public void Open(string title, string sub, IReadOnlyList<Tile> tiles, Color back)
        {
            _title.text = title;
            _sub.text = sub;
            _shown = Math.Min(MaxTiles, tiles.Count);
            Flipped = 0;
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            RectTransform card = _card;
            _tweens.Add(Motion.Time(Motion.Panel), t => { if (card != null) card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
            float width = _shown * TileWidth + (_shown - 1) * TileGap;
            for (int i = 0; i < MaxTiles; i++)
            {
                bool show = i < _shown;
                _slots[i].gameObject.SetActive(show);
                if (!show) continue;
                _slots[i].anchoredPosition = new Vector2(-width / 2f + TileWidth / 2f + i * (TileWidth + TileGap), -10f);
                Tile tile = tiles[i];
                _icons[i].sprite = tile.Icon;
                _icons[i].color = tile.Tint;
                _icons[i].gameObject.SetActive(!tile.Jar);
                _jars[i].gameObject.SetActive(tile.Jar);
                _amounts[i].text = tile.Amount;
                _names[i].text = tile.Name;
                _backArt[i].color = back;
                _backs[i].gameObject.SetActive(true);
                _backs[i].localScale = Vector3.one;
                _fronts[i].gameObject.SetActive(false);
                _fronts[i].localScale = new Vector3(0f, 1f, 1f);
                Flip(i, Motion.Time(FlipFirst + i * FlipEvery));
            }
        }

        void Flip(int i, float delay)
        {
            RectTransform back = _backs[i], front = _fronts[i];
            float half = Motion.Time(FlipHalf);
            _tweens.Add(half, t => { if (back != null) back.localScale = new Vector3(1f - t, 1f, 1f); }, Ease.InCubic, delay, () =>
            {
                if (back == null || front == null) return;
                back.gameObject.SetActive(false);
                front.gameObject.SetActive(true);
                Sfx.Blip();
                _tweens.Add(half, t => { if (front != null) front.localScale = new Vector3(t, 1f, 1f); }, Motion.Curve(Ease.OutBack), 0f, () => Flipped++);
            });
        }

        public void Close()
        {
            _root.SetActive(false);
        }
    }
}

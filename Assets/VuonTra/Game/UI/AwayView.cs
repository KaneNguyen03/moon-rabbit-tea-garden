using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class AwayView
    {
        public const float Height = 1180f;
        public const float CountSeconds = 0.8f;
        public const int MaxTicks = 12;

        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Func<int> _doubleCost;
        readonly Func<int> _golden;
        readonly Func<bool> _doubled;
        readonly Action _onCollect;
        readonly Action _onDouble;
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

        public AwayView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Func<int> doubleCost, Func<int> golden, Func<bool> doubled,
            Action onCollect, Action onDouble)
        {
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
                .AddText("+0", 72, Palette.Carrot, TextAnchor.MiddleLeft, FontStyle.Bold);
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
            _doubleLabel.color = ChunkyButton.InkFor(can);
            Ad?.Refresh();
        }

        public void Close() => Sheet.Close();

        public AdButton Ad { get; private set; }

        public AdButton AttachAd(Func<AdOffer> offer, Func<bool> busy, Action onWatch)
        {
            var rt = UiKit.Rect("Ad", Sheet.Card).Place(1f, 0f, -300, 259, 520, UiKit.MinTouch);
            Ad = new AdButton(rt, _catalog, "ad.away", "ad.away.now", offer, busy, onWatch);
            return Ad;
        }
    }
}

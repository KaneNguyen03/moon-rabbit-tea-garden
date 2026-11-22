using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The moon pond's card (#58b), sliding up when the pond is touched: the baits on hand and when the next one comes
    /// back, the water with a float that bobs and then sinks, a Cast button, and the basket with every fish caught so far.
    /// A touch on the water while the float sinks lands the fish, which flies into the basket. The rules live in
    /// <see cref="FishingCast"/> and <see cref="Fishing"/>; this draws them and feeds the runner time and taps.
    /// Reduced motion: the float does not bob and its sinking shows as a change of colour and size, on the same clock.
    /// </summary>
    public sealed class FishingView
    {
        public const float Height = 1240f;
        /// <summary>A caught fish rises over the water for this long, then flies to the basket in <see cref="FlySeconds"/>.</summary>
        public const float ShowSeconds = 0.5f, FlySeconds = 0.55f;
        /// <summary>How far the float goes down while it sinks, in canvas units.</summary>
        const float SinkDepth = 46f;
        /// <summary>The colours a fish is drawn in while its picture is missing.</summary>
        static readonly Color[] FishTints =
        {
            new Color(0.78f, 0.82f, 0.88f), new Color(0.98f, 0.62f, 0.30f), new Color(0.97f, 0.66f, 0.72f),
            new Color(0.96f, 0.90f, 0.62f), new Color(0.98f, 0.80f, 0.25f), new Color(0.36f, 0.40f, 0.78f),
        };

        enum Say { Hint, Wait, Sink, Early, Lost, Caught }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly RectTransform _card, _water, _float, _floatArt, _ring, _catch, _basket;
        readonly Image _cap, _body, _ringImage, _catchImage;
        readonly Text _baits, _next, _status, _castLabel;
        readonly Image[] _dots = new Image[Fishing.BaitCap];
        readonly Text[] _counts;
        readonly Image[] _fishIcons;
        readonly int[] _pending;
        readonly ChunkyButton _castButton;
        readonly Color _capColor = Palette.Mix(Palette.Carrot, new Color(0.85f, 0.2f, 0.2f), 0.6f);
        Say _say;
        string _caughtLine = "";
        float _clock, _popAge = 9f;
        int _shownBaits = -1, _shownCast = -1;
        long _shownNext = -1;

        /// <summary>The sliding card this view lives on.</summary>
        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        /// <summary>The line under the water: what the float is doing, or what the last cast came to.</summary>
        public string Status => _status.text;
        /// <summary>The basket's icon on the card, where a caught fish lands.</summary>
        public RectTransform BasketIcon => _basket;
        /// <summary>Fish on their way to the basket now.</summary>
        public int FishFlying { get; private set; }
        /// <summary>The float's picture: its colour turns when it sinks (the one sign of it under reduced motion).</summary>
        public Color FloatColor => _cap.color;
        public Vector3 FloatScale => _floatArt.localScale;

        public FishingView(RectTransform canvas, GameCatalog catalog, Tweens tweens, GameRunner runner)
        {
            _runner = runner;
            _catalog = catalog;
            _tweens = tweens;
            _counts = new Text[Fishing.Kinds];
            _fishIcons = new Image[Fishing.Kinds];
            _pending = new int[Fishing.Kinds];

            // A cast still out when the card is closed waits in the water until it is opened again.
            Sheet = new BottomSheet(canvas, "Pond", catalog, tweens, Height, null);
            _card = Sheet.Card;
            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -66, -66, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            Text title = UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -86, 760, 64)
                .AddText(L10n.T("pond.title"), 44, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            title.resizeTextForBestFit = true;
            title.resizeTextMinSize = 28;
            title.resizeTextMaxSize = UiKit.Scaled(44);

            // The baits: five dots, full while on hand, and the count; on the right, when the next one comes back.
            var dots = UiKit.Rect("Dots", _card).Place(0f, 1f, 50 + 5 * 34 / 2f, -164, 5 * 34, 34);
            for (int i = 0; i < _dots.Length; i++)
                _dots[i] = UiKit.Rect("Bait " + i, dots).Place(0f, 0.5f, 17 + 34 * i, 0, 26, 26).AddImage(catalog.Round, Palette.Track);
            _baits = UiKit.Rect("Baits", _card).Stretch(0, 1, 0.5f, 1, 240, -188, 0, 140)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _baits.resizeTextForBestFit = true;
            _baits.resizeTextMinSize = 16;
            _baits.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _next = UiKit.Rect("Next", _card).Stretch(0.5f, 1, 1, 1, 0, -188, 50, 140)
                .AddText("", UiKit.TextSmall, RabbitGridView.Quiet, TextAnchor.MiddleRight, FontStyle.Bold);
            _next.resizeTextForBestFit = true;
            _next.resizeTextMinSize = 14;
            _next.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            // The water: moonlit, the moon in it, and the float with its line. A touch anywhere on it strikes.
            _water = UiKit.Rect("Water", _card).Place(0.5f, 1f, 0, -450, 960, 500);
            Image waterImage = _water.AddImage(catalog.Panel, Palette.Mix(Palette.Night, new Color(0.25f, 0.45f, 0.62f), 0.55f), true);
            waterImage.raycastTarget = true;
            var waterButton = _water.gameObject.AddComponent<Button>();
            waterButton.transition = Selectable.Transition.None;
            waterButton.targetGraphic = waterImage;
            waterButton.onClick.AddListener(TapWater);
            UiKit.Rect("Shine", _water).Stretch(0, 0, 1, 1, 16, 16, 16, 16).AddImage(catalog.Glow, new Color(0.75f, 0.86f, 1f, 0.18f));
            UiKit.Rect("MoonGlow", _water).Place(0.5f, 0.5f, 210, 110, 300, 170).AddImage(catalog.Glow, new Color(1f, 0.95f, 0.75f, 0.35f));
            UiKit.Rect("Moon", _water).Place(0.5f, 0.5f, 210, 110, 92, 92).AddImage(catalog.Round, new Color(1f, 0.96f, 0.82f, 0.75f));
            UiKit.Rect("MoonCut", _water).Place(0.5f, 0.5f, 236, 124, 80, 80).AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, new Color(0.25f, 0.45f, 0.62f), 0.55f), 0.9f));

            _float = UiKit.Rect("Float", _water).Place(0.5f, 0.5f, -40, -40, 80, 130);
            _ring = UiKit.Rect("Ring", _float).Place(0.5f, 0.5f, 0, -38, 230, 96);
            _ringImage = _ring.AddImage(catalog.Glow, new Color(0.85f, 0.93f, 1f, 0.3f));
            _floatArt = UiKit.Rect("Art", _float).Stretch(0, 0, 1, 1);
            // The line runs up from the float out of the top of the water, and goes down with it.
            UiKit.Rect("Line", _floatArt).Place(0.5f, 0.5f, 0, 52 + 110, 3, 220).AddImage(null, new Color(1f, 1f, 1f, 0.45f));
            UiKit.Rect("Stick", _floatArt).Place(0.5f, 0.5f, 0, 36, 8, 34).AddImage(catalog.Panel, Palette.Ink, true);
            _body = UiKit.Rect("Body", _floatArt).Place(0.5f, 0.5f, 0, -24, 44, 44).AddImage(catalog.Round, Palette.Paper);
            _cap = UiKit.Rect("Cap", _floatArt).Place(0.5f, 0.5f, 0, 6, 42, 42).AddImage(catalog.Round, _capColor);
            _float.gameObject.SetActive(false);

            _status = UiKit.Rect("Status", _card).Place(0.5f, 1f, 0, -752, 940, 64)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _status.resizeTextForBestFit = true;
            _status.resizeTextMinSize = 18;
            _status.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);

            var cast = UiKit.Rect("Cast", _card).Place(0.5f, 1f, 0, -862, 460, 110);
            _castButton = UiKit.Chunky(cast, catalog.Panel, Cast);
            _castLabel = UiKit.Rect("Text", _castButton.FaceRect).Stretch(0, 0, 1, 1, 16, 0, 16, 0)
                .AddText(L10n.T("pond.cast"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _castLabel.resizeTextForBestFit = true;
            _castLabel.resizeTextMinSize = 18;
            _castLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextTitle);

            // The basket: its picture, then each fish with how many are in it; one not caught yet is a shadow.
            var basketRow = UiKit.Rect("Basket", _card).Place(0.5f, 1f, 0, -1058, 980, 210);
            basketRow.AddImage(catalog.Panel, Palette.Card, true);
            _basket = UiKit.Rect("Icon", basketRow).Place(0f, 0.5f, 96, 8, 150, 150);
            if (catalog.FishBasket != null) _basket.AddImage(catalog.FishBasket, Color.white).preserveAspect = true;
            else _basket.AddImage(catalog.Panel, Palette.Mix(Palette.Carrot, Palette.Ink, 0.35f), true);
            for (int i = 0; i < Fishing.Kinds; i++)
            {
                var slot = UiKit.Rect("Fish " + i, basketRow).Place(0f, 0.5f, 200f + 65f + i * 128f, 0, 124, 190);
                _fishIcons[i] = FishImage(UiKit.Rect("Art", slot).Place(0.5f, 1f, 0, -58, 112, 84), catalog, i);
                _counts[i] = UiKit.Rect("Count", slot).Place(0.5f, 0f, 0, 34, 124, 44)
                    .AddText("×0", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            // A caught fish, shown over the water and then sent to the basket.
            _catch = UiKit.Rect("Catch", _card).Place(0.5f, 1f, 0, -430, 260, 180);
            _catchImage = _catch.AddImage(catalog.Round, Color.white);
            _catchImage.preserveAspect = true;
            _catch.gameObject.SetActive(false);

            Sheet.Root.SetActive(false);
        }

        /// <summary>A fish's picture in a box, or a tinted circle while it has none.</summary>
        static Image FishImage(RectTransform rt, GameCatalog catalog, int fish)
        {
            Sprite art = catalog.FishSprite(fish);
            Image img = rt.AddImage(art != null ? art : catalog.Round, art != null ? Color.white : FishTints[Mathf.Clamp(fish, 0, FishTints.Length - 1)]);
            img.preserveAspect = true;
            return img;
        }

        public void Open()
        {
            _say = _runner.Cast != null ? Say.Wait : Say.Hint;
            _shownBaits = -1;
            _shownCast = -1;
            _shownNext = -1;
            _catch.gameObject.SetActive(false);
            for (int i = 0; i < _pending.Length; i++) _pending[i] = 0;
            FishFlying = 0;
            Sheet.Open();
            Sheet.Root.transform.SetAsLastSibling();
            Draw();
        }

        public void Close() => Sheet.Close();

        /// <summary>The Cast button: a bait goes on the hook and the float drops in.</summary>
        public void Cast()
        {
            if (!IsOpen || _runner.Cast != null) return;
            if (_runner.StartCast() == null)
            {
                Sfx.Deny();
                _castButton.Shake(_tweens);
                return;
            }
            Sfx.Cast();
            _castButton.Press(_tweens);
            _say = Say.Wait;
            _popAge = 0f;
            Draw();
        }

        /// <summary>A touch on the water: too early brings the float back up; while it sinks, the fish.</summary>
        public void TapWater()
        {
            FishingCast cast = _runner.Cast;
            if (!IsOpen || cast == null) return;
            int fish = cast.Fish;
            CastTap tap = _runner.TapCast();
            if (tap == CastTap.Early)
            {
                Sfx.FloatUp();
                _say = Say.Early;
                _popAge = 0f;
            }
            else if (tap == CastTap.Caught) Caught(fish);
            Draw();
        }

        void Caught(int fish)
        {
            Sfx.Strike();
            Haptics.Light();
            _say = Say.Caught;
            _caughtLine = L10n.T("pond.caught", L10n.T("fish." + fish)) + " · " + L10n.T("fish.points", Fishing.Points[fish]);
            _pending[fish]++;
            FishFlying++;

            // The fish comes up out of the water, holds there a moment and flies down into the basket.
            Sprite art = _catalog.FishSprite(fish);
            _catchImage.sprite = art != null ? art : _catalog.Round;
            _catchImage.color = art != null ? Color.white : FishTints[fish];
            _catch.gameObject.SetActive(true);
            _catch.SetAsLastSibling();
            Vector2 from = new Vector2(0f, -430f);
            Vector3 basket = _card.InverseTransformPoint(_basket.TransformPoint(Vector3.zero));
            // Card-local, measured from its top middle, where the fish is anchored.
            Vector2 to = new Vector2(basket.x - _card.rect.center.x, basket.y - _card.rect.yMax);
            float size = 0.6f + 0.1f * fish; // the rarer, the bigger
            _catch.anchoredPosition = from;
            _catch.localScale = Vector3.zero;
            _tweens.Add(Motion.Time(0.25f), t => { if (_catch != null) _catch.localScale = Vector3.one * size * t; }, Motion.Curve(Ease.OutBack));
            bool reeling = false;
            _tweens.Add(Motion.Time(FlySeconds), t =>
            {
                // The reel runs from the moment the fish leaves the water; its thunk lands with it in the basket.
                if (!reeling) { reeling = true; Sfx.Reel(); }
                if (_catch == null) return;
                Vector2 mid = (from + to) * 0.5f + new Vector2(0f, 140f);
                float u = 1f - t;
                _catch.anchoredPosition = u * u * from + 2f * u * t * mid + t * t * to;
                _catch.localScale = Vector3.one * Mathf.Lerp(size, 0.35f, t);
            }, Ease.InOutCubic, Motion.Time(ShowSeconds), () =>
            {
                _pending[fish] = Mathf.Max(0, _pending[fish] - 1);
                FishFlying = Mathf.Max(0, FishFlying - 1);
                if (_catch == null) return;
                if (FishFlying == 0) _catch.gameObject.SetActive(false);
                _tweens.Add(Motion.Tap, t => { if (_basket != null) _basket.localScale = Vector3.one * (1f + 0.15f * Ease.Pop(t)); }, Ease.Linear, 0f,
                    () => { if (_basket != null) _basket.localScale = Vector3.one; });
                Draw();
            });
        }

        /// <summary>The clock: the cast's time runs only while its card is up, so the float never sinks unseen.</summary>
        public void Update(float dt)
        {
            if (!IsOpen) return;
            _clock += dt;
            _popAge += dt;
            FishingCast cast = _runner.Cast;
            if (cast != null)
            {
                CastPhase before = cast.Phase;
                CastPhase phase = _runner.AdvanceCast(dt);
                if (phase == CastPhase.Lost)
                {
                    Sfx.BaitLost();
                    _say = Say.Lost;
                }
                else if (phase == CastPhase.Sinking && before != CastPhase.Sinking)
                {
                    Sfx.Sink();
                    Haptics.Light();
                    _say = Say.Sink;
                }
            }
            Draw();
        }

        /// <summary>Every number and the float, from the runner as it is now.</summary>
        void Draw()
        {
            FishingCast cast = _runner.Cast;
            int baits = _runner.Baits;
            long next = _runner.NextBaitIn;
            int castOut = cast != null ? 1 : 0;
            if (baits != _shownBaits || next != _shownNext || castOut != _shownCast)
            {
                _shownBaits = baits;
                _shownNext = next;
                _shownCast = castOut;
                _baits.text = L10n.T("fish.baits", baits, Fishing.BaitCap);
                _next.text = next > 0 ? L10n.T("fish.next", OrderView.Clock(next)) : "";
                for (int i = 0; i < _dots.Length; i++) _dots[i].color = i < baits ? Palette.Carrot : Palette.Track;
                bool can = cast == null && baits > 0;
                _castButton.SetEnabled(can);
                _castLabel.color = ChunkyButton.InkFor(can);
                _castLabel.text = cast != null ? L10n.T("pond.casting") : baits > 0 ? L10n.T("pond.cast") : L10n.T("pond.nobait");
            }
            for (int i = 0; i < Fishing.Kinds; i++)
            {
                int have = Mathf.Max(0, _runner.Basket[i] - _pending[i]);
                string count = "×" + have;
                if (_counts[i].text != count) _counts[i].text = count;
                Color c = _fishIcons[i].sprite == _catalog.Round ? FishTints[i] : Color.white;
                _fishIcons[i].color = have > 0 ? c : Palette.Alpha(c, 0.3f);
            }
            _status.text = StatusLine(cast, baits);
            DrawFloat(cast);
        }

        string StatusLine(FishingCast cast, int baits)
        {
            switch (_say)
            {
                case Say.Wait: return L10n.T("pond.wait");
                case Say.Sink: return L10n.T("pond.sink");
                case Say.Early: return L10n.T("pond.early");
                case Say.Lost: return baits > 0 ? L10n.T("pond.lost") : L10n.T("pond.lost") + " · " + L10n.T("fish.nobait");
                case Say.Caught: return _caughtLine;
                default: return baits > 0 || cast != null ? L10n.T("pond.hint") : L10n.T("fish.nobait");
            }
        }

        /// <summary>The float: bobbing, then going under; still and changing colour and size under reduced motion.</summary>
        void DrawFloat(FishingCast cast)
        {
            bool show = cast != null;
            if (_float.gameObject.activeSelf != show) _float.gameObject.SetActive(show);
            if (!show) return;
            bool sinking = cast.Phase == CastPhase.Sinking;
            float k = sinking ? Mathf.Clamp01((float)((cast.Elapsed - cast.BobSeconds) / Fishing.SinkSeconds)) : 0f;
            float pop = !Motion.Reduced && _popAge < 0.3f ? Ease.Pop(_popAge / 0.3f) : 0f;
            if (Motion.Reduced)
            {
                // No movement: the float turns gold and shrinks the moment it sinks, the ring lights up.
                _floatArt.anchoredPosition = Vector2.zero;
                _floatArt.localScale = Vector3.one * (sinking ? 0.7f : 1f);
                _cap.color = sinking ? Palette.Lantern : _capColor;
                _body.color = Palette.Paper;
                _ringImage.color = new Color(0.85f, 0.93f, 1f, sinking ? 0.85f : 0.3f);
                _ring.localScale = Vector3.one;
                return;
            }
            float bob = sinking ? 0f : 7f * Mathf.Sin(_clock * 3.4f) + 3f * Mathf.Sin(_clock * 7.1f + 1f);
            _floatArt.anchoredPosition = new Vector2(0f, bob - SinkDepth * k);
            _floatArt.localScale = Vector3.one * (1f + 0.25f * pop - 0.25f * k);
            _cap.color = sinking ? Palette.Mix(_capColor, Palette.Lantern, 0.6f) : _capColor;
            _body.color = Palette.Alpha(Palette.Paper, 1f - 0.6f * k);
            _ringImage.color = new Color(0.85f, 0.93f, 1f, sinking ? 0.6f + 0.3f * k : 0.25f + 0.08f * Mathf.Sin(_clock * 3.4f));
            _ring.localScale = Vector3.one * (sinking ? 1f + 0.5f * k : 1f + 0.04f * Mathf.Sin(_clock * 3.4f));
        }
    }
}

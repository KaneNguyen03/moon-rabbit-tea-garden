using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;
using TweenEase = DG.Tweening.Ease;

namespace VuonTra.Game.UI
{
    public sealed class FishingView
    {
        public const float Height = 1240f;
        public const float ShowSeconds = 0.5f, FlySeconds = 0.55f;
        const float SinkDepth = 46f, CatchArc = 140f;
        public const float RippleSeconds = 2f * RingGap + RingLife;
        const float RingGap = 0.18f, RingLife = 0.72f, RingRise = 0.12f;
        const float BobReach = 7f, BobHalfPeriod = Mathf.PI / 3.4f, SwellReach = 3f, SwellHalfPeriod = Mathf.PI / 7.1f;
        const float FloatArtScale = 0.3f, RingArtScale = 0.45f, WaterArtMax = 1.5f;
        const float DishY = -69f, LineUpY = 37f, LineDownY = 27f;
        static readonly Color[] FishTints =
        {
            new Color(0.78f, 0.82f, 0.88f), new Color(0.98f, 0.62f, 0.30f), new Color(0.97f, 0.66f, 0.72f),
            new Color(0.96f, 0.90f, 0.62f), new Color(0.98f, 0.80f, 0.25f), new Color(0.36f, 0.40f, 0.78f),
        };

        enum Say { Hint, Wait, Sink, Early, Lost, Caught }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly RectTransform _card, _water, _float, _bob, _floatArt, _ring, _catch, _basket, _line, _ripples;
        readonly Image _cap, _body, _ringImage, _catchImage;
        readonly Text _baits, _next, _status, _castLabel;
        readonly Image[] _dots = new Image[Fishing.BaitCap];
        readonly Text[] _counts;
        readonly Image[] _fishIcons;
        readonly int[] _pending;
        readonly ChunkyButton _castButton;
        readonly Image _picture;
        readonly Image[] _rings;
        readonly Tween[] _bobLoops;
        readonly Sprite _floatUp, _floatDown;
        readonly Color _capColor = Palette.Mix(Palette.Carrot, new Color(0.85f, 0.2f, 0.2f), 0.6f);
        Say _say;
        string _caughtLine = "";
        float _popAge = 9f;
        Sprite _shownFloat;
        int _shownBaits = -1, _shownCast = -1;
        long _shownNext = -1;

        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        public string Status => _status.text;
        public RectTransform BasketIcon => _basket;
        public int FishFlying { get; private set; }
        public Color FloatColor => _cap.color;
        public Vector3 FloatScale => _floatArt.localScale;
        public Sprite FloatSprite => _picture != null ? _picture.sprite : null;
        public int RipplesShowing
        {
            get
            {
                int n = 0;
                foreach (Image ring in _rings) if (ring.gameObject.activeSelf) n++;
                return n;
            }
        }

        public FishingView(RectTransform canvas, GameCatalog catalog, Tweens tweens, GameRunner runner)
        {
            _runner = runner;
            _catalog = catalog;
            _tweens = tweens;
            _counts = new Text[Fishing.Kinds];
            _fishIcons = new Image[Fishing.Kinds];
            _pending = new int[Fishing.Kinds];

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

            _water = UiKit.Rect("Water", _card).Place(0.5f, 1f, 0, -450, 960, 500);
            Image waterImage = _water.AddImage(catalog.Panel, Palette.Mix(Palette.Night, new Color(0.25f, 0.45f, 0.62f), 0.55f), true);
            waterImage.raycastTarget = true;
            var waterButton = _water.gameObject.AddComponent<Button>();
            waterButton.transition = Selectable.Transition.None;
            waterButton.targetGraphic = waterImage;
            waterButton.onClick.AddListener(TapWater);
            UiKit.Rect("Shine", _water).Stretch(0, 0, 1, 1, 16, 16, 16, 16).AddImage(catalog.Glow, new Color(0.75f, 0.86f, 1f, 0.18f));
            var moonGlow = UiKit.Rect("MoonGlow", _water).Place(0.5f, 0.5f, 210, 110, 300, 170);
            moonGlow.AddImage(catalog.Glow, new Color(1f, 0.95f, 0.75f, 0.35f));
            var moon = UiKit.Rect("Moon", _water).Place(0.5f, 0.5f, 210, 110, 92, 92);
            moon.AddImage(catalog.Round, new Color(1f, 0.96f, 0.82f, 0.75f));
            var moonCut = UiKit.Rect("MoonCut", _water).Place(0.5f, 0.5f, 236, 124, 80, 80);
            moonCut.AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, new Color(0.25f, 0.45f, 0.62f), 0.55f), 0.9f));
            if (catalog.PondWater != null)
            {
                waterImage.color = Palette.Mix(Palette.Grass, Palette.Night, 0.5f);
                Vector2 size = catalog.PondWater.rect.size;
                float k = Mathf.Min(WaterArtMax, Mathf.Min(940f / size.x, 480f / size.y));
                UiKit.Rect("Surface", _water).Place(0.5f, 0.5f, 0, 0, size.x * k, size.y * k).AddImage(catalog.PondWater, Color.white).preserveAspect = true;
                moonGlow.gameObject.SetActive(false);
                moon.gameObject.SetActive(false);
                moonCut.gameObject.SetActive(false);
            }

            _ripples = UiKit.Rect("Ripples", _water).Place(0.5f, 0.5f, -40, -40 + DishY + 14f, 240, 140);
            int ringCount = catalog.Ripples != null ? catalog.Ripples.Length : 0;
            _rings = new Image[ringCount];
            for (int i = 0; i < ringCount; i++)
            {
                Sprite art = catalog.Ripples[i];
                Vector2 ringSize = art != null ? art.rect.size * RingArtScale : new Vector2(120f + 50f * i, 66f + 28f * i);
                _rings[i] = UiKit.Rect("Ripple " + i, _ripples).Place(0.5f, 0.5f, 0, 0, ringSize.x, ringSize.y).AddImage(art != null ? art : catalog.Glow, Color.white);
                _rings[i].preserveAspect = true;
                _rings[i].gameObject.SetActive(false);
            }

            _float = UiKit.Rect("Float", _water).Place(0.5f, 0.5f, -40, -40, 80, 130);
            _ring = UiKit.Rect("Ring", _float).Place(0.5f, 0.5f, 0, -38, 230, 96);
            _ringImage = _ring.AddImage(catalog.Glow, new Color(0.85f, 0.93f, 1f, 0.3f));
            _bob = UiKit.Rect("Bob", _float).Stretch(0, 0, 1, 1);
            _floatArt = UiKit.Rect("Art", _bob).Stretch(0, 0, 1, 1);
            _line = UiKit.Rect("Line", _floatArt).Place(0.5f, 0.5f, 0, 52 + 110, 3, 220);
            _line.AddImage(null, new Color(1f, 1f, 1f, 0.45f));
            var stick = UiKit.Rect("Stick", _floatArt).Place(0.5f, 0.5f, 0, 36, 8, 34);
            stick.AddImage(catalog.Panel, Palette.Ink, true);
            _body = UiKit.Rect("Body", _floatArt).Place(0.5f, 0.5f, 0, -24, 44, 44).AddImage(catalog.Round, Palette.Paper);
            _cap = UiKit.Rect("Cap", _floatArt).Place(0.5f, 0.5f, 0, 6, 42, 42).AddImage(catalog.Round, _capColor);
            if (catalog.FloatUp != null && catalog.FloatDown != null)
            {
                _floatUp = catalog.FloatUp;
                _floatDown = catalog.FloatDown;
                var picture = UiKit.Rect("Picture", _floatArt);
                picture.anchorMin = picture.anchorMax = new Vector2(0.5f, 0.5f);
                picture.pivot = new Vector2(0.5f, 0f);
                _picture = picture.AddImage(_floatUp, Color.white);
                _picture.preserveAspect = true;
                ShowFloat(_floatUp);
                stick.gameObject.SetActive(false);
                _body.gameObject.SetActive(false);
                _cap.gameObject.SetActive(false);
                _ring.gameObject.SetActive(false);
            }
            _bob.anchoredPosition = new Vector2(0f, -BobReach);
            _floatArt.anchoredPosition = new Vector2(0f, -SwellReach);
            _ring.localScale = Vector3.one * 0.96f;
            _ringImage.color = new Color(0.85f, 0.93f, 1f, 0.17f);
            _bobLoops = new Tween[]
            {
                Breathe(_bob.DOAnchorPosY(BobReach, BobHalfPeriod), _bob),
                Breathe(_floatArt.DOAnchorPosY(SwellReach, SwellHalfPeriod), _floatArt),
                Breathe(_ring.DOScale(1.04f, BobHalfPeriod), _ring),
                Breathe(_ringImage.DOFade(0.33f, BobHalfPeriod), _ring),
            };
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

            _catch = UiKit.Rect("Catch", _card).Place(0.5f, 1f, 0, -430, 260, 180);
            _catchImage = _catch.AddImage(catalog.Round, Color.white);
            _catchImage.preserveAspect = true;
            _catch.gameObject.SetActive(false);

            Sheet.Root.SetActive(false);
        }

        void ShowFloat(Sprite art)
        {
            if (_shownFloat == art) return;
            _shownFloat = art;
            _picture.sprite = art;
            var rt = _picture.rectTransform;
            rt.sizeDelta = art.rect.size * FloatArtScale;
            rt.anchoredPosition = new Vector2(0f, DishY);
            _line.anchoredPosition = new Vector2(0f, (art == _floatDown ? LineDownY : LineUpY) + 110f);
        }

        static Tween Breathe(Tweener loop, Component owner) =>
            loop.SetEase(TweenEase.InOutSine).SetLoops(-1, LoopType.Yoyo).OnUiClock(owner);

        void Bobbing(bool on)
        {
            if (on == _bobLoops[0].IsPlaying()) return;
            foreach (Tween loop in _bobLoops)
            {
                if (on) loop.Restart();
                else loop.Pause();
            }
            if (!on) _bob.anchoredPosition = Vector2.zero;
        }

        void StillWater()
        {
            foreach (Image ring in _rings)
            {
                ring.DOKill();
                ring.gameObject.SetActive(false);
            }
        }

        void Ripple()
        {
            StillWater();
            for (int i = 0; i < _rings.Length; i++)
            {
                if (Motion.Reduced)
                {
                    if (i == Mathf.Min(1, _rings.Length - 1)) StillRing(_rings[i]);
                }
                else Spread(_rings[i], i * RingGap);
            }
        }

        static void StillRing(Image ring)
        {
            ring.transform.localScale = Vector3.one;
            ring.color = new Color(1f, 1f, 1f, 0.8f);
            DOTween.Sequence()
                .AppendCallback(() => ring.gameObject.SetActive(true))
                .AppendInterval(RippleSeconds)
                .AppendCallback(() => ring.gameObject.SetActive(false))
                .SetTarget(ring).OnUiClock(ring);
        }

        static void Spread(Image ring, float delay)
        {
            Transform rt = ring.transform;
            rt.localScale = Vector3.one * 0.8f;
            ring.color = new Color(1f, 1f, 1f, 0f);
            float rise = RingLife * RingRise;
            DOTween.Sequence()
                .InsertCallback(delay, () => ring.gameObject.SetActive(true))
                .Insert(delay, rt.DOScale(1.15f, RingLife).SetEase(TweenEase.OutCubic))
                .Insert(delay, ring.DOFade(0.85f * (1f - RingRise), rise).SetEase(TweenEase.Linear))
                .Insert(delay + rise, ring.DOFade(0f, RingLife - rise).SetEase(TweenEase.Linear))
                .AppendCallback(() => ring.gameObject.SetActive(false))
                .SetTarget(ring).OnUiClock(ring);
        }

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
            StillWater();
            for (int i = 0; i < _pending.Length; i++) _pending[i] = 0;
            FishFlying = 0;
            Sheet.Open();
            Sheet.Root.transform.SetAsLastSibling();
            Draw();
        }

        public void Close() => Sheet.Close();

        public void Cast()
        {
            if (!IsOpen || _runner.Cast != null) return;
            if (_runner.StartCast() == null)
            {
                Sfx.Deny();
                _castButton.Shake();
                return;
            }
            Sfx.Cast();
            _castButton.Press(_tweens);
            _say = Say.Wait;
            _popAge = 0f;
            Ripple();
            Draw();
        }

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
            Ripple();
            _say = Say.Caught;
            _caughtLine = L10n.T("pond.caught", L10n.T("fish." + fish)) + " · " + L10n.T("fish.points", Fishing.Points[fish]);
            _pending[fish]++;
            FishFlying++;

            Sprite art = _catalog.FishSprite(fish);
            _catchImage.sprite = art != null ? art : _catalog.Round;
            _catchImage.color = art != null ? Color.white : FishTints[fish];
            _catch.gameObject.SetActive(true);
            _catch.SetAsLastSibling();
            Vector2 from = new Vector2(0f, -430f);
            Vector3 basket = _card.InverseTransformPoint(_basket.TransformPoint(Vector3.zero));
            Vector2 to = new Vector2(basket.x - _card.rect.center.x, basket.y - _card.rect.yMax);
            float size = 0.6f + 0.1f * fish;
            _catch.anchoredPosition = from;
            _catch.localScale = Vector3.zero;
            _catch.DOScale(size, Motion.Time(0.25f)).SetEase(Motion.Curve(TweenEase.OutBack)).OnUiClock(_catch);
            float fly = Motion.Time(FlySeconds);
            Motion.After(Motion.Time(ShowSeconds), _catch, () =>
            {
                Sfx.Reel();
                DOTween.Sequence()
                    .Join(_catch.DOJumpAnchorPos(to, CatchArc / 2f, 1, fly))
                    .Join(_catch.DOScale(0.35f, fly).SetEase(TweenEase.Linear))
                    .SetEase(TweenEase.InOutCubic)
                    .OnComplete(() =>
                    {
                        _pending[fish] = Mathf.Max(0, _pending[fish] - 1);
                        FishFlying = Mathf.Max(0, FishFlying - 1);
                        if (FishFlying == 0) _catch.gameObject.SetActive(false);
                        _basket.DOKill();
                        _basket.localScale = Vector3.one;
                        _basket.DOScale(1.15f, Motion.Tap / 2f).SetEase(TweenEase.OutSine).SetLoops(2, LoopType.Yoyo).OnUiClock(_basket);
                        Draw();
                    })
                    .OnUiClock(_catch);
            });
        }

        public void Update(float dt)
        {
            if (!IsOpen) return;
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
                    Ripple();
                    _say = Say.Sink;
                }
            }
            Draw();
        }

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

        void DrawFloat(FishingCast cast)
        {
            bool show = cast != null;
            if (_float.gameObject.activeSelf != show) _float.gameObject.SetActive(show);
            if (!show) return;
            bool sinking = cast.Phase == CastPhase.Sinking;
            float k = sinking ? Mathf.Clamp01((float)((cast.Elapsed - cast.BobSeconds) / Fishing.SinkSeconds)) : 0f;
            float pop = !Motion.Reduced && _popAge < 0.3f ? Ease.Pop(_popAge / 0.3f) : 0f;
            Bobbing(!Motion.Reduced && !sinking);
            if (_picture != null)
            {
                ShowFloat(sinking ? _floatDown : _floatUp);
                if (Motion.Reduced) _floatArt.anchoredPosition = Vector2.zero;
                else if (sinking) _floatArt.anchoredPosition = new Vector2(0f, -SinkDepth * 0.25f * k);
                _floatArt.localScale = Vector3.one * (1f + 0.2f * pop);
                return;
            }
            if (Motion.Reduced)
            {
                _floatArt.anchoredPosition = Vector2.zero;
                _floatArt.localScale = Vector3.one * (sinking ? 0.7f : 1f);
                _cap.color = sinking ? Palette.Lantern : _capColor;
                _body.color = Palette.Paper;
                _ringImage.color = new Color(0.85f, 0.93f, 1f, sinking ? 0.85f : 0.3f);
                _ring.localScale = Vector3.one;
                return;
            }
            if (sinking) _floatArt.anchoredPosition = new Vector2(0f, -SinkDepth * k);
            _floatArt.localScale = Vector3.one * (1f + 0.25f * pop - 0.25f * k);
            _cap.color = sinking ? Palette.Mix(_capColor, Palette.Lantern, 0.6f) : _capColor;
            _body.color = Palette.Alpha(Palette.Paper, 1f - 0.6f * k);
            if (!sinking) return;
            _ringImage.color = new Color(0.85f, 0.93f, 1f, 0.6f + 0.3f * k);
            _ring.localScale = Vector3.one * (1f + 0.5f * k);
        }
    }
}

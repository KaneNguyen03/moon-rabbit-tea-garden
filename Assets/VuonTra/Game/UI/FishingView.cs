using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class FishingView
    {
        public const float Height = 1380f;
        public const float ShowSeconds = 0.5f, FlySeconds = 0.55f;
        const float SinkDepth = 46f;
        public const float RippleSeconds = 2f * RingGap + RingLife;
        const float RingGap = 0.18f, RingLife = 0.72f;
        const float FloatArtScale = 0.3f, RingArtScale = 0.45f, WaterArtMax = 1.5f;
        const float DishY = -69f, LineUpY = 37f, LineDownY = 27f;
        static readonly Color[] FishTints =
        {
            new Color(0.78f, 0.82f, 0.88f), new Color(0.98f, 0.62f, 0.30f), new Color(0.97f, 0.66f, 0.72f),
            new Color(0.96f, 0.90f, 0.62f), new Color(0.98f, 0.80f, 0.25f), new Color(0.36f, 0.40f, 0.78f),
        };

        enum Say { Hint, Wait, Sink, Early, Lost, Caught, Released }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly RectTransform _card, _water, _float, _floatArt, _ring, _catch, _basket, _line, _ripples;
        readonly Image _cap, _body, _ringImage, _catchImage;
        readonly Text _baits, _next, _status, _castLabel;
        readonly Image[] _dots = new Image[Fishing.BaitCap];
        readonly Text[] _counts;
        readonly Image[] _fishIcons;
        readonly Image[] _picks;
        readonly ChunkyButton _releaseButton;
        readonly Text _releaseLabel, _tankLine;
        int _chosen = -1, _shownRelease = -1;
        string _releasedLine = "";
        readonly int[] _pending;
        readonly ChunkyButton _castButton;
        readonly Image _picture;
        readonly Image[] _rings;
        readonly Sprite _floatUp, _floatDown;
        readonly Color _capColor = Palette.Mix(Palette.Carrot, new Color(0.85f, 0.2f, 0.2f), 0.6f);
        Say _say;
        string _caughtLine = "";
        float _clock, _popAge = 9f, _rippleAge = 99f;
        Sprite _shownFloat;
        int _shownBaits = -1, _shownCast = -1;
        long _shownNext = -1;

        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        public string Status => _status.text;
        public RectTransform BasketIcon => _basket;
        public int Chosen => _chosen;
        public bool ReleaseEnabled => _releaseButton.Button.interactable;
        public string ReleaseLabel => _releaseLabel.text;
        public string TankLine => _tankLine.text;
        public RectTransform TankSpot => _tankLine.rectTransform;
        public RectTransform IconOf(int fish) => _fishIcons[fish].rectTransform;
        public RectTransform ReleaseButton => _releaseButton.Root;
        public System.Action<ReleaseResult> Released;
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
            _picks = new Image[Fishing.Kinds];

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
            _floatArt = UiKit.Rect("Art", _float).Stretch(0, 0, 1, 1);
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
                _picks[i] = slot.AddImage(catalog.Panel, Palette.Alpha(Palette.Lantern, 0f), true);
                _picks[i].raycastTarget = true;
                int fish = i;
                var pick = slot.gameObject.AddComponent<Button>();
                pick.transition = Selectable.Transition.None;
                pick.targetGraphic = _picks[i];
                pick.onClick.AddListener(() => Choose(fish));
                _fishIcons[i] = FishImage(UiKit.Rect("Art", slot).Place(0.5f, 1f, 0, -58, 112, 84), catalog, i);
                _counts[i] = UiKit.Rect("Count", slot).Place(0.5f, 0f, 0, 34, 124, 44)
                    .AddText("×0", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }

            var release = UiKit.Rect("Release", _card).Place(0.5f, 1f, -230, -1240, 500, 100);
            _releaseButton = UiKit.Chunky(release, catalog.Panel, Release);
            _releaseLabel = UiKit.Rect("Text", _releaseButton.FaceRect).Stretch(0, 0, 1, 1, 16, 0, 16, 0)
                .AddText(L10n.T("tank.release"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _releaseLabel.resizeTextForBestFit = true;
            _releaseLabel.resizeTextMinSize = 14;
            _releaseLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _tankLine = UiKit.Rect("Tank", _card).Place(0.5f, 1f, 260, -1236, 440, 64)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);
            _tankLine.resizeTextForBestFit = true;
            _tankLine.resizeTextMinSize = 12;
            _tankLine.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _tankLine.verticalOverflow = VerticalWrapMode.Truncate;

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

        void Ripple()
        {
            if (_rings.Length > 0) _rippleAge = 0f;
        }

        void DrawRipples()
        {
            for (int i = 0; i < _rings.Length; i++)
            {
                bool show;
                if (Motion.Reduced)
                {
                    show = i == Mathf.Min(1, _rings.Length - 1) && _rippleAge < RippleSeconds;
                    if (show)
                    {
                        _rings[i].transform.localScale = Vector3.one;
                        _rings[i].color = new Color(1f, 1f, 1f, 0.8f);
                    }
                }
                else
                {
                    float u = (_rippleAge - i * RingGap) / RingLife;
                    show = u >= 0f && u < 1f;
                    if (show)
                    {
                        _rings[i].transform.localScale = Vector3.one * Mathf.Lerp(0.8f, 1.15f, Ease.OutCubic(u));
                        _rings[i].color = new Color(1f, 1f, 1f, 0.85f * Mathf.Min(1f, u / 0.12f) * (1f - u));
                    }
                }
                if (_rings[i].gameObject.activeSelf != show) _rings[i].gameObject.SetActive(show);
            }
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
            _rippleAge = 99f;
            for (int i = 0; i < _pending.Length; i++) _pending[i] = 0;
            FishFlying = 0;
            _chosen = -1;
            _shownRelease = -1;
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
                _castButton.Shake(_tweens);
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

        int Have(int fish) => Mathf.Max(0, _runner.Basket[fish] - _pending[fish]);

        public void Choose(int fish)
        {
            if (!IsOpen || fish < 0 || fish >= Fishing.Kinds) return;
            if (Have(fish) <= 0)
            {
                Sfx.Deny();
                return;
            }
            Sfx.Click();
            _chosen = fish;
            RectTransform icon = _fishIcons[fish].rectTransform;
            _tweens.Add(Motion.Tap, t => { if (icon != null) icon.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (icon != null) icon.localScale = Vector3.one; });
            Draw();
        }

        public void Release()
        {
            int fish = _chosen;
            if (!IsOpen || fish < 0 || Have(fish) <= 0)
            {
                Sfx.Deny();
                _releaseButton.Shake(_tweens);
                return;
            }
            ReleaseResult result = _runner.ReleaseFish(fish);
            if (!result.Released)
            {
                Sfx.Deny();
                _releaseButton.Shake(_tweens);
                return;
            }
            _releaseButton.Press(_tweens);
            _say = Say.Released;
            string line = L10n.T("tank.released", L10n.T("fish." + fish), Aquarium.Released(_runner.State, fish));
            _releasedLine = result.Milestones > 0 ? L10n.T("tank.milestone", AquariumView.Percent(result.Bonus)) : line;
            RectTransform tank = _tankLine.rectTransform;
            _tweens.Add(Motion.Tap, t => { if (tank != null) tank.localScale = Vector3.one * (1f + 0.1f * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (tank != null) tank.localScale = Vector3.one; });
            Released?.Invoke(result);
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
            _tweens.Add(Motion.Time(0.25f), t => { if (_catch != null) _catch.localScale = Vector3.one * size * t; }, Motion.Curve(Ease.OutBack));
            bool reeling = false;
            _tweens.Add(Motion.Time(FlySeconds), t =>
            {
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

        public void Update(float dt)
        {
            if (!IsOpen) return;
            _clock += dt;
            _popAge += dt;
            _rippleAge += dt;
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
            if (_chosen < 0 || Have(_chosen) <= 0) _chosen = FirstToRelease();
            for (int i = 0; i < Fishing.Kinds; i++)
            {
                int have = Have(i);
                string count = "×" + have;
                if (_counts[i].text != count) _counts[i].text = count;
                Color c = _fishIcons[i].sprite == _catalog.Round ? FishTints[i] : Color.white;
                _fishIcons[i].color = have > 0 ? c : Palette.Alpha(c, 0.3f);
                _picks[i].color = i == _chosen ? Palette.Alpha(Palette.Lantern, 0.55f) : Palette.Alpha(Palette.Lantern, 0f);
            }
            DrawRelease();
            _status.text = StatusLine(cast, baits);
            DrawFloat(cast);
            DrawRipples();
        }

        int FirstToRelease()
        {
            for (int i = 0; i < Fishing.Kinds; i++) if (Have(i) > 0) return i;
            return -1;
        }

        void DrawRelease()
        {
            GameState s = _runner.State;
            bool can = _chosen >= 0 && _runner.CanReleaseFish(_chosen);
            int shown = (can ? 1 : 0) + 2 * (Aquarium.KindsIn(s) + 8 * Aquarium.Reached(s));
            if (shown == _shownRelease) return;
            _shownRelease = shown;
            _releaseButton.SetEnabled(can);
            _releaseLabel.color = ChunkyButton.InkFor(can);
            string label = can ? L10n.T("tank.release") : L10n.T("tank.release.none");
            if (_releaseLabel.text != label) _releaseLabel.text = label;
            string tank = L10n.T("tank.basket", Aquarium.KindsIn(s), Aquarium.Kinds, AquariumView.Percent(Aquarium.Bonus(s)));
            if (_tankLine.text != tank) _tankLine.text = tank;
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
                case Say.Released: return _releasedLine;
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
            if (_picture != null)
            {
                ShowFloat(sinking ? _floatDown : _floatUp);
                float drawnBob = Motion.Reduced || sinking ? 0f : 7f * Mathf.Sin(_clock * 3.4f) + 3f * Mathf.Sin(_clock * 7.1f + 1f);
                _floatArt.anchoredPosition = Motion.Reduced ? Vector2.zero : new Vector2(0f, drawnBob - SinkDepth * 0.25f * k);
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

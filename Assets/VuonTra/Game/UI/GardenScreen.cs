using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The landscape screen, built in code. A side-scrolling street of stations fills the view,
    /// with layered backdrops that slide at different speeds; the lists (stations, upgrades,
    /// quests, rabbits) live in a sheet that opens over it. Views only read the state and call
    /// GameRunner; no rules live here.
    /// </summary>
    public sealed class GardenScreen : MonoBehaviour
    {
        public GameRunner Runner;
        public Camera Cam;

        const float RefreshInterval = 1f / 10f;
        const int TabCount = 4;
        // The world is a clearing seen from above at an angle, bigger than the screen and dragged
        // around in every direction. Sizes are in reference pixels; the screen is portrait, 1080 × 1920.
        const float ScreenW = 1080f, ScreenH = 1920f;
        // The garden is as wide as the screen and only scrolls up and down.
        const float WorldW = 1080f, WorldH = 2970f; // room below the last station, so it can scroll clear of the docked card
        /// <summary>Stations are drawn at this size so four or five fit on one screen.</summary>
        const float StationScale = 0.7f;
        const float DecorScale = 0.7f;
        const float DozeEnergy = 8f;

        static readonly string[] TabNames = { "Trạm", "Bùa", "Nhiệm vụ", "Thỏ" };

        readonly List<StationActorView> _actors = new List<StationActorView>();
        readonly List<StationRowView> _rows = new List<StationRowView>();
        readonly List<UpgradeRowView> _upgradeRows = new List<UpgradeRowView>();
        readonly List<QuestRowView> _questRows = new List<QuestRowView>();
        /// <summary>Every buy-mode button on the screen (the sheet's bar and the card's), by mode.</summary>
        readonly List<KeyValuePair<int, Image>> _modeButtons = new List<KeyValuePair<int, Image>>();
        readonly List<KeyValuePair<int, Text>> _modeLabels = new List<KeyValuePair<int, Text>>();
        /// <summary>The summary before upgrading everything.</summary>
        public UpgradeAllView UpgradeAll { get; private set; }
        /// <summary>The product tray above the tab bar.</summary>
        public TrayView ProductTray { get; private set; }

        // ----- photo mode -----
        /// <summary>The HUD and the tab bar are away so the garden can be framed and snapped.</summary>
        public bool PhotoMode { get; private set; }
        /// <summary>Where pictures are written. Null: the Pictures folder of the machine.</summary>
        public string PhotoFolder;
        /// <summary>The last picture written, or null.</summary>
        public string LastPhotoPath { get; private set; }
        GameObject _photoBar, _photoReview;
        Image _photoPreview;
        Text _photoNote, _photoGift;
        Texture2D _photoTexture;
        RectTransform _upgradeAllRow;
        readonly GameObject[] _lists = new GameObject[TabCount];
        readonly Text[] _openLabels = new Text[TabCount];
        readonly Image[] _tabIcons = new Image[TabCount];
        readonly RectTransform[] _tabLifts = new RectTransform[TabCount];
        readonly CanvasGroup[] _tabLiftGroups = new CanvasGroup[TabCount];
        /// <summary>How far each tab has come towards its chosen look (0 resting, 1 chosen), and a short pop when pressed.</summary>
        readonly float[] _tabShown = new float[TabCount], _tabPop = new float[TabCount];
        float _tabFade = 1f, _badgePop, _safeBottom = -1f;
        int _shownClaimable = -1;
        RectTransform _barBadgeRect, _rabbitBadgeRect;
        Text _rabbitBadgeText;
        const float TabBarHeight = 170f, TabBarGap = 16f, TabLabelSize = 26f;
        readonly List<Image> _tierChips = new List<Image>();
        readonly List<Text> _tierLabels = new List<Text>();
        readonly List<Image> _tierTradeBadges = new List<Image>();
        readonly List<Image> _tierTradeIcons = new List<Image>();
        // Life in the world: rabbits out for a walk, and ingredients carried between stations.
        sealed class Walker { public RectTransform Rect; public Image Art; public float X0, X1, Y, Speed, Phase; }

        /// <summary>
        /// Where resting rabbits sit, as offsets from the teapot. The two stumps by the tea table come with the
        /// garden; then a picnic rug by the pond, mushroom stools, a hammock and a swing, one for each place added.
        /// </summary>
        static readonly Vector2[] Seats =
        {
            new Vector2(246, -1082), new Vector2(-60, -1116),
            new Vector2(150, 880), new Vector2(402, -1052), new Vector2(-376, -1196), new Vector2(372, -1272),
        };
        /// <summary>For the places that bring their own furniture: how tall it is drawn, and how far the rabbit sits above its foot.</summary>
        static readonly float[] SeatPropHeight = { 0, 0, 104, 124, 150, 196 };
        static readonly Vector2[] SeatPropLift = { default, default, new Vector2(0, 26), new Vector2(-38, 56), new Vector2(-4, 46), new Vector2(2, 60) };
        /// <summary>The rabbit on the swing is drawn a little smaller so it fits between the posts.</summary>
        static readonly float[] SeatScale = { 1f, 1f, 1f, 1f, 1f, 0.82f };
        readonly List<GameObject> _seatNaps = new List<GameObject>();
        readonly List<RectTransform> _seats = new List<RectTransform>();
        readonly List<Image> _seatArt = new List<Image>();
        readonly List<GameObject> _seatCushions = new List<GameObject>();
        readonly long[] _seatIds = new long[RestSpots.Max];
        int _seated;
        sealed class Courier { public RectTransform Rect; public Vector2 From, To; public float T, Duration; public int Target; }
        readonly List<Walker> _walkers = new List<Walker>();
        readonly List<Courier> _couriers = new List<Courier>();
        readonly List<Vector2Int> _supplyRoutes = new List<Vector2Int>(); // ingredient station → brew station
        float _courierTimer;
        int _nextRoute;

        // The card that appears over the station the player tapped.
        RectTransform _card;
        Text _cardName, _cardLevel, _cardInfo, _cardBuyTop, _cardBuyCost;
        ChunkyButton _cardBuy;
        // Small events: a golden firefly to catch, and rabbits that nod off.
        RectTransform _gold;
        float _goldTimer = 25f, _goldAge, _dozeTimer = 18f;
        const float GoldSeconds = 12f;

        /// <summary>Index of the station whose card is open, or -1.</summary>
        public int Selected { get; private set; } = -1;

        public Tweens Tweens { get; } = new Tweens();
        public FeelLayer Feel { get; private set; }
        public ShellView Shell { get; private set; }
        public ItemPickerView Picker { get; private set; }
        /// <summary>The list of the player's rabbits, opened from a station's empty pad or its card.</summary>
        public WorkerPickerView Workers { get; private set; }
        public RenameView Rename { get; private set; }
        /// <summary>The player's profile: avatar, garden name, player code, backup.</summary>
        public ProfileView Profile { get; private set; }
        public RabbitDetailView Detail { get; private set; }
        /// <summary>The moon gate a newly called or merged rabbit steps out of.</summary>
        public SummonMomentView Arrival { get; private set; }
        /// <summary>Kinds met for the first time and not yet looked at in the album: their cells are marked.</summary>
        readonly HashSet<string> _freshKinds = new HashSet<string>();
        public bool IsFreshKind(string kindId) => _freshKinds.Contains(kindId);
        /// <summary>0 = stations, 1 = upgrades ("bùa"), 2 = quests, 3 = rabbits.</summary>
        public int Tab { get; private set; }
        /// <summary>True from the moment a sheet is asked for until it is asked to close (the slide out comes after).</summary>
        public bool SheetOpen { get; private set; }
        /// <summary>How tall the open sheet stands, in canvas units from the bottom of the screen.</summary>
        public float SheetHeight { get; private set; }
        /// <summary>The sheet opens at about 60% of the screen, so the garden stays in view above it.</summary>
        public const float SheetHalf = ScreenH * 0.6f;
        /// <summary>Pulled all the way up it stops under the HUD.</summary>
        public const float SheetFull = ScreenH - HudCover - 12f;
        const float SheetLeast = 260f, SheetSink = 90f, SheetFling = 1100f, SheetFoot = 196f;
        /// <summary>How far the garden's bottom edge may rise while a sheet is open, so the lowest stations can come above it.</summary>
        const float SheetLift = SheetHalf - TabCover;
        /// <summary>The moon pond at the top of the garden, where the camera starts from.</summary>
        public static Vector2 PondSpot => Heart + new Vector2(330f, 900f);
        int _sheetRun, _glideRun;
        float _sheetDragSpeed;

        RectTransform _canvas, _street, _hud, _purse, _currency, _panel, _tabBar, _energyFill, _plotRow;
        RawImage _groundTile;
        readonly List<RectTransform> _fireflies = new List<RectTransform>();
        readonly List<Vector2> _glowSpots = new List<Vector2>(); // lanterns and other lights the fireflies gather round
        RawImage _moonbeams;
        CanvasGroup _hintGroup;
        bool _hintFading;
        readonly List<Image> _fireflyImages = new List<Image>();
        CanvasGroup _sheetGroup;
        Text _coins, _perSecond, _awayAmount, _awayTime, _barBadgeText, _clovers, _energyLabel, _energyAction, _questToday, _questAchievements;
        RectTransform _questReady, _cardModes;
        ChunkyButton _claimAll;
        Text _plotTitle, _plotSub, _plotCost;
        ChunkyButton _plotBuy, _energyButton;
        GameObject _hint, _away, _sheet, _modes, _barBadge;
        RabbitGridView _rabbitList;
        Text _sheetTitle;
        float _sinceRefresh, _clock;
        double _shownCoins;
        int _shownStations = -1;
        bool _built;

        void Start()
        {
            if (!_built) Build();
            Runner.StateChanged += RefreshNow;
            Runner.Payout += OnPayout;
            Runner.ProductMade += OnProductMade;
            Runner.AwayClaimed += ShowAway;
            L10n.Changed += RebuildUi;
            RefreshNow();
        }

        void OnDestroy()
        {
            L10n.Changed -= RebuildUi;
            if (Runner == null) return;
            Runner.StateChanged -= RefreshNow;
            Runner.Payout -= OnPayout;
            Runner.ProductMade -= OnProductMade;
            Runner.AwayClaimed -= ShowAway;
        }

        void Update()
        {
            HandleZoomInput(Time.unscaledDeltaTime);
            Advance(Time.unscaledDeltaTime);
        }

        /// <summary>One frame of UI time. Public so tests and editor previews can step it.</summary>
        public void Advance(float dt)
        {
            _clock += dt;
            GameState s = Runner.State;
            bool boosting = s.BoostSecondsLeft > 0d;
            for (int i = 0; i < _actors.Count; i++) _actors[i].Animate(s.Progress[i], _clock, dt, boosting);

            Tweens.Update(dt);
            PlaceTabBar();
            ShowTabBar(dt);
            if (Arrival != null) Arrival.Update(dt);
            Feel.Update(dt);
            Shell.Update(_clock);
            UpdateWorld(dt);
            UpdateFireflies();

            // The counter counts up instead of snapping.
            double target = s.Coins.ToDouble();
            _shownCoins = System.Math.Abs(target - _shownCoins) < 0.5 || target > 1e15
                ? target
                : _shownCoins + (target - _shownCoins) * Mathf.Clamp01(dt * 10f);
            _coins.text = target > 1e15 ? s.Coins.Short() : new BigNum(_shownCoins).Short();

            _sinceRefresh += dt;
            if (_sinceRefresh >= RefreshInterval) Refresh(_sinceRefresh);
        }

        /// <summary>The teapot stands here, in the middle of the garden; everything else is placed around it.</summary>
        static readonly Vector2 Heart = new Vector2(540f, 1620f);

        /// <summary>
        /// Where each station stands, as an offset from the teapot, in the order they open. Placed by hand:
        /// the first ring opens first and later stations spread outward, and every ingredient station
        /// sits between the stations it supplies.
        /// </summary>
        /// <summary>The worn paths: out from the teapot to the first ring, then on to the far corners.</summary>
        static readonly Vector2Int[] Paths =
        {
            new Vector2Int(0, 4), new Vector2Int(0, 6), new Vector2Int(0, 2), new Vector2Int(0, 7),
            new Vector2Int(4, 9), new Vector2Int(2, 11),
        };

        static readonly Vector2[] Spots =
        {
            new Vector2(0, 0),       //  0 teapot: the heart
            new Vector2(326, 70),    //  1 leaf basket: beside the teapot, below the milk tea stall
            new Vector2(-276, -372), //  2 jam pot
            new Vector2(12, -548),   //  3 flower mortar: between the jam pot and the star jar
            new Vector2(-292, 408),  //  4 chestnut oven
            new Vector2(-336, 20),   //  5 beehive: between the oven and the jam pot
            new Vector2(270, 432),   //  6 moon milk tea
            new Vector2(296, -366),  //  7 star jar
            new Vector2(24, 690),    //  8 grape vine: between the milk tea and the bakery
            new Vector2(-270, 850),  //  9 mooncake bakery
            new Vector2(246, -770),  // 10 flower spring: between the star jar and the observatory
            new Vector2(-214, -900), // 11 star observatory
        };

        /// <summary>Position of station <paramref name="i"/> in the world.</summary>
        public static Vector2 StationSpot(int i)
        {
            if (i >= 0 && i < Spots.Length) return Heart + Spots[i];
            // More stations than the table knows: carry on downward rather than stack them up.
            return Heart + new Vector2((i % 2 == 0 ? -240f : 240f), -1000f - (i - Spots.Length) * 280f);
        }

        /// <summary>How large a station is drawn. The teapot is the centrepiece.</summary>
        public static float StationSize(int i) => i == 0 ? 1f : StationScale;

        /// <summary>How much of the screen the HUD covers at the top and the tab bar at the bottom, and how tall the card stands.</summary>
        const float WheelScroll = 60f;
        /// <summary>The strip at the top of the sheet: the page name, the buy modes, the close button.</summary>
        const float HeaderHeight = 148f;
        const float HudCover = 160f, TabCover = 250f, CardHeight = 340f;

        public enum ZoomLevel { Far, Default, Near }

        /// <summary>Far shows the whole garden top to bottom; near is for looking at one rabbit.</summary>
        public const float ZoomFar = ScreenH / WorldH, ZoomDefault = 1f, ZoomNear = 1.5f;
        float _zoom = 1f;
        Text _zoomMark, _levelText;
        Image _levelRing;
        RectTransform _levelBadge;
        int _shownGardenLevel = -1;
        readonly List<CanvasGroup> _fog = new List<CanvasGroup>();
        readonly List<GameObject> _fogTrees = new List<GameObject>();
        int _fogPlots = -1;
        bool _wheelLatched;
        float _wheelQuiet;
        RectTransform _cardWorker;
        Image _cardLevelChip, _cardWorkerArt, _cardWorkerFrame;
        Text _cardWorkerName, _cardWorkerSub;
        ScrollRect _scroll;
        ZoomLevel _shownZoom = ZoomLevel.Default;
        float _lastGroundTapAt = -10f;
        bool _pinching;
        float _pinchStartDistance, _pinchStartZoom, _wheelWait;
        Vector2 _pinchAt;

        public float ZoomScale => _zoom;
        public ZoomLevel Zoom => _zoom < 0.85f ? ZoomLevel.Far : (_zoom > 1.2f ? ZoomLevel.Near : ZoomLevel.Default);

        static float ScaleOf(ZoomLevel level) => level == ZoomLevel.Far ? ZoomFar : (level == ZoomLevel.Near ? ZoomNear : ZoomDefault);

        /// <summary>Where a point of the garden is on the screen (canvas units from the bottom left).</summary>
        Vector2 ToView(Vector2 world) => world * _zoom + _street.anchoredPosition;

        /// <summary>Keeps the view inside the garden; when the garden is smaller than the screen it sits in the middle.</summary>
        static Vector2 ClampView(Vector2 pos, float zoom, float lift = 0f)
        {
            float w = WorldW * zoom, h = WorldH * zoom;
            return new Vector2(
                w <= ScreenW ? (ScreenW - w) / 2f : Mathf.Clamp(pos.x, ScreenW - w, 0f),
                h <= ScreenH ? (ScreenH - h) / 2f : Mathf.Clamp(pos.y, ScreenH - h, lift));
        }

        /// <summary>Scales the garden so the point under <paramref name="pivotView"/> stays where it is.</summary>
        void ApplyZoom(float zoom, Vector2 pivotView)
        {
            Vector2 world = (pivotView - _street.anchoredPosition) / _zoom;
            PlaceView(world, pivotView, zoom);
        }

        /// <summary>Scales the garden and puts a garden point at a screen point, as far as the edges allow.</summary>
        void PlaceView(Vector2 world, Vector2 atView, float zoom)
        {
            _glideRun++; // the view was put somewhere on purpose: the opening glide lets go
            _zoom = Mathf.Clamp(zoom, ZoomFar, ZoomNear);
            _street.localScale = new Vector3(_zoom, _zoom, 1f);
            _street.anchoredPosition = ClampView(atView - world * _zoom, _zoom);
            if (_scroll != null) _scroll.horizontal = WorldW * _zoom > ScreenW + 1f;
            if (Zoom != _shownZoom) ShowZoomDetail();
        }

        /// <summary>Far away the plates and names would be specks, so they go; up close the worn items show too.</summary>
        void ShowZoomDetail()
        {
            _shownZoom = Zoom;
            if (_zoomMark != null) _zoomMark.text = _shownZoom == ZoomLevel.Far ? "–" : (_shownZoom == ZoomLevel.Near ? "+" : "");
            for (int i = 0; i < _actors.Count; i++)
            {
                Transform plate = _actors[i].Root.Find("Level");
                if (plate != null) plate.gameObject.SetActive(_shownZoom != ZoomLevel.Far);
            }
            if (Runner != null && Runner.State != null && _built) RefreshStreet(Runner.Defs, Runner.State);
        }

        /// <summary>Goes to one of the three zoom levels, keeping a screen point (the middle by default) in place.</summary>
        public void SetZoom(ZoomLevel level, bool animate = true, Vector2? pivotView = null)
        {
            Vector2 pivot = pivotView ?? new Vector2(ScreenW / 2f, ScreenH / 2f);
            float from = _zoom, to = ScaleOf(level);
            if (_scroll != null) _scroll.StopMovement();
            if (!animate) { ApplyZoom(to, pivot); return; }
            Vector2 world = (pivot - _street.anchoredPosition) / _zoom;
            Tweens.Add(Motion.Time(Motion.Travel), t => { if (_street != null) PlaceView(world, pivot, Mathf.LerpUnclamped(from, to, t)); }, Ease.InOutCubic);
        }

        /// <summary>The zoom button: far, usual, near, and round again.</summary>
        public void CycleZoom()
        {
            SelectStation(-1);
            SetZoom((ZoomLevel)(((int)Zoom + 1) % 3));
            DismissHint();
        }

        /// <summary>
        /// A tap on open ground closes the card. Two in quick succession zoom in on that spot, or back out.
        /// (Stations do not zoom on a double tap: tapping a rabbit fast is how you speed it up.)
        /// </summary>
        void OnGroundTap()
        {
            SelectStation(-1);
            bool second = _clock - _lastGroundTapAt < 0.35f;
            _lastGroundTapAt = second ? -10f : _clock;
            if (!second) return;
            Vector2 at = new Vector2(ScreenW / 2f, ScreenH / 2f);
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) at = ScreenToView(touch.primaryTouch.position.ReadValue());
            else if (mouse != null && Application.isPlaying) at = ScreenToView(mouse.position.ReadValue());
            SetZoom(Zoom == ZoomLevel.Near ? ZoomLevel.Default : ZoomLevel.Near, true, at);
            DismissHint();
        }

        /// <summary>Flies to a station and looks at it up close.</summary>
        public void ZoomToStation(int index, bool animate = true)
        {
            if (index < 0 || index >= _actors.Count) return;
            Vector2 world = StationSpot(index) + new Vector2(0, 40f * StationSize(index));
            Vector2 to = new Vector2(ScreenW / 2f, ScreenH * 0.5f), fromAt = ToView(world);
            float from = _zoom;
            if (_scroll != null) _scroll.StopMovement();
            if (!animate) { PlaceView(world, to, ZoomNear); return; }
            Tweens.Add(Motion.Time(Motion.Travel), t => { if (_street != null) PlaceView(world, Vector2.LerpUnclamped(fromAt, to, t), Mathf.LerpUnclamped(from, ZoomNear, t)); }, Ease.InOutCubic);
        }

        /// <summary>Pinch on a phone, wheel on a desktop. Both land on one of the three levels.</summary>
        void HandleZoomInput(float dt)
        {
            if (_street == null || Shell == null || Shell.State != ShellState.Playing || SheetOpen) return;
            _wheelWait -= dt;

            var touch = UnityEngine.InputSystem.Touchscreen.current;
            int down = 0;
            Vector2 a = default, b = default;
            if (touch != null)
                foreach (var finger in touch.touches)
                {
                    if (!finger.press.isPressed) continue;
                    if (down == 0) a = finger.position.ReadValue(); else if (down == 1) b = finger.position.ReadValue();
                    down++;
                }
            if (down >= 2)
            {
                float distance = Mathf.Max(1f, Vector2.Distance(a, b));
                _pinchAt = ScreenToView((a + b) / 2f);
                if (!_pinching)
                {
                    _pinching = true;
                    _pinchStartDistance = distance;
                    _pinchStartZoom = _zoom;
                    if (_scroll != null) { _scroll.StopMovement(); _scroll.enabled = false; } // one gesture at a time
                }
                ApplyZoom(_pinchStartZoom * distance / _pinchStartDistance, _pinchAt);
                DismissHint();
                return;
            }
            if (_pinching)
            {
                _pinching = false;
                if (_scroll != null) _scroll.enabled = true;
                SetZoom(Zoom, true, _pinchAt); // settle on the nearest level
                return;
            }

            // The wheel (or two fingers on a trackpad) scrolls the garden. Held with Cmd or Ctrl it zooms,
            // and a whole swipe, however many small steps it sends, changes one level.
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var keys = UnityEngine.InputSystem.Keyboard.current;
            bool held = keys != null && (keys.leftCommandKey.isPressed || keys.rightCommandKey.isPressed || keys.leftCtrlKey.isPressed || keys.rightCtrlKey.isPressed);
            if (_scroll != null) _scroll.scrollSensitivity = held ? 0f : WheelScroll;
            float wheel = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (!held || Mathf.Abs(wheel) < 0.01f)
            {
                _wheelQuiet += dt;
                if (_wheelQuiet > 0.25f) _wheelLatched = false; // the swipe is over
                return;
            }
            _wheelQuiet = 0f;
            if (_wheelLatched) return;
            _wheelLatched = true;
            int step = (int)Zoom + (wheel > 0f ? 1 : -1);
            if (step < 0 || step > 2) return;
            SetZoom((ZoomLevel)step, true, ScreenToView(mouse.position.ReadValue()));
        }

        /// <summary>A pointer position in screen pixels, as canvas units from the bottom left of the garden view.</summary>
        Vector2 ScreenToView(Vector2 screen)
        {
            var view = (RectTransform)_street.parent;
            Canvas canvas = _canvas.GetComponent<Canvas>();
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(view, screen, canvas != null ? canvas.worldCamera : null, out Vector2 local))
                return new Vector2(ScreenW / 2f, ScreenH / 2f);
            return local - view.rect.min;
        }

        /// <summary>Moves the view to a point in the garden (used by the preview shots).</summary>
        public void LookAt(Vector2 point) => CenterOn(point);

        /// <summary>Moves the view so a garden point sits a little above the middle of the screen, clear of the tab bar.</summary>
        void CenterOn(Vector2 point)
        {
            _glideRun++;
            _street.anchoredPosition = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - point * _zoom, _zoom);
        }

        /// <summary>The grass follows the drag, the walkers walk, and ingredients travel to where they are used.</summary>
        void UpdateWorld(float dt)
        {
            Vector2 offset = -_street.anchoredPosition / _zoom;
            _groundTile.uvRect = new Rect(offset.x / 512f, offset.y / 512f, ScreenW / 512f / _zoom, ScreenH / 512f / _zoom);

            for (int k = 0; k < _seats.Count; k++)
            {
                if (!_seats[k].gameObject.activeSelf) continue;
                _seats[k].localRotation = Quaternion.Euler(0, 0, Mathf.Sin(_clock * 0.8f + k * 1.9f) * 3.2f);
                bool dozing = Mathf.Repeat(_clock / 11f + k * 0.29f, 1f) < 0.3f;
                if (_seatNaps[k].activeSelf != dozing) _seatNaps[k].SetActive(dozing);
            }

            for (int i = 0; i < _walkers.Count; i++)
            {
                Walker w = _walkers[i];
                // Walker i is the rabbit from seat i, out for a stroll part of the time; its seat is empty meanwhile.
                bool strolling = i < _seated && Strolling(i, _clock);
                if (w.Rect.gameObject.activeSelf != strolling) w.Rect.gameObject.SetActive(strolling);
                if (i < _seats.Count && i < _seated && _seats[i].gameObject.activeSelf == strolling) _seats[i].gameObject.SetActive(!strolling);
                if (!strolling) continue;
                float span = w.X1 - w.X0, travelled = _clock * w.Speed + w.Phase;
                float along = Mathf.PingPong(travelled, span);
                bool goingRight = Mathf.FloorToInt(travelled / span) % 2 == 0;
                w.Rect.anchoredPosition = new Vector2(w.X0 + along, w.Y + Mathf.Abs(Mathf.Sin(_clock * 7f + w.Phase)) * 12f);
                w.Rect.localScale = new Vector3(goingRight ? 1f : -1f, 1f, 1f);
            }

            if (Shell.State == ShellState.Playing)
            {
                _courierTimer -= dt;
                if (_courierTimer <= 0f)
                {
                    _courierTimer = 1.1f;
                    SendCourier();
                }
            }
            UpdateEvents(dt);
            for (int i = _couriers.Count - 1; i >= 0; i--)
            {
                Courier c = _couriers[i];
                c.T += dt / c.Duration;
                if (c.T >= 1f)
                {
                    if (c.Target < _actors.Count) _actors[c.Target].PopProp();
                    Discard(c.Rect.gameObject);
                    _couriers.RemoveAt(i);
                    continue;
                }
                // Little hops along the way.
                Vector2 pos = Vector2.Lerp(c.From, c.To, c.T);
                pos.y += Mathf.Abs(Mathf.Sin(c.T * Mathf.PI * 4f)) * 34f;
                c.Rect.anchoredPosition = pos;
            }
        }

        /// <summary>Whether the rabbit of a seat is out walking at this moment: a little under half of every 36 seconds, each at its own time.</summary>
        public static bool Strolling(int seat, float clock)
        {
            return Mathf.Repeat(clock / 36f + seat * 0.37f, 1f) < 0.42f;
        }

        /// <summary>One ingredient sets off from its station to a station that uses it, when both are open.</summary>
        void SendCourier()
        {
            if (_supplyRoutes.Count == 0 || _couriers.Count >= 8) return;
            GameState s = Runner.State;
            for (int tries = 0; tries < _supplyRoutes.Count; tries++)
            {
                Vector2Int route = _supplyRoutes[_nextRoute];
                _nextRoute = (_nextRoute + 1) % _supplyRoutes.Count;
                if (s.Levels[route.x] <= 0 || s.Levels[route.y] <= 0) continue;

                GameCatalog catalog = Runner.Catalog;
                Vector2 from = StationSpot(route.x) + new Vector2(112, -70) * StationSize(route.x), to = StationSpot(route.y) + new Vector2(112, -70) * StationSize(route.y);
                var rt = UiKit.Rect("Courier", _street).Place(0f, 0f, from.x, from.y, 46, 46);
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, -2, 40, 12).AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.35f));
                UiKit.Rect("Item", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Stations[route.x].Prop, Color.white).preserveAspect = true;
                _couriers.Add(new Courier { Rect = rt, From = from, To = to, Target = route.y, Duration = Mathf.Max(1.2f, Vector2.Distance(from, to) / 260f) });
                return;
            }
        }

        /// <summary>Now and then a golden firefly crosses the screen, and a working rabbit nods off.</summary>
        void UpdateEvents(float dt)
        {
            if (Shell.State != ShellState.Playing) return;

            if (_gold.gameObject.activeSelf)
            {
                _goldAge += dt;
                float k = _goldAge / GoldSeconds;
                _gold.anchoredPosition = new Vector2(-80f + k * (ScreenW + 160f), ScreenH * 0.55f + Mathf.Sin(k * 9f) * 160f);
                _gold.localScale = Vector3.one * (1f + 0.15f * Mathf.Sin(_clock * 6f));
                if (k >= 1f) _gold.gameObject.SetActive(false);
            }
            else
            {
                _goldTimer -= dt;
                if (_goldTimer <= 0f) SpawnGoldFirefly();
            }

            _dozeTimer -= dt;
            if (_dozeTimer <= 0f)
            {
                _dozeTimer = 22f + Hash(_clock) * 16f;
                DozeSomeone();
            }
        }

        /// <summary>
        /// One working rabbit nods off, picked from wherever the clock happens to point. False when nobody
        /// is at work: an empty station has no one to fall asleep.
        /// </summary>
        public bool DozeSomeone()
        {
            GameState s = Runner.State;
            int start = Mathf.FloorToInt(Hash(_clock + 3f) * _actors.Count);
            for (int n = 0; n < _actors.Count; n++)
            {
                int i = (start + n) % _actors.Count;
                if (s.Levels[i] > 0 && Rabbits.AtStation(s, i) != null && !_actors[i].Dozing && _actors[i].Root.gameObject.activeSelf)
                {
                    _actors[i].SetDozing(true);
                    return true;
                }
            }
            return false;
        }

        /// <summary>Sends a golden firefly across the screen. Catching it pays about 45 seconds of income.</summary>
        public void SpawnGoldFirefly()
        {
            _goldTimer = 45f + Hash(_clock + 11f) * 35f;
            _goldAge = 0f;
            _gold.gameObject.SetActive(true);
            _gold.anchoredPosition = new Vector2(-80f, ScreenH * 0.55f);
        }

        void OnCatchGold()
        {
            if (!_gold.gameObject.activeSelf) return;
            _gold.gameObject.SetActive(false);
            Modifiers m = Runner.Mods;
            BigNum perSecond = Economy.IncomePerSecond(Runner.Defs, Runner.State) * (m.IncomeMultiplier * m.SpeedMultiplier);
            BigNum prize = perSecond * 45d;
            if (prize < new BigNum(20)) prize = new BigNum(20);
            Runner.Grant(prize);
            Sfx.Sting();
            Feel.Moment(L10n.T("gold.title"), L10n.T("gold.sub", prize.Short()));
        }

        void BuildEvents(GameCatalog catalog)
        {
            _gold = UiKit.Rect("GoldFirefly", _canvas).Place(0f, 0f, -80, 0, 150, 150);
            Image halo = _gold.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.9f));
            halo.raycastTarget = true; // generous: the whole glow is the target
            var button = _gold.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(OnCatchGold);
            UiKit.Rect("Core", _gold).Place(0.5f, 0.5f, 0, 0, 34, 34).AddImage(catalog.Round, Palette.Mix(Palette.Lantern, Color.white, 0.7f));
            _gold.gameObject.SetActive(false);
        }

        static void Discard(GameObject go)
        {
            go.SetActive(false);
            if (Application.isPlaying) Destroy(go);
            else DestroyImmediate(go);
        }

        /// <summary>
        /// Throws the whole UI away and builds it again, keeping where the player was. Used when
        /// the language changes: most labels are written once when the screen is built.
        /// </summary>
        public void RebuildUi()
        {
            if (!_built) return;
            ShellState before = Shell.State;
            bool sheetWasOpen = SheetOpen;
            int tab = Tab;
            Vector2 streetAt = _street.anchoredPosition;

            Tweens.Clear(); // running tweens point at objects that are about to be destroyed
            GameObject old = _canvas.gameObject;
            old.name = "Canvas (old)"; // so lookups by path find the new one straight away
            old.SetActive(false);
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);

            _actors.Clear(); _rows.Clear(); _upgradeRows.Clear(); _questRows.Clear();
            _modeButtons.Clear(); _modeLabels.Clear();
            UpgradeAll = null;
            ProductTray = null;
            PhotoMode = false;
            _tierChips.Clear(); _tierLabels.Clear(); _tierTradeBadges.Clear(); _tierTradeIcons.Clear();
            _walkers.Clear(); _seats.Clear(); _seatArt.Clear(); _seatCushions.Clear(); _seatNaps.Clear(); _seated = 0; _couriers.Clear(); _supplyRoutes.Clear(); _glowSpots.Clear();
            Selected = -1;
            _shownStations = -1;
            // The old helpers belong to the destroyed canvas; Build makes new ones.
            Feel = null;
            Picker = null;
            Workers = null;
            Rename = null;
            Profile = null;
            Detail = null;
            Arrival = null;
            Shell = null;

            Build();
            if (before != ShellState.Title) Shell.SkipTitle();
            Refresh(-1f);
            _glideRun++; // a rebuilt screen keeps the view it had
            _street.anchoredPosition = ClampView(streetAt, _zoom);
            ShowZoomDetail();
            if (sheetWasOpen) OpenSheet(tab);
            if (before == ShellState.Paused) Shell.OpenPause(); // back to the card the language buttons are on
        }

        /// <summary>Creates every UI object. Safe to call from the editor for a preview.</summary>
        public void Build()
        {
            _built = true;
            GameCatalog catalog = Runner.Catalog;
            UiKit.ShadowSprite = catalog.Shadow;

            if (Application.isPlaying && FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }

            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)) { layer = 5 };
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = Cam;
            canvas.planeDistance = 5f;
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(ScreenW, ScreenH);
            scaler.matchWidthOrHeight = 0.5f;
            _canvas = (RectTransform)canvasGo.transform;

            BuildBackdrop(catalog);
            Motion.Load();
            Haptics.Load();
            BuildWorld(catalog);
            BuildAtmosphere(catalog);
            BuildStationCard(catalog);
            BuildEvents(catalog);
            BuildHud(catalog);
            BuildTabBar(catalog);
            ProductTray = new TrayView(_canvas, catalog, OnSellProduct, OnSellAllProducts);
            BuildPhotoMode(catalog);
            ProductTray.Root.anchoredPosition = new Vector2(0f, SafeBottom() + TabBarGap + TabBarHeight + 10f); // just above the tab bar
            BuildSheet(catalog);
            Feel = new FeelLayer(_canvas, catalog, Tweens);
            Arrival = new SummonMomentView(_canvas, catalog);
            Detail = new RabbitDetailView(_canvas, catalog, Tweens, OnLevelUp, OnLevelMax,
                rabbitId => { Sfx.Click(); Rename.Open(Runner.State, rabbitId); },
                (rabbitId, slot) => { Sfx.Click(); Picker.Open(Runner.State, rabbitId, slot); },
                (rabbitId, station) => { Sfx.Blip(); Runner.AssignRabbit(rabbitId, station); },
                rabbitId => { Sfx.Click(); Runner.AssignRabbit(rabbitId, -1); });
            Picker = new ItemPickerView(_canvas, catalog, Tweens,
                (itemId, rabbitId) => { Sfx.Blip(); Runner.EquipItem(itemId, rabbitId); },
                itemId => { Sfx.Click(); Runner.UnequipItem(itemId); });
            Rename = new RenameView(_canvas, catalog, Tweens, (rabbitId, name) => { Sfx.Blip(); Runner.RenameRabbit(rabbitId, name); });
            Workers = new WorkerPickerView(_canvas, catalog, Tweens, (rabbitId, station) => { Sfx.Blip(); Runner.AssignRabbit(rabbitId, station); });
            UpgradeAll = new UpgradeAllView(_canvas, catalog, Tweens, share => Runner.SetSpendShare(share), OnUpgradeAll);
            BuildAwayPopup(catalog);
            Shell = new ShellView(_canvas, catalog, Tweens, () => Runner.ResetGame(), () => Profile.Open(Runner.Defs, Runner.State, Runner.Account));
            // Built after the shell so it draws over the pause card it opens from; the rename card is moved above it.
            Profile = new ProfileView(_canvas, catalog, Tweens,
                () => { Runner.NextAvatar(); Profile.Refresh(Runner.Defs, Runner.State, Runner.Account); },
                () => Rename.Open(L10n.T("profile.rename"), Runner.State.Profile.GardenName, VuonTra.Core.Profile.MaxNameLength, name =>
                {
                    Sfx.Blip();
                    Runner.RenameGarden(name);
                    Profile.Refresh(Runner.Defs, Runner.State, Runner.Account);
                }),
                () => Runner.Account.Link(ok => Profile.Refresh(Runner.Defs, Runner.State, Runner.Account)));
            Rename.Root.SetAsLastSibling();
            Shell.Changed += OnShellChanged;
            Sfx.Attach(gameObject);

            // Title state: the street runs behind it, the HUD and tab bar wait off screen.
            _hud.gameObject.SetActive(false);
            _tabBar.gameObject.SetActive(false);
            _shownCoins = Runner.State.Coins.ToDouble();
        }

        void BuildBackdrop(GameCatalog catalog)
        {
            var rt = UiKit.Rect("Ground", _canvas).Stretch(0, 0, 1, 1);
            _groundTile = rt.gameObject.AddComponent<RawImage>();
            _groundTile.texture = catalog.BgGrass;
            _groundTile.color = catalog.BgGrass != null ? Color.white : Palette.Grass;
            _groundTile.raycastTarget = false;
        }

        void BuildWorld(GameCatalog catalog)
        {
            var view = UiKit.Rect("Garden", _canvas).Stretch(0, 0, 1, 1);
            view.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true; // gives the scroll something to drag
            var ground = view.gameObject.AddComponent<Button>();
            ground.transition = Selectable.Transition.None;
            ground.onClick.AddListener(OnGroundTap);
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = true;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = WheelScroll;
            _scroll = scroll;

            _street = UiKit.Rect("Content", view);
            _street.anchorMin = _street.anchorMax = _street.pivot = Vector2.zero;
            _street.sizeDelta = new Vector2(WorldW, WorldH);
            _street.localScale = new Vector3(_zoom, _zoom, 1f);
            scroll.horizontal = WorldW * _zoom > ScreenW + 1f;
            scroll.content = _street;
            scroll.viewport = view;

            int count = catalog.Stations.Length;

            // A few worn paths lead out from the teapot; they lie flat on the grass, under everything.
            Color stone = Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Grass, 0.5f), 0.6f);
            foreach (Vector2Int path in Paths)
            {
                if (path.x >= count || path.y >= count) continue;
                Vector2 a = StationSpot(path.x) + new Vector2(0, -150f * StationSize(path.x)), b = StationSpot(path.y) + new Vector2(0, -150f * StationSize(path.y));
                int steps = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(a, b) / 56f));
                for (int k = 1; k < steps; k++)
                {
                    Vector2 at = Vector2.Lerp(a, b, k / (float)steps) + new Vector2(0, Mathf.Sin(k * 1.7f) * 7f);
                    UiKit.Rect("Stone", _street).Place(0f, 0f, at.x, at.y, 38, 15).AddImage(catalog.Round, stone);
                }
            }

            // Everything that stands up is drawn back to front: the lower on screen, the nearer.
            var standing = new List<KeyValuePair<float, Transform>>();

            // Land the garden has not grown into yet lies under mist with a few trees on it. The mist is on the
            // ground, under everything that stands, so the outline of the next station still shows through.
            _fog.Clear(); _fogTrees.Clear(); _fogPlots = -1;
            Color mist = new Color(0.66f, 0.74f, 0.88f, 0.62f);
            for (int i = 0; i < count; i++)
            {
                if (i < Plots.Starting) { _fog.Add(null); _fogTrees.Add(null); continue; }
                Vector2 spot = StationSpot(i);
                // Several banks of mist, each a different size and thickness, overlapping so no edge reads as a picture.
                var bank = UiKit.Rect("Mist " + i, _street).Place(0f, 0f, spot.x, spot.y, 0, 0);
                CanvasGroup veil = bank.gameObject.AddComponent<CanvasGroup>();
                veil.blocksRaycasts = false;
                if (catalog.Mist != null)
                {
                    float ratio = catalog.Mist.rect.height / catalog.Mist.rect.width;
                    float[] dx = { -70f, 90f, 10f, -20f }, dy = { 30f, 10f, -70f, 110f }, wide = { 520f, 460f, 600f, 400f }, thick = { 0.7f, 0.58f, 0.74f, 0.46f };
                    for (int k = 0; k < dx.Length; k++)
                    {
                        Image patch = UiKit.Rect("Patch", bank).Place(0.5f, 0.5f, dx[k] + (i % 2) * 24f, dy[k], wide[k], wide[k] * ratio)
                            .AddImage(catalog.Mist, new Color(0.86f, 0.9f, 1f, thick[k]));
                        if ((k + i) % 2 == 1) patch.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // flipped, so neighbours differ
                    }
                }
                else UiKit.Rect("Patch", bank).Place(0.5f, 0.5f, 6f, -10f, 620f, 470f).AddImage(catalog.Glow, mist);
                _fog.Add(veil);
                var grove = UiKit.Rect("Grove " + i, _street).Place(0f, 0f, 0, 0, 0, 0);
                if (catalog.Decor != null && catalog.Decor.Length > 0 && catalog.Decor[0] != null)
                {
                    Color dim = Palette.Mix(Color.white, Palette.Night, 0.3f);
                    // The trees stand on the station's own ground, so they never reach into a neighbour that is open.
                    AddDecor(grove, catalog.Decor[0], spot.x - 66f, spot.y - 40f, 190f, dim);
                    AddDecor(grove, catalog.Decor[0], spot.x + 84f, spot.y - 70f, 165f, dim);
                }
                _fogTrees.Add(grove.gameObject);
                standing.Add(new KeyValuePair<float, Transform>(spot.y - 40f, grove));
            }
            BuildDecor(catalog, count, standing);

            for (int i = 0; i < count; i++)
            {
                Vector2 spot = StationSpot(i);
                var actor = new StationActorView(i);
                actor.Build(_street, catalog, catalog.Stations[i], OnTap, OnPad);
                actor.Root.Place(0f, 0f, spot.x, spot.y, 440, 360);
                actor.BaseScale = StationSize(i);
                actor.Root.localScale = Vector3.one * StationSize(i);
                // Every station's level reads at the same size, clear of the rabbit's feet and the prop.
                actor.PlacePlate(0.78f / StationSize(i), new Vector2(0, -26f));
                _actors.Add(actor);
                standing.Add(new KeyValuePair<float, Transform>(spot.y - 112f * StationSize(i), actor.Root));

                // Every name plate is the same size on screen and floats the same distance above its rabbit.
                var chip = UiKit.Rect("Tier", actor.Root).Place(0.5f, 0.5f, -72, 122 + 40f / StationSize(i), 200, 44);
                chip.localScale = Vector3.one * (0.78f / StationSize(i));
                _tierChips.Add(chip.AddImage(catalog.Panel, Palette.Paper, true));
                _tierLabels.Add(UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1, 34, 0, 6, 0).AddText("", 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold));
                // The rabbit's trade, at the head of its name: gold when this is a station of that trade, grey when not.
                var badge = UiKit.Rect("Trade", chip).Place(0f, 0.5f, 4, 0, 50, 50);
                _tierTradeBadges.Add(badge.AddImage(catalog.Round, Palette.Lantern));
                Image tradeIcon = UiKit.Rect("Icon", badge).Stretch(0, 0, 1, 1, 9, 9, 9, 9).AddImage(null, Palette.Ink);
                tradeIcon.preserveAspect = true;
                _tierTradeIcons.Add(tradeIcon);
                chip.gameObject.SetActive(false);

                // Where this station's ingredients go, for the couriers.
                StationData[] boosts = catalog.Stations[i].Boosts;
                if (boosts == null) continue;
                foreach (StationData target in boosts)
                {
                    int to = System.Array.IndexOf(catalog.Stations, target);
                    if (to >= 0) _supplyRoutes.Add(new Vector2Int(i, to));
                }
            }

            // Resting rabbits sit about the garden. Each seat is a place a rabbit can be tapped.
            Color shade = Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.35f);
            for (int k = 0; k < Seats.Length && k < RestSpots.Max; k++)
            {
                int seat = k;
                Vector2 at = Heart + Seats[k];
                // The later places bring their own furniture, which appears with the place.
                RectTransform prop = UiKit.Rect("SeatProp " + k, _street);
                Sprite propArt = catalog.RestProps != null && k < catalog.RestProps.Length ? catalog.RestProps[k] : null;
                if (propArt != null && SeatPropHeight[k] > 0f)
                {
                    float h = SeatPropHeight[k], w = h * propArt.rect.width / propArt.rect.height;
                    prop.Place(0f, 0f, at.x, at.y + h / 2f, w, h).AddImage(propArt, Color.white);
                    at += SeatPropLift[k]; // the rabbit sits on it, not at its foot
                }
                else
                {
                    prop.Place(0f, 0f, at.x, at.y + 4f, 118, 38).AddImage(catalog.Round, Palette.Mix(Palette.Blush, Palette.Paper, 0.35f));
                }
                prop.gameObject.SetActive(false);
                _seatCushions.Add(prop.gameObject);
                RectTransform cushion = prop;

                var rt = UiKit.Rect("Seat " + k, _street).Place(0f, 0f, at.x, at.y, 124, 132);
                rt.pivot = new Vector2(0.5f, 0f);
                Image hit = rt.AddImage(null, new Color(0, 0, 0, 0));
                hit.raycastTarget = true;
                var seatButton = rt.gameObject.AddComponent<Button>();
                seatButton.transition = Selectable.Transition.None;
                seatButton.onClick.AddListener(() =>
                {
                    if (_seatIds[seat] <= 0) return;
                    Sfx.Click();
                    Detail.Open(Runner.Defs, Runner.State, _seatIds[seat]);
                });
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 4, 84, 20).AddImage(catalog.Round, shade);
                Image art = UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white);
                art.preserveAspect = true;
                rt.localScale = Vector3.one * SeatScale[k];
                // Resting looks different from working: a slow sway, and now and then a doze.
                var nap = UiKit.Rect("Nap", rt).Place(1f, 1f, 6, -6, 60, 34);
                nap.AddImage(catalog.Panel, Palette.Paper, true);
                UiKit.Rect("Text", nap).Stretch(0, 0, 1, 1).AddText("z z", 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                nap.gameObject.SetActive(false);
                _seatNaps.Add(nap.gameObject);
                _seats.Add(rt);
                _seatArt.Add(art);
                rt.gameObject.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>((Heart + Seats[k]).y + 60f, cushion)); // the furniture is always behind whoever sits on it
                standing.Add(new KeyValuePair<float, Transform>((Heart + Seats[k]).y, rt));
            }

            // Now and then a seated rabbit gets up for a stroll along a stretch of grass that no station stands on.
            float[] lanes = { Heart.y + 212f, Heart.y - 312f };
            float[] laneFrom = { 390f, 400f }, laneTo = { 700f, 715f };
            for (int i = 0; i < lanes.Length && catalog.Rabbit != null; i++)
            {
                var rt = UiKit.Rect("Walker", _street).Place(0f, 0f, 400, lanes[i], 104, 110);
                rt.pivot = new Vector2(0.5f, 0f);
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 2, 68, 16).AddImage(catalog.Round, shade);
                Image art = UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white);
                art.preserveAspect = true;
                _walkers.Add(new Walker { Rect = rt, Art = art, X0 = laneFrom[i], X1 = laneTo[i], Y = lanes[i], Speed = 50f + i * 14f, Phase = i * 900f });
                rt.gameObject.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>(float.MaxValue, rt)); // drawn first, so a walker never covers a level plate
            }

            standing.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Transform> item in standing) item.Value.SetAsLastSibling();

            BuildFireflies(catalog);
            CenterOn(StationSpot(0));
            // Behind the title the garden shows its moon pond; starting the game glides down from there.
            _street.anchoredPosition = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.5f) - PondSpot * _zoom, _zoom);

            var hint = UiKit.Rect("Hint", _canvas).Place(0.5f, 1f, 0, -196, 940, 60);
            hint.AddShadow(14f, 8f, 0.28f);
            UiKit.Rect("Body", hint).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1).AddText(L10n.T("hint"), 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint = hint.gameObject;
            _hintGroup = hint.gameObject.AddComponent<CanvasGroup>();
            _hintGroup.blocksRaycasts = false;
            // The first drag also counts as having understood.
            var drag = view.gameObject.AddComponent<UnityEngine.EventSystems.EventTrigger>();
            var entry = new UnityEngine.EventSystems.EventTrigger.Entry { eventID = UnityEngine.EventSystems.EventTriggerType.BeginDrag };
            entry.callback.AddListener(_ => { _glideRun++; DismissHint(); });
            drag.triggers.Add(entry);
        }

        /// <summary>The ground a station takes up, plates included, in garden units.</summary>
        static Rect StationBox(int i)
        {
            float size = StationSize(i);
            Vector2 spot = StationSpot(i);
            return Rect.MinMaxRect(spot.x - 160f * size, spot.y - 215f * size, spot.x + 178f * size, spot.y + 196f * size);
        }

        /// <summary>The hint is for the very first moments only: it fades at the first tap or drag and never returns.</summary>
        public void DismissHint()
        {
            if (Runner != null && Runner.State != null) Runner.State.HintSeen = true;
            if (_hint == null || !_hint.activeSelf || _hintFading) return;
            _hintFading = true;
            Tweens.Add(0.45f, t => { if (_hintGroup != null) _hintGroup.alpha = 1f - t; },
                Ease.OutCubic, 0f, () => { if (_hint != null) _hint.SetActive(false); _hintFading = false; if (_hintGroup != null) _hintGroup.alpha = 1f; });
        }

        /// <summary>
        /// The card of the tapped station. It is docked just above the tab bar, nearly as wide as the screen, and the
        /// garden scrolls so the station stands above it: it can never land on a neighbouring station.
        /// </summary>
        void BuildStationCard(GameCatalog catalog)
        {
            _card = UiKit.Rect("Card", _canvas).Place(0.5f, 0f, 0, TabCover + CardHeight / 2f, 1040, CardHeight);
            _card.AddShadow(18f, 10f, 0.4f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            Image face = UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 10, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            face.raycastTarget = true; // a tap on the card is not a tap on the ground behind it

            _cardName = UiKit.Rect("Name", _card).Stretch(0, 1, 1, 1, 32, -70, 200, 14)
                .AddText("", 36, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardName.horizontalOverflow = HorizontalWrapMode.Overflow;
            var chip = UiKit.Rect("Level", _card).Place(1f, 1f, -104, -42, 160, 46);
            _cardLevelChip = chip.AddImage(catalog.Panel, Palette.Moss, true);
            _cardLevel = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _cardInfo = UiKit.Rect("Info", _card).Stretch(0, 1, 1, 1, 32, -138, 32, 74)
                .AddText("", 24, Palette.InkSoft, TextAnchor.UpperLeft);
            // The same buy modes as the Stations tab; one choice, remembered in both places.
            _cardModes = UiKit.Rect("Modes", _card).Stretch(0, 1, 1, 1, 0, -202, 0, 142);
            BuildModeBar(_cardModes, catalog, -32, 0, 60);

            // Who works here: the rabbit's face, its name, and its kind and trade.
            _cardWorker = UiKit.Rect("Worker", _card).Place(0f, 0f, 262, 74, 460, 96);
            Image workerBack = _cardWorker.AddImage(catalog.Panel, Palette.Track, true);
            workerBack.raycastTarget = true; // the second place to put a rabbit to work
            var workerButton = _cardWorker.gameObject.AddComponent<Button>();
            workerButton.transition = Selectable.Transition.None;
            workerButton.targetGraphic = workerBack;
            workerButton.onClick.AddListener(() => OnPad(Selected));
            var portrait = UiKit.Rect("Portrait", _cardWorker).Place(0f, 0.5f, 50, 0, 80, 80);
            _cardWorkerFrame = portrait.AddImage(catalog.Round, Palette.Paper);
            _cardWorkerArt = UiKit.Rect("Art", portrait).Stretch(0, 0, 1, 1, 6, 6, 6, 6).AddImage(null, Color.white);
            _cardWorkerArt.preserveAspect = true;
            _cardWorkerName = UiKit.Rect("Name", _cardWorker).Stretch(0, 0.5f, 1, 1, 104, 2, 12, 8)
                .AddText("", 26, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _cardWorkerName.horizontalOverflow = HorizontalWrapMode.Overflow;
            _cardWorkerSub = UiKit.Rect("Sub", _cardWorker).Stretch(0, 0, 1, 0.5f, 104, 10, 12, 4)
                .AddText("", 20, Palette.InkSoft, TextAnchor.UpperLeft);
            _cardWorkerSub.resizeTextForBestFit = true;
            _cardWorkerSub.resizeTextMinSize = 13;
            _cardWorkerSub.resizeTextMaxSize = 20;

            var buy = UiKit.Rect("Buy", _card).Place(1f, 0f, -272, 74, 480, 96);
            _cardBuy = UiKit.Chunky(buy, catalog.Panel, () => OnQuick(Selected));
            _cardBuy.Dip.Denied = () => Deny(_cardBuy, true);
            // Held down on an open station, the button keeps upgrading; land and opening are one press each.
            _cardBuy.Dip.Repeat = () =>
            {
                GameState s = Runner.State;
                if (Selected >= 0 && Selected < s.Plots && s.Levels[Selected] > 0) OnBuy(Selected);
            };
            _cardBuyTop = UiKit.Rect("Top", _cardBuy.FaceRect).Stretch(0, 0, 0.5f, 1, 24, 0, 0, 0)
                .AddText("", 28, Palette.Card, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardBuyCost = UiKit.Rect("Cost", _cardBuy.FaceRect).Stretch(0.4f, 0, 1, 1, 0, 0, 64, 0)
                .AddText("", 32, Palette.Card, TextAnchor.MiddleRight, FontStyle.Bold);
            UiKit.Rect("Carrot", _cardBuy.FaceRect).Place(1f, 0.5f, -36, 0, 42, 42).AddImage(catalog.Currency, Color.white).preserveAspect = true;

            _card.gameObject.SetActive(false);
        }

        /// <summary>Brings a station to the middle of the screen and opens its card.</summary>
        public void FocusStation(int index)
        {
            if (index < 0 || index >= _actors.Count) return;
            CenterOn(StationSpot(index));
            SelectStation(index);
        }

        /// <summary>Opens the card over a station, or closes it with -1.</summary>
        public void SelectStation(int index)
        {
            if (index < 0 || index >= _actors.Count || !_actors[index].Root.gameObject.activeSelf)
            {
                Selected = -1;
                _card.gameObject.SetActive(false);
                return;
            }
            bool changed = Selected != index;
            Selected = index;
            _card.gameObject.SetActive(true);
            if (changed)
            {
                // Slide the garden so the station stands in the open strip between the HUD and the card.
                Rect station = StationBox(index);
                Vector2 from = _street.anchoredPosition;
                float top = station.yMax * _zoom + from.y, bottom = station.yMin * _zoom + from.y;
                float x = StationSpot(index).x * _zoom + from.x, floor = TabCover + CardHeight + 14f;
                var move = new Vector2(Mathf.Clamp(x, 290f, ScreenW - 290f) - x, 0f);
                if (bottom < floor) move.y = floor - bottom;
                else if (top > ScreenH - HudCover) move.y = ScreenH - HudCover - top;
                // The garden may rise past its own foot, as far as the card covers, so the lowest station clears the card too.
                Vector2 target = ClampView(from + move, _zoom, floor);
                if (_scroll != null) _scroll.StopMovement();
                _glideRun++;
                Tweens.Add(Motion.Time(Motion.Panel), t => { if (_street != null) _street.anchoredPosition = Vector2.LerpUnclamped(from, target, t); }, Ease.InOutCubic);
                Tweens.Add(0.22f, t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.92f, 1f, t); }, Ease.OutBack);
            }
            RefreshCard(Runner.Defs, Runner.State, Runner.Mods);
        }

        void RefreshCard(IReadOnlyList<StationDef> defs, GameState s, Modifiers m)
        {
            if (Selected < 0) return;
            int i = Selected, level = s.Levels[i];
            bool needsPlot = i >= s.Plots;
            _cardName.text = L10n.Station(defs[i].Id);
            _cardLevel.text = needsPlot ? L10n.T("station.empty") : (level <= 0 ? L10n.T("row.locked") : L10n.T("station.level", level));
            // Green would read as "all good"; a station that is not open yet is a quiet grey-purple.
            _cardLevelChip.color = level <= 0 || needsPlot ? Palette.Mix(Palette.Night, Palette.Paper, 0.45f) : Palette.Moss;

            // Who works here, and how many levels a press buys: neither means anything before the station is open.
            bool open = level > 0 && !needsPlot;
            if (_cardWorker.gameObject.activeSelf != open) _cardWorker.gameObject.SetActive(open);
            if (_cardModes.gameObject.activeSelf != open) _cardModes.gameObject.SetActive(open);
            if (open)
            {
                Rabbit worker = Rabbits.AtStation(s, i);
                _cardWorkerFrame.gameObject.SetActive(worker != null);
                if (worker == null)
                {
                    _cardWorkerName.text = L10n.T("card.noworker");
                    _cardWorkerSub.color = Palette.InkSoft;
                    _cardWorkerSub.text = L10n.T("card.noworker.sub", L10n.Trade(defs[i].Trade));
                }
                else
                {
                    bool inTrade = Rabbits.InTrade(defs, worker);
                    _cardWorkerArt.sprite = Runner.Catalog.RabbitArt(worker);
                    _cardWorkerFrame.color = Palette.Tiers[worker.Tier];
                    _cardWorkerName.text = worker.Name;
                    _cardWorkerSub.text = L10n.T("card.worker", L10n.Kind(worker), L10n.Trade(Rabbits.TradeOf(worker)))
                        + (inTrade ? L10n.T("rabbits.intrade", UiKit.Times(Rabbits.TradeBonus)) : "");
                    _cardWorkerSub.color = inTrade ? Palette.Mix(Palette.Moss, Palette.Ink, 0.45f) : Palette.InkSoft;
                }
            }

            BigNum cost;
            bool can;
            if (needsPlot)
            {
                cost = Plots.CostOfNext(defs, s);
                can = Expansion.CanExpand(defs, s);
                _cardInfo.text = ExpansionNeeds(defs, s);
                _cardBuyTop.text = L10n.T("plot.buy");
            }
            else if (level <= 0)
            {
                cost = Economy.CostOfNext(defs[i], 0, 1);
                bool previousOpen = Economy.CanUnlock(s, i);
                can = previousOpen && s.Coins >= cost;
                _cardInfo.text = previousOpen ? StationRowView.Describe(defs, s, i, 1, m) : L10n.T("row.needprev");
                _cardBuyTop.text = L10n.T("row.open");
            }
            else
            {
                int count = Simulation.BuyCount(defs, s, i);
                cost = Economy.CostOfNext(defs[i], level, count);
                can = s.Coins >= cost;
                int next = Economy.NextMilestone(level);
                _cardInfo.text = StationRowView.Describe(defs, s, i, level, m) + (next > 0 ? L10n.T("row.milestone", next) : "");
                _cardBuyTop.text = L10n.T("row.upgrade", count);
            }
            // Short of carrots and nothing else: the plate says how many are missing.
            bool shortOnly = !can && s.Coins < cost && (needsPlot ? Expansion.Next(defs, s) != null && Expansion.HasLevel(Expansion.Next(defs, s), s) && Expansion.HasRabbits(Expansion.Next(defs, s), s) : (level > 0 || Economy.CanUnlock(s, i)));
            if (shortOnly) _cardBuyTop.text = L10n.T("need.more");
            _cardBuyCost.text = shortOnly ? (cost - s.Coins).Short() : cost.Short();
            Color ink = ChunkyButton.InkFor(can);
            _cardBuyTop.color = ink;
            _cardBuyCost.color = ink;
            _cardBuy.SetEnabled(can);
        }

        void BuildPhotoMode(GameCatalog catalog)
        {
            // A small plate at the foot of the screen while framing: snap, or done.
            var bar = UiKit.Rect("PhotoBar", _canvas).Place(0.5f, 0f, 0, 150, 640, 130);
            bar.AddShadow(18f, 10f, 0.3f);
            UiKit.Rect("Lip", bar).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Plate", bar).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Hint", bar).Stretch(0, 1, 1, 1, 0, -6, 0, -44).AddText(L10n.T("photo.hint"), UiKit.TextSmall, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow();
            var done = UiKit.Rect("Done", bar).Place(0f, 0.5f, 130, 4, 200, 76);
            UiKit.Secondary(done, catalog.Panel, L10n.T("photo.done"), () => { Sfx.Click(); ExitPhotoMode(); });
            var snap = UiKit.Rect("Snap", bar).Place(1f, 0.5f, -170, 4, 280, 96);
            ChunkyButton snapButton = UiKit.Chunky(snap, catalog.Panel, Snap);
            UiKit.Rect("Text", snapButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("photo.snap"), UiKit.TextTitle, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _photoBar = bar.gameObject;
            _photoBar.SetActive(false);

            // The picture just taken, with what to do with it.
            var overlay = UiKit.Rect("PhotoReview", _canvas).Stretch(0, 0, 1, 1);
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 40, 920, 1360);
            UiKit.Rect("Lip", card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            var frame = UiKit.Rect("Frame", card).Place(0.5f, 1f, 0, -40 - 460, 820, 920);
            frame.AddImage(catalog.Panel, Palette.Card, true);
            _photoPreview = UiKit.Rect("Picture", frame).Stretch(0, 0, 1, 1, 14, 14, 14, 14).AddImage(null, Color.white);
            _photoPreview.preserveAspect = true;
            _photoGift = UiKit.Rect("Gift", card).Stretch(0, 0, 1, 0, 40, 290, 40, -340)
                .AddText("", UiKit.TextBody, Palette.Mix(Palette.Moss, Palette.Ink, 0.45f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _photoNote = UiKit.Rect("Note", card).Stretch(0, 0, 1, 0, 40, 236, 40, -286)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _photoNote.resizeTextForBestFit = true;
            _photoNote.resizeTextMinSize = 12;
            _photoNote.resizeTextMaxSize = UiKit.TextSmall;
            var save = UiKit.Rect("Save", card).Place(0f, 0f, 190, 150, 280, 84);
            UiKit.Secondary(save, catalog.Panel, L10n.T("photo.save"), () => { Sfx.Click(); RevealPhoto(); });
            var share = UiKit.Rect("Share", card).Place(0.5f, 0f, 0, 150, 280, 84);
            UiKit.Secondary(share, catalog.Panel, L10n.T("photo.share"), () => { Sfx.Click(); CopyPhoto(); });
            var close = UiKit.Rect("Close", card).Place(1f, 0f, -190, 150, 280, 96);
            ChunkyButton closeButton = UiKit.Chunky(close, catalog.Panel, () => { Sfx.Click(); _photoReview.SetActive(false); _photoBar.SetActive(true); });
            UiKit.Rect("Text", closeButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("photo.close"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _photoReview = overlay.gameObject;
            _photoReview.SetActive(false);
        }

        /// <summary>Everything but the garden goes away, and the snap bar comes up. Only while playing.</summary>
        public void EnterPhotoMode()
        {
            if (Shell.State != ShellState.Playing || PhotoMode) return;
            PhotoMode = true;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            DismissHint();
            _hud.gameObject.SetActive(false);
            _tabBar.gameObject.SetActive(false);
            ProductTray.Root.gameObject.SetActive(false);
            _photoBar.SetActive(true);
        }

        public void ExitPhotoMode()
        {
            if (!PhotoMode) return;
            PhotoMode = false;
            _photoReview.SetActive(false);
            _photoBar.SetActive(false);
            _hud.gameObject.SetActive(true);
            _tabBar.gameObject.SetActive(true);
            Refresh();
        }

        /// <summary>The garden alone, rendered through the game's own camera and written as a PNG.</summary>
        public void Snap()
        {
            if (!PhotoMode) return;
            _photoBar.SetActive(false);
            int width = Screen.width > 0 ? Screen.width : (int)ScreenW, height = Screen.height > 0 ? Screen.height : (int)ScreenH;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            RenderTexture was = Cam.targetTexture;
            Cam.targetTexture = rt;
            Canvas.ForceUpdateCanvases();
            Cam.Render();
            RenderTexture.active = rt;
            if (_photoTexture != null) { if (Application.isPlaying) Destroy(_photoTexture); else DestroyImmediate(_photoTexture); }
            _photoTexture = new Texture2D(width, height, TextureFormat.RGB24, false);
            _photoTexture.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            _photoTexture.Apply();
            RenderTexture.active = null;
            Cam.targetTexture = was;
            rt.Release();

            string folder = string.IsNullOrEmpty(PhotoFolder)
                ? System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures), "VuonTraTho")
                : PhotoFolder;
            string path = null;
            try
            {
                System.IO.Directory.CreateDirectory(folder);
                // One file per picture, even two in the same second.
                string stamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
                path = System.IO.Path.Combine(folder, "vuon-" + stamp + ".png");
                for (int n = 2; System.IO.File.Exists(path); n++) path = System.IO.Path.Combine(folder, "vuon-" + stamp + "-" + n + ".png");
                System.IO.File.WriteAllBytes(path, _photoTexture.EncodeToPNG());
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Không lưu được ảnh: " + e.Message);
            }
            LastPhotoPath = path;

            Sfx.Shutter();
            Haptics.Medium();
            Feel.Flash(Color.white, 0.18f);
            bool gift = Runner.TakePhoto();
            _photoPreview.sprite = Sprite.Create(_photoTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            _photoGift.text = gift ? L10n.T("photo.gift", Photos.GiftClovers) : L10n.T("photo.again");
            _photoNote.text = path != null ? L10n.T("photo.saved", path) : "";
            _photoReview.SetActive(true);
        }

        /// <summary>Opens the folder with the picture selected (Finder on a Mac).</summary>
        void RevealPhoto()
        {
            if (LastPhotoPath == null) return;
            try
            {
                if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
                    System.Diagnostics.Process.Start("open", "-R \"" + LastPhotoPath + "\"");
                else Application.OpenURL("file://" + System.IO.Path.GetDirectoryName(LastPhotoPath));
            }
            catch (System.Exception e) { Debug.LogWarning("Không mở được thư mục ảnh: " + e.Message); }
        }

        /// <summary>Puts the picture itself on the clipboard (a Mac can; elsewhere the path is copied).</summary>
        void CopyPhoto()
        {
            if (LastPhotoPath == null) return;
            try
            {
                if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
                    System.Diagnostics.Process.Start("osascript", "-e 'set the clipboard to (read (POSIX file \"" + LastPhotoPath + "\") as «class PNGf»)'");
                else GUIUtility.systemCopyBuffer = LastPhotoPath;
                _photoNote.text = L10n.T("photo.copied");
            }
            catch (System.Exception e) { Debug.LogWarning("Không chép được ảnh: " + e.Message); }
        }

        /// <summary>Moonlight and darkened corners over the garden, under the HUD.</summary>
        void BuildAtmosphere(GameCatalog catalog)
        {
            // The moon itself is above the canopy; its glow spills in at the top corner.
            UiKit.Rect("MoonGlow", _canvas).Place(1f, 1f, -140, -200, 1300, 1300)
                .AddImage(catalog.Glow, new Color(0.80f, 0.90f, 1f, 0.26f));
            if (catalog.BgVignette != null)
            {
                var v = UiKit.Rect("Vignette", _canvas).Stretch(0, 0, 1, 1);
                var raw = v.gameObject.AddComponent<RawImage>();
                raw.texture = catalog.BgVignette;
                raw.raycastTarget = false;
            }
        }

        /// <summary>A few warm fireflies that live in the garden itself, each circling a lantern or a station.</summary>
        void BuildFireflies(GameCatalog catalog)
        {
            _fireflies.Clear();
            _fireflyImages.Clear();
            if (_glowSpots.Count == 0) return;
            var layer = UiKit.Rect("Fireflies", _street).Stretch(0, 0, 0, 0);
            for (int i = 0; i < 22; i++)
            {
                var rt = UiKit.Rect("Firefly", layer).Place(0f, 0f, 0, 0, 30, 30);
                _fireflyImages.Add(rt.AddImage(catalog.Glow, Palette.Lantern));
                UiKit.Rect("Core", rt).Place(0.5f, 0.5f, 0, 0, 6, 6).AddImage(catalog.Round, Palette.Mix(Palette.Lantern, Color.white, 0.6f));
                _fireflies.Add(rt);
            }
        }

        /// <summary>Each firefly loops slowly around its light and pulses.</summary>
        void UpdateFireflies()
        {
            for (int i = 0; i < _fireflies.Count; i++)
            {
                float seed = i * 7.31f;
                Vector2 home = _glowSpots[i % _glowSpots.Count];
                float angle = _clock * (0.35f + Hash(seed) * 0.4f) + seed, reach = 50f + Hash(seed + 2f) * 80f;
                _fireflies[i].anchoredPosition = home + new Vector2(Mathf.Cos(angle) * reach, Mathf.Sin(angle * 1.3f) * reach * 0.6f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(_clock * (1.3f + Hash(seed + 9f)) + seed);
                _fireflyImages[i].color = Palette.Alpha(Palette.Lantern, 0.2f + 0.6f * pulse);
                _fireflies[i].localScale = Vector3.one * (0.7f + 0.5f * pulse);
            }
        }

        static float Hash(float n)
        {
            float v = Mathf.Sin(n * 12.9898f) * 43758.5453f;
            return v - Mathf.Floor(v);
        }

        Image AddDecor(Transform parent, Sprite sprite, float x, float y, float height, Color tint)
        {
            if (sprite == null) return null;
            float width = height * sprite.rect.width / sprite.rect.height;
            var rt = UiKit.Rect("Decor", parent).Place(0f, 0f, x, y + height / 2f, width, height);
            return rt.AddImage(sprite, tint);
        }

        /// <summary>
        /// Dresses the clearing: a canopy that frames it on every side, lanterns along the path,
        /// a pond that holds the moon, places to sit, and a scatter of smaller things. Placement
        /// is fixed (not random each run) so the garden always looks the same.
        /// </summary>
        void BuildDecor(GameCatalog catalog, int stations, List<KeyValuePair<float, Transform>> standing)
        {
            Sprite[] d = catalog.Decor;
            if (d == null || d.Length < 9) return;
            const int Tree = 0, Bush = 1, Lantern = 2, Mushrooms = 4, Stones = 5, Pond = 6, TeaTable = 7, Stump = 8;
            Color soft = Palette.Mix(Color.white, Palette.Night, 0.12f);

            // The pond lies in the top corner, where the moonlight comes in.
            Vector2 pondAt = PondSpot;

            void Put(int kind, float x, float y, float height, Color tint)
            {
                height *= DecorScale;
                // Nothing tall may stand in front of the pond: the moon in it has to stay in view.
                if (kind == Tree && x > 120f && x < WorldW - 60f && Mathf.Abs(x - pondAt.x) < 200f && y > pondAt.y - 330f && y < pondAt.y + 30f) return;
                if (kind == Lantern)
                {
                    var at = new Vector2(x + 14f * DecorScale, y + 150f * DecorScale);
                    var glow = UiKit.Rect("LanternGlow", _street).Place(0f, 0f, at.x, at.y, 300f, 300f);
                    glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.6f));
                    standing.Add(new KeyValuePair<float, Transform>(y + 0.5f, glow));
                    _glowSpots.Add(at);
                }
                Image img = AddDecor(_street, d[kind], x, y, height, tint);
                if (img != null) standing.Add(new KeyValuePair<float, Transform>(y, img.transform));
            }

            bool Free(float x, float y, float clearX, float below, float above)
            {
                for (int i = 0; i < stations; i++)
                {
                    Vector2 spot = StationSpot(i);
                    if (Mathf.Abs(spot.x - x) < clearX && y > spot.y - below && y < spot.y + above) return false;
                }
                return true;
            }

            // The pond lies flat, so it goes down before anything that stands. It holds the moon.
            AddDecor(_street, d[Pond], pondAt.x, pondAt.y - 85f, 175f, Color.white);
            UiKit.Rect("PondGlow", _street).Place(0f, 0f, pondAt.x + 20f, pondAt.y + 6f, 370f, 270f)
                .AddImage(catalog.Glow, new Color(0.80f, 0.90f, 1f, 0.3f));
            _glowSpots.Add(pondAt + new Vector2(14, 28));

            // Places to sit, in the corners the stations leave free.
            Put(TeaTable, Heart.x + 90f, Heart.y - 1130f, 170f, Color.white);
            Put(Stump, Heart.x + 246f, Heart.y - 1120f, 110f, Color.white);
            Put(Stump, Heart.x - 60f, Heart.y - 1150f, 96f, Color.white);

            // A lantern beside each path, halfway along.
            for (int n = 0; n < Paths.Length; n++)
            {
                Vector2Int path = Paths[n];
                if (path.x >= stations || path.y >= stations) continue;
                if (n == 1 || n == 2) continue; // two by the teapot are enough; four crowd it
                Vector2 a = StationSpot(path.x), b = StationSpot(path.y);
                bool outer = path.x != 0; // the long paths to the far corners run past name plates, so stand further off
                Vector2 mid = (a + b) / 2f + new Vector2(0, outer ? -70f : -30f);
                Vector2 side = new Vector2(-(b - a).y, (b - a).x).normalized * (outer ? 190f : 112f);
                if (side.x * (mid.x - Heart.x) < 0f) side = -side; // on the outer side, away from the teapot
                Vector2 at = mid + side;
                if (path.x == 0 && path.y == 7) at = Heart + new Vector2(-190f, -272f); // the computed spot lands on a level plate; here it is clear of plates and of the upgrade arrow
                Put(Lantern, at.x, at.y, 226f, Color.white);
            }

            // The wood around the clearing closes over every edge.
            for (float x = -40f; x < WorldW + 60f; x += 150f)
            {
                float jitter = Hash(x) * 50f;
                Put(Tree, x + jitter, WorldH - 300f + Hash(x + 5) * 40f, 380f + Hash(x + 9) * 70f, soft);
                Put(Tree, x + 75f - jitter, WorldH - 230f + Hash(x + 6) * 30f, 360f + Hash(x + 4) * 60f, soft);
                Put(Tree, x + jitter, -30f + Hash(x + 7) * 30f, 330f + Hash(x + 2) * 70f, soft);
                Put(Bush, x + 75f - jitter, 30f + Hash(x + 3) * 30f, 130f, soft);
            }
            // Beyond the edges the wood goes on, so zooming out shows forest and not the end of the world.
            for (float y = -120f; y < WorldH + 60f; y += 160f)
                for (int row = 1; row <= 3; row++)
                {
                    float away = 60f + row * 105f + Hash(y + row) * 40f, lift = (row % 2) * 80f;
                    Put(Tree, -away, y + lift, 330f + Hash(y + row * 3) * 90f, soft);
                    Put(Tree, WorldW + away, y + 80f - lift, 330f + Hash(y + row * 5) * 90f, soft);
                }
            for (float y = 180f; y < WorldH - 330f; y += 150f)
            {
                Put(Tree, -50f + Hash(y) * 40f, y, 340f + Hash(y + 4) * 80f, soft);
                Put(Tree, WorldW + 10f + Hash(y + 2) * 40f, y + 60f, 340f + Hash(y + 6) * 80f, soft);
            }

            // Inside: small things wherever there is room that is not a station, a seat or the pond.
            for (int n = 0; n < 60; n++)
            {
                float x = 150f + Hash(n * 3.1f + 1f) * (WorldW - 300f), y = 200f + Hash(n * 5.7f + 2f) * (WorldH - 560f);
                float pick = Hash(n * 9.3f);
                bool tall = pick < 0.10f;
                if (!Free(x, y, 230f, tall ? 380f : 220f, 240f)) continue; // nothing may cover a level plate or a name
                if (Vector2.Distance(new Vector2(x, y), pondAt) < 240f) continue;
                // Keep the places to rest in the open: nothing grows on a seat or in front of the tea table.
                bool onSeat = false;
                foreach (Vector2 seat in Seats)
                    if (Mathf.Abs(Heart.x + seat.x - x) < 190f && y > Heart.y + seat.y - (tall ? 380f : 150f) && y < Heart.y + seat.y + 190f) onSeat = true;
                if (onSeat) continue;
                if (tall) Put(Tree, x, y, 270f + Hash(n + 40f) * 90f, soft);
                else if (pick < 0.46f) Put(Bush, x, y, 104f, soft);
                else if (pick < 0.76f) Put(Mushrooms, x, y, 84f, soft);
                else Put(Stones, x, y, 62f, soft);
            }

            // Stations glow too, and draw their own few fireflies.
            for (int i = 0; i < stations; i += 2) _glowSpots.Add(StationSpot(i) + new Vector2(0, 40));
        }

        void BuildHud(GameCatalog catalog)
        {
            _hud = UiKit.Rect("Hud", _canvas).Stretch(0, 1, 1, 1);
            _hud.pivot = new Vector2(0.5f, 1f);
            _hud.sizeDelta = new Vector2(0, 150);

            // One row: carrots, tea power, zoom, settings. Everything below it is garden.
            // The garden's level: a number in a ring that fills towards the next one.
            var badge = UiKit.Rect("Level", _hud).Place(0f, 1f, 70, -76, 96, 96);
            badge.AddShadow(14f, 10f, 0.3f);
            Image badgeBack = badge.AddImage(catalog.Round, Palette.CardLip);
            badgeBack.raycastTarget = true;
            var badgeButton = badge.gameObject.AddComponent<Button>();
            badgeButton.transition = Selectable.Transition.None;
            badgeButton.targetGraphic = badgeBack;
            badgeButton.onClick.AddListener(() =>
            {
                Sfx.Click();
                GameState now = Runner.State;
                int level = Garden.Level(now);
                Feel.Moment(L10n.T("garden.info.title", level),
                    L10n.T("garden.info.sub", Mathf.CeilToInt((float)(Garden.XpForLevel(level + 1) - now.GardenXp))));
            });
            bool drawn = catalog.LevelBadge != null;
            if (drawn) badgeBack.color = new Color(0, 0, 0, 0); // the picture is the badge; this only catches the tap
            // The ring behind fills towards the next level; with the drawn badge it shows as a glow round its rim.
            _levelRing = UiKit.Rect("Ring", badge).Stretch(0, 0, 1, 1, drawn ? -5 : 3, drawn ? -5 : 3, drawn ? -5 : 3, drawn ? -5 : 3).AddImage(catalog.Round, Palette.Lantern);
            _levelRing.type = Image.Type.Filled;
            _levelRing.fillMethod = Image.FillMethod.Radial360;
            _levelRing.fillOrigin = (int)Image.Origin360.Top;
            _levelRing.fillClockwise = true;
            if (drawn)
            {
                UiKit.Rect("Rim", badge).Stretch(0, 0, 1, 1, 1, 1, 1, 1).AddImage(catalog.Round, Palette.Mix(Palette.Night, Palette.Ink, 0.5f));
                UiKit.Rect("Art", badge).Stretch(0, 0, 1, 1, -4, -4, -4, -4).AddImage(catalog.LevelBadge, Color.white).preserveAspect = true;
                // The number sits in the lower half, which the picture leaves clear for it.
                _levelText = UiKit.Rect("Text", badge).Place(0.5f, 0.5f, 0, -15, 70, 44).AddText("1", 34, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else
            {
                UiKit.Rect("Face", badge).Stretch(0, 0, 1, 1, 13, 13, 13, 13).AddImage(catalog.Round, Palette.Paper);
                _levelText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("1", 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            _levelBadge = badge;

            _purse = UiKit.Rect("Purse", _hud).Place(0f, 1f, 274, -76, 296, 100);
            _purse.AddShadow();
            UiKit.Rect("Lip", _purse).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _purse).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            _currency = UiKit.Rect("Currency", _purse).Place(0f, 0.5f, 44, 4, 60, 60);
            _currency.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _coins = UiKit.Rect("Coins", _purse).Stretch(0, 0.42f, 1, 1, 78, 0, 10, 6)
                .AddText("0", 38, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _perSecond = UiKit.Rect("PerSecond", _purse).Stretch(0, 0, 1, 0.46f, 78, 14, 10, 0)
                .AddText(L10n.T("persec", "0"), 22, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);

            // "Trà lực": the bar and the boost button are one control.
            var energy = UiKit.Rect("Energy", _hud).Place(0f, 1f, 632, -76, 396, 100);
            energy.AddShadow();
            _energyButton = UiKit.Chunky(energy, catalog.Panel, OnBoost);
            _energyLabel = UiKit.Rect("Label", _energyButton.FaceRect).Stretch(0, 0.46f, 0.55f, 1, 22, 0, 0, 6)
                .AddText("", 25, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _energyAction = UiKit.Rect("Action", _energyButton.FaceRect).Stretch(0.45f, 0.46f, 1, 1, 0, 0, 22, 6)
                .AddText("", 23, Palette.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            var track = UiKit.Rect("Track", _energyButton.FaceRect).Band(22, 22, 16, 18);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _energyFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _energyFill.AddImage(catalog.Panel, Palette.Moss, true);

            // A visible way to zoom, for players who would never guess at pinching or double-tapping.
            var camera = UiKit.Rect("Camera", _hud).Place(1f, 1f, -292, -76, 96, 96);
            camera.AddShadow(14f, 10f, 0.3f);
            Image cameraBack = camera.AddImage(catalog.Round, Palette.Paper);
            cameraBack.raycastTarget = true;
            var cameraButton = camera.gameObject.AddComponent<Button>();
            cameraButton.transition = Selectable.Transition.None;
            cameraButton.targetGraphic = cameraBack;
            cameraButton.onClick.AddListener(() => { Sfx.Click(); EnterPhotoMode(); });
            // A little camera: a body, a lens, a bump on top.
            UiKit.Rect("Body", camera).Place(0.5f, 0.5f, 0, -4, 52, 36).AddImage(catalog.Panel, Palette.Ink, true);
            UiKit.Rect("Bump", camera).Place(0.5f, 0.5f, -8, 16, 20, 12).AddImage(catalog.Panel, Palette.Ink, true);
            UiKit.Rect("Lens", camera).Place(0.5f, 0.5f, 0, -4, 22, 22).AddImage(catalog.Round, Palette.Paper);
            UiKit.Rect("Glint", camera).Place(0.5f, 0.5f, -3, -1, 7, 7).AddImage(catalog.Round, Palette.Ink);
            var zoom = UiKit.Rect("Zoom", _hud).Place(1f, 1f, -184, -76, 96, 96);
            zoom.AddShadow(14f, 10f, 0.3f);
            ChunkyButton zoomButton = null;
            zoomButton = UiKit.Chunky(zoom, catalog.Round, () =>
            {
                zoomButton.Press(Tweens);
                Sfx.Click();
                CycleZoom();
            });
            zoomButton.Lip.type = Image.Type.Simple;
            zoomButton.Face.type = Image.Type.Simple;
            zoomButton.Lip.color = Palette.CardLip;
            zoomButton.Face.color = Palette.Paper;
            Transform zoomShine = zoomButton.FaceRect.Find("Shine");
            if (zoomShine != null) zoomShine.gameObject.SetActive(false);
            // A magnifying glass drawn from what the catalog already has.
            var handle = UiKit.Rect("Handle", zoomButton.FaceRect).Place(0.5f, 0.5f, 16, -16, 10, 30);
            handle.localRotation = Quaternion.Euler(0, 0, 45);
            handle.AddImage(catalog.Panel, Palette.Ink, true);
            UiKit.Rect("Rim", zoomButton.FaceRect).Place(0.5f, 0.5f, -6, 6, 46, 46).AddImage(catalog.Round, Palette.Ink);
            UiKit.Rect("Glass", zoomButton.FaceRect).Place(0.5f, 0.5f, -6, 6, 32, 32).AddImage(catalog.Round, Palette.Paper);
            _zoomMark = UiKit.Rect("Mark", zoomButton.FaceRect).Place(0.5f, 0.5f, -6, 7, 32, 32)
                .AddText("", 22, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var gear = UiKit.Rect("Settings", _hud).Place(1f, 1f, -76, -76, 96, 96);
            gear.AddShadow(14f, 10f, 0.3f);
            ChunkyButton gearButton = null;
            gearButton = UiKit.Chunky(gear, catalog.Round, () =>
            {
                gearButton.Press(Tweens);
                Shell.OpenPause();
            });
            gearButton.Lip.type = Image.Type.Simple;
            gearButton.Face.type = Image.Type.Simple;
            gearButton.Lip.color = Palette.CardLip;
            gearButton.Face.color = Palette.Paper;
            UiKit.Rect("Icon", gearButton.FaceRect).Stretch(0, 0, 1, 1, 20, 20, 20, 20).AddImage(catalog.Gear, Palette.Ink).preserveAspect = true;
            UiKit.Rect("Hub", gearButton.FaceRect).Place(0.5f, 0.5f, 0, 0, 22, 22).AddImage(catalog.Round, Palette.Paper);
        }

        /// <summary>
        /// One plate along the bottom edge with the four tabs on it. The chosen tab stands out (bigger icon on a
        /// raised disc, bold name); the others step back while one is chosen.
        /// </summary>
        void BuildTabBar(GameCatalog catalog)
        {
            _tabBar = UiKit.Rect("TabBar", _canvas).Stretch(0, 0, 1, 0, 20, 0, 20, 0);
            _tabBar.pivot = new Vector2(0.5f, 0f);
            _tabBar.sizeDelta = new Vector2(_tabBar.sizeDelta.x, TabBarHeight);
            _tabBar.AddShadow(26f, 12f, 0.4f);
            UiKit.Rect("Lip", _tabBar).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true).pixelsPerUnitMultiplier = 0.6f;
            Image plate = UiKit.Rect("Plate", _tabBar).Stretch(0, 0, 1, 1, 0, 10, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            plate.pixelsPerUnitMultiplier = 0.6f;
            plate.raycastTarget = true; // a touch between two tabs must not reach the garden behind

            Sprite firstProp = catalog.Stations.Length > 0 ? catalog.Stations[0].Prop : null;
            Sprite[] icons = { firstProp, catalog.IconCharm, catalog.IconQuest, catalog.Rabbit };
            for (int i = 0; i < TabCount; i++)
            {
                int tab = i;
                var rt = UiKit.Rect("Open " + TabNames[i], _tabBar).Stretch(i / (float)TabCount, 0, (i + 1) / (float)TabCount, 1);
                Image hit = rt.AddImage(null, new Color(0, 0, 0, 0));
                hit.raycastTarget = true;
                var button = rt.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                button.onClick.AddListener(() =>
                {
                    Tweens.Add(0.22f, t => _tabPop[tab] = 0.14f * Ease.Pop(t), Ease.Linear, 0f, () => _tabPop[tab] = 0f);
                    if (SheetOpen && Tab != tab) Sfx.Click(); // turning a page; opening and closing have their own sounds
                    if (SheetOpen && Tab == tab) CloseSheet();
                    else OpenSheet(tab);
                });

                // The raised disc under the chosen tab's icon; it pokes a little above the plate.
                _tabLifts[i] = UiKit.Rect("Lift", rt).Place(0.5f, 0f, 0, 112, 136, 136);
                _tabLifts[i].AddShadow(12f, 8f, 0.3f);
                UiKit.Rect("Lip", _tabLifts[i]).Stretch(0, 0, 1, 1).AddImage(catalog.Round, Palette.CarrotLip);
                UiKit.Rect("Face", _tabLifts[i]).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Round, Palette.Lantern);
                _tabLiftGroups[i] = _tabLifts[i].gameObject.AddComponent<CanvasGroup>();
                _tabLiftGroups[i].blocksRaycasts = false;

                _tabIcons[i] = UiKit.Rect("Icon", rt).Place(0.5f, 0f, 0, 100, 84, 84).AddImage(icons[i], Color.white);
                _tabIcons[i].preserveAspect = true;
                _tabIcons[i].enabled = icons[i] != null;
                _openLabels[i] = UiKit.Rect("Label", rt).Stretch(0, 0, 1, 0, 4, 18, 4, -62)
                    .AddText(L10n.T("tab." + i), (int)TabLabelSize, Palette.InkSoft, TextAnchor.MiddleCenter);
                _tabShown[i] = 0f;
                _tabPop[i] = 0f;
            }
            _tabFade = 1f;

            // The count of quest rewards waiting, on the corner of the quest tab's icon.
            _barBadgeRect = UiKit.Rect("Badge", _tabBar).Place(2.5f / TabCount, 0f, 46, 140, 48, 48);
            _barBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _barBadgeText = UiKit.Rect("Text", _barBadgeRect).Stretch(0, 0, 1, 1).AddText("", 26, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _barBadge = _barBadgeRect.gameObject;
            // How many open stations still have nobody working, on the corner of the rabbits tab.
            _rabbitBadgeRect = UiKit.Rect("RabbitsBadge", _tabBar).Place(3.5f / TabCount, 0f, 46, 140, 48, 48);
            _rabbitBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _rabbitBadgeText = UiKit.Rect("Text", _rabbitBadgeRect).Stretch(0, 0, 1, 1).AddText("", 26, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _rabbitBadgeRect.gameObject.SetActive(false);
            _shownClaimable = -1;
            _safeBottom = -1f;
            PlaceTabBar();
            ShowTabBar(0f);
        }

        /// <summary>The part of the bottom edge the phone keeps for itself (home bar), in canvas units.</summary>
        static float SafeBottom() => Screen.height > 0 ? Mathf.Max(0f, Screen.safeArea.yMin / Screen.height * ScreenH) : 0f;

        /// <summary>Stands the tab bar just inside the safe area, and the sheet's lists just above the bar.</summary>
        void PlaceTabBar()
        {
            float safe = SafeBottom();
            if (Mathf.Approximately(safe, _safeBottom)) return;
            _safeBottom = safe;
            _tabBar.anchoredPosition = new Vector2(0f, safe + TabBarGap);
            if (ProductTray != null) ProductTray.Root.anchoredPosition = new Vector2(0f, safe + TabBarGap + TabBarHeight + 10f);
            for (int i = 0; i < _lists.Length; i++)
            {
                if (_lists[i] == null) continue;
                var list = (RectTransform)_lists[i].transform;
                list.offsetMin = new Vector2(list.offsetMin.x, SheetFoot + safe);
            }
        }

        /// <summary>Moves each tab towards its look: chosen ones grow onto their disc, the rest fade back a little.</summary>
        void ShowTabBar(float dt)
        {
            bool any = false;
            for (int i = 0; i < TabCount; i++) any |= SheetOpen && Tab == i;
            _tabFade = Mathf.MoveTowards(_tabFade, any ? 0.55f : 1f, dt / Motion.Quick);
            for (int i = 0; i < TabCount; i++)
            {
                bool on = SheetOpen && Tab == i;
                _tabShown[i] = Mathf.MoveTowards(_tabShown[i], on ? 1f : 0f, dt / Motion.Quick);
                float k = Mathf.SmoothStep(0f, 1f, _tabShown[i]);
                float size = Mathf.Lerp(84f, 108f, k) * (1f + _tabPop[i]);
                RectTransform icon = _tabIcons[i].rectTransform;
                icon.sizeDelta = new Vector2(size, size);
                icon.anchoredPosition = new Vector2(0f, Mathf.Lerp(100f, 122f, k));
                _tabIcons[i].color = new Color(1f, 1f, 1f, Mathf.Lerp(_tabFade, 1f, k));
                _tabLiftGroups[i].alpha = k;
                _tabLifts[i].localScale = Vector3.one * Mathf.Lerp(0.7f, 1f, k);
                _openLabels[i].color = Color.Lerp(Palette.InkSoft, Palette.Ink, k);
                _openLabels[i].fontStyle = _tabShown[i] > 0.5f ? FontStyle.Bold : FontStyle.Normal;
            }
            _barBadgeRect.localScale = Vector3.one * (1f + _badgePop);
        }

        void BuildSheet(GameCatalog catalog)
        {
            // The sheet sits under the HUD and the tab bar in the draw order: neither is dimmed or covered by it.
            var sheet = UiKit.Rect("Sheet", _canvas).Stretch(0, 0, 1, 1);
            sheet.SetSiblingIndex(_hud.GetSiblingIndex());
            _sheet = sheet.gameObject;
            _sheetGroup = _sheet.AddComponent<CanvasGroup>();
            SheetOpen = false; // a rebuilt screen starts closed; RebuildUi opens it again if it was open

            // A touch on the garden above the sheet closes it. Nothing is drawn here: the garden stays as it is.
            var outside = UiKit.Rect("Outside", sheet).Stretch(0, 0, 1, 1);
            outside.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var outsideButton = outside.gameObject.AddComponent<Button>();
            outsideButton.transition = Selectable.Transition.None;
            outsideButton.onClick.AddListener(CloseSheet);

            // Bottom-anchored: its height is how far it stands up, its position is the slide in and out.
            _panel = UiKit.Rect("Panel", sheet).Stretch(0, 0, 1, 0);
            _panel.pivot = new Vector2(0.5f, 0f);
            SetSheetHeight(SheetHalf);
            // Only the strip of garden right above the edge darkens a little.
            var shade = UiKit.Rect("Shade", _panel).Stretch(0, 1, 1, 1);
            shade.pivot = new Vector2(0.5f, 0f);
            shade.sizeDelta = new Vector2(0, 240);
            shade.anchoredPosition = new Vector2(0, -30);
            shade.AddImage(UiKit.FadeUp, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.34f));
            UiKit.Rect("Lip", _panel).Stretch(0, 0, 1, 1, 0, -SheetSink, 0, 0).AddImage(catalog.Panel, Palette.CardLip, true).pixelsPerUnitMultiplier = 0.6f;
            Image body = UiKit.Rect("Face", _panel).Stretch(0, 0, 1, 1, 0, -SheetSink, 0, 10).AddImage(catalog.Panel, Palette.Paper, true);
            body.pixelsPerUnitMultiplier = 0.6f;
            body.raycastTarget = true; // touches on the panel must not fall through to the garden

            var bar = UiKit.Rect("Bar", _panel).Stretch(0, 1, 1, 1);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0, HeaderHeight);
            bar.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true; // the whole header is the grab area
            UiKit.Rect("Grab", bar).Place(0.5f, 1f, 0, -26, 110, 12).AddImage(catalog.Panel, Palette.Mix(Palette.Track, Palette.Ink, 0.18f), true);
            var grab = bar.gameObject.AddComponent<EventTrigger>();
            AddDrag(grab, EventTriggerType.BeginDrag, _ => _sheetDragSpeed = 0f);
            AddDrag(grab, EventTriggerType.Drag, e =>
            {
                Canvas canvas = _canvas.GetComponent<Canvas>();
                float dy = ((PointerEventData)e).delta.y / Mathf.Max(0.01f, canvas != null ? canvas.scaleFactor : 1f);
                _sheetDragSpeed = Mathf.Lerp(_sheetDragSpeed, dy / Mathf.Max(0.001f, Time.unscaledDeltaTime), 0.5f);
                DragSheet(dy);
            });
            AddDrag(grab, EventTriggerType.EndDrag, _ => ReleaseSheet(_sheetDragSpeed));

            // The tab bar along the bottom already switches pages; up here there is only the page's name.
            _sheetTitle = UiKit.Rect("Title", bar).Stretch(0, 0, 1, 1, 40, 0, 600, 34)
                .AddText("", 44, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var modes = UiKit.Rect("Modes", bar).Stretch(0, 0, 1, 1);
            _modes = modes.gameObject;
            BuildModeBar(modes, catalog, -110, -18, 76);

            var close = UiKit.Rect("Close", bar).Place(1f, 0.5f, -62, -18, 80, 80);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(CloseSheet);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            RectTransform stations = AddList("Scroll", 0);
            _upgradeAllRow = UiKit.Rect("UpgradeAll", stations);
            _upgradeAllRow.AddImage(catalog.Panel, Palette.CardLip, true);
            _upgradeAllRow.gameObject.AddComponent<LayoutElement>().preferredHeight = 118;
            UiKit.Rect("Card", _upgradeAllRow).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.7f), true);
            Text upgradeAllNote = UiKit.Rect("Sub", _upgradeAllRow).Stretch(0, 0, 1, 1, 36, 8, 330, 0)
                .AddText(L10n.T("upgradeall.row"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft);
            upgradeAllNote.resizeTextForBestFit = true;
            upgradeAllNote.resizeTextMinSize = 16;
            upgradeAllNote.resizeTextMaxSize = UiKit.TextBody;
            var upgradeAllButton = UiKit.Rect("Button", _upgradeAllRow).Place(1f, 0.5f, -166, 4, 284, 84);
            ChunkyButton upgradeAllChunky = UiKit.Chunky(upgradeAllButton, catalog.Panel, () => { Sfx.Click(); UpgradeAll.Open(Runner.Defs, Runner.State); });
            UiKit.Rect("Text", upgradeAllChunky.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("upgradeall.title"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            for (int i = 0; i < catalog.Stations.Length; i++)
            {
                int index = i;
                var row = new StationRowView(i);
                row.Build(stations, catalog, catalog.Stations[i], OnBuy, at => { Sfx.Click(); ShowStationAboveSheet(at); });
                row.Buy.Dip.Denied = () => Deny(row.Buy, true);
                row.Buy.Dip.Repeat = () => OnBuy(index); // held down, the button keeps upgrading
                _rows.Add(row);
            }
            BuildPlotRow(stations, catalog);

            RectTransform upgrades = AddList("Upgrades", 1);
            AddNote(upgrades, L10n.T("upgrades.note"));
            for (int i = 0; i < catalog.Upgrades.Length; i++)
            {
                var row = new UpgradeRowView(i);
                row.Build(upgrades, catalog, catalog.Upgrades[i], OnBuyUpgrade);
                row.Buy.Dip.Denied = () => Deny(row.Buy, true);
                _upgradeRows.Add(row);
            }

            RectTransform quests = AddList("Quests", 2);
            // Finished quests gather up here, under one button that collects them all.
            _questReady = UiKit.Rect("Ready", quests);
            _questReady.gameObject.AddComponent<LayoutElement>().preferredHeight = 76;
            UiKit.Rect("Text", _questReady).Stretch(0, 0, 0.5f, 1).AddText(L10n.T("quests.ready"), 30, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var claimAll = UiKit.Rect("ClaimAll", _questReady).Place(1f, 0.5f, -130, 0, 260, 68);
            _claimAll = UiKit.Chunky(claimAll, catalog.Panel, OnClaimAll);
            UiKit.Rect("Text", _claimAll.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("quests.claimall"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _questReady.gameObject.SetActive(false);
            _clovers = AddNote(quests, "");
            _questToday = AddNote(quests, L10n.T("quests.today"), true);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) _questAchievements = AddNote(quests, L10n.T("quests.achievements"), true);
                for (int i = 0; i < catalog.Quests.Length; i++)
                {
                    if (catalog.Quests[i].Daily != (pass == 0)) continue;
                    var row = new QuestRowView(i);
                    row.Build(quests, catalog, catalog.Quests[i], OnClaim);
                    _questRows.Add(row);
                }
            }

            RectTransform rabbits = AddList("Rabbits", 3);
            _rabbitList = new RabbitGridView(rabbits, catalog, OnSummon, () =>
            {
                Sfx.Click();
                Runner.AutoAssignRabbits();
            }, OnMerge, rabbitId =>
            {
                Sfx.Click();
                Detail.Open(Runner.Defs, Runner.State, rabbitId);
            }, kindId =>
            {
                Sfx.Click();
                _freshKinds.Remove(kindId); // looked at now
                Detail.OpenKind(Runner.Defs, Runner.State, kindId);
            }, null, IsFreshKind);

            SetTab(0);
            _sheet.SetActive(false);
        }

        /// <summary>The card that sells the next garden plot; it sits right after the last station that has one.</summary>
        void BuildPlotRow(RectTransform list, GameCatalog catalog)
        {
            _plotRow = UiKit.Rect("Plot", list);
            _plotRow.AddImage(catalog.Panel, Palette.Mix(Palette.Grass, Palette.Ink, 0.25f), true);
            var layout = _plotRow.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 176;
            layout.minHeight = 176;
            UiKit.Rect("Card", _plotRow).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Moss, Palette.Paper, 0.6f), true);
            _plotTitle = UiKit.Rect("Title", _plotRow).Stretch(0, 0.55f, 1, 1, 36, 4, 330, 20)
                .AddText("", 34, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _plotSub = UiKit.Rect("Sub", _plotRow).Stretch(0, 0.14f, 1, 0.55f, 36, 0, 330, 6)
                .AddText("", 25, Palette.InkSoft, TextAnchor.UpperLeft);
            var buy = UiKit.Rect("Buy", _plotRow).Place(1f, 0.5f, -166, 4, 284, 120);
            _plotBuy = UiKit.Chunky(buy, catalog.Panel, OnBuyPlot);
            _plotBuy.Dip.Denied = () => Deny(_plotBuy, !Expansion.HasCarrots(Runner.Defs, Runner.State));
            UiKit.Rect("Top", _plotBuy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 10)
                .AddText(L10n.T("plot.buy"), 23, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _plotCost = UiKit.Rect("Cost", _plotBuy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 10, 8, 0)
                .AddText("", 36, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>A scrolling column of cards under the sheet's header; returns where the cards go.</summary>
        RectTransform AddList(string name, int tab)
        {
            // The tab bar stands in front of the sheet's foot, so the lists stop above it.
            var scrollRt = UiKit.Rect(name, _panel).Stretch(0, 0, 1, 1, 0, SheetFoot, 0, HeaderHeight);
            scrollRt.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();

            var content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(28, 28, 4, 40);
            list.spacing = 18;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = scrollRt;
            _lists[tab] = scrollRt.gameObject;
            return content;
        }

        Text AddNote(RectTransform list, string text, bool heading = false)
        {
            var rt = UiKit.Rect(heading ? "Heading" : "Note", list);
            var layout = rt.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = heading ? 52 : 44;
            return rt.AddText(text, heading ? 30 : 25, heading ? Palette.Ink : Palette.InkSoft,
                TextAnchor.MiddleLeft, heading ? FontStyle.Bold : FontStyle.Normal);
        }

        /// <summary>
        /// The four buy modes in one plate, its right edge at <paramref name="right"/> of the parent: ×1, ×10,
        /// to the next milestone, and as many as the purse allows. The same plate is on the sheet and the card.
        /// </summary>
        void BuildModeBar(RectTransform parent, GameCatalog catalog, float right, float y, float height)
        {
            const float Pad = 8f, Gap = 6f;
            int[] modes = { Simulation.BuyOne, Simulation.BuyTen, Simulation.BuyToMilestone, Simulation.BuyMax };
            string[] labels = { "×1", "×10", L10n.T("mode.milestone"), L10n.T("mode.max") };
            float[] widths = { 84f, 92f, 118f, 140f };
            float width = Pad * 2f + Gap * (modes.Length - 1);
            foreach (float w in widths) width += w;
            var track = UiKit.Rect("Track", parent).Place(1f, 0.5f, right - width / 2f, y, width, height);
            track.AddImage(catalog.Panel, Palette.Track, true);
            float x = right - width + Pad;
            for (int i = 0; i < modes.Length; i++)
            {
                int mode = modes[i];
                var rt = UiKit.Rect("Mode " + labels[i], parent).Place(1f, 0.5f, x + widths[i] / 2f, y, widths[i], height - 16f);
                Image img = rt.AddImage(catalog.Panel, Palette.Carrot, true);
                img.raycastTarget = true;
                var button = rt.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = img;
                button.onClick.AddListener(() =>
                {
                    Sfx.Click();
                    Runner.SetBuyMode(mode);
                });
                _modeLabels.Add(new KeyValuePair<int, Text>(mode, UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(labels[i], 26, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)));
                _modeButtons.Add(new KeyValuePair<int, Image>(mode, img));
                x += widths[i] + Gap;
            }
        }

        public void SetTab(int tab)
        {
            bool turned = SheetOpen && Tab != Mathf.Clamp(tab, 0, TabCount - 1);
            Tab = Mathf.Clamp(tab, 0, TabCount - 1);
            for (int i = 0; i < TabCount; i++)
            {
                _lists[i].SetActive(i == Tab);
            }
            _modes.SetActive(Tab == 0);
            if (_sheetTitle != null) _sheetTitle.text = L10n.T("sheet.title." + Tab);
            if (turned) StaggerRows();
            if (_built && Feel != null) Refresh(-1f);
        }

        /// <summary>Turns the rabbit tab to the album of kinds, or back to the player's rabbits.</summary>
        public void ShowRabbitAlbum(bool album)
        {
            _rabbitList.ShowAlbum(album);
            Refresh(-1f);
        }

        public void OpenSheet(int tab)
        {
            SetTab(tab);
            if (SheetOpen) return;
            SheetOpen = true;
            Sfx.SheetUp();
            SelectStation(-1); // the station card and a sheet never share the bottom of the screen
            SetSheetHeight(SheetHalf); // it always opens at the same fixed height
            if (!_sheet.activeSelf)
            {
                _sheet.SetActive(true);
                _panel.anchoredPosition = new Vector2(0, -SheetHalf);
            }
            _sheetGroup.blocksRaycasts = true;
            if (_scroll != null) { _scroll.StopMovement(); _scroll.enabled = false; } // the garden is moved by the sheet for now
            _glideRun++;
            SlideSheet(0f, Motion.Time(Motion.Panel), Motion.Curve(Ease.OutBackLight), null);
            StaggerRows();
            Refresh(-1f);
        }

        public void CloseSheet()
        {
            if (!SheetOpen) return;
            SheetOpen = false;
            Sfx.SheetDown();
            _sheetGroup.blocksRaycasts = false;
            SlideSheet(-SheetHeight - 60f, Motion.Time(Motion.PanelOut), Ease.InCubic, () => _sheet.SetActive(false));

            // The garden may have been lifted to show a low station: let it settle back inside its edges.
            Vector2 from = _street.anchoredPosition, home = ClampView(from, _zoom);
            Tweens.Add(Motion.Time(Motion.Panel), t => { if (_street != null && !SheetOpen) _street.anchoredPosition = Vector2.LerpUnclamped(from, home, t); },
                Ease.InOutCubic, 0f, () => { if (_scroll != null && !SheetOpen) _scroll.enabled = true; });
            Refresh(-1f);
        }

        /// <summary>The rows in view arrive one after another, a few hundredths of a second apart.</summary>
        void StaggerRows()
        {
            if (Motion.Reduced || _lists[Tab] == null) return;
            Transform content = _lists[Tab].transform.Find("Content");
            int shown = 0;
            for (int i = 0; i < content.childCount && shown < 8; i++)
            {
                Transform row = content.GetChild(i);
                if (!row.gameObject.activeSelf) continue;
                if (!row.TryGetComponent(out CanvasGroup group)) group = row.gameObject.AddComponent<CanvasGroup>();
                group.alpha = 0f;
                Tweens.Add(Motion.Quick, t =>
                {
                    if (group == null) return;
                    group.alpha = t;
                    row.localScale = Vector3.one * Mathf.Lerp(0.95f, 1f, t);
                }, Ease.OutCubic, shown * Motion.Stagger);
                shown++;
            }
        }

        void SetSheetHeight(float height)
        {
            SheetHeight = Mathf.Clamp(height, SheetLeast, SheetFull);
            _panel.sizeDelta = new Vector2(0, SheetHeight);
        }

        /// <summary>Slides the panel to a fixed resting place. A newer slide or a drag takes over from an older one.</summary>
        void SlideSheet(float toY, float seconds, System.Func<float, float> curve, System.Action done)
        {
            int run = ++_sheetRun;
            float fromY = _panel.anchoredPosition.y;
            Tweens.Add(seconds, t => { if (run == _sheetRun && _panel != null) _panel.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(fromY, toY, t)); },
                curve, 0f, () => { if (run == _sheetRun) done?.Invoke(); });
        }

        /// <summary>Stands the open sheet at one of its two heights, with the same light bounce as opening.</summary>
        void SnapSheet(float height)
        {
            int run = ++_sheetRun;
            float from = SheetHeight, fromY = _panel.anchoredPosition.y;
            Tweens.Add(Motion.Time(Motion.Panel), t =>
            {
                if (run != _sheetRun || _panel == null) return;
                SheetHeight = Mathf.LerpUnclamped(from, height, t);
                _panel.sizeDelta = new Vector2(0, SheetHeight);
                _panel.anchoredPosition = new Vector2(0, Mathf.LerpUnclamped(fromY, 0f, Mathf.Clamp01(t)));
            }, Motion.Curve(Ease.OutBackLight), 0f, () => { if (run == _sheetRun) SetSheetHeight(height); });
        }

        /// <summary>The grab handle moved by this much (canvas units, up is positive).</summary>
        public void DragSheet(float delta)
        {
            if (!SheetOpen) return;
            _sheetRun++; // the finger takes over from any slide
            _panel.anchoredPosition = Vector2.zero;
            SetSheetHeight(SheetHeight + delta);
        }

        /// <summary>The handle was let go: a flick or the height decides between closed, 60% and full.</summary>
        public void ReleaseSheet(float speed = 0f)
        {
            if (!SheetOpen) return;
            float middle = (SheetHalf + SheetFull) / 2f;
            if (speed > SheetFling) SnapSheet(SheetFull);
            else if (speed < -SheetFling)
            {
                if (SheetHeight > SheetHalf + 60f) SnapSheet(SheetHalf);
                else CloseSheet();
            }
            else if (SheetHeight < SheetHalf * 0.8f) CloseSheet();
            else SnapSheet(SheetHeight > middle ? SheetFull : SheetHalf);
        }

        /// <summary>A row in the station list was touched: the garden above the sheet slides to that station.</summary>
        public void ShowStationAboveSheet(int index)
        {
            if (!SheetOpen || index < 0 || index >= _actors.Count) return;
            if (SheetHeight > SheetHalf + 1f) SnapSheet(SheetHalf); // make room to see it
            float zoom = Zoom == ZoomLevel.Far ? ZoomDefault : _zoom, fromZoom = _zoom;
            var open = new Vector2(ScreenW / 2f, (SheetHalf + ScreenH - HudCover) / 2f);
            Vector2 spot = StationSpot(index), from = _street.anchoredPosition, to = ClampView(open - spot * zoom, zoom, SheetLift);
            _glideRun++;
            Tweens.Add(Motion.Time(Motion.Travel), t =>
            {
                if (_street == null || !SheetOpen) return;
                _zoom = Mathf.LerpUnclamped(fromZoom, zoom, t);
                _street.localScale = new Vector3(_zoom, _zoom, 1f);
                _street.anchoredPosition = Vector2.LerpUnclamped(from, to, t);
                if (Zoom != _shownZoom) ShowZoomDetail();
            }, Ease.InOutCubic);
            if (_actors[index].Root.gameObject.activeSelf) _actors[index].Bump();
        }

        static void AddDrag(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(call);
            trigger.triggers.Add(entry);
        }

        void BuildAwayPopup(GameCatalog catalog)
        {
            var overlay = UiKit.Rect("Away", _canvas).Stretch(0, 0, 1, 1);
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            _away = overlay.gameObject;

            var card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 800, 700);
            UiKit.Rect("Lip", card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Title", card).Stretch(0, 0.80f, 1, 1, 40, 0, 40, 40)
                .AddText(L10n.T("away.title"), 54, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Body", card).Stretch(0, 0.62f, 1, 0.80f, 60, 0, 60, 0)
                .AddText(L10n.T("away.body"), 30, Palette.InkSoft);
            UiKit.Rect("Carrot", card).Place(0.5f, 0.5f, -200, 4, 110, 110).AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _awayAmount = UiKit.Rect("Amount", card).Place(0.5f, 0.5f, 60, 4, 420, 110)
                .AddText("+0", 84, Palette.Carrot, TextAnchor.MiddleLeft, FontStyle.Bold);
            _awayTime = UiKit.Rect("Time", card).Stretch(0, 0.29f, 1, 0.40f)
                .AddText("", 28, Palette.InkSoft);

            var ok = UiKit.Rect("Ok", card).Place(0.5f, 0f, 0, 120, 580, 130);
            ChunkyButton okButton = null;
            okButton = UiKit.Chunky(ok, catalog.Panel, () =>
            {
                okButton.Press(Tweens);
                Sfx.Reward();
                Feel.Burst(Feel.PointOf(_purse), catalog.Currency, new[] { Color.white }, 10, 480f, 46f);
                _away.SetActive(false);
            });
            UiKit.Rect("Text", okButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("away.ok"), 40, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            _away.SetActive(false);
        }

        void OnShellChanged(ShellState state)
        {
            Runner.Paused = state == ShellState.Paused;
            if (state != ShellState.Playing || _hud.gameObject.activeSelf) return;

            // Leaving the title: the HUD drops in and the tab bar slides in from the right.
            _hud.gameObject.SetActive(true);
            _tabBar.gameObject.SetActive(true);
            Vector2 hudHome = _hud.anchoredPosition, barHome = _tabBar.anchoredPosition;
            Tweens.Add(Motion.Time(0.45f), t => _hud.anchoredPosition = hudHome + new Vector2(0, (1f - t) * 340f), Motion.Curve(Ease.OutBack));
            Tweens.Add(Motion.Time(0.5f), t => _tabBar.anchoredPosition = barHome + new Vector2(0, -(1f - t) * 320f), Ease.OutCubic);

            // The camera glides from the moon pond down to the teapot. It never blocks: any touch on the garden takes over.
            int run = ++_glideRun;
            Vector2 from = _street.anchoredPosition, home = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - StationSpot(0) * _zoom, _zoom);
            Tweens.Add(Motion.Time(Motion.Glide), t => { if (run == _glideRun && _street != null) _street.anchoredPosition = Vector2.LerpUnclamped(from, home, t); }, Ease.InOutCubic);
            Refresh(-1f);
        }

        void RefreshNow()
        {
            Refresh(-1f);
        }

        /// <summary>Pushes the current state into every label and button.</summary>
        /// <param name="dt">Seconds since the last refresh; negative skips the bar easing.</param>
        public void Refresh(float dt = -1f)
        {
            if (!_built) return;
            _sinceRefresh = 0f;
            GameState s = Runner.State;
            IReadOnlyList<StationDef> defs = Runner.Defs;
            Modifiers m = Runner.Mods;
            bool boosting = s.BoostSecondsLeft > 0d;

            BigNum perSecond = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier * (boosting ? Energy.BoostSpeed : 1d));
            _perSecond.text = L10n.T("persec", perSecond < new BigNum(10) ? UiKit.Seconds(perSecond.ToDouble()) : perSecond.Short());
            if (!_hintFading) _hint.SetActive(!s.HintSeen && Shell.State == ShellState.Playing && !SheetOpen);
            if (dt < 0f) _shownCoins = s.Coins.ToDouble();

            RefreshEnergy(s, boosting);
            RefreshStreet(defs, s);

            for (int i = 0; i < _rows.Count; i++) _rows[i].Refresh(defs, s, m, dt);
            for (int i = 0; i < _upgradeRows.Count; i++) _upgradeRows[i].Refresh(Runner.UpgradeDefs, s);
            for (int i = 0; i < _questRows.Count; i++) _questRows[i].Refresh(Runner.QuestDefs, defs, s, m, dt);
            _questToday.text = L10n.T("quests.today.refresh", Span(SecondsToNextDay(System.DateTimeOffset.Now)));
            OrderQuestRows();
            RefreshPlotRow(defs, s);
            _rabbitList.Refresh(defs, s);
            Detail.Refresh(defs, s);
            Picker.Refresh(s);
            Workers.Refresh(defs, s);
            Profile.Refresh(defs, s, Runner.Account);
            Rabbit avatar = VuonTra.Core.Profile.Avatar(s);
            Shell.SetProfile(ProfileView.NameOf(s), s.Profile.Code, avatar != null ? Runner.Catalog.RabbitArt(avatar) : Runner.Catalog.Rabbit);

            int claimable = Quests.ClaimableCount(Runner.QuestDefs, defs, s);
            _barBadge.SetActive(claimable > 0);
            _barBadgeText.text = claimable.ToString();
            if (_shownClaimable >= 0 && claimable > _shownClaimable)
                Tweens.Add(0.32f, t => _badgePop = 0.4f * Ease.Pop(t), Ease.Linear, 0f, () => _badgePop = 0f); // one more to collect
            _shownClaimable = claimable;
            int missing = Rabbits.StationsWithoutWorker(s);
            if (_rabbitBadgeRect.gameObject.activeSelf != (missing > 0)) _rabbitBadgeRect.gameObject.SetActive(missing > 0);
            _rabbitBadgeText.text = missing.ToString();
            _clovers.text = L10n.T("quests.clovers", s.Clovers);

            for (int i = 0; i < _modeButtons.Count; i++)
            {
                bool on = _modeButtons[i].Key == s.BuyMode;
                _modeButtons[i].Value.color = on ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _modeLabels[i].Value.color = on ? Palette.Card : Palette.InkSoft;
            }
            UpgradeAll.Refresh(defs, s);
            ProductTray.Refresh(s);
            // The tray lives in the plain garden view; a sheet or a station card covers its place.
            bool trayShown = Shell.State == ShellState.Playing && !SheetOpen && Selected < 0 && !PhotoMode && (s.Tray.Count > 0 || Tray.IsFull(s));
            if (ProductTray.Root.gameObject.activeSelf != trayShown) ProductTray.Root.gameObject.SetActive(trayShown);
        }

        void RefreshEnergy(GameState s, bool boosting)
        {
            bool can = Energy.CanBoost(s);
            int gardenLevel = Garden.Level(s);
            _levelText.text = gardenLevel.ToString();
            _levelRing.fillAmount = Mathf.Max(0.02f, (float)Garden.Progress(s));
            if (_shownGardenLevel > 0 && gardenLevel > _shownGardenLevel && Shell.State != ShellState.Title)
            {
                // A new garden level: say so, and say what it brought.
                Sfx.Sting();
                Feel.Moment(L10n.T("moment.garden.title", gardenLevel), L10n.T("moment.garden.sub", (gardenLevel - _shownGardenLevel) * Garden.CloversPerLevel));
                RectTransform badge = _levelBadge;
                Tweens.Add(0.5f, t => { if (badge != null) badge.localScale = Vector3.one * (1f + 0.3f * Ease.Pop(t)); });
            }
            _shownGardenLevel = gardenLevel;
            RefreshFog(s);

            _energyFill.anchorMax = new Vector2(Mathf.Clamp01((float)(s.Energy / Energy.Max)), 1f);
            _energyLabel.text = L10n.T("energy.label", Mathf.FloorToInt((float)s.Energy));
            _energyAction.text = boosting
                ? L10n.T("energy.boosting", Mathf.CeilToInt((float)s.BoostSecondsLeft))
                : (can ? L10n.T("energy.ready") : L10n.T("energy.need", (int)Energy.BoostCost));
            _energyButton.Button.interactable = can;
            // Cream like the purse, so it never outshines the garden; only the word lights up when it is ready.
            _energyButton.Lip.color = boosting ? Palette.Mix(Palette.Moss, Palette.Ink, 0.3f) : Palette.CardLip;
            _energyButton.Face.color = boosting ? Palette.Mix(Palette.Moss, Palette.Paper, 0.6f) : Palette.Paper;
            _energyAction.color = boosting ? Palette.Ink : (can ? Palette.CarrotLip : Palette.InkSoft);
        }

        /// <summary>Which stations stand in the garden, who works at each, and the open station card.</summary>
        void RefreshStreet(IReadOnlyList<StationDef> defs, GameState s)
        {
            // Stations with a plot, plus the one waiting for the next plot.
            // Open stations, plus the grey outline of the next two at most: the garden grows as you do.
            int open = 0;
            for (int i = 0; i < defs.Count; i++) if (s.Levels[i] > 0) open = i + 1;
            int shown = Mathf.Min(defs.Count, Mathf.Min(s.Plots + 1, open + 2));
            if (shown != _shownStations)
            {
                _shownStations = shown;
                for (int i = 0; i < _actors.Count; i++) _actors[i].Root.gameObject.SetActive(i < shown);
                if (Selected >= shown) SelectStation(-1);
            }

            for (int i = 0; i < shown; i++)
            {
                StationLook look = i >= s.Plots ? StationLook.Land : (s.Levels[i] <= 0 ? StationLook.Unbuilt : StationLook.Open);
                _actors[i].SetLook(look, s.Levels[i], defs[i].Kind == StationKind.Brew);
                if (look == StationLook.Land)
                {
                    _actors[i].SetSign(L10n.T("land.expand"), Expansion.CanExpand(defs, s));
                }
                else if (look == StationLook.Unbuilt)
                {
                    // The price sign: what it costs to build, or how many carrots are still missing for the next one.
                    BigNum cost = Economy.CostOfNext(defs[i], 0, 1);
                    bool next = Economy.CanUnlock(s, i), can = next && s.Coins >= cost;
                    _actors[i].SetSign(next && !can ? L10n.T("plot.short", (cost - s.Coins).Short()) : L10n.T("plot.build", cost.Short()), can);
                }
                _actors[i].SetCanUpgrade(CanAfford(defs, s, i));

                Rabbit worker = Rabbits.AtStation(s, i);
                GameCatalog catalog = Runner.Catalog;
                if (worker == null) _actors[i].SetWorker(null, null, null, false);
                else
                {
                    // The collected rabbit itself stands at the station, wearing what it wears.
                    Item hat = Items.Worn(s, worker.Id, ItemSlot.Hat), charm = Items.Worn(s, worker.Id, ItemSlot.Charm);
                    RabbitKind kind = RabbitKinds.Find(Rabbits.KindOf(worker));
                    _actors[i].SetWorker(catalog.RabbitArt(worker),
                        hat != null && !kind.NoHat ? catalog.ItemIcon(hat.DefId) : null, charm != null ? catalog.ItemIcon(charm.DefId) : null,
                        worker.Tier >= 4 && !kind.OwnGlow, new Vector2(kind.HatX, kind.HatY));
                }

                GameObject chip = _tierChips[i].gameObject;
                bool named = worker != null && Zoom != ZoomLevel.Far;
                if (chip.activeSelf != named) chip.SetActive(named);
                if (!named) continue;
                _tierChips[i].color = Palette.Tiers[worker.Tier];
                _tierLabels[i].text = worker.Name;
                bool inTrade = Rabbits.InTrade(defs, worker);
                _tierTradeIcons[i].sprite = catalog.TradeIcon(Rabbits.TradeOf(worker));
                _tierTradeBadges[i].color = inTrade ? Palette.Lantern : Palette.Mix(Palette.Ink, Palette.Paper, 0.6f);
                _tierTradeIcons[i].color = inTrade ? Palette.Ink : Palette.Paper;
            }
            RefreshCard(defs, s, Runner.Mods);
            RefreshSeats(s);
        }

        /// <summary>The mist lies on every plot the garden does not have; when the garden widens it lifts.</summary>
        void RefreshFog(GameState s)
        {
            // Trees grow only where no station is drawn at all; the next station's outline stands in mist alone.
            int open = 0;
            for (int i = 0; i < s.Levels.Length; i++) if (s.Levels[i] > 0) open = i + 1;
            int drawn = Mathf.Min(s.Levels.Length, Mathf.Min(s.Plots + 1, open + 2));
            int key = s.Plots * 100 + drawn;
            if (key == _fogPlots) return;
            bool lifting = _fogPlots >= 0 && s.Plots > _fogPlots / 100;
            for (int i = 0; i < _fog.Count; i++)
            {
                if (_fog[i] == null) continue;
                bool misty = i >= s.Plots;
                CanvasGroup fog = _fog[i];
                GameObject grove = _fogTrees[i];
                if (grove.activeSelf != (i >= drawn)) grove.SetActive(i >= drawn);
                if (misty) fog.gameObject.SetActive(true);
                else if (lifting && fog.gameObject.activeSelf)
                {
                    // The banks drift apart to either side and thin away.
                    var bank = (RectTransform)fog.transform;
                    var homes = new Vector2[bank.childCount];
                    for (int k = 0; k < homes.Length; k++) homes[k] = ((RectTransform)bank.GetChild(k)).anchoredPosition;
                    Tweens.Add(1.4f, t =>
                    {
                        if (fog == null) return;
                        fog.alpha = 1f - t;
                        for (int k = 0; k < homes.Length && k < bank.childCount; k++)
                            ((RectTransform)bank.GetChild(k)).anchoredPosition = homes[k] + new Vector2((k % 2 == 0 ? -1f : 1f) * 260f * t, 30f * t);
                    }, Ease.OutCubic, 0f, () =>
                    {
                        if (fog == null) return;
                        fog.gameObject.SetActive(false);
                        fog.alpha = 1f;
                        for (int k = 0; k < homes.Length && k < bank.childCount; k++) ((RectTransform)bank.GetChild(k)).anchoredPosition = homes[k];
                    });
                }
                else fog.gameObject.SetActive(false);
            }
            _fogPlots = key;
        }

        /// <summary>Resting rabbits that have a place sit in the garden as themselves; the rest are not out.</summary>
        void RefreshSeats(GameState s)
        {
            List<Rabbit> seated = RestSpots.Seated(s);
            _seated = Mathf.Min(seated.Count, _seats.Count);
            GameCatalog catalog = Runner.Catalog;
            for (int k = 0; k < _seats.Count; k++)
            {
                bool taken = k < _seated;
                _seatIds[k] = taken ? seated[k].Id : 0;
                if (taken)
                {
                    Sprite art = catalog.RabbitArt(seated[k]);
                    _seatArt[k].sprite = art;
                    if (k < _walkers.Count) _walkers[k].Art.sprite = art;
                }
                bool sitting = taken && !(k < _walkers.Count && Strolling(k, _clock));
                if (_seats[k].gameObject.activeSelf != sitting) _seats[k].gameObject.SetActive(sitting);
                // The first two places are the stumps; each later one shows its furniture once it exists.
                bool cushion = k >= 2 && k < s.RestSpots;
                if (_seatCushions[k].activeSelf != cushion) _seatCushions[k].SetActive(cushion);
            }
        }

        /// <summary>True when the one thing the station's card offers (land, opening, or levels) can be paid for.</summary>
        static bool CanAfford(IReadOnlyList<StationDef> defs, GameState s, int i)
        {
            if (i >= s.Plots) return Expansion.CanExpand(defs, s);
            int level = s.Levels[i];
            if (level <= 0) return Economy.CanUnlock(s, i) && s.Coins >= Economy.CostOfNext(defs[i], 0, 1);
            return s.Coins >= Economy.CostOfNext(defs[i], level, Simulation.BuyCount(defs, s, i));
        }

        /// <summary>
        /// What the next widening of the garden asks for, one condition after another, each in green once it
        /// is met and in rust while it is not: "Cấp vườn 3 · 3 bé thỏ (đang 1) · 650K cà rốt".
        /// </summary>
        static string ExpansionNeeds(IReadOnlyList<StationDef> defs, GameState s)
        {
            ExpansionWave wave = Expansion.Next(defs, s);
            if (wave == null) return "";
            string Mark(bool met, string text) => "<color=#" + (met ? "2E7D4F" : "B4532A") + ">" + text + "</color>";
            int level = Garden.Level(s);
            var parts = new List<string>();
            parts.Add(Mark(level >= wave.NeedLevel, L10n.T("expand.level", wave.NeedLevel) + (level >= wave.NeedLevel ? "" : L10n.T("expand.have", level))));
            if (wave.NeedRabbits > 0)
                parts.Add(Mark(s.Rabbits.Count >= wave.NeedRabbits, L10n.T("expand.rabbits", wave.NeedRabbits) + (s.Rabbits.Count >= wave.NeedRabbits ? "" : L10n.T("expand.have", s.Rabbits.Count))));
            if (wave.NeedKinds > 0)
                parts.Add(Mark(s.SeenKinds.Count >= wave.NeedKinds, L10n.T("expand.kinds", wave.NeedKinds) + (s.SeenKinds.Count >= wave.NeedKinds ? "" : L10n.T("expand.have", s.SeenKinds.Count))));
            parts.Add(Mark(Expansion.HasCarrots(defs, s), L10n.T("expand.carrots", Expansion.Cost(defs, s).Short())));
            return string.Join(" · ", parts);
        }

        void RefreshPlotRow(IReadOnlyList<StationDef> defs, GameState s)
        {
            bool more = Plots.CanBuy(defs, s);
            if (_plotRow.gameObject.activeSelf != more) _plotRow.gameObject.SetActive(more);
            if (!more) return;
            if (_plotRow.GetSiblingIndex() != s.Plots + 1) _plotRow.SetSiblingIndex(s.Plots + 1); // after the upgrade-all row and the owned plots
            BigNum cost = Plots.CostOfNext(defs, s);
            bool can = Expansion.CanExpand(defs, s);
            _plotTitle.text = L10n.T("plot.title", Expansion.NextIndex(defs, s) + 1);
            _plotSub.text = ExpansionNeeds(defs, s);
            _plotCost.text = cost.Short();
            _plotCost.color = ChunkyButton.InkFor(can);
            _plotBuy.SetEnabled(can);
        }

        public void ShowAway(BigNum amount, double seconds)
        {
            _awayAmount.text = "+" + amount.Short();
            string span = Span(seconds);
            bool capped = seconds >= Runner.Mods.AwayCapSeconds;
            _awayTime.text = L10n.T(capped ? "away.time.capped" : "away.time", span);
            _away.SetActive(true);
            RectTransform card = (RectTransform)_away.transform.Find("Card");
            card.localScale = Vector3.one * 0.8f;
            Tweens.Add(0.3f, t => card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, t), Ease.OutBack);
        }

        /// <summary>True when a station is inside the visible part of the street.</summary>
        bool OnScreen(RectTransform actor)
        {
            Vector2 at = Feel.PointOf(actor);
            return Mathf.Abs(at.x) < ScreenW / 2f + 160f && Mathf.Abs(at.y) < ScreenH / 2f + 160f;
        }

        void OnTap(int index)
        {
            if (Zoom == ZoomLevel.Far)
            {
                // From far away the station would sit behind its own card: come in to the usual zoom on it first.
                PlaceView(StationSpot(index), new Vector2(ScreenW / 2f, ScreenH * 0.6f), ZoomDefault);
            }
            SelectStation(index); // a tap always shows the station's card, asleep or not
            if (Runner.State.Levels[index] <= 0) return;
            if (_actors[index].Dozing)
            {
                // Waking a dozing rabbit is a small kindness, repaid in tea power.
                _actors[index].SetDozing(false);
                Runner.AddEnergy(DozeEnergy);
                Feel.FloatText(Feel.PointOf(_actors[index].Root, new Vector2(-70, 190)), L10n.T("doze.reward", (int)DozeEnergy), Palette.Lantern, 38);
            }
            Runner.Tap(index);
            _actors[index].Squish();
            Sfx.Pet();
            DismissHint();
        }

        /// <summary>The empty pad at a station, or the card's "who works here", was touched: choose a rabbit for it.</summary>
        void OnPad(int index)
        {
            GameState s = Runner.State;
            if (index < 0 || index >= s.Levels.Length || s.Levels[index] <= 0 || index >= s.Plots) return;
            Sfx.Click();
            Workers.Open(Runner.Defs, s, index);
        }

        void OnQuick(int index)
        {
            if (index < 0) return;
            if (index >= Runner.State.Plots) OnBuyPlot();
            else OnBuy(index);
        }

        void OnBoost()
        {
            if (!Runner.Boost()) return;
            _energyButton.Press(Tweens);
            Sfx.Sting();
            Feel.Moment(L10n.T("moment.boost.title"), L10n.T("moment.boost.sub", (int)Energy.BoostSeconds));
        }

        void OnBuy(int index)
        {
            GameState s = Runner.State;
            int before = s.Levels[index];
            double multiplierBefore = Economy.MilestoneMultiplier(before);
            if (!Runner.Buy(index)) return;

            _rows[index].Buy.Press(Tweens);
            if (Selected == index) _cardBuy.Press(Tweens);
            Sfx.Upgrade();
            Haptics.Light();
            RectTransform actor = _actors[index].Root;
            _actors[index].Bump(); // from the station's fixed size: presses in quick succession do not add up

            GameCatalog catalog = Runner.Catalog;
            int after = s.Levels[index];
            if (before == 0)
            {
                Sfx.Unlock();
                Feel.Moment(L10n.T("moment.unlock"), L10n.Station(catalog.Stations[index].Id));
            }
            else if (Economy.MilestoneMultiplier(after) > multiplierBefore)
            {
                Sfx.Milestone();
                Haptics.Medium();
                Feel.Moment(L10n.T("moment.milestone.title", Economy.PreviousMilestone(after)), L10n.T("moment.milestone.sub", L10n.Station(catalog.Stations[index].Id)));
            }
            else if (OnScreen(actor))
            {
                Feel.Burst(Feel.PointOf(actor, new Vector2(0, 40)), catalog.Round,
                    new[] { Palette.Carrot, Palette.Lantern, Palette.Blush }, 8, 380f, 18f);
            }
        }

        /// <summary>A button that cannot be used yet was touched: it shakes, and if carrots are what is missing the purse flashes red.</summary>
        void Deny(ChunkyButton button, bool carrots)
        {
            button.Shake(Tweens);
            Sfx.Deny();
            if (!carrots) return;
            Color alarm = Palette.Tiers[5];
            Tweens.Add(0.45f, t =>
            {
                if (_coins == null) return;
                float k = Mathf.Abs(Mathf.Sin(t * Mathf.PI * 2f)) * (1f - t);
                _coins.color = Color.Lerp(Palette.Ink, alarm, k);
                _purse.localScale = Vector3.one * (1f + 0.06f * k);
            }, Ease.Linear, 0f, () =>
            {
                if (_coins == null) return;
                _coins.color = Palette.Ink;
                _purse.localScale = Vector3.one;
            });
        }

        void OnBuyPlot()
        {
            int before = Runner.State.Plots;
            if (!Runner.BuyPlot()) return;
            _plotBuy.Press(Tweens);
            Sfx.Unlock();
            // Name what the new land is for.
            var names = new List<string>();
            for (int i = before; i < Runner.State.Plots && i < Runner.Defs.Count; i++) names.Add(L10n.Station(Runner.Defs[i].Id));
            Feel.Moment(L10n.T("moment.plot.title"), L10n.T("moment.plot.sub", string.Join(", ", names)));
        }

        void OnSummon()
        {
            Rabbit rabbit = Runner.SummonRabbit();
            if (rabbit == null) return;
            _rabbitList.Summon.Press(Tweens);
            Sfx.Sting();
            Haptics.Medium();
            Welcome(rabbit);
        }

        void OnMerge(int tier)
        {
            Rabbit merged = Runner.MergeRabbits(tier);
            if (merged == null) return;
            Sfx.Sting();
            Welcome(merged);
        }

        /// <summary>A rabbit has just arrived, called or merged: it steps out of the moon gate.</summary>
        void Welcome(Rabbit rabbit)
        {
            if (rabbit.FirstOfKind) _freshKinds.Add(Rabbits.KindOf(rabbit));
            Arrival.Show(Runner.Catalog.RabbitArt(rabbit), rabbit.Name, L10n.T("detail.kind", L10n.Kind(rabbit), L10n.Tier(rabbit.Tier)),
                Palette.Tiers[rabbit.Tier], rabbit.FirstOfKind, L10n.T("summon.new"), L10n.T("summon.hint"), rabbit.Tier == 0);
        }

        void OnLevelUp(long rabbitId)
        {
            if (!Runner.LevelUpRabbit(rabbitId)) return;
            Sfx.Upgrade();
            Haptics.Light();
            Rabbit r = Rabbits.Find(Runner.State, rabbitId);
            Feel.FloatText(new Vector2(-110, 300), L10n.T("float.levelup", r.Name, r.Level), Palette.Lantern, 44);
        }

        /// <summary>Every station in the plan rises at once: one bump each, one sound, the numbers run up.</summary>
        void OnUpgradeAll()
        {
            List<UpgradeStep> plan = Runner.UpgradeAll();
            UpgradeAll.Close();
            if (plan == null) return;
            Sfx.Upgrade();
            Haptics.Light();
            foreach (UpgradeStep step in plan)
                if (step.Station < _actors.Count) _actors[step.Station].Bump();
        }

        /// <summary>A rabbit levels up as far as the purse allows, never past its cap.</summary>
        void OnLevelMax(long rabbitId)
        {
            int n = Runner.LevelUpRabbitMax(rabbitId);
            if (n <= 0) return;
            Sfx.Upgrade();
            Haptics.Light();
            Rabbit r = Rabbits.Find(Runner.State, rabbitId);
            Feel.FloatText(new Vector2(-110, 300), L10n.T("float.levelup", r.Name, r.Level), Palette.Lantern, 44);
        }

        void OnBuyUpgrade(int index)
        {
            if (!Runner.BuyUpgrade(index)) return;
            _upgradeRows[index].Buy.Press(Tweens);
            Sfx.Sting();
            UpgradeData data = Runner.Catalog.Upgrades[index];
            Feel.Moment(L10n.T("moment.upgrade.title", L10n.T("upgrade." + data.Id + ".name"), Runner.State.UpgradeLevels[index]), L10n.T("upgrade." + data.Id + ".desc"));
        }

        void OnClaim(int index)
        {
            QuestRowView row = RowOfQuest(index);
            // Where the reward starts from is read before the claim, which reorders and may hide the row.
            Vector2 carrotsFrom = Feel.PointOf(row != null ? row.CarrotChip : _purse), cloversFrom = Feel.PointOf(row != null ? row.CloverChip : _purse);
            if (!Runner.ClaimQuest(index, out BigNum paid)) return;
            if (row != null) row.Claim.Press(Tweens);
            Sfx.Reward();
            GameCatalog catalog = Runner.Catalog;
            string gained = "+" + paid.Short();
            // The carrots fly from the row up to the purse, which swells as they land.
            Feel.Fly(carrotsFrom, Feel.PointOf(_purse), catalog.Currency, Color.white, 6, 44f, () =>
            {
                if (_purse == null) return;
                Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), gained, Palette.Lantern, 46);
                Feel.Burst(Feel.PointOf(_purse), catalog.Currency, new[] { Color.white }, 8, 460f, 40f);
                Tweens.Add(Motion.Tap, t => { if (_purse != null) _purse.localScale = Vector3.one * (1f + 0.08f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_purse != null) _purse.localScale = Vector3.one; });
            });
            // Clovers fly to the rabbits tab, where they are spent.
            int clovers = Runner.QuestDefs[index].RewardClovers;
            if (clovers > 0)
            {
                Feel.Fly(cloversFrom, Feel.PointOf(_tabIcons[3].rectTransform), catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, clovers, 38f,
                    () => Tweens.Add(0.22f, t => _tabPop[3] = 0.14f * Ease.Pop(t), Ease.Linear, 0f, () => _tabPop[3] = 0f));
            }
            Item gift = Runner.LastQuestItem;
            if (gift != null)
                Feel.Moment(L10n.T("moment.gift.title", L10n.ItemName(gift)), L10n.T("moment.gift.sub", L10n.Tier(gift.Tier), ItemPickerView.Describe(gift)));
        }

        /// <summary>Collects every finished quest at once.</summary>
        void OnClaimAll()
        {
            _claimAll.Press(Tweens);
            for (int i = 0; i < Runner.QuestDefs.Count; i++)
                if (Quests.CanClaim(Runner.QuestDefs[i], Runner.Defs, Runner.State)) OnClaim(i);
        }

        /// <summary>
        /// Finished quests first, under the Ready heading; then today's, then the achievements. Only
        /// rows whose place has changed are moved.
        /// </summary>
        void OrderQuestRows()
        {
            var order = new List<Transform>();
            bool any = false;
            foreach (QuestRowView row in _questRows) any |= row.Visible && row.CanClaim;
            if (_questReady.gameObject.activeSelf != any) _questReady.gameObject.SetActive(any);
            order.Add(_questReady);
            foreach (QuestRowView row in _questRows) if (row.CanClaim) order.Add(row.Root);
            order.Add(_clovers.rectTransform);
            order.Add(_questToday.rectTransform);
            foreach (QuestRowView row in _questRows) if (row.Daily && !row.CanClaim) order.Add(row.Root);
            order.Add(_questAchievements.rectTransform);
            foreach (QuestRowView row in _questRows) if (!row.Daily && !row.CanClaim) order.Add(row.Root);
            for (int k = 0; k < order.Count; k++)
                if (order[k].GetSiblingIndex() != k) order[k].SetSiblingIndex(k);
        }

        /// <summary>Seconds until the player's local midnight, when the daily quests come back.</summary>
        public static double SecondsToNextDay(System.DateTimeOffset now)
        {
            var midnight = new System.DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, now.Offset).AddDays(1);
            return (midnight - now).TotalSeconds;
        }

        /// <summary>A span in the player's words: "5 giờ 12 phút", never less than a minute.</summary>
        static string Span(double seconds)
        {
            int minutes = Mathf.Max(1, Mathf.CeilToInt((float)(seconds / 60d)));
            return minutes >= 60 ? L10n.T("away.hours", minutes / 60, minutes % 60) : L10n.T("away.minutes", minutes);
        }

        QuestRowView RowOfQuest(int questIndex)
        {
            // Rows are listed dailies first, so a row's position is not its quest index.
            GameCatalog catalog = Runner.Catalog;
            int position = 0;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < catalog.Quests.Length; i++)
                {
                    if (catalog.Quests[i].Daily != (pass == 0)) continue;
                    if (i == questIndex) return _questRows[position];
                    position++;
                }
            return null;
        }

        /// <summary>A station finished a special batch: its product flies from the station down to its slot in the tray.</summary>
        public void OnProductMade(int station)
        {
            if (Shell.State == ShellState.Title || station < 0 || station >= _actors.Count) return;
            GameState s = Runner.State;
            int slot = -1;
            for (int i = 0; i < s.Tray.Count; i++) if (s.Tray[i].Station == station) slot = i;
            if (slot < 0) return;
            Refresh();
            RectTransform actor = _actors[station].Root;
            _actors[station].PopProp();
            if (!ProductTray.Root.gameObject.activeInHierarchy || !actor.gameObject.activeSelf) return;
            int landing = slot;
            Feel.Fly(Feel.PointOf(actor, new Vector2(112, -40)), Feel.PointOf(ProductTray.Slot(slot)), ProductTray.ProductSprite(station), Color.white, 1, 64f,
                () => { if (ProductTray != null) ProductTray.Pop(Tweens, landing); });
        }

        /// <summary>A slot was touched: its stack is sold and the carrots fly to the purse.</summary>
        void OnSellProduct(int slot)
        {
            GameState s = Runner.State;
            if (slot < 0 || slot >= s.Tray.Count) return;
            Vector2 from = Feel.PointOf(ProductTray.Slot(slot));
            BigNum paid = Runner.SellProduct(slot, out int clovers);
            PaySale(from, paid, clovers);
        }

        /// <summary>A sweep across the tray: everything is sold at once.</summary>
        void OnSellAllProducts()
        {
            GameState s = Runner.State;
            if (s.Tray.Count == 0) return;
            Vector2 from = Feel.PointOf(ProductTray.Slot(s.Tray.Count / 2));
            BigNum paid = Runner.SellAllProducts(out int clovers);
            PaySale(from, paid, clovers);
        }

        void PaySale(Vector2 from, BigNum paid, int clovers)
        {
            if (paid <= BigNum.Zero && clovers <= 0) return;
            Sfx.Reward();
            Haptics.Light();
            GameCatalog catalog = Runner.Catalog;
            string gained = "+" + paid.Short();
            Feel.Fly(from, Feel.PointOf(_purse), catalog.Currency, Color.white, 5, 44f, () =>
            {
                if (_purse == null) return;
                Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), gained, Palette.Lantern, 46);
                Tweens.Add(Motion.Tap, t => { if (_purse != null) _purse.localScale = Vector3.one * (1f + 0.08f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_purse != null) _purse.localScale = Vector3.one; });
            });
            if (clovers > 0)
                Feel.Fly(from, Feel.PointOf(_tabIcons[3].rectTransform), catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, clovers, 38f,
                    () => Tweens.Add(0.22f, t => _tabPop[3] = 0.14f * Ease.Pop(t), Ease.Linear, 0f, () => _tabPop[3] = 0f));
        }

        /// <summary>A finger sweeping across the tray by this many canvas units, as the drag handler sees it.</summary>
        public void SweepTray(float distance)
        {
            if (Mathf.Abs(distance) >= TrayView.SweepDistance) OnSellAllProducts();
        }

        void OnPayout(int index, BigNum amount)
        {
            if (Shell.State == ShellState.Title) return;
            RectTransform actor = _actors[index].Root;
            _actors[index].Hop();
            if (!actor.gameObject.activeSelf || !OnScreen(actor)) return;
            if (SheetOpen && Feel.PointOf(actor).y + ScreenH / 2f < SheetHeight + 60f) return; // it stands behind the sheet

            Feel.FloatText(Feel.PointOf(actor, new Vector2(60, 150)), "+" + amount.Short(), Palette.Paper);
            Feel.Burst(Feel.PointOf(actor, new Vector2(110, 60)), Runner.Catalog.Currency, new[] { Color.white }, 2, 300f, 34f);
            Sfx.Tick();

            // The purse answers too.
            Tweens.Add(0.15f, t =>
            {
                if (_currency != null) _currency.localScale = Vector3.one * (1f + 0.25f * Ease.Pop(t));
            }, Ease.Linear);
        }
    }
}

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
        // The lawn is as wide as the portrait screen and only scrolls up and down. At the usual zoom the screen is
        // wider than the lawn, so it sits in the middle with open grass and, further out, the wood past both sides (#50).
        const float WorldW = 1080f, WorldH = 2970f; // room below the last station, so it can scroll clear of the docked card
        /// <summary>The feet of the nearest trees of the wood stand this far past the lawn's sides: clear of every station by 200 and more.</summary>
        const float RimLeft = -300f, RimRight = WorldW + 310f;
        /// <summary>No tree of the wood comes nearer than this to a station's sign, pad, rabbit or prop, or to the props that must be read (#50).</summary>
        public const float CanopyClearance = 200f;
        /// <summary>Stations are drawn at this size so four or five fit on one screen.</summary>
        const float StationScale = 0.7f;
        const float DecorScale = 0.7f;

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
        /// <summary>The first minute's guidance.</summary>
        public TutorialView Guide { get; private set; }
        /// <summary>The tea shop by the pond: the daily gift and the golden call.</summary>
        public ShopView Shop { get; private set; }
        /// <summary>The welcome-back sheet (#69).</summary>
        public AwayView AwayCard { get; private set; }
        RectTransform _shopProp, _goldenChip, _mortarProp;
        Text _mortarPlate;
        /// <summary>The mooncake game, opened from the mortar beside the teapot.</summary>
        public PoundingView Pound { get; private set; }
        /// <summary>The card that asks which of two copies of the garden to keep.</summary>
        public CloudChoiceView CloudChoice { get; private set; }
        System.Action<SyncReport> _onSyncChoice;
        System.Action<Season> _onSeasonBegan;
        System.Action<Rabbit> _onSeasonArrived;
        /// <summary>The leaf-catching game, opened from the basket under the tea tree past the leaf station.</summary>
        public LeavesView Basket { get; private set; }
        RectTransform _basketProp;
        Text _basketPlate;
        /// <summary>The firefly night, opened from the glass jar on a stump by the pond.</summary>
        public FirefliesView Jar { get; private set; }
        RectTransform _jarProp;
        Text _jarPlate;
        GameObject _shopBadge;
        Text _goldenText;
        int _guideStep = -1;

        // ----- photo mode -----
        /// <summary>The HUD and the tab bar are away so the garden can be framed and snapped.</summary>
        public bool PhotoMode { get; private set; }
        /// <summary>Where pictures are written. Null: the Pictures folder of the machine.</summary>
        public string PhotoFolder;
        /// <summary>The last picture written, or null.</summary>
        public string LastPhotoPath { get; private set; }
        GameObject _photoBar, _photoReview, _photoFrame, _photoAlbum, _fireflyLayer;
        Image _photoPreview;
        Text _photoNote, _photoGift, _frameName, _frameDate, _albumTitle;
        RectTransform _albumGrid;
        Texture2D _photoTexture;
        readonly List<Texture2D> _albumTextures = new List<Texture2D>();
        readonly Dictionary<string, Image> _photoToggles = new Dictionary<string, Image>();
        readonly Dictionary<string, Text> _photoToggleLabels = new Dictionary<string, Text>();
        /// <summary>What a picture includes: the paper frame, the rabbits' name plates, the fireflies. Remembered on the device.</summary>
        public static bool PhotoFrameOn { get => PlayerPrefs.GetInt("photo.frame", 1) == 1; set => PlayerPrefs.SetInt("photo.frame", value ? 1 : 0); }
        public static bool PhotoNamesOn { get => PlayerPrefs.GetInt("photo.names", 1) == 1; set => PlayerPrefs.SetInt("photo.names", value ? 1 : 0); }
        public static bool PhotoFirefliesOn { get => PlayerPrefs.GetInt("photo.fireflies", 1) == 1; set => PlayerPrefs.SetInt("photo.fireflies", value ? 1 : 0); }
        RectTransform _upgradeAllRow;
        readonly GameObject[] _lists = new GameObject[TabCount];
        readonly Text[] _openLabels = new Text[TabCount];
        readonly Image[] _tabIcons = new Image[TabCount];
        readonly RectTransform[] _tabLifts = new RectTransform[TabCount];
        readonly CanvasGroup[] _tabLiftGroups = new CanvasGroup[TabCount];
        /// <summary>How far each tab has come towards its chosen look (0 resting, 1 chosen), and a short pop when pressed.</summary>
        readonly float[] _tabShown = new float[TabCount], _tabPop = new float[TabCount];
        float _tabFade = 1f, _badgePop, _safeBottom = -1f;
        bool _energyReady;
        // A finger resting on the purse: after a moment the whole number shows, and that touch is not a tap.
        bool _purseDown, _purseHeld;
        float _purseDownFor;
        const float PurseHoldSeconds = 0.45f;
        // Pills at the top and bottom edges that count what waits out of view, and the way home to the teapot.
        RectTransform _edgeUp, _edgeDown;
        Text _edgeUpText, _edgeDownText;
        int _edgeUpTarget = -1, _edgeDownTarget = -1;
        GameObject _home;
        int _shownClaimable = -1;
        RectTransform _barBadgeRect, _rabbitBadgeRect, _stationBadgeRect;
        Text _rabbitBadgeText, _stationBadgeText;
        const float TabBarHeight = 170f, TabBarGap = 16f, TabLabelSize = 26f;
        readonly List<Image> _tierChips = new List<Image>();
        readonly List<Text> _tierLabels = new List<Text>();
        readonly List<Image> _tierTradeBadges = new List<Image>();
        readonly List<Image> _tierTradeIcons = new List<Image>();
        // Life in the world: rabbits out for a walk, and ingredients carried between stations.
        /// <summary>A resting rabbit out on the lawn (#55): its picture, what it carries at its spot, and the stroll it is on.</summary>
        sealed class Walker
        {
            public RectTransform Rect, Art, Cup, Steam, Fly, Bubble;
            public Image ArtImage, SteamImage, FlyImage;
            public Text BubbleText;
            public float Phase;
            public Outing Out = new Outing();
        }
        /// <summary>One stroll of one rabbit: which of its strolls it is, whether it was let out, where to, and what it does there.</summary>
        sealed class Outing { public int Number = int.MinValue; public bool Granted; public int Spot = -1; public StrollActivity Activity; public float Linger; }
        /// <summary>A rabbit on its way to the station it was just given.</summary>
        sealed class Traveler { public RectTransform Rect; public long Id; public int Station; public Vector2 From, To; public float T, Duration; }
        readonly List<Traveler> _travelers = new List<Traveler>();
        long[] _workerIds = new long[0];
        bool _worldSeen;
        /// <summary>A stroll: this share of every period, from the seat out to the lane and back.</summary>
        public const float StrollPeriod = 48f, StrollShare = 1f / 3f;
        /// <summary>Seconds the screen has been animating; what the strolls and sways are timed on.</summary>
        public float Clock => _clock;
        /// <summary>How many rabbits are walking to a station right now.</summary>
        public int TravelersWalking => _travelers.Count;
        /// <summary>How many rabbits are seated or out strolling from a seat.</summary>
        public int SeatedCount => _seated;
        /// <summary>The scenery that moves: trees lean from the root, lanterns swing from the hook and flicker, the pond shimmers.</summary>
        readonly List<RectTransform> _swayTrees = new List<RectTransform>(), _swayLanterns = new List<RectTransform>();
        readonly List<Image> _lanternGlows = new List<Image>(), _ripples = new List<Image>();
        /// <summary>Every paper lantern's picture, for the colour the player hangs on them (#11), and the colour its glow takes.</summary>
        readonly List<Image> _lanternArt = new List<Image>();
        Color _lanternGlowColor = Palette.Lantern;

        /// <summary>Decorating the garden (#11): one object per spot, showing a faint "+" while something waits in storage, or the piece standing there.</summary>
        sealed class DecorSpotView { public RectTransform Root; public GameObject Plate; public Image Item; public string Shown; }
        readonly List<DecorSpotView> _decorSpots = new List<DecorSpotView>();
        /// <summary>The card for one decoration spot: what stands there, what waits in storage.</summary>
        public DecorView DecorCard { get; private set; }
        /// <summary>The spot whose card is open, or -1.</summary>
        public int DecorSpotOpen => DecorCard != null && DecorCard.IsOpen ? DecorCard.Spot : -1;
        /// <summary>How tall each kind of decoration stands, by its place in <see cref="Decor.Kinds"/>: lantern, planter, chair, mat.</summary>
        static readonly float[] DecorHeights = { 132f, 92f, 110f, 84f };

        /// <summary>
        /// The eight places a decoration can stand, placed by hand where the garden has room: two in the resting corner at the
        /// bottom, one between each pair of station clusters up the sides and the middle, and one by the tea shop near the pond.
        /// Every one keeps 200 units from every station and from the props that come with the garden.
        /// </summary>
        static readonly Vector2[] DecorSpots =
        {
            new Vector2(610f, 290f),   // 0 resting corner, by the tea table
            new Vector2(340f, 300f),   // 1 resting corner, left
            new Vector2(400f, 910f),   // 2 between the jam pot and the flower spring, left of the flower mortar's level plate
            new Vector2(930f, 992f),   // 3 between the star jar and the flower spring, under the star jar's level plate
            new Vector2(560f, 1290f),  // 4 between the teapot and the flower mortar
            new Vector2(130f, 1400f),  // 5 between the jam pot and the beehive, under the beehive's level plate
            new Vector2(120f, 1830f),  // 6 between the beehive and the chestnut oven
            new Vector2(390f, 2190f),  // 7 by the tea shop, on the way to the pond
        };

        /// <summary>Where decoration spot <paramref name="i"/> stands in the world: the feet of whatever is placed there.</summary>
        public static Vector2 DecorSpotAt(int i) => i >= 0 && i < DecorSpots.Length ? DecorSpots[i] : DecorSpots[0];

        /// <summary>The tint a paper lantern's picture takes for a lantern colour: none for paper, a wash of the colour for the rest.</summary>
        public static Color LanternTint(int color)
        {
            if (color <= 0 || color >= Decor.LanternColors.Length) return Color.white;
            return Palette.Mix(Color.white, Palette.Hex(Decor.LanternColors[color]), 0.7f);
        }
        RectTransform _pondGlow;
        /// <summary>The wood around the clearing, on its own layer that slides a little slower than the ground.</summary>
        RectTransform _canopy;
        public const float CanopyDrift = 0.9f;
        RawImage _moonbeam;
        Image _moonPool;
        const float MoonbeamAlpha = 0.9f, MoonPoolAlpha = 0.4f;

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
        /// <summary>A seated rabbit's hat and its name plate (#3).</summary>
        readonly List<Image> _seatHats = new List<Image>();
        readonly List<Text> _seatNames = new List<Text>();
        /// <summary>How a rabbit holds itself at each place: a squat on the stumps, the rug, the stool and the swing; lying in the hammock.</summary>
        static readonly bool[] SeatLies = { false, false, false, false, true, false };
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
        /// <summary>Where the carrots come from, opened from the purse.</summary>
        public IncomeView Income { get; private set; }
        /// <summary>The player's profile: avatar, garden name, player code, backup.</summary>
        public ProfileView Profile { get; private set; }
        public RabbitDetailView Detail { get; private set; }
        /// <summary>The moon gate a newly called or merged rabbit steps out of.</summary>
        public SummonMomentView Arrival { get; private set; }
        /// <summary>The new moon (#1): the compare card and the moment that follows it.</summary>
        public NewMoonView NewMoonCard { get; private set; }
        /// <summary>Golden carrots for money (#9).</summary>
        public GoldenPacksView GoldenPacks { get; private set; }
        public NewMoonMomentView NewMoonMoment { get; private set; }
        RectTransform _seasonChip;
        Text _seasonText;
        /// <summary>Kinds met for the first time and not yet looked at in the album: their cells are marked.</summary>
        /// <summary>A kind that arrived and has not been looked at yet: the save remembers, so the mark outlives the session.</summary>
        public bool IsFreshKind(string kindId) => Runner.State.FreshKinds.Contains(kindId);
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
        /// <summary>Nothing that must be read stands further right than this: the canopy's crowns reach in from the edge.</summary>
        public const float ReadableRight = 1000f;
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
        Text _coins, _perSecond, _barBadgeText, _clovers, _energyLabel, _energyAction, _questToday, _questAchievements;
        RectTransform _questReady, _cardModes;
        ChunkyButton _claimAll;
        Text _plotTitle, _plotSub, _plotCost;
        ChunkyButton _plotBuy, _energyButton;
        GameObject _hint, _sheet, _modes, _barBadge;
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
            UiKit.TextChanged += RebuildUi;
            TrackpadPinch.Start();
            RefreshNow();
        }

        void OnDestroy()
        {
            TrackpadPinch.Stop();
            L10n.Changed -= RebuildUi;
            UiKit.TextChanged -= RebuildUi;
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
            UpdateEdges();
            Pound.Update(dt);
            Basket.Update(dt);
            Jar.Update(dt);
            if (_purseDown && !_purseHeld)
            {
                _purseDownFor += dt;
                if (_purseDownFor >= PurseHoldSeconds)
                {
                    _purseHeld = true;
                    Sfx.Blip();
                    Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -70)), L10n.T("purse.full", s.Coins.Full()), Palette.Ink, 34);
                }
            }
            if (Shell.State == ShellState.Playing)
            {
                // Remember where the player is looking, so the next opening glides back here.
                s.ViewX = (ScreenW / 2f - _street.anchoredPosition.x) / _zoom;
                s.ViewY = (ScreenH / 2f - _street.anchoredPosition.y) / _zoom;
                s.ViewZoom = _zoom;
            }
            // A full bar breathes: the one hint that the boost is there to be taken.
            _energyButton.FaceRect.localScale = Vector3.one * (_energyReady ? 1f + 0.025f * Mathf.Sin(_clock * 4f) : 1f);

            Tweens.Update(dt);
            PlaceTabBar();
            ShowTabBar(dt);
            if (Arrival != null) Arrival.Update(dt);
            if (NewMoonCard != null) NewMoonCard.Update(dt);
            if (NewMoonMoment != null) NewMoonMoment.Update(dt);
            Feel.Update(dt);
            Shell.Update(_clock);
            UpdateWorld(dt);
            UpdateFireflies();
            UpdateGuide(dt);
            UpdateBoostEdge(boosting, dt);

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
        /// <summary>The station card stands right on the tab bar: the bar's gap and height, plus a thin seam.</summary>
        const float HudCover = 160f, TabCover = TabBarGap + TabBarHeight + 10f, CardHeight = 340f;

        public enum ZoomLevel { Far, Default, Near }

        /// <summary>
        /// Far shows the whole garden top to bottom; near is for looking at one rabbit. The usual view stands back 17 %
        /// (#50): the screen spans 1300 garden units, so three station columns and five or more plots are in view at once.
        /// </summary>
        public const float ZoomFar = ScreenH / WorldH, ZoomDefault = 0.83f, ZoomNear = 1.5f;
        float _zoom = ZoomDefault;
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
        bool _pinching, _padPinching;
        float _pinchStartDistance, _pinchStartZoom, _wheelWait, _padPinchQuiet;
        Vector2 _pinchAt;
        /// <summary>
        /// How much a trackpad pinch zooms: the zoom is multiplied by 1 + gain × magnification each frame, so
        /// a whole pinch (its magnifications sum to about ±1) spans e ≈ 2.7×, a little more than Far to Near (2.3×).
        /// </summary>
        public const float PinchGain = 1f;

        public float ZoomScale => _zoom;
        public ZoomLevel Zoom => _zoom < (ZoomFar + ZoomDefault) / 2f ? ZoomLevel.Far : (_zoom > (ZoomDefault + ZoomNear) / 2f ? ZoomLevel.Near : ZoomLevel.Default);

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
            SetViewRaw(world, atView, zoom);
        }

        /// <summary>The same as <see cref="PlaceView"/> but without letting go of a glide: what a glide itself calls.</summary>
        void SetViewRaw(Vector2 world, Vector2 atView, float zoom)
        {
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
            if (SheetOpen)
            {
                CloseSheet(); // a touch on the garden above the sheet puts the sheet away
                return;
            }
            SelectStation(-1);
            Vector2 at = new Vector2(ScreenW / 2f, ScreenH / 2f);
            var mouse = UnityEngine.InputSystem.Mouse.current;
            var touch = UnityEngine.InputSystem.Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.wasReleasedThisFrame) at = ScreenToView(touch.primaryTouch.position.ReadValue());
            else if (mouse != null && Application.isPlaying) at = ScreenToView(mouse.position.ReadValue());
            // From far away one touch is enough: it comes in to the usual zoom on the spot that was touched.
            if (Zoom == ZoomLevel.Far)
            {
                _lastGroundTapAt = -10f;
                SetZoom(ZoomLevel.Default, true, at);
                DismissHint();
                return;
            }
            bool second = _clock - _lastGroundTapAt < 0.35f;
            _lastGroundTapAt = second ? -10f : _clock;
            if (!second) return;
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

        /// <summary>The zoom after one frame of trackpad pinch, kept between Far and Near.</summary>
        public static float PinchZoom(float current, float magnification)
        {
            float factor = Mathf.Max(0.1f, 1f + magnification * PinchGain); // a wild negative sum must not flip the garden
            return Mathf.Clamp(current * factor, ZoomFar, ZoomNear);
        }

        /// <summary>One frame of trackpad pinch around a point of the view (canvas units from the bottom left).</summary>
        public void SimulatePinch(float magnification, Vector2 viewPoint) => OnTrackpadPinch(magnification, viewPoint);

        void OnTrackpadPinch(float magnification, Vector2 viewPoint)
        {
            if (_street == null) return;
            _pinchAt = viewPoint;
            _padPinchQuiet = 0f;
            if (!_padPinching)
            {
                _padPinching = true;
                if (_scroll != null) _scroll.StopMovement();
            }
            ApplyZoom(PinchZoom(_zoom, magnification), viewPoint);
            DismissHint();
        }

        /// <summary>Pinch on a phone or a trackpad, wheel on a desktop. All land on one of the three levels.</summary>
        void HandleZoomInput(float dt)
        {
            // Drained every frame, even while the sheet is open, so nothing piles up and lands at once later.
            bool padPinched = TrackpadPinch.Read(out float padMagnification, out Vector2 padScreen, out bool smartMagnify);
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

            // A pinch on a Mac trackpad zooms around the pointer; a two-finger double tap goes in close there, or back.
            if (padPinched)
            {
                Vector2 at = ScreenToView(padScreen);
                if (smartMagnify)
                {
                    SelectStation(-1);
                    SetZoom(Zoom == ZoomLevel.Near ? ZoomLevel.Default : ZoomLevel.Near, true, at);
                    DismissHint();
                    _padPinching = false;
                    return;
                }
                OnTrackpadPinch(padMagnification, at);
                return;
            }
            if (_padPinching)
            {
                _padPinchQuiet += dt;
                if (_padPinchQuiet < 0.25f) return; // the fingers are still on the glass
                _padPinching = false;
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

            UpdateStrollers();

            UpdateScenery();
            UpdateTravelers(dt);
            if (_canopy != null)
            {
                // The canopy follows the ground at nine tenths of its pace, measured from the view that rests on the teapot.
                _canopy.localScale = _street.localScale;
                Vector2 rest = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - Heart * _zoom, _zoom);
                _canopy.anchoredPosition = rest + (_street.anchoredPosition - rest) * CanopyDrift;
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

        /// <summary>Whether the rabbit of a seat is out walking at this moment: half of every 48 seconds, each at its own time.</summary>
        public static bool Strolling(int seat, float clock) => StrollPhase(seat, clock) < StrollShare;

        static float StrollPhase(int seat, float clock) => Mathf.Repeat(clock / StrollPeriod + seat * 0.37f, 1f);

        /// <summary>Trees lean from the root, lanterns swing from the hook and flicker, the pond shimmers and ripples. All still under reduced motion.</summary>
        void UpdateScenery()
        {
            bool still = Motion.Reduced;
            for (int i = 0; i < _swayTrees.Count; i++)
                _swayTrees[i].localRotation = still ? Quaternion.identity
                    : Quaternion.Euler(0, 0, Mathf.Sin(_clock * 0.45f + i * 1.7f) * 1.6f + Mathf.Sin(_clock * 1.3f + i * 0.9f) * 0.4f);
            for (int i = 0; i < _swayLanterns.Count; i++)
            {
                _swayLanterns[i].localRotation = still ? Quaternion.identity : Quaternion.Euler(0, 0, Mathf.Sin(_clock * 1.4f + i * 2.3f) * 3f);
                float flicker = still ? 1f : 0.86f + 0.14f * Mathf.Sin(_clock * 9f + i * 3.1f) * Mathf.Sin(_clock * 2.3f + i);
                if (i < _lanternGlows.Count) _lanternGlows[i].color = Palette.Alpha(_lanternGlowColor, LanternGlowAlpha * flicker);
            }
            if (_moonbeam != null && _moonbeam.texture != null) _moonbeam.color = new Color(0.82f, 0.9f, 1f, MoonbeamAlpha + (still ? 0f : 0.1f * Mathf.Sin(_clock * 0.7f)));
            if (_moonPool != null) _moonPool.color = new Color(0.82f, 0.9f, 1f, MoonPoolAlpha + (still ? 0f : 0.05f * Mathf.Sin(_clock * 0.7f + 1f)));
            if (_pondGlow != null)
                _pondGlow.localScale = still ? Vector3.one : new Vector3(1f + 0.03f * Mathf.Sin(_clock * 0.9f), 1f + 0.02f * Mathf.Sin(_clock * 1.3f + 1f), 1f);
            for (int i = 0; i < _ripples.Count; i++)
            {
                float k = still ? 0f : Mathf.Repeat(_clock / 2.8f + i * 0.5f, 1f);
                _ripples[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(0.35f, 1.1f, k);
                _ripples[i].color = new Color(0.85f, 0.93f, 1f, still ? 0f : 0.22f * (1f - k));
            }
        }
        const float LanternGlowAlpha = 0.6f;

        /// <summary>Stands a piece of scenery on the ground at (x, y) so it can lean from there, and names it for what it is.</summary>
        static void Rooted(Image img, string name, float x, float y, List<RectTransform> into)
        {
            if (img == null) return;
            RectTransform rt = img.rectTransform;
            rt.name = name;
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, y);
            into.Add(rt);
        }

        /// <summary>A rabbit just given a station sets off from where it was (its old station, or its seat) and walks there.</summary>
        void StartTravel(Rabbit worker, int station)
        {
            CancelTravel(station);
            Vector2 from = _seats.Count > 0 ? _seats[0].anchoredPosition : Heart;
            for (int j = 0; j < _workerIds.Length; j++) if (j != station && _workerIds[j] == worker.Id && j < _actors.Count) from = StationSpot(j) + _actors[j].RabbitHome;
            for (int k = 0; k < _seats.Count; k++) if (_seatIds[k] == worker.Id) from = _seats[k].anchoredPosition;
            Vector2 to = StationSpot(station) + _actors[station].RabbitHome;
            var rt = UiKit.Rect("Traveler", _street).Place(0f, 0f, from.x, from.y, 104, 110);
            rt.pivot = new Vector2(0.5f, 0f);
            UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 2, 68, 16).AddImage(Runner.Catalog.Round, Palette.Alpha(Palette.Night, 0.35f));
            Image art = UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(Runner.Catalog.RabbitArt(worker), RabbitGridView.Fur(worker));
            art.preserveAspect = true;
            if (_standing != null && _standing.Count > 0 && _standing[0].Value != null) rt.SetSiblingIndex(_standing[0].Value.GetSiblingIndex()); // behind everything that stands, under every plate
            _actors[station].SetAway(true);
            _travelers.Add(new Traveler { Rect = rt, Id = worker.Id, Station = station, From = from, To = to, Duration = Mathf.Clamp(Vector2.Distance(from, to) / 300f, 0.4f, 2.4f) });
        }

        void CancelTravel(int station)
        {
            for (int i = _travelers.Count - 1; i >= 0; i--)
            {
                if (_travelers[i].Station != station) continue;
                Discard(_travelers[i].Rect.gameObject);
                _travelers.RemoveAt(i);
            }
            if (station < _actors.Count) _actors[station].SetAway(false);
        }

        /// <summary>The walk itself: a straight line with little hops, facing the way it goes; on arrival the rabbit appears and hops once.</summary>
        void UpdateTravelers(float dt)
        {
            for (int i = _travelers.Count - 1; i >= 0; i--)
            {
                Traveler t = _travelers[i];
                t.T += dt / t.Duration;
                if (t.T >= 1f)
                {
                    Discard(t.Rect.gameObject);
                    _travelers.RemoveAt(i);
                    if (t.Station < _actors.Count) { _actors[t.Station].SetAway(false); _actors[t.Station].Hop(); }
                    continue;
                }
                Vector2 pos = Vector2.Lerp(t.From, t.To, t.T);
                pos.y += Mathf.Abs(Mathf.Sin(t.T * Mathf.PI * Mathf.Max(2f, Mathf.Round(t.Duration * 4f)))) * 14f;
                t.Rect.anchoredPosition = pos;
                t.Rect.localScale = new Vector3(t.To.x >= t.From.x ? 1f : -1f, 1f, 1f);
            }
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
            UpdateOrders(dt);
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
            Guide = null;
            _guideStep = -1;
            Shop = null;
            AwayCard = null;
            DecorCard = null;
            _decorSpots.Clear(); _lanternArt.Clear(); _lanternGlowColor = Palette.Lantern;
            PhotoMode = false;
            _tierChips.Clear(); _tierLabels.Clear(); _tierTradeBadges.Clear(); _tierTradeIcons.Clear();
            _travelers.Clear(); _swayTrees.Clear(); _swayLanterns.Clear(); _lanternGlows.Clear(); _ripples.Clear(); _pondGlow = null; _worldSeen = false;
            _seatHats.Clear(); _seatNames.Clear();
            _walkers.Clear(); _seats.Clear(); _seatArt.Clear(); _seatCushions.Clear(); _seatNaps.Clear(); _seated = 0; _couriers.Clear(); _supplyRoutes.Clear(); _glowSpots.Clear();
            Selected = -1;
            _shownStations = -1;
            // The old helpers belong to the destroyed canvas; Build makes new ones.
            Feel = null;
            Picker = null;
            Workers = null;
            Rename = null;
            Profile = null;
            Income = null;
            Pound = null;
            Basket = null;
            Jar = null;
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
            UiKit.LoadTextSize();
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
            BuildEdges(catalog);
            BuildSheet(catalog);
            Feel = new FeelLayer(_canvas, catalog, Tweens);
            Arrival = new SummonMomentView(_canvas, catalog);
            Detail = new RabbitDetailView(_canvas, catalog, Tweens, OnLevelUp, OnLevelMax,
                rabbitId => { Sfx.Click(); Rename.Open(Runner.State, rabbitId); },
                (rabbitId, slot) => { Sfx.Click(); Picker.Open(Runner.State, rabbitId, slot); },
                (rabbitId, station) => { Sfx.Blip(); Runner.AssignRabbit(rabbitId, station); },
                rabbitId => { Sfx.Click(); Runner.AssignRabbit(rabbitId, -1); },
                OnDye, rabbitId => { if (Runner.WashRabbit(rabbitId)) Sfx.Click(); });
            Picker = new ItemPickerView(_canvas, catalog, Tweens,
                (itemId, rabbitId) => { Sfx.Blip(); Runner.EquipItem(itemId, rabbitId); },
                itemId => { Sfx.Click(); Runner.UnequipItem(itemId); });
            Rename = new RenameView(_canvas, catalog, Tweens, (rabbitId, name) => { Sfx.Blip(); Runner.RenameRabbit(rabbitId, name); });
            Workers = new WorkerPickerView(_canvas, catalog, Tweens, (rabbitId, station) => { Sfx.Blip(); Runner.AssignRabbit(rabbitId, station); });
            UpgradeAll = new UpgradeAllView(_canvas, catalog, Tweens, share => Runner.SetSpendShare(share), OnUpgradeAll);
            BuildAwayPopup(catalog);
            Guide = new TutorialView(_canvas, catalog, Feel, () => Runner.SkipTutorial());
            Shop = new ShopView(_canvas, catalog, Tweens, () => Runner.TodayNumber, () => SecondsToNextDay(System.DateTimeOffset.Now) / 3600d, OnDailyGift, OnSummonGolden, OnBuyVisitor,
                Runner.ItemDefs, OnBuyHat, OnBuyDecor, OnLanternColor, () => GoldenPacks.Open(Runner.Store));
            DecorCard = new DecorView(_canvas, catalog, Tweens, OnPlaceDecor, OnStoreDecor);
            BuildOrders(catalog);
            Shell = new ShellView(_canvas, catalog, Tweens, () => Runner.ResetGame(), () => Profile.Open(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus),
                () => NewMoon.CanBegin(Runner.State), () => NewMoon.LevelNeeded(Runner.State.Season), () => NewMoonCard.Open(Runner.Defs, Runner.State), () => NewMoon.AtCap(Runner.State));
            // Built after the shell so it draws over the pause card it opens from; the rename card is moved above it.
            Profile = new ProfileView(_canvas, catalog, Tweens,
                () => { Runner.NextAvatar(); Profile.Refresh(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus); },
                () => Rename.Open(L10n.T("profile.rename"), Runner.State.Profile.GardenName, VuonTra.Core.Profile.MaxNameLength, name =>
                {
                    Sfx.Blip();
                    Runner.RenameGarden(name);
                    Profile.Refresh(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus);
                }),
                () => Runner.LinkAccount((ok, gift) =>
                {
                    Profile.Refresh(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus);
                    if (gift) Feel.FloatText(Feel.PointOf(Profile.Root), L10n.T("backup.rewarded", VuonTra.Core.Profile.LinkRewardClovers), Palette.Lantern, 44);
                }),
                () => Runner.PushToCloud(r => Profile.Refresh(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus)),
                () => Runner.RestoreFromCloud(r =>
                {
                    Profile.Refresh(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus);
                    string note = r.Outcome == SyncOutcome.TookRemote ? "cloud.restored" : (r.Outcome == SyncOutcome.Nothing ? "cloud.nothing" : (r.Outcome == SyncOutcome.Corrupt ? "cloud.status.corrupt" : null));
                    if (note != null) Feel.FloatText(Feel.PointOf(Profile.Root), L10n.T(note), Palette.Ink, 34);
                    Refresh(-1f);
                }),
                () =>
                {
                    if (!Runner.RestoreLocalBackup()) return;
                    Profile.Refresh(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus);
                    Feel.FloatText(Feel.PointOf(Profile.Root), L10n.T("cloud.local.restored"), Palette.Ink, 34);
                    Refresh(-1f);
                },
                () => Runner.DeleteMyData(ok =>
                {
                    Profile.Close();
                    Shell.Resume();
                    Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -90)), L10n.T(ok ? "privacy.deleted" : "privacy.delete.partly"), Palette.Ink, 34);
                    Refresh(-1f);
                }));
            // Two copies of the garden that differ: the choice card, over everything, raised by the runner.
            NewMoonCard = new NewMoonView(_canvas, catalog, Tweens, OnNewMoonBegun);
            GoldenPacks = new GoldenPacksView(_canvas, catalog, Tweens, OnBuyPack, done => Runner.RestorePurchases(done));
            NewMoonMoment = new NewMoonMomentView(_canvas, catalog);
            CloudChoice = new CloudChoiceView(_canvas, catalog, Tweens, takeRemote => Runner.ResolveSync(takeRemote, r =>
            {
                Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -90)), L10n.T(r.Outcome == SyncOutcome.TookRemote ? "cloud.restored" : "cloud.kept"), Palette.Ink, 34);
                Refresh(-1f);
            }));
            if (_onSeasonBegan != null) Runner.SeasonBegan -= _onSeasonBegan;
            _onSeasonBegan = season =>
            {
                Sfx.Sting();
                RabbitKind kind = Seasons.KindOf(season);
                Feel.Moment(L10n.T("season.began.title", L10n.Season(season)), L10n.T("season.began.sub", kind != null ? L10n.T("kind." + kind.Id) : ""));
            };
            Runner.SeasonBegan += _onSeasonBegan;
            if (_onSeasonArrived != null) Runner.SeasonArrived -= _onSeasonArrived;
            _onSeasonArrived = rabbit => { Sfx.Summon(rabbit.Tier); Welcome(rabbit); };
            Runner.SeasonArrived += _onSeasonArrived;
            if (_onSyncChoice != null) Runner.SyncChoiceNeeded -= _onSyncChoice;
            _onSyncChoice = r => CloudChoice.Open(r);
            Runner.SyncChoiceNeeded += _onSyncChoice;
            if (Runner.Sync.Pending != null) CloudChoice.Open(Runner.Sync.Pending);
            Income = new IncomeView(_canvas, catalog, Tweens);
            Pound = new PoundingView(_canvas, catalog, Tweens, () => Refresh(-1f), run =>
            {
                PoundingResult result = Runner.FinishPounding(run);
                if (result.Stars >= 3) Sfx.Milestone(); else if (result.Stars >= 1) Sfx.Reward(); else Sfx.Deny();
                Pound.ShowPaid(result);
            });
            Basket = new LeavesView(_canvas, catalog, Tweens, () => Refresh(-1f), run =>
            {
                LeavesResult result = Runner.FinishLeaves(run);
                if (result.Stars >= 3) Sfx.Milestone(); else if (result.Stars >= 1) Sfx.Reward(); else Sfx.Deny();
                Basket.ShowPaid(result);
            });
            Jar = new FirefliesView(_canvas, catalog, Tweens, () => Refresh(-1f), run =>
            {
                FirefliesResult result = Runner.FinishFireflies(run);
                if (result.Stars >= 3) Sfx.Milestone(); else if (result.Stars >= 1) Sfx.Reward(); else Sfx.Deny();
                Jar.ShowPaid(result);
            });
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
            // The canopy lies over the content and follows it at nine tenths of the pace: the trees at the edge drift a little, for depth.
            _canopy = UiKit.Rect("Canopy", view);
            _canopy.anchorMin = _canopy.anchorMax = _canopy.pivot = Vector2.zero;
            _canopy.sizeDelta = _street.sizeDelta;
            _canopy.localScale = _street.localScale;

            int count = catalog.Stations.Length;
            BuildGroundLayers(catalog, count);

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
                if (catalog.Decor != null && catalog.Decor.Length > 5 && catalog.Decor[1] != null && catalog.Decor[4] != null)
                {
                    Color dim = Palette.Mix(Color.white, Palette.Night, 0.3f);
                    // Low growth on the station's own ground: bushes and mushrooms, so nothing tall cuts the view of the
                    // lawn or hides a neighbour that is open (#50: the big trees all stand in the wood around the garden).
                    Rooted(AddDecor(grove, catalog.Decor[1], spot.x - 66f, spot.y - 40f, 96f, dim), "Bush", spot.x - 66f, spot.y - 40f, _swayTrees);
                    Rooted(AddDecor(grove, catalog.Decor[1], spot.x + 84f, spot.y - 70f, 84f, dim), "Bush", spot.x + 84f, spot.y - 70f, _swayTrees);
                    Image caps = AddDecor(grove, catalog.Decor[4], spot.x + 10f, spot.y - 120f, 56f, dim);
                    if (caps != null) caps.name = "Mushrooms";
                }
                _fogTrees.Add(grove.gameObject);
                standing.Add(new KeyValuePair<float, Transform>(spot.y - 40f, grove));
            }
            BuildDecor(catalog, count, standing);

            for (int i = 0; i < count; i++)
            {
                Vector2 spot = StationSpot(i);
                var actor = new StationActorView(i);
                actor.Build(_street, catalog, catalog.Stations[i], OnTap, OnPad, OnStroke, _scroll);
                actor.Root.Place(0f, 0f, spot.x, spot.y, 440, 360);
                actor.BaseScale = StationSize(i);
                actor.Root.localScale = Vector3.one * StationSize(i);
                // Every station's level reads at the same size, clear of the rabbit's feet and the prop.
                actor.PlacePlate(0.78f / StationSize(i), new Vector2(0, -26f));
                _actors.Add(actor);
                _workerIds = new long[_actors.Count];
                standing.Add(new KeyValuePair<float, Transform>(spot.y - 112f * StationSize(i), actor.Root));

                // Every name plate is the same size on screen and floats the same distance above its rabbit.
                var chip = UiKit.Rect("Tier", actor.Root).Place(0.5f, 0.5f, -72, 122 + 40f / StationSize(i), 200, 44);
                chip.localScale = Vector3.one * (0.78f / StationSize(i));
                _tierChips.Add(chip.AddImage(catalog.Panel, Palette.Paper, true));
                _tierLabels.Add(UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1, 34, 0, 6, 0).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold));
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
                seatButton.onClick.AddListener(() => OnSeat(seat));
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 4, 84, 20).AddImage(catalog.Round, shade);
                Image art = UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white);
                art.preserveAspect = true;
                UiKit.Strokable(art.rectTransform, _scroll, () => StrokeSeat(seat), () => OnSeat(seat)); // #19: stroked where it sits
                rt.localScale = Vector3.one * SeatScale[k];
                // The hat hangs from the picture, so it lies down with it in the hammock; the name sits under the seat.
                var hatOn = UiKit.Rect("HatOn", art.rectTransform);
                Image hatImage = hatOn.AddImage(null, Color.white);
                hatImage.preserveAspect = true;
                hatOn.gameObject.SetActive(false);
                _seatHats.Add(hatImage);
                var namePlate = UiKit.Rect("Name", rt).Place(0.5f, 0f, 0, -16, 150, 34);
                namePlate.AddImage(catalog.Panel, Palette.Paper, true);
                Text nameText = UiKit.Rect("Text", namePlate).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                nameText.resizeTextForBestFit = true;
                nameText.resizeTextMinSize = 12;
                nameText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                nameText.verticalOverflow = VerticalWrapMode.Truncate;
                _seatNames.Add(nameText);
                // Resting looks different from working: a slow sway, and now and then a doze.
                var nap = UiKit.Rect("Nap", rt).Place(1f, 1f, 6, -6, 60, 34);
                nap.AddImage(catalog.Panel, Palette.Paper, true);
                UiKit.Rect("Text", nap).Stretch(0, 0, 1, 1).AddText("z z", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                nap.gameObject.SetActive(false);
                _seatNaps.Add(nap.gameObject);
                _seats.Add(rt);
                _seatArt.Add(art);
                rt.gameObject.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>((Heart + Seats[k]).y + 60f, cushion)); // the furniture is always behind whoever sits on it
                standing.Add(new KeyValuePair<float, Transform>((Heart + Seats[k]).y, rt));
            }

            // Now and then a seated rabbit gets up and strolls to an empty spot of the lawn (#55).
            BuildWalkers(catalog, shade, standing);

            // The tea shop stands at the edge of the pond from the first day; a touch opens it.
            if (catalog.ShopProp != null)
            {
                Vector2 shopAt = PondSpot + new Vector2(-330f, -150f);
                float shopHeight = 300f * DecorScale, shopWidth = shopHeight * catalog.ShopProp.rect.width / catalog.ShopProp.rect.height;
                _shopProp = UiKit.Rect("Shop", _street).Place(0f, 0f, shopAt.x, shopAt.y + shopHeight / 2f, shopWidth, shopHeight);
                Image shopImage = _shopProp.AddImage(catalog.ShopProp, Color.white);
                shopImage.raycastTarget = true;
                var shopButton = _shopProp.gameObject.AddComponent<Button>();
                shopButton.transition = Selectable.Transition.None;
                shopButton.targetGraphic = shopImage;
                shopButton.onClick.AddListener(() => { Sfx.Click(); OpenShop(); });
                var shopBadge = UiKit.Rect("Badge", _shopProp).Place(1f, 1f, -30, -20, 52, 52);
                shopBadge.AddImage(catalog.Round, Palette.CarrotLip);
                UiKit.Rect("Text", shopBadge).Stretch(0, 0, 1, 1).AddText("!", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                _shopBadge = shopBadge.gameObject;
                _shopBadge.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>(shopAt.y, _shopProp));
            }
            // The mortar for the mooncake game stands near the teapot from garden level three; a touch starts a game.
            {
                Vector2 mortarAt = MortarSpot; // above the teapot's name, between the oven and the milk tea
                _mortarProp = UiKit.Rect("Mortar", _street).Place(0f, 0f, mortarAt.x, mortarAt.y + 80f, 170, 170);
                Image mortarImage = _mortarProp.AddImage(catalog.Mortar != null ? catalog.Mortar : catalog.Round, catalog.Mortar != null ? Color.white : Palette.Mix(Palette.Ink, Palette.Paper, 0.4f));
                mortarImage.preserveAspect = true;
                mortarImage.raycastTarget = true;
                var mortarButton = _mortarProp.gameObject.AddComponent<Button>();
                mortarButton.transition = Selectable.Transition.None;
                mortarButton.targetGraphic = mortarImage;
                mortarButton.onClick.AddListener(OpenPounding);
                var plate = UiKit.Rect("Plate", _mortarProp).Place(0.5f, 0f, 0, -26, 250, 44);
                plate.AddImage(catalog.Panel, Palette.Paper, true);
                _mortarPlate = UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                _mortarPlate.resizeTextForBestFit = true;
                _mortarPlate.resizeTextMinSize = 14;
                _mortarPlate.resizeTextMaxSize = 20;
                _mortarProp.gameObject.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>(mortarAt.y, _mortarProp));
            }
            // The basket for the leaf-catching game waits under a small tea tree past the leaf basket station from garden level four.
            {
                int leafStation = 1;
                for (int i = 0; i < Runner.Defs.Count; i++) if (Runner.Defs[i].Id == "gio-la") { leafStation = i; break; }
                // Past the leaf station on the right, in the one gap between its name and the next station's plate: a
                // small basket, with its plate hung on its left so neither covers a sign.
                Vector2 basketAt = StationSpot(leafStation) + new Vector2(104f, 130f);
                if (catalog.Decor != null && catalog.Decor.Length > 0 && catalog.Decor[0] != null)
                {
                    Image tree = AddDecor(_street, catalog.Decor[0], basketAt.x + 30f, basketAt.y + 40f, 150f * DecorScale, Color.white);
                    Rooted(tree, "Tree", basketAt.x + 30f, basketAt.y + 40f, _swayTrees);
                    standing.Add(new KeyValuePair<float, Transform>(basketAt.y + 40f, tree.transform));
                }
                Sprite basketSprite = leafStation < catalog.Stations.Length && catalog.Stations[leafStation] != null ? catalog.Stations[leafStation].Prop : null;
                _basketProp = UiKit.Rect("Basket", _street).Place(0f, 0f, basketAt.x, basketAt.y + 48f, 110, 95);
                Image basketImage = _basketProp.AddImage(basketSprite != null ? basketSprite : catalog.Round, basketSprite != null ? Color.white : Palette.Mix(Palette.Carrot, Palette.Ink, 0.35f));
                basketImage.preserveAspect = true;
                basketImage.raycastTarget = true;
                var basketButton = _basketProp.gameObject.AddComponent<Button>();
                basketButton.transition = Selectable.Transition.None;
                basketButton.targetGraphic = basketImage;
                basketButton.onClick.AddListener(OpenLeaves);
                var basketPlate = UiKit.Rect("Plate", _basketProp).Place(0f, 0.5f, -120, 0, 240, 40); // wide enough for "Hứng lá · còn 3 lượt" on one line
                basketPlate.AddImage(catalog.Panel, Palette.Paper, true);
                _basketPlate = UiKit.Rect("Text", basketPlate).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                _basketPlate.resizeTextForBestFit = true;
                _basketPlate.resizeTextMinSize = 14;
                _basketPlate.resizeTextMaxSize = 20;
                _basketProp.gameObject.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>(basketAt.y, _basketProp));
            }
            // The jar for the firefly night waits on a stump by the pond from garden level five: glass, a glow inside, a cork.
            {
                // Far enough from the right edge that its plate ends before the canopy's trees, which are drawn over the garden.
                Vector2 jarAt = JarSpot;
                if (catalog.Decor != null && catalog.Decor.Length > 8 && catalog.Decor[8] != null)
                {
                    Image stump = AddDecor(_street, catalog.Decor[8], jarAt.x, jarAt.y - 20f, 90f * DecorScale, Color.white);
                    stump.name = "JarStump";
                    standing.Add(new KeyValuePair<float, Transform>(jarAt.y - 20f, stump.transform));
                }
                _jarProp = UiKit.Rect("Jar", _street).Place(0f, 0f, jarAt.x, jarAt.y + 80f, 150, 150);
                var jarGlass = UiKit.Rect("Glass", _jarProp).Place(0.5f, 0.5f, 0, -14, 96, 110);
                UiKit.Rect("Glow", jarGlass).Place(0.5f, 0.5f, 0, 0, 170, 170).AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
                Image jarImage = jarGlass.AddImage(catalog.Panel, Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Night, 0.2f), 0.5f), true);
                jarImage.raycastTarget = true;
                for (int k = 0; k < 3; k++)
                    UiKit.Rect("Dot", jarGlass).Place(0.5f, 0.5f, -22f + 22f * k, -24f + 18f * (k % 2), 10, 10).AddImage(catalog.Round, Palette.Mix(Palette.Lantern, Color.white, 0.5f));
                UiKit.Rect("Cork", _jarProp).Place(0.5f, 0.5f, 0, 48, 56, 24).AddImage(catalog.Panel, Palette.Mix(Palette.Carrot, Palette.Ink, 0.45f), true);
                var jarButton = _jarProp.gameObject.AddComponent<Button>();
                jarButton.transition = Selectable.Transition.None;
                jarButton.targetGraphic = jarImage;
                jarButton.onClick.AddListener(OpenFireflies);
                var jarPlate = UiKit.Rect("Plate", _jarProp).Place(0.5f, 0f, 0, -26, 270, 44);
                jarPlate.AddImage(catalog.Panel, Palette.Paper, true);
                _jarPlate = UiKit.Rect("Text", jarPlate).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                _jarPlate.resizeTextForBestFit = true;
                _jarPlate.resizeTextMinSize = 14;
                _jarPlate.resizeTextMaxSize = 20;
                _jarProp.gameObject.SetActive(false);
                standing.Add(new KeyValuePair<float, Transform>(jarAt.y, _jarProp));
            }
            BuildDecorSpots(catalog, standing);
            standing.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Transform> item in standing) item.Value.SetAsLastSibling();

            BuildFireflies(catalog);
            CenterOn(StationSpot(0));
            // Behind the title the garden shows its moon pond; starting the game glides down from there.
            _street.anchoredPosition = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.5f) - PondSpot * _zoom, _zoom);

            var hint = UiKit.Rect("Hint", _canvas).Place(0.5f, 1f, 0, -196, 940, 60);
            hint.AddShadow(14f, 8f, 0.28f);
            UiKit.Rect("Body", hint).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1).AddText(L10n.T("hint"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            // A sideways swipe on the card walks to the neighbouring station, and the garden follows.
            face.gameObject.AddComponent<CardSwipe>().Stepped = StepStation;

            _cardName = UiKit.Rect("Name", _card).Stretch(0, 1, 1, 1, 32, -70, 200, 14)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardName.horizontalOverflow = HorizontalWrapMode.Overflow;
            var chip = UiKit.Rect("Level", _card).Place(1f, 1f, -104, -42, 160, 46);
            _cardLevelChip = chip.AddImage(catalog.Panel, Palette.Moss, true);
            _cardLevel = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _cardInfo = UiKit.Rect("Info", _card).Stretch(0, 1, 1, 1, 32, -138, 32, 74)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperLeft);
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
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _cardWorkerName.horizontalOverflow = HorizontalWrapMode.Overflow;
            _cardWorkerSub = UiKit.Rect("Sub", _cardWorker).Stretch(0, 0, 1, 0.5f, 104, 10, 12, 4)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.UpperLeft);
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
                .AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardBuyCost = UiKit.Rect("Cost", _cardBuy.FaceRect).Stretch(0.4f, 0, 1, 1, 0, 0, 64, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleRight, FontStyle.Bold);
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

        /// <summary>
        /// Moves the card to the next (+1) or previous (-1) station that stands in the garden, and brings the
        /// garden along. At either end it stays where it is.
        /// </summary>
        public void StepStation(int direction)
        {
            if (Selected < 0 || direction == 0) return;
            int target = Selected + (direction > 0 ? 1 : -1);
            if (target < 0 || target >= _actors.Count || !_actors[target].Root.gameObject.activeSelf) return;
            Sfx.Click();
            FocusStation(target);
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
            _cardLevel.text = needsPlot ? L10n.T("station.empty") : (level <= 0 ? L10n.T("row.locked") : L10n.StationLevel(level));
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
                    _cardWorkerArt.color = RabbitGridView.Fur(worker);
                    _cardWorkerFrame.color = Palette.Tiers[worker.Tier];
                    _cardWorkerName.text = worker.Name;
                    _cardWorkerSub.text = L10n.T("card.worker", L10n.Kind(worker), L10n.Trade(Rabbits.TradeOf(worker)))
                        + (inTrade ? L10n.T("rabbits.intrade", UiKit.Times(Rabbits.TradeBonus)) : "");
                    _cardWorkerSub.color = inTrade ? Palette.Mix(Palette.Moss, Palette.Ink, 0.45f) : Palette.InkSoft;
                }
            }

            BigNum cost;
            bool can, capped = false;
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
                capped = count <= 0; // level 500: nothing left to buy
                can = !capped && s.Coins >= cost;
                int next = Economy.NextMilestone(level);
                _cardInfo.text = StationRowView.Describe(defs, s, i, level, m) + (next > 0 ? L10n.T("row.milestone", next) : "");
                _cardBuyTop.text = capped ? L10n.T("row.maxlevel") : L10n.T("row.upgrade", count);
            }
            // Short of carrots and nothing else: the plate says how many are missing.
            bool shortOnly = !capped && !can && s.Coins < cost && (needsPlot ? Expansion.Next(defs, s) != null && Expansion.HasLevel(Expansion.Next(defs, s), s) && Expansion.HasRabbits(Expansion.Next(defs, s), s) : (level > 0 || Economy.CanUnlock(s, i)));
            if (shortOnly) _cardBuyTop.text = L10n.T("need.more");
            _cardBuyCost.text = capped ? "" : (shortOnly ? (cost - s.Coins).Short() : cost.Short());
            Color ink = ChunkyButton.InkFor(can);
            _cardBuyTop.color = ink;
            _cardBuyCost.color = ink;
            _cardBuy.SetEnabled(can);
        }

        void BuildPhotoMode(GameCatalog catalog)
        {
            // A small plate at the foot of the screen while framing: snap, or done.
            var bar = UiKit.Rect("PhotoBar", _canvas).Place(0.5f, 0f, 0, 150, 860, 130);
            bar.AddShadow(18f, 10f, 0.3f);
            UiKit.Rect("Lip", bar).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Plate", bar).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Hint", bar).Stretch(0, 1, 1, 1, 0, -6, 0, -44).AddText(L10n.T("photo.hint"), UiKit.TextSmall, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow();
            // What goes into the picture: three pills above the bar, remembered between sessions.
            var options = UiKit.Rect("Options", bar).Place(0.5f, 1f, 0, 64, 620, 64);
            options.AddImage(catalog.Panel, Palette.Track, true).raycastTarget = true;
            AddPhotoToggle(options, catalog, "Frame", L10n.T("photo.frame"), 104, () => PhotoFrameOn, v => PhotoFrameOn = v);
            AddPhotoToggle(options, catalog, "Names", L10n.T("photo.names"), 310, () => PhotoNamesOn, v => PhotoNamesOn = v);
            AddPhotoToggle(options, catalog, "Fireflies", L10n.T("photo.fireflies"), 516, () => PhotoFirefliesOn, v => PhotoFirefliesOn = v);
            var album = UiKit.Rect("Album", bar).Place(0f, 0.5f, 110, 4, 170, UiKit.MinTouch);
            UiKit.Secondary(album, catalog.Panel, L10n.T("photo.album"), () => { Sfx.Click(); OpenAlbum(); });
            var done = UiKit.Rect("Done", bar).Place(0f, 0.5f, 300, 4, 170, UiKit.MinTouch);
            UiKit.Secondary(done, catalog.Panel, L10n.T("photo.done"), () => { Sfx.Click(); ExitPhotoMode(); });
            var snap = UiKit.Rect("Snap", bar).Place(1f, 0.5f, -170, 4, 280, 96);
            ChunkyButton snapButton = UiKit.Chunky(snap, catalog.Panel, Snap);
            UiKit.Rect("Text", snapButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("photo.snap"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _photoBar = bar.gameObject;
            _photoBar.SetActive(false);

            // The paper frame round the picture: a cream border with an ink line, the garden's name, the date
            // and the game's name in a corner. It lives in the canvas, so a snap takes it with the garden.
            var photoFrame = UiKit.Rect("PhotoFrame", _canvas).Stretch(0, 0, 1, 1);
            const float Edge = 44f, Foot = 150f;
            Color paper = Palette.Paper, ink = Palette.Ink;
            UiKit.Rect("Top", photoFrame).Stretch(0, 1, 1, 1, 0, -Edge, 0, 0).AddImage(null, paper);
            UiKit.Rect("Left", photoFrame).Stretch(0, 0, 0, 1, 0, 0, -Edge, 0).AddImage(null, paper);
            UiKit.Rect("Right", photoFrame).Stretch(1, 0, 1, 1, -Edge, 0, 0, 0).AddImage(null, paper);
            UiKit.Rect("Bottom", photoFrame).Stretch(0, 0, 1, 0, 0, 0, 0, -Foot).AddImage(null, paper);
            UiKit.Rect("Line", photoFrame).Stretch(0, 0, 1, 1, Edge - 4, Foot - 4, Edge - 4, Edge - 4).AddImage(catalog.Panel, Palette.Alpha(ink, 0.5f), true).pixelsPerUnitMultiplier = 3f;
            UiKit.Rect("Inner", photoFrame).Stretch(0, 0, 1, 1, Edge, Foot, Edge, Edge).AddImage(catalog.Panel, new Color(0, 0, 0, 0), true);
            _frameName = UiKit.Rect("GardenName", photoFrame).Stretch(0, 0, 0.6f, 0, Edge + 16, 70, 0, -(Foot - 10)).AddText("", 40, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _frameName.horizontalOverflow = HorizontalWrapMode.Overflow;
            _frameDate = UiKit.Rect("Date", photoFrame).Stretch(0.5f, 0, 1, 0, 0, 70, Edge + 16, -(Foot - 10)).AddText("", UiKit.TextTitle, Palette.InkSoft, TextAnchor.MiddleRight);
            UiKit.Rect("Logo", photoFrame).Stretch(0, 0, 1, 0, Edge + 16, 24, Edge + 16, -66).AddText(L10n.T("photo.logo"), UiKit.TextBody, Palette.CarrotLip, TextAnchor.MiddleLeft, FontStyle.Bold);
            photoFrame.SetSiblingIndex(bar.GetSiblingIndex()); // under the snap bar, over the garden
            _photoFrame = photoFrame.gameObject;
            _photoFrame.SetActive(false);

            // The album: the last pictures, three to a row; a touch opens one.
            var albumOverlay = UiKit.Rect("PhotoAlbum", _canvas).Stretch(0, 0, 1, 1);
            albumOverlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var albumScrim = albumOverlay.gameObject.AddComponent<Button>();
            albumScrim.transition = Selectable.Transition.None;
            albumScrim.onClick.AddListener(CloseAlbum);
            var albumCard = UiKit.Rect("Card", albumOverlay).Place(0.5f, 0.5f, 0, 40, 960, 1420);
            UiKit.Rect("Lip", albumCard).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", albumCard).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            _albumTitle = UiKit.Rect("Title", albumCard).Stretch(0, 1, 1, 1, 40, -110, 130, 30).AddText("", UiKit.TextTitle, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var albumClose = UiKit.Rect("Close", albumCard).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image albumCloseImage = albumClose.AddImage(catalog.Round, Palette.Track);
            albumCloseImage.raycastTarget = true;
            var albumCloseButton = albumClose.gameObject.AddComponent<Button>();
            albumCloseButton.transition = Selectable.Transition.None;
            albumCloseButton.onClick.AddListener(CloseAlbum);
            UiKit.Rect("Text", albumClose).Stretch(0, 0, 1, 1).AddText("×", 48, ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _albumGrid = UiKit.Rect("Grid", albumCard).Stretch(0, 0, 1, 1, 40, 40, 40, 130);
            _photoAlbum = albumOverlay.gameObject;
            _photoAlbum.SetActive(false);

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
            var save = UiKit.Rect("Save", card).Place(0f, 0f, 190, 150, 280, UiKit.MinTouch);
            UiKit.Secondary(save, catalog.Panel, L10n.T("photo.save"), () => { Sfx.Click(); RevealPhoto(); });
            var share = UiKit.Rect("Share", card).Place(0.5f, 0f, 0, 150, 280, UiKit.MinTouch);
            UiKit.Secondary(share, catalog.Panel, L10n.T("photo.share"), () => { Sfx.Click(); CopyPhoto(); });
            var close = UiKit.Rect("Close", card).Place(1f, 0f, -190, 150, 280, 96);
            ChunkyButton closeButton = UiKit.Chunky(close, catalog.Panel, () => { Sfx.Click(); _photoReview.SetActive(false); _photoBar.SetActive(true); });
            UiKit.Rect("Text", closeButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("photo.close"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _photoReview = overlay.gameObject;
            _photoReview.SetActive(false);
        }

        void AddPhotoToggle(RectTransform track, GameCatalog catalog, string name, string label, float x, System.Func<bool> get, System.Action<bool> set)
        {
            var rt = UiKit.Rect(name, track).Place(0f, 0.5f, x, 0, 196, 52);
            UiKit.Touchable(rt, 196, 52);
            Image img = rt.AddImage(catalog.Panel, Palette.Carrot, true);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = img;
            button.onClick.AddListener(() => { Sfx.Click(); set(!get()); ApplyPhotoOptions(); });
            _photoToggles[name] = img;
            _photoToggleLabels[name] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(label, UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Shows the frame, the name plates and the fireflies as the options say, in or out of photo mode.</summary>
        void ApplyPhotoOptions()
        {
            bool frame = PhotoMode && PhotoFrameOn;
            if (_photoFrame.activeSelf != frame) _photoFrame.SetActive(frame);
            if (frame)
            {
                _frameName.text = string.IsNullOrEmpty(Runner.State.Profile.GardenName) ? L10n.T("profile.unnamed") : Runner.State.Profile.GardenName;
                _frameDate.text = System.DateTime.Now.ToString("dd/MM/yyyy") + (Runner.State.Season > 0 ? " · " + L10n.T("hud.season", Runner.State.Season) : "");
            }
            bool fireflies = !PhotoMode || PhotoFirefliesOn;
            if (_fireflyLayer != null && _fireflyLayer.activeSelf != fireflies) _fireflyLayer.SetActive(fireflies);
            foreach (KeyValuePair<string, Image> pair in _photoToggles)
            {
                bool on = pair.Key == "Frame" ? PhotoFrameOn : (pair.Key == "Names" ? PhotoNamesOn : PhotoFirefliesOn);
                pair.Value.color = on ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _photoToggleLabels[pair.Key].color = on ? Palette.OnCarrot : Palette.InkSoft;
            }
            RefreshStreet(Runner.Defs, Runner.State); // the name plates follow the option
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
            ApplyPhotoOptions();
        }

        /// <summary>The last pictures, newest first. Files that are gone are dropped from the album first.</summary>
        public void OpenAlbum()
        {
            GameState s = Runner.State;
            if (VuonTra.Core.Photos.Tidy(s, System.IO.File.Exists) > 0) Runner.Save();
            var old = new List<GameObject>();
            foreach (Transform child in _albumGrid) old.Add(child.gameObject);
            foreach (GameObject go in old) { if (Application.isPlaying) Destroy(go); else DestroyImmediate(go); }
            foreach (Texture2D tex in _albumTextures) if (tex != null) { if (Application.isPlaying) Destroy(tex); else DestroyImmediate(tex); }
            _albumTextures.Clear();
            _albumTitle.text = L10n.T("photo.album.title", s.Album.Count);
            if (s.Album.Count == 0)
            {
                UiKit.Rect("Empty", _albumGrid).Stretch(0, 1, 1, 1, 0, -120, 0, 0).AddText(L10n.T("photo.album.empty"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            }
            const float Cell = 284f, Gap = 14f;
            for (int i = 0; i < s.Album.Count; i++)
            {
                string path = s.Album[s.Album.Count - 1 - i]; // newest first
                int column = i % 3, row = i / 3;
                var cell = UiKit.Rect("Photo " + i, _albumGrid).Place(0f, 1f, Cell / 2f + column * (Cell + Gap), -(Cell / 2f + row * (Cell + Gap)), Cell, Cell);
                Image plate = cell.AddImage(Runner.Catalog.Panel, Palette.Card, true);
                plate.raycastTarget = true;
                var button = cell.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = plate;
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                try { tex.LoadImage(System.IO.File.ReadAllBytes(path)); } catch (System.Exception e) { Debug.LogWarning("Không đọc được ảnh " + path + ": " + e.Message); }
                _albumTextures.Add(tex);
                var picture = UiKit.Rect("Picture", cell).Stretch(0, 0, 1, 1, 10, 10, 10, 10).gameObject.AddComponent<RawImage>();
                picture.texture = tex;
                picture.raycastTarget = false;
                // Keep the picture's own proportions inside the square.
                var fitter = picture.gameObject.AddComponent<AspectRatioFitter>();
                fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
                fitter.aspectRatio = tex.height > 0 ? tex.width / (float)tex.height : 1f;
                string chosen = path;
                Texture2D chosenTex = tex;
                button.onClick.AddListener(() => { Sfx.Click(); ShowPhoto(chosen, chosenTex); });
            }
            _photoAlbum.SetActive(true);
        }

        public void CloseAlbum()
        {
            _photoAlbum.SetActive(false);
        }

        /// <summary>Opens one picture from the album in the review card, with its file and no gift line.</summary>
        void ShowPhoto(string path, Texture2D tex)
        {
            LastPhotoPath = path;
            _photoPreview.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            _photoGift.text = "";
            _photoNote.text = path;
            CloseAlbum();
            _photoBar.SetActive(false);
            _photoReview.SetActive(true);
        }

        public void ExitPhotoMode()
        {
            if (!PhotoMode) return;
            PhotoMode = false;
            _photoReview.SetActive(false);
            _photoAlbum.SetActive(false);
            _photoBar.SetActive(false);
            ApplyPhotoOptions();
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
            bool gift = Runner.TakePhoto(path);
            _photoPreview.sprite = Sprite.Create(_photoTexture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f);
            _photoGift.text = gift ? L10n.T("photo.gift", Photos.GiftGolden) : L10n.T("photo.again");
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

        /// <summary>
        /// Keeps the guidance on the right step and the hand on the right thing: the teapot, then its card's
        /// button, then the tab that leads to the next thing and the button inside it, then the empty pad.
        /// </summary>
        void UpdateGuide(float dt)
        {
            GameState s = Runner.State;
            bool shown = Shell.State == ShellState.Playing && !PhotoMode && !Tutorial.IsDone(s);
            if (!shown)
            {
                // Done by doing the last step (not by skipping): a word of congratulation.
                if (_guideStep == Tutorial.PlaceRabbit && Tutorial.IsDone(s) && Shell.State == ShellState.Playing)
                {
                    Feel.Moment(L10n.T("tut.done.title"), L10n.T("tut.done.sub"));
                    Sfx.Sting();
                }
                _guideStep = -1;
                Guide.Hide();
                return;
            }
            int step = s.TutorialStep;
            if (step != _guideStep)
            {
                _guideStep = step;
                Guide.Show(step);
                if (step == Tutorial.CallRabbit && Runner.GrantTutorialClover())
                    Feel.FloatText(new Vector2(0, 200), L10n.T("tut.clover"), Palette.Lantern, 40);
            }
            Guide.Follow(GuideTarget(step), dt);
        }

        RectTransform GuideTarget(int step)
        {
            switch (step)
            {
                case Tutorial.TapRabbit:
                    return _actors.Count > 0 ? _actors[0].Root : null;
                case Tutorial.RaiseTeapot:
                    return Selected == 0 ? _cardBuy.Root : (_actors.Count > 0 ? _actors[0].Root : null);
                case Tutorial.OpenSecond:
                    return SheetOpen && Tab == 0 && _rows.Count > 1 ? _rows[1].Buy.Root : _tabIcons[0].rectTransform;
                case Tutorial.CollectQuest:
                    if (!SheetOpen || Tab != 2) return _tabIcons[2].rectTransform;
                    foreach (QuestRowView row in _questRows) if (row.CanClaim) return row.Claim.Root;
                    return _questReady;
                case Tutorial.CallRabbit:
                    return SheetOpen && Tab == 3 ? _rabbitList.Summon.Root : _tabIcons[3].rectTransform;
                case Tutorial.PlaceRabbit:
                    for (int i = 0; i < _actors.Count; i++)
                        if (Runner.State.Levels[i] > 0 && !_actors[i].HasGuest && _actors[i].Root.gameObject.activeSelf) return _actors[i].Pad;
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>Moonlight and darkened corners over the garden, under the HUD.</summary>
        /// <summary>The hour of the day the garden is lit for. Tests and previews set their own.</summary>
        public System.Func<double> ClockHours = () => System.DateTime.Now.TimeOfDay.TotalHours;
        Image _daylight, _moonGlow;
        /// <summary>The light round the edges of the screen while a speed burst runs, and how far it has come up.</summary>
        GameObject _boostEdge;
        readonly List<Image> _boostEdges = new List<Image>();
        float _boostGlow;
        static readonly Color MoonGlowColor = new Color(0.80f, 0.90f, 1f, 0.26f);

        void BuildAtmosphere(GameCatalog catalog)
        {
            // The time of day lies over the garden as a wash of colour: amber at dusk, rose and blue at dawn,
            // a pale warmth by day, and nothing at all at night, which is how the garden is meant to be seen.
            _daylight = UiKit.Rect("Daylight", _canvas).Stretch(0, 0, 1, 1).AddImage(null, new Color(0, 0, 0, 0));
            // While a speed burst runs the whole screen is rimmed with the tea bar's green, so the garden
            // itself says the burst is on. Four fading bands, one per edge, that pulse and then go.
            var edge = UiKit.Rect("BoostEdge", _canvas).Stretch(0, 0, 1, 1);
            _boostEdges.Clear();
            const float Band = 240f;
            _boostEdges.Add(UiKit.Rect("Bottom", edge).Stretch(0, 0, 1, 0, 0, 0, 0, -Band).AddImage(UiKit.FadeUp, Palette.Moss));
            var top = UiKit.Rect("Top", edge).Stretch(0, 1, 1, 1, 0, -Band, 0, 0);
            top.localRotation = Quaternion.Euler(0, 0, 180f); // the band is its own mirror, so it stays where it is
            _boostEdges.Add(top.AddImage(UiKit.FadeUp, Palette.Moss));
            var left = UiKit.Rect("Left", edge).Place(0f, 0.5f, Band / 2f, 0, ScreenH, Band);
            left.localRotation = Quaternion.Euler(0, 0, -90f);
            _boostEdges.Add(left.AddImage(UiKit.FadeUp, Palette.Moss));
            var right = UiKit.Rect("Right", edge).Place(1f, 0.5f, -Band / 2f, 0, ScreenH, Band);
            right.localRotation = Quaternion.Euler(0, 0, 90f);
            _boostEdges.Add(right.AddImage(UiKit.FadeUp, Palette.Moss));
            _boostEdge = edge.gameObject;
            _boostEdge.SetActive(false);
            _boostGlow = 0f;
            // The moon itself is above the canopy; its glow spills in at the top corner.
            _moonGlow = UiKit.Rect("MoonGlow", _canvas).Place(1f, 1f, -140, -200, 1300, 1300)
                .AddImage(catalog.Glow, MoonGlowColor);
            if (catalog.BgVignette != null)
            {
                var v = UiKit.Rect("Vignette", _canvas).Stretch(0, 0, 1, 1);
                var raw = v.gameObject.AddComponent<RawImage>();
                raw.texture = catalog.BgVignette;
                raw.raycastTarget = false;
            }
            // Last over the garden, under the HUD: the painting's paper shows through.
            PaperGrain.Add(_canvas, ScreenW, ScreenH);
        }

        /// <summary>The rim of light comes up in 0,4 s when a burst starts, breathes while it runs, and goes the same way.</summary>
        void UpdateBoostEdge(bool boosting, float dt)
        {
            if (_boostEdge == null) return;
            _boostGlow = Mathf.MoveTowards(_boostGlow, boosting && Shell.State == ShellState.Playing ? 1f : 0f, dt / 0.4f);
            bool shown = _boostGlow > 0f;
            if (_boostEdge.activeSelf != shown) _boostEdge.SetActive(shown);
            if (!shown) return;
            float breathe = Motion.Reduced ? 1f : 0.82f + 0.18f * Mathf.Sin(_clock * 4f);
            Color c = Palette.Alpha(Palette.Moss, 0.55f * _boostGlow * breathe);
            foreach (Image band in _boostEdges) band.color = c;
        }

        /// <summary>Lights the garden for the hour: the wash, the moon's glow and how many fireflies are out.</summary>
        void UpdateDaylight()
        {
            double hour = ClockHours();
            float night = (float)Daylight.NightAt(hour);
            float lift = (1f - night) / (1f - (float)Daylight.DayFloor); // 0 at night, 1 at midday
            Color wash;
            switch (Daylight.PhaseAt(hour))
            {
                case DayPhase.Dusk: wash = Palette.Alpha(Palette.Carrot, 0.22f * lift); break;
                case DayPhase.Dawn: wash = Palette.Alpha(Palette.Mix(Palette.Blush, new Color(0.62f, 0.77f, 1f), 0.5f), 0.22f * lift); break;
                case DayPhase.Day: wash = Palette.Alpha(Palette.Mix(Palette.Lantern, Color.white, 0.55f), 0.2f); break;
                default: wash = new Color(0, 0, 0, 0); break;
            }
            if (_daylight != null) _daylight.color = wash;
            if (_moonGlow != null) _moonGlow.color = Palette.Alpha(MoonGlowColor, MoonGlowColor.a * Mathf.Lerp(0.3f, 1f, night));
            int awake = Daylight.FirefliesAt(hour, _fireflies.Count);
            for (int i = 0; i < _fireflies.Count; i++)
                if (_fireflies[i].gameObject.activeSelf != (i < awake)) _fireflies[i].gameObject.SetActive(i < awake);
        }

        /// <summary>A few warm fireflies that live in the garden itself, each circling a lantern or a station.</summary>
        void BuildFireflies(GameCatalog catalog)
        {
            _fireflies.Clear();
            _fireflyImages.Clear();
            if (_glowSpots.Count == 0) return;
            var layer = UiKit.Rect("Fireflies", _street).Stretch(0, 0, 0, 0);
            _fireflyLayer = layer.gameObject;
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
            // The fireflies fly in a few flocks; each flock drifts from one light to the next and back on its own clock.
            int flocks = Mathf.Max(1, Mathf.Min(6, _glowSpots.Count));
            for (int i = 0; i < _fireflies.Count; i++)
            {
                float seed = i * 7.31f;
                int flock = i % flocks;
                Vector2 home = _glowSpots[flock % _glowSpots.Count];
                if (!Motion.Reduced && _glowSpots.Count > 1)
                {
                    Vector2 next = _glowSpots[(flock + 1) % _glowSpots.Count];
                    float t = Mathf.SmoothStep(0f, 1f, Mathf.PingPong(_clock / 26f + flock * 0.37f, 1f));
                    home = Vector2.Lerp(home, next, t) + new Vector2(Mathf.Sin(_clock * 0.3f + flock) * 30f, Mathf.Cos(_clock * 0.23f + flock) * 20f);
                }
                float angle = _clock * (0.35f + Hash(seed) * 0.4f) + seed, reach = 30f + Hash(seed + 2f) * 50f;
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

        /// <summary>True when a point lies on one of the player's decoration spots (#11): the garden's own dressing keeps off them.</summary>
        static bool OnDecorSpot(float x, float y)
        {
            foreach (Vector2 spot in DecorSpots) if (Vector2.Distance(spot, new Vector2(x, y)) < 150f) return true;
            return false;
        }

        /// <summary>
        /// The eight decoration spots (#11): each an object at its feet that shows a faint "+" plate while something waits in
        /// storage, or the piece the player stood there. A touch opens the spot's card. Hidden altogether while there is nothing
        /// to show, so a newcomer's garden has no clutter.
        /// </summary>
        void BuildDecorSpots(GameCatalog catalog, List<KeyValuePair<float, Transform>> standing)
        {
            for (int i = 0; i < Decor.Spots; i++)
            {
                int spot = i;
                Vector2 at = DecorSpotAt(i);
                var root = UiKit.Rect("Spot " + i, _street).Place(0f, 0f, at.x, at.y, 140, 140);
                root.pivot = new Vector2(0.5f, 0f);
                root.anchoredPosition = at;
                Image hit = root.AddImage(null, new Color(0, 0, 0, 0)); // the whole square answers to a touch, "+" or piece
                hit.raycastTarget = true;
                var button = root.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = hit;
                button.onClick.AddListener(() => OpenDecorSpot(spot));
                // The "+": a faint paper oval on the grass with a soft cross, the size of a small prop's foot.
                var plate = UiKit.Rect("Plate", root).Place(0.5f, 0f, 0, 14, 96, 44);
                plate.AddImage(catalog.Glow, Palette.Alpha(Palette.Paper, 0.55f));
                UiKit.Rect("Oval", plate).Stretch(0, 0, 1, 1, 14, 6, 14, 6).AddImage(catalog.Round, Palette.Alpha(Palette.Paper, 0.5f));
                UiKit.Rect("Text", plate).Place(0.5f, 0.5f, 0, 1, 60, 44).AddText("+", 32, Palette.Alpha(Palette.Ink, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
                // The piece itself stands on its feet; its picture and size follow what is placed.
                var item = UiKit.Rect("Item", root).Place(0.5f, 0f, 0, 0, 100, 100);
                item.pivot = new Vector2(0.5f, 0f);
                item.anchoredPosition = Vector2.zero;
                Image art = item.AddImage(null, Color.white);
                art.preserveAspect = true;
                art.raycastTarget = false;
                var shade = UiKit.Rect("Shade", root).Place(0.5f, 0f, 6, 4, 90, 30);
                shade.AddImage(catalog.Glow, Palette.Alpha(Palette.Night, 0.28f));
                shade.SetAsFirstSibling();
                plate.gameObject.SetActive(false);
                item.gameObject.SetActive(false);
                root.gameObject.SetActive(false);
                _decorSpots.Add(new DecorSpotView { Root = root, Plate = plate.gameObject, Item = art });
                standing.Add(new KeyValuePair<float, Transform>(at.y, root));
            }
        }

        /// <summary>What each spot shows now: nothing, a "+", or the piece standing there; and the colour of every paper lantern.</summary>
        void RefreshDecor(GameState s)
        {
            bool empties = Decor.ShowEmptySpots(s);
            for (int i = 0; i < _decorSpots.Count && i < s.DecorSpots.Length; i++)
            {
                DecorSpotView view = _decorSpots[i];
                string kind = s.DecorSpots[i];
                bool shown = kind != null || empties;
                if (view.Root.gameObject.activeSelf != shown) view.Root.gameObject.SetActive(shown);
                bool plate = kind == null && empties;
                if (view.Plate.activeSelf != plate) view.Plate.SetActive(plate);
                bool item = kind != null;
                if (view.Item.gameObject.activeSelf != item) view.Item.gameObject.SetActive(item);
                if (view.Shown != kind)
                {
                    view.Shown = kind;
                    if (kind != null)
                    {
                        Sprite sprite = Runner.Catalog.DecorArt(kind);
                        int k = Decor.IndexOf(kind);
                        float height = k >= 0 && k < DecorHeights.Length ? DecorHeights[k] : 100f;
                        float width = sprite != null ? height * sprite.rect.width / sprite.rect.height : height;
                        view.Item.sprite = sprite != null ? sprite : Runner.Catalog.Round;
                        view.Item.rectTransform.sizeDelta = new Vector2(width, height);
                    }
                }
            }
            Color tint = LanternTint(s.LanternColor);
            foreach (Image lantern in _lanternArt) if (lantern != null && lantern.color != tint) lantern.color = tint;
            _lanternGlowColor = s.LanternColor > 0 ? Palette.Mix(Palette.Lantern, Palette.Hex(Decor.LanternColors[s.LanternColor]), 0.5f) : Palette.Lantern;
            if (DecorCard != null) DecorCard.Refresh(s);
        }

        /// <summary>A decoration spot was touched: its card opens, listing what waits in storage and what stands there.</summary>
        public void OpenDecorSpot(int spot)
        {
            if (Shell.State != ShellState.Playing || PhotoMode || Pound.IsOpen || Basket.IsOpen || Jar.IsOpen) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            DecorCard.Open(Runner.State, spot);
        }

        /// <summary>A piece stood on a spot from its card, in place of whatever was there: a small burst where it lands.</summary>
        void OnPlaceDecor(int spot, string kindId)
        {
            if (!Runner.PlaceDecor(spot, kindId)) return;
            DecorCard.Close();
            Sfx.Blip();
            Haptics.Light();
            DecorMoment(spot);
        }

        /// <summary>A spot's piece taken back into storage from its card.</summary>
        void OnStoreDecor(int spot)
        {
            if (!Runner.StoreDecor(spot)) return;
            DecorCard.Close();
            Sfx.Blip();
            Haptics.Light();
            DecorMoment(spot);
        }

        /// <summary>A little paper and lantern-light burst at a spot, and the piece pops as it lands; still under reduced motion.</summary>
        void DecorMoment(int spot)
        {
            if (spot < 0 || spot >= _decorSpots.Count) return;
            RectTransform root = _decorSpots[spot].Root;
            if (OnScreen(root)) Feel.Burst(Feel.PointOf(root, new Vector2(0, 50)), Runner.Catalog.Round, new[] { Palette.Lantern, Palette.Paper, Palette.Blush }, 12, 420f, 20f);
            if (Motion.Reduced) return;
            RectTransform item = _decorSpots[spot].Item.rectTransform;
            Tweens.Add(Motion.Tap * 1.6f, t => { if (item != null) item.localScale = Vector3.one * (1f + 0.18f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (item != null) item.localScale = Vector3.one; });
        }

        /// <summary>A decoration bought at the shop (#11): it waits in storage; a word says where to put it.</summary>
        void OnBuyDecor(string kindId)
        {
            if (!Runner.BuyDecor(kindId)) return;
            Sfx.Reward();
            Haptics.Light();
            Feel.FloatText(new Vector2(0, 420), L10n.T("float.decor", L10n.T("decor." + kindId)), Palette.Lantern, 36);
        }

        /// <summary>A lantern colour hung from the shop: every paper lantern in the garden takes it at once.</summary>
        void OnLanternColor(int color)
        {
            if (!Runner.SetLanternColor(color)) return;
            Sfx.Blip();
            Haptics.Light();
            Feel.FloatText(new Vector2(0, 420), L10n.T("float.lantern", L10n.T("lantern." + Decor.LanternColorIds[color])), Palette.Hex(Decor.LanternColors[color]), 40);
        }

        /// <summary>
        /// Dresses the clearing: a canopy that frames it on every side, lanterns along the path,
        /// a pond that holds the moon, places to sit, and a scatter of smaller things. Placement
        /// is fixed (not random each run) so the garden always looks the same.
        /// </summary>
        /// <summary>
        /// The ground has layers before anything stands on it: a worn band of earth under the stepping stones of every
        /// path, flagstones around the teapot, carpets of small flowers where there is open grass, and the moon's
        /// light coming down on the teapot. All flat, all drawn first.
        /// </summary>
        void BuildGroundLayers(GameCatalog catalog, int count)
        {
            Color earth = Palette.Alpha(Palette.Mix(Palette.Mix(Palette.Grass, Palette.Carrot, 0.35f), Palette.Ink, 0.25f), 0.32f);
            foreach (Vector2Int path in Paths)
            {
                if (path.x >= count || path.y >= count) continue;
                Vector2 a = StationSpot(path.x) + new Vector2(0, -150f * StationSize(path.x)), b = StationSpot(path.y) + new Vector2(0, -150f * StationSize(path.y));
                int steps = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(a, b) / 44f));
                for (int k = 0; k <= steps; k++)
                {
                    Vector2 at = Vector2.Lerp(a, b, k / (float)steps) + new Vector2(Mathf.Sin(k * 2.3f) * 9f, Mathf.Sin(k * 1.7f) * 7f);
                    UiKit.Rect("Path", _street).Place(0f, 0f, at.x, at.y, 118, 64).AddImage(catalog.Round, earth);
                }
            }

            // Flagstones in a ring around the teapot, a few more inside it.
            Color stone = Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Ink, 0.38f), 0.5f);
            Vector2 ring = Heart + new Vector2(0, -70f);
            for (int k = 0; k < 14; k++)
            {
                float angle = k / 14f * Mathf.PI * 2f + 0.2f;
                Vector2 at = ring + new Vector2(Mathf.Cos(angle) * 225f, Mathf.Sin(angle) * 150f);
                float w = 62f + Hash(k + 11f) * 22f;
                UiKit.Rect("Flagstone", _street).Place(0f, 0f, at.x, at.y, w, w * 0.62f).AddImage(catalog.Round, stone);
            }
            for (int k = 0; k < 6; k++)
            {
                float angle = k / 6f * Mathf.PI * 2f + 0.7f;
                Vector2 at = ring + new Vector2(Mathf.Cos(angle) * 135f, Mathf.Sin(angle) * 92f);
                UiKit.Rect("Flagstone", _street).Place(0f, 0f, at.x, at.y, 56f, 34f).AddImage(catalog.Round, stone);
            }

            // Carpets of small flowers where the grass is most open: the spots furthest from every station and seat,
            // at most six, well apart. The same garden always gets the same carpets.
            var beds = new List<Vector2>();
            {
                var open = new List<KeyValuePair<float, Vector2>>();
                for (float x = 100f; x < WorldW - 90f; x += 60f)
                    for (float y = 180f; y < WorldH - 220f; y += 60f)
                    {
                        if (!OpenGrass(x, y, count)) continue;
                        float d = float.MaxValue;
                        for (int i = 0; i < count; i++) d = Mathf.Min(d, Vector2.Distance(new Vector2(x, y), StationSpot(i)));
                        foreach (Vector2 seat in Seats) d = Mathf.Min(d, Vector2.Distance(new Vector2(x, y), Heart + seat));
                        open.Add(new KeyValuePair<float, Vector2>(d, new Vector2(x, y)));
                    }
                open.Sort((a, b) => b.Key.CompareTo(a.Key));
                foreach (KeyValuePair<float, Vector2> spot in open)
                {
                    bool apart = true;
                    foreach (Vector2 bed in beds) if (Vector2.Distance(bed, spot.Value) < 260f) apart = false;
                    if (apart) beds.Add(spot.Value);
                    if (beds.Count == 6) break;
                }
            }
            Color[] petals = { Palette.Blush, Palette.Lantern, Palette.Paper, Palette.Mix(Palette.Blush, Palette.Paper, 0.5f) };
            foreach (Vector2 at in beds)
            {
                var carpet = UiKit.Rect("Flowers", _street).Place(0f, 0f, at.x, at.y, 0, 0);
                for (int k = 0; k < 14; k++)
                {
                    float seed = k * 3.7f + at.x * 0.01f;
                    Vector2 d = new Vector2(Hash(seed + at.x) - 0.5f, Hash(seed + at.y + 1f) - 0.5f) * new Vector2(190f, 110f);
                    float size = 12f + Hash(seed + 2f) * 10f;
                    UiKit.Rect("Petal", carpet).Place(0.5f, 0.5f, d.x, d.y, size, size).AddImage(catalog.Round, Palette.Alpha(petals[k % petals.Length], 0.85f));
                    UiKit.Rect("Leaf", carpet).Place(0.5f, 0.5f, d.x + 9f, d.y - 7f, size * 0.7f, size * 0.45f).AddImage(catalog.Round, Palette.Alpha(Palette.Moss, 0.7f));
                }
            }

            // The moon lights the teapot: a slanted shaft coming down from the top corner, and a pool on the ground.
            var pool = UiKit.Rect("MoonPool", _street).Place(0f, 0f, Heart.x, Heart.y - 60f, 620f, 340f);
            _moonPool = pool.AddImage(catalog.Glow, new Color(0.82f, 0.9f, 1f, MoonPoolAlpha));
            var beam = UiKit.Rect("Moonbeam", _street).Place(0f, 0f, Heart.x + 40f, Heart.y - 40f, 560f, 1500f);
            beam.pivot = new Vector2(0.5f, 0f);
            beam.anchoredPosition = new Vector2(Heart.x + 40f, Heart.y - 40f);
            beam.localRotation = Quaternion.Euler(0, 0, -16f);
            _moonbeam = beam.gameObject.AddComponent<RawImage>();
            _moonbeam.texture = catalog.BgMoonbeam;
            _moonbeam.color = new Color(0.82f, 0.9f, 1f, catalog.BgMoonbeam != null ? MoonbeamAlpha : 0f);
            _moonbeam.raycastTarget = false;
        }

        /// <summary>Where the mortar for the mooncake game stands, and the tea shop: nothing else is placed on them.</summary>
        static Vector2 MortarSpot => Heart + new Vector2(-10f, 300f);
        /// <summary>Where the firefly jar stands, by the pond.</summary>
        static Vector2 JarSpot => PondSpot + new Vector2(-5f, -290f);

        /// <summary>
        /// The ground a station takes, sign, pad, rabbit box, name plate and prop included, at its drawn size: what the
        /// wood keeps <see cref="CanopyClearance"/> away from. The offsets are the station actor's, at their widest.
        /// </summary>
        static Rect StationReach(int i)
        {
            Vector2 spot = StationSpot(i);
            float s = StationSize(i);
            return Rect.MinMaxRect(spot.x - 210f * s, spot.y - 243f * s, spot.x + 250f * s, spot.y + 205f * s);
        }

        /// <summary>How far apart two rectangles stand: negative when they overlap.</summary>
        static float Apart(Rect a, Rect b)
        {
            float dx = Mathf.Max(a.xMin - b.xMax, b.xMin - a.xMax), dy = Mathf.Max(a.yMin - b.yMax, b.yMin - a.yMax);
            return Mathf.Max(dx, dy);
        }
        static Vector2 ShopSpot => PondSpot + new Vector2(-330f, -150f);

        /// <summary>Open grass: off every station's prop, plates and name, off the seats, the pond, the mortar and the shop.</summary>
        static bool OpenGrass(float x, float y, int stations)
        {
            if (x < 90f || x > WorldW - 90f || y < 160f || y > WorldH - 220f) return false;
            for (int i = 0; i < stations; i++)
            {
                Vector2 spot = StationSpot(i);
                if (Mathf.Abs(spot.x - x) < 200f && y > spot.y - 230f && y < spot.y + 230f) return false;
            }
            foreach (Vector2 seat in Seats)
                if (Vector2.Distance(Heart + seat, new Vector2(x, y)) < 150f) return false;
            var at = new Vector2(x, y);
            return Vector2.Distance(at, PondSpot) > 300f && Vector2.Distance(at, MortarSpot) > 180f && Vector2.Distance(at, ShopSpot) > 220f;
        }

        void BuildDecor(GameCatalog catalog, int stations, List<KeyValuePair<float, Transform>> standing)
        {
            Sprite[] d = catalog.Decor;
            if (d == null || d.Length < 9) return;
            const int Tree = 0, Bush = 1, Lantern = 2, Mushrooms = 4, Stones = 5, Pond = 6, TeaTable = 7, Stump = 8;
            Color soft = Palette.Mix(Color.white, Palette.Night, 0.12f);

            // The pond lies in the top corner, where the moonlight comes in.
            Vector2 pondAt = PondSpot;

            var canopyStanding = new List<KeyValuePair<float, Transform>>();
            // What the wood keeps clear of: every station as drawn, and the props with a plate to read (#50).
            var keepOff = new List<Rect>();
            for (int i = 0; i < stations; i++) keepOff.Add(StationReach(i));
            {
                int leafStation = 1;
                for (int i = 0; i < stations; i++) if (catalog.Stations[i] != null && catalog.Stations[i].Id == "gio-la") { leafStation = i; break; }
                Vector2 basketAt = StationSpot(leafStation) + new Vector2(104f, 130f);
                keepOff.Add(Rect.MinMaxRect(basketAt.x - 245f, basketAt.y, basketAt.x + 60f, basketAt.y + 100f));
                keepOff.Add(Rect.MinMaxRect(JarSpot.x - 140f, JarSpot.y - 40f, JarSpot.x + 140f, JarSpot.y + 160f));
                keepOff.Add(Rect.MinMaxRect(MortarSpot.x - 130f, MortarSpot.y - 40f, MortarSpot.x + 130f, MortarSpot.y + 160f));
            }
            bool CanopyClear(Rect tree)
            {
                foreach (Rect r in keepOff) if (Apart(tree, r) < CanopyClearance + 4f) return false;
                return true;
            }
            // Everything that stands casts a soft shadow on the grass, drawn just behind it.
            void Shade(Transform parent, List<KeyValuePair<float, Transform>> into, float x, float y, float height)
            {
                float w = Mathf.Clamp(height * 0.55f, 50f, 260f);
                var rt = UiKit.Rect("Shade", parent).Place(0f, 0f, x + 6f, y + 4f, w, w * 0.34f);
                rt.AddImage(catalog.Glow, Palette.Alpha(Palette.Night, 0.28f));
                into.Add(new KeyValuePair<float, Transform>(y + 0.25f, rt));
            }
            // One piece of scenery standing on the ground, on the street or on the canopy.
            Image Stand(Sprite sprite, string name, float x, float y, float height, Color tint, bool canopy, bool shade)
            {
                Transform parent = canopy && _canopy != null ? _canopy : (Transform)_street;
                List<KeyValuePair<float, Transform>> into = canopy && _canopy != null ? canopyStanding : standing;
                if (shade) Shade(parent, into, x, y, height);
                Image img = AddDecor(parent, sprite, x, y, height, tint);
                if (img == null) return null;
                img.name = name;
                into.Add(new KeyValuePair<float, Transform>(y, img.transform));
                return img;
            }
            void Put(int kind, float x, float y, float height, Color tint, bool canopy = false)
            {
                height *= DecorScale;
                // Nothing tall may stand in front of the pond: the moon in it has to stay in view.
                if (kind == Tree && x > 120f && x < WorldW - 60f && Mathf.Abs(x - pondAt.x) < 200f && y > pondAt.y - 330f && y < pondAt.y + 30f) return;
                if (canopy && (kind == Tree || kind == Bush) && d[kind] != null)
                {
                    // The wood never comes within CanopyClearance of a sign, a pad, a rabbit or a plate (#50). A tree of the
                    // top rows that would come nearer is stood further up instead, clear of the highest stations; any other is left out.
                    float w = height * d[kind].rect.width / d[kind].rect.height;
                    Rect Foot(float at) => Rect.MinMaxRect(x - w / 2f, at, x + w / 2f, at + height);
                    if (!CanopyClear(Foot(y)))
                    {
                        if (y < WorldH / 2f) return;
                        float lifted = y;
                        while (!CanopyClear(Foot(lifted)) && lifted < y + 320f) lifted += 20f;
                        if (!CanopyClear(Foot(lifted))) return;
                        y = lifted;
                    }
                }
                if (kind == Lantern)
                {
                    var at = new Vector2(x + 14f * DecorScale, y + 150f * DecorScale);
                    var glow = UiKit.Rect("LanternGlow", _street).Place(0f, 0f, at.x, at.y, 300f, 300f);
                    _lanternGlows.Add(glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, LanternGlowAlpha)));
                    standing.Add(new KeyValuePair<float, Transform>(y + 0.5f, glow));
                    _glowSpots.Add(at);
                }
                string[] names = { "Tree", "Bush", "Lantern", "Fence", "Mushrooms", "Stones", "Pond", "TeaTable", "Stump" };
                Image img = Stand(d[kind], names[kind], x, y, height, tint, canopy, kind != Pond && kind != Stones);
                if (kind == Tree) Rooted(img, "Tree", x, y, _swayTrees);
                if (kind == Lantern && img != null)
                {
                    // A lantern hangs from its hook, so it swings from the top.
                    img.name = "Lantern";
                    img.rectTransform.pivot = new Vector2(0.5f, 1f);
                    img.rectTransform.anchoredPosition = new Vector2(x, y + height);
                    _swayLanterns.Add(img.rectTransform);
                    _lanternArt.Add(img);
                }
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
            _pondGlow = UiKit.Rect("PondGlow", _street).Place(0f, 0f, pondAt.x + 20f, pondAt.y + 6f, 370f, 270f);
            _pondGlow.AddImage(catalog.Glow, new Color(0.80f, 0.90f, 1f, 0.3f));
            // Two rings of light that widen and fade in turn: the water moving under the moon.
            for (int r = 0; r < 2; r++)
                _ripples.Add(UiKit.Rect("Ripple", _street).Place(0f, 0f, pondAt.x + 20f, pondAt.y + 6f, 260f, 180f).AddImage(catalog.Glow, new Color(0.85f, 0.93f, 1f, 0f)));
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
                // Low enough that their crowns, not just their trunks, are in view when the garden is scrolled to the top.
                Put(Tree, x + jitter, WorldH - 390f + Hash(x + 5) * 40f, 380f + Hash(x + 9) * 70f, soft, true);
                Put(Tree, x + 75f - jitter, WorldH - 320f + Hash(x + 6) * 30f, 360f + Hash(x + 4) * 60f, soft, true);
                Put(Tree, x + jitter, -30f + Hash(x + 7) * 30f, 330f + Hash(x + 2) * 70f, soft, true);
                Put(Bush, x + 75f - jitter, 30f + Hash(x + 3) * 30f, 130f, soft, true);
            }
            // The wood past both sides stands well out from the lawn (#50): its nearest crowns end 200 units and more
            // short of every station, so at the usual zoom the sides are open grass and from far away the forest shows.
            // Beyond the first row the wood goes on, dense, so zooming out shows forest and not the end of the world.
            for (float y = -120f; y < WorldH + 60f; y += 160f)
                for (int row = 1; row <= 3; row++)
                {
                    float away = row * 80f + Hash(y + row) * 30f, lift = (row % 2) * 80f;
                    Put(Tree, RimLeft - away, y + lift, 330f + Hash(y + row * 3) * 90f, soft, true);
                    Put(Tree, RimRight + away, y + 80f - lift, 330f + Hash(y + row * 5) * 90f, soft, true);
                }
            for (float y = 180f; y < WorldH - 330f; y += 150f)
            {
                Put(Tree, RimLeft - 20f + Hash(y) * 40f, y, 340f + Hash(y + 4) * 80f, soft, true);
                Put(Tree, RimRight - 20f + Hash(y + 2) * 40f, y + 60f, 340f + Hash(y + 6) * 80f, soft, true);
            }
            // Where the wood used to stand, a low hedge marks the lawn's sides without hiding anything behind it.
            for (float y = 300f; y < WorldH - 500f; y += 190f)
            {
                Put(Bush, -70f + Hash(y + 8) * 30f, y + Hash(y + 9) * 60f, 120f, soft);
                Put(Bush, WorldW + 40f + Hash(y + 10) * 30f, y + 90f + Hash(y + 11) * 60f, 120f, soft);
            }

            // Around every station, the same small set, placed where it fits: a planter on one side, mushrooms by the
            // front, stones at the back. Each tries a few spots in order and takes the first that is clear.
            bool Clear(float x, float y, int own, float halfWidth)
            {
                if (x < 70f + halfWidth || x > WorldW - 70f - halfWidth || y < 160f || y > WorldH - 220f) return false;
                if (Vector2.Distance(new Vector2(x, y), pondAt) < 250f) return false;
                if (Vector2.Distance(new Vector2(x, y), MortarSpot) < 170f || Vector2.Distance(new Vector2(x, y), ShopSpot) < 220f) return false;
                if (OnDecorSpot(x, y)) return false; // the player's own spots stay clear for what they buy (#11)
                foreach (Vector2 seat in Seats)
                    if (Mathf.Abs(Heart.x + seat.x - x) < 170f && y > Heart.y + seat.y - 170f && y < Heart.y + seat.y + 200f) return false;
                for (int j = 0; j < stations; j++)
                {
                    // Prop, pad, level plate and name of every station, the item's own included: the teapot's are the tallest.
                    Vector2 spot = StationSpot(j);
                    float reach = (j == own ? 130f : 150f) + halfWidth, below = j == 0 ? 330f : 280f;
                    if (Mathf.Abs(spot.x - x) < reach && y > spot.y - below && y < spot.y + 240f) return false;
                }
                return true;
            }
            void Dress(int i, Sprite sprite, string name, float height, Vector2[] offsets, bool shade)
            {
                Vector2 spot = StationSpot(i);
                float side = spot.x >= Heart.x ? 1f : -1f;
                var tries = new List<Vector2>(offsets);
                // Failing the preferred spots, a ring around the station, nearest first: the same for the same garden every time.
                for (float r = 200f; r <= 320f; r += 60f)
                    for (int k = 0; k < 12; k++)
                        tries.Add(new Vector2(Mathf.Cos(k * Mathf.PI / 6f) * r, Mathf.Sin(k * Mathf.PI / 6f) * r));
                foreach (Vector2 o in tries)
                {
                    float x = spot.x + o.x * side, y = spot.y + o.y;
                    if (!Clear(x, y, i, height * 0.3f)) continue;
                    if (sprite != null) Stand(sprite, name, x, y, height, sprite == catalog.Planter || sprite == catalog.StoneLantern ? Color.white : soft, false, shade);
                    return;
                }
            }
            Vector2[] planterAt = { new Vector2(230f, -40f), new Vector2(-230f, -40f), new Vector2(170f, -260f), new Vector2(-170f, -260f), new Vector2(120f, -300f), new Vector2(-120f, -300f), new Vector2(0f, 230f), new Vector2(230f, 120f), new Vector2(-230f, 120f), new Vector2(260f, -120f), new Vector2(-260f, -120f), new Vector2(0f, -330f) };
            Vector2[] mushroomsAt = { new Vector2(-130f, -240f), new Vector2(130f, -240f), new Vector2(220f, 70f) };
            Vector2[] stonesAt = { new Vector2(200f, 160f), new Vector2(-200f, 160f), new Vector2(0f, -320f) };
            for (int i = 0; i < stations; i++)
            {
                Dress(i, catalog.Planter, "Planter", 92f, planterAt, true);
                Dress(i, d[Mushrooms], "Mushrooms", 84f * DecorScale, mushroomsAt, true);
                Dress(i, d[Stones], "Stones", 62f * DecorScale, stonesAt, false);
            }
            // Two stone lanterns mark the way into the corner where the rabbits rest.
            foreach (Vector2 o in new[] { new Vector2(-380f, -1010f), new Vector2(120f, -930f) })
            {
                Vector2 at = Heart + o;
                if (catalog.StoneLantern != null && Free(at.x, at.y, 150f, 230f, 240f)) Stand(catalog.StoneLantern, "StoneLantern", at.x, at.y, 150f, Color.white, false, true);
            }

            // Inside: a few small things wherever there is still room that is not a station, a seat or the pond.
            // All of it low (#50): no tree stands on the lawn, so nothing hides a station or a rabbit.
            for (int n = 0; n < 24; n++)
            {
                float x = 150f + Hash(n * 3.1f + 1f) * (WorldW - 300f), y = 200f + Hash(n * 5.7f + 2f) * (WorldH - 560f);
                float pick = Hash(n * 9.3f);
                if (!Free(x, y, 230f, 220f, 240f)) continue; // nothing may cover a level plate or a name
                if (Vector2.Distance(new Vector2(x, y), pondAt) < 240f) continue;
                if (OnDecorSpot(x, y)) continue;
                // Keep the places to rest in the open: nothing grows on a seat or in front of the tea table.
                bool onSeat = false;
                foreach (Vector2 seat in Seats)
                    if (Mathf.Abs(Heart.x + seat.x - x) < 190f && y > Heart.y + seat.y - 150f && y < Heart.y + seat.y + 190f) onSeat = true;
                if (onSeat) continue;
                if (pick < 0.46f) Put(Bush, x, y, 104f, soft);
                else if (pick < 0.76f) Put(Mushrooms, x, y, 84f, soft);
                else Put(Stones, x, y, 62f, soft);
            }

            // Stations glow too, and draw their own few fireflies.
            for (int i = 0; i < stations; i += 2) _glowSpots.Add(StationSpot(i) + new Vector2(0, 40));
            canopyStanding.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Transform> item in canopyStanding) item.Value.SetAsLastSibling();
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
                _levelText = UiKit.Rect("Text", badge).Place(0.5f, 0.5f, 0, -15, 70, 44).AddText("1", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            else
            {
                UiKit.Rect("Face", badge).Stretch(0, 0, 1, 1, 13, 13, 13, 13).AddImage(catalog.Round, Palette.Paper);
                _levelText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("1", 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            _levelBadge = badge;
            // The season's chip under the level, only once a new moon has been begun (#1).
            _seasonChip = UiKit.Rect("Season", _hud).Place(0f, 1f, 70, -140, 120, 40);
            _seasonChip.AddShadow(10f, 6f, 0.25f);
            _seasonChip.AddImage(catalog.Panel, Palette.Lantern, true);
            _seasonText = UiKit.Rect("Text", _seasonChip).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _seasonText.resizeTextForBestFit = true;
            _seasonText.resizeTextMinSize = 14;
            _seasonText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _seasonChip.gameObject.SetActive(false);

            // Golden carrots, under the purse, only once there is at least one: nothing strange for a new player.
            _goldenChip = UiKit.Rect("Golden", _hud).Place(0f, 1f, 240, -154, 170, 52); // clear of the season chip (#1) on its left
            _goldenChip.AddShadow(10f, 6f, 0.25f);
            _goldenChip.AddImage(catalog.Panel, Palette.Paper, true);
            if (catalog.GoldenCarrot != null) UiKit.Rect("Icon", _goldenChip).Place(0f, 0.5f, 28, 0, 38, 38).AddImage(catalog.GoldenCarrot, Color.white).preserveAspect = true;
            _goldenText = UiKit.Rect("Text", _goldenChip).Stretch(0, 0, 1, 1, 56, 0, 10, 0).AddText("0", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _goldenChip.gameObject.SetActive(false);
            _purse = UiKit.Rect("Purse", _hud).Place(0f, 1f, 274, -76, 296, 100);
            _purse.AddShadow();
            UiKit.Rect("Lip", _purse).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            Image purseFace = UiKit.Rect("Face", _purse).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            // A touch on the purse explains the number: which station earns what, and the factors on top.
            purseFace.raycastTarget = true;
            var purseButton = _purse.gameObject.AddComponent<Button>();
            purseButton.transition = Selectable.Transition.None;
            purseButton.targetGraphic = purseFace;
            purseButton.onClick.AddListener(() =>
            {
                if (_purseHeld) { _purseHeld = false; return; } // the hold already answered; letting go is not a tap
                Sfx.Click();
                Income.Open(Runner.Defs, Runner.State, Runner.Mods);
            });
            var purseTrigger = _purse.gameObject.AddComponent<EventTrigger>();
            AddDrag(purseTrigger, EventTriggerType.PointerDown, _ => PursePointer(true));
            AddDrag(purseTrigger, EventTriggerType.PointerUp, _ => PursePointer(false));
            AddDrag(purseTrigger, EventTriggerType.PointerExit, _ => PursePointer(false));
            _currency = UiKit.Rect("Currency", _purse).Place(0f, 0.5f, 44, 4, 60, 60);
            _currency.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _coins = UiKit.Rect("Coins", _purse).Stretch(0, 0.42f, 1, 1, 78, 0, 10, 6)
                .AddText("0", 38, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _perSecond = UiKit.Rect("PerSecond", _purse).Stretch(0, 0, 1, 0.46f, 78, 14, 10, 0)
                .AddText(L10n.T("persec", "0"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);

            // "Trà lực": the bar and the boost button are one control.
            // Ends before the corner plate (which starts 354 from the right edge), so "Tăng tốc" is never under it.
            var energy = UiKit.Rect("Energy", _hud).Place(0f, 1f, 578, -76, 288, 100);
            energy.AddShadow();
            _energyButton = UiKit.Chunky(energy, catalog.Panel, OnBoost);
            // Short of power: the bar says how long until a burst can be paid for, instead of doing nothing.
            RectTransform energyRect = energy;
            _energyButton.Dip.Denied = () =>
            {
                Sfx.Deny();
                Feel.FloatText(Feel.PointOf(energyRect, new Vector2(0, 70)), L10n.T("energy.wait", Span(Energy.SecondsUntilBoost(Runner.State))), Palette.Lantern, 32);
            };
            // The name and the action each get their own half of the top row, one line tall, and shrink to fit it:
            // "Trà lực 28" and "×2 · 30 giây" never run into each other (test).
            _energyLabel = UiKit.Rect("Label", _energyButton.FaceRect).Stretch(0, 0.56f, 0.5f, 1, 22, 0, 4, 8)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _energyLabel.resizeTextForBestFit = true; // "Tea power 100" stays on one line with large text
            _energyLabel.resizeTextMinSize = 16;
            _energyLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _energyLabel.verticalOverflow = VerticalWrapMode.Truncate;
            _energyAction = UiKit.Rect("Action", _energyButton.FaceRect).Stretch(0.5f, 0.56f, 1, 1, 4, 0, 22, 8)
                .AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            _energyAction.resizeTextForBestFit = true;
            _energyAction.resizeTextMinSize = 14;
            _energyAction.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _energyAction.verticalOverflow = VerticalWrapMode.Truncate;
            var track = UiKit.Rect("Track", _energyButton.FaceRect).Band(22, 22, 16, 18);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _energyFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _energyFill.AddImage(catalog.Panel, Palette.Moss, true);

            // The three round buttons in the corner stand on one plate, so they read as one cluster and a fourth has a place to go.
            UiKit.Rect("Corner", _hud).Place(1f, 1f, -184, -76, 340, 112).AddImage(catalog.Panel, Palette.Alpha(Palette.Paper, 0.55f), true);
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
                .AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

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
            _barBadgeText = UiKit.Rect("Text", _barBadgeRect).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _barBadge = _barBadgeRect.gameObject;
            // How many open stations still have nobody working, on the corner of the rabbits tab.
            _rabbitBadgeRect = UiKit.Rect("RabbitsBadge", _tabBar).Place(3.5f / TabCount, 0f, 46, 140, 48, 48);
            _rabbitBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _rabbitBadgeText = UiKit.Rect("Text", _rabbitBadgeRect).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _rabbitBadgeRect.gameObject.SetActive(false);
            // How many stations could be opened right now, on the corner of the stations tab.
            _stationBadgeRect = UiKit.Rect("StationsBadge", _tabBar).Place(0.5f / TabCount, 0f, 46, 140, 48, 48);
            _stationBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _stationBadgeText = UiKit.Rect("Text", _stationBadgeRect).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _stationBadgeRect.gameObject.SetActive(false);
            _shownClaimable = -1;
            _safeBottom = -1f;
            PlaceTabBar();
            ShowTabBar(0f);
        }

        /// <summary>
        /// What the phone keeps for itself at the bottom (home bar) and the top (notch), in canvas
        /// units: x is the bottom, y the top. Tests swap this for a phone of their choosing.
        /// </summary>
        public static System.Func<Vector2> SafeInsets = () => Screen.height > 0
            ? new Vector2(Mathf.Max(0f, Screen.safeArea.yMin / Screen.height * ScreenH), Mathf.Max(0f, (Screen.height - Screen.safeArea.yMax) / Screen.height * ScreenH))
            : Vector2.zero;

        /// <summary>The part of the bottom edge the phone keeps for itself (home bar), in canvas units.</summary>
        static float SafeBottom() => SafeInsets().x;

        /// <summary>Stands the tab bar just inside the safe area, the HUD under the notch, and the sheet's lists just above the bar.</summary>
        void PlaceTabBar()
        {
            Vector2 insets = SafeInsets();
            if (_hud != null && !Mathf.Approximately(_hud.anchoredPosition.y, -insets.y)) _hud.anchoredPosition = new Vector2(0f, -insets.y);
            float safe = insets.x;
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
            _tabFade = Mathf.MoveTowards(_tabFade, any ? 0.68f : 1f, dt / Motion.Quick); // the others step back, but stay readable
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
                _openLabels[i].color = Color.Lerp(Palette.Mix(Palette.Ink, Palette.Paper, 0.22f), Palette.Ink, k);
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

            // Nothing covers the garden above the sheet: it can still be dragged, a touch on open ground closes
            // the sheet, and a touch on a station closes it and opens that station's card.

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

            var close = UiKit.Rect("Close", bar).Place(1f, 0.5f, -62, -18, UiKit.MinTouch, UiKit.MinTouch);
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
            var upgradeAllButton = UiKit.Rect("Button", _upgradeAllRow).Place(1f, 0.5f, -166, 4, 284, UiKit.MinTouch);
            ChunkyButton upgradeAllChunky = UiKit.Chunky(upgradeAllButton, catalog.Panel, () => { Sfx.Click(); UpgradeAll.Open(Runner.Defs, Runner.State); });
            UiKit.Rect("Text", upgradeAllChunky.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("upgradeall.title"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            UiKit.Rect("Text", _questReady).Stretch(0, 0, 0.5f, 1).AddText(L10n.T("quests.ready"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var claimAll = UiKit.Rect("ClaimAll", _questReady).Place(1f, 0.5f, -130, 0, 260, 68);
            UiKit.Touchable(claimAll, 260, 68);
            _claimAll = UiKit.Chunky(claimAll, catalog.Panel, OnClaimAll);
            UiKit.Rect("Text", _claimAll.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("quests.claimall"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
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
                Runner.LookAtKind(kindId); // looked at now
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
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _plotSub = UiKit.Rect("Sub", _plotRow).Stretch(0, 0.14f, 1, 0.55f, 36, 0, 330, 6)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperLeft);
            var buy = UiKit.Rect("Buy", _plotRow).Place(1f, 0.5f, -166, 4, 284, 120);
            _plotBuy = UiKit.Chunky(buy, catalog.Panel, OnBuyPlot);
            _plotBuy.Dip.Denied = () => Deny(_plotBuy, !Expansion.HasCarrots(Runner.Defs, Runner.State));
            UiKit.Rect("Top", _plotBuy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 10)
                .AddText(L10n.T("plot.buy"), UiKit.TextSmall, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _plotCost = UiKit.Rect("Cost", _plotBuy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 10, 8, 0)
                .AddText("", UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            // With the list at its top, a pull down anywhere on it drags the sheet, and letting go decides.
            var pull = scrollRt.gameObject.AddComponent<SheetPull>();
            pull.List = scroll;
            pull.Scale = () => { Canvas c = _canvas != null ? _canvas.GetComponent<Canvas>() : null; return c != null ? c.scaleFactor : 1f; };
            pull.Dragged = DragSheet;
            pull.Released = ReleaseSheet;

            var content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset((int)UiKit.CardPad, (int)UiKit.CardPad, 4, 40);
            list.spacing = UiKit.CardGap;
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
            for (int i = 0; i < widths.Length; i++) widths[i] *= UiKit.TextScale; // large text: wider pills, never a second line
            float width = Pad * 2f + Gap * (modes.Length - 1);
            foreach (float w in widths) width += w;
            var track = UiKit.Rect("Track", parent).Place(1f, 0.5f, right - width / 2f, y, width, height);
            track.AddImage(catalog.Panel, Palette.Track, true);
            float x = right - width + Pad;
            for (int i = 0; i < modes.Length; i++)
            {
                int mode = modes[i];
                var rt = UiKit.Rect("Mode " + labels[i], parent).Place(1f, 0.5f, x + widths[i] / 2f, y, widths[i], height - 16f);
                UiKit.Touchable(rt, widths[i], height - 16f);
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
                _modeLabels.Add(new KeyValuePair<int, Text>(mode, UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(labels[i], UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold)));
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
            if (_scroll != null) _scroll.StopMovement(); // the garden stays live under the sheet; it only lets go of its momentum
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
            // The welcome-back sheet (#69); the carrots are already in the purse, Collect only closes it, ×2 pays again.
            AwayCard = new AwayView(_canvas, catalog, Tweens, () => Runner.AwayDoubleCost, () => Runner.State.GoldenCarrots, () => Runner.LastAwayDoubled,
                () => Feel.Burst(Feel.PointOf(_purse), catalog.Currency, new[] { Color.white }, 10, 480f, 46f),
                OnDoubleAway);
        }

        /// <summary>The ×2 on the welcome-back sheet: the latest time away paid once more, then the sheet closes.</summary>
        void OnDoubleAway()
        {
            BigNum paid = Runner.LastAway.Paid;
            if (!Runner.DoubleAway())
            {
                AwayCard.RefreshDouble();
                Sfx.Deny();
                return;
            }
            Sfx.Reward();
            Haptics.Medium();
            Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -90)), "+" + paid.Short(), Palette.Carrot, 44);
            Feel.Burst(Feel.PointOf(_purse), Runner.Catalog.Currency, new[] { Color.white }, 16, 520f, 46f);
            AwayCard.Close();
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

            // The camera glides from the moon pond down to where the player last looked, or to the teapot the first
            // time. It never blocks: any touch on the garden takes over.
            int run = ++_glideRun;
            GameState opening = Runner.State;
            if (opening.ViewZoom > 0d)
            {
                Vector2 center = new Vector2(ScreenW / 2f, ScreenH / 2f);
                Vector2 fromWorld = (center - _street.anchoredPosition) / _zoom, toWorld = new Vector2((float)opening.ViewX, (float)opening.ViewY);
                float fromZoom = _zoom, toZoom = Mathf.Clamp((float)opening.ViewZoom, ZoomFar, ZoomNear);
                Tweens.Add(Motion.Time(Motion.Glide), t =>
                {
                    if (run != _glideRun || _street == null) return;
                    SetViewRaw(Vector2.LerpUnclamped(fromWorld, toWorld, t), center, Mathf.LerpUnclamped(fromZoom, toZoom, t));
                }, Ease.InOutCubic);
            }
            else
            {
                Vector2 from = _street.anchoredPosition, home = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - StationSpot(0) * _zoom, _zoom);
                Tweens.Add(Motion.Time(Motion.Glide), t => { if (run == _glideRun && _street != null) _street.anchoredPosition = Vector2.LerpUnclamped(from, home, t); }, Ease.InOutCubic);
            }
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
            // The one-line hint is for gardens from before the guidance; a new garden gets the guidance instead.
            if (!_hintFading) _hint.SetActive(!s.HintSeen && Tutorial.IsDone(s) && Shell.State == ShellState.Playing && !SheetOpen);
            if (dt < 0f) _shownCoins = s.Coins.ToDouble();

            RefreshEnergy(s, boosting);
            UpdateDaylight();
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
            Profile.Refresh(defs, s, Runner.Account, Runner.CloudStatus);
            Income.Refresh(defs, s, m);
            bool mortar = Pounding.Unlocked(s);
            if (_mortarProp.gameObject.activeSelf != mortar) _mortarProp.gameObject.SetActive(mortar);
            if (mortar)
            {
                int left = Pounding.PlaysLeft(s, Runner.TodayNumber);
                _mortarPlate.text = left > 0 ? L10n.T("mortar.plays", left) : L10n.T("mortar.tomorrow");
            }
            bool basket = Leaves.Unlocked(s);
            if (_basketProp.gameObject.activeSelf != basket) _basketProp.gameObject.SetActive(basket);
            if (basket)
            {
                int left = Leaves.PlaysLeft(s, Runner.TodayNumber);
                _basketPlate.text = left > 0 ? L10n.T("basket.plays", left) : L10n.T("basket.tomorrow");
            }
            bool jar = Fireflies.Unlocked(s);
            if (_jarProp.gameObject.activeSelf != jar) _jarProp.gameObject.SetActive(jar);
            if (jar)
            {
                int left = Fireflies.PlaysLeft(s, Runner.TodayNumber);
                _jarPlate.text = left > 0 ? L10n.T("jar.plays", left) : L10n.T("jar.tomorrow");
            }
            Rabbit avatar = VuonTra.Core.Profile.Avatar(s);
            Shell.SetProfile(ProfileView.NameOf(s), s.Profile.Code, avatar != null ? Runner.Catalog.RabbitArt(avatar) : Runner.Catalog.Rabbit);

            int claimable = Quests.ClaimableCount(Runner.QuestDefs, defs, s);
            _barBadge.SetActive(claimable > 0);
            _barBadgeText.text = claimable.ToString();
            if (_shownClaimable >= 0 && claimable > _shownClaimable)
                Tweens.Add(0.32f, t => _badgePop = 0.4f * Ease.Pop(t), Ease.Linear, 0f, () => _badgePop = 0f); // one more to collect
            _shownClaimable = claimable;
            // The rabbits tab counts what waits there: stations with nobody, merges that can be made, kinds not yet looked at.
            int waiting = Rabbits.StationsWithoutWorker(s) + s.FreshKinds.Count;
            for (int tier = 0; tier < Rabbits.TierCount; tier++) if (Rabbits.CanMerge(s, tier)) waiting++;
            if (_rabbitBadgeRect.gameObject.activeSelf != (waiting > 0)) _rabbitBadgeRect.gameObject.SetActive(waiting > 0);
            _rabbitBadgeText.text = waiting.ToString();
            // The stations tab counts the stations that could be opened for what is in the purse.
            int openable = 0;
            for (int i = 0; i < defs.Count; i++)
                if (s.Levels[i] <= 0 && Economy.CanUnlock(s, i) && s.Coins >= Economy.CostOfNext(defs[i], 0, 1)) openable++;
            if (_stationBadgeRect.gameObject.activeSelf != (openable > 0)) _stationBadgeRect.gameObject.SetActive(openable > 0);
            _stationBadgeText.text = openable.ToString();
            _clovers.text = L10n.T("quests.clovers", s.Clovers);

            for (int i = 0; i < _modeButtons.Count; i++)
            {
                bool on = _modeButtons[i].Key == s.BuyMode;
                _modeButtons[i].Value.color = on ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _modeLabels[i].Value.color = on ? Palette.OnCarrot : Palette.InkSoft;
            }
            UpgradeAll.Refresh(defs, s);
            Shop.Refresh(s);
            RefreshDecor(s);
            RefreshOrders(s);
            bool anyGolden = s.GoldenCarrots > 0;
            if (_goldenChip.gameObject.activeSelf != anyGolden) _goldenChip.gameObject.SetActive(anyGolden);
            _goldenText.text = s.GoldenCarrots.ToString();
            bool shopNews = Runner.GiftWaiting || Runner.VisitorUnseen; // a gift to take, or a visitor not yet looked at
            if (_shopBadge != null && _shopBadge.activeSelf != shopNews) _shopBadge.SetActive(shopNews);
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
            if (_seasonChip != null)
            {
                _seasonChip.gameObject.SetActive(s.Season > 0);
                if (s.Season > 0) _seasonText.text = L10n.T("hud.season", s.Season);
            }
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
            _energyReady = can && !boosting;
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
                _actors[i].SetRank(look == StationLook.Open ? Economy.Rank(s.Levels[i]) : 0);
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
                long workerId = worker != null ? worker.Id : 0;
                if (i < _workerIds.Length && _workerIds[i] != workerId)
                {
                    // A newcomer walks over from wherever it was; under reduced motion, or before the garden has been seen, it is simply there.
                    if (workerId != 0 && _worldSeen && !Motion.Reduced) StartTravel(worker, i);
                    else CancelTravel(i);
                    _workerIds[i] = workerId;
                }
                if (worker == null) _actors[i].SetWorker(null, null, null, false);
                else
                {
                    // The collected rabbit itself stands at the station, wearing what it wears.
                    Item hat = Items.Worn(s, worker.Id, ItemSlot.Hat), charm = Items.Worn(s, worker.Id, ItemSlot.Charm);
                    RabbitKind kind = RabbitKinds.Find(Rabbits.KindOf(worker));
                    _actors[i].SetWorker(catalog.RabbitArt(worker),
                        hat != null && !kind.NoHat ? catalog.ItemIcon(hat.DefId) : null, charm != null ? catalog.ItemIcon(charm.DefId) : null,
                        worker.Tier >= 4 && !kind.OwnGlow, new Vector2(kind.HatX, kind.HatY),
                        hat != null ? catalog.ItemHatLift(hat.DefId) : 0f, RabbitGridView.Fur(worker),
                        catalog.TradeIcon(defs[i].Trade), defs[i].Trade);
                }

                GameObject chip = _tierChips[i].gameObject;
                bool named = worker != null && Zoom != ZoomLevel.Far && (!PhotoMode || PhotoNamesOn);
                if (chip.activeSelf != named) chip.SetActive(named);
                if (!named) continue;
                _tierChips[i].color = Palette.Tiers[worker.Tier];
                _tierLabels[i].text = worker.Name + L10n.LevelStars(worker);
                bool inTrade = Rabbits.InTrade(defs, worker);
                _tierTradeIcons[i].sprite = catalog.TradeIcon(Rabbits.TradeOf(worker));
                _tierTradeBadges[i].color = inTrade ? Palette.Lantern : Palette.Mix(Palette.Ink, Palette.Paper, 0.6f);
                _tierTradeIcons[i].color = inTrade ? Palette.Ink : Palette.Paper;
            }
            RefreshCard(defs, s, Runner.Mods);
            RefreshSeats(s);
            _worldSeen = true;
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
                    Rabbit r = seated[k];
                    Sprite art = catalog.RabbitArt(r);
                    Color fur = RabbitGridView.Fur(r);
                    _seatArt[k].sprite = art;
                    _seatArt[k].color = fur;
                    if (k < _walkers.Count) { _walkers[k].ArtImage.sprite = art; _walkers[k].ArtImage.color = fur; }
                    PoseSeated(k, r, s, catalog);
                }
                bool sitting = taken && !Away(k);
                if (_seats[k].gameObject.activeSelf != sitting) _seats[k].gameObject.SetActive(sitting);
                // The first two places are the stumps; each later one shows its furniture once it exists.
                bool cushion = k >= 2 && k < s.RestSpots;
                if (_seatCushions[k].activeSelf != cushion) _seatCushions[k].SetActive(cushion);
            }
        }

        /// <summary>
        /// A resting rabbit holds itself like one (#3), made from the standing picture by code until poses are drawn:
        /// a squat (wider, lower) on the stumps, the rug, the stool and the swing; lying along the hammock. Its hat
        /// comes with it, placed on the head the way the rabbit list places it, and its name sits under the seat.
        /// </summary>
        void PoseSeated(int k, Rabbit r, GameState s, GameCatalog catalog)
        {
            RectTransform art = _seatArt[k].rectTransform;
            bool lies = k < SeatLies.Length && SeatLies[k];
            art.localRotation = Quaternion.Euler(0, 0, lies ? -72f : 0f);
            art.localScale = lies ? new Vector3(0.92f, 0.92f, 1f) : new Vector3(1.06f, 0.86f, 1f);
            art.anchoredPosition = lies ? new Vector2(12f, 26f) : new Vector2(0f, -6f);

            const float Box = 124f;
            RabbitKind kind = RabbitKinds.Find(Rabbits.KindOf(r));
            Item hat = Items.Worn(s, r.Id, ItemSlot.Hat);
            Sprite icon = hat != null && kind != null && !kind.NoHat ? catalog.ItemIcon(hat.DefId) : null;
            Image hatImage = _seatHats[k];
            if (icon != null)
            {
                float across = (GameCatalog.KindHeadX - 0.5f + kind.HatX) * Box * GameCatalog.KindAspect;
                RabbitGridView.PlaceHat(hatImage.rectTransform, icon, Box, across, (GameCatalog.KindHeadTop + kind.HatY) * Box, catalog.ItemHatLift(hat.DefId));
                hatImage.sprite = icon;
            }
            if (hatImage.gameObject.activeSelf != (icon != null)) hatImage.gameObject.SetActive(icon != null);
            _seatNames[k].text = r.Name;
        }

        /// <summary>True when the one thing the station's card offers (land, opening, or levels) can be paid for.</summary>
        static bool CanAfford(IReadOnlyList<StationDef> defs, GameState s, int i)
        {
            if (i >= s.Plots) return Expansion.CanExpand(defs, s);
            int level = s.Levels[i];
            if (level <= 0) return Economy.CanUnlock(s, i) && s.Coins >= Economy.CostOfNext(defs[i], 0, 1);
            int count = Simulation.BuyCount(defs, s, i);
            return count > 0 && s.Coins >= Economy.CostOfNext(defs[i], level, count);
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

        /// <summary>
        /// Coming back (#69): five minutes away or more opens the welcome-back sheet; a shorter absence just sends
        /// the carrots up to the purse.
        /// </summary>
        public void ShowAway(AwayReport away)
        {
            if (away.WorthTelling) { AwayCard.Open(away, Runner.State); return; }
            if (away.Paid < BigNum.One || Feel == null) return;
            Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -90)), "+" + away.Paid.Short(), Palette.Carrot, 40);
            Feel.Burst(Feel.PointOf(_purse), Runner.Catalog.Currency, new[] { Color.white }, 8, 420f, 40f);
        }

        /// <summary>The sheet for a plain amount and time, as older callers and the tests had it.</summary>
        public void ShowAway(BigNum amount, double seconds) => ShowAway(new AwayReport
        {
            Paid = amount, Seconds = seconds, Capped = seconds >= Runner.Mods.AwayCapSeconds, SleepiestId = VuonTra.Core.Away.Sleepiest(Runner.State)
        });

        /// <summary>
        /// Two pills at the edges say how many stations out of view can be raised or have a dozing rabbit; a
        /// touch flies to the nearest. A teapot button appears when the garden's heart has scrolled away.
        /// </summary>
        void BuildEdges(GameCatalog catalog)
        {
            float bottom = ProductTray.Root.anchoredPosition.y + ProductTray.Root.sizeDelta.y + 26f;
            _edgeDown = EdgePill(catalog, "EdgeDown", bottom, () => FlyTo(_edgeDownTarget), out _edgeDownText);
            _edgeUp = EdgePill(catalog, "EdgeUp", ScreenH - 150f - 46f, () => FlyTo(_edgeUpTarget), out _edgeUpText);

            var home = UiKit.Rect("Home", _canvas).Place(1f, 0f, -72, bottom + 4f, 92, 92);
            home.AddShadow(14f, 10f, 0.3f);
            Image homeBack = home.AddImage(catalog.Round, Palette.Paper);
            homeBack.raycastTarget = true;
            var homeButton = home.gameObject.AddComponent<Button>();
            homeButton.transition = Selectable.Transition.None;
            homeButton.targetGraphic = homeBack;
            homeButton.onClick.AddListener(() => FlyTo(0));
            Sprite teapot = catalog.Stations.Length > 0 ? catalog.Stations[0].Prop : null;
            if (teapot != null) UiKit.Rect("Icon", home).Stretch(0, 0, 1, 1, 12, 12, 12, 12).AddImage(teapot, Color.white).preserveAspect = true;
            _home = home.gameObject;
            _home.SetActive(false);
        }

        RectTransform EdgePill(GameCatalog catalog, string name, float y, System.Action onTap, out Text text)
        {
            var pill = UiKit.Rect(name, _canvas).Place(0.5f, 0f, 0, y, 160, 56);
            UiKit.Touchable(pill, 160, 56);
            pill.AddShadow(12f, 8f, 0.3f);
            Image back = pill.AddImage(catalog.Panel, Palette.Paper, true);
            back.raycastTarget = true;
            var button = pill.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = back;
            button.onClick.AddListener(() => onTap());
            text = UiKit.Rect("Text", pill).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            pill.gameObject.SetActive(false);
            return pill;
        }

        void FlyTo(int index)
        {
            if (index < 0 || index >= _actors.Count) return;
            Sfx.Click();
            ZoomToStation(index, !Motion.Reduced);
        }

        /// <summary>Counts, per edge, the stations out of view that wait for the player, and keeps the nearest as the target.</summary>
        void UpdateEdges()
        {
            GameState s = Runner.State;
            bool shown = Shell.State == ShellState.Playing && !SheetOpen;
            int up = 0, down = 0;
            float upNear = float.MaxValue, downNear = float.MaxValue;
            _edgeUpTarget = -1;
            _edgeDownTarget = -1;
            if (shown)
            {
                for (int i = 0; i < _actors.Count && i < s.Plots; i++)
                {
                    if (!_actors[i].Root.gameObject.activeSelf) continue;
                    if (!_actors[i].CanUpgradeShown && !_actors[i].Dozing) continue;
                    // Where the station sits on the screen, from the view's own numbers: no layout pass needed.
                    float y = ViewPointOf(i).y - ScreenH / 2f;
                    if (Mathf.Abs(y) < ScreenH / 2f + 160f) continue;
                    if (y > 0f) { up++; if (y < upNear) { upNear = y; _edgeUpTarget = i; } }
                    else { down++; if (-y < downNear) { downNear = -y; _edgeDownTarget = i; } }
                }
            }
            if (_edgeUp.gameObject.activeSelf != (up > 0)) _edgeUp.gameObject.SetActive(up > 0);
            if (_edgeDown.gameObject.activeSelf != (down > 0)) _edgeDown.gameObject.SetActive(down > 0);
            if (up > 0) _edgeUpText.text = "▲ " + up;
            if (down > 0) _edgeDownText.text = "▼ " + down;
            bool far = shown && _actors.Count > 0 && Mathf.Abs(ViewPointOf(0).y - ScreenH / 2f) > ScreenH / 2f + 160f;
            if (_home.activeSelf != far) _home.SetActive(far);
        }

        /// <summary>A station's spot in screen space (0..ScreenW, 0..ScreenH), from the street's position and the zoom.</summary>
        Vector2 ViewPointOf(int station) => _street.anchoredPosition + StationSpot(station) * _zoom;

        /// <summary>True when a station is inside the visible part of the street.</summary>
        bool OnScreen(RectTransform actor)
        {
            Vector2 at = Feel.PointOf(actor);
            return Mathf.Abs(at.x) < ScreenW / 2f + 160f && Mathf.Abs(at.y) < ScreenH / 2f + 160f;
        }

        void OnTap(int index)
        {
            if (SheetOpen) CloseSheet(); // the station was touched through the open garden: the sheet gives way to its card
            if (Zoom == ZoomLevel.Far)
            {
                // From far away the station would sit behind its own card: come in to the usual zoom on it first.
                PlaceView(StationSpot(index), new Vector2(ScreenW / 2f, ScreenH * 0.6f), ZoomDefault);
            }
            SelectStation(index); // a tap always shows the station's card, asleep or not
            if (Runner.State.Levels[index] <= 0) return;
            // Waking a dozing rabbit is a small kindness, repaid in tea power; any touch is a stroke (#19).
            bool wake = _actors[index].Dozing;
            if (wake) _actors[index].SetDozing(false);
            PetStation(index, wake);
            Runner.Tap(index);
            _actors[index].Squish();
            if (_actors[index].HasGuest) _actors[index].Hop(); // the rabbit likes it: a little jump
            Sfx.Pet();
            DismissHint();
        }

        /// <summary>A finger went down on or came off the purse. Held long enough, the whole number floats up.</summary>
        public void PursePointer(bool down)
        {
            _purseDown = down;
            if (down) { _purseDownFor = 0f; _purseHeld = false; }
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
            int rankBefore = Economy.Rank(before);
            if (!Runner.Buy(index)) return;

            _rows[index].Buy.Press(Tweens);
            if (Selected == index) _cardBuy.Press(Tweens);
            Sfx.Upgrade();
            Haptics.Light();
            RectTransform actor = _actors[index].Root;
            _actors[index].Bump(); // from the station's fixed size: presses in quick succession do not add up
            _actors[index].Flash();

            GameCatalog catalog = Runner.Catalog;
            int after = s.Levels[index];
            if (before == 0)
            {
                Sfx.Unlock();
                Feel.Moment(L10n.T("moment.unlock"), L10n.Station(catalog.Stations[index].Id));
            }
            else if (Economy.Rank(after) > rankBefore)
            {
                // A new rank: the station now looks different for good, so this moment names the rank, not just the doubling.
                Sfx.Milestone();
                Haptics.Medium();
                _actors[index].SetRank(Economy.Rank(after)); // worn at once, not on the next refresh
                Feel.Moment(L10n.T("moment.rank.title", Economy.Rank(after)), L10n.T("moment.rank.sub", L10n.Station(catalog.Stations[index].Id), after));
                if (OnScreen(actor)) Feel.Burst(Feel.PointOf(actor, new Vector2(0, 60)), catalog.Round, new[] { Palette.Lantern, Palette.Paper, Palette.Blush }, 18, 520f, 24f);
            }
            else if (Economy.MilestoneMultiplier(after) > multiplierBefore)
            {
                Sfx.Milestone();
                Haptics.Medium();
                Feel.Moment(L10n.T("moment.milestone.title", Economy.PreviousMilestone(after)), L10n.T("moment.milestone.sub", L10n.Station(catalog.Stations[index].Id)));
                // A ring of little stars leaves the station: the doubling is the station's own moment too.
                if (OnScreen(actor)) Feel.Burst(Feel.PointOf(actor, new Vector2(0, 60)), catalog.Round, new[] { Palette.Lantern, Palette.Paper }, 14, 460f, 22f);
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
            Sfx.Summon(rabbit.Tier);
            Haptics.Medium();
            Welcome(rabbit);
        }

        void OnMerge(int tier)
        {
            Rabbit merged = Runner.MergeRabbits(tier);
            if (merged == null) return;
            Sfx.Summon(merged.Tier);
            Welcome(merged);
        }

        /// <summary>The mortar was touched: a game starts if a play is left today; otherwise the plate says to come back tomorrow.</summary>
        public void OpenPounding()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || Pound.IsOpen || Basket.IsOpen || Jar.IsOpen) return;
            if (!Pounding.CanPlay(Runner.State, Runner.TodayNumber))
            {
                Sfx.Deny();
                Feel.FloatText(Feel.PointOf(_mortarProp, new Vector2(0, 120)), L10n.T("mortar.tomorrow"), Palette.Lantern, 36);
                return;
            }
            PoundingRun run = Runner.StartPounding();
            if (run == null) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            Rabbit helper = Pounding.Helper(Runner.State);
            Pound.Open(run, helper != null ? Runner.Catalog.RabbitArt(helper) : null, helper != null ? helper.Name : "");
            Refresh(-1f);
        }

        /// <summary>The basket was touched: a leaf-catching game starts if a play is left today; otherwise the plate says to come back tomorrow.</summary>
        public void OpenLeaves()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || Basket.IsOpen || Pound.IsOpen || Jar.IsOpen) return;
            if (!Leaves.CanPlay(Runner.State, Runner.TodayNumber))
            {
                Sfx.Deny();
                Feel.FloatText(Feel.PointOf(_basketProp, new Vector2(0, 100)), L10n.T("basket.tomorrow"), Palette.Lantern, 36);
                return;
            }
            LeavesRun run = Runner.StartLeaves();
            if (run == null) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            Basket.Open(run);
            Refresh(-1f);
        }

        /// <summary>The jar was touched: a firefly night starts if a play is left today; otherwise the plate says to come back tomorrow.</summary>
        public void OpenFireflies()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || Jar.IsOpen || Basket.IsOpen || Pound.IsOpen) return;
            if (!Fireflies.CanPlay(Runner.State, Runner.TodayNumber))
            {
                Sfx.Deny();
                Feel.FloatText(Feel.PointOf(_jarProp, new Vector2(0, 100)), L10n.T("jar.tomorrow"), Palette.Lantern, 36);
                return;
            }
            FirefliesRun run = Runner.StartFireflies();
            if (run == null) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            Jar.Open(run);
            Refresh(-1f);
        }

        public void OpenShop()
        {
            if (Shell.State != ShellState.Playing || PhotoMode) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Runner.MarkVisitorSeen();
            Shop.Open(Runner.State);
        }

        /// <summary>Today's visitor goes home with the player: it steps out of the moon gate like any arrival.</summary>
        void OnBuyVisitor()
        {
            Rabbit rabbit = Runner.BuyVisitor();
            if (rabbit == null) return;
            Shop.Close();
            Sfx.Summon(rabbit.Tier);
            Haptics.Medium();
            Welcome(rabbit);
        }

        /// <summary>Today's gift from the shop: golden carrots fly from the gift box up to their place on the HUD.</summary>
        /// <summary>A golden pack bought (#9): once the store has taken the money, the carrots fly to their chip.</summary>
        void OnBuyPack(string packId, System.Action<PurchaseResult> done)
        {
            Runner.BuyGolden(packId, result =>
            {
                if (result.Outcome == PurchaseOutcome.Paid)
                {
                    Sfx.Reward();
                    Haptics.Light();
                    GameCatalog catalog = Runner.Catalog;
                    GoldenPack pack = Storefront.Find(packId);
                    Vector2 from = new Vector2(0, -120); // the middle of the packs card
                    Feel.Fly(from, Feel.PointOf(_goldenChip), catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, 5, 44f,
                        () => Tweens.Add(Motion.Tap, t => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one; }));
                    Feel.FloatText(new Vector2(0, 420), L10n.T("packs.float", pack != null ? pack.Golden : 0), Palette.Lantern, 44);
                    Refresh(-1f);
                }
                done?.Invoke(result);
            });
        }

        void OnDailyGift()
        {
            int gift = Runner.ClaimDailyGift();
            if (gift <= 0) return;
            Sfx.Reward();
            Haptics.Light();
            GameCatalog catalog = Runner.Catalog;
            Vector2 from = Feel.PointOf(Shop.Sheet.Card, new Vector2(0, 200));
            Feel.Fly(from, Feel.PointOf(_goldenChip), catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, Mathf.Min(gift, 5), 44f,
                () => Tweens.Add(Motion.Tap, t => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one; }));
        }

        /// <summary>A hat for looks bought at the shop (#44): it goes to the wardrobe, where the rabbits' pickers find it.</summary>
        void OnBuyHat(string defId)
        {
            Item item = Runner.BuyItem(defId);
            if (item == null) return;
            Sfx.Reward();
            Haptics.Light();
            Feel.FloatText(new Vector2(0, 420), L10n.T("float.hat", L10n.ItemName(item)), Palette.Lantern, 40);
        }

        /// <summary>A rabbit's fur dyed from its sheet (#44): a word in the colour it now wears.</summary>
        void OnDye(long rabbitId, int dye)
        {
            if (!Runner.DyeRabbit(rabbitId, dye)) return;
            Sfx.Blip();
            Haptics.Light();
            VuonTra.Core.Shop.Dye colour = VuonTra.Core.Shop.Dyes[dye]; // the Shop property here is the sheet
            Feel.FloatText(new Vector2(0, 420), L10n.T("float.dye", L10n.T("dye." + colour.Id)), Palette.Hex(colour.Hex), 40);
        }

        void OnSummonGolden()
        {
            Rabbit rabbit = Runner.SummonGoldenRabbit();
            if (rabbit == null) return;
            Shop.Close();
            Sfx.Summon(rabbit.Tier);
            Haptics.Medium();
            Welcome(rabbit);
        }

        /// <summary>A rabbit has just arrived, called or merged: it steps out of the moon gate.</summary>
        /// <summary>The compare card was held and pressed: the season begins, the moon sets and rises, and the garden is small again.</summary>
        void OnNewMoonBegun()
        {
            if (!Runner.BeginNewMoon(out Rabbit gift)) return;
            Shell.Resume();
            Haptics.Medium();
            int season = Runner.State.Season;
            NewMoonMoment.Open(season, () =>
            {
                SelectStation(-1);
                LookAt(StationSpot(0));
                Refresh(-1f);
                Sfx.Sting();
                if (gift != null) Welcome(gift);
                else Feel.Moment(L10n.T("newmoon.done.title", season), L10n.T("newmoon.done.sub", (int)System.Math.Round((NewMoon.Bonus(season) - 1d) * 100d), NewMoon.GiftClovers, NewMoon.GiftGolden));
            });
        }

        void Welcome(Rabbit rabbit)
        {
            Arrival.SetSeasonDecor(Runner.Catalog.GateDecor(Runner.SeasonNow));
            Arrival.Show(Runner.Catalog.RabbitArt(rabbit), rabbit.Name, L10n.T("detail.kind", L10n.Kind(rabbit), L10n.Rarity(rabbit.Tier)),
                Palette.Tiers[rabbit.Tier], rabbit.FirstOfKind, L10n.T("summon.new"), L10n.T("summon.hint"), rabbit.Tier == 0);
        }

        void OnLevelUp(long rabbitId)
        {
            Rabbit r = Rabbits.Find(Runner.State, rabbitId);
            int before = r != null ? r.Level : 0;
            if (!Runner.LevelUpRabbit(rabbitId)) return;
            Sfx.Upgrade();
            Haptics.Light();
            Feel.FloatText(new Vector2(-110, 300), L10n.T("float.levelup", r.Name, r.Level), Palette.Lantern, 44);
            StarMoment(r, before);
        }

        /// <summary>A climb that passed a ten-level mark is a small occasion: the banner names the star.</summary>
        void StarMoment(Rabbit r, int before)
        {
            if (Rabbits.StarsEarned(before, r.Level) <= 0) return;
            Sfx.Sting();
            Feel.Moment(L10n.T("moment.star.title", r.Name, Rabbits.Stars(r)), L10n.T("moment.star.sub", r.Level));
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
            Rabbit r = Rabbits.Find(Runner.State, rabbitId);
            int before = r != null ? r.Level : 0;
            int n = Runner.LevelUpRabbitMax(rabbitId);
            if (n <= 0) return;
            Sfx.Upgrade();
            Haptics.Light();
            Feel.FloatText(new Vector2(-110, 300), L10n.T("float.levelup", r.Name, r.Level), Palette.Lantern, 44);
            StarMoment(r, before);
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
                Feel.Moment(L10n.T("moment.gift.title", L10n.ItemName(gift)), L10n.T("moment.gift.sub", L10n.Rarity(gift.Tier), ItemPickerView.Describe(gift)));
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
        /// <summary>A stretch of time in minutes, or hours and minutes, as the player reads it.</summary>
        public static string Span(double seconds)
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

        void PaySale(Vector2 from, BigNum paid, int golden)
        {
            if (paid <= BigNum.Zero && golden <= 0) return;
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
            if (golden > 0)
            {
                // A golden batch pays golden carrots: they fly to their chip under the purse.
                if (!_goldenChip.gameObject.activeSelf) _goldenChip.gameObject.SetActive(true);
                Feel.Fly(from, Feel.PointOf(_goldenChip), catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, golden, 38f,
                    () => Tweens.Add(Motion.Tap, t => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one; }));
            }
        }

        /// <summary>A finger sweeping across the tray by this many canvas units, as the drag handler sees it.</summary>
        public void SweepTray(float distance)
        {
            if (Mathf.Abs(distance) >= TrayView.SweepDistance) OnSellAllProducts();
        }

        void OnPayout(int index, BigNum amount) => PlayPayout(index, amount);

        /// <summary>A station just paid: the amount floats up, two carrots fly to the purse, and the purse swells when they land.</summary>
        public void PlayPayout(int index, BigNum amount)
        {
            if (Shell.State == ShellState.Title) return;
            RectTransform actor = _actors[index].Root;
            _actors[index].Hop();
            if (!actor.gameObject.activeSelf || !OnScreen(actor)) return;
            if (SheetOpen && Feel.PointOf(actor).y + ScreenH / 2f < SheetHeight + 60f) return; // it stands behind the sheet

            Feel.FloatText(Feel.PointOf(actor, new Vector2(60, 150)), "+" + amount.Short(), Palette.Paper);
            Sfx.Tick();
            if (Feel.Flying > 12) return; // a busy garden does not fill the screen with carrots
            Feel.Fly(Feel.PointOf(actor, new Vector2(110, 60)), Feel.PointOf(_currency), Runner.Catalog.Currency, Color.white, 2, 36f, () =>
            {
                // The purse answers when the carrots arrive.
                Tweens.Add(0.15f, t => { if (_currency != null) _currency.localScale = Vector3.one * (1f + 0.25f * Ease.Pop(t)); }, Ease.Linear);
            });
        }

        // ----- touching and stroking rabbits (#19) -----

        RectTransform _bondLine;
        Text _bondText;
        int _bondToken;

        /// <summary>The rabbit at a station was touched or stroked.</summary>
        void PetStation(int index, bool wake = false)
        {
            Rabbit worker = Rabbits.AtStation(Runner.State, index);
            if (worker == null || !_actors[index].Root.gameObject.activeSelf) return;
            Pet(worker.Id, _actors[index].Root, new Vector2(-70, 140), wake);
        }

        /// <summary>A finger was dragged across a working rabbit: a stroke, not a tap on its station.</summary>
        void OnStroke(int index)
        {
            if (Runner.State.Levels[index] <= 0) return;
            bool wake = _actors[index].Dozing;
            if (wake) _actors[index].SetDozing(false);
            PetStation(index, wake);
            Sfx.Pet();
            DismissHint();
        }

        /// <summary>A resting rabbit was touched where it sits or walks: a stroke, and its own sheet.</summary>
        void OnSeat(int seat)
        {
            if (seat < 0 || seat >= _seatIds.Length || _seatIds[seat] <= 0) return;
            StrokeSeat(seat);
            Sfx.Click();
            Detail.Open(Runner.Defs, Runner.State, _seatIds[seat]);
        }

        /// <summary>A finger was dragged across a resting rabbit: a stroke alone, the sheet stays shut.</summary>
        void StrokeSeat(int seat)
        {
            if (seat < 0 || seat >= _seatIds.Length || _seatIds[seat] <= 0) return;
            // Hearts rise from wherever the rabbit is at the moment: its seat, or the lane it strolls.
            RectTransform near = seat < _walkers.Count && _walkers[seat].Rect.gameObject.activeSelf ? _walkers[seat].Rect : (seat < _seats.Count ? _seats[seat] : null);
            if (near == null) return;
            Pet(_seatIds[seat], near, new Vector2(0, 150), false);
            Sfx.Pet();
        }

        /// <summary>
        /// A rabbit was touched or stroked, wherever it is: hearts float up, tea power on the first stroke of
        /// the day, a line when the bond reaches three hearts, and its clover when the bond is full.
        /// </summary>
        void Pet(long rabbitId, RectTransform near, Vector2 offset, bool wake)
        {
            Rabbit r = Rabbits.Find(Runner.State, rabbitId);
            if (r == null) return;
            PetResult pet = Runner.PetRabbit(rabbitId, wake);
            if (!pet.Found) return;
            GameCatalog catalog = Runner.Catalog;
            Vector2 at = Feel.PointOf(near, offset);
            Haptics.Light();
            // The heart comes first, always; the reward, when there is one, follows it up.
            if (pet.FirstToday)
            {
                // The day's first stroke: a burst of hearts and a little sparkle.
                Feel.FloatText(at + new Vector2(70, 30), "♥", Palette.Blush, 54);
                Feel.FloatText(at + new Vector2(20, 70), "♥", Palette.Blush, 36);
                Feel.FloatText(at + new Vector2(110, 80), "♥", Palette.Blush, 30);
                Feel.Burst(at + new Vector2(60, 20), catalog.Round, new[] { Palette.Blush, Palette.Paper }, 8, 420f, 18f);
            }
            else Feel.FloatText(at + new Vector2(70, 30), "♥", Palette.Blush, 36); // the rest of the day: a smaller heart
            if (pet.Energy > 0d) Feel.FloatText(at + new Vector2(-110, -60), L10n.T("doze.reward", (int)pet.Energy), Palette.Lantern, 38); // by the shoulder, clear of the hearts and of any line
            if (pet.LineUnlocked) Say(near, offset, L10n.T("bond.line." + Rabbits.TradeOf(r)));
            if (pet.GiftGiven)
            {
                Sfx.Sting();
                Feel.Moment(L10n.T("bond.gift.title", r.Name), L10n.T("bond.gift.sub", Rabbits.AffectionGiftClovers));
                // The clover flies to the rabbits tab, where clovers are spent.
                Feel.Fly(at, Feel.PointOf(_tabIcons[3].rectTransform), catalog.Clover != null ? catalog.Clover : catalog.Round,
                    catalog.Clover != null ? Color.white : Palette.Moss, Rabbits.AffectionGiftClovers, 38f);
            }
        }

        /// <summary>A short line in a paper bubble over a rabbit in the garden; it springs in and leaves by itself.</summary>
        void Say(RectTransform near, Vector2 offset, string text)
        {
            if (_bondLine == null)
            {
                _bondLine = UiKit.Rect("BondLine", _street).Place(0f, 0f, 0, 0, 520, 72);
                _bondLine.pivot = new Vector2(0.5f, 0f);
                _bondLine.AddImage(Runner.Catalog.Panel, Palette.Paper, true);
                _bondText = UiKit.Rect("Text", _bondLine).Stretch(0, 0, 1, 1, 18, 6, 18, 6)
                    .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                _bondText.resizeTextForBestFit = true; // the longest line in either language stays on its plate
                _bondText.resizeTextMinSize = 16;
                _bondText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
                _bondText.horizontalOverflow = HorizontalWrapMode.Wrap;
            }
            int token = ++_bondToken;
            _bondText.text = text;
            // Both sit on the street, so the bubble's place is the rabbit's own, lifted clear of its ears.
            _bondLine.anchoredPosition = near.anchoredPosition + new Vector2(0, Mathf.Abs(offset.y) * near.localScale.y + 60f);
            _bondLine.SetAsLastSibling();
            _bondLine.gameObject.SetActive(true);
            _bondLine.localScale = Vector3.zero;
            Tweens.Add(Motion.Time(Motion.Panel), t => _bondLine.localScale = Vector3.one * t, Motion.Curve(Ease.OutBack));
            Tweens.Add(Motion.Time(Motion.PanelOut), t => { if (token == _bondToken) _bondLine.localScale = Vector3.one * (1f - t); }, Ease.InCubic,
                Motion.Speech, () => { if (token == _bondToken) _bondLine.gameObject.SetActive(false); });
        }
        #region Guests and their tea orders (#20)

        /// <summary>The guest's order card, opened from the guest or the HUD pill.</summary>
        public OrderView OrderCard { get; private set; }
        /// <summary>Where a guest stands to order: in front of the tea shop, at the counter's open side.</summary>
        public static Vector2 GuestSpot => PondSpot + new Vector2(-110f, -250f);
        /// <summary>Where a guest comes in from and goes back to: off the garden's left edge, at the counter's height.</summary>
        static Vector2 GuestEntry => new Vector2(-90f, GuestSpot.y - 40f);
        /// <summary>How long the walk to the counter takes.</summary>
        public const float GuestWalkSeconds = 2.4f;
        enum GuestPhase { Away, Arriving, Waiting, Bowing, Leaving }
        GuestPhase _guestPhase;
        /// <summary>0 at the entry point, 1 at the counter.</summary>
        float _guestT;
        RectTransform _guest, _guestArt, _bubble;
        Image _guestImage;
        readonly RectTransform[] _bubbleItems = new RectTransform[VuonTra.Core.Orders.MaxLines];
        readonly Image[] _bubbleIcons = new Image[VuonTra.Core.Orders.MaxLines];
        readonly Text[] _bubbleCounts = new Text[VuonTra.Core.Orders.MaxLines];
        GameObject _orderBadge;
        /// <summary>True while a guest is in the garden: walking in, waiting, bowing or walking away.</summary>
        public bool GuestShown => _guest != null && _guest.gameObject.activeSelf;
        /// <summary>True once the guest stands at the counter with its order up.</summary>
        public bool GuestAtCounter => _guestPhase == GuestPhase.Waiting;

        void BuildOrders(GameCatalog catalog)
        {
            _guestPhase = GuestPhase.Away;
            _guestT = 0f;
            // The guest: a rabbit like the walkers, drawn over everything in the garden so it is never behind the shop.
            _guest = UiKit.Rect("Guest", _street).Place(0f, 0f, GuestEntry.x, GuestEntry.y, 104, 110);
            _guest.pivot = new Vector2(0.5f, 0f);
            UiKit.Rect("Shadow", _guest).Place(0.5f, 0f, 0, 2, 68, 16).AddImage(catalog.Round, Palette.Alpha(Palette.Night, 0.35f));
            _guestArt = UiKit.Rect("Art", _guest).Stretch(0, 0, 1, 1);
            _guestArt.pivot = new Vector2(0.5f, 0f); // a bow tips from the feet
            _guestImage = _guestArt.AddImage(catalog.Rabbit, Color.white);
            _guestImage.preserveAspect = true;
            _guestImage.raycastTarget = true;
            var guestButton = _guest.gameObject.AddComponent<Button>();
            guestButton.transition = Selectable.Transition.None;
            guestButton.targetGraphic = _guestImage;
            guestButton.onClick.AddListener(OpenOrder);
            // The speech bubble above its head: the products it wants, each with its count.
            _bubble = UiKit.Rect("Bubble", _guest).Place(0.5f, 1f, 34, 64, 120, 100);
            _bubble.AddShadow(10f, 6f, 0.25f);
            _bubble.AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = false;
            UiKit.Rect("Tail", _bubble).Place(0f, 0f, 28, -6, 26, 26).AddImage(catalog.Round, Palette.Paper).raycastTarget = false;
            for (int i = 0; i < _bubbleItems.Length; i++)
            {
                var item = UiKit.Rect("Item " + i, _bubble).Place(0f, 0.5f, 56 + i * 104, 2, 84, 84);
                _bubbleItems[i] = item;
                _bubbleIcons[i] = UiKit.Rect("Icon", item).Stretch(0, 0, 1, 1, 4, 4, 4, 4).AddImage(null, Color.white);
                _bubbleIcons[i].preserveAspect = true;
                _bubbleIcons[i].raycastTarget = false;
                var count = UiKit.Rect("Count", item).Place(1f, 0f, -10, 10, 46, 32);
                count.AddImage(catalog.Panel, Palette.CarrotLip, true).raycastTarget = false;
                _bubbleCounts[i] = UiKit.Rect("Text", count).Stretch(0, 0, 1, 1).AddText("", 20, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            _bubble.gameObject.SetActive(false);
            _guest.SetAsLastSibling();
            _guest.gameObject.SetActive(false);

            // The HUD pill that says a guest waits, wherever the player has scrolled: a touch goes to the guest and opens the card.
            var badge = UiKit.Rect("Order", _hud).Place(1f, 1f, -184, -176, 230, 56);
            UiKit.Touchable(badge, 230, 56);
            badge.AddShadow(12f, 8f, 0.3f);
            Image badgeBack = badge.AddImage(catalog.Panel, Palette.Lantern, true);
            badgeBack.raycastTarget = true;
            var badgeButton = badge.gameObject.AddComponent<Button>();
            badgeButton.transition = Selectable.Transition.None;
            badgeButton.targetGraphic = badgeBack;
            badgeButton.onClick.AddListener(() => { GlideTo(GuestSpot); OpenOrder(); });
            if (catalog.Rabbit != null) UiKit.Rect("Icon", badge).Place(0f, 0.5f, 34, 2, 44, 44).AddImage(catalog.Rabbit, Color.white).preserveAspect = true;
            UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1, 64, 0, 14, 0).AddText(L10n.T("order.badge"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _orderBadge = badge.gameObject;
            _orderBadge.SetActive(false);

            OrderCard = new OrderView(_canvas, catalog, Tweens, ProductTray.ProductSprite, OnDeliverOrder);
        }

        /// <summary>Open-play time goes to the counter's clock, and the guest walks in, waits, bows or walks away.</summary>
        void UpdateOrders(float dt)
        {
            OrderEvent e = Runner.AdvanceOrders(dt);
            if (e != OrderEvent.None)
            {
                if (e == OrderEvent.Arrived) Sfx.Blip();
                Refresh(-1f);
            }
            if (_guestPhase == GuestPhase.Arriving || _guestPhase == GuestPhase.Leaving)
            {
                float dir = _guestPhase == GuestPhase.Arriving ? 1f : -1f;
                _guestT = Mathf.Clamp01(_guestT + dir * dt / Motion.Time(GuestWalkSeconds));
                PlaceGuest(true);
                if (_guestPhase == GuestPhase.Arriving && _guestT >= 1f)
                {
                    _guestPhase = GuestPhase.Waiting;
                    PlaceGuest(false);
                    _bubble.gameObject.SetActive(Runner.State.Order != null);
                    RectTransform bubble = _bubble;
                    Tweens.Add(Motion.Time(Motion.Tap), t => { if (bubble != null) bubble.localScale = Vector3.one * (1f + 0.2f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (bubble != null) bubble.localScale = Vector3.one; });
                }
                else if (_guestPhase == GuestPhase.Leaving && _guestT <= 0f)
                {
                    _guestPhase = GuestPhase.Away;
                    _guest.gameObject.SetActive(false);
                }
            }
            else if (_guestPhase == GuestPhase.Waiting && !Motion.Reduced)
            {
                _guestArt.localScale = new Vector3(1f, 1f + 0.02f * Mathf.Sin(_clock * 2.2f), 1f); // breathing while it waits
            }
        }

        void PlaceGuest(bool walking)
        {
            Vector2 at = Vector2.Lerp(GuestEntry, GuestSpot, _guestT);
            if (walking && !Motion.Reduced) at.y += Mathf.Abs(Mathf.Sin(_guestT * Mathf.PI * 9f)) * 14f;
            _guest.anchoredPosition = at;
            _guestArt.localScale = new Vector3(_guestPhase == GuestPhase.Leaving ? -1f : 1f, 1f, 1f);
        }

        /// <summary>What the state says about the counter: a guest walks in when an order appears (or stands there already, after a load), walks away when it is gone; the bubble and the HUD pill follow.</summary>
        void RefreshOrders(GameState s)
        {
            Order o = s.Order;
            if (o != null && (_guestPhase == GuestPhase.Away || _guestPhase == GuestPhase.Leaving))
            {
                if (_guestPhase == GuestPhase.Away)
                {
                    _guest.gameObject.SetActive(true);
                    // An order that has already waited a while (a loaded save, a rebuilt screen) has its guest standing there; a fresh one walks in.
                    bool fresh = o.Left > VuonTra.Core.Orders.WaitSeconds - 1d;
                    _guestT = fresh ? 0f : 1f;
                    _guestPhase = fresh ? GuestPhase.Arriving : GuestPhase.Waiting;
                    PlaceGuest(fresh);
                    _bubble.gameObject.SetActive(!fresh);
                }
                else _guestPhase = GuestPhase.Arriving; // turned back at the gate
            }
            else if (o == null && (_guestPhase == GuestPhase.Arriving || _guestPhase == GuestPhase.Waiting))
            {
                // Patience ran out: the guest goes without a word.
                _guestPhase = GuestPhase.Leaving;
                _bubble.gameObject.SetActive(false);
                if (OrderCard.IsOpen) OrderCard.Close();
            }
            if (o != null)
            {
                // The guest is drawn as its kind, the same face the card shows.
                Sprite face = Runner.Catalog.KindSprite(o.Guest);
                if (face != null && _guestImage.sprite != face) _guestImage.sprite = face;
                for (int i = 0; i < _bubbleItems.Length; i++)
                {
                    bool has = i < o.Lines.Count;
                    if (_bubbleItems[i].gameObject.activeSelf != has) _bubbleItems[i].gameObject.SetActive(has);
                    if (!has) continue;
                    _bubbleIcons[i].sprite = ProductTray.ProductSprite(o.Lines[i].Station);
                    _bubbleCounts[i].text = "×" + o.Lines[i].Count;
                    _bubbleCounts[i].color = VuonTra.Core.Orders.LineMet(s, o.Lines[i]) ? Palette.Moss : Palette.Card;
                }
                _bubble.sizeDelta = new Vector2(16 + o.Lines.Count * 104, 100);
            }
            bool waiting = o != null;
            if (_orderBadge.activeSelf != waiting) _orderBadge.SetActive(waiting);
            OrderCard.Refresh(s, Runner.Mods, Runner.Defs);
        }

        /// <summary>Opens the guest's order card, over the plain garden view.</summary>
        public void OpenOrder()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || Runner.State.Order == null) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            OrderCard.Open(Runner.State, Runner.Mods, Runner.Defs);
        }

        /// <summary>Slides the view so a garden point sits a little above the middle of the screen.</summary>
        void GlideTo(Vector2 point)
        {
            Vector2 from = _street.anchoredPosition, to = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - point * _zoom, _zoom);
            if (_scroll != null) _scroll.StopMovement();
            int run = ++_glideRun;
            Tweens.Add(Motion.Time(Motion.Glide * 0.6f), t => { if (run == _glideRun && _street != null) _street.anchoredPosition = Vector2.LerpUnclamped(from, to, t); }, Ease.InOutCubic);
        }

        /// <summary>The order is served: the products leave the tray, the carrots fly to the purse and any clover to the rabbits tab, the guest bows and goes.</summary>
        void OnDeliverOrder()
        {
            GameState s = Runner.State;
            if (s.Order == null) return;
            Vector2 from = Feel.PointOf(OrderCard.IsOpen ? OrderCard.DeliverRect : _guest);
            GuestPhase was = _guestPhase;
            _guestPhase = GuestPhase.Bowing; // so the refresh that follows does not send the guest off before the bow
            if (!Runner.DeliverOrder(out BigNum paid, out int clovers))
            {
                _guestPhase = was;
                return;
            }
            Sfx.Reward();
            Haptics.Light();
            OrderCard.Close();
            GameCatalog catalog = Runner.Catalog;
            string gained = "+" + paid.Short();
            Feel.Fly(from, Feel.PointOf(_purse), catalog.Currency, Color.white, 6, 44f, () =>
            {
                if (_purse == null) return;
                Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), gained, Palette.Lantern, 46);
                Tweens.Add(Motion.Tap, t => { if (_purse != null) _purse.localScale = Vector3.one * (1f + 0.08f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_purse != null) _purse.localScale = Vector3.one; });
            });
            if (clovers > 0)
            {
                // Clovers fly to the rabbits tab, where they are spent, like a quest's.
                RectTransform tab = _tabIcons[3].rectTransform;
                Feel.Fly(from, Feel.PointOf(tab), catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, clovers, 38f,
                    () => { if (tab != null) Feel.FloatText(Feel.PointOf(tab, new Vector2(0, 90)), "+" + clovers, Palette.Moss, 40); });
            }
            Feel.Moment(L10n.T("order.done.title"), clovers > 0 ? L10n.T("order.done.clover", paid.Short()) : L10n.T("order.done.sub", paid.Short()));
            // A bow from the feet, then the walk home.
            _bubble.gameObject.SetActive(false);
            RectTransform artRect = _guestArt;
            Tweens.Add(Motion.Time(0.7f), t => { if (artRect != null) artRect.localRotation = Quaternion.Euler(0, 0, -24f * Ease.Pop(t)); }, Ease.Linear, 0f, () =>
            {
                if (artRect != null) artRect.localRotation = Quaternion.identity;
                if (_guestPhase == GuestPhase.Bowing) _guestPhase = GuestPhase.Leaving;
            });
        }

        #endregion

        #region Strolls to empty spots of the lawn (#55)

        /// <summary>
        /// Empty spots of the lawn a resting rabbit may stroll to, placed by hand where today's garden has room: each keeps
        /// <see cref="Strolls.Clearance"/> from every station and 150 from the seats, the props and the decoration spots, and
        /// stays in from the wood at the edges. The first version of #55; the hex grid (#52) will offer its empty tiles instead.
        /// </summary>
        static readonly Vector2[] Lawn =
        {
            new Vector2(750f, 380f), new Vector2(310f, 500f), new Vector2(550f, 820f), new Vector2(250f, 1020f),
            new Vector2(770f, 1060f), new Vector2(750f, 1480f), new Vector2(370f, 1500f), new Vector2(690f, 1840f),
            new Vector2(390f, 1860f), new Vector2(590f, 2080f), new Vector2(230f, 2260f),
        };
        static readonly Vec2[] LawnCore = System.Array.ConvertAll(Lawn, v => new Vec2(v.x, v.y));
        public static IReadOnlyList<Vector2> LawnSpots => Lawn;
        /// <summary>How far from its seat a rabbit strolls: about two tiles.</summary>
        public const float StrollRadius = 520f;
        /// <summary>Seconds a stroll lasts: out, the rest at the spot, and home.</summary>
        const float StrollWindow = StrollPeriod * StrollShare;

        /// <summary>Everything that stands on the street, by the ground it stands on, so a walker can be drawn among it (see <see cref="SortWalker"/>).</summary>
        List<KeyValuePair<float, Transform>> _standing;

        /// <summary>Walker i is seat i's rabbit when it is out; one for every place to rest. Built behind everything that stands, and drawn among it once it is out.</summary>
        void BuildWalkers(GameCatalog catalog, Color shade, List<KeyValuePair<float, Transform>> standing)
        {
            _standing = standing;
            Sprite cup = catalog.Stations.Length > 0 && catalog.Stations[0] != null && catalog.Stations[0].Product != null ? catalog.Stations[0].Product : catalog.Round;
            for (int i = 0; i < Seats.Length && catalog.Rabbit != null; i++)
            {
                var rt = UiKit.Rect("Walker " + i, _street).Place(0f, 0f, 400, 400, 104, 110);
                rt.pivot = new Vector2(0.5f, 0f);
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 2, 68, 16).AddImage(catalog.Round, shade);
                Image art = UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white);
                art.preserveAspect = true;
                int walker = i;
                UiKit.Strokable(art.rectTransform, _scroll, () => StrokeSeat(walker), () => OnSeat(walker)); // #19: the walker is seat i's rabbit
                var w = new Walker { Rect = rt, Art = art.rectTransform, ArtImage = art, Phase = i * 900f };
                // A small cup in its paws, with a wisp of steam.
                w.Cup = UiKit.Rect("Cup", rt).Place(0.5f, 0f, 34, 14, 36, 36);
                Image cupImage = w.Cup.AddImage(cup, cup == catalog.Round ? Palette.Paper : Color.white);
                cupImage.preserveAspect = true;
                w.Steam = UiKit.Rect("Steam", w.Cup).Place(0.5f, 1f, 2, 10, 16, 16);
                w.SteamImage = w.Steam.AddImage(catalog.Glow, Palette.Alpha(Palette.Paper, 0.7f));
                // One firefly of its own to chase.
                w.Fly = UiKit.Rect("Firefly", rt).Place(0.5f, 0f, 0, 100, 60, 60);
                w.FlyImage = w.Fly.AddImage(catalog.Glow, Palette.Lantern);
                UiKit.Rect("Halo", w.Fly).Place(0.5f, 0.5f, 0, 0, 22, 22).AddImage(catalog.Round, Palette.Alpha(Palette.Lantern, 0.45f));
                UiKit.Rect("Core", w.Fly).Place(0.5f, 0.5f, 0, 0, 11, 11).AddImage(catalog.Round, Palette.Mix(Palette.Lantern, Color.white, 0.7f));
                // The bubble over its head: a note, a heart or a snore.
                w.Bubble = UiKit.Rect("Bubble", rt).Place(0.5f, 1f, 40, 14, 64, 40);
                w.Bubble.AddImage(catalog.Panel, Palette.Paper, true);
                w.BubbleText = UiKit.Rect("Text", w.Bubble).Stretch(0, 0, 1, 1, 4, 2, 4, 2).AddText("", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
                w.BubbleText.resizeTextForBestFit = true;
                w.BubbleText.resizeTextMinSize = 12;
                w.BubbleText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
                w.Cup.gameObject.SetActive(false); w.Fly.gameObject.SetActive(false); w.Bubble.gameObject.SetActive(false);
                _walkers.Add(w);
                rt.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Draws a walker among what stands on the street by where its feet are, so it is in front of the stump behind it and behind
        /// the table in front of it; while its feet are on a station's ground it goes behind everything, so it never covers a level plate or a name.
        /// </summary>
        void SortWalker(Walker w, Vector2 feet)
        {
            if (_standing == null || _standing.Count == 0) return;
            float key = feet.y;
            for (int k = 0; k < Spots.Length; k++)
            {
                Vector2 s = StationSpot(k);
                if (Mathf.Abs(s.x - feet.x) < 240f && feet.y > s.y - 280f && feet.y < s.y + 60f) { key = float.MaxValue; break; }
            }
            int target = -1;
            for (int k = 0; k < _standing.Count; k++)
            {
                if (_standing[k].Value == null || !(_standing[k].Key < key)) continue;
                target = _standing[k].Value.GetSiblingIndex();
                break;
            }
            if (target < 0)
            {
                Transform last = _standing[_standing.Count - 1].Value;
                if (last == null) return;
                target = last.GetSiblingIndex() + 1;
            }
            int now = w.Rect.GetSiblingIndex();
            if (now < target) target--;
            if (now != target) w.Rect.SetSiblingIndex(target);
        }

        /// <summary>Which stroll of seat <paramref name="seat"/>'s rabbit the clock is in or before: counts up once a period.</summary>
        static int StrollNumber(int seat, float clock) => Mathf.FloorToInt(clock / StrollPeriod + seat * 0.37f);

        /// <summary>Seconds into its stroll window at <paramref name="clock"/>; only meaningful while <see cref="Strolling"/>.</summary>
        static float StrollElapsed(int seat, float clock) => StrollPhase(seat, clock) * StrollPeriod;

        /// <summary>Seconds the walk out takes (and the walk home): what the window leaves around the rest at the spot.</summary>
        static float WalkSeconds(Outing o) => (StrollWindow - o.Linger) / 2f;

        /// <summary>True when the seat's rabbit is standing at its spot (not walking) right now.</summary>
        public bool AtSpot(int seat)
        {
            if (!Granted(seat)) return false;
            float e = StrollElapsed(seat, _clock), walk = WalkSeconds(_walkers[seat].Out);
            return e >= walk && e < walk + _walkers[seat].Out.Linger;
        }

        /// <summary>What the seat's rabbit is doing at its spot, or nothing while it is not there.</summary>
        public StrollActivity? ActivityOf(int seat) => AtSpot(seat) ? _walkers[seat].Out.Activity : (StrollActivity?)null;

        /// <summary>The lawn spot the seat's rabbit is out to (an index into <see cref="LawnSpots"/>), or -1 while it is home.</summary>
        public int StrollSpotOf(int seat) => Away(seat) ? _walkers[seat].Out.Spot : -1;

        /// <summary>How many resting rabbits are out on the lawn right now.</summary>
        public int StrollersOut
        {
            get
            {
                int n = 0;
                for (int i = 0; i < _walkers.Count; i++) if (Away(i)) n++;
                return n;
            }
        }

        /// <summary>The seat's rabbit was let out on the stroll the clock is in.</summary>
        bool Granted(int seat)
        {
            if (seat < 0 || seat >= _walkers.Count || seat >= _seated || !Strolling(seat, _clock)) return false;
            PlanStrolls();
            Outing o = _walkers[seat].Out;
            return o.Granted && o.Number == StrollNumber(seat, _clock);
        }

        /// <summary>The seat is empty because its rabbit is out: walking or at its spot; under reduced motion only while at the spot.</summary>
        bool Away(int seat) => Granted(seat) && (!Motion.Reduced || AtSpot(seat));

        /// <summary>
        /// Settles every stroll that begins at this moment, earliest first: a rabbit is let out while fewer than
        /// <see cref="Strolls.MaxAtOnce"/> are out and an empty spot is in reach that no station and no other stroller stands on.
        /// Decided once per stroll from the seat and the stroll's number, so the same stroll is the same at any frame rate.
        /// </summary>
        void PlanStrolls()
        {
            List<int> starting = null;
            for (int i = 0; i < _walkers.Count && i < _seated; i++)
            {
                if (!Strolling(i, _clock) || _walkers[i].Out.Number == StrollNumber(i, _clock)) continue;
                (starting ?? (starting = new List<int>())).Add(i);
            }
            if (starting == null) return;
            starting.Sort((a, b) => StrollElapsed(b, _clock).CompareTo(StrollElapsed(a, _clock)));
            bool night = Daylight.NightAt(ClockHours()) > 0.6;
            int cap = Strolls.MaxAtOnce(Lawn.Length);
            foreach (int i in starting)
            {
                Outing o = _walkers[i].Out;
                int n = StrollNumber(i, _clock);
                var blocked = new List<Vec2>(Spots.Length + _walkers.Count);
                for (int k = 0; k < Spots.Length; k++) { Vector2 at = StationSpot(k); blocked.Add(new Vec2(at.x, at.y)); }
                int outNow = 0;
                for (int j = 0; j < _walkers.Count && j < _seated; j++)
                {
                    Outing other = _walkers[j].Out;
                    if (j == i || !Strolling(j, _clock) || !other.Granted || other.Number != StrollNumber(j, _clock)) continue;
                    outNow++;
                    blocked.Add(LawnCore[other.Spot]);
                }
                Vector2 seat = i < _seats.Count ? _seats[i].anchoredPosition : Heart;
                o.Number = n;
                o.Linger = Strolls.LingerFor(Strolls.Roll(i, n, 2));
                o.Activity = Strolls.ActivityFor(Strolls.Roll(i, n, 1), night);
                o.Spot = outNow < cap ? Strolls.PickSpot(LawnCore, blocked, new Vec2(seat.x, seat.y), StrollRadius, Strolls.Roll(i, n, 0)) : -1;
                o.Granted = o.Spot >= 0;
            }
        }

        /// <summary>Lets a stroll be seen on demand (previews): seat <paramref name="seat"/>'s rabbit goes out on its next stroll to do
        /// <paramref name="activity"/>; returns the seconds until it is at its spot.</summary>
        public float StageStroll(int seat, StrollActivity activity)
        {
            if (seat < 0 || seat >= _walkers.Count) return 0f;
            Outing o = _walkers[seat].Out;
            o.Activity = activity;
            o.Linger = Strolls.LingerMost;
            // This stroll if it has not reached the spot yet; otherwise the next.
            int n = StrollNumber(seat, _clock);
            if (!Strolling(seat, _clock) || StrollElapsed(seat, _clock) > WalkSeconds(o)) n++;
            o.Number = n;
            Vector2 seatAt = seat < _seats.Count ? _seats[seat].anchoredPosition : Heart;
            var stations = new List<Vec2>();
            for (int k = 0; k < Spots.Length; k++) { Vector2 at = StationSpot(k); stations.Add(new Vec2(at.x, at.y)); }
            o.Spot = Strolls.PickSpot(LawnCore, stations, new Vec2(seatAt.x, seatAt.y), StrollRadius, 0d);
            o.Granted = o.Spot >= 0;
            float start = (n - seat * 0.37f) * StrollPeriod;
            return Mathf.Max(0f, start + WalkSeconds(o) + 0.5f - _clock);
        }

        /// <summary>Every walker: out of its seat to its spot, the small thing it does there, and home; the seat stands empty meanwhile.</summary>
        void UpdateStrollers()
        {
            for (int i = 0; i < _walkers.Count; i++)
            {
                Walker w = _walkers[i];
                bool away = Away(i);
                if (w.Rect.gameObject.activeSelf != away) w.Rect.gameObject.SetActive(away);
                if (i < _seats.Count && i < _seated && _seats[i].gameObject.activeSelf == away) _seats[i].gameObject.SetActive(!away);
                if (!away) continue;
                Outing o = w.Out;
                Vector2 seat = i < _seats.Count ? _seats[i].anchoredPosition : Heart;
                Vector2 spot = Lawn[o.Spot];
                float e = StrollElapsed(i, _clock), walk = WalkSeconds(o);
                bool there = e >= walk && e < walk + o.Linger;
                bool facingRight;
                Vector2 at;
                if (e < walk) { at = Vector2.Lerp(seat, spot, walk > 0f ? e / walk : 1f); facingRight = spot.x >= seat.x; }
                else if (there) { at = spot; facingRight = seat.x >= spot.x; }
                else { at = Vector2.Lerp(spot, seat, walk > 0f ? (e - walk - o.Linger) / walk : 1f); facingRight = seat.x >= spot.x; }
                float since = there ? e - walk : 0f; // seconds at the spot: every movement there is a fixed cycle of it
                StrollActivity doing = there ? o.Activity : (StrollActivity)(-1);
                bool tea = doing == StrollActivity.Tea, fly = doing == StrollActivity.Firefly, doze = doing == StrollActivity.Doze;
                if (w.Cup.gameObject.activeSelf != tea) w.Cup.gameObject.SetActive(tea);
                if (w.Fly.gameObject.activeSelf != fly) w.Fly.gameObject.SetActive(fly);
                if (w.Bubble.gameObject.activeSelf != there) w.Bubble.gameObject.SetActive(there);
                float hop = 0f;
                if (Motion.Reduced) { /* simply there */ }
                else if (!there) hop = Mathf.Abs(Mathf.Sin(_clock * 7f + w.Phase)) * 12f;
                else if (fly)
                {
                    // The firefly loops round its head; the rabbit turns after it with little hops.
                    float a = since * 2f;
                    w.Fly.anchoredPosition = new Vector2(Mathf.Cos(a) * 52f, 104f + Mathf.Sin(a * 1.3f) * 22f);
                    w.FlyImage.color = Palette.Alpha(Palette.Lantern, 0.85f + 0.15f * Mathf.Sin(since * 5f));
                    facingRight = Mathf.Cos(a) >= 0f;
                    hop = Mathf.Abs(Mathf.Sin(since * 9f)) * 10f;
                }
                else if (tea)
                {
                    // A wisp of steam rises and thins, over and over.
                    float k = Mathf.Repeat(since / 1.4f, 1f);
                    w.Steam.anchoredPosition = new Vector2(2f + Mathf.Sin(k * Mathf.PI * 2f) * 4f, 10f + k * 22f);
                    w.SteamImage.color = Palette.Alpha(Palette.Paper, 0.7f * (1f - k));
                }
                w.BubbleText.text = tea ? "♪" : (fly ? "♥" : "zZ");
                // Lying down for a doze, rolling a little; standing otherwise.
                w.Art.localRotation = doze ? Quaternion.Euler(0, 0, -74f + (Motion.Reduced ? 0f : Mathf.Sin(since * 1.1f) * 8f)) : Quaternion.identity;
                w.Art.anchoredPosition = doze ? new Vector2(0, -14f) : Vector2.zero;
                w.Rect.anchoredPosition = at + new Vector2(0, hop);
                SortWalker(w, at);
                float face = facingRight ? 1f : -1f;
                w.Rect.localScale = new Vector3(face, 1f, 1f);
                w.Bubble.localScale = new Vector3(face, 1f, 1f); // the words read the right way round whichever way it faces
                if (w.Fly.gameObject.activeSelf) w.Fly.localScale = Vector3.one * (0.8f + 0.3f * (0.5f + 0.5f * Mathf.Sin(since * 5f)));
            }
        }

        #endregion
    }
}

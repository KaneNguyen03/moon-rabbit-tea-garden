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
    public sealed partial class GardenScreen : MonoBehaviour
    {
        public GameRunner Runner;
        public Camera Cam;

        const float RefreshInterval = 1f / 10f;
        const int TabCount = 4;
        // The world is a clearing seen from above at an angle, bigger than the screen and dragged
        // around in every direction. Sizes are in reference pixels; the screen is portrait, 1080 × 1920.
        const float ScreenW = 1080f, ScreenH = 1920f;
        const float WorldW = 4800f, WorldH = 5400f;
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
        QuestChestsView _questChests;
        public QuestChestsView ChestRows => _questChests;
        public QuestPrizeView Prizes { get; private set; }
        MonthPassView _monthPass;
        public MonthPassView MonthPassRows => _monthPass;
        public MonthKindChooser KindChooser { get; private set; }
        readonly List<Image> _framePaper = new List<Image>();
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
        /// Where decoration spot <paramref name="i"/> stands in the world: the feet of whatever is placed there. Each of the eight
        /// spots has a tile of the grid (#52): two in the resting corner at the bottom, one between each pair of station clusters
        /// up the sides and the middle, and one by the tea shop near the pond; every one clear of the stations and the props.
        /// </summary>
        public Vector2 DecorSpotAt(int i) => CellToWorld(Grid != null && Grid.TryTileOf(GardenGrid.DecorId(i), out Hex tile) ? tile : GardenGrid.DefaultDecorTile(i));

        /// <summary>The tint a paper lantern's picture takes for a lantern colour: none for paper, a wash of the colour for the rest.</summary>
        public static Color LanternTint(int color)
        {
            if (color <= 0 || color >= Fairs.LanternIndex(Fairs.All.Count)) return Color.white;
            return Palette.Mix(Color.white, Palette.Hex(MonthPass.LanternHex(color)), 0.7f);
        }
        RectTransform _pondGlow;
        /// <summary>The wood around the clearing, on its own layer that slides a little slower than the ground.</summary>
        RectTransform _canopy;
        public const float CanopyDrift = 0.9f;
        RawImage _moonbeam;
        Image _moonPool;
        const float MoonbeamAlpha = 0.9f, MoonPoolAlpha = 0.4f;

        /// <summary>
        /// Where resting rabbits sit: each place has a tile of the grid (#52). The two stumps by the tea table come with the
        /// garden; then a picnic rug on the grass by the jam pot, mushroom stools, a hammock and a swing, one for each place added.
        /// </summary>
        public Vector2 RestSpot(int k) => CellToWorld(Grid != null && Grid.TryTileOf(GardenGrid.RestId(k), out Hex tile) ? tile : GardenGrid.DefaultRestTile(k));
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
        GameObject _cardBuyCarrot;
        RectTransform _cardDish;
        Text _cardDishLine, _cardDishRaise;
        Image _cardDishLeaf;
        ChunkyButton _cardDishButton;
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
        /// <summary>The moon pond at the top left of the middle of the strip, where the camera starts from.</summary>
        public static Vector2 PondSpot => Heart + new Vector2(-450f, 430f);
        /// <summary>Nothing that must be read stands further right than this: the canopy's crowns reach in from the edge.</summary>
        public const float ReadableRight = WorldW - 80f;
        /// <summary>What must be read keeps this far inside the screen's edges, in canvas units.</summary>
        public const float ReadMargin = 24f;
        /// <summary>How far either side of the teapot the opening view shows, at the usual zoom, less <see cref="ReadMargin"/>.</summary>
        const float OpeningReach = (ScreenW / 2f - ReadMargin) / ZoomDefault;
        /// <summary>A station's level plate is drawn at this size, whatever the station's own.</summary>
        const float PlateScale = 0.78f;
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
        Text _recipeLeaves;
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
            if (_onOfferDelivered != null) Runner.OfferDelivered -= _onOfferDelivered;
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
            FishingCard.Update(dt); // #58b
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
            _energyButton.FaceRect.localScale = Vector3.one * (_energyReady && !Motion.Reduced ? 1f + 0.025f * Mathf.Sin(_clock * 4f) : 1f);

            Tweens.Update(dt);
            SyncFronts(); // after the arranging mode's glides moved a station this frame
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
            UpdateChatter(dt);
            UpdateBoostEdge(boosting, dt);
            UpdateSounds(boosting, dt);

            // The counter counts up instead of snapping.
            double target = s.Coins.ToDouble();
            _shownCoins = System.Math.Abs(target - _shownCoins) < 0.5 || target > 1e15
                ? target
                : _shownCoins + (target - _shownCoins) * Mathf.Clamp01(dt * 10f);
            _coins.text = target > 1e15 ? s.Coins.Short() : new BigNum(_shownCoins).Short();

            _sinceRefresh += dt;
            if (_sinceRefresh >= RefreshInterval) Refresh(_sinceRefresh);
            UpdateLayout(dt);
            UpdateRain(dt); // last: over the lanterns' flicker and the seats' poses of this frame (#62)
        }

        /// <summary>The teapot stands here, in the middle of the strip; everything else is placed around it.</summary>
        static readonly Vector2 Heart = new Vector2(WorldW / 2f, WorldH / 2f);

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

        /// <summary>The hex grid the garden stands on (#52): the player's own, or none before the runner is set.</summary>
        GardenGrid Grid => Runner != null && Runner.State != null ? Runner.State.Grid : null;

        /// <summary>Where a tile of the grid lies in the world: a pointy-top lattice from the heart, where the teapot stands, stretched tall for the portrait garden.</summary>
        public static Vector2 CellToWorld(Hex tile) => Heart + new Vector2((float)GardenGrid.LocalX(tile.Q, tile.R), (float)GardenGrid.LocalY(tile.Q, tile.R));

        /// <summary>
        /// Position of station <paramref name="i"/> in the world: its tile on the grid, or the default table's tile for a
        /// station that has no plot yet (a locked station still needs a place for its sign and its plot of mist).
        /// </summary>
        public Vector2 StationSpot(int i)
        {
            string id = Runner != null && i >= 0 && i < Runner.Defs.Count ? Runner.Defs[i].GridId : null;
            return CellToWorld(Grid != null && Grid.TryTileOf(id, out Hex tile) ? tile : GardenGrid.DefaultStationTile(i));
        }

        /// <summary>Where a prop that comes with the garden stands: its tile on the grid, or the default table's.</summary>
        Vector2 PropSpot(string id) => CellToWorld(Grid != null && Grid.TryTileOf(id, out Hex tile) ? tile : GardenGrid.DefaultTile(id, null));
        /// <summary>Where the mortar for the mooncake game stands, and the tea shop: nothing else is placed on them.</summary>
        public Vector2 MortarSpot => PropSpot(GardenGrid.MortarId);
        public Vector2 ShopSpot => PropSpot(GardenGrid.ShopId);

        /// <summary>How large a station is drawn. The teapot is the centrepiece.</summary>
        public static float StationSize(int i) => i == 0 ? 1f : StationScale;

        /// <summary>How much of the screen the HUD covers at the top and the tab bar at the bottom, and how tall the card stands.</summary>
        const float WheelScroll = 60f;
        /// <summary>The strip at the top of the sheet: the page name, the buy modes, the close button.</summary>
        const float HeaderHeight = 148f;
        /// <summary>The station card stands right on the tab bar: the bar's gap and height, plus a thin seam.</summary>
        const float HudCover = 160f, TabCover = TabBarGap + TabBarHeight + 10f, CardHeight = 440f;

        public enum ZoomLevel { Far, Default, Near }

        /// <summary>
        /// Far shows the whole garden top to bottom; near is for looking at one rabbit. The usual view stands back 17 %
        /// (#50): the screen spans 1300 garden units, so three station columns and five or more plots are in view at once.
        /// </summary>
        public const float ZoomFar = ScreenH / FarSpan, ZoomDefault = 0.83f, ZoomNear = 1.5f;
        /// <summary>
        /// How much garden height the far view spans: more than the strip is tall, so the whole strip sits in the middle, clear
        /// of the HUD and the tab bar, with as much of its width as the zoom allows. <see cref="ZoomFar"/> is that far.
        /// </summary>
        const float FarSpan = 2970f;
        /// <summary>The far zoom for this garden: <see cref="ZoomFar"/>, which shows the strip's whole height between the HUD and the tab bar.</summary>
        public float FarZoom => ZoomFar;
        float _zoom = ZoomDefault;
        Text _zoomMark, _levelText;
        Image _levelRing;
        RectTransform _levelBadge;
        int _shownGardenLevel = -1;
        // #51: land not bought yet is a dashed ring on clear grass; the forest rim at the lawn's edge steps back as the land opens.
        readonly List<CanvasGroup> _landRings = new List<CanvasGroup>(), _landBlooms = new List<CanvasGroup>();
        readonly List<RimPiece> _rim = new List<RimPiece>();
        readonly List<RimPiece> _wood = new List<RimPiece>();
        int _woodSegments = -1;
        /// <summary>What the rim keeps clear of whatever land is open, and by how much: the props with a plate to read by 200, the pond, seats and decoration spots by a hair.</summary>
        readonly List<KeyValuePair<Rect, float>> _rimKeepOff = new List<KeyValuePair<Rect, float>>();
        int _landPlots = -1;
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
        public ZoomLevel Zoom => _zoom < (FarZoom + ZoomDefault) / 2f ? ZoomLevel.Far : (_zoom > (ZoomDefault + ZoomNear) / 2f ? ZoomLevel.Near : ZoomLevel.Default);

        float ScaleOf(ZoomLevel level) => level == ZoomLevel.Far ? FarZoom : (level == ZoomLevel.Near ? ZoomNear : ZoomDefault);

        /// <summary>Where a point of the garden is on the screen (canvas units from the bottom left).</summary>
        Vector2 ToView(Vector2 world) => world * _zoom + _street.anchoredPosition;

        /// <summary>
        /// Keeps the view inside the garden; when the garden is smaller than the screen it sits in the middle. The strip is
        /// shorter than the screen at the usual zoom (the horizontal garden), so it sits in the middle top to bottom, and rises
        /// no further than <paramref name="lift"/> while a sheet or the card stands over the foot of the screen.
        /// </summary>
        Vector2 ClampView(Vector2 pos, float zoom, float lift = 0f)
        {
            Vector2 span = OpenSpan(), rows = OpenRows();
            float w = (span.y - span.x) * zoom, h = (rows.y - rows.x) * zoom, middle = ScreenH / 2f - (rows.x + rows.y) / 2f * zoom, floor = -rows.x * zoom;
            return new Vector2(
                w <= ScreenW ? ScreenW / 2f - (span.x + span.y) / 2f * zoom : Mathf.Clamp(pos.x, ScreenW - span.y * zoom, -span.x * zoom),
                h <= ScreenH ? Mathf.Clamp(pos.y, middle, Mathf.Max(middle, floor + lift)) : Mathf.Clamp(pos.y, ScreenH - rows.y * zoom, floor + lift));
        }

        public const float ViewMarginY = 600f;

        public Vector2 OpenRows()
        {
            int open = OpenSegments;
            return new Vector2(Mathf.Max(0f, LawnEdgeY(open, GardenSide.Down) - ViewMarginY), Mathf.Min(WorldH, LawnEdgeY(open, GardenSide.Up) + ViewMarginY));
        }

        public static float LawnEdgeY(int segments, GardenSide side)
        {
            float half = (GardenGrid.Reach(segments, side == GardenSide.Down ? GardenSide.Down : GardenSide.Up) + 0.5f) * (float)GardenGrid.PitchY;
            return Heart.y + (side == GardenSide.Down ? -half : half);
        }

        /// <summary>How far past the open lawn's ends the view may go: a strip of mist and wood, so the end of the lawn reads as an edge.</summary>
        public const float ViewMargin = 360f;

        /// <summary>The garden x the view may show, left and right: the open lawn and <see cref="ViewMargin"/> past each end, inside the world.</summary>
        public Vector2 OpenSpan()
        {
            int open = OpenSegments;
            float left = LawnEdgeX(open, GardenSide.Left) - ViewMargin, right = LawnEdgeX(open, GardenSide.Right) + ViewMargin;
            if (NextLandLabel(out Rect label))
            {
                left = Mathf.Min(left, label.xMin - ReadMargin);
                right = Mathf.Max(right, label.xMax + ReadMargin);
            }
            return new Vector2(Mathf.Max(0f, left), Mathf.Min(WorldW, right));
        }

        bool NextLandLabel(out Rect label)
        {
            label = default;
            int next = Runner != null && Runner.State != null ? Runner.State.Plots : -1;
            if (_street == null || next < 0 || next >= _actors.Count || next >= _shownStations) return false;
            var corners = new Vector3[4];
            _actors[next].Plate.GetWorldCorners(corners);
            Vector2 a = _street.InverseTransformPoint(corners[0]), b = _street.InverseTransformPoint(corners[2]);
            label = Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
            return true;
        }

        /// <summary>How many segments of the strip are open beside the middle.</summary>
        int OpenSegments => Runner != null && Runner.State != null ? Expansion.Segments(Runner.State) : 0;

        /// <summary>A finger is dragging the garden right now (the scroll's own drag): the view is not pulled back while it does.</summary>
        bool _dragging;

        /// <summary>
        /// Lets go of the garden after a drag past the open lawn's ends, or a fling that carried it there: it springs back
        /// inside, as the scroll's elastic edge does at the world's ends. Only sideways; at once under reduced motion.
        /// </summary>
        void KeepInsideOpenLawn(float dt)
        {
            if (_street == null || _dragging || _pinching || _padPinching || LayoutMode) return;
            float lift = SheetOpen ? SheetLift : 0f;
            Vector2 at = _street.anchoredPosition, target = ClampView(at, _zoom, lift), off = target - at;
            bool outX = Mathf.Abs(off.x) >= 0.5f, outY = Mathf.Abs(off.y) >= 0.5f;
            if (!outX && !outY) return;
            if (_scroll != null) _scroll.velocity = new Vector2(outX ? 0f : _scroll.velocity.x, outY ? 0f : _scroll.velocity.y);
            float k = Motion.Reduced || dt <= 0f || off.magnitude < 1f ? 1f : Mathf.Clamp01(dt * 9f);
            _street.anchoredPosition = Vector2.Lerp(at, target, k);
        }

        /// <summary>A drag of the garden by a finger, for the tests: the view moves by <paramref name="delta"/> view units, then is let go.</summary>
        public void SimulateGardenDrag(Vector2 delta, bool release = true)
        {
            _dragging = true;
            _glideRun++;
            _street.anchoredPosition += delta;
            if (release) _dragging = false;
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
            _zoom = Mathf.Clamp(zoom, FarZoom, ZoomNear);
            _street.localScale = new Vector3(_zoom, _zoom, 1f);
            _street.anchoredPosition = ClampView(atView - world * _zoom, _zoom);
            if (_scroll != null)
            {
                _scroll.horizontal = WorldW * _zoom > ScreenW + 1f;
                _scroll.vertical = WorldH * _zoom > ScreenH + 1f; // the strip only scrolls up and down when it is taller than the screen (near)
            }
            if (Zoom != _shownZoom) ShowZoomDetail();
        }

        /// <summary>Far away the plates and names would be specks, so they go; up close the worn items show too.</summary>
        void ShowZoomDetail()
        {
            _shownZoom = Zoom;
            if (_zoomMark != null) _zoomMark.text = _shownZoom == ZoomLevel.Far ? "–" : (_shownZoom == ZoomLevel.Near ? "+" : "");
            for (int i = 0; i < _actors.Count; i++)
            {
                Transform plate = _actors[i].Plate;
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

        /// <summary>The zoom after one frame of trackpad pinch, kept between Far (<paramref name="far"/>, by default <see cref="ZoomFar"/>) and Near.</summary>
        public static float PinchZoom(float current, float magnification, float far = ZoomFar)
        {
            float factor = Mathf.Max(0.1f, 1f + magnification * PinchGain); // a wild negative sum must not flip the garden
            return Mathf.Clamp(current * factor, far, ZoomNear);
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
            ApplyZoom(PinchZoom(_zoom, magnification, FarZoom), viewPoint);
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
                _seats[k].localRotation = Motion.Reduced ? Quaternion.identity : Quaternion.Euler(0, 0, Mathf.Sin(_clock * 0.8f + k * 1.9f) * 3.2f);
                bool dozing = Mathf.Repeat(_clock / 11f + k * 0.29f, 1f) < 0.3f;
                if (_seatNaps[k].activeSelf != dozing) _seatNaps[k].SetActive(dozing);
            }

            UpdateStrollers();

            UpdateScenery();
            UpdateTravelers(dt);
            KeepInsideOpenLawn(dt);
            if (_canopy != null)
            {
                _canopy.localScale = _street.localScale;
                Vector2 rest = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - Heart * _zoom, _zoom);
                float drift = Motion.Reduced ? 1f : CanopyDrift;
                Vector2 ground = _street.anchoredPosition;
                _canopy.anchoredPosition = rest + (ground - rest) * drift;
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
                if (s.Levels[route.x] <= 0 || s.Levels[route.y] <= 0 || !_actors[route.x].Root.gameObject.activeSelf || !_actors[route.y].Root.gameObject.activeSelf) continue; // none to or from a stored station (#54)

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
                float k = _goldAge / GoldSeconds, across = Motion.Reduced ? 0.5f : k;
                _gold.anchoredPosition = new Vector2(-80f + across * (ScreenW + 160f), ScreenH * 0.55f + (Motion.Reduced ? 0f : Mathf.Sin(k * 9f) * 160f));
                _gold.localScale = Vector3.one * (Motion.Reduced ? 1f : 1f + 0.15f * Mathf.Sin(_clock * 6f));
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
            UpdatePhotoAsk(dt);
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
            BuildRain(catalog); // the rain over the garden and the Frog Rabbit (#62)
            BuildStationCard(catalog);
            BuildEvents(catalog);
            BuildHud(catalog);
            BuildTabBar(catalog);
            ProductTray = new TrayView(_canvas, catalog, OnSellProduct, OnSellAllProducts);
            BuildPhotoMode(catalog);
            BuildPhotoAsk(catalog);
            ProductTray.Root.anchoredPosition = new Vector2(0f, SafeBottom() + TabBarGap + TabBarHeight + 10f); // just above the tab bar
            BuildEdges(catalog);
            BuildSheet(catalog);
            BuildLayout(catalog);
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
                Runner.ItemDefs, OnBuyHat, OnBuyDecor, OnLanternColor, () => { Runner.Analytics.LogGoldenPacksOpened(AnalyticsEvents.FromShop); GoldenPacks.Open(Runner.Store); }, OnBuyMoonJar);
            DecorCard = new DecorView(_canvas, catalog, Tweens, OnPlaceDecor, OnStoreDecor);
            BuildOrders(catalog);
            Shell = new ShellView(_canvas, catalog, Tweens, () => Runner.ResetGame(), () => Profile.Open(Runner.Defs, Runner.State, Runner.Account, Runner.CloudStatus),
                () => NewMoon.CanBegin(Runner.State), () => NewMoon.LevelNeeded(Runner.State.Season), () => NewMoonCard.Open(Runner.Defs, Runner.State), () => NewMoon.AtCap(Runner.State),
                OpenMail, () => Runner.Letters.Count);
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
                }), OnRedeemCode, () => Runner.OpenStorePage());
            BuildMail(catalog);
            FirstWeek = new NewcomerView(_canvas, _hud, Runner, Tweens, OnClaimNewcomer);
            BuildWelcomeBack();
            BuildFair();
            BuildTrunk();
            Prizes = new QuestPrizeView(_canvas, catalog, Tweens);
            KindChooser = new MonthKindChooser(_canvas, catalog, Tweens);
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
            AlbumBanner.Attach(this); // a set of the album just completed, or a milestone's gift (#61)
            if (_onSyncChoice != null) Runner.SyncChoiceNeeded -= _onSyncChoice;
            _onSyncChoice = r => CloudChoice.Open(r);
            Runner.SyncChoiceNeeded += _onSyncChoice;
            if (Runner.Sync.Pending != null) CloudChoice.Open(Runner.Sync.Pending);
            Income = new IncomeView(_canvas, catalog, Tweens, OnOpenMoonJar);
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
            BuildPond(catalog); // the moon pond's card and the rabbit's Feed button (#58b)
            BuildAdSeams(catalog); // the three rewarded-ad places and the no-ads pass (#66)
            BuildOffers(catalog);
            ReviewCard = new ReviewView(_canvas, catalog, Tweens, OnReviewLiked, OnReviewFeedback);
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
            scroll.vertical = WorldH * _zoom > ScreenH + 1f;
            scroll.horizontal = true;
            scroll.inertia = true;
            scroll.decelerationRate = 0.135f;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            // The scroll's own drag, watched so the view is not pulled back inside the open lawn under the finger.
            var watch = view.gameObject.AddComponent<EventTrigger>();
            AddDrag(watch, EventTriggerType.BeginDrag, e => _dragging = true);
            AddDrag(watch, EventTriggerType.EndDrag, e => _dragging = false);
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

            // Land the garden has not bought yet is clear grass with a faint dashed ring where the station will stand (#51);
            // the forest rim at the lawn's edge is what is not opened. When the land is bought the ring goes and fresh grass
            // and flowers come up on it. Both lie flat on the ground, under everything that stands.
            _landRings.Clear(); _landBlooms.Clear(); _rim.Clear(); _wood.Clear(); _woodSegments = -1; _rimKeepOff.Clear(); _landPlots = -1;
            for (int i = 0; i < count; i++)
            {
                if (i < Plots.Starting) { _landRings.Add(null); _landBlooms.Add(null); continue; }
                _landBlooms.Add(BuildBloom(catalog, i));
                _landRings.Add(BuildRing(catalog, i));
            }
            BuildDecor(catalog, count, standing);
            BuildForestRing(catalog, count); // #53: the grass of every ring still to open, and the sign at the lawn's edge

            for (int i = 0; i < count; i++)
            {
                Vector2 spot = StationSpot(i);
                var actor = new StationActorView(i);
                actor.Build(_street, catalog, catalog.Stations[i], OnTap, OnPad, OnStroke, _scroll);
                actor.Root.Place(0f, 0f, spot.x, spot.y, 440, 360);
                actor.BaseScale = StationSize(i);
                actor.Root.localScale = Vector3.one * StationSize(i);
                actor.SyncFront();
                // Every station's level reads at the same size, clear of the rabbit's feet and the prop, and whole in the opening view.
                actor.PlacePlate(PlateScale / StationSize(i), new Vector2(PlateNudge(spot, StationSize(i)) / StationSize(i), -26f));
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
            for (int k = 0; k < RestSpots.Max; k++)
            {
                int seat = k;
                Vector2 at = RestSpot(k);
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
                standing.Add(new KeyValuePair<float, Transform>(RestSpot(k).y + 60f, cushion)); // the furniture is always behind whoever sits on it
                standing.Add(new KeyValuePair<float, Transform>(RestSpot(k).y, rt));
            }

            // Now and then a seated rabbit gets up and strolls to an empty spot of the lawn (#55).
            BuildWalkers(catalog, shade, standing);

            // The tea shop stands at the edge of the pond from the first day; a touch opens it.
            if (catalog.ShopProp != null)
            {
                Vector2 shopAt = ShopSpot;
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
                BuildTrunkProp(catalog, shopAt, shopWidth, shopHeight, standing);
            }
            // The mortar for the mooncake game stands near the teapot from garden level three; a touch starts a game.
            {
                Vector2 mortarAt = MortarSpot; // just above the teapot, a row under the grape vine; its plate hangs over it, clear of the teapot's name
                _mortarProp = UiKit.Rect("Mortar", _street).Place(0f, 0f, mortarAt.x, mortarAt.y + 80f, 170, 170);
                Image mortarImage = _mortarProp.AddImage(catalog.Mortar != null ? catalog.Mortar : catalog.Round, catalog.Mortar != null ? Color.white : Palette.Mix(Palette.Ink, Palette.Paper, 0.4f));
                mortarImage.preserveAspect = true;
                mortarImage.raycastTarget = true;
                var mortarButton = _mortarProp.gameObject.AddComponent<Button>();
                mortarButton.transition = Selectable.Transition.None;
                mortarButton.targetGraphic = mortarImage;
                mortarButton.onClick.AddListener(OpenPounding);
                var plate = UiKit.Rect("Plate", _mortarProp).Place(0.5f, 1f, -80, 26, 250, 44); // a little to the left: the "▲ N" pill at the top edge stays clear of it (#71)
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
                Vector2 basketAt = BasketSpot(leafStation); // #54 follow-up: by the leaf station's home tile, wherever the station is moved
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
            for (int n = 1; n <= _mist.Count; n++)
                if (GardenGrid.SideOf(n) == GardenSide.Up) standing.Add(new KeyValuePair<float, Transform>(LawnEdgeY(n - 1, GardenSide.Up) + 0.1f, _mist[n - 1].transform));
            standing.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Transform> item in standing) item.Value.SetAsLastSibling();
            // Every station's front (its level plate and "Xếp thỏ +" button) over everything that stands, so a decoration
            // on a neighbouring tile never covers them, wherever things are arranged (#82).
            foreach (StationActorView actor in _actors) actor.Front.SetAsLastSibling();
            // The mist over the segments still to open lies over everything on their land (the horizontal garden); the sign stands
            // in front of it, at the lawn's end.
            for (int n = 1; n <= _mist.Count; n++)
                if (GardenGrid.SideOf(n) != GardenSide.Up) _mist[n - 1].transform.SetAsLastSibling();
            foreach (RimPiece piece in _wood)
            {
                if (piece.Shade != null) piece.Shade.SetAsLastSibling();
                piece.Art.SetAsLastSibling();
            }
            // What must be read stays over the mist's thick edge (#16): the firefly jar, whose plate reaches to the lawn's left
            // end, and every station's front (its level plate) and the sign.
            if (_jarProp != null) _jarProp.SetAsLastSibling();
            foreach (StationActorView actor in _actors) actor.Front.SetAsLastSibling();
            if (_forestSign != null) _forestSign.SetAsLastSibling();

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

        /// <summary>
        /// How far, in garden units, a station's level plate hangs towards the teapot so it reads whole in the opening view (#16):
        /// only for a station that stands in that view with its plate reaching past the view's edge, as the land plate of the
        /// station on the first segment did ("Mở rộng vư…").
        /// </summary>
        float PlateNudge(Vector2 spot, float size)
        {
            float teapot = StationSpot(0).x, reach = OpeningReach - StationActorView.PlateW * PlateScale / 2f;
            if (Mathf.Abs(spot.x - teapot) > OpeningReach) return 0f;
            float middle = spot.x + StationActorView.PlateX * size - teapot;
            return Mathf.Clamp(middle, -reach, reach) - middle;
        }

        /// <summary>The ground a station takes up, plates included, in garden units.</summary>
        Rect StationBox(int i)
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
            _cardLevel = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            // "Cấp 500 ✦✦✦" is wider than the pill at full size: the words shrink to stay on one line (#73).
            _cardLevel.resizeTextForBestFit = true;
            _cardLevel.resizeTextMinSize = 14;
            _cardLevel.resizeTextMaxSize = _cardLevel.fontSize;
            _cardLevel.verticalOverflow = VerticalWrapMode.Truncate;
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
            var carrot = UiKit.Rect("Carrot", _cardBuy.FaceRect).Place(1f, 0.5f, -36, 0, 42, 42);
            carrot.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _cardBuyCarrot = carrot.gameObject;
            BuildCardDish(catalog);

            _card.gameObject.SetActive(false);
        }

        public static Color LeafGold => Palette.Mix(Palette.Lantern, Palette.Carrot, 0.25f);

        static Sprite LeafIcon(GameCatalog catalog) => catalog.TradeIcon(RabbitTrade.Gatherer) ?? catalog.Round;

        void BuildCardDish(GameCatalog catalog)
        {
            _cardDish = UiKit.Rect("Dish", _card).Stretch(0, 0, 1, 0, 32, 136, 32, -224);
            _cardDishLine = UiKit.Rect("Line", _cardDish).Stretch(0, 0, 1, 1, 0, 0, 500, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardDishLine.resizeTextForBestFit = true;
            _cardDishLine.resizeTextMinSize = 16;
            _cardDishLine.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            var raise = UiKit.Rect("Raise", _cardDish).Place(1f, 0.5f, -240, 0, 480, UiKit.MinTouch);
            _cardDishButton = UiKit.Chunky(raise, catalog.Panel, () => OnRaiseDish(Selected));
            _cardDishButton.Dip.Denied = () => Deny(_cardDishButton, false);
            _cardDishRaise = UiKit.Rect("Text", _cardDishButton.FaceRect).Stretch(0, 0, 1, 1, 24, 0, 72, 0)
                .AddText("", UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardDishRaise.resizeTextForBestFit = true;
            _cardDishRaise.resizeTextMinSize = 16;
            _cardDishRaise.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _cardDishLeaf = UiKit.Rect("Leaf", _cardDishButton.FaceRect).Place(1f, 0.5f, -38, 0, 40, 40).AddImage(LeafIcon(catalog), LeafGold);
            _cardDishLeaf.preserveAspect = true;
        }

        void RefreshCardDish(IReadOnlyList<StationDef> defs, GameState s, int i, bool open)
        {
            int dish = Recipes.CurrentDish(s, i), level = Recipes.Level(s, i), cost = Recipes.RaiseCost(s, i);
            _cardDishLine.text = L10n.T("dish.line", L10n.Dish(defs[i].Id, dish), level);
            bool can = open && Recipes.CanRaise(s, i);
            _cardDishRaise.text = !Recipes.Unlocked(s) ? L10n.T("dish.locked", Recipes.UnlockLevel) : !open ? L10n.T("dish.closed") : cost <= 0 ? L10n.T("dish.max") : L10n.T("dish.raise", cost);
            _cardDishRaise.color = ChunkyButton.InkFor(can);
            _cardDishButton.SetEnabled(can);
            if (can) _cardDishButton.Face.color = LeafGold;
            _cardDishLeaf.color = can ? Palette.Mix(LeafGold, Palette.Ink, 0.5f) : LeafGold;
        }

        void OnRaiseDish(int station)
        {
            if (station < 0 || !Runner.RaiseDish(station)) return;
            Sfx.Milestone();
            Haptics.Light();
            Vector2 at = Feel.PointOf(_cardDishLine.rectTransform);
            Feel.FloatText(at + new Vector2(0f, 60f), L10n.T("dish.raised", Recipes.Level(Runner.State, station)), LeafGold, 40);
            RectTransform line = _cardDishLine.rectTransform;
            Tweens.Add(Motion.Tap, t => { if (line != null) line.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (line != null) line.localScale = Vector3.one; });
            Refresh(-1f);
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

            RefreshCardDish(defs, s, i, open);

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
                bool previousOpen = Economy.CanUnlock(defs, s, i);
                can = previousOpen && s.Coins >= cost;
                _cardInfo.text = previousOpen ? StationRowView.Describe(defs, s, i, 1, m) : StationRowView.LockedReason(defs, s, i);
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
            bool shortOnly = !capped && !can && s.Coins < cost && (needsPlot ? Expansion.Missing(defs, s) == ExpandNeed.Carrots : (level > 0 || Economy.CanUnlock(defs, s, i)));
            if (shortOnly) _cardBuyTop.text = L10n.T("need.more");
            _cardBuyCost.text = capped ? "" : (shortOnly ? (cost - s.Coins).Short() : cost.Short());
            if (_cardBuyCarrot.activeSelf == capped) _cardBuyCarrot.SetActive(!capped); // at the cap nothing is paid, so no carrot (#74)
            Color ink = ChunkyButton.InkFor(can);
            _cardBuyTop.color = ink;
            _cardBuyCost.color = ink;
            _cardBuy.SetEnabled(can);
        }

        void BuildPhotoMode(GameCatalog catalog)
        {
            // A small plate at the foot of the screen while framing: snap, or done. It stands above the paper frame's
            // foot, shadow and all, so the garden's name and the date show in the preview as they will in the picture (#75).
            var bar = UiKit.Rect("PhotoBar", _canvas).Place(0.5f, 0f, 0, 240, 860, 130);
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
            _framePaper.Clear();
            _framePaper.Add(UiKit.Rect("Top", photoFrame).Stretch(0, 1, 1, 1, 0, -Edge, 0, 0).AddImage(null, paper));
            _framePaper.Add(UiKit.Rect("Left", photoFrame).Stretch(0, 0, 0, 1, 0, 0, -Edge, 0).AddImage(null, paper));
            _framePaper.Add(UiKit.Rect("Right", photoFrame).Stretch(1, 0, 1, 1, -Edge, 0, 0, 0).AddImage(null, paper));
            _framePaper.Add(UiKit.Rect("Bottom", photoFrame).Stretch(0, 0, 1, 0, 0, 0, 0, -Foot).AddImage(null, paper));
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
            // #56: the thanks of the rabbit that asked for this picture, and what it paid.
            _photoAskLine = UiKit.Rect("Ask", card).Stretch(0, 0, 1, 0, 40, 344, 40, -396)
                .AddText("", UiKit.TextBody, Palette.Mix(Palette.Carrot, Palette.Ink, 0.55f), TextAnchor.MiddleCenter, FontStyle.Bold);
            _photoAskLine.resizeTextForBestFit = true;
            _photoAskLine.resizeTextMinSize = 16;
            _photoAskLine.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _photoAskLine.gameObject.SetActive(false);
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
                int month = Runner.State.PhotoFrame;
                string frameHex = Fairs.FrameHex(month);
                Color paper = frameHex != null ? Palette.Hex(frameHex) : Palette.Paper;
                foreach (Image side in _framePaper) side.color = paper;
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
            _photoAskLine.gameObject.SetActive(false);
            _photoNote.text = path;
            CloseAlbum();
            _photoBar.SetActive(false);
            _photoReview.SetActive(true);
        }

        public void ExitPhotoMode()
        {
            if (!PhotoMode) return;
            PhotoMode = false;
            if (PhotoAskFraming) { PhotoAskFraming = false; Runner.DeclinePhotoAsk(); } // closed without the picture: that ask is gone (#56)
            _photoReview.SetActive(false);
            _photoAlbum.SetActive(false);
            _photoBar.SetActive(false);
            ApplyPhotoOptions();
            _hud.gameObject.SetActive(true);
            _tabBar.gameObject.SetActive(true);
            Refresh();
            CelebratePhotoAsk();
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
            bool asked = PhotoAskFraming && AnswerPhotoAsk();
            _photoAskLine.gameObject.SetActive(asked);
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
                float angle = (Motion.Reduced ? 0f : _clock * (0.35f + Hash(seed) * 0.4f)) + seed, reach = 30f + Hash(seed + 2f) * 50f;
                _fireflies[i].anchoredPosition = home + new Vector2(Mathf.Cos(angle) * reach, Mathf.Sin(angle * 1.3f) * reach * 0.6f);
                float pulse = Motion.Reduced ? 0.5f : 0.5f + 0.5f * Mathf.Sin(_clock * (1.3f + Hash(seed + 9f)) + seed);
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
        bool OnDecorSpot(float x, float y)
        {
            for (int i = 0; i < Decor.Spots; i++) if (Vector2.Distance(DecorSpotAt(i), new Vector2(x, y)) < 150f) return true;
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
            _lanternGlowColor = s.LanternColor > 0 ? Palette.Mix(Palette.Lantern, Palette.Hex(MonthPass.LanternHex(s.LanternColor)), 0.5f) : Palette.Lantern;
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
                // On the strip's lawn only (the horizontal garden): between its top and bottom rows, inside its last segments.
                float half = LawnEdgeX(Expansion.LastSegments(Runner.Defs), GardenSide.Right) - Heart.x - 60f;
                for (float x = Heart.x - half; x < Heart.x + half; x += 60f)
                    for (float y = Heart.y - 560f; y < Heart.y + 560f; y += 60f)
                    {
                        if (!OpenGrass(x, y, count)) continue;
                        float d = float.MaxValue;
                        for (int i = 0; i < count; i++) d = Mathf.Min(d, Vector2.Distance(new Vector2(x, y), StationSpot(i)));
                        for (int k = 0; k < RestSpots.Max; k++) d = Mathf.Min(d, Vector2.Distance(new Vector2(x, y), RestSpot(k)));
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

        /// <summary>Where the firefly jar stands, by the pond.</summary>
        static Vector2 JarSpot => PondSpot + new Vector2(-5f, -290f);

        /// <summary>
        /// The ground a station takes, sign, pad, rabbit box, name plate and prop included, at its drawn size: what the
        /// wood keeps <see cref="CanopyClearance"/> away from. The offsets are the station actor's, at their widest.
        /// </summary>
        Rect StationReach(int i)
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

        // ----- #51: the forest rim and the land not bought yet -----

        /// <summary>
        /// The forest rim at its nearest comes to the open lawn's end on each side (the horizontal garden): its pieces end where
        /// the grass ends, and step out with the lawn as segments open.
        /// </summary>
        static float RimHug(int side, int plots) => LawnEdgeX(GardenGrid.SegmentsForPlots(plots), side < 0 ? GardenSide.Left : GardenSide.Right);
        /// <summary>How long a piece of the rim takes to step back when new land opens: it fades out, and in again further off.</summary>
        public const float RimStep = 0.6f;

        /// <summary>One piece of the forest rim: a tree, a sapling, a clump of mushrooms, a bush, stones or a stone lantern.</summary>
        sealed class RimPiece
        {
            public RectTransform Art, Shade;
            public Image Image, ShadeImage;
            /// <summary>Where its foot stands up the lawn, its size, which side (-1 left, +1 right), and how far behind the edge line its row stands.</summary>
            public float Y, Width, Height, Depth, ShadeAlpha;
            public int Side;
            public GardenSide Band;
            public int Reach;
            public float X, Target = float.NaN;
            public int Run;
        }

        /// <summary>
        /// The plot a station takes, as a ring on the ground: the station's pad, centred where its floor is drawn.
        /// The same shape as <see cref="StationReach"/> at its widest, so whatever keeps off the reach keeps off the ring.
        /// </summary>
        Rect PlotRing(int i)
        {
            Vector2 spot = StationSpot(i);
            float s = StationSize(i);
            Vector2 centre = spot + StationActorView.FloorAt * s, half = StationActorView.FloorSize * s / 2f;
            return Rect.MinMaxRect(centre.x - half.x, centre.y - half.y * 0.7f, centre.x + half.x, centre.y + half.y * 0.7f);
        }

        /// <summary>A faint dashed ring on the grass where a station will stand once the land is bought.</summary>
        CanvasGroup BuildRing(GameCatalog catalog, int i)
        {
            Rect at = PlotRing(i);
            var ring = UiKit.Rect("Ring " + i, _street).Place(0f, 0f, at.center.x, at.center.y, at.width, at.height);
            CanvasGroup group = ring.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Color dash = Palette.Alpha(Palette.Paper, 0.42f);
            const int Dashes = 22;
            for (int k = 0; k < Dashes; k++)
            {
                float angle = k / (float)Dashes * Mathf.PI * 2f;
                Vector2 o = new Vector2(Mathf.Cos(angle) * at.width / 2f, Mathf.Sin(angle) * at.height / 2f);
                Vector2 along = new Vector2(-Mathf.Sin(angle) * at.width, Mathf.Cos(angle) * at.height);
                var d = UiKit.Rect("Dash", ring).Place(0.5f, 0.5f, o.x, o.y, 24f, 7f);
                d.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
                d.AddImage(catalog.Round, dash).raycastTarget = false;
            }
            return group;
        }

        /// <summary>Fresh grass and a few flowers on land just bought: hidden until then, and it stays.</summary>
        CanvasGroup BuildBloom(GameCatalog catalog, int i)
        {
            Rect at = PlotRing(i);
            var bloom = UiKit.Rect("Bloom " + i, _street).Place(0f, 0f, at.center.x, at.center.y, at.width, at.height);
            CanvasGroup group = bloom.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Color[] blades = { Palette.Alpha(Palette.Moss, 0.75f), Palette.Alpha(Palette.Mix(Palette.Moss, Palette.Paper, 0.3f), 0.7f), Palette.Alpha(Palette.Mix(Palette.Moss, Palette.Grass, 0.4f), 0.8f) };
            Color[] petals = { Palette.Blush, Palette.Lantern, Palette.Paper };
            for (int k = 0; k < 26; k++)
            {
                float seed = i * 7.3f + k * 1.9f;
                float angle = Hash(seed) * Mathf.PI * 2f, r = 0.78f + Hash(seed + 1f) * 0.32f;
                Vector2 o = new Vector2(Mathf.Cos(angle) * at.width / 2f * r, Mathf.Sin(angle) * at.height / 2f * r);
                bool flower = k % 3 == 0;
                float size = flower ? 12f + Hash(seed + 2f) * 8f : 26f + Hash(seed + 3f) * 16f;
                var piece = UiKit.Rect(flower ? "Petal" : "Grass", bloom).Place(0.5f, 0.5f, o.x, o.y, size, flower ? size : size * 0.4f);
                if (!flower) piece.localRotation = Quaternion.Euler(0, 0, 60f + Hash(seed + 4f) * 60f);
                piece.AddImage(catalog.Round, flower ? Palette.Alpha(petals[k % petals.Length], 0.9f) : blades[k % blades.Length]).raycastTarget = false;
            }
            bloom.gameObject.SetActive(false);
            return group;
        }

        /// <summary>
        /// Where a piece of the rim stands when the garden has <paramref name="plots"/> plots: as near the lawn's edge as it may,
        /// stepping out until it keeps <see cref="CanopyClearance"/> from every plot that is bought and from the props that must be
        /// read, and stands on no plot at all, bought or not.
        /// </summary>
        float RimX(RimPiece p, int plots)
        {
            float x = 0f;
            // The steps out are counted from the teapot, not from the lawn's end, so a piece never steps back in when the
            // lawn widens; and they go on to the end of the world, past the plots of every segment still to open.
            int first = Mathf.CeilToInt((Mathf.Abs(RimHug(p.Side, plots) - Heart.x)) / 20f);
            for (int n = first; n < first + (int)(WorldW / 20f); n++)
            {
                float edge = Heart.x + p.Side * (p.Depth + n * 20f);
                x = edge + p.Side * p.Width / 2f;
                if (RimClear(Rect.MinMaxRect(x - p.Width / 2f, p.Y, x + p.Width / 2f, p.Y + p.Height), plots)) return x;
            }
            return x;
        }

        bool RimClear(Rect piece, int plots)
        {
            for (int i = 0; i < _landRings.Count; i++)
                if (Apart(piece, StationReach(i)) < (i < plots ? CanopyClearance + 4f : 4f)) return false;
            foreach (KeyValuePair<Rect, float> keep in _rimKeepOff)
                if (Apart(piece, keep.Key) < keep.Value) return false;
            return true;
        }

        /// <summary>Stands a piece of the rim at <paramref name="x"/>, with its shadow, as clear as <paramref name="alpha"/>.</summary>
        static void PlaceRim(RimPiece p, float x, float alpha)
        {
            p.X = x;
            if (p.Art == null) return;
            p.Art.anchoredPosition = new Vector2(x, p.Y);
            p.Image.color = Palette.Alpha(p.Image.color, alpha);
            if (p.Shade != null)
            {
                p.Shade.anchoredPosition = new Vector2(x + 6f, p.Y + 4f);
                p.ShadeImage.color = Palette.Alpha(p.ShadeImage.color, p.ShadeAlpha * alpha);
            }
        }

        /// <summary>Open grass: off every station's prop, plates and name, off the seats, the pond, the mortar and the shop.</summary>
        bool OpenGrass(float x, float y, int stations)
        {
            if (x < 90f || x > WorldW - 90f || y < 160f || y > WorldH - 220f) return false;
            for (int i = 0; i < stations; i++)
            {
                Vector2 spot = StationSpot(i);
                if (Mathf.Abs(spot.x - x) < 200f && y > spot.y - 230f && y < spot.y + 230f) return false;
            }
            for (int k = 0; k < RestSpots.Max; k++)
                if (Vector2.Distance(RestSpot(k), new Vector2(x, y)) < 150f) return false;
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
                Vector2 basketAt = BasketSpot(leafStation);
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

            // Places to sit, in the corners the stations leave free: the tea table between the two stumps, each at the foot of its seat.
            Vector2 stumpA = RestSpot(0), stumpB = RestSpot(1), table = (stumpA + stumpB) / 2f + new Vector2(-3f, -31f);
            Put(TeaTable, table.x, table.y, 170f, Color.white);
            Put(Stump, stumpA.x, stumpA.y - 38f, 110f, Color.white);
            Put(Stump, stumpB.x, stumpB.y - 34f, 96f, Color.white);

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

            // The forest rim (#51): along both sides of the lawn, a row of trees and saplings just behind the edge and, at the edge
            // itself, the low things of a wood's border. Each spot takes one of the templates by a fixed hash, so the rim never
            // repeats and is the same every time. Beside land not yet bought the rim comes right up to the grass; beside land
            // that is bought it stands back as far as the wood does (RefreshLand moves it).
            _rimKeepOff.Clear();
            for (int k = stations; k < keepOff.Count; k++) _rimKeepOff.Add(new KeyValuePair<Rect, float>(keepOff[k], CanopyClearance + 4f));
            _rimKeepOff.Add(new KeyValuePair<Rect, float>(Rect.MinMaxRect(pondAt.x - 200f, pondAt.y - 110f, pondAt.x + 210f, pondAt.y + 120f), 4f));
            for (int k = 0; k < RestSpots.Max; k++) { Vector2 seat = RestSpot(k); _rimKeepOff.Add(new KeyValuePair<Rect, float>(new Rect(seat.x - 80f, seat.y - 20f, 160f, 160f), 4f)); }
            for (int k = 0; k < Decor.Spots; k++) { Vector2 spot = DecorSpotAt(k); _rimKeepOff.Add(new KeyValuePair<Rect, float>(new Rect(spot.x - 70f, spot.y, 140f, 160f), 4f)); }
            // The rim stands on the ground with the lawn whose end it marks (the horizontal garden): it does not drift with the far wood.
            Transform rimParent = _street;
            List<KeyValuePair<float, Transform>> rimInto = standing;
            const int RimTree = 0, RimSapling = 1, RimMushrooms = 2, RimBush = 3, RimStones = 4, RimLantern = 5;
            Sprite RimSprite(int template)
            {
                if (template == RimLantern && catalog.StoneLantern == null) template = RimBush;
                return template == RimLantern ? catalog.StoneLantern : d[template == RimTree || template == RimSapling ? Tree : template == RimMushrooms ? Mushrooms : template == RimBush ? Bush : Stones];
            }
            float[] rimLow = { 380f, 230f, 92f, 130f, 74f, 150f / DecorScale }, rimHigh = { 460f, 280f, 110f, 150f, 86f, 150f / DecorScale };
            float RimHeight(int template, float seed) => Mathf.Lerp(rimLow[template], rimHigh[template], Hash(seed)) * DecorScale;
            void AddRim(int template, int side, float y, float depth, float seed)
            {
                RimPiece piece = MakeRim(template, side, y, depth, seed, "Rim ");
                if (piece == null) return;
                PlaceRim(piece, RimX(piece, Plots.Starting), 1f);
                _rim.Add(piece);
            }
            RimPiece MakeRim(int template, int side, float y, float depth, float seed, string prefix)
            {
                Sprite sprite = RimSprite(template);
                if (template == RimLantern && catalog.StoneLantern == null) template = RimBush;
                if (sprite == null) return null;
                string[] names = { "Tree", "Sapling", "Mushrooms", "Bush", "Stones", "Lantern" };
                float height = RimHeight(template, seed);
                float width = height * sprite.rect.width / sprite.rect.height;
                var piece = new RimPiece { Y = y, Width = width, Height = height, Depth = depth, Side = side };
                if (template != RimStones)
                {
                    float sw = Mathf.Clamp(height * 0.55f, 50f, 260f);
                    piece.Shade = UiKit.Rect("Rim Shade", rimParent).Place(0f, 0f, 0f, 0f, sw, sw * 0.34f);
                    piece.ShadeAlpha = 0.28f;
                    piece.ShadeImage = piece.Shade.AddImage(catalog.Glow, Palette.Alpha(Palette.Night, piece.ShadeAlpha));
                    piece.ShadeImage.raycastTarget = false;
                    rimInto.Add(new KeyValuePair<float, Transform>(y + 0.25f, piece.Shade));
                }
                // Mirrored on one side in two, so neighbours of the same kind differ.
                piece.Image = AddDecor(rimParent, sprite, 0f, y, height, template == RimLantern ? Color.white : soft);
                piece.Image.raycastTarget = false;
                piece.Art = piece.Image.rectTransform;
                piece.Art.name = prefix + names[template];
                piece.Art.pivot = new Vector2(0.5f, 0f);
                if (Hash(seed + 0.5f) < 0.5f) piece.Art.localScale = new Vector3(-1f, 1f, 1f);
                if (template == RimTree || template == RimSapling) _swayTrees.Add(piece.Art);
                rimInto.Add(new KeyValuePair<float, Transform>(y, piece.Art));
                return piece;
            }
            void AddWood(int template, GardenSide side, int reach, int shownUntil, float x, float gap, float seed)
            {
                Sprite sprite = RimSprite(template);
                if (sprite == null) return;
                float height = RimHeight(template == RimLantern && catalog.StoneLantern == null ? RimBush : template, seed), width = height * sprite.rect.width / sprite.rect.height;
                float edge = LawnEdgeY(shownUntil, side), outward = side == GardenSide.Up ? 1f : -1f;
                int plots = Expansion.PlotsForSegments(Runner.Defs, shownUntil);
                float y = side == GardenSide.Up ? edge + gap : edge - gap - height;
                int step = 0;
                while (!RimClear(new Rect(x - width / 2f, y, width, height), plots))
                {
                    if (++step > 20) return;
                    y += outward * 20f;
                }
                RimPiece piece = MakeRim(template, 0, y, 0f, seed, "Wood ");
                if (piece == null) return;
                piece.Band = side;
                piece.Reach = reach;
                PlaceRim(piece, x, 1f);
                piece.Art.gameObject.SetActive(false);
                if (piece.Shade != null) piece.Shade.gameObject.SetActive(false);
                _wood.Add(piece);
            }
            for (int side = -1; side <= 1; side += 2)
            {
                int lastEdge = -1;
                for (float y = 140f; y < WorldH - 340f; y += 115f)
                {
                    float seed = y * 0.37f + side * 11f;
                    // Behind: a tree or a sapling.
                    AddRim(Hash(seed + 2f) < 0.6f ? RimTree : RimSapling, side, y + 50f + Hash(seed + 1f) * 30f, 50f + Hash(seed + 3f) * 50f, seed + 4f);
                    // At the edge: something low, so the grass reads where it ends. A lantern now and then, never two running.
                    float pick = Hash(seed + 5f);
                    int edge = pick < 0.3f ? RimSapling : pick < 0.56f ? RimMushrooms : pick < 0.8f ? RimBush : pick < 0.93f ? RimStones : RimLantern;
                    if (edge == lastEdge) edge = edge == RimMushrooms ? RimBush : RimMushrooms;
                    AddRim(edge, side, y + Hash(seed + 6f) * 30f, Hash(seed + 7f) * 16f, seed + 8f);
                    lastEdge = edge;
                }
            }

            int lastSegments = Expansion.LastSegments(Runner.Defs);
            float woodFrom = LawnEdgeX(lastSegments, GardenSide.Left) + 40f, woodTo = LawnEdgeX(lastSegments, GardenSide.Right) - 40f;
            foreach (GardenSide band in new[] { GardenSide.Up, GardenSide.Down })
            {
                int reach = GardenGrid.Reach(lastSegments, band);
                for (float x = woodFrom; x <= woodTo; x += 125f)
                {
                    float seed = x * 0.31f + (int)band * 7f + reach * 3f;
                    if (band == GardenSide.Up)
                        AddWood(Hash(seed + 2f) < 0.6f ? RimTree : RimSapling, band, reach, lastSegments, x + (Hash(seed + 1f) - 0.5f) * 40f, 50f + Hash(seed + 3f) * 50f, seed + 4f);
                    float pick = Hash(seed + 5f);
                    int low = pick < 0.4f ? RimBush : pick < 0.75f ? RimMushrooms : RimStones;
                    AddWood(low, band, reach, lastSegments, x + 60f, Hash(seed + 7f) * 16f, seed + 8f);
                }
            }

            // Around every station, the same small set, placed where it fits: a planter on one side, mushrooms by the
            // front, stones at the back. Each tries a few spots in order and takes the first that is clear.
            bool Clear(float x, float y, int own, float halfWidth)
            {
                if (x < 70f + halfWidth || x > WorldW - 70f - halfWidth || y < 160f || y > WorldH - 220f) return false;
                if (Vector2.Distance(new Vector2(x, y), pondAt) < 250f) return false;
                if (Vector2.Distance(new Vector2(x, y), MortarSpot) < 170f || Vector2.Distance(new Vector2(x, y), ShopSpot) < 220f) return false;
                if (OnDecorSpot(x, y)) return false; // the player's own spots stay clear for what they buy (#11)
                for (int k = 0; k < RestSpots.Max; k++)
                {
                    Vector2 seat = RestSpot(k);
                    if (Mathf.Abs(seat.x - x) < 170f && y > seat.y - 170f && y < seat.y + 200f) return false;
                }
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
                // The rings start at 220, so a prop's picture (lifted half its height) still reads as beside the station, not on it.
                foreach (float r in new[] { 220f, 260f, 300f })
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
                for (int k = 0; k < RestSpots.Max; k++)
                {
                    Vector2 seat = RestSpot(k);
                    if (Mathf.Abs(seat.x - x) < 190f && y > seat.y - 150f && y < seat.y + 190f) onSeat = true;
                }
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
            BuildJarChip(catalog); // the moon jars on the shelf, beside the golden carrots (#64)
            BuildLeafChip(catalog);
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
                var rt = UiKit.Rect("Open " + TabNames[i], _tabBar).Stretch(TabX(i), 0, TabX(i + 1), 1); // right of the arranging button (#54)
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
            _barBadgeRect = UiKit.Rect("Badge", _tabBar).Place(TabX(2.5f), 0f, 46, 140, 48, 48);
            _barBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _barBadgeText = UiKit.Rect("Text", _barBadgeRect).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _barBadge = _barBadgeRect.gameObject;
            // How many open stations still have nobody working, on the corner of the rabbits tab.
            _rabbitBadgeRect = UiKit.Rect("RabbitsBadge", _tabBar).Place(TabX(3.5f), 0f, 46, 140, 48, 48);
            _rabbitBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _rabbitBadgeText = UiKit.Rect("Text", _rabbitBadgeRect).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _rabbitBadgeRect.gameObject.SetActive(false);
            // How many stations could be opened right now, on the corner of the stations tab.
            _stationBadgeRect = UiKit.Rect("StationsBadge", _tabBar).Place(TabX(0.5f), 0f, 46, 140, 48, 48);
            _stationBadgeRect.AddImage(catalog.Round, Palette.CarrotLip);
            _stationBadgeText = UiKit.Rect("Text", _stationBadgeRect).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _stationBadgeRect.gameObject.SetActive(false);
            BuildLayoutButton(catalog);
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
            BuildStoredRows(stations, catalog);

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
            _questChests = new QuestChestsView(quests, catalog, Tweens, OnOpenQuestBox, OnClaimWeekChest);
            _monthPass = new MonthPassView(quests, catalog, Tweens, OnMonthCell, OnBuyMonthPass);
            _monthPass.Changed += () => Refresh(-1f);
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
            _recipeLeaves = AddNote(quests, L10n.T("recipes.leaves", 0));
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

        /// <summary>The ×2 on the welcome-back sheet: the time away it shows paid once more, then the sheet closes.</summary>
        void OnDoubleAway()
        {
            BigNum paid = AwayCard.Report.Paid;
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
                float fromZoom = _zoom, toZoom = Mathf.Clamp((float)opening.ViewZoom, FarZoom, ZoomNear);
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
            _questChests.Refresh(Runner.QuestDefs, s, Runner.TodayNumber, dt);
            _monthPass.Refresh(s, Runner.TodayNumber, Runner.Store.PriceOf(MonthPass.ProductId));
            _questToday.text = L10n.T("quests.today.refresh", Span(SecondsToNextDay(System.DateTimeOffset.Now)));
            OrderQuestRows();
            RefreshPlotRow(defs, s);
            _rabbitList.Refresh(defs, s);
            Detail.Refresh(defs, s);
            Picker.Refresh(s);
            Workers.Refresh(defs, s);
            Profile.Refresh(defs, s, Runner.Account, Runner.CloudStatus);
            AskForReview();
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
            RefreshPond(); // #58b
            Rabbit avatar = VuonTra.Core.Profile.Avatar(s);
            Shell.SetProfile(ProfileView.NameOf(s), s.Profile.Code, avatar != null ? Runner.Catalog.RabbitArt(avatar) : Runner.Catalog.Rabbit);

            int claimable = Quests.ClaimableCount(Runner.QuestDefs, defs, s) + (_questChests.BoxReady ? 1 : 0) + QuestChests.ReadyCount(s, Runner.TodayNumber) + MonthPass.ReadyCount(s, Runner.TodayNumber);
            _barBadge.SetActive(claimable > 0);
            _barBadgeText.text = claimable.ToString();
            if (_shownClaimable >= 0 && claimable > _shownClaimable)
                Tweens.Add(0.32f, t => _badgePop = 0.4f * Ease.Pop(t), Ease.Linear, 0f, () => _badgePop = 0f); // one more to collect
            _shownClaimable = claimable;
            // The rabbits tab counts what waits there: stations with nobody, merges that can be made, kinds not yet looked at.
            int waiting = Rabbits.StationsWithoutWorker(defs, s) + s.FreshKinds.Count;
            for (int tier = 0; tier < Rabbits.TierCount; tier++) if (Rabbits.CanMerge(s, tier)) waiting++;
            if (_rabbitBadgeRect.gameObject.activeSelf != (waiting > 0)) _rabbitBadgeRect.gameObject.SetActive(waiting > 0);
            _rabbitBadgeText.text = waiting.ToString();
            // The stations tab counts the stations that could be opened for what is in the purse.
            int openable = 0;
            for (int i = 0; i < defs.Count; i++)
                if (s.Levels[i] <= 0 && Economy.CanUnlock(defs, s, i) && s.Coins >= Economy.CostOfNext(defs[i], 0, 1)) openable++;
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
            RefreshWelcomeBack();
            RefreshFair();
            RefreshTrunk();
            RefreshOffers();
            RefreshDecor(s);
            RefreshOrders(s);
            FirstWeek?.RefreshPill(_orderBadge.activeSelf);
            FirstWeek?.Refresh();
            bool anyGolden = s.GoldenCarrots > 0;
            if (_goldenChip.gameObject.activeSelf != anyGolden) _goldenChip.gameObject.SetActive(anyGolden);
            _goldenText.text = s.GoldenCarrots.ToString();
            RefreshJarChip(s);
            RefreshLeafChip(s);
            bool shopNews = Runner.GiftWaiting || Runner.VisitorUnseen; // a gift to take, or a visitor not yet looked at
            if (_shopBadge != null && _shopBadge.activeSelf != shopNews) _shopBadge.SetActive(shopNews);
            ProductTray.Refresh(s);
            // The tray lives in the plain garden view; a sheet or a station card covers its place.
            bool trayShown = Shell.State == ShellState.Playing && !SheetOpen && Selected < 0 && !PhotoMode && (s.Tray.Count > 0 || Tray.IsFull(s));
            if (ProductTray.Root.gameObject.activeSelf != trayShown) ProductTray.Root.gameObject.SetActive(trayShown);
            RefreshAdSeams();
            RefreshLayout(s);
            SyncFronts();
        }

        /// <summary>Each station's front follows its station: place, size and whether it is shown (#82).</summary>
        void SyncFronts()
        {
            for (int i = 0; i < _actors.Count; i++) _actors[i].SyncFront();
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
            RefreshLand(s);
            RefreshForestRing(Runner.Defs, s); // #53

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
                    _actors[i].SetSign(L10n.ExpandSign(defs, s), Expansion.CanExpand(defs, s));
                }
                else if (look == StationLook.Unbuilt)
                {
                    // The price sign: what it costs to build, or how many carrots are still missing for the next one.
                    BigNum cost = Economy.CostOfNext(defs[i], 0, 1);
                    bool next = Economy.CanUnlock(defs, s, i), can = next && s.Coins >= cost;
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

        /// <summary>
        /// Land not bought is a dashed ring on clear grass, with the forest rim right up to the lawn beside it (#51). When the
        /// garden widens the rings of the new plots go, grass and flowers come up on them and the rim beside them steps back:
        /// its pieces fade out and in again further off. Under reduced motion, and when the screen first shows, all of it is
        /// simply where it belongs.
        /// </summary>
        void RefreshLand(GameState s)
        {
            int plots = s.Plots;
            if (plots == _landPlots) return;
            bool growing = _landPlots >= 0 && plots > _landPlots && !Motion.Reduced;
            for (int i = 0; i < _landRings.Count; i++)
            {
                CanvasGroup ring = _landRings[i], bloom = _landBlooms[i];
                if (ring == null) continue;
                if (i >= plots)
                {
                    ring.gameObject.SetActive(true);
                    ring.alpha = 1f;
                    bloom.gameObject.SetActive(false);
                    continue;
                }
                bloom.gameObject.SetActive(true);
                if (!growing || i < _landPlots || !ring.gameObject.activeSelf)
                {
                    ring.gameObject.SetActive(false);
                    bloom.alpha = 1f;
                    bloom.transform.localScale = Vector3.one;
                    continue;
                }
                Tweens.Add(RimStep / 2f, t => { if (ring != null) ring.alpha = 1f - t; }, null, 0f,
                    () => { if (ring != null) { ring.gameObject.SetActive(false); ring.alpha = 1f; } });
                bloom.alpha = 0f;
                bloom.transform.localScale = Vector3.one * 0.6f;
                Tweens.Add(RimStep, t =>
                {
                    if (bloom == null) return;
                    bloom.alpha = Mathf.Clamp01(t);
                    bloom.transform.localScale = Vector3.one * Mathf.LerpUnclamped(0.6f, 1f, t);
                }, Ease.OutBack, RimStep / 3f);
            }
            foreach (RimPiece p in _rim)
            {
                float to = RimX(p, plots);
                if (to == p.Target) continue;
                p.Target = to;
                int run = ++p.Run;
                float from = p.X;
                if (!growing || Mathf.Abs(to - from) < 0.5f) { PlaceRim(p, to, 1f); continue; }
                Tweens.Add(RimStep, t =>
                {
                    if (p.Run != run || p.Art == null) return;
                    if (t < 0.5f) PlaceRim(p, from, 1f - 2f * t);
                    else PlaceRim(p, to, 2f * t - 1f);
                }, t => t);
            }
            RefreshWood(Expansion.Segments(s), growing);
            _landPlots = plots;
        }

        void RefreshWood(int segments, bool growing)
        {
            if (segments == _woodSegments) return;
            float from = LawnEdgeX(segments, GardenSide.Left), to = LawnEdgeX(segments, GardenSide.Right);
            foreach (RimPiece p in _wood)
            {
                bool show = p.Reach == GardenGrid.Reach(segments, p.Band) && p.X >= from && p.X <= to;
                float want = show ? 1f : 0f;
                if (p.Target == want) continue;
                p.Target = want;
                RimPiece piece = p;
                int run = ++piece.Run;
                if (show) ShowWood(piece, true);
                if (!growing || _woodSegments < 0)
                {
                    PlaceRim(piece, piece.X, 1f);
                    if (!show) ShowWood(piece, false);
                    continue;
                }
                float delay = show ? RimStep / 2f : 0f;
                PlaceRim(piece, piece.X, show ? 0f : 1f);
                Tweens.Add(RimStep / 2f, t =>
                {
                    if (piece.Run == run && piece.Art != null) PlaceRim(piece, piece.X, show ? t : 1f - t);
                }, null, delay, () =>
                {
                    if (piece.Run != run || piece.Art == null) return;
                    PlaceRim(piece, piece.X, 1f);
                    if (!show) ShowWood(piece, false);
                });
            }
            _woodSegments = segments;
        }

        static void ShowWood(RimPiece p, bool on)
        {
            p.Art.gameObject.SetActive(on);
            if (p.Shade != null) p.Shade.gameObject.SetActive(on);
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
            if (level <= 0) return Economy.CanUnlock(defs, s, i) && s.Coins >= Economy.CostOfNext(defs[i], 0, 1);
            int count = Simulation.BuyCount(defs, s, i);
            return count > 0 && s.Coins >= Economy.CostOfNext(defs[i], level, count);
        }

        /// <summary>
        /// What the next widening of the garden asks for, one condition after another, each in green once it
        /// is met and in rust while it is not: "Cấp vườn 3 · 3 bé thỏ (đang 1) · 650K cà rốt".
        /// </summary>
        static string ExpansionNeeds(IReadOnlyList<StationDef> defs, GameState s)
        {
            MistSegment wave = Expansion.Next(defs, s);
            if (wave == null) return "";
            string Mark(bool met, string text) => "<color=#" + (met ? "2E7D4F" : "B4532A") + ">" + text + "</color>";
            int level = Garden.Level(s);
            var parts = new List<string>();
            parts.Add(Mark(level >= wave.NeedLevel, L10n.T("expand.level", wave.NeedLevel) + (level >= wave.NeedLevel ? "" : L10n.T("expand.have", level))));
            if (wave.NeedRabbits > 0)
                parts.Add(Mark(s.Rabbits.Count >= wave.NeedRabbits, L10n.T("expand.rabbits", wave.NeedRabbits) + (s.Rabbits.Count >= wave.NeedRabbits ? "" : L10n.T("expand.have", s.Rabbits.Count))));
            if (wave.NeedKinds > 0)
                parts.Add(Mark(s.SeenKinds.Count >= wave.NeedKinds, L10n.T("expand.kinds", wave.NeedKinds) + (s.SeenKinds.Count >= wave.NeedKinds ? "" : L10n.T("expand.have", s.SeenKinds.Count))));
            if (wave.NeedDays > 0) parts.Add(Mark(Expansion.HasDays(wave, s), L10n.T("expand.days", wave.NeedDays) + (Expansion.HasDays(wave, s) ? "" : L10n.T("expand.have", Seasons.DaysPlayed(s)))));
            parts.Add(Mark(Expansion.HasCarrots(defs, s), L10n.T("expand.carrots", Expansion.CostOfNext(defs, s).Short())));
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
            _plotTitle.text = L10n.T("garden.segment", Expansion.Segments(s) + 1); // #53, a segment of the strip since the horizontal garden
            _plotSub.text = ExpansionNeeds(defs, s);
            _plotCost.text = cost.Short();
            _plotCost.color = ChunkyButton.InkFor(can);
            _plotBuy.SetEnabled(can);
        }

        /// <summary>
        /// Coming back (#69): five minutes away or more opens the welcome-back sheet; a shorter absence just sends
        /// the carrots up to the purse and leaves a sheet that is up, and its ×2, as they are (#89).
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
            // On the strip most of what waits out of view waits to the side (the horizontal garden): a pill at each side's middle.
            _edgeLeft = EdgePill(catalog, "EdgeLeft", SideEdgeY, () => FlyTo(_edgeLeftTarget), out _edgeLeftText);
            _edgeLeft.anchorMin = _edgeLeft.anchorMax = new Vector2(0f, 0f);
            _edgeLeft.sizeDelta = new Vector2(120f, 56f);
            _edgeLeft.anchoredPosition = new Vector2(70f, SideEdgeY);
            _edgeRight = EdgePill(catalog, "EdgeRight", SideEdgeY, () => FlyTo(_edgeRightTarget), out _edgeRightText);
            _edgeRight.anchorMin = _edgeRight.anchorMax = new Vector2(1f, 0f);
            _edgeRight.sizeDelta = new Vector2(120f, 56f);
            _edgeRight.anchoredPosition = new Vector2(-70f, SideEdgeY);

            // The way home: a paper pill at the bottom right, just over the tray, with the teapot and the words for where it goes (#16).
            var home = UiKit.Rect("Home", _canvas).Place(1f, 0f, -(ReadMargin + HomeW / 2f), bottom + 32f, HomeW, HomeH);
            home.AddShadow(14f, 10f, 0.3f);
            Image homeBack = home.AddImage(catalog.Panel, Palette.Paper, true);
            homeBack.raycastTarget = true;
            var homeButton = home.gameObject.AddComponent<Button>();
            homeButton.transition = Selectable.Transition.None;
            homeButton.targetGraphic = homeBack;
            homeButton.onClick.AddListener(GlideHome);
            Sprite teapot = catalog.Stations.Length > 0 ? catalog.Stations[0].Prop : null;
            if (teapot != null)
            {
                Image icon = UiKit.Rect("Icon", home).Place(0f, 0.5f, 48f, 0f, 68f, 68f).AddImage(teapot, Color.white);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }
            Text homeText = UiKit.Rect("Text", home).Stretch(0, 0, 1, 1, 90, 8, 16, 8)
                .AddText(L10n.T("edge.home"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            homeText.raycastTarget = false;
            homeText.horizontalOverflow = HorizontalWrapMode.Wrap;
            homeText.resizeTextForBestFit = true; // "Back to the tea shop" takes two lines, and at large text a smaller size
            homeText.resizeTextMinSize = 18;
            homeText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
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

        /// <summary>The home button's size: wide enough for its teapot and "Về tiệm trà" or "Back to the tea shop" on two lines.</summary>
        const float HomeW = 270f, HomeH = 92f;

        /// <summary>Where the home button looks: the tea shop above the teapot, the middle of the strip.</summary>
        public Vector2 HomeSpot => Heart + new Vector2(0f, 120f);

        /// <summary>The home button: the garden glides back to the tea shop and the teapot at the zoom it has (the usual one from far away).</summary>
        public void GlideHome()
        {
            Sfx.Click();
            if (Zoom == ZoomLevel.Far) SetZoom(ZoomLevel.Default, false);
            GlideTo(HomeSpot);
            if (Motion.Reduced) _street.anchoredPosition = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - HomeSpot * _zoom, _zoom);
        }

        void FlyTo(int index)
        {
            if (index < 0 || index >= _actors.Count) return;
            Sfx.Click();
            ZoomToStation(index, !Motion.Reduced);
        }

        /// <summary>The side pills stand at this height of the screen: the middle of the strip at the usual zoom.</summary>
        const float SideEdgeY = ScreenH * 0.5f;
        RectTransform _edgeLeft, _edgeRight;
        Text _edgeLeftText, _edgeRightText;
        int _edgeLeftTarget = -1, _edgeRightTarget = -1;

        /// <summary>Counts, per edge, the stations out of view that wait for the player, and keeps the nearest as the target.</summary>
        void UpdateEdges()
        {
            GameState s = Runner.State;
            bool shown = Shell.State == ShellState.Playing && !SheetOpen;
            int up = 0, down = 0, left = 0, right = 0;
            float upNear = float.MaxValue, downNear = float.MaxValue, leftNear = float.MaxValue, rightNear = float.MaxValue;
            _edgeUpTarget = -1;
            _edgeDownTarget = -1;
            _edgeLeftTarget = -1;
            _edgeRightTarget = -1;
            if (shown)
            {
                for (int i = 0; i < _actors.Count && i < s.Plots; i++)
                {
                    if (!_actors[i].Root.gameObject.activeSelf) continue;
                    if (!_actors[i].CanUpgradeShown && !_actors[i].Dozing) continue;
                    // Where the station sits on the screen, from the view's own numbers: no layout pass needed.
                    Vector2 at = ViewPointOf(i);
                    float y = at.y - ScreenH / 2f, x = at.x - ScreenW / 2f;
                    if (Mathf.Abs(y) >= ScreenH / 2f + 160f)
                    {
                        if (y > 0f) { up++; if (y < upNear) { upNear = y; _edgeUpTarget = i; } }
                        else { down++; if (-y < downNear) { downNear = -y; _edgeDownTarget = i; } }
                    }
                    else if (Mathf.Abs(x) >= ScreenW / 2f + 120f && Zoom != ZoomLevel.Far) // from far away the strip is an overview: no side pills over it
                    {
                        // Off to a side (the horizontal garden): counted by the side pills.
                        if (x > 0f) { right++; if (x < rightNear) { rightNear = x; _edgeRightTarget = i; } }
                        else { left++; if (-x < leftNear) { leftNear = -x; _edgeLeftTarget = i; } }
                    }
                }
            }
            if (_edgeLeft != null && _edgeLeft.gameObject.activeSelf != (left > 0)) _edgeLeft.gameObject.SetActive(left > 0);
            if (_edgeRight != null && _edgeRight.gameObject.activeSelf != (right > 0)) _edgeRight.gameObject.SetActive(right > 0);
            if (left > 0) _edgeLeftText.text = "◀ " + left;
            if (right > 0) _edgeRightText.text = right + " ▶";
            if (_edgeUp.gameObject.activeSelf != (up > 0)) _edgeUp.gameObject.SetActive(up > 0);
            if (_edgeDown.gameObject.activeSelf != (down > 0)) _edgeDown.gameObject.SetActive(down > 0);
            if (up > 0) _edgeUpText.text = "▲ " + up;
            if (down > 0) _edgeDownText.text = "▼ " + down;
            _edgeDown.anchoredPosition = new Vector2(DownPillShift(), _edgeDown.anchoredPosition.y);
            // The way home shows once the teapot is out of view above, below or, on the strip, to either side.
            Vector2 pot = _actors.Count > 0 ? ViewPointOf(0) : default;
            bool far = shown && _actors.Count > 0 && (Mathf.Abs(pot.y - ScreenH / 2f) > ScreenH / 2f + 160f || Mathf.Abs(pot.x - ScreenW / 2f) > ScreenW / 2f + 160f);
            if (_home.activeSelf != far) _home.SetActive(far);
        }

        float DownPillShift()
        {
            if (_shopProp == null || !_shopProp.gameObject.activeInHierarchy) return 0f;
            Vector2 size = _edgeDown.sizeDelta, shopSize = _shopProp.sizeDelta * _zoom;
            var shop = new Rect(ToView(_shopProp.anchoredPosition) - shopSize / 2f, shopSize);
            var middle = new Rect(new Vector2(ScreenW / 2f, _edgeDown.anchoredPosition.y) - size / 2f, size);
            if (!middle.Overlaps(shop)) return 0f;
            float low = ReadMargin + size.x / 2f, high = (_home.activeSelf ? ScreenW - ReadMargin - HomeW - PillGap : ScreenW - ReadMargin) - size.x / 2f;
            float left = shop.xMin - PillGap - size.x / 2f, right = shop.xMax + PillGap + size.x / 2f;
            bool leftFits = left >= low, rightFits = right <= high;
            if (leftFits && (!rightFits || ScreenW / 2f - left <= right - ScreenW / 2f)) return left - ScreenW / 2f;
            return rightFits ? right - ScreenW / 2f : 0f;
        }

        const float PillGap = 24f;

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
            Feel.Moment(L10n.T("moment.segment.title"), L10n.T("moment.plot.sub", string.Join(", ", names))); // #53
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
            int gift = Runner.ClaimDailyGift(out Shop.BigGift big);
            if (gift <= 0) return;
            // Day 7 of the first week (#57): a moment of its own, saying what came beside the golden carrots.
            if (big.Given)
            {
                Sfx.Sting();
                string what = big.Hat != null
                    ? L10n.T("shop.gift.big.hat", L10n.ItemName(big.Hat), big.Clovers)
                    : L10n.T("shop.gift.big.golden", L10n.T("item." + VuonTra.Core.Shop.BigGiftHat), big.Golden, big.Clovers);
                Feel.Moment(L10n.T("shop.gift.big", gift), what);
            }
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

        public ReviewView ReviewCard { get; private set; }

        public bool ReviewBusy => Shell.State != ShellState.Playing || Pound.IsOpen || Basket.IsOpen || Jar.IsOpen || FishingCard.IsOpen
            || Arrival.IsOpen || Feel.BannerVisible || Profile.IsOpen || Rename.IsOpen || Shop.IsOpen || GoldenPacks.IsOpen || NewMoonCard.IsOpen
            || NewMoonMoment.IsOpen || CloudChoice.IsOpen || ReviewCard.IsOpen || OfferCard.IsOpen || FirstWeek.IsOpen || Prizes.IsOpen || KindChooser.IsOpen || WelcomeCard.IsOpen || FairCard.IsOpen || TrunkCard.IsOpen;

        void AskForReview()
        {
            if (Runner.Review.Waiting == ReviewMoment.None || ReviewCard == null) return;
            if (Runner.TakeReviewMoment(ReviewBusy) != ReviewMoment.None) ReviewCard.Open(Runner.State.Profile.Code);
        }

        void OnReviewLiked()
        {
            Sfx.Reward();
            Runner.LikeReview();
            Feel.FloatText(new Vector2(0, 420), L10n.T("review.thanks"), Palette.Lantern, 44);
        }

        void OnReviewFeedback(string typed)
        {
            string mail = Runner.AnswerReviewFeedback(typed);
            if (mail == null) return;
            ProfileView.OpenUrl(mail);
            Feel.FloatText(new Vector2(0, 420), L10n.T("review.thanks"), Palette.Lantern, 44);
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
            DecorateGate();
            Arrival.Show(Runner.Catalog.RabbitArt(rabbit), rabbit.Name, L10n.T("detail.kind", L10n.Kind(rabbit), L10n.Rarity(rabbit.Tier)),
                Palette.Tiers[rabbit.Tier], rabbit.FirstOfKind, L10n.T("summon.new"), L10n.T("summon.hint"), rabbit.Tier == 0);
            Arrival.Dye(RabbitGridView.Fur(rabbit));
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
            Vector2 jarsFrom = Feel.PointOf(row != null ? row.JarChip : _purse);
            // A moon jar comes in a clover's place while the day's free jar can still come (#64), read before the claim gives it.
            int clovers = Quests.CloversNow(Runner.QuestDefs[index], Runner.State), jars = Quests.JarsNow(Runner.QuestDefs[index], Runner.State);
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
            if (jars > 0) JarArrived(jarsFrom);
            // Clovers fly to the rabbits tab, where they are spent.
            if (clovers > 0)
            {
                Feel.Fly(cloversFrom, Feel.PointOf(_tabIcons[3].rectTransform), catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, clovers, 38f,
                    () => Tweens.Add(0.22f, t => _tabPop[3] = 0.14f * Ease.Pop(t), Ease.Linear, 0f, () => _tabPop[3] = 0f));
            }
            Item gift = Runner.LastQuestItem;
            if (gift != null)
                Feel.Moment(L10n.T("moment.gift.title", L10n.ItemName(gift)), L10n.T("moment.gift.sub", L10n.Rarity(gift.Tier), ItemPickerView.Describe(gift)));
        }

        void OnOpenQuestBox()
        {
            GameState s = Runner.State;
            if (!_questChests.BoxReady)
            {
                Sfx.Click();
                _questChests.Pop(-1);
                int left = QuestChests.DailyTotal(Runner.QuestDefs, s) - QuestChests.DailyDone(Runner.QuestDefs, s);
                string why = QuestChests.BoxOpened(s) ? L10n.T("quests.box.opened") : L10n.T("quests.box.locked", left);
                Feel.FloatText(Feel.PointOf(_questChests.Box, new Vector2(-160, 70)), why, Palette.Ink, 32);
                return;
            }
            BoxPrize[] prizes = Runner.OpenQuestBox();
            if (prizes == null) return;
            Sfx.Reward();
            Haptics.Light();
            _questChests.Pop(-1);
            var tiles = new List<QuestPrizeView.Tile>();
            foreach (BoxPrize prize in prizes) tiles.Add(PrizeTile(prize.Kind, prize.Amount, prize.Coins));
            Prizes.Open(L10n.T("quests.box.open.title"), L10n.T("quests.box.open.sub", QuestChests.BoxPoints), tiles, Color.white);
            Refresh(-1f);
        }

        void OnClaimWeekChest(int chest)
        {
            GameState s = Runner.State;
            long today = Runner.TodayNumber;
            WeekChest def = QuestChests.Chests[chest];
            if (!QuestChests.ChestReady(s, chest, today))
            {
                Sfx.Click();
                _questChests.Pop(chest);
                bool opened = QuestChests.Claimed(s, chest) && QuestChests.WeekOf(today) == s.WeekIndex;
                string why = opened ? L10n.T("quests.chest.claimed") : L10n.T("quests.chest.need", def.Points - QuestChests.Points(s, today), MailView.GiftOf(def));
                Feel.FloatText(Feel.PointOf(_questChests.Chest(chest), new Vector2(0, 90)), why, Palette.Ink, 30);
                return;
            }
            if (!Runner.ClaimWeekChest(chest, out Rabbit rabbit)) return;
            Sfx.Reward();
            Haptics.Medium();
            _questChests.Pop(chest);
            var tiles = new List<QuestPrizeView.Tile>();
            if (rabbit != null)
                tiles.Add(new QuestPrizeView.Tile { Icon = Runner.Catalog.RabbitArt(rabbit), Tint = Color.white, Amount = rabbit.Name, Name = L10n.Rarity(rabbit.Tier) });
            if (def.Jars > 0) tiles.Add(PrizeTile(BoxPrizeKind.Jar, def.Jars, BigNum.Zero));
            if (def.Clovers > 0) tiles.Add(PrizeTile(BoxPrizeKind.Clover, def.Clovers, BigNum.Zero));
            if (def.Golden > 0) tiles.Add(PrizeTile(BoxPrizeKind.Golden, def.Golden, BigNum.Zero));
            Prizes.Open(L10n.T("quests.chest.title", def.Points), L10n.T("quests.chest.sub"), tiles, QuestChestsView.ChestTints[chest]);
            Refresh(-1f);
        }

        void OnMonthCell(MonthRow row, int tier)
        {
            GameState s = Runner.State;
            long today = Runner.TodayNumber;
            long month = MonthPass.MonthOf(today);
            int cal = MonthPass.CalendarMonth(month);
            MonthGift gift = MonthPass.GiftOf(row, tier);
            Vector2 at = Feel.PointOf(_monthPass.CellOf(row, tier), new Vector2(0, 90));
            if (MonthPass.Claimed(s, row, tier) && gift.Kind == MonthGiftKind.Lantern)
            {
                bool hung = MonthPass.LanternMonth(s.LanternColor) == cal;
                if (hung ? Runner.PaperLanterns() : Runner.HangMonthLantern(cal))
                {
                    Sfx.Click();
                    _monthPass.Pop(row, tier);
                    Feel.FloatText(at, hung ? L10n.T("monthpass.unhung") : L10n.T("monthpass.hung", MonthPassView.MonthName(month)), Palette.Ink, 30);
                    Refresh(-1f);
                }
                return;
            }
            if (MonthPass.Claimed(s, row, tier) && gift.Kind == MonthGiftKind.Frame)
            {
                bool worn = s.PhotoFrame == cal;
                if (Runner.WearMonthFrame(worn ? MonthPass.PlainFrame : cal))
                {
                    Sfx.Click();
                    _monthPass.Pop(row, tier);
                    Feel.FloatText(at, worn ? L10n.T("monthpass.unframed") : L10n.T("monthpass.framed", MonthPassView.MonthName(month)), Palette.Ink, 30);
                    Refresh(-1f);
                }
                return;
            }
            if (!MonthPass.Ready(s, row, tier, today))
            {
                Sfx.Click();
                _monthPass.Pop(row, tier);
                string why = MonthPass.Claimed(s, row, tier) ? L10n.T("quest.claimed")
                    : tier > MonthPass.Tier(s, today) ? L10n.T("monthpass.need", tier * MonthPass.PointsPerTier - MonthPass.Points(s, today), tier)
                    : L10n.T("monthpass.locked");
                Feel.FloatText(at, why, Palette.Ink, 30);
                return;
            }
            if (gift.Kind == MonthGiftKind.Rabbit)
            {
                Sfx.Click();
                KindChooser.Open(month, kind => ClaimMonth(row, tier, kind));
                return;
            }
            ClaimMonth(row, tier, null);
        }

        void ClaimMonth(MonthRow row, int tier, string kind)
        {
            long month = MonthPass.MonthOf(Runner.TodayNumber);
            if (!Runner.ClaimMonthTier(row, tier, kind, out MonthClaim claim)) return;
            Sfx.Reward();
            Haptics.Light();
            _monthPass.Pop(row, tier);
            var tiles = new List<QuestPrizeView.Tile> { MonthTile(claim, month) };
            if (claim.Gift.Golden > 0) tiles.Add(PrizeTile(BoxPrizeKind.Golden, claim.Gift.Golden, BigNum.Zero));
            Prizes.Open(L10n.T("monthpass.claimed.title", tier), L10n.T(row == MonthRow.Free ? "monthpass.claimed.free" : "monthpass.claimed.premium", MonthPassView.MonthName(month)),
                tiles, row == MonthRow.Free ? Color.white : Palette.Mix(Palette.Lantern, Color.white, 0.3f));
            Refresh(-1f);
        }

        QuestPrizeView.Tile MonthTile(MonthClaim claim, long month)
        {
            GameCatalog catalog = Runner.Catalog;
            int cal = MonthPass.CalendarMonth(month);
            switch (claim.Gift.Kind)
            {
                case MonthGiftKind.Income: return PrizeTile(BoxPrizeKind.Income, claim.Gift.Amount, claim.Coins);
                case MonthGiftKind.Clover: return PrizeTile(BoxPrizeKind.Clover, claim.Gift.Amount, BigNum.Zero);
                case MonthGiftKind.Jar: return claim.Jars > 0 ? PrizeTile(BoxPrizeKind.Jar, claim.Jars, BigNum.Zero) : PrizeTile(BoxPrizeKind.Clover, claim.JarClovers, BigNum.Zero);
                case MonthGiftKind.Golden: return PrizeTile(BoxPrizeKind.Golden, claim.Gift.Amount, BigNum.Zero);
                case MonthGiftKind.Lantern:
                    return new QuestPrizeView.Tile { Icon = catalog.Round, Tint = Palette.Hex(MonthPass.LanternColors[cal]), Amount = L10n.T("monthpass.cell.lantern"), Name = L10n.T("monthpass.lantern", MonthPassView.MonthName(month)) };
                case MonthGiftKind.Frame:
                    return new QuestPrizeView.Tile { Icon = catalog.Panel, Tint = Palette.Hex(MonthPass.FrameColors[cal]), Amount = L10n.T("monthpass.cell.frame"), Name = L10n.T("monthpass.frame", MonthPassView.MonthName(month)) };
                default:
                    Rabbit rabbit = claim.Rabbit;
                    return new QuestPrizeView.Tile { Icon = rabbit != null ? catalog.RabbitArt(rabbit) : catalog.RabbitFor(MonthPass.RabbitRarity), Tint = Color.white, Amount = rabbit != null ? rabbit.Name : "", Name = L10n.Rarity(MonthPass.RabbitRarity) };
            }
        }

        void OnBuyMonthPass()
        {
            GameState s = Runner.State;
            long today = Runner.TodayNumber;
            int passed = Mathf.Min(MonthPass.Tier(s, today), MonthPass.RabbitTier - 1);
            string name = MonthPassView.MonthName(MonthPass.MonthOf(today));
            Runner.BuyMonthPass(result =>
            {
                switch (result.Outcome)
                {
                    case PurchaseOutcome.Paid:
                        Sfx.Reward();
                        Haptics.Medium();
                        Feel.Moment(L10n.T("monthpass.bought.title", name), passed > 0 ? L10n.T("monthpass.bought.sub", passed) : L10n.T("monthpass.bought.none"));
                        break;
                    case PurchaseOutcome.Cancelled:
                        Feel.FloatText(Feel.PointOf(_monthPass.BuyButton, new Vector2(0, 90)), L10n.T("packs.cancelled"), Palette.Ink, 30);
                        break;
                    case PurchaseOutcome.Duplicate:
                        Feel.FloatText(Feel.PointOf(_monthPass.BuyButton, new Vector2(0, 90)), L10n.T("packs.duplicate"), Palette.Ink, 30);
                        break;
                    case PurchaseOutcome.Unavailable:
                        Feel.FloatText(Feel.PointOf(_monthPass.BuyButton, new Vector2(0, 90)), L10n.T("packs.notready"), Palette.Ink, 30);
                        break;
                    default:
                        Feel.FloatText(Feel.PointOf(_monthPass.BuyButton, new Vector2(0, 90)), L10n.T("packs.failed"), Palette.Ink, 30);
                        break;
                }
                Refresh(-1f);
            });
        }

        QuestPrizeView.Tile PrizeTile(BoxPrizeKind kind, int amount, BigNum coins)
        {
            GameCatalog catalog = Runner.Catalog;
            switch (kind)
            {
                case BoxPrizeKind.Income:
                    return new QuestPrizeView.Tile { Icon = catalog.Currency, Tint = Color.white, Amount = "+" + coins.Short(), Name = L10n.T("quests.prize.income", Mathf.RoundToInt(amount / 60f)) };
                case BoxPrizeKind.Clover:
                    return new QuestPrizeView.Tile { Icon = catalog.Clover != null ? catalog.Clover : catalog.Round, Tint = catalog.Clover != null ? Color.white : Palette.Moss, Amount = "+" + amount, Name = L10n.T("quests.prize.clovers", amount) };
                case BoxPrizeKind.Jar:
                    return new QuestPrizeView.Tile { Jar = true, Tint = Color.white, Amount = "+" + amount, Name = L10n.T("quests.prize.jars", amount) };
                default:
                    return new QuestPrizeView.Tile { Icon = catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Tint = catalog.GoldenCarrot != null ? Color.white : Palette.Lantern, Amount = "+" + amount, Name = L10n.T("quests.prize.golden", amount) };
            }
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
            var order = new List<Transform> { _questChests.TodayRow, _questChests.WeekRow };
            bool any = false;
            foreach (QuestRowView row in _questRows) any |= row.Visible && row.CanClaim;
            if (_questReady.gameObject.activeSelf != any) _questReady.gameObject.SetActive(any);
            order.Add(_questReady);
            foreach (QuestRowView row in _questRows) if (row.CanClaim) order.Add(row.Root);
            order.Add(_monthPass.Root);
            order.Add(_clovers.rectTransform);
            order.Add(_recipeLeaves.rectTransform);
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
            if (pet.TenHearts) TenHeartMoment(r, at); // #58b
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
        /// <summary>Where a guest stands to order: in front of the tea shop, at the counter's open side; the counter's tile of the grid (#52).</summary>
        public Vector2 GuestSpot => PropSpot(GardenGrid.CounterId);
        /// <summary>
        /// Where a guest comes in from and goes back to: along the top of the lawn from the right, the side away from the pond
        /// (the horizontal garden), as far from the counter as the column garden's left edge was, but never past the open lawn's
        /// end: a guest steps out of the mist there and walks only over land that is open (#16).
        /// </summary>
        public Vector2 GuestEntry => new Vector2(Mathf.Min(GuestSpot.x + 855f, LawnEdgeX(OpenSegments, GardenSide.Right) - GuestEdgeGap), GuestSpot.y - 40f);
        /// <summary>How far inside the open lawn's end a guest comes in.</summary>
        const float GuestEdgeGap = 60f;

        /// <summary>Where a walking guest is at <paramref name="t"/> of the way from <see cref="GuestEntry"/> to the counter (0 to 1).</summary>
        public Vector2 GuestPathAt(float t) => Vector2.Lerp(GuestEntry, GuestSpot, t);
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
            // The speech bubble above its head: the products it wants, each with its count. It grows to the right from its tail,
            // which stays over the guest, so the moon pond's "Câu cá" plate on the bank to its upper left stays in view (#84).
            _bubble = UiKit.Rect("Bubble", _guest).Place(0.5f, 1f, 4, 64, 120, 100);
            _bubble.pivot = new Vector2(0f, 0.5f);
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
            // It sits just under the HUD row, where it covers none of the garden's plates as the view rests on the teapot (#71, re-checked after #50 moved the jar).
            var badge = UiKit.Rect("Order", _hud).Place(1f, 1f, -184, -176, 230, 56); // just under the HUD row, clear of the jar plate since #50 moved it (#71)
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
            Vector2 at = GuestPathAt(_guestT);
            if (walking && !Motion.Reduced) at.y += Mathf.Abs(Mathf.Sin(_guestT * Mathf.PI * 9f)) * 14f;
            _guest.anchoredPosition = at;
            float toward = GuestSpot.x >= GuestEntry.x ? 1f : -1f; // the art faces right: it turns to face the way it walks
            _guestArt.localScale = new Vector3(_guestPhase == GuestPhase.Leaving ? -toward : toward, 1f, 1f);
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
            Feel.Fly(from, Feel.PointOf(_leafChip), LeafIcon(catalog), LeafGold, Recipes.LeavesPerOrder, 34f);
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
        /// Empty spots of the lawn a resting rabbit may stroll to: the empty tiles of the open strip (the hex grid, #52, and the
        /// horizontal garden) that keep <see cref="Strolls.Clearance"/> from every station and its plot, stay off every station's
        /// signs and plates, keep 150 from the seats, the props, the guest and the decoration spots and 300 from the pond, and
        /// stand 250 apart. Land still in the mist offers none. Worked out again when the open land or what stands on it changes.
        /// </summary>
        Vector2[] Lawn = new Vector2[0];
        Vec2[] LawnCore = new Vec2[0];
        string _lawnKey;
        public IReadOnlyList<Vector2> LawnSpots { get { RefreshLawnSpots(); return Lawn; } }

        /// <summary>The spots apart from each other at least this far, so two strollers never stand side by side.</summary>
        const float LawnSpotApart = 220f;

        void RefreshLawnSpots()
        {
            GardenGrid g = Grid;
            if (g == null || Runner == null) return;
            var key = new System.Text.StringBuilder();
            key.Append(Expansion.Segments(Runner.State)).Append('|').Append(Runner.State.Plots).Append('|');
            foreach (KeyValuePair<string, Hex> o in g.Occupants) key.Append(o.Value.Q).Append(',').Append(o.Value.R).Append(';');
            string now = key.ToString();
            if (now == _lawnKey) return;
            _lawnKey = now;
            var spots = new List<Vector2>();
            foreach (Hex h in GardenGrid.Strip(Expansion.Segments(Runner.State)))
            {
                if (g.OccupantAt(h.Q, h.R) != null || Reserved(h)) continue;
                Vector2 at = CellToWorld(h);
                if (at.x < 150f || at.x > ReadableRight - 80f || !LawnSpotClear(at)) continue;
                bool apart = true;
                foreach (Vector2 other in spots) if (Vector2.Distance(other, at) < LawnSpotApart) { apart = false; break; }
                if (apart) spots.Add(at);
            }
            Lawn = spots.ToArray();
            LawnCore = System.Array.ConvertAll(Lawn, v => new Vec2(v.x, v.y));
            // A stroll planned on the spots before goes home; the next one is planned on these.
            foreach (Walker w in _walkers) { w.Out.Number = int.MinValue; w.Out.Granted = false; w.Out.Spot = -1; }
        }

        bool LawnSpotClear(Vector2 at)
        {
            for (int i = 0; i < Runner.Defs.Count; i++)
            {
                if (Vector2.Distance(at, StationSpot(i)) < Strolls.Clearance) return false;
                Rect reach = StationReach(i);
                if (reach.Contains(at)) return false;
            }
            for (int k = 0; k < Decor.Spots; k++) if (Vector2.Distance(at, DecorSpotAt(k)) < 150f) return false;
            for (int k = 0; k < RestSpots.Max; k++) if (Vector2.Distance(at, RestSpot(k)) < 150f) return false;
            foreach (Vector2 prop in new[] { MortarSpot, ShopSpot, GuestSpot, JarSpot }) if (Vector2.Distance(at, prop) < 150f) return false;
            return Vector2.Distance(at, PondSpot) >= 320f;
        }
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
            for (int i = 0; i < RestSpots.Max && catalog.Rabbit != null; i++)
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
            for (int k = 0; k < _actors.Count; k++)
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
            RefreshLawnSpots();
            int cap = Strolls.MaxAtOnce(Lawn.Length);
            foreach (int i in starting)
            {
                Outing o = _walkers[i].Out;
                int n = StrollNumber(i, _clock);
                var blocked = new List<Vec2>(_actors.Count + _walkers.Count);
                for (int k = 0; k < _actors.Count; k++) { Vector2 at = StationSpot(k); blocked.Add(new Vec2(at.x, at.y)); }
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
            RefreshLawnSpots();
            var stations = new List<Vec2>();
            for (int k = 0; k < _actors.Count; k++) { Vector2 at = StationSpot(k); stations.Add(new Vec2(at.x, at.y)); }
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
                if (o.Spot < 0 || o.Spot >= Lawn.Length) continue;
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

        #region A rabbit asks for its picture (#56)

        RectTransform _askBubble;
        Text _photoAskLine;
        long _askShownId, _askRabbitId;
        BigNum _askPaid;
        bool _askCelebrate;

        /// <summary>True while photo mode was opened from a rabbit's camera bubble: the next picture answers it.</summary>
        public bool PhotoAskFraming { get; private set; }
        /// <summary>True while a camera bubble shows over a rabbit in the garden.</summary>
        public bool PhotoAskShown => _askBubble != null && _askBubble.gameObject.activeSelf;

        /// <summary>Where the asking rabbit is on the screen (canvas units), or null when nobody asks or it is not in view.</summary>
        public Vector2? AskingRabbitOnScreen
        {
            get
            {
                Rabbit r = PhotoAsks.Asking(Runner.State);
                long id = r != null ? r.Id : _askRabbitId;
                return AskSpot(id, out _, out Vector2 body) ? ToView(body) : (Vector2?)null;
            }
        }

        /// <summary>A round paper bubble with a little camera drawn in it, like the HUD's camera button; it stands on the garden.</summary>
        void BuildPhotoAsk(GameCatalog catalog)
        {
            _askBubble = UiKit.Rect("PhotoAsk", _street).Place(0f, 0f, 0, 0, 120, 120);
            _askBubble.pivot = new Vector2(0.5f, 0f);
            var tail = UiKit.Rect("Tail", _askBubble).Place(0.5f, 0f, 0, 10, 30, 30);
            tail.localRotation = Quaternion.Euler(0, 0, 45f);
            tail.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Shadow", _askBubble).Place(0.5f, 0.5f, 0, -6, 112, 112).AddImage(catalog.Round, Palette.Alpha(Palette.Night, 0.25f));
            Image plate = UiKit.Rect("Plate", _askBubble).Place(0.5f, 0.5f, 0, 6, 112, 112).AddImage(catalog.Round, Palette.Paper);
            plate.raycastTarget = true;
            var button = _askBubble.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = plate;
            button.onClick.AddListener(OnPhotoAskTap);
            RectTransform cam = plate.rectTransform;
            UiKit.Rect("Body", cam).Place(0.5f, 0.5f, 0, -4, 60, 42).AddImage(catalog.Panel, Palette.Ink, true);
            UiKit.Rect("Bump", cam).Place(0.5f, 0.5f, -10, 20, 22, 14).AddImage(catalog.Panel, Palette.Ink, true);
            UiKit.Rect("Lens", cam).Place(0.5f, 0.5f, 0, -4, 26, 26).AddImage(catalog.Round, Palette.Paper);
            UiKit.Rect("Glint", cam).Place(0.5f, 0.5f, -3, -1, 8, 8).AddImage(catalog.Round, Palette.Ink);
            UiKit.Rect("Flash", cam).Place(0.5f, 0.5f, 20, 10, 10, 8).AddImage(catalog.Round, Palette.Lantern);
            _askBubble.gameObject.SetActive(false);
            _askShownId = 0;
        }

        /// <summary>The ask's clock runs with the other open-play events, but stands still while the camera is up, so framing never costs the bubble.</summary>
        void UpdatePhotoAsk(float dt)
        {
            if (!PhotoMode && Runner.AdvancePhotoAsks(dt) == PhotoAskEvent.Asked) Sfx.Blip();
            PlacePhotoAsk();
        }

        /// <summary>The bubble over the asking rabbit's head, wherever it is; it springs up when it first shows and bobs while it waits. Never in a picture.</summary>
        void PlacePhotoAsk()
        {
            if (_askBubble == null) return;
            Rabbit r = PhotoAsks.Asking(Runner.State);
            bool show = r != null && !PhotoMode && AskSpot(r.Id, out Vector2 head, out _);
            if (_askBubble.gameObject.activeSelf != show) _askBubble.gameObject.SetActive(show);
            if (!show) { if (r == null) _askShownId = 0; return; }
            AskSpot(r.Id, out head, out _);
            float bob = Motion.Reduced ? 0f : Mathf.Sin(_clock * 3f) * 6f;
            _askBubble.anchoredPosition = head + new Vector2(0, bob);
            if (_askBubble.GetSiblingIndex() != _askBubble.parent.childCount - 1) _askBubble.SetAsLastSibling();
            if (_askShownId == r.Id) return;
            _askShownId = r.Id;
            RectTransform bubble = _askBubble;
            bubble.localScale = Vector3.zero;
            Tweens.Add(Motion.Time(Motion.Panel), t => { if (bubble != null) bubble.localScale = Vector3.one * t; }, Motion.Curve(Ease.OutBack));
        }

        /// <summary>
        /// Where a rabbit stands in the garden: above its head for the bubble, and its middle for the camera. False when it is
        /// not out where it can be seen (a closed station, resting with no seat).
        /// </summary>
        bool AskSpot(long id, out Vector2 head, out Vector2 body)
        {
            head = body = Vector2.zero;
            if (id <= 0 || Runner == null || Runner.State == null) return false;
            GameState s = Runner.State;
            for (int i = 0; i < _actors.Count; i++)
            {
                Rabbit worker = Rabbits.AtStation(s, i);
                if (worker == null || worker.Id != id || s.Levels[i] <= 0 || !_actors[i].Root.gameObject.activeSelf) continue;
                float size = StationSize(i);
                Vector2 feet = StationSpot(i) + _actors[i].RabbitHome * size;
                body = feet + new Vector2(0, 128f * size);
                head = feet + new Vector2(0, 350f * size); // above its name plate, clear of a doze's "z z z"
                return true;
            }
            for (int k = 0; k < _seats.Count && k < _seatIds.Length; k++)
            {
                if (_seatIds[k] != id) continue;
                RectTransform near = k < _walkers.Count && _walkers[k].Rect.gameObject.activeSelf ? _walkers[k].Rect : _seats[k];
                Vector2 feet = near.anchoredPosition;
                body = feet + new Vector2(0, 60f);
                head = feet + new Vector2(0, 175f); // above a walker's own little bubble
                return true;
            }
            return false;
        }

        /// <summary>A touch on the bubble: the camera comes up, close in and centred on the rabbit that asked.</summary>
        void OnPhotoAskTap()
        {
            Rabbit r = PhotoAsks.Asking(Runner.State);
            if (r == null || !AskSpot(r.Id, out _, out Vector2 body)) return;
            Sfx.Click();
            EnterPhotoMode();
            if (!PhotoMode) return;
            PhotoAskFraming = true;
            _askRabbitId = r.Id;
            PlacePhotoAsk(); // the bubble is not in the picture
            Vector2 to = new Vector2(ScreenW / 2f, ScreenH * 0.55f), fromAt = ToView(body);
            float from = _zoom;
            if (_scroll != null) _scroll.StopMovement();
            if (Motion.Reduced) { PlaceView(body, to, ZoomNear); return; }
            Tweens.Add(Motion.Time(Motion.Travel), t => { if (_street != null) PlaceView(body, Vector2.LerpUnclamped(fromAt, to, t), Mathf.LerpUnclamped(from, ZoomNear, t)); }, Ease.InOutCubic);
        }

        /// <summary>The picture was taken for the rabbit: the reward is paid now and the review card says so; the thanks follow once the garden is back.</summary>
        bool AnswerPhotoAsk()
        {
            PhotoAskFraming = false;
            Rabbit r = Rabbits.Find(Runner.State, _askRabbitId);
            if (!Runner.AnswerPhotoAsk(out BigNum paid)) return false;
            _askPaid = paid;
            _askCelebrate = true;
            _photoAskLine.text = L10n.T("photo.ask.paid", r != null ? r.Name : "", paid.Short());
            return true;
        }

        /// <summary>Back from the camera after answering: the rabbit hops, hearts rise, and the carrots fly from it to the purse.</summary>
        void CelebratePhotoAsk()
        {
            if (!_askCelebrate) return;
            _askCelebrate = false;
            GameCatalog catalog = Runner.Catalog;
            RectTransform near = null;
            GameState s = Runner.State;
            for (int i = 0; i < _actors.Count && near == null; i++)
            {
                Rabbit worker = Rabbits.AtStation(s, i);
                if (worker == null || worker.Id != _askRabbitId) continue;
                _actors[i].Hop();
                near = _actors[i].Root;
            }
            for (int k = 0; k < _seats.Count && k < _seatIds.Length && near == null; k++)
            {
                if (_seatIds[k] != _askRabbitId) continue;
                near = k < _walkers.Count && _walkers[k].Rect.gameObject.activeSelf ? _walkers[k].Rect : _seats[k];
                RectTransform seat = near;
                Vector3 rest = seat.localScale;
                Tweens.Add(Motion.Time(Motion.Tap), t => { if (seat != null) seat.localScale = new Vector3(rest.x, rest.y * (1f + 0.18f * Ease.Pop(t)), rest.z); }, Ease.Linear, 0f,
                    () => { if (seat != null) seat.localScale = rest; });
            }
            Vector2 at = near != null ? Feel.PointOf(near, new Vector2(0, 120)) : new Vector2(0, 200);
            Feel.FloatText(at + new Vector2(60, 30), "♥", Palette.Blush, 54);
            Feel.FloatText(at + new Vector2(0, 80), "♥", Palette.Blush, 38);
            Feel.FloatText(at + new Vector2(110, 90), "♥", Palette.Blush, 32);
            Sfx.Reward();
            Haptics.Light();
            string gained = "+" + _askPaid.Short();
            Feel.Fly(at, Feel.PointOf(_purse), catalog.Currency, Color.white, 6, 44f, () =>
            {
                if (_purse != null) Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), gained, Palette.Carrot, 44);
            });
        }

        #endregion

        #region Rabbits say a line now and then (#59)

        RectTransform _chatLine;
        Text _chatText;
        float _chatTimer = -1f, _chatLeft;
        long _chatRabbitId;

        /// <summary>True while an idle line shows over a rabbit's head.</summary>
        public bool ChatShown => _chatLine != null && _chatLine.gameObject.activeSelf;
        /// <summary>How many idle lines have been said since the screen was built.</summary>
        public int ChatLines { get; private set; }
        /// <summary>The string key of the last idle line.</summary>
        public string ChatKey { get; private set; }
        /// <summary>The rabbit that said the last idle line.</summary>
        public long ChatRabbitId => _chatRabbitId;

        /// <summary>A bond line or a guest outranks an idle line: while either is up, the idle line waits.</summary>
        bool ChatBlocked => (_bondLine != null && _bondLine.gameObject.activeSelf) || GuestShown;

        /// <summary>Every 40 to 70 seconds of open play one rabbit says a line; the bubble follows it and leaves after 2.5 seconds.</summary>
        void UpdateChatter(float dt)
        {
            if (ChatShown)
            {
                _chatLeft -= dt;
                if (_chatLeft <= 0f || ChatBlocked || PhotoMode || !AskSpot(_chatRabbitId, out Vector2 head, out _) || !PlaceChat(head, out Vector2 at))
                    _chatLine.gameObject.SetActive(false);
                else
                {
                    _chatLine.anchoredPosition = at;
                    float age = Chatter.Seconds - _chatLeft, grow = age / Motion.Panel, shrink = _chatLeft / Motion.PanelOut;
                    float scale = Motion.Reduced ? 1f : grow < 1f ? Ease.OutBack(grow) : shrink < 1f ? Ease.OutCubic(shrink) : 1f;
                    _chatLine.localScale = Vector3.one * scale;
                }
            }
            if (Shell.State != ShellState.Playing || PhotoMode) return;
            if (_chatTimer < 0f) _chatTimer = Chatter.Wait(Hash(_clock + 23f));
            _chatTimer -= dt;
            if (_chatTimer > 0f) return;
            if (ChatBlocked) { _chatTimer = 0f; return; } // it waits for the bond line or the guest to go
            _chatTimer = Chatter.Wait(Hash(_clock + 23f));
            ChatterNow();
        }

        /// <summary>
        /// One rabbit, picked from wherever the clock points, says a line now: an awake rabbit at work, or one resting on
        /// its seat or out for a stroll. False when nobody is there to say one, or a bond line or a guest is up.
        /// </summary>
        public bool ChatterNow()
        {
            if (ChatBlocked || PhotoMode || Runner == null || Runner.State == null) return false;
            GameState s = Runner.State;
            Rabbit asking = PhotoAsks.Asking(s); // its camera bubble is over its head already
            var who = new List<Rabbit>();
            var where = new List<int>();
            for (int i = 0; i < _actors.Count; i++)
            {
                Rabbit r = Rabbits.AtStation(s, i);
                if (r == null || s.Levels[i] <= 0 || _actors[i].Dozing || !_actors[i].Root.gameObject.activeSelf) continue;
                if (asking != null && asking.Id == r.Id) continue;
                who.Add(r);
                where.Add(i);
            }
            for (int k = 0; k < _seats.Count && k < _seatIds.Length; k++)
            {
                Rabbit r = _seatIds[k] > 0 ? Rabbits.Find(s, _seatIds[k]) : null;
                if (r == null || (asking != null && asking.Id == r.Id)) continue;
                who.Add(r);
                where.Add(-1);
            }
            if (who.Count == 0) return false;
            int pick = Mathf.Min(who.Count - 1, Mathf.FloorToInt(Hash(_clock + 5f) * who.Count));
            return SayIdleLine(who[pick], where[pick]);
        }

        /// <summary>
        /// Rabbit <paramref name="id"/> says a line now, wherever it is: at work, or resting on its seat or out for a stroll.
        /// False when it cannot (asleep, not out, asking for a photo, no room clear of the plates), or a bond line or a guest is up.
        /// </summary>
        public bool ChatterBy(long id)
        {
            if (ChatBlocked || PhotoMode || Runner == null || Runner.State == null) return false;
            GameState s = Runner.State;
            Rabbit asking = PhotoAsks.Asking(s);
            if (asking != null && asking.Id == id) return false;
            for (int i = 0; i < _actors.Count; i++)
            {
                Rabbit r = Rabbits.AtStation(s, i);
                if (r == null || r.Id != id) continue;
                if (s.Levels[i] <= 0 || _actors[i].Dozing || !_actors[i].Root.gameObject.activeSelf) return false;
                return SayIdleLine(r, i);
            }
            for (int k = 0; k < _seats.Count && k < _seatIds.Length; k++)
                if (_seatIds[k] == id) return SayIdleLine(Rabbits.Find(s, id), -1);
            return false;
        }

        /// <summary>The line itself: picked for the speaker's trade, the time of day and the season, in a bubble near its head.</summary>
        bool SayIdleLine(Rabbit speaker, int station)
        {
            if (speaker == null) return false;
            GameState s = Runner.State;
            bool resting = station < 0;
            int booster = resting ? -1 : Chatter.BoosterOf(Runner.Defs, s, station);
            bool dressed = speaker.Dye >= 0 || VuonTra.Core.Items.Worn(s, speaker.Id, ItemSlot.Hat) != null;
            List<string> keys = Chatter.KeysFor(Rabbits.TradeOf(speaker), resting, Daylight.PhaseAt(ClockHours()),
                Seasons.SeasonOf(Runner.TodayNumber), booster >= 0, dressed);
            string key = Chatter.Pick(keys, Hash(_clock + 7f));
            if (key == null || !AskSpot(speaker.Id, out Vector2 head, out _)) return false;
            if (_chatLine == null) BuildChatLine();
            KeepChatOff();
            _chatShift = Vector2.zero;
            if (!PlaceChat(head, out Vector2 at)) return false; // no room clear of the plates: the line waits for another time
            _chatText.text = key == Chatter.BoostKey ? L10n.T(key, L10n.Station(Runner.Defs[booster].Id)) : L10n.T(key);
            _chatRabbitId = speaker.Id;
            _chatLeft = Chatter.Seconds;
            _chatLine.anchoredPosition = at;
            _chatLine.SetAsLastSibling();
            _chatLine.localScale = Motion.Reduced ? Vector3.one : Vector3.zero; // reduced motion: it simply appears
            _chatLine.gameObject.SetActive(true);
            ChatKey = key;
            ChatLines++;
            return true;
        }

        /// <summary>Where the line stands from the rabbit's head: straight over it, unless that covers a plate (#16).</summary>
        Vector2 _chatShift;
        /// <summary>The line's paper, as <see cref="BuildChatLine"/> makes it; it stands on its foot.</summary>
        static readonly Vector2 ChatSize = new Vector2(560f, 84f);
        /// <summary>The places a line tries from the rabbit's head: over it, a little higher, then to either side, then higher still.</summary>
        static readonly Vector2[] ChatShifts =
        {
            new Vector2(0f, 0f), new Vector2(0f, 60f), new Vector2(-150f, 0f), new Vector2(150f, 0f), new Vector2(0f, 120f),
            new Vector2(-150f, 60f), new Vector2(150f, 60f), new Vector2(-240f, 0f), new Vector2(240f, 0f),
            new Vector2(-240f, 90f), new Vector2(240f, 90f), new Vector2(0f, 180f),
        };
        /// <summary>The plates a line keeps off, gathered when it starts.</summary>
        readonly List<RectTransform> _chatKeepOff = new List<RectTransform>();

        /// <summary>
        /// The plates of the garden a line must not cover (#16): every station's level plate, the mortar's "Giã bánh", the
        /// basket's, the jar's and the pond's "Câu cá" plates, and the sign at the lawn's end.
        /// </summary>
        void KeepChatOff()
        {
            _chatKeepOff.Clear();
            foreach (Transform child in _street)
            {
                if (child.name.StartsWith("Spot ")) continue; // a decoration spot's faint "+" is no plate to read
                Transform plate = child.name.StartsWith("Station ") ? child.Find("Level") : child.name == "Forest Sign" ? child.Find("Board") : child.Find("Plate");
                if (plate != null) _chatKeepOff.Add((RectTransform)plate);
            }
        }

        /// <summary>
        /// Where the line stands, near the rabbit's head and over none of the plates: where it stood if that is still clear, else
        /// the first clear place of <see cref="ChatShifts"/>, inside the readable part of the garden. False when there is no room.
        /// </summary>
        bool PlaceChat(Vector2 head, out Vector2 at)
        {
            var plates = new List<Rect>(_chatKeepOff.Count);
            foreach (RectTransform plate in _chatKeepOff)
                if (plate != null && plate.gameObject.activeInHierarchy) plates.Add(StreetRect(plate));
            at = head + _chatShift;
            if (ChatClear(at, plates)) return true;
            foreach (Vector2 shift in ChatShifts)
            {
                at = head + shift;
                if (!ChatClear(at, plates)) continue;
                _chatShift = shift;
                return true;
            }
            return false;
        }

        static bool ChatClear(Vector2 foot, List<Rect> plates)
        {
            var line = new Rect(foot.x - ChatSize.x / 2f, foot.y, ChatSize.x, ChatSize.y);
            if (line.xMin < WorldW - ReadableRight || line.xMax > ReadableRight) return false;
            foreach (Rect plate in plates) if (plate.Overlaps(line)) return false;
            return true;
        }

        /// <summary>A paper bubble like the bond line's, standing on the garden; it never takes a touch.</summary>
        void BuildChatLine()
        {
            _chatLine = UiKit.Rect("ChatLine", _street).Place(0f, 0f, 0, 0, ChatSize.x, ChatSize.y);
            _chatLine.pivot = new Vector2(0.5f, 0f);
            _chatLine.AddShadow(10f, 6f, 0.25f);
            _chatLine.AddImage(Runner.Catalog.Panel, Palette.Paper, true).raycastTarget = false;
            _chatText = UiKit.Rect("Text", _chatLine).Stretch(0, 0, 1, 1, 20, 8, 20, 8)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _chatText.raycastTarget = false;
            _chatText.resizeTextForBestFit = true; // the longest line in either language stays on its plate
            _chatText.resizeTextMinSize = 18;
            _chatText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _chatText.horizontalOverflow = HorizontalWrapMode.Wrap;
            _chatLine.gameObject.SetActive(false);
        }

        #endregion

        #region Moon jars (#64)

        RectTransform _jarChip;
        Text _jarText;

        /// <summary>The jars on the shelf, as a chip beside the golden carrots under the purse: only from garden level 3 and while there is one.</summary>
        void BuildJarChip(GameCatalog catalog)
        {
            _jarChip = UiKit.Rect("Jars", _hud).Place(0f, 1f, 392, -154, 118, 52);
            _jarChip.AddShadow(10f, 6f, 0.25f);
            _jarChip.AddImage(catalog.Panel, Palette.Paper, true);
            IncomeView.JarIcon(_jarChip, catalog, "Icon", 42f, 0f, 0.5f, 30, 2);
            _jarText = UiKit.Rect("Text", _jarChip).Stretch(0, 0, 1, 1, 56, 0, 10, 0).AddText("0", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _jarChip.gameObject.SetActive(false);
        }

        RectTransform _leafChip;
        Text _leafText;
        int _leavesShown = -1;

        void BuildLeafChip(GameCatalog catalog)
        {
            _leafChip = UiKit.Rect("RecipeLeaves", _hud).Place(0f, 1f, 535, -154, 150, 52);
            _leafChip.AddShadow(10f, 6f, 0.25f);
            _leafChip.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Icon", _leafChip).Place(0f, 0.5f, 30, 0, 38, 38).AddImage(LeafIcon(catalog), LeafGold).preserveAspect = true;
            _leafText = UiKit.Rect("Text", _leafChip).Stretch(0, 0, 1, 1, 56, 0, 10, 0).AddText("0", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _leafChip.gameObject.SetActive(false);
            _leavesShown = -1;
        }

        void RefreshLeafChip(GameState s)
        {
            bool show = s.RecipeLeaves > 0 || Recipes.AnyRaised(s);
            if (_leafChip.gameObject.activeSelf != show) _leafChip.gameObject.SetActive(show);
            if (s.RecipeLeaves == _leavesShown) return;
            bool more = _leavesShown >= 0 && s.RecipeLeaves > _leavesShown;
            _leavesShown = s.RecipeLeaves;
            _leafText.text = s.RecipeLeaves.ToString();
            if (_recipeLeaves != null) _recipeLeaves.text = L10n.T("recipes.leaves", s.RecipeLeaves);
            if (!more || !show) return;
            RectTransform chip = _leafChip;
            Tweens.Add(Motion.Tap, t => { if (chip != null) chip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (chip != null) chip.localScale = Vector3.one; });
        }

        void RefreshJarChip(GameState s)
        {
            bool show = MoonJars.Shown(s) && s.MoonJars > 0;
            if (_jarChip.gameObject.activeSelf != show) _jarChip.gameObject.SetActive(show);
            _jarText.text = s.MoonJars.ToString();
        }

        /// <summary>"Mở hũ trăng" on the purse's card: an hour of income flies from the button to the purse, like a sale from the tray.</summary>
        void OnOpenMoonJar()
        {
            Vector2 from = Feel.PointOf(Income.JarButton);
            BigNum paid = Runner.OpenMoonJar();
            if (paid <= BigNum.Zero) return;
            PaySale(from, paid, 0);
            Income.Refresh(Runner.Defs, Runner.State, Runner.Mods);
        }

        /// <summary>The jar on the shop's shelf, bought with the second tap.</summary>
        void OnBuyMoonJar()
        {
            if (!Runner.BuyMoonJar()) return;
            Sfx.Reward();
            Haptics.Light();
            Feel.FloatText(new Vector2(0, 420), L10n.T("float.jar.bought"), Palette.Lantern, 36);
        }

        /// <summary>A moon jar was won: a little moon flies from where it came to the purse, and the jars' chip swells.</summary>
        void JarArrived(Vector2 from)
        {
            GameCatalog catalog = Runner.Catalog;
            Feel.Fly(from, Feel.PointOf(_purse), catalog.Round, Palette.Lantern, 1, 44f, () =>
            {
                if (_purse == null) return;
                Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), L10n.T("float.jar.bought"), Palette.Lantern, 34);
                if (_jarChip != null) Tweens.Add(Motion.Tap, t => { if (_jarChip != null) _jarChip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_jarChip != null) _jarChip.localScale = Vector3.one; });
            });
        }

        #endregion

        #region Station sounds up close and the tune of the hour (#63)

        StationLoopPlayer _stationSound;
        /// <summary>The loop of the station looked at up close: its clip, volume and pitch, for tests.</summary>
        public StationLoopPlayer StationSound => _stationSound ?? (_stationSound = new StationLoopPlayer(gameObject));

        /// <summary>The tune follows the clock; up close the station in view plays its loop, a tenth faster while the boost runs.</summary>
        void UpdateSounds(bool boosting, float dt)
        {
            Sfx.UpdateTune(ClockHours());
            StationSound.Update(Zoom == ZoomLevel.Near ? HeardStation() : null, boosting, dt);
        }

        /// <summary>The station whose card is open, else the open station nearest the point the view centres on, if it is on screen.</summary>
        string HeardStation()
        {
            GameState s = Runner.State;
            IReadOnlyList<StationDef> defs = Runner.Defs;
            bool Heard(int i) => i >= 0 && i < _actors.Count && i < defs.Count && i < s.Plots && s.Levels[i] > 0 && _actors[i].Root.gameObject.activeSelf;
            if (Heard(Selected)) return defs[Selected].Id;
            var centre = new Vector2(ScreenW / 2f, ScreenH * 0.6f);
            int best = -1;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < _actors.Count; i++)
            {
                if (!Heard(i)) continue;
                Vector2 at = ViewPointOf(i);
                if (at.x < 0f || at.x > ScreenW || at.y < 0f || at.y > ScreenH) continue;
                float d = (at - centre).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = i; }
            }
            return best >= 0 ? defs[best].Id : null;
        }

        #endregion

        #region Gift codes and mailbox (#68)

        /// <summary>The mailbox card, opened from the pause card's "Thư".</summary>
        public MailView Mail { get; private set; }

        /// <summary>Built after the profile, so it draws over the pause card it opens from.</summary>
        void BuildMail(GameCatalog catalog)
        {
            Mail = new MailView(_canvas, catalog, Tweens, OnClaimLetter);
        }

        void OpenMail() => Mail.Open(Runner.Letters, Runner.TodayNumber);

        /// <summary>
        /// A gift code typed in the profile. When it pays, the profile and the pause card close so the gift is seen
        /// flying to the purse; otherwise the profile shakes its field and says why.
        /// </summary>
        GiftCodes.Result OnRedeemCode(string typed)
        {
            GiftCodes.Redeemed r = Runner.RedeemCode(typed);
            if (r.Result != GiftCodes.Result.Ok) return r.Result;
            Vector2 from = Feel.PointOf(Profile.Root);
            Profile.Close();
            Shell.Resume();
            Refresh(-1f);
            GiftArrived(from, r.Golden, r.Clovers, r.Hat);
            return r.Result;
        }

        /// <summary>A letter claimed in the mailbox: its gift is paid, the card says what came and the badge counts down.</summary>
        void OnClaimLetter(string id)
        {
            Letter letter = Runner.ClaimLetter(id, out Rabbit rabbit);
            if (letter == null) return;
            Sfx.Reward();
            Haptics.Light();
            Mail.ShowClaimed(L10n.T("mail.claimed", MailView.GiftOf(letter)));
            if (rabbit != null && letter.Kind != LetterKind.Newcomer) Feel.Moment(rabbit.Name, L10n.T("detail.kind", L10n.Kind(rabbit), L10n.Rarity(rabbit.Tier)));
            Mail.Refresh(Runner.Letters, Runner.TodayNumber);
            Shell.RefreshMail();
            Refresh(-1f);
            if (rabbit == null || letter.Kind != LetterKind.Newcomer) return;
            Mail.Close();
            Sfx.Summon(rabbit.Tier);
            Welcome(rabbit);
        }

        public NewcomerView FirstWeek { get; private set; }

        void OnClaimNewcomer(int day, int slot)
        {
            NewcomerClaim claim = Runner.ClaimNewcomer(day, slot);
            if (!claim.Ok)
            {
                Sfx.Deny();
                return;
            }
            Sfx.Reward();
            Haptics.Light();
            string paid = L10n.T("newcomer.carrots", claim.Carrots.Short()) + (claim.Clovers > 0 ? " · " + L10n.T("mail.clovers", claim.Clovers) : "") + (claim.Golden > 0 ? " · " + L10n.T("mail.golden", claim.Golden) : "");
            if (claim.DayPaid) paid += " + " + NewcomerView.DayGiftOf(day);
            FirstWeek.ShowClaimed(L10n.T("mail.claimed", paid));
            Refresh(-1f);
            if (claim.DayPaid) Sfx.Milestone();
            if (claim.Rabbit == null) return;
            FirstWeek.Close();
            Sfx.Summon(claim.Rabbit.Tier);
            Welcome(claim.Rabbit);
        }

        /// <summary>A gift from a code: golden carrots fly to their chip under the purse, clovers and a hat say so in words.</summary>
        void GiftArrived(Vector2 from, int golden, int clovers, Item hat)
        {
            Sfx.Reward();
            Haptics.Light();
            GameCatalog catalog = Runner.Catalog;
            if (golden > 0)
            {
                if (!_goldenChip.gameObject.activeSelf) _goldenChip.gameObject.SetActive(true);
                Feel.Fly(from, Feel.PointOf(_goldenChip), catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, Mathf.Min(golden, 5), 44f,
                    () => Tweens.Add(Motion.Tap, t => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one; }));
                Feel.FloatText(Feel.PointOf(_goldenChip, new Vector2(0, -70)), L10n.T("mail.golden", golden), Palette.Lantern, 34);
            }
            if (clovers > 0) Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -150)), L10n.T("mail.clovers", clovers), Palette.Lantern, 34);
            if (hat != null) Feel.FloatText(new Vector2(0, 420), L10n.T("float.hat", L10n.ItemName(hat)), Palette.Lantern, 40);
        }

        #endregion

        #region Rewarded ad seams (#66)

        /// <summary>The extra round's ad button on each little game's result card (#66).</summary>
        public AdButton PoundAd { get; private set; }
        public AdButton LeavesAd { get; private set; }
        public AdButton JarAd { get; private set; }

        /// <summary>
        /// The three places a rewarded video may be offered, each a secondary button shown only while it offers
        /// something: ×2 on the welcome-back sheet once the free one is spent, one more round on a game's result card
        /// once its rounds are spent, and a golden carrot in the shop's golden row. The no-ads pass goes on the packs
        /// card, only while ads are switched on. Nothing here at the rabbit call, and no ad ever interrupts.
        /// </summary>
        void BuildAdSeams(GameCatalog catalog)
        {
            AwayCard.AttachAd(() => Runner.AdOfferFor(Ads.AwayDouble), () => Runner.AdShowing, OnAwayAd);
            Shop.AttachAd(() => Runner.AdOfferFor(Ads.ShopGolden), () => Runner.AdShowing, OnShopAd);
            PoundAd = ExtraRoundButton(catalog, Pound.ResultCard, Ads.GamePounding, () => { Pound.Close(); OpenPounding(); });
            LeavesAd = ExtraRoundButton(catalog, Basket.ResultCard, Ads.GameLeaves, () => { Basket.Close(); OpenLeaves(); });
            JarAd = ExtraRoundButton(catalog, Jar.ResultCard, Ads.GameFireflies, () => { Jar.Close(); OpenFireflies(); });
            GoldenPacks.AttachPass(catalog, () => Runner.AdsOn, () => Runner.NoAdsDaysLeft, OnBuyNoAdsPass);
        }

        /// <summary>A secondary button hung just under a result card: one more round once the day's are spent.</summary>
        AdButton ExtraRoundButton(GameCatalog catalog, RectTransform card, string game, System.Action startAgain)
        {
            var rt = UiKit.Rect("ExtraRound", card).Place(0.5f, 0f, 0, -80, 560, UiKit.MinTouch);
            return new AdButton(rt, catalog, "ad.round", "ad.round.now", () => Runner.AdOfferFor(Ads.ExtraRound, game), () => Runner.AdShowing, () =>
                WatchAd(Ads.ExtraRound, game, ok => { if (ok) startAgain(); }));
        }

        /// <summary>The ad buttons as things stand (the shop's pill is refreshed with the shop).</summary>
        void RefreshAdSeams()
        {
            AwayCard?.Ad?.Refresh();
            Shop?.RefreshAd();
            PoundAd?.Refresh();
            LeavesAd?.Refresh();
            JarAd?.Refresh();
        }

        /// <summary>Asks the runner for a video (or the pass's instant claim), with the buttons saying "opening" meanwhile.</summary>
        void WatchAd(string placement, string game, System.Action<bool> paid)
        {
            Runner.WatchAd(placement, game, ok =>
            {
                if (!ok)
                {
                    Sfx.Deny();
                    if (Feel != null) Feel.FloatText(new Vector2(0, 300), L10n.T("ad.failed"), Palette.Ink, 34);
                }
                paid(ok);
                Refresh(-1f);
            });
            if (Runner.AdShowing) RefreshAdSeams(); // "opening" while the video plays
        }

        /// <summary>The welcome-back sheet's ad option: the time away it shows paid once more, as the ×2 button does.</summary>
        void OnAwayAd()
        {
            BigNum paid = AwayCard.Report.Paid;
            WatchAd(Ads.AwayDouble, null, ok =>
            {
                if (!ok) { AwayCard.RefreshDouble(); return; }
                Sfx.Reward();
                Haptics.Medium();
                Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -90)), "+" + paid.Short(), Palette.Carrot, 44);
                Feel.Burst(Feel.PointOf(_purse), Runner.Catalog.Currency, new[] { Color.white }, 16, 520f, 46f);
                AwayCard.Close();
            });
        }

        /// <summary>The shop's golden carrot for a watched video: it flies to the golden chip.</summary>
        void OnShopAd()
        {
            WatchAd(Ads.ShopGolden, null, ok =>
            {
                if (!ok) return;
                Sfx.Reward();
                Haptics.Light();
                GameCatalog catalog = Runner.Catalog;
                Vector2 from = Shop.Ad != null ? Feel.PointOf(Shop.Ad.Root) : new Vector2(0, 600);
                Feel.Fly(from, Feel.PointOf(_goldenChip), catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, 1, 44f);
                Feel.FloatText(from + new Vector2(0, -90), L10n.T("ad.shop.float"), Palette.Lantern, 40);
            });
        }

        /// <summary>The no-ads pass bought through the store: thirty days in which every ad button pays at once.</summary>
        void OnBuyNoAdsPass(System.Action<PurchaseResult> done)
        {
            Runner.BuyNoAdsPass(result =>
            {
                if (result.Outcome == PurchaseOutcome.Paid)
                {
                    Sfx.Reward();
                    Haptics.Light();
                    Feel.FloatText(new Vector2(0, 420), L10n.T("noads.float", Ads.PassDays), Palette.Lantern, 44);
                    Refresh(-1f);
                }
                done?.Invoke(result);
            });
        }

        #endregion

        #region Rain and the Frog Rabbit (#62)

        /// <summary>Whether it rains now. Unset, the screen asks the clamped day at the garden's clock; tests and previews pin it.</summary>
        public System.Func<bool> Raining;
        /// <summary>How hard it rains on screen, 0 to 1: it comes on and goes over a second and a half, at once under reduced motion.</summary>
        public float RainLevel { get; private set; }
        /// <summary>True while the rain's streaks are drawn (never under reduced motion, which keeps only the colour).</summary>
        public bool RainStreaksShown => _rainStreaks != null && _rainStreaks.gameObject.activeInHierarchy;
        /// <summary>How strong the rain's colour lies over the garden now.</summary>
        public float RainTintAlpha => _rainTint != null ? _rainTint.color.a : 0f;
        /// <summary>The lanterns burn a fifth brighter in the rain.</summary>
        public const float RainLanternBoost = 1.2f;
        /// <summary>The resting places whose rabbits run under a tree when it rains: the picnic rug and the hammock.</summary>
        public static readonly int[] RainShelterSeats = { 2, 4 };

        RectTransform _rainLayer, _rainStreaks;
        Image _rainTint;
        readonly List<RectTransform> _streaks = new List<RectTransform>();
        readonly List<Image> _streakImages = new List<Image>();
        readonly List<Image> _rainRipples = new List<Image>();
        readonly Vector2[] _seatHomes = new Vector2[RestSpots.Max];
        bool _sheltered;

        bool RainingNow() => Raining != null ? Raining() : Runner.RainingAt(ClockHours());

        /// <summary>The rain's colour over the whole garden, its streaks, the drops on the pond, and the Frog Rabbit with its gate.</summary>
        void BuildRain(GameCatalog catalog)
        {
            _streaks.Clear(); _streakImages.Clear(); _rainRipples.Clear();
            RainLevel = 0f;
            _sheltered = false;
            _rainLayer = UiKit.Rect("Rain", _canvas).Stretch(0, 0, 1, 1);
            _rainTint = UiKit.Rect("Tint", _rainLayer).Stretch(0, 0, 1, 1).AddImage(null, new Color(0, 0, 0, 0));
            // Faint slanting streaks, drawn by code like the mist: thin lines that fall and come round again.
            _rainStreaks = UiKit.Rect("Streaks", _rainLayer).Stretch(0, 0, 1, 1);
            for (int i = 0; i < 90; i++)
            {
                float length = 46f + Hash(i * 3.1f) * 40f;
                var streak = UiKit.Rect("Streak", _rainStreaks).Place(0f, 0f, Hash(i * 7.3f) * ScreenW, Hash(i * 1.9f) * ScreenH, 3f, length);
                streak.localRotation = Quaternion.Euler(0, 0, RainSlant);
                _streakImages.Add(streak.AddImage(catalog.Round, new Color(0.86f, 0.92f, 1f, 0f)));
                _streaks.Add(streak);
            }
            _rainLayer.gameObject.SetActive(false);
            // Drops landing on the pond: small rings that widen and fade, here and there on the water.
            for (int i = 0; i < 5; i++)
            {
                Image ring = UiKit.Rect("RainRipple", _street).Place(0f, 0f, PondSpot.x, PondSpot.y, 96f, 58f).AddImage(catalog.Glow, new Color(0.88f, 0.94f, 1f, 0f));
                _rainRipples.Add(ring);
            }
            for (int k = 0; k < _seats.Count && k < _seatHomes.Length; k++) _seatHomes[k] = _seats[k].anchoredPosition;
            BuildFrog(catalog);
        }

        /// <summary>How far the streaks lean from the upright, in degrees, and how fast they fall, in canvas units a second.</summary>
        const float RainSlant = -12f, RainFall = 1500f;

        /// <summary>The rain comes and goes with the showers: colour, streaks, drops on the pond, brighter lanterns, the patter, the resting rabbits under the trees, and the Frog Rabbit.</summary>
        void UpdateRain(float dt)
        {
            if (_rainLayer == null) return;
            bool raining = RainingNow();
            float target = raining ? 1f : 0f;
            RainLevel = Motion.Reduced ? target : Mathf.MoveTowards(RainLevel, target, dt / 1.5f);
            Sfx.SetRain(raining);
            bool shown = RainLevel > 0f;
            if (_rainLayer.gameObject.activeSelf != shown) _rainLayer.gameObject.SetActive(shown);
            if (shown)
            {
                // A grey-blue wash by day, a deeper blue at night, under the HUD.
                float night = (float)Daylight.NightAt(ClockHours());
                Color wash = Color.Lerp(new Color(0.46f, 0.56f, 0.68f), new Color(0.16f, 0.23f, 0.38f), night);
                _rainTint.color = Palette.Alpha(wash, Mathf.Lerp(0.13f, 0.22f, night) * RainLevel);
                bool streaks = !Motion.Reduced;
                if (_rainStreaks.gameObject.activeSelf != streaks) _rainStreaks.gameObject.SetActive(streaks);
                if (streaks)
                {
                    float lean = Mathf.Tan(-RainSlant * Mathf.Deg2Rad);
                    for (int i = 0; i < _streaks.Count; i++)
                    {
                        float speed = RainFall * (0.8f + 0.4f * Hash(i * 5.7f));
                        float fallen = Mathf.Repeat(Hash(i * 1.9f) * (ScreenH + 200f) + _clock * speed, ScreenH + 200f);
                        float x = Mathf.Repeat(Hash(i * 7.3f) * ScreenW - fallen * lean, ScreenW + 80f) - 40f;
                        _streaks[i].anchoredPosition = new Vector2(x, ScreenH + 100f - fallen);
                        _streakImages[i].color = new Color(0.86f, 0.92f, 1f, (0.14f + 0.16f * Hash(i * 2.3f)) * RainLevel);
                    }
                }
            }
            // Drops on the pond; still water under reduced motion.
            for (int i = 0; i < _rainRipples.Count; i++)
            {
                Image ring = _rainRipples[i];
                if (!shown || Motion.Reduced) { if (ring.color.a != 0f) ring.color = new Color(0.88f, 0.94f, 1f, 0f); continue; }
                float cycle = _clock / 1.1f + i * 0.21f;
                float k = Mathf.Repeat(cycle, 1f), n = Mathf.Floor(cycle);
                Vector2 at = PondSpot + new Vector2(20f + (Hash(n * 3.7f + i) - 0.5f) * 220f, 6f + (Hash(n * 5.3f + i) - 0.5f) * 110f);
                ring.rectTransform.anchoredPosition = at;
                ring.rectTransform.localScale = Vector3.one * Mathf.Lerp(0.2f, 1f, k);
                ring.color = new Color(0.88f, 0.94f, 1f, 0.4f * (1f - k) * RainLevel);
            }
            // The lanterns burn brighter in the rain (their flicker is set just before, every frame).
            if (shown)
                foreach (Image glow in _lanternGlows)
                    if (glow != null) glow.color = Palette.Alpha(glow.color, glow.color.a * Mathf.Lerp(1f, RainLanternBoost, RainLevel));
            ShelterSeats();
            UpdateFrog(dt);
        }

        /// <summary>
        /// The rabbits resting on the rug and in the hammock run under the trees at the wood's edge while it rains and
        /// sit there upright, then go back. Only where they are drawn changes: the save, the seats and their gifts stay.
        /// </summary>
        void ShelterSeats()
        {
            bool any = RainLevel > 0f;
            if (!any && !_sheltered) return;
            GameState s = Runner.State;
            foreach (int k in RainShelterSeats)
            {
                if (k >= _seats.Count) continue;
                Vector2 home = _seatHomes[k];
                _seats[k].anchoredPosition = Vector2.Lerp(home, ShelterOf(k), Ease.InOutCubic(RainLevel));
                if (any && _seatIds[k] > 0)
                {
                    // Under a tree it sits up, whatever its place has it do.
                    RectTransform art = _seatArt[k].rectTransform;
                    art.localRotation = Quaternion.identity;
                    art.localScale = new Vector3(1.06f, 0.86f, 1f);
                    art.anchoredPosition = new Vector2(0f, -6f);
                }
            }
            if (!any) RefreshSeats(s); // dry again: each sits as its place has it
            _sheltered = any;
        }

        /// <summary>
        /// Where a seat's rabbit sits out the rain: on open grass at the foot of the wood, under the trees nearest its
        /// place (the rug's rabbit by the trees left of the shop, the hammock's by those beside the hammock), placed by hand.
        /// </summary>
        static Vector2 ShelterOf(int seat) => seat == 2 ? new Vector2(200f, 2460f) : new Vector2(110f, 450f);

        // ----- the Frog Rabbit -----

        RectTransform _frog, _frogArt, _frogGate;
        Image _frogGateImage;
        Text _frogName;
        enum FrogPhase { Away, Coming, Waiting, Going }
        FrogPhase _frogPhase;
        /// <summary>0 at the moon gate, 1 at the pond.</summary>
        float _frogT, _frogGateAlpha;
        /// <summary>How long the walk from the gate to the pond takes.</summary>
        public const float FrogWalkSeconds = 3f;
        /// <summary>Where the small moon gate opens for it, on the grass past the pond's top left, and where it stands to wait, on the pond's near rim.</summary>
        public static Vector2 FrogGateSpot => PondSpot + new Vector2(190f, 150f);
        public static Vector2 FrogPondSpot => PondSpot + new Vector2(-10f, -100f);
        /// <summary>The green its borrowed wool hat is dyed to make a frog's.</summary>
        public static readonly Color FrogGreen = new Color(0.52f, 0.82f, 0.42f);
        /// <summary>True while the Frog Rabbit is in the garden: coming, waiting at the pond or going home.</summary>
        public bool FrogShown => _frog != null && _frog.gameObject.activeSelf;
        /// <summary>True once it stands at the pond.</summary>
        public bool FrogWaiting => _frogPhase == FrogPhase.Waiting;
        public RectTransform FrogRect => _frog;

        /// <summary>A cotton rabbit in the wool hat dyed frog green with two eyes on top, its name under it, and the moon gate it steps out of.</summary>
        void BuildFrog(GameCatalog catalog)
        {
            _frogPhase = FrogPhase.Away;
            _frogT = _frogGateAlpha = 0f;
            _frogGate = UiKit.Rect("FrogGate", _street).Place(0f, 0f, FrogGateSpot.x, FrogGateSpot.y, 150f, 170f);
            _frogGate.pivot = new Vector2(0.5f, 0f);
            _frogGateImage = _frogGate.AddImage(catalog.MoonGate != null ? catalog.MoonGate : catalog.Glow, new Color(1f, 1f, 1f, 0f));
            _frogGateImage.preserveAspect = true;
            _frogGate.gameObject.SetActive(false);

            const float Box = 124f;
            _frog = UiKit.Rect("Frog", _street).Place(0f, 0f, FrogGateSpot.x, FrogGateSpot.y, Box, 132f);
            _frog.pivot = new Vector2(0.5f, 0f);
            UiKit.Rect("Shadow", _frog).Place(0.5f, 0f, 0, 4, 84, 20).AddImage(catalog.Round, Palette.Alpha(Palette.Night, 0.35f));
            _frogArt = UiKit.Rect("Art", _frog).Stretch(0, 0, 1, 1);
            _frogArt.pivot = new Vector2(0.5f, 0f);
            Image art = _frogArt.AddImage(catalog.KindSprite(FrogKind) ?? catalog.Rabbit, Color.white);
            art.preserveAspect = true;
            art.raycastTarget = true;
            var button = _frog.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = art;
            button.onClick.AddListener(OnFrogTap);
            // The wool hat, dyed green, with two round eyes on its crown: a frog's hat, drawn by code until one is painted.
            RabbitKind kind = RabbitKinds.Find(FrogKind);
            Sprite hat = catalog.ItemIcon(FrogHat);
            if (hat != null && kind != null)
            {
                var hatOn = UiKit.Rect("Hat", _frogArt);
                Image hatImage = hatOn.AddImage(hat, FrogGreen);
                hatImage.preserveAspect = true;
                float across = (GameCatalog.KindHeadX - 0.5f + kind.HatX) * Box * GameCatalog.KindAspect;
                RabbitGridView.PlaceHat(hatOn, hat, Box, across, (GameCatalog.KindHeadTop + kind.HatY) * Box, catalog.ItemHatLift(FrogHat));
                for (int side = -1; side <= 1; side += 2)
                {
                    var eye = UiKit.Rect(side < 0 ? "Eye L" : "Eye R", hatOn).Place(0.5f, 1f, side * hatOn.sizeDelta.x * 0.24f, -4f, 20f, 20f);
                    eye.AddImage(catalog.Round, Palette.Paper);
                    UiKit.Rect("Pupil", eye).Place(0.5f, 0.5f, side * 1.5f, -1f, 9f, 9f).AddImage(catalog.Round, Palette.Ink);
                }
            }
            var plate = UiKit.Rect("Name", _frog).Place(0.5f, 0f, 0, -16, 150, 34);
            plate.AddImage(catalog.Panel, Palette.Mix(Palette.Paper, FrogGreen, 0.25f), true);
            _frogName = UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1, 8, 0, 8, 0).AddText(L10n.T("frog.name"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _frogName.resizeTextForBestFit = true;
            _frogName.resizeTextMinSize = 12;
            _frogName.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _frog.gameObject.SetActive(false);
        }

        /// <summary>The first look of the Frog Rabbit: Thỏ Bông in the Mũ len, dyed green.</summary>
        const string FrogKind = "tho-bong", FrogHat = "hat-mu-len";

        /// <summary>Open-play time goes to the frog's clock; it walks from the gate to the pond, waits, and walks back.</summary>
        void UpdateFrog(float dt)
        {
            if (_frog == null) return;
            GameState s = Runner.State;
            if (Shell.State == ShellState.Playing && !PhotoMode)
            {
                FrogEvent e = Runner.AdvanceFrog(dt, ClockHours());
                if (e == FrogEvent.Arrived) Sfx.Blip();
            }
            bool here = FrogRabbit.Here(s);
            if (here && (_frogPhase == FrogPhase.Away || _frogPhase == FrogPhase.Going))
            {
                // A frog has come (or one was here when the garden was saved, or the screen rebuilt): it walks in.
                bool fresh = s.FrogLeft > FrogRabbit.StaySeconds - 1d;
                if (_frogPhase == FrogPhase.Away) _frogT = fresh ? 0f : 1f;
                _frogPhase = _frogT >= 1f ? FrogPhase.Waiting : FrogPhase.Coming;
                _frog.gameObject.SetActive(true);
                _frogName.text = L10n.T("frog.name"); // in the language of the moment it comes
                _frogGate.gameObject.SetActive(true);
                _frogGate.SetAsLastSibling();
                _frog.SetAsLastSibling();
            }
            else if (!here && (_frogPhase == FrogPhase.Coming || _frogPhase == FrogPhase.Waiting)) _frogPhase = FrogPhase.Going;

            if (_frogPhase == FrogPhase.Coming || _frogPhase == FrogPhase.Going)
            {
                float dir = _frogPhase == FrogPhase.Coming ? 1f : -1f;
                _frogT = Mathf.Clamp01(_frogT + dir * dt / Motion.Time(FrogWalkSeconds));
                if (_frogPhase == FrogPhase.Coming && _frogT >= 1f) _frogPhase = FrogPhase.Waiting;
                else if (_frogPhase == FrogPhase.Going && _frogT <= 0f)
                {
                    _frogPhase = FrogPhase.Away;
                    _frog.gameObject.SetActive(false);
                }
            }
            if (_frogPhase != FrogPhase.Away) PlaceFrog();
            // The gate shows while it is on its way in or out, and fades when it is not needed.
            float gate = _frogPhase == FrogPhase.Coming || _frogPhase == FrogPhase.Going || (_frogPhase == FrogPhase.Waiting && _frogT < 1f) ? 1f : 0f;
            if (_frogPhase == FrogPhase.Waiting) gate = 0f;
            _frogGateAlpha = Motion.Reduced ? gate : Mathf.MoveTowards(_frogGateAlpha, gate, dt / 0.5f);
            _frogGateImage.color = new Color(1f, 1f, 1f, 0.85f * _frogGateAlpha);
            bool gateShown = _frogGateAlpha > 0f;
            if (_frogGate.gameObject.activeSelf != gateShown) _frogGate.gameObject.SetActive(gateShown);
        }

        void PlaceFrog()
        {
            Vector2 at = Vector2.Lerp(FrogGateSpot, FrogPondSpot, Ease.InOutCubic(_frogT));
            bool walking = _frogPhase == FrogPhase.Coming || _frogPhase == FrogPhase.Going;
            if (walking && !Motion.Reduced) at.y += Mathf.Abs(Mathf.Sin(_frogT * Mathf.PI * 7f)) * 16f; // little frog hops
            _frog.anchoredPosition = at;
            _frogArt.localScale = new Vector3(_frogPhase == FrogPhase.Going ? -1f : 1f, 1f, 1f);
            if (_frogPhase == FrogPhase.Waiting && !Motion.Reduced) _frogArt.localScale = new Vector3(1f, 1f + 0.025f * Mathf.Sin(_clock * 2.6f), 1f);
        }

        /// <summary>A touch greets it: ten minutes of income fly to the purse, a clover to the rabbits tab when one comes, and it hops home.</summary>
        void OnFrogTap()
        {
            if (_frogPhase != FrogPhase.Coming && _frogPhase != FrogPhase.Waiting) return;
            Vector2 from = Feel.PointOf(_frog, new Vector2(0, 90));
            if (!Runner.GreetFrog(ClockHours(), out BigNum paid, out int clovers)) return;
            GameCatalog catalog = Runner.Catalog;
            Sfx.Reward();
            Haptics.Light();
            Feel.FloatText(from + new Vector2(50, 40), "♥", Palette.Blush, 50);
            Feel.FloatText(from + new Vector2(-40, 70), "♥", Palette.Blush, 36);
            string gained = "+" + paid.Short();
            Feel.Fly(from, Feel.PointOf(_purse), catalog.Currency, Color.white, 6, 44f, () =>
            {
                if (_purse != null) Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), gained, Palette.Carrot, 44);
            });
            if (clovers > 0)
            {
                RectTransform tab = _tabIcons[3].rectTransform;
                Feel.Fly(from, Feel.PointOf(tab), catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, clovers, 38f,
                    () => { if (tab != null) Feel.FloatText(Feel.PointOf(tab, new Vector2(0, 90)), "+" + clovers, Palette.Moss, 40); });
            }
            Feel.Moment(L10n.T("frog.title"), clovers > 0 ? L10n.T("frog.sub.clover", paid.Short()) : L10n.T("frog.sub", paid.Short()));
            // A happy hop on the spot, then the walk home (the state already says it is going).
            RectTransform artRect = _frogArt;
            Tweens.Add(Motion.Time(0.4f), t => { if (artRect != null) artRect.anchoredPosition = new Vector2(0f, 40f * Ease.Pop(t)); }, Ease.Linear, 0f,
                () => { if (artRect != null) artRect.anchoredPosition = Vector2.zero; });
            _frogPhase = FrogPhase.Going;
        }

        #endregion

        #region Ring expansion (#53)

        /// <summary>The sign at the lawn's edge that widens the forest: it stands there while the garden can widen, and a press opens the next ring.</summary>
        RectTransform _forestSign;
        ChunkyButton _forestSignButton;
        Text _forestSignText;
        /// <summary>Tufts of new grass for every segment the garden can still open, the first first; hidden until their segment opens.</summary>
        readonly List<CanvasGroup> _ringGrass = new List<CanvasGroup>();
        /// <summary>The lawn's open segments as last drawn, and the plots the sign was last placed for.</summary>
        int _ringShown = -1, _signPlots = -1;
        const float ForestSignW = 230f, ForestSignH = 104f, ForestPostH = 80f;
        /// <summary>The sign's middle stands this far in from the garden's side, on the edge line where the wood begins, so all of its board is in view.</summary>
        const float ForestSignOut = 50f;
        const float SignInk = 4f;
        /// <summary>New grass and the sign keep this far from the garden's top and bottom, where the wood closes over the lawn.</summary>
        const float RingGrassBand = 540f;
        const float StripHalfH = 1100f;

        /// <summary>Where the sign's foot stands in the garden: at the lawn's edge, beside the land the next segment opens.</summary>
        public Vector2 ForestSignSpot { get; private set; }

        /// <summary>
        /// Where new grass comes up when a segment opens: on the segment's tiles that lie clear of every station, seat and
        /// prop, and at the lawn's edge where a piece of the rim stood before it stepped back for this segment. Only rows the view
        /// can bring clear of the tab bar and the HUD at every zoom; the wood closes over the rest.
        /// </summary>
        List<Vector2> RingGrassSpots(int segment, int stations)
        {
            var spots = new List<Vector2>();
            GardenSide band = GardenGrid.SideOf(segment);
            bool aside = band == GardenSide.Left || band == GardenSide.Right;
            void Add(Vector2 at)
            {
                if (aside ? Mathf.Abs(at.y - Heart.y) > StripHalfH - RingGrassBand : at.y < RingGrassBand || at.y > WorldH - RingGrassBand) return;
                foreach (Vector2 other in spots) if (Vector2.Distance(other, at) < 60f) return;
                spots.Add(at);
            }
            foreach (Hex h in GardenGrid.SegmentTiles(segment))
            {
                Vector2 at = CellToWorld(h) + new Vector2(0f, -60f);
                if (RingGrassClear(at, stations)) Add(at);
            }
            int before = Expansion.PlotsForSegments(Runner.Defs, segment - 1), after = Expansion.PlotsForSegments(Runner.Defs, segment);
            foreach (RimPiece piece in _rim)
            {
                float from = RimX(piece, before);
                if (Mathf.Abs(RimX(piece, after) - from) < 20f) continue;
                Add(new Vector2(Mathf.Clamp(from - piece.Side * piece.Width / 2f, 30f, WorldW - 30f), piece.Y)); // its edge nearest the lawn
            }
            return spots;
        }

        /// <summary>A tile's foot is clear for grass: inside the lawn, off every station's ground, and off the seats, decorations and props.</summary>
        bool RingGrassClear(Vector2 at, int stations)
        {
            if (at.x < 90f || at.x > WorldW - 90f) return false;
            for (int i = 0; i < stations; i++)
            {
                Vector2 d = at - (StationSpot(i) + new Vector2(0f, -60f));
                if (d.x * d.x / (170f * 170f) + d.y * d.y / (140f * 140f) < 1f) return false;
            }
            for (int k = 0; k < RestSpots.Max; k++) if (Vector2.Distance(RestSpot(k), at) < 110f) return false;
            for (int k = 0; k < Decor.Spots; k++) if (Vector2.Distance(DecorSpotAt(k), at) < 90f) return false;
            return Vector2.Distance(at, PondSpot) > 260f && Vector2.Distance(at, JarSpot) > 140f && Vector2.Distance(at, MortarSpot) > 150f && Vector2.Distance(at, ShopSpot) > 180f;
        }

        /// <summary>A rectangle of the garden, in its own units, from anything standing on it.</summary>
        Rect StreetRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 a = _street.InverseTransformPoint(corners[0]), b = _street.InverseTransformPoint(corners[2]);
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>
        /// Where the sign's foot stands for the next segment, whose land holds station <paramref name="next"/>: at the open
        /// lawn's edge on the side the segment opens, at the height of that station, moved up or down the edge until it covers
        /// no plate in the garden.
        /// </summary>
        Vector2 ForestSignSpotFor(int next, int segments)
        {
            Vector2 plot = StationSpot(next);
            // Just inside the lawn's end, so the whole board is in view when the garden looks at that end.
            GardenSide side = GardenGrid.SideOf(segments + 1);
            if (side == GardenSide.Up || side == GardenSide.Down) return BandSignSpot(Heart.x, segments, side);
            float x = LawnEdgeX(segments, side) - (side == GardenSide.Left ? -1f : 1f) * (ForestSignW / 2f + 10f);
            // Whole in the opening view when it stands in it, and short of the trees at the garden's ends (#16).
            float teapot = StationSpot(0).x, reach = OpeningReach - ForestSignW / 2f;
            if (Mathf.Abs(x - teapot) < OpeningReach) x = Mathf.Clamp(x, teapot - reach, teapot + reach);
            x = Mathf.Clamp(x, WorldW - ReadableRight + ForestSignW / 2f, ReadableRight - ForestSignW / 2f);
            var plates = new List<Rect>();
            foreach (RectTransform rt in _street.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "Level" || rt.name == "Plate") plates.Add(StreetRect(rt));
            float low = RingGrassBand, high = WorldH - RingGrassBand - ForestSignH - ForestPostH, start = Mathf.Clamp(plot.y - 150f, low, high);
            for (int n = 0; n < 60; n++)
            {
                float y = Mathf.Clamp(start + (n % 2 == 0 ? -1f : 1f) * ((n + 1) / 2) * 30f, low, high);
                var sign = new Rect(x - ForestSignW / 2f - 8f, y - 8f, ForestSignW + 16f, ForestSignH + ForestPostH + 16f);
                bool clear = true;
                foreach (Rect plate in plates) if (plate.Overlaps(sign)) { clear = false; break; }
                if (clear) return new Vector2(x, y);
            }
            return new Vector2(x, start);
        }

        /// <summary>The x of the open lawn's edge on one side with this many segments open: half a tile past the last tile of its rows.</summary>
        Vector2 BandSignSpot(float near, int segments, GardenSide side)
        {
            float edge = LawnEdgeY(segments, side), tall = ForestSignH + ForestPostH;
            float y = side == GardenSide.Up ? edge - tall - 40f : edge + 40f;
            Vector2 span = SegmentSpan(segments + 1);
            float low = span.x + 200f + ForestSignW / 2f, high = span.y - 200f - ForestSignW / 2f, start = Mathf.Clamp(near, low, high);
            var plates = new List<Rect>();
            foreach (RectTransform rt in _street.GetComponentsInChildren<RectTransform>(true))
                if (rt.name == "Level" || rt.name == "Plate") plates.Add(StreetRect(rt));
            for (int n = 0; n < 80; n++)
            {
                float x = Mathf.Clamp(start + (n % 2 == 0 ? -1f : 1f) * ((n + 1) / 2) * 30f, low, high);
                var sign = new Rect(x - ForestSignW / 2f - 160f, y - 60f, ForestSignW + 320f, tall + 120f);
                bool clear = true;
                foreach (Rect plate in plates) if (plate.Overlaps(sign)) { clear = false; break; }
                if (clear) return new Vector2(x, y);
            }
            return new Vector2(start, y);
        }

        static float LawnEdgeX(int segments, GardenSide side)
        {
            float half = (GardenGrid.Reach(segments, side == GardenSide.Left ? GardenSide.Left : GardenSide.Right) + 1) * (float)GardenGrid.PitchX / 2f;
            return Heart.x + (side == GardenSide.Left ? -half : half);
        }

        void BuildForestRing(GameCatalog catalog, int stations)
        {
            _ringGrass.Clear();
            _ringShown = _signPlots = -1;
            _mist.Clear();
            for (int n = 1; n <= Expansion.LastSegments(Runner.Defs); n++)
            {
                _ringGrass.Add(BuildRingGrass(catalog, n, stations));
                _mist.Add(BuildMist(catalog, n));
            }

            // On the ground, where it can be read (it does not drift with the far wood); brought to the front when it shows, so nothing covers it.
            Transform parent = _street;
            _forestSign = UiKit.Rect("Forest Sign", parent).Place(0f, 0f, 0f, 0f, ForestSignW, ForestSignH + ForestPostH);
            _forestSign.pivot = new Vector2(0.5f, 0f);
            UiKit.Rect("Shadow", _forestSign).Place(0.5f, 0f, 4f, 6f, 120f, 34f)
                .AddImage(catalog.Glow, Palette.Alpha(Palette.Night, 0.35f)).raycastTarget = false;
            UiKit.Rect("Post Ink", _forestSign).Place(0.5f, 0f, 0f, ForestPostH / 2f + 4f, 22f + 2f * SignInk, ForestPostH + 8f + 2f * SignInk)
                .AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.85f), true).raycastTarget = false;
            UiKit.Rect("Post", _forestSign).Place(0.5f, 0f, 0f, ForestPostH / 2f + 4f, 22f, ForestPostH + 8f)
                .AddImage(catalog.Panel, Palette.Mix(Palette.Carrot, Palette.Ink, 0.55f), true).raycastTarget = false;
            UiKit.Rect("Board Ink", _forestSign).Place(0.5f, 1f, 0f, -ForestSignH / 2f, ForestSignW + 2f * SignInk, ForestSignH + 2f * SignInk)
                .AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.85f), true).raycastTarget = false;
            var board = UiKit.Rect("Board", _forestSign).Place(0.5f, 1f, 0f, -ForestSignH / 2f, ForestSignW, ForestSignH);
            _forestSignButton = UiKit.Chunky(board, catalog.Panel, OnForestSign);
            _forestSignText = UiKit.Rect("Text", _forestSignButton.FaceRect).Stretch(0, 0, 1, 1, 14, 6, 14, 6)
                .AddText(L10n.T("garden.expand"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _forestSignText.resizeTextForBestFit = true;
            _forestSignText.resizeTextMinSize = 16;
            _forestSignText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _forestSign.gameObject.SetActive(false);
        }

        /// <summary>The mist over each segment still to open, the first first: shown while the segment is shut, cleared when it opens.</summary>
        readonly List<CanvasGroup> _mist = new List<CanvasGroup>();
        /// <summary>How long the mist of a segment takes to clear.</summary>
        public const float MistClear = 1.4f;

        /// <summary>The x range of the land segment <paramref name="segment"/> opens, in garden units: half a tile past each of its outer tiles.</summary>
        public static Vector2 SegmentSpan(int segment)
        {
            GardenSide side = GardenGrid.SideOf(segment);
            if (side == GardenSide.Up || side == GardenSide.Down)
                return new Vector2(LawnEdgeX(segment, GardenSide.Left), LawnEdgeX(segment, GardenSide.Right));
            float inner = (GardenGrid.Reach(segment - 1, side) + 1) * (float)GardenGrid.PitchX / 2f;
            float outer = (GardenGrid.Reach(segment, side) + 1) * (float)GardenGrid.PitchX / 2f;
            return side == GardenSide.Left ? new Vector2(Heart.x - outer, Heart.x - inner) : new Vector2(Heart.x + inner, Heart.x + outer);
        }

        /// <summary>The rows of the strip, top and bottom, with the art that stands on them: what the mist covers top to bottom.</summary>
        static readonly float MistLow = -760f, MistHigh = 860f;

        static Vector2 MistRows(int segment)
        {
            GardenSide side = GardenGrid.SideOf(segment);
            float pitch = (float)GardenGrid.PitchY, middleEdge = pitch * (GardenGrid.HalfRows + 0.5f);
            if (side == GardenSide.Up)
                return new Vector2(pitch * (GardenGrid.Reach(segment - 1, side) + 0.5f), pitch * (GardenGrid.Reach(segment, side) + 0.5f) + MistHigh - middleEdge);
            if (side == GardenSide.Down)
                return new Vector2(-pitch * (GardenGrid.Reach(segment, side) + 0.5f) + MistLow + middleEdge, -pitch * (GardenGrid.Reach(segment - 1, side) + 0.5f));
            return new Vector2(MistLow, MistHigh);
        }

        /// <summary>
        /// Soft mist over one segment's land: watercolour washes of pale blue-white that overlap into a cloud bank, a little
        /// over the edge on both sides so no seam shows. It hides the dashed plots and the grass that wait under it.
        /// </summary>
        CanvasGroup BuildMist(GameCatalog catalog, int segment)
        {
            Vector2 span = SegmentSpan(segment), rows = MistRows(segment);
            float low = rows.x, high = rows.y;
            bool aside = GardenGrid.SideOf(segment) == GardenSide.Right || GardenGrid.SideOf(segment) == GardenSide.Left;
            var mist = UiKit.Rect("Segment Mist " + segment, _street).Stretch(0, 0, 1, 1);
            CanvasGroup group = mist.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            Color[] washes = { Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.12f), 0.9f), Palette.Alpha(Palette.Mix(Color.white, Palette.Moss, 0.14f), 0.8f), Palette.Alpha(Palette.Mix(Palette.Paper, Color.white, 0.6f), 0.85f) };
            // A broad veil first, so the land reads as hidden; the washes over it make the cloud bank's soft shapes.
            UiKit.Rect("Veil", mist).Place(0f, 0f, (span.x + span.y) / 2f, Heart.y + (low + high) / 2f, span.y - span.x + 320f, high - low + 300f)
                .AddImage(catalog.Glow, Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.18f), 0.75f)).raycastTarget = false;
            // The bank at the edge it shares with the open lawn (#16): thick, with a wandering edge like a wash that dried, so the
            // land still to open reads as such on the dark grass. The washes over it keep it soft.
            bool right = GardenGrid.SideOf(segment) == GardenSide.Right;
            float inner = right ? span.x : span.y, inward = right ? 1f : -1f;
            if (aside)
                UiKit.Rect("Edge", mist).Place(0f, 0f, inner + inward * (MistEdgeWidth / 2f - MistEdgeOut), Heart.y + (low + high) / 2f, MistEdgeWidth, high - low + 200f)
                    .AddImage(MistEdgeSprite(right), Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.12f), 0.88f)).raycastTarget = false;
            else
            {
                bool up = GardenGrid.SideOf(segment) == GardenSide.Up;
                float lawn = LawnEdgeY(segment - 1, up ? GardenSide.Up : GardenSide.Down), outward = up ? 1f : -1f;
                RectTransform bank = UiKit.Rect("Edge", mist).Place(0f, 0f, (span.x + span.y) / 2f, lawn + outward * (MistEdgeWidth / 2f - MistEdgeOut), MistEdgeWidth, span.y - span.x + 400f);
                bank.localRotation = Quaternion.Euler(0f, 0f, up ? 90f : -90f);
                bank.AddImage(MistEdgeSprite(true), Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.12f), 0.88f)).raycastTarget = false;
            }
            int Across = aside ? 3 : Mathf.CeilToInt((span.y - span.x) / 300f), Down = 9;
            for (int a = 0; a < Across; a++)
                for (int d = 0; d < Down; d++)
                {
                    float seed = segment * 13.1f + a * 3.7f + d * 1.9f;
                    float x = Mathf.Lerp(span.x, span.y, (a + 0.5f) / Across) + (Hash(seed) - 0.5f) * 60f;
                    float y = Heart.y + Mathf.Lerp(low + 120f, high - 120f, d / (Down - 1f)) + (Hash(seed + 1f) - 0.5f) * 40f;
                    float w = 330f + Hash(seed + 2f) * 120f, h = 300f + Hash(seed + 3f) * 90f;
                    UiKit.Rect("Wash", mist).Place(0f, 0f, x, y, w, h).AddImage(catalog.Glow, washes[(a + d) % washes.Length]).raycastTarget = false;
                }
            mist.gameObject.SetActive(true);
            return group;
        }

        /// <summary>The mist's bank at a shut segment's edge: how wide it is, and how far it reaches over the open lawn at most.</summary>
        const float MistEdgeWidth = 330f, MistEdgeOut = 40f;
        static Sprite _mistEdgeRight, _mistEdgeLeft;

        /// <summary>
        /// The bank's picture, painted once: white, clear on the lawn's side, rising to full over a short wandering edge, then
        /// fading into the segment's own washes, with a little grain, and fading out at the top and the bottom.
        /// </summary>
        static Sprite MistEdgeSprite(bool right)
        {
            Sprite cached = right ? _mistEdgeRight : _mistEdgeLeft;
            if (cached != null) return cached;
            const int W = 64, H = 256;
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false) { name = "Mist Edge", wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[W * H];
            for (int y = 0; y < H; y++)
            {
                float v = (y + 0.5f) / H;
                float wander = 0.5f + 0.25f * Mathf.Sin(v * 17.3f) + 0.15f * Mathf.Sin(v * 41.9f + 1.3f) + 0.1f * Mathf.Sin(v * 97.1f + 0.7f);
                float edge = 0.08f * wander; // where the bank begins, from the lawn's side: never past the first tenth
                float ends = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(v / 0.1f)) * Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - v) / 0.1f));
                for (int x = 0; x < W; x++)
                {
                    float u = (x + 0.5f) / W, from = right ? u : 1f - u;
                    float rise = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((from - edge) / 0.12f));
                    float fade = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((from - 0.55f) / 0.45f));
                    float grain = 0.92f + 0.08f * Mathf.Sin(u * 31f + v * 53f) * Mathf.Sin(v * 23f - u * 11f);
                    pixels[y * W + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(255f * rise * fade * ends * grain));
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply(false, false);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "Mist Edge";
            if (right) _mistEdgeRight = sprite; else _mistEdgeLeft = sprite;
            return sprite;
        }

        /// <summary>The mist of segment <paramref name="segment"/> (1 for the first), for the tests and previews.</summary>
        public CanvasGroup MistOf(int segment) => segment >= 1 && segment <= _mist.Count ? _mist[segment - 1] : null;

        /// <summary>A few blades of fresh grass and now and then a flower on each of a segment's grass spots: hidden until the segment opens, and then they stay.</summary>
        CanvasGroup BuildRingGrass(GameCatalog catalog, int segment, int stations)
        {
            var ring = UiKit.Rect("Segment Grass " + segment, _street).Stretch(0, 0, 1, 1);
            CanvasGroup group = ring.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            Color[] blades = { Palette.Alpha(Palette.Moss, 0.8f), Palette.Alpha(Palette.Mix(Palette.Moss, Palette.Paper, 0.3f), 0.75f), Palette.Alpha(Palette.Mix(Palette.Moss, Palette.Grass, 0.4f), 0.85f) };
            Color[] petals = { Palette.Blush, Palette.Lantern, Palette.Paper };
            List<Vector2> spots = RingGrassSpots(segment, stations);
            for (int n = 0; n < spots.Count; n++)
            {
                Vector2 at = spots[n];
                var tuft = UiKit.Rect("Tuft " + n, ring).Place(0f, 0f, at.x, at.y, 80f, 50f);
                tuft.pivot = new Vector2(0.5f, 0f);
                float seed = at.x * 0.13f + at.y * 0.077f + segment;
                for (int k = 0; k < 5; k++)
                {
                    float x = (k - 2f) * 13f + (Hash(seed + k) - 0.5f) * 8f, size = 26f + Hash(seed + k + 3f) * 16f;
                    var blade = UiKit.Rect("Blade", tuft).Place(0.5f, 0f, x, size * 0.4f, size * 0.35f, size);
                    blade.localRotation = Quaternion.Euler(0, 0, (k - 2f) * -9f + (Hash(seed + k + 6f) - 0.5f) * 10f);
                    blade.AddImage(catalog.Round, blades[k % blades.Length]).raycastTarget = false;
                }
                if (Hash(seed + 9f) < 0.5f)
                    UiKit.Rect("Petal", tuft).Place(0.5f, 0f, (Hash(seed + 10f) - 0.5f) * 40f, 34f, 14f, 14f)
                        .AddImage(catalog.Round, Palette.Alpha(petals[(int)(Hash(seed + 11f) * petals.Length) % petals.Length], 0.9f)).raycastTarget = false;
            }
            ring.gameObject.SetActive(false);
            return group;
        }

        /// <summary>
        /// The grass of the segments the lawn holds, and the sign. When a segment opens its grass comes up tile by tile from the
        /// middle outward while the rim steps back (<see cref="RefreshLand"/>); under reduced motion, and when the screen first
        /// shows, it is simply there. The sign stands at the lawn's edge beside the next segment's land while the garden can widen.
        /// </summary>
        void RefreshForestRing(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (_forestSign == null) return;
            int radius = Expansion.Segments(s);
            if (radius != _ringShown)
            {
                bool growing = _ringShown >= 0 && radius > _ringShown && !Motion.Reduced;
                for (int k = 0; k < _ringGrass.Count; k++)
                {
                    int r = 1 + k;
                    CanvasGroup group = _ringGrass[k];
                    bool open = r <= radius;
                    if (k < _mist.Count) ClearMist(_mist[k], open, growing && r > _ringShown);
                    if (group.gameObject.activeSelf != open) group.gameObject.SetActive(open);
                    if (!open) continue;
                    if (!growing || r <= _ringShown)
                    {
                        foreach (Transform tuft in group.transform) tuft.localScale = Vector3.one;
                        continue;
                    }
                    int n = 0;
                    foreach (Transform child in group.transform)
                    {
                        Transform tuft = child;
                        tuft.localScale = Vector3.zero;
                        Tweens.Add(RimStep, t => { if (tuft != null) tuft.localScale = Vector3.one * Mathf.Max(0f, t); }, Ease.OutBack, RimStep / 3f + 0.04f * n++,
                            () => { if (tuft != null) tuft.localScale = Vector3.one; });
                    }
                }
                _ringShown = radius;
            }

            bool can = Expansion.CanExpand(defs, s);
            if (can && s.Plots != _signPlots)
            {
                // At the lawn's edge on the side the next segment opens, clear of every plate.
                ForestSignSpot = ForestSignSpotFor(Mathf.Min(s.Plots, defs.Count - 1), Expansion.Segments(s));
                _forestSign.anchoredPosition = ForestSignSpot;
                _signPlots = s.Plots;
            }
            if (_forestSign.gameObject.activeSelf == can) return;
            _forestSign.gameObject.SetActive(can);
            if (can) _forestSign.SetAsLastSibling();
            if (_forestSignText != null) _forestSignText.text = L10n.T("garden.expand");
            RectTransform sign = _forestSign;
            if (can && !Motion.Reduced)
                Tweens.Add(Motion.Panel, t => { if (sign != null) sign.localScale = Vector3.one * Mathf.Max(0f, t); }, Ease.OutBack, 0f, () => { if (sign != null) sign.localScale = Vector3.one; });
            else sign.localScale = Vector3.one;
        }

        /// <summary>Shows a segment's mist while it is shut; when it opens the mist thins and drifts out, or simply goes under reduced motion.</summary>
        void ClearMist(CanvasGroup mist, bool open, bool animate)
        {
            if (!open)
            {
                mist.gameObject.SetActive(true);
                mist.alpha = 1f;
                mist.transform.localScale = Vector3.one;
                return;
            }
            if (!mist.gameObject.activeSelf) return;
            if (!animate || Motion.Reduced)
            {
                mist.gameObject.SetActive(false);
                return;
            }
            Transform rt = mist.transform;
            var washes = new List<RectTransform>();
            var from = new List<Vector2>();
            foreach (Transform child in rt) { washes.Add((RectTransform)child); from.Add(((RectTransform)child).anchoredPosition); }
            Tweens.Add(MistClear, t =>
            {
                if (mist == null) return;
                mist.alpha = 1f - t;
                for (int i = 0; i < washes.Count; i++)
                    if (washes[i] != null) washes[i].anchoredPosition = from[i] + new Vector2((Hash(i * 2.3f) - 0.5f) * 160f, 40f + Hash(i * 1.7f) * 80f) * t;
            }, Ease.OutCubic, RimStep / 3f, () =>
            {
                if (mist == null) return;
                mist.gameObject.SetActive(false);
                mist.alpha = 1f;
                for (int i = 0; i < washes.Count; i++) if (washes[i] != null) washes[i].anchoredPosition = from[i];
            });
        }

        /// <summary>The sign is pressed: the next segment opens, the mist clears and the new grass comes up.</summary>
        void OnForestSign()
        {
            if (!Expansion.CanExpand(Runner.Defs, Runner.State)) return;
            _forestSignButton.Press(Tweens);
            OnBuyPlot();
        }

        #endregion

        #region Layout mode (#54)

        /// <summary>How long a finger rests on something before it is picked up in the arranging mode.</summary>
        public const float LayoutHoldSeconds = 0.3f;
        /// <summary>Remembered on the device once the arranging mode's one-line hint has been shown.</summary>
        public const string LayoutHintKey = "layout.hint";
        /// <summary>The share of the tab bar the arranging button takes, at its left beside the stations tab.</summary>
        const float LayoutSlot = 0.16f;
        /// <summary>How far a finger may wander before the hold is up and still pick something up, in view units.</summary>
        const float LayoutSlop = 24f;

        /// <summary>The garden is being arranged: empty tiles show rings, things are picked up and dropped, nothing else answers.</summary>
        public bool LayoutMode { get; private set; }
        /// <summary>What is picked up right now (an occupant id of the grid), or null.</summary>
        public string LayoutLifted => _lift != null ? _lift.Id : null;

        /// <summary>One thing picked up: its tile, the objects that draw it and where each sits from the tile's point, and how it looked before.</summary>
        sealed class Lift
        {
            public string Id;
            public Hex From;
            public RectTransform[] Parts;
            public Vector2[] Offsets;
            public Vector3[] Scales;
            public int[] Siblings;
            public Vector2 Grab, Point;
            public StationActorView Actor;
            public float BaseScale;
            public GameObject Shadow;
        }

        /// <summary>A stored station's row on the stations tab, with its "Take out" button.</summary>
        sealed class StoredRow { public int Index; public GameObject Root; public Text Name; public string Shown; }

        Lift _lift;
        GameObject _layoutBar, _layoutCatcher, _layoutHint;
        RectTransform _layoutRings, _layoutTarget, _layoutStore;
        Image _layoutStoreFace;
        Text _layoutStoreCount;
        readonly Dictionary<Hex, GameObject> _ringAt = new Dictionary<Hex, GameObject>();
        readonly List<StoredRow> _storedRows = new List<StoredRow>();
        bool _layoutDirty, _layoutPressed, _layoutScrolling;
        float _layoutHeld;
        Vector2 _layoutPressView;

        /// <summary>Where tab slot <paramref name="k"/> (0 to 4, fractions allowed) starts on the tab bar, right of the arranging button.</summary>
        static float TabX(float k) => LayoutSlot + (1f - LayoutSlot) * k / TabCount;

        /// <summary>The "Bố trí" button at the left of the tab bar: a little honeycomb of tiles over its name.</summary>
        void BuildLayoutButton(GameCatalog catalog)
        {
            var rt = UiKit.Rect("Layout", _tabBar).Stretch(0, 0, LayoutSlot, 1);
            Image hit = rt.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            button.onClick.AddListener(() => { Sfx.Click(); EnterLayoutMode(); });
            var icon = UiKit.Rect("Icon", rt).Place(0.5f, 0f, 0, 100, 84, 84);
            Color tile = Palette.Mix(Palette.Moss, Palette.Ink, 0.15f), rim = Palette.Mix(Palette.Paper, Palette.Ink, 0.3f);
            for (int k = 0; k < 7; k++)
            {
                Vector2 at = k == 0 ? Vector2.zero : new Vector2(Mathf.Cos(k * Mathf.PI / 3f), Mathf.Sin(k * Mathf.PI / 3f)) * 27f;
                UiKit.Rect("Rim", icon).Place(0.5f, 0.5f, at.x, at.y, 28, 28).AddImage(catalog.Round, rim);
                UiKit.Rect("Tile", icon).Place(0.5f, 0.5f, at.x, at.y, 22, 22).AddImage(catalog.Round, k == 0 ? Palette.Lantern : tile);
            }
            UiKit.Rect("Label", rt).Stretch(0, 0, 1, 0, 4, 18, 4, -62)
                .AddText(L10n.T("layout.button"), (int)TabLabelSize, Palette.Mix(Palette.Ink, Palette.Paper, 0.22f), TextAnchor.MiddleCenter);
            UiKit.Rect("Rule", rt).Stretch(1, 0.2f, 1, 0.8f, -2, 0, 0, 0).AddImage(null, Palette.Alpha(Palette.Ink, 0.12f));
        }

        /// <summary>The arranging mode's parts: the rings on the lawn, a pane that takes every touch on the garden, the bar with the storage slot and "Xong", the hint.</summary>
        void BuildLayout(GameCatalog catalog)
        {
            LayoutMode = false;
            _lift = null;
            _layoutDirty = _layoutPressed = _layoutScrolling = false;
            _ringAt.Clear();

            _layoutRings = UiKit.Rect("LayoutRings", _street).Stretch(0, 0, 1, 1);
            _layoutRings.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            _layoutTarget = UiKit.Rect("Target", _layoutRings).Place(0f, 0f, 0f, 0f, 160f, 78f);
            DashedOval(_layoutTarget, catalog, Palette.Lantern, Palette.Alpha(Palette.Lantern, 0.35f), 18, 26f, 8f);
            _layoutTarget.gameObject.SetActive(false);
            _layoutRings.gameObject.SetActive(false);

            // Over the garden, under everything else: every touch on the garden comes here, so stations, plates and props are quiet.
            var catcher = UiKit.Rect("LayoutCatcher", _canvas).Stretch(0, 0, 1, 1);
            catcher.SetSiblingIndex(_street.parent.GetSiblingIndex() + 1);
            catcher.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var trigger = catcher.gameObject.AddComponent<EventTrigger>();
            AddDrag(trigger, EventTriggerType.PointerDown, e => LayoutPress(ScreenToView(((PointerEventData)e).position)));
            AddDrag(trigger, EventTriggerType.InitializePotentialDrag, e => { if (_scroll != null) _scroll.OnInitializePotentialDrag((PointerEventData)e); });
            AddDrag(trigger, EventTriggerType.BeginDrag, e =>
            {
                if (_lift != null || _scroll == null) return;
                _layoutScrolling = true; // moved before the hold was up: a look around the garden
                _glideRun++;
                _scroll.OnBeginDrag((PointerEventData)e);
            });
            AddDrag(trigger, EventTriggerType.Drag, e =>
            {
                var p = (PointerEventData)e;
                if (_lift != null) LayoutDragTo(ScreenToView(p.position));
                else if (_layoutScrolling && _scroll != null) _scroll.OnDrag(p);
            });
            AddDrag(trigger, EventTriggerType.EndDrag, e =>
            {
                if (_layoutScrolling && _scroll != null) _scroll.OnEndDrag((PointerEventData)e);
                _layoutScrolling = false;
            });
            AddDrag(trigger, EventTriggerType.PointerUp, e => LayoutRelease(ScreenToView(((PointerEventData)e).position)));
            AddDrag(trigger, EventTriggerType.Scroll, e => { if (_scroll != null) _scroll.OnScroll((PointerEventData)e); });
            _layoutCatcher = catcher.gameObject;
            _layoutCatcher.SetActive(false);

            // The bar stands where the tab bar does: the storage slot on the left, "Xong" on the right.
            var bar = UiKit.Rect("LayoutBar", _canvas).Stretch(0, 0, 1, 0, 20, 0, 20, 0);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.sizeDelta = new Vector2(bar.sizeDelta.x, TabBarHeight);
            bar.AddShadow(26f, 12f, 0.4f);
            UiKit.Rect("Lip", bar).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true).pixelsPerUnitMultiplier = 0.6f;
            Image plate = UiKit.Rect("Plate", bar).Stretch(0, 0, 1, 1, 0, 10, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            plate.pixelsPerUnitMultiplier = 0.6f;
            plate.raycastTarget = true;
            _layoutStore = UiKit.Rect("Store", bar).Place(0f, 0.5f, 236, 5, 432, 132);
            _layoutStoreFace = _layoutStore.AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Ink, 0.08f), true);
            _layoutStoreFace.raycastTarget = true;
            DashedOval(UiKit.Rect("Edge", _layoutStore).Stretch(0, 0, 1, 1, 6, 6, 6, 6), catalog, Palette.Alpha(Palette.Ink, 0.35f), new Color(0, 0, 0, 0), 26, 22f, 5f);
            // A small chest: a body, a lid and a clasp.
            var chest = UiKit.Rect("Chest", _layoutStore).Place(0f, 0.5f, 62, 0, 76, 64);
            Color wood = Palette.Mix(Palette.Carrot, Palette.Ink, 0.45f);
            UiKit.Rect("Body", chest).Stretch(0, 0, 1, 0.62f).AddImage(catalog.Panel, wood, true);
            UiKit.Rect("Lid", chest).Stretch(0, 0.58f, 1, 1).AddImage(catalog.Panel, Palette.Mix(wood, Palette.Paper, 0.2f), true);
            UiKit.Rect("Clasp", chest).Place(0.5f, 0.6f, 0, 0, 16, 18).AddImage(catalog.Round, Palette.Lantern);
            Text label = UiKit.Rect("Label", _layoutStore).Stretch(0, 0.46f, 1, 1, 116, 0, 14, 12)
                .AddText(L10n.T("layout.store"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            BestFit(label, 18, UiKit.TextBody);
            _layoutStoreCount = UiKit.Rect("Count", _layoutStore).Stretch(0, 0, 1, 0.46f, 116, 12, 14, 0)
                .AddText(L10n.T("layout.store.count", 0), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft);
            BestFit(_layoutStoreCount, 18, UiKit.TextSmall);
            var done = UiKit.Rect("Done", bar).Place(1f, 0.5f, -150, 9, 260, 112);
            ChunkyButton doneButton = UiKit.Chunky(done, catalog.Panel, () => { Sfx.Click(); ExitLayoutMode(); });
            Text doneText = UiKit.Rect("Text", doneButton.FaceRect).Stretch(0, 0, 1, 1, 12, 0, 12, 0)
                .AddText(L10n.T("layout.done"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            BestFit(doneText, 20, UiKit.TextTitle);
            _layoutBar = bar.gameObject;
            _layoutBar.SetActive(false);

            var hint = UiKit.Rect("LayoutHint", _canvas).Place(0.5f, 1f, 0, -110, 1010, 76);
            hint.AddShadow(14f, 8f, 0.28f);
            UiKit.Rect("Body", hint).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Paper, true);
            Text hintText = UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1, 24, 0, 24, 0)
                .AddText(L10n.T("layout.hint"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            BestFit(hintText, 18, UiKit.TextBody);
            hint.gameObject.AddComponent<CanvasGroup>().blocksRaycasts = false;
            _layoutHint = hint.gameObject;
            _layoutHint.SetActive(false);
        }

        /// <summary>A label that shrinks to one line between these sizes (scaled for large text) rather than spill.</summary>
        static void BestFit(Text text, int least, int most)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(most);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        /// <summary>A soft oval with a dashed rim, filling its rect: the mark of a tile in the arranging mode.</summary>
        static void DashedOval(RectTransform at, GameCatalog catalog, Color dash, Color fill, int dashes, float length, float width)
        {
            if (fill.a > 0f) UiKit.Rect("Fill", at).Stretch(0, 0, 1, 1).AddImage(catalog.Glow, fill).raycastTarget = false;
            Vector2 size = at.rect.size.sqrMagnitude > 1f ? at.rect.size : at.sizeDelta;
            for (int k = 0; k < dashes; k++)
            {
                float angle = k / (float)dashes * Mathf.PI * 2f;
                Vector2 o = new Vector2(Mathf.Cos(angle) * size.x / 2f, Mathf.Sin(angle) * size.y / 2f);
                Vector2 along = new Vector2(-Mathf.Sin(angle) * size.x, Mathf.Cos(angle) * size.y);
                var d = UiKit.Rect("Dash", at).Place(0.5f, 0.5f, o.x, o.y, length, width);
                d.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(along.y, along.x) * Mathf.Rad2Deg);
                d.AddImage(catalog.Round, dash).raycastTarget = false;
            }
        }

        /// <summary>A row at the foot of the stations tab for each station, shown while it is in storage, with "Lấy ra" to take it out.</summary>
        void BuildStoredRows(RectTransform list, GameCatalog catalog)
        {
            _storedRows.Clear();
            for (int i = 0; i < catalog.Stations.Length; i++)
            {
                int index = i;
                var row = UiKit.Rect("Stored " + catalog.Stations[i].Id, list);
                row.AddImage(catalog.Panel, Palette.CardLip, true);
                var layout = row.gameObject.AddComponent<LayoutElement>();
                layout.preferredHeight = 132;
                layout.minHeight = 132;
                UiKit.Rect("Card", row).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Night, 0.08f), true);
                Text name = UiKit.Rect("Name", row).Stretch(0, 0, 1, 1, 36, 8, 320, 0)
                    .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                BestFit(name, 18, UiKit.TextBody);
                var take = UiKit.Rect("TakeOut", row).Place(1f, 0.5f, -160, 4, 268, 100);
                ChunkyButton button = UiKit.Chunky(take, catalog.Panel, () => TakeOutStation(index));
                Text text = UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 10, 0, 10, 0)
                    .AddText(L10n.T("layout.takeout"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
                BestFit(text, 18, UiKit.TextBody);
                row.gameObject.SetActive(false);
                _storedRows.Add(new StoredRow { Index = i, Root = row.gameObject, Name = name });
            }
        }

        /// <summary>A stored station comes back onto the empty tile nearest the centre; a word when the lawn has no room.</summary>
        public void TakeOutStation(int index)
        {
            if (Runner.TakeOutStation(index))
            {
                Sfx.Blip();
                Haptics.Light();
                if (index < _actors.Count) _actors[index].Root.anchoredPosition = StationSpot(index); // the scenery around it follows at the next build
                Refresh();
                RefreshRings();
                return;
            }
            Sfx.Deny();
            if (Feel != null) Feel.FloatText(new Vector2(0, 300), L10n.T("layout.full"), Palette.Ink, 34);
        }

        /// <summary>What the arranging mode shows now: stored stations away, their rows on the stations tab, the storage count; the tray and hints hidden while arranging.</summary>
        void RefreshLayout(GameState s)
        {
            if (_layoutBar == null) return;
            IReadOnlyList<StationDef> defs = Runner.Defs;
            for (int i = 0; i < _actors.Count && i < defs.Count; i++)
            {
                bool want = i < _shownStations && !Layout.IsStored(defs, s, i);
                if (_actors[i].Root.gameObject.activeSelf != want) _actors[i].Root.gameObject.SetActive(want);
            }
            foreach (StoredRow row in _storedRows)
            {
                bool stored = Layout.IsStored(defs, s, row.Index);
                if (row.Root.activeSelf != stored) row.Root.SetActive(stored);
                if (!stored) continue;
                string text = L10n.T("layout.stored.row", L10n.Station(defs[row.Index].Id), s.Levels[row.Index]);
                if (row.Shown != text) { row.Shown = text; row.Name.text = text; }
            }
            _layoutStoreCount.text = L10n.T("layout.store.count", s.Grid != null ? s.Grid.StoredCount : 0);
            if (!LayoutMode) return;
            if (ProductTray.Root.gameObject.activeSelf) ProductTray.Root.gameObject.SetActive(false);
            if (_hint.activeSelf) _hint.SetActive(false);
        }

        /// <summary>Opens the arranging mode. Only while playing in the plain garden view.</summary>
        public void EnterLayoutMode()
        {
            if (LayoutMode || _layoutBar == null || Shell.State != ShellState.Playing || PhotoMode) return;
            if (Pound.IsOpen || Basket.IsOpen || Jar.IsOpen) return;
            LayoutMode = true;
            _layoutDirty = _layoutPressed = _layoutScrolling = false;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            DismissHint();
            if (DecorCard != null && DecorCard.IsOpen) DecorCard.Close();
            _hud.gameObject.SetActive(false);
            _tabBar.gameObject.SetActive(false);
            ProductTray.Root.gameObject.SetActive(false);
            Vector2 insets = SafeInsets();
            ((RectTransform)_layoutBar.transform).anchoredPosition = new Vector2(0f, insets.x + TabBarGap);
            ((RectTransform)_layoutHint.transform).anchoredPosition = new Vector2(0f, -110f - insets.y);
            _layoutBar.SetActive(true);
            _layoutCatcher.SetActive(true);
            _layoutRings.gameObject.SetActive(true);
            _layoutRings.SetAsLastSibling(); // over the scenery, so every free tile reads
            RefreshRings();
            bool first = PlayerPrefs.GetInt(LayoutHintKey, 0) == 0;
            _layoutHint.SetActive(first);
            if (first) PlayerPrefs.SetInt(LayoutHintKey, 1);
            Refresh();
        }

        /// <summary>Closes the arranging mode. When anything moved, the garden is built again around the new places.</summary>
        public void ExitLayoutMode()
        {
            if (!LayoutMode) return;
            if (_lift != null) SendBack(_lift);
            LayoutMode = false;
            _layoutPressed = _layoutScrolling = false;
            _layoutBar.SetActive(false);
            _layoutCatcher.SetActive(false);
            _layoutRings.gameObject.SetActive(false);
            _layoutHint.SetActive(false);
            _hud.gameObject.SetActive(true);
            _tabBar.gameObject.SetActive(true);
            if (_layoutDirty)
            {
                _layoutDirty = false;
                RebuildUi();
                return;
            }
            Refresh();
        }

        /// <summary>One frame: the hold that picks a thing up; and the mode closes if the game leaves the garden.</summary>
        void UpdateLayout(float dt)
        {
            if (!LayoutMode) return;
            if (Shell.State != ShellState.Playing || PhotoMode) { ExitLayoutMode(); return; }
            if (!_layoutPressed || _lift != null || _layoutScrolling) return;
            _layoutHeld += dt;
            if (_layoutHeld < LayoutHoldSeconds) return;
            _layoutPressed = false;
            string id = ItemAt(ViewToWorld(_layoutPressView));
            if (id != null) LiftUp(id, _layoutPressView);
        }

        // ----- the gesture: press, hold, drag, let go (the pane's events and the tests both come here) -----

        public void SimulateLayoutPress(Vector2 viewPoint) => LayoutPress(viewPoint);
        public void SimulateLayoutDrag(Vector2 viewPoint) => LayoutDragTo(viewPoint);
        public void SimulateLayoutRelease(Vector2 viewPoint) => LayoutRelease(viewPoint);

        void LayoutPress(Vector2 view)
        {
            if (!LayoutMode || _lift != null) return;
            _layoutPressed = true;
            _layoutScrolling = false;
            _layoutHeld = 0f;
            _layoutPressView = view;
        }

        void LayoutDragTo(Vector2 view)
        {
            if (_lift != null) { FollowFinger(view); return; }
            if (_layoutPressed && (view - _layoutPressView).magnitude > LayoutSlop) { _layoutPressed = false; _layoutScrolling = true; }
        }

        void LayoutRelease(Vector2 view)
        {
            _layoutPressed = false;
            if (_lift != null) Drop(view);
        }

        // ----- tiles and points -----

        /// <summary>A garden point as canvas units from the bottom left of the view, and back.</summary>
        Vector2 ViewToWorld(Vector2 view) => (view - _street.anchoredPosition) / _zoom;

        /// <summary>Where a tile's point is on the view right now (canvas units from the bottom left), for the tests and previews.</summary>
        public Vector2 LayoutViewPoint(Hex tile) => ToView(CellToWorld(tile));

        /// <summary>The middle of the storage slot on the view.</summary>
        public Vector2 LayoutStorePoint => ViewRect(_layoutStore).center;

        /// <summary>The tile whose point is nearest a garden point.</summary>
        public static Hex WorldToCell(Vector2 world)
        {
            Vector2 local = world - Heart;
            double r = -local.y / GardenGrid.PitchY, q = local.x / GardenGrid.PitchX - r / 2d, y = -q - r;
            double rq = System.Math.Round(q), rr = System.Math.Round(r), ry = System.Math.Round(y);
            double dq = System.Math.Abs(rq - q), dr = System.Math.Abs(rr - r), dy = System.Math.Abs(ry - y);
            if (dq > dr && dq > dy) rq = -ry - rr;
            else if (dr > dy) rr = -rq - ry;
            return new Hex((int)rq, (int)rr);
        }

        /// <summary>A rect of the canvas in view units.</summary>
        Rect ViewRect(RectTransform rt)
        {
            var view = (RectTransform)_street.parent;
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            Vector2 a = (Vector2)view.InverseTransformPoint(corners[0]) - view.rect.min, b = (Vector2)view.InverseTransformPoint(corners[2]) - view.rect.min;
            return Rect.MinMaxRect(Mathf.Min(a.x, b.x), Mathf.Min(a.y, b.y), Mathf.Max(a.x, b.x), Mathf.Max(a.y, b.y));
        }

        /// <summary>A tile whose point is inside the drawn lawn, clear of the wood at its sides.</summary>
        static bool InWorld(Hex h)
        {
            Vector2 p = CellToWorld(h);
            return p.x >= 70f && p.x <= WorldW - 70f && p.y >= 80f && p.y <= WorldH - 80f;
        }

        /// <summary>
        /// A tile kept for land not bought yet (its station will stand there, and its dashed ring is already drawn), or kept clear
        /// for a prop off the grid or its plate: the firefly jar, the leaf basket, the mortar's plate (#54 follow-up).
        /// </summary>
        bool Reserved(Hex h)
        {
            if (GardenGrid.IsReserved(h)) return true;
            GameState s = Runner.State;
            for (int i = s.Plots; i < Runner.Defs.Count; i++) if (GardenGrid.DefaultStationTile(i) == h) return true;
            return false;
        }

        /// <summary>The empty tiles the arranging mode offers, nearest the centre first: on the lawn, in view of the garden, not kept for land.</summary>
        public List<Hex> LayoutEmptyTiles()
        {
            var tiles = new List<Hex>();
            GardenGrid g = Grid;
            if (g == null) return tiles;
            foreach (Hex h in GardenGrid.Strip(g.Segments))
                if (g.OccupantAt(h.Q, h.R) == null && InWorld(h) && !Reserved(h)) tiles.Add(h);
            return tiles;
        }

        /// <summary>A dashed ring on every empty tile, and none elsewhere.</summary>
        void RefreshRings()
        {
            if (_layoutRings == null || !LayoutMode) return;
            var empty = new HashSet<Hex>(LayoutEmptyTiles());
            foreach (Hex h in empty)
            {
                if (_ringAt.ContainsKey(h)) continue;
                Vector2 p = CellToWorld(h);
                var ring = UiKit.Rect("Ring " + h.Q + "," + h.R, _layoutRings).Place(0f, 0f, p.x, p.y, 128f, 60f);
                DashedOval(ring, Runner.Catalog, Palette.Mix(Palette.Paper, Palette.Hex("C9A27A"), 0.4f), Palette.Alpha(Palette.Paper, 0.2f), 14, 20f, 6f);
                _ringAt[h] = ring.gameObject;
            }
            foreach (KeyValuePair<Hex, GameObject> pair in _ringAt)
                if (pair.Value.activeSelf != empty.Contains(pair.Key)) pair.Value.SetActive(empty.Contains(pair.Key));
            _layoutTarget.SetAsLastSibling();
        }

        // ----- what can be picked up -----

        /// <summary>Whether something on the grid is drawn now and so can be picked up or swapped with.</summary>
        bool ItemShown(string id)
        {
            GameState s = Runner.State;
            int index = Layout.IndexOf(Runner.Defs, id);
            if (index >= 0) return index < _actors.Count && _actors[index].Root.gameObject.activeSelf;
            if (id.StartsWith("rest:")) return int.TryParse(id.Substring(5), out int k) && k < s.RestSpots && k < _seats.Count;
            if (id.StartsWith("decor:")) return int.TryParse(id.Substring(6), out int n) && n < s.DecorSpots.Length && n < _decorSpots.Count && s.DecorSpots[n] != null;
            return false;
        }

        /// <summary>The ground a thing answers to a finger on, in garden units.</summary>
        Rect GrabRect(string id, Vector2 point)
        {
            int index = Layout.IndexOf(Runner.Defs, id);
            if (index >= 0) return StationBox(index);
            if (id.StartsWith("rest:")) return new Rect(point.x - 75f, point.y - 30f, 150f, 190f);
            return new Rect(point.x - 75f, point.y - 20f, 150f, 180f);
        }

        /// <summary>The movable thing under a garden point, the one whose tile is nearest when several overlap; null for none.</summary>
        string ItemAt(Vector2 world)
        {
            GardenGrid g = Grid;
            if (g == null) return null;
            string best = null;
            float bestDistance = float.MaxValue;
            foreach (KeyValuePair<string, Hex> o in g.Occupants)
            {
                if (!GardenGrid.Movable(o.Key) || !ItemShown(o.Key)) continue;
                Vector2 point = CellToWorld(o.Value);
                if (!GrabRect(o.Key, point).Contains(world)) continue;
                float d = (point - world).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = o.Key; }
            }
            return best;
        }

        /// <summary>The objects that draw a thing: a station's actor, a seat and its furniture, a decoration's spot.</summary>
        RectTransform[] PartsOf(string id)
        {
            int index = Layout.IndexOf(Runner.Defs, id);
            if (index >= 0) return index < _actors.Count ? new[] { _actors[index].Root } : new RectTransform[0];
            if (id.StartsWith("rest:") && int.TryParse(id.Substring(5), out int k) && k < _seats.Count)
                return new[] { (RectTransform)_seatCushions[k].transform, _seats[k] };
            if (id.StartsWith("decor:") && int.TryParse(id.Substring(6), out int n) && n < _decorSpots.Count)
                return new[] { _decorSpots[n].Root };
            return new RectTransform[0];
        }

        // ----- picking up, following, dropping -----

        void LiftUp(string id, Vector2 view)
        {
            Hex? tile = Grid.TileOf(id);
            if (tile == null) return;
            var l = new Lift { Id = id, From = tile.Value, Parts = PartsOf(id), Point = CellToWorld(tile.Value) };
            int n = l.Parts.Length;
            l.Offsets = new Vector2[n];
            l.Scales = new Vector3[n];
            l.Siblings = new int[n];
            for (int k = 0; k < n; k++)
            {
                l.Offsets[k] = l.Parts[k].anchoredPosition - l.Point;
                l.Scales[k] = l.Parts[k].localScale;
                l.Siblings[k] = l.Parts[k].GetSiblingIndex();
            }
            l.Grab = ViewToWorld(view) - l.Point;
            for (int k = 0; k < n; k++) l.Parts[k].SetAsLastSibling(); // in front of everything while carried
            int index = Layout.IndexOf(Runner.Defs, id);
            if (index >= 0)
            {
                l.Actor = _actors[index];
                l.Actor.Front.SetAsLastSibling(); // and its plate over itself, where it always is
                l.BaseScale = l.Actor.BaseScale;
                l.Actor.BaseScale = l.BaseScale * 1.06f; // the actor keeps its own scale every frame from this
            }
            for (int k = 0; k < n; k++) l.Parts[k].localScale = l.Scales[k] * 1.06f;
            // A soft shadow under it: it is off the ground.
            if (n > 0)
            {
                RectTransform host = l.Parts[n - 1];
                RectTransform shadow = UiKit.Rect("LiftShadow", host);
                if (l.Actor != null) shadow.Place(0.5f, 0.5f, 0f, -150f, 320f, 90f);
                else shadow.Place(0.5f, 0f, 0f, 2f, 140f, 46f);
                shadow.AddImage(Runner.Catalog.Glow, Palette.Alpha(Palette.Night, 0.5f)).raycastTarget = false;
                shadow.SetAsFirstSibling();
                l.Shadow = shadow.gameObject;
            }
            _lift = l;
            if (_layoutHint.activeSelf) _layoutHint.SetActive(false);
            Sfx.Click();
            Haptics.Light();
            FollowFinger(view);
        }

        void FollowFinger(Vector2 view)
        {
            Lift l = _lift;
            l.Point = ViewToWorld(view) - l.Grab;
            for (int k = 0; k < l.Parts.Length; k++) if (l.Parts[k] != null) l.Parts[k].anchoredPosition = l.Point + l.Offsets[k];
            Hex to = WorldToCell(l.Point);
            bool target = !OverBar(view) && Droppable(to, l.From);
            if (_layoutTarget.gameObject.activeSelf != target) _layoutTarget.gameObject.SetActive(target);
            if (target) _layoutTarget.anchoredPosition = CellToWorld(to);
            bool store = OverStore(view);
            _layoutStoreFace.color = store ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.45f) : Palette.Mix(Palette.Paper, Palette.Ink, 0.08f);
        }

        bool OverStore(Vector2 view) => ViewRect(_layoutStore).Contains(view);
        bool OverBar(Vector2 view) => ViewRect((RectTransform)_layoutBar.transform).Contains(view);

        /// <summary>An empty tile on offer, or a tile holding another thing that is drawn and can move; never one that stands a station next to another (#88).</summary>
        bool Droppable(Hex to, Hex from)
        {
            if (to == from) return false;
            string carried = Grid.OccupantAt(from.Q, from.R), there = Grid.OccupantAt(to.Q, to.R);
            if (there == null) return Grid.InLawn(to) && InWorld(to) && !Reserved(to) && Grid.KeepsStationsApart(carried, to);
            return GardenGrid.Movable(there) && ItemShown(there) && Grid.KeepsStationsApart(carried, to, there) && Grid.KeepsStationsApart(there, from, carried);
        }

        void Drop(Vector2 view)
        {
            Lift l = _lift;
            _layoutTarget.gameObject.SetActive(false);
            _layoutStoreFace.color = Palette.Mix(Palette.Paper, Palette.Ink, 0.08f);
            if (OverStore(view))
            {
                if (Runner.StoreAway(l.Id))
                {
                    EndLift(l);
                    Vector2 home = HomeOf(l.Id);
                    for (int k = 0; k < l.Parts.Length; k++) if (l.Parts[k] != null) l.Parts[k].anchoredPosition = home + l.Offsets[k];
                    _layoutDirty = true;
                    Sfx.Blip();
                    Haptics.Light();
                    if (Feel != null) Feel.FloatText(Feel.PointOf(_layoutStore, new Vector2(0, 120)), L10n.T("layout.stored"), Palette.Ink, 34);
                    Refresh();
                    RefreshRings();
                    return;
                }
                Sfx.Deny();
                if (Feel != null) Feel.FloatText(Feel.PointOf(_layoutStore, new Vector2(0, 120)), L10n.T("layout.cannot"), Palette.Ink, 30);
                SendBack(l);
                return;
            }
            Hex to = WorldToCell(l.Point);
            if (OverBar(view) || !Droppable(to, l.From)) { SendBack(l); return; }
            string other = Grid.OccupantAt(to.Q, to.R);
            RectTransform[] otherParts = other != null ? PartsOf(other) : new RectTransform[0];
            var otherOffsets = new Vector2[otherParts.Length];
            for (int k = 0; k < otherParts.Length; k++) otherOffsets[k] = otherParts[k].anchoredPosition - CellToWorld(to);
            LayoutDrop done = Runner.DropInLayout(l.From, to);
            if (done == VuonTra.Core.LayoutDrop.Back) { SendBack(l); return; }
            _layoutDirty = true;
            EndLift(l);
            Glide(l.Parts, l.Offsets, l.Point, CellToWorld(to));
            if (done == VuonTra.Core.LayoutDrop.Swapped) Glide(otherParts, otherOffsets, CellToWorld(to), CellToWorld(l.From));
            Sfx.Blip();
            Haptics.Light();
            Refresh();
            RefreshRings();
        }

        /// <summary>Nothing changed: the thing glides back to its own tile.</summary>
        void SendBack(Lift l)
        {
            EndLift(l);
            Glide(l.Parts, l.Offsets, l.Point, CellToWorld(l.From));
        }

        /// <summary>Puts it down: its size, its place in the drawing order, no shadow; nothing is carried any more.</summary>
        void EndLift(Lift l)
        {
            if (_lift == l) _lift = null;
            if (l.Actor != null)
            {
                l.Actor.BaseScale = l.BaseScale;
            }
            for (int k = 0; k < l.Parts.Length; k++) if (l.Parts[k] != null) l.Parts[k].localScale = l.Scales[k];
            if (l.Shadow != null) { l.Shadow.name = "LiftShadow (old)"; Discard(l.Shadow); l.Shadow = null; }
            // Back to where each stood in the drawing order, lowest first so the indices hold.
            var order = new List<int>();
            for (int k = 0; k < l.Parts.Length; k++) order.Add(k);
            order.Sort((a, b) => l.Siblings[a].CompareTo(l.Siblings[b]));
            foreach (int k in order) if (l.Parts[k] != null) l.Parts[k].SetSiblingIndex(l.Siblings[k]);
        }

        /// <summary>Where a thing's point is by the grid now: its tile, or its default place when it has none.</summary>
        Vector2 HomeOf(string id)
        {
            int index = Layout.IndexOf(Runner.Defs, id);
            if (index >= 0) return StationSpot(index);
            if (id.StartsWith("rest:") && int.TryParse(id.Substring(5), out int k)) return RestSpot(k);
            if (id.StartsWith("decor:") && int.TryParse(id.Substring(6), out int n)) return DecorSpotAt(n);
            return Vector2.zero;
        }

        /// <summary>Moves objects from one point to another in a straight line, or at once under reduced motion.</summary>
        void Glide(RectTransform[] parts, Vector2[] offsets, Vector2 from, Vector2 to)
        {
            if (Motion.Reduced || (to - from).sqrMagnitude < 1f)
            {
                for (int k = 0; k < parts.Length; k++) if (parts[k] != null) parts[k].anchoredPosition = to + offsets[k];
                return;
            }
            Tweens.Add(0.2f, t =>
            {
                Vector2 at = Vector2.LerpUnclamped(from, to, t);
                for (int k = 0; k < parts.Length; k++) if (parts[k] != null) parts[k].anchoredPosition = at + offsets[k];
            }, Ease.OutCubic, 0f, () =>
            {
                for (int k = 0; k < parts.Length; k++) if (parts[k] != null) parts[k].anchoredPosition = to + offsets[k]; // exactly on the tile
            });
        }

        #endregion

        #region The pond card and feeding (#58b)

        /// <summary>The moon pond's card: the baits, the float on the water, the basket.</summary>
        public FishingView FishingCard { get; private set; }
        RectTransform _pondTap;
        Text _pondPlate;
        /// <summary>How many times the ten-heart moment has played (a banner and three golden carrots flying to their chip).</summary>
        public int TenHeartMoments { get; private set; }

        /// <summary>
        /// The pond's water as something to touch, with its plate on the near bank. It lies under everything that stands by the
        /// pond, so the Frog Rabbit waiting at the water, the firefly jar and a rabbit resting near it keep their own touch.
        /// </summary>
        void BuildPond(GameCatalog catalog)
        {
            FishingCard = new FishingView(_canvas, catalog, Tweens, Runner);
            Detail.OnFeed = OnFeedRabbit;
            _pondTap = UiKit.Rect("PondTap", _street).Place(0f, 0f, PondSpot.x + 15f, PondSpot.y + 5f, 380f, 210f);
            Image hit = _pondTap.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = _pondTap.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = hit;
            button.onClick.AddListener(OpenFishing);
            // The plate stands on the bank right of the water, towards the teapot, so the opening view reads it (the horizontal
            // garden): clear of the jar's plate below, the mortar's plate and the teapot's pad, and of the order bubble of a guest at
            // the counter, which grows to the right from over the guest's head (#84). Far enough right that it keeps 24 from the
            // screen's left edge when the view stands as far right as the lawn lets it, at the sign of the first segment (#16).
            var plate = UiKit.Rect("Plate", _pondTap).Place(0.5f, 0.5f, PondPlateX, -150f, 180f, 44f);
            plate.AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            _pondPlate = UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText("", 20, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _pondPlate.resizeTextForBestFit = true;
            _pondPlate.resizeTextMinSize = 14;
            _pondPlate.resizeTextMaxSize = 20;
            _pondTap.SetSiblingIndex(_pondGlow != null ? _pondGlow.GetSiblingIndex() : 0);
            RefreshPond();
        }

        /// <summary>How far right of the pond's middle its plate stands; the tiles it reaches are kept clear (GardenGrid's reserved tiles).</summary>
        const float PondPlateX = 240f;

        /// <summary>The pond shows while it can be fished, its plate counting the baits on hand.</summary>
        void RefreshPond()
        {
            if (_pondTap == null) return;
            bool open = Runner.PondOpen;
            if (_pondTap.gameObject.activeSelf != open) _pondTap.gameObject.SetActive(open);
            if (open) _pondPlate.text = L10n.T("pond.plate", Runner.Baits, Fishing.BaitCap);
        }

        /// <summary>The pond was touched: its card slides up. Not while arranging the garden, nor over another game.</summary>
        public void OpenFishing()
        {
            if (Shell.State != ShellState.Playing || PhotoMode || LayoutMode || !Runner.PondOpen) return;
            if (FishingCard.IsOpen || Pound.IsOpen || Basket.IsOpen || Jar.IsOpen) return;
            if (SheetOpen) CloseSheet();
            SelectStation(-1);
            Sfx.Click();
            FishingCard.Open();
            Refresh(-1f);
        }

        /// <summary>
        /// "Cho ăn" on a rabbit's sheet: the best fish in the basket flies from the button to its hearts, its points float up and
        /// the sheet shows the new hearts at once; ten hearts brings the banner and the golden carrots.
        /// </summary>
        void OnFeedRabbit(long rabbitId)
        {
            Rabbit r = Rabbits.Find(Runner.State, rabbitId);
            int fish = Fishing.BestInBasket(Runner.State);
            Vector2 from = Detail.FeedButton != null ? Feel.PointOf(Detail.FeedButton) : Vector2.zero;
            FeedResult fed = Runner.FeedBest(rabbitId);
            if (r == null || !fed.Fed)
            {
                Sfx.Deny();
                return;
            }
            Sfx.Pet();
            Haptics.Light();
            Detail.Refresh(Runner.Defs, Runner.State); // the hearts, their bar and the button, now
            Feel.BringToFront(); // over the rabbit's sheet
            Vector2 to = Detail.BondChip != null ? Feel.PointOf(Detail.BondChip) : from + new Vector2(0f, 200f);
            GameCatalog catalog = Runner.Catalog;
            Sprite art = catalog.FishSprite(fish);
            Feel.Fly(from, to, art != null ? art : catalog.Round, art != null ? Color.white : Palette.Blush, 1, 72f, () =>
            {
                RectTransform chip = Detail != null ? Detail.BondChip : null;
                if (chip == null) return;
                Tweens.Add(Motion.Tap, t => { if (chip != null) chip.localScale = Vector3.one * (1f + 0.15f * Ease.Pop(t)); }, Ease.Linear, 0f,
                    () => { if (chip != null) chip.localScale = Vector3.one; });
            });
            if (fed.Leaves > 0)
            {
                Feel.FloatText(to + new Vector2(0f, 30f), L10n.T("fish.leaf", fed.Leaves), LeafGold, 34);
                Feel.Fly(to, Feel.PointOf(_leafChip), LeafIcon(catalog), LeafGold, fed.Leaves, 40f);
            }
            else if (!fed.Bond.TenHearts) // the banner says it all at ten
            {
                Feel.FloatText(to + new Vector2(0f, 30f), L10n.T("fish.points", fed.Points), Palette.CarrotLip, 34);
                if (fed.HeartsAfter > fed.HeartsBefore) Feel.FloatText(to + new Vector2(130f, 10f), "♥", Palette.Blush, 54);
            }
            if (fed.Bond.GiftGiven)
            {
                Sfx.Sting();
                Feel.Moment(L10n.T("bond.gift.title", r.Name), L10n.T("bond.gift.sub", Rabbits.AffectionGiftClovers));
                Feel.Fly(to, Feel.PointOf(_tabIcons[3].rectTransform), catalog.Clover != null ? catalog.Clover : catalog.Round,
                    catalog.Clover != null ? Color.white : Palette.Moss, Rabbits.AffectionGiftClovers, 38f);
            }
            if (fed.Bond.TenHearts) TenHeartMoment(r, to);
            Refresh(-1f);
        }

        /// <summary>A rabbit's bond reached ten hearts, fed or stroked: the banner, and its three golden carrots fly to their chip.</summary>
        void TenHeartMoment(Rabbit r, Vector2 at)
        {
            TenHeartMoments++;
            GameCatalog catalog = Runner.Catalog;
            Sfx.Milestone();
            Haptics.Medium();
            Feel.BringToFront();
            Feel.Moment(L10n.T("bond.ten.title", r.Name), L10n.T("bond.ten.sub", Rabbits.TenHeartGolden));
            if (!_goldenChip.gameObject.activeSelf) _goldenChip.gameObject.SetActive(true);
            Feel.Fly(at, Feel.PointOf(_goldenChip), catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, Color.white, Rabbits.TenHeartGolden, 44f,
                () => Tweens.Add(Motion.Tap, t => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one * (1f + 0.12f * Ease.Pop(t)); }, Ease.Linear, 0f,
                    () => { if (_goldenChip != null) _goldenChip.localScale = Vector3.one; }));
        }

        #endregion

        #region Reserved prop tiles (#54 follow-up)

        /// <summary>Where the firefly jar stands by the pond, for the tests and previews: its tiles and its plate's are kept clear (<see cref="GardenGrid.ReservedTiles"/>).</summary>
        public static Vector2 FireflyJarSpot => JarSpot;

        /// <summary>
        /// Where the leaf-catching basket stands: past the leaf station's home tile on the right, under its small tea tree. It stays
        /// there when the leaf station is moved (#54), so the tiles kept clear for it and its plate hold wherever things are arranged.
        /// </summary>
        static Vector2 BasketSpot(int leafStation) => CellToWorld(GardenGrid.DefaultStationTile(leafStation)) + new Vector2(104f, 130f);

        #endregion
    }
}

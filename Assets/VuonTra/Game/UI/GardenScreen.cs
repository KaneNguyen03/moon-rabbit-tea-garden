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
        readonly Dictionary<int, Image> _modeButtons = new Dictionary<int, Image>();
        readonly Dictionary<int, Text> _modeLabels = new Dictionary<int, Text>();
        readonly GameObject[] _lists = new GameObject[TabCount];
        readonly Image[] _tabButtons = new Image[TabCount];
        readonly Text[] _tabLabels = new Text[TabCount];
        readonly ChunkyButton[] _openButtons = new ChunkyButton[TabCount];
        readonly Text[] _openLabels = new Text[TabCount];
        readonly List<Image> _tierChips = new List<Image>();
        readonly List<Text> _tierLabels = new List<Text>();
        readonly List<Image> _tierTradeBadges = new List<Image>();
        readonly List<Image> _tierTradeIcons = new List<Image>();
        // Life in the world: rabbits out for a walk, and ingredients carried between stations.
        sealed class Walker { public RectTransform Rect; public float X0, X1, Y, Speed, Phase; }
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
        public RenameView Rename { get; private set; }
        /// <summary>0 = stations, 1 = upgrades ("bùa"), 2 = quests, 3 = rabbits.</summary>
        public int Tab { get; private set; }
        public bool SheetOpen => _sheet != null && _sheet.activeSelf;

        RectTransform _canvas, _street, _hud, _purse, _currency, _panel, _tabBar, _energyFill, _plotRow;
        RawImage _groundTile;
        readonly List<RectTransform> _fireflies = new List<RectTransform>();
        readonly List<Vector2> _glowSpots = new List<Vector2>(); // lanterns and other lights the fireflies gather round
        RawImage _moonbeams;
        CanvasGroup _hintGroup;
        bool _hintFading;
        readonly List<Image> _fireflyImages = new List<Image>();
        CanvasGroup _sheetGroup;
        Text _coins, _perSecond, _awayAmount, _awayTime, _badgeText, _barBadgeText, _clovers, _energyLabel, _energyAction;
        Text _plotTitle, _plotSub, _plotCost;
        ChunkyButton _plotBuy, _energyButton;
        GameObject _hint, _away, _sheet, _modes, _badge, _barBadge;
        RabbitListView _rabbitList;
        float _sinceRefresh, _clock;
        double _shownCoins;
        int _shownStations = -1;
        bool _built;

        void Start()
        {
            if (!_built) Build();
            Runner.StateChanged += RefreshNow;
            Runner.Payout += OnPayout;
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
        const float HudCover = 160f, TabCover = 250f, CardHeight = 280f;

        public enum ZoomLevel { Far, Default, Near }

        /// <summary>Far shows the whole garden top to bottom; near is for looking at one rabbit.</summary>
        public const float ZoomFar = ScreenH / WorldH, ZoomDefault = 1f, ZoomNear = 1.5f;
        float _zoom = 1f;
        Text _zoomMark;
        bool _wheelLatched;
        float _wheelQuiet;
        RectTransform _cardWorker;
        Image _cardLevelChip, _cardWorkerArt, _cardWorkerFrame;
        Text _cardWorkerName, _cardWorkerSub;
        ScrollRect _scroll;
        ZoomLevel _shownZoom = ZoomLevel.Default;
        int _lastTapIndex = -1;
        float _lastTapAt = -10f;
        bool _pinching;
        float _pinchStartDistance, _pinchStartZoom, _wheelWait;
        Vector2 _pinchAt;

        public float ZoomScale => _zoom;
        public ZoomLevel Zoom => _zoom < 0.85f ? ZoomLevel.Far : (_zoom > 1.2f ? ZoomLevel.Near : ZoomLevel.Default);

        static float ScaleOf(ZoomLevel level) => level == ZoomLevel.Far ? ZoomFar : (level == ZoomLevel.Near ? ZoomNear : ZoomDefault);

        /// <summary>Where a point of the garden is on the screen (canvas units from the bottom left).</summary>
        Vector2 ToView(Vector2 world) => world * _zoom + _street.anchoredPosition;

        /// <summary>Keeps the view inside the garden; when the garden is smaller than the screen it sits in the middle.</summary>
        static Vector2 ClampView(Vector2 pos, float zoom)
        {
            float w = WorldW * zoom, h = WorldH * zoom;
            return new Vector2(
                w <= ScreenW ? (ScreenW - w) / 2f : Mathf.Clamp(pos.x, ScreenW - w, 0f),
                h <= ScreenH ? (ScreenH - h) / 2f : Mathf.Clamp(pos.y, ScreenH - h, 0f));
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
            Tweens.Add(0.28f, t => { if (_street != null) PlaceView(world, pivot, Mathf.LerpUnclamped(from, to, t)); }, Ease.OutCubic);
        }

        /// <summary>The zoom button: far, usual, near, and round again.</summary>
        public void CycleZoom()
        {
            SelectStation(-1);
            SetZoom((ZoomLevel)(((int)Zoom + 1) % 3));
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
            Tweens.Add(0.35f, t => { if (_street != null) PlaceView(world, Vector2.LerpUnclamped(fromAt, to, t), Mathf.LerpUnclamped(from, ZoomNear, t)); }, Ease.OutCubic);
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
            _street.anchoredPosition = ClampView(new Vector2(ScreenW / 2f, ScreenH * 0.6f) - point * _zoom, _zoom);
        }

        /// <summary>The grass follows the drag, the walkers walk, and ingredients travel to where they are used.</summary>
        void UpdateWorld(float dt)
        {
            Vector2 offset = -_street.anchoredPosition / _zoom;
            _groundTile.uvRect = new Rect(offset.x / 512f, offset.y / 512f, ScreenW / 512f / _zoom, ScreenH / 512f / _zoom);

            foreach (Walker w in _walkers)
            {
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
                GameState s = Runner.State;
                // One open station at a time, picked from wherever the clock happens to point.
                int start = Mathf.FloorToInt(Hash(_clock + 3f) * _actors.Count);
                for (int n = 0; n < _actors.Count; n++)
                {
                    int i = (start + n) % _actors.Count;
                    if (s.Levels[i] > 0 && !_actors[i].Dozing && _actors[i].Root.gameObject.activeSelf)
                    {
                        _actors[i].SetDozing(true);
                        break;
                    }
                }
            }
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
            _tierChips.Clear(); _tierLabels.Clear(); _tierTradeBadges.Clear(); _tierTradeIcons.Clear();
            _walkers.Clear(); _couriers.Clear(); _supplyRoutes.Clear(); _glowSpots.Clear();
            Selected = -1;
            _shownStations = -1;
            // The old helpers belong to the destroyed canvas; Build makes new ones.
            Feel = null;
            Picker = null;
            Rename = null;
            Shell = null;

            Build();
            if (before != ShellState.Title) Shell.SkipTitle();
            Refresh(-1f);
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
            BuildWorld(catalog);
            BuildAtmosphere(catalog);
            BuildStationCard(catalog);
            BuildEvents(catalog);
            BuildHud(catalog);
            BuildTabBar(catalog);
            BuildSheet(catalog);
            Feel = new FeelLayer(_canvas, catalog, Tweens);
            Picker = new ItemPickerView(_canvas, catalog, Tweens,
                (itemId, rabbitId) => { Sfx.Blip(); Runner.EquipItem(itemId, rabbitId); },
                itemId => { Sfx.Click(); Runner.UnequipItem(itemId); });
            Rename = new RenameView(_canvas, catalog, Tweens, (rabbitId, name) => { Sfx.Blip(); Runner.RenameRabbit(rabbitId, name); });
            BuildAwayPopup(catalog);
            Shell = new ShellView(_canvas, catalog, Tweens, () => Runner.ResetGame());
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
            ground.onClick.AddListener(() => SelectStation(-1)); // tapping empty ground closes the card
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
            BuildDecor(catalog, count, standing);

            for (int i = 0; i < count; i++)
            {
                Vector2 spot = StationSpot(i);
                var actor = new StationActorView(i);
                actor.Build(_street, catalog, catalog.Stations[i], OnTap);
                actor.Root.Place(0f, 0f, spot.x, spot.y, 440, 360);
                actor.Root.localScale = Vector3.one * StationSize(i);
                var plate = (RectTransform)actor.Root.Find("Level");
                plate.localScale = Vector3.one * (0.78f / StationSize(i)); // every station's level reads at the same size
                plate.anchoredPosition += new Vector2(0, -26f); // clear of the rabbit's feet and the prop
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

            // Rabbits out for a walk along the grass between the rows.
            // Each walker keeps to a stretch of grass that no station stands on.
            float[] lanes = { Heart.y + 212f, Heart.y - 312f, 150f };
            float[] laneFrom = { 390f, 400f, 170f }, laneTo = { 700f, 715f, WorldW - 170f };
            for (int i = 0; i < lanes.Length && catalog.Rabbit != null; i++)
            {
                var rt = UiKit.Rect("Walker", _street).Place(0f, 0f, 400, lanes[i], 92, 92);
                rt.pivot = new Vector2(0.5f, 0f);
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 2, 64, 16).AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.35f));
                UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white).preserveAspect = true;
                _walkers.Add(new Walker { Rect = rt, X0 = laneFrom[i], X1 = laneTo[i], Y = lanes[i], Speed = 50f + i * 14f, Phase = i * 900f });
                standing.Add(new KeyValuePair<float, Transform>(float.MaxValue, rt)); // drawn first, so a walker never covers a level plate
            }

            standing.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Transform> item in standing) item.Value.SetAsLastSibling();

            BuildFireflies(catalog);
            CenterOn(StationSpot(0));

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
            entry.callback.AddListener(_ => DismissHint());
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

            // Who works here: the rabbit's face, its name, and its kind and trade.
            _cardWorker = UiKit.Rect("Worker", _card).Place(0f, 0f, 262, 74, 460, 96);
            _cardWorker.AddImage(catalog.Panel, Palette.Track, true);
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
                Vector2 target = ClampView(from + move, _zoom);
                if (_scroll != null) _scroll.StopMovement();
                Tweens.Add(0.25f, t => { if (_street != null) _street.anchoredPosition = Vector2.LerpUnclamped(from, target, t); }, Ease.OutCubic);
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

            // Who works here. Nobody can work at a station that is not open.
            bool open = level > 0 && !needsPlot;
            if (_cardWorker.gameObject.activeSelf != open) _cardWorker.gameObject.SetActive(open);
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
                can = s.Coins >= cost;
                _cardInfo.text = L10n.T("plot.sub", L10n.Station(defs[i].Id));
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
            _cardBuyCost.text = cost.Short();
            Color ink = can ? Palette.Card : Palette.OffInk;
            _cardBuyTop.color = ink;
            _cardBuyCost.color = ink;
            _cardBuy.SetEnabled(can);
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
            var pondAt = Heart + new Vector2(330f, 900f);

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
            Put(TeaTable, Heart.x + 90f, Heart.y - 1240f, 170f, Color.white);
            Put(Stump, Heart.x + 246f, Heart.y - 1230f, 110f, Color.white);
            Put(Stump, Heart.x - 60f, Heart.y - 1260f, 96f, Color.white);

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
            _purse = UiKit.Rect("Purse", _hud).Place(0f, 1f, 204, -76, 360, 100);
            _purse.AddShadow();
            UiKit.Rect("Lip", _purse).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _purse).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            _currency = UiKit.Rect("Currency", _purse).Place(0f, 0.5f, 52, 4, 68, 68);
            _currency.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _coins = UiKit.Rect("Coins", _purse).Stretch(0, 0.42f, 1, 1, 92, 0, 14, 6)
                .AddText("0", 40, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _perSecond = UiKit.Rect("PerSecond", _purse).Stretch(0, 0, 1, 0.46f, 92, 14, 14, 0)
                .AddText(L10n.T("persec", "0"), 22, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);

            // "Trà lực": the bar and the boost button are one control.
            var energy = UiKit.Rect("Energy", _hud).Place(0f, 1f, 616, -76, 440, 100);
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

        /// <summary>The row of round icon buttons along the bottom edge that opens the sheet at a tab.</summary>
        void BuildTabBar(GameCatalog catalog)
        {
            _tabBar = UiKit.Rect("TabBar", _canvas).Place(0.5f, 0f, 0, 96, 760, 180);
            Sprite firstProp = catalog.Stations.Length > 0 ? catalog.Stations[0].Prop : null;
            Sprite[] icons = { firstProp, catalog.IconCharm, catalog.IconQuest, catalog.Rabbit };
            for (int i = 0; i < TabCount; i++)
            {
                int tab = i;
                var rt = UiKit.Rect("Open " + TabNames[i], _tabBar).Place(0.5f, 0.5f, (i - 1.5f) * 178f, 18, 118, 118);
                rt.AddShadow(14f, 10f, 0.32f);
                ChunkyButton button = null;
                button = UiKit.Chunky(rt, catalog.Round, () =>
                {
                    button.Press(Tweens);
                    Sfx.Click();
                    if (SheetOpen && Tab == tab) CloseSheet();
                    else OpenSheet(tab);
                });
                button.Lip.type = Image.Type.Simple;
                button.Face.type = Image.Type.Simple;
                button.Lip.color = Palette.CardLip;
                button.Face.color = Palette.Paper;
                Transform shine = button.FaceRect.Find("Shine");
                if (shine != null) shine.gameObject.SetActive(false); // the sliced gloss does not fit a circle
                _openButtons[i] = button;
                if (icons[i] != null)
                    UiKit.Rect("Icon", button.FaceRect).Stretch(0, 0, 1, 1, 22, 22, 22, 14).AddImage(icons[i], Color.white).preserveAspect = true;

                // The name sits on a small plate under the icon.
                var plate = UiKit.Rect("Plate", rt).Place(0.5f, 0f, 0, -12, 150, 36);
                plate.AddImage(catalog.Panel, Palette.Ink, true);
                _openLabels[i] = UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1)
                    .AddText(L10n.T("tab." + i), 20, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            var badge = UiKit.Rect("Badge", _tabBar).Place(0.5f, 0.5f, 0.5f * 178f + 44f, 62, 44, 44);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _barBadgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _barBadge = badge.gameObject;
        }

        void BuildSheet(GameCatalog catalog)
        {
            var sheet = UiKit.Rect("Sheet", _canvas).Stretch(0, 0, 1, 1);
            _sheet = sheet.gameObject;
            _sheetGroup = _sheet.AddComponent<CanvasGroup>();
            Image scrim = sheet.AddImage(null, Palette.Alpha(Palette.Scrim, 0.45f));
            scrim.raycastTarget = true;
            var scrimButton = _sheet.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(CloseSheet);

            _panel = UiKit.Rect("Panel", sheet).Place(0.5f, 0.5f, 0, 10, 1040, 1480);
            _panel.AddShadow(30f, 18f, 0.45f);
            UiKit.Rect("Lip", _panel).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true).pixelsPerUnitMultiplier = 0.6f;
            Image body = UiKit.Rect("Face", _panel).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            body.pixelsPerUnitMultiplier = 0.6f;
            body.raycastTarget = true; // clicks on the panel must not fall through to the scrim

            var bar = UiKit.Rect("Bar", _panel).Stretch(0, 1, 1, 1);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0, 230);

            UiKit.Rect("Tabs", bar).Place(0f, 0.5f, 318, 46, 580, 80).AddImage(catalog.Panel, Palette.Track, true);
            // Equal widths so every language's labels fit.
            AddTabButton(bar, catalog, 0, 102, 140);
            AddTabButton(bar, catalog, 1, 246, 140);
            AddTabButton(bar, catalog, 2, 390, 140);
            AddTabButton(bar, catalog, 3, 534, 140);

            var badge = UiKit.Rect("Badge", bar).Place(0f, 0.5f, 454, 81, 40, 40);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _badgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _badge = badge.gameObject;

            var modes = UiKit.Rect("Modes", bar).Stretch(0, 0, 1, 1);
            _modes = modes.gameObject;
            UiKit.Rect("Track", modes).Place(1f, 0.5f, -212, -56, 368, 80).AddImage(catalog.Panel, Palette.Track, true);
            AddModeButton(modes, catalog, 1, "×1", -344);
            AddModeButton(modes, catalog, 10, "×10", -246);
            AddModeButton(modes, catalog, 0, "Tối đa", -116);

            var close = UiKit.Rect("Close", bar).Place(1f, 0.5f, -62, 46, 80, 80);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() =>
            {
                Sfx.Click();
                CloseSheet();
            });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            RectTransform stations = AddList("Scroll", 0);
            for (int i = 0; i < catalog.Stations.Length; i++)
            {
                var row = new StationRowView(i);
                row.Build(stations, catalog, catalog.Stations[i], OnBuy);
                _rows.Add(row);
            }
            BuildPlotRow(stations, catalog);

            RectTransform upgrades = AddList("Upgrades", 1);
            AddNote(upgrades, L10n.T("upgrades.note"));
            for (int i = 0; i < catalog.Upgrades.Length; i++)
            {
                var row = new UpgradeRowView(i);
                row.Build(upgrades, catalog, catalog.Upgrades[i], OnBuyUpgrade);
                _upgradeRows.Add(row);
            }

            RectTransform quests = AddList("Quests", 2);
            _clovers = AddNote(quests, "");
            AddNote(quests, L10n.T("quests.today"), true);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) AddNote(quests, L10n.T("quests.achievements"), true);
                for (int i = 0; i < catalog.Quests.Length; i++)
                {
                    if (catalog.Quests[i].Daily != (pass == 0)) continue;
                    var row = new QuestRowView(i);
                    row.Build(quests, catalog, catalog.Quests[i], OnClaim);
                    _questRows.Add(row);
                }
            }

            RectTransform rabbits = AddList("Rabbits", 3);
            _rabbitList = new RabbitListView(rabbits, catalog, OnSummon, () =>
            {
                Sfx.Click();
                Runner.AutoAssignRabbits();
            }, OnMerge, id =>
            {
                Sfx.Click();
                Runner.ToggleRabbitWork(id);
            }, OnLevelUp, (rabbitId, slot) =>
            {
                Sfx.Click();
                Picker.Open(Runner.State, rabbitId, slot);
            }, rabbitId =>
            {
                Sfx.Click();
                Rename.Open(Runner.State, rabbitId);
            });

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
            UiKit.Rect("Top", _plotBuy.FaceRect).Stretch(0, 0.62f, 1, 1, 8, 0, 8, 10)
                .AddText(L10n.T("plot.buy"), 23, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _plotCost = UiKit.Rect("Cost", _plotBuy.FaceRect).Stretch(0, 0, 1, 0.64f, 8, 10, 8, 0)
                .AddText("", 36, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>A scrolling column of cards under the sheet's header; returns where the cards go.</summary>
        RectTransform AddList(string name, int tab)
        {
            var scrollRt = UiKit.Rect(name, _panel).Stretch(0, 0, 1, 1, 0, 24, 0, 230);
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

        void AddTabButton(RectTransform bar, GameCatalog catalog, int tab, float x, float width)
        {
            var rt = UiKit.Rect("Tab " + TabNames[tab], bar).Place(0f, 0.5f, x, 46, width, 64);
            Image img = rt.AddImage(catalog.Panel, Palette.Carrot, true);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = img;
            button.onClick.AddListener(() =>
            {
                Sfx.Click();
                SetTab(tab);
            });
            _tabLabels[tab] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(L10n.T("tab." + tab), 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _tabButtons[tab] = img;
        }

        void AddModeButton(RectTransform bar, GameCatalog catalog, int mode, string label, float x)
        {
            var rt = UiKit.Rect("Mode " + label, bar).Place(1f, 0.5f, x, -56, mode == 0 ? 152 : 92, 64);
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
            _modeLabels[mode] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(mode == 0 ? L10n.T("mode.max") : label, 28, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _modeButtons[mode] = img;
        }

        public void SetTab(int tab)
        {
            Tab = Mathf.Clamp(tab, 0, TabCount - 1);
            for (int i = 0; i < TabCount; i++)
            {
                _lists[i].SetActive(i == Tab);
                _tabButtons[i].color = i == Tab ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _tabLabels[i].color = i == Tab ? Palette.Card : Palette.InkSoft;
            }
            _modes.SetActive(Tab == 0);
            if (_built && Feel != null) Refresh(-1f);
        }

        public void OpenSheet(int tab)
        {
            SetTab(tab);
            if (SheetOpen) return;
            _sheet.SetActive(true);
            _sheetGroup.alpha = 0f;
            _sheetGroup.blocksRaycasts = true;
            _panel.localScale = Vector3.one * 0.9f;
            Tweens.Add(0.2f, t => _sheetGroup.alpha = t, Ease.OutCubic);
            Tweens.Add(0.28f, t => _panel.localScale = Vector3.one * Mathf.LerpUnclamped(0.9f, 1f, t), Ease.OutBack);
            Refresh(-1f);
        }

        public void CloseSheet()
        {
            if (!SheetOpen) return;
            _sheetGroup.blocksRaycasts = false;
            _sheet.SetActive(false);
            Refresh(-1f);
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
                Sfx.Blip();
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
            Tweens.Add(0.45f, t => _hud.anchoredPosition = hudHome + new Vector2(0, (1f - t) * 340f), Ease.OutBack);
            Tweens.Add(0.5f, t => _tabBar.anchoredPosition = barHome + new Vector2(0, -(1f - t) * 320f), Ease.OutCubic);
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
            RefreshPlotRow(defs, s);
            _rabbitList.Refresh(defs, s);
            Picker.Refresh(s);

            int claimable = Quests.ClaimableCount(Runner.QuestDefs, defs, s);
            _badge.SetActive(claimable > 0);
            _badgeText.text = claimable.ToString();
            _barBadge.SetActive(claimable > 0);
            _barBadgeText.text = claimable.ToString();
            _clovers.text = L10n.T("quests.clovers", s.Clovers);

            foreach (KeyValuePair<int, Image> pair in _modeButtons)
            {
                bool on = pair.Key == s.BuyMode;
                pair.Value.color = on ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _modeLabels[pair.Key].color = on ? Palette.Card : Palette.InkSoft;
            }
            for (int i = 0; i < TabCount; i++)
            {
                bool on = SheetOpen && Tab == i;
                _openButtons[i].Lip.color = on ? Palette.CarrotLip : Palette.CardLip;
                _openButtons[i].Face.color = on ? Palette.Lantern : Palette.Paper;
                _openLabels[i].color = on ? Palette.Lantern : Palette.Paper;
            }
        }

        void RefreshEnergy(GameState s, bool boosting)
        {
            bool can = Energy.CanBoost(s);
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
                _actors[i].SetLevel(s.Levels[i], defs[i].Kind == StationKind.Brew, i >= s.Plots);
                _actors[i].SetCanUpgrade(CanAfford(defs, s, i));

                Rabbit worker = Rabbits.AtStation(s, i);
                GameCatalog catalog = Runner.Catalog;
                if (worker == null) _actors[i].SetWorker(null, null, null, false);
                else
                {
                    // The collected rabbit itself stands at the station, wearing what it wears.
                    Item hat = Items.Worn(s, worker.Id, ItemSlot.Hat), charm = Items.Worn(s, worker.Id, ItemSlot.Charm);
                    _actors[i].SetWorker(catalog.RabbitArt(worker),
                        hat != null ? catalog.ItemIcon(hat.DefId) : null, charm != null ? catalog.ItemIcon(charm.DefId) : null,
                        worker.Tier >= 4);
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
        }

        /// <summary>True when the one thing the station's card offers (land, opening, or levels) can be paid for.</summary>
        static bool CanAfford(IReadOnlyList<StationDef> defs, GameState s, int i)
        {
            if (i >= s.Plots) return s.Coins >= Plots.CostOfNext(defs, s);
            int level = s.Levels[i];
            if (level <= 0) return Economy.CanUnlock(s, i) && s.Coins >= Economy.CostOfNext(defs[i], 0, 1);
            return s.Coins >= Economy.CostOfNext(defs[i], level, Simulation.BuyCount(defs, s, i));
        }

        void RefreshPlotRow(IReadOnlyList<StationDef> defs, GameState s)
        {
            bool more = Plots.CanBuy(defs, s);
            if (_plotRow.gameObject.activeSelf != more) _plotRow.gameObject.SetActive(more);
            if (!more) return;
            if (_plotRow.GetSiblingIndex() != s.Plots) _plotRow.SetSiblingIndex(s.Plots);
            BigNum cost = Plots.CostOfNext(defs, s);
            bool can = s.Coins >= cost;
            _plotTitle.text = L10n.T("plot.title", s.Plots + 1);
            _plotSub.text = L10n.T("plot.sub", L10n.Station(defs[s.Plots].Id));
            _plotCost.text = cost.Short();
            _plotCost.color = can ? Palette.Card : Palette.OffInk;
            _plotBuy.SetEnabled(can);
        }

        public void ShowAway(BigNum amount, double seconds)
        {
            _awayAmount.text = "+" + amount.Short();
            int minutes = Mathf.Max(1, (int)(seconds / 60d));
            string span = minutes >= 60 ? L10n.T("away.hours", minutes / 60, minutes % 60) : L10n.T("away.minutes", minutes);
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
            SelectStation(index); // a tap always shows the station's card, asleep or not
            if (index == _lastTapIndex && _clock - _lastTapAt < 0.35f && Zoom != ZoomLevel.Near) ZoomToStation(index);
            _lastTapIndex = index;
            _lastTapAt = _clock;
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
            Sfx.Blip();
            DismissHint();
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
            Sfx.Blip();
            RectTransform actor = _actors[index].Root;
            Vector3 home = actor.localScale;
            Tweens.Add(0.2f, t =>
            {
                if (actor != null) actor.localScale = home * (1f + 0.14f * Ease.Pop(t));
            }, Ease.Linear, done: () =>
            {
                if (actor != null) actor.localScale = home;
            });

            GameCatalog catalog = Runner.Catalog;
            int after = s.Levels[index];
            if (before == 0)
            {
                Sfx.Sting();
                Feel.Moment(L10n.T("moment.unlock"), L10n.Station(catalog.Stations[index].Id));
            }
            else if (Economy.MilestoneMultiplier(after) > multiplierBefore)
            {
                Sfx.Sting();
                Feel.Moment(L10n.T("moment.milestone.title", Economy.PreviousMilestone(after)), L10n.T("moment.milestone.sub", L10n.Station(catalog.Stations[index].Id)));
            }
            else if (OnScreen(actor))
            {
                Feel.Burst(Feel.PointOf(actor, new Vector2(0, 40)), catalog.Round,
                    new[] { Palette.Carrot, Palette.Lantern, Palette.Blush }, 8, 380f, 18f);
            }
        }

        void OnBuyPlot()
        {
            if (!Runner.BuyPlot()) return;
            _plotBuy.Press(Tweens);
            Sfx.Sting();
            Feel.Moment(L10n.T("moment.plot.title"), L10n.T("moment.plot.sub", L10n.Station(Runner.Defs[Runner.State.Plots - 1].Id)));
        }

        void OnSummon()
        {
            Rabbit rabbit = Runner.SummonRabbit();
            if (rabbit == null) return;
            _rabbitList.Summon.Press(Tweens);
            Sfx.Sting();
            Feel.Moment(L10n.T("moment.summon.title", rabbit.Name), L10n.T("moment.summon.sub", L10n.Tier(rabbit.Tier), UiKit.Times(Rabbits.Multipliers[rabbit.Tier])));
        }

        void OnMerge(int tier)
        {
            Rabbit merged = Runner.MergeRabbits(tier);
            if (merged == null) return;
            Sfx.Sting();
            Feel.Moment(L10n.T("moment.merge.title", merged.Name, L10n.Tier(merged.Tier)), L10n.T("moment.merge.sub", UiKit.Times(Rabbits.Multipliers[merged.Tier])));
        }

        void OnLevelUp(long rabbitId)
        {
            if (!Runner.LevelUpRabbit(rabbitId)) return;
            Sfx.Blip();
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
            if (!Runner.ClaimQuest(index, out BigNum paid)) return;
            if (row != null) row.Claim.Press(Tweens);
            Sfx.Sting();
            Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), "+" + paid.Short(), Palette.Lantern, 46);
            Feel.Burst(Feel.PointOf(_purse), Runner.Catalog.Currency, new[] { Color.white }, 12, 520f, 46f);
            Item gift = Runner.LastQuestItem;
            if (gift != null)
                Feel.Moment(L10n.T("moment.gift.title", L10n.ItemName(gift)), L10n.T("moment.gift.sub", L10n.Tier(gift.Tier), ItemPickerView.Describe(gift)));
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

        void OnPayout(int index, BigNum amount)
        {
            if (Shell.State == ShellState.Title) return;
            RectTransform actor = _actors[index].Root;
            _actors[index].Hop();
            if (!actor.gameObject.activeSelf || !OnScreen(actor) || SheetOpen) return;

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

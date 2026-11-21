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
        // around in every direction. Sizes are in reference pixels (1920 × 1080).
        const float WorldW = 3260f, WorldH = 1960f;
        const int PerRow = 4;

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
        /// <summary>Index of the station whose card is open, or -1.</summary>
        public int Selected { get; private set; } = -1;

        public Tweens Tweens { get; } = new Tweens();
        public FeelLayer Feel { get; private set; }
        public ShellView Shell { get; private set; }
        public ItemPickerView Picker { get; private set; }
        /// <summary>0 = stations, 1 = upgrades ("bùa"), 2 = quests, 3 = rabbits.</summary>
        public int Tab { get; private set; }
        public bool SheetOpen => _sheet != null && _sheet.activeSelf;

        RectTransform _canvas, _street, _hud, _purse, _currency, _panel, _tabBar, _energyFill, _plotRow;
        RawImage _groundTile;
        readonly List<RectTransform> _fireflies = new List<RectTransform>();
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

        /// <summary>Position of station <paramref name="i"/> in the world: rows of four, snaking back and forth.</summary>
        public static Vector2 StationSpot(int i)
        {
            int row = i / PerRow, col = i % PerRow;
            bool reversed = row % 2 == 1;
            if (reversed) col = PerRow - 1 - col;
            return new Vector2(540f + col * 640f + (reversed ? 260f : 0f), WorldH - 520f - row * 500f);
        }

        /// <summary>Moves the view so a world point sits near the middle of the screen.</summary>
        void CenterOn(Vector2 point)
        {
            _street.anchoredPosition = new Vector2(
                Mathf.Clamp(960f - point.x, 1920f - WorldW, 0f),
                Mathf.Clamp(600f - point.y, 1080f - WorldH, 0f));
        }

        /// <summary>The grass follows the drag, the walkers walk, and ingredients travel to where they are used.</summary>
        void UpdateWorld(float dt)
        {
            Vector2 offset = -_street.anchoredPosition;
            _groundTile.uvRect = new Rect(offset.x / 512f, offset.y / 512f, 1920f / 512f, 1080f / 512f);

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
                Vector2 from = StationSpot(route.x) + new Vector2(112, -70), to = StationSpot(route.y) + new Vector2(112, -70);
                var rt = UiKit.Rect("Courier", _street).Place(0f, 0f, from.x, from.y, 62, 62);
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, -2, 54, 16).AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.35f));
                UiKit.Rect("Item", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Stations[route.x].Prop, Color.white).preserveAspect = true;
                _couriers.Add(new Courier { Rect = rt, From = from, To = to, Target = route.y, Duration = Mathf.Max(1.2f, Vector2.Distance(from, to) / 380f) });
                return;
            }
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
            _tierChips.Clear(); _tierLabels.Clear();
            _walkers.Clear(); _couriers.Clear(); _supplyRoutes.Clear();
            Selected = -1;
            _shownStations = -1;
            // The old helpers belong to the destroyed canvas; Build makes new ones.
            Feel = null;
            Picker = null;
            Shell = null;

            Build();
            if (before != ShellState.Title) Shell.SkipTitle();
            Refresh(-1f);
            _street.anchoredPosition = streetAt;
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
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            _canvas = (RectTransform)canvasGo.transform;

            BuildBackdrop(catalog);
            BuildWorld(catalog);
            BuildAtmosphere(catalog);
            BuildHud(catalog);
            BuildTabBar(catalog);
            BuildSheet(catalog);
            Feel = new FeelLayer(_canvas, catalog, Tweens);
            Picker = new ItemPickerView(_canvas, catalog, Tweens,
                (itemId, rabbitId) => { Sfx.Blip(); Runner.EquipItem(itemId, rabbitId); },
                itemId => { Sfx.Click(); Runner.UnequipItem(itemId); });
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
            scroll.horizontal = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 60f;

            _street = UiKit.Rect("Content", view);
            _street.anchorMin = _street.anchorMax = _street.pivot = Vector2.zero;
            _street.sizeDelta = new Vector2(WorldW, WorldH);
            scroll.content = _street;
            scroll.viewport = view;

            int count = catalog.Stations.Length;

            // Stepping stones from each station to the next lie flat on the grass, under everything.
            Color stone = Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Grass, 0.5f), 0.6f);
            for (int i = 0; i + 1 < count; i++)
            {
                Vector2 a = StationSpot(i) + new Vector2(0, -150), b = StationSpot(i + 1) + new Vector2(0, -150);
                int steps = Mathf.Max(2, Mathf.RoundToInt(Vector2.Distance(a, b) / 74f));
                for (int k = 1; k < steps; k++)
                {
                    Vector2 at = Vector2.Lerp(a, b, k / (float)steps) + new Vector2(0, Mathf.Sin(k * 1.7f) * 10f);
                    UiKit.Rect("Stone", _street).Place(0f, 0f, at.x, at.y, 50, 20).AddImage(catalog.Round, stone);
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
                _actors.Add(actor);
                standing.Add(new KeyValuePair<float, Transform>(spot.y - 112f, actor.Root));

                var chip = UiKit.Rect("Tier", actor.Root).Place(0.5f, 0.5f, -72, 176, 200, 44);
                _tierChips.Add(chip.AddImage(catalog.Panel, Palette.Paper, true));
                _tierLabels.Add(UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold));
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
            float[] lanes = { WorldH - 770f, WorldH - 1270f, 200f };
            for (int i = 0; i < lanes.Length && catalog.Rabbit != null; i++)
            {
                var rt = UiKit.Rect("Walker", _street).Place(0f, 0f, 400, lanes[i], 130, 130);
                rt.pivot = new Vector2(0.5f, 0f);
                UiKit.Rect("Shadow", rt).Place(0.5f, 0f, 0, 2, 90, 22).AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.35f));
                UiKit.Rect("Art", rt).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white).preserveAspect = true;
                _walkers.Add(new Walker { Rect = rt, X0 = 320f, X1 = WorldW - 320f, Y = lanes[i], Speed = 70f + i * 22f, Phase = i * 900f });
                standing.Add(new KeyValuePair<float, Transform>(lanes[i], rt));
            }

            standing.Sort((a, b) => b.Key.CompareTo(a.Key));
            foreach (KeyValuePair<float, Transform> item in standing) item.Value.SetAsLastSibling();

            BuildStationCard(catalog);
            CenterOn(StationSpot(0));

            var hint = UiKit.Rect("Hint", _canvas).Place(0.5f, 1f, 0, -196, 900, 60);
            hint.AddShadow(14f, 8f, 0.28f);
            UiKit.Rect("Body", hint).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1).AddText(L10n.T("hint"), 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint = hint.gameObject;
        }

        /// <summary>The small card over a tapped station: what it is, what it earns, and the one button to grow it.</summary>
        void BuildStationCard(GameCatalog catalog)
        {
            _card = UiKit.Rect("Card", _street).Place(0f, 0f, 0, 0, 520, 250);
            _card.pivot = new Vector2(0.5f, 0f);
            _card.AddShadow(18f, 10f, 0.4f);
            var tail = UiKit.Rect("Tail", _card).Place(0.5f, 0f, 0, 8, 40, 40);
            tail.localRotation = Quaternion.Euler(0, 0, 45);
            tail.AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            Image face = UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 10, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            face.raycastTarget = true; // a tap on the card is not a tap on the ground behind it

            _cardName = UiKit.Rect("Name", _card).Stretch(0, 1, 1, 1, 28, -66, 170, 14)
                .AddText("", 34, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _cardName.horizontalOverflow = HorizontalWrapMode.Overflow;
            var chip = UiKit.Rect("Level", _card).Place(1f, 1f, -92, -40, 140, 44);
            chip.AddImage(catalog.Panel, Palette.Moss, true);
            _cardLevel = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _cardInfo = UiKit.Rect("Info", _card).Stretch(0, 1, 1, 1, 28, -128, 28, 70)
                .AddText("", 24, Palette.InkSoft, TextAnchor.UpperLeft);

            var buy = UiKit.Rect("Buy", _card).Place(0.5f, 0f, 0, 72, 464, 92);
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
            _card.SetAsLastSibling();
            _card.anchoredPosition = StationSpot(index) + new Vector2(16, 196);
            if (changed)
            {
                // Slide the world just enough that the station and its card are clear of the HUD and the tab bar.
                Vector2 spot = StationSpot(index), from = _street.anchoredPosition;
                Vector2 onScreen = spot + from;
                var target = new Vector2(
                    from.x + Mathf.Clamp(onScreen.x, 340f, 1580f) - onScreen.x,
                    from.y + Mathf.Clamp(onScreen.y, 330f, 440f) - onScreen.y);
                target.x = Mathf.Clamp(target.x, 1920f - WorldW, 0f);
                target.y = Mathf.Clamp(target.y, 1080f - WorldH, 0f);
                var scroll = _street.parent.GetComponent<ScrollRect>();
                if (scroll != null) scroll.StopMovement();
                Tweens.Add(0.25f, t => { if (_street != null) _street.anchoredPosition = Vector2.LerpUnclamped(from, target, t); }, Ease.OutCubic);

                _card.localScale = Vector3.one * 0.8f;
                Tweens.Add(0.22f, t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, t); }, Ease.OutBack);
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

        /// <summary>Fireflies drifting over the street and darkened corners, above the stations and below the HUD.</summary>
        void BuildAtmosphere(GameCatalog catalog)
        {
            _fireflies.Clear();
            _fireflyImages.Clear();
            var layer = UiKit.Rect("Fireflies", _canvas).Stretch(0, 0, 1, 1);
            for (int i = 0; i < 16; i++)
            {
                var rt = UiKit.Rect("Firefly", layer).Place(0f, 0f, 0, 0, 34, 34);
                _fireflyImages.Add(rt.AddImage(catalog.Glow, Palette.Lantern));
                UiKit.Rect("Core", rt).Place(0.5f, 0.5f, 0, 0, 7, 7).AddImage(catalog.Round, Palette.Mix(Palette.Lantern, Color.white, 0.6f));
                _fireflies.Add(rt);
            }
            if (catalog.BgVignette != null)
            {
                var v = UiKit.Rect("Vignette", _canvas).Stretch(0, 0, 1, 1);
                var raw = v.gameObject.AddComponent<RawImage>();
                raw.texture = catalog.BgVignette;
                raw.raycastTarget = false;
            }
        }

        /// <summary>Each firefly wanders on its own slow loop and pulses; they drift a little with the street.</summary>
        void UpdateFireflies()
        {
            float drift = -_street.anchoredPosition.x * 0.6f, lift = -_street.anchoredPosition.y * 0.6f;
            for (int i = 0; i < _fireflies.Count; i++)
            {
                float seed = i * 7.31f;
                float x = Mathf.Repeat(Hash(seed) * 2200f + Mathf.Sin(_clock * 0.23f + seed) * 140f - drift, 2200f) - 140f;
                float y = Mathf.Repeat(Hash(seed + 3f) * 1200f + Mathf.Sin(_clock * 0.31f + seed * 1.7f) * 46f - lift, 1200f) - 60f;
                _fireflies[i].anchoredPosition = new Vector2(x, y);
                float pulse = 0.5f + 0.5f * Mathf.Sin(_clock * (1.3f + Hash(seed + 9f)) + seed);
                _fireflyImages[i].color = Palette.Alpha(Palette.Lantern, 0.15f + 0.6f * pulse);
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
        /// Dresses the clearing: a ring of trees around its edge, and trees, bushes, lanterns,
        /// mushrooms and stones scattered between the stations. Placement is fixed (not random each
        /// run) so the garden always looks the same.
        /// </summary>
        void BuildDecor(GameCatalog catalog, int stations, List<KeyValuePair<float, Transform>> standing)
        {
            Sprite[] d = catalog.Decor;
            if (d == null || d.Length < 6) return;
            const int Tree = 0, Bush = 1, Lantern = 2, Mushrooms = 4, Stones = 5;
            Color soft = Palette.Mix(Color.white, Palette.Night, 0.12f);

            void Put(int kind, float x, float y, float height, Color tint)
            {
                if (kind == Lantern)
                {
                    var glow = UiKit.Rect("LanternGlow", _street).Place(0f, 0f, x + 14f, y + 150f, 380f, 380f);
                    glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.6f));
                    standing.Add(new KeyValuePair<float, Transform>(y + 0.5f, glow));
                }
                Image img = AddDecor(_street, d[kind], x, y, height, tint);
                if (img != null) standing.Add(new KeyValuePair<float, Transform>(y, img.transform));
            }

            // The wood around the clearing.
            for (float x = 60f; x < WorldW; x += 230f)
            {
                Put(Tree, x + Hash(x) * 80f, WorldH - 200f + Hash(x + 5) * 90f, 300f + Hash(x + 9) * 110f, soft);
                Put(Hash(x + 2) < 0.5f ? Tree : Bush, x + Hash(x + 3) * 90f, 20f + Hash(x + 7) * 50f, Hash(x + 2) < 0.5f ? 300f : 120f, soft);
            }
            for (float y = 220f; y < WorldH - 260f; y += 250f)
            {
                Put(Tree, 40f + Hash(y) * 70f, y, 300f + Hash(y + 4) * 90f, soft);
                Put(Tree, WorldW - 110f + Hash(y + 2) * 70f, y + 60f, 300f + Hash(y + 6) * 90f, soft);
            }

            // Inside: wherever there is room that is not a station.
            for (int n = 0; n < 70; n++)
            {
                float x = 260f + Hash(n * 3.1f + 1f) * (WorldW - 520f), y = 200f + Hash(n * 5.7f + 2f) * (WorldH - 560f);
                float pick = Hash(n * 9.3f);
                bool tall = pick < 0.22f || pick >= 0.82f; // trees and lanterns reach up over whatever is behind them
                bool free = true;
                for (int i = 0; i < stations && free; i++)
                {
                    Vector2 spot = StationSpot(i);
                    if (Mathf.Abs(spot.x - x) < 360f && y > spot.y - (tall ? 620f : 240f) && y < spot.y + 250f) free = false;
                }
                if (!free) continue;
                if (pick < 0.22f) Put(Tree, x, y, 270f + Hash(n + 40f) * 90f, soft);
                else if (pick < 0.47f) Put(Bush, x, y, 104f, soft);
                else if (pick < 0.67f) Put(Mushrooms, x, y, 84f, soft);
                else if (pick < 0.82f) Put(Stones, x, y, 62f, soft);
                else Put(Lantern, x, y, 226f, Color.white);
            }
        }

        void BuildHud(GameCatalog catalog)
        {
            _hud = UiKit.Rect("Hud", _canvas).Stretch(0, 1, 1, 1);
            _hud.pivot = new Vector2(0.5f, 1f);
            _hud.sizeDelta = new Vector2(0, 150);

            _purse = UiKit.Rect("Purse", _hud).Place(0f, 1f, 260, -78, 440, 104);
            _purse.AddShadow();
            UiKit.Rect("Lip", _purse).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _purse).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            _currency = UiKit.Rect("Currency", _purse).Place(0f, 0.5f, 60, 4, 76, 76);
            _currency.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _coins = UiKit.Rect("Coins", _purse).Stretch(0, 0.42f, 1, 1, 108, 0, 20, 6)
                .AddText("0", 44, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _perSecond = UiKit.Rect("PerSecond", _purse).Stretch(0, 0, 1, 0.46f, 108, 14, 20, 0)
                .AddText(L10n.T("persec", "0"), 24, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);

            // "Trà lực": the bar and the boost button are one control.
            var energy = UiKit.Rect("Energy", _hud).Place(0.5f, 1f, 0, -78, 520, 104);
            energy.AddShadow();
            _energyButton = UiKit.Chunky(energy, catalog.Panel, OnBoost);
            _energyLabel = UiKit.Rect("Label", _energyButton.FaceRect).Stretch(0, 0.46f, 0.6f, 1, 28, 0, 0, 6)
                .AddText("", 28, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _energyAction = UiKit.Rect("Action", _energyButton.FaceRect).Stretch(0.5f, 0.46f, 1, 1, 0, 0, 28, 6)
                .AddText("", 26, Palette.Ink, TextAnchor.MiddleRight, FontStyle.Bold);
            var track = UiKit.Rect("Track", _energyButton.FaceRect).Band(28, 28, 16, 20);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _energyFill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _energyFill.AddImage(catalog.Panel, Palette.Moss, true);

            var gear = UiKit.Rect("Settings", _hud).Place(1f, 1f, -92, -78, 104, 104);
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
            UiKit.Rect("Icon", gearButton.FaceRect).Stretch(0, 0, 1, 1, 22, 22, 22, 22).AddImage(catalog.Gear, Palette.Ink).preserveAspect = true;
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

            _panel = UiKit.Rect("Panel", sheet).Place(0.5f, 0.5f, 0, 20, 1120, 900);
            _panel.AddShadow(30f, 18f, 0.45f);
            UiKit.Rect("Lip", _panel).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true).pixelsPerUnitMultiplier = 0.6f;
            Image body = UiKit.Rect("Face", _panel).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            body.pixelsPerUnitMultiplier = 0.6f;
            body.raycastTarget = true; // clicks on the panel must not fall through to the scrim

            var bar = UiKit.Rect("Bar", _panel).Stretch(0, 1, 1, 1);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0, 128);

            UiKit.Rect("Tabs", bar).Place(0f, 0.5f, 318, -9, 580, 80).AddImage(catalog.Panel, Palette.Track, true);
            // Equal widths so every language's labels fit.
            AddTabButton(bar, catalog, 0, 102, 140);
            AddTabButton(bar, catalog, 1, 246, 140);
            AddTabButton(bar, catalog, 2, 390, 140);
            AddTabButton(bar, catalog, 3, 534, 140);

            var badge = UiKit.Rect("Badge", bar).Place(0f, 0.5f, 454, 26, 40, 40);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _badgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _badge = badge.gameObject;

            var modes = UiKit.Rect("Modes", bar).Stretch(0, 0, 1, 1);
            _modes = modes.gameObject;
            UiKit.Rect("Track", modes).Place(1f, 0.5f, -306, -9, 368, 80).AddImage(catalog.Panel, Palette.Track, true);
            AddModeButton(modes, catalog, 1, "×1", -438);
            AddModeButton(modes, catalog, 10, "×10", -340);
            AddModeButton(modes, catalog, 0, "Tối đa", -210);

            var close = UiKit.Rect("Close", bar).Place(1f, 0.5f, -62, -9, 80, 80);
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
            var scrollRt = UiKit.Rect(name, _panel).Stretch(0, 0, 1, 1, 0, 24, 0, 128);
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
            var rt = UiKit.Rect("Tab " + TabNames[tab], bar).Place(0f, 0.5f, x, -9, width, 64);
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
            var rt = UiKit.Rect("Mode " + label, bar).Place(1f, 0.5f, x, -9, mode == 0 ? 152 : 92, 64);
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
            Tweens.Add(0.45f, t => _hud.anchoredPosition = hudHome + new Vector2(0, (1f - t) * 240f), Ease.OutBack);
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
            _hint.SetActive(!s.HintSeen && Shell.State == ShellState.Playing && !SheetOpen);
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
            _energyButton.Lip.color = boosting ? Palette.Mix(Palette.Moss, Palette.Ink, 0.3f) : (can ? Palette.CarrotLip : Palette.CardLip);
            _energyButton.Face.color = boosting ? Palette.Mix(Palette.Moss, Palette.Paper, 0.45f) : (can ? Palette.Lantern : Palette.Paper);
        }

        /// <summary>Which stations stand in the garden, who works at each, and the open station card.</summary>
        void RefreshStreet(IReadOnlyList<StationDef> defs, GameState s)
        {
            // Stations with a plot, plus the one waiting for the next plot.
            int shown = Mathf.Min(defs.Count, s.Plots + 1);
            if (shown != _shownStations)
            {
                _shownStations = shown;
                for (int i = 0; i < _actors.Count; i++) _actors[i].Root.gameObject.SetActive(i < shown);
                if (Selected >= shown) SelectStation(-1);
            }

            for (int i = 0; i < shown; i++)
            {
                _actors[i].SetLevel(s.Levels[i], defs[i].Kind == StationKind.Brew, i >= s.Plots);

                Rabbit worker = Rabbits.AtStation(s, i);
                GameObject chip = _tierChips[i].gameObject;
                if (chip.activeSelf != (worker != null)) chip.SetActive(worker != null);
                if (worker == null) continue;
                _tierChips[i].color = Palette.Tiers[worker.Tier];
                _tierLabels[i].text = worker.Name;
            }
            RefreshCard(defs, s, Runner.Mods);
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
            return at.x > -1100f && at.x < 1100f && at.y > -640f && at.y < 640f;
        }

        void OnTap(int index)
        {
            SelectStation(index); // a tap always shows the station's card, asleep or not
            if (Runner.State.Levels[index] <= 0) return;
            Runner.Tap(index);
            _actors[index].Squish();
            Sfx.Blip();
            if (_hint.activeSelf) _hint.SetActive(false);
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

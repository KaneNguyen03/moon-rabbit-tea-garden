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
        // Street layout, in reference pixels (1920 × 1080).
        const float FirstStationX = 340f, StationSpacing = 390f, StreetEndPadding = 520f;
        const float BackLaneY = 430f, FrontLaneY = 290f, BackLaneScale = 0.80f, FrontLaneScale = 1f;

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
        readonly List<ChunkyButton> _quickButtons = new List<ChunkyButton>();
        readonly List<Text> _quickLabels = new List<Text>();

        public Tweens Tweens { get; } = new Tweens();
        public FeelLayer Feel { get; private set; }
        public ShellView Shell { get; private set; }
        public ItemPickerView Picker { get; private set; }
        /// <summary>0 = stations, 1 = upgrades ("bùa"), 2 = quests, 3 = rabbits.</summary>
        public int Tab { get; private set; }
        public bool SheetOpen => _sheet != null && _sheet.activeSelf;

        RectTransform _canvas, _street, _hud, _purse, _currency, _panel, _tabBar, _energyFill, _plotRow;
        RawImage _hills, _trees, _ground;
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
            UpdateParallax();

            // The counter counts up instead of snapping.
            double target = s.Coins.ToDouble();
            _shownCoins = System.Math.Abs(target - _shownCoins) < 0.5 || target > 1e15
                ? target
                : _shownCoins + (target - _shownCoins) * Mathf.Clamp01(dt * 10f);
            _coins.text = target > 1e15 ? s.Coins.Short() : new BigNum(_shownCoins).Short();

            _sinceRefresh += dt;
            if (_sinceRefresh >= RefreshInterval) Refresh(_sinceRefresh);
        }

        /// <summary>Far layers slide slower than the street, which is what gives the scene depth.</summary>
        void UpdateParallax()
        {
            float x = -_street.anchoredPosition.x;
            _hills.uvRect = new Rect(x * 0.10f / 1920f, 0, 1, 1);
            _trees.uvRect = new Rect(x * 0.28f / 1920f, 0, 1.5f, 1);
            _ground.uvRect = new Rect(x / 960f, 0, 2, 1);
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
            float streetX = _street.anchoredPosition.x;

            Tweens.Clear(); // running tweens point at objects that are about to be destroyed
            GameObject old = _canvas.gameObject;
            old.name = "Canvas (old)"; // so lookups by path find the new one straight away
            old.SetActive(false);
            if (Application.isPlaying) Destroy(old);
            else DestroyImmediate(old);

            _actors.Clear(); _rows.Clear(); _upgradeRows.Clear(); _questRows.Clear();
            _modeButtons.Clear(); _modeLabels.Clear();
            _tierChips.Clear(); _tierLabels.Clear(); _quickButtons.Clear(); _quickLabels.Clear();
            _shownStations = -1;
            // The old helpers belong to the destroyed canvas; Build makes new ones.
            Feel = null;
            Picker = null;
            Shell = null;

            Build();
            if (before != ShellState.Title) Shell.SkipTitle();
            Refresh(-1f);
            _street.anchoredPosition = new Vector2(streetX, _street.anchoredPosition.y);
            if (sheetWasOpen) OpenSheet(tab);
            if (before == ShellState.Paused) Shell.OpenPause(); // back to the card the language buttons are on
        }

        /// <summary>Creates every UI object. Safe to call from the editor for a preview.</summary>
        public void Build()
        {
            _built = true;
            GameCatalog catalog = Runner.Catalog;

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
            BuildStreet(catalog);
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

        RawImage AddLayer(string name, Texture texture, float y0, float y1)
        {
            var rt = UiKit.Rect(name, _canvas).Stretch(0, y0, 1, y1);
            var img = rt.gameObject.AddComponent<RawImage>();
            img.texture = texture;
            img.color = texture != null ? Color.white : Palette.Night;
            img.raycastTarget = false;
            return img;
        }

        void BuildBackdrop(GameCatalog catalog)
        {
            AddLayer("Sky", catalog.BgSky, 0, 1);
            _hills = AddLayer("Hills", catalog.BgHills, 0.50f, 0.86f);
            _trees = AddLayer("Trees", catalog.BgTrees, 0.50f, 0.74f);
            _ground = AddLayer("Ground", catalog.BgGround, 0, 0.565f);
            if (catalog.BgGround == null) _ground.color = Palette.Grass;
        }

        void BuildStreet(GameCatalog catalog)
        {
            var view = UiKit.Rect("Garden", _canvas).Stretch(0, 0, 1, 1);
            view.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true; // gives the scroll something to drag
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.vertical = false;
            scroll.horizontal = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 60f;

            _street = UiKit.Rect("Content", view);
            _street.anchorMin = new Vector2(0, 0);
            _street.anchorMax = new Vector2(0, 1);
            _street.pivot = new Vector2(0, 0.5f);
            _street.offsetMin = _street.offsetMax = Vector2.zero;
            scroll.content = _street;
            scroll.viewport = view;

            int count = catalog.Stations.Length;
            for (int i = 0; i < count; i++)
            {
                bool front = i % 2 == 1;
                var actor = new StationActorView(i);
                actor.Build(_street, catalog, catalog.Stations[i], OnTap);
                actor.Root.Place(0f, 0f, FirstStationX + i * StationSpacing, front ? FrontLaneY : BackLaneY, 440, 360);
                float scale = front ? FrontLaneScale : BackLaneScale;
                actor.Root.localScale = new Vector3(scale, scale, 1f);
                _actors.Add(actor);

                var chip = UiKit.Rect("Tier", actor.Root).Place(0.5f, 0.5f, -72, 176, 200, 44);
                _tierChips.Add(chip.AddImage(catalog.Panel, Palette.Paper, true));
                _tierLabels.Add(UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold));
                chip.gameObject.SetActive(false);

                // Buy right at the station, without opening the list.
                int index = i;
                var quick = UiKit.Rect("Quick", actor.Root).Place(0.5f, 0.5f, 16, -206, 270, 70);
                ChunkyButton button = UiKit.Chunky(quick, catalog.Panel, () => OnQuick(index));
                _quickButtons.Add(button);
                _quickLabels.Add(UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 6, 0, 6, 0)
                    .AddText("", 26, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold));
            }
            // Front-lane stations draw over back-lane ones.
            for (int i = 1; i < count; i += 2) _actors[i].Root.SetAsLastSibling();

            var hint = UiKit.Rect("Hint", _canvas).Place(0.5f, 1f, 0, -196, 900, 60);
            hint.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1).AddText(L10n.T("hint"), 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint = hint.gameObject;
        }

        void BuildHud(GameCatalog catalog)
        {
            _hud = UiKit.Rect("Hud", _canvas).Stretch(0, 1, 1, 1);
            _hud.pivot = new Vector2(0.5f, 1f);
            _hud.sizeDelta = new Vector2(0, 150);

            _purse = UiKit.Rect("Purse", _hud).Place(0f, 1f, 260, -78, 440, 104);
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

        /// <summary>The column of buttons on the right edge that opens the sheet at a tab.</summary>
        void BuildTabBar(GameCatalog catalog)
        {
            _tabBar = UiKit.Rect("TabBar", _canvas).Place(1f, 0.5f, -106, -60, 180, 480);
            for (int i = 0; i < TabCount; i++)
            {
                int tab = i;
                var rt = UiKit.Rect("Open " + TabNames[i], _tabBar).Place(0.5f, 1f, 0, -58 - i * 120, 176, 104);
                ChunkyButton button = null;
                button = UiKit.Chunky(rt, catalog.Panel, () =>
                {
                    button.Press(Tweens);
                    Sfx.Click();
                    if (SheetOpen && Tab == tab) CloseSheet();
                    else OpenSheet(tab);
                });
                button.Lip.color = Palette.CardLip;
                button.Face.color = Palette.Paper;
                _openButtons[i] = button;
                _openLabels[i] = UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1)
                    .AddText(L10n.T("tab." + i), 28, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            }
            var badge = UiKit.Rect("Badge", _tabBar).Place(0.5f, 1f, 78, -58 - 2 * 120 + 40, 46, 46);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _barBadgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("", 25, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
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

            _panel = UiKit.Rect("Panel", sheet).Place(0.5f, 0.5f, -110, -40, 1120, 880);
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
            Tweens.Add(0.5f, t => _tabBar.anchoredPosition = barHome + new Vector2((1f - t) * 320f, 0), Ease.OutCubic);
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
                _openButtons[i].Face.color = on ? Palette.Carrot : Palette.Paper;
                _openLabels[i].color = on ? Palette.Card : Palette.Ink;
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

        /// <summary>Which stations stand on the street, how long it is, and what each quick button offers.</summary>
        void RefreshStreet(IReadOnlyList<StationDef> defs, GameState s)
        {
            // Stations with a plot, plus the one waiting for the next plot.
            int shown = Mathf.Min(defs.Count, s.Plots + 1);
            if (shown != _shownStations)
            {
                _shownStations = shown;
                for (int i = 0; i < _actors.Count; i++) _actors[i].Root.gameObject.SetActive(i < shown);
                _street.sizeDelta = new Vector2(FirstStationX + (shown - 1) * StationSpacing + StreetEndPadding, 0);
            }

            for (int i = 0; i < shown; i++)
            {
                int level = s.Levels[i];
                bool needsPlot = i >= s.Plots;
                _actors[i].SetLevel(level, defs[i].Kind == StationKind.Brew, needsPlot);

                Rabbit worker = Rabbits.AtStation(s, i);
                GameObject chip = _tierChips[i].gameObject;
                if (chip.activeSelf != (worker != null)) chip.SetActive(worker != null);
                if (worker != null)
                {
                    _tierChips[i].color = Palette.Tiers[worker.Tier];
                    _tierLabels[i].text = worker.Name;
                }

                string label;
                bool can;
                if (needsPlot)
                {
                    BigNum cost = Plots.CostOfNext(defs, s);
                    label = L10n.T("quick.plot", cost.Short());
                    can = s.Coins >= cost;
                }
                else if (level <= 0)
                {
                    BigNum cost = Economy.CostOfNext(defs[i], 0, 1);
                    label = L10n.T("quick.open", cost.Short());
                    can = Economy.CanUnlock(s, i) && s.Coins >= cost;
                }
                else
                {
                    int count = Simulation.BuyCount(defs, s, i);
                    BigNum cost = Economy.CostOfNext(defs[i], level, count);
                    label = L10n.T("quick.upgrade", count, cost.Short());
                    can = s.Coins >= cost;
                }
                _quickLabels[i].text = label;
                _quickLabels[i].color = can ? Palette.Card : Palette.OffInk;
                _quickButtons[i].SetEnabled(can);
            }
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
            float x = Feel.PointOf(actor).x;
            return x > -1100f && x < 1100f;
        }

        void OnTap(int index)
        {
            if (Runner.State.Levels[index] <= 0) return;
            Runner.Tap(index);
            _actors[index].Squish();
            Sfx.Blip();
            if (_hint.activeSelf) _hint.SetActive(false);
        }

        void OnQuick(int index)
        {
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
            _quickButtons[index].Press(Tweens);
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

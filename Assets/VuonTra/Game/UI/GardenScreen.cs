using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The whole portrait screen, built in code: sky and garden on top, a paper drawer of upgrade
    /// cards below, with the title and pause screens around it. Views only read the state and call
    /// GameRunner; no rules live here.
    /// </summary>
    public sealed class GardenScreen : MonoBehaviour
    {
        public GameRunner Runner;
        public Camera Cam;

        const float RefreshInterval = 1f / 10f;
        const float DrawerHiddenY = -900f;

        readonly List<StationActorView> _actors = new List<StationActorView>();
        readonly List<StationRowView> _rows = new List<StationRowView>();
        readonly List<UpgradeRowView> _upgradeRows = new List<UpgradeRowView>();
        readonly List<QuestRowView> _questRows = new List<QuestRowView>();
        readonly Dictionary<int, Image> _modeButtons = new Dictionary<int, Image>();
        readonly Dictionary<int, Text> _modeLabels = new Dictionary<int, Text>();
        const int TabCount = 4;
        readonly GameObject[] _lists = new GameObject[TabCount];
        readonly Image[] _tabButtons = new Image[TabCount];
        readonly Text[] _tabLabels = new Text[TabCount];
        readonly List<Image> _tierChips = new List<Image>();
        readonly List<Text> _tierLabels = new List<Text>();
        RabbitListView _rabbitList;
        GameObject _modes, _badge;
        Text _badgeText, _clovers;

        /// <summary>0 = stations, 1 = upgrades ("bùa"), 2 = quests, 3 = rabbits.</summary>
        public int Tab { get; private set; }

        public Tweens Tweens { get; } = new Tweens();
        public FeelLayer Feel { get; private set; }
        public ShellView Shell { get; private set; }

        RectTransform _canvas, _garden, _drawer, _hud, _purse, _currency;
        Text _coins, _perSecond, _awayAmount, _awayTime;
        GameObject _hint, _away;
        float _sinceRefresh, _clock;
        double _shownCoins;
        bool _built;

        void Start()
        {
            if (!_built) Build();
            Runner.StateChanged += RefreshNow;
            Runner.Payout += OnPayout;
            Runner.AwayClaimed += ShowAway;
            RefreshNow();
        }

        void OnDestroy()
        {
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
            for (int i = 0; i < _actors.Count; i++) _actors[i].Animate(s.Progress[i], _clock, dt);

            Tweens.Update(dt);
            Feel.Update(dt);
            Shell.Update(_clock);

            // The counter counts up instead of snapping.
            double target = s.Coins.ToDouble();
            _shownCoins = System.Math.Abs(target - _shownCoins) < 0.5 || target > 1e15
                ? target
                : _shownCoins + (target - _shownCoins) * Mathf.Clamp01(dt * 10f);
            _coins.text = target > 1e15 ? s.Coins.ToShortString() : new BigNum(_shownCoins).ToShortString();

            _sinceRefresh += dt;
            if (_sinceRefresh >= RefreshInterval) Refresh(_sinceRefresh);
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
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            _canvas = (RectTransform)canvasGo.transform;

            UiKit.Rect("Background", _canvas).Stretch(0, 0, 1, 1).AddImage(catalog.Background, Color.white);

            BuildGarden(catalog);
            BuildHud(catalog);
            BuildDrawer(catalog);
            Feel = new FeelLayer(_canvas, catalog, Tweens);
            BuildAwayPopup(catalog);
            Shell = new ShellView(_canvas, catalog, Tweens, () => Runner.ResetGame());
            Shell.Changed += OnShellChanged;
            Sfx.Attach(gameObject);

            // Title state: the garden runs behind it, the HUD and drawer wait off screen.
            _hud.gameObject.SetActive(false);
            _drawer.anchoredPosition = new Vector2(0, DrawerHiddenY);
            _shownCoins = Runner.State.Coins.ToDouble();
        }

        void BuildGarden(GameCatalog catalog)
        {
            _garden = UiKit.Rect("Garden", _canvas).Stretch(0, 0.42f, 1, 0.93f);
            int count = catalog.Stations.Length;
            int rowsNeeded = Mathf.Max(1, (count + 1) / 2);
            for (int i = 0; i < count; i++)
            {
                int col = i % 2, row = i / 2;
                float x = 0.27f + 0.46f * col + (row % 2 == 0 ? -0.025f : 0.025f);
                float y = rowsNeeded == 1 ? 0.5f : 0.87f - 0.69f * row / (rowsNeeded - 1);
                var actor = new StationActorView(i);
                actor.Build(_garden, catalog, catalog.Stations[i], OnTap);
                actor.Root.Place(x, y, 0, 0, 440, 360);
                float depth = 0.66f + 0.10f * row / Mathf.Max(1, rowsNeeded - 1);
                actor.Root.localScale = new Vector3(depth, depth, 1f);
                _actors.Add(actor);

                var chip = UiKit.Rect("Tier", actor.Root).Place(0.5f, 0.5f, -72, 172, 200, 44);
                Image chipImage = chip.AddImage(catalog.Panel, Palette.Paper, true);
                _tierChips.Add(chipImage);
                _tierLabels.Add(UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold));
                chip.gameObject.SetActive(false);
            }

            var hint = UiKit.Rect("Hint", _garden).Place(0.468f, 0.87f, 0, 30, 226, 96);
            hint.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1).AddText("Chạm vào bé thỏ\nđể làm nhanh hơn", 21, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _hint = hint.gameObject;
        }

        void BuildHud(GameCatalog catalog)
        {
            _hud = UiKit.Rect("Hud", _canvas).Stretch(0, 0.93f, 1, 1);

            _purse = UiKit.Rect("Purse", _hud).Place(0f, 0.5f, 250, -4, 440, 104);
            UiKit.Rect("Lip", _purse).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _purse).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            _currency = UiKit.Rect("Currency", _purse).Place(0f, 0.5f, 60, 4, 76, 76);
            _currency.AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _coins = UiKit.Rect("Coins", _purse).Stretch(0, 0.42f, 1, 1, 108, 0, 20, 6)
                .AddText("0", 44, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _perSecond = UiKit.Rect("PerSecond", _purse).Stretch(0, 0, 1, 0.46f, 108, 14, 20, 0)
                .AddText("+0/giây", 24, Palette.InkSoft, TextAnchor.MiddleLeft, FontStyle.Bold);

            var gear = UiKit.Rect("Settings", _hud).Place(1f, 0.5f, -92, -4, 104, 104);
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

        void BuildDrawer(GameCatalog catalog)
        {
            // Extends below the screen so only its rounded top shows.
            _drawer = UiKit.Rect("Panel", _canvas).Stretch(0, 0, 1, 0.42f, 0, -80, 0, 0);
            Image body = _drawer.AddImage(catalog.Panel, Palette.Paper, true);
            body.raycastTarget = true;
            body.pixelsPerUnitMultiplier = 0.5f; // rounder top corners than the cards

            var bar = UiKit.Rect("Bar", _drawer).Stretch(0, 1, 1, 1);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0, 128);
            UiKit.Rect("Handle", bar).Place(0.5f, 1f, 0, -18, 96, 10).AddImage(catalog.Panel, Palette.Track, true);

            UiKit.Rect("Tabs", bar).Place(0f, 0.5f, 318, -9, 580, 80).AddImage(catalog.Panel, Palette.Track, true);
            AddTabButton(bar, catalog, 0, "Trạm", 96, 120);
            AddTabButton(bar, catalog, 1, "Bùa", 214, 108);
            AddTabButton(bar, catalog, 2, "Nhiệm vụ", 364, 180);
            AddTabButton(bar, catalog, 3, "Thỏ", 536, 132);

            var badge = UiKit.Rect("Badge", bar).Place(0f, 0.5f, 448, 24, 40, 40);
            badge.AddImage(catalog.Round, Palette.CarrotLip);
            _badgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1).AddText("", 24, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _badge = badge.gameObject;

            var modes = UiKit.Rect("Modes", bar).Stretch(0, 0, 1, 1);
            _modes = modes.gameObject;
            UiKit.Rect("Track", modes).Place(1f, 0.5f, -212, -9, 368, 80).AddImage(catalog.Panel, Palette.Track, true);
            AddModeButton(modes, catalog, 1, "×1", -344);
            AddModeButton(modes, catalog, 10, "×10", -246);
            AddModeButton(modes, catalog, 0, "Tối đa", -116);

            RectTransform stations = AddList("Scroll", 0);
            for (int i = 0; i < catalog.Stations.Length; i++)
            {
                var row = new StationRowView(i);
                row.Build(stations, catalog, catalog.Stations[i], OnBuy);
                _rows.Add(row);
            }

            RectTransform upgrades = AddList("Upgrades", 1);
            AddNote(upgrades, "Bùa có tác dụng với cả khu vườn.");
            for (int i = 0; i < catalog.Upgrades.Length; i++)
            {
                var row = new UpgradeRowView(i);
                row.Build(upgrades, catalog, catalog.Upgrades[i], OnBuyUpgrade);
                _upgradeRows.Add(row);
            }

            RectTransform quests = AddList("Quests", 2);
            _clovers = AddNote(quests, "");
            AddNote(quests, "Hôm nay", true);
            for (int pass = 0; pass < 2; pass++)
            {
                if (pass == 1) AddNote(quests, "Thành tựu", true);
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
            });

            SetTab(0);
        }

        /// <summary>A scrolling column of cards under the drawer's header; returns where the cards go.</summary>
        RectTransform AddList(string name, int tab)
        {
            var scrollRt = UiKit.Rect(name, _drawer).Stretch(0, 0, 1, 1, 0, 80, 0, 128);
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

        void AddTabButton(RectTransform bar, GameCatalog catalog, int tab, string label, float x, float width)
        {
            var rt = UiKit.Rect("Tab " + label, bar).Place(0f, 0.5f, x, -9, width, 64);
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
            _tabLabels[tab] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(label, 26, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _tabButtons[tab] = img;
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
            _modeLabels[mode] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(label, 28, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _modeButtons[mode] = img;
        }

        void BuildAwayPopup(GameCatalog catalog)
        {
            var overlay = UiKit.Rect("Away", _canvas).Stretch(0, 0, 1, 1);
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            _away = overlay.gameObject;

            var card = UiKit.Rect("Card", overlay).Place(0.5f, 0.52f, 0, 0, 800, 700);
            UiKit.Rect("Lip", card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Title", card).Stretch(0, 0.80f, 1, 1, 40, 0, 40, 40)
                .AddText("Mừng bạn quay lại", 54, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Body", card).Stretch(0, 0.62f, 1, 0.80f, 60, 0, 60, 0)
                .AddText("Lúc bạn đi vắng, các bé thỏ vẫn chăm chỉ pha trà và gom được", 30, Palette.InkSoft);
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
            UiKit.Rect("Text", okButton.FaceRect).Stretch(0, 0, 1, 1).AddText("Nhận cà rốt", 40, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            _away.SetActive(false);
        }

        void OnShellChanged(ShellState state)
        {
            Runner.Paused = state == ShellState.Paused;
            if (state != ShellState.Playing || _hud.gameObject.activeSelf) return;

            // Leaving the title: the HUD drops in and the drawer slides up.
            _hud.gameObject.SetActive(true);
            Vector2 hudHome = _hud.anchoredPosition;
            Tweens.Add(0.45f, t => _hud.anchoredPosition = hudHome + new Vector2(0, (1f - t) * 220f), Ease.OutBack);
            Tweens.Add(0.5f, t => _drawer.anchoredPosition = new Vector2(0, DrawerHiddenY * (1f - t)), Ease.OutCubic);
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
            BigNum perSecond = Economy.IncomePerSecond(defs, s) * (m.IncomeMultiplier * m.SpeedMultiplier);
            _perSecond.text = "+" + (perSecond < new BigNum(10) ? UiKit.Seconds(perSecond.ToDouble()) : perSecond.ToShortString()) + "/giây";
            _hint.SetActive(!s.HintSeen && Shell.State != ShellState.Title);
            if (dt < 0f) _shownCoins = s.Coins.ToDouble();

            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].Refresh(defs, s, m, dt);
                _actors[i].SetLevel(s.Levels[i], defs[i].Kind == StationKind.Brew);
            }
            for (int i = 0; i < _upgradeRows.Count; i++) _upgradeRows[i].Refresh(Runner.UpgradeDefs, s);
            for (int i = 0; i < _questRows.Count; i++) _questRows[i].Refresh(Runner.QuestDefs, defs, s, m, dt);

            _rabbitList.Refresh(defs, s);
            for (int i = 0; i < _tierChips.Count; i++)
            {
                Rabbit worker = Rabbits.AtStation(s, i);
                GameObject chip = _tierChips[i].gameObject;
                if (chip.activeSelf != (worker != null)) chip.SetActive(worker != null);
                if (worker == null) continue;
                _tierChips[i].color = Palette.Tiers[worker.Tier];
                _tierLabels[i].text = worker.Name;
            }

            int claimable = Quests.ClaimableCount(Runner.QuestDefs, defs, s);
            _badge.SetActive(claimable > 0);
            _badgeText.text = claimable.ToString();
            _clovers.text = "Cỏ bốn lá: " + s.Clovers + " · dùng để gọi thỏ ở tab Thỏ";

            foreach (KeyValuePair<int, Image> pair in _modeButtons)
            {
                bool on = pair.Key == s.BuyMode;
                pair.Value.color = on ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f);
                _modeLabels[pair.Key].color = on ? Palette.Card : Palette.InkSoft;
            }
        }

        public void ShowAway(BigNum amount, double seconds)
        {
            _awayAmount.text = "+" + amount.ToShortString();
            int minutes = Mathf.Max(1, (int)(seconds / 60d));
            string span = minutes >= 60 ? (minutes / 60) + " giờ " + (minutes % 60) + " phút" : minutes + " phút";
            bool capped = seconds >= Simulation.AwayCapSeconds;
            _awayTime.text = "trong " + span + (capped ? " (đã chạm giới hạn vắng mặt)" : "") + ".";
            _away.SetActive(true);
            RectTransform card = (RectTransform)_away.transform.Find("Card");
            card.localScale = Vector3.one * 0.8f;
            Tweens.Add(0.3f, t => card.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, t), Ease.OutBack);
        }

        void OnTap(int index)
        {
            if (Runner.State.Levels[index] <= 0) return;
            Runner.Tap(index);
            _actors[index].Squish();
            Sfx.Blip();
            if (_hint.activeSelf) _hint.SetActive(false);
        }

        void OnBuy(int index)
        {
            GameState s = Runner.State;
            int before = s.Levels[index];
            double multiplierBefore = Economy.MilestoneMultiplier(before);
            if (!Runner.Buy(index)) return;

            StationRowView row = _rows[index];
            row.Buy.Press(Tweens);
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
                Feel.Moment("Trạm mới thức dậy!", catalog.Stations[index].DisplayName);
            }
            else if (Economy.MilestoneMultiplier(after) > multiplierBefore)
            {
                Sfx.Sting();
                Feel.Moment("Cấp " + Economy.PreviousMilestone(after) + "!", catalog.Stations[index].DisplayName + " giờ thu gấp đôi");
            }
            else
            {
                Feel.Burst(Feel.PointOf(actor, new Vector2(0, 40)), catalog.Round,
                    new[] { Palette.Carrot, Palette.Lantern, Palette.Blush }, 8, 380f, 18f);
            }
        }

        void OnSummon()
        {
            Rabbit rabbit = Runner.SummonRabbit();
            if (rabbit == null) return;
            _rabbitList.Summon.Press(Tweens);
            Sfx.Sting();
            Feel.Moment("Bé " + rabbit.Name + " đã tới!", "Bậc " + Palette.TierNames[rabbit.Tier] + " · ×" + UiKit.Times(Rabbits.Multipliers[rabbit.Tier]) + " cho trạm bé làm");
        }

        void OnMerge(int tier)
        {
            Rabbit merged = Runner.MergeRabbits(tier);
            if (merged == null) return;
            Sfx.Sting();
            Feel.Moment(merged.Name + " lên bậc " + Palette.TierNames[merged.Tier] + "!", "×" + UiKit.Times(Rabbits.Multipliers[merged.Tier]) + " cho trạm bé làm");
        }

        void OnBuyUpgrade(int index)
        {
            if (!Runner.BuyUpgrade(index)) return;
            _upgradeRows[index].Buy.Press(Tweens);
            Sfx.Sting();
            UpgradeData data = Runner.Catalog.Upgrades[index];
            Feel.Moment(data.DisplayName + " cấp " + Runner.State.UpgradeLevels[index], data.Description);
        }

        void OnClaim(int index)
        {
            QuestRowView row = RowOfQuest(index);
            if (!Runner.ClaimQuest(index, out BigNum paid)) return;
            if (row != null) row.Claim.Press(Tweens);
            Sfx.Sting();
            Feel.FloatText(Feel.PointOf(_purse, new Vector2(0, -110)), "+" + paid.ToShortString(), Palette.Lantern, 46);
            Feel.Burst(Feel.PointOf(_purse), Runner.Catalog.Currency, new[] { Color.white }, 12, 520f, 46f);
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
            _actors[index].PopProp();
            Feel.FloatText(Feel.PointOf(actor, new Vector2(60, 150)), "+" + amount.ToShortString(), Palette.Paper);
            Feel.Burst(Feel.PointOf(actor, new Vector2(110, 60)), Runner.Catalog.Currency, new[] { Color.white }, 2, 300f, 34f);
            Sfx.Tick();

            // The purse answers too.
            Vector3 home = Vector3.one;
            Tweens.Add(0.15f, t =>
            {
                if (_currency != null) _currency.localScale = home * (1f + 0.25f * Ease.Pop(t));
            }, Ease.Linear);
        }
    }
}

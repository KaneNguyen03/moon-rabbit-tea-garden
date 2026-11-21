using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The whole portrait screen, built in code: sky and garden on top, upgrade list below.
    /// Views only read the state and call GameRunner; no rules live here.
    /// </summary>
    public sealed class GardenScreen : MonoBehaviour
    {
        public GameRunner Runner;
        public Camera Cam;

        const float RefreshInterval = 1f / 6f;
        const float FloatSeconds = 1.1f;

        readonly List<StationActorView> _actors = new List<StationActorView>();
        readonly List<StationRowView> _rows = new List<StationRowView>();
        readonly List<Floater> _floaters = new List<Floater>();
        readonly Dictionary<int, Image> _modeButtons = new Dictionary<int, Image>();
        readonly Dictionary<int, Text> _modeLabels = new Dictionary<int, Text>();

        RectTransform _canvas, _garden;
        Text _coins, _perSecond, _awayAmount, _awayTime;
        GameObject _hint, _away;
        float _nextRefresh;
        bool _built;

        struct Floater
        {
            public RectTransform Rect;
            public Text Text;
            public float Born;
            public Vector2 Start;
        }

        void Start()
        {
            if (!_built) Build();
            Runner.StateChanged += Refresh;
            Runner.Payout += OnPayout;
            Runner.AwayClaimed += ShowAway;
            Refresh();
        }

        void OnDestroy()
        {
            if (Runner == null) return;
            Runner.StateChanged -= Refresh;
            Runner.Payout -= OnPayout;
            Runner.AwayClaimed -= ShowAway;
        }

        void Update()
        {
            GameState s = Runner.State;
            float now = Time.unscaledTime;
            for (int i = 0; i < _actors.Count; i++)
                _actors[i].Animate(s.Progress[i], now, Time.unscaledDeltaTime);

            for (int i = _floaters.Count - 1; i >= 0; i--)
            {
                Floater f = _floaters[i];
                float life = (now - f.Born) / FloatSeconds;
                if (life >= 1f)
                {
                    Destroy(f.Rect.gameObject);
                    _floaters.RemoveAt(i);
                    continue;
                }
                f.Rect.anchoredPosition = f.Start + new Vector2(0, life * 90f);
                Color c = f.Text.color;
                c.a = 1f - life * life;
                f.Text.color = c;
            }

            if (now >= _nextRefresh)
            {
                _nextRefresh = now + RefreshInterval;
                Refresh();
            }
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
            BuildPanel(catalog);
            BuildAwayPopup(catalog);
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
            }

            var hint = UiKit.Rect("Hint", _garden).Place(0.5f, 0.87f, 0, -20, 236, 96);
            hint.AddImage(catalog.Panel, new Color(0.06f, 0.08f, 0.19f, 0.8f), true);
            UiKit.Rect("Text", hint).Stretch(0, 0, 1, 1).AddText("Chạm vào bé thỏ\nđể làm nhanh hơn", 23, UiKit.Cream);
            _hint = hint.gameObject;
        }

        void BuildHud(GameCatalog catalog)
        {
            var hud = UiKit.Rect("Hud", _canvas).Stretch(0, 0.93f, 1, 1);
            UiKit.Rect("Title", hud).Stretch(0, 0, 0.55f, 1, 40, 0, 0, 0)
                .AddText("Vườn Trà Thỏ Trăng", 40, UiKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);

            var purse = UiKit.Rect("Purse", hud).Place(1f, 0.5f, -230, 0, 400, 92);
            purse.AddImage(catalog.Panel, new Color(UiKit.Panel.r, UiKit.Panel.g, UiKit.Panel.b, 0.92f), true);
            UiKit.Rect("Currency", purse).Place(0f, 0.5f, 50, 0, 64, 64).AddImage(catalog.Currency != null ? catalog.Currency : catalog.Round, Color.white).preserveAspect = true;
            _coins = UiKit.Rect("Coins", purse).Stretch(0, 0.4f, 1, 1, 90, 0, 20, 4)
                .AddText("0", 40, UiKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            _perSecond = UiKit.Rect("PerSecond", purse).Stretch(0, 0, 1, 0.45f, 90, 6, 20, 0)
                .AddText("+0/giây", 24, UiKit.Muted, TextAnchor.MiddleLeft);
        }

        void BuildPanel(GameCatalog catalog)
        {
            var panel = UiKit.Rect("Panel", _canvas).Stretch(0, 0, 1, 0.42f);
            panel.AddImage(null, UiKit.Panel).raycastTarget = true;

            var bar = UiKit.Rect("Bar", panel).Stretch(0, 1, 1, 1);
            bar.pivot = new Vector2(0.5f, 1f);
            bar.sizeDelta = new Vector2(0, 110);
            UiKit.Rect("Heading", bar).Stretch(0, 0, 0.5f, 1, 40, 0, 0, 0)
                .AddText("Trạm trà", 34, UiKit.Cream, TextAnchor.MiddleLeft, FontStyle.Bold);
            AddModeButton(bar, catalog, 1, "×1", -420);
            AddModeButton(bar, catalog, 10, "×10", -280);
            AddModeButton(bar, catalog, 0, "Tối đa", -120);

            var scrollRt = UiKit.Rect("Scroll", panel).Stretch(0, 0, 1, 1, 0, 0, 0, 110);
            scrollRt.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();

            var content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            var list = content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(28, 28, 4, 40);
            list.spacing = 16;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content;
            scroll.viewport = scrollRt;

            for (int i = 0; i < catalog.Stations.Length; i++)
            {
                var row = new StationRowView(i);
                row.Build(content, catalog, catalog.Stations[i], OnBuy);
                _rows.Add(row);
            }
        }

        void AddModeButton(RectTransform bar, GameCatalog catalog, int mode, string label, float x)
        {
            var rt = UiKit.Rect("Mode " + label, bar).Place(1f, 0.5f, x, 0, mode == 0 ? 170 : 120, 72);
            Image img = rt.AddImage(catalog.Panel, UiKit.Night, true);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.targetGraphic = img;
            button.onClick.AddListener(() => Runner.SetBuyMode(mode));
            _modeLabels[mode] = UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1).AddText(label, 28, UiKit.Muted, TextAnchor.MiddleCenter, FontStyle.Bold);
            _modeButtons[mode] = img;
        }

        void BuildAwayPopup(GameCatalog catalog)
        {
            var overlay = UiKit.Rect("Away", _canvas).Stretch(0, 0, 1, 1);
            overlay.AddImage(null, new Color(0.04f, 0.05f, 0.13f, 0.8f)).raycastTarget = true;
            _away = overlay.gameObject;

            var card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 760, 620);
            card.AddImage(catalog.Panel, UiKit.Panel2, true);
            UiKit.Rect("Title", card).Stretch(0, 0.78f, 1, 1, 40, 0, 40, 30)
                .AddText("Mừng bạn quay lại", 50, UiKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Body", card).Stretch(0, 0.58f, 1, 0.78f, 50, 0, 50, 0)
                .AddText("Lúc bạn đi vắng, các bé thỏ vẫn chăm chỉ pha trà và gom được", 30, UiKit.Muted);
            _awayAmount = UiKit.Rect("Amount", card).Stretch(0, 0.38f, 1, 0.58f)
                .AddText("+0", 76, UiKit.Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            _awayTime = UiKit.Rect("Time", card).Stretch(0, 0.27f, 1, 0.38f)
                .AddText("", 28, UiKit.Muted);

            var ok = UiKit.Rect("Ok", card).Place(0.5f, 0f, 0, 100, 560, 110);
            Image okImage = ok.AddImage(catalog.Panel, UiKit.Lantern, true);
            okImage.raycastTarget = true;
            var button = ok.gameObject.AddComponent<Button>();
            button.targetGraphic = okImage;
            button.onClick.AddListener(() => _away.SetActive(false));
            UiKit.Rect("Text", ok).Stretch(0, 0, 1, 1).AddText("Nhận cà rốt", 36, UiKit.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _away.SetActive(false);
        }

        /// <summary>Pushes the current state into every label and button.</summary>
        public void Refresh()
        {
            if (!_built) return;
            GameState s = Runner.State;
            IReadOnlyList<StationDef> defs = Runner.Defs;

            _coins.text = s.Coins.ToShortString();
            BigNum perSecond = Economy.IncomePerSecond(defs, s);
            _perSecond.text = "+" + (perSecond < new BigNum(10) ? UiKit.Seconds(perSecond.ToDouble()) : perSecond.ToShortString()) + "/giây";
            _hint.SetActive(!s.HintSeen);

            for (int i = 0; i < _rows.Count; i++)
            {
                _rows[i].Refresh(defs, s);
                _actors[i].SetLevel(s.Levels[i], defs[i].Kind == StationKind.Brew);
            }

            foreach (KeyValuePair<int, Image> pair in _modeButtons)
            {
                bool on = pair.Key == s.BuyMode;
                pair.Value.color = on ? UiKit.Panel2 : UiKit.Night;
                _modeLabels[pair.Key].color = on ? UiKit.Cream : UiKit.Muted;
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
        }

        void OnTap(int index)
        {
            if (Runner.State.Levels[index] > 0)
            {
                Runner.Tap(index);
                if (_hint.activeSelf) _hint.SetActive(false);
            }
        }

        void OnBuy(int index)
        {
            Runner.Buy(index);
        }

        void OnPayout(int index, BigNum amount)
        {
            if (_floaters.Count > 24) return;
            RectTransform actor = _actors[index].Root;
            var rt = UiKit.Rect("Gain", _garden);
            rt.anchorMin = rt.anchorMax = actor.anchorMin;
            rt.sizeDelta = new Vector2(320, 60);
            var start = new Vector2(0, 150f * actor.localScale.y);
            rt.anchoredPosition = start;
            Text text = rt.AddText("+" + amount.ToShortString(), 40, UiKit.Cream, TextAnchor.MiddleCenter, FontStyle.Bold);
            var outline = rt.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.06f, 0.08f, 0.19f, 0.9f);
            outline.effectDistance = new Vector2(2, -2);
            _floaters.Add(new Floater { Rect = rt, Text = text, Born = Time.unscaledTime, Start = start });
        }
    }
}

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class GardenMapView
    {
        const float CloseSize = 96f;
        const float WideTouch = 144f;
        const int MinShotPixels = 256;
        public const double SeaAround = 1d;

        readonly Garden25DWorld _world;
        readonly Canvas _canvas;
        MapFrame _frame;
        double _aspect = 1d;

        public RectTransform Root { get; }
        public RectTransform Pill { get; }
        public RectTransform CloseButton { get; }
        public RawImage Shot { get; }
        public bool Open => Root.gameObject.activeSelf;

        public GardenMapView(RectTransform canvas, GameCatalog catalog, Garden25DWorld world, float topCover, float bottomCover, Vector2 pillAt, Vector2 pillSize)
        {
            _world = world;
            _canvas = canvas.GetComponent<Canvas>();

            Root = UiKit.Rect("Island Map", canvas).Stretch(0, 0, 1, 1);
            Root.AddImage(null, Palette.Night).raycastTarget = true;
            Shot = UiKit.Rect("Shot", Root).Stretch(0, 0, 1, 1, 0, bottomCover, 0, topCover).gameObject.AddComponent<RawImage>();
            Shot.color = Color.white;
            Shot.raycastTarget = true;
            var tap = Shot.gameObject.AddComponent<EventTrigger>();
            var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            click.callback.AddListener(OnTap);
            tap.triggers.Add(click);

            CloseButton = UiKit.Rect("Close", Root).Place(0f, 1f, GardenScreen.ReadMargin + CloseSize / 2f, -(topCover + GardenScreen.ReadMargin + CloseSize / 2f), CloseSize, CloseSize);
            CloseButton.AddShadow(14f, 10f, 0.3f);
            ChunkyButton close = null;
            close = UiKit.Chunky(CloseButton, catalog.Round, () =>
            {
                Sfx.Click();
                Close();
            });
            close.Lip.type = Image.Type.Simple;
            close.Face.type = Image.Type.Simple;
            close.Lip.color = Palette.CardLip;
            close.Face.color = Palette.Paper;
            Reach(CloseButton, CloseSize, CloseSize);
            UiKit.Rect("X", close.FaceRect).Stretch(0, 0, 1, 1).AddText("×", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold).raycastTarget = false;
            Root.gameObject.SetActive(false);

            Pill = UiKit.Rect("Map Pill", canvas).Place(1f, 0f, pillAt.x, pillAt.y, pillSize.x, pillSize.y);
            Pill.AddShadow(14f, 10f, 0.3f);
            Image back = Pill.AddImage(catalog.Panel, Palette.Paper, true);
            back.raycastTarget = true;
            var button = Pill.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = back;
            button.onClick.AddListener(() =>
            {
                Sfx.Click();
                Show();
            });
            Reach(Pill, pillSize.x, pillSize.y);
            var fold = UiKit.Rect("Fold", Pill).Place(0f, 0.5f, 48f, 0f, 60f, 52f);
            UiKit.Rect("West", fold).Stretch(0f, 0f, 1f / 3f, 1f, 1f, 2f, 1f, 6f).AddImage(catalog.Panel, Palette.Moss, true);
            UiKit.Rect("Middle", fold).Stretch(1f / 3f, 0f, 2f / 3f, 1f, 1f, 6f, 1f, 2f).AddImage(catalog.Panel, Palette.Grass, true);
            UiKit.Rect("East", fold).Stretch(2f / 3f, 0f, 1f, 1f, 1f, 2f, 1f, 6f).AddImage(catalog.Panel, Palette.Moss, true);
            Text label = UiKit.Rect("Text", Pill).Stretch(0, 0, 1, 1, 90, 8, 16, 8)
                .AddText(L10n.T("map.open"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
        }

        public void Show()
        {
            Root.gameObject.SetActive(true);
            Pill.gameObject.SetActive(false);
            Canvas.ForceUpdateCanvases();
            Rect area = Shot.rectTransform.rect;
            float scale = _canvas != null && _canvas.scaleFactor > 0f ? _canvas.scaleFactor : 1f;
            int height = Mathf.Max(MinShotPixels, Mathf.RoundToInt(area.height * scale));
            int width = Mathf.Max(1, Mathf.RoundToInt(height * area.width / Mathf.Max(1f, area.height)));
            _aspect = (double)width / height;
            GroundRect seen = GardenFraming.Pad(_world.Island, SeaAround);
            _frame = GardenFraming.Map(seen, _aspect);
            Shot.texture = _world.Map.Shoot(seen, width, height);
        }

        public void Close()
        {
            Root.gameObject.SetActive(false);
            Shot.texture = null;
            _world.Map.Release();
        }

        public void ShowPill(bool shown)
        {
            bool want = shown && !Open;
            if (Pill.gameObject.activeSelf != want) Pill.gameObject.SetActive(want);
        }

        public void TapAt(double u, double v)
        {
            GroundPoint spot = GardenFraming.MapPoint(_frame, _aspect, u, v);
            Close();
            _world.PanZoom.TravelTo(spot);
        }

        static void Reach(RectTransform button, float width, float height)
        {
            float dx = Mathf.Max(0f, (WideTouch - width) / 2f), dy = Mathf.Max(0f, (WideTouch - height) / 2f);
            Image hit = UiKit.Rect("Hit", button).Stretch(0, 0, 1, 1, -dx, -dy, -dx, -dy).AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
        }

        void OnTap(BaseEventData data)
        {
            var press = (PointerEventData)data;
            Vector2 moved = press.position - press.pressPosition;
            if (!GardenFraming.IsTap(moved.x, moved.y, Screen.width, Screen.height)) return;
            RectTransform area = Shot.rectTransform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(area, press.position, null, out Vector2 local)) return;
            Rect r = area.rect;
            Sfx.Click();
            TapAt((local.x - r.xMin) / r.width, (local.y - r.yMin) / r.height);
        }
    }
}

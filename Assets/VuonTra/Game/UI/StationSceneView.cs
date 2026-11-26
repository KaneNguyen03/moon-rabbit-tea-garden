using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class StationSceneView
    {
        public const float Height = 72f, Art = 64f, Gap = 16f;
        const float ModePills = 434f, ModeChrome = 34f;

        public static float WidthBeside(float inner) => inner - (ModeChrome + ModePills * UiKit.TextScale) - Gap;

        public readonly RectTransform Root;
        public readonly Image Plate, Prop, Dish, Worker;

        readonly GameCatalog _catalog;
        readonly Image[] _row;
        StationData _shown;
        int _shownDish = -1;
        long _shownWorker;
        bool _shownOpen;

        public StationSceneView(RectTransform parent, GameCatalog catalog, float left, float top, float width)
        {
            _catalog = catalog;
            Root = UiKit.Rect("Scene", parent).Place(0f, 1f, left + width / 2f, -top - Height / 2f, width, Height);
            Plate = Root.AddFrame(FrameKind.Slot, catalog.Panel, Palette.Track);
            Worker = Picture("Worker");
            Prop = Picture("Prop");
            Dish = Picture("Dish");
            _row = new[] { Worker, Prop, Dish };
        }

        Image Picture(string name)
        {
            Image picture = UiKit.Rect(name, Root).Place(0f, 0.5f, 0, 0, Art, Art).AddImage(null, Color.white);
            picture.preserveAspect = true;
            return picture;
        }

        public void Show(StationData data, int dish, Rabbit worker, bool open)
        {
            long workerId = open && worker != null ? worker.Id : 0L;
            if (_shown == data && _shownDish == dish && _shownWorker == workerId && _shownOpen == open) return;
            _shown = data;
            _shownDish = dish;
            _shownWorker = workerId;
            _shownOpen = open;
            Plate.color = UiFrames.Tint(FrameKind.Slot, Palette.Mix(data.Accent, Palette.Paper, 0.72f));
            bool staffed = workerId != 0L;
            Set(Worker, staffed ? _catalog.RabbitArt(worker) : null, staffed ? RabbitGridView.Fur(worker) : Color.white);
            Set(Prop, data.Prop, Color.white);
            Set(Dish, open ? data.Product : null, DishIcon.Tint(dish));
            int shown = 0;
            foreach (Image picture in _row) if (picture.enabled) shown++;
            float step = Root.sizeDelta.x / Mathf.Max(1, shown), x = step / 2f;
            foreach (Image picture in _row)
            {
                if (!picture.enabled) continue;
                picture.rectTransform.anchoredPosition = new Vector2(x, 0f);
                x += step;
            }
        }

        static void Set(Image picture, Sprite sprite, Color tint)
        {
            picture.sprite = sprite;
            picture.color = tint;
            picture.enabled = sprite != null;
        }
    }
}

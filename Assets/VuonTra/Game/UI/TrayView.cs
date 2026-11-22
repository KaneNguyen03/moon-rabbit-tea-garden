using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The short tray above the tab bar where the stations' special batches wait: a slot per stack, a
    /// touch sells one, a swipe across sells everything, and a mark says when nothing more fits.
    /// </summary>
    public sealed class TrayView
    {
        public RectTransform Root { get; private set; }
        public const float Height = 112f;
        /// <summary>How far a finger travels across the tray for it to count as a sweep.</summary>
        public const float SweepDistance = 120f;

        readonly RectTransform[] _slots = new RectTransform[Tray.MaxSlots];
        readonly Image[] _icons = new Image[Tray.MaxSlots];
        readonly GameObject[] _counts = new GameObject[Tray.MaxSlots], _gold = new GameObject[Tray.MaxSlots];
        readonly Text[] _countTexts = new Text[Tray.MaxSlots];
        readonly GameObject _full;
        readonly GameCatalog _catalog;
        float _sweepStart;
        bool _sweeping;

        public TrayView(RectTransform canvas, GameCatalog catalog, Action<int> onSell, Action onSellAll)
        {
            _catalog = catalog;
            Root = UiKit.Rect("Tray", canvas).Place(0.5f, 0f, 0, 0, 1040, Height);
            Root.pivot = new Vector2(0.5f, 0f);
            Root.AddShadow(18f, 10f, 0.3f);
            UiKit.Rect("Lip", Root).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            Image plate = UiKit.Rect("Plate", Root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            plate.raycastTarget = true; // a touch on the tray is not a touch on the garden behind it

            // A sweep across the tray sells everything: the sweep is measured from where the finger went down.
            var trigger = Root.gameObject.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.BeginDrag, e => { _sweeping = true; _sweepStart = ((PointerEventData)e).position.x; });
            Add(trigger, EventTriggerType.EndDrag, e =>
            {
                if (!_sweeping) return;
                _sweeping = false;
                Canvas c = canvas.GetComponent<Canvas>();
                float scale = c != null && c.scaleFactor > 0.01f ? c.scaleFactor : 1f;
                if (Mathf.Abs(((PointerEventData)e).position.x - _sweepStart) / scale >= SweepDistance) onSellAll();
            });

            for (int i = 0; i < Tray.MaxSlots; i++)
            {
                int slot = i;
                var rt = UiKit.Rect("Slot " + i, Root).Place(0f, 0.5f, 72 + i * 118f, 4, 96, 96);
                _slots[i] = rt;
                // Each product sits on a little wooden dish once that is drawn; a plain disc until then.
                Image back = rt.AddImage(catalog.TrayWood != null ? catalog.TrayWood : catalog.Round, catalog.TrayWood != null ? Color.white : Palette.Track);
                back.preserveAspect = true;
                back.raycastTarget = true;
                var button = rt.gameObject.AddComponent<Button>();
                button.transition = Selectable.Transition.None;
                button.targetGraphic = back;
                button.onClick.AddListener(() => onSell(slot));
                // A warm ring round a golden batch.
                var gold = UiKit.Rect("Gold", rt).Stretch(0, 0, 1, 1, -6, -6, -6, -6);
                gold.AddImage(catalog.Round, Palette.Lantern);
                gold.SetAsFirstSibling();
                _gold[i] = gold.gameObject;
                _icons[i] = UiKit.Rect("Icon", rt).Stretch(0, 0, 1, 1, 12, 12, 12, 12).AddImage(null, Color.white);
                _icons[i].preserveAspect = true;
                var count = UiKit.Rect("Count", rt).Place(1f, 0f, -14, 14, 48, 34);
                count.AddImage(catalog.Panel, Palette.CarrotLip, true);
                _countTexts[i] = UiKit.Rect("Text", count).Stretch(0, 0, 1, 1).AddText("", 20, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
                _counts[i] = count.gameObject;
            }

            // The mark that says nothing more fits, on the tray's far end.
            var full = UiKit.Rect("Full", Root).Place(1f, 0.5f, -70, 4, 112, 44);
            full.AddImage(catalog.Panel, Palette.Tiers[5], true);
            UiKit.Rect("Text", full).Stretch(0, 0, 1, 1).AddText(L10n.T("tray.full"), 20, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _full = full.gameObject;
            _full.SetActive(false);
        }

        static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(call);
            trigger.triggers.Add(entry);
        }

        /// <summary>The picture of a station's product: its own when drawn, its prop until then.</summary>
        public Sprite ProductSprite(int station)
        {
            StationData data = station >= 0 && station < _catalog.Stations.Length ? _catalog.Stations[station] : null;
            if (data == null) return null;
            return data.Product != null ? data.Product : data.Prop;
        }

        /// <summary>Where a slot sits, for things flying to or from it.</summary>
        public RectTransform Slot(int index) => _slots[Mathf.Clamp(index, 0, _slots.Length - 1)];

        public void Refresh(GameState s)
        {
            for (int i = 0; i < Tray.MaxSlots; i++)
            {
                bool open = i < s.TraySlots;
                if (_slots[i].gameObject.activeSelf != open) _slots[i].gameObject.SetActive(open);
                if (!open) continue;
                Product p = i < s.Tray.Count ? s.Tray[i] : null;
                bool has = p != null;
                if (_icons[i].gameObject.activeSelf != has) _icons[i].gameObject.SetActive(has);
                if (has) _icons[i].sprite = ProductSprite(p.Station);
                bool many = has && p.Count > 1;
                if (_counts[i].activeSelf != many) _counts[i].SetActive(many);
                if (many) _countTexts[i].text = "×" + p.Count;
                bool gold = has && p.Golden;
                if (_gold[i].activeSelf != gold) _gold[i].SetActive(gold);
            }
            bool full = Tray.IsFull(s);
            if (_full.activeSelf != full) _full.SetActive(full);
        }

        /// <summary>A slot swells for a moment: something landed in it.</summary>
        public void Pop(Tweens tweens, int index)
        {
            RectTransform slot = Slot(index);
            tweens.Add(Motion.Tap, t => { if (slot != null) slot.localScale = Vector3.one * (1f + 0.22f * Ease.Pop(t)); }, Ease.Linear, 0f, () => { if (slot != null) slot.localScale = Vector3.one; });
        }
    }
}

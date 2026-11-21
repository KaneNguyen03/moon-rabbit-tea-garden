using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>The overlay where the player chooses what a rabbit wears in one slot.</summary>
    public sealed class ItemPickerView
    {
        readonly GameObject _root;
        readonly RectTransform _card, _content;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action<long, long> _onEquip;
        readonly Action<long> _onUnequip;
        readonly Text _title;
        readonly List<GameObject> _rows = new List<GameObject>();
        long _rabbitId;
        ItemSlot _slot;
        string _signature;

        public bool IsOpen => _root.activeSelf;

        public ItemPickerView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<long, long> onEquip, Action<long> onUnequip)
        {
            _catalog = catalog;
            _tweens = tweens;
            _onEquip = onEquip;
            _onUnequip = onUnequip;

            var overlay = UiKit.Rect("Picker", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrimButton = _root.AddComponent<Button>();
            scrimButton.transition = Selectable.Transition.None;
            scrimButton.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 980, 820);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            _title = UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 40, -120, 130, 20)
                .AddText("", 42, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, 80, 80);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var scrollRt = UiKit.Rect("Scroll", _card).Stretch(0, 0, 1, 1, 0, 28, 0, 130);
            scrollRt.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            _content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            _content.pivot = new Vector2(0.5f, 1f);
            var list = _content.gameObject.AddComponent<VerticalLayoutGroup>();
            list.padding = new RectOffset(28, 28, 4, 30);
            list.spacing = 16;
            list.childControlWidth = true;
            list.childControlHeight = true;
            list.childForceExpandWidth = true;
            list.childForceExpandHeight = false;
            _content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = _content;
            scroll.viewport = scrollRt;

            _root.SetActive(false);
        }

        /// <summary>What an item does, in the player's words.</summary>
        public static string Describe(Item item)
        {
            int percent = Mathf.RoundToInt((float)(item.Amount * 100d));
            switch (item.Effect)
            {
                case ItemEffect.GardenSpeed: return L10n.T("item.effect.speed", percent);
                case ItemEffect.AwayIncome: return L10n.T("item.effect.away", percent);
                default: return L10n.T("item.effect.station", percent);
            }
        }

        public static string SlotName(ItemSlot slot) => L10n.T(slot == ItemSlot.Hat ? "slot.hat" : "slot.charm");

        public void Open(GameState s, long rabbitId, ItemSlot slot)
        {
            Rabbit rabbit = Rabbits.Find(s, rabbitId);
            if (rabbit == null) return;
            _rabbitId = rabbitId;
            _slot = slot;
            _signature = null;
            _title.text = L10n.T("picker.title", SlotName(slot), rabbit.Name);
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(0.25f, t => _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t), Ease.OutBack);
            Refresh(s);
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        public void Refresh(GameState s)
        {
            if (!IsOpen) return;
            if (Rabbits.Find(s, _rabbitId) == null)
            {
                Close(); // the rabbit was merged away
                return;
            }

            var sb = new System.Text.StringBuilder();
            foreach (Item item in s.Items)
                if (item.Slot == _slot) sb.Append(item.Id).Append(':').Append(item.WornBy).Append(';');
            string signature = sb.ToString();
            if (signature == _signature) return;
            _signature = signature;

            foreach (GameObject go in _rows)
            {
                if (go == null) continue;
                go.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                else UnityEngine.Object.DestroyImmediate(go);
            }
            _rows.Clear();

            var items = new List<Item>();
            foreach (Item item in s.Items)
                if (item.Slot == _slot) items.Add(item);
            items.Sort((a, b) => b.Tier != a.Tier ? b.Tier.CompareTo(a.Tier) : a.Id.CompareTo(b.Id));

            if (items.Count == 0)
            {
                var note = UiKit.Rect("Empty", _content);
                note.gameObject.AddComponent<LayoutElement>().preferredHeight = 120;
                note.AddText(L10n.T("picker.empty"), 28, Palette.InkSoft, TextAnchor.MiddleLeft);
                _rows.Add(note.gameObject);
                return;
            }
            foreach (Item item in items) AddRow(s, item);
        }

        void AddRow(GameState s, Item item)
        {
            Color tier = Palette.Tiers[item.Tier];
            bool wornHere = item.WornBy == _rabbitId;
            var root = UiKit.Rect("Item " + item.Id, _content);
            root.AddImage(_catalog.Panel, Palette.Mix(tier, Palette.Ink, 0.25f), true);
            root.gameObject.AddComponent<LayoutElement>().preferredHeight = 150;
            UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(_catalog.Panel, Palette.Card, true);

            var tile = UiKit.Rect("Tile", root).Place(0f, 0.5f, 86, 4, 108, 108);
            tile.AddImage(_catalog.Round, Palette.Mix(tier, Palette.Paper, 0.3f));
            UiKit.Rect("Glyph", tile).Stretch(0, 0, 1, 1).AddText(Glyph(item), 46, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            UiKit.Rect("Name", root).Stretch(0, 0.5f, 1, 1, 164, 0, 290, 22)
                .AddText(L10n.T("picker.name", L10n.ItemName(item), L10n.Tier(item.Tier)), 32, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            Rabbit wearer = item.WornBy > 0 ? Rabbits.Find(s, item.WornBy) : null;
            string where = wornHere ? L10n.T("picker.worn.here") : (wearer != null ? L10n.T("picker.worn.other", wearer.Name) : "");
            UiKit.Rect("Sub", root).Stretch(0, 0.12f, 1, 0.5f, 164, 0, 290, 4)
                .AddText(Describe(item) + where, 25, Palette.InkSoft, TextAnchor.UpperLeft);

            long itemId = item.Id;
            var button = UiKit.Rect("Use", root).Place(1f, 0.5f, -140, 4, 230, 100);
            ChunkyButton chunky = UiKit.Chunky(button, _catalog.Panel, () =>
            {
                if (wornHere) _onUnequip(itemId);
                else _onEquip(itemId, _rabbitId);
            });
            Text label = UiKit.Rect("Text", chunky.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(wornHere ? L10n.T("picker.remove") : L10n.T("picker.wear"), 34, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            if (wornHere)
            {
                chunky.Lip.color = Palette.OffLip;
                chunky.Face.color = Palette.Track;
                label.color = Palette.Ink;
            }
            _rows.Add(root.gameObject);
        }

        /// <summary>The first letter stands in for a picture until items have art.</summary>
        public static string Glyph(Item item)
        {
            string name = L10n.ItemName(item);
            return string.IsNullOrEmpty(name) ? "?" : name.Substring(0, 1).ToUpperInvariant();
        }
    }
}

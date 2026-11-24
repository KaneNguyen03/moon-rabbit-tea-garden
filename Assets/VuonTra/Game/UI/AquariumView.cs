using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class AquariumView
    {
        public const float Height = 1330f;
        const float SlotWidth = 316f, SlotHeight = 300f, SlotStep = 330f;
        const float TankWidth = 900f, TankHeight = 250f;

        sealed class Slot
        {
            public RectTransform Root;
            public Image Art, Back;
            public Text Count, Next;
            public GameObject Fresh;
            public Image[] Pips;
        }

        readonly GameRunner _runner;
        readonly GameCatalog _catalog;
        readonly Slot[] _slots = new Slot[Aquarium.Kinds];
        readonly Image[] _swimmers = new Image[Aquarium.Kinds];
        readonly Text _sub, _set;

        public BottomSheet Sheet { get; private set; }
        public bool IsOpen => Sheet.IsOpen;
        public string Sub => _sub.text;
        public string SetLine => _set.text;
        public string CountOf(int fish) => _slots[fish].Count.text;
        public string NextOf(int fish) => _slots[fish].Next.text;
        public bool FreshShown(int fish) => _slots[fish].Fresh.activeSelf;
        public bool SwimmerShown(int fish) => _swimmers[fish].gameObject.activeSelf;

        public AquariumView(RectTransform canvas, GameCatalog catalog, Tweens tweens, GameRunner runner)
        {
            _runner = runner;
            _catalog = catalog;
            Sheet = new BottomSheet(canvas, "Aquarium", catalog, tweens, Height, () => _runner.SeeTankFresh());
            RectTransform card = Sheet.Card;

            var close = UiKit.Rect("Close", card).Place(1f, 1f, -66, -66, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            Fit(UiKit.Rect("Title", card).Place(0.5f, 1f, 0, -86, 760, 64)
                .AddText(L10n.T("tank.title"), 44, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 44, 28);
            _sub = Fit(UiKit.Rect("Sub", card).Place(0.5f, 1f, 0, -150, 940, 48)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 14);

            RectTransform water = DrawTank(UiKit.Rect("Tank", card).Place(0.5f, 1f, 0, -320, TankWidth, TankHeight), catalog);
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                float x = -340f + 136f * k, y = k % 2 == 0 ? 22f : -30f;
                _swimmers[k] = FishArt(UiKit.Rect("Fish " + k, water).Place(0.5f, 0.5f, x, y, 104, 78), catalog, k);
            }

            for (int k = 0; k < Aquarium.Kinds; k++) _slots[k] = BuildSlot(card, k);

            Fit(UiKit.Rect("Rule", card).Place(0.5f, 0f, 0, 176, 960, 44)
                .AddText(L10n.T("tank.rule", string.Join("·", Aquarium.Milestones), string.Join("·", System.Array.ConvertAll(Aquarium.MilestoneBonus, Percent))), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter), UiKit.TextSmall, 12);
            Fit(UiKit.Rect("Keep", card).Place(0.5f, 0f, 0, 128, 960, 40)
                .AddText(L10n.T("tank.keep"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextSmall, 12);
            var setRow = UiKit.Rect("Set", card).Place(0.5f, 0f, 0, 62, 960, 64);
            setRow.AddImage(catalog.Panel, Palette.Mix(Palette.Lantern, Palette.Paper, 0.6f), true);
            _set = Fit(UiKit.Rect("Text", setRow).Stretch(0, 0, 1, 1, 20, 0, 20, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 12);

            Sheet.Root.SetActive(false);
        }

        Slot BuildSlot(RectTransform card, int fish)
        {
            int column = fish % 3, row = fish / 3;
            var slot = new Slot();
            slot.Root = UiKit.Rect("Slot " + fish, card).Place(0.5f, 1f, (column - 1) * SlotStep, -630 - row * (SlotHeight + 24f), SlotWidth, SlotHeight);
            slot.Back = slot.Root.AddImage(_catalog.Panel, Palette.Card, true);
            slot.Art = FishArt(UiKit.Rect("Art", slot.Root).Place(0.5f, 1f, 0, -66, 150, 104), _catalog, fish);
            Fit(UiKit.Rect("Name", slot.Root).Place(0.5f, 1f, 0, -140, SlotWidth - 24, 40)
                .AddText(L10n.T("fish." + fish), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 12);
            slot.Count = Fit(UiKit.Rect("Count", slot.Root).Place(0.5f, 1f, 0, -188, SlotWidth - 24, 52)
                .AddText("×0", UiKit.TextTitle, Palette.CarrotLip, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);
            slot.Next = Fit(UiKit.Rect("Next", slot.Root).Place(0.5f, 1f, 0, -232, SlotWidth - 24, 36)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextSmall, 11);
            int marks = Aquarium.Milestones.Length;
            slot.Pips = new Image[marks];
            for (int i = 0; i < marks; i++)
                slot.Pips[i] = UiKit.Rect("Pip " + i, slot.Root).Place(0.5f, 0f, (i - (marks - 1) / 2f) * 34f, 26, 22, 22).AddImage(_catalog.Round, Palette.Track);
            var fresh = UiKit.Rect("Fresh", slot.Root).Place(1f, 1f, -50, -24, 84, 38);
            fresh.AddImage(_catalog.Panel, Palette.CarrotLip, true);
            Fit(UiKit.Rect("Text", fresh).Stretch(0, 0, 1, 1, 6, 0, 6, 0)
                .AddText(L10n.T("tank.new"), UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextSmall, 11);
            slot.Fresh = fresh.gameObject;
            slot.Fresh.SetActive(false);
            return slot;
        }

        public static string Percent(double share) =>
            L10n.Number((share * 100d).ToString("0.##", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ','));

        static Text Fit(Text text, int size, int least)
        {
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(size);
            return text;
        }

        public static Image FishArt(RectTransform rt, GameCatalog catalog, int fish)
        {
            Sprite art = catalog.FishSprite(fish);
            Image img = rt.AddImage(art != null ? art : catalog.Round, art != null ? Color.white : Palette.Mix(Palette.Lantern, Palette.Carrot, fish / 6f));
            img.preserveAspect = true;
            return img;
        }

        public static RectTransform DrawTank(RectTransform at, GameCatalog catalog)
        {
            Vector2 size = at.sizeDelta;
            float rim = Mathf.Max(6f, size.y * 0.06f);
            UiKit.Rect("Shadow", at).Place(0.5f, 0f, 0, 2, size.x * 1.04f, size.y * 0.16f).AddImage(catalog.Glow, Palette.Alpha(Palette.Night, 0.35f));
            UiKit.Rect("Frame", at).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Night, 0.35f), true);
            var water = UiKit.Rect("Water", at).Stretch(0, 0, 1, 1, rim, rim, rim, rim * 1.6f);
            water.AddImage(catalog.Panel, Palette.Mix(Palette.Night, new Color(0.25f, 0.45f, 0.62f), 0.55f), true);
            water.gameObject.AddComponent<RectMask2D>();
            if (catalog.PondWater != null)
            {
                Vector2 art = catalog.PondWater.rect.size;
                Vector2 inner = size - new Vector2(rim * 2f, rim * 2.6f);
                float k = Mathf.Max(inner.x / (art.x * 0.62f), inner.y / (art.y * 0.56f));
                UiKit.Rect("Pond", water).Place(0.5f, 0.5f, 0, 0, art.x * k, art.y * k).AddImage(catalog.PondWater, Color.white).preserveAspect = true;
            }
            UiKit.Rect("Sand", water).Stretch(0, 0, 1, 0.16f).AddImage(catalog.Panel, Palette.Alpha(Palette.Mix(Palette.Lantern, Palette.Paper, 0.5f), 0.85f), true);
            UiKit.Rect("Glass", at).Stretch(0, 0, 1, 1, rim, rim, rim, rim * 1.6f).AddImage(catalog.Panel, Palette.Alpha(Color.white, 0.14f), true);
            UiKit.Rect("Shine", at).Place(0f, 0.5f, rim * 2.2f, size.y * 0.06f, Mathf.Max(4f, Mathf.Min(10f, size.x * 0.015f)), size.y * 0.56f).AddImage(catalog.Round, Palette.Alpha(Color.white, 0.35f));
            UiKit.Rect("Rim", at).Stretch(0, 1, 1, 1, -rim * 0.5f, -rim * 1.6f, -rim * 0.5f, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Carrot, Palette.Ink, 0.45f), true);
            foreach (Image image in at.GetComponentsInChildren<Image>()) image.raycastTarget = false;
            return water;
        }

        public void Open()
        {
            Sheet.Open();
            Sheet.Root.transform.SetAsLastSibling();
            Draw();
        }

        public void Close() => Sheet.Close();

        public void Draw()
        {
            GameState s = _runner.State;
            int kinds = Aquarium.KindsIn(s);
            _sub.text = L10n.T("tank.sub", kinds, Aquarium.Kinds, Percent(Aquarium.Bonus(s)), Percent(Aquarium.MaxBonus));
            for (int k = 0; k < Aquarium.Kinds; k++)
            {
                int count = Aquarium.Released(s, k);
                Slot slot = _slots[k];
                bool any = count > 0;
                slot.Count.text = "×" + L10n.Number(count.ToString());
                int next = Aquarium.NextMilestone(count);
                slot.Next.text = !any ? L10n.T("tank.none") : next > 0 ? L10n.T("tank.next", next, Percent(Aquarium.NextBonus(count))) : L10n.T("tank.done");
                slot.Art.color = any || slot.Art.sprite == _catalog.Round ? Color.white : Palette.Alpha(Palette.Ink, 0.35f);
                slot.Back.color = any ? Palette.Card : Palette.Mix(Palette.Card, Palette.Track, 0.5f);
                int reached = Aquarium.MilestonesOf(count);
                for (int i = 0; i < slot.Pips.Length; i++) slot.Pips[i].color = i < reached ? Palette.Moss : Palette.Track;
                bool fresh = Aquarium.IsFresh(s, k);
                if (slot.Fresh.activeSelf != fresh) slot.Fresh.SetActive(fresh);
                if (_swimmers[k].gameObject.activeSelf != any) _swimmers[k].gameObject.SetActive(any);
            }
            _set.text = s.Aquarium.SetGiven
                ? L10n.T("tank.set.got", Aquarium.Kinds)
                : L10n.T("tank.set.todo", Aquarium.Kinds, Aquarium.SetGolden, Aquarium.SetJars);
        }
    }
}

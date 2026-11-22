using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// One rabbit's own sheet, sliding up from the bottom: its portrait dressed as it is, its name, kind and
    /// worth, levelling, its trade, what it wears, and where it works. Also shows a kind from the album.
    /// </summary>
    public sealed class RabbitDetailView
    {
        readonly GameObject _root;
        readonly RectTransform _card, _body;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Action<long> _onLevelUp, _onLevelMax, _onRename, _onRest, _onWash;
        readonly Action<long, ItemSlot> _onSlot;
        readonly Action<long, int> _onAssign, _onDye;
        long _rabbitId;
        string _kindId, _signature;

        public bool IsOpen => Sheet.IsOpen;
        /// <summary>The sliding card this view lives on.</summary>
        public BottomSheet Sheet { get; private set; }
        public long RabbitId => _rabbitId;

        public RabbitDetailView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<long> onLevelUp, Action<long> onLevelMax, Action<long> onRename,
            Action<long, ItemSlot> onSlot, Action<long, int> onAssign, Action<long> onRest, Action<long, int> onDye, Action<long> onWash)
        {
            _catalog = catalog;
            _tweens = tweens;
            _onLevelUp = onLevelUp;
            _onLevelMax = onLevelMax;
            _onRename = onRename;
            _onSlot = onSlot;
            _onAssign = onAssign;
            _onRest = onRest;
            _onDye = onDye;
            _onWash = onWash;

            Sheet = new BottomSheet(canvas, "RabbitSheet", catalog, tweens, 1520f, OnClosed, 0.6f);
            _root = Sheet.Root;
            _card = Sheet.Card;
            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -66, -66, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(Close);
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _body = UiKit.Rect("Body", _card).Stretch(0, 0, 1, 1);
            _root.SetActive(false);
        }

        public void Open(IReadOnlyList<StationDef> defs, GameState s, long rabbitId)
        {
            if (Rabbits.Find(s, rabbitId) == null) return;
            _rabbitId = rabbitId;
            _kindId = null;
            Show(defs, s);
        }

        /// <summary>A page of the album: what this kind is and where it does best.</summary>
        public void OpenKind(IReadOnlyList<StationDef> defs, GameState s, string kindId)
        {
            if (RabbitKinds.Find(kindId) == null) return;
            _rabbitId = 0;
            _kindId = kindId;
            Show(defs, s);
        }

        void Show(IReadOnlyList<StationDef> defs, GameState s)
        {
            _signature = null;
            Sheet.Open();
            _root.transform.SetAsLastSibling();
            Refresh(defs, s);
        }

        public void Close() => Sheet.Close();

        void OnClosed()
        {
            _rabbitId = 0;
            _kindId = null;
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s)
        {
            if (!IsOpen) return;
            Rabbit r = _kindId == null ? Rabbits.Find(s, _rabbitId) : null;
            if (_kindId == null && r == null)
            {
                Close(); // merged away while its sheet was open
                return;
            }

            string signature = _kindId != null ? "kind:" + _kindId + L10n.Code : Signature(defs, s, r);
            if (signature == _signature) return;
            _signature = signature;
            for (int i = _body.childCount - 1; i >= 0; i--)
            {
                GameObject child = _body.GetChild(i).gameObject;
                child.SetActive(false);
                if (Application.isPlaying) UnityEngine.Object.Destroy(child);
                else UnityEngine.Object.DestroyImmediate(child);
            }
            if (_kindId != null) BuildKind(defs, RabbitKinds.Find(_kindId));
            else BuildRabbit(defs, s, r);
        }

        string Signature(IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            var sb = new StringBuilder();
            sb.Append(r.Id).Append(r.Tier).Append(r.Kind).Append(':').Append(r.Level).Append(':').Append(r.Station).Append(':').Append(r.Name).Append(':').Append(r.Affection).Append(L10n.Code);
            sb.Append(':').Append(Rabbits.LevelsAffordable(r, Simulation.Budget(s), out _)); // the max button's count
            sb.Append(s.Coins >= Rabbits.LevelCost(r) ? '+' : '-');
            sb.Append(':').Append(r.Dye).Append(s.GoldenCarrots >= Shop.DyeCost ? '+' : '-'); // the dye row
            foreach (Item item in s.Items) sb.Append(item.Id).Append('>').Append(item.WornBy).Append(';');
            // Who stands where, and which stations are open, decide the list of places.
            for (int i = 0; i < defs.Count; i++)
            {
                Rabbit there = Rabbits.AtStation(s, i);
                sb.Append(s.Levels[i] > 0 && !Layout.IsStored(defs, s, i) ? '1' : '0').Append(there != null ? there.Id : 0).Append(',');
            }
            return sb.ToString();
        }

        Text Line(string name, float y, float height, string text, int size, Color color, FontStyle style = FontStyle.Normal, float left = 420f, float right = 110f)
        {
            Text t = UiKit.Rect(name, _body).Stretch(0, 1, 1, 1, left, -(y + height), right, y)
                .AddText(text, size, color, TextAnchor.MiddleLeft, style);
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = Mathf.Max(14, size - 12);
            t.resizeTextMaxSize = size;
            return t;
        }

        void BuildRabbit(IReadOnlyList<StationDef> defs, GameState s, Rabbit r)
        {
            long id = r.Id;
            Color tier = Palette.Tiers[r.Tier];
            bool inTrade = Rabbits.InTrade(defs, r);
            RabbitTrade trade = Rabbits.TradeOf(r);

            // Left: the rabbit itself, large, wearing what it wears.
            var holder = UiKit.Rect("Portrait", _body).Place(0f, 1f, 216, -236, 360, 360);
            RabbitGridView.Portrait(holder, _catalog, s, r, 0, 0, 360);

            // Right: who it is.
            Line("Name", 56, 60, r.Name, 46, Palette.Ink, FontStyle.Bold, 420, 200);
            var pencil = UiKit.Rect("Rename", _body).Place(1f, 1f, -156, -86, 64, 64);
            UiKit.Touchable(pencil, 64, 64);
            Image pencilImage = pencil.AddImage(_catalog.Round, Palette.Track);
            pencilImage.raycastTarget = true;
            var pencilButton = pencil.gameObject.AddComponent<Button>();
            pencilButton.transition = Selectable.Transition.None;
            pencilButton.targetGraphic = pencilImage;
            pencilButton.onClick.AddListener(() => _onRename(id));
            var shaft = UiKit.Rect("Shaft", pencil).Place(0.5f, 0.5f, 3, 3, 9, 30);
            shaft.localRotation = Quaternion.Euler(0, 0, -45);
            shaft.AddImage(_catalog.Panel, Palette.Ink, true);
            var tip = UiKit.Rect("Tip", pencil).Place(0.5f, 0.5f, -11, -11, 8, 8);
            tip.localRotation = Quaternion.Euler(0, 0, 45);
            tip.AddImage(null, Palette.CarrotLip);

            var plate = UiKit.Rect("Kind", _body).Place(0f, 1f, 420 + 210, -150, 420, 46);
            plate.AddImage(_catalog.Panel, tier, true);
            Text plateText = UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1, 12, 0, 12, 0)
                .AddText(L10n.T("detail.kind", L10n.Kind(r), L10n.Rarity(r.Tier)), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            plateText.resizeTextForBestFit = true;
            plateText.resizeTextMinSize = 15;
            plateText.resizeTextMaxSize = 24;

            Line("Worth", 186, 54, L10n.T("detail.worth", UiKit.Times(Rabbits.WorkMultiplier(defs, s, r))), 40, Palette.Ink, FontStyle.Bold);

            var tradeChip = UiKit.Rect("Trade", _body).Place(0f, 1f, 420 + 150, -272, 300, 44);
            tradeChip.AddImage(_catalog.Panel, inTrade ? Palette.Lantern : Palette.Track, true);
            Image tradeIcon = UiKit.Rect("Icon", tradeChip).Place(0f, 0.5f, 28, 0, 30, 30).AddImage(_catalog.TradeIcon(trade), Palette.Ink);
            tradeIcon.preserveAspect = true;
            UiKit.Rect("Text", tradeChip).Stretch(0, 0, 1, 1, 52, 0, 10, 0)
                .AddText(L10n.Trade(trade) + (inTrade ? L10n.T("rabbits.intrade", UiKit.Times(Rabbits.TradeBonus)) : ""), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            // The bond (#19): full hearts for the days it has been stroked, hollow for the ones still to come.
            var bond = UiKit.Rect("Bond", _body).Place(0f, 1f, 420 + 300 + 20 + 110, -272, 220, 44);
            bond.AddImage(_catalog.Panel, Palette.Alpha(Palette.Paper, 0.92f), true);
            UiKit.Rect("Text", bond).Stretch(0, 0, 1, 1, 10, 0, 10, 0)
                .AddText(Rabbits.Hearts(r.Affection), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            List<int> suited = Rabbits.SuitedStations(defs, s, r);
            if (suited.Count > 0)
            {
                var names = new StringBuilder();
                for (int k = 0; k < suited.Count && k < 3; k++) names.Append(k > 0 ? ", " : "").Append(L10n.Station(defs[suited[k]].Id));
                Line("Suits", 304, 34, L10n.T("rabbits.suits", names.ToString()), 22, RabbitGridView.Quiet, FontStyle.Normal, 420, 40);
            }

            // What it wears.
            AddSlot(s, r, ItemSlot.Hat, 420 + 50);
            AddSlot(s, r, ItemSlot.Charm, 420 + 180);

            // Levelling, the full width of the sheet.
            bool maxed = Rabbits.IsMaxLevel(r);
            int cap = Rabbits.LevelCaps[r.Tier];
            Line("Level", 492, 40, L10n.T("detail.level", r.Level, cap) + L10n.LevelStars(r), 30, Palette.Ink, FontStyle.Bold, 44, 520);
            var track = UiKit.Rect("LevelBar", _body).Stretch(0, 1, 1, 1, 44, -564, 520, 544);
            track.AddImage(_catalog.Panel, Palette.Track, true);
            UiKit.Rect("Fill", track).Stretch(0, 0, Mathf.Clamp01(r.Level / (float)cap), 1).AddImage(_catalog.Panel, tier, true);
            BigNum cost = Rabbits.LevelCost(r);
            bool afford = !maxed && s.Coins >= cost;
            var up = UiKit.Rect("LevelUp", _body).Place(1f, 1f, -344, -530, 280, 96);
            ChunkyButton upButton = UiKit.Chunky(up, _catalog.Panel, () => _onLevelUp(id));
            upButton.SetEnabled(afford);
            Color ink = ChunkyButton.InkFor(afford);
            UiKit.Rect("Top", upButton.FaceRect).Stretch(0, 0, 0.5f, 1, 18, 0, 0, 0)
                .AddText(maxed ? L10n.T("rabbits.maxlevel") : (afford ? L10n.T("rabbits.levelup") : L10n.T("need.more")), UiKit.TextSmall, ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Rect("Cost", upButton.FaceRect).Stretch(0.4f, 0, 1, 1, 0, 0, 18, 0)
                .AddText(maxed ? "" : (afford ? cost.Short() : (cost - s.Coins).Short()), UiKit.TextBody, ink, TextAnchor.MiddleRight, FontStyle.Bold);

            // As many levels as the purse (its spendable share) allows, stopping at the tier's cap.
            BigNum manyCost = BigNum.Zero;
            int many = maxed ? 0 : Rabbits.LevelsAffordable(r, Simulation.Budget(s), out manyCost);
            var max = UiKit.Rect("LevelMax", _body).Place(1f, 1f, -120, -530, 160, 96);
            ChunkyButton maxButton = UiKit.Chunky(max, _catalog.Panel, () => _onLevelMax(id));
            maxButton.SetEnabled(many > 1);
            Color maxInk = ChunkyButton.InkFor(many > 1);
            UiKit.Rect("Top", maxButton.FaceRect).Stretch(0, 0.5f, 1, 1, 8, 0, 8, 6)
                .AddText(L10n.T("mode.max"), UiKit.TextBody, maxInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            Text maxSub = UiKit.Rect("Sub", maxButton.FaceRect).Stretch(0, 0, 1, 0.5f, 6, 8, 6, 0)
                .AddText(many > 1 ? L10n.T("rabbits.levelmax", many, manyCost.Short()) : "", 18, maxInk, TextAnchor.MiddleCenter, FontStyle.Bold);
            maxSub.resizeTextForBestFit = true;
            maxSub.resizeTextMinSize = 12;
            maxSub.resizeTextMaxSize = 18;

            // Where it works: every open station, its own trade first, and a way to rest.
            bool working = r.Station >= 0 && r.Station < defs.Count;
            // A rabbit seated at a place to rest says what it adds to the whole garden (#60).
            string where = working ? L10n.T("detail.working", L10n.Station(defs[r.Station].Id))
                : RestSpots.IsSeated(s, r) ? L10n.T("detail.resting.share", RabbitGridView.Percent(RestSpots.BonusOf(r)))
                : L10n.T("detail.resting");
            Line("Where", 622, 44, where, 30, Palette.Ink, FontStyle.Bold, 44, 330);
            if (working)
            {
                var rest = UiKit.Rect("Rest", _body).Place(1f, 1f, -174, -644, 260, 68);
                UiKit.Secondary(rest, _catalog.Panel, L10n.T("rabbits.rest"), () => _onRest(id));
            }

            // Fur dye (#44): the palette as a row of dots, each a tap for golden carrots; washing it out is free.
            bool canDye = s.GoldenCarrots >= Shop.DyeCost;
            var dyeRow = UiKit.Rect("Dye", _body).Stretch(0, 1, 1, 1, 44, -800, 44, 694);
            Text dyeLabel = UiKit.Rect("Label", dyeRow).Stretch(0, 0.62f, 0.72f, 1)
                .AddText(canDye ? L10n.T("detail.dye", Shop.DyeCost) : L10n.T("detail.dye.need", Shop.DyeCost), UiKit.TextSmall, canDye ? Palette.Ink : RabbitGridView.Quiet, TextAnchor.MiddleLeft, FontStyle.Bold);
            dyeLabel.resizeTextForBestFit = true;
            dyeLabel.resizeTextMinSize = 13;
            dyeLabel.resizeTextMaxSize = UiKit.TextSmall;
            var dots = UiKit.Rect("Dots", dyeRow).Stretch(0, 0, 0.72f, 0.62f);
            for (int i = 0; i < Shop.Dyes.Length; i++)
            {
                int dye = i;
                bool worn = r.Dye == i;
                var dot = UiKit.Rect("Dot " + i, dots).Place((i + 0.5f) / Shop.Dyes.Length, 0.5f, 0, 0, 56, 56);
                Image hit = dot.AddImage(null, new Color(0, 0, 0, 0));
                hit.raycastTarget = true;
                var dotButton = dot.gameObject.AddComponent<Button>();
                dotButton.transition = Selectable.Transition.None;
                dotButton.targetGraphic = hit;
                dotButton.interactable = canDye && !worn;
                dotButton.onClick.AddListener(() => _onDye(id, dye));
                UiKit.Touchable(dot, 56, 56);
                var ring = UiKit.Rect("Ring", dot).Stretch(0, 0, 1, 1, -7, -7, -7, -7);
                ring.AddImage(_catalog.Round, Palette.Ink);
                ring.gameObject.SetActive(worn);
                Color swatch = Palette.Hex(Shop.Dyes[i].Hex);
                UiKit.Rect("Fill", dot).Stretch(0, 0, 1, 1).AddImage(_catalog.Round, canDye || worn ? swatch : Palette.Alpha(swatch, 0.45f));
            }
            var wash = UiKit.Rect("Wash", dyeRow).Stretch(0.74f, 0, 1, 1);
            Button washButton = UiKit.TextButton(wash, L10n.T("detail.wash"), () => _onWash(id), UiKit.TextBody);
            washButton.interactable = r.Dye >= 0;
            if (r.Dye < 0) foreach (Graphic g in wash.GetComponentsInChildren<Graphic>()) if (g.transform != wash) g.color = Palette.Alpha(RabbitGridView.Quiet, 0.7f);

            var scrollRt = UiKit.Rect("Stations", _body).Stretch(0, 0, 1, 1, 0, 30, 0, 814);
            scrollRt.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            var scroll = scrollRt.gameObject.AddComponent<ScrollRect>();
            scroll.horizontal = false;
            scroll.scrollSensitivity = 40f;
            scrollRt.gameObject.AddComponent<RectMask2D>();
            var content = UiKit.Rect("Content", scrollRt).Stretch(0, 1, 1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            scroll.content = content;
            scroll.viewport = scrollRt;

            var open = new List<int>();
            for (int i = 0; i < defs.Count; i++) if (s.Levels[i] > 0 && !Layout.IsStored(defs, s, i)) open.Add(i); // not a stored station (#54)
            // Its own trade first; within each group, the order the stations open in.
            open.Sort((a, b) =>
            {
                bool sa = Rabbits.Suits(defs, r, a), sb = Rabbits.Suits(defs, r, b);
                return sa != sb ? (sa ? -1 : 1) : a.CompareTo(b);
            });
            const float rowH = 92f;
            content.sizeDelta = new Vector2(0, open.Count * rowH + 10f);
            for (int n = 0; n < open.Count; n++)
            {
                int station = open[n];
                bool suits = Rabbits.Suits(defs, r, station), here = r.Station == station;
                Rabbit there = Rabbits.AtStation(s, station);
                var row = UiKit.Rect("Station " + defs[station].Id, content).Stretch(0, 1, 1, 1, 30, -(n * rowH + rowH - 6f), 30, n * rowH);
                Image rowImage = row.AddImage(_catalog.Panel, here ? Palette.Mix(Palette.Moss, Palette.Paper, 0.45f) : (suits ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.55f) : Palette.Card), true);
                rowImage.raycastTarget = true;
                if (!here)
                {
                    var rowButton = row.gameObject.AddComponent<Button>();
                    rowButton.transition = Selectable.Transition.None;
                    rowButton.targetGraphic = rowImage;
                    rowButton.onClick.AddListener(() => _onAssign(id, station));
                }
                if (_catalog.Stations[station].Prop != null)
                    UiKit.Rect("Icon", row).Place(0f, 0.5f, 52, 0, 64, 64).AddImage(_catalog.Stations[station].Prop, Color.white).preserveAspect = true;
                Text rowName = UiKit.Rect("Name", row).Stretch(0, 0, 0.56f, 1, 100, 0, 0, 0)
                    .AddText(L10n.Station(defs[station].Id), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
                rowName.resizeTextForBestFit = true;
                rowName.resizeTextMinSize = 16;
                rowName.resizeTextMaxSize = 26;
                string note = here ? L10n.T("detail.here")
                    : (suits ? L10n.T("detail.suits", UiKit.Times(Rabbits.TradeBonus)) + (there != null ? " · " + there.Name : "")
                             : (there != null ? L10n.T("detail.taken", there.Name) : L10n.T("detail.free")));
                Text rowNote = UiKit.Rect("Note", row).Stretch(0.56f, 0, 1, 1, 8, 0, 20, 0)
                    .AddText(note, UiKit.TextSmall, suits || here ? Palette.Ink : RabbitGridView.Quiet, TextAnchor.MiddleRight, suits ? FontStyle.Bold : FontStyle.Normal);
                rowNote.resizeTextForBestFit = true;
                rowNote.resizeTextMinSize = 14;
                rowNote.resizeTextMaxSize = 22;
            }
        }

        void AddSlot(GameState s, Rabbit r, ItemSlot slot, float x)
        {
            long id = r.Id;
            Item worn = Items.Worn(s, r.Id, slot);
            var rt = UiKit.Rect(slot.ToString(), _body).Place(0f, 1f, x, -394, 92, 92);
            Image img = rt.AddImage(_catalog.Round, worn != null ? Palette.Mix(Palette.Tiers[worn.Tier], Palette.Paper, 0.25f) : Palette.Track);
            img.raycastTarget = true;
            var b = rt.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.targetGraphic = img;
            b.onClick.AddListener(() => _onSlot(id, slot));
            Sprite icon = worn != null ? _catalog.ItemIcon(worn.DefId) : null;
            if (icon != null) UiKit.Rect("Icon", rt).Stretch(0, 0, 1, 1, 13, 13, 13, 13).AddImage(icon, Color.white).preserveAspect = true;
            else UiKit.Rect("Plus", rt).Stretch(0, 0, 1, 1).AddText("+", 46, RabbitGridView.Quiet, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Label", rt).Place(0.5f, 0f, 0, -15, 150, 26).AddText(ItemPickerView.SlotName(slot), UiKit.TextSmall, RabbitGridView.Quiet, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        void BuildKind(IReadOnlyList<StationDef> defs, RabbitKind kind)
        {
            var tile = UiKit.Rect("Tile", _body).Place(0.5f, 1f, 0, -330, 440, 440);
            tile.AddImage(_catalog.Panel, Palette.Tiers[kind.Tier], true);
            UiKit.Rect("Back", tile).Stretch(0, 0, 1, 1, 12, 12, 12, 12).AddImage(_catalog.Panel, Palette.Mix(Palette.Paper, Color.white, 0.35f), true);
            UiKit.Rect("Art", tile).Stretch(0, 0, 1, 1, 26, 22, 26, 22).AddImage(_catalog.KindSprite(kind.Id), Color.white).preserveAspect = true;
            UiKit.Stars(tile, kind.Tier, 0.5f, 0f, 0, 30, 24);

            Text name = UiKit.Rect("Name", _body).Stretch(0, 1, 1, 1, 60, -650, 60, 580)
                .AddText(L10n.T("kind." + kind.Id), 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.resizeTextForBestFit = true;
            name.resizeTextMinSize = 28;
            name.resizeTextMaxSize = 48;
            var plate = UiKit.Rect("Tier", _body).Place(0.5f, 1f, 0, -700, 360, 50);
            plate.AddImage(_catalog.Panel, Palette.Tiers[kind.Tier], true);
            UiKit.Rect("Text", plate).Stretch(0, 0, 1, 1).AddText(L10n.T("album.kind.tier", L10n.Rarity(kind.Tier), UiKit.Times(Rabbits.Multipliers[kind.Tier])), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var tradeChip = UiKit.Rect("Trade", _body).Place(0.5f, 1f, 0, -776, 360, 50);
            tradeChip.AddImage(_catalog.Panel, Palette.Lantern, true);
            Image tradeIcon = UiKit.Rect("Icon", tradeChip).Place(0f, 0.5f, 34, 0, 32, 32).AddImage(_catalog.TradeIcon(kind.Trade), Palette.Ink);
            tradeIcon.preserveAspect = true;
            UiKit.Rect("Text", tradeChip).Stretch(0, 0, 1, 1, 60, 0, 12, 0).AddText(L10n.Trade(kind.Trade), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);

            // Its short description: where a rabbit of this kind earns its bonus.
            var names = new StringBuilder();
            for (int i = 0; i < defs.Count; i++)
                if (defs[i].Trade == kind.Trade) names.Append(names.Length > 0 ? ", " : "").Append(L10n.Station(defs[i].Id));
            Text about = UiKit.Rect("About", _body).Stretch(0, 1, 1, 1, 70, -1000, 70, 830)
                .AddText(L10n.T("album.kind.about", UiKit.Times(Rabbits.TradeBonus), names.ToString()), UiKit.TextBody, RabbitGridView.Quiet, TextAnchor.UpperCenter);
            about.horizontalOverflow = HorizontalWrapMode.Wrap;
        }
    }
}

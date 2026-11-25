using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;
using VuonTra.Game.Bunny;

namespace VuonTra.Game.UI
{
    /// <summary>What a station's place in the garden is at the moment.</summary>
    public enum StationLook
    {
        /// <summary>Land the garden has not grown into yet: a sign under the mist.</summary>
        Land,
        /// <summary>A plot the player owns with nothing built on it: bare ground, a signpost, a shadow of the prop.</summary>
        Unbuilt,
        /// <summary>A built station, working whether or not a rabbit stands at it.</summary>
        Open
    }

    /// <summary>
    /// One station in the garden: its prop, a cycle bar and a level chip, and the rabbit that works there.
    /// A rabbit is drawn only when one of the player's own rabbits stands here; otherwise its place is an
    /// empty pad that asks for one.
    /// </summary>
    public sealed class StationActorView
    {
        public RectTransform Root { get; private set; }
        /// <summary>
        /// The station's front (#82): its level plate, the empty pad's "Xếp thỏ +" button and the up arrow, on an object of
        /// their own beside <see cref="Root"/>. The garden draws every front after everything that stands, so a decoration
        /// on the next tile never covers them, wherever the arranging mode puts things. It follows the root's place, size and
        /// visibility (<see cref="SyncFront"/>).
        /// </summary>
        public RectTransform Front { get; private set; }
        /// <summary>The level plate, which is also the price sign before the station is built.</summary>
        public RectTransform Plate => _chipRect;

        readonly int _index;
        RectTransform _rabbit, _fill, _prop, _pad, _chipRect;
        Image _rabbitImage, _propImage, _chip, _groundImage;
        Text _label;
        GameObject _bar, _glow, _shadow, _ground, _floor;
        /// <summary>The painted ground's centre and box, under the prop and the pad together.</summary>
        public static readonly Vector2 FloorAt = new Vector2(20f, -128f), FloorSize = new Vector2(460f, 230f);
        /// <summary>The contact shadow's strongest opacity (skill: ink at 25–30%).</summary>
        public const float ShadowAlpha = 0.3f;
        /// <summary>The floors are painted in daylight; under the moon they take on the night, like the scenery around them.</summary>
        public static readonly Color FloorTint = Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.38f), 0.92f);
        float _shownProgress, _propPop, _bump, _flash, _yawn;
        RectTransform _glowRect;

        /// <summary>
        /// The size this station is drawn at when nothing is moving. Every pop is worked out from this fixed
        /// number, never from the size the station happens to be at the moment, so fast taps cannot add up.
        /// </summary>
        public float BaseScale = 1f;
        StationLook _look = StationLook.Open;
        Vector2 _rabbitHome, _chipHome;
        GameObject _upArrow, _dozeBubble, _halo;
        /// <summary>What a station of rank 1, 2 and 3 wears (#49): bunting, two stone lanterns, a breathing halo.</summary>
        GameObject _bunting, _rankLanterns, _rankHalo;
        RectTransform _rankHaloRect;
        int _rank;
        /// <summary>The paper-doll rabbit standing here (owner's review 23 Nov), drawn by the bunny stage into this station's slot.</summary>
        BunnyFigure _figure;
        RawImage _figureImage;
        RabbitTrade _trade;
        bool _guest, _haloWanted, _signBob;
        int _level = -1;
        float _shownLevel, _levelRun;
        RectTransform _upArrowRect;

        /// <summary>The rabbit has nodded off at its post; a tap wakes it.</summary>
        public bool Dozing { get; private set; }

        public StationActorView(int index)
        {
            _index = index;
        }

        /// <param name="onTap">The station itself was touched: it works faster.</param>
        /// <param name="onPad">The empty pad was touched: the player wants to put a rabbit here.</param>
        /// <param name="onStroke">A finger was dragged across the rabbit (#19): once per stroke. With it, a touch on the rabbit is still a tap on the station.</param>
        /// <param name="scroll">The garden's scroll, so a stroke still drags the garden along.</param>
        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onTap, Action<int> onPad, Action<int> onStroke = null, ScrollRect scroll = null)
        {
            Root = UiKit.Rect("Station " + data.Id, parent);
            Front = UiKit.Rect("Station " + data.Id + " Front", parent);

            Image hit = Root.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onTap(_index));

            var glow = UiKit.Rect("Glow", Root).Place(0.5f, 0.5f, 10, -20, 560, 470);
            glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            _glow = glow.gameObject;
            _glowRect = glow;

            // A working station stands on its own painted ground, so it reads apart from the grass.
            Sprite floorArt = catalog.StationPad(_index);
            var floor = UiKit.Rect("Floor", Root).Place(0.5f, 0.5f, FloorAt.x, FloorAt.y, FloorSize.x, FloorSize.y);
            if (floorArt != null) floor.AddImage(floorArt, FloorTint).preserveAspect = true;
            _floor = floor.gameObject;

            // A soft contact shadow under the prop and the rabbit: ink, blurred at the edge.
            var shadow = UiKit.Rect("Shadow", Root).Place(0.5f, 0.5f, 10, -110, 380, 84);
            shadow.AddImage(catalog.Glow, Palette.Alpha(Palette.Ink, ShadowAlpha));
            _shadow = shadow.gameObject;

            // Bare earth with a signpost, under everything, for a place with nothing built yet.
            var ground = UiKit.Rect("Ground", Root).Place(0.5f, 0.5f, 16, -156, 380, 240);
            ground.pivot = new Vector2(0.5f, 0f);
            _groundImage = ground.AddImage(catalog.PlotSign, Color.white);
            _groundImage.preserveAspect = true;
            _ground = ground.gameObject;
            _ground.SetActive(false);

            // Rank 3: a warm halo behind the prop; it sits right after the station's own glow, under everything else.
            var rankHalo = UiKit.Rect("RankHalo", Root).Place(0.5f, 0.5f, 112, -30, 320, 320);
            rankHalo.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.6f));
            rankHalo.SetSiblingIndex(_glowRect.GetSiblingIndex() + 1);
            _rankHaloRect = rankHalo;
            _rankHalo = rankHalo.gameObject;
            _rankHalo.SetActive(false);

            _prop = UiKit.Rect("Prop", Root).Place(0.5f, 0.5f, 112, -122, 168, 168);
            _prop.pivot = new Vector2(0.5f, 0f);
            _propImage = _prop.AddImage(data.Prop, Color.white);
            _propImage.preserveAspect = true;

            // Rank 2: two small stone lanterns either side of the prop.
            var lanterns = UiKit.Rect("RankLanterns", Root).Place(0.5f, 0.5f, 112, -122, 0, 0);
            foreach (float lx in new[] { -98f, 98f })
            {
                var lantern = UiKit.Rect("Lantern", lanterns).Place(0.5f, 0.5f, lx, 0, 44, 64);
                lantern.pivot = new Vector2(0.5f, 0f);
                Image lanternImage = lantern.AddImage(catalog.StoneLantern != null ? catalog.StoneLantern : catalog.Round, Color.white);
                lanternImage.preserveAspect = true;
            }
            _rankLanterns = lanterns.gameObject;
            _rankLanterns.SetActive(false);

            // Rank 1: a string of five little lights hung above the prop, sagging in the middle.
            var bunting = UiKit.Rect("Bunting", Root).Place(0.5f, 0.5f, 112, 66, 230, 40);
            Color[] bulbs = { Palette.Lantern, Palette.Blush, Palette.Paper, Palette.Moss, Palette.Lantern };
            for (int k = 0; k < bulbs.Length; k++)
            {
                float bx = -92f + k * 46f, by = k == 0 || k == 4 ? 10f : (k == 2 ? -10f : -2f);
                UiKit.Rect("Bulb " + k, bunting).Place(0.5f, 0.5f, bx, by, 36, 36).AddImage(catalog.Glow, Palette.Alpha(bulbs[k], 0.7f));
                UiKit.Rect("Core " + k, bunting).Place(0.5f, 0.5f, bx, by, 14, 14).AddImage(catalog.Round, bulbs[k]);
            }
            _bunting = bunting.gameObject;
            _bunting.SetActive(false);

            // A warm light behind a gold or red rabbit, so a dark coat does not sink into the night grass.
            var halo = UiKit.Rect("Halo", Root).Place(0.5f, 0.5f, -76, 16, 330, 330);
            halo.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.8f));
            _halo = halo.gameObject;
            _halo.SetActive(false);

            // Where a rabbit stands when nobody does: a cushion with a dotted edge and a plus, which is a button.
            _pad = UiKit.Rect("Pad", Front).Place(0.5f, 0.5f, -72, -134, 250, 156);
            _pad.pivot = new Vector2(0.5f, 0f);
            Image padImage = _pad.AddImage(catalog.Pad, Color.white);
            padImage.preserveAspect = true;
            padImage.raycastTarget = true;
            var padButton = _pad.gameObject.AddComponent<Button>();
            padButton.transition = Selectable.Transition.None;
            padButton.targetGraphic = padImage;
            padButton.onClick.AddListener(() => onPad(_index));
            var tag = UiKit.Rect("Tag", _pad).Place(0.5f, 1f, 0, -14, 150 * UiKit.TextScale, 42 * UiKit.TextScale);
            tag.AddImage(catalog.Panel, Palette.Paper, true);
            Text tagText = UiKit.Rect("Text", tag).Stretch(0, 0, 1, 1, 6, 0, 6, 0).AddText(L10n.T("pad.hint") + " +", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            tagText.resizeTextForBestFit = true; // one line on the small plate in either language
            tagText.resizeTextMinSize = 16;
            tagText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _pad.gameObject.SetActive(false);

            // The player's rabbit, when one works here. Dressed rabbits are taller than wide (ears, hats).
            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, GuestBox, GuestBox);
            _rabbit.pivot = new Vector2(0.5f, 0f); // squash from the feet
            _rabbitHome = _rabbit.anchoredPosition;
            // Touch and stroke land on the rabbit's box; what is seen is its figure, a slot of the bunny stage's picture.
            // The figure's 512 canvas holds the 411-pixel-high rabbit with 12 pixels under its feet, so it is drawn
            // that much larger than the box and a little lower, to stand where the flat picture stood.
            _rabbitImage = _rabbit.AddImage(null, new Color(1f, 1f, 1f, 0f));
            _rabbitImage.raycastTarget = true;
            if (onStroke != null) UiKit.Strokable(_rabbit, scroll, () => onStroke(_index), () => onTap(_index));
            _rabbit.gameObject.SetActive(false);
            float canvas = GuestBox * 512f / 411f;
            var figureRect = UiKit.Rect("Figure", _rabbit).Place(0.5f, 0f, 0f, canvas / 2f - GuestBox * 12f / 411f, canvas, canvas);
            _figureImage = figureRect.gameObject.AddComponent<RawImage>();
            _figureImage.raycastTarget = false;
            BunnyStage stage = BunnyStage.Shared(catalog.BunnyPrefab);
            _figure = stage != null ? stage.Figure(_index) : null;
            if (stage != null) { _figureImage.texture = stage.Texture; _figureImage.uvRect = BunnyStage.SlotRect(_index); }
            _rabbit.gameObject.AddComponent<FigureLink>().Figure = _figure;

            // Shown only while the station can be afforded: the one hint on the scene that says "tap me".
            _upArrowRect = UiKit.Rect("CanUp", Front).Place(0.5f, 0.5f, 150, -118, 56, 56);
            _upArrowRect.AddImage(catalog.IconUp, Color.white).preserveAspect = true;
            _upArrow = _upArrowRect.gameObject;
            _upArrow.SetActive(false);

            var doze = UiKit.Rect("Doze", Root).Place(0.5f, 0.5f, -150, 120, 110, 54);
            doze.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", doze).Stretch(0, 0, 1, 1).AddText("z z z", 26, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dozeBubble = doze.gameObject;
            _dozeBubble.SetActive(false);

            // Level and cycle progress share one plate, centred under the station. Before the station is
            // built the same plate is the price sign.
            _chipRect = UiKit.Rect("Level", Front).Place(0.5f, 0.5f, 16, -152, 236, 58);
            _chipHome = _chipRect.anchoredPosition;
            _chip = _chipRect.AddImage(catalog.Panel, Palette.Paper, true);
            var barBg = UiKit.Rect("Bar", _chipRect).Band(18, 18, 9, 10);
            barBg.AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.28f), true);
            _bar = barBg.gameObject;
            _fill = UiKit.Rect("Fill", barBg).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Carrot, true);
            _label = UiKit.Rect("Text", _chipRect).Stretch(0, 0, 1, 1, 0, 14, 0, 0)
                .AddText("", 27, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            // "Widen the garden" is longer than "Mở rộng vườn": the plate's words shrink to fit on one line.
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 16;
            _label.resizeTextMaxSize = 27;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
        }

        /// <summary>Puts the front where the root is, at its size and scale, shown when the root is.</summary>
        public void SyncFront()
        {
            if (Front == null) return;
            bool on = Root.gameObject.activeSelf;
            if (Front.gameObject.activeSelf != on) Front.gameObject.SetActive(on);
            Front.anchorMin = Root.anchorMin;
            Front.anchorMax = Root.anchorMax;
            Front.pivot = Root.pivot;
            Front.sizeDelta = Root.sizeDelta;
            Front.anchoredPosition = Root.anchoredPosition;
            Front.localScale = Root.localScale;
        }

        /// <summary>Where the level plate rests and how large it reads; the sign's bob starts from here.</summary>
        public void PlacePlate(float scale, Vector2 shift)
        {
            _chipRect.localScale = Vector3.one * scale;
            _chipHome += shift;
            _chipRect.anchoredPosition = _chipHome;
        }

        /// <summary>Switches the station between land, an unbuilt plot and a working station.</summary>
        public void SetLook(StationLook look, int level, bool isBrew)
        {
            StationLook was = _look;
            _look = look;
            bool open = look == StationLook.Open;
            _ground.SetActive(!open);
            _groundImage.color = look == StationLook.Land ? Palette.Alpha(Color.white, 0.62f) : Color.white;
            _shadow.SetActive(open);
            _floor.SetActive(open);
            _prop.gameObject.SetActive(look != StationLook.Land);
            // On bare earth the prop that will stand here is a pale ghost, not a dark shadow that sinks into the ground.
            _propImage.color = open ? Color.white : Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Ink, 0.25f), 0.55f);
            _glow.SetActive(open);
            _bar.SetActive(open && isBrew);
            if (open)
            {
                // The number runs up to a new level over a fixed time; the first showing, and a drop, just land.
                if (_level < 0 || level < _level || was != look)
                {
                    _shownLevel = level;
                    _label.text = L10n.StationLevel(level);
                }
                else if (level != _level) _levelRun = (level - _shownLevel) / Motion.Time(Motion.Panel);
                _level = level;
                _label.color = Palette.Ink;
                _chip.color = ChipColor();
                _signBob = false;
            }
            else _level = -1;
            ShowWho();
        }

        /// <summary>
        /// What the plate says while nothing is built here: the price, or what is missing. Lit, it looks
        /// like the button it stands for and bobs to be noticed.
        /// </summary>
        public void SetSign(string text, bool lit)
        {
            if (_look == StationLook.Open) return;
            _label.text = text;
            _label.color = ChunkyButton.InkFor(lit);
            _chip.color = lit ? Palette.Carrot : Palette.Track;
            _signBob = lit;
            if (!lit) _chipRect.anchoredPosition = _chipHome;
        }

        /// <summary>The box a collected rabbit is drawn in.</summary>
        const float GuestBox = 256f;

        /// <summary>
        /// Puts one of the player's rabbits at this station as its paper-doll figure (its kind's layers, hat, charm,
        /// coat and the station's trade), or with <paramref name="worker"/> null leaves the place empty, which shows as the pad.
        /// </summary>
        public void SetWorker(Rabbit worker, GameState s, GameCatalog catalog, bool halo, Sprite tool = null, RabbitTrade trade = RabbitTrade.None)
        {
            _trade = trade;
            _guest = worker != null;
            _haloWanted = halo;
            if (_guest && _figure != null)
            {
                RabbitKind kind = RabbitKinds.Find(Rabbits.KindOf(worker));
                _figure.SetKind(kind.Id);
                Item hat = Items.Worn(s, worker.Id, ItemSlot.Hat), charm = Items.Worn(s, worker.Id, ItemSlot.Charm);
                _figure.SetHat(hat != null && Items.CanWear(worker, ItemSlot.Hat) ? hat.DefId : null, hat != null && catalog.HatSeatOf(hat.DefId).EarsOver);
                _figure.SetCharm(charm != null && Items.CanWear(worker, ItemSlot.Charm) ? charm.DefId : null);
                bool dyed = worker.Dye >= 0;
                _figure.SetFur(RabbitLook.Fur(worker), dyed ? catalog.DyeFurSprite : null);
                _figure.SetJob(trade);
                _figure.SetDozing(Dozing);
            }
            ShowWho();
        }

        /// <summary>The figure standing at this station, or null without a bunny stage.</summary>
        public BunnyFigure Figure => _figure;

        /// <summary>The station's visible rank (#49): 0 plain, 1 bunting, 2 stone lanterns and a golden plate, 3 a halo too. Only an open station shows any.</summary>
        public void SetRank(int rank)
        {
            _rank = rank;
            bool open = _look == StationLook.Open;
            if (_bunting.activeSelf != (open && rank >= 1)) _bunting.SetActive(open && rank >= 1);
            if (_rankLanterns.activeSelf != (open && rank >= 2)) _rankLanterns.SetActive(open && rank >= 2);
            if (_rankHalo.activeSelf != (open && rank >= 3)) _rankHalo.SetActive(open && rank >= 3);
            if (open) _chip.color = ChipColor();
        }

        public int Rank => _rank;

        /// <summary>The level plate: paper, or paper warmed with gold from rank 2.</summary>
        Color ChipColor() => _rank >= 2 ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : Palette.Paper;

        /// <summary>True while one of the player's rabbits stands here.</summary>
        public bool HasGuest => _guest;
        /// <summary>Where the rabbit stands, from the station's own point.</summary>
        public Vector2 RabbitHome => _rabbitHome;
        /// <summary>While the rabbit is still walking over, the station shows nobody and no pad.</summary>
        public void SetAway(bool away)
        {
            if (_away == away) return;
            _away = away;
            ShowWho();
        }
        bool _away;
        /// <summary>The empty pad, for things that point at it.</summary>
        public RectTransform Pad => _pad;

        /// <summary>The rabbit, or the pad in its place: only one of them, and neither off an open station.</summary>
        void ShowWho()
        {
            bool open = _look == StationLook.Open;
            bool rabbit = open && _guest && !_away, pad = open && !_guest, lit = rabbit && _haloWanted;
            if (_rabbit.gameObject.activeSelf != rabbit) _rabbit.gameObject.SetActive(rabbit);
            if (_pad.gameObject.activeSelf != pad) _pad.gameObject.SetActive(pad);
            if (_halo.activeSelf != lit) _halo.SetActive(lit);
            if (!rabbit) SetDozing(false);
        }

        static void Show(Image slot, Sprite sprite)
        {
            if (slot.gameObject.activeSelf != (sprite != null)) slot.gameObject.SetActive(sprite != null);
            if (sprite != null) slot.sprite = sprite;
        }

        /// <summary>True while the "tap me" arrow is up: this station can be raised right now.</summary>
        public bool CanUpgradeShown => _upArrow.activeSelf;

        public void SetCanUpgrade(bool can)
        {
            if (_upArrow.activeSelf != can) _upArrow.SetActive(can);
        }

        /// <summary>Only a rabbit can nod off: an empty station never sleeps. Nodding off starts with a yawn.</summary>
        public void SetDozing(bool dozing)
        {
            bool was = Dozing;
            Dozing = dozing && _look == StationLook.Open && _guest;
            if (Dozing && !was) _yawn = 1f;
            if (_dozeBubble.activeSelf != Dozing) _dozeBubble.SetActive(Dozing);
            if (_figure != null && was != Dozing) _figure.SetDozing(Dozing);
        }

        /// <summary>True while the rabbit is still stretching into its nap; the stretch is skipped under reduced motion.</summary>
        public bool Yawning => _yawn > 0f;

        /// <summary>The station was upgraded: the whole of it swells for a moment and settles back.</summary>
        public void Bump()
        {
            _bump = 1f; // a new bump simply restarts the one in progress
        }

        /// <summary>The light behind the station blooms for a moment: an upgrade just landed.</summary>
        public void Flash()
        {
            _flash = 1f;
        }

        /// <summary>A tap landed: the rabbit squashes (its Squish clip).</summary>
        public void Squish()
        {
            if (_figure != null && _guest) _figure.Squish();
        }

        /// <summary>An ingredient arrived: the prop gives a little pop.</summary>
        public void PopProp()
        {
            _propPop = 1f;
        }

        /// <summary>A cycle finished: the prop pops and the rabbit jumps for joy.</summary>
        public void Hop()
        {
            _propPop = 1f;
            if (_figure != null && _guest) _figure.Hop();
        }

        /// <summary>Per-frame: cycle bar, idle breathing, tap squish, payout hop.</summary>
        /// <param name="boosting">A speed burst is running: everyone works at double tempo.</param>
        public void Animate(double progress, float time, float deltaTime, bool boosting = false)
        {
            bool open = _look == StationLook.Open;
            if (open)
            {
                float target = Mathf.Clamp01((float)progress);
                // Ease towards the real value; a finished cycle snaps back to the start.
                _shownProgress = target < _shownProgress ? target : Mathf.Lerp(_shownProgress, target, Mathf.Clamp01(deltaTime * 14f));
                _fill.anchorMax = new Vector2(Mathf.Max(0.05f, _shownProgress), 1f);
            }

            _bump = Mathf.Max(0f, _bump - deltaTime * 5f); // 0.2 s
            Root.localScale = Vector3.one * (BaseScale * (1f + 0.14f * Ease.Pop(1f - _bump) * (_bump > 0f ? 1f : 0f)));
            _flash = Mathf.Max(0f, _flash - deltaTime * 3f); // a third of a second
            _glowRect.localScale = Vector3.one * (1f + 0.45f * Ease.Pop(1f - _flash) * (_flash > 0f ? 1f : 0f));
            _propPop = Mathf.Max(0f, _propPop - deltaTime * 4f);
            _yawn = Mathf.Max(0f, _yawn - deltaTime / Motion.Time(Motion.Yawn));
            // The rabbit itself moves only in its animation clips: here it is told how fast they play
            // (twice in a speed burst, held still under reduced motion).
            if (_figure != null && _rabbit.gameObject.activeSelf) _figure.SetTempo(Motion.Reduced ? 0f : (boosting ? 2f : 1f));
            if (open && _shownLevel < _level)
            {
                _shownLevel = Mathf.Min(_level, _shownLevel + _levelRun * deltaTime);
                _label.text = L10n.StationLevel(Mathf.FloorToInt(_shownLevel));
            }
            // The rank-3 halo breathes slowly, between fixed sizes; still under reduced motion.
            if (_rankHalo.activeSelf) _rankHaloRect.localScale = Vector3.one * (Motion.Reduced ? 1f : 1f + 0.06f * Mathf.Sin(time * 1.1f + _index));
            // The empty pad breathes a little, so it reads as something waiting rather than part of the ground.
            if (_pad.gameObject.activeSelf) _pad.localScale = Vector3.one * (1f + 0.03f * Mathf.Sin(time * 2.2f + _index));
            // A price sign that can be paid bobs on its spot; everything else about it stays put.
            if (_signBob) _chipRect.anchoredPosition = _chipHome + new Vector2(0f, Mathf.Abs(Mathf.Sin(time * 3.4f + _index)) * 10f);
            if (_upArrow.activeSelf) _upArrowRect.anchoredPosition = new Vector2(150, -118 + Mathf.Abs(Mathf.Sin(time * 4f + _index)) * 16f); // bounces on the end of the plate
            float hop = Ease.Pop(1f - _propPop) * (_propPop > 0f ? 1f : 0f);
            _prop.localScale = new Vector3(1f - hop * 0.08f, 1f + hop * 0.14f, 1f);
        }
    }
}

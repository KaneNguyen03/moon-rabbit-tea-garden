using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

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
        CanvasGroup _plateGroup, _wordsGroup;
        bool _near;
        int _wordsState = -1;
        GameObject _bar, _glow, _shadow, _ground, _floor;
        /// <summary>The painted ground's centre and box, under the prop and the pad together.</summary>
        public static readonly Vector2 FloorAt = new Vector2(20f, -128f), FloorSize = new Vector2(460f, 230f);
        /// <summary>The contact shadow's strongest opacity (skill: ink at 25–30%).</summary>
        public const float ShadowAlpha = 0.3f;
        /// <summary>The floors are painted in daylight; under the moon they take on the night, like the scenery around them.</summary>
        public static readonly Color FloorTint = Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.38f), 0.92f);
        float _squish, _shownProgress, _propPop, _hop, _bump, _flash, _yawn;
        RectTransform _glowRect;

        /// <summary>
        /// The size this station is drawn at when nothing is moving. Every pop is worked out from this fixed
        /// number, never from the size the station happens to be at the moment, so fast taps cannot add up.
        /// </summary>
        public float BaseScale = 1f;
        StationLook _look = StationLook.Open;
        Vector2 _rabbitHome, _chipHome;
        GameObject _upArrow, _dozeBubble, _halo, _rabbitShade;
        /// <summary>What a station of rank 1, 2 and 3 wears (#49): bunting, two stone lanterns, a breathing halo.</summary>
        GameObject _bunting, _rankLanterns, _rankHalo;
        RectTransform _rankHaloRect;
        int _rank;
        Image _hatImage, _charmImage, _toolImage;
        RectTransform _tool;
        Vector2 _toolHome;
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
            tag.AddImage(catalog.Panel, Palette.Mix(Palette.Ink, Palette.Night, 0.35f), true);
            Text tagText = UiKit.Rect("Text", tag).Stretch(0, 0, 1, 1, 6, 0, 6, 0).AddText(L10n.T("pad.hint") + " +", UiKit.TextSmall, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            tagText.resizeTextForBestFit = true; // one line on the small plate in either language
            tagText.resizeTextMinSize = 16;
            tagText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _pad.gameObject.SetActive(false);

            // The player's rabbit, when one works here. Dressed rabbits are taller than wide (ears, hats).
            Image rabbitShade = UiKit.Rect("RabbitShade", Root).Place(0.5f, 0.5f, -72, -106, GuestBox * 0.6f, GuestBox * 0.16f).AddImage(catalog.Glow, Palette.Alpha(Palette.Ink, 0.3f));
            rabbitShade.raycastTarget = false;
            _rabbitShade = rabbitShade.gameObject;
            _rabbitShade.SetActive(false);
            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, GuestBox, GuestBox);
            _rabbit.pivot = new Vector2(0.5f, 0f); // squash from the feet
            _rabbitHome = _rabbit.anchoredPosition;
            _rabbitImage = _rabbit.AddImage(null, Color.white);
            _rabbitImage.preserveAspect = true;
            if (onStroke != null) UiKit.Strokable(_rabbit, scroll, () => onStroke(_index), () => onTap(_index));
            _rabbit.gameObject.SetActive(false);

            // What a collected rabbit wears. Both hang from the rabbit, so they squash and hop with it;
            // the hat sits on the top of the head, wherever the ears go.
            _hatImage = UiKit.Rect("HatOn", _rabbit).Place(0.5f, 0f, 0, 0, GuestBox * GameCatalog.HatSize, GuestBox * GameCatalog.HatSize).AddImage(null, Color.white);
            _hatImage.preserveAspect = true;
            _hatImage.gameObject.SetActive(false);
            _charmImage = UiKit.Rect("CharmOn", _rabbit).Place(0.5f, 0f, 0, 0, GuestBox * GameCatalog.CharmSize, GuestBox * GameCatalog.CharmSize).AddImage(null, Color.white);
            _charmImage.preserveAspect = true;
            _charmImage.gameObject.SetActive(false);

            // What the station's trade is worked with (#2): the trade's icon held in the near paw, moved by the trade
            // in Animate. Drawn with the rabbit, so it squashes, leans and hops along.
            _toolHome = new Vector2(GuestBox * 0.30f, GuestBox * 0.30f);
            _tool = UiKit.Rect("Tool", _rabbit).Place(0.5f, 0f, _toolHome.x, _toolHome.y, GuestBox * 0.26f, GuestBox * 0.26f);
            _tool.pivot = new Vector2(0.5f, 0.15f); // held near the handle's end, so it turns about the paw
            _toolImage = _tool.AddImage(null, Color.white);
            _toolImage.preserveAspect = true;
            _tool.gameObject.SetActive(false);

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
            _chipRect = UiKit.Rect("Level", Front).Place(0.5f, 0.5f, PlateX, -152, PlateW, PlateH);
            _chipHome = _chipRect.anchoredPosition;
            _chip = _chipRect.AddImage(catalog.Panel, Palette.Paper, true);
            _plateGroup = _chipRect.gameObject.AddComponent<CanvasGroup>();
            var barBg = UiKit.Rect("Bar", _chipRect).Band(16, 16, 7, 14);
            barBg.AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.42f), true);
            _bar = barBg.gameObject;
            _fill = UiKit.Rect("Fill", barBg).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Carrot, true);
            _label = UiKit.Rect("Text", _chipRect).Stretch(0, 0, 1, 1, 0, 19, 0, 0)
                .AddText("", 27, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            // "Widen the garden" is longer than "Mở rộng vườn": the plate's words shrink to fit on one line.
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 16;
            _label.resizeTextMaxSize = 27;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _wordsGroup = _label.gameObject.AddComponent<CanvasGroup>();
        }

        /// <summary>How far right of the station's middle its level plate stands, and how wide it is, in the station's own units.</summary>
        public const float PlateX = 16f, PlateW = 236f, PlateH = 58f, BarPlateH = 28f;

        public void ShowWords(bool near, Tweens tweens)
        {
            _near = near;
            ApplyWords(tweens);
        }

        void ApplyWords(Tweens tweens)
        {
            bool words = _near || _look != StationLook.Open, plate = words || _bar.activeSelf;
            int state = (words ? 1 : 0) | (plate ? 2 : 0);
            if (state == _wordsState) return;
            _wordsState = state;
            float height = words ? PlateH : BarPlateH;
            if (tweens == null || Motion.Reduced)
            {
                _wordsGroup.alpha = words ? 1f : 0f;
                _plateGroup.alpha = plate ? 1f : 0f;
                _chipRect.sizeDelta = new Vector2(PlateW, height);
                return;
            }
            tweens.Fade(_wordsGroup, words ? 1f : 0f);
            tweens.Fade(_plateGroup, plate ? 1f : 0f);
            float from = _chipRect.sizeDelta.y;
            tweens.Add(Motion.Quick, t => { if (_chipRect != null) _chipRect.sizeDelta = new Vector2(PlateW, from + (height - from) * t); });
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
            ApplyWords(null);
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
        /// Puts one of the player's rabbits at this station, with what it wears, or with <paramref name="art"/>
        /// null leaves the place empty, which shows as the pad.
        /// </summary>
        /// <param name="hatNudge">This kind's own nudge for the hat, in shares of the drawing's height.</param>
        /// <param name="hatLift">The hat kind's own lift (#44), in shares of the drawing's height.</param>
        /// <param name="fur">The tint of the rabbit's fur: white, or its dye (#44).</param>
        public void SetWorker(Sprite art, Sprite hat, Sprite charm, bool halo, Vector2 hatNudge = default, float hatLift = 0f, Color? fur = null, Sprite tool = null, RabbitTrade trade = RabbitTrade.None)
        {
            _trade = trade;
            Show(_toolImage, art != null ? tool : null);
            // The same numbers the rabbit list uses, so a hat sits the same on a rabbit wherever it is drawn.
            float across = (GameCatalog.KindHeadX - 0.5f + hatNudge.x) * GuestBox * GameCatalog.KindAspect;
            RabbitGridView.PlaceHat((RectTransform)_hatImage.transform, hat, GuestBox, across, (GameCatalog.KindHeadTop + hatNudge.y) * GuestBox, hatLift);
            ((RectTransform)_charmImage.transform).anchoredPosition = new Vector2(across, (GameCatalog.CharmHeight + hatNudge.y) * GuestBox);
            _guest = art != null;
            _haloWanted = halo;
            if (_guest) _rabbitImage.sprite = art;
            _rabbitImage.color = fur ?? Color.white;
            Show(_hatImage, _guest ? hat : null);
            Show(_charmImage, _guest ? charm : null);
            ShowWho();
        }

        /// <summary>
        /// The tool at work, between fixed bounds: a brewer stirs (the spoon turns about the paw), a baker kneads
        /// (the tool presses down and up), a gatherer swings the basket, a stargazer tilts the glass slowly. A
        /// dozing rabbit lets it rest, and under reduced motion it only sits in the paw.
        /// </summary>
        void MoveTool(float t)
        {
            float angle = 0f;
            Vector2 shift = Vector2.zero;
            if (!Dozing && !Motion.Reduced)
            {
                switch (_trade)
                {
                    case RabbitTrade.Brewer:
                        angle = Mathf.Sin(t * 5f + _index) * 22f;
                        shift = new Vector2(Mathf.Cos(t * 5f + _index), Mathf.Sin(t * 5f + _index)) * 4f;
                        break;
                    case RabbitTrade.Baker:
                        angle = Mathf.Sin(t * 4.5f + _index) * 6f;
                        shift = new Vector2(0f, -Mathf.Abs(Mathf.Sin(t * 4.5f + _index)) * 10f);
                        break;
                    case RabbitTrade.Gatherer:
                        angle = Mathf.Sin(t * 2.4f + _index) * 12f;
                        shift = new Vector2(0f, Mathf.Abs(Mathf.Sin(t * 2.4f + _index)) * 6f);
                        break;
                    default:
                        angle = Mathf.Sin(t * 1.1f + _index) * 8f;
                        shift = new Vector2(0f, 20f);
                        break;
                }
            }
            else if (Dozing) shift = new Vector2(0f, -12f);
            _tool.localRotation = Quaternion.Euler(0, 0, angle);
            _tool.anchoredPosition = _toolHome + shift;
        }

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
            if (_rabbitShade.activeSelf != rabbit) _rabbitShade.SetActive(rabbit);
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

        /// <summary>A tap landed: squash the rabbit.</summary>
        public void Squish()
        {
            _squish = 1f;
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
            _hop = 1f;
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
            _squish = Mathf.Max(0f, _squish - deltaTime * 5f);
            _propPop = Mathf.Max(0f, _propPop - deltaTime * 4f);
            _hop = Mathf.Max(0f, _hop - deltaTime * 2.6f);
            _yawn = Mathf.Max(0f, _yawn - deltaTime / Motion.Time(Motion.Yawn));
            float tempo = boosting ? 2f : 1f;

            if (_rabbit.gameObject.activeSelf)
            {
                float breathe = Motion.Reduced ? 0f : Mathf.Sin(time * 3f * tempo + _index * 1.7f) * 0.03f;
                // Landing squash at the end of a jump, stretch on the way up.
                float air = Motion.Reduced ? 0f : Mathf.Sin((1f - _hop) * Mathf.PI) * (_hop > 0f ? 1f : 0f);
                float land = !Motion.Reduced && _hop > 0f && _hop < 0.18f ? (0.18f - _hop) / 0.18f * 0.12f : 0f;
                // The yawn: one slow stretch up from the feet, from fixed numbers, so it never adds up.
                float yawn = Ease.Pop(1f - _yawn) * (_yawn > 0f ? 1f : 0f);
                _rabbit.localScale = new Vector3(
                    1f + _squish * 0.12f - breathe * 0.5f - air * 0.06f + land - yawn * 0.04f,
                    1f - _squish * 0.18f + breathe + air * 0.10f - land + yawn * 0.09f, 1f);
                _rabbit.anchoredPosition = _rabbitHome + new Vector2(0, air * 46f);
                // A working rabbit sways as it works; a sleeping one nods slowly.
                float lean = Dozing ? (Motion.Reduced ? 0f : Mathf.Sin(time * 0.9f + _index) * 2.2f) - 9f : Motion.Reduced ? 0f : Mathf.Sin(time * 2.1f * tempo + _index * 2.3f) * 3.2f;
                _rabbit.localRotation = Quaternion.Euler(0, 0, lean * (1f - air));
                if (_tool.gameObject.activeSelf) MoveTool(time * tempo);
            }
            if (open && _shownLevel < _level)
            {
                _shownLevel = Mathf.Min(_level, _shownLevel + _levelRun * deltaTime);
                _label.text = L10n.StationLevel(Mathf.FloorToInt(_shownLevel));
            }
            // The rank-3 halo breathes slowly, between fixed sizes; still under reduced motion.
            if (_rankHalo.activeSelf) _rankHaloRect.localScale = Vector3.one * (Motion.Reduced ? 1f : 1f + 0.06f * Mathf.Sin(time * 1.1f + _index));
            // The empty pad breathes a little, so it reads as something waiting rather than part of the ground.
            if (_pad.gameObject.activeSelf) _pad.localScale = Vector3.one * (Motion.Reduced ? 1f : 1f + 0.03f * Mathf.Sin(time * 2.2f + _index));
            // A price sign that can be paid bobs on its spot; everything else about it stays put.
            if (_signBob) _chipRect.anchoredPosition = _chipHome + (Motion.Reduced ? Vector2.zero : new Vector2(0f, Mathf.Abs(Mathf.Sin(time * 3.4f + _index)) * 10f));
            if (_upArrow.activeSelf) _upArrowRect.anchoredPosition = new Vector2(150, -118 + (Motion.Reduced ? 0f : Mathf.Abs(Mathf.Sin(time * 4f + _index)) * 16f)); // bounces on the end of the plate
            float hop = Ease.Pop(1f - _propPop) * (_propPop > 0f ? 1f : 0f);
            _prop.localScale = new Vector3(1f - hop * 0.08f, 1f + hop * 0.14f, 1f);
        }
    }
}

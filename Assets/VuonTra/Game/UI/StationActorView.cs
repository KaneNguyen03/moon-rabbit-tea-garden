using System;
using UnityEngine;
using UnityEngine.UI;

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

        readonly int _index;
        RectTransform _rabbit, _fill, _prop, _pad, _chipRect;
        Image _rabbitImage, _propImage, _chip, _groundImage;
        Text _label;
        GameObject _bar, _glow, _shadow, _ground;
        float _squish, _shownProgress, _propPop, _hop, _bump;

        /// <summary>
        /// The size this station is drawn at when nothing is moving. Every pop is worked out from this fixed
        /// number, never from the size the station happens to be at the moment, so fast taps cannot add up.
        /// </summary>
        public float BaseScale = 1f;
        StationLook _look = StationLook.Open;
        Vector2 _rabbitHome, _chipHome;
        GameObject _upArrow, _dozeBubble, _halo;
        Image _hatImage, _charmImage;
        bool _guest, _haloWanted, _signBob;
        RectTransform _upArrowRect;

        /// <summary>The rabbit has nodded off at its post; a tap wakes it.</summary>
        public bool Dozing { get; private set; }

        public StationActorView(int index)
        {
            _index = index;
        }

        /// <param name="onTap">The station itself was touched: it works faster.</param>
        /// <param name="onPad">The empty pad was touched: the player wants to put a rabbit here.</param>
        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onTap, Action<int> onPad)
        {
            Root = UiKit.Rect("Station " + data.Id, parent);

            Image hit = Root.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onTap(_index));

            var glow = UiKit.Rect("Glow", Root).Place(0.5f, 0.5f, 10, -20, 560, 470);
            glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            _glow = glow.gameObject;

            var shadow = UiKit.Rect("Shadow", Root).Place(0.5f, 0.5f, 10, -112, 330, 62);
            shadow.AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.4f));
            _shadow = shadow.gameObject;

            // Bare earth with a signpost, under everything, for a place with nothing built yet.
            var ground = UiKit.Rect("Ground", Root).Place(0.5f, 0.5f, 16, -156, 380, 240);
            ground.pivot = new Vector2(0.5f, 0f);
            _groundImage = ground.AddImage(catalog.PlotSign, Color.white);
            _groundImage.preserveAspect = true;
            _ground = ground.gameObject;
            _ground.SetActive(false);

            _prop = UiKit.Rect("Prop", Root).Place(0.5f, 0.5f, 112, -122, 168, 168);
            _prop.pivot = new Vector2(0.5f, 0f);
            _propImage = _prop.AddImage(data.Prop, Color.white);
            _propImage.preserveAspect = true;

            // A warm light behind a gold or red rabbit, so a dark coat does not sink into the night grass.
            var halo = UiKit.Rect("Halo", Root).Place(0.5f, 0.5f, -76, 16, 330, 330);
            halo.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.8f));
            _halo = halo.gameObject;
            _halo.SetActive(false);

            // Where a rabbit stands when nobody does: a cushion with a dotted edge and a plus, which is a button.
            _pad = UiKit.Rect("Pad", Root).Place(0.5f, 0.5f, -72, -134, 250, 156);
            _pad.pivot = new Vector2(0.5f, 0f);
            Image padImage = _pad.AddImage(catalog.Pad, Color.white);
            padImage.preserveAspect = true;
            padImage.raycastTarget = true;
            var padButton = _pad.gameObject.AddComponent<Button>();
            padButton.transition = Selectable.Transition.None;
            padButton.targetGraphic = padImage;
            padButton.onClick.AddListener(() => onPad(_index));
            var tag = UiKit.Rect("Tag", _pad).Place(0.5f, 1f, 0, -14, 150, 42);
            tag.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", tag).Stretch(0, 0, 1, 1).AddText(L10n.T("pad.hint") + " +", UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _pad.gameObject.SetActive(false);

            // The player's rabbit, when one works here. Dressed rabbits are taller than wide (ears, hats).
            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, GuestBox, GuestBox);
            _rabbit.pivot = new Vector2(0.5f, 0f); // squash from the feet
            _rabbitHome = _rabbit.anchoredPosition;
            _rabbitImage = _rabbit.AddImage(null, Color.white);
            _rabbitImage.preserveAspect = true;
            _rabbit.gameObject.SetActive(false);

            // What a collected rabbit wears. Both hang from the rabbit, so they squash and hop with it;
            // the hat sits on the top of the head, wherever the ears go.
            _hatImage = UiKit.Rect("HatOn", _rabbit).Place(0.5f, 0f, 0, 0, GuestBox * GameCatalog.HatSize, GuestBox * GameCatalog.HatSize).AddImage(null, Color.white);
            _hatImage.preserveAspect = true;
            _hatImage.gameObject.SetActive(false);
            _charmImage = UiKit.Rect("CharmOn", _rabbit).Place(0.5f, 0f, 0, 0, GuestBox * GameCatalog.CharmSize, GuestBox * GameCatalog.CharmSize).AddImage(null, Color.white);
            _charmImage.preserveAspect = true;
            _charmImage.gameObject.SetActive(false);

            // Shown only while the station can be afforded: the one hint on the scene that says "tap me".
            _upArrowRect = UiKit.Rect("CanUp", Root).Place(0.5f, 0.5f, 150, -118, 56, 56);
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
            _chipRect = UiKit.Rect("Level", Root).Place(0.5f, 0.5f, 16, -152, 236, 58);
            _chipHome = _chipRect.anchoredPosition;
            _chip = _chipRect.AddImage(catalog.Panel, Palette.Paper, true);
            var barBg = UiKit.Rect("Bar", _chipRect).Band(18, 18, 9, 10);
            barBg.AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.28f), true);
            _bar = barBg.gameObject;
            _fill = UiKit.Rect("Fill", barBg).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Carrot, true);
            _label = UiKit.Rect("Text", _chipRect).Stretch(0, 0, 1, 1, 0, 14, 0, 0)
                .AddText("", 27, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            _look = look;
            bool open = look == StationLook.Open;
            _ground.SetActive(!open);
            _groundImage.color = look == StationLook.Land ? Palette.Alpha(Color.white, 0.62f) : Color.white;
            _shadow.SetActive(open);
            _prop.gameObject.SetActive(look != StationLook.Land);
            // On bare earth the prop that will stand here is a pale ghost, not a dark shadow that sinks into the ground.
            _propImage.color = open ? Color.white : Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Ink, 0.25f), 0.55f);
            _glow.SetActive(open);
            _bar.SetActive(open && isBrew);
            if (open)
            {
                _label.text = L10n.T("station.level", level);
                _label.color = Palette.Ink;
                _chip.color = Palette.Paper;
                _signBob = false;
            }
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
        public void SetWorker(Sprite art, Sprite hat, Sprite charm, bool halo, Vector2 hatNudge = default)
        {
            // The same numbers the rabbit list uses, so a hat sits the same on a rabbit wherever it is drawn.
            float across = (GameCatalog.KindHeadX - 0.5f + hatNudge.x) * GuestBox * GameCatalog.KindAspect;
            ((RectTransform)_hatImage.transform).anchoredPosition = new Vector2(across, (GameCatalog.KindHeadTop + hatNudge.y + GameCatalog.HatLift) * GuestBox);
            ((RectTransform)_charmImage.transform).anchoredPosition = new Vector2(across, (GameCatalog.CharmHeight + hatNudge.y) * GuestBox);
            _guest = art != null;
            _haloWanted = halo;
            if (_guest) _rabbitImage.sprite = art;
            Show(_hatImage, _guest ? hat : null);
            Show(_charmImage, _guest ? charm : null);
            ShowWho();
        }

        /// <summary>True while one of the player's rabbits stands here.</summary>
        public bool HasGuest => _guest;

        /// <summary>The rabbit, or the pad in its place: only one of them, and neither off an open station.</summary>
        void ShowWho()
        {
            bool open = _look == StationLook.Open;
            bool rabbit = open && _guest, pad = open && !_guest, lit = rabbit && _haloWanted;
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

        public void SetCanUpgrade(bool can)
        {
            if (_upArrow.activeSelf != can) _upArrow.SetActive(can);
        }

        /// <summary>Only a rabbit can nod off: an empty station never sleeps.</summary>
        public void SetDozing(bool dozing)
        {
            Dozing = dozing && _look == StationLook.Open && _guest;
            if (_dozeBubble.activeSelf != Dozing) _dozeBubble.SetActive(Dozing);
        }

        /// <summary>The station was upgraded: the whole of it swells for a moment and settles back.</summary>
        public void Bump()
        {
            _bump = 1f; // a new bump simply restarts the one in progress
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
            _squish = Mathf.Max(0f, _squish - deltaTime * 5f);
            _propPop = Mathf.Max(0f, _propPop - deltaTime * 4f);
            _hop = Mathf.Max(0f, _hop - deltaTime * 2.6f);
            float tempo = boosting ? 2f : 1f;

            if (_rabbit.gameObject.activeSelf)
            {
                float breathe = Mathf.Sin(time * 3f * tempo + _index * 1.7f) * 0.03f;
                // Landing squash at the end of a jump, stretch on the way up.
                float air = Mathf.Sin((1f - _hop) * Mathf.PI) * (_hop > 0f ? 1f : 0f);
                float land = _hop > 0f && _hop < 0.18f ? (0.18f - _hop) / 0.18f * 0.12f : 0f;
                _rabbit.localScale = new Vector3(
                    1f + _squish * 0.12f - breathe * 0.5f - air * 0.06f + land,
                    1f - _squish * 0.18f + breathe + air * 0.10f - land, 1f);
                _rabbit.anchoredPosition = _rabbitHome + new Vector2(0, air * 46f);
                // A working rabbit sways as it works; a sleeping one nods slowly.
                float lean = Dozing ? Mathf.Sin(time * 0.9f + _index) * 2.2f - 9f : Mathf.Sin(time * 2.1f * tempo + _index * 2.3f) * 3.2f;
                _rabbit.localRotation = Quaternion.Euler(0, 0, lean * (1f - air));
            }
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

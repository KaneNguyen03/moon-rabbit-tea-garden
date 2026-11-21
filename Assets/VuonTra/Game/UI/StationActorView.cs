using System;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    /// <summary>One station in the garden: a rabbit, its prop, a cycle bar and a level chip.</summary>
    public sealed class StationActorView
    {
        public RectTransform Root { get; private set; }

        readonly int _index;
        RectTransform _rabbit, _fill, _prop;
        Image _rabbitImage, _scarfImage, _propImage, _chip;
        Text _label;
        GameObject _bar, _glow;
        float _squish, _shownProgress, _propPop, _hop;
        bool _locked;
        Vector2 _rabbitHome;
        Sprite _stillRabbit;
        Sprite[] _workFrames;
        int _frame = -1;
        GameObject _upArrow, _dozeBubble, _halo;
        Image _hatImage, _charmImage;
        bool _guest, _scarfByDefault;
        float _homeBox;
        RectTransform _upArrowRect;

        /// <summary>The rabbit has nodded off at its post; a tap wakes it.</summary>
        public bool Dozing { get; private set; }
        Color _accent;

        public StationActorView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, StationData data, Action<int> onTap)
        {
            _accent = data.Accent;
            Root = UiKit.Rect("Station " + data.Id, parent);

            Image hit = Root.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var button = Root.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(() => onTap(_index));

            var glow = UiKit.Rect("Glow", Root).Place(0.5f, 0.5f, 10, -20, 560, 470);
            glow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.55f));
            _glow = glow.gameObject;

            UiKit.Rect("Shadow", Root).Place(0.5f, 0.5f, 10, -112, 330, 62)
                .AddImage(catalog.Round, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.4f));

            _prop = UiKit.Rect("Prop", Root).Place(0.5f, 0.5f, 112, -122, 168, 168);
            _prop.pivot = new Vector2(0.5f, 0f);
            _propImage = _prop.AddImage(data.Prop, Color.white);
            _propImage.preserveAspect = true;

            // A warm light behind a gold or red rabbit, so a dark coat does not sink into the night grass.
            var halo = UiKit.Rect("Halo", Root).Place(0.5f, 0.5f, -76, 16, 330, 330);
            halo.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.8f));
            _halo = halo.gameObject;
            _halo.SetActive(false);

            bool ownRabbit = data.Rabbit != null;
            // Dressed rabbits are taller than wide (ears, hats), so they get a taller box to read at the same size.
            float box = ownRabbit ? 256 : 236;
            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, box, box);
            _rabbit.pivot = new Vector2(0.5f, 0f); // squash from the feet
            _rabbitHome = _rabbit.anchoredPosition;
            _rabbitImage = _rabbit.AddImage(ownRabbit ? data.Rabbit : catalog.Rabbit, Color.white);
            _rabbitImage.preserveAspect = true;
            _stillRabbit = _rabbitImage.sprite;
            _workFrames = data.WorkFrames;
            _scarfImage = UiKit.Rect("Scarf", _rabbit).Stretch(0, 0, 1, 1).AddImage(catalog.Scarf, data.Accent);
            _scarfImage.preserveAspect = true;
            // A dressed rabbit needs no scarf, and an Image without a sprite would draw a solid box.
            _scarfByDefault = !ownRabbit && catalog.Scarf != null;
            _scarfImage.gameObject.SetActive(_scarfByDefault);
            _homeBox = box;

            // What a collected rabbit wears. Both hang from the rabbit, so they squash and hop with it;
            // the hat sits on the top of the head, wherever the ears go.
            float across = (GameCatalog.KindHeadX - 0.5f) * GuestBox * GameCatalog.KindAspect;
            _hatImage = UiKit.Rect("HatOn", _rabbit).Place(0.5f, 0f, across, GameCatalog.KindHeadTop * GuestBox + 30f, 112, 112).AddImage(null, Color.white);
            _hatImage.preserveAspect = true;
            _hatImage.gameObject.SetActive(false);
            _charmImage = UiKit.Rect("CharmOn", _rabbit).Place(0.5f, 0f, across + 2f, GuestBox * 0.37f, 48, 48).AddImage(null, Color.white);
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

            // Level and cycle progress share one plate, centred under the station.
            var chip = UiKit.Rect("Level", Root).Place(0.5f, 0.5f, 16, -152, 236, 58);
            _chip = chip.AddImage(catalog.Panel, Palette.Paper, true);
            var barBg = UiKit.Rect("Bar", chip).Band(18, 18, 9, 10);
            barBg.AddImage(catalog.Panel, Palette.Alpha(Palette.Ink, 0.28f), true);
            _bar = barBg.gameObject;
            _fill = UiKit.Rect("Fill", barBg).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Carrot, true);
            _label = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1, 0, 14, 0, 0)
                .AddText("", 27, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Switches between the working look and the asleep look.</summary>
        /// <param name="needsPlot">The station has no plot yet: it shows as empty land.</param>
        public void SetLevel(int level, bool isBrew, bool needsPlot = false)
        {
            _locked = level <= 0;
            _label.text = needsPlot ? L10n.T("station.empty") : (_locked ? "z z z" : L10n.T("station.level", level));
            _label.color = _locked ? Palette.Mix(Palette.Paper, Palette.Night, 0.25f) : Palette.Ink;
            _chip.color = _locked ? Palette.Alpha(Palette.Mix(Palette.Night, Palette.Paper, 0.2f), 0.85f) : Palette.Paper;
            _rabbitImage.color = _locked ? Palette.Asleep : Color.white;
            _scarfImage.color = _locked ? Palette.Asleep : _accent;
            _propImage.color = _locked ? Palette.Alpha(Palette.Asleep, 0.55f) : Color.white;
            _glow.SetActive(!_locked);
            _bar.SetActive(!_locked && isBrew);
        }

        /// <summary>The box a collected rabbit is drawn in: as tall as the station's own rabbit.</summary>
        const float GuestBox = 256f;

        /// <summary>
        /// Puts a collected rabbit at this station, with what it wears, or with <paramref name="art"/> null
        /// gives the station back its own rabbit. A collected rabbit has no drawn work frames, so it bobs.
        /// </summary>
        public void SetWorker(Sprite art, Sprite hat, Sprite charm, bool halo)
        {
            bool guest = art != null;
            if (guest != _guest || (guest && _rabbitImage.sprite != art))
            {
                _guest = guest;
                _frame = -1;
                _rabbitImage.sprite = guest ? art : _stillRabbit;
                float box = guest ? GuestBox : _homeBox;
                _rabbit.sizeDelta = new Vector2(box, box);
                _scarfImage.gameObject.SetActive(!guest && _scarfByDefault);
            }
            Show(_hatImage, guest ? hat : null);
            Show(_charmImage, guest ? charm : null);
            bool lit = guest && halo && !_locked;
            if (_halo.activeSelf != lit) _halo.SetActive(lit);
        }

        /// <summary>True while a collected rabbit stands here instead of the station's own.</summary>
        public bool HasGuest => _guest;

        static void Show(Image slot, Sprite sprite)
        {
            if (slot.gameObject.activeSelf != (sprite != null)) slot.gameObject.SetActive(sprite != null);
            if (sprite != null) slot.sprite = sprite;
        }

        public void SetCanUpgrade(bool can)
        {
            if (_upArrow.activeSelf != can) _upArrow.SetActive(can);
        }

        public void SetDozing(bool dozing)
        {
            Dozing = dozing && !_locked;
            if (_dozeBubble.activeSelf != Dozing) _dozeBubble.SetActive(Dozing);
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
            if (!_locked)
            {
                float target = Mathf.Clamp01((float)progress);
                // Ease towards the real value; a finished cycle snaps back to the start.
                _shownProgress = target < _shownProgress ? target : Mathf.Lerp(_shownProgress, target, Mathf.Clamp01(deltaTime * 14f));
                _fill.anchorMax = new Vector2(Mathf.Max(0.05f, _shownProgress), 1f);
            }

            _squish = Mathf.Max(0f, _squish - deltaTime * 5f);
            _propPop = Mathf.Max(0f, _propPop - deltaTime * 4f);
            _hop = Mathf.Max(0f, _hop - deltaTime * 2.6f);
            float tempo = boosting ? 2f : 1f;

            // A rabbit with drawn work frames plays them while its station is open; asleep it holds still.
            if (!_guest && _workFrames != null && _workFrames.Length > 0)
            {
                int frame = _locked || Dozing ? -1 : (int)(time * 5f * tempo + _index) % _workFrames.Length;
                if (frame != _frame)
                {
                    _frame = frame;
                    _rabbitImage.sprite = frame < 0 ? _stillRabbit : _workFrames[frame];
                }
            }
            float breathe = _locked ? Mathf.Sin(time * 1.2f + _index) * 0.012f : Mathf.Sin(time * 3f * tempo + _index * 1.7f) * 0.03f;
            // Landing squash at the end of a jump, stretch on the way up.
            float air = Mathf.Sin((1f - _hop) * Mathf.PI) * (_hop > 0f ? 1f : 0f);
            float land = _hop > 0f && _hop < 0.18f ? (0.18f - _hop) / 0.18f * 0.12f : 0f;
            _rabbit.localScale = new Vector3(
                1f + _squish * 0.12f - breathe * 0.5f - air * 0.06f + land,
                1f - _squish * 0.18f + breathe + air * 0.10f - land, 1f);
            _rabbit.anchoredPosition = _rabbitHome + new Vector2(0, air * 46f);
            // A working rabbit sways as it works; a sleeping one nods slowly.
            float lean = _locked || Dozing ? Mathf.Sin(time * 0.9f + _index) * 2.2f - (Dozing ? 9f : 4f) : Mathf.Sin(time * 2.1f * tempo + _index * 2.3f) * 3.2f;
            if (_upArrow.activeSelf) _upArrowRect.anchoredPosition = new Vector2(150, -118 + Mathf.Abs(Mathf.Sin(time * 4f + _index)) * 16f); // bounces on the end of the plate
            _rabbit.localRotation = Quaternion.Euler(0, 0, lean * (1f - air));
            float hop = Ease.Pop(1f - _propPop) * (_propPop > 0f ? 1f : 0f);
            _prop.localScale = new Vector3(1f - hop * 0.08f, 1f + hop * 0.14f, 1f);
        }
    }
}

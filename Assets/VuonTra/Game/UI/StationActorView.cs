using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public enum StationLook
    {
        Land,
        Unbuilt,
        Open
    }

    public sealed class StationActorView
    {
        public RectTransform Root { get; private set; }
        public RectTransform Front { get; private set; }
        public RectTransform Plate => _chipRect;

        readonly int _index;
        RectTransform _rabbit, _fill, _prop, _pad, _padTag, _chipRect;
        Text _padText;
        Image _rabbitImage, _propImage, _chip, _groundImage;
        Text _label;
        CanvasGroup _plateGroup, _wordsGroup;
        bool _near;
        int _wordsState = -1;
        GameObject _bar, _glow, _shadow, _ground, _floor;
        public static readonly Vector2 FloorAt = new Vector2(20f, -128f), FloorSize = new Vector2(460f, 230f);
        public const float ShadowAlpha = 0.3f;
        public static readonly Color FloorTint = Palette.Alpha(Palette.Mix(Color.white, Palette.Night, 0.38f), 0.92f);
        float _squish, _shownProgress, _propPop, _hop, _bump, _flash, _yawn;
        RectTransform _glowRect;

        public float BaseScale = 1f;
        StationLook _look = StationLook.Open;
        Vector2 _rabbitHome, _chipHome;
        GameObject _upArrow, _dozeBubble, _halo, _rabbitShade;
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

        public bool Dozing { get; private set; }

        public StationActorView(int index)
        {
            _index = index;
        }

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

            Sprite floorArt = catalog.StationPad(_index);
            var floor = UiKit.Rect("Floor", Root).Place(0.5f, 0.5f, FloorAt.x, FloorAt.y, FloorSize.x, FloorSize.y);
            if (floorArt != null) floor.AddImage(floorArt, FloorTint).preserveAspect = true;
            _floor = floor.gameObject;

            var shadow = UiKit.Rect("Shadow", Root).Place(0.5f, 0.5f, 10, -110, 380, 84);
            shadow.AddImage(catalog.Glow, Palette.Alpha(Palette.Ink, ShadowAlpha));
            _shadow = shadow.gameObject;

            var ground = UiKit.Rect("Ground", Root).Place(0.5f, 0.5f, 16, -156, 380, 240);
            ground.pivot = new Vector2(0.5f, 0f);
            _groundImage = ground.AddImage(catalog.PlotSign, Color.white);
            _groundImage.preserveAspect = true;
            _ground = ground.gameObject;
            _ground.SetActive(false);

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

            var halo = UiKit.Rect("Halo", Root).Place(0.5f, 0.5f, -76, 16, 330, 330);
            halo.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.8f));
            _halo = halo.gameObject;
            _halo.SetActive(false);

            _pad = UiKit.Rect("Pad", Front).Place(0.5f, 0.5f, -72, -134, PadW, 156);
            _pad.pivot = new Vector2(0.5f, 0f);
            Image padImage = _pad.AddImage(catalog.Pad, Color.white);
            padImage.preserveAspect = true;
            padImage.raycastTarget = true;
            var padButton = _pad.gameObject.AddComponent<Button>();
            padButton.transition = Selectable.Transition.None;
            padButton.targetGraphic = padImage;
            padButton.onClick.AddListener(() => onPad(_index));
            _padTag = UiKit.Rect("Tag", _pad).Place(0.5f, 0.5f, PadTagX, PadTagY, PadTagMinW, PadTagPad);
            _padTag.AddImage(catalog.Panel, Palette.Mix(Palette.Ink, Palette.Night, 0.35f), true);
            _padText = UiKit.Rect("Text", _padTag).Stretch(0, 0, 1, 1, PadTagPad / 2f, 0, PadTagPad / 2f, 0).AddText(L10n.T("pad.hint") + " +", PadHintSize, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _padText.lineSpacing = PadTagLines;
            FitPadTag(1f);
            _pad.gameObject.SetActive(false);

            Image rabbitShade = UiKit.Rect("RabbitShade", Root).Place(0.5f, 0.5f, -72, -106, GuestBox * 0.6f, GuestBox * 0.16f).AddImage(catalog.Glow, Palette.Alpha(Palette.Ink, 0.3f));
            rabbitShade.raycastTarget = false;
            _rabbitShade = rabbitShade.gameObject;
            _rabbitShade.SetActive(false);
            _rabbit = UiKit.Rect("Rabbit", Root).Place(0.5f, 0.5f, -72, -112, GuestBox, GuestBox);
            _rabbit.pivot = new Vector2(0.5f, 0f);
            _rabbitHome = _rabbit.anchoredPosition;
            _rabbitImage = _rabbit.AddImage(null, Color.white);
            _rabbitImage.preserveAspect = true;
            if (onStroke != null) UiKit.Strokable(_rabbit, scroll, () => onStroke(_index), () => onTap(_index));
            _rabbit.gameObject.SetActive(false);

            _hatImage = UiKit.Rect("HatOn", _rabbit).Place(0.5f, 0f, 0, 0, GuestBox * GameCatalog.HatSize, GuestBox * GameCatalog.HatSize).AddImage(null, Color.white);
            _hatImage.preserveAspect = true;
            _hatImage.gameObject.SetActive(false);
            _charmImage = UiKit.Rect("CharmOn", _rabbit).Place(0.5f, 0f, 0, 0, GuestBox * GameCatalog.CharmSize, GuestBox * GameCatalog.CharmSize).AddImage(null, Color.white);
            _charmImage.preserveAspect = true;
            _charmImage.gameObject.SetActive(false);

            _toolHome = new Vector2(GuestBox * 0.30f, GuestBox * 0.30f);
            _tool = UiKit.Rect("Tool", _rabbit).Place(0.5f, 0f, _toolHome.x, _toolHome.y, GuestBox * 0.26f, GuestBox * 0.26f);
            _tool.pivot = new Vector2(0.5f, 0.15f);
            _toolImage = _tool.AddImage(null, Color.white);
            _toolImage.preserveAspect = true;
            _tool.gameObject.SetActive(false);

            _upArrowRect = UiKit.Rect("CanUp", Front).Place(0.5f, 0.5f, 150, -118, 56, 56);
            _upArrowRect.AddImage(catalog.IconUp, Color.white).preserveAspect = true;
            _upArrow = _upArrowRect.gameObject;
            _upArrow.SetActive(false);

            var doze = UiKit.Rect("Doze", Root).Place(0.5f, 0.5f, -150, 120, 110, 54);
            doze.AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Text", doze).Stretch(0, 0, 1, 1).AddText("z z z", 26, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _dozeBubble = doze.gameObject;
            _dozeBubble.SetActive(false);

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
            _label.resizeTextForBestFit = true;
            _label.resizeTextMinSize = 16;
            _label.resizeTextMaxSize = 27;
            _label.verticalOverflow = VerticalWrapMode.Truncate;
            _wordsGroup = _label.gameObject.AddComponent<CanvasGroup>();
        }

        public const float PlateX = 16f, PlateW = 236f, PlateH = 58f, BarPlateH = 28f;
        public const int PadHintSize = 38;
        const float PadW = 250f, PadTagMinW = 150f, PadTagPad = 24f, PadTagX = -14f, PadTagY = 6f, PadTagLines = 0.9f, PadTagTrim = 6f;

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
            ResizePlate(tweens, _chipRect.sizeDelta.y, height);
        }

        void ResizePlate(Tweens tweens, float from, float height)
        {
            tweens.Add(Motion.Quick, t => { if (_chipRect != null) _chipRect.sizeDelta = new Vector2(PlateW, from + (height - from) * t); });
        }

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

        void FitPadTag(float scale)
        {
            float width = Mathf.Clamp(_padText.preferredWidth + PadTagPad, PadTagMinW, PadW / scale);
            _padTag.sizeDelta = new Vector2(width, _padTag.sizeDelta.y);
            float height = _padText.preferredHeight;
            while (width - PadTagTrim >= PadTagMinW)
            {
                _padTag.sizeDelta = new Vector2(width - PadTagTrim, _padTag.sizeDelta.y);
                if (_padText.preferredHeight > height + 0.5f) break;
                width -= PadTagTrim;
            }
            _padTag.sizeDelta = new Vector2(width, height + PadTagPad);
        }

        public void PlacePlate(float scale, Vector2 shift)
        {
            _chipRect.localScale = Vector3.one * scale;
            _padTag.localScale = Vector3.one * scale;
            FitPadTag(scale);
            _chipHome += shift;
            _chipRect.anchoredPosition = _chipHome;
        }

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
            _propImage.color = open ? Color.white : Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Ink, 0.25f), 0.55f);
            _glow.SetActive(open);
            _bar.SetActive(open && isBrew);
            if (open)
            {
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

        public void SetSign(string text, bool lit)
        {
            if (_look == StationLook.Open) return;
            _label.text = text;
            _label.color = ChunkyButton.InkFor(lit);
            _chip.color = lit ? Palette.Carrot : Palette.Track;
            _signBob = lit;
            if (!lit) _chipRect.anchoredPosition = _chipHome;
        }

        const float GuestBox = 256f;

        public void SetWorker(Sprite art, Sprite hat, Sprite charm, bool halo, Vector2 hatNudge = default, float hatLift = 0f, Color? fur = null, Sprite tool = null, RabbitTrade trade = RabbitTrade.None)
        {
            _trade = trade;
            Show(_toolImage, art != null ? tool : null);
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

        Color ChipColor() => _rank >= 2 ? Palette.Mix(Palette.Paper, Palette.Lantern, 0.5f) : Palette.Paper;

        public bool HasGuest => _guest;
        public Vector2 RabbitHome => _rabbitHome;
        public void SetAway(bool away)
        {
            if (_away == away) return;
            _away = away;
            ShowWho();
        }
        bool _away;
        public RectTransform Pad => _pad;

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

        public bool CanUpgradeShown => _upArrow.activeSelf;

        public void SetCanUpgrade(bool can)
        {
            if (_upArrow.activeSelf != can) _upArrow.SetActive(can);
        }

        public void SetDozing(bool dozing)
        {
            bool was = Dozing;
            Dozing = dozing && _look == StationLook.Open && _guest;
            if (Dozing && !was) _yawn = 1f;
            if (_dozeBubble.activeSelf != Dozing) _dozeBubble.SetActive(Dozing);
        }

        public bool Yawning => _yawn > 0f;

        public void Bump()
        {
            _bump = 1f;
        }

        public void Flash()
        {
            _flash = 1f;
        }

        public void Squish()
        {
            _squish = 1f;
        }

        public void PopProp()
        {
            _propPop = 1f;
        }

        public void Hop()
        {
            _propPop = 1f;
            _hop = 1f;
        }

        public void Animate(double progress, float time, float deltaTime, bool boosting = false)
        {
            bool open = _look == StationLook.Open;
            if (open)
            {
                float target = Mathf.Clamp01((float)progress);
                _shownProgress = target < _shownProgress ? target : Mathf.Lerp(_shownProgress, target, Mathf.Clamp01(deltaTime * 14f));
                _fill.anchorMax = new Vector2(Mathf.Max(0.05f, _shownProgress), 1f);
            }

            _bump = Mathf.Max(0f, _bump - deltaTime * 5f);
            Root.localScale = Vector3.one * (BaseScale * (1f + 0.14f * Ease.Pop(1f - _bump) * (_bump > 0f ? 1f : 0f)));
            _flash = Mathf.Max(0f, _flash - deltaTime * 3f);
            _glowRect.localScale = Vector3.one * (1f + 0.45f * Ease.Pop(1f - _flash) * (_flash > 0f ? 1f : 0f));
            _squish = Mathf.Max(0f, _squish - deltaTime * 5f);
            _propPop = Mathf.Max(0f, _propPop - deltaTime * 4f);
            _hop = Mathf.Max(0f, _hop - deltaTime * 2.6f);
            _yawn = Mathf.Max(0f, _yawn - deltaTime / Motion.Time(Motion.Yawn));
            float tempo = boosting ? 2f : 1f;

            if (_rabbit.gameObject.activeSelf)
            {
                float breathe = Motion.Reduced ? 0f : Mathf.Sin(time * 3f * tempo + _index * 1.7f) * 0.03f;
                float air = Motion.Reduced ? 0f : Mathf.Sin((1f - _hop) * Mathf.PI) * (_hop > 0f ? 1f : 0f);
                float land = !Motion.Reduced && _hop > 0f && _hop < 0.18f ? (0.18f - _hop) / 0.18f * 0.12f : 0f;
                float yawn = Ease.Pop(1f - _yawn) * (_yawn > 0f ? 1f : 0f);
                _rabbit.localScale = new Vector3(
                    1f + _squish * 0.12f - breathe * 0.5f - air * 0.06f + land - yawn * 0.04f,
                    1f - _squish * 0.18f + breathe + air * 0.10f - land + yawn * 0.09f, 1f);
                _rabbit.anchoredPosition = _rabbitHome + new Vector2(0, air * 46f);
                float lean = Dozing ? (Motion.Reduced ? 0f : Mathf.Sin(time * 0.9f + _index) * 2.2f) - 9f : Motion.Reduced ? 0f : Mathf.Sin(time * 2.1f * tempo + _index * 2.3f) * 3.2f;
                _rabbit.localRotation = Quaternion.Euler(0, 0, lean * (1f - air));
                if (_tool.gameObject.activeSelf) MoveTool(time * tempo);
            }
            if (open && _shownLevel < _level)
            {
                _shownLevel = Mathf.Min(_level, _shownLevel + _levelRun * deltaTime);
                _label.text = L10n.StationLevel(Mathf.FloorToInt(_shownLevel));
            }
            if (_rankHalo.activeSelf) _rankHaloRect.localScale = Vector3.one * (Motion.Reduced ? 1f : 1f + 0.06f * Mathf.Sin(time * 1.1f + _index));
            if (_pad.gameObject.activeSelf) _pad.localScale = Vector3.one * (Motion.Reduced ? 1f : 1f + 0.03f * Mathf.Sin(time * 2.2f + _index));
            if (_signBob) _chipRect.anchoredPosition = _chipHome + (Motion.Reduced ? Vector2.zero : new Vector2(0f, Mathf.Abs(Mathf.Sin(time * 3.4f + _index)) * 10f));
            if (_upArrow.activeSelf) _upArrowRect.anchoredPosition = new Vector2(150, -118 + (Motion.Reduced ? 0f : Mathf.Abs(Mathf.Sin(time * 4f + _index)) * 16f));
            float hop = Ease.Pop(1f - _propPop) * (_propPop > 0f ? 1f : 0f);
            _prop.localScale = new Vector3(1f - hop * 0.08f, 1f + hop * 0.14f, 1f);
        }
    }
}

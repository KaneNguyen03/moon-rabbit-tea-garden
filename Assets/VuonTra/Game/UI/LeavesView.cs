using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Core;
using TweenEase = DG.Tweening.Ease;

namespace VuonTra.Game.UI
{
    public sealed class LeavesView
    {
        const int Pool = 12;
        const int StunHalfSwings = 8;

        const float FieldHalf = 450f, SpawnY = 170f, LandY = -540f, BasketY = -660f;
        const float Linger = 0.4f;

        readonly GameObject _root, _result, _stunPill;
        readonly RectTransform _overlay, _basket, _basketArt, _resultCard;
        readonly RectTransform[] _slots = new RectTransform[Pool];
        readonly Image[] _slotImages = new Image[Pool];
        readonly Image _basketImage;
        readonly Color _basketTint;
        readonly Text _score, _timer, _judge, _resultTitle, _resultStars, _resultScore, _resultCarrots, _resultExtra;
        readonly Tweens _tweens;
        readonly Action _onQuit;
        readonly Action<LeavesRun> _onFinished;
        readonly bool[] _caught = new bool[Leaves.Drops];
        readonly bool[] _used = new bool[Pool];
        float _judgeAge = 9f, _stunLeft, _resultDelay, _basketX = 0.5f;
        Tween _basketMove;
        double _time;
        bool _resultShown;

        public LeavesRun Run { get; private set; }
        public bool IsOpen => _root.activeSelf;
        public RectTransform ResultCard => _resultCard;
        public double Time => _time;
        public float BasketX => _basketX;

        public LeavesView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onQuit, Action<LeavesRun> onFinished)
        {
            _tweens = tweens;
            _onQuit = onQuit;
            _onFinished = onFinished;

            _overlay = UiKit.Rect("Leaves", canvas).Stretch(0, 0, 1, 1);
            _root = _overlay.gameObject;
            _overlay.AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Grass, 0.35f), 0.86f)).raycastTarget = true;
            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            var trigger = _root.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerDown, e => MoveBasket(PointerX(((PointerEventData)e).position, canvasComponent)));
            Add(trigger, EventTriggerType.Drag, e => SetBasket(PointerX(((PointerEventData)e).position, canvasComponent)));

            UiKit.Rect("Title", _overlay).Place(0.5f, 1f, 0, -230, 900, 70)
                .AddText(L10n.T("leaves.title"), 48, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow();
            UiKit.Rect("Hint", _overlay).Place(0.5f, 1f, 0, -300, 980, 44)
                .AddText(L10n.T("leaves.hint"), UiKit.TextBody, Palette.Alpha(Palette.Paper, 0.85f), TextAnchor.MiddleCenter);
            _score = UiKit.Rect("Score", _overlay).Place(0f, 1f, 164, -400, 280, 60)
                .AddText("", UiKit.TextTitle, Palette.Lantern, TextAnchor.MiddleLeft, FontStyle.Bold);
            _timer = UiKit.Rect("Timer", _overlay).Place(1f, 1f, -184, -400, 320, 60)
                .AddText("", UiKit.TextTitle, Palette.Paper, TextAnchor.MiddleRight, FontStyle.Bold);

            Sprite tree = catalog.Decor != null && catalog.Decor.Length > 0 ? catalog.Decor[0] : null;
            var canopy = UiKit.Rect("Canopy", _overlay).Place(0.5f, 0.5f, 0, 290, 640, 480);
            Image canopyImage = canopy.AddImage(tree != null ? tree : catalog.Round, tree != null ? Color.white : Palette.Moss);
            canopyImage.preserveAspect = true;
            if (tree == null) canopy.sizeDelta = new Vector2(760, 340);

            var field = UiKit.Rect("Field", _overlay).Stretch(0, 0, 1, 1);
            for (int i = 0; i < Pool; i++)
            {
                _slots[i] = UiKit.Rect("Drop " + i, field).Place(0.5f, 0.5f, 0, 0, 56, 36);
                _slotImages[i] = _slots[i].AddImage(catalog.Round, Palette.Moss);
                _slots[i].gameObject.SetActive(false);
            }

            Sprite basketSprite = null;
            if (catalog.Stations != null)
                foreach (StationData station in catalog.Stations)
                    if (station != null && station.Id == "gio-la" && station.Prop != null) { basketSprite = station.Prop; break; }
            _basket = UiKit.Rect("Basket", _overlay).Place(0.5f, 0.5f, 0, BasketY, 280, 240);
            _basket.pivot = new Vector2(0.5f, 0f);
            _basketArt = UiKit.Rect("Art", _basket).Stretch(0, 0, 1, 1);
            _basketArt.pivot = new Vector2(0.5f, 0f);
            _basketTint = basketSprite != null ? Color.white : Palette.Mix(Palette.Carrot, Palette.Ink, 0.35f);
            _basketImage = _basketArt.AddImage(basketSprite != null ? basketSprite : catalog.Round, _basketTint);
            _basketImage.preserveAspect = true;
            if (basketSprite == null) _basket.sizeDelta = new Vector2(240, 120);

            _judge = UiKit.Rect("Judge", _overlay).Place(0.5f, 0.5f, 0, -380, 600, 70)
                .AddText("", 44, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _judge.AddShadow();
            var stun = UiKit.Rect("Stun", _overlay).Place(0.5f, 0.5f, 0, -300, 300, 64);
            stun.AddImage(catalog.Panel, Palette.Blush, true);
            UiKit.Rect("Text", stun).Stretch(0, 0, 1, 1).AddText(L10n.T("leaves.cone"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _stunPill = stun.gameObject;
            _stunPill.SetActive(false);

            var quit = UiKit.Rect("Quit", _overlay).Place(1f, 1f, -140, -136, 220, 88);
            Image quitImage = quit.AddImage(catalog.Panel, Palette.Alpha(Palette.Paper, 0.2f), true);
            quitImage.raycastTarget = true;
            var quitButton = quit.gameObject.AddComponent<Button>();
            quitButton.transition = Selectable.Transition.None;
            quitButton.targetGraphic = quitImage;
            quitButton.onClick.AddListener(() => { Sfx.Click(); Close(); _onQuit(); });
            UiKit.Rect("Text", quit).Stretch(0, 0, 1, 1).AddText(L10n.T("leaves.quit"), UiKit.TextBody, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);

            var result = UiKit.Rect("Result", _overlay).Stretch(0, 0, 1, 1);
            result.AddTapCatcher();
            _result = result.gameObject;
            _resultCard = UiKit.Rect("Card", result).Place(0.5f, 0.5f, 0, 0, 860, 720);
            _resultCard.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _resultCard).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _resultCard).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            _resultTitle = UiKit.Rect("Title", _resultCard).Place(0.5f, 1f, 0, -90, 760, 70)
                .AddText("", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _resultStars = UiKit.Rect("Stars", _resultCard).Place(0.5f, 1f, 0, -190, 760, 90)
                .AddText("", 72, Palette.Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            _resultScore = UiKit.Rect("Score", _resultCard).Place(0.5f, 1f, 0, -280, 760, 50)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _resultCarrots = UiKit.Rect("Carrots", _resultCard).Place(0.5f, 1f, 0, -350, 760, 60)
                .AddText("", 40, Palette.CarrotLip, TextAnchor.MiddleCenter, FontStyle.Bold);
            _resultExtra = UiKit.Rect("Extra", _resultCard).Place(0.5f, 1f, 0, -440, 760, 110)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperCenter);
            var done = UiKit.Rect("Done", _resultCard).Place(0.5f, 0f, 0, 100, 420, 110);
            ChunkyButton doneButton = null;
            doneButton = UiKit.Chunky(done, catalog.Panel, () => { doneButton.Press(_tweens); Sfx.Blip(); Close(); _onQuit(); });
            UiKit.Rect("Text", doneButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("leaves.done"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.SetActive(false);

            _root.SetActive(false);
        }

        static void Add(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> call)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(call);
            trigger.triggers.Add(entry);
        }

        float PointerX(Vector2 screen, Canvas canvas)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, canvas != null ? canvas.worldCamera : null, out Vector2 local))
                return _basketX;
            return Mathf.Clamp01((local.x + FieldHalf) / (2f * FieldHalf));
        }

        public void Open(LeavesRun run)
        {
            Run = run;
            _time = 0d;
            _judgeAge = 9f;
            _stunLeft = 0f;
            _resultDelay = 0f;
            _resultShown = false;
            _basketMove?.Kill();
            _basketX = 0.5f;
            for (int i = 0; i < _caught.Length; i++) _caught[i] = false;
            for (int i = 0; i < Pool; i++) _slots[i].gameObject.SetActive(false);
            _result.SetActive(false);
            _stunPill.SetActive(false);
            _basket.DOKill();
            _basket.localRotation = Quaternion.identity;
            _basketImage.color = _basketTint;
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Place();
            Draw();
        }

        public void Close()
        {
            _root.SetActive(false);
            Run = null;
        }

        public void MoveBasket(float x)
        {
            if (Run == null || _resultShown) return;
            _basketMove?.Kill();
            _basketMove = DOTween.To(() => _basketX, v => _basketX = v, Mathf.Clamp01(x), Motion.Time(0.14f))
                .SetEase(Motion.Curve(TweenEase.OutCubic)).OnUiClock(_overlay);
        }

        public void SetBasket(float x)
        {
            if (Run == null || _resultShown) return;
            _basketMove?.Kill();
            _basketX = Mathf.Clamp01(x);
        }

        public void Update(float dt)
        {
            if (!IsOpen || Run == null) return;
            _time += dt;
            IReadOnlyList<Drop> settled = Run.Advance(_time, _basketX);
            for (int i = 0; i < settled.Count; i++) Show(settled[i]);
            _judgeAge += dt;
            _stunLeft = Mathf.Max(0f, _stunLeft - dt);
            if (_stunPill.activeSelf != (_stunLeft > 0f)) _stunPill.SetActive(_stunLeft > 0f);
            Color judge = _judge.color;
            judge.a = Mathf.Clamp01(1f - (_judgeAge - 0.5f) * 3f);
            _judge.color = judge;

            if (_time >= Leaves.Seconds && !_resultShown)
            {
                _resultDelay += dt;
                if (_resultDelay >= 0.6f) ShowResult();
            }
            Place();
            Draw();
        }

        void Show(Drop drop)
        {
            if (drop.Leaf.Index >= 0 && drop.Leaf.Index < _caught.Length) _caught[drop.Leaf.Index] = drop.Caught;
            if (!drop.Caught) return;
            _judgeAge = 0f;
            switch (drop.Leaf.Kind)
            {
                case LeafKind.Leaf:
                    _judge.text = L10n.T("leaves.leaf");
                    _judge.color = Palette.Paper;
                    Sfx.Tick();
                    break;
                case LeafKind.Golden:
                    _judge.text = L10n.T("leaves.golden");
                    _judge.color = Palette.Lantern;
                    Sfx.Reward();
                    Haptics.Light();
                    break;
                default:
                    _judge.text = L10n.T("leaves.cone");
                    _judge.color = Palette.Blush;
                    _stunLeft = (float)Leaves.StunSeconds;
                    if (!Motion.Reduced) Motion.ShakeAngle(_basket, 10f, _stunLeft, StunHalfSwings);
                    Sfx.Deny();
                    Haptics.Medium();
                    break;
            }
        }

        void Place()
        {
            float x = -FieldHalf + _basketX * 2f * FieldHalf;
            _basket.anchoredPosition = new Vector2(x, BasketY);
            _basketImage.color = _stunLeft > 0f ? Palette.Mix(_basketTint, Palette.Ink, 0.4f) : _basketTint;

            IReadOnlyList<Leaf> schedule = Run.Schedule;
            bool[] used = _used;
            for (int i = 0; i < Pool; i++) used[i] = false;
            for (int i = 0; i < schedule.Count; i++)
            {
                Leaf leaf = schedule[i];
                if (leaf.Drop > _time) break;
                if (leaf.Lands + Linger < _time) continue;
                int slot = i % Pool;
                used[slot] = true;
                RectTransform rt = _slots[slot];
                Image image = _slotImages[slot];
                if (!rt.gameObject.activeSelf) rt.gameObject.SetActive(true);
                float sway = Motion.Reduced ? 0f : Mathf.Sin((float)(_time - leaf.Drop) * 3f + i) * 18f;
                float lx = -FieldHalf + (float)leaf.X * 2f * FieldHalf;
                float k = Mathf.Clamp01((float)((_time - leaf.Drop) / Math.Max(0.01d, leaf.Lands - leaf.Drop)));
                Color color = leaf.Kind == LeafKind.Leaf ? Palette.Moss : (leaf.Kind == LeafKind.Golden ? Palette.Lantern : Palette.Mix(Palette.Carrot, Palette.Ink, 0.55f));
                rt.sizeDelta = leaf.Kind == LeafKind.Cone ? new Vector2(52, 60) : (leaf.Kind == LeafKind.Golden ? new Vector2(64, 40) : new Vector2(56, 36));
                float scale = 1f, alpha = 1f;
                float y = Mathf.Lerp(SpawnY, LandY, k);
                if (_time > leaf.Lands)
                {
                    float after = Mathf.Clamp01((float)(_time - leaf.Lands) / Linger);
                    bool caught = i < _caught.Length && _caught[i];
                    if (caught)
                    {
                        lx = Mathf.Lerp(lx, x, after);
                        y = Mathf.Lerp(LandY, BasketY + 60f, after);
                        scale = 1f - after;
                        sway = 0f;
                    }
                    else
                    {
                        y = LandY - 220f * after;
                        alpha = 1f - after;
                    }
                }
                rt.anchoredPosition = new Vector2(lx + sway, y);
                rt.localScale = Vector3.one * scale;
                rt.localRotation = Quaternion.Euler(0, 0, Motion.Reduced || leaf.Kind == LeafKind.Cone ? 0f : 30f + sway * 1.5f);
                image.color = Palette.Alpha(color, alpha);
            }
            for (int slot = 0; slot < Pool; slot++)
                if (!used[slot] && _slots[slot].gameObject.activeSelf) _slots[slot].gameObject.SetActive(false);
        }

        void Draw()
        {
            if (Run == null) return;
            _score.text = L10n.T("leaves.score", Run.Score);
            _timer.text = L10n.T("leaves.time", Mathf.Max(0, Mathf.CeilToInt((float)(Leaves.Seconds - _time))));
        }

        void ShowResult()
        {
            _resultShown = true;
            _onFinished(Run);
        }

        public void ShowPaid(LeavesResult result)
        {
            _resultTitle.text = L10n.T("leaves.result");
            _resultStars.text = new string('★', result.Stars) + new string('☆', 3 - result.Stars);
            _resultScore.text = L10n.T("leaves.score", result.Score) + " · " + L10n.T("leaves.caught", Run != null ? Run.Caught : 0);
            _resultCarrots.text = L10n.T("leaves.reward", result.Carrots.Short());
            _resultExtra.text = result.Clovers > 0 ? L10n.T("leaves.clover") : "";
            _result.SetActive(true);
            Motion.PopIn(_resultCard);
        }
    }
}

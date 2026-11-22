using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The leaf-catching game on screen: a tea tree's canopy at the top, leaves and pine cones falling from it on
    /// the game's schedule, and a basket at the bottom that follows the finger. The rules live in
    /// <see cref="LeavesRun"/>; this only draws them, keeps the clock and turns touches into a basket position.
    /// Every falling thing is placed from the clock each frame, never moved by accumulation.
    /// </summary>
    public sealed class LeavesView
    {
        /// <summary>Falling things on screen at once, at most: a leaf's slot is its index in the schedule modulo this.</summary>
        const int Pool = 12;
        const float FieldHalf = 450f, SpawnY = 170f, LandY = -540f, BasketY = -660f;
        /// <summary>After landing, a missed leaf keeps falling and fading for this long; a caught one shrinks into the basket.</summary>
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
        int _moveToken;
        double _time;
        bool _resultShown;

        public LeavesRun Run { get; private set; }
        public bool IsOpen => _root.activeSelf;
        /// <summary>The result card, where the extra round's ad button hangs (#66).</summary>
        public RectTransform ResultCard => _resultCard;
        /// <summary>Seconds into the game, the same clock the rules are settled on.</summary>
        public double Time => _time;
        /// <summary>Where the basket stands, 0 at the left edge of the field and 1 at the right.</summary>
        public float BasketX => _basketX;

        public LeavesView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onQuit, Action<LeavesRun> onFinished)
        {
            _tweens = tweens;
            _onQuit = onQuit;
            _onFinished = onFinished;

            _overlay = UiKit.Rect("Leaves", canvas).Stretch(0, 0, 1, 1);
            _root = _overlay.gameObject;
            _overlay.AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Grass, 0.35f), 0.86f)).raycastTarget = true;
            // Touches anywhere on the field: a tap sends the basket there, a drag carries it along.
            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            var trigger = _root.AddComponent<EventTrigger>();
            Add(trigger, EventTriggerType.PointerDown, e => MoveBasket(PointerX(((PointerEventData)e).position, canvasComponent)));
            Add(trigger, EventTriggerType.Drag, e => SetBasket(PointerX(((PointerEventData)e).position, canvasComponent)));

            UiKit.Rect("Title", _overlay).Place(0.5f, 1f, 0, -230, 900, 70)
                .AddText(L10n.T("leaves.title"), 48, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow();
            UiKit.Rect("Hint", _overlay).Place(0.5f, 1f, 0, -300, 980, 44)
                .AddText(L10n.T("leaves.hint"), UiKit.TextBody, Palette.Alpha(Palette.Paper, 0.85f), TextAnchor.MiddleCenter);
            // The score and the count keep 24 from either edge of the screen (#77).
            _score = UiKit.Rect("Score", _overlay).Place(0f, 1f, 164, -400, 280, 60)
                .AddText("", UiKit.TextTitle, Palette.Lantern, TextAnchor.MiddleLeft, FontStyle.Bold);
            _timer = UiKit.Rect("Timer", _overlay).Place(1f, 1f, -184, -400, 320, 60)
                .AddText("", UiKit.TextTitle, Palette.Paper, TextAnchor.MiddleRight, FontStyle.Bold);

            // The canopy: the garden's tree when there is one, else a round of moss.
            Sprite tree = catalog.Decor != null && catalog.Decor.Length > 0 ? catalog.Decor[0] : null;
            var canopy = UiKit.Rect("Canopy", _overlay).Place(0.5f, 0.5f, 0, 290, 640, 480);
            Image canopyImage = canopy.AddImage(tree != null ? tree : catalog.Round, tree != null ? Color.white : Palette.Moss);
            canopyImage.preserveAspect = true;
            if (tree == null) canopy.sizeDelta = new Vector2(760, 340);

            // The falling things: a fixed pool, each drawn from the clock while its leaf is in the air.
            var field = UiKit.Rect("Field", _overlay).Stretch(0, 0, 1, 1);
            for (int i = 0; i < Pool; i++)
            {
                _slots[i] = UiKit.Rect("Drop " + i, field).Place(0.5f, 0.5f, 0, 0, 56, 36);
                _slotImages[i] = _slots[i].AddImage(catalog.Round, Palette.Moss);
                _slots[i].gameObject.SetActive(false);
            }

            // The basket: the tea-leaf station's own basket when the catalog has it.
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

            // The result: stars, the score and what it paid.
            var result = UiKit.Rect("Result", _overlay).Stretch(0, 0, 1, 1);
            result.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true; // taps on the field behind are swallowed
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

        /// <summary>A pointer position in screen pixels as a place across the field, 0 to 1.</summary>
        float PointerX(Vector2 screen, Canvas canvas)
        {
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, canvas != null ? canvas.worldCamera : null, out Vector2 local))
                return _basketX;
            return Mathf.Clamp01((local.x + FieldHalf) / (2f * FieldHalf));
        }

        /// <summary>Starts a game.</summary>
        public void Open(LeavesRun run)
        {
            Run = run;
            _time = 0d;
            _judgeAge = 9f;
            _stunLeft = 0f;
            _resultDelay = 0f;
            _resultShown = false;
            _moveToken++;
            _basketX = 0.5f;
            for (int i = 0; i < _caught.Length; i++) _caught[i] = false;
            for (int i = 0; i < Pool; i++) _slots[i].gameObject.SetActive(false);
            _result.SetActive(false);
            _stunPill.SetActive(false);
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

        /// <summary>A tap: the basket slides to <paramref name="x"/> (0 left, 1 right); at once under reduced motion.</summary>
        public void MoveBasket(float x)
        {
            if (Run == null || _resultShown) return;
            int token = ++_moveToken;
            float from = _basketX, to = Mathf.Clamp01(x);
            _tweens.Add(Motion.Time(0.14f), t => { if (token == _moveToken && _root != null) _basketX = Mathf.LerpUnclamped(from, to, t); }, Motion.Curve(Ease.OutCubic));
        }

        /// <summary>A drag: the basket is wherever the finger is.</summary>
        public void SetBasket(float x)
        {
            if (Run == null || _resultShown) return;
            _moveToken++;
            _basketX = Mathf.Clamp01(x);
        }

        /// <summary>The clock. Drops that reach the basket line are settled; when the thirty seconds are up the result comes up.</summary>
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
                    Sfx.Deny();
                    Haptics.Medium();
                    break;
            }
        }

        /// <summary>Everything in the air and the basket, placed from the clock.</summary>
        void Place()
        {
            float x = -FieldHalf + _basketX * 2f * FieldHalf;
            _basket.anchoredPosition = new Vector2(x, BasketY);
            // A caught pine cone: the basket wobbles and goes dim while it is stunned. Still under reduced motion.
            float wobble = _stunLeft > 0f && !Motion.Reduced ? Mathf.Sin(_stunLeft * 50f) * 10f * _stunLeft / (float)Leaves.StunSeconds : 0f;
            _basket.localRotation = Quaternion.Euler(0, 0, wobble);
            _basketImage.color = _stunLeft > 0f ? Palette.Mix(_basketTint, Palette.Ink, 0.4f) : _basketTint;

            IReadOnlyList<Leaf> schedule = Run.Schedule;
            bool[] used = _used;
            for (int i = 0; i < Pool; i++) used[i] = false;
            for (int i = 0; i < schedule.Count; i++)
            {
                Leaf leaf = schedule[i];
                if (leaf.Drop > _time) break; // not yet, nor anything after it
                if (leaf.Lands + Linger < _time) continue; // gone
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
                        // Into the basket: slides to its middle and shrinks away.
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

        /// <summary>Fills the result card once the game has been paid.</summary>
        public void ShowPaid(LeavesResult result)
        {
            _resultTitle.text = L10n.T("leaves.result");
            _resultStars.text = new string('★', result.Stars) + new string('☆', 3 - result.Stars);
            _resultScore.text = L10n.T("leaves.score", result.Score) + " · " + L10n.T("leaves.caught", Run != null ? Run.Caught : 0);
            _resultCarrots.text = L10n.T("leaves.reward", result.Carrots.Short());
            _resultExtra.text = result.Clovers > 0 ? L10n.T("leaves.clover") : "";
            _result.SetActive(true);
            _resultCard.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_resultCard != null) _resultCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
        }
    }
}

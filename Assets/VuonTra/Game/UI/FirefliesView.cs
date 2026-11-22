using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The firefly night on screen: the garden washed dark, a swarm of glowing dots crossing it on the night's
    /// schedule, and a glass jar at the bottom that brightens with every catch. The rules live in
    /// <see cref="FirefliesRun"/>; this only draws them, keeps the clock and turns taps into catches.
    /// Every fly is placed from the clock each frame from a fixed pool, never moved by accumulation.
    /// </summary>
    public sealed class FirefliesView
    {
        /// <summary>The field the flies cross: half its width, and its bottom and top, in canvas units.</summary>
        const float FieldHalf = 450f, FieldBottom = -520f, FieldTop = 260f;
        /// <summary>A caught fly swells and fades for this long.</summary>
        const float Pop = 0.35f;

        readonly GameObject _root, _result;
        readonly RectTransform _overlay, _judge, _resultCard, _jarGlow;
        readonly RectTransform[] _slots = new RectTransform[Fireflies.Count];
        readonly Image[] _slotGlows = new Image[Fireflies.Count];
        readonly Image[] _slotCores = new Image[Fireflies.Count];
        readonly Image _jarGlowImage;
        readonly Text _score, _timer, _judgeText, _resultTitle, _resultStars, _resultScore, _resultCarrots, _resultExtra;
        readonly Tweens _tweens;
        readonly Action _onQuit;
        readonly Action<FirefliesRun> _onFinished;
        readonly float[] _caughtAt = new float[Fireflies.Count];
        float _judgeAge = 9f, _resultDelay;
        double _time;
        bool _resultShown;

        public FirefliesRun Run { get; private set; }
        public bool IsOpen => _root.activeSelf;
        /// <summary>The result card, where the extra round's ad button hangs (#66).</summary>
        public RectTransform ResultCard => _resultCard;
        /// <summary>Seconds into the night, the same clock the rules are settled on.</summary>
        public double Time => _time;

        public FirefliesView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onQuit, Action<FirefliesRun> onFinished)
        {
            _tweens = tweens;
            _onQuit = onQuit;
            _onFinished = onFinished;

            _overlay = UiKit.Rect("Fireflies", canvas).Stretch(0, 0, 1, 1);
            _root = _overlay.gameObject;
            // The night wash: the garden shows through, dark enough for the flies to glow.
            _overlay.AddImage(null, Palette.Alpha(Palette.Night, 0.88f)).raycastTarget = true;
            Canvas canvasComponent = canvas.GetComponent<Canvas>();
            var trigger = _root.AddComponent<EventTrigger>();
            var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            entry.callback.AddListener(e =>
            {
                if (PointerSpot(((PointerEventData)e).position, canvasComponent, out Vector2 spot)) Tap(spot.x, spot.y);
            });
            trigger.triggers.Add(entry);

            UiKit.Rect("Title", _overlay).Place(0.5f, 1f, 0, -230, 900, 70)
                .AddText(L10n.T("fireflies.title"), 48, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow();
            UiKit.Rect("Hint", _overlay).Place(0.5f, 1f, 0, -300, 980, 44)
                .AddText(L10n.T("fireflies.hint"), UiKit.TextBody, Palette.Alpha(Palette.Paper, 0.85f), TextAnchor.MiddleCenter);
            // The score and the count keep 24 from either edge of the screen (#77).
            _score = UiKit.Rect("Score", _overlay).Place(0f, 1f, 164, -400, 280, 60)
                .AddText("", UiKit.TextTitle, Palette.Lantern, TextAnchor.MiddleLeft, FontStyle.Bold);
            _timer = UiKit.Rect("Timer", _overlay).Place(1f, 1f, -184, -400, 320, 60)
                .AddText("", UiKit.TextTitle, Palette.Paper, TextAnchor.MiddleRight, FontStyle.Bold);

            // The jar at the bottom: glass, a glow inside that grows with the catch, and a cork.
            var jar = UiKit.Rect("Jar", _overlay).Place(0.5f, 0.5f, 0, -720, 220, 260);
            _jarGlow = UiKit.Rect("Glow", jar).Place(0.5f, 0.5f, 0, -20, 320, 320);
            _jarGlowImage = _jarGlow.AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0f));
            UiKit.Rect("Glass", jar).Stretch(0, 0, 1, 1, 0, 0, 0, 36).AddImage(catalog.Panel, Palette.Alpha(Palette.Mix(Palette.Paper, Palette.Night, 0.2f), 0.35f), true);
            UiKit.Rect("Cork", jar).Place(0.5f, 1f, 0, -18, 120, 44).AddImage(catalog.Panel, Palette.Mix(Palette.Carrot, Palette.Ink, 0.45f), true);

            // The swarm: one slot per fly, each a glow with a bright core, drawn from the clock while its fly is out.
            var field = UiKit.Rect("Field", _overlay).Stretch(0, 0, 1, 1);
            for (int i = 0; i < Fireflies.Count; i++)
            {
                _slots[i] = UiKit.Rect("Fly " + i, field).Place(0.5f, 0.5f, 0, 0, 60, 60);
                _slotGlows[i] = _slots[i].AddImage(catalog.Glow, Palette.Lantern);
                _slotCores[i] = UiKit.Rect("Core", _slots[i]).Place(0.5f, 0.5f, 0, 0, 12, 12).AddImage(catalog.Round, Palette.Mix(Palette.Lantern, Color.white, 0.6f));
                _slots[i].gameObject.SetActive(false);
            }

            _judge = UiKit.Rect("Judge", _overlay).Place(0.5f, 0.5f, 0, 0, 400, 70);
            _judgeText = _judge.AddText("", 44, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _judgeText.AddShadow();

            var quit = UiKit.Rect("Quit", _overlay).Place(1f, 1f, -140, -136, 220, 88);
            Image quitImage = quit.AddImage(catalog.Panel, Palette.Alpha(Palette.Paper, 0.2f), true);
            quitImage.raycastTarget = true;
            var quitButton = quit.gameObject.AddComponent<Button>();
            quitButton.transition = Selectable.Transition.None;
            quitButton.targetGraphic = quitImage;
            quitButton.onClick.AddListener(() => { Sfx.Click(); Close(); _onQuit(); });
            UiKit.Rect("Text", quit).Stretch(0, 0, 1, 1).AddText(L10n.T("fireflies.quit"), UiKit.TextBody, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);

            // The result: stars, the score and what it paid.
            var result = UiKit.Rect("Result", _overlay).Stretch(0, 0, 1, 1);
            result.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true; // taps on the field behind are swallowed
            _result = result.gameObject;
            _resultCard = UiKit.Rect("Card", result).Place(0.5f, 0.5f, 0, 0, 860, 760);
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
            _resultExtra = UiKit.Rect("Extra", _resultCard).Place(0.5f, 1f, 0, -500, 760, 160)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperCenter);
            var done = UiKit.Rect("Done", _resultCard).Place(0.5f, 0f, 0, 100, 420, 110);
            ChunkyButton doneButton = null;
            doneButton = UiKit.Chunky(done, catalog.Panel, () => { doneButton.Press(_tweens); Sfx.Blip(); Close(); _onQuit(); });
            UiKit.Rect("Text", doneButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("fireflies.done"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.SetActive(false);

            _root.SetActive(false);
        }

        /// <summary>A pointer position in screen pixels as a place on the field, both 0 to 1.</summary>
        bool PointerSpot(Vector2 screen, Canvas canvas, out Vector2 spot)
        {
            spot = Vector2.zero;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_overlay, screen, canvas != null ? canvas.worldCamera : null, out Vector2 local))
                return false;
            spot = new Vector2(Mathf.Clamp01((local.x + FieldHalf) / (2f * FieldHalf)), Mathf.Clamp01((local.y - FieldBottom) / (FieldTop - FieldBottom)));
            return true;
        }

        /// <summary>A place on the field as canvas units.</summary>
        static Vector2 FieldPoint(double x, double y) => new Vector2(-FieldHalf + (float)x * 2f * FieldHalf, FieldBottom + (float)y * (FieldTop - FieldBottom));

        /// <summary>Starts a night.</summary>
        public void Open(FirefliesRun run)
        {
            Run = run;
            _time = 0d;
            _judgeAge = 9f;
            _resultDelay = 0f;
            _resultShown = false;
            for (int i = 0; i < _caughtAt.Length; i++) _caughtAt[i] = -1f;
            for (int i = 0; i < _slots.Length; i++) _slots[i].gameObject.SetActive(false);
            _result.SetActive(false);
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

        /// <summary>A tap at (<paramref name="x"/>, <paramref name="y"/>) on the field, both 0 to 1: the nearest fly within reach is caught.</summary>
        public void Tap(float x, float y)
        {
            if (Run == null || _resultShown || _time >= Fireflies.Seconds) return;
            int index = Run.Catch(x, y, _time);
            if (index < 0) return;
            Firefly fly = Run.Schedule[index];
            if (index < _caughtAt.Length) _caughtAt[index] = (float)_time;
            FlySpot spot = Fireflies.PositionAt(fly, _time);
            _judge.anchoredPosition = FieldPoint(spot.X, spot.Y) + new Vector2(0, 60f);
            _judgeAge = 0f;
            _judgeText.text = L10n.T(fly.Golden ? "fireflies.golden" : "fireflies.plain");
            _judgeText.color = fly.Golden ? Palette.Lantern : Palette.Paper;
            if (fly.Golden)
            {
                Sfx.Reward();
                Haptics.Light();
            }
            else Sfx.Tick();
            Draw();
        }

        /// <summary>The clock. When the twenty seconds are up the result comes up.</summary>
        public void Update(float dt)
        {
            if (!IsOpen || Run == null) return;
            _time += dt;
            _judgeAge += dt;
            Color judge = _judgeText.color;
            judge.a = Mathf.Clamp01(1f - (_judgeAge - 0.5f) * 3f);
            _judgeText.color = judge;

            if (_time >= Fireflies.Seconds && !_resultShown)
            {
                _resultDelay += dt;
                if (_resultDelay >= 0.6f) ShowResult();
            }
            Place();
            Draw();
        }

        /// <summary>Every fly that is out, placed from the clock; a caught one swells and fades where it was taken.</summary>
        void Place()
        {
            IReadOnlyList<Firefly> schedule = Run.Schedule;
            float t = (float)_time;
            for (int i = 0; i < schedule.Count && i < _slots.Length; i++)
            {
                Firefly fly = schedule[i];
                RectTransform rt = _slots[i];
                bool caught = _caughtAt[i] >= 0f;
                bool show = caught ? t - _caughtAt[i] < Pop : Fireflies.Visible(fly, _time);
                if (rt.gameObject.activeSelf != show) rt.gameObject.SetActive(show);
                if (!show) continue;
                double at = caught ? _caughtAt[i] : _time;
                FlySpot spot = Fireflies.PositionAt(fly, at);
                rt.anchoredPosition = FieldPoint(spot.X, spot.Y);
                float size = fly.Golden ? 84f : 60f;
                Color glow = fly.Golden ? Palette.Mix(Palette.Lantern, Color.white, 0.25f) : Palette.Lantern;
                float scale = 1f, alpha = 1f;
                if (caught)
                {
                    float k = Mathf.Clamp01((t - _caughtAt[i]) / Pop);
                    scale = 1f + 1.2f * k;
                    alpha = 1f - k;
                }
                else if (!Motion.Reduced)
                {
                    // Each fly breathes on its own beat; steady under reduced motion.
                    float pulse = 0.5f + 0.5f * Mathf.Sin(t * 5f + i * 1.7f);
                    scale = 0.8f + 0.35f * pulse;
                    alpha = 0.55f + 0.45f * pulse;
                }
                rt.sizeDelta = new Vector2(size, size);
                rt.localScale = Vector3.one * scale;
                _slotGlows[i].color = Palette.Alpha(glow, alpha);
                _slotCores[i].color = Palette.Alpha(Palette.Mix(glow, Color.white, 0.6f), alpha);
            }
            // The jar brightens with the catch.
            float lit = Mathf.Clamp01(Run.Score / (float)Fireflies.MaxScore);
            _jarGlowImage.color = Palette.Alpha(Palette.Lantern, 0.15f + 0.75f * lit);
            _jarGlow.localScale = Vector3.one * (0.6f + 0.6f * lit);
        }

        void Draw()
        {
            if (Run == null) return;
            _score.text = L10n.T("fireflies.score", Run.Score);
            _timer.text = L10n.T("fireflies.time", Mathf.Max(0, Mathf.CeilToInt((float)(Fireflies.Seconds - _time))));
        }

        void ShowResult()
        {
            _resultShown = true;
            _onFinished(Run);
        }

        /// <summary>Fills the result card once the night has been paid.</summary>
        public void ShowPaid(FirefliesResult result)
        {
            _resultTitle.text = L10n.T("fireflies.result");
            _resultStars.text = new string('★', result.Stars) + new string('☆', 3 - result.Stars);
            _resultScore.text = L10n.T("fireflies.score", result.Score) + " · " + L10n.T("fireflies.caught", Run != null ? Run.Caught : 0);
            _resultCarrots.text = L10n.T("fireflies.reward", result.Carrots.Short());
            var extra = new List<string>(3);
            if (result.Clovers > 0) extra.Add(L10n.T("fireflies.clovers", result.Clovers));
            if (result.Golden > 0) extra.Add(L10n.T("fireflies.goldenCarrot"));
            if (result.Jars > 0) extra.Add(L10n.T("fireflies.jar"));
            if (result.CloversCapped || result.GoldenCapped) extra.Add(L10n.T("fireflies.capped"));
            _resultExtra.text = string.Join("\n", extra);
            _result.SetActive(true);
            _resultCard.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_resultCard != null) _resultCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
        }
    }
}

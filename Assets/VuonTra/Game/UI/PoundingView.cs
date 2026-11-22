using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The mooncake game on screen: the mortar in the middle, a pestle that comes down on every beat, a ring
    /// that shrinks onto the cake so the player sees the beat coming, and a tap anywhere to pound. The rules
    /// live in <see cref="PoundingRun"/>; this only draws them and keeps the clock.
    /// </summary>
    public sealed class PoundingView
    {
        readonly GameObject _root, _result, _chewy, _helper;
        readonly RectTransform _mortar, _ring, _cake, _shake, _resultCard;
        readonly Image _cakeImage, _helperArt;
        readonly Text _score, _beats, _judge, _helperName, _resultTitle, _resultStars, _resultScore, _resultCarrots, _resultExtra;
        readonly Tweens _tweens;
        readonly Action _onQuit;
        readonly Action<PoundingRun> _onFinished;
        float _judgeAge = 9f, _pestleDown, _shakeLeft, _resultDelay;
        double _time;
        bool _resultShown;

        public PoundingRun Run { get; private set; }
        public bool IsOpen => _root.activeSelf;
        /// <summary>Seconds into the game, the same clock the rules are scored on.</summary>
        public double Time => _time;

        public PoundingView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onQuit, Action<PoundingRun> onFinished)
        {
            _tweens = tweens;
            _onQuit = onQuit;
            _onFinished = onFinished;

            var overlay = UiKit.Rect("Pound", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.5f), 0.82f)).raycastTarget = true;
            var tapAnywhere = _root.AddComponent<Button>();
            tapAnywhere.transition = Selectable.Transition.None;
            tapAnywhere.onClick.AddListener(Tap);

            UiKit.Rect("Title", overlay).Place(0.5f, 1f, 0, -230, 900, 70)
                .AddText(L10n.T("pound.title"), 48, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold).AddShadow();
            UiKit.Rect("Hint", overlay).Place(0.5f, 1f, 0, -300, 900, 44)
                .AddText(L10n.T("pound.hint"), UiKit.TextBody, Palette.Alpha(Palette.Paper, 0.85f), TextAnchor.MiddleCenter);
            _score = UiKit.Rect("Score", overlay).Place(0f, 1f, 160, -400, 280, 60)
                .AddText("", UiKit.TextTitle, Palette.Lantern, TextAnchor.MiddleLeft, FontStyle.Bold);
            _beats = UiKit.Rect("Beats", overlay).Place(1f, 1f, -160, -400, 280, 60)
                .AddText("", UiKit.TextTitle, Palette.Paper, TextAnchor.MiddleRight, FontStyle.Bold);

            // Everything that shakes on a miss hangs from one node, so the shake is one number.
            _shake = UiKit.Rect("Shake", overlay).Stretch(0, 0, 1, 1);
            _mortar = UiKit.Rect("Mortar", _shake).Place(0.5f, 0.5f, 0, -60, 520, 520);
            _mortar.pivot = new Vector2(0.5f, 0.2f);
            _mortar.anchoredPosition = new Vector2(0, -60 - 520 * 0.3f);
            Image mortarImage = _mortar.AddImage(catalog.Mortar, Color.white);
            mortarImage.preserveAspect = true;
            if (catalog.Mortar == null) mortarImage.color = Palette.Mix(Palette.Ink, Palette.Paper, 0.3f);
            // The ring shrinks onto the cake between beats; the cake sits in the mortar's bowl. The mortar art
            // carries its own pestle, so a pound is the mortar squashing rather than a second pestle.
            _cake = UiKit.Rect("Cake", _shake).Place(0.5f, 0.5f, 0, -30, 120, 120);
            _cakeImage = _cake.AddImage(catalog.Round, Palette.Lantern);
            _ring = UiKit.Rect("Ring", _shake).Place(0.5f, 0.5f, 0, -30, 120, 120);
            _ring.AddImage(catalog.Round, Palette.Alpha(Palette.Paper, 0.35f));

            _judge = UiKit.Rect("Judge", _shake).Place(0.5f, 0.5f, 0, 220, 500, 70)
                .AddText("", 44, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _judge.AddShadow();
            var chewy = UiKit.Rect("Chewy", _shake).Place(0.5f, 0.5f, 0, -390, 300, 64);
            chewy.AddImage(catalog.Panel, Palette.Lantern, true);
            UiKit.Rect("Text", chewy).Stretch(0, 0, 1, 1).AddText(L10n.T("pound.chewy"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _chewy = chewy.gameObject;
            _chewy.SetActive(false);

            // The baker who pounds along stands beside the mortar.
            var helper = UiKit.Rect("Helper", _shake).Place(0.5f, 0.5f, -330, -120, 220, 230);
            _helperArt = UiKit.Rect("Art", helper).Stretch(0, 0, 1, 1).AddImage(catalog.Rabbit, Color.white);
            _helperArt.preserveAspect = true;
            _helperName = UiKit.Rect("Name", helper).Place(0.5f, 0f, 0, -34, 300, 40)
                .AddText("", UiKit.TextSmall, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _helper = helper.gameObject;
            _helper.SetActive(false);

            var quit = UiKit.Rect("Quit", overlay).Place(0.5f, 0f, 0, 390, 300, 90);
            Image quitImage = quit.AddImage(catalog.Panel, Palette.Alpha(Palette.Paper, 0.2f), true);
            quitImage.raycastTarget = true;
            var quitButton = quit.gameObject.AddComponent<Button>();
            quitButton.transition = Selectable.Transition.None;
            quitButton.targetGraphic = quitImage;
            quitButton.onClick.AddListener(() => { Sfx.Click(); Close(); _onQuit(); });
            UiKit.Rect("Text", quit).Stretch(0, 0, 1, 1).AddText(L10n.T("pound.quit"), UiKit.TextBody, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);

            // The result: stars, the score and what it paid.
            var result = UiKit.Rect("Result", overlay).Stretch(0, 0, 1, 1);
            result.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true; // taps on the garden behind are swallowed
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
            UiKit.Rect("Text", doneButton.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("pound.done"), UiKit.TextTitle, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _result.SetActive(false);

            _root.SetActive(false);
        }

        /// <summary>Starts a game. <paramref name="helperArt"/> is the baker who pounds along, or null.</summary>
        public void Open(PoundingRun run, Sprite helperArt, string helperName)
        {
            Run = run;
            _time = 0d;
            _judgeAge = 9f;
            _pestleDown = 0f;
            _shakeLeft = 0f;
            _resultDelay = 0f;
            _resultShown = false;
            _result.SetActive(false);
            _chewy.SetActive(false);
            _shake.anchoredPosition = Vector2.zero;
            _mortar.localScale = Vector3.one;
            _cakeImage.color = Palette.Lantern;
            _helper.SetActive(helperArt != null);
            if (helperArt != null)
            {
                _helperArt.sprite = helperArt;
                _helperName.text = L10n.T("pound.helper", helperName);
            }
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Draw();
        }

        public void Close()
        {
            _root.SetActive(false);
            Run = null;
        }

        /// <summary>A touch anywhere on the screen: one pound.</summary>
        public void Tap()
        {
            if (Run == null || Run.Finished || _resultShown) return;
            Hit hit = Run.Tap(_time);
            Sfx.Tick();
            _pestleDown = 1f;
            Show(hit);
            Draw();
        }

        /// <summary>The clock. Beats that pass untouched are misses; when the last one has gone the result comes up.</summary>
        public void Update(float dt)
        {
            if (!IsOpen || Run == null) return;
            _time += dt;
            int missed = Run.Advance(_time);
            if (missed > 0) Show(Hit.Miss);
            _judgeAge += dt;
            _pestleDown = Mathf.Max(0f, _pestleDown - dt * 6f);
            _shakeLeft = Mathf.Max(0f, _shakeLeft - dt);

            // The ring closes onto the cake as the next beat comes.
            float ring = 1f;
            if (!Run.Finished)
            {
                double beatAt = Pounding.BeatTime(Run.Beat), prev = Run.Beat == 0 ? 0d : Pounding.BeatTime(Run.Beat - 1);
                float k = Mathf.Clamp01((float)((_time - prev) / Math.Max(0.01d, beatAt - prev)));
                ring = Mathf.Lerp(2.4f, 1f, k);
            }
            _ring.localScale = Vector3.one * ring;
            float pound = _pestleDown > 0f ? Ease.Pop(1f - _pestleDown) : 0f;
            _mortar.localScale = new Vector3(1f + 0.05f * pound, 1f - 0.08f * pound, 1f);
            _shake.anchoredPosition = _shakeLeft > 0f && !Motion.Reduced ? new Vector2(Mathf.Sin(_shakeLeft * 60f) * 14f * _shakeLeft / 0.25f, 0) : Vector2.zero;
            Color judge = _judge.color;
            judge.a = Mathf.Clamp01(1f - (_judgeAge - 0.5f) * 3f);
            _judge.color = judge;

            if (Run.Finished && !_resultShown)
            {
                _resultDelay += dt;
                if (_resultDelay >= 0.6f) ShowResult();
            }
            Draw();
        }

        void Show(Hit hit)
        {
            _judgeAge = 0f;
            _judge.text = L10n.T(hit == Hit.Perfect ? "pound.perfect" : (hit == Hit.Good ? "pound.good" : "pound.miss"));
            _judge.color = hit == Hit.Perfect ? Palette.Lantern : (hit == Hit.Good ? Palette.Paper : Palette.Blush);
            if (hit == Hit.Miss)
            {
                _shakeLeft = 0.25f;
                _cakeImage.color = Palette.Mix(Palette.Lantern, Palette.Ink, 0.35f); // dented
            }
            else _cakeImage.color = Palette.Lantern;
            if (_chewy.activeSelf != Run.Chewy) _chewy.SetActive(Run.Chewy);
        }

        void Draw()
        {
            if (Run == null) return;
            _score.text = L10n.T("pound.score", Run.Score);
            _beats.text = L10n.T("pound.beats", Mathf.Min(Run.Beat, Pounding.Beats), Pounding.Beats);
        }

        void ShowResult()
        {
            _resultShown = true;
            _onFinished(Run);
        }

        /// <summary>Fills the result card once the game has been paid.</summary>
        public void ShowPaid(PoundingResult result)
        {
            _resultTitle.text = L10n.T("pound.result");
            _resultStars.text = new string('★', result.Stars) + new string('☆', 3 - result.Stars);
            _resultScore.text = L10n.T("pound.score", result.Score);
            _resultCarrots.text = L10n.T("pound.reward", result.Carrots.Short());
            string extra = "";
            if (result.HelperJoined) extra += L10n.T("pound.helper.bonus") + "\n";
            if (result.Clovers > 0) extra += L10n.T("pound.clover") + "\n";
            if (result.Golden > 0) extra += L10n.T("pound.golden");
            _resultExtra.text = extra.TrimEnd();
            _result.SetActive(true);
            _resultCard.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_resultCard != null) _resultCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
        }
    }
}

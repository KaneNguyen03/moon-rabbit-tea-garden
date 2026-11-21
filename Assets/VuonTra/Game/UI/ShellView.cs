using System;
using UnityEngine;
using UnityEngine.UI;

namespace VuonTra.Game.UI
{
    public enum ShellState
    {
        Title,
        Playing,
        Paused
    }

    /// <summary>
    /// The screens around play: the title over the live garden and the pause card with volume
    /// sliders. An idle garden never ends, so there is no "ended" screen.
    /// </summary>
    public sealed class ShellView
    {
        const float FadeSeconds = 0.3f;

        public ShellState State { get; private set; } = ShellState.Title;
        /// <summary>Raised after every state change with the new state.</summary>
        public event Action<ShellState> Changed;

        readonly Tweens _tweens;
        readonly GameObject _title, _pause;
        readonly CanvasGroup _titleGroup, _pauseGroup;
        readonly RectTransform _pauseCard;
        readonly Text _prompt, _resetLabel;
        readonly Action _onReset;
        float _resetArmedAt = -99f;

        public ShellView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onReset)
        {
            _tweens = tweens;
            _onReset = onReset;

            // ----- Title: the live garden is the backdrop -----
            var title = UiKit.Rect("Title", canvas).Stretch(0, 0, 1, 1);
            _title = title.gameObject;
            _titleGroup = _title.AddComponent<CanvasGroup>();
            Image hit = title.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            // A soft dusk over the open ground where the drawer will rise, so the name reads.
            UiKit.Rect("Dusk", title).Stretch(0.12f, 0.50f, 0.88f, 1.08f).AddImage(catalog.Glow, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.4f), 0.85f));
            var start = _title.AddComponent<Button>();
            start.transition = Selectable.Transition.None;
            start.onClick.AddListener(StartGame);

            Text name = UiKit.Rect("Name", title).Place(0.5f, 0.80f, 0, 0, 1600, 190)
                .AddText("Vườn Trà Thỏ Trăng", 128, Palette.Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.lineSpacing = 0.9f;
            name.AddShadow(0.85f, 8f);
            UiKit.Rect("Tagline", title).Place(0.5f, 0.80f, 0, -112, 1200, 60)
                .AddText("Tiệm trà nhỏ của những bé thỏ dưới trăng", 34, Palette.Alpha(Palette.Paper, 0.9f)).AddShadow();
            _prompt = UiKit.Rect("Prompt", title).Place(0.5f, 0.61f, 0, 0, 900, 60)
                .AddText("C H Ạ M   Đ Ể   B Ắ T   Đ Ầ U", 32, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.AddShadow();

            // ----- Pause and settings -----
            var pause = UiKit.Rect("Pause", canvas).Stretch(0, 0, 1, 1);
            _pause = pause.gameObject;
            _pauseGroup = _pause.AddComponent<CanvasGroup>();
            pause.AddImage(null, Palette.Scrim).raycastTarget = true;

            _pauseCard = UiKit.Rect("Card", pause).Place(0.5f, 0.52f, 0, 0, 820, 860);
            UiKit.Rect("Lip", _pauseCard).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _pauseCard).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Heading", _pauseCard).Place(0.5f, 1f, 0, -100, 700, 80)
                .AddText("Tạm nghỉ", 60, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            AddVolumeRow(catalog, "Music", "Nhạc nền", -250, Sfx.MusicGain, Sfx.SetMusic);
            AddVolumeRow(catalog, "Effects", "Âm thanh", -400, Sfx.SfxGain, v =>
            {
                Sfx.SetSfx(v);
                Sfx.Click();
            });

            var resume = UiKit.Rect("Resume", _pauseCard).Place(0.5f, 0f, 0, 260, 620, 130);
            ChunkyButton resumeButton = null;
            resumeButton = UiKit.Chunky(resume, catalog.Panel, () =>
            {
                resumeButton.Press(_tweens);
                Resume();
            });
            UiKit.Rect("Text", resumeButton.FaceRect).Stretch(0, 0, 1, 1)
                .AddText("Chơi tiếp", 42, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            var reset = UiKit.Rect("Reset", _pauseCard).Place(0.5f, 0f, 0, 120, 700, 80);
            Image resetHit = reset.AddImage(null, new Color(0, 0, 0, 0));
            resetHit.raycastTarget = true;
            var resetButton = reset.gameObject.AddComponent<Button>();
            resetButton.transition = Selectable.Transition.None;
            resetButton.onClick.AddListener(OnResetPressed);
            _resetLabel = UiKit.Rect("Text", reset).Stretch(0, 0, 1, 1).AddText("Chơi lại từ đầu", 30, Palette.InkSoft);

            _pause.SetActive(false);
        }

        void AddVolumeRow(GameCatalog catalog, string id, string label, float y, float value, Action<float> onChange)
        {
            UiKit.Rect(id + " Label", _pauseCard).Place(0.5f, 1f, -200, y, 300, 60)
                .AddText(label, 34, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var slider = UiKit.Rect(id, _pauseCard).Place(0.5f, 1f, 130, y, 420, 60);
            UiKit.AddSlider(slider, catalog.Panel, catalog.Round, value, onChange);
        }

        public void StartGame()
        {
            if (State != ShellState.Title) return;
            Sfx.Blip();
            SetState(ShellState.Playing);
            _titleGroup.blocksRaycasts = false;
            _tweens.Add(FadeSeconds, t => _titleGroup.alpha = 1f - t, Ease.OutCubic, done: () => _title.SetActive(false));
        }

        public void OpenPause()
        {
            if (State != ShellState.Playing) return;
            Sfx.Click();
            SetState(ShellState.Paused);
            _resetArmedAt = -99f;
            _resetLabel.text = "Chơi lại từ đầu";
            _resetLabel.color = Palette.InkSoft;
            _pause.SetActive(true);
            _pauseGroup.alpha = 0f;
            _pauseGroup.blocksRaycasts = true;
            _pauseCard.localScale = Vector3.one * 0.8f;
            _tweens.Add(FadeSeconds, t => _pauseGroup.alpha = t, Ease.OutCubic);
            _tweens.Add(FadeSeconds, t => _pauseCard.localScale = Vector3.one * Mathf.LerpUnclamped(0.8f, 1f, t), Ease.OutBack);
        }

        public void Resume()
        {
            if (State != ShellState.Paused) return;
            Sfx.Click();
            SetState(ShellState.Playing);
            _pauseGroup.blocksRaycasts = false;
            _tweens.Add(FadeSeconds, t => _pauseGroup.alpha = 1f - t, Ease.OutCubic, done: () =>
            {
                if (State != ShellState.Paused) _pause.SetActive(false);
            });
        }

        /// <summary>Wiping progress takes two presses within three seconds.</summary>
        void OnResetPressed()
        {
            if (Time.unscaledTime - _resetArmedAt <= 3f)
            {
                _resetArmedAt = -99f;
                _onReset();
                Resume();
                return;
            }
            _resetArmedAt = Time.unscaledTime;
            _resetLabel.text = "Chạm lần nữa để xóa toàn bộ tiến trình";
            _resetLabel.color = Palette.CarrotLip;
        }

        void SetState(ShellState next)
        {
            State = next;
            Changed?.Invoke(next);
        }

        public void Update(float time)
        {
            if (State == ShellState.Title)
            {
                // Motion is what marks the prompt as interactive.
                Color c = _prompt.color;
                c.a = 0.75f + 0.25f * Mathf.Sin(time * (2f * Mathf.PI / 1.6f));
                _prompt.color = c;
            }
            else if (State == ShellState.Paused && _resetArmedAt > 0f && Time.unscaledTime - _resetArmedAt > 3f)
            {
                _resetArmedAt = -99f;
                _resetLabel.text = "Chơi lại từ đầu";
                _resetLabel.color = Palette.InkSoft;
            }
        }
    }
}

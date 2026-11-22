using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;
using VuonTra.Game.Platform;

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
        readonly Text _prompt, _resetLabel, _profileName, _profileCode, _newMoonLocked;
        readonly Image _profileArt;
        readonly Action _onReset;
        readonly GameObject _newMoon;
        readonly Func<bool> _newMoonReady, _newMoonCapped;
        readonly Func<int> _newMoonLevel;
        readonly Func<int> _mailCount;
        readonly GameObject _mailBadge;
        readonly Text _mailBadgeText;
        float _resetArmedAt = -99f;

        public ShellView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onReset, Action onProfile = null,
            Func<bool> newMoonReady = null, Func<int> newMoonLevel = null, Action onNewMoon = null, Func<bool> newMoonCapped = null,
            Action onMail = null, Func<int> mailCount = null)
        {
            _mailCount = mailCount;
            _tweens = tweens;
            _onReset = onReset;
            _newMoonReady = newMoonReady;
            _newMoonLevel = newMoonLevel;
            _newMoonCapped = newMoonCapped;

            // ----- Title: the live garden is the backdrop -----
            var title = UiKit.Rect("Title", canvas).Stretch(0, 0, 1, 1);
            _title = title.gameObject;
            _titleGroup = _title.AddComponent<CanvasGroup>();
            Image hit = title.AddImage(null, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            // A soft dusk over the open ground where the drawer will rise, so the name reads.
            UiKit.Rect("Dusk", title).Stretch(-0.3f, 0.72f, 1.3f, 1.06f).AddImage(catalog.Glow, Palette.Alpha(Palette.Mix(Palette.Night, Palette.Ink, 0.4f), 0.85f));
            var start = _title.AddComponent<Button>();
            start.transition = Selectable.Transition.None;
            start.onClick.AddListener(StartGame);

            Text name = UiKit.Rect("Name", title).Place(0.5f, 0.895f, 0, 0, 980, 280)
                .AddText(L10n.T("title.name"), 100, Palette.Lantern, TextAnchor.MiddleCenter, FontStyle.Bold);
            name.lineSpacing = 0.9f;
            name.AddShadow(0.85f, 8f);
            UiKit.Rect("Tagline", title).Place(0.5f, 0.895f, 0, -168, 1000, 60)
                .AddText(L10n.T("title.tagline"), UiKit.TextTitle, Palette.Alpha(Palette.Paper, 0.9f)).AddShadow();
            _prompt = UiKit.Rect("Prompt", title).Place(0.5f, 0.17f, 0, 0, 900, 60)
                .AddText(L10n.T("title.prompt"), UiKit.TextTitle, Palette.Paper, TextAnchor.MiddleCenter, FontStyle.Bold);
            _prompt.AddShadow();

            // ----- Pause and settings -----
            var pause = UiKit.Rect("Pause", canvas).Stretch(0, 0, 1, 1);
            _pause = pause.gameObject;
            _pauseGroup = _pause.AddComponent<CanvasGroup>();
            pause.AddImage(null, Palette.Scrim).raycastTarget = true;

            _pauseCard = UiKit.Rect("Card", pause).Place(0.5f, 0.5f, 0, 0, 820, 1720);
            UiKit.Rect("Lip", _pauseCard).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _pauseCard).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true);
            UiKit.Rect("Heading", _pauseCard).Place(0.5f, 1f, 0, -100, 440, 80)
                .AddText(L10n.T("pause.title"), 60, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            // The mailbox (#68): gifts the game wrote itself, in the card's top corner; a badge counts the letters waiting.
            var mail = UiKit.Rect("Mail", _pauseCard).Place(0f, 1f, 115, -100, 150, UiKit.MinTouch);
            Image mailPlate = mail.AddImage(catalog.Panel, Palette.Track, true);
            mailPlate.raycastTarget = true;
            var mailButton = mail.gameObject.AddComponent<Button>();
            mailButton.transition = Selectable.Transition.None;
            mailButton.targetGraphic = mailPlate;
            mailButton.onClick.AddListener(() => { Sfx.Click(); onMail?.Invoke(); });
            Text mailLabel = UiKit.Rect("Text", mail).Stretch(0, 0, 1, 1, 10, 0, 10, 0)
                .AddText(L10n.T("pause.mail"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            mailLabel.resizeTextForBestFit = true;
            mailLabel.resizeTextMinSize = 14;
            mailLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            var mailBadge = UiKit.Rect("Badge", mail).Place(1f, 1f, -4, -4, 44, 44);
            mailBadge.AddImage(catalog.Round, Palette.Carrot);
            _mailBadgeText = UiKit.Rect("Text", mailBadge).Stretch(0, 0, 1, 1).AddText("", UiKit.TextSmall, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _mailBadgeText.resizeTextForBestFit = true;
            _mailBadgeText.resizeTextMinSize = 12;
            _mailBadgeText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _mailBadge = mailBadge.gameObject;
            RefreshMail();

            // Who is playing: the avatar rabbit, the garden's name and the player code; a touch opens the profile.
            var profile = UiKit.Rect("Profile", _pauseCard).Place(0.5f, 1f, 0, -210, 700, 110);
            Image profilePlate = profile.AddImage(catalog.Panel, Palette.Track, true);
            profilePlate.raycastTarget = true;
            var profileButton = profile.gameObject.AddComponent<Button>();
            profileButton.transition = Selectable.Transition.None;
            profileButton.targetGraphic = profilePlate;
            profileButton.onClick.AddListener(() => { Sfx.Click(); onProfile?.Invoke(); });
            var avatar = UiKit.Rect("Avatar", profile).Place(0f, 0.5f, 62, 0, 88, 88);
            avatar.AddImage(catalog.Round, Palette.Paper);
            _profileArt = UiKit.Rect("Art", avatar).Stretch(0, 0, 1, 1, 8, 8, 8, 8).AddImage(catalog.Rabbit, Color.white);
            _profileArt.preserveAspect = true;
            _profileName = UiKit.Rect("Name", profile).Stretch(0, 0.5f, 1, 1, 124, 0, 70, 10)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            _profileName.horizontalOverflow = HorizontalWrapMode.Overflow;
            _profileCode = UiKit.Rect("Code", profile).Stretch(0, 0, 1, 0.5f, 124, 12, 70, 0)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperLeft);
            UiKit.Rect("Chevron", profile).Place(1f, 0.5f, -40, 0, 40, 60).AddText("›", 44, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);

            AddVolumeRow(catalog, "Music", L10n.T("pause.music"), -350, Sfx.MusicGain, Sfx.SetMusic);
            AddVolumeRow(catalog, "Effects", L10n.T("pause.sfx"), -460, Sfx.SfxGain, v =>
            {
                Sfx.SetSfx(v);
                Sfx.Click();
            });

            // Language: two pills; the package does the switching and the screen rebuilds itself.
            UiKit.Rect("Language Label", _pauseCard).Place(0.5f, 1f, -200, -570, 300, 60)
                .AddText(L10n.T("pause.language"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            UiKit.Rect("Language Track", _pauseCard).Place(0.5f, 1f, 130, -570, 440, 76).AddImage(catalog.Panel, Palette.Track, true);
            AddLanguageButton(catalog, "vi", 22);
            AddLanguageButton(catalog, "en", 238);

            // For players who get dizzy: slides, glides and bounces are skipped.
            AddToggleRow(catalog, "Motion", L10n.T("pause.motion"), -680, () => Motion.Reduced, Motion.SetReduced);
            // Short buzzes on a phone for upgrades, milestones and new rabbits.
            AddToggleRow(catalog, "Haptics", L10n.T("pause.haptics"), -780, () => Haptics.On, Haptics.SetOn);
            // Every label a fifth bigger; the screen builds itself again, as for a language change.
            AddToggleRow(catalog, "Text", L10n.T("pause.text"), -880, () => UiKit.LargeText, UiKit.SetLargeText);
            // Reminders on the phone (#65): off until the player turns them on, which is when permission is asked.
            Action showRemind = AddToggleRow(catalog, "Remind", L10n.T("pause.remind"), -980, () => Notifications.On, Notifications.SetOn);
            Notifications.Watch(_pauseCard.gameObject, showRemind);
            // Station sounds up close (#63): a slider of their own, apart from the blips; and which tune plays.
            AddVolumeRow(catalog, "Station", L10n.T("pause.station"), -1080, Sfx.StationGain, Sfx.SetStation);
            AddTuneRow(catalog, -1180);

            // The new moon (#1): a warm button with a badge once the garden is far enough; until then one line says the level it asks for.
            var newMoon = UiKit.Rect("NewMoon", _pauseCard).Place(0.5f, 0f, 0, 420, 700, 100);
            _newMoon = newMoon.gameObject;
            ChunkyButton newMoonButton = null;
            newMoonButton = UiKit.Chunky(newMoon, catalog.Panel, () =>
            {
                newMoonButton.Press(_tweens);
                Sfx.Click();
                onNewMoon?.Invoke();
            });
            newMoonButton.Face.color = Palette.Lantern;
            newMoonButton.Lip.color = Palette.Mix(Palette.Lantern, Palette.Ink, 0.3f);
            UiKit.Rect("Text", newMoonButton.FaceRect).Stretch(0, 0, 1, 1, 24, 0, 150, 0)
                .AddText(L10n.T("pause.newmoon"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var badge = UiKit.Rect("Badge", newMoonButton.FaceRect).Place(1f, 0.5f, -70, 0, 120, 48);
            badge.AddImage(catalog.Panel, Palette.Carrot, true);
            Text badgeText = UiKit.Rect("Text", badge).Stretch(0, 0, 1, 1, 6, 0, 6, 0).AddText(L10n.T("pause.newmoon.ready"), UiKit.TextSmall, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            badgeText.resizeTextForBestFit = true;
            badgeText.resizeTextMinSize = 14;
            badgeText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _newMoonLocked = UiKit.Rect("NewMoonLocked", _pauseCard).Place(0.5f, 0f, 0, 420, 700, 100)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            _newMoonLocked.resizeTextForBestFit = true;
            _newMoonLocked.resizeTextMinSize = 16;
            _newMoonLocked.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            ShowNewMoon();

            var resume = UiKit.Rect("Resume", _pauseCard).Place(0.5f, 0f, 0, 260, 620, 130);
            ChunkyButton resumeButton = null;
            resumeButton = UiKit.Chunky(resume, catalog.Panel, () =>
            {
                resumeButton.Press(_tweens);
                Resume();
            });
            UiKit.Rect("Text", resumeButton.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(L10n.T("pause.resume"), 42, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);

            var reset = UiKit.Rect("Reset", _pauseCard).Place(0.5f, 0f, 0, 120, 700, UiKit.MinTouch);
            Image resetHit = reset.AddImage(null, new Color(0, 0, 0, 0));
            resetHit.raycastTarget = true;
            var resetButton = reset.gameObject.AddComponent<Button>();
            resetButton.transition = Selectable.Transition.None;
            resetButton.onClick.AddListener(OnResetPressed);
            _resetLabel = UiKit.Rect("Text", reset).Stretch(0, 0, 1, 1).AddText(L10n.T("pause.reset"), UiKit.TextTitle, Palette.InkSoft);

            _pause.SetActive(false);
        }

        /// <summary>The mailbox's badge: how many letters wait, hidden when none do.</summary>
        public void RefreshMail()
        {
            int n = _mailCount != null ? _mailCount() : 0;
            if (_mailBadge.activeSelf != n > 0) _mailBadge.SetActive(n > 0);
            _mailBadgeText.text = n.ToString();
        }

        /// <summary>The new-moon row as the garden stands: the button, or the line that says what it still needs.</summary>
        void ShowNewMoon()
        {
            bool ready = _newMoonReady != null && _newMoonReady();
            _newMoon.SetActive(ready);
            _newMoonLocked.gameObject.SetActive(!ready);
            if (ready) return;
            bool capped = _newMoonCapped != null && _newMoonCapped();
            _newMoonLocked.text = capped ? L10n.T("pause.newmoon.cap") : L10n.T("pause.newmoon.locked", _newMoonLevel != null ? _newMoonLevel() : NewMoon.FirstLevel);
        }

        /// <summary>A setting that is either on or off: its name on the left, a pill that says which on the right.</summary>
        Action AddToggleRow(GameCatalog catalog, string id, string label, float y, Func<bool> get, Action<bool> set)
        {
            UiKit.Rect(id + " Label", _pauseCard).Place(0.5f, 1f, -120, y, 460, 60)
                .AddText(label, UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var pill = UiKit.Rect(id, _pauseCard).Place(0.5f, 1f, 262, y, 176, UiKit.MinTouch);
            Image plate = pill.AddImage(catalog.Panel, Palette.Track, true);
            plate.raycastTarget = true;
            Text text = UiKit.Rect("Text", pill).Stretch(0, 0, 1, 1).AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            Action show = () =>
            {
                bool on = get();
                plate.color = on ? Palette.Carrot : Palette.Track;
                text.color = on ? Palette.OnCarrot : Palette.InkSoft;
                text.text = L10n.T(on ? "toggle.on" : "toggle.off");
            };
            show();
            var button = pill.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = plate;
            button.onClick.AddListener(() =>
            {
                Sfx.Click();
                set(!get());
                show();
            });
            return show;
        }

        /// <summary>Which tune plays: one pill that steps Tự động → Đêm trăng → Rạng sáng → Mưa and round again.</summary>
        void AddTuneRow(GameCatalog catalog, float y)
        {
            UiKit.Rect("Tune Label", _pauseCard).Place(0.5f, 1f, -200, y, 300, 60)
                .AddText(L10n.T("pause.tune"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var pill = UiKit.Rect("Tune", _pauseCard).Place(0.5f, 1f, 130, y, 420, UiKit.MinTouch);
            Image plate = pill.AddImage(catalog.Panel, Palette.Track, true);
            plate.raycastTarget = true;
            Text text = UiKit.Rect("Text", pill).Stretch(0, 0, 1, 1, 48, 0, 48, 0)
                .AddText(TuneName(Sfx.CurrentTune), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            UiKit.Rect("Next", pill).Place(1f, 0.5f, -32, 0, 40, 60).AddText("›", 44, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            var button = pill.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = plate;
            button.onClick.AddListener(() =>
            {
                Sfx.Click();
                Sfx.SetTune((Sfx.Tune)(((int)Sfx.CurrentTune + 1) % 4));
                text.text = TuneName(Sfx.CurrentTune);
            });
        }

        static string TuneName(Sfx.Tune tune) => L10n.T("tune." + tune.ToString().ToLowerInvariant());

        void AddLanguageButton(GameCatalog catalog, string code, float x)
        {
            bool current = L10n.Code == code;
            var rt = UiKit.Rect("Language " + code, _pauseCard).Place(0.5f, 1f, x, -570, 208, 60);
            UiKit.Touchable(rt, 208, 60);
            Image img = rt.AddImage(catalog.Panel, current ? Palette.Carrot : Palette.Alpha(Palette.Carrot, 0f), true);
            img.raycastTarget = true;
            var button = rt.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.targetGraphic = img;
            button.onClick.AddListener(() =>
            {
                if (L10n.Code == code) return;
                Sfx.Click();
                L10n.SetLocale(code);
            });
            UiKit.Rect("Text", rt).Stretch(0, 0, 1, 1)
                .AddText(L10n.T("lang." + code), UiKit.TextBody, current ? Palette.OnCarrot : Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <summary>Fills the profile row on the pause card.</summary>
        public void SetProfile(string name, string code, Sprite avatar)
        {
            _profileName.text = name;
            _profileCode.text = code;
            if (avatar != null) _profileArt.sprite = avatar;
        }

        /// <summary>Goes straight to play with no fade or sound (the screen was rebuilt mid-game).</summary>
        public void SkipTitle()
        {
            if (State != ShellState.Title) return;
            _titleGroup.blocksRaycasts = false;
            _title.SetActive(false);
            SetState(ShellState.Playing);
        }

        void AddVolumeRow(GameCatalog catalog, string id, string label, float y, float value, Action<float> onChange)
        {
            UiKit.Rect(id + " Label", _pauseCard).Place(0.5f, 1f, -200, y, 300, 60)
                .AddText(label, UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
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
            _resetLabel.text = L10n.T("pause.reset");
            _resetLabel.color = Palette.InkSoft;
            ShowNewMoon();
            RefreshMail();
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
            _resetLabel.text = L10n.T("pause.reset.confirm");
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
                _resetLabel.text = L10n.T("pause.reset");
                _resetLabel.color = Palette.InkSoft;
            }
        }
    }
}

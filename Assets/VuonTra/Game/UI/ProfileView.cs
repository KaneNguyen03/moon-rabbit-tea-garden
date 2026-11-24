using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class ProfileView
    {
        public static Action<string> Clipboard = text => GUIUtility.systemCopyBuffer = text;

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Image _avatar;
        readonly Text _name, _code, _stats, _backup, _copyLabel, _linkNote, _cloudStatus, _restoreLocalLabel, _deleteLabel;
        int _deletePresses;
        float _deletePressedAt = -99f;
        readonly ChunkyButton _link, _push, _restore;
        readonly GameObject _restoreLocal;
        readonly Action _onNextAvatar, _onRename;
        readonly Func<string, GiftCodes.Result> _onRedeem;
        readonly RectTransform _giftField;
        readonly InputField _giftInput;
        readonly Text _giftNote;
        readonly Text _introsCount;
        Vector2 _giftHome;
        float _copiedAt = -99f;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Root => _card;

        public ProfileView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onNextAvatar, Action onRename, Action onLink,
            Action onPushCloud = null, Action onRestoreCloud = null, Action onRestoreLocal = null, Action onDeleteData = null,
            Func<string, GiftCodes.Result> onRedeem = null, Action onIntros = null)
        {
            _onRedeem = onRedeem;
            _catalog = catalog;
            _tweens = tweens;
            _onNextAvatar = onNextAvatar;
            _onRename = onRename;

            var overlay = UiKit.Rect("Profile", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 860, 1680);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -80, 600, 70)
                .AddText(L10n.T("profile.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var avatar = UiKit.Rect("Avatar", _card).Place(0.5f, 1f, 0, -270, 240, 240);
            Image frame = avatar.AddImage(catalog.Round, Palette.Lantern);
            frame.raycastTarget = true;
            var avatarButton = avatar.gameObject.AddComponent<Button>();
            avatarButton.transition = Selectable.Transition.None;
            avatarButton.targetGraphic = frame;
            avatarButton.onClick.AddListener(() => { Sfx.Click(); _onNextAvatar(); });
            UiKit.Rect("Inner", avatar).Stretch(0, 0, 1, 1, 10, 10, 10, 10).AddImage(catalog.Round, Palette.Card);
            _avatar = UiKit.Rect("Art", avatar).Stretch(0, 0, 1, 1, 24, 18, 24, 30).AddImage(catalog.Rabbit, Color.white);
            _avatar.preserveAspect = true;
            var intros = UiKit.Rect("Intros", _card).Place(0f, 1f, 160, -250, 230, 110);
            Image introsPlate = intros.AddImage(catalog.Panel, Palette.Track, true);
            introsPlate.raycastTarget = true;
            var introsButton = intros.gameObject.AddComponent<Button>();
            introsButton.transition = Selectable.Transition.None;
            introsButton.targetGraphic = introsPlate;
            introsButton.onClick.AddListener(() => { Sfx.Click(); onIntros?.Invoke(); });
            Text introsLabel = UiKit.Rect("Text", intros).Stretch(0, 0.45f, 1, 1, 12, 0, 12, 8)
                .AddText(L10n.T("profile.intros"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            introsLabel.resizeTextForBestFit = true;
            introsLabel.resizeTextMinSize = 14;
            introsLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _introsCount = UiKit.Rect("Count", intros).Stretch(0, 0, 1, 0.5f, 12, 10, 12, 0)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _introsCount.resizeTextForBestFit = true;
            _introsCount.resizeTextMinSize = 12;
            _introsCount.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            UiKit.Rect("AvatarHint", _card).Place(0.5f, 1f, 0, -416, 700, 40)
                .AddText(L10n.T("profile.avatar.hint"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);

            _name = UiKit.Rect("Name", _card).Place(0.5f, 1f, -40, -490, 600, 70)
                .AddText("", 44, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _name.resizeTextForBestFit = true;
            _name.resizeTextMinSize = 24;
            _name.resizeTextMaxSize = 44;
            var rename = UiKit.Rect("Rename", _card).Place(0.5f, 1f, 310, -490, UiKit.MinTouch, UiKit.MinTouch);
            Image renamePlate = rename.AddImage(catalog.Round, Palette.Track);
            renamePlate.raycastTarget = true;
            var renameButton = rename.gameObject.AddComponent<Button>();
            renameButton.transition = Selectable.Transition.None;
            renameButton.targetGraphic = renamePlate;
            renameButton.onClick.AddListener(() => { Sfx.Click(); _onRename(); });
            UiKit.Rect("Text", rename).Stretch(0, 0, 1, 1).AddText("✎", 40, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _stats = UiKit.Rect("Stats", _card).Place(0.5f, 1f, 0, -560, 760, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);

            var codeRow = UiKit.Rect("CodeRow", _card).Place(0.5f, 1f, 0, -660, 740, 100);
            codeRow.AddImage(catalog.Panel, Palette.Track, true);
            UiKit.Rect("Label", codeRow).Stretch(0, 0.5f, 0.62f, 1, 32, 0, 0, 8)
                .AddText(L10n.T("profile.code"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.LowerLeft);
            _code = UiKit.Rect("Code", _card).Place(0.5f, 1f, -140, -676, 420, 50)
                .AddText("", 38, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var copy = UiKit.Rect("Copy", _card).Place(0.5f, 1f, 250, -660, 200, UiKit.MinTouch);
            ChunkyButton copyButton = null;
            copyButton = UiKit.Chunky(copy, catalog.Panel, () =>
            {
                copyButton.Press(_tweens);
                Sfx.Blip();
                Clipboard(_code.text);
                _copiedAt = Time.unscaledTime;
                _copyLabel.text = L10n.T("profile.copied");
            });
            _copyLabel = UiKit.Rect("Text", copyButton.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(L10n.T("profile.copy"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);

            _giftField = UiKit.Rect("GiftCode", _card).Place(0.5f, 1f, -120, -770, 500, UiKit.MinTouch);
            _giftHome = _giftField.anchoredPosition;
            Image giftPlate = _giftField.AddImage(catalog.Panel, Palette.Card, true);
            giftPlate.raycastTarget = true;
            Text giftText = UiKit.Rect("Text", _giftField).Stretch(0, 0, 1, 1, 28, 0, 28, 0)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            giftText.supportRichText = false;
            Text giftHint = UiKit.Rect("Hint", _giftField).Stretch(0, 0, 1, 1, 28, 0, 28, 0)
                .AddText(L10n.T("gift.hint"), UiKit.TextBody, Palette.OffInk, TextAnchor.MiddleLeft);
            giftHint.resizeTextForBestFit = true;
            giftHint.resizeTextMinSize = 14;
            giftHint.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _giftInput = _giftField.gameObject.AddComponent<InputField>();
            _giftInput.targetGraphic = giftPlate;
            _giftInput.textComponent = giftText;
            _giftInput.placeholder = giftHint;
            _giftInput.lineType = InputField.LineType.SingleLine;
            _giftInput.characterLimit = 24;
            _giftInput.onEndEdit.AddListener(_ => { if (Application.isPlaying && SubmitPressed()) Redeem(); });
            var redeem = UiKit.Rect("Redeem", _card).Place(0.5f, 1f, 250, -770, 200, UiKit.MinTouch);
            ChunkyButton redeemButton = null;
            redeemButton = UiKit.Chunky(redeem, catalog.Panel, () =>
            {
                redeemButton.Press(_tweens);
                Redeem();
            });
            Text redeemLabel = UiKit.Rect("Text", redeemButton.FaceRect).Stretch(0, 0, 1, 1, 8, 0, 8, 0)
                .AddText(L10n.T("gift.redeem"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            redeemLabel.resizeTextForBestFit = true;
            redeemLabel.resizeTextMinSize = 14;
            redeemLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextBody);
            _giftNote = UiKit.Rect("GiftNote", _card).Place(0.5f, 1f, 0, -836, 740, 40)
                .AddText("", UiKit.TextSmall, Palette.CarrotLip, TextAnchor.MiddleLeft, FontStyle.Bold);
            _giftNote.resizeTextForBestFit = true;
            _giftNote.resizeTextMinSize = 12;
            _giftNote.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            UiKit.Rect("BackupTitle", _card).Place(0.5f, 1f, 0, -890, 740, 44)
                .AddText(L10n.T("backup.title"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _backup = UiKit.Rect("Backup", _card).Place(0.5f, 1f, 0, -936, 740, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            var link = UiKit.Rect("Link", _card).Place(0.5f, 1f, 0, -1030, 620, 110);
            _link = UiKit.Chunky(link, catalog.Panel, () => { Sfx.Blip(); onLink(); });
            UiKit.Rect("Text", _link.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(L10n.T("backup.link"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            _linkNote = UiKit.Rect("LinkNote", _card).Place(0.5f, 1f, 0, -1110, 740, 60)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);

            _cloudStatus = UiKit.Rect("CloudStatus", _card).Place(0.5f, 1f, 0, -1180, 740, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            var push = UiKit.Rect("CloudPush", _card).Place(0.5f, 1f, -170, -1260, 320, 100);
            _push = UiKit.Chunky(push, catalog.Panel, () => { Sfx.Blip(); onPushCloud?.Invoke(); });
            UiKit.Rect("Text", _push.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("cloud.push"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            var restore = UiKit.Rect("CloudRestore", _card).Place(0.5f, 1f, 170, -1260, 320, 100);
            _restore = UiKit.Chunky(restore, catalog.Panel, () => { Sfx.Blip(); onRestoreCloud?.Invoke(); });
            UiKit.Rect("Text", _restore.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("cloud.restore"), UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            var restoreLocal = UiKit.Rect("RestoreLocal", _card).Place(0.5f, 1f, 0, -1350, 740, UiKit.MinTouch);
            Image restoreHit = restoreLocal.AddImage(null, new Color(0, 0, 0, 0));
            restoreHit.raycastTarget = true;
            var restoreLocalButton = restoreLocal.gameObject.AddComponent<Button>();
            restoreLocalButton.transition = Selectable.Transition.None;
            restoreLocalButton.onClick.AddListener(() => { Sfx.Click(); onRestoreLocal?.Invoke(); });
            _restoreLocalLabel = UiKit.Rect("Text", restoreLocal).Stretch(0, 0, 1, 1).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _restoreLocal = restoreLocal.gameObject;
            _restoreLocal.SetActive(false);

            UiKit.Rect("PrivacyTitle", _card).Place(0.5f, 1f, 0, -1420, 740, 44)
                .AddText(L10n.T("privacy.title"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            var privacy = UiKit.Rect("Privacy", _card).Place(0.5f, 1f, 0, -1472, 740, UiKit.MinTouch);
            Image privacyHit = privacy.AddImage(null, new Color(0, 0, 0, 0));
            privacyHit.raycastTarget = true;
            var privacyButton = privacy.gameObject.AddComponent<Button>();
            privacyButton.transition = Selectable.Transition.None;
            privacyButton.onClick.AddListener(() => { Sfx.Click(); OpenUrl(AppInfo.PrivacyUrl); });
            UiKit.Rect("Text", privacy).Stretch(0, 0, 1, 1).AddText(L10n.T("privacy.open"), UiKit.TextBody, Palette.Carrot, TextAnchor.MiddleLeft, FontStyle.Bold);
            Text support = UiKit.Rect("Support", _card).Place(0.5f, 1f, 0, -1530, 740, 44)
                .AddText(L10n.T("privacy.support", AppInfo.SupportEmail), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleLeft);
            support.resizeTextForBestFit = true;
            support.resizeTextMinSize = 12;
            support.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            support.verticalOverflow = VerticalWrapMode.Truncate;
            var delete = UiKit.Rect("DeleteData", _card).Place(0.5f, 1f, 0, -1600, 740, UiKit.MinTouch);
            Image deleteHit = delete.AddImage(null, new Color(0, 0, 0, 0));
            deleteHit.raycastTarget = true;
            var deleteButton = delete.gameObject.AddComponent<Button>();
            deleteButton.transition = Selectable.Transition.None;
            deleteButton.onClick.AddListener(() =>
            {
                Sfx.Click();
                _deletePresses++;
                _deletePressedAt = Time.unscaledTime;
                if (_deletePresses >= 3)
                {
                    _deletePresses = 0;
                    onDeleteData?.Invoke();
                    return;
                }
                _deleteLabel.text = L10n.T(_deletePresses == 1 ? "privacy.delete.once" : "privacy.delete.twice");
            });
            _deleteLabel = UiKit.Rect("Text", delete).Stretch(0, 0, 1, 1).AddText(L10n.T("privacy.delete"), UiKit.TextSmall, Palette.Tiers[5], TextAnchor.MiddleLeft, FontStyle.Bold);
            _deleteLabel.resizeTextForBestFit = true;
            _deleteLabel.resizeTextMinSize = 12;
            _deleteLabel.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);
            _deleteLabel.verticalOverflow = VerticalWrapMode.Truncate;

            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            _root.SetActive(false);
        }

        public static string NameOf(GameState s) => string.IsNullOrEmpty(s.Profile.GardenName) ? L10n.T("profile.unnamed") : s.Profile.GardenName;

        public static string Clock(long unixSeconds)
        {
            if (unixSeconds <= 0) return "—";
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString("HH:mm dd/MM", System.Globalization.CultureInfo.InvariantCulture);
        }

        public static string ProviderName(AccountProvider provider)
        {
            switch (provider)
            {
                case AccountProvider.GameCenter: return L10n.T("provider.gamecenter");
                case AccountProvider.PlayGames: return L10n.T("provider.playgames");
                case AccountProvider.Apple: return L10n.T("provider.apple");
                case AccountProvider.Google: return L10n.T("provider.google");
                default: return "";
            }
        }

        public static string BackupLine(IAccountService account)
        {
            switch (account.State)
            {
                case BackupState.Linked: return L10n.T("backup.linked", account.LinkedName);
                case BackupState.Linking: return L10n.T("backup.linking", ProviderName(account.Provider));
                case BackupState.Failed: return L10n.T("backup.failed", ProviderName(account.Provider));
                default: return L10n.T("backup.local");
            }
        }

        public static string CloudLine(CloudStatus cloud)
        {
            if (!cloud.Available) return L10n.T(cloud.SignedOut ? "cloud.status.signin" : "cloud.status.off");
            SyncOutcome? last = cloud.Last?.Outcome;
            if (last == SyncOutcome.NeedsChoice) return L10n.T("cloud.status.choice");
            if (last == SyncOutcome.Offline) return L10n.T("cloud.status.offline");
            if (last == SyncOutcome.TooLarge) return L10n.T("cloud.status.toolarge");
            if (last == SyncOutcome.NewerVersion) return L10n.T("cloud.status.newer");
            if (cloud.Last != null && cloud.Last.RemoteWasCorrupt && cloud.LastPushedAt < 0) return L10n.T("cloud.status.corrupt");
            return cloud.LastPushedAt > 0 ? L10n.T("cloud.status.at", Clock(cloud.LastPushedAt)) : L10n.T("cloud.status.never");
        }

        public static Action<string> OpenUrl = url => Application.OpenURL(url);

        public void Open(IReadOnlyList<StationDef> defs, GameState s, IAccountService account, CloudStatus cloud = default)
        {
            _giftInput.text = "";
            _giftNote.text = "";
            _giftField.anchoredPosition = _giftHome;
            _deletePresses = 0;
            if (_deleteLabel != null) _deleteLabel.text = L10n.T("privacy.delete");
            _copiedAt = -99f;
            _copyLabel.text = L10n.T("profile.copy");
            _root.SetActive(true);
            _card.localScale = Vector3.one * 0.85f;
            _tweens.Add(Motion.Time(0.25f), t => { if (_card != null) _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, t); }, Motion.Curve(Ease.OutBack));
            Refresh(defs, s, account, cloud);
        }

        public void Close()
        {
            _root.SetActive(false);
        }

        public void Refresh(IReadOnlyList<StationDef> defs, GameState s, IAccountService account, CloudStatus cloud = default)
        {
            if (!IsOpen) return;
            if (_deletePresses > 0 && Time.unscaledTime - _deletePressedAt > 6f)
            {
                _deletePresses = 0;
                _deleteLabel.text = L10n.T("privacy.delete");
            }
            _cloudStatus.text = CloudLine(cloud);
            _push.SetEnabled(cloud.Available);
            _restore.SetEnabled(cloud.Available);
            _push.FaceRect.GetComponentInChildren<Text>().color = ChunkyButton.InkFor(cloud.Available);
            _restore.FaceRect.GetComponentInChildren<Text>().color = ChunkyButton.InkFor(cloud.Available);
            if (_restoreLocal.activeSelf != cloud.HasLocalBackup) _restoreLocal.SetActive(cloud.HasLocalBackup);
            if (cloud.HasLocalBackup) _restoreLocalLabel.text = L10n.T("cloud.restore.local", Clock(cloud.LocalBackupAt));
            Rabbit avatar = Profile.Avatar(s);
            _avatar.sprite = avatar != null ? _catalog.RabbitArt(avatar) : _catalog.Rabbit;
            _name.text = NameOf(s);
            _code.text = s.Profile.Code;
            _introsCount.text = L10n.T("profile.intros.count", Intros.Opened(s).Count, Intros.All.Length);
            int open = 0;
            for (int i = 0; i < s.Levels.Length; i++) if (s.Levels[i] > 0) open++;
            _stats.text = s.Season > 0
                ? L10n.T("profile.stats.season", Garden.Level(s), s.Rabbits.Count, open, s.Season, (int)System.Math.Round((NewMoon.Bonus(s) - 1d) * 100d))
                : L10n.T("profile.stats", Garden.Level(s), s.Rabbits.Count, open);
            if (_copiedAt > 0f && Time.unscaledTime - _copiedAt > 1.5f)
            {
                _copiedAt = -99f;
                _copyLabel.text = L10n.T("profile.copy");
            }

            bool linked = account.State == BackupState.Linked;
            _backup.text = BackupLine(account);
            _link.SetEnabled(account.CanLink && !linked);
            _link.FaceRect.GetComponentInChildren<Text>().color = ChunkyButton.InkFor(account.CanLink && !linked);
            string reward = s.Profile.LinkRewarded ? "" : L10n.T("backup.reward", Profile.LinkRewardClovers);
            _linkNote.text = linked ? "" : (account.CanLink ? reward : L10n.T("backup.soon") + (reward.Length > 0 ? "\n" + reward : ""));
        }

        public InputField GiftInput => _giftInput;

        public int Refusals { get; private set; }

        public void Redeem()
        {
            string typed = _giftInput.text;
            GiftCodes.Result result = _onRedeem != null && !string.IsNullOrWhiteSpace(typed) ? _onRedeem(typed) : GiftCodes.Result.Invalid;
            if (result == GiftCodes.Result.Ok)
            {
                _giftInput.text = "";
                _giftNote.text = "";
                return;
            }
            Refusals++;
            _giftNote.text = L10n.T(result == GiftCodes.Result.Expired ? "gift.expired" : result == GiftCodes.Result.Used ? "gift.used" : "gift.invalid");
            Sfx.Deny();
            Haptics.Light();
            Vector2 home = _giftHome;
            _tweens.Add(0.32f, t =>
            {
                if (_giftField == null) return;
                _giftField.anchoredPosition = home + new Vector2(Mathf.Sin(t * Mathf.PI * 6f) * (1f - t) * 16f, 0f);
            }, Ease.Linear, 0f, () => { if (_giftField != null) _giftField.anchoredPosition = home; });
        }

        static bool SubmitPressed()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            return keyboard != null && (keyboard.enterKey.isPressed || keyboard.numpadEnterKey.isPressed);
        }
    }
}

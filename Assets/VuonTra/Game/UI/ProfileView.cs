using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// The player's profile: the avatar rabbit (a touch picks the next), the garden's name, the player code
    /// with a copy button, a line of numbers, and where the save is backed up to.
    /// </summary>
    public sealed class ProfileView
    {
        /// <summary>Puts text on the system clipboard. Tests swap it for a capture.</summary>
        public static Action<string> Clipboard = text => GUIUtility.systemCopyBuffer = text;

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly GameCatalog _catalog;
        readonly Tweens _tweens;
        readonly Image _avatar;
        readonly Text _name, _code, _stats, _backup, _copyLabel, _linkNote, _cloudStatus, _restoreLocalLabel;
        readonly ChunkyButton _link, _push, _restore;
        readonly GameObject _restoreLocal;
        readonly Action _onNextAvatar, _onRename;
        float _copiedAt = -99f;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Root => _card;

        public ProfileView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action onNextAvatar, Action onRename, Action onLink,
            Action onPushCloud = null, Action onRestoreCloud = null, Action onRestoreLocal = null)
        {
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

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.52f, 0, 0, 860, 1340);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -80, 600, 70)
                .AddText(L10n.T("profile.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            // The avatar: a big round portrait; a touch walks round the player's rabbits.
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
            UiKit.Rect("AvatarHint", _card).Place(0.5f, 1f, 0, -416, 700, 40)
                .AddText(L10n.T("profile.avatar.hint"), UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);

            // The garden's name, with the pencil beside it.
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

            // The player code on a plate, with Copy at its end.
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
                .AddText(L10n.T("profile.copy"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);

            // Backup: where the save lives, and the link button that waits for a store sign-in.
            UiKit.Rect("BackupTitle", _card).Place(0.5f, 1f, 0, -790, 740, 44)
                .AddText(L10n.T("backup.title"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            _backup = UiKit.Rect("Backup", _card).Place(0.5f, 1f, 0, -836, 740, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            var link = UiKit.Rect("Link", _card).Place(0.5f, 1f, 0, -930, 620, 110);
            _link = UiKit.Chunky(link, catalog.Panel, () => { Sfx.Blip(); onLink(); });
            UiKit.Rect("Text", _link.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(L10n.T("backup.link"), UiKit.TextTitle, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            _linkNote = UiKit.Rect("LinkNote", _card).Place(0.5f, 1f, 0, -1010, 740, 60)
                .AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);

            // The cloud: when the save last went up, a button to send it now, one to take what is up there,
            // and, once something has been replaced, the way back to the copy that was kept on the device.
            _cloudStatus = UiKit.Rect("CloudStatus", _card).Place(0.5f, 1f, 0, -1080, 740, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);
            var push = UiKit.Rect("CloudPush", _card).Place(0.5f, 1f, -170, -1160, 320, 100);
            _push = UiKit.Chunky(push, catalog.Panel, () => { Sfx.Blip(); onPushCloud?.Invoke(); });
            UiKit.Rect("Text", _push.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("cloud.push"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            var restore = UiKit.Rect("CloudRestore", _card).Place(0.5f, 1f, 170, -1160, 320, 100);
            _restore = UiKit.Chunky(restore, catalog.Panel, () => { Sfx.Blip(); onRestoreCloud?.Invoke(); });
            UiKit.Rect("Text", _restore.FaceRect).Stretch(0, 0, 1, 1).AddText(L10n.T("cloud.restore"), UiKit.TextBody, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
            var restoreLocal = UiKit.Rect("RestoreLocal", _card).Place(0.5f, 1f, 0, -1250, 740, UiKit.MinTouch);
            Image restoreHit = restoreLocal.AddImage(null, new Color(0, 0, 0, 0));
            restoreHit.raycastTarget = true;
            var restoreLocalButton = restoreLocal.gameObject.AddComponent<Button>();
            restoreLocalButton.transition = Selectable.Transition.None;
            restoreLocalButton.onClick.AddListener(() => { Sfx.Click(); onRestoreLocal?.Invoke(); });
            _restoreLocalLabel = UiKit.Rect("Text", restoreLocal).Stretch(0, 0, 1, 1).AddText("", UiKit.TextSmall, Palette.InkSoft, TextAnchor.MiddleCenter);
            _restoreLocal = restoreLocal.gameObject;
            _restoreLocal.SetActive(false);

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

        /// <summary>The name to show: the garden's own, or the stand-in while it has none.</summary>
        public static string NameOf(GameState s) => string.IsNullOrEmpty(s.Profile.GardenName) ? L10n.T("profile.unnamed") : s.Profile.GardenName;

        /// <summary>A Unix time as the player reads it: hour and minute, day and month, local time.</summary>
        public static string Clock(long unixSeconds)
        {
            if (unixSeconds <= 0) return "—";
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString("HH:mm dd/MM", System.Globalization.CultureInfo.InvariantCulture);
        }

        /// <summary>What the cloud row says for a status.</summary>
        public static string CloudLine(CloudStatus cloud)
        {
            if (!cloud.Available) return L10n.T("cloud.status.off");
            SyncOutcome? last = cloud.Last?.Outcome;
            if (last == SyncOutcome.NeedsChoice) return L10n.T("cloud.status.choice");
            if (last == SyncOutcome.Offline) return L10n.T("cloud.status.offline");
            if (last == SyncOutcome.NewerVersion) return L10n.T("cloud.status.newer");
            if (cloud.Last != null && cloud.Last.RemoteWasCorrupt && cloud.LastPushedAt < 0) return L10n.T("cloud.status.corrupt");
            return cloud.LastPushedAt > 0 ? L10n.T("cloud.status.at", Clock(cloud.LastPushedAt)) : L10n.T("cloud.status.never");
        }

        public void Open(IReadOnlyList<StationDef> defs, GameState s, IAccountService account, CloudStatus cloud = default)
        {
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
            int open = 0;
            for (int i = 0; i < s.Levels.Length; i++) if (s.Levels[i] > 0) open++;
            _stats.text = L10n.T("profile.stats", Garden.Level(s), s.Rabbits.Count, open);
            if (_copiedAt > 0f && Time.unscaledTime - _copiedAt > 1.5f)
            {
                _copiedAt = -99f;
                _copyLabel.text = L10n.T("profile.copy");
            }

            bool linked = account.State == BackupState.Linked;
            _backup.text = linked ? L10n.T("backup.linked", account.LinkedName) : L10n.T("backup.local");
            _link.SetEnabled(account.CanLink && !linked);
            _link.FaceRect.GetComponentInChildren<Text>().color = ChunkyButton.InkFor(account.CanLink && !linked);
            // What linking brings, until it has been brought; the honest "coming soon" above it while nothing can link.
            string reward = s.Profile.LinkRewarded ? "" : L10n.T("backup.reward", Profile.LinkRewardClovers);
            _linkNote.text = linked ? "" : (account.CanLink ? reward : L10n.T("backup.soon") + (reward.Length > 0 ? "\n" + reward : ""));
        }
    }
}

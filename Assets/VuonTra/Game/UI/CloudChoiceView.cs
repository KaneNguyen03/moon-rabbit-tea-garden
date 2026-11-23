using System;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class CloudChoiceView
    {
        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Text _sub, _localLevel, _localRabbits, _localPlayed, _localSeen, _remoteLevel, _remoteRabbits, _remotePlayed, _remoteSeen;
        readonly GameObject _localTip, _remoteTip;
        readonly ChunkyButton _keepLocal, _keepRemote;
        readonly Action<bool> _onChoose;

        public bool IsOpen => _root.activeSelf;
        public SyncReport Report { get; private set; }

        public CloudChoiceView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<bool> onChoose)
        {
            _tweens = tweens;
            _onChoose = onChoose;
            var overlay = UiKit.Rect("CloudChoice", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 900, 980);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -80, 800, 70)
                .AddText(L10n.T("cloud.choice.title"), 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            _sub = UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -150, 800, 70)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);

            Column(catalog, "Local", -210, L10n.T("cloud.local"), out _localLevel, out _localRabbits, out _localPlayed, out _localSeen, out _localTip);
            Column(catalog, "Remote", 210, L10n.T("cloud.remote"), out _remoteLevel, out _remoteRabbits, out _remotePlayed, out _remoteSeen, out _remoteTip);

            _keepLocal = Choice(catalog, "KeepLocal", -210, L10n.T("cloud.keep.local"), false);
            _keepRemote = Choice(catalog, "KeepRemote", 210, L10n.T("cloud.keep.remote"), true);
            _root.SetActive(false);
        }

        void Column(GameCatalog catalog, string id, float x, string heading, out Text level, out Text rabbits, out Text played, out Text seen, out GameObject tip)
        {
            var col = UiKit.Rect(id, _card).Place(0.5f, 1f, x, -480, 380, 460);
            col.AddImage(catalog.Panel, Palette.Track, true);
            UiKit.Rect("Heading", col).Place(0.5f, 1f, 0, -50, 340, 60).AddText(heading, UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            level = UiKit.Rect("Level", col).Place(0.5f, 1f, 0, -130, 340, 50).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            rabbits = UiKit.Rect("Rabbits", col).Place(0.5f, 1f, 0, -190, 340, 50).AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter);
            played = UiKit.Rect("Played", col).Place(0.5f, 1f, 0, -250, 340, 50).AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            seen = UiKit.Rect("Seen", col).Place(0.5f, 1f, 0, -310, 340, 50).AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            var pill = UiKit.Rect("Suggested", col).Place(0.5f, 1f, 0, -400, 220, 50);
            pill.AddImage(catalog.Panel, Palette.Lantern, true);
            UiKit.Rect("Text", pill).Stretch(0, 0, 1, 1).AddText(L10n.T("cloud.suggested"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            tip = pill.gameObject;
        }

        ChunkyButton Choice(GameCatalog catalog, string id, float x, string label, bool remote)
        {
            var rt = UiKit.Rect(id, _card).Place(0.5f, 0f, x, 110, 380, 110);
            ChunkyButton button = null;
            button = UiKit.Chunky(rt, catalog.Panel, () =>
            {
                button.Press(_tweens);
                Sfx.Blip();
                Close();
                _onChoose(remote);
            });
            UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 10, 0, 10, 0).AddText(label, UiKit.TextBody, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            return button;
        }

        public void Open(SyncReport report)
        {
            Report = report;
            _sub.text = L10n.T(report.SuggestRemote ? "cloud.choice.remote" : "cloud.choice.local");
            Fill(report.Local, _localLevel, _localRabbits, _localPlayed, _localSeen);
            Fill(report.Remote, _remoteLevel, _remoteRabbits, _remotePlayed, _remoteSeen);
            _localTip.SetActive(!report.SuggestRemote);
            _remoteTip.SetActive(report.SuggestRemote);
            Tint(_keepLocal, !report.SuggestRemote);
            Tint(_keepRemote, report.SuggestRemote);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            Motion.PopIn(_card);
        }

        static void Fill(SaveSummary s, Text level, Text rabbits, Text played, Text seen)
        {
            level.text = L10n.T("cloud.level", s.GardenLevel);
            rabbits.text = L10n.T("cloud.rabbits", s.Rabbits, s.Kinds);
            played.text = L10n.T("cloud.played", GardenScreen.Span(s.PlaySeconds));
            seen.text = L10n.T("cloud.seen", ProfileView.Clock(s.LastSeenUnixSeconds));
        }

        static void Tint(ChunkyButton button, bool suggested)
        {
            button.Face.color = suggested ? Palette.Carrot : Palette.Track;
            button.Lip.color = suggested ? Palette.CarrotLip : Palette.Mix(Palette.Track, Palette.Ink, 0.2f);
            button.FaceRect.GetComponentInChildren<Text>().color = suggested ? Palette.OnCarrot : Palette.Ink;
        }

        public void Close()
        {
            _root.SetActive(false);
        }
    }
}

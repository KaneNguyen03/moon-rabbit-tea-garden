using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class IntroView
    {
        public const float Width = 1000f, Height = 400f, Rest = 430f;

        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Image _icon;
        readonly Text _title, _body, _reward;
        readonly Action<string> _onTry, _onLater;
        int _run;

        public string Showing { get; private set; }
        public bool IsOpen => Showing != null;
        public RectTransform Card => _card;
        public ChunkyButton Try { get; }
        public Button Later { get; }

        public IntroView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Action<string> onTry, Action<string> onLater)
        {
            _tweens = tweens;
            _onTry = onTry;
            _onLater = onLater;
            _card = UiKit.Rect("Intro", canvas).Place(0.5f, 0f, 0, Rest, Width, Height);
            _card.AddShadow(22f, 14f, 0.4f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;

            var tag = UiKit.Rect("Tag", _card).Place(0f, 1f, 130, -44, 200, 46);
            tag.AddImage(catalog.Panel, Palette.Lantern, true);
            Text tagText = UiKit.Rect("Text", tag).Stretch(0, 0, 1, 1, 10, 0, 10, 0)
                .AddText(L10n.T("intro.tag"), UiKit.TextSmall, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            tagText.resizeTextForBestFit = true;
            tagText.resizeTextMinSize = 12;
            tagText.resizeTextMaxSize = UiKit.Scaled(UiKit.TextSmall);

            var disc = UiKit.Rect("Disc", _card).Place(0f, 1f, 130, -168, 170, 170);
            disc.AddImage(catalog.Round, Palette.Card);
            _icon = UiKit.Rect("Icon", disc).Stretch(0, 0, 1, 1, 22, 22, 22, 22).AddImage(catalog.Round, Color.white);
            _icon.preserveAspect = true;

            _title = UiKit.Rect("Title", _card).Stretch(0, 1, 1, 1, 250, -96, 36, 30)
                .AddText("", UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Fit(_title, 20, UiKit.TextTitle);
            _body = UiKit.Rect("Body", _card).Stretch(0, 1, 1, 1, 250, -212, 36, 104)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.UpperLeft);
            Fit(_body, 14, UiKit.TextBody);
            _body.alignByGeometry = true;
            _reward = UiKit.Rect("Reward", _card).Stretch(0, 1, 1, 1, 250, -262, 36, 218)
                .AddText("", UiKit.TextSmall, Palette.CarrotInk, TextAnchor.MiddleLeft, FontStyle.Bold);
            Fit(_reward, 12, UiKit.TextSmall);

            var tryRect = UiKit.Rect("Try", _card).Place(1f, 0f, -210, 80, 340, 104);
            Try = UiKit.Chunky(tryRect, catalog.Panel, () =>
            {
                Try.Press(_tweens);
                Choose(_onTry);
            });
            Text tryText = UiKit.Rect("Text", Try.FaceRect).Stretch(0, 0, 1, 1, 12, 0, 12, 0)
                .AddText(L10n.T("intro.try"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);
            Fit(tryText, 16, UiKit.TextTitle);

            var laterRect = UiKit.Rect("Later", _card).Place(1f, 0f, -530, 80, 240, UiKit.MinTouch);
            Later = UiKit.TextButton(laterRect, L10n.T("intro.later"), () =>
            {
                Sfx.Click();
                Choose(_onLater);
            }, UiKit.TextBody);

            _card.gameObject.SetActive(false);
        }

        static void Fit(Text text, int least, int most)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(most);
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public string Title => _title.text;
        public string Body => _body.text;
        public string RewardLine => _reward.text;

        public void Show(string id, Sprite icon, Color tint, string reward)
        {
            Showing = id;
            _icon.sprite = icon;
            _icon.color = tint;
            _title.text = L10n.T("intro." + id + ".title");
            _body.text = L10n.T("intro." + id + ".body");
            _reward.text = reward;
            _card.gameObject.SetActive(true);
            _card.SetAsLastSibling();
            int run = ++_run;
            RectTransform card = _card;
            card.anchoredPosition = new Vector2(0f, -Height);
            _tweens.Add(Motion.Time(Motion.Panel * 1.4f), t => { if (run == _run && card != null) card.anchoredPosition = new Vector2(0f, -Height + (Rest + Height) * t); },
                Motion.Curve(Ease.OutBackLight));
        }

        public void Close()
        {
            if (Showing == null) return;
            Showing = null;
            int run = ++_run;
            RectTransform card = _card;
            float from = card.anchoredPosition.y;
            _tweens.Add(Motion.Time(Motion.PanelOut), t => { if (run == _run && card != null) card.anchoredPosition = new Vector2(0f, from + (-Height - from) * t); },
                Ease.InCubic, 0f, () => { if (run == _run && card != null) card.gameObject.SetActive(false); });
        }

        void Choose(Action<string> then)
        {
            string id = Showing;
            if (id == null) return;
            Close();
            then?.Invoke(id);
        }
    }

    public sealed class IntroListView
    {
        public const float RowHeight = 136f, MaxHeight = 1560f;

        readonly GameObject _root;
        readonly RectTransform _card, _rows;
        readonly ScrollRect _scroll;
        readonly Tweens _tweens;
        readonly Text _empty;
        readonly Dictionary<string, RectTransform> _byId = new Dictionary<string, RectTransform>();

        public bool IsOpen => _root.activeSelf;
        public int RowCount { get; private set; }
        public RectTransform Card => _card;
        public ScrollRect Scroll => _scroll;

        public IntroListView(RectTransform canvas, GameCatalog catalog, Tweens tweens, Func<string, Sprite> icon, Func<string, Color> tint)
        {
            _tweens = tweens;
            var overlay = UiKit.Rect("Intros", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            float height = Mathf.Min(MaxHeight, 200f + RowHeight * Intros.All.Length);
            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, 900, height);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Paper, true).raycastTarget = true;
            UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -80, 600, 70)
                .AddText(L10n.T("intros.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);
            var close = UiKit.Rect("Close", _card).Place(1f, 1f, -70, -70, UiKit.MinTouch, UiKit.MinTouch);
            Image closeImage = close.AddImage(catalog.Round, Palette.Track);
            closeImage.raycastTarget = true;
            var closeButton = close.gameObject.AddComponent<Button>();
            closeButton.transition = Selectable.Transition.None;
            closeButton.targetGraphic = closeImage;
            closeButton.onClick.AddListener(() => { Sfx.Click(); Close(); });
            UiKit.Rect("Text", close).Stretch(0, 0, 1, 1).AddText("×", 48, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold);

            var view = UiKit.Rect("Rows", _card).Stretch(0, 0, 1, 1, 40, 40, 40, 150);
            view.AddImage(null, new Color(0, 0, 0, 0)).raycastTarget = true;
            view.gameObject.AddComponent<RectMask2D>();
            _rows = UiKit.Rect("Content", view).Stretch(0, 1, 1, 1);
            _rows.pivot = new Vector2(0.5f, 1f);
            _scroll = view.gameObject.AddComponent<ScrollRect>();
            _scroll.horizontal = false;
            _scroll.scrollSensitivity = 40f;
            _scroll.viewport = view;
            _scroll.content = _rows;
            _empty = UiKit.Rect("Empty", _rows).Stretch(0, 1, 1, 1, 0, -RowHeight, 0, 0)
                .AddText(L10n.T("intros.empty"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter);
            foreach (string id in Intros.All) _byId[id] = BuildRow(catalog, id, icon(id), tint(id));
            _root.SetActive(false);
        }

        RectTransform BuildRow(GameCatalog catalog, string id, Sprite iconSprite, Color tint)
        {
            var row = UiKit.Rect("Row " + id, _rows);
            row.AddImage(catalog.Panel, Palette.Track, true);
            var disc = UiKit.Rect("Disc", row).Place(0f, 0.5f, 66, 0, 100, 100);
            disc.AddImage(catalog.Round, Palette.Card);
            Image icon = UiKit.Rect("Icon", disc).Stretch(0, 0, 1, 1, 14, 14, 14, 14).AddImage(iconSprite, tint);
            icon.preserveAspect = true;
            Text title = UiKit.Rect("Title", row).Stretch(0, 0.55f, 1, 1, 134, 0, 20, 8)
                .AddText(L10n.T("intro." + id + ".title"), UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            Fit(title, 14, UiKit.TextBody);
            Text body = UiKit.Rect("Body", row).Stretch(0, 0, 1, 0.58f, 134, 4, 20, 0)
                .AddText(L10n.T("intro." + id + ".body").Replace('\n', ' '), UiKit.TextSmall, Palette.Ink, TextAnchor.UpperLeft);
            Fit(body, 11, UiKit.TextSmall);
            body.alignByGeometry = true;
            row.gameObject.SetActive(false);
            return row;
        }

        static void Fit(Text text, int least, int most)
        {
            text.resizeTextForBestFit = true;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(most);
            text.verticalOverflow = VerticalWrapMode.Truncate;
        }

        public void Open(GameState s)
        {
            List<string> opened = Intros.Opened(s);
            foreach (RectTransform row in _byId.Values) row.gameObject.SetActive(false);
            for (int i = 0; i < opened.Count; i++)
            {
                RectTransform row = _byId[opened[i]];
                row.Stretch(0, 1, 1, 1, 0, -RowHeight * (i + 1) + 8, 0, RowHeight * i);
                row.gameObject.SetActive(true);
            }
            RowCount = opened.Count;
            _rows.sizeDelta = new Vector2(_rows.sizeDelta.x, RowHeight * Mathf.Max(1, opened.Count));
            _rows.anchoredPosition = Vector2.zero;
            _empty.gameObject.SetActive(opened.Count == 0);
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _card.localScale = Vector3.one * 0.85f;
            RectTransform card = _card;
            _tweens.Add(Motion.Time(0.25f), t => { if (card != null) card.localScale = Vector3.one * (0.85f + 0.15f * t); }, Motion.Curve(Ease.OutBack));
        }

        public void Close() => _root.SetActive(false);
    }
}

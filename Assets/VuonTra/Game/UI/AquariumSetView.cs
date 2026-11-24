using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    public sealed class AquariumSetView
    {
        public const float CardWidth = 940f, CardHeight = 980f;

        readonly GameObject _root;
        readonly RectTransform _card;
        readonly Tweens _tweens;
        readonly Text _golden, _jars, _bonus;
        readonly GameObject _jarTile, _cloverTile;
        readonly Text _clovers;

        public bool IsOpen => _root.activeSelf;
        public RectTransform Card => _card;
        public string Gifts => _golden.text + " " + (_jarTile.activeSelf ? _jars.text : _clovers.text);
        public string Bonus => _bonus.text;

        public AquariumSetView(RectTransform canvas, GameCatalog catalog, Tweens tweens)
        {
            _tweens = tweens;
            var overlay = UiKit.Rect("AquariumSetCard", canvas).Stretch(0, 0, 1, 1);
            _root = overlay.gameObject;
            overlay.AddImage(null, Palette.Scrim).raycastTarget = true;
            var scrim = _root.AddComponent<Button>();
            scrim.transition = Selectable.Transition.None;
            scrim.onClick.AddListener(Close);

            _card = UiKit.Rect("Card", overlay).Place(0.5f, 0.5f, 0, 0, CardWidth, CardHeight);
            _card.AddShadow(26f, 16f, 0.45f);
            UiKit.Rect("Lip", _card).Stretch(0, 0, 1, 1).AddImage(catalog.Panel, Palette.CardLip, true);
            UiKit.Rect("Face", _card).Stretch(0, 0, 1, 1, 0, 12, 0, 0).AddImage(catalog.Panel, Palette.Mix(Palette.Paper, Palette.Lantern, 0.12f), true).raycastTarget = true;

            Fit(UiKit.Rect("Title", _card).Place(0.5f, 1f, 0, -84, 780, 70)
                .AddText(L10n.T("tank.full.title"), 52, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), 52, 28);
            Fit(UiKit.Rect("Sub", _card).Place(0.5f, 1f, 0, -146, 820, 46)
                .AddText(L10n.T("tank.full.sub", Aquarium.Kinds), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter), UiKit.TextBody, 14);

            UiKit.Rect("Halo", _card).Place(0.5f, 1f, 0, -330, 860, 320).AddImage(catalog.Glow, Palette.Alpha(Palette.Lantern, 0.45f));
            RectTransform water = AquariumView.DrawTank(UiKit.Rect("Tank", _card).Place(0.5f, 1f, 0, -330, 780, 250), catalog);
            for (int k = 0; k < Aquarium.Kinds; k++)
                AquariumView.FishArt(UiKit.Rect("Fish " + k, water).Place(0.5f, 0.5f, -295f + 118f * k, k % 2 == 0 ? 24f : -28f, 96, 72), catalog, k);

            _bonus = Fit(UiKit.Rect("Bonus", _card).Place(0.5f, 1f, 0, -500, 820, 50)
                .AddText("", UiKit.TextTitle, Palette.CarrotLip, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);

            Fit(UiKit.Rect("GiftsHead", _card).Place(0.5f, 1f, 0, -580, 820, 50)
                .AddText(L10n.T("tank.full.gifts"), UiKit.TextTitle, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);
            var gifts = UiKit.Rect("Gifts", _card).Place(0.5f, 1f, 0, -700, 820, 170);
            RectTransform golden = GiftTile(gifts, catalog, "Golden", -170f, out _golden);
            Icon(golden, catalog.GoldenCarrot, catalog);
            RectTransform jar = GiftTile(gifts, catalog, "Jars", 170f, out _jars);
            IncomeView.JarIcon(jar, catalog, "Icon", 80f, 0.5f, 1f, 0, -52);
            _jarTile = jar.gameObject;
            RectTransform clover = GiftTile(gifts, catalog, "Clovers", 170f, out _clovers);
            Icon(clover, catalog.Clover, catalog);
            _cloverTile = clover.gameObject;

            var ok = UiKit.Rect("Ok", _card).Place(0.5f, 0f, 0, 110, 520, 120);
            ChunkyButton button = null;
            button = UiKit.Chunky(ok, catalog.Panel, () => { button.Press(_tweens); Sfx.Click(); Close(); });
            Fit(UiKit.Rect("Text", button.FaceRect).Stretch(0, 0, 1, 1, 20, 0, 20, 0)
                .AddText(L10n.T("tank.full.ok"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextTitle, 16);

            _root.SetActive(false);
        }

        static Text Fit(Text text, int size, int least)
        {
            text.resizeTextForBestFit = true;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            text.resizeTextMinSize = least;
            text.resizeTextMaxSize = UiKit.Scaled(size);
            return text;
        }

        static RectTransform GiftTile(RectTransform parent, GameCatalog catalog, string name, float x, out Text amount)
        {
            var tile = UiKit.Rect(name, parent).Place(0.5f, 0.5f, x, 0, 300, 160);
            tile.AddImage(catalog.Panel, Palette.Card, true);
            amount = Fit(UiKit.Rect("Amount", tile).Place(0.5f, 0f, 0, 34, 280, 46)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleCenter, FontStyle.Bold), UiKit.TextBody, 12);
            return tile;
        }

        static void Icon(RectTransform tile, Sprite sprite, GameCatalog catalog)
        {
            Image icon = UiKit.Rect("Icon", tile).Place(0.5f, 1f, 0, -52, 80, 80).AddImage(sprite != null ? sprite : catalog.Round, sprite != null ? Color.white : Palette.Lantern);
            icon.preserveAspect = true;
        }

        public void Open(GameState s) => Show(Aquarium.SetGolden, s.Aquarium.SetJarsGiven, Aquarium.Bonus(s));

        public void Show(int golden, int jars, double bonus)
        {
            _golden.text = L10n.T("mail.golden", golden);
            _jars.text = L10n.T("mail.jars", jars);
            _clovers.text = L10n.T("mail.clovers", Aquarium.SetJars - jars);
            bool jar = jars > 0;
            if (_jarTile.activeSelf != jar) _jarTile.SetActive(jar);
            if (_cloverTile.activeSelf == jar) _cloverTile.SetActive(!jar);
            _bonus.text = L10n.T("tank.sub", Aquarium.Kinds, Aquarium.Kinds, AquariumView.Percent(bonus), AquariumView.Percent(Aquarium.MaxBonus));
            _root.SetActive(true);
            _root.transform.SetAsLastSibling();
            _card.localScale = Vector3.one * 0.9f;
            RectTransform card = _card;
            _tweens.Add(Motion.Time(Motion.Panel), t => { if (card != null) card.localScale = Vector3.one * (0.9f + 0.1f * t); }, Motion.Curve(Ease.OutBackLight));
        }

        public void Close()
        {
            if (!_root.activeSelf) return;
            _root.SetActive(false);
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>
    /// One quest card: the goal, how far along it is, the reward as chips with pictures, and, only
    /// once it is finished, the button that collects it.
    /// </summary>
    public sealed class QuestRowView
    {
        readonly int _index;
        GameObject _root, _track, _done;
        Text _count, _carrots, _clovers, _golden, _jars, _claimLabel;
        Image _face;
        RectTransform _fill;
        float _shown;

        public RectTransform Root { get; private set; }
        public ChunkyButton Claim { get; private set; }
        /// <summary>The reward chips: where a claimed reward starts its flight from.</summary>
        public RectTransform CarrotChip { get; private set; }
        public RectTransform CloverChip { get; private set; }
        public RectTransform GoldenChip { get; private set; }
        /// <summary>The moon jar a quest pays (#64), where the clover chip would be; the clover shows instead when no jar can come today.</summary>
        public RectTransform JarChip { get; private set; }
        public bool Visible => _root.activeSelf;
        /// <summary>Finished and not yet collected.</summary>
        public bool CanClaim { get; private set; }
        public bool Daily { get; private set; }

        public QuestRowView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, QuestData data, Action<int> onClaim)
        {
            Daily = data.Daily;
            Root = UiKit.Rect("Quest " + data.Id, parent);
            _root = Root.gameObject;
            Root.AddImage(catalog.Panel, Palette.CardLip, true);
            var layout = _root.AddComponent<LayoutElement>();
            layout.preferredHeight = 176;
            layout.minHeight = 176;
            _face = UiKit.Rect("Card", Root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Card, true);

            Text title = UiKit.Rect("Title", Root).Stretch(0, 0.58f, 1, 1, 36, 0, 290, 14)
                .AddText(L10n.T("quest." + data.Id), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            // How far along: a bar with the count at its end. Not a button that cannot be pressed.
            var track = UiKit.Rect("Track", Root).Band(36, 300, 76, 16);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _track = track.gameObject;
            _fill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Moss, true);
            _count = UiKit.Rect("Count", Root).Place(1f, 0f, -156, 84, 252, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold);

            // The reward: chips with the picture of what is paid and how much.
            CarrotChip = Chip(Root, catalog, "Carrots", 36, 224, catalog.Currency, Color.white, out _carrots);
            CloverChip = Chip(Root, catalog, "Clovers", 272, 136, catalog.Clover != null ? catalog.Clover : catalog.Round, catalog.Clover != null ? Color.white : Palette.Moss, out _clovers);
            GoldenChip = Chip(Root, catalog, "Golden", 420, 136, catalog.GoldenCarrot != null ? catalog.GoldenCarrot : catalog.Currency, catalog.GoldenCarrot != null ? Color.white : Palette.Lantern, out _golden);
            JarChip = Chip(Root, catalog, "Jars", 272, 136, null, new Color(0, 0, 0, 0), out _jars);
            IncomeView.JarIcon(JarChip, catalog, "Jar", 42f, 0f, 0.5f, 28, 0);

            var claim = UiKit.Rect("Claim", Root).Place(1f, 0.5f, -146, 4, 236, 116);
            Claim = UiKit.Chunky(claim, catalog.Panel, () => onClaim(_index));
            _claimLabel = UiKit.Rect("Text", Claim.FaceRect).Stretch(0, 0, 1, 1)
                .AddText(L10n.T("quest.claim"), UiKit.TextTitle, Palette.OnCarrot, TextAnchor.MiddleCenter, FontStyle.Bold);

            // A daily that has been collected says so and waits for tomorrow.
            var done = UiKit.Rect("Done", Root).Place(1f, 1f, -146, -48, 236, 56);
            done.AddImage(catalog.Panel, Palette.Track, true);
            UiKit.Rect("Text", done).Stretch(0, 0, 1, 1).AddText(L10n.T("quest.claimed"), UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleCenter, FontStyle.Bold);
            _done = done.gameObject;
            _done.SetActive(false);
        }

        static RectTransform Chip(RectTransform root, GameCatalog catalog, string name, float x, float width, Sprite icon, Color tint, out Text text)
        {
            var chip = UiKit.Rect(name, root).Place(0f, 0f, x + width / 2f, 44, width, 48);
            chip.AddImage(catalog.Panel, Palette.Track, true);
            UiKit.Rect("Icon", chip).Place(0f, 0.5f, 28, 0, 36, 36).AddImage(icon, tint).preserveAspect = true;
            text = UiKit.Rect("Text", chip).Stretch(0, 0, 1, 1, 52, 0, 12, 0)
                .AddText("", UiKit.TextBody, Palette.Ink, TextAnchor.MiddleLeft, FontStyle.Bold);
            return chip;
        }

        /// <param name="dt">Seconds since the last refresh; negative jumps the bar straight to its value.</param>
        public void Refresh(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s, Modifiers m, float dt)
        {
            QuestDef q = quests[_index];
            bool claimed = Quests.IsClaimed(q, s);
            // A finished achievement leaves the list; a finished daily stays as "done for today".
            bool show = (q.Daily || !claimed) && Quests.IsAvailable(q, s.MaxDaySeen);
            if (_root.activeSelf != show) _root.SetActive(show);
            CanClaim = false;
            if (!show) return;

            double target = Quests.TargetOf(q, s);
            double progress = Math.Min(Quests.Progress(q, defs, s), target);
            bool complete = Quests.IsComplete(q, defs, s);
            float fraction = target > 0d ? (float)(progress / target) : 0f;
            if (claimed) fraction = 1f;
            CanClaim = complete && !claimed;

            _shown = dt < 0f ? fraction : Mathf.Lerp(_shown, fraction, Mathf.Clamp01(dt * 8f));
            _fill.anchorMax = new Vector2(Mathf.Clamp01(_shown), 1f);
            _count.text = claimed ? L10n.T("quest.done") : new BigNum(progress).Short() + " / " + new BigNum(target).Short();

            _carrots.text = "+" + Quests.RewardOf(q, defs, s, m).Short();
            // A quest that pays a moon jar shows it in the clover's place, and the clover it replaced when no jar can come today (#64).
            int clovers = Quests.CloversNow(q, s), jars = Quests.JarsNow(q, s);
            _clovers.text = "+" + clovers;
            if (CloverChip.gameObject.activeSelf != (clovers > 0)) CloverChip.gameObject.SetActive(clovers > 0);
            _jars.text = "+" + jars;
            if (JarChip.gameObject.activeSelf != (jars > 0)) JarChip.gameObject.SetActive(jars > 0);
            _golden.text = "+" + q.RewardGolden;
            // A quest that pays golden carrots and no clovers shows its golden chip where the clover chip would be.
            if (clovers == 0 && jars == 0) GoldenChip.anchoredPosition = new Vector2(272 + 68, 44); else GoldenChip.anchoredPosition = new Vector2(420 + 68, 44);
            if (GoldenChip.gameObject.activeSelf != (q.RewardGolden > 0)) GoldenChip.gameObject.SetActive(q.RewardGolden > 0);

            // Finished: the button, and nothing else to measure. Not finished: the bar and the count. Collected: a note.
            if (Claim.Root.gameObject.activeSelf != CanClaim) Claim.Root.gameObject.SetActive(CanClaim);
            Claim.SetEnabled(CanClaim);
            _claimLabel.color = ChunkyButton.InkFor(CanClaim);
            if (_track.activeSelf == CanClaim) _track.SetActive(!CanClaim);
            if (_count.gameObject.activeSelf == CanClaim) _count.gameObject.SetActive(!CanClaim);
            if (_done.activeSelf != claimed) _done.SetActive(claimed);
            _face.color = CanClaim ? Palette.Mix(Palette.Lantern, Palette.Paper, 0.55f) : (claimed ? Palette.Mix(Palette.Card, Palette.CardLip, 0.45f) : Palette.Card);
        }
    }
}

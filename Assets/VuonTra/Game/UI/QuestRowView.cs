using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VuonTra.Core;

namespace VuonTra.Game.UI
{
    /// <summary>One quest card: the goal, how far along it is, the reward, and the claim button.</summary>
    public sealed class QuestRowView
    {
        readonly int _index;
        GameObject _root;
        Text _count, _reward, _claimLabel;
        Image _face;
        RectTransform _fill;
        float _shown;

        public ChunkyButton Claim { get; private set; }
        public bool Visible => _root.activeSelf;

        public QuestRowView(int index)
        {
            _index = index;
        }

        public void Build(Transform parent, GameCatalog catalog, QuestData data, Action<int> onClaim)
        {
            var root = UiKit.Rect("Quest " + data.Id, parent);
            _root = root.gameObject;
            root.AddImage(catalog.Panel, Palette.CardLip, true);
            var layout = _root.AddComponent<LayoutElement>();
            layout.preferredHeight = 176;
            layout.minHeight = 176;
            _face = UiKit.Rect("Card", root).Stretch(0, 0, 1, 1, 0, 8, 0, 0).AddImage(catalog.Panel, Palette.Card, true);

            Text title = UiKit.Rect("Title", root).Stretch(0, 0.60f, 1, 1, 36, 6, 290, 16)
                .AddText(L10n.T("quest." + data.Id), UiKit.TextTitle, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            var track = UiKit.Rect("Track", root).Band(36, 420, 72, 14);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _fill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Moss, true);
            _count = UiKit.Rect("Count", root).Band(0, 290, 55, 48)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold);

            UiKit.Rect("Carrot", root).Place(0f, 0f, 54, 44, 40, 40).AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _reward = UiKit.Rect("Reward", root).Band(84, 290, 22, 44)
                .AddText("", UiKit.TextBody, Palette.InkSoft, TextAnchor.MiddleLeft);

            var claim = UiKit.Rect("Claim", root).Place(1f, 0.5f, -146, 4, 236, 116);
            Claim = UiKit.Chunky(claim, catalog.Panel, () => onClaim(_index));
            _claimLabel = UiKit.Rect("Text", Claim.FaceRect).Stretch(0, 0, 1, 1)
                .AddText("", UiKit.TextTitle, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
        }

        /// <param name="dt">Seconds since the last refresh; negative jumps the bar straight to its value.</param>
        public void Refresh(IReadOnlyList<QuestDef> quests, IReadOnlyList<StationDef> defs, GameState s, Modifiers m, float dt)
        {
            QuestDef q = quests[_index];
            bool claimed = Quests.IsClaimed(q, s);
            // A finished achievement leaves the list; a finished daily stays as "done for today".
            bool show = q.Daily || !claimed;
            if (_root.activeSelf != show) _root.SetActive(show);
            if (!show) return;

            double target = Quests.TargetOf(q, s);
            double progress = Math.Min(Quests.Progress(q, defs, s), target);
            bool complete = Quests.IsComplete(q, defs, s);
            float fraction = target > 0d ? (float)(progress / target) : 0f;
            if (claimed) fraction = 1f;

            _shown = dt < 0f ? fraction : Mathf.Lerp(_shown, fraction, Mathf.Clamp01(dt * 8f));
            _fill.anchorMax = new Vector2(Mathf.Clamp01(_shown), 1f);
            _count.text = claimed ? L10n.T("quest.done") : new BigNum(progress).Short() + " / " + new BigNum(target).Short();

            string clovers = q.RewardClovers > 0 ? L10n.T("quest.reward.clovers", q.RewardClovers) : "";
            _reward.text = "+" + Quests.RewardOf(q, defs, s, m).Short() + clovers;

            bool canClaim = complete && !claimed;
            Claim.SetEnabled(canClaim);
            // Not finished: the plate shows how far along it is, not a button that cannot be pressed.
            _claimLabel.text = claimed ? L10n.T("quest.claimed") : (complete ? L10n.T("quest.claim") : Mathf.FloorToInt(fraction * 100f) + "%");
            _claimLabel.fontSize = canClaim ? UiKit.TextTitle : UiKit.TextBody;
            _claimLabel.color = ChunkyButton.InkFor(canClaim);
            _face.color = claimed ? Palette.Mix(Palette.Card, Palette.CardLip, 0.45f) : Palette.Card;
        }
    }
}

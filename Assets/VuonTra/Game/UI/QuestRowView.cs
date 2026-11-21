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
                .AddText(data.Title, 32, Palette.Ink, TextAnchor.LowerLeft, FontStyle.Bold);
            title.horizontalOverflow = HorizontalWrapMode.Overflow;

            var track = UiKit.Rect("Track", root).Band(36, 420, 72, 14);
            track.AddImage(catalog.Panel, Palette.Track, true);
            _fill = UiKit.Rect("Fill", track).Stretch(0, 0, 0, 1);
            _fill.AddImage(catalog.Panel, Palette.Moss, true);
            _count = UiKit.Rect("Count", root).Band(0, 290, 55, 48)
                .AddText("", 24, Palette.InkSoft, TextAnchor.MiddleRight, FontStyle.Bold);

            UiKit.Rect("Carrot", root).Place(0f, 0f, 54, 44, 40, 40).AddImage(catalog.Currency, Color.white).preserveAspect = true;
            _reward = UiKit.Rect("Reward", root).Band(84, 290, 22, 44)
                .AddText("", 25, Palette.InkSoft, TextAnchor.MiddleLeft);

            var claim = UiKit.Rect("Claim", root).Place(1f, 0.5f, -146, 4, 236, 116);
            Claim = UiKit.Chunky(claim, catalog.Panel, () => onClaim(_index));
            _claimLabel = UiKit.Rect("Text", Claim.FaceRect).Stretch(0, 0, 1, 1)
                .AddText("", 34, Palette.Card, TextAnchor.MiddleCenter, FontStyle.Bold);
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
            _count.text = claimed ? "Xong" : new BigNum(progress).ToShortString() + " / " + new BigNum(target).ToShortString();

            string clovers = q.RewardClovers > 0 ? " · " + q.RewardClovers + " cỏ bốn lá" : "";
            _reward.text = "+" + Quests.RewardOf(q, defs, s, m).ToShortString() + clovers;

            bool canClaim = complete && !claimed;
            Claim.SetEnabled(canClaim);
            _claimLabel.text = claimed ? "Đã nhận" : (complete ? "Nhận" : "Chưa xong");
            _claimLabel.fontSize = canClaim ? 38 : 26;
            _claimLabel.color = canClaim ? Palette.Card : Palette.OffInk;
            _face.color = claimed ? Palette.Mix(Palette.Card, Palette.CardLip, 0.45f) : Palette.Card;
        }
    }
}

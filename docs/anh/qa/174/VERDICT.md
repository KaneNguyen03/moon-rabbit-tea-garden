# #174: visual-critic-gate

Evidence: `tong-hop.png` puts all 12 frames of `tools/shot.sh overnight` (`PreviewShot.Overnight.cs`) side by side, vi on top and en below:
- `174-<vi|en>-co-va-la`: a clover and some recipe leaves
- `174-<vi|en>-dang-bay`: the clover flying out of its row
- `174-<vi|en>-da-nhan`: the clover row after it is taken
- `174-<vi|en>-tho-la-va-than`: a stranger at the gate and two rabbits growing closer
- `174-<vi|en>-ngu-gat`: one rabbit dozing
- `174-<vi|en>-chu-to`: large text

`cat-200-*.png` are 200% crops of the "Đêm qua…" card in three of those frames, in both languages. Frames are 1080×1920.

This task did not ask for helper agents, so the worker that built the card graded it, reading the images against the checklist. That is less independent than the fresh graders used on #170.

## Round 1: REJECTED
Fixed after this round:
- The English stranger line read "A Oolong Rabbit you have never met…". Both languages now say "A new friend waits at the moon gate: {kind}" / "Một bé lạ đang chờ ở cổng trăng: {kind}".
- In the two rabbits growing closer, the front portrait almost hid the one behind. The two tiles are now 68% size and set 24/18 units apart.

## Round 2: APPROVED
```
VERDICT: APPROVED
1. pass - portraits sit inside their rows. The two-rabbit pair overlaps on purpose, and both faces stay visible.
2. pass - the card sits under the carrot row and above the tray row. Rows, portraits and buttons line up in every frame and language; the tray, guest and sleepy-rabbit rows move down together.
3. pass - only existing palette colours: Paper/Card, Night at 7% on the card, Carrot buttons, Ink text. No pure black.
4. pass - portraits and the clover icon come from existing art at its usual import settings; no halos in the 200% crops.
5. pass - no new art and nothing baked in; the clover flies with the existing FeelLayer.
6. pass - the portraits are 118 units, the same size as the sleepy-rabbit portrait below them.
7. pass - every line is readable at normal and large text, with no clipping in vi or en. Long lines wrap to two lines. Buttons are 100 units tall (the floor is 88). "Xong"/"Done" greys out the button that was taken.
```
Not changed by #174: in the one-story frame, the tray strip behind the dim overlay shows above the shorter sheet. It is the garden behind the sheet, as on main.

## Round 3, after the QA review of `d697fe6`: APPROVED
These three changes come from the review's frame nits. The sheet and the crops are rebuilt from `tools/shot.sh overnight` on the new head, and the worker that built the card graded them again.
- The bond row now shows the two friends side by side at 52% size, 62 units apart. Both faces show, and `OvernightScreenTests` checks the spacing.
- A kind name now keeps its words together with no-break spaces, so it no longer splits as "Thỏ Ô / Long"; the whole name moves to the next line.
- The story buttons now have 22 units of padding on each side, so "Welcome" no longer touches the edges at large text.
```
VERDICT: APPROVED
1. pass - both friend portraits sit fully inside the portrait column with both faces visible
2. pass - rows, card and buttons are where they were in round 2
3. pass - no new colours
4. pass - no halos in the 200% crops
5. pass - no new art
6. pass - the pair tiles are smaller than the single portraits, as a pair should be
7. pass - the kind name stays whole, button labels have side padding, nothing clips in vi or en at either text size
```

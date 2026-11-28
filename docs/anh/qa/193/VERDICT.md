# visual-critic-gate, #193

Evidence: `tools/shot.sh feel-lane`, 1 s after a daily quest's claim, the sheet at 60 % and at full height.
"Before" is main's sheet heights (1152 and 1587 on every canvas) with this branch's harness; "after" is this branch.

- `truoc-sau-<vi|en>-phone.png`: 1080x1920
- `truoc-sau-<vi|en>-4x3.png`: 1536x2048
- `truoc-sau-<vi|en>-16x10.png`: 1200x1920
- `cat-200-lan.png`: 200 % crops of the HUD, the gain lane and the top of the full sheet, all three sizes, vi and en

Round 1, graded by the builder (a worker may not start a second agent), against the skill's checklist only:

```
VERDICT: APPROVED
1. pass - sheet edge, handle, corners, HUD chips and card frame are whole at every size; nothing floats loose.
2. pass - the full sheet's top sits under the gain lane at every size: "+58,5K"/"+58.5K" floats over the garden in the lane, clear above the sheet's edge, on the phone, 4:3 and 16:10. Before, on 4:3 the number lay on the "Nhiệm vụ"/"Quests" title and the sheet ran under the HUD's level and carrot chips; on 16:10 the number was hidden under the sheet.
3. pass - same gold number with ink rim, cream sheet, carrot buttons as main; vi and en match.
4. pass - no halo or box behind the number at 200 %.
5. pass - the number and the card are live UI, nothing baked.
6. pass - number and rows keep their size; under the two summary cards the full sheet shows 4 whole quest rows on the phone, 3 on 16:10 and 2 plus part of a third on 4:3 (a 60 % sheet on 4:3 shows 1 plus part of a second).
7. pass - no text covered: not the sheet title or rows, not the HUD, not the card. At full height the card lies on the tab bar for about 2 s, as #182 set up.
```

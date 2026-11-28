# visual-critic-gate: #184 (floating number over a full sheet)

Evidence: `truoc-sau-vi.png`, `truoc-sau-en.png` (the sheet at 60 % and at full height, before and after, 1 s after a daily quest's claim, from `tools/shot.sh feel-lane` frames `168-<vi|en>-bang-nua-qua` and `168-<vi|en>-bang-day-qua`), and `cat-200-vi.png`, `cat-200-en.png` (200 % crops of the HUD lane and the top of the full sheet).

"Before" is main's game code with this PR's preview fix, so it shows what a phone shows. Main's own preview put the number at (495,980) instead: see `docs/anh/qa/170/bang-day-truoc-sau-*.png`.

## Round 1: APPROVED

A new grader saw only the images and the checklist.

```
VERDICT: APPROVED
1. pass - sheet top edge, handle and corner radius clean at both heights; gift card frame intact; nothing floats loose. At full height the sheet stops at about y=335, leaving a clean band of garden under the HUD.
2. pass - the number sits in the garden lane at about (275,260), below the HUD's "40" pill and above the sheet, at both heights. The card is centred over the garden at 60 % and over the nav at full height.
3. pass - gold number with an ink outline, cream sheet, carrot buttons, ink text; no pure black lines; vi and en match.
4. pass - the number's outline is clean on the green ground, with no halo or box behind it; no fringe on the card or sheet edges.
5. pass - the number and card are live UI; nothing is painted into the sprites.
6. pass - the number is the same size as before; the card matches the sheet's type scale.
7. pass - all text readable, nothing clipped by the safe area, buttons over 44 pt. The floating number overlaps no text in any after panel: not the sheet's title or rows, not the HUD, not the card. Before, at full height, it sat on "Nhiệm vụ" / "Quests".
```

The grader's notes outside this issue:
- At full height the card lies over the tab bar for about 2 s, as it did before (#170).
- A sliver of a station's roof shows at the full sheet's top edge.

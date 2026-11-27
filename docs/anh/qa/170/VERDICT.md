# #170: visual-critic-gate

Evidence: `truoc-sau-vi.png` and `truoc-sau-en.png` (left is main `b90ef05`, right is this change, about 1 s after a daily quest is claimed). `cat-200-vi.png` and `cat-200-en.png` are 200% crops of the top HUD and of two empty pads, with main on the left half of each row. `bay-200-en.png` is the top HUD at 200% while the carrots are still in flight. The frames come from `tools/shot.sh feel-lane` (`PreviewShot.FeelLane.cs`): `168-<vi|en>-vuon`, `168-<vi|en>-dang-bay` and `168-<vi|en>-qua-va-so-noi`.

Each round went to a new grader that had not seen the earlier rounds.

## Round 1: REJECTED
Fixed after this round:
- An ingredient on its way between stations (a courier) was drawn over the "Add a rabbit +" sign: `TakeCourier` put it last among its siblings. Couriers now go under every station's front (`UnderFronts`), and a test covers it.
- The flying carrots aimed at the middle of the purse, over "64.2K". They now land on the purse's carrot icon (`_currency`).

## Round 2: REJECTED
Fixed after this round:
- Whole carrot sprites sprayed out of the purse and fell across "+1.95K/s". The purse now only bounces (`PopChip`).
- The number had weak contrast over garden art. It now has an ink rim (`Outline`, Ink 0.85).
- The one-line sign touched the pot and the pestle.

## Round 3: REJECTED
Fixed after this round:
- At 24 units on screen, the English sign is wider than the pad. It now wraps to the pad's width and sits on the empty mat, and a test checks that it stays inside the pad.
- The English frame was taken before the number appeared. The frame is now taken 1 s after the claim.

## Round 4: REJECTED
Fixed after this round:
- The sign's text sat too close to its edges. It now has 12 units of padding on each side.

## Round 5: APPROVED (graded only on what #170 changes)
```
VERDICT: APPROVED
1. pass - nothing changed has a gap or floats loose; the sign sits on the bamboo tray of each empty pad; the flying carrots end on the carrot icon
2. pass - "+58.5K" floats at about y 260, clear of the "40" pill; the card sits between the stations and the tab bar; the ingredient icons stay on the paths
3. pass - lantern-gold number with an ink-purple rim, night-navy sign with cream text, cream card with a carrot title; no pure black
4. pass - no halo, fringe or stray pixels at 200%
5. pass - no effects painted into the sprites
6. pass - the flying carrots match the HUD carrot; the signs suit the pads
7. pass - the sign text is about 26–28 units and the number about 45; the card covers no rabbit; the whole pad (about 230×130) is the tap target
```
Notes from the grader that do not block approval:
- About 0.5 s after the claim, the last carrots cross "+1.95K/s" for a moment on their way to the icon.
- While the card is up it covers the two stone lanterns at the bottom of the garden.

Out of scope here (no change made): the "Bông"/"Mây" name pills and "Tea power 100" are about 16–18 units, and the Vietnamese gift line breaks in the middle of a phrase.

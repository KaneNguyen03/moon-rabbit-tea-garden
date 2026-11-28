# #176: visual-critic-gate

Evidence: three frames from `tools/shot.sh 81-the-tho-ten-dai 81-so-tho-ten-dai 81-the-tho-ten-dai-en` (group `qa-nits-2`, made for #81 with the longest kind names). Frames are 1080×1920.
- `the-tho-vi.png`: the rabbit sheet of Thỏ Đèn Ông Sao (a working rabbit, so the Rest button is shown), with the new line "Hay hát · Thích: Tiệm bánh trung thu" under the level bar
- `the-tho-en.png`: the same sheet in English, "Songbird · Loves: Mooncake Bakery"
- `so-tho-vi.png`: the album page of Thỏ Chòm Sao, with "Hay tò mò · Thích: Đài ngắm sao" under the trade chip

Large text is not in these frames. `AccessibilityTests.The_Personality_Line_Fits_On_One_Line_On_The_Rabbit_Sheet_And_The_Album_Page_For_Every_Kind` covers it for all 76 kinds in vi and en, at normal and large text.

This task did not ask for helper agents, so the worker that built the line graded it, reading the images against the checklist. That is less independent than a fresh grader.

## Round 1: REJECTED
- The line drew at its smallest size, 16 px. The rect width read 0 while the sheet was being built, so `FitOneLine` shrank the text all the way. The line now uses best fit, from 24 px (scaled by large text) down to 16 px, measured against the real rect.
- On the album page the line sat far below the description, in empty space. It now sits under the trade chip, and the description moves down 40 units.

## Round 2: APPROVED
```
VERDICT: APPROVED
1. pass - the rabbit sheet line starts at the same left edge as "Cấp 1/50" and "Đang làm ở…", and stays clear of the level buttons above and the Rest button beside it. The album line is centred like the chip and description around it.
2. pass - the line keeps a clear gap from the level bar above and the "Đang làm ở" line below, and from the trade chip and description on the album page.
3. pass - only existing palette colours: Ink text in bold, on the sheet's Paper.
4. pass - no new art.
5. pass - nothing baked in, and no new motion.
6. pass - 24 px bold, the same weight as the other short labels on the sheet, smaller than the 30 px status lines so it reads as secondary.
7. pass - one line in vi and en, nothing clipped. The longest pairing ("Mê đồ ngọt · Thích: Quầy trà sữa trăng") fits at large text in the tests.
```

# #146 visual gate, round 9 (27/11), graded against docs/art-bible.md at c2d394a

Tree: AC-3 unit with the round-8 code fixes (steam-free cup from the rig's `Cup.png`, dish contact shadows), design's abb8993 assets (Cream PSBs, teapot, tea row, baskets), main 283f081; seed and `tools/test.sh --changed` 818/818. Evidence: `docs/anh/po-146/146-sau.png`, `146-sau-200.png`, `146-sau-gan.png`, `146-sau-gan-200.png`, `146-sau-xam-25.png`, `146-truoc-sau.png`, `146-4-nhip.png`, `146-am-tra-4-nhip.gif`, `146-gio-la-4-nhip.gif`, `146-tra-sua-4-nhip.gif`.

```
VERDICT: REJECTED
1. pass - Arms, ears, hip basket, spoon, cups and the small teapot stay attached through every pose in all three loops (40 frames each). Held tools sit in the hand, and nothing floats or leaves a gap at a joint. The milk drop, sparkle, leaf and steam bits that come loose are moving particles.
2. pass - The background barely moves between frames: mean change in static areas is 0.18–0.55/255, max 1.06. The teapot's three states keep the same stove and lower body, so only the lid and the darker tint change. The tea bush swaps state on the same pot base, and the milk-tea stall stays at 1.4–2.8/255, which is GIF noise. The rabbits only move in their animated poses.
3. fail - Outline colour is not the same on every object. Cream rabbit (#362948), teapot (#342C4B) and tea bush (#3A2A4D) use the ink colour. The milk-tea stall measures #282737–#302B36, a near-black grey. The lop rabbit's outline is brown (#49211F). The white rabbit at the pink pot is grey (#313639) and only 1–2 px wide, against the cream rabbit's 2–3 px. There is no pure #000000 anywhere: 0 pixels in all four PNGs. Island colours match the bible: lip #346B60, gap #2A2347, lit cliff #5C3F48, underside #1F1E44, locked land #223248, rim about #315758. Grass is #41755B, dimmed by moonlight.
4. pass - No white or grey halo against the grass or the pads. The stall edge has a few dark jagged specks at 4x zoom but no light fringe. The one cool-grey edge pixel (#CACFCF) on the outstretched arm appears only in the GIF, and the PNG key frame is clean, so it looks like GIF colour reduction.
5. pass - Steam above the teapot and cup, the sparkle on the milk tea, and the leaves and tea bits all move and fade from frame to frame, so they are particles. The stars and moon on the teapot are glaze pattern, not an effect.
6. pass - Rabbits match each other in size, and the stations sit at about 1–1.3 rabbits tall. Measured shadow dips under the rabbits' feet (10–30 luminance), the stove, planters, bushes, basket and mushrooms. On the dark pads the shadows are soft and never solid ink.
7. pass - HUD and nav text is readable. Nothing is cut off at the edges of the 1080x1920 frame. The Map pill has a 144-unit touch area (commit 0bad625), and other controls use UiKit.MinTouch.
Greyscale: rabbits brightest yes - In the world area of the 25% image, all five station rabbits reach 254–255. The Map pill is 242, the awnings 185–217 and the mortar 198–199.
```

Critic fix notes:
- 3 [art]: redraw the outline of `prop_tra-sua.png` in ink `#3A2A4D` (darkest source pixel `#211C2B`); wood lines may blend up to 30% toward the wood colour.
- 3 [art]: the lop rabbit (brown outline) and the white fluffy rabbit (thin grey outline) are older 2D sprites; recolour their outlines to `#3A2A4D` at 2–3 px on screen, or replace them with rig rabbits.

Critic notes outside the evidence, not graded: `Assets/VuonTra/Art/Final/product_am-tra.png` (the 2D product icon used by the UI tray, not the 2.5D cup) has painted steam; arms reaching past the body carry only the thin inner line in the milk-tea reach and finish poses.

## Phân loại (agent-146, 27/11)

Kỹ thuật: không còn mục kỹ thuật nào hỏng. Mục 5 và 6 của vòng 8 (hơi vẽ sẵn, bóng đĩa) đạt sau khi sửa phía code.

Art, của design (không đổi trong #146): mục 3, nét quầy trà sữa tối hơn mực, và nét thỏ cụp/thỏ trắng xù là hình 2D cũ của các giống chưa có rig (ADR-0007 mục 9: giống khác giữ hình của mình). Ghi chú ngoài mục về `Art/Final/product_am-tra.png` là hình UI của bản 2D, ngoài phạm vi 2.5D của #146.

## Phân xử art (art director, 27/11), chép nguyên văn từ `docs/anh/qa/146-vong9-design/VERDICT.md` (sua-146-asset f880ca6, dòng (b) theo df13c83)

Biên bản critic ở trên giữ nguyên. Phần này là kết luận của design cho mục 3; không đổi mục kỹ thuật nào thành đạt.

- (a) Viền quầy trà sữa gần đen (`#282737`–`#302B36`): **sửa**. Pixel viền trong 8 px quanh mép, tối hơn mực và ít sắc độ, đưa về mực `#3A2A4D`; vân gỗ bên trong giữ nguyên. Art director: đạt theo art bible mục 9.
- (b) Thỏ tai cụp viền nâu và thỏ bông trắng viền xám: Art director **đạt cho #146, chờ rig theo giống**. Đúng luật ADR-0007 mục 9: giống chưa có rig giữ hình 2D đúng giống. Giới hạn: viền của hai hình này chưa theo art bible mục 9 (nâu, xám, 1–2 px); sửa khi có rig ở #164.
- Ngoài phạm vi: `Art/Final/product_am-tra.png` (icon món 2D) có hơi nước vẽ sẵn; B chuyển PO để gắn vào #136.

Ghi chú agent-146: quầy (a) sửa ở nguồn, đã chép vào `Art/Garden25D/Stand`; vòng 10 (vòng đóng, sau AC-2 và AC-1) chấm kết quả trong game.

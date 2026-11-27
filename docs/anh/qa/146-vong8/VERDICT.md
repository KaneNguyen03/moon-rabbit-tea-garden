# #146 visual gate, round 8 (27/11), graded against docs/art-bible.md at c2d394a

Tree: AC-3 unit (station prop states, dishes, Shuriken bursts) plus design's 5872050 assets (Cream arms, stall and lantern edges), after seed and `tools/test.sh --changed` 816/816. Evidence: `docs/anh/po-146/146-sau.png`, `146-sau-200.png`, `146-sau-gan.png` (close zoom on the milk-tea stall), `146-sau-gan-200.png`, `146-sau-xam-25.png`, `146-truoc-sau.png`, `146-4-nhip.png`, `146-am-tra-4-nhip.gif`, `146-gio-la-4-nhip.gif`, `146-tra-sua-4-nhip.gif`.

```
VERDICT: REJECTED
1. fail - The rigged station rabbits (tea pot, leaf basket, milk tea) show a doubled ink arc across the top of the head between the ears: ink, a 1 px grey/background gap, then ink again (GIF am-tra frame 22, column x=110: #3C2E42 / #5C5F5B / #36203F, then fur). The ear-root outline sits about 2 px above the head outline and reads as a headband seam. The unrigged rabbits (mortar, pot) have one clean line. The tools in hand (spoon, tea caddy, teapot, cups, basket, leaf sprig) all meet a paw, and nothing floats.
2. pass - Feet are anchored in all 40 frames of all three loops (template match on the feet: 0 px drift, at most 1 px vertical). Face offsets of up to 3 px come from body lean between pose keys, not from the whole sprite sliding. The am-tra frames 20→21→23 go 0 → -3 → +1 px, which is the sharpest change; watch it, but it is not detachment.
3. fail - The outlines are inconsistent. The rigged rabbits have about a 1 px outer line in a greyish ink (#514559, #48364F, #5B5356) that washes out to #64606B/#756E76 in places. The mortar and pot rabbits have 2 px lines (#3A2E3B, #3D374E). The bible requires 2–3 px of #3A2A4D. The teapot outline is near-black navy #1D1F39 and the basket's is #24212E, both darker than the ink colour. Measured grass is #41755B against #3D8369 (darker and less green). The island edge matches: lip #346B60, cliff #5B3F48, gap #272245, underside #1F1E44, and locked land #27384D, which is inside the #2A3C52 allowance. There is no #000000 (darkest pixel #171127).
4. pass - The stall awning and leg edges at 500% are clean with no light halo. The tea cup, milk tea and leaf bundle have no fringe. There are no stray pixels or background remnants. Grey pixels at the rabbit edges are anti-aliasing, not a halo (the thin-line problem is under item 3).
5. fail - A teal steam wisp is painted into Assets/VuonTra/Art/Garden25D/Stand/product_am-tra.png. In the loop it stays identical and fixed to the cup from frame 30 to 39. The white teapot puffs and the milk-tea star move and fade, so those are real VFX.
6. fail - The finished dishes have no contact shadow. Under the tea cup the pad brightness is 116/108/106 compared with 107/107/106 in the empty frame, and the leaf bundle on the deck and the milk-tea cup on the counter show no difference either. Rabbits, basket and mushrooms do have shadows (grass 100 → 72–90 under them). Rabbit scale is consistent at about 105–110 px tall.
7. pass - The HUD and Map pill text is readable and nothing is clipped. The UI is unchanged from #145. The visual heights of the top buttons (about 95 px) and the Map pill (90 px) are under 44 pt at 1080-wide/3x, but the hit rects can't be checked from the images.
Greyscale: rabbits brightest yes - In the 25% grey image all five station rabbits reach 247–251 (95th percentile), more than every other garden mass: teapot 204, awning 202, paths 183–191, stone ring 183, pot 184. Only tiny mushroom-cap specks and the pot highlights reach 243–255. The HUD cards are as bright, but they are UI.
```

Critic fix notes:
- 1 [art]: on the rigged rabbit, erase the bottom outline arc of the ear-root sprites, or move the ears down or back in sorting order so the head outline is the only line across the crown; needs a rebuild through `RabbitRigBuilder`.
- 3 [art]: thicken the rigged rabbits' outer line to about 7 px at 512 px source (2–3 px on screen) in #3A2A4D; recolour the teapot and basket outlines from #1D1F39/#24212E to the ink colour, wood tinted at most 30%.
- 3 [placement/render]: if measured grass must be #3D8369, check the ground's light; the island edge colours are within tolerance.
- 5 [art]: remove the steam wisp from `product_am-tra.png` and spawn it from `vfx_hoi-nuoc.png` with a Shuriken emitter on the dish.
- 6 [placement/render]: give each finished dish a `shadow_elip` anchored per art bible §10, no darker on a dark pad than the pad mixed 50% with ink.

## Phân loại (agent-146, 27/11)

Kỹ thuật, của #146, đã sửa sau vòng 8 (chờ vòng 9):
- 5: đĩa của ấm trà là hình tạm chép từ `Art/Final/product_am-tra.png` (hình 2D có vẽ sẵn hơi nước). Đã thay bằng chén trà trên đĩa nhỏ `Art/Rabbit25D/Cream/Tools/Cup.png` của rig (không vẽ hơi), chép vào `Art/Garden25D/Stand/item_chen-tra.png`; hơi nước trên chén vẫn là hạt Shuriken `vfx_hoi-nuoc` ở nhịp 4.
- 6: mỗi đĩa món có bóng `shadow_elip` riêng (neo §10, 35 % ló trước chân đĩa, alpha × 0,45 như trên đế), bật/tắt cùng đĩa trong clip; nằm trên mặt mà đĩa đứng (đế, sàn gỗ, mặt quầy).

Art, của design (không đổi trong #146):
- 1: đường mực đôi ở đỉnh đầu giữa hai tai của rig Cream (gốc tai). Các vòng trước không nêu mục này; CreamFront.psb vừa đổi ở 5872050.
- 3: nét ngoài của rig mảnh và xám hơn §9; nét ấm trà và giỏ tối hơn mực. Màu cỏ: design đã ký "đạt về hướng màu" ở vòng 7.

Bằng chứng cho design ký quầy trong game: mục 4 đạt, quầy ở zoom gần sạch (`146-sau-gan.png`, `146-sau-gan-200.png`).

## Phân xử art (art director, 27/11), chép nguyên văn từ `docs/anh/qa/146-vong8-design/VERDICT.md` (sua-146-asset abb8993)

Biên bản critic ở trên giữ nguyên. Phần này là kết luận của design cho các mục art; không đổi mục kỹ thuật nào thành đạt.

- Nét đôi ở đỉnh đầu: **sửa**. Nguyên nhân là nét dày design thêm ở mép dưới lớp tai (vòng 5), nằm trên đường đầu khoảng 6 px; kèm một hàng bán trong suốt ở gốc tai và một đường chép hàng trong tai. Đã bỏ nét dày ở mép tai giáp đầu, lấp hàng bán trong suốt bằng màu tai, đường đầu là một nét mực liền. Art director: đạt, mọi khung clip chỉ còn một nét.
- Viền ngoài thỏ mảnh và ngả xám: **sửa**. Viền ngoài lên khoảng 9 px ở nguồn 512 (vòng 5 là 7 px), cho khoảng 2 px ở zoom mặc định. Tay trước thân giữ nét trong khoảng 4–5 px. Art director: đạt theo art bible mục 9.
- Viền ấm trà và giỏ tối hơn mực: **sửa**. Các pixel viền trong 7 px quanh mép, tối hơn mực và cùng sắc mực, đưa về đúng `#3A2A4D`; bếp sắt giữ nguyên đổ bóng. Art director: đạt.
- Quầy ở zoom gần: art director **đạt**. Ảnh trong game sau khi nhập bản xuất lại sạch ở 500%; mép còn lại là nét màu nước vẽ tay, không phải quầng.

Ghi chú agent-146: các bản sửa art ở trên (PSB Cream, ấm trà, luống chè, giỏ) đã nhập vào cây từng tệp; vòng 9 chấm kết quả trong game. Mục kỹ thuật 5 và 6 sửa ở phía code (xem phần phân loại).

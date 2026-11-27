# #146 visual gate, round 7 (27/11), graded against docs/art-bible.md at c2d394a

Evidence: `docs/anh/po-146/146-sau.png`, `146-sau-200.png`, `146-sau-gan.png`, `146-sau-gan-200.png`, `146-sau-xam-25.png`, `146-truoc-sau.png`, `146-4-nhip.png`, `146-am-tra-4-nhip.gif`, `146-gio-la-4-nhip.gif`, `146-tra-sua-4-nhip.gif`.

```
VERDICT: REJECTED
1. fail - In the work beats where the arms come in front of the body (am-tra "ve", tra-sua beats 2 and 4, gio-la beat 3), each arm is only a thin pale loop over the belly, about #967581 (luma 120–157) against the 2–3 px ink silhouette (luma ~50). The arms look see-through and pasted on rather than growing from the body. Tra-sua beat 3 also shows a lavender seam where the raised arm meets the shoulder, and art-bible item 9 says the shoulder has no line.
2. pass - Feet stay locked to the same pixels in all 40 frames of every loop (template offset (0,0), with one 1 px exception in tra-sua). The head drifts at most ±6 px, but it moves smoothly from frame to frame, so it reads as a lean and not jitter. Stations, props and pads match between the far and close frames.
3. fail - Several colours and outlines are off the art bible. (a) Open grass measures #40785C (median #41755B) where the bible asks for #3D8369: greener-yellow and darker, about 11 lower in G and 13 lower in B. (b) A teal band of #2F5C59, which is the rim colour, runs under the cliff wall between it and the underside (x=400, y≈1680–1704; also at x=60), and item 8 forbids any rim showing below the cliff. (c) Outline weight is not even: the pink pot's outline is about 5 px against 2–3 px on the rabbits, and the front arms use the pale inner lines from item 1 instead of ink. The background trees are flat vector shapes with heavy outlines, unlike the watercolour props. What does match: no #000000 anywhere (0 pixels), rabbit cream #FCF4DC ≈ #FFF4E0, lip #356E61, gap #2B2448, sunlit cliff #5D4049, underside #211F45, and the top locked land #27374D, which is inside the allowed drift up to #2A3C52.
4. fail - At close zoom (146-sau-gan / -200) the milk-tea stall shows a ragged dark speckled fringe outside its outline along the right roof edge and post, plus a soft double line along the left awning. The stall texture is also visibly blurrier than the rabbit next to it. Rabbits and the default-zoom props have clean edges.
5. pass - No sparkles, steam, dust or leaves are painted into any sprite. The teapot's moon and stars are surface decoration, and the stove embers belong to the prop.
6. pass - All five rabbits are the same size, and the pink pot is about 1.0 rabbit tall. Under the rabbits' feet the ground is 10–25% darker than beside them (am 89 vs ~100, gio-la 83 vs ~107, cối 88 vs ~95), so contact shadows are there, though faint. The stalls, basket and tree trunks also have shadows.
7. fail - The new "Bản đồ" pill is 270×92 px in a 1080-wide frame, about 33–35 pt or dp, so below 44 pt unless its hit rect is larger than what is drawn. The images can't show the hit rect. Its label is only about 18 px tall. Its fill, #DBD1C3, reads as disabled next to the HUD cards (luma ~241–251). The rest of the HUD is readable and unchanged from the 2D build.
Greyscale: rabbits brightest yes - The brightest 0.3% of the scene in 146-sau-xam-25.png (HUD and pill masked out) lands only on the five rabbits (max 254–255). Next are the mushroom caps at 228, the far stall awning at 217, the mortar stones at 199, the path at ~195 and the stone ring at 188.
```

Critic fix notes:
- 1 [art]: redraw the front-arm frames with a solid fill on each arm, a light inner shade so the arm separates from the belly, and a 1–1.5 px inner line in ink #3A2A4D; remove the shoulder stroke on the raised arm in tra-sua beat 3; keep the held cup and teapot inside the paw outline.
- 3a [placement/render]: the open grass comes out greener and darker than #3D8369; fix it in the island ground's light or tint, not by changing the colour.
- 3b [placement/render]: stop the rim and locked-ground plane at the island's front edge, or draw it behind the cliff, so the underside runs straight from the cliff to the bottom of the screen.
- 3c [art]: the pink pot (#131) and the vector trees need outlines at the 2–3 px silhouette weight.
- 4 [art]: defringe the stall source PNG and supply a higher-resolution version for the close zoom. [placement/render]: check the stall uses bilinear without mipmaps (it does since this round's seed).
- 7 [placement/render]: Bản đồ pill hit rect at least 44 pt, card cream fill, larger label.
- Not numbered [placement/render]: in the close frame the red mushroom left of the milk-tea stall is cut by the counter along a hard vertical edge; move it clear of the stall's outline.

## Phân xử art (art director, 27/11), chép nguyên văn từ `docs/anh/qa/146-vong7-design/VERDICT.md` (sua-146-asset 5872050)

Biên bản critic ở trên giữ nguyên. Phần này là kết luận của design cho bốn mục art; không đổi mục kỹ thuật nào thành đạt.

- Tay trước thân: **sửa**. Nét tay dưới vai thành mực khoảng 4–5 px ở nguồn, đầu vai vẫn mở. Art director: đạt, tay đọc rõ là chi đặt trước thân trong mọi khung clip; còn một mép sáng dưới 1 px ở đầu vai khi tay giơ cao, chấp nhận được theo art bible mục 9.
- Màu tile cỏ: **giữ tile #129, không tint material**. Art director: đạt về hướng màu. `#3D8369` là màu vai trò của cỏ, không phải trung vị bắt buộc của mọi pixel texture; trung vị đo `#41755B` nằm trong họ màu đó và cỏ không cạnh tranh với thỏ.
- Viền nồi hồng và cây nền phẳng: **chưa đạt**. Đây là hình cũ chờ thay (#131, ASSET-016), không phải lỗi billboard của #146. Không ký đạt về phong cách; PO quyết có tách khỏi tiêu chí đóng #146 hay không.
- Viền quầy ở zoom gần: **sửa nguồn**. `prop_tra-sua.png` có 47 cụm alpha rời quanh mép (ở ngưỡng alpha 8); đã bỏ các cụm rời và tràn màu mép vào vùng trong suốt, cũng làm cho `decor_lantern.png`. Chưa ký đạt trong game: cần agent chép bản mới vào Art/Garden25D/Stand và chụp lại ở zoom gần.

Quyết định PO (27/11): nồi hồng và cây nền phẳng **tồn tại, chuyển issue** (design mở và giữ, nhãn do-hoa + 2-5d; issue #163, hình dự kiến khoảng 2/12). Gate vòng 8 không chặn ở hai mục này.

Trạng thái sau vòng 7: tay đã sửa ở nguồn, cần vòng 8 xác nhận trong game; quầy chưa ký đạt trong game, còn mở tới khi design xem ảnh zoom gần vòng 8; nút Bản đồ (mục 7) chuyển QA D, không thuộc #146.

Cập nhật của design (sua-146-asset cf477c1, 27/11), dòng mới chép nguyên văn:

- Viền nồi hồng và cây nền phẳng: **tồn tại, chuyển issue #163**. Hình cũ chờ thay (#131, ASSET-016), không phải lỗi billboard của #146; PO tách khỏi tiêu chí đóng #146 ngày 27/11. Không ký đạt về phong cách.

# visual-critic-gate — #145 lát 3 (đất, camera, bản đồ sau cờ Garden25D)

Bằng chứng: `145-truoc-sau.png` (trái vườn 2D, phải đảo 2.5D, dưới mép trước 200 %), `145-sau.png`, `145-sau-en-chu-to.png`, `145-sau-ban-do.png`, `145-truoc.png`; 1080×1920, zoom mặc định, dựng bằng `PreviewShot.RenderGarden25D`. Lần chấm đầu REJECTED (mục 7: viên "Bản đồ" và nút × dưới 44 pt; kèm góp ý ô cỏ quá mịn, bản đồ không thấy mép đảo); đã sửa (vùng chạm 132, ô cỏ 3,4 m, bản đồ chừa 1 m biển) và chấm lại.

```
VERDICT: APPROVED
1. pass - mặt cỏ, viền #366F62, khe #453448, đất #85554C gặp nhau thành dải thẳng sạch, không hở; mép ảnh bản đồ sắc cả bốn phía
2. pass - HUD, chip "Tuần đầu", viên Bản đồ, thanh tab không đè nhau; nút × trên bản đồ dưới HUD ở lề trái
3. pass - trời gần Night #1B1E45, cỏ gần #3D8369; không có #000000
4. pass - không quầng, không viền, không điểm lạc ở mép đảo và mép bản đồ
5. pass - đốm sáng là của ô cỏ, không vẽ chết hiệu ứng
6. pass - ô cỏ to gấp 2–3 lần, đọc là bãi cỏ dưới phối cảnh nhẹ, mép đất dày tin được
7. pass - vùng chạm viên và × 132 đơn vị (44 pt); chữ đọc được ở vi và en Chữ to, không bị cắt
```

Góp ý cho design (không chặn): nền viên "Bản đồ" ra #DBD1C3 (Panel tô Paper, giống nút "Về tiệm trà" cũ) nên trông hơi như tắt; hai góc trước của khối đất ra ngoài mép màn và còn ~450 px trời trống phía trên (chờ hình đảo); mặt đất trước là một dải phẳng, cần lớp màu nước hoặc nửa dưới tối hơn khi có hình đảo; bản đồ là hình chữ nhật xanh, một đường viền teal sẽ cho thấy chỗ vườn hết; (có từ trước) en Chữ to: "Tea power 73" co nhỏ hơn "Boost!".

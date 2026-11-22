# Đề xuất #8 — Thỏ sự kiện theo mùa

Viết 22/11/2026. Hình đã vẽ bằng Antigravity (xem `docs/anh/VE-BANG-ANTIGRAVITY.md`), nằm ở `docs/anh/po-8/`: sáu ứng viên, mỗi mùa hai, chủ project chọn một (hoặc giữ cả hai làm bậc khác nhau).

| Mùa | Khoảng ngày (giờ máy) | Ứng viên 1 | Ứng viên 2 |
|---|---|---|---|
| Trung thu | 1/9 – 30/9 | `tho-den-ong-sao` Thỏ Đèn Ông Sao (cầm đèn sao đỏ) | `tho-banh-trung-thu` Thỏ Bánh Trung Thu (bụng in hoa văn bánh) |
| Tết | 15/1 – 28/2 | `tho-ao-dai` Thỏ Áo Dài (áo dài đỏ hoa mai) | `tho-banh-chung` Thỏ Bánh Chưng (xanh lá, dây lạt) |
| Giáng sinh | 1/12 – 31/12 | `tho-noel` Thỏ Noel (mũ đỏ, khăn kẹo) | `tho-tuan-loc` Thỏ Tuần Lộc (sừng nhỏ, mũi đỏ) |

Trung thu theo âm lịch thực tế rơi trong tháng 9 hoặc đầu tháng 10 dương lịch; dùng khoảng cố định 1/9–30/9 (cộng tới 10/10 nếu muốn) để không phải tính lịch âm trong Core. Tết giữ 15/1–28/2 để phủ mọi năm.

## Luật (Core, có test)

- **Loài theo mùa**: thêm một trường `Season` (None / MidAutumn / Tet / Christmas) vào `RabbitKind`; mỗi mùa một loài, bậc Vàng, nghề: Đèn Ông Sao → Ngắm sao, Áo Dài → Pha trà, Noel → Bếp bánh. Hệ số như bậc Vàng, không mạnh hơn.
- **Chỉ có trong mùa**: ngoài mùa, loài không rơi từ gọi bằng cỏ, gọi vàng hay thỏ ghé tiệm. Thỏ đã có thì giữ mãi, vẫn làm việc, vẫn trong sổ. Sổ thỏ hiện ô mùa với dòng "Chỉ có trong Trung thu".
- **Cách gặp trong mùa**: (1) thỏ ghé tiệm: trong mùa, bé ghé mỗi ngày là loài mùa với xác suất 1/3 (ngày còn lại theo luật cũ), giá như bậc Vàng (120 cà rốt vàng); (2) quà ngày cuối mùa: ngày cuối trong mùa, quà ngày cho thẳng loài mùa nếu chưa có (một con, không cần tiền) để ai chơi đều cũng có; (3) gọi vàng trong mùa: bậc Vàng rơi ra loài mùa với xác suất 50 %.
- **Nhiệm vụ mùa**: ba nhiệm vụ một lần, chỉ hiện trong mùa, thưởng cà rốt vàng (3, 5, 10): "Gặp thỏ mùa", "Xếp thỏ mùa vào đúng nghề", "Chụp ảnh có thỏ mùa". Hết mùa thì biến mất, chưa nhận thì mất (ghi rõ trên thẻ).
- **Trang trí mùa**: một đạo cụ trang trí theo mùa treo ở cổng trăng (đèn ông sao, cành mai, vòng lá thông), vẽ sau; vòng đầu dùng tint của cổng trăng.
- **Chống lùi đồng hồ**: save giữ `MaxDaySeen` (số ngày lớn nhất đã thấy, cùng cách đếm với `DailyDay`). Ngày hiện tại nhỏ hơn `MaxDaySeen` thì coi như ngày đó (mùa và quà tính theo `MaxDaySeen`), nên lùi đồng hồ không mở mùa đã qua và không nhận lại quà. Dùng chung cho quà ngày, lượt trò nhỏ và thỏ ghé tiệm (hiện mỗi nơi đang tự tính theo `DailyDay`).

## Test

- Trong và ngoài mùa theo ngày; biên 1/9, 30/9, 31/12, 1/1 và 15/1, 28/2 kể cả năm nhuận.
- Ngoài mùa không rơi loài mùa ở ba đường gọi; trong mùa đúng xác suất (quét 2000 lần).
- Quà ngày cuối mùa cho loài mùa đúng một lần, không cho nếu đã có.
- Nhiệm vụ mùa chỉ hiện trong mùa và mất khi hết.
- `MaxDaySeen` không giảm; lùi đồng hồ không đổi mùa.

## Chủ project cần chốt

- [ ] Chọn một ứng viên mỗi mùa (hoặc giữ cả hai: ứng viên 2 làm bậc Tím, cùng cách gặp).
- [ ] Ba khoảng ngày.
- [ ] Ba cách gặp và giá 120 cà rốt vàng khi ghé tiệm.
- [ ] Nhiệm vụ mùa thưởng 3/5/10 cà rốt vàng.

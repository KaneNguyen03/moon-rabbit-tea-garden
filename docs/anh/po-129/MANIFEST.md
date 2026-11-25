# #129 — nền cỏ

| Tệp | Kích thước | Nguồn | Dùng ở |
|---|---|---|---|
| `bg_grass.png` | 1024×1024, lặp liền 4 mép | Antigravity (AI), image-generator, 25/11/2026 | `Assets/VuonTra/Art/Ground/bg_grass.png`, nền vườn |
| `bg_grass_clover.png` | 1024×1024, lặp liền 4 mép | Antigravity (AI), image-generator, 25/11/2026 | biến thể nhiều cỏ ba lá, chưa dùng |
| `bg_grass-lap-2x2.png`, `bg_grass_clover-lap-2x2.png` | 1024×1024 | ghép 2×2 để kiểm mép | QA |

Kiểm mép đo bằng chênh lệch điểm ảnh trung bình: mép trái–phải 2,8 và trên–dưới 3,6 với `bg_grass`, ngang mức hai cột kề nhau ở giữa ảnh (2,9), nên không có đường nối. Màu trung bình (67, 121, 93), độ lệch chuẩn khoảng 8: tương phản thấp, gần `Grass #3D8369`.

| `bg_grass_flowers.png` | 1024×1024, lặp liền 4 mép | Antigravity (AI), image-generator, 25/11/2026 | biến thể chấm hoa dại trắng và hồng, chưa dùng |
| `rai/rai_co.png`, `rai_co-ba-la.png`, `rai_hoa-trang.png`, `rai_hoa-hong.png`, `rai_nam.png`, `rai_soi.png` | cạnh dài 432–512, bội số 4, nền trong suốt | Antigravity (AI), tấm `sheet-129-rai`, cắt chung một tỉ lệ | 6 sprite rải theo chùm (phần code của B) |
| `loi/loi_thang.png`, `loi_cong.png`, `loi_nga-ba.png`, `loi_dau.png` | xem `loi/noi.json` | Antigravity (AI), tấm `sheet-129-loi`, cắt chung một tỉ lệ | bộ lối mòn (phần code của B) |

## Lối mòn: cách nối

- `loi_thang` lặp ngang liền: hai đầu là cùng một cột ảnh (trộn cuộn nửa chiều dài). Dọc thì xoay +90°.
- Mỗi đầu cắt của `loi_cong`, `loi_nga-ba`, `loi_dau` được hoà 64 px vào đúng mặt cắt của `loi_thang`, nên nối với đoạn thẳng không lộ vết.
- `loi/noi.json` ghi dải lối đi ở từng đầu cắt (hàng từ trên xuống cho đầu trái/phải, cột từ trái cho đầu dưới): đặt hai mảnh sao cho hai dải này trùng nhau. Mọi đầu ngang nằm ở hàng 21–24 đến 206–210.
- `loi_cong` là góc 90° (ảnh không vẽ được 45°); `loi_dau` có đầu cắt bên trái, đầu tròn bên phải.
- Màu đất sáng hơn nền đêm: khi đặt trong game nên tô `Image.color` tối đi khoảng một bậc (ví dụ nhân `Palette.Mix(Color.white, Palette.Night, 0.35f)`) để lối đi không sáng hơn thỏ.
- `loi-ghep-thu.png`: ghép thử thẳng + ngã ba + thẳng + đầu, chân ngã ba nối đoạn thẳng xoay +90°. `cong-cu-lam-loi.py`: dựng lại bộ lối từ tấm vẽ (cắt, khử viền, gọt đầu cắt, làm liền).

## Kiểm định

`preview_qa_sheet.png`: mỗi hình trên ca rô, cỏ `#3D8369`, tối `#2B2420`, kèm mối nối 300 % và mép cỏ 400 %. Khử viền: 0 điểm màu nền cũ trên mép, 0 điểm đen thuần. Kết quả ở `VERDICT.md`.

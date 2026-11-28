# Design review #168 và #187 trên main fb21c25

Ngày 28/11. Ảnh `tools/shot.sh` 1080×1920 trên main `fb21c25`: `115-phut-15-en.png` (đêm), `11-garden-en.png`, `62-mua-ngay.png`, `62-mua-dem.png`; `truoc-115-phut-15-en.png` là main `b90ef05` trước #185. Bảng ghép: `truoc-sau-mua.png` (trước, sau, mưa ngày, mưa đêm).

| Mục | Đo | Kết luận |
|---|---|---|
| Cỏ đêm so với `#3D8369` | trước `#43795D` (độ sáng −8%, bão hoà −16%); sau `#407F64` (−3%, −7%) | Đạt (±10%) |
| Vignette | độ sáng mép trái so với giữa: trước 0,87, sau 0,96 | Đạt, mép không còn nặng |
| Lớp phủ đêm | vignette (gộp quầng trăng) + vân giấy | Đạt (2) |
| Vũng sáng đèn lồng và đèn đá viền rừng | thấy rõ dưới mọi đèn | Đạt |
| Lối đi khi mưa (#187) | lối cùng tông nền ở mưa ngày và mưa đêm, không thành chuỗi tối | Đạt |
| Mưa ban ngày | cỏ `#80947F`, bão hoà −73%: cả cảnh trắng đục | **Chưa đạt** art bible mục 4 ("không mờ đục") |
| Mưa đêm | cỏ `#3C7463` (độ sáng −11%) | Đạt (sát ngưỡng) |

```
VERDICT: REJECTED
1. pass - không vật nào lệch hay mất
2. pass - thứ tự vẽ và vị trí đúng
3. fail - mưa ban ngày làm cả cảnh trắng đục (cỏ #80947F); đêm và mưa đêm đạt màu
4. pass - không viền thừa
5. pass - không hiệu ứng vẽ chết
6. pass - đèn có vũng sáng, vật có bóng
7. pass - HUD đọc được
```

Đề xuất cho mưa ban ngày: lớp mưa ngày alpha tối đa 0,10 (hiện khoảng 0,13 nhân với lớp ngày), màu `Palette.Mix(Night, Grass, 0.5)` thay xám xanh nhạt; giữ bão hoà cỏ ≥ 70% của `#3D8369`. Nửa dưới vẫn trống là phần bố cục #169.

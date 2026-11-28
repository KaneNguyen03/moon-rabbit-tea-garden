# #187: lối đi cùng tông với nền mọi giờ, đèn đá viền rừng có vũng sáng

Chỉ code. Không thêm hình vẽ, không đổi vật liệu hay cài đặt.

## Đã đổi
| Chỗ | Trước (main `38c6775`) | Sau |
|---|---|---|
| Lối đi đất (`Path`), đá bước (`Stone`), đá lát quanh ấm (`Flagstone`), vòng nét đứt của đất chưa mua (`Dash`), sương đất chưa mở (`Veil`, `Edge`, `Wash`) | Sau #185, màu theo giờ và màu mưa chỉ nằm trên cỏ, nên các mảnh này giữ màu đêm: lối đi luôn là `#537460`, dù là ngày, hoàng hôn hay mưa | Mỗi mảnh phẳng nhận đúng lớp màu theo giờ rồi lớp mưa mà cỏ nhận, trộn trong không gian tuyến tính như màn hình trộn lớp phủ lên cỏ (`SceneOverlays.Washed`). Ban đêm khi trời khô thì không có lớp nào, nên màu giữ nguyên |
| Đèn đá ở viền rừng (`Rim Lantern`) | Không có vũng sáng | Có vũng `#FFC866` alpha 0,45, rộng 300 như hai đèn đá trong vườn. Vũng đi theo đèn khi viền rừng lùi ra, mờ đi cùng đèn và đổi màu theo màu đèn người chơi treo |

## Số đo: lối đi so với nền
Ảnh `tools/shot.sh` 1080×1920, lấy trung vị ô 9×9 trên vệt đất của lối đi và trên cỏ trống cạnh đó.

| Khung | | Lối đi | Nền | ΔL (lối đi − nền) |
|---|---|---|---|---|
| `21-ban-ngay` | main | `#537460` L39,0 S16,6 | `#869A7B` L54,3 S13,3 | −15,3 |
| `21-ban-ngay` | PR | `#8D9478` L52,5 S11,6 | `#869A7B` L54,3 S13,3 | **−1,8** |
| `21-hoang-hon` | main | `#537460` L39,0 S16,6 | `#6D8261` L44,5 S14,5 | −5,5 |
| `21-hoang-hon` | PR | `#77785F` L42,2 S11,6 | `#6D8261` L44,5 S14,5 | **−2,4** |
| `62-mua-ngay` | main | `#537460` L39,0 S16,6 | `#80947F` L53,9 S8,9 | −14,9 |
| `62-mua-ngay` | PR | `#868F7D` L52,5 S7,4 | `#80947F` L53,9 S8,9 | **−1,4** |
| `62-mua-dem` | main | `#537460` L39,0 S16,6 | `#3B7463` L34,3 S32,6 | +4,7 |
| `62-mua-dem` | PR | `#4B6960` L35,3 S16,7 | `#3B7463` L34,3 S32,6 | **+1,0** |

Ban đêm khi trời khô (`2-garden`), ảnh trước và sau giống hệt từng điểm ảnh, trừ vùng 116×104 ở mép trái, nơi đèn đá viền rừng nay có vũng sáng. Khung `115-phut-15-en` khác nhiều giữa hai lần chạy chỉ vì phiên 15 phút giả lập ra thỏ khác nhau; nền và lối đi giống nhau.

## Ảnh
- `21-ban-ngay`, `21-hoang-hon`, `62-mua-ngay`, `62-mua-dem`, `2-garden` (đêm) và `91c-6-doan-duoi` (`-truoc-sau.png`): bên trái là main, bên phải là PR, thu nhỏ 50 %.
- `200-loi-di.png`: cắt 200 % lối đi lúc ban ngày và lúc mưa ban ngày.
- `200-den-vien-rung-2-garden.png`: cắt 200 % đèn đá viền rừng ban đêm, trước và sau.

## visual-critic-gate
```
VERDICT: APPROVED
1. pass - không có khe hay chồng lệch: lối đi, đá bước và đá lát vẫn nằm đúng chỗ và đúng hình như trước; chỉ đổi màu
2. pass - vũng sáng mới nằm ngay dưới chân đèn đá viền rừng; các vũng cũ không đổi chỗ; ảnh đêm khớp từng điểm ảnh ngoài vũng mới
3. pass - lối đi cùng tông với nền ở mọi giờ (ΔL ≤ 2,4 lúc ngày, hoàng hôn và mưa), không còn chuỗi xanh đậm; vũng sáng nhuộm #FFC866; không có màu đen tuyền
4. pass - không có viền trắng hay xám quanh lối đi hay vũng sáng; sprite glow của vũng mờ dần mềm
5. pass - vũng sáng là Image riêng (LanternPool), không vẽ chết vào hình đèn
6. pass - lối đi vẫn đọc được là vệt đất ấm, ở mức một vệt mòn chứ không thành đường kẻ đậm; đèn đá viền rừng có vũng sáng như các đèn khác
7. pass - HUD, thanh tab, bảng tên, "Còn thiếu 120", "Xây · 1,40K" vẫn rõ; không gì bị cắt
```

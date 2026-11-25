# visual-critic-gate — nhánh do-net-do-hoa (#134 phần sương/ánh sáng, #135 bước 1, #129 nền cỏ)

Bằng chứng: ảnh GameView 1080×1920 từ `tools/verify.sh --fast --preview` (GREEN, 1570/1571, 379 khung), 25/11/2026.
- `114-be-ca-vuon-truoc-sau.png`, `91c-0-doan-phai-truoc-sau.png`, `91c-6-doan-tren-truoc-sau.png`, `110b-tab-tram-vi-truoc-sau.png`: trước (main) và sau cùng góc máy.
- `114-crop-200.png`: vùng ấm trà, thỏ, trạm phóng 200 %.
- `114-mep-200.png`: mép vùng chưa mở phóng 200 %.

Vòng 1: REJECTED (mép vùng tối thẳng như hình chữ nhật; đá lối đi đọc như vũng sương; thiếu bóng tiếp đất ở thỏ). Đã sửa: dải chuyển mép 0,12 → 0,28, bóng tối nhẹ hơn, đá màu đặc, bóng Ink 30 % cho đồ đứng trên cỏ và dưới chân thỏ ở trạm.

Vòng 2:

```
VERDICT: APPROVED
1. pass - mép vùng chưa mở chuyển dần khoảng 150 px, không còn đường thẳng; không khe hở giữa các lớp
2. pass - nhãn, bảng Cấp, thẻ "Xếp thỏ +" nằm đúng chỗ ở cả 4 khung
3. pass - màu theo art bible: cỏ #3D8369, nhãn Ink/Night chữ Paper, không đường đen thuần
4. pass - không quầng trắng hay viền rác quanh nhãn, đá, nền cỏ
5. pass - không hiệu ứng vẽ chết vào hình; đom đóm và hơi là lớp riêng
6. pass - đá lát màu đặc; bóng Ink 30 % dưới đồ vật và dưới chân thỏ ở trạm
7. pass - chữ nhãn tối đọc rõ ở 200 %, HUD không bị cắt, thanh tab giữ trạng thái chọn
```

Còn lại cho vòng sau: bảng tỉ lệ đồ vật và vũng sáng đèn (#134), huy hiệu nhãn và chữ bù zoom (#135 bước 2), lối mòn và cỏ hoa rải (#129, chờ hạn mức Gemini), vòng nét đứt ô đất thay bằng hình (#138).

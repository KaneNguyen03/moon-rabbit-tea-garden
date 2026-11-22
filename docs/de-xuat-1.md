# Đề xuất #1 — Mùa trăng mới: làm lại vườn để lấy hệ số vĩnh viễn

Viết 22/11/2026 để chủ project duyệt trước khi code. Lựa chọn đề xuất in đậm; số nào đổi thì chỉ đổi hằng số.

Số liệu đang có: trạm trần cấp 500 (mốc ×2 cuối), 12 ô đất, cấp vườn trần 99, thỏ giữ trong `Rabbits`, vật phẩm trong `Items`, cỏ bốn lá và cà rốt vàng là hai tiền phụ. Mô phỏng người chơi thong thả: cấp vườn 21 và 12 ô đất sau một tuần; chạm trần cấp 500 ở mọi trạm là chuyện của nhiều tuần.

## 1. Điều kiện mở

- **Mở khi cấp vườn đạt 30** (không đợi mọi trạm cấp 500: trạm cuối đắt tới mức người thong thả không bao giờ tới). Người chơi thong thả tới cấp 30 sau khoảng 2–3 tuần; người nhanh sau vài ngày.
- Mỗi mùa sau cần thêm 5 cấp: mùa 2 mở ở cấp 35, mùa 3 ở 40, trần ở cấp 60 (mùa 7). Cấp vườn về 1 khi sang mùa, nên mỗi mùa là một vòng đầy đủ.

## 2. Giữ gì, mất gì

| Giữ | Mất (về đầu) |
|---|---|
| Mọi thỏ, tên, cấp thỏ, mũ, màu nhuộm, sổ thỏ | Cà rốt |
| Mọi vật phẩm (bùa, mũ) và cấp bùa | Cấp trạm (về 0, chỉ ấm trà mở ở cấp 1 như save mới) |
| Cà rốt vàng, chuỗi quà ngày, thành tựu, album ảnh, hồ sơ | Ô đất (về 4) và chỗ nghỉ (về 2); thỏ ở ô mất được đưa về chỗ nghỉ, chỗ nghỉ thiếu thì "ngồi chờ" không mất |
| Cỏ bốn lá đang có | Cấp vườn và điểm kinh nghiệm (về 1) |
| Hướng dẫn đã xong, cài đặt | Khay sản phẩm, lượt trò chơi nhỏ trong ngày |

Giữ thỏ là điểm cốt lõi: hệ số của thỏ bậc cao làm vòng sau nhanh hơn, cộng với hệ số mùa.

## 3. Phần thưởng

- **Hệ số mùa vĩnh viễn: +25 % thu nhập mỗi mùa, cộng dồn tuyến tính** (mùa 1 ×1,25, mùa 4 ×2,0, mùa 7 ×2,75). Tuyến tính để không phá nhịp cấp vườn đã chỉnh; muốn mạnh hơn thì đổi một hằng số `SeasonBonus`.
- Quà khi bắt đầu mùa mới: 3 cỏ bốn lá và 5 cà rốt vàng, cùng một "Thỏ Trăng Non" riêng của mùa (loài chỉ có qua mùa mới, bậc Tím, nghề Ngắm sao) cho lần đầu.
- Huy hiệu mùa trên hồ sơ và cạnh cấp vườn ("Mùa 2"); khung ảnh ghi mùa.

## 4. Màn hình

- Nút "Mùa trăng mới" hiện trong thẻ Tạm nghỉ khi đủ cấp, có huy hiệu. Trước khi làm: một tấm so sánh "giữ / mất / được" với số thật của save, xác nhận hai bước (giữ nút 1,5 giây rồi bấm lại).
- Khoảnh khắc: trăng lặn rồi mọc lại trên vườn, 3 giây, bỏ qua được; vườn về 4 ô với sương như lúc mới.

## 5. Save

- Thêm `Season` (số mùa, mặc định 0) và `SeasonBonus` suy từ nó; save cũ là mùa 0. Không tăng version nếu chỉ thêm trường; test đọc save cũ.
- Hệ số mùa nằm trong `Modifiers.IncomeMultiplier` để mọi nơi (thưởng nhiệm vụ, trò nhỏ, khay) hưởng cùng.

## 6. Test Core

- Đủ cấp thì mở, thiếu thì không; cấp cần tăng 5 mỗi mùa, trần mùa 7.
- Sau khi sang mùa: thỏ và vật phẩm giữ nguyên, cà rốt 0, trạm về đầu, ô 4, chỗ nghỉ 2, thỏ không mất, cấp vườn 1, cỏ và cà rốt vàng cộng quà.
- Hệ số mùa áp vào thu nhập và thưởng; mô phỏng `GardenPacingTests` mùa 1 nhanh hơn mùa 0 nhưng vẫn qua mốc.

## 7. Chủ project cần chốt

- [ ] Mở ở cấp vườn 30 (đề xuất) hay mọi trạm cấp 500.
- [ ] +25 % mỗi mùa, cộng dồn tuyến tính.
- [ ] Ô đất và chỗ nghỉ về đầu (đề xuất) hay giữ.
- [ ] Thỏ riêng của mùa mới: có hay không.

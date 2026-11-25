# Art bible — Vườn Trà Thỏ Trăng

1. **Phong cách:** Truyện tranh đêm trăng: hình tròn mũm mĩm, viền mực tím đậm, giao diện là những tấm thẻ giấy kem có "gờ" dày như nút kẹo.
2. **Bảng màu** (mọi màu trong game lấy từ `Palette.cs`):
   - `#1B1E45` trời đêm — nền trời, lớp phủ tối
   - `#3D8369` cỏ — mặt đất khu vườn
   - `#FFF4E0` giấy kem — thẻ giao diện, lông thỏ
   - `#3A2A4D` mực tím — chữ, viền, bóng đổ
   - `#F6A5B8` má hồng — điểm nhấn phụ, tai thỏ
   - `#6FBF8A` rêu — tiến độ, điều tích cực
   - `#FFC866` ánh đèn lồng — quầng sáng, tiêu đề
   - **Nhấn:** `#FF9B4A` cà rốt — tiền tệ, nút bấm, mọi thứ "bấm được"
3. **Ánh sáng:** Trăng và đèn lồng giấy: những vũng sáng hổ phách ấm trên nền đêm xanh lam mát.
4. **Độ trong của cảnh:** Ảnh phải sắc, không mờ đục. Không chồng quá 2 lớp phủ toàn màn hình (vignette, vân giấy). Lớp phủ nào trong suốt hẳn thì tắt đi. Không dùng alpha để vẽ vật đặc (đá, lối đi, đạo cụ khoá): dùng màu đặc pha sẵn.
5. **Vùng chưa mở: không có sương.** Chủ dự án bỏ sương ngày 25/11 vì nhìn đau mắt. Đất chưa mở chỉ là cỏ chìm trong bóng đêm: một lớp tối đều, mềm, màu `#1B1E45` pha 1/4 `#3D8369`, mép mềm, không vân mây, không mảng sáng, không viền. Không dùng trắng, kem hay xám sáng cho vùng khoá.
6. **Vân giấy:** chỉ làm tối (mực tím), độ đậm 6–8%. uGUI không có chế độ Multiply, nên vân màu kem sẽ phủ một lớp sữa lên cả cảnh.
7. **Điểm nhấn:** màu kem sáng nhất dành cho thỏ, đèn lồng và thẻ giao diện. Nhãn trong vườn nhỏ, ít, không cạnh tranh với thỏ. Hướng thẩm mỹ đầy đủ: `docs/nghien-cuu-thiet-ke-do-hoa.md` mục 4.

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
8. **Đảo 2.5D** (quyết định design 27/11; gate chấm theo đúng các màu này, không theo Night mặc định):
   - Đất đã mở: cỏ `#3D8369` với ô cỏ #129.
   - Vành đất khoá ngay quanh đất đã mở: `#31605C` (cỏ pha 35% Night), rộng khoảng 1,5 m ở hai bên và 2,5 m ở phía sau để vẫn đọc được khi nhìn xiên. Vành chỉ nằm trên mặt đảo, không vượt qua mép đảo: phía trước mép trước là vách đất rồi mặt dưới đảo `#212047` kéo tới đáy màn hình, không thấy cỏ hay vành bên dưới vách.
   - Đất khoá ngoài vành: `#23374E`, đúng màu "Night pha 1/4 cỏ" ở mục 5. Màu đo trên màn hình được phép lệch tới khoảng `#2A3C52` do ánh trăng; nếu sáng hơn thì sửa ánh sáng, không đổi màu này.
   - Mép đảo nhuộm về Night như mọi bóng: môi cỏ `#366F62`, vách đất phía sáng `#60424A`, vách đất phía tối `#453448`, khe tối dưới môi cỏ `#2A2449`, mặt dưới đảo `#212047`. Không dùng nâu gạch.
   - Hình đảo: góc bo bán kính ≥ 4 m, mép trước lượn nhẹ ±0,4 m; camera không cuộn quá mép trước, nên mép trước luôn đọc là mép đảo chứ không phải một sọc ngang.
9. **Độ dày nét** đo trên màn hình ở zoom mặc định: viền ngoài (bóng dáng) 2–3 px, nét bên trong 1–1,5 px. Tay, chân, dụng cụ nằm trước thân là nét bên trong: không viền dày, đầu vai không có nét để tay mọc ra từ thân. Màu nét là mực `#3A2A4D`; gỗ được pha tối đa 30% về màu gỗ. Thỏ rig 512 px vẽ viền ngoài khoảng 7 px ở nguồn.
10. **Bóng tiếp xúc:** elip `shadow_elip` (mực, tâm 60%, mép mềm), vẽ bằng alpha blend chứ không alpha clip, tint trắng, rộng 1,3 lần bề ngang thân hoặc đế.
   - Điểm neo là tâm đế đứng của vật: với thỏ là điểm giữa hai chân; với hình vẽ 3/4 là giữa vùng chân hoặc đế vẽ, tức lùi sau pixel đục thấp nhất khoảng nửa bề sâu đế. Tâm ngang lấy giữa các cột đục ở dải đáy hình, không lấy pivot canvas.
   - Từ điểm neo, đẩy tâm elip về phía camera sao cho khoảng 35% bề sâu elip ló ra trước pixel đáy; phần còn lại nằm dưới vật. Vật không bao giờ lơ lửng trên bóng của nó.
   - Mọi vật đứng trên cỏ hoặc trên đế đều có bóng, kể cả cột đèn, giỏ, nấm, cối. Trên đế tối (`pad_dat`), bóng giữ cùng elip nhưng màu cuối không tối hơn đế pha 50% mực: không có mảng mực đặc.
11. **Lọc ảnh billboard 2.5D:** Bilinear, không mipmap. Gate so A/B ngày 27/11: trilinear có viền răng cưa tối ở mái và cột quầy, bilinear sạch.
12. **Hình cũ chờ thay (#131):** nồi hồng đặt scale theo chiều cao, cao khoảng 1,0 thỏ. Tấm lưng xám sáng của tiệm và cục trắng trên cối không được sáng ngang thỏ: tint dữ liệu 0,8 cho tới khi có hình mới.

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
13. **Một phong cách cho mọi hình 2.5D** (28/11, sau phản hồi "đồ hoạ xấu quá", #168, #169). Thỏ Kem là chuẩn: mọi trạm, đồ, cây, đèn, đế, lối đi vẽ như nó.
   - Màu nước pha gouache kiểu sách tranh: hình khối mũm mĩm bo tròn, 2–3 mảng sáng tối mềm, mép màu loang nhẹ, vân giấy rất nhẹ trong hình.
   - Nét mực mềm `#3A2A4D` có thay đổi độ dày, như thỏ Kem: viền ngoài theo mục 9, nét trong mảnh hơn.
   - Góc nhìn 3/4 từ trên khoảng 30–40° cho hợp camera nghiêng 40°: thấy mặt trên của bàn, nắp, mép đế. Không vẽ nhìn ngang, không vẽ thẳng từ trên (trừ hình nằm đất ở mục 18).
   - Không dùng: hình vector phẳng tô đều (placeholder), highlight trắng bóng như nhựa, gradient kiểu sticker, viền đen hoặc nâu đỏ, bóng đổ vẽ dưới chân (engine đổ, mục 10), hơi, khói, lấp lánh, lá rơi vẽ sẵn (Shuriken, luật nhà số 3), chữ.
   - Một hình đạt khi đặt cạnh thỏ Kem ở cỡ thật không ai đoán được là hai người vẽ.
14. **Màu vật liệu 2.5D.** Mọi màu lấy từ mục 2 hoặc pha từ chúng; vật liệu dùng các cặp sáng/tối dưới đây (đề xuất 28/11, chốt khi tấm mẫu qua gate):
   | Vật liệu | Sáng | Tối | Ghi chú |
   |---|---|---|---|
   | Gỗ | `#A8714F` | `#6E4A44` | tối pha về mực, không nâu đỏ |
   | Đá | `#A79FB0` | `#6E6680` | xám ngả tím, không xám trung tính |
   | Đất, lối đi | `#B98C66` | `#8A6352` | tối hơn kem rõ rệt |
   | Tán lá | `#6FBF8A` | `#2E5E49` | thân màu `#3D8369` |
   | Gốm men | `#4FA79A` | `#2F6E6C` | như ấm trà |
   | Giấy đèn | `#FFF4E0` | `#F2D8A8` | sáng nhất cảnh cùng thỏ |
   | Nước | `#2F4A6E` | `#1E2C52` | ánh trăng phản chiếu `#FFF4E0` |
   - Độ sáng: chỉ thỏ, giấy đèn và thẻ giao diện được tới kem `#FFF4E0`. Trạm, đồ, đá, đế có độ sáng trung bình thấp hơn lông thỏ ít nhất 15 % (ảnh xám), để thỏ là điểm sáng nhất (`de-xuat-2-5d.md` §5).
   - Cà rốt `#FF9B4A` và má hồng `#F6A5B8` chỉ là điểm nhỏ: than bếp, ruy băng, hoa, sọc mái. Không mảng lớn bão hoà (nồi hồng hiện tại là ví dụ sai).
   - Không vẽ tối hình cho "ra đêm": hình vẽ ở ánh sáng trung tính hơi ấm, cảnh (trăng, đèn, hậu kỳ) làm ra đêm.
15. **Bảng tỉ lệ 2.5D** (thỏ = 1, một thỏ = 1,15 m = `rabbitMetres`; chiều cao tính từ chân tới đỉnh, gồm tai):
   | Vật | Cao (thỏ) | Ghi chú |
   |---|---|---|
   | Thỏ | 1,0 | khoảng 142 px trên màn 1080×1920 ở zoom mặc định (FOV 25°, cách 35 m, 124 px/m) |
   | Trạm nhỏ (ấm trên bếp, nồi, cối, luống chè) | 0,8–1,1 | |
   | Đồ cầm tay, giỏ, khay | 0,3–0,5 | |
   | Quầy, tiệm | 1,9–2,1 | đỉnh mái |
   | Đèn lồng cột | 1,5 | đèn treo ngang tầm tai thỏ |
   | Đèn đá | 0,8 | |
   | Bụi | 0,7–0,9 | |
   | Nấm, đá nhỏ | 0,25–0,4 | |
   | Cây | 2,8–3,2 | |
   | Đế trạm (nằm đất) | rộng 1,6 | mọi đế cùng cỡ ± 10 % (#169) |
   | Lối đi (nằm đất) | rộng 0,5 | |
   | Ao (nằm đất) | 2,5 × 1,6 | |
   - Gate đo bằng bounds trong cảnh, sai số ± 15 % (#146).
16. **Độ phân giải và lọc ảnh.** Cùng mật độ điểm ảnh cho mọi hình đứng, để nét và độ mịn giống nhau:
   - Hình đứng: **448 px nguồn cho mỗi một thỏ chiều cao** (thỏ Kem cao 448 px trong khung 512). Cao nguồn = chiều cao theo thỏ × 448, làm tròn lên bội số 4: trạm 1,0 → 448; quầy 2,0 → 896; đèn cột 1,5 → 672; cây 3,0 → 1344; bụi 0,8 → 360.
   - Từ đó viền ngoài ở nguồn là 6–8 px cho mọi hình (7 px × 142/448 ≈ 2,2 px trên màn, đúng mục 9). Hình nhỏ không được vẽ nét dày hơn, hình to không được phóng từ nguồn nhỏ.
   - Hình nằm đất (đế, lối đi, ao, vũng sáng, ô cỏ): 300 px nguồn cho mỗi mét (ô cỏ #129 là 1024 px cho 3,4 m).
   - Không bao giờ phóng to một hình nhỏ hơn cỡ trên: vẽ lại. Thu nhỏ từ ảnh lớn thì được (Lanczos).
   - Nhập: Sprite, Bilinear, không mipmap (mục 11), Clamp, nén High Quality hoặc ASTC 6×6, cạnh tối đa 2048, cạnh bội số 4, pivot chân (Bottom Center) cho hình đứng, Center cho hình nằm đất. Khử viền: màu mép tràn vào vùng trong suốt, không điểm alpha rời, 0 điểm màu nền cũ trên mép.
17. **Ánh sáng vẽ trong hình và ánh sáng cảnh.**
   - Trăng trên-trái (`moonFrom` (-1, -1)): mọi hình vẽ mảng sáng ở trên-trái, mảng tối ở dưới-phải. Một hướng duy nhất cho mọi hình.
   - Không vẽ bóng đổ dưới chân; bóng là elip của engine (mục 10).
   - Chỉ giấy đèn lồng và than bếp được vẽ "tự sáng"; đèn đá phát ánh `#FFC866`, không xanh lá.
   - Đèn lồng hắt **vũng sáng** xuống cỏ: hình nằm đất, cộng màu, tâm kem `#FFF4E0` ra `#FFC866`, alpha tâm tối đa 45 %, mép tắt mềm, bán kính 1,5 thỏ (mẫu `docs/anh/2-5d-mau/vung-sang-den.png`). Vũng phải thấy rõ trên cỏ ở zoom mặc định (#147).
   - Bóng và mặt tối nhuộm về Night, không đen. Vignette ≤ 0,2 màu Night (#148, #168).
18. **Hình nằm đất** (đế trạm, lối đi, ao, vũng sáng, mặt đảo) là quad nằm trên mặt đất, camera tự làm phối cảnh. Vì vậy chúng **vẽ thẳng từ trên xuống**: đế tròn vẽ tròn, không vẽ thành elip 3/4 (đế hiện tại vẽ 3/4 nên bị dẹt hai lần).
   - Đế trạm: một họ, cùng đường kính, cùng độ sáng trung bình (± 8 %), cùng nét mép 6–8 px; chỉ khác chất liệu (đất nện, sàn gỗ, vòng đá, vòng hoa).
   - Lối đi: mảnh cong và mảnh thẳng, mép đất mềm có cỏ lấn, rộng 0,5 thỏ, nối liền mép khi ghép; không phải một cột thẳng (VERDICT spike).
   - Mặt đảo: ô cỏ lặp không đường nối, ít chấm sáng hơn #129 (nhiễu ở zoom mặc định, #168); vách đảo là dải lặp ngang theo màu mục 8.
19. **Quy trình và gate cho mỗi hình mới:** prompt và thông số nằm cạnh hình (`<tên>.prompt.md`) để người khác chạy lại; tấm QA theo `visual-critic-gate` (ca rô, cỏ `#3D8369`, tối `#2B2420`, một ô phóng 400 %) và một khung cảnh 2.5D cỡ thật cạnh thỏ Kem; nguồn ghi trong `design/assets/asset-manifest.md`.

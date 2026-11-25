# Art Bible: Vườn Trà Thỏ Trăng (Moon Rabbit Tea Garden)

> **Status**: Approved (26/11/2026, chủ project duyệt qua session điều phối: "Approve")
> **Owned By**: art-director (session design)
> **Last Updated**: 2026-11-26
> **Art Director Sign-Off (AD-ART-BIBLE)**: CONCERNS, hai việc tiếp theo ở cuối tệp

Tài liệu này theo mẫu studio (`.claude/docs/templates/art-bible.md`). Nó gác cổng trước `/asset-spec`. Chi tiết đã chốt không chép lại ở đây mà dẫn chiếu:
- [`docs/art-bible.md`](../../docs/art-bible.md): bảng màu và luật hình ảnh chi tiết (nguồn gốc của mọi hex).
- [`docs/de-xuat-2-5d.md`](../../docs/de-xuat-2-5d.md): hướng 2.5D chủ project duyệt 26/11.
- [`docs/nghien-cuu-thiet-ke-do-hoa.md`](../../docs/nghien-cuu-thiet-ke-do-hoa.md) §4: định hướng thẩm mỹ và nguồn nghiên cứu.

**Trụ trải nghiệm** (nguồn: [`design/gdd/game-concept.md`](../gdd/game-concept.md) mục Game Pillars; câu Visual Identity Anchor ở đó dẫn nguyên văn mục 1 dưới đây):
- **P1 Yên:** không gì làm chói mắt hay giục giã.
- **P2 Của mình:** vườn và thỏ là của người chơi.
- **P3 Thấy tiến bộ:** mỗi lần ghé thấy điều mới.
- **P4 Sưu tầm nhẹ:** thỏ, món, đồ trang trí.

## 1. Visual Identity Statement

**Một câu:** một trang sách tranh màu nước dưới trăng, đứng trên mặt đất có chiều sâu. Nền đêm sâu và yên, đèn lồng ấm là nơi mắt dừng, mọi thứ đáng chạm đều mũm mĩm, sắc nét và hơi nảy khi được chạm.

- **Hình vẽ tay đứng trên đất thật (P2).** Mọi trạm, đồ và thỏ là hình màu nước 2D dựng đứng trên đảo cỏ 3D, có bóng thật. *Phép thử:* khi phân vân giữa vẽ lại thành mesh 3D hay giữ hình vẽ, chọn giữ hình vẽ; chỉ mặt đất, mép đảo và ánh sáng là 3D.
- **Tối và yên là nền, ấm là điểm (P1).** *Phép thử:* khi phân vân một mảng nên sáng hay tối, nếu nó không phải thỏ, đèn lồng hay thứ cần chạm ngay thì chọn tối hơn.
- **Ít mà rõ (P1, P3).** *Phép thử:* khi phân vân thêm một nhãn, lớp phủ hay hiệu ứng, bỏ đi trước; chỉ thêm khi thiếu nó người chơi không biết bấm vào đâu.

## 2. Mood & Atmosphere

| Trạng thái | Cảm xúc | Ánh sáng | Từ khoá | Năng lượng | Yếu tố mang không khí |
|---|---|---|---|---|---|
| Vườn đêm (mặc định) | yên, ấm áp | trăng xanh mát trên-trái, tương phản vừa; đèn lồng hổ phách là điểm ấm | tĩnh, mềm, ấm, sâu | thấp | vũng sáng đèn lồng trên cỏ |
| Bình minh / hoàng hôn | dịu, chuyển giao | lớp màu hồng-xanh hoặc hổ phách nhẹ (theo `Daylight`), không quá 1 lớp | dịu, ấm, trong | thấp | màu trời và đèn lồng nhạt dần |
| Mưa | ấm cúng trong nhà | lớp xanh xám, vệt mưa mảnh; đèn lồng giữ nguyên | ẩm, êm, gần | thấp | ếch thỏ, mưa trên mái quầy |
| Khoảnh khắc thưởng (mở món, mở đất, thỏ mới) | vui khẽ, tự hào | nền tối đi (scrim), vật được thưởng sáng nhất | bừng, nảy, ngắn | trung bình, 0,6–0,8 s | lấp lánh Shuriken quanh vật |
| Bảng và menu | rõ ràng, bình thản | nền vườn tối đi sau scrim, thẻ kem đặc | gọn, sáng, đọc được | thấp | thẻ giấy kem có gờ |

Các trạng thái phân biệt bằng màu trời và độ sáng tổng, không bằng sương hay lớp phủ trắng (art bible §4–5).

## 3. Shape Language

- **Thỏ:** khối tròn mũm mĩm, tai dài là nét nhận biết; đọc được ở kích thước thu nhỏ 25 %. Mỗi giống khác ở màu lông, đốm và phụ kiện, không đổi dáng thân (P4).
- **Trạm và đồ:** khối bo tròn, đế rộng, đứng chắc trên đất; không góc nhọn. Trạm là hình anh hùng của ô đất; đồ trang trí thấp và nhỏ hơn, lùi về sau.
- **Đất:** đảo cỏ có mép đất dày, viền cỏ rủ, lối mòn cong mềm; không đường thẳng dài (spike 2.5D: lối thẳng đọc như thân cây).
- **Giao diện:** thẻ chữ nhật bo góc lớn có "gờ" dày như nút kẹo; nút cà rốt nổi; chấm tròn cho số đếm. Giao diện cố ý khác thế giới (phẳng, đặc, sạch) để đọc nhanh.
- **Anh hùng và lùi:** thỏ đang làm việc và đèn lồng là anh hùng; nhãn, đá, bụi, cỏ rải lùi về sau (tối hơn, nhỏ hơn).

## 4. Color System

Hex và tên màu ở [`docs/art-bible.md`](../../docs/art-bible.md) §2 và `Assets/VuonTra/Game/UI/Palette.cs`; mục này chỉ ghi vai trò.

| Màu | Vai trò trong thế giới | Ngữ nghĩa |
|---|---|---|
| Night (trời đêm) | bầu trời, bóng đổ, vùng chưa mở | yên, chưa tới |
| Grass (cỏ) | mặt đất | đất của mình |
| Paper (giấy kem) | thỏ, thẻ giao diện | thứ quan trọng, dễ thương |
| Ink (mực tím) | chữ, viền, bóng | cấu trúc, đọc được |
| Lantern (đèn lồng) | quầng đèn, tiêu đề | ấm, nơi mắt dừng |
| Carrot (cà rốt) | tiền, nút bấm | bấm được |
| Moss (rêu) | tiến độ, điều tốt | đang tiến lên |

- **Nhiệt độ:** cảnh lạnh (trăng, Night), điểm nóng ít và nhỏ (Lantern, Carrot). Bóng đổ nhuộm về Night, không đen.
- **Giao diện:** nền kem đặc, chữ Ink; chữ trên cà rốt là Ink (OnCarrot); màu pha `CarrotInk`, `MossInk` cho chữ nhỏ; mọi chữ đạt AA (#121).
- **An toàn mù màu:** cặp dễ lẫn là Moss (tốt) với Carrot (bấm được) và Moss với Grass. Không dùng màu làm tín hiệu duy nhất: tiến độ luôn là thanh có độ dài; "bấm được" luôn có hình nút có gờ và mũi tên ▲; trạng thái khoá có ổ khoá hoặc dấu "?" và chữ, không chỉ xám màu.

## 5. Character Design Direction

- **Thỏ của người chơi:** hình màu nước, viền mực tím mảnh, mắt chấm, má hồng; 76 giống chung dáng, khác màu và phụ kiện. Rig 2D PSB, góc trước và 3/4 (lật gương cho bên phải), pivot ở chân, bóng là lớp riêng (Cream đã có, main `47e6642`).
- **Phân biệt nhanh:** thỏ của mình đeo khăn hoặc phụ kiện đã mua và đứng ở trạm; khách ghé quán có bong bóng gọi món; ếch thỏ chỉ khi mưa. Không phân biệt chỉ bằng màu.
- **Dáng và biểu cảm:** chậm, tròn, có chuẩn bị và đi theo (chuẩn bị 100–200 ms, theo đà 50–150 ms); vòng làm việc bốn nhịp mỗi trạm; không toán hoạt ảnh trong C#.
- **Chi tiết ở tầm camera:** thỏ ở zoom mặc định cao khoảng 110 px trên màn 1080; chi tiết dưới 2 px (vân lông, ngón) không cần. Thỏ phải đủ to để là điểm sáng nhất (spike 2.5D để thỏ quá nhỏ).

## 6. Environment Design Language

- **Kiến trúc:** quầy gỗ mái vải sọc, đèn lồng giấy, sàn gỗ tròn: một khu vườn trà nhỏ đêm trăng, gợi Trung thu Việt Nam (đèn lồng, thỏ ngọc) mà không dùng chữ.
- **Kết cấu:** màu nước và bột màu vẽ tay cho mọi hình; mặt đất dùng ô cỏ #129 lặp liền, tần số trung bình, tương phản thấp. Không PBR, không loang tương phản mạnh.
- **Mật độ đồ:** quanh trạm dày (luống, hàng rào thấp, đồ dùng); giữa bãi thưa; không mảng cỏ trống lớn hơn một ô trạm; vùng chưa mở cỏ tối hơn một bậc, hàng rào thấp, biển gỗ, không sương.
- **Kể chuyện không chữ:** trạm có dụng cụ đúng nghề (cối, ấm, giỏ lá); đèn lồng sáng là chỗ có người; hơi nước và lá bay là hạt Shuriken, không vẽ chết vào hình.

## 7. UI/HUD Visual Direction

- **Không gian:** HUD trên màn hình (screen-space) cho tiền, trà lực, tab; nhãn trạm là World Space Canvas trên trạm với ba bậc của #135 (việc cần làm ngay; chấm số trò nhỏ; thông tin chỉ khi zoom gần).
- **Chữ:** chữ thân ≥ 26, chữ phụ ≥ 22 đơn vị trên màn 1080; đậm cho nút và tiêu đề. Phông riêng có dấu tiếng Việt là #142 (hiện dùng phông hệ điều hành).
- **Biểu tượng:** tròn, viền Ink, chiếm 65–70 % ô; cùng nét với hình thế giới.
- **Chuyển động giao diện:** vào 250–400 ms ease-out, ra 150–200 ms ease-in, nhấn 80–120 ms, nảy 180–250 ms OutBack; giảm chuyển động thì chuyển tức thì.
- **Xung đột đã quyết:** viên nhãn kem đẹp nhưng lấn thỏ, nên nhãn vườn nền mực tối chữ kem và ẩn bớt theo zoom (#135); sương đẹp ở mẫu nhưng đau mắt, nên bỏ (25/11).

## 8. Asset Standards

- **Định dạng:** PNG RGBA, cạnh bội số 4, nền trong suốt sạch (không quầng màu nền cũ, không điểm đen thuần), Bilinear; ô nền lặp (Default, Repeat, không nén) ở `Assets/VuonTra/Art/Ground/`.
- **Tên:** theo quy ước đang dùng: `prop_<id trạm>`, `product_<id trạm>`, `rabbit_<id giống>`, `kind_<id>`, `hat_*`, `bg_*`, `rai_*`, `loi_*` (không đổi sang mẫu `[category]_[name]_[variant]_[size]` để không vỡ `ContentSeeder`).
- **Độ phân giải:** đồ và trạm cạnh dài 512; ô nền 1024 hiển thị ở 512 đơn vị; thỏ theo rig PSB.
- **Billboard 2.5D:** pivot ở chân (Bottom Center), bóng là lớp hoặc quad riêng, không vẽ bóng vào hình; shader Lit alpha clip.
- **Mặt đất 3D:** mesh đảo dựng bằng script (Blender 5.3 hoặc code), không AI 3D trả phí (đã huỷ).
- **Giới hạn kỹ thuật:** tối đa 2 lớp phủ toàn màn hình; một đèn có bóng (trăng); ngân sách draw call và fps chốt ở #149.

## 9. Reference Direction

| Nguồn | Lấy gì | Tránh gì |
|---|---|---|
| Tranh màu nước minh hoạ sách thiếu nhi về động vật nhỏ (kiểu Beatrix Potter) | nét viền mảnh, mảng màu nước trong hình, thú mũm mĩm | độ tả thực của lông, bảng màu ban ngày |
| Tranh nền phim hoạt hình vẽ tay về vườn đêm | ánh đèn ấm trong đêm lạnh, bóng nhuộm xanh | chi tiết dày, phối cảnh rộng |
| Đèn lồng giấy và thỏ ngọc Trung thu Việt Nam | hình đèn lồng, màu hổ phách, hoạ tiết trăng | chữ, hoạ tiết cầu kỳ trên mọi bề mặt |
| Thể loại idle quản lý quán có nhân vật làm việc ở trạm | vòng làm việc đọc được từ xa, camera nghiêng cố định | ngoại hình, nhân vật, tên, giao diện của bất kỳ game cụ thể nào (không mô phỏng Cats & Soup) |

## Ký duyệt AD-ART-BIBLE (tự chấm theo `.claude/docs/director-gates/ad-art-bible.md`)

**CONCERNS**, giữ làm việc tiếp theo sau khi duyệt (trụ trải nghiệm đã khớp `design/gdd/game-concept.md`, gỡ khỏi danh sách):
- Ngân sách hiệu năng theo #149, máy yếu nhất là iPhone 11 và Android Snapdragon 665: §8 thêm số draw call và bộ nhớ khi #149 đo xong.
- Phông chữ riêng (#142) chưa có; §7 dựa trên phông hệ điều hành.
- Phần còn lại nhất quán: màu (§4) phục vụ không khí đêm yên (§2); hình khối (§3) theo câu nhận diện (§1); tiêu chuẩn (§8) khớp hướng 2.5D đã duyệt.

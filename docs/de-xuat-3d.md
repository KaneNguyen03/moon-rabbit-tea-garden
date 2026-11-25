# Đề xuất 3D: phụ lục thiết kế

Chủ project chọn "Full 3D" ngày 25/11/2026. Kế hoạch chính là [KE-HOACH-CHUYEN-3D.md](KE-HOACH-CHUYEN-3D.md) (kiến trúc, pipeline AI 3D cộng chỉnh tay, các giai đoạn 5.0–5.8). Tài liệu này không lặp lại kế hoạch đó. Nó trả lời năm câu PO hỏi session design và chốt hướng mỹ thuật 3D để mọi phiên làm theo.

Hướng B (3D hẳn) thay cho rig 2D PSB và cho đề xuất 2.5D. Không mô phỏng ngoại hình, tên hay giao diện của Cats & Soup: chỉ học cảm giác thể loại (camera nghiêng cố định, đồ vật mũm mĩm, ánh sáng ấm).

## 1. Pipeline tạo mesh và chi phí

### Hiện trạng máy (đo 25/11)

| Mục | Trạng thái |
|---|---|
| Blender | **Chưa cài.** `brew install --cask blender` (khoảng 1 GB). |
| Ổ đĩa | **Còn 17 GB, đã dùng 96 %.** Mỗi worktree Unity có `Library` khoảng 1,7 GB. Phải dọn worktree cũ trước khi mở `../VuonTraNam-3d`. |
| Máy | Apple M4, 16 GB RAM. Đủ cho Blender và một batch Unity cùng lúc. |
| Unity | 6000.6.10f1, URP 17.6.0. Chưa có Cinemachine, ProBuilder. Shader Graph đi kèm URP. |

### Nguồn miễn phí làm được trên máy này

| Nguồn | Hợp với | Giới hạn |
|---|---|---|
| **Blender + script Python** (`tools/blender/`) dựng low-poly từ khối cơ bản theo bản vẽ 2D có sẵn | trạm, đồ đạc, cây, đèn, đá, đất có độ dày | Thỏ cần dáng thân đẹp và vòng cạnh để gập: script dựng được bản thô, phải chỉnh tay |
| **Unity ProBuilder** (package miễn phí) | mặt đất, sàn gỗ, bậc, hàng rào, khối đơn giản | Không hợp với hình hữu cơ (thỏ, cây tròn) |
| Mesh tay trong Blender | thỏ, dáng thân | Tốn công nhất |

Bản vẽ 2D hiện có (`Assets/VuonTra/Art/Final`, `docs/anh/po-*`) giữ làm tham chiếu dáng và bảng màu cho texture 3D.

### Dịch vụ AI 3D trả phí (chưa dùng, chờ chủ project đồng ý rõ)

| Dịch vụ | Giá | Ghi chú |
|---|---|---|
| Tripo API | trả theo lượt, 1 credit = 0,01 USD. Ảnh sang 3D có texture 30 credit (0,30 USD); low-poly thông minh +10; auto rig 25 | Không cần gói tháng. [Bảng giá API](https://developers.tripo3d.ai/en/pricing) |
| Meshy | Pro 20 USD/tháng, 1.000 credit; ảnh sang 3D có texture 30 credit (khoảng 33 mô hình/tháng); auto rig 5 credit | API từ gói Pro. [Tổng hợp giá](https://www.subscriptionplaybook.com/pricing-teardowns/meshy/) |
| SpriteCook | không có mesh 3D | Không dùng cho việc này |

Ước chi phí theo số mô hình của kế hoạch §4.1 (4 dáng thân, 16 trạm, 12 đồ đội, khoảng 20 đồ trang trí, mỗi cái khoảng 3 lần sinh): khoảng 150 lượt, **45–60 USD** qua Tripo API, hoặc 3–5 tháng gói Meshy Pro. Nguồn giá là trang của hãng và trang tổng hợp ngày 25/11; kiểm lại trước khi mua.

**Đề xuất:** đồ cứng (trạm, đồ đạc, đất) làm miễn phí bằng script Blender và ProBuilder; chỉ dùng AI 3D cho dáng thân thỏ và đồ hữu cơ khó, nếu chủ project duyệt khoản chi.

## 2. Kiến trúc cảnh

- **Renderer:** `Assets/Settings/UniversalRP.asset` hiện chỉ có một renderer là `Renderer2D`. Cần thêm một `UniversalRendererData` (Forward+) làm renderer mặc định cho scene 3D. Renderer 2D giữ lại cho bản 2D đang phát hành tới khi qua mục 5.8 của kế hoạch.
- **Camera:** Cinemachine 3, perspective, FOV khoảng 25°, nghiêng cố định khoảng 40°, không xoay. Kéo có quán tính và dừng mềm ở biên (`CinemachineConfiner3D`).
- **Giao diện:** uGUI giữ nguyên. Camera UI là overlay xếp chồng trên camera thế giới, hoặc canvas Screen Space Overlay. Bảng trạm và nhãn là World Space Canvas trên prefab.
- **Bản đồ #91:** cùng scene 3D, một camera trực giao nhìn thẳng xuống, render vào `RenderTexture` cho màn bản đồ. Kéo bản đồ là kéo camera này trong giới hạn của mặt đất, không cần dựng lại bản đồ 2D riêng.
- **Ánh sáng:** một directional "trăng" có bóng mềm, các đèn lồng là point light không bóng kèm quầng cộng màu, light probe và lightmap nướng sẵn cho cảnh tĩnh.
- **Hậu kỳ:** tonemapping Neutral, bloom chỉ cho đèn lồng (threshold khoảng 1), vignette khoảng 0,2. Không độ sâu trường ảnh ở zoom mặc định (làm mờ là đúng thứ chủ project đã chê).

## 3. Hướng mỹ thuật 3D (chuẩn để chấm mọi ảnh 3D)

Câu định hướng giữ nguyên ([nghiên cứu §4](nghien-cuu-thiet-ke-do-hoa.md)): một trang sách tranh dưới trăng, nền đêm sâu và yên, đèn lồng ấm là nơi mắt dừng, mọi thứ đáng chạm đều mũm mĩm, sắc nét, hơi nảy.

| Thành phần | Chuẩn |
|---|---|
| Hình khối | Low-poly bo tròn, cạnh vát lớn, không góc nhọn. Thỏ 1.500–3.000 tam giác, trạm ≤ 2.500, đồ nhỏ ≤ 800 |
| Toon | Ramp 2 bậc lấy từ `Palette`: mặt sáng là màu gốc, mặt tối là màu gốc pha 35 % `Night`. Không gradient mềm kiểu PBR |
| Viền | Inverted hull màu `Ink #3A2A4D`, dày khoảng 2 px ở bề ngang 1080, theo không gian màn hình. Không viền đen thuần |
| Ánh trăng | Directional từ trên-trái, cao khoảng 50°, màu (0,82; 0,90; 1,0), bóng mềm. Bóng đổ nhuộm về `Night`, không đen |
| Đèn lồng | Point light màu `Lantern #FFC866`, tầm khoảng 3 m, quầng sprite cộng màu. Là điểm ấm duy nhất cạnh thỏ |
| Đất | Đảo cỏ có mép đất dày, viền cỏ rủ. Cỏ dùng texture ô lặp của #129 (`Art/Ground/bg_grass.png`) làm albedo |
| Vùng chưa mở | Không sương (art bible §5). Cỏ tối hơn một bậc, hàng rào thấp và biển gỗ |
| Vân giấy | Pass toàn màn hình kiểu **Multiply** 6–8 % (3D renderer làm được blend Multiply, khác uGUI) |
| Nhãn | Giữ ba bậc của #135: việc cần làm ngay, chấm số trò nhỏ, thông tin chỉ ở zoom gần. Chữ ≥ 22 đơn vị ở zoom mặc định |
| Giao diện | Giữ thẻ kem có gờ, nút cà rốt, bảng màu và luật tương phản AA đã đạt ở #121 |

Bài kiểm trước khi đóng một việc 3D: ảnh xám thu 25 % thì thỏ đang làm việc là điểm sáng nhất; không quá 2 lớp phủ toàn màn hình; không vùng trắng đục; viền đều nhau trên mọi vật; mọi vật đứng đất có bóng.

## 4. Lộ trình di trú, mỗi bước chơi được

Ngày công là công của session design và một phiên code, đã gồm verify và QA. Khớp với các giai đoạn của kế hoạch §5.

| Bước | Người chơi thấy | Việc chính | Ước công | Khớp kế hoạch |
|---|---|---|---|---|
| a. Đất và camera 3D | Vườn có chiều sâu, mép đất, bóng thật; trạm và thỏ vẫn là sprite 2D dựng đứng tạm | Renderer 3D, Cinemachine, mặt đất mesh, chạm bằng `PhysicsRaycaster`, nối Core | 6–9 ngày | 5.0 phần khung |
| b. Ba trạm mẫu | Trà sữa, hái chè, pha trà là mesh 3D; mỗi trạm có vòng làm việc 4 nhịp (ví dụ múc lá, rót nước, hơi bay, rót chén) | Trạm bằng script Blender/ProBuilder, clip đạo cụ, World Space Canvas cho bảng | 10–14 ngày | 5.0 bản mẫu |
| c. Thỏ 3D | Một thân chung, 76 giống là material và phụ kiện, không 76 mesh; Animator với idle, làm việc, đi, ngủ | 3–4 dáng thân, bộ xương chung, `RabbitKindLook`, chân dung nướng sẵn cho thẻ | 15–20 ngày | 5.2 |
| d. Đồ vật và cảnh | Cây, đèn, ao, hàng rào, 13 trạm còn lại | Hàng loạt bằng script, prefab, pool hạt Shuriken | 15–20 ngày | 5.3, 5.4 |
| e. Hiệu năng mobile | 60 fps trên máy yếu nhất chủ project chọn | Gộp draw call (SRP Batcher, atlas material), LOD 2 mức, giới hạn bóng 1 đèn, đo trên máy thật | 5–8 ngày | 5.6 |

Bị bỏ: rig 2D PSB (`../VuonTraNam-rig`), phần dựng thế giới bằng uGUI trong `GardenScreen.cs`, toán hoạt ảnh trong code (baseline `procedural-animation` của phần thế giới về 0). Giữ: Core và 701 test, toàn bộ giao diện, bảng màu, art bible, hình 2D làm tham chiếu và icon giao diện, ô cỏ #129 làm texture.

Việc 2D đang dở của session design: #135 bước 2 (nhãn theo bậc và zoom) vẫn gộp vì bản 2D còn phát hành tới khi 3D qua 5.8, và luật ba bậc chuyển nguyên sang World Space Canvas.

## 5. Tấm mẫu và spike

- Hai tấm mẫu (vườn ngày 1 nhìn nghiêng, trạm pha trà phóng to có thỏ 3D) ở `docs/anh/de-xuat-3d/`, chấm `visual-critic-gate` ở `VERDICT.md` cùng thư mục.
- Spike `../VuonTraNam-3d`, nhánh `spike-3d`: camera nghiêng, mặt đất mesh, một trạm ProBuilder, một thỏ thô (capsule và tai) có Animator một clip idle, trăng và bóng, chụp GameView. Không gộp. Chỉ dựng sau khi dọn ổ đĩa (mục 1) và khi có lượt Unity.

## 6. Chủ project cần quyết

1. **Chi cho AI 3D:** dùng Tripo API theo lượt (ước 45–60 USD cho bộ đầu) hay Meshy Pro, hay chỉ làm miễn phí bằng Blender (chậm hơn ở phần thỏ).
2. **Cài Blender và dọn ổ đĩa:** đồng ý cài Blender và xoá các worktree đã gộp (`../VuonTraNam-1xx`) để có chỗ cho `../VuonTraNam-3d`.
3. Các mục còn treo ở kế hoạch §7: phát hành 2D trước hay hoãn, DOTween hay `Tweens`, số dáng thân, máy yếu nhất phải chạy 60 fps.

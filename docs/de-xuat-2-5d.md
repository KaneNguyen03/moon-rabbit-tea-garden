# Đề xuất 2.5D

Chủ project chốt ngày 25/11/2026: "bỏ 3D mà quay lại 2.5D thôi nha" (sau khi xem spike 3D: "xấu xấu. 2.5D thôi"). Hạn mức AI 3D trả phí đã huỷ ("Cancel it"). Tài liệu này thay cho `de-xuat-3d.md` và `KE-HOACH-CHUYEN-3D.md`.

Học cảm giác thể loại (camera nghiêng cố định, đồ vật mũm mĩm đứng trên đất có chiều sâu, ánh sáng ấm), không mô phỏng ngoại hình, tên hay giao diện của Cats & Soup.

## 1. Người chơi thấy gì

Khu vườn là một hòn đảo cỏ có mép đất dày, nhìn từ trên xuống nghiêng khoảng 40°. Trạm, cây, đèn và thỏ vẫn là chính các hình vẽ màu nước hiện có, nhưng đứng thẳng trên mặt đất và đổ bóng mềm xuống cỏ theo ánh trăng. Đèn lồng có quầng sáng ấm thật trên mặt đất. Giao diện (HUD, thẻ, thanh tab) giữ nguyên.

## 2. Giữ, làm lại, bỏ

| | Phần |
|---|---|
| **Giữ** | Core và toàn bộ test; toàn bộ giao diện uGUI; mọi hình vẽ 2D (trạm, đồ, thỏ, cây, đèn); bảng màu, art bible; ô cỏ #129 làm texture mặt đất; nhãn ba bậc #135 (chuyển sang World Space Canvas); rig thỏ 2D PSB nếu chủ project mở lại |
| **Làm lại** | Phần dựng thế giới trong `GardenScreen.cs` (từ `RectTransform` sang scene có camera phối cảnh); chạm bằng `PhysicsRaycaster`; bản đồ #91 |
| **Bỏ** | Thỏ mesh 3D, bộ xương 3D, AI 3D trả phí; lớp vân giấy uGUI (thay bằng pass toàn màn hình) |

## 3. Kiến trúc cảnh

- **Renderer:** `Assets/Settings/UniversalRP.asset` hiện chỉ có `Renderer2D`. Thêm một `UniversalRendererData` (Forward+) làm renderer cho scene vườn 2.5D; giữ `Renderer2D` cho bản 2D đang phát hành tới khi 2.5D thay được.
- **Camera:** perspective, FOV khoảng 25°, nghiêng cố định khoảng 40°, không xoay. Kéo có quán tính, chụm để zoom, dừng mềm ở biên. Cinemachine 3 nếu cần confiner, không bắt buộc.
- **Mặt đất:** mesh đảo cỏ có mép đất và viền cỏ rủ, dựng bằng script Blender (đã cài 5.3) hoặc ProBuilder. Mặt trên dùng ô cỏ #129, lặp sao cho nét cỏ sắc ở zoom mặc định. Lối mòn #129 là quad phẳng có texture trên mặt đất.
- **Hình dựng đứng (billboard):** mỗi trạm, đồ, cây, đèn, thỏ là một quad mang sprite hiện có, đứng thẳng, pivot ở chân, quay về camera quanh trục dọc (không ngả theo góc nghiêng, để đáy luôn chạm đất). Shader Lit có alpha clip để nhận ánh sáng và đổ bóng.
- **Bóng:** directional "trăng" có bóng mềm. Sprite đổ bóng qua alpha clip; nếu bóng sprite răng cưa thì dùng một quad bóng mờ hình elip nằm phẳng dưới chân (Ink 30 %), như bóng 2D hiện nay. Chọn bằng ảnh so sánh ở spike.
- **Đèn lồng:** point light `Lantern #FFC866` tầm khoảng 3 m, không bóng, kèm quầng sprite cộng màu; bloom nhẹ.
- **Hậu kỳ:** tonemapping Neutral, bloom ngưỡng khoảng 1 chỉ cho đèn, vignette khoảng 0,2. Không độ sâu trường ảnh ở zoom mặc định. Vân giấy là pass toàn màn hình kiểu Multiply 6–8 %.
- **Bản đồ #91:** cùng scene, một camera trực giao nhìn thẳng xuống render vào `RenderTexture` cho màn bản đồ.
- **Nhãn:** World Space Canvas trên prefab trạm, giữ ba bậc của #135 và chữ ≥ 22 đơn vị ở zoom mặc định.

## 4. Thỏ 2D trong 2.5D

- Thỏ là billboard quay về camera quanh trục dọc, pivot ở chân, đứng trên mặt đất 3D bằng toạ độ đất, không treo theo màn hình.
- Bóng: quad bóng elip riêng nằm trên đất dưới chân, co giãn nhẹ khi thỏ nhún (bằng clip, không toán C#). Bóng sprite theo đèn trăng chỉ dùng nếu ảnh spike cho thấy đẹp hơn.
- Hai yêu cầu cho pipeline thỏ 2D: (1) góc nhìn trước và 3/4, hoặc lật gương, đủ cho camera nghiêng cố định; (2) pivot chân và bóng tách riêng khỏi hình thỏ.
- Hoạt ảnh vẫn là Animator và clip 2D (SpriteResolver hoặc rig PSB nếu mở lại), áp lên billboard.

## 5. Chuẩn mỹ thuật 2.5D (chấm mọi ảnh)

| Thành phần | Chuẩn |
|---|---|
| Hình | Giữ nguyên hình vẽ màu nước, viền mực tím; không vẽ lại thành 3D |
| Đất | Đảo cỏ có mép đất dày, viền cỏ rủ; cỏ #129 sắc, không loang |
| Vùng chưa mở | Không sương (art bible §5): cỏ tối hơn một bậc, hàng rào thấp, biển gỗ |
| Ánh sáng | Trăng trên-trái, bóng mềm nhuộm về `Night`, không đen; đèn lồng là điểm ấm duy nhất cạnh thỏ |
| Phủ toàn màn hình | Tối đa 2 lớp (vignette, vân giấy) |
| Điểm nhìn | Ảnh xám thu 25 %: thỏ đang làm việc là điểm sáng nhất |
| Giao diện | Giữ thẻ kem có gờ, nút cà rốt, tương phản AA đã đạt ở #121 |

## 6. Lộ trình, mỗi bước chơi được

| Bước | Người chơi thấy | Việc chính | Ai | Ước công |
|---|---|---|---|---|
| 1. Đất và camera | Vườn có chiều sâu, mép đất, camera nghiêng, kéo và zoom như cũ | Renderer 3D, scene, camera, mặt đất mesh, chạm, nối Core; bản đồ #91 qua RenderTexture | B code, design mesh và texture | 6–9 ngày |
| 2. Trạm và đồ dựng đứng | Mọi trạm, đồ, cây, đèn đứng trên đất với hình hiện có; nhãn ba bậc | Billboard prefab, World Space Canvas, bảng tỉ lệ theo chiều cao thỏ | B code, design tỉ lệ và bố cục | 6–8 ngày |
| 3. Ánh sáng và bóng | Bóng mềm theo trăng, đèn lồng có quầng ấm thật | Directional, point light, bóng sprite hoặc quad bóng, light probe | Design | 4–6 ngày |
| 4. Hậu kỳ | Ảnh trong, ấm, có vân giấy | Volume, pass vân giấy Multiply, bloom đèn | Design | 2–3 ngày |
| 5. Hiệu năng mobile | 60 fps trên máy yếu nhất chủ project chọn | SRP Batcher, atlas sprite, giới hạn bóng, đo máy thật | B | 4–6 ngày |

Thỏ 2D đi theo bước 2; rig thỏ là việc của pipeline thỏ 2D, chạy song song.

## 7. Hiện trạng máy (25/11)

Blender 5.3 đã cài. Ổ đĩa còn khoảng 39 GB sau khi dọn worktree. Unity 6000.6.10f1, URP 17.6.0; chưa có Cinemachine, ProBuilder. Không dùng dịch vụ AI 3D trả phí.

## 8. Tấm so sánh

Spike 2.5D trên nhánh `spike-3d` (`dfdc8b1`, không gộp): [so-sanh-2d-25d.png](anh/de-xuat-2-5d/so-sanh-2d-25d.png), chấm `visual-critic-gate` ở [VERDICT.md](anh/de-xuat-2-5d/VERDICT.md). Chủ project duyệt hướng 2.5D theo tấm này ngày 26/11 ("Approve"). Issue theo năm bước ở mục 6 mang nhãn `do-hoa` + `2-5d`, thêm `cats-and-soup` cho phần code B làm.

Những chỗ spike còn thiếu, đã đưa vào tiêu chí của bước tương ứng: lối đi thẳng như thân cây (bước 1), thỏ nhỏ nên chưa là điểm nhìn chính (bước 2), đèn lồng chưa hắt sáng xuống cỏ (bước 3), trời trống phía trên và chưa có HUD trên cảnh (bước 4).

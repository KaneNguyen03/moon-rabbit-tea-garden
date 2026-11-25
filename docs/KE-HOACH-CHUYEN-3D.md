# Kế hoạch chuyển Vườn Trà Thỏ Trăng sang 3D

Chủ project chốt ngày 25/11/2026: chuyển hẳn khu vườn và thỏ sang 3D, làm đúng cách, không workaround. Quyết định này thay cho hướng rig 2D PSB chốt ngày 24/11.

Tài liệu này mô tả đích đến, kiến trúc, các giai đoạn, luật mới và những việc chủ project còn phải quyết.

## 1. Đích đến

- Thỏ là mô hình 3D ít đa giác, tô toon shader theo [art bible](art-bible.md): hình tròn mũm mĩm, viền mực tím `#3A2A4D`, bóng hai bậc, vân giấy kem.
- Khu vườn là một scene 3D thật, camera cố định góc chéo, kéo và zoom có quán tính.
- Mọi chuyển động đều làm bằng Animator và clip làm tay. Hiệu ứng là Shuriken, phản hồi giao diện dùng tween. Không có dòng toán hoạt ảnh nào trong C#.
- 76 giống thỏ dùng chung **một bộ xương** và vài dáng thân. Mỗi giống chỉ khác nhau ở material, không cần vẽ lại.
- Giao diện (HUD, thẻ, sheet, cửa hàng) vẫn là uGUI, chỉ không còn vẽ thế giới nữa.

Lấy mô hình thể loại (nhân vật làm việc ở trạm trong idle game), không mô phỏng ngoại hình, tên gọi hay giao diện của Cats & Soup.

## 2. Hiện trạng cần thay

| Phần | Hiện nay | Sau khi chuyển |
|---|---|---|
| Core (`Assets/VuonTra/Core`, 60 file, 701 test) | C# thuần | **Giữ nguyên**, không sửa |
| Khu vườn | uGUI dựng bằng code: `GardenScreen.cs` 8.619 dòng, `StationActorView.cs` 528 dòng, `Image` và `RectTransform` | Scene 3D, prefab trạm và thỏ |
| Hình thỏ | `GameCatalog.KindArt`: mỗi giống một `Sprite` | `RabbitKindLook` (ScriptableObject): dáng thân + material |
| Đồ đội | Sprite icon đặt lên đầu bằng toạ độ (`RabbitGridView.PlaceHat`) | Prefab gắn vào socket xương |
| Hoạt ảnh | Toán trong code: baseline `procedural-animation` khoảng 250 chỗ (`GardenScreen.cs` 117, `Sfx.cs` 49) | Animator; các mục baseline của phần thế giới về 0 |
| Hiệu ứng nền | `FirefliesView`, `LeavesView` tự tính chuyển động | ParticleSystem |
| Prototype PSB | `../VuonTraNam-rig` | Ngừng; chỉ giữ lại bài học về clip nghề |

## 3. Kiến trúc

### 3.1 Tách thế giới khỏi giao diện

Thêm assembly mới `VuonTra.World` (`Assets/VuonTra/World/`). Assembly này tham chiếu `VuonTra.Core` và **không** tham chiếu `VuonTra.Game.UI`.

```
Core  ──events──▶  World (scene 3D: trạm, thỏ, đồ trang trí, camera, VFX)
  │                   │ sự kiện chạm (StationTapped, RabbitTapped...)
  └──events──▶  Game.UI (HUD, sheet, thẻ)  ◀──────┘
```

- `GardenWorld` là gốc của scene. Nó nhận trạng thái Core qua event, sinh prefab trạm và thỏ từ dữ liệu, rồi phát event chạm ra ngoài. Không dùng `Find`, không tìm kiếm scene; mọi tham chiếu nối bằng `[SerializeField]` hoặc truyền vào lúc dựng.
- Kênh sự kiện giữa World và UI là ScriptableObject event channel (skill `unity-developer`). UI không bao giờ chạm trực tiếp vào transform của thế giới.
- `GardenScreen.cs` được tách dần: phần nào vẽ thế giới thì chuyển sang World rồi xoá; phần HUD ở lại, mỗi phần một file. Không để hai bản vẽ cùng một thứ.

### 3.2 Dữ liệu

| ScriptableObject | Chứa |
|---|---|
| `StationData` (đã có, 16 bản) | Thêm: prefab trạm 3D, `AnimatorOverrideController` của nghề, prefab dụng cụ |
| `RabbitKindLook` (mới, 76 bản) | Dáng thân (mesh), material, màu tai và má |
| `ItemData` (đã có, 12 bản) | Thêm: prefab 3D, socket (`Head`, `Neck`, `Back`), độ lệch |
| `RabbitRigProfile` (mới, 1 bản) | Tên xương socket, tên tham số Animator, thời gian crossfade |

Material của 76 giống được **sinh bằng editor script** từ bảng màu trong `RabbitKindLook`, lưu thành asset, giữ tương thích SRP Batcher. Không dùng `MaterialPropertyBlock` lúc chạy.

### 3.3 Thỏ

- **Một bộ xương Generic** khoảng 24–30 xương: hông, cột sống, đầu, mỗi tai 3 xương, tay, chân, đuôi, `Grip` để cầm dụng cụ, cùng các socket `Head`, `Neck`, `Back`.
- **3–4 dáng thân** (tròn, tai cụp, lông xù, nhỏ) chung bộ xương nên dùng chung mọi clip.
- Giới hạn: ≤ 3.000 tam giác, 1 material, ≤ 4 xương ảnh hưởng mỗi đỉnh, bật GPU skinning.
- `RabbitActor` (MonoBehaviour) chỉ làm ba việc: đặt material theo giống, gắn đồ theo event của Core, đặt tham số Animator. Không có `Update()`.

### 3.4 Hoạt ảnh

- Một `AnimatorController` chung gồm các trạng thái `Idle`, `Work`, `Walk`, `Doze`, `Yawn`, `Cheer` (khi được chạm hay lên cấp), `Carry`.
- Mỗi nghề dùng một `AnimatorOverrideController` chỉ thay clip `Work`. 16 trạm nhưng khoảng 6–8 nghề (pha trà, nướng bánh, hái, ngắm sao, giã, câu...).
- `Idle` là sub-state có 3–4 biến thể chọn ngẫu nhiên, tham số `CycleOffset` gán ngẫu nhiên lúc sinh để các con không cử động cùng nhịp.
- Crossfade 0,2–0,3 giây, đặt trong transition, không đặt trong code.
- Clip làm tay trong Blender theo nguyên lý hoạt hình: chuẩn bị, theo đà ở tai và đuôi, ease. Không dùng clip tuyến tính hay mocap.
- `Animator.cullingMode = CullUpdateTransforms` cho thỏ ngoài màn hình.

### 3.5 Hình ảnh

- URP 17 (đã có), Forward+ để có nhiều đèn lồng.
- Shader Graph `ToonRabbit` và `ToonProp`: ramp 2 bậc lấy từ `Palette`, rim ánh hổ phách, nhận bóng từ đèn trăng.
- Viền mực tím bằng **inverted hull**: pass thứ hai trong Renderer Feature, độ dày theo không gian màn hình.
- Vân giấy là một fullscreen pass (Renderer Feature), thay cho `PaperGrain.cs`.
- Ánh sáng: một directional "trăng" có bóng, đèn lồng là point light không bóng kèm quầng sprite cộng màu. Light probe và lightmap nướng sẵn cho cảnh tĩnh.
- Post-processing Volume: tonemapping nhẹ, bloom cho đèn lồng, vignette.

### 3.6 Camera và chạm

- Thêm Cinemachine 3. Camera perspective FOV hẹp khoảng 25°, góc nghiêng cố định khoảng 40°.
- Kéo có quán tính, `CinemachineConfiner3D` giữ trong vườn, chụm hai ngón để zoom và dừng mềm ở biên.
- Chạm dùng Input System cùng `PhysicsRaycaster` trên camera. Trạm và thỏ có collider và `IPointerClickHandler`. Không tự raycast trong `Update()`.
- Bảng cấp và thanh tiến độ của trạm là World Space Canvas trên prefab trạm. Góc camera cố định nên không cần billboard.

### 3.7 Thẻ thỏ trong giao diện

- Lưới và album: **ảnh chân dung nướng sẵn** cho 76 giống, chụp bằng editor script `RabbitPortraitBaker` với cùng shader, ánh sáng và góc. Đồ đội vẫn chồng icon lên như hiện nay.
- Trang chi tiết một con: `RenderTexture` vẽ trực tiếp con thỏ đó cùng đồ đang đội, bằng một camera riêng chỉ bật khi trang mở.

### 3.8 Hiệu ứng và phản hồi

- Đom đóm, lá rơi, hơi nước, lấp lánh khi thu tiền là ParticleSystem gắn trên prefab, có pool.
- Nút nảy và số bay dùng tween. Chủ project còn phải chọn giữa DOTween và `Tweens` tự viết (mục 7).

## 4. Pipeline nội dung

1. Blender → FBX, một file cho mỗi dáng thân, một file clip cho mỗi nhóm nghề.
2. `RabbitModelPostprocessor` (AssetPostprocessor trong `VuonTra.Editor`) ép cấu hình import: scale 1, rig Generic với avatar chung, nén animation Optimal, bỏ material nhúng.
3. Script Blender (`tools/blender/`) xuất hàng loạt và kiểm tra tên xương, số tam giác, trọng số.
4. **Test EditMode kiểm nội dung**: mọi giống có `RabbitKindLook` và material; mọi trạm có prefab và override controller; mọi đồ có prefab và socket có thật trên bộ xương; mọi clip `Work` đặt đúng tên. Thiếu thì test đỏ, không có giá trị dự phòng lặng lẽ.
5. Ảnh QA: mỗi giai đoạn hình ảnh xuất GameView và tờ so sánh qua skill `visual-critic-gate`.

### 4.1 Nguồn mô hình: AI 3D cộng chỉnh tay (chủ project chọn 25/11)

Nhờ thiết kế "một bộ xương, màu theo material", AI 3D chỉ phải làm số mô hình nhỏ: 3–4 dáng thân thỏ, 12 món đồ, 16 trạm và đồ trang trí. 76 giống thỏ không cần mô hình riêng.

| Bước | Công cụ | Ai làm | Kết quả |
|---|---|---|---|
| 1. Tranh ba mặt (trước, nghiêng, sau, tư thế chữ A) | Antigravity (`agy`), theo art bible | Claude, chủ project duyệt | PNG trong `docs/anh/3d/tranh-ba-mat/` |
| 2. Ảnh sang 3D | Tripo (API, chế độ nhiều góc nhìn từ tranh ba mặt) | Claude qua API | Mô hình thô có texture |
| 3. Dựng lại lưới | Blender: Remesh QuadriFlow, rồi sửa vòng cạnh ở vai, khuỷu, gốc tai | Script `tools/blender/` làm phần lớn, chỉnh tay chỗ khớp | ≤ 3.000 tam giác, vòng cạnh đủ để gập |
| 4. Màu | Bỏ texture ảnh của AI. Tô theo vùng (lông, tai trong, má, mắt) bằng texture bảng màu nhỏ | Script | Material đổi màu theo giống được |
| 5. Rig | Bộ xương chung dựng bằng script, trọng số tự động, chỉnh tay ở tai, cổ, đuôi | Script + chỉnh tay | Một `.blend` cho mỗi dáng thân, cùng tên xương |
| 6. Clip | Key trong Blender theo pose dùng chung (chuẩn bị, đỉnh, theo đà) | Chỉnh tay, Claude dựng bản nháp bằng script | `.fbx` clip, xuất GIF QA mỗi lần sửa |
| 7. Kiểm tra | Script Blender kiểm tên xương, số tam giác, trọng số; test EditMode trong Unity | Tự động | Đỏ khi sai chuẩn |

Clip làm ở bước 6 là clip đã key sẵn, lưu thành asset. Unity chỉ phát lại qua Animator nên không vi phạm luật cấm toán hoạt ảnh trong C#.

Rủi ro còn lại:
- Lưới AI thường lệch hai bên và hỏng ở chỗ mảnh (tai, ngón). Bước 3 phải làm lại, không dùng lưới AI để rig.
- Clip key bằng script dễ trông cứng. Mỗi clip phải qua `visual-critic-gate` với GIF, và chủ project duyệt clip `Idle` và `Work` đầu tiên trước khi làm hàng loạt.
- Nếu sau giai đoạn 5.0 clip vẫn chưa đạt, thuê hoạt hoạ riêng phần clip; pipeline từ bước 1 đến 5 vẫn dùng tiếp.

Cần cài trước: Blender 5.3 (đã cài 25/11 bằng `brew install --cask blender`, chạy được không giao diện) và tài khoản Tripo có khoá API. Khoá để trong biến môi trường `TRIPO_API_KEY`, không commit.

## 5. Các giai đoạn

Mỗi giai đoạn chỉ xong khi có ảnh QA được duyệt và `tools/verify.sh` xanh. Toàn bộ làm trên nhánh `3d` trong worktree `../VuonTraNam-3d` (có Library riêng), merge `main` vào thường xuyên, và chỉ thay bản phát hành khi qua mục 5.8.

### 5.0 Khung và bản mẫu thẳng đứng (2–3 tuần)
- Tạo assembly `VuonTra.World`, scene `Garden3D`, camera Cinemachine, chạm.
- Một dáng thân, bộ xương, 3 clip (`Idle`, `Work` pha trà, `Doze`), shader toon, viền, vân giấy.
- Một trạm (Ấm trà) nối với Core thật: thỏ làm việc, thu tiền, lên cấp.
- Đo trên máy yếu nhất cần hỗ trợ. Mục tiêu 60 fps; dưới 45 fps thì sửa ngân sách trước khi đi tiếp.
- **Cổng duyệt của chủ project:** ảnh, video và số fps.

### 5.1 Luật và công cụ (song song với 5.0, khoảng 1 tuần)
- Sửa CLAUDE.md: luật 2 đổi `SpriteLibrary/SpriteResolver` thành socket xương + `RabbitKindLook`; luật 1 giữ nguyên.
- `tools/conventions.py`: áp các luật hiện có cho `Assets/VuonTra/World`, thêm luật cấm gán `localRotation`, `localPosition` hay `localScale` cho xương từ code trong World.
- Viết lại skill `unity-2d-native-rigging` thành bản 3D, sửa `cozy-watercolor-art` cho 3D toon.

### 5.2 Thỏ đầy đủ (3–4 tuần)
- 3–4 dáng thân, 76 material sinh từ dữ liệu, 12 món đồ gắn socket.
- Đủ trạng thái Animator, biến thể idle, `CycleOffset`.
- `RabbitPortraitBaker` và trang chi tiết dùng `RenderTexture`.

### 5.3 Khu vườn đầy đủ (4–5 tuần)
- 16 prefab trạm, mỗi trạm có 3 hạng trang trí (#49: cờ, đèn đá, quầng).
- Đất chưa mở, ô trống, sương, biển giá.
- Đồ trang trí và chế độ sắp xếp trên lưới 3D.
- Các khu phụ hiện vẽ trong vườn (hồ cá, câu cá, hội chợ, giã bánh, trăng non...) chuyển lần lượt, mỗi khu một issue.

### 5.4 Ánh sáng, VFX, phản hồi (2 tuần)
- Ngày và đêm, đèn lồng, lightmap.
- Thay `FirefliesView`, `LeavesView` và các hiệu ứng vẽ bằng code bằng Shuriken.
- Phản hồi khi chạm, thu tiền, lên cấp theo skill `game-feel-juice`.

### 5.5 Tách và xoá phần 2D cũ (song song với 5.3–5.4)
- Mỗi phần thế giới chuyển xong thì xoá đoạn uGUI tương ứng trong `GardenScreen.cs` ngay trong cùng commit.
- Baseline `procedural-animation` và `comment` của các file bị xoá giảm theo (`python3 tools/conventions.py --lower`).
- Xoá `KindArt` và sprite thỏ cũ khi không còn chỗ nào dùng.

### 5.6 Hiệu năng (1–2 tuần)
- Ngân sách: ≤ 150 draw call, ≤ 150k tam giác trên màn hình, ≤ 20 thỏ đang chạy Animator, bộ nhớ texture ≤ 200 MB.
- Profiler trên iOS và Android thật, LOD cho trạm, nén ASTC.

### 5.7 Hoàn thiện cửa hàng (1 tuần)
- Ảnh cửa hàng mới ([anh-cua-hang.md](phat-hanh/anh-cua-hang.md)), video, mô tả.

### 5.8 Điều kiện thay bản phát hành
- Mọi tính năng hiện có trong vườn đều có bản 3D. Danh sách lấy từ các view trong `Assets/VuonTra/Game/UI` ở giai đoạn 5.0 và đánh dấu từng mục.
- Save cũ mở được, không mất gì (Core không đổi nên đây là kiểm tra chứ không phải chuyển đổi).
- 701+ test Core xanh, test nội dung xanh, `verify.sh` xanh.
- fps đạt ngân sách trên máy yếu nhất.

**Tổng ước lượng:** khoảng 5–6 tháng với AI 3D cộng chỉnh tay (mục 4.1); phần clip quyết định tiến độ nhiều nhất.

## 6. Ảnh hưởng đến lộ trình kiếm tiền

[LO-TRINH-KIEM-TIEN.md](phat-hanh/LO-TRINH-KIEM-TIEN.md) đang quy định tính năng lớn phải chờ có dữ liệu. Chuyển 3D là ngoại lệ chủ project chọn. Có hai cách:

- **Phát hành bản 2D trước** để có dữ liệu, rồi thay bằng 3D ở bản cập nhật lớn. Dữ liệu và save đều không bị ảnh hưởng vì Core giữ nguyên.
- **Hoãn phát hành** đến khi 3D đạt mục 5.8.

## 7. Chủ project cần quyết

1. ~~Nguồn mô hình và clip~~: đã chọn AI 3D cộng chỉnh tay (25/11, mục 4.1). Dịch vụ: **Tripo** (chọn 25/11). Còn phải đăng ký gói trả phí có API.
2. **Phát hành 2D trước hay hoãn** (mục 6).
3. **DOTween hay `Tweens` tự viết** (đang treo từ phiên thiết kế).
4. **Số dáng thân:** 3 hay 4.
5. **Máy yếu nhất phải chạy 60 fps** (đời iPhone và Android cụ thể).

## 8. Phân việc giữa các phiên

- Phiên thiết kế (UX/UI, hướng mỹ thuật, hoạt ảnh): shader, ánh sáng, clip, duyệt ảnh QA.
- Phiên B: merge nhánh `3d` vào `main` khi qua 5.8; chạy Unity theo cổng `/tmp/unity-gate.sh`.
- PO management xếp các giai đoạn ở mục 5 vào hàng đợi và quyết cách theo dõi (issue hay comment trên issue cũ).

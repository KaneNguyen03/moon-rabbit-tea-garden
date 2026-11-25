# Story 145: Đất và camera

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#145
> **Epic**: Vườn 2.5D
> **Status**: In Progress
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: L (6–9 ngày theo docs/de-xuat-2-5d.md §6)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 1
**Requirement**: (chưa cấp TR-ID — `docs/architecture/tr-registry.yaml` chưa dựng, kế hoạch di trú Bước 3a)

**ADR Governing Implementation**: ADR-0005: Hướng 2.5D; ADR-0006: Camera và renderer vườn 2.5D (Proposed)
**ADR Decision Summary**: Mặt đất 3D và camera phối cảnh nghiêng cố định; mọi hình 2D màu nước hiện có đứng thẳng làm billboard pivot ở chân; không 3D toàn phần, không AI 3D trả phí. Luật vườn vẫn ở Core (ADR-0001).
**ADR Version**: 2026-11-26 (Last Verified của ADR-0005)

**Engine**: Unity 6000.6.10f1, URP 17.6 | **Risk**: MEDIUM
**Engine Notes**: Forward+ `UniversalRendererData` và bóng sprite alpha clip trên mobile chưa tra nguồn cho 6000.6 (`docs/engine-reference/unity/VERSION.md`); chạy `/setup-engine refresh` trước khi bắt đầu.

**Control Manifest Rules (this layer)** — `docs/architecture/control-manifest.md` chưa có; tạm lấy từ `CLAUDE.md` và `.claude/docs/technical-preferences.md`:
- Required: hoạt ảnh bằng Animator và `.anim` clip; hiệu ứng bằng Shuriken; phản hồi UI bằng DOTween; dữ liệu từ ScriptableObject
- Forbidden: comment trong code; toán hoạt ảnh `Mathf.Sin`/`Lerp` trên đạo cụ; `GameObject.Find`/`FindObjectOfType`; `Update()` mới ngoài input/physics; workaround (deferred frame, `Invoke` theo tên, `SendMessage`, `catch {}` rỗng)
- Guardrail: `python3 -I tools/conventions.py` không tăng baseline; 0 byte rác mỗi khung ở vườn đầy (#123)

---

## Acceptance Criteria

*Từ issue #145 và `docs/de-xuat-2-5d.md` §5–§6, trong phạm vi story này:*

- [ ] Mở game thấy đảo cỏ có mép đất dày, camera phối cảnh FOV ~25°, nghiêng cố định ~40°, không xoay; vùng chưa mở không có sương (cỏ tối một bậc, hàng rào thấp, biển gỗ)
- [ ] Kéo có quán tính, chụm để zoom, dừng mềm ở biên đảo; chạm trúng ô đất và trạm bằng `PhysicsRaycaster` + `IPointerClickHandler`, không raycast trong `Update()`
- [ ] Màn bản đồ (#91) hiện cảnh từ camera trực giao nhìn xuống qua `RenderTexture`
- [ ] Khung vườn trước/sau cùng góc (zoom mặc định, 1080×1920) ở `docs/anh/po-145/`, `visual-critic-gate` `APPROVED`, không còn lối đi thẳng một cột; chữ vi/en, Chữ to, Giảm chuyển động vẫn đúng
- [ ] `python3 -I tools/conventions.py` không tăng baseline

---

## Implementation Notes

*Từ ADR-0005 và issue:*

- Thêm `UniversalRendererData` (Forward+) cho scene vườn; giữ `Renderer2D` cho bản 2D tới khi 2.5D thay được.
- Tách thế giới vườn khỏi `GardenScreen.cs` uGUI thành scene/prefab mặt đất; Core giữ nguyên.
- Mesh đảo cỏ (design): script Blender hoặc dựng bằng code như spike `spike-3d` (`Spike3DBuilder.cs`, không gộp); mặt trên ô cỏ #129 `Assets/VuonTra/Art/Ground/bg_grass.png`; lối mòn #129 có đoạn cong.

---

## Out of Scope

- story-146 dựng đứng trạm/đồ/thỏ; story-147 ánh sáng và bóng; story-148 hậu kỳ và HUD; story-149 hiệu năng

---

## Files

*Đã có (26/11):* `Assets/VuonTra/Core/GardenFraming.cs`, `Assets/VuonTra/Game/Garden25D/GardenCameraSettings.cs`, `Assets/VuonTra/Game/Garden25D/GardenCameraRig.cs`, `Assets/Tests/EditMode/GardenFramingTests.cs`, `Assets/Tests/EditMode/GardenCameraRigTests.cs`, `docs/architecture/adr-0006-camera-va-renderer-vuon-2-5d.md`. *Còn dự kiến:*

- Assets/VuonTra/Game/UI/GardenScreen.cs (tách phần dựng thế giới)
- Assets/VuonTra/Game/Garden25D/ (scene, camera, mặt đất — tên thư mục do B chọn)
- Assets/Settings/ (UniversalRendererData mới)
- Assets/Tests/EditMode/GardenScreenTests.cs, MapViewScreenTests.cs

---

## QA Test Cases

*Review mode `lean`: QL-STORY-READY bỏ qua; các ca dưới đây lấy từ tiêu chí nghiệm thu của issue.*

- **AC-1**: Camera giữ trong biên ở mọi mức zoom
  - Given: vườn mặc định và vườn đủ 6 đoạn
  - When: kéo tới bốn góc ở zoom nhỏ nhất và lớn nhất
  - Then: tâm nhìn luôn trong hình bao của đảo
  - Edge cases: đoạn đất mới mở làm biên lớn ra
- **AC-2**: Chạm trúng ô đất
  - Given: một ô trống và một trạm
  - When: mô phỏng chạm qua PhysicsRaycaster tại toạ độ màn hình của chúng
  - Then: sự kiện chạm đúng ô/trạm, không trúng ô bên cạnh
  - Edge cases: ô ở mép đảo, zoom nhỏ nhất
- **AC-3**: Bản đồ có texture
  - Given: màn bản đồ mở
  - When: một khung được render
  - Then: RenderTexture khác null và đúng kích thước màn
  - Edge cases: mở bản đồ hai lần liền

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: test EditMode trong `Assets/Tests/EditMode/` (tên do `/dev-story` chọn) và số đo/playtest ghi lại

**Status**: [x] Một phần: `Assets/Tests/EditMode/GardenFramingTests.cs` (13 test Core: AC-1 biên, AC-2 chọn ô từ điểm đất, khung bản đồ AC-3), `Assets/Tests/EditMode/GardenCameraRigTests.cs` (2 test Unity)

---

## Dependencies

- Depends on: None
- Unlocks: story-146, story-149

---

## Tiến độ 26/11 đêm

**story-readiness** (lean, QL-STORY-READY bỏ qua): không có blocker. ADR-0005 Accepted, Dependencies None, `bg_grass.png` có. Gap nhỏ, không chặn: Context chưa trích một luật cụ thể của GDD (chỉ tên file); Engine Notes Forward+ mobile vẫn NOT SOURCEABLE (`/setup-engine refresh` chưa chạy); chưa có TR-ID.

**Đã làm**
- ADR-0006 `docs/architecture/adr-0006-camera-va-renderer-vuon-2-5d.md` (Proposed): khung hình tính ở Core, zoom là khoảng cách camera, renderer Forward+ thứ hai ở chỉ số 1, bản đồ là camera trực giao render theo yêu cầu, cờ `Features.Garden25D` mặc định tắt.
- `Assets/VuonTra/Core/GardenFraming.cs`: `CameraSpec` (FOV 25°, nghiêng 40°, mặc định 35 m, co giãn mép 0,6 m, quán tính còn 5 % sau 0,35 s, dừng < 0,05 m/s) và hàm thuần `Ground`, `Island`, `TileAt`, `Pose`, `Drag`, `Settle`, `Glide` (tắt dưới Giảm chuyển động), `Pinch`, `ZoomAbout`, `DistanceOf`/`ZoomOf`/`ShowsDetail`, `Map`. Ba mức zoom 2D (Xa 1920/2970, Mặc định 0,83, Gần 1,5, chi tiết 1,165) đổi thành khoảng cách camera.
- `Assets/VuonTra/Game/Garden25D/GardenCameraSettings.cs` (ScriptableObject cài đặt camera) và `GardenCameraRig.cs` (chỉ gán `CameraPose` cho camera, không `Update()`).
- Test: 13 `GardenFramingTests` chạy ngoài Unity, 2 `GardenCameraRigTests` trong Unity.

**Quyết định (cần chủ project duyệt qua ADR-0006)**
- Đơn vị: một bước lục giác theo q = 1 m, một hàng = 4/3 m (giữ tỉ lệ `PitchY/PitchX` của bản 2D); hàng phía trên màn 2D là hàng xa camera.
- Biên kéo là hình chữ nhật bao đảo cho tâm nhìn (AC-1 "tâm nhìn trong hình bao của đảo"); mép đảo không vuông có thể lộ góc trống khi zoom xa, xem lại bằng ảnh trước/sau.
- Khoảng cách mặc định 35 m cho bề ngang thấy ở 1080×1920 bằng bản 2D (±5 %).

**Tiếp theo**
1. Asset `GardenCameraSettings` và `Assets/Settings/Garden25DRenderer.asset` (Forward+) ở chỉ số 1 của `UniversalRP.asset`; cờ `Features.Garden25D`.
2. Input kéo/chụm trên prefab `Garden25D` gọi `Drag`/`Glide`/`Pinch`/`ZoomAbout` (sự kiện con trỏ, không `Update()` mới).
3. Mesh đảo (mép đất dày, ô cỏ #129) + `MeshCollider` + `PhysicsRaycaster`, chạm đất → `TileAt` (AC-2 trong Unity).
4. Camera bản đồ trực giao + `RenderTexture` cho #91 (AC-3), kiểm `SubmitRenderRequest` trên 6000.6.
5. Khung trước/sau `docs/anh/po-145/`, `visual-critic-gate`.


# Story 145: Đất và camera

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#145
> **Epic**: Vườn 2.5D
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: L (6–9 ngày theo docs/de-xuat-2-5d.md §6)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 1
**Requirement**: (chưa cấp TR-ID — `docs/architecture/tr-registry.yaml` chưa dựng, kế hoạch di trú Bước 3a)

**ADR Governing Implementation**: ADR-0005: Hướng 2.5D
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

*Dự kiến; `/dev-story` thay bằng danh sách thật.*

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

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: None
- Unlocks: story-146, story-149

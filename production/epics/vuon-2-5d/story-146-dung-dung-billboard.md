# Story 146: Trạm, đồ và thỏ dựng đứng có bóng, bảng tỉ lệ, vòng làm việc ba trạm mẫu

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#146
> **Epic**: Vườn 2.5D
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: L (6–8 ngày)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 2
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

*Từ issue #146 và `docs/de-xuat-2-5d.md` §5–§6, trong phạm vi story này:*

- [ ] Mọi trạm, đồ, cây, đèn, thỏ là billboard pivot ở chân, quay về camera quanh trục dọc, đặt theo toạ độ đất từ `GardenGrid`; test EditMode: mọi trạm có prefab, chân chạm đất, bounds đúng bảng tỉ lệ ±15 % (thỏ = 1: trạm 0,8–1,3; quầy 1,8–2,2; đèn cột 1,4; bể cá 0,9; cối 0,7; cây 2,5–3,5)
- [ ] Nhãn và bảng trạm trên World Space Canvas giữ ba bậc và ngưỡng zoom của #135, chữ ≥ 22 đơn vị ở zoom mặc định, không nhãn nào đè nhau, ≤ 4 nhãn chữ ở zoom mặc định
- [ ] Ba trạm mẫu (trà sữa, hái chè, pha trà) có vòng làm việc 4 nhịp bằng Animator/clip và SpriteResolver; GIF mỗi trạm qua `visual-critic-gate`
- [ ] Khung vườn và khung một trạm phóng to trước/sau ở `docs/anh/po-146/`, `visual-critic-gate` `APPROVED`; ảnh xám thu 25 %: thỏ ở trạm là điểm sáng nhất
- [ ] `python3 -I tools/conventions.py` không tăng baseline

---

## Implementation Notes

*Từ ADR-0005 và issue:*

- Prefab billboard: quad mang sprite, shader Lit alpha clip để nhận sáng và đổ bóng (tham chiếu `Spike3DSprite.shader` ở `spike-3d`).
- Thỏ dùng rig 2.5D đã có (`Assets/VuonTra/Game/Rabbit25D/RabbitActor.cs`, `Assets/VuonTra/Art/Rabbit25D`): góc trước và 3/4, pivot chân, bóng tách riêng.
- Hạ tầng vòng làm việc từ #132; 13 trạm còn lại (#139–#141) làm sau.

---

## Out of Scope

- story-145 đất và camera; story-147 bóng mềm và đèn hắt; story-148 hậu kỳ

---

## Files

*Dự kiến; `/dev-story` thay bằng danh sách thật.*

- Assets/VuonTra/Game/Garden25D/ (prefab billboard, đặt theo GardenGrid)
- Assets/VuonTra/Game/UI/GardenScreen.WorldLabels.cs
- Assets/VuonTra/Game/Rabbit25D/RabbitActor.cs
- Assets/Tests/EditMode/ (test bảng tỉ lệ và chân chạm đất)

---

## QA Test Cases

*Review mode `lean`: QL-STORY-READY bỏ qua; các ca dưới đây lấy từ tiêu chí nghiệm thu của issue.*

- **AC-1**: Bảng tỉ lệ
  - Setup: vườn đầy, zoom mặc định
  - Verify: đo chiều cao hiển thị từng loại so với thỏ
  - Pass condition: trong khoảng bảng tỉ lệ ±15 %
- **AC-2**: Thỏ là điểm nhìn
  - Setup: khung vườn đêm có thỏ ở ba trạm mẫu
  - Verify: ảnh xám thu 25 %
  - Pass condition: thỏ ở trạm là vùng sáng nhất

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- Visual/Feel: ảnh trước/sau ở `docs/anh/po-146/` (thư mục bằng chứng của dự án) + `visual-critic-gate` APPROVED ghi trong comment issue

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: production/epics/vuon-2-5d/story-145-dat-va-camera.md
- Unlocks: story-147

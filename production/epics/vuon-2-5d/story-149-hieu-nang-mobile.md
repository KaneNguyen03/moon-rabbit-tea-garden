# Story 149: Hiệu năng mobile: draw call, bóng, cấp chất lượng, 60 fps

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#149
> **Epic**: Vườn 2.5D
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: M (4–6 ngày)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 5
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

*Từ issue #149 và `docs/de-xuat-2-5d.md` §5–§6, trong phạm vi story này:*

- [ ] ≥ 60 fps (sàn 45) ở khung vườn đầy nhất trên iPhone 11 và Android Snapdragon 665 (chủ dự án chốt 26/11), hoặc profile tương đương trong Editor có ghi cách quy đổi; số đo và cách đo ghi vào `docs/bao-cao-po.md`
- [ ] SRP Batcher, atlas sprite cho billboard, gộp material; một đèn có bóng (trăng), khoảng cách bóng ngắn; cấp chất lượng thấp tắt bloom và giảm bóng
- [ ] Ảnh cấp chất lượng thấp qua `visual-critic-gate` `APPROVED`; `tools/verify.sh` xanh
- [ ] `python3 -I tools/conventions.py` không tăng baseline

---

## Implementation Notes

*Từ ADR-0005 và issue:*

- Đo từ story-145 trở đi; khoá ngân sách trước khi 2.5D thay bản 2D phát hành.
- Ngân sách trong `project.yaml` `performance.*`: 60 fps, 16,67 ms; draw call và bộ nhớ chưa đặt — ghi số đo vào đó sau story này.

---

## Out of Scope

- đổi hình vẽ; tính năng mới

---

## Files

*Dự kiến; `/dev-story` thay bằng danh sách thật.*

- Assets/Settings/ (cấp chất lượng URP)
- Assets/VuonTra/Game/Garden25D/
- docs/bao-cao-po.md (số đo)

---

## QA Test Cases

*Review mode `lean`: QL-STORY-READY bỏ qua; các ca dưới đây lấy từ tiêu chí nghiệm thu của issue.*

- **AC-1**: 60 fps vườn đầy
  - Given: save vườn đầy nhất (6 đoạn, 16 trạm, thỏ ở mọi chỗ)
  - When: kéo và zoom liên tục 60 giây trên máy mục tiêu
  - Then: fps trung bình ≥ 60, thấp nhất ≥ 45
  - Edge cases: mưa và đom đóm bật cùng lúc

---

## Test Evidence

**Story Type**: Integration
**Required evidence**:
- Integration: test EditMode trong `Assets/Tests/EditMode/` (tên do `/dev-story` chọn) và số đo/playtest ghi lại

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: production/epics/vuon-2-5d/story-145-dat-va-camera.md (đo từ đây); đóng sau story-148
- Unlocks: None

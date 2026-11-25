# Story 147: Ánh sáng và bóng

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#147
> **Epic**: Vườn 2.5D
> **Status**: Ready
> **Layer**: Presentation
> **Type**: Visual/Feel
> **Estimate**: M (4–6 ngày)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 3
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

*Từ issue #147 và `docs/de-xuat-2-5d.md` §5–§6, trong phạm vi story này:*

- [ ] Directional "trăng" từ trên-trái có bóng mềm; mọi vật đổ bóng cùng hướng, bóng nhuộm `Night`, không đen
- [ ] Đèn lồng là point light `#FFC866` tầm ~3 m, không bóng, kèm quầng sprite cộng màu, và hắt vũng sáng hổ phách thấy rõ trên cỏ
- [ ] Khung vườn đêm, bình minh, hoàng hôn (theo `Daylight`) trước/sau ở `docs/anh/po-147/`, `visual-critic-gate` `APPROVED`; chọn bóng sprite hay quad bóng elip bằng một tấm so sánh
- [ ] Đổi giờ trong ngày không đổi số cà rốt (test Core giữ nguyên); `tools/verify.sh` xanh
- [ ] `python3 -I tools/conventions.py` không tăng baseline

---

## Implementation Notes

*Từ ADR-0005 và issue:*

- Cascade và khoảng cách bóng hợp với camera cố định; light probe/lightmap cho phần tĩnh nếu cần.
- Toon ramp 2 bậc cho đất nếu cần (mặt tối = màu gốc pha 35 % `Night`).

---

## Out of Scope

- story-148 hậu kỳ, vân giấy; story-149 hiệu năng bóng

---

## Files

*Dự kiến; `/dev-story` thay bằng danh sách thật.*

- Assets/VuonTra/Game/Garden25D/ (đèn, volume ánh sáng)
- Assets/Settings/ (cấu hình bóng URP)

---

## QA Test Cases

*Review mode `lean`: QL-STORY-READY bỏ qua; các ca dưới đây lấy từ tiêu chí nghiệm thu của issue.*

- **AC-1**: Bóng cùng hướng, không đen
  - Setup: khung vườn đêm zoom mặc định
  - Verify: hướng bóng của 5 vật bất kỳ và màu vùng bóng
  - Pass condition: cùng hướng trên-trái sang dưới-phải; màu bóng không phải #000
- **AC-2**: Vũng đèn
  - Setup: đèn lồng cạnh một trạm có thỏ
  - Verify: mặt cỏ quanh chân đèn
  - Pass condition: thấy vũng sáng ấm trên cỏ, không chỉ trên hình gần

---

## Test Evidence

**Story Type**: Visual/Feel
**Required evidence**:
- Visual/Feel: ảnh trước/sau ở `docs/anh/po-147/` (thư mục bằng chứng của dự án) + `visual-critic-gate` APPROVED ghi trong comment issue

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: production/epics/vuon-2-5d/story-146-dung-dung-billboard.md
- Unlocks: story-148

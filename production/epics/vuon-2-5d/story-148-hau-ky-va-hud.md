# Story 148: Hậu kỳ, trời phía trên đảo, HUD trên cảnh 2.5D

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#148
> **Epic**: Vườn 2.5D
> **Status**: Ready
> **Layer**: Presentation
> **Type**: UI
> **Estimate**: S–M (2–3 ngày)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 4
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

*Từ issue #148 và `docs/de-xuat-2-5d.md` §5–§6, trong phạm vi story này:*

- [ ] Volume: tonemapping Neutral, bloom ngưỡng ~1 chỉ cho đèn, vignette ~0,2; vân giấy là pass toàn màn hình Multiply 6–8 % (Renderer Feature), lớp `PaperGrain` uGUI bỏ; tối đa 2 lớp phủ toàn màn hình
- [ ] HUD, thanh tab, sheet, thẻ và photo mode chạy như cũ trên cảnh 2.5D (camera UI xếp chồng hoặc Screen Space Overlay), không che thỏ và trạm ở zoom mặc định; test giao diện (HUD, sheet, Chữ to, vi/en) xanh
- [ ] Không còn vùng trời trống lớn phía trên đảo (rừng, trăng, sao hoặc đồi xa, theo #137); khung vườn có HUD và khung photo mode trước/sau ở `docs/anh/po-148/`, `visual-critic-gate` `APPROVED`
- [ ] `python3 -I tools/conventions.py` không tăng baseline

---

## Implementation Notes

*Từ ADR-0005 và issue:*

- Không thiết kế lại khung thẻ 9-slice (#143) hay phông chữ (#142); dùng như có.

---

## Out of Scope

- story-149 hiệu năng

---

## Files

*Dự kiến; `/dev-story` thay bằng danh sách thật.*

- Assets/VuonTra/Game/UI/PaperGrain.cs (bỏ)
- Assets/Settings/ (Volume, Renderer Feature vân giấy)
- Assets/VuonTra/Game/UI/ (camera UI)

---

## QA Test Cases

*Review mode `lean`: QL-STORY-READY bỏ qua; các ca dưới đây lấy từ tiêu chí nghiệm thu của issue.*

- **AC-1**: Hai lớp phủ
  - Setup: vườn có HUD, mở một sheet
  - Verify: đếm lớp phủ toàn màn hình đang bật
  - Pass condition: ≤ 2
- **AC-2**: HUD không che thỏ
  - Setup: zoom mặc định, thỏ ở ba trạm mẫu
  - Verify: HUD và thanh tab so với vị trí thỏ
  - Pass condition: không thỏ nào bị che quá nửa

---

## Test Evidence

**Story Type**: UI
**Required evidence**:
- UI: ảnh mỗi màn chạm tới ở `docs/anh/po-148/` + test giao diện EditMode

**Status**: [ ] Not yet created

---

## Dependencies

- Depends on: production/epics/vuon-2-5d/story-147-anh-sang-va-bong.md
- Unlocks: story-149

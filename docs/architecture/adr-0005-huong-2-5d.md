# ADR-0005: Vườn chuyển sang 2.5D: mặt đất 3D, camera nghiêng cố định, hình 2D hiện có đứng làm billboard; bỏ 3D hẳn và AI 3D trả phí

> **Reverse-documented** from code and history at commit 9663644 on 2026-11-26; rationale not stated in docs is marked INTENT UNKNOWN.

## Status

Accepted (hướng đã chọn, đang làm: bước 1–5 là issue #145–#149, đều còn mở)

## Date

2026-11-25: chủ project chốt "bỏ 3D mà quay lại 2.5D thôi nha" sau spike (`docs/de-xuat-2-5d.md`). 2026-11-26: chủ project "Approve" tấm so sánh; tài liệu vào git ở `0b96f27`.

## Last Verified

2026-11-26

## Decision Makers

Chủ project (chọn hướng, huỷ AI 3D trả phí: "Cancel it"); session design (đề xuất, spike, rig thỏ); session code B (bước 1, 2, 5).

## Summary

Vườn 2D dựng bằng `RectTransform` trong `GardenScreen.cs` thiếu chiều sâu so với thể loại; spike 3D hẳn bị chủ project chê ("xấu xấu"). Chọn 2.5D: đảo cỏ là mesh 3D, camera phối cảnh nghiêng khoảng 40° không xoay, mọi trạm, đồ, cây, đèn, thỏ vẫn là hình màu nước 2D hiện có đứng thẳng như billboard pivot ở chân; giao diện uGUI và Core giữ nguyên.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1, URP 17.6.0, `com.unity.2d.animation` 16.0.1, `com.unity.2d.psdimporter` 15.0.1 |
| **Domain** | Rendering |
| **Layer** | Presentation |
| **Knowledge Risk** | MEDIUM/HIGH: `docs/engine-reference/unity/VERSION.md` ghi NOT SOURCEABLE cho hành vi `UniversalRendererData` Forward+ trên mobile và cho thay đổi 6000.4 → 6000.6 |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `docs/de-xuat-2-5d.md`, `Assets/Settings/UniversalRP.asset` |
| **Post-Cutoff APIs Used** | Chưa xác định (renderer Forward+ chưa thêm) |
| **Verification Required** | 60 fps trên máy yếu nhất chủ project chọn (#149); bóng sprite qua alpha clip không răng cưa; `PhysicsRaycaster` chạm đúng ô đất |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Core giữ nguyên), ADR-0002 (sprite trong asset dùng lại làm billboard) |
| **Enables** | None |
| **Blocks** | #146 (billboard) chờ #145 (đất và camera); #147, #148 chờ #146; #149 cuối |
| **Ordering Note** | `Renderer2D` giữ cho bản 2D đang chạy tới khi 2.5D thay được |

## Context

### Problem Statement

Chủ project muốn cảm giác thể loại (camera nghiêng cố định, đồ vật đứng trên đất có chiều sâu, ánh sáng ấm) mà không vẽ lại toàn bộ hình 2D.

### Current State

**Đã có:**
- Đề xuất duyệt: `docs/de-xuat-2-5d.md` (renderer, camera, mặt đất, billboard, bóng, đèn, hậu kỳ, bản đồ #91, chuẩn mỹ thuật, lộ trình 5 bước), tấm `docs/anh/de-xuat-2-5d/so-sanh-2d-25d.png`, `VERDICT.md` APPROVED kèm 4 ghi chú.
- Spike trên nhánh `spike-3d` (`dfdc8b1`, 25/11, không gộp vào main): hình vẽ hiện có đứng như thẻ đổ bóng trên đảo cỏ 3D, `Spike3DBuilder.BuildAndShoot`.
- Ô cỏ màu nước lặp không đường nối `Assets/VuonTra/Art/Ground/bg_grass.png` (#129, `e26a577`), dự định làm texture mặt đất.
- Thỏ Cream cho 2.5D (`47e6642`): `Assets/VuonTra/Art/Rabbit25D/Cream/RabbitCream.prefab` với hai rig PSB (`CreamFront.psb`, `CreamSide.psb` 3/4, lật gương cho bên phải), pivot ở chân, `Shadow.png` tách riêng, đồ cầm tay đổi bằng `SpriteResolver` (`RabbitCream_Library.asset`), clip `Cream_Idle`, `Cream_MilkTea`, `Cream_TeaPicking`, `Cream_TeaBrewing`, `RabbitCream.controller` tham số `Work`. `Assets/VuonTra/Game/Rabbit25D/RabbitActor.cs` chỉ đặt tham số Animator và hướng nhìn. Công cụ dựng trong `Assets/VuonTra/Editor/Rabbit25D/` (asmdef riêng).
- Dữ liệu đường đi cho thỏ nghỉ tay trên cảnh 2.5D: `Core/Strolls.cs` `Break.From/To`, `LegAt` (#127, `dc54b15`), chưa nối vào cảnh.

**Chưa có:**
- `Assets/Settings/UniversalRP.asset` chỉ có một renderer (`Renderer2D`); chưa có `UniversalRendererData`.
- Chưa có scene vườn 2.5D, camera phối cảnh, mesh đảo, `PhysicsRaycaster` (#145).
- `RabbitActor`/`RabbitCream` chưa được scene hay mã `Game` nào dùng (#146). Chưa có prefab billboard cho trạm, đồ.
- Ánh sáng trăng, đèn lồng (#147); hậu kỳ, vân giấy, HUD trên cảnh (#148); đo hiệu năng mobile (#149).
- Chưa cài Cinemachine, ProBuilder.

### Constraints

- Không dùng dịch vụ AI 3D trả phí. Blender 5.3 có trên máy (25/11).
- Luật `CLAUDE.md`: hoạt ảnh bằng Animator/clip, không toán `Mathf.Sin/Lerp`; VFX bằng ParticleSystem; `Update()` rỗng. `GardenScreen.cs` hiện còn 117 vi phạm `procedural-animation` và 955 comment trong `tools/conventions.baseline`.

### Requirements

- Kéo, zoom, chạm trạm, mở bản đồ chạy như bản 2D; chữ vi/en, Chữ to, Giảm chuyển động vẫn đúng (#145).
- Thỏ đang làm là điểm sáng nhất trong ảnh xám thu 25 % (`de-xuat-2-5d.md` §5).

## Decision

Giữ Core, test, uGUI và mọi hình 2D; làm lại phần dựng thế giới của `GardenScreen.cs` thành scene có camera phối cảnh. Billboard quay về camera quanh trục dọc, không ngả theo góc nghiêng, pivot ở chân; bóng là quad elip riêng hoặc bóng sprite theo đèn trăng, chọn bằng ảnh so sánh. Bản đồ #91 là camera trực giao render vào `RenderTexture`. Nhãn ba bậc #135 chuyển sang World Space Canvas.

### Architecture Diagram

```
Core (unchanged) -> GameRunner -> Garden 2.5D scene (UniversalRendererData Forward+)
                                    perspective camera ~25° FOV, ~40° tilt
                                    island mesh (bg_grass.png) + billboard prefabs (sprites, pivot at feet)
                                    RabbitActor -> Animator (Work) on RabbitCream prefab
                                    ortho camera -> RenderTexture -> map screen (#91)
                  -> uGUI HUD/cards (unchanged, Screen Space) + World Space labels (#135)
```

### Key Interfaces

- `RabbitActor.SetView(RabbitView)`, `RabbitActor.SetWork(RabbitWork)`; tham số Animator `Work`.
- Chưa có interface cảnh cho trạm và mặt đất (thuộc #145, #146).

### Implementation Guidelines

- Hình trạm, đồ, thỏ must là sprite hiện có trên billboard; must never vẽ lại thành mesh 3D.
- Chuyển động thỏ must đi qua Animator và `.anim` clip; `RabbitActor` must chỉ đặt tham số.
- Chạm must dùng `PhysicsRaycaster` + `IPointerClickHandler`, không raycast trong `Update()` (#145).
- Core must không đổi vì 2.5D; dữ liệu cảnh lấy từ `GameState` và `Strolls`.
- `Renderer2D` must giữ cho tới khi 2.5D thay được bản 2D.

## Alternatives Considered

### Alternative 1: 3D hẳn (mesh thỏ, bộ xương 3D)

- **Description**: kế hoạch `a9d3988` và phụ lục `531591d` (25/11); spike `23a1ba6` với trạm toon shading và thỏ khối cơ bản.
- **Pros**: chiều sâu và ánh sáng thật cho mọi thứ.
- **Cons**: phải làm lại toàn bộ hình và hoạt ảnh; spike "xấu xấu".
- **Estimated Effort**: cao hơn nhiều (vẽ lại mọi tài sản).
- **Rejection Reason**: chủ project: "xấu xấu. 2.5D thôi" (`docs/anh/de-xuat-2-5d/spike-3d-lan-1-bo.png`).

### Alternative 2: Mua mesh bằng dịch vụ AI 3D trả phí

- **Description**: nguồn mesh trả phí nêu trong phụ lục `531591d`.
- **Pros**: có mesh nhanh.
- **Cons**: chi phí, lệch phong cách màu nước.
- **Estimated Effort**: không ghi.
- **Rejection Reason**: chủ project huỷ hạn mức ("Cancel it").

### Alternative 3: Giữ vườn 2D `RectTransform`

- **Description**: vườn hiện tại trên main, cột trái của `so-sanh-2d-25d.png`.
- **Pros**: đã chạy, đã qua QA.
- **Cons**: không có chiều sâu, ánh sáng đất.
- **Estimated Effort**: không đổi.
- **Rejection Reason**: chủ project duyệt cột 2.5D ngày 26/11; bản 2D giữ làm bản phát hành tới khi 2.5D thay được.

## Consequences

### Positive

- Dùng lại toàn bộ hình màu nước, bảng màu, art bible, rig thỏ 2D.
- Cơ hội gỡ phần lớn vi phạm `procedural-animation` của `GardenScreen.cs` khi viết lại phần dựng thế giới.

### Negative

- Hai renderer song song trong thời gian chuyển; thêm chi phí render 3D trên mobile.
- Lộ trình ước 22–32 ngày công cho 5 bước (`de-xuat-2-5d.md` §6).

### Neutral

- Gói `com.unity.2d.psdimporter` 15.0.1 mới thêm cho rig thỏ.

## Risks

- Forward+ và bóng trên mobile chưa tra tài liệu (VERSION.md). Giảm thiểu: `/setup-engine refresh` trước #145; đo máy thật ở #149.
- Thỏ nhỏ so với 2D nên mất điểm nhìn (ghi chú VERDICT). Giảm thiểu: bảng tỉ lệ theo chiều cao thỏ ở #146.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| [mo-rong-vuon-va-ban-do.md](../../design/gdd/mo-rong-vuon-va-ban-do.md) | Vườn kéo, zoom, mở đất; bản đồ #91 | Camera nghiêng có giới hạn đảo; bản đồ qua `RenderTexture` |
| [tram-va-thu-nhap.md](../../design/gdd/tram-va-thu-nhap.md) | Trạm hiện trên vườn, chạm được | Billboard trạm, `PhysicsRaycaster` |
| [tho-va-so-suu-tam.md](../../design/gdd/tho-va-so-suu-tam.md) | Thỏ làm việc ở trạm, nghỉ tay | `RabbitActor` + Animator; `Strolls` cho đường đi |

GDD đang viết song song; đối chiếu lại khi xong.

## Performance Implications

- **CPU**: thêm culling và bóng 3D; chưa đo.
- **Memory**: thêm mesh đảo, renderer data; sprite dùng lại.
- **Load Time**: chưa đo.
- **Network**: không có.

## Migration Plan

Theo `de-xuat-2-5d.md` §6: #145 đất và camera → #146 trạm, đồ, thỏ dựng đứng → #147 ánh sáng và bóng → #148 hậu kỳ và bố cục → #149 hiệu năng. Mỗi bước chơi được, có khung trước/sau và `visual-critic-gate` APPROVED.

**Rollback plan**: giữ `Renderer2D` và vườn 2D trên main tới khi 2.5D thay được; bỏ scene 2.5D là quay về bản 2D.

## Validation Criteria

- `grep -rlE "MeshRenderer|SkinnedMeshRenderer|MeshFilter" Assets/VuonTra/Art/Rabbit25D` không có kết quả; thỏ chỉ là PSB + Animator.
- `grep -E "Mathf\.(Sin|Lerp)" Assets/VuonTra/Game/Rabbit25D/*.cs` không có kết quả; `tools/conventions.py` không tăng baseline.
- Mỗi issue #145–#149 đóng với khung ở `docs/anh/po-<số>/` và `VERDICT.md` APPROVED.
- `Packages/manifest.json` không có gói AI 3D trả phí.

## Related

- ADR-0001, ADR-0002.
- `docs/de-xuat-2-5d.md`, `docs/anh/de-xuat-2-5d/VERDICT.md`, `docs/art-bible.md`, `docs/bao-cao-po.md` mục "Rig thỏ 2.5D".
- `Assets/VuonTra/Game/Rabbit25D/RabbitActor.cs`, `Assets/VuonTra/Art/Rabbit25D/Cream/`, `Assets/VuonTra/Art/Ground/bg_grass.png`.

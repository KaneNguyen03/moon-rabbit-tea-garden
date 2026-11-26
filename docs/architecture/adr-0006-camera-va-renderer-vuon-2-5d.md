# ADR-0006: Camera vườn 2.5D: khung hình tính ở Core, renderer Forward+ thứ hai, bản đồ render theo yêu cầu vào RenderTexture

## Status

Accepted

> Chủ project duyệt ngày 2026-11-26, nguyên văn: "Accept ADR-0006 and the Garden25D switch". Cờ `Features.Garden25D` giữ mặc định tắt tới khi vườn 2.5D thay vườn 2D.

## Date

2026-11-26 (agent-145, story-145 bước 1)

## Last Verified

2026-11-26

## Decision Makers

agent-145 (đề xuất); chủ project duyệt (2026-11-26).

## Summary

ADR-0005 chốt camera nghiêng cố định và bản đồ qua `RenderTexture` nhưng không nói camera, giới hạn kéo/zoom và renderer dựng thế nào. Quyết định: mọi phép tính khung hình (toạ độ đất, biên đảo, kéo co giãn ở mép, quán tính, zoom, chọn ô, khung bản đồ) là hàm thuần trong `Core/GardenFraming.cs` có test ngoài Unity; `Game` chỉ gán `CameraPose` cho camera; renderer 3D là `UniversalRendererData` thứ hai trong `UniversalRP.asset`, camera 2.5D chọn theo chỉ số; bản đồ là camera trực giao tắt sẵn, render một lần khi mở bản đồ hoặc khi đảo đổi.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1, URP 17.6.0 |
| **Domain** | Rendering, Input |
| **Layer** | Presentation (Core phần khung hình) |
| **Knowledge Risk** | HIGH cho renderer: `docs/engine-reference/unity/VERSION.md` ghi NOT SOURCEABLE cho Forward+ trên mobile; MEDIUM cho render camera theo yêu cầu trong URP 17 |
| **References Consulted** | ADR-0005, `docs/de-xuat-2-5d.md` §3, `design/ux/interaction-patterns.md`, `Assets/Settings/UniversalRP.asset`, `GardenScreen.cs` (`ZoomFar`, `ZoomDefault`, `ZoomNear`, `FarSpan`) |
| **Post-Cutoff APIs Used** | `UniversalAdditionalCameraData.SetRenderer(int)`; render theo yêu cầu bằng `RenderPipeline.SubmitRenderRequest` với `UniversalRenderPipeline.SingleCameraRequest` (cần kiểm lại trên 6000.6) |
| **Verification Required** | Forward+ chạy trên máy Android/iOS yếu nhất (#149), nếu không thì dùng Forward; `SubmitRenderRequest` ghi được vào `RenderTexture` trong EditMode/PlayMode test; `PhysicsRaycaster` trên camera phối cảnh chạm đúng ô |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 (Core thuần C#), ADR-0005 (hướng 2.5D) |
| **Enables** | story-145 phần camera, chạm, bản đồ; story-146 (billboard đặt theo `GardenFraming.Ground`) |
| **Blocks** | None |
| **Ordering Note** | `Renderer2D` vẫn là renderer mặc định (chỉ số 0) tới khi 2.5D thay bản 2D |

## Context

### Problem Statement

Camera 2D tính biên, zoom và quán tính trong `GardenScreen.cs` bằng toạ độ canvas (`ClampView`, `ApplyZoom`), lẫn với code dựng hình và không test được ngoài Unity. Camera 2.5D cần cùng cảm giác (kéo, quán tính, dừng mềm ở biên, chụm, ba mức zoom, chạm ô) trong toạ độ mét trên mặt đất.

### Current State

`UniversalRP.asset` có một renderer (`Renderer2D`, chỉ số 0). Chưa có camera phối cảnh, mesh đảo, `PhysicsRaycaster`. Zoom 2D: Xa `1920 / 2970`, Mặc định 0,83, Gần 1,5, ngưỡng chi tiết 1,165.

### Constraints

- Không `Update()` mới ngoài input; không toán hoạt ảnh `Mathf.Sin`/`Lerp` trong `Game`; không tìm trong scene.
- Bản 2D vẫn phát hành; 2.5D không được đổi hình của bản 2D.
- Core không biết Unity (ADR-0001).

### Requirements

- Tâm nhìn luôn trong hình bao đảo ở mọi mức zoom; đảo lớn ra khi mở đoạn (story-145 AC-1).
- Quán tính tắt dần khoảng 0,35 s, tắt hẳn dưới Giảm chuyển động; chụm giữ điểm giữa hai ngón (`interaction-patterns.md`).
- Chạm trúng ô, không trúng ô bên cạnh (AC-2); bản đồ có texture đúng kích thước (AC-3).

## Decision

1. **Khung hình ở Core.** `Core/GardenFraming.cs` giữ `CameraSpec` (FOV 25°, nghiêng 40°, khoảng cách mặc định 35 m, co giãn mép 0,6 m, quán tính còn 5 % sau 0,35 s, dừng dưới 0,05 m/s) và các hàm thuần: `Ground(Hex)`, `Island(segments)`, `TileAt(point)`, `Pose(focus, distance)`, `Drag`, `Settle`, `Glide`, `Pinch`, `ZoomAbout`, `DistanceOf/ZoomOf/ShowsDetail`, `Map`. `Game` đọc `CameraSpec` từ một ScriptableObject cài đặt (ADR-0002) và chỉ gán kết quả cho `Transform`/`Camera`.
2. **Đơn vị.** Một bước lục giác theo trục q là 1 m (cột 2D 75 px = 0,5 m); một hàng là `PitchY / PitchX` = 4/3 m, hàng lên trên màn 2D là hàng xa camera hơn (+Z). Biên đảo là hình chữ nhật bao tâm các ô theo `GardenGrid.Reach` cộng nửa ô.
3. **Zoom là khoảng cách camera**, giữ FOV và góc nghiêng. `distance = DefaultDistance × 0,83 / zoom2D`, nên ba mức và ngưỡng chi tiết 2D giữ nguyên ý nghĩa. Khoảng cách mặc định 35 m cho bề ngang nhìn thấy ở 1080×1920 bằng bản 2D (±5 %).
4. **Biên.** Kéo đặt tiêu điểm qua `Drag` (vượt mép co giãn, không quá 0,6 m); thả tay thì `Settle` kẹp vào đảo. Quán tính là `Glide` theo thời gian thật (không phụ thuộc số khung hình).
5. **Chạm.** Mặt đảo có `MeshCollider`; camera có `PhysicsRaycaster`; mặt đất nhận `IPointerClickHandler`, đổi điểm chạm sang ô bằng `GardenFraming.TileAt`. Trạm có collider riêng trên billboard (#146), vật gần camera thắng.
6. **Renderer.** Thêm `Assets/Settings/Garden25DRenderer.asset` (`UniversalRendererData`, Forward+) vào `m_RendererDataList` ở chỉ số 1; `Renderer2D` giữ chỉ số 0 và là mặc định. Camera 2.5D gọi `SetRenderer(1)`.
7. **Bản đồ #91.** Camera trực giao nhìn thẳng xuống trong cùng scene, `enabled = false`; `orthographicSize = Map(island, aspect).HalfHeight`, tâm `Map(...).Centre`. Khi mở bản đồ hoặc khi số đoạn đổi, render một lần vào `RenderTexture` cỡ pixel của `RawImage` bản đồ; đóng bản đồ thì trả texture. Không render mỗi khung.
8. **Công tắc.** Cảnh 2.5D bật bằng cờ `Features.Garden25D` (mặc định tắt, cùng kiểu `Features.Monetization`) cho tới khi thay được bản 2D.

### Architecture Diagram

```
GardenGrid (Core) -> GardenFraming (Core, pure) -> CameraPose / Hex / MapFrame
                                                      |
Game: Garden25D prefab -- GardenCameraRig (input events -> Drag/Glide/Pinch -> Pose -> Transform)
                       -- IslandGround (MeshCollider, IPointerClickHandler -> TileAt)
                       -- MapCamera (ortho, disabled, render on request -> RenderTexture -> map RawImage)
URP: UniversalRP.asset [0] Renderer2D (default, 2D garden)  [1] Garden25DRenderer (Forward+)
```

### Key Interfaces

- `GardenFraming.Pose(GroundPoint focus, double distance, CameraSpec) -> CameraPose`
- `GardenFraming.TileAt(GroundPoint, CameraSpec) -> Hex`
- `GardenFraming.Map(GroundRect island, double aspect) -> MapFrame`
- `Garden25DWorld.Load(int segments)`, `Grow(int)`, `Tick(float)`; `GardenGround.TileTapped(Hex)`; `GardenMapCamera.Shoot(GroundRect, int, int) -> RenderTexture`, `Release()`

### Implementation Guidelines

- `Game` must không tự tính biên, zoom hay quán tính; must gọi `GardenFraming`.
- Input kéo/chụm must đến từ sự kiện con trỏ (`IDragHandler`, `IBeginDragHandler`, `IEndDragHandler`) hoặc tick input đã có; quán tính chạy bằng tween hoặc tick input, không thêm `Update()` mới.
- Camera bản đồ must không bật thường trực.

## Alternatives Considered

### Alternative 1: Cinemachine 3 với confiner

- **Description**: `CinemachineCamera` + `CinemachineConfiner3D` cho biên, follow cho kéo.
- **Pros**: có sẵn damping, confiner.
- **Cons**: thêm gói; damping và confiner không test được ngoài Unity; khó ép đúng ba mức zoom 2D và Giảm chuyển động.
- **Rejection Reason**: logic khung hình nhỏ, viết thuần C# có test trong vài giây; `de-xuat-2-5d.md` §3 ghi Cinemachine "không bắt buộc".

### Alternative 2: Zoom bằng đổi FOV

- **Description**: giữ khoảng cách, đổi FOV.
- **Pros**: camera không di chuyển.
- **Cons**: FOV rộng làm méo billboard ở mép; lệch chuẩn FOV ~25°.
- **Rejection Reason**: `interaction-patterns.md` §2.5D ưu tiên đổi khoảng cách.

### Alternative 3: URP asset riêng cho 2.5D

- **Description**: một `UniversalRenderPipelineAsset` thứ hai, đổi `GraphicsSettings` khi vào cảnh.
- **Pros**: tách hẳn cài đặt.
- **Cons**: đổi pipeline asset lúc chạy là đổi toàn cục, dựng lại shader, dễ nháy.
- **Rejection Reason**: hai renderer trong một asset là cách URP hỗ trợ cho từng camera.

### Alternative 4: Bản đồ render mỗi khung

- **Rejection Reason**: tốn GPU trên mobile cho ảnh gần như đứng yên.

## Consequences

### Positive

- Biên, zoom, quán tính, chọn ô có 13 test chạy ngoài Unity (`GardenFramingTests`).
- Ba mức zoom và ngưỡng nhãn 2D giữ ý nghĩa nên nhãn ba bậc (#135) chuyển sang 2.5D không đổi luật.

### Negative

- Hai renderer song song; chi phí Forward+ trên mobile chưa đo.
- Hằng số zoom 2D lặp ở `CameraSpec` cho tới khi `GardenScreen.cs` đọc từ đó.

### Neutral

- Toạ độ mét do Core quy ước; mesh đảo phải dựng theo cùng quy ước.

## Risks

- Forward+ không chạy tốt trên máy yếu. Giảm thiểu: renderer data đổi sang Forward chỉ là một trường; đo ở #149.
- `SubmitRenderRequest` khác trên 6000.6. Giảm thiểu: test AC-3 chạy thật trong Unity trước khi đóng story.
- Biên hình chữ nhật cho tiêu điểm cho phép thấy biển trống ở góc đảo không vuông khi zoom xa. Giảm thiểu: chấp nhận ở bước 1; nếu ảnh trước/sau xấu thì kẹp theo đa giác đảo.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| [mo-rong-vuon-va-ban-do.md](../../design/gdd/mo-rong-vuon-va-ban-do.md) | Vườn kéo, zoom; mở đoạn làm đảo lớn ra; bản đồ #91 | `Island(segments)`, `Drag/Settle`, `Map` |
| [interaction-patterns.md](../../design/ux/interaction-patterns.md) | Quán tính ~0,35 s, dừng mềm ở biên, chụm quanh hai ngón, ba mức zoom, chạm bằng `PhysicsRaycaster` | `Glide`, `Drag`, `ZoomAbout`, `DistanceOf`, `TileAt` |

## Performance Implications

- **CPU**: vài phép tính double mỗi lần có input; 0 byte cấp phát (struct).
- **Memory**: một `RenderTexture` cỡ màn khi bản đồ mở.
- **Load Time**: không đáng kể.
- **Network**: không có.

## Migration Plan

1. Core `GardenFraming` + test (xong 26/11).
2. ScriptableObject cài đặt camera, renderer data Forward+, cảnh `Garden25DWorld` dựng bằng code sau `Features.Garden25D` (xong 26/11; dựng bằng code vì `Garden.unity` không được commit).
3. Mesh đảo chữ nhật, `PhysicsRaycaster`, chạm ô; camera bản đồ (xong phần code 26/11; mép đảo và màn bản đồ còn lại).
4. `GardenScreen.cs` đọc hằng số zoom từ `CameraSpec`; khi 2.5D thay bản 2D, bỏ `ClampView` 2D.

**Rollback plan**: tắt `Features.Garden25D`; bỏ renderer chỉ số 1.

## Validation Criteria

- `tools/ci/core-tests.sh --filter GardenFramingTests` xanh.
- `grep -nE "Mathf\.(Clamp|Lerp)|ClampView" Assets/VuonTra/Game/Garden25D` không có kết quả.
- AC-3 test: `RenderTexture` khác null và đúng cỡ sau khi mở bản đồ hai lần.

## Related

- ADR-0001, ADR-0002, ADR-0005; `production/epics/vuon-2-5d/story-145-dat-va-camera.md`.
- `Assets/VuonTra/Core/GardenFraming.cs`, `Assets/Tests/EditMode/GardenFramingTests.cs`.

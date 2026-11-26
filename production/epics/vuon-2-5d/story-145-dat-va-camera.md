# Story 145: Đất và camera

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#145
> **Epic**: Vườn 2.5D
> **Status**: In Review
> **Layer**: Presentation
> **Type**: Integration
> **Estimate**: L (6–9 ngày theo docs/de-xuat-2-5d.md §6)
> **Manifest Version**: none — `docs/architecture/control-manifest.md` chưa có
> **Last Updated**: 2026-11-26

Một story = một issue: thảo luận, ảnh và nghiệm thu ở issue; acceptance criteria sửa ở file này ([docs/studio/HUONG-DAN-STUDIO.md](../../../docs/studio/HUONG-DAN-STUDIO.md) §3). Issue lớn hơn một phiên (`story_granularity: balanced`), nên `/dev-story` có thể chạy nhiều phiên trên cùng story.

## Context

**GDD**: `design/gdd/mo-rong-vuon-va-ban-do.md` (cảnh vườn), chuẩn hình `design/art/art-bible.md`, lộ trình `docs/de-xuat-2-5d.md` §6 bước 1
**Requirement**: (chưa cấp TR-ID — `docs/architecture/tr-registry.yaml` chưa dựng, kế hoạch di trú Bước 3a)

**ADR Governing Implementation**: ADR-0005: Hướng 2.5D; ADR-0006: Camera và renderer vườn 2.5D (Accepted 26/11)
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

*Đã có (26/11):* Core `GardenFraming.cs`, `IslandShape.cs`, `Features.cs` (`Garden25D`); Game `Garden25D/` (`GardenCameraSettings`, `GardenCameraRig`, `GardenGround`, `GardenPanZoom`, `GardenMapCamera`, `Garden25DWorld`), `GameRunner.cs` (dựng khi bật cờ); `Assets/Settings/Garden25DRenderer.asset` + `UniversalRP.asset` (renderer chỉ số 1); `Assets/Resources/Garden25D/` (`GardenCameraSettings.asset`, `IslandLawn.mat`, `IslandEarth.mat`); test `GardenFramingTests`, `IslandShapeTests`, `GardenCameraRigTests`, `Garden25DWorldTests`; `docs/architecture/adr-0006-camera-va-renderer-vuon-2-5d.md`. *Lát 3:* `Game/UI/GardenMapView.cs`, `GardenScreen.cs` (lớp 2D lùi lại khi bật cờ), `Editor/PreviewShot.Garden25D.cs`, test `GardenTouchTests`, `Garden25DScreenTests`, ảnh `docs/anh/po-145/`. *Còn dự kiến (sau story này):*

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

**Status**: [x] `GardenFramingTests` (13, Core: AC-1 biên, chọn ô, khung bản đồ), `GardenTouchTests` (7, Core: ngưỡng chạm 12, chạm đúp, bước zoom, điểm trên bản đồ, biển quanh đảo), `IslandShapeTests` (10, Core: mặt đảo ba dải, phía trăng, cờ tắt, quán tính), `GardenCameraRigTests` (2, Unity), `Garden25DWorldTests` (15, Unity: chạm ô qua `PhysicsRaycaster` AC-2, kéo/chụm không thành chạm, chạm đúp, kéo/biên AC-1, chụm, Giảm chuyển động, `RenderTexture` bản đồ hai lần AC-3, renderer Forward+ chỉ số 1, màu đảo), `Garden25DScreenTests` (9, Unity: hai trạng thái cờ, màn bản đồ AC-3, vi/en). Ảnh: `docs/anh/po-145/`, `visual-critic-gate` APPROVED (`VERDICT.md`).

---

## Dependencies

- Depends on: None
- Unlocks: story-146, story-149

---

## Tiến độ 26/11 đêm

**story-readiness** (lean, QL-STORY-READY bỏ qua): không có blocker. ADR-0005 Accepted, Dependencies None, `bg_grass.png` có. Gap nhỏ, không chặn: Context chưa trích một luật cụ thể của GDD (chỉ tên file); Engine Notes Forward+ mobile vẫn NOT SOURCEABLE (`/setup-engine refresh` chưa chạy); chưa có TR-ID.

**Đã làm**
- ADR-0006 `docs/architecture/adr-0006-camera-va-renderer-vuon-2-5d.md` (Accepted 26/11): khung hình tính ở Core, zoom là khoảng cách camera, renderer Forward+ thứ hai ở chỉ số 1, bản đồ là camera trực giao render theo yêu cầu, cờ `Features.Garden25D` mặc định tắt.
- `Assets/VuonTra/Core/GardenFraming.cs`: `CameraSpec` (FOV 25°, nghiêng 40°, mặc định 35 m, co giãn mép 0,6 m, quán tính còn 5 % sau 0,35 s, dừng < 0,05 m/s) và hàm thuần `Ground`, `Island`, `TileAt`, `Pose`, `Drag`, `Settle`, `Glide` (tắt dưới Giảm chuyển động), `Pinch`, `ZoomAbout`, `DistanceOf`/`ZoomOf`/`ShowsDetail`, `Map`. Ba mức zoom 2D (Xa 1920/2970, Mặc định 0,83, Gần 1,5, chi tiết 1,165) đổi thành khoảng cách camera.
- `Assets/VuonTra/Game/Garden25D/GardenCameraSettings.cs` (ScriptableObject cài đặt camera) và `GardenCameraRig.cs` (chỉ gán `CameraPose` cho camera, không `Update()`).
- Test: 13 `GardenFramingTests` chạy ngoài Unity, 2 `GardenCameraRigTests` trong Unity.

**Quyết định (chủ project duyệt ADR-0006 và cờ `Features.Garden25D` ngày 26/11)**
- Đơn vị: một bước lục giác theo q = 1 m, một hàng = 4/3 m (giữ tỉ lệ `PitchY/PitchX` của bản 2D); hàng phía trên màn 2D là hàng xa camera.
- Biên kéo là hình chữ nhật bao đảo cho tâm nhìn (AC-1 "tâm nhìn trong hình bao của đảo"); mép đảo không vuông có thể lộ góc trống khi zoom xa, xem lại bằng ảnh trước/sau.
- Khoảng cách mặc định 35 m cho bề ngang thấy ở 1080×1920 bằng bản 2D (±5 %).

**Tiếp theo**
1. Asset `GardenCameraSettings` và `Assets/Settings/Garden25DRenderer.asset` (Forward+) ở chỉ số 1 của `UniversalRP.asset`; cờ `Features.Garden25D`.
2. Input kéo/chụm trên prefab `Garden25D` gọi `Drag`/`Glide`/`Pinch`/`ZoomAbout` (sự kiện con trỏ, không `Update()` mới).
3. Mesh đảo (mép đất dày, ô cỏ #129) + `MeshCollider` + `PhysicsRaycaster`, chạm đất → `TileAt` (AC-2 trong Unity).
4. Camera bản đồ trực giao + `RenderTexture` cho #91 (AC-3), kiểm `SubmitRenderRequest` trên 6000.6.
5. Khung trước/sau `docs/anh/po-145/`, `visual-critic-gate`.

## Tiến độ 26/11 sáng (lát 2)

**Đã làm**
- Sửa cache gói của worktree: `Library/PackageCache/com.unity.ugui@e13c0366db78` có thư mục `TMP` rỗng sau khi chuyển iCloud (TMPro không biên dịch); chép lại từ `BuiltInPackages/com.unity.ugui` của editor. Sau đó Unity biên dịch sạch.
- Cờ `Features.Garden25D` (mặc định tắt, `Garden25DByDefault = false`); `GameRunner` chỉ dựng cảnh 2.5D khi bật cờ, bản 2D không đổi.
- Renderer `Assets/Settings/Garden25DRenderer.asset` (`UniversalRendererData`, Forward+) ở chỉ số 1 của `UniversalRP.asset`; `Renderer2D` vẫn chỉ số 0, mặc định.
- `Assets/Resources/Garden25D/`: `GardenCameraSettings.asset` (25°, 40°, 35 m, mép 0,6 m, quán tính 0,35 s, đảo dày 0,8 m, ô cỏ lặp mỗi 2 m), `IslandLawn.mat` (URP Lit, `bg_grass.png` #129), `IslandEarth.mat` (màu đất tạm `#89574C`, chờ design).
- `IslandShape.Slab` (Core): đảo là khối chữ nhật, mặt cỏ và bốn mép đất là hai submesh. `GardenGround`: mesh + `MeshCollider`, `IPointerClickHandler` → `TileTapped(Hex)` chỉ cho ô đã mở. `GardenPanZoom`: kéo một ngón (co giãn ở mép), chụm hai ngón quanh điểm giữa, cuộn chuột, thả tay trượt bằng `Tweens` (không `Update()` mới; `GameRunner.Update` đã có gọi `Tick`), Giảm chuyển động dừng ngay. `GardenMapCamera`: camera trực giao tắt sẵn, `Shoot` render một lần vào `RenderTexture` qua `SubmitRenderRequest`, `Release` trả texture.
- Ảnh kiểm nhanh (không commit, render trong Unity batch): đảo nghiêng có mép đất ở zoom mặc định và xa, ảnh bản đồ nhìn thẳng xuống đúng khung. Chưa chấm `visual-critic-gate`.

**Quyết định**
- Theo đề xuất ADR-0006 mà coordinator chọn dùng: 1 bước lục giác = 1 m, camera 35 m. ADR-0006 đã Accepted (chủ project duyệt 26/11).
- Không có DOTween trong dự án; quán tính dùng `Tweens` sẵn có của `Ease.cs`.

**Tiếp theo**
1. Khi bật cờ: ẩn phần thế giới 2D của `GardenScreen` (canvas Screen Space Overlay đang phủ camera 3D), nối `TileTapped` vào mở trạm/ô (#146 cho trạm).
2. Mặt đảo theo art bible: mép bo, viền cỏ rủ, lối mòn cong; vùng chưa mở tối một bậc, hàng rào thấp, biển gỗ (cần design).
3. Màn bản đồ #91 dùng `GardenMapCamera.Shoot` thay bản vẽ 2D; chạm đúp zoom Mặc định ↔ Gần; ngưỡng phân biệt kéo/chạm 12 đơn vị.
4. Khung trước/sau 1080×1920 ở `docs/anh/po-145/`, `visual-critic-gate`; vi/en, Chữ to, Giảm chuyển động.
5. `/setup-engine refresh` cho Forward+ mobile; đo ở #149.


## Tiến độ 26/11 trưa (lát 3)

**Đã làm** (mọi phần sau cờ `Features.Garden25D`, mặc định vẫn tắt)
- Bật cờ thì lớp vườn 2D của `GardenScreen` lùi lại: canvas chuyển sang Screen Space Overlay, camera 2D tắt, lớp `Ground` và `Garden` (nền cỏ, nội dung, tán cây) ẩn, viên mép ▲▼◀▶ và nút "Về tiệm trà" không hiện; HUD, thanh tab, sheet vẫn chạy. Giữa màn không còn đồ họa nào nhận chạm, nên chạm đi tới `PhysicsRaycaster` của camera đảo. Nút kính lúp đổi khoảng cách camera đảo Xa → Mặc định → Gần (`GardenFraming.NextZoomStep`); nút chụp ảnh chụp qua camera đảo. Tắt cờ thì mọi thứ như cũ (test cả hai trạng thái).
- Màn bản đồ #91 (`GardenMapView`): viên "Bản đồ"/"Map" đứng chỗ nút về tiệm, mở ảnh `GardenMapCamera.Shoot` nằm dưới HUD và trên vườn, chừa 1 m biển đêm quanh đảo (`GardenFraming.Pad`); chạm một chỗ trên bản đồ thì đóng bản đồ, trả texture và đưa camera tới đó `Motion.Travel` `InOutCubic` (Giảm chuyển động: tới ngay). Nút × và viên có vùng chạm 132 (44 pt). Camera bản đồ cũng dùng renderer chỉ số 1.
- Chạm đúp: hai chạm trong 0,35 s, cách nhau ≤ 48 đơn vị canvas, thì chuyển Mặc định ↔ Gần quanh điểm chạm (`GardenFraming.IsDoubleTap`, `DoubleTapDistance`), `Motion.Travel` `InOutCubic`, tức thì dưới Giảm chuyển động. Tự đếm chạm thay cho `clickCount`, vì `InputSystemUIInputModule` bỏ trạng thái ngón chạm sau mỗi lần nhấc nên `clickCount` luôn là 1 trên điện thoại.
- Ngưỡng kéo/chạm 12 đơn vị canvas (`GardenFraming.TapSlopPixels`, theo tỉ lệ `CanvasScaler` khớp 0,5): ngón chưa đi quá ngưỡng không kéo vườn; một lần kéo (kể cả kéo rồi quay về chỗ cũ) hoặc chụm không bao giờ thành chạm ô. Chạm ô đi qua `GardenPanZoom.Tapped` → `GardenGround.Tap`.
- Màu mép đảo của design (art bible §4/§6) thay màu tạm `#89574C`: viền cỏ rủ `#366F62` (0,12 m), khe bóng dưới viền `#453448` (0,05 m), đất phía trăng `#85554C`, đất phía tối `#60424A`, đáy `#362C47`. `IslandShape.Slab` chia mỗi mép thành ba dải và thêm mặt đáy; phía sáng/tối theo hướng trăng `moonFrom` (−1, −1: trái và trước sáng, phải và sau tối, theo "trăng trên-trái" của art bible). Màu, độ dày dải và hướng trăng nằm trong `GardenCameraSettings.asset`, không ở C#. `IslandEarth.mat` và `IslandLawn.mat` đổi sang URP Unlit (bậc màu phẳng, cỏ sáng như bản 2D) cho tới khi #147 có ánh trăng; ô cỏ lặp mỗi 3,4 m cho cỡ ngọn cỏ bằng bản 2D ở zoom mặc định.
- Khung trước/sau 1080×1920 ở zoom mặc định: `docs/anh/po-145/145-truoc.png`, `145-sau.png`, `145-sau-en-chu-to.png` (en + Chữ to), `145-sau-ban-do.png`, tấm ghép `145-truoc-sau.png` (kèm mép trước 200 %), dựng bằng `PreviewShot.RenderGarden25D` (menu "VuonTra/Render 2.5D Garden Before And After (#145)"), bật cờ chỉ trong công cụ và trả về mặc định sau đó. Khi chụp, canvas chuyển tạm sang Screen Space Camera trên camera đảo để HUD vào được ảnh; trong game canvas là Overlay.
- Test: `GardenTouchTests` (7, Core), `IslandShapeTests` (10, Core), `Garden25DWorldTests` (15, Unity), `Garden25DScreenTests` (9, Unity: hai trạng thái cờ, không gì che giữa màn, viên mép ẩn, kính lúp và sheet, bản đồ mở hai lần, chạm bản đồ, Giảm chuyển động, tiếng Anh).

**Quyết định**
- Trăng trên-trái nên mép trước (phía camera) là đất sáng `#85554C`; mép phải là `#60424A`. Đổi hướng chỉ cần sửa `moonFrom` trong asset.
- Nút đóng bản đồ ở góc trên-trái vì góc trên-phải dưới HUD là chỗ của chip sự kiện (vùng B).

**Trạng thái: In Review.** Còn một tiêu chí chưa đạt, chờ design: AC-1 phần hình đảo và vùng chưa mở (mép bo, viền cỏ rủ có hình, lối mòn cong; vùng chưa mở cỏ tối một bậc, hàng rào thấp, biển gỗ). Đảo hiện là khối chữ nhật. Ghi chú cho người duyệt: AC-2 "chạm trúng trạm" chỉ làm được khi trạm đứng dậy ở #146 (ADR-0006 §5: collider trên billboard); phần ô đất đã có test. AC-1 "mở game thấy" chỉ đúng khi bật cờ, cờ vẫn mặc định tắt theo quyết định chủ project.

**Tiếp theo (ngoài story này)**
- Design: hình đảo và vùng chưa mở; lớp màu nước cho mặt đất; viền trên bản đồ; màu nền viên "Bản đồ".
- #146: chạm trạm, nối `TileTapped`; #147: ánh trăng (đổi cỏ/đất về Lit hoặc toon); #148: HUD trên camera 2.5D (vùng B ẩn khi mở bản đồ, cảnh chụp ảnh có HUD); #149: Forward+ trên máy yếu, ảnh bản đồ mất khi app vào nền trên mobile (chụp lại khi quay lại).
- Bản đồ: kéo và chụm trên bản đồ (mẫu "Bản đồ" của interaction-patterns) chưa làm; hiện chỉ chạm để tới.

# ADR-0007: Billboard vườn 2.5D: SpriteRenderer chân ở gốc, hướng cố định theo camera, bảng tỉ lệ ở Core, chạm qua collider chuyển tiếp cho kéo/zoom

## Status

Proposed — design duyệt một phần (2026-11-26), chủ project chưa trả lời

> Session design ("Độ nét đồ họa") trả lời trên #146 ngày 2026-11-26, nguyên văn: "ADR-0007: DUYỆT phía design, đúng art bible §5 §8", kèm hai thay đổi bắt buộc (bóng `shadow_elip`, đế trạm). Design đính chính sau đó: duyệt **chỉ** gồm `SpriteRenderer`, pivot ở chân, hướng cố định theo camera, `SortingGroup` và collider chuyển sự kiện (mục 3, 4, 6, 7 và cách đặt chân của mục 11); **không** duyệt việc thay giống thỏ của người chơi bằng Cream. Design cũng duyệt nền cỏ tối (mục 12) với màu tạm `#31605C`, không sương, giữ camera 35 m / 40° của ADR-0006. Duyệt kiến trúc tách riêng với visual gate: commit chỉ sau khi visual gate APPROVED và full verify GREEN trên cây cuối. Chủ project chưa trả lời.

## Date

2026-11-26 (agent-146, story-146 lát 1)

## Last Verified

2026-11-26

## Decision Makers

agent-146 (đề xuất); session design duyệt 2026-11-26; chủ project chưa trả lời.

## Summary

ADR-0005 chốt "mọi hình 2D đứng thẳng làm billboard pivot ở chân", ADR-0006 §5 chốt "trạm có collider riêng trên billboard, vật gần camera thắng", nhưng chưa nói billboard dựng bằng gì, xoay thế nào, chiều cao lấy từ đâu, xếp lớp ra sao và chạm trạm đi qua kéo/zoom thế nào. Quyết định: billboard là `SpriteRenderer` (để `SpriteLibrary`/`SpriteResolver` và rig PSB của thỏ dùng chung một kiểu) trong một gốc đặt ở chân; hướng quay bằng hướng camera một lần khi đặt (camera không xoay nên không cần cập nhật mỗi khung); chiều cao theo bảng tỉ lệ ở `Core/GardenStands.cs`; mỗi trạm là một `SortingGroup` xếp theo trục sâu của camera; collider của trạm chuyển sự kiện con trỏ cho `GardenPanZoom` để kéo bắt đầu trên trạm vẫn kéo vườn.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1, URP 17.6.0, `com.unity.2d.animation` 16.0.1 |
| **Domain** | Rendering, Input |
| **Layer** | Presentation (bảng tỉ lệ ở Core) |
| **Knowledge Risk** | MEDIUM: `Camera.transparencySortMode = CustomAxis` với renderer Forward+ của URP 17 chưa tra nguồn cho 6000.6; kiểm bằng ảnh trước/sau |
| **References Consulted** | ADR-0005, ADR-0006, `design/art/art-bible.md` §5, §8, `design/assets/specs/tram-mau-25d-assets.md`, `Sprite-Lit-Default.shader` (có pass `UniversalForward`) |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Thứ tự vẽ đúng giữa trạm gần và xa trên ảnh 1080×1920; chạm trạm qua `PhysicsRaycaster` thắng mặt đất (test EditMode) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0005, ADR-0006 |
| **Enables** | story-146 (billboard, chạm trạm), story-147 (đổi vật liệu billboard sang Lit alpha clip có bóng theo trăng) |
| **Blocks** | None |
| **Ordering Note** | Bóng elip phẳng là bước tạm tới #147 |

## Context

### Problem Statement

Vườn 2.5D (story-145) chỉ có mặt đảo. Trạm, đồ và thỏ phải đứng trên ô của `GardenGrid`, đúng tỉ lệ art bible (thỏ = 1), chạm được, và thỏ chạy vòng làm việc của rig PSB đã có (`RabbitCream.prefab`) mà không thêm `Update()` hay toán hoạt ảnh.

### Constraints

- Không `Update()` mới, không `Mathf.Sin/Lerp` trong `Game`, không tìm trong scene.
- Rig thỏ là `SpriteRenderer` + `SpriteSkin` + `SpriteResolver` trong `SortingGroup`; trạng thái đồ vật trạm đổi bằng `SpriteResolver` (`docs/kiem-dinh-do-hoa.md` §4a).
- Bản 2D vẫn phát hành; mọi thứ sau `Features.Garden25D`.

## Decision

Design đã duyệt hướng này (2026-11-26) với hai thay đổi ở mục 5 và 10; chủ project chưa trả lời. Phần dùng `GardenFraming.Ground`, camera không xoay, `PhysicsRaycaster` + collider và handler chạm là ADR-0005/0006 đã chốt; phần còn lại (SpriteRenderer thay quad Lit alpha clip, bóng tạm, đế, Cream ở ba trạm mẫu, luật import, nền đất khoá) là quyết định của ADR này. Chưa merge, chưa nghiệm thu.

1. **Bảng tỉ lệ ở Core.** `GardenStands.Range(StandRole)`: thỏ 1; trạm 0,8–1,3; quầy 1,8–2,2; đèn cột 1,4; bể cá 0,9; cối 0,7; cây 2,5–3,5; sai số cho phép ±15 % (`StandRange.Holds`). `Metres(role, ratio, rabbitMetres)` kẹp tỉ lệ mong muốn vào bảng. Cũng ở Core: trạm nào hiện (`Shown`, giống luật của phố 2D), trạm đang ở dạng đất/chưa xây/mở (`Look`), ô của trạm (`StationTile`), thỏ nào làm ở trạm nào (`Workers`, không cấp phát).
2. **Chiều cao thỏ.** `rabbitMetres` trong `GardenStandSettings` (ScriptableObject, ADR-0002) là 1,15 m: billboard thẳng đứng dưới camera nghiêng 40° hiện ngắn đi `cos 40°`, nên 1,15 m cho khoảng 110 px trên màn 1080×1920 ở zoom mặc định (art bible §5).
3. **Billboard là `SpriteRenderer`.** Gốc `GardenStand` đặt ở chân (`GardenFraming.Ground(ô)`, y = 0); con `Art` dời sao cho chân hình chạm y = 0 và giữa hình ở gốc: với hình trong `Art/Garden25D` chân là pivot (mục 11), với hình 2D dùng tạm là đáy `Sprite.bounds` (mesh Tight); chiều cao bằng `localScale`. Không dựng quad riêng: một kiểu renderer cho trạm và rig thỏ, `SpriteResolver` dùng được cho trạng thái trạm.
4. **Hướng.** Gốc quay quanh trục dọc bằng góc yaw của camera, đặt một lần khi dựng. Camera 2.5D không xoay (ADR-0006), nên không có cập nhật mỗi khung.
5. **Bóng (design yêu cầu, art bible §10 bản c2d394a).** Bóng là `shadow_elip.png` (elip mực Ink, alpha tâm 0,6, mép mềm), vẽ bằng alpha blend, tint trắng, rộng 1,3 × bề ngang đáy hình (`shadowShare`), sâu 0,55 bề rộng. Điểm neo là tâm đế: tâm ngang là giữa cột đục trái nhất và phải nhất ở dải đáy hình (mục 11), và elip đặt sao cho 35 % bề sâu ló ra trước pixel đáy (`shadowInFront`), phần còn lại nằm dưới vật, nên vật không lơ lửng trên bóng. Trên đế trạm, alpha bóng nhân `padShadowAlpha` 0,45 để bóng trạm và bóng thỏ chồng nhau vẫn không tối hơn đế pha 50 % mực. Bóng của thỏ dựng ngoài `SortingGroup` con của rig ở thứ tự −10; renderer bóng cũ trong rig tắt. #147 thay bằng bóng theo trăng khi có Lit alpha clip.
6. **Xếp lớp.** Mỗi trạm là một `SortingGroup` cho phần đứng (đồ vật 0, thỏ 1); phần nằm trên đất (đế −20, bóng −10 của trạm và của thỏ, đá lối −20) là renderer rời ngoài nhóm, đi theo gốc trạm qua `GardenStand.Flat`, nên mọi thứ nằm phẳng vẽ trước mọi thứ đứng và đế của một trạm không đè lên đồ đứng ngay sau nó; camera đảo dùng `transparencySortMode = CustomAxis` theo trục sâu mặt đất (0, 0, 1), nên trạm có chân gần camera hơn vẽ sau, kể cả khi lệch trái phải.
7. **Chạm.** `BoxCollider` trên gốc trạm bao hình trạm và thỏ của nó. `GardenStand` nhận `IPointerDown/Up/Drag/Scroll/Click` và chuyển cho `GardenPanZoom`, nên kéo bắt đầu trên trạm vẫn kéo vườn, và chạm/kéo vẫn phân biệt ở một chỗ (ngưỡng 12 đơn vị). `GardenPanZoom.Tapped` đưa sự kiện cho `Garden25DWorld.Touch`: nếu vật được nhấn có `GardenStand` thì `GardenStandRow.StationTapped(index)`, không thì `GardenGround.Tap`. `PhysicsRaycaster` xếp theo khoảng cách nên trạm trước mặt đất thắng.
8. **Thỏ (vị trí).** Thỏ đứng ở chỗ làm việc của trạm, trước-trái hình trạm, chân chồng nhẹ lên đế (mục 10), không ra giữa bãi cỏ. Nếu thỏ thấp hơn 100 px ở zoom mặc định thì tăng `rabbitMetres` hoặc kéo camera gần, không co giãn thỏ ra ngoài bảng tỉ lệ.
9. **Thỏ (rig, giữ đúng giống).** Mọi thỏ hiện đúng hình giống của nó (`GameCatalog.RabbitArt`). Rig chỉ dùng cho đúng giống nó vẽ: ánh xạ giống → rig là dữ liệu, bảng `GardenStandSettings.rigs` (mỗi dòng một `Kind` và một prefab rig), và ở ba trạm mẫu chỉ thỏ thuộc giống có dòng trong bảng mới chạy vòng 4 nhịp bằng Animator/clip. Không bao giờ thay giống khác bằng Cream để qua visual QA. **Khoảng trống:** chưa có ánh xạ chính thức từ rig Cream sang `RabbitKind.Id` nào (nguồn duy nhất là tên asset `RabbitRigSpec.Kind = "Cream"`; Core có `tho-trang` và `tho-cup-kem` nhưng rig Cream tai dựng còn `tho-cup-kem` tai cụp), nên bảng ghi một dòng: Cream → `tho-bong` (Thỏ Bông, bậc 0, tai dựng, giống mọi người chơi đều có). Đây là mặc định của PO (luật 22/11: câu hỏi nhỏ lấy mặc định, chủ project đổi sau) cộng xác nhận hình của design (26/11: cùng tai dựng, tai trong và má hồng, mắt chấm, lông kem trắng; khác: lông rig ấm hơn #FBE7CB so với #FEF4DA, thân tròn hơn, không thấy đuôi từ phía trước, chấp nhận được ở cỡ billboard; design đang chỉnh màu lông rig về #FEF4DA ở nhánh `sua-146-asset`). Chủ project chưa trả lời. Không bao giờ `tho-trang` hay `tho-cup-kem`. Các giống khác giữ hình của mình, không có vòng 4 nhịp; sau này mỗi giống có lớp lông riêng qua `SpriteLibrary`, giống tai cụp cần rig thứ hai, ngoài phạm vi #146.
10. **Đế trạm (design yêu cầu).** Mỗi trạm có một đế phẳng (sàn gỗ tròn, đất, khóm hoa như bản 2D: `GameCatalog.StationPad(i)`, màu `StationActorView.FloorTint`) nằm trên đất trong `SortingGroup` của trạm, thứ tự −20, dưới bóng (−10). Design: thiếu đế là lý do chính khiến trạm trông như dán lên cỏ.
11. **Luật import cho billboard 2.5D (design yêu cầu, art bible §11).** Hình đứng trong `Assets/VuonTra/Art/Garden25D/Stand` lọc Bilinear, không mipmap (gate so A/B 27/11: trilinear để răng cưa tối ở mái và cột quầy, bilinear sạch); mảnh nằm phẳng trong `Garden25D/Flat` giữ mipmap và Trilinear vì nhìn xiên. Cả hai không nén, mesh Tight. Pivot hình đứng ở chân thật: hàng thấp nhất có alpha ≥ 0,5, chiều ngang ở giữa cột đục trái nhất và phải nhất của dải chân (3 % chiều cao), nên cột đèn đứng và đổ bóng dưới chân cột chứ không dưới giữa hình. `Art/Final` của bản 2D giữ nguyên luật cũ (mipmap tắt); hình dùng chung có hai bản. Test kiểm cả hai luật.
12. **Nền đất khoá quanh đảo (phương án A, coordinator chọn 26/11, design duyệt; chỉnh vòng 4 và art bible §8 bản c2d394a).** Dải màu phẳng `#31605C` rộng 1,5 m hai bên và 2,5 m phía sau, không vượt mép trước; trước mép trước là mặt dưới `#212047` (cùng vật liệu mặt dưới đảo) phủ tới hết vùng, nằm trên dải và đất khoá, nên dưới vách trước không thấy cỏ; xa hơn là `#23374E` (không sương, không texture), thấp hơn mặt cỏ đúng bằng độ dày đảo cộng 0,01 m, rộng ra 60 m, có collider chuyển kéo/chạm cho `GardenPanZoom`; dựng lại bằng cách đổ lại mesh cũ (không `Destroy` khi đảo lớn ra, edit mode cấm). Giữ nguyên số camera của ADR-0006: ở 35 m, FOV 25°, nghiêng 40° khung nhìn phủ ~26 m mặt đất trước-sau, đảo lưu thử chỉ sâu ~10–13 m; muốn đảo lấp khung phải kéo camera về ~15 m (thỏ ~250 px) hoặc đổi góc, tức sửa ADR-0006 (phương án C, không chọn). Nền này là chỗ giữ cho hình vùng chưa mở của design (#145 còn lại): xong phần đất, chưa có hình, hàng rào, biển gỗ.

13. **Lối và hình đảo (design vòng 4).** Lối nối trạm là mảnh `loi_thang` #129 nằm phẳng, kéo đều giữa hai chân, thứ tự −30 dưới đế (`GardenStands.PathPieces`). Bờ đảo bo góc 4 m và mép trước gợn ±0,4 m (`IslandShape.Shore/Outline`, bờ nới ra để không cắt ô đã mở); giới hạn kéo giữ mép trước cách đáy màn 15 % (`GardenFraming.FocusArea`, `CameraSpec.FrontEdgeShare`), không đổi số camera của ADR-0006.
14. **Hình cũ chờ #131 (art bible §12).** Hình cũ đặt bằng dữ liệu, không vẽ lại trong code: nồi hồng `noi-mut` cao 1,0 thỏ (scale theo chiều cao, bề ngang theo hình); tiệm và cối tint 0,8 qua `FixtureStand.Tint`. Đồ trang trí và cột đèn giữ khoảng `KeepClear`/`LampKeepClear` với cả cối, tiệm và bể cá.

### Key Interfaces

- `GardenStands.Range(StandRole) -> StandRange`, `Metres`, `Shown`, `Look`, `StationTile`, `Workers(GameState, long[])`
- `Garden25DWorld.Raise(GameCatalog)`; `GardenStandRow.Refresh(IReadOnlyList<StationDef>, GameState)`; `event Action<int> StationTapped`
- `GardenStand.Show(Sprite, double metres, Color)`, `Height`, `Foot`

## Alternatives Considered

### Alternative 1: Quad + vật liệu Lit alpha clip ngay ở #146 (theo ghi chú story)

- **Pros**: nhận sáng và đổ bóng thật.
- **Cons**: rig thỏ là `SpriteRenderer`/`SpriteSkin`, phải đổi cả hai cách vẽ; `SpriteResolver` không đổi được texture của quad; ánh sáng là phạm vi #147.
- **Rejection Reason**: để #147 đổi vật liệu chung của `SpriteRenderer` một chỗ.

### Alternative 2: Billboard quay theo vị trí camera mỗi khung

- **Cons**: cần `Update()`/`LateUpdate()` mới; camera không xoay nên kết quả gần như không đổi.
- **Rejection Reason**: luật không `Update()` mới; hướng cố định đúng với camera cố định.

### Alternative 3: Xếp lớp bằng `sortingOrder` theo hàng

- **Cons**: phải đổi số thứ tự khi trạm đổi chỗ; dễ lệch với hạt và nhãn.
- **Rejection Reason**: trục sâu của camera làm việc đó không cần dữ liệu thêm.

## Consequences

### Positive

- Bảng tỉ lệ có test ngoài Unity; chiều cao mỗi billboard có test trong Unity.
- Không thêm `Update()`; mọi thay đổi đi qua `Refresh` khi trạng thái đổi.

### Negative

- Bóng elip phẳng chưa theo hướng trăng tới #147.
- Thỏ ở ba trạm mẫu luôn là Cream tới khi các giống khác có rig.

## Risks

- `CustomAxis` không được URP Forward+ dùng: thứ tự vẽ sai giữa trạm gần/xa. Giảm thiểu: ảnh trước/sau; nếu sai thì xếp lại bằng `sortingOrder` theo hàng.
- Hình `Final` cũ có viền trong suốt dày ở đáy: chân trông lơ lửng. Giảm thiểu: dùng `Sprite.bounds` của mesh Tight.

## Validation Criteria

- `GardenStandsTests` (Core) xanh; `Garden25DStandTests` (Unity): mọi trạm hiện có billboard, chân y = 0 trên ô của nó, chiều cao trong bảng ±15 %, thỏ ≈ 110 px ở zoom mặc định, chạm trạm qua `PhysicsRaycaster` gọi đúng trạm, kéo bắt đầu trên trạm không thành chạm.
- `grep -nE "void (Update|LateUpdate)|Mathf\.(Sin|Lerp)" Assets/VuonTra/Game/Garden25D` không có kết quả.

## Related

- ADR-0005, ADR-0006; `production/epics/vuon-2-5d/story-146-dung-dung-billboard.md`.

# Story 146: Trạm, đồ và thỏ dựng đứng có bóng, bảng tỉ lệ, vòng làm việc ba trạm mẫu

> **GitHub**: KaneNguyen03/moon-rabbit-tea-garden#146
> **Epic**: Vườn 2.5D
> **Status**: In Progress
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

*Bản nháp lát 1, chưa commit:*

- `Assets/VuonTra/Core/GardenStands.cs`: bảng tỉ lệ, trạm hiện/ẩn, ô đặt, thỏ làm việc.
- `Assets/VuonTra/Game/Garden25D/GardenStand.cs`, `GardenStandRow.cs`, `GardenStandSettings.cs`, `Garden25DWorld.cs`: dựng hình, collider, sự kiện chạm/kéo; cấu hình `Assets/Resources/Garden25D/GardenStands.asset`.
- `Assets/VuonTra/Game/UI/GardenScreen.cs`: nối dựng trạm và chạm đất/trạm, dùng chung luật hiện trạm.
- `Assets/VuonTra/Game/Rabbit25D/RabbitActor.cs`, `Assets/VuonTra/Art/Rabbit25D/Cream/RabbitCream.prefab`, `Assets/VuonTra/Editor/Rabbit25D/RabbitRigBuilder.cs`: tham chiếu bóng, bounds và trạng thái work.
- `Assets/VuonTra/Art/Final/`: bộ PNG từ `docs/anh/po-146/`, giữ bản gốc trong docs; bản trong Final đã thử khử thêm viền xám xanh.
- `Assets/VuonTra/Content/Stations/gio-la.asset`, `tra-sua.asset`: seeder nối hình Final.
- `Assets/Tests/EditMode/GardenStandsTests.cs`, `Garden25DStandTests.cs`, `Garden25DScreenTests.cs`.
- `Assets/VuonTra/Editor/PreviewShot.Garden25D.cs`, `PreviewShot.Stands25D.cs`: ảnh trước/sau.
- `docs/architecture/adr-0007-billboard-dung-dung-vuon-2-5d.md`: đề xuất chưa duyệt, không thay ADR Accepted.

*Còn chưa làm:* World Space Canvas trong `GardenScreen.WorldLabels.cs`, prefab cho từng trạm, đồ/cây/đèn, trạng thái trạm và particles.

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

**Status**: [ ] Chưa đạt gate. Có ảnh `146-truoc.png`, `146-sau.png`, `146-truoc-gan.png`, `146-sau-gan.png`, tấm `146-truoc-sau.png`, hai crop `146-sau-200.png`/`146-sau-gan-200.png`, và `146-sau-xam-25.png`. Visual review lần 3: REJECTED (còn lỗi phía art và một câu hỏi import); xem tiến độ bên dưới. `VERDICT.md` APPROVED trong thư mục chỉ chứng nhận bộ PNG đầu vào của design, không chứng nhận cảnh dựng lần này.

---

## Dependencies

- Depends on: production/epics/vuon-2-5d/story-145-dat-va-camera.md
- Unlocks: story-147

## Tiến độ 26/11 chiều — bản nháp lát 1, chưa nghiệm thu

**Trạng thái: In Progress. Không commit/push/GitHub edit.** `Features.Garden25DByDefault = false` giữ nguyên. Worktree `VuonTraNam-146`, nhánh `agent-146`; đã fetch và fast-forward `origin/main` từ `2e5276b` lên `f429226` trước lượt full verify, rồi lên `4460292` (chỉ `production/sprint-status.yaml`, lấy bản của main) trong lúc verify ở bước test.

**Workflow:** đã gọi `story-readiness` và helper lint; ADR-0005/0006 Accepted, story-145 tồn tại (In Review), thiếu TR registry/control manifest như đã ghi trong story. Gap còn: GDD chỉ dẫn tên tài liệu chưa dẫn luật cụ thể; engine refresh chưa thực hiện. Đề xuất ADR-0007 còn Proposed nên không được xem là quyết định đã duyệt. `/dev-story` bị tool từ chối do `disable-model-invocation`: cần người dùng chạy slash command; không coi quy trình đó đã hoàn tất. Tests Core được viết sau code trong lượt này, không có bằng chứng TDD đỏ trước triển khai.

**Đã có trong bản nháp:**
- Toàn bộ station data có stand runtime đặt trên `GardenGrid`, pivot chân, cao theo bảng tỉ lệ; quầy trà sữa thuộc nhóm quầy, cao hai lần thỏ. Chưa có prefab riêng từng trạm nên AC-1 chưa đạt toàn bộ.
- `PhysicsRaycaster` trúng `BoxCollider` trạm; `GardenStand` chuyển con trỏ cho `GardenPanZoom`, một nơi phân biệt kéo/chạm; chạm mở card trạm, chạm đất đóng card. Test kiểm trạm/đất, kéo bắt đầu trên trạm, không cấp phát khi refresh không đổi.
- (Lát 1, đã thay ở lát 2) Lát 1 dựng Cream rig cho mọi thỏ ở ba trạm mẫu; design không duyệt việc thay giống, nên lát 2 giữ hình đúng giống và chỉ dùng rig cho giống `riggedKind` (đang để trống, xem lát 2). Chưa chứng minh vòng chạy bằng GIF, chưa nối SpriteResolver trạng thái đạo cụ hoặc Shuriken.
- Bóng elip riêng nằm ở y = 0,01, màu Night. Bóng cũ trong rig tắt để tránh thứ tự vẽ nhóm của rig phủ bóng lên đạo cụ. Chưa có Lit alpha clip nhận sáng/đổ bóng thật: ADR-0007 chỉ là đề xuất cho cách tạm, cần PO/design duyệt hoặc sửa theo yêu cầu gốc.
- Bộ art đã chép vào Final, `prop_am-tra.png` thay bản có hơi nước vẽ sẵn; seeder cập nhật trà sữa và hái chè. Các texture hạt đã import nhưng chưa nối particle system.

**Test:**
- Core `GardenStandsTests`: 8/8 pass; suite Core đầy đủ chạy độc lập sau thay đổi cuối: 1003/1003 pass (warning CS0162 cũ ở `FishingTests.cs:112`).
- Unity targeted `Garden25DStandTests`: 14/14 pass ở lần cuối, gồm regression ẩn/hiện trạm phải đặt lại Work và test bóng tiếp xúc nằm giữa dưới chân (trạm và thỏ, lệch ≤ 0,05 m).
- Fast verify cũ: rules/seed/compile xanh, 1867/1867 tests (Slow skipped), nhưng RESULT RED ở bước save. Kết quả gate này **invalidated bởi sự cố gate**, không dùng để nghiệm thu save. `pgrep -fl VuonTraTho.app` lúc kiểm không thấy game; log gate cũ ghi PRIO 4 khôi phục save lúc 14:00:03, PRIO 5 lúc 14:03:00. Coordinator thông báo gate đã sửa để không bao giờ restore/write `save.json`; run mới phải báo riêng. Không quy lỗi cho game đang mở, không đụng save thật.
- Full verify (gate mới, không còn ghi save) trên `f429226`→`4460292`, trước khi chỉnh bóng: **RESULT GREEN** — rules ok, seed ok, preview ok (485 khung, khung vi đều từ bảng vi), compile ok, tests 1899/1899, save ok (`save.json` không đổi). Sau đó merge `001f6db` (chỉ file story-145) và chỉnh bóng.
- Full verify lần hai trên `001f6db` + bóng giữa chân: **RESULT GREEN** — rules ok, seed ok, preview ok (485 khung), compile ok, tests 1900/1900, save ok (`save.json` không đổi).
- `python3 -I tools/conventions.py`: ok, 1206 vi phạm cũ, không tăng baseline.

**Code review:** CHANGES REQUIRED ở mức story. Engine reviewer tìm thấy Animator mất Work khi stand tắt/bật và bóng rig đồng phẳng với đất; đã sửa reset trạng thái khi ẩn/hiện và thay bóng rig bằng bóng riêng phía dưới nhóm. Còn thiếu bằng chứng chạy clip thật; shader/prefab còn lệch yêu cầu gốc, visual gate chưa đạt. Collider hiện đúng cho camera yaw = 0; không hứa hỗ trợ camera xoay. Engine reviewer đã đọc lại delta bóng/reset cache cuối: không thấy blocker code mới, confidence medium-high; không chạy Unity độc lập, không xác nhận runtime Animator/playback và không duyệt story.

**Visual gate lần 3 (sau khi sửa phía code):** REJECTED. Đã sửa: bóng tiếp xúc nằm giữa dưới chân (`shadowFall` 0,04/−0,02, trước là 0,08/−0,14 làm bóng rời chân 14 px), màu bóng Night alpha 0,7; viền xám xanh ngoài mái quầy đã khử trong bản `Art/Final` (bản gốc ở `docs/anh/po-146/` giữ nguyên). Lần 3 chấm pass mục 2 (đứng chân, thứ tự vẽ), 6 (tỉ lệ, bóng), 7 (UI); fail còn lại: [art] đường nối cổ của rig Cream và nét viền thỏ cụp dày hơn; [art] mái quầy còn lốm đốm ngoài nét và mảng xanh trong khe tam giác dưới mái phải (nằm sẵn trong `prop_tra-sua.png`); [art cũ] nồi hồng có bọt vẽ sẵn; [render, cần quyết] ấm và bếp trông vỡ hạt vì texture 452 px thu còn ~140 px không có mipmap — `FinalArt.ImportFolder` ép tắt mipmap cho mọi hình Final, bật cho `prop_*` sẽ đổi cả bản 2D đang phát hành nên chưa làm. Ảnh xám: thỏ là vùng sáng nhất (cả ba lần).

**Visual gate lần 2:** REJECTED. Ảnh xám: thỏ là vùng sáng nhất trong thế giới. Các điểm reviewer yêu cầu xử lý: đường nối đầu/thân rig, khoảng hở nhìn thấy giữa chân và bóng, viền xanh/xám còn quanh mái quầy, độ nét/viền không đồng nhất với hình 2D placeholder; nồi hồng cũ vẫn có bọt vẽ vào sprite. Đây là finding thị giác, không tự nhận tất cả là lỗi cấu trúc rig; cần design xem ảnh thực. Không đổi VERDICT APPROVED của bộ PNG nguồn thành duyệt cảnh.

**Lát 2 (26/11 tối, theo design và PO; chưa chấm gate):**
- ADR-0007 ghi design duyệt ("ADR-0007: DUYỆT phía design, đúng art bible §5 §8", 26/11), đính chính: duyệt chỉ `SpriteRenderer`, pivot chân, hướng cố định, `SortingGroup`, collider chuyển sự kiện; **không** duyệt thay giống bằng Cream. Chủ project chưa trả lời. Duyệt kiến trúc tách riêng visual gate; commit chỉ khi visual gate APPROVED và full verify GREEN trên cây cuối.
- Giữ đúng giống: mọi thỏ hiện hình giống của mình; ánh xạ giống → rig là dữ liệu (`GardenStandSettings.rigs`, một dòng mỗi giống); rig chỉ chạy cho giống có dòng. **Khoảng trống:** chưa có ánh xạ chính thức rig Cream → `RabbitKind.Id` (rig tai dựng, `tho-cup-kem` tai cụp, `tho-trang` không có căn cứ), nên bảng ghi Cream → `tho-bong` (Thỏ Bông): mặc định của PO (luật 22/11) cộng xác nhận hình của design 26/11 (cùng tai dựng, má hồng, mắt chấm, lông kem; lông rig ấm hơn #FBE7CB/#FEF4DA, thân tròn hơn, chấp nhận được ở cỡ billboard; design đang chỉnh màu lông ở `sua-146-asset`), **chờ chủ project trả lời**. Không bao giờ `tho-trang`/`tho-cup-kem`. Các giống khác giữ hình, không có vòng 4 nhịp (sau này lớp lông theo giống qua `SpriteLibrary`; giống tai cụp cần rig thứ hai, ngoài #146). Test dùng bản sao settings: giống không có dòng giữ hình, giống có dòng chạy rig, đổi qua lại không sinh rig thứ hai; test bảng giao hàng kiểm `tho-bong` có rig, giống khác không.
- Bóng tạm đổi sang `shadow_elip.png` (ASSET-012 #147, Ink, alpha tâm 0,3): rộng 0,9 × đáy hình, sâu 0,55 × rộng trên mặt đất (≈ 0,35 trên màn ở góc 40°), lệch sau-phải 0,04/0,03 m, tâm ở chân thật.
- Đế phẳng dưới mỗi trạm (pad đất / sàn gỗ / vòng đá / hoa của bản 2D, màu `FloorTint`), thứ tự −20 dưới bóng −10; đế rộng 1,5 × đáy hình (tối thiểu 1,4 m).
- Luật import riêng cho 2.5D (`Editor/BillboardArt.cs`, gọi từ `FinalArt.Import`): `Assets/VuonTra/Art/Garden25D/Stand` (pivot ở hàng thấp nhất có alpha ≥ 0,5) và `/Flat` (pivot giữa), mipmap bật, Trilinear, không nén; `Art/Final` của bản 2D giữ nguyên. Hình 2.5D là bản sao: ba trạm mẫu, đế, bóng, đồ trang trí, cột đèn `decor_lantern` của #147, đá lối.
- Đồ trang trí quanh mỗi trạm đã mở (bồn hoa, nấm, bụi, đá; vai `Dressing` 0,3–0,7 thỏ, lấy từ cỡ bản 2D: đá 0,3, nấm 0,4, bồn hoa 0,63), lật trái/phải theo trạm chẵn/lẻ; không nhận chạm.
- Lối đá nối các cặp trạm của `GardenStands.Paths` (dời từ `GardenScreen.Paths`, bản 2D đọc lại từ Core), một cột đèn 1,4 thỏ cạnh giữa mỗi lối, phía xa ấm trà. Chưa có mảnh lối #129: dùng đá tròn như bản 2D.
- Nền cỏ tối quanh đảo (phương án A, coordinator chọn 26/11, design duyệt): mặt phẳng dưới mép đất 0,81 m, rộng ra 60 m quanh đảo, màu phẳng tạm `#31605C` của design, không sương, có collider chuyển kéo/chạm. Khi đảo lớn ra, mesh đảo và nền được đổ lại thay vì `Destroy` (fast verify trước báo lỗi "Destroy may not be called from edit mode" ở `GardenApron.Build`/`GardenGround.Build` qua `Grow`; đã sửa và thêm assert giữ nguyên mesh, collider lớn theo đảo). Lý do chọn A thay C: ở 35 m, FOV 25°, nghiêng 40° khung nhìn phủ ~26 m mặt đất trước-sau, đảo lưu thử sâu ~10–13 m; muốn đảo lấp khung phải kéo camera về ~15 m (thỏ ~250 px) hoặc đổi góc, tức sửa ADR-0006. Đây là chỗ giữ cho phần #145 còn lại: **xong phần đất, chưa có hình** đảo/vùng chưa mở, hàng rào thấp, biển gỗ (chờ design).
- Ghi chú phạm vi của design: độ đều nét còn lại ở các trạm placeholder vector cũ thuộc #131, không thuộc #146; nhưng hình placeholder cũ **không tự động được miễn** khỏi gate. Không tự sửa verdict của gate.
- Nút "Bản đồ": câu "97 px ≈ 29 pt" trong verdict là đo trên PNG, **chưa kiểm chứng**, không phải vi phạm đã xác nhận. Cần đo vùng chạm `RectTransform` thật qua `CanvasScaler` và safe area thiết bị so với 44 pt iOS / 48 dp Android. #146 không đổi camera/UI vì một quy đổi chưa kiểm.
- Asset sửa ở `sua-146-asset` **chưa sẵn**: chờ design bàn giao (cổ Cream phải qua clip chuyển động; viền quầy phải sạch cả khe tam giác dưới mái; nồi hồng do design vẽ lại). Chưa chép gì từ đó.

**Lát 2b (26/11 đêm):**
- Vòng làm việc 4 nhịp đã quay được ở ba trạm mẫu với Thỏ Bông (rig Cream): `PreviewShot.RenderLoops25D` lấy mẫu clip `Cream_TeaBrewing`, `Cream_TeaPicking`, `Cream_MilkTea` (3,28 s, 40 khung ở 12 fps) trên chính cảnh 2.5D; GIF `docs/anh/po-146/146-am-tra-4-nhip.gif`, `146-gio-la-4-nhip.gif`, `146-tra-sua-4-nhip.gif`, tấm khung nhịp `146-4-nhip.png`. Còn thiếu cho AC-3: trạng thái đồ vật trạm qua `SpriteResolver` (nắp ấm, giỏ vơi/nửa/đầy, luống đã hái), hạt Shuriken, món ở `ProductSpawn`; GIF chưa qua gate.
- Đồ trang trí, cột đèn và đá lối tránh đế của trạm khác (`keepClear` 1,1 m): ảnh trước đó có cột đèn đứng giữa đế hái chè và bụi cây đè lên đế bên cạnh.

**Lát 2c (26/11 khuya):**
- Asset của design (`origin/sua-146-asset` c487bc2, VERDICT APPROVED ở `docs/anh/qa/sua-146-asset`) đã lấy vào cây làm việc bằng `git checkout` từng tệp, **không tạo commit merge** (luật: chưa commit trước gate): `CreamFront.psb`, `CreamSide.psb` (bỏ nét mực ở cằm, lông #FEF4DA, giữ hộp lớp), `prop_tra-sua.png` (khe tam giác dưới mái trong suốt, hết viền), `prop_am-tra_nap-mo.png`, `decor_lantern.png`, `vfx_giot-sua/tra`, `vfx_lap-lanh`, nồi hồng không bọt `docs/anh/po-146/prop_noi-mut.png`. Đã chép lại bản 2.5D vào `Art/Garden25D/Stand` (và bản 2D vào `Art/Final` cho quầy, ấm nắp mở, ba hạt). Nồi hồng không bọt chỉ vào `Art/Garden25D/Stand` vì trạm `noi-mut` đứng trong vườn 2.5D; `Art/Placeholder/prop_noi-mut.png` của bản 2D giữ nguyên bọt tới #136. Sao lưu `.meta` của PSB, prefab và controller trước lần Unity đầu để trả lại nếu Unity sinh lại GUID xương.
- Code review lần 2 (engine reviewer): sửa đế/bóng nằm trong `SortingGroup` của trạm có thể vẽ đè đồ đứng ngay sau nó (đưa ra ngoài nhóm qua `GardenStand.Flat`); test 0 byte có đối chứng dương; rig khớp theo prefab thay vì tên; bỏ closure khi lọc đá; mesh đảo/nền giải phóng khi huỷ; chạm nền tối vẫn gửi `GardenGround.Touched` để thẻ trạm/sheet đóng như chạm cỏ.

**Phạm vi mang sang từ #145 (QA D đóng #145 lúc 14:59, main `4460292`; chưa làm, không tính là đạt):**
- Hình đảo và vùng chưa mở: cỏ tối một bậc (đã có nền phẳng tạm, xem lát 2), hàng rào thấp, biển gỗ. Chờ design giao hình rồi mới dựng hình.
- Test ảnh chụp qua camera đảo (QA khuyến nghị, advisory).

**Tiếp theo:**
1. Người dùng chạy `/dev-story` trên story này; chốt/loại ADR-0007 (SpriteRenderer, bóng tạm, Cream thay giống) trước khi tiếp tục hướng này.
2. Chờ design (asset): đường nối cổ rig Cream (prefab/PSB mới), xuất lại po-146/147/148 có màu mép tràn vào điểm trong suốt (nửa còn lại của viền mái), nồi hồng vẽ lại không bọt (bọt thành Shuriken). Phía code đã làm: bóng ở chân, mipmap/Trilinear cho billboard 2.5D. Chạy lại gate khi asset của design về.
3. Prefab, Lit alpha clip, đồ/cây/đèn/cối/bể cá và bảng tỉ lệ đầy đủ cho AC-1.
4. World Space Canvas ba bậc, ≤ 4 nhãn, ≥ 22 đơn vị; kiểm zoom, vi/en và Chữ to.
5. Nối trạng thái đạo cụ SpriteResolver, Shuriken, ProductSpawn và vòng 4 nhịp; GIF cả ba trạm, chấm lại visual gate.
6. Full verify xanh, code-review lại trên diff cuối, rồi mới commit per-path. Không commit scene/LocalizationSettings/Addressables phát sinh bởi seed.

# Interaction Pattern Library: Vườn Trà Thỏ Trăng

> **Status**: Draft (session design, 26/11/2026; ưu tiên các mẫu story-145 cần; chưa qua `/ux-review`)
> **Template**: `.claude/docs/templates/interaction-pattern-library.md`
> **Nguồn**: [`design/ux/hud.md`](hud.md); [`docs/de-xuat-2-5d.md`](../../docs/de-xuat-2-5d.md) §3; code hiện tại `GardenScreen.cs` (zoom, `ClampView`), `UiKit.cs` (`MinTouch`, nút chunky), `Ease.cs` (`Motion`, `Tweens`)

## Overview

Một chạm là một ý. Mọi thao tác thế giới là kéo, chụm hoặc chạm; không có giữ lâu ẩn, không có cử chỉ phải học. Mọi phản hồi theo bảng ở `hud.md` §8 và tắt khi Giảm chuyển động.

### How to Use This Library
Story mới dùng lại mẫu ở đây và ghi tên mẫu trong tiêu chí; mẫu mới thêm vào cuối "Game-Specific UI Patterns" kèm thông số.

## Pattern Catalog

### Pattern Catalog Index

| Mẫu | Dùng ở | Story |
|---|---|---|
| Kéo vườn | thế giới | 145 |
| Chụm và chạm đúp zoom | thế giới | 145 |
| Chạm vật trong thế giới | trạm, thỏ, đồ, biển | 145, 146 |
| Kéo thả bố trí | chế độ Bố trí | 146 |
| Bản đồ | màn bản đồ #91 | 145 |
| Nhãn ba bậc theo zoom | trạm | 146 (#135 đã có ở 2D) |
| Nút chunky, Sheet, Tab bar, Chip, Bay về HUD, Xác nhận | giao diện | có sẵn |

## Patterns

### Standard Control Patterns

#### Button (Primary) — nút chunky
- Thẻ có gờ (`UiKit.Chunky`), Carrot với chữ OnCarrot; vùng chạm ≥ 88 (`UiKit.MinTouch`).
- Nhấn: co 0,95 trong 80–120 ms; thả: nảy 1,05 → 1 trong 180–250 ms `OutBack`; tiếng click lệch cao độ ±5 %.
- Tắt: nền `Off`, chữ vẫn đạt AA (#121), không nhận chạm.

#### Modal Dialog — Sheet
- Trượt lên từ thanh tab 300 ms ease-out, ra 200 ms ease-in; scrim `Palette.Scrim` sau sheet; nút × ≥ 88 góc phải; chạm scrim để đóng.

#### Tab Bar
- 5 tab; tab mở: biểu tượng 84 → 108, đĩa nâng, chữ đậm Ink; chấm cam đếm việc. Chạm tab đang mở thì đóng sheet.

#### Toast / Notification — Bay về HUD
- Đồng/món bay 550 ms từ điểm phát tới thẻ đích (`Feel.Fly`), thẻ đích nảy; tối đa 10 vật bay một lượt.

#### Confirmation Dialog
- Chỉ cho hành động tốn tiền thật hoặc không hoàn lại; nút chính là hành động an toàn hơn ở bên phải.

### Game-Specific UI Patterns

#### Kéo vườn
- **Cử chỉ:** một ngón kéo trên vùng thế giới (không trên HUD, sheet, chip).
- **2.5D:** kéo dịch điểm nhìn trên mặt phẳng đất; camera giữ góc nghiêng ~40°, không xoay.
- **Quán tính:** thả tay trôi tiếp, giảm dần ~0,35 s; dừng mềm ở biên.
- **Biên:** `ClampView` giữ đảo trong khung; chừa vùng HUD trên + 24 và thanh tab + 24 (#120), cho hàng trạm ngoài cùng không nằm dưới HUD.
- **Giảm chuyển động:** không quán tính, dừng ngay khi thả.
- **Không:** kéo không bao giờ kích hoạt chạm; ngưỡng phân biệt kéo/chạm 12 đơn vị canvas.

#### Chụm và chạm đúp zoom
- **Mức:** Xa (`ZoomFar`), Mặc định 0,83, Gần 1,5 (`GardenScreen.cs:459`). Ngưỡng chi tiết `DetailZoom` = 1,165 (giữa Mặc định và Gần).
- **Chụm:** zoom liên tục quanh điểm giữa hai ngón; thả thì giữ nguyên mức đang có (không bắt về mức cố định).
- **Chạm đúp:** chuyển Mặc định ↔ Gần quanh điểm chạm, `Motion.Travel` 0,3 s `InOutCubic`.
- **Nút máy ảnh (HUD A):** cho người không biết chụm; vòng Xa → Mặc định → Gần.
- **2.5D:** zoom là đổi khoảng cách camera (hoặc FOV hẹp), giữ góc nghiêng; nhãn đổi bậc khi vượt `DetailZoom` (`ShowZoomDetail`, không chạy mỗi khung).

#### Chạm vật trong thế giới
- **Đích:** trạm, thỏ, đồ trang trí, ao/hũ/cối/giỏ trò nhỏ, biển mở đất, quầy.
- **2.5D:** `PhysicsRaycaster` trên camera + collider và `IPointerClickHandler` trên prefab; không tự raycast trong `Update()`.
- **Vùng chạm:** collider phủ hết hình billboard, tối thiểu tương đương 88 đơn vị canvas ở zoom mặc định; khi chồng nhau, vật gần camera thắng.
- **Phản hồi:** vật nảy 0,95 → 1,05 → 1 trong 180 ms, tiếng click; rồi mở sheet/trò của vật đó.
- **Thỏ:** chạm mở thẻ thỏ; vuốt (kéo ngắn qua lại trên thỏ) là vuốt ve (`UiKit.Strokable`), không mở thẻ.

#### Kéo thả bố trí (chế độ Bố trí)
- Giữ 0,25 s trên trạm hoặc đồ để nhấc, kéo theo ô lưới, ô hợp lệ sáng Moss, ô không hợp lệ viền Carrot và lắc khi thả; thả là đặt, có hoàn tác.
- Chỉ trong chế độ Bố trí; ngoài chế độ này không có giữ lâu.

#### Bản đồ
- Màn bản đồ là ảnh của cùng scene nhìn thẳng xuống (camera trực giao → `RenderTexture`), không phải bản vẽ riêng.
- Kéo và chụm như vườn; chạm một vùng trên bản đồ thì đóng bản đồ và đưa camera vườn tới vùng đó (`Motion.Travel`).
- Vùng chưa mở trên bản đồ: tối hơn một bậc, không sương (art bible §5).

#### Nhãn ba bậc theo zoom
- Bậc 1 (việc cần làm ngay) luôn hiện; bậc 2 (trò nhỏ còn lượt) là chấm số tối, câu đầy đủ ở zoom ≥ `DetailZoom`; bậc 3 (Cấp N, giờ mở) chỉ ở zoom gần. Đổi bậc mờ dần `Motion.Quick` (`Tweens.Fade`), tức thì dưới Giảm chuyển động. ≤ 4 nhãn chữ ở zoom mặc định.

## Ghi chú

- Mẫu Input Field, Dropdown, Slider, Toggle, Tooltip, Scroll Container dùng như hiện có trong `UiKit`; chưa viết riêng vì 2.5D không đổi chúng.
- Inventory Slot, Ability Icon, Damage Number, Dialogue Box: không dùng trong game này.

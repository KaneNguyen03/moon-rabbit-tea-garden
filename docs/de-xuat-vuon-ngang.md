# Vườn là bản đồ 2D (#91a, #91b, #91c)

Ngày 24/11/2026. Thay cho dải ngang ngày 23/11 (phần "Lịch sử" ở cuối). Chủ project trả lại #91: vườn phải kéo được cả dọc lẫn ngang, game vẫn khung dọc 1080 × 1920. Tài liệu tả bản đồ như hiện có: lõi (`Core/`: lưới, mở rộng bốn phía, luật bố trí, save cũ, nhịp, #91a) và màn hình (`GardenScreen`: camera, biên bốn phía, sương, rừng viền, biển, parallax, nút về tiệm, viên mép, #91b và #91c).

## Lưới

- Vẫn là lưới lục giác đỉnh nhọn của #52: toạ độ trục (q, r), cột kép `c = 2q + r`, hàng `r` tăng xuống dưới. Một ô cách ô kế trong hàng 150 đơn vị, hai hàng cách nhau 200 (`PitchX`, `PitchY` không đổi).
- Ấm trà ở ô (0,0), tiệm trà ngay trên (1,-3). Phần giữa mở từ đầu y như dải ngang: 7 hàng (r −3…3), |c| ≤ 7, 53 ô. Mọi thứ trong phần giữa (ấm trà, giỏ lá, nồi mứt, cối hoa, cối giã, tiệm, quầy, hai gốc cây, tám chỗ trang trí, các ô giữ chỗ của hũ đom đóm, giỏ hứng lá, bảng cối, ao) giữ nguyên ô.
- Sau mỗi đoạn, bãi cỏ là một hình chữ nhật theo hàng và cột kép (`GardenGrid.Reach(đoạn, hướng)`), nên một đoạn lên hoặc xuống chạy hết bề ngang đã mở.

## Thứ tự mở rộng

Mỗi đoạn giữ đúng cấp, số thỏ, số loài và giá (¼ giá hai trạm nó mở đất) của đoạn cùng số trên dải ngang. Bảng `Expansion.Table` không đổi một dòng; hướng và kích thước nằm ở `GardenGrid`.

| Đoạn | Hướng | Dải mở | Ô mở | Tổng ô | Trạm có đất | Cấp / thỏ / loài |
|---|---|---|---|---|---|---|
| giữa | — | 7 hàng, \|c\| ≤ 7 | 53 | 53 | ấm trà, giỏ lá, nồi mứt, cối hoa | — |
| 1 | phải (phía lò) | c 8…13, 3 ô/hàng | 21 | 74 | lò bánh hạt dẻ, tổ mật | theo bảng |
| 2 | trái (phía ao) | c −8…−13 | 21 | 95 | trà sữa trăng, bình ủ sao | theo bảng |
| 3 | phải | c 14…25, 6 ô/hàng | 42 | 137 | giàn nho, tiệm bánh trung thu | theo bảng |
| 4 | trái | c −14…−25 | 42 | 179 | suối hoa, đài ngắm sao | theo bảng |
| 5 | lên (trạm #67) | r −4…−11, hết bề ngang | 204 | 383 | vườn sen, lò kẹo | theo bảng |
| 6 | xuống (trạm #67) | r 4…11, hết bề ngang | 204 | 587 | ruộng nếp, thuyền trà đêm | theo bảng |
| 7, 8, 9 | phải, trái, lên | quá 16 trạm | 69, 69, 126 | | trạm ngoài catalog | +5 cấp mỗi đoạn |

Mở hết (16 trạm): bãi cỏ 51 cột kép × 23 hàng = 3 900 × 4 600 đơn vị. Ở zoom mặc định 0,83 một màn hình thấy 1 301 × 2 313 đơn vị, nên bãi cỏ là 3 màn hình ngang × 2 màn hình dọc, cộng rừng viền 450/400 mỗi bên là khoảng 3,7 × 2,3. Đoạn 1–4 trùng đất dải ngang (đoạn 3 và 4 gộp đoạn 3+5 và 4+6 của dải), nên trạm 0–11 đứng y chỗ cũ trừ tiệm bánh trung thu.

## Chỗ đứng mặc định (mở hết)

`0…F` là trạm 0…15 theo catalog, `r` chỗ nghỉ, `d` chỗ trang trí, `M` cối giã, `S` tiệm trà, `C` quầy khách, `#` ô giữ chỗ, chữ số là ô trống của đoạn đó, `.` ô trống phần giữa, `~` ngoài bản đồ.

```
-12 ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~
-11  5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5
 -7  5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5
 -6 ~ 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 E 5 5 5 5 ~
 -5  5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 D 5 5
 -4 ~ 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 C 5 5 5 ~
 -3  4 4 4 4 4 4 2 2 2 # # # S d C . d 1 1 1 3 3 3 3 3 3
 -2 ~ 4 4 4 4 r 4 2 2 # # # # # # d . 6 1 1 8 3 9 3 3 3 ~
 -1  4 4 4 4 4 4 2 2 2 # # # # M # # # 1 1 1 3 3 3 3 3 3
  0 ~ 4 4 4 4 B 4 2 r # # # . 0 . 1 . 1 1 4 3 3 3 3 3 3 ~
  1  4 4 4 4 4 4 2 2 2 r r . . . . . d r 1 1 3 3 3 3 3 3
  2 ~ 4 4 4 4 4 A 2 2 7 . . 3 . . 2 . 1 5 1 3 r 3 3 3 3 ~
  3  4 4 4 4 4 4 2 2 2 d . d . d . d . 1 1 1 3 3 3 3 3 3
  4 ~ 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 ~
  5  6 6 6 6 6 6 6 6 F 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
 11  6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
 12 ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~ ~
```
(Hàng −10…−8 và 6…10 giống hàng kề, lược đi.)

- Trạm 0–8, 10, 11, sáu chỗ nghỉ, tám chỗ trang trí, cối giã, tiệm, quầy, ô giữ chỗ: y như dải ngang.
- Tiệm bánh trung thu (9) lên hai hàng: (9,0) → (10,−2), cạnh giàn nho, để vườn sen trên dải lên đứng gần nó.
- Vườn sen (12) (11,−4), lò kẹo (13) (13,−5), ruộng nếp (14) (11,−6): dải lên, phía lò. Ruộng nếp đứng trên đất đoạn 5 cạnh lò kẹo nó tiếp sức, như trước.
- Thuyền trà đêm (15) (−7,5): dải xuống, phía ao.
- Luật cũ còn nguyên: trạm tiếp sức đứng trong 520 đơn vị của trạm nó tiếp sức (#67); hai trạm cách nhau ít nhất 2 ô (#88); không ô nào hai thứ; không gì đứng trên ô giữ chỗ.
- Quá 16 trạm: hai trạm mỗi đoạn đứng giữa dải của đoạn đó; quá đoạn 9 thì đứng ngoài bản đồ và `Settle` đặt vào ô trống gần nhất.

## Luật bố trí

- `Move`, `Swap`, `Store`, `TakeOut`, `Layout.Drop` không đổi luật: chỉ lên ô cỏ đã mở, không lên ô giữ chỗ, trạm cách trạm ≥ 2 ô (#88). Dải lên/xuống còn sương thì thả vào là trả về.
- `Settle`: thứ gì không có ô thì về ô mặc định; ô đó bị chiếm thì trạm lấy ô trống gần nhất vẫn cách các trạm khác ≥ 2 ô (mới), chỗ nghỉ và trang trí lấy ô trống gần nhất.
- Trăng non (#85, `LeaveClosedLand`): đất về phần giữa; thứ đứng trên dải lên/xuống hay hai bên về ô mặc định, ấm trà về nhà.

## Save cũ (vẫn version 14)

- Khối mới: `"grid":{"map":N,"tiles":[…],"stored":[…]}`, ô giữ nguyên khi đọc lại.
- Khối dải ngang `"segments":N` và khối lưới dọc `"radius":R`: mở N (hoặc R − 3) đoạn, không bớt đoạn nào; mọi trạm, chỗ nghỉ, trang trí về ô mặc định (hoặc ô trống gần nhất giữ khoảng cách); cái đã cất vẫn cất, cấp giữ nguyên; góc nhìn đã lưu bỏ đi (vườn đã lớn hơn), vườn mở ở ấm trà, zoom thường.

## Nhịp

Nhịp chỉ phụ thuộc số đoạn, cấp/thỏ/loài/giá từng đoạn, số ô đất và chỗ nghỉ, đều không đổi. Chạy lại 17 hạt (#83) ngoài Unity: 102 lần chạy (6 mô phỏng × 17 hạt) giống từng byte, trừ khối `grid` trong save cuối (toạ độ và tên khoá mới).

## Màn hình (#91b, #91c)

- **Thế giới:** `WorldW` 4 800 × `WorldH` 5 400 đơn vị, tiệm trà ở giữa. Ở zoom thường 0,83 màn hình thấy 1 301 × 2 313 đơn vị; nhìn xa (`ZoomFar`, 2 970 đơn vị theo chiều cao) và nhìn gần 1,5 vẫn chụm hai ngón hoặc cuộn chuột.
- **Kéo:** `ScrollRect` bật cả ngang lẫn dọc khi thế giới lớn hơn màn hình (ở zoom thường là cả hai), có quán tính; kéo chéo được.
- **Biên bốn phía (`ClampView`):** ngang là `OpenSpan` = mép bãi cỏ đã mở ± 360 (`ViewMargin`), nới thêm cho vừa nhãn "Mở rộng vườn" của ô đất kế tiếp (#91c, để nhãn không bị mép màn hình cắt); dọc là `OpenRows` = mép trên/dưới bãi cỏ ± 600 (`ViewMarginY`), trong thế giới. Kéo quá biên thì thả ra vườn tự trượt về (`KeepInsideOpenLawn`, đàn hồi cả hai trục; Giảm chuyển động thì về ngay). Khi mới có phần giữa, biên dọc vẫn rộng hơn màn hình nên kéo dọc được ngay từ đầu; mỗi đoạn lên/xuống nới biên thêm 8 hàng về phía đó.
- **Thứ tự mở:** phải, trái, phải, trái, lên, xuống (bảng ở trên). Đoạn lên/xuống mở hết bề ngang đã mở.
- **Sương:** mỗi đoạn chưa mở có sương phủ đúng dải của nó; dải lên/xuống còn đóng có thêm một bờ sương dày ("Edge") nằm dọc mép bãi cỏ, quay 90°/270° để phía bãi cỏ trong. Sương tan dần trong `MistClear` khi đoạn mở.
- **Rừng viền:** hai bên là viền rừng lùi dần theo đất mở; trên và dưới, một hàng cây ("Wood") mọc sát mép bãi cỏ khi dải lên/xuống đã mở (dải còn đóng thì bờ sương là mép), luôn cách mọi trạm đã mở ít nhất 200.
- **Biển "Mở rộng vườn":** đoạn trái/phải thì biển đứng ở mép bên đó; đoạn 5/6 thì biển đứng giữa mép trên/dưới của bãi cỏ, không che bảng nào. Biển có viền mực như đạo cụ vẽ tay (#91c).
- **Parallax:** cây ở tán (`Canopy`, rừng xa ở mép thế giới) đi theo mặt đất 9/10 tốc độ (`CanopyDrift`), đo từ góc nhìn đặt trên ấm trà, cả ngang lẫn dọc (#91c thêm chiều dọc). Chỉ là độ lệch tỉ lệ với vị trí nhìn, không có chuyển động theo thời gian. Giảm chuyển động thì tán đi đúng theo mặt đất.
- **Camera:** nút "Về tiệm trà" (góc dưới phải) hiện khi ấm trà ra khỏi màn hình theo bất kỳ hướng nào, bấm thì trượt về tiệm trà; bấm dòng trạm trong tab Trạm thì vườn trượt tới trạm đó, kể cả trạm ở dải lên/xuống, dừng ở khoảng giữa tờ trạm và HUD. Viên ◀ ▶ ▲ ▼ ở mép đếm trạm ngoài màn hình đang chờ (nâng được hoặc thỏ ngủ), bấm thì bay tới trạm gần nhất; viên ▼ né sang bên khi tiệm trà đứng ngay sau nó (#91c), không đè nút về tiệm.
- **Ảnh:** `docs/anh/po-vuon-2d/91b-*` (trước) và `91c-*` (sau): 0, 4, 5, 6 đoạn; ở biển, kéo lên, kéo xuống, kéo trái, kéo phải, nhìn xa. Menu `VuonTra/Render Map Edges`.

## Lịch sử: dải ngang (23/11)

Vườn từng là cột dọc 1080 × 3770 (vòng đồng tâm #52/#53), rồi dải ngang 7 hàng rộng 3–4 màn hình (`WorldW = 4800`, `WorldH = 2200`), mở rộng từng đoạn sang phải/trái, kéo dọc chỉ còn chút đàn hồi. Chủ project trả lại ngày 23/11 tối (#91): phải kéo được cả hai chiều.

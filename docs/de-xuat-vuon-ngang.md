# Đề xuất: vườn là bản đồ 2D (lõi, #91a)

Ngày 24/11/2026. Thay cho dải ngang ngày 23/11 (phần "Lịch sử" ở cuối). Chủ project trả lại #91: vườn phải kéo được cả dọc lẫn ngang, game vẫn khung dọc. Tài liệu này chỉ tả phần lõi (`Core/`): lưới, mở rộng bốn phía, luật bố trí, save cũ, nhịp. Phần màn hình (camera, biên, sương, rừng viền, parallax, nút về tiệm) là #91b, session UI làm.

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

## Phần màn hình (#91b, chưa làm)

- `GardenScreen` mới chỉ đọc toạ độ mới: `WorldH` 2200 → 5400, `LawnEdgeX`/`SegmentSpan` đọc `GardenGrid.Reach`, sương đoạn 5/6 phủ đúng hàng của dải lên/xuống.
- Còn phải làm: camera kéo dọc có biên theo đất đã mở (hiện `ClampView` cho kéo dọc hết thế giới), biên đàn hồi bốn phía, rìa sương dày của dải lên/xuống, biển "Mở rộng rừng" cho đoạn lên/xuống (đang đứng ở mép phải), rừng viền trên/dưới ôm theo đất mở, parallax dọc, nút về tiệm trà, viên ◀▶▲▼.

## Lịch sử: dải ngang (23/11)

Vườn từng là cột dọc 1080 × 3770 (vòng đồng tâm #52/#53), rồi dải ngang 7 hàng rộng 3–4 màn hình (`WorldW = 4800`, `WorldH = 2200`), mở rộng từng đoạn sang phải/trái, kéo dọc chỉ còn chút đàn hồi. Chủ project trả lại ngày 23/11 tối (#91): phải kéo được cả hai chiều.

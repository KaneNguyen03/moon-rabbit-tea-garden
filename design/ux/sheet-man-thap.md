# Sheet trên màn hình thấp (tablet 4:3, 16:10) — #196

> **Status**: Draft (session design, 28/11/2026). Áp cho mọi bottom sheet của vườn 2D (Trạm, Bùa, Nhiệm vụ, Thỏ). Chuẩn chung: `design/ux/hud.md` (vùng A cao khoảng 150, vùng D, lề an toàn 24; chữ thân ≥ 26, chữ phụ ≥ 22; vùng chạm ≥ 88).

## 1. Vấn đề
Trên 4:3 sheet 60% chỉ hiện khoảng 1,5 dòng nhiệm vụ, kéo hết cỡ khoảng 2,5 dòng (QA #195). Nguyên nhân: dòng cao 176, phần đầu sheet (tay kéo, tiêu đề, các dòng ghi chú) chiếm khoảng 730 đơn vị, và một số chỗ còn tính theo `ScreenH = 1920`.

## 2. Hình học (đơn vị canvas, CanvasScaler khớp 0,5)
| Màn | Canvas | Sheet 60% | Sheet kéo hết | Cột | Dòng thấy trọn ở 60% | Dòng thấy trọn khi kéo hết |
|---|---|---|---|---|---|---|
| Điện thoại 9:16 | 1080×1920 | 1152 | giữ như #193 | 1 | giữ như hiện tại (dòng 176) | giữ |
| 16:10 | 1138×1821 | 1093 | 1440 | 1 | **4** | **7** |
| 4:3 | 1247×1663 | 998 | 1282 | 2 | **8** (4 hàng × 2) | **12** (6 × 2) |

- Sheet 60% = `0,6 × CanvasHeight` (không dùng `ScreenH`).
- Sheet kéo hết = `CanvasHeight − HudGainFoot (321) − SheetUnderLane (12) − safe area trên`, như #193; không bao giờ lấn làn số nổi.
- **N chốt:** ở 60% ít nhất 3 dòng, kéo hết ít nhất 5 dòng trên mọi màn có `CanvasHeight` từ 1600 trở lên. Test đếm dòng đo bằng viewport thật của ScrollRect, không đo bằng 1920.

## 3. Mật độ trên màn thấp
Bật chế độ gọn khi `CanvasHeight < 1800` (4:3, 16:10, một số điện thoại gập):
| Phần | Thường | Gọn |
|---|---|---|
| Dòng nhiệm vụ (`QuestRowView`) | cao 176 | cao 132: tên một dòng `TextTitle` ở trên, thanh tiến độ cao 20 ở dưới, nút Nhận 200×96 bên phải (vẫn ≥ 88) |
| Khoảng cách giữa dòng | như cũ | 12 |
| Dòng ghi chú (cỏ 3 lá, lá công thức) | mỗi thứ một dòng | gộp vào một chip cao 56, chữ phụ 22 |
| Tiêu đề nhóm ("Hôm nay", "Thành tựu") | dòng riêng | giữ, cao 48 |
| Phần đầu sheet (tay kéo + tiêu đề + chip ghi chú + đệm) | khoảng 730 | ≤ 420 |

**Hai cột** khi `CanvasWidth ≥ 1200` (4:3): mỗi cột rộng `(CanvasWidth − 40 − 48 − 24) / 2`, cách nhau 24. Dòng sắp theo hàng (trái rồi phải), thứ tự như `OrderQuestRows`. Lưới thỏ đã nhiều cột thì chỉ tăng số cột theo bề ngang, không đổi cỡ thẻ.

Chữ không nhỏ hơn chuẩn: tên nhiệm vụ 26 đậm, số đếm và ghi chú 22. Chữ to (`UiKit.LargeText`) tắt chế độ hai cột và giữ dòng 176.

## 4. Hành vi kéo
- Mở ở 60%. Kéo lên quá điểm giữa (60% và kéo hết) thì bật lên kéo hết; kéo xuống dưới 80% của 60% thì đóng (như hiện tại).
- Trên màn thấp, nếu nội dung dài hơn khung 60%: khi mở lần đầu, dòng thứ N+1 ló ra nửa dòng (66) để báo còn nữa, không cần mũi tên.
- Không có mức kéo thứ ba.

## 5. Phần code (B hoặc Orca)
1. `GardenScreen.cs`: `SheetHalf` dùng `CanvasHeight` (bỏ `Mathf.Min(ScreenH, …)`); kiểm mọi chỗ còn dùng `ScreenH` cho chiều cao sheet, `SafeInsets()` lấy từ `Screen.safeArea` đổi ra đơn vị canvas.
2. `QuestRowView.cs`: tham số gọn (cao 132, nút 200×96, bố cục như bảng ở mục 3).
3. Danh sách nhiệm vụ: `GridLayoutGroup` hai cột khi `CanvasWidth ≥ 1200` và không bật Chữ to; một cột trong mọi trường hợp khác.
4. Gộp hai dòng ghi chú thành một chip khi gọn.
5. Test EditMode: ở 1536×2048 và 1600×2560, đếm dòng nằm trọn trong viewport của ScrollRect ở 60% và kéo hết, so với bảng mục 2; mép trên sheet kéo hết không vượt `CanvasHeight − HudGainFoot`.

## 6. Bằng chứng
Mockup: `docs/anh/po-196/mockup-4-3-16-10.png`. Ảnh trước/sau trong game bằng `tools/shot.sh` sau phần code, qua `visual-critic-gate`.

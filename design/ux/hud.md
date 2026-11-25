# HUD Design: Vườn vườn 2.5D

> **Status**: Draft (session design, 26/11/2026, cho #148; chưa qua `/ux-review`)
> **Template**: `.claude/docs/templates/hud-design.md`
> **Nguồn**: [`design/art/art-bible.md`](../art/art-bible.md) §7; [`docs/de-xuat-2-5d.md`](../../docs/de-xuat-2-5d.md); #120 (mép khung nhìn), #121 (tương phản AA), #135 (nhãn ba bậc), #142 (phông), #143 (khung thẻ); asset `design/assets/specs/troi-hud-25d-assets.md` ASSET-018

## 1. HUD Philosophy

- Màn vườn chỉ trả lời ba câu: tiền bao nhiêu, cái gì cần chạm ngay, thỏ đang làm gì (P1 Yên, P3 Thấy tiến bộ).
- HUD màn hình là thẻ kem đặc có gờ, cố ý khác thế giới màu nước để đọc nhanh (art bible §3, §7).
- Thông tin của từng trạm sống trên trạm (World Space, ba bậc #135), không lên HUD màn hình.
- Thỏ đang làm việc phải là điểm sáng nhất; HUD không che thỏ và trạm ở zoom mặc định.

## 2. Information Architecture

### 2.1 Full Information Inventory

| Thông tin | Tần suất đổi | Người chơi cần khi nào |
|---|---|---|
| Cấp vườn (huy hiệu) | hiếm | khi muốn biết tiến độ dài |
| Cà rốt và tốc độ/giây | liên tục | luôn |
| Trà lực và "Tăng tốc!" | theo phiên | khi muốn tăng tốc |
| Máy ảnh, tìm, cài đặt | không đổi | khi cần |
| Chip sự kiện (Tuần đầu, hội chợ ×2, ưu đãi) | theo ngày | khi có thưởng chờ |
| Thanh tab 5 mục và chấm đếm | khi có việc | luôn |
| Nhãn trạm (Cấp N, thanh chu kỳ, chấm số trò nhỏ, "Xếp thỏ +") | liên tục | trên trạm, theo bậc #135 |

### 2.2 Categorization

- **Luôn hiện (screen-space):** cà rốt và tốc độ, thanh tab.
- **Hiện khi có ý nghĩa (screen-space):** trà lực và tăng tốc, chip sự kiện, chấm đếm tab.
- **Theo ngữ cảnh (World Space trên trạm):** ba bậc của #135.
- **Ẩn trong menu:** cài đặt, chữ to, giảm chuyển động, âm lượng.

## 3. Layout Zones

### 3.1 Zone Diagram

```
┌──────────────────────────────────────┐  ← dải tối ui_dai-hud (ASSET-018)
│ [Cấp] [Cà rốt +/giây] [Trà lực] [⌕⚙] │  Vùng A: HUD trên, cao ~150
│                        [chip sự kiện]│  Vùng B: chip, mép phải
│                                      │
│          VƯỜN 2.5D (đảo cỏ)          │  Vùng C: thế giới, thỏ là tâm
│        nhãn World Space ba bậc       │
│                                      │
│ [Bố trí][Trạm][Bùa][Nhiệm vụ][Thỏ]   │  Vùng D: thanh tab, cao ~200
└──────────────────────────────────────┘
```

### 3.2 Zone Specification Table

| Vùng | Neo | Kích thước (canvas 1080×1920) | Nội dung | Luật |
|---|---|---|---|---|
| A | trên, trong safe area | cao ~150 | cấp, cà rốt, trà lực, nhóm nút góc | nền thẻ kem đặc, không bán trong suốt |
| B | trên-phải, dưới A | chip cao 64 | chip sự kiện xếp dọc, tối đa 2 | chỉ hiện khi có việc |
| C | giữa | phần còn lại | cảnh 2.5D | `ClampView` chừa A + 24 và D + 24 (#120) |
| D | dưới, trong safe area | cao ~200 | thanh tab | tab đang mở có đĩa nâng, chữ đậm |

## 4. HUD Element Specifications

### 4.1 Element Overview Table

| Phần tử | Vùng | Ưu tiên | Kích thước chạm |
|---|---|---|---|
| Cà rốt và tốc độ | A | 1 | không chạm (chỉ đọc) |
| Trà lực và Tăng tốc | A | 2 | nút ≥ 88 |
| Huy hiệu cấp | A | 3 | ≥ 88 |
| Máy ảnh, tìm, cài đặt | A | 4 | mỗi nút ≥ 88 |
| Chip sự kiện | B | 3 | ≥ 88 chiều cao vùng chạm |
| Thanh tab | D | 1 | mỗi tab ≥ 88 |

### 4.2 Element Detail Blocks

- **Cà rốt:** chữ Ink đậm trên thẻ kem; số đếm tween 400–700 ms `OutCubic`, nảy 1,1 khi xong; dòng "+N/giây" chữ phụ ≥ 22.
- **Trà lực:** thanh Moss trên nền Track; "Tăng tốc!" chữ CarrotInk; khi đang tăng tốc, mép màn sáng nhẹ (đã có), không thêm lớp phủ toàn màn.
- **Nhóm nút góc:** nền thẻ kem đặc với `CardLip` (#134 đã đổi từ Paper 0,55).
- **Chip sự kiện:** thẻ kem nhỏ, biểu tượng + chữ ngắn + chấm cam đếm; tối đa 2 chip, còn lại gom vào tab Nhiệm vụ.
- **Thanh tab:** biểu tượng 84 → 108 khi mở, đĩa nâng, chữ đậm Ink; chấm đếm cà rốt.

## 5. HUD States by Gameplay Context

| Ngữ cảnh | A | B | C (nhãn trạm) | D |
|---|---|---|---|---|
| Vườn, zoom mặc định | hiện | hiện nếu có | bậc 1 + chấm số; Cấp N ẩn | hiện |
| Vườn, zoom gần (≥ DetailZoom ≈ 1,165) | hiện | hiện | đủ ba bậc, câu đầy đủ | hiện |
| Vườn, zoom xa | hiện | ẩn | ẩn mọi bảng trừ biển mở đất | hiện |
| Mở bảng (sheet) | hiện, sau scrim | ẩn | ẩn sau scrim | hiện, tab mở nâng lên |
| Chụp ảnh | ẩn | ẩn | ẩn | ẩn, chỉ còn thanh chụp |
| Khoảnh khắc thưởng | mờ sau scrim | ẩn | ẩn | mờ |
| Bản đồ #91 | hiện | ẩn | — | hiện |

## 6. Information Hierarchy

1. Thỏ đang làm việc và đèn lồng (thế giới).
2. Cà rốt (A) và việc cần làm ngay (bậc 1 trên trạm, chấm đếm tab).
3. Trà lực, chip sự kiện.
4. Thông tin trạm (chỉ ở zoom gần), cài đặt.

Kiểm bằng ảnh xám thu 25 %: thỏ sáng nhất, rồi thẻ cà rốt; không chip nào sáng hơn thỏ.

## 7. Visual Budget

- Tối đa 2 lớp phủ toàn màn bật cùng lúc (vignette, vân giấy); dải tối HUD là cục bộ.
- HUD trên (A + B) ≤ 12 % chiều cao màn ở zoom mặc định; thanh tab ≤ 11 %.
- ≤ 4 nhãn chữ trong thế giới ở zoom mặc định (#135).

## 8. Feedback & Notification Systems

| Sự kiện | Hình | Tiếng | Hạt |
|---|---|---|---|
| Thu cà rốt | đồng bay 550 ms vào thẻ cà rốt, thẻ nảy | tiếng xu, cao độ ±5 % | lấp lánh ở thẻ |
| Nâng cấp trạm | bảng cấp nảy, số đếm lên | chuông | tia sáng Shuriken |
| Việc mới ở tab | chấm cam nảy 180–250 ms `OutBack` | click nhẹ | không |
| Thiếu tiền | lắc ngang 3 lần trong 250 ms | tiếng trầm | không |

Mọi chuyển động tắt hoặc tức thì dưới Giảm chuyển động.

## 9. Platform Adaptation

- Chỉ dọc (portrait), canvas tham chiếu 1080×1920, `matchWidthOrHeight` 0,5.
- Vùng A, D bám safe area (tai thỏ, thanh home); không đặt nút trong 24 px sát mép.
- Máy yếu (iPhone 11, Snapdragon 665, #149): HUD không dùng blur, không lớp phủ mờ.

## 10. Accessibility

### 10.1 Colorblind Modes
Không dùng màu làm tín hiệu duy nhất (art bible §4): tiến độ là thanh có độ dài; "bấm được" là nút có gờ và mũi tên ▲; khoá có ổ khoá hoặc "?".

### 10.2 Text Scaling
Chữ to (`UiKit.LargeText`) giữ được bố cục A, B, D ở vi và en; chữ thân ≥ 26, chữ phụ ≥ 22.

### 10.3 Motion Sensitivity
`Motion.Reduced`: số đếm nhảy thẳng, chip không nảy, tween chuyển trạng thái tức thì.

### 10.4 Subtitles Specification
Không có lời thoại; không cần phụ đề.

### 10.5 HUD Opacity and Visibility Controls
Không thêm tuỳ chọn độ trong HUD (thẻ đặc là luật). Chế độ chụp ảnh ẩn toàn bộ HUD.

## 11. Tuning Knobs

| Núm | Giá trị hiện | Ghi chú |
|---|---|---|
| `DetailZoom` | (ZoomDefault + ZoomNear) / 2 ≈ 1,165 | ngưỡng nhãn đầy đủ (#135) |
| Dải tối HUD | alpha 0,35 → 0, cao 260 | chỉ bật khi thấy trời sau HUD |
| Số chip tối đa | 2 | còn lại vào tab Nhiệm vụ |
| Lề an toàn | 24 | quanh A, D và khi `ClampView` |

## 12. Acceptance Criteria

- Khung vườn 2.5D zoom mặc định 1080×1920 có HUD: thỏ và trạm không bị A, B, D che; ảnh xám 25 % thỏ sáng nhất; `visual-critic-gate` APPROVED.
- Mọi chữ HUD đạt AA (#121, test `AccessibilityTests` xanh); mọi nút ≥ 88.
- Chữ to và vi/en: không chữ bị cắt ở A, B, D.
- Giảm chuyển động: không tween nảy hay số đếm chạy.
- ≤ 2 lớp phủ toàn màn bật cùng lúc.

## 13. Open Questions

- Phông riêng có dấu (#142) thay phông hệ điều hành.
- Khung thẻ 9-slice kiểu sách truyện (#143) cho thẻ A, B và thanh tab.
- Trên camera phối cảnh, giữ dải tối HUD cố định hay chỉ bật khi trời nằm sau HUD.

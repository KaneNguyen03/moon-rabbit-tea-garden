# #146 — hình ba trạm mẫu (ASSET-002, 004, 005, 006, 008)

Nguồn: Antigravity (AI), image-generator, 26/11/2026, theo `design/assets/specs/tram-mau-25d-assets.md`. Cắt theo nhóm trạng thái (một khung chung, căn đáy-giữa, một hệ số thu), khử viền, xoá lỗ nền kín (quai ấm, quai giỏ).

| Tệp | ASSET | Ghi chú |
|---|---|---|
| `prop_tra-sua.png` | 002 | quầy trà sữa, một hình cố định (bỏ trạng thái hũ/chồng ly, xem dưới) |
| `prop_gio-la.png`, `prop_gio-la_hai.png` | 004 | luống chè: búp non / đã hái |
| `item_gio-la_voi.png`, `_nua.png`, `_day.png` | 004 | giỏ ba mức (dụng cụ hông thỏ) |
| `product_gio-la.png` | 005 | bó lá chè buộc lạt |
| `prop_am-tra.png`, `prop_am-tra_nap-mo.png`, `prop_am-tra_ngam.png` | 006 | ấm trên bếp than: nắp đóng (idle) / nắp mở / ngấm |
| `vfx_giot-sua.png`, `vfx_giot-tra.png`, `vfx_la-che.png`, `vfx_hoi-nuoc.png`, `vfx_lap-lanh.png` | 008 | texture hạt Shuriken |

Quyết định thiết kế:
- **Quầy trà sữa không đổi trạng thái.** Ảnh AI vẽ lại cả quầy ở mỗi ô (khác 35–44 % điểm ảnh sau khi căn), nên đổi trạng thái làm quầy giật. Thông tin "đang làm" đọc qua thỏ và ly trên khay; hũ/chồng ly vơi là chi tiết quá nhỏ để đáng giật.
- **Ấm trà:** `_ngam` là `prop_am-tra` tối thân ấm 32 % bằng code; `_nap-mo` giữ bếp và thân dưới của `prop_am-tra`, chỉ ghép phần nắp mở từ ảnh vẽ (hoà 24 px). Đổi trạng thái chỉ thấy nắp và độ sẫm thay đổi.
- **`prop_am-tra.png` mới thay bản cũ trong `Assets/VuonTra/Art/Final`:** bản cũ không có bếp và có hơi nước vẽ chết vào hình (trái quy tắc nhà số 3). Khi #146 gắn vào game, chép bộ này vào `Art/Final` (Unity tạo `.meta` khi import).
- **Giỏ ba mức** vẽ lại từng giỏ nên lệch nhau; giỏ chỉ ~40 px bên hông thỏ nên không thấy.
- Món trà sữa (`product_tra-sua`) và chén trà (`product_am-tra`) đã có trong `Art/Final`, không vẽ lại.

Bằng chứng: `preview_qa_sheet.png` (mỗi hình trên ca rô, cỏ `#3D8369`, tối `#2B2420`; hàng đổi trạng thái ấm; quai ấm 400 %). Đo: 0 điểm màu nền cũ trên mép, 0 điểm đen thuần ở mọi tệp.

```
VERDICT: APPROVED
1. pass - ấm ba trạng thái dùng chung bếp và thân dưới, chỉ đổi nắp và độ sẫm; không khe ở mối ghép
2. pass - mỗi nhóm trạng thái chung một khung, căn đáy-giữa (pivot chân)
3. pass - viền mực tím, 0 điểm đen thuần; men xanh ngọc, than cà rốt, giỏ tre, lá xanh hợp art bible
4. pass - 0 điểm màu nền cũ trên mép; lỗ quai ấm và quai giỏ trong suốt; hơi nước không còn viền xanh
5. pass - không hơi nước hay giọt vẽ chết vào trạm; hạt là sprite riêng cho Shuriken
6. pass - tỉ lệ trong nhóm giữ chung một hệ số; bóng để engine đổ
7. n/a - không có UI
```

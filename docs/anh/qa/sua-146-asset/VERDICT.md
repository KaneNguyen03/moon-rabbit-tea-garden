# Sửa asset #146: VERDICT

Bằng chứng: `preview_qa_sheet.png` (sau khi sửa, trên ô cờ, cỏ #3D8369, tối #2B2420, kèm khe mái quầy phóng 400%), `truoc-sau.png` (hai cột trái là trước, hai cột phải là sau), `clip-flat.png` và `co-cream.png` (khung hình các clip Cream chụp batch bằng `RabbitRigBuilder.BuildAndCapture` qua gate, ngày 26/11).

```
VERDICT: APPROVED
1. pass - cổ Cream không còn vệt mực ở cằm trong mọi khung hình của clip, kể cả lúc gật đầu; bbox các lớp giữ nguyên nên rect và bone trong .meta không đổi
2. pass - các lớp giữ nguyên khung và vị trí; các khung hình clip không giật
3. pass - viền Ink trên mọi phần; lông rig kéo về tông Thỏ Bông (#FEF4DA); nồi hồng vẫn là placeholder nét dày, sẽ thay ở #131
4. pass - hết viền xám xanh ở quầy, đèn, nắp ấm, giọt và lấp lánh; tam giác giữa mái và xà của quầy giờ trong suốt
5. pass - bỏ bong bóng vẽ sẵn trên nồi hồng; hơi và bọt sẽ làm bằng Shuriken
6. pass - kích thước không đổi; bóng dưới chân là phần code (ASSET-012)
7. không áp dụng (không có UI)
```

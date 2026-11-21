# Báo cáo gửi PO

Mỗi bước xong được nối thêm một đoạn vào cuối file này. Việc cuối cùng của mỗi bước là chạy toàn bộ test EditMode; sau đó cây làm việc để yên.

## R5 — Khoảnh khắc gọi thỏ (cổng trăng), kèm ba sửa nhỏ của R4

- Test: 266/266 EditMode qua (chạy sau lần sửa file cuối cùng trong Assets).
- Thông điệp commit đề xuất: `A called rabbit steps out of the moon gate; new kinds are marked in the album`
  - Thân: resting rabbits sway and doze; the swing's rabbit sits on its seat in front of the frame.
  - Kết thúc: `Co-Authored-By: Claude Fable 5.2 <noreply@anthropic.com>`
- Ảnh (docs/anh/po-r5/): `18a-gate.png` (cổng và ánh sáng), `18b-shadow.png` (bóng thỏ bước ra), `18c-arrived.png` (lộ hình, tên, "Mới!"), `8c-album-top.png`, `8d-album-bottom.png` (Sổ thỏ), `17-rest-bottom.png` (góc nghỉ sau khi sửa xích đu, có "z z").

### Đã làm
- `SummonMomentView`: bấm Gọi hoặc Ghép thì cổng trăng (`prop_cong-trang.png`, đã chép vào Art/Final) bật lên giữa màn hình trên nền tối; ánh sáng trong cổng và quầng sau cổng loé màu bậc; bóng đen của con thỏ bước ra và lớn dần; bóng tan thành hình loài, hiện tên, tấm "loài · bậc". Dài 2,3 giây. Chạm lần một nhảy tới lúc lộ hình, chạm lần hai đóng. Thay cho băng thông báo cũ.
- Loài mới (`Rabbit.FirstOfKind`): nhãn "Mới!" ở góc; trong Sổ thỏ ô của loài đó viền vàng và có thẻ "Mới" cho tới khi mở trang loài.
- Xích đu: thỏ vẽ phía trước xích đu, chân đặt trên mặt đệm, thu còn 82%.
- Thỏ ở chỗ nghỉ: lắc lư chậm, thỉnh thoảng hiện bong bóng "z z" (khoảng 30% thời gian, mỗi con lệch pha).
- Sổ thỏ, ô loài chưa gặp: vốn đã là bóng đen của CHÍNH loài đó (tô đen hình loài, không phải một bóng chung). 42 loài cũ trông giống nhau vì chúng chung một dáng; 30 loài dáng mới thì thấy rõ dáng riêng (xem `8d-album-bottom.png`). Không phải sửa gì.

### Test mới
- Gọi thì màn mở, bắt đầu là bóng, đúng hình loài; chạy hết thì lộ hình và có "Mới!"; chạm sau khi lộ thì đóng; chạm giữa chừng thì nhảy tới cuối, chạm nữa mới đóng; ô trong sổ có thẻ "Mới" và mất khi mở trang loài.
- Ghép ra loài đã gặp thì không có "Mới!".

### Chưa đạt
- Mới soi ba khung hình tĩnh của màn cổng trăng; chưa xem chuyển động trong bản build. Bản Mac ở Build/Mac vẫn là bản R2.
- Bậc Bạc màu xám nhạt nên ánh sáng trong cổng gần như không đổi màu.
- Không có âm thanh riêng theo bậc (dùng tiếng "sting" sẵn có).
- Dấu "Mới" trong sổ chỉ nhớ trong phiên chơi, không lưu vào save.
- Thỏ đi dạo vẫn hiện ra và biến mất tức thì; thỏ ngồi không có mũ hay tên.

### Hình cần vẽ
- Chưa cần thêm cho R5. (Xích đu hai lớp không còn cần.)

### Việc kế tiếp
- R6: bảng số cấp vườn và mô phỏng sẽ được ghi vào file này trước khi code giao diện.

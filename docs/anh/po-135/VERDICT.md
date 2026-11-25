# visual-critic-gate — #135 bước 2 (nhãn vườn theo bậc và zoom)

Bằng chứng: `135-truoc-sau-xam.png` (trước trên nhánh do-net-do-hoa, sau trên nhánh nhan-vuon, bản xám thu 25 %), `135-sau-114.png`, `135-sau-91c-0.png`, từ `tools/verify.sh --fast --preview` GREEN 1684/1684.

```
VERDICT: APPROVED
1. pass - chấm số nằm sát đồ vật (ao ở mép nước, cối ở mép cối, hũ, giỏ), không trôi lơ lửng
2. pass - không nhãn nào đè nhau; bảng trạm chỉ còn thanh tiến độ ở zoom mặc định
3. pass - chấm nền mực tối, chữ kem; không đen thuần
4. pass - không quầng
5. pass - không hiệu ứng vẽ chết
6. pass - bản xám 25 %: thỏ là điểm sáng nhất, các chấm tối lùi về sau
7. pass - nhãn chữ ở zoom mặc định còn 4 ("Bông" và ba "Xếp thỏ +"); câu đầy đủ, "Cấp N" và giờ mở hiện ở zoom gần (DetailZoom ≈ 1,165)
```

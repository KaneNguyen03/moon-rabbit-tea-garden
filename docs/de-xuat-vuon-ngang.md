# Đề xuất: vườn là dải toàn cảnh cuộn ngang

Ngày 23/11/2026. Chủ game duyệt hướng này ("Ghi và giao B"). Bố cục là của mình, không chép game gốc.

## Hiện trạng

- `GardenScreen` (`Assets/VuonTra/Game/UI/GardenScreen.cs:30`): vườn là cột dọc `WorldW = 1080`, `WorldH = 3770`; `ScrollRect` chỉ cuộn dọc, cuộn ngang chỉ bật khi zoom gần làm vườn rộng hơn màn hình.
- Ở zoom mặc định 0,83 bãi cỏ nằm giữa, hai bên là rừng, không có gì để kéo ngang.
- Mở rộng (`Expansion` trong `Assets/VuonTra/Core/Garden.cs:120`) là vòng đồng tâm trên lưới lục giác (#52/#53); #67 đẩy hàng trạm mới lên đầu vườn (cao thêm 800).
- Hệ quả đã ghi ở báo cáo #50 và #67: nhìn xa không thấy cả vườn; trạm mới ở xa tiệm trà; giữa màn hình nhiều ô trống, trạm rải dọc, không thành một cảnh.
- Cử chỉ dọc bị dùng chung cho vườn, tấm trượt, danh sách, nên đã phải vá luật vuốt nhiều lần.
- HUD, khay và thanh tab che khoảng một phần tư chiều cao; chiều ngang gần như trống.

## Đề xuất

1. **Kích thước.** Vườn rộng khoảng 3–4 màn hình, cao khoảng 1,1–1,2 màn hình (phần nhìn được giữa HUD và khay/tab). Vườn kéo ngang là chính; kéo dọc chỉ còn chút đàn hồi.
2. **Bố cục.** Tiệm trà ở giữa là "nhà". Mỗi bên là một khu: trái là ao và suối, phải là lò bánh và cối giã (chỗ đặt cụ thể do B đề xuất theo lưới hiện có). Trạm tiếp sức vẫn đứng gần trạm chúng tiếp sức.
3. **Mở rộng.** Thay vòng đồng tâm bằng từng đoạn sương tan sang trái hoặc phải. Mỗi đoạn giữ giá và cấp vườn của vòng tương ứng hiện nay (R 4 … R 9) để nhịp không đổi; `GardenPacingTests` và luật ≥17 seed của #83 phải giữ nguyên kết quả.
4. **Camera.**
   - Kéo ngang có quán tính, biên đàn hồi (ScrollRect có sẵn).
   - Nút về tiệm trà.
   - Bấm dòng trạm trong tab Trạm thì vườn trượt ngang tới trạm đó; vuốt trái/phải trên tấm trạm vẫn nhảy trạm kế.
   - Ba mức zoom giữ nguyên. Nhìn xa thấy cả chiều cao vườn và càng nhiều chiều ngang càng tốt, không trạm nào bị thanh tab che.
5. **Chiều sâu.** Parallax: rừng xa trôi chậm hơn bãi cỏ (tắt khi Giảm chuyển động).
6. **Save cũ.** Trạm, ô đất đã mở, đạo cụ và zoom đã lưu phải mở đúng chỗ trên bố cục mới, không mất gì.

## Thứ tự và phạm vi

- Đụng tới luật bố trí của #54 (kéo thả trạm, ô Reserved cho đạo cụ ngoài lưới): làm trên nền #54 đã gộp, không làm song song với việc khác sửa `GardenScreen`/`Garden.cs`.
- Có thể tách: (a) Core — lưới và `Expansion` theo đoạn ngang, migrate save, test nhịp; (b) màn hình — kích thước thế giới, camera, rừng viền, parallax, ảnh chụp.
- Tiêu chí xong: test mới đỏ trên code cũ, xanh sau khi sửa; `tools/verify.sh` xanh; ảnh trước/sau ở `docs/anh/po-vuon-ngang/` (khung mở đầu, nhìn xa, sau một lần mở rộng, tab Trạm bấm dòng); mục trong `docs/bao-cao-po.md`; comment theo từng ý trên #16.

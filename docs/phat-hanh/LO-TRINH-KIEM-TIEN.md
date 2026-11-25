# Lộ trình tới doanh thu

*Viết 24/11/2026 theo yêu cầu của chủ project: mục tiêu là kiếm tiền. Nguyên tắc xếp: việc nào ra tiền sớm và đo được thì làm trước, tính năng mới làm sau.*

Tình trạng lúc viết:
- **Mua bằng tiền thật (#9):** code đã xong. Ba gói `vuontra.golden.50/150/500` chạy qua Unity IAP 4.15 (`Game/Store/UnityStorefront.cs`), chỉ còn chờ khai sản phẩm và giá trên hai cửa hàng.
- **Quảng cáo có thưởng (#66):** mới có phần khung (`IRewardedAds`). `GameRunner.AdsInPlayerBuilds = false`, chưa cắm SDK thật.
- **Analytics và báo crash:** chưa có.

## Giai đoạn 0: việc phải làm ngay (tuần 1, phần lớn do chủ project làm)
1. **Đăng ký tài khoản Google Play** ngay. Tài khoản cá nhân mới phải chạy thử kín 12 người trong 14 ngày mới được phát hành, nên đăng ký sớm thì 14 ngày đó bắt đầu sớm. Đăng ký Apple sau cũng được.
2. Gỡ các việc chặn A–E trong [CHECKLIST.md](CHECKLIST.md): trang chính sách quyền riêng tư công khai, email hỗ trợ thật, bản Android dạng AAB có keystore, tên gói `com.vuontratho.game`, ảnh chụp đúng cỡ. Liên quan #41, #42.

## Giai đoạn 1: đo lường và sửa lỗi giữ chân người chơi (tuần 1–2, làm song song)
3. **Thêm analytics và báo crash** (Firebase hoặc GameAnalytics); chưa có issue. Thêm xong phải sửa lại bản khai quyền riêng tư trong `docs/khai-bao-du-lieu-cua-hang.md`.
4. **#97**: vườn đầy thì giật vì GC chạy quá nhiều. Đây là lý do người chơi xoá game trên máy yếu.
5. **#99, #98**: nhịp chơi bị gãy ở ngày 2 và ở tuần 2–6.
6. **Tiếng Anh: #93, #94, #96, #92.** Phần lớn doanh thu sẽ đến từ người chơi nước ngoài.

## Giai đoạn 2: chạy thử kín Android 14 ngày, trong lúc đó thêm tính năng kéo người chơi quay lại
7. **#65 Thông báo nhắc**: đòn bẩy giữ chân mạnh nhất của thể loại này.
8. **#57 Lịch quà 7 ngày**, **#69 Màn chào khi quay lại**.
9. Sửa lỗi save cũ và quà: #85, #86, #89, #90.

## Giai đoạn 3: bật kiếm tiền (tuần 3–4)
10. **Bật #9**: khai sản phẩm, đặt giá, bật kiểm tra hoá đơn.
11. **#66 Quảng cáo có thưởng**: cắm SDK thật (đề xuất AdMob), hộp đồng ý GDPR và hộp ATT trên iOS, cập nhật bản khai quyền riêng tư.
12. **#64 Hũ trăng**: thêm chỗ đáng để tiêu cà rốt vàng.

## Giai đoạn 4: phát hành mềm rồi mở rộng
13. Phát hành Android ở vài nước giá quảng cáo rẻ, nói tiếng Anh (ví dụ Philippines, Canada). Đo tỉ lệ quay lại ngày 1/7/30 và doanh thu trên mỗi người chơi (ARPDAU). Ngưỡng tham khảo cho thể loại: ngày 1 ≥ 35–40 %, ngày 7 ≥ 15 %, ngày 30 ≥ 5 %.
14. Số liệu đạt thì ra iOS, phát hành toàn cầu, đẩy clip TikTok và Reels. Sau đó vận hành bằng sự kiện theo mùa (#8).

## Tạm gác tới khi có số liệu
#91 bản đồ 2D, #52 và #53 lưới lục giác và mở rộng rừng, #11 trang trí, #10 trò chơi nhỏ, #63 ASMR, #39 và #40 lưu lên mây.

## Chủ project cần quyết ngay
- Đăng ký tài khoản Google Play.
- Chọn SDK quảng cáo (đề xuất AdMob) và công cụ analytics.

Phía code có thể bắt đầu ngay mục 3–6 mà không cần chờ chủ project.

## Cờ bán hàng (#144): mặc định tắt
*Chủ project 25/11: "làm 1 cái cờ để tắt bật tính năng này … trạng thái default là tắt". Việc kiếm tiền dừng ở đây cho tới khi chủ project bật lại.*

- **Một chỗ:** `Features.Monetization` trong `Assets/VuonTra/Core/Features.cs`. Giá trị đầu là `Features.MonetizationByDefault`: `false`, trừ khi bản build có define `VUONTRA_MONETIZATION`.
- **Khi tắt:** Tiệm không có nút "Mua thêm" (thẻ gói cà rốt vàng, Vé trăng, Vé Bỏ quảng cáo, nút Khôi phục), không có gói người mới và gói mốc cấp (#105), không có nút xem quảng cáo (#66) ở Tiệm, màn đi vắng và các trò nhỏ. Vé tháng (#107) chỉ còn hàng miễn phí, thẻ thu gọn lại. Không có giá tiền ở đâu cả. `UnityStorefront` (Unity IAP) và nhà cung cấp video không được tạo, nên không gọi `UnityPurchasing.Initialize`. Các sự kiện analytics về mua hàng, gói, quảng cáo và vé (`AnalyticsEvents.Monetization`) không được ghi; các sự kiện chơi khác vẫn ghi. Đồng hồ gói ưu đãi không chạy, nên khi bật lại, gói người mới bắt đầu tính giờ từ lúc bật.
- **Quyền lợi đã trả vẫn giữ:** save đã mua Vé trăng vẫn nhận thư cà rốt mỗi ngày, viên "Vé trăng · còn N ngày" và giờ đi vắng thêm, nhưng không có thư nhắc mua lại. Save đã mua Vé tháng tháng này vẫn thấy và nhận hàng Vé tháng. Save còn hạn Vé Bỏ quảng cáo vẫn có nút "Nhận ngay" (không có video). Không thu hồi gì.
- **Bật cho bản build thử:** dựng với biến môi trường `VUONTRA_MONETIZATION=1`, ví dụ `VUONTRA_MONETIZATION=1 tools/build.sh android`. `BuildScript` thêm define `VUONTRA_MONETIZATION` cho lần dựng đó. Muốn bật hẳn thì thêm `VUONTRA_MONETIZATION` vào Player Settings → Scripting Define Symbols. Trong Editor và test, đặt `Features.Monetization = true` (test dùng `[MonetizationOn]`).
- **Không đổi:** gói, giá, trần quảng cáo, save (vẫn v14) và mã kiếm tiền đều giữ nguyên; bật cờ là về đúng như trước.

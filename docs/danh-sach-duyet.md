# Danh sách duyệt cho PO (cập nhật 22/11/2026 tối, theo main `a3663d6`)

Mọi việc dưới đây đã có trên `main` và đã push; bản Mac mới nhất ở `Build/Mac/VuonTraTho.app`. Ảnh ở `docs/anh/po-NN/` (NN là số issue). Theo quyết định ngày 22/11: nhóm đã xong được đóng theo mặc định, các việc "cần chủ project" đã làm phần code; những gì còn mở dưới đây chỉ chờ bạn. Mỗi dòng: xem gì, ảnh ở đâu, quyết gì.

Đã đóng hôm nay (23 issue): #7, #12, #14, #15, #17, #18, #22, #23, #24, #25, #26, #27, #30, #31, #32, #33, #34, #35, #36, #38, #43, #46, #47, #48, #49. Mở lại cái nào nếu không đồng ý mặc định ghi trên issue.

## A. Xem ảnh rồi đóng: không còn gì chờ bạn quyết

| Issue | Xem gì | Ảnh | Còn nhỏ |
|---|---|---|---|
| #2 Hoạt ảnh nghề (bản code) | Thỏ ở trạm cầm dụng cụ của trạm và làm động tác nghề | `po-2/2-dung-cu-nghe.png` | Hình riêng từng nghề vẽ sau nếu muốn |
| #3 Dáng nghỉ (bản code) | Thỏ ở võng nằm, ghế nấm và xích đu ngồi, có mũ và tên | `po-3/3-dang-nghi.png` | Hình dáng ngồi vẽ sau nếu muốn |
| #6 Cân lại kinh tế | Bảng mô phỏng trước và sau: cấp vườn 5 ở phút 7, cấp 10 tối ngày 1, nhiệm vụ không còn chiếm phần lớn cà rốt | `po-6/6-mo-phong-truoc.txt`, `6-mo-phong-sau.txt` | Chỉnh tiếp sau khi có người chơi thử |
| #11 Trang trí vườn | 8 chỗ cố định, 4 món từ hình sẵn, đổi màu đèn lồng, kệ trong Tiệm | `po-11/11-sau-trang-tri.png`, `11-sau-ke-trang-tri.png` | — |
| #20 Khách ghé vườn | Khách đặt đơn, giao đủ được thưởng | `po-20/` | Bong bóng chưa có hình riêng; mỗi lúc một khách |
| #28 Chuyển động | Cảnh nền đung đưa, thỏ đi thật, cà rốt bay, vòng sao, nút lún | `po-28/` | Chớp mắt và vẫy tai cần khung hình |
| #37 Khay sản phẩm | Bảy món vẽ riêng hiện trong khay, chạm để bán, vuốt bán hết | `po-37/37-khay-hinh.png` | — |
| #44 Cà rốt vàng và Tiệm | Quà ngày, Gọi vàng, thỏ ghé tiệm, sáu mũ, nhuộm lông | `po-44/` | — |
| #10 Hứng lá trà, Đêm đom đóm | Hai trò chơi 30 và 20 giây, thưởng theo công thức nhiệm vụ | `po-10/` | Lá, lọ và đom đóm vẫn là hình tròn tô màu |

## B. Đã làm theo mặc định, cần bạn xác nhận một điểm

| Issue | Mặc định đã làm | Ảnh | Quyết gì |
|---|---|---|---|
| #1 Mùa trăng mới | Mở ở cấp vườn 30, +25 % mỗi mùa cộng dồn, ô đất và chỗ nghỉ về đầu, Thỏ Trăng Non bậc Tím lần đầu | `po-1/1-mua-trang-*.png` | Bốn ô trong `docs/de-xuat-1.md` |
| #8 Thỏ theo mùa | Ba loài Vàng (đèn ông sao, áo dài, Noel), khoảng ngày 1/9–30/9, 15/1–28/2, 1/12–31/12, ghé tiệm 120 cà rốt vàng, nhiệm vụ mùa 3/5/10, trang trí cổng | `po-8/8-cong-*.png`, `8-so-tho-mua.png`; ứng viên 2 trong `po-8/_tong-hop.png` | Có dùng ứng viên 2 mỗi mùa (bậc Tím) không |
| #45 Giã bánh trăng | Bấm theo nhịp, 3 lượt/ngày, thưởng 60 giây thu nhập, hình chày và bánh đã vẽ | `po-45/` | Cửa sổ ±80/±160 ms có vừa tay không (chơi thử) |

## C. Cần bạn nghe hoặc chơi thử trên bản Mac

| Issue | Thử gì |
|---|---|
| #13 Khoảnh khắc gọi thỏ | Nghe tiếng theo từng bậc; dấu Mới còn sau khi mở lại game |
| #29 Phản hồi khi chạm | Nghe các tiếng mới; rung chỉ thử được trên điện thoại (#41) |
| #19 Chạm và vuốt thỏ | Cảm giác vuốt, tim bay, +5 Trà lực lần đầu mỗi ngày |
| #21 Ngày và đêm | Mở game ban ngày thật xem lớp phủ có đậm quá không |
| #4 Chơi thử tay | Danh sách trong issue; ghi lỗi thấy được thành comment |

## D. Chỉ bạn làm được (code đã xong tới mức máy này cho phép)

| Issue | Code đã có | Bạn cần |
|---|---|---|
| #9 Mua cà rốt vàng | Nút Mua thêm trong Tiệm, ba gói, khôi phục giao dịch, kiểm hoá đơn qua seam, chạy với cửa hàng giả trong editor | Tài khoản Apple Developer và Google Play Console, khai gói và giá thật; `po-9/` |
| #39 Game Center và iCloud | Tự đăng nhập, nhớ đăng nhập, save lên iCloud key-value qua plugin gốc | Tài khoản Apple, bật Game Center và iCloud, module iOS, thử trên iPhone |
| #40 Play Games và Saved Games | Tự đăng nhập, save trong Saved Games | Tài khoản Google Play Console, OAuth và SHA-1, module Android, thử trên máy thật |
| #41 Bản build iOS và Android | Đích build, bundle id, chỉ dọc, icon, màn hình chờ, `tools/build.sh` | Cài module iOS và Android qua Unity Hub, Xcode, Android SDK |
| #42 Quyền riêng tư | Chính sách bản nháp hai thứ tiếng, Xoá dữ liệu của tôi, chép mã, liên hệ, bản nháp khai báo dữ liệu và nhóm tuổi (`docs/khai-bao-du-lieu-cua-hang.md`) | Duyệt hai bản nháp; điền email thật vào `AppInfo.SupportEmail` và link chính sách công khai vào `AppInfo.PrivacyUrl` |
| #5 Chụm trackpad Mac | Bundle gốc macOS đọc magnify, chụm để zoom | Thử trên bản Mac mới (build sau `73d1611`) |
| #16 Hàng đợi MVP | Mọi dòng đã tick trừ "Màn lúc bạn đi vắng" | Nói muốn thêm gì ngoài tấm Away đang có, hoặc tick và đóng |

## Gợi ý cách làm

1. Nhóm A: xem ảnh, đóng một lượt (đưa danh sách số, session điều phối đóng hộ).
2. Nhóm B: trả lời từng câu một chữ; session điều phối chuyển cho session code.
3. Nhóm C: mở `Build/Mac/VuonTraTho.app`, chơi 10–15 phút.
4. Nhóm D: cung cấp tài khoản và cài module khi tới lúc lên cửa hàng; code đã sẵn.

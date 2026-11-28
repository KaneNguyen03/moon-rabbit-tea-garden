# Danh sách duyệt cho PO (cập nhật 23/11/2026, theo main `939c936`)

Mọi việc dưới đây đã có trên `main` và đã push. Ảnh ở `docs/anh/po-NN/` (NN là số issue; lỗi QA ở `docs/anh/qa-NN/`). Mỗi dòng: xem gì, ảnh ở đâu, quyết gì. Các lỗi QA #70–#78 đã kiểm lại đạt và đã đóng (comment đóng ghi commit), nên không còn trong danh sách.

## Các con số cần chủ project quyết

Đã có mặc định trong code; chỉ cần trả lời "giữ" hoặc đưa số khác.

| Issue | Đang để trong code | Quyết gì |
|---|---|---|
| #52 Lưới ô lục giác | Lưới bắt đầu ở bán kính 3 (đề xuất là 2), mỗi đợt mở đất thêm một vòng: 4, 5, 6, 7 | Giữ bán kính 3 (bốn trạm đầu cần 3 vòng với ô 150×200) |
| #53 Mở rộng rừng theo vòng | Giá mỗi vòng bằng giá hai trạm kế (thử 25 % giá một trạm thì trò chơi + đơn khách tụt còn 11,7 %); vòng R7 cần cấp vườn 15 | Đổi R7 sang cấp 16 để "đủ 12 ô không trước ngày 3" đúng với mọi seed (cấp 15 chỉ đúng với seed hiện tại) |
| #54 Chế độ Bố trí | (1) Ấm trà chỉ dời được, không cất vào kho; (2) trạm mới mua vẫn vào ô mặc định, không hỏi chọn ô; (3) đèn lối đi chỉ dựng lại khi bấm "Xong"; (4) giỏ hứng lá đứng cố định cạnh ô nhà của trạm Giỏ lá, không đi theo khi trạm bị dời; (5) mái tiệm trà chồm lên hai ô phía sau, đồ đặt ở đó đứng sau tiệm nên không che | Chấp nhận cả năm điểm, hay muốn đổi điểm nào |
| #56 Thỏ xin chụp ảnh | Thêm nhiệm vụ ngày "Chụp 3 tấm thỏ xin ảnh" và giữ "Chạm bé thỏ 50 lần", nên có năm nhiệm vụ ngày | Giữ năm, hay bỏ "Chạm bé thỏ 50 lần" về bốn |
| #58 Câu cá và tim | Tim lên 10, hệ số tim tới ×2,6; nút Cho ăn luôn dùng con cá điểm cao nhất | Giữ trần 10 tim; có cần bảng chọn từng loại cá khi cho ăn không |
| #61 Sổ thỏ có thưởng | **Đã chốt 23/11:** mốc 10 loài 1 cà rốt vàng, mốc 20 loài 2 cà rốt vàng + 1 cỏ | Không còn gì; đóng sau khi xem ảnh |
| #62 Mưa và Thỏ Ếch | Thỏ Ếch chỉ đến từ cấp vườn 15; hai cơn mưa mỗi ngày theo giờ máy | Giữ cấp 15 và giờ mưa (từ cấp 1 thì trò chơi + đơn khách ngày 1 tụt 9,5 %) |
| #63 Tiếng trạm khi phóng to | Mỗi trạm một vòng tiếng, thanh "Tiếng trạm" mặc định 0,5 (0,7 tới 23/11, xem #63 / #58 sửa tiếp) | Nghe trên bản Mac ở zoom gần, ghi tiếng nào chói |
| #64 Hũ trăng | Hũ từ nhiệm vụ thu hoạch và Đêm đom đóm chỉ từ cấp vườn 15 | Giữ cấp 15 (nếu bỏ, người thong thả mở đủ 12 ô ngay ngày 2) |
| #66 Quảng cáo thưởng | Chỉ có seam và quảng cáo giả trong editor, chưa cài SDK; ba chỗ với trần 1 / 1 mỗi trò / 2; vé Bỏ quảng cáo 2,99 $ (khoảng 75.000 ₫) cho 30 ngày, gói tiêu hao không khôi phục | Có bật quảng cáo không; trần; giá vé và số ngày; vé có cần khôi phục giao dịch không |
| #67 Bốn trạm mới | Trạm 13–16 mở ở cấp vườn 16 / 18 / 20 / 23 (issue đề xuất 18 / 22 / 26 / 30); vòng rừng R8, R9 hỏi cấp 16, 20 | Giữ 16 / 18 / 20 / 23 (người thong thả đứng ở cấp 23 từ ngày 9 tới 14) |
| #68 Mã quà và hộp thư | Mã trong bảng hết hạn 31/3/2027 (`TRATHO-2026`: 5 cà rốt vàng + 2 cỏ; `CAMON-PLAYTEST`: mũ Nơ trăng); hộp thư tối đa 10 thư, giữ 30 ngày; chưa có khoá ký mã thật | Giữ hạn mã và quà; tạo khoá ký (`keys.json`) khi muốn phát mã riêng |
| #84 Bong bóng đơn khách | Bong bóng mọc sang phải trên đầu khách, về phía lọ đom đóm; bảng Câu cá dời trái và hẹp lại | Xem bong bóng ở chỗ mới có ổn không |

## E. Mới tối qua và sáng nay: xem ảnh

| Issue | Xem gì | Ảnh | Còn lại |
|---|---|---|---|
| #50 Vườn trông chật | Zoom mặc định lùi 17 %, mở game thấy 11/12 chỗ trạm | `po-50/50-truoc-*.png` → `50-sau-*.png` | Save cũ nhớ zoom 1,0 giữ tới khi đổi zoom |
| #51 Bãi cỏ quang đãng | Phần chưa mở là vành rừng bao quanh, không còn mây giữa màn | `po-51/51-dot-0…4.png`, `51-mo-vuon*.png` | — |
| #52 Lưới ô lục giác | Trạm, chỗ nghỉ, quầy, trang trí đặt theo ô; khung ngày 1 gần như không đổi | `po-52/52-truoc-*.png` → `52-sau-*.png` | Quyết bán kính ở bảng trên |
| #53 Mở rộng rừng theo vòng | Bấm một nút, vành cây lùi ra, thêm một vòng ô | `po-53/53-vong-1-*.png` | Quyết R7 ở bảng trên |
| #54 Chế độ Bố trí | Giữ kéo thả trạm sang ô trống, hoán đổi, cất vào kho | `po-54/54-bo-tri-bat.png`, `54-dang-nhac.png`, `54-sau-hoan-doi.png`, `54r-truoc.png` → `54r-sau.png` | "+5 % khi trạm tiếp sức đứng kề" chưa làm |
| #55 Thỏ đi dạo qua ô trống | Thỏ nghỉ đi tới chỗ cỏ trống, uống trà, đuổi đom đóm, ngủ gật | `po-55/55-sau-uong-tra.png`, `55-sau-duoi-dom-dom.png` | — |
| #56 Thỏ xin chụp ảnh | Bong bóng máy ảnh mỗi 4 phút, chụp được 120 giây thu nhập | `po-56/56-xin-chup.png`, `56-khung-thu.png` | Quyết năm nhiệm vụ ngày ở bảng trên |
| #57 Lịch quà 7 ngày | Lịch thấy trước ở Tiệm, ngày 7 là 5 cà rốt vàng + Nón lá + 1 cỏ | `po-57/57-ngay-1.png`, `57-ngay-4.png`, `57-ngay-7.png` | — |
| #58 Câu cá và cho thỏ ăn | Ao chạm được, phao, cá bay về giỏ, Cho ăn trên tấm thỏ, huy hiệu 10 tim; hình phao, sóng, mặt ao vẽ tay | `po-58/58-ao-*.png`, `58-phao-chim.png`, `58-ca-bay.png`, `58-cho-an*.png`, `58-tim-10-*.png` | Nghe tiếng câu cá; quyết bảng chọn cá ở bảng trên |
| #59 Thỏ tự nói chuyện | Mỗi 40–70 giây một bé nói một câu theo nghề, pha ngày và mùa | `po-59/59-noi-ngay.png`, `59-noi-dem.png` | Chưa có tiếng, bong bóng chưa có đuôi |
| #60 Thỏ nghỉ góp sức | Mỗi bé ở chỗ nghỉ cộng 2–10 % thu nhập vườn theo bậc | `po-60/60-thu-nhap.png`, `60-tho-nghi.png`, `60-tab-tho.png` | Chưa có dấu "nghỉ" ngoài vườn |
| #61 Sổ thỏ có thưởng | Đủ bộ nghề trong bậc +3 %, mốc 10/20/40/72 loài | `po-61/61-so-tho-bo.png`, `61-du-bo.png` | Đã chốt; đóng được |
| #62 Mưa và Thỏ Ếch | Hai cơn mưa mỗi ngày, Thỏ Ếch tặng 10 phút thu nhập chỉ trong mưa | `po-62/62-mua-ngay.png`, `62-mua-dem.png`, `62-tho-ech-den.png` | Mũ ếch còn dùng Mũ len nhuộm xanh |
| #63 Tiếng trạm | Mỗi trạm một vòng tiếng khi phóng to, thanh Tiếng trạm | `po-63/63-tieng-tram.png`, `10-pause.png` | Cần nghe thử |
| #64 Hũ trăng | Hũ 1 giờ thu nhập ở ví và kệ Tiệm, hình hũ vẽ tay | `po-64/64-vi-hu-truoc.png` → `64-vi-hu-sau.png`, `64-ke-hu-truoc.png` → `64-ke-hu-sau.png` | Quyết cấp 15 ở bảng trên |
| #65 Nhắc tôi | Công tắc trong Tạm nghỉ, mặc định tắt, nhắc khi chạm trần vắng mặt, quà ngày, mùa về | `po-65/65-nhac-toi.png`, `65-nhac-toi-en.png` | Khởi động lại máy thì mất nhắc tới lần chơi sau |
| #66 Seam quảng cáo | Ba chỗ mời xem 30 giây, chạy với quảng cáo giả | `po-66/66-*.png` | Quyết ở bảng trên |
| #67 Bốn trạm mới | Chậu sen, lò kẹo, ruộng nếp, thuyền trà; món mới trong khay | `po-67/67-tram-moi-cho.png`, `67-vong-moi.png`, `67-tram-moi-mo.png`, `67-khay-keo.png` | Quyết cấp mở ở bảng trên |
| #68 Mã quà và hộp thư | Ô "Nhập mã quà" trong Hồ sơ, hộp thư trong Tạm nghỉ | `po-68/68-ho-so-ma-qua.png`, `68-hop-thu.png`, `68-ma-sai.png` | Thử nhập mã trên bản Mac |
| #69 Màn lúc bạn đi vắng | Trần vắng mặt 3 giờ, bốn dòng kể, nhận ×2 một lần mỗi ngày | `po-69/69-sau-mung-quay-lai.png` | Nút ×2 qua quảng cáo chờ #66 |

## F. Lỗi QA đã sửa: xem ảnh trước và sau rồi đóng

| Issue | Ảnh | Còn lại |
|---|---|---|
| #79 Chữ CHẠM ĐỂ BẮT ĐẦU đè tấm Mừng bạn quay lại | `qa-79/qa-79-truoc.png` → `qa-79-sau.png` | — |
| #80 Màn đi vắng luôn báo 8 vị khách | `qa-80/qa-80-truoc.png` → `qa-80-sau.png` | Không chia thời gian vắng tại nửa đêm |
| #81 Tấm chi tiết thỏ tràn với tên loài dài | `qa-81/qa-81-truoc*.png` → `qa-81-sau*.png` (vi, en, sổ thỏ) | — |
| #82 Ghế nấm che nhãn cấp trạm | `qa-82/qa-82-truoc*.png` → `qa-82-sau*.png` | Nhãn tên thỏ trên trạm chưa lên lớp trước |
| #84 Bong bóng đơn che bảng Câu cá | `qa-84/qa-84-truoc*.png` → `qa-84-sau*.png` | Quyết chỗ bong bóng ở bảng trên |

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
| #1 Huy hiệu mùa (hình, chưa nối) | Chip "Mùa N" ở HUD và hồ sơ đang vẽ bằng code; bốn ứng viên tròn để thay, số do game vẽ đè lên | `po-1/badge_mua-giay.png`, `badge_mua-dem.png`, `badge_mua-go.png`, `badge_mua-tim.png` (cả tấm: `po-1/_tong-hop-huy-hieu.png`) | Chọn một trong bốn (giấy viền vàng, men xanh đêm, gỗ có ruy băng, trăng tím), hoặc giữ chip vẽ bằng code |
| #49 Dấu mốc trạm (hình, chưa nối; #49 đã đóng) | Đang dùng dây đèn, đèn đá và vầng sáng vẽ bằng code; ba kiểu ứng viên, mỗi kiểu ba bậc cho cấp 25/100/300, cắt cùng tỉ lệ nên bậc 1 nhỏ hơn bậc 3 | `po-49/rank_co-1..3.png` (dây cờ), `rank_bien-1..3.png` (biển gỗ có sao), `rank_den-1..3.png` (dây đèn lồng); cả tấm `po-49/_tong-hop-dau-moc.png` | Chọn một kiểu, hoặc giữ kiểu hiện tại |

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

## G. Chờ thử trên máy thật sau bản build (#41)

| Issue | Code đã có | Thử gì trên máy thật |
|---|---|---|
| #109 Xin đánh giá đúng lúc | Thẻ "Bạn thích vườn trà chứ?" sau thỏ bậc Vàng trở lên, trò nhỏ 3 sao, mở đoạn đất hoặc tim thứ 10; chỉ từ ngày 3, sau 30 phút chơi, không lỗi văng hay mua hỏng trong phiên; tối đa 3 lần, cách 60 ngày; "Thích lắm" một lần là thôi hỏi. Seam `IReviewPrompt`, editor và Mac dùng bản giả ghi log. Ảnh `po-109/` | Cắm hộp đánh giá gốc (iOS `SKStoreReviewController`, Android Play In-App Review) và link trang cửa hàng vào `NativeReviewPrompt.Create()`; bấm "Thích lắm" thấy hộp của hệ điều hành; "Đánh giá game" trong Hồ sơ mở trang cửa hàng; "Chưa lắm" mở app mail với mã người chơi |

## Gợi ý cách làm

0. Bảng "Các con số cần chủ project quyết": trả lời "giữ" hoặc đưa số khác cho từng dòng; session điều phối chuyển cho session code.
1. Nhóm A, E, F: xem ảnh, đóng một lượt (đưa danh sách số, session điều phối đóng hộ).
2. Nhóm B: trả lời từng câu một chữ; session điều phối chuyển cho session code.
3. Nhóm C: mở `Build/Mac/VuonTraTho.app`, chơi 10–15 phút.
4. Nhóm D: cung cấp tài khoản và cài module khi tới lúc lên cửa hàng; code đã sẵn.

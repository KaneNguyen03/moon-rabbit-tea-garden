# Cats & Soup, vòng 2: game còn thiếu gì để thành bản phát hành hoàn chỉnh

Viết 24/11/2026 trên `main` `5b46772` (trước khi gộp #91). Vòng 1 (22/11) là `#50–#69`, ghi chép ở thư mục ghi chú của session điều phối (`cats-soup-gap.md`). Vòng này xét lại toàn bộ danh sách hệ thống của Cats & Soup, bám thứ tự ưu tiên trong [LO-TRINH-KIEM-TIEN.md](phat-hanh/LO-TRINH-KIEM-TIEN.md): giữ chân ngày 1–7 trước, rồi đến các món bán ra tiền.

## Đã xem gì

- Tài liệu: `docs/bao-cao-po.md` (bàn giao 23/11 tối, #97–#101), `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md`, `docs/issues-po.md`, 101 issue cũ (`gh issue list --state all`), source `Assets/VuonTra/Core` (Quests, Shop, Storefront, Ads, Mailbox, Away, Fishing, Seasons, Tutorial, Decor, RestSpots, Orders) và `Editor/MetaSeeder.cs`.
- Chơi thử: chưa có bản Mac sau khi gộp #91. Bản `/tmp/qaproj` (build 24/11 00:42, vườn ngang) mở được tới màn tiêu đề, nhưng máy không cho tiến trình của session gửi click hay phím giả lập (quyền Trợ năng chưa cấp, `AXIsProcessTrusted = false`), nên không qua được "Chạm để bắt đầu". Thay vào đó đã xem 175 ảnh preview do game tự chụp (`Logs/preview`, 24/11 04:16): vườn, Tiệm, Nhiệm vụ, tab Thỏ, Hồ sơ, gói mua, màn đi vắng, hướng dẫn, trò nhỏ, mùa lễ. Save của Editor (`~/Library/Application Support/VuonTraTho/Moon Rabbit Tea Garden/save.json`) đã sao lưu ở `/tmp/cs2/savebackup` và trả lại nguyên trạng.
- Cats & Soup: wiki fandom và miraheze (Daily missions, Facilities, Recipes, Little Trunk Shop, Special Events, Friends), thông báo cập nhật của nhà phát hành, các bài hướng dẫn cũ (levelwinner, playoholic). Những gì không kiểm được (thẻ tháng, gói người mới) ghi là "thể loại làm" thay vì "Cats & Soup làm".

## Bảng đối chiếu

Mức: **Đủ** (có, không cần gì cho bản đầu), **Mỏng** (có nhưng thiếu phần giữ chân hoặc ra tiền), **Chưa có**.

| Hệ thống của Cats & Soup | Game mình | Mức | Issue |
|---|---|---|---|
| Vòng lõi: mèo nấu, chạm bán, hàng đợi món | 16 trạm, chạm làm nhanh, khay 5→8 ô, khay đầy thì giữ mẻ (#37) | Đủ | — |
| Nâng cấp trạm, ×10/×100, nút gợi ý | Tới mốc, Tối đa, Nâng tất cả từ cấp vườn 15 (#36, #101), trần 500 | Đủ | — |
| Cơ sở mới theo tiến độ | 16 trạm theo 6 đoạn đất, cấp vườn và số ngày chơi (#67, #99) | Đủ | — |
| Mèo mới theo cấp, sao, giống | 72 loài + mùa + trăng non, 6 bậc, sao mỗi 10 cấp, ghép, Gọi vàng, thỏ ghé tiệm | Đủ | — |
| Tim và cho ăn | Vuốt mỗi ngày, cá cho ăn, tim tới 10 có hệ số (#19, #58) | Đủ | — |
| Trang phục và phụ kiện | 6 bùa/mũ có hệ số, 6 mũ đẹp, nhuộm 8 màu | Mỏng | Không tạo: cần tranh thỏ (pipeline của chủ project) |
| Nội thất, trang trí vườn | 4 món, 8 chỗ, màu đèn lồng; Bố trí (#54) | Mỏng | Không tạo: #11 đang gác theo lộ trình |
| Công thức và điểm công thức | Mỗi trạm 1 món; mốc 25/100/300 chỉ là đèn và biển | Mỏng | #110 |
| Chỗ nghỉ sinh tài nguyên | Thỏ nghỉ cộng 2–10 % (#60) | Mỏng | #110 (lá công thức) |
| Nhiệm vụ ngày, quà khi xong cả bộ | 5 nhiệm vụ ngày, không có quà cả bộ | Mỏng | #103 |
| Nhiệm vụ tuần | Không có | Chưa có | #103 |
| Vé nhiệm vụ tháng (Daily Missions Pass, Premium) | Không có | Chưa có | #107 |
| Thành tựu | ~30 cái một lần, hết trong tuần đầu | Mỏng | #111 |
| Lịch 7 ngày người mới, nhiệm vụ dẫn đường | Hướng dẫn 6 bước 60 giây (#30), lịch quà 7 ngày (#57) | Mỏng | #102 |
| Giới thiệu khi mở chức năng | Không có, tính năng chỉ xuất hiện | Chưa có | #104 |
| Sự kiện giới hạn | Ba mùa lễ (#8), 8 tháng/năm trống | Mỏng | #112 |
| Tiệm theo giờ, món sự kiện cũ (Little Trunk Shop) | Tiệm cố định, đồ mùa cũ không mua lại được | Chưa có | #113 |
| Ảnh và album | Chụp ảnh, khung, album 12, thỏ xin chụp mỗi 4 phút (#18, #56) | Đủ | — |
| Cá và câu | Ao, 5 mồi, 6 loài cá, cho ăn (#58) | Đủ | — |
| Bể cá (thả cá lấy thưởng vĩnh viễn) | Cá thừa sau tim 10 không có chỗ dùng | Chưa có | #114 |
| Mưa và ếch | Hai cơn mưa/ngày, Thỏ Ếch (#62) | Đủ | — |
| Thời gian vắng | Trần 3 giờ, kể chuyện, ×2 một lần/ngày (#69) | Đủ | — |
| Quay lại sau thời gian nghỉ (Welcome Back, We Missed You) | Không có | Chưa có | #108 |
| Quảng cáo thưởng | Seam và bản giả, chưa có SDK (#66) | Mỏng | Không tạo: #66 đang mở, SDK do chủ project chọn |
| Gói mua | 3 gói cà rốt vàng + vé Bỏ quảng cáo (#9) | Mỏng | #105 |
| Thẻ tháng | Không có | Chưa có | #106 |
| Bạn bè, thăm vườn | Không có | Chưa có | Không tạo: cần máy chủ (xem cuối) |
| Hộp thư, mã quà | Có (#68) | Đủ | — |
| Thông báo nhắc | Có, chờ bản mobile (#65) | Đủ | — |
| Xin đánh giá trên cửa hàng | Không có | Chưa có | #109 |
| Cảm giác chạm, rung | Có (#29, #28) | Đủ | — |
| Âm thanh, ASMR trạm, chọn nhạc | Có (#63), chờ nghe trên máy thật | Đủ | — |
| Lưu đám mây | Lõi có (#38), nền tảng chờ chủ project (#39, #40) | Đủ (phần code) | — |
| Prestige | Mùa trăng mới (#1, #98); Cats & Soup không có | Mình có thêm | — |
| Analytics, báo lỗi văng | Seam (#100) | Đủ (phần code) | — |

## Issue đã tạo (nhãn enhancement, cats-and-soup, vong-2)

Xếp theo ưu tiên: 1 là cần nhất cho ngày 1–7 của người chơi và cho doanh thu theo lộ trình.

| Ưu tiên | Issue | Việc | Cỡ | Tranh |
|---|---|---|---|---|
| 1 | #102 | Tuần đầu của người mới: 7 ngày × 4 việc, ngày 7 thỏ bậc Tím | M | không |
| 2 | #103 | Hộp quà khi xong nhiệm vụ ngày, thanh điểm tuần 3 rương | S–M | rương sau |
| 3 | #104 | Thẻ giới thiệu khi mở tính năng, nút "Thử ngay" | S–M | không |
| 4 | #105 | Gói người mới một lần, gói mốc cấp 10/20/30 (mở rộng #9) | M | khung gói sau |
| 5 | #106 | Vé trăng 30 ngày: 100 ngay + 15/ngày qua hộp thư (mở rộng #9) | M | vé sau |
| 6 | #107 | Vé nhiệm vụ tháng 30 nấc, hàng miễn phí và trả tiền (cần #103) | M–L | món nấc 30 sau |
| 7 | #108 | Vườn nhớ bạn: vắng từ 3 ngày, quà và điểm danh 7 ngày (mở rộng #69) | S–M | không |
| 8 | #109 | Xin đánh giá đúng lúc qua seam (bản thật sau #41) | S | không |
| 9 | #110 | Sổ công thức: lá công thức từ thỏ nghỉ, 3 món mỗi trạm (mở rộng #49, #60) | L | 32 icon món sau |
| 10 | #111 | Thành tựu nhiều nấc: 12 chuỗi × 8 nấc | M | không |
| 11 | #112 | Lễ hội nhỏ mỗi tháng không có mùa lễ (mở rộng #8) | M–L | hình hội sau |
| 12 | #113 | Rương xe đẩy hai khung giờ, món mùa cũ | S–M | xe rương sau |
| 13 | #114 | Bể cá cạnh ao, tới +12 % thu nhập (mở rộng #58) | S | bể sau |

Mỗi issue có các mục: Người chơi thấy gì, Số liệu mặc định, Tiêu chí nghiệm thu (test Core, `EconomySimulationTests` 17 seed, ảnh trong `docs/anh/po-NN/`, sự kiện analytics qua #100), Phần cần tranh (bản code-only trước), Không nằm trong.

Thứ tự làm gợi ý cho B: #102, #103, #104 song song (ba vùng file khác nhau, chỉ chung `GameState`/`SaveCodec`/`L10nStrings`); rồi #105 và #106 cùng một agent (chung `Storefront`); #107 sau #103. #110 lớn nhất, nên tách a/b.

Những món có giá (#105, #106, #107) chỉ đặt giá tham khảo trong `FakeStorefront`; giá thật và khai sản phẩm vẫn thuộc #9 (cần-chủ-project).

## Có trong bảng nhưng không tạo issue

- Trang phục nhiều bộ cho thỏ: cần tranh thỏ, pipeline 2D của chủ project đang làm.
- Mở rộng nội thất và điểm ấm cúng: #11 nằm trong mục "Tạm gác tới khi có số liệu" của lộ trình.
- Quảng cáo thưởng thật: #66 đang mở, chọn SDK là việc của chủ project.
- Bóng bay quà mỗi 3 phút: game đã có thỏ xin chụp ảnh mỗi 4 phút, khách mỗi 4 phút và Thỏ Ếch; thêm một đồng hồ nữa làm màn hình rối.
- Máy gắp (claw machine): là gacha thêm một lớp; đã có Gọi vàng.
- Phòng mini riêng cho từng thỏ: quá lớn cho bản đầu, cần nhiều tranh.

## Ba mục lớn nhất còn thiếu, cần chủ project quyết

1. **Bạn bè và thăm vườn.** Cats & Soup có bạn bè, thăm rừng và tặng quà. Làm thật cần máy chủ (tài khoản, danh sách bạn, quà gửi qua lại, chống gian lận) và chi phí chạy hằng tháng. Cần quyết: có làm mạng xã hội cho bản đầu không, và dùng dịch vụ nào (Unity Gaming Services, Firebase, PlayFab).
2. **Cấu hình và sự kiện từ xa (live-ops).** Lễ hội #112, vé tháng #107, giá và ưu đãi #105 đều đang viết cứng trong bản build; muốn đổi số hay mở sự kiện mới mà không ra bản cập nhật thì cần Remote Config và lịch sự kiện trên máy chủ. Cần quyết: dịch vụ nào, ai soạn sự kiện hằng tháng.
3. **Mức kiếm tiền tổng thể: quảng cáo dày tới đâu, có đăng ký tự gia hạn không.** Cats & Soup bị chê vì mời xem quảng cáo quá dày. #66 mới có ba chỗ; #106 cố ý làm thẻ tháng mua một lần thay vì đăng ký tự gia hạn. Cần quyết: số lần quảng cáo tối đa mỗi ngày, có bán đăng ký (cần thêm điều khoản trên cửa hàng) không, và giá thật của mọi gói.

## Nguồn

- Cats & Soup fandom wiki: Daily missions, Facilities, Special Events, Friends, Little Trunk Shop (`catsandsoupgame.fandom.com`, `cats-soup.fandom.com`).
- Cats & Soup miraheze wiki: Facilities, Recipes, Quests (`catsandsoup.miraheze.org`).
- Thông báo cập nhật của nhà phát hành (`nwzrise.oqupie.com`): Daily Missions Pass, Premium Pass, mở rộng rừng.
- Hướng dẫn: levelwinner.com, playoholic.com, gamezebo.com (danh sách nội thất).

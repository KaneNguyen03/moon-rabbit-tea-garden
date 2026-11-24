# Cats & Soup, vòng 3: lỗi QA còn lại, cảm giác chơi và nợ kỹ thuật

Viết 24/11/2026 tối trên `main` `046e8a1` (verify đầy đủ GREEN 1595/1596, bản Mac sạch). Vòng 1 là `#50–#69`, vòng 2 là `#102–#114` ([nghien-cuu-cats-and-soup-vong-2.md](nghien-cuu-cats-and-soup-vong-2.md)). Vòng này không thêm hệ thống lớn: gom lỗi và ghi chú QA chưa thành issue, phần cảm giác chơi vòng 2 bỏ sót, và nợ kỹ thuật đáng một issue. Thứ tự theo [LO-TRINH-KIEM-TIEN.md](phat-hanh/LO-TRINH-KIEM-TIEN.md): ngày 1–7 và doanh thu trước.

## Đã xem gì

- Comment của mọi issue từ 23/11 trưa tới 24/11 tối (186 comment, 95 là nghiệm thu QA), gồm 17 issue đóng ngày 24/11 (#8, #28, #88, #97, #102–#114) và các issue đóng tối 23/11 (#1–#69) có comment QA 24/11.
- `docs/bao-cao-po.md` (bàn giao 24/11 và cập nhật 24/11 chiều), `tools/conventions.baseline`, ghi chú các session QA trong thư mục ghi chú chung, báo cáo nhịp tuần 2–6 (`/tmp/econ-long/BAO-CAO.md`).
- Mã: các màn vòng 2 (`*View.cs`, `GardenScreen.*.cs`), `Sfx.cs`, `Haptics.cs`, `AccessibilityTests.cs`, `Intros.cs`, `Analytics.cs`, `GameRunner.WatchAd`, `SeasonTests.cs`, `Fixture.cs`.
- Không chạy Unity, không chơi thử (phiên không có quyền Trợ năng).

## Nguồn 1: lỗi và ghi chú QA chưa thành issue

| Mục | Ghi ở | Xử lý |
|---|---|---|
| Chỗ trang trí trống không giữ ô, thả trạm vào thì dấu "+" đè trạm | #11 (QA 24/11) | #119 |
| Giỏ hứng lá ở ô nhà của trạm Giỏ lá không được giữ khi dời trạm (nghi) | #10 | #119 |
| Mái tiệm trà chồm hai ô phía sau chưa giữ chỗ | #54 | #119 |
| Ấm trà chỉ dời, không cất được | #54 | Chờ chủ project (giữ như nay) |
| Vòng nét đứt trên ô sát, save cũ đã chồng, đổi chỗ hai trạm vốn sát | #88 | Chờ chủ project |
| Biển chưa viền mực; nhãn ô đất "Mở rộng vườn" bị cắt mép; viên ▼ che tiệm trà | #91 (máy chấm 91b) | Đã sửa ở 91c (`bao-cao-po.md`), không tạo |
| Viên ◀ ▶ đè bảng "Cấp"; bảng trạm nửa dưới HUD khi kéo hết lên; cây mép thế giới dưới thanh tab | #91 "Chưa làm", #50 | #120 |
| Góc sương vuông và sương hai bên chiếm nhiều khung nhìn xa khi chưa mở đoạn nào | #51, #91 | #120 |
| Bong bóng thỏ nói bị cắt mép trái khung nhìn | #59, #16 | #120 |
| Bảng "Còn thiếu …" của biển có thể bị cắt mép sau khi mở rộng | `bao-cao-po.md` (ngang-nits) | #120 |
| Lề nhìn 360/600, cách viên ▼ né tiệm trà | #91 | Chờ chủ project |
| Cảnh báo ComputeBuffer cuối `Player.log` khi thoát | #65 | #125 |
| `WatchAd` với callback null không trả thưởng | `bao-cao-po.md` (việc nhỏ đã biết) | #125 |
| Rung không chạy trên iOS (`Haptics.cs` chỉ Android) | #29 | #116 |
| ×2 màn đi vắng: số bay lên là báo cáo cũ khi có lần vắng khác được trả lúc đang xem | #89 | #118 |
| Còn ~10–16 KB/khung từ chữ số UI; chưa đo lại trong bản build sau #97 | #97 | #123 |
| Thiếu thẻ giới thiệu riêng cho bể cá | #114 | #122 |
| Thiếu analytics cho thẻ giới thiệu và thả cá | #104, #114 | #117 |
| Câu `shop.earn` "mỗi 5 cấp vườn" chưa nói "lần đầu" | #98 | #128 |
| 33 key chữ không dùng | #96 | #128 |
| Tên thỏ tiếng Anh | #96 | Chờ chủ project |
| Test mùa luôn Inconclusive (fixture không có trạm Brewer) | mọi lần verify | #126 |
| Test Editor ghi save.json vào thư mục save của bản Mac | ghi chú vòng 2, QA-A | #126 |
| PreviewShot dựng chữ en trong khung vi | #57, #69 | #126 |
| Tên test cũ "Pay_Clovers" | #37 | #126 |
| Tài liệu tả khách vào từ mép trái, code vào từ phải | #20 | #126 |
| Thỏ đang làm không đi dạo lúc nghỉ tay | #55 | #127 |
| Thỏ hiếm chỉ ghé tiệm ban đêm | #21 (để sau) | Không tạo: nhỏ, để sau vòng 3 |
| Mũ ếch chưa vẽ tay, chưa có ảnh Thỏ Ếch mưa đêm | #62 | Không tạo: tranh, pipeline của chủ project |
| 32 icon món, băng rôn hội, hình bể/rương/vé | #110, #112, #113, #114 | Không tạo: tranh để sau |
| Ảnh `104-the-thu-ech-chu-to.png` chỉ có băng lên cấp | #104 | Không tạo: ảnh minh chứng, không phải lỗi game |
| Harness QA thiếu đọc chuỗi nấc `C` (#111) | QA-D | Không tạo: harness ngoài repo |
| Người mua vé trăng phút đầu lên cấp 10 sau 21 phút (115 vàng ngày 1); 24 lần gọi vàng tuần đầu | #106 | Chờ chủ project (#9) |
| Quà Tuần đầu, hộp quà ngày, thành tựu nấc nhỏ hơn issue để giữ luật gọi vàng | #102, #103, #111 | Chờ chủ project |
| Vị trí thẻ Vé tháng, màu đèn/khung, thỏ nấc 30, quà nấc 2 phút | #107 | Chờ chủ project |
| Luật xin đánh giá (120 giây, mua hỏng, chỗ mục ★) | #109 | Chờ chủ project |
| Sổ công thức mở ở cấp 30, +11,1 %/cấp món, nới test nhịp tuần 2–6 | #110 | Chờ chủ project |
| Thỏ hội cần câu cá và trả lời thỏ xin ảnh | #112 | Chờ chủ project |
| Rương xe đẩy bán màu đèn Vé tháng tháng 8 (trước khi có Vé tháng) | #113 | Chờ chủ project |
| Mốc bể cá +0,25/+0,25/+0,5/+1 %, +12 % cần ~1.222 ngày | #114 | Chờ chủ project |
| Thẻ giới thiệu tự thành "Để sau" khi mở bảng khác | #104 | Chờ chủ project |
| Hũ trăng từ nhiệm vụ và Thỏ Ếch từ cấp vườn 15 | #64, #62 | Chờ chủ project |
| Nhịp tim (tim 10 ở 18.880 điểm) | #19, #58 | Chờ chủ project |
| Trạm mở 16/18/20/23, nhiệm vụ chỉ thưởng cỏ | #67 | Chờ chủ project |
| Trần 500 chỉ 2–3 trạm rẻ chạm tới | #16 (nhịp tuần 2–6) | Chờ chủ project |
| Các con số #53, #68, #83, #86, #90, #92–#96, #98, #99, #101 | `bao-cao-po.md` bảng "Con số chờ chủ project quyết" | Chờ chủ project |

## Nguồn 2: dòng "Mỏng" còn lại và cảm giác chơi vòng 2 bỏ sót

| Mục | Hiện trạng | Xử lý |
|---|---|---|
| Trang phục và phụ kiện (Mỏng) | Cần tranh thỏ | Không tạo: pipeline rig của chủ project |
| Nội thất, trang trí vườn (Mỏng) | #11 trong "Tạm gác tới khi có số liệu" của lộ trình | Không tạo |
| Quảng cáo thưởng (Mỏng) | #66 cần-chủ-project | Không tạo (trùng #66) |
| Bạn bè, live-ops, độ dày quảng cáo/đăng ký | Ba mục lớn của vòng 2 | Không tạo: chờ chủ project quyết |
| Phản hồi khi nhận quà ở vé, rương, bể cá, hội, sổ công thức | Các `*View.cs` vòng 2: 0 `Haptics`, 0 `Fly`/`Burst`; vài partial có `Fly`/`FloatText` | #116 |
| Onboarding ngày 1: 60 giây đầu, mốc 5/15/30 phút | Chưa test nào đo khoảng trống giữa các khoảnh khắc; thẻ vòng 2 chưa kiểm dồn nhau | #115 |
| Nhịp mở khoá tuần 2–4 | `Intros.All` chỉ có 10 thẻ vòng 1; chưa đo lại tuần 2–4 với đủ tính năng vòng 2 | #122 |
| Màn đi vắng | Chỉ một tiếng `Reward`, không đếm, không bay, không rung | #118 |
| Tiếng và nhạc nền theo mùa | Ba bài theo giờ (Moon, Dawn, Rain); không gì theo mùa lễ hay hội | #124 |
| Trợ năng phủ tính năng mới | Test chữ tràn/chạm/tương phản chỉ đi qua vườn, tab, Tiệm, ao, tạm dừng | #121 |
| Hiệu năng vườn đầy với mọi tính năng vòng 2 | Chưa đo; còn rác từ chữ số UI | #123 |
| Đo phễu tính năng vòng 2 | Thiếu sự kiện cho thẻ giới thiệu, bể cá, sổ công thức, hội, rương tuần, thành tựu nấc | #117 |

## Nguồn 3: chất lượng mã và bàn giao

| Khoản nợ | Số | Xử lý |
|---|---|---|
| Comment trong baseline | 1.272 (`GardenScreen.cs` 961, 8 tệp khác 311) | #128 (8 tệp trước; `GardenScreen.cs` sau khi nhánh `dotween-gardenscreen` gộp) |
| Toán hoạt ảnh trong baseline | ~250 (`GardenScreen.cs` 117, `Sfx.cs` 49) | Không tạo: nhánh `dotween-migrate`/`dotween-gardenscreen` của chủ project đang hạ |
| `Update()` mỗi khung | 3 (`GameRunner`, `GardenScreen`, `PressDip`) | Không tạo: `GameRunner` là nhịp mô phỏng; hai chỗ kia thuộc nhánh DOTween |
| Tìm đối tượng trong scene | 1 (`GardenScreen.cs`) | Không tạo: gộp vào lượt GardenScreen của #128 khi tới |
| Test Inconclusive `SeasonTests` | 1 mỗi lần verify | #126 |
| Cảnh báo ComputeBuffer khi thoát | 1 dòng log | #125 |
| Test ghi đè save người chơi, PreviewShot sai ngôn ngữ, tên test cũ, tài liệu lệch | — | #126 |
| `WatchAd` callback null | — | #125 |

## Issue đã tạo (nhãn cats-and-soup, vong-3; bug hoặc enhancement)

Ưu tiên 1 là ảnh hưởng ngày 1–7 và doanh thu nhất.

| Ưu tiên | Issue | Việc | Nhãn | Cỡ | Tranh |
|---|---|---|---|---|---|
| 1 | #115 | Ngày đầu không quãng trống: 60 giây đầu, mốc 5/15/30 phút, thẻ không dồn | enhancement | M | không |
| 2 | #116 | Tiếng, rung, đồ bay, rương bung ở mọi chỗ nhận quà vòng 2; rung iOS | enhancement | M | không |
| 3 | #117 | Đo phễu tính năng vòng 2 (analytics) | enhancement | S–M | không |
| 4 | #118 | Màn đi vắng: đếm lên, bay vào ví, trần vé trăng, ×2 đúng số | enhancement | S | không |
| 5 | #119 | Bố trí: chỗ trang trí trống, giỏ lá, mái tiệm trà giữ ô | bug | S–M | không |
| 6 | #120 | Mép khung nhìn: viên ◀ ▶, HUD, thanh tab, bong bóng, góc sương | bug | M | không |
| 7 | #121 | Chữ to, chạm, Giảm chuyển động phủ thẻ vòng 2 | enhancement | M | không |
| 8 | #122 | Tuần 2–4 cứ 2 ngày có thứ mới, thẻ giới thiệu tính năng vòng 2 | enhancement | M | không |
| 9 | #123 | Vườn đầy vòng 2: 0 byte/khung, đo lại trong bản build | enhancement | S–M | không |
| 10 | #124 | Nhạc nền mùa lễ, tiếng chợ khi có hội | enhancement | S–M | âm thanh thật sau |
| 11 | #125 | `WatchAd` callback null; ComputeBuffer khi thoát | bug | S | không |
| 12 | #126 | Test mùa Inconclusive, test ghi đè save, preview lẫn ngôn ngữ | bug | S–M | không |
| 13 | #127 | Thỏ đang làm nghỉ tay đi dạo | enhancement | S–M | không |
| 14 | #128 | Dọn comment theo baseline, 33 key thừa, câu `shop.earn` | enhancement | S (M nếu có GardenScreen) | không |

Thứ tự làm gợi ý cho B:
- Làm song song được (vùng file khác nhau): #115 (mô phỏng, `Newcomer`/`Intros`/`Offers`), #117 (`Analytics` và Core), #125, #126.
- #116, #118 và #121 cùng chạm các `*View.cs` vòng 2: một agent, hoặc lần lượt.
- #119 và #120 cùng chạm `GardenScreen.cs` phần vườn: lần lượt.
- #122 sau #117 (dùng sự kiện `intro_*`). #123 sau #116 (đo khi đã có bay/bung).
- #128 phần `GardenScreen.cs` chỉ sau khi nhánh DOTween của chủ project gộp.

Mỗi issue ghi luật `tools/conventions.py` và không đụng số kinh tế đang chờ chủ project.

## Chờ chủ project quyết (không tạo issue)

- Số kinh tế vòng 2: #106 nhịp cấp 10 của người mua vé phút đầu (115 vàng ngày 1, thuộc #9); #102, #103, #111 quà nhỏ hơn issue; #110 cổng cấp 30 và hệ số món; #112 điều kiện thỏ hội; #113 màu đèn tháng chưa có Vé tháng; #114 mốc bể cá.
- Lựa chọn vòng 2: #104 thẻ tự thành "Để sau"; #107 vị trí thẻ, màu, thỏ nấc 30; #109 luật xin đánh giá.
- Từ trước: #88 ba lựa chọn Bố trí; #54 cất ấm trà; #91 lề nhìn và viên ▼; #96 tên thỏ tiếng Anh; bảng "Con số chờ chủ project quyết" trong `bao-cao-po.md`.
- Ba mục lớn của vòng 2 (bạn bè, live-ops, độ dày quảng cáo và đăng ký) vẫn chờ.

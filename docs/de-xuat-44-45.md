# Đề xuất thiết kế cho #44 (Cà rốt vàng và Tiệm) và #45 (Giã bánh trăng)

Viết 22/11/2026 để chủ project duyệt trước khi code. Mỗi mục có một lựa chọn đề xuất (in đậm) và các con số bám theo luật đang có trong Core. Số nào chủ project đổi thì chỉ đổi hằng số, không đổi cấu trúc.

Số liệu đang có để đối chiếu: gọi thỏ tốn 1 cỏ bốn lá, tỉ lệ bậc 60 / 25 / 10 / 4 / 0,9 / 0,1 %; cỏ đến từ mỗi cấp vườn (1), nhiệm vụ, món vàng ở khay (1, tối đa 2 một ngày), ảnh đầu ngày (1), liên kết tài khoản (3). Người chơi thong thả có khoảng 22 thỏ sau 20 phút và 30 thỏ cuối ngày 1 (mô phỏng `GardenPacingTests`). Thưởng nhiệm vụ ngày bằng 600 giây thu nhập; món ở khay bằng 15 giây.

Hình đã có trong `docs/anh/po-do-vat/`: `prop_tiem.png` (quầy tiệm), `prop_coi-gia-banh.png` (cối và chày), `icon_ca-rot-vang.png`, `icon_hop-qua.png`.

---

## #44 Cà rốt vàng và Tiệm

### 1. Cà rốt vàng là gì

- Loại tiền thứ ba, số nguyên, lưu trong save (`GoldenCarrots`), hiện trên HUD cạnh cỏ bốn lá chỉ khi đã có ít nhất một củ (người mới không thấy thêm một ô lạ).
- Không mua bằng tiền thật ở bản chơi thử (#9 để sau). Không đổi được ra cà rốt thường và ngược lại, để hai nền kinh tế không kéo nhau.

### 2. Kiếm cà rốt vàng không cần mua

| Nguồn | Số | Trần | Ghi chú |
|---|---|---|---|
| Quà mỗi ngày (chuỗi) | ngày 1–2: 1, ngày 3–4: 2, ngày 5–6: 3, ngày 7: 5, rồi lặp từ ngày 5 (3, 3, 5) | 1 lần/ngày | Bỏ một ngày thì chuỗi về ngày 1. Tuần đầu 17 củ, tuần sau 25 củ nếu không bỏ ngày |
| Món vàng ở khay (#37) | 1 củ thay cho 1 cỏ bốn lá hiện nay | 2/ngày (đã có `GoldenPerDay`) | Giữ nguyên xác suất 4 % |
| Ảnh đầu ngày (#18) | 1 củ thay cho 1 cỏ | 1/ngày | #18 đã ghi "quà bằng cà rốt vàng" |
| Mốc cấp vườn | 3 củ ở mỗi cấp chia hết cho 5 (5, 10, 15 …) | | Cấp 5 ở phút 10, cấp 10 cuối ngày 1 |
| Thành tựu một lần | 5 củ: loài đầu tiên bậc Tím, Vàng, Đỏ; mở đủ 12 ô; thỏ cấp tối đa đầu tiên | | Nối vào bảng nhiệm vụ đang có |

Người chơi chăm mỗi ngày được khoảng 3 (quà) + 2 (khay) + 1 (ảnh) = 6 củ, một tuần khoảng 40 củ kể cả mốc. Người thong thả khoảng 20 củ một tuần. Giá ở mục 3 đặt theo con số này.

Cỏ bốn lá bớt hai nguồn (khay, ảnh) nên gọi thỏ bằng cỏ chậm đi một chút; mô phỏng cho thấy số thỏ đang vượt xa ngưỡng mở đất (14 thỏ ở phút 10 so với ngưỡng 6), nên không chặn ai. Chạy lại `GardenPacingTests` khi làm để chắc.

### 3. Tiêu cà rốt vàng (giá)

| Thứ | Giá | Ghi chú |
|---|---|---|
| **Gọi vàng** (gọi thỏ bằng cà rốt vàng) | 10 củ | Tỉ lệ bậc 25 / 35 / 22 / 12 / 5 / 1 %, công bố ngay trên màn gọi, cạnh tỉ lệ gọi bằng cỏ. Khoảng 2 ngày chơi chăm cho một lần gọi vàng |
| **Thỏ ghé tiệm** | Xanh dương 15, Tím 40, Vàng 120, Đỏ 400 | Mỗi ngày một bé ghé lúc 0h, ở 24 giờ; bậc rút theo 60 / 30 / 9 / 1 % (Xanh dương / Tím / Vàng / Đỏ); loài rút trong bậc, ưu tiên loài chưa gặp (gấp 3 trọng số). Người chăm mua được thỏ Tím mỗi tuần, thỏ Vàng mỗi tháng |
| Mũ và phụ kiện (chỉ để nhìn) | 20–60 củ, 6 món đầu | Không cộng hệ số, để không thành pay-to-win khi mở #9 |
| Nhuộm lông | 8 củ một lần, bảng 8 màu | Nhuộm phủ lên hình loài bằng tint như cách tạo 30 biến thể; đổi lại được, lưu theo con thỏ |

### 4. Quầy tiệm trong vườn

- Một đạo cụ `prop_tiem.png` đứng cố định ở mép ao, có từ đầu (không cần mở), chạm mở tấm trượt (theo #22) ba mục: Hôm nay (quà ngày, thỏ ghé), Gọi vàng, Đồ cho thỏ (mũ, nhuộm).
- Huy hiệu trên quầy khi có quà chưa nhận hoặc thỏ ghé chưa xem; một chấm trên tab Thỏ không cần vì quầy đã nhìn thấy trong vườn.
- Chỗ cho mục thứ năm trên thanh tab (#23) để dành cho Sự kiện (#8), không dùng cho Tiệm.

### 5. Thứ tự làm (ba lượt, mỗi lượt một commit, test Core trước)

1. **Lượt 1:** cà rốt vàng trong save (v13), quà ngày với chuỗi, Gọi vàng, chuyển món vàng và ảnh đầu ngày sang cà rốt vàng, mốc cấp vườn. Quầy tiệm với mục Hôm nay và Gọi vàng. Test: chuỗi ngày (kể cả bỏ ngày, lùi đồng hồ không được nhận hai lần), trần ngày, tỉ lệ gọi vàng cộng bằng 1, save cũ có 0 củ.
2. **Lượt 2:** thỏ ghé tiệm: rút bé thỏ theo ngày, hết hạn, mua; màn gọi hiện tỉ lệ cả hai cách gọi. Test: cùng một ngày rút ra cùng một bé (hạt giống theo ngày và mã người chơi), hết hạn thì không mua được, mua thì tiêu đúng giá.
3. **Lượt 3:** mũ, phụ kiện, nhuộm. Cần vẽ 6 món (quy trình Gemini trong `docs/anh/po-do-vat/cat-tam-do-vat.py`).

### 6. Chủ project cần chốt

- [ ] Giá và nguồn ở mục 2 và 3 (hoặc chỉ ghi "giữ như đề xuất").
- [ ] Món vàng và ảnh đầu ngày đổi từ cỏ sang cà rốt vàng: đồng ý hay giữ cỏ.
- [ ] Mũ chỉ để nhìn (đề xuất) hay có cộng hệ số nhỏ.
- [ ] Làm cả ba lượt trước bản chơi thử, hay chỉ lượt 1 và 2.

---

## #45 Giã bánh trăng

### 1. Cách chơi: **bấm theo nhịp**

- Một ván 25 giây. Chày giã lên xuống theo nhịp đều; một vòng tròn thu nhỏ về cái bánh, chạm đúng lúc vòng chạm bánh.
- Nhịp bắt đầu 1 nhịp/giây, sau mỗi 5 nhịp nhanh thêm 10 %, tới nhịp thứ 30 là khoảng 1,6 nhịp/giây. Một ván có 30 nhịp.
- Chấm điểm theo độ lệch thời gian: Chuẩn (±80 ms) 3 điểm, Được (±160 ms) 1 điểm, lệch hơn hoặc không chạm là Méo (0 điểm, bánh lõm một bên, đứt chuỗi). Năm Chuẩn liên tiếp thì bánh hiện chữ "Dẻo!" và các nhịp sau trong chuỗi được 4 điểm.
- Tối đa 120 điểm. Xếp sao: 1 sao từ 40, 2 sao từ 75, 3 sao từ 105.
- Chọn "bấm theo nhịp" vì phù hợp cối giã và chơi được bằng một ngón, chơi không cần nhìn chữ; "chạm nhanh" thì đụng với việc chạm thỏ liên tục (#35), "giữ và thả" khó cảm nhận trên điện thoại.

### 2. Mở từ đâu

- Đạo cụ cối giã (`prop_coi-gia-banh.png`) đứng cố định cạnh ấm trà, hiện từ cấp vườn 3 (phút thứ 4 của người thong thả, sau khi hướng dẫn 60 giây xong). Chạm cối để chơi. Không nút HUD, không qua Tiệm.
- Khi đã có Tiệm bánh trung thu (trạm thứ 10), cối không đổi chỗ.

### 3. Lượt chơi và phần thưởng

- 3 lượt mỗi ngày, làm mới lúc 0h cùng nhiệm vụ ngày (dùng `DailyDay` đang có). Hết lượt thì cối ghi "Mai giã tiếp".
- Thưởng cà rốt = thu nhập mỗi giây × 60 giây × (điểm / 120), tính theo cùng công thức thưởng nhiệm vụ (đã gồm hệ số thỏ và bùa). Ba lượt một ngày tối đa bằng 180 giây thu nhập, tức chưa bằng một phần ba thưởng nhiệm vụ ngày (600 giây): đủ để đáng chơi, không làm lệch kinh tế.
- Lượt đầu trong ngày đạt từ 1 sao: thêm 1 cỏ bốn lá. Ván 3 sao đầu tiên trong ngày: thêm 1 cà rốt vàng (chỉ khi #44 đã có; trước đó là 1 cỏ).
- Thành tựu: "Giã bánh 3 sao lần đầu", "Giã 30 ván".

### 4. Thỏ có tham gia

- Nếu trong vườn có ít nhất một thỏ nghề Bếp bánh đang làm ở trạm, một bé (chọn bé cấp cao nhất) hiện bên cối, giã cùng theo nhịp, và thưởng cà rốt nhân 1,5 (cùng con số với thưởng nghề đang có). Không thêm luật nào khác.
- Thỏ đang làm ở Tiệm bánh trung thu không có thưởng riêng, để luật ngắn.

### 5. Luật nằm trong Core, có test

- Lịch nhịp là hàm xác định từ chỉ số nhịp (không phụ thuộc khung hình); đầu vào là thời điểm chạm, đầu ra là Chuẩn / Được / Méo.
- Test: lịch nhịp đúng 30 nhịp trong 25 giây; ba cửa chấm điểm; chuỗi Dẻo bắt đầu và đứt đúng lúc; điểm tối đa 120; thưởng đúng công thức; 3 lượt một ngày và làm mới; cỏ chỉ ở lượt đầu; lùi đồng hồ không thêm lượt.
- Màn hình: tắt rung lắc khi bật Giảm chuyển động; chạm ở đâu trên màn cũng tính (không cần chạm trúng cối).

### 6. Hình cần vẽ

- Chày tách khỏi cối (cắt từ `prop_coi-gia-banh.png` nếu được) để chày chuyển động riêng.
- Bánh trăng ba trạng thái: bột, đang giã, bánh xong; thêm một bánh méo.
- Vòng nhịp và chữ "Dẻo!" vẽ bằng code.

### 7. Chủ project cần chốt

- [ ] Bấm theo nhịp (đề xuất) hay cách khác.
- [ ] 3 lượt một ngày, thưởng 60 giây thu nhập mỗi ván.
- [ ] Mở từ cối cạnh ấm trà ở cấp vườn 3.
- [ ] Thỏ Bếp bánh giã cùng, nhân 1,5.

# Issue trên GitHub (bản chép cho session code)

Chép nguyên văn từ KaneNguyen03/moon-rabbit-tea-garden lúc 2026-11-22 04:13. Nguồn chính vẫn là GitHub; file này do session PO cập nhật.

## #1 Mùa trăng mới: làm lại vườn để lấy hệ số vĩnh viễn

Nhãn: enhancement, question

**Trạng thái:** chờ chủ project duyệt đề xuất, chưa code.

Khi cả vườn chạm trần cấp, người chơi có thể bắt đầu một mùa mới: làm lại vườn từ đầu, giữ thỏ và vật phẩm, đổi lấy một hệ số nhân vĩnh viễn.

**Cần quyết trước khi làm**
- Điều kiện mở (mọi trạm cấp 500, hay một ngưỡng cấp vườn).
- Giữ gì, mất gì (cà rốt, cấp trạm, ô đất, chỗ nghỉ, cà rốt vàng).
- Công thức phần thưởng.
- Ảnh hưởng tới save.

Session code sẽ viết đề xuất ngắn vào `docs/bao-cao-po.md` khi tới lượt.

---

## #2 Thỏ sưu tầm chưa có hoạt ảnh làm việc theo nghề

Nhãn: enhancement

Thỏ sưu tầm đứng ở trạm hiện chỉ nhún và lắc, không có động tác nghề (khuấy trà, nhào bánh, hái lá, ngắm sao). Chỉ con thỏ mặc định của Ấm trà có vòng hoạt ảnh vẽ tay.

**Hướng làm:** vẽ 3–4 khung cho mỗi nghề trên một dáng thỏ chuẩn, hoặc một lớp đạo cụ cầm tay vẽ chồng lên mọi loài (muỗng, cây cán bột, giỏ, kính viễn vọng) kèm chuyển động tay bằng code.

**Xong khi:** thỏ ở trạm nhìn là biết đang làm nghề gì, ở cả 72 loài.

---

## #3 Thỏ ở chỗ nghỉ chưa có dáng ngồi hoặc nằm

Nhãn: enhancement

Thỏ đang nghỉ ở võng, thảm picnic, ghế nấm, xích đu vẫn dùng hình đứng của loài, chỉ lắc lư và hiện "z z". Thỏ ngồi cũng chưa hiện mũ hay tên.

**Hướng làm:** vẽ một dáng ngồi và một dáng nằm cho các dáng thân chính, hoặc tạm xoay và ép hình đứng bằng code cho võng.

**Xong khi:** thỏ ở võng trông như đang nằm, thỏ ở ghế nấm và xích đu trông như đang ngồi.

---

## #4 Chơi thử tay bản Mac: zoom, cuộn, gọi thỏ, tab Thỏ

Nhãn: question

Nhiều phần mới chỉ được kiểm bằng test tự động và ảnh tĩnh, chưa ai thử tay trên bản build `Build/Mac/VuonTraTho.app`.

**Cần thử**
- [ ] Cmd+cuộn đổi một mức zoom mỗi lần vuốt, không giật.
- [ ] Vuốt hai ngón cuộn dọc vườn.
- [ ] Nút kính lúp xoay đủ ba mức.
- [ ] Chạm đúp vào trạm bay tới mức gần.
- [ ] Ở mức xa, bấm trạm thì về mức mặc định rồi mới mở tấm trạm.
- [ ] Khoảnh khắc gọi thỏ: cổng trăng, bóng bước ra, lộ hình, chạm để bỏ qua.
- [ ] Tab Thỏ: lưới, tấm chi tiết, đổi tên, lên cấp, xếp trạm, sổ thỏ.
- [ ] Thỏ đang nghỉ ngồi đúng chỗ, chạm mở được tấm chi tiết.

Ghi lại lỗi thấy được thành issue riêng.

---

## #5 Chụm hai ngón trên trackpad Mac không zoom được

Nhãn: bug

Bản Mac không nhận cử chỉ chụm của trackpad: Input System không đưa pinch của trackpad thành điểm chạm. Chụm chỉ hoạt động trên màn cảm ứng.

**Hiện có thể zoom bằng:** Cmd/Ctrl+cuộn, nút kính lúp, chạm đúp vào trạm.

**Mức ưu tiên:** thấp, Mac chỉ là máy thử. Làm nếu có bản desktop chính thức (đọc sự kiện magnify gốc của macOS).

---

## #6 Cân lại kinh tế sau khi thêm thưởng nghề ×1,5

Nhãn: enhancement

Thỏ đứng đúng nghề được nhân thêm 1,5. Thu nhập tăng ở mọi save có thỏ đúng nghề, nhưng giá trạm, giá lên cấp thỏ và giá ô đất chưa được chỉnh theo.

Sẽ lệch thêm khi có cà rốt vàng, Tiệm và trò chơi nhỏ.

**Cần làm:** chạy mô phỏng tiến độ ngày đầu và tuần đầu với cả các nguồn thu mới, so với nhịp mong muốn, rồi chỉnh hằng số. Nên làm sau khi có feedback của người chơi thử đầu tiên.

---

## #7 Luật ghép 3 thỏ đi ngược với việc sưu tầm

Nhãn: question

Ghép 3 thỏ cùng bậc thành 1 thỏ bậc trên làm mất ba con thỏ đã đặt tên, và ra một loài ngẫu nhiên của bậc kế. Người thích sưu tầm có thể không muốn mất thỏ.

**Phương án thay thế:** thỏ trùng loài cộng sao cho con đang có; lên bậc bằng sao thay vì ghép.

**Cần:** hỏi thẳng người chơi thử đầu tiên trước khi quyết. Giữ luật hiện tại cho tới lúc đó.

---

## #8 Thỏ sự kiện theo mùa

Nhãn: enhancement

Loài thỏ chỉ có trong một khoảng thời gian (Trung thu, Tết, Giáng sinh), kèm nhiệm vụ và đồ trang trí riêng.

**Phụ thuộc:** Tiệm và cà rốt vàng (để bán hoặc thưởng), ngày giờ chống lùi đồng hồ máy.

**Ghi chú:** hình vẽ bằng Gemini theo quy trình trong `docs/anh/po-loai-tho/30-dang-moi/cat-tam-tho.py`. Thêm loài chỉ cần một dòng trong bảng RabbitKinds và một file hình.

---

## #9 Mua cà rốt vàng bằng tiền thật

Nhãn: enhancement, question

MVP chỉ cho kiếm cà rốt vàng trong game. Gắn mua bằng tiền thật cần:

- Tài khoản nhà phát triển trên App Store và Google Play.
- Bảng giá các gói.
- Công bố tỉ lệ từng bậc ở màn gọi thỏ (đã có trong kế hoạch Tiệm).
- Xem xét quy định về yếu tố may rủi và người chơi nhỏ tuổi ở từng thị trường.
- Khôi phục giao dịch và kiểm tra hoá đơn.

**Cần chủ project quyết** có làm hay không, sau khi biết người chơi có thích tiêu cà rốt vàng.

---

## #10 Hai trò chơi nhỏ còn lại: Hứng lá trà và Đêm đom đóm

Nhãn: enhancement

Trò đầu tiên (Giã bánh trăng) nằm trong hàng đợi MVP. Hai trò này để sau khi biết người chơi có thích trò đầu.

- **Hứng lá trà:** lá rơi từ tán cây, kéo giỏ qua lại để hứng, tránh quả thông. 30 giây. Thưởng chính: cà rốt.
- **Đêm đom đóm:** một đàn đom đóm bay qua vườn, chạm để bắt trong 20 giây. Thưởng chính: một ít cà rốt vàng hoặc cỏ bốn lá, có trần mỗi ngày.

Dùng chung luật lượt chơi, hồi lượt và trần ngày của trò đầu.

---

## #11 Cho người chơi tự trang trí vườn

Nhãn: enhancement

Người chơi mua và tự đặt đồ trang trí (đèn đá, bồn hoa, ghế, lối đi) trong vườn, đổi màu đèn lồng, đổi kiểu ao.

**Đã có hình:** `docs/anh/po-do-vat/decor_den-da.png`, `decor_bon-hoa.png` và bốn món chỗ nghỉ.

**Cần thiết kế:** lưới đặt đồ hay vị trí cố định, lưu vị trí vào save, bán ở kệ nào của Tiệm.

---

## #12 File link.xml của Addressables đang bị theo dõi trong git

Nhãn: bug

`Assets/AddressableAssetsData/link.xml` do Addressables tự sinh khi build rồi tự dọn, nên git lúc thì báo xoá, lúc thì báo sửa. Nó lọt vào repo từ commit Localization (`ba1cd71`).

Mọi checkpoint hiện đều phải loại file này ra bằng tay.

**Đề xuất:** `git rm --cached` file và `.meta` của nó, thêm vào `.gitignore`. Chờ chủ project đồng ý.

---

## #13 Khoảnh khắc gọi thỏ: âm thanh theo bậc, dấu Mới chưa lưu, bậc Bạc nhạt

Nhãn: enhancement

Ba điểm còn thiếu sau khi làm cổng trăng:

- [ ] Chưa có âm thanh riêng cho từng bậc, đang dùng chung một tiếng.
- [ ] Dấu "Mới" trong sổ thỏ chỉ nhớ trong phiên chơi, tắt game mở lại thì mất (loài vẫn nằm trong sổ).
- [x] Bậc Bạc màu xám nhạt nên ánh sáng trong cổng gần như không đổi; cần ánh trắng bạc và hạt lấp lánh (xong ở 0373045).


---

## #14 Hai biến thể màu Thỏ Việt Quất và Thỏ Dâu Tây chưa đúng tên

Nhãn: bug

Trong 30 biến thể màu tạo bằng code, hai con tai cụp dựa trên Thỏ Khoai Môn ra màu chưa khớp tên: Việt Quất ngả tím hồng thay vì xanh tím đậm, Dâu Tây ngả vàng hồng thay vì hồng đỏ.

**Sửa:** chỉnh thông số trong `docs/anh/po-loai-tho/30-bien-the/tao-bien-the.py` rồi xuất lại hai hình, hoặc vẽ lại hai con bằng Gemini.

---

## #15 Đổi tên app và cửa sổ sang Moon Rabbit Tea Garden?

Nhãn: question

Tên tiếng Anh của game đã chốt là "Moon Rabbit Tea Garden" và màn tiêu đề bản tiếng Anh đã dùng tên này. Tên app và cửa sổ (productName) vẫn là "Vườn Trà Thỏ Trăng".

Đổi productName an toàn trên Mac, iOS, Android vì save lưu theo bundle id `com.vuontratho.game`. Trên Windows đường dẫn save theo tên nên sẽ đổi chỗ lưu (chưa có bản Windows).

productName được đặt trong `ContentSeeder.cs`. **Cần chủ project quyết.**

---

## #16 Hàng đợi MVP: việc đang làm và sắp làm

Nhãn: enhancement

Theo dõi các việc trong hàng đợi của bản chơi thử đầu tiên. Mỗi việc xong, test qua và được duyệt thì có một commit checkpoint trên `main`.

**Đã xong**
- [x] Vườn cuộn dọc có tâm điểm, zoom ba mức, HUD một hàng (`cc79c47`)
- [x] Loài thỏ và nghề, thỏ sưu tầm đứng ở trạm, tấm trạm mới (`cc79c47`, `eb5f107`)
- [x] 72 loài thỏ (`49a9a59`, `271b7d6`)
- [x] Tab Thỏ dạng lưới, tấm chi tiết, sổ thỏ (`271b7d6`)
- [x] Chỗ nghỉ cho thỏ (`271b7d6`)
- [x] Khoảnh khắc gọi thỏ với cổng trăng (`5bd32fc`, `0373045`)

**Đang làm và sắp làm, theo thứ tự**
- [x] Cấp vườn và mở rộng đất theo cấp và số thỏ (`feb6c8f`, `7406a41`)
- [x] Sửa lỗi thỏ phình to khi bấm nhanh (#35, `7406a41`; chờ chủ project bấm thử)
- [x] Huy hiệu cấp vườn, sương, hình bùa vẽ tay; bù chỗ nghỉ cho save cũ (`200ebe4`)
- [x] Lượt UI thứ nhất, phần đã xong: nút và chữ (#24, `9b1f1b6`), tấm trượt (#22, `52be412`), thanh tab (#23, `a08ef1f`), chuyển động cơ bản (#28, `a011c73`), phản hồi khi chạm (#29, `16b4867`). Mỗi issue còn mục tồn, xem bình luận trong issue.
- [ ] Trạm trống không hiện thỏ, mỗi trạng thái một lời mời hành động (#43)
- [ ] Lượt UI thứ nhất, phần còn lại: tab Trạm (#25), tab Nhiệm vụ (#27)
- [ ] Nâng cấp nhanh: nâng tối đa, tới mốc, nâng tất cả (#36)
- [ ] Cà rốt vàng và Tiệm (quầy tiệm trong vườn, quà mỗi ngày, gọi thỏ, thỏ ghé tiệm, đồ cho thỏ, nhuộm lông)
- [ ] Khay sản phẩm: chạm món để nhận cà rốt (#37)
- [ ] Trò chơi nhỏ đầu tiên: Giã bánh trăng
- [ ] Chế độ chụp ảnh vườn và quà chụp ảnh mỗi ngày (#18, phần bản Mac)
- [ ] Hồ sơ người chơi và lớp giao tiếp tài khoản, chưa nối dịch vụ (#17, phần làm được ngay)
- [ ] Thang độ hiếm chung cho thỏ và vật phẩm
- [ ] Mốc trạm nhìn thấy được ở cấp 25, 100, 300
- [ ] Sao của thỏ mỗi 10 cấp
- [ ] Trần cấp trạm 500
- [ ] Màn "lúc bạn đi vắng"
- [ ] Lượt làm UI thứ hai: tab Bùa (#26), di chuyển trong vườn (#31), HUD (#32), phần còn lại của #22, #23, #24, #28
- [ ] Hướng dẫn 60 giây đầu (#30)

**Để sau, có issue riêng:** #1, #2, #3, #6, #7, #8, #9, #10, #11, #19, #20, #21, #33, #34.
**Lên cửa hàng (sau bản chơi thử):** #41 bản build iOS và Android → #38 lõi lưu mây → #39 Game Center và iCloud, #40 Play Games và Saved Games → #42 quyền riêng tư.

**Cần chủ project:** #1, #4, #7, #9, #12, #15, #17 (liên kết Unity Cloud), #39 và #40 và #41 (tài khoản nhà phát triển Apple và Google), #42 (duyệt chính sách).









---

## #17 Đăng nhập và lưu tiến độ lên mây

Nhãn: enhancement, question

Người chơi cần giữ được vườn khi đổi máy hoặc cài lại game. Hiện save chỉ nằm trên máy.

**Thiết kế**
- Mở game lần đầu: tự có tài khoản khách, chơi ngay, không hỏi gì.
- Hồ sơ người chơi: tên, thỏ đại diện, mã người chơi, cấp vườn.
- Trong Cài đặt: nút "Liên kết tài khoản" với Apple và Google để sao lưu. Trên iOS, đã cho đăng nhập Google thì phải có cả Apple.
- Save trên máy vẫn là bản chính khi không có mạng. Khi có mạng thì đồng bộ; hai bản lệch nhau thì hỏi người chơi giữ bản nào, kèm cấp vườn và số thỏ của từng bản.
- Thưởng một lần khi liên kết tài khoản.

**Chia hai phần**
- [ ] Phần làm được ngay (trong hàng đợi, việc R10): hồ sơ người chơi lưu trên máy, màn hồ sơ, và lớp giao tiếp tài khoản/lưu mây có bản cài đặt "chỉ trên máy". Không cần dịch vụ ngoài.
- [ ] Phần cần chủ project: project chưa liên kết Unity Cloud (cloudProjectId đang trống). Cần tạo và liên kết project trên Unity Cloud, bật Authentication và Cloud Save, cấu hình đăng nhập Apple và Google (cần tài khoản nhà phát triển), và có trang chính sách quyền riêng tư. Sau đó mới nối bản cài đặt thật vào lớp giao tiếp.

**Ghi chú:** không lưu mật khẩu trong game; mọi đăng nhập đi qua nhà cung cấp.

---

## #18 Chế độ chụp ảnh vườn, chia sẻ và quà chụp ảnh mỗi ngày

Nhãn: enhancement

Người chơi bấm nút máy ảnh để chụp khu vườn của mình, lưu hoặc chia sẻ, và được quà.

**Thiết kế**
- Nút máy ảnh trên HUD. Bấm vào: ẩn toàn bộ HUD và thanh tab, vẫn kéo và zoom được để căn khung.
- Tuỳ chọn trước khi chụp: khung ảnh (viền giấy kem, tên vườn, ngày, logo game ở góc), bật tắt tên thỏ, bật tắt đom đóm.
- Bấm chụp: có hiệu ứng nháy và tiếng màn trập, ảnh lưu thành PNG.
- Sau khi chụp: xem lại, nút Lưu và nút Chia sẻ.
- Quà: lần chụp đầu tiên mỗi ngày được một món quà (cỏ bốn lá hoặc cà rốt vàng). Quà gắn với việc CHỤP, không gắn với việc đã đăng lên mạng: game không kiểm tra được người chơi có đăng thật không, và một số nền tảng mạng xã hội cấm thưởng để đổi lấy lượt chia sẻ.
- Nhiệm vụ ăn theo: "chụp ảnh có một thỏ bậc Vàng", "chụp ảnh khi đủ 6 chỗ nghỉ".
- Album ảnh trong game: giữ 12 ảnh gần nhất.

**Theo nền tảng**
- Mac (bản thử): lưu vào thư mục Pictures và mở Finder tại file. Nút Chia sẻ chép ảnh vào clipboard.
- iOS và Android: cần bảng chia sẻ gốc của hệ điều hành và quyền lưu vào thư viện ảnh. Unity không có sẵn, phải viết plugin gốc nhỏ hoặc dùng một gói bên thứ ba. Chọn gói nào là việc cần chủ project đồng ý khi làm bản mobile.

**Trong hàng đợi:** việc R9 (bản Mac, quà mỗi ngày, khung ảnh, album). Phần chia sẻ gốc trên mobile để lại cho tới khi có bản mobile.

---

## #19 Chạm và vuốt thỏ: tương tác trực tiếp với từng con

Nhãn: enhancement

Hiện chạm vào thỏ ở trạm chỉ làm nó làm nhanh hơn. Ý tưởng đã bàn: thỏ có lúc ngáp, ngủ gật, ngồi uống trà; người chơi chạm để đánh thức hoặc vuốt ve, được thưởng một ít trà lực, thỏ hiện trái tim.

**Mở rộng:** mức thân thiết với từng con (vuốt mỗi ngày tăng dần), mở lời thoại ngắn hoặc một món quà khi đủ thân.

---

## #20 Khách ghé vườn và đơn hàng trà

Nhãn: enhancement

Sự kiện nhỏ xảy ra khi đang mở game, để có việc làm ngoài nâng cấp.

- Một vị khách ghé quầy, đặt một đơn (ví dụ 3 ấm trà và 1 mứt dâu). Giao đủ thì được cà rốt và thỉnh thoảng cỏ bốn lá.
- Đom đóm vàng bay ngang (đã có) là một dạng của nhóm này.
- Nối với nhiệm vụ ngày đang có.

---

## #21 Ngày và đêm theo giờ thật

Nhãn: enhancement

Vườn hiện luôn là đêm trăng, đây là bản sắc của game. Ý tưởng đã bàn: đổi ánh sáng theo giờ thật của máy (hoàng hôn, đêm trăng, rạng sáng) nhưng vẫn giữ đêm là cảnh chính; ban đêm đom đóm nhiều hơn, một số loài thỏ hiếm chỉ ghé tiệm vào ban đêm.

Chỉ là hình ảnh và lịch xuất hiện, không đổi thu nhập.

---

## #22 UI: các tấm Trạm, Bùa, Nhiệm vụ, Thỏ che kín khu vườn

Nhãn: enhancement

Mở bất kỳ tab nào thì một tấm kem to phủ gần hết màn hình, HUD bị xám đi và khu vườn biến mất. Người chơi mất cảm giác đang ở trong vườn.

**Làm lại thành tấm trượt từ dưới lên**
- Mặc định cao khoảng 60% màn hình, vườn vẫn thấy ở phía trên và vẫn chạy.
- Có tay nắm ở đầu tấm: kéo lên để mở hết cỡ, kéo xuống hoặc chạm ra ngoài để đóng.
- Mở và đóng bằng chuyển động trượt có độ nảy nhẹ, khoảng 0,25 giây.
- HUD không bị xám; chỉ phủ tối nhẹ phần vườn sát mép tấm.
- Khi mở tab Trạm và chạm một dòng, vườn phía trên cuộn tới đúng trạm đó.

**Xong khi:** mở tab nào cũng còn thấy vườn, đóng được bằng một cú vuốt xuống.

---

## #23 UI: thanh tab dưới đáy rời rạc và khó thấy tab đang chọn

Nhãn: enhancement

Bốn nút tròn đang lơ lửng trên hàng cây, nhãn nhỏ, tab đang chọn chỉ khác một chút màu.

**Làm lại**
- Một tấm nền liền mạch cho cả thanh, bo góc, có bóng, nằm trong vùng an toàn của máy.
- Tab đang chọn: icon to lên, nền nổi, nhãn đậm. Tab khác: nhạt hơn.
- Nhãn tối thiểu cỡ chữ đọc được ở khoảng cách tay.
- Huy hiệu số: chỉ hiện khi có thứ nhận được, nảy nhẹ khi con số tăng.
- Chỗ cho mục thứ năm (Tiệm hoặc Sự kiện) về sau.

---

## #24 UI: thứ bậc nút bấm và chữ chưa rõ, nút mờ gây nhiễu

Nhãn: enhancement

Trong mọi danh sách, nút nào cũng cam to cùng một cỡ; nút chưa bấm được ("Chưa xong", "Nâng +1" khi thiếu tiền) vẫn chiếm chỗ như nút thật. Chữ phụ màu xám nhỏ khó đọc, một dòng nhồi ba thông tin (ví dụ "2,80K cà rốt / 1,5 giây · ×2 ở cấp 50").

**Cần một bộ quy tắc dùng chung**
- Ba cấp nút: chính (cam, một nút mỗi vùng), phụ (viền), chữ. Việc chưa làm được thì hiện tiến độ hoặc số còn thiếu, không hiện nút mờ.
- Thang chữ ba cỡ, chữ phụ không nhạt hơn 60% mực tím.
- Mỗi dòng một ý chính; số quan trọng nhất (thu nhập mỗi giây, giá) to và đậm, phần còn lại thành chip nhỏ.
- Khoảng cách và bo góc thống nhất giữa mọi thẻ.
- Đặt trong UiKit và Palette để mọi màn dùng chung.

---

## #25 UI: tab Trạm khó đọc và nâng cấp chậm tay

Nhãn: enhancement

Mỗi dòng trạm hiện tên, cấp, một dòng chữ dài và một nút. Người chơi không thấy ngay trạm nào đáng nâng.

**Làm lại dòng trạm**
- Hình trạm to hơn, thu nhập mỗi giây là con số chính.
- Thanh tiến độ tới mốc kế tiếp có ghi "còn N cấp tới ×2".
- Nút nâng: đủ tiền thì cam, thiếu thì hiện số còn thiếu. Giữ tay để nâng liên tục, nhanh dần.
- Trạm nguyên liệu ghi rõ đang tiếp sức cho trạm nào, bằng hình nhỏ của các trạm đó.
- Trạm chưa mở gom xuống cuối, trạm kế tiếp có nút Mở nổi bật.
- Chạm vào dòng thì vườn cuộn tới trạm đó.

---

## #26 UI: tab Bùa dùng ký tự thay cho hình

Nhãn: enhancement

Bốn lá bùa đang dùng ký tự làm icon (+, », ×2, Zz), trông như bản nháp.

**Cần**
- Bốn hình bùa vẽ cùng nét với game: Bàn tay ấm, Gió đêm, Ánh trăng rằm, Giấc ngủ dài. Vẽ bằng Gemini theo quy trình trong `docs/anh/po-do-vat/cat-tam-do-vat.py`.
- Mỗi thẻ ghi rõ hiệu lực hiện tại và hiệu lực sau khi học thêm một cấp (ví dụ "+0,2 giây → +0,4 giây").
- Thanh cấp dạng các chấm (1/10) thay cho chip chữ.

---

## #27 UI: tab Nhiệm vụ chưa làm nổi thứ nhận được

Nhãn: enhancement

Nhiệm vụ nhận được nằm lẫn giữa nhiệm vụ chưa xong; dòng nào cũng có một nút, phần lớn là nút mờ "Chưa xong"; phần thưởng là chữ xám nhỏ.

**Làm lại**
- Nhiệm vụ nhận được lên đầu, có nền sáng; thêm nút "Nhận tất cả".
- Nhiệm vụ chưa xong không có nút, chỉ có thanh tiến độ và con số.
- Phần thưởng thành chip có hình (cà rốt, cỏ bốn lá, cà rốt vàng) cỡ đọc được.
- Nhóm Hôm nay có đồng hồ đếm tới lúc làm mới.
- Nhận thưởng: phần thưởng bay từ dòng lên ô tương ứng trên HUD.

---

## #28 Chuyển động: game cần quy tắc di chuyển cho camera, thỏ và phần thưởng

Nhãn: enhancement

Hiện nhiều thứ xuất hiện và biến mất tức thì, nên game có cảm giác cứng. Cần một bộ quy tắc chuyển động dùng chung.

**Camera**
- Kéo có quán tính, thả tay thì trôi rồi dừng êm; chạm biên thì nảy nhẹ.
- Đổi mức zoom và bay tới trạm đều có gia tốc vào và ra, 0,25–0,35 giây.
- Mở game: camera lướt từ ao trăng xuống ấm trà trong khoảng một giây.

**Thỏ**
- Lúc đứng yên: thở nhẹ, chớp mắt, thỉnh thoảng vẫy tai.
- Chạm vào: nảy lên, hiện trái tim hoặc nốt nhạc.
- Thỏ đi dạo phải đi thật theo lối mòn từ chỗ nghỉ ra rồi quay về, không hiện và biến tức thì; đi thì nhún theo nhịp bước.
- Thỏ được xếp vào trạm: đi từ chỗ nghỉ tới trạm thay vì nhảy vị trí.

**Phần thưởng và con số**
- Xong một mẻ: cà rốt nảy ra khỏi trạm rồi bay về ô cà rốt trên HUD, ô cà rốt phồng nhẹ khi nhận.
- Con số tăng bằng cách chạy số, không nhảy cóc.
- Nâng cấp: đạo cụ nảy, loé sáng; qua mốc thì có vòng sao toả ra.

**Giao diện**
- Nút bấm lún xuống khi chạm và nảy lại khi thả.
- Tấm và thẻ trượt vào, không hiện tức thì.
- Danh sách xuất hiện lần lượt từng dòng, cách nhau vài phần trăm giây.

**Cảnh nền**
- Tán cây đung đưa chậm, đèn lồng lắc và chập chờn, mặt ao gợn, đom đóm bay theo đàn.

**Ràng buộc**
- Một bảng thời lượng và kiểu gia tốc đặt trong `Ease` và dùng chung.
- Có nút "Giảm chuyển động" trong Cài đặt cho người dễ chóng mặt.
- Không chuyển động nào chặn thao tác quá 0,3 giây.

---

## #29 Phản hồi khi chạm: âm thanh và rung

Nhãn: enhancement

Nhiều thao tác chưa có phản hồi riêng, người chơi không chắc mình đã bấm trúng.

- Mỗi loại thao tác có một tiếng riêng: chạm thỏ, nâng cấp, qua mốc, mở trạm, nhận thưởng, mở và đóng tấm, lỗi thiếu tiền.
- Thiếu tiền: nút lắc nhẹ và ô cà rốt nháy đỏ, thay cho việc không có gì xảy ra.
- Rung nhẹ trên điện thoại cho nâng cấp, qua mốc và gọi thỏ; tắt được trong Cài đặt.
- Thanh âm lượng riêng cho nhạc và hiệu ứng.

---

## #30 Hướng dẫn 60 giây đầu cho người chơi mới

Nhãn: enhancement

Người mới hiện chỉ có một dòng chữ gợi ý rồi tự mò. Bản chơi thử đầu tiên cần dẫn dắt mà không dùng nhiều chữ.

**Các bước, mỗi bước một bàn tay chỉ vào đúng chỗ**
1. Chạm con thỏ ở ấm trà để pha nhanh hơn.
2. Nâng ấm trà lên cấp 2 khi đủ cà rốt.
3. Mở trạm thứ hai.
4. Gọi con thỏ đầu tiên, xem nó bước ra từ cổng trăng.
5. Xếp nó vào trạm đúng nghề, thấy dấu ×1,5.
6. Nhận nhiệm vụ đầu tiên.

**Ràng buộc**
- Bỏ qua được bất cứ lúc nào.
- Không khoá màn hình; chỉ làm mờ phần không liên quan.
- Tiến độ hướng dẫn lưu trong save, không lặp lại.

---

## #31 Di chuyển trong vườn: khó biết ngoài màn hình có gì đang chờ

Nhãn: enhancement

Vườn cao hơn màn hình, người chơi phải kéo lên xuống để tìm trạm nâng được hoặc thỏ đang ngủ gật.

- Mũi tên ở mép trên và mép dưới màn hình khi ngoài tầm nhìn có trạm nâng được, thưởng đang chờ hoặc thỏ ngủ gật; chạm mũi tên thì bay tới đó.
- Trên tấm trạm: vuốt trái hoặc phải để nhảy sang trạm kế, vườn cuộn theo.
- Nút "về ấm trà" khi đã kéo đi xa tâm vườn.
- Ở mức zoom xa: chạm một vùng thì phóng vào vùng đó.
- Nhớ vị trí và mức zoom lần trước khi mở lại game.

---

## #32 HUD: ô cà rốt và thanh Trà lực chưa giải thích được mình

Nhãn: enhancement

- Ô cà rốt: chạm vào mở bảng nhỏ cho biết thu nhập đến từ trạm nào, hệ số nào đang áp dụng.
- Thanh Trà lực: người mới không hiểu "Tăng tốc!" là gì. Khi đủ lực thì nút sáng và nảy; chạm khi chưa đủ thì hiện còn bao lâu nữa đầy. Trong lúc tăng tốc, cả vườn có dấu hiệu nhìn thấy được (thỏ làm nhanh hơn, viền sáng).
- Con số lớn: dùng chung một cách viết ở mọi nơi; chạm giữ để xem số đầy đủ.
- Cụm nút góc phải (zoom, máy ảnh, cài đặt) gom lại khi thêm nút mới.

---

## #33 Dễ đọc và dễ dùng: cỡ chữ, tương phản, độ hiếm không chỉ bằng màu

Nhãn: accessibility, enhancement

- Cỡ chữ nhỏ nhất phải đọc được trên điện thoại 6 inch; có tuỳ chọn chữ to.
- Tương phản chữ trên nền kem và trên nền vườn tối đạt mức dễ đọc.
- Độ hiếm đang phân biệt chỉ bằng màu (Bạc, Xanh lá, Xanh lam, Tím, Vàng, Đỏ). Thêm số sao hoặc hình khung riêng cho từng bậc để người mù màu phân biệt được.
- Vùng chạm tối thiểu cho mọi nút; nút quan trọng nằm trong tầm ngón cái.
- Tôn trọng vùng an toàn của máy có tai thỏ và thanh điều hướng.
- Kiểm tra tràn chữ ở cả tiếng Việt và tiếng Anh với chuỗi dài nhất.

---

## #34 Vườn còn phẳng và trống ở các khoảng giữa

Nhãn: enhancement

Mặt đất là một màu xanh đậm có loang, giữa các trạm là khoảng trống lớn, mọi thứ như dán lên một mặt phẳng.

- Mặt đất có lớp lang: cỏ, lối mòn đất, thảm hoa, đá lát quanh ấm trà.
- Vật ở dưới che vật ở trên theo đúng thứ tự xa gần; bóng đổ mềm dưới thỏ và đồ vật.
- Tán cây viền quanh trượt chậm hơn mặt đất khi kéo, tạo chiều sâu.
- Vệt sáng trăng chiếu xuống ấm trà làm tâm điểm.
- Trang trí rải có chủ đích quanh từng cụm trạm thay vì ngẫu nhiên.
- Hình có sẵn để dùng: đèn đá, bồn hoa trong `docs/anh/po-do-vat/`.

---

## #35 Bấm nhanh nhiều lần thì thỏ phình to dần, tràn màn hình

Nhãn: bug

**Chủ project báo:** bấm vào thỏ quá nhiều lần thì con thỏ to ra, bấm tiếp thì tràn màn hình.

**Đã đọc code, có hai nguyên nhân có thể cùng gây ra hiện tượng này**

1. **Cộng dồn tỉ lệ khi nâng cấp nhanh** (chắc chắn là lỗi). Trong `GardenScreen.OnBuy`:
   ```csharp
   Vector3 home = actor.localScale;
   Tweens.Add(0.2f, t => actor.localScale = home * (1f + 0.14f * Ease.Pop(t)), ..., done: () => actor.localScale = home);
   ```
   `home` lấy tỉ lệ ĐANG CÓ lúc bấm. Nếu bấm lần nữa khi hiệu ứng nảy 0,2 giây chưa xong, `home` đã bị phóng tới 14%, và khi kết thúc nó được ghi lại làm tỉ lệ gốc. Mỗi lần bấm chồng lên nhau làm cả trạm to thêm vĩnh viễn. Bấm nhanh vài chục lần là tràn màn hình. Đây là chỗ duy nhất trong `Assets/VuonTra/Game` lấy tỉ lệ hiện tại làm gốc.

2. **Chạm đúp để zoom đụng với việc chạm thỏ liên tục** (xung đột thiết kế). Game khuyến khích chạm thỏ nhiều lần để làm nhanh hơn, nhưng hai lần chạm trong 0,35 giây lại được hiểu là chạm đúp và bay tới mức zoom gần (1,5 lần). Người chơi chạm nhanh sẽ thấy thỏ đột ngột to ra.

**Cách sửa đề xuất**
- Nguyên nhân 1: tỉ lệ gốc của mỗi trạm là một giá trị cố định lưu sẵn (theo `StationSize`), không đọc từ `localScale` lúc bấm; hiệu ứng mới thay thế hiệu ứng đang chạy trên cùng một trạm.
- Nguyên nhân 2: chạm đúp vào thỏ không zoom nữa. Zoom chỉ còn qua nút kính lúp, Cmd/Ctrl+cuộn, chụm, và chạm đúp vào khoảng đất trống.
- Rà các hiệu ứng nảy khác (nút, ô cà rốt, đạo cụ, thẻ) theo cùng nguyên tắc: không hiệu ứng nào được lấy trạng thái đang chuyển động làm gốc.

**Test cần có trước khi sửa (phải đỏ trên code hiện tại)**
- Nâng một trạm 30 lần, mỗi lần cách nhau 0,05 giây, chạy hết hiệu ứng: tỉ lệ của trạm bằng đúng tỉ lệ gốc.
- Chạm một con thỏ 30 lần liên tiếp cách nhau 0,1 giây: mức zoom không đổi, tỉ lệ thỏ nằm trong giới hạn của hiệu ứng lún (không quá 1,2 lần gốc) và trở về gốc khi dừng.
- Chạm xen kẽ thỏ và nút nâng thật nhanh: như trên.

**Xong khi:** các test trên qua, và thử tay trên bản Mac bấm liên tục 10 giây không thấy gì to ra.

---

## #36 Nâng cấp nhanh: nâng tối đa theo số cà rốt đang có

Nhãn: enhancement

Nâng từng cấp một rất chậm tay khi cà rốt đã nhiều. Tab Trạm đã có công tắc ×1 / ×10 / Tối đa, nhưng tấm trạm trong vườn chỉ có nút +1, và không có cách nâng nhiều trạm một lúc.

**Cần có**
- **Trên tấm trạm trong vườn:** cùng công tắc ×1 / ×10 / Tối đa như tab Trạm, nhớ lựa chọn giữa hai nơi. Nút ghi rõ sẽ nâng bao nhiêu cấp và tốn bao nhiêu, ví dụ "Nâng +37 · 1,2M".
- **Thêm mức "Tới mốc":** nâng vừa đủ tới mốc ×2 kế tiếp (10, 25, 50, 100…), nếu đủ tiền.
- **Giữ tay để nâng liên tục,** nhanh dần, thả ra thì dừng.
- **Nút "Nâng tất cả" ở đầu tab Trạm:** dùng số cà rốt đang có để nâng nhiều trạm một lượt, ưu tiên trạm cho thêm thu nhập nhiều nhất trên mỗi cà rốt bỏ ra. Trước khi làm thì hiện tóm tắt (trạm nào lên bao nhiêu cấp, tổng tốn bao nhiêu) và một nút xác nhận.
- **Giữ lại một phần:** tuỳ chọn không tiêu quá 100% / 50% / 25% số cà rốt, để người chơi còn tiền mở trạm mới.
- **Thỏ cũng vậy:** nút lên cấp thỏ có mức Tối đa, dừng ở trần cấp của bậc.

**Luật**
- Phần tính "nâng được tối đa bao nhiêu cấp" và "phân bổ cho nhiều trạm" nằm trong Core, có test: không bao giờ tiêu quá số đang có, không vượt trần cấp, kết quả giống hệt việc bấm +1 từng lần theo cùng thứ tự.
- Hiệu ứng: một lần nâng nhiều cấp chỉ nảy một lần (xem #35), con số cấp chạy lên.

**Liên quan:** #25 (làm lại tab Trạm), #24 (quy tắc nút).

---

## #37 Khay sản phẩm: thỏ làm ra món, hiện ở thanh ngắn phía dưới, chạm để nhận cà rốt

Nhãn: enhancement

Hiện cà rốt tự cộng vào ví, người chơi không có gì để "nhặt". Ý của chủ project: thỏ làm ra sản phẩm, sản phẩm hiện thành một dãy ngắn ở thanh phía dưới, chạm vào thì nhận cà rốt.

**Thiết kế đề xuất**
- Một **khay ngắn** nằm ngay trên thanh tab, chứa 5 ô (mở rộng được tới 8).
- Mỗi trạm, sau một số mẻ thường, làm ra một **mẻ đặc biệt**: một món có hình riêng (tách trà, hũ mứt, bánh, trà sữa…) bay từ trạm xuống khay.
- **Chạm một món** để bán: cà rốt bung ra bay về ví, giá bằng một khoảng thu nhập của trạm đó (ví dụ 60 giây), nhân với hệ số của thỏ đang làm ở trạm.
- **Vuốt ngang qua khay** để nhận hết một lượt.
- **Khay đầy** thì món mới không mất mà xếp chồng lên món cùng loại tới một giới hạn; vượt giới hạn thì trạm ngừng làm mẻ đặc biệt cho tới khi có chỗ. Khay có dấu báo khi đầy.
- **Thu nhập tự động vẫn giữ nguyên.** Khay là phần thưởng thêm cho người đang mở game, không thay cho thu nhập nền, để game vẫn là idle.
- **Lúc vắng mặt:** khay được làm đầy tới sức chứa, không hơn.
- Thỉnh thoảng có **món vàng** hiếm, bán được cà rốt vàng hoặc cỏ bốn lá, có trần mỗi ngày.

**Mở rộng về sau**
- Khách ghé (#20) đặt đúng các món trong khay.
- Bùa hoặc món ở Tiệm tăng sức chứa khay và tốc độ ra mẻ đặc biệt.
- Thỏ đúng nghề làm ra món đặc biệt nhanh hơn.

**Cần quyết khi làm**
- Giá một món bằng bao nhiêu giây thu nhập, và bao lâu ra một món, sao cho người chăm nhặt được thêm khoảng 15–25% so với người không nhặt.
- Vị trí khay khi có tấm trượt (#22) đang mở.

**Hình cần vẽ:** một món cho mỗi trạm làm ra sản phẩm (7 trạm), và khung khay.

**Luật** nằm trong Core, có test: sức chứa, xếp chồng, bán, làm đầy lúc vắng mặt, trần ngày của món vàng.

---

## #38 Lưu mây: lõi đồng bộ save, không phụ thuộc nhà cung cấp

Nhãn: enhancement

Người chơi hiện chỉ có save trên máy: xoá game hoặc đổi máy là mất vườn. Đây là phần lõi dùng chung cho iOS (#39) và Android (#40).

**Cần làm**
- Lớp giao tiếp `ICloudSave` (đã lên kế hoạch ở #17, việc R10): tải lên, tải xuống, hỏi thông tin bản trên mây (thời điểm lưu, cấp vườn, số thỏ, số loài, tổng thời gian chơi).
- **Khi nào đồng bộ:** lúc mở game, lúc game ra nền, sau các mốc lớn (mở rộng đất, gọi được thỏ bậc Vàng trở lên, mua ở Tiệm), và tối đa vài phút một lần. Không đồng bộ mỗi lần nâng cấp.
- **Khi hai bản lệch nhau:** không tự ghi đè. Hiện một tấm so hai bản (trên máy và trên mây) với cấp vườn, số thỏ, số loài, lần chơi cuối, rồi để người chơi chọn. Gợi ý sẵn bản tiến xa hơn.
- **Không bao giờ mất save trên máy:** trước khi thay bằng bản trên mây, giữ một bản sao lưu cũ trên máy, khôi phục được trong Cài đặt.
- **Không có mạng:** game chạy bình thường bằng save trên máy, đồng bộ khi có mạng lại.
- **Chống gian lận nhẹ:** bản trên mây mang phiên bản save và một mã kiểm, bản hỏng thì bỏ qua và báo.
- Trong Cài đặt: trạng thái "Đã lưu lên mây lúc …", nút "Lưu ngay", nút "Khôi phục từ mây".

**Test (Core):** chọn bản khi lệch, khứ hồi qua bản cài đặt giả, bản hỏng, save cũ phiên bản thấp, mất mạng giữa chừng.

**Phụ thuộc:** #17 (hồ sơ người chơi), #41 (bản build mobile).

---

## #39 iOS: tự đăng nhập bằng Game Center và lưu vườn lên iCloud

Nhãn: enhancement, question

Trên iPhone và iPad, người chơi mong mở game là được nhận ra ngay và vườn tự theo họ sang máy mới, không phải tạo tài khoản.

**Thiết kế**
- Mở game: tự đăng nhập Game Center, không hỏi gì. Người chơi đã tắt Game Center thì chơi như khách, có lời mời nhẹ trong Cài đặt.
- **Nhớ đăng nhập:** lần sau mở game tự vào lại, không hiện màn đăng nhập.
- Save lưu lên iCloud của chính người chơi, theo luật đồng bộ ở #38.
- Tên và ảnh Game Center dùng làm tên vườn mặc định (sửa được).
- Về sau: bảng xếp hạng và thành tựu Game Center nối với nhiệm vụ đang có.

**Cần chủ project**
- Tài khoản Apple Developer (có phí hằng năm), tạo App ID, bật Game Center và iCloud cho app.
- Chọn cách lưu: iCloud key-value (đơn giản, giới hạn 1 MB, đủ cho save hiện tại) hay CloudKit.
- Unity không có sẵn cầu nối iCloud; cần một plugin gốc nhỏ tự viết hoặc một gói bên thứ ba. Chọn gói nào cần chủ project đồng ý.

**Yêu cầu của App Store cần nhớ**
- Có trang chính sách quyền riêng tư và khai báo dữ liệu thu thập.
- Nếu game cho tạo tài khoản riêng thì phải có nút xoá tài khoản trong game (xem #42). Chỉ dùng Game Center và iCloud thì không tạo tài khoản riêng.

**Phụ thuộc:** #38, #41.

---

## #40 Android: tự đăng nhập bằng Google Play Games và lưu vườn lên Saved Games

Nhãn: enhancement, question

Trên Android (CH Play), người chơi mong mở game là tự đăng nhập bằng tài khoản Google Play Games và vườn tự theo họ sang máy mới.

**Thiết kế**
- Mở game: tự đăng nhập Play Games, không hỏi gì. Thất bại hoặc người chơi từ chối thì chơi như khách, có nút đăng nhập trong Cài đặt.
- **Nhớ đăng nhập:** lần sau mở game tự vào lại.
- Save lưu vào Saved Games (snapshot) của Play Games, kèm ảnh bìa là ảnh chụp vườn và mô tả "Cấp vườn N · M thỏ", theo luật đồng bộ ở #38.
- Về sau: thành tựu và bảng xếp hạng Play Games nối với nhiệm vụ đang có.

**Cần chủ project**
- Tài khoản Google Play Console (phí một lần), tạo app, bật Play Games Services và Saved Games, tạo OAuth client và khai báo chữ ký SHA-1 của bản build.
- Thêm gói Google Play Games Plugin for Unity vào project (gói bên thứ ba của Google): cần chủ project đồng ý.
- `BillingMode.json` trong project đã đặt GooglePlay, nên hướng Android đã được tính từ trước.

**Yêu cầu của Google Play cần nhớ**
- Trang chính sách quyền riêng tư và mục An toàn dữ liệu.
- Khai báo nhóm tuổi; nếu nhắm cả trẻ em thì có thêm ràng buộc về đăng nhập và quảng cáo.

**Phụ thuộc:** #38, #41.

---

## #41 Bản build iOS và Android

Nhãn: enhancement, question

Game hiện chỉ có bản Mac để thử (`Build/Mac/VuonTraTho.app`). Mọi việc về đăng nhập cửa hàng (#39, #40), chia sẻ ảnh gốc (#18), rung (#29), mua bằng tiền thật (#9) đều cần bản mobile trước.

**Cần làm**
- [ ] Thêm đích build iOS và Android trong `BuildScript`, kèm lệnh build từ dòng lệnh như bản Mac.
- [ ] Bundle id cho từng nền tảng (hiện `com.vuontratho.game` cho Standalone), phiên bản và số build.
- [ ] Icon app và màn hình chờ theo art bible.
- [ ] Khoá hướng dọc, vùng an toàn cho máy có tai thỏ và thanh điều hướng (#23, #33).
- [ ] Kiểm thao tác chạm thật: chụm để zoom, kéo, vuốt đóng tấm, giữ để nâng liên tục.
- [ ] Đo hiệu năng trên một máy tầm trung: 72 loài thỏ, sương, đom đóm; giữ 60 khung hình hoặc hạ hiệu ứng.
- [ ] Dung lượng bản cài: nén hình, bỏ ảnh tài liệu khỏi bản build.
- [ ] Bản thử nội bộ: TestFlight cho iOS, Internal testing cho Android.

**Cần chủ project:** tài khoản Apple Developer và Google Play Console, máy thật để thử, và Xcode cùng Android SDK cài trên máy build.

---

## #42 Quyền riêng tư: chính sách, xoá dữ liệu và xuất dữ liệu người chơi

Nhãn: enhancement, question

Cả App Store và Google Play đều yêu cầu những thứ này trước khi lên cửa hàng, nhất là khi game có đăng nhập và lưu mây.

- [ ] Trang chính sách quyền riêng tư bằng tiếng Việt và tiếng Anh, có đường dẫn trong Cài đặt và trên trang cửa hàng. Nội dung cần chủ project duyệt.
- [ ] Trong Cài đặt: "Xoá dữ liệu của tôi" xoá save trên máy và trên mây, có bước xác nhận hai lần. Bắt buộc nếu game có tài khoản riêng.
- [ ] Trong Cài đặt: "Sao chép mã người chơi" và cách liên hệ hỗ trợ.
- [ ] Khai báo dữ liệu thu thập cho hai cửa hàng (hiện game không gọi mạng và không thu thập gì; cập nhật khi thêm đăng nhập, lưu mây, phân tích).
- [ ] Quyết định nhóm tuổi mục tiêu; ảnh hưởng tới đăng nhập, quảng cáo và việc gọi thỏ có yếu tố may rủi (#9).

**Phụ thuộc:** #38, #39, #40.

---

## #43 Trạm trống hiện sẵn con thỏ, người chơi không hiểu phải làm gì

Nhãn: enhancement

**Chủ project báo:** trạm trống chưa làm gì mà vẫn hiện con thỏ, nhìn vào không biết cần làm gì.

**Hiện trạng**
- Ô đất chưa xây trạm hiện một con thỏ xám mờ đứng cạnh đạo cụ mờ, nhãn "Đất trống".
- Trạm chưa mở hiện con thỏ mờ ngủ với nhãn "z z z", trông giống hệt một con thỏ đang ngủ gật vì hết trà lực.
- Trạm đã mở nhưng chưa xếp thỏ sưu tầm nào vẫn hiện một con thỏ mặc định đang làm việc, nên người chơi tưởng trạm đã có thỏ và không biết mình có thể xếp thỏ vào để được thưởng nghề.

Ba trạng thái khác nhau đang dùng chung một hình ảnh "có con thỏ đứng đó".

**Nguyên tắc mới: chỉ hiện thỏ khi thật sự có một con thỏ của người chơi ở đó. Mỗi trạng thái có đúng một lời mời hành động.**

| Trạng thái | Hiện gì | Lời mời |
|---|---|---|
| Đất chưa mở rộng | Chỉ có sương và cây | Thẻ "Mở rộng vườn" ghi ba điều kiện (đã có) |
| Ô đất đã có, chưa xây trạm | Một nền đất có cọc biển, bóng mờ của đạo cụ sẽ xây. KHÔNG có thỏ | Biển giá nảy nhẹ: "Xây · 120"; đủ tiền thì sáng, thiếu thì ghi còn thiếu bao nhiêu |
| Trạm đã xây, chưa có thỏ | Đạo cụ đầy đủ màu, tự chạy chậm (có hơi nước, thanh tiến độ). Chỗ đứng của thỏ là một tấm đệm trống có viền chấm và dấu "+" | Chạm tấm đệm: mở danh sách thỏ, thỏ đúng nghề đứng đầu và có dấu ×1,5 |
| Trạm có thỏ đang làm | Con thỏ sưu tầm, nhãn tên, icon nghề | Chạm thỏ để làm nhanh hơn |
| Thỏ ngủ gật vì hết trà lực | Con thỏ đó nhắm mắt, "z z" | Chạm để đánh thức |

**Thay đổi cơ chế đi kèm**
- Bỏ con thỏ mặc định của trạm khỏi vườn. Trạm không có thỏ vẫn sản xuất ở mức gốc như hiện nay (không đổi thu nhập), chỉ là không vẽ thỏ.
- Để ấm trà không trống lúc bắt đầu: người chơi mới được tặng một con thỏ đầu tiên (Thỏ Bông) đã đứng sẵn ở ấm trà. Save cũ: trạm nào đang không có thỏ sưu tầm thì giữ nguyên là "chưa có thỏ", không tặng thêm.
- Chạm vào đạo cụ của trạm chưa có thỏ vẫn làm nhanh hơn như chạm vào thỏ.
- Hình thỏ theo nghề đã vẽ cho các trạm (và vòng khuấy trà của ấm trà) để dành cho hoạt ảnh làm việc theo nghề (#2), không xoá.
- Ô "ai làm ở đây" trên tấm trạm (đã có) là nơi thứ hai để xếp thỏ.
- Số trạm còn thiếu thỏ hiện thành huy hiệu trên tab Thỏ (#23).

**Test**
- Trạng thái hiển thị của từng ô theo dữ liệu: không có hình thỏ nào khi ô chưa xây hoặc trạm chưa có thỏ.
- Thu nhập của trạm chưa có thỏ không đổi so với trước.
- Người chơi mới có đúng một thỏ, đứng ở ấm trà; save cũ không được tặng thêm.
- Chạm tấm đệm mở danh sách thỏ, xếp xong thì thỏ hiện ra.

**Hình cần vẽ:** nền đất có cọc biển cho ô chưa xây, tấm đệm trống cho chỗ thỏ.

**Liên quan:** #30 (hướng dẫn người mới dùng đúng các lời mời này), #2, #23, #25.

---


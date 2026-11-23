# Mô tả cửa hàng: App Store và Google Play (vi / en)

*Soạn 23/11/2026 cho #41/#42, chờ chủ project duyệt. Số ký tự đếm bằng máy trên đúng đoạn trong khung (Python `len`, tức số ký tự Unicode; chữ Việt có dấu tính 1 ký tự). Riêng từ khoá App Store, Apple đếm theo **byte**, nên ghi số byte UTF-8 (chữ có dấu chiếm 2–3 byte).*

Giới hạn dùng ở đây:

| Mục | App Store | Google Play |
|---|---|---|
| Tên | 30 | 30 |
| Phụ đề | 30 | — |
| Văn bản quảng bá (promotional text, sửa được không cần nộp lại) | 170 | — |
| Mô tả ngắn | — | 80 |
| Mô tả (dài) | 4000 | 4000 |
| Từ khoá | 100 byte, ngăn bằng dấu phẩy | — (Google lấy từ mô tả) |

**Ngôn ngữ chính.** Đề xuất: App Store chọn ngôn ngữ chính **English (U.S.)** và thêm bản địa hoá **Vietnamese**; Google Play chọn mặc định **en-US**, thêm **vi**. Lý do: tên en hiện ở mọi nước không có bản vi, còn người dùng máy tiếng Việt vẫn thấy bản vi. Nếu chỉ phát hành ở Việt Nam thì đảo lại.

**Hai chỗ tuỳ quyết định.**
- Câu "không quảng cáo" trong mô tả đúng khi #66 giữ tắt. Nếu bật quảng cáo thì bỏ cụm đó và tick "Contains ads" trên Google Play.
- Đoạn cuối mô tả viết cho trường hợp **có** bán cà rốt vàng (#9). Nếu bản đầu **không** bán, thay đoạn cuối bằng đoạn "không bán" ở mục 5 và bỏ phần công bố tỉ lệ ở cuối mô tả (vẫn có thể giữ, không bắt buộc).

---

## 1. App Store

### Tiếng Việt
**Tên** · 18/30 ký tự

```text
Vườn Trà Thỏ Trăng
```
**Phụ đề** · 24/30 ký tự

```text
Tiệm trà nhỏ của bầy thỏ
```
**Văn bản quảng bá** · 134/170 ký tự

```text
Tiệm trà nhỏ trong rừng đêm do những bé thỏ trông nom. Pha trà, gọi thỏ qua cổng trăng, câu cá ở ao và mở dần khu vườn dài dưới trăng.
```
**Từ khoá** · 95/100 byte UTF-8 (78 ký tự)

```text
idle,nhàn rỗi,mochi,trung thu,câu cá,sưu tập,dễ thương,thư giãn,cozy,tiệm bánh
```
Không lặp chữ đã có trong tên (thỏ, trà, vườn, trăng): Apple đã tự đánh chỉ mục tên.
**Mô tả** · 1418/4000 ký tự

```text
Giữa khu rừng đêm có một tiệm trà nhỏ, và chủ tiệm là những bé thỏ.

Vườn Trà Thỏ Trăng là trò chơi chăm vườn nhẹ nhàng: chạm để pha trà, mở thêm trạm, gọi thỏ về phụ việc rồi ngắm khu vườn tự lớn lên, kể cả lúc bạn đi vắng.

• Mười sáu trạm làm việc: Ấm trà sương, Cối giã hoa, Lò bánh hạt dẻ, Tiệm bánh trung thu, Thuyền trà đêm… mỗi trạm một nhịp và một tiếng riêng.
• Gọi thỏ qua cổng trăng: hơn 70 loài thỏ vẽ tay ở sáu bậc từ Bạc tới Đỏ. Ghép ba bé cùng bậc thành một bé bậc trên, điền dần Sổ thỏ.
• Khu vườn dài cuộn ngang: kéo sang hai bên, sương tan dần mở thêm đất cho trạm, chỗ nghỉ và đồ trang trí. Chế độ Bố trí để bạn tự xếp vườn.
• Câu cá ở ao trăng, cho thỏ ăn và kết thân tới mười tim.
• Ngày, đêm, mưa và đom đóm theo giờ của máy. Mùa Tết, Trung thu, Giáng sinh mang tới những bé thỏ chỉ gặp trong mùa.
• Đội mũ, đổi tên, chụp ảnh vườn. Thỏ tự nói chuyện và thỉnh thoảng xin bạn một tấm ảnh.
• Nhiệm vụ ngày, lịch quà bảy ngày, hũ trăng, và mùa trăng mới để vườn mạnh hơn mãi mãi.
• Vườn vẫn thu hoạch khi bạn đi vắng; quay lại là có quà chờ.

Chơi không cần mạng, không quảng cáo, không thu thập dữ liệu cá nhân. Lưu vườn lên mây qua Game Center và iCloud. Có tiếng Việt và tiếng Anh, có chế độ Giảm chuyển động.

Cà rốt vàng kiếm được khi chơi: quà ngày, mẻ vàng, tấm ảnh đầu tiên mỗi ngày, mỗi 5 cấp vườn và thành tựu. Cà rốt vàng cũng có thể mua trong ứng dụng; tỉ lệ gọi thỏ ghi ngay trong Tiệm.
```
Khi bật #9, dán thêm phần công bố tỉ lệ (mục 4) vào cuối: tổng **1926/4000**.

### English
**Name** · 22/30 ký tự

```text
Moon Rabbit Tea Garden
```
**Subtitle** · 29/30 ký tự

```text
Cozy idle tea shop of bunnies
```
**Promotional text** · 154/170 ký tự

```text
A little tea shop in a night forest, run by rabbits. Brew tea, call bunnies through the moon gate, fish the pond and open up a long garden under the moon.
```
**Keywords** · 92/100 byte UTF-8 (92 ký tự)

```text
idle,cozy,bunny,bunnies,cute,tycoon,clicker,mochi,mooncake,collect,fishing,relax,cafe,kawaii
```
Words already in the name (moon, rabbit, tea, garden) are left out on purpose.
**Description** · 1606/4000 ký tự

```text
Deep in a night forest there is a little tea shop, and the shopkeepers are rabbits.

Moon Rabbit Tea Garden is a gentle idle game: tap to brew, open new stations, call rabbits in to help, then watch the garden grow on its own, even while you are away.

• Sixteen stations: Dew Teapot, Flower Mortar, Chestnut Oven, Mooncake Bakery, Night Tea Boat… each with its own rhythm and its own sound.
• Call rabbits through the moon gate: more than 70 hand-drawn kinds across six tiers from Silver to Red. Merge three of a tier into one of the next and fill your rabbit album.
• A long garden that scrolls sideways: drag left and right as the mist lifts to open land for stations, rest spots and decorations. Arrange mode lets you lay it out yourself.
• Fish the moon pond, feed your rabbits and befriend them up to ten hearts.
• Day, night, rain and fireflies follow your device clock. Lunar New Year, Mid-Autumn and Christmas bring rabbits you only meet in season.
• Hats, names and photo mode. Rabbits chat on their own and now and then ask you for a picture.
• Daily quests, a seven-day gift calendar, moon jars, and new moons that make the garden stronger for good.
• The garden keeps harvesting while you are away; come back to a gift.

Plays offline, no ads, no personal data collected. Back up your garden through Game Center and iCloud. In English and Vietnamese, with a Reduce Motion option.

Golden carrots are earned in play: the daily gift, golden batches, the day's first photo, every fifth garden level and achievements. They can also be bought in the app; the rabbit call odds are shown in the shop.
```
With #9 on, append the odds disclosure (section 4): total **2186/4000**.

### Các ô khác của App Store Connect

| Ô | Điền |
|---|---|
| Support URL | **CP điền**: trang hỗ trợ công khai (có thể chính là trang chính sách, thêm một dòng email) |
| Marketing URL | để trống được |
| Privacy Policy URL | **CP điền**: URL công khai của chính sách (xem việc chặn A trong CHECKLIST) |
| Copyright | **CP điền**: `© 2026 <tên người hoặc tổ chức>` |
| Primary category | **Games**, subcategory **Simulation** |
| Secondary category | **Games → Casual** |
| Age rating | dự kiến **4+** (mục 3) |
| Price | Free |
| App Review → Sign-in required | Không (game không cần tài khoản; Game Center tuỳ chọn) |
| App Review → Notes | "No account is needed. Game Center and iCloud are optional and only back up progress. All content is reachable by playing; the in-app purchases (if any) are golden carrot packs, which are also earned in play." |
| App Review → Contact | **CP điền**: họ tên, điện thoại, email |

---

## 2. Google Play

### Tiếng Việt
**Tên ứng dụng** · 18/30 ký tự

```text
Vườn Trà Thỏ Trăng
```
**Mô tả ngắn** · 68/80 ký tự

```text
Chăm tiệm trà của bầy thỏ dưới trăng: gọi thỏ, câu cá, mở rộng vườn.
```
**Mô tả đầy đủ** · 1414/4000 ký tự

```text
Giữa khu rừng đêm có một tiệm trà nhỏ, và chủ tiệm là những bé thỏ.

Vườn Trà Thỏ Trăng là trò chơi chăm vườn nhẹ nhàng: chạm để pha trà, mở thêm trạm, gọi thỏ về phụ việc rồi ngắm khu vườn tự lớn lên, kể cả lúc bạn đi vắng.

• Mười sáu trạm làm việc: Ấm trà sương, Cối giã hoa, Lò bánh hạt dẻ, Tiệm bánh trung thu, Thuyền trà đêm… mỗi trạm một nhịp và một tiếng riêng.
• Gọi thỏ qua cổng trăng: hơn 70 loài thỏ vẽ tay ở sáu bậc từ Bạc tới Đỏ. Ghép ba bé cùng bậc thành một bé bậc trên, điền dần Sổ thỏ.
• Khu vườn dài cuộn ngang: kéo sang hai bên, sương tan dần mở thêm đất cho trạm, chỗ nghỉ và đồ trang trí. Chế độ Bố trí để bạn tự xếp vườn.
• Câu cá ở ao trăng, cho thỏ ăn và kết thân tới mười tim.
• Ngày, đêm, mưa và đom đóm theo giờ của máy. Mùa Tết, Trung thu, Giáng sinh mang tới những bé thỏ chỉ gặp trong mùa.
• Đội mũ, đổi tên, chụp ảnh vườn. Thỏ tự nói chuyện và thỉnh thoảng xin bạn một tấm ảnh.
• Nhiệm vụ ngày, lịch quà bảy ngày, hũ trăng, và mùa trăng mới để vườn mạnh hơn mãi mãi.
• Vườn vẫn thu hoạch khi bạn đi vắng; quay lại là có quà chờ.

Chơi không cần mạng, không quảng cáo, không thu thập dữ liệu cá nhân. Lưu vườn lên mây qua Google Play Games. Có tiếng Việt và tiếng Anh, có chế độ Giảm chuyển động.

Cà rốt vàng kiếm được khi chơi: quà ngày, mẻ vàng, tấm ảnh đầu tiên mỗi ngày, mỗi 5 cấp vườn và thành tựu. Cà rốt vàng cũng có thể mua trong ứng dụng; tỉ lệ gọi thỏ ghi ngay trong Tiệm.
```
Khi bật #9, dán thêm phần công bố tỉ lệ (mục 4): tổng **1922/4000**. Google yêu cầu công bố tỉ lệ cho vật phẩm ngẫu nhiên mua được bằng tiền thật trước khi mua; game đã hiện tỉ lệ ở Tiệm và màn Thỏ, đoạn trong mô tả là thêm cho rõ.

### English
**App name** · 22/30 ký tự

```text
Moon Rabbit Tea Garden
```
**Short description** · 79/80 ký tự

```text
Run a cozy tea shop with rabbits: call bunnies, fish the pond, grow the garden.
```
**Full description** · 1601/4000 ký tự

```text
Deep in a night forest there is a little tea shop, and the shopkeepers are rabbits.

Moon Rabbit Tea Garden is a gentle idle game: tap to brew, open new stations, call rabbits in to help, then watch the garden grow on its own, even while you are away.

• Sixteen stations: Dew Teapot, Flower Mortar, Chestnut Oven, Mooncake Bakery, Night Tea Boat… each with its own rhythm and its own sound.
• Call rabbits through the moon gate: more than 70 hand-drawn kinds across six tiers from Silver to Red. Merge three of a tier into one of the next and fill your rabbit album.
• A long garden that scrolls sideways: drag left and right as the mist lifts to open land for stations, rest spots and decorations. Arrange mode lets you lay it out yourself.
• Fish the moon pond, feed your rabbits and befriend them up to ten hearts.
• Day, night, rain and fireflies follow your device clock. Lunar New Year, Mid-Autumn and Christmas bring rabbits you only meet in season.
• Hats, names and photo mode. Rabbits chat on their own and now and then ask you for a picture.
• Daily quests, a seven-day gift calendar, moon jars, and new moons that make the garden stronger for good.
• The garden keeps harvesting while you are away; come back to a gift.

Plays offline, no ads, no personal data collected. Back up your garden through Google Play Games. In English and Vietnamese, with a Reduce Motion option.

Golden carrots are earned in play: the daily gift, golden batches, the day's first photo, every fifth garden level and achievements. They can also be bought in the app; the rabbit call odds are shown in the shop.
```
With #9 on, append the odds disclosure (section 4): total **2181/4000**.

Mô tả Google Play khác App Store đúng một câu: "Game Center và iCloud" đổi thành "Google Play Games".

### Các ô khác của Play Console

| Ô | Điền |
|---|---|
| App category | **Game → Simulation** |
| Tags (tối đa 5) | Idle, Casual, Simulation, Cute, Offline (chọn từ danh sách của Google; tên tag có thể khác chút) |
| Email liên hệ | **CP điền** (bắt buộc, hiện công khai) |
| Website | tuỳ chọn |
| Privacy policy | **CP điền**: cùng URL công khai với App Store |
| Contains ads | **No** (khi #66 tắt) |
| In-app purchases | Có/không theo #9 |

---

## 3. Nhóm tuổi (theo bản nháp #42, `docs/khai-bao-du-lieu-cua-hang.md` §3)

| Cửa hàng | Đề xuất | Ghi chú |
|---|---|---|
| App Store | **4+** | Không bạo lực, không nội dung người lớn, không cờ bạc bằng tiền thật. Gọi thỏ bằng tiền ảo kiếm trong game không phải "simulated gambling". Bảng câu hỏi tuổi của Apple đã đổi từ 2025 (thêm các mức 9+, 13+, 16+ và câu hỏi về vật phẩm ngẫu nhiên mua được): CP trả lời đúng theo bản có/không có #9 rồi để Apple tính mức. |
| Google Play (IARC) | **Everyone** | Trả lời "không" mọi mục nội dung. "In-game purchases" và "includes random items" chỉ chọn **Yes** khi bật #9. |
| Google Play target audience | **13 trở lên** | Không nhắm riêng trẻ em, tránh chính sách Families. Muốn nhắm cả trẻ em thì phải vào Designed for Families và đổi đăng nhập Play Games tự động. |

---

## 4. Công bố tỉ lệ gọi thỏ (dán cuối mô tả khi bật #9)

Số lấy đúng từ code: `Rabbits.Odds` và `Shop.GoldenOdds` (`Assets/VuonTra/Core/Rabbits.cs`, `Shop.cs`), loài trong bậc chia đều (`RabbitKinds.Pick`), mùa lễ `Seasons.GoldenCallShare = 0.5`. Đổi số trong code thì phải sửa đoạn này.

**Tiếng Việt** · 506 ký tự

```text
Tỉ lệ gọi thỏ
• Gọi bằng 1 cỏ bốn lá: Bạc 60 % · Xanh lá 25 % · Xanh dương 10 % · Tím 4 % · Vàng 0,9 % · Đỏ 0,1 %.
• Gọi vàng bằng 10 cà rốt vàng: Bạc 25 % · Xanh lá 35 % · Xanh dương 22 % · Tím 12 % · Vàng 5 % · Đỏ 1 %.
• Trong một bậc, mọi loài thường ngày có tỉ lệ bằng nhau. Trong mùa lễ, một nửa số lần gọi vàng rơi vào bậc của thỏ mùa sẽ ra thỏ mùa.
• Thỏ ghé Tiệm mỗi ngày hiện rõ loài và giá trước khi mua, không ngẫu nhiên lúc mua.
Cà rốt vàng kiếm được trong game; mua bằng tiền thật là tuỳ chọn.
```

**English** · 578 characters

```text
Rabbit call odds
• Clover call (1 four-leaf clover): Silver 60% · Green 25% · Blue 10% · Purple 4% · Gold 0.9% · Red 0.1%.
• Golden call (10 golden carrots): Silver 25% · Green 35% · Blue 22% · Purple 12% · Gold 5% · Red 1%.
• Within a tier, every everyday kind is equally likely. In a festival season, half of the golden calls that land on the season rabbit's tier bring the season rabbit.
• The rabbit visiting the shop each day shows its kind and price before you buy; nothing is random at purchase.
Golden carrots are earned in play; buying them with real money is optional.
```

## 5. Câu về mua trong ứng dụng

**Có bán (#9 bật)** — câu này đã nằm ở đoạn cuối các mô tả trên. Giá thật do CP đặt trên console (bản thử trong game đang hiện giá giữ chỗ 25.000 ₫ / 69.000 ₫ / 199.000 ₫ cho 50 / 150 / 500 cà rốt vàng). Chỉ cà rốt vàng được bán; cà rốt, thỏ, cỏ bốn lá không bao giờ bán bằng tiền.

Tên và mô tả cho ba sản phẩm (dùng chung hai cửa hàng):

| Product ID | Tên vi | Tên en | Mô tả vi | Mô tả en |
|---|---|---|---|---|
| `vuontra.golden.50` | 50 cà rốt vàng | 50 Golden Carrots | Một nắm cà rốt vàng cho Tiệm. | A handful of golden carrots for the shop. |
| `vuontra.golden.150` | 150 cà rốt vàng | 150 Golden Carrots | Giỏ cà rốt vàng, gói hời nhất. | A basket of golden carrots, the best deal. |
| `vuontra.golden.500` | 500 cà rốt vàng | 500 Golden Carrots | Cả xe cà rốt vàng cho vườn lớn. | A cartful of golden carrots for a big garden. |

**Không bán (#9 tắt)** — thay đoạn cuối mô tả bằng:

Tiếng Việt · 173 ký tự

```text
Cà rốt vàng kiếm được khi chơi: quà ngày, mẻ vàng, tấm ảnh đầu tiên mỗi ngày, mỗi 5 cấp vườn và thành tựu. Không có gì bán bằng tiền thật; tỉ lệ gọi thỏ ghi ngay trong Tiệm.
```

English · 208 characters

```text
Golden carrots are earned in play: the daily gift, golden batches, the day's first photo, every fifth garden level and achievements. Nothing is sold for real money; the rabbit call odds are shown in the shop.
```

---

## Chỗ chủ project cần điền hoặc quyết

- Ngôn ngữ chính (en hay vi) trên hai cửa hàng.
- #9 có/không → chọn đoạn cuối mô tả, phần tỉ lệ, sản phẩm IAP; giá thật ba gói.
- #66 có/không → câu "không quảng cáo".
- Support URL, Privacy Policy URL (công khai), email liên hệ, Copyright, người liên hệ cho App Review.

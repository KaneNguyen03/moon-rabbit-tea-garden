# Kiểm định đồ hoạ — 25/11/2026

Chủ project: "đồ hoạ game khá xấu". Tài liệu này chấm hiện trạng UI, assets, hoạt ảnh và ô đất theo các kỹ năng của dự án, viết vòng làm việc cho 16 trạm, và dẫn tới các issue nhãn `do-hoa`. Chỉ đọc khung hình có sẵn trong `docs/anh` và file trong `Assets/VuonTra`; không chạy Unity.

## 1. Quy tắc chấm lấy từ kỹ năng

| Kỹ năng | Quy tắc dùng để chấm |
|---|---|
| `cozy-watercolor-art` | Màu chỉ từ `docs/art-bible.md`/`Palette.cs`. Nét mực tím `#3A2A4D` mảnh, có độ dày thay đổi, không đen. Mỗi trạm, cây, đèn có bóng tiếp đất elip `Ink` 25–30 %, đứng trên tấm nền riêng (sàn gỗ, đất, cụm hoa). Đèn lồng và lò có điểm sáng ấm 2D. (Vân giấy và sương theo bản cũ của skill — 10–15 %, sương `Paper` — đã được art bible mới mục 4–7 thay: vân chỉ `Ink` 6–8 %, không còn sương: vùng chưa mở là bóng đêm đều `Night` pha 1/4 `Grass`, không mây, không mảng sáng, tối đa 2 lớp phủ toàn màn, vật đặc không vẽ bằng alpha, kem sáng nhất chỉ cho thỏ, đèn và thẻ UI; xem §6a.) HUD không có ô nét đứt kiểu khung dây; biểu tượng HUD chiếm 65–70 % chiều cao ô. Hình mới: PNG cạnh bội số 4, Bilinear, pivot Bottom Center cho đồ đứng đất. |
| `create-game-assets` | Khoá khung kỹ thuật và ngôn ngữ hình trước khi vẽ; duyệt một hình mẫu rồi mới vẽ cả họ; giữ bất biến bảng màu, góc nhìn, nét, hướng sáng, mật độ chi tiết; kiểm alpha giả, viền quầng; xem ở cỡ thật trên nền game; ghi nguồn gốc (AI) cho từng hình. |
| `game-ui-ux` | Bố cục bằng neo và container; vùng an toàn; HUD chạy bằng sự kiện; chữ ≥ 18 px ở 1080p, phóng 200 % không mất nội dung; tương phản chữ mang thông tin ≥ 4,5:1 (chữ lớn 3:1); màu không là tín hiệu duy nhất; chừa chỗ cho tiếng Anh dài hơn ~30 %. |
| `unity:craft-game-floors` (visual-scorecard, bộ neo 2D) | Sáu trục 1–5, mốc sàn là 3: kỷ luật bảng màu, đồng nhất phong cách (một mật độ nét, không trộn độ phân giải), viền nét thay cho chiều sâu ánh sáng (hoặc mọi hình quan trọng đều có viền, hoặc không hình nào), bóng dáng đọc được ở mức zoom chơi, UI có chủ đích, phản hồi nhìn thấy được trong khung bận nhất. 5 = ảnh quảng bá indie cách điệu, 3 = bản game jam chỉn chu, 1 = khối thô dưới đèn trắng. |
| `visual-critic-gate` | Bảy mục: mối nối, căn neo không rung, bảng màu và nét, alpha sạch, không vẽ dính hiệu ứng, tỉ lệ và bóng tiếp đất, UI đọc được và chạm ≥ 44 pt. Chỉ `APPROVED` mới đóng việc hình. |
| `unity-2d-native-rigging` | Cấm `Mathf.Sin/Cos/Lerp` hay ghi `localPosition/Rotation/Scale` để làm động tay chân, dụng cụ. Bắt buộc rig PSB/SpriteSkin, `SpriteLibrary` + `SpriteResolver`, mọi chuyển động là `.anim` trong `Animator Controller`, biến thể qua `AnimatorOverrideController`; code chỉ đặt tham số Animator. |
| `game-feel-juice` | Mỗi tương tác có đủ ba lớp: chuyển động (DOTween trên gốc UI/đồ vật), tiếng (cao độ ±5 %), hạt Shuriken lấy từ pool. Hiệu ứng nền (hơi nước, lá, bụi, đom đóm) là `ParticleSystem` lặp, không vẽ dính vào PNG. Tôn trọng Giảm chuyển động. |

## 2. Hiện trạng đo được trong `Assets/VuonTra`

- **Trạm (16):** `am-tra, gio-la, noi-mut, coi-hoa, lo-banh, to-mat, tra-sua, binh-sao, gian-nho, tiem-banh, suoi-hoa, dai-sao, ho-sen, lo-keo, ruong-nep, thuyen-tra` (`Editor/ContentSeeder.cs:43-58`).
- **Hình trạm:** chỉ 5 trạm có hình vẽ màu nước trong `Art/Final` (`prop_am-tra` 570×505, `prop_ho-sen`, `prop_lo-keo`, `prop_ruong-nep`, `prop_thuyen-tra`, cạnh dài 512). **11 trạm còn hình placeholder vector 256×256** trong `Art/Placeholder` (`binh-sao, coi-hoa, dai-sao, gian-nho, gio-la, lo-banh, noi-mut, suoi-hoa, tiem-banh, to-mat, tra-sua`): mảng màu phẳng, viền dày đều, không hạt giấy. `ContentSeeder` gọi `FinalArt.Load("prop_" + id)` nên cứ có hình ở `Final` là tự thay.
- **Món ra:** 9 món `product_*` cho 9 trạm pha chế (cạnh dài 512). 7 trạm nguyên liệu chưa có hình món.
- **Hoạt ảnh:** không có file `.anim`, `.controller`, `.psb` hay `SpriteLibraryAsset` nào trong `Assets`. `rabbit_am-tra_f0..f3` (484×484) được `MetaSeeder.cs:248-259` nạp vào `StationData.WorkFrames` nhưng **không chỗ nào trong `Game` đọc `WorkFrames`**, nên 4 khung đã vẽ không bao giờ hiện. Tay cầm dụng cụ đung đưa bằng `Mathf.Sin/Cos` theo nghề (`StationActorView.cs:342-372`), thân thỏ thở và nghiêng bằng sin (`StationActorView.cs:497-523`); `GardenScreen.cs` có 43 lần `Mathf.Sin`. Rig thân thỏ (Thân → HeadPivot → Arm, 4 nghề cầm thìa/cán bột/giỏ/ống nhòm) đang ở pipeline thỏ 2D (`docs/anh/do-hoa-tho/README.md`, `hoat-anh/lam-viec-4-nghe-khung.png`); dụng cụ chung theo nghề, chưa theo từng trạm.
- **Hạt:** không có `ParticleSystem` nào trong `Assets/VuonTra/Game`. Hơi nước ấm trà là sprite tĩnh, đom đóm là chấm UI. Chưa có DOTween (`Packages/manifest.json`).
- **Nền và cảnh:** `bg_grass` 512² là vệt loang xanh kiểu rằn ri; cây, bụi, hàng rào, nấm, ao, đá, gốc cây đều là placeholder vector (`Art/Placeholder/decor_*`, `bg_*`). Trang trí đã vẽ màu nước: đèn đá, bồn hoa, ghế nấm, thảm picnic, xích đu, vòng hoa, cổng mùa, sương.
- **Tấm nền trạm:** 5 tấm đã vẽ (`pad`, `pad_dat`, `pad_hoa`, `pad_san-go`, `pad_vong-da`). Chỗ "Xếp thỏ +" dùng chiếu tre viền hồng; ô xây dùng `plot_sign` placeholder trên vòng đất trơn.
- **UI:** toàn bộ uGUI dựng bằng code (`UiKit`), thẻ là `panel.png` 64×64 cắt 9 phần, màu từ `Palette`. Phông lấy từ hệ điều hành (`UiKit.cs:22`: Avenir Next → Helvetica Neue → Arial → Roboto), nên Mac và Android hiện hai phông khác nhau.
- **Tương phản (tính từ `Palette.cs`):** chữ cam `Carrot` trên thẻ `Card` 2,04:1 (dòng "+2,13K/giây", "Tăng tốc!"); chữ `OffInk` trên nút `Off` 1,96:1 ("Còn thiếu 8 cà rốt vàng"); `OffInk` trên thẻ 3,01:1; `InkSoft` trên thẻ 5,17:1 (đạt); `Ink` trên nút cam 6,2:1 (đạt).

## 3. Bảng điểm (1–5, mốc sàn 3)

Khung dẫn chứng chính: [vườn ngày có bể cá](anh/po-114/114-be-ca-vuon.png), [vườn cấp 32 + bảng trạm](anh/po-110/110b-tam-tram-mon-3-vi.png), [bản đồ 2D đoạn xa](anh/po-vuon-2d/91b-6-doan-xa.png), [bản đồ 2D đoạn trên](anh/po-vuon-2d/91c-6-doan-tren.png), [tab Trạm](anh/po-50/50-sau-tram.png), [thỏ đang làm](anh/po-43/43-sau-tho-dang-lam.png), [Tiệm](anh/po-105/105-tiem-goi.png), [Rương xe đẩy](anh/po-113/113-ruong-mo.png), [tab Thỏ](anh/8-rabbits.png), [Tuần đầu](anh/po-102/102-ngay-1.png).

### a. UI/HUD

| Mục | Điểm | Dẫn chứng |
|---|---|---|
| Khung 9-slice | 2 | Mọi thẻ là cùng một hình chữ nhật bo góc trơn màu kem với gờ đáy; không có giấy, mép xé, hoạ tiết góc. Trông như app web hơn là sách truyện ([105](anh/po-105/105-tiem-goi.png), [113](anh/po-113/113-ruong-mo.png)). |
| Chữ | 2 | Phông hệ điều hành, khác nhau giữa máy; tiêu đề và thân cùng một họ chữ không có cá tính. Chữ cam 2,04:1 và nút xám 1,96:1 dưới mức 4,5:1. "Còn thiếu 8 cà rốt vàng" xuống hai dòng trong nút ([105](anh/po-105/105-tiem-goi.png)). |
| Màu | 4 | Bám bảng màu: nút cà rốt, thẻ kem, tiến độ rêu, mực tím. Điểm trừ: ô "Màu đèn lồng tháng 8" chỉ là chấm hồng phẳng ([113](anh/po-113/113-ruong-mo.png)). |
| Khoảng cách | 3 | Nhịp lề đều trong danh sách; bảng trạm để trống nửa trái ([110b](anh/po-110/110b-tam-tram-mon-3-vi.png)); dòng tỉ lệ gọi thỏ trong Tiệm chen chúc. |
| Phân cấp | 2 | Mọi nút cam cùng độ nặng; trên vườn có 6–8 viên nhãn cùng kiểu ("Giã bánh · còn 3 lượt", "Hứng lá · còn 3 lượt", "Câu cá 4/5", "Đom đóm · còn 3 lượt", "Mở lúc 19:00", "Cấp 27 ✦", "Xếp thỏ +") tranh nhau ([114](anh/po-114/114-be-ca-vuon.png)). |
| Biểu tượng | 3 | Icon tab (ấm trà, trăng, thỏ, sổ) và mũ vẽ đẹp; icon trạm trong danh sách là placeholder vector (giỏ, nồi) đứng cạnh ấm trà màu nước ([50](anh/po-50/50-sau-tram.png)). |

### b. Assets

| Mục | Điểm | Dẫn chứng |
|---|---|---|
| Trạm | 2 | 11/16 trạm là vector phẳng 256 px: nồi mứt hồng, cối tím, giỏ lá, cốc trà sữa, tổ ong, lò bánh. Đứng cạnh ấm trà màu nước là gãy phong cách ngay giữa màn ([114](anh/po-114/114-be-ca-vuon.png), [91b-6](anh/po-vuon-2d/91b-6-doan-xa.png)). |
| Đồ vật, trang trí | 3 | Đèn đá, bồn hoa, ghế nấm, xích đu, cối giã đã vẽ đẹp; bàn trà gốc cây, nấm, bụi, đá vẫn placeholder. |
| Nền | 1 | Nền cỏ là vệt loang rằn ri lặp lại; cỏ cắm thành hàng lưới đều tăm tắp ([91c-6](anh/po-vuon-2d/91c-6-doan-tren.png)); không có đất, lối mòn, viền cỏ. |
| Cây, viền rừng | 2 | Cây kẹo mút placeholder lặp y hệt, một lớp; mép rừng là hàng thân cây thẳng tắp ([91c-6](anh/po-vuon-2d/91c-6-doan-tren.png)). |
| Ao | 2 | Elip xanh dương có trăng trắng, không bờ đá, lau sậy, phản chiếu ([114](anh/po-114/114-be-ca-vuon.png)). |
| Sương | 2 | Sương kem sáng hơn cỏ, là mảng sáng nhất cảnh, phủ gần 40 % màn ngày đầu, đọc thành ảnh nhoè ([114](anh/po-114/114-be-ca-vuon.png)). Chủ project đã quyết bỏ sương: vùng chưa mở thành bóng đêm đều (art bible mới mục 5). |
| Đồng nhất nét, bảng màu, tỉ lệ | 2 | Hai họ hình song song (vector viền dày và màu nước nét mảnh). Tỉ lệ lệch: quầy xe đẩy nhỏ bằng nửa tiệm, cối giã to hơn thỏ, bể cá bé như hộp diêm ([114](anh/po-114/114-be-ca-vuon.png)). |

### c. Hoạt ảnh

| Mục | Điểm | Dẫn chứng |
|---|---|---|
| Vòng làm việc đọc được | 1 | Thỏ đứng cạnh đồ vật, một tay cầm cốc trắng đưa qua lại; không thấy thỏ làm ra món gì, không có món xuất hiện ở trạm ([43](anh/po-43/43-sau-tho-dang-lam.png), [114](anh/po-114/114-be-ca-vuon.png)). 4 khung ấm trà đã vẽ nhưng không nối. |
| Đúng luật hoạt ảnh | 1 | Toàn bộ chuyển động là `Mathf.Sin` trong C# (`StationActorView`, `GardenScreen`), trái luật 1 trong CLAUDE.md và `unity-2d-native-rigging`. |
| Hiệu ứng hạt | 1 | Không có `ParticleSystem`; hơi nước là sprite tĩnh. |
| Trạng thái rảnh/làm/nâng cấp | 2 | Ngủ là viên "zzz", nâng cấp là phóng to nảy; trạm cấp 1 và cấp 112 trông y hệt trừ huy hiệu. |

### d. Ô đất và bố cục vườn

| Mục | Điểm | Dẫn chứng |
|---|---|---|
| Ô trống, ô khoá | 2 | Ô xây là biển gỗ placeholder trên vòng đất trơn với viên "Xây · 32,0M"; ô khoá là đám mây; chỗ "Xếp thỏ +" là chiếu tre viền hồng nổi lơ lửng ([110b](anh/po-110/110b-tam-tram-mon-3-vi.png)). |
| Lối đi, hàng rào, luống, chậu, đèn, cỏ | 2 | Chỉ có đá lát elip xám và vài cột đèn; không hàng rào, không luống chè, chậu đặt rời rạc. |
| Cảm giác "đầy" | 1 | Trạm là những đảo tròn giữa bãi cỏ trống; bản đồ 2D có cả màn chỉ cỏ lưới ([91c-6](anh/po-vuon-2d/91c-6-doan-tren.png)). Mục tiêu: mỗi ô trạm là một góc nhỏ có đồ dùng, cây trồng, lối và ánh đèn riêng, không còn mảng cỏ trống to hơn một ô. |

### Bảng sáu trục (`unity:craft-game-floors`, bộ neo 2D)

```
palette: 3/5 — mọi màu UI và hình vẽ từ Palette.cs; nền rằn ri và sương xám kéo lệch tông đêm
style-uniformity: 2/5 — 11 trạm và cây cỏ vector viền dày cạnh hình màu nước nét mảnh
outline-discipline: 2/5 — hai độ dày nét; bóng tiếp đất có ở thỏ, thiếu ở nhiều đồ vật
silhouette: 3/5 — thỏ và ấm trà đọc được ở zoom chơi; trạm vector nhỏ, mờ giữa nền loang
ui-intentionality: 3/5 — thẻ kem nút kẹo nhất quán nhưng phẳng, phông hệ điều hành, nhãn vườn dồn cục
feedback-visibility: 1/5 — trạm chạy không có hành động hay món hiện ra; không có hạt
```

Ba trục dưới 3 là đồng nhất phong cách, viền nét và phản hồi: đó là thứ người chơi thấy là "xấu".

## 4. Vòng làm việc cho 16 trạm

Nguyên tắc: mỗi vòng 3–4 nhịp, đọc được ở zoom vườn (thỏ ~110 px cao); nhịp cuối luôn đặt **món** lên khay hoặc vật chứa ở mép trước trạm, rồi món bay lên khay chung (#37). Thân thỏ và tay lấy từ rig pipeline thỏ 2D; trạm chỉ thêm **dụng cụ cầm tay** (gắn xương tay), **trạng thái đồ vật** (đổi nhãn `SpriteResolver`) và **hạt**. Một vòng = một `CycleSeconds`; trạm chu kỳ dài lặp nhịp 2 cho tới nhịp cuối.

| Trạm | Nhịp 1 | Nhịp 2 | Nhịp 3 | Nhịp 4 (món) | Dụng cụ cầm tay | Trạng thái đồ vật | Hạt |
|---|---|---|---|---|---|---|---|
| Ấm trà sương `am-tra` | múc lá vào ấm | rót nước nóng từ siêu đồng | đậy nắp, chờ ngấm, hơi bốc | rót ra chén, chén lên khay | muỗng tre, siêu đồng | ấm: nắp mở / nắp đóng / ngấm | hơi nước, giọt nước |
| Giỏ lá trà `gio-la` | cúi hái lá ở luống chè | bỏ lá vào giỏ | giỏ đầy, lá ló miệng | nhấc giỏ đặt lên sàn | (tay không), giỏ đeo hông | luống: búp non / đã hái; giỏ: vơi / nửa / đầy | lá bay nhẹ khi hái |
| Nồi mứt dâu `noi-mut` | thả dâu vào nồi | khuấy bằng muôi gỗ | múc mứt | rót vào hũ, đậy vải, hũ lên khay | rổ dâu, muôi gỗ | nồi: trống / có dâu / sủi; hũ: rỗng / đầy | hơi đỏ hồng, bọt sủi |
| Cối giã hoa `coi-hoa` | rắc cánh hoa vào cối | giã chày (lên) | giã chày (xuống) | dốc bột hoa vào túi giấy | chày, nắm cánh hoa | cối: hoa / bột | cánh hoa bay, bụi tím |
| Lò bánh hạt dẻ `lo-banh` | nặn viên bột | đặt khay vào lò | mở cửa lò, bánh vàng | xếp bánh ra khay gỗ | khay nướng, găng tay | cửa lò: đóng / mở; lửa nhỏ / to | khói ấm, tàn lửa |
| Tổ mật ong `to-mat` | phe phẩy quạt khói | nhấc khung sáp | quay vắt mật | mật chảy vào hũ, hũ lên khay | quạt khói, khung sáp | tổ: đóng / mở; hũ vơi / đầy | ong nhỏ bay quanh |
| Quầy trà sữa trăng `tra-sua` | rót sữa vào bình lắc | lắc bình | rót ra ly có trân châu | ly có ống hút lên khay | bình sữa, bình lắc | hũ trân châu đầy / vơi; chồng ly cao / thấp | giọt sữa bắn nhẹ |
| Bình ủ sao `binh-sao` | múc bụi sao bằng lưới | rắc vào bình thuỷ tinh | lắc bình, bình sáng | rót ra lọ, lọ phát sáng lên khay | lưới hớt sao, lọ | bình: tối / sáng | sao nhỏ lấp lánh |
| Giàn nho trăng `gian-nho` | đứng ghế, với chùm nho | cắt cuống bằng kéo | đặt chùm nho vào thúng | thúng đầy lên khay | kéo tỉa, thúng | giàn: chùm chín / đã hái | lá nho rơi |
| Tiệm bánh trung thu `tiem-banh` | ấn bột vào khuôn gỗ | gõ khuôn ra bánh | phết lòng đỏ trứng | bánh vàng lên khay | khuôn gỗ, chổi phết | khay: bánh sống / bánh vàng | bột mì bay |
| Suối khoáng hoa `suoi-hoa` | thả gáo xuống suối | múc nước | rót qua phễu lá | chai nước có hoa lên khay | gáo dừa, chai | mặt suối: phẳng / gợn | gợn nước, cánh hoa trôi |
| Đài ngắm sao `dai-sao` | xoay kính viễn vọng | nhìn qua kính | ghi chép vào sổ | thẻ bản đồ sao lên khay | sổ, bút lông | kính: hướng trái / phải | sao băng nhỏ |
| Vườn hoa sen `ho-sen` | chèo thuyền thúng ra | cúi ngắt hoa sen | bỏ vào giỏ | bó sen lên khay | mái chèo, giỏ | sen: nụ / nở / đã ngắt | gợn nước, đom đóm |
| Lò kẹo mạch nha `lo-keo` | khuấy mạch nha trong chảo | kéo dẻo kẹo | cắt kẹo bằng dao | kẹo gói giấy lên khay | đũa khuấy, dao | chảo: lỏng / đặc; lửa | hơi ngọt, tàn lửa |
| Ruộng lúa nếp `ruong-nep` | cắt bông lúa bằng liềm | bó lúa | đập lúa vào nong | bao nếp lên khay | liềm nhỏ, nong | ruộng: chín vàng / đã gặt | hạt thóc bay |
| Thuyền trà đêm `thuyen-tra` | đẩy sào cho thuyền trôi | pha trà trên bếp lò nhỏ | rót chén | chén trà lên khay mũi thuyền | sào, ấm nhỏ | đèn thuyền tắt / sáng | gợn nước, hơi nước |

### 4a. Ba trạm mẫu chuẩn (chủ project chọn, mọi trạm khác làm theo đúng khuôn này)

Khuôn chung: 4 nhịp, mỗi nhịp 0,6–1 s (chuẩn nhịp làm việc ở `nghien-cuu-thiet-ke-do-hoa.md` §7), có chuẩn bị ~100 ms và đi theo ~150 ms. Vòng hoạt ảnh không bị nén theo `CycleSeconds`: trạm chu kỳ ngắn (Ấm trà 1,5 s) vẫn chạy vòng ~3,2 s, chỉ số cà rốt chạy theo mô phỏng. Món ở nhịp 4 nằm trên **khay nhỏ trước trạm**, tại điểm `ProductSpawn` (thay toạ độ cứng `new Vector2(112, -40)` ở `GardenScreen.cs:5279`); khi mô phỏng thêm món vào khay chung (`Tray.Add`, #37) thì món ở `ProductSpawn` nhấc lên và bay vào ô khay bằng `Feel.Fly` như hiện nay, còn không thì món mờ đi ở đầu vòng kế. Dụng cụ gắn xương tay của rig thỏ; đồ vật trạm đổi nhãn `SpriteLibrary` (`Prop/idle, b1, b2, b3`); món trung gian là sprite riêng gắn tay hoặc đặt trên quầy; hạt là Shuriken.

**Quầy trà sữa trăng `tra-sua`** (tấm mẫu `de-xuat-do-hoa/tra-sua-4-nhip.png`, `.gif`)

| Nhịp | Thỏ làm | Dụng cụ trong tay | Món trung gian | Đồ vật trạm | Hạt | Món ra |
|---|---|---|---|---|---|---|
| 1 | rót sữa từ bình vào bình lắc | bình sữa (tay phải), bình lắc mở (tay trái) | dòng sữa trắng | hũ trân châu đầy, chồng ly cao | 3–5 giọt sữa bắn khi dòng chạm đáy | — |
| 2 | đậy nắp, lắc bình cạnh đầu | bình lắc đóng | — | như trên | không (chỉ nảy tai, đi theo 150 ms) | — |
| 3 | nghiêng bình, rót ra ly có trân châu | bình lắc mở | ly có trân châu trên khay, dòng trà caramel | hũ trân châu vơi một nấc, chồng ly thấp một ly | 2–3 giọt trà | — |
| 4 | chắp tay cười | (tay không) | — | như nhịp 3 | lấp lánh nhỏ 1 lần khi ly đặt xuống | ly trà sữa có nắp và ống hút hồng trên khay nhỏ giữa quầy = `ProductSpawn` |

**Trạm hái chè `gio-la`**

| Nhịp | Thỏ làm | Dụng cụ trong tay | Món trung gian | Đồ vật trạm | Hạt | Món ra |
|---|---|---|---|---|---|---|
| 1 | cúi hái búp ở luống chè | (tay không), giỏ đeo hông | nắm lá non trong tay | luống: búp non | 2–3 lá bay khi ngắt | — |
| 2 | bỏ lá vào giỏ | giỏ | lá rơi vào giỏ | giỏ vơi → nửa | 1–2 lá rơi | — |
| 3 | giỏ đầy, lá ló miệng, thỏ phủi tay | giỏ | — | giỏ đầy; luống: đã hái | vài hạt bụi xanh | — |
| 4 | nhấc giỏ đặt lên sàn gỗ trước luống | giỏ đầy | — | luống mọc lại búp (về `idle`) | lá xoay nhẹ 1 lần | bó lá chè buộc lạt trên sàn trước luống = `ProductSpawn` (bay về Ấm trà và Trà sữa khi tiếp sức) |

**Quầy pha trà `am-tra` (Ấm trà sương)**

| Nhịp | Thỏ làm | Dụng cụ trong tay | Món trung gian | Đồ vật trạm | Hạt | Món ra |
|---|---|---|---|---|---|---|
| 1 | múc lá vào ấm | muỗng tre có lá | lá rơi vào miệng ấm | ấm: nắp mở, bếp than đỏ | 1–2 lá nhỏ rơi | — |
| 2 | rót nước nóng từ siêu đồng | siêu đồng | dòng nước | ấm: nắp mở | hơi nước từ miệng siêu, giọt nước | — |
| 3 | đậy nắp, chờ ngấm, tay chắp sau lưng | (tay không) | — | ấm: nắp đóng, ngấm (thân ấm sẫm hơn) | hơi nước từ vòi ấm 0,8–1,2 s | — |
| 4 | rót trà ra chén | ấm (nhấc khỏi bếp) | dòng trà hổ phách | ấm về bếp | hơi nhẹ trên chén | chén trà trên đĩa nhỏ ở mép trước bếp = `ProductSpawn` |

Trạng thái chung mọi trạm: **rảnh** (không thỏ: đồ vật nghỉ, đèn trạm tắt), **làm** (vòng trên), **tăng tốc** (vòng nhanh gấp đôi, thêm hạt), **thỏ ngủ** (tư thế ngủ từ rig, đồ vật nghỉ), **nâng cấp** (bụi sao + đồ vật nảy, không phải phóng to cả trạm), **mốc cấp 25/100/300** (đồ trang trí ô dày thêm, xem mục 5).

## 5. Ô đất "đầy"

Mỗi ô trạm gồm bốn lớp, từ sau ra trước, đều đặt trong vòng lục giác của ô (#52):

1. **Tấm nền** theo loại trạm: sàn gỗ (pha chế), đất xới (hái lượm), đá vòng (lò), mặt nước (sen, thuyền, suối).
2. **Cây trồng hoặc nguồn nguyên liệu** phía sau: luống chè 3 hàng (giỏ lá), giàn nho, khóm hoa (cối giã), bụi dâu (nồi mứt), tổ ong trên cọc, ruộng nếp.
3. **Đồ dùng** hai bên: bao, sọt, chậu, ghế đẩu, kệ hũ, hàng rào thấp đan tre ở 1–2 cạnh.
4. **Ánh sáng và cỏ viền**: một đèn lồng hoặc đèn đá, cỏ rậm, hoa dại, cỏ ba lá ở mép; lối đá lát nối ô với lối chính.

Ở mốc 25/100/300 ô thêm một món của lớp 3 hoặc 4, nên trạm lâu năm trông giàu hơn trạm mới mở. Bãi cỏ giữa các ô: cỏ rải ngẫu nhiên có chùm, không lưới; mỗi màn có ít nhất một lối mòn đất chạy qua.

## 6. Tấm mẫu định hướng

Lưu trong [`docs/anh/de-xuat-do-hoa/`](anh/de-xuat-do-hoa/), vẽ bằng Antigravity theo `docs/anh/VE-BANG-ANTIGRAVITY.md`, chấm `visual-critic-gate` trong `de-xuat-do-hoa/VERDICT.md`. Đây là hướng đi để chủ project nhìn, không phải hình đưa vào game.

| Tấm | Nội dung | Chấm |
|---|---|---|
| [o-dat-day.png](anh/de-xuat-do-hoa/o-dat-day.png), [trước/sau](anh/de-xuat-do-hoa/o-dat-day-truoc-sau.png) | một ô đất "đầy" (#130): sàn gỗ, bếp ấm trà, luống chè, giỏ, hàng rào tre, lối mòn, đèn lồng, chậu, ghế | APPROVED vòng 1 |
| [tra-sua-4-nhip.png](anh/de-xuat-do-hoa/tra-sua-4-nhip.png), [.gif](anh/de-xuat-do-hoa/tra-sua-4-nhip.gif) | vòng 4 nhịp quầy trà sữa (#133, mẫu chuẩn §4a) | APPROVED vòng 5 |
| [hud-the.png](anh/de-xuat-do-hoa/hud-the.png), mảnh trong `hud-manh/` | thẻ giấy, ruy băng, nút, ô chiếu tre, đèn lồng tab, thanh tre (#143) | APPROVED vòng 5 |

## 6a. Đối chiếu với chuẩn thiết kế của session "Độ nét đồ họa"

Chuẩn ở `docs/nghien-cuu-thiet-ke-do-hoa.md` (nhánh `do-net-do-hoa`, worktree `../VuonTraNam-net`, lúc viết còn chưa commit) và phần bổ sung `docs/art-bible.md` mục 4–7 trong worktree đó. Session đó nhận các issue `do-hoa`; khi hai tài liệu khác nhau, **chuẩn thiết kế và art bible mới thắng**, tài liệu này theo.

| Chủ đề | Tài liệu này | Chuẩn thiết kế | Áp dụng |
|---|---|---|---|
| Sương vùng chưa mở | `Paper` pha 15–25 % `Night` | chủ project bỏ sương hẳn (25/11, "nhìn đau mắt"): vùng chưa mở là bóng đêm đều, `Night` pha 1/4 `Grass`, không mây, không mảng sáng (art bible mới mục 5) | theo chuẩn; #134, #138 đã sửa |
| Vân giấy | 10–15 % theo skill | chỉ mực `Ink`, 6–8 %, vì uGUI không có Multiply (V1, art bible mới mục 6) | theo chuẩn; #134 |
| Lớp phủ toàn màn | không đo | tối đa 2 lớp bật cùng lúc; cột trăng 0,9 → 0,3 (V2, V4) | thêm vào #134 |
| Đá lát, lối đi | sprite đá có bóng | màu đặc, không alpha (V3) | khớp; ghi vào #129 |
| Nhãn trên vườn | ≤ 3 viên có chữ ở zoom mặc định | ≤ 4 nhãn chữ trong thế giới (bài kiểm §4); nhãn nền mực tối chữ kem, kem sáng nhất dành cho thỏ, đèn, thẻ UI (art bible mới mục 7; đã làm cho cối, giỏ, hũ, ao, "Xếp thỏ +") | theo chuẩn, ≤ 4 là mức đạt; #135 |
| Thời lượng nhịp làm việc | 1 vòng = 1 `CycleSeconds` | 0,6–1 s mỗi nhịp, chuẩn bị 100 ms, đi theo 150 ms (§7) | theo chuẩn: vòng không nén theo chu kỳ; #132, §4a |
| Phông | Baloo 2 + Nunito (OFL) | phông tròn bundle có dấu tiếng Việt, cân nhắc TMP (mục 11) | đề xuất cụ thể, session thiết kế chốt; #142 |
| DOTween | không thêm | quyết định của chủ project (V19) | hết hiệu lực từ #155: DOTween 1.3.045 đã cài ở `Assets/Plugins/Demigiant/DOTween`; #136 đã làm bằng `Tweens`/`FeelLayer` trước đó |
| Thẻ trạm trống | hình trạm + món + thỏ bằng sprite có sẵn | RenderTexture (mục 13) | khớp hướng; #143 để session thiết kế chọn cách |
| Vòng làm việc theo trạm, ô đất đầy, 11 trạm placeholder | có (#130 #131 #133 #139–#141) | chưa nêu | bổ sung, không mâu thuẫn |

## 7. Issue đã tạo (nhãn `do-hoa`, ưu tiên 1 = thấy ngay ở màn vườn ngày 1)

| Ưu tiên | Issue | Việc | Loại |
|---|---|---|---|
| 1 | #129 | Nền vườn: bỏ cỏ loang và cỏ lưới, thêm lối mòn | code-only trước (`cats-and-soup`) + vẽ |
| 2 | #130 | Ô đất "đầy": luống, hàng rào, đồ dùng, đèn quanh mỗi trạm | code-only trước (`cats-and-soup`) + vẽ |
| 3 | #131 | Vẽ lại 11 trạm placeholder vector | cần vẽ |
| 4 | #132 | Hạ tầng vòng làm việc: Animator, `StationLoop`, nối 4 khung Ấm trà, món hiện ở trạm | code-only (`cats-and-soup`) |
| 5 | #133 | Vòng làm việc nhóm trà: Ấm trà, Giỏ lá (hái lá), Trà sữa (lắc ra trà sữa) | cần vẽ |
| 6 | #134 | Tỉ lệ, bóng tiếp đất, ánh đèn, vùng chưa mở | code-only (`cats-and-soup`) |
| 7 | #135 | Gom và phân cấp nhãn nổi trên vườn | code-only (`cats-and-soup`) |
| 8 | #136 | Hạt Shuriken: hơi nước, lá, bột, sao, đom đóm | code-only (`cats-and-soup`) + ít vẽ |
| 9 | #137 | Cảnh nền: cây, bụi, hàng rào, ao, viền rừng | cần vẽ |
| 10 | #138 | Ô chưa xây, ô khoá, chỗ "Xếp thỏ +" | cần vẽ |
| 11 | #139 | Vòng làm việc nhóm bếp | cần vẽ |
| 12 | #140 | Vòng làm việc nhóm sao và nước | cần vẽ |
| 13 | #141 | Vòng làm việc nhóm hái lượm | cần vẽ |
| 14 | #142 | Phông chữ OFL có dấu tiếng Việt, tương phản 4,5:1 | code-only (`cats-and-soup`) |
| 15 | #143 | Khung thẻ giấy 9-slice, HUD, tab; bảng trạm có hình | code-only trước (`cats-and-soup`) + vẽ |

Thân và tay thỏ ở mọi issue vòng làm việc lấy từ rig của pipeline thỏ 2D; issue chỉ mô tả nhịp, dụng cụ, trạng thái đồ vật, món và hạt. Issue có `cats-and-soup`: B làm phần code-only, session thiết kế "Độ nét đồ họa" làm phần vẽ.

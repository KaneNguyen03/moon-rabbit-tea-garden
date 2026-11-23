# Ảnh chụp cửa hàng: 8 ảnh chọn từ preview hiện có

*Soạn 23/11/2026 cho #41. Chọn từ 609 ảnh trong `docs/anh/**`; ưu tiên ảnh đẹp, mỗi ảnh một tính năng, thứ tự kể được câu chuyện: mở game → vườn → gọi thỏ → sưu tập → hoạt động → vườn lớn dần.*

## Cỡ ảnh mỗi cửa hàng cần

| Cửa hàng / thiết bị | Cỡ nhận | Ảnh hiện có (1080×1920, 9:16) |
|---|---|---|
| Google Play, điện thoại (2–8 ảnh) | cạnh 320–3840 px, tỉ lệ không quá 2:1 | **Dùng thẳng được** |
| App Store, iPhone 6,9" (bắt buộc nếu không nộp 6,5") | 1320×2868 hoặc 1290×2796 (dọc) | **Không nhận**, phải chụp lại |
| App Store, iPhone 6,5" (thay cho 6,9") | 1242×2688 hoặc 1284×2778 | Không nhận, phải chụp lại |
| App Store, iPad 13" (bắt buộc vì game bật iPad, `targetDevice: 2`) | 2064×2752 hoặc 2048×2732 | Không nhận, phải chụp lại |
| Google Play, ảnh nổi bật (bắt buộc) | 1024×500 | **Chưa có** |
| Google Play, icon | 512×512 PNG | Thu nhỏ từ `docs/anh/po-41/41-icon-app.png` (1024×1024) |
| App Store, icon | 1024×1024 | `docs/anh/po-41/41-icon-app.png` (đã nằm trong bản build) |

Tỉ lệ iPhone mới (≈ 19,5:9) dài hơn 9:16: không kéo giãn ảnh cũ, mà render lại để giao diện tự xếp theo màn dài. iPad 3:4 thì ngắn và rộng hơn nhiều: **cần xem game trông thế nào trên iPad trước** (bố cục dọc có bị hở hai bên không). Nếu không ổn, chủ project có thể chọn chỉ phát hành cho iPhone (Player Settings → Target Device: iPhone Only) để bỏ hẳn bộ ảnh iPad.

## 8 ảnh chọn

Cột **Giao diện mới?**: ảnh chụp sau khi có tab Bố trí và vườn cuộn ngang (thanh dưới có 5 nút). Ảnh cũ hơn có thanh 4 nút, vẫn đẹp nhưng không còn đúng bản sẽ phát hành, nên phải chụp lại kể cả cho Google Play.

| # | Ảnh | Chú thích vi | Chú thích en | Giao diện mới? | Việc cần làm |
|---|---|---|---|---|---|
| 1 | [po-u1/28-sau-1-title.png](../anh/po-u1/28-sau-1-title.png) | Tiệm trà nhỏ của những bé thỏ dưới trăng | A little tea shop run by rabbits under the moon | Không (nền là vườn dọc cũ) | Chụp lại (`PreviewShot` "1-title") |
| 2 | [po-vuon-ngang/sau-mo-dau.png](../anh/po-vuon-ngang/sau-mo-dau.png) | Pha trà, giã bánh, chăm vườn mỗi ngày | Brew, pound mochi and tend the garden | **Có** | Google Play dùng được ngay; App Store chụp lại ("2-garden") |
| 3 | [po-r5/18c-arrived.png](../anh/po-r5/18c-arrived.png) | Gọi thỏ qua cổng trăng | Call rabbits through the moon gate | Màn cổng không đổi; nền mờ phía sau là bảng Thỏ cũ | Nên chụp lại ("18c-arrived") cho đồng bộ |
| 4 | [po-u1/24-sau-8-rabbits.png](../anh/po-u1/24-sau-8-rabbits.png) | Hơn 70 loài thỏ, đội mũ thật xinh | 70+ rabbit kinds, each with a hat to wear | Không | Chụp lại ("8-rabbits"), chọn save có nhiều bé đội mũ |
| 5 | [po-58/58-ca-bay.png](../anh/po-58/58-ca-bay.png) | Câu cá ở ao trăng | Fish the moon pond | **Có** | Google Play dùng được ngay; App Store chụp lại ("58-ca-bay") |
| 6 | [po-62/62-mua-dem.png](../anh/po-62/62-mua-dem.png) | Mưa đêm, Thỏ Ếch ghé chơi | Night rain brings the Frog Rabbit | Không, và thanh dưới đang là chữ Anh (ảnh chụp trước bản sửa lẫn chữ `52d73ff`) | Chụp lại ("62-mua-dem") |
| 7 | [po-vuon-ngang/sau-suong-tan-giua.png](../anh/po-vuon-ngang/sau-suong-tan-giua.png) | Sương tan, vườn rộng thêm | The mist lifts and the garden grows | **Có** | Google Play dùng được ngay; App Store chụp lại |
| 8 | [po-59/59-noi-dem.png](../anh/po-59/59-noi-dem.png) | Thỏ tự trò chuyện khi đêm xuống | Rabbits chat as night falls | Không | Chụp lại ("59-noi-dem") |

Chú thích dùng làm dòng chữ in lên đầu ảnh (nếu làm khung quảng bá) hoặc để tham khảo khi xếp thứ tự; cả hai cửa hàng không có ô chú thích riêng cho từng ảnh.

**Dự bị** (đổi vào nếu chủ project thích hơn): `po-8/8-cong-tet.png` (cổng trăng mùa Tết, thỏ mùa), `po-57/57-ngay-7.png` (Tiệm và lịch quà 7 ngày; đang lẫn chữ Anh, phải chụp lại), `po-61/61-so-tho-bo.png` (Sổ thỏ theo bộ nghề), `po-56/56-xin-chup.png` (thỏ xin chụp ảnh), `po-55/55-sau-duoi-dom-dom.png` (đuổi đom đóm; lẫn chữ Anh).

## Bộ tiếng Anh

Gần như không có ảnh tiếng Anh dùng được: chỉ `11-garden-en`, `12-rabbits-en`, `13-quests-en`, `14-pause-en` (giao diện cũ). Bộ en phải render lại cùng 8 cảnh với `L10n.SetLocale("en")`, như khối en sẵn có trong `PreviewShot.cs` (quanh dòng 439).

## Cách chụp lại (việc cho session code, không cần chủ project)

1. Trong `Assets/VuonTra/Editor/PreviewShot.cs`, `Width`/`Height` đang cố định 1080×1920. Thêm một lối chạy riêng cho cửa hàng (vd. `PreviewShot.RenderStore`) nhận cỡ 1320×2868 và 2064×2752, chỉ chụp 8 cảnh trên, mỗi cảnh hai ngôn ngữ, ghi vào `Logs/store/<cỡ>/<vi|en>/`.
2. Dùng save mẫu có vườn đã mở vài đoạn, nhiều thỏ đội mũ, không có hộp hướng dẫn che màn hình (các ảnh `po-1/*` còn dòng "Mở trạm thứ hai trong tab Trạm" của hướng dẫn).
3. Kiểm từng ảnh: không lẫn chữ Anh/Việt, không bong bóng đè biển tên, thanh trên không hiện số khổng lồ khó hiểu (vd. "1,00af").
4. Không commit ảnh cửa hàng vào `docs/anh` nếu nặng; đặt ở `docs/phat-hanh/anh/` hoặc chỉ giữ trên máy rồi tải lên console.

## Ảnh nổi bật Google Play (1024×500)

Chưa có. Gợi ý: nền là dải vườn cuộn ngang (rất hợp khổ ngang), chữ "Moon Rabbit Tea Garden" / "Vườn Trà Thỏ Trăng" bên trái theo kiểu chữ màn tiêu đề, một bé thỏ cạnh ấm trà bên phải. Có thể dựng từ `po-vuon-ngang/sau-nhin-xa.png` (cắt ngang) hoặc vẽ qua Antigravity theo `docs/anh/VE-BANG-ANTIGRAVITY.md`. Google cấm chữ chiếm quá nhiều diện tích và cấm chữ kiểu "Top 1", "Miễn phí".

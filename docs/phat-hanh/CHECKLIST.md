# Phát hành Vườn Trà Thỏ Trăng: danh sách từng bước

*Viết 23/11/2026 cho #41 và #42. Đi từ trên xuống; mỗi bước dẫn tới tài liệu chi tiết đã có, không chép lại.*

Ký hiệu cột **Ai**:
- **CP**: chỉ chủ project làm được (tài khoản, tiền, ký hợp đồng, quyết định, khoá ký).
- **Máy**: ai có máy Mac của dự án cũng làm được, kể cả một session Claude nếu chủ project cho phép.
- **CP+Máy**: chủ project đăng nhập hoặc bấm duyệt, phần còn lại làm trên máy.

Thời gian là thời gian làm tay; dòng *chờ* là thời gian chờ Apple/Google, chạy song song được với bước khác.

Tài liệu đi kèm trong thư mục này:
- [mo-ta-cua-hang.md](mo-ta-cua-hang.md): tên, phụ đề, mô tả, từ khoá, danh mục, nhóm tuổi, câu công bố tỉ lệ (vi/en, đã đếm ký tự).
- [anh-cua-hang.md](anh-cua-hang.md): 8 ảnh chụp cửa hàng chọn sẵn, chú thích vi/en, ảnh nào phải chụp lại.
- [ghi-chu-phien-ban.md](ghi-chu-phien-ban.md): ghi chú phát hành 0.1.0 vi/en.

---

## Việc chặn phát hành phải gỡ trước (phát hiện khi soạn bộ này)

| # | Việc | Vì sao chặn | Ai |
|---|---|---|---|
| A | **Trang chính sách quyền riêng tư phải mở được công khai.** `AppInfo.PrivacyUrl` đang trỏ tới `github.com/KaneNguyen03/moon-rabbit-tea-garden/blob/main/docs/chinh-sach-quyen-rieng-tu.md`, nhưng repo đang **private**: người duyệt Apple/Google và người chơi mở ra sẽ gặp 404. Cách nhanh: tạo một repo public nhỏ (vd. `vuontratho-legal`) bật GitHub Pages, chép `docs/chinh-sach-quyen-rieng-tu.md` vào đó, rồi sửa `AppInfo.PrivacyUrl` và điền cùng URL vào hai console. | Cả hai cửa hàng bắt buộc URL chính sách mở được | CP (tạo repo/trang), session code sửa `AppInfo.cs` |
| B | **Email hỗ trợ thật.** `AppInfo.SupportEmail` đang là `hotro@vuontratho.game` (tên miền chưa chắc đã có). Cần một hộp thư nhận được thư. | Apple/Google gửi thư duyệt và người chơi gửi hỏi tới đó | CP |
| C | **Bản Android phải là AAB, có khoá ký.** `BuildScript.AndroidPath` đang là `Build/Android/VuonTraTho.apk`; Google Play chỉ nhận **.aab** cho app mới. Cần bật `EditorUserBuildSettings.buildAppBundle = true` khi build Android, và khoá upload (keystore) với mật khẩu đọc từ biến môi trường, không commit. | Play Console từ chối APK | session code (sửa `BuildScript`), CP (tạo và giữ keystore) |
| D | **Tên gói Android thống nhất.** `ProjectSettings` đặt `com.vuontratho.game` cho cả iOS và Android; `docs/build-android.md` §3.1 lấy ví dụ `com.vuontratho.moonrabbitteagarden`. Dùng **`com.vuontratho.game`** ở mọi nơi (tên gói không đổi được sau khi tạo app trên Play). | Sai một lần là phải tạo app mới | CP (lúc tạo app) |
| E | **Ảnh chụp đúng cỡ iPhone 6,9" và iPad 13".** Ảnh preview hiện có là 1080×1920 (9:16): dùng thẳng được cho Google Play, nhưng App Store bắt cỡ 1320×2868 (hoặc 1290×2796) cho iPhone và 2064×2752 (hoặc 2048×2732) cho iPad (game đang bật cả iPad: `targetDevice: 2`). Chi tiết ở [anh-cua-hang.md](anh-cua-hang.md). | App Store không nhận cỡ 1080×1920 cho iPhone | Máy |

---

## Bước 0. Chốt quyết định (trước khi đụng console)

| Bước | Việc | Ai | Thời gian | Tài liệu |
|---|---|---|---|---|
| 0.1 | Có bán cà rốt vàng bằng tiền thật ngay bản đầu không (#9). Nếu không: bỏ bước 4 và các câu "mua trong ứng dụng" trong mô tả; nếu có: chốt giá ba gói 50 / 150 / 500. | CP | 15 phút | [build-ios.md §5](../build-ios.md), [build-android.md §5](../build-android.md) |
| 0.2 | Có quảng cáo thưởng không (#66). Khuyên bản đầu **không** (code đang tắt, hai bảng khai báo giữ "No"). Nếu có thì làm lại toàn bộ phần khai báo dữ liệu. | CP | 10 phút | [khai-bao-du-lieu-cua-hang.md §5](../khai-bao-du-lieu-cua-hang.md) |
| 0.3 | Duyệt bản chính sách quyền riêng tư; sửa đoạn **Trẻ em** nếu bật #9 (bản nháp đang ghi "không có mua bằng tiền thật"). | CP | 20 phút | [chinh-sach-quyen-rieng-tu.md](../chinh-sach-quyen-rieng-tu.md) |
| 0.4 | Duyệt tên, mô tả, từ khoá, ảnh, ghi chú phiên bản trong thư mục này. | CP | 30 phút | ba file kèm |
| 0.5 | Số phiên bản: giữ `0.1.0` (build 1) như `BuildScript.Version`, hay đặt `1.0.0` cho bản công khai. Cả hai cửa hàng nhận `0.1.0`. | CP | 2 phút | `Assets/VuonTra/Editor/BuildScript.cs` |

## Bước 1. Tài khoản nhà phát triển

| Bước | Việc | Ai | Thời gian |
|---|---|---|---|
| 1.1 | Đăng ký **Apple Developer Program** (99 USD/năm). Cá nhân: dùng Apple ID có xác thực hai lớp. Tổ chức: cần số D-U-N-S. | CP | 30 phút · *chờ 1–2 ngày* (tổ chức: *1–2 tuần* nếu chưa có D-U-N-S) |
| 1.2 | Đăng ký **Google Play Console** (25 USD một lần), xác minh danh tính và số điện thoại. | CP | 30 phút · *chờ 1–3 ngày* |
| 1.3 | **Lưu ý Google:** tài khoản *cá nhân* mới phải chạy **thử nghiệm kín với ít nhất 12 người thử, liên tục 14 ngày** trước khi được xin lên Production. Bắt đầu tìm 12 tài khoản Gmail ngay từ bây giờ. Tài khoản *tổ chức* không bị luật này. | CP | 1 giờ gom người · *chờ 14 ngày* |
| 1.4 | Apple: ký **Paid Apps Agreement**, điền thuế và ngân hàng (Agreements, Tax, and Banking). Google: tạo **Payments profile**. Chỉ cần nếu bật #9. | CP | 45 phút · *chờ 1–3 ngày* |

## Bước 2. Cài module iOS/Android trên máy build

| Bước | Việc | Ai | Thời gian |
|---|---|---|---|
| 2.1 | Cài **Xcode** (App Store), mở một lần để đồng ý giấy phép và cài thành phần thêm. | Máy | 1 giờ (tải ~10 GB) |
| 2.2 | Unity Hub → Installs → **6000.6.10f1** → Add modules: **iOS Build Support**, **Android Build Support** kèm **OpenJDK** và **Android SDK & NDK Tools**. Hoặc từ Terminal (đóng Unity trước): | Máy | 30–60 phút (tải ~5 GB) |

```bash
"/Applications/Unity Hub.app/Contents/MacOS/Unity Hub" -- --headless install-modules --version 6000.6.10f1 -m ios android android-sdk-ndk-tools android-open-jdk
```

| Bước | Việc | Ai | Thời gian |
|---|---|---|---|
| 2.3 | Kiểm: `ls /Applications/Unity/Hub/Editor/6000.6.10f1/PlaybackEngines` có `iOSSupport` và `AndroidPlayer` (hiện chỉ có `MacStandaloneSupport`, `WebGLSupport`). Chạy `tools/verify.sh` một lần để chắc test vẫn xanh. | Máy | 15 phút |

## Bước 3. Tạo app trên hai console

| Bước | Việc | Ai | Thời gian | Tài liệu |
|---|---|---|---|---|
| 3.1 | Apple: Certificates, Identifiers & Profiles → App ID **`com.vuontratho.game`**, bật **Game Center** và **iCloud (Key-value storage)**. | CP | 10 phút | [build-ios.md §1](../build-ios.md) |
| 3.2 | App Store Connect → My Apps → New App: nền iOS, tên **Vườn Trà Thỏ Trăng** hoặc **Moon Rabbit Tea Garden** (xem [mo-ta-cua-hang.md](mo-ta-cua-hang.md) về ngôn ngữ chính), bundle id trên, SKU tuỳ ý (vd. `vuontratho-001`). Bật Game Center ở Services. | CP | 15 phút | [build-ios.md §1](../build-ios.md) |
| 3.3 | Play Console → Create app: tên, ngôn ngữ mặc định, **Game**, **Free**, đánh dấu hai cam kết. Package name là `com.vuontratho.game` (cố định từ bản AAB đầu tiên). | CP | 15 phút | việc chặn D ở trên |
| 3.4 | Play Console → Play Games Services: tạo, thêm credential Android (SHA-1 của khoá upload và khoá Play App Signing), bật **Saved Games**, thêm tester. | CP | 45 phút | [build-android.md §3](../build-android.md) |

## Bước 4. Mua trong ứng dụng (chỉ khi 0.1 chọn "có")

| Bước | Việc | Ai | Thời gian | Tài liệu |
|---|---|---|---|---|
| 4.1 | App Store Connect: ba sản phẩm **Consumable** `vuontra.golden.50`, `vuontra.golden.150`, `vuontra.golden.500`, tên/mô tả vi+en, giá, ảnh xét duyệt (chụp thẻ "Mua cà rốt vàng"). | CP | 45 phút | [build-ios.md §5](../build-ios.md) |
| 4.2 | Play Console: ba sản phẩm cùng ID, Activate. | CP | 30 phút | [build-android.md §5](../build-android.md) |
| 4.2b | **Gói một lần (#105):** thêm bốn sản phẩm **Non-Consumable** (mua một lần, khôi phục được) trên cả App Store Connect và Play Console, xem bảng ngay dưới. Mã phải đúng từng chữ. | CP | 40 phút | bảng dưới |
| 4.2b | Vé trăng 30 ngày (#106): App Store Connect thêm sản phẩm **Consumable** `vuontra.moonpass.30` (giá tham khảo 109.000 ₫), Play Console thêm **sản phẩm trong ứng dụng** cùng ID. Đây là sản phẩm **tiêu hao mua một lần**, KHÔNG phải đăng ký tự gia hạn (subscription): người chơi tự mua lại khi muốn, mua khi còn hạn thì cộng thêm 30 ngày. Mô tả cửa hàng: "100 cà rốt vàng ngay + 15 mỗi ngày mở game trong 30 ngày (tối đa 550), trần vắng mặt +1 giờ, thêm 1 lượt ×2 miễn phí mỗi ngày; ngày không mở game thì mất thư ngày đó". Ảnh xét duyệt: chụp thẻ "Vé trăng 30 ngày" ở "Mua cà rốt vàng". | CP | 20 phút | [docs/anh/po-106](../anh/po-106/) |
| 4.3 | Unity: bật In-App Purchasing, dán Google Play public key vào Receipt Validation Obfuscator, thêm define `RECEIPT_VALIDATION`, commit `GooglePlayTangle`/`AppleTangle`. | CP+Máy | 30 phút | [build-ios.md §5.4](../build-ios.md) |

### Mã sản phẩm gói một lần (#105)

Bốn gói này chỉ mua được một lần mỗi vườn, nên khai là **Non-Consumable** (App Store) / sản phẩm một lần không tiêu hao (Play); game tự chặn giao lần hai kể cả khi khôi phục giao dịch. Giá trong bảng là giá tham khảo đang dùng trong `FakeStorefront`; giá thật do chủ project đặt.

| Mã sản phẩm | Loại | Tên gợi ý (vi / en) | Nội dung | Giá tham khảo | Khi nào hiện trong game |
|---|---|---|---|---|---|
| `vuontra.starter` | Non-Consumable | Gói người mới / Starter pack | 80 cà rốt vàng, 3 hũ trăng, 3 cỏ bốn lá, mũ Nơ trăng | 25.000 ₫ (bằng gói 50) | cấp vườn 4, đếm ngược 72 giờ |
| `vuontra.milestone.10` | Non-Consumable | Gói mốc cấp vườn 10 / Garden level 10 pack | 150 cà rốt vàng, 5 hũ trăng, 1 thỏ bậc Tím loài chưa gặp | 69.000 ₫ (bằng gói 150) | lần đầu tới cấp 10, 48 giờ |
| `vuontra.milestone.20` | Non-Consumable | Gói mốc cấp vườn 20 / Garden level 20 pack | như trên | 69.000 ₫ | lần đầu tới cấp 20, 48 giờ |
| `vuontra.milestone.30` | Non-Consumable | Gói mốc cấp vườn 30 / Garden level 30 pack | như trên | 69.000 ₫ | lần đầu tới cấp 30, 48 giờ |

Ảnh xét duyệt: `docs/anh/po-105/105-the-giua.png` (thẻ giữa màn) và `105-tiem-goi.png` (đầu Tiệm).

## Bước 5. Build, ký và thử trên máy thật

| Bước | Việc | Ai | Thời gian | Tài liệu |
|---|---|---|---|---|
| 5.1 | `tools/build.sh ios` → mở `Build/iOS/Unity-iPhone.xcodeproj`, chọn Team, chạy trên iPhone thật. Kiểm dòng `[VuonTra] Xcode: Game Center + iCloud` trong `Logs/build-ios.log`. | CP+Máy | 1 giờ | [build-ios.md §2–3](../build-ios.md) |
| 5.2 | Thử theo danh sách iOS: Game Center, iCloud hai máy, Xoá dữ liệu, nhắc tôi, mua sandbox (nếu có #9). | CP | 1–2 giờ | [build-ios.md §3, §5.5, §6](../build-ios.md) |
| 5.3 | Tạo **keystore upload** (giữ bản sao ở nơi an toàn, mất là không cập nhật được app nếu không bật Play App Signing). Sửa việc chặn C. `tools/build.sh android` ra `.aab`. | CP+Máy | 45 phút | việc chặn C |
| 5.4 | Window → Google Play Games → Setup → Android Setup (dán XML resources), Resolve EDM4U, commit file sinh ra. | CP+Máy | 30 phút | [build-android.md §3.5–3.6](../build-android.md) |
| 5.5 | Tải AAB lên **Internal testing**, cài qua Play trên máy Android thật, thử theo danh sách Android. | CP | 1–2 giờ | [build-android.md §3.7, §5.3](../build-android.md) |

## Bước 6. Trang cửa hàng

| Bước | Việc | Ai | Thời gian | Tài liệu |
|---|---|---|---|---|
| 6.1 | Chụp lại ảnh đúng cỡ (việc chặn E), vi và en. | Máy | 1–2 giờ | [anh-cua-hang.md](anh-cua-hang.md) |
| 6.2 | Google Play cần thêm **ảnh nổi bật 1024×500** (feature graphic) — chưa có, phải vẽ/ghép từ tiêu đề và vườn. Icon 512×512 xuất từ `docs/anh/po-41/41-icon-app.png` (1024×1024). | Máy | 1 giờ | [anh-cua-hang.md](anh-cua-hang.md) |
| 6.3 | Dán tên, phụ đề, mô tả, từ khoá, URL chính sách, URL hỗ trợ, email cho cả hai ngôn ngữ. | CP | 45 phút | [mo-ta-cua-hang.md](mo-ta-cua-hang.md) |
| 6.4 | Danh mục: App Store **Games → Simulation**, phụ **Casual**; Google Play **Game → Simulation**. | CP | 5 phút | [mo-ta-cua-hang.md](mo-ta-cua-hang.md) |

## Bước 7. Khai báo dữ liệu, nhóm tuổi, các mục bắt buộc

| Bước | Việc | Ai | Thời gian | Tài liệu |
|---|---|---|---|---|
| 7.1 | App Store → **App Privacy**: "No, we do not collect data". Google → **Data safety**: "No". | CP | 20 phút | [khai-bao-du-lieu-cua-hang.md §1–2](../khai-bao-du-lieu-cua-hang.md) |
| 7.2 | App Store → **Age Rating**: trả lời bảng câu hỏi (dự kiến **4+**). Google → **Content rating** (IARC, dự kiến **Everyone**); nếu bật #9 chọn "In-game purchases (includes random items)". | CP | 20 phút | [khai-bao-du-lieu-cua-hang.md §3](../khai-bao-du-lieu-cua-hang.md), [mo-ta-cua-hang.md](mo-ta-cua-hang.md) |
| 7.3 | Google → **Target audience**: 13 trở lên (tránh chính sách Families). **Ads**: "No, my app does not contain ads". **App access**: mọi chức năng dùng được không cần đăng nhập. Các mục News / Health / Financial / Government: không. | CP | 15 phút | [khai-bao-du-lieu-cua-hang.md §3](../khai-bao-du-lieu-cua-hang.md) |
| 7.4 | Apple **Export Compliance**: game không gọi mạng và không tự mã hoá → trả lời không dùng mã hoá ngoài phạm vi miễn trừ (có thể thêm `ITSAppUsesNonExemptEncryption = NO` vào Info.plist để khỏi hỏi lại mỗi bản). | CP | 5 phút | — |
| 7.5 | Tỉ lệ gọi thỏ: đã hiện trong game (Tiệm và màn Thỏ). Dán câu công bố vào cuối mô tả cửa hàng nếu bật #9. | CP | 5 phút | [mo-ta-cua-hang.md](mo-ta-cua-hang.md) |

## Bước 8. Nộp duyệt

| Bước | Việc | Ai | Thời gian |
|---|---|---|---|
| 8.1 | Xcode → Product → Archive → Distribute → App Store Connect. Trong TestFlight điền "What to Test" từ [ghi-chu-phien-ban.md](ghi-chu-phien-ban.md), mời người thử. | CP+Máy | 45 phút · *chờ xử lý build 15–60 phút; duyệt TestFlight ngoài ~1 ngày* |
| 8.2 | App Store Connect → phiên bản 0.1.0 → chọn build, đính kèm IAP (nếu có, phải nộp cùng bản đầu), ghi chú cho người duyệt (game không cần đăng nhập; Game Center tuỳ chọn) → **Submit for Review**. | CP | 30 phút · *chờ duyệt thường 1–2 ngày* |
| 8.3 | Play Console: Internal → **Closed testing** (12 người × 14 ngày nếu tài khoản cá nhân mới) → **Apply for production** → Production release với ghi chú phiên bản. | CP | 30 phút · *chờ 14 ngày thử kín + duyệt production vài ngày* |
| 8.4 | Sau khi lên: comment ngày phát hành và link cửa hàng lên #41, đóng #41/#42. | CP | 5 phút |

---

**Tổng ước:** khoảng 2 ngày làm tay (rải rác), cộng thời gian chờ. Đường găng là Google: tài khoản cá nhân mới mất tối thiểu ~3 tuần (xác minh + 14 ngày thử kín + duyệt). iOS có thể lên trong ~1 tuần kể từ khi có tài khoản.

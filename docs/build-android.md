# Bản build Android: Google Play Games, Saved Games và mua cà rốt vàng (#40, #9)

Phần code của #40 đã có trên nhánh `agent-40`; máy phát triển hiện không có module Android nên **chưa chạy thử trên máy thật**. Trang này ghi đúng những việc chủ project phải làm để bản Android đăng nhập Play Games và lưu vườn lên Saved Games.

## 1. Gói trong project (đã làm)

`Packages/manifest.json` có hai gói của Google, cài thẳng từ GitHub qua Unity Package Manager (cần `git` trên máy build):

```json
"com.google.external-dependency-manager": "https://github.com/googlesamples/unity-jar-resolver.git?path=upm#v1.2.190",
"com.google.play.games": "https://github.com/playgameservices/play-games-plugin-for-unity.git?path=/Assets/Public/GooglePlayGames/com.google.play.games#v2.3.0",
```

- `com.google.play.games` là Google Play Games Plugin for Unity v2 (bản 2.3.0, 29/9/2026). Gói khai báo phụ thuộc `com.google.external-dependency-manager` (EDM4U), nên EDM4U được ghim cùng; UPM báo gói EDM4U từ GitHub là "deprecated" và gợi ý `com.unity.external-dependency-manager`, nhưng gói Play Games vẫn đòi đúng tên `com.google.external-dependency-manager`, nên giữ như trên.
- Trên máy Mac (không có module Android) editor vẫn biên dịch và test xanh: assembly `Google.Play.Games` rỗng ngoài Android, và mọi chỗ gọi plugin trong game nằm sau `#if UNITY_ANDROID && !UNITY_EDITOR && VUONTRA_PLAY_GAMES`.
- `VUONTRA_PLAY_GAMES` do `Assets/VuonTra/Game/VuonTra.Game.asmdef` tự bật khi gói `com.google.play.games` ≥ 2.0.0 có trong project (mục `versionDefines`). Gỡ gói thì bản Android quay về chơi như khách, không lỗi biên dịch; nhưng phải bỏ luôn dòng `"Google.Play.Games"` trong `references` của asmdef đó.

## 2. Code (đã làm)

| File | Vai trò |
|---|---|
| `Assets/VuonTra/Game/Platform/IPlayGames.cs` | Lớp giao tiếp nhỏ: SignIn, DisplayName, Open, Read, Commit, Delete. Test dùng bản giả; máy thật dùng plugin. |
| `Assets/VuonTra/Game/Platform/PlayGamesPlugin.cs` | Bộ chuyển sang plugin thật (`PlayGamesPlatform.Instance`, `ISavedGameClient`). Chỉ biên dịch trên Android máy thật có gói. |
| `Assets/VuonTra/Game/Platform/PlayGamesAccount.cs` | `IAccountService`: đăng nhập lặng khi mở game (`Authenticate`), nút Liên kết gọi `ManuallyAuthenticate`, nhớ lần đăng nhập thành công trong PlayerPrefs (`playgames.linked`), tên vườn mặc định `NameFor`. |
| `Assets/VuonTra/Game/Platform/SavedGamesCloudSave.cs` | `ICloudSave`: một snapshot tên `vuon`, mô tả "Cấp vườn N · M thỏ", ảnh bìa là ảnh chụp vườn mới nhất (≤ 800 KB), thời gian chơi lấy từ save; từ chối bản lưu quá 3 MB; không ghi đè bản chưa đọc được. |
| `Assets/VuonTra/Game/Platform/AndroidServices.cs` | `Install(GameRunner)`: cắm hai lớp trên vào runner, chỉ trên Android. Được gọi một dòng trong `GameRunner.Init` ngay trước lần đồng bộ mở game. |

Xung đột snapshot giữa hai máy: `OpenWithAutomaticConflictResolution` với `UseLongestPlaytime`. Thời gian chơi gửi lên chính là `PlaySeconds` trong save, nên bản chơi nhiều hơn thắng, cùng tinh thần luật chọn bản của #38; sau đó `CloudSync` vẫn cho người chơi thấy hai bản nếu khác nhau.

## 3. Việc chủ project phải làm (Play Console)

1. **Google Play Console**: tạo app (package name trong Player Settings, ví dụ `com.vuontratho.moonrabbitteagarden`), bật **Grow users → Play Games Services → Setup and management → Configuration**.
2. **Credential**: trong Configuration, **Add credential → Android**, chọn hoặc tạo **OAuth client** trong Google Cloud cho đúng package name và **SHA-1** của khóa ký (khóa upload của Keystore manager, và khóa Play App Signing nếu bật). Mỗi khóa một credential.
3. **Saved Games**: trong Configuration, bật **Saved Games** (mục Properties / Game details) rồi lưu. Không bật thì `OpenWithAutomaticConflictResolution` trả lỗi và game chơi như khách.
4. **Tester**: thêm tài khoản Google thử nghiệm vào mục Testers của Play Games Services khi app chưa publish.
5. **Trong Unity** (cần module Android): **Window → Google Play Games → Setup → Android Setup**. Dán nội dung "Android (XML)" từ **Get resources** của Play Console vào ô Resources definition (ít nhất có `app_id`), bấm Setup. Việc này ghi `app id` vào `GooglePlayGamesSettings` (`Assets/GooglePlayGames/Resources/`), sinh hằng số và `AndroidManifest` cho plugin. Commit các file sinh ra.
6. **Player Settings**: Scripting backend IL2CPP, ARM64, Target API theo yêu cầu Google Play hiện hành. **Assets → External Dependency Manager → Android Resolver → Resolve** để kéo `play-services-games-v2` về (cần mạng).
7. Build AAB, lên **Internal testing**, cài trên máy thật và kiểm:
   - mở game: tự đăng nhập, Hồ sơ hiện "Đã liên kết · <tên Play Games>", vườn chưa đặt tên lấy tên Play Games;
   - Hồ sơ → Lưu ngay: trạng thái "Đã lưu lên mây lúc …"; trong app Play Games, mục Saved games của game có một bản với ảnh bìa và dòng "Cấp vườn N · M thỏ";
   - cài lại game hoặc máy thứ hai: mở game thấy tấm chọn bản hoặc tự lấy bản trên mây;
   - tắt mạng rồi Lưu ngay: "Không có mạng, sẽ lưu khi có lại", không mất gì;
   - Xoá dữ liệu của tôi: bản trên Saved Games biến mất.

## 4. Chưa làm / cần lưu ý

- Chưa chạy trên máy thật: luồng plugin (mở snapshot hai lần cho đọc rồi ghi, callback về đúng thread, `Delete` không có callback) chỉ mới theo tài liệu.
- Ảnh chụp vườn là PNG toàn màn hình, thường quá 800 KB; khi đó snapshot không có ảnh bìa. Nếu muốn luôn có bìa thì cần thu nhỏ ảnh trước khi gửi.
- Thành tựu và bảng xếp hạng (phần "về sau" của #40) chưa làm.
- Tài khoản đăng nhập lặng không tính là "liên kết" để nhận quà 3 cỏ bốn lá; chỉ nút Liên kết trong Hồ sơ mới trả quà, như luật của #38.

## 5. Mua cà rốt vàng bằng tiền thật (#9)

Code đã có: `Core/Storefront.cs` (ba gói consumable `vuontra.golden.50`, `vuontra.golden.150` (gói hời), `vuontra.golden.500`), `Game/Store/UnityStorefront.cs` (Unity IAP 4.15, chỉ biên dịch khi có `UNITY_PURCHASING` và nền iOS/Android; `GameRunner` lắp nó trên máy thật, còn editor, test và bản Mac dùng cửa hàng giả với giá giữ chỗ).

1. **Play Console → Monetize → Products → In-app products**: tạo ba sản phẩm với đúng ba Product ID trên, tên, mô tả, giá; **Activate** từng cái. Cần tài khoản thanh toán (Payments profile) đã liên kết.
2. **Google Play public key**: Monetize → Monetization setup → **Licensing**, chép khoá Base64 RSA. Trong Unity: Services → In-App Purchasing → **Receipt Validation Obfuscator**, dán khoá, Obfuscate → sinh `GooglePlayTangle`/`AppleTangle`; rồi thêm define **`RECEIPT_VALIDATION`** (Player Settings → Scripting Define Symbols) để biên lai được kiểm; chưa có tangle thì define tắt và mọi biên lai được chấp nhận (chỉ khi thử).
3. **Build và thử**: sản phẩm chỉ mua được từ bản tải qua Play (Internal testing là đủ), cùng tài khoản là **License tester** (Settings → License testing) để không bị tính tiền thật. Kiểm: giá hiện đúng tiền tệ trên thẻ "Mua cà rốt vàng"; mua một gói cộng đúng một lần; đóng app giữa chừng rồi mở lại: giao dịch dang dở tự giao; "Khôi phục mua hàng" không cộng lặp.
4. Gói `com.unity.purchasing` 4.15.0 đã có trong `Packages/manifest.json`; không cần EDM4U cho IAP (Unity IAP tự mang thư viện Billing).
5. **Biên lai bị từ chối** (khi bật `RECEIPT_VALIDATION`): game báo "mua không thành" nhưng KHÔNG hoàn tất giao dịch (`PurchaseProcessingResult.Pending`), nên lần mở app sau Google Play đưa lại giao dịch đó để kiểm biên lai lần nữa; tiền không bị nuốt (Play tự hoàn tiền sau 3 ngày nếu vẫn không được xác nhận).

## 6. Nhắc tôi: thông báo tại máy (#65)

- Gói `com.unity.mobile.notifications` 2.5.0 đã có trong `Packages/manifest.json`; `VuonTra.Game.asmdef` bật `VUONTRA_NOTIFICATIONS` khi có gói; code gọi gói nằm trong `MobileNotifier` (`Assets/VuonTra/Game/Platform/Notifications.cs`), chỉ biên dịch trên máy thật.
- Kênh thông báo `reminders` ("Nhắc nhở" / "Reminders") được tạo lần đầu cần dùng. Android 13 trở lên: quyền `POST_NOTIFICATIONS` do gói thêm vào manifest; game chỉ hỏi khi người chơi bật "Nhắc tôi".
- Project Settings → Mobile Notifications → Android: để mặc định (không cần *Reschedule on Device Restart*, vì danh sách được đăng ký lại mỗi lần game vào nền; khởi động lại máy thì mất nhắc cho tới lần chơi sau). Có thể đặt icon nhỏ riêng ở đó; không đặt thì dùng icon app.
- Thử trên máy (sau #41) như mục iOS.

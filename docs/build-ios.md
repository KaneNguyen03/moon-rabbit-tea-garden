# Bản iOS: Game Center và iCloud (#39, #41)

Máy đang dùng chỉ có module Mac và WebGL, nên phần dưới đây là việc chủ project làm trên máy có module iOS và Xcode. Code đã sẵn: `Assets/VuonTra/Game/Platform/` (Game Center, iCloud key-value store, nối vào `GameRunner`), plugin native `Assets/Plugins/iOS/CloudKvs.mm`, và bước hậu kỳ Xcode `IOSCapabilities` trong `Assets/VuonTra/Editor/BuildScript.cs`.

## 1. Tài khoản nhà phát triển (một lần)

1. App Store Connect → ứng dụng với bundle id `com.vuontratho.game` (đã đặt ở #41).
2. Certificates, Identifiers & Profiles → Identifiers → App ID `com.vuontratho.game` → bật hai capability:
   - **Game Center**
   - **iCloud** (chỉ cần *Key-value storage*; không cần container CloudKit)
3. App Store Connect → ứng dụng → Services → **Game Center**: bật cho ứng dụng (không cần tạo leaderboard hay achievement; game chỉ dùng đăng nhập và tên người chơi).

## 2. Build

```
tools/build.sh ios          # Unity -executeMethod VuonTra.EditorTools.BuildScript.BuildIOS
```

Sau khi Unity xuất project Xcode ở `Build/iOS`, `IOSCapabilities.OnPostprocessBuild` tự:

- thêm capability **Game Center** (GameKit.framework) vào target Unity-iPhone;
- thêm capability **iCloud** kiểu key-value store, tức entitlement
  `com.apple.developer.ubiquity-kvstore-identifier = $(TeamIdentifierPrefix)$(CFBundleIdentifier)`;
- ghi file `Build/iOS/Unity-iPhone/VuonTraTho.entitlements` và trỏ `CODE_SIGN_ENTITLEMENTS` tới nó.

Nếu bước này không chạy (xem `Logs/build-ios.log` thiếu dòng `[VuonTra] Xcode: Game Center + iCloud`), làm tay trong Xcode: target Unity-iPhone → Signing & Capabilities → `+ Capability` → Game Center; `+ Capability` → iCloud → tick *Key-value storage*.

## 3. Ký và chạy thử

1. Mở `Build/iOS/Unity-iPhone.xcodeproj`, chọn Team; Xcode tự tạo provisioning profile có hai capability trên.
2. Chạy trên máy thật (simulator không có Game Center thật và iCloud KVS không đồng bộ).
3. Những điều cần thấy:
   - Mở game: tấm chào Game Center hiện lần đầu; Hồ sơ → Sao lưu ghi "Đang liên kết Game Center…" rồi "Đã liên kết · <tên GC>"; mở lại game thì ghi "Đã liên kết" ngay, không chờ.
   - Vườn chưa đặt tên lấy tên Game Center làm tên vườn một lần; vườn đã đặt tên thì giữ nguyên.
   - Dòng mây: "Chưa lưu lên mây" → "Đã lưu lên mây lúc …" sau khi mở game hoặc bấm Lưu ngay. Máy chưa đăng nhập iCloud: "Đăng nhập iCloud trong Cài đặt của máy để lưu lên mây".
   - Hai máy cùng Apple ID: chơi trên máy A, mở game trên máy B → tấm "Hai bản vườn khác nhau" hoặc tự lấy bản tiến xa hơn theo luật #38; máy B ghi vào mây thì máy A (đang mở) đồng bộ trong vòng vài giây (`PlatformServices.Tick` hỏi store mỗi 5 giây).
   - Hồ sơ → Xoá dữ liệu của tôi: khoá trên iCloud bị xoá.

## 4. Giới hạn đã tính

- iCloud key-value store: 1 MB cho cả store và 1 MB cho một khoá (Apple đếm 1 000 000 byte). Game dùng một khoá `vuontra.save`. Test `PlatformTests.A_Full_Garden_Wrapped_For_The_Cloud_Stays_Far_Under_The_iCloud_Limit` dựng vườn tối đa (12 ô đất, 12 trạm cấp 500, 60 thỏ có đồ, album 12 ảnh, mọi nhiệm vụ, đơn hàng, trang trí) và buộc phong bì dưới 512 KB; kích thước đo được ghi trong `TestResults.xml` (output của test).
- Bản lưu vượt giới hạn: không gửi, dòng mây ghi "Bản lưu quá lớn cho iCloud, chưa gửi lên", bản trên máy nguyên.
- Game Center không có đăng xuất: "bỏ liên kết" chỉ quên trên máy (`GameCenterAccount.Unlink`), chưa có nút trên màn hình.

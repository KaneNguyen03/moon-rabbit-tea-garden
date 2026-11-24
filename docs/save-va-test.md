# Save của người chơi và chạy test

## Save chính thức

Trên Mac, cả bản build (`Build/Mac/VuonTraTho.app`) lẫn Unity Editor đều lưu vào **một** tệp:

```
~/Library/Application Support/VuonTraTho/Moon Rabbit Tea Garden/save.json
```

Thư mục này là `Application.persistentDataPath`, dựng từ `companyName` (`VuonTraTho`) và `productName` (`Moon Rabbit Tea Garden`) trong `ProjectSettings`. Player.log của bản build nằm ở thư mục tương ứng `~/Library/Logs/VuonTraTho/Moon Rabbit Tea Garden/`. Bên cạnh `save.json` có thể có `save.backup.json` (bản lưu trước đó) và `save.bad.json` (bản hỏng được giữ lại). Người chơi thử cần sao lưu thư mục này.

Mã game chỉ ghi save qua `SaveStore` (`Assets/VuonTra/Game/SaveStore.cs`); thư mục mặc định là `SaveStore.DefaultDirectory`, tức `persistentDataPath`. Không có đường ghi save nào khác.

## Các thư mục khác trên máy (tàn dư, không có mã nào ghi vào)

| Thư mục | Là gì |
|---|---|
| `~/Library/Application Support/com.vuontratho.game/` | Save phiên bản 8; thư mục tạo lúc 21/11 22:40 (cùng lúc commit `615bcf7` đặt bundle id), tệp ghi lần cuối 22/11 22:52. Mã hiện tại không đọc cũng không ghi tệp này. Giữ lại: trong đó trạm ấm trà ở cấp 125, có thể dùng nếu chủ project muốn khôi phục một phần sau sự cố 25/11. |
| `~/Library/Application Support/VuonTraTho/Vườn Trà Thỏ Trăng/` | Thư mục của `productName` cũ, chỉ còn một `TestResults.xml` cũ. |
| `~/Library/Application Support/VuonTraTho/QA Perf Garden/` | Save của một bản build đo hiệu năng của QA (`productName` riêng). |
| `~/Library/Application Support/VuonTraTho/save-incident-25-11.json` | Bản giữ lại của save sau sự cố 25/11 03:21. |

## Chạy test không đụng vào save

- Test EditMode lưu vào `temporaryCachePath/tests/<tên test>` qua `Assets/Tests/EditMode/TestSaveFolder.cs` (`TestRunCallback`) và xoá sau mỗi test.
- `tools/verify.sh` ghi kết quả vào `TestResults.xml` trong thư mục project, và bước `save` báo FAIL nếu `save.json` của người chơi đổi trong lúc chạy.
- Chạy một nhóm test: `tools/test.sh <bộ lọc>` (ví dụ `tools/test.sh SaveStoreTests`). Kết quả vào `Logs/TestResults-filter.xml`, log vào `Logs/test-filter.log`.
- Sau mỗi lần chạy test, gói `com.unity.test-framework.performance` (đi kèm `com.unity.collections`, game không dùng) luôn ghi `TestResults.xml` và có thể cả `PerformanceTestResults.json` vào thư mục save của người chơi, dù có truyền `-testResults` hay không. Hai tệp này không phải save. `tools/verify.sh` và `tools/test.sh` xoá chúng sau mỗi lần chạy; chạy test từ cửa sổ Test Runner hoặc MCP thì tệp vẫn còn, không hại gì cho save.
- `tools/verify.sh --build` mở bản build vài giây trên save thật. Đóng game trước, và chạy qua gate (`/tmp/unity-gate.sh`), vì gate sao save trước khi chạy rồi trả lại nếu lần chạy đã đổi nó.

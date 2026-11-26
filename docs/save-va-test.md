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
- `tools/verify.sh --build` mở bản build vài giây trên save thật. Đóng game trước. Lệnh tự đi qua gate (mục dưới), và gate giữ một bản sao save từ trước lần chạy.

## Hàng đợi Unity: `tools/unity-gate.sh`

Mọi checkout dùng chung một máy Mac, nên mọi lần chạy Unity xếp chung một hàng. `tools/verify.sh`, `tools/test.sh` và `tools/build.sh` tự gọi gate; không cần và không nên gõ thẳng đường dẫn Unity.

| Lệnh | Làm gì |
|---|---|
| `tools/unity-gate.sh status` | Ai đang giữ Unity, giữ bao lâu, ai đang chờ, chờ bao lâu, giới hạn hiện tại |
| `tools/unity-gate.sh <ưu tiên> <lệnh>` | Chờ tới lượt rồi chạy lệnh. Số nhỏ đi trước; cùng số thì ai tới trước đi trước |
| `UNITY_GATE_PRIORITY=5 tools/verify.sh` | Đổi mức ưu tiên của một tool (mặc định 50) |
| `UNITY_GATE_MAX_WAIT=1800 tools/test.sh <bộ lọc>` | Bỏ cuộc với mã thoát 75 nếu sau số giây đó chưa có lượt; lệnh không chạy |

Gate bảo đảm:

- Số lần chạy cùng lúc không vượt giới hạn. Giới hạn (0 đến 3) tính theo pin, nguồn sạc, swap và bộ nhớ trống; tệp `/tmp/unity-gate-rules.sh`, nếu có, thay cách tính này (B dùng để chỉnh khi máy đang chạy).
- Tiến trình giữ lượt bị kill thì lượt được trả; tiến trình đang chờ bị kill thì chỗ của nó trong hàng bị xoá. Khoá nội bộ bị bỏ lại được phá sau 2 phút, nên một tiến trình bị `kill -9` đúng lúc giữ khoá có thể làm cả hàng chậm tối đa 2 phút.
- Trước mỗi lần chạy, gate sao `save.json` của người chơi vào `/tmp/save-guard/`. Nếu save đổi trong lúc chạy, gate giữ cả bản trước lẫn bản sau, ghi một dòng vào `/tmp/save-guard/changed.log` và báo ra stderr. Gate **không** tự ghi đè save; muốn trả lại thì chép bản trước về bằng tay.
- Lệnh chạy bên trong một lượt đang giữ (ví dụ `tools/unity-gate.sh 5 tools/verify.sh`) không xếp hàng lần hai.

Trạng thái hàng đợi nằm trong `/tmp` (`unity-wait`, `unity-running`, `unity-gate-info`, `unity-gate.lock`), dùng chung với script cũ `/tmp/unity-gate.sh`, nên lần chạy qua script cũ và qua `tools/unity-gate.sh` vẫn loại trừ nhau. Máy khởi động lại thì `/tmp` trống và hàng đợi bắt đầu lại từ đầu; gate nằm trong repo nên không mất. Gate cần thấy tiến trình khác và ghi được vào `/tmp`: trong sandbox của Claude Code nó dừng với mã thoát 73, hãy chạy lệnh ngoài sandbox. Tự kiểm của gate: `python3 -I tools/test_unity_gate.py` (không cần Unity).

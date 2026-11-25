# ADR-0003: Mua hàng, quảng cáo, analytics và lưu mây đi qua interface Core; mặc định là bản giả hoặc không làm gì, SDK thật là adapter trong `Game`

> **Reverse-documented** from code and history at commit 9663644 on 2026-11-26; rationale not stated in docs is marked INTENT UNKNOWN.

## Status

Accepted

## Date

2026-11-22: seam cửa hàng `IStorefront` (`cdde124`, #9) và lõi lưu mây (`d88b60c`), adapter Game Center/iCloud (`59d075c`) và Google Play Saved Games (`6ae4d55`). 2026-11-23: seam quảng cáo `IRewardedAds` (`6a544ac`, #66). 2026-11-24: seam analytics `IAnalytics` (`2a0ec7c`, #100), ghi rõ "theo mẫu seam quảng cáo #66" (`docs/bao-cao-po.md`).

## Last Verified

2026-11-26

## Decision Makers

Chủ project (duyệt từng issue #9, #66, #100) và session code. Chọn SDK quảng cáo và công cụ analytics vẫn là việc chủ project chưa quyết (`docs/phat-hanh/LO-TRINH-KIEM-TIEN.md`, "Chủ project cần quyết ngay").

## Summary

Game cần mua hàng, quảng cáo thưởng, analytics và lưu mây, nhưng chưa có tài khoản cửa hàng, chưa chọn SDK quảng cáo, và Core không được biết Unity (ADR-0001). Mỗi dịch vụ là một interface trong Core với bản giả cho Editor/test và bản "không làm gì" cho bản build người chơi; SDK thật chỉ là adapter trong `Assets/VuonTra/Game/Store` hoặc `Game/Platform`, biên dịch theo define và nền tảng.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1 |
| **Domain** | Core |
| **Layer** | Foundation |
| **Knowledge Risk** | MEDIUM: Unity IAP 4.15 (`IDetailedStoreListener`, `UnityPurchasing.Initialize`) là API của IAP 4; IAP 5 dùng `StoreController` |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `Packages/manifest.json`, `docs/bao-cao-po.md` mục #9, #66, #100 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Mua thử sandbox trên máy thật iOS và Android (chưa làm, cần tài khoản); khi nâng IAP 5 phải viết lại `UnityStorefront` |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0001 |
| **Enables** | ADR-0004 |
| **Blocks** | Epic cắm SDK quảng cáo thật (#66 bước SDK), analytics thật, phát hành mềm |
| **Ordering Note** | Adapter cửa hàng và quảng cáo chỉ được tạo khi cờ của ADR-0004 bật |

## Context

### Problem Statement

Tính năng tiền thật và dịch vụ ngoài phải code và test trước khi có tài khoản Apple/Google, SDK quảng cáo hay Firebase, và không được gửi gì từ bản build người chơi khi chưa được duyệt.

### Current State

| Seam (Core) | Bản giả / mặc định | Adapter thật (Game) | Khi nào dùng adapter |
|---|---|---|---|
| `IStorefront` (`Core/Storefront.cs`): `Ready`, `PriceOf`, `Buy`, `Restore`, `event Delivered` | `FakeStorefront` (giá giữ chỗ), mặc định của getter `GameRunner.Store` | `Game/Store/UnityStorefront.cs` (Unity IAP 4.15, `IReceiptCheck`; `TangleReceiptCheck` chỉ khi có `RECEIPT_VALIDATION`) | `UNITY_PURCHASING`, iOS/Android, không phải Editor, và `Features.Monetization`, trong `GameRunner.Awake` |
| `IRewardedAds` (`Core/Ads.cs`): `Ready`, `Show` | `FakeRewardedAds` (Editor, test), `NoAds` | Không có | Chưa có SDK; bản người chơi dùng `NoAds` trừ khi `GameRunner.AdsInPlayerBuilds` (đang `false`) |
| `IAnalytics` (`Core/Analytics.cs`): `Log`, `SetUserProperty`, `RecordException` | `MemoryAnalytics` (Editor, test), `NoAnalytics` | `Game/Platform/FirebaseAnalyticsAdapter.cs` | Chỉ biên dịch với `VUONTRA_FIREBASE` trên iOS/Android, và chỉ khi `GameRunner.AnalyticsInPlayerBuilds` (đang `false`) |
| `ICloudSave` (`Core/Profile.cs`) + `CloudSync` (`Core/CloudSave.cs`) | `LocalOnlyCloudSave`, `MemoryCloudSave` (test) | `ICloudKvsCloudSave` (iOS), `SavedGamesCloudSave` (Android) | `PlatformServices.Install` (iOS thiết bị), `AndroidServices.Install` (`VUONTRA_PLAY_GAMES`) |

Cùng mẫu cho rung (`IHapticMotor`), hỏi đánh giá (`IReviewPrompt`) và Play Games (`IPlayGames`). Gói Firebase không có trong `Packages/manifest.json`, nên mọi bản build hiện nay không gửi analytics (`docs/phat-hanh/PHEU-SU-KIEN.md`).

### Constraints

- Chưa có tài khoản Apple Developer/Google Play Console; chưa chọn SDK quảng cáo (`LO-TRINH-KIEM-TIEN.md` đề xuất AdMob; báo cáo #66 ghi "Chưa cài SDK LevelPlay").
- Mỗi giao dịch chỉ cộng một lần (`Storefront.Grant`, save giữ 64 mã gần nhất).

### Requirements

- Toàn bộ luồng mua, xem quảng cáo, ghi sự kiện, đồng bộ mây chạy được trong Editor và test.
- Bản build người chơi không gửi dữ liệu khi chưa bật công tắc.

## Decision

Interface và luật nằm trong Core; `GameRunner` giữ một thuộc tính cho mỗi seam (`Store`, `AdProvider`, `Analytics`, `Cloud`), getter tự tạo bản mặc định an toàn, setter nhận `null` thì quay về bản an toàn. Adapter SDK nằm trong `Game` sau `#if` theo nền tảng và define gói.

### Architecture Diagram

```
Core:  IStorefront      IRewardedAds     IAnalytics                 ICloudSave
         Fake             Fake / No        Memory / No                LocalOnly / Memory
Game:  UnityStorefront  (none yet)       FirebaseAnalyticsAdapter   ICloudKvsCloudSave / SavedGamesCloudSave
       GameRunner.Store / AdProvider / Analytics / Cloud  (tests set fakes through the setters)
```

### Key Interfaces

Như bảng trên; test thay bản giả qua setter (ví dụ `runner.Store = new FakeStorefront { ... }`).

### Implementation Guidelines

- SDK bên thứ ba must chỉ được tham chiếu trong `Assets/VuonTra/Game/Store` hoặc `Game/Platform`, sau `#if` của define gói.
- Core must never gọi SDK; nó chỉ thấy interface.
- Thuộc tính seam trên `GameRunner` must không bao giờ trả `null`.
- Dịch vụ gửi dữ liệu ra ngoài must mặc định tắt trong bản build người chơi (`AdsInPlayerBuilds`, `AnalyticsInPlayerBuilds`, `Features.Monetization`).
- Seam mới must theo cùng mẫu: interface Core, bản giả Core, adapter Game.

## Alternatives Considered

Không có phương án thay thế nào được ghi lại cho kiến trúc seam. Tên SDK quảng cáo (AdMob hay LevelPlay) là lựa chọn nhà cung cấp đang chờ chủ project, không phải phương án kiến trúc.

## Consequences

### Positive

- Mua hàng, quảng cáo, analytics, lưu mây có test Core và test màn hình (`StorefrontTests`, `AdsTests`, `AdSeamsScreenTests`, `AnalyticsTests`, `CloudSaveTests`, `PlatformTests`) không cần mạng.
- Bản build người chơi hiện không gửi gì; bản khai dữ liệu cửa hàng giữ đơn giản.

### Negative

- Bật `AdsInPlayerBuilds` trong bản người chơi sẽ dùng `FakeRewardedAds` (không có adapter thật); công tắc này chưa an toàn để bật.
- Ba công tắc khác nhau (`AdsInPlayerBuilds`, `AnalyticsInPlayerBuilds`, `Features.Monetization`) phải bật đúng tổ hợp.

### Neutral

- `GameRunner.Update` gọi `(_adProvider as FakeRewardedAds)?.Advance(dt)`: đồng hồ bản giả chạy theo khung.

## Risks

- IAP 4.15 cũ đi; giảm thiểu: viết lại `UnityStorefront` theo IAP 5 khi có tài khoản (`docs/bao-cao-po.md` #9).
- `AcceptAllReceipts` nhận mọi hoá đơn có nội dung tới khi bật `RECEIPT_VALIDATION`; giảm thiểu: tạo tangle trước khi phát hành.

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| [tiem-va-ca-rot-vang.md](../../design/gdd/tiem-va-ca-rot-vang.md) | Ba gói cà rốt vàng, khôi phục giao dịch | `IStorefront`, `Storefront.Packs`, `Storefront.Grant` |
| [di-vang.md](../../design/gdd/di-vang.md) | ×2 đi vắng qua video | `IRewardedAds`, chỗ `Ads.AwayDouble` |
| [tro-choi-nho.md](../../design/gdd/tro-choi-nho.md) | Thêm lượt qua video | chỗ `Ads.ExtraRound` |
| [co-kiem-tien.md](../../design/gdd/co-kiem-tien.md) | Không tạo storefront hay nhà cung cấp video khi tắt | Getter mặc định an toàn; ADR-0004 chặn tạo adapter |

GDD đang viết song song; đối chiếu lại khi xong.

## Performance Implications

- **CPU**: không đáng kể; chỉ `FakeRewardedAds.Advance` mỗi khung.
- **Memory**: SDK chỉ nạp khi adapter được tạo.
- **Load Time**: `UnityPurchasing.Initialize` chỉ chạy trên thiết bị khi cờ bật.
- **Network**: không có trong mọi bản build hiện nay.

## Migration Plan

Cắm SDK quảng cáo: thêm adapter `IRewardedAds` trong `Game/Platform` sau define gói, đổi `GameRunner.DefaultAds()` để trả adapter đó thay cho `FakeRewardedAds` trong bản người chơi.

**Rollback plan**: gán lại bản `No*` qua setter hoặc tắt công tắc tương ứng.

## Validation Criteria

- `grep -rE "UnityEngine\.Purchasing|Firebase|GooglePlayGames" Assets/VuonTra/Core` không có kết quả.
- Các test liệt kê ở Consequences xanh trong `tools/verify.sh`.
- `GameRunner.AdsInPlayerBuilds` và `GameRunner.AnalyticsInPlayerBuilds` còn `false` cho tới khi có quyết định mới.

## Related

- ADR-0001, ADR-0004.
- `docs/khai-bao-du-lieu-cua-hang.md`, `docs/chinh-sach-quyen-rieng-tu.md`, `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md`, `docs/phat-hanh/PHEU-SU-KIEN.md`.

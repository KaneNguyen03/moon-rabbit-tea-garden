# ADR-0004: Một cờ `Features.Monetization` trong Core bật/tắt mọi luồng tiền thật và quảng cáo, mặc định tắt

> **Reverse-documented** from code and history at commit 9663644 on 2026-11-26; rationale not stated in docs is marked INTENT UNKNOWN.

## Status

Accepted

## Date

2026-11-25 (commit `457d960`, cherry-pick của `8ac9d13` lên main `a2f5c08`; issue KaneNguyen03/moon-rabbit-tea-garden#144, đã đóng).

## Last Verified

2026-11-26

## Decision Makers

Chủ project, 25/11: "tạm thời cái luồng kiếm tiền thì làm 1 cái cờ để tắt bật tính năng này. Làm cái cờ này trước, khoan hẳn làm tiếp tính năng kiếm tiền, và trạng thái default là tắt." (thân issue #144). Code: agent-144. Issue mang nhãn `chờ-PO-duyệt`.

## Summary

Chủ project muốn dừng luồng kiếm tiền mà không xoá mã đã làm (gói, vé, quảng cáo, giá). Một thuộc tính tĩnh `Features.Monetization` trong `Assets/VuonTra/Core/Features.cs`, mặc định `false` trừ khi bản build có define `VUONTRA_MONETIZATION`, quyết định có tạo storefront/nhà cung cấp video, có hiện chỗ mua, có ghi sự kiện mua hay không.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1 |
| **Domain** | Core |
| **Layer** | Core |
| **Knowledge Risk** | LOW (chỉ dùng scripting define qua `BuildScript`) |
| **References Consulted** | `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md` mục "Cờ bán hàng (#144)", `docs/bao-cao-po.md` mục #144 |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Bản build có `VUONTRA_MONETIZATION=1` hiện lại đúng mọi chỗ mua (chưa chạy trên máy thật) |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | ADR-0003 (cờ chặn việc tạo adapter của các seam), ADR-0001 |
| **Enables** | None |
| **Blocks** | Mọi việc kiếm tiền mới (#66 SDK, #105, #107 hàng trả tiền) chờ chủ project bật lại |
| **Ordering Note** | Cờ đứng trước luật nhịp thẻ của #115 |

## Context

### Problem Statement

Mã kiếm tiền (#9 gói cà rốt vàng, #66 quảng cáo, #105 gói ưu đãi, #106 Vé trăng, #107 Vé tháng) đã có, nhưng chủ project chưa muốn bán. Cần tắt hết bằng một chỗ, giữ nguyên quyền lợi người đã trả.

### Current State

- `Features.cs`: `MonetizationByDefault` là `const` theo `#if VUONTRA_MONETIZATION`; `public static bool Monetization { get; set; } = MonetizationByDefault;`.
- `Editor/BuildScript.cs`: `MonetizationDefine = "VUONTRA_MONETIZATION"`, thêm define khi biến môi trường `VUONTRA_MONETIZATION=1` (`VUONTRA_MONETIZATION=1 tools/build.sh android`).
- Chỗ đọc cờ:
  - Core: `Analytics.cs` (`AnalyticsEvents.Sends`: 10 sự kiện trong `AnalyticsEvents.Monetization` không ghi khi tắt), `Intros.cs` (thẻ giới thiệu Vé tháng), `MoonPass.cs` (không thư nhắc mua lại).
  - Game: `GameRunner.cs` (tạo `UnityStorefront` trong `Awake`, `PriceOf`, `DefaultAds`, `AdsOn`, `AdOfferFor`), `GameRunner.Offers.cs` (gói ưu đãi, đồng hồ gói), UI `ShopView`, `GoldenPacksView`, `OfferView`, `MonthPassView`.
  - Editor: `PreviewShot.cs`, `PreviewShot.FirstSession.cs` (khung xem trước).
  - Test: `[MonetizationOn]` (`Assets/Tests/EditMode/MonetizationOnAttribute.cs`) đặt và trả lại cờ quanh test mua hàng cũ; `MonetizationFlagTests`, `MonetizationFlagScreenTests`.
- Khi tắt, quyền lợi đã trả giữ nguyên: Vé trăng vẫn gửi thư cà rốt, Vé tháng đã mua vẫn có hàng, Vé Bỏ quảng cáo còn hạn vẫn có nút "Nhận ngay". Save vẫn v14.

### Constraints

- Không xoá mã kiếm tiền, không đổi gói, giá, trần (phạm vi #144).
- Issue yêu cầu "không rải `#if` khắp nơi": đọc qua một thuộc tính chung.

### Requirements

- Tắt: không chỗ mua, không giá, không nút video, không tạo `IStorefront`/`IRewardedAds` thật, không gửi sự kiện mua.
- Bật: mọi test mua hàng cũ xanh.

## Decision

Một thuộc tính tĩnh có thể ghi trong Core, giá trị đầu là hằng biên dịch theo define. Mọi chỗ bán đọc `Features.Monetization`; không có cờ thứ hai cho từng phần.

### Architecture Diagram

```
VUONTRA_MONETIZATION (BuildScript / Player Settings)
        -> Features.MonetizationByDefault (const) -> Features.Monetization (static, set in tests)
              -> Core: AnalyticsEvents.Sends, Intros, MoonPass
              -> Game: GameRunner (Store, Ads, Offers) -> Shop/GoldenPacks/Offer/MonthPass views
```

### Key Interfaces

- `VuonTra.Core.Features.Monetization` (`bool`, get/set), `Features.MonetizationByDefault` (`const bool`).
- `[MonetizationOn(bool on = true)]` cho test NUnit.

### Implementation Guidelines

- Mọi chỗ hiện giá, mua, video hay gói mới must kiểm `Features.Monetization`.
- Code must never tạo adapter cửa hàng hay quảng cáo thật khi cờ tắt.
- Sự kiện analytics mới liên quan mua must được thêm vào `AnalyticsEvents.Monetization`.
- Tắt cờ must never thu hồi quyền lợi đã trả.
- Test cần cờ bật must dùng `[MonetizationOn]`, không đặt cờ trực tiếp mà không trả lại.

## Alternatives Considered

### Alternative 1: Cờ trong ScriptableObject cấu hình game

- **Description**: thân issue #144 cho phép "ScriptableObject cấu hình game hoặc hằng trong Core có thể ghi đè".
- **Pros**: chỉnh trong Inspector.
- **Cons**: Core không đọc được ScriptableObject (ADR-0001); cờ được đọc cả trong Core.
- **Estimated Effort**: tương đương.
- **Rejection Reason**: không ghi lại; chọn hằng Core có thể ghi đè. INTENT UNKNOWN ngoài ràng buộc của ADR-0001.

### Alternative 2: Mục bật cờ trong Tạm nghỉ → Cài đặt của bản Development

- **Description**: thân issue #144 nêu như một cách bật cho bản build thử.
- **Pros**: bật không cần dựng lại.
- **Cons**: thêm giao diện.
- **Estimated Effort**: cao hơn define.
- **Rejection Reason**: chưa làm; báo cáo #144 ghi "Chưa có mục bật cờ trong Cài đặt bản Development (dùng define)".

## Consequences

### Positive

- Một chỗ duy nhất; bản build mặc định không có chỗ nào đụng tiền thật.
- Bật lại là về đúng như trước (save, gói, giá không đổi).

### Negative

- Trạng thái tĩnh có thể ghi: test quên trả cờ sẽ làm hỏng test sau; `[MonetizationOn]` giảm thiểu.
- Đồng hồ gói ưu đãi đứng yên khi tắt; bật lại thì gói người mới tính giờ từ lúc bật.

### Neutral

- Nút "Nhận ngay" của Vé Bỏ quảng cáo còn hạn vẫn hiện khi tắt (chờ PO quyết).

## Risks

- Chỗ bán mới quên kiểm cờ. Giảm thiểu: `MonetizationFlagScreenTests` quét Tiệm và Tạm nghỉ khi tắt.
- Bật cờ khi `AdsInPlayerBuilds` cũng bật sẽ dùng quảng cáo giả trong bản người chơi (ADR-0003).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| [co-kiem-tien.md](../../design/gdd/co-kiem-tien.md) | Một cờ, mặc định tắt, giữ quyền lợi đã trả | `Features.Monetization` |
| [tiem-va-ca-rot-vang.md](../../design/gdd/tiem-va-ca-rot-vang.md) | Không nút "Mua thêm", không giá khi tắt | `ShopView`, `GoldenPacksView`, `GameRunner.PriceOf` |
| [nhiem-vu-ngay-tuan-thang.md](../../design/gdd/nhiem-vu-ngay-tuan-thang.md) | Vé tháng chỉ còn hàng miễn phí khi tắt | `MonthPassView`, `Intros` |
| [di-vang.md](../../design/gdd/di-vang.md) | Không ×2 qua video khi tắt | `GameRunner.AdOfferFor` truyền `null` provider |

GDD đang viết song song; đối chiếu lại khi xong.

## Performance Implications

- **CPU**: một lần đọc `bool` ở mỗi chỗ; không đáng kể.
- **Memory**: không tạo SDK cửa hàng khi tắt.
- **Load Time**: không gọi `UnityPurchasing.Initialize` khi tắt.
- **Network**: không có.

## Migration Plan

Đã xong ở `457d960`. Bật hẳn: thêm `VUONTRA_MONETIZATION` vào Player Settings → Scripting Define Symbols.

**Rollback plan**: build với `VUONTRA_MONETIZATION=1`; mọi chỗ bán hiện lại như trước #144.

## Validation Criteria

- `MonetizationFlagTests.Selling_Is_Off_Unless_The_Build_Says_Otherwise` và các test `Off_*`/`On_*` xanh (chạy cả trong `tools/ci/core-tests.sh`).
- `MonetizationFlagScreenTests` xanh trong `tools/verify.sh`.
- `grep -rn "VUONTRA_MONETIZATION" ProjectSettings` không có kết quả (mặc định tắt) cho tới khi chủ project bật.

## Related

- ADR-0003 (seam), ADR-0001.
- `docs/phat-hanh/LO-TRINH-KIEM-TIEN.md`, `docs/phat-hanh/PHEU-SU-KIEN.md`, `docs/anh/po-144/`.

# ADR-0001: Luật chơi nằm trong assembly `VuonTra.Core` thuần C#, tách khỏi `VuonTra.Game`

> **Reverse-documented** from code and history at commit 9663644 on 2026-11-26; rationale not stated in docs is marked INTENT UNKNOWN.

## Status

Accepted

## Date

2026-11-21 (commit `db1051f` "feat: Unity project scaffold and tested game rules": BigNum, kinh tế trạm, mô phỏng, save codec trong một assembly không có engine, 76 test). Bước chạy test Core không cần Unity vào CI ngày 2026-11-24 (`7596a9b`).

## Last Verified

2026-11-26

## Decision Makers

Chủ project và session lập kế hoạch mốc 0–1 (`docs/superpowers/plans/2026-11-21-moc-0-1-vong-choi-chinh.md`, dòng "Architecture").

## Summary

Luật chơi (kinh tế, mô phỏng, đi vắng, nhiệm vụ, mùa, save codec, cửa hàng, quảng cáo, analytics) phải chạy và test được mà không cần mở Unity. Toàn bộ luật nằm trong `Assets/VuonTra/Core` (`VuonTra.Core.asmdef`, `noEngineReferences: true`); `VuonTra.Game` (MonoBehaviour, uGUI) chỉ giữ trạng thái, gọi Core mỗi khung và vẽ.

## Engine Compatibility

| Field | Value |
|-------|-------|
| **Engine** | Unity 6000.6.10f1 (URP 17.6.0) |
| **Domain** | Core |
| **Layer** | Foundation |
| **Knowledge Risk** | MEDIUM (theo `docs/engine-reference/unity/VERSION.md`); riêng Core gần như không phụ thuộc Unity nên rủi ro thực tế LOW |
| **References Consulted** | `docs/engine-reference/unity/VERSION.md`, `Assets/VuonTra/Core/VuonTra.Core.asmdef`, `tools/ci/CoreTests.csproj` |
| **Post-Cutoff APIs Used** | None |
| **Verification Required** | Core phải biên dịch được bằng .NET SDK ngoài Unity (`net8.0`, `LangVersion 9.0`); nếu Unity nâng phiên bản C#, giữ Core ở cú pháp cả hai bên cùng hiểu |

## ADR Dependencies

| Field | Value |
|-------|-------|
| **Depends On** | None |
| **Enables** | ADR-0002, ADR-0003, ADR-0004, ADR-0005 |
| **Blocks** | Mọi epic luật chơi (trạm, đi vắng, nhiệm vụ, mùa, tiệm) |
| **Ordering Note** | Là nền; mọi ADR khác giả định Core không biết Unity |

## Context

### Problem Statement

Game idle có nhiều luật số học (giá tăng theo cấp, thu nhập theo chu kỳ, trả bù khi đi vắng, BigNum) cần chỉnh nhanh và test nhiều. Mở Unity để chạy test thì chậm và không chạy được trên CI Linux rẻ.

### Current State

- `Assets/VuonTra/Core/VuonTra.Core.asmdef`: `"references": []`, `"noEngineReferences": true`. Grep `UnityEngine|UnityEditor` trong `Assets/VuonTra/Core` cho 0 kết quả.
- `Assets/VuonTra/Game/VuonTra.Game.asmdef` tham chiếu `VuonTra.Core` cùng uGUI, Input System, Localization, Play Games, Purchasing, Notifications.
- Cầu nối là `Assets/VuonTra/Game/GameRunner.cs` (`sealed partial class GameRunner : MonoBehaviour`, 1438 dòng cùng 6 tệp partial): `Init` dựng định nghĩa từ `GameCatalog` (`BuildDefs`, `BuildUpgrades`, `BuildQuests`, `BuildItems`), nạp save qua `SaveStore`, rồi gọi luật Core (`Quests.RollDaily`, `Intros.Settle`, `Rabbits.GiftFirst`…). `Update()` gọi `Simulation.Tick(Defs, State, dt, Payout, Mods, Roll, ProductMade)` hoặc `ClaimAway()` khi khung dài hơn 2 s. UI đọc `GameRunner.State` và nghe các `event` (`StateChanged`, `Payout`, `AwayClaimed`…).
- `tools/ci/core-tests.sh` gom mọi tệp trong `Assets/Tests/EditMode/*.cs` không chứa `UnityEngine|UnityEditor|VuonTra.Game|VuonTra.EditorTools` (61/112 tệp) vào `tools/ci/CoreTests.csproj` cùng `Assets/VuonTra/Core/**/*.cs`, rồi `dotnet test`. Chạy ngày 26/11: **963 test qua, 0 lỗi, ~1 s**.
- `.github/workflows/checks.yml` job `core-tests` chạy script đó trên `ubuntu-latest` với .NET 8.

### Constraints

- Core không được dùng `JsonUtility`, `Mathf`, `Time`, `Debug`; save được viết tay (`SaveCodec.cs`), như kế hoạch mốc 0–1 ghi rõ.
- Một session khác (B) cùng sửa code; Unity chỉ chạy được tối đa 3 lô một lúc trên máy chủ project, nên test không cần Unity là đường thử nhanh duy nhất.

### Requirements

- Luật chơi test được trong vài giây, không cần Editor.
- Mô phỏng kinh tế lặp lại được (hạt cố định) để chỉnh nhịp.

## Decision

Mọi luật chơi và dữ liệu luật nằm trong `VuonTra.Core`; `VuonTra.Game` là lớp vỏ Unity.

### Architecture Diagram

```
VuonTra.Core (noEngineReferences)          VuonTra.Game (MonoBehaviour, uGUI)
  GameState, Simulation, Economy, BigNum  <- GameRunner (Update -> Simulation.Tick)
  Quests, Seasons, Offers, Storefront...  <- GameCatalog.Build*() -> StationDef/QuestDef...
  IStorefront, IRewardedAds, IAnalytics   <- Game/Store, Game/Platform adapters
        ^                                        ^
  tools/ci/CoreTests.csproj (dotnet)       VuonTra.Core.Tests.asmdef (Unity EditMode)
```

### Key Interfaces

- `GameState` (dữ liệu save), `Simulation.Tick/TryBuy/Tap/Budget`, các lớp tĩnh luật (`Quests`, `Rabbits`, `Seasons`, `Offers`…).
- Các `*Def` thuần (`StationDef`, `UpgradeDef`, `QuestDef`, `ItemDef`) là thứ Core nhận vào (ADR-0002).
- Seam nền tảng là interface Core (ADR-0003).

### Implementation Guidelines

- Mã trong `Assets/VuonTra/Core` must never tham chiếu `UnityEngine`, `UnityEditor` hay `VuonTra.Game`.
- Luật mới must nằm trong Core kèm test không tham chiếu Unity, để `core-tests.sh` tự nhặt.
- `GameRunner` must chỉ chuyển lệnh và thời gian vào Core rồi phát `event`; không tính luật trong View.
- Test cần Unity (màn hình, asset) must để ở tệp riêng; một tệp có `UnityEngine` sẽ bị loại khỏi CI nhanh cả tệp.

## Alternatives Considered

Không có phương án thay thế nào được ghi lại. Kế hoạch mốc 0–1 chọn Core thuần ngay từ đầu, không nêu phương án khác. INTENT UNKNOWN về việc có cân nhắc đặt luật trong MonoBehaviour hay không.

## Consequences

### Positive

- 963 test luật chạy trong khoảng 1 s ngoài Unity; CI Linux chạy chúng ở mỗi push.
- Mô phỏng kinh tế chỉnh được bằng .NET SDK đi kèm bản cài Unity (`core-tests.sh` tự rơi về `.../6000.6.10f1/.../DotNetSdk/dotnet` khi máy không có `dotnet`).

### Negative

- Mọi thứ Unity cần (sprite, màu, thời gian thật) phải đi qua `GameRunner`; tệp này đã lớn 1438 dòng.
- Save phải viết tay vì không có `JsonUtility`.

### Neutral

- `VuonTra.Core.Tests.asmdef` vẫn tham chiếu `VuonTra.Game` và `VuonTra.Editor` cho test màn hình; tách theo tệp, không theo assembly.

## Risks

- Một test lõi thêm `using UnityEngine` sẽ âm thầm rời CI nhanh. Giảm thiểu: so số test `core-tests` giữa các lần chạy.
- `GameRunner` phình thêm. Giảm thiểu: tách partial theo tính năng như đã làm (`GameRunner.Offers.cs`…).

## GDD Requirements Addressed

| GDD System | Requirement | How This ADR Addresses It |
|------------|-------------|--------------------------|
| [tram-va-thu-nhap.md](../../design/gdd/tram-va-thu-nhap.md) | Giá, thu nhập, chu kỳ trạm và tăng cấp | `Economy`, `Simulation` trong Core, test không cần Unity |
| [di-vang.md](../../design/gdd/di-vang.md) | Trả bù khi đi vắng | `Away`, `ClaimAway` gọi Core; khung > 2 s cũng đi đường này |
| [nhiem-vu-ngay-tuan-thang.md](../../design/gdd/nhiem-vu-ngay-tuan-thang.md) | Đổi nhiệm vụ theo ngày | `Quests.RollDaily` trong Core |
| [mua-va-le-hoi.md](../../design/gdd/mua-va-le-hoi.md) | Mùa theo ngày | `Seasons` trong Core |

Foundational: mọi GDD còn lại đặt luật vào Core theo ADR này. Các GDD đang viết song song; đối chiếu lại yêu cầu khi chúng xong.

## Performance Implications

- **CPU**: một `Simulation.Tick` mỗi khung, không cấp phát theo thiết kế (chưa đo riêng).
- **Memory**: không đáng kể.
- **Load Time**: không đổi.
- **Network**: không có.

## Migration Plan

Không cần; quyết định có từ commit đầu tiên.

**Rollback plan**: không áp dụng; bỏ `noEngineReferences` sẽ phá `core-tests.sh` và CI.

## Validation Criteria

- `Assets/VuonTra/Core/VuonTra.Core.asmdef` còn `"noEngineReferences": true` và `"references": []`.
- `grep -rE "UnityEngine|UnityEditor|VuonTra\.Game" Assets/VuonTra/Core` không có kết quả.
- `tools/ci/core-tests.sh` xanh (963 test ngày 26/11) và job `core-tests` của `.github/workflows/checks.yml` xanh.

## Related

- ADR-0002 (dữ liệu ScriptableObject soi sang `*Def` thuần), ADR-0003 (seam nền tảng là interface Core).
- `CLAUDE.md` mục "Enforced Rules" (ghi "701 tests", đã cũ: nay 963).
- `docs/superpowers/plans/2026-11-21-moc-0-1-vong-choi-chinh.md`, `docs/save-va-test.md`.
- `Assets/VuonTra/Game/GameRunner.cs`, `tools/ci/core-tests.sh`, `tools/ci/CoreTests.csproj`.
